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
            ViewBag.VarsayilanSirket = NakitAkisOrnek.Sirketler.Any(s => s.DbName == sirket) ? sirket : sirket == "" || sirket == "tumu" || sirket == "konsolide" ? "" : "URASKIMYA";
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
            var avanslar = NakitAvansOrnek.Talepler.Where(a => a.Onaylandi && a.OdemeBekleyen > 0).ToList();
            return Json(new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), bas = b1.ToString("yyyy-MM-dd"), bit = b2.ToString("yyyy-MM-dd"),
                sirketler = NakitAkisOrnek.Sirketler.Select(s => new { ad = s.Ad, dbName = s.DbName, dbKey = s.DbKey }), grupSirketleri = CekAkibetOrnek.GrupSirketleri, yontemler = NakitAkisOrnek.OdemeYontemleri, durumlar = NakitAkisOrnek.Durumlar, satinalmacilar = NakitAkisOrnek.Satinalmacilar,
                bankaHesaplari = NakitAkisOrnek.BankaHesaplari, kasalar = NakitAkisOrnek.Kasalar, kartlar = NakitAkisOrnek.Kartlar, taksitler = NakitAkisOrnek.TaksitSecenekleri, kurlar = NakitAkisOrnek.Kurlar, tedarikciler = Tedarikciler(Db(sirket)),
                odemeler, bakiyeler, gunluk, grupIci = NakitAkisOrnek.GrupIciOzet(sirket),
                grupBakiyeleri = CekAkibetOrnek.GrupBakiyeleri.Where(x => x.Value > 0).Select(x => new { borclu = x.Key.Split('|')[0], alacakli = x.Key.Split('|')[1], tutar = x.Value }).OrderBy(x => x.borclu).ThenBy(x => x.alacakli).ToList(),
                talimatlar = TalimatOrnek.Listele(sirket).Select(TalimatJson).ToList(),
                avans = new { bekleyen = avanslar.Count, bekleyenTutar = avanslar.Sum(a => NakitAvansOrnek.TlKarsiligi(a.OdemeBekleyen, a.ParaBirimi)), link = "/IkTalep/AvansBekleyenOdemeler" },
                onaycilar = new { nakit = NakitAkisOrnek.NakitOnaycilari, cekCikis = NakitAkisOrnek.CekCikisOnaycilari }, kullanici = Kullanici, belgeTipleri = NakitAkisOrnek.BelgeTipleri,
                makbuzlar = MakbuzOrnek.Listele(string.IsNullOrEmpty(sirket) ? null : sirket).Take(100).Select(MakbuzOrnek.Json).ToList()
            });
        }

        /// <summary>
        /// Aylık Ödeme · Çek Vade sekmesi: yılın ödeme satırları (gerçekleşen = ödenme tarihi, planlanan = planlanan tarih) ve çek / senet satırlarındaki belgeler
        /// (çıkış ayı × vade ayı dağılımı için: çıkış tarihi = satırın ödenme ya da planlanan tarihi, vade = belgenin vadesi). Toplamlar istemcide (kasa-nakit.js › cizAylik).
        /// </summary>
        [HttpGet("AylikOzet")]
        public IActionResult AylikOzet(string sirket, int? yil)
        {
            int y = yil ?? DateTime.Today.Year;
            var dbler = string.IsNullOrEmpty(sirket) ? NakitAkisOrnek.Sirketler.Select(s => s.DbName).ToList() : new List<string> { sirket };
            var hepsi = dbler.SelectMany(db => NakitAkisOrnek.Odemeler(db)).ToList();
            DateTime? Tarih(NakitAkisViewModel m) => m.U_Durum == "ÖDENDİ" ? (m.U_OdenmeTarihi ?? m.U_PlanlananTarih) : m.U_PlanlananTarih;
            var yillar = hepsi.SelectMany(m => new[] { Tarih(m)?.Year }.Concat(m.Belgeler.Select(b => (int?)b.Vade.Year))).Where(x => x != null).Select(x => x.Value).Distinct().OrderByDescending(x => x).ToList(); if (!yillar.Contains(y)) yillar.Add(y);
            var rows = hepsi.Where(m => Tarih(m)?.Year == y || m.Belgeler.Any(b => b.Vade.Year == y)).ToList();
            var odemeler = rows.Select(m => new { docEntry = m.DocEntry, dbName = m.U_KaynakFirma, sirket = NakitAkisOrnek.SirketAdi(m.U_KaynakFirma), kurum = m.U_Kurum, faturaNo = m.U_FaturaSiparisNo, belgeTipi = m.U_BagliBelgeTipi, yontem = m.U_OdemeYontemi, tutar = m.U_Tutar ?? 0, pb = m.U_ParaBirimi, tutarTL = m.U_TutarTL ?? 0, durum = m.U_Durum, gerceklesen = m.U_Durum == "ÖDENDİ", tarih = Tarih(m)?.ToString("yyyy-MM-dd"), odenme = m.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), planlanan = m.U_PlanlananTarih?.ToString("yyyy-MM-dd"), faturaSahibi = m.U_FaturaSahibi, odeyenSirket = m.U_OdeyenSirket, cekNolar = m.U_CekNolar, cikisTalebi = m.U_CikisTalebi, bankaHesabi = m.U_BankaHesabi, kasa = m.U_Kasa, makbuzNo = m.U_MakbuzNo, talimatNo = m.U_TalimatNo, talepEden = m.U_TalepEden, mutabakat = m.U_MutabakatDurumu }).ToList();
            var cekler = rows.Where(m => m.U_OdemeYontemi == "Çek" || m.U_OdemeYontemi == "Senet").SelectMany(m => m.Belgeler.Select(b => new { docEntry = m.DocEntry, dbName = m.U_KaynakFirma, sirket = NakitAkisOrnek.SirketAdi(m.U_KaynakFirma), kurum = m.U_Kurum, no = b.No, belge = b.Belge, tip = CekAkibetOrnek.Bul(b.CekDocEntry)?.IslemTipi ?? "X", banka = b.Banka, kesideci = b.AsilBorclu, kimden = b.KimdenGeldi, vade = b.Vade.ToString("yyyy-MM-dd"), tutar = b.Tutar, akibet = b.Akibet, kontrol = b.Kontrol, ibrazYeri = b.IbrazYeri, cikisTarihi = Tarih(m)?.ToString("yyyy-MM-dd"), cikti = m.U_Durum == "ÖDENDİ" || (b.Akibet != "Planlandı" && b.Akibet != "Portföyde"), durum = m.U_Durum, cikisTalebi = m.U_CikisTalebi })).ToList();
            return Json(new { success = true, yil = y, yillar, odemeler, cekler });
        }

        // ---------- ÖDEME TALİMATI ----------
        public class TalimatIstek { public string DbName { get; set; } public List<int> DocEntryler { get; set; } public string BankaHesabi { get; set; } public string Kasa { get; set; } public string No { get; set; } public string Durum { get; set; } public string Not { get; set; } }
        /// <summary>Seçilen satırlardan talimat: ödeyen şirket + yöntem bazında; çek satırlarında seçili belgeler ve dolaşım planı talimata girer.</summary>
        [HttpPost("TalimatOlustur")]
        public IActionResult TalimatOlustur([FromBody] TalimatIstek istek)
        {
            if (istek == null || istek.DocEntryler == null || istek.DocEntryler.Count == 0) return Json(new { success = false, message = "Talimat için satır seçin." });
            var r = TalimatOrnek.Olustur(Db(istek.DbName), istek.DocEntryler, Kullanici, istek.BankaHesabi, istek.Kasa);
            return Json(new { success = r.talimatlar.Count > 0, message = r.talimatlar.Count > 0 ? $"{r.talimatlar.Count} talimat hazırlandı: {string.Join(", ", r.talimatlar.Select(t => t.No + " (" + t.Baslik + ", " + t.Satirlar.Count + " satır)"))}" + (r.atlananlar.Count > 0 ? " · atlanan: " + string.Join("; ", r.atlananlar) : "") : "Talimat oluşturulamadı: " + string.Join("; ", r.atlananlar), talimatlar = r.talimatlar.Select(TalimatJson).ToList(), atlananlar = r.atlananlar });
        }
        [HttpPost("TalimatDurum")]
        public IActionResult TalimatDurum([FromBody] TalimatIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = TalimatOrnek.DurumDegistir(istek.No, istek.Durum, istek.Not, Kullanici); var t = TalimatOrnek.Bul(istek.No);
            return Json(new { success = r.ok, message = r.mesaj, talimat = t == null ? null : TalimatJson(t) });
        }
        [HttpGet("TalimatVeri")] public IActionResult TalimatVeri(string no) { var t = TalimatOrnek.Bul(no); return Json(t == null ? new { success = false, message = "Talimat bulunamadı." } : (object)new { success = true, talimat = TalimatJson(t) }); }
        /// <summary>Yazdırılabilir talimat (ana projedeki PDF belgelerinin HTML karşılığı; tarayıcıdan yazdırılır / PDF kaydedilir).</summary>
        [HttpGet("Talimat")]
        public IActionResult Talimat(string no) { var t = TalimatOrnek.Bul(no); if (t == null) return Content("Talimat bulunamadı: " + no, "text/plain; charset=utf-8"); ViewData["Title"] = t.Baslik + " · " + t.No; return View("Talimat", t); }
        static object TalimatJson(OdemeTalimati t) => new
        {
            no = t.No, tarih = t.Tarih.ToString("yyyy-MM-dd HH:mm"), dbName = t.DbName, sirket = t.Sirket, yontem = t.Yontem, baslik = t.Baslik, bankaHesabi = t.BankaHesabi, kasa = t.Kasa, durum = t.Durum, sonrakiDurum = t.SonrakiDurum, kapandi = t.Kapandi,
            hazirlayan = t.Hazirlayan, onaylayan = t.Onaylayan, onayTarihi = t.OnayTarihi?.ToString("yyyy-MM-dd HH:mm"), iletimTarihi = t.IletimTarihi?.ToString("yyyy-MM-dd HH:mm"), gerceklesmeTarihi = t.GerceklesmeTarihi?.ToString("yyyy-MM-dd HH:mm"), iptalNedeni = t.IptalNedeni,
            satirSayisi = t.Satirlar.Count, toplamTL = t.ToplamTL, toplam = t.Toplam, pb = t.PB, link = "/Rapor79/Talimat?no=" + Uri.EscapeDataString(t.No),
            satirlar = t.Satirlar.Select(s => { var m = NakitAkisOrnek.Bul(s.DbName, s.DocEntry); return new { s.DocEntry, dbName = s.DbName, faturaSahibi = s.FaturaSahibi, odeyenSirket = s.OdeyenSirket, kurum = s.Kurum, faturaNo = s.FaturaNo, aciklama = s.Aciklama, tutar = s.Tutar, pb = s.PB, tutarTL = s.TutarTL, aliciIban = s.AliciIban, grupIciNo = s.GrupIciNo, avansNo = s.AvansNo, makbuzNo = m?.U_MakbuzNo ?? "", yevmiyeNo = m?.U_YevmiyeNo ?? "", durum = m?.U_Durum ?? "", belgeler = s.Belgeler.Select(b => new { b.CekDocEntry, b.No, b.Belge, b.Banka, b.CiroEden, b.AsilBorclu, b.Tutar, b.PB, vade = b.Vade.ToString("yyyy-MM-dd"), b.Sahip, b.Dolasim, b.DolasimAdim, b.DolasimToplam }) }; }),
            gunluk = t.Gunluk.Select(g => new { tarih = g.Tarih.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, g.Islem, g.Aciklama })
        };

        // ---------- ÇEK DOLAŞIM SENARYOSU (grup içi zincir + uygunluk simülasyonu) ----------
        public class DolasimIstek { public int CekDocEntry { get; set; } public List<string> Zincir { get; set; } public string SonAdim { get; set; } public string Tedarikci { get; set; } public string FaturaSahibi { get; set; } public int? PlanDocEntry { get; set; } public int Sira { get; set; } public string Hedef { get; set; } }
        [HttpPost("DolasimSimule")]
        public IActionResult DolasimSimule([FromBody] DolasimIstek istek) { if (istek == null) return Json(new { success = false }); var s = CekAkibetOrnek.DolasimSimule(istek.CekDocEntry, istek.Zincir, istek.SonAdim ?? "Tedarikçiye Ciro", istek.Tedarikci, istek.FaturaSahibi); return Json(new { success = true, sonuc = s }); }
        [HttpGet("DolasimOner")] public IActionResult DolasimOner(int cekDocEntry, string hedef) => Json(new { success = true, zincir = CekAkibetOrnek.DolasimOner(cekDocEntry, hedef) });
        [HttpPost("DolasimKaydet")]
        public IActionResult DolasimKaydet([FromBody] DolasimIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = CekAkibetOrnek.DolasimKaydet(istek.CekDocEntry, istek.Zincir, istek.SonAdim ?? "Tedarikçiye Ciro", istek.Tedarikci, istek.PlanDocEntry, Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, belge = r.kayit == null ? null : BelgeJson(r.kayit), row = SatirByCek(r.kayit) });
        }
        /// <summary>Sıradaki dolaşım adımını gerçekleştirir (ciro / ibraz); son adımda satır ÖDENDİ olur.</summary>
        [HttpPost("DolasimAdim")]
        public IActionResult DolasimAdim([FromBody] DolasimIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = CekAkibetOrnek.DolasimAdimGerceklestir(istek.CekDocEntry, istek.Sira, Kullanici);
            if (r.ok && r.kayit != null && r.kayit.SonrakiAdim == null && r.kayit.DolasimDocEntry != null) { var m = NakitAkisOrnek.Bul(r.kayit.DolasimDocEntry.Value); if (m != null) NakitAkisOrnek.OdendiIsaretle(m.U_KaynakFirma, m.DocEntry, "dolaşım tamamlandı", Kullanici); }
            return Json(new { success = r.ok, message = r.mesaj, belge = r.kayit == null ? null : BelgeJson(r.kayit), row = SatirByCek(r.kayit) });
        }
        [HttpPost("DolasimIptal")] public IActionResult DolasimIptal([FromBody] DolasimIstek istek) { var r = CekAkibetOrnek.DolasimIptal(istek?.CekDocEntry ?? 0, Kullanici); var k = CekAkibetOrnek.Bul(istek?.CekDocEntry ?? 0); return Json(new { success = r.ok, message = r.mesaj, belge = k == null ? null : BelgeJson(k), row = SatirByCek(k) }); }
        static object SatirByCek(CekKaydi k) { if (k == null) return null; var doc = k.DolasimDocEntry ?? k.PlanDocEntry; if (doc == null) return null; var m = NakitAkisOrnek.Bul(doc.Value); if (m == null) return null; NakitAkisOrnek.BelgeleriDoldur(m); return SatirJson(m); }
        static object BelgeJson(CekKaydi k) => new
        {
            cekDocEntry = k.DocEntry, no = k.No, belge = k.Belge, tip = k.IslemTipi, banka = k.Banka, asilBorclu = k.AsilBorclu, kimdenGeldi = k.Musteri, vade = k.Vade.ToString("yyyy-MM-dd"), tutar = k.Tutar, pb = k.PB, akibet = k.IslemTuru, ibrazYeri = k.IbrazYeri, kontrol = k.Kontrol, planlananTedarikci = k.PlanlananTedarikci,
            sahip = k.Sirket, kaynakSirket = k.KaynakSirket, dolasim = k.DolasimOzeti, gerceklesen = k.GerceklesenAdim, adimToplam = k.Dolasim.Count, adimlar = k.Dolasim.Select(a => new { a.Sira, a.Eden, a.Alan, a.Tur, planTarih = a.PlanTarih.ToString("yyyy-MM-dd"), a.Durum, gercekTarih = a.GercekTarih?.ToString("yyyy-MM-dd"), a.BelgeNo, a.KapananBakiye, a.YeniBorc, a.Not })
        };
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
            var mk = MukerrerCevabi(Db(dbName), model); if (mk != null) return mk;
            bool ok = NakitAkisOrnek.Guncelle(Db(dbName), model, Kullanici); var m = NakitAkisOrnek.Bul(Db(dbName), model.DocEntry); if (m != null) NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new { success = ok, message = ok ? "Kaydedildi." : "Satır bulunamadı.", row = m == null ? null : SatirJson(m) });
        }
        [HttpPost("SatirEkle")]
        public IActionResult SatirEkle([FromBody] NakitAkisViewModel model, [FromQuery] string dbName)
        {
            if (model == null) return Json(new { success = false, message = "Veri yok." });
            var mk = MukerrerCevabi(Db(dbName), model); if (mk != null) return mk;
            var m = NakitAkisOrnek.Ekle(Db(dbName), model, Kullanici); NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new { success = true, message = m.U_OdemeYontemi == "Nakit" ? $"Nakit ödeme talebi açıldı (#{m.DocEntry}); onay bekliyor: {string.Join(" / ", NakitAkisOrnek.NakitOnaycilari)}." : $"Ödeme kaydı eklendi (#{m.DocEntry}).", row = SatirJson(m) });
        }
        /// <summary>Kayıt öncesi mükerrer kontrolü: aynı belge + tutar ya da aynı cari + tutar + tarih varsa kayıt durdurulur; kullanıcı onaylarsa (MukerrerKontrolEdildi = 1) kaydedilir.</summary>
        IActionResult MukerrerCevabi(string dbName, NakitAkisViewModel model)
        {
            if (model.MukerrerKontrolEdildi == 1) return null;
            var adaylar = NakitAkisOrnek.MukerrerAdaylar(dbName, model); if (adaylar.Count == 0) return null;
            return Json(new { success = false, mukerrer = true, message = "MÜKERRER UYARISI: " + string.Join("; ", adaylar.Select(a => $"#{a.DocEntry} {NakitAkisOrnek.SirketAdi(a.U_KaynakFirma)} · {a.U_Kurum} · {a.U_FaturaSiparisNo} · {(a.U_Tutar ?? 0).ToString("N2", Tr)} {a.U_ParaBirimi} · {a.U_PlanlananTarih:dd.MM.yyyy} · {a.U_Durum}")), docEntryler = adaylar.Select(a => a.DocEntry).ToList() });
        }

        // ---------- NAKİT ONAYI · ÇEK ÇIKIŞ ONAYI (GMY) · MAKBUZ · TAKSİT · BELGE ----------
        public class OnayIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string Karar { get; set; } public string Onaylayan { get; set; } public string Not { get; set; } }
        /// <summary>Nakit ödeme talebi onayı / reddi (onaycılar NakitAkisOrnek.NakitOnaycilari).</summary>
        [HttpPost("NakitOnay")]
        public IActionResult NakitOnay([FromBody] OnayIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = istek.Karar == "Reddet" ? NakitAkisOrnek.NakitReddet(Db(istek.DbName), istek.DocEntry, istek.Onaylayan, istek.Not, Kullanici) : NakitAkisOrnek.NakitOnayla(Db(istek.DbName), istek.DocEntry, istek.Onaylayan, istek.Not, Kullanici);
            var m = NakitAkisOrnek.Bul(Db(istek.DbName), istek.DocEntry); if (m != null) NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new { success = r.ok, message = r.mesaj, row = m == null ? null : SatirJson(m) });
        }
        /// <summary>Plansız çek çıkış talebi: GMY onaylarsa çıkış işlenir (satır ÖDENDİ), reddederse belge portföyde kalır ve satır kaldırılır.</summary>
        [HttpPost("CekCikisOnay")]
        public IActionResult CekCikisOnay([FromBody] OnayIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = istek.Karar == "Reddet" ? NakitAkisOrnek.CekCikisReddet(Db(istek.DbName), istek.DocEntry, istek.Onaylayan, istek.Not, Kullanici) : NakitAkisOrnek.CekCikisOnayla(Db(istek.DbName), istek.DocEntry, istek.Onaylayan, istek.Not, Kullanici);
            var m = NakitAkisOrnek.Bul(Db(istek.DbName), istek.DocEntry); if (m != null) NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new { success = r.ok, message = r.mesaj, row = m == null ? null : SatirJson(m), kaldirildi = r.ok && m == null });
        }
        public class MakbuzIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string TeslimAlan { get; set; } public string Kasa { get; set; } }
        /// <summary>Kasa tediye makbuzu: onaylı nakit satırından kesilir; satır ÖDENDİ, muhasebe fişi ve avans olayı otomatik.</summary>
        [HttpPost("MakbuzOlustur")]
        public IActionResult MakbuzOlustur([FromBody] MakbuzIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = MakbuzOrnek.Olustur(Db(istek.DbName), istek.DocEntry, istek.TeslimAlan, istek.Kasa, Kullanici);
            var m = NakitAkisOrnek.Bul(Db(istek.DbName), istek.DocEntry); if (m != null) NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new { success = r.ok, message = r.mesaj, makbuz = MakbuzOrnek.Json(r.makbuz), row = m == null ? null : SatirJson(m) });
        }
        [HttpGet("MakbuzVeri")] public IActionResult MakbuzVeri(string no) { var k = MakbuzOrnek.Bul(no); return Json(k == null ? new { success = false, message = "Makbuz bulunamadı." } : (object)new { success = true, makbuz = MakbuzOrnek.Json(k), yevmiye = MuhasebeOrnek.Json(MuhasebeOrnek.Bul(k.YevmiyeNo)) }); }
        /// <summary>Yazdırılabilir kasa tediye makbuzu (muhasebe fişi satırlarıyla).</summary>
        [HttpGet("Makbuz")]
        public IActionResult Makbuz(string no) { var k = MakbuzOrnek.Bul(no); if (k == null) return Content("Makbuz bulunamadı: " + no, "text/plain; charset=utf-8"); ViewData["Title"] = "Kasa Tediye Makbuzu · " + k.No; ViewBag.Yevmiye = MuhasebeOrnek.Bul(k.YevmiyeNo); return View("Makbuz", k); }
        [HttpGet("Yevmiye")] public IActionResult Yevmiye(string no) => Json(new { success = true, yevmiye = MuhasebeOrnek.Json(MuhasebeOrnek.Bul(no)) });
        public class TaksitParca { public decimal Oran { get; set; } public string Planlanan { get; set; } }
        public class TaksitIstek { public string DbName { get; set; } public int DocEntry { get; set; } public List<TaksitParca> Parcalar { get; set; } }
        /// <summary>Kısmi ödeme / taksit: satır oranlara bölünür (ör. %70 + %30), kardeş satırlar aynı belge no ile izlenir.</summary>
        [HttpPost("Taksitlendir")]
        public IActionResult Taksitlendir([FromBody] TaksitIstek istek)
        {
            if (istek == null || istek.Parcalar == null) return Json(new { success = false, message = "Veri yok." });
            var parcalar = istek.Parcalar.Select(p => (p.Oran, DateTime.TryParse(p.Planlanan, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : DateTime.Today)).ToList();
            var r = NakitAkisOrnek.Taksitlendir(Db(istek.DbName), istek.DocEntry, parcalar, Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, rows = r.satirlar?.Select(m => { NakitAkisOrnek.BelgeleriDoldur(m); return SatirJson(m); }).ToList() });
        }
        /// <summary>Belge no tıklanınca: fatura / sipariş başlığı, kalemler, ödeme planı (kısmi ödemeler, oranlar), mükerrer uyarıları; Cari Bakiye satırında açık kalemler.</summary>
        [HttpGet("BelgeDetay")] public IActionResult BelgeDetay(string dbName, int docEntry) { var d = NakitAkisOrnek.BelgeDetay(Db(dbName), docEntry); return Json(d == null ? new { success = false, message = "Satır bulunamadı." } : (object)new { success = true, belge = d }); }
        /// <summary>Çekmecede "Belge Seç": carinin açık faturaları / siparişleri ve cari bakiyesi (satır fatura ya da siparişten değil cari bakiyeden de açılabilir).</summary>
        [HttpGet("CariBelgeler")] public IActionResult CariBelgeler(string dbName, string kurum, string tip) => Json(new { success = true, veri = NakitAkisOrnek.CariBelgeler(Db(dbName), kurum ?? "", tip ?? "") });
        /// <summary>Yeni Ödeme › Ödenmemiş Faturalar / Cari Bakiyeleri: şirketin carileri ve açık faturaları (Cari Mutabakat ekstresinden), cari ekstre bağlantılarıyla.</summary>
        [HttpGet("CariAcikFaturalar")] public IActionResult CariAcikFaturalar(string dbName) => Json(new { success = true, veri = NakitAkisOrnek.CariAcikFaturalar(Db(dbName)) });
        public class TalepEkleIstek { public string No { get; set; } }
        /// <summary>Departman ödeme talebini doğrudan nakit akış satırı yapar (Data/OdemeTalepOrnek.cs).</summary>
        [HttpPost("OdemeTalebiEkle")]
        public IActionResult OdemeTalebiEkle([FromBody] TalepEkleIstek istek)
        {
            var r = OdemeTalepOrnek.NakitAkisaAl(istek?.No, Kullanici); if (r.satir != null) NakitAkisOrnek.BelgeleriDoldur(r.satir);
            return Json(new { success = r.ok, message = r.mesaj, row = r.satir == null ? null : SatirJson(r.satir), talep = OdemeTalepOrnek.Json(OdemeTalepOrnek.Bul(istek?.No)) });
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
        /// <summary>Portföy belgeleri: sirket = portföy şirketi (varsayılan satırın şirketi; başka grup şirketinin portföyünden de seçilebilir → dolaşım planı); hepsi = planlı / kullanılamaz olanlar da gelir (seçilemez işaretli).</summary>
        [HttpGet("SecilebilirBelgeler")]
        public IActionResult SecilebilirBelgeler(string dbName, string belge, string sirket, bool hepsi = false)
        {
            string portfoy = string.IsNullOrEmpty(sirket) ? Db(dbName) : sirket;
            var l = CekAkibetOrnek.Secilebilirler(portfoy, string.IsNullOrEmpty(belge) ? "Çek" : belge, hepsi);
            return Json(new { success = true, sirket = CekAkibetOrnek.SirketAdi(portfoy), data = l.Select(k => new { docEntry = k.DocEntry, no = k.No, belge = k.Belge, tip = k.IslemTipi, banka = k.Banka, sube = k.Sube, asilBorclu = k.AsilBorclu, kimdenGeldi = k.Musteri, musteriKod = k.MusteriKod, vade = k.Vade.ToString("yyyy-MM-dd"), vadeDurumu = k.VadeDurumu, tutar = k.Tutar, pb = k.PB, portfoyGiris = k.PortfoyGiris.ToString("yyyy-MM-dd"), kullanim = k.KullanimDurumu, secilebilir = k.Secilebilir, akibet = k.IslemTuru, planlananTedarikci = k.PlanlananTedarikci, planDocEntry = k.PlanDocEntry, sahip = k.Sirket }) });
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
            faturaSahibi = string.IsNullOrEmpty(m.U_FaturaSahibi) ? NakitAkisOrnek.SirketAdi(m.U_KaynakFirma) : m.U_FaturaSahibi, odeyenSirket = string.IsNullOrEmpty(m.U_OdeyenSirket) ? NakitAkisOrnek.SirketAdi(m.U_KaynakFirma) : m.U_OdeyenSirket, avansNo = m.U_AvansNo, talimatNo = m.U_TalimatNo, avans = NakitAkisOrnek.AvansBilgi(m),
            sonOdeme = m.U_SonOdemeTarihi?.ToString("yyyy-MM-dd"), faturaTutari = m.U_FaturaTutari, ustDocEntry = m.U_UstDocEntry, plan = NakitAkisOrnek.BelgeOzeti(m), talepEden = m.U_TalepEden, avansSahsi = NakitAkisOrnek.AvansSahsiMi(m),
            onay = string.IsNullOrEmpty(m.U_OnayDurumu) ? null : new { durum = m.U_OnayDurumu, onaylayan = m.U_Onaylayan, tarih = m.U_OnayTarihi?.ToString("yyyy-MM-dd HH:mm"), not = m.U_OnayNotu, tip = string.IsNullOrEmpty(m.U_CikisTalebi) ? "nakit" : "cekCikis" },
            cikis = string.IsNullOrEmpty(m.U_CikisTalebi) ? null : new { islem = m.U_CikisTalebi, ibrazYeri = m.U_CikisIbrazYeri, belgeNo = m.U_CikisBelgeNo, talepEden = m.U_CikisTalepEden },
            yevmiye = string.IsNullOrEmpty(m.U_YevmiyeNo) ? null : MuhasebeOrnek.Json(MuhasebeOrnek.Bul(m.U_YevmiyeNo)), yevmiyeNo = m.U_YevmiyeNo,
            makbuz = string.IsNullOrEmpty(m.U_MakbuzNo) ? null : new { no = m.U_MakbuzNo, link = MakbuzOrnek.Bul(m.U_MakbuzNo) != null ? "/Rapor79/Makbuz?no=" + Uri.EscapeDataString(m.U_MakbuzNo) : (string)null },
            talimat = string.IsNullOrEmpty(m.U_TalimatNo) ? null : (TalimatOrnek.Bul(m.U_TalimatNo) is OdemeTalimati tt ? new { no = tt.No, durum = tt.Durum, baslik = tt.Baslik, link = "/Rapor79/Talimat?no=" + Uri.EscapeDataString(tt.No) } : null),
            kurum = m.U_Kurum, urun = m.U_UrunHizmet, pb = m.U_ParaBirimi, kur = m.U_Kur ?? 1, tutar = m.U_Tutar ?? 0, tutarTL = m.U_TutarTL ?? 0, yontem = m.U_OdemeYontemi, odemeYontemi = m.U_OdemeYontemi, odenme = m.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), odenen = m.U_OdenenTutar ?? 0, odenenPB = m.U_OdenenTutarPB, durum = m.U_Durum, notlar = m.U_Notlar,
            belgeTipi = m.U_BagliBelgeTipi, bagliDocEntry = m.U_BagliDocEntry, faturaNo = m.U_FaturaSiparisNo, islemTipi = m.U_IslemTipi, kayitTipi = m.U_KayitTipi, mutabakat = m.U_MutabakatDurumu, mutabakatTarihi = m.SonMutabakatTarihi?.ToString("yyyy-MM-dd"), satinalmaci = m.U_Satinalmaci, muafiyet = m.MuafiyetSebebi,
            dekontlar = m.DekontListesi.Select(d => new { id = d.Id, ad = d.DosyaAdi, yol = d.DosyaYolu, tarih = d.Tarih.ToString("yyyy-MM-dd HH:mm"), yukleyen = d.Yukleyen, aciklama = d.Aciklama }),
            cekNolar = m.U_CekNolar, cekDocEntryler = m.U_CekDocEntryler,
            belgeler = m.Belgeler.Select(b => { var k = CekAkibetOrnek.Bul(b.CekDocEntry); return k != null ? BelgeJson(k) : new { cekDocEntry = b.CekDocEntry, no = b.No, belge = b.Belge, banka = b.Banka, asilBorclu = b.AsilBorclu, kimdenGeldi = b.KimdenGeldi, vade = b.Vade.ToString("yyyy-MM-dd"), tutar = b.Tutar, pb = b.PB, akibet = b.Akibet, ibrazYeri = b.IbrazYeri, kontrol = b.Kontrol, planlananTedarikci = b.PlanlananTedarikci, sahip = "", kaynakSirket = "", dolasim = "", gerceklesen = 0, adimToplam = 0, adimlar = Enumerable.Empty<object>() }; }),
            alanlar = new { bankaHesabi = m.U_BankaHesabi, aliciIban = m.U_AliciIban, dekontNo = m.U_DekontNo, kasa = m.U_Kasa, makbuzNo = m.U_MakbuzNo, teslimAlan = NakitAkisOrnek.AvansSahsiMi(m) ? "Personel (gizli)" : !string.IsNullOrEmpty(m.U_AvansNo) ? "Avans sahibi (çekmecede)" : m.U_TeslimAlan, kart = m.U_Kart, taksit = m.U_Taksit, ekstreDonemi = m.U_EkstreDonemi, mahsupFisNo = m.U_MahsupFisNo, karsiBelge = m.U_KarsiBelge, temlikNo = m.U_TemlikNo, temlikEden = m.U_TemlikEden, yansitmaNo = m.U_YansitmaNo },
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
