// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WebApplication3.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Globalization;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using System.IO;
using System.Text;

namespace WebApplication3.Controllers
{
    [Route("rapor22")]

    public class Rapor22Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor2Controller> _logger;

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

        private readonly List<CheckTransactionType> _checkTransactionTypes = new List<CheckTransactionType>
        {
            new CheckTransactionType { Code = "0", Description = "Seçiniz" },
            new CheckTransactionType { Code = "1", Description = "Portföyde" },
            new CheckTransactionType { Code = "2", Description = "Planlandı" },
            new CheckTransactionType { Code = "3", Description = "Erteleme Yapıldı – Çek Hâlâ Portföyde" },
            new CheckTransactionType { Code = "4", Description = "Erteleme Yapıldı – Ciro Edildi / Bankaya Verildi" },
            new CheckTransactionType { Code = "5", Description = "Bankaya Tahsile Verildi" },
            new CheckTransactionType { Code = "6", Description = "Bankadan Tahsil Edildi" },
            new CheckTransactionType { Code = "7", Description = "Ciro Edildi" },
            new CheckTransactionType { Code = "8", Description = "Teminata Verildi" },
            new CheckTransactionType { Code = "9", Description = "Karşılıksız" },
            new CheckTransactionType { Code = "10", Description = "İade Edildi" },
            new CheckTransactionType { Code = "11", Description = "İptal Edildi" },
            new CheckTransactionType { Code = "12", Description = "Seçiniz" }
        };

        private string GetSelectedDatabase()
 {return default;
}
        [Route("index")]

        public IActionResult Index()
 {ViewBag.CheckTransactionTypes = WebApplication3.OrnekDoldurucu.Liste<CheckTransactionType>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Rapor2ViewModel>(12));
}

        public class ExportRequestModel
        {
            public List<FilterData> Filters { get; set; } = new List<FilterData>();
            public SortData Sort { get; set; }
        }

        public class FilterData
        {
            public int ColumnIndex { get; set; }
            public string Type { get; set; }
            public List<string> Values { get; set; }
        }

        public class SortData
        {
            public int ColumnIndex { get; set; }
            public string SortType { get; set; }
            public string Direction { get; set; }
        }

        [HttpPost]
        [Route("ExportToExcel")]
        public async Task<IActionResult> ExportToExcel([FromBody] ExportRequestModel requestModel)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
        public class IbrazBelgeUpdateModel
        {
            public string IbrazBelgeNo { get; set; }
            public string NewCheckTransactionType { get; set; }
        }





        [HttpPost]
        [Route("UpdateCheckTransactionTypeByIbrazBelge")]
        public async Task<IActionResult> UpdateCheckTransactionTypeByIbrazBelge([FromBody] IbrazBelgeUpdateModel model)
 {return Json(new { success = true, message = "Çek işlem türü başarıyla güncellendi." });
}
        public class PlannedSupplierUpdateModel
        {
            public int DocEntry { get; set; }
            public string NewPlannedSupplier { get; set; }
        }

        [HttpPost]
        [Route("UpdatePlannedSupplier")]
        public async Task<IActionResult> UpdatePlannedSupplier([FromBody] PlannedSupplierUpdateModel model)
 {return Json(new { success = true, message = "Planlanan tedarikçi başarıyla güncellendi." });
}
        [HttpGet]
        [Route("DownloadAttachment/{ibrazBelgeNo}")]
        public async Task<IActionResult> DownloadAttachment(string ibrazBelgeNo)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
        [HttpGet]
        [Route("SearchSuppliers")]
        public async Task<IActionResult> SearchSuppliers(string term)
 {return Json(global::System.Linq.Enumerable.Range(0, 12).Select(i1 => global::WebApplication3.OrnekDoldurucu.Deger<string>("", i1)).ToList());
}

    }
}