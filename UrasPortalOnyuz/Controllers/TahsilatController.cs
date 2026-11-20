using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Tahsilat ekranı uçları (Views/Rapor117/Index.cshtml, wwwroot/js/tahsilat.js; kurallar Data/TahsilatOrnek.cs):
    /// çoklu ödeme satırı (nakit + çek + havale …), otomatik / manuel fatura seçimi, FIFO simülasyonu (gün, vade farkı, kur farkı), otomatik / elle kur, taslak kaydet → işleme al,
    /// fark faturası, otomatik kapama, ekstre ve tahsilat simülasyonu PDF / Excel. Konsolide Tahsilat Takip (Rapor17) verisi de burada.
    /// ÖDEME YÖNÜ: OdemeController (/Odeme/…) bu sınıftan türer, OdemeMi = true — cariler tedarikçi, kayıtlar TahsilatOrnek.Odemeler (ÖD-…), makbuz / simülasyon başlıkları "Ödeme".
    /// </summary>
    [Authorize]
    public class TahsilatController : Controller
    {
        private static readonly CultureInfo Tr = CariKartOrnek.Tr;
        /// <summary>false: müşteri tahsilatı (/Tahsilat); true: tedarikçiye ödeme (/Odeme — Yapılan Ödemeler › Yeni Ödeme). Aynı motor, yön ters.</summary>
        protected virtual bool OdemeMi => false;
        string Ad => OdemeMi ? "Ödeme" : "Tahsilat"; string KartTipi => OdemeMi ? "Tedarikçi" : "Müşteri"; string ListeAdi => OdemeMi ? "Kayıtlı Ödemeler" : "Tahsilatlar"; string DosyaOnEk => OdemeMi ? "Odeme" : "Tahsilat";
        List<TahsilatKaydi> Kayitlar => OdemeMi ? TahsilatOrnek.Odemeler : TahsilatOrnek.Tahsilatlar;
        private NaKisi Ben() => NakitAvansOrnek.KisiBul(User?.Identity?.Name) ?? NakitAvansOrnek.Personel[0];
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static string Zmn(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm", Tr) : "";
        private static string Tl(decimal v) => v.ToString("N2", Tr) + " ₺";
        private static string N2(decimal v) => v.ToString("N2", Tr);
        private static decimal KurTl(string pb) => TahsilatOrnek.TlMi(pb) ? 1m : (MutabakatOrnek.Kurlar.TryGetValue(pb, out var x) ? x : 1m);
        public class SatirIstek { public int Sira { get; set; } public string Yontem { get; set; } public string Tarih { get; set; } public string BelgeNo { get; set; } public string Banka { get; set; } public string Pb { get; set; } public decimal Kur { get; set; } public bool KurElle { get; set; } public decimal Tutar { get; set; } public string Not { get; set; } }
        public class Istek { public string Kod { get; set; } public string Tarih { get; set; } public string IslemTipi { get; set; } = "X"; public string Pb { get; set; } = "TL"; public string Mod { get; set; } = "Otomatik"; public Dictionary<string, decimal> Secim { get; set; } public List<SatirIstek> Satirlar { get; set; } public string Aciklama { get; set; } public bool Isle { get; set; }
            /// <summary>Tahsilat işlenirken doğan vade farkının faturası aynı anda kesilsin (yalnız Isle = true ve müşteri tahsilatında).</summary>
            public bool VadeFarkiKes { get; set; }
            /// <summary>İşlem Tipi çoklu seçimi (X / R): açık fatura kümesini süzer; boş / null = tümü. IslemTipi kaydın kendi tipi (tek seçimde o, karışıkta ilk kapanan faturanın tipi).</summary>
            public List<string> Tipler { get; set; } }
        public class FaturaIstek { public string TahsilatNo { get; set; } public string Tur { get; set; } }
        public class NoIstek { public string No { get; set; } }
        static DateTime TarihCoz(string s, DateTime varsayilan) => DateTime.TryParseExact(s ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : varsayilan;
        static List<TahsilatSatir> Satirlar(Istek i) { var tarih = TarihCoz(i.Tarih, DateTime.Today); return (i.Satirlar ?? new List<SatirIstek>()).Select(s => new TahsilatSatir { Sira = s.Sira, Yontem = s.Yontem, Tarih = TarihCoz(s.Tarih, tarih), BelgeNo = s.BelgeNo, Banka = s.Banka, Pb = string.IsNullOrEmpty(s.Pb) ? i.Pb : s.Pb, Kur = s.Kur, KurElle = s.KurElle, Tutar = s.Tutar, Not = s.Not }).ToList(); }

        // ---------- DTO ----------
        static object CariDto(CariKart k)
        {
            var r = CariKartOrnek.RiskHesapla(k); var acik = TahsilatOrnek.AcikFaturalar(k.Kod);
            return new { suzgec = CariSuzgec.Alanlar(k, r), k.Kod, k.KisaAd, k.ResmiUnvan, k.VadeSarti, k.Sirket, sirketDb = CekAkibetOrnek.DbName(k.Sirket), temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), bakiye = r.Bakiye, bakiyeTr = Tl(r.Bakiye), bakiyeXTr = Tl(r.BakiyeX), bakiyeRTr = Tl(r.BakiyeR), vadesiGecmis = r.VadesiGecmis, vadesiGecmisTr = Tl(r.VadesiGecmis), vadesiGecmisAdet = r.VadesiGecmisAdet, maxGecikme = r.MaxGecikme, risk = r.Durum, cek = r.Cek, cekAdet = r.CekAdet, acikAdet = acik.Count, acikTl = Math.Round(acik.Sum(f => f.Kalan * KurTl(f.ParaBirimi)), 2), pbler = acik.Select(f => f.ParaBirimi).Distinct().OrderBy(p => p == "TL" ? "" : p).ToArray(), acikPb = acik.GroupBy(f => f.ParaBirimi).Select(g => new { pb = g.Key, adet = g.Count(), kalan = g.Sum(f => f.Kalan), kalanTr = N2(g.Sum(f => f.Kalan)) + " " + g.Key }) };
        }
        static object BilgiDto(TahsilatCariBilgi b) => new { sonOdeme = Trh(b.SonOdeme), sonOdemeGun = b.SonOdeme.HasValue ? (DateTime.Today - b.SonOdeme.Value.Date).Days : (int?)null, sonOdemeTutar = b.SonOdemeTutar, sonOdemeTr = b.SonOdeme.HasValue ? $"{N2(b.SonOdemeTutar)} {b.SonOdemePb}" : "", b.SonOdemeTur, b.SonOdemeBelge, sonYukleme = Trh(b.SonYukleme), sonYuklemeGun = b.SonYukleme.HasValue ? (DateTime.Today - b.SonYukleme.Value.Date).Days : (int?)null, sonYuklemeTr = b.SonYukleme.HasValue ? $"{N2(b.SonYuklemeTutar)} {b.SonYuklemePb}" : "", b.SonYuklemeBelge, b.OrtalamaGecikme, b.OdemeAdet12Ay, odeme12AyTr = Tl(b.Odeme12AyTl), b.FaturaAdet12Ay, fatura12AyTr = Tl(b.Fatura12AyTl), b.EslesmemisOdeme, eslesmemisOdemeTr = Tl(b.EslesmemisOdemeTl), b.VadeBekleyenCek, vadeBekleyenCekTr = Tl(b.VadeBekleyenCekTl) };
        static object SatirDto(TahsilatSatir s) => new { s.Sira, s.Yontem, tarih = Trh(s.Tarih), tarihIso = s.Tarih.ToString("yyyy-MM-dd"), s.BelgeNo, s.Banka, s.Pb, s.Kur, s.KurElle, s.Tutar, tutarTr = N2(s.Tutar) + " " + s.Pb, tutarTl = s.TutarTl, tutarTlTr = Tl(s.TutarTl), s.Not, s.HareketNo, vadeli = s.Vadeli, nakitAkisDoc = s.NakitAkisDocEntry, nakitAkisLink = TahsilatOrnek.NakitAkisLink(s) };
        static object DagitimDto(TahsilatDagitim d) => new { d.BelgeNo, faturaTarihi = Trh(d.FaturaTarihi), vade = Trh(d.Vade), d.Pb, d.IslemTipi, d.Kalan, kalanTr = N2(d.Kalan), d.Tutar, tutarTr = N2(d.Tutar), d.OdemeTutar, tutarTl = d.TutarTl, d.BazKur, d.BazKaynak, tutarTlTr = Tl(d.TutarTl), d.FaturaKuru, d.TahsilatKuru, d.GecikmeGun, d.VadeFarki, vadeFarkiTr = Tl(d.VadeFarki), d.KurFarki, kurFarkiTr = Tl(d.KurFarki), d.OdemeSira, d.OdemeYontem, odemeTarihi = Trh(d.OdemeTarihi), d.OdemeBelge, d.FaturaKalan, faturaKalanTr = N2(d.FaturaKalan) };
        static object TahsilatDto(TahsilatKaydi t) => t == null ? null : new { t.No, t.Tur, odeme = t.Odeme, tarih = Trh(t.Tarih), tarihIso = t.Tarih.ToString("yyyy-MM-dd"), t.CariKodu, t.CariAd, t.Sirket, sirketDb = CekAkibetOrnek.DbName(t.Sirket), panelSekme = t.Satirlar.Any(s => s.Yontem == "Havale / EFT" || s.Yontem == "Kredi Kartı") ? "banka" : t.Satirlar.Any(s => s.Yontem == "Nakit") ? "kasa" : "cek", t.Yontem, t.ParaBirimi, t.Tutar, tutarTr = N2(t.Tutar) + " " + t.ParaBirimi, t.Kur, tutarTl = t.TutarTl, tutarTlTr = Tl(t.TutarTl), t.IslemTipi, t.Mod, t.Aciklama, t.BelgeNo, t.Durum, islendi = t.Islendi, satirlar = t.Satirlar.Select(SatirDto), faturaSecim = t.FaturaSecim, dagitim = t.Dagitim.Select(DagitimDto), dagitilan = t.Dagitilan, avans = t.Avans, avansTr = N2(t.Avans) + " " + t.ParaBirimi, kurFarki = t.KurFarki, kurFarkiTr = Tl(t.KurFarki), vadeFarki = t.VadeFarki, vadeFarkiTr = Tl(t.VadeFarki), t.KurFarkiFaturaNo, t.VadeFarkiFaturaNo, t.Kullanici, zaman = Zmn(t.Zaman), t.IslemeAlan, islemeZamani = Zmn(t.IslemeZamani), satirAdet = t.Satirlar.Count, cekAdet = t.Satirlar.Count(s => s.Vadeli) };
        static object FaturaDto(FarkFaturasi f) => FaturaDtoK(f, TahsilatOrnek.FaturaKalan(f));
        // 19 Kas 2026 (kullanıcı: "hem kur farkı değerlemesi hem kur farkı faturası olmalı"): Kesilen Fark Faturaları listesi = vade farkı faturaları + kur farkı faturaları (tahsilattan) + kur farkı DEĞERLEME dekontları (muhasebe virmanı)
        static object DegerlemeDto(KurDegerleme d)
        {
            var k = CariKartOrnek.KartBul(d.CariKodu);
            return new { no = d.DekontNo, tarih = Trh(d.Tarih), tarihIso = d.Tarih.ToString("yyyy-MM-dd"), vade = Trh(d.Tarih), vadeIso = d.Tarih.ToString("yyyy-MM-dd"), cariKodu = d.CariKodu, cariAd = d.CariAd, cariUnvan = k?.ResmiUnvan ?? d.CariAd, vergiNo = k?.VergiNo ?? "", vergiDairesi = k?.VergiDairesi ?? "",
                tur = "Kur Farkı Değerlemesi", tahsilatNo = "", islemTipi = d.IslemTipi, kaynak = d.Kaynak, donem = d.Donem, kalan = 0m, odeme = "", degerleme = true, fisNo = d.FisNo, kurlar = d.Kurlar, faturaAdet = d.Faturalar.Count,
                kalemler = d.Faturalar.Select(f => new { aciklama = $"{f.BelgeNo}: {f.Kalan.ToString("N2", Tr)} {f.Pb} × ({f.YeniKur.ToString("N4", Tr)} − {f.BazKur.ToString("N4", Tr)}) · baz: {f.BazKaynak}", tutar = f.Fark, tutarTr = Tl(f.Fark) }),
                matrah = d.Toplam, matrahTr = Tl(d.Toplam), kdvOran = 0m, kdv = 0m, kdvTr = Tl(0), toplam = d.Toplam, toplamTr = Tl(d.Toplam), kullanici = d.Kullanici, zaman = Zmn(d.Zaman), durum = "Dekont" };
        }
        static List<object> FarkBelgeleri() => TahsilatOrnek.Faturalar.Select(FaturaDto).Concat(KurDegerlemeOrnek.Liste().Select(DegerlemeDto)).ToList();
        /// <summary>Yeni Tahsilat › bakiye denklemi yanındaki kur farkı bilgisi: R dövizli açık faturalarda değerleme bekleyen fark, X dövizli faturalarda değerlenmeyen fark.</summary>
        static object KurOnizlemeDto(string kod)
        {
            var (r, x) = KurDegerlemeOrnek.Onizleme(kod); var son = KurDegerlemeOrnek.Liste(kod).FirstOrDefault();
            return new { rAdet = r.Count, rFark = Math.Round(r.Sum(f => f.Fark), 2), xAdet = x.Count, xFark = Math.Round(x.Sum(f => f.Fark), 2), sonDegerleme = son == null ? "" : Trh(son.Tarih), sonDekont = son?.DekontNo ?? "", sonToplam = son?.Toplam ?? 0,
                r = r.Select(f => new { f.BelgeNo, f.Pb, f.Kalan, f.BazKur, f.BazKaynak, f.YeniKur, f.Fark }), x = x.Select(f => new { f.BelgeNo, f.Pb, f.Kalan, f.BazKur, f.YeniKur, f.Fark }) };
        }
        static object FaturaDtoK(FarkFaturasi f, decimal kalan) => new { f.No, tarih = Trh(f.Tarih), tarihIso = f.Tarih.ToString("yyyy-MM-dd"), vade = Trh(f.Vade), vadeIso = f.Vade.ToString("yyyy-MM-dd"), f.CariKodu, f.CariAd, f.CariUnvan, f.VergiNo, f.VergiDairesi, f.Tur, f.TahsilatNo, f.IslemTipi, f.Kaynak, f.Donem, kalan, odeme = kalan <= 0.005m ? "Ödendi" : f.Vade.Date < DateTime.Today ? "Vadesi Geçti" : kalan < f.Toplam - 0.005m ? "Kısmi Ödendi" : "Açık", kalemler = f.Kalemler.Select(k => new { aciklama = k.Aciklama, tutar = k.Tutar, tutarTr = Tl(k.Tutar) }), matrah = f.Matrah, matrahTr = Tl(f.Matrah), f.KdvOran, kdv = f.Kdv, kdvTr = Tl(f.Kdv), toplam = f.Toplam, toplamTr = Tl(f.Toplam), f.Kullanici, zaman = Zmn(f.Zaman), f.Durum };

        /// <summary>Müşteri carileri (süzgeç alanları, bakiye, X / R, vadesi geçmiş, risk, para birimleri), seçenekler, parametreler, kayıtlı tahsilatlar ve fark faturaları.</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            var ben = Ben();
            return Json(new { ben = new { ben.AdSoyad, ben.Departman, ben.Sirket }, odeme = OdemeMi, ad = Ad, kartTipi = KartTipi, cariler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == KartTipi).OrderBy(k => k.Kod).Select(CariDto), secenekler = CariSuzgec.Secenekler(), yontemler = TahsilatOrnek.Yontemler, bankalar = TahsilatOrnek.Bankalar, islemTipleri = TahsilatOrnek.IslemTipleri, kurlar = TahsilatOrnek.Kurlar, kurTarihi = Trh(MutabakatOrnek.KurTarihi), vadeFarkiAylikOran = TahsilatOrnek.VadeFarkiAylikOran, kdvOran = TahsilatOrnek.KdvOran, bugun = DateTime.Today.ToString("yyyy-MM-dd"), tahsilatlar = Kayitlar.Select(TahsilatDto), faturalar = OdemeMi ? new List<object>() : FarkBelgeleri() });   /* fark faturaları + kur farkı değerleme dekontları (19 Kas 2026) */
        }
        /// <summary>Carinin açık faturaları + tahsilat özeti (son ödeme, son yükleme, ortalama gecikme, eşleşmemiş ödeme) + kapatılabilir eşleşme sayısı.</summary>
        [HttpGet]
        // ---------- FATURA DETAYI (19 Kas 2026; kullanıcı: "Belge No'ya tıkladığımda fatura açılsın, detay gözüksün" — Fatura İnceleme düzeninde: başlık, alıcı, kalemler, bağlı tahsilatlar, e-fatura görünümü) ----------
        [HttpGet] public IActionResult FaturaDetay(string kod, string no) { ViewBag.Kod = kod ?? ""; ViewBag.No = no ?? ""; return View("~/Views/Tahsilat/FaturaDetay.cshtml"); }
        [HttpGet]
        public IActionResult FaturaDetayVeri(string kod, string no)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); var c = MutabakatOrnek.Bul(kod ?? ""); if (k == null || c == null) return Json(new { success = false, message = "Cari bulunamadı." });
            MtHareket h; lock (MutabakatOrnek.Kilit) h = c.Hareketler.FirstOrDefault(x => (x.BelgeTipi == "Fatura" || x.BelgeTipi == "İade Faturası") && string.Equals(x.BelgeNo, no, StringComparison.OrdinalIgnoreCase));
            if (h == null) return Json(new { success = false, message = $"{no} numaralı fatura bu caride bulunamadı." });
            var bugun = DateTime.Today; var vade = (h.Vade ?? h.Tarih).Date; bool ted = k.KartTipi == "Tedarikçi"; bool fatura = h.Borc > 0;
            dynamic d = SatisRaporOrnek.FaturaDetayi(k, h);
            // bağlı tahsilatlar: Tahsilat modülünde bu faturayı kapatan dağıtım satırları + cari hesapta açıklaması bu belgeyi anan ödeme hareketleri
            var kayitlar = (ted ? TahsilatOrnek.Odemeler : TahsilatOrnek.Tahsilatlar).Where(t => t.Islendi && t.CariKodu == k.Kod && t.Dagitim.Any(x => x.BelgeNo == h.BelgeNo)).ToList();
            var tahsilatlar = new List<object>(); var gorulen = new HashSet<string>();
            foreach (var t in kayitlar) foreach (var x in t.Dagitim.Where(x => x.BelgeNo == h.BelgeNo)) { var s = t.Satirlar.FirstOrDefault(y => y.Sira == x.OdemeSira); if (s != null && !string.IsNullOrEmpty(s.HareketNo)) gorulen.Add(s.HareketNo); tahsilatlar.Add(new { kaynak = "Tahsilat Modülü", no = t.No, tarih = Trh(x.OdemeTarihi == default ? t.Tarih : x.OdemeTarihi), yontem = x.OdemeYontem, belge = x.OdemeBelge, tutar = x.Tutar, pb = x.Pb, gun = x.GecikmeGun, vadeFarki = x.VadeFarki, kurFarki = x.KurFarki, kalanSonra = x.FaturaKalan, kur = x.TahsilatKuru, bazKur = x.BazKur, bazKaynak = x.BazKaynak, link = $"/Rapor117?sekme=liste&no={Uri.EscapeDataString(t.No)}" }); }
            // kur geçmişi (19 Kas 2026): fatura kuru → muhasebe değerlemeleri (R) → ödeme kurları; kur farkı faturasının bazı son değerleme
            var degerlemeler = KurDegerlemeOrnek.FaturaGecmisi(k.Kod, h.BelgeNo).Select(g => new { dekont = g.D.DekontNo, tarih = Trh(g.D.Tarih), donem = g.D.Donem, bazKur = g.F.BazKur, bazKaynak = g.F.BazKaynak, yeniKur = g.F.YeniKur, kalan = g.F.Kalan, fark = g.F.Fark, kullanici = g.D.Kullanici, fisNo = g.D.FisNo }).ToList();
            var bazSimdi = KurDegerlemeOrnek.BazKur(k.Kod, h);
            List<MtHareket> har; lock (MutabakatOrnek.Kilit) har = c.Hareketler.Where(x => x.Alacak > 0 && (x.BelgeTipi == "Tahsilat" || x.BelgeTipi == "Çek" || x.BelgeTipi == "Senet" || x.BelgeTipi == "Mahsup" || x.BelgeTipi == "Virman") && (x.Aciklama ?? "").Contains(h.BelgeNo ?? "~") && !gorulen.Contains(x.BelgeNo ?? "")).OrderBy(x => x.Tarih).ToList();
            foreach (var x in har) tahsilatlar.Add(new { kaynak = "Cari Hesap", no = x.BelgeNo, tarih = Trh(x.Tarih), yontem = x.BelgeTipi, belge = "", tutar = x.Alacak, pb = TahsilatOrnek.TlMi(x.ParaBirimi) ? "TL" : x.ParaBirimi, gun = (x.Tarih.Date - vade).Days, vadeFarki = 0m, kurFarki = 0m, kalanSonra = (decimal?)null, link = $"/Rapor20?bpCode={Uri.EscapeDataString(k.Kod)}" });
            var fark = TahsilatOrnek.Faturalar.Where(f => f.CariKodu == k.Kod && f.Kalemler.Any(kl => (kl.Aciklama ?? "").Contains(h.BelgeNo ?? "~"))).Select(f => new { f.No, tarih = Trh(f.Tarih), f.Tur, f.Kaynak, kalem = f.Kalemler.Where(kl => (kl.Aciklama ?? "").Contains(h.BelgeNo)).Sum(kl => kl.Tutar), f.Toplam, link = $"/Rapor117?sekme=fatura" }).ToList();
            // e-belge kimliği (ön yüz örneği: belge numarasından deterministik)
            string ettn; unchecked { uint a = 2166136261; foreach (char ch in (k.Kod + "|" + h.BelgeNo)) { a ^= ch; a *= 16777619; } var g = new byte[16]; BitConverter.GetBytes(a).CopyTo(g, 0); BitConverter.GetBytes(a * 31u).CopyTo(g, 4); BitConverter.GetBytes(a * 131u).CopyTo(g, 8); BitConverter.GetBytes(a * 1313u).CopyTo(g, 12); ettn = new Guid(g).ToString(); }
            string efatNo = (h.BelgeNo ?? "").StartsWith("URS") || (h.BelgeNo ?? "").StartsWith("VFF") || (h.BelgeNo ?? "").StartsWith("KFF") ? h.BelgeNo : $"URS{h.Tarih:yyyy}{Math.Abs(h.BelgeNo?.GetHashCode() ?? 0) % 1000000000:000000000}";
            decimal brut = d.brut, net = d.net, kdv = d.kdv, kur = d.kur; decimal kalan = fatura ? h.Kalan : 0m;
            return Json(new
            {
                success = true,
                baslik = new
                {
                    no = h.BelgeNo, tur = fatura ? (ted ? "Satınalma Faturası" : "Satış Faturası") : (ted ? "Satınalma İade Faturası" : "Satış İade Faturası"), tarih = Trh(h.Tarih), tarihIso = h.Tarih.ToString("yyyy-MM-dd"), vade = Trh(vade), vadeIso = vade.ToString("yyyy-MM-dd"),
                    gun = (bugun - vade).Days, fatGun = (bugun - h.Tarih.Date).Days, pb = (string)d.pb, kur, brut, net, kdv, kdvOran = (decimal)d.kdvOran, brutTl = Math.Round(brut * kur, 2), kalan, kalanTl = Math.Round(kalan * kur, 2), odenen = Math.Round(brut - kalan, 2),
                    durum = fatura ? (kalan <= 0.005m ? "Kapalı" : (h.KapamaDurumu ?? "Açık")) : "Kapalı", islemTipi = h.IslemTipi, aciklama = h.Aciklama ?? "", temsilci = (string)d.temsilci, efatNo, ettn,
                    senaryo = (bool)d.yurtici ? "TEMELFATURA · e-Fatura" : "İHRACAT · e-Fatura", sirket = d.sirket, depo = (string)d.sirket.depo,
                    cari = new { k.Kod, k.KisaAd, k.ResmiUnvan, k.VergiNo, k.VergiDairesi, k.Sehir, k.Ulke, k.Bolge, k.Yetkili, k.Telefon, k.Eposta, k.VadeSarti, k.Sinif, k.IliskiDurumu }
                },
                kalemler = d.kalemler, tahsilatlar, farkFaturalari = fark, inceleme = "/Rapor166",
                kurGecmisi = new { pb = (string)d.pb, faturaKuru = (decimal)d.kur, degerlemeler, bazKur = bazSimdi.Kur, bazKaynak = bazSimdi.Kaynak, bugunKur = TahsilatOrnek.TlMi((string)d.pb) ? 1m : TahsilatOrnek.KurOto((string)d.pb, DateTime.Today), degerlenir = (h.IslemTipi ?? "X") == "R" }
            });
        }
        /// <summary>
        /// BELGE İNCELEMESİ (19 Kas 2026; kullanıcı: "tüm sekmelerde belge no / fatura no tıklanınca belge ya da fatura inceleme detayı açılsın"): belge no'ya göre
        /// fatura ise Fatura İncelemesi'ne yönlendirir; tahsilat / çek / senet / dekont ise hareket detayı (bağlı tahsilat kaydı, kapattığı faturalar, kapamalar, değerleme kırılımı); banka hareketi ise banka kaydı.
        /// </summary>
        [HttpGet]
        public IActionResult BelgeDetay(string kod, string no)
        {
            no = (no ?? "").Trim(); if (no == "") return Json(new { success = false, message = "Belge no boş." });
            var k = CariKartOrnek.KartBul(kod ?? ""); var c = k == null ? null : MutabakatOrnek.Bul(k.Kod);
            MtHareket h = null; if (c != null) lock (MutabakatOrnek.Kilit) h = c.Hareketler.FirstOrDefault(x => string.Equals(x.BelgeNo, no, StringComparison.OrdinalIgnoreCase));
            if (h == null) foreach (var cc in MutabakatOrnek.Cariler) { lock (MutabakatOrnek.Kilit) h = cc.Hareketler.FirstOrDefault(x => string.Equals(x.BelgeNo, no, StringComparison.OrdinalIgnoreCase)); if (h != null) { c = cc; k = CariKartOrnek.KartBul(cc.CariKodu); break; } }
            if (h != null)
            {
                if (h.BelgeTipi == "Fatura" || h.BelgeTipi == "İade Faturası") return Json(new { success = true, tur = "Fatura", kod = c.CariKodu, no = h.BelgeNo });
                decimal kur = KurTl(h.ParaBirimi);
                var t = TahsilatOrnek.Tahsilatlar.Concat(TahsilatOrnek.Odemeler).FirstOrDefault(x => x.Satirlar.Any(s => s.HareketNo == h.BelgeNo)); var sat = t?.Satirlar.FirstOrDefault(s => s.HareketNo == h.BelgeNo);
                var dag = t == null || sat == null ? new List<TahsilatDagitim>() : t.Dagitim.Where(x => x.OdemeSira == sat.Sira).ToList();
                var kapamalar = KapamaOrnek.Kayitlar.Where(x => x.CariKodu == c.CariKodu && x.Ciftler.Any(y => y.OdemeNo == h.BelgeNo)).SelectMany(x => x.Ciftler.Where(y => y.OdemeNo == h.BelgeNo).Select(y => new { zaman = Zmn(x.Zaman), x.Mod, y.FaturaNo, y.Tutar, y.Pb, y.Yontem, x.Kullanici })).ToList();
                var deg = h.BelgeTipi == "Kur Farkı" ? KurDegerlemeOrnek.Liste(c.CariKodu).FirstOrDefault(x => x.DekontNo == h.BelgeNo) : null;
                return Json(new
                {
                    success = true, tur = "Hareket", kod = c.CariKodu, kisaAd = k?.KisaAd ?? c.CariKodu,
                    hareket = new { belgeNo = h.BelgeNo, tip = h.BelgeTipi, tarih = Trh(h.Tarih), vade = h.Vade.HasValue ? Trh(h.Vade) : "", pb = h.ParaBirimi, borc = h.Borc, alacak = h.Alacak, tl = Math.Round((h.Borc + h.Alacak) * kur, 2), kalan = h.Kalan, kalanTl = Math.Round(h.Kalan * kur, 2), durum = h.KapamaDurumu ?? "", islemTipi = h.IslemTipi, aciklama = h.Aciklama ?? "", bakiyeSonra = h.Bakiye },
                    tahsilat = t == null ? null : new { t.No, tarih = Trh(t.Tarih), t.Yontem, tutar = t.Tutar, pb = t.ParaBirimi, durum = t.Durum, kullanici = t.Kullanici, mod = t.Mod, satir = sat == null ? null : new { sat.Sira, sat.Yontem, tarih = Trh(sat.Tarih), sat.BelgeNo, sat.Banka, sat.Tutar, sat.Kur, sat.Not } },
                    dagitim = dag.Select(x => new { x.BelgeNo, tutar = x.Tutar, x.Pb, gun = x.GecikmeGun, x.VadeFarki, x.KurFarki, kalanSonra = x.FaturaKalan }),
                    kapamalar,
                    degerleme = deg == null ? null : new { deg.DekontNo, deg.Donem, deg.FisNo, kurlar = deg.Kurlar, toplam = deg.Toplam, deg.Kullanici, faturalar = deg.Faturalar.Select(f => new { f.BelgeNo, f.Pb, f.Kalan, f.BazKur, f.BazKaynak, f.YeniKur, f.Fark }) }
                });
            }
            // 19 Kas 2026 gece: tahsilat / ödeme KAYDI numarası (TH- / ÖD-; taslak dahil) → kaydın kendisi (satırlar, kapattığı faturalar, fark faturaları)
            var tk = TahsilatOrnek.Tahsilatlar.Concat(TahsilatOrnek.Odemeler).FirstOrDefault(x => string.Equals(x.No, no, StringComparison.OrdinalIgnoreCase));
            if (tk != null) return Json(new { success = true, tur = "TahsilatKaydi", kod = tk.CariKodu, kayit = TahsilatDto(tk) });
            var b = FinekraOrnek.Listele(null).FirstOrDefault(x => string.Equals(x.No, no, StringComparison.OrdinalIgnoreCase));
            if (b != null) return Json(new { success = true, tur = "Banka", banka = new { b.No, b.Sirket, b.Banka, tarih = Trh(b.Tarih), b.Yon, tutar = b.Tutar, pb = b.PB, dovizTutar = b.DovizTutar, kur = b.Kur, islemTuru = b.IslemTuru, karsiAd = b.KarsiAd, karsiIban = b.KarsiIban, aciklama = b.Aciklama, referans = b.Referans, durum = b.Durum, onaylayan = b.Onaylayan, onayTarihi = Trh(b.OnayTarihi), onayNotu = b.OnayNotu, cariKod = b.CariKod, cariAd = b.CariAd, yevmiyeNo = b.YevmiyeNo, iban = b.Iban, hesapKodu = b.HesapKodu, tahsilatNo = BankaTahsilati(b.No)?.No ?? "" } });
            return Json(new { success = false, message = $"{no} belgesi bulunamadı." });
        }
        /// <summary>e-fatura görünümündeki kare kod (QRCoder); metin = ETTN · belge no · tutar.</summary>
        [HttpGet]
        public IActionResult FaturaQr(string metin)
        {
            using var gen = new QRCoder.QRCodeGenerator(); using var data = gen.CreateQrCode(metin ?? "", QRCoder.QRCodeGenerator.ECCLevel.Q);
            var png = new QRCoder.PngByteQRCode(data).GetGraphic(5); return File(png, "image/png");
        }

        public IActionResult Faturalar(string kod)
        {
            var bugun = DateTime.Today; var c = MutabakatOrnek.Bul(kod ?? ""); var oner = c == null ? 0 : KapamaOrnek.Oner(c).Count;
            // 19 Kas 2026 (kullanıcı: "Bakiye 4.768.232 ile Kalan 11.282.575 yan yana kafa karıştırıyor; fatura dışı borç ve kapatılmamış ödeme de görünsün, virman buradan atılsın"):
            //   Açık Faturalar + Fatura Dışı Borç − Kapatılmamış Ödeme / Avans = Bakiye (Alacak Takibi ile aynı denklem). fdb: fatura olmayan borç hareketleri; odemeAcik: faturayla eşlenmemiş alacak (kalan).
            var fdb = c == null ? new List<object>() : c.Hareketler.Where(h => h.BelgeTipi != "Fatura" && h.Borc > 0).OrderBy(h => h.Tarih).Select(h => (object)new { belge = h.BelgeNo, tip = h.BelgeTipi, tarih = Trh(h.Tarih), pb = h.ParaBirimi, tutar = h.Borc, tl = Math.Round(h.Borc * KurTl(h.ParaBirimi), 2), aciklama = h.Aciklama ?? "", islemTipi = h.IslemTipi }).ToList();
            var odemeAcik = c == null ? new List<object>() : c.Hareketler.Where(h => h.Alacak > 0 && h.Kalan > 0.005m).OrderBy(h => h.Tarih).Select(h => (object)new { belge = h.BelgeNo, tip = h.BelgeTipi, tarih = Trh(h.Tarih), vade = h.Vade.HasValue ? Trh(h.Vade) : "", pb = h.ParaBirimi, tutar = h.Alacak, kalan = h.Kalan, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), aciklama = h.Aciklama ?? "", islemTipi = h.IslemTipi, durum = h.KapamaDurumu, vadeBekliyor = h.KapamaDurumu == "Vade Bekliyor" }).ToList();
            decimal acikTl = c == null ? 0 : Math.Round(TahsilatOrnek.AcikFaturalar(kod ?? "").Sum(f => f.Kalan * KurTl(f.ParaBirimi)), 2), fdbTl = Math.Round(fdb.Sum(x => (decimal)((dynamic)x).tl), 2), odemeAcikTl = Math.Round(odemeAcik.Sum(x => (decimal)((dynamic)x).tl), 2), bakiyeTl = c?.BakiyeTl ?? 0;
            return Json(new { success = true, faturalar = TahsilatOrnek.AcikFaturalar(kod ?? "").Select(f => new { f.BelgeNo, tarih = Trh(f.Tarih), tarihIso = f.Tarih.ToString("yyyy-MM-dd"), vade = Trh(f.Vade ?? f.Tarih), vadeIso = (f.Vade ?? f.Tarih).ToString("yyyy-MM-dd"), gecikme = (bugun - (f.Vade ?? f.Tarih).Date).Days, pb = f.ParaBirimi, f.Kalan, kalanTr = N2(f.Kalan), tutar = f.Borc, faturaKuru = TahsilatOrnek.FaturaKuru(f), islemTipi = f.IslemTipi, f.Aciklama, f.KapamaDurumu, bazKur = KurDegerlemeOrnek.BazKur(kod ?? "", f).Kur, bazKaynak = KurDegerlemeOrnek.BazKur(kod ?? "", f).Kaynak, degerlemeTarih = KurDegerlemeOrnek.BazKur(kod ?? "", f).Tarih?.ToString("dd.MM.yyyy") ?? "" }), oran = TahsilatOrnek.Oran(kod ?? ""), oranOzel = TahsilatTakipKayit.MusteriOran(kod ?? "") != null, bakiyeTl, acikTl, fdbTl, odemeAcikTl, kapamaFarki = Math.Round(acikTl + fdbTl - odemeAcikTl - bakiyeTl, 2), fdb, odemeAcik, kurOnizleme = KurOnizlemeDto(kod ?? ""), bilgi = BilgiDto(TahsilatOrnek.CariBilgi(kod ?? "")), kapatilabilir = oner, kapatilabilirTl = c == null ? 0 : KapamaOrnek.Kapatilabilir(c) });
        }
        /// <summary>Tarihe göre otomatik kur (TCMB serisi).</summary>
        [HttpGet]
        public IActionResult Kur(string pb, string tarih) { var t = TarihCoz(tarih, DateTime.Today); var k = TahsilatOrnek.KurOto(pb, t); var tc = KurOrnek.Tcmb(pb, t); return Json(new { success = true, pb, tarih = t.ToString("yyyy-MM-dd"), kur = k, tahmini = tc.tahmini, kurTarihi = tc.tarih.ToString("dd.MM.yyyy"), aciklama = TahsilatOrnek.TlMi(pb) ? "TL" : tc.tahmini ? $"{t:dd.MM.yyyy} ileri tarih: bugünün kuru tahmini; gün gelince kesinleşir" : $"{tc.tarih:dd.MM.yyyy} TCMB kuru" }); }
        /// <summary>Dağıtım önizlemesi (kaydetmez): satır bazında eşleşme, gün, vade farkı, kur farkı ve toplamlar; hazırlanmış ödeme satırları (otomatik kur yazılmış).</summary>
        [HttpPost]
        public IActionResult Onizle([FromBody] Istek i)
        {
            if (i == null || string.IsNullOrEmpty(i.Kod)) return Json(new { success = false, message = "Cari seçin." });
            var sat = TahsilatOrnek.SatirlariHazirla(Satirlar(i), i.Pb ?? "TL", TarihCoz(i.Tarih, DateTime.Today)); var d = TahsilatOrnek.Hesapla(i.Kod, i.Pb ?? "TL", i.Mod, i.Secim, sat, i.Tipler); decimal tutar = sat.Sum(s => s.Tutar);
            return Json(new { success = true, satirlar = sat.Select(SatirDto), dagitim = d.Select(DagitimDto), tutar, tutarTl = sat.Sum(s => s.TutarTl), dagitilan = d.Sum(x => x.OdemeTutar), avans = Math.Round(tutar - d.Sum(x => x.OdemeTutar), 2), vadeFarki = d.Sum(x => x.VadeFarki), kurFarki = d.Sum(x => x.KurFarki), kdvOran = TahsilatOrnek.KdvOran, kapananFatura = d.Select(x => x.BelgeNo).Distinct().Count(), tamKapanan = d.GroupBy(x => x.BelgeNo).Count(g => g.Last().FaturaKalan <= 0.005m) });
        }
        [HttpPost]
        public IActionResult Kaydet([FromBody] Istek i)
        {
            if (i == null) return Json(new { success = false, message = "Geçersiz istek." });
            if (CariKartOrnek.KartBul(i.Kod ?? "")?.KartTipi != KartTipi) return Json(new { success = false, message = $"Bu ekranda yalnız {KartTipi.ToLower(Tr)} carileri seçilir." });
            var h = TahsilatOrnek.Kaydet(i.Kod, TarihCoz(i.Tarih, DateTime.Today), i.IslemTipi, i.Pb ?? "TL", i.Mod, i.Secim, Satirlar(i), i.Aciklama, Ben().AdSoyad, i.Isle, out var k, null, i.Tipler);
            if (h != null) return Json(new { success = false, message = h });
            // tahsilat alınırken vade farkı faturası: işlenen tahsilatta vade farkı doğduysa ve istenmişse aynı anda kesilir
            FarkFaturasi vff = null; string vfNot = "";
            if (i.Isle && i.VadeFarkiKes && !OdemeMi && k.VadeFarki > 0.005m) { var vh = TahsilatOrnek.FaturaKes(k.No, "Vade Farkı", Ben().AdSoyad, out vff); vfNot = vh == null ? $" Vade farkı faturası {vff.No} kesildi: {Tl(vff.Toplam)} (KDV dahil)." : $" Vade farkı faturası kesilemedi: {vh}"; }
            return Json(new { success = true, message = (i.Isle ? $"{k.No} kaydedildi ve cari hesabına işlendi: {N2(k.Tutar)} {k.ParaBirimi} ({k.Yontem}) · {k.Dagitim.Select(d => d.BelgeNo).Distinct().Count()} fatura{(k.Avans > 0.005m ? $", {N2(k.Avans)} {k.ParaBirimi} {(OdemeMi ? "tedarikçiye" : "bakiyeye")} avans" : "")}{(k.VadeFarki > 0 ? $" · vade farkı {Tl(k.VadeFarki)}" : "")}{(k.KurFarki != 0 ? $" · kur farkı {Tl(k.KurFarki)}" : "")}.{NakitAkisNotu(k)}" : $"{k.No} TASLAK olarak kaydedildi ({N2(k.Tutar)} {k.ParaBirimi}, {k.Yontem}); cariye işlenmedi. {ListeAdi} sekmesinden \"İşleme Al\" ile istediğiniz zaman işlenir.") + vfNot, tahsilat = TahsilatDto(k), fatura = vff == null ? null : FaturaDto(vff) });
        }
        /// <summary>Ödemede: işlenince açılan Nakit Akış Planlama satırlarının notu.</summary>
        static string NakitAkisNotu(TahsilatKaydi k) { var l = k.Satirlar.Where(s => s.NakitAkisDocEntry != null).ToList(); return l.Count == 0 ? "" : $" Nakit Akış Planlama'ya ÖDENDİ satır{(l.Count > 1 ? "ları" : "ı")} açıldı: {string.Join(", ", l.Select(s => "#" + s.NakitAkisDocEntry))}; Ödeme Listesi'nde görünür."; }
        [HttpPost]
        public IActionResult IslemeAl([FromBody] NoIstek i) { var h = TahsilatOrnek.IslemeAl(i?.No, Ben().AdSoyad); var t = TahsilatOrnek.TahsilatBul(i?.No); return Json(new { success = h == null, message = h ?? $"{t.No} cari hesabına işlendi: {t.Satirlar.Count} ödeme satırı, {t.Dagitim.Select(d => d.BelgeNo).Distinct().Count()} fatura{(t.Avans > 0.005m ? $", {N2(t.Avans)} {t.ParaBirimi} avans" : "")}.{NakitAkisNotu(t)}", tahsilat = TahsilatDto(t) }); }
        [HttpPost]
        public IActionResult Sil([FromBody] NoIstek i) { var h = TahsilatOrnek.Sil(i?.No, Ben().AdSoyad); return Json(new { success = h == null, message = h ?? $"{i.No} taslağı silindi." }); }
        [HttpPost]
        public IActionResult FaturaKes([FromBody] FaturaIstek i)
        {
            if (i == null) return Json(new { success = false, message = "Geçersiz istek." });
            var h = TahsilatOrnek.FaturaKes(i.TahsilatNo, i.Tur, Ben().AdSoyad, out var f);
            if (h != null) return Json(new { success = false, message = h });
            return Json(new { success = true, message = $"{f.No} kesildi: {Tl(f.Toplam)} (KDV dahil) · {f.CariAd} cari hesabına işlendi.", fatura = FaturaDto(f), tahsilat = TahsilatDto(TahsilatOrnek.TahsilatBul(i.TahsilatNo)) });
        }
        /// <summary>Tahsilat almadan önce bu caride kapaması yapılmamış ödemeleri otomatik kapat (Otomatik Kapatma: birebir + FIFO).</summary>
        [HttpPost]
        public IActionResult OtomatikKapat([FromBody] Dictionary<string, string> i)
        {
            var kod = i != null && i.TryGetValue("kod", out var k) ? k : ""; if (string.IsNullOrEmpty(kod)) return Json(new { success = false, message = "Cari seçin." });
            var r = KapamaOrnek.TumunuKapat(new[] { kod }, Ben().AdSoyad);
            return Json(new { success = true, message = r.Eslesme == 0 ? "Bu caride kapatılacak eşleşmemiş ödeme yok." : $"{r.Eslesme} fatura–ödeme eşleşmesi kapatıldı · {Tl(r.ToplamTl)}. Açık faturalar güncellendi.", eslesme = r.Eslesme, toplamTl = r.ToplamTl, hatalar = r.Hatalar });
        }

        // ---------- PDF / Excel ----------
        DaBelge EkstreBelgesi(string kod)
        {
            var c = MutabakatOrnek.Bul(kod) ?? throw new Exception("Cari bulunamadı."); var k = c.Kart; var b = TahsilatOrnek.CariBilgi(kod); var r = CariKartOrnek.RiskHesapla(k);
            var belge = new DaBelge { Baslik = "Cari Hesap Ekstresi", AltBaslik = $"{k.Kod} · {k.ResmiUnvan} · VKN {k.VergiNo} · {k.Sirket}", Sirket = k.Sirket, Hazirlayan = Ben().AdSoyad };
            belge.Ozet.Add(("Bakiye (TL)", Tl(c.BakiyeTl))); belge.Ozet.Add(("Vadesi Geçmiş", Tl(r.VadesiGecmis) + (r.VadesiGecmisAdet > 0 ? $" · {r.VadesiGecmisAdet} fatura · {r.MaxGecikme} gün" : ""))); belge.Ozet.Add(("Son Ödeme", b.SonOdeme.HasValue ? $"{Trh(b.SonOdeme)} · {N2(b.SonOdemeTutar)} {b.SonOdemePb}" : "—")); belge.Ozet.Add(("Son Yükleme", b.SonYukleme.HasValue ? $"{Trh(b.SonYukleme)} · {N2(b.SonYuklemeTutar)} {b.SonYuklemePb}" : "—")); belge.Ozet.Add(("Ort. Ödeme Gecikmesi", $"{b.OrtalamaGecikme.ToString("0.#", Tr)} gün")); belge.Ozet.Add(("Risk", r.Durum));
            foreach (var pb in c.Hareketler.Select(h => h.ParaBirimi).Distinct().OrderBy(p => p == "TL" ? "" : p))
            {
                var l = c.Hareketler.Where(h => h.ParaBirimi == pb).OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList(); var bl = new DaBlok { Baslik = $"Hareketler · {pb}", Kolonlar = new List<string> { "Tarih", "Belge Tipi", "Belge No", "Açıklama", "Vade", "Tip", "Borç", "Alacak", "Bakiye", "Kalan", "Durum" }, Sag = new List<bool> { false, false, false, false, false, false, true, true, true, true, false } };
                foreach (var h in l) bl.Satirlar.Add(new[] { Trh(h.Tarih), h.BelgeTipi, h.BelgeNo, h.Aciklama ?? "", Trh(h.Vade), h.IslemTipi, h.Borc > 0 ? N2(h.Borc) : "", h.Alacak > 0 ? N2(h.Alacak) : "", N2(h.Bakiye), h.Kalan > 0.005m ? N2(h.Kalan) : "", h.KapamaDurumu ?? "" });
                bl.Toplam = new[] { "Toplam", "", "", $"{l.Count} hareket", "", "", N2(l.Sum(h => h.Borc)), N2(l.Sum(h => h.Alacak)), N2(l.Sum(h => h.Borc - h.Alacak)), N2(l.Where(h => h.BelgeTipi == "Fatura").Sum(h => h.Kalan)), "" }; belge.Bloklar.Add(bl);
            }
            var acik = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Kalan > 0.005m).OrderBy(h => h.Vade ?? h.Tarih).ToList(); var ab = new DaBlok { Baslik = "Açık Faturalar", Kolonlar = new List<string> { "Belge No", "Fatura Tarihi", "Vade", "Gecikme", "Tip", "PB", "Tutar", "Kalan", "Kalan (TL)" }, Sag = new List<bool> { false, false, false, true, false, false, true, true, true } };
            foreach (var f in acik) ab.Satirlar.Add(new[] { f.BelgeNo, Trh(f.Tarih), Trh(f.Vade), $"{Math.Max(0, (DateTime.Today - (f.Vade ?? f.Tarih).Date).Days)} gün", f.IslemTipi, f.ParaBirimi, N2(f.Borc > 0 ? f.Borc : f.Alacak), N2(f.Kalan), N2(f.Kalan * KurTl(f.ParaBirimi)) });
            ab.Toplam = new[] { "Toplam", $"{acik.Count} fatura", "", "", "", "", "", "", N2(acik.Sum(f => f.Kalan * KurTl(f.ParaBirimi))) }; belge.Bloklar.Add(ab);
            return belge;
        }
        DaBelge SimulasyonBelgesi(string kod, string pb, string mod, Dictionary<string, decimal> secim, List<TahsilatSatir> sat, string islemTipi, string no, string durum, string aciklama, List<string> tipler = null)
        {
            var k = CariKartOrnek.KartBul(kod) ?? throw new Exception("Cari bulunamadı."); sat = TahsilatOrnek.SatirlariHazirla(sat, pb, DateTime.Today); var d = TahsilatOrnek.Hesapla(kod, pb, mod, secim, sat, tipler); var b = TahsilatOrnek.CariBilgi(kod);
            decimal tutar = sat.Sum(s => s.Tutar), dag = d.Sum(x => x.OdemeTutar);
            var belge = new DaBelge { Baslik = string.IsNullOrEmpty(no) ? $"{Ad} Simülasyonu" : $"{Ad} {no} ({durum})", AltBaslik = $"{k.Kod} · {k.ResmiUnvan} · {k.Sirket} · işlem tipi {islemTipi} · {(mod == "Manuel" ? "manuel fatura seçimi" : mod == "Cari" ? "cariye istinaden (hesaba)" : "otomatik FIFO")}{(string.IsNullOrEmpty(aciklama) ? "" : " · " + aciklama)}", Sirket = k.Sirket, Hazirlayan = Ben().AdSoyad };
            belge.Ozet.Add(($"{Ad} Tutarı", $"{N2(tutar)} {pb}")); belge.Ozet.Add(("TL Karşılığı", Tl(sat.Sum(s => s.TutarTl)))); belge.Ozet.Add(("Kapanan Fatura", $"{d.Select(x => x.BelgeNo).Distinct().Count()} · {N2(dag)} {pb}")); belge.Ozet.Add((OdemeMi ? "Tedarikçiye Avans" : "Avans", $"{N2(tutar - dag)} {pb}")); belge.Ozet.Add(("Vade Farkı", Tl(d.Sum(x => x.VadeFarki)))); belge.Ozet.Add(("Kur Farkı", Tl(d.Sum(x => x.KurFarki)))); belge.Ozet.Add(("Son Ödeme / Yükleme", $"{Trh(b.SonOdeme)} / {Trh(b.SonYukleme)}"));
            var ob = new DaBlok { Baslik = "Ödeme Satırları", Kolonlar = new List<string> { "#", "Yöntem", "Tarih / Vade", "Belge No", "Banka", "PB", "Kur", "Tutar", "TL", "Not" }, Sag = new List<bool> { false, false, false, false, false, false, true, true, true, false } };
            foreach (var s in sat) ob.Satirlar.Add(new[] { s.Sira.ToString(), s.Yontem, Trh(s.Tarih), s.BelgeNo, s.Banka, s.Pb, s.Kur.ToString("N4", Tr) + (s.KurElle ? " (elle)" : ""), N2(s.Tutar), N2(s.TutarTl), s.Not });
            ob.Toplam = new[] { "", "Toplam", "", "", "", "", "", N2(tutar), N2(sat.Sum(s => s.TutarTl)), "" }; belge.Bloklar.Add(ob);
            var sb = new DaBlok { Baslik = "Fatura Kapanış Simülasyonu (FIFO)", Kolonlar = new List<string> { "Fatura", "Fatura Tarihi", "Vade", "Ödeme", "Ödeme Tarihi", "Gün", "Kapanan", "Fatura Kalanı", "Fatura Kuru", "Ödeme Kuru", "Kur Farkı (TL)", "Vade Farkı (TL)" }, Sag = new List<bool> { false, false, false, false, false, true, true, true, true, true, true, true } };
            foreach (var x in d) sb.Satirlar.Add(new[] { x.BelgeNo, Trh(x.FaturaTarihi), Trh(x.Vade), $"#{x.OdemeSira} {x.OdemeYontem}{(string.IsNullOrEmpty(x.OdemeBelge) ? "" : " " + x.OdemeBelge)}", Trh(x.OdemeTarihi), x.GecikmeGun.ToString(), N2(x.Tutar), N2(x.FaturaKalan), TahsilatOrnek.TlMi(pb) ? "" : x.FaturaKuru.ToString("N4", Tr), TahsilatOrnek.TlMi(pb) ? "" : x.TahsilatKuru.ToString("N4", Tr), N2(x.KurFarki), N2(x.VadeFarki) });
            sb.Toplam = new[] { "Toplam", "", "", "", "", "", N2(dag), "", "", "", N2(d.Sum(x => x.KurFarki)), N2(d.Sum(x => x.VadeFarki)) }; sb.Not = $"Gün = ödeme tarihi − fatura vadesi (negatif = erken). Vade farkı = kapanan × kur × %{TahsilatOrnek.VadeFarkiAylikOran.ToString("0.##", Tr)} / 30 × gün (yalnız gün > 0). Kur farkı = kapanan × (ödeme kuru − fatura kuru). KDV %{TahsilatOrnek.KdvOran} fark faturasında eklenir."; belge.Bloklar.Add(sb);
            return belge;
        }
        FileResult Dosya(byte[] b, string ad, bool pdf) => File(b, pdf ? "application/pdf" : "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", ad + (pdf ? ".pdf" : ".xlsx"));
        [HttpGet] public IActionResult EkstrePdf(string kod) => Dosya(DisaAktar.Pdf(EkstreBelgesi(kod)), $"Ekstre_{kod}_{DateTime.Today:yyyyMMdd}", true);
        [HttpGet] public IActionResult EkstreExcel(string kod) => Dosya(DisaAktar.Excel(EkstreBelgesi(kod)), $"Ekstre_{kod}_{DateTime.Today:yyyyMMdd}", false);
        [HttpPost] public IActionResult SimulasyonPdf([FromBody] Istek i) => Dosya(DisaAktar.Pdf(SimulasyonBelgesi(i.Kod, i.Pb ?? "TL", i.Mod, i.Secim, Satirlar(i), i.IslemTipi, "", "", i.Aciklama, i.Tipler)), $"{DosyaOnEk}Simulasyon_{i.Kod}_{DateTime.Today:yyyyMMdd}", true);
        [HttpPost] public IActionResult SimulasyonExcel([FromBody] Istek i) => Dosya(DisaAktar.Excel(SimulasyonBelgesi(i.Kod, i.Pb ?? "TL", i.Mod, i.Secim, Satirlar(i), i.IslemTipi, "", "", i.Aciklama, i.Tipler)), $"{DosyaOnEk}Simulasyon_{i.Kod}_{DateTime.Today:yyyyMMdd}", false);
        [HttpGet] public IActionResult TahsilatPdf(string no) { var t = TahsilatOrnek.TahsilatBul(no); if (t == null) return NotFound(); return Dosya(DisaAktar.Pdf(KayitBelgesi(t)), $"{(t.Odeme ? "Odeme" : "Tahsilat")}_{t.No}", true); }
        [HttpGet] public IActionResult TahsilatExcel(string no) { var t = TahsilatOrnek.TahsilatBul(no); if (t == null) return NotFound(); return Dosya(DisaAktar.Excel(KayitBelgesi(t)), $"{(t.Odeme ? "Odeme" : "Tahsilat")}_{t.No}", false); }
        DaBelge KayitBelgesi(TahsilatKaydi t)
        {
            string ad = t.Odeme ? "Ödeme" : "Tahsilat";
            var k = CariKartOrnek.KartBul(t.CariKodu); var belge = new DaBelge { Baslik = $"{ad} Makbuzu {t.No}", AltBaslik = $"{t.CariKodu} · {k?.ResmiUnvan ?? t.CariAd} · {t.Sirket} · {Trh(t.Tarih)} · {t.Durum}{(t.Islendi ? $" ({t.IslemeAlan} {Zmn(t.IslemeZamani)})" : "")} · işlem tipi {t.IslemTipi} · {(t.Mod == "Manuel" ? "manuel fatura seçimi" : t.Mod == "Cari" ? "cariye istinaden (hesaba)" : "otomatik FIFO")}{(string.IsNullOrEmpty(t.Aciklama) ? "" : " · " + t.Aciklama)}", Sirket = t.Sirket, Hazirlayan = t.Kullanici };
            belge.Ozet.Add(($"{ad} Tutarı", $"{N2(t.Tutar)} {t.ParaBirimi}")); belge.Ozet.Add(("TL Karşılığı", Tl(t.TutarTl))); belge.Ozet.Add(("Kapanan Fatura", $"{t.Dagitim.Select(x => x.BelgeNo).Distinct().Count()} · {N2(t.Dagitilan)} {t.ParaBirimi}")); belge.Ozet.Add((t.Odeme ? "Tedarikçiye Avans" : "Avans", $"{N2(t.Avans)} {t.ParaBirimi}")); belge.Ozet.Add(("Vade Farkı", Tl(t.VadeFarki) + (string.IsNullOrEmpty(t.VadeFarkiFaturaNo) ? "" : " · " + t.VadeFarkiFaturaNo))); belge.Ozet.Add(("Kur Farkı", Tl(t.KurFarki) + (string.IsNullOrEmpty(t.KurFarkiFaturaNo) ? "" : " · " + t.KurFarkiFaturaNo)));
            var ob = new DaBlok { Baslik = "Ödeme Satırları", Kolonlar = new List<string> { "#", "Yöntem", "Tarih / Vade", "Belge No", "Banka", "PB", "Kur", "Tutar", "TL", "Cari Hareket" }, Sag = new List<bool> { false, false, false, false, false, false, true, true, true, false } };
            foreach (var s in t.Satirlar) ob.Satirlar.Add(new[] { s.Sira.ToString(), s.Yontem, Trh(s.Tarih), s.BelgeNo, s.Banka, s.Pb, s.Kur.ToString("N4", Tr), N2(s.Tutar), N2(s.TutarTl), s.HareketNo + (s.NakitAkisDocEntry != null ? $" · nakit akış #{s.NakitAkisDocEntry}" : "") }); ob.Toplam = new[] { "", "Toplam", "", "", "", "", "", N2(t.Tutar), N2(t.TutarTl), "" }; belge.Bloklar.Add(ob);
            var sb = new DaBlok { Baslik = "Kapatılan Faturalar", Kolonlar = new List<string> { "Fatura", "Fatura Tarihi", "Vade", "Ödeme", "Ödeme Tarihi", "Gün", "Kapanan", "Fatura Kalanı", "Kur Farkı (TL)", "Vade Farkı (TL)" }, Sag = new List<bool> { false, false, false, false, false, true, true, true, true, true } };
            foreach (var x in t.Dagitim) sb.Satirlar.Add(new[] { x.BelgeNo, Trh(x.FaturaTarihi), Trh(x.Vade), $"#{x.OdemeSira} {x.OdemeYontem}{(string.IsNullOrEmpty(x.OdemeBelge) ? "" : " " + x.OdemeBelge)}", Trh(x.OdemeTarihi), x.GecikmeGun.ToString(), N2(x.Tutar), N2(x.FaturaKalan), N2(x.KurFarki), N2(x.VadeFarki) }); sb.Toplam = new[] { "Toplam", "", "", "", "", "", N2(t.Dagitilan), "", N2(t.KurFarki), N2(t.VadeFarki) }; belge.Bloklar.Add(sb);
            return belge;
        }

        /// <summary>Konsolide Tahsilat Takip (Rapor17): şirket bazında müşteri carilerinin açık fatura vade kırılımı, içerideki çek / senet, son 30 gün tahsilatı, son tahsilat, risk ve temsilci; haftalık beklenen tahsilat planı için fatura vadeleri.</summary>
        [HttpGet]
        public IActionResult KonsolideTakip()
        {
            TahsilatOrnek.AySonuOtomatik();   // ay döndüyse geçen ay sonunun vade farkı faturaları otomatik kesilir (bir kez)
            EMutabakatOrnek.Hazirla();        // mutabakat durumu sütunu için e-mutabakat örnek geçmişi hazır olsun
            var bugun = DateTime.Today; var son30 = bugun.AddDays(-30); var aySonu = TahsilatOrnek.AySonu(bugun); var tumFaturalar = TahsilatOrnek.Faturalar; var tumTahsilatlar = TahsilatOrnek.Tahsilatlar;
            var notlar = TahsilatTakipKayit.Notlar(); string ben = Ben().AdSoyad; var ayBasi = new DateTime(bugun.Year, bugun.Month, 1);
            // aylık süreç (kullanıcı 17 Kas 2026): 2. – 3. hafta arama (ayın 8 – 21'i), son hafta (ay sonundan önceki 6 gün + ay sonu) vade farkı robotu
            DateTime aramaBas = ayBasi.AddDays(7), aramaBit = ayBasi.AddDays(20), robotBas = aySonu.AddDays(-6);
            var mizan = OrnekVeri.MusteriMizani();   // uyum: mizan alt hesapları bir kez hesaplanır
            var satirlar = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").Select(k =>
            {
                var c = MutabakatOrnek.Bul(k.Kod); var r = CariKartOrnek.RiskHesapla(k); var vf = VadeFarkiOzet(k, c, tumFaturalar, tumTahsilatlar, bugun, aySonu);
                var acik = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Kalan > 0.005m).ToList();
                var kalemler = acik.Select(h => new { vade = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), tarih = h.Tarih.ToString("yyyy-MM-dd"), gun = ((h.Vade ?? h.Tarih).Date - bugun).Days, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), belge = h.BelgeNo, pb = h.ParaBirimi, tutar = h.Borc, kalan = h.Kalan, islemTipi = h.IslemTipi, tur = (h.BelgeNo ?? "").StartsWith("VFF-") ? "Vade Farkı" : (h.BelgeNo ?? "").StartsWith("KFF-") ? "Kur Farkı" : "Satış", faturaKuru = TahsilatOrnek.FaturaKuru(h), bazKur = KurDegerlemeOrnek.BazKur(k.Kod, h).Kur, bazKaynak = KurDegerlemeOrnek.BazKur(k.Kod, h).Kaynak }).OrderBy(x => x.vade).ToList();
                // 20 Kas 2026 (kullanıcı: "çek / senet tutarına tıkladığımda hemen o çekin bilgileri gelmeli"): portföydeki (vadesi bekleyen) çek / senetler kalem kalem — Çek / Senet sütunundaki tutarla aynı küme (CariKartOrnek risk hesabı)
                var cekler = c == null ? new List<object>() : c.Hareketler.Where(h => (h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.KapamaDurumu == "Vade Bekliyor").OrderBy(h => h.Vade ?? h.Tarih).Select(h => (object)new { belge = h.BelgeNo, tip = h.BelgeTipi, tarih = h.Tarih.ToString("yyyy-MM-dd"), vade = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), gun = ((h.Vade ?? h.Tarih).Date - bugun).Days, pb = h.ParaBirimi, tutar = h.Alacak, tl = Math.Round(h.Alacak * KurTl(h.ParaBirimi), 2), aciklama = h.Aciklama, islemTipi = h.IslemTipi }).ToList();
                var kurOz = KurOnizlemeDto(k.Kod);   /* Vade Farkı Takibi › Kur Değerlemesi (R) sütunu: değerleme bekleyen fark, son değerleme */
                var ag = IbkbOrnek.AcikGorev(k.Kod, "Tahsilat"); var sk = IbkbOrnek.SonKontrol(k.Kod, "Tahsilat");   /* Muhasebe Uyumu hücresi altı: açık düzeltme görevi / son kontrol (19 Kas 2026) */
                var uyumGorev = ag == null ? null : new { ag.Id, ag.Durum, ag.Atanan, termin = ag.Termin?.ToString("yyyy-MM-dd") ?? "", gecikti = ag.Termin.HasValue && ag.Termin.Value.Date < bugun, ag.Oncelik };
                var uyumKontrol = sk == null ? null : new { tarih = sk.Tarih.ToString("yyyy-MM-dd"), sk.Durum, sk.Kullanici };
                // BAKİYE MUTABAKATI (19 Kas 2026, kullanıcı: "sütunları topladığımda bakiyeye ulaşmalıyım, her şeyi kalem kalem görebilmeliyim"):
                //   Bakiye = Açık Faturalar (kalan) + Fatura Dışı Borç (kur farkı dekontu, devir, iade … — fatura olmayan borç hareketleri) − Kapatılmamış Ödeme / Avans (faturalarla eşlenmemiş alacak)
                //   fdb: fatura dışı borç kalemleri; odemeAcik: kapaması yapılmamış ödemeler (alacak hareketinin Kalan'ı). Kapatılmamış ödeme ekranda kalan olarak hesaplanır (açık + fatura dışı − bakiye); listeyle farkı "kapama farkı" satırıdır.
                var fdb = c == null ? new List<object>() : c.Hareketler.Where(h => h.BelgeTipi != "Fatura" && h.Borc > 0).OrderBy(h => h.Tarih).Select(h => (object)new { belge = h.BelgeNo, tip = h.BelgeTipi, tarih = h.Tarih.ToString("yyyy-MM-dd"), pb = h.ParaBirimi, tutar = h.Borc, tl = Math.Round(h.Borc * KurTl(h.ParaBirimi), 2), aciklama = h.Aciklama, islemTipi = h.IslemTipi }).ToList();
                // Müşteri Cari Kartları sekmesi (19 Kas 2026) ve Aylık Süreç "Son Kesilen Fatura" sütunu için kart alanları ve son satış faturası
                var anaF = k.Faaliyetler.FirstOrDefault(f => f.AnaFaaliyet) ?? k.Faaliyetler.FirstOrDefault(); var lim = vf.Limit;
                var kart = new { k.VergiNo, k.VergiDairesi, k.Sehir, k.Rol, esCari = k.EsCariKodu, anaFaaliyet = anaF == null ? "" : $"{anaF.AnaGrup} › {anaF.Grup}", durum = k.Durum, k.Telefon, k.Eposta, k.Yetkili, olusturma = k.OlusturmaTarihi.ToString("yyyy-MM-dd"), limit = lim.Limit, limitTur = lim.Tur, limitYuzde = lim.KullanimYuzde, limitDurum = lim.Durum, limitRisk = lim.ToplamRisk };
                var sonF = c == null ? null : c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && !(h.BelgeNo ?? "").StartsWith("VFF-") && !(h.BelgeNo ?? "").StartsWith("KFF-")).OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id).FirstOrDefault();
                var sonFatura = sonF == null ? null : new { tarih = sonF.Tarih.ToString("yyyy-MM-dd"), belge = sonF.BelgeNo, pb = sonF.ParaBirimi, tutar = sonF.Borc, tl = Math.Round(sonF.Borc * KurTl(sonF.ParaBirimi), 2), kalan = sonF.Kalan, vade = (sonF.Vade ?? sonF.Tarih).ToString("yyyy-MM-dd") };
                var odemeAcik = c == null ? new List<object>() : c.Hareketler.Where(h => h.Alacak > 0 && h.Kalan > 0.005m).OrderBy(h => h.Tarih).Select(h => (object)new { belge = h.BelgeNo, tip = h.BelgeTipi, tarih = h.Tarih.ToString("yyyy-MM-dd"), pb = h.ParaBirimi, tutar = h.Alacak, kalan = h.Kalan, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), aciklama = h.Aciklama, islemTipi = h.IslemTipi, durum = h.KapamaDurumu }).ToList();
                decimal Kova(int a, int b) => kalemler.Where(x => x.gun >= a && x.gun <= b).Sum(x => x.tl);
                var tahs = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.Alacak > 0).ToList();
                var sonTahsilat = tahs.OrderByDescending(h => h.Tarih).FirstOrDefault();
                // Kapama Robotu sekmesi (19 Kas 2026): eşleşmemiş ödemeler, açık faturalar, önerilen eşleşme (birebir + FIFO), son kapama
                object kapama = null;
                if (c != null) { List<KapamaCifti> kp; List<MtHareket> kpO, kpF; lock (MutabakatOrnek.Kilit) { kp = KapamaOrnek.Oner(c); kpO = KapamaOrnek.AcikOdemeler(c); kpF = KapamaOrnek.AcikFaturalar(c); } var sonK = KapamaOrnek.Kayitlar.FirstOrDefault(x => x.CariKodu == k.Kod);
                    kapama = new { oneri = kp.Count, oneriTl = Math.Round(kp.Sum(x => x.Tutar * KurTl(x.Pb)), 2), birebir = kp.Count(x => x.Yontem == "Birebir"), fifo = kp.Count(x => x.Yontem == "FIFO"), odemeAdet = kpO.Count, odemeTl = Math.Round(kpO.Sum(h => h.Kalan * KurTl(h.ParaBirimi)), 2), faturaAdet = kpF.Count, faturaTl = Math.Round(kpF.Sum(h => h.Kalan * KurTl(h.ParaBirimi)), 2), sonKapama = sonK == null ? "" : sonK.Zaman.ToString("yyyy-MM-dd HH:mm"), sonKapamaKim = sonK?.Kullanici ?? "", sonKapamaAdet = sonK?.Ciftler.Count ?? 0, sonKapamaTl = sonK == null ? 0 : Math.Round(sonK.Ciftler.Sum(x => x.Tutar * KurTl(x.Pb)), 2) }; }
                return new { suzgec = CariSuzgec.Alanlar(k, r), k.Kod, k.KisaAd, k.ResmiUnvan, k.Sirket, sirketDb = CekAkibetOrnek.DbName(k.Sirket), k.Bolge, k.Sinif, k.VadeSarti, temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), risk = r.Durum, riskNeden = r.Neden, bakiye = r.AcikBakiye, bakiyeX = r.BakiyeX, bakiyeR = r.BakiyeR,
                    gecmis = Kova(int.MinValue, -1), gecmisAdet = kalemler.Count(x => x.gun < 0), maxGecikme = r.MaxGecikme, buHafta = Kova(0, 7), g8_30 = Kova(8, 30), g31_60 = Kova(31, 60), g60 = Kova(61, int.MaxValue), acikToplam = kalemler.Sum(x => x.tl), acikAdet = kalemler.Count,
                    cek = r.Cek, cekAdet = r.CekAdet, son30Tahsilat = Math.Round(tahs.Where(h => h.Tarih >= son30).Sum(h => h.Alacak * KurTl(h.ParaBirimi)), 2), son30Adet = tahs.Count(h => h.Tarih >= son30), sonTahsilat = sonTahsilat == null ? "" : Trh(sonTahsilat.Tarih), sonTahsilatTl = sonTahsilat == null ? 0 : Math.Round(sonTahsilat.Alacak * KurTl(sonTahsilat.ParaBirimi), 2),
                    limitDurum = vf.Limit.Durum, sonTahsilatIso = sonTahsilat == null ? "" : sonTahsilat.Tarih.ToString("yyyy-MM-dd"), iliski = k.IliskiDurumu, ulke = k.Ulke, yetkili = k.Yetkili, vf = vf.Dto, takip = TakipOzet(k, c, notlar, bugun, ben), uyum = UyumOzet(k, c, mizan), kalemler, cekler, fdb, odemeAcik, kart, sonFatura, kapama, kur = kurOz, uyumGorev, uyumKontrol };
            }).ToList();
            return Json(new { success = true, bugun = bugun.ToString("yyyy-MM-dd"), aySonu = aySonu.ToString("yyyy-MM-dd"), sonOtomatikAySonu = TahsilatOrnek.SonOtomatikAySonu?.ToString("yyyy-MM-dd") ?? "",
                /* 19 Kas 2026 (kullanıcı: "Vade Farkı Takibi'nde 1–12 yaz, robot çalıştıysa / ay sonu vade farkı faturaları kesildiyse yeşil olsun"): bu yılın ayları, o dönem "Ay Sonu" kaynaklı vade farkı faturası var mı */
                vfAylar = Enumerable.Range(1, 12).Select(m => { string dn = $"{bugun.Year}-{m:00}"; var f = tumFaturalar.Where(x => x.Tur == "Vade Farkı" && x.Kaynak == "Ay Sonu" && x.Donem == dn).ToList(); var son = new DateTime(bugun.Year, m, DateTime.DaysInMonth(bugun.Year, m)); return new { ay = m, donem = dn, ad = new DateTime(bugun.Year, m, 1).ToString("MMMM yyyy", Tr), kesildi = f.Count > 0, adet = f.Count, musteri = f.Select(x => x.CariKodu).Distinct().Count(), tutar = f.Sum(x => x.Toplam), sonTarih = f.Count == 0 ? "" : Trh(f.Max(x => x.Tarih)), gecti = son < bugun, buAy = m == bugun.Month, bas = $"{dn}-01", bit = son.ToString("yyyy-MM-dd") }; }).ToList(),
                surec = new { donem = bugun.ToString("yyyy-MM"), donemAd = bugun.ToString("MMMM yyyy", Tr), aramaBas = aramaBas.ToString("yyyy-MM-dd"), aramaBit = aramaBit.ToString("yyyy-MM-dd"), robotBas = robotBas.ToString("yyyy-MM-dd"), asama = bugun < aramaBas ? "Ay Başı" : bugun <= aramaBit ? "Arama" : bugun < robotBas ? "Arama Sonrası" : "Robot" },
                notlar = notlar.Select(n => NotDto(n, ben)).ToList(), kanallar = TahsilatTakipKayit.Kanallar, sonuclar = TahsilatTakipKayit.Sonuclar, ben,
                iletisimler = TahsilatTakipKayit.Iletisimler().Take(400).Select(IletisimDto).ToList(), talepler = TahsilatTakipKayit.Talepler().Select(TalepDto).ToList(), otoYanit = TahsilatTakipKayit.OtoYanit, icerikTurleri = TahsilatTakipKayit.IcerikTurleri,
                kurlar = TahsilatOrnek.Kurlar, kurTarihi = Trh(MutabakatOrnek.KurTarihi),   /* "$ PB Getir": bütün tutarlar USD kuruyla dolara çevrilir (19 Kas 2026) */
                vadeSartlari = CariKartOrnek.VadeSartlari,   /* Müşteri Cari Kartları › Vade hücresinde hızlı düzenleme */
                sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, db = s.DbName }), satirlar, tahsilatlar = TahsilatOrnek.Tahsilatlar.Select(TahsilatDto), faturalar = FarkBelgeleri(), vadeFarkiAylikOran = TahsilatOrnek.VadeFarkiAylikOran, kdvOran = TahsilatOrnek.KdvOran,
                // 19 Kas 2026 (kullanıcı: "Aylık Tahsilatlar'ı İhracat Finansal Takibi ile senkronize et; yurt içi / yurt dışı birleşimi"): İhracat Finansal Takibi'nin banka tahsilatları (döviz girişi) TL karşılığıyla
                ihracatTahsilat = IbkbOrnek.TahsilatListesi().Select(x => new { x.T.Id, tarihIso = x.T.Tarih?.ToString("yyyy-MM-dd") ?? "", x.T.Sirket, x.T.Banka, x.T.MusteriKodu, musteriAd = CariKartOrnek.KartBul(x.T.MusteriKodu ?? "")?.KisaAd ?? x.T.Gonderen, x.T.Gonderen, x.T.Tutar, pb = x.T.Doviz == "TRY" ? "TL" : x.T.Doviz, tl = Math.Round(x.T.Tutar * KurTl(x.T.Doviz == "TRY" ? "TL" : x.T.Doviz), 2), x.T.Durum, x.T.Kaynak, x.T.Referans, dosyaNo = x.DosyaNo, belgeNo = x.BelgeNo }).ToList() });
        }

        // ---------- VADE FARKI TAKİBİ (16 Kas 2026; müşteri bazında) ----------
        // Kullanıcı: "vade farkı raporu çok önemli … vadesi geçmişleri, mevcut ay sonu vadesi gelecek bakiyeleri, toplam müşteri bakiyesini görebilmek; her ay sonu vadesi geçen bakiyelere otomatik vade farkı
        // faturası kesilmeli; cariye tıkladığımda detayını görebilmeliyim, ne kadar vade farkı kesilmiş; en son ne zaman ödeme yapmış; vade farkı faturası kesildikten sonra ödeme yapmadı ve hâlâ fatura kesiliyor mu;
        // limitleri aşmış mı, aştıysa satış durdur uyarısı buradan verilebilmeli; tahsilat alınırken vade farkı faturası kesilebilmeli; buradan otomatik mutabakat yapılabilmeli, doğrudan müşteriye; mizan raporundan bakiyeler gelsin."
        //   Bakiye: müşterinin 120.xxx hesabının mizan bakiyesi (borç − alacak, TL karşılığı + döviz kırılımı; kaynak cari hesap hareketleri — İhracat Finansal Takibi › Cari Bakiyeler (Mizan) ile aynı).
        //   Ay sonu otomatik kesim ve çift sayım kuralı: Data/TahsilatOrnek.cs (MusteriVfKalemleri / MusteriVadeFarkiKes / AySonuOtomatik).
        public class KodlarIstek { public List<string> Kodlar { get; set; } public string Kanal { get; set; } }
        /// <summary>VİRMAN (19 Kas 2026): Yeni Tahsilat › Açık Faturalar'daki kapatılmamış ödeme / avans satırı açık faturalara kapatılır (birebir + FIFO, aynı para birimi); Belge boşsa carinin bütün kapatılmamış ödemeleri.</summary>
        public class VirmanIstek { public string Kod { get; set; } public string Belge { get; set; } }
        [HttpPost]
        public IActionResult OdemeKapat([FromBody] VirmanIstek i)
        {
            var c = MutabakatOrnek.Bul(i?.Kod ?? ""); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." });
            List<KapamaCifti> ciftler; lock (MutabakatOrnek.Kilit) ciftler = KapamaOrnek.Oner(c, null, string.IsNullOrEmpty(i.Belge) ? null : new[] { i.Belge });
            if (ciftler.Count == 0) return Json(new { success = false, message = string.IsNullOrEmpty(i.Belge) ? "Kapatılacak eşleşme yok (aynı para biriminde açık fatura bulunamadı)." : $"{i.Belge} için aynı para biriminde açık fatura yok; virman yapılamadı." });
            var h = KapamaOrnek.Kapat(c.CariKodu, ciftler, "Virman", Ben().AdSoyad, out var kayit); if (h != null) return Json(new { success = false, message = h });
            decimal tl = Math.Round(kayit.Ciftler.Sum(x => x.Tutar * KurTl(x.Pb)), 2);
            return Json(new { success = true, message = $"Virman: {kayit.Ciftler.Count} fatura–ödeme eşleşmesi kapatıldı · {Tl(tl)} ({string.Join(", ", kayit.Ciftler.Select(x => x.FaturaNo + " ← " + x.OdemeNo).Take(4))}{(kayit.Ciftler.Count > 4 ? " …" : "")}).", eslesme = kayit.Ciftler.Count, toplamTl = tl });
        }
        // ---------- KAPAMA ROBOTU (19 Kas 2026; kullanıcı: "filtreleyerek ya da cariyi arayarak toplu veya tek cari bazlı mutabakat kapama robotu") ----------
        /// <summary>Bir carinin eşleşme önerisi (birebir + FIFO), eşleşmemiş ödemeleri ve açık faturaları — pencere.</summary>
        [HttpGet]
        public IActionResult KapamaOneri(string kod)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); var c = MutabakatOrnek.Bul(kod ?? ""); if (k == null || c == null) return Json(new { success = false, message = "Cari bulunamadı." });
            List<KapamaCifti> kp; List<MtHareket> od, fa; lock (MutabakatOrnek.Kilit) { kp = KapamaOrnek.Oner(c); od = KapamaOrnek.AcikOdemeler(c); fa = KapamaOrnek.AcikFaturalar(c); }
            MtHareket H(string no) => c.Hareketler.FirstOrDefault(h => h.BelgeNo == no);
            return Json(new
            {
                success = true, kod = k.Kod, kisaAd = k.KisaAd,
                ciftler = kp.Select(x => { var f = H(x.FaturaNo); var o = H(x.OdemeNo); return new { x.FaturaNo, faturaTarih = Trh(f?.Tarih), vade = Trh(f?.Vade ?? f?.Tarih), faturaKalan = f?.Kalan ?? 0, x.OdemeNo, odemeTip = o?.BelgeTipi ?? "", odemeTarih = Trh(o?.Tarih), odemeKalan = o?.Kalan ?? 0, x.Tutar, x.Pb, tl = Math.Round(x.Tutar * KurTl(x.Pb), 2), x.Yontem, gun = f == null || o == null ? 0 : (o.Tarih.Date - (f.Vade ?? f.Tarih).Date).Days }; }),
                odemeler = od.Select(h => new { h.BelgeNo, tip = h.BelgeTipi, tarih = Trh(h.Tarih), h.ParaBirimi, tutar = h.Alacak, h.Kalan, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), h.Aciklama }),
                faturalar = fa.Select(h => new { h.BelgeNo, tarih = Trh(h.Tarih), vade = Trh(h.Vade ?? h.Tarih), h.ParaBirimi, tutar = h.Borc, h.Kalan, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), gun = (DateTime.Today - (h.Vade ?? h.Tarih).Date).Days, h.IslemTipi }),
                gecmis = KapamaOrnek.Kayitlar.Where(x => x.CariKodu == k.Kod).Take(10).Select(x => new { zaman = x.Zaman.ToString("dd.MM.yyyy HH:mm"), x.Mod, adet = x.Ciftler.Count, tl = Math.Round(x.Ciftler.Sum(y => y.Tutar * KurTl(y.Pb)), 2), x.Kullanici })
            });
        }
        /// <summary>Seçilen carilerde (ya da tek caride) robotu çalıştırır: öneri uygulanır, kalanlar düşer, kart günlüğüne yazılır.</summary>
        [HttpPost]
        public IActionResult KapamaRobotu([FromBody] KodlarIstek i)
        {
            var kodlar = (i?.Kodlar ?? new List<string>()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList(); if (kodlar.Count == 0) return Json(new { success = false, message = "Cari seçin." });
            var r = KapamaOrnek.TumunuKapat(kodlar, Ben().AdSoyad);
            return Json(new { success = true, cari = r.Cari, eslesme = r.Eslesme, toplamTl = r.ToplamTl, hatalar = r.Hatalar, message = r.Eslesme == 0 ? $"{kodlar.Count} caride kapatılacak eşleşme yok (eşleşmemiş ödeme ya da aynı para biriminde açık fatura bulunamadı)." : $"Kapama robotu: {r.Cari} caride {r.Eslesme} fatura–ödeme eşleşmesi kapatıldı · {Tl(r.ToplamTl)}." + (r.Hatalar.Count > 0 ? $" {r.Hatalar.Count} hata: {string.Join("; ", r.Hatalar.Take(3))}" : "") });
        }
        /// <summary>Carinin tahsilat hareketleri (havale, çek, senet, nakit; alacak) — tarih aralığı; Aylık Süreç / Alacak Takibi / Vade Farkı Takibi'nde tutara tıklayınca pencere.</summary>
        [HttpGet]
        public IActionResult CariOdemeler(string kod, string bas, string bit)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); var c = MutabakatOrnek.Bul(kod ?? ""); if (k == null || c == null) return Json(new { success = false, message = "Cari bulunamadı." });
            DateTime? b1 = string.IsNullOrEmpty(bas) ? null : TarihCoz(bas, DateTime.MinValue), b2 = string.IsNullOrEmpty(bit) ? null : TarihCoz(bit, DateTime.MaxValue);
            List<MtHareket> l; lock (MutabakatOrnek.Kilit) l = c.Hareketler.Where(h => (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.Alacak > 0 && (b1 == null || h.Tarih.Date >= b1) && (b2 == null || h.Tarih.Date <= b2)).OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id).ToList();
            var th = TahsilatOrnek.Tahsilatlar.Where(t => t.CariKodu == k.Kod && t.Islendi).SelectMany(t => t.Satirlar.Select(s => (s.HareketNo, t.No))).Where(x => !string.IsNullOrEmpty(x.HareketNo)).GroupBy(x => x.HareketNo).ToDictionary(g => g.Key, g => g.First().No);
            return Json(new { success = true, kod = k.Kod, kisaAd = k.KisaAd, bas = b1?.ToString("dd.MM.yyyy") ?? "", bit = b2?.ToString("dd.MM.yyyy") ?? "", toplamTl = Math.Round(l.Sum(h => h.Alacak * KurTl(h.ParaBirimi)), 2), adet = l.Count,
                hareketler = l.Select(h => new { tarih = Trh(h.Tarih), tarihIso = h.Tarih.ToString("yyyy-MM-dd"), tip = h.BelgeTipi, h.BelgeNo, h.ParaBirimi, tutar = h.Alacak, tl = Math.Round(h.Alacak * KurTl(h.ParaBirimi), 2), kalan = h.Kalan, durum = h.Kalan > 0.005m ? "Kapama Bekliyor" : (h.KapamaDurumu == "Vade Bekliyor" ? "Vade Bekliyor" : "Kapandı"), vade = h.Vade.HasValue ? Trh(h.Vade) : "", h.Aciklama, h.IslemTipi, tahsilatNo = th.TryGetValue(h.BelgeNo ?? "", out var tn) ? tn : "" }) });
        }
        // 19 Kas 2026 (kullanıcı: "Vade Farkı Takibi'ne vade farkı oranı sütun olarak ekle, kolaylıkla değiştirilebilsin"): müşteriye özel aylık oran; 0 / boş → varsayılana döner; kart günlüğüne yazılır
        public class OranIstek { public List<string> Kodlar { get; set; } public decimal? Oran { get; set; } }
        [HttpPost]
        public IActionResult OranKaydet([FromBody] OranIstek i)
        {
            var kodlar = (i?.Kodlar ?? new List<string>()).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList(); if (kodlar.Count == 0) return Json(new { success = false, message = "Müşteri seçin." });
            if (i.Oran != null && (i.Oran < 0 || i.Oran > 30)) return Json(new { success = false, message = "Aylık vade farkı oranı % 0 – 30 arasında olmalı (0 = varsayılan)." });
            var kisi = Ben().AdSoyad; int n = 0;
            foreach (var kod in kodlar) { var k = CariKartOrnek.KartBul(kod); if (k == null) continue; var eski = TahsilatOrnek.Oran(kod); TahsilatTakipKayit.OranKaydet(kod, i.Oran); var yeni = TahsilatOrnek.Oran(kod); n++; CariKartOrnek.Gunluk(k, DateTime.Now, kisi, "Vade Farkı Oranı", $"% {eski.ToString("0.##", Tr)}", i.Oran == null || i.Oran <= 0 ? $"% {yeni.ToString("0.##", Tr)} (varsayılan)" : $"% {yeni.ToString("0.##", Tr)} (müşteriye özel)", "Tahsilat › Vade Farkı Takibi"); }
            return Json(new { success = n > 0, message = n == 0 ? "Müşteri bulunamadı." : i.Oran == null || i.Oran <= 0 ? $"{n} müşteri varsayılan orana (% {TahsilatOrnek.VadeFarkiAylikOran.ToString("0.##", Tr)}) döndü." : $"{n} müşteri için aylık vade farkı oranı % {i.Oran.Value.ToString("0.##", Tr)} kaydedildi; ay sonu kesimi, tahsilat kapaması ve adat bu oranla hesaplanır." });
        }
        public class SatisDurdurIstek { public string Kod { get; set; } public bool Durdur { get; set; } public string Neden { get; set; } }
        static bool OdemeHareketi(MtHareket h) => (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.Alacak > 0;
        /// <summary>"Vade farkı sonrası" karşılaştırmasının dayandığı fatura: bugünden ÖNCE kesilmiş en son vade farkı faturası (bugün kesilen faturadan sonra ödeme beklenemez); yoksa en son fatura.</summary>
        static FarkFaturasi VffReferans(List<FarkFaturasi> vff, DateTime bugun) => vff.Where(f => f.Tarih.Date < bugun).OrderByDescending(f => f.Tarih).ThenByDescending(f => f.Zaman).FirstOrDefault() ?? vff.OrderByDescending(f => f.Tarih).ThenByDescending(f => f.Zaman).FirstOrDefault();
        /// <summary>Hareket referans faturadan sonra mı: sonraki günler; tahsilat anında kesilen faturada aynı günün ödemesi de sayılır (fatura o tahsilatla birlikte kesilmiştir).</summary>
        static bool VffSonrasi(MtHareket h, FarkFaturasi f) => h.Tarih.Date > f.Tarih.Date || (f.Kaynak == "Tahsilat" && h.Tarih.Date == f.Tarih.Date);
        /// <summary>Müşterinin vade farkı / limit / satış durumu / mutabakat özeti (rapor satırı ve detay penceresi ortak).</summary>
        static (object Dto, CariLimit Limit) VadeFarkiOzet(CariKart k, MtCari c, List<FarkFaturasi> tumFaturalar, List<TahsilatKaydi> tumTahsilatlar, DateTime bugun, DateTime aySonu)
        {
            var lim = CariKartOrnek.LimitHesapla(k); List<MtHareket> hareketler; if (c == null) hareketler = new List<MtHareket>(); else lock (MutabakatOrnek.Kilit) hareketler = c.Hareketler.ToList();
            var acik = hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Kalan > 0.005m).ToList();
            decimal aySonuGelecek = Math.Round(acik.Where(h => (h.Vade ?? h.Tarih).Date >= bugun && (h.Vade ?? h.Tarih).Date <= aySonu).Sum(h => h.Kalan * KurTl(h.ParaBirimi)), 2);
            // 19 Kas 2026 gece: yurtdışı müşteride ay sonu kesimi yok → aySonuVf 0, vfKural "Tahsilatta"; bugunVf bilgi (tahsilat işlenirken kesilebilecek, bugüne doğan)
            bool aySonuKesilir = TahsilatOrnek.AySonuKesilir(k);
            var vfAy = aySonuKesilir ? TahsilatOrnek.MusteriVfKalemleri(k.Kod, aySonu) : new List<TahsilatOrnek.VfKalem>(); var vfBugun = TahsilatOrnek.MusteriVfKalemleri(k.Kod, bugun);
            var vff = tumFaturalar.Where(f => f.CariKodu == k.Kod && f.Tur == "Vade Farkı").ToList(); var vffSon = VffReferans(vff, bugun);
            decimal vffAcik = Math.Round(hareketler.Where(h => h.BelgeTipi == "Fatura" && (h.BelgeNo ?? "").StartsWith("VFF-")).Sum(h => h.Kalan), 2);
            var sonrasiOdeme = vffSon == null ? new List<MtHareket>() : hareketler.Where(h => OdemeHareketi(h) && VffSonrasi(h, vffSon)).ToList();
            var sonrasiSatis = vffSon == null ? new List<MtHareket>() : hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && !TahsilatOrnek.VfDisi(h) && h.Tarih.Date > vffSon.Tarih.Date).ToList();
            var sonOdeme = hareketler.Where(OdemeHareketi).OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id).FirstOrDefault();
            var tahsVf = tumTahsilatlar.Where(t => t.CariKodu == k.Kod && t.Islendi && t.VadeFarki > 0.005m && string.IsNullOrEmpty(t.VadeFarkiFaturaNo)).ToList();
            var emSon = c == null ? null : EMutabakatOrnek.Son(c);
            var dto = new
            {
                mizanHesap = k.Kod, mizanBakiye = c?.BakiyeTl ?? 0, mizanDoviz = c == null ? "" : string.Join(" · ", c.Dovizler.Where(d => d.Net != 0).Select(d => $"{d.Net.ToString("N2", Tr)} {d.ParaBirimi}")),
                aySonuGelecek, aySonuVf = vfAy.Sum(x => x.Tutar), aySonuVfAdet = vfAy.Count, bugunVf = vfBugun.Sum(x => x.Tutar), bugunVfAdet = vfBugun.Count, vfKural = TahsilatOrnek.VfKural(k),
                kesilen = vff.Sum(f => f.Toplam), kesilenMatrah = vff.Sum(f => f.Matrah), kesilenAdet = vff.Count, acik = vffAcik, sonTarih = vff.Count == 0 ? "" : vff.Max(f => f.Tarih).ToString("yyyy-MM-dd"), refTarih = vffSon?.Tarih.ToString("yyyy-MM-dd") ?? "", refNo = vffSon?.No ?? "",
                sonrasiOdeme = Math.Round(sonrasiOdeme.Sum(h => h.Alacak * KurTl(h.ParaBirimi)), 2), sonrasiOdemeAdet = sonrasiOdeme.Count, sonrasiSatis = Math.Round(sonrasiSatis.Sum(h => h.Borc * KurTl(h.ParaBirimi)), 2), sonrasiSatisAdet = sonrasiSatis.Count,
                // vade farkı sonrası durum: Ödedi | Ödeme Yok | Ödeme Yok · Satış Sürüyor | "" (vade farkı faturası yok)
                sonrasiDurum = vffSon == null ? "" : sonrasiOdeme.Count > 0 ? "Ödeme Yaptı" : sonrasiSatis.Count > 0 ? "Ödeme Yok · Satış Sürüyor" : "Ödeme Yok",
                sonOdeme = sonOdeme?.Tarih.ToString("yyyy-MM-dd") ?? "", sonOdemeTl = sonOdeme == null ? 0 : Math.Round(sonOdeme.Alacak * KurTl(sonOdeme.ParaBirimi), 2), sonOdemeGun = sonOdeme == null ? (int?)null : (bugun - sonOdeme.Tarih.Date).Days,
                tahsilatVf = tahsVf.Sum(t => t.VadeFarki), tahsilatVfAdet = tahsVf.Count,
                oran = TahsilatOrnek.Oran(k.Kod), oranOzel = TahsilatTakipKayit.MusteriOran(k.Kod) != null,   /* Vade Farkı Takibi › VF Oranı sütunu (müşteriye özel / varsayılan) */
                limit = lim.Limit, limitTur = lim.Tur, limitRisk = lim.ToplamRisk, limitYuzde = lim.KullanimYuzde, limitDurum = lim.Durum, limitAsim = lim.Durum == "Limit Aşıldı" ? Math.Round(lim.ToplamRisk - lim.Limit, 2) : 0,
                satisDurdu = k.SatisDurdu, satisNeden = k.SatisDurdurNeden, satisKim = k.SatisDurdurKim, satisZaman = Zmn(k.SatisDurdurZaman),
                mutabakat = c == null ? "Gönderilmedi" : EMutabakatOrnek.Durum(c), mutabakatTarih = emSon == null ? "" : Trh(emSon.Tarih), mutabakatBelge = emSon?.BelgeNo ?? "", mutabakatKanal = emSon?.Kanal ?? "", sonMutabik = Trh(c?.SonMutabakat), mailUygun = c != null && EMutabakatOrnek.MailUygun(c), waUygun = c != null && EMutabakatOrnek.WaUygun(c)
            };
            return (dto, lim);
        }
        // ---------- AYLIK SÜREÇ (17 Kas 2026): görüşme notları + vade farkı robotu ----------
        // Kullanıcı: "Tahsilat her ayın sonunda müşteri diyaloglarını not girsin, kendi de okuyabilmeli. Süreç akışım: 2. – 3. hafta müşterileri arasın, ayın son haftası ödeme yapmayanlara
        // otomatik vade farkı robotu çalıştırılsın, seçilen 1 – 2 müşteri dışında." Kayıtlar: Data/TahsilatTakipKayit.cs (App_Data/tahsilat-takip.json); robot: TahsilatOrnek.RobotCalistir.
        public class NotIstek { public int Id { get; set; } public string Kod { get; set; } public string Kanal { get; set; } public string Gorusulen { get; set; } public string Sonuc { get; set; } public string SozTarihi { get; set; } public decimal SozTutar { get; set; } public string Not { get; set; } }
        public class HaricIstek { public string Kod { get; set; } public bool Haric { get; set; } public string Neden { get; set; } public bool Surekli { get; set; } }
        static object NotDto(TahsilatTakipKayit.Not n, string ben) => new { id = n.Id, cariKodu = n.CariKodu, cariAd = CariKartOrnek.KartBul(n.CariKodu)?.KisaAd ?? "", donem = n.Donem, zaman = Zmn(n.Zaman), tarih = n.Zaman.ToString("yyyy-MM-dd"), kullanici = n.Kullanici, kanal = n.Kanal, gorusulen = n.Gorusulen, sonuc = n.Sonuc, sozTarihi = n.SozTarihi ?? "", sozTutar = n.SozTutar, metin = n.Metin, ornek = n.Ornek, benim = !n.Ornek && string.Equals(n.Kullanici, ben, StringComparison.OrdinalIgnoreCase) };
        /// <summary>Müşterinin bu ayki süreç durumu: görüşme notları, bu ay yaptığı ödeme, robot dışı işareti, bu ay kesilen robot faturası.</summary>
        static object TakipOzet(CariKart k, MtCari c, List<TahsilatTakipKayit.Not> notlar, DateTime bugun, string ben)
        {
            string donem = bugun.ToString("yyyy-MM"); var ayBasi = new DateTime(bugun.Year, bugun.Month, 1); var kn = notlar.Where(n => n.CariKodu == k.Kod).ToList(); var buAy = kn.Where(n => n.Donem == donem).ToList();
            List<MtHareket> odemeler; if (c == null) odemeler = new List<MtHareket>(); else lock (MutabakatOrnek.Kilit) odemeler = c.Hareketler.Where(h => OdemeHareketi(h) && h.Tarih.Date >= ayBasi && h.Tarih.Date <= bugun).ToList();
            var haric = TahsilatTakipKayit.HaricMi(k.Kod, donem);
            return new { notAdet = kn.Count, buAyNot = buAy.Count, sonNot = kn.Count == 0 ? null : NotDto(kn[0], ben), buAySonNot = buAy.Count == 0 ? null : NotDto(buAy[0], ben), buAyOdeme = Math.Round(odemeler.Sum(h => h.Alacak * KurTl(h.ParaBirimi)), 2), buAyOdemeAdet = odemeler.Count,
                haric = haric != null, haricNeden = haric?.Neden ?? "", haricSurekli = haric != null && string.IsNullOrEmpty(haric.Donem), haricKim = haric == null ? "" : $"{haric.Kullanici} · {Zmn(haric.Zaman)}", robotFatura = TahsilatOrnek.RobotFaturasi(k.Kod, donem) };
        }
        /// <summary>Görüşme notu ekle (müşteri + bu ay).</summary>
        [HttpPost]
        public IActionResult NotKaydet([FromBody] NotIstek i)
        {
            var k = CariKartOrnek.KartBul(i?.Kod ?? ""); if (k == null || k.KartTipi != "Müşteri") return Json(new { success = false, message = "Müşteri bulunamadı." });
            var h = TahsilatTakipKayit.NotEkle(k.Kod, i.Kanal, i.Gorusulen, i.Sonuc, i.SozTarihi, i.SozTutar, i.Not, Ben().AdSoyad, out var n); if (h != null) return Json(new { success = false, message = h });
            return Json(new { success = true, message = $"{k.Kod} {k.KisaAd} için görüşme notu kaydedildi ({n.Sonuc}{(string.IsNullOrEmpty(n.SozTarihi) ? "" : $" · söz {DateTime.Parse(n.SozTarihi):dd.MM.yyyy}{(n.SozTutar > 0 ? " · " + Tl(n.SozTutar) : "")}")})." });
        }
        /// <summary>Görüşme notunu sil (yalnız notu giren).</summary>
        [HttpPost]
        public IActionResult NotSil([FromBody] NotIstek i) { var h = TahsilatTakipKayit.NotSil(i?.Id ?? 0, Ben().AdSoyad); return Json(new { success = h == null, message = h ?? "Not silindi." }); }
        /// <summary>Müşteriyi bu ay (ya da sürekli) vade farkı robotunun dışında bırak / yeniden dahil et.</summary>
        [HttpPost]
        public IActionResult RobotHaric([FromBody] HaricIstek i)
        {
            var k = CariKartOrnek.KartBul(i?.Kod ?? ""); if (k == null || k.KartTipi != "Müşteri") return Json(new { success = false, message = "Müşteri bulunamadı." });
            string donem = DateTime.Today.ToString("yyyy-MM"), kisi = Ben().AdSoyad; var h = TahsilatTakipKayit.HaricAyarla(k.Kod, donem, i.Haric, i.Neden, i.Surekli, kisi); if (h != null) return Json(new { success = false, message = h });
            CariKartOrnek.Gunluk(k, DateTime.Now, kisi, "Vade Farkı Robotu", i.Haric ? "Dahil" : "Robot Dışı", i.Haric ? (i.Surekli ? "Robot Dışı (Sürekli)" : $"Robot Dışı ({DateTime.Today.ToString("MMMM yyyy", Tr)})") : "Dahil", "Tahsilat", i.Neden);
            return Json(new { success = true, message = i.Haric ? $"{k.Kod} {k.KisaAd} {(i.Surekli ? "sürekli" : DateTime.Today.ToString("MMMM yyyy", Tr) + " için")} vade farkı robotunun dışında bırakıldı." : $"{k.Kod} {k.KisaAd} yeniden robota dahil edildi." });
        }
        /// <summary>Vade farkı robotunu çalıştır: ödeme yapmamış (vadesi geçmiş açık faturası olan) müşterilere bu ayın vade farkı faturası; robot dışı işaretliler atlanır.</summary>
        [HttpPost]
        public IActionResult RobotCalistir()
        {
            var r = TahsilatOrnek.RobotCalistir(Ben().AdSoyad);
            string mesaj = r.Kesilen.Count == 0 ? "Robot fatura kesmedi: " + (r.ZatenKesilmis > 0 ? $"{r.ZatenKesilmis} müşteriye bu ay zaten kesilmiş" : "vade farkı doğan gecikmiş fatura yok") + (r.Haric.Count > 0 ? $" · robot dışı: {string.Join(", ", r.Haric)}" : "") + "."
                : $"Robot {r.Kesilen.Count} müşteriye vade farkı faturası kesti ({r.Kesilen[0].No}{(r.Kesilen.Count > 1 ? " … " + r.Kesilen[r.Kesilen.Count - 1].No : "")}) · toplam {Tl(r.Kesilen.Sum(f => f.Toplam))} (KDV dahil)" + (r.Haric.Count > 0 ? $" · robot dışı bırakılan: {string.Join(", ", r.Haric)}" : "") + (r.ZatenKesilmis > 0 ? $" · {r.ZatenKesilmis} müşteriye bu ay daha önce kesilmişti" : "") + ".";
            return Json(new { success = r.Kesilen.Count > 0, message = mesaj });
        }

        // ---------- BAKİYE UYUMU (19 Kas 2026) ----------
        // Kullanıcı: "mizan bakiyesi yerine muhasebedeki cari ekstre raporu / hesap ekstresi raporu / mizan raporu ile bu tahsilat raporunun bakiye karşılaştırmasını yap; uyumlu / uyumsuz yaz;
        // tıklayınca tüm bakiyeleri kalem kalem getir, kolayca karşılaştırayım."
        // Kaynaklar: (1) BU RAPOR = cari hesap hareketleri (MutabakatOrnek; tüm borç − alacak, döviz × güncel kur = Cari Kartı / Cari Bakiyeler (Mizan) ile aynı);
        //   (2) MUHASEBE HESAP EKSTRESİ 120.01.xxx = Cari Ekstre / Hesap Ekstresi Mutabakatı'nın (/Rapor141, EkstreKarsilastirmaOrnek) hesap tarafı — ön yüz örneğinde cari satırlarından deterministik farklarla
        //   (aktarılmamış fatura, kur / kuruş farkı, mükerrer fiş, cariye işlenmemiş kur farkı / mahsup fişi) türetilir; düzeltme fişi atılan farklar kapanır;
        //   (3) CARİ EKSTRE RAPORU /Rapor20 (OrnekVeri.CariEkstre: sentetik, TRY, sabit devir — ÖRNEK VERİ); (4) HESAP EKSTRESİ /Rapor39 (taslak, müşteri bakiyesi üretmiyor); (5) MİZAN /Rapor28 (OrnekVeri.Mizan:
        //   yalnız 120.01.001 toplamı, müşteri alt hesabı yok — ÖRNEK VERİ). Uyum durumu yalnız gerçek kaynakla (2) hesaplanır (± 0,50 ₺); örnek kaynaklar pencerede "Örnek Veri" rozetiyle listelenir,
        //   SAP bağlanınca aynı pencerede gerçek değerleriyle karşılaştırılır.
        const decimal UyumTolerans = 0.5m;
        /// <summary>Muhasebe hesap tarafı: bütün hareketler (ilk hareketten bugüne) için cari satırları, hesap satırları ve eşleşme.</summary>
        static (List<EkSatir> cari, List<EkSatir> hesap, List<EkEslesme> es, bool tanimsiz) HesapTarafi(MtCari c)
        {
            if (c == null) return (new List<EkSatir>(), new List<EkSatir>(), new List<EkEslesme>(), true);
            lock (MutabakatOrnek.Kilit)
            {
                var bas = c.Hareketler.Count == 0 ? DateTime.Today : c.Hareketler.Min(h => h.Tarih); var cari = EkstreKarsilastirmaOrnek.CariSatirlari(c, bas, DateTime.Today.AddYears(5));
                bool tanimsiz = EkstreKarsilastirmaOrnek.HesapTanimsiz(c.CariKodu); var hesap = tanimsiz ? new List<EkSatir>() : EkstreKarsilastirmaOrnek.HesapSatirlari(c, cari);
                return (cari, hesap, tanimsiz ? new List<EkEslesme>() : EkstreKarsilastirmaOrnek.Eslestir(cari, hesap), tanimsiz);
            }
        }
        static string UyumDurumu(bool tanimsiz, decimal fark) => tanimsiz ? "Hesap Tanımsız" : Math.Abs(fark) < UyumTolerans ? "Uyumlu" : "Uyumsuz";
        /// <summary>Bir kaynağın karşılaştırma satırı (Bakiye Karşılaştırması penceresi ve rapor sütunu).</summary>
        class UyumKaynak { public string Ad, Rapor, Link, Durum, Not; public decimal? Bakiye, Borc, Alacak, Fark; public bool Esas; }
        class UyumSonuc { public string Durum, HesapKodu; public decimal Fark, Cari, Hesap; public int Sorun; public bool Tanimsiz; public List<UyumKaynak> Kaynaklar = new List<UyumKaynak>(); public List<EkSatir> CariL, HesapL; public List<EkEslesme> Es; }
        /// <summary>
        /// BEŞ KAYNAK karşılaştırması (kullanıcı 19 Kas 2026: "Uyumlu yazabilmen için Tahsilat Raporu · Cari Hesap, Muhasebe Hesap Ekstresi, Cari Ekstre Raporu, Hesap Ekstresi Raporu, Mizan — hepsinin uyumlu olması lazım"):
        ///   (1) bu rapor = cari hesap (esas); (2) muhasebe hesap ekstresi 120.01.xxx (Rapor141 hesap tarafı); (3) Cari Ekstre Raporu /Rapor20 (artık aynı cari hareketlerinden üretilir);
        ///   (4) Hesap Ekstresi Raporu /Rapor39 (muhasebe hesabının fiş satırları); (5) Mizan /Rapor28 (120.01.xxx alt hesabı = fiş satırları toplamı). Uyumlu = beşi de ± 0,50 ₺ içinde; biri bile sapıyorsa Uyumsuz.
        /// </summary>
        UyumSonuc Uyum(CariKart k, MtCari c, List<HesapOzeti> mizan = null)
        {
            var u = new UyumSonuc(); if (c == null) { u.Durum = "Kayıt Yok"; return u; } var bugun = DateTime.Today;
            var (cari, hesap, es, tanimsiz) = HesapTarafi(c); u.CariL = cari; u.HesapL = hesap; u.Es = es; u.Tanimsiz = tanimsiz; u.Sorun = es.Count(e => e.Durum != "Eşleşti");
            decimal cb = c.BakiyeTl, cbB = cari.Sum(s => s.Borc), cbA = cari.Sum(s => s.Alacak), hbB = hesap.Sum(s => s.Borc), hbA = hesap.Sum(s => s.Alacak), hb = Math.Round(hbB - hbA, 2); u.Cari = cb; u.Hesap = hb; u.HesapKodu = tanimsiz ? "" : EkstreKarsilastirmaOrnek.HesapKodu(k);
            string kk = Uri.EscapeDataString(k.Kod); decimal? F(decimal? b) => b == null ? (decimal?)null : Math.Round(cb - b.Value, 2); string D(decimal? b) => b == null ? "Hesap Tanımsız" : UyumDurumu(false, cb - b.Value);
            u.Kaynaklar.Add(new UyumKaynak { Ad = "Tahsilat Raporu · Cari Hesap", Rapor = "Cari Mutabakat hareketleri (Cari Kartı ve Cari Bakiyeler (Mizan) ile aynı kaynak)", Link = "/CariKart/Musteri?kod=" + kk, Bakiye = cb, Borc = cbB, Alacak = cbA, Fark = 0, Durum = "Esas", Esas = true, Not = $"{cari.Count} hareket · tüm borç − alacak · döviz × güncel kur ({Trh(MutabakatOrnek.KurTarihi)})" });
            u.Kaynaklar.Add(new UyumKaynak { Ad = $"Muhasebe Hesap Ekstresi · {(tanimsiz ? "hesap tanımsız" : u.HesapKodu)}", Rapor = "Cari Ekstre / Hesap Ekstresi Mutabakatı (/Rapor141) · hesap tarafı", Link = "/Rapor141?kod=" + kk, Bakiye = tanimsiz ? (decimal?)null : hb, Borc = tanimsiz ? (decimal?)null : hbB, Alacak = tanimsiz ? (decimal?)null : hbA, Fark = tanimsiz ? null : F(hb), Durum = D(tanimsiz ? null : hb), Not = tanimsiz ? "Bu cari için muhasebe hesabı tanımlı değil; hesap açılmalı." : $"{hesap.Count} fiş satırı · {u.Sorun} satır farkı · {es.Count(e => e.Durum == "Eşleşti")} eşleşen" });
            var ekstre = OrnekVeri.CariEkstre(k.Kod, new DateTime(bugun.Year, 1, 1), bugun); decimal r20 = ekstre.LastOrDefault()?.TRY_KmlBky ?? 0;
            u.Kaynaklar.Add(new UyumKaynak { Ad = "Cari Ekstre Raporu", Rapor = "/Rapor20 · cari hareketlerinden ekstre (devir + dönem)", Link = "/Rapor20?bpCode=" + kk, Bakiye = r20, Borc = ekstre.Sum(x => x.TRYB ?? 0), Alacak = ekstre.Sum(x => x.TRYA ?? 0), Fark = F(r20), Durum = D(r20), Not = $"{ekstre.Count - 1} satır + devir · {bugun.Year} yılı · dönem sonu kümülatif bakiye" });
            u.Kaynaklar.Add(new UyumKaynak { Ad = $"Hesap Ekstresi Raporu · {(tanimsiz ? "hesap tanımsız" : u.HesapKodu)}", Rapor = "/Rapor39 · muhasebe hesabının fiş satırları", Link = "/Rapor39?bpCode=" + kk, Bakiye = tanimsiz ? (decimal?)null : hb, Borc = tanimsiz ? (decimal?)null : hbB, Alacak = tanimsiz ? (decimal?)null : hbA, Fark = tanimsiz ? null : F(hb), Durum = D(tanimsiz ? null : hb), Not = tanimsiz ? "Muhasebe hesabı yok." : $"{hesap.Count} fiş satırı · kümülatif bakiye" });
            var mz = tanimsiz ? null : (mizan ?? OrnekVeri.MusteriMizani()).FirstOrDefault(x => x.HesapKodu == u.HesapKodu); bool paylasimli = mz != null && (mz.HesapAdi ?? "").Contains(" · ");
            u.Kaynaklar.Add(new UyumKaynak { Ad = $"Mizan · {(tanimsiz ? "120.01 alt hesabı yok" : u.HesapKodu)}", Rapor = "/Rapor28 · 120 ALICILAR alt hesabı", Link = "/Rapor28?calistir=1&anaHesap=120", Bakiye = mz?.ToplamBakiye, Borc = mz?.ToplamBorc, Alacak = mz?.ToplamAlacak, Fark = F(mz?.ToplamBakiye), Durum = D(mz?.ToplamBakiye), Not = mz == null ? "Mizanda bu carinin alt hesabı yok." : paylasimli ? $"Aynı hesapta birden çok cari: {mz.HesapAdi}" : "Hesap bakiyesi = fiş satırları toplamı" });
            var olcul = u.Kaynaklar.Where(x => !x.Esas).ToList(); var sapan = olcul.Where(x => x.Durum == "Uyumsuz").ToList();
            u.Durum = tanimsiz ? "Hesap Tanımsız" : olcul.Any(x => x.Bakiye == null) ? "Eksik Kaynak" : sapan.Count == 0 ? "Uyumlu" : "Uyumsuz";
            u.Fark = sapan.Count > 0 ? sapan.OrderByDescending(x => Math.Abs(x.Fark ?? 0)).First().Fark ?? 0 : 0;
            return u;
        }
        /// <summary>Rapor satırı için uyum özeti (beş kaynak).</summary>
        object UyumOzet(CariKart k, MtCari c, List<HesapOzeti> mizan = null)
        {
            var u = Uyum(k, c, mizan);
            return new { durum = u.Durum, fark = u.Fark, hesapKodu = u.HesapKodu ?? "", hesapBakiye = u.Hesap, sorun = u.Sorun, uyumlu = u.Kaynaklar.Count(x => x.Durum == "Uyumlu"), toplam = Math.Max(0, u.Kaynaklar.Count - 1), kaynaklar = u.Kaynaklar.Where(x => !x.Esas).Select(x => new { x.Ad, x.Durum, x.Bakiye, x.Fark }).ToList() };
        }
        /// <summary>Bakiye karşılaştırma penceresi: kaynak bazında bakiyeler (bu rapor, muhasebe hesap ekstresi, Cari Ekstre Raporu, Hesap Ekstresi, Mizan), para birimi kırılımı, bu raporun kalemleri, cari ↔ muhasebe satır farkları.</summary>
        [HttpGet]
        static object KontrolDto(IbkbBakiyeKontrol x) => x == null ? null : new { x.Id, tarih = Zmn(x.Tarih), x.Kullanici, x.Durum, x.Not, x.BakiyeOzet, x.FarkOzet };
        static object GorevKisa(IbkbGorev g) { var e = IbkbOrnek.GorevEposta(g); return new { g.Id, g.Atanan, g.AtananEposta, g.Durum, konu = e.Konu, govde = e.Govde, g.Bildirim }; }
        /// <summary>Bakiye Karşılaştırması › Kontrol Sonucu + Düzeltme Görevi (19 Kas 2026): İhracat Finansal Takibi'ndeki akışın aynısı, Kaynak = Tahsilat.</summary>
        [HttpPost]
        public IActionResult UyumKontrolKaydet([FromBody] IbkbBakiyeKontrolIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." }); d.Kaynak = "Tahsilat"; if (d.Gorev != null) d.Gorev.Kaynak = "Tahsilat";
            var h = IbkbOrnek.BakiyeKontrolKaydet(d, User?.Identity?.Name, out var k, out var g); var kart = CariKartOrnek.KartBul(d.CariKodu ?? "");
            if (h == null && kart != null) CariKartOrnek.Gunluk(kart, DateTime.Now, Ben().AdSoyad, "Bakiye Kontrolü", "", $"{k.Durum}{(string.IsNullOrEmpty(k.Not) ? "" : " · " + k.Not)}{(g == null ? "" : $" · düzeltme görevi #{g.Id} → {g.Atanan}")}", "Tahsilat › Bakiye Karşılaştırması");
            return Json(new { success = h == null, message = h ?? $"Bakiye kontrolü kaydedildi: {k.CariKodu} · {k.Durum}." + (g == null ? "" : $" Düzeltme görevi {g.Atanan} kişisine atandı (#{g.Id}){(string.IsNullOrEmpty(g.Bildirim) ? "" : " · " + g.Bildirim)}."), gorev = g == null ? null : GorevKisa(g) });
        }
        [HttpPost]
        public IActionResult UyumGorevKaydet([FromBody] IbkbGorevIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." }); d.Kaynak = "Tahsilat";
            var h = IbkbOrnek.GorevKaydet(d, User?.Identity?.Name, out var g); string islem = (d.Islem ?? "").ToLowerInvariant();
            if (h == null && g != null) { var kart = CariKartOrnek.KartBul(g.CariKodu); if (kart != null) CariKartOrnek.Gunluk(kart, DateTime.Now, Ben().AdSoyad, "Düzeltme Görevi", "", $"#{g.Id} · {g.Durum} · {g.Atanan}{(string.IsNullOrEmpty(d.Not) ? "" : " · " + d.Not)}", "Tahsilat › Bakiye Karşılaştırması"); }
            return Json(new { success = h == null, message = h ?? $"Görev #{g.Id}: {g.Durum}." + (g.Bildirim != "" && (islem == "ata" || islem == "" || islem == "guncelle" || islem == "iade") ? " · " + g.Bildirim : ""), gorev = g == null ? null : GorevKisa(g) });
        }
        /// <summary>E-posta / portal mesajındaki bağlantı (?gorev=ID): görevin carisi için Bakiye Karşılaştırması açılır.</summary>
        [HttpGet] public IActionResult UyumGorev(int id) { var g = IbkbOrnek.GorevBul(id); return g == null ? Json(new { success = false, message = "Görev bulunamadı." }) : Json(new { success = true, gorev = IbkbOrnek.GorevDtoVer(g) }); }
        public IActionResult BakiyeKarsilastir(string kod)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); if (k == null || k.KartTipi != "Müşteri") return Json(new { success = false, message = "Müşteri bulunamadı." });
            var bugun = DateTime.Today; var c = MutabakatOrnek.Bul(k.Kod); if (c == null) return Json(new { success = false, message = "Cari hesap kaydı yok." }); var r = CariKartOrnek.RiskHesapla(k); var acik = TahsilatOrnek.AcikFaturalar(k.Kod); decimal acikTl = Math.Round(acik.Sum(f => f.Kalan * KurTl(f.ParaBirimi)), 2);
            var u = Uyum(k, c); var cari = u.CariL; var hesap = u.HesapL; var es = u.Es; decimal cb = u.Cari, cbB = cari.Sum(s => s.Borc), cbA = cari.Sum(s => s.Alacak), hbB = hesap.Sum(s => s.Borc), hbA = hesap.Sum(s => s.Alacak), hb = u.Hesap;
            string hesapKodu = u.HesapKodu; var sorunlar = es.Where(e => e.Durum != "Eşleşti").ToList();
            var kaynaklar = u.Kaynaklar.Select(x => new { ad = x.Ad, rapor = x.Rapor, link = x.Link, bakiye = x.Bakiye, borc = x.Borc, alacak = x.Alacak, fark = x.Fark, durum = x.Durum, ornek = false, not = x.Not }).ToList();
            List<MtDoviz> dv; lock (MutabakatOrnek.Kilit) dv = c.Dovizler.ToList();
            // 19 Kas 2026 (kullanıcı: "İhracat'taki gibi ilgili kişilere görev atayabilelim"): kontrol sonucu + düzeltme görevi (IbkbOrnek görev motoru, Kaynak = Tahsilat)
            var sonKontrol = IbkbOrnek.SonKontrol(k.Kod, "Tahsilat"); var acikGorev = IbkbOrnek.AcikGorev(k.Kod, "Tahsilat");
            string bakiyeOzet = Tl(cb) + (dv.Count(d => d.Net != 0) > 1 || dv.Any(d => d.Net != 0 && !TahsilatOrnek.TlMi(d.ParaBirimi)) ? " (" + string.Join(" · ", dv.Where(d => d.Net != 0).Select(d => $"{d.Net.ToString("N2", Tr)} {d.ParaBirimi}")) + ")" : "");
            string farkOzet = u.Durum == "Uyumlu" ? "fark yok" : string.Join(" · ", u.Kaynaklar.Where(x => x.Durum != "Uyumlu" && x.Durum != "Esas").Select(x => $"{x.Ad}: {(x.Fark.HasValue ? (x.Fark.Value >= 0 ? "+" : "") + Tl(x.Fark.Value) : x.Durum)}"));
            return Json(new { success = true, kod = k.Kod, ad = k.KisaAd, unvan = k.ResmiUnvan, sirket = k.Sirket, hesapKodu, bugun = bugun.ToString("yyyy-MM-dd"), durum = u.Durum, fark = u.Fark, bakiye = cb, hesapBakiye = hb, uyumlu = u.Kaynaklar.Count(x => x.Durum == "Uyumlu"), toplam = u.Kaynaklar.Count - 1, kaynaklar,
                kontrol = KontrolDto(sonKontrol), kontrolSayisi = IbkbOrnek.KontrolSayisi(k.Kod, "Tahsilat"), gorev = acikGorev == null ? null : IbkbOrnek.GorevDtoVer(acikGorev), gorevler = IbkbOrnek.GorevlerListe("Tahsilat", k.Kod), gorevKisiler = IbkbOrnek.GorevKisilerListe(), kontrolDurumlari = IbkbOrnek.BakiyeKontrolDurumlari, oncelikler = IbkbOrnek.GorevOncelikleri, benKod = User?.Identity?.Name ?? "", bakiyeOzet, farkOzet,
                dovizler = dv.Select(d => new { pb = d.ParaBirimi, d.Borc, d.Alacak, net = d.Net, d.Kur, tl = d.TlKarsiligi, acikAdet = d.AcikFaturaAdet, acikTutar = d.AcikFaturaTutar }),
                tahsilat = new { acikToplam = acikTl, acikAdet = acik.Count, kapanmamis = Math.Max(0, Math.Round(acikTl - cb, 2)), bakiyeX = r.BakiyeX, bakiyeR = r.BakiyeR, cek = r.Cek, cekAdet = r.CekAdet, vadesiGecmis = r.VadesiGecmis },
                hesapOzet = new { cariBorc = cbB, cariAlacak = cbA, hesapBorc = hbB, hesapAlacak = hbA, satir = cari.Count, hesapSatir = hesap.Count, eslesen = es.Count(e => e.Durum == "Eşleşti"), sorun = sorunlar.Count },
                farklar = sorunlar.Take(200).Select(e => { var s = e.Cari ?? e.Hesap; return new { tarih = Trh(s.Tarih), s.Belge, s.Tip, s.Pb, cari = e.Cari == null ? (decimal?)null : e.Cari.Borc - e.Cari.Alacak, hesap = e.Hesap == null ? (decimal?)null : e.Hesap.Borc - e.Hesap.Alacak, e.Fark, e.Durum, e.Neden, e.Oneri, fis = (e.Hesap ?? e.Cari).Fis }; }) });
        }

        // ---------- MÜŞTERİ İLETİŞİMİ (19 Kas 2026; Aylık Süreç) ----------
        // Kullanıcı: "WhatsApp mı, mail mi, ikisi mi seçeyim; mutabakat yapsın, müşteri ödeyeceği tutarı yazsın buraya otomatik gelsin ya da itiraz etsin; cari ekstreyi, ödeme planı tahsilat simülasyonunu,
        //   ödeme planı faturalarını buradan göndereyim; müşteri WhatsApp'tan talep edince bana uyarı düşsün, otomatik göndersin." Kayıtlar Data/TahsilatTakipKayit.cs (Iletisim / Talep / OtoYanit).
        //   WhatsApp gönderimi: wa.me bağlantısı (metin hazır) + kayıt; e-posta: mailto bağlantısı + kayıt. Gelen WhatsApp mesajı /Tahsilat/WhatsAppGelen ucuna düşer (gerçek webhook aynı uca bağlanır;
        //   ekranda "müşteri yanıtını simüle et" ile denenir): ödeme sözü → görüşme notu (Ödeme Sözü Alındı, tutar / tarih), itiraz → not + e-mutabakat İtirazlı, belge isteği → Talep (+ otomatik gönderim).
        public class IletisimIstek { public string Kod { get; set; } public List<string> Kanallar { get; set; } public List<string> Icerik { get; set; } public string Mesaj { get; set; } public int TalepId { get; set; } public int KisiId { get; set; } }
        public class GelenIstek { public string Kod { get; set; } public string Telefon { get; set; } public string Metin { get; set; } }
        public class OtoIstek { public bool Acik { get; set; } }
        static object IletisimDto(TahsilatTakipKayit.Iletisim i) => new { id = i.Id, cariKodu = i.CariKodu, cariAd = CariKartOrnek.KartBul(i.CariKodu)?.KisaAd ?? "", zaman = Zmn(i.Zaman), tarih = i.Zaman.ToString("yyyy-MM-dd"), zamanIso = i.Zaman.ToString("yyyy-MM-ddTHH:mm:ss"), yon = i.Yon, kanal = i.Kanal, kullanici = i.Kullanici, icerik = i.Icerik, metin = i.Metin, baglantilar = i.Baglantilar.Select(b => { var p = b.Split('|'); return new { ad = p[0], url = p.Length > 1 ? p[1] : "" }; }).ToList(), sozTarihi = i.SozTarihi, sozTutar = i.SozTutar, otomatik = i.Otomatik, talepId = i.TalepId, kisiId = i.KisiId, kime = i.Kime, ornek = i.Ornek };
        static object TalepDto(TahsilatTakipKayit.Talep t) => new { id = t.Id, cariKodu = t.CariKodu, cariAd = CariKartOrnek.KartBul(t.CariKodu)?.KisaAd ?? "", zaman = Zmn(t.Zaman), tarih = t.Zaman.ToString("yyyy-MM-dd"), kanal = t.Kanal, tur = t.Tur, metin = t.Metin, durum = t.Durum, iletisimId = t.IletisimId, gonderim = Zmn(t.GonderimZamani) };
        string Kok() => $"{Request.Scheme}://{Request.Host}";
        /// <summary>Seçilen içeriklerin belge bağlantıları ("ad|url").</summary>
        List<string> BelgeBaglantilari(string kod, IEnumerable<string> icerik)
        {
            var l = new List<string>(); string k = Uri.EscapeDataString(kod), kok = Kok();
            foreach (var i in icerik ?? new List<string>())
            {
                if (i == "Cari Ekstre") l.Add($"Cari Ekstre (PDF)|{kok}/Tahsilat/EkstrePdf?kod={k}");
                else if (i == "Ödeme Planı · Tahsilat Simülasyonu") l.Add($"Ödeme Planı · Tahsilat Simülasyonu (PDF)|{kok}/Tahsilat/OdemePlaniPdf?kod={k}&tur=simulasyon");
                else if (i == "Ödeme Planı · Faturalar") l.Add($"Ödeme Planı · Faturalar (PDF)|{kok}/Tahsilat/OdemePlaniPdf?kod={k}&tur=faturalar");
            }
            return l;
        }
        /// <summary>Müşteriye giden metin: hitap, bakiye, mutabakat bağlantısı, belge bağlantıları, serbest mesaj, yanıt yönergesi.</summary>
        static string IletisimMetni(MtCari c, CariKart k, MtKisi kisi, List<string> icerik, List<string> baglantilar, string mesaj, string gonderen, string mutabakatLink)
        {
            var s = new System.Text.StringBuilder();
            s.AppendLine($"Sayın {(string.IsNullOrWhiteSpace(kisi?.AdSoyad) ? "Yetkili" : kisi.AdSoyad)},");
            s.AppendLine($"{k.ResmiUnvan ?? k.KisaAd} ({k.Kod}) cari hesabınızın {DateTime.Today:dd.MM.yyyy} itibarıyla bakiyesi: {EMutabakatOrnek.BakiyeMetni(c)}.");
            if (icerik.Contains("Mutabakat")) s.AppendLine($"Mutabakat yanıtınız (Mutabıkız / İtiraz) için: {mutabakatLink}");
            if (baglantilar.Count > 0) { s.AppendLine("Belgeler:"); foreach (var b in baglantilar) { var p = b.Split('|'); s.AppendLine($"• {p[0]}: {(p.Length > 1 ? p[1] : "")}"); } }
            if (!string.IsNullOrWhiteSpace(mesaj)) { s.AppendLine(); s.AppendLine(mesaj.Trim()); }
            s.AppendLine(); s.AppendLine("Ödeme yapacağınız tutarı ve tarihi bu kanaldan yazabilirsiniz; itirazınız varsa \"itiraz\" ve nedenini, cari ekstre ya da ödeme planı isterseniz \"ekstre\" / \"ödeme planı\" yazmanız yeterlidir.");
            s.AppendLine($"{gonderen} · {k.Sirket} Tahsilat");
            return s.ToString();
        }
        /// <summary>Gönderim: kanal(lar) × içerik(ler) → her kanal için iletişim kaydı ve açılacak bağlantı (wa.me / mailto); Mutabakat seçiliyse e-mutabakat gönderimi (yanıt bağlantısı) de oluşur.</summary>
        [HttpPost]
        public IActionResult IletisimGonder([FromBody] IletisimIstek i)
        {
            var k = CariKartOrnek.KartBul(i?.Kod ?? ""); if (k == null || k.KartTipi != "Müşteri") return Json(new { success = false, message = "Müşteri bulunamadı." });
            var c = MutabakatOrnek.Bul(k.Kod); if (c == null) return Json(new { success = false, message = "Cari hesap kaydı yok." }); EMutabakatOrnek.Hazirla();
            var kanallar = (i.Kanallar ?? new List<string>()).Where(x => x == "WhatsApp" || x == "E-posta").Distinct().ToList(); if (kanallar.Count == 0) return Json(new { success = false, message = "Kanal seçin (WhatsApp / E-posta)." });
            var icerik = (i.Icerik ?? new List<string>()).Where(x => TahsilatTakipKayit.IcerikTurleri.Contains(x)).Distinct().ToList(); if (icerik.Count == 0 && string.IsNullOrWhiteSpace(i.Mesaj)) return Json(new { success = false, message = "İçerik seçin ya da mesaj yazın." });
            string ben = Ben().AdSoyad; MtKisi kisi; lock (MutabakatOrnek.Kilit) kisi = c.Kisiler.FirstOrDefault(x => x.Id == i.KisiId) ?? EMutabakatOrnek.AnaYetkili(c);   // 19 Kas 2026: alıcı pencereden seçilir (son / sık kullanılan), yoksa ana yetkili
            if (kisi == null) return Json(new { success = false, message = "Bu caride kayıtlı yetkili kişi yok; önce kişi ekleyin." });
            var baglantilar = BelgeBaglantilari(k.Kod, icerik); var notlar = new List<string>();
            string mutLink = ""; if (icerik.Contains("Mutabakat")) { var m = EMutabakatOrnek.Metin(c, "WhatsApp", ben); var r = EMutabakatOrnek.Gonder(new[] { k.Kod }, kanallar.Count == 2 ? "İkisi" : kanallar[0], ben); notlar.AddRange(r.Hatalar); var ix = m.Govde.IndexOf("http", StringComparison.OrdinalIgnoreCase); mutLink = ix >= 0 ? m.Govde.Substring(ix).Split(' ', '\n', '\r')[0] : Kok() + "/EMutabakat/Yanit/" + k.Kod; }
            string metin = IletisimMetni(c, k, kisi, icerik, baglantilar, i.Mesaj, ben, mutLink); var giden = new List<object>(); string waLink = "", mailLink = "";
            foreach (var kn in kanallar)
            {
                string kime;
                if (kn == "WhatsApp") { var cep = EMutabakatOrnek.Cep(kisi); if (cep == "") { notlar.Add($"WhatsApp: {kisi.AdSoyad} için cep telefonu yok; Kayıtlı Kişiler'den ekleyin."); continue; } waLink = "https://wa.me/" + EMutabakatOrnek.WaNumara(cep) + "?text=" + Uri.EscapeDataString(metin); kime = $"{kisi.AdSoyad} · {cep}"; }
                else { if (string.IsNullOrWhiteSpace(kisi.Eposta) || !kisi.Eposta.Contains("@")) { notlar.Add($"E-posta: {kisi.AdSoyad} için e-posta adresi yok; Kayıtlı Kişiler'den ekleyin."); continue; } mailLink = "mailto:" + kisi.Eposta + "?subject=" + Uri.EscapeDataString($"{k.Sirket} · {k.KisaAd} cari hesap bilgilendirmesi {DateTime.Today:dd.MM.yyyy}") + "&body=" + Uri.EscapeDataString(metin); kime = $"{kisi.AdSoyad} · {kisi.Eposta}"; }
                var ic = icerik.ToList(); if (!string.IsNullOrWhiteSpace(i.Mesaj)) ic.Add("Mesaj");
                var kayit = TahsilatTakipKayit.IletisimEkle(k.Kod, "Giden", kn, ben, ic, metin, baglantilar, "", 0, false, i.TalepId, kisi.Id, kime); giden.Add(IletisimDto(kayit));
                if (i.TalepId > 0) TahsilatTakipKayit.TalepGonderildi(i.TalepId, kayit.Id, false);
            }
            if (giden.Count == 0) return Json(new { success = false, message = string.Join(" ", notlar) });
            CariKartOrnek.Gunluk(k, DateTime.Now, ben, "Müşteri İletişimi", "", string.Join(" + ", kanallar) + " · " + string.Join(", ", icerik.Count > 0 ? icerik : new List<string> { "Mesaj" }), "Tahsilat");
            return Json(new { success = true, message = $"{k.KisaAd}: {string.Join(" + ", kanallar)} ile {(icerik.Count > 0 ? string.Join(", ", icerik) : "mesaj")} hazırlandı; açılan bağlantıdan gönderin.{(notlar.Count > 0 ? " " + string.Join(" ", notlar) : "")}", waLink, mailLink, metin, iletisimler = giden });
        }
        /// <summary>Gönderilecek metnin önizlemesi (kayıt oluşturmaz): seçilen kişi, içerik ve mesajla; Mutabakat için yanıt bağlantısı dahil. İçerik kartına tıklanınca ne gideceği buradan gösterilir.</summary>
        [HttpGet]
        public IActionResult IletisimOnizle(string kod, int kisiId, string icerik, string mesaj)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); var c = k == null ? null : MutabakatOrnek.Bul(k.Kod); if (k == null || c == null) return Json(new { success = false, message = "Müşteri bulunamadı." }); EMutabakatOrnek.Hazirla();
            MtKisi kisi; lock (MutabakatOrnek.Kilit) kisi = c.Kisiler.FirstOrDefault(x => x.Id == kisiId) ?? EMutabakatOrnek.AnaYetkili(c);
            var ic = (icerik ?? "").Split('|', ',').Select(x => x.Trim()).Where(x => TahsilatTakipKayit.IcerikTurleri.Contains(x)).Distinct().ToList(); var bag = BelgeBaglantilari(k.Kod, ic);
            string mutLink = ""; if (ic.Contains("Mutabakat")) { var m = EMutabakatOrnek.Metin(c, "WhatsApp", Ben().AdSoyad); var ix = m.Govde.IndexOf("http", StringComparison.OrdinalIgnoreCase); mutLink = ix >= 0 ? m.Govde.Substring(ix).Split(' ', '\n', '\r')[0] : ""; }
            var acik = TahsilatOrnek.AcikFaturalar(k.Kod);
            return Json(new { success = true, metin = IletisimMetni(c, k, kisi, ic, bag, mesaj, Ben().AdSoyad, mutLink), kime = kisi == null ? "" : kisi.AdSoyad, mutabakat = new { bakiye = EMutabakatOrnek.BakiyeMetni(c), link = mutLink, acikAdet = acik.Count, acikTl = Tl(Math.Round(acik.Sum(f => f.Kalan * KurTl(f.ParaBirimi)), 2)), sablon = EMutabakatOrnek.Metin(c, "WhatsApp", Ben().AdSoyad).Govde }, baglantilar = bag.Select(b => { var p = b.Split('|'); return new { ad = p[0], url = p[1] }; }) });
        }
        /// <summary>Gelen WhatsApp mesajı (webhook / simülasyon): ödeme sözü → görüşme notu; itiraz / mutabık → e-mutabakat yanıtı + not; belge isteği → talep (otomatik yanıt açıksa hemen gönderilir); diğer → gelen mesaj.</summary>
        [HttpPost]
        public IActionResult WhatsAppGelen([FromBody] GelenIstek g)
        {
            string metin = (g?.Metin ?? "").Trim(); if (metin == "") return Json(new { success = false, message = "Mesaj boş." });
            var k = CariKartOrnek.KartBul(g.Kod ?? "");
            if (k == null && !string.IsNullOrWhiteSpace(g.Telefon)) { string tel = EMutabakatOrnek.WaNumara(g.Telefon); lock (MutabakatOrnek.Kilit) { var c0 = MutabakatOrnek.Cariler.FirstOrDefault(c => c.Kisiler.Any(p => EMutabakatOrnek.WaNumara(p.CepTelefonu ?? "") == tel && tel != "")); if (c0 != null) k = c0.Kart; } }
            if (k == null || k.KartTipi != "Müşteri") return Json(new { success = false, message = "Mesajın geldiği numara bir müşteri yetkilisiyle eşleşmedi." });
            EMutabakatOrnek.Hazirla(); var cari = MutabakatOrnek.Bul(k.Kod); string kul = "Müşteri (WhatsApp)", yet = cari == null ? "" : (EMutabakatOrnek.AnaYetkili(cari)?.AdSoyad ?? ""); string m = metin.ToLowerInvariant().Replace('ı', 'i').Replace('ş', 's').Replace('ğ', 'g').Replace('ü', 'u').Replace('ö', 'o').Replace('ç', 'c');
            var sonuc = new List<string>(); var uyari = new List<string>(); var icerik = new List<string>(); bool oto = TahsilatTakipKayit.OtoYanit; var talepler = new List<string>();
            if (m.Contains("ekstre")) talepler.Add("Cari Ekstre");
            if (m.Contains("simulasyon") || m.Contains("simülasyon") || (m.Contains("odeme plan") && !m.Contains("fatura"))) talepler.Add("Ödeme Planı · Tahsilat Simülasyonu");
            if (m.Contains("fatura") && !m.Contains("itiraz")) talepler.Add("Ödeme Planı · Faturalar");
            if (m.Contains("mutabakat") && !m.Contains("mutabik") && !m.Contains("itiraz")) talepler.Add("Mutabakat");
            // ödeme sözü: ilk tutar (≥ 1.000) + tarih (gg.aa.yyyy / gg.aa) ya da "yarin" / "haftaya" / "ay sonu"
            var tutarE = System.Text.RegularExpressions.Regex.Matches(metin, @"\d{1,3}(?:\.\d{3})+(?:,\d+)?|\d{4,}(?:,\d+)?").Cast<System.Text.RegularExpressions.Match>().Select(x => decimal.TryParse(x.Value.Replace(".", "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var v) ? v : 0).FirstOrDefault(v => v >= 1000);
            if (m.Contains("milyon")) { var mm = System.Text.RegularExpressions.Regex.Match(metin, @"(\d+(?:[,.]\d+)?)\s*milyon"); if (mm.Success && decimal.TryParse(mm.Groups[1].Value.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var mv)) tutarE = mv * 1_000_000m; }
            DateTime? soz = null; var tm = System.Text.RegularExpressions.Regex.Match(metin, @"\b(\d{1,2})[./](\d{1,2})(?:[./](\d{4}))?\b");
            if (tm.Success && int.TryParse(tm.Groups[1].Value, out var gg) && int.TryParse(tm.Groups[2].Value, out var aa) && gg >= 1 && gg <= 31 && aa >= 1 && aa <= 12) { int yy = tm.Groups[3].Success ? int.Parse(tm.Groups[3].Value) : DateTime.Today.Year; try { soz = new DateTime(yy, aa, Math.Min(gg, DateTime.DaysInMonth(yy, aa))); if (soz < DateTime.Today && !tm.Groups[3].Success) soz = soz.Value.AddYears(1); } catch { } }
            else if (m.Contains("yarin")) soz = DateTime.Today.AddDays(1); else if (m.Contains("haftaya") || m.Contains("gelecek hafta")) soz = DateTime.Today.AddDays(7); else if (m.Contains("ay sonu")) soz = TahsilatOrnek.AySonu(DateTime.Today);
            if (m.Contains("itiraz"))
            {
                icerik.Add("İtiraz"); TahsilatTakipKayit.NotEkle(k.Kod, "WhatsApp", yet, "İtiraz Var", "", 0, "WhatsApp: " + metin, kul, out _);
                var h = cari == null ? "e-mutabakat yok" : EMutabakatOrnek.Yanit(k.Kod, 0, "İtirazlı", "WhatsApp: " + metin, kul); sonuc.Add("İtiraz görüşme notu olarak kaydedildi" + (h == null ? "; e-mutabakat İtirazlı işaretlendi." : ".")); uyari.Add($"{k.KisaAd} WhatsApp'tan İTİRAZ etti: {metin}");
            }
            else if (m.Contains("mutabik") || m.Contains("mutabık") || m.Contains("onayliyoruz") || m.Contains("dogrudur"))
            {
                icerik.Add("Mutabık"); var h = cari == null ? "e-mutabakat yok" : EMutabakatOrnek.Yanit(k.Kod, 0, "Mutabık", "WhatsApp: " + metin, kul); TahsilatTakipKayit.NotEkle(k.Kod, "WhatsApp", yet, "Bilgi Verildi", "", 0, "WhatsApp mutabakat yanıtı: " + metin, kul, out _);
                sonuc.Add(h == null ? "E-mutabakat MUTABIK işaretlendi." : "Mutabık yanıtı not olarak kaydedildi (" + h + ").");
            }
            if (tutarE >= 1000 && (m.Contains("ode") || m.Contains("öde") || m.Contains("havale") || m.Contains("gonder") || m.Contains("yat") || soz != null))
            {
                var st = soz ?? DateTime.Today.AddDays(7); icerik.Add("Ödeme Sözü");
                var h = TahsilatTakipKayit.NotEkle(k.Kod, "WhatsApp", yet, "Ödeme Sözü Alındı", st.ToString("yyyy-MM-dd"), tutarE, "WhatsApp: " + metin, kul, out _);
                sonuc.Add(h == null ? $"Ödeme sözü görüşme notuna işlendi: {Tl(tutarE)} · {st:dd.MM.yyyy}." : "Ödeme sözü kaydedilemedi: " + h); uyari.Add($"{k.KisaAd} WhatsApp'tan ödeme sözü verdi: {Tl(tutarE)} · {st:dd.MM.yyyy}");
                TahsilatTakipKayit.IletisimEkle(k.Kod, "Gelen", "WhatsApp", kul, new[] { "Ödeme Sözü" }, metin, null, st.ToString("yyyy-MM-dd"), tutarE);
            }
            else TahsilatTakipKayit.IletisimEkle(k.Kod, "Gelen", "WhatsApp", kul, icerik.Count > 0 ? icerik : (talepler.Count > 0 ? new List<string> { "Belge Talebi" } : new List<string> { "Mesaj" }), metin);
            foreach (var t in talepler.Distinct())
            {
                var talep = TahsilatTakipKayit.TalepEkle(k.Kod, "WhatsApp", t, metin);
                if (oto && cari != null)
                {
                    var bag = BelgeBaglantilari(k.Kod, new[] { t }); string mutLink = ""; if (t == "Mutabakat") { var mt = EMutabakatOrnek.Metin(cari, "WhatsApp", "Tahsilat (otomatik)"); EMutabakatOrnek.Gonder(new[] { k.Kod }, "WhatsApp", "Tahsilat (otomatik)"); var ix = mt.Govde.IndexOf("http", StringComparison.OrdinalIgnoreCase); mutLink = ix >= 0 ? mt.Govde.Substring(ix).Split(' ', '\n', '\r')[0] : ""; }
                    var mesaj = IletisimMetni(cari, k, EMutabakatOrnek.AnaYetkili(cari), new List<string> { t }, bag, $"Talebiniz üzerine {t} ekte / bağlantıda.", "Tahsilat (otomatik yanıt)", mutLink);
                    var gid = TahsilatTakipKayit.IletisimEkle(k.Kod, "Giden", "WhatsApp", "Otomatik Yanıt", new[] { t }, mesaj, bag, "", 0, true, talep.Id); TahsilatTakipKayit.TalepGonderildi(talep.Id, gid.Id, true);
                    sonuc.Add($"{t} talebi OTOMATİK gönderildi (WhatsApp)."); uyari.Add($"{k.KisaAd} WhatsApp'tan {t} istedi; otomatik gönderildi.");
                }
                else { sonuc.Add($"{t} talebi kaydedildi; Aylık Süreç › Gelen Talepler'den gönderin."); uyari.Add($"{k.KisaAd} WhatsApp'tan {t} istiyor — gönderim bekliyor."); }
            }
            if (sonuc.Count == 0) { sonuc.Add("Mesaj gelen kutusuna kaydedildi."); uyari.Add($"{k.KisaAd} WhatsApp'tan yazdı: {metin}"); }
            return Json(new { success = true, message = $"{k.Kod} {k.KisaAd}: " + string.Join(" ", sonuc), uyari, kod = k.Kod });
        }
        // ---------- iletişim bilgileri (19 Kas 2026, kullanıcı: "Müşteri İletişimi penceresinde iletişim bilgilerini kolayca güncelleyebilelim") ----------
        public class KisiIstek { public string Kod { get; set; } public int Id { get; set; } public string AdSoyad { get; set; } public string Unvan { get; set; } public string Eposta { get; set; } public string CepTelefonu { get; set; } public bool AnaYetkili { get; set; } public bool Sil { get; set; } }
        static object KisiDto(MtKisi k) => new { k.Id, k.AdSoyad, k.Unvan, k.Departman, k.Eposta, k.CepTelefonu, k.AnaYetkili, k.MutabakatYetkilisi, wa = EMutabakatOrnek.WaNumara(k.CepTelefonu ?? "").Length >= 12, mail = !string.IsNullOrWhiteSpace(k.Eposta) && k.Eposta.Contains("@") };
        object KisiPaketi(MtCari c) { List<MtKisi> l; lock (MutabakatOrnek.Kilit) l = c.Kisiler.ToList(); return new { kisiler = l.OrderByDescending(k => k.AnaYetkili).ThenBy(k => k.Id).Select(KisiDto).ToList(), waUygun = EMutabakatOrnek.WaUygun(c), mailUygun = EMutabakatOrnek.MailUygun(c) }; }
        /// <summary>Müşterinin yetkili kişileri (iletişim penceresi).</summary>
        [HttpGet]
        public IActionResult IletisimKisiler(string kod) { var c = MutabakatOrnek.Bul(kod ?? ""); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." }); EMutabakatOrnek.Hazirla(); return Json(new { success = true, kod = c.CariKodu, paket = KisiPaketi(c) }); }
        /// <summary>Kişi ekle / güncelle / sil: ad soyad, unvan, cep (WhatsApp), e-posta, ana yetkili. Değişiklik cari günlüğüne yazılır.</summary>
        [HttpPost]
        public IActionResult IletisimKisiKaydet([FromBody] KisiIstek i)
        {
            var c = MutabakatOrnek.Bul(i?.Kod ?? ""); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." }); EMutabakatOrnek.Hazirla(); string ben = Ben().AdSoyad;
            string ad = (i.AdSoyad ?? "").Trim(), eposta = (i.Eposta ?? "").Trim(), cep = (i.CepTelefonu ?? "").Trim(), unvan = (i.Unvan ?? "").Trim();
            if (i.Sil)
            {
                MtKisi s; lock (MutabakatOrnek.Kilit) { s = c.Kisiler.FirstOrDefault(x => x.Id == i.Id); if (s == null) return Json(new { success = false, message = "Kişi bulunamadı." }); if (c.Kisiler.Count == 1) return Json(new { success = false, message = "Son kişi silinemez; önce başka bir yetkili ekleyin." }); c.Kisiler.Remove(s); if (s.AnaYetkili && c.Kisiler.Count > 0) { c.Kisiler[0].AnaYetkili = true; c.Kisiler[0].MutabakatYetkilisi = true; } }
                MutabakatOrnek.LogYaz(c, ben, "İletişim Kişisi Silindi", s.AdSoyad, "", $"{s.Eposta} / {s.CepTelefonu}");
                return Json(new { success = true, message = $"{s.AdSoyad} silindi.", paket = KisiPaketi(c) });
            }
            if (ad.Length < 2) return Json(new { success = false, message = "Ad soyad girin." }); if (eposta != "" && !eposta.Contains("@")) return Json(new { success = false, message = "E-posta adresi geçersiz." }); if (cep != "" && EMutabakatOrnek.WaNumara(cep).Length < 12) return Json(new { success = false, message = "Cep telefonu geçersiz (05xx xxx xx xx ya da +90…)." });
            if (eposta == "" && cep == "") return Json(new { success = false, message = "En az bir iletişim bilgisi (cep ya da e-posta) girin." });
            MtKisi k; string eski;
            lock (MutabakatOrnek.Kilit)
            {
                k = c.Kisiler.FirstOrDefault(x => x.Id == i.Id);
                if (k == null) { k = new MtKisi { Id = (c.Kisiler.Count == 0 ? 0 : c.Kisiler.Max(x => x.Id)) + 1, MutabakatYetkilisi = true, EpostaGonder = true }; c.Kisiler.Add(k); eski = "(yeni)"; } else eski = $"{k.AdSoyad} · {k.Unvan} · {k.Eposta} / {k.CepTelefonu}{(k.AnaYetkili ? " / ana" : "")}";
                k.AdSoyad = ad; k.Unvan = unvan; k.Eposta = eposta; k.CepTelefonu = cep; k.Telefon = string.IsNullOrWhiteSpace(k.Telefon) ? cep : k.Telefon;
                if (i.AnaYetkili || c.Kisiler.Count(x => x.AnaYetkili) == 0) { foreach (var x in c.Kisiler) x.AnaYetkili = false; k.AnaYetkili = true; k.MutabakatYetkilisi = true; }
            }
            MutabakatOrnek.LogYaz(c, ben, eski == "(yeni)" ? "İletişim Kişisi Eklendi" : "İletişim Bilgisi Güncellendi", eski, $"{k.AdSoyad} · {k.Unvan} · {k.Eposta} / {k.CepTelefonu}{(k.AnaYetkili ? " / ana" : "")}", "Tahsilat › Müşteri İletişimi");
            return Json(new { success = true, message = $"{k.AdSoyad} kaydedildi{(k.AnaYetkili ? " (ana yetkili)" : "")}: {(cep != "" ? "WhatsApp " + cep : "")}{(cep != "" && eposta != "" ? " · " : "")}{(eposta != "" ? "e-posta " + eposta : "")}.", paket = KisiPaketi(c) });
        }
        [HttpPost] public IActionResult OtoYanitAyarla([FromBody] OtoIstek i) { TahsilatTakipKayit.OtoYanitAyarla(i != null && i.Acik); return Json(new { success = true, message = i != null && i.Acik ? "Otomatik yanıt AÇIK: müşteri WhatsApp'tan belge isteyince hemen gönderilir." : "Otomatik yanıt KAPALI: talepler Gelen Talepler'de bekler." }); }
        /// <summary>Ödeme Planı PDF'leri (örnek belgeler: OdemePlani_…_Tahsilat_Simulasyon.pdf / _Faturalar.pdf düzeni). tur = simulasyon: tahsilat özeti (taslak ödeme satırları) + ödeme kapanış simülasyonu; tur = faturalar: aylık adat dağılımı ve vade farkı + fatura bazında ekstre detayı.</summary>
        [HttpGet]
        public IActionResult OdemePlaniPdf(string kod, string tur)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); if (k == null) return NotFound(); var c = MutabakatOrnek.Bul(k.Kod); var bugun = DateTime.Today; decimal oran = TahsilatOrnek.Oran(k.Kod);
            var acik = TahsilatOrnek.AcikFaturalar(k.Kod).OrderBy(f => f.Vade ?? f.Tarih).ThenBy(f => f.BelgeNo).ToList();
            decimal Tlk(MtHareket f) => Math.Round(f.Kalan * KurTl(f.ParaBirimi), 2); int Gun(MtHareket f) => (bugun - (f.Vade ?? f.Tarih).Date).Days; decimal Vf(MtHareket f) => Gun(f) > 0 ? Math.Round(Tlk(f) * oran / 100m / 30m * Gun(f), 2) : 0;
            bool sim = (tur ?? "") != "faturalar"; string no = $"A{bugun:yyyyMMdd}{Math.Abs(k.Kod.GetHashCode()) % 100:00}";
            var belge = new DaBelge { Baslik = sim ? "Ödeme Planı · Tahsilat Simülasyonu" : "Ödeme Planı · Faturalar", AltBaslik = $"No {no} · {bugun:dd.MM.yyyy} · SATICI {k.Sirket} · ALICI {k.ResmiUnvan ?? k.KisaAd} ({k.Kod})", Sirket = k.Sirket, Hazirlayan = Ben().AdSoyad };
            belge.Ozet.Add(("Bakiye (TL)", Tl(c?.BakiyeTl ?? 0))); belge.Ozet.Add(("Açık Fatura", $"{acik.Count} adet · {Tl(acik.Sum(Tlk))}")); belge.Ozet.Add(("Vadesi Geçmiş", Tl(acik.Where(f => Gun(f) > 0).Sum(Tlk)))); belge.Ozet.Add(("Bugüne Vade Farkı", Tl(acik.Sum(Vf)) + $" (aylık % {oran.ToString("0.##", Tr)})"));
            if (sim)
            {
                var taslak = TahsilatOrnek.Tahsilatlar.Where(t => t.CariKodu == k.Kod && !t.Islendi).SelectMany(t => t.Satirlar).OrderBy(s => s.Tarih).ToList();
                var tb = new DaBlok { Baslik = "Tahsilat Özeti", Kolonlar = new List<string> { "Ödeme Yöntemi", "Çek / Evrak No", "Vade Tarihi", "Kalan Gün", "Tutar", "Tutar (TL)" }, Sag = new List<bool> { false, false, false, true, true, true } };
                foreach (var s in taslak) tb.Satirlar.Add(new[] { s.Yontem, s.BelgeNo ?? "", Trh(s.Tarih), ((s.Tarih.Date - bugun).Days).ToString(), $"{N2(s.Tutar)} {s.Pb}", N2(s.TutarTl) });
                tb.Toplam = new[] { "TOPLAM", "", "", "", "", N2(taslak.Sum(s => s.TutarTl)) }; tb.Not = taslak.Count == 0 ? "Planlanmış (taslak) tahsilat satırı yok; ödeme satırları girildiğinde burada listelenir." : ""; belge.Bloklar.Add(tb);
                var sb = new DaBlok { Baslik = "Ödeme Kapanış Simülasyonu", Kolonlar = new List<string> { "Belge No", "Fatura Tarihi", "Vade Tarihi", "Gün", "Birim", "Kalan", "Kalan Bakiye (TL)", "VF Tutarı (TL)" }, Sag = new List<bool> { false, false, false, true, false, true, true, true } };
                foreach (var f in acik) sb.Satirlar.Add(new[] { f.BelgeNo, Trh(f.Tarih), Trh(f.Vade ?? f.Tarih), Gun(f).ToString(), f.ParaBirimi, N2(f.Kalan), N2(Tlk(f)), N2(Vf(f)) });
                sb.Toplam = new[] { "TOPLAM", $"{acik.Count} fatura", "", "", "", "", N2(acik.Sum(Tlk)), N2(acik.Sum(Vf)) }; sb.Not = $"Gün = bugün − vade (pozitif gecikme). VF = kalan (TL) × % {oran.ToString("0.##", Tr)} / 30 × gün. Bugün tahsil edilirse doğacak vade farkıdır (KDV hariç)."; belge.Bloklar.Add(sb);
            }
            else
            {
                var ab = new DaBlok { Baslik = "Aylık Adat Dağılımı ve Vade Farkı", Kolonlar = new List<string> { "Vade Dönemi (Ay-Yıl)", "Ortalama Vade", "Fatura Tutarı (TL)", "Net Gecikme", "Net Vade Farkı (TL)" }, Sag = new List<bool> { false, false, true, true, true } };
                foreach (var g in acik.GroupBy(f => new DateTime((f.Vade ?? f.Tarih).Year, (f.Vade ?? f.Tarih).Month, 1)).OrderBy(g => g.Key))
                {
                    decimal top = g.Sum(Tlk); double ortTick = top == 0 ? g.Average(f => (f.Vade ?? f.Tarih).Ticks) : g.Sum(f => (double)(f.Vade ?? f.Tarih).Ticks * (double)Tlk(f)) / (double)top; var ortVade = new DateTime((long)ortTick).Date; decimal gun = top == 0 ? 0 : g.Sum(f => Tlk(f) * Gun(f)) / top;
                    ab.Satirlar.Add(new[] { g.Key.ToString("MMMM yyyy", Tr).ToUpper(Tr), Trh(ortVade), N2(top), $"{Math.Max(0, gun).ToString("0.##", Tr)} Gün", N2(g.Sum(Vf)) });
                }
                ab.Toplam = new[] { "TOPLAM", "", N2(acik.Sum(Tlk)), "", N2(acik.Sum(Vf)) }; ab.Not = "Ortalama vade ve net gecikme tutar ağırlıklıdır; vadesi gelmemiş faturalarda gecikme 0 sayılır."; belge.Bloklar.Add(ab);
                var fb = new DaBlok { Baslik = "Fatura Bazında Ekstre Detayı", Kolonlar = new List<string> { "Belge No", "Fatura Tarihi", "Vade Tarihi", "Gecikme", "Birim", "Belge Tutarı", "Kalan Bakiye", "Kalan (TL)" }, Sag = new List<bool> { false, false, false, true, false, true, true, true } };
                foreach (var f in acik) fb.Satirlar.Add(new[] { f.BelgeNo, Trh(f.Tarih), Trh(f.Vade ?? f.Tarih), Gun(f).ToString(), f.ParaBirimi, N2(f.Borc), N2(f.Kalan), N2(Tlk(f)) });
                fb.Toplam = new[] { "TOPLAM", $"{acik.Count} fatura", "", "", "", "", "", N2(acik.Sum(Tlk)) }; belge.Bloklar.Add(fb);
            }
            return Dosya(DisaAktar.Pdf(belge), $"OdemePlani_{k.Kod.Replace(".", "")}_{bugun:yyyyMMdd}_{(sim ? "Tahsilat_Simulasyon" : "Faturalar")}", true);
        }

        /// <summary>Müşteri detayı (Vade Farkı Takibi'nde müşteriye tıklanınca): kart bilgisi, mizan bakiyesi, açık faturalar + ay sonu vade farkı, kesilen vade farkı faturaları ve ödenme durumu, son ödemeler, vade farkı sonrası satışlar, limit, mutabakat gönderimleri, kart günlüğündeki satış durumu kayıtları.</summary>
        [HttpGet]
        public IActionResult MusteriDetay(string kod)
        {
            var k = CariKartOrnek.KartBul(kod ?? ""); if (k == null || k.KartTipi != "Müşteri") return Json(new { success = false, message = "Müşteri bulunamadı." });
            EMutabakatOrnek.Hazirla(); var bugun = DateTime.Today; var aySonu = TahsilatOrnek.AySonu(bugun); var c = MutabakatOrnek.Bul(k.Kod); var r = CariKartOrnek.RiskHesapla(k); var tumFaturalar = TahsilatOrnek.Faturalar;
            var vf = VadeFarkiOzet(k, c, tumFaturalar, TahsilatOrnek.Tahsilatlar, bugun, aySonu); List<MtHareket> hareketler; if (c == null) hareketler = new List<MtHareket>(); else lock (MutabakatOrnek.Kilit) hareketler = c.Hareketler.ToList();
            var vfAy = TahsilatOrnek.MusteriVfKalemleri(k.Kod, aySonu).ToDictionary(x => x.BelgeNo); var vfBugun = TahsilatOrnek.MusteriVfKalemleri(k.Kod, bugun).ToDictionary(x => x.BelgeNo);
            var vff = tumFaturalar.Where(f => f.CariKodu == k.Kod && f.Tur == "Vade Farkı").OrderByDescending(f => f.Tarih).ThenByDescending(f => f.Zaman).ToList(); var vffSon = VffReferans(vff, bugun);
            var acik = hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Kalan > 0.005m).OrderBy(h => h.Vade ?? h.Tarih).Select(h => new { belge = h.BelgeNo, tarih = h.Tarih.ToString("yyyy-MM-dd"), vade = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), gun = ((h.Vade ?? h.Tarih).Date - bugun).Days, pb = h.ParaBirimi, kalan = h.Kalan, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), islemTipi = h.IslemTipi, vfFaturasi = TahsilatOrnek.VfDisi(h),
                vfSon = TahsilatOrnek.VfDisi(h) || TahsilatOrnek.VfBaslangic(k.Kod, h) == (h.Vade ?? h.Tarih).Date ? "" : TahsilatOrnek.VfBaslangic(k.Kod, h).ToString("yyyy-MM-dd"), vfBugun = vfBugun.TryGetValue(h.BelgeNo, out var vb) ? vb.Tutar : 0, vfAySonu = vfAy.TryGetValue(h.BelgeNo, out var va) ? va.Tutar : 0 }).ToList();
            var odemeler = hareketler.Where(OdemeHareketi).OrderByDescending(h => h.Tarih).ThenByDescending(h => h.Id).Take(12).Select(h => new { tarih = h.Tarih.ToString("yyyy-MM-dd"), belge = h.BelgeNo, tip = h.BelgeTipi, tutar = h.Alacak, pb = h.ParaBirimi, tl = Math.Round(h.Alacak * KurTl(h.ParaBirimi), 2), aciklama = h.Aciklama, vfSonrasi = vffSon != null && VffSonrasi(h, vffSon) }).ToList();
            var satislar = vffSon == null ? null : hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && !TahsilatOrnek.VfDisi(h) && h.Tarih.Date > vffSon.Tarih.Date).OrderByDescending(h => h.Tarih).Select(h => new { tarih = h.Tarih.ToString("yyyy-MM-dd"), belge = h.BelgeNo, vade = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), tutar = h.Borc, pb = h.ParaBirimi, tl = Math.Round(h.Borc * KurTl(h.ParaBirimi), 2), kalan = h.Kalan }).ToList();
            var gonderimler = c == null ? null : EMutabakatOrnek.Gonderimler(c).Take(6).Select(g => new { g.BelgeNo, tarih = Zmn(g.Tarih), g.Kanal, g.Kime, g.Ozet, g.Sonuc, yanit = Trh(g.YanitTarihi), g.YanitAciklama, g.Gonderen, g.Tekrar }).ToList();
            var yetkili = c == null ? null : EMutabakatOrnek.AnaYetkili(c);
            var gunluk = k.Degisiklikler.Where(a => a.Alan == "Satış Durumu" || a.Alan == "Vade Farkı Faturası" || a.Alan == "Vade Farkı Robotu" || a.Alan == "Kredi Limiti").Take(12).Select(a => new { zaman = Zmn(a.Zaman), a.Kullanici, a.Alan, a.Eski, a.Yeni, a.Gerekce }).ToList();
            return Json(new { success = true, bugun = bugun.ToString("yyyy-MM-dd"), aySonu = aySonu.ToString("yyyy-MM-dd"),
                kart = new { k.Kod, k.KisaAd, k.ResmiUnvan, k.Sirket, k.Bolge, k.Ulke, k.Sehir, k.Sinif, iliski = k.IliskiDurumu, k.VadeSarti, k.Yetkili, k.Telefon, k.Eposta, temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), mutabakatYetkilisi = yetkili == null ? "" : $"{yetkili.AdSoyad} · {yetkili.Eposta}{(string.IsNullOrWhiteSpace(yetkili.CepTelefonu) ? "" : " · " + yetkili.CepTelefonu)}" },
                risk = new { durum = r.Durum, neden = r.Neden, gecmis = r.VadesiGecmis, gecmisAdet = r.VadesiGecmisAdet, maxGecikme = r.MaxGecikme, gelecek = r.VadesiGelecek, cek = r.Cek, cekAdet = r.CekAdet, bakiyeX = r.BakiyeX, bakiyeR = r.BakiyeR },
                limitAciklama = vf.Limit.Aciklama, vf = vf.Dto, takip = TakipOzet(k, c, TahsilatTakipKayit.Notlar(), bugun, Ben().AdSoyad), notlar = TahsilatTakipKayit.Notlar().Where(n => n.CariKodu == k.Kod).Select(n => NotDto(n, Ben().AdSoyad)).ToList(), acik, faturalar = vff.Select(FaturaDto).ToList(), odemeler, satislar, gonderimler, gunluk });
        }
        /// <summary>Müşteri bazında vade farkı faturası (ay sonunu beklemeden, bugüne kadar olan kesilmemiş gecikme günleri): seçilen her müşteriye tek fatura. Ay sonu kesimi kalan günlerden devam eder.</summary>
        [HttpPost]
        public IActionResult MusteriVadeFarki([FromBody] KodlarIstek i)
        {
            var kodlar = (i?.Kodlar ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(); if (kodlar.Count == 0) return Json(new { success = false, message = "Müşteri seçin." });
            string kisi = Ben().AdSoyad; var kesilen = new List<FarkFaturasi>(); var hatalar = new List<string>();
            foreach (var kod in kodlar) { var h = TahsilatOrnek.MusteriVadeFarkiKes(kod, DateTime.Today, "Elle", kisi, out var f); if (h != null) hatalar.Add(h); else kesilen.Add(f); }
            string mesaj = kesilen.Count == 0 ? "Fatura kesilemedi: " + string.Join(" · ", hatalar)
                : $"{kesilen.Count} müşteriye vade farkı faturası kesildi ({string.Join(", ", kesilen.Take(4).Select(f => $"{f.No} {f.CariAd}"))}{(kesilen.Count > 4 ? " …" : "")}) · toplam {Tl(kesilen.Sum(f => f.Toplam))} (KDV dahil)" + (hatalar.Count > 0 ? $" · {hatalar.Count} kesilemedi: " + string.Join(" · ", hatalar) : "") + ".";
            return Json(new { success = kesilen.Count > 0, message = mesaj, kesilen = kesilen.Select(f => f.No).ToList() });
        }
        /// <summary>Satış durdur uyarısı ver / kaldır (müşteri kartına yazılır; kart günlüğünde "Satış Durumu").</summary>
        [HttpPost]
        public IActionResult SatisDurdur([FromBody] SatisDurdurIstek i)
        {
            var k = CariKartOrnek.KartBul(i?.Kod ?? ""); var h = CariKartOrnek.SatisDurumuDegistir(k, i != null && i.Durdur, i?.Neden, Ben().AdSoyad); if (h != null) return Json(new { success = false, message = h });
            string temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct());
            return Json(new { success = true, message = i.Durdur ? $"{k.Kod} {k.KisaAd} için SATIŞ DURDURULDU: {k.SatisDurdurNeden}. Müşteri kartına işlendi{(temsilci == "" ? "" : $"; temsilci: {temsilci}")}." : $"{k.Kod} {k.KisaAd} için satış yeniden açıldı." });
        }
        /// <summary>Müşteriye doğrudan otomatik mutabakat (E-Mutabakat: kayıtlı mutabakat yetkilisinin e-postası / cep telefonu; Data/EMutabakatOrnek.cs). Kanal: E-posta | WhatsApp | İkisi.</summary>
        [HttpPost]
        public IActionResult MutabakatGonder([FromBody] KodlarIstek i)
        {
            var kodlar = (i?.Kodlar ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(); if (kodlar.Count == 0) return Json(new { success = false, message = "Müşteri seçin." });
            var r = EMutabakatOrnek.Gonder(kodlar, string.IsNullOrEmpty(i.Kanal) ? "E-posta" : i.Kanal, Ben().AdSoyad);
            string mesaj = r.Basarili == 0 ? "Mutabakat gönderilemedi: " + string.Join(" · ", r.Hatalar)
                : $"{r.Basarili} müşteriye mutabakat gönderildi ({string.Join(", ", r.Gidenler.Take(4).Select(g => $"{g.BelgeNo} → {g.Kime}"))}{(r.Gidenler.Count > 4 ? " …" : "")})" + (r.Hatalar.Count > 0 ? $" · {r.Hatalar.Count} gönderilemedi: " + string.Join(" · ", r.Hatalar) : "") + ".";
            return Json(new { success = r.Basarili > 0, message = mesaj });
        }

        // ---------- TAHSİLAT RAPORU (16 Kas 2026; Views/Rapor117/Index.cshtml, wwwroot/js/tahsilat-takip.js) ----------
        // Kullanıcı: "Tahsilat raporunu da İhracat Finansal Takibi tarzında geliştir; tahsilattaki kişi tüm detayları buradan takip edebilsin, toplu vade farkı kesebilsin,
        // banka hareketlerinden düşenleri müşteri seçimini buradan yapabilsin; Adat Hesaplama raporunu da tahsilata sekme olarak ekle."
        public class BankaMusteriIstek { public int Id { get; set; } public string CariKod { get; set; } public bool TaslakOlustur { get; set; } }
        public class TopluVadeFarkiIstek { public List<string> Nolar { get; set; } }
        /// <summary>Banka hareketinin bağlı olduğu tahsilat kaydı: ödeme satırının belge no'su banka hareket no'sudur (FNK-…).</summary>
        static TahsilatKaydi BankaTahsilati(string hareketNo) => TahsilatOrnek.Tahsilatlar.FirstOrDefault(t => t.Satirlar.Any(s => s.BelgeNo == hareketNo));
        static object BankaDto(BankaHareketi h)
        {
            var t = BankaTahsilati(h.No); bool tl = h.PB == "TRY" || string.IsNullOrEmpty(h.PB);
            return new { id = h.Id, no = h.No, sirket = h.Sirket, banka = h.Banka, tarih = h.Tarih.ToString("yyyy-MM-dd"), tutar = h.Tutar, pb = tl ? "TL" : h.PB, dovizTutar = tl ? h.Tutar : h.DovizTutar, kur = tl ? 1 : h.Kur, islemTuru = h.IslemTuru, karsiAd = h.KarsiAd, aciklama = h.Aciklama, referans = h.Referans,
                durum = h.Durum, cariKod = h.CariKod, cariAd = h.CariAd, onaylayan = h.Onaylayan, onayTarihi = Trh(h.OnayTarihi), yevmiyeNo = h.YevmiyeNo, tahsilatNo = t?.No, tahsilatDurum = t?.Durum,
                musteriVar = !string.IsNullOrEmpty(h.CariKod) && CariKartOrnek.KartBul(h.CariKod)?.KartTipi == "Müşteri" };   // onaylı harekette cari bir müşteri kartıysa taslak tahsilat açılabilir (POS toplu tahsilatı gibi kartsız onaylarda açılmaz)
        }
        /// <summary>Banka Tahsilatları sekmesi: bankadan gelen girişler (son 90 gün, bütün şirketler; Data/FinekraOrnek.cs) ve müşteri seçimi için müşteri carileri.</summary>
        [HttpGet]
        public IActionResult BankaHareketleri()
        {
            var l = FinekraOrnek.Listele(null, DateTime.Today.AddDays(-90)).Where(h => h.Giris).ToList();
            var musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").OrderBy(k => k.KisaAd, StringComparer.Create(Tr, true)).Select(k => new { kod = k.Kod, ad = k.KisaAd, unvan = k.ResmiUnvan, sirket = k.Sirket }).ToList();
            return Json(new { success = true, hareketler = l.Select(BankaDto).ToList(), musteriler });
        }
        /// <summary>
        /// Banka hareketine müşteri seçer: hareket seçilen müşteriyle onaylanır (FinekraOrnek.Onayla: 102 / 120 fişi; Takip Paneli'ndeki onayın aynısı). İstenirse hareket için TASLAK tahsilat açılır
        /// (Havale / EFT satırı, belge no = banka hareket no); taslak cariye işlenmez, Tahsilatlar sekmesinden "İşleme Al" ile işlenir. Aynı harekete ikinci tahsilat açılmaz.
        /// </summary>
        [HttpPost]
        public IActionResult BankaMusteriSec([FromBody] BankaMusteriIstek i)
        {
            var h = i == null ? null : FinekraOrnek.Bul(i.Id); if (h == null || !h.Giris) return Json(new { success = false, message = "Banka hareketi bulunamadı." });
            var k = CariKartOrnek.KartBul(i.CariKod); if (k == null || k.KartTipi != "Müşteri") return Json(new { success = false, message = "Müşteri seçin." });
            string kisi = Ben().AdSoyad; var ek = new List<string>();
            if (h.Durum != "Onaylandı")
            {
                var r = FinekraOrnek.Onayla(h.Id, k.Kod, k.KisaAd, "120", "Tahsilat raporundan müşteri seçildi", kisi);
                if (!r.ok) return Json(new { success = false, message = r.mesaj });
                ek.Add($"{h.No} {k.KisaAd} ile onaylandı" + (string.IsNullOrEmpty(h.YevmiyeNo) ? "" : $" (fiş {h.YevmiyeNo})"));
            }
            else if (!string.IsNullOrEmpty(h.CariKod) && h.CariKod != k.Kod && CariKartOrnek.KartBul(h.CariKod) != null) return Json(new { success = false, message = $"{h.No} zaten {h.CariAd} ile onaylı; müşterisi buradan değiştirilemez." });
            TahsilatKaydi kayit = BankaTahsilati(h.No);
            if (i.TaslakOlustur)
            {
                if (kayit != null) ek.Add($"tahsilat {kayit.No} zaten var");
                else
                {
                    bool tl = h.PB == "TRY" || string.IsNullOrEmpty(h.PB); string pb = tl ? "TL" : h.PB;
                    var satirlar = new List<TahsilatSatir> { new TahsilatSatir { Sira = 1, Yontem = "Havale / EFT", Tarih = h.Tarih.Date, BelgeNo = h.No, Banka = h.Banka, Pb = pb, Kur = tl ? 1 : h.Kur, KurElle = !tl, Tutar = tl ? h.Tutar : h.DovizTutar, Not = h.Referans } };
                    var hata = TahsilatOrnek.Kaydet(k.Kod, h.Tarih.Date, "R", pb, "Otomatik", null, satirlar, $"Banka hareketi {h.No} · {h.Banka} · {h.KarsiAd}", kisi, false, out kayit);
                    if (hata != null) ek.Add("tahsilat taslağı açılamadı: " + hata);
                    else
                    {
                        ek.Add($"taslak tahsilat {kayit.No} açıldı (Tahsilatlar sekmesinden işleme alınır)");
                        // tahsilat yalnız kendi para birimindeki faturaları kapatır (FIFO): müşterinin o para biriminde açık faturası yoksa tutar avans kalır
                        if (!TahsilatOrnek.AcikFaturalar(k.Kod).Any(f => f.ParaBirimi == pb)) ek.Add($"{k.KisaAd} carisinde {pb} açık fatura yok; işlenirse tutar avans kalır");
                    }
                }
            }
            return Json(new { success = true, message = string.Join(" · ", ek) + ".", hareket = BankaDto(h), tahsilatNo = kayit?.No, tahsilatlar = TahsilatOrnek.Tahsilatlar.Select(TahsilatDto), faturalar = FarkBelgeleri() });
        }
        /// <summary>Toplu vade farkı faturası: seçilen işlenmiş tahsilatların her biri için vade farkı faturası kesilir (TahsilatOrnek.FaturaKes; tahsilat başına bir fatura). Kesilemeyenler nedeniyle döner.</summary>
        [HttpPost]
        public IActionResult TopluVadeFarki([FromBody] TopluVadeFarkiIstek i)
        {
            var nolar = (i?.Nolar ?? new List<string>()).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().ToList(); if (nolar.Count == 0) return Json(new { success = false, message = "Tahsilat seçin." });
            string kisi = Ben().AdSoyad; var kesilen = new List<FarkFaturasi>(); var hatalar = new List<string>();
            foreach (var no in nolar) { var h = TahsilatOrnek.FaturaKes(no, "Vade Farkı", kisi, out var f); if (h != null) hatalar.Add($"{no}: {h}"); else kesilen.Add(f); }
            string mesaj = kesilen.Count == 0 ? "Fatura kesilemedi: " + string.Join(" · ", hatalar)
                : $"{kesilen.Count} vade farkı faturası kesildi ({kesilen[0].No}{(kesilen.Count > 1 ? " … " + kesilen[kesilen.Count - 1].No : "")}) · toplam {Tl(kesilen.Sum(f => f.Toplam))} (KDV dahil)" + (hatalar.Count > 0 ? $" · {hatalar.Count} kesilemedi: " + string.Join(" · ", hatalar) : "") + ".";
            return Json(new { success = kesilen.Count > 0, message = mesaj, kesilen = kesilen.Select(f => f.No).ToList(), tahsilatlar = TahsilatOrnek.Tahsilatlar.Select(TahsilatDto), faturalar = FarkBelgeleri() });
        }
    }
}
