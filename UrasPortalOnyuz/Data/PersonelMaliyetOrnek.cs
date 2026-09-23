// ============================================================
// PERSONEL MALİYET RAPORU — örnek veri, doğrulama ve hesaplama (Yönetim › Özel Raporlar ve İK › Personel Maliyet ve Bordro)
// Kaynak: "PERSONEL 19.06 GÜNCEL" Google E-Tablosu (2026). Sütun düzeni tabloyla aynıdır:
//   Personel Kimlik Bilgileri : Sıra, SGK İşvereni, Fiili Görev Yeri, Ad Soyad, Ünvan, Bölüm, Görev, İşe Giriş, Çatı Departman, Özel Durumu
//   Brüt Ücret                : 2026 X-Net, 2026 R-Net, 2026 Top. Net, Brüt Üc. Masrafları, Toplam Brüt Ücret
//   İşveren Payları           : Top. İşv. Payı            · Ek Çalışmalar : Top. Ek Çalışma
//   Sosyal Yardımlar          : Yemek, Yol, Servis, Kira Desteği, Eğitim, Top. Sosyal Yrd.
//   Prim                      : Satış Primi, Özel Ödeme, Top. Prim   · Yan Haklar : Top. Yan Haklar · Yasal Ödemeler : Top. Yasal Öd.
//   Maliyet                   : 2026 Genel Maliyet        · Not
// Personel Cari Kartı: her personelin 335.xxx cari kodu (CARİ_MASTER "Personel Carileri" ön eki), durum (Aktif / İşten Ayrıldı), ayrılış tarihi,
//   TC kimlik no, telefon ve e-posta alanları kart çekmecesinden düzenlenir. Kimlik ve tutar alanlarının tamamı ekrandan düzenlenir, personel
//   eklenir / silinir (bellek içi).
// Hesap: Top. Net = X-Net + R-Net · Toplam Brüt Ücret = Top. Net + Brüt Üc. Masrafları · Top. Sosyal Yrd. = Yemek + Yol + Servis + Kira + Eğitim
//        Top. Prim = Satış Primi + Özel Ödeme · Genel Maliyet = Toplam Brüt Ücret + Top. İşv. Payı + Top. Ek Çalışma + Top. Sosyal Yrd. + Top. Prim + Top. Yan Haklar + Top. Yasal Öd.
// Ekran: Controllers/PersonelMaliyetController.cs + Views/PersonelMaliyet/Index.cshtml + wwwroot/js/personel-maliyet.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class PmPersonel
    {
        public int Sira { get; set; }
        public string CariKodu => "335." + Sira.ToString("000");
        public string SgkIsvereni { get; set; }
        public string FiiliGorevYeri { get; set; }
        public string AdSoyad { get; set; }
        public string Unvan { get; set; }
        public string Bolum { get; set; }
        public string Gorev { get; set; }
        public DateTime? IseGiris { get; set; }
        public string CatiDepartman { get; set; }
        public string OzelDurum { get; set; } = "";
        // ---- personel cari kartı ek alanları
        public string Durum { get; set; } = "Aktif";        // Aktif | İşten Ayrıldı
        public DateTime? AyrilisTarihi { get; set; }
        public string TcKimlik { get; set; } = "";
        public string Telefon { get; set; } = "";
        public string Eposta { get; set; } = "";
        // ---- girilen tutarlar (aylık, TL)
        public decimal XNet { get; set; }
        public decimal RNet { get; set; }
        public decimal BrutUcMasraflari { get; set; }
        public decimal TopIsvPayi { get; set; }
        public decimal TopEkCalisma { get; set; }
        public decimal Yemek { get; set; }
        public decimal Yol { get; set; }
        public decimal Servis { get; set; }
        public decimal KiraDestegi { get; set; }
        public decimal Egitim { get; set; }
        public decimal SatisPrimi { get; set; }
        public decimal OzelOdeme { get; set; }
        public decimal TopYanHaklar { get; set; }
        public decimal TopYasalOd { get; set; }
        public string Not { get; set; } = "";
        // ---- hesaplanan
        public decimal TopNet => XNet + RNet;
        public decimal ToplamBrutUcret => TopNet + BrutUcMasraflari;
        public decimal TopSosyal => Yemek + Yol + Servis + KiraDestegi + Egitim;
        public decimal TopPrim => SatisPrimi + OzelOdeme;
        public decimal GenelMaliyet => ToplamBrutUcret + TopIsvPayi + TopEkCalisma + TopSosyal + TopPrim + TopYanHaklar + TopYasalOd;
        /// <summary>Kıdem (yıl): işe girişten bugüne, ayrılanlarda ayrılış tarihine; planlanmış (gelecek tarihli) işe girişte 0.</summary>
        public decimal? KidemYil => IseGiris.HasValue ? Math.Max(0m, Math.Round((decimal)((AyrilisTarihi ?? DateTime.Today) - IseGiris.Value.Date).TotalDays / 365.25m, 1)) : (decimal?)null;
    }

    /// <summary>Ekrandan gelen personel satırı (tablo düzenlemesi, kart çekmecesi ve yeni personel için aynı yapı).</summary>
    public class PmDuzenleme
    {
        public int Sira { get; set; }
        public string SgkIsvereni { get; set; } public string FiiliGorevYeri { get; set; } public string AdSoyad { get; set; } public string Unvan { get; set; } public string Bolum { get; set; } public string Gorev { get; set; }
        public string IseGiris { get; set; } public string CatiDepartman { get; set; } public string OzelDurum { get; set; }
        public string Durum { get; set; } public string AyrilisTarihi { get; set; } public string TcKimlik { get; set; } public string Telefon { get; set; } public string Eposta { get; set; }
        public decimal XNet { get; set; } public decimal RNet { get; set; } public decimal BrutUcMasraflari { get; set; } public decimal TopIsvPayi { get; set; } public decimal TopEkCalisma { get; set; }
        public decimal Yemek { get; set; } public decimal Yol { get; set; } public decimal Servis { get; set; } public decimal KiraDestegi { get; set; } public decimal Egitim { get; set; }
        public decimal SatisPrimi { get; set; } public decimal OzelOdeme { get; set; } public decimal TopYanHaklar { get; set; } public decimal TopYasalOd { get; set; }
        public string Not { get; set; }
    }

    public static class PersonelMaliyetOrnek
    {
        public static readonly object Kilit = new object();
        public static readonly string Kaynak = "Personel 19.06 Güncel";
        public static (string Kisi, DateTime Tarih)? SonKayit { get; private set; }

        public static readonly (string Grup, string[] Sutunlar)[] SutunGruplari =
        {
            ("Personel Kimlik Bilgileri", new[] { "Sıra", "SGK İşvereni", "Fiili Görev Yeri", "Ad Soyad", "Ünvan", "Bölüm", "Görev", "İşe Giriş", "Kıdem (Yıl)", "Çatı Departman", "Özel Durumu" }),
            ("Brüt Ücret", new[] { "X-Net", "R-Net", "Top. Net", "Brüt Üc. Masrafları", "Toplam Brüt Ücret" }),
            ("İşveren Payları", new[] { "Top. İşv. Payı" }), ("Ek Çalışmalar", new[] { "Top. Ek Çalışma" }),
            ("Sosyal Yardımlar", new[] { "Yemek", "Yol", "Servis", "Kira Desteği", "Eğitim", "Top. Sosyal Yrd." }),
            ("Prim", new[] { "Satış Primi", "Özel Ödeme", "Top. Prim" }), ("Yan Haklar", new[] { "Top. Yan Haklar" }), ("Yasal Ödemeler", new[] { "Top. Yasal Öd." }),
            ("Maliyet", new[] { "Genel Maliyet" }), ("Not", new[] { "Not" }), ("Kart", new[] { "Personel Kartı" })
        };
        public static readonly string[] OzelDurumlar = { "Emekli", "Engelli" };
        public static readonly string[] Durumlar = { "Aktif", "İşten Ayrıldı" };

        private static readonly Lazy<List<PmPersonel>> _liste = new Lazy<List<PmPersonel>>(Tohumla);
        public static List<PmPersonel> Liste => _liste.Value;
        private static readonly StringComparer TrSira = StringComparer.Create(new CultureInfo("tr-TR"), true);
        private static string[] Ayrik(Func<PmPersonel, string> f) => Liste.Select(f).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x, TrSira).ToArray();
        public static string[] SgkIsverenleri => Ayrik(p => p.SgkIsvereni);
        public static string[] GorevYerleri => Ayrik(p => p.FiiliGorevYeri);
        public static string[] CatiDepartmanlar => Ayrik(p => p.CatiDepartman);
        public static string[] Bolumler => Ayrik(p => p.Bolum);
        public static string[] Unvanlar => Ayrik(p => p.Unvan);
        public static string[] Gorevler => Ayrik(p => p.Gorev);

        private static readonly string[] TarihBicimleri = { "yyyy-MM-dd", "dd.MM.yyyy", "d.MM.yyyy", "dd.M.yyyy", "d.M.yyyy" };
        private static DateTime? Tarih(string s) => DateTime.TryParseExact((s ?? "").Trim(), TarihBicimleri, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : (DateTime?)null;
        private static string Temiz(string s) => (s ?? "").Trim();

        /// <summary>Satır doğrulaması; hata metni döner (null = geçerli).</summary>
        private static string Dogrula(PmDuzenleme d, string kim)
        {
            if (string.IsNullOrWhiteSpace(d.AdSoyad)) return $"{kim}: Ad Soyad zorunludur.";
            if (string.IsNullOrWhiteSpace(d.SgkIsvereni)) return $"{kim}: SGK İşvereni zorunludur.";
            if (string.IsNullOrWhiteSpace(d.FiiliGorevYeri)) return $"{kim}: Fiili Görev Yeri zorunludur.";
            if (string.IsNullOrWhiteSpace(d.Unvan)) return $"{kim}: Ünvan zorunludur.";
            if (string.IsNullOrWhiteSpace(d.Bolum)) return $"{kim}: Bölüm zorunludur.";
            if (string.IsNullOrWhiteSpace(d.Gorev)) return $"{kim}: Görev zorunludur.";
            if (string.IsNullOrWhiteSpace(d.CatiDepartman)) return $"{kim}: Çatı Departman zorunludur.";
            if (!string.IsNullOrWhiteSpace(d.IseGiris) && Tarih(d.IseGiris) == null) return $"{kim}: İşe Giriş tarihi geçersiz.";
            if (!string.IsNullOrWhiteSpace(d.OzelDurum) && !OzelDurumlar.Contains(d.OzelDurum.Trim())) return $"{kim}: Özel Durumu yalnız {string.Join(" / ", OzelDurumlar)} olabilir.";
            string durum = string.IsNullOrWhiteSpace(d.Durum) ? "Aktif" : d.Durum.Trim();
            if (!Durumlar.Contains(durum)) return $"{kim}: Durum yalnız {string.Join(" / ", Durumlar)} olabilir.";
            if (durum == "İşten Ayrıldı")
            {
                if (string.IsNullOrWhiteSpace(d.AyrilisTarihi)) return $"{kim}: İşten Ayrıldı için ayrılış tarihi zorunludur.";
                var at = Tarih(d.AyrilisTarihi); if (at == null) return $"{kim}: ayrılış tarihi geçersiz.";
                if (at > DateTime.Today) return $"{kim}: ayrılış tarihi bugünden sonra olamaz.";
                if (Tarih(d.IseGiris) != null && at < Tarih(d.IseGiris)) return $"{kim}: ayrılış tarihi işe giriş tarihinden önce olamaz.";
            }
            string tc = Temiz(d.TcKimlik); if (tc.Length > 0 && (tc.Length != 11 || !tc.All(char.IsDigit))) return $"{kim}: TC Kimlik No 11 haneli rakam olmalıdır.";
            string ep = Temiz(d.Eposta); if (ep.Length > 0 && (!ep.Contains("@") || !ep.Contains("."))) return $"{kim}: e-posta biçimi geçersiz.";
            foreach (var v in new[] { d.XNet, d.RNet, d.BrutUcMasraflari, d.TopIsvPayi, d.TopEkCalisma, d.Yemek, d.Yol, d.Servis, d.KiraDestegi, d.Egitim, d.SatisPrimi, d.OzelOdeme, d.TopYanHaklar, d.TopYasalOd })
                if (v < 0) return $"{kim}: tutarlar negatif olamaz.";
            return null;
        }
        private static void Uygula(PmPersonel p, PmDuzenleme d)
        {
            p.AdSoyad = Temiz(d.AdSoyad); p.SgkIsvereni = Temiz(d.SgkIsvereni); p.FiiliGorevYeri = Temiz(d.FiiliGorevYeri); p.Unvan = Temiz(d.Unvan); p.Bolum = Temiz(d.Bolum); p.Gorev = Temiz(d.Gorev);
            p.IseGiris = Tarih(d.IseGiris); p.CatiDepartman = Temiz(d.CatiDepartman); p.OzelDurum = Temiz(d.OzelDurum);
            p.Durum = string.IsNullOrWhiteSpace(d.Durum) ? "Aktif" : d.Durum.Trim(); p.AyrilisTarihi = p.Durum == "İşten Ayrıldı" ? Tarih(d.AyrilisTarihi) : null;
            p.TcKimlik = Temiz(d.TcKimlik); p.Telefon = Temiz(d.Telefon); p.Eposta = Temiz(d.Eposta);
            p.XNet = d.XNet; p.RNet = d.RNet; p.BrutUcMasraflari = d.BrutUcMasraflari; p.TopIsvPayi = d.TopIsvPayi; p.TopEkCalisma = d.TopEkCalisma;
            p.Yemek = d.Yemek; p.Yol = d.Yol; p.Servis = d.Servis; p.KiraDestegi = d.KiraDestegi; p.Egitim = d.Egitim;
            p.SatisPrimi = d.SatisPrimi; p.OzelOdeme = d.OzelOdeme; p.TopYanHaklar = d.TopYanHaklar; p.TopYasalOd = d.TopYasalOd; p.Not = Temiz(d.Not);
        }

        /// <summary>Düzenlenen satırları uygular (önce hepsi doğrulanır); hata metni döner (null = başarılı).</summary>
        public static string Kaydet(List<PmDuzenleme> satirlar, string kisi)
        {
            if (satirlar == null || satirlar.Count == 0) return "Kaydedilecek değişiklik yok.";
            lock (Kilit)
            {
                // önce hepsi doğrulanır (mükerrer sıra, zorunlu alanlar, aynı adlı ikinci aktif personel), sonra uygulanır
                var gorulenSira = new HashSet<int>(); var gorulenAd = new HashSet<string>(TrSira);
                var gelenSiralar = new HashSet<int>(satirlar.Select(s => s.Sira));
                foreach (var d in satirlar)
                {
                    if (!gorulenSira.Add(d.Sira)) return $"Sıra {d.Sira} istekte birden fazla kez geçiyor.";
                    var p = Liste.FirstOrDefault(x => x.Sira == d.Sira); if (p == null) return $"Sıra {d.Sira} bulunamadı.";
                    var h = Dogrula(d, $"Sıra {d.Sira} ({p.AdSoyad})"); if (h != null) return h;
                    string ad = Temiz(d.AdSoyad), durum = string.IsNullOrWhiteSpace(d.Durum) ? "Aktif" : d.Durum.Trim();
                    if (durum == "Aktif")
                    {
                        if (!gorulenAd.Add(ad)) return $"Sıra {d.Sira}: {ad} adı istekte birden fazla aktif satırda geçiyor.";
                        if (Liste.Any(x => x.Sira != d.Sira && !gelenSiralar.Contains(x.Sira) && x.Durum == "Aktif" && TrSira.Compare(x.AdSoyad, ad) == 0)) return $"Sıra {d.Sira}: {ad} adlı aktif bir personel zaten var.";
                    }
                }
                foreach (var d in satirlar) Uygula(Liste.First(x => x.Sira == d.Sira), d);
                SonKayit = (kisi, DateTime.Now);
            }
            return null;
        }

        /// <summary>Yeni personel kartı açar; hata metni döner (null = başarılı), kayıt out parametresiyle döner.</summary>
        public static string Ekle(PmDuzenleme d, string kisi, out PmPersonel kayit)
        {
            kayit = null; if (d == null) return "Veri yok.";
            var h = Dogrula(d, "Yeni personel"); if (h != null) return h;
            lock (Kilit)
            {
                if (Liste.Any(x => TrSira.Compare(x.AdSoyad, Temiz(d.AdSoyad)) == 0 && x.Durum == "Aktif")) return $"{Temiz(d.AdSoyad)} adlı aktif bir personel zaten var.";
                var p = new PmPersonel { Sira = (Liste.Count == 0 ? 0 : Liste.Max(x => x.Sira)) + 1 };
                Uygula(p, d); Liste.Add(p); kayit = p; SonKayit = (kisi, DateTime.Now);
            }
            return null;
        }

        /// <summary>Satırı kalıcı olarak siler (hatalı giriş için); işten ayrılanlar silinmez, Durum = İşten Ayrıldı yapılır.</summary>
        public static string Sil(int sira, string kisi)
        {
            lock (Kilit)
            {
                var p = Liste.FirstOrDefault(x => x.Sira == sira); if (p == null) return $"Sıra {sira} bulunamadı.";
                Liste.Remove(p); SonKayit = (kisi, DateTime.Now);
            }
            return null;
        }

        // ---------- Tohum: "PERSONEL 19.06 GÜNCEL" tablosu (boş hücreler 0) ----------
        private static PmPersonel P(int sira, string yer, string sgk, string ad, string unvan, string bolum, string gorev, string giris, string cati, string ozel, decimal x, decimal r = 0, decimal yemek = 0, decimal yol = 0, decimal prim = 0, decimal ozelOdeme = 0)
            => new PmPersonel
            {
                Sira = sira, FiiliGorevYeri = yer, SgkIsvereni = sgk, AdSoyad = ad, Unvan = unvan, Bolum = bolum, Gorev = gorev, CatiDepartman = cati, OzelDurum = ozel ?? "",
                IseGiris = Tarih(giris), XNet = x, RNet = r, Yemek = yemek, Yol = yol, SatisPrimi = prim, OzelOdeme = ozelOdeme
            };

        private static List<PmPersonel> Tohumla() => new List<PmPersonel>
        {
            P(1,  "Alv Kimya Çorlu", "ALV Kimya", "Arslan Çoşkun", "Personel", "İdari İşler", "Temizlik Görevlisi", "23.03.2019", "Kurumsal Hizmetler", "", 35000),
            P(2,  "Alv Kimya Çorlu", "ALV Kimya", "Burhan Evvel", "Personel", "İdari İşler", "Temizlik Görevlisi", "30.11.2020", "Kurumsal Hizmetler", "", 35000),
            P(3,  "ALV Kimya İst.", "ALV Kimya", "Dilek Güden", "Yönetici", "Muhasebe", "Muhasebe Yöneticisi", "01.09.2022", "Mali İşler", "", 90000, 0, 3300),
            P(4,  "Alv Kimya Çorlu", "ALV Kimya", "Emre Can Yavuz", "Uzman", "Üretim", "Otomasyon Uzmanı", "18.10.2021", "Üretim", "", 54000, 0, 9100),
            P(5,  "ALV Kimya İst.", "ALV Kimya", "Eser Şenürek", "Uzman", "Ar-Ge", "Ar-Ge Uzmanı", "11.06.2019", "Üretim", "", 70000, 0, 3300),
            P(6,  "Alv Kimya Çorlu", "ALV Kimya", "Görkem Akbulut", "Uzman", "Ar-Ge", "Ar-Ge Uzmanı", "04.07.2024", "Üretim", "", 47000),
            P(7,  "ALV Kimya İst.", "ALV Kimya", "Nevruz Sarıtaş", "Uzman", "Muhasebe", "Muhasebe Uzmanı", "25.10.2021", "Mali İşler", "", 52000, 0, 3300),
            P(8,  "Alv Kimya Çorlu", "ALV Kimya", "Özgür Pamir", "Direktör", "Üretim", "Dijital Üretim Direktörü", "03.08.2016", "Üretim", "", 140000, 100000),
            P(9,  "Alv Kimya Çorlu", "ALV Kimya", "Savaş Özcan", "Uzman", "Üretim", "Üretim ve Proses Kontrol Sorumlusu", "02.04.2019", "Üretim", "", 68000, 0, 25000),
            P(10, "Alv Kimya Çorlu", "ALV Kimya", "Semih Evirgen", "Yönetici", "Kalite Güvence", "Uyumluluk ve Çevre Yöneticisi", "04.01.2021", "Üretim", "", 65000),
            P(11, "Alv Kimya Çorlu", "ALV Kimya", "Soner İkizoğlu", "Teknisyen", "Teknik Hizmetler", "Elektrik Teknisyeni", "04.07.2024", "Satış ve Pazarlama", "", 58500),
            P(12, "Alv Kimya Çorlu", "ALV Kimya", "Şirin Tunç", "Personel", "İdari İşler", "Servis Personeli", "24.06.2024", "Kurumsal Hizmetler", "", 32000),
            P(13, "Alv Kimya Çorlu", "ALV Kimya", "Ümit Özdemir", "Uzman", "Depo ve Sevkiyat", "Sevkiyat Uzmanı", "20.11.2024", "Tedarik Zinciri ve Operasyonlar", "Emekli", 50733, 4267),
            P(14, "Urs Makine", "Asia", "Mesut Akıncı", "Uzman", "Satış", "İç Piyasa Satış Uzmanı", "16.02.2022", "Satış ve Pazarlama", "", 37000, 0, 7500, 0, 13302, 15000),
            P(15, "ALV Kimya İst.", "Uras Kimya", "Berkan Özkan", "Müdür", "İhracat", "İhracat Müdürü", "01.08.2022", "Satış ve Pazarlama", "", 88000, 0, 3300),
            P(16, "ALV Kimya İst.", "Uras Kimya", "Yusuf Öztürk", "Uzman", "Satış", "İç Piyasa Satış Uzmanı – Emprime", "20.08.2022", "Satış ve Pazarlama", "", 31000, 0, 7500, 0, 37249),
            P(17, "Uras Kimya", "Uras Kimya", "Burak Kaya", "Personel", "İdari İşler", "Finans Ulaşım", "01.07.2025", "Kurumsal Hizmetler", "", 53000, 0, 3300),
            P(18, "ALV Kimya İst.", "Uras Kimya", "Cafer İpek", "Müdür", "Teknik Hizmetler", "İnşaat Mühendisi", "03.05.2025", "Satış ve Pazarlama", "Emekli", 114667, 5333),
            P(19, "ALV Kimya İst.", "Uras Kimya", "Berk Yenilmez", "Uzman", "Üretim", "Üretim Planlama Uzmanı", "01.07.2025", "Üretim", "", 65000, 0, 3300),
            P(20, "ALV Kimya İst.", "Uras Kimya", "Mustafa Budak", "Uzman", "İhracat", "İhracat Operasyon Uzmanı", "04.08.2025", "Tedarik Zinciri ve Operasyonlar", "", 58500, 0, 3300),
            P(21, "Uras Kimya", "Uras Kimya", "Özge İsen", "Kıdemli Uzman", "Üst Yönetim", "Proje Kıdemli Uzmanı", "24.11.2023", "Genel Yönetim", "", 60000, 0, 5000),
            P(22, "Uras Kimya", "URS Makine", "Joao Paulo Araujo Fernandes", "Genel Müdür", "Üst Yönetim", "Genel Müdür", "11.02.2023", "Genel Yönetim", "", 160000, 90000),
            P(23, "Selvi Kimya", "Selvi Kimya", "Birol Şeker", "Uzman", "Depo ve Sevkiyat", "Mal Kabul Uzmanı", "18.05.2019", "Tedarik Zinciri ve Operasyonlar", "", 52000, 0, 3300),
            P(24, "Selvi Kimya", "Selvi Kimya", "Beyza Gizem Zazoğlu", "Uzman", "Muhasebe", "Muhasebe Uzmanı", "28.08.2024", "Mali İşler", "", 68000, 0, 3300),
            P(25, "Selvi Kimya", "Selvi Kimya", "Seda Saraç", "Personel", "İdari İşler", "Servis Personeli", "", "Kurumsal Hizmetler", "", 30000, 0, 3300),
            P(26, "Uras Kimya", "Uras Kimya", "Ahmet Özkan", "Uzman", "Finans", "Finans Uzmanı", "02.10.2024", "Mali İşler", "", 75000, 0, 3300),
            P(27, "Alv Kimya İst.", "Alv Kimya İst.", "Aksel Güler", "Yönetici", "İhracat", "İhracat Operasyon Yöneticisi", "01.03.2022", "Tedarik Zinciri ve Operasyonlar", "", 85000, 0, 3300),
            P(28, "Uras Kimya", "Uras Kimya", "Ayfer Şahin", "Yönetici", "Muhasebe", "Muhasebe Yöneticisi", "01.04.2023", "Mali İşler", "", 90000, 0, 3300),
            P(29, "Uras Kimya", "Uras Kimya", "Ayşegül Zent Ula", "Uzman", "Cari İşler", "Cari Hesap Uzmanı", "11.12.2023", "Mali İşler", "Emekli", 46733, 4267, 3300),
            P(30, "Uras Kimya", "Uras Kimya", "Bahar Seyrekoğlu", "Uzman Yardımcısı", "İdari İşler", "Danışma Personeli", "04.07.2023", "Kurumsal Hizmetler", "", 40000, 0, 3300),
            P(31, "Uras Kimya", "Uras Kimya", "Bedirhan Genç", "Kıdemli Uzman", "Kalite Güvence", "Kalite Sistem Kıdemli Uzmanı", "24.11.2025", "Üretim", "", 78500, 0, 3300),
            P(32, "Uras Kimya", "Uras Kimya", "Berk Berberoğlu", "Uzman", "Finans", "Finans Uzmanı", "25.09.2023", "Mali İşler", "", 65000, 0, 3300),
            P(33, "Alv Kimya İst.", "Uras Kimya", "Burhan Gün", "Personel", "İdari İşler", "Sevkiyat Şoförü", "29.11.2021", "Kurumsal Hizmetler", "", 46000),
            P(34, "Uras Kimya", "Uras Kimya", "Can Gürsel", "Uzman", "Depo ve Sevkiyat", "Sevkiyat Uzmanı", "15.08.2022", "Tedarik Zinciri ve Operasyonlar", "", 37000, 0, 3300),
            P(35, "Alv Kimya Çorlu", "Uras Kimya", "Dilber Güney", "Yönetici", "İnsan Kaynakları", "Bordro ve Özlük İşleri Yöneticisi", "23.11.2020", "Kurumsal Hizmetler", "", 80000, 0, 3300),
            P(36, "Alv Kimya İst.", "Uras Kimya", "Dinçer Özdemir", "Personel", "İdari İşler", "Sevkiyat Şoförü", "24.03.2023", "Kurumsal Hizmetler", "Emekli", 41733, 4267, 733),
            P(37, "Uras Kimya", "Uras Kimya", "Eda Bozkurt", "Uzman", "Satınalma", "İthalat Uzmanı", "09.05.2022", "Tedarik Zinciri ve Operasyonlar", "", 67000, 0, 1100),
            P(38, "Alv Kimya İst.", "Uras Kimya", "Efe Akçakale", "Uzman Yardımcısı", "Teknik Hizmetler", "Teknik Servis Personeli", "24.06.2024", "Satış ve Pazarlama", "", 35000, 0, 7500, 3300),
            P(39, "Uras Kimya", "Uras Kimya", "Emrah Ateş", "Uzman", "Depo ve Sevkiyat", "Sevkiyat Uzmanı", "01.06.2022", "Tedarik Zinciri ve Operasyonlar", "", 50000, 0, 3300),
            P(40, "Uras Kimya", "Uras Kimya", "Emre Keskin", "Uzman", "Teknik Hizmetler", "Teknik Servis Renk Uzmanı", "21.03.2019", "Satış ve Pazarlama", "", 70000, 0, 7500),
            P(41, "Uras Kimya", "Uras Kimya", "Erdoğan Polat", "Uzman", "Satış", "İç Piyasa Satış Uzmanı – Emprime", "31.08.2023", "Satış ve Pazarlama", "Emekli", 26867, 4133, 7500, 0, 109713),
            P(42, "Uras Kimya", "Uras Kimya", "Ezgi Kara", "Kıdemli Uzman", "Kalite Kontrol", "Kalite Kontrol Kıdemli Uzmanı", "07.05.2019", "Üretim", "", 52000, 0, 3300),
            P(43, "Uras Kimya", "Uras Kimya", "Hacı Mustafa Korkmaz", "Uzman", "Ar-Ge", "Ar-Ge Uzmanı", "04.07.2022", "Üretim", "", 55000, 0, 3300),
            P(44, "Uras Kimya", "Uras Kimya", "Hakan Haydut", "Uzman", "Muhasebe", "Muhasebe Uzmanı", "22.07.2024", "Mali İşler", "", 55000, 0, 3300),
            P(45, "Uras Kimya", "Uras Kimya", "İdil Akel", "Uzman", "İnsan Kaynakları", "İnsan Kaynakları Uzmanı", "16.06.2025", "Kurumsal Hizmetler", "", 55000, 0, 3300),
            P(46, "Alv Kimya İst.", "Uras Kimya", "İsmail Duman", "Personel", "İdari İşler", "Sevkiyat Şoförü", "23.01.2024", "Kurumsal Hizmetler", "Emekli", 41733, 4267),
            P(47, "Uras Kimya", "Uras Kimya", "Melek Demir", "Personel", "İdari İşler", "Temizlik Görevlisi", "19.02.2024", "Kurumsal Hizmetler", "Engelli", 28100),
            P(48, "Uras Kimya", "Uras Kimya", "Melike Ural Kaya", "Uzman", "Cari İşler", "Cari Hesap Uzmanı", "07.11.2022", "Mali İşler", "", 50000, 0, 3300),
            P(49, "Uras Kimya", "Uras Kimya", "Mustafa Özdeniz", "Uzman", "Teknik Hizmetler", "Teknik Servis Personeli", "21.03.2019", "Satış ve Pazarlama", "", 60000, 0, 7500),
            P(50, "Alv Kimya İst.", "Uras Kimya", "Mustafa Ulaş Erikli", "Uzman", "Depo ve Sevkiyat", "Sevkiyat Uzmanı", "20.02.2023", "Tedarik Zinciri ve Operasyonlar", "Emekli", 38867, 4133, 3300),
            P(51, "Uras Kimya", "Uras Kimya", "Müjgan Yetgin", "Uzman", "Satış", "İç Piyasa Satış Uzmanı – Emprime", "15.04.2022", "Satış ve Pazarlama", "Emekli", 26867, 4133, 7500, 0, 20970),
            P(52, "Alv Kimya İst.", "Uras Kimya", "Neslihan Uludağ", "Direktör", "Üst Yönetim", "YK Üyesi", "29.03.2023", "Genel Yönetim", "Emekli", 384667, 5333),
            P(53, "Uras Kimya", "Uras Kimya", "Olgun Küskü", "Uzman", "Kalite Kontrol", "Kalite Kontrol Uzmanı", "19.08.2020", "Üretim", "", 40500, 0, 3300),
            P(54, "Uras Kimya", "Uras Kimya", "Ömer Kantar", "Uzman", "Teknik Hizmetler", "Teknik Servis Personeli", "04.12.2020", "Satış ve Pazarlama", "", 60000, 0, 7500),
            P(55, "Uras Kimya", "Uras Kimya", "Ömer Yıldırım", "Uzman", "Teknik Hizmetler", "Teknik Servis Personeli", "05.10.2020", "Satış ve Pazarlama", "", 60000, 0, 7500, 3300),
            P(56, "Alv Kimya İst.", "Uras Kimya", "Rabiye Doğan", "Personel", "İdari İşler", "Temizlik Görevlisi", "22.01.2024", "Kurumsal Hizmetler", "Emekli", 30733, 4267, 3300),
            P(57, "Uras Kimya", "Uras Kimya", "Refik Polat", "Yönetici", "Depo ve Sevkiyat", "Depo Yöneticisi", "29.03.2023", "Tedarik Zinciri ve Operasyonlar", "Emekli", 70867, 4133),
            P(58, "Alv Kimya Çorlu", "ALV Kimya", "Selçuk Mert", "Müdür", "Üretim", "Üretim Müdürü", "06.09.2024", "Üretim", "Emekli", 129667, 5333),
            P(59, "Uras Kimya", "Uras Kimya", "Serhat Mutlu", "Uzman", "Satış", "İç Piyasa Satış Uzmanı – Dijital", "21.03.2019", "Satış ve Pazarlama", "", 60000, 0, 7500, 0, 28872),
            P(60, "Uras Kimya", "Uras Kimya", "Serkan Genç", "Yönetici", "Satınalma", "Satınalma Yöneticisi", "24.01.2023", "Tedarik Zinciri ve Operasyonlar", "", 85000, 0, 3300),
            P(61, "Uras Kimya", "Uras Kimya", "Sevgi Kaya", "Müdür", "İhracat", "İhracat Müdürü", "19.11.2022", "Satış ve Pazarlama", "", 110000, 0, 3300, 0, 27320),
            P(62, "Uras Kimya", "Uras Kimya", "Vu Thi Thuy Ha", "Kıdemli Uzman", "Satınalma", "Satınalma Kıdemli Uzmanı", "08.03.2023", "Tedarik Zinciri ve Operasyonlar", "", 50000, 50000),
            P(63, "Alv Kimya İst.", "Uras Kimya", "Yalçın Avcı", "Uzman", "Depo ve Sevkiyat", "Stok Kontrol Uzmanı", "18.10.2021", "Tedarik Zinciri ve Operasyonlar", "", 45000, 0, 3300),
            P(64, "Uras Kimya", "Uras Kimya", "Yeşim Karakütük", "Personel", "İdari İşler", "Temizlik Görevlisi", "18.11.2024", "Kurumsal Hizmetler", "", 35000, 0, 3300),
            P(65, "Alv Kimya İst.", "Uras Kimya", "Burak Sergen Ada", "Uzman", "İdari İşler", "İdari İşler Uzmanı", "27.07.2026", "Kurumsal Hizmetler", "", 40000),
            P(66, "Alv Kimya İst.", "Uras Kimya", "Zahit Ercan", "Personel", "İdari İşler", "Temizlik Görevlisi", "01.09.2021", "Kurumsal Hizmetler", "Emekli", 30733, 4267, 3300),
            P(67, "Uras Kimya + ALV Kimya + Asia + Uras Holding", "Uras Holding", "Cüneyt Arda Ceylan", "Yönetici", "Hukuk", "Avukat", "21.03.2019", "Kurumsal Hizmetler", "", 70000)
        };
    }
}
