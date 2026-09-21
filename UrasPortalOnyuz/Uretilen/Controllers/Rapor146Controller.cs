// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
    [Route("Rapor146")]
    public class Rapor146Controller : Controller
    {
        private readonly IConfiguration _yapilandirma;
        private readonly ILogger<Rapor146Controller> _gunluk;

        public class R146DatabaseConfig
        {
            public string Key { get; set; }
            public string Display { get; set; }
        }

        private readonly List<R146DatabaseConfig> _databases = new List<R146DatabaseConfig>
        {
            new R146DatabaseConfig { Key = "DefaultConnection",   Display = "URASKIMYA" },
            new R146DatabaseConfig { Key = "DefaultConnection1",  Display = "URSMAKINE" },
            new R146DatabaseConfig { Key = "DefaultConnection2",  Display = "AVRUPA_PAPER" },
            new R146DatabaseConfig { Key = "DefaultConnection3",  Display = "ALV_KIMYA" },
            new R146DatabaseConfig { Key = "DefaultConnection4",  Display = "DAF_KIMYA" },
            new R146DatabaseConfig { Key = "DefaultConnection5",  Display = "SELVI" },
            new R146DatabaseConfig { Key = "DefaultConnection6",  Display = "ALVFILO" },
            new R146DatabaseConfig { Key = "DefaultConnection7",  Display = "AVRASYA" },
            new R146DatabaseConfig { Key = "DefaultConnection8",  Display = "ASIA_KIMYA" },
            new R146DatabaseConfig { Key = "DefaultConnection9",  Display = "DEKORLIM" },
            new R146DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new R146DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new R146DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new R146DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new R146DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new R146DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new R146DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new R146DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new R146DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new R146DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new R146DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new R146DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new R146DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new R146DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new R146DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new R146DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new R146DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new R146DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
            new R146DatabaseConfig { Key = "DefaultConnection31", Display = "AVRASYA_2026" },
            new R146DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new R146DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new R146DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new R146DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new R146DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" }
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string BaglantiDizesi(string dbKey)  {return default;
}



        public class TahsilatSatir
        {
            public string Grup { get; set; }          // Nakit / Cek / Havale
            public string OdemeTuru { get; set; }     // Havale/EFT, Kredi Kartı ...
            public int DocNum { get; set; }
            public DateTime DocDate { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string Satisci { get; set; }
            public string IslemTipi { get; set; }     // U_BE1_AKTAR
            public string ParaBirimi { get; set; }
            public decimal Tutar { get; set; }        // belge para biriminde
            public decimal TutarTL { get; set; }      // yerel para biriminde
            public string Hesap { get; set; }


            public string CekNo { get; set; }
            public DateTime? CekVade { get; set; }
            public string Banka { get; set; }
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
        public async Task<IActionResult> Veri(string bas, string bit, string islemTipi)
 {return Json(new { success = true, satirlar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor146Controller.TahsilatSatir>(12), ozet = new { nakit = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("nakit", 0), cek = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("cek", 0), havale = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("havale", 0), toplam = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("toplam", 0) } });
}

        private async Task<List<TahsilatSatir>> SatirlariGetirAsync(string dbKey, DateTime bas, DateTime bit, string islemTipi)
 {return default;
}





        [HttpGet("Excel")]
        public async Task<IActionResult> Excel(string bas, string bit, string islemTipi)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}
