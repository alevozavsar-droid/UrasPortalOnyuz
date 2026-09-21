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

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor113Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor113Controller> _logger;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },


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
 {ViewBag.CurrentDbDisplay = "";
ViewBag.Users = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
ViewBag.Employees = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
ViewBag.Departments = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
ViewBag.Branches = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
ViewBag.Items = WebApplication3.OrnekDoldurucu.Yeni<System.Collections.IEnumerable>();
ViewBag.ActiveHeaderUDFs = new System.Collections.Generic.HashSet<string>();
ViewBag.ActiveLineUDFs = new System.Collections.Generic.HashSet<string>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}



        private void SecimListeleriniYukle(string dbKey)
 {}





        [HttpGet("GetPurchaseRequestNavigation")]
        public IActionResult GetPurchaseRequestNavigation(int currentDocEntry, string direction)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor113Controller.PurchaseRequestDetailViewModel>() });
}

        [HttpPost("FindPurchaseRequest")]
        public IActionResult FindPurchaseRequest([FromBody] PurchaseRequestSearchModel search)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor113Controller.PurchaseRequestSearchResultModel>(12) });
}

        [HttpGet("GetPurchaseRequestDetails")]
        public IActionResult GetPurchaseRequestDetails(int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor113Controller.PurchaseRequestDetailViewModel>() });
}




        [HttpPost("CreatePurchaseRequest")]
        public async Task<IActionResult> CreatePurchaseRequest([FromBody] PurchaseRequestCreationModel request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private List<ItemModel> GetItems(string dbKey)
 {return default;
}

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class ItemModel { public string ItemCode { get; set; } public string ItemName { get; set; } public string UoM { get; set; } }
        public class PurchaseRequestSearchModel { public int? DocNum { get; set; } public string ReqName { get; set; } }
        public class PurchaseRequestSearchResultModel { public int DocEntry { get; set; } public int DocNum { get; set; } public string ReqName { get; set; } public string DocDate { get; set; } }
        public class PurchaseRequestDetailViewModel { public int DocEntry { get; set; } public int DocNum { get; set; } public string ReqName { get; set; } public string DocStatus { get; set; } public string DocDate { get; set; } public string DocDueDate { get; set; } public string TaxDate { get; set; } public string ReqDate { get; set; } public string Comments { get; set; } public List<PurchaseRequestLineModel> Lines { get; set; } = new List<PurchaseRequestLineModel>(); }
        public class PurchaseRequestCreationModel { public string ReqName { get; set; } public DateTime DocDate { get; set; } public DateTime DocDueDate { get; set; } public DateTime TaxDate { get; set; } public DateTime ReqDate { get; set; } public string Comments { get; set; } public List<PurchaseRequestLineModel> DocumentLines { get; set; } }
        public class PurchaseRequestLineModel { public string ItemCode { get; set; } public string ItemName { get; set; } public double Quantity { get; set; } }
    }
}