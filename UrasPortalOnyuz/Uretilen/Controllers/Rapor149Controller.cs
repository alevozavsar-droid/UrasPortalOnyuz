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
    public class Rapor149Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor149Controller> _logger;
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
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA", DbName = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
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
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" }
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

        private string CleanJournalRemarks(string rawMemo, string targetAccount)
 {return default;
}

        private string TranslateSapError(string englishError)
 {return default;
}

        private string ExtractErrorMessage(string resStr)
 {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.Databases = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor149Controller.DatabaseConfig>(12);
ViewBag.CurrentDbDisplay = "";
ViewBag.Currencies = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.IadeDurumlari = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor149Controller.UdfValidValue>(12);
ViewBag.AktarTipleri = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor149Controller.UdfValidValue>(12);
ViewBag.CheckTransactionTypes = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor149Controller.UdfValidValue>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("SearchAccountsAndBPs")]
        public IActionResult SearchAccountsAndBPs(string q, string type)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor149Controller.AccountBPModel>(12));
}

        [HttpGet("GetAvailableChecks")]
        public IActionResult GetAvailableChecks(string checkType, string date1, string date2, string currency)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor149Controller.AvailableCheckModel>(12));
}

        [HttpGet("GetDepositNavigation")]
        public IActionResult GetDepositNavigation(int currentDocEntry, string direction)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor149Controller.DepositDetailViewModel>() });
}

        [HttpPost("FindDeposit")]
        public IActionResult FindDeposit([FromBody] DepositSearchModel search)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor149Controller.DepositSearchResultModel>(12) });
}

        [HttpGet("GetDepositDetails")]
        public IActionResult GetDepositDetails(int deposId)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor149Controller.DepositDetailViewModel>() });
}





        [HttpPost("CreateDeposit")]
        public Task<IActionResult> CreateDeposit([FromBody] DepositCreationModel request)
 {return System.Threading.Tasks.Task.FromResult<IActionResult>(Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 }));
}


        private async Task<IActionResult> CreateDepositEski(DepositCreationModel request)
 {return default;
}

        private async Task<Dictionary<int, string>> GetCheckCurrenciesAsync(string connectionString, List<int> checkKeys)
 {return default;
}

        private async Task<string> GetAccountCurrencyAsync(string connectionString, string accountType, string accountCode)
 {return default;
}

        private Dictionary<string, object> BuildDepositPayload(DepositCreationModel request, List<object> checkLines, string paraBirimi = null)
 {return default;
}

        private const string OdptIptalMesaji = "SAP 'Vadeli çek ibrazı' (ODPT) belgesi için SAP API bulunmadığından web'den iptal/iade yapılamaz. Lütfen SAP B1 › Bankacılık › İbrazlar › Vadeli Çek İbrazı ekranından iptal edin.";

        [HttpPost("CancelDeposit")]
        public async Task<IActionResult> CancelDeposit(int deposId, string dateType)
 {return Json(new { success = true, message = "Vadeli İbraz başarıyla iptal edildi ve çekler portföye geri alındı." });
}

        [HttpPost("ReturnDeposit")]
        public async Task<IActionResult> ReturnDeposit(int deposId, string returnDate, string dateType)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpGet("GetOpenDepositsForBulk")]
        public IActionResult GetOpenDepositsForBulk(bool onlyErrors = false)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DeposId", "DeposDate", "LocTotal", "BanckAcct", "BanckAcctName", "CekNumaralari", "TahsilatTarihi" }) });
}

[HttpPost("BulkCancelDeposits")]
        public async Task<IActionResult> BulkCancelDeposits([FromBody] BulkCancelDepositRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), successList = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<int>("successList", i2)).ToList(), errorList = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("errorList", i2)).ToList() });
}

        [HttpPost("BulkFixDeposits")]
        public async Task<IActionResult> BulkFixDeposits([FromBody] BulkFixDepositRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), successList = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<int>("successList", i2)).ToList(), errorList = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("errorList", i2)).ToList() });
}

        [HttpPost("CreateIbrazBordro")]
        public async Task<IActionResult> CreateIbrazBordro([FromBody] List<int> docNums)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private List<string> GetCurrencies(string dbKey)
 {return default;
}




        private static readonly string _bekleyenSql = @"
            SELECT H.CheckKey, H.CheckNum, H.CheckDate, H.CheckSum, ISNULL(NULLIF(H.Currency, ''), 'TRY') AS Currency,
                   H.CardCode, H.CardName, H.BankCode, ISNULL(B.BankName, H.BankCode) AS BankName, H.Branch, H.RcptNum,
                   D.DeposId, D.DeposNum, D.DeposDate, D.BanckAcct, ISNULL(A.AcctName, R.CardName) AS BanckAcctName
            FROM OCHH H WITH (NOLOCK)
            OUTER APPLY (
                SELECT TOP 1 D0.DeposId, D0.DeposNum, D0.DeposDate, D0.BanckAcct
                FROM DPS1 L0 WITH (NOLOCK)
                INNER JOIN ODPS D0 WITH (NOLOCK) ON D0.DeposId = L0.DepositId
                WHERE L0.CheckKey = H.CheckKey AND D0.ChkType = 'S' AND ISNULL(L0.DepCancel, 'N') <> 'Y'
                ORDER BY CASE WHEN D0.Canceled = 'N' THEN 0 ELSE 1 END, D0.DeposId DESC
            ) D
            LEFT JOIN OACT A WITH (NOLOCK) ON A.AcctCode = D.BanckAcct
            LEFT JOIN OCRD R WITH (NOLOCK) ON R.CardCode = D.BanckAcct
            LEFT JOIN (SELECT BankCode, MAX(BankName) AS BankName FROM ODSC WITH (NOLOCK) GROUP BY BankCode) B ON B.BankCode = H.BankCode
            WHERE H.Deposited = 'S' AND H.Canceled = 'N'
            ORDER BY H.CheckDate, H.CheckNum";



        private static readonly string _tamamlananSql = @"
            SELECT P.DeposId, P.DeposDate, P.BanckAcct, A.AcctName AS BanckAcctName, P.LocTotal, P.DeposCurr, P.Memo, P.TransAbs,
                   J.Account AS KaynakHesap, J.Ref3Line AS CheckNum,
                   H.CheckKey, H.CheckDate, H.CardCode, H.CardName,
                   CASE WHEN EXISTS (SELECT 1 FROM OJDT S WITH (NOLOCK) WHERE S.StornoToTr = P.TransAbs) THEN 1 ELSE 0 END AS Iptal
            FROM ODPT P WITH (NOLOCK)
            LEFT JOIN OACT A WITH (NOLOCK) ON A.AcctCode = P.BanckAcct
            OUTER APPLY (
                SELECT TOP 1 J0.Account, J0.Ref3Line FROM JDT1 J0 WITH (NOLOCK)
                WHERE J0.TransId = P.TransAbs AND J0.Credit > 0 ORDER BY J0.Line_ID
            ) J
            OUTER APPLY (
                SELECT TOP 1 H0.CheckKey, H0.CheckDate, H0.CardCode, H0.CardName FROM OCHH H0 WITH (NOLOCK)
                WHERE CAST(H0.CheckNum AS NVARCHAR(50)) = J.Ref3Line
                ORDER BY CASE WHEN H0.Deposited = 'C' THEN 0 ELSE 1 END, H0.CheckKey DESC
            ) H
            WHERE P.DeposDate BETWEEN @Baslangic AND @Bitis
            ORDER BY P.DeposDate DESC, P.DeposId DESC";


        [HttpGet("GetBekleyenler")]
        public IActionResult GetBekleyenler()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor149Controller.BekleyenCekModel>(12) });
}

        [HttpGet("GetTamamlananlar")]
        public IActionResult GetTamamlananlar(DateTime? baslangic, DateTime? bitis)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor149Controller.TamamlananModel>(12) });
}



        [HttpGet("HesapAra")]
        public IActionResult HesapAra(string q)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "AcctCode", "AcctName", "ActCurr" }));
}

        [HttpGet("GetAktarTipleri")]
        public IActionResult GetAktarTipleri()
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "FldValue", "Descr" }));
}

        public class VadeliIbrazIstek
        {
            public List<int> CheckKeys { get; set; } = new List<int>();
            public string BankaHesabi { get; set; }
            public DateTime Tarih { get; set; }
            public string Aciklama { get; set; }
            public string U_BE1_AKTAR { get; set; }
        }

        [HttpPost("VadeliIbrazYap")]
        public async Task<IActionResult> VadeliIbrazYap([FromBody] VadeliIbrazIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docNums = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<int>("docNums", i2)).ToList(), basarili = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("basarili", i2)).ToList(), hatali = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatali", i2)).ToList() });
}

        private static string SapHataMetni(string ham)
 {return default;
}


        public class BekleyenCekModel
        {
            public int CheckKey { get; set; }
            public string CheckNum { get; set; }
            public DateTime? CheckDate { get; set; }
            public decimal CheckSum { get; set; }
            public string Currency { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string BankName { get; set; }
            public string Branch { get; set; }
            public int? RcptNum { get; set; }
            public int? DeposId { get; set; }
            public int? DeposNum { get; set; }
            public DateTime? DeposDate { get; set; }
            public string BanckAcct { get; set; }
            public string BanckAcctName { get; set; }
        }

        public class TamamlananModel
        {
            public int DeposId { get; set; }
            public DateTime? DeposDate { get; set; }
            public string BanckAcct { get; set; }
            public string BanckAcctName { get; set; }
            public decimal LocTotal { get; set; }
            public string DeposCurr { get; set; }
            public string Memo { get; set; }
            public int? TransAbs { get; set; }
            public string KaynakHesap { get; set; }
            public string CheckNum { get; set; }
            public DateTime? CheckDate { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public bool Iptal { get; set; }
        }

        public class BordroCekDetay
        {
            public int CheckKey { get; set; }
            public string CekNo { get; set; }
            public DateTime Tarih { get; set; }
            public string Banka { get; set; }
            public string Sube { get; set; }
            public string Musteri { get; set; }
            public string AsilBorclu { get; set; }
            public decimal CekTutari { get; set; }
            public string ParaBirimi { get; set; }
        }

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class UdfValidValue { public string Code { get; set; } public string Description { get; set; } }
        public class AccountBPModel { public string Code { get; set; } public string Name { get; set; } public string Type { get; set; } }

        public class AvailableCheckModel
        {
            public int CheckKey { get; set; }
            public string DueDate { get; set; }
            public string CheckNum { get; set; }
            public string BankCode { get; set; }
            public string BankName { get; set; }
            public string Branch { get; set; }
            public string AcctNum { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public decimal CheckSum { get; set; }
            public string Project { get; set; }
        }

        public class DepositSearchModel
        {
            public int? DeposNum { get; set; }
            public string BanckAcct { get; set; }
            public string DeposDate { get; set; }
        }

        public class DepositSearchResultModel
        {
            public int DeposId { get; set; }
            public int DeposNum { get; set; }
            public string BanckAcct { get; set; }
            public string BanckAcctName { get; set; }
            public string DeposDate { get; set; }
            public decimal LocTotal { get; set; }
            public string Memo { get; set; }
        }

        public class DepositDetailViewModel
        {
            public int DeposId { get; set; }
            public int DeposNum { get; set; }
            public string DeposType { get; set; }
            public string ChkType { get; set; }
            public string DeposDate { get; set; }
            public string BanckAcct { get; set; }
            public string BanckAcctName { get; set; }
            public string DpsBank { get; set; }
            public string BpActType { get; set; }
            public string Memo { get; set; }
            public bool Canceled { get; set; }
            public decimal LocTotal { get; set; }
            public int TransAbs { get; set; }

            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_IADE { get; set; }
            public string U_BE1_CEKISLEMTUR { get; set; }

            public List<AvailableCheckModel> Checks { get; set; } = new List<AvailableCheckModel>();
        }

        public class DepositCreationModel
        {
            public string DepositCurrency { get; set; }
            public string AccountType { get; set; }
            public string DepositAccount { get; set; }
            public DateTime DepositDate { get; set; }
            public string BnkCode { get; set; }
            public string BnkBranch { get; set; }
            public string BnkAccount { get; set; }
            public string BankReference { get; set; }
            public string Depositor { get; set; }
            public string JournalRemarks { get; set; }
            public bool SplitChecks { get; set; }
            public string CheckDepositType { get; set; }

            public bool PerformReconciliation { get; set; }

            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_IADE { get; set; }
            public string U_BE1_CEKISLEMTUR { get; set; }

            public List<int> CheckKeys { get; set; } = new List<int>();
        }

        public class BulkCancelDepositRequest
        {
            public List<int> DeposIds { get; set; }
        }

        public class BulkFixDepositRequest
        {
            public List<int> DeposIds { get; set; }
            public string TargetDate { get; set; }
        }
    }
}
