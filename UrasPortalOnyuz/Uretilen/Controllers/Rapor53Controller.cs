// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WebApplication3.Models; // TedarikciFinansalBorcRaporuViewModel ve ExportRequestModel burada varsayılır
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Text;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.IO;
using System.Globalization;
using System.Data;

namespace WebApplication3.Controllers
{
    

    [Route("[controller]")]
    public class Rapor53Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor53Controller> _logger;

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
        public async Task<IActionResult> Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.TedarikciFinansalBorcRaporuViewModel>(12));
}

        [HttpPost]
        [Route("ExportToExcel")]

        public async Task<IActionResult> ExportToExcel([FromBody] ExportRequestModel requestModel)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private void SetNumericCellValue<T>(ICell cell, T? value, ICellStyle style = null) where T : struct
 {}

        private void SetDateCellValue(ICell cell, DateTime? value, ICellStyle style = null)
 {}
    }
}