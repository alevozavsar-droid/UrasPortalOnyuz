using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Değerleme dekontunun bir fatura satırı: kalan döviz × (yeni kur − baz kur).</summary>
    public class KdFatura
    {
        public string BelgeNo { get; set; } public string Pb { get; set; } public decimal Kalan { get; set; } public string IslemTipi { get; set; } = "R";
        public decimal BazKur { get; set; } public string BazKaynak { get; set; } = "Fatura Kuru"; public decimal YeniKur { get; set; } public decimal Fark { get; set; }
    }
    /// <summary>KUR FARKI DEĞERLEMESİ = muhasebenin dönem sonunda attığı virman (dekont); fatura değildir, KDV yoktur, yalnız R (resmî) dövizli açık faturalar için.</summary>
    public class KurDegerleme
    {
        public int Id { get; set; } public string DekontNo { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string Donem { get; set; } public DateTime Tarih { get; set; }
        public decimal Toplam { get; set; } public string Kaynak { get; set; } = "Muhasebe Virmanı"; public string IslemTipi { get; set; } = "R"; public string Kullanici { get; set; } = ""; public DateTime Zaman { get; set; } public string HareketNo { get; set; } = ""; public string FisNo { get; set; } = "";
        public List<KdFatura> Faturalar { get; set; } = new List<KdFatura>();
        public string Kurlar => string.Join(" · ", Faturalar.GroupBy(f => f.Pb).Select(g => $"{g.Key} {g.First().YeniKur.ToString("N4", CariKartOrnek.Tr)}"));
    }

    /// <summary>
    /// KUR FARKI DEĞERLEMESİ (19 Kas 2026; kullanıcı: "raporda hem kur farkı değerlemesi hem kur farkı faturası olmalı; değerlemeler muhasebenin attığı virmanlardır, fatura tahsilattan doğar; rapor X ve R satışlardan oluşur").
    ///   • Değerleme = muhasebe virmanı: dönem sonunda R (resmî) dövizli açık faturaların kalanı dönem sonu kuruyla değerlenir, fark cari hesaba "Kur Farkı" dekontu (KF-yyyy-MM-nnn) olarak işlenir (borç: kur arttı · alacak: kur düştü). KDV yok.
    ///   • X (resmî olmayan) faturalarda muhasebe değerlemesi olmaz; ekranda yalnız "değerlenmemiş kur farkı" bilgisi gösterilir.
    ///   • ÇİFT SAYIM KORUMASI: tahsilatta kesilecek kur farkı FATURASININ bazı, R faturada son değerleme kuru (yoksa fatura kuru); X faturada fatura kuru. Böylece aynı kur farkı hem dekontla hem faturayla alınmaz.
    ///   • Örnek veride son iki ay sonunda "Muhasebe (Dönem Sonu)" değerlemesi yapılmış sayılır; raporun kendisi dekont atmaz (muhasebe virmanı beklenir).
    /// </summary>
    public static class KurDegerlemeOrnek
    {
        static readonly CultureInfo Tr = CariKartOrnek.Tr;
        static readonly object Kilit = new object(); static readonly List<KurDegerleme> _l = new List<KurDegerleme>(); static int _sayac; static bool _tohumlandi;
        /// <summary>"cari|belge" → son değerleme (kur, tarih, dekont no). Kilitsiz okunur (TahsilatOrnek / MutabakatOrnek kilitleri altında çağrılabilir).</summary>
        static readonly ConcurrentDictionary<string, (decimal Kur, DateTime Tarih, string Dekont)> _son = new ConcurrentDictionary<string, (decimal, DateTime, string)>();

        public static List<KurDegerleme> Liste(string kod = null) { Tohumla(); lock (Kilit) return _l.Where(d => kod == null || d.CariKodu == kod).OrderByDescending(d => d.Tarih).ThenByDescending(d => d.Id).ToList(); }
        public static (decimal Kur, DateTime Tarih, string Dekont)? SonDegerleme(string kod, string belge) { Tohumla(); return _son.TryGetValue(kod + "|" + belge, out var v) ? v : ((decimal, DateTime, string)?)null; }
        /// <summary>Faturanın değerleme geçmişi (dekont, dönem, baz → yeni kur, fark).</summary>
        public static List<(KurDegerleme D, KdFatura F)> FaturaGecmisi(string kod, string belge) { Tohumla(); lock (Kilit) return _l.Where(d => d.CariKodu == kod).SelectMany(d => d.Faturalar.Where(f => f.BelgeNo == belge).Select(f => (d, f))).OrderBy(x => x.d.Tarih).ToList(); }
        /// <summary>Kur farkı faturası için BAZ KUR: R faturada son değerleme kuru (yoksa fatura kuru), X faturada fatura kuru.</summary>
        public static (decimal Kur, string Kaynak, DateTime? Tarih) BazKur(string kod, MtHareket f)
        {
            decimal fk = TahsilatOrnek.FaturaKuru(f); if (TahsilatOrnek.TlMi(f.ParaBirimi)) return (1m, "TL", null);
            if ((f.IslemTipi ?? "X") != "R") return (fk, "Fatura Kuru (X · değerlenmez)", null);
            var s = SonDegerleme(kod, f.BelgeNo); return s == null ? (fk, "Fatura Kuru", null) : (s.Value.Kur, $"Değerleme {s.Value.Tarih:dd.MM.yyyy} ({s.Value.Dekont})", s.Value.Tarih);
        }
        /// <summary>Önizleme: R dövizli açık faturalarda değerleme bekleyen fark (bugünkü kur − baz) ve X dövizli faturalarda değerlenmeyen fark (bilgi).</summary>
        public static (List<KdFatura> R, List<KdFatura> X) Onizleme(string kod, DateTime? tarih = null)
        {
            var c = MutabakatOrnek.Bul(kod); var r = new List<KdFatura>(); var x = new List<KdFatura>(); if (c == null) return (r, x); var t = (tarih ?? DateTime.Today).Date;
            List<MtHareket> fat; lock (MutabakatOrnek.Kilit) fat = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Kalan > 0.005m && !TahsilatOrnek.TlMi(h.ParaBirimi)).ToList();
            foreach (var f in fat)
            {
                var baz = BazKur(kod, f); var yeni = TahsilatOrnek.KurOto(f.ParaBirimi, t); var k = new KdFatura { BelgeNo = f.BelgeNo, Pb = f.ParaBirimi, Kalan = f.Kalan, IslemTipi = f.IslemTipi ?? "X", BazKur = baz.Kur, BazKaynak = baz.Kaynak, YeniKur = yeni, Fark = Math.Round(f.Kalan * (yeni - baz.Kur), 2) };
                if ((f.IslemTipi ?? "X") == "R") r.Add(k); else x.Add(k);
            }
            return (r, x);
        }
        /// <summary>Dönem sonu değerlemesi (muhasebe virmanı): R dövizli açık faturalar → tek dekont, cari hesaba "Kur Farkı" hareketi. Fark yoksa null.</summary>
        public static KurDegerleme Degerle(string kod, DateTime aySonu, string kullanici, DateTime? simdi = null)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return null; aySonu = aySonu.Date;
            List<MtHareket> fat; lock (MutabakatOrnek.Kilit) fat = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Kalan > 0.005m && !TahsilatOrnek.TlMi(h.ParaBirimi) && (h.IslemTipi ?? "X") == "R" && h.Tarih.Date <= aySonu).ToList();
            var kal = new List<KdFatura>();
            foreach (var f in fat) { var baz = BazKur(kod, f); if (baz.Tarih.HasValue && baz.Tarih.Value >= aySonu) continue; var yeni = TahsilatOrnek.KurOto(f.ParaBirimi, aySonu); var fark = Math.Round(f.Kalan * (yeni - baz.Kur), 2); if (Math.Abs(fark) < 0.01m) continue; kal.Add(new KdFatura { BelgeNo = f.BelgeNo, Pb = f.ParaBirimi, Kalan = f.Kalan, IslemTipi = "R", BazKur = baz.Kur, BazKaynak = baz.Kaynak, YeniKur = yeni, Fark = fark }); }
            if (kal.Count == 0) return null;
            decimal toplam = Math.Round(kal.Sum(x => x.Fark), 2); var z = simdi ?? DateTime.Now; string no; int id; lock (Kilit) { id = ++_sayac; no = $"KF-{aySonu:yyyy}-{aySonu:MM}-{id:000}"; }
            var h = new MtHareket { Tarih = aySonu, ParaBirimi = "TL", BelgeTipi = "Kur Farkı", BelgeNo = no, IslemTipi = "R", KapamaDurumu = "—", Borc = toplam > 0 ? toplam : 0, Alacak = toplam < 0 ? -toplam : 0,
                Aciklama = $"Dönem sonu kur değerlemesi · {aySonu.ToString("MMMM yyyy", Tr)} · muhasebe virmanı · {kal.Count} R fatura · {string.Join(", ", kal.GroupBy(x => x.Pb).Select(g => g.Key + " " + g.First().YeniKur.ToString("N4", Tr)))}" };
            MutabakatOrnek.HareketEkle(kod, h);
            var d = new KurDegerleme { Id = id, DekontNo = no, CariKodu = kod, CariAd = c.Kart?.KisaAd ?? kod, Donem = aySonu.ToString("yyyy-MM"), Tarih = aySonu, Toplam = toplam, Kullanici = kullanici ?? "", Zaman = z, HareketNo = no, FisNo = $"YEV-{aySonu:yyyy}-KF{id:0000}", Faturalar = kal };
            lock (Kilit) _l.Add(d); foreach (var x in kal) _son[kod + "|" + x.BelgeNo] = (x.YeniKur, aySonu, no);
            return d;
        }
        /// <summary>Örnek veri: son iki ay sonunda bütün müşteri carilerinde muhasebe değerlemesi yapılmış sayılır (TahsilatOrnek.Tohumla içinden, vade farkı kesiminden önce).</summary>
        public static void Tohumla()
        {
            if (_tohumlandi) return; lock (Kilit) { if (_tohumlandi) return; _tohumlandi = true; }
            var b = DateTime.Today; var sonAy = new DateTime(b.Year, b.Month, 1).AddDays(-1); var oncekiAy = new DateTime(sonAy.Year, sonAy.Month, 1).AddDays(-1);
            foreach (var ays in new[] { oncekiAy, sonAy })
                foreach (var c in MutabakatOrnek.Cariler.Where(x => x.Kart?.KartTipi == "Müşteri").ToList())
                    try { Degerle(c.CariKodu, ays, "Muhasebe (Dönem Sonu)", ays.AddHours(18)); } catch { }
        }
    }
}
