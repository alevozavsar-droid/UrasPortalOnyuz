// SATIŞ SİPARİŞİ — Yurtiçi (/Rapor94) ve Yurtdışı / İhracat (/Rapor94/Ihracat); Satış ve Operasyon › Yurtiçi Satış › Satış İşlemleri · Yurtdışı Satış › İhracat Satış İşlemleri
// Elle yazılmış sürüm (üretilmiş Uretilen/Controllers/Rapor94Controller.cs URETILEN_KAPALI ile devre dışı). Veri: Data/SatisSiparisOrnek.cs.
// Satıcı Selvi Kimya; marka (Uras / Alv Kimya / Inknovators / Selvi) seçilir, belgeler (Proforma, Commercial Invoice, Packing List; yurtiçi Sipariş Formu / Proforma Fatura) tek tuşla markanın adıyla üretilir.
// Satışçı sipariş talepleri burada siparişe dönüşür; yükleme tarihi / saati planlanır ve süreç izlenir.
// Ekran: Views/Rapor94/Index.cshtml · betik: wwwroot/js/satis-siparis.js
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("Rapor94")]
    public class Rapor94Controller : Controller
    {
        private string Kod() => User?.Identity?.Name ?? "";
        private string Ad() => OrnekVeri.KullaniciAdi(Kod());

        IActionResult Sayfa(string bolge, string sekme, string no)
        {
            ViewData["Title"] = bolge == "Yurtdışı" ? "Yurtdışı Satış Siparişi" : "Yurtiçi Satış Siparişi";
            ViewBag.Bolge = bolge; ViewBag.Sekme = new[] { "siparisler", "talepler", "takvim", "yeni", "ozet" }.Contains(sekme) ? sekme : "siparisler"; ViewBag.No = no ?? ""; ViewBag.Ben = Ad();
            return View("Index");
        }
        /// <param name="sekme">siparisler | talepler | takvim | yeni | ozet</param><param name="no">açılışta çekmecede gösterilecek sipariş / talep no</param>
        [HttpGet("")] public IActionResult Index(string sekme, string no) => Sayfa("Yurtiçi", sekme, no);
        [HttpGet("Ihracat")] public IActionResult Ihracat(string sekme, string no) => Sayfa("Yurtdışı", sekme, no);

        [HttpGet("Veri")]
        public IActionResult Veri(string bolge)
        {
            bolge = bolge == "Yurtdışı" ? "Yurtdışı" : "Yurtiçi"; var l = SatisSiparisOrnek.Liste(bolge); var s = FiyatOrnek.SaticiBul(SatisSiparisOrnek.Satici);
            return Json(new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), ben = Ad(), bolge, satici = SatisSiparisOrnek.Satici,
                markalar = SatisSiparisOrnek.Markalar.Select(SatisSiparisOrnek.MarkaJson).ToList(), sirketler = SatisSiparisOrnek.Sirketler.Select(SatisSiparisOrnek.SirketJson).ToList(),
                musteriler = SatisSiparisOrnek.Musteriler(bolge).Select(k => new { kod = k.Kod, ad = k.KisaAd, ulke = k.Ulke, sehir = k.Sehir, sinif = k.Sinif, vade = k.VadeSarti, temsilci = SatisSiparisOrnek.Temsilci(k), ozelFiyat = s == null ? 0 : OzelFiyatOrnek.Fiyatlar(s.Ad, k.Kod).Count }).ToList(),
                temsilciler = SatisSiparisOrnek.Temsilciler(bolge), urunler = FiyatOrnek.Urunler.Where(u => u.Durum == "Aktif").Select(u => new { kod = u.Kod, ad = u.Ad, grup = u.GrupAnahtari.Replace("|", " / "), anaGrup = u.AnaGrup, seri = u.Seri, birim = u.Birim, hs = SatisSiparisOrnek.HsKodu(u.AnaGrup) }).ToList(),
                odemeSekilleri = bolge == "Yurtiçi" ? SatisSiparisOrnek.OdemeYurtici : SatisSiparisOrnek.OdemeYurtdisi, teslimSekilleri = bolge == "Yurtiçi" ? SatisSiparisOrnek.TeslimYurtici : SatisSiparisOrnek.TeslimYurtdisi,
                yuklemeYerleri = SatisSiparisOrnek.YuklemeYerleri, nakliyeler = bolge == "Yurtiçi" ? SatisSiparisOrnek.NakliyeYurtici : SatisSiparisOrnek.NakliyeYurtdisi, nakliyeFirmalari = SatisSiparisOrnek.NakliyeFirmalari,
                durumlar = SatisSiparisOrnek.Durumlar, asamalar = SatisSiparisOrnek.Asamalar, belgeTurleri = bolge == "Yurtiçi" ? SatisSiparisOrnek.BelgeTurleriYurtici : SatisSiparisOrnek.BelgeTurleriYurtdisi, paraBirimleri = bolge == "Yurtiçi" ? new[] { "TL" } : new[] { "USD", "EUR" },
                kur = new { usd = SatisSiparisOrnek.KurBul("USD"), eur = SatisSiparisOrnek.KurBul("EUR"), tarih = FiyatOrnek.Kur.Tarih.ToString("dd.MM.yyyy") },
                siparisler = l.Select(SatisSiparisOrnek.Json).ToList(), ozet = SatisSiparisOrnek.Ozet(l)
            });
        }

        /// <summary>Kalem fiyatı: müşterinin özel fiyatı (Muhataplar İçin Özel Fiyatlar) yoksa Liste Fiyatı, istenen para biriminde.</summary>
        [HttpGet("Fiyat")]
        public IActionResult Fiyat(string kod, string urun, string pb)
        {
            var u = FiyatOrnek.UrunBul(urun ?? ""); if (u == null) return Json(new { success = false, message = "Ürün bulunamadı." });
            var (f, kaynak) = SatisSiparisOrnek.Fiyat(kod, u, pb ?? "USD"); return Json(new { success = true, fiyat = f, kaynak, pb = pb ?? "USD", hs = SatisSiparisOrnek.HsKodu(u.AnaGrup) });
        }
        [HttpGet("Siparis")]
        public IActionResult Siparis(string no) { var s = SatisSiparisOrnek.Bul(no); return s == null ? Json(new { success = false, message = "Kayıt bulunamadı." }) : Json(new { success = true, siparis = SatisSiparisOrnek.Json(s) }); }

        [HttpPost("Kaydet")] public IActionResult Kaydet([FromBody] SsKayitIstek i) { var r = SatisSiparisOrnek.Kaydet(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, siparis = r.s == null ? null : SatisSiparisOrnek.Json(r.s) }); }
        [HttpPost("Donustur")] public IActionResult Donustur([FromBody] DonusturIstek i) { var r = SatisSiparisOrnek.Donustur(i?.No, i?.YuklemeTarihi, i?.YuklemeSaati, i?.YuklemeYeri, i?.Marka, Ad()); return Json(new { success = r.ok, message = r.mesaj, siparis = r.s == null ? null : SatisSiparisOrnek.Json(r.s) }); }
        [HttpPost("Yukleme")] public IActionResult Yukleme([FromBody] SsYuklemeIstek i) { var r = SatisSiparisOrnek.YuklemePlanla(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, siparis = r.s == null ? null : SatisSiparisOrnek.Json(r.s) }); }
        [HttpPost("Durum")] public IActionResult Durum([FromBody] SsDurumIstek i) { var r = SatisSiparisOrnek.DurumDegistir(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, siparis = r.s == null ? null : SatisSiparisOrnek.Json(r.s) }); }
        [HttpPost("Sirket")] public IActionResult Sirket([FromBody] SsSirketIstek i) { var r = SatisSiparisOrnek.SirketDegistir(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, siparis = r.s == null ? null : SatisSiparisOrnek.Json(r.s) }); }
        [HttpPost("Marka")] public IActionResult Marka([FromBody] SsMarkaIstek i) { var r = SatisSiparisOrnek.MarkaDegistir(i, Ad()); return Json(new { success = r.ok, message = r.mesaj, siparis = r.s == null ? null : SatisSiparisOrnek.Json(r.s) }); }
        public class DonusturIstek { public string No { get; set; } public string YuklemeTarihi { get; set; } public string YuklemeSaati { get; set; } public string YuklemeYeri { get; set; } public string Marka { get; set; } }

        /// <summary>Belge PDF'i: tur = Proforma Invoice | Commercial Invoice | Packing List | Sipariş Formu | Proforma Fatura | set (tek tuş: belge seti). Belge numarası sipariş kaydına yazılır.</summary>
        [HttpGet("Belge")]
        public IActionResult Belge(string no, string tur)
        {
            var r = SatisSiparisOrnek.BelgeOlustur(no, tur ?? "set", Ad()); if (!r.ok) return Content(r.mesaj, "text/plain; charset=utf-8");
            string ad = (tur == "set" || string.IsNullOrEmpty(tur) ? "belge-seti" : tur.ToLowerInvariant().Replace(" ", "-").Replace("ş", "s").Replace("İ", "i")) + "-" + (no ?? "").Replace("İ", "I") + ".pdf";
            return File(DisaAktar.Pdf(r.belge), "application/pdf", ad);
        }
        /// <summary>Sipariş listesi Excel (süzülmüş liste ekranın CSV'si yerine tam liste).</summary>
        [HttpGet("Excel")]
        public IActionResult Excel(string bolge)
        {
            bolge = bolge == "Yurtdışı" ? "Yurtdışı" : "Yurtiçi"; var l = SatisSiparisOrnek.Liste(bolge); var tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
            var b = new DaBelge { Baslik = bolge + " Satış Siparişleri", AltBaslik = $"{SatisSiparisOrnek.Satici} · {DateTime.Today:dd.MM.yyyy}", Sirket = SatisSiparisOrnek.Satici, Hazirlayan = Ad() };
            var bl = new DaBlok { Baslik = "Siparişler ve Talepler", Kolonlar = { "No", "Talep No", "Tarih", "Müşteri", "Ülke", "Marka", "Fatura Kesen", "Gönderici", "Temsilci", "Kalem", "Miktar (kg)", "Tutar", "PB", "Ödeme", "Teslim", "Yükleme", "Yükleme Yeri", "Nakliye", "Durum", "Belgeler", "Fatura" }, Sag = { false, false, false, false, false, false, false, false, false, true, true, true, false, false, false, false, false, false, false, false, false } };
            foreach (var s in l) bl.Satirlar.Add(new[] { s.No ?? "", s.TalepNo ?? "", s.Tarih.ToString("dd.MM.yyyy"), s.Cari, s.Ulke ?? "", SatisSiparisOrnek.MarkaBul(s.Marka).Ad, s.FaturaKesen, s.Gonderici, s.Temsilci, s.Kalemler.Count.ToString(), s.ToplamKg.ToString("N0", tr), s.Tutar.ToString("N2", tr), s.ParaBirimi, s.OdemeSekli ?? "", s.TeslimSekli ?? "", s.YuklemeTarihi != null ? $"{s.YuklemeTarihi:dd.MM.yyyy} {s.YuklemeSaati}" : "", s.YuklemeYeri ?? "", $"{s.Nakliye} {s.NakliyeFirmasi}".Trim(), s.Durum, string.Join(", ", s.Belgeler.Select(x => x.No)), s.FaturaNo ?? "" });
            b.Bloklar.Add(bl);
            return File(DisaAktar.Excel(b), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"satis-siparisleri-{(bolge == "Yurtiçi" ? "yurtici" : "ihracat")}-{DateTime.Today:yyyyMMdd}.xlsx");
        }
    }
}
