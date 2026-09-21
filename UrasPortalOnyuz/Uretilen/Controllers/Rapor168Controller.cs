// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{










    [Route("Rapor168")]
    public class Rapor168Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private readonly ILogger<Rapor168Controller> _logger;
        private static readonly string Klasor = "ibkb";
        private static int _semaHazir;


        private List<string> BaglantiAnahtarlari()  {return default;
}

        private string GetSelectedDatabase()
 {return default;
}

        private string DbAdi(string key)
 {return default;
}


        private string DbKeyForDbName(string dbName)
 {return default;
}
        private string Kullanici()  {return default;
}
        private bool Yonetici()  {return default;
}
        private static string H(string s)  {return default;
}


        public class Beyanname
        {
            public int Id { get; set; }
            public string SirketDb { get; set; }
            public string DosyaNo { get; set; }
            public int? FaturaDocEntry { get; set; } public string FaturaNo { get; set; } public DateTime? FaturaTarihi { get; set; }
            public string CardCode { get; set; } public string CardName { get; set; }
            public string UlkeKodu { get; set; } public string UlkeAdi { get; set; }
            public string BeyannameNo { get; set; } public DateTime? BeyannameTarihi { get; set; } public DateTime? FiiliIhracTarihi { get; set; } public DateTime? VadeTarihi { get; set; }
            public string GumrukMudurlugu { get; set; }
            public string Doviz { get; set; } public decimal? BeyannameTutar { get; set; } public decimal? FaturaTutar { get; set; }
            public string UlkeGerekliligi { get; set; } public decimal? UlkeOran { get; set; }
            public string IbkbGerekliligi { get; set; }
            public string IbkbNo { get; set; } public DateTime? IbkbTarihi { get; set; } public decimal? IbkbTutar { get; set; } public string IbkbBanka { get; set; }
            public string DabNo { get; set; }
            public string Durum { get; set; }
            public string Aciklama { get; set; }
            public string Olusturan { get; set; } public DateTime OlusturmaTarihi { get; set; }
            public string Guncelleyen { get; set; } public DateTime? GuncellemeTarihi { get; set; }
            public int DosyaSayisi { get; set; }

            public decimal? BeyannameTutarUsd { get; set; }
            public decimal? IbkbTutarUsd { get; set; }

            public decimal GerekenTutar => Math.Round((BeyannameTutar ?? 0) * ((UlkeOran ?? 100) / 100m), 2);
            public decimal KalanTutar => Math.Max(0, GerekenTutar - (IbkbTutar ?? 0));





            public decimal? GerekenTutarUsd => BeyannameTutarUsd.HasValue ? Math.Round(Math.Max(0, BeyannameTutarUsd.Value - 15000m) * ((UlkeOran ?? 100) / 100m), 2) : (decimal?)null;
            public decimal? KalanTutarUsd => GerekenTutarUsd.HasValue ? Math.Max(0, GerekenTutarUsd.Value - (IbkbTutarUsd ?? 0)) : (decimal?)null;
            public int? KalanGun => VadeTarihi.HasValue ? (int?)(VadeTarihi.Value.Date - DateTime.Today).Days : null;
        }

        public class UlkeKurali
        {
            public string UlkeKodu { get; set; } public string UlkeAdi { get; set; }
            public string Gereklilik { get; set; } public decimal Oran { get; set; } public bool IbkbGerekli { get; set; }
            public string Not_ { get; set; } public string Guncelleyen { get; set; } public DateTime GuncellemeTarihi { get; set; }
        }


        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()
 {ViewBag.CurrentDbDisplay = "";
ViewBag.CurrentDbKey = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        [HttpGet("Liste")]
        public IActionResult Liste(string durum = null, string sirket = null, string q = null)
 {return Json(new { success = true, data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { Id = global::WebApplication3.OrnekDoldurucu.Deger<int>("Id", i2), SirketDb = global::WebApplication3.OrnekDoldurucu.Deger<string>("SirketDb", i2), DosyaNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("DosyaNo", i2), FaturaDocEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("FaturaDocEntry", i2), FaturaNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("FaturaNo", i2), FaturaTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("FaturaTarihi", i2), CardCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("CardCode", i2), CardName = global::WebApplication3.OrnekDoldurucu.Deger<string>("CardName", i2), UlkeKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("UlkeKodu", i2), UlkeAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("UlkeAdi", i2), BeyannameNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("BeyannameNo", i2), BeyannameTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("BeyannameTarihi", i2), FiiliIhracTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("FiiliIhracTarihi", i2), VadeTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("VadeTarihi", i2), GumrukMudurlugu = global::WebApplication3.OrnekDoldurucu.Deger<string>("GumrukMudurlugu", i2), Doviz = global::WebApplication3.OrnekDoldurucu.Deger<string>("Doviz", i2), BeyannameTutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("BeyannameTutar", i2), FaturaTutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("FaturaTutar", i2), BeyannameTutarUsd = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("BeyannameTutarUsd", i2), UlkeGerekliligi = global::WebApplication3.OrnekDoldurucu.Deger<string>("UlkeGerekliligi", i2), UlkeOran = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("UlkeOran", i2), IbkbGerekliligi = global::WebApplication3.OrnekDoldurucu.Deger<string>("IbkbGerekliligi", i2), IbkbNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("IbkbNo", i2), IbkbTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("IbkbTarihi", i2), IbkbTutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("IbkbTutar", i2), IbkbTutarUsd = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("IbkbTutarUsd", i2), IbkbBanka = global::WebApplication3.OrnekDoldurucu.Deger<string>("IbkbBanka", i2), DabNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("DabNo", i2), Durum = global::WebApplication3.OrnekDoldurucu.Deger<string>("Durum", i2), Aciklama = global::WebApplication3.OrnekDoldurucu.Deger<string>("Aciklama", i2), Olusturan = global::WebApplication3.OrnekDoldurucu.Deger<string>("Olusturan", i2), OlusturmaTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("OlusturmaTarihi", i2), Guncelleyen = global::WebApplication3.OrnekDoldurucu.Deger<string>("Guncelleyen", i2), GuncellemeTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("GuncellemeTarihi", i2), DosyaSayisi = global::WebApplication3.OrnekDoldurucu.Deger<int>("DosyaSayisi", i2), GerekenTutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("GerekenTutar", i2), KalanTutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("KalanTutar", i2), GerekenTutarUsd = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("GerekenTutarUsd", i2), KalanTutarUsd = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("KalanTutarUsd", i2), KalanGun = global::WebApplication3.OrnekDoldurucu.Deger<int>("KalanGun", i2) }).ToList(), ozet = new { toplam = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplam", 0), acik = global::WebApplication3.OrnekDoldurucu.Deger<int>("acik", 0), kapandi = global::WebApplication3.OrnekDoldurucu.Deger<int>("kapandi", 0), muaf = global::WebApplication3.OrnekDoldurucu.Deger<int>("muaf", 0), vadesiGecen = global::WebApplication3.OrnekDoldurucu.Deger<int>("vadesiGecen", 0), vadeYaklasan = global::WebApplication3.OrnekDoldurucu.Deger<int>("vadeYaklasan", 0), dovizler = global::System.Linq.Enumerable.Range(0, 12).Select(i3 => new { doviz = global::WebApplication3.OrnekDoldurucu.Deger<string>("doviz", i3), gereken = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("gereken", i3), getirilen = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("getirilen", i3), kalan = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("kalan", i3) }).ToList(), gerekenUsdToplam = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("gerekenUsdToplam", 0), kalanUsdToplam = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("kalanUsdToplam", 0), kuruBulunamayan = global::WebApplication3.OrnekDoldurucu.Deger<int>("kuruBulunamayan", 0) }, sirketler = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("sirketler", i2)).ToList() });
}





        [HttpGet("SapFaturalar")]
        public IActionResult SapFaturalar(DateTime? baslangic = null, DateTime? bitis = null, bool sadeceDosyali = false)
 {return Json(new { success = true, sirket = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirket", 0), data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", i2), docNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("docNum", i2), docDate = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("docDate", i2), numAtCard = global::WebApplication3.OrnekDoldurucu.Deger<string>("numAtCard", i2), cardCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("cardCode", i2), cardName = global::WebApplication3.OrnekDoldurucu.Deger<string>("cardName", i2), ulkeKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("ulkeKodu", i2), ulkeAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("ulkeAdi", i2), doviz = global::WebApplication3.OrnekDoldurucu.Deger<string>("doviz", i2), tutar = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("tutar", i2), tutarTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("tutarTL", i2), dosyaNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("dosyaNo", i2), nakliyeTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("nakliyeTarihi", i2), beyannameNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("beyannameNo", i2), faturaDurum = global::WebApplication3.OrnekDoldurucu.Deger<string>("faturaDurum", i2), kayitliId = global::WebApplication3.OrnekDoldurucu.Deger<int>("kayitliId", i2) }).ToList() });
}

        public class KayitIstek
        {
            public int Id { get; set; }
            public string SirketDb { get; set; } public string DosyaNo { get; set; }
            public int? FaturaDocEntry { get; set; } public string FaturaNo { get; set; } public DateTime? FaturaTarihi { get; set; }
            public string CardCode { get; set; } public string CardName { get; set; }
            public string UlkeKodu { get; set; } public string UlkeAdi { get; set; }
            public string BeyannameNo { get; set; } public DateTime? BeyannameTarihi { get; set; } public DateTime? FiiliIhracTarihi { get; set; } public DateTime? VadeTarihi { get; set; }
            public string GumrukMudurlugu { get; set; }
            public string Doviz { get; set; } public decimal? BeyannameTutar { get; set; } public decimal? FaturaTutar { get; set; }
            public string UlkeGerekliligi { get; set; } public decimal? UlkeOran { get; set; } public string IbkbGerekliligi { get; set; }
            public string IbkbNo { get; set; } public DateTime? IbkbTarihi { get; set; } public decimal? IbkbTutar { get; set; } public string IbkbBanka { get; set; }
            public string DabNo { get; set; } public string Durum { get; set; } public string Aciklama { get; set; }

            public decimal? BeyannameTutarUsd { get; set; }
            public decimal? IbkbTutarUsd { get; set; }
        }


        [HttpPost("Kaydet")]
        public IActionResult Kaydet([FromBody] KayitIstek r)
 {return Json(new { success = true, id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0), durum = global::WebApplication3.OrnekDoldurucu.Deger<string>("durum", 0), message = "Beyanname güncellendi." });
}






        [HttpPost("VadeTarihleriDuzelt")]
        public IActionResult VadeTarihleriDuzelt()
 {return Json(new { success = true, guncellenen = global::WebApplication3.OrnekDoldurucu.Deger<int>("guncellenen", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("Sil")]
        public IActionResult Sil([FromBody] KayitIstek r)
 {return Json(new { success = true, message = "Beyanname kaydı ve belgeleri silindi." });
}


        [HttpGet("Dosyalar")]
        public IActionResult Dosyalar(int beyannameId)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Id", "BeyannameId", "Tur", "DosyaAdi", "DosyaYolu", "Boyut", "Yukleyen", "Tarih" }) });
}


        [HttpPost("DosyaYukle")]
        public async Task<IActionResult> DosyaYukle(IFormFile dosya, int beyannameId, string tur = "DIGER")
 {return Json(new { success = true, id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0), yol = global::WebApplication3.OrnekDoldurucu.Deger<string>("yol", 0), message = "Belge yüklendi." });
}

        [HttpPost("DosyaSil")]
        public IActionResult DosyaSil(int id)
 {return Json(new { success = true, message = "Belge silindi." });
}



        [HttpGet("UlkeKurallari")]
        public IActionResult UlkeKurallari()
 {return Json(new { success = true, data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { ulkeKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("ulkeKodu", i2), ulkeAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("ulkeAdi", i2), tanimli = global::WebApplication3.OrnekDoldurucu.Deger<bool>("tanimli", i2), gereklilik = global::WebApplication3.OrnekDoldurucu.Deger<string>("gereklilik", i2), oran = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("oran", i2), ibkbGerekli = global::WebApplication3.OrnekDoldurucu.Deger<bool>("ibkbGerekli", i2), not = global::WebApplication3.OrnekDoldurucu.Deger<string>("not", i2), guncelleyen = global::WebApplication3.OrnekDoldurucu.Deger<string>("guncelleyen", i2), guncellemeTarihi = global::WebApplication3.OrnekDoldurucu.Deger<global::System.DateTime>("guncellemeTarihi", i2), beyannameSayisi = global::WebApplication3.OrnekDoldurucu.Deger<int>("beyannameSayisi", i2) }).ToList() });
}

        public class UlkeKuraliIstek { public string UlkeKodu { get; set; } public string UlkeAdi { get; set; } public string Gereklilik { get; set; } public decimal? Oran { get; set; } public bool IbkbGerekli { get; set; } = true; public string Not { get; set; } public bool Sil { get; set; } }

        [HttpPost("UlkeKuraliKaydet")]
        public IActionResult UlkeKuraliKaydet([FromBody] UlkeKuraliIstek r)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpGet("Excel")]
        public IActionResult Excel(string durum = null, string sirket = null)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}
