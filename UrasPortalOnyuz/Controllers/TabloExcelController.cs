using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Tablo → Excel (ortak uç): ekrandaki tablo göründüğü gibi .xlsx olur. İstemci bileşeni wwwroot/js/tablo-excel.js tabloyu hücre hücre (değer / sayı biçimi, renkler,
    /// kalınlık, birleşik hücre, zengin metin, kenarlık) okuyup JSON gönderir; dosya Data/TabloExcel.cs (ClosedXML) ile üretilir.
    /// Kullanan ekranlar: Personel Maliyet Raporu (tüm sekmelerin Excel düğmeleri), Personel Muhasebe İşlemleri.
    /// </summary>
    [Authorize]
    public class TabloExcelController : Controller
    {
        [HttpPost, IgnoreAntiforgeryToken, RequestSizeLimit(64 * 1024 * 1024)]
        public IActionResult Indir([FromBody] TxBelge b)
        {
            if (b?.Sayfalar == null || b.Sayfalar.Count == 0) return BadRequest("Dışa aktarılacak tablo yok.");
            int hucre = b.Sayfalar.Sum(s => s.Satirlar?.Sum(r => r.H?.Count ?? 0) ?? 0);
            if (b.Sayfalar.Count > 40 || hucre > 500000) return BadRequest("Tablo Excel için çok büyük (en çok 40 sayfa, 500.000 hücre).");
            string ad = new string((b.Dosya ?? "rapor").Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.').ToArray()); if (string.IsNullOrEmpty(ad)) ad = "rapor";
            byte[] dosya;
            try { dosya = TabloExcel.Uret(b, OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "")); }
            catch (Exception e) { return StatusCode(500, "Excel üretilemedi: " + e.Message); }
            return File(dosya, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad + ".xlsx");
        }
    }
}
