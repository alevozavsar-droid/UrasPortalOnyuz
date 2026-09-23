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
    /// Personel Maliyet Raporu — Yönetim › Özel Analizler › Özel Raporlar ve İK ve İdari İşler › İnsan Kaynakları › Personel Maliyet ve Bordro.
    /// "PERSONEL 19.06 GÜNCEL" tablosunun portal karşılığı: kimlik bilgileri + brüt ücret (X / R-net), işveren payı, ek çalışma,
    /// sosyal yardımlar, prim, yan haklar, yasal ödemeler ve genel maliyet; filtreler, KPI, özet raporu, satır ve kart düzenleme,
    /// personel ekleme / silme (bellek içi) ve Excel. Veri ve kurallar: Data/PersonelMaliyetOrnek.cs · Betik: wwwroot/js/personel-maliyet.js
    /// </summary>
    [Authorize]
    public class PersonelMaliyetController : Controller
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");

        public IActionResult Index()
        {
            ViewData["Title"] = "Personel Maliyet Raporu";
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Kaynak = PersonelMaliyetOrnek.Kaynak;
            ViewBag.Gruplar = PersonelMaliyetOrnek.SutunGruplari;
            return View();
        }

        [HttpGet]
        public IActionResult Veri()
        {
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, null));
        }

        /// <summary>Düzenlenen satırları kaydeder (tablo düzenlemesi ya da kart). Gövde: { satirlar: [ { sira, adSoyad, …, xNet, …, not } ] }</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Kaydet([FromBody] KaydetIstek d)
        {
            if (d == null || d.Satirlar == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMaliyetOrnek.Kaydet(d.Satirlar, Ben());
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{d.Satirlar.Count} personel satırı kaydedildi."));
        }

        /// <summary>Yeni personel kartı açar. Gövde: PmDuzenleme (sıra yok sayılır).</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Ekle([FromBody] PmDuzenleme d)
        {
            string hata = PersonelMaliyetOrnek.Ekle(d, Ben(), out var p);
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{p.AdSoyad} eklendi (sıra {p.Sira}, cari kodu {p.CariKodu}).", p.Sira));
        }

        /// <summary>Satırı siler (hatalı giriş). Gövde: { sira }</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Sil([FromBody] SilIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMaliyetOrnek.Sil(d.Sira, Ben());
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"Sıra {d.Sira} silindi."));
        }
        public class KaydetIstek { public List<PmDuzenleme> Satirlar { get; set; } }
        public class SilIstek { public int Sira { get; set; } }

        private object Paket(bool ok, string message, int? yeniSira = null) => new
        {
            success = ok, message, yeniSira, yil = DateTime.Today.Year, kaynak = PersonelMaliyetOrnek.Kaynak, ben = Ben(),
            sonKayit = PersonelMaliyetOrnek.SonKayit.HasValue ? new { kisi = PersonelMaliyetOrnek.SonKayit.Value.Kisi, tarih = PersonelMaliyetOrnek.SonKayit.Value.Tarih.ToString("dd.MM.yyyy HH:mm") } : null,
            sgkIsverenleri = PersonelMaliyetOrnek.SgkIsverenleri, gorevYerleri = PersonelMaliyetOrnek.GorevYerleri, catiDepartmanlar = PersonelMaliyetOrnek.CatiDepartmanlar,
            bolumler = PersonelMaliyetOrnek.Bolumler, unvanlar = PersonelMaliyetOrnek.Unvanlar, gorevler = PersonelMaliyetOrnek.Gorevler, ozelDurumlar = PersonelMaliyetOrnek.OzelDurumlar, durumlar = PersonelMaliyetOrnek.Durumlar,
            personel = PersonelMaliyetOrnek.Liste.OrderBy(p => p.Sira).Select(Dto).ToList()   // kilit içinde maddeleştir: serileştirme action döndükten sonra çalışır, ertelenmiş sorgu kilitsiz gezilirdi
        };

        private static object Dto(PmPersonel p) => new
        {
            p.Sira, p.CariKodu, p.SgkIsvereni, p.FiiliGorevYeri, p.AdSoyad, p.Unvan, p.Bolum, p.Gorev, iseGiris = p.IseGiris?.ToString("yyyy-MM-dd"), iseGirisTr = p.IseGiris?.ToString("dd.MM.yyyy"), kidemYil = p.KidemYil, p.CatiDepartman, p.OzelDurum,
            p.Durum, ayrilisTarihi = p.AyrilisTarihi?.ToString("yyyy-MM-dd"), ayrilisTr = p.AyrilisTarihi?.ToString("dd.MM.yyyy"), p.TcKimlik, p.Telefon, p.Eposta,
            p.XNet, p.RNet, p.TopNet, p.BrutUcMasraflari, p.ToplamBrutUcret, p.TopIsvPayi, p.TopEkCalisma, p.Yemek, p.Yol, p.Servis, p.KiraDestegi, p.Egitim, p.TopSosyal, p.SatisPrimi, p.OzelOdeme, p.TopPrim, p.TopYanHaklar, p.TopYasalOd, p.GenelMaliyet, p.Not
        };
    }
}
