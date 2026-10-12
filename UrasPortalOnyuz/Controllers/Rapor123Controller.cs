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
    /// KONSOLİDE FİNANSAL TAKİP PANELİ (Rapor123) — elle yazılmış; KASA / BANKA hareketleri Data/NakitAkisOrnek.cs ödemelerinden, ÇEK hareketleri Data/CekAkibetOrnek.cs kayıtlarından.
    /// ÇEK sekmesi: çek no, Kasa Nakit Akışı'nda planlanan tedarikçi, gerçek ibraz / ciro bilgisi ve KONTROL (Uyumlu / Farklı / İbraz Bekliyor / Plansız İbraz) — finans plana aykırı çıkan çekleri buradan izler.
    /// Üretilmiş sürüm Uretilen/Controllers/Rapor123Controller.cs içinde #if ile kapalı (R123_* model sınıfları orada).
    /// </summary>
    [Authorize]
    [Route("[controller]")]
    public class Rapor123Controller : Controller
    {
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        static int Seed(string s) => s.Aggregate(31, (a, ch) => unchecked(a * 31 + ch));

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(DateTime? startDate, DateTime? endDate, string hesapTipi = "KASA", string tab = "TUMU", string dbFilter = "TUMU")
        {
            hesapTipi = (hesapTipi ?? "KASA").ToUpperInvariant(); if (hesapTipi == "ÇEK") hesapTipi = "CEK"; tab = (tab ?? "TUMU").ToUpperInvariant();
            var bas = (startDate ?? DateTime.Today.AddDays(-30)).Date; var bit = (endDate ?? DateTime.Today).Date;
            var sirketler = CekAkibetOrnek.Sirketler.Where(s => dbFilter == "TUMU" || string.IsNullOrEmpty(dbFilter) || s.DbKey == dbFilter || s.DbName == dbFilter).ToList();
            var hepsi = new List<R123_HareketViewModel>();
            foreach (var s in sirketler) { hepsi.AddRange(Kasa(s, bas, bit)); hepsi.AddRange(Banka(s, bas, bit)); hepsi.AddRange(Cek(s, bas, bit)); }
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
            return View("Index", model);
        }

        // ---------- hareket üreticileri ----------
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
            { var h = Temel(s, "BANKA", m.DocEntry, m.U_OdenmeTarihi.Value, m.U_IslemTipi == "R" ? "R" : "X"); var banka = (m.U_BankaHesabi ?? "Ziraat Bankası").Split(" · ")[0]; h.KasaKodu = "102.01." + (Array.IndexOf(NakitAkisOrnek.BankaHesaplari, m.U_BankaHesabi) + 1).ToString("000"); h.KasaAdi = banka; h.KarsiHesapKodu = "320"; h.KarsiHesapAdi = m.U_Kurum; h.Alacak = m.U_OdenenTutar ?? m.U_TutarTL ?? 0; h.ParaBirimi = "TRY"; h.IslemYonu = "Çıkış"; h.Aciklama = $"{m.U_UrunHizmet} · {m.U_FaturaSiparisNo} · dekont {m.U_DekontNo} · alıcı IBAN {m.U_AliciIban}"; if (m.DekontListesi.Count == 0 && m.DocEntry % 7 == 0) { h.OnayDurumu = ""; h.OnaylayanKullaniciAdi = null; h.OnayTarihi = null; } yield return h; }
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
        /// <summary>Çek: portföye giriş (giriş) ve ibraz / ciro (çıkış) hareketleri; her satırda plan ↔ ibraz kontrolü. Planı, girişi ya da çıkışı aralıkta olan belgeler listelenir.</summary>
        static IEnumerable<R123_HareketViewModel> Cek((string Ad, string DbName, string DbKey) s, DateTime bas, DateTime bit)
        {
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

        // ---------- işlem uçları (bellek içi örnek) ----------
        [HttpPost("SetOnayDurumu")] public IActionResult SetOnayDurumu([FromBody] R123_OnayPostModel model) => Json(new { success = true, message = $"Kayıt {model?.IslemNo} için durum {model?.Durum} kaydedildi (ön yüz örneği; sayfa yenilenince örnek veri yeniden üretilir)." });
        [HttpPost("SilManuelKayit")] public IActionResult SilManuelKayit([FromBody] R123_OnayPostModel model) => Json(new { success = true, message = "Kayıt kaldırıldı (ön yüz örneği)." });
        [HttpPost("CreateYevmiye")] public IActionResult CreateYevmiye([FromForm] R123_YevmiyePostModel model) { TempData["Success"] = "Yevmiye fişi oluşturuldu (ön yüz örneği)."; return RedirectToAction("Index"); }
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
