using WebApplication3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>Bellek içi mesajlaşma: uygulama yeniden başlayınca örnek mesajlara döner.</summary>
    [Authorize]
    [Route("[controller]")]
    public class MesajController : Controller
    {
        private string Ben => User?.Identity?.Name ?? "";
        private static readonly object _kilit = new object();

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string ile = null) { ViewBag.Ben = Ben; ViewBag.Ile = ile ?? ""; return View(); }

        [HttpGet("Kisiler")]
        public IActionResult Kisiler()
        {
            var kisiler = OrnekVeri.Kullanicilar.Where(k => !k.Kod.Equals(Ben, StringComparison.OrdinalIgnoreCase)).Select(k =>
            {
                var son = OrnekVeri.Mesajlar.Where(m => (m.Gonderen == k.Kod && m.Alici == Ben) || (m.Gonderen == Ben && m.Alici == k.Kod)).OrderByDescending(m => m.Id).FirstOrDefault();
                string metin = son == null ? null : (string.IsNullOrWhiteSpace(son.Metin) && son.Ekler.Count > 0 ? "📎 Dosya" : son.Metin);
                return new { kod = k.Kod, ad = k.Ad, sonMesaj = metin != null && metin.Length > 80 ? metin.Substring(0, 80) + "…" : metin, sonTarih = son?.Tarih, sonBenden = son != null && son.Gonderen == Ben, okunmamis = OrnekVeri.Mesajlar.Count(m => m.Gonderen == k.Kod && m.Alici == Ben && m.Okundu == null) };
            }).OrderByDescending(k => k.sonTarih.HasValue).ThenByDescending(k => k.sonTarih).ThenBy(k => k.ad).ToList();
            return Json(new { success = true, ben = Ben, kisiler });
        }

        [HttpGet("Konusma")]
        public IActionResult Konusma(string kullanici, int sonId = 0)
        {
            var liste = OrnekVeri.Mesajlar.Where(m => ((m.Gonderen == Ben && m.Alici == kullanici) || (m.Gonderen == kullanici && m.Alici == Ben)) && m.Id > sonId).OrderBy(m => m.Id).ToList();
            foreach (var m in OrnekVeri.Mesajlar.Where(m => m.Gonderen == kullanici && m.Alici == Ben && m.Okundu == null)) m.Okundu = DateTime.Now;
            int? sonOkunan = OrnekVeri.Mesajlar.Where(m => m.Gonderen == Ben && m.Alici == kullanici && m.Okundu != null).Select(m => (int?)m.Id).Max();
            return Json(new
            {
                success = true, ben = Ben, karsi = kullanici, karsiAd = OrnekVeri.KullaniciAdi(kullanici), sonOkunan,
                mesajlar = liste.Select(m => new { id = m.Id, benden = m.Gonderen == Ben, metin = m.Metin, tarih = m.Tarih, okundu = m.Okundu != null, ekler = m.Ekler.Select(e => new { id = e.Id, ad = e.Ad, tur = e.Tur, boyut = e.Boyut, resimMi = (e.Tur ?? "").StartsWith("image/") }) })
            });
        }

        [HttpPost("Gonder")]
        public IActionResult Gonder(string alici, string metin)
        {
            if (string.IsNullOrWhiteSpace(metin)) return Json(new { success = false, message = "Boş mesaj gönderilemez." });
            lock (_kilit)
            {
                var m = new OrnekVeri.Mesaj { Id = OrnekVeri.SonMesajId() + 1, Gonderen = Ben, Alici = alici, Metin = metin.Trim(), Tarih = DateTime.Now };
                OrnekVeri.Mesajlar.Add(m);
                // Örnek: karşı taraf 3 sn sonra otomatik yanıt verir (canlı görünüm için)
                var cevap = new OrnekVeri.Mesaj { Id = m.Id + 1, Gonderen = alici, Alici = Ben, Metin = "Ön yüz örneği otomatik yanıt: \"" + (metin.Length > 40 ? metin.Substring(0, 40) + "…" : metin) + "\" mesajını aldım 👍", Tarih = DateTime.Now.AddSeconds(3) };
                OrnekVeri.Mesajlar.Add(cevap);
                return Json(new { success = true, id = m.Id, tarih = m.Tarih });
            }
        }

        [HttpPost("GonderEk"), RequestSizeLimit(60_000_000)]
        public IActionResult GonderEk(string alici, string metin, List<IFormFile> dosyalar)
        {
            lock (_kilit)
            {
                var m = new OrnekVeri.Mesaj { Id = OrnekVeri.SonMesajId() + 1, Gonderen = Ben, Alici = alici, Metin = (metin ?? "").Trim(), Tarih = DateTime.Now };
                foreach (var f in (dosyalar ?? new List<IFormFile>()).Where(f => f != null && f.Length > 0))
                {
                    using var ms = new MemoryStream(); f.CopyTo(ms);
                    m.Ekler.Add(new OrnekVeri.MesajEk { Id = OrnekVeri.SonEkId() + 1 + m.Ekler.Count, Ad = Path.GetFileName(f.FileName), Tur = f.ContentType, Boyut = f.Length, Icerik = ms.ToArray() });
                }
                if (m.Ekler.Count == 0 && m.Metin.Length == 0) return Json(new { success = false, message = "Dosya seçin ya da mesaj yazın." });
                OrnekVeri.Mesajlar.Add(m);
                return Json(new { success = true, id = m.Id, tarih = m.Tarih, ekler = m.Ekler.Select(e => new { id = e.Id, ad = e.Ad, tur = e.Tur, boyut = e.Boyut, resimMi = (e.Tur ?? "").StartsWith("image/") }) });
            }
        }

        [HttpGet("Ek/{id:int}")]
        public IActionResult Ek(int id, bool indir = false)
        {
            var e = OrnekVeri.Mesajlar.SelectMany(m => m.Ekler).FirstOrDefault(x => x.Id == id);
            if (e == null) return NotFound();
            if (indir) return File(e.Icerik, e.Tur ?? "application/octet-stream", e.Ad);
            Response.Headers["Content-Disposition"] = "inline; filename*=UTF-8''" + Uri.EscapeDataString(e.Ad);
            return File(e.Icerik, e.Tur ?? "application/octet-stream");
        }

        [HttpGet("OkunmamisSayisi")]
        public IActionResult OkunmamisSayisi()
        {
            var okunmamis = OrnekVeri.Mesajlar.Where(m => m.Alici == Ben && m.Okundu == null && m.Tarih <= DateTime.Now).OrderByDescending(m => m.Id).ToList();
            var son = okunmamis.FirstOrDefault();
            return Json(new { success = true, adet = okunmamis.Count, sonId = son?.Id, sonKod = son?.Gonderen, sonAd = son == null ? null : OrnekVeri.KullaniciAdi(son.Gonderen), sonMetin = son?.Metin });
        }
    }
}
