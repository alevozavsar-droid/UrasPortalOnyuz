// ============================================================
// ARAÇ TAKİP RAPORU (FİLO ÖZETİ) — İdari İşler (İK ve İdari İşler › İdari İşler › Araç, Taşınmaz ve Demirbaş; /AracTakip)
// Tüm araçlar tek tabloda ve SENKRON: kayıtlı şirket / zimmet / km (Data/AracBakimOrnek.cs), kasko ve trafik poliçesi (Data/SigortaOrnek.cs), bakım durumu ve muayene (AracBakimOrnek),
// ruhsat / muayene belgeleri (Data/SirketDokumanOrnek.cs). Bu ekranda ek olarak idari işler verisi tutulur: KM GİRİŞLERİ (aylık / istenildiğinde; araç km'si otomatik güncellenir),
// YAKIT (tüketim L/100 km, aylık litre ve ₺ limiti, yakıt alımları), HGS (aylık limit, aylık geçiş tutarı), EV–İŞ MESAFESİ → 30 günde (çalışma günü × 2 × mesafe) kaç litre /
// kaç ₺ yakıt hesabı; limit aşımları işaretlenir. Ekran: Controllers/AracTakipController.cs + Views/AracTakip/Index.cshtml + wwwroot/js/arac-takip.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class AtKm { public DateTime Tarih { get; set; } public int Km { get; set; } public string Giren { get; set; } public string Not { get; set; } = ""; }
    public class AtYakit { public DateTime Tarih { get; set; } public decimal Litre { get; set; } public decimal Tutar { get; set; } public string Istasyon { get; set; } = ""; public int Km { get; set; } public string Giren { get; set; } }
    public class AtHgs { public string Ay { get; set; } public decimal Tutar { get; set; } public int Gecis { get; set; } public string Giren { get; set; } }
    public class AtArac
    {
        public string Plaka { get; set; } public decimal Tuketim { get; set; } public decimal YakitLimitLitre { get; set; } public decimal HgsLimitTl { get; set; } public decimal EvIsKm { get; set; } public int CalismaGunu { get; set; } = 22; public string HgsEtiket { get; set; } = ""; public string YakitKarti { get; set; } = ""; public string Not { get; set; } = "";
        public List<AtKm> KmKayitlari { get; set; } = new List<AtKm>(); public List<AtYakit> YakitAlimlari { get; set; } = new List<AtYakit>(); public List<AtHgs> HgsAylari { get; set; } = new List<AtHgs>();
    }
    public class AtKmIstek { public string Plaka { get; set; } public int Km { get; set; } public string Tarih { get; set; } public string Not { get; set; } }
    public class AtYakitIstek { public string Plaka { get; set; } public string Tarih { get; set; } public decimal Litre { get; set; } public decimal Tutar { get; set; } public string Istasyon { get; set; } public int? Km { get; set; } }
    public class AtHgsIstek { public string Plaka { get; set; } public string Ay { get; set; } public decimal Tutar { get; set; } public int Gecis { get; set; } }
    public class AtLimitIstek { public string Plaka { get; set; } public decimal? Tuketim { get; set; } public decimal? YakitLimitLitre { get; set; } public decimal? HgsLimitTl { get; set; } public decimal? EvIsKm { get; set; } public int? CalismaGunu { get; set; } public string HgsEtiket { get; set; } public string YakitKarti { get; set; } public string Not { get; set; } }

    public static class AracTakipOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public static decimal BenzinFiyati = 52.80m, DizelFiyati = 54.10m;   // ₺ / L (örnek; İdari İşler günceller)
        private static readonly Dictionary<string, AtArac> _araclar = new Dictionary<string, AtArac>(StringComparer.OrdinalIgnoreCase);
        private static bool _tohumlandi;
        public static decimal YakitFiyati(ArArac a) => (a?.Yakit ?? "").Contains("Dizel") ? DizelFiyati : BenzinFiyati;
        public static readonly string[] Istasyonlar = { "Opet Çorlu", "Shell Velimeşe", "BP Çerkezköy", "Petrol Ofisi Gebze", "Total Kadıköy" };

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var rnd = new Random(20261103); var bugun = DateTime.Today; int idx = 0;
                foreach (var a in AracBakimOrnek.Araclar)
                {
                    decimal tuketim = a.Tip == "Tır" ? 32m : a.Tip == "Servis" || a.Tip == "Minibüs" ? 11.5m : a.Tip == "Kamyonet" || a.Tip == "Panelvan" ? 8.6m : a.Tip == "Pickup" ? 9.8m : a.Yakit.Contains("Hibrit") ? 4.9m : a.Yakit.Contains("Dizel") ? 5.6m : 7.2m;
                    decimal evIs = a.Tip == "Tır" || a.Tip == "Servis" ? 0 : new[] { 12m, 28m, 8m, 35m, 18m, 22m, 45m, 15m, 30m, 10m, 25m, 40m }[idx % 12]; int calisma = 22;
                    decimal isKm = a.Tip == "Tır" ? 9000 : a.Tip == "Servis" ? 2200 : a.Tip == "Kamyonet" || a.Tip == "Panelvan" ? 2600 : a.Departman == "Satış" ? 1800 : 500;
                    decimal aylikKmTahmin = evIs * 2 * calisma + isKm; decimal limitL = Math.Ceiling(aylikKmTahmin * tuketim / 100 * 1.15m / 10) * 10;
                    var t = new AtArac { Plaka = a.Plaka, Tuketim = tuketim, YakitLimitLitre = limitL, HgsLimitTl = a.Tip == "Tır" ? 9000 : a.Departman == "Satış" || a.Departman == "Yönetim" ? 3500 : a.Departman == "Lojistik" ? 4500 : 1500, EvIsKm = evIs, CalismaGunu = calisma, HgsEtiket = $"HGS-{rnd.Next(1000000, 9999999)}", YakitKarti = $"OPET-{rnd.Next(100000, 999999)}" };
                    // km kayıtları: son 6 ay, aylık; son kayıt aracın güncel km'si
                    // tarihler: son bildirim (1–5 gün önce) + ay başları; km geriye doğru gün farkıyla orantılı azalır (gerçek tüketim hesabı tutarlı kalsın)
                    var ilk = bugun.AddDays(-rnd.Next(1, 6)); var tarihler = new List<DateTime> { ilk }; for (int i = 0; i < 7 && tarihler.Count < 6; i++) { var ay = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-i).AddDays(rnd.Next(0, 4)); if (ay < ilk.AddDays(-7)) tarihler.Add(ay); }
                    int km = a.Km; var liste = new List<AtKm>(); for (int i = 0; i < tarihler.Count; i++) { liste.Add(new AtKm { Tarih = tarihler[i], Km = km, Giren = i == 0 ? "İdari İşler · Serkan Genç" : "Filo · Hakan Yıldız", Not = i == 0 ? "aylık km bildirimi" : "" }); if (i + 1 < tarihler.Count) km -= (int)(aylikKmTahmin * (decimal)(tarihler[i] - tarihler[i + 1]).TotalDays / 30 * (0.9m + (decimal)rnd.NextDouble() * 0.2m)); } t.KmKayitlari = liste.OrderBy(x => x.Tarih).ToList();
                    // yakıt alımları: son 60 gün
                    decimal fiyat = YakitFiyati(a); decimal aylikL = aylikKmTahmin * tuketim / 100; int alim = Math.Max(2, (int)Math.Round(aylikL / (a.Tip == "Tır" ? 350 : 45)));
                    int aralik = Math.Max(1, 60 / (alim * 2)); decimal birim = aylikL / alim * 0.8m * (idx % 4 == 1 ? 1.65m : 1m);   // alımlar 60 güne eşit aralıkla dağılır; idx % 4 == 1 olan araçlar limiti aşar (örnek uyarı), diğerleri limitin ~%70'inde
                    for (int i = 0; i < alim * 2; i++) { var tar = bugun.AddDays(-(1 + i * aralik + rnd.Next(0, Math.Max(1, aralik / 2)))); decimal L = Math.Round(birim * (0.85m + (decimal)rnd.NextDouble() * 0.3m), 1); t.YakitAlimlari.Add(new AtYakit { Tarih = tar, Litre = L, Tutar = Math.Round(L * fiyat, 2), Istasyon = Istasyonlar[rnd.Next(Istasyonlar.Length)], Km = a.Km - (int)((decimal)(bugun - tar).TotalDays * aylikKmTahmin / 30), Giren = "Yakıt Kartı (otomatik)" }); }
                    t.YakitAlimlari = t.YakitAlimlari.OrderByDescending(x => x.Tarih).ToList();
                    // HGS: son 3 ay
                    for (int i = 0; i < 3; i++) { var ay = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-i); decimal oran = idx % 5 == 2 ? 1.3m : 0.55m + (decimal)rnd.NextDouble() * 0.4m; t.HgsAylari.Add(new AtHgs { Ay = ay.ToString("yyyy-MM"), Tutar = Math.Round(t.HgsLimitTl * oran * (i == 0 ? (decimal)bugun.Day / DateTime.DaysInMonth(bugun.Year, bugun.Month) + 0.1m : 1m), 2), Gecis = rnd.Next(10, 80), Giren = "HGS Ekstresi (otomatik)" }); }
                    _araclar[a.Plaka] = t; idx++;
                }
            }
        }
        public static AtArac Bul(string plaka) { Tohumla(); lock (Kilit) return _araclar.TryGetValue(plaka ?? "", out var t) ? t : null; }

        public static (bool ok, string mesaj) KmGir(AtKmIstek i, string kullanici)
        {
            var a = AracBakimOrnek.AracBul(i?.Plaka ?? ""); var t = Bul(i?.Plaka ?? ""); if (a == null || t == null) return (false, "Araç bulunamadı.");
            if (i.Km <= 0) return (false, "Km girin."); var tar = DateTime.TryParse(i.Tarih, out var d) ? d.Date : DateTime.Today; if (tar > DateTime.Today) return (false, "Tarih bugünden sonra olamaz.");
            var son = t.KmKayitlari.OrderByDescending(x => x.Tarih).FirstOrDefault(); if (son != null && tar >= son.Tarih && i.Km < son.Km) return (false, $"Km son kayıttan ({son.Km:N0} km · {son.Tarih:dd.MM.yyyy}) küçük olamaz.");
            lock (Kilit) t.KmKayitlari.Add(new AtKm { Tarih = tar, Km = i.Km, Giren = kullanici, Not = (i.Not ?? "").Trim() });
            if (i.Km > a.Km) AracBakimOrnek.AracGuncelle(new ArAracIstek { Plaka = a.Plaka, Km = i.Km }, kullanici);   // senkron: Araç Bakım ekranındaki km de güncellenir
            return (true, $"{a.Plaka}: {i.Km:N0} km kaydedildi ({tar:dd.MM.yyyy}); Araç Bakım ve sonraki bakım hesabı güncellendi.");
        }
        public static (bool ok, string mesaj) YakitEkle(AtYakitIstek i, string kullanici)
        {
            var a = AracBakimOrnek.AracBul(i?.Plaka ?? ""); var t = Bul(i?.Plaka ?? ""); if (a == null || t == null) return (false, "Araç bulunamadı."); if (i.Litre <= 0) return (false, "Litre girin.");
            var tar = DateTime.TryParse(i.Tarih, out var d) ? d.Date : DateTime.Today; decimal tutar = i.Tutar > 0 ? i.Tutar : Math.Round(i.Litre * YakitFiyati(a), 2);
            lock (Kilit) { t.YakitAlimlari.Insert(0, new AtYakit { Tarih = tar, Litre = i.Litre, Tutar = tutar, Istasyon = (i.Istasyon ?? "").Trim(), Km = i.Km ?? a.Km, Giren = kullanici }); t.YakitAlimlari = t.YakitAlimlari.OrderByDescending(x => x.Tarih).ToList(); }
            if (i.Km.HasValue && i.Km > a.Km) AracBakimOrnek.AracGuncelle(new ArAracIstek { Plaka = a.Plaka, Km = i.Km }, kullanici);
            return (true, $"{a.Plaka}: {i.Litre:0.#} L · {tutar.ToString("N2", Tr)} ₺ yakıt alımı kaydedildi.");
        }
        public static (bool ok, string mesaj) HgsEkle(AtHgsIstek i, string kullanici)
        {
            var t = Bul(i?.Plaka ?? ""); if (t == null) return (false, "Araç bulunamadı."); if (string.IsNullOrEmpty(i.Ay) || i.Ay.Length != 7) return (false, "Ay (YYYY-AA) girin.");
            lock (Kilit) { var m = t.HgsAylari.FirstOrDefault(x => x.Ay == i.Ay); if (m == null) t.HgsAylari.Add(new AtHgs { Ay = i.Ay, Tutar = i.Tutar, Gecis = i.Gecis, Giren = kullanici }); else { m.Tutar = i.Tutar; m.Gecis = i.Gecis; m.Giren = kullanici; } }
            return (true, $"{i.Plaka}: {i.Ay} HGS {i.Tutar.ToString("N2", Tr)} ₺ ({i.Gecis} geçiş) kaydedildi.");
        }
        public static (bool ok, string mesaj) LimitKaydet(AtLimitIstek i, string kullanici)
        {
            var t = Bul(i?.Plaka ?? ""); if (t == null) return (false, "Araç bulunamadı.");
            lock (Kilit) { if (i.Tuketim.HasValue && i.Tuketim > 0) t.Tuketim = i.Tuketim.Value; if (i.YakitLimitLitre.HasValue && i.YakitLimitLitre >= 0) t.YakitLimitLitre = i.YakitLimitLitre.Value; if (i.HgsLimitTl.HasValue && i.HgsLimitTl >= 0) t.HgsLimitTl = i.HgsLimitTl.Value; if (i.EvIsKm.HasValue && i.EvIsKm >= 0) t.EvIsKm = i.EvIsKm.Value; if (i.CalismaGunu.HasValue && i.CalismaGunu > 0 && i.CalismaGunu <= 31) t.CalismaGunu = i.CalismaGunu.Value; if (i.HgsEtiket != null) t.HgsEtiket = i.HgsEtiket.Trim(); if (i.YakitKarti != null) t.YakitKarti = i.YakitKarti.Trim(); if (i.Not != null) t.Not = i.Not.Trim(); }
            return (true, $"{t.Plaka}: limitler güncellendi · tüketim {t.Tuketim:0.#} L/100 km · yakıt {t.YakitLimitLitre:0} L/ay · HGS {t.HgsLimitTl:N0} ₺/ay · ev–iş {t.EvIsKm:0.#} km.");
        }

        /// <summary>Aracın takip özeti: km, yakıt, HGS, ev–iş hesabı, limit aşımları + bakım / sigorta / şirket (senkron).</summary>
        public static object Json(ArArac a)
        {
            var t = Bul(a.Plaka); var bugun = DateTime.Today; decimal fiyat = YakitFiyati(a); string buAy = bugun.ToString("yyyy-MM");
            var kmList = t.KmKayitlari.OrderByDescending(x => x.Tarih).ToList(); var sonKm = kmList.FirstOrDefault(); var onceki = kmList.FirstOrDefault(x => x.Tarih <= bugun.AddDays(-28)); int? son30Km = sonKm != null && onceki != null && sonKm.Tarih > onceki.Tarih ? (int?)(int)Math.Round((sonKm.Km - onceki.Km) * 30.0 / Math.Max(1, (sonKm.Tarih - onceki.Tarih).TotalDays)) : null;
            var y30 = t.YakitAlimlari.Where(x => x.Tarih >= bugun.AddDays(-30)).ToList(); decimal y30L = y30.Sum(x => x.Litre), y30Tl = y30.Sum(x => x.Tutar); decimal? gercekTuketim = son30Km.HasValue && son30Km > 0 && y30L > 0 ? Math.Round(y30L / son30Km.Value * 100, 1) : (decimal?)null;
            decimal evIsAylikKm = t.EvIsKm * 2 * t.CalismaGunu, evIsLitre = Math.Round(evIsAylikKm * t.Tuketim / 100, 1), evIsTl = Math.Round(evIsLitre * fiyat, 2); decimal limitTl = Math.Round(t.YakitLimitLitre * fiyat, 2);
            var hgs = t.HgsAylari.FirstOrDefault(x => x.Ay == buAy); decimal hgsBuAy = hgs?.Tutar ?? 0; var hgsOnceki = t.HgsAylari.FirstOrDefault(x => x.Ay == bugun.AddMonths(-1).ToString("yyyy-MM"));
            dynamic o = AracBakimOrnek.AracOzet(a); var sig = SigortaOrnek.AracPoliceleri(a.Plaka);
            var uyarilar = new List<string>(); if (t.YakitLimitLitre > 0 && y30L > t.YakitLimitLitre) uyarilar.Add($"Yakıt limiti aşıldı: {y30L:0} / {t.YakitLimitLitre:0} L"); if (t.HgsLimitTl > 0 && hgsBuAy > t.HgsLimitTl) uyarilar.Add($"HGS limiti aşıldı: {hgsBuAy:N0} / {t.HgsLimitTl:N0} ₺"); if (gercekTuketim.HasValue && gercekTuketim > t.Tuketim * 1.25m) uyarilar.Add($"Gerçek tüketim yüksek: {gercekTuketim:0.#} L/100 km (beklenen {t.Tuketim:0.#})"); if (((dynamic)sig).kasko == null) uyarilar.Add("Kasko poliçesi yok"); if (((dynamic)sig).trafik == null) uyarilar.Add("Trafik poliçesi yok"); if ((string)o.bakimDurum == "Bakımı Gecikti") uyarilar.Add("Periyodik bakım gecikti"); if ((int)o.muayeneKalan < 0) uyarilar.Add("Muayene gecikti"); if (sonKm == null || sonKm.Tarih < bugun.AddDays(-35)) uyarilar.Add("Aylık km girişi yapılmadı");
            return new
            {
                plaka = a.Plaka, arac = $"{a.Marka} {a.Model}", yil = a.Yil, tip = a.Tip, yakit = a.Yakit, sirket = a.Sirket, zimmet = a.Zimmet, departman = a.Departman, lokasyon = a.Lokasyon, durum = a.Durum, km = a.Km, muayeneBitis = a.MuayeneBitis.ToString("yyyy-MM-dd"),
                bakim = new { durum = (string)o.bakimDurum, sonrakiKm = (int?)o.sonrakiBakimKm, sonrakiTarih = (string)o.sonrakiBakimTarihi, kalanKm = (int?)o.kalanKm, kalanGun = (int?)o.kalanGun, muayeneKalan = (int)o.muayeneKalan, muayeneDurum = (string)o.muayeneDurum, yilMaliyet = (decimal)o.yilMaliyet, toplamMaliyet = (decimal)o.toplamMaliyet, acik = (int)o.acik }, sigorta = sig,
                belgeler = new { ruhsatUrl = "/SirketDokuman/Varlik?sirket=" + (SirketDokumanOrnek.Sirketler.FirstOrDefault(s => s.Ad == a.Sirket || s.Ad.Replace(" Kimya", "") == a.Sirket)?.Kod ?? ""), ruhsatVar = SirketDokumanOrnek.Belgeler.Any(b => b.TurKod == "arac-ruhsat" && !b.Arsiv && SirketDokumanOrnek.SirketBul(b.SirketKod)?.Ad == a.Sirket) },
                sonKm = sonKm == null ? null : new { tarih = sonKm.Tarih.ToString("yyyy-MM-dd"), km = sonKm.Km, giren = sonKm.Giren, gunOnce = (bugun - sonKm.Tarih).Days }, son30Km, kmKayitlari = kmList.Take(12).Select(x => new { tarih = x.Tarih.ToString("yyyy-MM-dd"), x.Km, x.Giren, x.Not }).ToList(),
                tuketim = t.Tuketim, yakitFiyati = fiyat, yakitLimitLitre = t.YakitLimitLitre, yakitLimitTl = limitTl, son30Litre = y30L, son30Tl = y30Tl, yakitKullanim = t.YakitLimitLitre > 0 ? Math.Round(y30L / t.YakitLimitLitre * 100) : 0, gercekTuketim, yakitAlimlari = t.YakitAlimlari.Take(15).Select(x => new { tarih = x.Tarih.ToString("yyyy-MM-dd"), x.Litre, x.Tutar, x.Istasyon, x.Km, x.Giren }).ToList(), yakitKarti = t.YakitKarti,
                hgsLimitTl = t.HgsLimitTl, hgsBuAy, hgsGecis = hgs?.Gecis ?? 0, hgsKullanim = t.HgsLimitTl > 0 ? Math.Round(hgsBuAy / t.HgsLimitTl * 100) : 0, hgsOnceki = hgsOnceki?.Tutar ?? 0, hgsAylari = t.HgsAylari.OrderByDescending(x => x.Ay).Take(6).Select(x => new { x.Ay, x.Tutar, x.Gecis, x.Giren }).ToList(), hgsEtiket = t.HgsEtiket,
                evIsKm = t.EvIsKm, calismaGunu = t.CalismaGunu, evIsAylikKm, evIsLitre, evIsTl, evIsPay = t.YakitLimitLitre > 0 ? Math.Round(evIsLitre / t.YakitLimitLitre * 100) : 0, not = t.Not, uyarilar
            };
        }
        public static object Ozet(List<object> l)
        {
            var d = l.Cast<dynamic>().ToList();
            return new { arac = d.Count, kaskoYok = d.Count(x => x.sigorta.kasko == null), trafikYok = d.Count(x => x.sigorta.trafik == null), bakimGeciken = d.Count(x => (string)x.bakim.durum == "Bakımı Gecikti"), muayeneYaklasan = d.Count(x => (int)x.bakim.muayeneKalan <= 30), yakitAsan = d.Count(x => (decimal)x.yakitLimitLitre > 0 && (decimal)x.son30Litre > (decimal)x.yakitLimitLitre), hgsAsan = d.Count(x => (decimal)x.hgsLimitTl > 0 && (decimal)x.hgsBuAy > (decimal)x.hgsLimitTl), kmEksik = d.Count(x => x.sonKm == null || (int)x.sonKm.gunOnce > 35), yakitLimitTl = d.Sum(x => (decimal)x.yakitLimitTl), son30Tl = d.Sum(x => (decimal)x.son30Tl), hgsLimit = d.Sum(x => (decimal)x.hgsLimitTl), hgsBuAy = d.Sum(x => (decimal)x.hgsBuAy), evIsTl = d.Sum(x => (decimal)x.evIsTl), uyari = d.Sum(x => (int)((System.Collections.Generic.List<string>)x.uyarilar).Count), benzin = BenzinFiyati, dizel = DizelFiyati };
        }
    }
}
