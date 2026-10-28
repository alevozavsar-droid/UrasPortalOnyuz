// ============================================================
// ADAT HESAPLAMA — cari bazında açık faturalar (müşteride satış, tedarikçide alış) ile ödeme / çek satırlarının FIFO kapanış simülasyonu, aylık adat dağılımı ve vade farkı
//   (Views/Rapor21/Index.cshtml, js/adat.js; hesap istemcide canlı, taslak ödeme satırları ve oran burada cari bazında saklanır). Tahsilata Aktar: müşteri carisinde girilen
//   ödeme satırları Tahsilat ekranına TASLAK tahsilat olarak aktarılır (Data/TahsilatOrnek.cs); Otomatik Kapat: Data/KapamaOrnek.cs.
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    public class AdatSatir { public string Tip { get; set; } = "Nakit"; public DateTime Tarih { get; set; } public string No { get; set; } = ""; public string Banka { get; set; } = ""; public decimal Tutar { get; set; } public bool Secili { get; set; } = true; }
    public class AdatTaslak { public string CariKodu { get; set; } public decimal Oran { get; set; } = 5m; public List<AdatSatir> Satirlar { get; set; } = new List<AdatSatir>(); public bool DinamikVade { get; set; } public string Kullanici { get; set; } = ""; public DateTime Zaman { get; set; } }

    public static class AdatOrnek
    {
        private static readonly object Kilit = new object(); private static readonly Dictionary<string, AdatTaslak> _taslaklar = new Dictionary<string, AdatTaslak>(); private static bool _tohumlandi;
        public static decimal VarsayilanOran = 5m;   // aylık vade farkı oranı (%)
        public static readonly string[] Tipler = { "Nakit", "Çek", "Senet", "Havale / EFT" };
        public static AdatTaslak Taslak(string kod) { Tohumla(); lock (Kilit) return _taslaklar.TryGetValue(kod ?? "", out var t) ? t : null; }
        public static void TaslakKaydet(string kod, decimal oran, bool dinamikVade, List<AdatSatir> satirlar, string kullanici)
        {
            Tohumla(); var l = (satirlar ?? new List<AdatSatir>()).Where(s => s != null && s.Tutar > 0).Select(s => new AdatSatir { Tip = Tipler.Contains(s.Tip ?? "") ? s.Tip : "Nakit", Tarih = s.Tarih == default ? DateTime.Today : s.Tarih.Date, No = (s.No ?? "").Trim(), Banka = (s.Banka ?? "").Trim(), Tutar = Math.Round(s.Tutar, 2), Secili = s.Secili }).ToList();
            lock (Kilit) _taslaklar[kod] = new AdatTaslak { CariKodu = kod, Oran = oran <= 0 ? VarsayilanOran : oran, DinamikVade = dinamikVade, Satirlar = l, Kullanici = kullanici ?? "", Zaman = DateTime.Now };
            var k = CariKartOrnek.KartBul(kod); if (k != null) CariKartOrnek.Gunluk(k, DateTime.Now, kullanici, "Adat Taslağı", "", $"{l.Count} ödeme satırı · {l.Sum(s => s.Tutar).ToString("N2", CariKartOrnek.Tr)} · oran %{oran.ToString("0.##", CariKartOrnek.Tr)}", "Adat Hesaplama");
        }
        public static void TaslakSil(string kod) { lock (Kilit) _taslaklar.Remove(kod ?? ""); }
        static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var b = DateTime.Today;
                _taslaklar["120.001"] = new AdatTaslak { CariKodu = "120.001", Oran = 5m, Kullanici = "Nazlı Erdem", Zaman = b.AddDays(-2).AddHours(10), Satirlar = new List<AdatSatir> { new AdatSatir { Tip = "Nakit", Tarih = b, Tutar = 350_000m }, new AdatSatir { Tip = "Çek", Tarih = b.AddDays(45), No = "ÇK-7781204", Banka = "Garanti BBVA", Tutar = 600_000m }, new AdatSatir { Tip = "Çek", Tarih = b.AddDays(90), No = "ÇK-7781205", Banka = "Garanti BBVA", Tutar = 600_000m } } };
                _taslaklar["120.011"] = new AdatTaslak { CariKodu = "120.011", Oran = 6m, Kullanici = "Nazlı Erdem", Zaman = b.AddDays(-5).AddHours(15), Satirlar = new List<AdatSatir> { new AdatSatir { Tip = "Çek", Tarih = b.AddDays(60), No = "ÇK-1190344", Banka = "İş Bankası", Tutar = 400_000m } } };
            }
        }
    }
}
