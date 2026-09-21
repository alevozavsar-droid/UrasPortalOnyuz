// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace WebApplication3.Controllers
{





    [Authorize]
    [Route("[controller]")]
    public class UrasUretimController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UrasUretimController> _logger;

        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.UretimDashboardVM>());
}
    }

    public class UretimDashboardVM
    {
        public int? AcikIsEmri { get; set; }
        public int? PlanliIsEmri { get; set; }
        public int? BuAyAcilan { get; set; }
        public int? StoktaKalem { get; set; }
        public bool BaglantiHatasi { get; set; }
    }
}
