// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor102Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor102Controller> _logger;

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
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "ASIAKIMYA2026" },
            new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S" },
            new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S" },
            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };

        private bool IsDealer(string companyName)
 {return default;
}

        private string GetExclusionFilter(string companyName)
 {return default;
}

        private string GetGiderGlobalExclusion()
 {return default;
}


        private string GetGlobalCariExclusion()
 {return default;
}

        [HttpGet]
        [HttpGet("Index")]
        public IActionResult Index(DateTime? startDate, DateTime? endDate, bool includeVat = false, bool includeDealers = false)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor102ViewModel>());
}

        [HttpGet("GetDetay")]
        public JsonResult GetDetay(string firma, string tip, string start, string end, bool isGrupIci = false, bool isOperasyon = false)
 {return Json(new { success = true, data = new object[0] });
}

        [HttpGet("GetMatrisDetay")]
        public JsonResult GetMatrisDetay(string anaKat, string altKat, int ay, string start)
 {return Json(new { success = true, data = new object[0] });
}

        [HttpGet("GetBelgeDetay")]
        public JsonResult GetBelgeDetay(string firma, string turKod, string docId)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CariKodu", "Kod", "Aciklama", "Miktar", "Fiyat", "ParaBirimi", "Kur", "OrjNetTutar", "OrjKdv", "TLNetTutar", "TLKdv", "GiderKategori" }) });
}

        private string GetGrupIciCondition(string companyName)
 {return default;
}

        private (string AnaKategori, string AltKategori) GetKategoriDetay(string descr)
 {return default;
}
    }

    public class Rapor102ViewModel
    {
        public DateTime BaslangicTarihi { get; set; }
        public DateTime BitisTarihi { get; set; }
        public bool IncludeVat { get; set; }
        public bool IncludeDealers { get; set; }

        public decimal GenelSatis { get; set; }
        public decimal GenelTaslakSatis { get; set; }
        public decimal GenelAlis { get; set; }
        public decimal GenelGider { get; set; }

        public decimal GenelSatisNet { get; set; }
        public decimal GenelSatisKdv { get; set; }
        public decimal GenelTaslakSatisNet { get; set; }
        public decimal GenelTaslakSatisKdv { get; set; }
        public decimal GenelAlisNet { get; set; }
        public decimal GenelAlisKdv { get; set; }
        public decimal GenelGiderNet { get; set; }
        public decimal GenelGiderKdv { get; set; }

        public decimal GenelOperasyonSatis { get; set; }
        public decimal GenelOperasyonAlis { get; set; }
        public decimal GenelOperasyonGider { get; set; }

        public decimal GenelAlimVeGiderToplami { get; set; }

        public int OkunanFirmaSayisi { get; set; }
        public List<SirketOzet> FirmaOzetleri { get; set; }
        public List<SirketOzet> BayiOzetleri { get; set; }
        public List<SirketOzet> OperasyonOzetleri { get; set; }
        public List<AylikTrendOzet> TrendOzetler { get; set; }
        public List<AylikOzet> AylikGiderOzetler { get; set; }
        public List<MukerrerKayit> MukerrerKayitlar { get; set; }
    }

    public class MukerrerKayit
    {
        public string FirmaAdi { get; set; }
        public string Tip { get; set; }
        public string Tarih { get; set; }
        public string BelgeNo { get; set; }
        public string CariKodu { get; set; }
        public string CariHesap { get; set; }
        public decimal Tutar { get; set; }
    }

    public class SirketOzet
    {
        public string FirmaAdi { get; set; }
        public decimal ToplamGider { get; set; }
        public decimal ToplamAlis { get; set; }
        public decimal ToplamSatis { get; set; }
        public decimal ToplamTaslakSatis { get; set; }
        public decimal GrupIciSatis { get; set; }
        public decimal GrupIciAlis { get; set; }
        public decimal GrupIciGider { get; set; }
        public decimal GrupIciTaslakSatis { get; set; }
    }

    public class AylikTrendOzet
    {
        public int Ay { get; set; }
        public decimal Gider { get; set; }
        public decimal Alis { get; set; }
        public decimal Satis { get; set; }
    }

    public class TempGiderSatir
    {
        public int Ay { get; set; }
        public decimal Tutar { get; set; }
        public string AnaKat { get; set; }
        public string AltKat { get; set; }
    }

    public class AylikOzet
    {
        public string AnaKategori { get; set; }
        public string AltKategori { get; set; }
        public decimal Ocak { get; set; }
        public decimal Subat { get; set; }
        public decimal Mart { get; set; }
        public decimal Nisan { get; set; }
        public decimal Mayis { get; set; }
        public decimal Haziran { get; set; }
        public decimal Temmuz { get; set; }
        public decimal Agustos { get; set; }
        public decimal Eylul { get; set; }
        public decimal Ekim { get; set; }
        public decimal Kasim { get; set; }
        public decimal Aralik { get; set; }
        public decimal YilToplami { get; set; }
    }
}