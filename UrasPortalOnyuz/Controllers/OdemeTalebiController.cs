using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Talep › Yeni Talep › Ödeme Talebi: birimlerin ödeme talebi ekranı (talep motoru modülü Data/OdemeTalebiModulu.cs; görünüm Views/IkTalep/Modul.cshtml +
    /// js/talep-modulu.js; veri uçları IkTalep/Tm*). Cari kapsamı talep eden kişinin birimine göre: satın alma satınalmacısı olduğu tedarikçiler, idari işler
    /// temsilcisi olduğu cariler, muhasebe / finans resmi kurumlar ve finansal kuruluşlar. Onaylanan talep Kasa Nakit Akışı › Departman Talepleri'ne düşer.
    /// </summary>
    [Authorize]
    public class OdemeTalebiController : Controller
    {
        // Ekranın üstünde tür şeridi yok (tek modül); boş dizi → Modul.cshtml şeridi çizmez.
        private static readonly (string Url, string Ad, string Ikon, string Kod)[] Sekmeler = new (string, string, string, string)[0];

        /// <summary>?sekme=yeni|liste|detay|onay|takip, ?no=ÖT-2026-000312 detay açar.</summary>
        public IActionResult Index(string sekme, string no) => Sayfa(sekme, no);
        public IActionResult Onay() => Sayfa("onay", null);
        public IActionResult Takip() => Sayfa("takip", null);

        private IActionResult Sayfa(string sekme, string no)
        {
            var m = TalepMotoru.Modul(OdemeTalebiModulu.Kod); if (m == null) return NotFound();
            ViewData["Title"] = m.Ad; ViewBag.Kod = m.Kod; ViewBag.Sekme = string.IsNullOrEmpty(sekme) ? (string.IsNullOrEmpty(no) ? "yeni" : "detay") : sekme; ViewBag.No = no ?? "";
            ViewBag.Sekmeler = Sekmeler;
            return View("~/Views/IkTalep/Modul.cshtml");
        }
    }
}
