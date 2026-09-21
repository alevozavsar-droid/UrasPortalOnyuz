// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace WebApplication3.Controllers
{







    [Authorize]
    [Route("[controller]")]
    public class Rapor165Controller : Controller
    {
        private readonly IConfiguration _configuration;

        [HttpGet]
        public IActionResult Index()
 {ViewBag.Onaylayanlar = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}
    }
}
