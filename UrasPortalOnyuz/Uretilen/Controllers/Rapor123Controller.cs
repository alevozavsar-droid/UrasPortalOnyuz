// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Primitives;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Text;
using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor123Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;


        private readonly string _finekraBaseUrl = "https://polynom-api.finekra.com/";

        private readonly List<R123_DatabaseConfig> _databases = new List<R123_DatabaseConfig>
        {
            new R123_DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new R123_DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new R123_DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new R123_DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new R123_DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new R123_DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new R123_DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new R123_DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new R123_DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
            new R123_DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new R123_DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new R123_DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya" },
            new R123_DatabaseConfig { Key = "DefaultConnection7", Display = "Avrasya" },
            new R123_DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },
            new R123_DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new R123_DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new R123_DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new R123_DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new R123_DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new R123_DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new R123_DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new R123_DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new R123_DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new R123_DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new R123_DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
            new R123_DatabaseConfig { Key = "DefaultConnection37", Display = "RGB_TEKSTIL" }
        };




        private bool IsUserAuthorized()
 {return default;
}

        private string GetSelectedDatabase()
 {return default;
}

        private R123_FinekraCompanyConfig GetCompanyConfig(string selectedDbKey)
 {return default;
}

        private List<R123_DatabaseConfig> GetKonsolideDatabases()
 {return default;
}




        private bool IsValidVknTckn(string vkn)
 {return default;
}

        private string ExtractVknFromText(string text)
 {return default;
}

        private string CleanVkn(string val)
 {return default;
}

        private string NormalizeForMatch(string text)
 {return default;
}

        private List<string> GetMatchKeywords(string text)
 {return default;
}

        private (string CardCode, string CardName) FindSapCari(string connectionString, string vknTckn, string unvan, string aciklama)
 {return default;
}

        private List<R123_BankAccountModel> GetSapBankAccounts(string connectionString)
 {return default;
}

        [HttpGet("DekontIndir/{id}")]
        public async Task<IActionResult> DekontIndir(string id, [FromQuery] string dbKey)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private List<R123_BankAccountModel> GetAllSapAccounts(string connectionString)
 {return default;
}

        private decimal GetSapExchangeRate(string connectionString, string currency, DateTime date)
 {return default;
}

        private (bool Success, string BelgeTuru, string BelgeNo, decimal SapTlTutar, int TransId) GetSapDocumentDetailsSmart(
            string connectionString, DateTime? date, decimal amount, bool isGiris, string finekraId, List<int> assignedTransIds)
 {return default;
}




        private async Task<List<R123_NakitAkisViewModel>> GetKonsolideNakitAkisVeEslestirAsync(List<R123_DatabaseConfig> targetDbs, DateTime startDate, DateTime endDate)
 {return default;
}

        private List<R123_AccountBPModel> GetKonsolideAccountsAndBPs(List<R123_DatabaseConfig> targetDbs)
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}





        [HttpGet]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, string hesapTipi = "KASA", string tab = "TUMU", string dbFilter = "TUMU")
 {ViewBag.IsAuthorized = false;
ViewBag.CekOzet = WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.R123_CekOzetModel>();
ViewBag.KasaOzet = WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.R123_KasaOzetModel>();
ViewBag.BankaOzet = WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.R123_BankaOzetModel>();
ViewBag.FinekraHareketler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_BankaHareketiViewModel>(12);
ViewBag.KasaBakiyeleri = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_KasaBakiyeModel>(12);
ViewBag.BankaBakiyeleri = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_BankaBakiyeModel>(12);
ViewBag.EmailList = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.KonsolideSirketler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_DatabaseConfig>(12);
ViewBag.KonsolideBakiyeler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_KonsolideBakiye>(12);
ViewBag.SapBankAccounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_BankAccountModel>(12);
ViewBag.AllSapAccounts = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_BankAccountModel>(12);
ViewBag.NakitAkisListesi = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_NakitAkisViewModel>(12);
ViewBag.AccountsAndBPs = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_AccountBPModel>(12);
ViewBag.KasaListesi = new System.Collections.Generic.List<object>();
ViewBag.CariListesi = new System.Collections.Generic.List<object>();
ViewBag.HesapListesi = new System.Collections.Generic.List<object>();
ViewBag.FinekraHata = false;
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.R123_HareketViewModel>(12));
}

        private string NormalizeBankName(string name)
 {return default;
}

        private string NormalizeCurrency(string currency)
 {return default;
}

        private List<R123_BankaBakiyeModel> GetBankaHesapBakiyeleriEski(List<R123_DatabaseConfig> databases, DateTime endDate, string tab)
 {return default;
}

        private async Task<(string Token, string ErrorMessage)> GetFinekraTokenAsync(R123_FinekraCompanyConfig config)
 {return default;
}

        private async Task<(List<R123_BankaHareketiViewModel> Data, string RawResponse)> GetBankaHareketleriAsync(string token, DateTime startDate, DateTime endDate)
 {return default;
}

        private async Task<List<R123_FinekraAccount>> GetFinekraAccountsAsync(string token)
 {return default;
}

        private void EsitLEOnayDurumlari(List<R123_HareketViewModel> hareketler, string merkezConnStr)
 {}

        private List<R123_HareketViewModel> GetManuelEksikler(string merkezConnStr, List<R123_DatabaseConfig> targetDbs, DateTime startDate, DateTime endDate, string hesapTipi, string tab)
 {return default;
}

        [HttpPost("SetOnayDurumu")]
        public async Task<IActionResult> SetOnayDurumu([FromBody] R123_OnayPostModel model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SilManuelKayit")]
        public async Task<IActionResult> SilManuelKayit([FromBody] R123_OnayPostModel model)
 {return Json(new { success = true, message = "Kayıt başarıyla kaldırıldı." });
}

        [HttpPost("CreateYevmiye")]
        public async Task<IActionResult> CreateYevmiye([FromForm] R123_YevmiyePostModel model)
 {return RedirectToAction("Index");
}

        [HttpGet("GenerateDekont/{dbKey}/{transId}")]
        public IActionResult GenerateDekont(string dbKey, int transId, string type)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return PartialView("DekontView", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.R123_DekontViewModel>());
}

        private R123_CekOzetModel GetCekOzet(List<R123_DatabaseConfig> databases, DateTime startDate, DateTime endDate, string tab, out string errorMsg)
 {errorMsg = default;
return default;
}

        private R123_KasaOzetModel GetKasaOzet(List<R123_DatabaseConfig> databases, DateTime startDate, DateTime endDate, string tab, out string errorMsg)
 {errorMsg = default;
return default;
}

        private R123_BankaOzetModel GetBankaOzet(List<R123_DatabaseConfig> databases, DateTime startDate, DateTime endDate, string tab, out string errorMsg)
 {errorMsg = default;
return default;
}

        private List<R123_KonsolideBakiye> GetKonsolideBakiyeler(List<R123_DatabaseConfig> databases, DateTime startDate, DateTime endDate, out string errorMsg)
 {errorMsg = default;
return default;
}

        private List<R123_HareketViewModel> GetFinansalHareketler(List<R123_DatabaseConfig> databases, DateTime startDate, DateTime endDate, string hesapTipi, string tab, out string errorMsg)
 {errorMsg = default;
return default;
}

        private R123_DekontViewModel GetDekontDetails(string connectionString, int transId, out string errorMsg)
 {errorMsg = default;
return default;
}

        private List<R123_CariViewModel> GetCariList(string connectionString, out string errorMsg)
 {errorMsg = default;
return default;
}

        private List<R123_HesapViewModel> GetHesapListesi(string connectionString, string prefix, out string errorMsg)
 {errorMsg = default;
return default;
}

        [HttpPost("AktarSap")]
        public async Task<IActionResult> AktarSap([FromBody] R123_SapAktarimRequest request)
 {return Json(new { success = true, message = "Belge SAP'ye başarıyla aktarıldı!", docNum = global::WebApplication3.OrnekDoldurucu.Deger<string>("docNum", 0) });
}




        [HttpPost("CreateVirmanJournalEntry")]
        public async Task<IActionResult> CreateVirmanJournalEntry([FromBody] R123_VirmanCreationRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        [HttpPost("EksikBildirimKaydet")]
        public async Task<IActionResult> EksikBildirimKaydet([FromBody] EksikBildirimRequestModel model)
 {return Json(new { success = true, message = "Bildirim başarıyla kaydedildi." });
}

        [HttpGet("EksikBildirimleriGetir")]
        public async Task<IActionResult> EksikBildirimleriGetir(string bolum, string sirketDb)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.EksikBildirimViewModel>(12) });
}

        [HttpPost("EksikBildirimTamamla/{id}")]
        public async Task<IActionResult> EksikBildirimTamamla(int id)
 {return Json(new { success = true, message = "Bildirim tamamlandı olarak işaretlendi ve kayıtlardan düşürüldü." });
}

    }




    public class R123_FinekraCompanyConfig
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string TenantCode { get; set; }
    }

    public class R123_FinekraLoginRequest
    {
        [JsonPropertyName("email")] public string Email { get; set; }
        [JsonPropertyName("password")] public string Password { get; set; }
        [JsonPropertyName("tenantCode")] public string TenantCode { get; set; }
        [JsonPropertyName("screenOption")] public int ScreenOption { get; set; } = 0;
    }

    public class R123_FinekraAccount
    {
        [JsonPropertyName("bankName")] public string BankName { get; set; }
        [JsonPropertyName("balance")] public decimal Balance { get; set; }
        [JsonPropertyName("currency")] public string Currency { get; set; }
        [JsonPropertyName("updateDate")] public DateTime? UpdateDate { get; set; }
        [JsonPropertyName("lastTransactionDate")] public DateTime? LastTransactionDate { get; set; }
    }

    public class R123_BankaKarsilastirmaModel
    {
        public string DbKey { get; set; }
        public string Sirket { get; set; }
        public string BankaAdi { get; set; }
        public string ParaBirimi { get; set; }
        public decimal SapBakiyeTL { get; set; }
        public decimal FinekraBakiyeTL { get; set; }
        public decimal FarkTL => FinekraBakiyeTL - SapBakiyeTL;
        public string SonGuncelleme { get; set; }
    }

    public class R123_BankAccountModel
    {
        public string DbKey { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public string Currency { get; set; }
    }

    public class R123_BankaHareketiViewModel
    {
        [JsonIgnore] public string DbKey { get; set; }
        [JsonIgnore] public string Sirket { get; set; }

        [JsonPropertyName("id")] public string Id { get; set; }
        [JsonPropertyName("transactionDate")] public string TransactionDateRaw { get; set; }

        [JsonIgnore]
        public DateTime? IslemTarihi
        {
            get
            {
                if (string.IsNullOrWhiteSpace(TransactionDateRaw)) return null;
                if (DateTime.TryParse(TransactionDateRaw, new CultureInfo("tr-TR"), DateTimeStyles.None, out DateTime trDt)) return trDt;
                if (DateTime.TryParse(TransactionDateRaw, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt)) return dt;
                return null;
            }
        }

        [JsonPropertyName("bankName")] public string BankaAdi { get; set; }
        [JsonPropertyName("tenantIban")] public string Iban { get; set; }
        [JsonPropertyName("transactionProcessTypeString")] public string IslemTipi { get; set; }
        [JsonPropertyName("transactionTypeName")] public string IslemTuruRaw { get; set; }

        [JsonIgnore]
        public string IslemTuru
        {
            get
            {
                if (!string.IsNullOrEmpty(IslemTuruRaw) && IslemTuruRaw.Equals("Faiz", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrEmpty(Aciklama) && Aciklama.ToUpper(new CultureInfo("tr-TR")).Contains("HAVALE"))
                        return "Havale";
                }
                return IslemTuruRaw;
            }
        }

        [JsonPropertyName("transactionCode")] public string IslemKodu { get; set; }
        [JsonPropertyName("description")] public string Aciklama { get; set; }
        [JsonPropertyName("amount")] public decimal Tutar { get; set; }
        [JsonPropertyName("amountForExcel")] public decimal TutarExcel { get; set; }
        [JsonPropertyName("amountString")] public string TutarString { get; set; }
        [JsonPropertyName("debtOrCredit")] public string BorcAlacakDurumu { get; set; }
        [JsonPropertyName("transactionProcessTypeValue")] public int? IslemYonuDegeri { get; set; }
        [JsonPropertyName("balance")] public decimal Bakiye { get; set; }

        [JsonPropertyName("senderName")] public string GonderenAdi { get; set; }
        [JsonPropertyName("firmName")] public string FirmaAdi { get; set; }
        [JsonPropertyName("vkn")] public string Vkn { get; set; }
        [JsonPropertyName("tckn")] public string Tckn { get; set; }
        [JsonPropertyName("senderVkn")] public string SenderVkn { get; set; }
        [JsonPropertyName("senderTckn")] public string SenderTckn { get; set; }
        [JsonPropertyName("receiverVkn")] public string ReceiverVkn { get; set; }
        [JsonPropertyName("receiverTckn")] public string ReceiverTckn { get; set; }
        [JsonPropertyName("firmVkn")] public string FirmVkn { get; set; }
        [JsonPropertyName("firmTckn")] public string FirmTckn { get; set; }

        [JsonPropertyName("currency")] public string ParaBirimi { get; set; }
        [JsonPropertyName("currencyValue")] public string DovizKuruStr { get; set; }

        [JsonIgnore] public decimal TutarTRY { get; set; }

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

        [JsonIgnore] public string EkrandaGorunenVkn { get; set; }
        [JsonIgnore] public string EkrandaGorunenVknKaynagi { get; set; }
        [JsonIgnore] public bool SapAktarildiMi { get; set; }
        [JsonIgnore] public string SapBelgeTuru { get; set; }
        [JsonIgnore] public string SapBelgeNo { get; set; }
        [JsonIgnore] public string SapCariKodu { get; set; }
        [JsonIgnore] public string SapCariAdi { get; set; }
        [JsonIgnore] public string OnerilenBankaKodu { get; set; }
        [JsonIgnore] public string OnerilenBankaAdi { get; set; }

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

    public class EksikBildirimRequestModel
    {
        public string Bolum { get; set; }
        public string SirketDb { get; set; }
        public int IslemNo { get; set; }
        public string TabTipi { get; set; }
        public string AliciMailler { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public List<string> Maddeler { get; set; }
    }

    public class EksikBildirimViewModel
    {
        public int Id { get; set; }
        public string Bolum { get; set; }
        public string Sirket { get; set; }
        public string GonderenKullanici { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public List<string> Maddeler { get; set; }
        public string Durum { get; set; }
        public string KayitTarihi { get; set; }
        public string TamamlanmaTarihi { get; set; }
    }

    public class R123_HareketViewModel
    {
        public string DbKey { get; set; }
        public string Sirket { get; set; }
        public int IslemNo { get; set; }
        public DateTime KayitTarihi { get; set; }
        public string HesapTipi { get; set; }
        public string KasaKodu { get; set; }
        public string KasaAdi { get; set; }
        public string KarsiHesapKodu { get; set; }
        public string KarsiHesapAdi { get; set; }
        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
        public decimal Bakiye => Borc - Alacak;
        public string ParaBirimi { get; set; }
        public string Aciklama { get; set; }
        public string IslemTipi { get; set; }
        public string IslemYonu { get; set; }

        public DateTime? CekVadeTarihi { get; set; }
        public string CekNumarasi { get; set; }

        public string OnayDurumu { get; set; }
        public string NotBasligi { get; set; }
        public string NotIcerigi { get; set; }
        public string OnaylayanKullaniciKodu { get; set; }
        public string OnaylayanKullaniciAdi { get; set; }
        public DateTime? OnayTarihi { get; set; }
    }

    public class R123_BankaBakiyeModel
    {
        public string DbKey { get; set; }
        public string Sirket { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public string Currency { get; set; }
        public decimal SapTLBalance { get; set; }
        public decimal SapCurrencyBalance { get; set; }
        public decimal FinekraTLBalance { get; set; }
        public decimal FinekraCurrencyBalance { get; set; }
        public decimal FarkTL => FinekraTLBalance - SapTLBalance;
    }

    public class R123_KasaBakiyeModel
    {
        public string DbKey { get; set; }
        public string Sirket { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public string Currency { get; set; }
        public decimal TLBalance { get; set; }
        public decimal CurrencyBalance { get; set; }
    }

    public class R123_OnayDurumModel
    {
        public string DbKey { get; set; }
        public int IslemNo { get; set; }
        public string Durum { get; set; }
        public string NotBasligi { get; set; }
        public string NotIcerigi { get; set; }
        public string KullaniciKodu { get; set; }
        public string KullaniciAdi { get; set; }
        public DateTime IslemTarihi { get; set; }
    }

    public class R123_OnayPostModel
    {
        public string DbKey { get; set; }
        public int IslemNo { get; set; }
        public string HesapTipi { get; set; }
        public string TabTipi { get; set; }
        public string Durum { get; set; }
        public string NotBasligi { get; set; }
        public string NotIcerigi { get; set; }
        public string[] GonderilecekEmails { get; set; }
    }

    public class R123_KasaOzetModel
    {
        public int GirenAdet { get; set; }
        public decimal GirenTutar { get; set; }
        public int CikanAdet { get; set; }
        public decimal CikanTutar { get; set; }
    }

    public class R123_BankaOzetModel
    {
        public int GirenAdet { get; set; }
        public decimal GirenTutar { get; set; }
        public int CikanAdet { get; set; }
        public decimal CikanTutar { get; set; }
    }

    public class R123_CekOzetModel
    {
        public int GirenAdet { get; set; }
        public decimal GirenTutar { get; set; }
        public int CikanAdet { get; set; }
        public decimal CikanTutar { get; set; }
        public int TedarikciyeAdet { get; set; }
        public decimal TedarikciyeTutar { get; set; }
        public int BankayaVerilenAdet { get; set; }
        public decimal BankayaVerilenTutar { get; set; }
        public int TahsilEdilenAdet { get; set; }
        public decimal TahsilEdilenTutar { get; set; }
    }

    public class R123_YevmiyePostModel
    {
        public DateTime IslemTarihi { get; set; }
        public string KasaHesabi { get; set; }
        public string KarsiTur { get; set; }
        public string KarsiCari { get; set; }
        public string KarsiHesap { get; set; }
        public string IslemYonu { get; set; }
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; }
        public string IslemTipi { get; set; }
        public string Aciklama { get; set; }
    }

    public class R123_HesapBakiyeModel
    {
        public string DbKey { get; set; }
        public string Sirket { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public string Currency { get; set; }
        public decimal TLBalance { get; set; }
        public decimal CurrencyBalance { get; set; }
    }

    public class R123_KonsolideBakiye
    {
        public string HesapTipi { get; set; }
        public string IslemTipi { get; set; }
        public decimal ToplamGiris { get; set; }
        public decimal ToplamCikis { get; set; }
        public decimal Bakiye => ToplamGiris - ToplamCikis;
    }

    public class R123_DekontViewModel
    {
        public DateTime IslemTarihi { get; set; }
        public string Aciklama { get; set; }
        public string ParaBirimi { get; set; }
        public decimal Tutar { get; set; }
        public string KarsiHesapKodu { get; set; }
        public string KarsiHesapAdi { get; set; }
        public string IslemTipi { get; set; }
    }

    public class R123_CariViewModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
    }

    public class R123_HesapViewModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Currency { get; set; }
    }

    public class R123_DatabaseConfig
    {
        public string Key { get; set; }
        public string Display { get; set; }
    }

    public class R123_AccountBPModel
    {
        public string DbKey { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsBP { get; set; }
    }

    public class R123_NakitAkisViewModel
    {
        public string DbKey { get; set; }
        public string Sirket { get; set; }
        public int DocEntry { get; set; }
        public DateTime? PlanlananTarih { get; set; }
        public DateTime? OdenmeTarihi { get; set; }
        public string CardCode { get; set; }
        public string CariVknTckn { get; set; }
        public string KurumAdi { get; set; }
        public string BagliBelgeTipi { get; set; }
        public string FaturaSiparisNo { get; set; }
        public string UrunHizmetAciklama { get; set; }
        public string BelgePB { get; set; }
        public decimal BelgeTutar { get; set; }
        public decimal AnlasilanKur { get; set; }
        public string OdenenPB { get; set; }
        public decimal TutarTL { get; set; }
        public string VirmanDurumu { get; set; }
        public int? VirmanTransId { get; set; }
        public decimal TlKarsiligi => BelgeTutar * AnlasilanKur;

        public bool FinekraEslestiMi { get; set; } = false;
        public string FinekraId { get; set; }
        public DateTime? FinekraIslemTarihi { get; set; }
        public string FinekraBanka { get; set; }
        public decimal? FinekraTutar { get; set; }
        public string FinekraAciklama { get; set; }

        public decimal FarkTutar { get; set; } = 0;
    }

    public class R123_VirmanCreationRequest
    {
        public string DbKey { get; set; }
        public int NakitAkisDocEntry { get; set; }
        public DateTime RefDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime TaxDate { get; set; }
        public string Memo { get; set; }
        public string Ref1 { get; set; }
        public string FinekraId { get; set; }
        public string IslemTipi { get; set; }
        public List<R123_JournalEntryLineModel> JournalEntryLines { get; set; }
    }

    public class R123_JournalEntryLineModel
    {
        public string AccountCode { get; set; }
        public bool IsBP { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal FCDebit { get; set; }
        public decimal FCCredit { get; set; }
        public string FCCurrency { get; set; }
        public string LineMemo { get; set; }
    }

    public class R123_SapAktarimRequest
    {
        public string Id { get; set; }
        public string BelgeTuru { get; set; }
        public DateTime IslemTarihi { get; set; }
        public string BankaHesapKodu { get; set; }
        public string KarsiTarafTipi { get; set; }
        public string CariKodu { get; set; }
        public string KarsiHesapKodu { get; set; }
        public decimal Tutar { get; set; }
        public string Aciklama { get; set; }
        public string IslemTipi { get; set; }
        public bool IsKkeg { get; set; }
        public string Gider70Hesap { get; set; }
        public string Kdv70Hesap { get; set; }
        public string Kkeg30Hesap { get; set; }
        public string KkegKdv30Hesap { get; set; }
        public decimal KdvRate { get; set; }
    }
}