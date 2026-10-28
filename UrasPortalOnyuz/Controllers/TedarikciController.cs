using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;
using FinansRaporlama.Controllers;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Tedarikçi raporları uçları: Tedarikçi Fatura ve Ödeme Takip (Rapor52; Views/Rapor52/Index.cshtml, js/tedarikci-takip.js) ve Tedarikçi Konsolide Ödeme Takip (Rapor53; js/tedarikci-konsolide.js).
    /// Veri: tedarikçi cari kartları (ortak süzgeç), Cari Mutabakat alış faturaları / ödemeleri / verilen çekler, Kasa Nakit Akışı planlı ödemeleri (Data/NakitAkisOrnek.cs; tedarikçi adıyla eşlenir), kredi limiti ve risk.
    /// </summary>
    [Authorize]
    public class TedarikciController : Controller
    {
        private static readonly CultureInfo Tr = CariKartOrnek.Tr;
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static string N2(decimal v) => v.ToString("N2", Tr);
        private static decimal KurTl(string pb) => TahsilatOrnek.TlMi(pb) ? 1m : (MutabakatOrnek.Kurlar.TryGetValue(pb, out var x) ? x : 1m);
        static List<CariKart> Tedarikciler() => CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Tedarikçi").OrderBy(k => k.Kod).ToList();
        /// <summary>Kasa Nakit Akışı'ndaki (tüm grup şirketleri) ödenmemiş / ödenmiş planlı ödemeler; tedarikçi adı kartla eşlenir.</summary>
        static List<(string Db, string Sirket, Rapor79Controller_NakitAkis m)> NakitAkis()
        {
            var l = new List<(string, string, Rapor79Controller_NakitAkis)>();
            foreach (var s in NakitAkisOrnek.Sirketler) foreach (var m in NakitAkisOrnek.Odemeler(s.DbName)) if (m.U_KayitTipi != "Avans" && !string.IsNullOrEmpty(m.U_Kurum)) l.Add((s.DbName, s.Ad, new Rapor79Controller_NakitAkis(m)));
            return l;
        }
        /// <summary>NakitAkisViewModel'in bu raporda kullanılan alanları (sarmalayıcı: dış model değişse de rapor etkilenmesin).</summary>
        public class Rapor79Controller_NakitAkis
        {
            public NakitAkisViewModel M; public Rapor79Controller_NakitAkis(NakitAkisViewModel m) { M = m; }
            public int DocEntry => M.DocEntry; public string Kurum => M.U_Kurum ?? ""; public DateTime? Plan => M.U_PlanlananTarih; public DateTime? Odendi => M.U_OdenmeTarihi; public string Durum => M.U_Durum ?? ""; public string Yontem => M.U_OdemeYontemi ?? ""; public string Belge => M.U_FaturaSiparisNo ?? ""; public string BelgeTipi => M.U_BagliBelgeTipi ?? M.U_KayitTipi ?? ""; public decimal TutarTl => M.U_TutarTL ?? 0; public decimal Tutar => M.U_Tutar ?? 0; public string Pb => string.IsNullOrEmpty(M.U_ParaBirimi) ? "TRY" : M.U_ParaBirimi; public string Satinalmaci => M.U_Satinalmaci ?? ""; public string Urun => M.U_UrunHizmet ?? ""; public DateTime? SonOdeme => M.U_SonOdemeTarihi; public string Onay => M.U_OnayDurumu ?? ""; public string FaturaSahibi => M.U_FaturaSahibi ?? ""; public string Odeyen => M.U_OdeyenSirket ?? "";
        }
        static object PlanDto((string Db, string Sirket, Rapor79Controller_NakitAkis m) p, string kod = null) => new { docEntry = p.m.DocEntry, db = p.Db, sirket = p.Sirket, kod, kurum = p.m.Kurum, plan = Trh(p.m.Plan), planIso = p.m.Plan?.ToString("yyyy-MM-dd"), gun = p.m.Plan.HasValue ? (p.m.Plan.Value.Date - DateTime.Today).Days : (int?)null, durum = p.m.Durum, yontem = p.m.Yontem, belge = p.m.Belge, belgeTipi = p.m.BelgeTipi, tutarTl = p.m.TutarTl, tutar = p.m.Tutar, pb = p.m.Pb, satinalmaci = p.m.Satinalmaci, urun = p.m.Urun, sonOdeme = Trh(p.m.SonOdeme), odendi = Trh(p.m.Odendi), onay = p.m.Onay, faturaSahibi = p.m.FaturaSahibi, link = $"/Rapor79?doc={p.m.DocEntry}&sirket={Uri.EscapeDataString(p.Db)}" };

        /// <summary>Tüm tedarikçiler: süzgeç alanları, borç bakiyesi, açık alış faturaları vade kırılımı, verilen çek / senet, son 30 gün ödeme, son ödeme / alım, planlı ödeme, limit, risk, satınalmacı + planlı ödeme listesi.</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            var bugun = DateTime.Today; var son30 = bugun.AddDays(-30); var na = NakitAkis(); var planListe = new List<object>();
            var satirlar = Tedarikciler().Select(k =>
            {
                var c = MutabakatOrnek.Bul(k.Kod); var r = CariKartOrnek.RiskHesapla(k); var l = CariKartOrnek.LimitHesapla(k); var b = TahsilatOrnek.CariBilgi(k.Kod);
                var acik = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Alacak > 0 && h.Kalan > 0.005m).ToList();
                var kalemler = acik.Select(h => new { vade = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), gun = ((h.Vade ?? h.Tarih).Date - bugun).Days, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), belge = h.BelgeNo, pb = h.ParaBirimi, kalan = h.Kalan, islemTipi = h.IslemTipi }).OrderBy(x => x.vade).ToList();
                decimal Kova(int a, int z) => kalemler.Where(x => x.gun >= a && x.gun <= z).Sum(x => x.tl);
                var ode = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => (h.BelgeTipi == "Ödeme" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.Borc > 0).ToList();
                var plan = na.Where(p => CariKartOrnek.AdEsle(p.m.Kurum, k)).ToList(); foreach (var p in plan) planListe.Add(PlanDto(p, k.Kod)); var planAcik = plan.Where(p => p.m.Durum != "ÖDENDİ").ToList();
                return new { suzgec = CariSuzgec.Alanlar(k, r, l), k.Kod, k.KisaAd, k.ResmiUnvan, k.Sirket, sirketDb = CekAkibetOrnek.DbName(k.Sirket), k.Bolge, k.Ulke, k.Sinif, k.VadeSarti, k.IliskiDurumu, temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), satinalmaci = string.Join(", ", MutabakatOrnek.Temsilciler(k)), risk = r.Durum, riskNeden = r.Neden, bakiye = c?.BakiyeTl ?? 0, borc = r.AcikBakiye, bakiyeX = r.BakiyeX, bakiyeR = r.BakiyeR,
                    gecmis = Kova(int.MinValue, -1), gecmisAdet = kalemler.Count(x => x.gun < 0), maxGecikme = r.MaxGecikme, buHafta = Kova(0, 7), g8_30 = Kova(8, 30), g31_60 = Kova(31, 60), g60 = Kova(61, int.MaxValue), acikToplam = kalemler.Sum(x => x.tl), acikAdet = kalemler.Count,
                    cek = r.Cek, cekAdet = r.CekAdet, enYakinCekVade = Trh(r.EnYakinCekVade), son30Odeme = Math.Round(ode.Where(h => h.Tarih >= son30).Sum(h => h.Borc * KurTl(h.ParaBirimi)), 2), son30Adet = ode.Count(h => h.Tarih >= son30), sonOdeme = Trh(b.SonOdeme), sonOdemeTl = b.SonOdemeTutar * KurTl(b.SonOdemePb), sonOdemeGun = b.SonOdeme.HasValue ? (bugun - b.SonOdeme.Value.Date).Days : (int?)null, sonAlim = Trh(b.SonYukleme), sonAlimTl = b.SonYuklemeTutar * KurTl(b.SonYuklemePb), ortalamaGecikme = b.OrtalamaGecikme, alim12Ay = b.Fatura12AyTl, odeme12Ay = b.Odeme12AyTl, eslesmemis = b.EslesmemisOdeme, eslesmemisTl = b.EslesmemisOdemeTl,
                    planAdet = planAcik.Count, planTl = Math.Round(planAcik.Sum(p => p.m.TutarTl), 2), planGecikmis = planAcik.Count(p => p.m.Plan.HasValue && p.m.Plan.Value.Date < bugun), planBuHafta = Math.Round(planAcik.Where(p => p.m.Plan.HasValue && (p.m.Plan.Value.Date - bugun).Days >= 0 && (p.m.Plan.Value.Date - bugun).Days <= 7).Sum(p => p.m.TutarTl), 2), planOdenen30 = Math.Round(plan.Where(p => p.m.Durum == "ÖDENDİ" && p.m.Odendi >= son30).Sum(p => p.m.TutarTl), 2),
                    limitDurum = l.Durum, limit = l.Limit, limitKullanim = l.KullanimYuzde, limitKalan = l.Kalan, kalemler };
            }).ToList();
            return Json(new { success = true, ben = Ben(), bugun = bugun.ToString("yyyy-MM-dd"), secenekler = CariSuzgec.Secenekler(), sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, db = s.DbName }), satirlar, planlar = planListe, odemeler = TahsilatOrnek.Tahsilatlar.Count });
        }
        /// <summary>Tedarikçi detayı: alış faturaları (açık / kapalı, ödeme eşleşmesi), ödemeler ve verilen çekler, eşleşmemiş ödemeler, Kasa Nakit Akışı planlı ödemeleri, günlük.</summary>
        [HttpGet]
        public IActionResult Detay(string kod, int ay = 12)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); var c = MutabakatOrnek.Bul(kod ?? ""); if (k == null || c == null || k.KartTipi != "Tedarikçi") return Json(new { success = false, message = "Tedarikçi bulunamadı." });
            var bugun = DateTime.Today; var bas = bugun.AddMonths(-Math.Max(1, ay)); var b = TahsilatOrnek.CariBilgi(k.Kod); var r = CariKartOrnek.RiskHesapla(k); var l = CariKartOrnek.LimitHesapla(k);
            var odemeler = c.Hareketler.Where(h => (h.BelgeTipi == "Ödeme" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.Borc > 0).OrderByDescending(h => h.Tarih).ToList();
            var faturalar = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Alacak > 0 && (h.Kalan > 0.005m || h.Tarih >= bas)).OrderByDescending(h => h.Vade ?? h.Tarih).Select(h =>
            {
                var es = odemeler.Where(o => !string.IsNullOrEmpty(o.Aciklama) && o.Aciklama.Contains(h.BelgeNo)).ToList(); int gun = (bugun - (h.Vade ?? h.Tarih).Date).Days;
                return new { h.BelgeNo, tarih = Trh(h.Tarih), vade = Trh(h.Vade ?? h.Tarih), vadeIso = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), gun, pb = h.ParaBirimi, tutar = h.Alacak, h.Kalan, kalanTl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), tutarTl = Math.Round(h.Alacak * KurTl(h.ParaBirimi), 2), h.IslemTipi, durum = h.Kalan <= 0.005m ? "Ödendi" : gun > 0 ? "Gecikmiş" : gun <= 7 ? "Bu Hafta" : "Vadesi Gelecek", h.KapamaDurumu, odemeler = es.Select(o => new { o.BelgeNo, o.BelgeTipi, tarih = Trh(o.Tarih), gecikme = (o.Tarih.Date - (h.Vade ?? h.Tarih).Date).Days }), h.Aciklama };
            }).ToList();
            var na = NakitAkis().Where(p => CariKartOrnek.AdEsle(p.m.Kurum, k)).OrderBy(p => p.m.Plan).Select(p => PlanDto(p, k.Kod)).ToList();
            return Json(new { success = true, kod = k.Kod, kisaAd = k.KisaAd, resmiUnvan = k.ResmiUnvan, k.VergiNo, k.Sirket, k.VadeSarti, satinalmaci = string.Join(", ", MutabakatOrnek.Temsilciler(k)), kisiler = c.Kisiler.Select(x => new { x.AdSoyad, x.Unvan, x.Eposta, x.Telefon }), bakiyeTl = c.BakiyeTl, dovizler = c.Dovizler.Where(d => d.Net != 0).Select(d => new { pb = d.ParaBirimi, net = d.Net }), risk = r.Durum, riskNeden = r.Neden, vadesiGecmis = r.VadesiGecmis, vadesiGecmisAdet = r.VadesiGecmisAdet, maxGecikme = r.MaxGecikme, vadesiGelecek = r.VadesiGelecek, cek = r.Cek, cekAdet = r.CekAdet, limit = new { l.Tur, l.Limit, l.Durum, kullanim = l.KullanimYuzde, l.Kalan, l.Aciklama },
                bilgi = new { sonOdeme = Trh(b.SonOdeme), sonOdemeTr = b.SonOdeme.HasValue ? $"{N2(b.SonOdemeTutar)} {b.SonOdemePb}" : "", sonOdemeGun = b.SonOdeme.HasValue ? (bugun - b.SonOdeme.Value.Date).Days : (int?)null, b.SonOdemeTur, sonAlim = Trh(b.SonYukleme), sonAlimTr = b.SonYukleme.HasValue ? $"{N2(b.SonYuklemeTutar)} {b.SonYuklemePb}" : "", sonAlimGun = b.SonYukleme.HasValue ? (bugun - b.SonYukleme.Value.Date).Days : (int?)null, b.OrtalamaGecikme, b.FaturaAdet12Ay, fatura12AyTr = N2(b.Fatura12AyTl) + " ₺", b.OdemeAdet12Ay, odeme12AyTr = N2(b.Odeme12AyTl) + " ₺", b.EslesmemisOdeme, eslesmemisOdemeTr = N2(b.EslesmemisOdemeTl) + " ₺", b.VadeBekleyenCek, vadeBekleyenCekTr = N2(b.VadeBekleyenCekTl) + " ₺" },
                faturalar, odemeler = odemeler.Where(h => h.Tarih >= bas || h.Kalan > 0.005m || h.KapamaDurumu == "Vade Bekliyor").Select(h => new { h.BelgeNo, h.BelgeTipi, tarih = Trh(h.Tarih), vade = Trh(h.Vade), pb = h.ParaBirimi, tutar = h.Borc, tutarTl = Math.Round(h.Borc * KurTl(h.ParaBirimi), 2), h.Kalan, h.KapamaDurumu, h.IslemTipi, h.Aciklama, eslesmemis = h.Kalan > 0.005m }), planlar = na, kapatilabilir = KapamaOrnek.Oner(c).Count, log = c.Log.Take(15).Select(x => new { zaman = x.Zaman.ToString("dd.MM.yyyy HH:mm", Tr), x.Kullanici, x.Islem, x.Aciklama }) });
        }
        [HttpPost]
        public IActionResult OtomatikKapat([FromBody] Dictionary<string, string> i) { var kod = i != null && i.TryGetValue("kod", out var k) ? k : ""; var r = KapamaOrnek.TumunuKapat(new[] { kod }, Ben()); return Json(new { success = true, message = r.Eslesme == 0 ? "Bu tedarikçide kapatılacak eşleşmemiş ödeme yok." : $"{r.Eslesme} fatura–ödeme eşleşmesi kapatıldı · {N2(r.ToplamTl)} ₺.", eslesme = r.Eslesme }); }
    }
}
