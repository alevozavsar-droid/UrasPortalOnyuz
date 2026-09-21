// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor39Controller : Controller
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
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
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
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };



        private string GetSelectedDatabase()
 {return default;
}

        private List<CariViewModel> GetAccountList(string connectionString)
 {return default;
}

        private List<string> GetAktarimTipiList(string connectionString)
 {return default;
}

        private string GetAccountName(string connectionString, string acctCode)
 {return default;
}

        private List<string> GetLeafAccounts(string connectionString, List<string> selectedCodes)
 {return default;
}





        [HttpGet]
        public IActionResult Index(List<string> bpCode, DateTime? startDate, DateTime? endDate,
                                   [FromQuery] List<string> aktarimTipi, bool includeInitialBalance = false,
                                   string yevIptal = "All", bool detayliAciklama = true)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.SelectedYevIptal = "";
ViewBag.SelectedBpCodes = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.SelectedAktarimTipi = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.CariList = WebApplication3.OrnekDoldurucu.Liste<CariViewModel>(12);
ViewBag.AktarimTipiList = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.CariEkstreViewModel>(12));
}



















        private static readonly ConcurrentDictionary<string, bool> _udfVarMi =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private bool UdfVarMi(string connectionString, string tablo, string kolon)
 {return default;
}


        private string UdfIfadesi(string connectionString, string tablo, string kolon, string takmaAd)  {return default;
}

        private List<CariEkstreViewModel> GetCariEkstreData(string connectionString, string acctCode, DateTime? startDate, DateTime? endDate, List<string> aktarimTipi, bool includeInitialBalance, string exclusionKeyword = null, string yevIptal = "All", bool detayliAciklama = true)
 {return default;
}





        [HttpGet("downloadpdf")]
        [Authorize]
        public IActionResult DownloadPdf(List<string> bpCode, DateTime? startDate, DateTime? endDate,
                                         [FromQuery] List<string> aktarimTipi, bool includeInitialBalance = false,
                                         string orientation = "Portrait", string yevIptal = "All", bool detayliAciklama = true)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}





        [HttpGet("downloadexcel")]
        [Authorize]
        public IActionResult DownloadExcel(List<string> bpCode, DateTime? startDate, DateTime? endDate,
                                           [FromQuery] List<string> aktarimTipi, bool includeInitialBalance = false,
                                           string yevIptal = "All", bool detayliAciklama = true)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

    }
}