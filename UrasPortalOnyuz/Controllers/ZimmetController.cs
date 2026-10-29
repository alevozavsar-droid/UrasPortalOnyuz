// ZİMMET RAPORU (/Zimmet; İK ve İdari İşler › IT › Zimmet ve Envanter) — Data/ZimmetOrnek.cs
// Uçlar: Veri, Varlik, Kaydet (JSON), ZimmetVer, Iade, DosyaYukle (multipart), Dosya, Tutanak (kişi zimmet tutanağı PDF), Excel. Ekran: Views/Zimmet/Index.cshtml · betik: wwwroot/js/zimmet.js
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("Zimmet")]
    public class ZimmetController : Controller
    {
        private string Ad() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

        /// <param name="sekme">varliklar | kisiler | depo | yeni | gunluk</param><param name="personel">ön süzgeç: personel no</param><param name="id">açılışta çekmecede gösterilecek varlık</param>
        [HttpGet("")]
        public IActionResult Index(string sekme, string personel, int? id)
        {
            ViewData["Title"] = "Zimmet Raporu"; ViewBag.Sekme = new[] { "varliklar", "kisiler", "depo", "yeni", "gunluk" }.Contains(sekme) ? sekme : "varliklar"; ViewBag.Personel = personel ?? ""; ViewBag.Id = id?.ToString() ?? ""; ViewBag.Ben = Ad();
            return View("Index");
        }
        [HttpGet("Veri")]
        public IActionResult Veri() => Json(new
        {
            success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(), turler = ZimmetOrnek.Turler, durumlar = ZimmetOrnek.Durumlar, lokasyonlar = ZimmetOrnek.Lokasyonlar, sirketler = ZimmetOrnek.Sirketler,
            personel = NakitAvansOrnek.Personel.Select(ZimmetOrnek.KisiOzet).ToList(), varliklar = ZimmetOrnek.Varliklar.Select(ZimmetOrnek.Json).ToList(), ozet = ZimmetOrnek.Ozet()
        });
        [HttpGet("Varlik")] public IActionResult Varlik(int id) { var v = ZimmetOrnek.Bul(id); return v == null ? Json(new { success = false, message = "Varlık bulunamadı." }) : Json(new { success = true, varlik = ZimmetOrnek.Json(v) }); }
        [HttpPost("Kaydet")] public IActionResult Kaydet([FromBody] ZmVarlikIstek i) { var r = ZimmetOrnek.Kaydet(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, varlik = r.v == null ? null : ZimmetOrnek.Json(r.v) }); }
        [HttpPost("ZimmetVer")] public IActionResult ZimmetVer([FromBody] ZmZimmetIstek i) { var r = ZimmetOrnek.ZimmetVer(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, varlik = r.v == null ? null : ZimmetOrnek.Json(r.v) }); }
        [HttpPost("Iade")] public IActionResult Iade([FromBody] ZmIadeIstek i) { var r = ZimmetOrnek.Iade(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, varlik = r.v == null ? null : ZimmetOrnek.Json(r.v) }); }
        [HttpPost("DosyaYukle")]
        public IActionResult DosyaYukle(int id, string tur, IFormFile dosya)
        {
            byte[] veri = null; if (dosya != null && dosya.Length > 0) { using var ms = new MemoryStream(); dosya.CopyTo(ms); veri = ms.ToArray(); }
            var r = ZimmetOrnek.DosyaEkle(id, tur, dosya?.FileName, dosya?.ContentType, veri, Ad()); return Json(new { success = r.ok, message = r.mesaj, varlik = r.v == null ? null : ZimmetOrnek.Json(r.v) });
        }
        [HttpGet("Dosya")]
        public IActionResult Dosya(int id, int? no)
        {
            var v = ZimmetOrnek.Bul(id); if (v == null) return NotFound("Varlık bulunamadı."); var d = no.HasValue ? v.Dosyalar.FirstOrDefault(x => x.No == no.Value) : v.Dosyalar.FirstOrDefault();
            if (d?.Veri != null) return File(d.Veri, d.Tip ?? "application/octet-stream", d.Dosya);
            var k = NakitAvansOrnek.KisiBul(v.PersonelNo); if (k != null) return File(DisaAktar.Pdf(ZimmetOrnek.Tutanak(k, Ad())), "application/pdf", d?.Dosya ?? $"{v.No}.pdf");
            return Content("Dosya yüklenmemiş (örnek kayıt).", "text/plain; charset=utf-8");
        }
        [HttpGet("Tutanak")] public IActionResult Tutanak(string personel) { var k = NakitAvansOrnek.KisiBul(personel ?? ""); if (k == null) return NotFound("Personel bulunamadı."); return File(DisaAktar.Pdf(ZimmetOrnek.Tutanak(k, Ad())), "application/pdf", $"zimmet-tutanagi-{k.PersonelNo}.pdf"); }
        [HttpGet("Excel")]
        public IActionResult Excel()
        {
            var l = ZimmetOrnek.Varliklar; var d = new DaBelge { Baslik = "Zimmet ve Envanter Raporu", AltBaslik = $"{l.Count} varlık · {DateTime.Today:dd.MM.yyyy}", Sirket = "Uras Grup", Hazirlayan = Ad() };
            var bl = new DaBlok { Baslik = "Varlıklar", Kolonlar = { "No", "Tür", "Marka", "Model", "Seri No", "Özellik", "Şirket", "Alış Tarihi", "Değer (₺)", "Garanti Bitiş", "Durum", "Zimmetli Personel", "Departman", "Zimmet Tarihi", "Lokasyon", "Not" }, Sag = { false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false } };
            foreach (var v in l) bl.Satirlar.Add(new[] { v.No, v.Tur, v.Marka, v.Model, v.SeriNo, v.Ozellik, v.Sirket, v.AlisTarihi.ToString("dd.MM.yyyy"), v.Deger.ToString("N0", Tr), v.GarantiBitis?.ToString("dd.MM.yyyy") ?? "", v.Durum, v.PersonelAd, NakitAvansOrnek.KisiBul(v.PersonelNo)?.Departman ?? "", v.ZimmetTarihi?.ToString("dd.MM.yyyy") ?? "", v.Lokasyon, v.Not }); d.Bloklar.Add(bl);
            var kb = new DaBlok { Baslik = "Kişi Bazında", Kolonlar = { "Personel No", "Ad Soyad", "Şirket", "Departman", "Ünvan", "Varlık", "Değer (₺)", "Araç" }, Sag = { false, false, false, false, false, true, true, false } };
            foreach (var p in NakitAvansOrnek.Personel) { dynamic o = ZimmetOrnek.KisiOzet(p); kb.Satirlar.Add(new[] { p.PersonelNo, p.AdSoyad, p.Sirket, p.Departman, p.Unvan, ((int)o.adet).ToString(), ((decimal)o.deger).ToString("N0", Tr), string.Join(", ", (System.Collections.Generic.List<string>)o.araclar) }); } d.Bloklar.Add(kb);
            return File(DisaAktar.Excel(d), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"zimmet-{DateTime.Today:yyyyMMdd}.xlsx");
        }
    }
}
