using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Mali İşler › Grup İçi Özel Finansal İşlemler: Yansıtma, Mahsup ve Temlik işlem ekranları (talep motoru; tanımlar
    /// Data/TalepModulleri.cs, veri uçları IkTalep/Tm*) ve üçünü birleştiren rapor (Views/GrupIci/Rapor.cshtml).
    /// </summary>
    [Authorize]
    public class GrupIciController : Controller
    {
        // Ekranların üstünde tür sekmesi YOK: Yansıtma, Mahsup, Temlik ve Rapor ayrı ekranlardır; geçiş sol menüden
        // (Mali İşler › Grup İçi Özel Finansal İşlemler) yapılır. Boş dizi → Views/IkTalep/Modul.cshtml sekme şeridini çizmez.
        private static readonly (string Url, string Ad, string Ikon, string Kod)[] Sekmeler = new (string, string, string, string)[0];

        /// <summary>
        /// Her 2. alt menü (Yansıtma / Mahsup / Temlik) aynı sayfada üç üst sekme taşır (GRUP_ICI_OZEL_FINANSAL_ISLEMLER_SEKME_YAPISI):
        /// Sekme 1 "İşlem Ekranı" (varsayılan), Sekme 2 "Raporu", Sekme 3 "Muhasebe Kontrolü". Rapor ve kontrol ayrı menü değildir;
        /// ?ust=islem|rapor|kontrol ile açılır (wwwroot/js/grup-ici-sekmeler.js).
        /// </summary>
        public IActionResult Yansitma(string sekme, string no, string ust) => Sayfa("YANSITMA", sekme, no, ust);
        public IActionResult Mahsup(string sekme, string no, string ust) => Sayfa("MAHSUP", sekme, no, ust);
        public IActionResult Temlik(string sekme, string no, string ust) => Sayfa("TEMLIK", sekme, no, ust);

        private IActionResult Sayfa(string kod, string sekme, string no, string ust)
        {
            var m = TalepMotoru.Modul(kod); if (m == null) return NotFound();
            ViewData["Title"] = m.Ad; ViewBag.Kod = m.Kod; ViewBag.Sekme = string.IsNullOrEmpty(sekme) ? (string.IsNullOrEmpty(no) ? "yeni" : "detay") : sekme; ViewBag.No = no ?? "";
            ViewBag.Sekmeler = Sekmeler;
            ViewBag.GrupIci = true;
            ViewBag.Ust = ust == "rapor" || ust == "kontrol" ? ust : "islem";
            return View("~/Views/IkTalep/Modul.cshtml");
        }

        /// <summary>Üç işlem türünün birleşik raporu ve gösterge kutucukları.</summary>
        public IActionResult Rapor()
        {
            ViewData["Title"] = "Grup İçi Özel Finansal İşlemler Raporu";
            ViewBag.Sekmeler = Sekmeler;
            ViewBag.Sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct().ToArray();
            return View();
        }
    }
}
