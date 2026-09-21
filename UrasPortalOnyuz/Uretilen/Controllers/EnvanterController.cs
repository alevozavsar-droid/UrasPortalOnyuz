// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApplication3.Controllers
{

















    [Route("Envanter")]
    public class EnvanterController : Controller
    {
        private readonly IConfiguration _yapilandirma;
        private readonly IWebHostEnvironment _ortam;
        private readonly ILogger<EnvanterController> _gunluk;





        public static readonly List<(string DbName, string Ad)> Sirketler = new List<(string, string)>
        {
            ("ALVKIMYA_2026",  "ALV KİMYA 2026"),
            ("URASKIMYA_AS",   "URAS KİMYA A.Ş."),
            ("AVRUPAPAPER_AS", "AVRUPA PAPER A.Ş."),
            ("AVRASYA_2026",   "AVRASYA 2026"),
            ("URSMAKINE_2026", "URS MAKİNE 2026"),
            ("SELVI_2026",     "SELVİ 2026")
        };

        public static readonly List<(string Kod, string Ad, string Betik)> RaporTipleri =
            new List<(string, string, string)>
        {
            ("Urun",      "Ürün",       "BE1_DENGE_URUN2"),
            ("Malzeme",   "Malzeme",    "BE1_DENGE2"),
            ("YariMamul", "Yarı Mamül", "BE1_DENGE_YARIMAMUL")
        };


        private static readonly DateTime CipaAy = new DateTime(2025, 11, 1);

        private string PortalBaglantisi()  {return default;
}

        private string SirketBaglantisi(string dbName)  {return default;
}





        private static readonly Dictionary<string, string> _betikOnbellek =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _betikKilit = new object();


        private string BetigiOku(string ad)
 {return default;
}



        private static readonly Regex KategoriSuzgeci = new Regex(
            @"[A-Za-z0-9_]+\.u_be1_um\s*(?:=|in|not\s+in)\s*(?:\([^)]*\)|'[^']*')",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Dictionary<string, bool> _kategoriAlaniVar =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private async Task<bool> KategoriAlaniVarMiAsync(string dbName)
 {return default;
}





        private static int _semaHazir;





        [HttpGet("")]
        public async Task<IActionResult> Index(string sirket = null, int? yil = null, int? ay = null)
 {ViewBag.Sirketler = WebApplication3.OrnekDoldurucu.Liste<(string DbName, string Ad)>(12);
ViewBag.RaporTipleri = WebApplication3.OrnekDoldurucu.Liste<(string Kod, string Ad, string Betik)>(12);
ViewBag.SecilenSirket = "";
ViewBag.Yil = 0;
ViewBag.Ay = 0;
ViewBag.CipaAy = System.DateTime.Today;
ViewBag.KategoriVar = false;
ViewBag.Durumlar = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
ViewBag.HesaplananAylar = WebApplication3.OrnekDoldurucu.Liste<dynamic>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}





        [HttpGet("Veri")]
        public async Task<IActionResult> Veri(string sirket, int yil, int ay, string tip)
 {return Json(new { success = true, hesaplandi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("hesaplandi", 0), satirlar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.EnvanterSatir>(12) });
}





        [HttpGet("KalemGecmisi")]
        public async Task<IActionResult> KalemGecmisi(string sirket, string tip, string kalem)
 {return Json(new { success = true, aylar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.EnvanterSatir>(12) });
}





        [HttpGet("KalemAyHareket")]
        public async Task<IActionResult> KalemAyHareket(string sirket, string kalem, int yil, int ay)
 {return Json(new { success = true, hareketler = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.KalemHareket>(12) });
}






        [HttpPost("Hesapla")]
        public async Task<IActionResult> Hesapla(string sirket, int yil, int ay, bool zorla = false)
 {return Json(new { success = true, kayitlar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("kayitlar", i2)).ToList() });
}






        private async Task<List<EnvanterSatir>> BetigiCalistirAsync(string dbName, string betik, DateTime bas, DateTime bit)
 {return default;
}
    }


    public class DevirSatiri
    {
        public string KalemKodu { get; set; }
        public decimal SonQ { get; set; }
        public decimal SonT { get; set; }
    }

    public class EnvanterSatir
    {
        public int Yil { get; set; }   // KalemGecmisi (ay bazli gecmis) icin
        public int Ay { get; set; }
        public string KalemKodu { get; set; }
        public string KalemAdi { get; set; }


        public decimal SapDevirQ { get; set; }
        public decimal SapDevirT { get; set; }


        public bool SapDevirVar { get; set; }

        public decimal DevirQ { get; set; }
        public decimal DevirT { get; set; }
        public decimal GirisQ { get; set; }
        public decimal GirisT { get; set; }
        public decimal CikisQ { get; set; }
        public decimal CikisT { get; set; }
        public decimal SonQ { get; set; }
        public decimal SonT { get; set; }

        public decimal? AlimQ { get; set; }
        public decimal? AlimT { get; set; }
        public decimal? SatisQ { get; set; }
        public decimal? SatisT { get; set; }
        public decimal? IhracatQ { get; set; }
        public decimal? IhracatT { get; set; }
    }


    public class KalemHareket
    {
        public DateTime Tarih { get; set; }
        public string Islem { get; set; }
        public string BelgeNo { get; set; }

        public string BelgeAnahtari { get; set; }
        public string Depo { get; set; }
        public decimal GirisQ { get; set; }
        public decimal CikisQ { get; set; }
        public decimal Fiyat { get; set; }

        public decimal Tutar { get; set; }

        public int Iptal { get; set; }

        public string Aciklama { get; set; }
    }
}
