using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// KONSOLİDE SATIŞ RAPORU (13 Kas 2026) — Yurtiçi Satış › Satış Analizleri ve Performans.
    /// Yedi satış raporunun (Satış Raporu, Satış Ekstre, Satış Analiz, Satış Analiz Dashboard, Satışçı Analiz, Konsolide Prim, Tahsilat Prim Tablosu)
    /// süzgeç, sütun ve bilgileri tek ekranda. Ana sekme pazar (Yurtiçi · Yurtdışı · Tümü); alt sekmeler Faturalar · Kalemler · Siparişler · Tahsilatlar · Müşteri / Ürün / Temsilci / Şirket / Kırılım · Özet (Prim sekmesi 13 Kas 2026'da kullanıcı isteğiyle kaldırıldı).
    /// Veri Data/SatisRaporOrnek.cs (cari ekstre faturaları + satış siparişleri + tahsilatlar; canlı), betik wwwroot/js/satis-raporu.js.
    /// Ekran yalnız okur; süzme, sıralama ve özetleme tarayıcıda yapılır.
    /// </summary>
    [Authorize]
    [Route("SatisRaporu")]
    public class SatisRaporuController : Controller
    {
        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Konsolide Satış Raporu";
            ViewBag.Sekme = sekme ?? "belge";
            return View();
        }

        [HttpGet("Veri")]
        public IActionResult Veri() => Json(SatisRaporOrnek.Paket());
    }
}
