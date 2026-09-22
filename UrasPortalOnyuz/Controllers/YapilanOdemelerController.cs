using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Yapılan Ödemeler — Yönetim › Özel Analizler › Özel Raporlar.
    /// Tüm grup şirketlerinin yaptığı ödemelerin konsolide listesi: Banka (Havale / EFT), Çek, Nakit (Kasa) ve Nakit Avans.
    /// Veri kaynakları ve örnek kayıtlar: Data/YapilanOdemelerOrnek.cs. Filtreleme / özetler istemcide (wwwroot/js/yapilan-odemeler.js).
    /// </summary>
    [Authorize]
    public class YapilanOdemelerController : Controller
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        public IActionResult Index()
        {
            ViewData["Title"] = "Yapılan Ödemeler";
            ViewBag.Sirketler = YapilanOdemelerOrnek.Sirketler;
            return View();
        }

        /// <summary>Konsolide ödeme listesi + seçim listeleri. Tarih filtreleri istemcide uygulanır (veri hacmi küçüktür).</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            lock (YapilanOdemelerOrnek.Kilit)
            {
                var liste = YapilanOdemelerOrnek.Liste();
                return Json(new
                {
                    success = true,
                    bugun = DateTime.Today.ToString("yyyy-MM-dd"),
                    sirketler = YapilanOdemelerOrnek.Sirketler,
                    turler = YapilanOdemelerOrnek.Turler.Select(t => new { kanal = t.Kanal, ad = t.Ad }),
                    kategoriler = YapilanOdemelerOrnek.Kategoriler,
                    aliciTipleri = YapilanOdemelerOrnek.AliciTipleri,
                    durumlar = YapilanOdemelerOrnek.Durumlar,
                    kaynaklar = YapilanOdemelerOrnek.Kaynaklar,
                    paraBirimleri = YapilanOdemelerOrnek.Kurlar.Keys,
                    kurlar = YapilanOdemelerOrnek.Kurlar,
                    odemeler = liste.Select(o => new
                    {
                        o.Id, o.No, tarih = o.Tarih.ToString("yyyy-MM-dd"), tarihTr = o.Tarih.ToString("dd.MM.yyyy"), o.Sirket, o.Tur, o.Kanal, o.KaynakHesap, o.Banka,
                        o.Alici, o.AliciTipi, o.Kategori, o.Tutar, o.ParaBirimi, o.Kur, o.TutarTl, o.Belge,
                        vade = o.Vade?.ToString("yyyy-MM-dd"), vadeTr = o.Vade?.ToString("dd.MM.yyyy"), o.Durum, o.Aciklama, o.Kaynak, o.Onaylayan, o.IslemiYapan, o.Link
                    })
                });
            }
        }
    }
}
