// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{





    [Authorize]
    [Route("Rapor169")]
    public class Rapor169Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor169Controller> _logger;

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
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        [HttpGet("Index")]
        public IActionResult Index()
 {ViewBag.CurrentDbDisplay = "";
ViewBag.CurrentDbKey = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        public class TaslakSatir
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public DateTime DocDate { get; set; }
            public DateTime? DocDueDate { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string DocCur { get; set; }
            public decimal DocTotal { get; set; }
            public decimal DocTotalFC { get; set; }
            public string Comments { get; set; }
            public string Olusturan { get; set; }
            public DateTime? CreateDate { get; set; }
            public string FaturaTipi { get; set; }
            public string MuafKodu { get; set; }
            public string Ihracat { get; set; }
            public string IhracatDosyaNo { get; set; }
            public string Incoterm { get; set; }
            public string TasimaSekli { get; set; }
            public string KapCinsi { get; set; }
            public string KapAdedi { get; set; }
            public string KapNo { get; set; }
            public int? IrsaliyeNo { get; set; }
            public int? SiparisNo { get; set; }
            public int? FaturaDocEntry { get; set; }
            public int? FaturaNo { get; set; }
            public string Durum => FaturaNo.HasValue ? "CEVRILDI" : "ACIK";
        }

        private async Task<List<TaslakSatir>> TaslaklariGetirAsync(string dbKey, DateTime baslangic, DateTime bitis, string ara, string durum, bool sadeceIhracat)
 {return default;
}


        [HttpGet("Liste")]
        public async Task<IActionResult> Liste(DateTime? baslangic, DateTime? bitis, string ara, string durum = "ACIK", bool ihracat = false)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor169Controller.TaslakSatir>(12), acik = global::WebApplication3.OrnekDoldurucu.Deger<int>("acik", 0), cevrildi = global::WebApplication3.OrnekDoldurucu.Deger<int>("cevrildi", 0) });
}


        [HttpGet("Detay")]
        public async Task<IActionResult> Detay(int docEntry)
 {return Json(new { success = true, baslik = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("baslik", 0), satirlar = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate", "CardCode", "CardName", "DocCur", "DocTotal", "DocTotalFC", "VatSum", "Comments", "FaturaTipi", "MuafKodu", "Ihracat", "IhracatDosyaNo", "Incoterm", "TasimaSekli", "KapCinsi", "KapAdedi", "KapNo", "KapMarka", "OdemeSekli", "OdemeKanali", "Iban", "Sofor", "Plaka", "Nakliyeci", "FaturaNo" }) });
}

        [HttpGet("Excel")]
        public async Task<IActionResult> Excel(DateTime? baslangic, DateTime? bitis, string ara, string durum = "ACIK", bool ihracat = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}
