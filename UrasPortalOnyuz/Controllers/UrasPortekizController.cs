using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// URAS PORTEKİZ RAPORU (20 Kas 2026) — Uras Portekiz'in gönderdiği Excel raporunun (satışlar, kasa, banka hesapları, ödeme öngörüsü) Türkçe portal karşılığı.
    /// Veri App_Data/uras-portekiz klasöründeki en yeni dosyadan okunur (Data/UrasPortekizOrnek.cs); açıklamalar İngilizceden Türkçeye çevrilir (Data/UrasPortekizCeviri.cs).
    /// Ekran yalnız okur; süzme, sıralama, özetleme tarayıcıda (wwwroot/js/uras-portekiz.js). "Yeni Rapor Yükle" yeni Excel'i klasöre ekler.
    /// </summary>
    [Authorize]
    [Route("UrasPortekiz")]
    public class UrasPortekizController : Controller
    {
        private readonly IWebHostEnvironment _env;
        public UrasPortekizController(IWebHostEnvironment env) { _env = env; }

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Uras Portekiz Raporu";
            ViewBag.Sekme = sekme ?? "ozet";
            return View();
        }

        [HttpGet("Veri")]
        public IActionResult Veri() => Json(UrasPortekizOrnek.Paket(_env.ContentRootPath));

        /// <summary>Yeni rapor dosyası (.xls / .xlsx): önce geçici adla kaydedilir, Uras Portekiz raporu olduğu doğrulanınca klasöre alınır; en yeni dosya rapor olur.</summary>
        [HttpPost("Yukle"), IgnoreAntiforgeryToken, RequestSizeLimit(40_000_000)]
        public IActionResult Yukle(IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0) return Json(new { success = false, message = "Dosya seçilmedi." });
            string uzanti = Path.GetExtension(dosya.FileName ?? "").ToLowerInvariant();
            if (uzanti != ".xls" && uzanti != ".xlsx") return Json(new { success = false, message = "Yalnız Excel dosyası (.xls / .xlsx) yüklenebilir." });
            if (dosya.Length > 40_000_000) return Json(new { success = false, message = "Dosya 40 MB'tan büyük olamaz." });
            string klasor = UrasPortekizOrnek.Klasor(_env.ContentRootPath); Directory.CreateDirectory(klasor);
            string ad = Regex.Replace(Path.GetFileNameWithoutExtension(dosya.FileName ?? "rapor"), @"[^A-Za-z0-9._-]+", "_").Trim('_', '.'); if (ad.Length == 0) ad = "REPORT_PORTUGAL"; if (ad.Length > 80) ad = ad.Substring(0, 80);
            string gecici = Path.Combine(klasor, "_yukleme_" + Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                using (var fs = System.IO.File.Create(gecici)) dosya.CopyTo(fs);
                string hata = UrasPortekizOrnek.Dogrula(gecici);
                if (hata != null) { System.IO.File.Delete(gecici); return Json(new { success = false, message = hata }); }
                string hedef = Path.Combine(klasor, ad + uzanti);
                if (System.IO.File.Exists(hedef)) hedef = Path.Combine(klasor, ad + "_" + DateTime.Now.ToString("yyyyMMdd-HHmm") + uzanti);
                System.IO.File.Move(gecici, hedef); System.IO.File.SetLastWriteTimeUtc(hedef, DateTime.UtcNow);
                UrasPortekizOrnek.OnbellekTemizle();
                return Json(new { success = true, message = Path.GetFileName(hedef) + " yüklendi; rapor bu dosyadan okunuyor." });
            }
            catch (Exception ex)
            {
                try { if (System.IO.File.Exists(gecici)) System.IO.File.Delete(gecici); } catch { /* yoksay */ }
                return Json(new { success = false, message = "Dosya yüklenemedi: " + ex.Message });
            }
        }
    }
}
