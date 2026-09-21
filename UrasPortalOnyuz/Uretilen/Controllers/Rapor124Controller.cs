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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("Rapor124")]
    public class Rapor124Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor124Controller> _logger;
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

        private string GetSelectedDatabase()
 {return default;
}

        private string GetCompanyLogoPath(string companyName)
 {return default;
}

        [HttpGet]
        public async Task<IActionResult> Index()
 {ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.DatabaseConfig>(12);
ViewBag.SelectedDatabaseKey = "";
ViewBag.CurrentDbDisplay = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Rapor124ViewModel>(12));
}

        [HttpPost]
        [Route("CreateTahsilatBordro")]
        public async Task<IActionResult> CreateTahsilatBordro([FromBody] List<int> docEntries)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpPost]
        [Route("CreateIbrazBordro")]
        public async Task<IActionResult> CreateIbrazBordro([FromBody] List<int> docEntries)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}


namespace WebApplication3.Models
{
    public class Rapor124ViewModel
    {
        public int DocEntry { get; set; }
        public string CekNumarasi { get; set; }
        public decimal? Tutar { get; set; }
        public string ParaBirimi { get; set; }
        public DateTime? VadeTarihi { get; set; }
        public string CekIptal { get; set; }
        public string TahsilatBelgeNo { get; set; }
        public string TahsilatDurumu { get; set; }
        public string TahsilatIade { get; set; }
        public string TahsilatYevmiyeNo { get; set; }
        public string TahsilatYevIptal { get; set; }
        public string TahsilatIptalBelgeNo { get; set; }
        public string TahsilatIptalBelgeIade { get; set; }
        public string TahsilatIptalYevmiyeNo { get; set; }
        public string TahsilatIptalYevIptal { get; set; }
        public string IbrazBelgeNo { get; set; }
        public string IbrazDurumu { get; set; }
        public string IbrazYevmiyeNo { get; set; }
        public string IbrazYevIptal { get; set; }
        public string VadeliIbrazNo { get; set; }
        public string VadeliIbrazDurumu { get; set; }
        public string VadeliIbrazYevmiyeNo { get; set; }
        public string VadeliIbrazYevIptal { get; set; }
    }

    public class BordroCekDetay
    {
        public string CekNo { get; set; }
        public DateTime Tarih { get; set; }
        public string Banka { get; set; }
        public string Sube { get; set; }
        public string Musteri { get; set; }
        public string AsilBorclu { get; set; }
        public decimal CekTutari { get; set; }
        public string ParaBirimi { get; set; }
        public string IbrazBelgeNo { get; set; }
        public DateTime? IbrazTarihi { get; set; }
        public DateTime? TahsilatTarihi { get; set; }
        public string TahsilatBelgeNo { get; set; }
        public string CekKimeVerildi { get; set; }
    }
}