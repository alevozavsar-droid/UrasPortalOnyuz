using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinansRaporlama.Controllers;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// KREDİ YAPILANDIRMA RAPORU (9 Kas 2026) — Mali İşler › Finans › Krediler ve Finansman. Veri Data/KrediYapilandirmaOrnek.cs (App_Data/kredi-yapilandirma.json kalıcı),
    /// görünüm Views/KrediYapilandirma/Index.cshtml, betik wwwroot/js/kredi-yapilandirma.js. Yapılandırma öncesi / sonrası kredi bilgileri, teminatlar (ipotek …), taksit takvimi;
    /// taksitler Kasa Nakit Akışı'na (Havale / EFT satırı) ve Planlı Düzenli Ödemeler'e akar. ?sekme=krediler|oncesi|sonrasi|teminat|takvim|aylik
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class KrediYapilandirmaController : Controller
    {
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        public class IdIstek { public int Id { get; set; } }
        public class AktarIstek { public int Id { get; set; } public bool Aktar { get; set; } = true; }

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Kredi Yapılandırma Raporu";
            ViewBag.Sekme = string.IsNullOrEmpty(sekme) ? "krediler" : sekme;
            OrnekDoldurucu.Hazirla(this);
            return View("Index");
        }
        [HttpGet("Veri")]
        public IActionResult Veri() => Json(KrediYapilandirmaOrnek.Paket());

        /// <summary>Kredi ekle / düzenle: kaydeder, taksit satırlarını Kasa Nakit Akışı'na ve düzenli ödeme kaydına eşler; tüm paket döner.</summary>
        [HttpPost("Kaydet"), IgnoreAntiforgeryToken]
        public IActionResult Kaydet([FromBody] KrediIstek d)
        {
            var h = KrediYapilandirmaOrnek.Kaydet(d, Kullanici, out var k, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, id = k?.Id, veri = h == null ? KrediYapilandirmaOrnek.Paket() : null });
        }
        [HttpPost("Sil"), IgnoreAntiforgeryToken]
        public IActionResult Sil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.Sil(d?.Id ?? 0, Kullanici, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, veri = h == null ? KrediYapilandirmaOrnek.Paket() : null });
        }
        /// <summary>Nakit akışa aktarımı aç / kapat (kapalı: ödenmemiş taksit satırları kaldırılır, düzenli ödeme pasife alınır); yeniden eşleme için aynı değerle çağrılabilir.</summary>
        [HttpPost("Aktar"), IgnoreAntiforgeryToken]
        public IActionResult Aktar([FromBody] AktarIstek d)
        {
            var h = KrediYapilandirmaOrnek.AktarDegistir(d?.Id ?? 0, d?.Aktar ?? true, Kullanici, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, veri = h == null ? KrediYapilandirmaOrnek.Paket() : null });
        }
    }
}
