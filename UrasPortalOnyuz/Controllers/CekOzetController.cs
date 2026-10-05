using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Vade Tarihine Göre Çek Özeti — Mali İşler › Finans › Finansal Araçlar (Yönetim › Özel Raporlar'da aynı ekran). Sekmeler:
    ///   Verilen Çekler          : şirketin verdiği tüm çekler vade ayına göre cari bazında (tedarikçi, maaş, kira, sigorta ve diğer); 12 ay + yıllık toplam +
    ///                             aylık ortalama + pay %, tür ara toplamları, aylık toplam ve çek adedi satırları; hücreden çek listesi
    ///   Tahsile Gönderilen Çekler: müşteriden alınıp bankaya tahsile gönderilen çekler vade ayına göre müşteri bazında (tahsil edildi / tahsilde / karşılıksız);
    ///                             portföyde bekleyen ve ciro edilenler ayrıca
    ///   Aylık Nakit Görünümü    : ay ay verilen (tür bazında) ↔ tahsile gönderilen, net ve kümülatif
    /// Veri: Data/CekOrnek.cs (verilen çekler) + Data/ButceOrnek.cs › Cekler (alınan müşteri çekleri; Bütçe Raporu ile aynı kaynak). Hesaplar istemcide (wwwroot/js/cek-ozet.js).
    /// </summary>
    [Authorize]
    public class CekOzetController : Controller
    {
        /// <param name="sekme">verilen (varsayılan) | tahsil | nakit</param>
        public IActionResult Index(string sekme, string cari)
        {
            ViewData["Title"] = "Vade Tarihine Göre Çek Özeti";
            ViewBag.Sekme = sekme == "tahsil" || sekme == "nakit" ? sekme : "verilen";
            ViewBag.Cari = cari ?? "";
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Sirketler = CekOrnek.Sirketler;
            return View();
        }

        [HttpGet]
        public IActionResult Veri()
        {
            lock (CekOrnek.Kilit)
            {
                return Json(new
                {
                    success = true,
                    bugun = DateTime.Today.ToString("yyyy-MM-dd"), yil = DateTime.Today.Year, ay = DateTime.Today.Month,
                    yillar = CekOrnek.Yillar, sirketler = CekOrnek.Sirketler, turler = CekOrnek.Turler, durumlar = CekOrnek.Durumlar, aylar = ButceOrnek.Aylar,
                    alinanDurumlari = ButceOrnek.CekDurumlari, tahsilBankalari = ButceOrnek.TahsilBankalari,
                    verilen = CekOrnek.Verilen.Select(c => new { c.Sirket, c.No, c.CariKod, c.Cari, c.Tur, c.Banka, keside = c.Keside.ToString("dd.MM.yyyy"), vade = c.Vade.ToString("dd.MM.yyyy"), yil = c.Vade.Year, ay = c.Vade.Month, c.Tutar, c.Durum, c.Aciklama }),
                    // alınan müşteri çekleri; tahsile gönderilenler yalnız Ziraat Bankası ya da Halk Bankası'na ibraz edilir (tahsilBankasi)
                    alinan = ButceOrnek.Cekler.Select(c => new { c.Sirket, c.No, c.MusteriKod, c.Musteri, c.Banka, vade = c.Vade.ToString("dd.MM.yyyy"), yil = c.Vade.Year, ay = c.Vade.Month, c.Tutar, c.Durum, tahsile = c.TahsileGonderildi, tahsilBankasi = c.TahsilBankasi })
                });
            }
        }
    }
}
