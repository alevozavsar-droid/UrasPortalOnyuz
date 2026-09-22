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
        /// <summary>
        /// Yansıtma İşlemleri: "URAS HOLDING Yansıtma Modülü IT Gereksinim Dokümanı v1.0" ile ayrı modüle dönüştü (Data/YansitmaOrnek.cs,
        /// Controllers/YansitmaController.cs, Views/GrupIci/Yansitma.cshtml, wwwroot/js/yansitma.js). Aynı üç sekme: İşlem Ekranı / Raporu / Muhasebe Kontrolü.
        /// ?ust=islem|rapor|kontrol, ?no=YNS-000125 detay açar.
        /// </summary>
        public IActionResult Yansitma(string sekme, string no, string ust)
        {
            ViewData["Title"] = "Yansıtma İşlemleri";
            ViewBag.Ust = ust == "rapor" || ust == "kontrol" ? ust : "islem"; ViewBag.No = no ?? "";
            ViewBag.Sirketler = YansitmaOrnek.Sirketler;
            return View("~/Views/GrupIci/Yansitma.cshtml");
        }
        /// <summary>
        /// Mahsup İşlemleri: "URAS HOLDING Mahsup Modülü IT Gereksinim Dokümanı v1.0" ile ayrı modüle dönüştü (Data/MahsupOrnek.cs,
        /// Controllers/MahsupController.cs, Views/GrupIci/Mahsup.cshtml, wwwroot/js/mahsup.js). Üç sekme: İşlem Ekranı / Raporu / Muhasebe Kontrolü.
        /// </summary>
        public IActionResult Mahsup(string sekme, string no, string ust)
        {
            ViewData["Title"] = "Mahsup İşlemleri";
            ViewBag.Ust = ust == "rapor" || ust == "kontrol" ? ust : "islem"; ViewBag.No = no ?? "";
            return View("~/Views/GrupIci/Mahsup.cshtml");
        }
        /// <summary>
        /// Temlik İşlemleri: "URAS HOLDING Temlik Modülü IT Gereksinim Dokümanı v1.0" ile ayrı modüle dönüştü (Data/TemlikOrnek.cs,
        /// Controllers/TemlikController.cs, Views/GrupIci/Temlik.cshtml, wwwroot/js/temlik.js). Üç sekme: İşlem Ekranı / Raporu / Muhasebe Kontrolü.
        /// </summary>
        public IActionResult Temlik(string sekme, string no, string ust)
        {
            ViewData["Title"] = "Temlik İşlemleri";
            ViewBag.Ust = ust == "rapor" || ust == "kontrol" ? ust : "islem"; ViewBag.No = no ?? "";
            return View("~/Views/GrupIci/Temlik.cshtml");
        }

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
