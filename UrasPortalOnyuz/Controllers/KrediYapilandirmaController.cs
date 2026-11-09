using System;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FinansRaporlama.Controllers;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// KREDİ RAPORU (9 Kas 2026) — Mali İşler › Finans › Krediler ve Finansman. Veri Data/KrediYapilandirmaOrnek.cs (App_Data/kredi-yapilandirma.json kalıcı, belgeler App_Data/kredi-belgeler),
    /// görünüm Views/KrediYapilandirma/Index.cshtml, betik wwwroot/js/kredi-yapilandirma.js. Yapılandırma öncesi / sonrası kredi bilgileri, peşinat, taksit takvimi (Kasa Nakit Akışı + Planlı Düzenli Ödemeler),
    /// rehin · ipotek takibi (kredi teminatları + eski krediler için bağımsız kayıtlar), teminata verilen çekler, belgeler (sözleşme / GKS / ipotek PDF'leri). ?sekme=krediler|oncesi|sonrasi|takvim|aylik|rehin|cekler|belgeler
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class KrediYapilandirmaController : Controller
    {
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        public class IdIstek { public int Id { get; set; } }
        public class AktarIstek { public int Id { get; set; } public bool Aktar { get; set; } = true; }
        static object Paket() => KrediYapilandirmaOrnek.Paket();

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Kredi Raporu";
            ViewBag.Sekme = string.IsNullOrEmpty(sekme) ? "krediler" : sekme;
            OrnekDoldurucu.Hazirla(this);
            return View("Index");
        }
        [HttpGet("Veri")]
        public IActionResult Veri() => Json(Paket());

        /// <summary>Kredi ekle / düzenle: kaydeder, taksit satırlarını Kasa Nakit Akışı'na ve düzenli ödeme kaydına eşler; tüm paket döner.</summary>
        [HttpPost("Kaydet"), IgnoreAntiforgeryToken]
        public IActionResult Kaydet([FromBody] KrediIstek d)
        {
            var h = KrediYapilandirmaOrnek.Kaydet(d, Kullanici, out var k, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, id = k?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("Sil"), IgnoreAntiforgeryToken]
        public IActionResult Sil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.Sil(d?.Id ?? 0, Kullanici, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, veri = h == null ? Paket() : null });
        }
        /// <summary>Nakit akışa aktarımı aç / kapat (kapalı: ödenmemiş taksit satırları kaldırılır, düzenli ödeme pasife alınır); yeniden eşleme için aynı değerle çağrılabilir.</summary>
        [HttpPost("Aktar"), IgnoreAntiforgeryToken]
        public IActionResult Aktar([FromBody] AktarIstek d)
        {
            var h = KrediYapilandirmaOrnek.AktarDegistir(d?.Id ?? 0, d?.Aktar ?? true, Kullanici, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, veri = h == null ? Paket() : null });
        }

        // ---------- rehin / ipotek takibi (bağımsız kayıtlar) ----------
        [HttpPost("TeminatKaydet"), IgnoreAntiforgeryToken]
        public IActionResult TeminatKaydet([FromBody] TeminatIstek d)
        {
            var h = KrediYapilandirmaOrnek.TeminatKaydet(d, Kullanici, out var t);
            return Json(new { success = h == null, message = h ?? $"Rehin / ipotek kaydı {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {t?.Banka} · {t?.Tur} · {t?.Aciklama}.", id = t?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("TeminatSil"), IgnoreAntiforgeryToken]
        public IActionResult TeminatSil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.TeminatSil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "Rehin / ipotek kaydı silindi.", veri = h == null ? Paket() : null });
        }
        // ---------- teminata verilen çekler ----------
        [HttpPost("CekKaydet"), IgnoreAntiforgeryToken]
        public IActionResult CekKaydet([FromBody] CekIstek d)
        {
            var h = KrediYapilandirmaOrnek.CekKaydet(d, Kullanici, out var c);
            return Json(new { success = h == null, message = h ?? $"Teminat çeki {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {c?.CekNo} · {c?.Banka} · {c?.Durum}.", id = c?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("CekSil"), IgnoreAntiforgeryToken]
        public IActionResult CekSil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.CekSil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "Teminat çeki kaydı silindi.", veri = h == null ? Paket() : null });
        }
        // ---------- belgeler (sözleşme, GKS, ipotek, çek fotokopisi …) ----------
        /// <summary>Çok parçalı yükleme: dosya (PDF / JPG / PNG, en çok 20 MB) + tur, baslik, krediId, cekId, teminatId, sirket, banka, tarih, aciklama.</summary>
        [HttpPost("BelgeYukle"), IgnoreAntiforgeryToken, RequestSizeLimit(25 * 1024 * 1024)]
        public IActionResult BelgeYukle(IFormFile dosya, string tur, string baslik, int krediId, int cekId, int teminatId, string sirket, string banka, string tarih, string aciklama)
        {
            if (dosya == null || dosya.Length == 0) return Json(new { success = false, message = "Dosya seçilmedi." });
            byte[] veri; using (var ms = new MemoryStream()) { dosya.CopyTo(ms); veri = ms.ToArray(); }
            var h = KrediYapilandirmaOrnek.BelgeEkle(tur, baslik, krediId, cekId, teminatId, sirket, banka, tarih, aciklama, dosya.FileName, dosya.ContentType, veri, Kullanici, out var b);
            return Json(new { success = h == null, message = h ?? $"Belge yüklendi: {b?.Tur} · {b?.Baslik} ({Math.Max(1, (b?.Boyut ?? 0) / 1024)} KB).", id = b?.Id, veri = h == null ? Paket() : null });
        }
        /// <summary>Belgeyi açar (PDF / görsel tarayıcıda; ?indir=1 ile indirir).</summary>
        [HttpGet("Belge/{id:int}")]
        public IActionResult Belge(int id, int indir = 0)
        {
            var b = KrediYapilandirmaOrnek.BelgeBul(id); var yol = KrediYapilandirmaOrnek.BelgeYolu(b);
            if (b == null || yol == null) return NotFound("Belge bulunamadı.");
            if (indir == 1) return PhysicalFile(yol, b.IcerikTuru, b.DosyaAdi);
            var cd = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline"); cd.SetHttpFileName(b.DosyaAdi);   // Türkçe karakterli dosya adı RFC 5987 (filename*) ile; düz yazım 500 verir
            Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition] = cd.ToString();
            return PhysicalFile(yol, b.IcerikTuru);
        }
        [HttpPost("BelgeSil"), IgnoreAntiforgeryToken]
        public IActionResult BelgeSil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.BelgeSil(d?.Id ?? 0, Kullanici);
            return Json(new { success = h == null, message = h ?? "Belge silindi.", veri = h == null ? Paket() : null });
        }
    }
}
