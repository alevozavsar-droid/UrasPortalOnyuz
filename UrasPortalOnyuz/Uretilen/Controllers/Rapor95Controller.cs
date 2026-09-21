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
using System.Text.RegularExpressions;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor95Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor95Controller> _logger;

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
            new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S", DbName = "URSMAKINE__A.S" },
            new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S", DbName = "ALVKIMYA_A.S" },
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


        private decimal? ExtractBcOran(string itemName)
 {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor95Controller.DatabaseConfig>(12);
ViewBag.CurrentDbKey = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        [HttpGet("GetCustomerSpecialPrices")]
        public IActionResult GetCustomerSpecialPrices(string cardCode)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor95Controller.SpecialPriceViewModel>(12) });
}


        [HttpGet("GetItemSpecialPrices")]
        public IActionResult GetItemSpecialPrices(string itemCode)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor95Controller.ItemSpecialPriceViewModel>(12) });
}

        [HttpGet("GetAllPrices")]
        public IActionResult GetAllPrices()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName", "ItemCode", "ItemName", "Price", "Currency" }) });
}


        [HttpGet("SearchCustomers")]
        public IActionResult SearchCustomers(string q)
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName" }) });
}

        [HttpGet("SearchItems")]
        public IActionResult SearchItems(string q)
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName" }) });
}

        private string MetinTemizle(string metin)  {return default;
}
        private string MetinTemizleBosluksuz(string metin)  {return default;
}

        private void OnbellekleriDoldur(string dbKey, out Dictionary<string, string> cariCache, out Dictionary<string, string> stokCache)
 {cariCache = default;
stokCache = default;
}

        [HttpPost("BulkImport")]
        public async Task<IActionResult> BulkImport([FromBody] ExcelImportRequest request)
 {return Json(new { success = true, basarili = global::WebApplication3.OrnekDoldurucu.Deger<int>("basarili", 0), hatali = global::WebApplication3.OrnekDoldurucu.Deger<int>("hatali", 0), detay = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName" }) });
}


        [HttpPost("SaveMultiplePrices")]
        public async Task<IActionResult> SaveMultiplePrices([FromBody] MultiplePriceRequest req)
 {return Json(new { success = true, warning = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("SaveMultipleCustomerPrices")]
        public async Task<IActionResult> SaveMultipleCustomerPrices([FromBody] MultipleCustomerPriceRequest req)
 {return Json(new { success = true, warning = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpGet("DownloadTemplate")]
        public IActionResult DownloadTemplate(int type)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private List<CustomerModel> GetCustomers(string dbKey)
 {return default;
}

        private List<ItemModel> GetItems(string dbKey)
 {return default;
}


        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class CustomerModel { public string CardCode { get; set; } public string CardName { get; set; } }
        public class ItemModel { public string ItemCode { get; set; } public string ItemName { get; set; } public decimal? BcOran { get; set; } }
        public class SpecialPriceViewModel { public string ItemCode { get; set; } public string ItemName { get; set; } public decimal? UnitPrice { get; set; } public decimal PackagePrice { get; set; } public decimal? BcOran { get; set; } public string Currency { get; set; } }
        public class ExcelImportRequest { public bool MatchNames { get; set; } public List<ExcelRow> Rows { get; set; } }
        public class ExcelRow { public string Cari { get; set; } public string Stok { get; set; } public string Fiyat { get; set; } public string Doviz { get; set; } }
        public class MultiplePriceRequest { public string CardCode { get; set; } public List<PriceItem> Items { get; set; } }
        public class PriceItem { public string ItemCode { get; set; } public double PackagePrice { get; set; } public string Currency { get; set; } }


        public class ItemSpecialPriceViewModel { public string CardCode { get; set; } public string CardName { get; set; } public string ItemCode { get; set; } public string ItemName { get; set; } public decimal? UnitPrice { get; set; } public decimal PackagePrice { get; set; } public decimal? BcOran { get; set; } public string Currency { get; set; } }
        public class MultipleCustomerPriceRequest { public string ItemCode { get; set; } public List<CustomerPriceItem> Customers { get; set; } }
        public class CustomerPriceItem { public string CardCode { get; set; } public double PackagePrice { get; set; } public string Currency { get; set; } }
    }
}