// PLANLI ÖDEMELER — Mali İşler › Finans › Ödeme Yönetimi › Planlı Ödemeler (/PlanliOdemeler)
// Konsolide Finansal Takip Paneli › Düzenli Ödemeler sekmesiyle AYNI VERİ ve AYNI TASARIM: Data/DuzenliOdemeOrnek.cs (aylık ödeme takvim listesi — tedarikçi, ürün / hizmet, şirket,
// ödeme şekli, ödeme günü, ortalama aylık tutar, kategori). Her ay aktif her kayıt için o ayın ödeme gününe planlanmış bir Kasa Nakit Akışı satırı açılır; burada ya da panelde yapılan
// kayıt / düzenleme / pasif / akıt işlemleri iki ekranda da anında görünür (tek liste, bellek içi). Ekran: Views/PlanliOdeme/Index.cshtml · betik: wwwroot/js/planli-odeme.js
// (Eski "hatırlatma e-postası + taksit planı" ekranı kaldırıldı; Uretilen/Services/PlanliOdeme* dosyaları kullanılmıyor.)
using System;
using System.Collections.Generic;
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
        /// <param name="sekme">açılış sekmesi: liste (varsayılan) · ozet · matris · takvim</param>
        [HttpGet("")]
        public IActionResult Index(string sirket, int? id, string sekme)
        {
            ViewData["Title"] = "Planlı Ödemeler";
            ViewBag.Sirket = sirket ?? ""; ViewBag.Id = id?.ToString() ?? "";
            ViewBag.Sekme = new[] { "liste", "ozet", "matris", "takvim" }.Contains(sekme) ? sekme : "liste";
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
                duzenli = new { liste = l.Select(DuzenliOdemeOrnek.Json).ToList(), ozet = DuzenliOdemeOrnek.Ozet(l), sorumlu = DuzenliOdemeOrnek.Sorumlu, odemeSekilleri = DuzenliOdemeOrnek.OdemeSekilleri, kategoriler = DuzenliOdemeOrnek.Kategoriler, sirketler = CekAkibetOrnek.Sirketler.Select(s => s.Ad).ToList(), otomatik = oto.eklenen, otomatikMesaj = oto.mesaj,
                    kullanimlar = DuzenliOdemeOrnek.Kullanimlar, cekPlanUfku = DuzenliOdemeOrnek.CekPlanUfku, butceKalemleri = ButceOrnek.Kalemler.Select(k => new { kod = k.Kod, ad = k.Ad, grup = k.Grup, altGrup = k.AltGrup }).ToList() }
            });
        }

        public class Istek { public int Id { get; set; } public string Ay { get; set; } public string DbName { get; set; } public string Tedarikci { get; set; } public string UrunHizmet { get; set; } public string Sirket { get; set; } public string OdemeSekli { get; set; } public string Kategori { get; set; } public int Gun { get; set; } public decimal AylikTutar { get; set; } public string Not { get; set; } public string Durum { get; set; } public string ButceKalem { get; set; } public int[] Cekler { get; set; } public string Kullanim { get; set; } public int CekDocEntry { get; set; } }

        /// <summary>Bir ay için seçilebilir portföy çekleri / senetleri (tüm grup şirketleri; vade uygunluğu ödeme gününe göre) + o aya zaten planlı olanlar.</summary>
        [HttpGet("PortfoyCekler")]
        public IActionResult PortfoyCekler(int id, string ay)
        {
            var d = DuzenliOdemeOrnek.Bul(id); if (d == null) return Json(new { success = false, message = "Kayıt bulunamadı." });
            if (!DuzenliOdemeOrnek.AyGecerli(ay)) return Json(new { success = false, message = "Ay AAAA-AA biçiminde olmalı." });
            var tarih = DuzenliOdemeOrnek.OdemeTarihi(ay, d.Gun); var bugun = DateTime.Today;
            HashSet<int> planli; lock (DuzenliOdemeOrnek.Kilit) planli = new HashSet<int>(d.CekPlanlari.Where(p => p.Ay == ay).Select(p => p.CekDocEntry));
            var adaylar = CekAkibetOrnek.Secilebilirler(null, "Çek").Concat(CekAkibetOrnek.Secilebilirler(null, "Senet")).Concat(CekAkibetOrnek.BulHepsi(planli)).GroupBy(k => k.DocEntry).Select(g => g.First()).ToList();
            string Uygunluk(CekKaydi k) { int gun = (tarih - k.Vade.Date).Days; return k.Vade.Date < bugun ? "Vadesi Geçmiş" : gun >= 0 ? "Uygun" : gun >= -7 ? "Sınırda" : "Vade Geç"; }
            int Sira(string u) => u == "Uygun" ? 0 : u == "Sınırda" ? 1 : u == "Vade Geç" ? 2 : 3;
            var cekler = adaylar.Select(k => new { k.DocEntry, k.No, k.Belge, k.Sirket, k.Musteri, k.Banka, vade = k.Vade.ToString("yyyy-MM-dd"), vadeTr = k.Vade.ToString("dd.MM.yyyy"), k.Tutar, k.IslemTipi, k.IslemTuru, uygunluk = Uygunluk(k), gunFark = (tarih - k.Vade.Date).Days, secili = planli.Contains(k.DocEntry) })
                .OrderByDescending(x => x.secili).ThenBy(x => Sira(x.uygunluk)).ThenBy(x => x.uygunluk == "Uygun" ? x.gunFark : -x.gunFark).ThenBy(x => x.vade).ToList();
            decimal planlanan; lock (DuzenliOdemeOrnek.Kilit) planlanan = d.CekPlanlari.Where(p => p.Ay == ay).Sum(p => p.Tutar);
            return Json(new { success = true, id, ay, ayAdi = DuzenliOdemeOrnek.AyAdi(ay), tarih = tarih.ToString("yyyy-MM-dd"), ihtiyac = d.AylikTutar, planlanan, kalan = Math.Max(0, d.AylikTutar - planlanan), kullanimOneri = DuzenliOdemeOrnek.KullanimOner(d), kullanimlar = DuzenliOdemeOrnek.Kullanimlar, cekler });
        }
        /// <summary>Seçilen çek / senetleri bir ayın düzenli ödemesine planlar (Ciro ya da Tahsil).</summary>
        [HttpPost("CekPlanla")]
        public IActionResult CekPlanla([FromBody] Istek i)
        {
            if (i == null) return Json(new { success = false, message = "Veri yok." });
            var r = DuzenliOdemeOrnek.CekPlanla(i.Id, i.Ay, i.Cekler ?? Array.Empty<int>(), i.Kullanim, Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, kayit = r.d == null ? null : DuzenliOdemeOrnek.Json(r.d) });
        }
        [HttpPost("CekPlanKaldir")]
        public IActionResult CekPlanKaldir([FromBody] Istek i)
        {
            if (i == null) return Json(new { success = false, message = "Veri yok." });
            var r = DuzenliOdemeOrnek.CekPlanKaldir(i.Id, i.Ay, i.CekDocEntry, Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, kayit = r.d == null ? null : DuzenliOdemeOrnek.Json(r.d) });
        }

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
            var r = DuzenliOdemeOrnek.Kaydet(i.Id, i.Tedarikci, i.UrunHizmet, i.Sirket, i.OdemeSekli, i.Kategori, i.Gun, i.AylikTutar, i.Not, Kullanici, i.ButceKalem);
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
