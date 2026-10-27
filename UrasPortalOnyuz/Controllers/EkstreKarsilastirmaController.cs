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
    /// Cari Ekstre / Hesap Ekstresi Mutabakatı uçları (Views/Rapor141/Index.cshtml, wwwroot/js/ekstre-karsilastirma.js; kurallar Data/EkstreKarsilastirmaOrnek.cs):
    /// cari bazında cari ekstre ↔ muhasebe hesap ekstresi bakiye karşılaştırması, satır satır eşleştirme, fark nedeni ve düzeltme fişi.
    /// </summary>
    [Authorize]
    public class EkstreKarsilastirmaController : Controller
    {
        private static readonly CultureInfo Tr = CariKartOrnek.Tr;
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        public class FisIstek { public string Kod { get; set; } public string Belge { get; set; } public string Tur { get; set; } public decimal Tutar { get; set; } public string Aciklama { get; set; } }
        static (DateTime bas, DateTime bit) Aralik(string bas, string bit)
        {
            var bugun = DateTime.Today; DateTime b1 = DateTime.TryParseExact(bas ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var x) ? x : new DateTime(bugun.Year, 1, 1), b2 = DateTime.TryParseExact(bit ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var y) ? y : bugun;
            return (b1, b2);
        }
        static object SatirDto(EkSatir s) => s == null ? null : new { s.Taraf, tarih = s.Tarih.ToString("dd.MM.yyyy", Tr), tarihIso = s.Tarih.ToString("yyyy-MM-dd"), s.Belge, s.Tip, s.Aciklama, s.Pb, s.Borc, s.Alacak, s.Fis, s.IslemTipi };
        static object DuzeltmeDto(EkDuzeltme d) => new { d.Id, zaman = d.Zaman.ToString("dd.MM.yyyy HH:mm", Tr), d.CariKodu, d.CariAd, d.FisNo, d.Tur, d.Aciklama, d.Tutar, tutarTr = d.Tutar.ToString("N2", Tr) + " ₺", d.Kullanici };

        /// <summary>Cari listesi: cari ekstre ↔ hesap ekstresi borç / alacak / bakiye, fark, durum (Uyuşuyor · Fark Var · Hesap Tanımsız), fark sayısı ve nedenleri.</summary>
        [HttpGet]
        public IActionResult Veri(string bas, string bit)
        {
            var (b1, b2) = Aralik(bas, bit); var l = new List<object>();
            lock (MutabakatOrnek.Kilit)
                foreach (var c in MutabakatOrnek.Cariler)
                {
                    var k = c.Kart; var cari = EkstreKarsilastirmaOrnek.CariSatirlari(c, b1, b2); bool tanimsiz = EkstreKarsilastirmaOrnek.HesapTanimsiz(c.CariKodu);
                    var hesap = tanimsiz ? new List<EkSatir>() : EkstreKarsilastirmaOrnek.HesapSatirlari(c, cari); var es = tanimsiz ? new List<EkEslesme>() : EkstreKarsilastirmaOrnek.Eslestir(cari, hesap);
                    decimal cb = cari.Sum(s => s.Borc), ca = cari.Sum(s => s.Alacak), hb = hesap.Sum(s => s.Borc), ha = hesap.Sum(s => s.Alacak); var farklar = es.Where(e => e.Durum != "Eşleşti").ToList();
                    l.Add(new { kod = c.CariKodu, kisaAd = k?.KisaAd ?? c.CariKodu, resmiUnvan = k?.ResmiUnvan ?? "", tip = k?.KartTipi ?? "", sirket = k?.Sirket ?? "", hesapKodu = tanimsiz ? "" : EkstreKarsilastirmaOrnek.HesapKodu(k), cariBorc = cb, cariAlacak = ca, cariBakiye = cb - ca, hesapBorc = hb, hesapAlacak = ha, hesapBakiye = hb - ha, fark = tanimsiz ? 0 : Math.Round((cb - ca) - (hb - ha), 2), satir = cari.Count, hesapSatir = hesap.Count,
                        durum = tanimsiz ? "Hesap Tanımsız" : farklar.Count == 0 ? "Uyuşuyor" : "Fark Var", farkAdet = farklar.Count, nedenler = farklar.GroupBy(f => f.Neden).Select(g => $"{g.Key} ({g.Count()})").ToArray(), tipler = farklar.GroupBy(f => f.Durum).Select(g => new { durum = g.Key, adet = g.Count() }) });
                }
            return Json(new { success = true, bas = b1.ToString("yyyy-MM-dd"), bit = b2.ToString("yyyy-MM-dd"), cariler = l, duzeltmeler = EkstreKarsilastirmaOrnek.Duzeltmeler.Select(DuzeltmeDto) });
        }
        /// <summary>Satır satır karşılaştırma: her satırda cari tarafı, hesap tarafı, durum, neden, öneri, fark.</summary>
        [HttpGet]
        public IActionResult Detay(string kod, string bas, string bit)
        {
            var c = MutabakatOrnek.Bul(kod ?? ""); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." }); var (b1, b2) = Aralik(bas, bit);
            lock (MutabakatOrnek.Kilit)
            {
                var cari = EkstreKarsilastirmaOrnek.CariSatirlari(c, b1, b2); bool tanimsiz = EkstreKarsilastirmaOrnek.HesapTanimsiz(c.CariKodu); var hesap = tanimsiz ? new List<EkSatir>() : EkstreKarsilastirmaOrnek.HesapSatirlari(c, cari); var es = tanimsiz ? new List<EkEslesme>() : EkstreKarsilastirmaOrnek.Eslestir(cari, hesap);
                return Json(new { success = true, kod = c.CariKodu, kisaAd = c.Kart?.KisaAd, hesapKodu = tanimsiz ? "" : EkstreKarsilastirmaOrnek.HesapKodu(c.Kart), tanimsiz, cariBakiye = cari.Sum(s => s.Borc - s.Alacak), hesapBakiye = hesap.Sum(s => s.Borc - s.Alacak), satirlar = es.Select(e => new { cari = SatirDto(e.Cari), hesap = SatirDto(e.Hesap), e.Durum, e.Neden, e.Oneri, e.Fark }), ozet = es.GroupBy(e => e.Durum).Select(g => new { durum = g.Key, adet = g.Count(), fark = Math.Round(g.Sum(e => e.Fark), 2) }), duzeltmeler = EkstreKarsilastirmaOrnek.Duzeltmeler.Where(d => d.CariKodu == c.CariKodu).Select(DuzeltmeDto) });
            }
        }
        /// <summary>Düzeltme fişi oluştur (simülasyon): fark giderilmiş sayılır.</summary>
        [HttpPost]
        public IActionResult DuzeltmeFisi([FromBody] FisIstek i)
        {
            var h = EkstreKarsilastirmaOrnek.DuzeltmeFisi(i?.Kod, i?.Belge, i?.Tur, i?.Tutar ?? 0, i?.Aciklama ?? "", Ben(), out var d); if (h != null) return Json(new { success = false, message = h });
            return Json(new { success = true, message = $"{d.FisNo} oluşturuldu: {d.Tur} · {d.Aciklama} · {d.Tutar.ToString("N2", Tr)} ₺.", duzeltme = DuzeltmeDto(d) });
        }
    }
}
