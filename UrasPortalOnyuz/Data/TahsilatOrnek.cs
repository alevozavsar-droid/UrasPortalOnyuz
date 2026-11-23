// ============================================================
// TAHSİLAT (Müşteri tahsilatı) — Tahsilat ekranı (Controllers/TahsilatController.cs, Views/Rapor117/Index.cshtml, wwwroot/js/tahsilat.js)
//   • ÖDEME SATIRLARI: bir tahsilatta birden çok ödeme (nakit + çek + çek + havale …) birlikte girilir — her satırda yöntem, tarih (çek / senette vade), belge no, banka, kur, tutar.
//     Kur OTOMATİK (o günün TCMB kuru, Data/KurOrnek.cs) ya da ELLE.
//   • FATURA SEÇİMİ: OTOMATİK (tüm açık faturalar en eski vadeliden FIFO) ya da MANUEL (kapatılacak faturalar işaretlenir, istenirse tutar sınırı yazılır).
//   • SİMÜLASYON (Adat mantığı): ödemeler tarih sırasıyla (aynı gün önce nakit) faturalara FIFO uygulanır; her eşleşmede gün (ödeme tarihi − fatura vadesi), VADE FARKI (tutar × kur × aylık oran / 30 × gün, gün > 0)
//     ve KUR FARKI (dövizli faturada tutar × (ödeme kuru − fatura kuru)). Artan ödeme bakiyeye avans (Otomatik Kapatma'da eşleştirilmemiş tahsilat).
//   • KAYIT: "Kaydet ve İşle" → Cari Mutabakat hareketleri (satır başına Tahsilat / Çek / Senet, X / R) eklenir, faturaların kalanı düşer, Konsolide Finansal Takip Paneli'ne düşer;
//     "Taslak Kaydet" → cariye işlenmez, listede Taslak olarak durur, istenen zaman "İşleme Al" ile aynı kayıttan işlenir (ibraz).
//   • Basit fatura kesme: kur farkı / vade farkı için tek tıkla KDV'li fark faturası (KFF / VFF). PDF / Excel: Controllers/TahsilatController.cs + Data/DisaAktar.cs
//   • ÖDEME (Yapılan Ödemeler › Yeni Ödeme, Controllers/OdemeController.cs): AYNI MOTOR, YÖN TERS — cari tedarikçiyse kayıt Tur = "Ödeme" (ÖD-…): açık ALIŞ faturaları (alacak) FIFO kapanır,
//     işlenince cari hesabına BORÇ (Ödeme / Çek / Senet) yazılır, her ödeme satırı şirketin Nakit Akış Planlama'ya ÖDENDİ satır olarak düşer (Data/NakitAkisOrnek.cs) ve Yapılan Ödemeler listesinde görünür.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>Bir tahsilattaki ödeme satırı (nakit / çek / senet / havale / kart).</summary>
    public class TahsilatSatir
    {
        public int Sira { get; set; } public string Yontem { get; set; } = "Havale / EFT"; public DateTime Tarih { get; set; }   // ödeme tarihi; çek / senette vade
        public string BelgeNo { get; set; } = ""; public string Banka { get; set; } = ""; public string Pb { get; set; } = "TL"; public decimal Kur { get; set; } = 1; public bool KurElle { get; set; }
        public decimal Tutar { get; set; } public decimal TutarTl => Math.Round(Tutar * Kur, 2); public string Not { get; set; } = "";
        public string HareketNo { get; set; } = "";   // işlenince Cari Mutabakat hareket belge no
        public int? NakitAkisDocEntry { get; set; } public string NakitAkisDb { get; set; } = "";   // ödemede: işlenince açılan Nakit Akış Planlama satırı (ÖDENDİ)
        public bool Nakit => Yontem == "Nakit"; public bool Vadeli => Yontem == "Çek" || Yontem == "Senet";
    }
    /// <summary>Fatura ↔ ödeme satırı eşleşmesi (simülasyon / dağıtım).</summary>
    public class TahsilatDagitim
    {
        public string BelgeNo { get; set; } public DateTime FaturaTarihi { get; set; } public DateTime Vade { get; set; } public string Pb { get; set; } public string IslemTipi { get; set; }
        public decimal Kalan { get; set; } public decimal Tutar { get; set; }           // fatura para biriminde
        /// <summary>Kapamada kullanılan tutar ÖDEME satırının para biriminde (19 Kas 2026: çapraz para birimi — TL havale EUR faturayı kapatabilir; avans = satır tutarı − Σ OdemeTutar).</summary>
        public decimal OdemeTutar { get; set; }
        /// <summary>FaturaKuru: fatura günü kuru (fatura PB). TahsilatKuru: fatura PB'nin ödeme günü kuru (aynı PB'de satır kuru); TutarTl = Tutar × TahsilatKuru.</summary>
        public decimal FaturaKuru { get; set; } public decimal TahsilatKuru { get; set; }
        /// <summary>Kur farkı FATURASININ baz kuru (19 Kas 2026, çift sayım koruması): R faturada son muhasebe değerleme kuru (yoksa fatura kuru), X faturada fatura kuru. KurFarki = Tutar × (TahsilatKuru − BazKur).</summary>
        public decimal BazKur { get; set; } public string BazKaynak { get; set; } = "Fatura Kuru";
        public int GecikmeGun { get; set; } public decimal VadeFarki { get; set; } public decimal KurFarki { get; set; }   // gün: ödeme tarihi − vade (negatif = erken); VF yalnız gün > 0; TL
        /// <summary>Vade farkı hesaplanan gün: ödeme tarihi − (vade ya da bu faturaya en son vade farkı kesilen tarih). Ay sonu faturasıyla kesilmiş günler yeniden sayılmaz.</summary>
        public int VfGun { get; set; }
        public int OdemeSira { get; set; } public string OdemeYontem { get; set; } public DateTime OdemeTarihi { get; set; } public string OdemeBelge { get; set; }
        public decimal FaturaKalan { get; set; }   // eşleşme sonrası faturanın kalanı
        public decimal TutarTl => Math.Round(Tutar * TahsilatKuru, 2);
    }
    public class TahsilatKaydi
    {
        public string No { get; set; } public DateTime Tarih { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string Sirket { get; set; }
        /// <summary>Tahsilat (müşteriden, TH-…) | Ödeme (tedarikçiye, ÖD-…; Yapılan Ödemeler › Yeni Ödeme).</summary>
        public string Tur { get; set; } = "Tahsilat"; public bool Odeme => Tur == "Ödeme";
        public string ParaBirimi { get; set; } public string IslemTipi { get; set; } = "X"; public string Mod { get; set; } = "Otomatik"; public string Aciklama { get; set; } = ""; public string BelgeNo { get; set; } = "";
        public List<TahsilatSatir> Satirlar { get; set; } = new List<TahsilatSatir>();
        public Dictionary<string, decimal> FaturaSecim { get; set; } = new Dictionary<string, decimal>();   // Manuel: belge no → tutar sınırı (0 = kalanın tamamı)
        /// <summary>İşlem Tipi süzgeci (X / R; boş = tümü): kapatılacak açık fatura kümesi bu tiplerle sınırlıdır (19 Kas 2026). İşleme alırken yeniden hesapta da kullanılır.</summary>
        public List<string> Tipler { get; set; } = new List<string>();
        public List<TahsilatDagitim> Dagitim { get; set; } = new List<TahsilatDagitim>();
        public string Durum { get; set; } = "Taslak";   // Taslak | İşlendi
        public string Kullanici { get; set; } = ""; public DateTime Zaman { get; set; } public string IslemeAlan { get; set; } = ""; public DateTime? IslemeZamani { get; set; }
        public string KurFarkiFaturaNo { get; set; } = ""; public string VadeFarkiFaturaNo { get; set; } = "";
        public decimal Tutar => Math.Round(Satirlar.Sum(s => s.Tutar), 2); public decimal TutarTl => Math.Round(Satirlar.Sum(s => s.TutarTl), 2);
        public decimal Kur => Tutar > 0 ? Math.Round(TutarTl / Tutar, 4) : 1;
        public string Yontem => Satirlar.Count == 0 ? "" : string.Join(" + ", Satirlar.GroupBy(s => s.Yontem).Select(g => g.Count() > 1 ? $"{g.Count()} {g.Key}" : g.Key));
        public decimal Dagitilan => Math.Round(Dagitim.Sum(d => d.OdemeTutar), 2); public decimal Avans => Math.Round(Tutar - Dagitilan, 2);   // tahsilat para biriminde
        public decimal KurFarki => Math.Round(Dagitim.Sum(d => d.KurFarki), 2); public decimal VadeFarki => Math.Round(Dagitim.Sum(d => d.VadeFarki), 2);
        public bool Islendi => Durum == "İşlendi";
    }
    public class FarkFaturasi
    {
        public string No { get; set; } public DateTime Tarih { get; set; } public DateTime Vade { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string CariUnvan { get; set; } public string VergiNo { get; set; } public string VergiDairesi { get; set; }
        public string Tur { get; set; }   // Kur Farkı | Vade Farkı
        public string TahsilatNo { get; set; } public string IslemTipi { get; set; } = "X";
        /// <summary>Tahsilat (tahsilat işlenirken doğan fark) | Ay Sonu (her ay sonu vadesi geçmiş bakiyelere otomatik) | Elle (müşteri bazında, bugüne kadar). Dönem: yyyy-MM (müşteri bazlı faturada).</summary>
        public string Kaynak { get; set; } = "Tahsilat"; public string Donem { get; set; } = "";
        public List<(string Aciklama, decimal Tutar)> Kalemler { get; set; } = new List<(string, decimal)>();
        public decimal Matrah => Math.Round(Kalemler.Sum(k => k.Tutar), 2); public decimal KdvOran { get; set; } = 20m; public decimal Kdv => Math.Round(Matrah * KdvOran / 100m, 2); public decimal Toplam => Math.Round(Matrah + Kdv, 2);
        public string Kullanici { get; set; } = ""; public DateTime Zaman { get; set; } public string Durum { get; set; } = "Kesildi";
    }
    /// <summary>Cari tahsilat özeti: son ödeme, son yükleme (fatura), ortalama ödeme gecikmesi, açık / vadesi geçmiş.</summary>
    public class TahsilatCariBilgi
    {
        public DateTime? SonOdeme { get; set; } public decimal SonOdemeTutar { get; set; } public string SonOdemePb { get; set; } public string SonOdemeTur { get; set; } public string SonOdemeBelge { get; set; }
        public DateTime? SonYukleme { get; set; } public decimal SonYuklemeTutar { get; set; } public string SonYuklemePb { get; set; } public string SonYuklemeBelge { get; set; }
        public decimal OrtalamaGecikme { get; set; } public int OdemeAdet12Ay { get; set; } public decimal Odeme12AyTl { get; set; } public decimal Fatura12AyTl { get; set; } public int FaturaAdet12Ay { get; set; }
        public int EslesmemisOdeme { get; set; } public decimal EslesmemisOdemeTl { get; set; } public int VadeBekleyenCek { get; set; } public decimal VadeBekleyenCekTl { get; set; }
    }

    public static class TahsilatOrnek
    {
        public static readonly CultureInfo Tr = CariKartOrnek.Tr; private static readonly object Kilit = new object();
        /// <summary>Vade farkı aylık oranı (%); günlük = oran / 30. Fark faturalarında KDV oranı (%).</summary>
        public static decimal VadeFarkiAylikOran = 5m; public static decimal KdvOran = 20m;   // 19 Kas 2026 (kullanıcı): aylık vade farkı oranı %5 (Adat Hesaplama ile aynı); eskisi %3,5
        /// <summary>Müşterinin aylık vade farkı oranı (%): Vade Farkı Takibi'nde müşteriye özel girilmişse o, yoksa genel oran. Tahsilat kapaması, ay sonu / robot / elle kesim ve adat önerisi bunu kullanır.</summary>
        public static decimal Oran(string kod) => TahsilatTakipKayit.MusteriOran(kod) ?? VadeFarkiAylikOran;
        /// <summary>Vade farkı uygulanmayan tolerans (gün).</summary>
        public static int VadeToleransGun = 0;
        public static readonly string[] Yontemler = { "Havale / EFT", "Nakit", "Çek", "Senet", "Kredi Kartı" };
        public static readonly string[] IslemTipleri = { "X", "R" };
        public static readonly string[] Bankalar = { "Ziraat Bankası", "Halk Bankası", "İş Bankası", "Garanti BBVA", "Yapı Kredi", "Akbank", "QNB", "Vakıfbank", "Denizbank" };
        public static Dictionary<string, decimal> Kurlar => MutabakatOrnek.Kurlar;
        private static readonly List<TahsilatKaydi> _tahsilatlar = new List<TahsilatKaydi>(); private static readonly List<FarkFaturasi> _faturalar = new List<FarkFaturasi>();
        private static int _sayac = 0, _odemeSayac = 0, _kff = 0, _vff = 0; private static bool _tohumlandi;
        /// <summary>Müşteri tahsilatları (TH-…).</summary>
        public static List<TahsilatKaydi> Tahsilatlar { get { Tohumla(); lock (Kilit) return _tahsilatlar.Where(t => !t.Odeme).OrderByDescending(t => t.Zaman).ToList(); } }
        /// <summary>Tedarikçiye yapılan ödemeler (ÖD-…; Yapılan Ödemeler › Yeni Ödeme).</summary>
        public static List<TahsilatKaydi> Odemeler { get { Tohumla(); lock (Kilit) return _tahsilatlar.Where(t => t.Odeme).OrderByDescending(t => t.Zaman).ToList(); } }
        public static bool TedarikciMi(string kod) => CariKartOrnek.KartBul(kod ?? "")?.KartTipi == "Tedarikçi";
        /// <summary>İşlenmiş ödeme satırının Nakit Akış Planlama bağlantısı (satır çekmecesi).</summary>
        public static string NakitAkisLink(TahsilatSatir s) => s.NakitAkisDocEntry == null ? "" : $"/Rapor79?sirket={s.NakitAkisDb}&sekme={(s.Yontem == "Nakit" ? "nakit" : s.Yontem == "Çek" ? "cek" : s.Yontem == "Senet" ? "senet" : s.Yontem == "Havale / EFT" ? "havale" : "kredi")}&doc={s.NakitAkisDocEntry}";
        public static List<FarkFaturasi> Faturalar { get { Tohumla(); lock (Kilit) return _faturalar.OrderByDescending(f => f.Zaman).ToList(); } }
        public static TahsilatKaydi TahsilatBul(string no) { Tohumla(); lock (Kilit) return _tahsilatlar.FirstOrDefault(t => t.No == no); }
        public static FarkFaturasi FaturaBul(string no) { Tohumla(); lock (Kilit) return _faturalar.FirstOrDefault(t => t.No == no); }
        static decimal Kur(string pb) => Kurlar.TryGetValue(pb ?? "TL", out var k) ? k : 1m;
        public static bool TlMi(string pb) => string.IsNullOrEmpty(pb) || pb == "TL" || pb == "TRY";
        /// <summary>Tarihe göre OTOMATİK kur: bugünkü ekstre kuru (MutabakatOrnek.Kurlar) TCMB serisinin (KurOrnek) o güne oranıyla geriye / ileriye götürülür. TL'de 1.</summary>
        public static decimal KurOto(string pb, DateTime tarih)
        {
            if (TlMi(pb)) return 1m; var bugun = KurOrnek.Tcmb(pb, DateTime.Today).alis; var gun = KurOrnek.Tcmb(pb, tarih).alis;
            return bugun <= 0 ? Kur(pb) : Math.Round(Kur(pb) * gun / bugun, 4);
        }
        /// <summary>Fatura tarihindeki kur (TCMB serisi; TL faturada 1).</summary>
        public static decimal FaturaKuru(MtHareket f) => TlMi(f.ParaBirimi) ? 1m : KurOto(f.ParaBirimi, f.Tarih);
        /// <summary>Carinin açık faturaları (vade sırasıyla): müşteride satış faturaları (borç), tedarikçide ALIŞ faturaları (alacak).</summary>
        public static List<MtHareket> AcikFaturalar(string kod)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return new List<MtHareket>(); bool ted = c.Kart?.KartTipi == "Tedarikçi";
            return c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && (ted ? h.Alacak > 0 : h.Borc > 0) && h.Kalan > 0.005m).OrderBy(h => h.Vade ?? h.Tarih).ThenBy(h => h.Tarih).ToList();
        }
        /// <summary>Cari tahsilat özeti (son ödeme, son yükleme, ortalama gecikme, 12 ay hacim, eşleşmemiş ödeme, vade bekleyen çek).</summary>
        public static TahsilatCariBilgi CariBilgi(string kod)
        {
            var b = new TahsilatCariBilgi(); var c = MutabakatOrnek.Bul(kod); if (c == null) return b; var bugun = DateTime.Today; var yil = bugun.AddMonths(-12);
            bool ted = c.Kart?.KartTipi == "Tedarikçi";   // tedarikçide ödeme = bizim ödememiz (borç), fatura = alış faturası (alacak)
            decimal KurTl(string pb) => TlMi(pb) ? 1m : Kur(pb); decimal Ode(MtHareket h) => ted ? h.Borc : h.Alacak; decimal Fat(MtHareket h) => ted ? h.Alacak : h.Borc;
            var odemeler = c.Hareketler.Where(h => (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Ödeme" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && Ode(h) > 0).OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList();
            var faturalar = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && Fat(h) > 0).OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList();
            var so = odemeler.LastOrDefault(); if (so != null) { b.SonOdeme = so.Tarih; b.SonOdemeTutar = Ode(so); b.SonOdemePb = so.ParaBirimi; b.SonOdemeTur = so.BelgeTipi == "Tahsilat" || so.BelgeTipi == "Ödeme" ? (so.Aciklama ?? "").Split('·')[0].Trim() : so.BelgeTipi; b.SonOdemeBelge = so.BelgeNo; }
            var sy = faturalar.LastOrDefault(); if (sy != null) { b.SonYukleme = sy.Tarih; b.SonYuklemeTutar = Fat(sy); b.SonYuklemePb = sy.ParaBirimi; b.SonYuklemeBelge = sy.BelgeNo; }
            b.OdemeAdet12Ay = odemeler.Count(h => h.Tarih >= yil); b.Odeme12AyTl = Math.Round(odemeler.Where(h => h.Tarih >= yil).Sum(h => Ode(h) * KurTl(h.ParaBirimi)), 2);
            b.FaturaAdet12Ay = faturalar.Count(h => h.Tarih >= yil); b.Fatura12AyTl = Math.Round(faturalar.Where(h => h.Tarih >= yil).Sum(h => Fat(h) * KurTl(h.ParaBirimi)), 2);
            // ortalama gecikme: ödeme açıklamasında geçen fatura numaralarına göre (ödeme tarihi − fatura vadesi), tutar ağırlıklı, son 12 ay
            decimal agirlik = 0, toplam = 0;
            foreach (var o in odemeler.Where(h => h.Tarih >= yil))
                foreach (var f in faturalar.Where(f => !string.IsNullOrEmpty(o.Aciklama) && o.Aciklama.Contains(f.BelgeNo)))
                { int gun = (o.Tarih.Date - (f.Vade ?? f.Tarih).Date).Days; decimal w = Fat(f) * KurTl(f.ParaBirimi); agirlik += w; toplam += w * gun; }
            b.OrtalamaGecikme = agirlik > 0 ? Math.Round(toplam / agirlik, 1) : 0;
            var es = c.Hareketler.Where(h => (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Ödeme" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.Kalan > 0.005m).ToList(); b.EslesmemisOdeme = es.Count; b.EslesmemisOdemeTl = Math.Round(es.Sum(h => h.Kalan * KurTl(h.ParaBirimi)), 2);
            var ck = c.Hareketler.Where(h => (h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.KapamaDurumu == "Vade Bekliyor").ToList(); b.VadeBekleyenCek = ck.Count; b.VadeBekleyenCekTl = Math.Round(ck.Sum(h => Ode(h) * KurTl(h.ParaBirimi)), 2);
            return b;
        }
        /// <summary>Satırları temizler: sıra, tarih, otomatik kur (elle değilse), para birimi.</summary>
        public static List<TahsilatSatir> SatirlariHazirla(List<TahsilatSatir> satirlar, string pb, DateTime varsayilanTarih)
        {
            var l = (satirlar ?? new List<TahsilatSatir>()).Where(s => s != null && s.Tutar > 0).ToList(); int i = 0;
            foreach (var s in l)
            {
                s.Sira = ++i; s.Pb = string.IsNullOrEmpty(s.Pb) ? pb : s.Pb; if (s.Tarih == default) s.Tarih = varsayilanTarih; s.Tarih = s.Tarih.Date; s.Yontem = Yontemler.Contains(s.Yontem ?? "") ? s.Yontem : Yontemler[0];
                if (TlMi(s.Pb)) { s.Kur = 1; s.KurElle = false; } else if (!s.KurElle || s.Kur <= 0) { s.Kur = KurOto(s.Pb, s.Tarih); s.KurElle = false; }
                s.Tutar = Math.Round(s.Tutar, 2); s.BelgeNo = (s.BelgeNo ?? "").Trim(); s.Banka = (s.Banka ?? "").Trim(); s.Not = (s.Not ?? "").Trim();
            }
            return l;
        }
        /// <summary>
        /// Dağıtım simülasyonu (FIFO): ödeme satırları tarih sırasıyla (aynı gün önce nakit) açık faturalara uygulanır. Mod "Otomatik" → tüm açık faturalar (en eski vade önce);
        /// "Manuel" → yalnız secim'deki faturalar (değer > 0 ise tutar sınırı). Her eşleşmede gün, vade farkı ve kur farkı.
        /// </summary>
        public static List<TahsilatDagitim> Hesapla(string kod, string pb, string mod, Dictionary<string, decimal> secim, List<TahsilatSatir> satirlar, List<string> tipler = null)
        {
            // 19 Kas 2026 (kullanıcı: "Tahsilat raporundaki tüm sekmeler birbiriyle uyumlu olmalı" — Yeni Tahsilat yalnız tahsilat para birimindeki faturaları gösteriyordu, Adat hepsini):
            //   para birimi süzgeci kalktı; bütün açık faturalar en eski vadeden başlayarak kapanır. Çapraz para birimi: ödeme satırı kendi kuruyla TL'ye, fatura para birimi ödeme günü kuruyla
            //   (aynı para biriminde satır kuru) fatura tutarına çevrilir. Tutar = fatura PB, OdemeTutar = ödeme PB; kur farkı fatura PB üzerinden (ödeme günü kuru − fatura günü kuru).
            var l = new List<TahsilatDagitim>(); var faturalar = AcikFaturalar(kod).ToList();
            if (tipler != null && tipler.Count > 0) faturalar = faturalar.Where(f => tipler.Contains(f.IslemTipi ?? "X")).ToList();   // 19 Kas 2026: İşlem Tipi çoklu seçimi (X / R) açık fatura kümesini süzer; boş = tümü
            if (mod == "Cari") return l;   // CARİYE İSTİNADEN · kapamasız (eski kayıtlar; ekranda artık seçilmez)
            if (mod == "Manuel") faturalar = faturalar.Where(f => secim != null && secim.ContainsKey(f.BelgeNo)).ToList();
            var odemeler = SatirlariHazirla(satirlar, pb, DateTime.Today).OrderBy(s => s.Tarih).ThenBy(s => s.Nakit ? 0 : s.Vadeli ? 2 : 1).ThenBy(s => s.Sira).Select(s => (S: s, Kalan: s.Tutar)).ToList();
            int pi = 0;
            foreach (var f in faturalar)
            {
                string fpb = TlMi(f.ParaBirimi) ? "TL" : f.ParaBirimi;
                decimal cap = f.Kalan; if (mod == "Manuel" && secim.TryGetValue(f.BelgeNo, out var sinir) && sinir > 0) cap = Math.Min(cap, Math.Round(sinir, 2));
                decimal fKalan = f.Kalan; var vade = (f.Vade ?? f.Tarih).Date; decimal fk = FaturaKuru(f); var baz = TlMi(fpb) ? (Kur: 1m, Kaynak: "TL", Tarih: (DateTime?)null) : KurDegerlemeOrnek.BazKur(kod, f);
                while (cap > 0.005m && pi < odemeler.Count)
                {
                    var o = odemeler[pi]; if (o.Kalan <= 0.005m) { pi++; continue; }
                    decimal kurF = TlMi(fpb) ? 1m : (fpb == (TlMi(o.S.Pb) ? "TL" : o.S.Pb) ? o.S.Kur : KurOto(fpb, o.S.Tarih));
                    decimal odemeTl = o.Kalan * o.S.Kur, capTl = cap * kurF, alTl = Math.Min(capTl, odemeTl);
                    decimal al = Math.Round(alTl / kurF, 2), alOdeme = Math.Round(alTl / o.S.Kur, 2);
                    if (alTl >= capTl - 0.004m) al = Math.Round(cap, 2); if (alTl >= odemeTl - 0.004m) alOdeme = Math.Round(o.Kalan, 2);   // yuvarlama artığı bırakma
                    cap -= al; fKalan = Math.Round(fKalan - al, 2); odemeler[pi] = (o.S, Math.Round(o.Kalan - alOdeme, 2));
                    // vade farkı günü: ay sonu / elle faturasıyla kesilmiş günler yeniden sayılmaz (başlangıç = son kesim tarihi); vade farkı ve kur farkı faturalarına vade farkı işlemez
                    int gun = (o.S.Tarih - vade).Days; var vfBas = VfBaslangic(kod, f); int vfGun = VfDisi(f) ? 0 : Math.Max(0, (o.S.Tarih - vfBas).Days - (vfBas == vade ? VadeToleransGun : 0));
                    l.Add(new TahsilatDagitim { BelgeNo = f.BelgeNo, FaturaTarihi = f.Tarih, Vade = vade, Pb = fpb, IslemTipi = f.IslemTipi, Kalan = f.Kalan, Tutar = al, OdemeTutar = alOdeme, FaturaKuru = fk, TahsilatKuru = kurF, BazKur = baz.Kur, BazKaynak = baz.Kaynak, GecikmeGun = gun, VfGun = vfGun, VadeFarki = Math.Round(al * kurF * Oran(kod) / 100m / 30m * vfGun, 2), KurFarki = TlMi(fpb) ? 0 : Math.Round(al * (kurF - baz.Kur), 2), OdemeSira = o.S.Sira, OdemeYontem = o.S.Yontem, OdemeTarihi = o.S.Tarih, OdemeBelge = o.S.BelgeNo, FaturaKalan = fKalan });
                    if (odemeler[pi].Kalan <= 0.005m) pi++;
                }
            }
            return l;
        }
        /// <summary>Tahsilatı kaydeder (isle=true: cariye işler; false: taslak). Hata metni ya da null.</summary>
        public static string Kaydet(string kod, DateTime tarih, string islemTipi, string pb, string mod, Dictionary<string, decimal> secim, List<TahsilatSatir> satirlar, string aciklama, string kullanici, bool isle, out TahsilatKaydi kayit, DateTime? simdi = null, List<string> tipler = null)
        {
            kayit = null; Tohumla();
            var k = CariKartOrnek.KartBul(kod); if (k == null) return "Cari bulunamadı."; bool ted = k.KartTipi == "Tedarikçi"; if (!ted && k.KartTipi != "Müşteri") return "Tahsilat yalnız müşteri, ödeme yalnız tedarikçi carilerinde kaydedilir.";
            pb = string.IsNullOrEmpty(pb) ? "TL" : pb; var l = SatirlariHazirla(satirlar, pb, tarih); if (l.Count == 0) return "En az bir ödeme satırı girin (tutar sıfırdan büyük)."; if (l.Any(s => s.Pb != pb)) return "Tüm ödeme satırları tahsilat para biriminde olmalı.";
            if (l.Any(s => s.Kur <= 0)) return "Kur sıfırdan büyük olmalı."; if (l.Any(s => s.Vadeli && string.IsNullOrEmpty(s.BelgeNo))) return "Çek / senet satırlarında belge (çek) no zorunludur."; if (!IslemTipleri.Contains(islemTipi ?? "")) return "İşlem tipi X ya da R olmalı.";
            mod = mod == "Manuel" ? "Manuel" : mod == "Cari" ? "Cari" : "Otomatik"; if (mod == "Manuel" && (secim == null || secim.Count == 0)) return "Manuel modda kapatılacak faturaları işaretleyin.";   // Cari: cariye istinaden, fatura seçimi yok
            var dag = Hesapla(kod, pb, mod, secim, l, tipler);
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var no = ted ? $"ÖD-{tarih:yyyy}-{++_odemeSayac:00000}" : $"TH-{tarih:yyyy}-{++_sayac:00000}";
                kayit = new TahsilatKaydi { No = no, Tur = ted ? "Ödeme" : "Tahsilat", Tarih = tarih.Date, CariKodu = k.Kod, CariAd = k.KisaAd, Sirket = k.Sirket, ParaBirimi = pb, IslemTipi = islemTipi, Mod = mod, Aciklama = (aciklama ?? "").Trim(), BelgeNo = no, Satirlar = l, FaturaSecim = mod == "Manuel" ? new Dictionary<string, decimal>(secim) : new Dictionary<string, decimal>(), Tipler = (tipler ?? new List<string>()).ToList(), Dagitim = dag, Durum = "Taslak", Kullanici = kullanici ?? "", Zaman = z };
                _tahsilatlar.Insert(0, kayit);
                CariKartOrnek.Gunluk(k, z, kullanici, ted ? "Ödeme Taslağı" : "Tahsilat Taslağı", "", $"{no} · {kayit.Tutar.ToString("N2", Tr)} {pb} ({kayit.Yontem}, {islemTipi}) · {dag.Count} fatura eşleşmesi", ted ? "Ödeme" : "Tahsilat", kayit.Aciklama);
            }
            if (isle) { var h = IslemeAl(kayit.No, kullanici, simdi); if (h != null) return h; }
            return null;
        }
        /// <summary>
        /// Taslak kaydı cariye işler (ibraz): satır başına Cari Mutabakat hareketi, fatura kalanları düşer, avans eşleşmemiş tahsilat / ödeme olarak kalır. Hata metni ya da null.
        /// Tahsilat → alacak (Tahsilat / Çek / Senet); Ödeme → BORÇ (Ödeme / Çek / Senet) ve her satır için Nakit Akış Planlama'ya ÖDENDİ satır (NakitAkisaYaz).
        /// </summary>
        public static string IslemeAl(string no, string kullanici, DateTime? simdi = null)
        {
            var t = TahsilatBul(no); if (t == null) return "Kayıt bulunamadı."; if (t.Islendi) return $"{no} zaten işlenmiş.";
            var k = CariKartOrnek.KartBul(t.CariKodu); if (k == null) return "Cari bulunamadı."; bool od = t.Odeme; string ad = od ? "ödeme" : "tahsilat";
            // güncel açık faturalarla yeniden hesapla (taslak bekletilirken faturalar değişmiş olabilir)
            var dag = Hesapla(t.CariKodu, t.ParaBirimi, t.Mod, t.FaturaSecim, t.Satirlar, t.Tipler);
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; t.Dagitim = dag;
                foreach (var s in t.Satirlar)
                {
                    var kapat = dag.Where(d => d.OdemeSira == s.Sira).GroupBy(d => d.BelgeNo).Select(g => (g.Key, g.Sum(d => d.Tutar))).ToList(); decimal dagitilan = Math.Round(dag.Where(d => d.OdemeSira == s.Sira).Sum(d => d.OdemeTutar), 2), avans = Math.Round(s.Tutar - dagitilan, 2);   // ödeme para biriminde (kapat listesi fatura PB'sinde)
                    var h = new MtHareket { Tarih = s.Vadeli ? t.Tarih : s.Tarih, ParaBirimi = t.ParaBirimi, BelgeTipi = s.Yontem == "Çek" ? "Çek" : s.Yontem == "Senet" ? "Senet" : (od ? "Ödeme" : "Tahsilat"), BelgeNo = s.Vadeli && !string.IsNullOrEmpty(s.BelgeNo) ? s.BelgeNo : $"{t.No}/{s.Sira}", Alacak = od ? 0 : s.Tutar, Borc = od ? s.Tutar : 0, IslemTipi = t.IslemTipi,
                        Aciklama = $"{s.Yontem}{(string.IsNullOrEmpty(s.Banka) ? "" : " · " + s.Banka)}{(s.Vadeli ? $" · vade {s.Tarih:dd.MM.yyyy}" : "")} · {ad} {t.No} · {(t.Mod == "Manuel" ? "manuel" : t.Mod == "Cari" ? "cariye istinaden (hesaba)" : "otomatik")}{(kapat.Count > 0 ? " · " + string.Join(", ", kapat.Select(x => x.Key)) : "")}{(string.IsNullOrWhiteSpace(t.Aciklama) ? "" : " · " + t.Aciklama)}", KapamaDurumu = s.Vadeli ? (s.Tarih > DateTime.Today ? "Vade Bekliyor" : (od ? "Ödendi" : "Tahsil Edildi")) : "—" };
                    if (s.Vadeli) h.Vade = s.Tarih;
                    if (avans > 0.005m) { h.Kalan = avans; if (!s.Vadeli) h.KapamaDurumu = "Açık"; }   // dağıtılmayan kısım kapama bekleyen tahsilat / ödeme (Otomatik Kapatma)
                    MutabakatOrnek.HareketEkle(t.CariKodu, h, kapat); s.HareketNo = h.BelgeNo;
                }
                t.Durum = "İşlendi"; t.IslemeAlan = kullanici ?? ""; t.IslemeZamani = z;
                CariKartOrnek.Gunluk(k, z, kullanici, od ? "Ödeme" : "Tahsilat", "Taslak", $"{t.No} işlendi · {t.Tutar.ToString("N2", Tr)} {t.ParaBirimi} ({t.Yontem}, {t.IslemTipi}) · {dag.Count} fatura eşleşmesi{(t.Avans > 0.005m ? $" · avans {t.Avans.ToString("N2", Tr)} {t.ParaBirimi}" : "")}{(t.VadeFarki > 0 ? $" · vade farkı {t.VadeFarki.ToString("N0", Tr)} ₺" : "")}{(t.KurFarki != 0 ? $" · kur farkı {t.KurFarki.ToString("N0", Tr)} ₺" : "")}", od ? "Ödeme" : "Tahsilat", t.Aciklama);
            }
            if (od) NakitAkisaYaz(t, k, kullanici);   // kilit dışında: NakitAkisOrnek kendi kilidini kullanır
            return null;
        }
        /// <summary>
        /// İşlenen ÖDEMENİN her satırını şirketin Nakit Akış Planlama'ya ÖDENDİ satır olarak yazar (Takip Paneli'nden "Ödeme Ekle" ile aynı yol): yöntem, belge / dekont no, banka hesabı, kapatılan fatura, kur;
        /// nakit satırda makbuz; çek / senette planlanan tarih = vade. Satıra DocEntry yazılır (çekmecede bağlantı). Hata nakit akışı bozmasın diye satır satır yutulur.
        /// </summary>
        private static void NakitAkisaYaz(TahsilatKaydi t, CariKart k, string kullanici)
        {
            string db; try { db = CekAkibetOrnek.DbName(k.Sirket); } catch { return; } if (string.IsNullOrEmpty(db)) return;
            foreach (var s in t.Satirlar)
            {
                if (s.NakitAkisDocEntry != null) continue;
                try
                {
                    var kapat = t.Dagitim.Where(d => d.OdemeSira == s.Sira).Select(d => d.BelgeNo).Distinct().ToList();
                    string yontem = s.Yontem == "Kredi Kartı" ? "Kredi Kartı / Banka Kartı" : s.Yontem; string pb = TlMi(s.Pb) ? "TRY" : s.Pb;
                    var m = NakitAkisOrnek.Ekle(db, new NakitAkisViewModel
                    {
                        U_Kurum = k.KisaAd, U_UrunHizmet = kapat.Count > 0 ? $"Alış faturası ödemesi · {string.Join(", ", kapat)}" : "Cari bakiye ödemesi", U_OdemeYontemi = yontem, U_BagliBelgeTipi = kapat.Count > 0 ? "Fatura" : "Cari Bakiye", U_FaturaSiparisNo = kapat.FirstOrDefault() ?? "",
                        U_PlanlananTarih = s.Tarih, U_SonOdemeTarihi = s.Tarih, U_Tutar = s.Tutar, U_ParaBirimi = pb, U_Kur = s.Kur, U_TutarTL = s.TutarTl, U_IslemTipi = t.IslemTipi, U_TalepEden = kullanici ?? "", U_Kasa = yontem == "Nakit" ? NakitAkisOrnek.Kasalar[0] : "",
                        U_Kaynak = "Gerçekleşen Ödemeler", U_Notlar = $"Yapılan Ödemeler › Yeni Ödeme {t.No}/{s.Sira} · {(t.Mod == "Manuel" ? "manuel fatura seçimi" : "otomatik (FIFO)")}{(s.Vadeli ? $" · {s.Yontem} vade {s.Tarih:dd.MM.yyyy}" : "")}{(string.IsNullOrEmpty(s.Not) ? "" : " · " + s.Not)}", U_Satinalmaci = "", U_FaturaSahibi = k.Sirket, U_OdeyenSirket = k.Sirket
                    }, kullanici);
                    if (!string.IsNullOrEmpty(s.BelgeNo)) NakitAkisOrnek.AlanKaydet(db, m.DocEntry, yontem == "Nakit" ? "U_MakbuzNo" : "U_DekontNo", s.BelgeNo, kullanici);
                    if (yontem == "Havale / EFT" && !string.IsNullOrEmpty(s.Banka)) { var bh = NakitAkisOrnek.BankaHesaplari.FirstOrDefault(x => x.StartsWith(s.Banka, StringComparison.OrdinalIgnoreCase)); if (bh != null) NakitAkisOrnek.AlanKaydet(db, m.DocEntry, "U_BankaHesabi", bh, kullanici); }
                    NakitAkisOrnek.OdendiIsaretle(db, m.DocEntry, $"Yapılan Ödemeler · {t.No}", kullanici, true, s.Vadeli ? t.Tarih : s.Tarih);
                    lock (Kilit) { s.NakitAkisDocEntry = m.DocEntry; s.NakitAkisDb = db; }
                }
                catch { /* nakit akış yazılamadıysa ödeme kaydı yine de işlenmiştir */ }
            }
        }
        /// <summary>Taslak kaydı siler (işlenmiş kayıt silinemez).</summary>
        public static string Sil(string no, string kullanici)
        {
            var t = TahsilatBul(no); if (t == null) return "Kayıt bulunamadı."; if (t.Islendi) return $"İşlenmiş {(t.Odeme ? "ödeme" : "tahsilat")} silinemez.";
            lock (Kilit) _tahsilatlar.Remove(t); var k = CariKartOrnek.KartBul(t.CariKodu); if (k != null) CariKartOrnek.Gunluk(k, DateTime.Now, kullanici, t.Odeme ? "Ödeme Taslağı Silindi" : "Tahsilat Taslağı Silindi", t.No, "", t.Odeme ? "Ödeme" : "Tahsilat");
            return null;
        }
        /// <summary>Kur farkı / vade farkı faturası keser (KDV'li, 30 gün vadeli) ve cari hesabına Fatura olarak işler. Hata metni ya da null.</summary>
        public static string FaturaKes(string tahsilatNo, string tur, string kullanici, out FarkFaturasi fatura, DateTime? simdi = null)
        {
            fatura = null; var t = TahsilatBul(tahsilatNo); if (t == null) return "Tahsilat bulunamadı."; if (t.Odeme) return "Fark faturası yalnız müşteri tahsilatında kesilir (ödemede tedarikçi keser)."; if (!t.Islendi) return "Fark faturası yalnız işlenmiş tahsilatta kesilir; önce İşleme Al.";
            bool kur = tur == "Kur Farkı"; if (!kur && tur != "Vade Farkı") return "Fatura türü Kur Farkı ya da Vade Farkı olmalı.";
            if (kur && !string.IsNullOrEmpty(t.KurFarkiFaturaNo)) return $"Kur farkı faturası zaten kesildi: {t.KurFarkiFaturaNo}."; if (!kur && !string.IsNullOrEmpty(t.VadeFarkiFaturaNo)) return $"Vade farkı faturası zaten kesildi: {t.VadeFarkiFaturaNo}.";
            var kalemler = t.Dagitim.Where(d => (kur ? d.KurFarki : d.VadeFarki) > 0.005m).Select(d => (kur ? $"{d.BelgeNo} kur farkı: {d.Tutar.ToString("N2", Tr)} {d.Pb} × (ödeme kuru {d.TahsilatKuru.ToString("N4", Tr)} − baz {(d.BazKur > 0 ? d.BazKur : d.FaturaKuru).ToString("N4", Tr)} · {d.BazKaynak ?? "Fatura Kuru"})" : $"{d.BelgeNo} vade farkı: {d.VfGun} gün{(d.VfGun < d.GecikmeGun ? $" (toplam gecikme {d.GecikmeGun} gün; öncesi kesildi)" : " gecikme")} × %{Oran(t.CariKodu).ToString("0.##", Tr)} / ay ({d.OdemeYontem} {d.OdemeTarihi:dd.MM.yyyy})", kur ? d.KurFarki : d.VadeFarki)).ToList();
            if (kalemler.Count == 0) return kur ? "Bu tahsilatta pozitif kur farkı yok." : "Bu tahsilatta vade farkı yok (gecikme yok).";
            var k = CariKartOrnek.KartBul(t.CariKodu);
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var no = kur ? $"KFF-{z:yyyy}-{++_kff:0000}" : $"VFF-{z:yyyy}-{++_vff:0000}";
                fatura = new FarkFaturasi { No = no, Tarih = z.Date, Vade = z.Date.AddDays(30), CariKodu = t.CariKodu, CariAd = t.CariAd, CariUnvan = k?.ResmiUnvan ?? t.CariAd, VergiNo = k?.VergiNo ?? "", VergiDairesi = k?.VergiDairesi ?? "", Tur = tur, TahsilatNo = t.No, IslemTipi = t.IslemTipi, Kalemler = kalemler, KdvOran = KdvOran, Kullanici = kullanici ?? "", Zaman = z };
                _faturalar.Insert(0, fatura); if (kur) t.KurFarkiFaturaNo = no; else t.VadeFarkiFaturaNo = no;
                MutabakatOrnek.HareketEkle(t.CariKodu, new MtHareket { Tarih = z.Date, ParaBirimi = "TL", BelgeTipi = "Fatura", BelgeNo = no, Aciklama = $"{tur} faturası · {t.No}", Borc = fatura.Toplam, Vade = fatura.Vade, KapamaDurumu = "Açık", Kalan = fatura.Toplam, IslemTipi = t.IslemTipi });
                if (k != null) CariKartOrnek.Gunluk(k, z, kullanici, tur + " Faturası", "", $"{no} · {fatura.Toplam.ToString("N2", Tr)} ₺ (KDV dahil) · tahsilat {t.No}", "Tahsilat");
                return null;
            }
        }
        // =====================================================================
        // MÜŞTERİ BAZINDA VADE FARKI — AY SONU OTOMATİK + ELLE (kullanıcı 16 Kas 2026: "her ay sonu vadesi geçen bakiyelere otomatik vade farkı faturası kesilmeli")
        //   • Her ay sonu, o gün vadesi geçmiş açık satış faturalarına müşteri başına TEK vade farkı faturası kesilir (Kaynak = "Ay Sonu", Donem = yyyy-MM):
        //     kalan × kur × aylık oran / 30 × gün; gün = ay sonu − (vade | o faturaya son kesim tarihi | önceki ay sonu, hangisi sonraysa) → her ay yalnız o ayın gecikme günleri.
        //   • Kesilen her fatura için fatura bazında "son kesim tarihi" tutulur (_vfSon): sonraki ay sonu ve tahsilat anındaki vade farkı o tarihten başlar (aynı gün iki kez faturalanmaz).
        //   • Vade farkı / kur farkı faturalarının (VFF- / KFF-) kendisine vade farkı işlemez (faize faiz yok).
        //   • Otomatik çalışma: AySonuOtomatik() — son tamamlanan ay sonuna kadar kesilmemiş her ay sonu için bir kez (rapor verisi okunurken çağrılır; ay döndüğünde ilk açılışta keser).
        //   • Elle: MusteriVadeFarkiKes(kod, bugün, "Elle") — ay sonunu beklemeden bugüne kadar olan günler; ay sonu kesimi kalan günlerden devam eder.
        // =====================================================================
        public class VfKalem { public string BelgeNo { get; set; } public DateTime Vade { get; set; } public string Pb { get; set; } public decimal Kalan { get; set; } public decimal KalanTl { get; set; } public DateTime Bas { get; set; } public DateTime Bit { get; set; } public int Gun { get; set; } public int ToplamGecikme { get; set; } public decimal Tutar { get; set; } public string IslemTipi { get; set; } public decimal Mahsup { get; set; } /* avans mahsubu (virman sayılan tutar) */ }
        private static readonly Dictionary<string, DateTime> _vfSon = new Dictionary<string, DateTime>();   // "cari|belge" → o faturaya vade farkı kesilen son gün
        private static DateTime? _vfSonAySonu;                                                             // otomatik kesimi yapılmış son ay sonu
        public static DateTime? SonOtomatikAySonu { get { Tohumla(); lock (Kilit) return _vfSonAySonu; } }
        public static DateTime AySonu(DateTime t) => new DateTime(t.Year, t.Month, DateTime.DaysInMonth(t.Year, t.Month));
        /// <summary>Vade farkı / kur farkı faturası mı (bunlara vade farkı işlemez).</summary>
        public static bool VfDisi(MtHareket f) => (f?.BelgeNo ?? "").StartsWith("VFF-") || (f?.BelgeNo ?? "").StartsWith("KFF-");
        /// <summary>Faturada vade farkının başlayacağı gün: vade ya da (daha sonraysa) bu faturaya en son vade farkı kesilen tarih.</summary>
        public static DateTime VfBaslangic(string kod, MtHareket f) { var vade = (f.Vade ?? f.Tarih).Date; lock (Kilit) return _vfSon.TryGetValue(kod + "|" + f.BelgeNo, out var s) && s > vade ? s : vade; }
        /// <summary>
        /// Müşterinin "tarih" günü itibarıyla vade farkı doğan gecikmiş faturaları (kesilmemiş günler). Geçmiş bir tarih için (ay sonu yakalama) faturanın o günkü kalanı,
        /// o günden SONRA faturayı kapatan tahsilat varsa (ve öncesinde yoksa) fatura tutarı olarak alınır; aksi halde bugünkü kalan.
        /// </summary>
        public static List<VfKalem> MusteriVfKalemleri(string kod, DateTime tarih)
        {
            var l = new List<VfKalem>(); var c = MutabakatOrnek.Bul(kod); if (c == null) return l; tarih = tarih.Date; bool gecmis = tarih < DateTime.Today;
            var oncekiAySonu = new DateTime(tarih.Year, tarih.Month, 1).AddDays(-1); var kurGunu = tarih > DateTime.Today ? DateTime.Today : tarih;
            List<MtHareket> hareketler; lock (MutabakatOrnek.Kilit) hareketler = c.Hareketler.ToList();
            // 19 Kas 2026 (kullanıcı: "ay sonu vade farkı kesilirken avans bakiyesine dikkat; avansı olan carinin virmanı yapılıp borcundan düşülmeli, sonra vade farkı hesaplanmalı"):
            //   kapatılmamış ödeme / avans, aynı para birimindeki açık faturalara (birebir + FIFO) mahsup edilmiş sayılır; vade farkı kalan üzerinden. Gerçek kesimde (MusteriVadeFarkiKes) virman önce fiilen yapılır.
            var mahsup = new Dictionary<string, decimal>(); try { List<KapamaCifti> on; lock (MutabakatOrnek.Kilit) on = KapamaOrnek.Oner(c); foreach (var ct in on) mahsup[ct.FaturaNo] = (mahsup.TryGetValue(ct.FaturaNo, out var m0) ? m0 : 0) + ct.Tutar; } catch { }
            foreach (var f in hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && !VfDisi(h) && h.Tarih.Date <= tarih))
            {
                var vade = (f.Vade ?? f.Tarih).Date; if (vade >= tarih) continue; decimal kalan = f.Kalan;
                if (gecmis) { var od = hareketler.Where(h => h.BelgeTipi != "Fatura" && h.Alacak > 0 && (h.Aciklama ?? "").Contains(f.BelgeNo)).ToList(); if (od.Any(h => h.Tarih.Date > tarih) && !od.Any(h => h.Tarih.Date <= tarih)) kalan = f.Borc; }
                decimal mh = 0; if (mahsup.TryGetValue(f.BelgeNo, out var mTut) && mTut > 0) { mh = Math.Min(kalan, mTut); kalan = Math.Round(kalan - mh, 2); }
                if (kalan <= 0.005m) continue;
                // başlangıç: bu faturaya daha önce vade farkı kesildiyse son kesim tarihi (robot ay içinde çalıştıysa kalan günler sonraki aya devreder); hiç kesilmediyse vade, o da önceki ay sonundan eskiyse önceki ay sonu
                bool kesilmis; lock (Kilit) kesilmis = _vfSon.ContainsKey(kod + "|" + f.BelgeNo);
                var bas = VfBaslangic(kod, f); bool vadeden = bas == vade; if (!kesilmis && oncekiAySonu > bas) { bas = oncekiAySonu; vadeden = false; }
                int gun = (tarih - bas).Days - (vadeden ? VadeToleransGun : 0); if (gun <= 0) continue;
                decimal kur = TlMi(f.ParaBirimi) ? 1m : KurOto(f.ParaBirimi, kurGunu), tutar = Math.Round(kalan * kur * Oran(kod) / 100m / 30m * gun, 2); if (tutar <= 0.005m) continue;
                l.Add(new VfKalem { BelgeNo = f.BelgeNo, Vade = vade, Pb = f.ParaBirimi, Kalan = kalan, KalanTl = Math.Round(kalan * kur, 2), Bas = bas, Bit = tarih, Gun = gun, ToplamGecikme = (tarih - vade).Days, Tutar = tutar, IslemTipi = f.IslemTipi, Mahsup = mh });
            }
            return l.OrderBy(x => x.Vade).ToList();
        }
        /// <summary>Müşteri bazında TEK vade farkı faturası ("tarih" itibarıyla kesilmemiş gecikme günleri): cari hesabına Fatura olarak işlenir, faturaların son kesim tarihi ilerler. Hata metni ya da null.</summary>
        // 19 Kas 2026 gece (kullanıcı: "yurtdışı tahsilatlarında düzenli vade farkı kesilmiyor, tahsilat dövizli olduğu için; belki sadece tahsilat işlenirken kesilebilir"):
        //   YURTDIŞI müşteride (cari kart Bölge = Yurtdışı) ay sonu / robot / elle vade farkı kesimi YAPILMAZ; vade farkı yalnız tahsilat işlenirken (FaturaKes, Kaynak "Tahsilat") kesilir.
        public const string YurtdisiVfKural = "Yurtdışı müşteride vade farkı yalnız tahsilat işlenirken kesilir (dövizli tahsilat; ay sonu, robot ve elle kesim yok).";
        public static bool AySonuKesilir(CariKart k) => k != null && k.Bolge != "Yurtdışı";
        public static string VfKural(CariKart k) => AySonuKesilir(k) ? "Ay Sonu" : "Tahsilatta";
        public static string MusteriVadeFarkiKes(string kod, DateTime tarih, string kaynak, string kullanici, out FarkFaturasi fatura, DateTime? simdi = null)
        {
            fatura = null; var k = CariKartOrnek.KartBul(kod); if (k == null || k.KartTipi != "Müşteri") return "Müşteri bulunamadı."; tarih = tarih.Date;
            if (!AySonuKesilir(k)) return $"{k.KisaAd}: {YurtdisiVfKural}";
            // önce VİRMAN (19 Kas 2026): kapatılmamış ödeme / avans aynı para birimindeki açık faturalara fiilen kapatılır; vade farkı kalan borç üzerinden hesaplanır
            string virmanNot = ""; var cV = MutabakatOrnek.Bul(kod);
            if (cV != null) { List<KapamaCifti> ciftler; lock (MutabakatOrnek.Kilit) ciftler = KapamaOrnek.Oner(cV); if (ciftler.Count > 0) { var hv = KapamaOrnek.Kapat(kod, ciftler, "Virman · vade farkı öncesi", kullanici, out var kv, simdi); if (hv == null && kv != null) virmanNot = $" · önce virman: {kv.Ciftler.Count} eşleşme, {kv.Ciftler.GroupBy(x => x.Pb).Select(g => g.Sum(x => x.Tutar).ToString("N2", Tr) + " " + g.Key).Aggregate((a, b) => a + " + " + b)} avans borçtan düşüldü"; } }
            var kalemler = MusteriVfKalemleri(kod, tarih); if (kalemler.Count == 0) return $"{k.KisaAd}: {tarih:dd.MM.yyyy} itibarıyla vade farkı doğan gecikmiş fatura yok (ya da günleri kesilmiş){(virmanNot == "" ? "" : "; " + virmanNot.TrimStart(' ', '·', ' '))}.";
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var no = $"VFF-{tarih:yyyy}-{++_vff:0000}"; string oranTr = Oran(kod).ToString("0.##", Tr);
                fatura = new FarkFaturasi { No = no, Tarih = tarih, Vade = tarih.AddDays(30), CariKodu = k.Kod, CariAd = k.KisaAd, CariUnvan = k.ResmiUnvan ?? k.KisaAd, VergiNo = k.VergiNo ?? "", VergiDairesi = k.VergiDairesi ?? "", Tur = "Vade Farkı", TahsilatNo = "", Kaynak = kaynak, Donem = tarih.ToString("yyyy-MM"),
                    IslemTipi = kalemler.All(x => x.IslemTipi == "R") ? "R" : "X", KdvOran = KdvOran, Kullanici = kullanici ?? "", Zaman = z,
                    Kalemler = kalemler.Select(x => ($"{x.BelgeNo} vade farkı: {x.Gun} gün ({x.Bas:dd.MM}–{x.Bit:dd.MM.yyyy}) × {x.Kalan.ToString("N2", Tr)} {x.Pb}{(x.Mahsup > 0 ? $" (avans mahsubu −{x.Mahsup.ToString("N2", Tr)} sonrası)" : "")} × %{oranTr} / ay", x.Tutar)).ToList() };
                _faturalar.Insert(0, fatura); foreach (var x in kalemler) _vfSon[kod + "|" + x.BelgeNo] = tarih;
                MutabakatOrnek.HareketEkle(kod, new MtHareket { Tarih = tarih, ParaBirimi = "TL", BelgeTipi = "Fatura", BelgeNo = no, Aciklama = $"Vade farkı faturası · {kaynak} · {tarih.ToString("MMMM yyyy", Tr)} · {kalemler.Count} gecikmiş fatura", Borc = fatura.Toplam, Vade = fatura.Vade, KapamaDurumu = "Açık", Kalan = fatura.Toplam, IslemTipi = fatura.IslemTipi });
                CariKartOrnek.Gunluk(k, z, kullanici, "Vade Farkı Faturası", "", $"{no} · {fatura.Toplam.ToString("N2", Tr)} ₺ (KDV dahil) · {kaynak} {tarih:dd.MM.yyyy} · {kalemler.Count} gecikmiş fatura{virmanNot}", "Tahsilat");
            }
            return null;
        }
        /// <summary>Bir ay sonu için bütün müşterilere ay sonu vade farkı faturası (o dönem "Ay Sonu" faturası olan müşteri atlanır). Kesilen fatura sayısı.</summary>
        public static int AySonuKes(DateTime aySonu, string kullanici, DateTime? simdi = null)
        {
            int n = 0; string donem = aySonu.ToString("yyyy-MM");
            foreach (var k in CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Müşteri" && AySonuKesilir(x)).OrderBy(x => x.Kod).ToList())   // yurtdışı müşteri ay sonu kesiminin dışında (vade farkı tahsilatta)
            {
                bool varMi; lock (Kilit) varMi = _faturalar.Any(f => f.CariKodu == k.Kod && f.Kaynak == "Ay Sonu" && f.Donem == donem); if (varMi) continue;
                // robot dışı bırakılan müşteri (Tahsilat › Aylık Süreç): o ay fatura kesilmez; o ayın gecikme günleri sonraki aya da taşınmaz (son kesim tarihi ay sonuna ilerler)
                var haric = TahsilatTakipKayit.HaricMi(k.Kod, donem);
                if (haric != null)
                {
                    var atlanan = MusteriVfKalemleri(k.Kod, aySonu); if (atlanan.Count == 0) continue;
                    lock (Kilit) foreach (var x in atlanan) _vfSon[k.Kod + "|" + x.BelgeNo] = aySonu;
                    CariKartOrnek.Gunluk(k, simdi ?? DateTime.Now, kullanici, "Vade Farkı Robotu", "", $"{aySonu.ToString("MMMM yyyy", Tr)} robot dışı: {atlanan.Sum(x => x.Tutar).ToString("N2", Tr)} ₺ vade farkı kesilmedi", "Tahsilat", haric.Neden); continue;
                }
                if (MusteriVadeFarkiKes(k.Kod, aySonu, "Ay Sonu", kullanici, out _, simdi) == null) n++;
            }
            return n;
        }
        /// <summary>
        /// VADE FARKI ROBOTU (kullanıcı 17 Kas 2026: "ayın son haftası ödeme yapmayanlara otomatik vade farkı robotu çalıştırılsın, seçilen 1 – 2 müşteri dışında"): bugün itibarıyla vadesi geçmiş açık faturası olan
        /// (ödeme yapmamış) her müşteriye o ayın vade farkı faturası kesilir (Kaynak = "Ay Sonu", Donem = bu ay). Robot dışı işaretlenenler ve bu ay faturası kesilmiş olanlar atlanır.
        /// Robot ay içinde çalıştırılmazsa ay döndüğünde AySonuOtomatik aynı işi ay sonu tarihiyle yapar; robotun çalıştığı günden ay sonuna kalan günler sonraki ayın faturasına girer.
        /// </summary>
        public static (List<FarkFaturasi> Kesilen, List<string> Haric, int ZatenKesilmis) RobotCalistir(string kullanici)
        {
            Tohumla(); var bugun = DateTime.Today; string donem = bugun.ToString("yyyy-MM"); var kesilen = new List<FarkFaturasi>(); var haricler = new List<string>(); int zaten = 0;
            foreach (var k in CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Müşteri" && AySonuKesilir(x)).OrderBy(x => x.Kod).ToList())   // yurtdışı müşteri robotun dışında (vade farkı tahsilatta)
            {
                bool varMi; lock (Kilit) varMi = _faturalar.Any(f => f.CariKodu == k.Kod && f.Kaynak == "Ay Sonu" && f.Donem == donem); if (varMi) { zaten++; continue; }
                if (MusteriVfKalemleri(k.Kod, bugun).Count == 0) continue;
                if (TahsilatTakipKayit.HaricMi(k.Kod, donem) != null) { haricler.Add(k.KisaAd); continue; }
                if (MusteriVadeFarkiKes(k.Kod, bugun, "Ay Sonu", kullanici, out var f2) == null) kesilen.Add(f2);
            }
            return (kesilen, haricler, zaten);
        }
        /// <summary>Müşteriye bu dönem robot (ay sonu) faturası kesildi mi; kesildiyse fatura no.</summary>
        public static string RobotFaturasi(string kod, string donem) { Tohumla(); lock (Kilit) return _faturalar.FirstOrDefault(f => f.CariKodu == kod && f.Kaynak == "Ay Sonu" && f.Donem == donem)?.No ?? ""; }
        /// <summary>OTOMATİK ay sonu kesimi: son tamamlanan ay sonuna kadar kesilmemiş her ay sonu için bir kez çalışır (rapor verisi okunurken çağrılır). Kesilen fatura sayısı.</summary>
        public static int AySonuOtomatik()
        {
            Tohumla(); var bugun = DateTime.Today; var hedef = new DateTime(bugun.Year, bugun.Month, 1).AddDays(-1); DateTime? son; lock (Kilit) son = _vfSonAySonu; if (son >= hedef) return 0;
            int n = 0; var t = son.HasValue ? AySonu(son.Value.AddDays(1)) : hedef;
            while (t <= hedef) { n += AySonuKes(t, "Sistem (Ay Sonu)", t.AddHours(23).AddMinutes(55)); lock (Kilit) _vfSonAySonu = t; t = AySonu(t.AddDays(1)); }
            return n;
        }
        /// <summary>Vade farkı / kur farkı faturasının cari hesabındaki kalanı (ödenmemiş kısım).</summary>
        public static decimal FaturaKalan(FarkFaturasi f) { var c = MutabakatOrnek.Bul(f.CariKodu); if (c == null) return 0; lock (MutabakatOrnek.Kilit) return c.Hareketler.FirstOrDefault(h => h.BelgeTipi == "Fatura" && h.BelgeNo == f.No)?.Kalan ?? 0; }

        /// <summary>Örnek kayıtlar: son üç ay sonunun otomatik vade farkı faturaları (bütün cari sınıflarından), otomatik TL havale (işlenmiş), fatura bazlı USD tahsilat (kur farkı), gecikmeli çek + nakit toplu tahsilat (vade farkı faturası kesilmiş), bekleyen taslak.</summary>
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var b = DateTime.Today;
                // --- AY SONU VADE FARKI geçmişi (kullanıcı 16 Kas 2026): son üç ay sonu "Sistem (Ay Sonu)" tarafından kesilmiş sayılır. Tahsilat örneklerinden ÖNCE çalışır (ay sonu kesimi kasım tahsilatlarından öncedir).
                //     Eski dönem (son ay sonundan önceki) faturaları ödeyen müşterilerde vadesinden 5 gün önce tahsil edilmiş görünür; Özel Takip sınıfı ve birkaç müşteri ödememiştir (rapordaki "ödeme yok" örnekleri).
                var sonAy = new DateTime(b.Year, b.Month, 1).AddDays(-1);
                KurDegerlemeOrnek.Tohumla();   // 19 Kas 2026: muhasebenin dönem sonu kur değerlemeleri (R dövizli açık faturalar) vade farkı kesiminden önce
                foreach (var ays in new[] { AySonu(sonAy.AddMonths(-2)), AySonu(sonAy.AddMonths(-1)), sonAy }) AySonuKes(ays, "Sistem (Ay Sonu)", ays.AddHours(23).AddMinutes(55));
                _vfSonAySonu = sonAy;
                var odemeyenler = new HashSet<string> { "120.007", "120.008", "120.014" }; int vfOd = 0;
                foreach (var ff in _faturalar.Where(x => x.Kaynak == "Ay Sonu" && x.Tarih < sonAy).OrderBy(x => x.Tarih).ThenBy(x => x.No).ToList())
                {
                    var kk = CariKartOrnek.KartBul(ff.CariKodu); var ot = ff.Vade.AddDays(-5); if (kk == null || kk.Sinif == "Özel Takip" || odemeyenler.Contains(ff.CariKodu) || ot > b) continue;
                    MutabakatOrnek.HareketEkle(ff.CariKodu, new MtHareket { Tarih = ot, ParaBirimi = "TL", BelgeTipi = "Tahsilat", BelgeNo = $"TH-{ot:yyyy}-VF{++vfOd:000}", Aciklama = "Havale / EFT · " + ff.No, Alacak = ff.Toplam, KapamaDurumu = "—", IslemTipi = ff.IslemTipi }, new[] { (ff.No, ff.Toplam) });
                }
                TahsilatSatir S(string y, DateTime t, decimal tutar, string pb = "TL", string belge = "", string banka = "") => new TahsilatSatir { Yontem = y, Tarih = t, Tutar = tutar, Pb = pb, BelgeNo = belge, Banka = banka };
                Kaydet("120.003", b.AddDays(-6), "X", "TL", "Otomatik", null, new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-6), 850_000m, "TL", "", "Ziraat Bankası") }, "Haftalık ödeme planı", "Ahmet Özkan", true, out _, b.AddDays(-6).AddHours(14));
                var usd = AcikFaturalar("120.001").Where(f => f.ParaBirimi == "USD").Take(2).ToList();
                if (usd.Count > 0) Kaydet("120.001", b.AddDays(-3), "X", "USD", "Manuel", usd.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => 0m), new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-3), Math.Round(usd.Sum(f => f.Kalan), 2), "USD", "", "Garanti BBVA") }, "USD faturalar kapatıldı", "Melike Ural Kaya", true, out _, b.AddDays(-3).AddHours(11));
                var gec = AcikFaturalar("120.011").Where(f => f.ParaBirimi == "TL" && !VfDisi(f) && (f.Vade ?? f.Tarih) < b.AddDays(-30)).Take(2).ToList();
                if (gec.Count > 0) { decimal top = Math.Round(gec.Sum(f => f.Kalan), 2); Kaydet("120.011", b.AddDays(-1), "R", "TL", "Manuel", gec.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => 0m), new List<TahsilatSatir> { S("Nakit", b.AddDays(-1), Math.Round(top * 0.3m, 2)), S("Çek", b.AddDays(44), Math.Round(top * 0.7m, 2), "TL", "ÇK-4471920", "İş Bankası") }, "Gecikmiş faturalar nakit + çekle tahsil", "Berk Berberoğlu", true, out var k3, b.AddDays(-1).AddHours(16)); if (k3 != null && k3.VadeFarki > 0) FaturaKes(k3.No, "Vade Farkı", "Berk Berberoğlu", out _, b.AddDays(-1).AddHours(16).AddMinutes(20)); }
                var tas = AcikFaturalar("120.002").Where(f => f.ParaBirimi == "TL").Take(3).ToList();
                if (tas.Count > 0) Kaydet("120.002", b, "X", "TL", "Otomatik", null, new List<TahsilatSatir> { S("Çek", b.AddDays(30), Math.Round(tas.Sum(f => f.Kalan) * 0.5m, 2), "TL", "ÇK-5580113", "Yapı Kredi"), S("Çek", b.AddDays(60), Math.Round(tas.Sum(f => f.Kalan) * 0.5m, 2), "TL", "ÇK-5580114", "Yapı Kredi") }, "Müşteriden gelen iki vadeli çek; teslim alınınca işlenecek", "Berk Berberoğlu", false, out _, b.AddHours(9));
                // --- ÖDEME örnekleri (Yapılan Ödemeler › Yeni Ödeme): havaleyle fatura bazlı ödeme (işlenmiş), nakit + vadeli çek toplu ödeme (işlenmiş), teslim bekleyen çek taslağı
                var tedler = CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Tedarikçi").Select(x => x.Kod).Where(kod => AcikFaturalar(kod).Any(f => f.ParaBirimi == "TL")).Take(3).ToList();
                if (tedler.Count > 0) { var f1 = AcikFaturalar(tedler[0]).Where(f => f.ParaBirimi == "TL").Take(2).ToList(); Kaydet(tedler[0], b.AddDays(-5), "X", "TL", "Manuel", f1.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => 0m), new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-5), Math.Round(f1.Sum(f => f.Kalan), 2), "TL", "DK-5520341", "Ziraat Bankası") }, "Vadesi gelen alış faturaları havaleyle ödendi", "Serkan Genç", true, out _, b.AddDays(-5).AddHours(15)); }
                if (tedler.Count > 1) { decimal top = Math.Round(AcikFaturalar(tedler[1]).Where(f => f.ParaBirimi == "TL").Sum(f => f.Kalan), 2); if (top > 0) Kaydet(tedler[1], b.AddDays(-2), "X", "TL", "Otomatik", null, new List<TahsilatSatir> { S("Nakit", b.AddDays(-2), Math.Round(top * 0.25m, 2)), S("Çek", b.AddDays(58), Math.Round(top * 0.75m, 2), "TL", "ÇK-7731204", "Garanti BBVA") }, "Bir kısmı nakit, kalanı 60 gün vadeli çekle", "Serkan Genç", true, out _, b.AddDays(-2).AddHours(11)); }
                if (tedler.Count > 2) { var f3 = AcikFaturalar(tedler[2]).Where(f => f.ParaBirimi == "TL").Take(2).ToList(); if (f3.Count > 0) Kaydet(tedler[2], b, "X", "TL", "Manuel", f3.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => 0m), new List<TahsilatSatir> { S("Çek", b.AddDays(45), Math.Round(f3.Sum(f => f.Kalan), 2), "TL", "ÇK-7731205", "Garanti BBVA") }, "Çek hazırlandı; tedarikçiye teslim edilince işlenecek", "Serkan Genç", false, out _, b.AddHours(10)); }
                // --- VADE FARKI örnekleri (Tahsilat › Vade Farkı sekmesi; kullanıcı 16 Kas 2026: "Vade Farkı sekmesine örnekler ver"): gecikmiş TL faturaların İŞLENMİŞ tahsilatı, faturası KESİLMEMİŞ.
                //     Sona eklenir (önceki TH / ÖD numaraları kaymasın); yalnız yurtiçi TL cariler (yurtdışı cari bakiyeleri İhracat Finansal Takibi karşılaştırmasında kullanılıyor).
                MtHareket Gecikmis(string kod) => AcikFaturalar(kod).Where(f => f.ParaBirimi == "TL" && f.BelgeTipi == "Fatura" && !VfDisi(f) && (f.Vade ?? f.Tarih) < b.AddDays(-12)).OrderBy(f => f.Vade ?? f.Tarih).FirstOrDefault();
                // 23 Kas 2026 (kullanıcı: "Tahsilatı yapan kişiler Melike Ural Kaya, Aksel Güler, Ahmet Özkan, Berk Berberoğlu olabilir"): örnek tahsilatları giren kişi artık bu dört gerçek kişi (eski uydurma ad kalktı);
                //     dağılım: nakit / çek → Berk Berberoğlu (kasa sorumlusu), yurtiçi havale → Ahmet Özkan · Melike Ural Kaya, yurtdışı döviz havalesi → ağırlıkla Aksel Güler. Yeni girilen tahsilatta kişi = oturum açan kullanıcı.
                void Vf(string kod, int gunOnce, int saat, string aciklama, string kisi, Func<MtHareket, (decimal Sinir, List<TahsilatSatir> Satirlar)> kur)
                {
                    var f = Gecikmis(kod); if (f == null) return; var p = kur(f); if (p.Satirlar.Sum(s => s.Tutar) <= 0) return;
                    Kaydet(kod, b.AddDays(-gunOnce), IslemTipleri.Contains(f.IslemTipi ?? "") ? f.IslemTipi : "X", "TL", "Manuel", new Dictionary<string, decimal> { [f.BelgeNo] = p.Sinir }, p.Satirlar, aciklama, kisi, true, out _, b.AddDays(-gunOnce).AddHours(saat));
                }
                // 1-2) aynı müşteriden iki ayrı tahsilat: önce kısmi havale, sonra kalan nakit (toplu kesimde iki ayrı fatura)
                Vf("120.004", 5, 11, "Gecikmiş faturaya kısmi havale", "Ahmet Özkan", f => { decimal t = Math.Round(Math.Min(f.Kalan, 200_000m), 2); return (t, new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-5), t, "TL", "", "Ziraat Bankası") }); });
                Vf("120.004", 1, 15, "Gecikmiş faturanın kalanı nakit tahsil edildi", "Berk Berberoğlu", f => (0m, new List<TahsilatSatir> { S("Nakit", b.AddDays(-1), f.Kalan) }));
                // 3) tek havaleyle tam kapama
                Vf("120.006", 2, 10, "Gecikmiş fatura havaleyle kapatıldı", "Melike Ural Kaya", f => (0m, new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-2), f.Kalan, "TL", "", "Garanti BBVA") }));
                // 4) havale + ileri vadeli çek: çek kısmında vade farkı çek vadesine kadar işler
                Vf("120.007", 3, 14, "Gecikmiş fatura havale + 30 gün vadeli çekle tahsil", "Melike Ural Kaya", f => { decimal h = Math.Round(f.Kalan * 0.4m, 2); return (0m, new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-3), h, "TL", "", "İş Bankası"), S("Çek", b.AddDays(30), Math.Round(f.Kalan - h, 2), "TL", "ÇK-6614275", "Akbank") }); });
                // --- YURTDIŞI örnekleri (19 Kas 2026 gece; kullanıcı: "İhracat Finansal Takibi tüm sekmelere örnek veri ekle"): yurtdışı müşterilerin DÖVİZLİ havale tahsilatları (bölge Yurtdışı; vade farkı yalnız tahsilatta kesilir).
                //     Gecikmeli ödemede vade farkı faturası tahsilatla kesilmiş (120.008, 120.010) ya da kesilmemiş (120.013 → Gecikmeli Tahsilatlar "Kesilmedi"), zamanında (120.012), kısmi (120.014), taslak (120.009). Kur farkı doğarsa kur farkı faturası.
                MtHareket Yd(string kod, Func<MtHareket, bool> f) => AcikFaturalar(kod).Where(x => x.BelgeTipi == "Fatura" && x.Borc > 0 && !TlMi(x.ParaBirimi) && !VfDisi(x) && f(x)).OrderBy(x => x.Vade ?? x.Tarih).FirstOrDefault();
                void YdKaydet(string kod, int gunOnce, int saat, string banka, string aciklama, string kisi, bool gecikmis, bool vfKes, bool isle = true, decimal pay = 1m)
                {
                    var t = b.AddDays(-gunOnce); var f = Yd(kod, x => gecikmis ? (x.Vade ?? x.Tarih) < t.AddDays(-10) : (x.Vade ?? x.Tarih) >= t) ?? Yd(kod, x => true); if (f == null) return;
                    decimal tutar = Math.Round(f.Kalan * pay, 2); if (tutar <= 0.005m) return;
                    var h = Kaydet(kod, t, IslemTipleri.Contains(f.IslemTipi ?? "") ? f.IslemTipi : "X", f.ParaBirimi, "Manuel", new Dictionary<string, decimal> { [f.BelgeNo] = pay < 1m ? tutar : 0m }, new List<TahsilatSatir> { S("Havale / EFT", t, tutar, f.ParaBirimi, "", banka) }, aciklama, kisi, isle, out var k, t.AddHours(saat));
                    if (h != null || k == null || !isle) return;
                    if (vfKes && k.VadeFarki > 0.005m) FaturaKes(k.No, "Vade Farkı", kisi, out _, t.AddHours(saat).AddMinutes(15));
                    if (k.KurFarki > 0.005m && string.IsNullOrEmpty(k.KurFarkiFaturaNo)) FaturaKes(k.No, "Kur Farkı", kisi, out _, t.AddHours(saat).AddMinutes(20));
                }
                YdKaydet("120.008", 40, 10, "Garanti BBVA", "Export invoice payment · gecikmeli, vade farkı faturası tahsilatla kesildi", "Aksel Güler", true, true);
                YdKaydet("120.010", 25, 11, "Yapı Kredi", "SWIFT havalesi · gecikmeli ödeme, vade farkı kesildi", "Aksel Güler", true, true);
                YdKaydet("120.013", 2, 15, "Garanti BBVA", "USD havale · gecikmeli, vade farkı faturası henüz kesilmedi", "Aksel Güler", true, false);
                YdKaydet("120.012", 6, 9, "İş Bankası", "Vadesinde ödeme (EUR havale)", "Melike Ural Kaya", false, true);
                YdKaydet("120.014", 9, 14, "Akbank", "Kısmi ödeme · faturanın yarısı", "Ahmet Özkan", false, true, true, 0.5m);
                YdKaydet("120.009", 0, 8, "Garanti BBVA", "Bankadan gelen döviz girişi · taslak, işleme alınacak", "Aksel Güler", false, true, false);
            }
        }
    }
}
