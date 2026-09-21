// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using WebApplication3.Models;
using WebApplication3.Helpers;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("Rapor57")]
    public class Rapor57Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor57Controller> _logger;
        private static readonly string FixedDbKey = "DefaultConnection10";
        private static readonly string FixedDbDisplay = "URAS_HOLDING";

        private string GetConnectionString()
 {return default;
}

        [HttpGet]
        [Route("")]
        [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
        public async Task<IActionResult> Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.PersonelMaliyetViewModel>(12));
}




        [HttpGet]
        [Route("DownloadTemplate")]
        public IActionResult DownloadTemplate()
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}




        [HttpPost]
        [Route("UploadAndSave")]
        public async Task<IActionResult> UploadAndSave(IFormFile file)
 {return RedirectToAction("Index");
}




        [HttpPost]
        [Route("UpdateCell")]
        public async Task<IActionResult> UpdateCell(int docEntry, string field, string value)
 {return Json(new { success = true, newMaliyet2025 = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("newMaliyet2025", 0), newMaliyet = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("newMaliyet", 0) });
}




        [HttpGet]
        [Route("GetBordroDetay")]
        public async Task<IActionResult> GetBordroDetay(int docEntry)
 {return Json(new { success = true, adSoyad = global::WebApplication3.OrnekDoldurucu.Deger<string>("adSoyad", 0), valXNet26 = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("valXNet26", 0), valRNet26 = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("valRNet26", 0), valTopNet26 = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("valTopNet26", 0), valBrut26 = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("valBrut26", 0), valMaliyet26 = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("valMaliyet26", 0), valFark = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("valFark", 0), valNetArtis = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("valNetArtis", 0), valMalArtis = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("valMalArtis", 0), modalSgkIsci = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("modalSgkIsci", 0), modalGv = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("modalGv", 0), modalSgkIsv = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("modalSgkIsv", 0), modalEskiRNet = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("modalEskiRNet", 0), modalZam = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("modalZam", 0), modalXNet = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("modalXNet", 0) });
}




        [HttpGet]
        [Route("ExportToExcel")]
        public async Task<IActionResult> ExportToExcel()
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}




        private async Task<List<PersonelMaliyetViewModel>> GetDataFromSqlAsync()
 {return default;
}


        private decimal ParseMoney(string value)
 {return default;
}

        private decimal GetDecimal(IXLWorksheet ws, int row, int col)
 {return default;
}

        private object GetDate(IXLWorksheet ws, int row, int col)
 {return default;
}
    }
}