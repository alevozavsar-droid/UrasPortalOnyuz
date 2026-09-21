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
using QuestPDF.Infrastructure; // IDocument için gerekli
using WebApplication3.Documents; // Yeni oluşturduğunuz doküman sınıfları için
using Microsoft.AspNetCore.Authorization; // Yetkilendirme için EKLENDİ
using System.Security.Claims; // Claim'lere erişim için EKLENDİ

namespace WebApplication3.Controllers
{

    [Authorize]
    [Route("[controller]")]
    public class Rapor23controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _applicationContext;
        private readonly ILogger<Rapor23controller> _logger;
        private readonly IWebHostEnvironment _env;

        private const int InitialPageSize = 500;
        private const int CommandTimeoutSeconds = 180;

        [HttpPost("Guncelle")]

        [Authorize] // Nakit Akış raporlarını güncellediği varsayımıyla
        public IActionResult Guncelle(List<BelgeViewModel> belgeler)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}


        [HttpGet("Index")]
        [Authorize]
        public IActionResult Index(int pageNumber = 1, int pageSize = InitialPageSize)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("GuncelleSira")]
        [ValidateAntiForgeryToken]

        [Authorize] // Nakit Akış raporlarının sıralamasını güncellediği varsayımıyla
        public IActionResult GuncelleSira([FromBody] List<BelgeViewModel> siralama)
 {return Json(new { success = true, message = "Sıralama başarıyla kaydedildi." });
}

        [HttpPost("CreatePaymentInstruction")]
        [ValidateAntiForgeryToken]


        [Authorize]
        public async Task<IActionResult> CreatePaymentInstruction([FromBody] PaymentInstructionRequest request)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}



        private string GetSelectedDatabase()
 {return default;
}
        [HttpGet("GetSupp")]

        [Authorize]
        public async Task<IActionResult> GetSupp()
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.Supplier>(12));
}

        [HttpGet("GetSuppliers")]

        [Authorize] // Ödeme talimatı oluşturma yetkisiyle aynı politikayı kullanıyoruz.
        public async Task<IActionResult> GetSuppliers()
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.Supplier>(12));
}

        [HttpGet("GetCompanySpecificSuppliers")] // Yeni ve farklı bir endpoint adı

        [Authorize] // Ödeme talimatı oluşturma yetkisiyle aynı politikayı kullanıyoruz.
        public async Task<IActionResult> GetCompanySpecificSuppliers() // Yeni metot adı
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.Supplier>(12));
}


        [HttpGet("GetSupplierBankInfo")]
        [Authorize]
        public async Task<IActionResult> GetSupplierBankInfo(string cardCode)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.SupplierBankInfo>(12));
}



        [HttpGet("GetCompanyBankAccounts")]

        [Authorize] // Ödeme talimatı oluşturma yetkisiyle aynı politikayı kullanıyoruz.
        public async Task<IActionResult> GetCompanyBankAccounts()
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.CompanyBankInfo>(12));
}


        [HttpGet("GetPortfolioChecks")]
        [Authorize]
        public async Task<IActionResult> GetPortfolioChecks()
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.CekViewModel>(12));
}

        private List<BelgeViewModel> GetBelgeler(string connectionString, int pageNumber, int pageSize, out int totalRecords, bool getAll = false)
 {totalRecords = default;
return default;
}
    }
}
