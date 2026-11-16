using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// KONSOLİDE STOK RAPORU (13 Kas 2026) — Tedarik Zinciri › Malzeme ve Stok Yönetimi › Stok ve Envanter.
    /// Ana sekme stok grubu (Hammadde · Bitmiş Ürün · Ambalaj · Diğer Stoklar · Demirbaş · Tümü); alt sekmeler: Demirbaş Listesi (yalnız Demirbaş grubunda) · Stok Listesi · Tüketim ve Yeterlilik · Sayım ve Risk ·
    /// Aylık Stok Hareketleri (görünümler: Aylık Hareket · Stok ve Uyum) · Tüketim Verisi | Fiili Stok Takibi · Muhasebe Stok Takibi | Özet.
    /// Muhasebe stoku (R) ile fiili stok ayrı tutulur; muhasebe değerleri otomatik gelir.
    /// 16 Kas 2026: Sayım ve Sayım Onayı sekmeleri kullanıcı isteğiyle kaldırıldı; ekrandan sayım girilmez / onaylanmaz. SayimKaydet · SayimKarar uçları ve Data/StokSayimKayit.cs duruyor (ekranı yok);
    /// önceden onaylanmış sayım kaydı varsa rapor onu fiili stok olarak kullanmayı sürdürür.
    /// Kullanıcının girdiği tanımlar (Data/StokTanimKayit.cs): stok eşikleri (Tüketim ve Yeterlilik), listede olmayan ürün (Stok Listesi › Ürün Ekle; girilen miktar fiili stoktur), demirbaş kayıtları (Demirbaş Listesi).
    /// Stok verisi Data/StokRaporOrnek.cs (örnek kayıtlar; gerçek stok kaynağı bağlı değil), betik wwwroot/js/stok-raporu.js; hesap tarayıcıda.
    /// </summary>
    [Authorize]
    [Route("StokRaporu")]
    public class StokRaporuController : Controller
    {
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Konsolide Stok Raporu";
            ViewBag.Sekme = sekme ?? "stok";
            return View();
        }

        [HttpGet("Veri")]
        public IActionResult Veri() => Json(new { paket = StokRaporOrnek.Paket(), sayimlar = StokSayimKayit.Liste(), urunler = StokTanimKayit.Urunler(), esikler = StokTanimKayit.Esikler(), kullanici = Kullanici });

        /// <summary>Sayım girişi: satır başına sayılan miktar, bloke / karantina, konum, son kullanma tarihi, sayım tarihi, not → "Onay Bekliyor".</summary>
        [HttpPost("SayimKaydet")]
        public IActionResult SayimKaydet([FromBody] List<StokSayimKayit.Giris> g)
        {
            var (adet, hata) = StokSayimKayit.Kaydet(g, Kullanici);
            return Json(new { ok = hata == null && adet > 0, mesaj = hata ?? (adet > 0 ? $"{adet} sayım kaydedildi; onay bekliyor." : "Kaydedilecek sayım yok."), sayimlar = StokSayimKayit.Liste() });
        }

        /// <summary>Sayım kararı: onay · red · geriAl · sil. Yalnız onaylı sayım fiili stoku değiştirir.</summary>
        [HttpPost("SayimKarar")]
        public IActionResult SayimKarar([FromBody] StokSayimKayit.Karar g)
        {
            var (adet, hata) = StokSayimKayit.Karara(g, Kullanici);
            return Json(new { ok = hata == null, mesaj = hata ?? $"{adet} kayıt işlendi.", sayimlar = StokSayimKayit.Liste() });
        }

        object Tanimlar(bool ok, string mesaj) => new { ok, mesaj, urunler = StokTanimKayit.Urunler(), esikler = StokTanimKayit.Esikler(), sayimlar = StokSayimKayit.Liste() };

        /// <summary>Listede olmayan ürün (Sayım sekmesi) ya da demirbaş (Demirbaş Listesi) kaydı: ekle / güncelle. Sayım sırasında eklenen üründe sayılan miktar da gelirse "Onay Bekliyor" sayım kaydı açılır.</summary>
        [HttpPost("UrunKaydet")]
        public IActionResult UrunKaydet([FromBody] StokTanimKayit.UrunIstek g)
        {
            var (kayit, hata) = StokTanimKayit.UrunKaydet(g, Kullanici);
            if (hata != null) return Json(Tanimlar(false, hata));
            if (g.Sayilan.HasValue)
            {
                var (_, sHata) = StokSayimKayit.Kaydet(new[] { new StokSayimKayit.Giris { Anahtar = StokTanimKayit.Anahtar(kayit), Sayilan = g.Sayilan, Tarih = g.SayimTarihi, Not = g.SayimNot } }, Kullanici);
                if (sHata != null) return Json(Tanimlar(false, "Ürün eklendi; sayım kaydedilemedi: " + sHata));
            }
            return Json(Tanimlar(true, "Kaydedildi."));
        }

        public class UrunSilIstek { public int Id { get; set; } }

        /// <summary>Kullanıcının eklediği ürün / demirbaş kaydını ve ona bağlı sayım kayıtlarını siler.</summary>
        [HttpPost("UrunSil")]
        public IActionResult UrunSil([FromBody] UrunSilIstek g)
        {
            var (anahtar, hata) = StokTanimKayit.UrunSil(g?.Id ?? 0);
            if (hata == null) StokSayimKayit.AnahtarSil(anahtar);
            return Json(Tanimlar(hata == null, hata ?? "Silindi."));
        }

        /// <summary>Tüketim ve Yeterlilik › stok eşikleri: kritik stok, minimum stok, tedarik süresi, hedef stok (stok kartı bazında).</summary>
        [HttpPost("EsikKaydet")]
        public IActionResult EsikKaydet([FromBody] List<StokTanimKayit.Esik> g)
        {
            var (adet, hata) = StokTanimKayit.EsikKaydet(g, Kullanici);
            return Json(Tanimlar(hata == null && adet > 0, hata ?? (adet > 0 ? $"{adet} stok kartının eşikleri kaydedildi." : "Kaydedilecek değer yok.")));
        }
    }
}
