// ŞİRKET DÖKÜMANLARI (/SirketDokuman; Yönetim › Şirket Dökümanları) — tüm grup şirketlerinin resmî belgeleri tek yerde; kategori kırılımları ayrı menü raporları (aynı ekran, ön süzgeç).
// Veri: Data/SirketDokumanOrnek.cs. Uçlar: Veri, Belge (satır), Kaydet (multipart: alanlar + dosya), Surum (multipart), Yenile, Arsivle, Dosya (sürüm dosyası ya da kapak sayfası PDF'i), SirketPdf (şirket dosya dizini), Excel.
// Ekran: Views/SirketDokuman/Index.cshtml · betik: wwwroot/js/sirket-dokuman.js
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("SirketDokuman")]
    public class SirketDokumanController : Controller
    {
        private string Kod() => User?.Identity?.Name ?? "";
        private string Ad() => OrnekVeri.KullaniciAdi(Kod());

        IActionResult Sayfa(string kategori, string sekme, string sirket, int? id)
        {
            var k = SirketDokumanOrnek.KategoriBul(kategori ?? "");
            ViewData["Title"] = k?.Ad ?? "Şirket Dökümanları";
            ViewBag.Kategori = k?.Kod ?? ""; ViewBag.KategoriAd = k?.Ad ?? ""; ViewBag.Sekme = new[] { "belgeler", "matris", "takvim", "eksik", "sirketler", "yeni", "gunluk" }.Contains(sekme) ? sekme : "belgeler"; ViewBag.Sirket = sirket ?? ""; ViewBag.Id = id?.ToString() ?? ""; ViewBag.Ben = Ad();
            return View("Index");
        }
        /// <param name="sekme">belgeler | matris | takvim | eksik | sirketler | yeni | gunluk</param><param name="sirket">ön süzgeç: şirket kodu (URASKIMYA …)</param><param name="id">açılışta çekmecede gösterilecek belge</param>
        [HttpGet("")] public IActionResult Index(string sekme, string sirket, int? id) => Sayfa("", sekme, sirket, id);
        [HttpGet("Takvim")] public IActionResult Takvim(string sirket) => Sayfa("", "takvim", sirket, null);
        [HttpGet("Eksik")] public IActionResult Eksik(string sirket) => Sayfa("", "eksik", sirket, null);
        [HttpGet("Sirketler")] public IActionResult Sirketler() => Sayfa("", "sirketler", null, null);
        [HttpGet("Kurulus")] public IActionResult Kurulus(string sekme, string sirket) => Sayfa("kurulus", sekme, sirket, null);
        [HttpGet("Vergi")] public IActionResult Vergi(string sekme, string sirket) => Sayfa("vergi", sekme, sirket, null);
        [HttpGet("Imza")] public IActionResult Imza(string sekme, string sirket) => Sayfa("imza", sekme, sirket, null);
        [HttpGet("Ruhsat")] public IActionResult Ruhsat(string sekme, string sirket) => Sayfa("ruhsat", sekme, sirket, null);
        [HttpGet("Sozlesme")] public IActionResult Sozlesme(string sekme, string sirket) => Sayfa("sozlesme", sekme, sirket, null);
        [HttpGet("Sigorta")] public IActionResult Sigorta(string sekme, string sirket) => Sayfa("sigorta", sekme, sirket, null);
        [HttpGet("Banka")] public IActionResult Banka(string sekme, string sirket) => Sayfa("banka", sekme, sirket, null);
        [HttpGet("Marka")] public IActionResult Marka(string sekme, string sirket) => Sayfa("marka", sekme, sirket, null);
        [HttpGet("Varlik")] public IActionResult Varlik(string sekme, string sirket) => Sayfa("varlik", sekme, sirket, null);

        [HttpGet("Veri")]
        public IActionResult Veri()
        {
            var l = SirketDokumanOrnek.Belgeler;
            return Json(new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(), yaklasanGun = SirketDokumanOrnek.YaklasanGun,
                sirketler = SirketDokumanOrnek.Sirketler.Select(SirketDokumanOrnek.SirketJson).ToList(), kategoriler = SirketDokumanOrnek.Kategoriler.Select(SirketDokumanOrnek.KategoriJson).ToList(), turler = SirketDokumanOrnek.Turler.Select(SirketDokumanOrnek.TurJson).ToList(),
                durumlar = SirketDokumanOrnek.Durumlar, gizlilikler = SirketDokumanOrnek.Gizlilikler, sorumlular = l.Select(b => b.Sorumlu).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x).ToList(),
                belgeler = l.OrderBy(b => b.Arsiv).ThenBy(b => b.GecerlilikBitis ?? DateTime.MaxValue).ThenBy(b => b.SirketKod).Select(SirketDokumanOrnek.Json).ToList(),
                eksikler = SirketDokumanOrnek.Eksikler().Select(e => new { sirketKod = e.Sirket.Kod, sirket = e.Sirket.Ad, sirketRenk = e.Sirket.Renk, turKod = e.Tur.Kod, tur = e.Tur.Ad, kategori = e.Tur.Kategori, kategoriAd = SirketDokumanOrnek.KategoriBul(e.Tur.Kategori)?.Ad, kurum = e.Tur.Kurum, sureliAy = e.Tur.SureliAy, sorumlu = SirketDokumanOrnek.KategoriBul(e.Tur.Kategori)?.Sorumlu }).ToList(),
                matris = SirketDokumanOrnek.Matris(l), ozet = SirketDokumanOrnek.Ozet(l),
                gunluk = l.SelectMany(b => b.Hareketler.Select(h => new { belgeId = b.Id, no = b.No, sirket = SirketDokumanOrnek.SirketBul(b.SirketKod)?.Ad, ad = b.Ad, zaman = h.Zaman, h.Kullanici, h.Islem, h.Aciklama })).OrderByDescending(h => h.zaman).Take(300).Select(h => new { h.belgeId, h.no, h.sirket, h.ad, zaman = h.zaman.ToString("yyyy-MM-dd HH:mm"), h.Kullanici, h.Islem, h.Aciklama }).ToList()
            });
        }
        [HttpGet("Belge")]
        public IActionResult Belge(int id) { var b = SirketDokumanOrnek.Bul(id); return b == null ? Json(new { success = false, message = "Belge bulunamadı." }) : Json(new { success = true, belge = SirketDokumanOrnek.Json(b) }); }

        /// <summary>Yeni belge / güncelleme: form alanları + isteğe bağlı dosya (multipart).</summary>
        [HttpPost("Kaydet")]
        public IActionResult Kaydet([FromForm] SdBelgeIstek i, IFormFile dosya)
        {
            byte[] veri = null; if (dosya != null && dosya.Length > 0) { using var ms = new MemoryStream(); dosya.CopyTo(ms); veri = ms.ToArray(); }
            var r = SirketDokumanOrnek.Kaydet(i, dosya?.FileName, dosya?.ContentType, veri, Ad()); return Json(new { success = r.ok, message = r.mesaj, belge = r.b == null ? null : SirketDokumanOrnek.Json(r.b) });
        }
        [HttpPost("Surum")]
        public IActionResult Surum(int id, string not, IFormFile dosya)
        {
            byte[] veri = null; if (dosya != null && dosya.Length > 0) { using var ms = new MemoryStream(); dosya.CopyTo(ms); veri = ms.ToArray(); }
            var r = SirketDokumanOrnek.SurumEkle(id, dosya?.FileName, dosya?.ContentType, veri, not, Ad()); return Json(new { success = r.ok, message = r.mesaj, belge = r.b == null ? null : SirketDokumanOrnek.Json(r.b) });
        }
        [HttpPost("Yenile")] public IActionResult Yenile([FromBody] SdYenileIstek i) { var r = SirketDokumanOrnek.Yenile(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, belge = r.b == null ? null : SirketDokumanOrnek.Json(r.b) }); }
        public class ArsivIstek { public int Id { get; set; } public bool Arsiv { get; set; } }
        [HttpPost("Arsivle")] public IActionResult Arsivle([FromBody] ArsivIstek i) { var r = SirketDokumanOrnek.Arsivle(i?.Id ?? 0, i?.Arsiv ?? true, Ad()); return Json(new { success = r.ok, message = r.mesaj, belge = r.b == null ? null : SirketDokumanOrnek.Json(r.b) }); }

        /// <summary>Sürüm dosyası (yüklenmişse) ya da örnek belge için kapak sayfası PDF'i.</summary>
        [HttpGet("Dosya")]
        public IActionResult Dosya(int id, int? surum)
        {
            var b = SirketDokumanOrnek.Bul(id); if (b == null) return NotFound("Belge bulunamadı.");
            var v = surum.HasValue ? b.Surumler.FirstOrDefault(x => x.No == surum.Value) : b.SonSurum;
            if (v?.Veri != null) return File(v.Veri, v.Tip ?? "application/octet-stream", v.Dosya);
            return File(DisaAktar.Pdf(SirketDokumanOrnek.KapakSayfasi(b)), "application/pdf", v?.Dosya ?? $"{b.No}.pdf");
        }
        [HttpGet("SirketPdf")]
        public IActionResult SirketPdf(string kod) { var s = SirketDokumanOrnek.SirketBul(kod ?? ""); if (s == null) return NotFound("Şirket bulunamadı."); return File(DisaAktar.Pdf(SirketDokumanOrnek.SirketDosyasi(s, Ad())), "application/pdf", $"sirket-dosyasi-{s.Kod.ToLowerInvariant()}-{DateTime.Today:yyyyMMdd}.pdf"); }
        [HttpGet("Excel")]
        public IActionResult Excel(string kategori, string sirket)
        {
            var l = SirketDokumanOrnek.Belgeler.Where(b => (string.IsNullOrEmpty(kategori) || b.Kategori == kategori) && (string.IsNullOrEmpty(sirket) || b.SirketKod == sirket)).OrderBy(b => b.SirketKod).ThenBy(b => b.Kategori).ThenBy(b => b.Ad).ToList();
            var d = new DaBelge { Baslik = "Şirket Dökümanları" + (string.IsNullOrEmpty(kategori) ? "" : " · " + SirketDokumanOrnek.KategoriBul(kategori)?.Ad), AltBaslik = $"{SirketDokumanOrnek.Sirketler.Count} şirket · {DateTime.Today:dd.MM.yyyy}", Sirket = "Uras Grup", Hazirlayan = Ad() };
            var bl = new DaBlok { Baslik = "Belgeler", Kolonlar = { "Portal No", "Şirket", "Kategori", "Belge", "Belge No", "Düzenleyen", "Düzenleme", "Geçerlilik", "Kalan Gün", "Durum", "Zorunlu", "Sorumlu", "Gizlilik", "Sürüm", "Dosya", "Not" }, Sag = { false, false, false, false, false, false, false, false, true, false, false, false, false, false, false, false } };
            foreach (var b in l) bl.Satirlar.Add(new[] { b.No, SirketDokumanOrnek.SirketBul(b.SirketKod)?.Ad, SirketDokumanOrnek.KategoriBul(b.Kategori)?.Ad, b.Ad, b.BelgeNo, b.Kurum, b.DuzenlemeTarihi.ToString("dd.MM.yyyy"), b.GecerlilikBitis?.ToString("dd.MM.yyyy") ?? "Süresiz", b.KalanGun?.ToString() ?? "", b.Durum, SirketDokumanOrnek.TurBul(b.TurKod)?.Zorunlu == true ? "Evet" : "Hayır", b.Sorumlu, b.Gizlilik, b.SonSurum == null ? "" : "v" + b.SonSurum.No, b.SonSurum?.Dosya ?? "", b.Not });
            d.Bloklar.Add(bl);
            var ek = new DaBlok { Baslik = "Eksik Zorunlu Belgeler", Kolonlar = { "Şirket", "Kategori", "Belge Türü", "Düzenleyen Kurum", "Sorumlu" } }; foreach (var e in SirketDokumanOrnek.Eksikler().Where(e => (string.IsNullOrEmpty(kategori) || e.Tur.Kategori == kategori) && (string.IsNullOrEmpty(sirket) || e.Sirket.Kod == sirket))) ek.Satirlar.Add(new[] { e.Sirket.Ad, SirketDokumanOrnek.KategoriBul(e.Tur.Kategori)?.Ad, e.Tur.Ad, e.Tur.Kurum, SirketDokumanOrnek.KategoriBul(e.Tur.Kategori)?.Sorumlu }); d.Bloklar.Add(ek);
            return File(DisaAktar.Excel(d), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"sirket-dokumanlari-{DateTime.Today:yyyyMMdd}.xlsx");
        }
    }
}
