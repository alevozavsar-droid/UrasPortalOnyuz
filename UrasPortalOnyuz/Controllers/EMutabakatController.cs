using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// E-Mutabakat uçları (Views/Rapor59/Index.cshtml, wwwroot/js/e-mutabakat.js; kurallar Data/EMutabakatOrnek.cs):
    /// kayıtlı iletişim bilgileriyle e-posta / WhatsApp üzerinden otomatik mutabakat — önizleme, tekil / toplu gönderim, hatırlatma, yanıt işleme ve senkron, şablon, iletişim bilgisi düzeltme.
    /// </summary>
    [Authorize]
    public class EMutabakatController : Controller
    {
        private static readonly CultureInfo Tr = CariKartOrnek.Tr;
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        private static string Zmn(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm", Tr) : "";
        public class GonderIstek { public List<string> Kodlar { get; set; } public string Kanal { get; set; } = "E-posta"; }
        public class YanitIstek { public string Kod { get; set; } public int GonderimId { get; set; } public string Sonuc { get; set; } public string Aciklama { get; set; } }
        public class KisiIstek { public string Kod { get; set; } public int KisiId { get; set; } public string Eposta { get; set; } public string Cep { get; set; } public bool? AnaYetkili { get; set; } }
        public class KodIstek { public string Kod { get; set; } }

        static object GonderimDto(MtGonderim g, string kod = null) => new { g.Id, kod, g.BelgeNo, tarih = Zmn(g.Tarih), tarihIso = g.Tarih.ToString("yyyy-MM-dd"), g.Kanal, g.Kime, g.Ozet, g.TlKarsiligi, tlTr = g.TlKarsiligi.ToString("N2", Tr) + " ₺", g.Sonuc, yanitTarihi = Zmn(g.YanitTarihi), g.YanitAciklama, g.Gonderen, g.Tekrar, gun = (DateTime.Today - g.Tarih.Date).Days };
        static object CariDto(MtCari c)
        {
            var k = c.Kart; var ana = EMutabakatOrnek.AnaYetkili(c); var son = EMutabakatOrnek.Son(c);
            return new { kod = c.CariKodu, kisaAd = k?.KisaAd ?? c.CariKodu, resmiUnvan = k?.ResmiUnvan ?? "", tip = k?.KartTipi ?? "", sirket = k?.Sirket ?? "", bolge = k?.Bolge ?? "", temsilci = string.Join(", ", c.Temsilciler), bakiyeTl = c.BakiyeTl, bakiyeTr = c.BakiyeTl.ToString("N2", Tr) + " ₺", dovizler = c.Dovizler.Where(d => d.Net != 0).Select(d => new { pb = d.ParaBirimi, net = d.Net, netTr = d.Net.ToString("N2", Tr) + " " + d.ParaBirimi }), acikFatura = c.Dovizler.Sum(d => d.AcikFaturaAdet), aktif = c.Aktif,
                yetkili = ana == null ? null : new { ana.Id, ana.AdSoyad, ana.Unvan, ana.Eposta, cep = EMutabakatOrnek.Cep(ana), wa = EMutabakatOrnek.WaNumara(EMutabakatOrnek.Cep(ana)), ana.AnaYetkili }, kisiler = c.Kisiler.Select(x => new { x.Id, x.AdSoyad, x.Unvan, x.Departman, x.Eposta, cep = EMutabakatOrnek.Cep(x), x.AnaYetkili, x.MutabakatYetkilisi }),
                mail = EMutabakatOrnek.MailUygun(c), wa = EMutabakatOrnek.WaUygun(c), durum = EMutabakatOrnek.Durum(c), hatirlat = EMutabakatOrnek.HatirlatmaZamani(c), resmiDurum = c.Durum, son = son == null ? null : GonderimDto(son), gonderim = EMutabakatOrnek.Gonderimler(c).Count, sonMutabik = c.Gonderimler.Where(g => g.Sonuc == "Mutabık").Select(g => (DateTime?)(g.YanitTarihi ?? g.Tarih)).Max()?.ToString("dd.MM.yyyy", Tr) ?? "" };
        }
        /// <summary>Tüm cariler (iletişim bilgisi, kanal uygunluğu, bakiye, e-mutabakat durumu, son gönderim), gönderim listesi, şablon, parametreler.</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            EMutabakatOrnek.Hazirla(); List<object> cariler, gonderimler;
            lock (MutabakatOrnek.Kilit) { var cs = MutabakatOrnek.Cariler; cariler = cs.Select(CariDto).ToList(); gonderimler = cs.SelectMany(c => EMutabakatOrnek.Gonderimler(c).Select(g => (g.Id, dto: GonderimDto(g, c.CariKodu)))).OrderByDescending(x => x.Id).Select(x => x.dto).ToList(); }
            return Json(new { success = true, ben = Ben(), bugun = DateTime.Today.ToString("dd.MM.yyyy"), hatirlatmaGunu = EMutabakatOrnek.HatirlatmaGunu, yanitAdresi = EMutabakatOrnek.YanitAdresi, sablon = EMutabakatOrnek.Sablon, alanlar = EMutabakatOrnek.Alanlar, cariler, gonderimler, log = MutabakatOrnek.GenelLog.Where(l => l.Islem.Contains("E-Mutabakat") || l.Islem.Contains("İletişim Bilgisi")).Take(60).Select(l => new { zaman = Zmn(l.Zaman), l.Kullanici, l.Islem, l.Yeni, l.Aciklama }) });
        }
        /// <summary>Mesaj önizlemesi (e-posta konu + gövde, WhatsApp gövdesi; mailto / wa.me bağlantıları).</summary>
        [HttpGet]
        public IActionResult Onizle(string kod, bool hatirlatma = false)
        {
            var c = MutabakatOrnek.Bul(kod ?? ""); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." });
            var ana = EMutabakatOrnek.AnaYetkili(c); var m = EMutabakatOrnek.Metin(c, "E-posta", Ben(), hatirlatma); var w = EMutabakatOrnek.Metin(c, "WhatsApp", Ben(), hatirlatma);
            return Json(new { success = true, cari = CariDto(c), mail = new { konu = m.Konu, govde = m.Govde, kime = ana?.Eposta ?? "", uygun = EMutabakatOrnek.MailUygun(c), mailto = ana == null || string.IsNullOrEmpty(ana.Eposta) ? "" : $"mailto:{ana.Eposta}?subject={Uri.EscapeDataString(m.Konu)}&body={Uri.EscapeDataString(m.Govde)}", ek = $"CariEkstre_{c.CariKodu}_{DateTime.Today:yyyyMMdd}.pdf" }, wa = new { govde = w.Govde, kime = EMutabakatOrnek.Cep(ana), uygun = EMutabakatOrnek.WaUygun(c), link = EMutabakatOrnek.WaUygun(c) ? $"https://wa.me/{EMutabakatOrnek.WaNumara(EMutabakatOrnek.Cep(ana))}?text={Uri.EscapeDataString(w.Govde)}" : "" } });
        }
        [HttpPost]
        public IActionResult Gonder([FromBody] GonderIstek i)
        {
            if (i == null || i.Kodlar == null || i.Kodlar.Count == 0) return Json(new { success = false, message = "Cari seçin." });
            var r = EMutabakatOrnek.Gonder(i.Kodlar, i.Kanal ?? "E-posta", Ben());
            return Json(new { success = r.Basarili > 0, message = (r.Basarili > 0 ? $"{r.Basarili} cariye {r.Gidenler.Count} mesaj gönderildi ({string.Join(", ", r.Gidenler.GroupBy(g => g.Kanal).Select(g => $"{g.Count()} {g.Key}"))})." : "Gönderim yapılamadı.") + (r.Hatalar.Count > 0 ? " Atlanan: " + string.Join(" · ", r.Hatalar) : ""), hatalar = r.Hatalar, gidenler = r.Gidenler.Select(g => GonderimDto(g)) });
        }
        [HttpPost]
        public IActionResult Hatirlat([FromBody] KodIstek i) { var h = EMutabakatOrnek.Hatirlat(i?.Kod, Ben()); return Json(new { success = h == null, message = h ?? "Hatırlatma gönderildi." }); }
        [HttpPost]
        public IActionResult Yanit([FromBody] YanitIstek i) { var h = EMutabakatOrnek.Yanit(i?.Kod, i?.GonderimId ?? 0, i?.Sonuc, i?.Aciklama, Ben()); return Json(new { success = h == null, message = h ?? $"Yanıt işlendi: {i.Sonuc}." }); }
        /// <summary>Yanıt bağlantısından gelen cevapları çek (simülasyon).</summary>
        [HttpPost]
        public IActionResult YanitlariCek() { var r = EMutabakatOrnek.YanitlariCek(Ben()); return Json(new { success = true, message = r.Mutabik + r.Itirazli == 0 ? "Yeni yanıt yok." : $"{r.Mutabik} mutabık, {r.Itirazli} itirazlı yanıt alındı." }); }
        [HttpPost]
        public IActionResult KisiGuncelle([FromBody] KisiIstek i) { var h = EMutabakatOrnek.KisiGuncelle(i?.Kod, i?.KisiId ?? 0, i?.Eposta, i?.Cep, i?.AnaYetkili, Ben()); return Json(new { success = h == null, message = h ?? "İletişim bilgisi güncellendi." }); }
        [HttpPost]
        public IActionResult SablonKaydet([FromBody] EmSablon s) { var h = EMutabakatOrnek.SablonKaydet(s, Ben()); return Json(new { success = h == null, message = h ?? "Şablon kaydedildi." }); }
    }
}
