// ============================================================
// CARİ KART VE FAALİYET EŞLEŞTİRME — Müşteri / Tedarikçi Cari Kartları ve Cari Kart Talep Formu (Müşteri / Tedarikçi)
// Kaynak: "Cari Kart ve Faaliyet Eşleştirme Yapısı — IT Uygulama Raporu, Geliştirilmiş v6 (13.08.2026)".
//   CARİ_MASTER   : kimlik, vergi, bölge / ülke / şehir, sınıflandırma (tip + bölge → sınıf matrisi), ilişki durumu, rol, eş cari kodu (çift rol), iletişim, vade, durum.
//   CARİ_FAALİYET : carinin çalıştığı faaliyet ana grubu → faaliyet grubu (bağımlı), faaliyet grubu bazında satış temsilcisi, ana faaliyet mi, ürün hiyerarşisi (sınıf > grup > seri > ad).
//   Seçim listeleri §14'teki nihai değerlerdir; validasyonlar §13. Kayıtlar bellek içindedir.
// Ekran: Controllers/CariKartController.cs → Views/CariKart/Kartlar.cshtml (liste / detay) ve Views/CariKart/Talep.cshtml (talep formu); betik wwwroot/js/cari-kart.js.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>CARİ_FAALİYET_EŞLEŞTİRME satırı.</summary>
    public class CariFaaliyet
    {
        public int Sira { get; set; } public string AnaGrup { get; set; } public string Grup { get; set; } public string SatisTemsilcisi { get; set; } public bool AnaFaaliyet { get; set; }
        public string UrunSinifi { get; set; } = ""; public string UrunGrubu { get; set; } = ""; public string UrunSerisi { get; set; } = ""; public string UrunAdi { get; set; } = ""; public string Durum { get; set; } = "Aktif"; public DateTime? BaslangicTarihi { get; set; } public string Not { get; set; } = "";
    }
    /// <summary>CARİ_MASTER kaydı.</summary>
    public class CariKart
    {
        public string Kod { get; set; } public string KisaAd { get; set; } public string KisaltmaliUnvan { get; set; } public string ResmiUnvan { get; set; } public string VergiNo { get; set; } public string VergiDairesi { get; set; }
        public string KartTipi { get; set; } public string Bolge { get; set; } public string Ulke { get; set; } public string Sehir { get; set; } public string Sinif { get; set; } public string IliskiDurumu { get; set; } public string Rol { get; set; } public string EsCariKodu { get; set; } = "";
        public string Yetkili { get; set; } public string Telefon { get; set; } public string Eposta { get; set; } public string VadeSarti { get; set; } public string Not { get; set; } = "";
        /// <summary>Elle pasife alma ("Pasif" | null). Otomatik kuralı ezer; kaldırılınca otomatik kural yeniden geçerli olur.</summary>
        public string ElleDurum { get; set; } public string ElleDurumKim { get; set; } public DateTime? ElleDurumZaman { get; set; }
        /// <summary>DURUM OTOMATİK: son 1 yıl (365 gün) içinde cari hareketi varsa Aktif, yoksa Pasif (kart açılışı hareket sayılır; kaynaklar CariKartOrnek.SonHareket). Elle pasife alınmışsa Pasif.</summary>
        public string Durum => ElleDurum == "Pasif" ? "Pasif" : (CariKartOrnek.SonHareket(this).Tarih >= DateTime.Today.AddDays(-365) ? "Aktif" : "Pasif");
        public string DurumKaynagi => ElleDurum == "Pasif" ? $"Elle pasif · {ElleDurumKim} {ElleDurumZaman:dd.MM.yyyy}" : (Durum == "Aktif" ? "Otomatik · son 1 yılda hareket var" : "Otomatik · 1 yıldır hareket yok");
        public string Sirket { get; set; } public DateTime OlusturmaTarihi { get; set; } public string Olusturan { get; set; } public string KaynakTalepNo { get; set; } = "";
        public List<CariFaaliyet> Faaliyetler { get; set; } = new List<CariFaaliyet>();
        public string AnaFaaliyet => Faaliyetler.FirstOrDefault(f => f.AnaFaaliyet)?.Grup ?? "";
        /// <summary>KREDİ LİMİTİ (müşteri kartı). Tür: Yok | Manuel (elle girilen TL tutar) | Dinamik (son N ayın aylık alım ortalaması × çarpan; satış faturalarından her seferinde yeniden hesaplanır). Hesap: CariKartOrnek.LimitHesapla.</summary>
        public string LimitTuru { get; set; } = "Yok"; public decimal ManuelLimit { get; set; } public int DinamikAy { get; set; } = 3; public decimal DinamikCarpan { get; set; } = 2m;
        public string LimitNot { get; set; } = ""; public string LimitKim { get; set; } = ""; public DateTime? LimitZaman { get; set; }
        public List<CariLimitLog> LimitGecmisi { get; set; } = new List<CariLimitLog>();
    }
    /// <summary>Kredi limiti değişiklik kaydı.</summary>
    public class CariLimitLog { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Eski { get; set; } public string Yeni { get; set; } public string Not { get; set; } = ""; }
    /// <summary>Kredi limiti hesabı (CariKartOrnek.LimitHesapla sonucu): limit, dinamik hesap ayrıntısı, cari risk (bakiye), kullanım ve durum.</summary>
    public class CariLimit
    {
        public string Tur { get; set; } = "Yok"; public decimal Limit { get; set; } public int Ay { get; set; } public decimal Carpan { get; set; }
        public decimal AlimToplam { get; set; } public decimal AylikOrtalama { get; set; } public List<(string Ay, decimal Tutar, int Adet)> Aylar { get; set; } = new List<(string, decimal, int)>();
        public decimal Bakiye { get; set; } public decimal Risk => Math.Max(Bakiye, 0); public string BakiyeKaynak { get; set; } = "";
        public decimal? KullanimYuzde => Limit > 0 ? Math.Round(Risk / Limit * 100, 1) : (decimal?)null;
        public decimal Kalan => Limit - Risk;
        /// <summary>Limit Yok | Limit İçinde | Limite Yakın (≥ %80) | Limit Aşıldı</summary>
        public string Durum => Tur == "Yok" ? "Limit Yok" : Limit <= 0 ? (Risk > 0 ? "Limit Aşıldı" : "Limit İçinde") : KullanimYuzde >= 100 ? "Limit Aşıldı" : KullanimYuzde >= 80 ? "Limite Yakın" : "Limit İçinde";
        public string Aciklama { get; set; } = "";
    }
    /// <summary>Cari Kart Talebi (Müşteri / Tedarikçi Talep Formu): onaylanınca CARİ_MASTER'a kart açılır.</summary>
    public class CariTalep
    {
        public int Id { get; set; } public string No { get; set; } public DateTime Tarih { get; set; } public string TalepTuru { get; set; } = "Yeni Kart"; public string TalepEden { get; set; } public string Departman { get; set; } public string Sirket { get; set; }
        public CariKart Kart { get; set; } = new CariKart(); public string Gerekce { get; set; } = ""; public string Durum { get; set; } = "Taslak"; public string OlusanKod { get; set; } = ""; public string RedNedeni { get; set; } = "";
        public List<YnOnay> Onaylar { get; set; } = new List<YnOnay>(); public List<YnAudit> Audit { get; set; } = new List<YnAudit>(); public DateTime GuncellemeZamani { get; set; }
        public bool Aktif => Durum != "Reddedildi" && Durum != "İptal";
    }
    public class CariFaaliyetGiris { public string AnaGrup { get; set; } public string Grup { get; set; } public string SatisTemsilcisi { get; set; } public bool AnaFaaliyet { get; set; } public string UrunSinifi { get; set; } public string UrunGrubu { get; set; } public string UrunSerisi { get; set; } public string UrunAdi { get; set; } public string Durum { get; set; } public string BaslangicTarihi { get; set; } public string Not { get; set; } }
    public class CariTalepGiris
    {
        public int Id { get; set; } public string Rol { get; set; } public string TalepTuru { get; set; } public string Sirket { get; set; } public string Gerekce { get; set; }
        public string KisaAd { get; set; } public string KisaltmaliUnvan { get; set; } public string ResmiUnvan { get; set; } public string VergiNo { get; set; } public string VergiDairesi { get; set; } public string KartTipi { get; set; } public string Bolge { get; set; } public string Ulke { get; set; } public string Sehir { get; set; } public string Sinif { get; set; } public string IliskiDurumu { get; set; } public string CariRolu { get; set; } public string EsCariKodu { get; set; }
        public string Yetkili { get; set; } public string Telefon { get; set; } public string Eposta { get; set; } public string VadeSarti { get; set; } public string Durum { get; set; } public string Not { get; set; }
        public List<CariFaaliyetGiris> Faaliyetler { get; set; }
    }

    public static class CariKartOrnek
    {
        public static readonly CultureInfo Tr = new CultureInfo("tr-TR"); public static readonly object Kilit = new object();
        // ---- §14 nihai seçim listeleri ----
        public static readonly string[] KartTipleri = { "Müşteri", "Tedarikçi", "Finansal Kuruluşlar", "Resmi Kurumlar", "Personel Carileri" };
        public static readonly string[] Bolgeler = { "Yurtiçi", "Yurtdışı" };
        public static readonly string[] Ulkeler = { "Türkiye", "Almanya", "İtalya", "İspanya", "Fransa", "Hollanda", "Belçika", "Polonya", "Romanya", "Bulgaristan", "Yunanistan", "Birleşik Krallık", "ABD", "Çin", "Hindistan", "Pakistan", "Bangladeş", "Mısır", "Fas", "Tunus", "Cezayir", "Irak", "İran", "Azerbaycan", "Özbekistan", "Rusya", "Ukrayna", "Güney Kore", "Japonya", "Birleşik Arap Emirlikleri" };
        public static readonly string[] TurkiyeIlleri = { "Adana", "Adıyaman", "Afyonkarahisar", "Ağrı", "Aksaray", "Amasya", "Ankara", "Antalya", "Ardahan", "Artvin", "Aydın", "Balıkesir", "Bartın", "Batman", "Bayburt", "Bilecik", "Bingöl", "Bitlis", "Bolu", "Burdur", "Bursa", "Çanakkale", "Çankırı", "Çorum", "Denizli", "Diyarbakır", "Düzce", "Edirne", "Elazığ", "Erzincan", "Erzurum", "Eskişehir", "Gaziantep", "Giresun", "Gümüşhane", "Hakkari", "Hatay", "Iğdır", "Isparta", "İstanbul", "İzmir", "Kahramanmaraş", "Karabük", "Karaman", "Kars", "Kastamonu", "Kayseri", "Kilis", "Kırıkkale", "Kırklareli", "Kırşehir", "Kocaeli", "Konya", "Kütahya", "Malatya", "Manisa", "Mardin", "Mersin", "Muğla", "Muş", "Nevşehir", "Niğde", "Ordu", "Osmaniye", "Rize", "Sakarya", "Samsun", "Siirt", "Sinop", "Sivas", "Şanlıurfa", "Şırnak", "Tekirdağ", "Tokat", "Trabzon", "Tunceli", "Uşak", "Van", "Yalova", "Yozgat", "Zonguldak" };
        public static readonly Dictionary<string, string[]> YurtdisiSehirler = new Dictionary<string, string[]> { ["Almanya"] = new[] { "Berlin", "Hamburg", "Münih", "Köln", "Frankfurt", "Düsseldorf", "Stuttgart" }, ["İtalya"] = new[] { "Milano", "Roma", "Torino", "Bologna", "Prato" }, ["İspanya"] = new[] { "Barselona", "Madrid", "Valensiya" }, ["Fransa"] = new[] { "Paris", "Lyon", "Marsilya" }, ["Hollanda"] = new[] { "Amsterdam", "Rotterdam" }, ["ABD"] = new[] { "New York", "Los Angeles", "Houston", "Chicago" }, ["Çin"] = new[] { "Şanghay", "Guangzhou", "Shenzhen", "Pekin" }, ["Hindistan"] = new[] { "Mumbai", "Surat", "Delhi" }, ["Mısır"] = new[] { "Kahire", "İskenderiye" }, ["Polonya"] = new[] { "Varşova", "Lodz" }, ["Birleşik Arap Emirlikleri"] = new[] { "Dubai", "Abu Dabi" } };
        /// <summary>§5 Cari Kart Tipi / Bölge → Cari Sınıfı matrisi ("*" = bölge opsiyonel).</summary>
        public static readonly Dictionary<string, string[]> SinifMatrisi = new Dictionary<string, string[]>
        {
            ["Müşteri|Yurtiçi"] = new[] { "Bayi", "Distribütör", "Ticari Müşteriler", "Özel Takip" },
            ["Müşteri|Yurtdışı"] = new[] { "Yurtdışı Şube", "Yurtdışı Bayi", "Yurtdışı Distribütör", "Ticari Müşteriler", "Özel Takip" },
            ["Tedarikçi|Yurtiçi"] = new[] { "Hammadde", "Yardımcı Malzemeler", "Fason", "Hizmetler", "Danışmanlık", "Özel Takip" },
            ["Tedarikçi|Yurtdışı"] = new[] { "Hammadde", "Yardımcı Malzemeler", "Fason", "Hizmetler", "Danışmanlık", "Özel Takip" },
            ["Finansal Kuruluşlar|*"] = new[] { "Banka", "Leasing", "Faktoring" },
            ["Resmi Kurumlar|*"] = new[] { "Vergi Dairesi", "SGK", "Gümrük / Harç" },
            ["Personel Carileri|*"] = new[] { "Personel", "Personel Avans", "Personel Masraf", "Emanet", "Özel Takip" }
        };
        public static string[] Siniflar(string tip, string bolge) => SinifMatrisi.TryGetValue(tip + "|" + bolge, out var l) ? l : SinifMatrisi.TryGetValue(tip + "|*", out var l2) ? l2 : new string[0];
        public static readonly string[] IliskiDurumlari = { "Grup İçi", "İlişkili Cari", "Grup Dışı" };
        public static readonly Dictionary<string, string> IliskiAciklama = new Dictionary<string, string> { ["Grup İçi"] = "Ortaklık / sermaye bağı bulunan grup şirketleri, şubeler, bağlı birimler.", ["İlişkili Cari"] = "Doğrudan grup bağı olmayan; yönetim bağlantısı ya da ticari ilişki nedeniyle takip amaçlı sınıflandırılan cariler.", ["Grup Dışı"] = "Grup ya da özel ilişki bağı bulunmayan standart cariler." };
        public static readonly string[] Roller = { "Sadece Müşteri", "Sadece Tedarikçi", "Çift Rol", "Diğer" };
        public static readonly string[] VadeSartlari = { "Peşin", "Vade 30 Gün", "Vade 45 Gün", "Vade 60 Gün", "Vade 90 Gün", "Vade 120 Gün", "Çek (Vadeli)", "Akreditif (L/C)", "Mal Mukabili", "Vesaik Mukabili" };
        public static readonly string[] Durumlar = { "Aktif", "Pasif" };
        /// <summary>§10 faaliyet hiyerarşisi: ana grup → gruplar.</summary>
        public static readonly Dictionary<string, string[]> FaaliyetHiyerarsisi = new Dictionary<string, string[]>
        {
            ["Boya Grubu"] = new[] { "Dijital", "Emprime" }, ["Kağıt Grubu"] = new[] { "Süblime Kağıt" }, ["Binder Grubu"] = new[] { "İnşaat ve Boya Binderleri", "Tekstil Binderleri", "Kağıt Kaplama Binderleri" },
            ["Yardımcı Kimyasallar"] = new[] { "İnşaat ve Boya Yardımcı Kimyasalları", "Tekstil Yardımcı Kimyasalları", "Kağıt Kaplama Yardımcı Kimyasalları" }, ["Baskı Makineleri"] = new[] { "Dijital", "Emprime" }
        };
        public static readonly Dictionary<string, string> AnaGrupAciklama = new Dictionary<string, string> { ["Boya Grubu"] = "Dijital veya emprime baskı boyaları ana ürün ailesi.", ["Kağıt Grubu"] = "Süblime kağıt gibi kağıt ürünleri ana ailesi.", ["Binder Grubu"] = "Tekstil, inşaat, boya veya kağıt kaplama binderleri.", ["Yardımcı Kimyasallar"] = "Üretimde kullanılan yardımcı kimyasallar.", ["Baskı Makineleri"] = "Dijital veya emprime baskı makineleri." };
        /// <summary>§11 / §15.3 ürün hiyerarşisi örnekleri: "AnaGrup|Grup" → ürün sınıfı → ürün grubu → seri → ad.</summary>
        public static readonly Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string[]>>>> UrunHiyerarsisi = new Dictionary<string, Dictionary<string, Dictionary<string, Dictionary<string, string[]>>>>
        {
            ["Boya Grubu|Emprime"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Su Bazlı"] = new Dictionary<string, Dictionary<string, string[]>> { ["Hybrid"] = new Dictionary<string, string[]> { ["Clear Bases"] = new[] { "S 10 CLEAR", "S 20 CLEAR", "S 30 SOFT CLEAR" }, ["White Bases"] = new[] { "W 10 WHITE", "W 20 SUPER WHITE" } }, ["Pigment"] = new Dictionary<string, string[]> { ["Color Concentrates"] = new[] { "PC 100 RED", "PC 200 BLUE", "PC 300 YELLOW" } } }, ["Plastisol"] = new Dictionary<string, Dictionary<string, string[]>> { ["Standart"] = new Dictionary<string, string[]> { ["Plasti White"] = new[] { "PW 01", "PW 02 SOFT" }, ["Plasti Color"] = new[] { "PC 01 BLACK", "PC 02 RED" } } } },
            ["Boya Grubu|Dijital"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["DTF"] = new Dictionary<string, Dictionary<string, string[]>> { ["DTF Ink"] = new Dictionary<string, string[]> { ["Film Series"] = new[] { "DTF INK CMYK", "DTF INK WHITE" } } }, ["DTG"] = new Dictionary<string, Dictionary<string, string[]>> { ["DTG Ink"] = new Dictionary<string, string[]> { ["Cotton Series"] = new[] { "DTG CMYK", "DTG WHITE" } } } },
            ["Kağıt Grubu|Süblime Kağıt"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Süblimasyon"] = new Dictionary<string, Dictionary<string, string[]>> { ["Hızlı Kuruyan"] = new Dictionary<string, string[]> { ["FD Serisi"] = new[] { "FD 45 gr", "FD 57 gr", "FD 70 gr" } }, ["Standart"] = new Dictionary<string, string[]> { ["ST Serisi"] = new[] { "ST 60 gr", "ST 90 gr" } } } },
            ["Binder Grubu|Tekstil Binderleri"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Tekstil Binder"] = new Dictionary<string, Dictionary<string, string[]>> { ["Akrilik"] = new Dictionary<string, string[]> { ["TB Serisi"] = new[] { "TB 100", "TB 200 SOFT" } } } },
            ["Binder Grubu|İnşaat ve Boya Binderleri"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["İnşaat Binder"] = new Dictionary<string, Dictionary<string, string[]>> { ["Stiren Akrilik"] = new Dictionary<string, string[]> { ["IB Serisi"] = new[] { "IB 500", "IB 550" } } } },
            ["Binder Grubu|Kağıt Kaplama Binderleri"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Kağıt Binder"] = new Dictionary<string, Dictionary<string, string[]>> { ["SB Lateks"] = new Dictionary<string, string[]> { ["KB Serisi"] = new[] { "KB 10", "KB 20" } } } },
            ["Yardımcı Kimyasallar|Tekstil Yardımcı Kimyasalları"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Yardımcı Kimyasal"] = new Dictionary<string, Dictionary<string, string[]>> { ["Kıvamlaştırıcı"] = new Dictionary<string, string[]> { ["TK Serisi"] = new[] { "TK 40", "TK 60" } }, ["Yumuşatıcı"] = new Dictionary<string, string[]> { ["SF Serisi"] = new[] { "SF 10", "SF 20" } } } },
            ["Yardımcı Kimyasallar|İnşaat ve Boya Yardımcı Kimyasalları"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Yardımcı Kimyasal"] = new Dictionary<string, Dictionary<string, string[]>> { ["Dispersant"] = new Dictionary<string, string[]> { ["DP Serisi"] = new[] { "DP 100" } } } },
            ["Yardımcı Kimyasallar|Kağıt Kaplama Yardımcı Kimyasalları"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Yardımcı Kimyasal"] = new Dictionary<string, Dictionary<string, string[]>> { ["Köpük Kesici"] = new Dictionary<string, string[]> { ["KK Serisi"] = new[] { "KK 5" } } } },
            ["Baskı Makineleri|Dijital"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Dijital Baskı Makinesi"] = new Dictionary<string, Dictionary<string, string[]>> { ["DTF Makine"] = new Dictionary<string, string[]> { ["UP Serisi"] = new[] { "UP 600 DTF", "UP 1200 DTF" } }, ["Süblime Makine"] = new Dictionary<string, string[]> { ["US Serisi"] = new[] { "US 1800", "US 3200" } } } },
            ["Baskı Makineleri|Emprime"] = new Dictionary<string, Dictionary<string, Dictionary<string, string[]>>> { ["Emprime Baskı Makinesi"] = new Dictionary<string, Dictionary<string, string[]>> { ["Rotasyon"] = new Dictionary<string, string[]> { ["UR Serisi"] = new[] { "UR 8", "UR 12" } }, ["Ahtapot"] = new Dictionary<string, string[]> { ["UA Serisi"] = new[] { "UA 6/8", "UA 8/10" } } } }
        };
        /// <summary>§12 / §15.5 satış temsilcileri faaliyet grubu bazında ("AnaGrup|Grup" → temsilciler).</summary>
        public static readonly Dictionary<string, string[]> SatisTemsilcileri = new Dictionary<string, string[]>
        {
            ["Boya Grubu|Emprime"] = new[] { "Ayşe Demir", "Selin Kara" }, ["Boya Grubu|Dijital"] = new[] { "Mehmet Kaya", "Tolga Yaman" }, ["Kağıt Grubu|Süblime Kağıt"] = new[] { "Mehmet Kaya", "Gizem Tan" },
            ["Binder Grubu|İnşaat ve Boya Binderleri"] = new[] { "Emre Taş", "Burcu Aydın" }, ["Binder Grubu|Tekstil Binderleri"] = new[] { "Ayşe Demir", "Emre Taş" }, ["Binder Grubu|Kağıt Kaplama Binderleri"] = new[] { "Gizem Tan", "Emre Taş" },
            ["Yardımcı Kimyasallar|İnşaat ve Boya Yardımcı Kimyasalları"] = new[] { "Burcu Aydın" }, ["Yardımcı Kimyasallar|Tekstil Yardımcı Kimyasalları"] = new[] { "Selin Kara", "Ayşe Demir" }, ["Yardımcı Kimyasallar|Kağıt Kaplama Yardımcı Kimyasalları"] = new[] { "Gizem Tan" },
            ["Baskı Makineleri|Dijital"] = new[] { "Ali Yılmaz" }, ["Baskı Makineleri|Emprime"] = new[] { "Ali Yılmaz", "Tolga Yaman" }
        };
        public static readonly string[] Sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct().ToArray();
        public static readonly Dictionary<string, string> KodOnEkleri = new Dictionary<string, string> { ["Müşteri"] = "120", ["Tedarikçi"] = "320", ["Finansal Kuruluşlar"] = "102", ["Resmi Kurumlar"] = "360", ["Personel Carileri"] = "335" };
        public static readonly string[] TalepDurumlari = { "Taslak", "Onay Bekliyor", "Onaylandı", "Reddedildi", "İptal" };
        public static readonly Dictionary<string, string[]> Yetkiler = new Dictionary<string, string[]> { ["Talep Eden (Satış / Satın Alma)"] = new[] { "taslak", "onayaGonder", "iptal" }, ["Muhasebe (Cari Açılış Onayı)"] = new[] { "taslak", "onayaGonder", "onay", "kartAc", "iptal", "limit" }, ["Yönetici"] = new[] { "taslak", "onayaGonder", "onay", "kartAc", "iptal", "limit" } };
        /// <summary>Kredi limiti türleri: Yok (limit izlenmez) · Manuel (elle TL tutar) · Dinamik (son N ay aylık alım ortalaması × çarpan).</summary>
        public static readonly string[] LimitTurleri = { "Yok", "Manuel", "Dinamik" };
        public static readonly Dictionary<string, string> LimitTuruAciklama = new Dictionary<string, string> { ["Yok"] = "Limit izlenmez (ör. grup içi cari).", ["Manuel"] = "Muhasebe / yönetici tarafından elle girilen TL limit.", ["Dinamik"] = "Son N ayın satış faturalarından aylık alım ortalaması alınır ve çarpanla çarpılır (ör. son 3 ay ortalaması × 2); her açılışta güncel veriyle yeniden hesaplanır. Çarpan genellikle vade süresine göre seçilir (30 gün ≈ ×2, 60 gün ≈ ×3, 90 gün ≈ ×4)." };
        public static bool Yetkili(string rol, string e) => Yetkiler.TryGetValue(rol ?? "", out var l) && l.Contains(e);

        // ---- kayıtlar ----
        private static readonly List<CariKart> _kartlar = new List<CariKart>(); private static readonly List<CariTalep> _talepler = new List<CariTalep>(); private static bool _tohumlandi; private static int _sonTalep = 0;
        public static List<CariKart> Kartlar { get { Tohumla(); return _kartlar; } }
        public static List<CariTalep> Talepler { get { Tohumla(); return _talepler; } }
        public static CariKart KartBul(string kod) => Kartlar.FirstOrDefault(k => string.Equals(k.Kod, kod, StringComparison.OrdinalIgnoreCase));
        public static CariTalep TalepBul(string no) => Talepler.FirstOrDefault(t => string.Equals(t.No, no, StringComparison.OrdinalIgnoreCase));
        private static void Log(CariTalep t, DateTime z, string k, string alan, string eski, string yeni, string kaynak = "Ekran", string gerekce = null) => t.Audit.Add(new YnAudit { Zaman = z, Kullanici = k, Alan = alan, Eski = eski ?? "", Yeni = yeni ?? "", Kaynak = kaynak, Gerekce = gerekce ?? "" });
        /// <summary>Kart tipi ön ekine göre sıradaki benzersiz cari kodu (ör. 120.031).</summary>
        public static string YeniKod(string tip) { var on = KodOnEkleri.TryGetValue(tip ?? "", out var p) ? p : "199"; int n = Kartlar.Where(k => k.Kod.StartsWith(on + ".")).Select(k => int.TryParse(k.Kod.Substring(on.Length + 1), out var x) ? x : 0).DefaultIfEmpty(0).Max() + 1; return $"{on}.{n:000}"; }

        // =====================================================================
        // DOĞRULAMA (§4, §7, §13)
        // =====================================================================
        public static YnSonuc Dogrula(CariKart k, bool taslak, string haricKod = null)
        {
            var s = new YnSonuc();
            if (string.IsNullOrEmpty(k.KartTipi) || !KartTipleri.Contains(k.KartTipi)) s.Hatalar.Add("Cari Kart Tipi zorunludur.");
            bool mt = k.KartTipi == "Müşteri" || k.KartTipi == "Tedarikçi";
            if (mt && string.IsNullOrEmpty(k.Bolge)) s.Hatalar.Add("Müşteri / Tedarikçi için Cari Bölge (Yurtiçi / Yurtdışı) zorunludur.");
            if (k.Bolge == "Yurtiçi" && !string.IsNullOrEmpty(k.Ulke) && k.Ulke != "Türkiye") s.Hatalar.Add("Cari Bölge = Yurtiçi ise ülke yalnız Türkiye olabilir.");
            if (k.Bolge == "Yurtiçi" && string.IsNullOrEmpty(k.Ulke)) k.Ulke = "Türkiye";
            if (k.Bolge == "Yurtdışı" && (string.IsNullOrEmpty(k.Ulke) || k.Ulke == "Türkiye")) s.Hatalar.Add("Cari Bölge = Yurtdışı ise ülke listesinden Türkiye dışında bir ülke seçilmelidir.");
            if (!mt && string.IsNullOrEmpty(k.Ulke)) k.Ulke = "Türkiye";
            if (k.Sinif == "Yurtdışı Şube" && (k.Bolge != "Yurtdışı" || string.IsNullOrEmpty(k.Ulke))) s.Hatalar.Add("Yurtdışı Şube için Cari Bölge = Yurtdışı ve ülke seçimi zorunludur.");
            var siniflar = Siniflar(k.KartTipi ?? "", k.Bolge ?? "");
            if (!taslak && string.IsNullOrEmpty(k.Sinif)) s.Hatalar.Add("Cari Sınıfı zorunludur (kart tipi + bölge matrisinden seçilir).");
            if (!string.IsNullOrEmpty(k.Sinif) && siniflar.Length > 0 && !siniflar.Contains(k.Sinif)) s.Hatalar.Add($"Cari Sınıfı '{k.Sinif}', {k.KartTipi} / {k.Bolge} matrisinde yok (izinli: {string.Join(", ", siniflar)}).");
            if ((k.KartTipi == "Finansal Kuruluşlar" || k.KartTipi == "Resmi Kurumlar") && k.Sinif == "Özel Takip") s.Hatalar.Add($"{k.KartTipi} için Özel Takip sınıfı seçilemez.");
            if (!taslak && string.IsNullOrEmpty(k.IliskiDurumu)) s.Hatalar.Add("İlişki Durumu (Grup İçi / İlişkili Cari / Grup Dışı) zorunludur.");
            if (!taslak && string.IsNullOrEmpty(k.Rol)) s.Hatalar.Add("Cari Rolü zorunludur.");
            if (k.KartTipi == "Müşteri" && k.Rol == "Sadece Tedarikçi") s.Hatalar.Add("Müşteri kartında rol 'Sadece Tedarikçi' olamaz.");
            if (k.KartTipi == "Tedarikçi" && k.Rol == "Sadece Müşteri") s.Hatalar.Add("Tedarikçi kartında rol 'Sadece Müşteri' olamaz.");
            if (!mt && !string.IsNullOrEmpty(k.Rol) && k.Rol != "Diğer") s.Uyarilar.Add($"{k.KartTipi} için rol genellikle 'Diğer' seçilir.");
            if (k.Rol == "Çift Rol") { if (string.IsNullOrEmpty(k.EsCariKodu)) s.Hatalar.Add("Çift Rol seçildi: Eş Cari Kodu zorunludur (müşteri ve tedarikçi kartları karşılıklı bağlanır)."); else { var es = KartBul(k.EsCariKodu); if (es == null) s.Uyarilar.Add($"Eş Cari Kodu {k.EsCariKodu} sistemde yok; karşı kart onay sonrası açılacaksa boş bırakıp sonradan bağlanabilir."); else if (es.KartTipi == k.KartTipi) s.Hatalar.Add($"Eş Cari Kodu {k.EsCariKodu} aynı tipte ({es.KartTipi}); karşı rolün kartı olmalıdır."); } }
            if (k.Rol != "Çift Rol" && !string.IsNullOrEmpty(k.EsCariKodu)) s.Uyarilar.Add("Eş Cari Kodu yalnız Çift Rol'de kullanılır; alan yok sayılacak.");
            if (!taslak) { if (string.IsNullOrWhiteSpace(k.ResmiUnvan)) s.Hatalar.Add("Cari Resmi Ünvan zorunludur."); if (string.IsNullOrWhiteSpace(k.KisaAd)) s.Hatalar.Add("Cari Kısa Adı zorunludur."); if (mt && string.IsNullOrWhiteSpace(k.VergiNo)) s.Hatalar.Add("Vergi No / TCKN zorunludur."); if (mt && k.Bolge == "Yurtiçi" && string.IsNullOrWhiteSpace(k.VergiDairesi)) s.Hatalar.Add("Yurtiçi cari için vergi dairesi zorunludur."); if (mt && string.IsNullOrWhiteSpace(k.VadeSarti)) s.Hatalar.Add("Vade / Ödeme Şartı zorunludur."); }
            if (!string.IsNullOrWhiteSpace(k.VergiNo)) { var vn = k.VergiNo.Trim(); if (k.Ulke == "Türkiye" && !(vn.Length == 10 || vn.Length == 11) ) s.Uyarilar.Add("Vergi no 10, TCKN 11 haneli olmalıdır."); var ayni = Kartlar.FirstOrDefault(x => x.VergiNo == vn && x.Kod != haricKod && x.KartTipi == k.KartTipi); if (ayni != null) s.Hatalar.Add($"Aynı vergi no ile {k.KartTipi} kartı zaten var: {ayni.Kod} {ayni.KisaAd} (mükerrer kart açılamaz; güncelleme talebi ya da çift rol için karşı tip kartı kullanın)."); }
            if (!string.IsNullOrWhiteSpace(k.Eposta) && !k.Eposta.Contains("@")) s.Uyarilar.Add("E-posta biçimi geçersiz görünüyor.");
            // faaliyet satırları
            if (mt && !taslak && k.Faaliyetler.Count == 0) s.Uyarilar.Add("Faaliyet eşleştirmesi girilmedi; müşteri / tedarikçi kartı için en az bir faaliyet grubu önerilir.");
            var gruplar = new HashSet<string>();
            foreach (var f in k.Faaliyetler)
            {
                string ad = $"Faaliyet satırı {f.Sira}";
                if (string.IsNullOrEmpty(f.AnaGrup) || !FaaliyetHiyerarsisi.ContainsKey(f.AnaGrup)) { s.Hatalar.Add($"{ad}: Faaliyet Ana Grubu seçilmelidir."); continue; }
                if (string.IsNullOrEmpty(f.Grup)) s.Hatalar.Add($"{ad}: Faaliyet Ana Grubu seçilmeden Faaliyet Grubu seçilemez; grup zorunludur.");
                else if (!FaaliyetHiyerarsisi[f.AnaGrup].Contains(f.Grup)) s.Hatalar.Add($"{ad}: '{f.Grup}', {f.AnaGrup} altında yok.");
                if (!string.IsNullOrEmpty(f.SatisTemsilcisi)) { if (string.IsNullOrEmpty(f.Grup)) s.Hatalar.Add($"{ad}: Faaliyet Grubu seçilmeden temsilci atanamaz."); else if (SatisTemsilcileri.TryGetValue(f.AnaGrup + "|" + f.Grup, out var tl) && !tl.Contains(f.SatisTemsilcisi)) s.Uyarilar.Add($"{ad}: {f.SatisTemsilcisi} bu faaliyet grubunun temsilci listesinde değil."); }
                else if (!taslak && k.KartTipi == "Müşteri") s.Hatalar.Add($"{ad}: müşteri kartında satış temsilcisi faaliyet grubu bazında seçilmelidir.");
                if (!string.IsNullOrEmpty(f.Grup) && !gruplar.Add(f.AnaGrup + "|" + f.Grup)) s.Hatalar.Add($"{ad}: aynı cari için {f.AnaGrup} / {f.Grup} zaten girildi (mükerrer faaliyet).");
                if (!string.IsNullOrEmpty(f.UrunGrubu) && string.IsNullOrEmpty(f.UrunSinifi)) s.Uyarilar.Add($"{ad}: ürün grubu girildi ama ürün sınıfı boş; hiyerarşi sırası sınıf > grup > seri > ad.");
                if (!string.IsNullOrEmpty(f.UrunAdi) && (string.IsNullOrEmpty(f.UrunSerisi) || string.IsNullOrEmpty(f.UrunGrubu))) s.Uyarilar.Add($"{ad}: ürün adı için seri ve grup da doldurulmalıdır.");
            }
            if (k.Faaliyetler.Count(f => f.AnaFaaliyet) > 1) s.Hatalar.Add("Ana Faaliyet mi? bir cari için tek satırda 'Evet' olabilir.");
            if (mt && k.Faaliyetler.Count > 0 && !k.Faaliyetler.Any(f => f.AnaFaaliyet)) s.Uyarilar.Add("Ana faaliyet işaretlenmedi; raporlama için bir satır 'Evet' olmalıdır.");
            return s;
        }
        public static CariKart KartKur(CariTalepGiris g)
        {
            var k = new CariKart { KisaAd = (g.KisaAd ?? "").Trim(), KisaltmaliUnvan = (g.KisaltmaliUnvan ?? "").Trim(), ResmiUnvan = (g.ResmiUnvan ?? "").Trim(), VergiNo = (g.VergiNo ?? "").Trim(), VergiDairesi = (g.VergiDairesi ?? "").Trim(), KartTipi = g.KartTipi, Bolge = (g.KartTipi == "Müşteri" || g.KartTipi == "Tedarikçi") ? g.Bolge : (string.IsNullOrEmpty(g.Bolge) ? "Yurtiçi" : g.Bolge), Ulke = g.Ulke, Sehir = (g.Sehir ?? "").Trim(), Sinif = g.Sinif, IliskiDurumu = g.IliskiDurumu, Rol = g.CariRolu, EsCariKodu = g.CariRolu == "Çift Rol" ? (g.EsCariKodu ?? "").Trim() : "", Yetkili = (g.Yetkili ?? "").Trim(), Telefon = (g.Telefon ?? "").Trim(), Eposta = (g.Eposta ?? "").Trim(), VadeSarti = g.VadeSarti, ElleDurum = g.Durum == "Pasif" ? "Pasif" : null, Not = (g.Not ?? "").Trim(), Sirket = g.Sirket };   // durum otomatik; formda Pasif seçildiyse elle pasif
            if (string.IsNullOrEmpty(k.KisaltmaliUnvan)) k.KisaltmaliUnvan = k.KisaAd;
            int n = 0; k.Faaliyetler = (g.Faaliyetler ?? new List<CariFaaliyetGiris>()).Where(f => !string.IsNullOrEmpty(f.AnaGrup) || !string.IsNullOrEmpty(f.Grup)).Select(f => new CariFaaliyet { Sira = ++n, AnaGrup = f.AnaGrup, Grup = f.Grup, SatisTemsilcisi = f.SatisTemsilcisi ?? "", AnaFaaliyet = f.AnaFaaliyet, UrunSinifi = f.UrunSinifi ?? "", UrunGrubu = f.UrunGrubu ?? "", UrunSerisi = f.UrunSerisi ?? "", UrunAdi = f.UrunAdi ?? "", Durum = string.IsNullOrEmpty(f.Durum) ? "Aktif" : f.Durum, BaslangicTarihi = DateTime.TryParseExact(f.BaslangicTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : (DateTime?)null, Not = f.Not ?? "" }).ToList();
            return k;
        }

        // =====================================================================
        // TALEP AKIŞI: taslak → onaya gönder → Muhasebe onayı → kart açılır (CARİ_MASTER)
        // =====================================================================
        public static (CariTalep Talep, YnSonuc Sonuc) Kaydet(CariTalepGiris g, string kullanici, string departman, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var s = new YnSonuc(); var t = g.Id > 0 ? Talepler.FirstOrDefault(x => x.Id == g.Id) : null; if (g.Id > 0 && t == null) { s.Hatalar.Add("Talep bulunamadı."); return (null, s); }
                if (t != null && t.Durum != "Taslak" && t.Durum != "Reddedildi") { s.Hatalar.Add($"{t.Durum} durumundaki talep düzenlenemez."); return (t, s); }
                bool yeni = t == null; if (yeni) { t = new CariTalep { Id = ++_sonTalep, No = $"CT-{z:yyyy}-{_sonTalep:0000}", Tarih = z, TalepEden = kullanici, Departman = departman }; }
                var eski = t.Kart; t.Kart = KartKur(g); t.TalepTuru = string.IsNullOrEmpty(g.TalepTuru) ? "Yeni Kart" : g.TalepTuru; t.Sirket = string.IsNullOrEmpty(g.Sirket) ? (t.Sirket ?? Sirketler[0]) : g.Sirket; t.Kart.Sirket = t.Sirket; t.Gerekce = (g.Gerekce ?? "").Trim();
                var d = Dogrula(t.Kart, true); s.Uyarilar.AddRange(d.Uyarilar); s.Hatalar.AddRange(d.Hatalar.Where(h => h.StartsWith("Cari Kart Tipi") || h.Contains("mükerrer kart")));
                if (!s.Gecerli) return (yeni ? null : t, s);
                if (yeni) Log(t, z, kullanici, "STATUS", "", "Taslak", "Sistem"); else { foreach (var (alan, e, y) in new[] { ("KART_TIPI", eski.KartTipi, t.Kart.KartTipi), ("BOLGE", eski.Bolge, t.Kart.Bolge), ("ULKE", eski.Ulke, t.Kart.Ulke), ("SINIF", eski.Sinif, t.Kart.Sinif), ("ILISKI", eski.IliskiDurumu, t.Kart.IliskiDurumu), ("ROL", eski.Rol, t.Kart.Rol), ("ES_CARI", eski.EsCariKodu, t.Kart.EsCariKodu), ("RESMI_UNVAN", eski.ResmiUnvan, t.Kart.ResmiUnvan), ("VERGI_NO", eski.VergiNo, t.Kart.VergiNo), ("VADE", eski.VadeSarti, t.Kart.VadeSarti) }) if ((e ?? "") != (y ?? "")) Log(t, z, kullanici, alan, e, y); if (eski.Faaliyetler.Count != t.Kart.Faaliyetler.Count) Log(t, z, kullanici, "FAALIYET_SATIR", eski.Faaliyetler.Count.ToString(), t.Kart.Faaliyetler.Count.ToString()); if (t.Durum == "Reddedildi") { t.Durum = "Taslak"; t.Onaylar.Clear(); t.RedNedeni = ""; Log(t, z, kullanici, "STATUS", "Reddedildi", "Taslak (revize)"); } }
                t.GuncellemeZamani = z; if (yeni) _talepler.Insert(0, t); return (t, s);
            }
        }
        public static YnSonuc OnayaGonder(CariTalep t, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var s = Dogrula(t.Kart, false); if (t.Durum != "Taslak") s.Hatalar.Insert(0, $"Yalnız Taslak talep onaya gönderilebilir ({t.Durum})."); if (!s.Gecerli) return s;
                var muh = NakitAvansOrnek.Personel.FirstOrDefault(p => p.Rol == "Muhasebe" && p.Sirket == t.Sirket) ?? NakitAvansOrnek.Personel.First(p => p.Rol == "Muhasebe");
                t.Onaylar = new List<YnOnay> { new YnOnay { Sira = 1, Adim = "Muhasebe — Cari Açılış Onayı", Kisi = muh.AdSoyad } };
                if (t.Kart.IliskiDurumu == "Grup İçi" || t.Kart.IliskiDurumu == "İlişkili Cari" || t.Kart.Sinif == "Özel Takip") t.Onaylar.Add(new YnOnay { Sira = 2, Adim = "Mali İşler Direktörü (grup içi / ilişkili / özel takip)", Kisi = NakitAvansOrnek.UstYonetim(muh).AdSoyad });
                t.Durum = "Onay Bekliyor"; t.GuncellemeZamani = z; Log(t, z, kullanici, "STATUS", "Taslak", "Onay Bekliyor", "Workflow", string.Join(" → ", t.Onaylar.Select(o => o.Adim))); return s;
            }
        }
        public static string Onayla(CariTalep t, string eylem, string not, string rol, string kullanici, DateTime? simdi = null)
        {
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; if (!Yetkili(rol, "onay")) return "Bu rol onay veremez."; if (t.Durum != "Onay Bekliyor") return $"Talep onay sürecinde değil ({t.Durum})."; var adim = t.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor"); if (adim == null) return "Bekleyen adım yok.";
                if (eylem == "Reddet") { if (string.IsNullOrWhiteSpace(not)) return "Ret nedeni zorunludur."; adim.Durum = "Reddedildi"; adim.Tarih = z; adim.Not = not; t.Durum = "Reddedildi"; t.RedNedeni = not; Log(t, z, kullanici, "STATUS", "Onay Bekliyor", "Reddedildi", "Workflow", not); return null; }
                adim.Durum = "Onaylandı"; adim.Tarih = z; adim.Not = not ?? ""; Log(t, z, kullanici, "APPROVAL", adim.Adim, "Onaylandı", "Workflow", not);
                if (t.Onaylar.All(o => o.Durum == "Onaylandı")) { var d = Dogrula(t.Kart, false); if (!d.Gecerli) return "Kart açılamadı: " + string.Join(" ", d.Hatalar); KartAc(t, kullanici, z); }
                t.GuncellemeZamani = z; return null;
            }
        }
        /// <summary>Onaylanan talep için CARİ_MASTER kartı açar; çift rolde eş kart karşılıklı bağlanır.</summary>
        private static void KartAc(CariTalep t, string kullanici, DateTime z)
        {
            var k = t.Kart; k.Kod = YeniKod(k.KartTipi); k.OlusturmaTarihi = z; k.Olusturan = kullanici; k.KaynakTalepNo = t.No; foreach (var f in k.Faaliyetler) if (f.BaslangicTarihi == null) f.BaslangicTarihi = z.Date;
            _kartlar.Add(k); if (k.Rol == "Çift Rol") { var es = KartBul(k.EsCariKodu); if (es != null && string.IsNullOrEmpty(es.EsCariKodu)) { es.EsCariKodu = k.Kod; es.Rol = "Çift Rol"; } }
            t.Durum = "Onaylandı"; t.OlusanKod = k.Kod; Log(t, z, kullanici, "STATUS", "Onay Bekliyor", "Onaylandı", "Workflow"); Log(t, z, kullanici, "CARI_MASTER", "", $"{k.Kod} {k.KisaAd} açıldı · {k.Faaliyetler.Count} faaliyet satırı", "ERP");
        }
        public static string Iptal(CariTalep t, string neden, string kullanici, DateTime? simdi = null) { lock (Kilit) { var z = simdi ?? DateTime.Now; if (string.IsNullOrWhiteSpace(neden)) return "İptal nedeni zorunludur."; if (t.Durum == "Onaylandı") return "Kartı açılmış talep iptal edilemez; kart pasife alınmalıdır."; Log(t, z, kullanici, "STATUS", t.Durum, "İptal", "Ekran", neden); t.Durum = "İptal"; t.RedNedeni = neden; t.GuncellemeZamani = z; return null; } }
        /// <summary>Durum otomatik hesaplanır; buradan yalnız "elle pasif" konur ("Pasif") ya da kaldırılır ("Aktif" → otomatik kural yeniden geçerli).</summary>
        public static string KartDurum(CariKart k, string durum, string kullanici)
        {
            lock (Kilit)
            {
                if (durum == "Pasif") { k.ElleDurum = "Pasif"; k.ElleDurumKim = kullanici; k.ElleDurumZaman = DateTime.Now; k.Not = (k.Not + $" [{DateTime.Now:dd.MM.yyyy} {kullanici}: elle pasif]").Trim(); return null; }
                if (durum == "Aktif") { if (k.ElleDurum != "Pasif") return $"Kart elle pasife alınmamış; durum otomatik hesaplanıyor ({k.DurumKaynagi})."; k.ElleDurum = null; k.ElleDurumKim = null; k.ElleDurumZaman = null; k.Not = (k.Not + $" [{DateTime.Now:dd.MM.yyyy} {kullanici}: elle pasif kaldırıldı]").Trim(); return null; }
                return "Durum Aktif / Pasif olmalı.";
            }
        }

        // =====================================================================
        // KREDİ LİMİTİ (müşteri kartı): Manuel (elle TL) ya da Dinamik (son N ay aylık alım ortalaması × çarpan)
        // Alımlar = müşterinin satış faturaları (Cari Mutabakat hareketleri, TL karşılığı; döviz faturalar güncel kurla); cari risk = açık bakiye (TL).
        // =====================================================================
        static decimal KurTl(string pb) => string.IsNullOrEmpty(pb) || pb == "TL" || pb == "TRY" ? 1m : (MutabakatOrnek.Kurlar.TryGetValue(pb, out var k) ? k : 1m);
        /// <summary>Limit hesabı; her çağrıda güncel satış faturaları ve bakiyeyle yeniden üretilir (dinamik limit sabit tutulmaz).</summary>
        public static CariLimit LimitHesapla(CariKart k)
        {
            var s = new CariLimit { Tur = k.LimitTuru ?? "Yok", Ay = k.DinamikAy <= 0 ? 3 : k.DinamikAy, Carpan = k.DinamikCarpan <= 0 ? 2m : k.DinamikCarpan };
            var c = string.IsNullOrEmpty(k.Kod) ? null : MutabakatOrnek.Bul(k.Kod);
            if (c != null) { s.Bakiye = c.BakiyeTl; s.BakiyeKaynak = c.Dovizler.Count == 0 ? "" : string.Join(" · ", c.Dovizler.Where(d => d.Net != 0).Select(d => $"{d.ParaBirimi} {d.Net.ToString("N2", Tr)}")); }
            var bugun = DateTime.Today; var bas = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-(s.Ay - 1));   // içinde bulunulan ay dahil son N takvim ayı
            for (int i = 0; i < s.Ay; i++) { var ay = bas.AddMonths(i); var fat = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Tarih.Year == ay.Year && h.Tarih.Month == ay.Month).ToList(); s.Aylar.Add((ay.ToString("MMMM yyyy", Tr), Math.Round(fat.Sum(h => h.Borc * KurTl(h.ParaBirimi)), 2), fat.Count)); }
            s.AlimToplam = s.Aylar.Sum(a => a.Tutar); s.AylikOrtalama = Math.Round(s.AlimToplam / s.Ay, 2);
            if (s.Tur == "Manuel") { s.Limit = k.ManuelLimit; s.Aciklama = $"Manuel limit{(string.IsNullOrEmpty(k.LimitKim) ? "" : $" · {k.LimitKim} {k.LimitZaman:dd.MM.yyyy}")}"; }
            else if (s.Tur == "Dinamik") { s.Limit = Math.Round(s.AylikOrtalama * s.Carpan / 1000m, 0) * 1000m; s.Aciklama = $"Son {s.Ay} ay alım {s.AlimToplam.ToString("N0", Tr)} ₺ → aylık ortalama {s.AylikOrtalama.ToString("N0", Tr)} ₺ × {s.Carpan.ToString("0.##", Tr)} = {s.Limit.ToString("N0", Tr)} ₺ (1.000 ₺'ye yuvarlı; her açılışta yeniden hesaplanır)"; }
            else s.Aciklama = k.KartTipi == "Müşteri" ? "Limit tanımlı değil; bakiye izlenir ama limit kontrolü yapılmaz." : "Limit yalnız müşteri kartlarında izlenir.";
            return s;
        }
        static string LimitOzet(CariKart k) => k.LimitTuru == "Manuel" ? $"Manuel {k.ManuelLimit.ToString("N0", Tr)} ₺" : k.LimitTuru == "Dinamik" ? $"Dinamik son {k.DinamikAy} ay ort. × {k.DinamikCarpan.ToString("0.##", Tr)}" : "Yok";
        /// <summary>Kredi limiti tanımı: tür + (manuel tutar | dinamik ay ve çarpan). Yetki: Muhasebe / Yönetici ("limit"). Dönüş: hata metni ya da null.</summary>
        public static string LimitDegistir(CariKart k, string tur, decimal manuelLimit, int ay, decimal carpan, string not, string kullanici)
        {
            lock (Kilit)
            {
                if (k.KartTipi != "Müşteri") return "Kredi limiti yalnız müşteri kartlarında tanımlanır.";
                if (!LimitTurleri.Contains(tur ?? "")) return "Limit türü Yok / Manuel / Dinamik olmalı.";
                if (tur == "Manuel" && manuelLimit < 0) return "Manuel limit sıfırdan küçük olamaz.";
                if (tur == "Dinamik" && (ay < 1 || ay > 24)) return "Dinamik limitte ay sayısı 1–24 arasında olmalı.";
                if (tur == "Dinamik" && (carpan <= 0 || carpan > 12)) return "Dinamik limitte çarpan 0'dan büyük, en çok 12 olmalı.";
                string eski = LimitOzet(k); var z = DateTime.Now;
                k.LimitTuru = tur; if (tur == "Manuel") k.ManuelLimit = Math.Round(manuelLimit, 2); if (tur == "Dinamik") { k.DinamikAy = ay; k.DinamikCarpan = carpan; }
                k.LimitNot = (not ?? "").Trim(); k.LimitKim = kullanici ?? ""; k.LimitZaman = z;
                string yeni = LimitOzet(k); if (eski != yeni || !string.IsNullOrEmpty(k.LimitNot)) k.LimitGecmisi.Insert(0, new CariLimitLog { Zaman = z, Kullanici = kullanici ?? "", Eski = eski, Yeni = yeni, Not = k.LimitNot });
                return null;
            }
        }

        // =====================================================================
        // SON HAREKET → OTOMATİK DURUM (1 yıl kuralı)
        // Kaynaklar: kart açılışı; müşteri kodu ile teklif / sipariş (TeklifOrnek), numune (NumuneOrnek), ihracat belge / tahsilat (IhracatBelgeOrnek);
        // ad eşleşmesiyle alım faturaları (AlimOnayOrnek) ve ödemeler (YapilanOdemelerOrnek); personel carilerinde nakit avans (NakitAvansOrnek).
        // Ad eşleşmesi: yasal ek ve noktalama temizlenir, iki ardışık sözcük ortaksa ("Vergi Dairesi", "Ziraat Bankası") ya da tek sözcüklü ad diğerinde geçiyorsa ("Petkim") eşleşir.
        // =====================================================================
        private static readonly HashSet<string> AdDurak = new HashSet<string> { "aş", "as", "ltd", "şti", "sti", "san", "tic", "ve", "hiz", "co", "inc", "gmbh", "sa", "spa", "srl", "sarl", "pvt", "tc", "sae", "ag", "se" };
        private static string[] AdTokenleri(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return new string[0];
            var t = s.ToLower(Tr).Replace("(t)", " ").Replace("(avans)", " ");
            var sb = new System.Text.StringBuilder(t.Length); foreach (var ch in t) sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
            return sb.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(x => x.Length > 1 && !AdDurak.Contains(x)).ToArray();
        }
        /// <summary>Serbest metin alıcı / tedarikçi adı bu cari karta ait mi?</summary>
        public static bool AdEsle(string ad, CariKart k)
        {
            var a = AdTokenleri(ad); if (a.Length == 0) return false;
            foreach (var hedef in new[] { k.ResmiUnvan, k.KisaAd, k.KisaltmaliUnvan })
            {
                var b = AdTokenleri(hedef); if (b.Length == 0) continue;
                if (a.Length == 1 || b.Length == 1) { var tek = a.Length == 1 ? a[0] : b[0]; var cok = a.Length == 1 ? b : a; if (tek.Length >= 5 && cok.Contains(tek)) return true; if (a.Length == 1 && b.Length == 1 && a[0] == b[0]) return true; continue; }
                for (int i = 0; i + 1 < a.Length; i++) for (int j = 0; j + 1 < b.Length; j++) if (a[i] == b[j] && a[i + 1] == b[j + 1]) return true;
            }
            return false;
        }
        private static List<(string Ad, DateTime Tarih, string Kaynak)> _adHareketleri; private static DateTime _adHareketZaman;
        /// <summary>Ad üzerinden eşlenen hareketler (alım faturaları + ödemeler); 10 sn önbellek.</summary>
        private static List<(string Ad, DateTime Tarih, string Kaynak)> AdHareketleri()
        {
            lock (Kilit)
            {
                if (_adHareketleri != null && (DateTime.Now - _adHareketZaman).TotalSeconds < 10) return _adHareketleri;
                var l = new List<(string, DateTime, string)>();
                foreach (var f in AlimOnayOrnek.Faturalar) l.Add((f.Tedarikci, f.Tarih, "Alım Faturası " + f.FaturaNo));
                foreach (var o in YapilanOdemelerOrnek.Liste()) if (o.Durum != "İptal") l.Add((o.Alici, o.Tarih, "Ödeme " + o.No));
                _adHareketleri = l; _adHareketZaman = DateTime.Now; return l;
            }
        }
        /// <summary>Carinin son hareket tarihi ve kaynağı (bugünden ileri tarihler sayılmaz; kart açılışı taban; taslak kartta bugün).</summary>
        public static (DateTime Tarih, string Kaynak) SonHareket(CariKart k)
        {
            var bugun = DateTime.Today;
            var en = (Tarih: k.OlusturmaTarihi == default ? bugun : k.OlusturmaTarihi.Date, Kaynak: k.OlusturmaTarihi == default ? "Taslak Kart" : "Kart Açılışı");
            void G(DateTime? t, string kaynak) { if (t.HasValue && t.Value.Date > en.Tarih && t.Value.Date <= bugun) en = (t.Value.Date, kaynak); }
            if (!string.IsNullOrEmpty(k.Kod) && k.KartTipi == "Müşteri")
            {
                foreach (var t in TeklifOrnek.Liste) if (t.CariKodu == k.Kod && t.Durum != "İptal") { G(t.Tarih, "Teklif " + t.No); G(t.SiparisTarihi, "Sipariş " + t.SiparisNo); }
                foreach (var n in NumuneOrnek.Liste) if (n.CariKodu == k.Kod && n.Durum != "İptal") { G(n.Tarih, "Numune " + n.No); G(n.TeslimTarihi, "Numune Teslimi " + n.No); }
                foreach (var b in IhracatBelgeOrnek.Liste) if (b.CariKodu == k.Kod && b.Durum != "İptal") { G(b.YuklemeTarihi, "İhracat " + b.DosyaNo); G(b.Tarih, "Belge Gönderimi " + b.No); G(b.OdemeTarihi, "Tahsilat " + b.FaturaNo); }
            }
            foreach (var h in AdHareketleri()) if (h.Tarih.Date > en.Tarih && AdEsle(h.Ad, k)) G(h.Tarih, h.Kaynak);
            if (k.KartTipi == "Personel Carileri" && !string.IsNullOrEmpty(k.Yetkili))
                lock (NakitAvansOrnek.Kilit) { foreach (var a in NakitAvansOrnek.Talepler) if (a.Kullanan != null && a.Kullanan.AdSoyad == k.Yetkili) { G(a.Tarih, "Avans " + a.No); foreach (var o in a.Odemeler) G(o.Tarih, "Avans Teslimi " + o.FisNo); } }
            return en;
        }

        // =====================================================================
        // ÖRNEK KAYITLAR (§8 senaryoları)
        // =====================================================================
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var b = DateTime.Today;
                CariFaaliyet F(string ana, string grup, string temsilci, bool anaF = false, string sinif = "", string urunGrubu = "", string seri = "", string ad = "", int gunOnce = 120) => new CariFaaliyet { AnaGrup = ana, Grup = grup, SatisTemsilcisi = temsilci, AnaFaaliyet = anaF, UrunSinifi = sinif, UrunGrubu = urunGrubu, UrunSerisi = seri, UrunAdi = ad, BaslangicTarihi = b.AddDays(-gunOnce) };
                CariKart K(string kod, string kisa, string unvan, string vkn, string vd, string tip, string bolge, string ulke, string sehir, string sinif, string iliski, string rol, string es, string yetkili, string tel, string mail, string vade, string sirket, int gunOnce, string not, params CariFaaliyet[] f)
                { var k = new CariKart { Kod = kod, KisaAd = kisa, KisaltmaliUnvan = kisa + (unvan.EndsWith("A.Ş.") ? " A.Ş." : unvan.EndsWith("Ltd. Şti.") ? " Ltd." : ""), ResmiUnvan = unvan, VergiNo = vkn, VergiDairesi = vd, KartTipi = tip, Bolge = bolge, Ulke = ulke, Sehir = sehir, Sinif = sinif, IliskiDurumu = iliski, Rol = rol, EsCariKodu = es, Yetkili = yetkili, Telefon = tel, Eposta = mail, VadeSarti = vade, Sirket = sirket, OlusturmaTarihi = b.AddDays(-gunOnce), Olusturan = "Ayten Yıldırım", Not = not, Faaliyetler = f.Select((x, i) => { x.Sira = i + 1; return x; }).ToList() }; _kartlar.Add(k); return k; }
                // ---- Müşteriler
                K("120.001", "ABC Tekstil", "ABC Tekstil San. ve Tic. A.Ş.", "0010012345", "Çorlu", "Müşteri", "Yurtiçi", "Türkiye", "Tekirdağ", "Ticari Müşteriler", "Grup Dışı", "Sadece Müşteri", "", "Hakan Yıldız", "0282 654 10 10", "muhasebe@abctekstil.com.tr", "Vade 60 Gün", "Uras Kimya", 400, "§12 örnek: birden fazla faaliyet grubu, faaliyet grubu bazında temsilci.",
                    F("Boya Grubu", "Emprime", "Ayşe Demir", true, "Su Bazlı", "Hybrid", "Clear Bases", "S 10 CLEAR"), F("Boya Grubu", "Dijital", "Mehmet Kaya", false, "DTF"), F("Baskı Makineleri", "Dijital", "Ali Yılmaz"), F("Kağıt Grubu", "Süblime Kağıt", "Mehmet Kaya"), F("Binder Grubu", "Tekstil Binderleri", "Ayşe Demir"));
                K("120.002", "Marmara Plastik", "Marmara Plastik San. A.Ş.", "6120045670", "Gebze", "Müşteri", "Yurtiçi", "Türkiye", "Kocaeli", "Bayi", "Grup Dışı", "Sadece Müşteri", "", "Levent Şahin", "0262 751 22 33", "levent@marmaraplastik.com", "Vade 45 Gün", "Uras Kimya", 380, "", F("Binder Grubu", "İnşaat ve Boya Binderleri", "Emre Taş", true, "İnşaat Binder", "Stiren Akrilik", "IB Serisi", "IB 500"), F("Yardımcı Kimyasallar", "İnşaat ve Boya Yardımcı Kimyasalları", "Burcu Aydın"));
                K("120.003", "Ege Boya", "Ege Boya ve Vernik San. A.Ş.", "3300034560", "Kemalpaşa", "Müşteri", "Yurtiçi", "Türkiye", "İzmir", "Distribütör", "Grup Dışı", "Çift Rol", "320.045", "Cem Aydın", "0232 877 45 45", "cem.aydin@egeboya.com", "Vade 90 Gün", "Uras Kimya", 360, "Hem distribütör hem hammadde tedarikçisi; eş kart 320.045.", F("Boya Grubu", "Emprime", "Selin Kara", true, "Plastisol"), F("Binder Grubu", "İnşaat ve Boya Binderleri", "Emre Taş"));
                K("120.004", "Karadeniz Ambalaj", "Karadeniz Ambalaj San. ve Tic. Ltd. Şti.", "5210056780", "Samsun", "Müşteri", "Yurtiçi", "Türkiye", "Samsun", "Ticari Müşteriler", "Grup Dışı", "Çift Rol", "320.058", "Nihat Yavuz", "0362 266 10 20", "info@karadenizambalaj.com", "Vade 60 Gün", "Uras Kimya", 300, "", F("Kağıt Grubu", "Süblime Kağıt", "Gizem Tan", true, "Süblimasyon", "Hızlı Kuruyan", "FD Serisi", "FD 57 gr"), F("Binder Grubu", "Kağıt Kaplama Binderleri", "Gizem Tan"));
                K("120.005", "Alv Kimya", "ALV Kimya San. A.Ş.", "0610023456", "Gebze", "Müşteri", "Yurtiçi", "Türkiye", "Kocaeli", "Ticari Müşteriler", "Grup İçi", "Çift Rol", "320.005", "Sevgi Ay", "0262 751 00 05", "muhasebe@alvkimya.com", "Vade 30 Gün", "Uras Kimya", 500, "Grup şirketi; yansıtma ve grup içi satışlar.", F("Boya Grubu", "Emprime", "Ayşe Demir", true, "Su Bazlı"), F("Yardımcı Kimyasallar", "Tekstil Yardımcı Kimyasalları", "Selin Kara"));
                K("120.006", "Selvi Kimya", "Selvi Kimya ve Ambalaj San. A.Ş.", "7600034567", "Çerkezköy", "Müşteri", "Yurtiçi", "Türkiye", "Tekirdağ", "Özel Takip", "Grup İçi", "Sadece Müşteri", "", "Pınar Ak", "0282 726 33 33", "finans@selvi.com.tr", "Vade 30 Gün", "Uras Kimya", 500, "Grup içi özel takip.", F("Binder Grubu", "Kağıt Kaplama Binderleri", "Gizem Tan", true));
                K("120.007", "Anadolu Emprime", "Anadolu Emprime Tekstil Ltd. Şti.", "0700067890", "Bursa Ertuğrulgazi", "Müşteri", "Yurtiçi", "Türkiye", "Bursa", "Ticari Müşteriler", "İlişkili Cari", "Sadece Müşteri", "", "Murat Güler", "0224 411 78 90", "murat@anadoluemprime.com", "Vade 60 Gün", "Uras Kimya", 220, "Yönetim bağlantılı; takip amaçlı ilişkili cari.", F("Boya Grubu", "Emprime", "Ayşe Demir", true, "Su Bazlı", "Pigment", "Color Concentrates", "PC 100 RED"), F("Baskı Makineleri", "Emprime", "Ali Yılmaz"));
                K("120.008", "TextilDruck GmbH", "TextilDruck Handels GmbH", "DE812345678", "", "Müşteri", "Yurtdışı", "Almanya", "Köln", "Yurtdışı Bayi", "Grup Dışı", "Sadece Müşteri", "", "Stefan Weber", "+49 221 555 010", "order@textildruck.de", "Akreditif (L/C)", "Uras Kimya", 250, "", F("Boya Grubu", "Dijital", "Mehmet Kaya", true, "DTF", "DTF Ink", "Film Series", "DTF INK CMYK"), F("Baskı Makineleri", "Dijital", "Ali Yılmaz"));
                K("120.009", "Uras Kimya GmbH", "Uras Kimya Vertriebs GmbH", "DE998877665", "", "Müşteri", "Yurtdışı", "Almanya", "Frankfurt", "Yurtdışı Şube", "Grup İçi", "Çift Rol", "320.009", "Esra Kaya", "+49 69 555 999", "office@uraskimya.de", "Vade 30 Gün", "Uras Kimya", 450, "Yurtdışı şube; ülke zorunlu.", F("Boya Grubu", "Emprime", "Selin Kara", true), F("Kağıt Grubu", "Süblime Kağıt", "Mehmet Kaya"));
                K("120.010", "Sublitex S.p.A.", "Sublitex Carta Sublimatica S.p.A.", "IT01234567890", "", "Müşteri", "Yurtdışı", "İtalya", "Prato", "Yurtdışı Distribütör", "Grup Dışı", "Sadece Müşteri", "", "Marco Rossi", "+39 0574 55 11 22", "acquisti@sublitex.it", "Mal Mukabili", "Avrupa Paper", 200, "", F("Kağıt Grubu", "Süblime Kağıt", "Gizem Tan", true, "Süblimasyon", "Standart", "ST Serisi", "ST 90 gr"));
                K("120.011", "Nil Tekstil", "Nil Tekstil Boya San. Ltd. Şti.", "6310078901", "Denizli", "Müşteri", "Yurtiçi", "Türkiye", "Denizli", "Özel Takip", "Grup Dışı", "Sadece Müşteri", "", "Ferhat Nil", "0258 371 44 55", "ferhat@niltekstil.com", "Peşin", "Uras Kimya", 90, "Vadesi geçmiş bakiye nedeniyle özel takip; peşin çalışılır.", F("Boya Grubu", "Emprime", "Ayşe Demir", true));
                // ---- Yurtdışı ihracat müşterileri (akreditif / vesaik / mal mukabili; İhracat Belge Gönderim Takibi ve Numune / Teklif ekranları da kullanır)
                K("120.012", "Nile Textile", "Nile Textile Co. S.A.E.", "EG100234567", "", "Müşteri", "Yurtdışı", "Mısır", "Kahire", "Ticari Müşteriler", "Grup Dışı", "Sadece Müşteri", "", "Ahmed Hassan", "+20 2 2575 4400", "import@niletextile.com.eg", "Akreditif (L/C)", "Uras Kimya", 330, "Akreditifle çalışır; amir banka Banque Misr.", F("Boya Grubu", "Emprime", "Ayşe Demir", true, "Su Bazlı", "Hybrid", "Clear Bases", "S 20 CLEAR"), F("Yardımcı Kimyasallar", "Tekstil Yardımcı Kimyasalları", "Selin Kara"));
                K("120.013", "Lahore Prints", "Lahore Prints (Pvt.) Ltd.", "PK0301234567", "", "Müşteri", "Yurtdışı", "Pakistan", "Lahor", "Yurtdışı Distribütör", "Grup Dışı", "Sadece Müşteri", "", "Imran Sheikh", "+92 42 3577 1200", "imran@lahoreprints.pk", "Akreditif (L/C)", "Uras Kimya", 290, "Akreditifle çalışır; amir banka Habib Bank Ltd.", F("Boya Grubu", "Emprime", "Selin Kara", true, "Plastisol"), F("Binder Grubu", "Tekstil Binderleri", "Emre Taş"));
                K("120.014", "Sublimation Supply", "Sublimation Supply Inc.", "US47-3312890", "", "Müşteri", "Yurtdışı", "ABD", "Houston", "Yurtdışı Bayi", "Grup Dışı", "Sadece Müşteri", "", "Karen Miller", "+1 713 555 0192", "purchasing@sublimationsupply.com", "Vesaik Mukabili", "Avrupa Paper", 260, "", F("Kağıt Grubu", "Süblime Kağıt", "Gizem Tan", true, "Süblimasyon", "Hızlı Kuruyan", "FD Serisi", "FD 57 gr"), F("Boya Grubu", "Dijital", "Mehmet Kaya"));
                K("120.015", "Warszawa Papier", "Warszawa Papier Sp. z o.o.", "PL5262345678", "", "Müşteri", "Yurtdışı", "Polonya", "Varşova", "Ticari Müşteriler", "Grup Dışı", "Sadece Müşteri", "", "Piotr Nowak", "+48 22 555 08 10", "zakupy@warszawapapier.pl", "Vade 60 Gün", "Avrupa Paper", 240, "AB içi; A.TR ile sevk.", F("Kağıt Grubu", "Süblime Kağıt", "Gizem Tan", true, "Süblimasyon", "Standart", "ST Serisi", "ST 60 gr"), F("Binder Grubu", "Kağıt Kaplama Binderleri", "Emre Taş"));
                K("120.016", "Dhaka Dyeing", "Dhaka Dyeing & Printing Mills Ltd.", "BD001234567", "", "Müşteri", "Yurtdışı", "Bangladeş", "Dakka", "Ticari Müşteriler", "Grup Dışı", "Sadece Müşteri", "", "Rahim Uddin", "+880 2 5566 7788", "rahim@dhakadyeing.com", "Akreditif (L/C)", "Alv Kimya", 200, "Akreditifle çalışır; amir banka Eastern Bank Ltd.", F("Binder Grubu", "Tekstil Binderleri", "Ayşe Demir", true, "Tekstil Binder", "Akrilik", "TB Serisi", "TB 100"), F("Yardımcı Kimyasallar", "Tekstil Yardımcı Kimyasalları", "Ayşe Demir"));
                K("120.017", "Casablanca Textile", "Casablanca Textile SARL", "MA001987654", "", "Müşteri", "Yurtdışı", "Fas", "Kazablanka", "Yurtdışı Bayi", "Grup Dışı", "Sadece Müşteri", "", "Youssef Benali", "+212 522 30 40 50", "achat@casatextile.ma", "Vesaik Mukabili", "Daf Kimya", 180, "", F("Yardımcı Kimyasallar", "Tekstil Yardımcı Kimyasalları", "Selin Kara", true, "Yardımcı Kimyasal", "Yumuşatıcı", "SF Serisi", "SF 10"), F("Baskı Makineleri", "Emprime", "Ali Yılmaz"));
                // ---- Kredi limitleri (müşteri kartları): varsayılan Dinamik = son 3 ay aylık alım ortalaması × çarpan; çarpan vade şartına göre (30 gün ×2, 45–60 gün ×3, 90 gün ×4, 120 gün ×5,
                //      akreditif / vesaik / mal mukabili ×3, vadeli çek ×4). Bazıları manuel (yönetim kararı / teminat), grup içi cariler limitsiz.
                decimal VadeCarpan(string v) => v == "Vade 30 Gün" ? 2m : v == "Vade 90 Gün" || v == "Çek (Vadeli)" ? 4m : v == "Vade 120 Gün" ? 5m : 3m;
                foreach (var k in _kartlar.Where(x => x.KartTipi == "Müşteri")) { k.LimitTuru = k.IliskiDurumu == "Grup İçi" ? "Yok" : "Dinamik"; k.DinamikAy = 3; k.DinamikCarpan = VadeCarpan(k.VadeSarti); k.LimitNot = k.LimitTuru == "Yok" ? "Grup içi cari; limit izlenmez." : $"Çarpan vade şartına göre ({k.VadeSarti})."; k.LimitKim = "Nazlı Erdem"; k.LimitZaman = b.AddDays(-45).AddHours(10); }
                void ML(string kod, decimal tutar, int gunOnce, string not) { var k = _kartlar.First(x => x.Kod == kod); string eski = LimitOzet(k); k.LimitTuru = "Manuel"; k.ManuelLimit = tutar; k.LimitNot = not; k.LimitKim = "Nazlı Erdem"; k.LimitZaman = b.AddDays(-gunOnce).AddHours(11); k.LimitGecmisi.Add(new CariLimitLog { Zaman = k.LimitZaman.Value, Kullanici = "Nazlı Erdem", Eski = eski, Yeni = LimitOzet(k), Not = not }); }
                ML("120.001", 9_000_000m, 30, "Yönetim kararı: yıllık çerçeve anlaşması; teminat mektubu 3 Mn ₺.");
                ML("120.003", 15_000_000m, 20, "Distribütör; teminat ipoteği var.");
                ML("120.011", 250_000m, 12, "Vadesi geçmiş bakiye; peşin çalışılır, limit düşürüldü.");
                ML("120.012", 5_000_000m, 25, "Akreditifli çalışır; açık hesap limiti yönetim onaylı.");
                ML("120.007", 900_000m, 8, "İlişkili cari; yönetim onaylı limit.");
                // ---- Tedarikçiler
                K("320.001", "Petkim", "Petkim Petrokimya Holding A.Ş.", "7290030050", "Aliağa", "Tedarikçi", "Yurtiçi", "Türkiye", "İzmir", "Hammadde", "Grup Dışı", "Sadece Tedarikçi", "", "Satış Ofisi", "0232 616 12 40", "satis@petkim.com.tr", "Vade 60 Gün", "Uras Kimya", 600, "", F("Binder Grubu", "Tekstil Binderleri", "", true, "Tekstil Binder"), F("Yardımcı Kimyasallar", "Tekstil Yardımcı Kimyasalları", ""));
                K("320.002", "Brenntag", "Brenntag Kimya Tic. Ltd. Şti.", "1850035940", "Tuzla", "Tedarikçi", "Yurtiçi", "Türkiye", "İstanbul", "Hammadde", "Grup Dışı", "Sadece Tedarikçi", "", "Deniz Aksu", "0216 593 40 00", "tr-siparis@brenntag.com", "Vade 45 Gün", "Uras Kimya", 520, "", F("Yardımcı Kimyasallar", "İnşaat ve Boya Yardımcı Kimyasalları", "", true), F("Yardımcı Kimyasallar", "Kağıt Kaplama Yardımcı Kimyasalları", ""));
                K("320.003", "Korozo Ambalaj", "Korozo Ambalaj San. ve Tic. A.Ş.", "5780012030", "Esenyurt", "Tedarikçi", "Yurtiçi", "Türkiye", "İstanbul", "Yardımcı Malzemeler", "Grup Dışı", "Sadece Tedarikçi", "", "Sibel Koç", "0212 866 00 00", "satis@korozo.com.tr", "Vade 60 Gün", "Selvi", 480, "Ambalaj ve yardımcı malzeme.", F("Kağıt Grubu", "Süblime Kağıt", "", true));
                K("320.004", "Öz Fason Baskı", "Öz Fason Baskı ve Konfeksiyon Ltd. Şti.", "6690023456", "Gaziantep", "Tedarikçi", "Yurtiçi", "Türkiye", "Gaziantep", "Fason", "Grup Dışı", "Sadece Tedarikçi", "", "Özgür Demir", "0342 337 12 12", "ozfason@gmail.com", "Vade 30 Gün", "Uras Kimya", 150, "", F("Boya Grubu", "Emprime", "", true));
                K("320.005", "Alv Kimya (T)", "ALV Kimya San. A.Ş.", "0610023456", "Gebze", "Tedarikçi", "Yurtiçi", "Türkiye", "Kocaeli", "Hizmetler", "Grup İçi", "Çift Rol", "120.005", "Sevgi Ay", "0262 751 00 05", "muhasebe@alvkimya.com", "Vade 30 Gün", "Uras Kimya", 500, "Grup içi hizmet ve hammadde tedarikçisi; eş kart 120.005.", F("Yardımcı Kimyasallar", "Tekstil Yardımcı Kimyasalları", "", true));
                K("320.006", "Anadolu Temizlik", "Anadolu Endüstriyel Temizlik Hiz. Ltd. Şti.", "0690043220", "İkitelli", "Tedarikçi", "Yurtiçi", "Türkiye", "İstanbul", "Hizmetler", "Grup Dışı", "Sadece Tedarikçi", "", "Halil Demirci", "0212 549 10 10", "info@anadolutemizlik.com", "Vade 30 Gün", "Avrupa Paper", 300, "Aylık endüstriyel temizlik hizmeti.", F("Hizmetler", "İdari Hizmetler", "Canan Su", true));
                K("320.007", "Bureau Veritas", "Bureau Veritas Gözetim Hizmetleri Ltd. Şti.", "1900024110", "Beşiktaş", "Tedarikçi", "Yurtiçi", "Türkiye", "İstanbul", "Danışmanlık", "Grup Dışı", "Sadece Tedarikçi", "", "Ece Tan", "0212 292 00 00", "tr@bureauveritas.com", "Peşin", "Uras Kimya", 200, "ISO denetim ve danışmanlık.", F("Hizmetler", "Danışmanlık / Denetim", "Canan Su", true));
                K("320.008", "BASF SE", "BASF SE Ludwigshafen", "DE149145247", "", "Tedarikçi", "Yurtdışı", "Almanya", "Ludwigshafen", "Hammadde", "Grup Dışı", "Sadece Tedarikçi", "", "Klaus Berger", "+49 621 60 0", "order@basf.com", "Akreditif (L/C)", "Uras Kimya", 550, "Yurtdışı hammadde tedarikçisi.", F("Binder Grubu", "İnşaat ve Boya Binderleri", "", true), F("Binder Grubu", "Kağıt Kaplama Binderleri", ""));
                K("320.009", "Uras Kimya GmbH (T)", "Uras Kimya Vertriebs GmbH", "DE998877665", "", "Tedarikçi", "Yurtdışı", "Almanya", "Frankfurt", "Hizmetler", "Grup İçi", "Çift Rol", "120.009", "Esra Kaya", "+49 69 555 999", "office@uraskimya.de", "Vade 30 Gün", "Uras Kimya", 450, "Yurtdışı şube hizmet faturaları.");
                K("320.010", "Dost Kimya", "Dost Kimya San. Ltd. Şti.", "3130056789", "Dilovası", "Tedarikçi", "Yurtiçi", "Türkiye", "Kocaeli", "Özel Takip", "İlişkili Cari", "Sadece Tedarikçi", "", "Kemal Dost", "0262 754 66 66", "kemal@dostkimya.com", "Vade 45 Gün", "Uras Kimya", 120, "Yönetim bağlantılı; kalite şikayeti nedeniyle özel takip.", F("Yardımcı Kimyasallar", "Tekstil Yardımcı Kimyasalları", "", true));
                K("320.045", "Ege Boya (T)", "Ege Boya ve Vernik San. A.Ş.", "3300034560", "Kemalpaşa", "Tedarikçi", "Yurtiçi", "Türkiye", "İzmir", "Hammadde", "Grup Dışı", "Çift Rol", "120.003", "Cem Aydın", "0232 877 45 45", "cem.aydin@egeboya.com", "Vade 60 Gün", "Uras Kimya", 360, "Eş kart 120.003.", F("Binder Grubu", "İnşaat ve Boya Binderleri", "", true));
                K("320.058", "Karadeniz Ambalaj (T)", "Karadeniz Ambalaj San. ve Tic. Ltd. Şti.", "5210056780", "Samsun", "Tedarikçi", "Yurtiçi", "Türkiye", "Samsun", "Yardımcı Malzemeler", "Grup Dışı", "Çift Rol", "120.004", "Nihat Yavuz", "0362 266 10 20", "info@karadenizambalaj.com", "Vade 60 Gün", "Uras Kimya", 300, "Eş kart 120.004.");
                // İdari İşler temsilcisi (Canan Su) olan kira / hizmet tedarikçisi — Ödeme Talebi kapsamı örneği (Data/OdemeTalebiModulu.cs)
                K("320.011", "Gençosman Gayrimenkul", "Gençosman Gayrimenkul Yatırım Ltd. Şti.", "3910045678", "Gebze", "Tedarikçi", "Yurtiçi", "Türkiye", "Kocaeli", "Kira / Gayrimenkul", "Grup Dışı", "Sadece Tedarikçi", "", "Hasan Gençosman", "0262 644 20 20", "info@gencosman.com.tr", "Peşin", "Alv Kimya", 420, "Ofis ve depo kirası; İdari İşler temsilcisi Canan Su.", F("Hizmetler", "Kira / Gayrimenkul", "Canan Su", true));
                // ---- Diğer tipler
                K("102.001", "Ziraat Bankası", "T.C. Ziraat Bankası A.Ş.", "9980000001", "Büyük Mükellefler", "Finansal Kuruluşlar", "Yurtiçi", "Türkiye", "Kocaeli", "Banka", "Grup Dışı", "Diğer", "", "Şube Müdürü", "0262 641 00 00", "gebze@ziraatbank.com.tr", "", "Uras Kimya", 700, "");
                K("102.002", "Yapı Kredi Leasing", "Yapı Kredi Finansal Kiralama A.O.", "9370012345", "Büyük Mükellefler", "Finansal Kuruluşlar", "Yurtiçi", "Türkiye", "İstanbul", "Leasing", "Grup Dışı", "Diğer", "", "Portföy Yöneticisi", "0212 336 60 00", "leasing@ykb.com", "", "Uras Kimya", 400, "");
                K("360.001", "Gebze Vergi Dairesi", "Gebze Vergi Dairesi Müdürlüğü", "", "", "Resmi Kurumlar", "Yurtiçi", "Türkiye", "Kocaeli", "Vergi Dairesi", "Grup Dışı", "Diğer", "", "", "0262 642 00 00", "", "", "Uras Kimya", 700, "");
                K("360.002", "SGK Gebze", "Sosyal Güvenlik Kurumu Gebze Merkezi", "", "", "Resmi Kurumlar", "Yurtiçi", "Türkiye", "Kocaeli", "SGK", "Grup Dışı", "Diğer", "", "", "0262 643 00 00", "", "", "Uras Kimya", 700, "");
                K("335.001", "Ali Veli (Avans)", "Ali Veli — Personel Avans Carisi", "12345678901", "", "Personel Carileri", "Yurtiçi", "Türkiye", "Kocaeli", "Personel Avans", "Grup Dışı", "Diğer", "", "Ali Veli", "0532 111 22 33", "ali.veli@uraskimya.com", "", "Uras Kimya", 300, "");
                // ---- Talepler
                CariTalep T(int gunOnce, string talepEden, string dept, string sirket, string tur, string gerekce, CariKart k) { var z = b.AddDays(-gunOnce).AddHours(11); var t = new CariTalep { Id = ++_sonTalep, No = $"CT-{z:yyyy}-{_sonTalep:0000}", Tarih = z, TalepEden = talepEden, Departman = dept, Sirket = sirket, TalepTuru = tur, Gerekce = gerekce, Kart = k, GuncellemeZamani = z }; k.Sirket = sirket; int n = 0; foreach (var f in k.Faaliyetler) f.Sira = ++n; Log(t, z, talepEden, "STATUS", "", "Taslak", "Sistem"); _talepler.Insert(0, t); return t; }
                CariKart Y(string kisa, string unvan, string vkn, string vd, string tip, string bolge, string ulke, string sehir, string sinif, string iliski, string rol, string es, string yetkili, string tel, string mail, string vade, string not, params CariFaaliyet[] f) => new CariKart { KisaAd = kisa, KisaltmaliUnvan = kisa, ResmiUnvan = unvan, VergiNo = vkn, VergiDairesi = vd, KartTipi = tip, Bolge = bolge, Ulke = ulke, Sehir = sehir, Sinif = sinif, IliskiDurumu = iliski, Rol = rol, EsCariKodu = es, Yetkili = yetkili, Telefon = tel, Eposta = mail, VadeSarti = vade, Not = not, Faaliyetler = f.ToList() };
                // onaylanmış → kart açıldı
                var t1 = T(12, "Tolga Yaman", "Satış", "Uras Kimya", "Yeni Kart", "Yeni dijital baskı müşterisi; ilk sipariş alındı.", Y("Dijital Baskı Merkezi", "Dijital Baskı Merkezi Reklam San. Ltd. Şti.", "2950034567", "Kadıköy", "Müşteri", "Yurtiçi", "Türkiye", "İstanbul", "Ticari Müşteriler", "Grup Dışı", "Sadece Müşteri", "", "Serkan Öz", "0216 411 90 90", "serkan@dijitalbaskimerkezi.com", "Vade 30 Gün", "", F("Boya Grubu", "Dijital", "Mehmet Kaya", true, "DTF", "DTF Ink", "Film Series", "DTF INK WHITE", 12), F("Baskı Makineleri", "Dijital", "Ali Yılmaz", false, "", "", "", "", 12)));
                var s1 = OnayaGonder(t1, "Tolga Yaman", t1.Tarih.AddHours(1)); Onayla(t1, "Onayla", "Vergi levhası ve ticaret sicil kontrol edildi.", "Muhasebe (Cari Açılış Onayı)", "Nazlı Erdem", t1.Tarih.AddHours(20));
                // onay bekliyor (grup içi → direktör adımı)
                var t2 = T(3, "Selin Kara", "Satış", "Uras Kimya", "Yeni Kart", "Uras Power'ın emprime boya ihtiyacı için grup içi müşteri kartı.", Y("Uras Power", "Uras Power Enerji Üretim A.Ş.", "8930067890", "Gebze", "Müşteri", "Yurtiçi", "Türkiye", "Kocaeli", "Özel Takip", "Grup İçi", "Sadece Müşteri", "", "Mert Doğan", "0262 751 00 30", "muhasebe@uraspower.com", "Vade 30 Gün", "", F("Boya Grubu", "Emprime", "Selin Kara", true, "", "", "", "", 3)));
                OnayaGonder(t2, "Selin Kara", t2.Tarih.AddHours(1)); Onayla(t2, "Onayla", "Grup şirketi; vergi bilgileri doğrulandı.", "Muhasebe (Cari Açılış Onayı)", "Nazlı Erdem", t2.Tarih.AddHours(6));
                // onay bekliyor (tedarikçi, yurtdışı)
                var t3 = T(2, "Emre Taş", "Satın Alma", "Selvi", "Yeni Kart", "Süblime kağıt hammaddesi için yeni yurtdışı tedarikçi; numune onaylandı.", Y("Nordic Paper AB", "Nordic Paper Sublimation AB", "SE556677889901", "", "Tedarikçi", "Yurtdışı", "Hollanda", "Rotterdam", "Hammadde", "Grup Dışı", "Sadece Tedarikçi", "", "Lars Eriksson", "+31 10 555 11 22", "sales@nordicpaper.eu", "Vesaik Mukabili", "", F("Kağıt Grubu", "Süblime Kağıt", "", true, "Süblimasyon", "", "", "", 2)));
                OnayaGonder(t3, "Emre Taş", t3.Tarih.AddHours(2));
                // taslak (çift rol, eş cari)
                T(1, "Gizem Tan", "Satış", "Uras Kimya", "Yeni Kart", "Korozo'nun kağıt grubu alımı başlıyor; tedarikçi kartı 320.003 ile eşleşecek.", Y("Korozo Ambalaj (M)", "Korozo Ambalaj San. ve Tic. A.Ş.", "5780012030", "Esenyurt", "Müşteri", "Yurtiçi", "Türkiye", "İstanbul", "Bayi", "Grup Dışı", "Çift Rol", "320.003", "Sibel Koç", "0212 866 00 00", "satis@korozo.com.tr", "Vade 60 Gün", "", F("Kağıt Grubu", "Süblime Kağıt", "Gizem Tan", true, "", "", "", "", 1)));
                // reddedildi
                var t5 = T(8, "Burcu Aydın", "Satın Alma", "Avrupa Paper", "Yeni Kart", "Fason kaplama hizmeti.", Y("Hızlı Fason", "Hızlı Fason Kaplama", "", "", "Tedarikçi", "Yurtiçi", "Türkiye", "Tekirdağ", "Fason", "Grup Dışı", "Sadece Tedarikçi", "", "Ahmet Hızlı", "0532 999 00 11", "", "Peşin", "", F("Kağıt Grubu", "Süblime Kağıt", "", true, "", "", "", "", 8)));
                t5.Kart.VergiNo = "1230098765"; t5.Kart.VergiDairesi = "Çorlu"; OnayaGonder(t5, "Burcu Aydın", t5.Tarih.AddHours(1)); Onayla(t5, "Reddet", "Vergi levhası ve imza sirküleri eksik; belgeler tamamlanınca yeniden gönderilsin.", "Muhasebe (Cari Açılış Onayı)", "Nazlı Erdem", t5.Tarih.AddHours(30));
                _talepler.Sort((x, y) => y.Id.CompareTo(x.Id));
            }
        }
    }
}
