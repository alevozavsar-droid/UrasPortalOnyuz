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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{




























    [Authorize]
    [Route("[controller]")]
    public class UrasUretimUrunAgaciController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UrasUretimUrunAgaciController> _logger;

        private bool TamYetkiliMi()  {return default;
}

        private bool GercekAd()  {return default;
}

        private JsonResult Veri(Func<object> f)
 {return default;
}

        private static readonly string ReceteMaliyetCte = @"
WITH Fiyat AS (
    SELECT
        I.ItemCode,
        CASE WHEN ISNULL(I.LastPurPrc, 0) > 0 THEN I.LastPurPrc ELSE F.Price END AS Fiyat,
        CASE WHEN ISNULL(I.LastPurPrc, 0) > 0 THEN ISNULL(NULLIF(I.LastPurCur, ''), 'TRY')
             ELSE ISNULL(NULLIF(F.Currency, ''), 'TRY') END AS ParaBirimi,
        CASE WHEN ISNULL(I.LastPurPrc, 0) > 0 THEN N'Son alış fiyatı'
             WHEN F.Price IS NOT NULL THEN N'Son fatura' END AS Kaynak
    FROM OITM I
    OUTER APPLY (
        SELECT TOP 1 L.Price, L.Currency
        FROM PCH1 L
        INNER JOIN OPCH P ON P.DocEntry = L.DocEntry
        WHERE L.ItemCode = I.ItemCode AND ISNULL(L.Price, 0) > 0
        ORDER BY P.DocDate DESC, P.DocEntry DESC
    ) F
),
Patlat AS (
    SELECT T.Code AS Kok, L.Code AS Bilesen,
           CAST(L.Quantity / NULLIF(T.Qauntity, 0) AS DECIMAL(38, 12)) AS EtkinMiktar,
           1 AS Seviye,
           CAST('|' + T.Code + '|' + L.Code + '|' AS NVARCHAR(4000)) AS Yol
    FROM OITT T
    INNER JOIN ITT1 L ON L.Father = T.Code
    WHERE T.Qauntity > 0
    UNION ALL
    SELECT P.Kok, L.Code,
           CAST(P.EtkinMiktar * L.Quantity / NULLIF(T2.Qauntity, 0) AS DECIMAL(38, 12)),
           P.Seviye + 1,
           CAST(P.Yol + L.Code + '|' AS NVARCHAR(4000))
    FROM Patlat P
    INNER JOIN OITT T2 ON T2.Code = P.Bilesen AND T2.Qauntity > 0
    INNER JOIN ITT1 L ON L.Father = P.Bilesen
    WHERE P.Seviye < 10 AND P.Yol NOT LIKE '%|' + L.Code + '|%'
),
KokMaliyet AS (
    SELECT
        P.Kok,
        SUM(CASE WHEN F.ParaBirimi = 'TRY' THEN P.EtkinMiktar * F.Fiyat ELSE 0 END) AS MaliyetTRY,
        SUM(CASE WHEN F.ParaBirimi = 'USD' THEN P.EtkinMiktar * F.Fiyat ELSE 0 END) AS MaliyetUSD,
        SUM(CASE WHEN F.ParaBirimi = 'EUR' THEN P.EtkinMiktar * F.Fiyat ELSE 0 END) AS MaliyetEUR,
        SUM(CASE WHEN F.Fiyat IS NULL THEN 1 ELSE 0 END) AS FiyatsizBilesen
    FROM Patlat P
    LEFT JOIN Fiyat F ON F.ItemCode = P.Bilesen
    WHERE NOT EXISTS (SELECT 1 FROM OITT T3 WHERE T3.Code = P.Bilesen AND T3.Qauntity > 0)
    GROUP BY P.Kok
)";

        [HttpGet("ReceteMaliyet")]
        [HttpGet("")]
        public IActionResult ReceteMaliyet()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("ReceteMaliyet");
}

        [HttpGet("GetReceteMaliyetListe")]
        public JsonResult GetReceteMaliyetListe(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetReceteMaliyet")]
        public JsonResult GetReceteMaliyet(string code)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("ReceteMaliyetExcel")]
        public IActionResult ReceteMaliyetExcel(string code)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}





        [HttpGet("Index")]
        public IActionResult Index()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetOWHS")]
        public JsonResult GetOWHS()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetOITM")]
        public JsonResult GetOITM()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetOITMGizliKod")]
        public JsonResult GetOITMGizliKod()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetITT1")]
        public JsonResult GetITT1(string code)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetKalemMaliyet")]
        public JsonResult GetKalemMaliyet(string code)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetUrunAgaciBaslik")]
        public JsonResult GetUrunAgaciBaslik(string code)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class ProductTreeSatirDto
        {
            public string No { get; set; }
            public decimal Quantity { get; set; }
            public string Warehouse { get; set; }
            public int ChildNum { get; set; } = -1;
        }

        public class ProductTreeKaydetDto
        {
            public string ItemCode { get; set; }
            public decimal Quantity { get; set; }
            public string Warehouse { get; set; }
            public List<ProductTreeSatirDto> Items { get; set; }
        }

        private async Task<HttpClient> SlOturumAc()
 {return default;
}

        private static async Task SlOturumKapat(HttpClient client)
 {}

        private static string SlHataMesaji(string govde)
 {return default;
}



        private async Task<string> SlIstek(HttpClient client, HttpMethod metot, string yol, object govde = null, bool tumKoleksiyonuDegistir = false)
 {return default;
}

        [HttpPost("addProductTree")]
        public async Task<JsonResult> AddProductTree([FromBody] ProductTreeKaydetDto model)
 {return Json(new { success = true, message = "Ürün ağacı eklendi." });
}

        [HttpPost("updateProductTree")]
        public async Task<JsonResult> UpdateProductTree([FromBody] ProductTreeKaydetDto model)
 {return Json(new { success = true, message = "Ürün ağacı güncellendi." });
}

        [HttpPost("deleteProductTreeLines")]
        public async Task<JsonResult> DeleteProductTreeLines(string itemCode, int visOrder)
 {return Json(new { success = true, message = "Satır silindi." });
}
    }
}
