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
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Globalization;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor96Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor96Controller> _logger;

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
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya", DbName = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS", DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS", DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS", DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S", DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S", DbName = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS", DbName = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.Customers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor96Controller.CustomerModel>(12);
ViewBag.Items = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor96Controller.ItemModel>(12);
ViewBag.ItemGroups = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.Warehouses = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor96Controller.WarehouseModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}




        [HttpPost("RunReport")]
        public IActionResult RunReport([FromBody] ReportFilterModel filter)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::System.Collections.Generic.Dictionary<string, object>>(12) });
}




        [HttpPost("ExportExcel")]
        public IActionResult ExportExcel([FromBody] ReportFilterModel filter)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}




        [HttpPost("ExportPdf")]
        public IActionResult ExportPdf([FromBody] ReportFilterModel filter)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}




        private string BuildReportQuery(ReportFilterModel filter, string dbKey)
 {return default;
}




        private string BuildIcmalQuery(ReportFilterModel filter, string dBas, string dBit, string dbKey)
 {return default;
}

        private string GetIcmalBaseQuery(string t0, string t1, string dBas, string dBit, string filters, string ydKodlar, bool isIade, bool isTaslak)
 {return default;
}




        private string GetOrtakFiltreler(ReportFilterModel filter, string dbKey, out string yurtDisiKodlari)
 {yurtDisiKodlari = default;
return default;
}

        private void GrupKodlariniBul(string dbKey, out string yurtIciKodlari, out string yurtDisiKodlari)
 {yurtIciKodlari = default;
yurtDisiKodlari = default;
}

        private string GetFaturaQuery(string dBas, string dBit, string innerFilters)
 {return default;
}

        private string GetIadeQuery(string dBas, string dBit, string innerFilters)
 {return default;
}

        private string GetTaslakFaturaQuery(string dBas, string dBit, string innerFilters)
 {return default;
}
        private string GetTaslakIadeQuery(string dBas, string dBit, string innerFilters)
 {return default;
}

        private string GetTaslakBaseQuery(string dBas, string dBit, string innerFilters, string objType, string bTip, bool isIade)
 {return default;
}

        private string GetAcikSiparisQuery(string dBas, string dBit, string innerFilters)
 {return default;
}

        private string GetAcikTeslimatQuery(string dBas, string dBit, string innerFilters)
 {return default;
}




        private List<Dictionary<string, object>> ExecuteQuery(string sql, string dbKey)
 {return default;
}

        private List<CustomerModel> GetCustomers(string dbKey)
 {return default;
}

        private List<ItemModel> GetItems(string dbKey)
 {return default;
}

        private List<string> GetItemGroups(string dbKey)
 {return default;
}

        private List<WarehouseModel> GetWarehouses(string dbKey)
 {return default;
}


        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class CustomerModel { public string CardCode { get; set; } public string CardName { get; set; } }
        public class ItemModel { public string ItemCode { get; set; } public string ItemName { get; set; } }
        public class WarehouseModel { public string WhsCode { get; set; } public string WhsName { get; set; } }

        public class ReportFilterModel
        {
            public string StartDate { get; set; }
            public string EndDate { get; set; }
            public string ReportType { get; set; }
            public string Warehouse { get; set; }
            public string ProcessType { get; set; }
            public string Market { get; set; }
            public string CompanyType { get; set; }
            public bool IncInvoice { get; set; }
            public bool IncReturn { get; set; }
            public bool IncItem { get; set; }
            public bool IncService { get; set; }
            public bool IncDraft { get; set; }
            public List<string> SelectedCustomers { get; set; }
            public List<string> SelectedItems { get; set; }
            public List<string> SelectedItemGroups { get; set; }
            public List<string> VisibleColumns { get; set; }
        }
    }
}