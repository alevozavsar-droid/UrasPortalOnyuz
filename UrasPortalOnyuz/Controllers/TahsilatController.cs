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
        public class Istek { public string Kod { get; set; } public string Tarih { get; set; } public string IslemTipi { get; set; } = "X"; public string Pb { get; set; } = "TL"; public string Mod { get; set; } = "Otomatik"; public Dictionary<string, decimal> Secim { get; set; } public List<SatirIstek> Satirlar { get; set; } public string Aciklama { get; set; } public bool Isle { get; set; } }
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
        static object DagitimDto(TahsilatDagitim d) => new { d.BelgeNo, faturaTarihi = Trh(d.FaturaTarihi), vade = Trh(d.Vade), d.Pb, d.IslemTipi, d.Kalan, kalanTr = N2(d.Kalan), d.Tutar, tutarTr = N2(d.Tutar), tutarTl = d.TutarTl, tutarTlTr = Tl(d.TutarTl), d.FaturaKuru, d.TahsilatKuru, d.GecikmeGun, d.VadeFarki, vadeFarkiTr = Tl(d.VadeFarki), d.KurFarki, kurFarkiTr = Tl(d.KurFarki), d.OdemeSira, d.OdemeYontem, odemeTarihi = Trh(d.OdemeTarihi), d.OdemeBelge, d.FaturaKalan, faturaKalanTr = N2(d.FaturaKalan) };
        static object TahsilatDto(TahsilatKaydi t) => t == null ? null : new { t.No, t.Tur, odeme = t.Odeme, tarih = Trh(t.Tarih), tarihIso = t.Tarih.ToString("yyyy-MM-dd"), t.CariKodu, t.CariAd, t.Sirket, sirketDb = CekAkibetOrnek.DbName(t.Sirket), panelSekme = t.Satirlar.Any(s => s.Yontem == "Havale / EFT" || s.Yontem == "Kredi Kartı") ? "banka" : t.Satirlar.Any(s => s.Yontem == "Nakit") ? "kasa" : "cek", t.Yontem, t.ParaBirimi, t.Tutar, tutarTr = N2(t.Tutar) + " " + t.ParaBirimi, t.Kur, tutarTl = t.TutarTl, tutarTlTr = Tl(t.TutarTl), t.IslemTipi, t.Mod, t.Aciklama, t.BelgeNo, t.Durum, islendi = t.Islendi, satirlar = t.Satirlar.Select(SatirDto), faturaSecim = t.FaturaSecim, dagitim = t.Dagitim.Select(DagitimDto), dagitilan = t.Dagitilan, avans = t.Avans, avansTr = N2(t.Avans) + " " + t.ParaBirimi, kurFarki = t.KurFarki, kurFarkiTr = Tl(t.KurFarki), vadeFarki = t.VadeFarki, vadeFarkiTr = Tl(t.VadeFarki), t.KurFarkiFaturaNo, t.VadeFarkiFaturaNo, t.Kullanici, zaman = Zmn(t.Zaman), t.IslemeAlan, islemeZamani = Zmn(t.IslemeZamani), satirAdet = t.Satirlar.Count, cekAdet = t.Satirlar.Count(s => s.Vadeli) };
        static object FaturaDto(FarkFaturasi f) => new { f.No, tarih = Trh(f.Tarih), vade = Trh(f.Vade), f.CariKodu, f.CariAd, f.CariUnvan, f.VergiNo, f.VergiDairesi, f.Tur, f.TahsilatNo, f.IslemTipi, kalemler = f.Kalemler.Select(k => new { aciklama = k.Aciklama, tutar = k.Tutar, tutarTr = Tl(k.Tutar) }), matrah = f.Matrah, matrahTr = Tl(f.Matrah), f.KdvOran, kdv = f.Kdv, kdvTr = Tl(f.Kdv), toplam = f.Toplam, toplamTr = Tl(f.Toplam), f.Kullanici, zaman = Zmn(f.Zaman), f.Durum };

        /// <summary>Müşteri carileri (süzgeç alanları, bakiye, X / R, vadesi geçmiş, risk, para birimleri), seçenekler, parametreler, kayıtlı tahsilatlar ve fark faturaları.</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            var ben = Ben();
            return Json(new { ben = new { ben.AdSoyad, ben.Departman, ben.Sirket }, odeme = OdemeMi, ad = Ad, kartTipi = KartTipi, cariler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == KartTipi).OrderBy(k => k.Kod).Select(CariDto), secenekler = CariSuzgec.Secenekler(), yontemler = TahsilatOrnek.Yontemler, bankalar = TahsilatOrnek.Bankalar, islemTipleri = TahsilatOrnek.IslemTipleri, kurlar = TahsilatOrnek.Kurlar, kurTarihi = Trh(MutabakatOrnek.KurTarihi), vadeFarkiAylikOran = TahsilatOrnek.VadeFarkiAylikOran, kdvOran = TahsilatOrnek.KdvOran, bugun = DateTime.Today.ToString("yyyy-MM-dd"), tahsilatlar = Kayitlar.Select(TahsilatDto), faturalar = TahsilatOrnek.Faturalar.Where(f => !OdemeMi).Select(FaturaDto) });
        }
        /// <summary>Carinin açık faturaları + tahsilat özeti (son ödeme, son yükleme, ortalama gecikme, eşleşmemiş ödeme) + kapatılabilir eşleşme sayısı.</summary>
        [HttpGet]
        public IActionResult Faturalar(string kod)
        {
            var bugun = DateTime.Today; var c = MutabakatOrnek.Bul(kod ?? ""); var oner = c == null ? 0 : KapamaOrnek.Oner(c).Count;
            return Json(new { success = true, faturalar = TahsilatOrnek.AcikFaturalar(kod ?? "").Select(f => new { f.BelgeNo, tarih = Trh(f.Tarih), vade = Trh(f.Vade ?? f.Tarih), vadeIso = (f.Vade ?? f.Tarih).ToString("yyyy-MM-dd"), gecikme = (bugun - (f.Vade ?? f.Tarih).Date).Days, pb = f.ParaBirimi, f.Kalan, kalanTr = N2(f.Kalan), tutar = f.Borc, faturaKuru = TahsilatOrnek.FaturaKuru(f), islemTipi = f.IslemTipi, f.Aciklama, f.KapamaDurumu }), bilgi = BilgiDto(TahsilatOrnek.CariBilgi(kod ?? "")), kapatilabilir = oner, kapatilabilirTl = c == null ? 0 : KapamaOrnek.Kapatilabilir(c) });
        }
        /// <summary>Tarihe göre otomatik kur (TCMB serisi).</summary>
        [HttpGet]
        public IActionResult Kur(string pb, string tarih) { var t = TarihCoz(tarih, DateTime.Today); var k = TahsilatOrnek.KurOto(pb, t); var tc = KurOrnek.Tcmb(pb, t); return Json(new { success = true, pb, tarih = t.ToString("yyyy-MM-dd"), kur = k, tahmini = tc.tahmini, kurTarihi = tc.tarih.ToString("dd.MM.yyyy"), aciklama = TahsilatOrnek.TlMi(pb) ? "TL" : tc.tahmini ? $"{t:dd.MM.yyyy} ileri tarih: bugünün kuru tahmini; gün gelince kesinleşir" : $"{tc.tarih:dd.MM.yyyy} TCMB kuru" }); }
        /// <summary>Dağıtım önizlemesi (kaydetmez): satır bazında eşleşme, gün, vade farkı, kur farkı ve toplamlar; hazırlanmış ödeme satırları (otomatik kur yazılmış).</summary>
        [HttpPost]
        public IActionResult Onizle([FromBody] Istek i)
        {
            if (i == null || string.IsNullOrEmpty(i.Kod)) return Json(new { success = false, message = "Cari seçin." });
            var sat = TahsilatOrnek.SatirlariHazirla(Satirlar(i), i.Pb ?? "TL", TarihCoz(i.Tarih, DateTime.Today)); var d = TahsilatOrnek.Hesapla(i.Kod, i.Pb ?? "TL", i.Mod, i.Secim, sat); decimal tutar = sat.Sum(s => s.Tutar);
            return Json(new { success = true, satirlar = sat.Select(SatirDto), dagitim = d.Select(DagitimDto), tutar, tutarTl = sat.Sum(s => s.TutarTl), dagitilan = d.Sum(x => x.Tutar), avans = Math.Round(tutar - d.Sum(x => x.Tutar), 2), vadeFarki = d.Sum(x => x.VadeFarki), kurFarki = d.Sum(x => x.KurFarki), kdvOran = TahsilatOrnek.KdvOran, kapananFatura = d.Select(x => x.BelgeNo).Distinct().Count(), tamKapanan = d.GroupBy(x => x.BelgeNo).Count(g => g.Last().FaturaKalan <= 0.005m) });
        }
        [HttpPost]
        public IActionResult Kaydet([FromBody] Istek i)
        {
            if (i == null) return Json(new { success = false, message = "Geçersiz istek." });
            if (CariKartOrnek.KartBul(i.Kod ?? "")?.KartTipi != KartTipi) return Json(new { success = false, message = $"Bu ekranda yalnız {KartTipi.ToLower(Tr)} carileri seçilir." });
            var h = TahsilatOrnek.Kaydet(i.Kod, TarihCoz(i.Tarih, DateTime.Today), i.IslemTipi, i.Pb ?? "TL", i.Mod, i.Secim, Satirlar(i), i.Aciklama, Ben().AdSoyad, i.Isle, out var k);
            if (h != null) return Json(new { success = false, message = h });
            return Json(new { success = true, message = i.Isle ? $"{k.No} kaydedildi ve cari hesabına işlendi: {N2(k.Tutar)} {k.ParaBirimi} ({k.Yontem}) · {k.Dagitim.Select(d => d.BelgeNo).Distinct().Count()} fatura{(k.Avans > 0.005m ? $", {N2(k.Avans)} {k.ParaBirimi} {(OdemeMi ? "tedarikçiye" : "bakiyeye")} avans" : "")}{(k.VadeFarki > 0 ? $" · vade farkı {Tl(k.VadeFarki)}" : "")}{(k.KurFarki != 0 ? $" · kur farkı {Tl(k.KurFarki)}" : "")}.{NakitAkisNotu(k)}" : $"{k.No} TASLAK olarak kaydedildi ({N2(k.Tutar)} {k.ParaBirimi}, {k.Yontem}); cariye işlenmedi. {ListeAdi} sekmesinden \"İşleme Al\" ile istediğiniz zaman işlenir.", tahsilat = TahsilatDto(k) });
        }
        /// <summary>Ödemede: işlenince açılan Kasa Nakit Akışı satırlarının notu.</summary>
        static string NakitAkisNotu(TahsilatKaydi k) { var l = k.Satirlar.Where(s => s.NakitAkisDocEntry != null).ToList(); return l.Count == 0 ? "" : $" Kasa Nakit Akışı'na ÖDENDİ satır{(l.Count > 1 ? "ları" : "ı")} açıldı: {string.Join(", ", l.Select(s => "#" + s.NakitAkisDocEntry))}; Ödeme Listesi'nde görünür."; }
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
        DaBelge SimulasyonBelgesi(string kod, string pb, string mod, Dictionary<string, decimal> secim, List<TahsilatSatir> sat, string islemTipi, string no, string durum, string aciklama)
        {
            var k = CariKartOrnek.KartBul(kod) ?? throw new Exception("Cari bulunamadı."); sat = TahsilatOrnek.SatirlariHazirla(sat, pb, DateTime.Today); var d = TahsilatOrnek.Hesapla(kod, pb, mod, secim, sat); var b = TahsilatOrnek.CariBilgi(kod);
            decimal tutar = sat.Sum(s => s.Tutar), dag = d.Sum(x => x.Tutar);
            var belge = new DaBelge { Baslik = string.IsNullOrEmpty(no) ? $"{Ad} Simülasyonu" : $"{Ad} {no} ({durum})", AltBaslik = $"{k.Kod} · {k.ResmiUnvan} · {k.Sirket} · işlem tipi {islemTipi} · {(mod == "Manuel" ? "manuel fatura seçimi" : "otomatik FIFO")}{(string.IsNullOrEmpty(aciklama) ? "" : " · " + aciklama)}", Sirket = k.Sirket, Hazirlayan = Ben().AdSoyad };
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
        [HttpPost] public IActionResult SimulasyonPdf([FromBody] Istek i) => Dosya(DisaAktar.Pdf(SimulasyonBelgesi(i.Kod, i.Pb ?? "TL", i.Mod, i.Secim, Satirlar(i), i.IslemTipi, "", "", i.Aciklama)), $"{DosyaOnEk}Simulasyon_{i.Kod}_{DateTime.Today:yyyyMMdd}", true);
        [HttpPost] public IActionResult SimulasyonExcel([FromBody] Istek i) => Dosya(DisaAktar.Excel(SimulasyonBelgesi(i.Kod, i.Pb ?? "TL", i.Mod, i.Secim, Satirlar(i), i.IslemTipi, "", "", i.Aciklama)), $"{DosyaOnEk}Simulasyon_{i.Kod}_{DateTime.Today:yyyyMMdd}", false);
        [HttpGet] public IActionResult TahsilatPdf(string no) { var t = TahsilatOrnek.TahsilatBul(no); if (t == null) return NotFound(); return Dosya(DisaAktar.Pdf(KayitBelgesi(t)), $"{(t.Odeme ? "Odeme" : "Tahsilat")}_{t.No}", true); }
        [HttpGet] public IActionResult TahsilatExcel(string no) { var t = TahsilatOrnek.TahsilatBul(no); if (t == null) return NotFound(); return Dosya(DisaAktar.Excel(KayitBelgesi(t)), $"{(t.Odeme ? "Odeme" : "Tahsilat")}_{t.No}", false); }
        DaBelge KayitBelgesi(TahsilatKaydi t)
        {
            string ad = t.Odeme ? "Ödeme" : "Tahsilat";
            var k = CariKartOrnek.KartBul(t.CariKodu); var belge = new DaBelge { Baslik = $"{ad} Makbuzu {t.No}", AltBaslik = $"{t.CariKodu} · {k?.ResmiUnvan ?? t.CariAd} · {t.Sirket} · {Trh(t.Tarih)} · {t.Durum}{(t.Islendi ? $" ({t.IslemeAlan} {Zmn(t.IslemeZamani)})" : "")} · işlem tipi {t.IslemTipi} · {(t.Mod == "Manuel" ? "manuel fatura seçimi" : "otomatik FIFO")}{(string.IsNullOrEmpty(t.Aciklama) ? "" : " · " + t.Aciklama)}", Sirket = t.Sirket, Hazirlayan = t.Kullanici };
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
            var bugun = DateTime.Today; var son30 = bugun.AddDays(-30);
            var satirlar = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").Select(k =>
            {
                var c = MutabakatOrnek.Bul(k.Kod); var r = CariKartOrnek.RiskHesapla(k);
                var acik = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Kalan > 0.005m).ToList();
                var kalemler = acik.Select(h => new { vade = (h.Vade ?? h.Tarih).ToString("yyyy-MM-dd"), gun = ((h.Vade ?? h.Tarih).Date - bugun).Days, tl = Math.Round(h.Kalan * KurTl(h.ParaBirimi), 2), belge = h.BelgeNo, pb = h.ParaBirimi, kalan = h.Kalan, islemTipi = h.IslemTipi }).OrderBy(x => x.vade).ToList();
                decimal Kova(int a, int b) => kalemler.Where(x => x.gun >= a && x.gun <= b).Sum(x => x.tl);
                var tahs = c == null ? new List<MtHareket>() : c.Hareketler.Where(h => (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.Alacak > 0).ToList();
                var sonTahsilat = tahs.OrderByDescending(h => h.Tarih).FirstOrDefault();
                return new { suzgec = CariSuzgec.Alanlar(k, r), k.Kod, k.KisaAd, k.ResmiUnvan, k.Sirket, sirketDb = CekAkibetOrnek.DbName(k.Sirket), k.Bolge, k.Sinif, k.VadeSarti, temsilci = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), risk = r.Durum, riskNeden = r.Neden, bakiye = r.AcikBakiye, bakiyeX = r.BakiyeX, bakiyeR = r.BakiyeR,
                    gecmis = Kova(int.MinValue, -1), gecmisAdet = kalemler.Count(x => x.gun < 0), maxGecikme = r.MaxGecikme, buHafta = Kova(0, 7), g8_30 = Kova(8, 30), g31_60 = Kova(31, 60), g60 = Kova(61, int.MaxValue), acikToplam = kalemler.Sum(x => x.tl), acikAdet = kalemler.Count,
                    cek = r.Cek, cekAdet = r.CekAdet, son30Tahsilat = Math.Round(tahs.Where(h => h.Tarih >= son30).Sum(h => h.Alacak * KurTl(h.ParaBirimi)), 2), son30Adet = tahs.Count(h => h.Tarih >= son30), sonTahsilat = sonTahsilat == null ? "" : Trh(sonTahsilat.Tarih), sonTahsilatTl = sonTahsilat == null ? 0 : Math.Round(sonTahsilat.Alacak * KurTl(sonTahsilat.ParaBirimi), 2),
                    limitDurum = CariKartOrnek.LimitHesapla(k).Durum, kalemler };
            }).ToList();
            return Json(new { success = true, bugun = bugun.ToString("yyyy-MM-dd"), sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, db = s.DbName }), satirlar, tahsilatlar = TahsilatOrnek.Tahsilatlar.Select(TahsilatDto) });
        }
    }
}
