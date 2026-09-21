// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;

namespace FinansRaporlama.Controllers
{




    public class NakitAkis130ViewModel
    {
        public int DocEntry { get; set; }
        public int? DocNum { get; set; }
        public int? U_Sira { get; set; }
        public DateTime? U_PlanlananTarih { get; set; }
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
        public string U_Notlar { get; set; }
        public string U_BagliBelgeTipi { get; set; }
        public int? U_BagliDocEntry { get; set; }
        public string U_Renk { get; set; }
        public string U_KayitTipi { get; set; }
        public string U_FaturaSiparisNo { get; set; }
        public string U_KaynakFirma { get; set; }
        public string U_BE1_DEKONT { get; set; }
        public string U_BE1_TALIMAT { get; set; }
        public string U_IslemTipi { get; set; }

        public string U_MutabakatDurumu { get; set; }


        public DateTime? SonMutabakatTarihi { get; set; }

        public string U_BelgeOlusturanUserCode { get; set; }
        public string U_BelgeSatinalmaci { get; set; }
    }

    public class KasaBakiye130ViewModel
    {
        public DateTime? GuncellenmeTarihi { get; set; }
        public string BankaKasa { get; set; }
        public string Detay { get; set; }
        public string PB { get; set; }
        public decimal Tutar { get; set; }
        public decimal TutarTL { get; set; }
    }

    public class Projeksiyon130Item
    {
        public string ParaBirimi { get; set; }
        public decimal MevcutBanka { get; set; }
        public decimal BekleyenNakitOdeme { get; set; }
        public decimal KalanBanka => MevcutBanka - BekleyenNakitOdeme;
        public decimal MevcutCek { get; set; }
        public decimal BekleyenCekOdeme { get; set; }
        public decimal KalanCek => MevcutCek - BekleyenCekOdeme;
    }

    public class CompanyDashboard130ViewModel
    {
        public string DbName { get; set; }
        public string DisplayName { get; set; }
        public List<NakitAkis130ViewModel> OdemeListesi { get; set; } = new List<NakitAkis130ViewModel>();
        public List<KasaBakiye130ViewModel> KasaBakiyeleri { get; set; } = new List<KasaBakiye130ViewModel>();

        public decimal ToplamTutar => OdemeListesi.Sum(x => x.U_TutarTL ?? x.U_Tutar ?? 0);
        public decimal ToplamOdenen => OdemeListesi.Sum(x => x.U_OdenenTutar ?? 0);
        public decimal KalanOdeme => ToplamTutar - ToplamOdenen;
        public decimal ToplamKasaMevcudu => KasaBakiyeleri.Sum(x => x.TutarTL);
        public decimal NakitAkisDurumu => ToplamKasaMevcudu - KalanOdeme;

        public List<Projeksiyon130Item> ProjeksiyonListesi
        {
            get
            {
                string Normalize(string c) => (c == "TL" || c == "TRY" || string.IsNullOrEmpty(c)) ? "TRY" : c;
                var kBakiyeler = KasaBakiyeleri.Select(x => new { PB = Normalize(x.PB), x.BankaKasa, x.Detay, x.Tutar }).ToList();
                var odemeler = OdemeListesi.Where(x => x.U_Durum != "ÖDENDİ").Select(x => new { PB = Normalize(x.U_ParaBirimi), x.U_OdemeYontemi, Tutar = x.U_Tutar ?? 0 }).ToList();
                var currencies = kBakiyeler.Select(x => x.PB).Union(odemeler.Select(x => x.PB)).Distinct().ToList();
                var result = new List<Projeksiyon130Item>();
                foreach (var pb in currencies)
                {
                    var item = new Projeksiyon130Item { ParaBirimi = pb };
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

    public class KonsolideDashboard130ViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ActiveViewRole { get; set; }
        public List<CompanyDashboard130ViewModel> Companies { get; set; } = new List<CompanyDashboard130ViewModel>();
    }

    public class OdenenBelge130Dto
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
        public string TalimatLink { get; set; }
    }

    public class Rapor130UpdateRequest
    {
        public string DbName { get; set; }
        public int DocEntry { get; set; }
        public string MutabakatDurumu { get; set; }
        public string YeniNot { get; set; }
        public string TamNot { get; set; }
    }




    [Authorize]
    [Route("[controller]")]
    public class Rapor130Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor130Controller> _logger;

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


        private readonly string[] _muhUsers = new[]
        {
            "muh45",
            "muh47",
            "kullanici@ornek.local",
            "seda.isik",
            "kullanici@ornek.local",
            "kullanici@ornek.local",
            "canan.su",
            "canansu"
        };








        private string EtkinRol(string currentUserName, string viewRole, out bool isSuperUser)
 {isSuperUser = default;
return default;
}

        private bool MutabakatDegistirebilirMi(string currentUserName)
 {return default;
}

        private string GetDbConnectionString(string dbName)
 {return default;
}

        private async Task<string> GetSapUserNameAsync(string dbName, string userCode)
 {return default;
}

        private async Task LogIslemAsync(string dbName, string islemTipi, int? docEntry, string aciklama)
 {}

        [HttpGet("Index")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string viewRole)
 {ViewBag.CurrentUserName = "";
ViewBag.IsSuperUser = false;
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<FinansRaporlama.Controllers.KonsolideDashboard130ViewModel>());
}

        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportExcel(DateTime? startDate, DateTime? endDate, string viewRole)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private async Task<CompanyDashboard130ViewModel> GetCompanyDataAsync(string dbName, string displayName, DateTime start, DateTime end, string effectiveRole)
 {return default;
}

        [HttpPost("UpdateMutabakatVeNot")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateMutabakatVeNot([FromBody] Rapor130UpdateRequest request)
 {return Json(new { success = true, eklenenNot = global::WebApplication3.OrnekDoldurucu.Deger<string>("eklenenNot", 0) });
}






        [HttpGet("GetRowLogs")]
        public async Task<IActionResult> GetRowLogs(string dbName, int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "LogId", "IslemTarihi", "Kullanici", "IslemTipi", "Aciklama" }) });
}

        [HttpGet("GetSistemLoglari")]
        public async Task<IActionResult> GetSistemLoglari(string dbName, DateTime? startDate, DateTime? endDate, bool sadeceSilinenler = false)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "LogId", "IslemTarihi", "Kullanici", "IslemTipi", "Aciklama", "DocEntry" }) });
}

        [HttpGet("GetOdenenler")]
        public async Task<IActionResult> GetOdenenler(DateTime start, DateTime end, string search, string viewRole)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.OdenenBelge130Dto>(12));
}

        [HttpGet("GetBelgeAltDetay")]
        public async Task<IActionResult> GetBelgeAltDetay(string kaynakDbName, string belgeTipi, int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocNum", "FaturaNo", "ParaBirimi", "ItemCode", "KalemAdi", "Miktar", "KdvsizTutar", "KdvTutari", "KdvliTutar", "UUID" }) });
}
    }
}