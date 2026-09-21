// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using ClosedXML.Excel;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor106Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor106Controller> _logger;

        private readonly List<DatabaseConfig> _allDatabases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA", DbName = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TESTURASKIMYA", DbName = "TESTURASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS", DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS", DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS", DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S", DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S", DbName = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS", DbName = "TESTURASKIMYA_A.SS" }
        };

        private List<DatabaseConfig> _databases {get {return default;
}
}
        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.CurrentDbDisplay = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetItemsList")]
        public IActionResult GetItemsList()
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName" }));
}

        [HttpGet("DownloadTemplate")]
        public IActionResult DownloadTemplate()
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpPost("UploadExcel")]
        public IActionResult UploadExcel(IFormFile file)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor106Controller.ExcelRowModel>(12) });
}

        [HttpPost("SimulateProduction")]
        public IActionResult SimulateProduction([FromBody] SimulationRequestPayload payload)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor106Controller.SimulationResultModel>(12), warnings = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("warnings", i2)).ToList() });
}
        [HttpPost("UpdateMuadil")]
        public IActionResult UpdateMuadil(string itemCode, string muadilCode1, string muadilCode2, string muadilCode3)
 {return Json(new { success = true, message = "Muadil ürünler (3 adet) başarıyla SAP kartına işlendi." });
}

        [HttpPost("ExecuteProduction")]
        public async Task<IActionResult> ExecuteProduction([FromBody] ExecutionRequestPayload payload)
 {return Json(new { success = true, hasErrors = true, successfulItems = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("successfulItems", i2)).ToList(), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}



        [HttpGet("GetErroneousOrders")]
        public IActionResult GetErroneousOrders()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor106Controller.ErroneousOrderModel>(12) });
}

        [HttpPost("CancelOrders")]
        public async Task<IActionResult> CancelOrders([FromBody] List<int> prodDocEntries)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private string ParseSLError(string jsonResponse)
 {return default;
}



        public class ErroneousOrderModel
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public string PostDate { get; set; }
            public string ItemCode { get; set; }
            public decimal PlannedQty { get; set; }
        }

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class ExcelRowModel { public string ItemCode { get; set; } public decimal TargetQty { get; set; } }

        public class SimulationRequestPayload
        {
            public string SelectedDate { get; set; }
            public List<ExcelRowModel> Items { get; set; }
        }

        public class ExecutionRequestPayload
        {
            public string SelectedDate { get; set; }
            public List<ProdExecutionModel> Requests { get; set; }
        }

        private class BomLineTempModel
        {
            public string ParentCode { get; set; }
            public string ChildCode { get; set; }
            public string ChildName { get; set; }
            public decimal BaseQty { get; set; }
            public string WhsCode { get; set; }
            public string Muadil1 { get; set; }
            public string Muadil2 { get; set; }
            public string Muadil3 { get; set; }
        }

        private class CandidateStockInfo
        {
            public string ItemCode { get; set; }
            public bool IsOriginal { get; set; }
            public decimal AvailableStock { get; set; }
        }

        public class SimulationResultModel
        {
            public string ParentCode { get; set; }
            public decimal TargetQty { get; set; }
            public string ChildCode { get; set; }
            public string ChildName { get; set; }
            public decimal TotalRequiredQty { get; set; }
            public decimal BaseQty { get; set; }
            public string WhsCode { get; set; }
            public string FinalItemToUse { get; set; }
            public decimal AllocatedQty { get; set; }
            public decimal OnHand { get; set; }
            public string MuadilInfo { get; set; }
            public bool IsMissing { get; set; }
            public decimal MissingQty { get; set; }
        }

        public class ProdExecutionModel
        {
            public string ParentCode { get; set; }
            public decimal TargetQty { get; set; }
            public List<ProdExecutionComponent> Components { get; set; }
        }

        public class ProdExecutionComponent
        {
            public string FinalItemToUse { get; set; }
            public decimal BaseQty { get; set; }
            public decimal TargetQty { get; set; }
            public decimal AllocatedQty { get; set; }
            public string WhsCode { get; set; }
        }
    }
}