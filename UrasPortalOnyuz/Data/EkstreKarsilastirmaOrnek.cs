// ============================================================
// CARİ EKSTRE / HESAP EKSTRESİ MUTABAKATI — carinin cari ekstresi (120 / 320 cari hesabı, Cari Mutabakat hareketleri) ile muhasebe hesap ekstresi (120.01.xxx / 320.01.xxx kebir hesabı) satır satır karşılaştırılır
//   (Views/Rapor141/Index.cshtml, js/ekstre-karsilastirma.js). Hesap ekstresi ön yüz örneğinde cari ekstresinden türetilir ve bazı carilerde deterministik farklar üretilir:
//   fiş cariye işlenmemiş (yalnız hesapta), fatura muhasebeye aktarılmamış (yalnız caride), tutar farkı (kur / kuruş), mükerrer fiş, hesap tanımsız. Düzeltme fişi önerisi ve (simüle) fiş oluşturma.
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    public class EkSatir { public string Taraf { get; set; } public DateTime Tarih { get; set; } public string Belge { get; set; } public string Tip { get; set; } public string Aciklama { get; set; } public string Pb { get; set; } public decimal Borc { get; set; } public decimal Alacak { get; set; } public string Fis { get; set; } public string IslemTipi { get; set; } }
    public class EkEslesme { public EkSatir Cari { get; set; } public EkSatir Hesap { get; set; } public string Durum { get; set; } public string Neden { get; set; } public string Oneri { get; set; } public decimal Fark { get; set; } }
    public class EkDuzeltme { public int Id { get; set; } public DateTime Zaman { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string FisNo { get; set; } public string Tur { get; set; } public string Aciklama { get; set; } public decimal Tutar { get; set; } public string Kullanici { get; set; } }

    public static class EkstreKarsilastirmaOrnek
    {
        private static readonly object Kilit = new object(); private static readonly List<EkDuzeltme> _duzeltmeler = new List<EkDuzeltme>(); private static int _fisSayac = 4120;
        private static readonly HashSet<string> _duzeltildi = new HashSet<string>();   // "kod|belge" düzeltme fişi atılmış farklar
        public static List<EkDuzeltme> Duzeltmeler { get { lock (Kilit) return _duzeltmeler.OrderByDescending(d => d.Zaman).ToList(); } }
        static int Seed(string s) => s.Aggregate(17, (a, ch) => unchecked(a * 31 + ch));
        public static string HesapKodu(CariKart k) => k == null ? "" : (k.KartTipi == "Tedarikçi" ? "320.01." : "120.01.") + (Math.Abs(Seed(k.Kod)) % 900 + 100).ToString("000");
        public static bool HesapTanimsiz(string kod) => Math.Abs(Seed("ht" + kod)) % 13 == 0;
        public static string FisNo(MtHareket h) => $"{h.Tarih:yyMM}-{(Math.Abs(Seed(h.BelgeNo)) % 9000 + 1000)}";

        /// <summary>Cari tarafı: Cari Mutabakat hareketleri (tarih aralığında). Belge tipine göre borç / alacak zaten MtHareket'te.</summary>
        public static List<EkSatir> CariSatirlari(MtCari c, DateTime bas, DateTime bit) => c.Hareketler.Where(h => h.Tarih.Date >= bas.Date && h.Tarih.Date <= bit.Date).OrderBy(h => h.Tarih).ThenBy(h => h.Id)
            .Select(h => new EkSatir { Taraf = "Cari", Tarih = h.Tarih, Belge = h.BelgeNo, Tip = h.BelgeTipi, Aciklama = h.Aciklama, Pb = h.ParaBirimi, Borc = Tl(h.Borc, h.ParaBirimi), Alacak = Tl(h.Alacak, h.ParaBirimi), Fis = FisNo(h), IslemTipi = h.IslemTipi }).ToList();
        static decimal Tl(decimal v, string pb) => Math.Round(v * (pb == "TL" || pb == "TRY" || string.IsNullOrEmpty(pb) ? 1m : (MutabakatOrnek.Kurlar.TryGetValue(pb, out var k) ? k : 1m)), 2);

        /// <summary>Hesap tarafı: cari satırlarından türetilir; deterministik farklar eklenir. Düzeltme fişi atılmış farklar giderilmiş sayılır.</summary>
        public static List<EkSatir> HesapSatirlari(MtCari c, List<EkSatir> cari)
        {
            var l = new List<EkSatir>(); int z = Math.Abs(Seed("hs" + c.CariKodu)); var rnd = new Random(z);
            for (int i = 0; i < cari.Count; i++)
            {
                var s = cari[i]; var h = new EkSatir { Taraf = "Hesap", Tarih = s.Tarih, Belge = s.Belge, Tip = s.Tip, Aciklama = s.Aciklama, Pb = s.Pb, Borc = s.Borc, Alacak = s.Alacak, Fis = s.Fis, IslemTipi = s.IslemTipi };
                bool duz = _duzeltildi.Contains(c.CariKodu + "|" + s.Belge);
                int f = (z + i * 7) % 23;
                if (!duz && f == 0 && s.Tip == "Fatura" && i >= cari.Count - 6) continue;                                                            // fatura muhasebeye aktarılmamış → yalnız caride
                if (!duz && f == 1 && s.Pb != "TL" && s.Pb != "TRY") { if (h.Borc > 0) h.Borc = Math.Round(h.Borc * 0.994m, 2); else h.Alacak = Math.Round(h.Alacak * 0.994m, 2); h.Aciklama += " (muhasebe kuru)"; }   // kur farkı
                if (!duz && f == 2 && (s.Pb == "TL" || s.Pb == "TRY")) { if (h.Borc > 0) h.Borc += 0.10m; else h.Alacak += 0.10m; }                   // kuruş farkı
                l.Add(h);
                if (!duz && f == 3 && (s.Tip == "Tahsilat" || s.Tip == "Ödeme") && i >= cari.Count - 8) l.Add(new EkSatir { Taraf = "Hesap", Tarih = s.Tarih, Belge = s.Belge, Tip = s.Tip, Aciklama = s.Aciklama + " (mükerrer fiş)", Pb = s.Pb, Borc = s.Borc, Alacak = s.Alacak, Fis = s.Fis + "-M", IslemTipi = s.IslemTipi });
            }
            if (!_duzeltildi.Contains(c.CariKodu + "|KF") && z % 9 == 0 && cari.Count > 0) { var t = cari.Last().Tarih.AddDays(-rnd.Next(1, 10)); bool mus = c.Kart?.KartTipi != "Tedarikçi"; decimal kf = rnd.Next(800, 9000); l.Add(new EkSatir { Taraf = "Hesap", Tarih = t, Belge = $"KFF-{t:yyyy}-{rnd.Next(100, 999)}", Tip = "Kur Farkı", Aciklama = "Dönem sonu kur farkı fişi (cariye işlenmemiş)", Pb = "TL", Borc = mus ? kf : 0, Alacak = mus ? 0 : kf, Fis = $"{t:yyMM}-{rnd.Next(1000, 9999)}", IslemTipi = "X" }); }
            if (!_duzeltildi.Contains(c.CariKodu + "|MH") && z % 11 == 0 && cari.Count > 0) { var t = cari.Last().Tarih.AddDays(-rnd.Next(2, 20)); decimal m = rnd.Next(1500, 25000); l.Add(new EkSatir { Taraf = "Hesap", Tarih = t, Belge = $"MHS-{t:yyyy}-{rnd.Next(100, 999)}", Tip = "Mahsup", Aciklama = "Grup içi mahsup fişi (cari ekstresinde yok)", Pb = "TL", Borc = 0, Alacak = m, Fis = $"{t:yyMM}-{rnd.Next(1000, 9999)}", IslemTipi = "X" }); }
            return l.OrderBy(x => x.Tarih).ThenBy(x => x.Belge).ToList();
        }
        /// <summary>Satır eşleştirme: belge no + taraf; tutar farkı, yalnız cari, yalnız hesap, mükerrer.</summary>
        public static List<EkEslesme> Eslestir(List<EkSatir> cari, List<EkSatir> hesap)
        {
            var l = new List<EkEslesme>(); var kalanH = hesap.ToList();
            foreach (var c in cari)
            {
                var h = kalanH.FirstOrDefault(x => x.Belge == c.Belge && !x.Fis.EndsWith("-M")); if (h != null) kalanH.Remove(h);
                if (h == null) l.Add(new EkEslesme { Cari = c, Durum = "Yalnız Cari", Neden = c.Tip == "Fatura" ? "Fatura muhasebeye aktarılmamış" : "Kayıt muhasebe hesabında yok", Oneri = c.Tip == "Fatura" ? "Faturayı muhasebeye aktar (fiş oluştur)" : "Muhasebe fişi oluştur", Fark = c.Borc - c.Alacak });
                else { decimal fark = (c.Borc - c.Alacak) - (h.Borc - h.Alacak); if (Math.Abs(fark) < 0.005m) l.Add(new EkEslesme { Cari = c, Hesap = h, Durum = "Eşleşti", Fark = 0 }); else l.Add(new EkEslesme { Cari = c, Hesap = h, Durum = "Tutar Farkı", Neden = Math.Abs(fark) < 1 ? "Kuruş yuvarlama farkı" : c.Pb != "TL" && c.Pb != "TRY" ? "Kur farkı (cari kuru ≠ muhasebe kuru)" : "Tutar farkı", Oneri = Math.Abs(fark) < 1 ? "Yuvarlama düzeltme fişi" : "Kur farkı düzeltme fişi", Fark = fark }); }
            }
            foreach (var h in kalanH) l.Add(new EkEslesme { Hesap = h, Durum = h.Fis.EndsWith("-M") ? "Mükerrer" : "Yalnız Hesap", Neden = h.Fis.EndsWith("-M") ? "Aynı belge iki kez fişlenmiş" : h.Tip == "Kur Farkı" ? "Kur farkı fişi cariye işlenmemiş" : h.Tip == "Mahsup" ? "Mahsup fişi cari ekstresinde yok" : "Fiş cariye işlenmemiş", Oneri = h.Fis.EndsWith("-M") ? "Mükerrer fişi iptal et" : "Cari hareketi oluştur / fişi cariye bağla", Fark = -(h.Borc - h.Alacak) });
            return l.OrderBy(e => (e.Cari ?? e.Hesap).Tarih).ThenBy(e => (e.Cari ?? e.Hesap).Belge).ToList();
        }
        /// <summary>Düzeltme fişi (simülasyon): fark giderilmiş sayılır, kayıt tutulur ve cari günlüğüne yazılır.</summary>
        public static string DuzeltmeFisi(string kod, string belge, string tur, decimal tutar, string aciklama, string kullanici, out EkDuzeltme d)
        {
            d = null; var c = MutabakatOrnek.Bul(kod); if (c == null) return "Cari bulunamadı."; if (string.IsNullOrWhiteSpace(belge)) return "Belge seçin.";
            string anahtar = belge.StartsWith("KFF-") ? "KF" : belge.StartsWith("MHS-") ? "MH" : belge;
            lock (Kilit)
            {
                if (_duzeltildi.Contains(kod + "|" + anahtar)) return "Bu fark için düzeltme fişi zaten oluşturuldu.";
                _duzeltildi.Add(kod + "|" + anahtar);
                d = new EkDuzeltme { Id = _duzeltmeler.Count + 1, Zaman = DateTime.Now, CariKodu = kod, CariAd = c.Kart?.KisaAd ?? kod, FisNo = $"DZL-{DateTime.Today:yyyy}-{++_fisSayac}", Tur = tur ?? "Düzeltme", Aciklama = $"{belge} · {aciklama}", Tutar = Math.Round(Math.Abs(tutar), 2), Kullanici = kullanici ?? "" };
                _duzeltmeler.Insert(0, d);
            }
            MutabakatOrnek.LogYaz(c, kullanici, "Ekstre Düzeltme Fişi", "", d.FisNo, $"{d.Tur} · {d.Aciklama} · {d.Tutar.ToString("N2", CariKartOrnek.Tr)} ₺");
            return null;
        }
    }
}
