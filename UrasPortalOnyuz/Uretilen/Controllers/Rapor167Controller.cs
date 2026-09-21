// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{







    [Authorize]
    [Route("[controller]")]
    public class Rapor167Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor167Controller> _logger;
        private readonly EmailService _email;
        private static string H(string s)  {return default;
}

        private string Kullanici()  {return default;
}
        private bool Yetkili()
 {return default;
}

        [HttpGet]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        [HttpGet("Liste")]
        public IActionResult Liste(string durum = null, string kullanici = null)
 {return Json(new { success = true, yetkili = global::WebApplication3.OrnekDoldurucu.Deger<bool>("yetkili", 0), kullanici = global::WebApplication3.OrnekDoldurucu.Deger<string>("kullanici", 0), kullanicilar = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "kod", "ad" }), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.GiderGelirKatalog.SatirAtama>(12) });
}

        public class OnayIstek { public int Id { get; set; } public string Durum { get; set; } public string Aciklama { get; set; } public string Kod { get; set; } }


        [HttpPost("OnayVer")]
        public IActionResult OnayVer([FromBody] OnayIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}
    }
}
