using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Vade Tarihine Göre Çek Özeti — Mali İşler › Finans › Finansal Araçlar (Yönetim › Özel Raporlar'da aynı ekran). Sekmeler:
    ///   Verilen Çekler / Verilen Senetler    : verilen belgeler vade ayına göre cari bazında (tedarikçi, maaş, kira); 12 ay + yıllık toplam + aylık ortalama + pay %,
    ///                                          tür ara toplamları, verilen toplam ve adet; altında tahsile gönderilen (Ziraat Bankası / Halk Bankası) ve NET; hücreden belge listesi
    ///   Tahsile Gönderilen Çekler / Senetler : bankaya ibraz edilen müşteri belgeleri banka bazında (tahsil edildi / tahsilde / karşılıksız); durum ve banka filtresi
    ///   Aylık Nakit Görünümü                 : çek ve senet ortak — ay ay verilen (çek / senet, tür bazında) ↔ tahsile gönderilen (çek / senet, banka bazında), net, kümülatif
    /// Veri: Data/CekOrnek.cs (verilen çekler) + Data/ButceOrnek.cs › Cekler (alınan müşteri çekleri; Bütçe Raporu ile aynı kaynak). Hesaplar istemcide (wwwroot/js/cek-ozet.js).
    /// </summary>
    [Authorize]
    public class CekOzetController : Controller
    {
        /// <param name="sekme">vc (Verilen Çekler, varsayılan) | vs (Verilen Senetler) | tc (Tahsile Gönderilen Çekler) | ts (Tahsile Gönderilen Senetler) | nakit (çek + senet ortak)</param>
        public IActionResult Index(string sekme, string cari)
        {
            ViewData["Title"] = "Vade Tarihine Göre Çek ve Senet Özeti";
            ViewBag.Sekme = new[] { "vs", "tc", "ts", "nakit" }.Contains(sekme) ? sekme : sekme == "tahsil" ? "tc" : "vc";
            ViewBag.Cari = cari ?? "";
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Sirketler = CekOrnek.Sirketler;
            ViewBag.VarsayilanSirket = CekOrnek.VarsayilanSirket;   // açılışta seçili şirket (kullanıcının şirketi; 2026 verilen çekleri bütçe analizi tablosuyla birebir)
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
                    alinanDurumlari = ButceOrnek.CekDurumlari, tahsilBankalari = ButceOrnek.TahsilBankalari, belgeler = new[] { "Çek", "Senet" },
                    // verilen çek ve senetler (belge alanı); alınan müşteri çek ve senetleri — tahsile gönderilenler yalnız Ziraat Bankası ya da Halk Bankası'na ibraz edilir (tahsilBankasi)
                    verilen = CekOrnek.Verilen.Select(c => new { c.Sirket, c.No, c.CariKod, c.Cari, c.Tur, c.Banka, keside = c.Keside.ToString("dd.MM.yyyy"), vade = c.Vade.ToString("dd.MM.yyyy"), yil = c.Vade.Year, ay = c.Vade.Month, c.Tutar, c.Durum, c.Aciklama, c.Belge }),
                    alinan = ButceOrnek.Cekler.Concat(ButceOrnek.Senetler).Select(c => new { c.Sirket, c.No, c.MusteriKod, c.Musteri, c.Banka, vade = c.Vade.ToString("dd.MM.yyyy"), yil = c.Vade.Year, ay = c.Vade.Month, c.Tutar, c.Durum, tahsile = c.TahsileGonderildi, tahsilBankasi = c.TahsilBankasi, c.Belge })
                });
            }
        }
    }
}
