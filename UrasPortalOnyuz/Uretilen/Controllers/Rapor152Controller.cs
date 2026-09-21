// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]














    [Route("[controller]")]
    public class Rapor152Controller : Controller
    {

        private string UrasDbKey {get {return default;
}
}        private string SelviDbKey {get {return default;
}
}        private string SelviCariUrasta {get {return default;
}
}        private string UrasCariSelvide {get {return default;
}
}        private string GrupDepo {get {return default;
}
}
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor152Controller> _logger;
        private readonly IWebHostEnvironment _hostingEnvironment;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya", DbName = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS", DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS", DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS", DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S", DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S", DbName = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS", DbName = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
            new DatabaseConfig { Key = "DefaultConnection37", Display = "RGB_TEKSTIL", DbName = "RGB_TEKSTIL" },
        };


        private string GetSelectedDatabase()  {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        [HttpGet("Index")]
        public IActionResult Index()
 {ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.DatabaseConfig>(12);
ViewBag.CurrentDbDisplay = "";
ViewBag.Customers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.CustomerModel>(12);
ViewBag.Items = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.ItemModel>(12);
ViewBag.SalesEmployees = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.SalesEmployeeModel>(12);
ViewBag.VatGroups = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.VatGroupModel>(12);
ViewBag.Currencies = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.MuafCodes = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.MuafCodeModel>(12);
ViewBag.Countries = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.CountryModel>(12);
ViewBag.States = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.StateModel>(12);
ViewBag.Warehouses = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.WarehouseModel>(12);
ViewBag.Drivers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.DriverModel>(12);
ViewBag.SelviCari = "";
ViewBag.SelviSirketAdi = "";
ViewBag.NakliyeFirmalari = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.NakliyeFirmaModel>(12);
ViewBag.SelviCustomers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.CustomerModel>(12);
ViewBag.SelviDrivers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor152Controller.DriverModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}







        [HttpGet("GetItemBatches")]
        public IActionResult GetItemBatches(string itemCode, string whsCode = null)
 {return Json(new { partili = global::WebApplication3.OrnekDoldurucu.Deger<bool>("partili", 0), batches = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "BatchNum", "Quantity", "ExpDate", "MnfDate" }) });
}


        [HttpGet("GetSelviItemInfo")]
        public IActionResult GetSelviItemInfo(string urasItemCode, string selviCardCode)
 {return Json(new { success = true, selviItemCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("selviItemCode", 0), selviItemName = global::WebApplication3.OrnekDoldurucu.Deger<string>("selviItemName", 0), price = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("price", 0), currency = global::WebApplication3.OrnekDoldurucu.Deger<string>("currency", 0), partili = global::WebApplication3.OrnekDoldurucu.Deger<bool>("partili", 0) });
}

        [HttpGet("GetSelviCustomerAddresses")]
        public IActionResult GetSelviCustomerAddresses(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor152Controller.BPAddressModel>(12));
}

        [HttpPost("SaveSelviBPAddress")]
        public async Task<IActionResult> SaveSelviBPAddress([FromBody] BPAddressModel request, [FromQuery] string cardCode)
 {return Json(new { success = true, message = "Adres başarıyla cariye eklendi." });
}

        [HttpPost("AddSelviDriver")]
        public IActionResult AddSelviDriver([FromBody] DriverModel request)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor152Controller.DriverModel>() });
}

        [HttpGet("SearchItems")]
        public IActionResult SearchItems(string q)
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName", "StockDetails" }) });
}
        [HttpGet("GetCustomerInfo")]
        public IActionResult GetCustomerInfo(string cardCode)
 {return Json(new { currency = global::WebApplication3.OrnekDoldurucu.Deger<string>("currency", 0), slpCode = global::WebApplication3.OrnekDoldurucu.Deger<int>("slpCode", 0), extraDays = global::WebApplication3.OrnekDoldurucu.Deger<int>("extraDays", 0), extraMonth = global::WebApplication3.OrnekDoldurucu.Deger<int>("extraMonth", 0) });
}

        [HttpGet("GetCustomerAddresses")]
        public IActionResult GetCustomerAddresses(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor152Controller.BPAddressModel>(12));
}

        [HttpGet("GetAllExchangeRates")]
        public IActionResult GetAllExchangeRates(string date)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, decimal>>());
}





        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, bool> _kolonBellegi
            = new System.Collections.Concurrent.ConcurrentDictionary<string, bool>();

        [HttpGet("GetItemPrice")]
        public IActionResult GetItemPrice(string cardCode, string itemCode)
 {return Json(new { price = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("price", 0), currency = global::WebApplication3.OrnekDoldurucu.Deger<string>("currency", 0), dfltWH = global::WebApplication3.OrnekDoldurucu.Deger<string>("dfltWH", 0), secUnitName = global::WebApplication3.OrnekDoldurucu.Deger<string>("secUnitName", 0), secUnitMultiplier = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("secUnitMultiplier", 0), freeTxt = global::WebApplication3.OrnekDoldurucu.Deger<string>("freeTxt", 0) });
}

        [HttpGet("GetItemStock")]
        public IActionResult GetItemStock(string itemCode, string whsCode)
 {return Json(new { onHand = 0 });
}

        [HttpGet("GetOrderNavigation")]
        public IActionResult GetOrderNavigation(int currentDocEntry, string direction)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor152Controller.OrderDetailViewModel>() });
}

        [HttpPost("FindOrder")]
        public IActionResult FindOrder([FromBody] OrderSearchModel search)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor152Controller.OrderSearchResultModel>(12) });
}

        [HttpGet("GetOrderDetails")]
        public IActionResult GetOrderDetails(int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor152Controller.OrderDetailViewModel>() });
}
        private string SayiyiYaziyaCevir(long sayi)
 {return default;
}

        private string TutarYaziyla(decimal tutar, string paraBirimi)
 {return default;
}

        [HttpGet("PrintOrder")]
        public IActionResult PrintOrder(int docEntry)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpPost("AddDriver")]
        public IActionResult AddDriver([FromBody] DriverModel request)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor152Controller.DriverModel>() });
}

        [HttpPost("CheckStockForOrder")]
        public IActionResult CheckStockForOrder([FromBody] SalesOrderCreationModel request)
 {return Json(new { success = true, hasMissingStock = global::WebApplication3.OrnekDoldurucu.Deger<bool>("hasMissingStock", 0), missingItems = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemName" }) });
}






        private static string AciklamaBirlestir(string nakliyeci, string kullanici, string robot, int sinir = 254)
 {return default;
}

        [HttpPost("CreateOrder")]
        public async Task<IActionResult> CreateOrder([FromBody] SalesOrderCreationModel request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), selviDocs = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("selviDocs", i2)).ToList(), docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), deliveryDocNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("deliveryDocNum", 0), invoiceDocNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("invoiceDocNum", 0), akis = global::WebApplication3.OrnekDoldurucu.Deger<string>("akis", 0), autoPrint = global::WebApplication3.OrnekDoldurucu.Deger<bool>("autoPrint", 0) });
}





        private class SelviSonuc
        {
            public bool Basarili;
            public string Mesaj;
            public List<string> Olusanlar = new List<string>();
        }

        private static string SlHata(string json)
 {return default;
}

        private async Task<SelviSonuc> SelviZinciriniOlusturAsync(SalesOrderCreationModel request, string teslimTarihi, string vadeTarihi)
 {return default;
}




        [HttpPost("CancelOrder")]
        public async Task<IActionResult> CancelOrder(int docEntry)
 {return Json(new { success = true, message = "Sipariş ve bağlı tüm belgeler (İrsaliye / Fatura) başarıyla iptal edildi." });
}

        private string GetSlErrorMessage(string jsonStr)
 {return default;
}

        [HttpPost("SaveBPAddress")]
        public async Task<IActionResult> SaveBPAddress([FromBody] BPAddressModel request, [FromQuery] string cardCode)
 {return Json(new { success = true, message = "Adres başarıyla cariye eklendi." });
}

        private List<CustomerModel> GetCustomers(string dbKey)
 {return default;
}

        private List<ItemModel> GetItems(string dbKey)
 {return default;
}

        private List<SalesEmployeeModel> GetSalesEmployees(string dbKey)
 {return default;
}

        private List<VatGroupModel> GetVatGroups(string dbKey)
 {return default;
}

        private List<string> GetCurrencies(string dbKey)
 {return default;
}





        private List<NakliyeFirmaModel> GetNakliyeFirmalari(string dbKey)
 {return default;
}

        [HttpGet("NakliyeFirmalari")]
        public IActionResult NakliyeFirmalari()
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor152Controller.NakliyeFirmaModel>(12));
}








        [HttpPost("NakliyeFirmaEkle")]
        public async Task<IActionResult> NakliyeFirmaEkle([FromBody] NakliyeFirmaModel request)
 {return Json(new { success = true, message = "Nakliyeci firma eklendi.", firma = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor152Controller.NakliyeFirmaModel>() });
}

        private List<MuafCodeModel> GetMuafCodes(string dbKey)
 {return default;
}

        private List<CountryModel> GetCountries(string dbKey)
 {return default;
}

        private List<StateModel> GetStates(string dbKey)
 {return default;
}

        private List<WarehouseModel> GetWarehouses(string dbKey)
 {return default;
}

        private List<DriverModel> GetDrivers(string dbKey)
 {return default;
}

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class CustomerModel { public string CardCode { get; set; } public string CardName { get; set; } }
        public class ItemModel { public string ItemCode { get; set; } public string ItemName { get; set; } public string StockDetails { get; set; } }
        public class SalesEmployeeModel { public int SlpCode { get; set; } public string SlpName { get; set; } }
        public class VatGroupModel { public string Code { get; set; } public string Name { get; set; } public decimal Rate { get; set; } }
        public class MuafCodeModel { public string Code { get; set; } public string Name { get; set; } public string Type { get; set; } }

        public class NakliyeFirmaModel
        {
            public string FirmaNo { get; set; }
            public string FirmaAdi { get; set; }
            public string FirmaVkn { get; set; }
        }
        public class CountryModel { public string Code { get; set; } public string Name { get; set; } }
        public class StateModel { public string Code { get; set; } public string Country { get; set; } public string Name { get; set; } }
        public class WarehouseModel { public string WhsCode { get; set; } public string WhsName { get; set; } }

        public class DriverModel
        {
            public string DocEntry { get; set; }
            public string Name { get; set; }
            public string Plate { get; set; }
            public string Title { get; set; }
            public string VKN { get; set; }
            public string KVKK { get; set; }
            public string FirmaNo { get; set; }
            public string Tel { get; set; }
        }

        public class BPAddressModel
        {
            public string AddressName { get; set; }
            public string AddressType { get; set; }
            public string Street { get; set; }
            public string StreetNo { get; set; }
            public string Block { get; set; }
            public string ZipCode { get; set; }
            public string City { get; set; }
            public string County { get; set; }
            public string State { get; set; }
            public string Country { get; set; }
        }

        public class OrderSearchModel
        {
            public int? DocNum { get; set; }
            public string CardCode { get; set; }
            public int? SlpCode { get; set; }
            public string DocDate { get; set; }
            public string DocDueDate { get; set; }
            public string TaxDate { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_SEND { get; set; }
            public string U_BE1_MUAFCODE { get; set; }
            public string DocCurrency { get; set; }
        }

        public class OrderSearchResultModel
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string DocDate { get; set; }
            public string DocCurrency { get; set; }
        }

        public class AddressExtensionModel
        {
            public string BillToStreet { get; set; }
            public string BillToStreetNo { get; set; }
            public string BillToBlock { get; set; }
            public string BillToZipCode { get; set; }
            public string BillToCity { get; set; }
            public string BillToCounty { get; set; }
            public string BillToState { get; set; }
            public string BillToCountry { get; set; }
            public string ShipToStreet { get; set; }
            public string ShipToStreetNo { get; set; }
            public string ShipToBlock { get; set; }
            public string ShipToZipCode { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToCounty { get; set; }
            public string ShipToState { get; set; }
            public string ShipToCountry { get; set; }
        }

        public class OrderDetailViewModel
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string LicTradNum { get; set; }
            public string DocDate { get; set; }
            public string DocDueDate { get; set; }
            public string DocStatus { get; set; }
            public string Canceled { get; set; }
            public string TaxDate { get; set; }
            public int SlpCode { get; set; }
            public string DocCurrency { get; set; }
            public decimal DocRate { get; set; }
            public string Comments { get; set; }
            public string DeliveryComments { get; set; }
            public string InvoiceComments { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_SEND { get; set; }
            public string U_BE1_MUAFCODE { get; set; }
            public string U_BE1_DESPATCHDESC { get; set; }
            public string U_BE1_NAKLIYETARIHI { get; set; }
            public string U_BE1_NAKLIYESAATI { get; set; }
            public string U_BE1_SOFORSECIMI { get; set; }
            public string U_BE1_SRCNAME { get; set; }
            public string U_BE1_SRCPLAKA { get; set; }
            public string U_BE1_SRCTITLE { get; set; }
            public string U_BE1_SRCVKN { get; set; }
            public string U_BE1_SRCKVKK { get; set; }
            public string U_BE1_FIRMANO { get; set; }
            public string U_BE1_FIRMAADI { get; set; }
            public string U_BE1_FIRMAVKN { get; set; }
            public string U_BE1_SRCTEL { get; set; }

            public string PayToCode { get; set; }
            public string ShipToCode { get; set; }

            public Dictionary<string, decimal> ExchangeRates { get; set; } = new Dictionary<string, decimal>();

            public AddressExtensionModel AddressExtension { get; set; }
            public List<SalesOrderLineModel> Lines { get; set; } = new List<SalesOrderLineModel>();
        }

        public class SalesOrderCreationModel
        {
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public DateTime DocDate { get; set; }
            public DateTime DocDueDate { get; set; }






            public DateTime? VadeTarihi { get; set; }

            public DateTime TaxDate { get; set; }
            public int SalesPersonCode { get; set; }
            public string DocCurrency { get; set; }
            public decimal DocRate { get; set; }

            public decimal EURRate { get; set; }
            public decimal USDRate { get; set; }

            public bool UpdateGlobalRates { get; set; }

            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_SEND { get; set; }
            public string U_BE1_MUAFCODE { get; set; }

            public string OrderComments { get; set; }
            public string OrderDescFlag { get; set; }
            public string DeliveryComments { get; set; }
            public string DeliveryDescFlag { get; set; }
            public string InvoiceComments { get; set; }
            public string InvoiceDescFlag { get; set; }

            public string U_BE1_NAKLIYETARIHI { get; set; }
            public string U_BE1_NAKLIYESAATI { get; set; }

            public string U_BE1_SOFORSECIMI { get; set; }
            public string U_BE1_SOFORADSOYAD { get; set; }
            public string U_BE1_ARACPLAKASI { get; set; }
            public string U_BE1_SOFORUNVANI { get; set; }
            public string U_BE1_SOFORKIMLIK { get; set; }
            public string U_BE1_SOFORTEL { get; set; }
            public string U_BE1_FRMNO { get; set; }
            public string U_BE1_FRMADI { get; set; }
            public string U_BE1_FRMVKN { get; set; }







            public string Akis { get; set; }

            public string PayToCode { get; set; }
            public string ShipToCode { get; set; }

            public AddressExtensionModel AddressExtension { get; set; }
            public List<SalesOrderLineModel> DocumentLines { get; set; }


            public string SelviCardCode { get; set; }
            public string SelviCardName { get; set; }
            public string SelviShipToCode { get; set; }
            public string SelviPayToCode { get; set; }
            public AddressExtensionModel SelviAddressExtension { get; set; }
            public string SelviSoforSecimi { get; set; }
            public string SelviSoforAdSoyad { get; set; }
            public string SelviAracPlakasi { get; set; }
            public string SelviSoforUnvani { get; set; }
            public string SelviSoforKimlik { get; set; }
            public string SelviSoforTel { get; set; }
            public int SelviSalesPersonCode { get; set; }
        }

        public class SalesOrderLineModel
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            public string Currency { get; set; }
            public string VatGroup { get; set; }
            public decimal VatRate { get; set; }
            public string WarehouseCode { get; set; }
            public string SecUnitName { get; set; }
            public decimal SecUnitMultiplier { get; set; }


            public string FreeTxt { get; set; }


            public string LotNo { get; set; }

            public string BatchNumber { get; set; }


            public double LineRate { get; set; }


            public double LineTotalLC { get; set; }
        }
    }
}