// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{





    [Route("PlanliOdemeler")]
    public class PlanliOdemeController : Controller
    {
        private readonly IConfiguration _yapilandirma;
        private readonly PlanliOdemeServisi _servis;
        private readonly ILogger<PlanliOdemeController> _gunluk;

        private string KullaniciKodu()  {return default;
}


        [HttpGet("")]
        public async Task<IActionResult> Index()
 {ViewBag.Sirketler = WebApplication3.OrnekDoldurucu.Liste<(string DbName, string Ad)>(12);
ViewBag.Alicilar = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
ViewBag.Gecmis = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
ViewBag.Bugun = System.DateTime.Today;
ViewBag.TaksitOzet = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
ViewBag.SonrakiTaksitler = WebApplication3.OrnekDoldurucu.Liste<PlanliOdemeTaksit>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Services.PlanliOdeme>(12));
}






        [HttpGet("Taksitler")]
        public async Task<IActionResult> Taksitler(int planId)
 {return Json(new { success = true, plan = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Services.PlanliOdeme>(), taksitler = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.PlanliOdemeTaksit>(12) });
}


        [HttpPost("TaksitOnizle")]
        public IActionResult TaksitOnizle([FromBody] PlanliOdeme model)
 {return Json(new { success = true, taksitler = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.PlanliOdemeTaksit>(12), toplam = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("toplam", 0), anapara = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("anapara", 0), faiz = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("faiz", 0) });
}


        [HttpPost("TaksitUret")]
        public async Task<IActionResult> TaksitUret(int planId)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        public class TaksitKayitIstek { public int PlanId { get; set; } public List<PlanliOdemeTaksit> Taksitler { get; set; } }


        [HttpPost("TaksitKaydet")]
        public async Task<IActionResult> TaksitKaydet([FromBody] TaksitKayitIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("TaksitOdendi")]
        public async Task<IActionResult> TaksitOdendi(int id, bool odendi)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("Kaydet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kaydet(PlanliOdeme model)
 {return RedirectToAction("Index");
}

        [HttpPost("Sil")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil(int id)
 {return RedirectToAction("Index");
}

        [HttpPost("DurumDegistir")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DurumDegistir(int id)
 {return RedirectToAction("Index");
}


        [HttpPost("AliciEkle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AliciEkle(string eposta, string adSoyad)
 {return RedirectToAction("Index");
}

        [HttpPost("AliciSil")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AliciSil(int id)
 {return RedirectToAction("Index");
}



        [HttpPost("HatirlatmalariCalistir")]
        [AllowAnonymous]
        public async Task<IActionResult> HatirlatmalariCalistir(string anahtar = null, string tarih = null)
 {return Json(new { tarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("tarih", 0), HatirlatmaGonderildi = global::WebApplication3.OrnekDoldurucu.Deger<int>("HatirlatmaGonderildi", 0), SatirOlusturuldu = global::WebApplication3.OrnekDoldurucu.Deger<int>("SatirOlusturuldu", 0), Hatalar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("Hatalar", i2)).ToList() });
}


        [HttpPost("SimdiGonder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimdiGonder(int id)
 {return RedirectToAction("Index");
}

    }
}
