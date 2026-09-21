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
using System.Collections.Concurrent;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor140Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor140Controller> _logger;

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

        [HttpGet]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetStockData")]
        public async Task<IActionResult> GetStockData(string dbKey = "")
 {return Json(new { success = true, companyName = global::WebApplication3.OrnekDoldurucu.Deger<string>("companyName", 0), hammaddeData = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.StockItemModel>(12), yariMamulData = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.StockItemModel>(12), urunData = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.StockItemModel>(12), digerData = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.StockItemModel>(12), kpis = new { totalItems = global::WebApplication3.OrnekDoldurucu.Deger<int>("totalItems", 0), totalQuantity = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalQuantity", 0), totalValueTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalValueTL", 0), totalValueUSD = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalValueUSD", 0), totalValueEUR = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalValueEUR", 0), currentUsdRate = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("currentUsdRate", 0), currentEurRate = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("currentEurRate", 0) } });
}
    }


    public class StockItemModel
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Category { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal TotalValueTL { get; set; }
        public decimal TotalValueUSD { get; set; }
        public decimal TotalValueEUR { get; set; }


        public decimal LastPurPrcTL { get; set; }
        public decimal LastPurPrcUSD { get; set; }
        public decimal LastPurPrcEUR { get; set; }
        public decimal LastPurPrc { get; set; }

        public string LastPurCur { get; set; }
        public string SourceType { get; set; }
        public string LastInvoiceNum { get; set; }
        public string LastInvoiceDateStr { get; set; }
        public List<StockDetailModel> Details { get; set; }
    }

    public class StockDetailModel
    {
        public string CompanyName { get; set; }
        public string DbName { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public decimal OnHand { get; set; }


        public decimal LastPurPrcTL { get; set; }
        public decimal LastPurPrcUSD { get; set; }
        public decimal LastPurPrcEUR { get; set; }
        public decimal LastPurPrc { get; set; }

        public string LastPurCur { get; set; }
        public string SourceType { get; set; }
        public string LastInvoiceNum { get; set; }
        public DateTime? LastInvoiceDate { get; set; }


        public decimal TotalValueTL { get; set; }
        public decimal TotalValueUSD { get; set; }
        public decimal TotalValueEUR { get; set; }
        public decimal TotalValue { get; set; }
    }
}