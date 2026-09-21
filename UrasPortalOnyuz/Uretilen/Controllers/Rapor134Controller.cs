// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Net.Http.Headers;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor134Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor134Controller> _logger;


        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "Avrasya", DbName = "Avrasya" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya", DbName = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.S", DbName = "TESTURASKIMYA_A.S" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS", DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS", DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS", DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S", DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S", DbName = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        [HttpGet]
        public async Task<IActionResult> Index()
 {ViewBag.UlkeListesi = WebApplication3.OrnekDoldurucu.Liste<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(12);
ViewBag.BankaListesi = WebApplication3.OrnekDoldurucu.Liste<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(12);
ViewBag.GrupListesi = WebApplication3.OrnekDoldurucu.Liste<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(12);
ViewBag.HesapGruplari = WebApplication3.OrnekDoldurucu.Liste<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(12);
ViewBag.OdemeSekliListesi = WebApplication3.OrnekDoldurucu.Liste<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(12);
ViewBag.ParaBirimiListesi = WebApplication3.OrnekDoldurucu.Liste<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.TedarikciTalepModel>());
}

        [HttpGet("GetIller")]
        public async Task<IActionResult> GetIller(string ulkeKodu)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Code", "Name" }));
}

        private async Task<List<SelectListItem>> GetListFromDB(string connectionString, string query)
 {return default;
}

        [HttpPost("Kaydet")]
        public async Task<IActionResult> Kaydet(TedarikciTalepModel model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private async Task<(int NextNum, string FatherAcctCode)> GetNextCardCode(string connectionString, string prefix)
 {return default;
}

        private async Task<(bool IsSuccess, string ErrorMessage)> CreateBPAndGLAccountViaServiceLayer(string targetCompanyDb, string connectionString, TedarikciTalepModel model, string newCardCode, string fatherAcctCode)
 {return default;
}

        private async Task SaveToCustomTable(string connectionString, TedarikciTalepModel model, string newCardCode)
 {}
    }




    public class TedarikciTalepModel
    {
        public string Hazirlayan { get; set; }
        public DateTime HazirlanmaTarihi { get; set; }
        public string QROkutunuz { get; set; }
        public string MukkellefBilgisi { get; set; }
        public string HesapKodu { get; set; }
        public string FirmaUnvani { get; set; }
        public string Adres { get; set; }
        public string Ulke { get; set; }
        public string Il { get; set; }
        public string Ilce { get; set; }
        public string PostaKodu { get; set; }
        public string TelNo { get; set; }
        public string EPosta { get; set; }
        public string MuhatapGrubu { get; set; }
        public string SatisAdi { get; set; }
        public string SatisGSM { get; set; }
        public string SatisEPosta { get; set; }
        public string MuhasebeAdi { get; set; }
        public string MuhasebeGSM { get; set; }
        public string MuhasebeEPosta { get; set; }
        public string VergiDairesi { get; set; }
        public string VergiNo { get; set; }
        public string TCKimlik { get; set; }
        public string OdemeSekli { get; set; }
        public string Banka1Adi { get; set; }
        public string Banka1ParaBirimi { get; set; }
        public string Banka1HesapNo { get; set; }
        public string Banka1IBAN { get; set; }
        public string Banka2Adi { get; set; }
        public string Banka2ParaBirimi { get; set; }
        public string Banka2HesapNo { get; set; }
        public string Banka2IBAN { get; set; }
        public string Banka3Adi { get; set; }
        public string Banka3ParaBirimi { get; set; }
        public string Banka3HesapNo { get; set; }
        public string Banka3IBAN { get; set; }
    }
}