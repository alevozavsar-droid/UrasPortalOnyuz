// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor12Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor12Controller> _logger;
        private readonly string _connectionString;

        [HttpGet("{reportType?}/{year:int?}")]
        public async Task<IActionResult> Index(string reportType = "Ciro", int? year = 2025)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetDetail")]
        public async Task<IActionResult> GetDetail(string reportType, int year, int month, string rowName)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor12Controller.CiroDetailData>(12), summary = new { prevYear = global::WebApplication3.OrnekDoldurucu.Deger<int>("prevYear", 0), currYear = global::WebApplication3.OrnekDoldurucu.Deger<int>("currYear", 0), prevTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("prevTL", 0), currTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("currTL", 0), diffTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("diffTL", 0), percTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("percTL", 0), prevUSD = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("prevUSD", 0), currUSD = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("currUSD", 0), diffUSD = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("diffUSD", 0), percUSD = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("percUSD", 0) } });
}

        [HttpGet("GetPreviousYearData")]
        public async Task<IActionResult> GetPreviousYearData(string reportType, int currentYear)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor12Controller.CiroStandartData>(12) });
}




        private string GetCiroByCurrencySQL(int reportYear, string targetCurrency)
 {return default;
}




        private string GetStandartCiroSQL(int reportYear)
 {return default;
}




        private string GetCiroDetailSQL(int reportYear, int month, string targetCurrency, string rowName)
 {return default;
}

        private async Task<List<CiroDovizliData>> ExecuteCiroDovizliQuery(string sqlQuery)
 {return default;
}

        private async Task<List<CiroStandartData>> ExecuteStandartCiroQuery(string sqlQuery, string reportType)
 {return default;
}






        private string GetCiroDovizliSQL(int reportYear)
 {return default;
}
        public class CiroDovizliData
        {
            public string Aciklama { get; set; }
            public string Ocak { get; set; }
            public string Subat { get; set; }
            public string Mart { get; set; }
            public string Nisan { get; set; }
            public string Mayis { get; set; }
            public string Haziran { get; set; }
            public string Temmuz { get; set; }
            public string Agustos { get; set; }
            public string Eylul { get; set; }
            public string Ekim { get; set; }
            public string Kasim { get; set; }
            public string Aralik { get; set; }
            public string Toplam { get; set; }
        }

        public class CiroStandartData
        {
            public string IslemTipi { get; set; }
            public string RaporTuru { get; set; }
            public decimal Ocak { get; set; }
            public decimal Subat { get; set; }
            public decimal Mart { get; set; }
            public decimal Nisan { get; set; }
            public decimal Mayis { get; set; }
            public decimal Haziran { get; set; }
            public decimal Temmuz { get; set; }
            public decimal Agustos { get; set; }
            public decimal Eylul { get; set; }
            public decimal Ekim { get; set; }
            public decimal Kasim { get; set; }
            public decimal Aralik { get; set; }
            public decimal Toplam { get; set; }
            public decimal YillikOrtalama { get; set; }
            public decimal Sira { get; set; }
        }

        public class CiroDetailData
        {
            public string Sirket { get; set; }
            public string BelgeNo { get; set; }
            public string Tarih { get; set; }
            public string CariKodu { get; set; }
            public string CariAdi { get; set; }
            public decimal Tutar { get; set; }
            public string ParaBirimi { get; set; }
        }
    }
}