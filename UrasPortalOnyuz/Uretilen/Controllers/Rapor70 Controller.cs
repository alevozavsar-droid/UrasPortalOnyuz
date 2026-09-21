// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WebApplication3.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Data;
using ClosedXML.Excel;
using System.IO;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor70Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _context;



        private static readonly List<(string Code, string Label, string SrcKey)> _sirketCiftleri = new List<(string, string, string)>
        {
            ("ALV",        "ALV KİMYA - ALV A.Ş.",        "DefaultConnection3"),
            ("ALV2026",    "ALV KİMYA - ALV 2026",        "DefaultConnection3"),
            ("URS",        "URS MAKİNE - URS A.Ş.",       "DefaultConnection1"),
            ("URS2026",    "URS MAKİNE - URS 2026",       "DefaultConnection1"),
            ("HOLDING",    "URAS HOLDİNG - HOLDİNG A.Ş.", "DefaultConnection10"),
            ("HOLDING2026","URAS HOLDİNG - HOLDİNG 2026", "DefaultConnection10"),
            ("DAF",        "DAF KİMYA - DAF A.Ş.",        "DefaultConnection4"),
            ("DAF2026",    "DAF KİMYA - DAF 2026",        "DefaultConnection4"),
            ("SELVI",      "SELVİ - SELVİ A.Ş.",          "DefaultConnection5"),
            ("SELVI2026",  "SELVİ - SELVİ 2026",          "DefaultConnection5"),
            ("AVRUPA",     "AVRUPA PAPER - AVRUPA A.Ş.",  "DefaultConnection2"),
            ("AVRUPA2026", "AVRUPA PAPER - AVRUPA 2026",  "DefaultConnection2"),
            ("FILO",       "ALV FİLO - FİLO A.Ş.",        "DefaultConnection6"),
            ("FILO2026",   "ALV FİLO - FİLO 2026",        "DefaultConnection6"),
            ("URAS",       "URAS KİMYA - URAS KİMYA A.Ş.","DefaultConnection"),
            ("BASKI2026",  "URAS BASKI - URAS BASKI 2026","DefaultConnection26"),
            ("AVRASYA2026","AVRASYA - AVRASYA 2026",      "DefaultConnection7"),
        };


        private List<(string Code, string Label)> YetkiliSirketCiftleri()
 {return default;
}




        public class HesapViewModel
        {
            public string HesapKodu { get; set; }
            public string HesapAdi { get; set; }
            public int Level { get; set; }
            public bool IsPostable { get; set; }
            public string ParentKodu { get; set; }

        }

        public class RawDataModel
        {
            public string HesapKodu { get; set; }
            public string DovizCinsi { get; set; }
            public decimal TLBorc { get; set; }
            public decimal TLAlacak { get; set; }
            public decimal DovizBorc { get; set; }
            public decimal DovizAlacak { get; set; }
        }

        public class HesapOzeti
        {
            public string HesapKodu { get; set; }
            public string HesapAdi { get; set; }
            public decimal ToplamBorc { get; set; }
            public decimal ToplamAlacak { get; set; }
            public decimal BorcBakiye => (ToplamBorc - ToplamAlacak) > 0 ? (ToplamBorc - ToplamAlacak) : 0;
            public decimal AlacakBakiye => (ToplamAlacak - ToplamBorc) > 0 ? (ToplamAlacak - ToplamBorc) : 0;
            public decimal NetBakiye => BorcBakiye - AlacakBakiye;
            public int Level { get; set; }
            public bool IsPostable { get; set; }
        }

        public class ChildFirstHesapComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (string.Equals(x, y)) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                string xKey = x.Trim() + "~";
                string yKey = y.Trim() + "~";

                return string.Compare(xKey, yKey, StringComparison.Ordinal);
            }
        }


        private (string ConnSource, string ConnTarget, string NameSource, string NameTarget) GetCompanySettings(string code)
 {return default;
}



        [HttpGet]
        public IActionResult Index(DateTime? startDate, DateTime? endDate, [FromQuery] List<string> aktarimTipi, string bakiyeTipi = "TRY", bool sadeceFarklar = false, string companyCode = "ALV")
 {ViewBag.SadeceFarklar = false;
ViewBag.SelectedAktarimTipi = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.BakiyeTipi = "";
ViewBag.StartDate = "";
ViewBag.EndDate = "";
ViewBag.CompanyCode = "";
ViewBag.SourceName = "";
ViewBag.TargetName = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Models.MizanKarsilastirmaViewModel>());
}

        [HttpGet("GetMuavinKarsilastirma")]
        public IActionResult GetMuavinKarsilastirma(string hesapKodu, DateTime startDate, DateTime endDate, [FromQuery] List<string> aktarimTipi, string bakiyeTipi, string companyCode = "ALV")
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.MuavinEslesmeRow>(12) });
}

        [HttpGet("DownloadExcel")]
        public IActionResult DownloadExcel(DateTime? startDate, DateTime? endDate, bool sadeceFarklar = false, [FromQuery] List<string> aktarimTipi = null, string bakiyeTipi = "TRY", string companyCode = "ALV")
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}





        private Dictionary<string, HesapViewModel> GetAllHesapList(string connectionString)
 {return default;
}

        private List<RawDataModel> GetMizanDataRaw(string connectionString, DateTime startDate, DateTime endDate, List<string> aktarimTipleri, string specialRule)
 {return default;
}

        private string[] OlusturParentZinciri(string code, Dictionary<string, HesapViewModel> tumHesaplar)
 {return default;
}

        private Dictionary<string, HesapOzeti> CalculateHierarchy(List<RawDataModel> rawData, Dictionary<string, HesapViewModel> tumHesaplar, string bakiyeTipi)
 {return default;
}

        private List<KarsilastirmaliMuavinRow> GetMuavinRows(string connStr, string hesapKodu, DateTime sDate, DateTime eDate, List<string> akt, string bakiyeTipi, string specialRule = "")
 {return default;
}


    }
}