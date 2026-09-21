// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [AllowAnonymous]
    [Route("[controller]")]
    public class MobilSiparisController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MobilSiparisController> _logger;

        private string GetConnectionString()  {return default;
}
        private string GetDbName()  {return default;
}


        public class LoginRequest { public string Username { get; set; } public string Password { get; set; } }
        public class SalesEmployeeModel { public int SlpCode { get; set; } public string SlpName { get; set; } }
        public class VatGroupModel { public string Code { get; set; } public string Name { get; set; } public decimal Rate { get; set; } }

        public class MobileOrderLine
        {
            public string ItemCode { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            public string Currency { get; set; }
        }

        public class MobileOrderRequest
        {
            public string CardCode { get; set; }
            public int SalesPersonCode { get; set; }
            public string VatGroup { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string Comments { get; set; }
            public List<MobileOrderLine> DocumentLines { get; set; }
        }

        public class CariViewModel
        {
            public string CardCode { get; set; }
            public string CardName { get; set; }
        }

        public class AccountBalanceViewModel
        {
            public string CompanyName { get; set; }
            public string AccountCode { get; set; }
            public string AccountName { get; set; }
            public decimal TLBalance { get; set; }
            public decimal CurrencyBalance { get; set; }
            public string Currency { get; set; }
        }

        public class CariEkstreViewModel
        {
            public string Sirket { get; set; }
            public int IslemNo { get; set; }
            public DateTime? KayitTarihi { get; set; }
            public DateTime? VadeTarihi { get; set; }
            public string MuhatapKodu { get; set; }
            public string MuhatapAdi { get; set; }
            public string Phone1 { get; set; }
            public string Street { get; set; }
            public string County { get; set; }
            public string State { get; set; }
            public string Country { get; set; }
            public string Aciklama { get; set; }
            public string AktarimTipi { get; set; }
            public string IslemTipi { get; set; }
            public decimal? TRYB { get; set; }
            public decimal? TRYA { get; set; }
            public decimal? TRY_KmlBky { get; set; }
            public decimal? IslemB { get; set; }
            public decimal? IslemA { get; set; }
            public decimal? Islem_KmlBky { get; set; }
            public string IslemPB { get; set; }
        }


        [HttpGet]
        public IActionResult Index()
 {ViewBag.VatGroups = new System.Collections.Generic.List<object>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("~/Views/MobilSiparis/Index.cshtml");
}


        [HttpPost("UserLogin")]
        public IActionResult UserLogin([FromBody] LoginRequest req)
 {return Json(new { success = true, slpCode = global::WebApplication3.OrnekDoldurucu.Deger<int>("slpCode", 0), slpName = "SİSTEM YÖNETİCİSİ (ADMİN)" });
}




        [HttpGet("SearchCustomers")]
        public IActionResult SearchCustomers(string q, int slpCode)
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName" }) });
}

        [HttpGet("SearchItems")]
        public IActionResult SearchItems(string q)
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName" }) });
}

        [HttpGet("GetCustomerHistoryItems")]
        public IActionResult GetCustomerHistoryItems(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName" }));
}

        private List<VatGroupModel> GetVatGroups(string connStr)
 {return default;
}




        [HttpGet("GetOrderStatuses")]
        public IActionResult GetOrderStatuses(int slpCode, string cardCode, string startDate, string endDate)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "CardCode", "CardName", "DocTotal", "DocCur", "StatusName" }));
}

        [HttpGet("GetOrderDetails")]
        public IActionResult GetOrderDetails(int docEntry)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "Dscription", "Quantity", "Price", "Currency", "LineTotal" }));
}

        [HttpGet("GetItemPrice")]
        public IActionResult GetItemPrice(string cardCode, string itemCode)
 {return Json(new { price = 0, currency = "TRY" });
}

        [HttpGet("GetPriceList")]
        public IActionResult GetPriceList(string itemCode, string cardCode, int slpCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Code", "Name", "Price", "Currency", "Type" }));
}



        [HttpPost("SubmitOrder")]
        public async Task<IActionResult> SubmitOrder([FromBody] MobileOrderRequest req)
 {return Json(new { success = true, message = "Siparişiniz SAP sistemine başarıyla kaydedildi!" });
}




        [HttpGet("GetCariEkstreDataJSON")]
        public IActionResult GetCariEkstreDataJSON(string bpCode, string startDate, string endDate, [FromQuery] List<string> aktarimTipi, bool includeInitialBalance = false, bool includeConnectedBp = false, bool tlEkstre = false)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.MobilSiparisController.CariEkstreViewModel>(12), accountBalances = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.MobilSiparisController.AccountBalanceViewModel>(12) });
}

        private string GetBpName(string connectionString, string bpCode)
 {return default;
}

        private string GetConnectedBpCode(string connectionString, string bpCode)
 {return default;
}

        private List<AccountBalanceViewModel> GetBpRelatedAccountBalances(string connectionString, string bpName)
 {return default;
}

        private List<CariEkstreViewModel> GetCariEkstreData(string connectionString, string bpCode, DateTime? startDate, DateTime? endDate, List<string> aktarimTipi, bool includeInitialBalance, string exclusionKeyword = null)
 {return default;
}

        [HttpGet("DownloadEkstrePdf")]
        public IActionResult DownloadEkstrePdf(string bpCode, DateTime? startDate, DateTime? endDate, [FromQuery] List<string> aktarimTipi, bool includeInitialBalance = false, bool includeConnectedBp = false, bool tlEkstre = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

    }
}