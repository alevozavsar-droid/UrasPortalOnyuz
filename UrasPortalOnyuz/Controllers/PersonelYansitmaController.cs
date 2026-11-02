using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Personel Maliyet Yansıtması — 2 Kas 2026'da "Personel Muhasebe İşlemleri" ekranına (Controllers/PersonelMuhasebeController.cs) dönüştü:
    /// yansıtma hazırlığı, yansıtma kayıtları ve kalem eşleme orada sekme olarak sürer; eski adres oraya yönlenir. Veri uçları eski bağlantılar için korunur
    /// (aynı Data/PersonelYansitmaOrnek.cs). Kural: SGK işvereni ≠ fiili görev yeri olan personelin aylık maliyeti GDR.13.AA.KK ↔ YNS.13.AA.KK ile iki şirkete kaydedilir.
    /// </summary>
    [Authorize]
    public class PersonelYansitmaController : Controller
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");

        /// <param name="sekme">eski: hazirlik | kayitlar | esleme → yeni ekranın hazirlik | yansitma | esleme sekmesi</param>
        public IActionResult Index(string sekme)
        {
            string yeni = sekme == "kayitlar" ? "yansitma" : sekme == "esleme" ? "esleme" : "hazirlik";
            return Redirect("/PersonelMuhasebe?sekme=" + yeni);
        }

        /// <summary>Dönemin adayları (otomatik tespit), tüm kayıtlar, eşleme, katalog ve parametreler.</summary>
        [HttpGet]
        public IActionResult Veri(string donem)
        {
            if (string.IsNullOrWhiteSpace(donem)) donem = PersonelYansitmaOrnek.DonemBugun;
            lock (PersonelYansitmaOrnek.Kilit) return Json(PersonelYansitmaOrnek.Paket(donem, Ben(), true, null));
        }

        public class OlusturIstek { public string Donem { get; set; } public List<int> Siralar { get; set; } }
        /// <summary>Seçilen personel için iki taraflı yansıtma kayıtlarını oluşturur.</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Olustur([FromBody] OlusturIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var (olusan, atlanan, kalemsiz, hata) = PersonelYansitmaOrnek.Olustur(d.Donem, d.Siralar, Ben());
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelYansitmaOrnek.Kilit) return Json(PersonelYansitmaOrnek.Paket(d.Donem, Ben(), true, OlusturMesaji(olusan, atlanan, kalemsiz), olusan.Select(k => k.No).ToList()));
        }
        /// <summary>Toplu yansıtma sonucunun özeti (yeni ekranla ortak).</summary>
        public static string OlusturMesaji(List<PyKayit> olusan, int atlanan, int kalemsiz)
        {
            var notlar = new List<string>();
            if (atlanan > 0) notlar.Add($"{atlanan} kayıt bu dönemde zaten mevcut");
            if (kalemsiz > 0) notlar.Add($"{kalemsiz} personelde maliyet kalemi yok (tutarlar 0)");
            return olusan.Count == 0 ? $"Yeni kayıt oluşmadı; {string.Join(", ", notlar)}." :
                $"{olusan.Count} yansıtma kaydı oluşturuldu ({olusan.Select(k => k.KaynakSirket + " → " + k.HedefSirket).Distinct().Count()} şirket çifti, {olusan.Sum(k => k.Toplam).ToString("N2", Tr)} TL); her kayıt SGK işvereni ve fiili görev yeri şirketine fiş olarak atıldı{(notlar.Count > 0 ? "; atlanan: " + string.Join(", ", notlar) : "")}.";
        }

        public class NoIstek { public string No { get; set; } public string Neden { get; set; } public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult FaturaOlustur([FromBody] NoIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.FaturaOlustur(d.No, Ben()); if (h != null) return Json(new { success = false, message = h });
            lock (PersonelYansitmaOrnek.Kilit) { var k = PersonelYansitmaOrnek.Kayitlar.First(x => x.No == d.No); return Json(PersonelYansitmaOrnek.Paket(d.Donem ?? k.Donem, Ben(), true, $"{k.No}: yansıtma faturası {k.FaturaNo} oluşturuldu ({k.KaynakSirket} → {k.HedefSirket}).")); }
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Iptal([FromBody] NoIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.Iptal(d.No, d.Neden, Ben()); if (h != null) return Json(new { success = false, message = h });
            lock (PersonelYansitmaOrnek.Kilit) { var k = PersonelYansitmaOrnek.Kayitlar.First(x => x.No == d.No); return Json(PersonelYansitmaOrnek.Paket(d.Donem ?? k.Donem, Ben(), true, $"{k.No} iptal edildi; iki şirketteki fişler ters kayıtla kapatıldı.")); }
        }

        public class EslemeIstek { public string Alan { get; set; } public string GiderKodu { get; set; } public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult EslemeKaydet([FromBody] EslemeIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.EslemeKaydet(d.Alan, d.GiderKodu); if (h != null) return Json(new { success = false, message = h });
            var k = PersonelYansitmaOrnek.KatalogBul(d.GiderKodu);
            lock (PersonelYansitmaOrnek.Kilit) return Json(PersonelYansitmaOrnek.Paket(d.Donem ?? PersonelYansitmaOrnek.DonemBugun, Ben(), true, $"Eşleme güncellendi: {d.Alan} → {k.GiderKodu} {k.Kalem} ↔ {k.YansitmaKodu} {k.YansitmaKalem}. Yeni kayıtlarda geçerlidir."));
        }

        public class DagilimIstek { public int Sira { get; set; } public string Donem { get; set; } public List<DagilimSatir> Dagilim { get; set; } }
        public class DagilimSatir { public string Sirket { get; set; } public decimal Pay { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult DagilimKaydet([FromBody] DagilimIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.DagilimKaydet(d.Sira, d.Dagilim?.Select(x => (x.Sirket, x.Pay)).ToList()); if (h != null) return Json(new { success = false, message = h });
            lock (PersonelYansitmaOrnek.Kilit) return Json(PersonelYansitmaOrnek.Paket(d.Donem ?? PersonelYansitmaOrnek.DonemBugun, Ben(), true, d.Dagilim == null || d.Dagilim.Count == 0 ? "Dağılım varsayılana döndürüldü." : "Görev yeri dağılımı kaydedildi; hedef tutarlar yeniden hesaplandı."));
        }

        public class KdvIstek { public decimal Kdv { get; set; } public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult KdvKaydet([FromBody] KdvIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.KdvKaydet(d.Kdv); if (h != null) return Json(new { success = false, message = h });
            lock (PersonelYansitmaOrnek.Kilit) return Json(PersonelYansitmaOrnek.Paket(d.Donem ?? PersonelYansitmaOrnek.DonemBugun, Ben(), true, $"Yansıtma faturası KDV oranı %{d.Kdv.ToString("0.##", Tr)} olarak kaydedildi (yeni kayıtlarda geçerli)."));
        }
    }
}
