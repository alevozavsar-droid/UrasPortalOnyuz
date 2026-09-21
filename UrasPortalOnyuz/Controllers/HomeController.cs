using WebApplication3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private static readonly int[] ZoomKademeleri = { 60, 67, 75, 80, 90, 100, 110, 125, 150 };
        private static readonly Dictionary<string, Dictionary<string, string>> _tercihler = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        public IActionResult Index()
        {
            string kod = User.Identity?.Name ?? "";
            var menuler = OrnekVeri.Menuler.Where(m => m.IsActive).ToList();
            var model = new HomeIndexViewModel
            {
                RaporGruplari = menuler.GroupBy(m => m.Category).ToDictionary(g => g.Key, g => g.OrderBy(m => m.DisplayOrder).ToList()),
                AuthorizedDatabases = OrnekVeri.Sirketler.ToList(),
                FavoriteMenuIds = OrnekVeri.Favoriler.Where(f => f.UserCode.Equals(kod, StringComparison.OrdinalIgnoreCase)).Select(f => f.MenuId).ToList()
            };
            model.FavoriteMenus = menuler.Where(m => model.FavoriteMenuIds.Contains(m.Id)).ToList();
            ViewBag.AuthorizedDatabases = model.AuthorizedDatabases;
            ViewBag.Kademeler = ZoomKademeleri;
            return View(model);
        }

        /// <summary>Eski ana sayfa (rapor kartları). Raporlar Grup ERP menü yerleşimine göre modül bazında gruplanır.</summary>
        public IActionResult RaporPaneli()
        {
            string kod = User.Identity?.Name ?? "";
            var menuler = OrnekVeri.Menuler.Where(m => m.IsActive).ToList();
            var gruplar = new Dictionary<string, List<AppMenu>>();
            foreach (var modul in KurumsalMenu.Moduller.Where(m => m.ModulMu))
            {
                var liste = KurumsalMenu.TumRaporlar(modul).Where(r => r.Hazir)
                    .Select(r => menuler.FirstOrDefault(m => string.Equals(m.ControllerName, r.Controller, StringComparison.OrdinalIgnoreCase) && string.Equals(m.ActionName, r.Action, StringComparison.OrdinalIgnoreCase)))
                    .Where(m => m != null).GroupBy(m => m.Id).Select(g => g.First()).ToList();
                if (liste.Count > 0) gruplar[modul.Ad] = liste;
            }
            var model = new HomeIndexViewModel
            {
                RaporGruplari = gruplar,
                AuthorizedDatabases = OrnekVeri.Sirketler.ToList(),
                FavoriteMenuIds = OrnekVeri.Favoriler.Where(f => f.UserCode.Equals(kod, StringComparison.OrdinalIgnoreCase)).Select(f => f.MenuId).ToList()
            };
            model.FavoriteMenus = menuler.Where(m => model.FavoriteMenuIds.Contains(m.Id)).ToList();
            ViewBag.AuthorizedDatabases = model.AuthorizedDatabases;
            ViewBag.Kademeler = ZoomKademeleri;
            return View(model);
        }

        /// <summary>Portal değişiklik günlüğü: yeni eklenen ekranlar ve taşıma / güncelleme kayıtları (Data/PortalDegisiklikler.cs).</summary>
        public IActionResult Degisiklikler(string tur)
        {
            ViewData["Title"] = "Portal Değişiklik Günlüğü";
            ViewBag.Tur = tur ?? "";
            var liste = PortalDegisiklikler.Liste.OrderByDescending(x => x.Tarih).ThenBy(x => x.Ekran).ToList();
            if (!string.IsNullOrEmpty(tur)) liste = liste.Where(x => x.Tur == tur).ToList();
            return View(liste);
        }

        [AllowAnonymous, Route("Home/Error")]
        public IActionResult Error() => View("~/Views/Shared/Error.cshtml");

        // ---- Dashboard verileri (örnek) ----
        [HttpGet]
        public IActionResult GetDashboardData()
        {
            var rnd = new Random(7);
            decimal[] satis = new decimal[12], alim = new decimal[12];
            for (int i = 0; i < 12; i++) { satis[i] = i <= DateTime.Today.Month - 1 ? rnd.Next(18, 42) * 1_000_000m + rnd.Next(0, 999_999) : 0; alim[i] = i <= DateTime.Today.Month - 1 ? rnd.Next(9, 28) * 1_000_000m + rnd.Next(0, 999_999) : 0; }
            return Json(new
            {
                success = true,
                satisAylik = satis, alimAylik = alim,
                satisToplam = satis.Sum(), alimToplam = alim.Sum(),
                acikSiparis = 37, acikTeslimat = 12, acikFatura = 148,
                aktifMusteri = 642, aktifTedarikci = 218,
                netKdv = 9_065_796m, hesaplananKdv = 21_540_310m, indirilecekKdv = 12_474_514m,
                portfoyCekTutar = 248_484_755m, portfoyCekAdet = 312,
                bekleyenCariNot = 4, bekleyenIslem = 4,
                sonGuncelleme = DateTime.Now
            });
        }

        /// <summary>Tüm ekranların gerçek adresleri (özel Route tanımları dahil) — toplu kontrol için.</summary>
        [HttpGet] public IActionResult OrnekAdresler() => Content(string.Join("\n", OrnekVeri.Menuler.Select(m => Url.Action(m.ActionName, m.ControllerName) ?? ("/" + m.ControllerName + "/" + m.ActionName))), "text/plain");

        [AcceptVerbs("GET", "HEAD")] public IActionResult Ping() => Json(new { ok = true, zaman = DateTime.Now });   // kabuk nabzı HEAD atar; yalnız GET olunca 405 dönüyordu
        [HttpGet] public IActionResult CheckMaintenance() => Json(new { active = false });
        [HttpGet] public IActionResult Bildirimler() => Json(new
        {
            okunmamis = 2,
            liste = new object[]
            {
                new { id = 1, baslik = "Mutabakat onayı", mesaj = "AKSA TEKSTİL Ağustos mutabakatı onaylandı.", zaman = DateTime.Now.AddMinutes(-35), okundu = false, url = "/Rapor20" },
                new { id = 2, baslik = "Aktarım servisi", mesaj = "5 belge aktarım hatasında; denetim ekranına bakın.", zaman = DateTime.Now.AddHours(-2), okundu = false, url = "/Rapor145" },
                new { id = 3, baslik = "Sürüm notu", mesaj = "Mesajlaşma ve WhatsApp ekstre asistanı yayında.", zaman = DateTime.Now.AddDays(-1), okundu = true, url = "/Mesaj" }
            }
        });
        [HttpPost] public IActionResult BildirimleriGordum() => Json(new { success = true });
        [HttpPost] public IActionResult BildirimOkundu() => Json(new { success = true });
        [HttpGet] public IActionResult EkranRozetleri() => Json(new { success = true, rozetler = new object[0] });
        [HttpPost] public IActionResult EkranGordum() => Json(new { success = true });
        [HttpGet] public IActionResult SonGuncellemeler() => Json(new { success = true, liste = PortalDegisiklikler.Liste.OrderByDescending(x => x.Tarih).Take(10).Select(x => new { ekran = x.Ekran, aciklama = x.Aciklama, tarih = x.Tarih, tur = x.Tur, url = x.Href }) });
        [HttpGet] public IActionResult ZoomYonetimi() => Json(new { success = true, kademeler = ZoomKademeleri, kullanicilar = OrnekVeri.Kullanicilar.Select(k => new { kod = k.Kod, ad = k.Ad, zoom = 100 }) });

        [HttpPost]
        public IActionResult KullaniciTercihKaydet(string anahtar, string deger)
        {
            string kod = User.Identity?.Name ?? "";
            if (!_tercihler.TryGetValue(kod, out var t)) _tercihler[kod] = t = new Dictionary<string, string>();
            t[anahtar ?? ""] = deger;
            return Json(new { success = true });
        }
        public static string Tercih(string kod, string anahtar) => _tercihler.TryGetValue(kod ?? "", out var t) && t.TryGetValue(anahtar, out var v) ? v : null;

        [HttpPost]
        public IActionResult ToggleFavoriteMenu(int menuId)
        {
            string kod = User.Identity?.Name ?? "";
            var f = OrnekVeri.Favoriler.FirstOrDefault(x => x.UserCode.Equals(kod, StringComparison.OrdinalIgnoreCase) && x.MenuId == menuId);
            if (f != null) OrnekVeri.Favoriler.Remove(f); else OrnekVeri.Favoriler.Add(new UserFavoriteMenu { Id = OrnekVeri.Favoriler.Count + 1, UserCode = kod, MenuId = menuId });
            return Json(new { success = true, favori = f == null });
        }

        [HttpPost] public IActionResult SetMaintenance() => Json(new { success = false, message = "Ön yüz örneğinde bakım modu yok." });
        [HttpPost] public IActionResult ImhaEt() => Json(new { success = false, message = "Ön yüz örneğinde bu işlem yok." });
        [HttpPost] public IActionResult IstemciHata() => Json(new { success = true });
    }

}
