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
    [Route("Rapor148")]
    public class Rapor148Controller : Controller
    {
        private readonly IConfiguration _yapilandirma;
        private readonly ILogger<Rapor148Controller> _gunluk;

        public class R148DatabaseConfig
        {
            public string Key { get; set; }
            public string Display { get; set; }
        }

        private readonly List<R148DatabaseConfig> _databases = new List<R148DatabaseConfig>
        {
            new R148DatabaseConfig { Key = "DefaultConnection",   Display = "URASKIMYA" },
            new R148DatabaseConfig { Key = "DefaultConnection1",  Display = "URSMAKINE" },
            new R148DatabaseConfig { Key = "DefaultConnection2",  Display = "AVRUPA_PAPER" },
            new R148DatabaseConfig { Key = "DefaultConnection3",  Display = "ALV_KIMYA" },
            new R148DatabaseConfig { Key = "DefaultConnection4",  Display = "DAF_KIMYA" },
            new R148DatabaseConfig { Key = "DefaultConnection5",  Display = "SELVI" },
            new R148DatabaseConfig { Key = "DefaultConnection6",  Display = "ALVFILO" },
            new R148DatabaseConfig { Key = "DefaultConnection7",  Display = "AVRASYA" },
            new R148DatabaseConfig { Key = "DefaultConnection8",  Display = "ASIA_KIMYA" },
            new R148DatabaseConfig { Key = "DefaultConnection9",  Display = "DEKORLIM" },
            new R148DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new R148DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new R148DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new R148DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new R148DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new R148DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new R148DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new R148DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new R148DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new R148DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new R148DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new R148DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new R148DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new R148DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new R148DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new R148DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new R148DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new R148DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
            new R148DatabaseConfig { Key = "DefaultConnection31", Display = "AVRASYA_2026" },
            new R148DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new R148DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new R148DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new R148DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new R148DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" }
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string BaglantiDizesi(string dbKey)  {return default;
}

        public class StokSatir
        {
            public string KalemKodu { get; set; }
            public string KalemAdi { get; set; }
            public string Birim { get; set; }
            public string GrupAdi { get; set; }
            public string DepoKodu { get; set; }
            public string DepoAdi { get; set; }

            public decimal Stok { get; set; }
            public decimal Rezerve { get; set; }
            public decimal Siparis { get; set; }
            public decimal Kullanilabilir { get; set; }

            public decimal Fiyat { get; set; }
            public string FiyatKaynagi { get; set; }
            public decimal Tutar { get; set; }

            public decimal MinStok { get; set; }
            public bool AltindaMi { get; set; }
        }

        public class SecenekOgesi
        {
            public string Kod { get; set; }
            public string Ad { get; set; }
        }

        [HttpGet("")]
        public IActionResult Index()
 {ViewBag.Depolar = WebApplication3.OrnekDoldurucu.Liste<Rapor148Controller.SecenekOgesi>(12);
ViewBag.Gruplar = WebApplication3.OrnekDoldurucu.Liste<Rapor148Controller.SecenekOgesi>(12);
ViewBag.CurrentDbDisplay = "";
ViewBag.SelectedDb = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        private List<SecenekOgesi> DepolariGetir(string dbKey)
 {return default;
}


        private List<SecenekOgesi> GruplariGetir(string dbKey)
 {return default;
}

        [HttpGet("Veri")]
        public async Task<IActionResult> Veri(string depolar, string gruplar, string arama,
            bool sifirlariGoster = false, bool pasifleriGoster = false)
 {return Json(new { basarili = true, satirlar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor148Controller.StokSatir>(12) });
}

        private async Task<List<StokSatir>> SatirlariGetirAsync(string dbKey, string depolar,
            string gruplar, string arama, bool sifirlariGoster, bool pasifleriGoster)
 {return default;
}

        private static List<string> Ayikla(string csv)
 {return default;
}

        [HttpGet("Excel")]
        public async Task<IActionResult> Excel(string depolar, string gruplar, string arama,
            bool sifirlariGoster = false, bool pasifleriGoster = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}
