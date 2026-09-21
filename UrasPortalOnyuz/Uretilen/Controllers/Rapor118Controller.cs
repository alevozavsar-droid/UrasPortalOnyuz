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
    public class Rapor118Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor118Controller> _logger;
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

        private readonly List<UdfValidValue> _checkTransactionTypesFallback = new List<UdfValidValue>
        {
            new UdfValidValue { Code = "1", Description = "Portföyde" },
            new UdfValidValue { Code = "2", Description = "Planlandı" },
            new UdfValidValue { Code = "3", Description = "Erteleme Yapıldı - Portföyde" },
            new UdfValidValue { Code = "4", Description = "Erteleme Yapıldı - Bankada" },
            new UdfValidValue { Code = "5", Description = "Bankaya Tahsile Verildi" },
            new UdfValidValue { Code = "6", Description = "Bankadan Tahsil Edildi" },
            new UdfValidValue { Code = "7", Description = "Elden Tahsil Edildi" },
            new UdfValidValue { Code = "8", Description = "Ciro Edildi" },
            new UdfValidValue { Code = "9", Description = "Teminata Verildi" },
            new UdfValidValue { Code = "10", Description = "Karşılıksız" },
            new UdfValidValue { Code = "11", Description = "Protesto Edildi" },
            new UdfValidValue { Code = "12", Description = "Konkordato - Portföyde" },
            new UdfValidValue { Code = "13", Description = "Konkordato - Bankada" },
            new UdfValidValue { Code = "14", Description = "Konkordato - Tedarikçide" },
            new UdfValidValue { Code = "15", Description = "İade Edildi" },
            new UdfValidValue { Code = "16", Description = "İptal Edildi" }
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        private string GetCompanyLogoPath(string companyName)
 {return default;
}

        private List<UdfValidValue> GetUDFValidValues(string dbKey, string tableId, string aliasId)
 {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor118Controller.DatabaseConfig>(12);
ViewBag.CurrentDbDisplay = "";
ViewBag.Accounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor118Controller.AccountModel>(12);
ViewBag.Currencies = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.Banks = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor118Controller.BankModel>(12);
ViewBag.CreditCards = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor118Controller.CreditCardDefModel>(12);
ViewBag.DovizTipleri = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor118Controller.UdfValidValue>(12);
ViewBag.IadeDurumlari = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor118Controller.UdfValidValue>(12);
ViewBag.AktarTipleri = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor118Controller.UdfValidValue>(12);
ViewBag.CheckTransactionTypes = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor118Controller.UdfValidValue>(12);
ViewBag.IsCheckBond = false;
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("SearchVendors")]
        public IActionResult SearchVendors(string q)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor118Controller.VendorModel>(12));
}

        [HttpGet("GetVendorByCode")]
        public IActionResult GetVendorByCode(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor118Controller.VendorModel>());
}

        [HttpGet("GetVendorAddresses")]
        public IActionResult GetVendorAddresses(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor118Controller.BPAddressModel>(12));
}

        [HttpGet("GetOpenInvoices")]
        public IActionResult GetOpenInvoices(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor118Controller.OpenInvoiceModel>(12));
}

        [HttpGet("GetOutgoingPaymentNavigation")]
        public IActionResult GetOutgoingPaymentNavigation(int currentDocEntry, string direction)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor118Controller.OutgoingPaymentDetailViewModel>() });
}

        [HttpPost("FindOutgoingPayment")]
        public IActionResult FindOutgoingPayment([FromBody] OutgoingPaymentSearchModel search)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor118Controller.OutgoingPaymentSearchResultModel>(12) });
}

        [HttpGet("GetOutgoingPaymentDetails")]
        public IActionResult GetOutgoingPaymentDetails(int docEntry)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor118Controller.OutgoingPaymentDetailViewModel>() });
}

        [HttpPost("CreateOutgoingPayment")]
        public async Task<IActionResult> CreateOutgoingPayment([FromBody] OutgoingPaymentCreationModel request)
 {return Json(new { success = true, docNums = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<int>("docNums", i2)).ToList(), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private Dictionary<string, object> BuildPaymentPayload(OutgoingPaymentCreationModel request, bool includeNonCheckMeans, List<PaymentCheckModel> checksToInclude)
 {return default;
}

        private string ExtractErrorMessage(string resStr)
 {return default;
}

        [HttpPost("CancelOutgoingPayment")]
        public async Task<IActionResult> CancelOutgoingPayment(int docEntry)
 {return Json(new { success = true, message = "Belge başarıyla iptal edildi." });
}

        [HttpPost("CreateOdemeBordro")]
        public async Task<IActionResult> CreateOdemeBordro([FromBody] List<int> docNums)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        public class OdemeHeader
        {
            public int DocNum { get; set; }
            public DateTime DocDate { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string DocCurr { get; set; }
            public decimal DocTotal { get; set; }
            public decimal CashSum { get; set; }
            public decimal TrsfrSum { get; set; }
            public decimal CreditSum { get; set; }
            public decimal CheckSum { get; set; }
            public string Canceled { get; set; }
            public DateTime? UpdateDate { get; set; }
            public string IptalEdenKisi { get; set; }
        }

        public class BordroCekDetay
        {
            public string CekNo { get; set; }
            public DateTime Tarih { get; set; }
            public string Banka { get; set; }
            public string Sube { get; set; }
            public string Musteri { get; set; }
            public decimal CekTutari { get; set; }
            public string ParaBirimi { get; set; }
            public string OdemeBelgeNo { get; set; }
            public string BankaHesapNo { get; set; }
        }

        private List<AccountModel> GetAccounts(string dbKey)
 {return default;
}

        private List<string> GetCurrencies(string dbKey)
 {return default;
}

        private List<BankModel> GetBanks(string dbKey)
 {return default;
}

        private List<CreditCardDefModel> GetCreditCards(string dbKey)
 {return default;
}

        public class UdfValidValue { public string Code { get; set; } public string Description { get; set; } }
        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class VendorModel { public string CardCode { get; set; } public string CardName { get; set; } }
        public class AccountModel { public string AcctCode { get; set; } public string AcctName { get; set; } public string FormatCode { get; set; } }
        public class BankModel { public string BankCode { get; set; } public string BankName { get; set; } public string CountryCode { get; set; } }
        public class CreditCardDefModel { public int CreditCard { get; set; } public string CardName { get; set; } }
        public class BPAddressModel { public string AddressName { get; set; } public string AddressType { get; set; } }

        public class OpenInvoiceModel
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public string DocDate { get; set; }
            public string DocDueDate { get; set; }
            public string Currency { get; set; }
            public decimal DocTotal { get; set; }
            public decimal OpenAmount { get; set; }
        }

        public class OutgoingPaymentSearchModel
        {
            public int? DocNum { get; set; }
            public string CardCode { get; set; }
            public string DocDate { get; set; }
        }

        public class OutgoingPaymentSearchResultModel
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string DocDate { get; set; }
            public decimal DocTotal { get; set; }
            public string DocCurrency { get; set; }
        }

        public class OutgoingPaymentDetailViewModel
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string DocDate { get; set; }
            public string TaxDate { get; set; }
            public bool Canceled { get; set; }
            public string Comments { get; set; }
            public string JournalRemarks { get; set; }
            public string DocCurrency { get; set; }
            public decimal TotalAmount { get; set; }

            public decimal CashSum { get; set; }
            public decimal TransferSum { get; set; }
            public decimal CreditSum { get; set; }
            public decimal CheckSum { get; set; }

            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_DOVIZTIPI { get; set; }
            public string U_BE1_NOT { get; set; }
            public string U_BE1_SLPNAME { get; set; }
            public string U_BE1_COMPANY { get; set; }
            public string U_BE1_TAHSILAT { get; set; }
            public string U_BE1_GRSYAPAN { get; set; }
            public string U_BE1_CKSYAPAN { get; set; }
            public string U_BE1_IADE { get; set; }
            public string U_BE1_CEKISLEMTUR { get; set; }
            public string U_BE1_TARGETDOC { get; set; }
            public string U_BE1_TARGETTYPE { get; set; }

            public List<InvoicePaymentLine> Invoices { get; set; } = new List<InvoicePaymentLine>();
            public List<PaymentCheckModel> Checks { get; set; } = new List<PaymentCheckModel>();
        }

        public class OutgoingPaymentCreationModel
        {
            public string CardCode { get; set; }
            public string DocCurrency { get; set; }
            public DateTime DocDate { get; set; }
            public DateTime TaxDate { get; set; }
            public string Comments { get; set; }
            public string JournalRemarks { get; set; }
            public decimal TotalPaymentAmount { get; set; }

            public string TransferAccount { get; set; }
            public DateTime? TransferDate { get; set; }
            public string TransferReference { get; set; }
            public decimal TransferSum { get; set; }

            public string CashAccount { get; set; }
            public decimal CashSum { get; set; }

            public bool SplitChecks { get; set; }
            public List<PaymentCheckModel> PaymentChecks { get; set; } = new List<PaymentCheckModel>();
            public List<PaymentCreditCardModel> PaymentCreditCards { get; set; } = new List<PaymentCreditCardModel>();

            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_DOVIZTIPI { get; set; }
            public string U_BE1_NOT { get; set; }
            public string U_BE1_SLPNAME { get; set; }
            public string U_BE1_COMPANY { get; set; }
            public string U_BE1_TAHSILAT { get; set; }
            public string U_BE1_GRSYAPAN { get; set; }
            public string U_BE1_CKSYAPAN { get; set; }
            public string U_BE1_IADE { get; set; }
            public string U_BE1_CEKISLEMTUR { get; set; }
            public string U_BE1_TARGETDOC { get; set; }
            public string U_BE1_TARGETTYPE { get; set; }

            public List<InvoicePaymentLine> Invoices { get; set; } = new List<InvoicePaymentLine>();
        }

        public class PaymentCheckModel
        {
            public DateTime DueDate { get; set; }
            public decimal CheckSum { get; set; }
            public string BankCode { get; set; }
            public string CountryCode { get; set; }
            public int CheckNumber { get; set; }
            public string CheckAccount { get; set; }
            public string AccounttNum { get; set; }
            public string Branch { get; set; }

            public string U_BE1_CHECKNUMBER { get; set; }
            public string U_BE1_BANKBRANCH { get; set; }
            public string U_BE1_ISCHECKBOND { get; set; }
            public string U_BE1_CHECKPORTNO { get; set; }
            public string U_BE1_TRANSFERBP { get; set; }
            public string U_BE1_CHECKSTATUS { get; set; }
            public string U_BE1_BANKNUM { get; set; }
            public string U_BE1_NOT { get; set; }
            public string U_BE1_KLD { get; set; }
        }

        public class PaymentCreditCardModel
        {
            public int CreditCard { get; set; }
            public string CreditCardNumber { get; set; }
            public string VoucherNum { get; set; }
            public decimal CreditSum { get; set; }
            public string CreditAcct { get; set; }
        }

        public class InvoicePaymentLine
        {
            public int DocEntry { get; set; }
            public decimal AppliedAmount { get; set; }
        }
    }
}