// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{



    public class BusinessPartnerViewModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }
        public string LicTradNum { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Cellular { get; set; }
        public string Email { get; set; }

        public string AddressName { get; set; }
        public string Address2 { get; set; }
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public string County { get; set; }      // İlçe
        public string StateCode { get; set; }   // İl Kodu (örn: 42)
        public string StateName { get; set; }   // İl Adı (örn: Konya)
        public string CountryCode { get; set; } // Ülke Kodu (örn: TR)
        public string CountryName { get; set; } // Ülke Adı (örn: Turkey)
    }

    public class UpdateBpRequest
    {
        public string CardCode { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Cellular { get; set; }
        public string EmailAddress { get; set; }

        public string AddressName { get; set; }
        public string Address2 { get; set; }
        public string Street { get; set; }
        public string ZipCode { get; set; }
        public string County { get; set; }
        public string State { get; set; } // İl Kodu
        public string Country { get; set; } // Ülke Kodu
    }

    public class CountryModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class StateModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string CountryCode { get; set; }
    }




    [Authorize]
    [Route("[controller]")]
    public class Rapor120Controller : Controller
    {
        private readonly IConfiguration _configuration;

        private string GetSelectedDatabase()
 {return default;
}

        private List<CountryModel> GetCountries(string connectionString)
 {return default;
}

        private List<StateModel> GetStates(string connectionString)
 {return default;
}

        /// <summary>Cari Bilgileri = Müşteri ve Tedarikçi Cari Kartları'nın birleşik görünümü (Views/CariKart/Kartlar.cshtml, tip "Tümü"; veri CariKart/Veri). Süzgeç ön ayarı için ?tip=&bolge=&sinif=&risk=&limit=&ara=&kod= desteklenir.</summary>
        [HttpGet]
        public IActionResult Index(string searchKeyword)
        {
            ViewData["Title"] = "Cari Bilgileri"; ViewBag.Tip = "Tümü"; ViewBag.Sayfa = "kartlar"; ViewBag.Ara = searchKeyword ?? "";
            return View("~/Views/CariKart/Kartlar.cshtml");
        }

        [HttpPost("UpdateBp")]
        public async Task<IActionResult> UpdateBp([FromBody] UpdateBpRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), newAddressName = global::WebApplication3.OrnekDoldurucu.Deger<string>("newAddressName", 0) });
}
    }
}