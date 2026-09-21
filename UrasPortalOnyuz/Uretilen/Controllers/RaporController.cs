// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using WebApplication3.Data;
using WebApplication3.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Data;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using WebApplication3.Documents;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace FinansRaporlama.Controllers
{



    public class NakitAkisaGonderDto
    {
        public string HedefFirmaDb { get; set; }
        public List<NakitAkisTransferItem> Kalemler { get; set; }
    }

    public class NakitAkisTransferItem
    {
        public string Kurum { get; set; }
        public string UrunHizmet { get; set; }
        public DateTime? PlanlananTarih { get; set; }
        public string ParaBirimi { get; set; }
        public decimal Tutar { get; set; }
        public decimal TutarTL { get; set; }
        public string OdemeYontemi { get; set; }
        public string BagliBelgeTipi { get; set; }
        public int BagliDocEntry { get; set; }
        public string FaturaSiparisNo { get; set; }
        public string KaynakFirmaDb { get; set; }
    }

    [Authorize]
    [Route("[controller]")]
    public class RaporController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _applicationContext;

        private readonly ILogger<RaporController> _logger;
        private readonly IWebHostEnvironment _env;

        private const int InitialPageSize = 1500;
        private const int CommandTimeoutSeconds = 180;





        private string GetDbConnectionString(string dbName)
 {return default;
}

        [HttpPost("NakitAkisaGonder")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> NakitAkisaGonder([FromBody] NakitAkisaGonderDto request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        [HttpPost("Guncelle")]
        [Authorize]
        public async Task<IActionResult> Guncelle(List<BelgeViewModel> belgeler)
 {return RedirectToAction("Index");
}


        [HttpGet("Index")]
        [Authorize]
        public IActionResult Index()
 {ViewBag.DatabaseName = "";
ViewBag.HaftaList = new System.Collections.Generic.List<object>();
ViewBag.TarihAraligiList = new System.Collections.Generic.List<object>();
ViewBag.BelgeTarihiAraligiList = new System.Collections.Generic.List<object>();
ViewBag.BelgeTarihiList = new System.Collections.Generic.List<object>();
ViewBag.TedarikciAlacakliList = new System.Collections.Generic.List<object>();
ViewBag.DocNumList = new System.Collections.Generic.List<object>();
ViewBag.FaturaNoList = new System.Collections.Generic.List<object>();
ViewBag.OdeplanTarihiList = new System.Collections.Generic.List<object>();
ViewBag.OdeplanNotList = new System.Collections.Generic.List<object>();
ViewBag.OdemeNedeniList = new System.Collections.Generic.List<object>();
ViewBag.MutabakatDurumuList = new System.Collections.Generic.List<object>();
ViewBag.VadeTarihiList = new System.Collections.Generic.List<object>();
ViewBag.GunFarkiList = new System.Collections.Generic.List<object>();
ViewBag.TutarList = new System.Collections.Generic.List<object>();
ViewBag.OdenenTutarList = new System.Collections.Generic.List<object>();
ViewBag.KalanTutarList = new System.Collections.Generic.List<object>();
ViewBag.ParaBirimiList = new System.Collections.Generic.List<object>();
ViewBag.OdemeSekliList = new System.Collections.Generic.List<object>();
ViewBag.GuncelTLTutarList = new System.Collections.Generic.List<object>();
ViewBag.DurumList = new System.Collections.Generic.List<object>();
ViewBag.BelgeTipiList = new System.Collections.Generic.List<object>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.BelgeViewModel>(12));
}

        [HttpGet("OnayBekleyenler")]
        [Authorize]
        public IActionResult OnayBekleyenler()
 {return Json(new { success = true, data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { DocEntry = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("DocEntry", i2), DocNum = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("DocNum", i2), TedarikciAlacakli = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("TedarikciAlacakli", i2), Tutar = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("Tutar", i2), ParaBirimi = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("ParaBirimi", i2), OdeplanTarihi = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("OdeplanTarihi", i2), OdemeNedeni = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("OdemeNedeni", i2), Sahip = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("Sahip", i2), ApprovalStatus = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("ApprovalStatus", i2) }).ToList() });
}

        [HttpPost("OnayReddet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OnayReddet([FromForm] int docEntry, [FromForm] string islem)
 {return Json(new { success = true, message = "Kullanıcı bilgisi alınamadı." });
}

        [HttpPost("GuncelleSira")]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult GuncelleSira([FromBody] List<SiralamaViewModel> siralama)
 {return Json(new { success = true, message = "Sıralama başarıyla kaydedildi." });
}

        [HttpPost("Ekle")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Ekle(OdemeViewModel belge)
 {return RedirectToAction("Index");
}

        [HttpPost("Sil")]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Sil([FromForm] string belgeTipi, [FromForm] int docEntry)
 {return Json(new { success = true, message = "Sadece manuel ödemeler silinebilir." });
}

        private string GetSelectedDatabase()
 {return default;
}

        private List<BelgeViewModel> GetBelgeler(string connectionString, int pageNumber, int pageSize, out int totalRecords, bool getAll = false)
 {totalRecords = default;
return default;
}
    }
}