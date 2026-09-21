// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor77Controller : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
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

        [HttpGet]
        public IActionResult Index()
 {ViewBag.DynamicColumns = WebApplication3.OrnekDoldurucu.Liste<int>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Rapor77ViewModel>(12));
}

        private decimal GetTodayUsdRate(string connectionString)
 {return default;
}


        private List<Rapor77RawData> GetRawItemData(string connectionString, string dbDisplay)
 {return default;
}
        private List<Rapor77ViewModel> PivotDataForExcelView(List<Rapor77RawData> rawData)
 {return default;
}
    }
}

namespace WebApplication3.Models
{
    
 

    public class Rapor77RawData
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public int AmbalajKilo { get; set; }
        public decimal Maliyet { get; set; }
        public decimal AmbalajsizMaliyet { get; set; }
        public bool HasZeroCostComponent { get; set; }
        public bool HasZeroCostPureComponent { get; set; }
    }

    public class AmbalajHucresi
    {
        public decimal Maliyet { get; set; }
        public bool SifirMaliyetliKalemVarMi { get; set; }
    }

    public class Rapor77ViewModel
    {
        public string AnaUrunAdi { get; set; }

        public decimal? Ambalajsiz1Kg { get; set; }
        public bool AmbalajsizSifirMaliyetliKalemVarMi { get; set; }

        public Dictionary<int, AmbalajHucresi> AmbalajMaliyetleri { get; set; } = new Dictionary<int, AmbalajHucresi>();

        public string EnUygunAmbalaj
        {
            get
            {
                if (AmbalajMaliyetleri == null || !AmbalajMaliyetleri.Any()) return "-";

                var gecerliMaliyetler = AmbalajMaliyetleri.Where(x => !x.Value.SifirMaliyetliKalemVarMi && x.Value.Maliyet > 0).ToList();

                if (!gecerliMaliyetler.Any()) return "Hesaplanamadı";

                var minCost = gecerliMaliyetler.Min(x => x.Value.Maliyet);

                var bestPackage = gecerliMaliyetler
                                    .Where(x => x.Value.Maliyet == minCost)
                                    .OrderByDescending(x => x.Key)
                                    .FirstOrDefault().Key;

                return $"{bestPackage} KG";
            }
        }
    }
}