// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using WebApplication3.Models;

using WebApplication3.Services;
namespace WebApplication3.Models
{
    public class UrunKarsilastirmaViewModel
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; } // ANA ÜRÜN İSMİ


        public string FaaliyetAnaGrubu { get; set; }
        public string FaaliyetGrubu { get; set; }
        public string UretimDirektoru { get; set; }
        public string UrunSinifi { get; set; }
        public string UrunGrubu { get; set; }
        public string UrunSerisi { get; set; }


        public string UrasKimyaUrunAdi { get; set; }
        public string UrasKimyaUrunKodu { get; set; }

        public string AlvKimyaUrunAdi { get; set; }
        public string AlvKimyaUrunKodu { get; set; }

        public string InknovatorsUrunAdi { get; set; }
        public string InknovatorsUrunKodu { get; set; }

        public string SelviKimyaUrunAdi { get; set; }
        public string SelviKimyaUrunKodu { get; set; }

        public string AsyaUrunAdi { get; set; }
        public string AsyaUrunKodu { get; set; }

        public string AvrasyaUrunAdi { get; set; }
        public string AvrasyaUrunKodu { get; set; }

        public string DrnUrunAdi { get; set; }
        public string DrnUrunKodu { get; set; }

        public string UrasBaskiUrunAdi { get; set; }
        public string UrasBaskiUrunKodu { get; set; }
    }

    public class MassUpdateHierarchyRequest
    {
        public List<string> ItemCodes { get; set; }
        public string FaaliyetAnaGrubu { get; set; }
        public string FaaliyetGrubu { get; set; }
        public string UretimDirektoru { get; set; }
        public string UrunSinifi { get; set; }
        public string UrunGrubu { get; set; }
        public string UrunSerisi { get; set; }
    }


}

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor139Controller : Controller
    {
        private readonly IConfiguration _configuration;


        private readonly string _slBaseUrl = "https://localhost:50000/b1s/v1";


        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },      // Inknovators
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },          // Selvi Kimya
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },     // Asya
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S" },
            new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },           // DRN (19 YAPILDI)
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },     // URAS BASKI (26 YAPILDI)

            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };


        private readonly string[] _requiredUDFs = new[]
        {
            "U_F_AnaGrb", "U_F_Grb", "U_U_Dir", "U_U_Snf", "U_U_Grb", "U_U_Seri",
            "U_UK_Ad", "U_UK_Kod", "U_ALV_Ad", "U_ALV_Kod", "U_INK_Ad", "U_INK_Kod",
            "U_SLV_Ad", "U_SLV_Kod", "U_ASY_Ad", "U_ASY_Kod", "U_AVR_Ad", "U_AVR_Kod",
            "U_DRN_Ad", "U_DRN_Kod", "U_URB_Ad", "U_URB_Kod"
        };



        private string GetSelectedDatabase()  {return default;
}
        private string GetConnectionString(string dbKey)  {return default;
}


        private async Task<(string SessionId, string ErrorMessage)> GetServiceLayerSessionIdForCompany(string dbKey)
 {return default;
}


        private List<string> CheckMissingUDFsAcrossAllCompanies()
 {return default;
}





        [HttpGet]
        public IActionResult Index(string searchQuery = "")
 {ViewBag.UdfWarnings = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.UrunKarsilastirmaViewModel>(12));
}





        [HttpGet("SearchItems")]
        public IActionResult SearchItems(string term, string dbKey)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName" }));
}





        [HttpPost("UpdateCompanyProductMapping")]
        public async Task<IActionResult> UpdateCompanyProductMapping(
            string itemCode, string mainItemName, string targetDbKey,
            string targetCodeFieldName, string mappedItemCode,
            string targetNameFieldName, string mappedItemName)
 {return Json(new { success = true, message = "Eşleştirme başarıyla kaydedildi." });
}





        [HttpPost("UpdateField")]
        public async Task<IActionResult> UpdateField(string itemCode, string fieldName, string value)
 {return Json(new { success = true, message = "Başarıyla güncellendi." });
}





        [HttpPost("MassUpdateHierarchy")]
        public async Task<IActionResult> MassUpdateHierarchy([FromBody] MassUpdateHierarchyRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        [HttpPost("SetupAllCompanyUDFs")]
        public async Task<IActionResult> SetupAllCompanyUDFs()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


    }
}