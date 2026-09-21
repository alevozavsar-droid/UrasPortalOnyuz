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
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Text.RegularExpressions;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor97Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor97Controller> _logger;

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
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.Customers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor97Controller.CustomerModel>(12);
ViewBag.Groups = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor97Controller.LookupModel>(12);
ViewBag.SalesPersons = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor97Controller.LookupModel>(12);
ViewBag.PaymentTerms = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor97Controller.LookupModel>(12);
ViewBag.ControlAccounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor97Controller.LookupModel>(12);
ViewBag.ErrorMessage = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetBPDetails")]
        public IActionResult GetBPDetails(string cardCode)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor97Controller.BPFullModel>() });
}

        [HttpPost("UpdateBP")]
        public async Task<IActionResult> UpdateBP([FromBody] BPFullModel req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpGet("GetBPCodePrefixes")]
        public IActionResult GetBPCodePrefixes(string cardType = "C")
 {return Json(new { success = true, prefixes = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("prefixes", i2)).ToList() });
}

        [HttpGet("GetNextBPCode")]
        public IActionResult GetNextBPCode(string prefix, string cardType = "C")
 {return Json(new { success = true, code = global::WebApplication3.OrnekDoldurucu.Deger<string>("code", 0) });
}

        [HttpGet("GetCountries")]
        public IActionResult GetCountries()
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Code", "Name" }));
}

        [HttpGet("GetStates")]
        public IActionResult GetStates(string countryCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Code", "Name" }));
}


        [HttpGet("GetBanks")]
        public IActionResult GetBanks(string countryCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "BankCode", "BankName", "SwiftNum" }));
}










        [HttpGet("UstHesaplar")]
        public IActionResult UstHesaplar(string cardType = "C")
 {return Json(new { success = true, liste = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "AcctCode", "AcctName", "Adet", "SonKod" }) });
}

        [HttpPost("MutabakatHesabiAc")]
        public async Task<IActionResult> MutabakatHesabiAc([FromBody] YeniMutabakatHesabiIstek istek)
 {return Json(new { success = true, kod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kod", 0), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", 0), etiket = global::WebApplication3.OrnekDoldurucu.Deger<string>("etiket", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private List<LookupModel> GetControlAccounts(string dbKey)
 {return default;
}

        private List<CustomerModel> GetCustomers(string dbKey)
 {return default;
}

        private List<LookupModel> GetBPGroups(string dbKey)
 {return default;
}

        private List<LookupModel> GetSalesPersons(string dbKey)
 {return default;
}

        private List<LookupModel> GetPaymentTerms(string dbKey)
 {return default;
}

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class CustomerModel { public string CardCode { get; set; } public string CardName { get; set; } }
        public class LookupModel { public int Id { get; set; } public string Code { get; set; } public string Name { get; set; } }

        public class BPAddressModel
        {
            public int? LineNum { get; set; }
            public string AddressName { get; set; }
            public string AddressType { get; set; }
            public string Address2 { get; set; }
            public string Address3 { get; set; }
            public string Street { get; set; }
            public string Block { get; set; }
            public string ZipCode { get; set; }
            public string City { get; set; }
            public string County { get; set; }
            public string Country { get; set; }
            public string State { get; set; }
        }

        public class BPBankAccountModel
        {
            public string BankCode { get; set; }
            public string Country { get; set; }
            public string AccountNo { get; set; }
            public string Branch { get; set; }
            public string IBAN { get; set; }
            public string BICSwiftCode { get; set; }
            public string AccountName { get; set; }
        }

        public class YeniMutabakatHesabiIstek
    {
        public string UstHesap { get; set; }
        public string Ad { get; set; }
    }

    public class BPFullModel
        {
            public bool IsNew { get; set; }
            public bool AddressesModified { get; set; }


            public string CardType { get; set; }

            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string CardFName { get; set; }
            public int GroupCode { get; set; }
            public string Currency { get; set; }
            public string LicTradNum { get; set; }
            public decimal Balance { get; set; }
            public decimal DNotesBal { get; set; }
            public decimal OrdersBal { get; set; }
            public int OprCount { get; set; }

            public string Phone1 { get; set; }
            public string Phone2 { get; set; }
            public string Cellular { get; set; }
            public string Fax { get; set; }
            public string Email { get; set; }
            public string IntrntSite { get; set; }
            public int ShipType { get; set; }
            public string Password { get; set; }
            public string Indicator { get; set; }
            public string ProjectCod { get; set; }
            public int Industry { get; set; }
            public int CmpPrivate { get; set; }
            public string AliasName { get; set; }
            public string CntctPrsn { get; set; }
            public string AddID { get; set; }
            public string VatIdUnCmp { get; set; }
            public string BoEPrsnt { get; set; }
            public string BoEDiscnt { get; set; }
            public string Notes { get; set; }
            public int SlpCode { get; set; }
            public string AgentCode { get; set; }
            public string ChannlBP { get; set; }
            public int DfTcnician { get; set; }
            public int Territory { get; set; }
            public int LangCode { get; set; }
            public string GlblLocNum { get; set; }
            public string BlockComm { get; set; }
            public string ValidFor { get; set; }
            public string ValidFrom { get; set; }
            public string ValidTo { get; set; }
            public string FrozenFor { get; set; }
            public string FrozenFrom { get; set; }
            public string FrozenTo { get; set; }

            public int GroupNum { get; set; }
            public decimal IntrstRate { get; set; }
            public int ListNum { get; set; }
            public decimal Discount { get; set; }
            public decimal CreditLine { get; set; }
            public decimal DebtLine { get; set; }
            public string DunTerm { get; set; }
            public string PriceMode { get; set; }
            public string EffcAllSrc { get; set; }
            public string BankCountr { get; set; }
            public string BankCode { get; set; }
            public string HousBnkAct { get; set; }
            public string HsBnkSwift { get; set; }
            public string HousBnkBrn { get; set; }
            public string IBAN { get; set; }
            public string MandateID { get; set; }
            public string SignDate { get; set; }
            public string CreditCard { get; set; }
            public string CrCardNum { get; set; }
            public string CardValid { get; set; }
            public string OwnerIdNum { get; set; }
            public int AvrageLate { get; set; }
            public int Priority { get; set; }
            public string DflIBAN { get; set; }
            public string HldCode { get; set; }
            public string PartDelivr { get; set; }
            public string EdrsFromBP { get; set; }
            public string EdrsToBP { get; set; }

            public string FatherCard { get; set; }
            public string FatherType { get; set; }
            public string DebPayAcct { get; set; }
            public string DpmClear { get; set; }
            public string DpmIntAct { get; set; }
            public string BlockDunn { get; set; }
            public int DunnLevel { get; set; }
            public string DunnDate { get; set; }
            public string PlngGroup { get; set; }
            public string Affiliate { get; set; }

            public List<BPAddressModel> Addresses { get; set; }
            public List<BPBankAccountModel> BPBankAccounts { get; set; }
        }
    }
}