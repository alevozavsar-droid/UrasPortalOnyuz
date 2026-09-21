// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using Microsoft.Extensions.Primitives;
using System.Text;
using ClosedXML.Excel;
using System.Net.Mime;

namespace WebApplication3.Controllers
{

    public class SatisItemViewModel
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
    }

    public class SatisCariViewModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
    }

    public class SatisEkstreViewModel
    {
        public DateTime? KayitTarihi { get; set; }
        public int BelgeNo { get; set; }
        public string IslemTipi { get; set; }
        public string AktarimTipi { get; set; }
        public string MuhatapKodu { get; set; }
        public string MuhatapAdi { get; set; }
        public string KalemKodu { get; set; }
        public string KalemAdi { get; set; }

        public string Birim { get; set; }
        public decimal GuncelStok { get; set; }
        public decimal Miktar { get; set; }
        public decimal TonMiktari { get; set; }

        public decimal Fiyat { get; set; }
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; }


        public decimal UsdKarsiligi { get; set; }
        public decimal EurKarsiligi { get; set; }
    }

    public class Rapor74DatabaseConfig
    {
        public string Key { get; set; }
        public string Display { get; set; }
    }

    [Authorize]
    [Route("[controller]")]
    public class Rapor74Controller : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly List<Rapor74DatabaseConfig> _databases = new List<Rapor74DatabaseConfig>
        {
            new Rapor74DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new Rapor74DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" }
        };

        private string GetSelectedDatabase()
 {return default;
}

        private List<SatisItemViewModel> GetItemList(string connectionString)
 {return default;
}

        private List<SatisCariViewModel> GetCariList(string connectionString)
 {return default;
}

        private List<string> GetAktarimTipiList(string connectionString)
 {return default;
}

        [Authorize]
        public IActionResult Index([FromQuery] List<string> itemCode, [FromQuery] List<string> bpCode, DateTime? startDate, DateTime? endDate, [FromQuery] List<string> aktarimTipi)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.SelectedItemCodes = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.SelectedBpCodes = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.SelectedAktarimTipi = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.ItemList = WebApplication3.OrnekDoldurucu.Liste<SatisItemViewModel>(12);
ViewBag.CariList = WebApplication3.OrnekDoldurucu.Liste<SatisCariViewModel>(12);
ViewBag.AktarimTipiList = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.SatisEkstreViewModel>(12));
}

        private List<SatisEkstreViewModel> GetSatisEkstreData(string connectionString, List<string> itemCode, List<string> bpCode, DateTime? startDate, DateTime? endDate, List<string> aktarimTipi)
 {return default;
}

        [HttpGet("downloadexcel")]
        [Authorize]
        public IActionResult DownloadExcel([FromQuery] List<string> itemCode, [FromQuery] List<string> bpCode, DateTime? startDate, DateTime? endDate, [FromQuery] List<string> aktarimTipi)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("downloadpdf")]
        [Authorize]
        public IActionResult DownloadPdf([FromQuery] List<string> itemCode, [FromQuery] List<string> bpCode, DateTime? startDate, DateTime? endDate, [FromQuery] List<string> aktarimTipi)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}