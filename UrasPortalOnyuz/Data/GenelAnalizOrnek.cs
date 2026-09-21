// ============================================================
// GENEL ANALİZ RAPORU — "2026 Aylık Analiz Raporu" Excel şablonunun portal karşılığı.
// Satır yapısı şablondan: CİRO (şirket alt toplamlarıyla), ALIMLAR, GİDERLER, ARA SONUÇ (Alım + Gider, Faaliyet Kâr / Zararı),
// AMORTİSMAN, ARA SONUÇ (Dönem Net Kâr / Zarar), KUR FARKI, SONUÇ (Kur Farkı Sonrası Kâr / Zarar).
// Sütunlar: Ocak … Aralık, TOPLAM (yıl), AYLIK ORT. (dolu ayların ortalaması; Excel'deki IF(COUNT()=0,"",SUM/AVERAGE) mantığı).
// Yalnız yaprak satırlar veri taşır; alt toplam ve sonuç satırları terim listesinden (işaretli toplam) hesaplanır.
// Değerler bellek içidir (yıl → satır anahtarı → 12 aylık dizi); örnek veri kapanmış aylar için tohumlanır, muhasebe kaynağı
// bağlanınca gerçek değerlerle değişecek. Ekran: Controllers/GenelAnalizController.cs + Views/GenelAnaliz/Index.cshtml
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    public class GaSatir
    {
        public string K { get; set; }            // anahtar
        public string Ad { get; set; }
        public string Bolum { get; set; }        // CİRO | ALIMLAR | GİDERLER | ARA SONUÇ | AMORTİSMAN | KUR FARKI | SONUÇ
        public string Tip { get; set; }          // leaf | sub (alt toplam) | sonuc (ara sonuç / sonuç)
        /// <summary>Hesaplanan satırlar için terimler: (anahtar, işaret)</summary>
        public List<(string K, int Isaret)> Terimler { get; set; } = new List<(string, int)>();
        /// <summary>Örnek veri ölçeği (TL, aylık; 0 → veri üretilmez)</summary>
        public decimal OrnekMin { get; set; }
        public decimal OrnekMax { get; set; }
    }

    public static class GenelAnalizOrnek
    {
        public static readonly object Kilit = new object();
        public static readonly string[] Aylar = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

        private static GaSatir L(string k, string ad, string bolum, decimal min, decimal max) => new GaSatir { K = k, Ad = ad, Bolum = bolum, Tip = "leaf", OrnekMin = min, OrnekMax = max };
        private static GaSatir S(string k, string ad, string bolum, params string[] toplananlar) => new GaSatir { K = k, Ad = ad, Bolum = bolum, Tip = "sub", Terimler = toplananlar.Select(x => (x, 1)).ToList() };
        private static GaSatir R(string k, string ad, string bolum, params (string, int)[] terimler) => new GaSatir { K = k, Ad = ad, Bolum = bolum, Tip = "sonuc", Terimler = terimler.ToList() };

        // ---- Satır yapısı (Excel şablonu sırasıyla) ----
        public static readonly List<GaSatir> Satirlar = new List<GaSatir>
        {
            // CİRO
            L("urasSelviYiX", "URAS Selvi Yurtiçi Ciro - X", "CİRO", 9_000_000m, 16_000_000m),
            L("urasSelviYiR", "URAS Selvi Yurtiçi Ciro - R", "CİRO", 3_000_000m, 7_000_000m),
            L("urasSelviYd", "URAS Selvi Yurtdışı Ciro", "CİRO", 6_000_000m, 14_000_000m),
            S("urasToplam", "URAS TOPLAM CİRO", "CİRO", "urasSelviYiX", "urasSelviYiR", "urasSelviYd"),
            L("alvSelviYi", "ALV Selvi Yurtiçi Ciro", "CİRO", 2_500_000m, 6_000_000m),
            L("alvSelviYd", "ALV Selvi Yurtdışı Ciro", "CİRO", 1_500_000m, 4_500_000m),
            S("alvToplam", "ALV TOPLAM CİRO", "CİRO", "alvSelviYi", "alvSelviYd"),
            L("apYi", "Avrupa Paper Yurtiçi Ciro", "CİRO", 4_000_000m, 9_000_000m),
            L("apYd", "Avrupa Paper Yurtdışı Ciro", "CİRO", 2_000_000m, 6_000_000m),
            L("apSelvi", "Avrupa Selvi Ciro", "CİRO", 800_000m, 2_500_000m),
            S("apToplam", "AVRUPA PAPER TOPLAM CİRO", "CİRO", "apYi", "apYd", "apSelvi"),
            L("dafDrnYd", "DAF DRN Yurtdışı Ciro", "CİRO", 1_200_000m, 3_500_000m),
            L("dafSelviYd", "DAF Selvi Yurtdışı Ciro", "CİRO", 600_000m, 2_000_000m),
            S("dafToplam", "DAF TOPLAM CİRO", "CİRO", "dafDrnYd", "dafSelviYd"),
            L("ursDrnYd", "URS DRN Yurtdışı Ciro", "CİRO", 700_000m, 2_200_000m),
            L("ursSelviYd", "URS Selvi Yurtdışı Ciro", "CİRO", 400_000m, 1_500_000m),
            L("ursSelviYi", "URS Selvi Yurtiçi Ciro", "CİRO", 900_000m, 2_800_000m),
            S("ursToplam", "URS MAKİNE CİRO", "CİRO", "ursDrnYd", "ursSelviYd", "ursSelviYi"),
            L("ges", "GES CİRO", "CİRO", 150_000m, 450_000m),
            S("toplamCiro", "TOPLAM CİRO", "CİRO", "urasToplam", "alvToplam", "apToplam", "dafToplam", "ursToplam", "ges"),
            // ALIMLAR
            L("alimUras", "Uras Kimya Alımları", "ALIMLAR", 8_000_000m, 16_000_000m),
            L("alimAlv", "ALV Kimya Alımları", "ALIMLAR", 2_000_000m, 5_000_000m),
            L("alimAp", "Avrupa Paper Alımları", "ALIMLAR", 3_000_000m, 7_500_000m),
            L("alimDaf", "DAF Kimya Alımları", "ALIMLAR", 900_000m, 2_800_000m),
            L("alimSelvi", "Selvi Kimya Alımları", "ALIMLAR", 1_500_000m, 4_000_000m),
            L("alimUrs", "URS Makine Alımları", "ALIMLAR", 700_000m, 2_200_000m),
            S("toplamAlim", "TOPLAM ALIMLAR", "ALIMLAR", "alimUras", "alimAlv", "alimAp", "alimDaf", "alimSelvi", "alimUrs"),
            // GİDERLER
            L("giderUras", "Uras Giderleri", "GİDERLER", 2_500_000m, 4_500_000m),
            L("giderAlv", "ALV Giderleri", "GİDERLER", 700_000m, 1_500_000m),
            L("giderAp", "Avrupa Paper Giderleri", "GİDERLER", 1_000_000m, 2_200_000m),
            L("giderDaf", "DAF Giderleri", "GİDERLER", 350_000m, 900_000m),
            L("giderSelvi", "Selvi Giderleri", "GİDERLER", 600_000m, 1_400_000m),
            L("giderAlvFilo", "ALV Filo Giderleri", "GİDERLER", 250_000m, 700_000m),
            L("giderHolding", "Uras Holding Giderleri", "GİDERLER", 900_000m, 1_800_000m),
            L("giderUrs", "URS Makine Giderleri", "GİDERLER", 300_000m, 800_000m),
            S("toplamGider", "TOPLAM GİDERLER", "GİDERLER", "giderUras", "giderAlv", "giderAp", "giderDaf", "giderSelvi", "giderAlvFilo", "giderHolding", "giderUrs"),
            // ARA SONUÇ
            R("alimGider", "ALIM + GİDERLER TOPLAMI", "ARA SONUÇ", ("toplamAlim", 1), ("toplamGider", 1)),
            R("faaliyet", "FAALİYET KÂR / ZARARI", "ARA SONUÇ", ("toplamCiro", 1), ("alimGider", -1)),
            // AMORTİSMAN
            L("amorUras", "Uras Demirbaş Amortisman", "AMORTİSMAN", 400_000m, 700_000m),
            L("amorAlv", "ALV Demirbaş Amortisman", "AMORTİSMAN", 120_000m, 220_000m),
            L("amorAp", "Avrupa Paper Amortisman", "AMORTİSMAN", 180_000m, 320_000m),
            L("amorDaf", "DAF Demirbaş Amortisman", "AMORTİSMAN", 60_000m, 120_000m),
            L("amorSelvi", "Selvi Amortisman", "AMORTİSMAN", 90_000m, 170_000m),
            L("amorAlvFilo", "ALV Filo Amortisman", "AMORTİSMAN", 140_000m, 260_000m),
            L("amorHolding", "Uras Holding Amortisman", "AMORTİSMAN", 70_000m, 140_000m),
            L("amorUrs", "URS Makine Amortisman", "AMORTİSMAN", 50_000m, 110_000m),
            S("toplamAmor", "TOPLAM DEMİRBAŞ + AMORTİSMAN", "AMORTİSMAN", "amorUras", "amorAlv", "amorAp", "amorDaf", "amorSelvi", "amorAlvFilo", "amorHolding", "amorUrs"),
            // ARA SONUÇ
            R("donemNet", "DÖNEM NET KÂR / ZARAR", "ARA SONUÇ", ("faaliyet", 1), ("toplamAmor", -1)),
            // KUR FARKI (eksi olabilir)
            L("kurUras", "Uras Kur Farkı", "KUR FARKI", -900_000m, 1_200_000m),
            L("kurAlv", "ALV Kur Farkı", "KUR FARKI", -300_000m, 400_000m),
            L("kurAp", "Avrupa Paper Kur Farkı", "KUR FARKI", -500_000m, 700_000m),
            L("kurDaf", "DAF Kur Farkı", "KUR FARKI", -250_000m, 350_000m),
            L("kurSelvi", "Selvi Kur Farkı", "KUR FARKI", -200_000m, 300_000m),
            L("kurAlvFilo", "ALV Filo Kur Farkı", "KUR FARKI", -120_000m, 150_000m),
            L("kurHolding", "Uras Holding Kur Farkı", "KUR FARKI", -400_000m, 500_000m),
            L("kurUrs", "URS Makine Kur Farkı", "KUR FARKI", -150_000m, 200_000m),
            S("toplamKur", "TOPLAM KUR FARKI", "KUR FARKI", "kurUras", "kurAlv", "kurAp", "kurDaf", "kurSelvi", "kurAlvFilo", "kurHolding", "kurUrs"),
            // SONUÇ
            R("sonuc", "KUR FARKI SONRASI KÂR / ZARAR", "SONUÇ", ("donemNet", 1), ("toplamKur", 1))
        };

        // ---- Değerler: yıl → satır anahtarı → 12 ay (null = boş hücre) ----
        private static readonly Dictionary<int, Dictionary<string, decimal?[]>> _degerler = new Dictionary<int, Dictionary<string, decimal?[]>>();
        public static readonly Dictionary<int, (string Kisi, DateTime Tarih)> SonKayit = new Dictionary<int, (string, DateTime)>();

        /// <summary>Yılın yaprak değerleri; ilk erişimde kapanmış aylar için örnek veri tohumlanır.</summary>
        public static Dictionary<string, decimal?[]> Degerler(int yil)
        {
            lock (Kilit)
            {
                if (_degerler.TryGetValue(yil, out var d)) return d;
                d = new Dictionary<string, decimal?[]>();
                var rnd = new Random(yil * 31 + 7);
                int kapali = KapaliAy(yil);
                decimal buyume = 1 + (yil - 2026) * 0.18m;   // yıllar arası kaba büyüme (örnek)
                foreach (var s in Satirlar.Where(x => x.Tip == "leaf"))
                {
                    var dizi = new decimal?[12];
                    for (int ay = 0; ay < kapali; ay++)
                    {
                        decimal t = (decimal)rnd.NextDouble();
                        decimal mevsim = 1 + 0.08m * (decimal)Math.Sin((ay + 1) / 12.0 * Math.PI * 2);   // hafif mevsimsellik
                        dizi[ay] = Math.Round((s.OrnekMin + (s.OrnekMax - s.OrnekMin) * t) * mevsim * buyume, 0);
                    }
                    d[s.K] = dizi;
                }
                _degerler[yil] = d;
                return d;
            }
        }

        /// <summary>Kapanmış (verisi olan) ay sayısı: geçmiş yıl 12, bu yıl geçen aya kadar, gelecek yıl 0.</summary>
        public static int KapaliAy(int yil) => yil < DateTime.Today.Year ? 12 : yil > DateTime.Today.Year ? 0 : DateTime.Today.Month - 1;

        public static string Kaydet(int yil, Dictionary<string, decimal?[]> yeni, string kisi)
        {
            lock (Kilit)
            {
                var d = Degerler(yil);
                foreach (var kv in yeni)
                {
                    if (!Satirlar.Any(s => s.K == kv.Key && s.Tip == "leaf")) return $"Tanımsız ya da hesaplanan satır: {kv.Key}";
                    if (kv.Value == null || kv.Value.Length != 12) return $"{kv.Key}: 12 aylık değer bekleniyor.";
                    d[kv.Key] = kv.Value;
                }
                SonKayit[yil] = (kisi, DateTime.Now);
                return null;
            }
        }

        /// <summary>Hesaplanan satırlar dahil tüm satırların 12 aylık değerleri (rapor / dışa aktarma için).</summary>
        public static Dictionary<string, decimal?[]> Hesapla(int yil)
        {
            var d = Degerler(yil);
            var h = new Dictionary<string, decimal?[]>();
            foreach (var s in Satirlar)
            {
                if (s.Tip == "leaf") { h[s.K] = d.TryGetValue(s.K, out var v) ? v : new decimal?[12]; continue; }
                var dizi = new decimal?[12];
                for (int ay = 0; ay < 12; ay++)
                {
                    decimal? top = null;
                    foreach (var (k, isaret) in s.Terimler) { var v = h.TryGetValue(k, out var x) ? x[ay] : null; if (v.HasValue) top = (top ?? 0) + isaret * v.Value; }
                    dizi[ay] = top;
                }
                h[s.K] = dizi;
            }
            return h;
        }
    }
}
