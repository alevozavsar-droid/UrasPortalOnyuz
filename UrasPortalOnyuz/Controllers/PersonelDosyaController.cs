// PERSONEL DOSYASI (/PersonelDosya; İK ve İdari İşler › İnsan Kaynakları › Personel ve Organizasyon) — Data/PersonelDosyaOrnek.cs (+ ZimmetOrnek, AracBakimOrnek, SigortaOrnek, NakitAvansOrnek canlı)
// Kişiler (9 Kas 2026): Konsolide İK Paneli kayıtları (Data/IkPersonelMaster.cs; personel no = cari kodu 335.xxx). Telefon / e-posta / işe giriş kaydı panele yazılır ve kalıcı günlüğe "kimlik" satırı düşer.
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

        /// <param name="sekme">personel | pasaport | belgeler | zimmet</param><param name="no">açılışta dosyası açılacak personel (cari kodu 335.xxx, ad ya da sicil no)</param>
        [HttpGet("")]
        public IActionResult Index(string sekme, string no)
        {
            ViewData["Title"] = "Personel Dosyası"; ViewBag.Sekme = new[] { "personel", "pasaport", "belgeler", "zimmet" }.Contains(sekme) ? sekme : "personel"; ViewBag.No = no ?? ""; ViewBag.Ben = Ad();
            return View("Index");
        }
        /// <summary>Personel listesi Konsolide İK Paneli'nden (her sıra için en yeni dönem kaydı); şirket = SGK işvereni, departman = bölüm.</summary>
        [HttpGet("Veri")]
        public IActionResult Veri()
        {
            var kisiler = IkPersonelMaster.Kisiler();
            return Json(new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(), pasaportDurumlari = PersonelDosyaOrnek.PasaportDurumlari, belgeTurleri = PersonelDosyaOrnek.BelgeTurleri,
                kaynak = "Konsolide İK Paneli · " + PersonelMaliyetOrnek.DonemAd(PersonelMaliyetOrnek.EnYeniDonem), panel = "/PersonelMaliyet?sekme=cari",
                sirketler = kisiler.Select(p => p.SgkIsvereni).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(x => x, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), true)).ToList(),
                departmanlar = kisiler.Select(p => p.Bolum).Where(s => !string.IsNullOrEmpty(s)).Distinct().OrderBy(x => x, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), true)).ToList(),
                personel = kisiler.Select(PersonelDosyaOrnek.Ozet).ToList()
            });
        }
        [HttpGet("Dosya")] public IActionResult Dosya(string no) { var p = PersonelDosyaOrnek.Kisi(no ?? ""); return p == null ? Json(new { success = false, message = "Personel bulunamadı." }) : Json(new { success = true, dosya = PersonelDosyaOrnek.Json(p) }); }
        [HttpPost("Kaydet")]
        public IActionResult Kaydet([FromBody] PdKayitIstek i)
        {
            var r = PersonelDosyaOrnek.Kaydet(i, Ad(), out var kimlik);
            if (r.ok && kimlik != null) PersonelMaliyetKalici.Yaz("kimlik", kimlik, Ad());   // Konsolide İK Paneli'ne yazılan telefon / e-posta / işe giriş kalıcı (yeniden başlatınca aynı sırayla uygulanır)
            return Json(new { success = r.ok, message = r.mesaj });
        }
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
            var p = PersonelDosyaOrnek.Kisi(no ?? ""); var d = p == null ? null : PersonelDosyaOrnek.Dosya(p); var b = d?.Belgeler.FirstOrDefault(x => x.No == belgeNo); if (b == null) return NotFound("Belge bulunamadı.");
            if (b.Veri != null) return File(b.Veri, b.Tip ?? "application/octet-stream", b.Dosya);
            var k = new DaBelge { Baslik = b.Tur, AltBaslik = $"{p.AdSoyad} · {p.Gorev} · {p.Bolum} · {p.SgkIsvereni}", Sirket = p.SgkIsvereni, Hazirlayan = Ad(), Yatay = false }; k.Ozet.Add(("Cari Kodu", p.CariKodu)); k.Ozet.Add(("Belge Tarihi", b.Tarih.ToString("dd.MM.yyyy"))); k.Ozet.Add(("Geçerlilik", b.GecerlilikBitis?.ToString("dd.MM.yyyy") ?? "Süresiz")); k.Ozet.Add(("Durum", b.Durum)); k.Bloklar.Add(new DaBlok { Baslik = "Not", Not = "Ön yüz örneği: belgenin taranmış aslı yüklenmediği için kapak sayfası üretildi. Gerçek dosya yüklendiğinde bu bağlantı dosyayı açar." });
            return File(DisaAktar.Pdf(k), "application/pdf", b.Dosya);
        }
        [HttpGet("Pdf")] public IActionResult Pdf(string no) { var p = PersonelDosyaOrnek.Kisi(no ?? ""); if (p == null) return NotFound("Personel bulunamadı."); return File(DisaAktar.Pdf(PersonelDosyaOrnek.DosyaPdf(p, Ad())), "application/pdf", $"personel-dosyasi-{p.CariKodu}.pdf"); }
    }
}
