using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>
    /// BÜTÇE RAPORU örnek verisi — Mali İşler › Finans › Bütçe ve Planlama.
    /// Üç gider grubu ayrı izlenir: SABİT GİDERLER (personel, kira, sigorta, amortisman, lisans, finansman, vergi-aidat),
    /// TEDARİK ZİNCİRİ · HAMMADDE (hammadde, ticari mal, ambalaj, lojistik, ithalat, sarf) ve GENEL GİDERLER (enerji, bakım, danışmanlık,
    /// hukuk, seyahat, pazarlama, ofis, iletişim, hizmet, eğitim, banka, temsil, bağış). Her kalem için şirket × yıl bazında 12 aylık BÜTÇE ve
    /// kapanmış aylar için GERÇEKLEŞEN tutulur; içinde bulunulan ay "geçici" (ay kapanışı yapılmadı) olarak kısmi gerçekleşenle gelir.
    /// Tutarlar TL, KDV hariç. Bütçe = Uras Kimya ölçeğindeki aylık taban × şirket ölçeği × yıl çarpanı × mevsimsellik; gerçekleşen = bütçe ×
    /// kalem eğilimi × deterministik ±%6 sapma (şirket + kalem + yıl tohumu). Açık aylara bütçe revizyonu yapılabilir; her revizyon günlüğe yazılır.
    /// </summary>
    public class BtKalem
    {
        public string Kod { get; set; }           // SG-01, TZ-03, GG-07
        public string Grup { get; set; }          // Sabit Giderler | Tedarik Zinciri · Hammadde | Genel Giderler
        public string AltGrup { get; set; }       // Personel, Kira … / Hammadde, Ambalaj, Lojistik … / Enerji, Bakım …
        public string Ad { get; set; }
        public string HesapKodu { get; set; }     // muhasebe hesap kodu
        public string Sorumlu { get; set; }       // bütçe sahibi departman
        public string[] Sirketler { get; set; }   // null → tüm şirketler
        public decimal Taban { get; set; }        // Uras Kimya ölçeğinde aylık bütçe (2026, TL)
        public double[] Mevsim { get; set; }      // 12 aylık çarpan (null → düz)
        public double Egilim { get; set; } = 1.0; // gerçekleşen / bütçe eğilimi (1.18 → %18 aşım eğilimi)
        public string Not { get; set; }           // bütçe varsayımı
    }

    /// <summary>Şirket × yıl × kalem satırı: 12 aylık bütçe (ilk bütçe revizyon izi için saklanır) ve gerçekleşen (kapanmamış ay: null).</summary>
    public class BtSatir
    {
        public string Sirket { get; set; }
        public int Yil { get; set; }
        public string KalemKod { get; set; }
        public decimal[] Butce { get; set; } = new decimal[12];
        public decimal[] IlkButce { get; set; } = new decimal[12];
        public decimal?[] Gercek { get; set; } = new decimal?[12];
    }

    public class BtRevizyon
    {
        public int Id { get; set; } public DateTime Zaman { get; set; } public string Kullanici { get; set; }
        public string Sirket { get; set; } public int Yil { get; set; } public string KalemKod { get; set; } public string KalemAd { get; set; }
        public int Ay { get; set; } public decimal Onceki { get; set; } public decimal Yeni { get; set; } public string Gerekce { get; set; }
    }

    public class BtIstek { public string Sirket { get; set; } public int Yil { get; set; } public string KalemKod { get; set; } public int Ay { get; set; } public decimal? Tutar { get; set; } public string Gerekce { get; set; } }

    public static class ButceOrnek
    {
        public static readonly object Kilit = new object();
        public static readonly string[] Gruplar = { "Sabit Giderler", "Tedarik Zinciri · Hammadde", "Genel Giderler" };
        public static string[] Sirketler => TeklifOrnek.Sirketler;
        public static readonly int[] Yillar = { 2025, 2026 };
        public static readonly string[] Aylar = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        /// <summary>Şirket ölçeği (Uras Kimya = 1).</summary>
        public static readonly Dictionary<string, decimal> Olcek = new Dictionary<string, decimal> { ["Uras Kimya"] = 1.00m, ["Selvi"] = 0.32m, ["Avrupa Paper"] = 0.42m, ["Alv Kimya"] = 0.28m, ["Daf Kimya"] = 0.24m, ["URS Makine"] = 0.18m };
        /// <summary>Yıl çarpanı (2026 = 1; önceki yıl enflasyon etkisiyle daha düşük).</summary>
        public static readonly Dictionary<int, decimal> YilCarpani = new Dictionary<int, decimal> { [2025] = 0.74m, [2026] = 1.00m };
        /// <summary>Kapanmış (gerçekleşeni kesinleşmiş) ay sayısı: geçmiş yıl 12, içinde bulunulan yıl bir önceki ay.</summary>
        public static int KapanmisAy(int yil) => yil < DateTime.Today.Year ? 12 : yil > DateTime.Today.Year ? 0 : DateTime.Today.Month - 1;
        /// <summary>Geçici (ay kapanışı yapılmamış, kısmi gerçekleşen) ay numarası; yoksa 0.</summary>
        public static int GeciciAy(int yil) => yil == DateTime.Today.Year ? DateTime.Today.Month : 0;

        static readonly string[] URETICI = { "Uras Kimya", "Daf Kimya" };
        static readonly string[] SATICI = { "Selvi", "Alv Kimya", "Avrupa Paper" };
        static readonly string[] MAKINE = { "URS Makine" };
        static readonly string[] KAGIT = { "Uras Kimya", "Avrupa Paper" };
        static readonly string[] ITHALATCI = { "Uras Kimya", "Avrupa Paper", "Daf Kimya", "URS Makine" };
        static readonly string[] SANAYI = { "Uras Kimya", "Daf Kimya", "URS Makine" };
        static readonly double[] ZAM = { 1, 1, 1, 1, 1, 1, 1.22, 1.22, 1.22, 1.22, 1.22, 1.22 };            // Temmuz ücret zammı
        static readonly double[] HAMMADDE = { 0.9, 0.95, 1.1, 1.1, 1.05, 1.0, 0.9, 0.85, 1.1, 1.15, 1.05, 0.85 };
        static readonly double[] ELEKTRIK = { 1.05, 1.0, 1.0, 0.95, 1.0, 1.1, 1.2, 1.2, 1.05, 0.95, 0.95, 1.0 };
        static readonly double[] DOGALGAZ = { 1.35, 1.3, 1.1, 0.9, 0.8, 0.75, 0.8, 0.8, 0.85, 1.0, 1.2, 1.3 };
        static readonly double[] SEYAHAT = { 0.7, 0.9, 1.3, 1.1, 1.2, 1.0, 0.7, 0.6, 1.2, 1.4, 1.1, 0.7 };
        static readonly double[] PAZARLAMA = { 0.6, 0.8, 1.8, 0.9, 1.0, 0.8, 0.6, 0.5, 1.2, 1.9, 0.9, 0.7 };   // Mart / Ekim fuarları
        static readonly double[] BAKIM = { 0.8, 0.8, 0.9, 1.0, 1.0, 1.0, 1.6, 1.6, 1.0, 0.9, 0.9, 0.8 };      // yaz duruşu
        static readonly double[] NAVLUN = { 0.9, 0.9, 1.1, 1.1, 1.0, 1.0, 0.9, 0.8, 1.2, 1.2, 1.1, 0.9 };
        static readonly double[] EGITIM = { 0.5, 1.0, 1.5, 1.0, 1.0, 0.8, 0.3, 0.3, 1.5, 1.5, 1.0, 0.6 };
        static readonly double[] DANISMANLIK = { 1.0, 1.0, 1.5, 1.5, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.3 };
        static readonly double[] LISANS = { 1.6, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };
        static readonly double[] EMLAK = { 1, 1, 1, 1, 2.2, 1, 1, 1, 1, 1, 2.2, 1 };
        static readonly double[] YEMEK = { 1, 1, 1, 1, 1, 0.9, 0.85, 0.85, 1, 1, 1, 1 };

        static BtKalem K(string kod, string grup, string alt, string ad, string hesap, string sorumlu, decimal taban, double egilim = 1.0, double[] mevsim = null, string[] sirketler = null, string not = null)
            => new BtKalem { Kod = kod, Grup = grup, AltGrup = alt, Ad = ad, HesapKodu = hesap, Sorumlu = sorumlu, Taban = taban, Egilim = egilim, Mevsim = mevsim, Sirketler = sirketler, Not = not };

        public static readonly List<BtKalem> Kalemler = new List<BtKalem>
        {
            // ---------------- SABİT GİDERLER ----------------
            K("SG-01", Gruplar[0], "Personel", "Brüt Maaş ve Ücretler", "770.01.001", "İnsan Kaynakları", 9_500_000m, 0.99, ZAM, null, "Temmuz'da %22 ücret zammı varsayımı"),
            K("SG-02", Gruplar[0], "Personel", "SGK İşveren Payı", "770.01.002", "İnsan Kaynakları", 2_100_000m, 1.00, ZAM),
            K("SG-03", Gruplar[0], "Personel", "Kıdem ve İhbar Karşılıkları", "770.01.003", "İnsan Kaynakları", 420_000m, 1.15, null, null, "Ayrılış sayısı bütçe varsayımının üzerinde"),
            K("SG-04", Gruplar[0], "Personel", "Yemek ve Personel Servisi", "770.01.004", "İdari İşler", 880_000m, 1.05, YEMEK),
            K("SG-05", Gruplar[0], "Personel", "Özel Sağlık Sigortası", "770.01.005", "İnsan Kaynakları", 240_000m, 1.00),
            K("SG-06", Gruplar[0], "Kira", "Fabrika ve Depo Kirası", "770.02.001", "Mali İşler", 1_150_000m, 1.00, null, ITHALATCI, "Sözleşme TÜFE endeksli, Ocak artışı"),
            K("SG-07", Gruplar[0], "Kira", "Ofis Kirası", "770.02.002", "Mali İşler", 340_000m, 1.00),
            K("SG-08", Gruplar[0], "Kira", "Araç Kiralama (Filo)", "770.02.003", "İdari İşler", 610_000m, 1.03),
            K("SG-09", Gruplar[0], "Sigorta", "İşyeri, Mal ve Sorumluluk Sigortaları", "770.03.001", "Mali İşler", 290_000m, 1.02),
            K("SG-10", Gruplar[0], "Amortisman", "Amortisman ve İtfa Payları", "770.04.001", "Muhasebe", 1_800_000m, 1.00),
            K("SG-11", Gruplar[0], "Lisans ve Abonelik", "Yazılım Lisans ve Abonelikleri (ERP, Office, Güvenlik)", "770.05.001", "Bilgi Teknolojileri", 275_000m, 1.04, LISANS, null, "Yıllık yenilemeler Ocak'ta"),
            K("SG-12", Gruplar[0], "Finansman", "Kredi Faiz ve Banka Komisyonları", "780.01.001", "Finans", 1_450_000m, 1.08, null, null, "Politika faizi bütçe varsayımının üzerinde"),
            K("SG-13", Gruplar[0], "Vergi ve Aidat", "Emlak Vergisi, Aidat ve Harçlar", "770.06.001", "Muhasebe", 120_000m, 1.00, EMLAK, null, "Emlak vergisi Mayıs / Kasım taksitleri"),
            // ---------------- TEDARİK ZİNCİRİ · HAMMADDE ----------------
            K("TZ-01", Gruplar[1], "Hammadde", "Pigment ve Boyar Maddeler", "150.01.001", "Satın Alma", 14_000_000m, 1.12, HAMMADDE, URETICI, "USD bazlı alım; kur artışı sapmanın ana nedeni"),
            K("TZ-02", Gruplar[1], "Hammadde", "Binder ve Reçineler", "150.01.002", "Satın Alma", 9_200_000m, 1.05, HAMMADDE, URETICI),
            K("TZ-03", Gruplar[1], "Hammadde", "Kimyasal Yardımcı Maddeler", "150.01.003", "Satın Alma", 5_100_000m, 1.02, HAMMADDE, URETICI),
            K("TZ-04", Gruplar[1], "Hammadde", "Süblime Kağıt ve Karton", "150.01.004", "Satın Alma", 6_300_000m, 1.04, HAMMADDE, KAGIT),
            K("TZ-05", Gruplar[1], "Ticari Mal", "Ticari Mal Alımları (Grup İçi · Uras Kimya)", "153.01.001", "Satın Alma", 7_500_000m, 1.06, HAMMADDE, SATICI, "Yetkili satıcı alımları; satış hacmine bağlı"),
            K("TZ-06", Gruplar[1], "Hammadde", "Çelik ve Metal Malzeme", "150.02.001", "Satın Alma", 3_800_000m, 1.03, HAMMADDE, MAKINE),
            K("TZ-07", Gruplar[1], "Hammadde", "Elektrik / Elektronik Komponent", "150.02.002", "Satın Alma", 2_100_000m, 1.09, HAMMADDE, MAKINE, "İthal komponent, EUR bazlı"),
            K("TZ-08", Gruplar[1], "Ambalaj", "Ambalaj Malzemeleri (IBC, Bidon, Koli)", "150.03.001", "Satın Alma", 2_200_000m, 1.03, HAMMADDE),
            K("TZ-09", Gruplar[1], "Lojistik", "Yurt İçi Nakliye ve Dağıtım", "760.01.001", "Lojistik", 1_600_000m, 1.09, HAMMADDE, null, "Akaryakıt artışı"),
            K("TZ-10", Gruplar[1], "Lojistik", "İhracat Navlun ve Yük Sigortası", "760.01.002", "İhracat Operasyon", 1_400_000m, 1.07, NAVLUN, ITHALATCI),
            K("TZ-11", Gruplar[1], "İthalat", "İthalat Gümrük, Antrepo ve Ardiye", "760.02.001", "İthalat", 1_100_000m, 1.05, HAMMADDE, ITHALATCI),
            K("TZ-12", Gruplar[1], "Sarf", "Üretim Yedek Parça ve Sarf Malzeme", "150.04.001", "Bakım", 700_000m, 1.10, BAKIM, SANAYI),
            // ---------------- GENEL GİDERLER ----------------
            K("GG-01", Gruplar[2], "Enerji", "Elektrik", "770.07.001", "Üretim", 2_400_000m, 1.18, ELEKTRIK, null, "Sanayi tarifesi artışı; bütçe eski tarifeyle yapıldı"),
            K("GG-02", Gruplar[2], "Enerji", "Doğalgaz ve Buhar", "770.07.002", "Üretim", 1_900_000m, 1.07, DOGALGAZ, SANAYI),
            K("GG-03", Gruplar[2], "Enerji", "Su ve Atık Su", "770.07.003", "Üretim", 180_000m, 1.02),
            K("GG-04", Gruplar[2], "Bakım", "Bakım ve Onarım", "770.08.001", "Bakım", 900_000m, 1.12, BAKIM, null, "Yaz duruşu bakımları Temmuz / Ağustos"),
            K("GG-05", Gruplar[2], "Danışmanlık", "Mali Müşavirlik, Denetim ve Danışmanlık", "770.09.001", "Mali İşler", 350_000m, 1.00, DANISMANLIK),
            K("GG-06", Gruplar[2], "Hukuk", "Hukuk, Noter ve Dava Giderleri", "770.09.002", "Hukuk", 120_000m, 1.20),
            K("GG-07", Gruplar[2], "Seyahat", "Seyahat ve Konaklama", "770.10.001", "Satış", 400_000m, 0.85, SEYAHAT),
            K("GG-08", Gruplar[2], "Pazarlama", "Pazarlama, Fuar ve Tanıtım", "770.11.001", "Pazarlama", 800_000m, 0.92, PAZARLAMA, null, "Mart ve Ekim fuar dönemleri"),
            K("GG-09", Gruplar[2], "Pazarlama", "Numune ve Müşteri Ziyaret Giderleri", "770.11.002", "Satış", 220_000m, 1.00),
            K("GG-10", Gruplar[2], "Ofis", "Ofis, Kırtasiye ve Sarf", "770.12.001", "İdari İşler", 90_000m, 0.95),
            K("GG-11", Gruplar[2], "İletişim", "Telefon, İnternet ve Veri Hatları", "770.12.002", "Bilgi Teknolojileri", 110_000m, 1.00),
            K("GG-12", Gruplar[2], "Hizmet Alımı", "Temizlik ve Güvenlik Hizmetleri", "770.13.001", "İdari İşler", 450_000m, 1.03),
            K("GG-13", Gruplar[2], "Eğitim", "Eğitim ve Sertifikasyon", "770.14.001", "İnsan Kaynakları", 80_000m, 0.70, EGITIM, null, "Planlanan eğitimlerin bir kısmı ertelendi"),
            K("GG-14", Gruplar[2], "Banka", "Banka Masrafları ve Havale Ücretleri", "780.02.001", "Finans", 200_000m, 1.04),
            K("GG-15", Gruplar[2], "Temsil", "Temsil ve Ağırlama", "770.15.001", "Genel Müdürlük", 150_000m, 1.10),
            K("GG-16", Gruplar[2], "Bağış ve Üyelik", "Bağış, Sponsorluk ve Üyelik Aidatları", "770.16.001", "Genel Müdürlük", 50_000m, 1.00)
        };
        public static BtKalem KalemBul(string kod) => Kalemler.FirstOrDefault(k => k.Kod == kod);
        public static bool Uygulanir(BtKalem k, string sirket) => k.Sirketler == null || k.Sirketler.Contains(sirket);

        // ---------- Kayıtlar ----------
        private static readonly List<BtSatir> _satirlar = new List<BtSatir>();
        public static readonly List<BtRevizyon> Revizyonlar = new List<BtRevizyon>();
        private static bool _tohumlandi; private static int _revSayac;
        private static int Seed(string s) => s.Aggregate(17, (a, ch) => unchecked(a * 31 + ch));
        public static List<BtSatir> Satirlar { get { Tohumla(); lock (Kilit) return _satirlar.ToList(); } }
        public static BtSatir Bul(string sirket, int yil, string kod) { Tohumla(); lock (Kilit) return _satirlar.FirstOrDefault(s => s.Sirket == sirket && s.Yil == yil && s.KalemKod == kod); }

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                foreach (var yil in Yillar) foreach (var sirket in Sirketler) foreach (var k in Kalemler.Where(x => Uygulanir(x, sirket))) _satirlar.Add(Uret(k, sirket, yil));
                Senaryolar();
            }
        }

        /// <summary>Bütçe: taban × şirket ölçeği × yıl çarpanı × mevsim (1.000 TL'ye yuvarlı). Gerçekleşen: kapanmış aylar için bütçe × eğilim × ±%6; geçici ay kısmi.</summary>
        private static BtSatir Uret(BtKalem k, string sirket, int yil)
        {
            var rnd = new Random(Seed(k.Kod + "|" + sirket + "|" + yil));
            var s = new BtSatir { Sirket = sirket, Yil = yil, KalemKod = k.Kod };
            decimal olcek = Olcek.TryGetValue(sirket, out var o) ? o : 0.3m, yilC = YilCarpani.TryGetValue(yil, out var y) ? y : 1m;
            int kapali = KapanmisAy(yil), gecici = GeciciAy(yil);
            double sirketEgilim = 1 + (rnd.NextDouble() - 0.5) * 0.04;                  // şirkete özgü küçük eğilim farkı
            for (int i = 0; i < 12; i++)
            {
                double mevsim = k.Mevsim == null ? 1.0 : k.Mevsim[i];
                decimal butce = Math.Round(k.Taban * olcek * yilC * (decimal)mevsim / 1000m) * 1000m;
                s.Butce[i] = butce; s.IlkButce[i] = butce;
                double sapma = 1 + (rnd.NextDouble() - 0.5) * 0.12;                      // ±%6 aylık sapma
                decimal gercek = Math.Round(butce * (decimal)(k.Egilim * sirketEgilim * sapma));
                if (i < kapali) s.Gercek[i] = gercek;
                else if (gecici > 0 && i == gecici - 1) s.Gercek[i] = Math.Round(gercek * DateTime.Today.Day / DateTime.DaysInMonth(yil, gecici));
                else s.Gercek[i] = null;
            }
            return s;
        }

        /// <summary>Örnek revizyon geçmişi (finansın açık aylara yaptığı bütçe düzeltmeleri).</summary>
        private static void Senaryolar()
        {
            int yil = DateTime.Today.Year; var bugun = DateTime.Today;
            void R(string sirket, string kod, int ay, decimal carpan, string gerekce, int gunOnce, string kim)
            {
                var s = _satirlar.FirstOrDefault(x => x.Sirket == sirket && x.Yil == yil && x.KalemKod == kod); if (s == null || ay < 1 || ay > 12) return;
                decimal onceki = s.Butce[ay - 1], yeni = Math.Round(onceki * carpan / 1000m) * 1000m; s.Butce[ay - 1] = yeni;
                Revizyonlar.Add(new BtRevizyon { Id = ++_revSayac, Zaman = bugun.AddDays(-gunOnce).AddHours(11), Kullanici = kim, Sirket = sirket, Yil = yil, KalemKod = kod, KalemAd = KalemBul(kod)?.Ad, Ay = ay, Onceki = onceki, Yeni = yeni, Gerekce = gerekce });
            }
            int ay = bugun.Month;
            R("Uras Kimya", "GG-01", Math.Min(12, ay + 1), 1.15m, "Sanayi elektrik tarifesi artışı (EPDK); Ekim–Aralık bütçesi yeni tarifeyle güncellendi", 6, "Nazlı Erdem");
            R("Uras Kimya", "GG-01", Math.Min(12, ay + 2), 1.15m, "Sanayi elektrik tarifesi artışı (EPDK)", 6, "Nazlı Erdem");
            R("Uras Kimya", "GG-01", Math.Min(12, ay + 3), 1.15m, "Sanayi elektrik tarifesi artışı (EPDK)", 6, "Nazlı Erdem");
            R("Uras Kimya", "GG-08", Math.Min(12, ay + 1), 1.30m, "Ekim fuar stant alanı büyütüldü (yönetim kararı)", 12, "Nazlı Erdem");
            R("Selvi", "TZ-05", Math.Min(12, ay + 1), 1.10m, "Q4 satış hedefi artışına bağlı ticari mal alımı", 3, "Nazlı Erdem");
            Revizyonlar.Sort((a, b) => b.Zaman.CompareTo(a.Zaman));
        }

        /// <summary>Açık (kapanmamış) aya bütçe revizyonu: şirket + yıl + kalem + ay + yeni tutar + zorunlu gerekçe. Hata metni döner; null → başarılı.</summary>
        public static string Revize(BtIstek g, string kullanici)
        {
            Tohumla();
            if (g == null) return "İstek boş.";
            if (string.IsNullOrEmpty(g.Sirket) || !Sirketler.Contains(g.Sirket)) return "Revizyon için şirket seçin (Tümü ile revizyon yapılamaz).";
            if (!Yillar.Contains(g.Yil)) return "Geçersiz yıl.";
            var k = KalemBul(g.KalemKod); if (k == null) return "Kalem bulunamadı.";
            if (!Uygulanir(k, g.Sirket)) return $"{k.Ad} kalemi {g.Sirket} için tanımlı değil.";
            if (g.Ay < 1 || g.Ay > 12) return "Geçersiz ay.";
            if (g.Ay <= KapanmisAy(g.Yil)) return $"{Aylar[g.Ay - 1]} {g.Yil} kapanmış; kapanmış aya bütçe revizyonu yapılamaz.";
            if (!g.Tutar.HasValue || g.Tutar < 0) return "Yeni bütçe tutarı zorunludur (0 ya da üstü).";
            if (string.IsNullOrWhiteSpace(g.Gerekce) || g.Gerekce.Trim().Length < 10) return "Revizyon gerekçesi zorunludur (en az 10 karakter).";
            lock (Kilit)
            {
                var s = _satirlar.FirstOrDefault(x => x.Sirket == g.Sirket && x.Yil == g.Yil && x.KalemKod == g.KalemKod); if (s == null) return "Bütçe satırı bulunamadı.";
                decimal onceki = s.Butce[g.Ay - 1], yeni = Math.Round(g.Tutar.Value, 2);
                if (onceki == yeni) return "Tutar değişmedi.";
                s.Butce[g.Ay - 1] = yeni;
                Revizyonlar.Insert(0, new BtRevizyon { Id = ++_revSayac, Zaman = DateTime.Now, Kullanici = string.IsNullOrEmpty(kullanici) ? "Sistem" : kullanici, Sirket = g.Sirket, Yil = g.Yil, KalemKod = k.Kod, KalemAd = k.Ad, Ay = g.Ay, Onceki = onceki, Yeni = yeni, Gerekce = g.Gerekce.Trim() });
            }
            return null;
        }
    }
}
