// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApplication3.Controllers
{

    public class YevmiyeGiderViewModel
    {
        public int TransId { get; set; }
        public int Line_ID { get; set; }
        public DateTime? RefDate { get; set; }
        public string Account { get; set; }
        public string AcctName { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public string LineMemo { get; set; }
        public int TransType { get; set; }
        public string TransTypeName { get; set; }
        public string BaseRef { get; set; }
        public string FaturaNo { get; set; }
        public int? SeciliGiderId { get; set; }
        public string KaydedenKullaniciKodu { get; set; }
        public string KaydedenKullaniciAdi { get; set; }
    }

    public class GiderKoduModel
    {
        public int Id { get; set; }
        public string TamAciklama { get; set; }
    }

    public class BelgeDetayModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string ItemOrAccountCode { get; set; }
        public string Dscription { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class YevmiyeFisiPdfModel
    {
        public int TransId { get; set; }
        public DateTime? RefDate { get; set; }
        public string Memo { get; set; }
        public List<YevmiyeFisiSatirModel> Lines { get; set; } = new List<YevmiyeFisiSatirModel>();
    }

    public class YevmiyeFisiSatirModel
    {
        public string Account { get; set; }
        public string AcctName { get; set; }
        public string LineMemo { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
    }

    public class KayitIstegi
    {
        public int TransId { get; set; }
        public int LineId { get; set; }
        public int GiderId { get; set; }
    }

   


    [Authorize]
    [Route("[controller]")]
    public class Rapor127Controller : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "Avrasya" },
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
            new DatabaseConfig { Key = "DefaultConnection37", Display = "RGB_TEKSTIL" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        [HttpGet]
        public IActionResult Index(int? year, int? month)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.GiderKodlari = WebApplication3.OrnekDoldurucu.Liste<GiderKoduModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.YevmiyeGiderViewModel>(12));
}

        private List<YevmiyeGiderViewModel> GetYevmiyeSatirlari(string connectionString, DateTime startDate, DateTime endDate)
 {return default;
}

        private List<GiderKoduModel> GetGiderKodlari()
 {return default;
}

        [HttpPost("GiderKaydet")]
        public IActionResult GiderKaydet([FromBody] KayitIstegi req)
 {return Json(new { success = true, message = "Başarıyla kaydedildi." });
}

        [HttpGet("BelgeDetayGetir")]
        public IActionResult BelgeDetayGetir(int transId, int transType)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.BelgeDetayModel>(12) });
}

        [HttpGet("DownloadYevmiyePdf")]
        public IActionResult DownloadYevmiyePdf(int transId)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}



        [HttpGet("DownloadQnbFatura")]
        public async Task<IActionResult> DownloadQnbFatura(string faturaNo)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private decimal ParseDecimal(string val)
 {return default;
}
    }
}