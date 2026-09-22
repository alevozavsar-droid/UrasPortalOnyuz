using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Modül sayfası (Grup ERP Portalı › modul.html karşılığı): ana menüdeki bir modülün
    /// 1. alt menü → 2. alt menü → rapor / ekran kırılımı, gösterge paneli ve rapor listesi.
    /// Adres: /Modul/{key}  (derin bağlantı: /Modul/{key}?yol=alt1/alt2/r0  ya da  #alt1/alt2/r0)
    /// </summary>
    [Authorize]
    public class ModulController : Controller
    {
        private static string _yapiJs;   // bir kez üretilir (menü statik)

        [HttpGet("Modul")]
        [HttpGet("Modul/{key}")]
        public IActionResult Index(string key, string yol)
        {
            var modul = KurumsalMenu.Bul(key) ?? KurumsalMenu.Moduller.First(m => m.ModulMu);
            if (!modul.ModulMu) return Redirect(modul.Href ?? "/");
            ViewData["Title"] = modul.Ad;
            ViewBag.Yol = yol ?? "";
            return View(modul);
        }

        /// <summary>Menü yapısı + ikonlar; window.MENU_YAPISI / window.MENU_ICONS olarak yüklenir (arama ve modül sayfası).</summary>
        [HttpGet("Modul/Yapi.js")]
        [ResponseCache(Duration = 60)]
        public IActionResult Yapi()
        {
            if (_yapiJs == null)
            {
                // Son 30 günde eklenen raporlar "(Yeni)", güncellenen / taşınan raporlar "(Düzenlendi)" rozeti alır (Data/PortalDegisiklikler.cs);
                // kayıt, raporun modül derin bağlantısına (/Modul/x?yol=.../rN) ya da ekran adresine (/CariKart/Musteri gibi) göre eşlenir.
                var yeniler = Adresler("yeni");
                var duzenlenenler = Adresler("guncelleme", "tasima");
                var ayar = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                string yapi = JsonConvert.SerializeObject(KurumsalMenu.Moduller.Select(m => new
                {
                    key = m.Key, ad = m.Ad, ic = m.Ikon, amac = m.Amac, kpi = m.Kpi,
                    href = m.ModulMu ? null : m.Href,
                    alt = m.ModulMu ? m.Alt.Select(a => Grup(a, m.Key, a.Key, yeniler, duzenlenenler)).ToList() : null
                }), ayar);
                string ikonlar = JsonConvert.SerializeObject(KurumsalMenu.Ikonlar);
                _yapiJs = "window.MENU_YAPISI = " + yapi + ";\nwindow.MENU_ICONS = " + ikonlar + ";\n";
            }
            return Content(_yapiJs, "application/javascript; charset=utf-8");
        }

        /// <summary>Bir rapor bu kadar gün "yeni" / "düzenlendi" sayılır.</summary>
        public const int YeniGun = 30;

        /// <summary>Verilen türdeki (yeni | guncelleme | tasima | kaldirma) son 30 günlük günlük kayıtlarının adresleri.</summary>
        private static HashSet<string> Adresler(params string[] turler) => new HashSet<string>(
            PortalDegisiklikler.Liste.Where(x => turler.Contains(x.Tur) && (System.DateTime.Today - x.Tarih.Date).Days <= YeniGun && !string.IsNullOrEmpty(x.Href)).Select(x => x.Href),
            System.StringComparer.OrdinalIgnoreCase);

        private object Grup(KmGrup g, string modulKey, string yol, HashSet<string> yeniler, HashSet<string> duzenlenenler) => new
        {
            key = g.Key, ad = g.Ad, not = g.Not, dogrudan = g.Dogrudan ? true : (bool?)null,
            alt = g.Alt?.Select(b => Grup(b, modulKey, yol + "/" + b.Key, yeniler, duzenlenenler)).ToList(),
            raporlar = g.Raporlar?.Select((r, i) =>
            {
                string url = r.Hazir ? (Url.Action(r.Action, r.Controller) ?? ("/" + r.Controller + "/" + r.Action)) : null;
                string derin = $"/Modul/{modulKey}?yol={yol}/r{i}";
                // Ekran adresi yalnız gerçek bir ekranı gösteriyorsa eşlenir; ana sayfaya ("/") giden kısayollar "Ana Sayfa" kayıtlarıyla karışmasın
                string adres = url != null && url.Length > 1 ? url : null;
                bool yeni = yeniler.Contains(derin) || (adres != null && yeniler.Contains(adres));
                bool duzenlendi = !yeni && (duzenlenenler.Contains(derin) || (adres != null && duzenlenenler.Contains(adres)));
                return new
                {
                    ad = r.Ad,
                    url,
                    kisayol = r.Kisayol,
                    durum = r.Durum,
                    yeni = yeni ? true : (bool?)null,
                    duzenlendi = duzenlendi ? true : (bool?)null
                };
            }).ToList()
        };
    }
}
