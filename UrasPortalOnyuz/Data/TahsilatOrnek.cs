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
//     işlenince cari hesabına BORÇ (Ödeme / Çek / Senet) yazılır, her ödeme satırı şirketin Kasa Nakit Akışı'na ÖDENDİ satır olarak düşer (Data/NakitAkisOrnek.cs) ve Yapılan Ödemeler listesinde görünür.
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
        public int? NakitAkisDocEntry { get; set; } public string NakitAkisDb { get; set; } = "";   // ödemede: işlenince açılan Kasa Nakit Akışı satırı (ÖDENDİ)
        public bool Nakit => Yontem == "Nakit"; public bool Vadeli => Yontem == "Çek" || Yontem == "Senet";
    }
    /// <summary>Fatura ↔ ödeme satırı eşleşmesi (simülasyon / dağıtım).</summary>
    public class TahsilatDagitim
    {
        public string BelgeNo { get; set; } public DateTime FaturaTarihi { get; set; } public DateTime Vade { get; set; } public string Pb { get; set; } public string IslemTipi { get; set; }
        public decimal Kalan { get; set; } public decimal Tutar { get; set; }           // fatura para biriminde
        public decimal FaturaKuru { get; set; } public decimal TahsilatKuru { get; set; }
        public int GecikmeGun { get; set; } public decimal VadeFarki { get; set; } public decimal KurFarki { get; set; }   // gün: ödeme tarihi − vade (negatif = erken); VF yalnız gün > 0; TL
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
        public List<TahsilatDagitim> Dagitim { get; set; } = new List<TahsilatDagitim>();
        public string Durum { get; set; } = "Taslak";   // Taslak | İşlendi
        public string Kullanici { get; set; } = ""; public DateTime Zaman { get; set; } public string IslemeAlan { get; set; } = ""; public DateTime? IslemeZamani { get; set; }
        public string KurFarkiFaturaNo { get; set; } = ""; public string VadeFarkiFaturaNo { get; set; } = "";
        public decimal Tutar => Math.Round(Satirlar.Sum(s => s.Tutar), 2); public decimal TutarTl => Math.Round(Satirlar.Sum(s => s.TutarTl), 2);
        public decimal Kur => Tutar > 0 ? Math.Round(TutarTl / Tutar, 4) : 1;
        public string Yontem => Satirlar.Count == 0 ? "" : string.Join(" + ", Satirlar.GroupBy(s => s.Yontem).Select(g => g.Count() > 1 ? $"{g.Count()} {g.Key}" : g.Key));
        public decimal Dagitilan => Math.Round(Dagitim.Sum(d => d.Tutar), 2); public decimal Avans => Math.Round(Tutar - Dagitilan, 2);
        public decimal KurFarki => Math.Round(Dagitim.Sum(d => d.KurFarki), 2); public decimal VadeFarki => Math.Round(Dagitim.Sum(d => d.VadeFarki), 2);
        public bool Islendi => Durum == "İşlendi";
    }
    public class FarkFaturasi
    {
        public string No { get; set; } public DateTime Tarih { get; set; } public DateTime Vade { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string CariUnvan { get; set; } public string VergiNo { get; set; } public string VergiDairesi { get; set; }
        public string Tur { get; set; }   // Kur Farkı | Vade Farkı
        public string TahsilatNo { get; set; } public string IslemTipi { get; set; } = "X";
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
        public static decimal VadeFarkiAylikOran = 3.5m; public static decimal KdvOran = 20m;
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
        /// <summary>İşlenmiş ödeme satırının Kasa Nakit Akışı bağlantısı (satır çekmecesi).</summary>
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
        public static List<TahsilatDagitim> Hesapla(string kod, string pb, string mod, Dictionary<string, decimal> secim, List<TahsilatSatir> satirlar)
        {
            var l = new List<TahsilatDagitim>(); var faturalar = AcikFaturalar(kod).Where(f => f.ParaBirimi == pb).ToList();
            if (mod == "Manuel") faturalar = faturalar.Where(f => secim != null && secim.ContainsKey(f.BelgeNo)).ToList();
            var odemeler = SatirlariHazirla(satirlar, pb, DateTime.Today).OrderBy(s => s.Tarih).ThenBy(s => s.Nakit ? 0 : s.Vadeli ? 2 : 1).ThenBy(s => s.Sira).Select(s => (S: s, Kalan: s.Tutar)).ToList();
            int pi = 0;
            foreach (var f in faturalar)
            {
                decimal cap = f.Kalan; if (mod == "Manuel" && secim.TryGetValue(f.BelgeNo, out var sinir) && sinir > 0) cap = Math.Min(cap, Math.Round(sinir, 2));
                decimal fKalan = f.Kalan; var vade = (f.Vade ?? f.Tarih).Date; decimal fk = FaturaKuru(f);
                while (cap > 0.005m && pi < odemeler.Count)
                {
                    var o = odemeler[pi]; if (o.Kalan <= 0.005m) { pi++; continue; }
                    decimal al = Math.Round(Math.Min(cap, o.Kalan), 2); cap -= al; fKalan = Math.Round(fKalan - al, 2); odemeler[pi] = (o.S, Math.Round(o.Kalan - al, 2));
                    int gun = (o.S.Tarih - vade).Days; int vfGun = Math.Max(0, gun - VadeToleransGun);
                    l.Add(new TahsilatDagitim { BelgeNo = f.BelgeNo, FaturaTarihi = f.Tarih, Vade = vade, Pb = pb, IslemTipi = f.IslemTipi, Kalan = f.Kalan, Tutar = al, FaturaKuru = fk, TahsilatKuru = o.S.Kur, GecikmeGun = gun, VadeFarki = Math.Round(al * o.S.Kur * VadeFarkiAylikOran / 100m / 30m * vfGun, 2), KurFarki = TlMi(pb) ? 0 : Math.Round(al * (o.S.Kur - fk), 2), OdemeSira = o.S.Sira, OdemeYontem = o.S.Yontem, OdemeTarihi = o.S.Tarih, OdemeBelge = o.S.BelgeNo, FaturaKalan = fKalan });
                    if (odemeler[pi].Kalan <= 0.005m) pi++;
                }
            }
            return l;
        }
        /// <summary>Tahsilatı kaydeder (isle=true: cariye işler; false: taslak). Hata metni ya da null.</summary>
        public static string Kaydet(string kod, DateTime tarih, string islemTipi, string pb, string mod, Dictionary<string, decimal> secim, List<TahsilatSatir> satirlar, string aciklama, string kullanici, bool isle, out TahsilatKaydi kayit, DateTime? simdi = null)
        {
            kayit = null; Tohumla();
            var k = CariKartOrnek.KartBul(kod); if (k == null) return "Cari bulunamadı."; bool ted = k.KartTipi == "Tedarikçi"; if (!ted && k.KartTipi != "Müşteri") return "Tahsilat yalnız müşteri, ödeme yalnız tedarikçi carilerinde kaydedilir.";
            pb = string.IsNullOrEmpty(pb) ? "TL" : pb; var l = SatirlariHazirla(satirlar, pb, tarih); if (l.Count == 0) return "En az bir ödeme satırı girin (tutar sıfırdan büyük)."; if (l.Any(s => s.Pb != pb)) return "Tüm ödeme satırları tahsilat para biriminde olmalı.";
            if (l.Any(s => s.Kur <= 0)) return "Kur sıfırdan büyük olmalı."; if (l.Any(s => s.Vadeli && string.IsNullOrEmpty(s.BelgeNo))) return "Çek / senet satırlarında belge (çek) no zorunludur."; if (!IslemTipleri.Contains(islemTipi ?? "")) return "İşlem tipi X ya da R olmalı.";
            mod = mod == "Manuel" ? "Manuel" : "Otomatik"; if (mod == "Manuel" && (secim == null || secim.Count == 0)) return "Manuel modda kapatılacak faturaları işaretleyin.";
            var dag = Hesapla(kod, pb, mod, secim, l);
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var no = ted ? $"ÖD-{tarih:yyyy}-{++_odemeSayac:00000}" : $"TH-{tarih:yyyy}-{++_sayac:00000}";
                kayit = new TahsilatKaydi { No = no, Tur = ted ? "Ödeme" : "Tahsilat", Tarih = tarih.Date, CariKodu = k.Kod, CariAd = k.KisaAd, Sirket = k.Sirket, ParaBirimi = pb, IslemTipi = islemTipi, Mod = mod, Aciklama = (aciklama ?? "").Trim(), BelgeNo = no, Satirlar = l, FaturaSecim = mod == "Manuel" ? new Dictionary<string, decimal>(secim) : new Dictionary<string, decimal>(), Dagitim = dag, Durum = "Taslak", Kullanici = kullanici ?? "", Zaman = z };
                _tahsilatlar.Insert(0, kayit);
                CariKartOrnek.Gunluk(k, z, kullanici, ted ? "Ödeme Taslağı" : "Tahsilat Taslağı", "", $"{no} · {kayit.Tutar.ToString("N2", Tr)} {pb} ({kayit.Yontem}, {islemTipi}) · {dag.Count} fatura eşleşmesi", ted ? "Ödeme" : "Tahsilat", kayit.Aciklama);
            }
            if (isle) { var h = IslemeAl(kayit.No, kullanici, simdi); if (h != null) return h; }
            return null;
        }
        /// <summary>
        /// Taslak kaydı cariye işler (ibraz): satır başına Cari Mutabakat hareketi, fatura kalanları düşer, avans eşleşmemiş tahsilat / ödeme olarak kalır. Hata metni ya da null.
        /// Tahsilat → alacak (Tahsilat / Çek / Senet); Ödeme → BORÇ (Ödeme / Çek / Senet) ve her satır için Kasa Nakit Akışı'na ÖDENDİ satır (NakitAkisaYaz).
        /// </summary>
        public static string IslemeAl(string no, string kullanici, DateTime? simdi = null)
        {
            var t = TahsilatBul(no); if (t == null) return "Kayıt bulunamadı."; if (t.Islendi) return $"{no} zaten işlenmiş.";
            var k = CariKartOrnek.KartBul(t.CariKodu); if (k == null) return "Cari bulunamadı."; bool od = t.Odeme; string ad = od ? "ödeme" : "tahsilat";
            // güncel açık faturalarla yeniden hesapla (taslak bekletilirken faturalar değişmiş olabilir)
            var dag = Hesapla(t.CariKodu, t.ParaBirimi, t.Mod, t.FaturaSecim, t.Satirlar);
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; t.Dagitim = dag;
                foreach (var s in t.Satirlar)
                {
                    var kapat = dag.Where(d => d.OdemeSira == s.Sira).GroupBy(d => d.BelgeNo).Select(g => (g.Key, g.Sum(d => d.Tutar))).ToList(); decimal dagitilan = Math.Round(kapat.Sum(x => x.Item2), 2), avans = Math.Round(s.Tutar - dagitilan, 2);
                    var h = new MtHareket { Tarih = s.Vadeli ? t.Tarih : s.Tarih, ParaBirimi = t.ParaBirimi, BelgeTipi = s.Yontem == "Çek" ? "Çek" : s.Yontem == "Senet" ? "Senet" : (od ? "Ödeme" : "Tahsilat"), BelgeNo = s.Vadeli && !string.IsNullOrEmpty(s.BelgeNo) ? s.BelgeNo : $"{t.No}/{s.Sira}", Alacak = od ? 0 : s.Tutar, Borc = od ? s.Tutar : 0, IslemTipi = t.IslemTipi,
                        Aciklama = $"{s.Yontem}{(string.IsNullOrEmpty(s.Banka) ? "" : " · " + s.Banka)}{(s.Vadeli ? $" · vade {s.Tarih:dd.MM.yyyy}" : "")} · {ad} {t.No} · {(t.Mod == "Manuel" ? "manuel" : "otomatik")}{(kapat.Count > 0 ? " · " + string.Join(", ", kapat.Select(x => x.Key)) : "")}{(string.IsNullOrWhiteSpace(t.Aciklama) ? "" : " · " + t.Aciklama)}", KapamaDurumu = s.Vadeli ? (s.Tarih > DateTime.Today ? "Vade Bekliyor" : (od ? "Ödendi" : "Tahsil Edildi")) : "—" };
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
        /// İşlenen ÖDEMENİN her satırını şirketin Kasa Nakit Akışı'na ÖDENDİ satır olarak yazar (Takip Paneli'nden "Ödeme Ekle" ile aynı yol): yöntem, belge / dekont no, banka hesabı, kapatılan fatura, kur;
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
                        U_Notlar = $"Yapılan Ödemeler › Yeni Ödeme {t.No}/{s.Sira} · {(t.Mod == "Manuel" ? "manuel fatura seçimi" : "otomatik (FIFO)")}{(s.Vadeli ? $" · {s.Yontem} vade {s.Tarih:dd.MM.yyyy}" : "")}{(string.IsNullOrEmpty(s.Not) ? "" : " · " + s.Not)}", U_Satinalmaci = "", U_FaturaSahibi = k.Sirket, U_OdeyenSirket = k.Sirket
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
            var kalemler = t.Dagitim.Where(d => (kur ? d.KurFarki : d.VadeFarki) > 0.005m).Select(d => (kur ? $"{d.BelgeNo} kur farkı: {d.Tutar.ToString("N2", Tr)} {d.Pb} × ({d.TahsilatKuru.ToString("N4", Tr)} − {d.FaturaKuru.ToString("N4", Tr)})" : $"{d.BelgeNo} vade farkı: {d.GecikmeGun} gün gecikme × %{VadeFarkiAylikOran.ToString("0.##", Tr)} / ay ({d.OdemeYontem} {d.OdemeTarihi:dd.MM.yyyy})", kur ? d.KurFarki : d.VadeFarki)).ToList();
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
        /// <summary>Örnek kayıtlar: otomatik TL havale (işlenmiş), fatura bazlı USD tahsilat (kur farkı), gecikmeli çek + nakit toplu tahsilat (vade farkı faturası kesilmiş), bekleyen taslak.</summary>
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var b = DateTime.Today;
                TahsilatSatir S(string y, DateTime t, decimal tutar, string pb = "TL", string belge = "", string banka = "") => new TahsilatSatir { Yontem = y, Tarih = t, Tutar = tutar, Pb = pb, BelgeNo = belge, Banka = banka };
                Kaydet("120.003", b.AddDays(-6), "X", "TL", "Otomatik", null, new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-6), 850_000m, "TL", "", "Ziraat Bankası") }, "Haftalık ödeme planı", "Nazlı Erdem", true, out _, b.AddDays(-6).AddHours(14));
                var usd = AcikFaturalar("120.001").Where(f => f.ParaBirimi == "USD").Take(2).ToList();
                if (usd.Count > 0) Kaydet("120.001", b.AddDays(-3), "X", "USD", "Manuel", usd.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => 0m), new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-3), Math.Round(usd.Sum(f => f.Kalan), 2), "USD", "", "Garanti BBVA") }, "USD faturalar kapatıldı", "Nazlı Erdem", true, out _, b.AddDays(-3).AddHours(11));
                var gec = AcikFaturalar("120.011").Where(f => f.ParaBirimi == "TL" && (f.Vade ?? f.Tarih) < b.AddDays(-30)).Take(2).ToList();
                if (gec.Count > 0) { decimal top = Math.Round(gec.Sum(f => f.Kalan), 2); Kaydet("120.011", b.AddDays(-1), "R", "TL", "Manuel", gec.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => 0m), new List<TahsilatSatir> { S("Nakit", b.AddDays(-1), Math.Round(top * 0.3m, 2)), S("Çek", b.AddDays(44), Math.Round(top * 0.7m, 2), "TL", "ÇK-4471920", "İş Bankası") }, "Gecikmiş faturalar nakit + çekle tahsil", "Nazlı Erdem", true, out var k3, b.AddDays(-1).AddHours(16)); if (k3 != null && k3.VadeFarki > 0) FaturaKes(k3.No, "Vade Farkı", "Nazlı Erdem", out _, b.AddDays(-1).AddHours(16).AddMinutes(20)); }
                var tas = AcikFaturalar("120.002").Where(f => f.ParaBirimi == "TL").Take(3).ToList();
                if (tas.Count > 0) Kaydet("120.002", b, "X", "TL", "Otomatik", null, new List<TahsilatSatir> { S("Çek", b.AddDays(30), Math.Round(tas.Sum(f => f.Kalan) * 0.5m, 2), "TL", "ÇK-5580113", "Yapı Kredi"), S("Çek", b.AddDays(60), Math.Round(tas.Sum(f => f.Kalan) * 0.5m, 2), "TL", "ÇK-5580114", "Yapı Kredi") }, "Müşteriden gelen iki vadeli çek; teslim alınınca işlenecek", "Nazlı Erdem", false, out _, b.AddHours(9));
                // --- ÖDEME örnekleri (Yapılan Ödemeler › Yeni Ödeme): havaleyle fatura bazlı ödeme (işlenmiş), nakit + vadeli çek toplu ödeme (işlenmiş), teslim bekleyen çek taslağı
                var tedler = CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Tedarikçi").Select(x => x.Kod).Where(kod => AcikFaturalar(kod).Any(f => f.ParaBirimi == "TL")).Take(3).ToList();
                if (tedler.Count > 0) { var f1 = AcikFaturalar(tedler[0]).Where(f => f.ParaBirimi == "TL").Take(2).ToList(); Kaydet(tedler[0], b.AddDays(-5), "X", "TL", "Manuel", f1.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => 0m), new List<TahsilatSatir> { S("Havale / EFT", b.AddDays(-5), Math.Round(f1.Sum(f => f.Kalan), 2), "TL", "DK-5520341", "Ziraat Bankası") }, "Vadesi gelen alış faturaları havaleyle ödendi", "Serkan Genç", true, out _, b.AddDays(-5).AddHours(15)); }
                if (tedler.Count > 1) { decimal top = Math.Round(AcikFaturalar(tedler[1]).Where(f => f.ParaBirimi == "TL").Sum(f => f.Kalan), 2); if (top > 0) Kaydet(tedler[1], b.AddDays(-2), "X", "TL", "Otomatik", null, new List<TahsilatSatir> { S("Nakit", b.AddDays(-2), Math.Round(top * 0.25m, 2)), S("Çek", b.AddDays(58), Math.Round(top * 0.75m, 2), "TL", "ÇK-7731204", "Garanti BBVA") }, "Bir kısmı nakit, kalanı 60 gün vadeli çekle", "Serkan Genç", true, out _, b.AddDays(-2).AddHours(11)); }
                if (tedler.Count > 2) { var f3 = AcikFaturalar(tedler[2]).Where(f => f.ParaBirimi == "TL").Take(2).ToList(); if (f3.Count > 0) Kaydet(tedler[2], b, "X", "TL", "Manuel", f3.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => 0m), new List<TahsilatSatir> { S("Çek", b.AddDays(45), Math.Round(f3.Sum(f => f.Kalan), 2), "TL", "ÇK-7731205", "Garanti BBVA") }, "Çek hazırlandı; tedarikçiye teslim edilince işlenecek", "Serkan Genç", false, out _, b.AddHours(10)); }
            }
        }
    }
}
