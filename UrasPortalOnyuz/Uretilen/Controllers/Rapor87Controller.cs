// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using WebApplication3.Services;

namespace FinansRaporlama.Controllers
{

    public class SapAlisFaturasiModel
    {
        public string NumAtCard { get; set; }
        public DateTime DocDate { get; set; }
        public decimal VatSumTRY { get; set; }
        public decimal DocTotalTRY { get; set; }
        public decimal VatSumFC { get; set; }
        public decimal DocTotalFC { get; set; }
        public string DocCur { get; set; }
    }

    public class QnbSapKarsilastirmaViewModel
    {
        public string BelgeNo { get; set; }
        public DateTime BelgeTarihi { get; set; }
        public string SaticiUnvan { get; set; }
        public string Ettn { get; set; }
        public decimal QnbToplamTutar { get; set; }
        public string ParaBirimi { get; set; }
        public string VknTckn { get; set; }
        public string FaturaDurumu { get; set; }
        public bool IptalMi { get; set; }
    }

    public class KarsilastirmaListeResponse
    {
        public string BelgeNo { get; set; }
        public DateTime BelgeTarihi { get; set; }
        public string SaticiUnvan { get; set; }
        public string Ettn { get; set; }
        public decimal QnbToplamTutar { get; set; }
        public string ParaBirimi { get; set; }
        public string VknTckn { get; set; }
        public string FaturaDurumu { get; set; }
        public bool IptalMi { get; set; }
        public bool SapteVarMi { get; set; }
        public bool MukerrerMi { get; set; }
        public int SapFaturaSayisi { get; set; }
        public decimal? SapKdvTutari { get; set; }
        public decimal? SapToplamTutar { get; set; }
        public string SapParaBirimi { get; set; }
        public DateTime? SapBelgeTarihi { get; set; }
    }

    public class QnbDetailResponse
    {
        public bool Success { get; set; }
        public decimal KdvTutari { get; set; }
        public string IrsaliyeNo { get; set; }
        public bool IrsaliyeSaptaVarMi { get; set; }
        public string FaturaTipi { get; set; }
        public string GercekDurum { get; set; }
        public bool IsIptal { get; set; }
    }

    public class QnbFaturaDurumResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; }
        public string GibStatusKodu { get; set; }
        public string GibStatusAciklama { get; set; }
        public bool IptalMi { get; set; }
        public string Message { get; set; }
    }

    public class HesapOneriDto
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
    }

    public class SearchResultDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class IhracatDosyaDto
    {
        public string DosyaNo { get; set; }
        public string CardName { get; set; }
    }

    public class FaturaTaslakDto
    {
        public string BelgeNo { get; set; }
        public string FaturaTipi { get; set; }
        public string DocType { get; set; }
        public string BelgeTarihi { get; set; }
        public string KayitTarihi { get; set; }
        public string VadeTarihi { get; set; }
        public string SaticiVkn { get; set; }
        public string SaticiUnvan { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CariBulunmaYontemi { get; set; }
        public string IrsaliyeNo { get; set; }
        public bool IrsaliyeVarMi { get; set; }
        public bool IrsaliyeSaptaVarMi { get; set; }
        public string ParaBirimi { get; set; }
        public decimal AraToplam { get; set; }
        public decimal KdvToplam { get; set; }
        public decimal GenelToplam { get; set; }
        public string VarsayilanIslemTipi { get; set; } = "R";
        public List<IhracatDosyaDto> IhracatDosyalar { get; set; } = new List<IhracatDosyaDto>();
        public List<string> IthalatDosyalar { get; set; } = new List<string>();
        public List<FaturaTaslakSatirDto> Satirlar { get; set; } = new List<FaturaTaslakSatirDto>();
        public List<FaturaTaslakSatirDto> GecmisHizmetSatirlari { get; set; } = new List<FaturaTaslakSatirDto>();
    }

    public class FaturaTaslakSatirDto
    {
        public string UrunKodu { get; set; }
        public string UrunAdi { get; set; }
        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal KdvOrani { get; set; }
        public string KdvKodu { get; set; }
        public decimal SatirToplami { get; set; }
        public string Kaynak { get; set; }
        public string OnerilenItemKodu { get; set; }
        public string OnerilenItemAdi { get; set; }
        public List<SearchResultDto> DigerOnerilenItemler { get; set; } = new List<SearchResultDto>();
        public List<HesapOneriDto> OnerilenHesaplar { get; set; } = new List<HesapOneriDto>();
    }

    public class LastServiceLineModel
    {
        public string AcctCode { get; set; }
        public string Dscription { get; set; }
        public string VatGroup { get; set; }
    }

    public class SapSaveRequestDto
    {
        public string DbSelection { get; set; }
        public string CardCode { get; set; }
        public string NumAtCard { get; set; }
        public string DocType { get; set; }
        public DateTime? DocDate { get; set; }
        public DateTime? DocDueDate { get; set; }
        public DateTime? TaxDate { get; set; }
        public string U_BE1_AKTAR { get; set; }
        public string U_FaturaTipi { get; set; }
        public string U_BE1_ITHALAT { get; set; }
        public string U_BE1_IHRACAT { get; set; }
        public List<SapSaveLineDto> Lines { get; set; } = new List<SapSaveLineDto>();
    }

    public class SapSaveLineDto
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public string VatGroup { get; set; }
    }


    [Authorize]
    [Route("[controller]")]
    public class Rapor87Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor87Controller> _logger;






        private static readonly System.Threading.SemaphoreSlim _qnbKuyruk = new System.Threading.SemaphoreSlim(3, 3);
        private static readonly HttpClient _qnbIstemci = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };

        private async Task<HttpResponseMessage> QnbPostAsync(string url, string soap)
 {return default;
}

        private string VeritabaniAdi(string dbKey)
 {return default;
}

        private List<KeyValuePair<string, string>> QnbSirketleri()
 {return default;
}

        [HttpGet("Index")]
        public IActionResult Index(string dbSelection = "DefaultConnection5")
 {ViewBag.SelectedDb = "";
ViewBag.QnbSirketler = WebApplication3.OrnekDoldurucu.Liste<KeyValuePair<string, string>>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetFaturalarListesi")]
        public async Task<IActionResult> GetFaturalarListesi(string startDate, string endDate, string dbSelection)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.KarsilastirmaListeResponse>(12) });
}

        private async Task<List<QnbSapKarsilastirmaViewModel>> GetOnlyQnbFaturalar(string vkn, DateTime startDate, DateTime endDate, string dbSelection)
 {return default;
}

        [HttpGet("ExtractQnbDetail")]
        public async Task<IActionResult> ExtractQnbDetail(string ettn, string dbSelection)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::FinansRaporlama.Controllers.QnbDetailResponse>());
}

        [HttpGet("GetQnbFaturaDurumu")]
        public async Task<IActionResult> GetQnbFaturaDurumu(string ettn, string dbSelection)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::FinansRaporlama.Controllers.QnbFaturaDurumResponse>());
}

        [HttpGet("SearchSapData")]
        public async Task<IActionResult> SearchSapData(string term, string type, string dbSelection)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Code", "Name" }));
}

        [HttpGet("GetFaturaTaslak")]
        public async Task<IActionResult> GetFaturaTaslak(string ettn, string dbSelection)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::FinansRaporlama.Controllers.FaturaTaslakDto>() });
}





        [HttpGet("GercekGoruntu")]
        public async Task<IActionResult> GercekGoruntu(string ettn, string dbSelection, string format = "HTML")
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("GetFaturaHtml")]
        public async Task<IActionResult> GetFaturaHtml(string ettn, string dbSelection)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private IActionResult GenerateCustomHtmlFromQnbXml(XDocument doc, string rawUblXml)
 {return default;
}

        [HttpPost("SaveToSap")]
        public async Task<IActionResult> SaveToSap([FromBody] SapSaveRequestDto request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private async Task<List<SapAlisFaturasiModel>> GetSapAlisFaturalariAsync(string connectionString, DateTime startDate, DateTime endDate)
 {return default;
}

        private string DetermineVatCodeForRate(decimal rate, bool isIade, bool isIstisna)
 {return default;
}

        private decimal ParseDecimal(string val)
 {return default;
}
    }
}