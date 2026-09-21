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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text;
using System.Data;
using System.Globalization;
using System.Net.Mime;
using ClosedXML.Excel;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor55Controller : Controller
    {
        private readonly IConfiguration _configuration;


        private readonly List<DatabaseConfig> _allDatabases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },
                        new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
                                                new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S" },

                                                new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S" }
        };


        public IActionResult Index(DateTime? startDate, DateTime? endDate, List<string> selectedDbs, string searchDesc)
 {ViewBag.AllDatabases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.DatabaseConfig>(12);
ViewBag.SelectedDbs = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.ConsolidatedFaturaViewModel>(12));
}


        [HttpGet("ExportExcel")]
        public IActionResult ExportExcel(DateTime? startDate, DateTime? endDate, List<string> selectedDbs, string searchDesc)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}


        [HttpGet("downloadpdf")]
        public IActionResult DownloadPdf(DateTime? startDate, DateTime? endDate, List<string> selectedDbs, string searchDesc)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}


        private List<ConsolidatedFaturaViewModel> GetFilteredConsolidatedData(DateTime? startDate, DateTime? endDate, List<string> selectedDbs, string searchDesc)
 {return default;
}

        private List<ConsolidatedFaturaViewModel> FetchFaturaFromDb(string connStr, string dbName, DateTime start, DateTime end, string searchDesc)
 {return default;
}
    }

    public class ConsolidatedFaturaViewModel
    {
        public string VeritabanıKaynağı { get; set; }
        public DateTime FaturaTarihi { get; set; }
        public string TedarikciAdi { get; set; }
        public string Kod { get; set; }
        public string Aciklama { get; set; }
        public decimal Miktar { get; set; }
        public string BirimCinsi { get; set; }
        public decimal Fiyat { get; set; }
        public decimal VergiHaricTutar { get; set; }
        public decimal KdvTutari { get; set; }
        public decimal KdvOrani { get; set; }
        public string FaturaRefNo { get; set; }
    }
}