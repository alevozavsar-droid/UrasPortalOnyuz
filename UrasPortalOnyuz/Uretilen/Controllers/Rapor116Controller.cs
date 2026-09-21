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
    public class Rapor116Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor116Controller> _logger;

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
 {ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.DatabaseConfig>(12);
ViewBag.CurrentDbDisplay = "";
ViewBag.Customers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.CustomerModel>(12);
ViewBag.Items = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.ItemModel>(12);
ViewBag.SalesEmployees = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.SalesEmployeeModel>(12);
ViewBag.VatGroups = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.VatGroupModel>(12);
ViewBag.Currencies = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.MuafCodes = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.MuafCodeModel>(12);
ViewBag.Countries = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.CountryModel>(12);
ViewBag.States = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.StateModel>(12);
ViewBag.Warehouses = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.WarehouseModel>(12);
ViewBag.Drivers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.DriverModel>(12);
ViewBag.Accounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.AccountModel>(12);
ViewBag.Employees = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.EmployeeModel>(12);
ViewBag.StopajKodlari = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.StopajKoduModel>(12);
ViewBag.NakliyeFirmalari = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor116Controller.NakliyeFirmaModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        private List<StopajKoduModel> GetStopajKodlari(string dbKey)
 {return default;
}






        private static readonly Dictionary<int, (string Hdr, string Ln, string Ad)> _bazTablolar = new Dictionary<int, (string, string, string)>
        {
            { 22, ("OPOR", "POR1", "Satınalma Siparişi") },
            { 20, ("OPDN", "PDN1", "Satınalma Mal Girişi") },
            { 18, ("OPCH", "PCH1", "Satınalma Faturası") },
            { 21, ("ORPD", "RPD1", "Satınalma İade İrsaliyesi") },
            { 19, ("ORPC", "RPC1", "Satınalma İade Faturası") }
        };


        private static readonly int[] _izinliBazTipleri = new int[] { 18, 21 };


        [HttpGet("GetOpenBaseDocuments")]
        public IActionResult GetOpenBaseDocuments(string cardCode, int baseType, int docNum = 0)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "CardCode", "CardName", "DocCur", "DocTotal", "ReferansliIadeler" }), baseTypeName = global::WebApplication3.OrnekDoldurucu.Deger<string>("baseTypeName", 0), neden = global::WebApplication3.OrnekDoldurucu.Deger<string>("neden", 0) });
}



        [HttpGet("KopyaEk")]
        public IActionResult KopyaEk(int docEntry)  {return Json(new { success = true, ek = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("ek", 0) });
}

        [HttpGet("GetBaseDocument")]
        public IActionResult GetBaseDocument(int baseEntry, int baseType)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor116Controller.PurchaseCreditNoteDetailViewModel>(), baseTypeName = global::WebApplication3.OrnekDoldurucu.Deger<string>("baseTypeName", 0), baseType = global::WebApplication3.OrnekDoldurucu.Deger<int>("baseType", 0), baseEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("baseEntry", 0), ek = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("ek", 0) });
}


        [HttpGet("GetCariStopajKodlari")]
        public IActionResult GetCariStopajKodlari(string cardCode)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor116Controller.StopajKoduModel>(12) });
}

        [HttpGet("GetCustomerInfo")]
        public IActionResult GetCustomerInfo(string cardCode)
 {return Json(new { currency = global::WebApplication3.OrnekDoldurucu.Deger<string>("currency", 0), wtLiable = global::WebApplication3.OrnekDoldurucu.Deger<string>("wtLiable", 0), defWtCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("defWtCode", 0), wtCodes = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Currency", "WTLiable", "DefWTCode", "ExtraDays", "ExtraMonth", "PayDuMonth", "PymntGroup" }), vadeGun = global::WebApplication3.OrnekDoldurucu.Deger<int>("vadeGun", 0), vadeAy = global::WebApplication3.OrnekDoldurucu.Deger<int>("vadeAy", 0), vadeAySonu = global::WebApplication3.OrnekDoldurucu.Deger<bool>("vadeAySonu", 0), vadeAd = global::WebApplication3.OrnekDoldurucu.Deger<string>("vadeAd", 0) });
}

        [HttpGet("GetCustomerAddresses")]
        public IActionResult GetCustomerAddresses(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor116Controller.BPAddressModel>(12));
}

        [HttpGet("GetAllExchangeRates")]
        public IActionResult GetAllExchangeRates(string date)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, decimal>>());
}

        [HttpGet("GetItemPrice")]
        public IActionResult GetItemPrice(string cardCode, string itemCode)
 {return Json(new { price = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("price", 0), currency = global::WebApplication3.OrnekDoldurucu.Deger<string>("currency", 0), dfltWH = global::WebApplication3.OrnekDoldurucu.Deger<string>("dfltWH", 0), secUnitName = global::WebApplication3.OrnekDoldurucu.Deger<string>("secUnitName", 0), secUnitMultiplier = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("secUnitMultiplier", 0) });
}

        public class StokIstek { public string ItemCode { get; set; } public string WhsCode { get; set; } }


        [HttpPost("GetItemStocks")]
        public IActionResult GetItemStocks([FromBody] List<StokIstek> istekler)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "WhsCode", "OnHand" }) });
}

        [HttpGet("GetItemStock")]
        public IActionResult GetItemStock(string itemCode, string whsCode)
 {return Json(new { onHand = 0 });
}

        [HttpGet("GetPurchaseCreditNoteNavigation")]
        public IActionResult GetPurchaseCreditNoteNavigation(int currentDocEntry, string direction)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor116Controller.PurchaseCreditNoteDetailViewModel>() });
}

        [HttpPost("FindPurchaseCreditNote")]
        public IActionResult FindPurchaseCreditNote([FromBody] PurchaseCreditNoteSearchModel search)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor116Controller.PurchaseCreditNoteSearchResultModel>(12) });
}

        [HttpGet("GetPurchaseCreditNoteDetails")]
        public IActionResult GetPurchaseCreditNoteDetails(int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor116Controller.PurchaseCreditNoteDetailViewModel>() });
}





        public class NakliyeFirmaModel { public string FirmaNo { get; set; } public string FirmaAdi { get; set; } public string FirmaVkn { get; set; } }


        private static string NakliyeSaatiMetni(object deger)
 {return default;
}

        private List<NakliyeFirmaModel> GetNakliyeFirmalari(string dbKey)
 {return default;
}

        [HttpGet("NakliyeFirmalari")]
        public IActionResult NakliyeFirmalari()  {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor116Controller.NakliyeFirmaModel>(12));
}


        [HttpPost("NakliyeFirmaEkle")]
        public async Task<IActionResult> NakliyeFirmaEkle([FromBody] NakliyeFirmaModel request)
 {return Json(new { success = true, message = "Nakliyeci firma eklendi.", firma = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor116Controller.NakliyeFirmaModel>() });
}


        [HttpPost("SaveBPAddress")]
        public async Task<IActionResult> SaveBPAddress([FromBody] BPAddressModel request, [FromQuery] string cardCode)
 {return Json(new { success = true, message = "Adres başarıyla cariye eklendi." });
}

        [HttpPost("AddDriver")]
        public IActionResult AddDriver([FromBody] DriverModel request)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor116Controller.DriverModel>() });
}




        [HttpPost("CreatePurchaseCreditNote")]
        public async Task<IActionResult> CreatePurchaseCreditNote([FromBody] PurchaseCreditNoteCreationModel request)
 {return Json(new { success = true, taslak = global::WebApplication3.OrnekDoldurucu.Deger<bool>("taslak", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("docNum", 0) });
}

        public class AccountModel { public string AcctCode { get; set; } public string FormatCode { get; set; } public string AcctName { get; set; } }


        private List<AccountModel> GetAccounts(string dbKey)
 {return default;
}

        public class EmployeeModel { public int EmpId { get; set; } public string Ad { get; set; } }


        private List<EmployeeModel> GetEmployees(string dbKey)
 {return default;
}









        private static readonly string GiderKolonAdi = "U_BE1_GIDER";


        private void GiderleriYaz(string connectionString, string tablo, int docEntry, List<PurchaseCreditNoteLineModel> lines)
 {}



        [HttpGet("SatirHesabi")]
        public IActionResult SatirHesabi(string itemCode, string accountCode, string whsCode, string cardCode, string vatGroup, string docDate, int? baseType, int? baseEntry, int? baseLine)
 {return Json(new { success = true, hesap = global::WebApplication3.OrnekDoldurucu.Deger<string>("hesap", 0), hesapKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("hesapKodu", 0), hesapAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("hesapAdi", 0), tip = global::WebApplication3.OrnekDoldurucu.Deger<string>("tip", 0) });
}

        [HttpPost("GiderAlaniniHazirla")]        public async Task<IActionResult> GiderAlaniniHazirla()
 {return Json(new { success = true, mevcut = true, olusturuldu = false, degerler = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("degerler", i2)).ToList() });
}


        private List<CustomerModel> GetCustomers(string dbKey)
 {return default;
}




        [HttpGet("KalemAra")]
        public IActionResult KalemAra(string q)
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode" }) });
}

        [HttpGet("HesapAra")]
        public IActionResult HesapAra(string q)
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "AcctCode" }) });
}
        private List<ItemModel> GetItems(string dbKey)
 {return default;
}

        private List<SalesEmployeeModel> GetSalesEmployees(string dbKey)
 {return default;
}






        private async Task<string> MutabakatYapAsync(HttpClient client, string connectionString, string cardCode, int iadeDocEntry, List<int> bazFaturaEntryler, DateTime reconDate)
 {return default;
}

        private class BazVergiCakismasi { public int Sira; public string ItemCode; public string Secilen; public string BazKod; public string BazBelge; }





        private List<BazVergiCakismasi> BazVergiKoduCakismalari(string connectionString, List<PurchaseCreditNoteLineModel> lines)
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

        public class PurchaseCreditNoteSearchModel
        {
            public int? DocNum { get; set; }

            public string DocType { get; set; }
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

        public class PurchaseCreditNoteSearchResultModel
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

        public class PurchaseCreditNoteDetailViewModel
        {
            public int DocEntry { get; set; }

            public string DocType { get; set; } = "I";

            public string DocStatus { get; set; }

            public string Canceled { get; set; }
            public int OwnerCode { get; set; }
            public int DocNum { get; set; }
            public string CardCode { get; set; }
            public string DocDate { get; set; }
            public string DocDueDate { get; set; }
            public string TaxDate { get; set; }
            public int SlpCode { get; set; }
            public string DocCurrency { get; set; }
            public decimal DocRate { get; set; }

            public decimal WTSum { get; set; }
            public string Comments { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_SEND { get; set; }
            public string U_BE1_MUAFCODE { get; set; }
            public string IadeFaturaNo { get; set; }
            public string IadeFaturaTarihi { get; set; }
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
            public string U_BE1_FRMADI { get; set; }
            public string U_BE1_FRMVKN { get; set; }
            public string U_BE1_NAKLIYETARIHI { get; set; }
            public string U_BE1_NAKLIYESAATI { get; set; }

            public AddressExtensionModel AddressExtension { get; set; }
            public List<PurchaseCreditNoteLineModel> Lines { get; set; } = new List<PurchaseCreditNoteLineModel>();
        }

        public class PurchaseCreditNoteCreationModel
        {

            public int? KopyaKaynak { get; set; }

            public string DocType { get; set; } = "I";

            public string DocStatus { get; set; }

            public string Canceled { get; set; }

            public int? OwnerCode { get; set; }

            public bool Taslak { get; set; }
            public string CardCode { get; set; }
            public DateTime DocDate { get; set; }
            public DateTime DocDueDate { get; set; }
            public DateTime TaxDate { get; set; }
            public int SalesPersonCode { get; set; }
            public string Comments { get; set; }
            public string DocCurrency { get; set; }
            public decimal DocRate { get; set; }

            public decimal WTSum { get; set; }
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

            public string U_BE1_FRMADI { get; set; }
            public string U_BE1_FRMVKN { get; set; }

            public string U_BE1_NAKLIYETARIHI { get; set; }
            public string U_BE1_NAKLIYESAATI { get; set; }

            public string PayToCode { get; set; }

            public string NumAtCard { get; set; }
            public string ShipToCode { get; set; }


            public string WTCode { get; set; }

            public string IadeFaturaNo { get; set; }

            public string IadeFaturaTarihi { get; set; }




            public string BazBaglantiTercihi { get; set; }

            public AddressExtensionModel AddressExtension { get; set; }
            public List<PurchaseCreditNoteLineModel> DocumentLines { get; set; }
        }

        public class StopajKoduModel { public string Code { get; set; } public string Name { get; set; } public decimal Rate { get; set; } /* OWHT.Type: V = KDV tevkifatı (baz KDV), diğer = net üzerinden */ public string Type { get; set; } }

        public class PurchaseCreditNoteLineModel
        {

            public int? KopyaSatir { get; set; }
            public string ItemCode { get; set; }

            public string AccountCode { get; set; }
            public string AccountName { get; set; }
            public string Description { get; set; }
            public double LineTotal { get; set; }

            public string Gider { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            public string Currency { get; set; }
            public string VatGroup { get; set; }
            public string WarehouseCode { get; set; }
            public string SecUnitName { get; set; }
            public decimal SecUnitMultiplier { get; set; }


            public bool WTLiable { get; set; }


            public int? BaseType { get; set; }
            public int? BaseEntry { get; set; }
            public int? BaseLine { get; set; }
        }
    }
}