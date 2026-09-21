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
    [Route("Rapor172")]
    public class Rapor172Controller : Controller
    {
        private readonly IConfiguration _yapilandirma;
        private readonly ILogger<Rapor172Controller> _gunluk;

        private string BaglantiDizesi()  {return default;
}

        public class DepoOgesi
        {
            public string WhsCode { get; set; }
            public string WhsName { get; set; }
        }

        public class PartiSatir
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string Birim { get; set; }
            public string Tip { get; set; }
            public string Parti { get; set; }
            public string WhsCode { get; set; }
            public decimal Miktar { get; set; }
            public string GirisTarihi { get; set; }
            public int YasGun { get; set; }
            public string Kova { get; set; }
            public string SonKullanma { get; set; }
            public int SuresiDoldu { get; set; }
        }

        public class KovaOzet
        {
            public string Kova { get; set; }
            public int PartiSayisi { get; set; }
            public decimal Miktar { get; set; }
            public int Mamul { get; set; }
            public int YariMamul { get; set; }
            public int Hammadde { get; set; }
        }

        [HttpGet("")]
        public IActionResult Index()
 {ViewBag.Depolar = WebApplication3.OrnekDoldurucu.Liste<Rapor172Controller.DepoOgesi>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        private List<DepoOgesi> DepolariGetir()
 {return default;
}

        private static string TipKosul(string tip, string kolon)
 {return default;
}

        private (string sql, object param) SatirSorgusu(string whs, string tip, string arama)
 {return default;
}

        [HttpGet("Veri")]
        public async Task<IActionResult> Veri(string whs, string tip, string arama)
 {return Json(new { basarili = true, satirlar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor172Controller.PartiSatir>(12), kovalar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor172Controller.KovaOzet>(12) });
}

        [HttpGet("Excel")]
        public async Task<IActionResult> Excel(string whs, string tip, string arama)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}
