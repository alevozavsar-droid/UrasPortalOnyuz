using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Otomatik Kapatma uçları (Views/Rapor64/Index.cshtml, wwwroot/js/otomatik-kapatma.js; kurallar Data/KapamaOrnek.cs):
    /// tüm carilerde açık fatura ↔ eşleştirilmemiş ödeme / tahsilat / çek / senet; otomatik öneri (birebir + FIFO), elle seçerek kapama, tek tıkla tüm carilerde otomatik kapama, kapama kayıtları.
    /// </summary>
    [Authorize]
    public class KapamaController : Controller
    {
        private static readonly CultureInfo Tr = CariKartOrnek.Tr;
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static decimal KurTl(string pb) => pb == "TL" || pb == "TRY" || string.IsNullOrEmpty(pb) ? 1m : (MutabakatOrnek.Kurlar.TryGetValue(pb, out var x) ? x : 1m);
        public class OnerIstek { public string Kod { get; set; } public List<string> Faturalar { get; set; } public List<string> Odemeler { get; set; } }
        public class KapatIstek { public string Kod { get; set; } public string Mod { get; set; } = "Elle"; public List<KapamaCifti> Ciftler { get; set; } }
        public class TopluIstek { public List<string> Kodlar { get; set; } }

        static object HareketDto(MtHareket h, DateTime bugun) => new { h.Id, h.BelgeNo, h.BelgeTipi, tarih = Trh(h.Tarih), tarihIso = h.Tarih.ToString("yyyy-MM-dd"), vade = Trh(h.Vade), gecikme = h.Vade.HasValue ? Math.Max(0, (bugun - h.Vade.Value.Date).Days) : 0, pb = h.ParaBirimi, tutar = h.Borc > 0 ? h.Borc : h.Alacak, h.Kalan, kalanTr = h.Kalan.ToString("N2", Tr), kalanTl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), h.IslemTipi, h.KapamaDurumu, h.Aciklama };
        static object CariDto(MtCari c)
        {
            var fat = KapamaOrnek.AcikFaturalar(c); var ode = KapamaOrnek.AcikOdemeler(c); var k = c.Kart; var oner = fat.Count > 0 && ode.Count > 0 ? KapamaOrnek.Oner(c) : new List<KapamaCifti>();
            decimal fatTl = fat.Sum(f => f.Kalan * KurTl(f.ParaBirimi)), odeTl = ode.Sum(o => o.Kalan * KurTl(o.ParaBirimi)), kap = KapamaOrnek.Kapatilabilir(c);
            return new { suzgec = k == null ? null : CariSuzgec.Alanlar(k), kod = c.CariKodu, kisaAd = k?.KisaAd ?? c.CariKodu, resmiUnvan = k?.ResmiUnvan ?? "", tip = k?.KartTipi ?? "", sirket = k?.Sirket ?? "", bolge = k?.Bolge ?? "", vergiNo = k?.VergiNo ?? "", yetkili = k?.Yetkili ?? "", eposta = k?.Eposta ?? "", bakiyeTl = c.BakiyeTl, acikFatura = fat.Count, acikFaturaTl = Math.Round(fatTl, 2), acikOdeme = ode.Count, acikOdemeTl = Math.Round(odeTl, 2), kapatilabilir = kap, oneri = oner.Count, oneriTl = Math.Round(oner.Sum(o => o.Tutar * KurTl(o.Pb)), 2), birebir = oner.Count(o => o.Yontem == "Birebir"),
                durum = oner.Count > 0 ? "Kapatılabilir" : ode.Count > 0 ? "Ödeme Bekliyor" : fat.Count > 0 ? "Yalnız Açık Fatura" : "Temiz", pbler = fat.Concat(ode).Select(h => h.ParaBirimi).Distinct().OrderBy(p => p == "TL" ? "" : p).ToArray(), sonKapama = c.Log.Where(l => l.Islem == "Kapama").Select(l => (DateTime?)l.Zaman).FirstOrDefault()?.ToString("dd.MM.yyyy HH:mm", Tr) ?? "" };
        }
        static object KayitDto(KapamaKaydi k) => new { k.Id, zaman = k.Zaman.ToString("dd.MM.yyyy HH:mm", Tr), k.CariKodu, k.CariAd, k.Mod, adet = k.Ciftler.Count, toplamTl = Math.Round(k.Ciftler.Sum(c => c.Tutar * KurTl(c.Pb)), 2), ozet = string.Join(" · ", k.Ciftler.GroupBy(c => c.Pb).Select(g => g.Sum(c => c.Tutar).ToString("N2", Tr) + " " + g.Key)), k.Kullanici, ciftler = k.Ciftler.Select(c => new { c.FaturaNo, c.OdemeNo, c.Tutar, tutarTr = c.Tutar.ToString("N2", Tr) + " " + c.Pb, c.Pb, c.Yontem }) };

        /// <summary>Tüm cariler: açık fatura / eşleştirilmemiş ödeme sayı ve tutarları, kapatılabilir tutar, öneri adedi, durum; kapama kayıtları.</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            List<object> cariler; lock (MutabakatOrnek.Kilit) cariler = MutabakatOrnek.Cariler.Select(CariDto).ToList();
            return Json(new { success = true, ben = Ben(), bugun = DateTime.Today.ToString("yyyy-MM-dd"), kurlar = MutabakatOrnek.Kurlar, secenekler = CariSuzgec.Secenekler(), cariler, kayitlar = KapamaOrnek.Kayitlar.Select(KayitDto) });
        }
        /// <summary>Cari detayı: açık faturalar ve eşleştirilmemiş ödemeler (kalan, para birimi, vade, gecikme) + otomatik öneri.</summary>
        [HttpGet]
        public IActionResult Detay(string kod)
        {
            var c = MutabakatOrnek.Bul(kod ?? ""); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." }); var bugun = DateTime.Today;
            lock (MutabakatOrnek.Kilit) return Json(new { success = true, cari = CariDto(c), faturalar = KapamaOrnek.AcikFaturalar(c).Select(h => HareketDto(h, bugun)), odemeler = KapamaOrnek.AcikOdemeler(c).Select(h => HareketDto(h, bugun)), oneri = KapamaOrnek.Oner(c).Select(o => new { o.FaturaNo, o.OdemeNo, o.Tutar, o.Pb, o.Yontem }), log = c.Log.Where(l => l.Islem == "Kapama").Take(10).Select(l => new { zaman = l.Zaman.ToString("dd.MM.yyyy HH:mm", Tr), l.Kullanici, l.Yeni, l.Aciklama }) });
        }
        /// <summary>Seçilen fatura / ödemeler için eşleştirme önerisi (kaydetmez). Liste boşsa tümü.</summary>
        [HttpPost]
        public IActionResult Oner([FromBody] OnerIstek i)
        {
            var c = MutabakatOrnek.Bul(i?.Kod ?? ""); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." });
            var fs = i.Faturalar != null && i.Faturalar.Count > 0 ? i.Faturalar : null; var os = i.Odemeler != null && i.Odemeler.Count > 0 ? i.Odemeler : null;
            lock (MutabakatOrnek.Kilit) { var l = KapamaOrnek.Oner(c, fs, os); return Json(new { success = true, ciftler = l.Select(o => new { o.FaturaNo, o.OdemeNo, o.Tutar, o.Pb, o.Yontem }), toplamTl = Math.Round(l.Sum(o => o.Tutar * KurTl(o.Pb)), 2), message = l.Count == 0 ? "Seçimde aynı para biriminde eşleşebilecek fatura–ödeme yok." : $"{l.Count} eşleşme önerildi ({l.Count(o => o.Yontem == "Birebir")} birebir, {l.Count(o => o.Yontem == "FIFO")} FIFO)." }); }
        }
        /// <summary>Kapatmayı uygular (önerilen ya da elle düzenlenmiş çiftler).</summary>
        [HttpPost]
        public IActionResult Kapat([FromBody] KapatIstek i)
        {
            if (i == null) return Json(new { success = false, message = "Geçersiz istek." });
            var h = KapamaOrnek.Kapat(i.Kod, i.Ciftler, i.Mod ?? "Elle", Ben(), out var k); if (h != null) return Json(new { success = false, message = h });
            return Json(new { success = true, message = $"{k.CariKodu} {k.CariAd}: {k.Ciftler.Count} eşleşme kapatıldı · {string.Join(" · ", k.Ciftler.GroupBy(c => c.Pb).Select(g => g.Sum(c => c.Tutar).ToString("N2", Tr) + " " + g.Key))}.", kayit = KayitDto(k), cari = CariDto(MutabakatOrnek.Bul(i.Kod)) });
        }
        /// <summary>Tüm carilerde (ya da verilen listede) otomatik kapatma.</summary>
        [HttpPost]
        public IActionResult TumunuKapat([FromBody] TopluIstek i)
        {
            var kodlar = i?.Kodlar != null && i.Kodlar.Count > 0 ? i.Kodlar : MutabakatOrnek.Cariler.Select(c => c.CariKodu).ToList();
            var r = KapamaOrnek.TumunuKapat(kodlar, Ben());
            return Json(new { success = true, message = r.Cari == 0 ? "Kapatılacak eşleşme bulunamadı." : $"{r.Cari} caride {r.Eslesme} fatura–ödeme eşleşmesi kapatıldı · {r.ToplamTl.ToString("N2", Tr)} ₺.", hatalar = r.Hatalar, cari = r.Cari, eslesme = r.Eslesme, toplamTl = r.ToplamTl });
        }
    }
}
