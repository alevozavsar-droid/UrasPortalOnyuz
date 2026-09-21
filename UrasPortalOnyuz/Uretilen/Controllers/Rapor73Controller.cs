// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ClosedXML.Excel;
using System.IO;
using System.Linq;

namespace WebApplication3.Controllers
{

    [Route("[controller]")]
    public class Rapor73Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor73Controller> _logger;
        private readonly string _connectionString;

        public class GunlukTahsilatData
        {
            public string Veritabani { get; set; }
            public string MusteriGrubu { get; set; }
            public DateTime? BelgeTarihi { get; set; }
            public string HareketTipiAciklamasi { get; set; }
            public string CariHesapAciklamasi { get; set; }
            public string CekNo { get; set; }
            public string CekBankaAdi { get; set; }
            public DateTime? CekVadeTarihi { get; set; }
            public decimal Tutar_TRY { get; set; }
            public decimal Tutar_Doviz { get; set; }
            public string DokumanParaBirimi { get; set; }
            public string IslemTipi { get; set; }
            public string TahsilatBelgeNumarasi { get; set; }
            public string CekSube { get; set; }
        }

        [HttpGet("Index")]
        [HttpGet("")]
        public async Task<IActionResult> Index(DateTime? baslangicTarihi, DateTime? bitisTarihi)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor73Controller.GunlukTahsilatData>(12));
}

        [HttpGet("ExportExcel")]
        public async Task<IActionResult> ExportExcel(DateTime? baslangicTarihi, DateTime? bitisTarihi)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private async Task<List<GunlukTahsilatData>> ExecuteGunlukTahsilatQuery(string sqlQuery)
 {return default;
}

        private string GetGunlukTahsilatSQL(DateTime startDate, DateTime endDate)
 {return default;
}
    }
}