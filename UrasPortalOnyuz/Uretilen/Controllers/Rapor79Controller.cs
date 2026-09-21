// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

using WebApplication3.Models;

namespace FinansRaporlama.Controllers
{




    public class NakitAkisViewModel
    {
        public int DocEntry { get; set; }
        public int? DocNum { get; set; }
        public int? U_Sira { get; set; }
        public DateTime? U_PlanlananTarih { get; set; }
        public DateTime? BelgeTarihi { get; set; }
        public string U_Kurum { get; set; }
        public string U_UrunHizmet { get; set; }
        public string U_ParaBirimi { get; set; }
        public decimal? U_Tutar { get; set; }
        public decimal? U_TutarTL { get; set; }
        public string U_OdemeYontemi { get; set; }
        public DateTime? U_OdenmeTarihi { get; set; }

        public decimal? U_OdenenTutar { get; set; }
        public string U_OdenenTutarPB { get; set; }
        public decimal? U_Kur { get; set; }

        public string U_Durum { get; set; }
        public string U_EskiDurum { get; set; } // HATA ÇÖZÜMÜ İÇİN EKLENDİ
        public string U_Notlar { get; set; }
        public string U_BagliBelgeTipi { get; set; }
        public int? U_BagliDocEntry { get; set; }
        public string U_Renk { get; set; }
        public string U_KayitTipi { get; set; }
        public string U_FaturaSiparisNo { get; set; }
        public string U_KaynakFirma { get; set; }
        public string U_BE1_DEKONT { get; set; }
        public string U_IslemTipi { get; set; }

        public string U_MutabakatDurumu { get; set; }
        public DateTime? SonMutabakatTarihi { get; set; }

        public string U_Satinalmaci { get; set; }


        public string MukerrerDocEntryler { get; set; }

        public int MukerrerKontrolEdildi { get; set; }





        public string MuafiyetSebebi { get; set; }



        public List<DekontKaydi> DekontListesi { get; set; } = new List<DekontKaydi>();
    }

    public class KasaBakiyeViewModel
    {
        public DateTime? GuncellenmeTarihi { get; set; }
        public string BankaKasa { get; set; }
        public string Detay { get; set; }
        public string PB { get; set; }
        public decimal Tutar { get; set; }
        public decimal TutarTL { get; set; }
    }

    public class ProjeksiyonItem
    {
        public string ParaBirimi { get; set; }
        public decimal MevcutBanka { get; set; }
        public decimal BekleyenNakitOdeme { get; set; }
        public decimal KalanBanka => MevcutBanka - BekleyenNakitOdeme;
        public decimal MevcutCek { get; set; }
        public decimal BekleyenCekOdeme { get; set; }
        public decimal KalanCek => MevcutCek - BekleyenCekOdeme;
    }

    public class CompanyDashboardViewModel
    {
        public string DbName { get; set; }
        public string DisplayName { get; set; }
        public List<NakitAkisViewModel> OdemeListesi { get; set; } = new List<NakitAkisViewModel>();
        public List<KasaBakiyeViewModel> KasaBakiyeleri { get; set; } = new List<KasaBakiyeViewModel>();

        public decimal ToplamTutar => OdemeListesi.Sum(x => x.U_TutarTL ?? x.U_Tutar ?? 0);
        public decimal ToplamOdenen => OdemeListesi.Sum(x => x.U_OdenenTutar ?? 0);
        public decimal KalanOdeme => ToplamTutar - ToplamOdenen;
        public decimal ToplamKasaMevcudu => KasaBakiyeleri.Sum(x => x.TutarTL);
        public decimal NakitAkisDurumu => ToplamKasaMevcudu - KalanOdeme;

        public List<ProjeksiyonItem> ProjeksiyonListesi
        {
            get
            {
                string Normalize(string c) => (c == "TL" || c == "TRY" || string.IsNullOrEmpty(c)) ? "TRY" : c;


                var kBakiyeler = (KasaBakiyeleri ?? new List<KasaBakiyeViewModel>()).Select(x => new { PB = Normalize(x.PB), BankaKasa = x.BankaKasa ?? "", Detay = x.Detay ?? "", x.Tutar }).ToList();
                var odemeler = (OdemeListesi ?? new List<NakitAkisViewModel>()).Where(x => x.U_Durum != "ÖDENDİ").Select(x => new { PB = Normalize(x.U_ParaBirimi), x.U_OdemeYontemi, Tutar = x.U_Tutar ?? 0 }).ToList();
                var currencies = kBakiyeler.Select(x => x.PB).Union(odemeler.Select(x => x.PB)).Distinct().ToList();
                var result = new List<ProjeksiyonItem>();
                foreach (var pb in currencies)
                {
                    var item = new ProjeksiyonItem { ParaBirimi = pb };
                    item.MevcutBanka = kBakiyeler.Where(x => x.BankaKasa == "BANKA" && x.PB == pb).Sum(x => x.Tutar);
                    item.BekleyenNakitOdeme = odemeler.Where(x => x.PB == pb && (x.U_OdemeYontemi == "Nakit" || x.U_OdemeYontemi == "Havale / EFT" || string.IsNullOrEmpty(x.U_OdemeYontemi))).Sum(x => x.Tutar);
                    item.MevcutCek = kBakiyeler.Where(x => x.Detay.ToUpper().Contains("ÇEK") && x.PB == pb).Sum(x => x.Tutar);
                    item.BekleyenCekOdeme = odemeler.Where(x => x.PB == pb && x.U_OdemeYontemi == "Çek").Sum(x => x.Tutar);
                    result.Add(item);
                }
                return result.OrderBy(x => x.ParaBirimi == "TRY" ? 0 : 1).ThenBy(x => x.ParaBirimi).ToList();
            }
        }
    }

    public class DekontKaydi
    {
        public int Id { get; set; }
        public string SirketDb { get; set; }
        public int DocEntry { get; set; }
        public string DosyaAdi { get; set; }
        public string DosyaYolu { get; set; }
        public string Aciklama { get; set; }
        public string Yukleyen { get; set; }
        public DateTime Tarih { get; set; }
    }

    public class KonsolideDashboardViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<CompanyDashboardViewModel> Companies { get; set; } = new List<CompanyDashboardViewModel>();
    }

    public class BelgeAramaViewModel
    {
        public string Firma { get; set; }
        public string DbName { get; set; }
        public int DocEntry { get; set; }
        public string BelgeTipi { get; set; }
        public string CardName { get; set; }
        public decimal DocTotalOrijinal { get; set; }
        public string ItemAcctName { get; set; }
        public decimal DocTotalTL { get; set; }
        public decimal OdenenTutar { get; set; }
        public string ParaBirimi { get; set; }
        public DateTime DocDate { get; set; }
        public string U_BE1_Aktar { get; set; }
        public string DocNum { get; set; }
        public string FaturaNo { get; set; }
        public string Satinalmaci { get; set; }
        public string DisplayText => $"[{Firma}] {BelgeTipi} | {CardName} | {DocTotalTL:N2} ₺ | {DocDate:dd.MM.yyyy}";
    }

    public class BankaDetayViewModel
    {
        public string BankaAdi { get; set; }
        public string ParaBirimi { get; set; }
        public decimal Bakiye { get; set; }
        public decimal GuncelTLTutari { get; set; }
        public DateTime? GuncellenmeTarihi { get; set; }
    }

    public class CekDetayViewModel
    {
        public string CekNumarasi { get; set; }
        public string VadeTarihi { get; set; }
        public string CekKimdenGeldi { get; set; }
        public string AsilBorclu { get; set; }
        public string BankaAdi { get; set; }
        public decimal CekTutari { get; set; }
        public decimal TLKarsiligi { get; set; }
        public string ParaBirimi { get; set; }
        public string BelgeTipi { get; set; }
    }

    public class OdenenBelgeDto
    {
        public string Firma { get; set; }
        public DateTime? OdenmeTarihi { get; set; }
        public string Kurum { get; set; }
        public string UrunHizmet { get; set; }
        public decimal? Tutar { get; set; }
        public decimal? TutarTL { get; set; }
        public decimal? Kur { get; set; }
        public string ParaBirimi { get; set; }
        public decimal? OdenenTutar { get; set; }
        public string OdenenTutarPB { get; set; }
        public string OdemeYontemi { get; set; }
        public string FaturaSiparisNo { get; set; }
        public string DekontLink { get; set; }
    }

    public class DetayliCekViewModel
    {
        public int DocEntry { get; set; }
        public string CekNumarasi { get; set; }
        public string BankaAdi { get; set; }
        public string Sube { get; set; }
        public string CekKimdenGeldi { get; set; }
        public string AsilBorclu { get; set; }
        public decimal CekTutari { get; set; }
        public string ParaBirimi { get; set; }
        public string VadeTarihi { get; set; }
        public string PortfoyeGirisTarihi { get; set; }
        public string BelgeTipi { get; set; }
        public string IslemTipiAdi { get; set; }
        public decimal TLKarsiligi { get; set; }
    }

    public class CekEkleRequest
    {
        public string DbName { get; set; }
        public List<int> SecilenCekDocEntryListesi { get; set; }
    }




    [Authorize]
    [Route("[controller]")]
    public class Rapor79Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor79Controller> _logger;
        private readonly IWebHostEnvironment _env;

        private readonly List<(string DbName, string DisplayName)> _hedefSirketler = new List<(string, string)>
        {
            ("URASKIMYA", "URAS KİMYA"),
            ("URSMAKINE", "URS MAKİNE"),
            ("ALV_KIMYA", "ALV KİMYA"),
            ("AVRUPA_PAPER", "AVRUPA PAPER"),
            ("SELVI", "SELVİ KİMYA"),
            ("DRN", "DRN"),
            ("ALVFILO", "ALV FİLO"),
            ("URAS_HOLDING", "URAS HOLDİNG")
        };

        private string GetDbConnectionString(string dbName)
 {return default;
}

        private async Task<string> GetSapUserNameAsync(string dbName, string userCode)
 {return default;
}

        private async Task LogIslemAsync(string dbName, string islemTipi, int? docEntry, string aciklama)
 {}










        private static int _mukerrerSemasiHazir;
        private string MerkezDbAdi()
 {return default;
}
        private void MukerrerTablosunuHazirla()
 {}


        private static string NotTemizle(string not)
 {return default;
}


        [HttpGet("MukerrerDetay")]
        public async Task<IActionResult> MukerrerDetay(string dbName, string docEntries)
 {return Json(new { success = true, data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { docEntry = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("docEntry", i2), faturaNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("faturaNo", i2), kurum = global::WebApplication3.OrnekDoldurucu.Deger<string>("kurum", i2), urun = global::WebApplication3.OrnekDoldurucu.Deger<string>("urun", i2), planlanan = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("planlanan", i2), tutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("tutar", i2), pb = global::WebApplication3.OrnekDoldurucu.Deger<string>("pb", i2), tutarTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("tutarTL", i2), durum = global::WebApplication3.OrnekDoldurucu.Deger<string>("durum", i2), odenmeTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("odenmeTarihi", i2), odenenTutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("odenenTutar", i2), kaynak = global::WebApplication3.OrnekDoldurucu.Deger<string>("kaynak", i2), satinalmaci = global::WebApplication3.OrnekDoldurucu.Deger<string>("satinalmaci", i2), notlar = global::WebApplication3.OrnekDoldurucu.Deger<string>("notlar", i2), kayitTipi = global::WebApplication3.OrnekDoldurucu.Deger<string>("kayitTipi", i2), belgeTipi = global::WebApplication3.OrnekDoldurucu.Deger<string>("belgeTipi", i2), odemeYontemi = global::WebApplication3.OrnekDoldurucu.Deger<string>("odemeYontemi", i2), mutabakat = global::WebApplication3.OrnekDoldurucu.Deger<string>("mutabakat", i2) }).ToList(), kararlar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { docEntry = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("docEntry", i2), karar = global::WebApplication3.OrnekDoldurucu.Deger<string>("karar", i2), asilDocEntry = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("asilDocEntry", i2), kullanici = global::WebApplication3.OrnekDoldurucu.Deger<string>("kullanici", i2), tarih = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("tarih", i2), not = global::WebApplication3.OrnekDoldurucu.Deger<string>("not", i2) }).ToList() });
}

        public class MukerrerKararIstek { public string DbName { get; set; } public int DocEntry { get; set; } public string Karar { get; set; } public int? AsilDocEntry { get; set; } public string Digerler { get; set; } public string Not { get; set; } }





        [HttpPost("MukerrerKarar")]
        public async Task<IActionResult> MukerrerKarar([FromBody] MukerrerKararIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private async Task<(bool isDuplicate, string errorMessage)> CheckDuplicateFaturaSiparisNoAsync(string dbName, string faturaSiparisNo, decimal? tutar, int currentDocEntry)
 {return default;
}

        private void SendPlanlandiMail(string dbName, NakitAkisViewModel model)
 {}

        [HttpGet("GetRowLogs")]
        public async Task<IActionResult> GetRowLogs(string dbName, int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "LogId", "IslemTarihi", "Kullanici", "IslemTipi", "Aciklama" }) });
}

        [HttpGet("Index")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
 {ViewBag.GizliKolonlar = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<FinansRaporlama.Controllers.KonsolideDashboardViewModel>());
}





        private async Task<string> KullaniciTercihOkuAsync(string anahtar)
 {return default;
}














        private static int _dekontSemasiHazir;

        private async Task DekontlariYukleAsync(KonsolideDashboardViewModel model)
 {}


        private async Task DekontKaydetAsync(string dbName, int docEntry, string dosyaAdi, string dosyaYolu, string aciklama)
 {}

        private static int _muafiyetSemasiHazir;

        private class MuafiyetKaydi
        {
            public string SirketDb { get; set; }
            public int DocEntry { get; set; }
            public string Sebep { get; set; }
        }

        private async Task MuafiyetleriYukleAsync(KonsolideDashboardViewModel model)
 {}


        private static readonly string[] MuafiyetSebepleri =
        {
            "Vergi ödemesi",
            "Kontör / Yakıt / Sertifika",
            "Fatura (Elektrik/Su/Doğalgaz)",
            "Diğer"
        };

        public class MuafiyetPostModel
        {
            public string DbName { get; set; }
            public int DocEntry { get; set; }
            public string Sebep { get; set; }
        }

        [HttpPost("MuafiyetKaydet")]
        public async Task<IActionResult> MuafiyetKaydet([FromBody] MuafiyetPostModel model)
 {return Json(new { success = true, sebep = "" });
}

        private async Task<CompanyDashboardViewModel> GetCompanyDataAsync(string dbName, string displayName, DateTime start, DateTime end)
 {return default;
}

        [HttpGet("GetSistemLoglari")]
        public async Task<IActionResult> GetSistemLoglari(string dbName, DateTime? startDate, DateTime? endDate, bool sadeceSilinenler = false)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "LogId", "IslemTarihi", "Kullanici", "IslemTipi", "Aciklama", "DocEntry" }) });
}

        [HttpPost("AjaxRowGuncelle")]
        public async Task<IActionResult> AjaxRowGuncelle([FromBody] NakitAkisViewModel model, [FromQuery] string dbName)
 {return Json(new { success = true });
}

        [HttpPost("TopluGuncelle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopluGuncelle(string DbName, List<NakitAkisViewModel> OdemeListesi, DateTime? startDate, DateTime? endDate)
 {return RedirectToAction("Index");
}

        [HttpPost("Guncelle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Guncelle(string DbName, NakitAkisViewModel model, DateTime? startDate, DateTime? endDate)
 {return RedirectToAction("Index");
}

        [HttpPost("Ekle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(string DbName, NakitAkisViewModel model, DateTime? startDate, DateTime? endDate)
 {return RedirectToAction("Index");
}

        [HttpPost("Sil")]
        public async Task<IActionResult> Sil(string dbName, int docEntry)
 {return Json(new { success = true });
}

        [HttpGet("GetOdenenler")]
        public async Task<IActionResult> GetOdenenler(DateTime start, DateTime end, string search)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.OdenenBelgeDto>(12));
}

        [HttpGet("GetGuncelKur")]
        public async Task<IActionResult> GetGuncelKur(string dbName, string pb)
 {return Json(1.0);
}

        [HttpGet("GetCariListesi")]
        public async Task<IActionResult> GetCariListesi(string term, string dbName)
 {return Json(global::System.Linq.Enumerable.Range(0, 12).Select(i1 => global::WebApplication3.OrnekDoldurucu.Deger<string>("", i1)).ToList());
}

        [HttpGet("GetCariSirketleri")]
        public async Task<IActionResult> GetCariSirketleri(string cardName)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "1" }));
}

        [HttpGet("GetBelgelerForCari")]
        public async Task<IActionResult> GetBelgelerForCari(string dbName, string cardName, string belgeTipi)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.BelgeAramaViewModel>(12));
}

        [HttpGet("BelgeAra")]
        public async Task<IActionResult> BelgeAra(string term)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.BelgeAramaViewModel>(12));
}

        [HttpGet("GetSatinalmacilar")]
        public async Task<IActionResult> GetSatinalmacilar(string dbName)
 {return Json(global::System.Linq.Enumerable.Range(0, 12).Select(i1 => global::WebApplication3.OrnekDoldurucu.Deger<string>("", i1)).ToList());
}

        [HttpPost("UploadDekont")]
        public async Task<IActionResult> UploadDekont(IFormFile dekontDosya, string dbName, int docEntry, string secilenMail, string dekontAciklama = null)
 {return Json(new { success = true, message = "Dekont başarıyla yüklendi.", filePath = global::WebApplication3.OrnekDoldurucu.Deger<string>("filePath", 0) });
}

        [HttpPost("UploadTopluDekont")]
        public async Task<IActionResult> UploadTopluDekont(IFormFile dekontDosya, string dbName, string docEntries, string secilenMail)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), filePath = global::WebApplication3.OrnekDoldurucu.Deger<string>("filePath", 0) });
}

        private void SendDekontMail(string toEmail, string dbName, int docEntry, string kurumAdi, string attachmentPhysicalPath, string attachmentName)
 {}

        [HttpPost("DeleteDekont")]
        public async Task<IActionResult> DeleteDekont(string dbName, int docEntry, int dekontId = 0)
 {return Json(new { success = true, message = "Dekont silindi." });
}

        [HttpGet("GetBankaDetaylari")]
        public async Task<IActionResult> GetBankaDetaylari(string dbName, string pb)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.BankaDetayViewModel>(12) });
}

        [HttpGet("GetCekDetaylari")]
        public async Task<IActionResult> GetCekDetaylari(string dbName, string detay, string pb)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.CekDetayViewModel>(12) });
}

        [HttpGet("GetBelgeAltDetay")]
        public async Task<IActionResult> GetBelgeAltDetay(string kaynakDbName, string belgeTipi, int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocNum", "FaturaNo", "ParaBirimi", "ItemCode", "KalemAdi", "Miktar", "KdvsizTutar", "KdvTutari", "KdvliTutar", "UUID" }) });
}

        [HttpGet("GetCompanyBankAccounts")]
        public async Task<IActionResult> GetCompanyBankAccounts(string dbName)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "BankName", "Iban", "ParaBirimi", "BranchName", "AccountNumber" }));
}

        [HttpGet("GetSuppliers")]
        public async Task<IActionResult> GetSuppliers(string dbName)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName" }));
}

        [HttpGet("GetSupplierBankInfo")]
        public async Task<IActionResult> GetSupplierBankInfo(string dbName, string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Iban", "BankName" }));
}

        [HttpGet("GetPortfolioChecks")]
        public async Task<IActionResult> GetPortfolioChecks(string dbName)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "CekNumarasi", "CiroEden", "AsilBorclu", "Tutar", "ParaBirimi", "VadeTarihi", "VadeAyiVeYili", "PortfoyeGirisTarihi", "HareketDurumu", "BankaAdi" }));
}

        [HttpGet("GetPortfoyCekleriDetayli")]
        public async Task<IActionResult> GetPortfoyCekleriDetayli(string dbName)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.DetayliCekViewModel>(12) });
}

        [HttpPost("TopluCekleriAkisaEkle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TopluCekleriAkisaEkle([FromBody] CekEkleRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private string GetPortfoyCekleriSqlStringi(bool filterById = false)
 {return default;
}
    }
}