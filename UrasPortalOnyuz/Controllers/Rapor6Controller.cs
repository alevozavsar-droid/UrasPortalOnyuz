// SATINALMA FİYAT ANALİZ RAPORU (/UrunFiyatKarsilastirma) — Tedarik Zinciri › Fiyat ve Alım Analizi
// Elle yazılmış sürüm (üretilmiş Uretilen/Controllers/Rapor6Controller.cs URETILEN_KAPALI ile devre dışı). Veri: Data/SatinalmaFiyatOrnek.cs.
// 23 Kas 2026 (kullanıcı: "Satınalma Fiyat Analizi raporunu geliştirelim, temsilci seçelim, TL fiyat, dövizli fiyat, detaylı rapor hazırla"):
// 12 satırlık rastgele örnek liste yerine tedarikçi alış faturalarının kalemlerinden çalışan yedi sekmeli rapor —
// Malzeme Fiyat Analizi · Tedarikçi Karşılaştırması · Aylık Fiyat Seyri · Alım Detayı | Temsilci Özeti · Tedarikçi Özeti · Kategori Özeti.
// Her alımın birim fiyatı ₺ ve fatura günü kuruyla $ / € olarak gösterilir; son alım ↔ önceki alım değişimi iki para biriminde ayrı hesaplanır.
// Ekran: Views/Rapor6/Index.cshtml · betik: wwwroot/js/satinalma-fiyat.js · hesap tarayıcıda.
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("UrunFiyatKarsilastirma")]
    public class Rapor6Controller : Controller
    {
        /// <param name="sekme">malzeme | tedarikci | aylik | detay | temsilci | tedOzet | kategori</param>
        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Satınalma Fiyat Analiz Raporu";
            ViewBag.Sekme = sekme ?? "malzeme";
            return View("Index");
        }

        [HttpGet("Veri")]
        public IActionResult Veri() => Json(new { paket = SatinalmaFiyatOrnek.Paket() });
    }
}
