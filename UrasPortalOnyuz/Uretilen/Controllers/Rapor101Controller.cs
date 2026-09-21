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
using System.Globalization;
using ClosedXML.Excel;
using System.IO;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor101Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor101Controller> _logger;

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
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA", DbName = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
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
            new DatabaseConfig { Key = "DefaultConnection31", Display = "AVRASYA_2026", DbName = "AVRASYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" }
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}


        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }

        public class FiltreModel
        {
            public string Kod { get; set; }
            public string Tanim { get; set; }
            public bool Sec { get; set; }
        }

        public class RowData
        {
            public string HesapKodu { get; set; }
            public string AnaHesap { get; set; }
            public string HesapAdi { get; set; }
            public string IslemTipi { get; set; }
            public double BorcTL { get; set; }
            public double AlacakTL { get; set; }
            public string DovizCinsi { get; set; }
            public string BorcDoviz { get; set; }
            public string AlacakDoviz { get; set; }
            public string GrupTipi { get; set; }
            public string KiyasDurum { get; set; }
        }

        public class SystemCalcRequest
        {
            public DateTime BasTarih { get; set; }
            public DateTime BitTarih { get; set; }
            public string HesapGrubu { get; set; }
            public List<string> Filtreler { get; set; }
            public string FarkHesabi { get; set; }
        }

        public class SaveRequest
        {
            public DateTime KayitTarihi { get; set; }
            public DateTime AcilisTarihi { get; set; }
            public bool IsAcilisYapiyor { get; set; }
            public bool IsAcilisOnlyMode { get; set; }
            public string HesapGrubu { get; set; }
            public string FarkHesabi { get; set; }
            public string HedefIslemTipi { get; set; }
            public string AnaAciklama { get; set; }
            public List<RowData> Rows { get; set; }
        }

        public class AccountState
        {
            public string Code { get; set; }
            public string FrozenFor { get; set; }
            public string ValidFor { get; set; }
        }

        private class SapAcc { public string Code { get; set; } public string Name { get; set; } public string Curr { get; set; } public string IsCtrl { get; set; } }

        private class SapBp
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string Curr { get; set; }
            public string DebPayAcct { get; set; }
            public string DflAccount { get; set; }
        }


        private string NormalizeString(string text)
 {return default;
}

        private bool IsNameMatch(string excelName, string sapName)
 {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.Databases = new System.Collections.Generic.List<object>();
ViewBag.Filtreler = new System.Collections.Generic.List<object>();
ViewBag.LookupList = new System.Collections.Generic.List<object>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("ExcelSablonIndir")]
        public IActionResult ExcelSablonIndir()
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}



        private List<FiltreModel> GetAktarimFiltreleri(string dbKey)
 {return default;
}

        [HttpPost("SistemdenHesapla")]
        public IActionResult SistemdenHesapla([FromBody] SystemCalcRequest req)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor101Controller.RowData>(12), summary = new { mizTL = global::WebApplication3.OrnekDoldurucu.Deger<double>("mizTL", 0), mizEU = global::WebApplication3.OrnekDoldurucu.Deger<double>("mizEU", 0), mizUS = global::WebApplication3.OrnekDoldurucu.Deger<double>("mizUS", 0), fisTL = global::WebApplication3.OrnekDoldurucu.Deger<double>("fisTL", 0), fisEU = global::WebApplication3.OrnekDoldurucu.Deger<double>("fisEU", 0), fisUS = global::WebApplication3.OrnekDoldurucu.Deger<double>("fisUS", 0), kalTL = global::WebApplication3.OrnekDoldurucu.Deger<double>("kalTL", 0) }, mizanKontrol = new { kapsam = global::WebApplication3.OrnekDoldurucu.Deger<string>("kapsam", 0), uyumlu = global::WebApplication3.OrnekDoldurucu.Deger<bool>("uyumlu", 0), farkSayisi = global::WebApplication3.OrnekDoldurucu.Deger<int>("farkSayisi", 0), farklar = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "HesapKodu", "AnaHesapKodu", "HesapAdi", "DovizCinsi", "NetTL", "NetFC", "HesapDovizi" }) } });
}





        [HttpPost("ExcelYukle")]
        public IActionResult ExcelYukle(IFormFile file, [FromForm] string farkHesabi, [FromForm] bool isAcilisOnly)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor101Controller.RowData>(12), summary = new { mizTL = global::WebApplication3.OrnekDoldurucu.Deger<double>("mizTL", 0), mizEU = global::WebApplication3.OrnekDoldurucu.Deger<double>("mizEU", 0), mizUS = global::WebApplication3.OrnekDoldurucu.Deger<double>("mizUS", 0), fisTL = global::WebApplication3.OrnekDoldurucu.Deger<double>("fisTL", 0), fisEU = global::WebApplication3.OrnekDoldurucu.Deger<double>("fisEU", 0), fisUS = global::WebApplication3.OrnekDoldurucu.Deger<double>("fisUS", 0), kalTL = global::WebApplication3.OrnekDoldurucu.Deger<double>("kalTL", 0) } });
}

        private double ParseDouble(string val)
 {return default;
}



        private List<RowData> GenerateFinalRows(List<RowData> normalList, Dictionary<string, List<RowData>> kfDict, string farkHesabi, ref double totalKalan)
 {return default;
}



        [HttpPost("YevmiyeKaydet")]
        public async Task<IActionResult> YevmiyeKaydet([FromBody] SaveRequest req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}









        private HashSet<string> GetActiveUDFs(string dbKey, string tableName)
 {return default;
}




        private static void EkleUdfEgerVarsa(Dictionary<string, object> govde, HashSet<string> aktifAlanlar,
                                             string alanAdi, string gonderilecekAd, string deger)
 {}

        private List<object> BuildJournalLines_Saf_TL(List<RowData> rows, Dictionary<string, (string Curr, string IsCtrl)> dictAcc, Dictionary<string, (string Curr, string DebPayAcct, string DflAccount)> dictBp, Dictionary<string, string> ctrlAccToBp, string localCurrency, bool isAcilis, bool isKurFarki, string kfCur)
 {return default;
}




        private List<object> BuildJournalLines_Normal(List<RowData> rows, Dictionary<string, (string Curr, string IsCtrl)> dictAcc, Dictionary<string, (string Curr, string DebPayAcct, string DflAccount)> dictBp, Dictionary<string, string> ctrlAccToBp, string localCurrency, bool isAcilis, bool isKurFarki, string kfCur)
 {return default;
}




        private List<object> BuildJournalLines_Ozel(List<RowData> rows, Dictionary<string, (string Curr, string IsCtrl)> dictAcc, Dictionary<string, (string Curr, string DebPayAcct, string DflAccount)> dictBp, Dictionary<string, string> ctrlAccToBp, string localCurrency, bool isAcilis, bool isKurFarki, string kfCur, bool isKarmaYevmiye)
 {return default;
}

    }
}