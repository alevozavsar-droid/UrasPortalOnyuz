// ============================================================
// YANSITMA MODÜLÜ — veri modeli, iş kuralları, muhasebe simülasyonu, kontrol motoru ve örnek kayıtlar
// Kaynak: "URAS HOLDING Yansıtma Modülü — IT Teknik ve İşlevsel Gereksinim Dokümanı v1.0 (21.09.2026)".
// Üç sekme aynı veri setini kullanır: İşlem Ekranı kayıt oluşturur (kaynak fatura + satır bazında seçim, kısmi miktar, kâr %,
// simülasyon, onay), Rapor izler, Muhasebe Kontrolü beklenen (snapshot) ile gerçekleşen fişi karşılaştırır.
// Kaynak faturalar Aylık Alım Onay Ekranı'ndaki tedarikçi faturalarıdır (Data/AlimOnayOrnek.cs); satır kimliği fatura id + satır no.
// Hesap kodları sabit değildir: REFLECTION_ACCOUNT_MAPPING karşılığı Esleme tablosundan gelir. Kayıtlar bellek içindedir.
// Ekran: Controllers/GrupIciController.Yansitma → Views/GrupIci/Yansitma.cshtml + wwwroot/js/yansitma.js; uçlar Controllers/YansitmaController.cs
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>REFLECTION_ALLOCATION — bir satırın birden fazla hedef şirkete dağıtımı.</summary>
    public class YnDagitim { public string Sirket { get; set; } public decimal Miktar { get; set; } public decimal KarPct { get; set; } }

    /// <summary>REFLECTION_LINE — kaynak fatura kaleminden türeyen yansıtma satırı.</summary>
    public class YnKalem
    {
        public int Id { get; set; }
        public int KaynakFaturaId { get; set; }          // source_doc_id
        public string KaynakFaturaNo { get; set; }       // source_invoice_no
        public int SatirNo { get; set; }                 // source_line_no
        public string KalemKod { get; set; }
        public string KalemAd { get; set; }
        public string Kategori { get; set; }             // hesap eşlemesi için kalem türü (Hammadde, Ambalaj, Hizmet, …)
        public string Birim { get; set; }
        public decimal FaturaMiktari { get; set; }       // source_qty
        public decimal OncedenYansitilan { get; set; }   // previously_reflected_qty (snapshot; kayıt anında yeniden hesaplanır)
        public decimal YansitilacakMiktar { get; set; }  // reflection_qty
        public decimal BirimMaliyet { get; set; }        // unit_cost (belge dövizinde, net)
        public decimal KarPct { get; set; }              // profit_pct
        public decimal BirimYansitma { get; set; }       // unit_reflection_price
        public bool ManuelFiyat { get; set; }            // birim yansıtma elle girildi → kâr % geriye hesaplanır
        public decimal KdvPct { get; set; }              // vat_pct
        public string MasrafMerkezi { get; set; }
        public string Proje { get; set; }
        public string Gerekce { get; set; }              // negatif kâr / manuel fiyat gerekçesi
        public int EslemeId { get; set; }                // mapping_id
        public List<YnDagitim> Dagitim { get; set; } = new List<YnDagitim>();

        public decimal KalanMiktar => Math.Max(0, FaturaMiktari - OncedenYansitilan);
        public decimal BirimKar => Math.Round(BirimMaliyet * KarPct / 100m, 6);
        public decimal ToplamMaliyet => Math.Round(YansitilacakMiktar * BirimMaliyet, 2);
        public decimal ToplamYansitma => Math.Round(Parcalar().Sum(p => p.Toplam), 2);
        public decimal KdvTutar => Math.Round(ToplamYansitma * KdvPct / 100m, 2);
        public decimal KdvDahil => ToplamYansitma + KdvTutar;
        public decimal ToplamKar => ToplamYansitma - ToplamMaliyet;

        /// <summary>Satırın hedef şirket parçaları: dağıtım yoksa tek parça (başlıktaki hedef şirket, satır kâr oranı).</summary>
        public List<(string Sirket, decimal Miktar, decimal KarPct, decimal BirimYansitma, decimal Toplam)> Parcalar(string varsayilanHedef = null)
        {
            if (Dagitim.Count == 0) return new List<(string, decimal, decimal, decimal, decimal)> { (varsayilanHedef, YansitilacakMiktar, KarPct, BirimYansitma, Math.Round(YansitilacakMiktar * BirimYansitma, 2)) };
            return Dagitim.Select(d => { var bf = Math.Round(BirimMaliyet * (1 + d.KarPct / 100m), 6); return (d.Sirket, d.Miktar, d.KarPct, bf, Math.Round(d.Miktar * bf, 2)); }).ToList();
        }
    }

    /// <summary>REFLECTION_ACCOUNT_SIM / REFLECTION_ACTUAL_POSTING satırı.</summary>
    public class YnFisSatir
    {
        public string Sirket { get; set; }
        public string FisNo { get; set; }                // yalnız gerçekleşen kayıtta
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public decimal Borc { get; set; }                // yerel para (TL)
        public decimal Alacak { get; set; }
        public string ParaBirimi { get; set; }
        public decimal Kur { get; set; }
        public decimal DovizTutar { get; set; }          // belge dövizinde (TRY ise TL ile aynı)
        public string CariKod { get; set; }
        public string MasrafMerkezi { get; set; }
        public string Proje { get; set; }
        public string KalemRef { get; set; }             // hangi yansıtma kaleminden geldiği (satır no / kalem adı)
        public string Aciklama { get; set; }
        public string Kural { get; set; }                // hangi eşleme kuralıyla seçildi
        public string Tip { get; set; }                  // CARI | GELIR | GIDER | KDV
        public string Referans { get; set; }             // fişte yansıtma no + kaynak belge referansı
    }

    public class YnOnay { public int Sira { get; set; } public string Adim { get; set; } public string Kisi { get; set; } public string Durum { get; set; } = "Bekliyor"; public DateTime? Tarih { get; set; } public string Not { get; set; } }
    public class YnAudit { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Alan { get; set; } public string Eski { get; set; } public string Yeni { get; set; } public string Kaynak { get; set; } public string Gerekce { get; set; } }
    public class YnKontrol { public string Alan { get; set; } public string Sirket { get; set; } public string Beklenen { get; set; } public string Gerceklesen { get; set; } public string Sonuc { get; set; } public bool Kritik { get; set; } }
    public class YnEntegrasyon { public string Durum { get; set; } = "BEKLIYOR"; public string Istek { get; set; } public string Cevap { get; set; } public DateTime? Zaman { get; set; } public string IdempotencyKey { get; set; } }

    /// <summary>REFLECTION_HEADER — yansıtma üst bilgisi.</summary>
    public class YnIslem
    {
        public int Id { get; set; }
        public string No { get; set; }                   // reflection_no (YNS-000125)
        public DateTime Tarih { get; set; }              // reflection_date
        public string Yansitan { get; set; }             // source_company
        public string Yansitilan { get; set; }           // target_company (birincil hedef; satır dağıtımı farklı şirketler ekleyebilir)
        public string Tur { get; set; }                  // Fatura / Masraf / Stok / Hizmet / Diğer
        public string KaynakBelgeTuru { get; set; }      // Alış Faturası vb.
        public string ParaBirimi { get; set; } = "TRY";
        public decimal Kur { get; set; } = 1;
        public string MasrafMerkezi { get; set; }
        public string Proje { get; set; }
        public string Departman { get; set; }
        public string Aciklama { get; set; }
        public string Durum { get; set; } = "Taslak";    // Taslak → Onay Bekliyor → Onaylandı → Fatura Oluştu → Muhasebeleşti → Uyumlu | Reddedildi | İptal
        public int KaynakFaturaId { get; set; }
        public string KaynakFaturaNo { get; set; }
        public string KaynakTedarikci { get; set; }
        public DateTime? KaynakFaturaTarihi { get; set; }
        public List<YnKalem> Kalemler { get; set; } = new List<YnKalem>();
        public List<YnFisSatir> Simulasyon { get; set; } = new List<YnFisSatir>();   // onaya gönderme anındaki snapshot
        public DateTime? SimulasyonTarihi { get; set; }
        public List<YnOnay> Onaylar { get; set; } = new List<YnOnay>();
        public List<YnAudit> Audit { get; set; } = new List<YnAudit>();
        public string YansitmaFaturaNo { get; set; }     // ERP belge no
        public string ErpDocEntry { get; set; }
        public YnEntegrasyon Entegrasyon { get; set; } = new YnEntegrasyon();
        public List<YnFisSatir> Gercek { get; set; } = new List<YnFisSatir>();      // ERP'den gelen gerçek yevmiye satırları
        public DateTime? GercekTarihi { get; set; }
        public string KontrolDurumu { get; set; }        // UYUMLU / KONTROL GEREKLİ / HESAP HATASI / … (boş: henüz kontrol yok)
        public List<YnKontrol> Kontroller { get; set; } = new List<YnKontrol>();
        public DateTime? KontrolTarihi { get; set; }
        public string RedNedeni { get; set; }
        public string IptalNedeni { get; set; }
        public string Olusturan { get; set; }
        public DateTime OlusturmaZamani { get; set; }
        public string Guncelleyen { get; set; }
        public DateTime GuncellemeZamani { get; set; }
        public int Versiyon { get; set; } = 1;
        public string OlusturanSirket { get; set; }
        public string OlusturanDepartman { get; set; }

        public decimal ToplamMaliyet => Kalemler.Sum(k => k.ToplamMaliyet);
        public decimal ToplamYansitma => Kalemler.Sum(k => k.ToplamYansitma);
        public decimal ToplamKar => ToplamYansitma - ToplamMaliyet;
        public decimal Kdv => Kalemler.Sum(k => k.KdvTutar);
        public decimal GenelToplam => ToplamYansitma + Kdv;
        public decimal AgirlikliKarPct => ToplamMaliyet > 0 ? Math.Round(ToplamKar / ToplamMaliyet * 100m, 2) : 0;
        public bool Aktif => Durum != "Reddedildi" && Durum != "İptal";
        public bool Onaylandi => Durum == "Onaylandı" || Durum == "Fatura Oluştu" || Durum == "Muhasebeleşti" || Durum == "Uyumlu";
        /// <summary>Hedef şirketler: başlık + satır dağıtımları.</summary>
        public IEnumerable<string> HedefSirketler => Kalemler.SelectMany(k => k.Parcalar(Yansitilan)).Select(p => p.Sirket).Where(s => !string.IsNullOrEmpty(s)).Distinct();
    }

    /// <summary>REFLECTION_ACCOUNT_MAPPING — Mali İşler'in yönettiği hesap eşleme kuralı ("*" = her değer).</summary>
    public class YnEsleme
    {
        public int Id { get; set; }
        public string Sirket { get; set; } = "*";
        public string YansitmaTuru { get; set; } = "*";
        public string Kategori { get; set; } = "*";
        public string GiderHesap { get; set; }           // yansıtılan tarafta borç (150/153/730/760/770 …)
        public string GiderHesapAdi { get; set; }
        public string GelirHesap { get; set; }           // yansıtan tarafta alacak (yansıtma / satış geliri)
        public string GelirHesapAdi { get; set; }
        public string KdvKodu { get; set; } = "KDV20";
        public bool MasrafMerkeziZorunlu { get; set; }
        public bool ProjeZorunlu { get; set; }
        public DateTime GecerlilikBas { get; set; } = new DateTime(2026, 1, 1);
        public DateTime? GecerlilikBit { get; set; }
        public bool Aktif { get; set; } = true;
        public int Ozgulluk => (Sirket == "*" ? 0 : 4) + (YansitmaTuru == "*" ? 0 : 2) + (Kategori == "*" ? 0 : 1);
    }

    // ---- ekran / servis giriş sözleşmeleri (JSON) ----
    public class YnDagitimGiris { public string Sirket { get; set; } public decimal Miktar { get; set; } public decimal KarPct { get; set; } }
    public class YnKalemGiris
    {
        public int KaynakFaturaId { get; set; } public int SatirNo { get; set; } public decimal YansitilacakMiktar { get; set; } public decimal KarPct { get; set; }
        public decimal? BirimYansitma { get; set; } public bool ManuelFiyat { get; set; } public decimal? KdvPct { get; set; } public string MasrafMerkezi { get; set; } public string Proje { get; set; } public string Gerekce { get; set; }
        public List<YnDagitimGiris> Dagitim { get; set; }
    }
    public class YnIslemGiris
    {
        public int Id { get; set; } public string Rol { get; set; } public string Tarih { get; set; } public string Yansitan { get; set; } public string Yansitilan { get; set; } public string Tur { get; set; } public string KaynakBelgeTuru { get; set; }
        public string ParaBirimi { get; set; } public decimal? Kur { get; set; } public string MasrafMerkezi { get; set; } public string Proje { get; set; } public string Departman { get; set; } public string Aciklama { get; set; }
        public int KaynakFaturaId { get; set; } public List<YnKalemGiris> Kalemler { get; set; } public string Gerekce { get; set; }
    }

    public class YnSonuc
    {
        public List<string> Hatalar { get; set; } = new List<string>();
        public List<string> Uyarilar { get; set; } = new List<string>();
        public bool Gecerli => Hatalar.Count == 0;
    }

    public static class YansitmaOrnek
    {
        public static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        public static readonly object Kilit = new object();
        public static readonly string[] Turler = { "Fatura", "Masraf", "Stok", "Hizmet", "Diğer" };
        public static readonly string[] KaynakBelgeTurleri = { "Alış Faturası", "Gider Pusulası", "Masraf Formu", "Stok Hareketi", "Hizmet Sözleşmesi" };
        public static readonly string[] ParaBirimleri = { "TRY", "USD", "EUR" };
        public static readonly string[] KontrolDurumlari = { "UYUMLU", "KONTROL GEREKLİ", "HESAP HATASI", "TUTAR FARKI", "KDV FARKI", "CARİ HESAP HATASI", "MASRAF MERKEZİ HATASI", "PROJE HATASI", "MUHASEBE KAYDI YOK", "MÜKERRER KAYIT" };
        /// <summary>Kontrol durumu önceliği: birden fazla fark varsa en kritik olan işlem durumu olur.</summary>
        public static readonly string[] KontrolOnceligi = { "MUHASEBE KAYDI YOK", "MÜKERRER KAYIT", "TUTAR FARKI", "KDV FARKI", "CARİ HESAP HATASI", "HESAP HATASI", "MASRAF MERKEZİ HATASI", "PROJE HATASI", "KONTROL GEREKLİ" };
        public static readonly string[] Durumlar = { "Taslak", "Onay Bekliyor", "Onaylandı", "Fatura Oluştu", "Muhasebeleşti", "Uyumlu", "Reddedildi", "İptal" };
        /// <summary>Yetki matrisi (§10): rol → izinli eylemler.</summary>
        public static readonly Dictionary<string, string[]> Yetkiler = new Dictionary<string, string[]>
        {
            ["Operasyon Kullanıcısı"] = new[] { "yeni", "faturaSec", "kar", "simulasyon", "onayaGonder", "taslak", "iptal", "kontrolGor" },
            ["Yönetici / Onaycı"] = new[] { "yeni", "faturaSec", "kar", "manuelFiyat", "negatifKar", "simulasyon", "onayaGonder", "taslak", "onay", "faturaOlustur", "iptal", "kontrolGor" },
            ["Muhasebe"] = new[] { "yeni", "taslak", "onayaGonder", "iptal", "faturaSec", "kar", "manuelFiyat", "negatifKar", "simulasyon", "onay", "faturaOlustur", "kontrol", "kontrolGor", "esleme", "fisAl" },   // yeni yansıtma: "opsiyonel" (§10) → açık
            ["Sistem Yöneticisi"] = new[] { "yeni", "faturaSec", "kar", "manuelFiyat", "negatifKar", "simulasyon", "onayaGonder", "taslak", "onay", "faturaOlustur", "iptal", "kontrol", "kontrolGor", "esleme", "fisAl" }
        };
        public static bool Yetkili(string rol, string eylem) => Yetkiler.TryGetValue(rol ?? "", out var l) ? l.Contains(eylem) : rol == null && Yetkiler["Operasyon Kullanıcısı"].Contains(eylem);

        // ---- Parametreler (onay akışı ve politika) ----
        public static readonly Dictionary<string, decimal> Parametreler = new Dictionary<string, decimal> { ["direktorEsik"] = 250000m, ["karUstSinir"] = 25m, ["kurTolerans"] = 0.5m, ["tutarTolerans"] = 1m };
        public static readonly Dictionary<string, string> ParametreEtiketler = new Dictionary<string, string> { ["direktorEsik"] = "Bu KDV dahil tutarı aşan yansıtmada Direktör onayı (TL)", ["karUstSinir"] = "Bu ağırlıklı kâr oranını (%) aşan işlemde Direktör onayı", ["kurTolerans"] = "Kur farkı toleransı (%)", ["tutarTolerans"] = "Tutar farkı toleransı (TL)" };

        // ---- Şirket kısa kodları ve karşı cari eşlemesi (merkezi konfigürasyon) ----
        public static readonly Dictionary<string, string> SirketKisa = new Dictionary<string, string> { ["Uras Kimya"] = "URAS", ["Selvi"] = "SELVI", ["Avrupa Paper"] = "AVPAP", ["Alv Kimya"] = "ALV", ["Daf Kimya"] = "DAF", ["Uras Kimya A.Ş."] = "URASAS", ["Uras Power"] = "UPOWER", ["Uras Holding"] = "UHOLD" };
        public static string Kisa(string s) => SirketKisa.TryGetValue(s ?? "", out var k) ? k : (s ?? "").ToUpperInvariant().Replace(" ", "");
        /// <summary>Yansıtan tarafta karşı şirket alacak carisi (120) ve yansıtılan tarafta borç carisi (320).</summary>
        public static string AlacakCari(string karsi) => "120.02." + Kisa(karsi);
        public static string BorcCari(string karsi) => "320.02." + Kisa(karsi);
        public static string HesaplananKdv(decimal pct) => "391.01." + pct.ToString("00", CultureInfo.InvariantCulture);
        public static string IndirilecekKdv(decimal pct) => "191.01." + pct.ToString("00", CultureInfo.InvariantCulture);
        public static readonly string[] Sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct().ToArray();
        public static readonly string[] MasrafMerkezleri = { "CC40 - Üretim", "CC41 - Bakım", "CC42 - Depo", "CC43 - Kalite", "CC10 - Bilgi Teknolojileri", "CC05 - İdari İşler", "CC20 - Finans", "CC30 - Operasyon", "CC21 - Muhasebe" };
        public static readonly string[] Projeler = { "", "PRJ-2026-01 Kapasite Artışı", "PRJ-2026-04 ERP Geçişi", "PRJ-2026-07 Enerji Verimliliği" };

        // ---- Hesap eşleme tablosu (REFLECTION_ACCOUNT_MAPPING) ----
        public static readonly List<YnEsleme> Esleme = new List<YnEsleme>
        {
            new YnEsleme { Id = 1, YansitmaTuru = "Stok", Kategori = "Hammadde", GiderHesap = "150.01.001", GiderHesapAdi = "İlk Madde ve Malzeme", GelirHesap = "600.05.001", GelirHesapAdi = "Grup İçi Yurtiçi Satışlar" },
            new YnEsleme { Id = 2, YansitmaTuru = "Stok", Kategori = "Ambalaj", GiderHesap = "150.02.001", GiderHesapAdi = "Ambalaj Malzemesi", GelirHesap = "600.05.001", GelirHesapAdi = "Grup İçi Yurtiçi Satışlar" },
            new YnEsleme { Id = 3, YansitmaTuru = "Stok", Kategori = "*", GiderHesap = "153.01.001", GiderHesapAdi = "Ticari Mallar", GelirHesap = "600.05.001", GelirHesapAdi = "Grup İçi Yurtiçi Satışlar" },
            new YnEsleme { Id = 4, YansitmaTuru = "Fatura", Kategori = "Hammadde", GiderHesap = "150.01.001", GiderHesapAdi = "İlk Madde ve Malzeme", GelirHesap = "600.05.001", GelirHesapAdi = "Grup İçi Yurtiçi Satışlar" },
            new YnEsleme { Id = 5, YansitmaTuru = "Fatura", Kategori = "Ambalaj", GiderHesap = "150.02.001", GiderHesapAdi = "Ambalaj Malzemesi", GelirHesap = "600.05.001", GelirHesapAdi = "Grup İçi Yurtiçi Satışlar" },
            new YnEsleme { Id = 6, YansitmaTuru = "Fatura", Kategori = "Yedek Parça", GiderHesap = "730.01.005", GiderHesapAdi = "Genel Üretim Gideri — Bakım Onarım", GelirHesap = "770.90.001", GelirHesapAdi = "Yansıtılan Giderler (-)", MasrafMerkeziZorunlu = true },
            new YnEsleme { Id = 7, YansitmaTuru = "*", Kategori = "Enerji", GiderHesap = "730.01.010", GiderHesapAdi = "Genel Üretim Gideri — Enerji", GelirHesap = "770.90.001", GelirHesapAdi = "Yansıtılan Giderler (-)", MasrafMerkeziZorunlu = true },
            new YnEsleme { Id = 8, YansitmaTuru = "*", Kategori = "IT / Lisans", GiderHesap = "770.01.020", GiderHesapAdi = "Genel Yönetim Gideri — IT / Lisans", GelirHesap = "770.90.001", GelirHesapAdi = "Yansıtılan Giderler (-)", MasrafMerkeziZorunlu = true },
            new YnEsleme { Id = 9, YansitmaTuru = "Hizmet", Kategori = "*", GiderHesap = "770.01.030", GiderHesapAdi = "Genel Yönetim Gideri — Dışarıdan Sağlanan Hizmet", GelirHesap = "649.01.001", GelirHesapAdi = "Yansıtma / Hizmet Gelirleri", MasrafMerkeziZorunlu = true },
            new YnEsleme { Id = 10, YansitmaTuru = "Masraf", Kategori = "Lojistik", GiderHesap = "760.01.004", GiderHesapAdi = "Pazarlama Satış Dağıtım — Nakliye", GelirHesap = "770.90.001", GelirHesapAdi = "Yansıtılan Giderler (-)", MasrafMerkeziZorunlu = true },
            new YnEsleme { Id = 11, YansitmaTuru = "Masraf", Kategori = "*", GiderHesap = "770.01.001", GiderHesapAdi = "Genel Yönetim Gideri", GelirHesap = "770.90.001", GelirHesapAdi = "Yansıtılan Giderler (-)", MasrafMerkeziZorunlu = true },
            new YnEsleme { Id = 12, YansitmaTuru = "Fatura", Kategori = "*", GiderHesap = "770.01.001", GiderHesapAdi = "Genel Yönetim Gideri", GelirHesap = "770.90.001", GelirHesapAdi = "Yansıtılan Giderler (-)", MasrafMerkeziZorunlu = true },
            new YnEsleme { Id = 13, YansitmaTuru = "Diğer", Kategori = "*", GiderHesap = "689.01.001", GiderHesapAdi = "Diğer Olağandışı Gider ve Zararlar", GelirHesap = "679.01.001", GelirHesapAdi = "Diğer Olağandışı Gelir ve Kârlar" },
            new YnEsleme { Id = 14, Sirket = "Uras Power", YansitmaTuru = "*", Kategori = "Enerji", GiderHesap = "730.01.011", GiderHesapAdi = "GES Üretim Gideri — Enerji", GelirHesap = "770.90.001", GelirHesapAdi = "Yansıtılan Giderler (-)", MasrafMerkeziZorunlu = true, ProjeZorunlu = true },
            new YnEsleme { Id = 15, YansitmaTuru = "Masraf", Kategori = "Demirbaş", GiderHesap = "255.01.001", GiderHesapAdi = "Demirbaşlar", GelirHesap = "679.01.001", GelirHesapAdi = "Diğer Olağandışı Gelir ve Kârlar", Aktif = false }   // pasif örnek
        };
        /// <summary>En özgül aktif ve tarih aralığında geçerli kural (şirket = hedef şirket).</summary>
        public static YnEsleme EslemeBul(string hedefSirket, string tur, string kategori, DateTime tarih) =>
            Esleme.Where(e => e.Aktif && e.GecerlilikBas <= tarih && (e.GecerlilikBit == null || e.GecerlilikBit >= tarih) && (e.Sirket == "*" || e.Sirket == hedefSirket) && (e.YansitmaTuru == "*" || e.YansitmaTuru == tur) && (e.Kategori == "*" || e.Kategori == kategori))
                  .OrderByDescending(e => e.Ozgulluk).FirstOrDefault();

        // ---- Kayıtlar ----
        private static readonly List<YnIslem> _islemler = new List<YnIslem>();
        private static bool _tohumlandi; private static int _sonId = 120;
        public static List<YnIslem> Islemler { get { Tohumla(); return _islemler; } }
        public static YnIslem Bul(string no) => Islemler.FirstOrDefault(i => string.Equals(i.No, no, StringComparison.OrdinalIgnoreCase));
        public static YnIslem Bul(int id) => Islemler.FirstOrDefault(i => i.Id == id);
        private static string YeniNo() => $"YNS-{(++_sonId):000000}";
        private static string N(decimal d, int b = 2) => d.ToString("N" + b, Tr);

        // =====================================================================
        // KAYNAK FATURA VE KALEM (ERP'den benzersiz belge / satır kimliğiyle)
        // =====================================================================
        /// <summary>Aynı kaynak satırdan (belge id + satır no) daha önce yansıtılan toplam miktar; iptal / red hariç, taslak ve onay bekleyen dahil (mükerrer engeli).</summary>
        public static decimal OncedenYansitilan(int faturaId, int satirNo, int haricIslemId = 0) =>
            Islemler.Where(i => i.Id != haricIslemId && i.Aktif).SelectMany(i => i.Kalemler).Where(k => k.KaynakFaturaId == faturaId && k.SatirNo == satirNo).Sum(k => k.YansitilacakMiktar);
        public static decimal BirimMaliyet(AlimKalem k, AlimFatura f) => Math.Round(k.BirimFiyat * (1 - k.Iskonto / 100m) / (f.Kur <= 0 ? 1 : f.Kur), 6);   // belge dövizinde net birim maliyet
        public static string ParaBirimi(AlimFatura f) => f.ParaBirimi == "TL" ? "TRY" : f.ParaBirimi;

        /// <summary>Kaynak Fatura Seç penceresi: yansıtan şirkete ait, seçilebilir faturalar (iptal / reddedilmiş ve tamamen yansıtılmış belgeler kilitli).</summary>
        public static IEnumerable<object> KaynakFaturalar(string yansitan, string ara, int haricIslemId = 0)
        {
            ara = (ara ?? "").Trim().ToLower(Tr);
            return AlimOnayOrnek.Faturalar.Where(f => f.FaturaSirketi == yansitan)
                .Where(f => ara == "" || (f.FaturaNo + " " + f.Tedarikci + " " + f.Aciklama + " " + f.Kategori + " " + string.Join(" ", f.Kalemler.Select(k => k.Aciklama))).ToLower(Tr).Contains(ara))
                .OrderByDescending(f => f.Tarih).ThenByDescending(f => f.Id)
                .Select(f =>
                {
                    var kalemler = f.Kalemler.Select(k => { var on = OncedenYansitilan(f.Id, k.Sira, haricIslemId); var bm = BirimMaliyet(k, f); return new { satirNo = k.Sira, kod = k.Kod, ad = k.Aciklama, kategori = f.Kategori, miktar = k.Miktar, birim = k.Birim, birimMaliyet = bm, kdvPct = k.KdvOrani, oncedenYansitilan = on, kalan = Math.Max(0, k.Miktar - on), toplam = Math.Round(k.Miktar * bm, 2) }; }).ToList();
                    decimal netDoviz = kalemler.Sum(k => k.toplam), yansitilanNet = kalemler.Sum(k => Math.Round(k.oncedenYansitilan * k.birimMaliyet, 2)), kalanNet = netDoviz - yansitilanNet;
                    bool iptal = f.OnayDurumu == "Reddedildi", tamam = kalemler.All(k => k.kalan <= 0);
                    string kilit = iptal ? "Reddedilmiş / iptal belge" : tamam ? "Tamamen yansıtılmış" : "";
                    return new
                    {
                        id = f.Id, faturaNo = f.FaturaNo, tarih = f.Tarih.ToString("yyyy-MM-dd"), tarihTr = f.Tarih.ToString("dd.MM.yyyy"), tedarikci = f.Tedarikci, aciklama = f.Aciklama, kategori = f.Kategori, faturaTuru = f.FaturaTuru,
                        paraBirimi = ParaBirimi(f), kur = f.Kur, net = netDoviz, kdv = Math.Round(kalemler.Sum(k => k.toplam * k.kdvPct / 100m), 2), genelToplam = Math.Round(netDoviz + kalemler.Sum(k => k.toplam * k.kdvPct / 100m), 2),
                        yansitilanTutar = yansitilanNet, kalanYansitilabilir = kalanNet, kullanimSirketi = f.KullanimSirketi, masrafMerkezi = f.MasrafMerkezi, muhasebeFisNo = f.MuhasebeFisNo, kilitli = kilit != "", kilitNedeni = kilit, kalemler
                    };
                });
        }

        // =====================================================================
        // HESAPLAMA (§3.1) ve DOĞRULAMA (§13)
        // =====================================================================
        /// <summary>Girişten kalem kurar: kaynak satır ERP'den okunur (salt okunur alanlar), kullanıcı miktar / kâr / fiyat alanlarını verir.</summary>
        public static YnKalem KalemKur(YnKalemGiris g, string rol, int haricIslemId, YnSonuc s)
        {
            var f = AlimOnayOrnek.Bul(g.KaynakFaturaId); var ak = f?.Kalemler.FirstOrDefault(k => k.Sira == g.SatirNo);
            if (ak == null) { s.Hatalar.Add($"Kaynak satır bulunamadı (belge {g.KaynakFaturaId} / satır {g.SatirNo})."); return null; }
            var k = new YnKalem
            {
                KaynakFaturaId = f.Id, KaynakFaturaNo = f.FaturaNo, SatirNo = ak.Sira, KalemKod = ak.Kod, KalemAd = ak.Aciklama, Kategori = f.Kategori, Birim = ak.Birim, FaturaMiktari = ak.Miktar,
                OncedenYansitilan = OncedenYansitilan(f.Id, ak.Sira, haricIslemId), YansitilacakMiktar = g.YansitilacakMiktar, BirimMaliyet = BirimMaliyet(ak, f), KarPct = g.KarPct, KdvPct = g.KdvPct ?? ak.KdvOrani,
                MasrafMerkezi = (g.MasrafMerkezi ?? "").Trim(), Proje = (g.Proje ?? "").Trim(), Gerekce = (g.Gerekce ?? "").Trim(), ManuelFiyat = g.ManuelFiyat && g.BirimYansitma.HasValue
            };
            if (g.Dagitim != null && g.Dagitim.Count > 0) { k.Dagitim = g.Dagitim.Where(d => !string.IsNullOrEmpty(d.Sirket) && d.Miktar > 0).Select(d => new YnDagitim { Sirket = d.Sirket, Miktar = d.Miktar, KarPct = d.KarPct }).ToList(); if (k.Dagitim.Count > 0) k.YansitilacakMiktar = k.Dagitim.Sum(d => d.Miktar); }
            Hesapla(k, g.BirimYansitma);
            return k;
        }
        /// <summary>Birim yansıtma = maliyet + kâr; manuel fiyatta kâr % geriye hesaplanır (§4.6). Toplam yansıtma hiçbir zaman elle girilmez.</summary>
        public static void Hesapla(YnKalem k, decimal? manuelBirim = null)
        {
            if (k.ManuelFiyat && manuelBirim.HasValue && k.BirimMaliyet > 0) { k.BirimYansitma = Math.Round(manuelBirim.Value, 6); k.KarPct = Math.Round((k.BirimYansitma - k.BirimMaliyet) / k.BirimMaliyet * 100m, 4); }
            else { k.ManuelFiyat = false; k.BirimYansitma = Math.Round(k.BirimMaliyet + k.BirimKar, 6); }
        }
        /// <summary>V01–V13: blokaj hataları ve uyarılar. Onaya göndermede tamamı, taslakta yalnız yapısal olanlar uygulanır.</summary>
        public static YnSonuc Dogrula(YnIslem i, string rol, bool taslak)
        {
            var s = new YnSonuc();
            if (string.IsNullOrEmpty(i.Yansitan) || string.IsNullOrEmpty(i.Yansitilan)) s.Hatalar.Add("Yansıtan ve yansıtılan şirket seçilmelidir.");
            else if (i.Yansitan == i.Yansitilan) s.Hatalar.Add("V01 Yansıtan ve yansıtılan şirket aynı olamaz.");
            if (i.Kalemler.Count == 0) s.Hatalar.Add("V02 En az bir kaynak kalem seçilmelidir.");
            foreach (var k in i.Kalemler)
            {
                string ad = $"Satır {k.SatirNo} ({k.KalemAd})";
                if (k.YansitilacakMiktar <= 0) s.Hatalar.Add($"V03 {ad}: yansıtılacak miktar sıfırdan büyük olmalıdır.");
                if (k.YansitilacakMiktar > k.KalanMiktar) s.Hatalar.Add($"V04 {ad}: yansıtılacak miktar ({N(k.YansitilacakMiktar, 3)}) kalan miktarı ({N(k.KalanMiktar, 3)} {k.Birim}) aşamaz.");
                if (k.Dagitim.Count > 0)
                {
                    if (k.Dagitim.Sum(d => d.Miktar) > k.KalanMiktar) s.Hatalar.Add($"V05 {ad}: toplam dağıtım miktarı kalan miktarı aşamaz.");
                    if (k.Dagitim.Any(d => d.Sirket == i.Yansitan)) s.Hatalar.Add($"V01 {ad}: dağıtımda yansıtan şirket hedef olamaz.");
                    var tekrar = k.Dagitim.GroupBy(d => d.Sirket).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                    if (tekrar.Count > 0) s.Uyarilar.Add($"{ad}: {string.Join(", ", tekrar)} için birden fazla dağıtım satırı var; simülasyonda birleştirilir.");
                }
                if (k.BirimMaliyet < 0) s.Hatalar.Add($"V06 {ad}: birim maliyet negatif olamaz.");
                if (k.BirimYansitma <= 0 && !(k.KarPct < 0 && Yetkili(rol, "negatifKar"))) s.Hatalar.Add($"V07 {ad}: birim yansıtma tutarı sıfır / negatif olamaz.");
                if (k.KarPct < 0 || k.Dagitim.Any(d => d.KarPct < 0)) { if (!Yetkili(rol, "negatifKar")) s.Hatalar.Add($"V08 {ad}: negatif kâr / iskonto için özel yetki gerekir."); else if (string.IsNullOrEmpty(k.Gerekce)) s.Hatalar.Add($"V08 {ad}: negatif kâr / iskonto gerekçesi zorunludur."); }
                if (k.ManuelFiyat && !Yetkili(rol, "manuelFiyat")) s.Hatalar.Add($"{ad}: birim yansıtma tutarını manuel değiştirme yetkiniz yok.");
                if (k.KdvPct < 0) s.Hatalar.Add($"V09 {ad}: KDV oranı boş bırakılamaz.");
                if (k.KdvPct == 0) s.Uyarilar.Add($"{ad}: KDV %0 — vergisiz işlem kuralı (istisna kodu) ayrıca tanımlı olmalıdır.");
                if (k.KarPct > Parametreler["karUstSinir"]) s.Uyarilar.Add($"{ad}: kâr oranı %{N(k.KarPct, 1)} politika üst sınırını (%{N(Parametreler["karUstSinir"], 0)}) aşıyor; Direktör onayı eklenir.");
                foreach (var p in k.Parcalar(i.Yansitilan))
                {
                    var e = EslemeBul(p.Sirket, i.Tur, k.Kategori, i.Tarih);
                    if (e == null) s.Hatalar.Add($"V12 {ad}: {p.Sirket} / {i.Tur} / {k.Kategori} için muhasebe hesap eşlemesi bulunamadı; Mali İşler eşleme tablosuna kural eklemeli.");
                    else
                    {
                        if (e.MasrafMerkeziZorunlu && string.IsNullOrEmpty(k.MasrafMerkezi) && string.IsNullOrEmpty(i.MasrafMerkezi)) (taslak ? s.Uyarilar : s.Hatalar).Add($"V11 {ad}: {e.GiderHesap} hesabı için masraf merkezi zorunludur.");
                        if (e.ProjeZorunlu && string.IsNullOrEmpty(k.Proje) && string.IsNullOrEmpty(i.Proje)) (taslak ? s.Uyarilar : s.Hatalar).Add($"V11 {ad}: {p.Sirket} için proje zorunludur.");
                    }
                }
            }
            if (i.ParaBirimi != "TRY" && i.Kur <= 0) s.Hatalar.Add("V10 Dövizli işlemde kur sıfırdan büyük olmalıdır.");
            if (!taslak && string.IsNullOrWhiteSpace(i.Aciklama)) s.Hatalar.Add("Açıklama zorunludur.");
            var f = AlimOnayOrnek.Bul(i.KaynakFaturaId); if (f != null && f.OnayDurumu == "Reddedildi") s.Hatalar.Add("V14 Kaynak belge reddedilmiş / iptal; seçilemez.");
            if (f != null && f.KullanimSirketi != f.FaturaSirketi && !i.HedefSirketler.Contains(f.KullanimSirketi)) s.Uyarilar.Add($"Kaynak faturanın kullanıcı firması {f.KullanimSirketi}; hedef şirketler arasında yok.");
            if (i.Tarih > DateTime.Today) s.Uyarilar.Add("Yansıtma tarihi ileri tarihli.");
            if (f != null && i.Tarih < f.Tarih) s.Uyarilar.Add("Yansıtma tarihi kaynak fatura tarihinden önce.");
            if (i.GenelToplam > Parametreler["direktorEsik"]) s.Uyarilar.Add($"KDV dahil toplam {N(i.GenelToplam, 0)} TL, {N(Parametreler["direktorEsik"], 0)} TL eşiğini aşıyor; Direktör onayı eklenir.");
            return s;
        }

        // =====================================================================
        // ONAY AKIŞI (§9) — şirket, tutar, tür ve kâr oranına göre parametrik
        // =====================================================================
        public static List<YnOnay> OnayAkisi(YnIslem i)
        {
            var l = new List<YnOnay>();
            var muh = NakitAvansOrnek.Personel.FirstOrDefault(p => p.Rol == "Muhasebe" && p.Sirket == i.Yansitan) ?? NakitAvansOrnek.Personel.First(p => p.Rol == "Muhasebe");
            l.Add(new YnOnay { Sira = 1, Adim = "Muhasebe Onayı", Kisi = muh.AdSoyad });
            bool direktor = i.GenelToplam * i.Kur > Parametreler["direktorEsik"] || i.AgirlikliKarPct > Parametreler["karUstSinir"] || i.Kalemler.Any(k => k.KarPct < 0) || i.Tur == "Hizmet";
            if (direktor) l.Add(new YnOnay { Sira = l.Count + 1, Adim = "Direktör Onayı (Mali İşler)", Kisi = NakitAvansOrnek.UstYonetim(muh).AdSoyad });
            l.Add(new YnOnay { Sira = l.Count + 1, Adim = "Finans Onayı", Kisi = NakitAvansOrnek.Finans().AdSoyad });
            return l;
        }
        public static string OnayNedeni(YnIslem i)
        {
            var n = new List<string>();
            if (i.GenelToplam * i.Kur > Parametreler["direktorEsik"]) n.Add("tutar eşiği");
            if (i.AgirlikliKarPct > Parametreler["karUstSinir"]) n.Add("kâr oranı üst sınırı");
            if (i.Kalemler.Any(k => k.KarPct < 0)) n.Add("negatif kâr / iskonto");
            if (i.Tur == "Hizmet") n.Add("hizmet yansıtması (transfer fiyatlandırması)");
            return n.Count == 0 ? "standart akış" : "Direktör: " + string.Join(", ", n);
        }

        // =====================================================================
        // MUHASEBE KAYDI SİMÜLASYONU (§5) — iki şirketin beklenen yevmiye satırları
        // =====================================================================
        public static (List<YnFisSatir> Satirlar, YnSonuc Sonuc) Simule(YnIslem i)
        {
            var s = new YnSonuc(); var l = new List<YnFisSatir>();
            if (string.IsNullOrEmpty(i.Yansitan) || i.Kalemler.Count == 0) { s.Hatalar.Add("Simülasyon için yansıtan şirket ve en az bir kalem gerekir."); return (l, s); }
            decimal kur = i.ParaBirimi == "TRY" ? 1 : (i.Kur <= 0 ? 1 : i.Kur);
            string pb = i.ParaBirimi, kaynak = $"{i.No ?? "TASLAK"} · {i.KaynakFaturaNo}";
            YnFisSatir S(string sirket, string hesap, string ad, decimal borc, decimal alacak, string tip, string kalemRef, string acik, string kural, string cari = null, string mm = null, string prj = null) =>
                new YnFisSatir { Sirket = sirket, HesapKodu = hesap, HesapAdi = ad, Borc = Math.Round(borc * kur, 2), Alacak = Math.Round(alacak * kur, 2), DovizTutar = Math.Round(borc + alacak, 2), ParaBirimi = pb, Kur = kur, Tip = tip, KalemRef = kalemRef, Aciklama = $"{kaynak} · {acik}", Kural = kural, CariKod = cari, MasrafMerkezi = mm, Proje = prj, Referans = kaynak };
            // hedef şirket parçaları: satır → (hedef, miktar, kâr, toplam)
            var parcalar = i.Kalemler.SelectMany(k => k.Parcalar(i.Yansitilan).Select(p => (Kalem: k, p.Sirket, p.Miktar, p.KarPct, p.Toplam, Kdv: Math.Round(p.Toplam * k.KdvPct / 100m, 2)))).ToList();
            foreach (var hedef in parcalar.Select(p => p.Sirket).Distinct())
            {
                var ph = parcalar.Where(p => p.Sirket == hedef).ToList();
                decimal net = ph.Sum(p => p.Toplam), kdv = ph.Sum(p => p.Kdv), brut = net + kdv;
                // ---- Yansıtan şirket: 120 hedef cari (B) / gelir hesabı (A) / 391 hesaplanan KDV (A)
                l.Add(S(i.Yansitan, AlacakCari(hedef), $"Alıcılar — {hedef} (grup)", brut, 0, "CARI", "başlık", $"{hedef} yansıtma faturası alacağı", "Karşı şirket cari eşlemesi", AlacakCari(hedef)));
                foreach (var p in ph)
                {
                    var e = EslemeBul(hedef, i.Tur, p.Kalem.Kategori, i.Tarih);
                    string mm = string.IsNullOrEmpty(p.Kalem.MasrafMerkezi) ? i.MasrafMerkezi : p.Kalem.MasrafMerkezi, prj = string.IsNullOrEmpty(p.Kalem.Proje) ? i.Proje : p.Kalem.Proje, kref = $"Satır {p.Kalem.SatirNo} · {p.Kalem.KalemAd}";
                    if (e == null) { s.Hatalar.Add($"Hesap eşlemesi yok: {hedef} / {i.Tur} / {p.Kalem.Kategori} (satır {p.Kalem.SatirNo})."); continue; }
                    l.Add(S(i.Yansitan, e.GelirHesap, e.GelirHesapAdi, 0, p.Toplam, "GELIR", kref, $"net yansıtma · kâr %{p.KarPct.ToString("0.##", Tr)}", $"Eşleme #{e.Id} ({(e.Sirket == "*" ? "tüm şirketler" : e.Sirket)} / {e.YansitmaTuru} / {e.Kategori})", null, i.Tur == "Stok" ? null : mm, prj));
                    if (e.MasrafMerkeziZorunlu && string.IsNullOrEmpty(mm)) s.Uyarilar.Add($"{hedef} · satır {p.Kalem.SatirNo}: {e.GiderHesap} için masraf merkezi eksik.");
                    if (e.ProjeZorunlu && string.IsNullOrEmpty(prj)) s.Uyarilar.Add($"{hedef} · satır {p.Kalem.SatirNo}: proje eksik.");
                }
                foreach (var g in ph.GroupBy(p => p.Kalem.KdvPct).Where(g => g.Sum(x => x.Kdv) != 0)) l.Add(S(i.Yansitan, HesaplananKdv(g.Key), $"Hesaplanan KDV %{g.Key.ToString("0.##", Tr)}", 0, g.Sum(x => x.Kdv), "KDV", "başlık", $"KDV %{g.Key.ToString("0.##", Tr)}", $"Vergi kodu KDV{g.Key:00}"));
                // ---- Yansıtılan şirket: stok / gider hesabı (B) / 191 indirilecek KDV (B) / 320 yansıtan cari (A)
                foreach (var p in ph)
                {
                    var e = EslemeBul(hedef, i.Tur, p.Kalem.Kategori, i.Tarih); if (e == null) continue;
                    string mm = string.IsNullOrEmpty(p.Kalem.MasrafMerkezi) ? i.MasrafMerkezi : p.Kalem.MasrafMerkezi, prj = string.IsNullOrEmpty(p.Kalem.Proje) ? i.Proje : p.Kalem.Proje;
                    l.Add(S(hedef, e.GiderHesap, e.GiderHesapAdi, p.Toplam, 0, "GIDER", $"Satır {p.Kalem.SatirNo} · {p.Kalem.KalemAd}", $"{p.Miktar.ToString("0.###", Tr)} {p.Kalem.Birim} × {(p.Toplam / (p.Miktar == 0 ? 1 : p.Miktar)).ToString("N2", Tr)}", $"Eşleme #{e.Id}", null, mm, prj));
                }
                foreach (var g in ph.GroupBy(p => p.Kalem.KdvPct).Where(g => g.Sum(x => x.Kdv) != 0)) l.Add(S(hedef, IndirilecekKdv(g.Key), $"İndirilecek KDV %{g.Key.ToString("0.##", Tr)}", g.Sum(x => x.Kdv), 0, "KDV", "başlık", $"KDV %{g.Key.ToString("0.##", Tr)}", $"Vergi kodu KDV{g.Key:00}"));
                l.Add(S(hedef, BorcCari(i.Yansitan), $"Satıcılar — {i.Yansitan} (grup)", 0, brut, "CARI", "başlık", $"{i.Yansitan} grup şirketine borç", "Karşı şirket cari eşlemesi", BorcCari(i.Yansitan)));
            }
            // ---- Validasyonlar (§5.5)
            foreach (var g in l.GroupBy(x => x.Sirket)) { decimal b = g.Sum(x => x.Borc), a = g.Sum(x => x.Alacak); if (Math.Abs(b - a) > 0.01m) s.Hatalar.Add($"V13 {g.Key}: borç ({N(b)}) ≠ alacak ({N(a)})."); }
            if (kur != 1) s.Uyarilar.Add($"Dövizli işlem: {pb} kuru {kur.ToString("N4", Tr)} (TCMB döviz alış, {i.Tarih:dd.MM.yyyy}); TL tutarlar bu kurla hesaplandı.");
            return (l, s);
        }

        // =====================================================================
        // İŞLEM EYLEMLERİ (durum makinesi §9)
        // =====================================================================
        private static void Log(YnIslem i, DateTime z, string kullanici, string alan, string eski, string yeni, string kaynak = "Ekran", string gerekce = null) => i.Audit.Add(new YnAudit { Zaman = z, Kullanici = kullanici, Alan = alan, Eski = eski ?? "", Yeni = yeni ?? "", Kaynak = kaynak, Gerekce = gerekce ?? "" });
        private static string P(decimal d) => d.ToString("0.####", CultureInfo.InvariantCulture);

        /// <summary>Taslak kaydet / güncelle (id = 0 yeni). Yalnız Taslak ve Reddedildi (revize) durumunda düzenlenebilir; miktar / kâr / fiyat değişiklikleri alan bazında loglanır.</summary>
        public static (YnIslem Islem, YnSonuc Sonuc) Kaydet(YnIslemGiris g, string kullanici, NaKisi ben, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var s = new YnSonuc();
                var i = g.Id > 0 ? Bul(g.Id) : null;
                if (g.Id > 0 && i == null) { s.Hatalar.Add("İşlem bulunamadı."); return (null, s); }
                if (i != null && i.Durum != "Taslak" && i.Durum != "Reddedildi") { s.Hatalar.Add($"V16/V17 {i.Durum} durumundaki işlem düzenlenemez; onay akışı geri alınmalı ya da yeni versiyon açılmalıdır."); return (i, s); }
                var yeni = i == null;
                if (yeni) { i = new YnIslem { Id = ++_sonId, No = $"YNS-{_sonId:000000}", Olusturan = kullanici, OlusturmaZamani = z, OlusturanSirket = ben?.Sirket, OlusturanDepartman = ben?.Departman }; }
                var f = AlimOnayOrnek.Bul(g.KaynakFaturaId);
                var eskiKalemler = i.Kalemler.ToDictionary(k => k.KaynakFaturaId + "/" + k.SatirNo);
                string eskiHedef = i.Yansitilan;
                i.Tarih = DateTime.TryParseExact(g.Tarih ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : (i.Tarih == default ? z.Date : i.Tarih);
                i.Yansitan = g.Yansitan; i.Yansitilan = g.Yansitilan; i.Tur = string.IsNullOrEmpty(g.Tur) ? "Fatura" : g.Tur; i.KaynakBelgeTuru = string.IsNullOrEmpty(g.KaynakBelgeTuru) ? "Alış Faturası" : g.KaynakBelgeTuru;
                i.ParaBirimi = f != null ? ParaBirimi(f) : (g.ParaBirimi ?? "TRY"); i.Kur = i.ParaBirimi == "TRY" ? 1 : (g.Kur ?? (f?.Kur ?? 1));
                i.MasrafMerkezi = (g.MasrafMerkezi ?? "").Trim(); i.Proje = (g.Proje ?? "").Trim(); i.Departman = (g.Departman ?? ben?.Departman ?? "").Trim(); i.Aciklama = (g.Aciklama ?? "").Trim();
                if (f != null) { i.KaynakFaturaId = f.Id; i.KaynakFaturaNo = f.FaturaNo; i.KaynakTedarikci = f.Tedarikci; i.KaynakFaturaTarihi = f.Tarih; }
                i.Kalemler = (g.Kalemler ?? new List<YnKalemGiris>()).Select(k => KalemKur(k, g.Rol, i.Id, s)).Where(k => k != null).GroupBy(k => k.KaynakFaturaId + "/" + k.SatirNo).Select(x => x.First()).ToList();   // aynı kaynak satır bir kez
                int no = 1; foreach (var k in i.Kalemler) { k.Id = i.Id * 100 + no++; k.EslemeId = EslemeBul(i.Yansitilan, i.Tur, k.Kategori, i.Tarih)?.Id ?? 0; }
                if (!s.Gecerli) return (i, s);
                // audit: alan bazında eski / yeni
                if (!yeni)
                {
                    if (eskiHedef != i.Yansitilan) Log(i, z, kullanici, "TARGET_COMPANY", eskiHedef, i.Yansitilan, "Ekran", g.Gerekce);
                    foreach (var k in i.Kalemler)
                    {
                        if (!eskiKalemler.TryGetValue(k.KaynakFaturaId + "/" + k.SatirNo, out var e)) { Log(i, z, kullanici, "LINE_ADDED", "", $"satır {k.SatirNo} · {P(k.YansitilacakMiktar)} {k.Birim}"); continue; }
                        if (e.YansitilacakMiktar != k.YansitilacakMiktar) Log(i, z, kullanici, $"QTY[{k.SatirNo}]", P(e.YansitilacakMiktar), P(k.YansitilacakMiktar), "Ekran", g.Gerekce);
                        if (e.KarPct != k.KarPct) Log(i, z, kullanici, $"PROFIT_PCT[{k.SatirNo}]", P(e.KarPct), P(k.KarPct), "Ekran", g.Gerekce);
                        if (e.BirimYansitma != k.BirimYansitma) Log(i, z, kullanici, $"UNIT_PRICE[{k.SatirNo}]", P(e.BirimYansitma), P(k.BirimYansitma), k.ManuelFiyat ? "Ekran (manuel)" : "Ekran", k.Gerekce);
                        if (e.KdvPct != k.KdvPct) Log(i, z, kullanici, $"VAT_PCT[{k.SatirNo}]", P(e.KdvPct), P(k.KdvPct), "Ekran", g.Gerekce);
                        if ((e.MasrafMerkezi ?? "") != (k.MasrafMerkezi ?? "")) Log(i, z, kullanici, $"COST_CENTER[{k.SatirNo}]", e.MasrafMerkezi, k.MasrafMerkezi);
                        if ((e.Proje ?? "") != (k.Proje ?? "")) Log(i, z, kullanici, $"PROJECT[{k.SatirNo}]", e.Proje, k.Proje);
                    }
                    foreach (var e in eskiKalemler.Values.Where(e => !i.Kalemler.Any(k => k.KaynakFaturaId == e.KaynakFaturaId && k.SatirNo == e.SatirNo))) Log(i, z, kullanici, "LINE_REMOVED", $"satır {e.SatirNo} · {P(e.YansitilacakMiktar)} {e.Birim}", "");
                }
                else { Log(i, z, kullanici, "STATUS", "", "Taslak", "Sistem"); foreach (var k in i.Kalemler.Where(k => k.ManuelFiyat)) Log(i, z, kullanici, $"UNIT_PRICE[{k.SatirNo}]", P(k.BirimMaliyet + k.BirimKar), P(k.BirimYansitma), "Ekran (manuel)", k.Gerekce); }
                if (i.Durum == "Reddedildi") { i.Durum = "Taslak"; i.Versiyon++; i.Onaylar.Clear(); i.RedNedeni = null; Log(i, z, kullanici, "STATUS", "Reddedildi", "Taslak (revize, v" + i.Versiyon + ")"); }
                i.Guncelleyen = kullanici; i.GuncellemeZamani = z;
                var d = Dogrula(i, g.Rol, true); s.Uyarilar.AddRange(d.Uyarilar); s.Hatalar.AddRange(d.Hatalar.Where(h => h.StartsWith("V01") || h.StartsWith("V04") || h.StartsWith("V05") || h.StartsWith("V14")));
                if (!s.Gecerli) { if (yeni) return (null, s); return (i, s); }
                if (yeni) _islemler.Insert(0, i);
                return (i, s);
            }
        }

        /// <summary>Onaya Gönder: tüm validasyonlar + simülasyon snapshot'ı + parametrik onay akışı.</summary>
        public static YnSonuc OnayaGonder(YnIslem i, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var s = Dogrula(i, rol, false);
                if (i.Durum != "Taslak") s.Hatalar.Insert(0, $"Yalnız Taslak durumundaki işlem onaya gönderilebilir (şu an: {i.Durum}).");
                foreach (var k in i.Kalemler) { k.OncedenYansitilan = OncedenYansitilan(k.KaynakFaturaId, k.SatirNo, i.Id); if (k.YansitilacakMiktar > k.KalanMiktar) s.Hatalar.Add($"V15 Satır {k.SatirNo}: eşzamanlı işlem sonrası kalan miktar {N(k.KalanMiktar, 3)} {k.Birim}; talep {N(k.YansitilacakMiktar, 3)}."); }
                var (sim, ss) = Simule(i); s.Hatalar.AddRange(ss.Hatalar); s.Uyarilar.AddRange(ss.Uyarilar);
                if (!s.Gecerli) return s;
                i.Simulasyon = sim; i.SimulasyonTarihi = z; i.Onaylar = OnayAkisi(i); i.Durum = "Onay Bekliyor"; i.Guncelleyen = kullanici; i.GuncellemeZamani = z;
                Log(i, z, kullanici, "STATUS", "Taslak", "Onay Bekliyor", "Workflow", OnayNedeni(i)); Log(i, z, kullanici, "SIM_SNAPSHOT", "", $"{sim.Count} satır · {i.Yansitan} + {string.Join(", ", i.HedefSirketler)}", "Sistem");
                return s;
            }
        }

        public static string Onayla(YnIslem i, string eylem, string not, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now;
                if (!Yetkili(rol, "onay")) return "Bu rol onay veremez.";
                if (i.Durum != "Onay Bekliyor") return $"İşlem onay sürecinde değil ({i.Durum}).";
                var adim = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor"); if (adim == null) return "Bekleyen onay adımı yok.";
                if (eylem == "Reddet")
                {
                    if (string.IsNullOrWhiteSpace(not)) return "Ret nedeni zorunludur.";
                    adim.Durum = "Reddedildi"; adim.Tarih = z; adim.Not = not; i.Durum = "Reddedildi"; i.RedNedeni = not;
                    Log(i, z, kullanici, "STATUS", "Onay Bekliyor", "Reddedildi", "Workflow", not); return null;
                }
                adim.Durum = "Onaylandı"; adim.Tarih = z; adim.Not = not ?? "";
                Log(i, z, kullanici, "APPROVAL", adim.Adim, "Onaylandı", "Workflow", not);
                if (i.Onaylar.All(o => o.Durum == "Onaylandı")) { i.Durum = "Onaylandı"; Log(i, z, kullanici, "STATUS", "Onay Bekliyor", "Onaylandı", "Workflow"); }
                i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }

        /// <summary>Onay bekleyen kaydı geri çek (yetkiye göre): taslağa döner, onay akışı sıfırlanır (V16).</summary>
        public static string GeriCek(YnIslem i, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now;
                if (i.Durum != "Onay Bekliyor") return "Yalnız onay bekleyen işlem geri çekilebilir.";
                if (i.Onaylar.Any(o => o.Durum == "Onaylandı") && !Yetkili(rol, "onay")) return "Kısmen onaylanmış işlemi yalnız onaycı geri çekebilir.";
                i.Durum = "Taslak"; i.Onaylar.Clear(); i.Versiyon++; Log(i, z, kullanici, "STATUS", "Onay Bekliyor", $"Taslak (geri çekildi, v{i.Versiyon})", "Workflow"); return null;
            }
        }

        /// <summary>ERP / SAP yansıtma faturası oluştur: idempotent (aynı işlem için ikinci çağrı mükerrer belge üretmez, V18).</summary>
        public static (string Hata, string Mesaj) FaturaOlustur(YnIslem i, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now;
                if (!Yetkili(rol, "faturaOlustur")) return ("Bu rol ERP belgesi oluşturamaz.", null);
                if (!string.IsNullOrEmpty(i.YansitmaFaturaNo)) return (null, $"Bu işlem için ERP belgesi zaten var ({i.YansitmaFaturaNo}); idempotency anahtarı {i.Entegrasyon.IdempotencyKey} ile ikinci belge üretilmedi.");
                if (i.Durum != "Onaylandı") return ($"Yalnız Onaylandı durumundaki işlem için fatura oluşturulabilir ({i.Durum}).", null);
                if (i.Simulasyon.Count == 0 || i.Kalemler.Any(k => EslemeBul(i.Yansitilan, i.Tur, k.Kategori, i.Tarih) == null)) return ("V12 Hesap eşlemesi eksik; fatura oluşturulamaz.", null);
                i.Entegrasyon = new YnEntegrasyon { Durum = "GONDERILIYOR", IdempotencyKey = $"YNS-{i.Id}-v{i.Versiyon}", Istek = $"POST /b1s/v1/Invoices {{CardCode:\"{AlacakCari(i.Yansitilan)}\", NumAtCard:\"{i.No}\", DocDate:\"{i.Tarih:yyyy-MM-dd}\", Lines:{i.Kalemler.Count}}}", Zaman = z };
                i.YansitmaFaturaNo = $"URS{z:yyyy}{(200000000 + i.Id * 917):000000000}"; i.ErpDocEntry = (18000 + i.Id * 3).ToString();
                i.Entegrasyon.Durum = "BASARILI"; i.Entegrasyon.Cevap = $"200 OK · DocEntry {i.ErpDocEntry} · DocNum {i.YansitmaFaturaNo} · {z:dd.MM.yyyy HH:mm}";
                i.Durum = "Fatura Oluştu"; i.Guncelleyen = kullanici; i.GuncellemeZamani = z;
                Log(i, z, kullanici, "STATUS", "Onaylandı", "Fatura Oluştu", "ERP"); Log(i, z, kullanici, "INVOICE_NO", "", i.YansitmaFaturaNo, "ERP");
                var f = AlimOnayOrnek.Bul(i.KaynakFaturaId); if (f != null && f.KullanimSirketi == i.Yansitilan) f.YansitmaNo = i.No;   // Aylık Alım Onay ekranındaki "Yansıtıldı" rozeti
                return (null, $"ERP yansıtma faturası oluşturuldu: {i.YansitmaFaturaNo} (DocEntry {i.ErpDocEntry}).");
            }
        }

        public static string Iptal(YnIslem i, string neden, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now;
                if (string.IsNullOrWhiteSpace(neden)) return "İptal nedeni zorunludur.";
                if (i.Durum == "İptal") return "İşlem zaten iptal.";
                if (!string.IsNullOrEmpty(i.YansitmaFaturaNo)) return "Oluşmuş ERP belgesi var: doğrudan iptal edilemez; ters kayıt (iade faturası) süreci başlatılmalıdır.";
                Log(i, z, kullanici, "STATUS", i.Durum, "İptal", "Ekran", neden); i.Durum = "İptal"; i.IptalNedeni = neden; i.Guncelleyen = kullanici; i.GuncellemeZamani = z; return null;
            }
        }

        // =====================================================================
        // GERÇEK MUHASEBE FİŞİ (ERP'den okuma simülasyonu) ve KONTROL MOTORU (§7)
        // =====================================================================
        /// <summary>ERP'den gerçek yevmiye satırlarını çeker (örnek: snapshot'tan üretir; senaryo ile sapma eklenir). Fatura oluşmuş işlemde çalışır.</summary>
        public static string FisAl(YnIslem i, string senaryo, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now;
                if (string.IsNullOrEmpty(i.YansitmaFaturaNo)) return "Önce ERP yansıtma faturası oluşmalı.";
                senaryo = senaryo ?? "uyumlu";
                if (senaryo == "yok") { i.Gercek = new List<YnFisSatir>(); i.GercekTarihi = z; Kontrol(i, kullanici, z); return null; }
                int n = 0; var fis = new Dictionary<string, string>();
                string FisNo(string sirket) { if (!fis.ContainsKey(sirket)) fis[sirket] = $"MF-{Kisa(sirket)}-{(51000 + i.Id * 7 + fis.Count):00000}"; return fis[sirket]; }
                var l = i.Simulasyon.Select(x => new YnFisSatir { Sirket = x.Sirket, FisNo = FisNo(x.Sirket), HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Borc = x.Borc, Alacak = x.Alacak, ParaBirimi = x.ParaBirimi, Kur = x.Kur, DovizTutar = x.DovizTutar, CariKod = x.CariKod, MasrafMerkezi = x.MasrafMerkezi, Proje = x.Proje, KalemRef = x.KalemRef, Aciklama = x.Aciklama, Tip = x.Tip, Referans = x.Referans }).ToList();
                var hedef = i.HedefSirketler.First();
                YnFisSatir Ilk(string tip, string sirket = null) => l.FirstOrDefault(x => x.Tip == tip && (sirket == null || x.Sirket == sirket));
                switch (senaryo)
                {
                    case "hesap": { var g = Ilk("GIDER", hedef); if (g != null) { g.HesapKodu = g.HesapKodu.StartsWith("770") ? "760.01.001" : "770.01.001"; g.HesapAdi = g.HesapKodu.StartsWith("760") ? "Pazarlama Satış Dağıtım Gideri" : "Genel Yönetim Gideri"; } break; }
                    case "kdv": { var k = Ilk("KDV", hedef); var c = Ilk("CARI", hedef); if (k != null && c != null) { var fark = Math.Round(k.Borc * 0.5m, 2); k.Borc -= fark; k.HesapAdi = "İndirilecek KDV %10"; k.HesapKodu = IndirilecekKdv(10); c.Alacak -= fark; } break; }
                    case "cari": { var c = Ilk("CARI", hedef); if (c != null) { var yanlis = Sirketler.First(s => s != i.Yansitan && s != hedef); c.CariKod = BorcCari(yanlis); c.HesapKodu = BorcCari(yanlis); c.HesapAdi = $"Satıcılar — {yanlis} (grup)"; } break; }
                    case "masraf": { foreach (var g in l.Where(x => x.Tip == "GIDER" && x.Sirket == hedef)) g.MasrafMerkezi = g.MasrafMerkezi == "CC40 - Üretim" ? "CC41 - Bakım" : "CC40 - Üretim"; break; }
                    case "tutar": { var g = Ilk("GIDER", hedef); var c = Ilk("CARI", hedef); if (g != null && c != null) { g.Borc = Math.Round(g.Borc * 1.02m, 2); c.Alacak += Math.Round(g.Borc / 1.02m * 0.02m, 2); } break; }
                    case "referans": { foreach (var x in l.Where(x => x.Sirket == hedef)) x.Referans = i.YansitmaFaturaNo; break; }
                    case "mukerrer": { var ek = l.Where(x => x.Sirket == hedef).Select(x => new YnFisSatir { Sirket = x.Sirket, FisNo = x.FisNo.Replace("MF-", "MF2-"), HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Borc = x.Borc, Alacak = x.Alacak, ParaBirimi = x.ParaBirimi, Kur = x.Kur, DovizTutar = x.DovizTutar, CariKod = x.CariKod, MasrafMerkezi = x.MasrafMerkezi, Proje = x.Proje, KalemRef = x.KalemRef, Aciklama = x.Aciklama, Tip = x.Tip, Referans = x.Referans }).ToList(); l.AddRange(ek); break; }
                    case "kur": { foreach (var x in l.Where(x => x.Sirket == hedef)) { x.Kur = Math.Round(x.Kur * 1.003m, 4); x.Borc = Math.Round(x.DovizTutar * x.Kur, 2) * (x.Borc > 0 ? 1 : 0); x.Alacak = Math.Round(x.DovizTutar * x.Kur, 2) * (x.Alacak > 0 ? 1 : 0); } break; }
                }
                i.Gercek = l; i.GercekTarihi = z; n = l.Select(x => x.FisNo).Distinct().Count();
                if (i.Durum == "Fatura Oluştu") { i.Durum = "Muhasebeleşti"; Log(i, z, kullanici, "STATUS", "Fatura Oluştu", "Muhasebeleşti", "ERP"); }
                Log(i, z, kullanici, "JOURNAL", "", string.Join(", ", l.Select(x => x.FisNo).Distinct()), "ERP");
                Kontrol(i, kullanici, z);
                return null;
            }
        }

        /// <summary>Beklenen (snapshot) ile gerçekleşen kayıtları alan alan karşılaştırır (§7.3); durumu §7.4 önceliğine göre belirler.</summary>
        public static string Kontrol(YnIslem i, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var K = new List<YnKontrol>();
                if (string.IsNullOrEmpty(i.YansitmaFaturaNo)) return "Yansıtma faturası oluşmadan muhasebe kontrolü yapılamaz.";
                void Ek(string alan, string sirket, string b, string g, string sonuc, bool kritik = true) => K.Add(new YnKontrol { Alan = alan, Sirket = sirket, Beklenen = b, Gerceklesen = g, Sonuc = sonuc, Kritik = kritik });
                string T(decimal d) => d.ToString("N2", Tr);
                var durumlar = new List<string>();
                foreach (var sirket in i.Simulasyon.Select(x => x.Sirket).Distinct())
                {
                    var b = i.Simulasyon.Where(x => x.Sirket == sirket).ToList(); var g = i.Gercek.Where(x => x.Sirket == sirket).ToList();
                    if (g.Count == 0) { Ek("Muhasebe Fişi", sirket, $"{b.Count} satır bekleniyor", "fiş yok", "MUHASEBE KAYDI YOK"); durumlar.Add("MUHASEBE KAYDI YOK"); continue; }
                    var fisler = g.Select(x => x.FisNo).Distinct().ToList();
                    if (fisler.Count > 1) { Ek("Mükerrer Fiş", sirket, "1 aktif fiş", string.Join(", ", fisler), "MÜKERRER KAYIT"); durumlar.Add("MÜKERRER KAYIT"); }
                    else Ek("Muhasebe Fişi", sirket, "1 aktif fiş", fisler[0], "UYUMLU");
                    decimal gb = g.Sum(x => x.Borc), ga = g.Sum(x => x.Alacak);
                    Ek("Borç = Alacak", sirket, T(b.Sum(x => x.Borc)) + " / " + T(b.Sum(x => x.Alacak)), T(gb) + " / " + T(ga), Math.Abs(gb - ga) <= Parametreler["tutarTolerans"] ? "UYUMLU" : "TUTAR FARKI");
                    if (Math.Abs(gb - ga) > Parametreler["tutarTolerans"]) durumlar.Add("TUTAR FARKI");
                    decimal bn = b.Where(x => x.Tip == "GELIR" || x.Tip == "GIDER").Sum(x => x.Borc + x.Alacak), gn = g.Where(x => x.Tip == "GELIR" || x.Tip == "GIDER").Sum(x => x.Borc + x.Alacak) / Math.Max(1, fisler.Count);
                    bool netOk = Math.Abs(bn - gn) <= Parametreler["tutarTolerans"]; Ek("Net Tutar", sirket, T(bn), T(gn), netOk ? "UYUMLU" : "TUTAR FARKI"); if (!netOk) durumlar.Add("TUTAR FARKI");
                    decimal bk = b.Where(x => x.Tip == "KDV").Sum(x => x.Borc + x.Alacak), gk = g.Where(x => x.Tip == "KDV").Sum(x => x.Borc + x.Alacak) / Math.Max(1, fisler.Count);
                    var bkh = string.Join(", ", b.Where(x => x.Tip == "KDV").Select(x => x.HesapKodu).Distinct()); var gkh = string.Join(", ", g.Where(x => x.Tip == "KDV").Select(x => x.HesapKodu).Distinct());
                    bool kdvOk = Math.Abs(bk - gk) <= Parametreler["tutarTolerans"] && bkh == gkh; Ek("KDV Tutarı ve Hesabı", sirket, $"{T(bk)} · {bkh}", $"{T(gk)} · {gkh}", kdvOk ? "UYUMLU" : "KDV FARKI"); if (!kdvOk) durumlar.Add("KDV FARKI");
                    foreach (var x in b.Where(x => x.Tip == "GELIR" || x.Tip == "GIDER"))
                    {
                        var y = g.FirstOrDefault(q => q.Tip == x.Tip && q.KalemRef == x.KalemRef);
                        string gh = y?.HesapKodu ?? "—"; bool ok = y != null && y.HesapKodu == x.HesapKodu;
                        Ek($"Hesap Kodu · {x.KalemRef}", sirket, x.HesapKodu, gh, ok ? "UYUMLU" : "HESAP HATASI"); if (!ok) durumlar.Add("HESAP HATASI");
                        if (!string.IsNullOrEmpty(x.MasrafMerkezi) || !string.IsNullOrEmpty(y?.MasrafMerkezi)) { bool mmOk = y != null && (y.MasrafMerkezi ?? "") == (x.MasrafMerkezi ?? ""); Ek($"Masraf Merkezi · {x.KalemRef}", sirket, x.MasrafMerkezi ?? "—", y?.MasrafMerkezi ?? "—", mmOk ? "UYUMLU" : "MASRAF MERKEZİ HATASI", false); if (!mmOk) durumlar.Add("MASRAF MERKEZİ HATASI"); }
                        if (!string.IsNullOrEmpty(x.Proje) || !string.IsNullOrEmpty(y?.Proje)) { bool pOk = y != null && (y.Proje ?? "") == (x.Proje ?? ""); Ek($"Proje · {x.KalemRef}", sirket, x.Proje ?? "—", y?.Proje ?? "—", pOk ? "UYUMLU" : "PROJE HATASI", false); if (!pOk) durumlar.Add("PROJE HATASI"); }
                    }
                    var bc = b.FirstOrDefault(x => x.Tip == "CARI"); var gc = g.FirstOrDefault(x => x.Tip == "CARI");
                    if (bc != null) { bool cOk = gc != null && gc.CariKod == bc.CariKod; Ek("Cari Hesap", sirket, bc.CariKod, gc?.CariKod ?? "—", cOk ? "UYUMLU" : "CARİ HESAP HATASI"); if (!cOk) durumlar.Add("CARİ HESAP HATASI"); }
                    var bpb = b.First().ParaBirimi; var gpb = g.First().ParaBirimi; Ek("Para Birimi", sirket, bpb, gpb, bpb == gpb ? "UYUMLU" : "KONTROL GEREKLİ", false); if (bpb != gpb) durumlar.Add("KONTROL GEREKLİ");
                    if (bpb != "TRY") { decimal bkur = b.First().Kur, gkur = g.First().Kur; bool kurOk = bkur > 0 && Math.Abs(gkur - bkur) / bkur * 100m <= Parametreler["kurTolerans"]; Ek("Kur", sirket, bkur.ToString("N4", Tr), gkur.ToString("N4", Tr), kurOk ? "UYUMLU" : "KONTROL GEREKLİ", false); if (!kurOk) durumlar.Add("KONTROL GEREKLİ"); }
                    bool refOk = g.All(x => (x.Referans ?? "").Contains(i.No)); Ek("Belge Referansı", sirket, i.No + " · " + i.KaynakFaturaNo, g.Select(x => x.Referans).Distinct().FirstOrDefault() ?? "—", refOk ? "UYUMLU" : "KONTROL GEREKLİ", false); if (!refOk) durumlar.Add("KONTROL GEREKLİ");
                }
                // §7.5 şirketler arası karşı kayıt: yansıtan 120 alacağı = yansıtılan 320 borcu (aynı belge ilişkisi)
                foreach (var hedef in i.HedefSirketler)
                {
                    var a = i.Gercek.FirstOrDefault(x => x.Sirket == i.Yansitan && x.Tip == "CARI" && x.CariKod == AlacakCari(hedef)); var bo = i.Gercek.FirstOrDefault(x => x.Sirket == hedef && x.Tip == "CARI");
                    if (a == null || bo == null) { if (i.Gercek.Count > 0) { Ek("Şirketler Arası Karşı Kayıt", $"{i.Yansitan} ⇄ {hedef}", "alacak = borç", a == null ? "yansıtan tarafı yok" : "yansıtılan tarafı yok", "MUHASEBE KAYDI YOK"); durumlar.Add("MUHASEBE KAYDI YOK"); } continue; }
                    bool ok = Math.Abs(a.Borc - bo.Alacak) <= Parametreler["tutarTolerans"]; Ek("Şirketler Arası Karşı Kayıt", $"{i.Yansitan} ⇄ {hedef}", $"{T(a.Borc)} alacak ({a.CariKod})", $"{T(bo.Alacak)} borç ({bo.CariKod})", ok ? "UYUMLU" : "TUTAR FARKI"); if (!ok) durumlar.Add("TUTAR FARKI");
                }
                string eskiDurum = i.KontrolDurumu;
                i.KontrolDurumu = KontrolOnceligi.FirstOrDefault(d => durumlar.Contains(d)) ?? "UYUMLU";   // öncelik: kritikten hafife (§7.4)
                i.Kontroller = K; i.KontrolTarihi = z;
                if (i.KontrolDurumu == "UYUMLU" && i.Durum == "Muhasebeleşti") { i.Durum = "Uyumlu"; Log(i, z, kullanici, "STATUS", "Muhasebeleşti", "Uyumlu", "Kontrol"); }
                else if (i.KontrolDurumu != "UYUMLU" && i.Durum == "Uyumlu") { i.Durum = "Muhasebeleşti"; Log(i, z, kullanici, "STATUS", "Uyumlu", "Muhasebeleşti", "Kontrol"); }
                if (eskiDurum != i.KontrolDurumu) Log(i, z, kullanici, "CONTROL_STATUS", eskiDurum, i.KontrolDurumu, "Kontrol");
                return null;
            }
        }

        // =====================================================================
        // ÖRNEK KAYITLAR — her durumdan bir işlem; muhasebe kontrolü senaryoları
        // =====================================================================
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var bugun = DateTime.Today; var F = AlimOnayOrnek.Faturalar;
                var ben = NakitAvansOrnek.Personel.First(p => p.Rol == "Muhasebe"); string ku = ben.AdSoyad, muh = "Nazlı Erdem", fin = NakitAvansOrnek.Finans().AdSoyad;
                AlimFatura Fat(string sirket, string ted, int nth = 0, string kullanim = null) => F.Where(f => f.FaturaSirketi == sirket && f.Tedarikci.StartsWith(ted) && f.OnayDurumu != "Reddedildi" && (kullanim == null || f.KullanimSirketi == kullanim)).OrderBy(f => f.Tarih).ThenBy(f => f.Id).Skip(nth).FirstOrDefault() ?? F.First(f => f.FaturaSirketi == sirket);
                YnIslem Yap(int gunOnce, string yansitan, string yansitilan, string tur, AlimFatura f, (int satir, decimal oran, decimal kar)[] satirlar, string aciklama, string mm = null, string proje = null, List<YnDagitimGiris> dagitim = null, decimal? manuel = null)
                {
                    var z = bugun.AddDays(-gunOnce).AddHours(9).AddMinutes(20 + gunOnce % 30);
                    var g = new YnIslemGiris
                    {
                        Rol = "Sistem Yöneticisi", Tarih = z.ToString("yyyy-MM-dd"), Yansitan = yansitan, Yansitilan = yansitilan, Tur = tur, KaynakBelgeTuru = "Alış Faturası", MasrafMerkezi = mm ?? "", Proje = proje ?? "", Departman = "Muhasebe", Aciklama = aciklama, KaynakFaturaId = f.Id,
                        Kalemler = satirlar.Select(s => { var k = f.Kalemler.First(x => x.Sira == s.satir); var kg = new YnKalemGiris { KaynakFaturaId = f.Id, SatirNo = s.satir, YansitilacakMiktar = Math.Round(k.Miktar * s.oran, 3), KarPct = s.kar, MasrafMerkezi = mm ?? f.MasrafMerkezi }; if (dagitim != null) kg.Dagitim = dagitim.Select(d => new YnDagitimGiris { Sirket = d.Sirket, Miktar = Math.Round(k.Miktar * d.Miktar, 3), KarPct = d.KarPct }).ToList(); if (manuel.HasValue) { kg.ManuelFiyat = true; kg.BirimYansitma = manuel; kg.Gerekce = "Grup içi anlaşmalı fiyat"; } return kg; }).ToList()
                    };
                    var (i, s) = Kaydet(g, ku, ben, z);
                    if (i == null) throw new InvalidOperationException("Tohum kaydı oluşturulamadı: " + string.Join(" | ", s.Hatalar));
                    return i;
                }
                DateTime Z(YnIslem i, int saatSonra) => i.OlusturmaZamani.AddHours(saatSonra);
                void Gonder(YnIslem i, int saat = 2) { var s = OnayaGonder(i, "Sistem Yöneticisi", ku, Z(i, saat)); if (!s.Gecerli) throw new InvalidOperationException(i.No + ": " + string.Join(" | ", s.Hatalar)); }
                void OnaylaHepsi(YnIslem i, int saat = 20) { int n = 0; while (i.Durum == "Onay Bekliyor") { var a = i.Onaylar.First(o => o.Durum == "Bekliyor"); Onayla(i, "Onayla", n == 0 ? "Kaynak fatura ve kalemler kontrol edildi." : "Uygundur.", "Yönetici / Onaycı", a.Kisi, Z(i, saat + n * 6)); n++; } }
                void Fatura(YnIslem i, int saat = 40) { var (h, m) = FaturaOlustur(i, "Muhasebe", muh, Z(i, saat)); if (h != null) throw new InvalidOperationException(i.No + ": " + h); }
                void Fis(YnIslem i, string senaryo, int saat = 52) { var h = FisAl(i, senaryo, "SAP B1 Entegrasyon", Z(i, saat)); if (h != null) throw new InvalidOperationException(i.No + ": " + h); }

                // 0 Şartname örneği: Hammadde A faturası (1.000 KG × 500 TL, KDV %20; AlimOnayOrnek URS2026100099001)
                //   a) Uras Kimya → Selvi 300 KG (§4.7 "daha önce yansıtılan") → Uyumlu
                //   b) Uras Kimya → Alv Kimya 100 KG, kâr %10 → birim 550, net 55.000, KDV 11.000, genel toplam 66.000 TL (§18 uçtan uca senaryo) → Uyumlu; kalan 600 KG
                var fa = F.First(f => f.FaturaNo == "URS2026100099001");
                var i0a = Yap(28, "Uras Kimya", "Selvi", "Stok", fa, new[] { (1, 0.3m, 10m) }, "Hammadde A: Selvi üretim hattına 300 KG devir (kâr %10).");
                Gonder(i0a); OnaylaHepsi(i0a); Fatura(i0a); Fis(i0a, "uyumlu");
                var i0b = Yap(24, "Uras Kimya", "Alv Kimya", "Stok", fa, new[] { (1, 0.1m, 10m) }, "Hammadde A: Alv Kimya'ya 100 KG × 500 TL maliyet, kâr %10 → birim yansıtma 550 TL, net 55.000 TL, KDV 11.000 TL, genel toplam 66.000 TL.");
                Gonder(i0b); OnaylaHepsi(i0b); Fatura(i0b); Fis(i0b, "uyumlu");
                // 1 Uras Holding → Uras Kimya · Masraf (IT lisans, tamamı, kâr 0) → Uyumlu; Alım Onay ekranındaki "Yansıtıldı" rozeti bu no'yu gösterir
                var i1 = Yap(26, "Uras Holding", "Uras Kimya", "Masraf", Fat("Uras Holding", "Microsoft", 0, "Uras Kimya"), new[] { (1, 1m, 0m) }, "Microsoft 365 lisanslarının kullanıcı firma Uras Kimya'ya yansıtılması (kâr eklenmeden).", "CC10 - Bilgi Teknolojileri");
                Gonder(i1); OnaylaHepsi(i1); Fatura(i1); Fis(i1, "uyumlu");
                // 2 Uras Kimya → Alv Kimya · Stok (hammadde, %40, kâr 10) → Uyumlu
                var f2 = Fat("Uras Kimya", "Petkim");
                var i2 = Yap(22, "Uras Kimya", "Alv Kimya", "Stok", f2, new[] { (1, 0.4m, 10m) }, "Alv Kimya üretimi için LDPE granül devri; grup içi kâr %10.");
                Gonder(i2); OnaylaHepsi(i2); Fatura(i2); Fis(i2, "uyumlu");
                // 3 Uras Kimya → Selvi · Stok (PET cips, %30, kâr 8) → Muhasebeleşti · HESAP HATASI
                var i3 = Yap(19, "Uras Kimya", "Selvi", "Stok", Fat("Uras Kimya", "Sasa"), new[] { (1, 0.3m, 8m) }, "Selvi'nin dönem üretimi için PET cips devri.");
                Gonder(i3); OnaylaHepsi(i3); Fatura(i3); Fis(i3, "hesap");
                // 4 Uras Kimya → Uras Holding · Hizmet (denetim, tamamı, kâr 5) → Muhasebeleşti · KONTROL GEREKLİ (fişte yansıtma no referansı eksik)
                var i4 = Yap(17, "Uras Kimya", "Uras Holding", "Hizmet", Fat("Uras Kimya", "Bureau"), new[] { (1, 1m, 5m) }, "ISO 9001 gözetim denetimi holding ortak sertifikasyonu; hizmet bedeli yansıtması (transfer fiyatlandırması dosyası ekli).", "CC43 - Kalite");
                Gonder(i4); OnaylaHepsi(i4); Fatura(i4); Fis(i4, "referans");
                // 5 Uras Kimya → Avrupa Paper · Fatura (NaOH, %50, kâr 12) → Fatura Oluştu · MUHASEBE KAYDI YOK (fiş bekleniyor)
                var i5 = Yap(12, "Uras Kimya", "Avrupa Paper", "Fatura", Fat("Uras Kimya", "Brenntag", 1), new[] { (1, 0.5m, 12m) }, "Sodyum hidroksit çözeltisinin yarısı Avrupa Paper hamur hattına devredildi.");
                Gonder(i5); OnaylaHepsi(i5); Fatura(i5); Fis(i5, "yok");
                // 6 Avrupa Paper → Daf Kimya · Masraf (elektrik, %25, kâr 0) → Muhasebeleşti · KDV FARKI
                var i6 = Yap(14, "Avrupa Paper", "Daf Kimya", "Masraf", Fat("Avrupa Paper", "Enerjisa"), new[] { (1, 0.25m, 0m) }, "Ortak OSB sayacından Daf Kimya'nın elektrik tüketim payı (sayaç okuma tutanağı).", "CC40 - Üretim");
                Gonder(i6); OnaylaHepsi(i6); Fatura(i6); Fis(i6, "kdv");
                // 7 Uras Kimya → Uras Power · Stok, satır iki hedef şirkete dağıtıldı (Uras Power 2/3, Daf Kimya 1/3) → Onay Bekliyor (Direktör: kâr üst sınırı)
                var i7 = Yap(3, "Uras Kimya", "Uras Power", "Stok", Fat("Uras Kimya", "Petkim", 2), new[] { (1, 0.6m, 15m) }, "LDPE granül iki grup şirketine dağıtıldı; Uras Power için proje bazlı.", null, "PRJ-2026-07 Enerji Verimliliği", new List<YnDagitimGiris> { new YnDagitimGiris { Sirket = "Uras Power", Miktar = 0.4m, KarPct = 15m }, new YnDagitimGiris { Sirket = "Daf Kimya", Miktar = 0.2m, KarPct = 12m } });
                Gonder(i7); Onayla(i7, "Onayla", "Kaynak fatura ve dağıtım miktarları kontrol edildi.", "Muhasebe", muh, Z(i7, 5));
                // 8 Selvi → Alv Kimya · Fatura (Brenntag, kullanıcı firma Alv Kimya, tamamı, kâr 10) → Onaylandı (fatura oluşmamış)
                var i8 = Yap(6, "Selvi", "Alv Kimya", "Fatura", Fat("Selvi", "Brenntag", 0, "Alv Kimya"), new[] { (1, 1m, 10m) }, "Alv Kimya adına alınan hammadde faturasının tamamı yansıtılıyor (kullanıcı firma Alv Kimya).");
                Gonder(i8); OnaylaHepsi(i8);
                // 9 Uras Kimya → Uras Holding · Hizmet (nakliye, %30, kâr 20) → Reddedildi
                var i9 = Yap(9, "Uras Kimya", "Uras Holding", "Hizmet", Fat("Uras Kimya", "Ekol"), new[] { (1, 0.3m, 20m) }, "Holding sevkiyatları için nakliye payı.", "CC30 - Operasyon");
                Gonder(i9); Onayla(i9, "Onayla", "", "Muhasebe", muh, Z(i9, 4)); Onayla(i9, "Reddet", "Kâr oranı %20 grup transfer fiyatlandırması politikasının (%5 hizmet) üstünde; %5 ile revize edilmeli.", "Yönetici / Onaycı", NakitAvansOrnek.UstYonetim(ben).AdSoyad, Z(i9, 30));
                // 10 Uras Kimya → Selvi · Stok (i2 ile aynı kaynak satır, %20 daha; kalan %40) → Taslak
                var i10 = Yap(1, "Uras Kimya", "Selvi", "Stok", f2, new[] { (1, 0.2m, 10m) }, "Aynı LDPE partisinden Selvi'ye kısmi devir (kalan miktar üzerinden).");
                // 11 Avrupa Paper → Uras Kimya · Masraf (rulman, %50, kâr 5) → Muhasebeleşti · CARİ HESAP HATASI
                var i11 = Yap(15, "Avrupa Paper", "Uras Kimya", "Masraf", Fat("Avrupa Paper", "SKF"), new[] { (1, 0.5m, 5m) }, "Ortak bakım stoğundan Uras Kimya hattına verilen rulmanlar.", "CC41 - Bakım");
                Gonder(i11); OnaylaHepsi(i11); Fatura(i11); Fis(i11, "cari");
                // 12 Selvi → Uras Kimya · Masraf (sarf, tamamı, kâr 0) → Muhasebeleşti · MÜKERRER KAYIT
                var i12 = Yap(11, "Selvi", "Uras Kimya", "Masraf", Fat("Selvi", "Ofix"), new[] { (1, 1m, 0m) }, "Ortak satın alınan kırtasiye / sarf malzemesinin Uras Kimya payı.", "CC05 - İdari İşler");
                Gonder(i12); OnaylaHepsi(i12); Fatura(i12); Fis(i12, "mukerrer");
                // 13 Uras Holding → Selvi · Hizmet (ERP bakım, EUR, %50, kâr 5) → Uyumlu (dövizli örnek)
                var i13 = Yap(20, "Uras Holding", "Selvi", "Hizmet", Fat("Uras Holding", "Logo"), new[] { (1, 0.5m, 5m) }, "ERP yıllık bakım bedelinin Selvi kullanıcı payı (EUR).", "CC10 - Bilgi Teknolojileri");
                Gonder(i13); OnaylaHepsi(i13); Fatura(i13); Fis(i13, "uyumlu");
                // 14 Avrupa Paper → Alv Kimya · Masraf (temizlik hizmeti, tamamı, manuel fiyat) → Muhasebeleşti · MASRAF MERKEZİ HATASI
                var f14 = Fat("Avrupa Paper", "Anadolu"); var k14 = f14.Kalemler[0];
                var i14 = Yap(13, "Avrupa Paper", "Alv Kimya", "Masraf", f14, new[] { (1, 1m, 0m) }, "Ortak tesis temizlik hizmetinin Alv Kimya payı; grup içi anlaşmalı fiyat.", "CC05 - İdari İşler", null, null, Math.Round(BirimMaliyet(k14, f14) * 1.06m, 2));
                Gonder(i14); OnaylaHepsi(i14); Fatura(i14); Fis(i14, "masraf");
                _islemler.Sort((a, b) => b.Id.CompareTo(a.Id));
            }
        }
    }
}
