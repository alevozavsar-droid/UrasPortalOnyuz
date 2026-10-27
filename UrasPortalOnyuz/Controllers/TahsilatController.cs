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
    /// Tahsilat ekranı uçları (Views/Rapor117/Index.cshtml, wwwroot/js/tahsilat.js; kurallar Data/TahsilatOrnek.cs):
    /// bakiyeden (FIFO) ya da fatura bazlı tahsilat, otomatik vade farkı / kur farkı, basit fark faturası kesme. Tahsilatlar Cari Mutabakat hareketlerine işlenir.
    /// </summary>
    [Authorize]
    public class TahsilatController : Controller
    {
        private static readonly CultureInfo Tr = CariKartOrnek.Tr;
        private NaKisi Ben() => NakitAvansOrnek.KisiBul(User?.Identity?.Name) ?? NakitAvansOrnek.Personel[0];
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static string Zmn(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm", Tr) : "";
        private static string Tl(decimal v) => v.ToString("N2", Tr) + " ₺";
        public class Istek { public string Kod { get; set; } public string Tarih { get; set; } public string Yontem { get; set; } public string IslemTipi { get; set; } = "X"; public string Pb { get; set; } = "TL"; public decimal Tutar { get; set; } public decimal Kur { get; set; } = 1; public string Mod { get; set; } = "Bakiye"; public Dictionary<string, decimal> Secim { get; set; } public string Aciklama { get; set; } }
        public class FaturaIstek { public string TahsilatNo { get; set; } public string Tur { get; set; } }

        /// <summary>Müşteri carileri (bakiye, X / R, vadesi geçmiş, risk, para birimleri), parametreler, kayıtlı tahsilatlar ve fark faturaları.</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            var ben = Ben();
            var cariler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").OrderBy(k => k.Kod).Select(k =>
            {
                var r = CariKartOrnek.RiskHesapla(k); var acik = TahsilatOrnek.AcikFaturalar(k.Kod);
                return new { k.Kod, k.KisaAd, k.ResmiUnvan, k.VadeSarti, k.Sirket, temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), bakiye = r.Bakiye, bakiyeTr = Tl(r.Bakiye), bakiyeXTr = Tl(r.BakiyeX), bakiyeRTr = Tl(r.BakiyeR), vadesiGecmisTr = Tl(r.VadesiGecmis), vadesiGecmisAdet = r.VadesiGecmisAdet, maxGecikme = r.MaxGecikme, risk = r.Durum, acikAdet = acik.Count, pbler = acik.Select(f => f.ParaBirimi).Distinct().OrderBy(p => p == "TL" ? "" : p).ToArray(), acikPb = acik.GroupBy(f => f.ParaBirimi).Select(g => new { pb = g.Key, adet = g.Count(), kalan = g.Sum(f => f.Kalan), kalanTr = g.Sum(f => f.Kalan).ToString("N2", Tr) + " " + g.Key }) };
            });
            return Json(new { ben = new { ben.AdSoyad, ben.Departman, ben.Sirket }, cariler, yontemler = TahsilatOrnek.Yontemler, islemTipleri = TahsilatOrnek.IslemTipleri, kurlar = TahsilatOrnek.Kurlar, kurTarihi = Trh(MutabakatOrnek.KurTarihi), vadeFarkiAylikOran = TahsilatOrnek.VadeFarkiAylikOran, kdvOran = TahsilatOrnek.KdvOran, tahsilatlar = TahsilatOrnek.Tahsilatlar.Select(TahsilatDto), faturalar = TahsilatOrnek.Faturalar.Select(FaturaDto) });
        }
        /// <summary>Carinin açık faturaları: belge, tarih, vade, gecikme, para birimi, kalan, fatura kuru, işlem tipi.</summary>
        [HttpGet]
        public IActionResult Faturalar(string kod)
        {
            var bugun = DateTime.Today;
            return Json(new { success = true, faturalar = TahsilatOrnek.AcikFaturalar(kod ?? "").Select(f => new { f.BelgeNo, tarih = Trh(f.Tarih), vade = Trh(f.Vade ?? f.Tarih), gecikme = Math.Max(0, (bugun - (f.Vade ?? f.Tarih).Date).Days), pb = f.ParaBirimi, f.Kalan, kalanTr = f.Kalan.ToString("N2", Tr), tutar = f.Borc, faturaKuru = TahsilatOrnek.FaturaKuru(f), islemTipi = f.IslemTipi, f.Aciklama }) });
        }
        /// <summary>Dağıtım önizlemesi (kaydetmez): satır bazında tutar, gecikme, vade farkı, kur farkı ve toplamlar.</summary>
        [HttpPost]
        public IActionResult Onizle([FromBody] Istek i)
        {
            if (i == null || string.IsNullOrEmpty(i.Kod)) return Json(new { success = false, message = "Cari seçin." });
            var tarih = DateTime.TryParseExact(i.Tarih ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : DateTime.Today;
            var d = TahsilatOrnek.Hesapla(i.Kod, tarih, i.Pb ?? "TL", i.Tutar, i.Kur <= 0 ? 1 : i.Kur, i.Mod, i.Secim);
            return Json(new { success = true, dagitim = d.Select(DagitimDto), dagitilan = d.Sum(x => x.Tutar), avans = Math.Round(i.Tutar - d.Sum(x => x.Tutar), 2), vadeFarki = d.Sum(x => x.VadeFarki), kurFarki = d.Sum(x => x.KurFarki), kdvOran = TahsilatOrnek.KdvOran });
        }
        [HttpPost]
        public IActionResult Kaydet([FromBody] Istek i)
        {
            if (i == null) return Json(new { success = false, message = "Geçersiz istek." });
            var tarih = DateTime.TryParseExact(i.Tarih ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : DateTime.Today;
            var h = TahsilatOrnek.Kaydet(i.Kod, tarih, i.Yontem, i.IslemTipi, i.Pb ?? "TL", i.Tutar, i.Kur, i.Mod, i.Secim, i.Aciklama, Ben().AdSoyad, out var k);
            if (h != null) return Json(new { success = false, message = h });
            return Json(new { success = true, message = $"{k.No} kaydedildi: {k.Tutar.ToString("N2", Tr)} {k.ParaBirimi} · {k.Dagitim.Count} fatura kapatıldı{(k.Avans > 0.005m ? $", {k.Avans.ToString("N2", Tr)} {k.ParaBirimi} bakiyeye avans" : "")}{(k.VadeFarki > 0 ? $" · vade farkı {Tl(k.VadeFarki)}" : "")}{(k.KurFarki != 0 ? $" · kur farkı {Tl(k.KurFarki)}" : "")}.", tahsilat = TahsilatDto(k) });
        }
        [HttpPost]
        public IActionResult FaturaKes([FromBody] FaturaIstek i)
        {
            if (i == null) return Json(new { success = false, message = "Geçersiz istek." });
            var h = TahsilatOrnek.FaturaKes(i.TahsilatNo, i.Tur, Ben().AdSoyad, out var f);
            if (h != null) return Json(new { success = false, message = h });
            return Json(new { success = true, message = $"{f.No} kesildi: {Tl(f.Toplam)} (KDV dahil) · {f.CariAd} cari hesabına işlendi.", fatura = FaturaDto(f), tahsilat = TahsilatDto(TahsilatOrnek.TahsilatBul(i.TahsilatNo)) });
        }
        private static object DagitimDto(TahsilatDagitim d) => new { d.BelgeNo, faturaTarihi = Trh(d.FaturaTarihi), vade = Trh(d.Vade), d.Pb, d.IslemTipi, d.Kalan, kalanTr = d.Kalan.ToString("N2", Tr), d.Tutar, tutarTr = d.Tutar.ToString("N2", Tr), tutarTl = d.TutarTl, tutarTlTr = Tl(d.TutarTl), d.FaturaKuru, d.TahsilatKuru, d.GecikmeGun, d.VadeFarki, vadeFarkiTr = Tl(d.VadeFarki), d.KurFarki, kurFarkiTr = Tl(d.KurFarki) };
        /// <summary>Konsolide Tahsilat Takip (Rapor17): şirket bazında müşteri carilerinin açık fatura vade kırılımı (gecikmiş, bu hafta, 8–30, 31–60, 60+ gün), içerideki çek / senet, son 30 gün tahsilatı, son tahsilat, risk ve temsilci; haftalık beklenen tahsilat planı için fatura vadeleri.</summary>
        [HttpGet]
        public IActionResult KonsolideTakip()
        {
            var bugun = DateTime.Today; var son30 = bugun.AddDays(-30);
            var satirlar = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").Select(k =>
            {
                var c = MutabakatOrnek.Bul(k.Kod); var r = CariKartOrnek.RiskHesapla(k);
                decimal KurTl(string pb) => pb == "TL" || pb == "TRY" || string.IsNullOrEmpty(pb) ? 1m : (MutabakatOrnek.Kurlar.TryGetValue(pb, out var x) ? x : 1m);
                var acik = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Kalan > 0.005m).ToList();
                var kalemler = acik.Select(h => new { vade = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), gun = ((h.Vade ?? h.Tarih).Date - bugun).Days, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), belge = h.BelgeNo, pb = h.ParaBirimi, kalan = h.Kalan, islemTipi = h.IslemTipi }).OrderBy(x => x.vade).ToList();
                decimal Kova(int a, int b) => kalemler.Where(x => x.gun >= a && x.gun <= b).Sum(x => x.tl);
                var tahs = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.Alacak > 0).ToList();
                var sonTahsilat = tahs.OrderByDescending(h => h.Tarih).FirstOrDefault();
                return new { k.Kod, k.KisaAd, k.ResmiUnvan, k.Sirket, sirketDb = CekAkibetOrnek.DbName(k.Sirket), k.Bolge, k.Sinif, k.VadeSarti, temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), risk = r.Durum, riskNeden = r.Neden, bakiye = r.AcikBakiye, bakiyeX = r.BakiyeX, bakiyeR = r.BakiyeR,
                    gecmis = Kova(int.MinValue, -1), gecmisAdet = kalemler.Count(x => x.gun < 0), maxGecikme = r.MaxGecikme, buHafta = Kova(0, 7), g8_30 = Kova(8, 30), g31_60 = Kova(31, 60), g60 = Kova(61, int.MaxValue), acikToplam = kalemler.Sum(x => x.tl), acikAdet = kalemler.Count,
                    cek = r.Cek, cekAdet = r.CekAdet, son30Tahsilat = Math.Round(tahs.Where(h => h.Tarih >= son30).Sum(h => h.Alacak * KurTl(h.ParaBirimi)), 2), son30Adet = tahs.Count(h => h.Tarih >= son30), sonTahsilat = sonTahsilat == null ? "" : Trh(sonTahsilat.Tarih), sonTahsilatTl = sonTahsilat == null ? 0 : Math.Round(sonTahsilat.Alacak * KurTl(sonTahsilat.ParaBirimi), 2),
                    limitDurum = CariKartOrnek.LimitHesapla(k).Durum, kalemler };
            }).ToList();
            return Json(new { success = true, bugun = bugun.ToString("yyyy-MM-dd"), sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, db = s.DbName }), satirlar, tahsilatlar = TahsilatOrnek.Tahsilatlar.Select(TahsilatDto) });
        }
        private static object TahsilatDto(TahsilatKaydi t) => t == null ? null : new { t.No, tarih = Trh(t.Tarih), t.CariKodu, t.CariAd, t.Sirket, sirketDb = CekAkibetOrnek.DbName(t.Sirket), panelSekme = t.Yontem == "Havale / EFT" || t.Yontem == "Kredi Kartı" ? "banka" : t.Yontem == "Nakit" ? "kasa" : "cek", t.Yontem, t.ParaBirimi, t.Tutar, tutarTr = t.Tutar.ToString("N2", Tr) + " " + t.ParaBirimi, t.Kur, tutarTl = t.TutarTl, tutarTlTr = Tl(t.TutarTl), t.IslemTipi, t.Mod, t.Aciklama, t.BelgeNo, dagitim = t.Dagitim.Select(DagitimDto), dagitilan = t.Dagitilan, avans = t.Avans, avansTr = t.Avans.ToString("N2", Tr) + " " + t.ParaBirimi, kurFarki = t.KurFarki, kurFarkiTr = Tl(t.KurFarki), vadeFarki = t.VadeFarki, vadeFarkiTr = Tl(t.VadeFarki), t.KurFarkiFaturaNo, t.VadeFarkiFaturaNo, t.Kullanici, zaman = Zmn(t.Zaman) };
        private static object FaturaDto(FarkFaturasi f) => new { f.No, tarih = Trh(f.Tarih), vade = Trh(f.Vade), f.CariKodu, f.CariAd, f.CariUnvan, f.VergiNo, f.VergiDairesi, f.Tur, f.TahsilatNo, f.IslemTipi, kalemler = f.Kalemler.Select(k => new { aciklama = k.Aciklama, tutar = k.Tutar, tutarTr = Tl(k.Tutar) }), matrah = f.Matrah, matrahTr = Tl(f.Matrah), f.KdvOran, kdv = f.Kdv, kdvTr = Tl(f.Kdv), toplam = f.Toplam, toplamTr = Tl(f.Toplam), f.Kullanici, zaman = Zmn(f.Zaman), f.Durum };
    }
}
