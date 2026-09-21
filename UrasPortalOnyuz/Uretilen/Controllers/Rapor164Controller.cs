// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using System.Linq;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{






    [Authorize]
    [Route("[controller]")]
    public class Rapor164Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor164Controller> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _email;



        private bool Onaylayici()
 {return default;
}
        private string BildirimMail()  {return default;
}

        private static string TipAdi(string tip)  {return default;
}

        private void TalepMailiGonder(GiderGelirKatalog.KodTalep t, string konu, string ustMetin, IEnumerable<string> alicilar, IEnumerable<string> cc)
 {}

        public class TalepIstek { public string Tip { get; set; } public string AnaGrup { get; set; } public string AltGrup { get; set; } public string Kalem { get; set; } public string Aciklama { get; set; } public string HesapKodu { get; set; } public string Ekran { get; set; } }


        [HttpPost("TalepOlustur")]
        public IActionResult TalepOlustur([FromBody] TalepIstek req)
 {return Json(new { success = true, id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpGet("Talepler")]
        public IActionResult Talepler(string durum = null)
 {return Json(new { success = true, onaylayici = global::WebApplication3.OrnekDoldurucu.Deger<bool>("onaylayici", 0), kullanici = global::WebApplication3.OrnekDoldurucu.Deger<string>("kullanici", 0), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.GiderGelirKatalog.KodTalep>(12) });
}

        [HttpGet("BekleyenTalepSayisi")]
        public IActionResult BekleyenTalepSayisi()
 {return Json(new { success = true, adet = global::WebApplication3.OrnekDoldurucu.Deger<int>("adet", 0), onaylayici = global::WebApplication3.OrnekDoldurucu.Deger<bool>("onaylayici", 0) });
}

        [HttpPost("TalepOnayla")]
        public IActionResult TalepOnayla(int id, string not = null)
 {return Json(new { success = true, kokKod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kokKod", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("TalepDuzenle")]
        public IActionResult TalepDuzenle(int id, string tip, string anaGrup, string altGrup, string kalem, string aciklama = null, string hesapKodu = null)
 {return Json(new { success = true, message = "Talep güncellendi.", data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Services.GiderGelirKatalog.KodTalep>() });
}

        [HttpPost("TalepReddet")]
        public IActionResult TalepReddet(int id, string not = null)
 {return Json(new { success = true, message = "Talep reddedildi." });
}
        private string Kullanici()  {return default;
}


        private string SeciliDbAdi()
 {return default;
}


        private string DbKeyForDbName(string dbName)
 {return default;
}



        private int SabitiGecmiseUygula(string cardCode, string tip, string tamKod, string sirketDbAdi = null)
 {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.SeciliDb = "";
ViewBag.SirketKodu = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        public class KodKullanimSatiri
        {
            public string DbKey { get; set; } public string DbName { get; set; } public string SirketKodu { get; set; } public bool KopyaMi { get; set; }
            public string Kaynak { get; set; } public int TransId { get; set; } public int LineId { get; set; } public int DocEntry { get; set; }
            public string BelgeTuru { get; set; } public string BelgeNo { get; set; } public string Tarih { get; set; } public int Yil { get; set; } public int Ay { get; set; }
            public string Cari { get; set; } public string HesapKodu { get; set; } public string HesapAdi { get; set; } public decimal Tutar { get; set; } public string Kod { get; set; }
        }

        private static string BelgeTuruAdi(int tt)  {return default;
}


        [HttpGet("KodKullanim")]
        public IActionResult KodKullanim(string kokKod)
 {return Json(new { success = true, kokKod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kokKod", 0), toplam = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplam", 0), ozet = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { dbName = global::WebApplication3.OrnekDoldurucu.Deger<string>("dbName", i2), sirketKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirketKodu", i2), kopyaMi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("kopyaMi", i2), adet = global::WebApplication3.OrnekDoldurucu.Deger<int>("adet", i2), tutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("tutar", i2) }).ToList(), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor164Controller.KodKullanimSatiri>(12), sabitler = new object[0], talepler = new object[0], hatalar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatalar", i2)).ToList(), sure = global::WebApplication3.OrnekDoldurucu.Deger<int>("sure", 0), taranan = global::WebApplication3.OrnekDoldurucu.Deger<int>("taranan", 0) });
}


        [HttpPost("KalemSil")]
        public IActionResult KalemSil(int id)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpGet("Liste")]
        public IActionResult Liste(string tip = null)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.GiderGelirKatalog.Kalem>(12) });
}


        [HttpGet("Excel")]
        public IActionResult Excel(string tip = null)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("Sirketler")]
        public IActionResult Sirketler()
 {return Json(new { success = true, sirketler = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.GiderGelirKatalog.Sirket>(12), dbler = new object[0] });
}


        [HttpGet("Kodlar")]
        public IActionResult Kodlar(string tip = null)
 {return Json(new { success = true, sirketKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirketKodu", 0), dbAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("dbAdi", 0), uyari = global::WebApplication3.OrnekDoldurucu.Deger<string>("uyari", 0), data = new object[0] });
}


        [HttpGet("Oneri")]
        public IActionResult Oneri(string hesap)
 {return Json(new { success = true, kokKod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kokKod", 0), kod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kod", 0) });
}


        [HttpPost("Kaydet")]
        public IActionResult Kaydet([FromBody] GiderGelirKatalog.Kalem k)
 {return Json(new { success = true, id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0), kokKod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kokKod", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("AktifDegistir")]
        public IActionResult AktifDegistir(int id, bool aktif)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SirketKaydet")]
        public IActionResult SirketKaydet(string sirketKodu, string ad, bool aktif = true)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("SirketDbKaydet")]
        public IActionResult SirketDbKaydet(string dbName, string sirketKodu)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("IceAktar")]
        [RequestSizeLimit(50_000_000)]
        public IActionResult IceAktar(IFormFile dosya, bool temizle = false)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), uyarilar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("uyarilar", i2)).ToList() });
}

        public class SabitIstek { public string CardCode { get; set; } public string CardName { get; set; } public string Tip { get; set; } public string Kod { get; set; } public string Aciklama { get; set; } }


        [HttpGet("SabitCariler")]
        public IActionResult SabitCariler()
 {return Json(new { success = true, onaylayici = global::WebApplication3.OrnekDoldurucu.Deger<bool>("onaylayici", 0), sirketKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirketKodu", 0), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.GiderGelirKatalog.SabitCari>(12) });
}


        [HttpPost("SabitCariKaydet")]
        public IActionResult SabitCariKaydet([FromBody] SabitIstek req)
 {return Json(new { success = true, id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0), onayli = global::WebApplication3.OrnekDoldurucu.Deger<bool>("onayli", 0), guncellenen = global::WebApplication3.OrnekDoldurucu.Deger<int>("guncellenen", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SabitCariOnayla")]
        public IActionResult SabitCariOnayla(int id, string not = null)
 {return Json(new { success = true, guncellenen = global::WebApplication3.OrnekDoldurucu.Deger<int>("guncellenen", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SabitCariReddet")]
        public IActionResult SabitCariReddet(int id, string not = null)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SabitCariSil")]
        public IActionResult SabitCariSil(int id)
 {return Json(new { success = true, message = "Sabit tanım kaldırıldı." });
}







        [HttpPost("SabitleriTopluSenkronizeEt")]
        public IActionResult SabitleriTopluSenkronizeEt()
 {return Json(new { success = true, toplamGuncellenen = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplamGuncellenen", 0), hataSayisi = global::WebApplication3.OrnekDoldurucu.Deger<int>("hataSayisi", 0), sonuclar = new object[0] });
}


        [HttpGet("SabitKod")]
        public IActionResult SabitKod(string cardCode, string tip)
 {return Json(new { success = true, kod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kod", 0), kokKod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kokKod", 0), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", 0), tip = global::WebApplication3.OrnekDoldurucu.Deger<string>("tip", 0) });
}


        [HttpGet("KodGecis")]
        public IActionResult KodGecis()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.GiderGelirKatalog.KodGecis>(12) });
}
    }
}
