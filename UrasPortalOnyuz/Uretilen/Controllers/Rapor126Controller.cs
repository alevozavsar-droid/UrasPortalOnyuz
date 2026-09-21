// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("Rapor126")]
    public class Rapor126Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor126Controller> _logger;

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
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },
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

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } }

        public class Rapor126ViewModel
        {
            public string KaynakTablo { get; set; }
            public string IslemTuru { get; set; }
            public string BelgeNumarasi { get; set; }
            public string BelgeTarihi { get; set; }

            public string SapIptal { get; set; }
            public string UBe1IadeDegeri { get; set; }
            public string DurumTipi { get; set; }

            public string OrijinalYevmiyeNo { get; set; }
            public string OrijinalYev_UBe1Yeviptal { get; set; }
            public string TersYevmiyeNo { get; set; }
            public string TersYev_UBe1Yeviptal { get; set; }

            public bool IsUyumlu { get; set; }
            public string HataNedeni { get; set; }
            public string HataKodu { get; set; }
        }

        public class FixYevmiyeModel
        {
            public string KaynakTablo { get; set; }
            public string BelgeAnahtar { get; set; }
            public string OrijinalYevmiyeNo { get; set; }
            public string TersYevmiyeNo { get; set; }
            public string HataKodu { get; set; }
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor126Controller.Rapor126ViewModel>(12));
}

        [HttpPost("FixYeviptal")]
        public async Task<IActionResult> FixYeviptal([FromBody] FixYevmiyeModel model)
 {return Json(new { success = true, message = "İşlem başarıyla uygulandı." });
}
    }
}