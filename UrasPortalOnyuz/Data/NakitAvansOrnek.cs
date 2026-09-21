// ============================================================
// NAKİT AVANS TALEP, ÖDEME, MAHSUP VE KAPATMA MODÜLÜ — örnek veri ve iş kuralları
// Kaynak: "Uras Holding | Nakit Avans Talep, Ödeme, Mahsup ve Kapatma Modülü — IT Analiz ve Geliştirme
// Gereksinimleri v1.0 (20.09.2026)". Bu ön yüz projesinde Finans / Kasa / Muhasebe sistemi yoktur;
// finansal gerçekleşmeler (ödeme, harcama belgesi, iade, muhasebe fişi) ekrana elle girilmez,
// "kaynak sistem olayı" (CashPaymentCreated, ExpensePosted, CashReturned, JournalPosted …) olarak
// Talep No üzerinden işlenir ve tutarlar / statüler otomatik yeniden hesaplanır (§1, §14, §21, §30, §33).
// Kayıtlar bellek içindedir; uygulama yeniden başlayınca örnek tohuma döner.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class NaKisi
    {
        public string PersonelNo { get; set; }
        public string AdSoyad { get; set; }
        public string Sirket { get; set; }
        public string Departman { get; set; }
        public string Unvan { get; set; }
        public string MasrafMerkezi { get; set; }
        public bool Aktif { get; set; } = true;
        public string KullaniciId { get; set; }
        public string Rol { get; set; }            // Talep Sahibi | Yönetici | Üst Yönetim | Finans | Muhasebe
    }

    public class NaOnay
    {
        public int Sira { get; set; }
        public string Adim { get; set; }           // Talep Oluşturma | Yönetici Onayı | Üst Yönetim Onayı | Finans Uygunluk Onayı
        public string Kisi { get; set; }
        public string Durum { get; set; }          // Tamamlandı | Onaylandı | Onay Bekliyor | Pasif | Reddedildi | Revizyon
        public DateTime? Tarih { get; set; }
        public string Not { get; set; }
    }

    public class NaOdeme
    {
        public string IslemId { get; set; }        // FINANCE_TRANSACTION_ID
        public DateTime Tarih { get; set; }
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; }
        public string Kasa { get; set; }
        public string FisNo { get; set; }          // Kasa çıkış fiş no
        public string TeslimEden { get; set; }
        public string TeslimAlan { get; set; }
        public string TeslimSekli { get; set; }    // Nakit | Havale
        public string Durum { get; set; }          // Teslim Edildi | İptal
        public bool TeslimAlanOnayi { get; set; }  // §12 dijital teslim teyidi
        public DateTime? TeslimOnayTarihi { get; set; }
    }

    public class NaHarcama
    {
        public string BelgeId { get; set; }        // DOCUMENT_ID
        public DateTime Tarih { get; set; }
        public string BelgeNo { get; set; }
        public string Tedarikci { get; set; }
        public string BelgeTuru { get; set; }      // Fatura | E-Fatura | E-Arşiv Fatura | Perakende Fiş | Makbuz | Resmî Kurum Belgesi
        public decimal Tutar { get; set; }
        public string MahsupDurumu { get; set; }   // Mahsup Edildi | Kontrol Bekliyor | Reddedildi
        public string MuhasebeFisNo { get; set; }
        public string YevmiyeNo { get; set; }
        public string HesapKodu { get; set; }
    }

    public class NaIade
    {
        public string IslemId { get; set; }        // RETURN_TRANSACTION_ID
        public DateTime Tarih { get; set; }
        public decimal Tutar { get; set; }
        public string Kasa { get; set; }
        public string FisNo { get; set; }          // Kasa giriş fiş no
        public string IadeyiAlan { get; set; }
    }

    public class NaYansitma
    {
        public string IslemTipi { get; set; } = "Grup İçi Avans";
        public string OdeyenSirket { get; set; }
        public string MasrafSirketi { get; set; }
        public decimal Tutar { get; set; }
        public string BelgeNo { get; set; }
        public string Durum { get; set; }          // Bekliyor | Kısmi | Yansıtıldı | Hata
    }

    public class NaHareket
    {
        public DateTime Tarih { get; set; }
        public string Hareket { get; set; }
        public string Kaynak { get; set; }
    }

    public class NaAudit
    {
        public DateTime Tarih { get; set; }
        public string Tablo { get; set; }
        public string Alan { get; set; }
        public string Eski { get; set; }
        public string Yeni { get; set; }
        public string Kullanici { get; set; }
        public string IslemTipi { get; set; }      // Insert | Update | Event
        public string KaynakSistem { get; set; }   // Portal | Workflow | Finans/Kasa | Muhasebe | Sistem
    }

    public class NaEk
    {
        public string Ad { get; set; }
        public string Tur { get; set; }            // Teklif | Proforma | Liste | Görsel | Belge
        public string Boyut { get; set; }
        public DateTime Tarih { get; set; }
        public string Yukleyen { get; set; }
    }

    public class NakitAvans
    {
        public int Id { get; set; }                // REQUEST_ID
        public string No { get; set; }             // REQUEST_NO  (NA-2026-000146)
        public DateTime Tarih { get; set; }
        public string Sirket { get; set; }
        public string Departman { get; set; }
        public string MasrafMerkezi { get; set; }
        public NaKisi Olusturan { get; set; }
        public NaKisi Kullanan { get; set; }
        public string TalepTuru { get; set; } = "Nakit Avans";
        public string Konu { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; } = "TL";
        public DateTime KullanimTarihi { get; set; }
        public DateTime KapatmaTarihi { get; set; }        // beklenen
        public string FaturaDurumu { get; set; }           // Faturalı | Kısmen Faturalı | Faturasız
        public List<string> BelgeTurleri { get; set; } = new List<string>();
        public string HarcamaKategorisi { get; set; }      // "02 - Teknik Malzeme"
        public string Oncelik { get; set; }                // Normal | Acil | Çok Acil
        public List<NaEk> Ekler { get; set; } = new List<NaEk>();
        public List<NaOnay> Onaylar { get; set; } = new List<NaOnay>();
        public decimal OnaylananTutar { get; set; }
        public List<NaOdeme> Odemeler { get; set; } = new List<NaOdeme>();
        public List<NaHarcama> Harcamalar { get; set; } = new List<NaHarcama>();
        public List<NaIade> Iadeler { get; set; } = new List<NaIade>();
        public NaYansitma Yansitma { get; set; }
        public List<NaHareket> Zaman { get; set; } = new List<NaHareket>();
        public List<NaAudit> Audit { get; set; } = new List<NaAudit>();
        public int Revizyon { get; set; }
        public string EntegrasyonDurumu { get; set; } = "Başarılı";   // §24
        public DateTime? SonSenkron { get; set; }

        // ---- Hesaplanan alanlar (§13, §26): ekrana elle girilmez ----
        public string TalepDurumu
        {
            get
            {
                if (Onaylar.Any(o => o.Durum == "Reddedildi")) return "Reddedildi";
                if (Onaylar.Any(o => o.Durum == "Revizyon")) return "Revizyon Bekliyor";
                var bekleyen = Onaylar.FirstOrDefault(o => o.Durum == "Onay Bekliyor");
                return bekleyen != null ? bekleyen.Adim + " Bekliyor" : "Onaylandı";
            }
        }
        public bool Onaylandi => TalepDurumu == "Onaylandı";
        public decimal TeslimEdilen => Odemeler.Where(o => o.Durum != "İptal").Sum(o => o.Tutar);
        public decimal MahsupEdilen => Harcamalar.Where(h => h.MahsupDurumu == "Mahsup Edildi").Sum(h => h.Tutar);
        public decimal KontrolBekleyen => Harcamalar.Where(h => h.MahsupDurumu == "Kontrol Bekliyor").Sum(h => h.Tutar);
        public decimal IadeEdilen => Iadeler.Sum(i => i.Tutar);
        public decimal AcikTutar => TeslimEdilen - MahsupEdilen - IadeEdilen;
        public decimal OdemeBekleyen => Onaylandi ? Math.Max(0, OnaylananTutar - TeslimEdilen) : 0;
        public string OdemeDurumu
        {
            get
            {
                if (TalepDurumu == "Reddedildi") return "Ödeme İptal Edildi";
                if (TeslimEdilen <= 0) return "Ödeme Bekliyor";
                return TeslimEdilen < OnaylananTutar ? "Kısmi Ödendi" : "Ödeme Teslim Edildi";
            }
        }
        public string KapatmaDurumu
        {
            get
            {
                if (TeslimEdilen <= 0) return "Henüz Ödeme Yok";
                if (AcikTutar <= 0) return "Kapandı";
                if (DateTime.Today > KapatmaTarihi) return "Süresi Geçti";
                if (KontrolBekleyen > 0) return "Muhasebe Mahsubu Bekliyor";
                if (MahsupEdilen > 0 || IadeEdilen > 0) return "Kısmi Kapandı";
                if (DateTime.Today > KullanimTarihi) return "Belge Bekleniyor";
                return "Açık";
            }
        }
        public bool Kapandi => KapatmaDurumu == "Kapandı";
        public bool Gecikmis => DateTime.Today > KapatmaTarihi && AcikTutar > 0 && TeslimEdilen > 0;
        public int GecikenGun => Gecikmis ? (DateTime.Today - KapatmaTarihi).Days : 0;
        public DateTime? IlkTeslim => Odemeler.Where(o => o.Durum != "İptal").OrderBy(o => o.Tarih).Select(o => (DateTime?)o.Tarih).FirstOrDefault();
        public int BekleyenGun => IlkTeslim.HasValue && !Kapandi ? (DateTime.Today - IlkTeslim.Value.Date).Days : 0;
        public DateTime? SonIslem => new[] { Odemeler.Select(o => (DateTime?)o.Tarih).DefaultIfEmpty().Max(), Harcamalar.Select(h => (DateTime?)h.Tarih).DefaultIfEmpty().Max(), Iadeler.Select(i => (DateTime?)i.Tarih).DefaultIfEmpty().Max() }.Max();
        public DateTime? FiiliKapatma => Kapandi ? Zaman.Where(z => z.Hareket.StartsWith("Avans kapatıldı")).Select(z => (DateTime?)z.Tarih).LastOrDefault() ?? SonIslem : null;
        /// <summary>§17: ödemeyi yapan şirket ile avansı kullanan kişinin (masrafın) şirketi farklıysa grup içi yansıtma gerekir.</summary>
        public bool GrupIci => Kullanan != null && !string.IsNullOrEmpty(Sirket) && Sirket != Kullanan.Sirket;
        public string Yonetici => Onaylar.FirstOrDefault(o => o.Adim == "Yönetici Onayı")?.Kisi;
        public string UstOnay => Onaylar.FirstOrDefault(o => o.Adim == "Üst Yönetim Onayı")?.Kisi;
        public string FinansOnayi => Onaylar.FirstOrDefault(o => o.Adim == "Finans Uygunluk Onayı")?.Kisi;
        public DateTime? OnayTarihi => Onaylandi ? Onaylar.Where(o => o.Tarih.HasValue).Select(o => o.Tarih).Max() : null;
        public string MuhasebeFisNo => string.Join(", ", Harcamalar.Where(h => !string.IsNullOrEmpty(h.MuhasebeFisNo)).Select(h => h.MuhasebeFisNo).Distinct());
    }

    public static class NakitAvansOrnek
    {
        public static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        public static readonly object Kilit = new object();

        // ---------- Master veriler ----------
        public static readonly string[] ParaBirimleri = { "TL", "USD", "EUR", "AED" };
        public static readonly string[] Oncelikler = { "Normal", "Acil", "Çok Acil" };
        public static readonly string[] FaturaDurumlari = { "Faturalı", "Kısmen Faturalı", "Faturasız" };
        public static readonly Dictionary<string, string[]> BelgeTurleri = new Dictionary<string, string[]>
        {
            ["Faturalı"] = new[] { "Fatura", "E-Fatura", "E-Arşiv Fatura" },
            ["Kısmen Faturalı"] = new[] { "Fatura", "E-Fatura", "E-Arşiv Fatura", "Perakende Fiş", "Makbuz" },
            ["Faturasız"] = new[] { "Makbuz", "Resmî Kurum Belgesi", "Belgesiz" }
        };
        public static readonly string[] HarcamaKategorileri =
        {
            "01 - İdari Alım", "02 - Teknik Malzeme", "03 - Bakım / Onarım", "04 - Saha Masrafı", "05 - Nakliye / Lojistik", "06 - Kargo", "07 - Resmî Kurum Ödemesi",
            "08 - Seyahat", "09 - Konaklama", "10 - Temsil / Ağırlama", "11 - Acil Satın Alma", "12 - Operasyonel Gider", "13 - Diğer"
        };
        /// <summary>§8.2 Limit master (TL). Üst yönetim "Parametre": tutar UstOnayEsigi'ni aşarsa devreye girer.</summary>
        public static readonly (decimal Min, decimal Max, bool Yonetici, string UstYonetim, bool Finans)[] LimitMaster =
        {
            (0m, 10000m, true, "", true),
            (10001m, 49999m, true, "Parametre", true),
            (50000m, 999999999m, true, "X", true)
        };
        public static decimal UstOnayEsigi = 25000m;
        /// <summary>§18.1 Yeni talep politikası: Sadece uyar | Yönetici onayına gönder | Ek üst onay iste | Yeni talebi engelle</summary>
        public static string AcikAvansPolitikasi = "Sadece uyar";
        public static readonly string[] Kasalar = { "KS01 - Merkez TL Kasası", "KS02 - Fabrika Kasası", "KS03 - Döviz Kasası" };

        // ---------- Personel (İK master taklidi) ----------
        public static readonly List<NaKisi> Personel = new List<NaKisi>
        {
            K("P1001", "Ali Veli", "Uras Kimya", "Bilgi Teknolojileri", "IT Uzmanı", "CC10 - Bilgi Teknolojileri", "it02", "Talep Sahibi"),
            K("P1002", "Kerem Aksoy", "Uras Kimya", "Bilgi Teknolojileri", "IT Müdürü", "CC10 - Bilgi Teknolojileri", "fns2", "Yönetici"),
            K("P1003", "Nazlı Erdem", "Uras Kimya", "Muhasebe", "Muhasebe Uzmanı", "CC21 - Muhasebe", "muh36", "Muhasebe"),
            K("P1004", "Tolga Yaman", "Selvi", "Satış", "Satış Temsilcisi", "CC30 - Satış", "sat5", "Talep Sahibi"),
            K("P1005", "Gizem Tan", "Avrupa Paper", "Satış", "Satış Uzmanı", "CC30 - Satış", "sat8", "Talep Sahibi"),
            K("P1006", "Onur Bal", "Uras Kimya", "Muhasebe", "Muhasebe Şefi", "CC21 - Muhasebe", "muh58", "Muhasebe"),
            K("P1007", "Sevgi Ay", "Uras Holding", "Yönetim", "Genel Müdür Yardımcısı", "CC01 - Yönetim", "ykb", "Üst Yönetim"),
            K("P1008", "Hülya Er", "Daf Kimya", "Muhasebe", "Muhasebe Uzmanı", "CC21 - Muhasebe", "muh19", "Muhasebe"),
            K("P1009", "Canan Su", "Alv Kimya", "İdari İşler", "İdari İşler Sorumlusu", "CC05 - İdari İşler", "muh45", "Talep Sahibi"),
            K("P1010", "Esra Kaya", "Uras Kimya", "Teknik Servis", "Teknik Servis Sorumlusu", "CC23 - Teknik Servis", "dns", "Talep Sahibi"),
            K("P1011", "Mert Doğan", "Uras Kimya", "Üretim", "Üretim Şefi", "CC40 - Üretim", "isg1", "Talep Sahibi"),
            K("P1012", "Pınar Ak", "Uras Holding", "Finans", "Finans Uzmanı", "CC20 - Finans", "fns1", "Finans"),
            K("P1013", "Ertan Yavuz", "URS Makina", "Teknik Servis", "Servis Teknisyeni", "CC23 - Teknik Servis", "ery", "Talep Sahibi"),
            K("P1014", "Yasin Sezgin", "URS Makina", "Teknik Servis", "Teknik Servis Müdürü", "CC23 - Teknik Servis", "ysz", "Yönetici"),
            K("P1015", "Neslihan Uludağ", "Uras Holding", "Yönetim", "CFO", "CC01 - Yönetim", "nul", "Üst Yönetim"),
            K("P1016", "Berk Berberoğlu", "Uras Holding", "Finans", "Finans Müdürü", "CC20 - Finans", "bbb", "Finans"),
            K("P1017", "Selin Kara", "Uras Kimya", "Satın Alma", "Satın Alma Uzmanı", "CC31 - Satın Alma", "skr", "Talep Sahibi", false),
            K("P1018", "Derya Şen", "Uras Holding", "İnsan Kaynakları", "İK Uzmanı", "CC02 - İnsan Kaynakları", "dsn", "İK"),
            K("P1019", "Selçuk Arı", "Uras Holding", "Hukuk", "Hukuk Müşaviri", "CC03 - Hukuk", "sar", "Hukuk")
        };
        private static NaKisi K(string no, string ad, string sirket, string dep, string unvan, string mm, string kul, string rol, bool aktif = true)
            => new NaKisi { PersonelNo = no, AdSoyad = ad, Sirket = sirket, Departman = dep, Unvan = unvan, MasrafMerkezi = mm, KullaniciId = kul, Rol = rol, Aktif = aktif };

        public static NaKisi KisiBul(string adVeyaNo) => Personel.FirstOrDefault(p => string.Equals(p.PersonelNo, adVeyaNo, StringComparison.OrdinalIgnoreCase) || string.Equals(p.AdSoyad, adVeyaNo, StringComparison.OrdinalIgnoreCase) || string.Equals(p.KullaniciId, adVeyaNo, StringComparison.OrdinalIgnoreCase));
        public static NaKisi Yonetici(NaKisi k) => Personel.FirstOrDefault(p => p.Rol == "Yönetici" && p.Departman == k.Departman && p.Sirket == k.Sirket) ?? Personel.FirstOrDefault(p => p.Rol == "Yönetici" && p.Departman == k.Departman) ?? Personel.First(p => p.Rol == "Yönetici");
        public static NaKisi UstYonetim(NaKisi k) => Personel.FirstOrDefault(p => p.Rol == "Üst Yönetim" && p.Unvan == "CFO") ?? Personel.First(p => p.Rol == "Üst Yönetim");
        public static NaKisi Finans() => Personel.First(p => p.Rol == "Finans" && p.Unvan.Contains("Müdür"));

        // ---------- Onay matrisi (§8) ----------
        public static bool UstOnayGerekli(decimal tutarTl)
        {
            var satir = LimitMaster.FirstOrDefault(l => tutarTl >= l.Min && tutarTl <= l.Max);
            if (satir.UstYonetim == "X") return true;
            if (satir.UstYonetim == "Parametre") return tutarTl >= UstOnayEsigi;
            return false;
        }
        public static decimal TlKarsiligi(decimal tutar, string pb) => pb == "USD" ? tutar * 41.2m : pb == "EUR" ? tutar * 47.9m : pb == "AED" ? tutar * 11.2m : tutar;

        public static List<NaOnay> OnayAkisiOlustur(NakitAvans a, DateTime t)
        {
            decimal tl = TlKarsiligi(a.Tutar, a.ParaBirimi);
            var liste = new List<NaOnay> { new NaOnay { Sira = 1, Adim = "Talep Oluşturma", Kisi = a.Olusturan.AdSoyad, Durum = "Tamamlandı", Tarih = t } };
            liste.Add(new NaOnay { Sira = 2, Adim = "Yönetici Onayı", Kisi = Yonetici(a.Kullanan).AdSoyad, Durum = "Onay Bekliyor" });
            if (UstOnayGerekli(tl)) liste.Add(new NaOnay { Sira = liste.Count + 1, Adim = "Üst Yönetim Onayı", Kisi = UstYonetim(a.Kullanan).AdSoyad, Durum = "Pasif" });
            liste.Add(new NaOnay { Sira = liste.Count + 1, Adim = "Finans Uygunluk Onayı", Kisi = Finans().AdSoyad, Durum = "Pasif" });
            return liste;
        }

        // ---------- Kayıtlar ----------
        private static int _sonId = 0;
        public static readonly List<NakitAvans> Talepler = new List<NakitAvans>();
        public static readonly HashSet<string> IslenenIslemler = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // idempotency (§24)

        static NakitAvansOrnek() { Tohum(); }

        private static void Hareket(NakitAvans a, DateTime t, string h, string kaynak) => a.Zaman.Add(new NaHareket { Tarih = t, Hareket = h, Kaynak = kaynak });
        private static void Log(NakitAvans a, DateTime t, string tablo, string alan, string eski, string yeni, string kul, string islem, string kaynak)
            => a.Audit.Add(new NaAudit { Tarih = t, Tablo = tablo, Alan = alan, Eski = eski, Yeni = yeni, Kullanici = kul, IslemTipi = islem, KaynakSistem = kaynak });

        /// <summary>Yeni talep (§4–§7, §17, §18). Doğrulama sonucu hata metni döner; null = başarılı.</summary>
        public static string Olustur(NakitAvans a, out NakitAvans kayit)
        {
            kayit = null;
            if (a.Kullanan == null) return "Avansı kullanacak kişi seçilmelidir.";
            if (!a.Kullanan.Aktif) return "Aktif olmayan personele avans açılamaz (T02).";
            if (a.Tutar <= 0) return "Talep tutarı sıfırdan büyük olmalıdır.";
            if (a.KapatmaTarihi < a.KullanimTarihi) return "Beklenen kapatma tarihi, kullanım tarihinden önce olamaz.";
            var acik = AcikAvanslar(a.Kullanan);
            if (AcikAvansPolitikasi == "Yeni talebi engelle" && acik.Count > 0) return $"{a.Kullanan.AdSoyad} adına {acik.Count} açık avans var; politika gereği yeni talep engellendi.";
            var t = DateTime.Now;
            lock (Kilit) { a.Id = ++_sonId; a.No = $"NA-{t.Year}-{(_sonId + 140):000000}"; }
            a.Tarih = t;
            a.Departman = a.Kullanan.Departman; a.MasrafMerkezi = a.Kullanan.MasrafMerkezi;        // §5.2 masraf merkezi kullanacak kişiden
            a.Sirket = a.Sirket ?? a.Olusturan.Sirket;
            a.Onaylar = OnayAkisiOlustur(a, t);
            if (AcikAvansPolitikasi == "Ek üst onay iste" && acik.Count > 0 && a.Onaylar.All(o => o.Adim != "Üst Yönetim Onayı"))
                a.Onaylar.Insert(2, new NaOnay { Sira = 3, Adim = "Üst Yönetim Onayı", Kisi = UstYonetim(a.Kullanan).AdSoyad, Durum = "Pasif", Not = "Açık avans politikası" });
            for (int i = 0; i < a.Onaylar.Count; i++) a.Onaylar[i].Sira = i + 1;
            if (a.GrupIci) a.Yansitma = new NaYansitma { OdeyenSirket = a.Sirket, MasrafSirketi = a.Kullanan.Sirket, Tutar = a.Tutar, BelgeNo = "Otomatik/ERP", Durum = "Bekliyor" };
            Hareket(a, t, "Talep oluşturuldu", a.Olusturan.AdSoyad);
            Log(a, t, "CASH_ADVANCE_HEADER", "REQUEST_STATUS", "", a.TalepDurumu, a.Olusturan.KullaniciId, "Insert", "Portal");
            Log(a, t, "CASH_ADVANCE_HEADER", "REQUEST_AMOUNT", "", a.Tutar.ToString("N2", Tr) + " " + a.ParaBirimi, a.Olusturan.KullaniciId, "Insert", "Portal");
            a.SonSenkron = t;
            lock (Kilit) Talepler.Add(a);
            kayit = a;
            return null;
        }

        /// <summary>§8.3 Onay eylemleri: Onayla | Reddet (gerekçe zorunlu) | Revizyona Gönder (açıklama zorunlu).</summary>
        public static string OnayEylemi(NakitAvans a, string eylem, string not, string kullanici, decimal? yeniTutar = null)
        {
            var adim = a.Onaylar.FirstOrDefault(o => o.Durum == "Onay Bekliyor");
            if (adim == null) return "Bekleyen onay adımı yok.";
            var t = DateTime.Now;
            if (eylem == "Reddet")
            {
                if (string.IsNullOrWhiteSpace(not)) return "Red gerekçesi zorunludur.";
                adim.Durum = "Reddedildi"; adim.Tarih = t; adim.Not = not;
                foreach (var o in a.Onaylar.Where(o => o.Durum == "Pasif")) o.Durum = "İptal";
                Hareket(a, t, $"{adim.Adim} — reddedildi ({not})", adim.Kisi);
                Log(a, t, "CASH_ADVANCE_APPROVAL", "STATUS", "Onay Bekliyor", "Reddedildi", kullanici, "Update", "Workflow");
                return null;
            }
            if (eylem == "Revizyon")
            {
                if (string.IsNullOrWhiteSpace(not)) return "Revizyon açıklaması zorunludur.";
                adim.Durum = "Revizyon"; adim.Tarih = t; adim.Not = not;
                Hareket(a, t, $"{adim.Adim} — revizyona gönderildi ({not})", adim.Kisi);
                Log(a, t, "CASH_ADVANCE_APPROVAL", "STATUS", "Onay Bekliyor", "Revizyon", kullanici, "Update", "Workflow");
                return null;
            }
            if (eylem == "Onayla")
            {
                adim.Durum = "Onaylandı"; adim.Tarih = t; adim.Not = not;
                var sonraki = a.Onaylar.FirstOrDefault(o => o.Durum == "Pasif");
                if (sonraki != null) sonraki.Durum = "Onay Bekliyor";
                else { a.OnaylananTutar = a.Tutar; Log(a, t, "CASH_ADVANCE_HEADER", "APPROVED_AMOUNT", "0", a.Tutar.ToString("N2", Tr), "Sistem", "Update", "Workflow"); }
                string metin = adim.Adim == "Finans Uygunluk Onayı" ? "Finans uygunluk onayı verildi" : adim.Adim.Replace(" Onayı", "") + " onayladı";
                Hareket(a, t, metin, adim.Kisi);
                Log(a, t, "CASH_ADVANCE_APPROVAL", "STATUS", "Onay Bekliyor", "Onaylandı", kullanici, "Update", "Workflow");
                return null;
            }
            return "Tanımsız eylem.";
        }

        /// <summary>Revizyon: tutar değişince onay matrisi yeniden hesaplanır (§8 Tutar Değişikliği, T05).</summary>
        public static string Revize(NakitAvans a, decimal yeniTutar, string konu, string aciklama, string kullanici)
        {
            if (a.TalepDurumu != "Revizyon Bekliyor") return "Talep revizyon durumunda değil.";
            if (yeniTutar <= 0) return "Tutar sıfırdan büyük olmalıdır.";
            var t = DateTime.Now; decimal eski = a.Tutar;
            a.Tutar = yeniTutar; if (!string.IsNullOrWhiteSpace(konu)) a.Konu = konu; if (!string.IsNullOrWhiteSpace(aciklama)) a.Aciklama = aciklama;
            a.Revizyon++;
            a.Onaylar = OnayAkisiOlustur(a, t);
            if (a.Yansitma != null) a.Yansitma.Tutar = yeniTutar;
            Hareket(a, t, $"Talep revize edildi: {eski.ToString("N0", Tr)} → {yeniTutar.ToString("N0", Tr)} {a.ParaBirimi}; onay matrisi yeniden hesaplandı", a.Olusturan.AdSoyad);
            Log(a, t, "CASH_ADVANCE_HEADER", "REQUEST_AMOUNT", eski.ToString("N2", Tr), yeniTutar.ToString("N2", Tr), kullanici, "Update", "Portal");
            return null;
        }

        /// <summary>§30 Kaynak sistem olayları. Talep ekranına elle tutar girilmez; olay Talep No ile gelir, ekran yeniden hesaplanır. Idempotent (§24).</summary>
        public static string Olay(NakitAvans a, string olay, string islemId, decimal tutar, string ek1, string ek2, string ek3, string kullanici)
        {
            if (string.IsNullOrWhiteSpace(islemId)) return "Transaction ID zorunludur.";
            lock (Kilit) { if (IslenenIslemler.Contains(islemId)) return $"'{islemId}' daha önce işlendi; mükerrer hareket yok sayıldı (T10)."; }
            var t = DateTime.Now;
            switch (olay)
            {
                case "CashPaymentCreated":
                    if (!a.Onaylandi) return "Talep onaylanmadan ödeme yapılamaz (§27).";
                    if (tutar <= 0 || tutar > a.OdemeBekleyen) return $"Ödeme tutarı 0 ile ödeme bekleyen tutar ({a.OdemeBekleyen.ToString("N2", Tr)}) arasında olmalıdır.";
                    a.Odemeler.Add(new NaOdeme { IslemId = islemId, Tarih = t, Tutar = tutar, ParaBirimi = a.ParaBirimi, Kasa = string.IsNullOrEmpty(ek1) ? Kasalar[0] : ek1, FisNo = "KÇ-" + (2600 + a.Odemeler.Count + a.Id * 3), TeslimEden = Finans().AdSoyad, TeslimAlan = a.Kullanan.AdSoyad, TeslimSekli = string.IsNullOrEmpty(ek2) ? "Nakit" : ek2, Durum = "Teslim Edildi" });
                    Hareket(a, t, $"{tutar.ToString("N0", Tr)} {a.ParaBirimi} teslim edildi", "Finans/Kasa");
                    Log(a, t, "CASH_ADVANCE_PAYMENT", "PAID_AMOUNT", (a.TeslimEdilen - tutar).ToString("N2", Tr), a.TeslimEdilen.ToString("N2", Tr), "Sistem", "Event", "Finans/Kasa");
                    break;
                case "CashPaymentReversed":
                    var od = a.Odemeler.FirstOrDefault(o => o.IslemId == ek1 && o.Durum != "İptal");
                    if (od == null) return "Ters kaydı yapılacak ödeme bulunamadı.";
                    od.Durum = "İptal";
                    Hareket(a, t, $"{od.Tutar.ToString("N0", Tr)} {a.ParaBirimi} ödeme ters kaydı işlendi", "Finans/Kasa");
                    Log(a, t, "CASH_ADVANCE_PAYMENT", "STATUS", "Teslim Edildi", "İptal", "Sistem", "Event", "Finans/Kasa");
                    break;
                case "ExpensePosted":
                    if (a.TeslimEdilen <= 0) return "Teslim edilmemiş avansa harcama belgesi işlenemez.";
                    if (tutar <= 0) return "Belge tutarı sıfırdan büyük olmalıdır.";
                    a.Harcamalar.Add(new NaHarcama { BelgeId = islemId, Tarih = t, BelgeNo = string.IsNullOrEmpty(ek1) ? "BLG-" + islemId : ek1, Tedarikci = string.IsNullOrEmpty(ek2) ? "Tedarikçi" : ek2, BelgeTuru = string.IsNullOrEmpty(ek3) ? "Fatura" : ek3, Tutar = tutar, MahsupDurumu = "Kontrol Bekliyor" });
                    Hareket(a, t, $"{tutar.ToString("N0", Tr)} {a.ParaBirimi} harcama belgesi kaydedildi ({ek3 ?? "Fatura"}) — muhasebe kontrolü bekliyor", "Muhasebe");
                    Log(a, t, "CASH_ADVANCE_EXPENSE", "DOCUMENT_ID", "", islemId, "Sistem", "Event", "Muhasebe");
                    break;
                case "JournalPosted":
                    var bekleyenler = a.Harcamalar.Where(h => h.MahsupDurumu == "Kontrol Bekliyor").ToList();
                    if (bekleyenler.Count == 0) return "Muhasebe kontrolü bekleyen harcama belgesi yok.";
                    string fis = "MF-" + (7400 + a.Id * 7 + a.Harcamalar.Count), yev = "Y-" + (18800 + a.Id * 11 + a.Harcamalar.Count);
                    foreach (var h in bekleyenler) { h.MahsupDurumu = "Mahsup Edildi"; h.MuhasebeFisNo = fis; h.YevmiyeNo = yev; h.HesapKodu = "195.01.001"; }
                    Hareket(a, t, $"{bekleyenler.Sum(h => h.Tutar).ToString("N0", Tr)} {a.ParaBirimi} mahsup edildi (Fiş {fis}, Yevmiye {yev})", "Muhasebe");
                    Log(a, t, "CASH_ADVANCE_ACCOUNTING", "JOURNAL_ENTRY_ID", "", islemId, "Sistem", "Event", "Muhasebe");
                    Log(a, t, "CASH_ADVANCE_HEADER", "SETTLED_AMOUNT", (a.MahsupEdilen - bekleyenler.Sum(h => h.Tutar)).ToString("N2", Tr), a.MahsupEdilen.ToString("N2", Tr), "Sistem", "Event", "Muhasebe");
                    break;
                case "ExpenseReversed":
                    var hr = a.Harcamalar.FirstOrDefault(h => h.BelgeId == ek1);
                    if (hr == null) return "İptal edilecek harcama belgesi bulunamadı.";
                    a.Harcamalar.Remove(hr);
                    Hareket(a, t, $"{hr.Tutar.ToString("N0", Tr)} {a.ParaBirimi} harcama belgesi iptal edildi ({hr.BelgeNo}); mahsup yeniden hesaplandı", "Muhasebe");
                    Log(a, t, "CASH_ADVANCE_EXPENSE", "STATUS", hr.MahsupDurumu, "İptal", "Sistem", "Event", "Muhasebe");
                    break;
                case "CashReturned":
                    if (tutar <= 0 || tutar > a.AcikTutar) return $"İade tutarı 0 ile açık tutar ({a.AcikTutar.ToString("N2", Tr)}) arasında olmalıdır.";
                    a.Iadeler.Add(new NaIade { IslemId = islemId, Tarih = t, Tutar = tutar, Kasa = string.IsNullOrEmpty(ek1) ? Kasalar[0] : ek1, FisNo = "KG-" + (3100 + a.Iadeler.Count + a.Id * 5), IadeyiAlan = Finans().AdSoyad });
                    Hareket(a, t, $"{tutar.ToString("N0", Tr)} {a.ParaBirimi} iade alındı", "Finans/Kasa");
                    Log(a, t, "CASH_ADVANCE_RETURN", "RETURNED_AMOUNT", (a.IadeEdilen - tutar).ToString("N2", Tr), a.IadeEdilen.ToString("N2", Tr), "Sistem", "Event", "Finans/Kasa");
                    break;
                case "CashReturnReversed":
                    var ia = a.Iadeler.FirstOrDefault(i => i.IslemId == ek1);
                    if (ia == null) return "Geri alınacak iade bulunamadı.";
                    a.Iadeler.Remove(ia);
                    Hareket(a, t, $"{ia.Tutar.ToString("N0", Tr)} {a.ParaBirimi} iade geri alındı", "Finans/Kasa");
                    Log(a, t, "CASH_ADVANCE_RETURN", "STATUS", "İade", "Geri alındı", "Sistem", "Event", "Finans/Kasa");
                    break;
                case "IntercompanyPosted":
                    if (a.Yansitma == null) return "Bu talepte grup içi yansıtma gereksinimi yok.";
                    a.Yansitma.Durum = "Yansıtıldı"; a.Yansitma.BelgeNo = "IC-" + (5200 + a.Id);
                    Hareket(a, t, $"Grup içi yansıtma tamamlandı ({a.Yansitma.OdeyenSirket} → {a.Yansitma.MasrafSirketi}, {a.Yansitma.BelgeNo})", "ERP");
                    Log(a, t, "CASH_ADVANCE_INTERCOMPANY", "STATUS", "Bekliyor", "Yansıtıldı", "Sistem", "Event", "ERP");
                    break;
                default: return "Tanımsız olay: " + olay;
            }
            lock (Kilit) IslenenIslemler.Add(islemId);
            a.SonSenkron = t;
            OtomatikKapanis(a, t);
            return null;
        }

        /// <summary>§16: Teslim = Mahsup + İade sağlanınca sistem kapatır; açık bakiye oluşursa kapanış geri alınır.</summary>
        private static void OtomatikKapanis(NakitAvans a, DateTime t)
        {
            bool kapaliKaydi = a.Zaman.Count > 0 && a.Zaman.Last().Hareket.StartsWith("Avans kapatıldı");
            if (a.TeslimEdilen > 0 && a.AcikTutar == 0 && !kapaliKaydi)
            {
                Hareket(a, t, "Avans kapatıldı (Teslim = Mahsup + İade)", "Sistem");
                Log(a, t, "CASH_ADVANCE_HEADER", "SETTLEMENT_STATUS", "Açık", "Kapandı", "Sistem", "Event", "Sistem");
                if (a.Yansitma != null && a.Yansitma.Durum == "Bekliyor") Hareket(a, t, "Grup içi yansıtma gereksinimi oluştu", "Sistem");
            }
        }

        /// <summary>§12 Teslim alan dijital teyidi.</summary>
        public static string TeslimTeyidi(NakitAvans a, string islemId, string kullanici)
        {
            var od = a.Odemeler.FirstOrDefault(o => o.IslemId == islemId);
            if (od == null) return "Ödeme bulunamadı.";
            if (od.TeslimAlanOnayi) return "Bu teslim zaten teyit edilmiş.";
            od.TeslimAlanOnayi = true; od.TeslimOnayTarihi = DateTime.Now;
            Hareket(a, od.TeslimOnayTarihi.Value, $"Teslim teyidi: \"{od.Tutar.ToString("N0", Tr)} {a.ParaBirimi} nakit teslim aldım.\"", od.TeslimAlan);
            Log(a, od.TeslimOnayTarihi.Value, "CASH_ADVANCE_PAYMENT", "DELIVERY_CONFIRMED", "false", "true", kullanici, "Update", "Portal");
            return null;
        }

        /// <summary>§18 Açık avans kontrolü (canlı hesap).</summary>
        public static List<NakitAvans> AcikAvanslar(NaKisi k) => Talepler.Where(t => t.Kullanan.PersonelNo == k.PersonelNo && t.TeslimEdilen > 0 && !t.Kapandi).ToList();

        // ---------- Örnek tohum ----------
        private static void Tohum()
        {
            var bugun = DateTime.Today;
            NakitAvans T(string olusturan, string kullanan, decimal tutar, string pb, string konu, string aciklama, string kat, string fatura, string[] belgeler, string oncelik, int gunOnce, int kullanimGun, int kapatmaGun, string sirket = null)
            {
                var o = KisiBul(olusturan); var k = KisiBul(kullanan);
                var a = new NakitAvans
                {
                    Id = ++_sonId, No = $"NA-{bugun.Year}-{(_sonId + 140):000000}", Tarih = bugun.AddDays(-gunOnce).AddHours(10).AddMinutes(42),
                    Olusturan = o, Kullanan = k, Sirket = sirket ?? o.Sirket, Departman = k.Departman, MasrafMerkezi = k.MasrafMerkezi,
                    Konu = konu, Aciklama = aciklama, Tutar = tutar, ParaBirimi = pb, KullanimTarihi = bugun.AddDays(kullanimGun), KapatmaTarihi = bugun.AddDays(kapatmaGun),
                    FaturaDurumu = fatura, BelgeTurleri = belgeler.ToList(), HarcamaKategorisi = kat, Oncelik = oncelik
                };
                a.Onaylar = OnayAkisiOlustur(a, a.Tarih);
                if (a.GrupIci) a.Yansitma = new NaYansitma { OdeyenSirket = a.Sirket, MasrafSirketi = k.Sirket, Tutar = tutar, BelgeNo = "Otomatik/ERP", Durum = "Bekliyor" };
                Hareket(a, a.Tarih, "Talep oluşturuldu", o.AdSoyad);
                Log(a, a.Tarih, "CASH_ADVANCE_HEADER", "REQUEST_STATUS", "", a.TalepDurumu, o.KullaniciId, "Insert", "Portal");
                a.SonSenkron = a.Tarih;
                Talepler.Add(a);
                return a;
            }
            void OnaylaHepsi(NakitAvans a, int adet, double saat = 4.5)
            {
                for (int i = 0; i < adet; i++)
                {
                    var adim = a.Onaylar.FirstOrDefault(x => x.Durum == "Onay Bekliyor"); if (adim == null) break;
                    var t = a.Tarih.AddHours(saat * (i + 1));
                    adim.Durum = "Onaylandı"; adim.Tarih = t;
                    var sonraki = a.Onaylar.FirstOrDefault(x => x.Durum == "Pasif");
                    if (sonraki != null) sonraki.Durum = "Onay Bekliyor"; else a.OnaylananTutar = a.Tutar;
                    Hareket(a, t, adim.Adim == "Finans Uygunluk Onayı" ? "Finans uygunluk onayı verildi" : adim.Adim.Replace(" Onayı", "") + " onayladı", adim.Kisi);
                    Log(a, t, "CASH_ADVANCE_APPROVAL", "STATUS", "Onay Bekliyor", "Onaylandı", adim.Kisi, "Update", "Workflow");
                }
            }
            void Odeme(NakitAvans a, decimal tutar, int gunOnce, bool teyit = true)
            {
                var t = bugun.AddDays(-gunOnce).AddHours(16).AddMinutes(25);
                var od = new NaOdeme { IslemId = "FT-" + (90000 + a.Id * 10 + a.Odemeler.Count), Tarih = t, Tutar = tutar, ParaBirimi = a.ParaBirimi, Kasa = Kasalar[0], FisNo = "KÇ-" + (2600 + a.Id * 3 + a.Odemeler.Count), TeslimEden = "Berk Berberoğlu", TeslimAlan = a.Kullanan.AdSoyad, TeslimSekli = "Nakit", Durum = "Teslim Edildi", TeslimAlanOnayi = teyit, TeslimOnayTarihi = teyit ? t.AddMinutes(6) : (DateTime?)null };
                a.Odemeler.Add(od); IslenenIslemler.Add(od.IslemId);
                Hareket(a, t, $"{tutar.ToString("N0", Tr)} {a.ParaBirimi} teslim edildi", "Finans/Kasa");
                Log(a, t, "CASH_ADVANCE_PAYMENT", "PAID_AMOUNT", (a.TeslimEdilen - tutar).ToString("N2", Tr), a.TeslimEdilen.ToString("N2", Tr), "Sistem", "Event", "Finans/Kasa");
                if (teyit) Hareket(a, t.AddMinutes(6), $"Teslim teyidi: \"{tutar.ToString("N0", Tr)} {a.ParaBirimi} nakit teslim aldım.\"", a.Kullanan.AdSoyad);
                a.SonSenkron = t;
            }
            void Harcama(NakitAvans a, string belgeNo, string tedarikci, string tur, decimal tutar, int gunOnce, bool mahsup)
            {
                var t = bugun.AddDays(-gunOnce).AddHours(11).AddMinutes(34);
                var h = new NaHarcama { BelgeId = "DOC-" + belgeNo, Tarih = t, BelgeNo = belgeNo, Tedarikci = tedarikci, BelgeTuru = tur, Tutar = tutar, MahsupDurumu = mahsup ? "Mahsup Edildi" : "Kontrol Bekliyor", MuhasebeFisNo = mahsup ? "MF-" + (7400 + a.Id * 7 + a.Harcamalar.Count) : null, YevmiyeNo = mahsup ? "Y-" + (18800 + a.Id * 11 + a.Harcamalar.Count) : null, HesapKodu = mahsup ? "195.01.001" : null };
                a.Harcamalar.Add(h); IslenenIslemler.Add(h.BelgeId);
                Hareket(a, t, mahsup ? $"{tutar.ToString("N0", Tr)} {a.ParaBirimi} mahsup edildi ({belgeNo}, Fiş {h.MuhasebeFisNo})" : $"{tutar.ToString("N0", Tr)} {a.ParaBirimi} harcama belgesi kaydedildi ({belgeNo}) — muhasebe kontrolü bekliyor", "Muhasebe");
                Log(a, t, "CASH_ADVANCE_EXPENSE", "DOCUMENT_ID", "", h.BelgeId, "Sistem", "Event", "Muhasebe");
                a.SonSenkron = t;
            }
            void Iade(NakitAvans a, decimal tutar, int gunOnce)
            {
                var t = bugun.AddDays(-gunOnce).AddHours(15).AddMinutes(42);
                var i = new NaIade { IslemId = "RT-" + (70000 + a.Id * 10 + a.Iadeler.Count), Tarih = t, Tutar = tutar, Kasa = Kasalar[0], FisNo = "KG-" + (3100 + a.Id * 5 + a.Iadeler.Count), IadeyiAlan = "Berk Berberoğlu" };
                a.Iadeler.Add(i); IslenenIslemler.Add(i.IslemId);
                Hareket(a, t, $"{tutar.ToString("N0", Tr)} {a.ParaBirimi} iade alındı", "Finans/Kasa");
                Log(a, t, "CASH_ADVANCE_RETURN", "RETURNED_AMOUNT", (a.IadeEdilen - tutar).ToString("N2", Tr), a.IadeEdilen.ToString("N2", Tr), "Sistem", "Event", "Finans/Kasa");
                a.SonSenkron = t;
                OtomatikKapanis(a, t.AddMinutes(1));
            }

            // 1) Şartnamedeki örnek: NA-…-000146 benzeri, kısmi ödeme → mahsup → iade → kapandı, grup içi yansıtma (URS Makina)
            var a1 = T("Ertan Yavuz", "Ertan Yavuz", 50000, "TL", "Proje için gerekli malzeme alımları", "Saha projesi için acil teknik malzeme ve sarf alımı; teklifler ekte.", "02 - Teknik Malzeme", "Kısmen Faturalı", new[] { "Fatura", "Perakende Fiş" }, "Acil", 25, -24, -21, "Uras Holding");
            a1.Ekler.Add(new NaEk { Ad = "teklif-abc-ltd.pdf", Tur = "Teklif", Boyut = "212 KB", Tarih = a1.Tarih, Yukleyen = "Ertan Yavuz" });
            a1.Ekler.Add(new NaEk { Ad = "malzeme-listesi.xlsx", Tur = "Liste", Boyut = "38 KB", Tarih = a1.Tarih, Yukleyen = "Ertan Yavuz" });
            OnaylaHepsi(a1, 3, 1.8);
            Odeme(a1, 30000, 24); Odeme(a1, 20000, 23);
            Harcama(a1, "FTR-123", "ABC Ltd.", "Fatura", 15000, 23, true); Harcama(a1, "FTR-124", "XYZ A.Ş.", "Fatura", 12000, 23, true); Harcama(a1, "PF-005", "Market", "Perakende Fiş", 3500, 22, true); Harcama(a1, "FTR-131", "Delta Teknik", "E-Fatura", 11500, 22, true);
            Iade(a1, 8000, 22);
            a1.Yansitma.Durum = "Yansıtıldı"; a1.Yansitma.BelgeNo = "IC-5201";
            Hareket(a1, bugun.AddDays(-22).AddHours(16), "Grup içi yansıtma tamamlandı (Uras Holding → URS Makina, IC-5201)", "ERP");

            // 2) Giriş yapan kullanıcı (Ali Veli): açık avans, kısmi mahsup, kontrol bekleyen belge
            var a2 = T("Ali Veli", "Ali Veli", 18500, "TL", "Sunucu odası iklimlendirme yedek parça", "Klima kompresörü ve montaj sarfları; servis firması nakit çalışıyor.", "03 - Bakım / Onarım", "Faturalı", new[] { "E-Fatura" }, "Normal", 12, -10, 5);
            OnaylaHepsi(a2, 2, 5);
            Odeme(a2, 18500, 10);
            Harcama(a2, "EF-2026-8811", "Soğuk Teknik Ltd.", "E-Fatura", 12400, 8, true);
            Harcama(a2, "EF-2026-8830", "Soğuk Teknik Ltd.", "E-Fatura", 2800, 3, false);

            // 3) Ali Veli: gecikmiş açık avans (kapatma tarihi geçti)
            var a3 = T("Ali Veli", "Ali Veli", 6000, "TL", "Fuar stant sarf malzemesi", "Kimya fuarı stant kurulumu için küçük alımlar.", "12 - Operasyonel Gider", "Kısmen Faturalı", new[] { "Perakende Fiş", "Makbuz" }, "Normal", 40, -38, -12);
            OnaylaHepsi(a3, 2, 3);
            Odeme(a3, 6000, 38);
            Harcama(a3, "PF-311", "Kırtasiye Dünyası", "Perakende Fiş", 1350, 36, true);

            // 4) Ali Veli: üst yönetim onayı bekliyor (35.000 → parametre eşiği 25.000 üstü)
            var a4 = T("Ali Veli", "Mert Doğan", 35000, "TL", "Üretim hattı acil rulman ve kayış alımı", "Hat 2 duruşu; tedarikçi peşin çalışıyor, onay sonrası aynı gün teslim gerekiyor.", "11 - Acil Satın Alma", "Faturalı", new[] { "Fatura" }, "Çok Acil", 1, 1, 10);
            a4.Ekler.Add(new NaEk { Ad = "proforma-rulman.pdf", Tur = "Proforma", Boyut = "146 KB", Tarih = a4.Tarih, Yukleyen = "Ali Veli" });
            OnaylaHepsi(a4, 1, 3);

            // 5) Tolga Yaman (Selvi): finans uygunluk bekliyor, ödeme bekleyen listesine düşecek
            var a5 = T("Tolga Yaman", "Tolga Yaman", 9000, "TL", "Müşteri ziyareti yol ve temsil giderleri", "Ege bölgesi 3 günlük bayi ziyareti.", "10 - Temsil / Ağırlama", "Kısmen Faturalı", new[] { "Fatura", "Perakende Fiş" }, "Normal", 3, 2, 12);
            OnaylaHepsi(a5, 1, 6);

            // 6) Gizem Tan (Avrupa Paper): onaylı, ödeme bekliyor
            var a6 = T("Gizem Tan", "Gizem Tan", 2500, "USD", "Yurt dışı fuar kayıt ücreti", "Paper Expo katılım ve stant kayıt ödemesi.", "08 - Seyahat", "Faturalı", new[] { "Fatura" }, "Acil", 4, 3, 30);
            OnaylaHepsi(a6, 3, 4);

            // 7) Esra Kaya: kısmi ödendi, belge bekleniyor
            var a7 = T("Esra Kaya", "Esra Kaya", 24000, "TL", "Saha ekipmanı kalibrasyon ödemeleri", "Üç ayrı kalibrasyon firmasına nakit ödeme.", "04 - Saha Masrafı", "Faturalı", new[] { "Fatura", "E-Arşiv Fatura" }, "Normal", 9, -6, 8);
            OnaylaHepsi(a7, 2, 4);
            Odeme(a7, 15000, 6, false);

            // 8) Canan Su (Alv Kimya), masraf Uras Kimya'ya değil kendi şirketine; talebi Ali Veli açtı → grup içi yansıtma bekliyor
            var a8 = T("Ali Veli", "Canan Su", 4200, "TL", "Ofis taşınma nakliye ve hamaliye", "Alv Kimya ofis kat değişikliği; nakliyeci nakit istiyor.", "05 - Nakliye / Lojistik", "Faturasız", new[] { "Makbuz" }, "Normal", 15, -13, -2);
            OnaylaHepsi(a8, 2, 5);
            Odeme(a8, 4200, 13);
            Harcama(a8, "MKB-77", "Yıldız Nakliyat", "Makbuz", 4200, 12, true);
            OtomatikKapanis(a8, bugun.AddDays(-12).AddHours(12));

            // 9) Reddedilen talep
            var a9 = T("Ali Veli", "Ali Veli", 15000, "TL", "Kişisel dizüstü bilgisayar yenileme", "Mevcut cihaz yavaş.", "01 - İdari Alım", "Faturalı", new[] { "E-Fatura" }, "Normal", 20, -18, 10);
            { var adim = a9.Onaylar.First(x => x.Durum == "Onay Bekliyor"); adim.Durum = "Reddedildi"; adim.Tarih = a9.Tarih.AddHours(3); adim.Not = "Demirbaş alımı avansla değil satınalma talebiyle yapılmalı."; foreach (var o in a9.Onaylar.Where(o => o.Durum == "Pasif")) o.Durum = "İptal"; Hareket(a9, adim.Tarih.Value, "Yönetici Onayı — reddedildi (" + adim.Not + ")", adim.Kisi); }

            // 10) Revizyon bekleyen
            var a10 = T("Ali Veli", "Ali Veli", 40000, "TL", "Yedekleme ünitesi disk alımı", "NAS için 4 adet kurumsal disk.", "02 - Teknik Malzeme", "Faturalı", new[] { "E-Fatura" }, "Normal", 2, 3, 20);
            { var adim = a10.Onaylar.First(x => x.Durum == "Onay Bekliyor"); adim.Durum = "Revizyon"; adim.Tarih = a10.Tarih.AddHours(2); adim.Not = "Kapasite 2 kat artırılsın; tutarı güncelleyin."; Hareket(a10, adim.Tarih.Value, "Yönetici Onayı — revizyona gönderildi (" + adim.Not + ")", adim.Kisi); }

            // 11) Bugün kapanması gereken açık avans
            var a11 = T("Mert Doğan", "Mert Doğan", 7500, "TL", "Vardiya personeli yemek ve ulaşım", "Hafta sonu ek mesai yemek giderleri.", "12 - Operasyonel Gider", "Kısmen Faturalı", new[] { "Perakende Fiş" }, "Normal", 8, -7, 0);
            OnaylaHepsi(a11, 2, 4);
            Odeme(a11, 7500, 7);
            Harcama(a11, "PF-402", "Lezzet Lokantası", "Perakende Fiş", 5100, 2, true);

            // 12) Kısmi ödenen (ödeme bekleyen kalan)
            var a12 = T("Ertan Yavuz", "Ertan Yavuz", 20000, "TL", "Müşteri sahasında acil onarım malzemesi", "Kompresör revizyonu için parça.", "03 - Bakım / Onarım", "Faturalı", new[] { "Fatura" }, "Acil", 2, 0, 14, "Uras Holding");
            OnaylaHepsi(a12, 2, 3);
            Odeme(a12, 12000, 1);
        }
    }
}
