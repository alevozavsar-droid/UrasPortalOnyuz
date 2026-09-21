// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor85Controller : Controller
    {
        private readonly ILogger<Rapor85Controller> _logger;


        private readonly List<string> _databases = new List<string>
        {
            "URASKIMYA", "URSMAKINE", "AVRUPA_PAPER", "ALV_KIMYA",
            "DAF_KIMYA", "ALVFILO", "AVRASYA", "ASIA_KIMYA", "DEKORLIM",
            "URASHOLDING_2026", "DAFKIMYA_2026", "AVRUPAPAPER_2026", "ALVFILO_2026", "URASBASKI_2026"
        };

        private string GetConnectionString(string dbName)
 {return default;
}

        [HttpGet]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor85ViewModel>());
}

        [HttpPost("TekrarDene")]
        public async Task<IActionResult> TekrarDene(string dbName, int id)
 {return Json(new { success = true });
}

        [HttpPost("TopluTekrarDene")]
        public async Task<IActionResult> TopluTekrarDene(string tip)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("ManuelTetikle")]
        public async Task<IActionResult> ManuelTetikle(string dbName, string docEntry)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}
    }

    public class Rapor85ViewModel
    {
        public List<SelectListItem> DatabaseList { get; set; } = new List<SelectListItem>();
        public List<TalepAktarimDetay> Listesi { get; set; } = new List<TalepAktarimDetay>();
        public List<string> UyariMesajlari { get; set; } = new List<string>();

        public int Bekleyen { get; set; }
        public int Hatali { get; set; }
        public int Reddedilen { get; set; }
        public int Basarili { get; set; }
        public int Kayip { get; set; }
    }

    public class TalepAktarimDetay
    {
        public int Id { get; set; }
        public string KaynakDB { get; set; }
        public string KaynakDocEntry { get; set; }
        public string Durum { get; set; }
        public string HataMesaji { get; set; }
        public DateTime IslemTarihi { get; set; }
        public string HedefTalepNo { get; set; }
        public string HedefDocEntry { get; set; }
        public string OnayBekleyenKisi { get; set; }
    }
}