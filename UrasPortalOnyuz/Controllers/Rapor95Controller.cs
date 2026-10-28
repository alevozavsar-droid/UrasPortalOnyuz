// MUHATAPLAR İÇİN ÖZEL FİYATLAR (/Rapor95; Satış ve Operasyon › Müşteri ve CRM Yönetimi › Fiyat ve Müşteri Koşulları)
// Elle yazılmış sürüm (üretilmiş Uretilen/Controllers/Rapor95Controller.cs URETILEN_KAPALI ile devre dışı). Veri: Data/OzelFiyatOrnek.cs (+ FiyatOrnek: ürünler, satıcı alış fiyatı, kur; CariKartOrnek: müşteri kartları).
// Amaç: her müşteri için TÜM ürünlerde fiyat; daha önce fiyat verilmemiş / hiç alınmamış kalemlere Min Fiyat Listesi + % ile toplu fiyat üretilir.
// Uçlar: Veri (satıcı + müşteri + tüm ürün satırları + özet), Toplu (önizleme), Kaydet, Kopyala, Urun (ürün bazında tüm müşteriler), Gunluk, Pdf / Excel (müşteriye gönderilecek fiyat listesi).
// Ekran: Views/Rapor95/Index.cshtml · betik: wwwroot/js/ozel-fiyat.js
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
    [Route("Rapor95")]
    public class Rapor95Controller : Controller
    {
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        private string Kod() => User?.Identity?.Name ?? "";
        private string Ad() => OrnekVeri.KullaniciAdi(Kod());
        private bool Yonetici => FiyatOrnek.Yonetici(Kod());

        /// <param name="satici">satıcı şirket (FySatici.Ad: Selvi Kimya …)</param><param name="kod">müşteri cari kodu</param><param name="sekme">musteri | urun | gunluk</param><param name="urun">Ürün Bazında sekmesinde açılacak ürün kodu</param>
        [HttpGet("")]
        public IActionResult Index(string satici, string kod, string sekme, string urun)
        {
            ViewData["Title"] = "Muhataplar İçin Özel Fiyatlar";
            ViewBag.Satici = satici ?? ""; ViewBag.Kod = kod ?? ""; ViewBag.Urun = urun ?? ""; ViewBag.Sekme = new[] { "musteri", "urun", "gunluk" }.Contains(sekme) ? sekme : "musteri";
            ViewBag.Yonetici = Yonetici; ViewBag.Ben = Ad();
            return View("Index");
        }

        static object MusteriDto(FySatici s, CariKart k) => new { kod = k.Kod, ad = k.KisaAd, sirket = k.Sirket, sinif = k.Sinif, bolge = k.Bolge, iliski = k.IliskiDurumu, fiyatli = OzelFiyatOrnek.Fiyatlar(s.Ad, k.Kod).Count, temsilci = Temsilci(k) };
        static string Temsilci(CariKart k) => k.Faaliyetler.FirstOrDefault(x => x.AnaFaaliyet)?.SatisTemsilcisi ?? k.Faaliyetler.FirstOrDefault()?.SatisTemsilcisi ?? "";
        static object CariDto(CariKart k) => new { kod = k.Kod, kisaAd = k.KisaAd, resmiUnvan = k.ResmiUnvan, sirket = k.Sirket, sinif = k.Sinif, bolge = k.Bolge, sehir = k.Sehir, ulke = k.Ulke, iliski = k.IliskiDurumu, vade = k.VadeSarti, yetkili = k.Yetkili, telefon = k.Telefon, eposta = k.Eposta, temsilci = Temsilci(k), durum = k.Durum, faaliyetler = k.Faaliyetler.Select(f => f.AnaGrup + " / " + f.Grup + (f.AnaFaaliyet ? " (ana)" : "")).Distinct().ToList(), link = "/Rapor120?searchKeyword=" + Uri.EscapeDataString(k.Kod) };

        /// <summary>Satıcı, müşteriler, seçili müşteri için tüm ürün satırları (min / liste fiyatı, son satış, özel fiyat), özet ve son günlük.</summary>
        [HttpGet("Veri")]
        public IActionResult Veri(string satici, string kod)
        {
            var s = OzelFiyatOrnek.SaticiBul(satici); var musteriler = OzelFiyatOrnek.Musteriler(s);
            var kart = (!string.IsNullOrEmpty(kod) ? musteriler.FirstOrDefault(k => string.Equals(k.Kod, kod, StringComparison.OrdinalIgnoreCase)) ?? CariKartOrnek.KartBul(kod) : null)
                       ?? musteriler.FirstOrDefault(k => OzelFiyatOrnek.Fiyatlar(s.Ad, k.Kod).Count > 0) ?? musteriler.FirstOrDefault();
            var liste = kart == null ? new List<OzelFiyatOrnek.Satir>() : OzelFiyatOrnek.Liste(s, kart.Kod); var kur = FiyatOrnek.Kur;
            return Json(new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(), yonetici = Yonetici,
                satici = new { s.Ad, s.Sirket, s.Bolge, s.CariKodu }, saticilar = OzelFiyatOrnek.Saticilar.Select(x => new { x.Ad, x.Sirket, x.Bolge, musteri = OzelFiyatOrnek.Musteriler(x).Count }).ToList(),
                musteriler = musteriler.Select(k => MusteriDto(s, k)).ToList(), cari = kart == null ? null : CariDto(kart),
                gruplar = FiyatOrnek.Gruplar.Select(g => new { anahtar = g.Anahtar, ad = g.Ad }).ToList(), urunler = FiyatOrnek.Urunler.Where(u => u.Durum == "Aktif").Select(u => OzelFiyatOrnek.UrunJson(s, u)).ToList(),
                kur = new { kur.UsdTl, kur.EurTl, kur.Marj, kur.FiyatUsdTl, kur.FiyatParite, tarih = kur.Tarih.ToString("dd.MM.yyyy"), kur.Kaynak }, minMarj = OzelFiyatOrnek.MinMarj, listeMarj = OzelFiyatOrnek.ListeMarj, yuvarlamalar = OzelFiyatOrnek.Yuvarlamalar, kaynaklar = OzelFiyatOrnek.Kaynaklar,
                satirlar = liste.Select(OzelFiyatOrnek.SatirJson).ToList(), ozet = OzelFiyatOrnek.Ozet(liste),
                gunluk = kart == null ? new List<object>() : OzelFiyatOrnek.Gunluk(s.Ad, kart.Kod).Take(60).Select(OzelFiyatOrnek.GunlukJson).ToList()
            });
        }

        /// <summary>Toplu fiyat önizlemesi: kapsamdaki kalemlere taban (Min / Liste / Son Satış) × (1 + %) — kaydetmez; ekran Yeni Fiyat sütununa yazar.</summary>
        [HttpPost("Toplu")]
        public IActionResult Toplu([FromBody] OzelFiyatTopluIstek i) { var r = OzelFiyatOrnek.TopluHesapla(i); return Json(new { success = r.hata == null, message = r.hata ?? $"{r.satirlar.Count} kalem için fiyat hesaplandı; Kaydet ile tanımlanır.", satirlar = r.satirlar }); }

        [HttpPost("Kaydet")]
        public IActionResult Kaydet([FromBody] OzelFiyatKayitIstek i) { var r = OzelFiyatOrnek.Kaydet(i, Ad(), Yonetici); return Json(new { success = r.ok, message = r.mesaj, r.eklenen, r.guncellenen, r.silinen }); }

        [HttpPost("Kopyala")]
        public IActionResult Kopyala([FromBody] OzelFiyatKopyaIstek i) { var r = OzelFiyatOrnek.Kopyala(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, r.adet }); }

        /// <summary>Ürün bazında: bir ürünün satıcının tüm müşterilerindeki özel fiyatı, son satışı, Fiyat Yönetimi oran fiyatı.</summary>
        [HttpGet("Urun")]
        public IActionResult Urun(string satici, string kod)
        {
            var s = OzelFiyatOrnek.SaticiBul(satici); var u = FiyatOrnek.UrunBul(kod ?? "") ?? FiyatOrnek.Urunler.FirstOrDefault(); if (u == null) return Json(new { success = false, message = "Ürün bulunamadı." });
            return Json(new { success = true, urun = OzelFiyatOrnek.UrunJson(s, u), musteriler = OzelFiyatOrnek.UrunBazinda(s, u.Kod) });
        }

        [HttpGet("Gunluk")]
        public IActionResult Gunluk(string satici, string kod) { var s = OzelFiyatOrnek.SaticiBul(satici); return Json(new { success = true, gunluk = OzelFiyatOrnek.Gunluk(s?.Ad, kod).Take(400).Select(OzelFiyatOrnek.GunlukJson).ToList() }); }

        // ---------- müşteriye gönderilecek fiyat listesi (PDF / Excel): yalnız tanımlı ve geçerli özel fiyatlar, ürün grubuna göre bloklar; min / liste fiyatı ve maliyet yazılmaz ----------
        DaBelge Belge(string satici, string kod, bool tumu)
        {
            var s = OzelFiyatOrnek.SaticiBul(satici); var kart = CariKartOrnek.KartBul(kod ?? ""); if (kart == null) return null;
            var liste = OzelFiyatOrnek.Liste(s, kart.Kod).Where(r => r.Ozel != null && (tumu || r.Ozel.Gecerli(DateTime.Today))).ToList(); var kur = FiyatOrnek.Kur;
            var b = new DaBelge { Baslik = "Müşteri Özel Fiyat Listesi", AltBaslik = $"{kart.Kod} {kart.KisaAd} · {s.Ad} · {DateTime.Today:dd.MM.yyyy}", Sirket = s.Ad, Hazirlayan = Ad(), Yatay = true };
            b.Ozet.Add(("Müşteri", $"{kart.KisaAd} ({kart.ResmiUnvan})")); b.Ozet.Add(("Satıcı", s.Ad)); b.Ozet.Add(("Kalem", liste.Count.ToString())); b.Ozet.Add(("Para Birimi", "USD · ₺ karşılığı fiyatlandırma kuru " + kur.FiyatUsdTl.ToString("0.0000", Tr) + " (" + kur.Tarih.ToString("dd.MM.yyyy") + ")")); b.Ozet.Add(("Vade", kart.VadeSarti ?? "")); b.Ozet.Add(("Satış Temsilcisi", Temsilci(kart)));
            foreach (var g in liste.GroupBy(r => r.Urun.GrupAnahtari))
            {
                var blok = new DaBlok { Baslik = g.Key.Replace("|", " / ") + $" · {g.Count()} kalem", Kolonlar = { "Kod", "Ürün", "Seri", "Birim", "Fiyat ($)", "Fiyat (₺)", "Geçerlilik", "Not" }, Sag = { false, false, false, false, true, true, false, false } };
                foreach (var r in g.OrderBy(r => r.Urun.Kod)) blok.Satirlar.Add(new[] { r.Urun.Kod, r.Urun.Ad, r.Urun.Seri ?? "", r.Urun.Birim, r.Ozel.Fiyat.ToString("N2", Tr), kur.TlKarsiligi(r.Ozel.Fiyat).ToString("N2", Tr), r.Ozel.Baslangic.ToString("dd.MM.yyyy") + (r.Ozel.Bitis != null ? " – " + r.Ozel.Bitis.Value.ToString("dd.MM.yyyy") : " – süresiz"), r.Ozel.Not ?? "" });
                b.Bloklar.Add(blok);
            }
            if (liste.Count == 0) b.Bloklar.Add(new DaBlok { Baslik = "Tanımlı özel fiyat yok", Kolonlar = { "Bilgi" }, Satirlar = { new[] { "Bu müşteri için henüz özel fiyat tanımlanmadı." } } });
            b.Bloklar.Last().Not = "Fiyatlar KDV hariç, USD; ₺ karşılığı belge tarihindeki fiyatlandırma kuruyla hesaplanmıştır. Geçerlilik süresi dolan fiyatlar listede yer almaz.";
            return b;
        }
        [HttpGet("Pdf")]
        public IActionResult Pdf(string satici, string kod, bool tumu = false) { var b = Belge(satici, kod, tumu); if (b == null) return NotFound("Müşteri bulunamadı."); return File(DisaAktar.Pdf(b), "application/pdf", $"ozel-fiyat-{kod}-{DateTime.Today:yyyyMMdd}.pdf"); }
        [HttpGet("Excel")]
        public IActionResult Excel(string satici, string kod, bool tumu = false) { var b = Belge(satici, kod, tumu); if (b == null) return NotFound("Müşteri bulunamadı."); return File(DisaAktar.Excel(b), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"ozel-fiyat-{kod}-{DateTime.Today:yyyyMMdd}.xlsx"); }
    }
}
