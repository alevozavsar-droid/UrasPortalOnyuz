// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
    public class Rapor83Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor83Controller> _logger;


        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA", DbName = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS", DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS", DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS", DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S", DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S", DbName = "URASHOLDING_A.S" },
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
        [HttpGet("Index")]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetFiltersData")]
        public async Task<IActionResult> GetFiltersData()
 {return Json(new { success = true, companies = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("companies", i2)).ToList(), reps = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("reps", i2)).ToList(), countries = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { Value = global::WebApplication3.OrnekDoldurucu.Deger<string>("Value", i2), Text = global::WebApplication3.OrnekDoldurucu.Deger<string>("Text", i2) }).ToList() });
}

        [HttpPost("GetReportData")]
        public async Task<IActionResult> GetReportData([FromBody] Rapor83FilterModel filter)
 {return Json(new { success = true, summary = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor83Summary>(), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor83InvoiceDto>(12) });
}



        [HttpPost("AddSalesEmployee")]
        public async Task<IActionResult> AddSalesEmployee([FromBody] AddSlpModel model)
 {return Json(new { success = true, message = "Satış çalışanı kontrol edildi; sadece olmayan şirketlere başarıyla eklendi." });
}

        [HttpPost("BulkUpdateSalesEmployee")]
        public async Task<IActionResult> BulkUpdateSalesEmployee([FromBody] BulkUpdateRequest request)
 {return Json(new { success = true, message = "Seçili faturalar başarıyla güncellendi." });
}
    }



    public class FilterDto { public string Type { get; set; } public string Value { get; set; } public string Text { get; set; } }

    public class Rapor83FilterModel { public List<string> Companies { get; set; } public List<string> SlpNames { get; set; } public List<string> Countries { get; set; } public string InvoiceType { get; set; } public DateTime StartDate { get; set; } public DateTime EndDate { get; set; } }

    public class Rapor83Summary { public decimal TotalMiktar1 { get; set; } public decimal TotalMiktar2 { get; set; } public decimal TotalTRY { get; set; } public decimal TotalUSD { get; set; } public decimal TotalEUR { get; set; } }

    public class Rapor83RawDataRow
    {
        public string Sirket { get; set; }
        public int DocEntry { get; set; }
        public string BelgeTipi { get; set; }
        public string FaturaTuru { get; set; }
        public string SlpName { get; set; }
        public string DocNum { get; set; }
        public string DocDate { get; set; }
        public string CardName { get; set; }
        public string Country { get; set; }
        public string ItemCode { get; set; }
        public string Dscription { get; set; }
        public decimal Miktar1 { get; set; }
        public string Birim1 { get; set; }
        public decimal Miktar2 { get; set; }
        public string Birim2 { get; set; }
        public decimal TutarTRY { get; set; }
        public decimal TutarUSD { get; set; }
        public decimal TutarEUR { get; set; }
        public string ParaBirimi { get; set; }
    }

    public class Rapor83InvoiceDto
    {
        public string Sirket { get; set; }
        public int DocEntry { get; set; }
        public string BelgeTipi { get; set; }
        public string FaturaTuru { get; set; }
        public string SlpName { get; set; }
        public string DocNum { get; set; }
        public string DocDate { get; set; }
        public string CardName { get; set; }
        public string Country { get; set; }
        public string ParaBirimi { get; set; }
        public decimal ToplamMiktar1 { get; set; }
        public decimal ToplamMiktar2 { get; set; }
        public decimal TutarTRY { get; set; }
        public decimal TutarUSD { get; set; }
        public decimal TutarEUR { get; set; }
        public List<Rapor83InvoiceDetailDto> Details { get; set; }
    }

    public class Rapor83InvoiceDetailDto { public string ItemCode { get; set; } public string Dscription { get; set; } public decimal Miktar1 { get; set; } public string Birim1 { get; set; } public decimal Miktar2 { get; set; } public string Birim2 { get; set; } public decimal TutarTRY { get; set; } public decimal TutarUSD { get; set; } public decimal TutarEUR { get; set; } }

    public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }

    public class AddSlpModel { public string SlpName { get; set; } }

    public class BulkUpdateRequest
    {
        public string NewSlpName { get; set; }
        public List<InvoiceIdentifier> Invoices { get; set; }
    }

    public class InvoiceIdentifier
    {
        public string Sirket { get; set; }
        public int DocEntry { get; set; }
        public string DocNum { get; set; }
        public string BelgeTipi { get; set; }
    }
}