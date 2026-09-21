// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebApplication3.Models;
using System.Linq;
using Microsoft.Extensions.Primitives;
using System.Data;

public class Rapor16Controller : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<Rapor16Controller> _logger;

    private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
    {
        new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
                        new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
                                                new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S" },

                                                new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S" },
                                                new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
                                                new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
                                                new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
                                                new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
                                                new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

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

    public async Task<IActionResult> Index(int? docEntry)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Models.AlvFiyatViewModel>());
}

    [HttpPost]
    public async Task<IActionResult> SaveData([FromBody] AlvFiyatViewModel model)
 {return Json(new { success = true, docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0) });
}

    [HttpGet]
    public async Task<JsonResult> GetSavedReports()
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "U_BE1_CUSTOMER", "CardName", "CreateDate" }));
}

    public async Task<AlvFiyatViewModel> GetReportDetails(int docEntry)
 {return default;
}

    [HttpGet]
    public async Task<JsonResult> GetCustomers()
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName" }));
}

    [HttpGet]
    public async Task<JsonResult> GetCountries()
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Code", "Name" }));
}

    [HttpGet]
    public async Task<JsonResult> GetProducts()
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName" }));
}

    [HttpGet]
    public async Task<JsonResult> GetCustomerDetails(string cardCode)
 {return Json(new { Balance = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("Balance", 0), CreditLine = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("CreditLine", 0), Term = global::WebApplication3.OrnekDoldurucu.Deger<string>("Term", 0), CofasLimit = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("CofasLimit", 0) });
}

    [HttpGet]
    public async Task<JsonResult> GetLatestEuroUsdParity()
 {return Json(new { Parity = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("Parity", 0) });
}

    [HttpGet]
    public async Task<JsonResult> GetProductCost(string itemCode)
 {return Json(new { totalCost = 0m });
}
}