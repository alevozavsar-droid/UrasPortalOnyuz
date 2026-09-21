using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Genel Analiz Raporu — Yönetim › Yönetim Özeti › Özel Analizler › Özel Raporlar.
    /// "2026 Aylık Analiz Raporu" Excel şablonunun portal karşılığı: bölüm / satır / 12 ay / TOPLAM / AYLIK ORT. matrisi,
    /// alt toplam ve sonuç satırları otomatik. Yaprak hücreler ekrandan girilip kaydedilir (bellek içi).
    /// Yapı ve örnek veri: Data/GenelAnalizOrnek.cs
    /// </summary>
    [Authorize]
    public class GenelAnalizController : Controller
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");

        public IActionResult Index()
        {
            ViewData["Title"] = "Genel Analiz Raporu";
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Kullanici = Ben();
            return View();
        }

        /// <summary>Satır yapısı + yılın yaprak değerleri (hesaplanan satırlar istemcide de sunucuda da aynı kuralla üretilir).</summary>
        [HttpGet]
        public IActionResult Veri(int yil)
        {
            if (yil == 0) yil = DateTime.Today.Year;
            lock (GenelAnalizOrnek.Kilit)
            {
                var d = GenelAnalizOrnek.Degerler(yil);
                var kayit = GenelAnalizOrnek.SonKayit.TryGetValue(yil, out var k) ? new { kisi = k.Kisi, tarih = k.Tarih.ToString("dd.MM.yyyy HH:mm") } : null;
                return Json(new
                {
                    success = true, yil, kapaliAy = GenelAnalizOrnek.KapaliAy(yil), aylar = GenelAnalizOrnek.Aylar, sonKayit = kayit,
                    satirlar = GenelAnalizOrnek.Satirlar.Select(s => new { k = s.K, ad = s.Ad, bolum = s.Bolum, tip = s.Tip, terimler = s.Terimler.Select(t => new { k = t.K, i = t.Isaret }) }),
                    degerler = d
                });
            }
        }

        /// <summary>Yaprak satır değerlerini kaydeder. Gövde: { yil, degerler: { anahtar: [12 sayı | null] } }</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Kaydet([FromBody] KaydetIstek d)
        {
            if (d == null || d.Degerler == null) return Json(new { success = false, message = "Veri yok." });
            int yil = d.Yil == 0 ? DateTime.Today.Year : d.Yil;
            string hata = GenelAnalizOrnek.Kaydet(yil, d.Degerler, Ben());
            return hata != null ? Json(new { success = false, message = hata }) : Json(new { success = true, message = $"{yil} Aylık Analiz Raporu kaydedildi ({d.Degerler.Count} satır).", kisi = Ben(), tarih = DateTime.Now.ToString("dd.MM.yyyy HH:mm") });
        }
        public class KaydetIstek { public int Yil { get; set; } public Dictionary<string, decimal?[]> Degerler { get; set; } }

        /// <summary>Excel şablonuyla aynı düzende CSV (noktalı virgül; Excel doğrudan açar).</summary>
        [HttpGet]
        public IActionResult Csv(int yil)
        {
            if (yil == 0) yil = DateTime.Today.Year;
            var h = GenelAnalizOrnek.Hesapla(yil);
            var sb = new StringBuilder();
            sb.AppendLine($"{yil} AYLIK ANALİZ RAPORU");
            sb.AppendLine("BÖLÜM;AÇIKLAMA;" + string.Join(";", GenelAnalizOrnek.Aylar.Select(a => a.ToUpper(Tr))) + $";TOPLAM {yil};AYLIK ORT.");
            string sonBolum = null;
            foreach (var s in GenelAnalizOrnek.Satirlar)
            {
                var v = h[s.K]; var dolu = v.Where(x => x.HasValue).Select(x => x.Value).ToList();
                string F(decimal? x) => x.HasValue ? x.Value.ToString("N0", Tr) : "";
                sb.AppendLine($"{(s.Bolum != sonBolum ? s.Bolum : "")};{s.Ad};" + string.Join(";", v.Select(F)) + ";" + (dolu.Count > 0 ? dolu.Sum().ToString("N0", Tr) : "") + ";" + (dolu.Count > 0 ? dolu.Average().ToString("N0", Tr) : ""));
                sonBolum = s.Bolum;
            }
            return File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray(), "text/csv; charset=utf-8", $"{yil}_Aylik_Analiz_Raporu.csv");
        }
    }
}
