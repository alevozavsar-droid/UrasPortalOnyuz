// ARAÇ TAKİP RAPORU · FİLO ÖZETİ (/AracTakip; İK ve İdari İşler › İdari İşler › Araç, Taşınmaz ve Demirbaş) — Data/AracTakipOrnek.cs (+ AracBakimOrnek, SigortaOrnek, SirketDokumanOrnek senkron)
// Uçlar: Veri, Arac, KmGir, Yakit, Hgs, Limit, Fiyat (yakıt fiyatı), Pdf (araç takip özeti), Excel. Ekran: Views/AracTakip/Index.cshtml · betik: wwwroot/js/arac-takip.js
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("AracTakip")]
    public class AracTakipController : Controller
    {
        private string Ad() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

        /// <param name="sekme">ozet | km | yakit | hgs | limitler | hesap</param><param name="plaka">açılışta çekmecede gösterilecek araç</param>
        [HttpGet("")]
        public IActionResult Index(string sekme, string plaka)
        {
            ViewData["Title"] = "Araç Takip Raporu"; ViewBag.Sekme = new[] { "ozet", "km", "yakit", "hgs", "limitler", "hesap" }.Contains(sekme) ? sekme : "ozet"; ViewBag.Plaka = plaka ?? ""; ViewBag.Ben = Ad();
            return View("Index");
        }
        [HttpGet("Veri")]
        public IActionResult Veri()
        {
            var l = AracBakimOrnek.Araclar.Select(AracTakipOrnek.Json).ToList();
            return Json(new { success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(), araclar = l, ozet = AracTakipOrnek.Ozet(l), istasyonlar = AracTakipOrnek.Istasyonlar, sirketler = AracBakimOrnek.Sirketler });
        }
        [HttpGet("Arac")] public IActionResult Arac(string plaka) { var a = AracBakimOrnek.AracBul(plaka ?? ""); return a == null ? Json(new { success = false, message = "Araç bulunamadı." }) : Json(new { success = true, arac = AracTakipOrnek.Json(a) }); }
        [HttpPost("KmGir")] public IActionResult KmGir([FromBody] AtKmIstek i) { var r = AracTakipOrnek.KmGir(i, Ad()); return Json(new { success = r.ok, message = r.mesaj }); }
        [HttpPost("Yakit")] public IActionResult Yakit([FromBody] AtYakitIstek i) { var r = AracTakipOrnek.YakitEkle(i, Ad()); return Json(new { success = r.ok, message = r.mesaj }); }
        [HttpPost("Hgs")] public IActionResult Hgs([FromBody] AtHgsIstek i) { var r = AracTakipOrnek.HgsEkle(i, Ad()); return Json(new { success = r.ok, message = r.mesaj }); }
        [HttpPost("Limit")] public IActionResult Limit([FromBody] AtLimitIstek i) { var r = AracTakipOrnek.LimitKaydet(i, Ad()); return Json(new { success = r.ok, message = r.mesaj }); }
        public class FiyatIstek { public decimal? Benzin { get; set; } public decimal? Dizel { get; set; } }
        [HttpPost("Fiyat")] public IActionResult Fiyat([FromBody] FiyatIstek i) { if (i?.Benzin > 0) AracTakipOrnek.BenzinFiyati = i.Benzin.Value; if (i?.Dizel > 0) AracTakipOrnek.DizelFiyati = i.Dizel.Value; return Json(new { success = true, message = $"Yakıt fiyatları güncellendi: benzin {AracTakipOrnek.BenzinFiyati:N2} ₺ · dizel {AracTakipOrnek.DizelFiyati:N2} ₺." }); }

        [HttpGet("Pdf")]
        public IActionResult Pdf(string plaka)
        {
            var a = AracBakimOrnek.AracBul(plaka ?? ""); if (a == null) return NotFound("Araç bulunamadı."); dynamic j = AracTakipOrnek.Json(a); var t = AracTakipOrnek.Bul(a.Plaka);
            var d = new DaBelge { Baslik = $"Araç Takip Özeti · {a.Plaka}", AltBaslik = $"{a.Marka} {a.Model} ({a.Yil}) · {a.Sirket} · zimmet {a.Zimmet} · {a.Departman}", Sirket = a.Sirket, Hazirlayan = Ad(), Yatay = false };
            d.Ozet.Add(("Km", a.Km.ToString("N0", Tr))); d.Ozet.Add(("Kasko", j.sigorta.kasko == null ? "YOK" : (string)j.sigorta.kasko.bitis)); d.Ozet.Add(("Trafik", j.sigorta.trafik == null ? "YOK" : (string)j.sigorta.trafik.bitis)); d.Ozet.Add(("Bakım", (string)j.bakim.durum)); d.Ozet.Add(("Muayene", a.MuayeneBitis.ToString("dd.MM.yyyy"))); d.Ozet.Add(("Uyarı", ((List<string>)j.uyarilar).Count.ToString()));
            var y = new DaBlok { Baslik = "Yakıt · HGS · Ev–İş Hesabı", Kolonlar = { "Alan", "Değer" } };
            foreach (var (k, v) in new[] { ("Tüketim (beklenen)", $"{t.Tuketim:0.#} L / 100 km"), ("Gerçek tüketim (son 30 gün)", j.gercekTuketim == null ? "—" : $"{(decimal)j.gercekTuketim:0.#} L / 100 km"), ("Son 30 gün km", j.son30Km == null ? "—" : $"{(int)j.son30Km:N0} km"), ("Yakıt limiti", $"{t.YakitLimitLitre:0} L / ay · {(decimal)j.yakitLimitTl:N0} ₺"), ("Son 30 gün yakıt", $"{(decimal)j.son30Litre:0.#} L · {(decimal)j.son30Tl:N0} ₺ · %{(decimal)j.yakitKullanim}"), ("HGS limiti", $"{t.HgsLimitTl:N0} ₺ / ay"), ("HGS bu ay", $"{(decimal)j.hgsBuAy:N0} ₺ · {(int)j.hgsGecis} geçiş · %{(decimal)j.hgsKullanim}"), ("Ev–iş mesafesi", $"{t.EvIsKm:0.#} km (tek yön) · {t.CalismaGunu} gün"), ("30 günde ev–iş", $"{(decimal)j.evIsAylikKm:N0} km → {(decimal)j.evIsLitre:0.#} L → {(decimal)j.evIsTl:N0} ₺"), ("Uyarılar", string.Join(" · ", (List<string>)j.uyarilar)) }) y.Satirlar.Add(new[] { k, v }); d.Bloklar.Add(y);
            var km = new DaBlok { Baslik = "Km Girişleri", Kolonlar = { "Tarih", "Km", "Giren", "Not" }, Sag = { false, true, false, false } }; foreach (var x in t.KmKayitlari.OrderByDescending(x => x.Tarih).Take(12)) km.Satirlar.Add(new[] { x.Tarih.ToString("dd.MM.yyyy"), x.Km.ToString("N0", Tr), x.Giren, x.Not }); d.Bloklar.Add(km);
            var yk = new DaBlok { Baslik = "Yakıt Alımları (Son 60 Gün)", Kolonlar = { "Tarih", "Litre", "Tutar (₺)", "İstasyon", "Km" }, Sag = { false, true, true, false, true } }; foreach (var x in t.YakitAlimlari.Take(20)) yk.Satirlar.Add(new[] { x.Tarih.ToString("dd.MM.yyyy"), x.Litre.ToString("0.#", Tr), x.Tutar.ToString("N2", Tr), x.Istasyon, x.Km.ToString("N0", Tr) }); yk.Toplam = new[] { "Toplam", t.YakitAlimlari.Sum(x => x.Litre).ToString("0.#", Tr), t.YakitAlimlari.Sum(x => x.Tutar).ToString("N2", Tr), "", "" }; d.Bloklar.Add(yk);
            return File(DisaAktar.Pdf(d), "application/pdf", $"arac-takip-{a.Plaka.Replace(" ", "")}.pdf");
        }
        [HttpGet("Excel")]
        public IActionResult Excel()
        {
            var l = AracBakimOrnek.Araclar.Select(a => (a, j: (dynamic)AracTakipOrnek.Json(a))).ToList(); var d = new DaBelge { Baslik = "Araç Takip Raporu · Filo Özeti", AltBaslik = $"{l.Count} araç · {DateTime.Today:dd.MM.yyyy}", Sirket = "Uras Grup", Hazirlayan = Ad() };
            var bl = new DaBlok { Baslik = "Araçlar", Kolonlar = { "Plaka", "Araç", "Şirket", "Zimmet", "Departman", "Km", "Son Km Girişi", "Kasko Bitiş", "Trafik Bitiş", "Bakım Durumu", "Sonraki Bakım Km", "Muayene", "Tüketim L/100", "Yakıt Limit L", "Son 30 Gün L", "Son 30 Gün ₺", "HGS Limit ₺", "HGS Bu Ay ₺", "Ev–İş Km", "30 Gün Ev–İş L", "30 Gün Ev–İş ₺", "Uyarılar" }, Sag = { false, false, false, false, false, true, false, false, false, false, true, false, true, true, true, true, true, true, true, true, true, false } };
            foreach (var (a, j) in l) bl.Satirlar.Add(new[] { a.Plaka, $"{a.Marka} {a.Model}", a.Sirket, a.Zimmet, a.Departman, a.Km.ToString("N0", Tr), j.sonKm == null ? "" : (string)j.sonKm.tarih, j.sigorta.kasko == null ? "YOK" : (string)j.sigorta.kasko.bitis, j.sigorta.trafik == null ? "YOK" : (string)j.sigorta.trafik.bitis, (string)j.bakim.durum, j.bakim.sonrakiKm == null ? "" : ((int)j.bakim.sonrakiKm).ToString("N0", Tr), a.MuayeneBitis.ToString("dd.MM.yyyy"), ((decimal)j.tuketim).ToString("0.#", Tr), ((decimal)j.yakitLimitLitre).ToString("0", Tr), ((decimal)j.son30Litre).ToString("0.#", Tr), ((decimal)j.son30Tl).ToString("N0", Tr), ((decimal)j.hgsLimitTl).ToString("N0", Tr), ((decimal)j.hgsBuAy).ToString("N0", Tr), ((decimal)j.evIsKm).ToString("0.#", Tr), ((decimal)j.evIsLitre).ToString("0.#", Tr), ((decimal)j.evIsTl).ToString("N0", Tr), string.Join(" · ", (List<string>)j.uyarilar) });
            d.Bloklar.Add(bl); return File(DisaAktar.Excel(d), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"arac-takip-{DateTime.Today:yyyyMMdd}.xlsx");
        }
    }
}
