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

    public class SapAlisIrsaliyesiModel
    {
        public string NumAtCard { get; set; }
        public DateTime DocDate { get; set; }
        public decimal DocTotalTRY { get; set; }
    }

    public class QnbSapIrsaliyeKarsilastirmaViewModel
    {
        public string BelgeNo { get; set; }
        public DateTime BelgeTarihi { get; set; }
        public string SaticiUnvan { get; set; }
        public string Ettn { get; set; }
        public string VknTckn { get; set; }
        public string IrsaliyeDurumu { get; set; }
        public bool IptalMi { get; set; }
    }

    public class IrsaliyeKarsilastirmaListeResponse
    {
        public string BelgeNo { get; set; }
        public DateTime BelgeTarihi { get; set; }
        public string SaticiUnvan { get; set; }
        public string Ettn { get; set; }
        public string VknTckn { get; set; }
        public string IrsaliyeDurumu { get; set; }
        public bool IptalMi { get; set; }
        public bool SapteVarMi { get; set; }
        public bool MukerrerMi { get; set; }
        public int SapIrsaliyeSayisi { get; set; }
        public DateTime? SapBelgeTarihi { get; set; }
    }

    public class QnbIrsaliyeDetailResponse
    {
        public bool Success { get; set; }
        public string FaturaTipi { get; set; }
        public string GercekDurum { get; set; }
        public bool IsIptal { get; set; }
    }

    public class IrsaliyeHesapOneriDto
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
    }

    public class IrsaliyeSearchResultDto
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }


    public class IrsaliyeTaslakDto
    {
        public string BelgeNo { get; set; }
        public string DocType { get; set; }
        public string BelgeTarihi { get; set; }
        public string KayitTarihi { get; set; }
        public string VadeTarihi { get; set; }
        public string SaticiVkn { get; set; }
        public string SaticiUnvan { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CariBulunmaYontemi { get; set; }
        public string SiparisNo { get; set; }
        public List<IrsaliyeTaslakSatirDto> Satirlar { get; set; } = new List<IrsaliyeTaslakSatirDto>();
    }

    public class IrsaliyeTaslakSatirDto
    {
        public string UrunKodu { get; set; }
        public string UrunAdi { get; set; }
        public decimal Miktar { get; set; }
        public string Birim { get; set; }
        public string Kaynak { get; set; }
        public string OnerilenItemKodu { get; set; }
        public string OnerilenItemAdi { get; set; }
        public List<IrsaliyeSearchResultDto> DigerOnerilenItemler { get; set; } = new List<IrsaliyeSearchResultDto>();
    }

    public class SapIrsaliyeSaveRequestDto
    {
        public string DbSelection { get; set; }
        public string CardCode { get; set; }
        public string NumAtCard { get; set; }
        public DateTime? DocDate { get; set; }
        public DateTime? DocDueDate { get; set; }
        public DateTime? TaxDate { get; set; }
        public List<SapIrsaliyeSaveLineDto> Lines { get; set; } = new List<SapIrsaliyeSaveLineDto>();
    }

    public class SapIrsaliyeSaveLineDto
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal Quantity { get; set; }
    }


    [Authorize]
    [Route("[controller]")]
    public class Rapor135Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor135Controller> _logger;

        [HttpGet("Index")]

        private string VeritabaniAdi(string dbKey)
 {return default;
}


        private List<KeyValuePair<string, string>> QnbSirketleri()
 {return default;
}
        public IActionResult Index(string dbSelection = "DefaultConnection5")
 {ViewBag.SelectedDb = "";
ViewBag.QnbSirketler = WebApplication3.OrnekDoldurucu.Liste<KeyValuePair<string, string>>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetIrsaliyelerListesi")]
        public async Task<IActionResult> GetIrsaliyelerListesi(string startDate, string endDate, string dbSelection)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.IrsaliyeKarsilastirmaListeResponse>(12) });
}

        [HttpGet("ExtractQnbDetail")]
        public async Task<IActionResult> ExtractQnbDetail(string ettn, string dbSelection)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::FinansRaporlama.Controllers.QnbIrsaliyeDetailResponse>());
}

        [HttpGet("SearchSapData")]
        public async Task<IActionResult> SearchSapData(string term, string dbSelection)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Code", "Name" }));
}

        [HttpGet("GetIrsaliyeTaslak")]
        public async Task<IActionResult> GetIrsaliyeTaslak(string ettn, string dbSelection)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::FinansRaporlama.Controllers.IrsaliyeTaslakDto>() });
}

        [HttpGet("GetIrsaliyeHtml")]
        public async Task<IActionResult> GetIrsaliyeHtml(string ettn, string dbSelection)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private IActionResult GenerateCustomHtmlFromQnbIrsaliyeXml(XDocument doc, string rawUblXml)
 {return default;
}

        [HttpPost("SaveToSap")]
        public async Task<IActionResult> SaveToSap([FromBody] SapIrsaliyeSaveRequestDto request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private async Task<List<QnbSapIrsaliyeKarsilastirmaViewModel>> GetOnlyQnbIrsaliyeler(string vkn, DateTime startDate, DateTime endDate, string dbSelection)
 {return default;
}

        private async Task<List<SapAlisIrsaliyesiModel>> GetSapAlisIrsaliyeleriAsync(string connectionString, DateTime startDate, DateTime endDate)
 {return default;
}

        private decimal ParseDecimal(string val)
 {return default;
}
    }
}