// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("CekAkibeti")]
    public class Rapor2Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor2Controller> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

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

        private readonly List<CheckTransactionType> _checkTransactionTypes = new List<CheckTransactionType>
        {
            new CheckTransactionType { Code = "0", Description = "Seçiniz" },
            new CheckTransactionType { Code = "1", Description = "Portföyde" },
            new CheckTransactionType { Code = "2", Description = "Planlandı" },
            new CheckTransactionType { Code = "3", Description = "Erteleme Yapıldı - Portföyde" },
            new CheckTransactionType { Code = "4", Description = "Erteleme Yapıldı - Bankada" },
            new CheckTransactionType { Code = "5", Description = "Bankaya Tahsile Verildi" },
            new CheckTransactionType { Code = "6", Description = "Bankadan Tahsil Edildi" },
            new CheckTransactionType { Code = "7", Description = "Elden Tahsil Edildi" },
            new CheckTransactionType { Code = "8", Description = "Ciro Edildi" },
            new CheckTransactionType { Code = "9", Description = "Teminata Verildi" },
            new CheckTransactionType { Code = "10", Description = "Karşılıksız" },
            new CheckTransactionType { Code = "11", Description = "Protesto Edildi" },
            new CheckTransactionType { Code = "12", Description = "Konkordato - Portföyde" },
            new CheckTransactionType { Code = "13", Description = "Konkordato - Bankada" },
            new CheckTransactionType { Code = "14", Description = "Konkordato - Tedarikçide" },
            new CheckTransactionType { Code = "15", Description = "İade Edildi" },
            new CheckTransactionType { Code = "16", Description = "İptal Edildi" }
        };

        private string GetSelectedDatabase()
 {return default;
}

        public async Task<IActionResult> Index2(string yil = null)
 {ViewBag.CheckTransactionTypes = WebApplication3.OrnekDoldurucu.Liste<CheckTransactionType>(12);
ViewBag.MevcutYillar = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index2", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Rapor2ViewModel>(12));
}

        private List<string> GetSortedMonthYearList(IEnumerable<string> items)
 {return default;
}





        [HttpGet("GetTahsilatDetay")]
        public async Task<IActionResult> GetTahsilatDetay(string no)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetIbrazDetay")]
        public async Task<IActionResult> GetIbrazDetay(string no)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class ExportRequestModel { public List<FilterData> Filters { get; set; } = new List<FilterData>(); public SortData Sort { get; set; } }
        public class FilterData { public int ColumnIndex { get; set; } public string Type { get; set; } public List<string> Values { get; set; } }
        public class SortData { public int ColumnIndex { get; set; } public string SortType { get; set; } public string Direction { get; set; } }

        public class KullanımDurumuUpdateModel
        {
            public int DocEntry { get; set; }
            public string NewKullanimDurumu { get; set; }
        }

        [HttpPost]
        [Route("UpdateKullanimDurumu")]
        public async Task<IActionResult> UpdateKullanimDurumu([FromBody] KullanımDurumuUpdateModel model)
 {return Json(new { success = true, message = "Kullanım Durumu başarıyla güncellendi." });
}

        public class IbrazBelgeUpdateModel { public string IbrazBelgeNo { get; set; } public string NewCheckTransactionType { get; set; } }

        [HttpPost]
        [Route("UpdateCheckTransactionTypeByIbrazBelge")]
        public async Task<IActionResult> UpdateCheckTransactionTypeByIbrazBelge([FromBody] IbrazBelgeUpdateModel model)
 {return Json(new { success = true, message = "Çek işlem türü başarıyla güncellendi." });
}

        public class PlannedSupplierUpdateModel { public int DocEntry { get; set; } public string NewPlannedSupplier { get; set; } }

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

        public class TahsilatIslemTuruUpdateModel
        {
            public int TahsilatBelgeNo { get; set; }
            public string NewCheckTransactionType { get; set; }
        }

        [HttpPost]
        [Route("UpdateCheckTransactionTypeByTahsilat")]
        public async Task<IActionResult> UpdateCheckTransactionTypeByTahsilat([FromBody] TahsilatIslemTuruUpdateModel model)
 {return Json(new { success = true, message = "Tahsilat işlem türü başarıyla güncellendi." });
}





        public class ReportTemplateModel
        {
            public string RaporAdi { get; set; }
            public string SablonVerisi { get; set; }
        }

        [HttpPost]
        [Route("SaveUserTemplate")]
        public async Task<IActionResult> SaveUserTemplate([FromBody] ReportTemplateModel model)
 {return Json(new { success = true });
}

        [HttpGet]
        [Route("GetUserTemplate")]
        public async Task<IActionResult> GetUserTemplate(string raporAdi)
 {return Json(null);
}





        public class UyumsuzIslemModel
        {
            public string IslemKaynagi { get; set; }
            public string BelgeNo { get; set; }
            public string BelgeTarihi { get; set; }
            public string BelgedekiAktar { get; set; }
            public string YevmiyeNo { get; set; }
            public string YevmiyedekiAktar { get; set; }
            public string YevmiyeAciklamasi { get; set; }
        }

        [HttpGet]
        [Route("GetFarkliIslemler")]
        public async Task<IActionResult> GetFarkliIslemler()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor2Controller.UyumsuzIslemModel>(12) });
}

        public class SyncUBe1AktarModel
        {
            public string IslemKaynagi { get; set; }
            public string BelgeNo { get; set; }
            public int YevmiyeNo { get; set; }
            public string AktarDegeri { get; set; }
            public string Direction { get; set; } // "ToYevmiye" veya "ToBelge"
        }

        [HttpPost]
        [Route("SyncUBe1Aktar")]
        public async Task<IActionResult> SyncUBe1Aktar([FromBody] SyncUBe1AktarModel model)
 {return Json(new { success = true, message = "Kayıt başarıyla eşitlendi." });
}
    }
}