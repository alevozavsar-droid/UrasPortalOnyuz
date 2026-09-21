// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using WebApplication3.Data;
using WebApplication3.Models;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{















    [Route("SapBilgi")]
    public class SapBilgiController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IDataProtectionProvider _koruyucu;
        private readonly ApplicationDbContext _context;

        public class SirketSatiri
        {
            public string DbKey { get; set; }
            public string Display { get; set; }
            public string DbName { get; set; }
            public string SapKullanici { get; set; }
            public bool SifreKayitli { get; set; }
            public bool KendiKullanicisi { get; set; }
            public DateTime? Guncelleme { get; set; }
            public bool Secili { get; set; }
        }

        public class SapBilgiViewModel
        {
            public bool Yetkili { get; set; }
            public string KullaniciKodu { get; set; }
            public string VarsayilanKullanici { get; set; }

            public bool VarsayilanSecilebilir { get; set; }
            public List<SirketSatiri> Sirketler { get; set; } = new List<SirketSatiri>();
        }

        private string KullaniciKodu {get {return default;
}
}        private bool Yetkili {get {return default;
}
}        private bool VarsayilanSecilebilir {get {return default;
}
}
        private string SirketVeritabaniAdi(string dbKey)
 {return default;
}


        private List<AppDatabase> YetkiliSirketler()
 {return default;
}

        private bool SirketeYetkili(string dbKey)  {return default;
}

        [HttpGet("")]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.SapBilgiController.SapBilgiViewModel>());
}

        private KayitliBilgiGuvenli MevcutKayit(string dbKey)
 {return default;
}
        private class KayitliBilgiGuvenli { public SapKimlik.KayitliBilgi Kayit { get; set; } }

        private string Dogrula(string dbKey, ref string sapKullanici, string sapSifre, ref bool kendiKullanicisi)
 {return default;
}


        [HttpPost("Kaydet")]
        [ValidateAntiForgeryToken]
        public IActionResult Kaydet(string dbKey, string sapKullanici, string sapSifre, bool kendiKullanicisi = false)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("TumuneKaydet")]
        [ValidateAntiForgeryToken]
        public IActionResult TumuneKaydet(string sapKullanici, string sapSifre, bool kendiKullanicisi, string[] dbKeys)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("Sil")]
        [ValidateAntiForgeryToken]
        public IActionResult Sil(string dbKey)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        [HttpPost("Test")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Test(string dbKey, string sapKullanici, string sapSifre)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}
    }
}
