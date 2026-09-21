// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebApplication3.Data;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{







    [Authorize]
    [Route("Profil")]
    public class ProfilController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProfilController> _logger;
        private readonly ApplicationDbContext _context;

        private string Kullanici()  {return default;
}
        private string Yetki()  {return default;
}
        private bool Yonetici()  {return default;
}

        public class KullaniciSatiri
        {
            public int UserId { get; set; }
            public string Kod { get; set; }
            public string Ad { get; set; }
            public string Mail { get; set; }
            public string BolumKodu { get; set; }
            public string Bolum { get; set; }
            public string Yetki { get; set; }
            public bool Kilitli { get; set; }
            public bool SuperUser { get; set; }
            public bool PortalKullanicisi { get; set; }
            public int? EmpId { get; set; }
            public string PersonelAd { get; set; }
            public string PersonelMail { get; set; }
            public string PersonelTel { get; set; }
            public string PersonelPozisyon { get; set; }
            public bool PersonelAktif { get; set; }
            public List<string> Eksikler { get; set; } = new List<string>();


            public string DigerSirketMail { get; set; }
            public string DigerSirketKaynak { get; set; }

            public string MailOtomatikKaynak { get; set; }

            public bool KisiYetkisiVar { get; set; }
        }


        private HashSet<string> KisiYetkiliKodlar()
 {return default;
}





        private async Task<string> MailiOtomatikTamamlaAsync(KullaniciSatiri k)
 {return default;
}

        private static readonly string KullaniciSql = @"
            SELECT U.USERID AS UserId, U.USER_CODE AS Kod, ISNULL(U.U_NAME,'') AS Ad, ISNULL(U.E_Mail,'') AS Mail,
                   CAST(U.Department AS NVARCHAR(20)) AS BolumKodu, ISNULL(D.Name,'') AS Bolum, ISNULL(U.U_BE1_YETKI,'') AS Yetki,
                   CASE WHEN U.Locked='Y' THEN 1 ELSE 0 END AS Kilitli, CASE WHEN U.SUPERUSER='Y' THEN 1 ELSE 0 END AS SuperUser,
                   CASE WHEN ISNULL(U.U_BE1_PASSWORD,'')<>'' THEN 1 ELSE 0 END AS PortalKullanicisi,
                   E.empID AS EmpId, LTRIM(RTRIM(ISNULL(E.firstName,'') + ' ' + ISNULL(E.lastName,''))) AS PersonelAd, ISNULL(E.email,'') AS PersonelMail,
                   ISNULL(E.mobile, ISNULL(E.homeTel, '')) AS PersonelTel, ISNULL(P.name,'') AS PersonelPozisyon, CASE WHEN E.Active='Y' THEN 1 ELSE 0 END AS PersonelAktif
            FROM OUSR U WITH(NOLOCK)
            LEFT JOIN OUDP D WITH(NOLOCK) ON D.Code = U.Department
            LEFT JOIN OHEM E WITH(NOLOCK) ON E.userId = U.USERID
            LEFT JOIN OHPS P WITH(NOLOCK) ON P.posID = E.position";

        private static void EksikleriIsaretle(KullaniciSatiri k)
 {}

        [HttpGet("")]
        public async Task<IActionResult> Index()
 {ViewBag.Yetki = "";
ViewBag.Ben = WebApplication3.OrnekDoldurucu.Yeni<ProfilController.KullaniciSatiri>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        [HttpGet("Liste")]
        public async Task<IActionResult> Liste()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.ProfilController.KullaniciSatiri>(12), ozet = new { toplam = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplam", 0), portal = global::WebApplication3.OrnekDoldurucu.Deger<int>("portal", 0), mailBos = global::WebApplication3.OrnekDoldurucu.Deger<int>("mailBos", 0), adBos = global::WebApplication3.OrnekDoldurucu.Deger<int>("adBos", 0), yetkiBos = global::WebApplication3.OrnekDoldurucu.Deger<int>("yetkiBos", 0), personelYok = global::WebApplication3.OrnekDoldurucu.Deger<int>("personelYok", 0), kilitli = global::WebApplication3.OrnekDoldurucu.Deger<int>("kilitli", 0) } });
}

        public class GuncelleIstek { public string Kod { get; set; } public string Ad { get; set; } public string Mail { get; set; } }


        [HttpPost("Guncelle")]
        public async Task<IActionResult> Guncelle([FromBody] GuncelleIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", 0), mail = global::WebApplication3.OrnekDoldurucu.Deger<string>("mail", 0) });
}


        private async Task SlKullaniciGuncelleAsync(int userId, string ad, string mail)
 {}
    }
}
