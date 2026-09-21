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

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor159Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor159Controller> _logger;

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
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index(int? baseEntry, int? baseType)
 {ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.DatabaseConfig>(12);
ViewBag.CurrentDbDisplay = "";
ViewBag.Customers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.CustomerModel>(12);
ViewBag.Items = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.ItemModel>(12);
ViewBag.SalesEmployees = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.SalesEmployeeModel>(12);
ViewBag.VatGroups = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.VatGroupModel>(12);
ViewBag.Currencies = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.MuafCodes = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.MuafCodeModel>(12);
ViewBag.Countries = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.CountryModel>(12);
ViewBag.States = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.StateModel>(12);
ViewBag.Warehouses = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.WarehouseModel>(12);
ViewBag.Drivers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.DriverModel>(12);
ViewBag.Employees = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor159Controller.EmployeeModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}






        private static readonly Dictionary<int, (string Hdr, string Ln, string Ad)> _bazTablolar = new Dictionary<int, (string, string, string)>
        {
            { 22, ("OPOR", "POR1", "Satınalma Siparişi") },
            { 20, ("OPDN", "PDN1", "Satınalma Mal Girişi") },
            { 18, ("OPCH", "PCH1", "Satınalma Faturası") },
            { 21, ("ORPD", "RPD1", "Satınalma İade İrsaliyesi") },
            { 19, ("ORPC", "RPC1", "Satınalma İade Faturası") }
        };


        private static readonly int[] _izinliBazTipleri = new int[] {  };


        [HttpGet("GetOpenBaseDocuments")]
        public IActionResult GetOpenBaseDocuments(string cardCode, int baseType)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "CardCode", "CardName", "DocCur", "DocTotal" }), baseTypeName = global::WebApplication3.OrnekDoldurucu.Deger<string>("baseTypeName", 0) });
}


        [HttpGet("GetBaseDocument")]
        public IActionResult GetBaseDocument(int baseEntry, int baseType)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor159Controller.PurchaseOrderDetailViewModel>(), baseTypeName = global::WebApplication3.OrnekDoldurucu.Deger<string>("baseTypeName", 0), baseType = global::WebApplication3.OrnekDoldurucu.Deger<int>("baseType", 0), baseEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("baseEntry", 0) });
}

        [HttpGet("GetCustomerInfo")]
        public IActionResult GetCustomerInfo(string cardCode)
 {return Json(new { currency = global::WebApplication3.OrnekDoldurucu.Deger<string>("currency", 0) });
}

        [HttpGet("GetCustomerAddresses")]
        public IActionResult GetCustomerAddresses(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor159Controller.BPAddressModel>(12));
}

        [HttpGet("GetAllExchangeRates")]
        public IActionResult GetAllExchangeRates(string date)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, decimal>>());
}

        [HttpGet("GetItemPrice")]
        public IActionResult GetItemPrice(string cardCode, string itemCode)
 {return Json(new { price = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("price", 0), currency = global::WebApplication3.OrnekDoldurucu.Deger<string>("currency", 0), dfltWH = global::WebApplication3.OrnekDoldurucu.Deger<string>("dfltWH", 0), secUnitName = global::WebApplication3.OrnekDoldurucu.Deger<string>("secUnitName", 0), secUnitMultiplier = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("secUnitMultiplier", 0) });
}

        [HttpGet("GetItemStock")]
        public IActionResult GetItemStock(string itemCode, string whsCode)
 {return Json(new { onHand = 0 });
}

        [HttpGet("GetPurchaseOrderNavigation")]
        public IActionResult GetPurchaseOrderNavigation(int currentDocEntry, string direction)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor159Controller.PurchaseOrderDetailViewModel>() });
}

        [HttpPost("FindPurchaseOrder")]
        public IActionResult FindPurchaseOrder([FromBody] PurchaseOrderSearchModel search)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor159Controller.PurchaseOrderSearchResultModel>(12) });
}

        [HttpGet("GetPurchaseOrderDetails")]
        public IActionResult GetPurchaseOrderDetails(int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor159Controller.PurchaseOrderDetailViewModel>() });
}

        [HttpPost("AddDriver")]
        public IActionResult AddDriver([FromBody] DriverModel request)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor159Controller.DriverModel>() });
}




        [HttpPost("CreatePurchaseOrder")]
        public async Task<IActionResult> CreatePurchaseOrder([FromBody] PurchaseOrderCreationModel request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("docNum", 0) });
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

        private List<EmployeeModel> GetEmployees(string dbKey)
 {return default;
}

        private List<DriverModel> GetDrivers(string dbKey)
 {return default;
}


        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class CustomerModel { public string CardCode { get; set; } public string CardName { get; set; } }
        public class ItemModel { public string ItemCode { get; set; } public string ItemName { get; set; } }
        public class SalesEmployeeModel { public int SlpCode { get; set; } public string SlpName { get; set; } }
        public class VatGroupModel { public string Code { get; set; } public string Name { get; set; } public decimal Rate { get; set; } }
        public class MuafCodeModel { public string Code { get; set; } public string Name { get; set; } public string Type { get; set; } }
        public class CountryModel { public string Code { get; set; } public string Name { get; set; } }
        public class StateModel { public string Code { get; set; } public string Country { get; set; } public string Name { get; set; } }
        public class WarehouseModel { public string WhsCode { get; set; } public string WhsName { get; set; } }

        public class EmployeeModel { public int EmpId { get; set; } public string AdSoyad { get; set; } }

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

        public class PurchaseOrderSearchModel
        {
            public int? DocNum { get; set; }
            public string CardCode { get; set; }
            public int? SlpCode { get; set; }
            public string NumAtCard { get; set; }
            public string DocDate { get; set; }
            public string DocDueDate { get; set; }
            public string TaxDate { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_SEND { get; set; }
            public string U_BE1_MUAFCODE { get; set; }
            public string DocCurrency { get; set; }
        }

        public class PurchaseOrderSearchResultModel
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

        public class PurchaseOrderDetailViewModel
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public string CardCode { get; set; }
            public string DocDate { get; set; }
            public string DocDueDate { get; set; }
            public string TaxDate { get; set; }
            public int SlpCode { get; set; }
            public string DocCurrency { get; set; }
            public decimal DocRate { get; set; }
            public string Comments { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_SEND { get; set; }
            public string U_BE1_MUAFCODE { get; set; }
            public string PayToCode { get; set; }

            public string NumAtCard { get; set; }
            public string ShipToCode { get; set; }

            public string U_BE1_SOFORSECIMI { get; set; }
            public string U_BE1_SOFORADSOYAD { get; set; }
            public string U_BE1_ARACPLAKASI { get; set; }
            public string U_BE1_SOFORUNVANI { get; set; }
            public string U_BE1_SOFORKIMLIK { get; set; }
            public string U_BE1_SOFORTEL { get; set; }
            public string U_BE1_FRMNO { get; set; }

            public AddressExtensionModel AddressExtension { get; set; }
            public List<PurchaseOrderLineModel> Lines { get; set; } = new List<PurchaseOrderLineModel>();
        }

        public class PurchaseOrderCreationModel
        {
            public string CardCode { get; set; }

            public string Requester { get; set; }
            public DateTime DocDate { get; set; }
            public DateTime DocDueDate { get; set; }
            public DateTime TaxDate { get; set; }
            public int SalesPersonCode { get; set; }
            public string Comments { get; set; }
            public string DocCurrency { get; set; }
            public decimal DocRate { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_SEND { get; set; }
            public string U_BE1_MUAFCODE { get; set; }

            public string U_BE1_SOFORSECIMI { get; set; }
            public string U_BE1_SOFORADSOYAD { get; set; }
            public string U_BE1_ARACPLAKASI { get; set; }
            public string U_BE1_SOFORUNVANI { get; set; }
            public string U_BE1_SOFORKIMLIK { get; set; }
            public string U_BE1_SOFORTEL { get; set; }
            public string U_BE1_FRMNO { get; set; }

            public string PayToCode { get; set; }

            public string NumAtCard { get; set; }
            public string ShipToCode { get; set; }

            public AddressExtensionModel AddressExtension { get; set; }
            public List<PurchaseOrderLineModel> DocumentLines { get; set; }
        }

        public class PurchaseOrderLineModel
        {
            public string ItemCode { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            public string Currency { get; set; }
            public string VatGroup { get; set; }
            public string WarehouseCode { get; set; }
            public string SecUnitName { get; set; }
            public decimal SecUnitMultiplier { get; set; }


            public int? BaseType { get; set; }
            public int? BaseEntry { get; set; }
            public int? BaseLine { get; set; }
        }
    }
}