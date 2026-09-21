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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{






    [Authorize]
    public class Rapor64Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor64Controller> _logger;



        private readonly string _merkeziKuyrukDbKey = "DefaultConnection5";

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
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya", DbName = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor64ViewModel>(12));
}





        [HttpPost]
        public async Task<IActionResult> MutabakatEmriVer([FromBody] MutabakatEmriRequest request)
 {return Json(new { success = true, isRobotPending = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}






        private async Task<int> KalanTlKapatAsync(string connectionString, string companyDb, string cardCode)
 {return default;
}



        private void UIRobotKuyrugunaYaz(string hedefSirketDbName, string cardCode, List<(int TransId, int LineId, decimal Amount)> lines, string slHataMesaji, DateTime reconDate, int grupNo)
 {}


        private async Task<(bool success, string message)> PostReconciliationAsync(
            string companyDb, string cardCode, List<(int TransId, int LineId, decimal Amount)> reconRows, DateTime reconDate)
 {return default;
}

        private string ExtractErrorMessage(string jsonResponse)
 {return default;
}

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }

        public class SimulationLineModel
        {
            public int TransId { get; set; }
            public int LineId { get; set; }
            public decimal Tutar { get; set; }
            public decimal TutarFC { get; set; }
            public int GrupNo { get; set; }
        }
    }

    public class Rapor64ViewModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal Bakiye { get; set; }
        public string ParaBirimi { get; set; }
    }

    public class MutabakatEmriRequest
    {
        public string CardCode { get; set; }
    }
}
