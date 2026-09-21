// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
    public class UrasUretimUretimController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UrasUretimUretimController> _logger;

        private bool TamYetkiliMi()  {return default;
}
        private bool GercekAd()  {return default;
}

        private JsonResult Veri(Func<object> f)
 {return default;
}

        private string KullaniciAdi()  {return default;
}





        private async Task<HttpClient> SlOturumAc()
 {return default;
}

        private static async Task SlOturumKapat(HttpClient client)
 {}

        private static string SlHataMesaji(string govde)
 {return default;
}

        private async Task<string> SlIstek(HttpClient client, HttpMethod metot, string yol, object govde = null)
 {return default;
}


        private async Task UdoEkle(HttpClient client, string udo, string code, string name, Dictionary<string, object> alanlar)
 {}

        private async Task UdoGuncelle(HttpClient client, string udo, string code, string name, Dictionary<string, object> alanlar)
 {}

        private static string Kisalt(string s, int n)  {return default;
}
        private static string Simdi()  {return default;
}





        [HttpGet("SiloDolum")]
        public IActionResult SiloDolum()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("SiloDolum");
}

        [HttpGet("GetSilolar")]
        public JsonResult GetSilolar(bool sadeceAktif = false)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSiloOzet")]
        public JsonResult GetSiloOzet()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSiloHareketler")]
        public JsonResult GetSiloHareketler(string siloCode = "", string tip = "", string search = "", string basTarih = "", string bitTarih = "", int page = 1, int pageSize = 50)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetLotBilgi")]
        public JsonResult GetLotBilgi(string itemCode, string parti, string whsCode = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("SiloDolumKaydet")]
        public async Task<JsonResult> SiloDolumKaydet([FromBody] SiloDolumIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<double>("data", 0) });
}

        [HttpPost("SiloBosalt")]
        public async Task<JsonResult> SiloBosalt([FromBody] SiloDolumIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SiloDuzelt")]
        public async Task<JsonResult> SiloDuzelt([FromBody] SiloDolumIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SiloTanimKaydet")]
        public async Task<JsonResult> SiloTanimKaydet([FromBody] SiloTanimIstek m)
 {return Json(new { success = true, message = "Silo tanımı kaydedildi.", data = global::WebApplication3.OrnekDoldurucu.Deger<string>("data", 0) });
}

        [HttpGet("GetWarehousesUretim")]
        public JsonResult GetWarehousesUretim()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}





        [HttpGet("KalemAnaVerileri")]
        public IActionResult KalemAnaVerileri()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("KalemAnaVerileri");
}

        [HttpGet("GetKalemler")]
        public JsonResult GetKalemler(string search = "", string frozenfor = "", int page = 1, int pageSize = 50, int? grup = null, string parti = "", string stok = "", string birim = "")
 {return new JsonResult(WebApplication3.Data.UretimOrnek.Kalemler(search, page, pageSize), WebApplication3.Data.UretimOrnek.PascalCase);
}

        [HttpGet("GetKalemStats")]
        public JsonResult GetKalemStats()
 {return new JsonResult(WebApplication3.Data.UretimOrnek.KalemStats(), WebApplication3.Data.UretimOrnek.PascalCase);
}

        [HttpGet("GetItemGroups")]
        public JsonResult GetItemGroups()
 {return new JsonResult(WebApplication3.Data.UretimOrnek.ItemGroups(), WebApplication3.Data.UretimOrnek.PascalCase);
}

        [HttpGet("GetUomList")]
        public JsonResult GetUomList()
 {return new JsonResult(WebApplication3.Data.UretimOrnek.UomList(), WebApplication3.Data.UretimOrnek.PascalCase);
}

        private static object SlEvetHayir(bool v)  {return default;
}

        [HttpPost("AddKalem")]
        public async Task<JsonResult> AddKalem([FromBody] KalemFormIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("UpdateKalem")]
        public async Task<JsonResult> UpdateKalem([FromBody] KalemFormIstek model)
 {return Json(new { success = true, message = "Kalem başarıyla güncellendi." });
}

        [HttpPost("UpdateKalemDurum")]
        public async Task<JsonResult> UpdateKalemDurum(string itemCode, bool etkin)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("DeleteKalem")]
        public async Task<JsonResult> DeleteKalem(string itemCode)
 {return Json(new { success = true, message = "Kalem başarıyla kaldırıldı." });
}





        [HttpGet("SeriPartiTanim")]
        public IActionResult SeriPartiTanim()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("SeriPartiTanim");
}

        [HttpGet("GetSeriPartiTanimlar")]
        public JsonResult GetSeriPartiTanimlar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("UpdateSeriPartiTanim")]
        public async Task<JsonResult> UpdateSeriPartiTanim([FromBody] SeriPartiTanimIstek model)
 {return Json(new { success = true, message = "Tanım güncellendi." });
}

        [HttpGet("GetLotSayaclar")]
        public JsonResult GetLotSayaclar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetLotResetLog")]
        public JsonResult GetLotResetLog()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("ResetLotSayac")]
        public async Task<JsonResult> ResetLotSayac([FromBody] LotResetIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SetLotSayac")]
        public async Task<JsonResult> SetLotSayac([FromBody] LotResetIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SetOworLotNo")]
        public async Task<JsonResult> SetOworLotNo([FromBody] OworLotDuzeltIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<string>("data", 0) });
}

































        [HttpGet("UretimSiparisi")]
        public IActionResult UretimSiparisi()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("UretimSiparisi");
}

        [HttpGet("GetOITT")]
        public JsonResult GetOITT()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetITT1Uretim")]
        public JsonResult GetITT1Uretim(string code)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetBatchNumberUretim")]
        public JsonResult GetBatchNumberUretim(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetNewlineWOR1")]
        public JsonResult GetNewlineWOR1()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetORSC")]
        public JsonResult GetORSC()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetORDRUretim")]
        public JsonResult GetORDRUretim()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetOWOR")]
        public JsonResult GetOWOR(string durum = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetWOR1")]
        public JsonResult GetWOR1(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetBelgeLinesUretim")]
        public JsonResult GetBelgeLinesUretim(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetQCRangesUretim")]
        public JsonResult GetQCRangesUretim()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetQCRangeByItemCodeUretim")]
        public JsonResult GetQCRangeByItemCodeUretim(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetQCFormsByWorkOrderUretim")]
        public JsonResult GetQCFormsByWorkOrderUretim(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private static string SlUretimTipi(string type)  {return default;
}

        [HttpPost("AddProductionOrder")]
        public async Task<JsonResult> AddProductionOrder([FromBody] AddOworIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<int>("data", 0) });
}

        [HttpPost("UpdateProductionOrder")]
        public async Task<JsonResult> UpdateProductionOrder([FromBody] UpdateOworIstek m)
 {return Json(new { success = true, message = "Üretim siparişi başarıyla güncellendi." });
}

        [HttpPost("CancelProductionOrder")]
        public async Task<JsonResult> CancelProductionOrder(int docEntry)
 {return Json(new { success = true, message = "Üretim siparişi başarıyla iptal edildi." });
}

        [HttpPost("AddInventoryGenEntryUretim")]
        public async Task<JsonResult> AddInventoryGenEntryUretim([FromBody] InventoryEntryIstek m)
 {return Json(new { success = true, message = "İşlem başarılı" });
}

        [HttpPost("AddInventoryGenExitUretim")]
        public async Task<JsonResult> AddInventoryGenExitUretim([FromBody] InventoryExitIstek m)
 {return Json(new { success = true, message = "İşlem başarılı." });
}






        [HttpPost("SaveQCRange")]
        public JsonResult SaveQCRange([FromBody] QCRangeIstek model)
 {return Json(new { success = true, message = "Aranan değerler kaydedildi.", data = global::WebApplication3.OrnekDoldurucu.Deger<string>("data", 0) });
}




































        [HttpGet("SahaUretim")]
        public IActionResult SahaUretim()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("SahaUretim");
}

        [HttpGet("GetUraskimyaSaha")]
        public JsonResult GetUraskimyaSaha()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetUraskimyaSahaBit")]
        public JsonResult GetUraskimyaSahaBit()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetBilesenPartiOnizleme")]
        public JsonResult GetBilesenPartiOnizleme(int docEntry, double? qty = null)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetBilesenPartiIzi")]
        public JsonResult GetBilesenPartiIzi(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetYariMamulPartiler")]
        public JsonResult GetYariMamulPartiler(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private static string ResolveMiddleUretim(string selType, string value, string itemCode)
 {return default;
}

        private static string BuildLotNoUretim(string prefix, string middle, bool isDate, int digit, int no)
 {return default;
}

        [HttpPost("UpdateUrasKimyaSahaBas")]
        public async Task<JsonResult> UpdateUrasKimyaSahaBas([FromBody] UpdateOworIstek m)
 {return Json(new { success = true, message = "Üretim siparişi ve izlenebilirlik verileri başarıyla güncellendi." });
}

        public class UretimBitIstek
        {
            public int DocEntry { get; set; }
            public string SecilenLot { get; set; }
            public string U_BE1_UVTDURUM { get; set; }
            public double? U_BE1_URETMIKTAR { get; set; }
        }

        [HttpPost("UpdateUrasKimyaSahaBit")]
        public async Task<JsonResult> UpdateUrasKimyaSahaBit([FromBody] UretimBitIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<string>("data", 0) });
}

        public class BilesenLotSecimIstek
        {
            public int LineNum { get; set; } = -1;
            public string ItemCode { get; set; }
            public string Parti { get; set; }
            public string Kaynak { get; set; } // M=Manuel, Y=Yarı Mamul lotu
        }

        public class BilesenCikisIstek
        {
            public int DocEntry { get; set; }
            public double? Qty { get; set; }
            public string SecilenLot { get; set; }
            public List<BilesenLotSecimIstek> Secimler { get; set; }
        }






        [HttpPost("AddInventoryGenExitOtomatik")]
        public async Task<JsonResult> AddInventoryGenExitOtomatik([FromBody] BilesenCikisIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private static void istendenKarsilanaEkle(ref double toplam, double al)  {}
        private static string istendenKarsilanaMesaj(string itemCode, string istenenParti, double karsilanan, double fifo)  {return default;
}



        [HttpGet("UrunDonusumu")]
        public IActionResult UrunDonusumu()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("UrunDonusumu");
}

        [HttpGet("GetUrunDonusumuList")]
        public JsonResult GetUrunDonusumuList()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpPost("AddUrunDonusumu")]
        public async Task<JsonResult> AddUrunDonusumu([FromBody] AddOworIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}



        [HttpGet("UrunAyristirma")]
        public IActionResult UrunAyristirma()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("UrunAyristirma");
}

        [HttpGet("GetKapaliUretimSiparisleri")]
        public JsonResult GetKapaliUretimSiparisleri(bool girisFisliDahil = false, bool ymHaric = false)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetWOR1ForAyristirma")]
        public JsonResult GetWOR1ForAyristirma(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetStokDemonteList")]
        public JsonResult GetStokDemonteList()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetOITTForAyristirma")]
        public JsonResult GetOITTForAyristirma(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetUrunAyristirmaList")]
        public JsonResult GetUrunAyristirmaList()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}


        [HttpPost("AddUrunAyristirma")]
        public async Task<JsonResult> AddUrunAyristirma([FromBody] KucukDemontajIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<int>("data", 0) });
}


        [HttpPost("AddUrunAyristirmaStok")]
        public async Task<JsonResult> AddUrunAyristirmaStok([FromBody] StokAyristirmaIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}



        [HttpGet("Numune")]
        public IActionResult Numune()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Numune");
}

        [HttpGet("GetYariMamulLotlari")]
        public JsonResult GetYariMamulLotlari()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetNumuneList")]
        public JsonResult GetNumuneList()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}





        [HttpPost("AddNumune")]
        public async Task<JsonResult> AddNumune([FromBody] NumuneIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<int>("data", 0) });
}
    }




    public class SiloDolumIstek
    {
        public string SiloCode { get; set; }
        public string ItemCode { get; set; }
        public string Parti { get; set; }
        public double Miktar { get; set; }
        public string Aciklama { get; set; }
    }

    public class SiloTanimIstek
    {
        public string Code { get; set; }
        public string Ad { get; set; }
        public double Kapasite { get; set; }
        public string WhsCode { get; set; }
        public bool Aktif { get; set; } = true;
        public int Sira { get; set; }
        public string Aciklama { get; set; }
    }

    public class KalemFormIstek
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ForeignName { get; set; }
        public int ItmsGrpCod { get; set; }
        public string UomCode { get; set; }
        public string SecondUomCode { get; set; }
        public string ManBtchNum { get; set; }
        public string Frozenfor { get; set; }
    }

    public class SeriPartiTanimIstek
    {
        public int DocEntry { get; set; }
        public string SpType { get; set; }
        public string Prefix { get; set; }
        public string SelType { get; set; }
        public string Value { get; set; }
        public int Digit { get; set; }
        public int MaxNo { get; set; }
        public string Active { get; set; }
    }

    public class LotResetIstek
    {
        public string ItemCode { get; set; }
        public string Aciklama { get; set; }
        public int? YeniNo { get; set; }
    }

    public class OworLotDuzeltIstek
    {
        public int? DocEntry { get; set; }
        public string YeniLot { get; set; }
        public string Aciklama { get; set; }
    }



    public class AddWor1Satir
    {
        public string Code { get; set; }
        public double Quantity { get; set; }
        public double PlannedQty { get; set; }
        public int Type { get; set; } // 290=Kaynak, 4=Kalem
    }

    public class AddOworIstek
    {
        public string OCode { get; set; }
        public double PlannedQty { get; set; }
        public string WhsCode { get; set; }
        public DateTime PostDate { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Type { get; set; } // S/P/D
        public string LinkToObj { get; set; } // 2=Satış Siparişi, 3=Üretim Siparişi
        public int? OrderNo { get; set; }
        public string Comments { get; set; }
        public string U_BE1_UVTONAY { get; set; }
        public double? U_BE1_KAP1KG { get; set; }
        public double? U_BE1_KAP5KG { get; set; }
        public double? U_BE1_KAP10KG { get; set; }
        public double? U_BE1_KAP20KG { get; set; }
        public double? U_BE1_KAP30KG { get; set; }
        public double? U_BE1_KAP40KG { get; set; }
        public double? U_BE1_KAP60KG { get; set; }
        public int? U_BE1_ONCELIK { get; set; }
        public List<AddWor1Satir> AddWOR1 { get; set; }
    }

    public class UpdateWor1Satir
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public double BaseQty { get; set; }
        public double PlannedQty { get; set; }
        public int ItemType { get; set; }
    }

    public class UpdateOworIstek
    {
        public int DocEntry { get; set; }
        public string OCode { get; set; }
        public double PlannedQty { get; set; }
        public string WhsCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime DueDate { get; set; }
        public string Type { get; set; }
        public string LinkToObj { get; set; }
        public int? OrderNo { get; set; }
        public string Comments { get; set; }
        public string U_BE1_UVTONAY { get; set; }
        public double? U_BE1_KAP1KG { get; set; }
        public double? U_BE1_KAP5KG { get; set; }
        public double? U_BE1_KAP10KG { get; set; }
        public double? U_BE1_KAP20KG { get; set; }
        public double? U_BE1_KAP30KG { get; set; }
        public double? U_BE1_KAP40KG { get; set; }
        public double? U_BE1_KAP60KG { get; set; }
        public int? U_BE1_ONCELIK { get; set; }
        public List<UpdateWor1Satir> UpdateWOR1 { get; set; }
    }

    public class InventoryEntrySatir
    {
        public string ItemCode { get; set; }
        public double Quantity { get; set; }
        public string Warehouse { get; set; }
        public int DocEntry { get; set; }
        public string BatchNumber { get; set; }
        public double FireMiktar { get; set; }
    }

    public class InventoryEntryIstek
    {
        public DateTime PostDate { get; set; }
        public List<InventoryEntrySatir> Items { get; set; }
    }

    public class InventoryExitBatchSatir
    {
        public string DistNumber { get; set; }
        public double PlannedQty { get; set; }
    }

    public class InventoryExitSatir
    {
        public int DocEntry { get; set; }
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public double PlannedQty { get; set; }
        public string Warehouse { get; set; }
        public List<InventoryExitBatchSatir> BatchNumbers { get; set; }
    }

    public class InventoryExitIstek
    {
        public DateTime PostDate { get; set; }
        public List<InventoryExitSatir> Items { get; set; }
    }


    public class QCRangeIstek
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string ItemCode { get; set; }
        public double? PhMin { get; set; }
        public double? PhMax { get; set; }
        public double? Visc1Min { get; set; }
        public double? Visc1Max { get; set; }
        public string Spindle1 { get; set; }
        public double? RPM1 { get; set; }
        public double? Visc2Min { get; set; }
        public double? Visc2Max { get; set; }
        public string Spindle2 { get; set; }
        public double? RPM2 { get; set; }
        public double? DensityMin { get; set; }
        public double? DensityMax { get; set; }
        public double? SolidMin { get; set; }
        public double? SolidMax { get; set; }
    }


    public class KucukDemontajIstek
    {
        public int DocEntry { get; set; }
        public double Miktar { get; set; }
    }

    public class StokAyristirmaIstek
    {
        public string ItemCode { get; set; }
        public string WhsCode { get; set; }
        public string BatchNum { get; set; }
        public double Miktar { get; set; }
    }

    public class NumuneIstek
    {
        public int DocEntry { get; set; }
        public double NumuneQty { get; set; }
        public string NumuneItemCode { get; set; }

        public string KaynakTip { get; set; }
        public string KaynakItemCode { get; set; }
        public string KaynakLot { get; set; }
        public string KaynakWhs { get; set; }
    }
}
