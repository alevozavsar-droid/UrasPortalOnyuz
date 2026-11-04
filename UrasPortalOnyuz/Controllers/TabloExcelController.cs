using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Tablo → Excel / PDF (ortak uç): ekrandaki tablo göründüğü gibi .xlsx ya da .pdf olur. İstemci bileşeni wwwroot/js/tablo-excel.js tabloyu hücre hücre okuyup JSON gönderir;
    /// dosya Data/TabloExcel.cs (ClosedXML) ya da Data/TabloPdf.cs (QuestPDF) ile üretilir.
    /// Kullanan ekranlar: Personel Maliyet Raporu (tüm sekmelerin Excel ve PDF düğmeleri), Personel Muhasebe İşlemleri.
    /// </summary>
    [Authorize]
    public class TabloExcelController : Controller
    {
        [HttpPost, IgnoreAntiforgeryToken, RequestSizeLimit(64 * 1024 * 1024)]
        public IActionResult Indir([FromBody] TxBelge b)
        {
            var hata = Kontrol(b); if (hata != null) return BadRequest(hata);
            byte[] dosya;
            try { dosya = TabloExcel.Uret(b, OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "")); }
            catch (Exception e) { return StatusCode(500, "Excel üretilemedi: " + e.Message); }
            return File(dosya, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Ad(b) + ".xlsx");
        }

        [HttpPost, IgnoreAntiforgeryToken, RequestSizeLimit(64 * 1024 * 1024)]
        public IActionResult Pdf([FromBody] TxBelge b)
        {
            var hata = Kontrol(b); if (hata != null) return BadRequest(hata);
            byte[] dosya;
            try { dosya = TabloPdf.Uret(b, OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "")); }
            catch (Exception e) { return StatusCode(500, "PDF üretilemedi: " + e.Message); }
            return File(dosya, "application/pdf", Ad(b) + ".pdf");
        }

        /// <summary>Boş belge ve boyut sınırı (en çok 40 sayfa, 500.000 hücre); sorun yoksa null.</summary>
        static string Kontrol(TxBelge b)
        {
            if (b?.Sayfalar == null || b.Sayfalar.Count == 0) return "Dışa aktarılacak tablo yok.";
            int hucre = b.Sayfalar.Sum(s => s.Satirlar?.Sum(r => r.H?.Count ?? 0) ?? 0);
            return b.Sayfalar.Count > 40 || hucre > 500000 ? "Tablo çok büyük (en çok 40 sayfa, 500.000 hücre)." : null;
        }

        /// <summary>Dosya adı: yalnız harf, rakam, _, - ve . karakterleri.</summary>
        static string Ad(TxBelge b)
        {
            var ad = new string((b.Dosya ?? "rapor").Where(ch => char.IsLetterOrDigit(ch) || ch == '_' || ch == '-' || ch == '.').ToArray());
            return string.IsNullOrEmpty(ad) ? "rapor" : ad;
        }
    }
}
