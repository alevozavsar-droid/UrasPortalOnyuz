using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Personel Maliyet Raporu — Yönetim › Özel Analizler › Özel Raporlar ve İK ve İdari İşler › İnsan Kaynakları › Personel Maliyet ve Bordro.
    /// "Personel Bordro ve Ödeme Tablosu"nun portal karşılığı: kimlik bilgileri, net ücret (X / R), puantaj ve çalışma kazançları, ek çalışmalar,
    /// sosyal yardımlar, prim, yan haklar, yasal ödemeler, brüt kazanç ve SGK matrahı, gelir / damga vergisi, SGK işçi ve işveren payları,
    /// kesintiler, ödeme özeti ve maliyet; filtreler, KPI, özet raporu, satır ve kart düzenleme, personel ekleme / silme (bellek içi) ve Excel.
    /// Veri, sütun tanımı ve bordro hesabı: Data/PersonelMaliyetOrnek.cs · Betik: wwwroot/js/personel-maliyet.js
    /// </summary>
    [Authorize]
    public class PersonelMaliyetController : Controller
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        private string Yetki() => OrnekVeri.Kullanicilar.FirstOrDefault(k => string.Equals(k.Kod, User?.Identity?.Name ?? "", StringComparison.OrdinalIgnoreCase)).Yetki ?? "";

        public IActionResult Index()
        {
            ViewData["Title"] = "Personel Maliyet Raporu";
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Kaynak = PersonelMaliyetOrnek.KaynakDonem(null);
            ViewBag.Gruplar = PersonelMaliyetOrnek.SutunGruplari;
            return View();
        }

        /// <summary>Seçili dönemin verisi (?donem=yyyy-MM; boş = güncel dönem).</summary>
        [HttpGet]
        public IActionResult Veri(string donem = null)
        {
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, null, null, donem));
        }

        /// <summary>Düzenlenen satırları kaydeder (tablo düzenlemesi ya da kart). Gövde: { satirlar: [ { sira, adSoyad, …, xNet, …, not } ] }</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Kaydet([FromBody] KaydetIstek d)
        {
            if (d == null || d.Satirlar == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMaliyetOrnek.Kaydet(d.Satirlar, Ben(), d.Donem);
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{d.Satirlar.Count} personel satırı kaydedildi.", null, d.Donem));
        }

        /// <summary>Yeni bordro dönemi oluşturur (güncel dönemin kopyası; kesinti taksitleri yeni aya göre yazılır). Gövde: { donem: "yyyy-aa" }</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult DonemOlustur([FromBody] DonemIstek d)
        {
            try
            {
                lock (PersonelMaliyetOrnek.Kilit)
                {
                    var liste = PersonelMaliyetOrnek.DonemOlustur(d?.Donem, d?.Kurlar, out bool yeni);
                    if (yeni && d.Donem != PersonelMaliyetEkimOrnek.Donem) PersonelKesintiOrnek.KesintileriDonemYansit(d.Donem);   // Ekim dosyadan kurulur: kesinti sütunları dosyadaki değerlerdir (plansız BES vb. sıfırlanmaz)
                    if (yeni) { PersonelMaliyetEkOrnek.PrimleriDonemdenTuret(d.Donem); PersonelKesintiOrnek.MesaiTalepleriniTohumla(d.Donem); }   // Prim Ödemeleri ve Ek Çalışmalar sekmeleri yeni dönemin bordro tutarlarıyla birebir (5 Kas 2026)
                    return Json(Paket(true, $"{PersonelMaliyetOrnek.DonemAd(d.Donem)} bordrosu {(yeni ? "oluşturuldu" : "zaten var")} ({liste.Count} kişi).", null, d.Donem));
                }
            }
            catch (ArgumentException ex) { return Json(new { success = false, message = ex.Message }); }
        }

        public class TesvikIstek { public string Sirket { get; set; } public string Kod { get; set; } public bool Secili { get; set; } public string Donem { get; set; } }
        /// <summary>Şirket Teşvik ve İndirimleri: şirketin bir teşvikten faydalanma seçimi (seçili değilse muaf / faydalanamıyor); tüm dönemlerin hesabına yansır. Gövde: { sirket, kod, secili, donem }</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult TesvikKaydet([FromBody] TesvikIstek d)
        {
            string hata = PersonelTesvikOrnek.Kaydet(d?.Sirket, d?.Kod, d?.Secili ?? false);
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{d.Sirket} · {d.Kod}: {(d.Secili ? "faydalanıyor" : "muaf / faydalanamıyor")} olarak kaydedildi; hesaplar yenilendi.", null, d.Donem));
        }

        /// <summary>Yeni personel kartı açar. Gövde: PmDuzenleme (sıra yok sayılır).</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Ekle([FromBody] PmDuzenleme d)
        {
            string hata = PersonelMaliyetOrnek.Ekle(d, Ben(), out var p, d?.Donem);
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{p.AdSoyad} eklendi (sıra {p.Sira}, cari kodu {p.CariKodu}).", p.Sira, d.Donem));
        }

        /// <summary>Satırı siler (hatalı giriş). Gövde: { sira }</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Sil([FromBody] SilIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMaliyetOrnek.Sil(d.Sira, Ben(), d.Donem);
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"Sıra {d.Sira} silindi.", null, d.Donem));
        }
        public class KaydetIstek { public List<PmDuzenleme> Satirlar { get; set; } public string Donem { get; set; } }
        public class DonemIstek { public string Donem { get; set; } public Dictionary<string, decimal> Kurlar { get; set; } }
        public class SilIstek { public int Sira { get; set; } public string Donem { get; set; } }

        // ---- Diğer Kesintiler sekmesi: kesinti / icra planı (PDF ekiyle), ek yükleme ve indirme (Data/PersonelKesintiOrnek.cs)
        /// <summary>Yeni kesinti / icra planı (multipart form: personelSira, tur, kaynak, icraDairesi, alacakli, dosyaNo, toplam, taksit, baslangic, aciklama, donem, dosya).</summary>
        [HttpPost, IgnoreAntiforgeryToken, RequestSizeLimit(10 * 1024 * 1024)]
        public IActionResult KesintiEkle(int personelSira, string tur, string kaynak, string icraDairesi, string alacakli, string dosyaNo, decimal toplam, decimal taksit, string baslangic, string aciklama, string donem, IFormFile dosya)
        {
            (string ad, string tip, byte[] veri)? ek = null;
            if (dosya != null && dosya.Length > 0) { if (dosya.Length > 8 * 1024 * 1024) return Json(new { success = false, message = "Dosya 8 MB'tan büyük olamaz." }); using var ms = new MemoryStream(); dosya.CopyTo(ms); ek = (dosya.FileName, dosya.ContentType, ms.ToArray()); }
            string hata = PersonelKesintiOrnek.KesintiEkle(personelSira, tur, kaynak, icraDairesi, alacakli, dosyaNo, toplam, taksit, baslangic, aciklama, Ben(), ek, out var k);
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{k.AdSoyad} için {k.Tur} planı eklendi: {k.TaksitSayisi} taksit, {PersonelMaliyetOrnek.DonemAd(k.BaslangicDonem)} – {PersonelMaliyetOrnek.DonemAd(k.BitisDonem)}.", null, donem));
        }
        [HttpPost, IgnoreAntiforgeryToken, RequestSizeLimit(10 * 1024 * 1024)]
        public IActionResult KesintiEkYukle(int id, string donem, IFormFile dosya)
        {
            if (dosya == null || dosya.Length == 0) return Json(new { success = false, message = "Dosya seçilmedi." });
            using var ms = new MemoryStream(); dosya.CopyTo(ms);
            string hata = PersonelKesintiOrnek.EkYukle(id, dosya.FileName, dosya.ContentType, ms.ToArray(), Ben());
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{dosya.FileName} eklendi.", null, donem));
        }
        // ---- sekmelerde satır düzenleme (çekmece): üretim personeli, prim satırları, kesinti planları — kaydedilince bordro satırı / kart / cari güncellenir
        public class SilIstek2 { public int Id { get; set; } public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken] public IActionResult UretimKaydet([FromBody] PersonelMaliyetEkOrnek.UretimIstek d) { string h = PersonelMaliyetEkOrnek.UretimKaydet(d, out var u); if (h != null) return Json(new { success = false, message = h }); lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{u.AdSoyad} kaydedildi (üretim personeli).", null, d?.Donem)); }
        [HttpPost, IgnoreAntiforgeryToken] public IActionResult UretimSil([FromBody] SilIstek2 d) { string h = PersonelMaliyetEkOrnek.UretimSil(d?.Id ?? 0); if (h != null) return Json(new { success = false, message = h }); lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, "Üretim personeli silindi.", null, d?.Donem)); }
        [HttpPost, IgnoreAntiforgeryToken] public IActionResult PrimKaydet([FromBody] PersonelMaliyetEkOrnek.PrimIstek d) { string h = PersonelMaliyetEkOrnek.PrimKaydet(d, out var r); if (h != null) return Json(new { success = false, message = h }); lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{r.AdSoyad} · {r.DonemAd} · {r.PrimTuru}: {r.HesaplananPrim.ToString("N2", Tr)} ₺ kaydedildi; {PersonelMaliyetOrnek.DonemAd(r.OdemeDonem)} bordrosundaki Prim-{r.Odeme} güncellendi.", null, d?.BordroDonem)); }
        [HttpPost, IgnoreAntiforgeryToken] public IActionResult PrimSil([FromBody] SilIstek2 d) { string h = PersonelMaliyetEkOrnek.PrimSil(d?.Id ?? 0); if (h != null) return Json(new { success = false, message = h }); lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, "Prim satırı silindi; bordro primi güncellendi.", null, d?.Donem)); }
        [HttpPost, IgnoreAntiforgeryToken] public IActionResult KesintiGuncelle([FromBody] PersonelKesintiOrnek.KesintiIstek d) { string h = PersonelKesintiOrnek.KesintiGuncelle(d, Ben(), out var k); if (h != null) return Json(new { success = false, message = h }); lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{k.AdSoyad} · {k.Tur} planı güncellendi: {k.TaksitSayisi} taksit, {PersonelMaliyetOrnek.DonemAd(k.BaslangicDonem)} – {PersonelMaliyetOrnek.DonemAd(k.BitisDonem)}; bordro kesinti sütunu yenilendi.", null, d?.Donem)); }
        [HttpPost, IgnoreAntiforgeryToken] public IActionResult KesintiSil([FromBody] SilIstek2 d) { string h = PersonelKesintiOrnek.KesintiSil(d?.Id ?? 0); if (h != null) return Json(new { success = false, message = h }); lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, "Kesinti planı silindi; bordro kesinti sütunu yenilendi.", null, d?.Donem)); }

        // ---- Muhasebe Kayıtları sekmesi: simülasyon → kontrol → onay → muhasebeleştirme → ters kayıt; hesap eşleştirme (Data/PersonelMuhasebeOrnek.cs)
        public class SimuleIstek { public string Donem { get; set; } public string Kapsam { get; set; } }
        public class MuhasebeIslemIstek { public int Id { get; set; } public string Islem { get; set; } public string Not { get; set; } public string Donem { get; set; } }
        public class EslemeIstek { public int? Id { get; set; } public string Sirket { get; set; } public string KarsiSirket { get; set; } public string IslemTuru { get; set; } public string KaynakKalem { get; set; } public string XR { get; set; } public string Yon { get; set; } public string HesapKodu { get; set; } public bool Varsayilan { get; set; } = true; public string GecerlilikBas { get; set; } public string GecerlilikBit { get; set; } public bool Aktif { get; set; } = true; public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult MuhasebeSimule([FromBody] SimuleIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMuhasebeOrnek.Olustur(d.Donem, d.Kapsam, Ben(), out var s);
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"{s.No} v{s.Versiyon} oluşturuldu: {s.PersonelSayisi} personel, {s.Fisler.Count} fiş, {s.Fisler.Sum(f => f.Satirlar.Count)} satır · {s.Durum}. Gerçek fiş atılmadı.", null, d.Donem));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult MuhasebeIslem([FromBody] MuhasebeIslemIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMuhasebeOrnek.Islem(d.Id, d.Islem, d.Not, Ben(), Yetki(), out var s);
            if (hata != null) return Json(new { success = false, message = hata });
            string m = d.Islem == "onayaGonder" ? $"{s.No} v{s.Versiyon} onaya gönderildi." : d.Islem == "onayla" ? $"{s.No} v{s.Versiyon} onaylandı; içerik kilitlendi." : d.Islem == "muhasebelestir" ? $"{s.No} muhasebeleştirildi: {string.Join(", ", s.Fisler.Select(f => f.MuhasebeFisNo))}." : d.Islem == "tersKayit" ? $"{s.No} için ters kayıt oluşturuldu: {string.Join(", ", s.Fisler.Select(f => f.TersFisNo))}." : $"{s.No} iptal edildi.";
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, m, null, d.Donem));
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult MuhasebeEsleme([FromBody] EslemeIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            string hata = PersonelMuhasebeOrnek.EslemeKaydet(d.Id, d.Sirket, d.KarsiSirket, d.IslemTuru, d.KaynakKalem, d.XR, d.Yon, d.HesapKodu, d.Varsayilan, d.GecerlilikBas, d.GecerlilikBit, d.Aktif, Ben(), out var e);
            if (hata != null) return Json(new { success = false, message = hata });
            lock (PersonelMaliyetOrnek.Kilit) return Json(Paket(true, $"Hesap eşleştirmesi kaydedildi: {e.IslemTuru} / {e.KaynakKalem} / {e.XR} / {e.Yon} → {e.HesapKodu} {e.HesapAdi}. Açık simülasyonlar güncelliğini yitirdi; yeniden simülasyon gerekir.", null, d.Donem));
        }
        [HttpGet]
        public IActionResult KesintiEk(int id, int no)
        {
            var e = PersonelKesintiOrnek.Ek(id, no); if (e == null || e.Veri == null) return NotFound("Ek bulunamadı.");
            return File(e.Veri, e.Tip ?? "application/octet-stream", e.Ad);
        }

        private object Paket(bool ok, string message, int? yeniSira, string donem) { PersonelMaliyetEkOrnek.UretimYansit(); return PaketIc(ok, message, yeniSira, donem); }   // gündelikçi R mahsubu / elden X önce ana tabloya yazılır
        private object PaketIc(bool ok, string message, int? yeniSira, string donem)
        {
            string etkin = PersonelMaliyetOrnek.ListeDonem(donem) != null ? donem : PersonelMaliyetOrnek.EnYeniDonem;   // istenen dönem yoksa en yeni dönem; etiket, kişi listesi ve ek sekmeler hep aynı dönemden (5 Kas 2026: önce dönemsiz istekte kişi listesi Eylül'e düşüyordu)
            return new
            {
                success = ok, message, yeniSira, yil = DateTime.Today.Year, kaynak = PersonelMaliyetOrnek.KaynakDonem(etkin), ben = Ben(),
                donem = etkin, donemAd = PersonelMaliyetOrnek.DonemAd(etkin), donemler = PersonelMaliyetOrnek.DonemPaketi,
                sonKayit = PersonelMaliyetOrnek.SonKayit.HasValue ? new { kisi = PersonelMaliyetOrnek.SonKayit.Value.Kisi, tarih = PersonelMaliyetOrnek.SonKayit.Value.Tarih.ToString("dd.MM.yyyy HH:mm") } : null,
                sutunlar = PersonelMaliyetOrnek.SutunPaketi, parametre = BordroParametre.Paket, odemeSirketi = new { r = PersonelMuhasebeOrnek.OdemeSirketiR, fili = PersonelMuhasebeOrnek.FiiliSirketler },
                sgkIsverenleri = PersonelMaliyetOrnek.SgkIsverenleri, gorevYerleri = PersonelMaliyetOrnek.GorevYerleri, catiDepartmanlar = PersonelMaliyetOrnek.CatiDepartmanlar,
                bolumler = PersonelMaliyetOrnek.Bolumler, unvanlar = PersonelMaliyetOrnek.Unvanlar, gorevler = PersonelMaliyetOrnek.Gorevler, ozelDurumlar = PersonelMaliyetOrnek.OzelDurumlar, durumlar = PersonelMaliyetOrnek.Durumlar, paraBirimleri = PersonelMaliyetOrnek.ParaBirimleri,
                personel = PersonelMaliyetOrnek.ListeDonem(etkin).OrderBy(p => p.Sira).Select(PersonelMaliyetOrnek.Dto).ToList(),   // seçili dönem; kilit içinde maddeleştir: serileştirme action döndükten sonra çalışır, ertelenmiş sorgu kilitsiz gezilirdi
                tesvik = PersonelTesvikOrnek.Paket(),   // Şirket Teşvik ve İndirimleri: katalog, şirket seçimleri, istemci hesabı için oranlar (Data/PersonelTesvikOrnek.cs)
                ekSutunlar = PersonelMaliyetEkOrnek.SutunPaketi, uretim = PersonelMaliyetEkOrnek.UretimDto(etkin), primler = PersonelMaliyetEkOrnek.PrimDto(),   // ek sekmeler (Data/PersonelMaliyetEkOrnek.cs); üretim R mahsup / elden X seçili döneme göre; özel kişiler personel listesinde ozel = true
                ekCalisma = PersonelKesintiOrnek.EkCalismaPaketi(etkin),   // Ek Çalışmalar: mesai talepleri (Data/PersonelKesintiOrnek.cs)
                kesinti = PersonelKesintiOrnek.KesintiPaketi(etkin),       // Diğer Kesintiler: şahsi avanslar, kesinti planları, ekler
                muhasebe = PersonelMuhasebeOrnek.Paket(etkin, Ben(), Yetki())   // Muhasebe Kayıtları: simülasyonlar, hesap eşleştirme, yetki (Data/PersonelMuhasebeOrnek.cs)
            };
        }
    }
}
