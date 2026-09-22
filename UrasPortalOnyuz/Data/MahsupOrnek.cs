// ============================================================
// MAHSUP MODÜLÜ — veri modeli, açık kalem eşleştirme / dağılım motoru, muhasebe / iç mutabakat simülasyonu, ERP ve kontrol motoru, örnek kayıtlar
// Kaynak: "URAS HOLDING Mahsup Modülü — IT Teknik ve İşlevsel Gereksinim Dokümanı v1.0 (21.09.2026)".
// Üç sekme aynı veri setini kullanır: İşlem Ekranı (açık kalemleri getir, borç / alacak kalemlerini TEK TEK seç, kısmi tutar, 1-1 / 1-N / N-1 / N-N
// dağılım, otomatik eşleştirme, simülasyon, onay, ERP), Raporu, Muhasebe Kontrolü (beklenen–gerçekleşen + açık kalem kapanma + mirror).
// Muhasebe prensibi (§1): mahsup KDV doğurmaz; yalnız açık kalem kapama, cari aktarımı ve kur / yuvarlama farkı kaydı oluşur.
// Hesaplar MAHSUP_HESAP_ESLEME (Esleme) tablosundan gelir. Kayıtlar bellek içindedir.
// Ekran: Controllers/GrupIciController.Mahsup → Views/GrupIci/Mahsup.cshtml + wwwroot/js/mahsup.js; uçlar Controllers/MahsupController.cs.
// Ortak sınıflar (YnOnay, YnAudit, YnKontrol, YnSonuc) Data/YansitmaOrnek.cs içindedir.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>ERP açık kalemi (cari borç / alacak belgesi). Yon: "Borç" = borç bakiyeli (120 satış faturası, 159 verilen avans …), "Alacak" = alacak bakiyeli (320 alış faturası, 340 alınan avans, iade …).</summary>
    public class MhAcikKalem
    {
        public int Id { get; set; } public string Sirket { get; set; } public string CariKod { get; set; } public string CariAd { get; set; } public string CariTip { get; set; }   // Müşteri | Tedarikçi | Grup Şirketi
        public string Yon { get; set; } public string BelgeTuru { get; set; } public string BelgeNo { get; set; } public int DocEntry { get; set; } public DateTime Tarih { get; set; } public DateTime Vade { get; set; }
        public string ParaBirimi { get; set; } = "TRY"; public decimal Kur { get; set; } = 1; public decimal OrijinalTutar { get; set; } public decimal Kapanan { get; set; }
        public string Proje { get; set; } public string MasrafMerkezi { get; set; } public string Referans { get; set; } public string Durum { get; set; } = "Açık";
        public string HesapRolu => MahsupOrnek.HesapRolu(this);
    }
    /// <summary>OFFSET_OPEN_ITEM — işleme seçilen açık kalem (snapshot + bu işlem mahsup tutarı).</summary>
    public class MhKalem
    {
        public int KalemId { get; set; } public string Yon { get; set; } public string Sirket { get; set; } public string CariKod { get; set; } public string CariAd { get; set; } public string CariTip { get; set; } public string BelgeTuru { get; set; } public string BelgeNo { get; set; } public int DocEntry { get; set; }
        public DateTime Tarih { get; set; } public DateTime Vade { get; set; } public string ParaBirimi { get; set; } public decimal BelgeKuru { get; set; } public decimal OrijinalTutar { get; set; } public decimal Kapanan { get; set; } public decimal Rezerve { get; set; } public decimal MahsupTutari { get; set; }
        public string Proje { get; set; } public string MasrafMerkezi { get; set; } public string Referans { get; set; } public string HesapRolu { get; set; } public string Hesap { get; set; }
        public decimal AcikTutar => Math.Max(0, OrijinalTutar - Kapanan - Rezerve); public decimal SonrasiKalan => AcikTutar - MahsupTutari; public string KapamaTipi => MahsupTutari <= 0 ? "—" : SonrasiKalan <= 0.005m ? "Tam Kapandı" : "Kısmi Açık";
    }
    /// <summary>OFFSET_ALLOCATION — borç belge → alacak belge dağılımı.</summary>
    public class MhDagilim { public string Id { get; set; } public int BorcKalemId { get; set; } public string BorcBelge { get; set; } public int AlacakKalemId { get; set; } public string AlacakBelge { get; set; } public decimal Tutar { get; set; } public string ParaBirimi { get; set; } public decimal Kur { get; set; } public decimal TutarTl { get; set; } public decimal KurFarki { get; set; } public string Durum { get; set; } = "Hazır"; }
    /// <summary>Beklenen (simülasyon) ya da gerçekleşen (ERP) satır.</summary>
    public class MhFisSatir { public string Sirket { get; set; } public string Tip { get; set; } public string FisNo { get; set; } public string HesapRolu { get; set; } public string HesapKodu { get; set; } public string HesapAdi { get; set; } public string Cari { get; set; } public decimal Borc { get; set; } public decimal Alacak { get; set; } public string ParaBirimi { get; set; } public decimal Kur { get; set; } public decimal DovizTutar { get; set; } public string Proje { get; set; } public string MasrafMerkezi { get; set; } public string Aciklama { get; set; } public string Kural { get; set; } public string Referans { get; set; } }
    public class MhErp { public string Sirket { get; set; } public string Durum { get; set; } = "NotStarted"; public string YevmiyeNo { get; set; } public string MutabakatNo { get; set; } public string Cevap { get; set; } public DateTime? Zaman { get; set; } public string IdempotencyKey { get; set; } public string CorrelationId { get; set; } }
    public class MhAcikKalemKontrol { public string Belge { get; set; } public decimal OncekiAcik { get; set; } public decimal BeklenenMahsup { get; set; } public decimal BeklenenKalan { get; set; } public decimal ErpKalan { get; set; } public string Sonuc { get; set; } }

    public class MhIslem
    {
        public int Id { get; set; } public string No { get; set; } public string GrupMahsupNo { get; set; } public DateTime Tarih { get; set; } public DateTime KayitTarihi { get; set; }
        public string Sirket { get; set; } public string KarsiSirket { get; set; } public string Kapsam { get; set; } = "Şirket İçi"; public string Tur { get; set; } public string TurAd => MahsupOrnek.TurAd(Tur);
        public string BorcCari { get; set; } public string AlacakCari { get; set; } public string ParaBirimi { get; set; } = "TRY"; public decimal Kur { get; set; } = 1; public DateTime? KurTarihi { get; set; }
        public string MasrafMerkezi { get; set; } public string Proje { get; set; } public string Departman { get; set; } public string NedenKodu { get; set; } public string Neden { get; set; } public string Aciklama { get; set; } public string EslestirmeYontemi { get; set; } = "Manuel";
        public string Durum { get; set; } = "Taslak";
        public List<MhKalem> Kalemler { get; set; } = new List<MhKalem>(); public List<MhDagilim> Dagilimlar { get; set; } = new List<MhDagilim>();
        public List<MhFisSatir> Simulasyon { get; set; } = new List<MhFisSatir>(); public string SimulasyonTipi { get; set; } public int SimulasyonVersiyonu { get; set; } public string SimulasyonHash { get; set; } public DateTime? SimulasyonTarihi { get; set; }
        public List<YnOnay> Onaylar { get; set; } = new List<YnOnay>(); public List<YnAudit> Audit { get; set; } = new List<YnAudit>();
        public List<MhErp> Erp { get; set; } = new List<MhErp>(); public List<MhFisSatir> Gercek { get; set; } = new List<MhFisSatir>(); public Dictionary<string, decimal> ErpKalan { get; set; } = new Dictionary<string, decimal>(); public DateTime? GercekTarihi { get; set; }
        public string KontrolDurumu { get; set; } public List<YnKontrol> Kontroller { get; set; } = new List<YnKontrol>(); public List<MhAcikKalemKontrol> AcikKalemKontrol { get; set; } = new List<MhAcikKalemKontrol>(); public DateTime? KontrolTarihi { get; set; } public string FarkKapatma { get; set; }
        public string RedNedeni { get; set; } public string IptalNedeni { get; set; } public string Olusturan { get; set; } public DateTime OlusturmaZamani { get; set; } public string Guncelleyen { get; set; } public DateTime GuncellemeZamani { get; set; } public int Versiyon { get; set; } = 1;

        public IEnumerable<MhKalem> Borclar => Kalemler.Where(k => k.Yon == "Borç"); public IEnumerable<MhKalem> Alacaklar => Kalemler.Where(k => k.Yon == "Alacak");
        public decimal BorcToplam => Borclar.Sum(k => k.MahsupTutari); public decimal AlacakToplam => Alacaklar.Sum(k => k.MahsupTutari); public decimal Fark => BorcToplam - AlacakToplam; public decimal MahsupTutari => Math.Min(BorcToplam, AlacakToplam);
        /// <summary>Kur farkı (TL, + = kambiyo kârı / − = zarar); yuvarlama farkı aynı işaret kuralıyla.</summary>
        public decimal KurFarki => Simulasyon.Where(x => x.Tip == "FX").Sum(x => x.Alacak - x.Borc); public decimal YuvarlamaFarki => Simulasyon.Where(x => x.Tip == "YUV").Sum(x => x.Alacak - x.Borc);
        public int TamKapanan => Kalemler.Count(k => k.KapamaTipi == "Tam Kapandı"); public int KismiKapanan => Kalemler.Count(k => k.KapamaTipi == "Kısmi Açık"); public decimal SonrasiAcik => Kalemler.Sum(k => Math.Max(0, k.SonrasiKalan)); public int BelgeSayisi => Kalemler.Select(k => k.BelgeNo).Distinct().Count();
        public bool Aktif => Durum != "İptal" && Durum != "Reddedildi" && Durum != "Ters Kayıt"; public bool Onaylandi => Onaylar.Count > 0 && Onaylar.All(o => o.Durum == "Onaylandı"); public bool Duzenlenebilir => Durum == "Taslak" || Durum == "Reddedildi";
        public string ErpDurumu { get { if (Erp.Count == 0) return "NotStarted"; if (Erp.All(e => e.Durum == "Reversed")) return "Reversed"; if (Erp.All(e => e.Durum == "Posted" || e.Durum == "Reconciled")) return Erp.Any(e => e.Durum == "Posted") ? "Posted" : "Reconciled"; if (Erp.Any(e => e.Durum == "Failed")) return Erp.Any(e => e.Durum == "Posted" || e.Durum == "Reconciled") ? "PartiallyCompleted" : "Failed"; return "Processing"; } }
        public string MuhasebeDurumu { get { var d = ErpDurumu; return d == "NotStarted" ? "Başlamadı" : d == "Reconciled" ? "Mutabakat Oluştu" : d == "Posted" ? "Muhasebeleşti" : d == "PartiallyCompleted" ? "Kısmi" : d == "Failed" ? "Hata" : d == "Reversed" ? "Ters Kayıt" : d; } }
    }
    public class MhEsleme { public int Id { get; set; } public string Sirket { get; set; } = "*"; public string MahsupTuru { get; set; } = "*"; public string HesapRolu { get; set; } public string CariGrubu { get; set; } = "*"; public string ParaBirimi { get; set; } = "*"; public string Hesap { get; set; } public string HesapAdi { get; set; } public string BpMode { get; set; } = "BP"; public DateTime GecerlilikBas { get; set; } = new DateTime(2026, 1, 1); public bool Aktif { get; set; } = true; public int Ozgulluk => (Sirket == "*" ? 0 : 4) + (MahsupTuru == "*" ? 0 : 2) + (ParaBirimi == "*" ? 0 : 1); }

    // ---- JSON giriş ----
    public class MhKalemGiris { public int KalemId { get; set; } public decimal MahsupTutari { get; set; } }
    public class MhDagilimGiris { public int BorcKalemId { get; set; } public int AlacakKalemId { get; set; } public decimal Tutar { get; set; } }
    public class MhIslemGiris
    {
        public int Id { get; set; } public string Rol { get; set; } public string Tarih { get; set; } public string KayitTarihi { get; set; } public string Sirket { get; set; } public string KarsiSirket { get; set; } public string Kapsam { get; set; } public string Tur { get; set; } public string BorcCari { get; set; } public string AlacakCari { get; set; }
        public string ParaBirimi { get; set; } public decimal? Kur { get; set; } public string KurTarihi { get; set; } public string MasrafMerkezi { get; set; } public string Proje { get; set; } public string Departman { get; set; } public string NedenKodu { get; set; } public string Neden { get; set; } public string Aciklama { get; set; } public string EslestirmeYontemi { get; set; }
        public List<MhKalemGiris> Kalemler { get; set; } public List<MhDagilimGiris> Dagilimlar { get; set; } public string Gerekce { get; set; }
    }

    public static class MahsupOrnek
    {
        public static readonly CultureInfo Tr = new CultureInfo("tr-TR"); public static readonly object Kilit = new object();
        public static readonly (string Kod, string Ad, string Aciklama, string Sonuc)[] Turler =
        {
            ("M01", "Aynı Cari İç Mutabakat", "Aynı cari üzerindeki borç ve alacak açık kalemlerinin eşleştirilmesi.", "Yeni yevmiye olmayabilir; iç mutabakat / açık kalem kapama."),
            ("M02", "Müşteri-Tedarikçi Mahsubu", "Aynı tüzel kişiyle ilişkili müşteri ve tedarikçi bakiyelerinin mahsubu.", "Cari kontrol hesapları arasında kayıt (320 Borç / 120 Alacak) + iç mutabakat."),
            ("M03", "Tedarikçi Avans-Fatura", "Verilen avansın tedarikçi faturasıyla kapatılması.", "320 Borç / 159 Alacak + iç mutabakat."),
            ("M04", "Müşteri Avans-Fatura", "Alınan avansın satış faturasıyla kapatılması.", "340 Borç / 120 Alacak + iç mutabakat."),
            ("M05", "İade/Credit Note-Fatura", "İade / credit note açık kaleminin faturayla kapatılması.", "Çoğunlukla iç mutabakat; hesap farklıysa fiş."),
            ("M06", "Grup Şirketleri Arası Mahsup", "Karşılıklı grup şirketi cari bakiyelerinin iki şirket tarafında kapatılması.", "İki şirkette bağlantılı / mirror kayıt + mutabakat kontrolü."),
            ("M07", "Diğer Onaylı Mahsup", "Yetkili tanımlı özel mahsup senaryosu.", "Hesap eşleme tablosuna göre.")
        };
        public static string TurAd(string kod) => Turler.FirstOrDefault(t => t.Kod == kod).Ad ?? kod;
        public static readonly string[] Kapsamlar = { "Şirket İçi", "Grup Şirketleri Arası" };
        public static readonly string[] EslestirmeYontemleri = { "Manuel", "En Eski Vade Önce", "En Eski Belge Önce", "Aynı Tutarları Eşleştir", "Referans Bazlı" };
        public static readonly string[] NedenKodlari = { "MN01 Avans Kapama", "MN02 İade / Credit Note Kapama", "MN03 Karşılıklı Cari Netleştirme", "MN04 Grup İçi Bakiye Kapama", "MN05 Dönem Sonu Mutabakat", "MN06 Diğer" };
        public static readonly string[] Durumlar = { "Taslak", "Onay Bekliyor", "Onaylandı", "Mutabakat Oluştu", "Muhasebeleşti", "Kısmi Entegrasyon", "Hata", "Uyumlu", "Kontrol Gerekli", "Reddedildi", "İptal", "Ters Kayıt" };
        public static readonly string[] KontrolDurumlari = { "Uyumlu", "Kontrol Gerekli", "Hesap Hatası", "Tutar Farkı", "Kur Farkı Hatası", "Açık Kalem Farkı", "İç Mutabakat Oluşmamış", "Mükerrer Kayıt", "Kısmi Entegrasyon", "Muhasebe Kaydı Oluşmamış" };
        public static readonly string[] KontrolOnceligi = { "Muhasebe Kaydı Oluşmamış", "Kısmi Entegrasyon", "Mükerrer Kayıt", "İç Mutabakat Oluşmamış", "Tutar Farkı", "Hesap Hatası", "Kur Farkı Hatası", "Açık Kalem Farkı", "Kontrol Gerekli" };
        public static readonly Dictionary<string, string[]> Yetkiler = new Dictionary<string, string[]>
        {
            ["Operasyon"] = new[] { "taslak", "kalemSec", "simulasyon", "onayaGonder", "iptal", "kontrolGor" },
            ["Muhasebe"] = new[] { "taslak", "kalemSec", "caprazDoviz", "farkKabul", "simulasyon", "onayaGonder", "onay", "muhasebelestir", "tersKayit", "esleme", "kontrol", "farkKapat", "iptal", "kontrolGor" },
            ["Yönetici"] = new[] { "taslak", "kalemSec", "caprazDoviz", "farkKabul", "simulasyon", "onayaGonder", "onay", "muhasebelestir", "tersKayit", "esleme", "kontrol", "farkKapat", "iptal", "kontrolGor" },
            ["Sistem Admin"] = new[] { "taslak", "kalemSec", "caprazDoviz", "farkKabul", "simulasyon", "onayaGonder", "muhasebelestir", "tersKayit", "esleme", "kontrol", "farkKapat", "iptal", "kontrolGor" }
        };
        public static bool Yetkili(string rol, string eylem) => Yetkiler.TryGetValue(rol ?? "", out var l) && l.Contains(eylem);
        public static readonly Dictionary<string, decimal> Parametreler = new Dictionary<string, decimal> { ["direktorEsik"] = 500000m, ["yuvarlamaTolerans"] = 1m, ["kurFarkiUst"] = 50000m, ["tutarTolerans"] = 1m, ["caprazDoviz"] = 0, ["rezervasyonGun"] = 7 };
        public static readonly Dictionary<string, string> ParametreEtiketler = new Dictionary<string, string> { ["direktorEsik"] = "Bu mahsup tutarını (TL) aşan işlemde Direktör onayı", ["yuvarlamaTolerans"] = "Yuvarlama farkı toleransı (TL); aşarsa işlem bloklanır", ["kurFarkiUst"] = "Kur farkı üst sınırı (TL); aşarsa Direktör onayı", ["tutarTolerans"] = "Kontrol tutar toleransı (TL)", ["caprazDoviz"] = "Farklı para birimleri arası mahsup açık (1 = evet, yetkiyle)", ["rezervasyonGun"] = "Taslak / onay rezervasyonu serbest bırakma süresi (gün)" };
        public static readonly string[] Sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct().ToArray();
        public static readonly string[] MasrafMerkezleri = YansitmaOrnek.MasrafMerkezleri; public static readonly string[] Projeler = YansitmaOrnek.Projeler;
        public static string Kisa(string s) => YansitmaOrnek.Kisa(s);

        // ---- hesap rolleri (belge türüne göre) ve eşleme tablosu (MAHSUP_HESAP_ESLEME) ----
        public static string HesapRolu(MhAcikKalem k) => k.CariTip == "Grup Şirketi" ? (k.Yon == "Borç" ? "Grup Şirketi Alacak" : "Grup Şirketi Borç") : k.BelgeTuru == "Verilen Avans" ? "Verilen Avans" : k.BelgeTuru == "Alınan Avans" ? "Alınan Avans" : (k.CariTip == "Müşteri" ? "Müşteri Cari Kontrol" : "Tedarikçi Cari Kontrol");
        public static readonly List<MhEsleme> Esleme = new List<MhEsleme>
        {
            new MhEsleme { Id = 1, HesapRolu = "Müşteri Cari Kontrol", Hesap = "120.01.{CARI}", HesapAdi = "Alıcılar", BpMode = "BP ShortName" },
            new MhEsleme { Id = 2, HesapRolu = "Tedarikçi Cari Kontrol", Hesap = "320.01.{CARI}", HesapAdi = "Satıcılar", BpMode = "BP ShortName" },
            new MhEsleme { Id = 3, HesapRolu = "Verilen Avans", Hesap = "159.01.{CARI}", HesapAdi = "Verilen Sipariş Avansları", BpMode = "GL" },
            new MhEsleme { Id = 4, HesapRolu = "Alınan Avans", Hesap = "340.01.{CARI}", HesapAdi = "Alınan Sipariş Avansları", BpMode = "GL" },
            new MhEsleme { Id = 5, HesapRolu = "Grup Şirketi Alacak", Hesap = "120.02.{CARI}", HesapAdi = "Alıcılar — grup şirketi", BpMode = "BP ShortName" },
            new MhEsleme { Id = 6, HesapRolu = "Grup Şirketi Borç", Hesap = "320.02.{CARI}", HesapAdi = "Satıcılar — grup şirketi", BpMode = "BP ShortName" },
            new MhEsleme { Id = 7, HesapRolu = "Mahsup Clearing", Hesap = "195.05.001", HesapAdi = "Mahsup Geçici / Clearing", BpMode = "Clearing" },
            new MhEsleme { Id = 8, HesapRolu = "Kur Farkı Geliri", Hesap = "646.01.001", HesapAdi = "Kambiyo Kârları", BpMode = "GL" },
            new MhEsleme { Id = 9, HesapRolu = "Kur Farkı Gideri", Hesap = "656.01.001", HesapAdi = "Kambiyo Zararları", BpMode = "GL" },
            new MhEsleme { Id = 10, HesapRolu = "Yuvarlama Farkı", Hesap = "679.09.001 / 689.09.001", HesapAdi = "Yuvarlama Farkları (gelir / gider)", BpMode = "GL" },
            new MhEsleme { Id = 11, Sirket = "Avrupa Paper", HesapRolu = "Müşteri Cari Kontrol", ParaBirimi = "EUR", Hesap = "120.03.{CARI}", HesapAdi = "Alıcılar — yurt dışı / dövizli", BpMode = "BP ShortName" },
            new MhEsleme { Id = 12, Sirket = "Uras Power", HesapRolu = "Grup Şirketi Borç", Hesap = "336.02.{CARI}", HesapAdi = "Diğer Çeşitli Borçlar — grup", BpMode = "GL", Aktif = false }
        };
        public static MhEsleme EslemeBul(string sirket, string tur, string rol, string pb, DateTime tarih) => Esleme.Where(e => e.Aktif && e.GecerlilikBas <= tarih && e.HesapRolu == rol && (e.Sirket == "*" || e.Sirket == sirket) && (e.MahsupTuru == "*" || e.MahsupTuru == tur) && (e.ParaBirimi == "*" || e.ParaBirimi == pb)).OrderByDescending(e => e.Ozgulluk).FirstOrDefault();
        private static string Hesap(MhEsleme e, string cari) => e.Hesap.Replace("{CARI}", cari);
        /// <summary>Hesap kodundaki cari alt kırılımı: grup şirketinde kısa kod, diğerlerinde ERP cari kodu.</summary>
        public static string CariKisa(MhAcikKalem k) => k.CariTip == "Grup Şirketi" ? Kisa(k.CariAd) : k.CariKod;

        // ---- açık kalemler (ERP kaynağı; örnek) ----
        private static readonly Lazy<List<MhAcikKalem>> _kalemler = new Lazy<List<MhAcikKalem>>(KalemOlustur);
        public static List<MhAcikKalem> AcikKalemler => _kalemler.Value;
        private static List<MhAcikKalem> KalemOlustur()
        {
            var b = DateTime.Today; int id = 0, de = 5000;
            MhAcikKalem K(string sirket, string cariKod, string cariAd, string cariTip, string yon, string tur, string no, int gunOnce, int vadeGun, decimal tutar, decimal kapanan = 0, string pb = "TRY", decimal kur = 1, string referans = null, string proje = null, string mm = null) =>
                new MhAcikKalem { Id = ++id, Sirket = sirket, CariKod = cariKod, CariAd = cariAd, CariTip = cariTip, Yon = yon, BelgeTuru = tur, BelgeNo = no, DocEntry = ++de, Tarih = b.AddDays(-gunOnce), Vade = b.AddDays(-gunOnce + vadeGun), ParaBirimi = pb, Kur = kur, OrijinalTutar = tutar, Kapanan = kapanan, Referans = referans ?? "", Proje = proje ?? "", MasrafMerkezi = mm ?? "CC20 - Finans" };
            var l = new List<MhAcikKalem>
            {
                // §19.2 / §4.7: Uras Kimya · Marmara Plastik (müşteri) — SF 100.000 borç; iadeler 40.000 + 35.000 alacak
                K("Uras Kimya", "M-120-0031", "Marmara Plastik San. A.Ş.", "Müşteri", "Borç", "Satış Faturası", "SF-2026-105", 45, 60, 100000, referans: "SAS-2026-M-118"),
                K("Uras Kimya", "M-120-0031", "Marmara Plastik San. A.Ş.", "Müşteri", "Borç", "Satış Faturası", "SF-2026-108", 38, 60, 48000, referans: "SAS-2026-M-131"),
                K("Uras Kimya", "M-120-0031", "Marmara Plastik San. A.Ş.", "Müşteri", "Alacak", "Satış İade Faturası", "IA-2026-021", 30, 0, 40000, referans: "SAS-2026-M-118"),
                K("Uras Kimya", "M-120-0031", "Marmara Plastik San. A.Ş.", "Müşteri", "Alacak", "Satış İade Faturası", "IA-2026-024", 22, 0, 35000, referans: "SAS-2026-M-131"),
                K("Uras Kimya", "M-120-0031", "Marmara Plastik San. A.Ş.", "Müşteri", "Alacak", "Tahsilat", "TH-2026-0412", 10, 0, 25000, referans: "Havale"),
                // §19.1: Uras Kimya · Petkim (tedarikçi) — AF 100.000 alacak; verilen avans 60.000 borç
                K("Uras Kimya", "T-320-0007", "Petkim Petrokimya Holding A.Ş.", "Tedarikçi", "Alacak", "Alış Faturası", "AF-2026-0771", 20, 60, 100000, referans: "SAS-2026-04117"),
                K("Uras Kimya", "T-320-0007", "Petkim Petrokimya Holding A.Ş.", "Tedarikçi", "Borç", "Verilen Avans", "AV-2026-0118", 35, 0, 60000, referans: "SAS-2026-04117"),
                K("Uras Kimya", "T-320-0007", "Petkim Petrokimya Holding A.Ş.", "Tedarikçi", "Alacak", "Alış Faturası", "AF-2026-0802", 8, 60, 244604.39m, referans: "SAS-2026-04130"),
                K("Uras Kimya", "T-320-0007", "Petkim Petrokimya Holding A.Ş.", "Tedarikçi", "Borç", "Alış İade Faturası", "AI-2026-0033", 12, 0, 18500, referans: "SAS-2026-04117"),
                // M02: Uras Kimya · Karadeniz Ambalaj hem müşteri hem tedarikçi
                K("Uras Kimya", "M-120-0058", "Karadeniz Ambalaj Ltd. Şti.", "Müşteri", "Borç", "Satış Faturası", "SF-2026-112", 40, 45, 66000, referans: "SAS-2026-K-092"),
                K("Uras Kimya", "M-120-0058", "Karadeniz Ambalaj Ltd. Şti.", "Müşteri", "Borç", "Satış Faturası", "SF-2026-119", 15, 45, 90000, referans: "SAS-2026-K-101"),
                K("Uras Kimya", "T-320-0058", "Karadeniz Ambalaj Ltd. Şti.", "Tedarikçi", "Alacak", "Alış Faturası", "AF-2026-0790", 25, 60, 66000, referans: "Koli alımı"),
                K("Uras Kimya", "T-320-0058", "Karadeniz Ambalaj Ltd. Şti.", "Tedarikçi", "Alacak", "Alış Faturası", "AF-2026-0815", 6, 60, 42000, referans: "Koli alımı"),
                // M06: Uras Kimya ↔ Alv Kimya karşılıklı (yansıtma faturaları); mirror kalemler Alv Kimya defterinde
                K("Uras Kimya", "G-120-ALV", "Alv Kimya", "Grup Şirketi", "Borç", "Grup İçi Fatura", "URS2026200111874", 22, 30, 159789.70m, referans: "YNS-000122"),
                K("Uras Kimya", "G-120-ALV", "Alv Kimya", "Grup Şirketi", "Borç", "Grup İçi Fatura", "URS2026200126000", 5, 30, 50000, referans: "YNS-000138"),
                K("Uras Kimya", "G-320-ALV", "Alv Kimya", "Grup Şirketi", "Alacak", "Grup İçi Fatura", "ALV2026200000431", 18, 30, 50000, referans: "ALV yansıtma"),
                K("Uras Kimya", "G-320-ALV", "Alv Kimya", "Grup Şirketi", "Alacak", "Grup İçi Fatura", "ALV2026200000452", 9, 30, 128000, referans: "ALV hizmet"),
                K("Alv Kimya", "G-320-URAS", "Uras Kimya", "Grup Şirketi", "Alacak", "Grup İçi Fatura", "URS2026200111874", 22, 30, 159789.70m, referans: "YNS-000122"),
                K("Alv Kimya", "G-320-URAS", "Uras Kimya", "Grup Şirketi", "Alacak", "Grup İçi Fatura", "URS2026200126000", 5, 30, 50000, referans: "YNS-000138"),
                K("Alv Kimya", "G-120-URAS", "Uras Kimya", "Grup Şirketi", "Borç", "Grup İçi Fatura", "ALV2026200000431", 18, 30, 50000, referans: "ALV yansıtma"),
                K("Alv Kimya", "G-120-URAS", "Uras Kimya", "Grup Şirketi", "Borç", "Grup İçi Fatura", "ALV2026200000452", 9, 30, 128000, referans: "ALV hizmet"),
                // M04: Selvi · müşteri avans-fatura (80.000)
                K("Selvi", "M-120-0102", "Ege Boya ve Vernik San. A.Ş.", "Müşteri", "Alacak", "Alınan Avans", "AA-2026-0045", 28, 0, 80000, referans: "SAS-2026-S-044"),
                K("Selvi", "M-120-0102", "Ege Boya ve Vernik San. A.Ş.", "Müşteri", "Borç", "Satış Faturası", "SLV2026100004512", 25, 60, 1728000, referans: "SAS-2026-S-044"),
                K("Selvi", "M-120-0102", "Ege Boya ve Vernik San. A.Ş.", "Müşteri", "Borç", "Satış Faturası", "SLV2026100004530", 12, 60, 96000, referans: "SAS-2026-S-051"),
                // M05 dövizli: Avrupa Paper · Karadeniz Ambalaj EUR fatura ve iade (farklı kayıt kurları → kur farkı)
                K("Avrupa Paper", "M-120-0210", "Karadeniz Ambalaj Ltd. Şti.", "Müşteri", "Borç", "Satış Faturası", "AVP2026100002230", 22, 75, 209520, 0, "EUR", 46.90m, "SAS-2026-A-017"),
                K("Avrupa Paper", "M-120-0210", "Karadeniz Ambalaj Ltd. Şti.", "Müşteri", "Alacak", "Satış İade Faturası", "AVI2026100000071", 9, 0, 12000, 0, "EUR", 47.60m, "SAS-2026-A-017"),
                K("Avrupa Paper", "M-120-0210", "Karadeniz Ambalaj Ltd. Şti.", "Müşteri", "Alacak", "Tahsilat", "TH-2026-0906", 4, 0, 30000, 0, "EUR", 47.85m, "Swift"),
                // M03 ikinci tedarikçi: Selvi · Korozo
                K("Selvi", "T-320-0044", "Korozo Ambalaj San. ve Tic. A.Ş.", "Tedarikçi", "Alacak", "Alış Faturası", "AF-2026-0611", 30, 60, 385000, referans: "SAS-2026-S-030"),
                K("Selvi", "T-320-0044", "Korozo Ambalaj San. ve Tic. A.Ş.", "Tedarikçi", "Borç", "Verilen Avans", "AV-2026-0090", 50, 0, 150000, referans: "SAS-2026-S-030"),
                K("Selvi", "T-320-0044", "Korozo Ambalaj San. ve Tic. A.Ş.", "Tedarikçi", "Borç", "Ödeme", "OD-2026-0712", 6, 0, 100000, referans: "EFT"),
                // Daf Kimya ↔ Uras Holding grup (M06 ikinci örnek)
                K("Daf Kimya", "G-120-UHOLD", "Uras Holding", "Grup Şirketi", "Borç", "Grup İçi Fatura", "DAF2026200000090", 16, 30, 89000, referans: "Hizmet"),
                K("Daf Kimya", "G-320-UHOLD", "Uras Holding", "Grup Şirketi", "Alacak", "Grup İçi Fatura", "UHO2026200000311", 14, 30, 233000, referans: "Yönetim hizmet bedeli"),
                K("Uras Holding", "G-320-DAF", "Daf Kimya", "Grup Şirketi", "Alacak", "Grup İçi Fatura", "DAF2026200000090", 16, 30, 89000, referans: "Hizmet"),
                K("Uras Holding", "G-120-DAF", "Daf Kimya", "Grup Şirketi", "Borç", "Grup İçi Fatura", "UHO2026200000311", 14, 30, 233000, referans: "Yönetim hizmet bedeli"),
                // kapalı örnek (V04)
                K("Uras Kimya", "M-120-0031", "Marmara Plastik San. A.Ş.", "Müşteri", "Borç", "Satış Faturası", "SF-2026-099", 70, 60, 52000, 52000, referans: "SAS-2026-M-090")
            };
            l.Last().Durum = "Kapalı";
            return l;
        }
        public static MhAcikKalem KalemBul(int id) => AcikKalemler.FirstOrDefault(k => k.Id == id);
        /// <summary>Aktif / rezerve mahsup toplamı (iptal, red, ters kayıt hariç; taslak ve onay süreci rezervasyon, §3.4 / V14).</summary>
        public static decimal Rezerve(int kalemId, int haricId = 0) => Islemler.Where(i => i.Id != haricId && i.Aktif).SelectMany(i => i.Kalemler).Where(k => k.KalemId == kalemId).Sum(k => k.MahsupTutari);
        /// <summary>Açık Kalemleri Getir (§4.3 / §4.4): şirket, kapsam / karşı şirket, cari ve türe göre; kapalı belgeler opsiyonla.</summary>
        public static IEnumerable<object> AcikKalemListesi(string sirket, string karsiSirket, string cari, string tur, string yon, string ara, bool kapali, int haricId)
        {
            ara = (ara ?? "").Trim().ToLower(Tr);
            return AcikKalemler.Where(k => k.Sirket == sirket).Where(k => kapali || k.Durum == "Açık")
                .Where(k => string.IsNullOrEmpty(karsiSirket) ? true : (k.CariTip == "Grup Şirketi" && k.CariAd == karsiSirket))
                .Where(k => string.IsNullOrEmpty(cari) || k.CariKod == cari || k.CariAd == cari)
                .Where(k => string.IsNullOrEmpty(tur) || TurUygun(tur, k)).Where(k => string.IsNullOrEmpty(yon) || k.Yon == yon)
                .Where(k => ara == "" || (k.BelgeNo + " " + k.CariAd + " " + k.CariKod + " " + k.Referans + " " + k.BelgeTuru).ToLower(Tr).Contains(ara))
                .OrderBy(k => k.Yon == "Borç" ? 0 : 1).ThenBy(k => k.Vade).Select(k => { var rz = Rezerve(k.Id, haricId); var acik = Math.Max(0, k.OrijinalTutar - k.Kapanan - rz); var e = EslemeBul(k.Sirket, tur, k.HesapRolu, k.ParaBirimi, DateTime.Today); return new { id = k.Id, sirket = k.Sirket, cariKod = k.CariKod, cariAd = k.CariAd, cariTip = k.CariTip, yon = k.Yon, belgeTuru = k.BelgeTuru, belgeNo = k.BelgeNo, docEntry = k.DocEntry, tarih = k.Tarih.ToString("yyyy-MM-dd"), tarihTr = k.Tarih.ToString("dd.MM.yyyy"), vade = k.Vade.ToString("yyyy-MM-dd"), vadeTr = k.Vade.ToString("dd.MM.yyyy"), vadeGecti = k.Vade < DateTime.Today && k.Vade != k.Tarih, paraBirimi = k.ParaBirimi, kur = k.Kur, orijinalTutar = k.OrijinalTutar, kapanan = k.Kapanan, rezerve = rz, acikTutar = acik, tlKarsiligi = Math.Round(acik * k.Kur, 2), proje = k.Proje, masrafMerkezi = k.MasrafMerkezi, referans = k.Referans, durum = k.Durum, hesapRolu = k.HesapRolu, hesap = e == null ? "—" : Hesap(e, CariKisa(k)), kilitli = k.Durum != "Açık" || acik <= 0, kilitNedeni = k.Durum != "Açık" ? k.Durum + " belge (V04)" : acik <= 0 ? "Açık tutar yok / tamamen rezerve (V04)" : "" }; });
        }
        private static bool TurUygun(string tur, MhAcikKalem k) => tur switch
        {
            "M03" => k.CariTip == "Tedarikçi" && (k.BelgeTuru == "Verilen Avans" || k.BelgeTuru == "Alış Faturası"),
            "M04" => k.CariTip == "Müşteri" && (k.BelgeTuru == "Alınan Avans" || k.BelgeTuru == "Satış Faturası"),
            "M05" => k.BelgeTuru.Contains("İade") || k.BelgeTuru.Contains("Fatura"),
            "M06" => k.CariTip == "Grup Şirketi",
            "M02" => k.CariTip != "Grup Şirketi",
            _ => true
        };
        public static IEnumerable<object> Cariler(string sirket) => AcikKalemler.Where(k => string.IsNullOrEmpty(sirket) || k.Sirket == sirket).GroupBy(k => new { k.Sirket, k.CariAd }).Select(g => new { sirket = g.Key.Sirket, ad = g.Key.CariAd, kodlar = g.Select(x => x.CariKod).Distinct(), tipler = g.Select(x => x.CariTip).Distinct(), acik = g.Count(x => x.Durum == "Açık") });

        // ---- kayıtlar ----
        private static readonly List<MhIslem> _islemler = new List<MhIslem>(); private static bool _tohumlandi; private static int _sonId = 124;
        public static List<MhIslem> Islemler { get { Tohumla(); return _islemler; } }
        public static MhIslem Bul(string no) => Islemler.FirstOrDefault(i => string.Equals(i.No, no, StringComparison.OrdinalIgnoreCase));
        public static MhIslem Bul(int id) => Islemler.FirstOrDefault(i => i.Id == id);
        private static string N(decimal d, int b = 2) => d.ToString("N" + b, Tr); private static string P(decimal d) => d.ToString("0.##", CultureInfo.InvariantCulture);
        private static void Log(MhIslem i, DateTime z, string k, string alan, string eski, string yeni, string kaynak = "Ekran", string gerekce = null) => i.Audit.Add(new YnAudit { Zaman = z, Kullanici = k, Alan = alan, Eski = eski ?? "", Yeni = yeni ?? "", Kaynak = kaynak, Gerekce = gerekce ?? "" });

        // =====================================================================
        // KALEM KURMA ve DAĞILIM MOTORU (§3.2, §4.5, §4.6)
        // =====================================================================
        public static MhKalem KalemKur(MhKalemGiris g, string tur, int haricId, YnSonuc s)
        {
            var k = KalemBul(g.KalemId); if (k == null) { s.Hatalar.Add($"Açık kalem bulunamadı ({g.KalemId})."); return null; }
            var e = EslemeBul(k.Sirket, tur, k.HesapRolu, k.ParaBirimi, DateTime.Today);
            return new MhKalem { KalemId = k.Id, Yon = k.Yon, Sirket = k.Sirket, CariKod = k.CariKod, CariAd = k.CariAd, CariTip = k.CariTip, BelgeTuru = k.BelgeTuru, BelgeNo = k.BelgeNo, DocEntry = k.DocEntry, Tarih = k.Tarih, Vade = k.Vade, ParaBirimi = k.ParaBirimi, BelgeKuru = k.Kur, OrijinalTutar = k.OrijinalTutar, Kapanan = k.Kapanan, Rezerve = Rezerve(k.Id, haricId), MahsupTutari = Math.Round(g.MahsupTutari, 2), Proje = k.Proje, MasrafMerkezi = k.MasrafMerkezi, Referans = k.Referans, HesapRolu = k.HesapRolu, Hesap = e == null ? null : Hesap(e, CariKisa(k)) };
        }
        /// <summary>Otomatik eşleştirme (§4.6): yönteme göre sıralar, borç / alacak kalemlerini greedy dağıtır; tutarları ve dağılımı döner (ön izleme; kullanıcı onaylamadan kalıcı olmaz).</summary>
        public static void OtomatikEslestir(MhIslem i, string yontem)
        {
            IEnumerable<MhKalem> Sirala(IEnumerable<MhKalem> l) => yontem == "En Eski Belge Önce" ? l.OrderBy(k => k.Tarih) : yontem == "Aynı Tutarları Eşleştir" ? l.OrderByDescending(k => k.AcikTutar) : yontem == "Referans Bazlı" ? l.OrderBy(k => k.Referans).ThenBy(k => k.Vade) : l.OrderBy(k => k.Vade);
            var borc = Sirala(i.Borclar).ToList(); var alacak = Sirala(i.Alacaklar).ToList();
            foreach (var k in i.Kalemler) k.MahsupTutari = 0; i.Dagilimlar.Clear();
            if (yontem == "Aynı Tutarları Eşleştir") { foreach (var b in borc) { var a = alacak.FirstOrDefault(x => x.MahsupTutari == 0 && Math.Abs(x.AcikTutar - b.AcikTutar) < 0.005m); if (a != null) { b.MahsupTutari = a.MahsupTutari = b.AcikTutar; } } }
            if (yontem == "Referans Bazlı") { foreach (var b in borc.Where(x => x.MahsupTutari == 0)) foreach (var a in alacak.Where(x => x.Referans == b.Referans && x.MahsupTutari < x.AcikTutar)) { var t = Math.Min(b.AcikTutar - b.MahsupTutari, a.AcikTutar - a.MahsupTutari); if (t <= 0) continue; b.MahsupTutari += t; a.MahsupTutari += t; } }
            // kalan: greedy (en küçük tarafın toplamına kadar)
            decimal bt = Math.Min(borc.Sum(k => k.AcikTutar), alacak.Sum(k => k.AcikTutar)), bk = borc.Sum(k => k.MahsupTutari), ak = alacak.Sum(k => k.MahsupTutari);
            foreach (var b in borc) { if (bk >= bt) break; var t = Math.Min(b.AcikTutar - b.MahsupTutari, bt - bk); if (t > 0) { b.MahsupTutari += t; bk += t; } }
            foreach (var a in alacak) { if (ak >= bt) break; var t = Math.Min(a.AcikTutar - a.MahsupTutari, bt - ak); if (t > 0) { a.MahsupTutari += t; ak += t; } }
            foreach (var k in i.Kalemler) k.MahsupTutari = Math.Round(k.MahsupTutari, 2);
            i.EslestirmeYontemi = yontem; DagilimKur(i, null);
        }
        /// <summary>Belge → belge dağılımı: verilen dağılım varsa aynen (manuel, N-N), yoksa kalem tutarlarından FIFO kurulur.</summary>
        public static void DagilimKur(MhIslem i, List<MhDagilimGiris> manuel)
        {
            var l = new List<MhDagilim>(); int n = 0;
            MhDagilim D(MhKalem b, MhKalem a, decimal t) => new MhDagilim { Id = $"A-{++n:000}", BorcKalemId = b.KalemId, BorcBelge = b.BelgeNo, AlacakKalemId = a.KalemId, AlacakBelge = a.BelgeNo, Tutar = Math.Round(t, 2), ParaBirimi = i.ParaBirimi, Kur = i.Kur, TutarTl = Math.Round(t * i.Kur, 2), KurFarki = Math.Round(t * (b.BelgeKuru - a.BelgeKuru), 2) };
            if (manuel != null && manuel.Count > 0)
            {
                foreach (var m in manuel.Where(m => m.Tutar > 0)) { var b = i.Kalemler.FirstOrDefault(k => k.KalemId == m.BorcKalemId && k.Yon == "Borç"); var a = i.Kalemler.FirstOrDefault(k => k.KalemId == m.AlacakKalemId && k.Yon == "Alacak"); if (b != null && a != null) l.Add(D(b, a, m.Tutar)); }
                foreach (var k in i.Kalemler) k.MahsupTutari = Math.Round(l.Where(d => (k.Yon == "Borç" ? d.BorcKalemId : d.AlacakKalemId) == k.KalemId).Sum(d => d.Tutar), 2);   // manuel dağılımda kalem tutarı dağılımdan türer
            }
            else
            {
                var borc = i.Borclar.Where(k => k.MahsupTutari > 0).Select(k => (k, kalan: k.MahsupTutari)).ToList(); var alacak = i.Alacaklar.Where(k => k.MahsupTutari > 0).Select(k => (k, kalan: k.MahsupTutari)).ToList();
                int bi = 0, ai = 0;
                while (bi < borc.Count && ai < alacak.Count) { var t = Math.Min(borc[bi].kalan, alacak[ai].kalan); if (t > 0) l.Add(D(borc[bi].k, alacak[ai].k, t)); borc[bi] = (borc[bi].k, borc[bi].kalan - t); alacak[ai] = (alacak[ai].k, alacak[ai].kalan - t); if (borc[bi].kalan <= 0.005m) bi++; if (alacak[ai].kalan <= 0.005m) ai++; }
            }
            i.Dagilimlar = l;
        }

        // =====================================================================
        // DOĞRULAMA (§13 V01–V15)
        // =====================================================================
        public static YnSonuc Dogrula(MhIslem i, string rol, bool taslak)
        {
            var s = new YnSonuc();
            if (string.IsNullOrEmpty(i.Sirket)) s.Hatalar.Add("Şirket zorunludur.");
            if (!i.Borclar.Any(k => k.MahsupTutari > 0) || !i.Alacaklar.Any(k => k.MahsupTutari > 0)) s.Hatalar.Add("V01 En az bir borç ve bir alacak açık kalemi seçilmelidir.");
            foreach (var k in i.Kalemler)
            {
                if (k.MahsupTutari <= 0) s.Hatalar.Add($"V02 {k.BelgeNo}: mahsup tutarı sıfırdan büyük olmalıdır.");
                if (k.MahsupTutari > k.AcikTutar + 0.005m) s.Hatalar.Add($"V02/V14 {k.BelgeNo}: mahsup tutarı ({N(k.MahsupTutari)}) güncel açık tutarı ({N(k.AcikTutar)}; orijinal {N(k.OrijinalTutar)} − kapanan {N(k.Kapanan)} − rezerve {N(k.Rezerve)}) aşamaz.");
                var ak = KalemBul(k.KalemId); if (ak != null && ak.Durum != "Açık") s.Hatalar.Add($"V04 {k.BelgeNo}: {ak.Durum} belge seçilemez.");
                if (k.Hesap == null) s.Hatalar.Add($"V10 {k.BelgeNo}: '{k.HesapRolu}' hesap rolü için eşleme kaydı yok; işlem bloklanır.");
            }
            var pbs = i.Kalemler.Select(k => k.ParaBirimi).Distinct().ToList();
            if (pbs.Count > 1) { if (Parametreler["caprazDoviz"] == 1 && Yetkili(rol, "caprazDoviz")) s.Uyarilar.Add("V09 Farklı para birimli mahsup: çapraz kur ve kur kaynağı zorunlu (yetkiyle açıldı)."); else s.Hatalar.Add("V09 Farklı para birimli açık kalemler aynı mahsupta birleştirilemez (parametre / yetki yok)."); }
            decimal fark = Math.Abs(i.Fark);
            if (fark > 0.005m) { if (fark <= Parametreler["yuvarlamaTolerans"]) s.Uyarilar.Add($"Borç / alacak farkı {N(fark)} {i.ParaBirimi} yuvarlama toleransı içinde; yuvarlama hesabına alınır."); else s.Hatalar.Add($"V03 Borç tarafı mahsup toplamı ({N(i.BorcToplam)}) ile alacak tarafı ({N(i.AlacakToplam)}) eşit değil; fark {N(fark)} {i.ParaBirimi}. Tutarları eşitleyin."); }
            if (i.Dagilimlar.Count > 0) { foreach (var k in i.Kalemler.Where(k => k.MahsupTutari > 0)) { var d = i.Dagilimlar.Where(x => (k.Yon == "Borç" ? x.BorcKalemId : x.AlacakKalemId) == k.KalemId).Sum(x => x.Tutar); if (Math.Abs(d - k.MahsupTutari) > 0.005m) s.Hatalar.Add($"{k.BelgeNo}: dağılım toplamı ({N(d)}) kalem mahsup tutarına ({N(k.MahsupTutari)}) eşit değil."); } }
            if (i.ParaBirimi != "TRY" && (i.Kur <= 0 || !i.KurTarihi.HasValue)) s.Hatalar.Add("V08 Dövizli işlemde kur ve kur tarihi boş olamaz.");
            if (i.KayitTarihi < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1)) s.Hatalar.Add("V07 Muhasebe (kayıt) tarihi kapalı mali dönemde; açık dönem olmalıdır.");
            if (i.Tur == "M06") { if (string.IsNullOrEmpty(i.KarsiSirket) || i.KarsiSirket == i.Sirket) s.Hatalar.Add("V12 Grup şirketleri arası mahsupta karşı şirket zorunludur."); if (i.Kalemler.Any(k => k.CariTip != "Grup Şirketi" || k.CariAd != i.KarsiSirket)) s.Hatalar.Add("V12 Grup şirketleri arası mahsupta tüm kalemler karşı şirket carisine ait olmalıdır."); if (EslemeBul(i.KarsiSirket, i.Tur, "Grup Şirketi Alacak", i.ParaBirimi, i.Tarih) == null || EslemeBul(i.KarsiSirket, i.Tur, "Grup Şirketi Borç", i.ParaBirimi, i.Tarih) == null) s.Hatalar.Add("V12 Karşı şirket mirror hesap eşlemesi eksik."); }
            else if (i.Kapsam == "Grup Şirketleri Arası") s.Uyarilar.Add("Kapsam grup şirketleri arası seçildi ama mahsup türü M06 değil.");
            if (i.Tur == "M01" && i.Kalemler.Select(k => k.CariKod).Distinct().Count() > 1) s.Uyarilar.Add("M01 aynı cari iç mutabakat: birden fazla cari kodu seçildi; tür M02 olmalı olabilir.");
            if (i.Tur == "M03" && !i.Kalemler.Any(k => k.BelgeTuru == "Verilen Avans")) s.Uyarilar.Add("M03 tedarikçi avans-fatura: verilen avans kalemi seçilmedi.");
            if (i.Tur == "M04" && !i.Kalemler.Any(k => k.BelgeTuru == "Alınan Avans")) s.Uyarilar.Add("M04 müşteri avans-fatura: alınan avans kalemi seçilmedi.");
            if (i.Kalemler.Any(k => k.BelgeTuru.Contains("KDV"))) s.Hatalar.Add("V15 Mahsup KDV satırı üretemez.");
            if (!taslak) { if (string.IsNullOrWhiteSpace(i.Aciklama)) s.Hatalar.Add("Açıklama zorunludur."); if (string.IsNullOrWhiteSpace(i.NedenKodu)) s.Hatalar.Add("Mahsup nedeni (kod + metin) zorunludur."); }
            decimal kf = Math.Abs(KurFarkiHesapla(i)); if (kf > Parametreler["kurFarkiUst"]) s.Uyarilar.Add($"Kur farkı {N(kf)} TL üst sınırı aşıyor; Direktör onayı ve fark kabul yetkisi gerekir."); else if (kf > 0.005m) s.Uyarilar.Add($"Dövizli mahsup: {N(kf)} TL kur farkı oluşacak (simülasyonda gösterilir).");
            if (i.MahsupTutari * i.Kur > Parametreler["direktorEsik"]) s.Uyarilar.Add($"Mahsup tutarı {N(i.MahsupTutari * i.Kur, 0)} TL eşiği aşıyor; Direktör onayı eklenir.");
            return s;
        }
        /// <summary>Kur farkı (TL): kapanan kalemlerin belge kuru ile TL karşılıkları arasındaki fark (borç kalemler − alacak kalemler).</summary>
        public static decimal KurFarkiHesapla(MhIslem i) => i.ParaBirimi == "TRY" ? 0 : Math.Round(i.Alacaklar.Sum(k => k.MahsupTutari * k.BelgeKuru) - i.Borclar.Sum(k => k.MahsupTutari * k.BelgeKuru), 2);
        public static List<YnOnay> OnayAkisi(MhIslem i)
        {
            var l = new List<YnOnay>(); var muh = NakitAvansOrnek.Personel.FirstOrDefault(p => p.Rol == "Muhasebe" && p.Sirket == i.Sirket) ?? NakitAvansOrnek.Personel.First(p => p.Rol == "Muhasebe");
            l.Add(new YnOnay { Sira = 1, Adim = "Muhasebe Onayı", Kisi = muh.AdSoyad });
            if (i.MahsupTutari * i.Kur > Parametreler["direktorEsik"] || i.Tur == "M06" || i.Tur == "M07" || Math.Abs(KurFarkiHesapla(i)) > Parametreler["kurFarkiUst"]) l.Add(new YnOnay { Sira = 2, Adim = "Direktör Onayı (Mali İşler)", Kisi = NakitAvansOrnek.UstYonetim(muh).AdSoyad });
            l.Add(new YnOnay { Sira = l.Count + 1, Adim = "Finans Onayı", Kisi = NakitAvansOrnek.Finans().AdSoyad }); return l;
        }
        public static string OnayNedeni(MhIslem i) { var n = new List<string>(); if (i.MahsupTutari * i.Kur > Parametreler["direktorEsik"]) n.Add("tutar eşiği"); if (i.Tur == "M06") n.Add("grup şirketleri arası"); if (i.Tur == "M07") n.Add("özel mahsup"); if (Math.Abs(KurFarkiHesapla(i)) > Parametreler["kurFarkiUst"]) n.Add("kur farkı üst sınırı"); return n.Count == 0 ? "standart akış (Muhasebe → Finans)" : "Direktör: " + string.Join(", ", n); }

        // =====================================================================
        // SİMÜLASYON (§5): iç mutabakat / yevmiye + mutabakat; kur ve yuvarlama farkı; M06 mirror
        // =====================================================================
        public static (List<MhFisSatir> Satirlar, string Tip, YnSonuc Sonuc) Simule(MhIslem i)
        {
            var s = new YnSonuc(); var l = new List<MhFisSatir>(); string refr = $"{i.No ?? "TASLAK"}{(i.GrupMahsupNo != null ? " · " + i.GrupMahsupNo : "")}";
            if (!i.Kalemler.Any(k => k.MahsupTutari > 0)) { s.Hatalar.Add("Simülasyon için seçili kalem ve tutar gerekir."); return (l, "—", s); }
            MhFisSatir F(string sirket, string tip, string rol, string hesap, string ad, string cari, decimal borc, decimal alacak, decimal doviz, decimal kur, string acik, string kural, string prj = null, string mm = null) => new MhFisSatir { Sirket = sirket, Tip = tip, HesapRolu = rol, HesapKodu = hesap, HesapAdi = ad, Cari = cari, Borc = Math.Round(borc, 2), Alacak = Math.Round(alacak, 2), ParaBirimi = i.ParaBirimi, Kur = kur, DovizTutar = Math.Round(doviz, 2), Proje = prj ?? i.Proje, MasrafMerkezi = mm ?? i.MasrafMerkezi, Aciklama = acik, Kural = kural, Referans = refr };
            // Aynı hesap / aynı cari ise yalnız iç mutabakat (yeni yevmiye HAYIR)
            var hesaplar = i.Kalemler.Where(k => k.MahsupTutari > 0).Select(k => k.Hesap).Distinct().ToList();
            bool icMutabakat = hesaplar.Count == 1 && i.Kalemler.Select(k => k.CariKod).Distinct().Count() == 1 && i.ParaBirimi == "TRY";
            foreach (var k in i.Kalemler.Where(k => k.MahsupTutari > 0)) l.Add(new MhFisSatir { Sirket = i.Sirket, Tip = "MUTABAKAT", HesapRolu = k.HesapRolu, HesapKodu = k.Hesap ?? "—", HesapAdi = $"{k.BelgeTuru} {k.BelgeNo}", Cari = k.CariAd, Borc = k.Yon == "Alacak" ? Math.Round(k.MahsupTutari * i.Kur, 2) : 0, Alacak = k.Yon == "Borç" ? Math.Round(k.MahsupTutari * i.Kur, 2) : 0, ParaBirimi = i.ParaBirimi, Kur = i.Kur, DovizTutar = k.MahsupTutari, Proje = k.Proje, MasrafMerkezi = k.MasrafMerkezi, Aciklama = $"Açık kalem kapama · kalan {N(k.SonrasiKalan)} {k.ParaBirimi}", Kural = "İç mutabakat (açık kalem)", Referans = refr });
            string tip;
            if (icMutabakat) tip = "İç Mutabakat";
            else
            {
                tip = "Yevmiye + Mutabakat";
                // yevmiye: alacak bakiyeli kalemler kapanır → Borç; borç bakiyeli kalemler kapanır → Alacak (belge kuruyla TL)
                foreach (var g in i.Kalemler.Where(k => k.MahsupTutari > 0).GroupBy(k => new { k.Yon, k.HesapRolu, k.Hesap, k.CariAd }))
                {
                    var e = EslemeBul(i.Sirket, i.Tur, g.Key.HesapRolu, i.ParaBirimi, i.Tarih); if (e == null) { s.Hatalar.Add($"V10 '{g.Key.HesapRolu}' için hesap eşlemesi yok."); continue; }
                    decimal doviz = g.Sum(k => k.MahsupTutari), tl = Math.Round(g.Sum(k => k.MahsupTutari * k.BelgeKuru), 2);
                    l.Add(F(i.Sirket, "YEVMIYE", g.Key.HesapRolu, g.Key.Hesap, e.HesapAdi, g.Key.CariAd, g.Key.Yon == "Alacak" ? tl : 0, g.Key.Yon == "Borç" ? tl : 0, doviz, i.Kur, g.Key.Yon == "Alacak" ? $"{g.Key.CariAd} borcunun kapatılması ({string.Join(", ", g.Select(k => k.BelgeNo))})" : $"{g.Key.CariAd} alacağının kapatılması ({string.Join(", ", g.Select(k => k.BelgeNo))})", $"Eşleme #{e.Id} · {e.BpMode}", g.First().Proje, g.First().MasrafMerkezi));
                }
                // kur farkı / yuvarlama (yalnız yevmiye satırları dengeye getirilir)
                decimal fark = Math.Round(l.Where(x => x.Tip == "YEVMIYE").Sum(x => x.Borc) - l.Where(x => x.Tip == "YEVMIYE").Sum(x => x.Alacak), 2);
                if (Math.Abs(fark) > 0.005m)
                {
                    bool yuv = Math.Abs(fark) <= Parametreler["yuvarlamaTolerans"] || i.ParaBirimi == "TRY";
                    string rol = yuv ? "Yuvarlama Farkı" : fark > 0 ? "Kur Farkı Geliri" : "Kur Farkı Gideri"; var e = EslemeBul(i.Sirket, i.Tur, rol, i.ParaBirimi, i.Tarih);
                    if (e == null) s.Hatalar.Add($"V10 '{rol}' hesabı eşleme tablosunda yok."); else l.Add(F(i.Sirket, yuv ? "YUV" : "FX", rol, e.Hesap, e.HesapAdi, "", fark < 0 ? -fark : 0, fark > 0 ? fark : 0, 0, i.Kur, yuv ? "Tolerans içi yuvarlama farkı" : (fark > 0 ? "Kambiyo kârı: alacak kalemler daha yüksek kurla kayıtlı" : "Kambiyo zararı: borç kalemler daha yüksek kurla kayıtlı"), $"Eşleme #{e.Id}"));
                    if (!yuv && i.ParaBirimi != "TRY" && Math.Abs(fark) > Parametreler["yuvarlamaTolerans"] && Math.Abs(fark) > Parametreler["kurFarkiUst"]) s.Uyarilar.Add($"Kur farkı {N(Math.Abs(fark))} TL üst sınırın üstünde.");
                    if (yuv && i.ParaBirimi == "TRY" && Math.Abs(fark) > Parametreler["yuvarlamaTolerans"]) s.Hatalar.Add($"V11 Yuvarlama farkı {N(Math.Abs(fark))} TL toleransı ({N(Parametreler["yuvarlamaTolerans"])} TL) aşıyor; manuel fark kapama yapılamaz.");
                }
            }
            // M06 mirror: karşı şirket tarafında bağlantılı kayıt (aynı Grup Mahsup ID)
            if (i.Tur == "M06" && !string.IsNullOrEmpty(i.KarsiSirket))
            {
                var eA = EslemeBul(i.KarsiSirket, i.Tur, "Grup Şirketi Alacak", i.ParaBirimi, i.Tarih); var eB = EslemeBul(i.KarsiSirket, i.Tur, "Grup Şirketi Borç", i.ParaBirimi, i.Tarih); decimal t = Math.Round(i.MahsupTutari * i.Kur, 2);
                if (eA != null && eB != null) { l.Add(F(i.KarsiSirket, "MIRROR", "Grup Şirketi Borç", Hesap(eB, Kisa(i.Sirket)), eB.HesapAdi, i.Sirket, t, 0, i.MahsupTutari, i.Kur, $"{i.Sirket} borç carisinin kapatılması (mirror)", $"Eşleme #{eB.Id} · Grup Mahsup {i.GrupMahsupNo}")); l.Add(F(i.KarsiSirket, "MIRROR", "Grup Şirketi Alacak", Hesap(eA, Kisa(i.Sirket)), eA.HesapAdi, i.Sirket, 0, t, i.MahsupTutari, i.Kur, $"{i.Sirket} alacak carisinin kapatılması (mirror)", $"Eşleme #{eA.Id} · Grup Mahsup {i.GrupMahsupNo}")); tip += " (iki şirket mirror)"; }
            }
            foreach (var g in l.Where(x => x.Tip != "MUTABAKAT").GroupBy(x => x.Sirket)) { decimal b = g.Sum(x => x.Borc), a = g.Sum(x => x.Alacak); if (Math.Abs(b - a) > 0.01m) s.Hatalar.Add($"{g.Key}: yevmiye borç ({N(b)}) ≠ alacak ({N(a)})."); }
            return (l, tip, s);
        }

        // =====================================================================
        // KAYDET / ONAY
        // =====================================================================
        public static (MhIslem Islem, YnSonuc Sonuc) Kaydet(MhIslemGiris g, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var s = new YnSonuc(); var i = g.Id > 0 ? Bul(g.Id) : null; if (g.Id > 0 && i == null) { s.Hatalar.Add("İşlem bulunamadı."); return (null, s); }
                if (i != null && !i.Duzenlenebilir) { s.Hatalar.Add($"V06 {i.Durum} durumundaki işlem düzenlenemez; onay düşürülmeli (geri çek) ya da iptal edilmelidir."); return (i, s); }
                bool yeni = i == null; if (yeni) { i = new MhIslem { Id = ++_sonId, No = $"MHS-{_sonId:000000}", Olusturan = kullanici, OlusturmaZamani = z }; }
                var eskiToplam = i.MahsupTutari; var eskiKalem = i.Kalemler.ToDictionary(k => k.KalemId);
                i.Tarih = DateTime.TryParseExact(g.Tarih ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : (i.Tarih == default ? z.Date : i.Tarih);
                i.KayitTarihi = DateTime.TryParseExact(g.KayitTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var kt) ? kt : i.Tarih;
                i.Sirket = g.Sirket; i.Tur = string.IsNullOrEmpty(g.Tur) ? "M01" : g.Tur; i.Kapsam = i.Tur == "M06" ? "Grup Şirketleri Arası" : (g.Kapsam ?? "Şirket İçi"); i.KarsiSirket = i.Tur == "M06" ? g.KarsiSirket : null;
                if (i.Tur == "M06" && i.GrupMahsupNo == null) i.GrupMahsupNo = $"GM-{z:yyyy}-{i.Id:00000}";
                i.BorcCari = g.BorcCari; i.AlacakCari = g.AlacakCari; i.MasrafMerkezi = (g.MasrafMerkezi ?? "").Trim(); i.Proje = (g.Proje ?? "").Trim(); i.Departman = (g.Departman ?? "").Trim(); i.NedenKodu = (g.NedenKodu ?? "").Trim(); i.Neden = (g.Neden ?? "").Trim(); i.Aciklama = (g.Aciklama ?? "").Trim(); i.EslestirmeYontemi = string.IsNullOrEmpty(g.EslestirmeYontemi) ? "Manuel" : g.EslestirmeYontemi;
                i.Kalemler = (g.Kalemler ?? new List<MhKalemGiris>()).Select(k => KalemKur(k, i.Tur, i.Id, s)).Where(k => k != null).GroupBy(k => k.KalemId).Select(x => x.First()).ToList();
                var ilk = i.Kalemler.FirstOrDefault(); i.ParaBirimi = ilk?.ParaBirimi ?? (g.ParaBirimi ?? "TRY"); i.Kur = i.ParaBirimi == "TRY" ? 1 : (g.Kur ?? ilk?.BelgeKuru ?? 1); i.KurTarihi = i.ParaBirimi == "TRY" ? (DateTime?)null : (DateTime.TryParseExact(g.KurTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var kd) ? kd : i.Tarih);
                if (string.IsNullOrEmpty(i.BorcCari)) i.BorcCari = i.Borclar.FirstOrDefault()?.CariAd; if (string.IsNullOrEmpty(i.AlacakCari)) i.AlacakCari = i.Alacaklar.FirstOrDefault()?.CariAd;
                DagilimKur(i, g.Dagilimlar);
                if (!s.Gecerli) return (yeni ? null : i, s);
                var d = Dogrula(i, g.Rol, true); s.Uyarilar.AddRange(d.Uyarilar); s.Hatalar.AddRange(d.Hatalar.Where(h => h.StartsWith("V02/V14") || h.StartsWith("V04") || h.StartsWith("V09") || h.StartsWith("V15")));
                if (!s.Gecerli) return (yeni ? null : i, s);
                if (yeni) Log(i, z, kullanici, "STATUS", "", "Taslak", "Sistem");
                else { if (eskiToplam != i.MahsupTutari) Log(i, z, kullanici, "OFFSET_AMOUNT", P(eskiToplam), P(i.MahsupTutari), "Ekran", g.Gerekce); foreach (var k in i.Kalemler) { if (!eskiKalem.TryGetValue(k.KalemId, out var e)) Log(i, z, kullanici, "ITEM_ADDED", "", $"{k.BelgeNo} {P(k.MahsupTutari)}"); else if (e.MahsupTutari != k.MahsupTutari) Log(i, z, kullanici, $"ITEM_AMOUNT[{k.BelgeNo}]", P(e.MahsupTutari), P(k.MahsupTutari), "Ekran", g.Gerekce); } foreach (var e in eskiKalem.Values.Where(e => i.Kalemler.All(k => k.KalemId != e.KalemId))) Log(i, z, kullanici, "ITEM_REMOVED", $"{e.BelgeNo} {P(e.MahsupTutari)}", ""); if (i.Durum == "Reddedildi") { i.Durum = "Taslak"; i.Versiyon++; i.Onaylar.Clear(); i.RedNedeni = null; Log(i, z, kullanici, "STATUS", "Reddedildi", $"Taslak (revize, v{i.Versiyon})"); } }
                Log(i, z, kullanici, "ALLOCATION", "", $"{i.EslestirmeYontemi} · {i.Dagilimlar.Count} dağılım · rezerve {N(i.BorcToplam)} / {N(i.AlacakToplam)} {i.ParaBirimi}", "Sistem");
                i.Guncelleyen = kullanici; i.GuncellemeZamani = z; if (yeni) _islemler.Insert(0, i); return (i, s);
            }
        }
        public static YnSonuc OnayaGonder(MhIslem i, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var s = Dogrula(i, rol, false); if (i.Durum != "Taslak") s.Hatalar.Insert(0, $"Yalnız Taslak durumundaki işlem onaya gönderilebilir ({i.Durum}).");
                foreach (var k in i.Kalemler) { k.Rezerve = Rezerve(k.KalemId, i.Id); if (k.MahsupTutari > k.AcikTutar + 0.005m) s.Hatalar.Add($"V05/V14 {k.BelgeNo}: ERP'den yeniden okunan açık tutar {N(k.AcikTutar)}; talep {N(k.MahsupTutari)}."); }
                var (sim, tip, ss) = Simule(i); s.Hatalar.AddRange(ss.Hatalar); s.Uyarilar.AddRange(ss.Uyarilar); if (!s.Gecerli) return s;
                i.Simulasyon = sim; i.SimulasyonTipi = tip; i.SimulasyonVersiyonu++; i.SimulasyonTarihi = z; i.SimulasyonHash = TemlikOrnek.Sha(string.Join("|", sim.Select(x => x.Sirket + x.HesapKodu + x.Borc + x.Alacak)));
                i.Onaylar = OnayAkisi(i); i.Durum = "Onay Bekliyor"; i.Guncelleyen = kullanici; i.GuncellemeZamani = z;
                Log(i, z, kullanici, "STATUS", "Taslak", "Onay Bekliyor", "Workflow", OnayNedeni(i)); Log(i, z, kullanici, "SIM_SNAPSHOT", "", $"v{i.SimulasyonVersiyonu} · {tip} · {sim.Count} satır · hash {i.SimulasyonHash}", "Sistem"); return s;
            }
        }
        public static string Onayla(MhIslem i, string eylem, string not, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "onay")) return "Bu rol onay veremez."; if (i.Durum != "Onay Bekliyor") return $"İşlem onay sürecinde değil ({i.Durum})."; var adim = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor"); if (adim == null) return "Bekleyen adım yok.";
                if (eylem == "Reddet") { if (string.IsNullOrWhiteSpace(not)) return "Ret nedeni zorunludur."; adim.Durum = "Reddedildi"; adim.Tarih = z; adim.Not = not; i.Durum = "Reddedildi"; i.RedNedeni = not; Log(i, z, kullanici, "STATUS", "Onay Bekliyor", "Reddedildi", "Workflow", not); return null; }
                adim.Durum = "Onaylandı"; adim.Tarih = z; adim.Not = not ?? ""; Log(i, z, kullanici, "APPROVAL", adim.Adim, "Onaylandı", "Workflow", not); if (i.Onaylar.All(o => o.Durum == "Onaylandı")) { i.Durum = "Onaylandı"; Log(i, z, kullanici, "STATUS", "Onay Bekliyor", "Onaylandı", "Workflow"); } i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }
        public static string GeriCek(MhIslem i, string kullanici, DateTime? simdi = null) { lock (Kilit) { var z = simdi ?? DateTime.Now; if (i.Durum != "Onay Bekliyor" && i.Durum != "Onaylandı") return "Yalnız onay bekleyen / ERP'ye gitmemiş onaylı işlem geri çekilebilir."; if (i.Erp.Count > 0) return "ERP kaydı başlamış; geri çekilemez."; Log(i, z, kullanici, "STATUS", i.Durum, $"Taslak (geri çekildi, v{i.Versiyon + 1})", "Workflow"); i.Durum = "Taslak"; i.Onaylar.Clear(); i.Versiyon++; return null; } }

        // =====================================================================
        // ERP (§12): iç mutabakat / yevmiye, idempotent; M06 iki şirket bağımsız çağrı, kısmi entegrasyon ve tekrar
        // =====================================================================
        public static string Muhasebelestir(MhIslem i, string senaryo, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "muhasebelestir")) return "Bu rol muhasebeleştiremez (Muhasebe / Yönetici / Sistem Admin)."; if (!i.Onaylandi) return "Onay akışı tamamlanmadan ERP aksiyonu tetiklenemez.";
                if (i.Erp.Any(e => e.Durum == "Posted" || e.Durum == "Reconciled") && i.Erp.All(e => e.Durum != "Failed")) return $"V13 Bu Mahsup ID için aktif ERP kaydı var ({string.Join(", ", i.Erp.Select(e => e.YevmiyeNo ?? e.MutabakatNo))}); yeniden muhasebeleştirme engellendi.";
                foreach (var k in i.Kalemler) { k.Rezerve = Rezerve(k.KalemId, i.Id); if (k.MahsupTutari > k.AcikTutar + 0.005m) return $"V05 {k.BelgeNo}: açık tutar değişmiş ({N(k.AcikTutar)}); işlem yeniden onaya gönderilmeli (V06)."; }
                if (i.Simulasyon.Count == 0) return "Simülasyon snapshot'ı yok."; senaryo = senaryo ?? "uyumlu";
                var sirketler = i.Simulasyon.Select(x => x.Sirket).Distinct().ToList(); int n = 0;
                foreach (var sirket in sirketler)
                {
                    var e = i.Erp.FirstOrDefault(x => x.Sirket == sirket); if (e != null && (e.Durum == "Posted" || e.Durum == "Reconciled")) continue;   // idempotent: tamamlanan taraf tekrar gönderilmez
                    if (e == null) { e = new MhErp { Sirket = sirket, IdempotencyKey = $"{i.No}-{Kisa(sirket)}-v{i.Versiyon}", CorrelationId = $"{i.GrupMahsupNo ?? i.No}/{Kisa(sirket)}" }; i.Erp.Add(e); }
                    e.Zaman = z.AddSeconds(n * 3); e.Durum = "Processing";
                    if (senaryo == "karsiHata" && sirket == i.KarsiSirket) { e.Durum = "Failed"; e.Cevap = "SAP B1 -5002: Karşı şirket DB bağlantısı zaman aşımı (InternalReconciliations)"; Log(i, e.Zaman.Value, kullanici, $"ERP[{sirket}]", "Processing", "Failed · " + e.Cevap, "ERP", "idempotency " + e.IdempotencyKey); n++; continue; }
                    bool yevmiye = i.Simulasyon.Any(x => x.Sirket == sirket && x.Tip != "MUTABAKAT");
                    if (yevmiye) e.YevmiyeNo = $"JE-{Kisa(sirket)}-{(81000 + i.Id * 7 + n):00000}"; e.MutabakatNo = $"RC-{Kisa(sirket)}-{(4400 + i.Id * 3 + n):00000}";
                    e.Durum = yevmiye ? "Posted" : "Reconciled"; e.Cevap = $"200 OK · {(yevmiye ? "JournalEntry " + e.YevmiyeNo + " · " : "")}InternalReconciliation {e.MutabakatNo} · {e.Zaman:dd.MM.yyyy HH:mm}";
                    Log(i, e.Zaman.Value, kullanici, $"ERP[{sirket}]", "Processing", e.Durum + " · " + (e.YevmiyeNo ?? e.MutabakatNo), "ERP", $"correlation {e.CorrelationId} · idempotency {e.IdempotencyKey}");
                    GercekUret(i, sirket, senaryo, e); n++;
                }
                i.GercekTarihi = z; string eski = i.Durum; var d = i.ErpDurumu; i.Durum = d == "PartiallyCompleted" ? "Kısmi Entegrasyon" : d == "Failed" ? "Hata" : d == "Reconciled" ? "Mutabakat Oluştu" : "Muhasebeleşti";
                if (eski != i.Durum) Log(i, z, kullanici, "STATUS", eski, i.Durum, "ERP"); Kontrol(i, kullanici, z); i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }
        private static void GercekUret(MhIslem i, string sirket, string senaryo, MhErp e)
        {
            var l = i.Simulasyon.Where(x => x.Sirket == sirket).Select(x => new MhFisSatir { Sirket = x.Sirket, Tip = x.Tip, FisNo = x.Tip == "MUTABAKAT" ? e.MutabakatNo : e.YevmiyeNo, HesapRolu = x.HesapRolu, HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Cari = x.Cari, Borc = x.Borc, Alacak = x.Alacak, ParaBirimi = x.ParaBirimi, Kur = x.Kur, DovizTutar = x.DovizTutar, Proje = x.Proje, MasrafMerkezi = x.MasrafMerkezi, Aciklama = x.Aciklama, Referans = x.Referans }).ToList();
            i.ErpKalan.Clear(); foreach (var k in i.Kalemler) i.ErpKalan[k.BelgeNo] = Math.Round(Math.Max(0, k.SonrasiKalan), 2);
            switch (senaryo)
            {
                case "hesap": { var g = l.FirstOrDefault(x => x.Tip == "YEVMIYE" && x.Borc > 0); if (g != null) { g.HesapKodu = "195.05.001"; g.HesapAdi = "Mahsup Geçici / Clearing (yanlış hesap)"; } break; }
                case "tutar": { var g = l.FirstOrDefault(x => x.Tip == "YEVMIYE" && x.Borc > 0); var a = l.FirstOrDefault(x => x.Tip == "YEVMIYE" && x.Alacak > 0); if (g != null && a != null) { g.Borc = Math.Round(g.Borc * 0.98m, 2); a.Alacak = Math.Round(a.Alacak * 0.98m, 2); } break; }
                case "kur": { foreach (var x in l) x.Kur = Math.Round(x.Kur * 1.01m, 4); var fx = l.FirstOrDefault(x => x.Tip == "FX"); if (fx != null) { fx.HesapKodu = "656.01.001"; fx.HesapAdi = "Kambiyo Zararları (yanlış yön)"; } break; }
                case "acikKalem": { var k = i.Kalemler.LastOrDefault(); if (k != null) i.ErpKalan[k.BelgeNo] = i.ErpKalan[k.BelgeNo] + 1000m; break; }
                case "mutabakatYok": { l.RemoveAll(x => x.Tip == "MUTABAKAT"); e.MutabakatNo = null; break; }
                case "mukerrer": { l.AddRange(l.Where(x => x.Tip == "YEVMIYE").Select(x => new MhFisSatir { Sirket = x.Sirket, Tip = x.Tip, FisNo = (x.FisNo ?? "").Replace("JE-", "JE2-"), HesapRolu = x.HesapRolu, HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Cari = x.Cari, Borc = x.Borc, Alacak = x.Alacak, ParaBirimi = x.ParaBirimi, Kur = x.Kur, DovizTutar = x.DovizTutar, Proje = x.Proje, MasrafMerkezi = x.MasrafMerkezi, Aciklama = x.Aciklama, Referans = x.Referans }).ToList()); break; }
                case "referans": { foreach (var x in l) x.Referans = e.YevmiyeNo ?? e.MutabakatNo; break; }
                case "proje": { foreach (var x in l.Where(x => x.Tip == "YEVMIYE")) { x.Proje = ""; x.MasrafMerkezi = "MERKEZ"; } break; }
            }
            i.Gercek.RemoveAll(x => x.Sirket == sirket); i.Gercek.AddRange(l);
        }
        public static string TersKayit(MhIslem i, string neden, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "tersKayit")) return "Ters kayıt / unreconcile yalnız Muhasebe / Yönetici yetkisiyle."; if (string.IsNullOrWhiteSpace(neden)) return "Neden zorunludur.";
                var tamam = i.Erp.Where(e => e.Durum == "Posted" || e.Durum == "Reconciled").ToList(); if (tamam.Count == 0) return "ERP kaydı yok; iptal kullanın.";
                foreach (var e in tamam) { string eski = e.Durum; e.Durum = "Reversed"; e.Cevap = $"{(eski == "Posted" ? "Ters yevmiye JE-R-" + (e.YevmiyeNo ?? "").Split('-').Last() + " · " : "")}Unreconcile {e.MutabakatNo} · {z:dd.MM.yyyy HH:mm}"; Log(i, z, kullanici, $"ERP_REVERSE[{e.Sirket}]", e.YevmiyeNo ?? e.MutabakatNo, "Reversed (kayıt silinmedi; ters yevmiye + unreconcile)", "ERP", neden); }
                i.Gercek.AddRange(i.Gercek.Where(x => x.Tip != "MUTABAKAT").Select(x => new MhFisSatir { Sirket = x.Sirket, Tip = x.Tip, FisNo = "R-" + x.FisNo, HesapRolu = x.HesapRolu + " (ters)", HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Cari = x.Cari, Borc = x.Alacak, Alacak = x.Borc, ParaBirimi = x.ParaBirimi, Kur = x.Kur, DovizTutar = x.DovizTutar, Proje = x.Proje, MasrafMerkezi = x.MasrafMerkezi, Aciklama = "TERS KAYIT · " + x.Aciklama, Referans = x.Referans }).ToList());
                Log(i, z, kullanici, "STATUS", i.Durum, "Ters Kayıt", "ERP", neden); i.Durum = "Ters Kayıt"; i.IptalNedeni = neden; i.KontrolDurumu = null; i.Kontroller.Clear(); i.AcikKalemKontrol.Clear(); i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }
        public static string Iptal(MhIslem i, string neden, string rol, string kullanici, DateTime? simdi = null) { lock (Kilit) { var z = simdi ?? DateTime.Now; if (string.IsNullOrWhiteSpace(neden)) return "İptal nedeni zorunludur."; if (!i.Aktif) return "İşlem zaten kapalı."; if (i.Erp.Any(e => e.Durum == "Posted" || e.Durum == "Reconciled")) return "ERP kaydı oluşmuş: iptal yerine ters kayıt / unreconcile prosedürü çalıştırılmalıdır."; Log(i, z, kullanici, "STATUS", i.Durum, "İptal", "Ekran", neden); Log(i, z, kullanici, "RESERVATION", $"{N(i.BorcToplam)} / {N(i.AlacakToplam)} rezerve", "serbest bırakıldı", "Sistem"); i.Durum = "İptal"; i.IptalNedeni = neden; i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null; } }

        // =====================================================================
        // MUHASEBE KONTROLÜ (§7)
        // =====================================================================
        public static string Kontrol(MhIslem i, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var K = new List<YnKontrol>(); var d = new List<string>(); if (i.Erp.Count == 0) return "ERP aksiyonu başlamadan kontrol yapılamaz.";
                void Ek(string alan, string sirket, string b, string g, string sonuc, bool kritik = true) { K.Add(new YnKontrol { Alan = alan, Sirket = sirket, Beklenen = b, Gerceklesen = g, Sonuc = sonuc, Kritik = kritik }); if (sonuc != "Uyumlu") d.Add(sonuc); }
                string T(decimal v) => v.ToString("N2", Tr);
                foreach (var sirket in i.Simulasyon.Select(x => x.Sirket).Distinct())
                {
                    var e = i.Erp.FirstOrDefault(x => x.Sirket == sirket); var b = i.Simulasyon.Where(x => x.Sirket == sirket).ToList(); var g = i.Gercek.Where(x => x.Sirket == sirket && !(x.FisNo ?? "").StartsWith("R-")).ToList();
                    if (e == null || (e.Durum != "Posted" && e.Durum != "Reconciled")) { Ek("ERP Kaydı", sirket, b.Any(x => x.Tip != "MUTABAKAT") ? "Yevmiye + iç mutabakat" : "İç mutabakat", e == null ? "başlamadı" : e.Durum + (e.Cevap != null ? " · " + e.Cevap : ""), i.Tur == "M06" && i.Erp.Any(x => x.Durum == "Posted" || x.Durum == "Reconciled") ? "Kısmi Entegrasyon" : "Muhasebe Kaydı Oluşmamış"); continue; }
                    bool yevBek = b.Any(x => x.Tip != "MUTABAKAT"), yevGer = g.Any(x => x.Tip != "MUTABAKAT");
                    Ek("Yevmiye Fişi", sirket, yevBek ? "EVET" : "HAYIR (yalnız iç mutabakat)", yevGer ? "EVET · " + string.Join(", ", g.Where(x => x.Tip != "MUTABAKAT").Select(x => x.FisNo).Distinct()) : "Fiş yok", yevBek == yevGer ? "Uyumlu" : yevGer ? "Kontrol Gerekli" : "Muhasebe Kaydı Oluşmamış", yevBek);
                    var fisler = g.Where(x => x.Tip != "MUTABAKAT").Select(x => x.FisNo).Distinct().ToList(); if (fisler.Count > 1) Ek("Mükerrer Fiş", sirket, "1 aktif fiş", string.Join(", ", fisler), "Mükerrer Kayıt");
                    bool mutBek = b.Any(x => x.Tip == "MUTABAKAT"), mutGer = g.Any(x => x.Tip == "MUTABAKAT"); Ek("İç Mutabakat", sirket, mutBek ? "EVET" : "—", mutGer ? "EVET · " + e.MutabakatNo : "Oluşmamış", !mutBek || mutGer ? "Uyumlu" : "İç Mutabakat Oluşmamış");
                    if (yevBek && yevGer)
                    {
                        var bj = b.Where(x => x.Tip != "MUTABAKAT").ToList(); var gj = g.Where(x => x.Tip != "MUTABAKAT" && x.FisNo == fisler.FirstOrDefault()).ToList();
                        Ek("Borç = Alacak", sirket, $"{T(bj.Sum(x => x.Borc))} / {T(bj.Sum(x => x.Alacak))}", $"{T(gj.Sum(x => x.Borc))} / {T(gj.Sum(x => x.Alacak))}", Math.Abs(gj.Sum(x => x.Borc) - gj.Sum(x => x.Alacak)) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Tutar Farkı");
                        decimal bm = bj.Where(x => x.Tip == "YEVMIYE").Sum(x => x.Borc), gm = gj.Where(x => x.Tip == "YEVMIYE").Sum(x => x.Borc); Ek("Mahsup Tutarı (TL)", sirket, T(bm), T(gm), Math.Abs(bm - gm) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Tutar Farkı");
                        foreach (var x in bj)
                        {
                            var y = gj.FirstOrDefault(q => q.HesapRolu == x.HesapRolu && q.Cari == x.Cari && (q.Borc > 0) == (x.Borc > 0)) ?? gj.FirstOrDefault(q => q.HesapRolu == x.HesapRolu && q.Cari == x.Cari);
                            Ek($"Hesap / BP Rolü · {x.HesapRolu}", sirket, $"{x.HesapKodu} {x.Cari}".Trim(), y == null ? "—" : $"{y.HesapKodu} {y.Cari}".Trim(), y != null && y.HesapKodu == x.HesapKodu ? "Uyumlu" : x.Tip == "FX" ? "Kur Farkı Hatası" : "Hesap Hatası");
                            Ek($"{(x.Borc > 0 ? "Borç" : "Alacak")} · {x.HesapRolu}", sirket, T(x.Borc + x.Alacak), y == null ? "—" : T(y.Borc + y.Alacak), y != null && Math.Abs((y.Borc + y.Alacak) - (x.Borc + x.Alacak)) <= Parametreler["tutarTolerans"] ? "Uyumlu" : x.Tip == "FX" ? "Kur Farkı Hatası" : "Tutar Farkı");
                            if (x.Tip == "YEVMIYE") { Ek($"Proje / MM · {x.HesapRolu}", sirket, $"{(x.Proje == "" ? "—" : x.Proje)} / {(x.MasrafMerkezi == "" ? "—" : x.MasrafMerkezi)}", y == null ? "—" : $"{(y.Proje == "" ? "—" : y.Proje)} / {(y.MasrafMerkezi == "" ? "—" : y.MasrafMerkezi)}", y != null && (y.Proje ?? "") == (x.Proje ?? "") && (y.MasrafMerkezi ?? "") == (x.MasrafMerkezi ?? "") ? "Uyumlu" : "Kontrol Gerekli", false); }
                        }
                        Ek("PB / Kur", sirket, $"{b[0].ParaBirimi} {b[0].Kur.ToString("N4", Tr)}", $"{g[0].ParaBirimi} {g[0].Kur.ToString("N4", Tr)}", b[0].ParaBirimi == g[0].ParaBirimi && Math.Abs(g[0].Kur - b[0].Kur) < 0.0001m ? "Uyumlu" : "Kur Farkı Hatası", i.ParaBirimi != "TRY");
                    }
                    bool refOk = g.All(x => (x.Referans ?? "").Contains(i.No)); Ek("ERP Referansı ↔ Mahsup ID", sirket, i.No + (i.GrupMahsupNo != null ? " · " + i.GrupMahsupNo : ""), g.Select(x => x.Referans).Distinct().FirstOrDefault() ?? "—", refOk ? "Uyumlu" : "Kontrol Gerekli", false);
                }
                // açık kalem kapanma (§7.3)
                i.AcikKalemKontrol = i.Kalemler.Select(k => { decimal erp = i.ErpKalan.TryGetValue(k.BelgeNo, out var v) ? v : Math.Max(0, k.SonrasiKalan); bool ok = i.Erp.Any(x => x.Sirket == k.Sirket && (x.Durum == "Posted" || x.Durum == "Reconciled")) && Math.Abs(erp - Math.Max(0, k.SonrasiKalan)) <= Parametreler["tutarTolerans"]; return new MhAcikKalemKontrol { Belge = k.BelgeNo, OncekiAcik = k.AcikTutar, BeklenenMahsup = k.MahsupTutari, BeklenenKalan = Math.Max(0, k.SonrasiKalan), ErpKalan = erp, Sonuc = ok ? "Uyumlu" : $"FARK {T(erp - Math.Max(0, k.SonrasiKalan))}" }; }).ToList();
                foreach (var a in i.AcikKalemKontrol.Where(a => a.Sonuc != "Uyumlu")) Ek($"Açık Kalem · {a.Belge}", i.Sirket, $"kalan {T(a.BeklenenKalan)}", $"ERP kalan {T(a.ErpKalan)}", "Açık Kalem Farkı");
                // M06 mirror (§7.6)
                if (i.Tur == "M06")
                {
                    var ea = i.Erp.FirstOrDefault(x => x.Sirket == i.Sirket); var eb = i.Erp.FirstOrDefault(x => x.Sirket == i.KarsiSirket); string ok(bool c) => c ? "Uyumlu" : "Kısmi Entegrasyon";
                    bool aOk = ea != null && (ea.Durum == "Posted" || ea.Durum == "Reconciled"), bOk = eb != null && (eb.Durum == "Posted" || eb.Durum == "Reconciled");
                    Ek("Mirror · Grup Mahsup ID", $"{i.Sirket} ⇄ {i.KarsiSirket}", i.GrupMahsupNo, i.GrupMahsupNo, "Uyumlu");
                    decimal ta = i.Gercek.Where(x => x.Sirket == i.Sirket && x.Tip == "YEVMIYE" && !(x.FisNo ?? "").StartsWith("R-")).Sum(x => x.Borc), tb = i.Gercek.Where(x => x.Sirket == i.KarsiSirket && x.Tip == "MIRROR" && !(x.FisNo ?? "").StartsWith("R-")).Sum(x => x.Borc);
                    Ek("Mirror · Mahsup Tutarı", $"{i.Sirket} ⇄ {i.KarsiSirket}", T(ta), bOk ? T(tb) : "—", !bOk ? "Kısmi Entegrasyon" : Math.Abs(ta - tb) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Tutar Farkı");
                    Ek("Mirror · Para Birimi / Kayıt Tarihi", $"{i.Sirket} ⇄ {i.KarsiSirket}", $"{i.ParaBirimi} · {i.KayitTarihi:dd.MM.yyyy}", bOk ? $"{i.ParaBirimi} · {eb.Zaman:dd.MM.yyyy}" : "—", ok(bOk), false);
                    Ek("Mirror · Karşı Cari", $"{i.Sirket} ⇄ {i.KarsiSirket}", $"{i.KarsiSirket} / {i.Sirket}", bOk ? $"{i.KarsiSirket} / {i.Sirket}" : "—", ok(bOk), false);
                    Ek("Mirror · ERP Durumu", $"{i.Sirket} ⇄ {i.KarsiSirket}", "Muhasebeleşti / Muhasebeleşti", $"{(ea?.Durum ?? "—")} / {(eb?.Durum ?? "—")}", ok(aOk && bOk));
                }
                string eski = i.KontrolDurumu; i.KontrolDurumu = KontrolOnceligi.FirstOrDefault(x => d.Contains(x)) ?? "Uyumlu"; i.Kontroller = K; i.KontrolTarihi = z;
                if ((i.Durum == "Muhasebeleşti" || i.Durum == "Mutabakat Oluştu") && i.KontrolDurumu == "Uyumlu") { Log(i, z, kullanici, "STATUS", i.Durum, "Uyumlu", "Kontrol"); i.Durum = "Uyumlu"; }
                else if ((i.Durum == "Muhasebeleşti" || i.Durum == "Mutabakat Oluştu" || i.Durum == "Uyumlu") && i.KontrolDurumu != "Uyumlu") { if (i.Durum != "Kontrol Gerekli") Log(i, z, kullanici, "STATUS", i.Durum, "Kontrol Gerekli", "Kontrol"); i.Durum = "Kontrol Gerekli"; }
                if (eski != i.KontrolDurumu) Log(i, z, kullanici, "CONTROL_STATUS", eski, i.KontrolDurumu, "Kontrol"); return null;
            }
        }
        public static string FarkKapat(MhIslem i, string aciklama, string rol, string kullanici, DateTime? simdi = null) { lock (Kilit) { var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "farkKapat")) return "Kontrol farkını yalnız Muhasebe / Yönetici kapatır."; if (string.IsNullOrWhiteSpace(aciklama)) return "Açıklama zorunludur."; if (i.KontrolDurumu == null || i.KontrolDurumu == "Uyumlu") return "Kapatılacak fark yok."; if (i.Kontroller.Any(k => k.Sonuc != "Uyumlu" && k.Kritik && k.Sonuc != "Kontrol Gerekli")) return "Kritik fark (tutar / hesap / mutabakat / mükerrer / kısmi entegrasyon) açıklamayla kapatılamaz; düzeltme ya da ters kayıt gerekir."; i.FarkKapatma = aciklama; Log(i, z, kullanici, "CONTROL_CLOSE", i.KontrolDurumu, "Uyumlu (fark kapatıldı)", "Kontrol", aciklama); i.KontrolDurumu = "Uyumlu"; if (i.Durum == "Kontrol Gerekli") i.Durum = "Uyumlu"; return null; } }

        // =====================================================================
        // ÖRNEK KAYITLAR (§19 senaryoları + her durumdan bir işlem)
        // =====================================================================
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var bugun = DateTime.Today; string ku = "Nazlı Erdem";
                var AK = AcikKalemler; int Kn(string no, string sirket = null) => AK.First(k => k.BelgeNo == no && (sirket == null || k.Sirket == sirket)).Id;
                MhIslem Yap(int gunOnce, string sirket, string tur, string neden, string aciklama, (string belge, decimal tutar)[] kalemler, string karsi = null, string yontem = "Manuel")
                {
                    var z = bugun.AddDays(-gunOnce).AddHours(9).AddMinutes(30 + gunOnce % 25);
                    var g = new MhIslemGiris { Rol = "Yönetici", Tarih = z.ToString("yyyy-MM-dd"), KayitTarihi = z.ToString("yyyy-MM-dd"), Sirket = sirket, KarsiSirket = karsi, Tur = tur, NedenKodu = neden.Split(' ')[0], Neden = neden.Substring(neden.IndexOf(' ') + 1), Aciklama = aciklama, EslestirmeYontemi = yontem, MasrafMerkezi = "CC20 - Finans", Departman = "Muhasebe", Kalemler = kalemler.Select(k => new MhKalemGiris { KalemId = Kn(k.belge, sirket), MahsupTutari = k.tutar }).ToList() };
                    var (i, s) = Kaydet(g, ku, z); if (i == null) throw new InvalidOperationException("Mahsup tohumu: " + string.Join(" | ", s.Hatalar)); return i;
                }
                DateTime Z(MhIslem i, int saat) => i.OlusturmaZamani.AddHours(saat);
                void Gonder(MhIslem i) { var s = OnayaGonder(i, "Yönetici", ku, Z(i, 2)); if (!s.Gecerli) throw new InvalidOperationException(i.No + ": " + string.Join(" | ", s.Hatalar)); }
                void OnaylaHepsi(MhIslem i) { int n = 0; while (i.Durum == "Onay Bekliyor") { var a = i.Onaylar.First(o => o.Durum == "Bekliyor"); Onayla(i, "Onayla", n == 0 ? "Açık kalemler ve dağılım kontrol edildi." : "Uygundur.", "Yönetici", a.Kisi, Z(i, 20 + n * 5)); n++; } }
                void Erp(MhIslem i, string senaryo, int saat = 40) { var h = Muhasebelestir(i, senaryo, "Muhasebe", ku, Z(i, saat)); if (h != null) throw new InvalidOperationException(i.No + ": " + h); }

                // MHS-000125 · §19.2 Senaryo 2 — Aynı cari 1-N iç mutabakat: SF-105 100.000 ↔ IA-021 40.000 + IA-024 35.000 → 75.000; borçta 25.000 açık → Mutabakat Oluştu · Uyumlu (yeni yevmiye HAYIR)
                var i1 = Yap(26, "Uras Kimya", "M01", "MN02 İade / Credit Note Kapama", "Marmara Plastik satış faturasının iade faturalarıyla iç mutabakatı (1-N).", new[] { ("SF-2026-105", 75000m), ("IA-2026-021", 40000m), ("IA-2026-024", 35000m) }, null, "En Eski Vade Önce");
                Gonder(i1); OnaylaHepsi(i1); Erp(i1, "uyumlu");
                // MHS-000126 · §19.1 Senaryo 1 — Tedarikçi avans-fatura: AF-0771 100.000 ↔ AV-0118 60.000 → 60.000; faturada 40.000 açık → Muhasebeleşti · Uyumlu (320 Borç / 159 Alacak)
                var i2 = Yap(22, "Uras Kimya", "M03", "MN01 Avans Kapama", "Petkim'e verilen sipariş avansının alış faturasıyla mahsubu.", new[] { ("AF-2026-0771", 60000m), ("AV-2026-0118", 60000m) });
                Gonder(i2); OnaylaHepsi(i2); Erp(i2, "uyumlu");
                // MHS-000127 · §19.3 Senaryo 3 — Grup şirketleri arası 50.000 URAS ⇄ ALV: karşı şirket ERP hatası → Kısmi Entegrasyon
                var i3 = Yap(18, "Uras Kimya", "M06", "MN04 Grup İçi Bakiye Kapama", "URAS Kimya ile ALV Kimya arasındaki karşılıklı 50.000 TL bakiyenin tek Grup Mahsup ID ile kapatılması.", new[] { ("URS2026200126000", 50000m), ("ALV2026200000431", 50000m) }, "Alv Kimya");
                Gonder(i3); OnaylaHepsi(i3); Erp(i3, "karsiHata");
                // MHS-000128 · M04 Selvi müşteri avans-fatura 80.000 → Muhasebeleşti · Uyumlu (340 Borç / 120 Alacak)
                var i4 = Yap(20, "Selvi", "M04", "MN01 Avans Kapama", "Ege Boya'dan alınan avansın satış faturasıyla kapatılması.", new[] { ("AA-2026-0045", 80000m), ("SLV2026100004512", 80000m) });
                Gonder(i4); OnaylaHepsi(i4); Erp(i4, "uyumlu");
                // MHS-000129 · M02 müşteri-tedarikçi 66.000 (Karadeniz Ambalaj) → Kontrol Gerekli · Açık Kalem Farkı (ERP kalan +1.000)
                var i5 = Yap(16, "Uras Kimya", "M02", "MN03 Karşılıklı Cari Netleştirme", "Karadeniz Ambalaj müşteri alacağı ile tedarikçi borcunun netleştirilmesi (aynı tüzel kişi).", new[] { ("SF-2026-112", 66000m), ("AF-2026-0790", 66000m) });
                Gonder(i5); OnaylaHepsi(i5); Erp(i5, "acikKalem");
                // MHS-000130 · M05 dövizli (EUR): Avrupa Paper fatura ↔ iade + tahsilat; farklı kayıt kurları → kur farkı satırı → Uyumlu
                var i6 = Yap(14, "Avrupa Paper", "M05", "MN02 İade / Credit Note Kapama", "Karadeniz Ambalaj EUR faturasının iade faturası ve tahsilatla kapatılması; kur farkı simülasyonda.", new[] { ("AVP2026100002230", 42000m), ("AVI2026100000071", 12000m), ("TH-2026-0906", 30000m) });
                Gonder(i6); OnaylaHepsi(i6); Erp(i6, "uyumlu");
                // MHS-000131 · M03 Selvi Korozo: avans + ödeme ↔ fatura → Onay Bekliyor (Direktör: tutar eşiği yok; standart)
                var i7 = Yap(4, "Selvi", "M03", "MN01 Avans Kapama", "Korozo avansı ve ödemesinin alış faturasıyla mahsubu (N-1).", new[] { ("AF-2026-0611", 250000m), ("AV-2026-0090", 150000m), ("OD-2026-0712", 100000m) }, null, "En Eski Belge Önce");
                Gonder(i7); Onayla(i7, "Onayla", "Açık kalemler ERP ile doğrulandı.", "Muhasebe", ku, Z(i7, 5));
                // MHS-000132 · M01 Taslak: Marmara SF-108 48.000 ↔ tahsilat 25.000 (kısmi)
                var i8 = Yap(1, "Uras Kimya", "M01", "MN05 Dönem Sonu Mutabakat", "SF-108 faturasının açık tahsilatla kısmi kapatılması (taslak).", new[] { ("SF-2026-108", 25000m), ("TH-2026-0412", 25000m) });
                // MHS-000133 · M06 Reddedildi: Daf ⇄ Uras Holding 89.000
                var i9 = Yap(10, "Daf Kimya", "M06", "MN04 Grup İçi Bakiye Kapama", "Daf Kimya ile Uras Holding karşılıklı bakiye kapama.", new[] { ("DAF2026200000090", 89000m), ("UHO2026200000311", 89000m) }, "Uras Holding");
                Gonder(i9); Onayla(i9, "Onayla", "", "Muhasebe", ku, Z(i9, 4)); Onayla(i9, "Reddet", "Uras Holding tarafındaki hizmet faturası mutabakatı henüz imzalanmadı; mutabakat sonrası tekrar.", "Yönetici", "Kerem Yıldız", Z(i9, 30));
                // MHS-000134 · M03 Ters Kayıt: Petkim iade ↔ fatura 18.500 muhasebeleşti sonra geri alındı
                var i10 = Yap(12, "Uras Kimya", "M05", "MN02 İade / Credit Note Kapama", "Petkim alış iade faturasının alış faturasıyla mahsubu; tedarikçi iadeyi iptal ettiği için ters kayıt.", new[] { ("AF-2026-0802", 18500m), ("AI-2026-0033", 18500m) });
                Gonder(i10); OnaylaHepsi(i10); Erp(i10, "uyumlu"); var h10 = TersKayit(i10, "Tedarikçi iade faturasını iptal etti; mahsup konusuz kaldı.", "Muhasebe", ku, Z(i10, 96)); if (h10 != null) throw new InvalidOperationException(h10);
                // MHS-000135 · M02 Hesap Hatası: Karadeniz SF-119 ↔ AF-0815 42.000
                var i11 = Yap(8, "Uras Kimya", "M02", "MN03 Karşılıklı Cari Netleştirme", "Karadeniz Ambalaj ikinci netleştirme.", new[] { ("SF-2026-119", 42000m), ("AF-2026-0815", 42000m) });
                Gonder(i11); OnaylaHepsi(i11); Erp(i11, "hesap");
                // MHS-000136 · M06 tam uyumlu mirror: URAS ⇄ ALV 128.000
                var i12 = Yap(6, "Uras Kimya", "M06", "MN04 Grup İçi Bakiye Kapama", "URAS Kimya ↔ ALV Kimya hizmet faturası bakiyelerinin iki tarafta mirror kayıtla kapatılması.", new[] { ("URS2026200111874", 128000m), ("ALV2026200000452", 128000m) }, "Alv Kimya", "Aynı Tutarları Eşleştir");
                Gonder(i12); OnaylaHepsi(i12); Erp(i12, "uyumlu");
                _islemler.Sort((a, b) => b.Id.CompareTo(a.Id));
            }
        }
    }
}
