// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{



    public class ManuelTahsilatRequest122
    {
        public string CariKodu { get; set; }
        public decimal Tutar { get; set; }
        public string BankaHesapKodu { get; set; }
        public string IslemTipi { get; set; }
        public string Aciklama { get; set; }
        public DateTime IslemTarihi { get; set; }
        public string OdemeTipi { get; set; }
    }

    public class SapCariModel122
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardFName { get; set; }
        public string Vkn1 { get; set; }
        public string Vkn2 { get; set; }
    }

    public class SapAktarimRequest122
    {
        public string Id { get; set; }
        public string BelgeTuru { get; set; }
        public string IslemTipi { get; set; }
        public string KarsiTarafTipi { get; set; }
        public string CariKodu { get; set; }
        public string KarsiHesapKodu { get; set; }
        public decimal Tutar { get; set; }
        public DateTime IslemTarihi { get; set; }
        public string Aciklama { get; set; }
        public string BankaHesapKodu { get; set; }

        public bool IsKkeg { get; set; }
        public int KdvRate { get; set; }
        public string Gider70Hesap { get; set; }
        public string Kkeg30Hesap { get; set; }
        public string Kdv70Hesap { get; set; }
        public string KkegKdv30Hesap { get; set; }
    }

    [Authorize]
    [Route("[controller]")]
    public class Rapor122Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _finekraBaseUrl = "https://polynom-api.finekra.com/";

        private string GetSelectedDatabase()
 {return default;
}




        private (string Email, string Password, string TenantCode, string CompanyDb) GetCompanySettings(string dbKey)
 {return default;
}
        private async Task<(string Token, string ErrorMessage)> GetFinekraTokenAsync(string email, string password, string tenantCode)
 {return default;
}

        private bool IsValidVknTckn(string vkn)
 {return default;
}

        private string ExtractVknFromText(string text)
 {return default;
}

        private List<SapCariModel122> GetTumSapCariler(string connectionString)
 {return default;
}

        private string NormalizeText(string text)
 {return default;
}

        private (string CardCode, string CardName) FindSapCariSmart(List<SapCariModel122> cariler, string vkn, string unvan, string aciklama)
 {return default;
}

        private (bool Success, string BelgeTuru, string BelgeNo, decimal SapTlTutar, int TransId) GetSapDocumentDetailsSmart(
            string connectionString, DateTime? date, decimal amount, bool isGiris, string finekraId, List<int> assignedTransIds)
 {return default;
}

        private async Task<(List<BankaHareketiViewModel> Data, string RawResponse)> GetBankaHareketleriAsync(string token, DateTime startDate, DateTime endDate)
 {return default;
}

        private List<BankAccountModel> GetSapBankAccounts(string connectionString)
 {return default;
}

        private List<BankAccountModel> GetSapCashAccounts(string connectionString)
 {return default;
}

        private List<BankAccountModel> GetAllSapAccounts(string connectionString)
 {return default;
}

        private decimal GetSapExchangeRate(string connectionString, string currency, DateTime date)
 {return default;
}

        private List<BankaHareketiViewModel> GetSapNakitTahsilatlar(string connectionString, DateTime startDate, DateTime endDate)
 {return default;
}




        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
 {ViewBag.SapCashAccounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.BankAccountModel>(12);
ViewBag.SapBankAccounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.BankAccountModel>(12);
ViewBag.AllSapAccounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.BankAccountModel>(12);
ViewBag.TumCariler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.SapCariModel122>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.BankaHareketiViewModel>(12));
}

        [HttpGet("DekontIndir")]
        public async Task<IActionResult> DekontIndir(string id)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpPost("AktarSapManuel")]
        public async Task<IActionResult> AktarSapManuel([FromBody] ManuelTahsilatRequest122 request)
 {return Json(new { success = true, message = "Tahsilat SAP'ye başarıyla aktarıldı!", docNum = global::WebApplication3.OrnekDoldurucu.Deger<string>("docNum", 0) });
}

        [HttpPost("AktarSap")]
        public async Task<IActionResult> AktarSap([FromBody] SapAktarimRequest122 request)
 {return Json(new { success = true, message = "Belge SAP'ye başarıyla aktarıldı!", docNum = global::WebApplication3.OrnekDoldurucu.Deger<string>("docNum", 0) });
}
    }
}