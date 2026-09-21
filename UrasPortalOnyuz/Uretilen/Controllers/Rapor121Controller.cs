// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("Rapor121")]
    public class Rapor121Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor121Controller> _logger;


        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" }, // Algoritma başlangıç önceliği taşıyan merkez şirket
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
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
        };


        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
 {ViewBag.Databases = new System.Collections.Generic.List<object>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        [HttpGet("GetCariler")]
        public IActionResult GetCariler(string dbKey)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor121Controller.CariModel>(12) });
}


        [HttpPost("YoluBul")]
        public IActionResult YoluBul([FromBody] RotaIstekModel istek)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor121Controller.KompleSimulasyonModel>() });
}


        private KompleSimulasyonModel FullSimulasyonHesapla(string hedefSirket, string cariCode, string cariAd)
 {return default;
}


        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } }

        public class CariModel
        {
            public string Id { get; set; }
            public string Text { get; set; }
            public decimal Bakiye { get; set; }
        }

        public class RotaIstekModel
        {
            public string KaynakDbKey { get; set; }
            public string AlacakliCariCode { get; set; }
            public string AlacakliCariAd { get; set; }
        }

        public class KompleSimulasyonModel
        {
            public List<BorcMahsupModel> BorcMahsupSimulasyonu { get; set; } = new List<BorcMahsupModel>();
            public List<CekAkisModel> CekAkisSimulasyonu { get; set; } = new List<CekAkisModel>();
            public List<MuhasebeKayitModel> MuhasebeKayitSimulasyonu { get; set; } = new List<MuhasebeKayitModel>();
        }

        public class BorcMahsupModel
        {
            public int Sira { get; set; }
            public string Borclu { get; set; }
            public string Alacakli { get; set; }
            public string IliskiTuru { get; set; }
            public string Tutar { get; set; }
            public string Aciklama { get; set; }
            public string Durum { get; set; }
        }

        public class CekAkisModel
        {
            public int Sira { get; set; }
            public string Islem { get; set; }
            public string CekinGirisAlan { get; set; }
            public string CekinCikisVeren { get; set; }
            public string IslemAciklamasi { get; set; }
            public string Tutar { get; set; }
            public string CekDurumu { get; set; }
        }

        public class MuhasebeKayitModel
        {
            public int Sira { get; set; }
            public string IslemUnvani { get; set; }
            public string Aciklama { get; set; }
            public string BorcHesap { get; set; }
            public decimal BorcTutar { get; set; }
            public string AlacakHesap { get; set; }
            public decimal AlacakTutar { get; set; }
        }
    }
}