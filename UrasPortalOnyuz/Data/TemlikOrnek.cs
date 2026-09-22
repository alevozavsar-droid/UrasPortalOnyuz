// ============================================================
// TEMLİK MODÜLÜ — veri modeli, çok taraflı zincir kuralları, step bazlı muhasebe simülasyonu, Temlik Belgesi / imza, ERP ve kontrol motoru, örnek kayıtlar
// Kaynak: "URAS HOLDING Temlik Modülü — IT Teknik ve İşlevsel Gereksinim Dokümanı v1.0 (21.09.2026)" (Çok Taraflı Temlik Zinciri + 2-10 Taraf + N'li Virman + Temlik Belgesi).
// Üç sekme aynı veri setini kullanır: İşlem Ekranı (kaynak alacak satırlarını TEK TEK seç, kısmi tutar, taraf zinciri 2-10, N-1 transfer step,
// simülasyon, onay, Temlik Belgesi Oluştur, imza, step bazlı ERP), Raporu, Muhasebe Kontrolü (beklenen–gerçekleşen + zincir bütünlüğü).
// Kaynak alacaklar örnek satış faturaları / cari açık kalemlerdir (KaynakAlacaklar); satır kimliği belge id + satır no. Hesap kodları Esleme tablosundan gelir.
// Ekran: Controllers/GrupIciController.Temlik → Views/GrupIci/Temlik.cshtml + wwwroot/js/temlik.js; uçlar Controllers/TemlikController.cs.
// Ortak sınıflar (YnOnay, YnAudit, YnKontrol, YnSonuc) Data/YansitmaOrnek.cs içindedir.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication3.Data
{
    /// <summary>Kaynak alacak belgesi (satış faturası / cari açık kalem / sözleşme) — satırlar tek tek temlik edilebilir.</summary>
    public class TmlKaynakKalem { public int Sira { get; set; } public string Kod { get; set; } public string Ad { get; set; } public decimal Miktar { get; set; } public string Birim { get; set; } public decimal BirimFiyat { get; set; } public decimal KdvPct { get; set; } public decimal Kapanan { get; set; } public decimal Nominal => Math.Round(Miktar * BirimFiyat * (1 + KdvPct / 100m), 2); }
    public class TmlKaynak
    {
        public int Id { get; set; } public string KaynakSirket { get; set; } public string AlacakSahibi { get; set; } public string AlacakSahibiTipi { get; set; } public string AsilBorclu { get; set; }
        public string BelgeTuru { get; set; } public string BelgeNo { get; set; } public DateTime Tarih { get; set; } public DateTime Vade { get; set; } public string ParaBirimi { get; set; } = "TRY"; public decimal Kur { get; set; } = 1;
        public string Proje { get; set; } public string MasrafMerkezi { get; set; } public string Referans { get; set; } public string Durum { get; set; } = "Açık";   // Açık | Kapalı | İptal
        public List<TmlKaynakKalem> Kalemler { get; set; } = new List<TmlKaynakKalem>();
        public decimal Nominal => Kalemler.Sum(k => k.Nominal); public decimal Kapanan => Kalemler.Sum(k => k.Kapanan);
    }
    /// <summary>Taraf master (belge üretimi için unvan / vergi / adres / yetkili; V22).</summary>
    public class TmlTarafMaster { public string Ad { get; set; } public string Tip { get; set; } public string Unvan { get; set; } public string Vkn { get; set; } public string VergiDairesi { get; set; } public string Mersis { get; set; } public string Adres { get; set; } public string Yetkili { get; set; } public string YetkiliUnvan { get; set; } }

    // ---- işlem alt nesneleri ----
    /// <summary>ASSIGNMENT_STEP_ALLOCATION — kaynak alacak satırının bu işlemdeki temlik tutarı.</summary>
    public class TmlKaynakSatir { public int KaynakId { get; set; } public string BelgeNo { get; set; } public int Sira { get; set; } public string KalemAd { get; set; } public decimal Nominal { get; set; } public decimal Kapanan { get; set; } public decimal OncedenTemlik { get; set; } public decimal BuIslem { get; set; } public decimal Edilebilir => Math.Max(0, Nominal - Kapanan - OncedenTemlik); public decimal SonrasiKalan => Edilebilir - BuIslem; }
    /// <summary>ASSIGNMENT_CHAIN_PARTY — zincirdeki taraf (sıra, tip, rol, giren / çıkan / kalan hak).</summary>
    public class TmlTaraf { public int Sira { get; set; } public string Tip { get; set; } public string Ad { get; set; } public bool ImzaGerekli { get; set; } = true; public string Rol { get; set; } public decimal Giren { get; set; } public decimal Cikan { get; set; } public decimal Kalan => Giren - Cikan; public bool GrupSirketi => Tip == "Grup Şirketi"; }
    /// <summary>ASSIGNMENT_TRANSFER_STEP — A → B devri; ayrı ERP birimi.</summary>
    public class TmlStep { public int Sira { get; set; } public string Eden { get; set; } public string Alan { get; set; } public decimal Tutar { get; set; } public decimal Kur { get; set; } = 1; public decimal TutarTl => Math.Round(Tutar * Kur, 2); public DateTime EtkinlikTarihi { get; set; } public string Durum { get; set; } = "Hazır"; public string IdempotencyKey { get; set; } public string CorrelationId { get; set; } public string ErpRef { get; set; } public string ErpCevap { get; set; } public DateTime? ErpZamani { get; set; } public string ErpDurum { get; set; } = "NotStarted"; }
    /// <summary>Beklenen (simülasyon) ya da gerçekleşen (ERP) muhasebe satırı.</summary>
    public class TmlFisSatir { public string Sirket { get; set; } public int Step { get; set; } public bool Erp { get; set; } public string FisNo { get; set; } public string Yon { get; set; } public string HesapRolu { get; set; } public string HesapKodu { get; set; } public string HesapAdi { get; set; } public decimal Borc { get; set; } public decimal Alacak { get; set; } public string ParaBirimi { get; set; } public decimal Kur { get; set; } public decimal DovizTutar { get; set; } public string KarsiTaraf { get; set; } public string KaynakRef { get; set; } public string Aciklama { get; set; } public string Kural { get; set; } public string Referans { get; set; } }
    public class TmlImza { public int Sira { get; set; } public string Taraf { get; set; } public string Rol { get; set; } public string Imzaci { get; set; } public string Unvan { get; set; } public string Durum { get; set; } = "Bekliyor"; public DateTime? Tarih { get; set; } public string Yontem { get; set; } public bool Zorunlu { get; set; } = true; }
    /// <summary>ASSIGNMENT_DOCUMENT — Temlik Belgesi versiyonu (hukuk şablonu, hash, dinamik imza blokları).</summary>
    public class TmlBelge { public int Versiyon { get; set; } public string BelgeNo { get; set; } public DateTime OlusturmaZamani { get; set; } public string Olusturan { get; set; } public string SablonVersiyonu { get; set; } public string Hash { get; set; } public string Durum { get; set; } = "Üretildi"; public List<TmlImza> Imzalar { get; set; } = new List<TmlImza>(); public DateTime? ImzayaGonderim { get; set; } public string YuklenenDosya { get; set; } public string DosyaHash { get; set; } public string GecersizNedeni { get; set; } public bool BorcluBildirimi { get; set; } public DateTime? BildirimTarihi { get; set; } public bool Docx { get; set; } }

    /// <summary>ASSIGNMENT_HEADER — temlik işlemi ve zinciri.</summary>
    public class TmlIslem
    {
        public int Id { get; set; } public string No { get; set; } public string ZincirNo { get; set; } public DateTime Tarih { get; set; } public DateTime EtkinlikTarihi { get; set; }
        public string KaynakSirket { get; set; } public string AsilBorclu { get; set; } public string Tur { get; set; } public string KaynakBelgeTuru { get; set; } public string ParaBirimi { get; set; } = "TRY"; public decimal Kur { get; set; } = 1; public DateTime? KurTarihi { get; set; }
        public string NedenKodu { get; set; } public string Neden { get; set; } public string Proje { get; set; } public string MasrafMerkezi { get; set; } public string Aciklama { get; set; }
        public string Durum { get; set; } = "Taslak";
        public List<TmlKaynakSatir> Kaynaklar { get; set; } = new List<TmlKaynakSatir>();
        public List<TmlTaraf> Taraflar { get; set; } = new List<TmlTaraf>();
        public List<TmlStep> Stepler { get; set; } = new List<TmlStep>();
        public List<TmlFisSatir> Simulasyon { get; set; } = new List<TmlFisSatir>(); public DateTime? SimulasyonTarihi { get; set; } public int SimulasyonVersiyonu { get; set; } public string SimulasyonHash { get; set; }
        public List<TmlBelge> Belgeler { get; set; } = new List<TmlBelge>();
        public List<YnOnay> Onaylar { get; set; } = new List<YnOnay>(); public List<YnAudit> Audit { get; set; } = new List<YnAudit>();
        public List<TmlFisSatir> Gercek { get; set; } = new List<TmlFisSatir>(); public DateTime? GercekTarihi { get; set; }
        public string KontrolDurumu { get; set; } public List<YnKontrol> Kontroller { get; set; } = new List<YnKontrol>(); public DateTime? KontrolTarihi { get; set; } public string FarkKapatma { get; set; }
        public string RedNedeni { get; set; } public string IptalNedeni { get; set; } public string IstisnaGerekce { get; set; }
        public string Olusturan { get; set; } public DateTime OlusturmaZamani { get; set; } public string Guncelleyen { get; set; } public DateTime GuncellemeZamani { get; set; } public int Versiyon { get; set; } = 1;

        public int TarafSayisi => Taraflar.Count; public int StepSayisi => Stepler.Count;
        public decimal KaynakToplam => Kaynaklar.Sum(k => k.Nominal); public decimal OncedenTemlik => Kaynaklar.Sum(k => k.OncedenTemlik); public decimal Edilebilir => Kaynaklar.Sum(k => k.Edilebilir); public decimal BuTemlikTutari => Kaynaklar.Sum(k => k.BuIslem);
        public decimal NihaiTutar => Stepler.Count > 0 ? Stepler.Last().Tutar : 0; public decimal AraTarafKalani => Taraflar.Where(t => t.Rol == "Ara Temlik Alan / Devreden").Sum(t => t.Kalan);
        public string IlkEden => Taraflar.FirstOrDefault()?.Ad; public string NihaiAlan => Taraflar.LastOrDefault()?.Ad;
        public TmlBelge AktifBelge => Belgeler.LastOrDefault(b => b.Durum != "Geçersiz");
        public string BelgeDurumu { get { var b = AktifBelge; if (b == null) return Belgeler.Count > 0 ? "Geçersiz" : "Belge Yok"; int z = b.Imzalar.Count(i => i.Zorunlu), ok = b.Imzalar.Count(i => i.Zorunlu && i.Durum == "İmzalandı"); return ok == 0 ? (b.ImzayaGonderim.HasValue ? "İmzada" : "Üretildi") : ok < z ? "Kısmi İmzalı" : "Tam İmzalı"; } }
        public string ImzaDurumu { get { var b = AktifBelge; if (b == null) return "—"; return $"{b.Imzalar.Count(i => i.Zorunlu && i.Durum == "İmzalandı")}/{b.Imzalar.Count(i => i.Zorunlu)}"; } }
        public bool TamImzali => AktifBelge != null && AktifBelge.Imzalar.Where(i => i.Zorunlu).All(i => i.Durum == "İmzalandı") && AktifBelge.Imzalar.Any();
        public string MuhasebeDurumu { get { int p = Stepler.Count(s => s.ErpDurum == "Posted" || s.ErpDurum == "Reversed"), f = Stepler.Count(s => s.ErpDurum == "Failed"); if (Stepler.Any(s => s.ErpDurum == "Reversed")) return "Ters Kayıt"; if (p == 0 && f == 0) return "Başlamadı"; if (f > 0) return "Hata"; return p == Stepler.Count ? "Tamamlandı" : "Kısmi"; } }
        public string ErpDurumu { get { if (Stepler.Count == 0) return "NotStarted"; if (Stepler.All(s => s.ErpDurum == "Reversed")) return "Reversed"; if (Stepler.All(s => s.ErpDurum == "Posted")) return "Completed"; if (Stepler.Any(s => s.ErpDurum == "Failed")) return "Failed"; if (Stepler.Any(s => s.ErpDurum == "Posted")) return "PartiallyCompleted"; return "NotStarted"; } }
        public bool Aktif => Durum != "İptal" && Durum != "Reddedildi" && Durum != "Ters Kayıt";
        public bool Onaylandi => Onaylar.Count > 0 && Onaylar.All(o => o.Durum == "Onaylandı");
        public bool Duzenlenebilir => Durum == "Taslak" || Durum == "Reddedildi";
    }

    /// <summary>TEMLIK_HESAP_ESLEME — şirket / yön / hesap rolü / karşı taraf tipine göre hesap ("*" = her değer).</summary>
    public class TmlEsleme { public int Id { get; set; } public string Sirket { get; set; } = "*"; public string Yon { get; set; } public string HesapRolu { get; set; } public string TarafTipi { get; set; } = "*"; public string KaynakBelgeTuru { get; set; } = "*"; public string Hesap { get; set; } public string HesapAdi { get; set; } public string BpMode { get; set; } = "GL"; public DateTime GecerlilikBas { get; set; } = new DateTime(2026, 1, 1); public bool Aktif { get; set; } = true; public int Ozgulluk => (Sirket == "*" ? 0 : 4) + (TarafTipi == "*" ? 0 : 2) + (KaynakBelgeTuru == "*" ? 0 : 1); }

    // ---- JSON giriş sözleşmeleri ----
    public class TmlKaynakGiris { public int KaynakId { get; set; } public int Sira { get; set; } public decimal BuIslem { get; set; } }
    public class TmlTarafGiris { public string Tip { get; set; } public string Ad { get; set; } public bool ImzaGerekli { get; set; } = true; }
    public class TmlStepGiris { public decimal? Tutar { get; set; } public string EtkinlikTarihi { get; set; } }
    public class TmlIslemGiris
    {
        public int Id { get; set; } public string Rol { get; set; } public string Tarih { get; set; } public string EtkinlikTarihi { get; set; } public string KaynakSirket { get; set; } public string AsilBorclu { get; set; } public string Tur { get; set; } public string KaynakBelgeTuru { get; set; }
        public string ParaBirimi { get; set; } public decimal? Kur { get; set; } public string KurTarihi { get; set; } public string NedenKodu { get; set; } public string Neden { get; set; } public string Proje { get; set; } public string MasrafMerkezi { get; set; } public string Aciklama { get; set; }
        public List<TmlKaynakGiris> Kaynaklar { get; set; } public List<TmlTarafGiris> Taraflar { get; set; } public List<TmlStepGiris> Stepler { get; set; } public string Gerekce { get; set; }
    }

    public static class TemlikOrnek
    {
        public static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        public static readonly object Kilit = new object();
        public static readonly string[] Turler = { "Alacak Temliki", "Kısmi Temlik", "Zincir Temlik", "Grup İçi Temlik", "Teminat Amaçlı Temlik" };
        public static readonly string[] KaynakBelgeTurleri = { "Fatura", "Cari Alacak", "Sözleşme", "Diğer" };
        public static readonly string[] TarafTipleri = { "Grup Şirketi", "Tedarikçi", "Müşteri", "Diğer Tüzel Kişi" };
        public static readonly string[] NedenKodlari = { "TN01 Grup İçi Finansman", "TN02 Tedarikçi Borcuna Mahsuben", "TN03 Kredi Teminatı", "TN04 Alacak Yönetimi / Faktoring", "TN05 Diğer" };
        public static readonly string[] Durumlar = { "Taslak", "Onay Bekliyor", "Onaylandı", "Belge Üretildi", "İmzaya Gönderildi", "Kısmi İmzalı", "Tam İmzalı", "Kısmi Muhasebeleşti", "Tamamlandı", "Kontrol Gerekli", "Reddedildi", "İptal", "Ters Kayıt" };
        public static readonly string[] KontrolDurumlari = { "Uyumlu", "Kontrol Gerekli", "Hesap Hatası", "Tutar Farkı", "Zincir Kırık", "Ara Taraf Farkı", "Nihai Hak Sahibi Farkı", "İmza Eksik", "Mükerrer Kayıt", "Kısmi Entegrasyon", "Muhasebe Kaydı Oluşmamış" };
        public static readonly string[] KontrolOnceligi = { "Muhasebe Kaydı Oluşmamış", "Zincir Kırık", "Mükerrer Kayıt", "Kısmi Entegrasyon", "Tutar Farkı", "Hesap Hatası", "Nihai Hak Sahibi Farkı", "Ara Taraf Farkı", "İmza Eksik", "Kontrol Gerekli" };
        public const int MinTaraf = 2, MaxTaraf = 10;
        /// <summary>Yetki matrisi (§14).</summary>
        public static readonly Dictionary<string, string[]> Yetkiler = new Dictionary<string, string[]>
        {
            ["Operasyon"] = new[] { "taslak", "kaynakSec", "taraf", "simulasyon", "onayaGonder", "belgeOlustur", "imzayaGonder", "imzala", "belgeYukle", "iptal", "kontrolGor" },
            ["Muhasebe"] = new[] { "taslak", "kaynakSec", "taraf", "simulasyon", "onayaGonder", "esleme", "belgeOlustur", "imzayaGonder", "imzala", "belgeYukle", "muhasebelestir", "kontrol", "farkKapat", "iptal", "tersKayit", "onay", "kontrolGor" },
            ["Hukuk"] = new[] { "simulasyon", "sablon", "belgeOlustur", "imzayaGonder", "imzala", "belgeYukle", "bildirim", "iptal", "onay", "kontrolGor", "belgeGecersiz" },
            ["Yönetici"] = new[] { "taslak", "kaynakSec", "taraf", "simulasyon", "onayaGonder", "esleme", "sablon", "belgeOlustur", "imzayaGonder", "imzala", "belgeYukle", "bildirim", "belgeGecersiz", "muhasebelestir", "istisna", "kontrol", "farkKapat", "iptal", "tersKayit", "onay", "kontrolGor" }
        };
        public static bool Yetkili(string rol, string eylem) => Yetkiler.TryGetValue(rol ?? "", out var l) && l.Contains(eylem);
        public static readonly Dictionary<string, decimal> Parametreler = new Dictionary<string, decimal> { ["direktorEsik"] = 1000000m, ["direktorTaraf"] = 4, ["tutarTolerans"] = 1m, ["kurTolerans"] = 0.5m, ["borcluImzasi"] = 0, ["araTarafFazlaDevir"] = 0 };
        public static readonly Dictionary<string, string> ParametreEtiketler = new Dictionary<string, string> { ["direktorEsik"] = "Bu temlik tutarını aşan işlemde Direktör onayı (TL)", ["direktorTaraf"] = "Taraf sayısı bu değerden büyük / eşitse Direktör onayı", ["tutarTolerans"] = "Tutar / yuvarlama farkı toleransı (TL)", ["kurTolerans"] = "Kur farkı toleransı (%)", ["borcluImzasi"] = "Asıl borçlu kabul imzası zorunlu (1 = evet)", ["araTarafFazlaDevir"] = "Ara taraf aldığından fazlasını devredebilir (1 = evet, gerekçeyle)" };
        public static readonly string[] Sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct().ToArray();
        public static readonly string[] MasrafMerkezleri = YansitmaOrnek.MasrafMerkezleri; public static readonly string[] Projeler = YansitmaOrnek.Projeler;
        public static string Kisa(string s) => YansitmaOrnek.Kisa(s);
        public const string SablonVersiyonu = "HUK-TML-ŞABLON v3.2 (Hukuk, 01.07.2026)";

        // ---- taraf master (belge üretimi V22) ----
        public static readonly List<TmlTarafMaster> Taraflar = new List<TmlTarafMaster>
        {
            new TmlTarafMaster { Ad = "Uras Kimya", Tip = "Grup Şirketi", Unvan = "Uras Kimya San. ve Tic. A.Ş.", Vkn = "8930012345", VergiDairesi = "Gebze", Mersis = "0893001234500015", Adres = "GOSB 1000. Cad. No:12 Gebze / Kocaeli", Yetkili = "Kerem Aksoy", YetkiliUnvan = "Genel Müdür" },
            new TmlTarafMaster { Ad = "Alv Kimya", Tip = "Grup Şirketi", Unvan = "ALV Kimya San. A.Ş.", Vkn = "0610023456", VergiDairesi = "Gebze", Mersis = "0061002345600011", Adres = "GOSB 1200. Cad. No:4 Gebze / Kocaeli", Yetkili = "Sevgi Ay", YetkiliUnvan = "Yönetim Kurulu Üyesi" },
            new TmlTarafMaster { Ad = "Selvi", Tip = "Grup Şirketi", Unvan = "Selvi Kimya ve Ambalaj San. A.Ş.", Vkn = "7600034567", VergiDairesi = "Çerkezköy", Mersis = "0760003456700012", Adres = "ÇOSB 3. Cad. No:8 Çerkezköy / Tekirdağ", Yetkili = "Pınar Ak", YetkiliUnvan = "Finans Müdürü" },
            new TmlTarafMaster { Ad = "Avrupa Paper", Tip = "Grup Şirketi", Unvan = "Avrupa Paper Kağıt San. A.Ş.", Vkn = "1190045678", VergiDairesi = "Çorlu", Mersis = "0119004567800014", Adres = "Çorlu OSB 5. Sok. No:3 Çorlu / Tekirdağ", Yetkili = "Onur Bal", YetkiliUnvan = "Mali İşler Direktörü" },
            new TmlTarafMaster { Ad = "Daf Kimya", Tip = "Grup Şirketi", Unvan = "DAF Kimya San. Ltd. Şti.", Vkn = "2670056789", VergiDairesi = "Dilovası", Mersis = "0267005678900019", Adres = "Dilovası OSB 2. Kısım No:7 Dilovası / Kocaeli", Yetkili = "Hülya Er", YetkiliUnvan = "Şirket Müdürü" },
            new TmlTarafMaster { Ad = "Uras Kimya A.Ş.", Tip = "Grup Şirketi", Unvan = "Uras Kimya A.Ş. (Yurt Dışı)", Vkn = "8930012399", VergiDairesi = "Büyük Mükellefler", Mersis = "0893001239900017", Adres = "Maslak Mah. Büyükdere Cad. No:255 Sarıyer / İstanbul", Yetkili = "Esra Kaya", YetkiliUnvan = "Genel Müdür" },
            new TmlTarafMaster { Ad = "Uras Power", Tip = "Grup Şirketi", Unvan = "Uras Power Enerji Üretim A.Ş.", Vkn = "8930067890", VergiDairesi = "Gebze", Mersis = "0893006789000013", Adres = "GOSB 1000. Cad. No:12/A Gebze / Kocaeli", Yetkili = "Mert Doğan", YetkiliUnvan = "Genel Müdür" },
            new TmlTarafMaster { Ad = "Uras Holding", Tip = "Grup Şirketi", Unvan = "Uras Holding A.Ş.", Vkn = "8930000001", VergiDairesi = "Büyük Mükellefler", Mersis = "0893000000100010", Adres = "Maslak Mah. Büyükdere Cad. No:255 K:18 Sarıyer / İstanbul", Yetkili = "Kerem Yıldız", YetkiliUnvan = "CFO" },
            new TmlTarafMaster { Ad = "A Tedarikçisi", Tip = "Tedarikçi", Unvan = "Anadolu Kimyevi Maddeler San. ve Tic. A.Ş.", Vkn = "0690012340", VergiDairesi = "İkitelli", Mersis = "0069001234000018", Adres = "İkitelli OSB Atatürk Bulvarı No:64 Başakşehir / İstanbul", Yetkili = "Halil Demirci", YetkiliUnvan = "Şirket Yetkilisi" },
            new TmlTarafMaster { Ad = "B Tedarikçisi", Tip = "Tedarikçi", Unvan = "Boğaziçi Solvent ve Kimya Tic. Ltd. Şti.", Vkn = "1780023450", VergiDairesi = "Tuzla", Mersis = "0178002345000016", Adres = "Tuzla Kimya OSB Melek Aras Bulv. No:21 Tuzla / İstanbul", Yetkili = "Seda Karahan", YetkiliUnvan = "Genel Müdür" },
            new TmlTarafMaster { Ad = "Petkim Petrokimya Holding A.Ş.", Tip = "Tedarikçi", Unvan = "Petkim Petrokimya Holding A.Ş.", Vkn = "7290030050", VergiDairesi = "Aliağa", Mersis = "0729003005000011", Adres = "Siteler Mah. Necmettin Giritlioğlu Cad. Aliağa / İzmir", Yetkili = "Yetkili İmza", YetkiliUnvan = "Finans Direktörü" },
            new TmlTarafMaster { Ad = "X Müşterisi", Tip = "Müşteri", Unvan = "Ege Boya ve Vernik San. A.Ş.", Vkn = "3300034560", VergiDairesi = "Kemalpaşa", Mersis = "0330003456000012", Adres = "Kemalpaşa OSB 12. Cad. No:5 Kemalpaşa / İzmir", Yetkili = "Cem Aydın", YetkiliUnvan = "Mali İşler Müdürü" },
            new TmlTarafMaster { Ad = "Marmara Plastik San. A.Ş.", Tip = "Müşteri", Unvan = "Marmara Plastik San. A.Ş.", Vkn = "6120045670", VergiDairesi = "Gebze", Mersis = "0612004567000015", Adres = "Dilovası OSB 1. Kısım No:2 Dilovası / Kocaeli", Yetkili = "Levent Şahin", YetkiliUnvan = "Genel Müdür" },
            new TmlTarafMaster { Ad = "Karadeniz Ambalaj Ltd. Şti.", Tip = "Müşteri", Unvan = "Karadeniz Ambalaj San. ve Tic. Ltd. Şti.", Vkn = "5210056780", VergiDairesi = "Samsun", Mersis = "0521005678000013", Adres = "Samsun OSB 4. Cad. No:9 Tekkeköy / Samsun", Yetkili = "Nihat Yavuz", YetkiliUnvan = "Şirket Müdürü" },
            new TmlTarafMaster { Ad = "Ziraat Bankası Gebze Şb.", Tip = "Diğer Tüzel Kişi", Unvan = "T.C. Ziraat Bankası A.Ş. Gebze Şubesi", Vkn = "9980000001", VergiDairesi = "Büyük Mükellefler", Mersis = "0998000000100019", Adres = "Hacı Halil Mah. İsmet Paşa Cad. No:31 Gebze / Kocaeli", Yetkili = "Şube Müdürü", YetkiliUnvan = "Şube Müdürü" },
            new TmlTarafMaster { Ad = "Yeni Taraf (master eksik)", Tip = "Diğer Tüzel Kişi", Unvan = "", Vkn = "", VergiDairesi = "", Adres = "", Yetkili = "", YetkiliUnvan = "" }
        };
        public static TmlTarafMaster Master(string ad) => Taraflar.FirstOrDefault(t => t.Ad == ad);
        public static string TarafTipi(string ad) => Master(ad)?.Tip ?? (Sirketler.Contains(ad) ? "Grup Şirketi" : "Diğer Tüzel Kişi");
        public static bool GrupMu(string ad) => TarafTipi(ad) == "Grup Şirketi";

        // ---- hesap eşleme tablosu ----
        public static readonly List<TmlEsleme> Esleme = new List<TmlEsleme>
        {
            new TmlEsleme { Id = 1, Yon = "ASSIGN_OUT", HesapRolu = "Kaynak Alacak", TarafTipi = "*", Hesap = "120.01.{BORCLU}", HesapAdi = "Alıcılar — asıl borçlu", BpMode = "BP" },
            new TmlEsleme { Id = 2, Yon = "ASSIGN_OUT", HesapRolu = "Temlik Alacağı", TarafTipi = "*", Hesap = "127.01.{BORCLU}", HesapAdi = "Temlik Yoluyla Devralınan Alacaklar", BpMode = "GL" },
            new TmlEsleme { Id = 3, Yon = "ASSIGN_OUT", HesapRolu = "Temlik Karşı Hak", TarafTipi = "Grup Şirketi", Hesap = "120.02.{KARSI}", HesapAdi = "Alıcılar — grup şirketi (temlik karşı hak)", BpMode = "BP" },
            new TmlEsleme { Id = 4, Yon = "ASSIGN_OUT", HesapRolu = "Temlik Karşı Hak", TarafTipi = "Tedarikçi", Hesap = "320.01.{KARSI}", HesapAdi = "Satıcılar — temlik karşılığı mahsup", BpMode = "BP" },
            new TmlEsleme { Id = 5, Yon = "ASSIGN_OUT", HesapRolu = "Temlik Karşı Hak", TarafTipi = "*", Hesap = "136.05.{KARSI}", HesapAdi = "Diğer Çeşitli Alacaklar — temlik karşı hak", BpMode = "GL" },
            new TmlEsleme { Id = 6, Yon = "ASSIGN_IN", HesapRolu = "Temlik Alacağı", TarafTipi = "*", Hesap = "127.01.{BORCLU}", HesapAdi = "Temlik Yoluyla Devralınan Alacaklar", BpMode = "GL" },
            new TmlEsleme { Id = 7, Yon = "ASSIGN_IN", HesapRolu = "Temlik Borcu / Karşı Cari", TarafTipi = "Grup Şirketi", Hesap = "320.02.{KARSI}", HesapAdi = "Satıcılar — grup şirketi (temlik borcu)", BpMode = "BP" },
            new TmlEsleme { Id = 8, Yon = "ASSIGN_IN", HesapRolu = "Temlik Borcu / Karşı Cari", TarafTipi = "Tedarikçi", Hesap = "320.01.{KARSI}", HesapAdi = "Satıcılar — temlik eden tedarikçi", BpMode = "BP" },
            new TmlEsleme { Id = 9, Yon = "ASSIGN_IN", HesapRolu = "Temlik Borcu / Karşı Cari", TarafTipi = "*", Hesap = "336.05.{KARSI}", HesapAdi = "Diğer Çeşitli Borçlar — temlik borcu", BpMode = "GL" },
            new TmlEsleme { Id = 10, Yon = "ASSIGN_IN", HesapRolu = "Temlik Clearing", TarafTipi = "*", Hesap = "195.09.001", HesapAdi = "Temlik Geçici / Clearing", BpMode = "Clearing" },
            new TmlEsleme { Id = 11, Yon = "*", HesapRolu = "Temlik İskonto Gider", TarafTipi = "*", Hesap = "659.05.001", HesapAdi = "Temlik İskonto Giderleri", BpMode = "GL" },
            new TmlEsleme { Id = 12, Yon = "*", HesapRolu = "Kur Farkı Gelir/Gider", TarafTipi = "*", Hesap = "646.01.001 / 656.01.001", HesapAdi = "Kambiyo Kârları / Zararları", BpMode = "GL" },
            new TmlEsleme { Id = 13, Sirket = "Uras Power", Yon = "ASSIGN_IN", HesapRolu = "Temlik Alacağı", TarafTipi = "*", Hesap = "127.02.{BORCLU}", HesapAdi = "GES Temlik Alacakları", BpMode = "GL", Aktif = false }
        };
        public static TmlEsleme EslemeBul(string sirket, string yon, string rol, string karsiTip, string belgeTuru, DateTime tarih) =>
            Esleme.Where(e => e.Aktif && e.GecerlilikBas <= tarih && (e.Yon == "*" || e.Yon == yon) && e.HesapRolu == rol && (e.Sirket == "*" || e.Sirket == sirket) && (e.TarafTipi == "*" || e.TarafTipi == karsiTip) && (e.KaynakBelgeTuru == "*" || e.KaynakBelgeTuru == belgeTuru)).OrderByDescending(e => e.Ozgulluk).FirstOrDefault();
        private static string HesapKodu(TmlEsleme e, string borclu, string karsi) => e.Hesap.Replace("{BORCLU}", Kisa(borclu)).Replace("{KARSI}", Kisa(karsi));

        // ---- kaynak alacaklar (örnek satış faturaları / cari açık kalemler / tedarikçi alacak kayıtları) ----
        private static readonly Lazy<List<TmlKaynak>> _kaynaklar = new Lazy<List<TmlKaynak>>(KaynakOlustur);
        public static List<TmlKaynak> KaynakAlacaklar => _kaynaklar.Value;
        private static List<TmlKaynak> KaynakOlustur()
        {
            var b = DateTime.Today; int id = 0;
            TmlKaynakKalem K(int sira, string kod, string ad, decimal miktar, string birim, decimal fiyat, decimal kdv = 20, decimal kapanan = 0) => new TmlKaynakKalem { Sira = sira, Kod = kod, Ad = ad, Miktar = miktar, Birim = birim, BirimFiyat = fiyat, KdvPct = kdv, Kapanan = kapanan };
            TmlKaynak Y(string sirket, string sahip, string borclu, string tur, string no, int gunOnce, int vadeGun, string pb, decimal kur, string referans, params TmlKaynakKalem[] kalemler) =>
                new TmlKaynak { Id = ++id, KaynakSirket = sirket, AlacakSahibi = sahip, AlacakSahibiTipi = TarafTipi(sahip), AsilBorclu = borclu, BelgeTuru = tur, BelgeNo = no, Tarih = b.AddDays(-gunOnce), Vade = b.AddDays(-gunOnce + vadeGun), ParaBirimi = pb, Kur = kur, Referans = referans, MasrafMerkezi = "CC20 - Finans", Kalemler = kalemler.ToList() };
            var l = new List<TmlKaynak>
            {
                // §6.2 / §23.1 örneği: A Tedarikçisi'nin X Müşterisi'nden alacağı (ALV Kimya ERP'sinde kayıtlı), 600.000 + 400.000 = 1.000.000 TL
                Y("Alv Kimya", "A Tedarikçisi", "X Müşterisi", "Fatura", "FTR-2026-00125", 48, 90, "TRY", 1, "SZL-2026-114", K(1, "SLV-TOL-01", "Toluen Teknik (dökme)", 5000, "kg", 58.333333m), K(2, "SLV-KSL-02", "Ksilen Karışım", 2500, "kg", 83.333333m)),
                Y("Alv Kimya", "A Tedarikçisi", "X Müşterisi", "Fatura", "FTR-2026-00148", 40, 90, "TRY", 1, "SZL-2026-114", K(1, "SLV-AST-03", "Etil Asetat", 8000, "kg", 41.666667m)),
                // §23.2: B Tedarikçisi → X Müşterisi 1.000.000
                Y("Alv Kimya", "B Tedarikçisi", "X Müşterisi", "Cari Alacak", "CAR-2026-00311", 35, 60, "TRY", 1, "Mutabakat 08/2026", K(1, "CAR", "Cari açık kalem — Ağustos 2026 sevkiyatları", 1, "adet", 1000000m, 0)),
                // Grup şirketi satış faturaları (2 taraflı standart temlik: alacak sahibi grup şirketi)
                Y("Uras Kimya", "Uras Kimya", "Marmara Plastik San. A.Ş.", "Fatura", "URS2026100071204", 30, 60, "TRY", 1, "SAS-2026-M-118", K(1, "HM-LDPE-01", "Polietilen LDPE Granül", 12000, "kg", 47.50m), K(2, "HM-EA-02", "Etil Asetat Teknik", 6000, "kg", 44.20m), K(3, "AM-IBC-05", "IBC Tank 1000 L", 20, "adet", 4650m, 20, 111600m)),
                Y("Uras Kimya", "Uras Kimya", "Marmara Plastik San. A.Ş.", "Fatura", "URS2026100071391", 18, 60, "TRY", 1, "SAS-2026-M-131", K(1, "HM-LDPE-01", "Polietilen LDPE Granül", 8000, "kg", 47.50m)),
                Y("Uras Kimya", "Uras Kimya", "Karadeniz Ambalaj Ltd. Şti.", "Fatura", "URS2026100070988", 55, 45, "TRY", 1, "SAS-2026-K-092", K(1, "AM-STRC-02", "Streç Film 50cm 17µ", 1500, "rulo", 236m), K(2, "AM-KOLI-01", "Oluklu Mukavva Koli", 40000, "adet", 19.80m)),
                Y("Selvi", "Selvi", "Marmara Plastik San. A.Ş.", "Fatura", "SLV2026100004512", 25, 60, "TRY", 1, "SAS-2026-S-044", K(1, "AM-KOLI-01", "Oluklu Mukavva Koli 40x30x30", 60000, "adet", 20.40m), K(2, "AM-STRC-02", "Streç Film", 900, "rulo", 240m)),
                Y("Avrupa Paper", "Avrupa Paper", "Karadeniz Ambalaj Ltd. Şti.", "Fatura", "AVP2026100002230", 22, 75, "EUR", 47.85m, "SAS-2026-A-017", K(1, "KG-TST-01", "Testliner 140 gr", 180, "ton", 610m, 20), K(2, "KG-FLT-02", "Fluting 120 gr", 120, "ton", 540m, 20)),
                Y("Uras Kimya", "Uras Kimya", "X Müşterisi", "Sözleşme", "SZL-2026-207", 60, 180, "TRY", 1, "Tedarik sözleşmesi 2026/207", K(1, "SZL", "Sözleşmeye bağlı aylık hak ediş (Eylül–Aralık)", 4, "ay", 187500m, 20)),
                Y("Uras Kimya", "Petkim Petrokimya Holding A.Ş.", "X Müşterisi", "Cari Alacak", "CAR-2026-00420", 12, 30, "TRY", 1, "Mutabakat 09/2026", K(1, "CAR", "Cari açık kalem — Eylül 2026", 1, "adet", 450000m, 0)),
                Y("Daf Kimya", "Daf Kimya", "Marmara Plastik San. A.Ş.", "Fatura", "DAF2026100001087", 70, 45, "TRY", 1, "SAS-2026-D-009", K(1, "HM-NAOH-21", "Sodyum Hidroksit %50", 30, "ton", 14900m))
            };
            l[10].Durum = "Kapalı"; l[10].Kalemler[0].Kapanan = l[10].Kalemler[0].Nominal;   // tahsil edilmiş → V21
            return l;
        }
        public static TmlKaynak KaynakBul(int id) => KaynakAlacaklar.FirstOrDefault(k => k.Id == id);
        /// <summary>Aynı kaynak satırın aktif / rezerve temlik toplamı (iptal, red, ters kayıt hariç; taslak ve onay süreci rezervasyon olarak dahil, §4.2).</summary>
        public static decimal OncedenTemlik(int kaynakId, int sira, int haricId = 0) => Islemler.Where(i => i.Id != haricId && i.Aktif).SelectMany(i => i.Kaynaklar).Where(k => k.KaynakId == kaynakId && k.Sira == sira).Sum(k => k.BuIslem);

        /// <summary>Kaynak Alacak Seç penceresi (§6.1): kaynak şirket ve/veya alacak sahibine göre filtreli; satır bazında temlik edilebilir bakiye.</summary>
        public static IEnumerable<object> KaynakListesi(string kaynakSirket, string ara, int haricId = 0)
        {
            ara = (ara ?? "").Trim().ToLower(Tr);
            return KaynakAlacaklar.Where(k => string.IsNullOrEmpty(kaynakSirket) || k.KaynakSirket == kaynakSirket)
                .Where(k => ara == "" || (k.BelgeNo + " " + k.AlacakSahibi + " " + k.AsilBorclu + " " + k.Referans + " " + string.Join(" ", k.Kalemler.Select(x => x.Ad))).ToLower(Tr).Contains(ara))
                .OrderByDescending(k => k.Tarih).Select(k =>
                {
                    var kalemler = k.Kalemler.Select(x => { var on = OncedenTemlik(k.Id, x.Sira, haricId); return new { sira = x.Sira, kod = x.Kod, ad = x.Ad, miktar = x.Miktar, birim = x.Birim, birimFiyat = x.BirimFiyat, kdvPct = x.KdvPct, nominal = x.Nominal, kapanan = x.Kapanan, oncedenTemlik = on, edilebilir = Math.Max(0, x.Nominal - x.Kapanan - on) }; }).ToList();
                    string kilit = k.Durum == "İptal" ? "İptal belge" : k.Durum == "Kapalı" ? "Kapalı / tahsil edilmiş (V21)" : kalemler.All(x => x.edilebilir <= 0) ? "Tamamen temlik edilmiş" : "";
                    return new { id = k.Id, kaynakSirket = k.KaynakSirket, alacakSahibi = k.AlacakSahibi, alacakSahibiTipi = k.AlacakSahibiTipi, asilBorclu = k.AsilBorclu, belgeTuru = k.BelgeTuru, belgeNo = k.BelgeNo, tarih = k.Tarih.ToString("yyyy-MM-dd"), tarihTr = k.Tarih.ToString("dd.MM.yyyy"), vade = k.Vade.ToString("dd.MM.yyyy"), vadeGecti = k.Vade < DateTime.Today, paraBirimi = k.ParaBirimi, kur = k.Kur, proje = k.Proje, masrafMerkezi = k.MasrafMerkezi, referans = k.Referans, durum = k.Durum, nominal = kalemler.Sum(x => x.nominal), kapanan = kalemler.Sum(x => x.kapanan), oncedenTemlik = kalemler.Sum(x => x.oncedenTemlik), edilebilir = kalemler.Sum(x => x.edilebilir), kilitli = kilit != "", kilitNedeni = kilit, kalemler };
                });
        }

        // ---- kayıtlar ----
        private static readonly List<TmlIslem> _islemler = new List<TmlIslem>(); private static bool _tohumlandi; private static int _sonId = 44;
        public static List<TmlIslem> Islemler { get { Tohumla(); return _islemler; } }
        public static TmlIslem Bul(string no) => Islemler.FirstOrDefault(i => string.Equals(i.No, no, StringComparison.OrdinalIgnoreCase));
        public static TmlIslem Bul(int id) => Islemler.FirstOrDefault(i => i.Id == id);
        private static string N(decimal d, int b = 2) => d.ToString("N" + b, Tr);
        private static string P(decimal d) => d.ToString("0.##", CultureInfo.InvariantCulture);
        private static void Log(TmlIslem i, DateTime z, string k, string alan, string eski, string yeni, string kaynak = "Ekran", string gerekce = null) => i.Audit.Add(new YnAudit { Zaman = z, Kullanici = k, Alan = alan, Eski = eski ?? "", Yeni = yeni ?? "", Kaynak = kaynak, Gerekce = gerekce ?? "" });
        public static string Sha(string s) { using (var h = SHA256.Create()) return string.Concat(h.ComputeHash(Encoding.UTF8.GetBytes(s)).Take(12).Select(b => b.ToString("x2"))); }

        // =====================================================================
        // ZİNCİR KURMA (§4, §7): taraflar → N-1 step; rol, giren / çıkan / kalan hak
        // =====================================================================
        public static void ZinciriKur(TmlIslem i, List<TmlStepGiris> stepGiris)
        {
            var eski = i.Stepler.ToDictionary(s => s.Sira);
            i.Stepler = new List<TmlStep>();
            decimal onceki = i.BuTemlikTutari;
            for (int n = 0; n < i.Taraflar.Count - 1; n++)
            {
                var g = stepGiris != null && n < stepGiris.Count ? stepGiris[n] : null;
                decimal tutar = g?.Tutar ?? (eski.TryGetValue(n + 1, out var e) ? e.Tutar : onceki);
                if (n == 0 && g?.Tutar == null) tutar = i.BuTemlikTutari;   // step 1 = bu işlem temlik tutarı
                var st = new TmlStep { Sira = n + 1, Eden = i.Taraflar[n].Ad, Alan = i.Taraflar[n + 1].Ad, Tutar = Math.Round(tutar, 2), Kur = i.Kur, EtkinlikTarihi = DateTime.TryParseExact(g?.EtkinlikTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : i.EtkinlikTarihi, IdempotencyKey = $"{i.No}-S{n + 1}-v{i.Versiyon}", CorrelationId = $"{i.ZincirNo}/{n + 1}" };
                if (eski.TryGetValue(n + 1, out var e2) && e2.Eden == st.Eden && e2.Alan == st.Alan) { st.Durum = e2.Durum; st.ErpDurum = e2.ErpDurum; st.ErpRef = e2.ErpRef; st.ErpCevap = e2.ErpCevap; st.ErpZamani = e2.ErpZamani; }
                i.Stepler.Add(st); onceki = st.Tutar;
            }
            for (int n = 0; n < i.Taraflar.Count; n++)
            {
                var t = i.Taraflar[n]; t.Sira = n + 1; t.Tip = TarafTipi(t.Ad);
                t.Giren = n == 0 ? 0 : i.Stepler[n - 1].Tutar; t.Cikan = n == i.Taraflar.Count - 1 ? 0 : i.Stepler[n].Tutar;
                t.Rol = i.Taraflar.Count < 2 ? "" : n == 0 ? "İlk Temlik Eden" : n == i.Taraflar.Count - 1 ? "Nihai Temlik Alan" : "Ara Temlik Alan / Devreden";
            }
        }

        // =====================================================================
        // DOĞRULAMA (§18 V01–V23)
        // =====================================================================
        public static YnSonuc Dogrula(TmlIslem i, string rol, bool taslak)
        {
            var s = new YnSonuc(); int n = i.Taraflar.Count;
            if (n < MinTaraf || n > MaxTaraf) s.Hatalar.Add($"V01/V23 Taraf sayısı en az {MinTaraf}, en fazla {MaxTaraf} olmalıdır (şu an {n}).");
            if (n >= 2 && i.Stepler.Count != n - 1) s.Hatalar.Add($"V02 Taraf sayısı {n} ise {n - 1} transfer step oluşmalıdır.");
            for (int k = 1; k < n; k++) if (i.Taraflar[k].Ad == i.Taraflar[k - 1].Ad) s.Hatalar.Add($"V03 Aynı taraf ardışık iki sırada seçilemez ({i.Taraflar[k].Ad}, sıra {k}-{k + 1}).");
            if (n >= 2 && i.Taraflar[0].Ad == i.Taraflar[n - 1].Ad) s.Hatalar.Add("V04 İlk ve son taraf aynı olamaz; döngüsel zincir bloklanır.");
            if (i.Taraflar.Any(t => string.IsNullOrEmpty(t.Ad))) s.Hatalar.Add("Tüm taraf satırları doldurulmalıdır.");
            if (i.Kaynaklar.Count == 0) s.Hatalar.Add("V05 Kaynak alacak / fatura en az bir satır seçilmelidir.");
            foreach (var k in i.Kaynaklar)
            {
                string ad = $"{k.BelgeNo} satır {k.Sira}";
                if (k.BuIslem <= 0) s.Hatalar.Add($"V06 {ad}: bu işlem temlik tutarı sıfırdan büyük olmalıdır.");
                if (k.BuIslem > k.Edilebilir + 0.005m) s.Hatalar.Add($"V06/V07 {ad}: temlik tutarı ({N(k.BuIslem)}) temlik edilebilir bakiyeyi ({N(k.Edilebilir)}; nominal {N(k.Nominal)} − kapanan {N(k.Kapanan)} − rezerve {N(k.OncedenTemlik)}) aşamaz.");
                var ky = KaynakBul(k.KaynakId); if (ky != null && ky.Durum != "Açık") s.Hatalar.Add($"V21 {k.BelgeNo}: kaynak alacak {ky.Durum.ToLower(Tr)}; yeni temlik bloklanır.");
                if (ky != null && ky.Vade < DateTime.Today) s.Uyarilar.Add($"{k.BelgeNo}: alacağın vadesi geçmiş ({ky.Vade:dd.MM.yyyy}); hukuk / tahsilat riski değerlendirilmeli.");
            }
            if (i.Kaynaklar.Count > 0 && i.Kaynaklar.Select(k => KaynakBul(k.KaynakId)?.AsilBorclu).Distinct().Count() > 1) s.Uyarilar.Add("Birden fazla asıl borçlu seçildi; belge her kaynağı ayrı gösterir, başlıktaki asıl borçlu ilk kaynağa göre.");
            for (int k = 0; k < i.Stepler.Count; k++)
            {
                var st = i.Stepler[k]; decimal kul = k == 0 ? i.BuTemlikTutari : i.Stepler[k - 1].Tutar;
                if (st.Tutar <= 0) s.Hatalar.Add($"Step {st.Sira} ({st.Eden} → {st.Alan}): tutar sıfırdan büyük olmalıdır.");
                if (st.Tutar > kul + 0.005m) { if (k > 0 && Parametreler["araTarafFazlaDevir"] == 1 && Yetkili(rol, "istisna") && !string.IsNullOrEmpty(i.IstisnaGerekce)) s.Uyarilar.Add($"V09 Step {st.Sira}: ara taraf aldığından ({N(kul)}) fazlasını ({N(st.Tutar)}) devrediyor; istisna gerekçesi loglandı."); else s.Hatalar.Add($"V08/V09 Step {st.Sira} ({st.Eden} → {st.Alan}): tutar ({N(st.Tutar)}) önceki taraftan kullanılabilir hakkı ({N(kul)}) aşamaz."); }
                if (k > 0 && st.Tutar < kul) s.Uyarilar.Add($"Step {st.Sira}: {st.Eden} aldığı {N(kul)} tutarın {N(st.Tutar)} kısmını devrediyor; {N(kul - st.Tutar)} kalan temlik hakkı {st.Eden} üzerinde açık gösterilir (§7.3).");
                if (i.ParaBirimi != "TRY" && (st.Kur <= 0 || !i.KurTarihi.HasValue)) s.Hatalar.Add($"V15 Step {st.Sira}: dövizli stepte kur ve kur tarihi zorunludur.");
                foreach (var (sirket, yon, karsi) in new[] { (st.Eden, "ASSIGN_OUT", st.Alan), (st.Alan, "ASSIGN_IN", st.Eden) })
                {
                    if (!GrupMu(sirket)) continue;
                    string rolAd = yon == "ASSIGN_OUT" ? (k == 0 ? "Kaynak Alacak" : "Temlik Alacağı") : "Temlik Alacağı", rolKarsi = yon == "ASSIGN_OUT" ? "Temlik Karşı Hak" : "Temlik Borcu / Karşı Cari";
                    if (EslemeBul(sirket, yon, rolAd, TarafTipi(karsi), i.KaynakBelgeTuru, i.Tarih) == null || EslemeBul(sirket, yon, rolKarsi, TarafTipi(karsi), i.KaynakBelgeTuru, i.Tarih) == null) s.Hatalar.Add($"V13 Step {st.Sira} / {sirket} ({yon}): muhasebe hesap eşlemesi eksik; step bloklanır.");
                }
            }
            if (n >= 2 && i.Taraflar.Last().Rol != "Nihai Temlik Alan") s.Hatalar.Add("V10 Nihai Temlik Alan otomatik son taraf olmalıdır.");
            if (i.Tarih < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1)) s.Hatalar.Add("V14 Kapalı döneme kayıt tarihi verilemez (önceki ay kapalı).");
            if (i.EtkinlikTarihi < i.Tarih) s.Uyarilar.Add("Etkinlik tarihi işlem tarihinden önce.");
            if (!taslak) { if (string.IsNullOrWhiteSpace(i.Aciklama)) s.Hatalar.Add("Açıklama zorunludur."); if (string.IsNullOrWhiteSpace(i.NedenKodu)) s.Hatalar.Add("Temlik nedeni (kod + metin) zorunludur."); if (string.IsNullOrEmpty(i.AsilBorclu)) s.Hatalar.Add("Asıl borçlu zorunludur."); }
            if (i.Taraflar.Count > 0 && i.Kaynaklar.Count > 0) { var sahip = KaynakBul(i.Kaynaklar[0].KaynakId)?.AlacakSahibi; if (sahip != null && i.Taraflar[0].Ad != sahip) s.Uyarilar.Add($"İlk temlik eden ({i.Taraflar[0].Ad}) kaynak alacağın sahibi ({sahip}) değil; hukuk kontrolü gerekir."); }
            if (i.BuTemlikTutari * i.Kur > Parametreler["direktorEsik"]) s.Uyarilar.Add($"Temlik tutarı {N(i.BuTemlikTutari * i.Kur, 0)} TL, {N(Parametreler["direktorEsik"], 0)} TL eşiğini aşıyor; Direktör onayı eklenir.");
            if (n >= Parametreler["direktorTaraf"]) s.Uyarilar.Add($"{n} taraflı zincir: Direktör onayı eklenir.");
            return s;
        }
        public static List<YnOnay> OnayAkisi(TmlIslem i)
        {
            var l = new List<YnOnay>();
            var muh = NakitAvansOrnek.Personel.FirstOrDefault(p => p.Rol == "Muhasebe" && p.Sirket == i.KaynakSirket) ?? NakitAvansOrnek.Personel.First(p => p.Rol == "Muhasebe");
            l.Add(new YnOnay { Sira = 1, Adim = "Muhasebe Onayı", Kisi = muh.AdSoyad });
            l.Add(new YnOnay { Sira = 2, Adim = "Hukuk Onayı", Kisi = "Av. Deniz Korkmaz" });
            if (i.BuTemlikTutari * i.Kur > Parametreler["direktorEsik"] || i.TarafSayisi >= Parametreler["direktorTaraf"] || i.Tur == "Teminat Amaçlı Temlik") l.Add(new YnOnay { Sira = 3, Adim = "Direktör Onayı (Mali İşler)", Kisi = NakitAvansOrnek.UstYonetim(muh).AdSoyad });
            l.Add(new YnOnay { Sira = l.Count + 1, Adim = "Finans Onayı", Kisi = NakitAvansOrnek.Finans().AdSoyad });
            return l;
        }
        public static string OnayNedeni(TmlIslem i) { var n = new List<string>(); if (i.BuTemlikTutari * i.Kur > Parametreler["direktorEsik"]) n.Add("tutar eşiği"); if (i.TarafSayisi >= Parametreler["direktorTaraf"]) n.Add($"{i.TarafSayisi} taraflı zincir"); if (i.Tur == "Teminat Amaçlı Temlik") n.Add("teminat amaçlı"); return n.Count == 0 ? "standart akış (Muhasebe → Hukuk → Finans)" : "Direktör: " + string.Join(", ", n); }

        // =====================================================================
        // MUHASEBE SİMÜLASYONU (§8): her step, her taraf; ERP dışı taraflar bilgi satırı
        // =====================================================================
        public static (List<TmlFisSatir> Satirlar, YnSonuc Sonuc) Simule(TmlIslem i)
        {
            var s = new YnSonuc(); var l = new List<TmlFisSatir>();
            if (i.Stepler.Count == 0) { s.Hatalar.Add("Simülasyon için en az 2 taraf ve bir kaynak alacak gerekir."); return (l, s); }
            string borclu = i.AsilBorclu ?? "", kaynakRef = string.Join(", ", i.Kaynaklar.Select(k => k.BelgeNo).Distinct()), refr = $"{i.No ?? "TASLAK"} · {i.ZincirNo ?? ""}";
            TmlFisSatir S(string sirket, int step, string yon, string rol, TmlEsleme e, decimal borc, decimal alacak, string karsi, string acik, decimal kur)
            {
                bool erp = GrupMu(sirket);
                return new TmlFisSatir { Sirket = sirket, Step = step, Erp = erp, Yon = yon, HesapRolu = rol, HesapKodu = e == null ? (erp ? "—" : "kaynak sistem") : HesapKodu(e, borclu, karsi), HesapAdi = e == null ? (erp ? "EŞLEME YOK" : "ERP dışı taraf — bilgi amaçlı") : e.HesapAdi, Borc = Math.Round(borc * kur, 2), Alacak = Math.Round(alacak * kur, 2), ParaBirimi = i.ParaBirimi, Kur = kur, DovizTutar = borc + alacak, KarsiTaraf = karsi, KaynakRef = kaynakRef, Aciklama = acik, Kural = e == null ? "" : $"Eşleme #{e.Id} ({e.Yon} / {e.HesapRolu} / {(e.TarafTipi == "*" ? "tüm taraf tipleri" : e.TarafTipi)}) · {e.BpMode}", Referans = $"{refr} · Step {step}" };
            }
            foreach (var st in i.Stepler)
            {
                bool ilk = st.Sira == 1; string tipAlan = TarafTipi(st.Alan), tipEden = TarafTipi(st.Eden);
                // Temlik eden (ASSIGN_OUT): Borç karşı hak (temlik alan carisi) / Alacak kaynak alacak (ilk) ya da temlik alacağı (ara)
                var eKarsi = EslemeBul(st.Eden, "ASSIGN_OUT", "Temlik Karşı Hak", tipAlan, i.KaynakBelgeTuru, i.Tarih); var eAlacak = EslemeBul(st.Eden, "ASSIGN_OUT", ilk ? "Kaynak Alacak" : "Temlik Alacağı", tipAlan, i.KaynakBelgeTuru, i.Tarih);
                l.Add(S(st.Eden, st.Sira, "ASSIGN_OUT", "Temlik Karşı Hak", eKarsi, st.Tutar, 0, st.Alan, $"Alacak hakkı {st.Alan} tarafına {(ilk ? "devredildi" : "yeniden temlik edildi")}", st.Kur));
                l.Add(S(st.Eden, st.Sira, "ASSIGN_OUT", ilk ? "Kaynak Alacak" : "Temlik Alacağı", eAlacak, 0, st.Tutar, borclu, ilk ? $"Kaynak alacak devredildi (asıl borçlu {borclu})" : $"{st.Eden} üzerindeki temlik hakkı devredildi", st.Kur));
                // Temlik alan (ASSIGN_IN): Borç temlik alacağı / Alacak temlik borcu (temlik eden carisi)
                var eTa = EslemeBul(st.Alan, "ASSIGN_IN", "Temlik Alacağı", tipEden, i.KaynakBelgeTuru, i.Tarih); var eTb = EslemeBul(st.Alan, "ASSIGN_IN", "Temlik Borcu / Karşı Cari", tipEden, i.KaynakBelgeTuru, i.Tarih);
                l.Add(S(st.Alan, st.Sira, "ASSIGN_IN", "Temlik Alacağı", eTa, st.Tutar, 0, borclu, $"Temlik yoluyla alacak hakkı devralındı (asıl borçlu {borclu})", st.Kur));
                l.Add(S(st.Alan, st.Sira, "ASSIGN_IN", "Temlik Borcu / Karşı Cari", eTb, 0, st.Tutar, st.Eden, $"Temlik karşılığı cari / clearing kayıt ({st.Eden})", st.Kur));
                if (GrupMu(st.Eden) && (eKarsi == null || eAlacak == null)) s.Hatalar.Add($"V13 Step {st.Sira} / {st.Eden}: hesap eşlemesi eksik.");
                if (GrupMu(st.Alan) && (eTa == null || eTb == null)) s.Hatalar.Add($"V13 Step {st.Sira} / {st.Alan}: hesap eşlemesi eksik.");
            }
            foreach (var g in l.Where(x => x.Erp).GroupBy(x => new { x.Sirket, x.Step })) { decimal b = g.Sum(x => x.Borc), a = g.Sum(x => x.Alacak); if (Math.Abs(b - a) > 0.01m) s.Hatalar.Add($"V16 {g.Key.Sirket} step {g.Key.Step}: borç ({N(b)}) ≠ alacak ({N(a)})."); }
            foreach (var t in i.Taraflar.Where(t => t.Rol == "Ara Temlik Alan / Devreden")) { if (t.Kalan > 0) s.Uyarilar.Add($"Ara taraf {t.Ad}: giren {N(t.Giren)} − çıkan {N(t.Cikan)} = {N(t.Kalan)} kalan temlik hakkı açık bakiye olarak raporlanır (V17)."); else if (t.Kalan < 0) s.Hatalar.Add($"V17 Ara taraf {t.Ad}: çıkan hak girenden fazla."); }
            if (i.ParaBirimi != "TRY") s.Uyarilar.Add($"Dövizli temlik: her stepin TL karşılığı {i.ParaBirimi} kuru {i.Kur.ToString("N4", Tr)} ({i.KurTarihi:dd.MM.yyyy}, TCMB) ile hesaplandı; kur farkı eşleme #12 ile ayrı izlenir.");
            return (l, s);
        }
        /// <summary>§8.4 konsolide görünüm: taraf bazında giren / çıkan / net hak.</summary>
        public static IEnumerable<object> Konsolide(TmlIslem i) => i.Taraflar.Select(t => new { t.Sira, t.Ad, t.Tip, t.Rol, giren = t.Giren, cikan = t.Cikan, net = t.Giren - t.Cikan, kontrol = t.Rol == "İlk Temlik Eden" ? "Kaynak alacak devri gerçekleşmeli." : t.Rol == "Nihai Temlik Alan" ? "Nihai alacak sahibi olarak kalır." : t.Kalan == 0 ? "Ara temlik alacağı tam devredildi; net sıfır." : $"Ara tarafta {N(t.Kalan)} kalan temlik hakkı açık." });

        // =====================================================================
        // KAYDET / ONAY
        // =====================================================================
        public static (TmlIslem Islem, YnSonuc Sonuc) Kaydet(TmlIslemGiris g, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var s = new YnSonuc();
                var i = g.Id > 0 ? Bul(g.Id) : null; if (g.Id > 0 && i == null) { s.Hatalar.Add("İşlem bulunamadı."); return (null, s); }
                if (i != null && !i.Duzenlenebilir) { s.Hatalar.Add($"V11 {i.Durum} durumundaki işlem düzenlenemez (belge / imza / zincir kilitli); yeni versiyon için geri çekme ya da iptal gerekir."); return (i, s); }
                bool yeni = i == null;
                if (yeni) { i = new TmlIslem { Id = ++_sonId, No = $"TML-{_sonId:00000}", ZincirNo = $"TML-Z-{z:yyyy}-{_sonId:00000}", Olusturan = kullanici, OlusturmaZamani = z }; }
                var eskiTaraf = string.Join(" → ", i.Taraflar.Select(t => t.Ad)); var eskiTutar = i.BuTemlikTutari; var eskiStep = i.Stepler.Select(x => x.Tutar).ToList();
                i.Tarih = DateTime.TryParseExact(g.Tarih ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : (i.Tarih == default ? z.Date : i.Tarih);
                i.EtkinlikTarihi = DateTime.TryParseExact(g.EtkinlikTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var et) ? et : i.Tarih;
                i.KaynakSirket = g.KaynakSirket; i.Tur = string.IsNullOrEmpty(g.Tur) ? "Alacak Temliki" : g.Tur; i.KaynakBelgeTuru = string.IsNullOrEmpty(g.KaynakBelgeTuru) ? "Fatura" : g.KaynakBelgeTuru;
                i.NedenKodu = (g.NedenKodu ?? "").Trim(); i.Neden = (g.Neden ?? "").Trim(); i.Proje = (g.Proje ?? "").Trim(); i.MasrafMerkezi = (g.MasrafMerkezi ?? "").Trim(); i.Aciklama = (g.Aciklama ?? "").Trim(); i.IstisnaGerekce = (g.Gerekce ?? "").Trim();
                // kaynak satırları (tek tek seçilen kalemler)
                i.Kaynaklar = (g.Kaynaklar ?? new List<TmlKaynakGiris>()).Select(k =>
                {
                    var ky = KaynakBul(k.KaynakId); var kl = ky?.Kalemler.FirstOrDefault(x => x.Sira == k.Sira); if (kl == null) { s.Hatalar.Add($"Kaynak satır bulunamadı ({k.KaynakId}/{k.Sira})."); return null; }
                    return new TmlKaynakSatir { KaynakId = ky.Id, BelgeNo = ky.BelgeNo, Sira = kl.Sira, KalemAd = kl.Ad, Nominal = kl.Nominal, Kapanan = kl.Kapanan, OncedenTemlik = OncedenTemlik(ky.Id, kl.Sira, i.Id), BuIslem = Math.Round(k.BuIslem, 2) };
                }).Where(k => k != null).GroupBy(k => k.KaynakId + "/" + k.Sira).Select(x => x.First()).ToList();
                var ilkKaynak = i.Kaynaklar.Count > 0 ? KaynakBul(i.Kaynaklar[0].KaynakId) : null;
                i.AsilBorclu = string.IsNullOrEmpty(g.AsilBorclu) ? (ilkKaynak?.AsilBorclu ?? "") : g.AsilBorclu;
                if (string.IsNullOrEmpty(i.KaynakSirket)) i.KaynakSirket = ilkKaynak?.KaynakSirket;
                i.ParaBirimi = ilkKaynak?.ParaBirimi ?? (g.ParaBirimi ?? "TRY"); i.Kur = i.ParaBirimi == "TRY" ? 1 : (g.Kur ?? ilkKaynak?.Kur ?? 1);
                i.KurTarihi = i.ParaBirimi == "TRY" ? (DateTime?)null : (DateTime.TryParseExact(g.KurTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var kt) ? kt : i.Tarih);
                // taraflar ve stepler
                i.Taraflar = (g.Taraflar ?? new List<TmlTarafGiris>()).Select((tg, n) => new TmlTaraf { Sira = n + 1, Ad = (tg.Ad ?? "").Trim(), Tip = string.IsNullOrEmpty(tg.Tip) ? TarafTipi(tg.Ad) : tg.Tip, ImzaGerekli = tg.ImzaGerekli }).ToList();
                if (i.Taraflar.Count > MaxTaraf) { s.Hatalar.Add($"V23 Taraf sayısı en fazla {MaxTaraf} olabilir."); return (yeni ? null : i, s); }
                ZinciriKur(i, g.Stepler);
                if (!s.Gecerli) return (yeni ? null : i, s);
                var d = Dogrula(i, g.Rol, true); s.Uyarilar.AddRange(d.Uyarilar); s.Hatalar.AddRange(d.Hatalar.Where(h => h.StartsWith("V01") || h.StartsWith("V03") || h.StartsWith("V04") || h.StartsWith("V06/V07") || h.StartsWith("V21") || h.StartsWith("V23")));
                if (!s.Gecerli) return (yeni ? null : i, s);
                if (yeni) Log(i, z, kullanici, "STATUS", "", "Taslak", "Sistem");
                else
                {
                    var yeniTaraf = string.Join(" → ", i.Taraflar.Select(x => x.Ad)); if (eskiTaraf != yeniTaraf) Log(i, z, kullanici, "CHAIN", eskiTaraf, yeniTaraf, "Ekran", g.Gerekce);
                    if (eskiTutar != i.BuTemlikTutari) Log(i, z, kullanici, "ASSIGNED_AMOUNT", P(eskiTutar), P(i.BuTemlikTutari), "Ekran", g.Gerekce);
                    for (int k = 0; k < i.Stepler.Count; k++) if (k >= eskiStep.Count || eskiStep[k] != i.Stepler[k].Tutar) Log(i, z, kullanici, $"STEP_AMOUNT[{k + 1}]", k < eskiStep.Count ? P(eskiStep[k]) : "", P(i.Stepler[k].Tutar), "Ekran", g.Gerekce);
                    if (i.Durum == "Reddedildi") { i.Durum = "Taslak"; i.Versiyon++; i.Onaylar.Clear(); i.RedNedeni = null; Log(i, z, kullanici, "STATUS", "Reddedildi", $"Taslak (revize, v{i.Versiyon})"); }
                }
                Log(i, z, kullanici, "RESERVATION", "", $"{N(i.BuTemlikTutari)} {i.ParaBirimi} rezerve · {i.Kaynaklar.Count} kaynak satır", "Sistem");
                i.Guncelleyen = kullanici; i.GuncellemeZamani = z;
                if (yeni) _islemler.Insert(0, i);
                return (i, s);
            }
        }
        public static YnSonuc OnayaGonder(TmlIslem i, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var s = Dogrula(i, rol, false);
                if (i.Durum != "Taslak") s.Hatalar.Insert(0, $"Yalnız Taslak durumundaki işlem onaya gönderilebilir ({i.Durum}).");
                foreach (var k in i.Kaynaklar) { k.OncedenTemlik = OncedenTemlik(k.KaynakId, k.Sira, i.Id); if (k.BuIslem > k.Edilebilir + 0.005m) s.Hatalar.Add($"V07 {k.BelgeNo} satır {k.Sira}: eşzamanlı rezervasyon sonrası edilebilir bakiye {N(k.Edilebilir)}; talep {N(k.BuIslem)}."); }
                var (sim, ss) = Simule(i); s.Hatalar.AddRange(ss.Hatalar); s.Uyarilar.AddRange(ss.Uyarilar);
                if (!s.Gecerli) return s;
                i.Simulasyon = sim; i.SimulasyonTarihi = z; i.SimulasyonVersiyonu++; i.SimulasyonHash = Sha(string.Join("|", sim.Select(x => x.Sirket + x.Step + x.HesapKodu + x.Borc + x.Alacak)));
                i.Onaylar = OnayAkisi(i); i.Durum = "Onay Bekliyor"; i.Guncelleyen = kullanici; i.GuncellemeZamani = z;
                Log(i, z, kullanici, "STATUS", "Taslak", "Onay Bekliyor", "Workflow", OnayNedeni(i)); Log(i, z, kullanici, "SIM_SNAPSHOT", "", $"v{i.SimulasyonVersiyonu} · {sim.Count} satır · hash {i.SimulasyonHash}", "Sistem");
                return s;
            }
        }
        public static string Onayla(TmlIslem i, string eylem, string not, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "onay")) return "Bu rol onay veremez.";
                if (i.Durum != "Onay Bekliyor") return $"İşlem onay sürecinde değil ({i.Durum})."; var adim = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor"); if (adim == null) return "Bekleyen adım yok.";
                if (eylem == "Reddet") { if (string.IsNullOrWhiteSpace(not)) return "Ret nedeni zorunludur."; adim.Durum = "Reddedildi"; adim.Tarih = z; adim.Not = not; i.Durum = "Reddedildi"; i.RedNedeni = not; Log(i, z, kullanici, "STATUS", "Onay Bekliyor", "Reddedildi", "Workflow", not); return null; }
                adim.Durum = "Onaylandı"; adim.Tarih = z; adim.Not = not ?? ""; Log(i, z, kullanici, "APPROVAL", adim.Adim, "Onaylandı", "Workflow", not);
                if (i.Onaylar.All(o => o.Durum == "Onaylandı")) { i.Durum = "Onaylandı"; Log(i, z, kullanici, "STATUS", "Onay Bekliyor", "Onaylandı", "Workflow"); }
                i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }
        public static string GeriCek(TmlIslem i, string rol, string kullanici, DateTime? simdi = null) { lock (Kilit) { var z = simdi ?? DateTime.Now; if (i.Durum != "Onay Bekliyor") return "Yalnız onay bekleyen işlem geri çekilebilir."; i.Durum = "Taslak"; i.Onaylar.Clear(); i.Versiyon++; Log(i, z, kullanici, "STATUS", "Onay Bekliyor", $"Taslak (geri çekildi, v{i.Versiyon})", "Workflow"); return null; } }

        // =====================================================================
        // TEMLİK BELGESİ (§9): üretim, ön izleme verisi, imza, yükleme, geçersiz kılma, bildirim
        // =====================================================================
        public static (string Hata, TmlBelge Belge) BelgeOlustur(TmlIslem i, string rol, string kullanici, bool docx, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now;
                if (!Yetkili(rol, "belgeOlustur")) return ("Bu rol belge üretemez.", null);
                if (!(i.Durum == "Onaylandı" || i.Durum == "Belge Üretildi" || i.Durum == "İmzaya Gönderildi" || i.Durum == "Kısmi İmzalı")) return ($"Temlik Belgesi yalnız onaylanmış işlemde üretilir ({i.Durum}).", null);
                if (i.TamImzali) return ("Tam imzalı belge üzerinde yeni versiyon üretilemez; iptal / yeni temlik süreci gerekir (V20).", null);
                var eksik = i.Taraflar.Where(t => { var m = Master(t.Ad); return m == null || string.IsNullOrEmpty(m.Vkn) || string.IsNullOrEmpty(m.Adres) || string.IsNullOrEmpty(m.Yetkili); }).Select(t => t.Ad).ToList();
                if (eksik.Count > 0) return ($"V22 Taraf master bilgisi eksik (unvan / vergi no / adres / yetkili): {string.Join(", ", eksik)}; belge üretimi bloklandı.", null);
                foreach (var b in i.Belgeler.Where(b => b.Durum != "Geçersiz")) { b.Durum = "Geçersiz"; b.GecersizNedeni = "Yeni versiyon üretildi"; Log(i, z, kullanici, "DOCUMENT", $"{b.BelgeNo} v{b.Versiyon}", "Geçersiz (yeni versiyon)", "Belge"); }
                var v = i.Belgeler.Count + 1;
                var belge = new TmlBelge { Versiyon = v, BelgeNo = $"TB-{z:yyyy}-{i.Id:00000}-{v}", OlusturmaZamani = z, Olusturan = kullanici, SablonVersiyonu = SablonVersiyonu, Docx = docx && (Yetkili(rol, "sablon") || Yetkili(rol, "esleme")) };
                belge.Imzalar = i.Taraflar.Select(t => { var m = Master(t.Ad); return new TmlImza { Sira = t.Sira, Taraf = t.Ad, Rol = t.Rol == "Ara Temlik Alan / Devreden" ? "Temlik Alan + Devreden" : t.Rol, Imzaci = m?.Yetkili ?? "Yetkili", Unvan = m?.YetkiliUnvan ?? "Şirket Yetkilisi", Zorunlu = t.ImzaGerekli }; }).ToList();
                if (Parametreler["borcluImzasi"] == 1) { var m = Master(i.AsilBorclu); belge.Imzalar.Add(new TmlImza { Sira = belge.Imzalar.Count + 1, Taraf = i.AsilBorclu, Rol = "Asıl Borçlu Kabul / Bildirim", Imzaci = m?.Yetkili ?? "Yetkili", Unvan = m?.YetkiliUnvan ?? "", Zorunlu = true }); }
                belge.Hash = Sha($"{belge.BelgeNo}|{i.No}|{i.ZincirNo}|{string.Join(">", i.Taraflar.Select(t => t.Ad))}|{string.Join(",", i.Stepler.Select(st => st.Tutar))}|{string.Join(",", i.Kaynaklar.Select(k => k.BelgeNo + ":" + k.BuIslem))}|{SablonVersiyonu}");
                i.Belgeler.Add(belge); if (i.Durum == "Onaylandı" || i.Durum == "İmzaya Gönderildi" || i.Durum == "Kısmi İmzalı") { Log(i, z, kullanici, "STATUS", i.Durum, "Belge Üretildi", "Belge"); i.Durum = "Belge Üretildi"; }
                Log(i, z, kullanici, "DOCUMENT", "", $"{belge.BelgeNo} v{v} üretildi · {belge.Imzalar.Count} imza bloğu · hash {belge.Hash} · {SablonVersiyonu}{(belge.Docx ? " · DOCX taslak" : "")}", "Belge");
                i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return (null, belge);
            }
        }
        public static string ImzayaGonder(TmlIslem i, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "imzayaGonder")) return "Bu rol imzaya gönderemez."; var b = i.AktifBelge; if (b == null) return "Önce Temlik Belgesi oluşturulmalı.";
                if (b.ImzayaGonderim.HasValue) return $"Belge zaten imzada ({b.ImzayaGonderim:dd.MM.yyyy HH:mm}).";
                b.ImzayaGonderim = z; b.Durum = "İmzada"; Log(i, z, kullanici, "STATUS", i.Durum, "İmzaya Gönderildi", "Belge"); i.Durum = "İmzaya Gönderildi";
                Log(i, z, kullanici, "SIGNATURE", "", $"{b.BelgeNo}: {b.Imzalar.Count} imza sahibine e-imza talebi gönderildi", "E-İmza"); i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }
        public static string Imzala(TmlIslem i, int sira, string eylem, string yontem, string not, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var b = i.AktifBelge; if (b == null) return "Aktif belge yok."; if (!b.ImzayaGonderim.HasValue) return "Belge imzaya gönderilmedi.";
                var im = b.Imzalar.FirstOrDefault(x => x.Sira == sira); if (im == null) return "İmza bloğu bulunamadı."; if (im.Durum == "İmzalandı") return $"{im.Taraf} zaten imzaladı.";
                im.Durum = eylem == "Reddet" ? "Reddedildi" : "İmzalandı"; im.Tarih = z; im.Yontem = yontem ?? "E-İmza";
                Log(i, z, im.Imzaci, "SIGNATURE", im.Taraf, $"{im.Durum} ({im.Yontem}){(string.IsNullOrEmpty(not) ? "" : " · " + not)}", yontem == "Islak İmza" ? "Ekran" : "E-İmza callback");
                if (im.Durum == "Reddedildi") { b.Durum = "Reddedildi"; Log(i, z, kullanici, "STATUS", i.Durum, "Belge Üretildi (imza reddi; yeni versiyon gerekir)", "Belge"); i.Durum = "Belge Üretildi"; b.ImzayaGonderim = null; foreach (var x in b.Imzalar) { x.Durum = "Bekliyor"; x.Tarih = null; } return null; }
                ImzaDurumunuGuncelle(i, z, kullanici); return null;
            }
        }
        private static void ImzaDurumunuGuncelle(TmlIslem i, DateTime z, string kullanici)
        {
            var b = i.AktifBelge; string yeni = i.TamImzali ? "Tam İmzalı" : b.Imzalar.Any(x => x.Durum == "İmzalandı") ? "Kısmi İmzalı" : i.Durum;
            b.Durum = i.TamImzali ? "Tam İmzalı" : b.Imzalar.Any(x => x.Durum == "İmzalandı") ? "Kısmi İmzalı" : b.Durum;
            if (yeni != i.Durum && (i.Durum == "İmzaya Gönderildi" || i.Durum == "Kısmi İmzalı" || i.Durum == "Belge Üretildi")) { Log(i, z, kullanici, "STATUS", i.Durum, yeni, "Belge"); i.Durum = yeni; }
            if (i.TamImzali) Log(i, z, kullanici, "ARCHIVE", "", $"{b.BelgeNo} tam imzalı olarak arşivlendi (Temlik {i.No}, Zincir {i.ZincirNo}); hash {b.DosyaHash ?? b.Hash}", "Arşiv");
        }
        public static string ImzaliBelgeYukle(TmlIslem i, string dosya, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "belgeYukle")) return "Bu rol belge yükleyemez."; var b = i.AktifBelge; if (b == null) return "Aktif belge yok."; if (string.IsNullOrWhiteSpace(dosya)) return "Dosya seçilmedi.";
                if (!b.ImzayaGonderim.HasValue) { b.ImzayaGonderim = z; Log(i, z, kullanici, "STATUS", i.Durum, "İmzaya Gönderildi (manuel imza)", "Belge"); i.Durum = "İmzaya Gönderildi"; }
                b.YuklenenDosya = dosya; b.DosyaHash = Sha(dosya + "|" + b.Hash + "|" + z.Ticks);
                foreach (var im in b.Imzalar.Where(x => x.Durum != "İmzalandı")) { im.Durum = "İmzalandı"; im.Tarih = z; im.Yontem = "Islak İmza (yüklendi)"; }
                Log(i, z, kullanici, "SIGNATURE_UPLOAD", "", $"{dosya} · dosya hash {b.DosyaHash} · imza kontrol listesi tamam", "Ekran"); ImzaDurumunuGuncelle(i, z, kullanici); return null;
            }
        }
        public static string BelgeGecersizKil(TmlIslem i, string neden, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "belgeGecersiz") && !Yetkili(rol, "belgeOlustur")) return "Yetki yok."; var b = i.AktifBelge; if (b == null) return "Aktif belge yok."; if (string.IsNullOrWhiteSpace(neden)) return "Neden zorunludur.";
                if (i.TamImzali && i.Stepler.Any(s => s.ErpDurum == "Posted")) return "Muhasebeleşmiş tam imzalı belge geçersiz kılınamaz; ters kayıt süreci gerekir.";
                b.Durum = "Geçersiz"; b.GecersizNedeni = neden; Log(i, z, kullanici, "DOCUMENT", $"{b.BelgeNo} v{b.Versiyon}", "Geçersiz", "Belge", neden);
                if (i.Durum == "Belge Üretildi" || i.Durum == "İmzaya Gönderildi" || i.Durum == "Kısmi İmzalı" || i.Durum == "Tam İmzalı") { Log(i, z, kullanici, "STATUS", i.Durum, "Onaylandı (belge geçersiz; yeni versiyon gerekir)", "Belge"); i.Durum = "Onaylandı"; }
                return null;
            }
        }
        public static string BildirimOlustur(TmlIslem i, string rol, string kullanici, DateTime? simdi = null) { lock (Kilit) { var z = simdi ?? DateTime.Now; var b = i.AktifBelge; if (b == null) return "Önce Temlik Belgesi oluşturulmalı."; b.BorcluBildirimi = true; b.BildirimTarihi = z; Log(i, z, kullanici, "NOTIFICATION", "", $"Asıl borçlu {i.AsilBorclu} için temlik bildirimi üretildi (TBK m.186)", "Belge"); return null; } }

        // =====================================================================
        // ERP MUHASEBELEŞTİRME (§16): step bazlı, idempotent, kısmi entegrasyon ve yalnız başarısız step tekrarı
        // =====================================================================
        public static string Muhasebelestir(TmlIslem i, string senaryo, string rol, string kullanici, string istisnaGerekce, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "muhasebelestir")) return "Bu rol muhasebeleştiremez (Muhasebe / Yönetici).";
                if (!i.Onaylandi) return "Onay akışı tamamlanmadan muhasebeleştirilemez.";
                if (!i.TamImzali) { if (Yetkili(rol, "istisna") && !string.IsNullOrWhiteSpace(istisnaGerekce)) { i.IstisnaGerekce = istisnaGerekce; Log(i, z, kullanici, "EXCEPTION", "İmza " + i.ImzaDurumu, "İstisna yetkisiyle muhasebeleştirme", "Workflow", istisnaGerekce); } else return $"V12 Muhasebeleştirme için zorunlu imzalar tamamlanmalı (imza {i.ImzaDurumu}); istisna yalnız Yönetici yetkisi ve gerekçe ile."; }
                foreach (var k in i.Kaynaklar) { k.OncedenTemlik = OncedenTemlik(k.KaynakId, k.Sira, i.Id); if (k.BuIslem > k.Edilebilir + 0.005m) return $"V07 {k.BelgeNo} satır {k.Sira}: temlik edilebilir bakiye yeniden doğrulandı ({N(k.Edilebilir)}); işlem aşıyor."; }
                if (i.Simulasyon.Count == 0) return "Simülasyon snapshot'ı yok.";
                senaryo = senaryo ?? "uyumlu"; int islenen = 0; string hataStep = null;
                foreach (var st in i.Stepler)
                {
                    if (st.ErpDurum == "Posted") continue;                                                                                 // V18 idempotent: tamamlanan step tekrar gönderilmez
                    if (hataStep != null) { st.ErpDurum = "NotStarted"; st.Durum = "Bloke"; continue; }                                    // V19 önceki step başarısız → sonraki işlenmez
                    st.ErpDurum = "Processing"; st.ErpZamani = z.AddSeconds(islenen * 3);
                    if (senaryo == "step2hata" && st.Sira == 2) { st.ErpDurum = "Failed"; st.Durum = "Hata"; st.ErpCevap = "SAP B1 -5002: Cari kart bloke / bütçe kontrolü (JournalEntries) — step tekrar denenmeli"; hataStep = st.Sira.ToString(); Log(i, st.ErpZamani.Value, kullanici, $"ERP_STEP[{st.Sira}]", "Processing", "Failed · " + st.ErpCevap, "ERP", "idempotency " + st.IdempotencyKey); continue; }
                    st.ErpDurum = "Posted"; st.Durum = "Muhasebeleşti"; st.ErpRef = $"JE-{Kisa(GrupMu(st.Alan) ? st.Alan : st.Eden)}-{(72000 + i.Id * 9 + st.Sira):00000}"; st.ErpCevap = $"200 OK · JournalEntry {st.ErpRef} · {st.ErpZamani:dd.MM.yyyy HH:mm}";
                    Log(i, st.ErpZamani.Value, kullanici, $"ERP_STEP[{st.Sira}]", "Processing", "Posted · " + st.ErpRef, "ERP", $"correlation {st.CorrelationId} · idempotency {st.IdempotencyKey}");
                    GercekUret(i, st, senaryo); islenen++;
                }
                i.GercekTarihi = z;
                string eski = i.Durum; i.Durum = i.ErpDurumu == "Completed" ? "Tamamlandı" : "Kısmi Muhasebeleşti";
                if (eski != i.Durum) Log(i, z, kullanici, "STATUS", eski, i.Durum, "ERP");
                Kontrol(i, kullanici, z); i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }
        /// <summary>ERP'den gelen gerçek satırlar (örnek: snapshot'tan step bazında üretilir; senaryo ile sapma).</summary>
        private static void GercekUret(TmlIslem i, TmlStep st, string senaryo)
        {
            var l = i.Simulasyon.Where(x => x.Step == st.Sira && x.Erp).Select(x => new TmlFisSatir { Sirket = x.Sirket, Step = x.Step, Erp = true, FisNo = $"{st.ErpRef}/{Kisa(x.Sirket)}", Yon = x.Yon, HesapRolu = x.HesapRolu, HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Borc = x.Borc, Alacak = x.Alacak, ParaBirimi = x.ParaBirimi, Kur = x.Kur, DovizTutar = x.DovizTutar, KarsiTaraf = x.KarsiTaraf, KaynakRef = x.KaynakRef, Aciklama = x.Aciklama, Referans = x.Referans }).ToList();
            switch (senaryo)
            {
                case "hesap": { var g = l.FirstOrDefault(x => x.Yon == "ASSIGN_IN" && x.HesapRolu == "Temlik Alacağı"); if (g != null) { g.HesapKodu = "120.01." + Kisa(i.AsilBorclu); g.HesapAdi = "Alıcılar (yanlış hesap: temlik alacağı yerine doğrudan alıcı)"; } break; }
                case "tutar": { var son = i.Stepler.Count; if (st.Sira == son) { var fark = Math.Round(st.TutarTl * 0.02m, 2); foreach (var x in l.Where(x => x.Yon == "ASSIGN_IN")) { if (x.Borc > 0) x.Borc -= fark; else x.Alacak -= fark; } } break; }
                case "mukerrer": { if (st.Sira == 1) l.AddRange(l.Where(x => x.Yon == "ASSIGN_IN").Select(x => new TmlFisSatir { Sirket = x.Sirket, Step = x.Step, Erp = true, FisNo = x.FisNo.Replace("JE-", "JE2-"), Yon = x.Yon, HesapRolu = x.HesapRolu, HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Borc = x.Borc, Alacak = x.Alacak, ParaBirimi = x.ParaBirimi, Kur = x.Kur, DovizTutar = x.DovizTutar, KarsiTaraf = x.KarsiTaraf, KaynakRef = x.KaynakRef, Aciklama = x.Aciklama, Referans = x.Referans }).ToList()); break; }
                case "nihai": { if (st.Sira == i.Stepler.Count) { var yanlis = Sirketler.First(s => s != st.Alan && s != st.Eden); foreach (var x in l.Where(x => x.Yon == "ASSIGN_OUT")) { x.KarsiTaraf = yanlis; x.HesapKodu = x.HesapKodu.Replace(Kisa(st.Alan), Kisa(yanlis)); } } break; }
                case "aratarafFark": { if (st.Sira == 2) { var fark = Math.Round(st.TutarTl * 0.1m, 2); foreach (var x in l.Where(x => x.Yon == "ASSIGN_OUT")) { if (x.Borc > 0) x.Borc -= fark; else x.Alacak -= fark; } } break; }
                case "referans": { foreach (var x in l) x.Referans = st.ErpRef; break; }
                case "kur": { foreach (var x in l) { x.Kur = Math.Round(x.Kur * 1.008m, 4); x.Borc = x.Borc > 0 ? Math.Round(x.DovizTutar * x.Kur, 2) : 0; x.Alacak = x.Alacak > 0 ? Math.Round(x.DovizTutar * x.Kur, 2) : 0; } break; }
            }
            i.Gercek.RemoveAll(x => x.Step == st.Sira); i.Gercek.AddRange(l);
        }
        public static string TersKayit(TmlIslem i, string neden, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "tersKayit")) return "Ters kayıt yalnız Muhasebe / Yönetici yetkisiyle başlatılır."; if (string.IsNullOrWhiteSpace(neden)) return "Ters kayıt nedeni zorunludur.";
                var posted = i.Stepler.Where(s => s.ErpDurum == "Posted").OrderByDescending(s => s.Sira).ToList(); if (posted.Count == 0) return "Muhasebeleşmiş step yok; iptal kullanın.";
                int n = 0; foreach (var st in posted) { st.ErpDurum = "Reversed"; st.Durum = "Ters Kayıt"; st.ErpCevap = $"Ters kayıt JE-R-{st.ErpRef?.Split('-').Last()} · {z.AddSeconds(n * 3):dd.MM.yyyy HH:mm}"; Log(i, z.AddSeconds(n * 3), kullanici, $"ERP_REVERSE[{st.Sira}]", st.ErpRef, "Reversed (son stepten ilk stepe)", "ERP", neden); n++; }
                foreach (var st in i.Stepler.Where(s => s.ErpDurum != "Reversed")) { st.Durum = "İptal"; }
                i.Gercek = i.Gercek.Concat(i.Gercek.Where(x => posted.Any(p => p.Sira == x.Step)).Select(x => new TmlFisSatir { Sirket = x.Sirket, Step = x.Step, Erp = true, FisNo = "R-" + x.FisNo, Yon = x.Yon, HesapRolu = x.HesapRolu + " (ters)", HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Borc = x.Alacak, Alacak = x.Borc, ParaBirimi = x.ParaBirimi, Kur = x.Kur, DovizTutar = x.DovizTutar, KarsiTaraf = x.KarsiTaraf, KaynakRef = x.KaynakRef, Aciklama = "TERS KAYIT · " + x.Aciklama, Referans = x.Referans }).ToList()).ToList();
                Log(i, z, kullanici, "STATUS", i.Durum, "Ters Kayıt", "ERP", neden); i.Durum = "Ters Kayıt"; i.IptalNedeni = neden; i.KontrolDurumu = null; i.Kontroller.Clear(); i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }
        public static string Iptal(TmlIslem i, string neden, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (string.IsNullOrWhiteSpace(neden)) return "İptal nedeni zorunludur."; if (!i.Aktif) return "İşlem zaten kapalı.";
                if (i.Stepler.Any(s => s.ErpDurum == "Posted")) return "Muhasebeleşmiş step var: iptal yerine ters kayıt süreci (son stepten ilk stepe) başlatılmalıdır.";
                if (i.TamImzali && !Yetkili(rol, "tersKayit") && rol != "Hukuk") return "Tam imzalı işlem yalnız Hukuk / Mali İşler yetkisiyle iptal edilir; belge arşivde kalır.";
                var b = i.AktifBelge; if (b != null) { b.Durum = "Geçersiz"; b.GecersizNedeni = "İşlem iptal: " + neden; Log(i, z, kullanici, "DOCUMENT", $"{b.BelgeNo} v{b.Versiyon}", i.TamImzali ? "Arşivde (işlem iptal)" : "Geçersiz · imza süreci sonlandırıldı", "Belge"); }
                Log(i, z, kullanici, "STATUS", i.Durum, "İptal", "Ekran", neden); Log(i, z, kullanici, "RESERVATION", $"{N(i.BuTemlikTutari)} rezerve", "serbest bırakıldı", "Sistem");
                i.Durum = "İptal"; i.IptalNedeni = neden; i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }

        // =====================================================================
        // MUHASEBE KONTROLÜ (§11): step bazlı beklenen–gerçekleşen + zincir bütünlüğü
        // =====================================================================
        public static string Kontrol(TmlIslem i, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var K = new List<YnKontrol>(); var d = new List<string>();
                if (i.Stepler.All(s => s.ErpDurum == "NotStarted")) return "ERP muhasebeleştirme başlamadan kontrol yapılamaz.";
                void Ek(string alan, string sirket, string b, string g, string sonuc, bool kritik = true) { K.Add(new YnKontrol { Alan = alan, Sirket = sirket, Beklenen = b, Gerceklesen = g, Sonuc = sonuc, Kritik = kritik }); if (sonuc != "Uyumlu") d.Add(sonuc); }
                string T(decimal v) => v.ToString("N2", Tr);
                // step bazlı
                for (int n = 0; n < i.Stepler.Count; n++)
                {
                    var st = i.Stepler[n]; string ad = $"Step {st.Sira} · {st.Eden} → {st.Alan}";
                    if (st.ErpDurum == "Reversed") { Ek("ERP Step", ad, "Posted", "Reversed", "Kontrol Gerekli", false); continue; }
                    if (st.ErpDurum != "Posted") { bool onceHata = n > 0 && i.Stepler[n - 1].ErpDurum != "Posted"; Ek("ERP Step", ad, "Posted", st.ErpDurum + (st.ErpCevap != null && st.ErpDurum == "Failed" ? " · " + st.ErpCevap : ""), st.ErpDurum == "Failed" ? "Kısmi Entegrasyon" : onceHata ? "Kısmi Entegrasyon" : "Muhasebe Kaydı Oluşmamış"); continue; }
                    if (n > 0 && i.Stepler[n - 1].ErpDurum != "Posted") Ek("Zincir Sırası", ad, "önceki step Posted", i.Stepler[n - 1].ErpDurum, "Zincir Kırık");
                    foreach (var sirket in i.Simulasyon.Where(x => x.Step == st.Sira && x.Erp).Select(x => x.Sirket).Distinct())
                    {
                        var b = i.Simulasyon.Where(x => x.Step == st.Sira && x.Sirket == sirket).ToList(); var g = i.Gercek.Where(x => x.Step == st.Sira && x.Sirket == sirket && !x.FisNo.StartsWith("R-")).ToList();
                        string sc = $"{sirket} · Step {st.Sira}";
                        if (g.Count == 0) { Ek("Muhasebe Fişi", sc, $"{b.Count} satır", "kayıt yok", "Muhasebe Kaydı Oluşmamış"); continue; }
                        var fisler = g.Select(x => x.FisNo).Distinct().ToList(); Ek("Mükerrer Fiş", sc, "1 aktif kayıt", string.Join(", ", fisler), fisler.Count > 1 ? "Mükerrer Kayıt" : "Uyumlu");
                        decimal gb = g.Sum(x => x.Borc) / fisler.Count, ga = g.Sum(x => x.Alacak) / fisler.Count; Ek("Borç = Alacak", sc, $"{T(b.Sum(x => x.Borc))} / {T(b.Sum(x => x.Alacak))}", $"{T(gb)} / {T(ga)}", Math.Abs(gb - ga) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Tutar Farkı");
                        foreach (var x in b)
                        {
                            var y = g.FirstOrDefault(q => q.HesapRolu == x.HesapRolu);
                            Ek($"Hesap · {x.HesapRolu}", sc, x.HesapKodu, y?.HesapKodu ?? "—", y != null && y.HesapKodu == x.HesapKodu ? "Uyumlu" : "Hesap Hatası");
                            decimal bt = x.Borc + x.Alacak, gt = y == null ? 0 : y.Borc + y.Alacak; Ek($"Tutar · {x.HesapRolu}", sc, $"B {T(x.Borc)} / A {T(x.Alacak)}", y == null ? "—" : $"B {T(y.Borc)} / A {T(y.Alacak)}", Math.Abs(bt - gt) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Tutar Farkı");
                            if (x.HesapRolu.Contains("Karşı")) Ek("Karşı Taraf", sc, x.KarsiTaraf, y?.KarsiTaraf ?? "—", y != null && y.KarsiTaraf == x.KarsiTaraf ? "Uyumlu" : (st.Sira == i.Stepler.Count && x.Yon == "ASSIGN_OUT" ? "Nihai Hak Sahibi Farkı" : "Hesap Hatası"));
                        }
                        Ek("Kaynak Alacak Ref", sc, b[0].KaynakRef, g[0].KaynakRef, b[0].KaynakRef == g[0].KaynakRef ? "Uyumlu" : "Kontrol Gerekli", false);
                        Ek("PB / Kur", sc, $"{b[0].ParaBirimi} {b[0].Kur.ToString("N4", Tr)}", $"{g[0].ParaBirimi} {g[0].Kur.ToString("N4", Tr)}", b[0].ParaBirimi == g[0].ParaBirimi && (b[0].Kur == 0 || Math.Abs(g[0].Kur - b[0].Kur) / b[0].Kur * 100m <= Parametreler["kurTolerans"]) ? "Uyumlu" : "Kontrol Gerekli", false);
                        bool refOk = g.All(x => (x.Referans ?? "").Contains(i.No) && (x.Referans ?? "").Contains("Step " + st.Sira)); Ek("Temlik / Zincir / Step ID", sc, $"{i.No} · {i.ZincirNo} · Step {st.Sira}", g[0].Referans ?? "—", refOk ? "Uyumlu" : "Kontrol Gerekli", false);
                    }
                    // grup şirketleri arası mirror: eden karşı hak (borç) = alan temlik borcu (alacak)
                    if (GrupMu(st.Eden) && GrupMu(st.Alan))
                    {
                        var a = i.Gercek.FirstOrDefault(x => x.Step == st.Sira && x.Sirket == st.Eden && x.HesapRolu == "Temlik Karşı Hak" && !x.FisNo.StartsWith("R-")); var bo = i.Gercek.FirstOrDefault(x => x.Step == st.Sira && x.Sirket == st.Alan && x.HesapRolu == "Temlik Borcu / Karşı Cari" && !x.FisNo.StartsWith("R-"));
                        if (a != null && bo != null) Ek("Mirror Cari (grup)", ad, $"{T(a.Borc)} alacak hakkı ({a.HesapKodu})", $"{T(bo.Alacak)} borç ({bo.HesapKodu})", Math.Abs(a.Borc - bo.Alacak) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Tutar Farkı");
                    }
                }
                // zincir bütünlüğü (§11.3)
                string zc = "Zincir"; Ek("Step sayısı", zc, $"Taraf sayısı − 1 = {i.TarafSayisi - 1}", i.StepSayisi.ToString(), i.StepSayisi == i.TarafSayisi - 1 ? "Uyumlu" : "Zincir Kırık");
                for (int n = 1; n < i.Stepler.Count; n++)
                {
                    var onc = i.Stepler[n - 1]; var st = i.Stepler[n]; var ara = i.Taraflar[n];
                    decimal gerc = i.Gercek.Where(x => x.Step == st.Sira && x.Sirket == st.Eden && x.Yon == "ASSIGN_OUT" && x.HesapRolu == "Temlik Karşı Hak" && !x.FisNo.StartsWith("R-")).Select(x => x.Borc).DefaultIfEmpty(GrupMu(st.Eden) ? 0 : st.TutarTl).First();
                    decimal bekl = Math.Round(st.TutarTl, 2);
                    Ek($"Step {onc.Sira} çıkan → Step {st.Sira} giren", zc, $"{T(onc.TutarTl)} → {T(bekl)} (ara taraf kalanı {T((onc.Tutar - st.Tutar) * i.Kur)})", st.ErpDurum == "Posted" ? $"{T(onc.TutarTl)} → {T(gerc)}" : "step işlenmedi", st.ErpDurum != "Posted" ? "Kontrol Gerekli" : Math.Abs(gerc - bekl) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Ara Taraf Farkı");
                    Ek($"Ara taraf net hakkı · {ara.Ad}", zc, T(ara.Kalan * i.Kur), st.ErpDurum == "Posted" ? T(onc.TutarTl - gerc) : "—", st.ErpDurum != "Posted" ? "Kontrol Gerekli" : Math.Abs((onc.TutarTl - gerc) - ara.Kalan * i.Kur) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Ara Taraf Farkı");
                }
                var son = i.Stepler.Last(); var sonGer = i.Gercek.FirstOrDefault(x => x.Step == son.Sira && x.Sirket == son.Eden && x.Yon == "ASSIGN_OUT" && x.HesapRolu == "Temlik Karşı Hak" && !x.FisNo.StartsWith("R-"));
                string gercNihai = sonGer?.KarsiTaraf ?? (son.ErpDurum == "Posted" ? (GrupMu(son.Eden) ? "—" : son.Alan) : "—");
                Ek("Nihai temlik alan", zc, i.NihaiAlan, gercNihai, son.ErpDurum != "Posted" ? "Kontrol Gerekli" : gercNihai == i.NihaiAlan ? "Uyumlu" : "Nihai Hak Sahibi Farkı");
                Ek("Nihai hak tutarı", zc, T(i.NihaiTutar * i.Kur), sonGer != null ? T(sonGer.Borc) : (son.ErpDurum == "Posted" ? T(son.TutarTl) : "—"), son.ErpDurum != "Posted" ? "Kontrol Gerekli" : Math.Abs((sonGer?.Borc ?? son.TutarTl) - i.NihaiTutar * i.Kur) <= Parametreler["tutarTolerans"] ? "Uyumlu" : "Tutar Farkı");
                foreach (var k in i.Kaynaklar) { decimal aktif = OncedenTemlik(k.KaynakId, k.Sira, 0); Ek($"Kaynak aktif temlik · {k.BelgeNo}/{k.Sira}", zc, $"≤ {T(k.Nominal - k.Kapanan)}", T(aktif), aktif <= k.Nominal - k.Kapanan + 0.005m ? "Uyumlu" : "Tutar Farkı"); }
                var b2 = i.AktifBelge; Ek("Tüm zorunlu imzalar", zc, $"{(b2?.Imzalar.Count(x => x.Zorunlu) ?? i.TarafSayisi)}/{(b2?.Imzalar.Count(x => x.Zorunlu) ?? i.TarafSayisi)}", i.ImzaDurumu, i.TamImzali ? "Uyumlu" : "İmza Eksik");
                int posted = i.Stepler.Count(s => s.ErpDurum == "Posted"); Ek("ERP step tamamlanma", zc, $"{i.StepSayisi}/{i.StepSayisi}", $"{posted}/{i.StepSayisi}", posted == i.StepSayisi ? "Uyumlu" : "Kısmi Entegrasyon");
                string eski = i.KontrolDurumu; i.KontrolDurumu = KontrolOnceligi.FirstOrDefault(x => d.Contains(x)) ?? "Uyumlu"; i.Kontroller = K; i.KontrolTarihi = z;
                if (i.Durum == "Tamamlandı" && i.KontrolDurumu != "Uyumlu") { Log(i, z, kullanici, "STATUS", "Tamamlandı", "Kontrol Gerekli", "Kontrol"); i.Durum = "Kontrol Gerekli"; }
                else if (i.Durum == "Kontrol Gerekli" && i.KontrolDurumu == "Uyumlu") { Log(i, z, kullanici, "STATUS", "Kontrol Gerekli", "Tamamlandı", "Kontrol"); i.Durum = "Tamamlandı"; }
                if (eski != i.KontrolDurumu) Log(i, z, kullanici, "CONTROL_STATUS", eski, i.KontrolDurumu, "Kontrol");
                return null;
            }
        }
        public static string FarkKapat(TmlIslem i, string aciklama, string rol, string kullanici, DateTime? simdi = null) { lock (Kilit) { var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "farkKapat")) return "Kontrol farkını yalnız Muhasebe / Yönetici kapatır."; if (string.IsNullOrWhiteSpace(aciklama)) return "Fark kapatma açıklaması zorunludur."; if (i.KontrolDurumu == null || i.KontrolDurumu == "Uyumlu") return "Kapatılacak fark yok."; if (i.Kontroller.Any(k => k.Sonuc != "Uyumlu" && k.Kritik && !k.Sonuc.StartsWith("Kontrol"))) return "Kritik fark (tutar / hesap / zincir / mükerrer) açıklamayla kapatılamaz; düzeltme fişi ya da ters kayıt gerekir."; i.FarkKapatma = aciklama; Log(i, z, kullanici, "CONTROL_CLOSE", i.KontrolDurumu, "Uyumlu (fark kapatıldı)", "Kontrol", aciklama); i.KontrolDurumu = "Uyumlu"; if (i.Durum == "Kontrol Gerekli") i.Durum = "Tamamlandı"; return null; } }

        // =====================================================================
        // ÖRNEK KAYITLAR (§23 senaryoları + her durumdan bir işlem)
        // =====================================================================
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var bugun = DateTime.Today; string ku = "Nazlı Erdem", muh = "Nazlı Erdem";
                var KA = KaynakAlacaklar; TmlKaynak Kn(string no) => KA.First(k => k.BelgeNo == no);
                TmlIslem Yap(int gunOnce, string tur, string neden, string aciklama, (string belge, int sira, decimal oran)[] kaynaklar, string[] taraflar, decimal?[] stepler = null, string proje = null)
                {
                    var z = bugun.AddDays(-gunOnce).AddHours(10).AddMinutes(15 + gunOnce % 40);
                    var g = new TmlIslemGiris
                    {
                        Rol = "Yönetici", Tarih = z.ToString("yyyy-MM-dd"), EtkinlikTarihi = z.AddDays(1).ToString("yyyy-MM-dd"), Tur = tur, KaynakBelgeTuru = Kn(kaynaklar[0].belge).BelgeTuru, NedenKodu = neden.Split(' ')[0], Neden = neden.Substring(neden.IndexOf(' ') + 1), Aciklama = aciklama, Proje = proje ?? "", MasrafMerkezi = "CC20 - Finans",
                        Kaynaklar = kaynaklar.Select(k => { var ky = Kn(k.belge); var kl = ky.Kalemler.First(x => x.Sira == k.sira); return new TmlKaynakGiris { KaynakId = ky.Id, Sira = k.sira, BuIslem = Math.Round((kl.Nominal - kl.Kapanan - OncedenTemlik(ky.Id, k.sira)) * k.oran, 2) }; }).ToList(),
                        Taraflar = taraflar.Select(t => new TmlTarafGiris { Ad = t }).ToList(), Stepler = stepler?.Select(s => new TmlStepGiris { Tutar = s }).ToList()
                    };
                    var (i, s) = Kaydet(g, ku, z); if (i == null) throw new InvalidOperationException("Temlik tohumu: " + string.Join(" | ", s.Hatalar)); return i;
                }
                DateTime Z(TmlIslem i, int saat) => i.OlusturmaZamani.AddHours(saat);
                void Gonder(TmlIslem i) { var s = OnayaGonder(i, "Yönetici", ku, Z(i, 2)); if (!s.Gecerli) throw new InvalidOperationException(i.No + ": " + string.Join(" | ", s.Hatalar)); }
                void OnaylaHepsi(TmlIslem i, int kac = 99) { int n = 0; while (i.Durum == "Onay Bekliyor" && n < kac) { var a = i.Onaylar.First(o => o.Durum == "Bekliyor"); Onayla(i, "Onayla", n == 0 ? "Kaynak alacak ve zincir kontrol edildi." : a.Adim.StartsWith("Hukuk") ? "Sözleşme şablonu v3.2 uygun; borçluya bildirim planlandı." : "Uygundur.", "Yönetici", a.Kisi, Z(i, 20 + n * 5)); n++; } }
                void Belge(TmlIslem i, int saat = 44) { var (h, _) = BelgeOlustur(i, "Hukuk", "Av. Deniz Korkmaz", true, Z(i, saat)); if (h != null) throw new InvalidOperationException(i.No + ": " + h); }
                void Imza(TmlIslem i, int kac, int saat = 50) { var h = ImzayaGonder(i, "Hukuk", "Av. Deniz Korkmaz", Z(i, saat - 2)); if (h != null) throw new InvalidOperationException(i.No + ": " + h); var b = i.AktifBelge; for (int n = 0; n < Math.Min(kac, b.Imzalar.Count); n++) { h = Imzala(i, b.Imzalar[n].Sira, "Onayla", n % 2 == 0 ? "E-İmza" : "Islak İmza", null, ku, Z(i, saat + n * 7)); if (h != null) throw new InvalidOperationException(i.No + ": " + h); } }
                void Erp(TmlIslem i, string senaryo, int saat = 80) { var h = Muhasebelestir(i, senaryo, "Muhasebe", muh, null, Z(i, saat)); if (h != null) throw new InvalidOperationException(i.No + ": " + h); }

                // TML-00045 · §23.1 Senaryo 1: A Tedarikçisi → ALV Kimya → URAS Kimya, X Müşterisi 1.000.000 TL (FTR-00125 600.000 + FTR-00148 400.000) → Tamamlandı · Uyumlu
                var i1 = Yap(30, "Zincir Temlik", "TN02 Tedarikçi Borcuna Mahsuben", "A Tedarikçisi'nin X Müşterisi'nden alacağı ALV Kimya'ya, ALV Kimya'dan URAS Kimya'ya zincir temlik (3 taraflı virman).", new[] { ("FTR-2026-00125", 1, 1m), ("FTR-2026-00125", 2, 1m), ("FTR-2026-00148", 1, 1m) }, new[] { "A Tedarikçisi", "Alv Kimya", "Uras Kimya" });
                Gonder(i1); OnaylaHepsi(i1); Belge(i1); Imza(i1, 3); Erp(i1, "uyumlu");
                // TML-00046 · §23.2 Senaryo 2: B Tedarikçisi → ALV → URAS 1.000.000 / 700.000, ALV kalan 300.000; step 2 ERP eksik → Kısmi Muhasebeleşti · Kısmi Entegrasyon
                var i2 = Yap(24, "Zincir Temlik", "TN02 Tedarikçi Borcuna Mahsuben", "B Tedarikçisi alacağı ALV'ye tam, ALV'den URAS'a 700.000 TL kısmi devir; ALV üzerinde 300.000 TL kalan temlik hakkı.", new[] { ("CAR-2026-00311", 1, 1m) }, new[] { "B Tedarikçisi", "Alv Kimya", "Uras Kimya" }, new decimal?[] { null, 700000m });
                Gonder(i2); OnaylaHepsi(i2); Belge(i2); Imza(i2, 3); Erp(i2, "step2hata");
                // TML-00047 · §23.3 Senaryo 3: 4 taraflı zincir A → ALV → URAS → Avrupa Paper (3 step) → Tamamlandı · Uyumlu (Direktör onayı: taraf sayısı)
                var i3 = Yap(20, "Zincir Temlik", "TN01 Grup İçi Finansman", "4 taraflı zincir: A Tedarikçisi → ALV Kimya → URAS Kimya → Avrupa Paper; her step ayrı ERP birimi.", new[] { ("CAR-2026-00420", 1, 1m) }, new[] { "Petkim Petrokimya Holding A.Ş.", "Alv Kimya", "Uras Kimya", "Avrupa Paper" });
                Gonder(i3); OnaylaHepsi(i3); Belge(i3); Imza(i3, 4); Erp(i3, "uyumlu");
                // TML-00048 · 2 taraflı standart temlik: Uras Kimya → Uras Holding, Marmara Plastik faturalarından satır bazlı kısmi (fatura 1 satır 1-2 tamamı, fatura 2 satır 1 %50) → Tam İmzalı, ERP bekliyor
                var i4 = Yap(9, "Grup İçi Temlik", "TN01 Grup İçi Finansman", "Marmara Plastik alacağının seçili satırları Uras Holding'e temlik (kısmi); tam imzalı, muhasebeleştirme bekliyor.", new[] { ("URS2026100071204", 1, 1m), ("URS2026100071204", 2, 1m), ("URS2026100071391", 1, 0.5m) }, new[] { "Uras Kimya", "Uras Holding" });
                Gonder(i4); OnaylaHepsi(i4); Belge(i4); Imza(i4, 2);
                // TML-00049 · Selvi → Alv Kimya → Uras Kimya, kısmi imzalı (1/3)
                var i5 = Yap(6, "Zincir Temlik", "TN04 Alacak Yönetimi / Faktoring", "Selvi'nin Marmara Plastik alacağı grup içinde zincir temlik; imza süreci devam ediyor.", new[] { ("SLV2026100004512", 1, 1m) }, new[] { "Selvi", "Alv Kimya", "Uras Kimya" });
                Gonder(i5); OnaylaHepsi(i5); Belge(i5); Imza(i5, 1);
                // TML-00050 · Onay Bekliyor (dövizli, EUR): Avrupa Paper → Uras Holding
                var i6 = Yap(3, "Alacak Temliki", "TN03 Kredi Teminatı", "Karadeniz Ambalaj EUR alacağının Uras Holding'e temliki (kredi teminatı).", new[] { ("AVP2026100002230", 1, 1m), ("AVP2026100002230", 2, 0.5m) }, new[] { "Avrupa Paper", "Uras Holding" });
                Gonder(i6); Onayla(i6, "Onayla", "Kaynak fatura ve kur kontrol edildi.", "Muhasebe", muh, Z(i6, 5));
                // TML-00051 · Taslak: Uras Kimya → Ziraat Bankası (teminat), sözleşme alacağı
                var i7 = Yap(1, "Teminat Amaçlı Temlik", "TN03 Kredi Teminatı", "Sözleşme hak edişlerinin kredi teminatı olarak bankaya temliki (taslak).", new[] { ("SZL-2026-207", 1, 0.6m) }, new[] { "Uras Kimya", "Ziraat Bankası Gebze Şb." });
                // TML-00052 · Reddedildi: Uras Kimya → Daf Kimya (Karadeniz Ambalaj vadesi geçmiş alacak)
                var i8 = Yap(12, "Alacak Temliki", "TN05 Diğer", "Vadesi geçmiş Karadeniz Ambalaj alacağının Daf Kimya'ya temliki.", new[] { ("URS2026100070988", 1, 1m) }, new[] { "Uras Kimya", "Daf Kimya" });
                Gonder(i8); Onayla(i8, "Onayla", "", "Muhasebe", muh, Z(i8, 4)); Onayla(i8, "Reddet", "Vadesi geçmiş alacak; önce tahsilat / hukuki takip değerlendirilmeli.", "Hukuk", "Av. Deniz Korkmaz", Z(i8, 26));
                // TML-00053 · Tamamlandı ama Hesap Hatası: Uras Kimya → Selvi (Karadeniz Ambalaj satır 2)
                var i9 = Yap(16, "Grup İçi Temlik", "TN01 Grup İçi Finansman", "Karadeniz Ambalaj koli faturası satırının Selvi'ye temliki.", new[] { ("URS2026100070988", 2, 1m) }, new[] { "Uras Kimya", "Selvi" });
                Gonder(i9); OnaylaHepsi(i9); Belge(i9); Imza(i9, 2); Erp(i9, "hesap");
                // TML-00054 · 5 taraflı zincir Tamamlandı · Uyumlu: Petkim → Uras Kimya → Alv Kimya → Daf Kimya → Uras Power (kalan CAR-00420 yok → SZL kullan)
                var i10 = Yap(14, "Zincir Temlik", "TN01 Grup İçi Finansman", "5 taraflı zincir temlik (4 transfer step); Uras Power proje finansmanı.", new[] { ("SZL-2026-207", 1, 0.3m) }, new[] { "Uras Kimya", "Alv Kimya", "Daf Kimya", "Selvi", "Uras Power" }, null, "PRJ-2026-07 Enerji Verimliliği");
                Gonder(i10); OnaylaHepsi(i10); Belge(i10); Imza(i10, 5); Erp(i10, "uyumlu");
                // TML-00055 · Ters Kayıt: Uras Kimya → Alv Kimya (Marmara fatura 2 satır 1 kalan %50) muhasebeleşti sonra geri alındı
                var i11 = Yap(18, "Grup İçi Temlik", "TN01 Grup İçi Finansman", "Marmara Plastik alacağının Alv Kimya'ya temliki; müşteri erken ödeme yaptığı için ters kayıtla geri alındı.", new[] { ("URS2026100071391", 1, 0.4m) }, new[] { "Uras Kimya", "Alv Kimya" });
                Gonder(i11); OnaylaHepsi(i11); Belge(i11); Imza(i11, 2); Erp(i11, "uyumlu"); var h11 = TersKayit(i11, "Asıl borçlu alacağı temlik edene erken ödedi; temlik konusuz kaldı.", "Muhasebe", muh, Z(i11, 120)); if (h11 != null) throw new InvalidOperationException(h11);
                // TML-00056 · Belge Üretildi, imzaya gönderilmemiş: Uras Kimya → Uras Holding (Karadeniz satır 1 kalan? kullanılmış) → Selvi fatura satır 2
                var i12 = Yap(4, "Alacak Temliki", "TN04 Alacak Yönetimi / Faktoring", "Selvi'nin streç film satırı alacağının Uras Holding'e temliki; belge üretildi, imza bekliyor.", new[] { ("SLV2026100004512", 2, 1m) }, new[] { "Selvi", "Uras Holding" });
                Gonder(i12); OnaylaHepsi(i12); Belge(i12);
                _islemler.Sort((a, b) => b.Id.CompareTo(a.Id));
            }
        }
    }
}
