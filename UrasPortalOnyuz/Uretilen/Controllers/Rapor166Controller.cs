// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{







    [Authorize]
    [Route("[controller]")]
    public class Rapor166Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor166Controller> _logger;
        private readonly QnbEArsivServisi _eArsiv;

        private string DbKey()  {return default;
}
        private string Baglanti()  {return default;
}
        private string DbAdi()  {return default;
}

        private static readonly Dictionary<int, (string Hdr, string Ln, string Ad, bool Satis)> _tablolar = new Dictionary<int, (string, string, string, bool)>
        {
            { 13, ("OINV", "INV1", "Satış Faturası", true) },
            { 14, ("ORIN", "RIN1", "Satış İade Faturası", true) },
            { 18, ("OPCH", "PCH1", "Satınalma Faturası", false) },
            { 19, ("ORPC", "RPC1", "Satınalma İade Faturası", false) }
        };

        [HttpGet]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("Cariler")]
        public IActionResult Cariler(string q)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName", "CardType" }) });
}


        [HttpGet("Liste")]
        public IActionResult Liste(int tur = 0, string bas = null, string bit = null, string cardCode = null, string belgeNo = null, string faturaNo = null, decimal? minTutar = null, decimal? maxTutar = null, bool iptalDahil = false, string durum = null)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ObjType", "DocEntry", "DocNum", "DocDate", "DocDueDate", "CardCode", "CardName", "NumAtCard", "DocTotal", "DocTotalFC", "DocCur", "VatSum", "PaidToDate", "DocStatus", "Canceled", "DocType", "TransId", "EfatNo", "Uuid", "Aktar", "SlpName", "KodEksik" }), sinir = global::WebApplication3.OrnekDoldurucu.Deger<bool>("sinir", 0) });
}


        [HttpGet("Detay")]
        public IActionResult Detay(int objType, int docEntry)
 {return Json(new { success = true, baslik = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("baslik", 0), satirlar = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate", "CardCode", "CardName", "NumAtCard", "DocTotal", "DocTotalFC", "DocCur", "DocRate", "VatSum", "DiscSum", "WTSum", "PaidToDate", "DocStatus", "Canceled", "DocType", "TransId", "Comments", "Address", "Address2", "PayToCode", "ShipToCode", "SlpName", "Sahip", "Olusturan", "CreateDate", "OdemeKosulu", "Vkn", "Tel", "Mail", "EfatNo", "Uuid", "Aktar", "Send", "Muaf" }), yevmiye = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate", "CardCode", "CardName", "NumAtCard", "DocTotal", "DocTotalFC", "DocCur", "DocRate", "VatSum", "DiscSum", "WTSum", "PaidToDate", "DocStatus", "Canceled", "DocType", "TransId", "Comments", "Address", "Address2", "PayToCode", "ShipToCode", "SlpName", "Sahip", "Olusturan", "CreateDate", "OdemeKosulu", "Vkn", "Tel", "Mail", "EfatNo", "Uuid", "Aktar", "Send", "Muaf" }), odemeler = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate", "CardCode", "CardName", "NumAtCard", "DocTotal", "DocTotalFC", "DocCur", "DocRate", "VatSum", "DiscSum", "WTSum", "PaidToDate", "DocStatus", "Canceled", "DocType", "TransId", "Comments", "Address", "Address2", "PayToCode", "ShipToCode", "SlpName", "Sahip", "Olusturan", "CreateDate", "OdemeKosulu", "Vkn", "Tel", "Mail", "EfatNo", "Uuid", "Aktar", "Send", "Muaf" }) });
}


        private class FaturaBilgi
        {
            public int ObjType, DocEntry, DocNum;
            public string CardCode, CardName, NumAtCard, Uuid, EfatNo, VknCari;
            public DateTime? DocDate;
            public string Yon => (ObjType == 18 || ObjType == 19) ? "GELEN" : "GIDEN";
        }
        private static readonly ConcurrentDictionary<string, string> _ettnOnbellek = new ConcurrentDictionary<string, string>();
        private bool TestOrtami()  {return default;
}


        private async Task<(FaturaBilgi F, string Ettn, string Hata, string Ipucu)> EttnCozAsync(int objType, int docEntry)
 {return default;
}

        [HttpGet("FaturaGoruntuDurum")]
        public async Task<IActionResult> FaturaGoruntuDurum(int objType, int docEntry)
 {return Json(new { success = true, bulundu = global::WebApplication3.OrnekDoldurucu.Deger<bool>("bulundu", 0), yon = global::WebApplication3.OrnekDoldurucu.Deger<string>("yon", 0), faturaNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("faturaNo", 0), kanal = global::WebApplication3.OrnekDoldurucu.Deger<string>("kanal", 0), kaynak = global::WebApplication3.OrnekDoldurucu.Deger<string>("kaynak", 0), test = global::WebApplication3.OrnekDoldurucu.Deger<bool>("test", 0), mesaj = global::WebApplication3.OrnekDoldurucu.Deger<string>("mesaj", 0), ipucu = global::WebApplication3.OrnekDoldurucu.Deger<string>("ipucu", 0) });
}

        [HttpGet("FaturaGoruntu")]
        public async Task<IActionResult> FaturaGoruntu(int objType, int docEntry, string format = "HTML")
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}





        [HttpGet("GelenEFaturalar")]
        public IActionResult GelenEFaturalar(string bas = null, string bit = null, string ara = null, string durum = "yok", string cardCode = null)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "118", "DocEntry", "DocNum", "X" }), indeksAdet = 0, indeksSonTarih = "", toplam = 0, uyari = "Seçilen cari bulunamadı." });
}

        private static string TarihStr(string yyyymmdd)
 {return default;
}


        [HttpPost("GelenDurumlar")]
        public async Task<IActionResult> GelenDurumlar([FromBody] List<string> ettnler, bool zorla = false)
 {return Json(new { success = true, data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { ettn = global::WebApplication3.OrnekDoldurucu.Deger<string>("ettn", i2), durum = global::WebApplication3.OrnekDoldurucu.Deger<string>("durum", i2), iptal = global::WebApplication3.OrnekDoldurucu.Deger<bool>("iptal", i2), detay = global::WebApplication3.OrnekDoldurucu.Deger<string>("detay", i2), hata = global::WebApplication3.OrnekDoldurucu.Deger<string>("hata", i2) }).ToList() });
}


        [HttpGet("GelenGoruntu")]
        public async Task<IActionResult> GelenGoruntu(string ettn, string format = "HTML")
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}


        [HttpGet("GelenIndeksDurum")]
        public IActionResult GelenIndeksDurum()
 {return Json(new { success = true, vkn = global::WebApplication3.OrnekDoldurucu.Deger<string>("vkn", 0), adet = global::WebApplication3.OrnekDoldurucu.Deger<long>("adet", 0), sonSira = global::WebApplication3.OrnekDoldurucu.Deger<long>("sonSira", 0), ilkTarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("ilkTarih", 0), sonTarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("sonTarih", 0), sonGuncelleme = global::WebApplication3.OrnekDoldurucu.Deger<string>("sonGuncelleme", 0) });
}


        [HttpPost("GelenIndeksTara")]
        public async Task<IActionResult> GelenIndeksTara(int sayfa = 40, bool tam = false)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), eklenen = global::WebApplication3.OrnekDoldurucu.Deger<int>("eklenen", 0), sayfa = global::WebApplication3.OrnekDoldurucu.Deger<int>("sayfa", 0), bitti = global::WebApplication3.OrnekDoldurucu.Deger<bool>("bitti", 0), adet = global::WebApplication3.OrnekDoldurucu.Deger<long>("adet", 0), sonSira = global::WebApplication3.OrnekDoldurucu.Deger<long>("sonSira", 0), sonTarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("sonTarih", 0) });
}
    }
}
