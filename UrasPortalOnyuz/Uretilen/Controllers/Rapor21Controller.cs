// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebApplication3.Models;
using WebApplication3.Services;


using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;


using ClosedXML.Excel;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Controllers
{
    [Authorize] // Temel giriş izni
    [Route("[controller]")]
    public class Rapor21Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly IWebHostEnvironment _webHostEnvironment;


        private readonly string[] _authorizedApprovers = { "IT02", "GK", "YKB", "GKB", "YK" };

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
             new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
             new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
             new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
             new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
             new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
             new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
             new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
             new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
             new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
             new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
             new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
             new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
             new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
             new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
             new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
             new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

             new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
             new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
             new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
             new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
             new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
             new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
             new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
             new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
             new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
             new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
             new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
             new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
             new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
             new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
             new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
             new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
             new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };

        private readonly List<ApproverViewModel> _approvers = new List<ApproverViewModel>
        {
            new ApproverViewModel { Code = "IT02", Name = "Ali Veli", Email = "kullanici@ornek.local" },
            new ApproverViewModel { Code = "YKB", Name = "Sevgi Ay", Email = "kullanici@ornek.local" },
            new ApproverViewModel { Code = "GK", Name = "Genel Koordinatör", Email = "kullanici@ornek.local" }
        };


        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        private string GetLogoFilename(string dbDisplay)
 {return default;
}

        private string GetCurrentUser()  {return default;
}

        private bool IsUserAuthorizedForApproval()
 {return default;
}


        private string GetApproverName(string code)
 {return default;
}



        private List<CariViewModel> GetCariList(string connectionString)
 {return default;
}

        private string GetBpName(string connectionString, string bpCode)
 {return default;
}


        private List<string> GetAktarimTipleri(string connectionString)
 {return default;
}





        private List<AcikFaturaViewModel> GetAcikFaturalar(string connectionString, string bpCode, List<string> aktarimTipi = null)
 {return default;
}








        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, HashSet<string>> _onayTablosuKolonlari =
            new System.Collections.Concurrent.ConcurrentDictionary<string, HashSet<string>>();


        private static readonly string[] _onayGerekliAlanlar =
        {
            "U_BE1_CARDCODE", "U_BE1_CARDNAME", "U_BE1_DUEDATE", "U_BE1_CHECKNUMBER",
            "U_BE1_AMOUNT", "U_BE1_APPROVALSTATUS", "U_BE1_APPROVERCODE"
        };






        private static HashSet<string> OnayTablosuKolonlari(string connectionString)
 {return default;
}

        private static bool OnayTablosuVarMi(string connectionString)  {return default;
}


        private static List<string> OnayTablosuEksikleri(string connectionString)
 {return default;
}


        private static void OnayTablosuOnbelleginiTemizle(string connectionString)
 {}


        private List<CheckApprovalModel> GetCheckStatus(string connectionString, string bpCode)
 {return default;
}


        private List<CheckApprovalModel> GetAllApprovalHistory(string connectionString)
 {return default;
}


        private List<CheckApprovalModel> GetPendingChecks(string connectionString)
 {return default;
}




        private static string NormalizeAmountForStorage(string raw)
 {return default;
}

        private void AddCheckForApproval(string connectionString, string bpCode, string bpName, CheckRowModel check, string approverCode, string creator, string status = "O")
 {}

        private string GetCheckApprovalStatus(string connectionString, int docEntry)
 {return default;
}

        private void UpdateCheckStatus(string connectionString, int docEntry, string status, string approverCode)
 {}


        private void UpdateCheckEntry(string connectionString, int docEntry, CheckRowModel row)
 {}

        private void DeleteCheckFromDatabase(string connectionString, int docEntry)
 {}


        [HttpGet]
        public IActionResult Index(string bpCode, [FromQuery] List<string> aktarimTipi)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.Approvers = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.ApproverViewModel>(12);
ViewBag.OnayKurulumEksikleri = new System.Collections.Generic.List<object>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Models.AdatHesaplamaModel>());
}










        [HttpPost]
        [Route("SetupOnayTablosu")] // controller [Route("[controller]")] oldugu icin sablonsuz action /Rapor21'e dusuyordu
        public async Task<IActionResult> SetupOnayTablosu()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        [HttpPost]
        [Route("SendForApproval")]
        public IActionResult SendForApproval([FromBody] CheckApprovalRequestModel request)
 {return Json("Başarıyla onaya gönderildi.");
}

        [HttpPost]
        [Route("PerformApproval")]
        public IActionResult PerformApproval([FromBody] ApprovalPerformModel req)
 {return Json("Onaylandı.");
}

        [HttpPost]
        [Route("DeclineApproval")]
        public IActionResult DeclineApproval([FromBody] ApprovalPerformModel req)
 {return Json("Reddedildi.");
}




        [HttpPost]
        [Route("SaveDraftEntries")]
        public IActionResult SaveDraftEntries([FromBody] CheckApprovalRequestModel request)
 {return Json("Kaydedildi.");
}

        [HttpPost]
        [Route("UpdateCheckEntries")]
        public IActionResult UpdateCheckEntries([FromBody] CheckApprovalRequestModel request)
 {return Json(global::WebApplication3.OrnekDoldurucu.Deger<string>("", 0));
}

        [HttpPost]
        [Route("DeleteCheck")]
        public IActionResult DeleteCheck([FromBody] int docEntry)
 {return Json("Silindi.");
}

        [HttpGet]
        [Route("GetMyPendingApprovalsJson")]
        public IActionResult GetMyPendingApprovalsJson()
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.CheckApprovalModel>(12));
}

        [HttpGet]
        [Route("GetApprovalHistoryJson")]
        public IActionResult GetApprovalHistoryJson()
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.CheckApprovalModel>(12));
}



        [HttpPost]
        [Route("DownloadPdf")]
        public IActionResult DownloadPdf([FromBody] PdfExportRequestModel data)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        static IContainer PdfHeaderStyle(IContainer container)  {return default;
}
        static IContainer PdfValueStyle(IContainer container)  {return default;
}
        static IContainer PdfFooterStyle(IContainer container)  {return default;
}



        [HttpPost]
        [Route("DownloadExcel")]
        public IActionResult DownloadExcel([FromBody] PdfExportRequestModel data)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

    }
}
namespace WebApplication3.Models
{
    public class PdfExportRequestModel
    {
        public string BpCode { get; set; }
        public string BpName { get; set; }
        public decimal InterestRate { get; set; }
        public string ReportType { get; set; }
        public List<PdfInvoiceItem> Invoices { get; set; }
        public List<PdfCheckItem> Payments { get; set; }
        public List<PdfMonthlySummary> MonthlySummaries { get; set; }
        public List<SimulationRow> SimulationRows { get; set; }
    }

    public class PdfInvoiceItem
    {
        [Display(Name = "Belge No")]
        public string DocNum { get; set; }
        [Display(Name = "Fatura Tarihi")]
        public string DocDate { get; set; }
        [Display(Name = "Vade Tarihi")]
        public string DueDate { get; set; }
        [Display(Name = "Tutar")]
        public decimal Amount { get; set; }
    }

    public class PdfCheckItem
    {
        [Display(Name = "Ödeme Yöntemi")]
        public string Type { get; set; }
        [Display(Name = "Çek / Evrak No")]
        public string CheckNumber { get; set; }
        [Display(Name = "Vade Tarihi")]
        public string DueDate { get; set; }
        [Display(Name = "Tutar")]
        public decimal Amount { get; set; }
    }

    public class PdfMonthlySummary
    {
        public string MonthName { get; set; }
        public string AvgDate { get; set; }
        public decimal Amount { get; set; }
        public string NetGecikme { get; set; }
        public decimal VadeFarki { get; set; }
    }

    public class SimulationRow
    {
        public string BelgeNo { get; set; }
        public string OdemeTipi { get; set; }
        public string EvrakNo { get; set; }
        public string OdemeTarihi { get; set; }
        public string VadeTarihi { get; set; }
        public int Gun { get; set; }
        public decimal Odenen { get; set; }
        public decimal KalanBakiye { get; set; }
        public decimal VFTutari { get; set; }
    }
}