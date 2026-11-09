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
    /// Personel Muhasebe İşlemleri — İK ve İdari İşler › İnsan Kaynakları › Personel Maliyet ve Bordro (eski adı: Personel Maliyet Yansıtması; /PersonelYansitma buraya yönlenir).
    /// Personel ile ilgili tüm muhasebe işlemlerinin ayrıntılı ekranı. Personel Maliyet Raporu › Muhasebe Kayıtları sekmesiyle AYNI veriyi (Data/PersonelMuhasebeOrnek.cs) kullanır:
    /// burada yapılan onay / muhasebeleştirme / ters kayıt / hesap eşleştirmesi orada da görünür (ve tersi). Sekmeler: Muhasebe Kayıtları (personel bazlı satırlar, kontroller, fişler,
    /// özet, audit, aksiyonlar) · Ödemeler (banka R / kasa X net ödeme listesi) · Yansıtma Hazırlığı · Yansıtma Kayıtları (+ simülasyon mutabakatı) · Kalem Eşleme (GDR.13 ↔ YNS.13) ·
    /// Hesap Eşleştirme · Fiş Arşivi. Yansıtma kuralları: Data/PersonelYansitmaOrnek.cs. Betik: wwwroot/js/personel-muhasebe.js
    /// </summary>
    [Authorize]
    public class PersonelMuhasebeController : Controller
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        private string Yetki() => OrnekVeri.Kullanicilar.FirstOrDefault(k => string.Equals(k.Kod, User?.Identity?.Name ?? "", StringComparison.OrdinalIgnoreCase)).Yetki ?? "";
        public static readonly string[] Sekmeler = { "kayitlar", "odemeler", "hazirlik", "yansitma", "esleme", "hesap", "arsiv" };
        private static string Donem(string d) => PersonelMaliyetOrnek.ListeDonem(d) != null ? d : PersonelMaliyetOrnek.EnYeniDonem;

        public IActionResult Index(string sekme, string donem)
        {
            ViewData["Title"] = "Personel Muhasebe İşlemleri";
            ViewBag.Sekme = Sekmeler.Contains(sekme ?? "") ? sekme : "kayitlar";
            ViewBag.Donem = Donem(donem);
            ViewBag.DonemAd = PersonelMaliyetOrnek.DonemAd(Donem(donem));
            return View();
        }

        /// <summary>Seçili dönemin tüm paketi: muhasebe (simülasyonlar, eşleştirme), ödemeler, yansıtma, arşiv.</summary>
        [HttpGet] public IActionResult Veri(string donem) => Json(Paket(donem, true, null));

        // ---- muhasebe kayıtları (Personel Maliyet Raporu › Muhasebe Kayıtları ile aynı motor)
        public class SimuleIstek { public string Donem { get; set; } public string Kapsam { get; set; } }
        public class IslemIstek { public int Id { get; set; } public string Islem { get; set; } public string Not { get; set; } public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Simule([FromBody] SimuleIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMuhasebeOrnek.Olustur(Donem(d.Donem), d.Kapsam, Ben(), out var s);
            if (hata != null) return Json(new { success = false, message = hata });
            return Json(Paket(d.Donem, true, $"{s.No} v{s.Versiyon} oluşturuldu: {s.PersonelSayisi} personel, {s.Fisler.Count} fiş, {s.Fisler.Sum(f => f.Satirlar.Count)} satır · {s.Durum}. Gerçek fiş atılmadı."));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Islem([FromBody] IslemIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMuhasebeOrnek.Islem(d.Id, d.Islem, d.Not, Ben(), Yetki(), out var s);
            if (hata != null) return Json(new { success = false, message = hata });
            string m = d.Islem == "onayaGonder" ? $"{s.No} v{s.Versiyon} onaya gönderildi." : d.Islem == "onayla" ? $"{s.No} v{s.Versiyon} onaylandı; içerik kilitlendi." : d.Islem == "muhasebelestir" ? $"{s.No} muhasebeleştirildi: {string.Join(", ", s.Fisler.Select(f => f.MuhasebeFisNo))}." : d.Islem == "tersKayit" ? $"{s.No} için ters kayıt oluşturuldu: {string.Join(", ", s.Fisler.Select(f => f.TersFisNo))}." : $"{s.No} iptal edildi.";
            return Json(Paket(d.Donem ?? s.Donem, true, m + " Konsolide İK Paneli › Muhasebe Kayıtları sekmesi de güncellendi."));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Esleme([FromBody] PersonelMaliyetController.EslemeIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMuhasebeOrnek.EslemeKaydet(d.Id, d.Sirket, d.KarsiSirket, d.IslemTuru, d.KaynakKalem, d.XR, d.Yon, d.HesapKodu, d.Varsayilan, d.GecerlilikBas, d.GecerlilikBit, d.Aktif, Ben(), out var e);
            if (hata != null) return Json(new { success = false, message = hata });
            return Json(Paket(d.Donem, true, $"Hesap eşleştirmesi kaydedildi: {e.IslemTuru} / {e.KaynakKalem} / {e.XR} / {e.Yon} → {e.HesapKodu} {e.HesapAdi}. Açık simülasyonlar güncelliğini yitirdi; yeniden simülasyon gerekir."));
        }

        // ---- yansıtma (eski Personel Maliyet Yansıtması uçlarıyla aynı kurallar; yanıt bu ekranın paketi)
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult YansitOlustur([FromBody] PersonelYansitmaController.OlusturIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var (olusan, atlanan, kalemsiz, hata) = PersonelYansitmaOrnek.Olustur(d.Donem, d.Siralar, Ben());
            if (hata != null) return Json(new { success = false, message = hata });
            return Json(Paket(d.Donem, true, PersonelYansitmaController.OlusturMesaji(olusan, atlanan, kalemsiz), olusan.Select(k => k.No).ToList()));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult YansitFatura([FromBody] PersonelYansitmaController.NoIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.FaturaOlustur(d.No, Ben()); if (h != null) return Json(new { success = false, message = h });
            PyKayit k; lock (PersonelYansitmaOrnek.Kilit) k = PersonelYansitmaOrnek.Kayitlar.First(x => x.No == d.No);
            return Json(Paket(d.Donem ?? k.Donem, true, $"{k.No}: yansıtma faturası {k.FaturaNo} oluşturuldu ({k.KaynakSirket} → {k.HedefSirket})."));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult YansitIptal([FromBody] PersonelYansitmaController.NoIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.Iptal(d.No, d.Neden, Ben()); if (h != null) return Json(new { success = false, message = h });
            PyKayit k; lock (PersonelYansitmaOrnek.Kilit) k = PersonelYansitmaOrnek.Kayitlar.First(x => x.No == d.No);
            return Json(Paket(d.Donem ?? k.Donem, true, $"{k.No} iptal edildi; iki şirketteki fişler ters kayıtla kapatıldı."));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult KalemEsleme([FromBody] PersonelYansitmaController.EslemeIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.EslemeKaydet(d.Alan, d.GiderKodu); if (h != null) return Json(new { success = false, message = h });
            var k = PersonelYansitmaOrnek.KatalogBul(d.GiderKodu);
            return Json(Paket(d.Donem, true, $"Kalem eşlemesi güncellendi: {d.Alan} → {k.GiderKodu} {k.Kalem} ↔ {k.YansitmaKodu} {k.YansitmaKalem}. Yeni kayıtlarda geçerlidir."));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Dagilim([FromBody] PersonelYansitmaController.DagilimIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.DagilimKaydet(d.Sira, d.Dagilim?.Select(x => (x.Sirket, x.Pay)).ToList()); if (h != null) return Json(new { success = false, message = h });
            return Json(Paket(d.Donem, true, d.Dagilim == null || d.Dagilim.Count == 0 ? "Dağılım varsayılana döndürüldü." : "Görev yeri dağılımı kaydedildi; hedef tutarlar yeniden hesaplandı."));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Kdv([FromBody] PersonelYansitmaController.KdvIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.KdvKaydet(d.Kdv); if (h != null) return Json(new { success = false, message = h });
            return Json(Paket(d.Donem, true, $"Yansıtma faturası KDV oranı %{d.Kdv.ToString("0.##", Tr)} olarak kaydedildi (yeni kayıtlarda geçerli)."));
        }

        // ---- paket: muhasebe paketi Personel Maliyet kilidi altında (Personel Maliyet Raporu ile aynı sıra), yansıtma paketi kendi kilidi altında (kilit sırası çakışmasın diye ayrı)
        private object Paket(string donem, bool ok, string message, List<string> yeniler = null)
        {
            donem = Donem(donem);
            object muhasebe, odemeler, filtreler; List<object> arsiv;
            lock (PersonelMaliyetOrnek.Kilit) { PersonelMaliyetEkOrnek.UretimYansit(); muhasebe = PersonelMuhasebeOrnek.Paket(donem, Ben(), Yetki()); odemeler = PersonelMuhasebeOrnek.OdemePaketi(donem); arsiv = PersonelMuhasebeOrnek.Arsiv(); filtreler = PersonelMuhasebeOrnek.Filtreler(donem); }
            object yansitma; lock (PersonelYansitmaOrnek.Kilit) yansitma = PersonelYansitmaOrnek.Paket(donem, Ben(), true, null);
            return new
            {
                success = ok, message, yeniler, donem, donemAd = PersonelMaliyetOrnek.DonemAd(donem), donemler = PersonelMaliyetOrnek.DonemPaketi, ben = Ben(), yetki = Yetki(),
                muhasebe, odemeler, arsiv, yansitma, filtreler,   // filtreler: SGK İşvereni / Fiili Görev Yeri / Ödeme Şirketi seçenekleri (tüm sekmelerde ortak)
                raporAdresi = "/PersonelMaliyet?sekme=muhasebe&donem=" + donem, kartAdresi = "/PersonelMaliyet?kod="
            };
        }
    }
}
