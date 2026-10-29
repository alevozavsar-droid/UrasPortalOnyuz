// PERSONEL DOSYASI (/PersonelDosya; İK ve İdari İşler › İnsan Kaynakları › Personel ve Organizasyon) — Data/PersonelDosyaOrnek.cs (+ ZimmetOrnek, AracBakimOrnek, SigortaOrnek, NakitAvansOrnek canlı)
// Uçlar: Veri (personel listesi + özetler), Dosya (tek personel), Kaydet (JSON), BelgeYukle (multipart), Egitim, Belge (dosya), Pdf. Ekran: Views/PersonelDosya/Index.cshtml · betik: wwwroot/js/personel-dosya.js
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
    [Route("PersonelDosya")]
    public class PersonelDosyaController : Controller
    {
        private string Ad() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");

        /// <param name="sekme">personel | pasaport | belgeler | zimmet</param><param name="no">açılışta dosyası açılacak personel</param>
        [HttpGet("")]
        public IActionResult Index(string sekme, string no)
        {
            ViewData["Title"] = "Personel Dosyası"; ViewBag.Sekme = new[] { "personel", "pasaport", "belgeler", "zimmet" }.Contains(sekme) ? sekme : "personel"; ViewBag.No = no ?? ""; ViewBag.Ben = Ad();
            return View("Index");
        }
        [HttpGet("Veri")]
        public IActionResult Veri() => Json(new
        {
            success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(), pasaportDurumlari = PersonelDosyaOrnek.PasaportDurumlari, belgeTurleri = PersonelDosyaOrnek.BelgeTurleri,
            sirketler = NakitAvansOrnek.Personel.Select(p => p.Sirket).Distinct().ToList(), departmanlar = NakitAvansOrnek.Personel.Select(p => p.Departman).Distinct().OrderBy(x => x).ToList(),
            personel = NakitAvansOrnek.Personel.Select(PersonelDosyaOrnek.Ozet).ToList()
        });
        [HttpGet("Dosya")] public IActionResult Dosya(string no) { var p = NakitAvansOrnek.KisiBul(no ?? ""); return p == null ? Json(new { success = false, message = "Personel bulunamadı." }) : Json(new { success = true, dosya = PersonelDosyaOrnek.Json(p) }); }
        [HttpPost("Kaydet")] public IActionResult Kaydet([FromBody] PdKayitIstek i) { var r = PersonelDosyaOrnek.Kaydet(i, Ad()); return Json(new { success = r.ok, message = r.mesaj }); }
        [HttpPost("Egitim")] public IActionResult Egitim([FromBody] PdEgitimIstek i) { var r = PersonelDosyaOrnek.EgitimEkle(i, Ad()); return Json(new { success = r.ok, message = r.mesaj }); }
        [HttpPost("BelgeYukle")]
        public IActionResult BelgeYukle(string personelNo, string tur, string gecerlilik, IFormFile dosya)
        {
            byte[] veri = null; if (dosya != null && dosya.Length > 0) { using var ms = new MemoryStream(); dosya.CopyTo(ms); veri = ms.ToArray(); }
            var r = PersonelDosyaOrnek.BelgeYukle(personelNo, tur, gecerlilik, dosya?.FileName, dosya?.ContentType, veri, Ad()); return Json(new { success = r.ok, message = r.mesaj });
        }
        [HttpGet("Belge")]
        public IActionResult Belge(string no, int belgeNo)
        {
            var p = NakitAvansOrnek.KisiBul(no ?? ""); var d = p == null ? null : PersonelDosyaOrnek.Dosya(p.PersonelNo); var b = d?.Belgeler.FirstOrDefault(x => x.No == belgeNo); if (b == null) return NotFound("Belge bulunamadı.");
            if (b.Veri != null) return File(b.Veri, b.Tip ?? "application/octet-stream", b.Dosya);
            var k = new DaBelge { Baslik = b.Tur, AltBaslik = $"{p.AdSoyad} · {p.Unvan} · {p.Departman} · {p.Sirket}", Sirket = p.Sirket, Hazirlayan = Ad(), Yatay = false }; k.Ozet.Add(("Personel No", p.PersonelNo)); k.Ozet.Add(("Belge Tarihi", b.Tarih.ToString("dd.MM.yyyy"))); k.Ozet.Add(("Geçerlilik", b.GecerlilikBitis?.ToString("dd.MM.yyyy") ?? "Süresiz")); k.Ozet.Add(("Durum", b.Durum)); k.Bloklar.Add(new DaBlok { Baslik = "Not", Not = "Ön yüz örneği: belgenin taranmış aslı yüklenmediği için kapak sayfası üretildi. Gerçek dosya yüklendiğinde bu bağlantı dosyayı açar." });
            return File(DisaAktar.Pdf(k), "application/pdf", b.Dosya);
        }
        [HttpGet("Pdf")] public IActionResult Pdf(string no) { var p = NakitAvansOrnek.KisiBul(no ?? ""); if (p == null) return NotFound("Personel bulunamadı."); return File(DisaAktar.Pdf(PersonelDosyaOrnek.DosyaPdf(p, Ad())), "application/pdf", $"personel-dosyasi-{p.PersonelNo}.pdf"); }
    }
}
