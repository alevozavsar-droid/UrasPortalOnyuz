// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor103Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor103Controller> _logger;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya", DbName = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS", DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS", DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS", DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S", DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S", DbName = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS", DbName = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
        };


        private readonly string _merkeziKuyrukDbKey = "DefaultConnection5";

        private string GetSelectedDatabase(string explicitlyPassedDb = null)
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.CurrentDbDisplay = "";
ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor103Controller.DatabaseConfig>(12);
ViewBag.Customers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor103Controller.DatabaseConfig>(12);
ViewBag.Suppliers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor103Controller.DatabaseConfig>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetCustomersList")]
        public IActionResult GetCustomersList([FromQuery] string db = null)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor103Controller.DatabaseConfig>(12) });
}

        [HttpGet("GetSuppliersList")]
        public IActionResult GetSuppliersList([FromQuery] string db = null)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor103Controller.DatabaseConfig>(12) });
}




        private void UIRobotKuyrugunaYaz(string hedefSirketDbName, string cardCode, List<SimulationLineModel> lines, string slHataMesaji, DateTime reconDate, int grupNo)
 {}




        [HttpGet("GetOpenTransactions")]
        public async Task<IActionResult> GetOpenTransactions(string cardCode)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor103Controller.OpenTransactionModel>(12) });
}




        [HttpPost("PerformReconciliation")]
        public async Task<IActionResult> PerformReconciliation([FromBody] ReconciliationRequestModel request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}




        [HttpPost("AutoReconcile")]
        public async Task<IActionResult> AutoReconcile(string cardCode)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}




        [HttpPost("CancelReconciliations")]
        public async Task<IActionResult> CancelReconciliations(string cardCode)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), hasError = true });
}




        private async Task<(bool success, string message)> SendToServiceLayerSmartAsync(
            string companyDb, string cardCode, string cardType, List<SimulationLineModel> lines,
            Dictionary<string, string> lineDirections, DateTime reconDate, bool hasBoe)
 {return default;
}

        private async Task<(bool success, string message, string payload)> TryNormalReconciliationAsync(
            HttpClient client, string cardCode, List<SimulationLineModel> lines, DateTime reconDate)
 {return default;
}

        private string ExtractErrorMessage(string jsonResponse)
 {return default;
}




        private List<DatabaseConfig> GetBusinessPartners(string dbKey, string cardType)
 {return default;
}

        private string GetTransTypeName(string typeId)
 {return default;
}


        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class OpenTransactionModel
        {
            public int TransId { get; set; }
            public int LineId { get; set; }
            public string DocDate { get; set; }
            public string DueDate { get; set; }
            public string Memo { get; set; }
            public string TransType { get; set; }
            public decimal OpenDebit { get; set; }
            public decimal OpenCredit { get; set; }
            public string AktarimTarihi { get; set; }
            public string IslemTipi { get; set; }
            public string ParaBirimi { get; set; }
            public decimal DovizliBorc { get; set; }
            public decimal DovizliAlacak { get; set; }
            public bool IsBOE { get; set; }
        }
        public class ReconciliationRequestModel { public string CardCode { get; set; } public List<ReconLine> Lines { get; set; } }
        
        public class ReconLine 
        { 
            public int TransId { get; set; } 
            public int LineId { get; set; } 
            public decimal ReconcileAmount { get; set; } 
            public decimal ReconcileAmountFC { get; set; } // YENİ EKLENEN
        }
        
        public class SimulationLineModel 
        { 
            public int TransId { get; set; } 
            public int LineId { get; set; } 
            public decimal Tutar { get; set; } 
            public decimal TutarFC { get; set; } // YENİ EKLENEN
            public int GrupNo { get; set; } 
        }
    }
}