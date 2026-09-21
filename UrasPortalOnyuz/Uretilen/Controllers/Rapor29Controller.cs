// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WebApplication3.Models;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace FinansRaporlama.Controllers
{
    [Authorize]
    public class Rapor29Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor29Controller> _logger;

        public IActionResult Index(string searchTerm, int? yil, int? ay, string urunler)
 {ViewBag.SeciliUrunler = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.Yillar = new System.Collections.Generic.List<object>();
ViewBag.Aylar = new System.Collections.Generic.List<object>();
ViewBag.UrunListesi = new System.Collections.Generic.List<object>();
ViewBag.FaturaAyiList = new System.Collections.Generic.List<object>();
ViewBag.MusteriAdiList = new System.Collections.Generic.List<object>();
ViewBag.SatisParaBirimiList = new System.Collections.Generic.List<object>();
ViewBag.DurumList = new System.Collections.Generic.List<object>();
ViewBag.SeciliYil = "";
ViewBag.SeciliAy = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.SalesReportViewModel>(12));
}

        public IActionResult GetSaleDetails(string docNum)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return PartialView("_SaleDetailsPartial", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.SaleDetailsViewModel>(12));
}

        public IActionResult GetPurchaseDetails(string docNum)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "FaturaNo", "FaturaTarihi", "TedarikciKodu", "TedarikciAdi", "UrunKodu", "UrunAciklamasi", "Miktar", "ParaBirimi", "BirimFiyat", "SatirToplami" }));
}

        private List<SalesReportViewModel> GetSalesReportData(string connectionString, string searchTerm, int year, int month, List<string> urunler)
 {return default;
}

        private List<SaleDetailsViewModel> GetSaleDetailsData(string connectionString, string docNum)
 {return default;
}

        private string GetSelectedDatabase()
 {return default;
}
    }
}