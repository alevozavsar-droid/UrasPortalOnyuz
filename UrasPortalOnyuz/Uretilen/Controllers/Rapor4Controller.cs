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
using WebApplication3.Models;
using System.Net.Http;
using System.Net.Http.Headers;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor4Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor4Controller> _logger;


        private static readonly string FILTER_SEPARATOR = "|||";

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

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.DatabaseConfig>(12);
ViewBag.CurrentDbDisplay = "";
ViewBag.AllSalesEmployees = new System.Collections.Generic.List<object>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor4Controller.SalesOrderViewModel>(12));
}

        [HttpGet("GetData")]
        public async Task<IActionResult> GetData(
            int page = 1,
            int pageSize = 300,
            string sortColumn = "BelgeTarihi",
            string sortDirection = "desc",
            string filters = "")
 {return Json(new { data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor4Controller.SalesOrderViewModel>(12), totalRecords = global::WebApplication3.OrnekDoldurucu.Deger<int>("totalRecords", 0) });
}

        [HttpGet("GetTotalAmount")]
        public async Task<IActionResult> GetTotalAmount([FromQuery] string filters = "")
 {return Json(new { success = true, totalAmount = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalAmount", 0), totalKDVsiz = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("totalKDVsiz", 0) });
}

        [HttpPost("UpdateSalesEmployee")]
        public async Task<IActionResult> UpdateSalesEmployee([FromBody] UpdateSlpRequest request)
 {return Json(new { success = true, message = "Satış temsilcisi başarıyla güncellendi." });
}

        private List<SalesEmployeeModel> GetAllSalesEmployees(string selectedDbKey)
 {return default;
}

        private StringBuilder BuildQuery(int page, int pageSize, string sortColumn, string sortDirection, string filters)
 {return default;
}

        private StringBuilder BuildCountQuery(string filters)
 {return default;
}

        private void AddFiltersToQuery(StringBuilder queryBuilder, string filters)
 {}
        [HttpGet("GetDynamicFilters")]
        public async Task<IActionResult> GetDynamicFilters([FromQuery] string filters = "")
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor4Controller.FilterDropdownsViewModel>());
}
        private string GetSqlColumnForFilter(string column)
 {return default;
}

        private FilterDropdownsViewModel GetFilterDropdownsData(string selectedDbKey)
 {return default;
}

        public class SalesOrderViewModel
        {
            public decimal? ToplamMaliyetStdTL { get; set; }
            public decimal? ToplamKarZararStdTL { get; set; }
            public decimal? KarZararStdYuzdesi { get; set; }

            public decimal? ToplamMaliyetAlvTL { get; set; }
            public decimal? ToplamKarZararAlvTL { get; set; }
            public decimal? KarZararAlvYuzdesi { get; set; }
            public int DocEntry { get; set; }
            public int BelgeNumarasi { get; set; }
            public string BelgeTipi { get; set; }
            public string BelgeTarihi { get; set; }
            public string BelgeAyYil { get; set; }
            public string HesaplananVadeTarihi { get; set; }
            public string HesaplananVadeAyYil { get; set; }
            public string MusteriAdi { get; set; }
            public string MusteriGrubu { get; set; }
            public decimal? SiparisBelgeTutari { get; set; }
            public decimal? KDVsizSiparisBelgeTutari { get; set; }
            public string BelgeParaBirimi { get; set; }
            public string SatisTemsilcisi { get; set; }
            public string OdemeKosuluGrubu { get; set; }
            public decimal? SiparisTLTutariGuncelKur { get; set; }
            public decimal? ToplamFaturaTutariFaturaPB { get; set; }
            public decimal? ToplamFaturaTutariTL { get; set; }
            public decimal? ToplamOdenenTutarFaturaPB { get; set; }
            public decimal? ToplamOdenenTutarTL { get; set; }
            public decimal? KalanTutarFaturaPB { get; set; }
            public decimal? KalanTutarTL { get; set; }
            public decimal? MusteriGuncelBakiyesi { get; set; }
            public decimal? VadesiGecmisFaturaTutariTL { get; set; }
            public int SlpCode { get; set; }
        }

        public class FilterDropdownsViewModel
        {
            public List<string> BelgeTarihiList { get; set; } = new List<string>();
            public List<string> BelgeAyiList { get; set; } = new List<string>();
            public List<string> VadeAyiList { get; set; } = new List<string>();
            public List<string> MusteriAdiList { get; set; } = new List<string>();
            public List<string> MusteriGrubuList { get; set; } = new List<string>();
            public List<string> SatisTemsilcisiList { get; set; } = new List<string>();
            public List<string> BelgeParaBirimiList { get; set; } = new List<string>();
        }

        public class SalesEmployeeModel
        {
            public int SlpCode { get; set; }
            public string SlpName { get; set; }
        }

        public class UpdateSlpRequest
        {
            public int DocEntry { get; set; }
            public string BelgeTipi { get; set; }
            public int NewSlpCode { get; set; }
        }
    }
}