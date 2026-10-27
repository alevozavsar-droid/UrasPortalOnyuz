// ============================================================
// OTOMATİK KAPATMA — açık faturalar ile kapanmamış (faturayla eşleştirilmemiş) ödeme / tahsilat / çek hareketlerinin kapatılması (Views/Rapor64/Index.cshtml, js/otomatik-kapatma.js)
//   Kaynak: Cari Mutabakat hareketleri (Data/MutabakatOrnek.cs). Fatura: Kalan = kapanmamış tutar. Ödeme / tahsilat / çek / senet: Kalan = faturaya dağıtılmamış tutar.
//   Otomatik eşleştirme: aynı para biriminde önce tutarı birebir eşleşen fatura–ödeme çiftleri, sonra kalan ödemeler en eski vadeli açık faturalara (FIFO) dağıtılır.
//   Kapatma: fatura ve ödeme kalanları düşer, cari günlüğüne (MtLog) ve kapama kaydına yazılır; "tüm carilerde otomatik" tek tıkla çalışır.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class KapamaCifti { public string FaturaNo { get; set; } public string OdemeNo { get; set; } public decimal Tutar { get; set; } public string Pb { get; set; } public string Yontem { get; set; } }   // Yöntem: Birebir | FIFO | Elle
    public class KapamaKaydi { public int Id { get; set; } public DateTime Zaman { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string Mod { get; set; } public List<KapamaCifti> Ciftler { get; set; } = new List<KapamaCifti>(); public decimal Toplam => Ciftler.Sum(c => c.Tutar); public string Kullanici { get; set; } public string Not { get; set; } = ""; }

    public static class KapamaOrnek
    {
        private static readonly object Kilit = new object(); private static readonly List<KapamaKaydi> _kayitlar = new List<KapamaKaydi>(); private static int _sayac;
        public static readonly string[] OdemeTipleri = { "Tahsilat", "Ödeme", "Çek", "Senet" };
        public static List<KapamaKaydi> Kayitlar { get { lock (Kilit) return _kayitlar.OrderByDescending(k => k.Zaman).ToList(); } }
        public static List<MtHareket> AcikFaturalar(MtCari c) => c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Kalan > 0.005m).OrderBy(h => h.Vade ?? h.Tarih).ThenBy(h => h.Id).ToList();
        public static List<MtHareket> AcikOdemeler(MtCari c) => c.Hareketler.Where(h => OdemeTipleri.Contains(h.BelgeTipi) && h.Kalan > 0.005m).OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList();
        /// <summary>Kapatılabilir tutar (para birimi bazında min(açık fatura, açık ödeme) toplamı, TL karşılığı).</summary>
        public static decimal Kapatilabilir(MtCari c)
        {
            decimal t = 0; foreach (var pb in c.Hareketler.Select(h => h.ParaBirimi).Distinct())
            { decimal f = AcikFaturalar(c).Where(x => x.ParaBirimi == pb).Sum(x => x.Kalan), o = AcikOdemeler(c).Where(x => x.ParaBirimi == pb).Sum(x => x.Kalan); t += Math.Min(f, o) * (MutabakatOrnek.Kurlar.TryGetValue(pb, out var k) ? k : 1m); }
            return Math.Round(t, 2);
        }
        /// <summary>Otomatik eşleştirme önerisi: para birimi bazında birebir tutar eşleşmeleri, sonra FIFO dağıtım (yalnız öneri; Kapat ile uygulanır).</summary>
        public static List<KapamaCifti> Oner(MtCari c, IEnumerable<string> faturaSec = null, IEnumerable<string> odemeSec = null)
        {
            var l = new List<KapamaCifti>(); var fs = faturaSec?.ToHashSet(); var os = odemeSec?.ToHashSet();
            foreach (var pb in c.Hareketler.Select(h => h.ParaBirimi).Distinct())
            {
                var fat = AcikFaturalar(c).Where(f => f.ParaBirimi == pb && (fs == null || fs.Contains(f.BelgeNo))).Select(f => (H: f, Kalan: f.Kalan)).ToList();
                var ode = AcikOdemeler(c).Where(o => o.ParaBirimi == pb && (os == null || os.Contains(o.BelgeNo))).Select(o => (H: o, Kalan: o.Kalan)).ToList();
                // 1) birebir
                for (int i = 0; i < ode.Count; i++)
                {
                    int j = fat.FindIndex(f => f.Kalan > 0.005m && Math.Abs(f.Kalan - ode[i].Kalan) < 0.01m); if (j < 0 || ode[i].Kalan <= 0.005m) continue;
                    l.Add(new KapamaCifti { FaturaNo = fat[j].H.BelgeNo, OdemeNo = ode[i].H.BelgeNo, Tutar = fat[j].Kalan, Pb = pb, Yontem = "Birebir" }); fat[j] = (fat[j].H, 0); ode[i] = (ode[i].H, 0);
                }
                // 2) FIFO
                for (int i = 0; i < ode.Count; i++)
                {
                    for (int j = 0; j < fat.Count && ode[i].Kalan > 0.005m; j++)
                    {
                        if (fat[j].Kalan <= 0.005m) continue; decimal al = Math.Min(fat[j].Kalan, ode[i].Kalan);
                        l.Add(new KapamaCifti { FaturaNo = fat[j].H.BelgeNo, OdemeNo = ode[i].H.BelgeNo, Tutar = Math.Round(al, 2), Pb = pb, Yontem = "FIFO" }); fat[j] = (fat[j].H, fat[j].Kalan - al); ode[i] = (ode[i].H, ode[i].Kalan - al);
                    }
                }
            }
            return l;
        }
        /// <summary>Kapatmayı uygular (fatura ve ödeme kalanları düşer). Hata metni ya da null.</summary>
        public static string Kapat(string kod, List<KapamaCifti> ciftler, string mod, string kullanici, out KapamaKaydi kayit, DateTime? simdi = null)
        {
            kayit = null; var c = MutabakatOrnek.Bul(kod); if (c == null) return "Cari bulunamadı."; if (ciftler == null || ciftler.Count == 0) return "Kapatılacak eşleşme yok.";
            lock (MutabakatOrnek.Kilit)
            {
                var uygulanan = new List<KapamaCifti>();
                foreach (var ct in ciftler)
                {
                    var f = c.Hareketler.FirstOrDefault(h => h.BelgeTipi == "Fatura" && h.BelgeNo == ct.FaturaNo); var o = c.Hareketler.FirstOrDefault(h => OdemeTipleri.Contains(h.BelgeTipi) && h.BelgeNo == ct.OdemeNo);
                    if (f == null || o == null) return $"Belge bulunamadı: {ct.FaturaNo} / {ct.OdemeNo}."; if (f.ParaBirimi != o.ParaBirimi) return $"{ct.FaturaNo} ({f.ParaBirimi}) ile {ct.OdemeNo} ({o.ParaBirimi}) para birimi farklı.";
                    decimal t = Math.Round(Math.Min(ct.Tutar <= 0 ? Math.Min(f.Kalan, o.Kalan) : ct.Tutar, Math.Min(f.Kalan, o.Kalan)), 2); if (t <= 0.005m) continue;
                    f.Kalan = Math.Round(f.Kalan - t, 2); f.KapamaDurumu = f.Kalan <= 0.005m ? "Kapalı" : "Kısmi Kapalı"; o.Kalan = Math.Round(o.Kalan - t, 2); o.KapamaDurumu = o.Kalan <= 0.005m ? "—" : "Açık";
                    o.Aciklama = (o.Aciklama ?? "").Replace(" · faturayla eşleştirilmedi (kapama bekliyor)", "") + $" · {f.BelgeNo} kapatıldı";
                    uygulanan.Add(new KapamaCifti { FaturaNo = f.BelgeNo, OdemeNo = o.BelgeNo, Tutar = t, Pb = f.ParaBirimi, Yontem = ct.Yontem ?? mod });
                }
                if (uygulanan.Count == 0) return "Kapatılacak tutar kalmadı.";
                var z = simdi ?? DateTime.Now; MutabakatOrnek.Yenile(c);
                kayit = new KapamaKaydi { Id = ++_sayac, Zaman = z, CariKodu = c.CariKodu, CariAd = c.Kart?.KisaAd ?? c.CariKodu, Mod = mod, Ciftler = uygulanan, Kullanici = kullanici ?? "" }; lock (Kilit) _kayitlar.Insert(0, kayit);
                c.Log.Add(new MtLog { Zaman = z, Kullanici = kullanici ?? "", Islem = "Kapama", Onceki = "", Yeni = $"{uygulanan.Count} eşleşme · {string.Join(", ", uygulanan.GroupBy(u => u.Pb).Select(g => g.Sum(u => u.Tutar).ToString("N2", CariKartOrnek.Tr) + " " + g.Key))}", Aciklama = $"Otomatik Kapatma ({mod}): {string.Join(", ", uygulanan.Select(u => u.FaturaNo + " ← " + u.OdemeNo))}", DurumGecisi = "" });
                var k = CariKartOrnek.KartBul(kod); if (k != null) CariKartOrnek.Gunluk(k, z, kullanici, "Kapama", "", $"{uygulanan.Count} fatura–ödeme eşleşmesi kapatıldı ({mod})", "Otomatik Kapatma");
                return null;
            }
        }
        /// <summary>Tüm carilerde (ya da verilen listede) otomatik kapatma; (cari sayısı, eşleşme sayısı, toplam TL).</summary>
        public static (int Cari, int Eslesme, decimal ToplamTl, List<string> Hatalar) TumunuKapat(IEnumerable<string> kodlar, string kullanici)
        {
            int cari = 0, es = 0; decimal tl = 0; var hatalar = new List<string>();
            foreach (var kod in kodlar.ToList())
            {
                var c = MutabakatOrnek.Bul(kod); if (c == null) continue; var oner = Oner(c); if (oner.Count == 0) continue;
                var h = Kapat(kod, oner, "Otomatik", kullanici, out var k); if (h != null) { hatalar.Add($"{kod}: {h}"); continue; }
                cari++; es += k.Ciftler.Count; tl += k.Ciftler.Sum(x => x.Tutar * (MutabakatOrnek.Kurlar.TryGetValue(x.Pb, out var kr) ? kr : 1m));
            }
            return (cari, es, Math.Round(tl, 2), hatalar);
        }
    }
}
