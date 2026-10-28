// ARAÇ BAKIM VE ONARIM TAKİBİ (/AracBakim; İK ve İdari İşler › İdari İşler › Araç ve Seyahat · Yönetim › Şirket Dökümanları › Araç, Taşınmaz) — Data/AracBakimOrnek.cs (+ SigortaOrnek araç poliçeleri)
// Uçlar: Veri, Kayit, Kaydet (JSON), Durum, DosyaYukle (multipart), Arac (km / zimmet güncelle), Dosya (yüklü dosya ya da servis formu PDF), Pdf (servis formu), AracPdf (araç dosyası), Excel.
// Ekran: Views/AracBakim/Index.cshtml · betik: wwwroot/js/arac-bakim.js
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
    [Route("AracBakim")]
    public class AracBakimController : Controller
    {
        private string Ad() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");

        /// <param name="sekme">kayitlar | araclar | takvim | maliyet | yeni | gunluk</param><param name="plaka">ön süzgeç</param><param name="id">açılışta çekmecede gösterilecek kayıt</param>
        [HttpGet("")]
        public IActionResult Index(string sekme, string plaka, int? id)
        {
            ViewData["Title"] = "Araç Bakım ve Onarım Takibi";
            ViewBag.Sekme = new[] { "kayitlar", "araclar", "takvim", "maliyet", "yeni", "gunluk" }.Contains(sekme) ? sekme : "kayitlar"; ViewBag.Plaka = plaka ?? ""; ViewBag.Id = id?.ToString() ?? ""; ViewBag.Ben = Ad();
            return View("Index");
        }
        [HttpGet("Veri")]
        public IActionResult Veri() => Json(new
        {
            success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(),
            turler = AracBakimOrnek.Turler, durumlar = AracBakimOrnek.Durumlar, servisTurleri = AracBakimOrnek.ServisTurleri, odemeler = AracBakimOrnek.Odemeler, kalemTurleri = AracBakimOrnek.KalemTurleri, sorumlular = AracBakimOrnek.Sorumlular, sirketler = AracBakimOrnek.Sirketler,
            servisler = AracBakimOrnek.Servisler.Select(s => new { ad = s.Ad, tur = s.Tur, sehir = s.Sehir }).ToList(),
            araclar = AracBakimOrnek.Araclar.Select(AracBakimOrnek.AracJson).ToList(), kayitlar = AracBakimOrnek.Bakimlar.Select(AracBakimOrnek.Json).ToList(), ozet = AracBakimOrnek.Ozet()
        });
        [HttpGet("Kayit")] public IActionResult Kayit(int id) { var b = AracBakimOrnek.Bul(id); return b == null ? Json(new { success = false, message = "Kayıt bulunamadı." }) : Json(new { success = true, kayit = AracBakimOrnek.Json(b) }); }
        [HttpPost("Kaydet")] public IActionResult Kaydet([FromBody] ArBakimIstek i) { var r = AracBakimOrnek.Kaydet(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, kayit = r.b == null ? null : AracBakimOrnek.Json(r.b) }); }
        [HttpPost("Durum")] public IActionResult Durum([FromBody] ArDurumIstek i) { var r = AracBakimOrnek.DurumDegistir(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, kayit = r.b == null ? null : AracBakimOrnek.Json(r.b) }); }
        [HttpPost("Arac")] public IActionResult Arac([FromBody] ArAracIstek i) { var r = AracBakimOrnek.AracGuncelle(i, Ad()); return Json(new { success = r.ok, message = r.mesaj }); }
        [HttpPost("DosyaYukle")]
        public IActionResult DosyaYukle(int id, string tur, IFormFile dosya)
        {
            byte[] veri = null; if (dosya != null && dosya.Length > 0) { using var ms = new MemoryStream(); dosya.CopyTo(ms); veri = ms.ToArray(); }
            var r = AracBakimOrnek.DosyaEkle(id, tur, dosya?.FileName, dosya?.ContentType, veri, Ad()); return Json(new { success = r.ok, message = r.mesaj, kayit = r.b == null ? null : AracBakimOrnek.Json(r.b) });
        }
        DaBelge ServisFormu(ArBakim b)
        {
            var a = AracBakimOrnek.AracBul(b.Plaka); var d = new DaBelge { Baslik = $"Servis / Bakım Formu · {b.No}", AltBaslik = $"{b.Plaka} · {a?.Marka} {a?.Model} ({a?.Yil}) · {a?.Sirket} · zimmet {a?.Zimmet}", Sirket = a?.Sirket ?? "Uras Grup", Hazirlayan = Ad(), Yatay = false };
            d.Ozet.Add(("Tür", b.Tur)); d.Ozet.Add(("Servis", $"{b.Servis} ({b.ServisTuru})")); d.Ozet.Add(("Tarih · Km", $"{b.Tarih:dd.MM.yyyy} · {b.Km:N0} km")); d.Ozet.Add(("Durum", b.Durum)); d.Ozet.Add(("Toplam (KDV dahil)", b.Toplam.ToString("N2", Tr) + " ₺")); d.Ozet.Add(("Ödeme", b.Odeme + (b.FaturaNo != "" ? " · " + b.FaturaNo : "")));
            var k = new DaBlok { Baslik = "Kalemler", Kolonlar = { "No", "Kalem", "Tür", "Miktar", "Birim Fiyat (₺)", "Tutar (₺)" }, Sag = { false, false, false, true, true, true } }; foreach (var x in b.Kalemler) k.Satirlar.Add(new[] { x.Sira.ToString(), x.Ad, x.Tur, x.Miktar.ToString("0.##", Tr), x.BirimFiyat.ToString("N2", Tr), x.Tutar.ToString("N2", Tr) }); k.Toplam = new[] { "", "Ara toplam / KDV %20 / Toplam", "", "", $"{b.Tutar.ToString("N2", Tr)} / {b.Kdv.ToString("N2", Tr)}", b.Toplam.ToString("N2", Tr) };
            k.Not = (b.Sikayet != "" ? "Şikâyet: " + b.Sikayet + " · " : "") + (b.Aciklama != "" ? "Yapılan işlem: " + b.Aciklama + " · " : "") + (b.SonrakiBakimKm != null ? $"Sonraki bakım {b.SonrakiBakimKm:N0} km / {b.SonrakiBakimTarihi:dd.MM.yyyy} · " : "") + (b.GarantiAy > 0 ? $"Garanti {b.GarantiAy} ay · " : "") + (b.HasarDosyaNo != "" ? "Hasar dosyası " + b.HasarDosyaNo + " · " : "") + "Sorumlu: " + b.Sorumlu; d.Bloklar.Add(k);
            var g = new DaBlok { Baslik = "Günlük", Kolonlar = { "Zaman", "Kullanıcı", "İşlem", "Açıklama" } }; foreach (var x in b.Hareketler.OrderByDescending(x => x.Zaman)) g.Satirlar.Add(new[] { x.Zaman.ToString("dd.MM.yyyy HH:mm"), x.Kullanici, x.Islem, x.Aciklama }); d.Bloklar.Add(g);
            return d;
        }
        [HttpGet("Dosya")]
        public IActionResult Dosya(int id, int? no)
        {
            var b = AracBakimOrnek.Bul(id); if (b == null) return NotFound("Kayıt bulunamadı."); var d = no.HasValue ? b.Dosyalar.FirstOrDefault(x => x.No == no.Value) : b.Dosyalar.FirstOrDefault();
            if (d?.Veri != null) return File(d.Veri, d.Tip ?? "application/octet-stream", d.Dosya);
            return File(DisaAktar.Pdf(ServisFormu(b)), "application/pdf", d?.Dosya ?? $"{b.No}.pdf");
        }
        [HttpGet("Pdf")] public IActionResult Pdf(int id) { var b = AracBakimOrnek.Bul(id); if (b == null) return NotFound("Kayıt bulunamadı."); return File(DisaAktar.Pdf(ServisFormu(b)), "application/pdf", $"servis-formu-{b.No}.pdf"); }
        [HttpGet("AracPdf")]
        public IActionResult AracPdf(string plaka)
        {
            var a = AracBakimOrnek.AracBul(plaka ?? ""); if (a == null) return NotFound("Araç bulunamadı."); var l = AracBakimOrnek.Bakimlar.Where(b => b.Plaka == a.Plaka && b.Durum != "İptal").OrderByDescending(b => b.Tarih).ToList(); dynamic o = AracBakimOrnek.AracOzet(a);
            var d = new DaBelge { Baslik = $"Araç Dosyası · {a.Plaka}", AltBaslik = $"{a.Marka} {a.Model} ({a.Yil}) · {a.Sirket} · zimmet {a.Zimmet} · şasi {a.Sasi}", Sirket = a.Sirket, Hazirlayan = Ad(), Yatay = true };
            d.Ozet.Add(("Km", a.Km.ToString("N0", Tr))); d.Ozet.Add(("Sonraki Bakım", o.sonrakiBakimKm != null ? $"{((int)o.sonrakiBakimKm).ToString("N0", Tr)} km / {o.sonrakiBakimTarihi}" : "—")); d.Ozet.Add(("Bakım Durumu", (string)o.bakimDurum)); d.Ozet.Add(("Muayene", a.MuayeneBitis.ToString("dd.MM.yyyy"))); d.Ozet.Add(("Yıl Maliyeti", ((decimal)o.yilMaliyet).ToString("N2", Tr) + " ₺")); d.Ozet.Add(("Toplam Maliyet", ((decimal)o.toplamMaliyet).ToString("N2", Tr) + " ₺"));
            var bl = new DaBlok { Baslik = $"Bakım ve Onarım Geçmişi · {l.Count} kayıt", Kolonlar = { "No", "Tarih", "Tür", "Servis", "Km", "Şikâyet / İşlem", "Parça (₺)", "İşçilik (₺)", "Toplam (₺)", "Ödeme", "Fatura", "Durum" }, Sag = { false, false, false, false, true, false, true, true, true, false, false, false } };
            foreach (var b in l) bl.Satirlar.Add(new[] { b.No, b.Tarih.ToString("dd.MM.yyyy"), b.Tur, b.Servis, b.Km.ToString("N0", Tr), (b.Sikayet != "" ? b.Sikayet + " → " : "") + b.Aciklama, b.ParcaTutar.ToString("N2", Tr), b.IscilikTutar.ToString("N2", Tr), b.Toplam.ToString("N2", Tr), b.Odeme, b.FaturaNo, b.Durum });
            bl.Toplam = new[] { "", "", "", "", "", "Toplam", l.Sum(b => b.ParcaTutar).ToString("N2", Tr), l.Sum(b => b.IscilikTutar).ToString("N2", Tr), l.Sum(b => b.Toplam).ToString("N2", Tr), "", "", "" }; d.Bloklar.Add(bl);
            return File(DisaAktar.Pdf(d), "application/pdf", $"arac-dosyasi-{a.Plaka.Replace(" ", "")}.pdf");
        }
        [HttpGet("Excel")]
        public IActionResult Excel()
        {
            var l = AracBakimOrnek.Bakimlar; var d = new DaBelge { Baslik = "Araç Bakım ve Onarım Kayıtları", AltBaslik = $"{AracBakimOrnek.Araclar.Count} araç · {l.Count} kayıt · {DateTime.Today:dd.MM.yyyy}", Sirket = "Uras Grup", Hazirlayan = Ad() };
            var bl = new DaBlok { Baslik = "Kayıtlar", Kolonlar = { "No", "Plaka", "Araç", "Şirket", "Tarih", "Tür", "Servis", "Servis Türü", "Km", "Şikâyet", "İşlem", "Parça (₺)", "İşçilik (₺)", "KDV (₺)", "Toplam (₺)", "Ödeme", "Fatura", "Durum", "Sonraki Bakım Km", "Sonraki Bakım", "Garanti (Ay)", "Hasar Dosyası", "Sorumlu" }, Sag = { false, false, false, false, false, false, false, false, true, false, false, true, true, true, true, false, false, false, true, false, true, false, false } };
            foreach (var b in l) { var a = AracBakimOrnek.AracBul(b.Plaka); bl.Satirlar.Add(new[] { b.No, b.Plaka, $"{a?.Marka} {a?.Model}", a?.Sirket, b.Tarih.ToString("dd.MM.yyyy"), b.Tur, b.Servis, b.ServisTuru, b.Km.ToString("N0", Tr), b.Sikayet, b.Aciklama, b.ParcaTutar.ToString("N2", Tr), b.IscilikTutar.ToString("N2", Tr), b.Kdv.ToString("N2", Tr), b.Toplam.ToString("N2", Tr), b.Odeme, b.FaturaNo, b.Durum, b.SonrakiBakimKm?.ToString("N0", Tr) ?? "", b.SonrakiBakimTarihi?.ToString("dd.MM.yyyy") ?? "", b.GarantiAy.ToString(), b.HasarDosyaNo, b.Sorumlu }); } d.Bloklar.Add(bl);
            var ar = new DaBlok { Baslik = "Araçlar", Kolonlar = { "Plaka", "Marka", "Model", "Yıl", "Şirket", "Tip", "Yakıt", "Km", "Zimmet", "Lokasyon", "Durum", "Muayene Bitiş", "Sonraki Bakım Km", "Sonraki Bakım", "Bakım Durumu", "Yıl Maliyeti (₺)", "Toplam Maliyet (₺)" }, Sag = { false, false, false, true, false, false, false, true, false, false, false, false, true, false, false, true, true } };
            foreach (var a in AracBakimOrnek.Araclar) { dynamic o = AracBakimOrnek.AracOzet(a); ar.Satirlar.Add(new[] { a.Plaka, a.Marka, a.Model, a.Yil.ToString(), a.Sirket, a.Tip, a.Yakit, a.Km.ToString("N0", Tr), a.Zimmet, a.Lokasyon, a.Durum, a.MuayeneBitis.ToString("dd.MM.yyyy"), o.sonrakiBakimKm != null ? ((int)o.sonrakiBakimKm).ToString("N0", Tr) : "", (string)(o.sonrakiBakimTarihi ?? ""), (string)o.bakimDurum, ((decimal)o.yilMaliyet).ToString("N2", Tr), ((decimal)o.toplamMaliyet).ToString("N2", Tr) }); } d.Bloklar.Add(ar);
            return File(DisaAktar.Excel(d), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"arac-bakim-{DateTime.Today:yyyyMMdd}.xlsx");
        }
    }
}
