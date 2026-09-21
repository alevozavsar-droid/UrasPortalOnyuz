// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor81Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor81Controller> _logger;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
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
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        [HttpGet]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
 {ViewBag.Databases = new System.Collections.Generic.List<object>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Models.Rapor81DashboardViewModel>());
}


        [HttpGet("GetProductSalesApi")]
        public async Task<IActionResult> GetProductSalesApi(string itemCode, string db)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.Rapor81SalesDetail>(12) });
}

        private async Task<Rapor81DashboardViewModel> GetDetailedDashboardData(string connectionString, string currentDbDisplay)
 {return default;
}
    }
}

namespace WebApplication3.Models
{
    public class Rapor81DashboardViewModel
    {
        public Rapor81ManagerSummary ManagerSummary { get; set; }
        public List<Rapor81MonthlyTrend> MonthlyTrends { get; set; }
        public List<Rapor81ProductAnalysis> AllProducts { get; set; }
        public List<Rapor81CustomerAnalysis> AllCustomers { get; set; }
        public List<Rapor81SalesRep> SalesReps { get; set; }
        public List<Rapor81ItemGroup> ItemGroups { get; set; }
    }

    public class Rapor81ManagerSummary
    {
        public decimal CurrYearSales { get; set; }
        public decimal LastYearSales { get; set; }
        public decimal CurrYearProfit { get; set; }
        public decimal CurrMonthSales { get; set; }
        public decimal PrevMonthSales { get; set; }

        public decimal YoYPercentage => LastYearSales == 0 ? 100 : ((CurrYearSales - LastYearSales) / LastYearSales) * 100;
        public decimal MoMPercentage => PrevMonthSales == 0 ? 100 : ((CurrMonthSales - PrevMonthSales) / PrevMonthSales) * 100;
        public decimal ProfitMargin => CurrYearSales == 0 ? 0 : (CurrYearProfit / CurrYearSales) * 100;
    }

    public class Rapor81MonthlyTrend
    {
        public int Month { get; set; }
        public decimal CurrYearSales { get; set; }
        public decimal LastYearSales { get; set; }
    }

    public class Rapor81ProductAnalysis
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal NetQty { get; set; }
        public decimal NetSales { get; set; }

        public decimal UnitCost { get; set; }
        public decimal TotalCost => UnitCost * NetQty;
        public decimal ExactProfit => NetSales - TotalCost;
        public decimal ProfitMargin => NetSales == 0 ? 0 : (ExactProfit / NetSales) * 100;

        public decimal CurrentStock { get; set; }
        public string StockStatus => CurrentStock <= 0 ? "Stok Yok" : (CurrentStock > (NetQty * 2) ? "Şişkin" : "İyi");
    }

    public class Rapor81CustomerAnalysis
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public int InvoiceCount { get; set; }
        public decimal TotalSales { get; set; }
        public decimal TotalProfit { get; set; }
        public decimal ProfitMargin => TotalSales == 0 ? 0 : (TotalProfit / TotalSales) * 100;
    }

    public class Rapor81SalesRep
    {
        public string SlpName { get; set; }
        public decimal CurrTotal { get; set; }
        public decimal PrevTotal { get; set; }
        public decimal CurrProfit { get; set; }
        public decimal YoY => PrevTotal == 0 ? 100 : ((CurrTotal - PrevTotal) / Math.Abs(PrevTotal)) * 100;
        public decimal ProfitMargin => CurrTotal == 0 ? 0 : (CurrProfit / CurrTotal) * 100;

        public decimal[] CurrMonths { get; set; } = new decimal[13];
        public decimal[] PrevMonths { get; set; } = new decimal[13];
    }

    public class Rapor81ItemGroup
    {
        public string GroupName { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class Rapor81SalesDetail
    {
        public string DocDate { get; set; }
        public string DocNum { get; set; }
        public string CardName { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal NetTotal { get; set; }
        public string Currency { get; set; }
        public string DocType { get; set; }
        public string Company { get; set; }
    }
}