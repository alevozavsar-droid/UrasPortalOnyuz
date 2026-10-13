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
    /// ÇEK AKIBETİ RAPORU (Rapor2 › Index2, adres /CekAkibeti) — elle yazılmış; veri Data/CekAkibetOrnek.cs (Kasa Nakit Akışı ve Konsolide Finansal Takip Paneli ile ortak).
    /// Kasa Nakit Akışı'nda satıra seçilen belge burada "Planlandı" işlem türüyle ve planlanan tedarikçiyle görünür; ibraz / ciro bilgisi plana karşı kontrol edilir (açıklama sütunu + ibraz detayı).
    /// Üretilmiş sürüm Uretilen/Controllers/Rapor2Controller.cs içinde #if ile kapalı. Görünüm ana projeden (Views/Rapor2/Index2.cshtml; küçük eklemeler: Planlananlar kartı, kayıtla işlem türü güncelleme).
    /// </summary>
    [Authorize]
    [Route("CekAkibeti")]
    public class Rapor2Controller : Controller
    {
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        string Sirket() { string db = Request.Query["db"].ToString(); if (string.IsNullOrEmpty(db)) db = Request.Cookies["SelectedDatabase"] ?? "DefaultConnection"; return CekAkibetOrnek.SirketAdi(db); }

        [HttpGet(""), HttpGet("Index"), HttpGet("Index2")]
        public IActionResult Index2(string yil = null)
        {
            // şirketin portföyündeki belgeler + grup içi dolaşımla bu şirketten geçip ciro edilmiş belgeler (şirket açısından "Ciro Edildi")
            string sirket = Sirket(); var hepsi = CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == sirket || k.Dolasim.Any(a => a.Durum == "Gerçekleşti" && a.Eden == sirket)).ToList();
            var yillar = hepsi.Select(k => k.Vade.Year).Distinct().OrderByDescending(y => y).Select(y => y.ToString()).ToList();
            string secilen = !string.IsNullOrEmpty(yil) && yil != "all" ? yil : DateTime.Today.Year.ToString();
            var liste = hepsi.Where(k => yil == "all" || k.Vade.Year.ToString() == secilen).OrderBy(k => k.Vade).ThenBy(k => k.No).Select(k => k.Sirket == sirket ? Satir(k) : GecisSatiri(k, sirket)).ToList();
            ViewBag.CheckTransactionTypes = new[] { new CheckTransactionType { Code = "0", Description = "Seçiniz" } }.Concat(CekAkibetOrnek.IslemTurleri.Select(t => new CheckTransactionType { Code = t.Kod, Description = t.Ad })).ToList();
            ViewBag.MevcutYillar = yillar; ViewBag.SecilenYil = secilen; ViewData["CurrentDbDisplay"] = sirket;
            OrnekDoldurucu.Hazirla(this);
            return View("Index2", liste);
        }

        /// <summary>Çek Akıbeti kaydı → ekran satırı (ana projedeki alan adları).</summary>
        static Rapor2ViewModel Satir(CekKaydi k)
        {
            bool portfoyde = k.Portfoyde; string hareket = portfoyde ? "Portföyde" : k.IslemTuru == "İade Edildi" ? "İade Edildi" : "İşlem Yapıldı";
            string aciklama = k.IslemTuru == "Planlandı" ? $"Kasa Nakit Akışı #{k.PlanDocEntry} · {k.PlanlananTedarikci} için planlandı" + (k.PlanlamaTarihi != null ? $" ({k.PlanlamaTarihi:dd.MM.yyyy}, {k.Planlayan})" : "")
                : k.IbrazYeri != "" ? $"{k.IbrazTuru}: {k.IbrazYeri}" + (k.Kontrol == "Farklı" ? $" · DİKKAT: plan {k.PlanlananTedarikci} (farklı)" : k.Kontrol == "Uyumlu" ? " · plana uygun" : k.Kontrol == "Plansız İbraz" ? " · plansız" : "") : k.KullanimDurumu != "Kullanılabilir" ? k.KullanimDurumu : "";
            if (k.Dolasim.Count > 0) aciklama = $"Grup içi dolaşım {k.GerceklesenAdim}/{k.Dolasim.Count}: {k.DolasimOzeti}" + (k.Sirket != k.KaynakSirket ? $" · şu an {k.Sirket} portföyünde" : "") + " · " + aciklama;
            return new Rapor2ViewModel
            {
                DocEntry = k.DocEntry, CekNumarasi = k.No, BankaAdi = k.Banka, Sube = k.Sube, HareketDurumu = hareket, IslemTuru = k.IslemTuru, KonumDurumu = k.KonumDurumu + (k.Portfoyde && !string.IsNullOrEmpty(k.KaynakSirket) && k.Sirket != k.KaynakSirket ? " · " + k.Sirket : ""), VadeDurumu = k.VadeDurumu, Aciklama = aciklama,
                // uygunluk: portföyde (ya da Kasa Nakit Akışı'nda planlı) ve kullanılabilir belge "Uygun"; ciro / ibraz / teminat ile çıkan belge "Uygun Değil"
                UygunlukDurumu = k.Portfoyde && k.KullanimDurumu == "Kullanılabilir" ? "Uygun" : "Uygun Değil", CekKimdenGeldi = k.Musteri, AsilBorclu = k.AsilBorclu, Tutar = k.Tutar, ParaBirimi = k.PB, VadeTarihi = k.Vade,
                VadeAyiVeYili = ButceOrnek.Aylar[k.Vade.Month - 1] + " " + k.Vade.Year, VadeliCekIbrazi = k.IbrazTuru == "Bankaya İbraz" ? "Evet" : "", PortfoyVadeAyiYili = ButceOrnek.Aylar[k.PortfoyGiris.Month - 1] + " " + k.PortfoyGiris.Year,
                PortfoyeGirisTarihi = k.PortfoyGiris, IbrazTarihi = k.IbrazTarihi, TahsilatBelgeNo = k.TahsilatBelgeNo, IbrazBelgeNo = k.IbrazBelgeNo, PlanlananTedarikci = k.PlanlananTedarikci, BelgeTipi = k.Belge,
                CekIslemTuru = CekAkibetOrnek.IslemKodu(k.IslemTuru), IslemTipiAdi = k.IslemTuru, CekKimeVerildi = k.IbrazTuru == "Ciro" ? k.IbrazYeri : k.IbrazTuru == "Bankaya İbraz" ? k.IbrazYeri + " (Tahsil)" : k.IbrazTuru == "Teminat" ? k.IbrazYeri : "", TlKarsiligi = k.Tutar, KullanımDurumu = k.KullanimDurumu,
                CekinBankasi = k.Banka, GelmeNedeni = "Tahsilat", SirketAdi = k.Sirket, SirlamaTarihi = k.Vade, CekBankaAdi = k.Banka
            };
        }

        /// <summary>Dolaşım zincirinde bu şirketten geçmiş belge: şirket açısından grup içi ciro ile çıkmıştır (alan şirket, ciro belge no, tarih); belge şu an başka şirketin portföyündedir.</summary>
        static Rapor2ViewModel GecisSatiri(CekKaydi k, string sirket)
        {
            var s = Satir(k); var adim = k.Dolasim.Last(a => a.Durum == "Gerçekleşti" && a.Eden == sirket);
            s.HareketDurumu = "İşlem Yapıldı"; s.IslemTuru = "Ciro Edildi"; s.IslemTipiAdi = "Ciro Edildi"; s.CekIslemTuru = CekAkibetOrnek.IslemKodu("Ciro Edildi"); s.KonumDurumu = "Grup İçi Ciro · " + adim.Alan; s.CekKimeVerildi = adim.Alan; s.IbrazTarihi = adim.GercekTarih; s.IbrazBelgeNo = adim.BelgeNo ?? ""; s.SirketAdi = sirket;
            s.Aciklama = $"Grup içi dolaşım {k.GerceklesenAdim}/{k.Dolasim.Count}: {k.DolasimOzeti} · {sirket} → {adim.Alan} ciro edildi ({adim.BelgeNo}, {adim.GercekTarih:dd.MM.yyyy}, adım {adim.Sira}) · şu an {k.Sirket} portföyünde · plana uygun";
            return s;
        }

        static string Html(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");
        static string DetayHtml(CekKaydi k, string baslik)
        {
            string kontrolRozet = k.Kontrol == "Uyumlu" ? "<span class='badge bg-success'>Uyumlu</span>" : k.Kontrol == "Farklı" ? "<span class='badge bg-danger'>FARKLI — plana aykırı çıkış</span>" : k.Kontrol == "İbraz Bekliyor" ? "<span class='badge bg-warning text-dark'>İbraz Bekliyor</span>" : k.Kontrol == "Plansız İbraz" ? "<span class='badge bg-info text-dark'>Plansız İbraz</span>" : "<span class='text-muted'>—</span>";
            return $@"<div class='p-2'><h6 class='fw-bold mb-2'>{Html(baslik)} · {Html(k.No)}</h6><table class='table table-sm mb-2'><tbody>
<tr><th class='text-muted' style='width:200px'>Belge</th><td>{Html(k.Belge)} · {Html(k.Banka)} / {Html(k.Sube)}</td></tr>
<tr><th class='text-muted'>Keşideci / Kimden Geldi</th><td>{Html(k.AsilBorclu)} / {Html(k.Musteri)}</td></tr>
<tr><th class='text-muted'>Vade · Tutar</th><td>{k.Vade:dd.MM.yyyy} · {k.Tutar.ToString("N2", Tr)} {Html(k.PB)}</td></tr>
<tr><th class='text-muted'>İşlem Türü</th><td>{Html(k.IslemTuru)}</td></tr>
<tr><th class='text-muted'>Kasa Nakit Akışı Planı</th><td>{(k.PlanlananTedarikci == "" ? "<span class='text-muted'>plan yok</span>" : Html(k.PlanlananTedarikci) + (k.PlanDocEntry != null ? $" · kayıt #{k.PlanDocEntry}" : "") + (k.PlanlamaTarihi != null ? $" · {k.PlanlamaTarihi:dd.MM.yyyy} {Html(k.Planlayan)}" : ""))}</td></tr>
<tr><th class='text-muted'>İbraz / Ciro</th><td>{(k.IbrazYeri == "" ? "<span class='text-muted'>henüz çıkış yok</span>" : Html(k.IbrazTuru) + ": " + Html(k.IbrazYeri) + (k.IbrazTarihi != null ? $" · {k.IbrazTarihi:dd.MM.yyyy}" : "") + (k.IbrazBelgeNo != "" ? " · belge " + Html(k.IbrazBelgeNo) : "") + (k.TahsilatBelgeNo != "" ? " · tahsilat " + Html(k.TahsilatBelgeNo) : ""))}</td></tr>
<tr><th class='text-muted'>Plan ↔ İbraz Kontrolü</th><td>{kontrolRozet}</td></tr></tbody></table>
<div class='small text-muted'>Kaynak: Kasa Nakit Akışı (Rapor79) › Çek / Senet sekmesi · Konsolide Finansal Takip Paneli › Çek: plana aykırı çıkışlar.</div></div>";
        }
        /// <summary>Çek kaydı değişim sayacı: Kasa Nakit Akışı'nda plan / ibraz yapılıp bu sekmeye dönülünce sayfa kendini tazeler (Index2.cshtml visibilitychange).</summary>
        [HttpGet("Versiyon")] public IActionResult Versiyon() => Json(new { v = CekAkibetOrnek.Gunluk.Count + CekAkibetOrnek.Hareketler.Count });
        [HttpGet("GetIbrazDetay")]
        public IActionResult GetIbrazDetay(string no) { var k = CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.IbrazBelgeNo == no) ?? CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.No == no); return Content(k == null ? "<div class='p-3 text-muted'>İbraz belgesi bulunamadı.</div>" : DetayHtml(k, "İbraz Detayı"), "text/html; charset=utf-8"); }
        [HttpGet("GetTahsilatDetay")]
        public IActionResult GetTahsilatDetay(string no) { var k = CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.TahsilatBelgeNo == no) ?? CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.No == no); return Content(k == null ? "<div class='p-3 text-muted'>Tahsilat belgesi bulunamadı.</div>" : DetayHtml(k, "Tahsilat Detayı"), "text/html; charset=utf-8"); }

        // ---------- güncellemeler (bellek içi) ----------
        public class KullanimDurumuIstek { public int DocEntry { get; set; } public string NewKullanimDurumu { get; set; } }
        [HttpPost("UpdateKullanimDurumu")]
        public IActionResult UpdateKullanimDurumu([FromBody] KullanimDurumuIstek model) { var r = CekAkibetOrnek.KullanimDurumuDegistir(model?.DocEntry ?? 0, model?.NewKullanimDurumu, Kullanici); return Json(new { success = r.ok, message = r.mesaj }); }
        public class IbrazBelgeIstek { public string IbrazBelgeNo { get; set; } public string NewCheckTransactionType { get; set; } }
        [HttpPost("UpdateCheckTransactionTypeByIbrazBelge")]
        public IActionResult UpdateCheckTransactionTypeByIbrazBelge([FromBody] IbrazBelgeIstek model)
        {
            var k = CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.IbrazBelgeNo == model?.IbrazBelgeNo); if (k == null) return Json(new { success = false, message = "İbraz belgesi bulunamadı." });
            var r = NakitAkisOrnek.CekCikisKontrollu(k.DocEntry, model.NewCheckTransactionType, null, null, Kullanici); return Json(new { success = r.ok, message = r.mesaj, onayaDustu = r.onayaDustu, nakitAkisDocEntry = r.docEntry });
        }
        public class TahsilatIstek { public string TahsilatBelgeNo { get; set; } public string NewCheckTransactionType { get; set; } }
        [HttpPost("UpdateCheckTransactionTypeByTahsilat")]
        public IActionResult UpdateCheckTransactionTypeByTahsilat([FromBody] TahsilatIstek model)
        {
            var k = CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.TahsilatBelgeNo == model?.TahsilatBelgeNo); if (k == null) return Json(new { success = false, message = "Tahsilat belgesi bulunamadı." });
            var r = NakitAkisOrnek.CekCikisKontrollu(k.DocEntry, model.NewCheckTransactionType, null, null, Kullanici); return Json(new { success = r.ok, message = r.mesaj, onayaDustu = r.onayaDustu, nakitAkisDocEntry = r.docEntry });
        }
        public class DocEntryIslemIstek { public int DocEntry { get; set; } public string NewCheckTransactionType { get; set; } public string IbrazYeri { get; set; } public string IbrazBelgeNo { get; set; } }
        /// <summary>Belge numarası olmayan (portföydeki) çekler için kayıtla işlem türü güncelleme. Kasa Nakit Akışı'nda planlanmamış belge için çıkış türleri uygulanmaz; GMY onayına düşer (NakitAkisOrnek.CekCikisKontrollu).</summary>
        [HttpPost("UpdateCheckTransactionTypeByDocEntry")]
        public IActionResult UpdateCheckTransactionTypeByDocEntry([FromBody] DocEntryIslemIstek model) { var r = NakitAkisOrnek.CekCikisKontrollu(model?.DocEntry ?? 0, model?.NewCheckTransactionType, model?.IbrazYeri, model?.IbrazBelgeNo, Kullanici); return Json(new { success = r.ok, message = r.mesaj, onayaDustu = r.onayaDustu, nakitAkisDocEntry = r.docEntry }); }
        public class PlanlananTedarikciIstek { public int DocEntry { get; set; } public string NewPlannedSupplier { get; set; } }
        [HttpPost("UpdatePlannedSupplier")]
        public IActionResult UpdatePlannedSupplier([FromBody] PlanlananTedarikciIstek model) { var r = CekAkibetOrnek.PlanlananTedarikciDegistir(model?.DocEntry ?? 0, model?.NewPlannedSupplier, Kullanici); return Json(new { success = r.ok, message = r.mesaj }); }
        [HttpGet("SearchSuppliers")]
        public IActionResult SearchSuppliers(string term) { string q = (term ?? "").ToUpper(Tr); return Json(CekOrnek.TedarikciAdlari.Where(t => q == "" || t.ToUpper(Tr).Contains(q)).Take(20).ToList()); }
        [HttpGet("DownloadAttachment/{ibrazBelgeNo}")] public IActionResult DownloadAttachment(string ibrazBelgeNo) => Content("Ön yüz örneği: ibraz belgesi eki üretilmez.", "text/plain; charset=utf-8");

        public class ReportTemplateModel { public string RaporAdi { get; set; } public string SablonVerisi { get; set; } }
        [HttpPost("SaveUserTemplate")] public IActionResult SaveUserTemplate([FromBody] ReportTemplateModel model) => Json(new { success = true });
        [HttpGet("GetUserTemplate")] public IActionResult GetUserTemplate(string raporAdi) => Json(null);
        [HttpGet("GetFarkliIslemler")] public IActionResult GetFarkliIslemler() => Json(new { success = true, data = new object[0] });
        [HttpPost("SyncUBe1Aktar")] public IActionResult SyncUBe1Aktar([FromBody] object model) => Json(new { success = true, message = "Kayıt eşitlendi (ön yüz örneği)." });
    }
}
