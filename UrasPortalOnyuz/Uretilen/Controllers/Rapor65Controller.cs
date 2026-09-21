// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using WebApplication3.Models;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor65Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IMemoryCache _cache;


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

        private string GetSelviContextType(string dbKey)
 {return default;
}


        private List<LookupModel> GetPaymentTerms(string dbKey)
 {return default;
}


        [HttpGet]
        [Route("")]
        [Route("Index")]
        public IActionResult Index(string searchKeyword, bool hideZeroRisk = true, bool useBpDueDate = false, bool isLoad = false, string cariTipi = "")
 {ViewBag.SelectedCariTipi = "";
ViewBag.CurrentDbDisplay = "";
ViewBag.SelviContext = "";
ViewBag.SearchKeyword = "";
ViewBag.PaymentTerms = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor65Controller.LookupModel>(12);
ViewBag.IsLoad = false;
ViewBag.ShowSelviColumn = false;
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.CariRiskRaporuViewModel>(12));
}

        [HttpPost("UpdateBpPaymentTerm")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBpPaymentTerm(string cardCode, int groupNum)
 {return Json(new { success = true, message = "Ödeme koşulu SAP'de başarıyla güncellendi." });
}

        [HttpGet("GetCariRiskDetay")]
        public IActionResult GetCariRiskDetay(string cardName, string cardCode, bool useBpDueDate = false)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Models.CariRiskDetayViewModel>());
}

        [HttpPost("DownloadExcel")]
        public IActionResult DownloadExcel(string searchKeyword, bool hideZeroRisk, bool useBpDueDate = false, string cariTipi = "")
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}



        private List<CariRiskRaporuViewModel> GetRiskRaporuData(string connectionString, bool isSelvi, string selviContextType, bool useBpDueDate, string currentDbDisplay)
 {return default;
}







        [HttpGet("GetBakiyeDetay")]
        public IActionResult GetBakiyeDetay(string cardCode, string tip, string cardName = null)
 {return Json(new { success = true, tip = global::WebApplication3.OrnekDoldurucu.Deger<string>("tip", 0), cardCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("cardCode", 0), toplamTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("toplamTL", 0), dbKey = global::WebApplication3.OrnekDoldurucu.Deger<string>("dbKey", 0), data = new object[0] });
}

        private string BuildRiskQuery(bool isSelvi, bool isDetail, bool excludeSelvi = false)
 {return default;
}


        public class LookupModel
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }
    }
}