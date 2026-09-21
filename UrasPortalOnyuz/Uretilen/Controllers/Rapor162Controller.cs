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
    public class Rapor162Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor162Controller> _logger;

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
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
        };


        private static readonly List<UdfTanim> YonetilenUdfler = new List<UdfTanim>
        {
            new UdfTanim { Alias = "CariKartTipi", Baslik = "Cari Kart Tipi", Boyut = 50 },
            new UdfTanim { Alias = "CariBolge",    Baslik = "Cari Bölge",     Boyut = 20 },
            new UdfTanim { Alias = "Ulke",         Baslik = "Ülke",           Boyut = 60 },
            new UdfTanim { Alias = "Sehir",        Baslik = "Şehir",          Boyut = 60 },
            new UdfTanim { Alias = "CariSinifi",   Baslik = "Cari Sınıfı",    Boyut = 60 },
            new UdfTanim { Alias = "IliskiDurumu", Baslik = "İlişki Durumu",  Boyut = 30 },
            new UdfTanim { Alias = "CariRolu",     Baslik = "Cari Rolü",      Boyut = 30 },
            new UdfTanim { Alias = "EsCariKodu",   Baslik = "Eş Cari Kodu",   Boyut = 30 },
            new UdfTanim { Alias = "FaalAnaGrup",  Baslik = "Faaliyet Ana Grubu", Boyut = 50 },
            new UdfTanim { Alias = "FaalGrup",     Baslik = "Faaliyet Grubu", Boyut = 80 },
            new UdfTanim { Alias = "AnaFaaliyet",  Baslik = "Ana Faaliyet mi?", Boyut = 10 },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.SatisTemsilcileri = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor162Controller.LookupModel>(12);
ViewBag.OdemeKosullari = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor162Controller.LookupModel>(12);
ViewBag.Ulkeler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor162Controller.KodAd>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        [HttpGet("Liste")]
        public IActionResult Liste(string tip = "all", string q = "")
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName", "CardType", "LicTradNum", "Phone1", "validFor", "frozenFor" }) });
}


        [HttpGet("CariAra")]
        public IActionResult CariAra(string q = "")
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName" }) });
}


        [HttpGet("Sehirler")]
        public IActionResult Sehirler(string ulkeKod = "")
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Code", "Name" }) });
}


        [HttpGet("Detay")]
        public IActionResult Detay(string cardCode)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, object>>(), alanDurumu = global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, bool>>() });
}


        [HttpPost("Kaydet")]
        public async Task<IActionResult> Kaydet([FromBody] MuhatapModel req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), uyarilar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("uyarilar", i2)).ToList() });
}



        [HttpPost("AlanlariHazirla")]
        public async Task<IActionResult> AlanlariHazirla()
 {return Json(new { success = true, olusturulan = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("olusturulan", i2)).ToList(), alanDurumu = global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, bool>>() });
}


        [HttpPost("AlanAc")]
        public async Task<IActionResult> AlanAc([FromBody] AlanAcIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        private List<LookupModel> SatisTemsilcileri(string dbKey)
 {return default;
}

        private List<LookupModel> OdemeKosullari(string dbKey)
 {return default;
}

        private List<KodAd> Ulkeler(string dbKey)
 {return default;
}


        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class LookupModel { public int Id { get; set; } public string Name { get; set; } }
        public class KodAd { public string Kod { get; set; } public string Ad { get; set; } }
        public class UdfTanim { public string Alias { get; set; } public string Baslik { get; set; } public int Boyut { get; set; } }
        public class AlanAcIstek { public string Alias { get; set; } }

        public class MuhatapModel
        {
            public string CardType { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }     // Cari Kısa Adı
            public string AliasName { get; set; }    // Kısaltmalı Ünvan
            public string CardFName { get; set; }    // Resmi Ünvan
            public string LicTradNum { get; set; }   // Vergi No / TCKN
            public string AddID { get; set; }        // Vergi Dairesi
            public int GroupNum { get; set; }        // Ödeme şartı / vade
            public int SlpCode { get; set; }         // Satış Temsilcisi (OSLP)
            public string CntctPrsn { get; set; }    // Yetkili Kişi
            public string Phone1 { get; set; }       // Telefon
            public string Email { get; set; }        // E-posta
            public string Notes { get; set; }        // Not
            public bool Aktif { get; set; }          // Durum

            public string CariKartTipi { get; set; }
            public string CariBolge { get; set; }
            public string Ulke { get; set; }
            public string Sehir { get; set; }
            public string CariSinifi { get; set; }
            public string IliskiDurumu { get; set; }
            public string CariRolu { get; set; }
            public string EsCariKodu { get; set; }
            public string FaalAnaGrup { get; set; }
            public string FaalGrup { get; set; }
            public string AnaFaaliyet { get; set; }
        }
    }
}
