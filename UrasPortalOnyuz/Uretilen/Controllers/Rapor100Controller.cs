// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor100Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor100Controller> _logger;


        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya", DbName = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S", DbName = "URSMAKINE__A.S" },
            new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S", DbName = "ALVKIMYA_A.S" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS", DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS", DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS", DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S", DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S", DbName = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS", DbName = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.AlanMesaji = "";
ViewBag.Items = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor100Controller.LookupModel>(12);
ViewBag.ItemGroups = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor100Controller.LookupModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetItemDetails")]
        public IActionResult GetItemDetails(string itemCode)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor100Controller.ItemFullModel>() });
}

        [HttpPost("UpdateItem")]
        public async Task<IActionResult> UpdateItem([FromBody] ItemFullModel req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private List<LookupModel> GetItems(string dbKey)
 {return default;
}

        private List<LookupModel> GetItemGroups(string dbKey)
 {return default;
}

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class LookupModel { public int Id { get; set; } public string Code { get; set; } public string Name { get; set; } }

        public class ItemFullModel
        {
            public bool IsNew { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string FrgnName { get; set; }
            public int ItmsGrpCod { get; set; }
            public string CodeBars { get; set; }

            public bool InvntItem { get; set; }
            public bool SellItem { get; set; }
            public bool PrchseItem { get; set; }
            public bool ManSerNum { get; set; }
            public bool ManBtchNum { get; set; }
            public bool ValidFor { get; set; }

            public decimal OnHand { get; set; }
            public decimal IsCommited { get; set; }
            public decimal OnOrder { get; set; }


            public string U_BE1_GTIP { get; set; }
            public string U_BE1_MENSEI { get; set; }
            public string U_BE1_GTIPNAME { get; set; }
            public string U_BE1_CPALISTVERSION { get; set; }
            public string U_BE1_GTIPLISTVERSION { get; set; }
            public string U_BE1_CPA { get; set; }
            public string U_BE1_UBBCODE { get; set; }
            public string U_BE1_IkOB { get; set; }
            public string U_BE1_IkOBm { get; set; }
            public string U_BE1_ORIGIN { get; set; }
            public string U_BE1_HZRMLZM { get; set; }
            public string U_BE1_ALTGRUP1 { get; set; }
            public string U_BE1_ALTGRUP2 { get; set; }
            public string U_BE1_ALTGRUP3 { get; set; }
            public string U_BE1_ALTGRUP4 { get; set; }
            public string U_BE1_ALTGRUP5 { get; set; }
            public string U_BE1_DEGISKEN { get; set; }
            public string U_BE1_AT { get; set; }
            public string U_BE1_FAYDA { get; set; }
            public string U_BE1_SK { get; set; }
            public string U_BE1_DAB { get; set; }
            public string U_BE1_DBD { get; set; }
            public string U_BE1_KAPALI { get; set; }
            public string U_BE1_DAT { get; set; }
            public string U_BE1_YENI { get; set; }
            public string U_BE1_DSD { get; set; }
            public string U_BE1_3112 { get; set; }
            public string U_BE1_257 { get; set; }
            public string U_BE1_062024 { get; set; }
            public string U_BE1_BIRIK257 { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_CASNO { get; set; }
            public string U_BE1_KKDIK { get; set; }
            public string U_BE1_REACH { get; set; }
            public string U_BE1_OECO { get; set; }
            public string U_BE1_ZDHC { get; set; }
            public string U_BE1_ADR { get; set; }
            public string U_BE1_KRTSTK { get; set; }
            public string U_BE1_BOYUTA { get; set; }
            public string U_BE1_BCORAN { get; set; }
            public string U_BE1_MVGACIKLAMA { get; set; }
            public string U_BE1_URETIMYN { get; set; }
            public string U_BE1_KULLANIMD { get; set; }
            public string U_BE1_KARLILIKRA { get; set; }
            public string U_BE1_LKACIKLAMA { get; set; }
            public string U_BE1_SKACIKLAMA { get; set; }
            public string U_BE1_DRGACIKLAMA { get; set; }
            public string U_BE1_KSKGACIKLAMA { get; set; }
            public string U_BE1_OZELLIK1A { get; set; }
            public string U_BE1_MDRACIKLAMA { get; set; }
            public string U_BE1_OZELLIK2A { get; set; }
            public string U_BE1_SEGMENTKOD { get; set; }
            public string U_BE1_FURL { get; set; }
            public string U_BE1_OZELLIK1 { get; set; }
            public string U_BE1_MDGRAPORU { get; set; }
            public string U_BE1_OZELLIK2 { get; set; }
            public string U_BE1_MVERGIGRUBU { get; set; }
            public string U_BE1_KARLILIKR { get; set; }
            public string U_BE1_LOTK { get; set; }
            public string U_BE1_DRGRUBU { get; set; }
            public string U_BE1_KSGKODU { get; set; }
            public string U_BE1_UTACIKLAMA { get; set; }
            public string U_BE1_MAGAZA { get; set; }
            public string U_BE1_INTERNET { get; set; }
            public string U_BE1_URETIM { get; set; }
            public string U_BE1_OZELLIK7A { get; set; }
            public string U_BE1_UDRACIKLAMA { get; set; }
            public string U_BE1_UTARACIKLAMA { get; set; }
            public string U_BE1_SKTACIKLAMA { get; set; }
            public string U_BE1_UGACIKLAMA { get; set; }
            public string U_BE1_KRACIKLAMA { get; set; }
            public string U_BE1_OZELLIK3A { get; set; }
            public string U_BE1_URUNTIP { get; set; }
            public string U_BE1_OZELLIK3 { get; set; }
            public string U_BE1_KAPASITER { get; set; }
            public string U_BE1_OZELLIK7 { get; set; }
            public string U_BE1_UDRAPOR { get; set; }
            public string U_BE1_MMHESAPG { get; set; }
            public string U_BE1_MVG { get; set; }
            public string U_BE1_URUNGRUP { get; set; }
            public string U_BE1_ANAKTGR { get; set; }
            public string U_BE1_MARKA { get; set; }
            public string U_BE1_PACKAGINGTYPECODE { get; set; }
            public string U_BE1_YERLILIK { get; set; }
        }
    }
}