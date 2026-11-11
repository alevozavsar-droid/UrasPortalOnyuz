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
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "İhracat Finansal Takibi";
            ViewBag.Sekme = sekme ?? "ozet";
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
