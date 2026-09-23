using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Yapılan Ödemeler — Yönetim › Özel Analizler › Özel Raporlar. Üç sekme:
    ///   Ödeme Listesi            : tüm grup şirketlerinin ödemeleri (Banka / Çek / Nakit Kasa / Nakit Avans) filtre, KPI, matris ve detay ile
    ///   Şirket Bazlı Ödeme Raporu: "2026 Şirket Bazlı Ödeme Raporu" şablonu — şirket × (Banka / Çek / Nakit) × (X / R) × 12 ay + Toplam + Aylık Ort.
    ///   Konsolide Ödeme Raporu   : "2026 Konsolide Ödeme Raporu" şablonu — ödeme türü konsolidesi (X / R) ve şirket konsolidesi
    /// Veri kaynakları ve örnek kayıtlar: Data/YapilanOdemelerOrnek.cs. Filtreleme / özetler istemcide (wwwroot/js/yapilan-odemeler.js).
    /// </summary>
    [Authorize]
    public class YapilanOdemelerController : Controller
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        /// <param name="sekme">sirket (varsayılan) | konsolide | liste (derin bağlantı)</param>
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Yapılan Ödemeler";
            ViewBag.Sirketler = YapilanOdemelerOrnek.Sirketler;
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Sekme = sekme == "konsolide" || sekme == "liste" ? sekme : "sirket";
            return View();
        }

        /// <summary>Konsolide ödeme listesi + seçim listeleri. Tarih / yıl filtreleri istemcide uygulanır (veri hacmi küçüktür).</summary>
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
                    yil = DateTime.Today.Year,
                    sirketler = YapilanOdemelerOrnek.Sirketler,
                    bankaSirketleri = YapilanOdemelerOrnek.BankaSirketleri,
                    turler = YapilanOdemelerOrnek.Turler.Select(t => new { kanal = t.Kanal, ad = t.Ad }),
                    aktarimTipleri = YapilanOdemelerOrnek.AktarimTipleri,
                    kategoriler = YapilanOdemelerOrnek.Kategoriler,
                    aliciTipleri = YapilanOdemelerOrnek.AliciTipleri,
                    durumlar = YapilanOdemelerOrnek.Durumlar,
                    kaynaklar = YapilanOdemelerOrnek.Kaynaklar,
                    paraBirimleri = YapilanOdemelerOrnek.Kurlar.Keys,
                    kurlar = YapilanOdemelerOrnek.Kurlar,
                    odemeler = liste.Select(o => new
                    {
                        o.Id, o.No, tarih = o.Tarih.ToString("yyyy-MM-dd"), tarihTr = o.Tarih.ToString("dd.MM.yyyy"), o.Sirket, o.Tur, o.Kanal, o.AktarimTipi, o.KaynakHesap, o.Banka,
                        o.Alici, o.AliciTipi, o.Kategori, o.Tutar, o.ParaBirimi, o.Kur, o.TutarTl, o.Belge,
                        vade = o.Vade?.ToString("yyyy-MM-dd"), vadeTr = o.Vade?.ToString("dd.MM.yyyy"), o.Durum, o.Aciklama, o.Kaynak, o.Onaylayan, o.IslemiYapan, o.Link
                    })
                });
            }
        }
    }
}
