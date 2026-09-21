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
using System.Data;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor90Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor90Controller> _logger;


        private readonly string _connectionString = "";

        private readonly string _baslangicTarihi = "2026-04-14";
        private readonly string _kaynakCari = "M1408";

        [HttpGet]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor90ViewModel>());
}




        [HttpGet("BulBenzerStok")]
        public async Task<IActionResult> BulBenzerStok(string kaynakItemCode)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "ItemCode", "ItemName" }), kaynakIsim = global::WebApplication3.OrnekDoldurucu.Deger<string>("kaynakIsim", 0) });
}




        [HttpPost("GuncelleUrasKodu")]
        public async Task<IActionResult> GuncelleUrasKodu(string kaynakItemCode, string hedefItemCode, int kuyrukId)
 {return Json(new { success = true, message = "Eşleştirme başarıyla kaydedildi! Robot işlemi tekrar denemek üzere sıraya aldı." });
}

        [HttpPost("TumEksikleriKuyrugaEkle")]
        public async Task<IActionResult> TumEksikleriKuyrugaEkle()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("TekrarKuyrugaEkle")]
        public async Task<IActionResult> TekrarKuyrugaEkle(int id)
 {return Json(new { success = true });
}

        [HttpPost("TumHatalilariKuyrugaEkle")]
        public async Task<IActionResult> TumHatalilariKuyrugaEkle()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("IptalKuyrugaEkle")]
        public async Task<IActionResult> IptalKuyrugaEkle(string objType, string docEntry)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}
    }

    public class Rapor90ViewModel
    {
        public BelgeAktarimOzet Summary { get; set; }
        public BelgeSayim Sayim { get; set; }
        public List<BelgeAktarimDetay> KuyrukListesi { get; set; }
        public List<MukerrerBelge> MukerrerKayitlar { get; set; }
        public List<EksikBelge> EksikKayitlar { get; set; }
        public List<EksikBelge> ReferansiDoluKayitlar { get; set; }
        public List<BelgeIptalFark> IptalFarkliKayitlar { get; set; }
    }

    public class BelgeAktarimOzet { public int BekleyenSayisi { get; set; } public int BasariliSayisi { get; set; } public int HataliSayisi { get; set; } public int KayipSayisi { get; set; } }
    public class BelgeSayim { public int KaynakSayi { get; set; } public int HedefSayi { get; set; } public int AtlananReferansSayisi { get; set; } public int GercekFark => KaynakSayi - HedefSayi - AtlananReferansSayisi; }
    public class BelgeAktarimDetay { public int KuyrukNo { get; set; } public string BelgeTipi { get; set; } public string KaynakDocEntry { get; set; } public string KaynakDocNum { get; set; } public string HedefDocEntry { get; set; } public string HedefDocNum { get; set; } public string Durum { get; set; } public string KayitZamani { get; set; } public string HataMesaji { get; set; } public string DurumBadgeClass => Durum == "P" ? "bg-warning text-dark" : (Durum == "S" ? "bg-success" : "bg-danger"); public string DurumIkonu => Durum == "P" ? "fa-hourglass-half" : (Durum == "S" ? "fa-check-circle" : "fa-times-circle"); public string DurumMetni => Durum == "P" ? "Bekliyor" : (Durum == "S" ? "Başarılı" : "Hatalı"); }
    public class EksikBelge { public string ObjType { get; set; } public string BelgeTipi { get; set; } public string KaynakDocEntry { get; set; } public string KaynakDocNum { get; set; } public string Tarih { get; set; } public string Aciklama { get; set; } public string NumAtCard { get; set; } }
    public class MukerrerBelge { public string KaynakDocEntry { get; set; } public string KaynakDocNum { get; set; } public string HedefTipi { get; set; } public string HedefDocEntry { get; set; } public string HedefDocNum { get; set; } public string Tarih { get; set; } }
    public class BelgeIptalFark { public string BelgeTipi { get; set; } public string KaynakDocEntry { get; set; } public string KaynakDocNum { get; set; } public string HedefDocEntry { get; set; } public string HedefDocNum { get; set; } public string KaynakIptal { get; set; } public string HedefIptal { get; set; } }
}