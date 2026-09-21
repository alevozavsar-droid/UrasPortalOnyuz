// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor86Controller : Controller
    {
        private readonly ILogger<Rapor86Controller> _logger;

        private readonly Dictionary<string, string> _sourceDatabases = new Dictionary<string, string>
        {
            { "URASKIMYA", "" },
            { "URSMAKINE", "" },
            { "AVRUPA_PAPER", "" },
            { "ALV_KIMYA", "" },
            { "DAF_KIMYA", "" },
            { "ALVFILO", "" },
            { "AVRASYA", "" },
            { "ASIA_KIMYA", "" },
            { "DEKORLIM", "" }
        };

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor86ViewModel>());
}

        [HttpPost("Onayla")]
        public async Task<IActionResult> Onayla(string sirket, int id)
 {return Json(new { success = true, message = "Selvi'ye aktarım başlatıldı." });
}

        [HttpPost("Reddet")]
        public async Task<IActionResult> Reddet(string sirket, int id, string sebep)
 {return Json(new { success = true });
}
    }

    public class Rapor86ViewModel { public List<OnayDetay> BekleyenTalepler { get; set; } = new List<OnayDetay>(); }

    public class OnayDetay
    {
        public string Sirket { get; set; }
        public int KuyrukId { get; set; }
        public string KaynakDocEntry { get; set; }
        public string TalepNo { get; set; }
        public DateTime Tarih { get; set; }
        public string TalepEden { get; set; }
        public string TalebiAcanKullanici { get; set; }
        public decimal Tutar { get; set; }
        public string Aciklama { get; set; }
        public List<TalepSatir> Satirlar { get; set; }
    }

    public class TalepSatir
    {
        public string KalemKodu { get; set; }
        public string KalemAdi { get; set; }
        public decimal Miktar { get; set; }
        public decimal SatirTutari { get; set; }
        public string SonTedarikci { get; set; }
        public string SonOdemeSekli { get; set; }
    }
}