using System;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FinansRaporlama.Controllers;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// KREDİ RAPORU (9 Kas 2026) — Mali İşler › Finans › Krediler ve Finansman. Veri Data/KrediYapilandirmaOrnek.cs (App_Data/kredi-yapilandirma.json kalıcı, belgeler App_Data/kredi-belgeler),
    /// görünüm Views/KrediYapilandirma/Index.cshtml, betik wwwroot/js/kredi-yapilandirma.js. Yapılandırma öncesi / sonrası kredi bilgileri, peşinat, taksit takvimi (Kasa Nakit Akışı + Planlı Düzenli Ödemeler),
    /// rehin · ipotek takibi (kredi teminatları + eski krediler için bağımsız kayıtlar), teminata verilen çekler, belgeler (sözleşme / GKS / ipotek PDF'leri). ?sekme=krediler|oncesi|sonrasi|takvim|aylik|rehin|cekler|belgeler
    /// Konkordato kümesi (10 Kas 2026, Data/KonkordatoOrnek.cs, App_Data/konkordato.json; Bakırköy 2. ATM 2024/894 E. bilirkişi raporu tohumu): ?sekme=konkordato|sozlesmeler|hesaplar|tahsilatlar|mektuplar
    /// Banka Borç Dökümü (11 Kas 2026, Emlak Katılım · Alv Kimya / Daf Kimya taksit bazlı güncel borç; tohum Data/Tohum/emlak-katilim-borc-2026-11.json; kredi kaydı değildir, nakit akışa yazmaz): ?sekme=borclar
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class KrediYapilandirmaController : Controller
    {
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        static string P(decimal? v) => (v ?? 0).ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("tr-TR"));   // mesajlarda Türkçe sayı biçimi
        public class IdIstek { public int Id { get; set; } }
        public class AktarIstek { public int Id { get; set; } public bool Aktar { get; set; } = true; }
        static object Paket() => KrediYapilandirmaOrnek.Paket();

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Kredi Raporu";
            ViewBag.Sekme = string.IsNullOrEmpty(sekme) ? "ozet" : sekme;   // açılış: Özet (10 Kas 2026)
            OrnekDoldurucu.Hazirla(this);
            return View("Index");
        }
        [HttpGet("Veri")]
        public IActionResult Veri() => Json(Paket());

        /// <summary>Kredi ekle / düzenle: kaydeder, taksit satırlarını Kasa Nakit Akışı'na ve düzenli ödeme kaydına eşler; tüm paket döner.</summary>
        [HttpPost("Kaydet"), IgnoreAntiforgeryToken]
        public IActionResult Kaydet([FromBody] KrediIstek d)
        {
            var h = KrediYapilandirmaOrnek.Kaydet(d, Kullanici, out var k, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, id = k?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("Sil"), IgnoreAntiforgeryToken]
        public IActionResult Sil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.Sil(d?.Id ?? 0, Kullanici, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, veri = h == null ? Paket() : null });
        }
        /// <summary>Nakit akışa aktarımı aç / kapat (kapalı: ödenmemiş taksit satırları kaldırılır, düzenli ödeme pasife alınır); yeniden eşleme için aynı değerle çağrılabilir.</summary>
        [HttpPost("Aktar"), IgnoreAntiforgeryToken]
        public IActionResult Aktar([FromBody] AktarIstek d)
        {
            var h = KrediYapilandirmaOrnek.AktarDegistir(d?.Id ?? 0, d?.Aktar ?? true, Kullanici, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, veri = h == null ? Paket() : null });
        }

        // ---------- rehin / ipotek takibi (bağımsız kayıtlar) ----------
        [HttpPost("TeminatKaydet"), IgnoreAntiforgeryToken]
        public IActionResult TeminatKaydet([FromBody] TeminatIstek d)
        {
            var h = KrediYapilandirmaOrnek.TeminatKaydet(d, Kullanici, out var t);
            return Json(new { success = h == null, message = h ?? $"Rehin / ipotek kaydı {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {t?.Banka} · {t?.Tur} · {t?.Aciklama}.", id = t?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("TeminatSil"), IgnoreAntiforgeryToken]
        public IActionResult TeminatSil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.TeminatSil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "Rehin / ipotek kaydı silindi.", veri = h == null ? Paket() : null });
        }
        // ---------- teminata verilen çekler ----------
        [HttpPost("CekKaydet"), IgnoreAntiforgeryToken]
        public IActionResult CekKaydet([FromBody] CekIstek d)
        {
            var h = KrediYapilandirmaOrnek.CekKaydet(d, Kullanici, out var c);
            return Json(new { success = h == null, message = h ?? $"Teminat çeki {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {c?.CekNo} · {c?.Banka} · {c?.Durum}.", id = c?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("CekSil"), IgnoreAntiforgeryToken]
        public IActionResult CekSil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.CekSil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "Teminat çeki kaydı silindi.", veri = h == null ? Paket() : null });
        }
        // ---------- banka borç dökümü (11 Kas 2026: Emlak Katılım · Alv Kimya / Daf Kimya taksit dökümü; kredi kaydı değildir, nakit akışa yazmaz) ----------
        [HttpPost("BorcKaydet"), IgnoreAntiforgeryToken]
        public IActionResult BorcKaydet([FromBody] BorcIstek d)
        {
            var h = KrediYapilandirmaOrnek.BorcKaydet(d, Kullanici, out var b);
            return Json(new { success = h == null, message = h ?? $"Borç satırı {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {b?.Sirket} · {b?.Banka} · {b?.Vade:dd.MM.yyyy}{(string.IsNullOrEmpty(b?.EkNo) ? "" : " · Ek No " + b.EkNo)}.", id = b?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("BorcSil"), IgnoreAntiforgeryToken]
        public IActionResult BorcSil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.BorcSil(d?.Id ?? 0);
            return Json(new { success = h == null, message = h ?? "Borç satırı silindi.", veri = h == null ? Paket() : null });
        }
        /// <summary>Firma × döviz özetinin Yapılandırma Güncel tutarı (11 Kas 2026; ÖZET sayfası): firma + banka + döviz anahtarıyla; tutar boşsa satır silinir.</summary>
        [HttpPost("BorcOzetKaydet"), IgnoreAntiforgeryToken]
        public IActionResult BorcOzetKaydet([FromBody] BorcOzetSatiri d)
        {
            var h = KrediYapilandirmaOrnek.BorcOzetKaydet(d, Kullanici, out var o);
            return Json(new { success = h == null, message = h ?? (o == null ? "Yapılandırma güncel tutarı kaldırıldı." : $"Yapılandırma güncel tutarı kaydedildi: {o.Sirket} · {o.PB} · {P(o.YapilandirmaGuncel)}."), veri = h == null ? Paket() : null });
        }
        // ---------- belgeler (sözleşme, GKS, ipotek, çek fotokopisi …) ----------
        /// <summary>Çok parçalı yükleme: dosya (PDF / JPG / PNG, en çok 20 MB) + tur, baslik, krediId, cekId, teminatId, sirket, banka, tarih, aciklama.</summary>
        [HttpPost("BelgeYukle"), IgnoreAntiforgeryToken, RequestSizeLimit(25 * 1024 * 1024)]
        public IActionResult BelgeYukle(IFormFile dosya, string tur, string baslik, int krediId, int cekId, int teminatId, string sirket, string banka, string tarih, string aciklama)
        {
            if (dosya == null || dosya.Length == 0) return Json(new { success = false, message = "Dosya seçilmedi." });
            byte[] veri; using (var ms = new MemoryStream()) { dosya.CopyTo(ms); veri = ms.ToArray(); }
            var h = KrediYapilandirmaOrnek.BelgeEkle(tur, baslik, krediId, cekId, teminatId, sirket, banka, tarih, aciklama, dosya.FileName, dosya.ContentType, veri, Kullanici, out var b);
            return Json(new { success = h == null, message = h ?? $"Belge yüklendi: {b?.Tur} · {b?.Baslik} ({Math.Max(1, (b?.Boyut ?? 0) / 1024)} KB).", id = b?.Id, veri = h == null ? Paket() : null });
        }
        /// <summary>Belgeyi açar (PDF / görsel tarayıcıda; ?indir=1 ile indirir).</summary>
        [HttpGet("Belge/{id:int}")]
        public IActionResult Belge(int id, int indir = 0)
        {
            var b = KrediYapilandirmaOrnek.BelgeBul(id); var yol = KrediYapilandirmaOrnek.BelgeYolu(b);
            if (b == null || yol == null) return NotFound("Belge bulunamadı.");
            if (indir == 1) return PhysicalFile(yol, b.IcerikTuru, b.DosyaAdi);
            var cd = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("inline"); cd.SetHttpFileName(b.DosyaAdi);   // Türkçe karakterli dosya adı RFC 5987 (filename*) ile; düz yazım 500 verir
            Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.ContentDisposition] = cd.ToString();
            return PhysicalFile(yol, b.IcerikTuru);
        }
        [HttpPost("BelgeSil"), IgnoreAntiforgeryToken]
        public IActionResult BelgeSil([FromBody] IdIstek d)
        {
            var h = KrediYapilandirmaOrnek.BelgeSil(d?.Id ?? 0, Kullanici);
            return Json(new { success = h == null, message = h ?? "Belge silindi.", veri = h == null ? Paket() : null });
        }

        // ---------- konkordato · alacaklı bankalar (10 Kas 2026; Data/KonkordatoOrnek.cs, App_Data/konkordato.json) ----------
        [HttpPost("KonkordatoDosya"), IgnoreAntiforgeryToken]
        public IActionResult KonkordatoDosya([FromBody] KonkordatoDosya d)
        {
            var h = KonkordatoOrnek.DosyaKaydet(d, Kullanici);
            return Json(new { success = h == null, message = h ?? $"Konkordato dosyası kaydedildi: {d?.Mahkeme} {d?.DosyaNo}.", veri = h == null ? Paket() : null });
        }
        [HttpPost("KontrolKaydet"), IgnoreAntiforgeryToken]   // banka × şirket veri kontrol listesi (11 Kas 2026)
        public IActionResult KontrolKaydet([FromBody] KonkordatoKontrol d)
        {
            var h = KonkordatoOrnek.KontrolKaydet(d, Kullanici, out var k);
            return Json(new { success = h == null, message = h ?? $"Kontrol işareti kaydedildi: {k?.Banka} × {k?.Sirket} · {k?.Durum}.", veri = h == null ? Paket() : null });
        }
        [HttpPost("AlacakKaydet"), IgnoreAntiforgeryToken]
        public IActionResult AlacakKaydet([FromBody] KonkordatoAlacak d)
        {
            var h = KonkordatoOrnek.AlacakKaydet(d, Kullanici, out var a);
            return Json(new { success = h == null, message = h ?? $"Alacak kaydı {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {a?.Banka} · {a?.Borclu} · mühlet bakiyesi {P(a == null ? 0 : KonkordatoOrnek.Bakiye(a))} ₺.", id = a?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("AlacakSil"), IgnoreAntiforgeryToken]
        public IActionResult AlacakSil([FromBody] IdIstek d)
        {
            var h = KonkordatoOrnek.AlacakSil(d?.Id ?? 0, out var ozet);
            return Json(new { success = h == null, message = h ?? ozet, veri = h == null ? Paket() : null });
        }
        [HttpPost("SozlesmeKaydet"), IgnoreAntiforgeryToken]
        public IActionResult SozlesmeKaydet([FromBody] KonkordatoSozlesme d)
        {
            var h = KonkordatoOrnek.SozlesmeKaydet(d, Kullanici, out var s);
            return Json(new { success = h == null, message = h ?? $"Sözleşme {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {s?.Banka} · {s?.Borclu} · {s?.Tur} {P(s?.Tutar)} {s?.PB} · {s?.Kefiller?.Count ?? 0} kefil.", id = s?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("SozlesmeSil"), IgnoreAntiforgeryToken]
        public IActionResult SozlesmeSil([FromBody] IdIstek d) { var h = KonkordatoOrnek.SozlesmeSil(d?.Id ?? 0); return Json(new { success = h == null, message = h ?? "Sözleşme kaydı silindi.", veri = h == null ? Paket() : null }); }
        [HttpPost("HesapKaydet"), IgnoreAntiforgeryToken]
        public IActionResult HesapKaydet([FromBody] KonkordatoHesap d)
        {
            var h = KonkordatoOrnek.HesapKaydet(d, Kullanici, out var x);
            return Json(new { success = h == null, message = h ?? $"Hesap satırı {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {x?.Banka} · {x?.KrediNo} {x?.Tur} · bakiye {P(x?.Bakiye)} ₺.", id = x?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("HesapSil"), IgnoreAntiforgeryToken]
        public IActionResult HesapSil([FromBody] IdIstek d) { var h = KonkordatoOrnek.HesapSil(d?.Id ?? 0); return Json(new { success = h == null, message = h ?? "Hesap satırı silindi.", veri = h == null ? Paket() : null }); }
        [HttpPost("TaksitDurum"), IgnoreAntiforgeryToken]   // ödeme planı taksiti ödendi / ödenmedi (12 Kas 2026)
        public IActionResult TaksitDurum([FromBody] KonkordatoTaksitIstek d)
        {
            var h = KonkordatoOrnek.TaksitDurum(d, Kullanici, out var x, out var t);
            return Json(new { success = h == null, message = h ?? $"{x?.KrediNo} · {t?.Sira}. taksit ({t?.Vade:dd.MM.yyyy}) {t?.Durum}{(t?.OdemeTarihi != null ? " · " + t.OdemeTarihi.Value.ToString("dd.MM.yyyy") : "")}{(t?.Durum == "Kısmi Ödendi" ? " · " + P(t.OdenenTutar) : "")}.", veri = h == null ? Paket() : null });
        }
        [HttpPost("TahsilatKaydet"), IgnoreAntiforgeryToken]
        public IActionResult TahsilatKaydet([FromBody] KonkordatoTahsilat d)
        {
            var h = KonkordatoOrnek.TahsilatKaydet(d, Kullanici, out var x);
            return Json(new { success = h == null, message = h ?? $"Tahsilat {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {x?.Banka} · {x?.Kanal} {x?.CekNo} · {P(x?.Tutar)} {x?.PB}{(x?.Mahsup == true ? "" : " (mahsup edilmedi)")}.", id = x?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("TahsilatSil"), IgnoreAntiforgeryToken]
        public IActionResult TahsilatSil([FromBody] IdIstek d) { var h = KonkordatoOrnek.TahsilatSil(d?.Id ?? 0); return Json(new { success = h == null, message = h ?? "Tahsilat kaydı silindi.", veri = h == null ? Paket() : null }); }
        [HttpPost("MektupKaydet"), IgnoreAntiforgeryToken]
        public IActionResult MektupKaydet([FromBody] KonkordatoMektup d)
        {
            var h = KonkordatoOrnek.MektupKaydet(d, Kullanici, out var x);
            return Json(new { success = h == null, message = h ?? $"Teminat mektubu {(d?.Id > 0 ? "güncellendi" : "eklendi")}: {x?.Banka} · {x?.MektupNo} · {x?.Durum} {P(x?.TazminBedeli)} ₺.", id = x?.Id, veri = h == null ? Paket() : null });
        }
        [HttpPost("MektupSil"), IgnoreAntiforgeryToken]
        public IActionResult MektupSil([FromBody] IdIstek d) { var h = KonkordatoOrnek.MektupSil(d?.Id ?? 0); return Json(new { success = h == null, message = h ?? "Teminat mektubu kaydı silindi.", veri = h == null ? Paket() : null }); }
    }
}
