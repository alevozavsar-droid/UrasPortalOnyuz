// SİGORTA VE KASKO POLİÇE TAKİBİ (/Sigorta; Yönetim › Şirket Dökümanları › Sigorta · İdari İşler › Araç ve Seyahat kısayolu) — Data/SigortaOrnek.cs
// Uçlar: Veri, Police, Kaydet (multipart: alanlar + poliçe PDF), TaksitOde, Hasar, DosyaYukle (multipart), Yenile, Iptal, Dosya (yüklü dosya ya da özet PDF), Pdf, Excel (liste + taksit planı).
// Ekran: Views/Sigorta/Index.cshtml · betik: wwwroot/js/sigorta.js
using System;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("Sigorta")]
    public class SigortaController : Controller
    {
        private string Ad() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

        /// <param name="sekme">policeler | taksit | araclar | yenileme | yeni | ozet</param><param name="plaka">ön süzgeç: araç plakası</param><param name="id">açılışta çekmecede gösterilecek poliçe</param>
        [HttpGet("")]
        public IActionResult Index(string sekme, string plaka, int? id, string sirket, string tur)
        {
            ViewData["Title"] = "Sigorta ve Kasko Poliçe Takibi";
            ViewBag.Sekme = new[] { "policeler", "taksit", "araclar", "yenileme", "yeni", "ozet" }.Contains(sekme) ? sekme : "policeler"; ViewBag.Plaka = plaka ?? ""; ViewBag.Id = id?.ToString() ?? ""; ViewBag.Sirket = sirket ?? ""; ViewBag.Tur = tur ?? ""; ViewBag.Ben = Ad();
            return View("Index");
        }
        [HttpGet("Veri")]
        public IActionResult Veri() => Json(new
        {
            success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(), yenilemeGun = SigortaOrnek.YenilemeGun,
            turler = SigortaOrnek.Turler, sirketler = SigortaOrnek.Sirketler, sigortacilar = SigortaOrnek.Sigortacilar, acenteler = SigortaOrnek.Acenteler, odemeYontemleri = SigortaOrnek.OdemeYontemleri, hasarDurumlari = SigortaOrnek.HasarDurumlari, sorumlular = SigortaOrnek.Sorumlular,
            araclar = AracBakimOrnek.Araclar.Select(a => new { a.Plaka, arac = $"{a.Marka} {a.Model}", a.Yil, a.Sirket, a.Zimmet, a.Tip }).ToList(),
            policeler = SigortaOrnek.Policeler.Select(SigortaOrnek.Json).ToList(), ozet = SigortaOrnek.Ozet()
        });
        [HttpGet("Police")] public IActionResult Police(int id) { var p = SigortaOrnek.Bul(id); return p == null ? Json(new { success = false, message = "Poliçe bulunamadı." }) : Json(new { success = true, police = SigortaOrnek.Json(p) }); }

        static byte[] Oku(IFormFile f) { if (f == null || f.Length == 0) return null; using var ms = new MemoryStream(); f.CopyTo(ms); return ms.ToArray(); }
        [HttpPost("Kaydet")] public IActionResult Kaydet([FromForm] SgPoliceIstek i, IFormFile dosya) { var r = SigortaOrnek.Kaydet(i, dosya?.FileName, dosya?.ContentType, Oku(dosya), Ad()); return Json(new { success = r.ok, message = r.mesaj, police = r.p == null ? null : SigortaOrnek.Json(r.p) }); }
        [HttpPost("TaksitOde")] public IActionResult TaksitOde([FromBody] SgTaksitIstek i) { var r = SigortaOrnek.TaksitOde(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, police = r.p == null ? null : SigortaOrnek.Json(r.p) }); }
        [HttpPost("Hasar")] public IActionResult Hasar([FromBody] SgHasarIstek i) { var r = SigortaOrnek.HasarEkle(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, police = r.p == null ? null : SigortaOrnek.Json(r.p) }); }
        [HttpPost("DosyaYukle")] public IActionResult DosyaYukle(int id, string tur, IFormFile dosya) { var r = SigortaOrnek.DosyaEkle(id, tur, dosya?.FileName, dosya?.ContentType, Oku(dosya), Ad()); return Json(new { success = r.ok, message = r.mesaj, police = r.p == null ? null : SigortaOrnek.Json(r.p) }); }
        [HttpPost("Yenile")] public IActionResult Yenile([FromBody] SgYenileIstek i) { var r = SigortaOrnek.Yenile(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, police = r.p == null ? null : SigortaOrnek.Json(r.p) }); }
        public class IptalIstek { public int Id { get; set; } public string Neden { get; set; } }
        [HttpPost("Iptal")] public IActionResult Iptal([FromBody] IptalIstek i) { var r = SigortaOrnek.Iptal(i?.Id ?? 0, i?.Neden, Ad()); return Json(new { success = r.ok, message = r.mesaj, police = r.p == null ? null : SigortaOrnek.Json(r.p) }); }

        /// <summary>Yüklü dosya (poliçe / dekont / zeyil) ya da örnek poliçe için özet PDF.</summary>
        [HttpGet("Dosya")]
        public IActionResult Dosya(int id, int? no)
        {
            var p = SigortaOrnek.Bul(id); if (p == null) return NotFound("Poliçe bulunamadı.");
            var d = no.HasValue ? p.Dosyalar.FirstOrDefault(x => x.No == no.Value) : p.Dosyalar.FirstOrDefault(x => x.Tur == "Poliçe");
            if (d?.Veri != null) return File(d.Veri, d.Tip ?? "application/octet-stream", d.Dosya);
            return File(DisaAktar.Pdf(SigortaOrnek.PoliceOzeti(p, Ad())), "application/pdf", d?.Dosya ?? $"{p.No}.pdf");
        }
        [HttpGet("Pdf")] public IActionResult Pdf(int id) { var p = SigortaOrnek.Bul(id); if (p == null) return NotFound("Poliçe bulunamadı."); return File(DisaAktar.Pdf(SigortaOrnek.PoliceOzeti(p, Ad())), "application/pdf", $"police-ozeti-{p.No}.pdf"); }
        [HttpGet("Excel")]
        public IActionResult Excel()
        {
            var l = SigortaOrnek.Policeler; var d = new DaBelge { Baslik = "Sigorta ve Kasko Poliçeleri", AltBaslik = $"{l.Count} poliçe · {DateTime.Today:dd.MM.yyyy}", Sirket = "Uras Grup", Hazirlayan = Ad() };
            var bl = new DaBlok { Baslik = "Poliçeler", Kolonlar = { "Poliçe No", "Tür", "Şirket", "Plaka", "Konu", "Sigortacı", "Acente", "Başlangıç", "Bitiş", "Kalan Gün", "Prim (₺)", "Taksit", "Ödeme", "Ödenen (₺)", "Kalan (₺)", "Geciken Taksit", "Durum", "Sorumlu" }, Sag = { false, false, false, false, false, false, false, false, false, true, true, true, false, true, true, true, false, false } };
            foreach (var p in l) bl.Satirlar.Add(new[] { p.No, p.Tur, p.Sirket, p.Plaka, p.Konu, p.Sigortaci, p.Acente, p.Baslangic.ToString("dd.MM.yyyy"), p.Bitis.ToString("dd.MM.yyyy"), p.KalanGun.ToString(), p.Prim.ToString("N2", Tr), p.TaksitSayisi.ToString(), p.OdemeYontemi, p.Odenen.ToString("N2", Tr), p.Kalan.ToString("N2", Tr), p.GecikenTaksit.ToString(), p.HesapDurum, p.Sorumlu }); d.Bloklar.Add(bl);
            var tk = new DaBlok { Baslik = "Taksit Planı", Kolonlar = { "Poliçe No", "Tür", "Şirket", "Taksit", "Vade", "Tutar (₺)", "Durum", "Ödeme Tarihi", "Yöntem", "Dekont" }, Sag = { false, false, false, false, false, true, false, false, false, false } };
            foreach (var p in l.Where(p => p.Durum != "Yenilendi" && p.Durum != "İptal")) foreach (var t in p.Taksitler) tk.Satirlar.Add(new[] { p.No, p.Tur, p.Sirket, $"{t.No}/{p.TaksitSayisi}", t.Vade.ToString("dd.MM.yyyy"), t.Tutar.ToString("N2", Tr), t.Durum, t.OdemeTarihi?.ToString("dd.MM.yyyy") ?? "", t.Yontem, t.Dekont }); d.Bloklar.Add(tk);
            return File(DisaAktar.Excel(d), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"sigorta-policeleri-{DateTime.Today:yyyyMMdd}.xlsx");
        }
    }
}
