// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{





    [Authorize]
    [Route("Rapor170")]
    public class Rapor170Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor170Controller> _logger;

        private string GetConnectionString(string dbKey)  {return default;
}

        public class SirketBilgi
        {
            public string DbKey { get; set; }
            public string DbName { get; set; }
            public string SirketKodu { get; set; }
            public string SirketAd { get; set; }
            public string Etiket { get; set; }
            public int Sira { get; set; }
        }

        private static bool KopyaDb(string dbName)
 {return default;
}

        private List<SirketBilgi> Sirketler()
 {return default;
}

        [HttpGet]
        [HttpGet("Index")]
        public IActionResult Index()
 {ViewBag.Sirketler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor170Controller.SirketBilgi>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("Sirketler")]
        public IActionResult SirketListesi()  {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor170Controller.SirketBilgi>(12) });
}

        private class KatalogKalem { public string Tip; public int AnaGrupNo; public string AnaGrup; public int AltGrupNo; public string AltGrup; public int KalemNo; public string Kalem; }

        private Dictionary<string, KatalogKalem> Katalog()
 {return default;
}

        public class KalemOzet
        {
            public string Kok { get; set; }
            public string Tip { get; set; }
            public int AnaGrupNo { get; set; }
            public string AnaGrup { get; set; }
            public int AltGrupNo { get; set; }
            public string AltGrup { get; set; }
            public int KalemNo { get; set; }
            public string Kalem { get; set; }
            public decimal[] Aylar { get; set; } = new decimal[12];
            public int[] Adet { get; set; } = new int[12];
            public decimal Toplam => Aylar.Sum();
            public string DbKey { get; set; }
            public string Sirket { get; set; }
        }

        private static string OzetSql(bool jdtKodVar, bool inv1, bool pch1, bool rin1, bool rpc1)
 {return default;
}

        private static string Tur(string hesapKodu, Dictionary<string, string> ov)
 {return default;
}

        private List<KalemOzet> SirketOzeti(SirketBilgi s, int yil, string tur, Dictionary<string, KatalogKalem> katalog)
 {return default;
}

        private List<SirketBilgi> HedefSirketleriBelirle(string db, List<SirketBilgi> sirketler)
 {return default;
}

        [HttpGet("Ozet")]
        public IActionResult Ozet(string db, int yil = 0, string tur = "Gider")
 {return Json(new { success = true, yil = global::WebApplication3.OrnekDoldurucu.Deger<int>("yil", 0), tur = global::WebApplication3.OrnekDoldurucu.Deger<string>("tur", 0), konsolide = global::WebApplication3.OrnekDoldurucu.Deger<bool>("konsolide", 0), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor170Controller.KalemOzet>(12), hatalar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatalar", i2)).ToList() });
}

        public class SatirDto
        {
            public string DbKey { get; set; }
            public string Sirket { get; set; }
            public int TransId { get; set; }
            public int LineId { get; set; }
            public int TransType { get; set; }
            public string Tarih { get; set; }
            public int Ay { get; set; }
            public string BelgeTuru { get; set; }
            public string BelgeNo { get; set; }
            public bool FaturaMi { get; set; }
            public string Cari { get; set; }
            public string CardCode { get; set; }
            public string HesapKodu { get; set; }
            public string HesapAdi { get; set; }
            public decimal Borc { get; set; }
            public decimal Alacak { get; set; }
            public decimal Tutar { get; set; }
            public string Kod { get; set; }
            public string Aciklama { get; set; }
        }

        private static string BelgeTuruAdi(int tt)  {return default;
}

        private List<SatirDto> SirketSatirlari(SirketBilgi s, int yil, int ay, string kok, string tur)
 {return default;
}

        [HttpGet("Satirlar")]
        public IActionResult Satirlar(string db, int yil, int ay = 0, string kok = null, string tur = "Gider")
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor170Controller.SatirDto>(12), toplam = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("toplam", 0), hatalar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatalar", i2)).ToList() });
}







        [HttpGet("Kodlar")]
        public IActionResult Kodlar(string dbKey = null)
 {return Json(new { success = true, sirketKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirketKodu", 0), data = new object[0] });
}


        [HttpGet("Oneri")]
        public IActionResult Oneri(string dbKey, string cardCode, string tip = "GIDER")
 {return Json(new { success = true, data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("data", i2)).ToList() });
}

        public class KodSatir { public string DbKey { get; set; } public int TransId { get; set; } public int LineId { get; set; } }
        public class KodIstek { public List<KodSatir> Satirlar { get; set; } public string KokKod { get; set; } }

        private static readonly UdfHazirlayici.Alan[] _giderAlani = { new UdfHazirlayici.Alan("JDT1", "BE1_GIDER", "Gider / Gelir Kodu", 100) };
        private static string FaturaSatirTablosu(int tt)  {return default;
}

        [HttpPost("KodKaydet")]
        public async Task<IActionResult> KodKaydet([FromBody] KodIstek req)
 {return Json(new { success = true, kismi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("kismi", 0), yazilan = global::WebApplication3.OrnekDoldurucu.Deger<int>("yazilan", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), hatalar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatalar", i2)).ToList() });
}

        private static readonly string[] AyAdlari = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };

        private static void Baslik(IXLWorksheet ws, string[] basliklar)
 {}

        [HttpGet("Excel")]
        public IActionResult Excel(string db, int yil = 0, string tur = "Gider", int ay = 0)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("SatirlarExcel")]
        public IActionResult SatirlarExcel(string db, int yil, int ay = 0, string kok = null, string tur = "Gider")
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}