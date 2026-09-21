// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using WebApplication3.Models;
using WebApplication3.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Rendering;
using ClosedXML.Excel;
using System.IO;
using WebApplication3.Repositories;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor54Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IFiloRepository _filoRepository;

        private static readonly string ALVFILO_DISPLAY_NAME = "ALVFILO";




        public async Task<IActionResult> Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}


        [HttpGet("Export")]
        public async Task<IActionResult> ExportToExcel()
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}





        [HttpGet("Araclar")]
        public async Task<IActionResult> AraclarIndex()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpGet("AracEkle")]
        public IActionResult AracEkle()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpPost("AracEkle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AracEkle(Arac arac)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpGet("AracDuzenle/{id}")]
        public async Task<IActionResult> AracDuzenle(int id)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpPost("AracDuzenle/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AracDuzenle(int id, Arac arac)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpPost("AracSil")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AracSil(int id)
 {return RedirectToAction("Index");
}





        [HttpGet("Personel")]
        public async Task<IActionResult> PersonelIndex()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpGet("PersonelEkle")]
        public IActionResult PersonelEkle()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpPost("PersonelEkle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonelEkle(Personel personel)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpGet("PersonelDuzenle/{id}")]
        public async Task<IActionResult> PersonelDuzenle(int id)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpPost("PersonelDuzenle/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonelDuzenle(int id, Personel personel)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpPost("PersonelPasiflestir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonelPasiflestir(int id)
 {return RedirectToAction("Index");
}





        [HttpGet("YeniAtama")]
        public async Task<IActionResult> YeniAtama()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpPost("YeniAtama")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> YeniAtama(YeniAtamaViewModel model)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpGet("AtamaSonlandir/{aracId}")]
        public async Task<IActionResult> AtamaSonlandir(int aracId)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}

        [HttpPost("AtamaSonlandir/{atamaId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AtamaSonlandir(int atamaId, [Bind("BitisKM")] AracAtama guncelAtama)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.Arac>(12));
}


    }
}