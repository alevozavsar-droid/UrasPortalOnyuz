// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using ClosedXML.Excel;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{

    public class ChildFirstHesapComparer91 : IComparer<string>
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

    [Authorize]
    [Route("[controller]")]
    public class Rapor91Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S" },
            new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        [HttpGet]
        public IActionResult Index(DateTime? startDate, DateTime? endDate)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.HesapList = WebApplication3.OrnekDoldurucu.Liste<HesapViewModel>(12);
ViewBag.MainHesapList = WebApplication3.OrnekDoldurucu.Liste<HesapViewModel>(12);
ViewBag.HesapListWithLevels = WebApplication3.OrnekDoldurucu.Yeni<System.Collections.Generic.Dictionary<string, int>>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor91ViewModel>());
}

        [HttpPost]
        public IActionResult Index(Rapor91ViewModel model, IFormFile excelFile)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.HesapList = WebApplication3.OrnekDoldurucu.Liste<HesapViewModel>(12);
ViewBag.MainHesapList = WebApplication3.OrnekDoldurucu.Liste<HesapViewModel>(12);
ViewBag.HesapListWithLevels = WebApplication3.OrnekDoldurucu.Yeni<System.Collections.Generic.Dictionary<string, int>>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor91ViewModel>());
}

        private void PrepareViewBags(Rapor91ViewModel model, List<HesapViewModel> allHesapList, string dbKey)
 {}



        private string BasicClean(string code)
 {return default;
}

        private string CleanAccountCode(string code)
 {return default;
}

        private string GetGroupPrefix(string code)
 {return default;
}

        private List<HesapOzeti> ParseExcelFile(IFormFile file)
 {return default;
}

        private List<HesapViewModel> GetAllHesapList(string connectionString)
 {return default;
}

        private HashSet<string> GetAccountHierarchy(string accountCode, List<HesapViewModel> allAccounts)
 {return default;
}


        private List<HesapOzeti> GetSapMizanDataTRY(string connectionString, DateTime startDate, DateTime endDate, List<string> selectedAktarimTipi, List<string> hesapKodlari)
 {return default;
}


        private List<HesapOzetiDovizli> GetSapMizanDataDovizli(string connectionString, DateTime startDate, DateTime endDate, List<string> selectedAktarimTipi, List<string> hesapKodlari)
 {return default;
}

        private List<HesapOzeti> CalculateParentBalances(List<HesapOzeti> postableData, List<HesapViewModel> allHesapList, HashSet<string> accountsToProcess)
 {return default;
}

        private List<HesapOzetiDovizli> CalculateParentBalancesDovizli(List<HesapOzetiDovizli> postableData, List<HesapViewModel> allHesapList, HashSet<string> accountsToProcess)
 {return default;
}


    }


    public class Rapor91KiyaslamaSonucu
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public int Level { get; set; }
        public string Postable { get; set; }
        public decimal SapBorc { get; set; }
        public decimal SapAlacak { get; set; }
        public decimal SapBakiye => SapBorc - SapAlacak;
        public decimal ExcelBorc { get; set; }
        public decimal ExcelAlacak { get; set; }
        public decimal ExcelBakiye => ExcelBorc - ExcelAlacak;
        public decimal FarkBorc => SapBorc - ExcelBorc;
        public decimal FarkAlacak => SapAlacak - ExcelAlacak;
        public decimal FarkBakiye => SapBakiye - ExcelBakiye;
        public bool FarkVarmi => Math.Abs(FarkBakiye) > 0.01m;
    }

    public class Rapor91ViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<string> AktarimTipi { get; set; } = new List<string>();
        public List<string> AnaHesap { get; set; } = new List<string>();
        public List<string> HesapKodlari { get; set; } = new List<string>();
        public bool KebirMizan { get; set; }
        public bool IncludeZeroBalance { get; set; }
        public string BakiyeTipi { get; set; }
        public bool SadeceFarklariGoster { get; set; }
        public string Message { get; set; }
        public bool IsSuccess { get; set; }
        public List<Rapor91KiyaslamaSonucu> KiyaslamaListesi { get; set; } = new List<Rapor91KiyaslamaSonucu>();
    }
}