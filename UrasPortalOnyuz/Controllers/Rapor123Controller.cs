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
    /// KONSOLİDE FİNANSAL TAKİP PANELİ (Rapor123) — Kasa Nakit Akışı tasarım dilinde modern ekran (Views/Rapor123/Index.cshtml + wwwroot/js/takip-paneli.js; veri /Rapor123/Veri).
    /// KASA / BANKA hareketleri Data/NakitAkisOrnek.cs ödemelerinden, ÇEK hareketleri Data/CekAkibetOrnek.cs kayıtlarından (plan ↔ ibraz kontrolü), Finekra sekmesi bankadan otomatik akan
    /// hareketler (Data/FinekraOrnek.cs): onay + cari / hesap seçimi → muhasebe fişi; her hareket Kasa Nakit Akışı ile eşlenir, nakit akışta olmayan çıkış "Nakit Akışa Gönder" ile satır olur.
    /// Eski (ana projedeki) görünüm /Rapor123/Eski. Üretilmiş sürüm Uretilen/Controllers/Rapor123Controller.cs içinde #if ile kapalı (R123_* model sınıfları orada).
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class Rapor123Controller : Controller
    {
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        static int Seed(string s) => s.Aggregate(31, (a, ch) => unchecked(a * 31 + ch));

        /// <summary>Modern ekran. ?sirket=URASKIMYA&bas=&bit=&sekme=finekra|banka|kasa|cek|eslesme|bakiye|gunluk — eski bağlantılar (hesapTipi=CEK, tab=FINEKRA, dbFilter) sekmeye çevrilir.</summary>
        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(string sirket, string bas, string bit, string sekme, string hesapTipi, string tab, string dbFilter, DateTime? startDate, DateTime? endDate)
        {
            ViewData["Title"] = "Konsolide Finansal Takip Paneli";
            ViewBag.Sirketler = CekAkibetOrnek.Sirketler.Select(s => new KeyValuePair<string, string>(s.DbName, s.Ad)).ToList();
            string db = sirket; if (string.IsNullOrEmpty(db) && !string.IsNullOrEmpty(dbFilter) && dbFilter != "TUMU") db = CekAkibetOrnek.Sirketler.FirstOrDefault(s => s.DbKey == dbFilter || s.DbName == dbFilter).DbName;
            ViewBag.VarsayilanSirket = CekAkibetOrnek.Sirketler.Any(s => s.DbName == db) ? db : "";
            ViewBag.Bas = (startDate ?? (DateTime.TryParse(bas, CultureInfo.InvariantCulture, DateTimeStyles.None, out var b1) ? b1 : DateTime.Today.AddDays(-30))).ToString("yyyy-MM-dd");
            ViewBag.Bit = (endDate ?? (DateTime.TryParse(bit, CultureInfo.InvariantCulture, DateTimeStyles.None, out var b2) ? b2 : DateTime.Today)).ToString("yyyy-MM-dd");
            string sk = sekme;
            if (string.IsNullOrEmpty(sk)) { string ht = (hesapTipi ?? "").ToUpperInvariant(), tb = (tab ?? "").ToUpperInvariant(); sk = tb == "FINEKRA" ? "finekra" : tb == "VIRMAN" ? "eslesme" : ht == "BANKA" ? "banka" : ht == "CEK" || ht == "ÇEK" ? "cek" : ht == "KASA" ? "kasa" : "finekra"; }
            ViewBag.Sekme = sk;
            OrnekDoldurucu.Hazirla(this);
            return View("Index");
        }

        /// <summary>Ana projedeki eski görünüm (Views/Rapor123/Eski.cshtml) — karşılaştırma için.</summary>
        [HttpGet("Eski")]
        public IActionResult Eski(DateTime? startDate, DateTime? endDate, string hesapTipi = "KASA", string tab = "TUMU", string dbFilter = "TUMU")
        {
            hesapTipi = (hesapTipi ?? "KASA").ToUpperInvariant(); if (hesapTipi == "ÇEK") hesapTipi = "CEK"; tab = (tab ?? "TUMU").ToUpperInvariant();
            var bas = (startDate ?? DateTime.Today.AddDays(-30)).Date; var bit = (endDate ?? DateTime.Today).Date;
            var sirketler = CekAkibetOrnek.Sirketler.Where(s => dbFilter == "TUMU" || string.IsNullOrEmpty(dbFilter) || s.DbKey == dbFilter || s.DbName == dbFilter).ToList();
            var hepsi = Hareketler(sirketler, bas, bit);
            var model = hepsi.Where(h => h.HesapTipi == hesapTipi && (tab == "TUMU" || tab == "FINEKRA" || tab == "VIRMAN" || h.IslemTipi == tab)).OrderByDescending(h => h.KayitTarihi).ThenBy(h => h.Sirket).ThenBy(h => h.IslemNo).ToList();

            ViewBag.HesapTipi = hesapTipi; ViewBag.CurrentTab = tab; ViewBag.CurrentDbFilter = dbFilter ?? "TUMU"; ViewBag.StartDate = bas.ToString("yyyy-MM-dd"); ViewBag.EndDate = bit.ToString("yyyy-MM-dd");
            ViewBag.IsAuthorized = true; ViewBag.FinekraHata = false; ViewBag.SqlError = null; ViewBag.PrintDekontId = null;
            ViewBag.KonsolideSirketler = CekAkibetOrnek.Sirketler.Select(s => new R123_DatabaseConfig { Key = s.DbKey, Display = s.Ad }).ToList();
            ViewBag.KonsolideBakiyeler = hepsi.GroupBy(h => new { h.HesapTipi, h.IslemTipi }).Select(g => new R123_KonsolideBakiye { HesapTipi = g.Key.HesapTipi, IslemTipi = g.Key.IslemTipi, ToplamGiris = g.Sum(x => x.Borc), ToplamCikis = g.Sum(x => x.Alacak) }).ToList();
            var cekSatirlari = hepsi.Where(h => h.HesapTipi == "CEK").ToList();
            ViewBag.CekOzet = new R123_CekOzetModel
            {
                GirenAdet = cekSatirlari.Count(h => h.IslemYonu == "Giriş"), GirenTutar = cekSatirlari.Where(h => h.IslemYonu == "Giriş").Sum(h => h.Borc),
                CikanAdet = cekSatirlari.Count(h => h.IslemYonu == "Çıkış"), CikanTutar = cekSatirlari.Where(h => h.IslemYonu == "Çıkış").Sum(h => h.Alacak),
                TedarikciyeAdet = cekSatirlari.Count(h => h.IslemYonu == "Çıkış" && h.IbrazTuru == "Ciro"), TedarikciyeTutar = cekSatirlari.Where(h => h.IslemYonu == "Çıkış" && h.IbrazTuru == "Ciro").Sum(h => h.Alacak),
                BankayaVerilenAdet = cekSatirlari.Count(h => h.IslemYonu == "Çıkış" && h.IbrazTuru == "Bankaya İbraz"), BankayaVerilenTutar = cekSatirlari.Where(h => h.IslemYonu == "Çıkış" && h.IbrazTuru == "Bankaya İbraz").Sum(h => h.Alacak),
                TahsilEdilenAdet = cekSatirlari.Count(h => h.IslemYonu == "Çıkış" && (h.Aciklama ?? "").Contains("Tahsil Edildi")), TahsilEdilenTutar = cekSatirlari.Where(h => h.IslemYonu == "Çıkış" && (h.Aciklama ?? "").Contains("Tahsil Edildi")).Sum(h => h.Alacak)
            };
            var kasaS = hepsi.Where(h => h.HesapTipi == "KASA").ToList(); ViewBag.KasaOzet = new R123_KasaOzetModel { GirenAdet = kasaS.Count(h => h.IslemYonu == "Giriş"), GirenTutar = kasaS.Sum(h => h.Borc), CikanAdet = kasaS.Count(h => h.IslemYonu == "Çıkış"), CikanTutar = kasaS.Sum(h => h.Alacak) };
            var bankaS = hepsi.Where(h => h.HesapTipi == "BANKA").ToList(); ViewBag.BankaOzet = new R123_BankaOzetModel { GirenAdet = bankaS.Count(h => h.IslemYonu == "Giriş"), GirenTutar = bankaS.Sum(h => h.Borc), CikanAdet = bankaS.Count(h => h.IslemYonu == "Çıkış"), CikanTutar = bankaS.Sum(h => h.Alacak) };
            ViewBag.KasaBakiyeleri = sirketler.SelectMany(s => NakitAkisOrnek.Bakiyeler(s.DbName).Where(b => b.BankaKasa == "KASA").Select(b => new R123_KasaBakiyeModel { DbKey = s.DbKey, Sirket = s.Ad, AcctCode = "100.01.001", AcctName = b.Detay, Currency = b.PB, TLBalance = b.TutarTL, CurrencyBalance = b.Tutar })).ToList();
            ViewBag.BankaBakiyeleri = sirketler.SelectMany(s => NakitAkisOrnek.Bakiyeler(s.DbName).Where(b => b.BankaKasa == "BANKA").Select((b, i) => new R123_BankaBakiyeModel { DbKey = s.DbKey, Sirket = s.Ad, AcctCode = "102.01." + (i + 1).ToString("000"), AcctName = b.Detay, Currency = b.PB, SapTLBalance = b.TutarTL, SapCurrencyBalance = b.Tutar, FinekraTLBalance = b.TutarTL + (i == 1 ? 12_500m : 0), FinekraCurrencyBalance = b.Tutar + (i == 1 ? 12_500m : 0) })).ToList();
            ViewBag.EmailList = OrnekVeri.Kullanicilar.Select(k => k.Kod + "@urasholding.com").ToList();
            ViewBag.SapBankAccounts = sirketler.SelectMany(s => NakitAkisOrnek.Bakiyeler(s.DbName).Where(b => b.BankaKasa == "BANKA").Select((b, i) => new R123_BankAccountModel { DbKey = s.DbKey, AcctCode = "102.01." + (i + 1).ToString("000"), AcctName = b.Detay, Currency = b.PB })).ToList();
            ViewBag.AllSapAccounts = ((List<R123_BankAccountModel>)ViewBag.SapBankAccounts).Concat(new[] { new R123_BankAccountModel { DbKey = "DefaultConnection", AcctCode = "770.01.001", AcctName = "Genel Yönetim Giderleri", Currency = "TRY" }, new R123_BankAccountModel { DbKey = "DefaultConnection", AcctCode = "191.01.001", AcctName = "İndirilecek KDV", Currency = "TRY" }, new R123_BankAccountModel { DbKey = "DefaultConnection", AcctCode = "689.01.001", AcctName = "KKEG", Currency = "TRY" } }).ToList();
            ViewBag.AccountsAndBPs = CekOrnek.TedarikciAdlari.Select((t, i) => new R123_AccountBPModel { DbKey = "DefaultConnection", Code = "320." + (i + 1).ToString("000"), Name = t, IsBP = true }).ToList();
            ViewBag.NakitAkisListesi = sirketler.SelectMany(s => NakitAkisOrnek.Odemeler(s.DbName).Where(m => m.U_OdemeYontemi == "Havale / EFT" && (m.U_Durum == "Planlandı" || m.U_Durum == "ÖDENDİ") && m.U_PlanlananTarih >= bas && m.U_PlanlananTarih <= bit).Take(12).Select(m => new R123_NakitAkisViewModel { DbKey = s.DbKey, Sirket = s.Ad, DocEntry = m.DocEntry, PlanlananTarih = m.U_PlanlananTarih, OdenmeTarihi = m.U_OdenmeTarihi, CardCode = "", KurumAdi = m.U_Kurum, BagliBelgeTipi = m.U_BagliBelgeTipi, FaturaSiparisNo = m.U_FaturaSiparisNo, UrunHizmetAciklama = m.U_UrunHizmet, BelgePB = m.U_ParaBirimi, BelgeTutar = m.U_Tutar ?? 0, AnlasilanKur = m.U_Kur ?? 1, OdenenPB = m.U_OdenenTutarPB, TutarTL = m.U_TutarTL ?? 0, VirmanDurumu = m.U_Durum == "ÖDENDİ" ? "TAMAMLANDI" : "BEKLIYOR", VirmanTransId = m.U_Durum == "ÖDENDİ" ? m.DocEntry + 70000 : (int?)null, FinekraEslestiMi = m.U_Durum == "ÖDENDİ", FinekraId = m.U_Durum == "ÖDENDİ" ? "FNK-" + m.DocEntry : null, FinekraIslemTarihi = m.U_OdenmeTarihi, FinekraBanka = m.U_BankaHesabi?.Split(" · ")[0], FinekraTutar = m.U_Durum == "ÖDENDİ" ? m.U_TutarTL : null, FinekraAciklama = m.U_Durum == "ÖDENDİ" ? m.U_DekontNo : null })).ToList();
            ViewBag.FinekraHareketler = new List<R123_BankaHareketiViewModel>();
            ViewBag.KasaListesi = new List<object>(); ViewBag.CariListesi = new List<object>(); ViewBag.HesapListesi = new List<object>();
            OrnekDoldurucu.Hazirla(this);
            return View("Eski", model);
        }

        // ---------- modern ekran verisi ----------
        [HttpGet("Veri")]
        public IActionResult Veri(string sirket, string bas, string bit, string gun)
        {
            if (string.Equals(sirket, "tumu", StringComparison.OrdinalIgnoreCase) || !CekAkibetOrnek.Sirketler.Any(s => s.DbName == sirket)) sirket = null; // konsolide
            var b1 = DateTime.TryParse(bas, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1) ? d1.Date : DateTime.Today.AddDays(-30);
            var b2 = DateTime.TryParse(bit, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d2) ? d2.Date : DateTime.Today;
            var gunT = DateTime.TryParse(gun, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dg) ? dg.Date : (b2 <= DateTime.Today ? b2 : DateTime.Today); // gün sonu mutabakatı günü
            var sirketler = CekAkibetOrnek.Sirketler.Where(s => string.IsNullOrEmpty(sirket) || s.DbName == sirket).ToList();
            var finekraListe = FinekraOrnek.Listele(string.IsNullOrEmpty(sirket) ? null : sirket, b1, b2);
            var fnkMap = finekraListe.Where(h => h.NakitAkisDocEntry != null).GroupBy(h => h.NakitAkisDb + "|" + h.NakitAkisDocEntry).ToDictionary(g => g.Key, g => g.First());
            var hepsi = Hareketler(sirketler, b1, b2);
            object HareketJson(R123_HareketViewModel h)
            {
                string dbName = CekAkibetOrnek.DbName(h.Sirket); bool nakitAkistan = (h.HesapTipi == "KASA" || h.HesapTipi == "BANKA") && h.IslemNo < 90000;
                fnkMap.TryGetValue(dbName + "|" + h.IslemNo, out var f);
                return new
                {
                    key = $"{h.DbKey}|{h.HesapTipi}|{h.IslemNo}", dbKey = h.DbKey, dbName, sirket = h.Sirket, islemNo = h.IslemNo, tarih = h.KayitTarihi.ToString("yyyy-MM-dd"), hesapTipi = h.HesapTipi, kasaKodu = h.KasaKodu, kasaAdi = h.KasaAdi, karsiKod = h.KarsiHesapKodu, karsiAd = h.KarsiHesapAdi,
                    borc = h.Borc, alacak = h.Alacak, pb = h.ParaBirimi, aciklama = h.Aciklama, islemTipi = h.IslemTipi, yon = h.IslemYonu,
                    onay = new { durum = h.OnayDurumu ?? "", kullanici = h.OnaylayanKullaniciAdi, tarih = h.OnayTarihi?.ToString("yyyy-MM-dd"), notBaslik = h.NotBasligi, notIcerik = h.NotIcerigi },
                    cek = h.HesapTipi == "CEK" || h.CekDocEntry != null ? new { docEntry = h.CekDocEntry, no = h.CekNumarasi, vade = h.CekVadeTarihi?.ToString("yyyy-MM-dd"), belge = h.CekBelge, planlananTedarikci = h.PlanlananTedarikci, planKaynak = h.PlanKaynak, planlamaTarihi = h.PlanlamaTarihi?.ToString("yyyy-MM-dd"), ibrazYeri = h.IbrazYeri, ibrazTuru = h.IbrazTuru, ibrazTarihi = h.IbrazTarihi?.ToString("yyyy-MM-dd"), ibrazBelgeNo = h.IbrazBelgeNo, kontrol = h.PlanKontrol } : null,
                    nakitAkisDocEntry = nakitAkistan ? h.IslemNo : (int?)null, nakitAkisLink = nakitAkistan ? $"/Rapor79?sirket={dbName}&sekme={(h.HesapTipi == "KASA" ? "nakit" : "havale")}" : null,
                    fnk = f == null ? null : new { id = f.Id, no = f.No, durum = f.Durum, eslesme = f.Eslesme }
                };
            }
            var hareketler = hepsi.OrderByDescending(h => h.KayitTarihi).ThenBy(h => h.Sirket).ThenBy(h => h.IslemNo).Select(HareketJson).ToList();
            var bankaBak = sirketler.SelectMany(s => NakitAkisOrnek.Bakiyeler(s.DbName).Where(b => b.BankaKasa == "BANKA").Select((b, i) => new { dbName = s.DbName, sirket = s.Ad, hesapKodu = "102.01." + (i + 1).ToString("000"), hesapAdi = b.Detay, pb = b.PB, sapTL = b.TutarTL, sapDoviz = b.Tutar, finekraTL = b.TutarTL + (i == 1 ? 12_500m : 0), finekraDoviz = b.Tutar + (i == 1 ? 12_500m : 0), fark = (i == 1 ? 12_500m : 0), guncelleme = b.GuncellenmeTarihi?.ToString("yyyy-MM-dd") })).ToList();
            var kasaBak = sirketler.SelectMany(s => NakitAkisOrnek.Bakiyeler(s.DbName).Where(b => b.BankaKasa == "KASA").Select(b => new { dbName = s.DbName, sirket = s.Ad, hesapKodu = "100.01.001", hesapAdi = b.Detay, pb = b.PB, tl = b.TutarTL, doviz = b.Tutar, guncelleme = b.GuncellenmeTarihi?.ToString("yyyy-MM-dd") })).ToList();
            object Ozet(string tip) { var l = hepsi.Where(h => h.HesapTipi == tip).ToList(); return new { girisAdet = l.Count(h => h.IslemYonu == "Giriş"), giris = l.Sum(h => h.Borc), cikisAdet = l.Count(h => h.IslemYonu == "Çıkış"), cikis = l.Sum(h => h.Alacak), onayBekleyen = l.Count(h => string.IsNullOrEmpty(h.OnayDurumu)), eksik = l.Count(h => h.OnayDurumu == "EKSİK"), farkli = l.Count(h => h.PlanKontrol == "Farklı") }; }
            var eslesmeyen = finekraListe.Where(h => h.NakitAkisDocEntry == null && !h.Giris && h.IslemTuru != "Virman").ToList(); var bekleyen = finekraListe.Where(h => h.Durum != "Onaylandı").ToList();
            // gün sonu mutabakatı (şirket × gün): gün içi onay → kalan bakiyeler → fiziksel sayım
            var gunSonu = sirketler.Select(s => GunSonuJson(s, gunT)).ToList(); var gsKayit = sirketler.Select(s => GunSonuOrnek.Bul(s.DbName, gunT)).ToList();
            int gsGunIci = gsKayit.Count(g => g != null && g.GunIciDurum == "Onaylandı"), gsSayim = gsKayit.Count(g => g != null && g.SayimDurumu == "Onaylandı"), gsEksik = gsKayit.Count(g => g != null && g.SayimDurumu == "Eksik / Fazla");
            var bildirimler = GunSonuOrnek.Bildirimler(sirket);
            return Json(new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), bas = b1.ToString("yyyy-MM-dd"), bit = b2.ToString("yyyy-MM-dd"), kullanici = Kullanici,
                sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, dbName = s.DbName, dbKey = s.DbKey }),
                hareketler, finekra = finekraListe.Select(FinekraOrnek.Json).ToList(), eslesme = FinekraOrnek.NakitAkisEslesme(string.IsNullOrEmpty(sirket) ? null : sirket, b1, b2),
                bakiyeler = new { banka = bankaBak, kasa = kasaBak },
                ozet = new { kasa = Ozet("KASA"), banka = Ozet("BANKA"), cek = Ozet("CEK"), finekra = new { toplam = finekraListe.Count, bekleyen = bekleyen.Count, bekleyenTutar = bekleyen.Sum(h => h.Tutar), eslesmeyen = eslesmeyen.Count, eslesmeyenTutar = eslesmeyen.Sum(h => h.Tutar), onayli = finekraListe.Count(h => h.Durum == "Onaylandı"), giris = finekraListe.Where(h => h.Giris).Sum(h => h.Tutar), cikis = finekraListe.Where(h => !h.Giris).Sum(h => h.Tutar), sonAkis = FinekraOrnek.SonAkis.ToString("yyyy-MM-dd HH:mm") }, bakiyeFark = bankaBak.Sum(b => b.fark), gunSonu = new { toplam = sirketler.Count, gunIciOnayli = gsGunIci, bakiyeOnayli = gsSayim, eksikFazla = gsEksik, acikBildirim = bildirimler.Count(b => b.Durum == "Açık") } },
                cariler = FinekraOrnek.Cariler(string.IsNullOrEmpty(sirket) ? null : sirket), hesaplar = FinekraOrnek.KarsiHesaplar.Select(k => new { kod = k.Kod, ad = k.Ad }), islemTurleri = FinekraOrnek.IslemTurleri,
                gunluk = FinekraOrnek.GunlukListesi(string.IsNullOrEmpty(sirket) ? null : sirket).Select(g => new { tarih = g.Tarih.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, g.Sirket, g.No, g.Islem, g.Aciklama }),
                gun = gunT.ToString("yyyy-MM-dd"), gunSonu, bildirimler = bildirimler.Select(GunSonuOrnek.BildirimJson).ToList(),
                sorumlular = new { gunIci = new { ad = GunSonuOrnek.GunIciSorumlu.Ad, unvan = GunSonuOrnek.GunIciSorumlu.Unvan }, kasa = new { ad = GunSonuOrnek.KasaSorumlu.Ad, unvan = GunSonuOrnek.KasaSorumlu.Unvan } }
            });
        }

        // ---------- gün sonu mutabakatı (Data/GunSonuOrnek.cs): 1) gün içi işlemler onayı — Ahmet Özkan · 2) kalan bakiyeler otomatik · 3) fiziksel sayım — Berk Berberoğlu ----------
        public class GunSonuIstek { public string DbName { get; set; } public string Gun { get; set; } public string Onaylayan { get; set; } public string Not { get; set; } public string SayimYapan { get; set; } public List<GsSayim> Sayim { get; set; } public bool Onayla { get; set; } public string BelgeNolar { get; set; } public int Id { get; set; } public string Cozum { get; set; } }
        static bool GunCoz(string s, out DateTime gun) => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out gun);
        /// <summary>Günün kasa / banka / çek hareketleri, onaysız / eksik sayıları, Finekra bekleyenleri ve canlı kalan bakiyeler.</summary>
        static (List<R123_HareketViewModel> gunH, int onaysiz, int eksik, int fnkBekleyen, List<GsBakiye> canli) GunVerisi((string Ad, string DbName, string DbKey) s, DateTime gun)
        {
            var gunH = Hareketler(new List<(string Ad, string DbName, string DbKey)> { s }, gun, gun);
            int onaysiz = gunH.Count(h => string.IsNullOrEmpty(h.OnayDurumu)), eksik = gunH.Count(h => h.OnayDurumu == "EKSİK");
            int fnkBekleyen = FinekraOrnek.Listele(s.DbName, gun, gun).Count(h => h.Durum != "Onaylandı");
            var kasa = gunH.Where(h => h.HesapTipi == "KASA").ToList();
            var canli = GunSonuOrnek.Hesapla(s.DbName, gun, kasa.Count(h => h.IslemYonu == "Giriş"), kasa.Sum(h => h.Borc), kasa.Count(h => h.IslemYonu == "Çıkış"), kasa.Sum(h => h.Alacak));
            return (gunH, onaysiz, eksik, fnkBekleyen, canli);
        }
        static object GunSonuJson((string Ad, string DbName, string DbKey) s, DateTime gun) { var g = GunSonuOrnek.Bul(s.DbName, gun) ?? new GunSonu { DbName = s.DbName, Sirket = s.Ad, Gun = gun.Date }; var v = GunVerisi(s, gun); return GunSonuOrnek.Json(g, v.canli, v.gunH.Count, v.onaysiz, v.eksik, v.fnkBekleyen); }
        static (string Ad, string DbName, string DbKey) SirketBul(string dbName) => CekAkibetOrnek.Sirketler.FirstOrDefault(x => x.DbName == dbName);

        /// <summary>1) Gün içi işlemler onayı: günün onaysız kasa / banka / çek satırları toplu onaylanır (eksik / kontrol işaretliler hariç), kalan bakiyeler kesinleşir.</summary>
        [HttpPost("GunIciOnayla")]
        public IActionResult GunIciOnayla([FromBody] GunSonuIstek istek)
        {
            if (istek == null || !GunCoz(istek.Gun, out var gun)) return Json(new { success = false, message = "Şirket ve gün gerekli." });
            var s = SirketBul(istek.DbName); if (s.DbName == null) return Json(new { success = false, message = "Şirket bulunamadı." });
            if (!string.Equals((istek.Onaylayan ?? "").Trim(), GunSonuOrnek.GunIciSorumlu.Ad, StringComparison.OrdinalIgnoreCase)) return Json(new { success = false, message = $"Gün içi işlemleri yalnız {GunSonuOrnek.GunIciSorumlu.Ad} ({GunSonuOrnek.GunIciSorumlu.Unvan}) onaylar." });
            var mevcut = GunSonuOrnek.Bul(s.DbName, gun); if (mevcut != null && mevcut.GunIciDurum == "Onaylandı") return Json(new { success = false, message = $"{s.Ad} {gun:dd.MM.yyyy} gün içi işlemleri zaten onaylı ({mevcut.GunIciOnaylayan}, {mevcut.GunIciOnayTarihi:dd.MM.yyyy HH:mm})." });
            if (gun > DateTime.Today) return Json(new { success = false, message = "İleri tarih için gün sonu onayı verilemez." });
            var v = GunVerisi(s, gun); int onaylanan = 0;
            foreach (var h in v.gunH.Where(h => string.IsNullOrEmpty(h.OnayDurumu))) { FinekraOrnek.HareketOnayla($"{h.DbKey}|{h.HesapTipi}|{h.IslemNo}", "ONAYLANDI", "Gün içi işlemler onayı", GunSonuOrnek.GunIciSorumlu.Ad, false); onaylanan++; }
            var r = GunSonuOrnek.GunIciOnayla(s.DbName, gun, istek.Onaylayan, istek.Not, Kullanici, v.gunH.Count, onaylanan, v.eksik, v.fnkBekleyen, v.canli);
            if (r.ok) FinekraOrnek.GunlukEkle(s.Ad, $"Gün Sonu {gun:dd.MM.yyyy}", "Gün İçi Onay", r.mesaj, GunSonuOrnek.GunIciSorumlu.Ad);
            return Json(new { success = r.ok, message = r.mesaj, gunSonu = r.ok ? GunSonuJson(s, gun) : null });
        }
        [HttpPost("GunIciGeriAl")]
        public IActionResult GunIciGeriAl([FromBody] GunSonuIstek istek)
        {
            if (istek == null || !GunCoz(istek.Gun, out var gun)) return Json(new { success = false, message = "Şirket ve gün gerekli." });
            var s = SirketBul(istek.DbName); if (s.DbName == null) return Json(new { success = false, message = "Şirket bulunamadı." });
            var r = GunSonuOrnek.GunIciGeriAl(s.DbName, gun, Kullanici); if (r.ok) FinekraOrnek.GunlukEkle(s.Ad, $"Gün Sonu {gun:dd.MM.yyyy}", "Gün İçi Onay Geri Alındı", r.mesaj, Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, gunSonu = r.ok ? GunSonuJson(s, gun) : null });
        }
        /// <summary>3) Fiziksel sayım: Onayla=true → sistemle birebir eşleşmeli (bakiyeler onaylanır); Onayla=false → eksik / fazla bildirimi.</summary>
        [HttpPost("GunSonuSayim")]
        public IActionResult GunSonuSayim([FromBody] GunSonuIstek istek)
        {
            if (istek == null || !GunCoz(istek.Gun, out var gun)) return Json(new { success = false, message = "Şirket ve gün gerekli." });
            var s = SirketBul(istek.DbName); if (s.DbName == null) return Json(new { success = false, message = "Şirket bulunamadı." });
            var r = GunSonuOrnek.SayimKaydet(s.DbName, gun, istek.SayimYapan, istek.Sayim, istek.Onayla, istek.Not, istek.BelgeNolar, Kullanici);
            if (r.ok) FinekraOrnek.GunlukEkle(s.Ad, $"Gün Sonu {gun:dd.MM.yyyy}", istek.Onayla ? "Bakiye Onayı" : "Eksik / Fazla Bildirimi", r.mesaj, GunSonuOrnek.KasaSorumlu.Ad);
            return Json(new { success = r.ok, message = r.mesaj, gunSonu = r.ok ? GunSonuJson(s, gun) : null });
        }
        [HttpPost("GunSonuBildirimCoz")]
        public IActionResult GunSonuBildirimCoz([FromBody] GunSonuIstek istek)
        {
            if (istek == null) return Json(new { success = false, message = "Veri yok." });
            var r = GunSonuOrnek.BildirimCoz(istek.Id, istek.Cozum, Kullanici);
            if (r.ok) FinekraOrnek.GunlukEkle(r.b.Sirket, $"Gün Sonu {r.b.Gun:dd.MM.yyyy}", "Bildirim Çözüldü", $"{r.b.Tur} {r.b.Yon} · {r.b.Cozum}", Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, bildirim = r.b == null ? null : GunSonuOrnek.BildirimJson(r.b) });
        }
        /// <summary>Sayım yardımcısı: gün sonu itibarıyla portföydeki belgeler (tür: "Çek X", "Senet R" …).</summary>
        [HttpGet("GunSonuPortfoy")]
        public IActionResult GunSonuPortfoy(string sirket, string gun, string tur) { if (!GunCoz(gun, out var g)) g = DateTime.Today; return Json(new { success = true, data = GunSonuOrnek.Portfoy(sirket, g, tur) }); }
        public class HareketOnayIstek { public string Key { get; set; } public string Durum { get; set; } public string Not { get; set; } }
        /// <summary>Kasa / Banka / Çek satırı onayı (ONAYLANDI | EKSİK | boş); bellek içi (Data/FinekraOrnek.cs HareketOnaylari).</summary>
        [HttpPost("HareketOnay")]
        public IActionResult HareketOnay([FromBody] HareketOnayIstek istek)
        {
            if (istek == null || string.IsNullOrEmpty(istek.Key)) return Json(new { success = false, message = "Veri yok." });
            FinekraOrnek.HareketOnayla(istek.Key, istek.Durum ?? "", istek.Not, Kullanici);
            return Json(new { success = true, message = istek.Durum == "EKSİK" ? "Satır eksik / kontrol gerekli olarak işaretlendi." : istek.Durum == "ONAYLANDI" ? "Satır onaylandı." : "Onay kaldırıldı.", onay = new { durum = istek.Durum ?? "", kullanici = Kullanici, tarih = DateTime.Today.ToString("yyyy-MM-dd"), notBaslik = istek.Durum == "EKSİK" ? "Eksik / kontrol" : (string.IsNullOrEmpty(istek.Not) ? null : "Not"), notIcerik = istek.Not } });
        }
        public class FinekraIstek { public int Id { get; set; } public string CariKod { get; set; } public string CariAd { get; set; } public string KarsiHesap { get; set; } public string Not { get; set; } public string DbName { get; set; } public int DocEntry { get; set; } }
        /// <summary>Finekra hareketi onayı: cari / hesap → muhasebe fişi; eşleşmiş nakit akış satırı ÖDENDİ olur.</summary>
        [HttpPost("FinekraOnayla")]
        public IActionResult FinekraOnayla([FromBody] FinekraIstek istek) { if (istek == null) return Json(new { success = false, message = "Veri yok." }); var r = FinekraOrnek.Onayla(istek.Id, istek.CariKod, istek.CariAd, istek.KarsiHesap, istek.Not, Kullanici); return Json(new { success = r.ok, message = r.mesaj, hareket = FinekraOrnek.Json(r.h) }); }
        /// <summary>Nakit akışta karşılığı olmayan banka çıkışı → Kasa Nakit Akışı'nda ÖDENDİ satır.</summary>
        [HttpPost("FinekraNakitAkisaGonder")]
        public IActionResult FinekraNakitAkisaGonder([FromBody] FinekraIstek istek) { if (istek == null) return Json(new { success = false, message = "Veri yok." }); var r = FinekraOrnek.NakitAkisaGonder(istek.Id, Kullanici); return Json(new { success = r.ok, message = r.mesaj, hareket = FinekraOrnek.Json(r.h), satir = r.satir == null ? null : new { docEntry = r.satir.DocEntry, dbName = r.satir.U_KaynakFirma, link = $"/Rapor79?sirket={r.satir.U_KaynakFirma}&sekme=havale" } }); }
        [HttpPost("FinekraEslestir")]
        public IActionResult FinekraEslestir([FromBody] FinekraIstek istek) { if (istek == null) return Json(new { success = false, message = "Veri yok." }); var r = FinekraOrnek.Eslestir(istek.Id, istek.DbName, istek.DocEntry, Kullanici); return Json(new { success = r.ok, message = r.mesaj, hareket = FinekraOrnek.Json(r.h) }); }
        [HttpPost("FinekraEslesmeKaldir")]
        public IActionResult FinekraEslesmeKaldir([FromBody] FinekraIstek istek) { if (istek == null) return Json(new { success = false, message = "Veri yok." }); var r = FinekraOrnek.EslesmeKaldir(istek.Id, Kullanici); return Json(new { success = r.ok, message = r.mesaj, hareket = FinekraOrnek.Json(r.h) }); }
        /// <summary>Elle eşleştirme adayları (aynı şirkette bağlanmamış Havale / EFT satırları, tutar yakınlığına göre).</summary>
        [HttpGet("FinekraAdaylar")] public IActionResult FinekraAdaylar(int id) => Json(new { success = true, data = FinekraOrnek.Adaylar(id) });
        [HttpGet("FinekraVeri")] public IActionResult FinekraVeri(int id) { var h = FinekraOrnek.Bul(id); return Json(h == null ? new { success = false, message = "Hareket bulunamadı." } : (object)new { success = true, hareket = FinekraOrnek.Json(h), yevmiye = MuhasebeOrnek.Json(MuhasebeOrnek.Bul(h.YevmiyeNo)) }); }

        // ---------- hareket üreticileri (Kasa / Banka / Çek) ----------
        static List<R123_HareketViewModel> Hareketler(List<(string Ad, string DbName, string DbKey)> sirketler, DateTime bas, DateTime bit)
        {
            var hepsi = new List<R123_HareketViewModel>();
            foreach (var s in sirketler) { hepsi.AddRange(Kasa(s, bas, bit)); hepsi.AddRange(Banka(s, bas, bit)); hepsi.AddRange(Cek(s, bas, bit)); }
            // Takip Paneli'nde elle verilen onay / eksik işaretleri (bellek içi) üretilmiş satırların üstüne yazılır
            foreach (var h in hepsi) if (FinekraOrnek.HareketOnaylari.TryGetValue($"{h.DbKey}|{h.HesapTipi}|{h.IslemNo}", out var o)) { h.OnayDurumu = o.Durum; h.OnaylayanKullaniciAdi = o.Durum == "" ? null : o.Kullanici; h.OnayTarihi = o.Durum == "" ? (DateTime?)null : o.Tarih; if (!string.IsNullOrEmpty(o.Not)) { h.NotBasligi = o.Durum == "EKSİK" ? "Eksik / kontrol" : "Not"; h.NotIcerigi = o.Not; } }
            return hepsi;
        }
        static R123_HareketViewModel Temel((string Ad, string DbName, string DbKey) s, string hesapTipi, int islemNo, DateTime tarih, string islemTipi) => new R123_HareketViewModel { DbKey = s.DbKey, Sirket = s.Ad, IslemNo = islemNo, KayitTarihi = tarih, HesapTipi = hesapTipi, IslemTipi = islemTipi, ParaBirimi = "TRY", OnayDurumu = "ONAYLANDI", OnaylayanKullaniciAdi = "Nazlı Erdem", OnayTarihi = tarih.AddDays(1) };
        /// <summary>Kasa: ödenen Nakit satırları (çıkış) + örnek müşteri nakit tahsilatları (giriş).</summary>
        static IEnumerable<R123_HareketViewModel> Kasa((string Ad, string DbName, string DbKey) s, DateTime bas, DateTime bit)
        {
            foreach (var m in NakitAkisOrnek.Odemeler(s.DbName).Where(m => m.U_OdemeYontemi == "Nakit" && m.U_Durum == "ÖDENDİ" && m.U_OdenmeTarihi >= bas && m.U_OdenmeTarihi <= bit))
            { var h = Temel(s, "KASA", m.DocEntry, m.U_OdenmeTarihi.Value, m.U_IslemTipi == "R" ? "R" : "X"); h.KasaKodu = "100.01.001"; h.KasaAdi = m.U_Kasa; h.KarsiHesapKodu = "320"; h.KarsiHesapAdi = m.U_Kurum; h.Alacak = m.U_OdenenTutar ?? m.U_TutarTL ?? 0; h.IslemYonu = "Çıkış"; h.Aciklama = $"{m.U_UrunHizmet} · makbuz {m.U_MakbuzNo} · teslim alan {m.U_TeslimAlan}"; yield return h; }
            var rnd = new Random(Seed("kasa|" + s.DbName)); var musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" && k.Sirket == s.Ad).ToList(); if (musteriler.Count == 0) musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").ToList();
            decimal olcek = ButceOrnek.Olcek.TryGetValue(s.Ad, out var o) ? o : 0.3m;
            for (int i = 0; i < 8 + (int)(10 * olcek); i++)
            {
                var t = DateTime.Today.AddDays(-rnd.Next(0, 60)); if (t < bas || t > bit) continue; var mu = musteriler[rnd.Next(musteriler.Count)];
                var h = Temel(s, "KASA", 90000 + i + Seed(s.DbName) % 1000, t, rnd.NextDouble() < 0.75 ? "X" : "R"); h.KasaKodu = "100.01.001"; h.KasaAdi = "Merkez Kasa"; h.KarsiHesapKodu = mu.Kod; h.KarsiHesapAdi = mu.KisaAd; h.Borc = rnd.Next(5, 120) * 1000m; h.IslemYonu = "Giriş"; h.Aciklama = "Nakit tahsilat · tahsilat makbuzu TM-" + rnd.Next(10000, 99999); if (rnd.NextDouble() < 0.15) { h.OnayDurumu = ""; h.OnaylayanKullaniciAdi = null; h.OnayTarihi = null; } yield return h;
            }
        }
        /// <summary>Banka: ödenen Havale / EFT satırları (çıkış), tahsil edilen çekler (giriş) ve örnek müşteri havaleleri (giriş).</summary>
        static IEnumerable<R123_HareketViewModel> Banka((string Ad, string DbName, string DbKey) s, DateTime bas, DateTime bit)
        {
            foreach (var m in NakitAkisOrnek.Odemeler(s.DbName).Where(m => m.U_OdemeYontemi == "Havale / EFT" && m.U_Durum == "ÖDENDİ" && m.U_OdenmeTarihi >= bas && m.U_OdenmeTarihi <= bit))
            { var h = Temel(s, "BANKA", m.DocEntry, m.U_OdenmeTarihi.Value, m.U_IslemTipi == "R" ? "R" : "X"); var banka = (m.U_BankaHesabi ?? "Ziraat Bankası").Split(" · ")[0]; h.KasaKodu = "102.01." + (Array.IndexOf(NakitAkisOrnek.BankaHesaplari, m.U_BankaHesabi) + 1).ToString("000"); h.KasaAdi = banka; h.KarsiHesapKodu = "320"; h.KarsiHesapAdi = m.U_Kurum; h.Alacak = m.U_OdenenTutar.HasValue && (m.U_OdenenTutarPB ?? "TRY") == "TRY" ? m.U_OdenenTutar.Value : m.U_TutarTL ?? 0; h.ParaBirimi = "TRY"; h.IslemYonu = "Çıkış"; h.Aciklama = $"{m.U_UrunHizmet} · {m.U_FaturaSiparisNo} · dekont {m.U_DekontNo} · alıcı IBAN {m.U_AliciIban}"; if (m.DekontListesi.Count == 0 && m.DocEntry % 7 == 0) { h.OnayDurumu = ""; h.OnaylayanKullaniciAdi = null; h.OnayTarihi = null; } yield return h; }
            foreach (var k in CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == s.Ad && k.IslemTuru == "Bankadan Tahsil Edildi" && k.Vade >= bas && k.Vade <= bit))
            { var h = Temel(s, "BANKA", k.DocEntry * 10 + 3, k.Vade, k.DocEntry % 4 == 0 ? "R" : "X"); h.KasaKodu = k.IbrazYeri == "Halk Bankası" ? "102.01.002" : "102.01.001"; h.KasaAdi = k.IbrazYeri; h.KarsiHesapKodu = k.MusteriKod; h.KarsiHesapAdi = k.Musteri; h.Borc = k.Tutar; h.IslemYonu = "Giriş"; h.Aciklama = $"{k.Belge} tahsilatı {k.No} · tahsilat {k.TahsilatBelgeNo}"; h.CekNumarasi = k.No; h.CekVadeTarihi = k.Vade; yield return h; }
            var rnd = new Random(Seed("banka|" + s.DbName)); var musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" && k.Sirket == s.Ad).ToList(); if (musteriler.Count == 0) musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").ToList();
            decimal olcek = ButceOrnek.Olcek.TryGetValue(s.Ad, out var o) ? o : 0.3m;
            for (int i = 0; i < 10 + (int)(14 * olcek); i++)
            {
                var t = DateTime.Today.AddDays(-rnd.Next(0, 60)); if (t < bas || t > bit) continue; var mu = musteriler[rnd.Next(musteriler.Count)];
                var h = Temel(s, "BANKA", 95000 + i + Seed(s.DbName) % 1000, t, rnd.NextDouble() < 0.75 ? "X" : "R"); int b = rnd.Next(2); h.KasaKodu = "102.01.00" + (b + 1); h.KasaAdi = b == 0 ? "Ziraat Bankası" : "Halk Bankası"; h.KarsiHesapKodu = mu.Kod; h.KarsiHesapAdi = mu.KisaAd; h.Borc = rnd.Next(20, 900) * 1000m; h.IslemYonu = "Giriş"; h.Aciklama = "Müşteri havalesi · " + mu.KisaAd + " fatura ödemesi"; if (rnd.NextDouble() < 0.12) { h.OnayDurumu = ""; h.OnaylayanKullaniciAdi = null; h.OnayTarihi = null; } yield return h;
            }
        }
        /// <summary>Çek: portföye giriş (giriş) ve ibraz / ciro (çıkış) hareketleri; her satırda plan ↔ ibraz kontrolü. Planı, girişi ya da çıkışı aralıkta olan belgeler listelenir.
        /// Grup içi dolaşım (Kasa Nakit Akışı › Dolaşım Planı): gerçekleşen her adım eden şirkette çıkış, alan şirkette giriş satırı; planlı (gerçekleşmemiş) adımlar onay bekleyen "PLAN" satırı.</summary>
        static IEnumerable<R123_HareketViewModel> Cek((string Ad, string DbName, string DbKey) s, DateTime bas, DateTime bit)
        {
            foreach (var h in CekAkibetOrnek.Hareketler.Where(h => (h.Eden == s.Ad || h.Alan == s.Ad) && h.Tarih.Date >= bas && h.Tarih.Date <= bit))
            {
                var k = CekAkibetOrnek.Bul(h.CekDocEntry); if (k == null) continue; bool cikis = h.Eden == s.Ad;
                var r = Temel(s, "CEK", h.CekDocEntry * 10 + 3 + h.AdimSira, h.Tarih.Date, "X"); Doldur(r, k);
                r.KasaKodu = k.Belge == "Senet" ? "121.01.001" : "101.01.001"; r.KasaAdi = k.Belge == "Senet" ? "Alınan Senetler" : "Alınan Çekler"; r.KarsiHesapKodu = h.Tur == "Grup İçi Ciro" ? "131/331" : h.Tur == "Bankaya İbraz" ? "102.01" : "320"; r.KarsiHesapAdi = cikis ? h.Alan : h.Eden;
                if (cikis) r.Alacak = h.Tutar; else r.Borc = h.Tutar; r.IslemYonu = cikis ? "Çıkış" : "Giriş"; r.ParaBirimi = "TRY";
                r.Aciklama = $"{h.Tur} · {h.Eden} → {h.Alan} · {h.BelgeNo} · dolaşım adımı {h.AdimSira}/{h.AdimToplam}" + (k.Dolasim.Count > 0 ? $" · plan: {k.DolasimOzeti}" : ""); r.IbrazYeri = h.Alan; r.IbrazTuru = h.Tur; r.IbrazTarihi = h.Tarih; r.IbrazBelgeNo = h.BelgeNo; r.PlanKontrol = "Uyumlu"; r.OnaylayanKullaniciAdi = h.Kullanici;
                yield return r;
            }
            foreach (var k in CekAkibetOrnek.Kayitlar.Where(k => k.Dolasim.Count > 0))
                foreach (var a in k.Dolasim.Where(a => a.Durum == "Planlandı" && (a.Eden == s.Ad || a.Alan == s.Ad) && a.PlanTarih >= bas && a.PlanTarih <= bit))
                {
                    bool cikis = a.Eden == s.Ad; var r = Temel(s, "CEK", k.DocEntry * 10 + 5 + a.Sira, a.PlanTarih, "X"); Doldur(r, k);
                    r.KasaKodu = k.Belge == "Senet" ? "121.01.001" : "101.01.001"; r.KasaAdi = k.Belge == "Senet" ? "Alınan Senetler" : "Alınan Çekler"; r.KarsiHesapKodu = a.Tur == "Grup İçi Ciro" ? "131/331" : a.Tur == "Bankaya İbraz" ? "102.01" : "320"; r.KarsiHesapAdi = cikis ? a.Alan : a.Eden;
                    if (cikis) r.Alacak = k.Tutar; else r.Borc = k.Tutar; r.IslemYonu = cikis ? "Çıkış" : "Giriş"; r.ParaBirimi = "TRY"; r.OnayDurumu = ""; r.OnaylayanKullaniciAdi = null; r.OnayTarihi = null;
                    r.Aciklama = $"PLAN · {a.Tur} · {a.Eden} → {a.Alan} · adım {a.Sira}/{k.Dolasim.Count} · {a.Not}"; r.PlanKontrol = "İbraz Bekliyor"; r.IbrazYeri = ""; r.IbrazTuru = ""; r.IbrazBelgeNo = "";
                    yield return r;
                }
            foreach (var k in CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == s.Ad))
            {
                bool girisAralikta = k.PortfoyGiris >= bas && k.PortfoyGiris <= bit, planAralikta = k.PlanlamaTarihi != null && k.PlanlamaTarihi.Value.Date >= bas && k.PlanlamaTarihi.Value.Date <= bit, ibrazAralikta = k.IbrazTarihi != null && k.IbrazTarihi.Value.Date >= bas && k.IbrazTarihi.Value.Date <= bit;
                if (!girisAralikta && !planAralikta && !ibrazAralikta) continue;
                string tip = k.DocEntry % 4 == 0 ? "R" : "X";
                if (girisAralikta || (planAralikta && !ibrazAralikta))
                {
                    var g = Temel(s, "CEK", k.DocEntry * 10 + 1, girisAralikta ? k.PortfoyGiris : k.PlanlamaTarihi.Value.Date, tip); Doldur(g, k);
                    g.KasaKodu = k.Belge == "Senet" ? "121.01.001" : "101.01.001"; g.KasaAdi = k.Belge == "Senet" ? "Alınan Senetler" : "Alınan Çekler"; g.KarsiHesapKodu = k.MusteriKod; g.KarsiHesapAdi = k.Musteri; g.Borc = k.Tutar; g.IslemYonu = "Giriş";
                    g.Aciklama = (girisAralikta ? "Portföye giriş" : "Kasa Nakit Akışı planı") + $" · {k.Belge} {k.No}" + (k.IslemTuru == "Planlandı" ? $" · PLANLANDI → {k.PlanlananTedarikci}" : "");
                    if (k.Kontrol == "Farklı") { g.OnayDurumu = "EKSİK"; g.NotBasligi = "Plana aykırı çıkış"; g.NotIcerigi = $"Plan: {k.PlanlananTedarikci} · Çıkış: {k.IbrazYeri}"; g.OnaylayanKullaniciAdi = null; g.OnayTarihi = null; }
                    yield return g;
                }
                if (ibrazAralikta)
                {
                    var c = Temel(s, "CEK", k.DocEntry * 10 + 2, k.IbrazTarihi.Value.Date, tip); Doldur(c, k);
                    c.KasaKodu = k.Belge == "Senet" ? "121.01.001" : "101.01.001"; c.KasaAdi = k.Belge == "Senet" ? "Alınan Senetler" : "Alınan Çekler"; c.KarsiHesapKodu = k.IbrazTuru == "Ciro" ? "320" : "102.01"; c.KarsiHesapAdi = k.IbrazYeri; c.Alacak = k.Tutar; c.IslemYonu = "Çıkış";
                    c.Aciklama = $"{k.IbrazTuru} · {k.IslemTuru} · belge {k.IbrazBelgeNo}" + (k.TahsilatBelgeNo != "" ? $" · tahsilat {k.TahsilatBelgeNo}" : "") + (k.Kontrol == "Farklı" ? " · PLANA AYKIRI" : "");
                    if (k.Kontrol == "Farklı") { c.OnayDurumu = "EKSİK"; c.NotBasligi = "Plana aykırı çıkış"; c.NotIcerigi = $"Kasa Nakit Akışı planı {k.PlanlananTedarikci} iken {k.IbrazYeri} yerine çıktı"; c.OnaylayanKullaniciAdi = null; c.OnayTarihi = null; }
                    else if (k.Kontrol == "Plansız İbraz" && k.IbrazTuru == "Ciro") { c.OnayDurumu = ""; c.OnaylayanKullaniciAdi = null; c.OnayTarihi = null; }
                    yield return c;
                }
            }
        }
        static void Doldur(R123_HareketViewModel h, CekKaydi k)
        {
            h.CekNumarasi = k.No; h.CekVadeTarihi = k.Vade; h.CekDocEntry = k.DocEntry; h.CekBelge = k.Belge; h.PlanlananTedarikci = k.PlanlananTedarikci; h.PlanlamaTarihi = k.PlanlamaTarihi;
            h.PlanKaynak = k.PlanlananTedarikci == "" ? "" : (k.PlanDocEntry != null ? $"Kasa Nakit Akışı #{k.PlanDocEntry}" : "Çek Akıbeti") + (k.Planlayan != "" ? " · " + k.Planlayan : "");
            h.IbrazYeri = k.IbrazYeri; h.IbrazTuru = k.IbrazTuru; h.IbrazTarihi = k.IbrazTarihi; h.IbrazBelgeNo = k.IbrazBelgeNo; h.PlanKontrol = k.Kontrol;
        }

        // ---------- eski görünümün uçları (bellek içi örnek) ----------
        [HttpPost("SetOnayDurumu")] public IActionResult SetOnayDurumu([FromBody] R123_OnayPostModel model) { if (model != null) FinekraOrnek.HareketOnayla($"{model.DbKey}|{model.HesapTipi}|{model.IslemNo}", model.Durum ?? "", model.NotIcerigi, Kullanici); return Json(new { success = true, message = $"Kayıt {model?.IslemNo} için durum {model?.Durum} kaydedildi." }); }
        [HttpPost("SilManuelKayit")] public IActionResult SilManuelKayit([FromBody] R123_OnayPostModel model) => Json(new { success = true, message = "Kayıt kaldırıldı (ön yüz örneği)." });
        [HttpPost("CreateYevmiye")] public IActionResult CreateYevmiye([FromForm] R123_YevmiyePostModel model) { TempData["Success"] = "Yevmiye fişi oluşturuldu (ön yüz örneği)."; return RedirectToAction("Eski"); }
        [HttpGet("GenerateDekont/{dbKey}/{transId}")]
        public IActionResult GenerateDekont(string dbKey, int transId, string type) { OrnekDoldurucu.Hazirla(this); return PartialView("DekontView", new R123_DekontViewModel { IslemTarihi = DateTime.Today, Aciklama = "Ön yüz örneği dekontu · işlem " + transId, ParaBirimi = "TRY", Tutar = 125_000m, KarsiHesapKodu = "320.001", KarsiHesapAdi = "Öztürk Plastik", IslemTipi = type ?? "X" }); }
        [HttpGet("DekontIndir/{id}")] public IActionResult DekontIndir(string id, [FromQuery] string dbKey) => Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
        [HttpPost("AktarSap")] public IActionResult AktarSap([FromBody] R123_SapAktarimRequest request) => Json(new { success = true, message = "Belge SAP'ye aktarıldı (ön yüz örneği).", docNum = "SAP-" + DateTime.Now.ToString("HHmmss") });
        [HttpPost("CreateVirmanJournalEntry")] public IActionResult CreateVirmanJournalEntry([FromBody] R123_VirmanCreationRequest request) => Json(new { success = true, message = "Virman yevmiye kaydı oluşturuldu (ön yüz örneği)." });
        [HttpPost("EksikBildirimKaydet")] public IActionResult EksikBildirimKaydet([FromBody] EksikBildirimRequestModel model) => Json(new { success = true, message = "Bildirim kaydedildi (ön yüz örneği)." });
        [HttpGet("EksikBildirimleriGetir")]
        public IActionResult EksikBildirimleriGetir(string bolum, string sirketDb)
        {
            var farkli = CekAkibetOrnek.Kayitlar.Where(k => k.Kontrol == "Farklı").Take(8).Select((k, i) => new EksikBildirimViewModel { Id = i + 1, Bolum = "Finans", Sirket = k.Sirket, GonderenKullanici = "Kerem Aksoy", Baslik = $"{k.No} plana aykırı çıktı", Aciklama = $"Kasa Nakit Akışı planı {k.PlanlananTedarikci}; belge {k.IbrazYeri} yerine {k.IbrazTuru.ToLower(Tr)} edildi ({k.IbrazTarihi:dd.MM.yyyy}).", Maddeler = new List<string> { "Ciro / ibraz belgesini kontrol et", "Kasa Nakit Akışı planını düzelt" }, Durum = "Bekliyor", KayitTarihi = (k.IbrazTarihi ?? DateTime.Today).ToString("dd.MM.yyyy"), TamamlanmaTarihi = "" }).ToList();
            return Json(new { success = true, data = farkli });
        }
        [HttpPost("EksikBildirimTamamla/{id}")] public IActionResult EksikBildirimTamamla(int id) => Json(new { success = true, message = "Bildirim tamamlandı olarak işaretlendi (ön yüz örneği)." });
        [HttpGet("AraCari")] public IActionResult AraCari(string db, string q) { string s = (q ?? "").ToUpper(Tr); return Json(new { results = CekOrnek.TedarikciAdlari.Where(t => s == "" || t.ToUpper(Tr).Contains(s)).Take(20).Select((t, i) => new { id = "320." + (i + 1).ToString("000"), text = t }).ToList() }); }
    }
}
