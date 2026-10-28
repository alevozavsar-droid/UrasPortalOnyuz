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
    /// Adat Hesaplama uçları (Views/Rapor21/Index.cshtml, wwwroot/js/adat.js; taslaklar Data/AdatOrnek.cs): cari listesi (ortak süzgeç), açık faturalar + vade bekleyen çekler + taslak ödeme satırları,
    /// taslak kaydet, Tahsilata Aktar (taslak tahsilat), otomatik kapat, PDF / Excel (istemcide hesaplanan simülasyon). Hesap istemcide canlıdır (FIFO, aylık adat, vade farkı).
    /// </summary>
    [Authorize]
    public class AdatController : Controller
    {
        private static readonly CultureInfo Tr = CariKartOrnek.Tr;
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static string N2(decimal v) => v.ToString("N2", Tr);
        private static decimal KurTl(string pb) => TahsilatOrnek.TlMi(pb) ? 1m : (MutabakatOrnek.Kurlar.TryGetValue(pb, out var x) ? x : 1m);
        public class SatirIstek { public string Tip { get; set; } public string Tarih { get; set; } public string No { get; set; } public string Banka { get; set; } public decimal Tutar { get; set; } public bool Secili { get; set; } = true; }
        public class TaslakIstek { public string Kod { get; set; } public decimal Oran { get; set; } public bool DinamikVade { get; set; } public List<SatirIstek> Satirlar { get; set; } public Dictionary<string, decimal> FaturaSecim { get; set; } public string IslemTipi { get; set; } = "X"; }
        public class OzetIstek { public string Etiket { get; set; } public string Deger { get; set; } }
        public class RaporIstek { public string Kod { get; set; } public decimal Oran { get; set; } public string Tur { get; set; } public List<OzetIstek> Ozet { get; set; } public List<string[]> Faturalar { get; set; } public List<string[]> Odemeler { get; set; } public List<string[]> Aylik { get; set; } public List<string[]> Simulasyon { get; set; } public string[] AylikToplam { get; set; } public string[] SimToplam { get; set; } }
        static DateTime TarihCoz(string s, DateTime v) => DateTime.TryParseExact(s ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : v;
        static List<AdatSatir> Satirlar(List<SatirIstek> l) => (l ?? new List<SatirIstek>()).Select(s => new AdatSatir { Tip = s.Tip, Tarih = TarihCoz(s.Tarih, DateTime.Today), No = s.No, Banka = s.Banka, Tutar = s.Tutar, Secili = s.Secili }).ToList();

        [HttpGet]
        public IActionResult Veri()
        {
            var cariler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" || k.KartTipi == "Tedarikçi").OrderBy(k => k.Kod).Select(k =>
            {
                var r = CariKartOrnek.RiskHesapla(k); var c = MutabakatOrnek.Bul(k.Kod); bool ted = k.KartTipi == "Tedarikçi";
                var acik = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && (ted ? h.Alacak : h.Borc) > 0 && h.Kalan > 0.005m).ToList();
                return new { suzgec = CariSuzgec.Alanlar(k, r), k.Kod, k.KisaAd, k.ResmiUnvan, k.KartTipi, k.Sirket, k.VadeSarti, vadeGun = NakitAkisOrnek.VadeGunu(k.VadeSarti), bakiye = r.Bakiye, acikAdet = acik.Count, acikTl = Math.Round(acik.Sum(f => f.Kalan * KurTl(f.ParaBirimi)), 2), vadesiGecmis = r.VadesiGecmis, maxGecikme = r.MaxGecikme, risk = r.Durum, taslak = AdatOrnek.Taslak(k.Kod) != null };
            });
            return Json(new { success = true, ben = Ben(), bugun = DateTime.Today.ToString("yyyy-MM-dd"), oran = AdatOrnek.VarsayilanOran, tipler = AdatOrnek.Tipler, bankalar = TahsilatOrnek.Bankalar, kurlar = MutabakatOrnek.Kurlar, secenekler = CariSuzgec.Secenekler(), cariler });
        }
        /// <summary>Carinin açık faturaları (kalan, TL, dinamik vade), vade bekleyen çek / senetleri (ödeme satırı olarak), taslak ödeme satırları, oran, cari bilgisi.</summary>
        [HttpGet]
        public IActionResult Detay(string kod)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); var c = MutabakatOrnek.Bul(kod ?? ""); if (k == null || c == null) return Json(new { success = false, message = "Cari bulunamadı." });
            bool ted = k.KartTipi == "Tedarikçi"; var bugun = DateTime.Today; int vadeGun = NakitAkisOrnek.VadeGunu(k.VadeSarti); var t = AdatOrnek.Taslak(k.Kod); var b = TahsilatOrnek.CariBilgi(k.Kod); var r = CariKartOrnek.RiskHesapla(k);
            var faturalar = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && (ted ? h.Alacak : h.Borc) > 0 && h.Kalan > 0.005m).OrderBy(h => h.Vade ?? h.Tarih).Select(h => new { h.BelgeNo, tarih = Trh(h.Tarih), tarihIso = h.Tarih.ToString("yyyy-MM-dd"), vade = Trh(h.Vade ?? h.Tarih), vadeIso = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), dinamikVadeIso = h.Tarih.AddDays(vadeGun).ToString("yyyy-MM-dd"), pb = h.ParaBirimi, kur = KurTl(h.ParaBirimi), tutar = ted ? h.Alacak : h.Borc, h.Kalan, kalanTl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), gun = (bugun - (h.Vade ?? h.Tarih).Date).Days, h.IslemTipi, h.KapamaDurumu, h.Aciklama });
            var cekler = c.Hareketler.Where(h => (h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.KapamaDurumu == "Vade Bekliyor").OrderBy(h => h.Vade).Select(h => new { tip = h.BelgeTipi, tarihIso = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), no = h.BelgeNo, banka = "", tutar = Math.Round((ted ? h.Borc : h.Alacak) * KurTl(h.ParaBirimi), 2), pb = h.ParaBirimi, kaynak = "Portföy", secili = true });
            var eslesmemis = c.Hareketler.Where(h => (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Ödeme") && h.Kalan > 0.005m).Select(h => new { tip = "Havale / EFT", tarihIso = h.Tarih.ToString("yyyy-MM-dd"), no = h.BelgeNo, banka = "", tutar = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), pb = h.ParaBirimi, kaynak = "Eşleşmemiş Ödeme", secili = true });
            return Json(new { success = true, kod = k.Kod, kisaAd = k.KisaAd, resmiUnvan = k.ResmiUnvan, kartTipi = k.KartTipi, tedarikci = ted, sirket = k.Sirket, vadeSarti = k.VadeSarti, vadeGun, bakiyeTl = c.BakiyeTl, risk = r.Durum, riskNeden = r.Neden, vadesiGecmis = r.VadesiGecmis, vadesiGecmisAdet = r.VadesiGecmisAdet, maxGecikme = r.MaxGecikme,
                bilgi = new { sonOdeme = Trh(b.SonOdeme), sonOdemeTr = b.SonOdeme.HasValue ? $"{N2(b.SonOdemeTutar)} {b.SonOdemePb}" : "", sonOdemeGun = b.SonOdeme.HasValue ? (bugun - b.SonOdeme.Value.Date).Days : (int?)null, sonYukleme = Trh(b.SonYukleme), sonYuklemeTr = b.SonYukleme.HasValue ? $"{N2(b.SonYuklemeTutar)} {b.SonYuklemePb}" : "", sonYuklemeGun = b.SonYukleme.HasValue ? (bugun - b.SonYukleme.Value.Date).Days : (int?)null, b.OrtalamaGecikme, b.EslesmemisOdeme, eslesmemisOdemeTr = N2(b.EslesmemisOdemeTl) + " ₺", b.VadeBekleyenCek, vadeBekleyenCekTr = N2(b.VadeBekleyenCekTl) + " ₺" },
                faturalar, mevcutOdemeler = cekler.Concat(eslesmemis), taslak = t == null ? null : new { t.Oran, t.DinamikVade, t.Kullanici, zaman = t.Zaman.ToString("dd.MM.yyyy HH:mm", Tr), satirlar = t.Satirlar.Select(s => new { tip = s.Tip, tarihIso = s.Tarih.ToString("yyyy-MM-dd"), no = s.No, banka = s.Banka, tutar = s.Tutar, pb = "TL", kaynak = "Taslak", secili = s.Secili }) }, oran = t?.Oran ?? AdatOrnek.VarsayilanOran, kapatilabilir = KapamaOrnek.Oner(c).Count });
        }
        [HttpPost]
        public IActionResult TaslakKaydet([FromBody] TaslakIstek i) { if (i == null || string.IsNullOrEmpty(i.Kod)) return Json(new { success = false, message = "Cari seçin." }); AdatOrnek.TaslakKaydet(i.Kod, i.Oran, i.DinamikVade, Satirlar(i.Satirlar), Ben()); return Json(new { success = true, message = $"{i.Kod} adat taslağı kaydedildi ({(i.Satirlar ?? new List<SatirIstek>()).Count(s => s.Tutar > 0)} ödeme satırı, oran %{i.Oran.ToString("0.##", Tr)}); cariye girince otomatik yüklenir." }); }
        [HttpPost]
        public IActionResult TaslakSil([FromBody] TaslakIstek i) { AdatOrnek.TaslakSil(i?.Kod); return Json(new { success = true, message = "Adat taslağı silindi." }); }
        /// <summary>Girilen ödeme satırlarını Tahsilat ekranına TASLAK tahsilat olarak aktarır (müşteri carisi; işleme alma Tahsilat ekranından).</summary>
        [HttpPost]
        public IActionResult TahsilataAktar([FromBody] TaslakIstek i)
        {
            if (i == null || string.IsNullOrEmpty(i.Kod)) return Json(new { success = false, message = "Cari seçin." });
            var sat = Satirlar(i.Satirlar).Where(s => s.Secili && s.Tutar > 0).Select((s, n) => new TahsilatSatir { Sira = n + 1, Yontem = s.Tip == "Nakit" ? "Nakit" : s.Tip == "Çek" ? "Çek" : s.Tip == "Senet" ? "Senet" : "Havale / EFT", Tarih = s.Tarih, BelgeNo = s.No, Banka = s.Banka, Pb = "TL", Kur = 1, Tutar = s.Tutar }).ToList();
            if (sat.Count == 0) return Json(new { success = false, message = "Aktarılacak (seçili, tutarlı) ödeme satırı yok." });
            var h = TahsilatOrnek.Kaydet(i.Kod, DateTime.Today, i.IslemTipi ?? "X", "TL", i.FaturaSecim != null && i.FaturaSecim.Count > 0 ? "Manuel" : "Otomatik", i.FaturaSecim, sat, "Adat Hesaplama'dan aktarıldı", Ben(), false, out var k);
            if (h != null) return Json(new { success = false, message = h });
            return Json(new { success = true, message = $"{k.No} taslak tahsilat oluşturuldu ({N2(k.Tutar)} TL, {k.Yontem}); Tahsilat ekranından İşleme Al.", no = k.No, link = "/Rapor117?no=" + Uri.EscapeDataString(k.No) });
        }
        [HttpPost]
        public IActionResult OtomatikKapat([FromBody] TaslakIstek i) { var r = KapamaOrnek.TumunuKapat(new[] { i?.Kod ?? "" }, Ben()); return Json(new { success = true, message = r.Eslesme == 0 ? "Bu caride kapatılacak eşleşmemiş ödeme yok." : $"{r.Eslesme} fatura–ödeme eşleşmesi kapatıldı · {N2(r.ToplamTl)} ₺.", eslesme = r.Eslesme }); }

        DaBelge Belge(RaporIstek i)
        {
            var k = CariKartOrnek.KartBul(i.Kod) ?? throw new Exception("Cari bulunamadı."); var tur = i.Tur ?? "ALL";
            var b = new DaBelge { Baslik = tur == "PART1" ? "Adat Raporu · Faturalar" : tur == "PART2" ? "Adat Raporu · Tahsilat Simülasyonu" : "Adat Hesaplama Raporu", AltBaslik = $"{k.Kod} · {k.ResmiUnvan} · {k.Sirket} · aylık vade farkı oranı %{i.Oran.ToString("0.##", Tr)}", Sirket = k.Sirket, Hazirlayan = Ben() };
            foreach (var o in i.Ozet ?? new List<OzetIstek>()) b.Ozet.Add((o.Etiket ?? "", o.Deger ?? ""));
            if (tur != "PART2") { b.Bloklar.Add(new DaBlok { Baslik = "Aylık Adat Dağılımı", Kolonlar = new List<string> { "Vade Dönemi", "Ortalama Vade", "Fatura Tutarı (TL)", "Net Gecikme (gün)", "Vade Farkı Payı (TL)" }, Sag = new List<bool> { false, false, true, true, true }, Satirlar = i.Aylik ?? new List<string[]>(), Toplam = i.AylikToplam }); b.Bloklar.Add(new DaBlok { Baslik = "Açık Faturalar", Kolonlar = new List<string> { "Seçili", "Belge No", "Fatura Tarihi", "Vade", "PB", "Hesap Tutarı (TL)", "Orijinal Kalan", "Gün" }, Sag = new List<bool> { false, false, false, false, false, true, true, true }, Satirlar = i.Faturalar ?? new List<string[]>() }); }
            if (tur != "PART1") { b.Bloklar.Add(new DaBlok { Baslik = "Ödemeler / Çekler", Kolonlar = new List<string> { "Tip", "Vade", "No", "Banka", "Kaynak", "Tutar (TL)" }, Sag = new List<bool> { false, false, false, false, false, true }, Satirlar = i.Odemeler ?? new List<string[]>() }); b.Bloklar.Add(new DaBlok { Baslik = "Ödeme Kapanış Simülasyonu (FIFO)", Kolonlar = new List<string> { "Belge No", "Ödeme", "Ödeme Tarihi", "Vade Tarihi", "Gün", "Ödenen (Kapanan)", "Kalan Bakiye", "VF Tutarı" }, Sag = new List<bool> { false, false, false, false, true, true, true, true }, Satirlar = i.Simulasyon ?? new List<string[]>(), Toplam = i.SimToplam, Not = "Gün = ödeme tarihi − fatura vadesi; kapanmayan faturada bugün − vade (vadesi gelmemişse 0). VF = kapanan × aylık oran × gün ÷ 3000. Negatif VF lehte." }); }
            return b;
        }
        FileResult Dosya(byte[] b, string ad, bool pdf) => File(b, pdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad + (pdf ? ".pdf" : ".xlsx"));
        [HttpPost] public IActionResult Pdf([FromBody] RaporIstek i) => Dosya(DisaAktar.Pdf(Belge(i)), $"Adat_{i.Kod}_{DateTime.Today:yyyyMMdd}", true);
        [HttpPost] public IActionResult Excel([FromBody] RaporIstek i) => Dosya(DisaAktar.Excel(Belge(i)), $"Adat_{i.Kod}_{DateTime.Today:yyyyMMdd}", false);
    }
}
