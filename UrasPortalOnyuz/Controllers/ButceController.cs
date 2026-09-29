using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Bütçe Raporu — Mali İşler › Finans › Bütçe ve Planlama. Sekmeler:
    ///   Bütçe Özeti               : üç gider grubunun (Sabit Giderler, Tedarik Zinciri · Hammadde, Genel Giderler) aylık bütçe / gerçekleşen / sapma tablosu,
    ///                               KPI'lar, şirket bazlı özet, en yüksek sapmalar, bütçe revizyon günlüğü
    ///   Sabit Giderler            : personel, kira, sigorta, amortisman, lisans, finansman, vergi-aidat kalemleri (alt grup ara toplamlı)
    ///   Tedarik Zinciri · Hammadde: hammadde, ticari mal, ambalaj, lojistik, ithalat, sarf kalemleri
    ///   Genel Giderler            : enerji, bakım, danışmanlık, hukuk, seyahat, pazarlama, ofis, iletişim, hizmet, eğitim, banka, temsil, bağış
    /// Her kalem satırı: seçili ay ve yıl başından bugüne (YTD) bütçe / gerçekleşen / sapma, yıllık bütçe, kullanım ve durum; satırdan detay (aylık kırılım,
    /// şirket kırılımı, açık aylara bütçe revizyonu). Veri ve kurallar: Data/ButceOrnek.cs; hesaplar istemcide (wwwroot/js/butce.js).
    /// </summary>
    [Authorize]
    public class ButceController : Controller
    {
        /// <param name="sekme">ozet (varsayılan) | sabit | tedarik | genel</param>
        /// <param name="kalem">açılışta detayı açılacak kalem kodu (ör. GG-01)</param>
        public IActionResult Index(string sekme, string kalem)
        {
            ViewData["Title"] = "Bütçe Raporu";
            ViewBag.Sekme = new[] { "sabit", "tedarik", "genel" }.Contains(sekme) ? sekme : "ozet";
            ViewBag.Kalem = kalem ?? "";
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Sirketler = ButceOrnek.Sirketler;
            ViewBag.KapanisAy = ButceOrnek.KapanmisAy(DateTime.Today.Year) > 0 ? ButceOrnek.Aylar[ButceOrnek.KapanmisAy(DateTime.Today.Year) - 1] : "—";
            ViewBag.GeciciAy = ButceOrnek.Aylar[DateTime.Today.Month - 1];
            return View();
        }

        [HttpGet]
        public IActionResult Veri()
        {
            lock (ButceOrnek.Kilit)
            {
                return Json(new
                {
                    success = true,
                    bugun = DateTime.Today.ToString("yyyy-MM-dd"), yil = DateTime.Today.Year, ay = DateTime.Today.Month,
                    yillar = ButceOrnek.Yillar, sirketler = ButceOrnek.Sirketler, gruplar = ButceOrnek.Gruplar, aylar = ButceOrnek.Aylar,
                    kapanmisAy = ButceOrnek.Yillar.ToDictionary(y => y.ToString(), y => ButceOrnek.KapanmisAy(y)),
                    geciciAy = ButceOrnek.Yillar.ToDictionary(y => y.ToString(), y => ButceOrnek.GeciciAy(y)),
                    kalemler = ButceOrnek.Kalemler.Select(k => new { k.Kod, k.Grup, k.AltGrup, k.Ad, k.HesapKodu, k.Sorumlu, sirketler = k.Sirketler, not = k.Not ?? "", k.Egilim }),
                    satirlar = ButceOrnek.Satirlar.Select(s => new { s.Sirket, s.Yil, s.KalemKod, s.Butce, s.IlkButce, s.Gercek }),
                    revizyonlar = ButceOrnek.Revizyonlar.Select(RevDto)
                });
            }
        }

        /// <summary>Açık aya bütçe revizyonu (şirket + yıl + kalem + ay + tutar + gerekçe).</summary>
        [HttpPost]
        public IActionResult Revize([FromBody] BtIstek g)
        {
            string kullanici = OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
            var hata = ButceOrnek.Revize(g, kullanici);
            var s = hata == null ? ButceOrnek.Bul(g.Sirket, g.Yil, g.KalemKod) : null;
            return Json(new
            {
                success = hata == null,
                message = hata ?? $"{ButceOrnek.KalemBul(g.KalemKod)?.Ad} · {ButceOrnek.Aylar[g.Ay - 1]} {g.Yil} bütçesi revize edildi.",
                satir = s == null ? null : new { s.Sirket, s.Yil, s.KalemKod, s.Butce, s.IlkButce, s.Gercek },
                revizyonlar = ButceOrnek.Revizyonlar.Select(RevDto)
            });
        }

        private static object RevDto(BtRevizyon r) => new { r.Id, zaman = r.Zaman.ToString("dd.MM.yyyy HH:mm"), r.Kullanici, r.Sirket, r.Yil, r.KalemKod, r.KalemAd, r.Ay, ayAd = ButceOrnek.Aylar[r.Ay - 1], r.Onceki, r.Yeni, fark = r.Yeni - r.Onceki, r.Gerekce };
    }
}
