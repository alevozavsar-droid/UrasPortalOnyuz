// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;

namespace FinansRaporlama.Controllers
{
    public class ZimmetViewModel
    {
        public int DocEntry { get; set; }
        public int? DocNum { get; set; }
        public string Object { get; set; } = "ZIMMET";
        public string Canceled { get; set; } = "N";
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdateDate { get; set; }

        public string U_EkipmanTuru { get; set; }
        public string U_EkipmanAdi { get; set; }
        public string U_Marka { get; set; }
        public string U_Model { get; set; }
        public string U_SeriNo { get; set; }
        public string U_DemirbasNo { get; set; }
        public string U_FaturaNo { get; set; }
        public DateTime? U_FaturaTarihi { get; set; }
        public string U_FaturaTutari { get; set; }
        public string U_MuhasebeDegeri { get; set; }
        public string U_Sirket { get; set; }
        public string U_PersonelAdSoyad { get; set; }
        public string U_Departman { get; set; }
        public DateTime? U_TeslimTarihi { get; set; }
        public DateTime? U_IadeTarihi { get; set; }
        public string U_Durumu { get; set; }
        public string U_Lokasyon { get; set; }
        public string U_TeslimEden { get; set; }
        public string U_Onaylayan { get; set; }
        public string U_Aciklama { get; set; }
    }

    public class CokluZimmetKayitDTO
    {
        public string U_Sirket { get; set; }
        public string U_PersonelAdSoyad { get; set; }
        public string U_Departman { get; set; }
        public string U_Lokasyon { get; set; }
        public DateTime? U_TeslimTarihi { get; set; }
        public string U_TeslimEden { get; set; }
        public string U_Onaylayan { get; set; }
        public List<ZimmetKalemDTO> Kalemler { get; set; }
    }

    public class ZimmetKalemDTO
    {
        public int DocEntry { get; set; }
        public string U_EkipmanTuru { get; set; }
        public string U_EkipmanAdi { get; set; }
        public string U_Marka { get; set; }
        public string U_Model { get; set; }
        public string U_SeriNo { get; set; }
        public string U_DemirbasNo { get; set; }
        public string U_FaturaNo { get; set; }
        public DateTime? U_FaturaTarihi { get; set; }
        public string U_FaturaTutari { get; set; }
        public string U_MuhasebeDegeri { get; set; }
        public DateTime? U_IadeTarihi { get; set; }
        public string U_Durumu { get; set; }
        public string U_Aciklama { get; set; }
    }

    [Authorize]
    [Route("[controller]")]
    public class Rapor80Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;
        private static readonly string TableName = "[dbo].[@ZIMMET_KAYITLARI]";

        private string GetConnectionString()  {return default;
}

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
 {ViewBag.EkipmanTuruList = new System.Collections.Generic.List<object>();
ViewBag.EkipmanAdiList = new System.Collections.Generic.List<object>();
ViewBag.MarkaList = new System.Collections.Generic.List<object>();
ViewBag.ModelList = new System.Collections.Generic.List<object>();
ViewBag.SeriNoList = new System.Collections.Generic.List<object>();
ViewBag.DemirbasNoList = new System.Collections.Generic.List<object>();
ViewBag.FaturaNoList = new System.Collections.Generic.List<object>();
ViewBag.FaturaTarihiList = new System.Collections.Generic.List<object>();
ViewBag.FaturaTutariList = new System.Collections.Generic.List<object>();
ViewBag.MuhasebeDegeriList = new System.Collections.Generic.List<object>();
ViewBag.SirketList = new System.Collections.Generic.List<object>();
ViewBag.PersonelList = new System.Collections.Generic.List<object>();
ViewBag.DepartmanList = new System.Collections.Generic.List<object>();
ViewBag.TeslimTarihiList = new System.Collections.Generic.List<object>();
ViewBag.IadeTarihiList = new System.Collections.Generic.List<object>();
ViewBag.DurumuList = new System.Collections.Generic.List<object>();
ViewBag.LokasyonList = new System.Collections.Generic.List<object>();
ViewBag.TeslimEdenList = new System.Collections.Generic.List<object>();
ViewBag.OnaylayanList = new System.Collections.Generic.List<object>();
ViewBag.AciklamaList = new System.Collections.Generic.List<object>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<FinansRaporlama.Controllers.ZimmetViewModel>(12));
}

        [HttpGet("GetPersonelZimmetleri")]
        public async Task<IActionResult> GetPersonelZimmetleri(string sirket, string personel)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::FinansRaporlama.Controllers.ZimmetViewModel>(12) });
}

        [HttpPost("Kaydet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Kaydet([FromBody] CokluZimmetKayitDTO model)
 {return Json(new { success = true, message = "Zimmet işlemleri başarıyla kaydedildi." });
}

        [HttpPost("PdfIndir")]
        public async Task<IActionResult> PdfIndir([FromForm] List<int> selectedIds)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpPost("ExcelIceAktar")]
        public async Task<IActionResult> ExcelIceAktar(IFormFile file)
 {return RedirectToAction("Index");
}

        [HttpGet("ExcelIndir")]
        public async Task<IActionResult> ExcelIndir()
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}