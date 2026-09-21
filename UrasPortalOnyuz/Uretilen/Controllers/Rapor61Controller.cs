// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using ClosedXML.Excel;
using System.IO;
using Microsoft.Extensions.Primitives;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Rendering; // SelectListItem için gerekli
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor61Controller : Controller
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
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
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

        private string GetConnectionString(string dbKey)  {return default;
}


        private List<SelectListItem> GetMaterialList(string connectionString)
 {return default;
}


        public IActionResult Index(DateTime? startDate, DateTime? endDate, List<string> selectedItems)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.MaterialList = WebApplication3.OrnekDoldurucu.Liste<SelectListItem>(12);
ViewBag.SelectedItems = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor61ViewModel>(12));
}

        private List<Rapor61ViewModel> GetRaporData(string connectionString, DateTime startDate, DateTime endDate, List<string> selectedItems)
 {return default;
}

        private decimal ParseDecimal(object value)
 {return default;
}

        private int ParseInt(object value)
 {return default;
}

        [HttpGet("Export")]
        public IActionResult ExportToExcel(DateTime? startDate, DateTime? endDate, List<string> selectedItems)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }

    public class Rapor61ViewModel
    {
        public string MalzemeKodu { get; set; }
        public string MalzemeAdi { get; set; }
        public string BirimCinsi { get; set; }
        public DateTime IslemTarihi { get; set; }
        public DateTime BelgeTarihi { get; set; }
        public string IadeDurumu { get; set; }
        public string SurecAciklamasi { get; set; }
        public string StokHareketSekli { get; set; }
        public string BelgeRefNo { get; set; }
        public string BelgeNo { get; set; }
        public decimal GirenMiktar { get; set; }
        public decimal GirisFiyati { get; set; }
        public decimal GirisTutari { get; set; }
        public string AnlasilanDoviz { get; set; }
        public decimal CikanMiktar { get; set; }
        public int SiparisNo { get; set; }
        public decimal SatisMiktar { get; set; }
        public decimal FaturalanmisMiktar { get; set; }
        public decimal CikisFiyati { get; set; }
        public decimal CikisTutari { get; set; }
        public decimal Envanter { get; set; }
        public string CariHesapTipi { get; set; }
        public string CariKodu { get; set; }
        public string CariAdi { get; set; }
        public string DepoKodu { get; set; }
    }
}