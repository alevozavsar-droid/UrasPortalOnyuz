// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WebApplication3.Models; // Namespace'in doğru olduğundan emin olun
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using WebApplication3.Data;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks; // Asenkron işlemler için
using Microsoft.AspNetCore.Authorization; // Yetkilendirme için eklendi
using System.Security.Claims; // Claim'lere erişim için eklendi
using Microsoft.AspNetCore.Mvc.Rendering; // SelectListItem için gerekli

namespace FinansRaporlama.Controllers
{


    [Authorize] // "A", "B", "D" yetkilerine sahip kullanıcılar erişebilir.
    public class Rapor3Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _applicationContext; // Now handles URASKIMYA
        private readonly ILogger<Rapor3Controller> _logger; // Make sure to use Rapor3Controller here
        private const int InitialPageSize = 500;


       


        public IActionResult Index(int pageNumber = 1, int pageSize = InitialPageSize)
 {ViewBag.DatabaseName = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.BelgeViewModel>(12));
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