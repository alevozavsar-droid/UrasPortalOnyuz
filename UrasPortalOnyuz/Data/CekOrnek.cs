using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>
    /// VADE TARİHİNE GÖRE ÇEK ÖZETİ örnek verisi — Mali İşler › Finans › Finansal Araçlar (Yönetim › Özel Raporlar'da aynı ekran).
    /// "2026 Vade Tarihine Göre Tedarikçi Aylık Çek Özeti" tablosunun uyarlaması: şirketin VERDİĞİ çekler (tedarikçi, maaş, kira, sigorta ve diğer)
    /// vade ayına göre cari bazında; müşteriden ALINAN ve tahsile gönderilen çekler Data/ButceOrnek.cs › Cekler listesinden (Bütçe Raporu ile aynı kaynak).
    /// Verilen çek tutarları: cari için yıllık taban (Uras Kimya ölçeği) × şirket ölçeği × yıl çarpanı, aktif aylara ağırlıklı dağıtım, ayda 1–3 çek;
    /// maaş çeki bütçedeki brüt maaş kaleminin %30'u, kira çekleri bütçedeki kira kalemleri (Data/ButceOrnek.cs). Durum vadeye göre: Ödendi / Ödenecek / İade Alındı.
    /// </summary>
    public class VcCek
    {
        public string Sirket { get; set; }
        public string No { get; set; }           // GAR-1234567 (keşideci banka kısaltması)
        public string CariKod { get; set; }      // cari kart kodu (320.xxx) ya da boş (kartı olmayan alıcı)
        public string Cari { get; set; }
        public string Tur { get; set; }          // Tedarikçi | Maaş | Kira | Sigorta ve Diğer
        public string Banka { get; set; }        // çekin keşide edildiği banka (şirket hesabı)
        public DateTime Keside { get; set; }
        public DateTime Vade { get; set; }
        public decimal Tutar { get; set; }
        public string Durum { get; set; }        // Ödenecek | Ödendi | İade Alındı
        public string Aciklama { get; set; }
    }

    public static class CekOrnek
    {
        public static readonly object Kilit = new object();
        public static readonly string[] Turler = { "Tedarikçi", "Maaş", "Kira", "Sigorta ve Diğer" };
        public static readonly string[] Durumlar = { "Ödenecek", "Ödendi", "İade Alındı" };
        public static string[] Sirketler => TeklifOrnek.Sirketler;
        public static int[] Yillar => ButceOrnek.Yillar;
        static readonly string[] Bankalar = { "Garanti BBVA", "İş Bankası", "Ziraat Bankası", "QNB", "Yapı Kredi", "Akbank" };
        static readonly string[] BankaKisa = { "GAR", "ISB", "ZRT", "QNB", "YKB", "AKB" };

        /// <summary>Alıcı havuzu: ad, tür, Uras Kimya ölçeğinde yıllık çek tutarı (2026, TL), yılda kaç ay çek verildiği (12 = her ay). Kartı olan tedarikçiler cari koduyla eklenir.</summary>
        static readonly (string Ad, string Tur, decimal Yillik, int AyAdedi)[] Havuz =
        {
            ("Öztürk Plastik", "Tedarikçi", 16_000_000m, 12), ("Arslan Güneydoğu", "Tedarikçi", 10_500_000m, 5), ("Plastay", "Tedarikçi", 8_600_000m, 10), ("Kimteks", "Tedarikçi", 6_100_000m, 6),
            ("Derin Kimya", "Tedarikçi", 4_900_000m, 10), ("Melkim Kimya", "Tedarikçi", 4_000_000m, 7), ("Globus", "Tedarikçi", 2_800_000m, 7), ("Chemplast", "Tedarikçi", 2_700_000m, 5),
            ("Proses", "Tedarikçi", 2_460_000m, 8), ("Atlaschem", "Tedarikçi", 2_250_000m, 3), ("Karınca Lojistik", "Tedarikçi", 1_900_000m, 3), ("Opera Catering", "Tedarikçi", 1_780_000m, 4),
            ("Gökaş Yemek", "Tedarikçi", 1_720_000m, 5), ("Narcon", "Tedarikçi", 1_620_000m, 4), ("Plastiksan", "Tedarikçi", 1_560_000m, 6), ("Veskim", "Tedarikçi", 1_310_000m, 3),
            ("CC Mekotranik", "Tedarikçi", 1_240_000m, 2), ("Kuzey Personel", "Tedarikçi", 1_230_000m, 3), ("Ventera", "Tedarikçi", 1_220_000m, 5), ("CRN Boya", "Tedarikçi", 1_110_000m, 2),
            ("Bulutpark", "Tedarikçi", 1_020_000m, 4), ("Balkan Geri Dönüşüm", "Tedarikçi", 890_000m, 7), ("TF Gümrük", "Tedarikçi", 790_000m, 5), ("Nora Çevre", "Tedarikçi", 780_000m, 2),
            ("A3 Gıda Catering", "Tedarikçi", 720_000m, 4), ("Azelis", "Tedarikçi", 670_000m, 2), ("Mars Lojistik", "Tedarikçi", 670_000m, 1), ("Egesil Kimya", "Tedarikçi", 670_000m, 2),
            ("Cantat Tabldot", "Tedarikçi", 666_000m, 2), ("HT Zirai", "Tedarikçi", 657_000m, 3), ("Çınar Orman Ürünleri", "Tedarikçi", 602_000m, 6), ("Makrosel", "Tedarikçi", 552_000m, 4),
            ("Güveniş", "Tedarikçi", 535_000m, 5), ("Aymek Matbaa", "Tedarikçi", 524_000m, 6), ("Papex", "Tedarikçi", 486_000m, 4), ("Express Ofis", "Tedarikçi", 450_000m, 4),
            ("Deniz Boya", "Tedarikçi", 432_000m, 2), ("Doğuş Palet", "Tedarikçi", 427_000m, 6), ("Garanti İstif", "Tedarikçi", 408_000m, 2), ("Onur Kompresör", "Tedarikçi", 400_000m, 1),
            ("Türkmen Lojistik", "Tedarikçi", 372_000m, 1), ("Cargo Partner", "Tedarikçi", 330_000m, 1), ("Meka Nakliyat", "Tedarikçi", 325_000m, 2), ("Fix Antrepo", "Tedarikçi", 315_000m, 2),
            ("Kimpaz", "Tedarikçi", 240_000m, 1), ("Likit Kimya", "Tedarikçi", 236_000m, 1), ("Logist Kargo", "Tedarikçi", 253_000m, 2), ("HGL Gümrük", "Tedarikçi", 205_000m, 2),
            ("Hakel Ambalaj", "Tedarikçi", 202_000m, 1), ("Koray Kimya", "Tedarikçi", 180_000m, 2), ("Çelik Ambalaj", "Tedarikçi", 166_000m, 3), ("General Kargo", "Tedarikçi", 31_000m, 1),
            ("Değişim Sigorta", "Sigorta ve Diğer", 7_500_000m, 11), ("Omya Sigorta", "Sigorta ve Diğer", 250_000m, 1), ("Deri OSB", "Sigorta ve Diğer", 760_000m, 1), ("Veli Meşe OSB", "Sigorta ve Diğer", 249_000m, 1),
            ("Qural Denetim", "Sigorta ve Diğer", 130_000m, 1), ("Elite Mühendislik", "Sigorta ve Diğer", 350_000m, 2), ("Tarkan Reklam", "Sigorta ve Diğer", 180_000m, 2), ("Section Turizm", "Sigorta ve Diğer", 174_000m, 2)
        };
        /// <summary>Kira alıcıları: fabrika / depo kirası (bütçe SG-06) ve ofis kirası (SG-07) çekleri her ay.</summary>
        static readonly (string Ad, string ButceKalemi)[] Kiralar = { ("Bizim Emlak", "SG-06"), ("Erdal Özcan", "SG-07") };

        private static readonly List<VcCek> _verilen = new List<VcCek>();
        private static bool _tohumlandi;
        private static int Seed(string s) => s.Aggregate(17, (a, ch) => unchecked(a * 31 + ch));
        public static List<VcCek> Verilen { get { Tohumla(); lock (Kilit) return _verilen.ToList(); } }

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                foreach (var yil in Yillar) foreach (var sirket in Sirketler) _verilen.AddRange(Uret(sirket, yil));
                _verilen.Sort((a, b) => a.Vade != b.Vade ? a.Vade.CompareTo(b.Vade) : string.CompareOrdinal(a.No, b.No));
            }
        }

        private static string Durum(DateTime vade, Random rnd, DateTime bugun) => vade < bugun ? (rnd.Next(40) == 0 ? "İade Alındı" : "Ödendi") : "Ödenecek";

        private static IEnumerable<VcCek> Uret(string sirket, int yil)
        {
            var rnd = new Random(Seed("verilen|" + sirket + "|" + yil)); var bugun = DateTime.Today;
            decimal olcek = ButceOrnek.Olcek.TryGetValue(sirket, out var o) ? o : 0.3m, yilC = ButceOrnek.YilCarpani.TryGetValue(yil, out var y) ? y : 1m;
            var liste = new List<VcCek>();
            int bankaBas = Array.IndexOf(Sirketler, sirket) % Bankalar.Length;
            string Banka(int i) => Bankalar[(bankaBas + i) % Bankalar.Length];
            string No(int bi) => $"{BankaKisa[(bankaBas + bi) % Bankalar.Length]}-{rnd.Next(1000000, 9999999)}";
            void Ekle(string cariKod, string cari, string tur, DateTime vade, decimal tutar, string aciklama)
            {
                int bi = rnd.Next(3); var keside = vade.AddDays(-rnd.Next(45, 121));
                liste.Add(new VcCek { Sirket = sirket, No = No(bi), CariKod = cariKod, Cari = cari, Tur = tur, Banka = Banka(bi), Keside = keside, Vade = vade, Tutar = tutar, Durum = Durum(vade, rnd, bugun), Aciklama = aciklama });
            }
            // 1) Tedarikçi / sigorta alıcıları: havuz (Uras Kimya tamamı, diğer şirketler deterministik alt küme) + şirketin kartlı tedarikçileri
            var alicilar = new List<(string Kod, string Ad, string Tur, decimal Yillik, int AyAdedi)>();
            int adim = sirket == "Uras Kimya" ? 1 : 2 + Array.IndexOf(Sirketler, sirket) % 2;
            for (int i = 0; i < Havuz.Length; i++) if (sirket == "Uras Kimya" || (i + Array.IndexOf(Sirketler, sirket)) % adim == 0) alicilar.Add(("", Havuz[i].Ad, Havuz[i].Tur, Havuz[i].Yillik, Havuz[i].AyAdedi));
            foreach (var k in CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Tedarikçi" && x.Sirket == sirket))
                alicilar.Add((k.Kod, k.KisaAd, "Tedarikçi", k.Sinif == "Hammadde" ? 6_500_000m : k.Sinif == "Hizmetler" || k.Sinif == "Danışmanlık" ? 900_000m : 2_200_000m, k.Sinif == "Hammadde" ? 10 : 6));
            foreach (var a in alicilar)
            {
                decimal yillik = Math.Round(a.Yillik * olcek * yilC * (decimal)(0.85 + rnd.NextDouble() * 0.3) / 1000m) * 1000m;
                var aylar = Enumerable.Range(1, 12).OrderBy(_ => rnd.Next()).Take(Math.Min(12, a.AyAdedi)).OrderBy(x => x).ToList();
                var agirlik = aylar.Select(_ => 0.5 + rnd.NextDouble()).ToArray(); double top = agirlik.Sum();
                for (int i = 0; i < aylar.Count; i++)
                {
                    decimal ayTutar = Math.Round(yillik * (decimal)(agirlik[i] / top) / 1000m) * 1000m; if (ayTutar <= 0) continue;
                    int n = ayTutar > 1_500_000m ? rnd.Next(2, 4) : rnd.Next(1, 3);
                    var par = Enumerable.Range(0, n).Select(_ => 0.6 + rnd.NextDouble()).ToArray(); double parTop = par.Sum(); decimal kalan = ayTutar;
                    for (int j = 0; j < n; j++)
                    {
                        decimal t = j == n - 1 ? kalan : Math.Round(ayTutar * (decimal)(par[j] / parTop) / 1000m) * 1000m; kalan -= t; if (t <= 0) continue;
                        var vade = new DateTime(yil, aylar[i], 1 + rnd.Next(DateTime.DaysInMonth(yil, aylar[i])));
                        Ekle(a.Kod, a.Ad, a.Tur, vade, t, a.Tur == "Tedarikçi" ? "Fatura ödemesi" : a.Tur == "Sigorta ve Diğer" ? "Poliçe / aidat / hizmet" : "");
                    }
                }
            }
            // 2) Maaş: her ay brüt maaş bütçesinin %30'u çekle (bütçe satırı yoksa taban)
            for (int ay = 1; ay <= 12; ay++)
            {
                var sg1 = ButceOrnek.Bul(sirket, yil, "SG-01"); decimal maas = Math.Round((sg1 != null ? sg1.Butce[ay - 1] : 9_500_000m * olcek * yilC) * 0.30m / 1000m) * 1000m;
                Ekle("", "Personel Maaş Ödemesi", "Maaş", new DateTime(yil, ay, 5), maas, $"{ButceOrnek.Aylar[ay - 1]} maaşları · çekle ödenen kısım (%30)");
            }
            // 3) Kira: fabrika / depo (SG-06, tanımlıysa) ve ofis (SG-07) kira çekleri her ayın başında
            foreach (var k in Kiralar)
                for (int ay = 1; ay <= 12; ay++)
                {
                    var s = ButceOrnek.Bul(sirket, yil, k.ButceKalemi); if (s == null) continue;
                    Ekle("", k.Ad, "Kira", new DateTime(yil, ay, 1 + rnd.Next(3)), s.Butce[ay - 1], (k.ButceKalemi == "SG-06" ? "Fabrika ve depo kirası" : "Ofis kirası") + " · " + ButceOrnek.Aylar[ay - 1]);
                }
            return liste;
        }
    }
}
