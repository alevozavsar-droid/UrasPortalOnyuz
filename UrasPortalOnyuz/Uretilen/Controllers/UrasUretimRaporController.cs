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
using WebApplication3.Services;

namespace WebApplication3.Controllers
{

















    [Authorize]
    [Route("[controller]")]
    public class UrasUretimRaporController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UrasUretimRaporController> _logger;

        private bool TamYetkiliMi()
 {return default;
}


        private bool GercekAd()  {return default;
}


        private bool HammaddeGorebilir()  {return default;
}

        private string AdExpr(string itemCodeCol, string itemNameExpr, string gizliAlias)
 {return default;
}

        private string GizliJoin(string itemCodeCol, string alias)
 {return default;
}

        private static DateTime Tarih(string s, DateTime varsayilan)  {return default;
}

        private static string TipKosul(string tip, string col)  {return default;
}

        private static string PeriyotKeyExpr(string grup, string tarihExpr)
 {return default;
}

        private static List<Dictionary<string, object>> Dyn(IEnumerable<dynamic> rows)
 {return default;
}

        private JsonResult Veri(Func<object> f)
 {return default;
}



        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}



        [HttpGet("StokYasi")]
        public IActionResult StokYasi()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("StokYasi");
}

        [HttpGet("GetStokYasi")]
        public JsonResult GetStokYasi(string whs = "", string tip = "", string search = "")
 {return new JsonResult(WebApplication3.Data.UretimOrnek.StokYasi(), WebApplication3.Data.UretimOrnek.PascalCase);
}



        [HttpGet("Silo")]
        public IActionResult Silo()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Silo");
}

        [HttpGet("GetSiloRaporu")]
        public JsonResult GetSiloRaporu(string bas, string bit, string siloCode = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpGet("HammaddeTuketim")]
        public IActionResult HammaddeTuketim()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("HammaddeTuketim");
}

        [HttpGet("GetHammaddeTuketim")]
        public JsonResult GetHammaddeTuketim(string bas, string bit, string tip = "H", string itemCode = "")
 {return new JsonResult(WebApplication3.Data.UretimOrnek.HammaddeTuketim(), WebApplication3.Data.UretimOrnek.PascalCase);
}



        [HttpGet("LotSira")]
        public IActionResult LotSira()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("LotSira");
}

        [HttpGet("GetLotSira")]
        public JsonResult GetLotSira(string bas, string bit, string itemCode = "", bool sadeceSorun = false)
 {return new JsonResult(WebApplication3.Data.UretimOrnek.LotSira(), WebApplication3.Data.UretimOrnek.PascalCase);
}

        [HttpGet("GetKalemLotSira")]
        public JsonResult GetKalemLotSira(string itemCode)
 {return new JsonResult(WebApplication3.Data.UretimOrnek.KalemLotSira(itemCode), WebApplication3.Data.UretimOrnek.PascalCase);
}

        private Dictionary<string, object> LotSiraHesapla(string bas, string bit, string itemCode, bool sadeceSorun, bool tamKod)
 {return default;
}



        [HttpGet("Uretim")]
        public IActionResult Uretim()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Uretim");
}

        [HttpGet("GetUretimRaporu")]
        public JsonResult GetUretimRaporu(string bas, string bit, string grup = "G", string tip = "", string itemCode = "", string operatorAd = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetUretimFiltreler")]
        public JsonResult GetUretimFiltreler()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpGet("Performans")]
        public IActionResult Performans()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Performans");
}

        [HttpGet("GetPerformans")]
        public JsonResult GetPerformans(string bas, string bit, string tip = "", string operatorAd = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}



        [HttpGet("Sayim")]
        public IActionResult Sayim()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Sayim");
}


        private string SayimKapsamFiltresi(string alias)  {return default;
}

        [HttpGet("GetSayimRaporu")]
        public JsonResult GetSayimRaporu(string bas, string bit, string depo = "", string durum = "")
 {return new JsonResult(WebApplication3.Data.UretimOrnek.SayimRaporu(), WebApplication3.Data.UretimOrnek.PascalCase);
}









        [HttpGet("Izlenebilirlik")]
        public IActionResult Izlenebilirlik(string itemCode = "", string parti = "")
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Izlenebilirlik");
}

        [HttpGet("LotAra")]
        public JsonResult LotAra(string q = "", int top = 40)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetLotDetay")]
        public JsonResult GetLotDetay(string itemCode, string parti)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private Dictionary<string, object> LotDetayHesapla(string itemCode, string parti)
 {return default;
}

        private static Dictionary<string, object> Dugum(string id, string tip, int katman, string baslik, string alt, string detay, string itemCode = null, string parti = null, object ref_ = null)
 {return default;
}

        private static Dictionary<string, object> Bag(string kaynak, string hedef, string etiket)
 {return default;
}

        private static string Mik(object v, string birim)
 {return default;
}

        private static string S(Dictionary<string, object> r, string k)
 {return default;
}

        private static int I(Dictionary<string, object> r, string k)
 {return default;
}

        private static string SonucOzet(IEnumerable<string> sonuclar)
 {return default;
}
    }
}
