// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{









    [Authorize]
    [Route("[controller]")]
    public class Rapor160Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor160Controller> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

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
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string ConnStr(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.CurrentDbDisplay = "";
ViewBag.IslemTipleri = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.ParaBirimleri = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        private List<string> GetIslemTipleri(string dbKey)
 {return default;
}

        private List<string> GetParaBirimleri(string dbKey)
 {return default;
}


        [HttpGet("Ara")]
        public IActionResult Ara(string tip, string q)
 {return Json(new { success = true, liste = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Kod", "Ad", "PB" }) });
}






        [HttpGet("Bakiye")]
        public IActionResult Bakiye(string tip, string kod, string bas, string bit, string islemTipi, bool devirDahil, string paraBirimi)
 {return Json(new { success = true, paraBirimi = global::WebApplication3.OrnekDoldurucu.Deger<string>("paraBirimi", 0), lc = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("lc", 0), fc = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("fc", 0) });
}


        [HttpGet("TcmbKuru")]
        public async Task<IActionResult> TcmbKuru(string paraBirimi, string tarih)
 {return Json(new { success = true, kur = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("kur", 0), kurTarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("kurTarih", 0) });
}






        private static DateTime DegerlemeKurTarihi(DateTime fisTarih)
 {return default;
}

        private async Task<(decimal Rate, DateTime Tarih, string Mesaj)> TcmbAlisKuruAsync(string paraBirimi, DateTime tarih)
 {return default;
}





        [HttpPost("KurFarkiOlanlar")]
        public async Task<IActionResult> KurFarkiOlanlar([FromBody] KurFarkiIstek istek)
 {return Json(new { success = true, liste = new object[0], kur = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("kur", 0), kurTarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("kurTarih", 0), paraBirimi = global::WebApplication3.OrnekDoldurucu.Deger<string>("paraBirimi", 0) });
}





        [HttpPost("Onizleme")]
        public async Task<IActionResult> Onizleme([FromBody] KurFarkiIstek istek)
 {return Json(new { success = true, kalemler = new object[0], satirlar = new object[0], toplamBorc = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("toplamBorc", 0), toplamAlacak = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("toplamAlacak", 0), uyarilar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("uyarilar", i2)).ToList(), fisTarihi = global::WebApplication3.OrnekDoldurucu.Deger<string>("fisTarihi", 0) });
}


        [HttpPost("KurFarkiYevmiyesi")]
        public async Task<IActionResult> KurFarkiYevmiyesi([FromBody] KurFarkiIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), kesilen = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("kesilen", i2)).ToList() });
}


        private bool IslemKoduVar(string baglanti, string kod)
 {return default;
}




        private (decimal LC, decimal FC, string PB, string Hata) HesaplaBakiye(string baglanti, string tip, string kod, string bas, string bit, List<string> islemTipleri, bool devirDahil, string paraBirimi)
 {return default;
}

        private string KambiyoHesabi(string baglanti, bool kar, bool cariMi)
 {return default;
}

        private bool KolonVar(string baglanti, string tablo, string kolon)
 {return default;
}

        private string TekDeger(string baglanti, string sql, string kod, params (string, object)[] ek)
 {return default;
}

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }

        public class KalemSecim
        {
            public string Tip { get; set; }          // "hesap" | "cari"
            public string Kod { get; set; }
            public string ParaBirimi { get; set; }   // isteğe bağlı, çok dövizli (##) için
        }

        public class KurFarkiIstek
        {
            public List<KalemSecim> Kalemler { get; set; } = new List<KalemSecim>();
            public string Bas { get; set; }
            public string Bit { get; set; }
            public string FisTarihi { get; set; }

            public List<string> IslemTipleri { get; set; } = new List<string>();

            public string FisIslemTipi { get; set; }
            public bool DevirDahil { get; set; }
            public string ParaBirimi { get; set; }
        }
    }
}
