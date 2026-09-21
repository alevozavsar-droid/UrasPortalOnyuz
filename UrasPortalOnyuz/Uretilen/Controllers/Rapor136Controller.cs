// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;
using System.Globalization;
using System.Collections.Concurrent;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor136Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor136Controller> _logger;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "Avrasya" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
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

        private readonly string _merkeziKuyrukDbKey = "DefaultConnection5";
        private static readonly ConcurrentDictionary<string, string> _reconProgress = new ConcurrentDictionary<string, string>();

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetConsolidatedData")]
        public async Task<IActionResult> GetConsolidatedData(int year, int month)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.ConsolidatedPremiumModel>(12), kpis = new { totalSales = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalSales", 0), totalPremium = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalPremium", 0), totalOverdueChange = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalOverdueChange", 0), personnelCount = global::WebApplication3.OrnekDoldurucu.Deger<int>("personnelCount", 0) } });
}

        private static string CleanCompanyName(string name)
 {return default;
}
        private static int ComputeLevenshteinDistance(string s, string t)
 {return default;
}
        private static double CalculateSimilarity(string source, string target)
 {return default;
}

        private (string CardCode, string CardName) GetEquivalentBpInfo(string sourceConnStr, string targetConnStr, string sourceBpCode)
 {return default;
}

        [HttpGet("GetOpenTransactions")]
        public async Task<IActionResult> GetOpenTransactions(string cardCode, string targetDbName, bool isConsolidated = false, bool useBpDueDate = false)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.OpenTransactionModel>(12) });
}

        [HttpPost("PerformReconciliation")]
        public async Task<IActionResult> PerformReconciliation([FromBody] ReconciliationRequestModel request)
 {return Json(new { success = true, isRobotPending = true, matchedCardCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("matchedCardCode", 0), message = "Servis katmanı işlemi kapatamadı. İşlem UI Robot'a aktarıldı, arka planda kapanması bekleniyor..." });
}

        [HttpPost("CancelReconciliations")]
        public async Task<IActionResult> CancelReconciliations(string cardCode, string targetDbName)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), hasError = true });
}

        [HttpGet("CheckUnreconciledForMutabakat")]
        public async Task<IActionResult> CheckUnreconciledForMutabakat(string cardCode, bool isConsolidated)
 {return Json(new { success = true, hasOpen = true, message = "Bu carinin SAP sisteminde eşleştirilebilecek 'Açık İşlemleri' bulunmaktadır. Mutabakat kaydetmeden önce lütfen 'Mutabakat Yap' butonunu kullanarak ilgili belgeleri kapatınız." });
}

        [HttpPost("SaveManualMutabakat")]
        public async Task<IActionResult> SaveManualMutabakat(string cardCode, DateTime mutabakatTarihi, decimal bakiye, bool isConsolidated)
 {return Json(new { success = true, message = "Mutabakat başarıyla kaydedildi." });
}

        [HttpGet("GetAutoReconcileProgress")]
        public IActionResult GetAutoReconcileProgress(string cardCode, string targetDbName)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("AutoReconcile")]
        public async Task<IActionResult> AutoReconcile(string cardCode, string targetDbName, bool useBpDueDate = false)
 {return Json(new { success = true, isRobotPending = true, matchedCardCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("matchedCardCode", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpGet("GetRobotCheckStatus")]
        public async Task<IActionResult> GetRobotCheckStatus(string matchedCardCode, string dbName)
 {return Json(new { success = true, isFinished = global::WebApplication3.OrnekDoldurucu.Deger<bool>("isFinished", 0), statusMessage = global::WebApplication3.OrnekDoldurucu.Deger<string>("statusMessage", 0), stats = new { bekleyen = global::WebApplication3.OrnekDoldurucu.Deger<int>("bekleyen", 0), islemeAlinan = global::WebApplication3.OrnekDoldurucu.Deger<int>("islemeAlinan", 0), basarili = global::WebApplication3.OrnekDoldurucu.Deger<int>("basarili", 0), iptal = global::WebApplication3.OrnekDoldurucu.Deger<int>("iptal", 0), toplamIslem = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplamIslem", 0) } });
}

        private async Task<(bool success, string message)> SendToServiceLayerSmartAsync(string companyDb, string cardCode, string cardType, List<SimulationLineModel> lines, Dictionary<string, string> lineDirections, DateTime reconDate, bool hasBoe)
 {return default;
}

        private void UIRobotKuyrugunaYaz(string hedefSirketDbName, string cardCode, List<SimulationLineModel> lines, string slHataMesaji, DateTime reconDate, int grupNo)
 {}

        private string ExtractErrorMessage(string jsonResponse)
 {return default;
}
    }

  

    public class ConsolidatedPremiumModel
    {
        public string SalesPersonName { get; set; }
        public bool IsForeign { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalNetSales { get; set; }
        public decimal TotalSalesPremium { get; set; }
        public decimal TotalOverdueChange { get; set; }
        public decimal TotalOverdueEffect { get; set; }
        public decimal TotalFinalPremium { get; set; }
        public List<CustomerPremiumModel> Customers { get; set; }
    }

    public class CustomerPremiumModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public bool HasOpenTransactions { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalNetSales { get; set; }
        public decimal TotalSalesPremium { get; set; }
        public decimal TotalOverdueChange { get; set; }
        public decimal TotalOverdueEffect { get; set; }
        public decimal TotalFinalPremium { get; set; }
        public List<PremiumDetailModel> Details { get; set; }
    }

    public class PremiumDetailModel
    {
        public string CompanyName { get; set; }
        public string DbName { get; set; }
        public string SalesPersonName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public bool HasOpenTransactions { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal NetSales { get; set; }
        public decimal PrevMonthOverdue { get; set; }
        public decimal CurrMonthOverdue { get; set; }
        public decimal OverdueChange { get; set; }
        public decimal SalesPremium { get; set; }
        public decimal OverdueEffect { get; set; }
        public decimal FinalPremium { get; set; }
    }
}