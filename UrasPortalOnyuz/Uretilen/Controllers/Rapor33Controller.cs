// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WebApplication3.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Data;
using System.Globalization;
using System.Diagnostics;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor33Controller : Controller
    {
        private readonly IConfiguration _configuration;


        private readonly List<string> RestrictedUserCodes = new List<string>
        {
            "IT01", "IT02", "URT6", "URT", "MUH01", "MUH36", "MUH65", "MUH45", "MUH47",
            "FNS1", "FNS2", "FNS3", "FNS4", "FNS01", "MUH100", "SSAT5", "SAT8", "MUH61", "ALV1",
            "ALV2", "ALV3", "MUH58", "YKB", "GK", "URAS1", "SATIS19", "SAT2", "URT6",
            "SATIS6", "ISG", "SATIS8", "MUH86", "MUH41","MUH42", "MUH87", "SATIS4", "SATIS3",
            "DEPO1", "DEPO2", "SATIS7", "MUH25","MUH9", "URK1", "SATIS18", "SATIS12", "MUH19",
            "MUH44", "MUH43","MUH54"
        };


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





        private List<UserViewModel> GetDistinctUsers()
 {return default;
}




        private UserActivityStats GetUserActivityStats(string userCode)
 {return default;
}

        private string GenerateMultiCompanyQuery()
 {return default;
}


        private List<KullaniciIslemViewModel> GetRaporData(string userCode, DateTime? startDate, DateTime? endDate)
 {return default;
}





        private List<KullaniciAktiviteHamViewModel> GetUserActivityRawData(string userCode, DateTime? startDate, DateTime? endDate)
 {return default;
}

        [Authorize]
        public IActionResult Index(string userCode, DateTime? startDate, DateTime? endDate, [FromQuery] List<string> belgeTipiFilter)
 {ViewBag.SelectedUserCode = "";
ViewBag.SelectedUserName = "";
ViewBag.StartDate = "";
ViewBag.EndDate = "";
ViewBag.UserList = WebApplication3.OrnekDoldurucu.Liste<UserViewModel>(12);
ViewBag.ActivityStats = WebApplication3.OrnekDoldurucu.Yeni<UserActivityStats>();
ViewBag.BelgeTipiList = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.SelectedBelgeTipiFilter = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.OzelGirisListesi = WebApplication3.OrnekDoldurucu.Liste<KullaniciAktiviteHamViewModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.KullaniciIslemViewModel>(12));
}

        [Authorize]
        [Route("ActivityRaw")]
        public IActionResult ActivityRaw(string userCode, DateTime? startDate, DateTime? endDate)
 {ViewBag.SelectedUserCode = "";
ViewBag.SelectedUserName = "";
ViewBag.StartDate = "";
ViewBag.EndDate = "";
ViewBag.UserList = WebApplication3.OrnekDoldurucu.Liste<UserViewModel>(12);
ViewBag.ActivityStats = WebApplication3.OrnekDoldurucu.Yeni<UserActivityStats>();
ViewBag.BelgeTipiList = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.SelectedBelgeTipiFilter = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.OzelGirisListesi = WebApplication3.OrnekDoldurucu.Liste<KullaniciAktiviteHamViewModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.KullaniciIslemViewModel>(12));
}
    }
}