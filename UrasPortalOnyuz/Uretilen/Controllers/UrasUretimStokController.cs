// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
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
    public class UrasUretimStokController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UrasUretimStokController> _logger;

        private bool HammaddeGorebilir()  {return default;
}

        private JsonResult Veri(Func<object> f)
 {return default;
}

        private static readonly string DepoStokKaynak = @"
    SELECT
        T1.ItemCode,
        T1.WhsCode,
        ISNULL(T0.DistNumber,'') AS DistNumber,
        T0.ExpDate,
        T0.InDate,
        SUM(T1.Quantity)         AS Quantity
    FROM OBTN T0
    INNER JOIN OBTQ T1 ON T0.AbsEntry = T1.MdAbsEntry
    GROUP BY T1.ItemCode, T1.WhsCode, T0.DistNumber, T0.ExpDate, T0.InDate
    UNION ALL
    SELECT
        T2.ItemCode,
        T2.WhsCode,
        ''   AS DistNumber,
        NULL AS ExpDate,
        NULL AS InDate,
        ISNULL(T2.OnHand, 0) AS Quantity
    FROM OITW T2
    INNER JOIN OITM T3 ON T2.ItemCode = T3.ItemCode
    WHERE ISNULL(T3.ManBtchNum,'N') = 'N'";

        private string DepoStokWhere(string search, string warehouse, string partiDurum, bool sifirGoster)
 {return default;
}

        [HttpGet("DepoStokRaporu")]
        [HttpGet("")]
        public IActionResult DepoStokRaporu()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("DepoStokRaporu");
}

        [HttpGet("GetWarehouses")]
        public JsonResult GetWarehouses()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetDepoStokRaporu")]
        public JsonResult GetDepoStokRaporu(string search = "", string warehouse = "", string partiDurum = "", bool sifirGoster = false, int page = 1, int pageSize = 50)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetDepoStokPartiDetay")]
        public JsonResult GetDepoStokPartiDetay(string itemCode, string warehouse = "", string partiDurum = "", bool sifirGoster = false)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("DepoStokRaporuExcel")]
        public IActionResult DepoStokRaporuExcel(string search = "", string warehouse = "", string partiDurum = "", bool sifirGoster = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}



        private static string StokHammaddeFiltresi(bool hammaddeGorebilir, string itemCodeCol)
 {return default;
}



        [HttpGet("MalGirisi")]
        public IActionResult MalGirisi()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("MalGirisi");
}

        [HttpGet("GetItemsForMalGirisi")]
        public JsonResult GetItemsForMalGirisi(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMalGirisiSeries")]
        public JsonResult GetMalGirisiSeries()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMalGirisiGecmis")]
        public JsonResult GetMalGirisiGecmis(string search = "", int page = 1, int pageSize = 50)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMalGirisiDetay")]
        public JsonResult GetMalGirisiDetay(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpGet("MalCikisi")]
        public IActionResult MalCikisi()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("MalCikisi");
}

        [HttpGet("GetMalCikisiSeries")]
        public JsonResult GetMalCikisiSeries()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMalCikisiGecmis")]
        public JsonResult GetMalCikisiGecmis(string search = "", int page = 1, int pageSize = 50)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMalCikisiDetay")]
        public JsonResult GetMalCikisiDetay(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetBatchesForMalCikisi")]
        public JsonResult GetBatchesForMalCikisi(string itemCode, string whsCode = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpGet("DepoNakli")]
        public IActionResult DepoNakli()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("DepoNakli");
}

        [HttpGet("GetDepoNakliSeries")]
        public JsonResult GetDepoNakliSeries()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetDepoNakliGecmis")]
        public JsonResult GetDepoNakliGecmis(string search = "", int page = 1, int pageSize = 50)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetDepoNakliDetay")]
        public JsonResult GetDepoNakliDetay(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpGet("StokKayitListesi")]
        public IActionResult StokKayitListesi()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("StokKayitListesi");
}

        private string StokKayitWhere(string search, string warehouse, int? transType, string yon, DateTime? dateFrom, DateTime? dateTo)
 {return default;
}

        private static readonly string BelgeTipiCase = @"
CASE T0.TransType
    WHEN -2       THEN 'Açılış Bakiyesi'
    WHEN 13       THEN 'Satış Faturası'
    WHEN 14       THEN 'Satış Alacak Dekontu'
    WHEN 15       THEN 'Teslimat'
    WHEN 16       THEN 'Satış İade'
    WHEN 18       THEN 'Satınalma Faturası'
    WHEN 19       THEN 'Satınalma Alacak Dekontu'
    WHEN 20       THEN 'Satınalma Teslimatı'
    WHEN 21       THEN 'Satınalma İade'
    WHEN 58       THEN 'Stok Sayım Kaydı'
    WHEN 59       THEN 'Mal Girişi'
    WHEN 60       THEN 'Mal Çıkışı'
    WHEN 67       THEN 'Stok Transferi'
    WHEN 69       THEN 'İthalat Maliyetleri'
    WHEN 162      THEN 'Stok Değerleme'
    WHEN 10000071 THEN 'Stok Kaydı (Sayım)'
    ELSE 'Diğer (' + CAST(T0.TransType AS VARCHAR) + ')'
END";

        [HttpGet("GetStokKayitListesi")]
        public JsonResult GetStokKayitListesi(string search = "", string warehouse = "", int? transType = null, string yon = "", string dateFrom = "", string dateTo = "", int page = 1, int pageSize = 50)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetStokKayitBelgeTipleri")]
        public JsonResult GetStokKayitBelgeTipleri()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        private static IActionResult ExcelSablonUret(string sayfaAdi, string dosyaAdi, string notMetni)
 {return default;
}

        [HttpGet("MalGirisiSablon")]
        public IActionResult MalGirisiSablon()  {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("MalCikisiSablon")]
        public IActionResult MalCikisiSablon()  {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}






        [HttpPost("MalGirisiExcelAktar")]
        public JsonResult MalGirisiExcelAktar(IFormFile dosya)
 {return Json(new { success = true, data = new object[0], hatalar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatalar", i2)).ToList(), message = "0 satır okundu." });
}

        [HttpGet("StokKayitListesiExcel")]
        public IActionResult StokKayitListesiExcel(string search = "", string warehouse = "", int? transType = null, string yon = "", string dateFrom = "", string dateTo = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpGet("EtiketYazdir")]
        public IActionResult EtiketYazdir()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("EtiketYazdir");
}


        [HttpGet("GetEtiketStokListesi")]
        public JsonResult GetEtiketStokListesi()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetEtiketDetay")]
        public JsonResult GetEtiketDetay(string itemCode, string batchNum)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}





        private bool TamYetkiliMi()  {return default;
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

        public class StokSatiriIstek
        {
            public string ItemCode { get; set; }
            public double Quantity { get; set; }
            public string WarehouseCode { get; set; }
            public double UnitPrice { get; set; }
            public bool ManBtchNum { get; set; }
            public string BatchNumber { get; set; }
        }

        public class MalGirisCikisIstek
        {
            public DateTime DocDate { get; set; }
            public DateTime TaxDate { get; set; }
            public string Ref2 { get; set; }
            public string Comments { get; set; }
            public int Series { get; set; }
            public List<StokSatiriIstek> Satirlar { get; set; }
        }

        public class DepoNakliIstek
        {
            public DateTime DocDate { get; set; }
            public DateTime TaxDate { get; set; }
            public string Ref2 { get; set; }
            public string Comments { get; set; }
            public string JrnlMemo { get; set; }
            public int Series { get; set; }
            public string FromWarehouse { get; set; }
            public string ToWarehouse { get; set; }
            public List<StokSatiriIstek> Satirlar { get; set; }
        }

        private static List<Dictionary<string, object>> SlBelgeSatirlari(List<StokSatiriIstek> satirlar, bool cikis)
 {return default;
}

        [HttpPost("AddMalGirisi")]
        public async Task<JsonResult> AddMalGirisi([FromBody] MalGirisCikisIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<string>("docNum", 0) });
}

        [HttpPost("AddMalCikisi")]
        public async Task<JsonResult> AddMalCikisi([FromBody] MalGirisCikisIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<string>("docNum", 0) });
}

        [HttpPost("AddDepoNakli")]
        public async Task<JsonResult> AddDepoNakli([FromBody] DepoNakliIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<string>("docNum", 0) });
}
























        private string SayimKapsamFiltresi(string alias)  {return default;
}

        [HttpGet("StokSayim")]
        public IActionResult StokSayim()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("StokSayim");
}

        [HttpGet("GetSayimBelgeleri")]
        public JsonResult GetSayimBelgeleri(string durum = "O", string depo = "", string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSayimDetay")]
        public JsonResult GetSayimDetay(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSayimKalemler")]
        public JsonResult GetSayimKalemler(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSayimKalemPartiler")]
        public JsonResult GetSayimKalemPartiler(string itemCode, string whsCode = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSayimDepolar")]
        public JsonResult GetSayimDepolar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class SayimPartiIstek { public string PartiNo { get; set; } public double Miktar { get; set; } }
        public class SayimSatirIstek { public string ItemCode { get; set; } public string WhsCode { get; set; } public double Miktar { get; set; } public bool Partili { get; set; } public List<SayimPartiIstek> Partiler { get; set; } }
        public class YeniSayimIstek { public string WhsCode { get; set; } public string Remarks { get; set; } public List<SayimSatirIstek> Satirlar { get; set; } }
        public class SayimSatirEkleIstek { public int DocEntry { get; set; } public bool Degistir { get; set; } public List<SayimSatirIstek> Satirlar { get; set; } }
        public class SayimKayitIstek { public int DocEntry { get; set; } public double VarsayilanFiyat { get; set; } public string Remarks { get; set; } }
        public class SayimEksikCikisIstek { public string Bas { get; set; } public string Bit { get; set; } public string Depo { get; set; } public List<string> Kalemler { get; set; } public string Remarks { get; set; } }

        private static Dictionary<string, object> SayimSatirGovdesi(SayimSatirIstek s)
 {return default;
}

        [HttpPost("YeniSayimOlustur")]
        public async Task<JsonResult> YeniSayimOlustur([FromBody] YeniSayimIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = new { docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("docNum", 0) } });
}

        [HttpPost("SayimSatirEkle")]
        public async Task<JsonResult> SayimSatirEkle([FromBody] SayimSatirEkleIstek model)
 {return Json(new { success = true, message = "Sayım kaydedildi." });
}

        [HttpPost("SayimSatirSil")]
        public async Task<JsonResult> SayimSatirSil(int docEntry, int lineNum)
 {return Json(new { success = true, message = "Satır silindi." });
}

        [HttpPost("SayimKapat")]
        public async Task<JsonResult> SayimKapat(int docEntry)
 {return Json(new { success = true, message = "Sayım belgesi kapatıldı." });
}

        [HttpPost("SayimStokKaydinaCevir")]
        public async Task<JsonResult> SayimStokKaydinaCevir([FromBody] SayimKayitIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}



        [HttpGet("SayimEksik")]
        public IActionResult SayimEksik()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("SayimEksik");
}

        [HttpGet("GetSayimEksik")]
        public JsonResult GetSayimEksik(string bas, string bit, string depo)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("SayimEksikCikis")]
        public async Task<JsonResult> SayimEksikCikis([FromBody] SayimEksikCikisIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}
    }
}
