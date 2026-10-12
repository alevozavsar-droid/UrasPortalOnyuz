using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FinansRaporlama.Controllers;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// KASA NAKİT AKIŞI (Rapor79) — elle yazılmış denetleyici; bellek içi örnek veri Data/NakitAkisOrnek.cs (ödeme planı) + Data/CekAkibetOrnek.cs (çek / senet kaydı).
    /// Görünüm ana projeden (Views/Rapor79/Index.cshtml + wwwroot/js/rapor79.js); ödeme yöntemi sekmeleri ve belge seçimi: görünümdeki yöntem panelleri + wwwroot/js/rapor79-yontem.js.
    /// Çek / Senet satırına belge seçilince (BelgePlanla) belge Çek Akıbeti Raporu'nda "Planlandı" olur; planlanan tedarikçi = satırın kurumu. Üretilmiş sürüm Uretilen/Controllers/Rapor79Controller.cs içinde #if ile kapalı (model sınıfları orada).
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class Rapor79Controller : Controller
    {
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        static string Db(string dbName) => string.IsNullOrWhiteSpace(dbName) ? "URASKIMYA" : dbName;
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

        /// <summary>Modern ekran (Views/Rapor79/Index.cshtml + wwwroot/js/kasa-nakit.js): veri /Rapor79/Veri ile gelir. ?sirket=URASKIMYA&bas=&bit=&sekme=tumu|nakit|cek|senet|havale|kredi|mahsup|temlik|yansitma|diger|bakiye|gunluk</summary>
        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sirket, string bas, string bit, string sekme, DateTime? startDate, DateTime? endDate)
        {
            ViewData["Title"] = "Kasa Nakit Akışı";
            ViewBag.Sirketler = NakitAkisOrnek.Sirketler.Select(s => new KeyValuePair<string, string>(s.DbName, s.Ad)).ToList();
            ViewBag.VarsayilanSirket = NakitAkisOrnek.Sirketler.Any(s => s.DbName == sirket) ? sirket : sirket == "" ? "" : "URASKIMYA";
            ViewBag.Bas = (startDate ?? (DateTime.TryParse(bas, CultureInfo.InvariantCulture, DateTimeStyles.None, out var b1) ? b1 : DateTime.Today.AddDays(-30))).ToString("yyyy-MM-dd");
            ViewBag.Bit = (endDate ?? (DateTime.TryParse(bit, CultureInfo.InvariantCulture, DateTimeStyles.None, out var b2) ? b2 : DateTime.Today.AddDays(60))).ToString("yyyy-MM-dd");
            ViewBag.Sekme = sekme ?? "tumu";
            OrnekDoldurucu.Hazirla(this);
            return View("Index");
        }
        /// <summary>Ana projedeki eski görünüm (Views/Rapor79/Eski.cshtml; 23 sütunlu tablo, rapor79.js) — karşılaştırma için.</summary>
        [HttpGet("Eski")]
        public IActionResult Eski(DateTime? startDate, DateTime? endDate, string viewRole)
        {
            var bas = (startDate ?? DateTime.Today.AddDays(-30)).Date; var bit = (endDate ?? DateTime.Today.AddDays(60)).Date;
            var model = new KonsolideDashboardViewModel { StartDate = bas, EndDate = bit };
            foreach (var s in NakitAkisOrnek.Sirketler)
                model.Companies.Add(new CompanyDashboardViewModel
                {
                    DbName = s.DbName, DisplayName = NakitAkisOrnek.GorunenAd(s.DbName),
                    OdemeListesi = NakitAkisOrnek.Odemeler(s.DbName).Where(m => m.U_PlanlananTarih == null || (m.U_PlanlananTarih.Value.Date >= bas && m.U_PlanlananTarih.Value.Date <= bit)).ToList(),
                    KasaBakiyeleri = NakitAkisOrnek.Bakiyeler(s.DbName)
                });
            ViewBag.GizliKolonlar = ""; ViewBag.ViewRole = viewRole ?? "all";
            OrnekDoldurucu.Hazirla(this);
            return View("Eski", model);
        }

        // ---------- modern ekran veri ve kayıt uçları ----------
        [HttpGet("Veri")]
        public IActionResult Veri(string sirket, string bas, string bit)
        {
            var b1 = DateTime.TryParse(bas, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1) ? d1.Date : DateTime.Today.AddDays(-30);
            var b2 = DateTime.TryParse(bit, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2) ? d2.Date : DateTime.Today.AddDays(60);
            var dbler = string.IsNullOrEmpty(sirket) ? NakitAkisOrnek.Sirketler.Select(s => s.DbName).ToList() : new List<string> { sirket };
            var odemeler = dbler.SelectMany(db => NakitAkisOrnek.Odemeler(db)).Where(m => m.U_PlanlananTarih == null || (m.U_PlanlananTarih.Value.Date >= b1 && m.U_PlanlananTarih.Value.Date <= b2)).OrderBy(m => m.U_PlanlananTarih).ThenBy(m => m.DocEntry).Select(SatirJson).ToList();
            var bakiyeler = dbler.SelectMany(db => NakitAkisOrnek.Bakiyeler(db).Select(b => new { dbName = db, sirket = NakitAkisOrnek.SirketAdi(db), bankaKasa = b.BankaKasa, detay = b.Detay, pb = b.PB, tutar = b.Tutar, tutarTL = b.TutarTL, tarih = b.GuncellenmeTarihi?.ToString("yyyy-MM-dd") })).ToList();
            var gunluk = NakitAkisOrnek.Gunluk(string.IsNullOrEmpty(sirket) ? null : sirket).Take(300).Select(g => new { tarih = g.IslemTarihi.ToString("yyyy-MM-dd HH:mm"), kullanici = g.Kullanici, dbName = g.DbName, sirket = NakitAkisOrnek.SirketAdi(g.DbName), docEntry = g.DocEntry, tip = g.IslemTipi, aciklama = g.Aciklama }).ToList();
            return Json(new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), bas = b1.ToString("yyyy-MM-dd"), bit = b2.ToString("yyyy-MM-dd"),
                sirketler = NakitAkisOrnek.Sirketler.Select(s => new { ad = s.Ad, dbName = s.DbName, dbKey = s.DbKey }), yontemler = NakitAkisOrnek.OdemeYontemleri, durumlar = NakitAkisOrnek.Durumlar, satinalmacilar = NakitAkisOrnek.Satinalmacilar,
                bankaHesaplari = NakitAkisOrnek.BankaHesaplari, kasalar = NakitAkisOrnek.Kasalar, kartlar = NakitAkisOrnek.Kartlar, taksitler = NakitAkisOrnek.TaksitSecenekleri, kurlar = NakitAkisOrnek.Kurlar, tedarikciler = Tedarikciler(Db(sirket)),
                odemeler, bakiyeler, gunluk, grupIci = NakitAkisOrnek.GrupIciOzet(sirket)
            });
        }
        public class AlanIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string Alan { get; set; } public string Deger { get; set; } }
        /// <summary>Satır içi düzenleme: ana alan (durum, tarih, tutar, kurum …) ya da yöntem alanı.</summary>
        [HttpPost("AlanKaydet")]
        public IActionResult AlanKaydet([FromBody] AlanIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = NakitAkisOrnek.AlanKaydet(Db(istek.DbName), istek.DocEntry, istek.Alan, istek.Deger, Kullanici); var m = NakitAkisOrnek.Bul(Db(istek.DbName), istek.DocEntry); if (m != null) NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new { success = r.ok, message = r.mesaj, row = m == null ? null : SatirJson(m) });
        }
        /// <summary>Çekmece: satırın tüm alanları (JSON gövde NakitAkisViewModel; dbName sorgu).</summary>
        [HttpPost("SatirKaydet")]
        public IActionResult SatirKaydet([FromBody] NakitAkisViewModel model, [FromQuery] string dbName)
        {
            if (model == null) return Json(new { success = false, message = "Veri yok." });
            bool ok = NakitAkisOrnek.Guncelle(Db(dbName), model, Kullanici); var m = NakitAkisOrnek.Bul(Db(dbName), model.DocEntry); if (m != null) NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new { success = ok, message = ok ? "Kaydedildi." : "Satır bulunamadı.", row = m == null ? null : SatirJson(m) });
        }
        [HttpPost("SatirEkle")]
        public IActionResult SatirEkle([FromBody] NakitAkisViewModel model, [FromQuery] string dbName)
        {
            if (model == null) return Json(new { success = false, message = "Veri yok." });
            var m = NakitAkisOrnek.Ekle(Db(dbName), model, Kullanici); NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new { success = true, message = $"Ödeme kaydı eklendi (#{m.DocEntry}).", row = SatirJson(m) });
        }
        /// <summary>Grup içi modül kayıtları (Mahsup / Temlik / Yansıtma) — seçim için, modülden canlı.</summary>
        [HttpGet("GrupIciKayitlar")] public IActionResult GrupIciKayitlar(string dbName, string tur) => Json(new { success = true, data = NakitAkisOrnek.GrupIciKayitlar(Db(dbName), tur ?? "Mahsup") });
        public class GrupIciIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string Tur { get; set; } public string No { get; set; } }
        [HttpPost("GrupIciBagla")]
        public IActionResult GrupIciBagla([FromBody] GrupIciIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = NakitAkisOrnek.GrupIciBagla(Db(istek.DbName), istek.DocEntry, istek.Tur, istek.No, Kullanici); if (r.satir != null) NakitAkisOrnek.BelgeleriDoldur(r.satir);
            return Json(new { success = r.ok, message = r.mesaj, row = r.satir == null ? null : SatirJson(r.satir) });
        }
        [HttpPost("GrupIciKaldir")]
        public IActionResult GrupIciKaldir([FromBody] GrupIciIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = NakitAkisOrnek.GrupIciKaldir(Db(istek.DbName), istek.DocEntry, Kullanici); if (r.satir != null) NakitAkisOrnek.BelgeleriDoldur(r.satir);
            return Json(new { success = r.ok, message = r.mesaj, row = r.satir == null ? null : SatirJson(r.satir) });
        }
        public class DekontIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string Ad { get; set; } public string Aciklama { get; set; } }
        [HttpPost("DekontEkle")]
        public IActionResult DekontEkle([FromBody] DekontIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = NakitAkisOrnek.DekontEkle(Db(istek.DbName), istek.DocEntry, istek.Ad, istek.Aciklama, Kullanici); if (r.satir != null) NakitAkisOrnek.BelgeleriDoldur(r.satir);
            return Json(new { success = r.ok, message = r.mesaj, row = r.satir == null ? null : SatirJson(r.satir) });
        }
        [HttpPost("SilJson")]
        public IActionResult SilJson([FromBody] GrupIciIstek istek) => Json(new { success = istek != null && NakitAkisOrnek.Sil(Db(istek.DbName), istek.DocEntry, Kullanici) });

        [HttpGet("ExportExcel")] public IActionResult ExportExcel(DateTime? startDate, DateTime? endDate, string viewRole) => Content("Ön yüz örneğinde Excel üretimi yok.", "text/plain; charset=utf-8");

        // ---------- satır kaydetme ----------
        [HttpPost("AjaxRowGuncelle")]
        public IActionResult AjaxRowGuncelle([FromBody] NakitAkisViewModel model, [FromQuery] string dbName)
        {
            if (model == null) return Json(new { success = false, message = "Veri yok." });
            bool ok = NakitAkisOrnek.Guncelle(Db(dbName), model, Kullanici);
            return Json(new { success = ok, message = ok ? "Kaydedildi." : "Satır bulunamadı." });
        }
        [HttpPost("Guncelle"), ValidateAntiForgeryToken]
        public IActionResult Guncelle(string DbName, NakitAkisViewModel model, DateTime? startDate, DateTime? endDate)
        {
            TempData[NakitAkisOrnek.Guncelle(Db(DbName), model, Kullanici) ? "Success" : "Error"] = NakitAkisOrnek.Bul(Db(DbName), model.DocEntry) != null ? "Ödeme kaydı güncellendi (bellek içi)." : "Satır bulunamadı.";
            return RedirectToAction("Index", new { startDate = startDate?.ToString("yyyy-MM-dd"), endDate = endDate?.ToString("yyyy-MM-dd") });
        }
        [HttpPost("Ekle"), ValidateAntiForgeryToken]
        public IActionResult Ekle(string DbName, NakitAkisViewModel model, DateTime? startDate, DateTime? endDate)
        {
            var m = NakitAkisOrnek.Ekle(Db(DbName), model, Kullanici);
            TempData["Success"] = $"Ödeme kaydı eklendi: {m.U_Kurum} · {m.U_OdemeYontemi} · {m.U_Tutar:N2} {m.U_ParaBirimi} (bellek içi).";
            return RedirectToAction("Index", new { startDate = startDate?.ToString("yyyy-MM-dd"), endDate = endDate?.ToString("yyyy-MM-dd") });
        }
        [HttpPost("TopluGuncelle"), ValidateAntiForgeryToken]
        public IActionResult TopluGuncelle(string DbName, List<NakitAkisViewModel> OdemeListesi, DateTime? startDate, DateTime? endDate)
        {
            foreach (var m in OdemeListesi ?? new List<NakitAkisViewModel>()) NakitAkisOrnek.Guncelle(Db(DbName), m, Kullanici);
            return RedirectToAction("Index", new { startDate = startDate?.ToString("yyyy-MM-dd"), endDate = endDate?.ToString("yyyy-MM-dd") });
        }
        [HttpPost("Sil")] public IActionResult Sil(string dbName, int docEntry) => Json(new { success = NakitAkisOrnek.Sil(Db(dbName), docEntry, Kullanici) });

        // ---------- ÇEK / SENET: belge seçimi (yöntem sekmesi) ----------
        /// <summary>Seçilebilir belgeler: şirket portföyünde, planlanmamış, kullanılabilir çek ya da senetler.</summary>
        [HttpGet("SecilebilirBelgeler")]
        public IActionResult SecilebilirBelgeler(string dbName, string belge)
        {
            var l = NakitAkisOrnek.SecilebilirBelgeler(Db(dbName), belge);
            return Json(new { success = true, data = l.Select(k => new { docEntry = k.DocEntry, no = k.No, belge = k.Belge, banka = k.Banka, sube = k.Sube, asilBorclu = k.AsilBorclu, kimdenGeldi = k.Musteri, musteriKod = k.MusteriKod, vade = k.Vade.ToString("yyyy-MM-dd"), vadeDurumu = k.VadeDurumu, tutar = k.Tutar, pb = k.PB, portfoyGiris = k.PortfoyGiris.ToString("yyyy-MM-dd"), kullanim = k.KullanimDurumu }) });
        }
        public class BelgePlanIstek { public string DbName { get; set; } public int DocEntry { get; set; } public List<int> CekDocEntryler { get; set; } public int CekDocEntry { get; set; } }
        /// <summary>Belgeleri satıra bağlar; Çek Akıbeti kaydında "Planlandı" (planlanan tedarikçi = kurum).</summary>
        [HttpPost("BelgePlanla")]
        public IActionResult BelgePlanla([FromBody] BelgePlanIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = NakitAkisOrnek.BelgePlanla(Db(istek.DbName), istek.DocEntry, istek.CekDocEntryler ?? new List<int>(), Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, row = r.satir == null ? null : SatirJson(r.satir) });
        }
        [HttpPost("BelgePlanKaldir")]
        public IActionResult BelgePlanKaldir([FromBody] BelgePlanIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = NakitAkisOrnek.BelgePlanKaldir(Db(istek.DbName), istek.DocEntry, istek.CekDocEntry, Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, row = r.satir == null ? null : SatirJson(r.satir) });
        }
        [HttpGet("SatirBelgeleri")]
        public IActionResult SatirBelgeleri(string dbName, int docEntry)
        {
            var m = NakitAkisOrnek.Bul(Db(dbName), docEntry); if (m == null) return Json(new { success = false, message = "Satır bulunamadı." });
            NakitAkisOrnek.BelgeleriDoldur(m); return Json(new { success = true, row = SatirJson(m) });
        }
        public class YontemAlanIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string Alan { get; set; } public string Deger { get; set; } }
        /// <summary>Yöntem sekmesi alanı (IBAN, dekont no, kasa, makbuz, kart, taksit, mahsup fişi, temlik …).</summary>
        [HttpPost("YontemAlanKaydet")]
        public IActionResult YontemAlanKaydet([FromBody] YontemAlanIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = NakitAkisOrnek.YontemAlanKaydet(Db(istek.DbName), istek.DocEntry, istek.Alan, istek.Deger, Kullanici); return Json(new { success = r.ok, message = r.mesaj });
        }
        /// <summary>Satır JSON'u (modern ekran ve eski yöntem paneli ortak; planlananTarih / odemeYontemi eski betik için, planlanan / yontem yeni betik için).</summary>
        static object SatirJson(NakitAkisViewModel m) => new
        {
            docEntry = m.DocEntry, dbName = m.U_KaynakFirma, sirket = NakitAkisOrnek.SirketAdi(m.U_KaynakFirma), sira = m.U_Sira, planlanan = m.U_PlanlananTarih?.ToString("yyyy-MM-dd"), planlananTarih = m.U_PlanlananTarih?.ToString("yyyy-MM-dd"), belgeTarihi = m.BelgeTarihi?.ToString("yyyy-MM-dd"),
            kurum = m.U_Kurum, urun = m.U_UrunHizmet, pb = m.U_ParaBirimi, kur = m.U_Kur ?? 1, tutar = m.U_Tutar ?? 0, tutarTL = m.U_TutarTL ?? 0, yontem = m.U_OdemeYontemi, odemeYontemi = m.U_OdemeYontemi, odenme = m.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), odenen = m.U_OdenenTutar ?? 0, odenenPB = m.U_OdenenTutarPB, durum = m.U_Durum, notlar = m.U_Notlar,
            belgeTipi = m.U_BagliBelgeTipi, bagliDocEntry = m.U_BagliDocEntry, faturaNo = m.U_FaturaSiparisNo, islemTipi = m.U_IslemTipi, kayitTipi = m.U_KayitTipi, mutabakat = m.U_MutabakatDurumu, mutabakatTarihi = m.SonMutabakatTarihi?.ToString("yyyy-MM-dd"), satinalmaci = m.U_Satinalmaci, muafiyet = m.MuafiyetSebebi,
            dekontlar = m.DekontListesi.Select(d => new { id = d.Id, ad = d.DosyaAdi, yol = d.DosyaYolu, tarih = d.Tarih.ToString("yyyy-MM-dd HH:mm"), yukleyen = d.Yukleyen, aciklama = d.Aciklama }),
            cekNolar = m.U_CekNolar, cekDocEntryler = m.U_CekDocEntryler,
            belgeler = m.Belgeler.Select(b => new { cekDocEntry = b.CekDocEntry, no = b.No, belge = b.Belge, banka = b.Banka, asilBorclu = b.AsilBorclu, kimdenGeldi = b.KimdenGeldi, vade = b.Vade.ToString("yyyy-MM-dd"), tutar = b.Tutar, pb = b.PB, akibet = b.Akibet, ibrazYeri = b.IbrazYeri, kontrol = b.Kontrol, planlananTedarikci = b.PlanlananTedarikci }),
            alanlar = new { bankaHesabi = m.U_BankaHesabi, aliciIban = m.U_AliciIban, dekontNo = m.U_DekontNo, kasa = m.U_Kasa, makbuzNo = m.U_MakbuzNo, teslimAlan = m.U_TeslimAlan, kart = m.U_Kart, taksit = m.U_Taksit, ekstreDonemi = m.U_EkstreDonemi, mahsupFisNo = m.U_MahsupFisNo, karsiBelge = m.U_KarsiBelge, temlikNo = m.U_TemlikNo, temlikEden = m.U_TemlikEden, yansitmaNo = m.U_YansitmaNo },
            grupIci = NakitAkisOrnek.GrupIciBilgi(m)
        };

        // ---------- portföy çekleri (ana projedeki uçlar) ----------
        [HttpGet("GetPortfoyCekleriDetayli")]
        public IActionResult GetPortfoyCekleriDetayli(string dbName)
        {
            var l = NakitAkisOrnek.SecilebilirBelgeler(Db(dbName), "Çek").Concat(NakitAkisOrnek.SecilebilirBelgeler(Db(dbName), "Senet")).OrderBy(k => k.Vade).ToList();
            return Json(new { success = true, data = l.Select(k => new DetayliCekViewModel { DocEntry = k.DocEntry, CekNumarasi = k.No, BankaAdi = k.Banka, Sube = k.Sube, CekKimdenGeldi = k.Musteri, AsilBorclu = k.AsilBorclu, CekTutari = k.Tutar, ParaBirimi = k.PB, VadeTarihi = k.Vade.ToString("yyyy-MM-dd"), PortfoyeGirisTarihi = k.PortfoyGiris.ToString("yyyy-MM-dd"), BelgeTipi = k.Belge, IslemTipiAdi = k.IslemTuru, TLKarsiligi = k.Tutar }).ToList() });
        }
        [HttpGet("GetPortfolioChecks")]
        public IActionResult GetPortfolioChecks(string dbName) => Json(NakitAkisOrnek.SecilebilirBelgeler(Db(dbName), "Çek").Select(k => new { DocEntry = k.DocEntry, CekNumarasi = k.No, CiroEden = k.Musteri, AsilBorclu = k.AsilBorclu, Tutar = k.Tutar, ParaBirimi = k.PB, VadeTarihi = k.Vade.ToString("yyyy-MM-dd"), VadeAyiVeYili = ButceOrnek.Aylar[k.Vade.Month - 1] + " " + k.Vade.Year, PortfoyeGirisTarihi = k.PortfoyGiris.ToString("yyyy-MM-dd"), HareketDurumu = "Portföyde", BankaAdi = k.Banka }).ToList());
        [HttpPost("TopluCekleriAkisaEkle"), ValidateAntiForgeryToken]
        public IActionResult TopluCekleriAkisaEkle([FromBody] CekEkleRequest request)
        {
            int n = NakitAkisOrnek.TopluBelgeEkle(Db(request?.DbName), request?.SecilenCekDocEntryListesi ?? new List<int>(), Kullanici);
            return Json(new { success = n > 0, message = n > 0 ? $"{n} belge ayrı satır olarak akışa eklendi ve Çek Akıbeti'nde Planlandı oldu; kurumu satırdan girin." : "Seçilen belgeler eklenemedi (portföyde ve kullanılabilir olmalı)." });
        }
        [HttpGet("GetCekDetaylari")]
        public IActionResult GetCekDetaylari(string dbName, string detay, string pb)
        {
            bool senet = (detay ?? "").ToUpper(Tr).Contains("SENET");
            var l = CekAkibetOrnek.Kayitlar.Where(k => (k.DbName == Db(dbName) || k.Sirket == Db(dbName)) && k.Portfoyde && k.Belge == (senet ? "Senet" : "Çek")).OrderBy(k => k.Vade).ToList();
            return Json(new { success = true, data = l.Select(k => new CekDetayViewModel { CekNumarasi = k.No, VadeTarihi = k.Vade.ToString("dd.MM.yyyy"), CekKimdenGeldi = k.Musteri, AsilBorclu = k.AsilBorclu, BankaAdi = k.Banka, CekTutari = k.Tutar, TLKarsiligi = k.Tutar, ParaBirimi = k.PB, BelgeTipi = k.Belge + (k.IslemTuru == "Planlandı" ? " · Planlandı → " + k.PlanlananTedarikci : "") }).ToList() });
        }
        [HttpGet("GetBankaDetaylari")]
        public IActionResult GetBankaDetaylari(string dbName, string pb) => Json(new { success = true, data = NakitAkisOrnek.Bakiyeler(Db(dbName)).Where(b => b.BankaKasa == "BANKA" && (string.IsNullOrEmpty(pb) || b.PB == pb)).Select(b => new BankaDetayViewModel { BankaAdi = b.Detay, ParaBirimi = b.PB, Bakiye = b.Tutar, GuncelTLTutari = b.TutarTL, GuncellenmeTarihi = b.GuncellenmeTarihi }).ToList() });

        // ---------- yardımcı uçlar ----------
        [HttpGet("GetGuncelKur")] public IActionResult GetGuncelKur(string dbName, string pb) => Json(NakitAkisOrnek.Kurlar.TryGetValue(pb == "EURO" ? "EUR" : pb ?? "TRY", out var k) ? k : 1m);
        [HttpGet("GetCariListesi")] public IActionResult GetCariListesi(string term, string dbName) => Json(Tedarikciler(Db(dbName)).Where(t => string.IsNullOrEmpty(term) || t.ToUpper(Tr).Contains((term ?? "").ToUpper(Tr))).Take(30).ToList());
        [HttpGet("GetCariSirketleri")] public IActionResult GetCariSirketleri(string cardName) => Json(NakitAkisOrnek.Sirketler.Select(s => new Dictionary<string, object> { ["1"] = s.DbName }).ToList());
        [HttpGet("GetSatinalmacilar")] public IActionResult GetSatinalmacilar(string dbName) => Json(NakitAkisOrnek.Satinalmacilar.ToList());
        [HttpGet("GetSuppliers")] public IActionResult GetSuppliers(string dbName) => Json(Tedarikciler(Db(dbName)).Select((t, i) => new { CardCode = "320." + (i + 1).ToString("000"), CardName = t }).ToList());
        [HttpGet("GetSupplierBankInfo")] public IActionResult GetSupplierBankInfo(string dbName, string cardCode) => Json(new[] { new { Iban = "TR" + Math.Abs((cardCode ?? "").GetHashCode() % 90 + 10) + " 0006 2000 1230 0006 " + Math.Abs((cardCode ?? "x").GetHashCode() % 9000 + 1000) + " 00", BankName = "Garanti BBVA" } });
        [HttpGet("GetCompanyBankAccounts")] public IActionResult GetCompanyBankAccounts(string dbName) => Json(NakitAkisOrnek.BankaHesaplari.Select(h => { var p = h.Split(" · "); return new { BankName = p[0], Iban = p.Length > 1 ? p[1] : "", ParaBirimi = "TRY", BranchName = "Merkez", AccountNumber = p.Length > 1 ? p[1].Replace(" ", "").Substring(10) : "" }; }).ToList());
        [HttpGet("GetBelgelerForCari")]
        public IActionResult GetBelgelerForCari(string dbName, string cardName, string belgeTipi) => Json(Belgeler(Db(dbName), cardName, belgeTipi));
        [HttpGet("BelgeAra")] public IActionResult BelgeAra(string term) => Json(NakitAkisOrnek.Sirketler.SelectMany(s => Belgeler(s.DbName, term, null)).Take(30).ToList());
        [HttpGet("GetBelgeAltDetay")]
        public IActionResult GetBelgeAltDetay(string kaynakDbName, string belgeTipi, int docEntry)
        {
            var rnd = new Random(docEntry); var kalemler = new[] { "Sodyum Hidroksit %50", "Polietilen Granül", "Etiket Rulosu 100x50", "Nakliye Hizmeti", "Bakım Hizmeti", "Solvent Bazlı Reçine" };
            var rows = Enumerable.Range(0, 2 + rnd.Next(3)).Select(i => { decimal miktar = rnd.Next(1, 40) * 25, fiyat = rnd.Next(80, 900), net = miktar * fiyat; return new Dictionary<string, object> { ["DocNum"] = docEntry, ["FaturaNo"] = $"FT-{DateTime.Today.Year}-{docEntry % 90000 + 10000}", ["ParaBirimi"] = "TRY", ["ItemCode"] = $"HM-{1000 + rnd.Next(900)}", ["KalemAdi"] = kalemler[rnd.Next(kalemler.Length)], ["Miktar"] = miktar, ["KdvsizTutar"] = net, ["KdvTutari"] = Math.Round(net * 0.2m, 2), ["KdvliTutar"] = Math.Round(net * 1.2m, 2), ["UUID"] = Guid.NewGuid().ToString() }; }).ToList();
            return Json(new { success = true, data = rows });
        }
        [HttpGet("GetOdenenler")]
        public IActionResult GetOdenenler(DateTime start, DateTime end, string search)
        {
            var l = NakitAkisOrnek.Sirketler.SelectMany(s => NakitAkisOrnek.Odemeler(s.DbName).Where(m => m.U_Durum == "ÖDENDİ" && m.U_OdenmeTarihi != null && m.U_OdenmeTarihi >= start && m.U_OdenmeTarihi <= end.AddDays(1)).Select(m => new OdenenBelgeDto { Firma = NakitAkisOrnek.GorunenAd(s.DbName), OdenmeTarihi = m.U_OdenmeTarihi, Kurum = m.U_Kurum, UrunHizmet = m.U_UrunHizmet, Tutar = m.U_Tutar, TutarTL = m.U_TutarTL, Kur = m.U_Kur, ParaBirimi = m.U_ParaBirimi, OdenenTutar = m.U_OdenenTutar, OdenenTutarPB = m.U_OdenenTutarPB, OdemeYontemi = m.U_OdemeYontemi, FaturaSiparisNo = m.U_FaturaSiparisNo, DekontLink = m.DekontListesi.FirstOrDefault()?.DosyaYolu ?? "" }));
            if (!string.IsNullOrWhiteSpace(search)) { string q = search.ToUpper(Tr); l = l.Where(x => (x.Kurum ?? "").ToUpper(Tr).Contains(q) || (x.UrunHizmet ?? "").ToUpper(Tr).Contains(q) || (x.FaturaSiparisNo ?? "").ToUpper(Tr).Contains(q)); }
            return Json(l.OrderByDescending(x => x.OdenmeTarihi).ToList());
        }
        [HttpGet("GetRowLogs")] public IActionResult GetRowLogs(string dbName, int docEntry) => Json(new { success = true, data = NakitAkisOrnek.Gunluk(Db(dbName), docEntry).Select(LogJson).ToList() });
        [HttpGet("GetSistemLoglari")]
        public IActionResult GetSistemLoglari(string dbName, DateTime? startDate, DateTime? endDate, bool sadeceSilinenler = false)
        {
            var l = NakitAkisOrnek.Gunluk(string.IsNullOrEmpty(dbName) || dbName == "TUMU" ? null : dbName).Where(g => (startDate == null || g.IslemTarihi >= startDate) && (endDate == null || g.IslemTarihi < endDate.Value.AddDays(1)) && (!sadeceSilinenler || g.IslemTipi == "Silme"));
            return Json(new { success = true, data = l.Take(300).Select(LogJson).ToList() });
        }
        static object LogJson(NakitAkisLog g) => new { LogId = g.LogId, IslemTarihi = g.IslemTarihi.ToString("yyyy-MM-ddTHH:mm:ss"), Kullanici = g.Kullanici, IslemTipi = g.IslemTipi, Aciklama = g.Aciklama, DocEntry = g.DocEntry, DbName = g.DbName };

        // ---------- mükerrer / muafiyet / dekont ----------
        [HttpGet("MukerrerDetay")]
        public IActionResult MukerrerDetay(string dbName, string docEntries)
        {
            var idler = (docEntries ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x.Trim(), out var n) ? n : 0).Where(n => n > 0).ToList();
            var rows = idler.Select(id => NakitAkisOrnek.Bul(Db(dbName), id)).Where(m => m != null).Select(m => new { docEntry = m.DocEntry, faturaNo = m.U_FaturaSiparisNo, kurum = m.U_Kurum, urun = m.U_UrunHizmet, planlanan = m.U_PlanlananTarih, tutar = m.U_Tutar, pb = m.U_ParaBirimi, tutarTL = m.U_TutarTL, durum = m.U_Durum, odenmeTarihi = m.U_OdenmeTarihi, odenenTutar = m.U_OdenenTutar, kaynak = m.U_KaynakFirma, satinalmaci = m.U_Satinalmaci, notlar = m.U_Notlar, kayitTipi = m.U_KayitTipi, belgeTipi = m.U_BagliBelgeTipi, odemeYontemi = m.U_OdemeYontemi, mutabakat = m.U_MutabakatDurumu }).ToList();
            return Json(new { success = true, data = rows, kararlar = new object[0] });
        }
        public class MukerrerKararIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string Karar { get; set; } public int? AsilDocEntry { get; set; } public string Digerler { get; set; } public string Not { get; set; } }
        [HttpPost("MukerrerKarar")] public IActionResult MukerrerKarar([FromBody] MukerrerKararIstek req) => Json(new { success = true, message = "Mükerrer kararı kaydedildi (bellek içi örnek)." });
        public class MuafiyetIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string Sebep { get; set; } }
        [HttpPost("MuafiyetKaydet")]
        public IActionResult MuafiyetKaydet([FromBody] MuafiyetIstek model)
        {
            var m = model == null ? null : NakitAkisOrnek.Bul(Db(model.DbName), model.DocEntry); if (m != null) m.MuafiyetSebebi = model.Sebep ?? "";
            return Json(new { success = m != null, sebep = m?.MuafiyetSebebi ?? "" });
        }
        [HttpPost("UploadDekont")]
        public IActionResult UploadDekont(IFormFile dekontDosya, string dbName, int docEntry, string secilenMail, string dekontAciklama = null)
        {
            var m = NakitAkisOrnek.Bul(Db(dbName), docEntry); if (m == null) return Json(new { success = false, message = "Satır bulunamadı." });
            string ad = dekontDosya?.FileName ?? $"dekont_{docEntry}.pdf"; string yol = "/Dekontlar/" + ad;
            lock (NakitAkisOrnek.Kilit) m.DekontListesi.Add(new DekontKaydi { Id = (m.DekontListesi.Count > 0 ? m.DekontListesi.Max(d => d.Id) : 0) + 1, SirketDb = Db(dbName), DocEntry = docEntry, DosyaAdi = ad, DosyaYolu = yol, Aciklama = dekontAciklama ?? "", Yukleyen = Kullanici, Tarih = DateTime.Now });
            return Json(new { success = true, message = "Dekont kaydedildi (ön yüz örneği: dosya saklanmaz)." + (string.IsNullOrEmpty(secilenMail) ? "" : $" E-posta: {secilenMail}"), filePath = yol });
        }
        [HttpPost("UploadTopluDekont")]
        public IActionResult UploadTopluDekont(IFormFile dekontDosya, string dbName, string docEntries, string secilenMail)
        {
            int n = 0; foreach (var id in (docEntries ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)) if (int.TryParse(id.Trim(), out var d) && UploadDekont(dekontDosya, dbName, d, secilenMail) is JsonResult) n++;
            return Json(new { success = n > 0, message = $"{n} kayda dekont eklendi (ön yüz örneği).", filePath = "/Dekontlar/" + (dekontDosya?.FileName ?? "toplu_dekont.pdf") });
        }
        [HttpPost("DeleteDekont")]
        public IActionResult DeleteDekont(string dbName, int docEntry, int dekontId = 0)
        {
            var m = NakitAkisOrnek.Bul(Db(dbName), docEntry); if (m == null) return Json(new { success = false, message = "Satır bulunamadı." });
            lock (NakitAkisOrnek.Kilit) { if (dekontId > 0) m.DekontListesi.RemoveAll(d => d.Id == dekontId); else m.DekontListesi.Clear(); }
            return Json(new { success = true, message = "Dekont silindi." });
        }

        static List<string> Tedarikciler(string dbName) => CekOrnek.TedarikciAdlari.Concat(CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Tedarikçi" && k.Sirket == NakitAkisOrnek.SirketAdi(dbName)).Select(k => k.KisaAd)).Distinct().OrderBy(x => x, StringComparer.Create(Tr, true)).ToList();
        static List<BelgeAramaViewModel> Belgeler(string dbName, string cardName, string belgeTipi)
        {
            var rnd = new Random((dbName + "|" + cardName).GetHashCode()); string firma = NakitAkisOrnek.GorunenAd(dbName);
            return Enumerable.Range(0, 4).Select(i => { decimal t = rnd.Next(50, 2500) * 1000m; var tarih = DateTime.Today.AddDays(-rnd.Next(5, 90)); string tip = string.IsNullOrEmpty(belgeTipi) ? (i % 2 == 0 ? "Fatura" : "Sipariş") : belgeTipi; return new BelgeAramaViewModel { Firma = firma, DbName = dbName, DocEntry = 40000 + rnd.Next(1, 9999), BelgeTipi = tip, CardName = cardName ?? "", DocTotalOrijinal = t, ItemAcctName = "Hammadde", DocTotalTL = t, OdenenTutar = i == 0 ? t / 2 : 0, ParaBirimi = "TRY", DocDate = tarih, U_BE1_Aktar = "X", DocNum = (40000 + i).ToString(), FaturaNo = (tip == "Fatura" ? "FT-" : "SP-") + tarih.Year + "-" + rnd.Next(10000, 99999), Satinalmaci = NakitAkisOrnek.Satinalmacilar[rnd.Next(NakitAkisOrnek.Satinalmacilar.Length)] }; }).ToList();
        }
    }
}
