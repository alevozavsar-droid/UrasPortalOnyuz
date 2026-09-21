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
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Globalization; // CultureInfo için eklendi

namespace WebApplication3.Controllers
{


    [Authorize]
    [Route("[controller]")]
    public class Rapor11Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor11Controller> _logger;

        [HttpGet]
        public async Task<IActionResult> Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.CompanySalesData>(12));
}
    }
}
