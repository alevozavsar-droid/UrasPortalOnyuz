// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{




    public class FinekraCompanyConfig
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string TenantCode { get; set; }
    }

    public class FinekraLoginRequest
    {
        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("password")]
        public string Password { get; set; }

        [JsonPropertyName("tenantCode")]
        public string TenantCode { get; set; }

        [JsonPropertyName("screenOption")]
        public int ScreenOption { get; set; } = 0;
    }

    public class FinekraLoginResponse
    {
        [JsonPropertyName("token")]
        public string Token { get; set; }

        [JsonPropertyName("expiration")]
        public DateTime Expiration { get; set; }
    }

    public class BankAccountModel
    {
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public string Currency { get; set; }
    }





    public class FinekraHesapEslesme
    {
        public string FinekraHesapId { get; set; }
        public string Iban { get; set; }
        public string SapHesapKodu { get; set; }
        public string SapHesapAdi { get; set; }
    }

    public class FinekraHesapBakiyeModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("branchName")]
        public string BranchName { get; set; }

        [JsonPropertyName("iban")]
        public string Iban { get; set; }

        [JsonPropertyName("currency")]
        public string ParaBirimi { get; set; }

        [JsonPropertyName("balance")]
        public decimal Bakiye { get; set; }

        [JsonPropertyName("bankName")]
        public string BankaAdi { get; set; }

        [JsonIgnore]
        public string HesapAdi => !string.IsNullOrWhiteSpace(Name) ? Name : (!string.IsNullOrWhiteSpace(BranchName) ? BranchName : "Tanımsız Hesap");
    }

    public class BankaHareketiViewModel
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("transactionDate")]
        public string TransactionDateRaw { get; set; }

        [JsonIgnore]
        public DateTime? IslemTarihi
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TransactionDateRaw)) return null;

                if (DateTime.TryParse(TransactionDateRaw, new CultureInfo("tr-TR"), DateTimeStyles.None, out DateTime trDt))
                    return trDt;

                if (DateTime.TryParse(TransactionDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
                    return dt;

                return null;
            }
        }

        [JsonPropertyName("bankName")]
        public string BankaAdi { get; set; }

        [JsonPropertyName("tenantIban")]
        public string Iban { get; set; }


        [JsonPropertyName("tenantAccountId")]
        public string TenantAccountId { get; set; }

        [JsonPropertyName("transactionProcessTypeString")]
        public string IslemTipi { get; set; }

        [JsonPropertyName("transactionTypeName")]
        public string IslemTuruRaw { get; set; }

        [JsonIgnore]
        public string IslemTuru
        {
            get
            {
                if (!string.IsNullOrEmpty(IslemTuruRaw) && IslemTuruRaw.Equals("Faiz", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(Aciklama) && Aciklama.ToUpper(new CultureInfo("tr-TR")).Contains("HAVALE"))
                    {
                        return "Havale";
                    }
                }
                return IslemTuruRaw;
            }
        }

        [JsonPropertyName("transactionCode")]
        public string IslemKodu { get; set; }

        [JsonPropertyName("description")]
        public string Aciklama { get; set; }

        [JsonPropertyName("amount")]
        public decimal Tutar { get; set; }

        [JsonPropertyName("amountForExcel")]
        public decimal TutarExcel { get; set; }

        [JsonPropertyName("amountString")]
        public string TutarString { get; set; }

        [JsonPropertyName("debtOrCredit")]
        public string BorcAlacakDurumu { get; set; }

        [JsonPropertyName("transactionProcessTypeValue")]
        public int? IslemYonuDegeri { get; set; }





        [JsonPropertyName("balanceAfterTransaction")]
        public decimal? BakiyeHam { get; set; }

        [JsonIgnore]
        public decimal Bakiye => BakiyeHam ?? 0m;

        [JsonPropertyName("senderName")]
        public string GonderenAdi { get; set; }

        [JsonPropertyName("firmName")]
        public string FirmaAdi { get; set; }

        [JsonPropertyName("vkn")]
        public string Vkn { get; set; }

        [JsonPropertyName("tckn")]
        public string Tckn { get; set; }

        [JsonPropertyName("senderVkn")]
        public string SenderVkn { get; set; }

        [JsonPropertyName("senderTckn")]
        public string SenderTckn { get; set; }

        [JsonPropertyName("receiverVkn")]
        public string ReceiverVkn { get; set; }

        [JsonPropertyName("receiverTckn")]
        public string ReceiverTckn { get; set; }

        [JsonPropertyName("firmVkn")]
        public string FirmVkn { get; set; }

        [JsonPropertyName("firmTckn")]
        public string FirmTckn { get; set; }

        [JsonPropertyName("currency")]
        public string ParaBirimi { get; set; }

        [JsonPropertyName("currencyValue")]
        public string DovizKuruStr { get; set; }

        [JsonIgnore]
        public decimal TutarTRY { get; set; }

        [JsonIgnore]
        public decimal? AciklamadanGelenKur { get; set; }

        [JsonIgnore]
        public decimal SapSistemKuru { get; set; }

        [JsonIgnore]
        public string FinekraCariIsmi
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(GonderenAdi)) return GonderenAdi;
                if (!string.IsNullOrWhiteSpace(FirmaAdi)) return FirmaAdi;
                return "Açıklamada Belirtilmiş";
            }
        }

        [JsonIgnore]
        public string EkrandaGorunenVkn { get; set; }

        [JsonIgnore]
        public string EkrandaGorunenVknKaynagi { get; set; }

        [JsonIgnore]
        public bool SapAktarildiMi { get; set; }

        [JsonIgnore]
        public string SapBelgeTuru { get; set; }

        [JsonIgnore]
        public string SapBelgeNo { get; set; }

        [JsonIgnore]
        public int? SapTransId { get; set; } // YENİ: İptal İşlemi İçin TransId

        [JsonIgnore]
        public string SapCariKodu { get; set; }

        [JsonIgnore]
        public string SapCariAdi { get; set; }

        [JsonIgnore]
        public string OnerilenBankaKodu { get; set; }

        [JsonIgnore]
        public string OnerilenBankaAdi { get; set; }


        [JsonIgnore]
        public string OnerilenBankaKaynagi { get; set; }

        [JsonIgnore]
        public bool IsGiris
        {
            get
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(BorcAlacakDurumu))
                    {
                        string flag = BorcAlacakDurumu.Trim().ToUpperInvariant();
                        if (flag == "B" || flag == "D") return false;
                        if (flag == "A" || flag == "C") return true;
                    }

                    if (TutarExcel < 0) return false;
                    if (!string.IsNullOrWhiteSpace(TutarString) && TutarString.Trim().StartsWith("-")) return false;
                    if (Tutar < 0) return false;

                    if (IslemYonuDegeri.HasValue)
                    {
                        if (IslemYonuDegeri.Value == 2) return false;
                        if (IslemYonuDegeri.Value == 1) return true;
                    }
                }
                catch { }

                return true;
            }
        }
    }

    public class SapYevmiyeSatir
    {
        public string Tipi { get; set; }
        public string Kod { get; set; }
        public decimal Tutar { get; set; }
    }

    public class SapAktarimRequest
    {
        public string Id { get; set; }
        public string BelgeTuru { get; set; }
        public string IslemTipi { get; set; }

        public decimal UygulanacakKur { get; set; }


        public bool VknGuncelle { get; set; }
        public string FinekraVkn { get; set; }

        public string KarsiTarafTipi { get; set; }
        public string CariKodu { get; set; }
        public string KarsiHesapKodu { get; set; }

        public List<SapYevmiyeSatir> YevmiyeSatirlari { get; set; }

        public decimal Tutar { get; set; }
        public DateTime IslemTarihi { get; set; }
        public string Aciklama { get; set; }
        public string BankaHesapKodu { get; set; }
        public string ParaBirimi { get; set; }
        public bool IsKkeg { get; set; }
        public int KdvRate { get; set; }
        public string Gider70Hesap { get; set; }
        public string Kkeg30Hesap { get; set; }
        public string Kdv70Hesap { get; set; }
        public string KkegKdv30Hesap { get; set; }
    }


    public class SapIptalRequest
    {
        public string BelgeTuru { get; set; } // Gelen Ödeme, Yapılan Ödeme, Yevmiye Fişi
        public string BelgeNo { get; set; }
        public int TransId { get; set; }
    }


    public class HataliVadeliIslemModel
    {
        public int TransId { get; set; }
        public string BelgeNo { get; set; }
        public string BelgeTuru { get; set; }
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
        public string KayitTarihi { get; set; }
        public string Aciklama { get; set; }
    }





    [Authorize]
    [Route("[controller]")]
    public class Rapor105Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _finekraBaseUrl = "https://polynom-api.finekra.com/";

        private string GetSelectedDatabase()
 {return default;
}

        private FinekraCompanyConfig GetCompanyConfig()
 {return default;
}

        private bool IsValidVknTckn(string vkn)
 {return default;
}

        private string ExtractVknFromText(string text)
 {return default;
}

        private (string CardCode, string CardName) FindSapCari(string connectionString, string vknTckn, string unvan, string aciklama)
 {return default;
}

        private string GetSapCariType(string connectionString, string cardCode)
 {return default;
}

        [HttpGet("CheckDuplicate")]
        public IActionResult CheckDuplicate(string finekraId, string cariKodu, string dateStr, decimal amount, bool isGiris)
 {return Json(new { isProcessed = true, belgeTuru = global::WebApplication3.OrnekDoldurucu.Deger<string>("belgeTuru", 0), belgeNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("belgeNo", 0) });
}


        private (bool Success, string BelgeTuru, string BelgeNo, decimal SapTlTutar, int TransId, int LineId) GetSapDocumentDetailsSmart(
                string connectionString, DateTime? date, decimal amount, bool isGiris, string finekraId, List<string> assignedSapLines)
 {return default;
}
        private async Task<(string Token, string ErrorMessage)> GetFinekraTokenAsync(FinekraCompanyConfig config)
 {return default;
}

        private async Task<List<FinekraHesapBakiyeModel>> GetFinekraHesapBakiyeleriAsync(string token)
 {return default;
}

        private async Task<(List<BankaHareketiViewModel> Data, string RawResponse)> GetBankaHareketleriAsync(string token, DateTime startDate, DateTime endDate)
 {return default;
}








        private string MerkezBaglantisi()  {return default;
}

        private static string IbanAnahtari(string iban)
 {return default;
}


        private static string HesapIdAnahtari(string hesapId)  {return default;
}


        private static string EslesmeAnahtari(string iban, string hesapId)  {return default;
}


        private Dictionary<string, FinekraHesapEslesme> EslesmeleriOku(string dbKey)
 {return default;
}




        [HttpPost("HesapEslesmeKaydet")]
        public IActionResult HesapEslesmeKaydet(string finekraHesapId, string iban, string bankaAdi, string hesapAdi, string sapHesapKodu)
 {return Json(new { success = true, sapHesapKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("sapHesapKodu", 0), sapHesapAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("sapHesapAdi", 0) });
}

        private List<BankAccountModel> GetSapBankAccounts(string connectionString)
 {return default;
}

        private List<BankAccountModel> GetAllSapAccounts(string connectionString)
 {return default;
}

        private decimal GetSapExchangeRate(string connectionString, string currency, DateTime date)
 {return default;
}

        [HttpGet("AraCari")]
        public IActionResult AraCari(string q)
 {return Json(new { results = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName" }) });
}

        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
 {ViewBag.SapBankAccounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.BankAccountModel>(12);
ViewBag.AllSapAccounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.BankAccountModel>(12);
ViewBag.HesapEslesmeleri = WebApplication3.OrnekDoldurucu.Yeni<System.Collections.Generic.Dictionary<string, WebApplication3.Controllers.FinekraHesapEslesme>>();
ViewBag.HesapBakiyeleri = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.FinekraHesapBakiyeModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.BankaHareketiViewModel>(12));
}

        [HttpGet("DekontIndir")]
        public async Task<IActionResult> DekontIndir(string id)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("downloadexcel")]
        public IActionResult DownloadExcel(DateTime? startDate, DateTime? endDate)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}




        [HttpGet("GetHatalivadeliIslemler")]
        public IActionResult GetHatalivadeliIslemler()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.HataliVadeliIslemModel>(12) });
}





        [HttpPost("IptalEtSap")]
        public async Task<IActionResult> IptalEtSap([FromBody] List<SapIptalRequest> requests)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("AktarSap")]
        public async Task<IActionResult> AktarSap([FromBody] SapAktarimRequest request)
 {return Json(new { success = true, message = "Belge SAP'ye başarıyla aktarıldı!", docNum = global::WebApplication3.OrnekDoldurucu.Deger<string>("docNum", 0) });
}
    }
}