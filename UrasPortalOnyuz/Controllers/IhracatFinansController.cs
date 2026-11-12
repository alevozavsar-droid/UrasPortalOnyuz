using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// İHRACAT FİNANSAL TAKİBİ (11 Kas 2026) — Mali İşler › Finans › Döviz ve Dış Ticaret Finansmanı; Yurtdışı Satış › İhracat Operasyon Takibi (iki menüde de doğrudan açılır).
    /// "İhracat Tahsilat Raporu" ile "İBKB Beyanname Takibi" tek, konsolide (tüm şirketler) ve dinamik raporda birleşti:
    ///   İhracat Satışları (Yurtdışı Satış Siparişi'nden canlı) · Beyanname / İBKB Dosyaları · Banka Tahsilatları (bankadan gelen döviz girişleri; hangi ihracat dosyası / GÇB ile
    ///   eşleşeceği seçilir, seçimde "İBKB oluşturulsun mu?" sorulur) · İBKB Belgeleri (bankaya verilecek basılabilir başvuru formu) · Müşteri · Vade · DAB.
    /// Veri Data/IbkbOrnek.cs (App_Data/ibkb.json; Excel tohumu), betik wwwroot/js/ihracat-finans.js. Eski /Ibkb adresi buraya yönlenir.
    /// </summary>
    [Authorize]
    [Route("IhracatFinans")]
    public class IhracatFinansController : Controller
    {
        public class IdIstek { public int Id { get; set; } }
        private string Kullanici => User?.Identity?.Name ?? "";
        private static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        private static string P(decimal? v) => (v ?? 0).ToString("N2", Tr);

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme, int gorev = 0)
        {
            ViewData["Title"] = "İhracat Finansal Takibi";
            ViewBag.Sekme = gorev > 0 ? "bakiye" : sekme ?? "ozet"; ViewBag.Gorev = gorev;   // ?gorev=ID: e-posta / portal mesajındaki bağlantı; görev penceresi açılır
            return View();
        }

        [HttpGet("Veri")]
        public IActionResult Veri() => Json(IbkbOrnek.Paket());

        [HttpPost("Kaydet"), IgnoreAntiforgeryToken]
        public IActionResult Kaydet([FromBody] IbkbIstek d)
        {
            var h = IbkbOrnek.Kaydet(d, Kullanici, out var k);
            return Json(new { success = h == null, message = h ?? $"İhracat dosyası {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {k?.Sirket} · {k?.DosyaNo} · {k?.Musteri}.", id = k?.Id, veri = h == null ? IbkbOrnek.Paket() : null });
        }

        [HttpPost("Sil"), IgnoreAntiforgeryToken]
        public IActionResult Sil([FromBody] IdIstek d)
        {
            var h = IbkbOrnek.Sil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "İhracat dosyası silindi.", veri = h == null ? IbkbOrnek.Paket() : null });
        }

        // ---------- banka tahsilatları
        [HttpPost("TahsilatKaydet"), IgnoreAntiforgeryToken]
        public IActionResult TahsilatKaydet([FromBody] IhracatTahsilatIstek d)
        {
            var h = IbkbOrnek.TahsilatKaydet(d, Kullanici, out var t);
            return Json(new { success = h == null, message = h ?? $"Banka tahsilatı {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {t?.Banka} · {P(t?.Tutar)} {t?.Doviz} · {t?.Gonderen}.", id = t?.Id, veri = h == null ? IbkbOrnek.Paket() : null });
        }
        [HttpPost("TahsilatSil"), IgnoreAntiforgeryToken]
        public IActionResult TahsilatSil([FromBody] IdIstek d)
        {
            var h = IbkbOrnek.TahsilatSil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "Banka tahsilatı silindi.", veri = h == null ? IbkbOrnek.Paket() : null });
        }
        /// <summary>Tahsilat ↔ ihracat dosyası eşleştirme; IbkbOlustur ile İBKB belgesi üretilir (bankaya verilecek form).</summary>
        [HttpPost("Eslestir"), IgnoreAntiforgeryToken]
        public IActionResult Eslestir([FromBody] EslestirIstek d)
        {
            var h = IbkbOrnek.Eslestir(d, Kullanici, out var b);
            return Json(new { success = h == null, message = h ?? (b != null ? $"Tahsilat dosyaya bağlandı ve İBKB belgesi hazırlandı: {b.No} · {P(b.Tutar)} {b.Doviz} · {b.Banka}." : "Tahsilat ihracat dosyasına bağlandı; DAB satırı yazıldı."), belgeId = b?.Id, belgeNo = b?.No, veri = h == null ? IbkbOrnek.Paket() : null });
        }
        [HttpPost("EslesmeKaldir"), IgnoreAntiforgeryToken]
        public IActionResult EslesmeKaldir([FromBody] IdIstek d)
        {
            var h = IbkbOrnek.EslesmeKaldir(d?.Id ?? 0, Kullanici);
            return Json(new { success = h == null, message = h ?? "Eşleşme kaldırıldı; DAB satırı silindi, İBKB belgesi iptal edildi.", veri = h == null ? IbkbOrnek.Paket() : null });
        }

        // ---------- İBKB belgeleri
        [HttpPost("BelgeDurum"), IgnoreAntiforgeryToken]
        public IActionResult BelgeDurum([FromBody] IbkbBelgeIstek d)
        {
            var h = IbkbOrnek.BelgeDurum(d, Kullanici);
            return Json(new { success = h == null, message = h ?? "İBKB belgesi güncellendi.", veri = h == null ? IbkbOrnek.Paket() : null });
        }
        /// <summary>İhracat cari bakiyesi kontrol işareti (Cari Bakiyeler sekmesi; 11 Kas 2026).</summary>
        [HttpPost("BakiyeKontrolKaydet"), IgnoreAntiforgeryToken]
        public IActionResult BakiyeKontrolKaydet([FromBody] IbkbBakiyeKontrolIstek d)
        {
            var h = IbkbOrnek.BakiyeKontrolKaydet(d, Kullanici, out var k, out var g);
            return Json(new { success = h == null, message = h ?? $"Bakiye kontrolü kaydedildi: {k?.CariKodu} · {k?.Durum}." + (g == null ? "" : $" Düzeltme görevi {g.Atanan} kişisine atandı (#{g.Id}){(g.Bildirim == "" ? "" : " · " + g.Bildirim)}."), gorev = g == null ? null : GorevKisa(g), veri = h == null ? IbkbOrnek.Paket() : null });
        }
        static object GorevKisa(IbkbGorev g) { var e = IbkbOrnek.GorevEposta(g); return new { g.Id, g.Atanan, g.AtananEposta, g.Durum, konu = e.Konu, govde = e.Govde }; }
        /// <summary>Cari bakiye düzeltme görevi akışı (12 Kas 2026): ata · guncelle · duzeltildi · onayla · iade · iptal.</summary>
        [HttpPost("GorevKaydet"), IgnoreAntiforgeryToken]
        public IActionResult GorevKaydet([FromBody] IbkbGorevIstek d)
        {
            var h = IbkbOrnek.GorevKaydet(d, Kullanici, out var g);
            string islem = (d?.Islem ?? "").ToLowerInvariant();
            string m = islem == "duzeltildi" ? "Görev Düzeltildi olarak işaretlendi; muhasebe onayı bekliyor." : islem == "onayla" ? "Düzeltme onaylandı; görev kapandı." : islem == "iade" ? "Görev iade edildi; atanan kişi yeniden düzeltecek." : islem == "iptal" ? "Görev iptal edildi." : islem == "guncelle" ? "Görev güncellendi." : $"Düzeltme görevi {g?.Atanan} kişisine atandı (#{g?.Id})";
            return Json(new { success = h == null, message = h ?? m + (g != null && g.Bildirim != "" && (islem == "ata" || islem == "" || islem == "guncelle" || islem == "iade") ? " · " + g.Bildirim : ""), gorev = g == null ? null : GorevKisa(g), veri = h == null ? IbkbOrnek.Paket() : null });
        }
        /// <summary>Müşteriye mutabakat gönderimi (E-posta / WhatsApp; bakiye ya da ekstre); id > 0 ise yanıt (Mutabık / İtirazlı).</summary>
        [HttpPost("CariGonderim"), IgnoreAntiforgeryToken]
        public IActionResult CariGonderim([FromBody] CariGonderimIstek d)
        {
            var h = IbkbOrnek.CariGonderimKaydet(d, Kullanici, out var g);
            return Json(new { success = h == null, message = h ?? (d?.Id > 0 ? $"Mutabakat yanıtı işlendi: {g?.Durum}." : $"Mutabakat gönderimi kaydedildi: {g?.Kanal} · {g?.Icerik} · {g?.Alici}{(string.IsNullOrEmpty(g?.MtBelgeNo) ? "" : " · " + g.MtBelgeNo)}."), id = g?.Id, veri = h == null ? IbkbOrnek.Paket() : null });
        }
        /// <summary>Müşteriye gönderilen ekstre bağlantısı: jetonla açılır (portal girişi istemez); döviz bazında bakiye, hareketler, açık ihracat dosyaları. Yazdır → PDF.</summary>
        [HttpGet("CariEkstre"), AllowAnonymous]
        public IActionResult CariEkstre(string kod, string t)
        {
            if (string.IsNullOrEmpty(kod) || (User?.Identity?.IsAuthenticated != true && !string.Equals(t, IbkbOrnek.EkstreJeton(kod), System.StringComparison.OrdinalIgnoreCase))) return NotFound("Ekstre bağlantısı geçersiz.");
            var c = MutabakatOrnek.Bul(kod); if (c == null) return NotFound("Cari bulunamadı.");
            ViewData["Title"] = (c.Kart?.KisaAd ?? kod) + " · Cari Hesap Ekstresi";
            ViewBag.Dosyalar = IbkbOrnek.CariDosyalar(kod, c.Kart);
            ViewBag.Gonderimler = IbkbOrnek.CariGonderimler(kod);
            ViewBag.Sirket = string.IsNullOrEmpty(c.Kart?.Sirket) ? null : SatisSiparisOrnek.SirketBul(c.Kart.Sirket);
            return View(c);
        }
        // ---------- yurtdışı şube carileri (11 Kas 2026)
        [HttpPost("SubeKaydet"), IgnoreAntiforgeryToken]
        public IActionResult SubeKaydet([FromBody] SubeIstek d)
        {
            var h = IbkbOrnek.SubeKaydet(d, Kullanici, out var s);
            return Json(new { success = h == null, message = h ?? $"Şube {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {s?.Kod} · {s?.Sube} · bakiye {P(s == null ? 0 : IbkbOrnek.SubeBakiye(s))} {s?.ParaBirimi}.", id = s?.Id, veri = h == null ? IbkbOrnek.Paket() : null });
        }
        [HttpPost("SubeSil"), IgnoreAntiforgeryToken]
        public IActionResult SubeSil([FromBody] IdIstek d)
        {
            var h = IbkbOrnek.SubeSil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "Şube silindi.", veri = h == null ? IbkbOrnek.Paket() : null });
        }
        /// <summary>Şube yıl satırı kalemleri (12 Kas 2026): tutara tıklayınca açılan detay; ekle / düzenle / sil / yıl satırını kalem toplamıyla eşitle.</summary>
        [HttpPost("SubeKalemKaydet"), IgnoreAntiforgeryToken]
        public IActionResult SubeKalemKaydet([FromBody] SubeKalemIstek d)
        {
            var h = IbkbOrnek.SubeKalemKaydet(d, Kullanici, out var s);
            return Json(new { success = h == null, message = h ?? $"Kalem {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {d?.Yil} · {P(d?.Tutar)} {s?.ParaBirimi} · {d?.BelgeNo}.", veri = h == null ? IbkbOrnek.Paket() : null });
        }
        public class KalemSilIstek { public int SubeId { get; set; } public int Id { get; set; } }
        [HttpPost("SubeKalemSil"), IgnoreAntiforgeryToken]
        public IActionResult SubeKalemSil([FromBody] KalemSilIstek d)
        {
            var h = IbkbOrnek.SubeKalemSil(d?.SubeId ?? 0, d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "Kalem silindi.", veri = h == null ? IbkbOrnek.Paket() : null });
        }
        public class EsitleIstek { public int SubeId { get; set; } public string Yil { get; set; } public string Alan { get; set; } }
        [HttpPost("SubeYilEsitle"), IgnoreAntiforgeryToken]
        public IActionResult SubeYilEsitle([FromBody] EsitleIstek d)
        {
            var h = IbkbOrnek.SubeYilEsitle(d?.SubeId ?? 0, d?.Yil, d?.Alan, Kullanici, out var yeni);
            return Json(new { success = h == null, message = h ?? $"Yıl satırı kalem toplamıyla eşitlendi: {d?.Yil} · {P(yeni)}.", veri = h == null ? IbkbOrnek.Paket() : null });
        }
        /// <summary>Şubeye ekstre / mutabakat gönderimi kaydı (E-posta ya da WhatsApp); id > 0 ise yanıt / durum güncelleme.</summary>
        [HttpPost("SubeGonderim"), IgnoreAntiforgeryToken]
        public IActionResult SubeGonderim([FromBody] SubeGonderimIstek d)
        {
            var h = IbkbOrnek.SubeGonderimKaydet(d, Kullanici, out var g);
            return Json(new { success = h == null, message = h ?? (d?.Id > 0 ? $"Gönderim durumu güncellendi: {g?.Durum}." : $"Ekstre gönderimi kaydedildi: {g?.Kanal} · {g?.Alici} · {g?.Donem}."), id = g?.Id, veri = h == null ? IbkbOrnek.Paket() : null });
        }
        /// <summary>Şube ekstresi / mutabakat mektubu (basılabilir; e-postaya PDF olarak eklenir).</summary>
        [HttpGet("SubeEkstre/{id:int}")]
        public IActionResult SubeEkstre(int id)
        {
            var s = IbkbOrnek.SubeBul(id); if (s == null) return NotFound("Şube bulunamadı.");
            ViewData["Title"] = s.Sube + " · Şube Ekstresi";
            ViewBag.Gonderimler = IbkbOrnek.SubeGonderimler(id);
            ViewBag.Sirket = string.IsNullOrEmpty(s.Sirket) ? null : SatisSiparisOrnek.SirketBul(s.Sirket);
            return View(s);
        }
        [HttpPost("BelgeSil"), IgnoreAntiforgeryToken]
        public IActionResult BelgeSil([FromBody] IdIstek d)
        {
            var h = IbkbOrnek.BelgeSil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "İptal edilmiş İBKB belgesi silindi.", veri = h == null ? IbkbOrnek.Paket() : null });
        }
        /// <summary>Bankaya verilecek İBKB başvuru formu (basılabilir sayfa; tarayıcıdan PDF).</summary>
        [HttpGet("Belge/{id:int}")]
        public IActionResult Belge(int id)
        {
            var b = IbkbOrnek.BelgeBul(id); if (b == null) return NotFound("İBKB belgesi bulunamadı.");
            ViewData["Title"] = b.No + " · İBKB Başvuru Formu";
            ViewBag.Dosya = IbkbOrnek.DosyaBul(b.DosyaId);
            ViewBag.Sirket = SatisSiparisOrnek.SirketBul(b.Sirket);
            ViewBag.Kart = string.IsNullOrEmpty(b.MusteriKodu) ? null : CariKartOrnek.KartBul(b.MusteriKodu);
            return View(b);
        }
    }

    /// <summary>Eski adres: /Ibkb → İhracat Finansal Takibi (11 Kas 2026; İBKB Beyanname Takibi bu rapora katıldı).</summary>
    [Authorize]
    [Route("Ibkb")]
    public class IbkbController : Controller
    {
        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme) => RedirectPermanent("/IhracatFinans" + (string.IsNullOrEmpty(sekme) ? "" : "?sekme=" + sekme));
        [HttpGet("Veri")]
        public IActionResult Veri() => Json(IbkbOrnek.Paket());
    }
}
