// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Controllers
{

    public class KdvReportViewModel
    {
        public string DbCode { get; set; }
        public string SirketUnvani { get; set; }
        public decimal DevredenKDV_190 { get; set; }
        public decimal IndirilecekKDV_191 { get; set; }
        public decimal HesaplananKDV_391 { get; set; }
        public decimal KdvDurumu { get; set; }
    }

    public class SubAccountViewModel
    {
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal Balance { get; set; }
        public string DbCode { get; set; }
    }

    public class TransactionViewModel
    {
        public int TransId { get; set; }
        public DateTime RefDate { get; set; }
        public string LineMemo { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal NetTutar { get; set; }
        public string IslemTipi { get; set; } // Ekranda göstermek isterseniz diye ekledim
    }

    [Authorize]
    [Route("[controller]")]
    public class Rapor67Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor67Controller> _logger;

        private readonly Dictionary<string, string> _dbMap = new Dictionary<string, string>
        {
            {"ALV_KIMYA", "ALV_KIMYA"}, {"ALVFILO", "ALVFILO"}, {"ASIA_KIMYA", "ASIA_KIMYA"},
            {"AVRASYA", "AVRASYA"}, {"AVRUPA_PAPER", "AVRUPA_PAPER"}, {"DAF_KIMYA", "DAF_KIMYA"},
            {"SELVI", "SELVI"}, {"URAS_HOLDING", "URAS_HOLDING"}, {"URASKIMYA", "URASKIMYA"},
            {"URSMAKINE", "URSMAKINE"}
        };


        private string BuildTransactionTypeFilter(List<string> types)
 {return default;
}


        [HttpGet]
        public IActionResult Index(DateTime? startDate, DateTime? endDate, List<string> transactionTypes)
 {ViewBag.SelectedTypes = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.KdvReportViewModel>(12));
}


        [HttpGet("GetSubAccounts")]
        public IActionResult GetSubAccounts(string dbCode, string mainAccountPattern, DateTime startDate, DateTime endDate, List<string> transactionTypes)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return PartialView("_SubAccountList", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.SubAccountViewModel>(12));
}


        [HttpGet("GetTransactions")]
        public IActionResult GetTransactions(string dbCode, string exactAccountCode, DateTime startDate, DateTime endDate, List<string> transactionTypes)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return PartialView("_TransactionList", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.TransactionViewModel>(12));
}
    }
}