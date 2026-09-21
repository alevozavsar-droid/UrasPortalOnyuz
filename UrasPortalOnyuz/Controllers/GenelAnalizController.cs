using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Genel Analiz Raporu — Yönetim › Yönetim Özeti › Özel Analizler › Özel Raporlar.
    /// Şirket bazında aylık satış / alım / brüt fark ve dönem özeti (örnek veri; kaynak bağlanınca gerçek değerlerle değişecek).
    /// Tahsilat Analiz Raporu (Rapor35) ile aynı grupta, ayrı ekran.
    /// </summary>
    [Authorize]
    public class GenelAnalizController : Controller
    {
        public IActionResult Index()
        {
            ViewData["Title"] = "Genel Analiz Raporu";
            ViewBag.Sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct().ToArray();
            ViewBag.Yil = DateTime.Today.Year;
            return View();
        }

        /// <summary>Aylık satış, alım, tahsilat ve ödeme serileri (örnek; şirket ve yıla göre tohumlu üretilir).</summary>
        [HttpGet]
        public IActionResult Veri(string sirket, int yil)
        {
            if (yil == 0) yil = DateTime.Today.Year;
            var rnd = new Random((sirket ?? "").GetHashCode() ^ yil);
            int sonAy = yil < DateTime.Today.Year ? 12 : yil > DateTime.Today.Year ? 0 : DateTime.Today.Month;
            decimal olcek = string.IsNullOrEmpty(sirket) ? 3.2m : 1m;
            var aylar = Enumerable.Range(1, 12).Select(i =>
            {
                bool var = i <= sonAy;
                decimal satis = var ? Math.Round((18 + rnd.Next(0, 24)) * 1_000_000m * olcek + rnd.Next(0, 999_999), 2) : 0;
                decimal alim = var ? Math.Round(satis * (0.52m + rnd.Next(0, 18) / 100m), 2) : 0;
                decimal tahsilat = var ? Math.Round(satis * (0.78m + rnd.Next(0, 20) / 100m), 2) : 0;
                decimal odeme = var ? Math.Round(alim * (0.80m + rnd.Next(0, 18) / 100m), 2) : 0;
                return new { ay = i, satis, alim, brutKar = satis - alim, tahsilat, odeme, netNakit = tahsilat - odeme };
            }).ToList();
            var dolu = aylar.Where(a => a.satis > 0).ToList();
            return Json(new
            {
                success = true, sirket = string.IsNullOrEmpty(sirket) ? "Tüm Şirketler" : sirket, yil, sonAy, aylar,
                ozet = new
                {
                    satis = dolu.Sum(a => a.satis), alim = dolu.Sum(a => a.alim), brutKar = dolu.Sum(a => a.brutKar), tahsilat = dolu.Sum(a => a.tahsilat), odeme = dolu.Sum(a => a.odeme),
                    acikAlacak = Math.Round(dolu.Sum(a => a.satis - a.tahsilat), 2), acikBorc = Math.Round(dolu.Sum(a => a.alim - a.odeme), 2),
                    aktifMusteri = 480 + rnd.Next(0, 300), aktifTedarikci = 160 + rnd.Next(0, 120), acikSiparis = 20 + rnd.Next(0, 60)
                }
            });
        }
    }
}
