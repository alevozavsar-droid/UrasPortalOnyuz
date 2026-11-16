using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Müşteri Fiyat ve Kâr Oranları — YALNIZ Yönetim › Yönetim Özeti › Fiyat ve Kâr Yönetimi. Üç sekme:
    ///   Yetkili Satıcı Fiyatları : üretici (Uras Kimya) → yetkili satıcı (Selvi Kimya …) kâr oranı; genel / ürün grubu / kalem bazında (en özel kazanır)
    ///   Müşteri Fiyatları        : yetkili satıcı → müşteri carisi kâr oranı; aynı kademe; satıcı alış fiyatı üzerinden müşteri satış fiyatı ve uçtan uca marj
    ///   Özet ve Günlük           : satıcı × grup ve müşteri × grup oran matrisleri, değişiklik günlüğü
    /// 13 Kas 2026 — salt okunur dört sekme (hesap tarayıcıda, wwwroot/js/fiyat-yonetimi.js): Müşteri Fiyat Ortalamaları (tüm müşterilerin fiyat listesi ortalamaları toplu),
    ///   Kâr Oranı Sorgusu (% eşiğinin altındaki ürün × müşteri fiyatları), Ürün Fiyat Karşılaştırması (aynı ürünün müşteriler arası fiyat farkı),
    ///   Satış Uyarıları (şirket + minimum kâr oranı → altında kalan fatura kalemleri; satış verisi /SatisRaporu/Veri).
    /// Kaydetme yalnız Yönetici yetkisiyle (OrnekVeri.Kullanicilar Yetki = "A"); diğer roller salt okunur. Veri ve kurallar: Data/FiyatOrnek.cs.
    /// </summary>
    [Authorize]
    public class FiyatYonetimiController : Controller
    {
        private string Kod() => User?.Identity?.Name ?? "";

        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Müşteri Fiyat ve Kâr Oranları";
            ViewBag.Sekme = new[] { "musteri", "ozet", "ortalama", "sorgu", "urunk", "uyari", "gider" }.Contains(sekme) ? sekme : "satici";   // 13 Kas 2026: dört salt okunur analiz sekmesi (ortalama, sorgu, urunk, uyari)
            ViewBag.Yonetici = FiyatOrnek.Yonetici(Kod());
            ViewBag.Ben = OrnekVeri.KullaniciAdi(Kod());
            return View();
        }

        [HttpGet]
        public IActionResult Veri()
        {
            lock (FiyatOrnek.Kilit)
            {
                return Json(new
                {
                    success = true,
                    ben = new { ad = OrnekVeri.KullaniciAdi(Kod()), kod = Kod(), yonetici = FiyatOrnek.Yonetici(Kod()) },
                    uretici = FiyatOrnek.Uretici,
                    // 13 Kas 2026: zincir dinamik (üretici seçilir) + maliyet bileşenleri (toplu ambalaj maliyeti ve risk payı; kalem değerleri ürün satırında)
                    ureticiler = FiyatOrnek.Ureticiler,
                    maliyet = new { genelAmbalaj = FiyatOrnek.GenelAmbalaj, genelAmbalajElle = FiyatOrnek.GenelAmbalajElle, genelRisk = FiyatOrnek.GenelRisk, ambalajTipi = FiyatOrnek.AmbalajTipi, ambalajTipleri = FiyatOrnek.AmbalajTipleri() },
                    paraBirimi = "USD",
                    // Kur parametreleri: piyasa kuru + döviz kâr marjı → fiyatlandırma kuru (TL karşılığı) ve fiyatlandırma paritesi (EUR karşılığı)
                    kur = new { FiyatOrnek.Kur.UsdTl, FiyatOrnek.Kur.EurTl, FiyatOrnek.Kur.Marj, tarih = FiyatOrnek.Kur.Tarih.ToString("dd.MM.yyyy"), FiyatOrnek.Kur.Kaynak, FiyatOrnek.Kur.Guncelleyen, FiyatOrnek.Kur.FiyatUsdTl, FiyatOrnek.Kur.Parite, FiyatOrnek.Kur.FiyatParite },
                    gruplar = FiyatOrnek.Gruplar.Select(g => new { anahtar = g.Anahtar, ad = g.Ad }),
                    urunler = FiyatOrnek.Urunler.Select(u => new { u.Kod, u.Ad, u.AnaGrup, u.Grup, grupAnahtari = u.GrupAnahtari, u.UrunSinifi, u.UrunGrubu, u.Seri, hiyerarsi = string.Join(" › ", new[] { u.UrunSinifi, u.UrunGrubu, u.Seri }.Where(x => !string.IsNullOrEmpty(x))), u.Birim, u.ParaBirimi, u.Maliyet, u.AmbalajMaliyeti, u.RiskPayi, u.Durum }),
                    saticilar = FiyatOrnek.ZincirSaticilar.Select(s => new { s.Ad, s.Sirket, s.CariKodu, s.Bolge, s.Not, cari = CariDto(CariKartOrnek.KartBul(s.CariKodu)), oranlar = s.Oranlar.Select(OranDto), zincir = s.UreticiOranlari.Where(x => x.Value.Count > 0).Select(x => new { uretici = x.Key, oranlar = x.Value.Select(OranDto) }), musteriSayisi = FiyatOrnek.MusteriTanimlari.Count(m => m.Satici == s.Ad) }),
                    musteriler = FiyatOrnek.Musteriler().Select(CariDto),
                    musteriTanimlari = FiyatOrnek.MusteriTanimlari.Select(m => new { m.Satici, m.CariKodu, risk = m.RiskPayi, urunRisk = m.UrunRisk.Select(x => new { kod = x.Key, oran = x.Value }), oranlar = m.Oranlar.Select(OranDto), zaman = m.Zaman.ToString("dd.MM.yyyy HH:mm"), m.Kullanici }),
                    audit = FiyatOrnek.Audit.OrderByDescending(a => a.Zaman).Take(200).Select(a => new { zaman = a.Zaman.ToString("dd.MM.yyyy HH:mm"), a.Kullanici, a.Katman, a.Hedef, a.Alan, a.Eski, a.Yeni })
                });
            }
        }

        [HttpPost]
        public IActionResult SaticiKaydet([FromBody] FyKayit g)
        {
            var h = FiyatOrnek.SaticiKaydet(g, Kod(), out var n);
            return Json(new { success = h == null, message = h ?? (n == 0 ? "Değişiklik yok." : $"{(string.IsNullOrWhiteSpace(g.Uretici) ? FiyatOrnek.Uretici : g.Uretici)} → {g.Satici}: {n} oran tanımı kaydedildi."), degisen = n });
        }

        /// <summary>Genel Gider Payı sekmesi: seçili üretici şirketin sabit / genel gider kalemleri (Bütçe Raporu) ve aylık üretim rakamları.</summary>
        [HttpGet]
        public IActionResult GenelGider(string uretici, int yil) => Json(FiyatOrnek.GenelGiderPaketi(uretici, yil));

        [HttpPost]
        public IActionResult UretimKaydet([FromBody] FyUretimKayit g)
        {
            var h = FiyatOrnek.UretimKaydet(g, Kod(), out var n);
            return Json(new { success = h == null, message = h ?? (n == 0 ? "Değişiklik yok." : $"Üretim rakamları kaydedildi ({n} ay)."), degisen = n });
        }

        [HttpPost]
        public IActionResult MaliyetKaydet([FromBody] FyMaliyetKayit g)
        {
            var h = FiyatOrnek.MaliyetKaydet(g, Kod(), out var n);
            return Json(new { success = h == null, message = h ?? (n == 0 ? "Değişiklik yok." : $"Maliyet bileşenleri kaydedildi ({n} alan)."), degisen = n });
        }

        [HttpPost]
        public IActionResult KurKaydet([FromBody] FyKurGiris g)
        {
            var h = FiyatOrnek.KurKaydet(g, Kod(), out var n);
            return Json(new { success = h == null, message = h ?? (n == 0 ? "Değişiklik yok." : $"Kur parametreleri kaydedildi ({n} alan). Fiyatlandırma kuru USD/TL {FiyatOrnek.Kur.FiyatUsdTl:0.0000}, EUR paritesi {FiyatOrnek.Kur.FiyatParite:0.0000}."), degisen = n });
        }

        [HttpPost]
        public IActionResult MusteriKaydet([FromBody] FyKayit g)
        {
            var h = FiyatOrnek.MusteriKaydet(g, Kod(), out var n);
            return Json(new { success = h == null, message = h ?? (n == 0 ? "Değişiklik yok." : $"{g.CariKodu}: {n} değer kaydedildi (kâr oranı / risk payı)."), degisen = n });
        }

        private static object OranDto(FyOran o) => new { o.Kapsam, o.Anahtar, o.Oran, zaman = o.Zaman.ToString("dd.MM.yyyy HH:mm"), o.Kullanici };
        private static string Trh(DateTime? d) => d?.ToString("dd.MM.yyyy") ?? "";
        /// <summary>CARİ_MASTER özet alanları (Müşteri Cari Kartları ekranıyla aynı adlar).</summary>
        private static object CariDto(CariKart k) => k == null ? null : new
        {
            k.Kod, k.KisaAd, k.KisaltmaliUnvan, k.ResmiUnvan, k.VergiNo, k.VergiDairesi, k.KartTipi, k.Bolge, k.Ulke, k.Sehir, k.Sinif, k.IliskiDurumu, k.Rol, k.EsCariKodu, k.Yetkili, k.Telefon, k.Eposta, k.VadeSarti, k.Durum, durumKaynagi = k.DurumKaynagi, k.Sirket, k.Not, anaFaaliyet = k.AnaFaaliyet,
            temsilciler = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()),
            faaliyetGruplari = k.Faaliyetler.Select(f => f.AnaGrup + "|" + f.Grup).Distinct()
        };
    }
}
