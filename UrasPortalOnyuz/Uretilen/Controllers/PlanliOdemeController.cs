// PLANLI ÖDEMELER — Mali İşler › Finans › Ödeme Yönetimi › Planlı Ödemeler (/PlanliOdemeler)
// Konsolide Finansal Takip Paneli › Düzenli Ödemeler sekmesiyle AYNI VERİ ve AYNI TASARIM: Data/DuzenliOdemeOrnek.cs (aylık ödeme takvim listesi — tedarikçi, ürün / hizmet, şirket,
// ödeme şekli, ödeme günü, ortalama aylık tutar, kategori). Her ay aktif her kayıt için o ayın ödeme gününe planlanmış bir Kasa Nakit Akışı satırı açılır; burada ya da panelde yapılan
// kayıt / düzenleme / pasif / akıt işlemleri iki ekranda da anında görünür (tek liste, bellek içi). Ekran: Views/PlanliOdeme/Index.cshtml · betik: wwwroot/js/planli-odeme.js
// (Eski "hatırlatma e-postası + taksit planı" ekranı kaldırıldı; Uretilen/Services/PlanliOdeme* dosyaları kullanılmıyor.)
using System;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("PlanliOdemeler")]
    public class PlanliOdemeController : Controller
    {
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        private string Kullanici => NakitAvansOrnek.KisiBul(User?.Identity?.Name)?.AdSoyad ?? (User?.Identity?.Name ?? "Finans");

        /// <param name="sirket">ön süzgeç: şirket DbName (ör. URASKIMYA)</param>
        /// <param name="id">açılışta çekmecede gösterilecek kayıt (DÖ-… Id)</param>
        [HttpGet("")]
        public IActionResult Index(string sirket, int? id)
        {
            ViewData["Title"] = "Planlı Ödemeler";
            ViewBag.Sirket = sirket ?? ""; ViewBag.Id = id?.ToString() ?? "";
            return View("Index");
        }

        /// <summary>Liste + özet (Takip Paneli › Düzenli Ödemeler ile birebir aynı nesne); bu ay için akmamış aktif kayıtlar her açılışta tamamlanır (ay başı otomatik işi).</summary>
        [HttpGet("Veri")]
        public IActionResult Veri(string sirket)
        {
            string db = string.IsNullOrEmpty(sirket) || string.Equals(sirket, "tumu", StringComparison.OrdinalIgnoreCase) ? null : sirket;
            var oto = DuzenliOdemeOrnek.OtomatikAkit("Sistem (otomatik)"); var l = DuzenliOdemeOrnek.Listele(db);
            return Json(new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), kullanici = Kullanici,
                sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, db = s.DbName }).ToList(),
                duzenli = new { liste = l.Select(DuzenliOdemeOrnek.Json).ToList(), ozet = DuzenliOdemeOrnek.Ozet(l), sorumlu = DuzenliOdemeOrnek.Sorumlu, odemeSekilleri = DuzenliOdemeOrnek.OdemeSekilleri, kategoriler = DuzenliOdemeOrnek.Kategoriler, sirketler = CekAkibetOrnek.Sirketler.Select(s => s.Ad).ToList(), otomatik = oto.eklenen, otomatikMesaj = oto.mesaj }
            });
        }

        public class Istek { public int Id { get; set; } public string Ay { get; set; } public string DbName { get; set; } public string Tedarikci { get; set; } public string UrunHizmet { get; set; } public string Sirket { get; set; } public string OdemeSekli { get; set; } public string Kategori { get; set; } public int Gun { get; set; } public decimal AylikTutar { get; set; } public string Not { get; set; } public string Durum { get; set; } }

        /// <summary>Bir ayın (bu ay / gelecek ay) akmamış aktif kayıtlarını — ya da tek kaydı — Kasa Nakit Akışı'na akıtır (aynı aya akmış olanlar atlanır).</summary>
        [HttpPost("Akit")]
        public IActionResult Akit([FromBody] Istek i)
        {
            if (i == null) return Json(new { success = false, message = "Veri yok." });
            string db = string.IsNullOrEmpty(i.DbName) || string.Equals(i.DbName, "tumu", StringComparison.OrdinalIgnoreCase) ? null : i.DbName;
            var r = DuzenliOdemeOrnek.Akit(string.IsNullOrEmpty(i.Ay) ? DuzenliOdemeOrnek.BuAy : i.Ay, Kullanici, db, i.Id > 0 ? i.Id : (int?)null);
            return Json(new { success = r.ok, message = r.mesaj, eklenen = r.eklenen, atlanan = r.atlanan });
        }
        [HttpPost("Kaydet")]
        public IActionResult Kaydet([FromBody] Istek i)
        {
            if (i == null) return Json(new { success = false, message = "Veri yok." });
            var r = DuzenliOdemeOrnek.Kaydet(i.Id, i.Tedarikci, i.UrunHizmet, i.Sirket, i.OdemeSekli, i.Kategori, i.Gun, i.AylikTutar, i.Not, Kullanici);
            if (r.ok) FinekraOrnek.GunlukEkle(r.d.Sirket, r.d.No, i.Id > 0 ? "Düzenli Ödeme Güncellendi" : "Düzenli Ödeme Eklendi", $"{r.d.Tedarikci} · {r.d.UrunHizmet} · {r.d.OdemeSekli} · {r.d.AylikTutar.ToString("N2", Tr)} ₺ · gün {r.d.Gun} · Planlı Ödemeler ekranı", Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, kayit = r.d == null ? null : DuzenliOdemeOrnek.Json(r.d) });
        }
        [HttpPost("Durum")]
        public IActionResult Durum([FromBody] Istek i)
        {
            if (i == null) return Json(new { success = false, message = "Veri yok." });
            var r = DuzenliOdemeOrnek.DurumDegistir(i.Id, i.Durum, Kullanici);
            if (r.ok) FinekraOrnek.GunlukEkle(r.d.Sirket, r.d.No, r.d.Durum == "Pasif" ? "Düzenli Ödeme Pasif" : "Düzenli Ödeme Aktif", r.mesaj + " (Planlı Ödemeler ekranı)", Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, kayit = r.d == null ? null : DuzenliOdemeOrnek.Json(r.d) });
        }
    }
}
