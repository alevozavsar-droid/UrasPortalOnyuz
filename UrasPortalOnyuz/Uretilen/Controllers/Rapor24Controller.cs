// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor24Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor24Controller> _logger;

        private readonly List<R24_DatabaseConfig> _databases = new List<R24_DatabaseConfig>
        {
            new R24_DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new R24_DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new R24_DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new R24_DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new R24_DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new R24_DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI_KIMYA" },
            new R24_DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new R24_DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new R24_DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
            new R24_DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new R24_DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new R24_DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S" },
            new R24_DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S" },
            new R24_DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new R24_DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new R24_DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new R24_DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new R24_DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" }
        };

        private string GetSelectedDatabase()
 {return default;
}

        private bool IsUserAuthorized()
 {return default;
}

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string hesapTipi = "KASA", string tab = "TUMU")
 {ViewBag.IsAuthorized = false;
ViewBag.KonsolideSirketler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R24_DatabaseConfig>(12);
ViewBag.Ozet = WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.R24_OzetModel>();
ViewBag.CekSenetOzetList = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.CekSenetOzetModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R24_BakiyeModel>(12));
}

        private async Task<List<R24_BakiyeModel>> GetHesapBakiyeleri(string connectionString, string prefix, string tab)
 {return default;
}
        private async Task<List<CekSenetOzetModel>> GetCekSenetOzet(string connectionString, string belgeTipi, string tab)
 {return default;
}

        [HttpGet("GetCekSenetDetay")]
        public async Task<IActionResult> GetCekSenetDetay(string db, string hesapTipi, string islemTipi)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.CekSenetDetayModel>(12) });
}




        private async Task MatchOnayDurumlari(string merkezConnStr, string dbKey, string hesapTipi, List<R24_BakiyeModel> bakiyeListesi)
 {}

        private async Task MatchFizikselOnayDurumlari(string merkezConnStr, string dbKey, string fizikselGrup, List<CekSenetOzetModel> fizikselListe)
 {}

        [HttpPost("SetOnayDurumu")]
        public async Task<IActionResult> SetOnayDurumu([FromBody] R24_OnayPostModel model)
 {return Json(new { success = true, message = "Durum başarıyla kaydedildi." });
}

        [HttpGet("GetOnayGecmisi")]
        public async Task<IActionResult> GetOnayGecmisi(string dbKey, string anaGrup, string hesapKodu, string islemTipi)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "OnayGunu", "KullaniciAdi", "Durum", "HesapAdi", "ParaBirimi", "Tutar", "Adet", "NotBasligi" }) });
}
    }

    public class R24_DatabaseConfig { public string Key { get; set; } public string Display { get; set; } }

    public class R24_BakiyeModel
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public string IslemTipi { get; set; }
        public string ParaBirimi { get; set; }
        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
        public decimal Bakiye { get; set; }
        public decimal BorcTRY { get; set; }
        public decimal AlacakTRY { get; set; }
        public int IslemAdedi { get; set; }
        public string OnayDurumu { get; set; }
        public string NotBasligi { get; set; }
        public string OnaylayanKullaniciAdi { get; set; }
        public DateTime? OnayTarihi { get; set; }
    }

    public class R24_OzetModel { public decimal GirenTutar { get; set; } public decimal CikanTutar { get; set; } public int IslemAdedi { get; set; } }

    public class R24_OnayPostModel
    {
        public string DbKey { get; set; }
        public string HesapGrubu { get; set; }
        public string HesapKodu { get; set; }
        public string IslemTipi { get; set; }
        public string Durum { get; set; }
        public string NotBasligi { get; set; }
        public string NotIcerigi { get; set; }
        public decimal Tutar { get; set; }
        public int Adet { get; set; }
        public string HesapAdi { get; set; }
        public string ParaBirimi { get; set; }
    }

    public class CekSenetOzetModel
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public string ParaBirimi { get; set; }
        public string IslemTipi { get; set; }
        public string HareketDurumu { get; set; }
        public int Adet { get; set; }
        public decimal Tutar { get; set; }
        public string OnayDurumu { get; set; }
        public string NotBasligi { get; set; }
        public string OnaylayanKullaniciAdi { get; set; }
        public DateTime? OnayTarihi { get; set; }
    }

    public class CekSenetDetayModel
    {
        public string CekNo { get; set; }
        public string Vade { get; set; }
        public string CariAdi { get; set; }
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; }
        public decimal TlKarsiligi { get; set; }
    }
}