// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApplication3.Controllers
{
















    [Authorize]
    [Route("Rapor147")]
    public class Rapor147Controller : Controller
    {
        private readonly IConfiguration _yapilandirma;
        private readonly ILogger<Rapor147Controller> _gunluk;

        public class R147DatabaseConfig
        {
            public string Key { get; set; }
            public string Display { get; set; }
        }

        private readonly List<R147DatabaseConfig> _databases = new List<R147DatabaseConfig>
        {
            new R147DatabaseConfig { Key = "DefaultConnection",   Display = "URASKIMYA" },
            new R147DatabaseConfig { Key = "DefaultConnection1",  Display = "URSMAKINE" },
            new R147DatabaseConfig { Key = "DefaultConnection2",  Display = "AVRUPA_PAPER" },
            new R147DatabaseConfig { Key = "DefaultConnection3",  Display = "ALV_KIMYA" },
            new R147DatabaseConfig { Key = "DefaultConnection4",  Display = "DAF_KIMYA" },
            new R147DatabaseConfig { Key = "DefaultConnection5",  Display = "SELVI" },
            new R147DatabaseConfig { Key = "DefaultConnection6",  Display = "ALVFILO" },
            new R147DatabaseConfig { Key = "DefaultConnection7",  Display = "AVRASYA" },
            new R147DatabaseConfig { Key = "DefaultConnection8",  Display = "ASIA_KIMYA" },
            new R147DatabaseConfig { Key = "DefaultConnection9",  Display = "DEKORLIM" },
            new R147DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new R147DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new R147DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new R147DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new R147DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new R147DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new R147DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new R147DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new R147DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new R147DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new R147DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new R147DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new R147DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new R147DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new R147DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new R147DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new R147DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new R147DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
            new R147DatabaseConfig { Key = "DefaultConnection31", Display = "AVRASYA_2026" },
            new R147DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new R147DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new R147DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new R147DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new R147DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" }
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string BaglantiDizesi(string dbKey)  {return default;
}

        public class BakiyeSatir
        {
            public string Grup { get; set; }          // Kasa / Banka
            public string HesapKodu { get; set; }
            public string HesapAdi { get; set; }


            public string ParaBirimi { get; set; }



            public bool CokParaBirimli { get; set; }

            public decimal DevirTL { get; set; }
            public decimal BorcTL { get; set; }
            public decimal AlacakTL { get; set; }
            public decimal SonTL { get; set; }

            public decimal DevirDoviz { get; set; }
            public decimal BorcDoviz { get; set; }
            public decimal AlacakDoviz { get; set; }
            public decimal SonDoviz { get; set; }
        }

        [HttpGet("")]
        public IActionResult Index()
 {ViewBag.IslemTipleri = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        private List<string> IslemTipleriniGetir(string dbKey)
 {return default;
}

        [HttpGet("Veri")]
        public async Task<IActionResult> Veri(string bas, string bit, string islemTipi,
                                              bool sifirlariGoster = false, bool devirDahil = true)
 {return Json(new { success = true, satirlar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor147Controller.BakiyeSatir>(12), ozet = new { kasa = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("kasa", 0), banka = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("banka", 0), kasaDevir = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("kasaDevir", 0), bankaDevir = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("bankaDevir", 0) } });
}

        private async Task<List<BakiyeSatir>> BakiyeleriGetirAsync(
            string dbKey, DateTime bas, DateTime bit, string islemTipi, bool sifirlariGoster, bool devirDahil = true)
 {return default;
}

        [HttpGet("Excel")]
        public async Task<IActionResult> Excel(string bas, string bit, string islemTipi,
                                               bool sifirlariGoster = false, bool devirDahil = true)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}
