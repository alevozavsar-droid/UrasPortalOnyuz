// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.Primitives;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{

    [Route("[controller]")]
    public class Rapor125Controller : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "Avrasya" },
            new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
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

        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index(int? year, int? month)
 {ViewBag.CurrentDbDisplay = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.DengeMalzemeViewModel>(12));
}

        private List<DengeMalzemeViewModel> GetRaporData(string connectionString, DateTime baslangic, DateTime bitis)
 {return default;
}

        [HttpGet("GetHareketDetay")]
        public IActionResult GetHareketDetay(string itemCode, string kolonTipi, int year, int month)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.DengeMalzemeDetayViewModel>(12));
}
    }
}

namespace WebApplication3.Models
{
    public class DengeMalzemeViewModel
    {
        public string KalemKodu { get; set; }
        public string KalemTanimi { get; set; }

        public decimal DevirMiktari { get; set; }
        public decimal DevirTutari { get; set; }
        public decimal DevirBirimFiyati { get; set; }

        public decimal AlisMiktari { get; set; }
        public decimal AlisTutari { get; set; }
        public decimal SatisIadeMiktari { get; set; }
        public decimal SatisIadeTutari { get; set; }
        public decimal UretimGirisMiktari { get; set; }
        public decimal UretimGirisTutari { get; set; }


        public decimal ToplamGirisMiktari { get; set; }
        public decimal ToplamGirisTutari { get; set; }

        public decimal SatisMiktari { get; set; }
        public decimal SatisTutari { get; set; }
        public decimal AlisIadeMiktari { get; set; }
        public decimal AlisIadeTutari { get; set; }
        public decimal UretimeCikisMiktari { get; set; }
        public decimal UretimeCikisTutari { get; set; }


        public decimal ToplamCikisMiktari { get; set; }
        public decimal ToplamCikisTutari { get; set; }

        public decimal KalanMiktar { get; set; }
        public decimal KalanTutar { get; set; }
        public decimal OrtalamaMaliyet { get; set; }
    }

    public class DengeMalzemeDetayViewModel
    {
        public string BelgeTarihi { get; set; }
        public string BelgeTipi { get; set; }
        public string BelgeNo { get; set; }
        public string CariKodu { get; set; }
        public string CariAdi { get; set; }
        public decimal Miktar { get; set; }
        public decimal Tutar { get; set; }
    }
}