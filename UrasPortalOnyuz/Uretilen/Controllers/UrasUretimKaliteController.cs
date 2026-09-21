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
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{





















































    [Authorize]
    [Route("[controller]")]
    public class UrasUretimKaliteController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UrasUretimKaliteController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;

        private bool TamYetkiliMi()  {return default;
}
        private bool GercekAd()  {return default;
}

        private JsonResult Veri(Func<object> f)
 {return default;
}

        private static string YeniZamanDamgaliKod(string onEk)  {return default;
}





        private async Task<HttpClient> SlOturumAc()
 {return default;
}

        private static async Task SlOturumKapat(HttpClient client)
 {}

        private static string SlHataMesaji(string govde)
 {return default;
}


        private async Task<string> SlIstek(HttpClient client, HttpMethod metot, string yol, object govde = null, bool tumKoleksiyonuDegistir = false)
 {return default;
}

        private async Task<JsonElement?> SlGetJson(HttpClient client, string yol)
 {return default;
}

        private static string YazDegerVeyaBos(JsonElement el, string alan)
 {return default;
}





        [HttpGet("KaliteKontrol")]
        [HttpGet("")]
        public IActionResult KaliteKontrol()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("KaliteKontrol");
}

        [HttpGet("GetOITMForQC")]
        public JsonResult GetOITMForQC()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetQCRanges")]
        public JsonResult GetQCRanges()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetQCRangeByItemCode")]
        public JsonResult GetQCRangeByItemCode(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private static string QcRangeSelect()  {return default;
}

        [HttpGet("GetQCTarget")]
        public JsonResult GetQCTarget(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class QCTargetIstek
        {
            public string ItemCode { get; set; }
            public string Gorunus { get; set; }
            public string Ph { get; set; }
            public string Sicaklik { get; set; }
            public string Visk1 { get; set; }
            public string Visk2 { get; set; }
            public string Yogunluk { get; set; }
            public string Kati { get; set; }
            public string Tork { get; set; }
        }

        [HttpPost("SaveQCTarget")]
        public async Task<JsonResult> SaveQCTarget([FromBody] QCTargetIstek istek)
 {return Json(new { success = true, message = "Aranan değerler kaydedildi." });
}



        public class QCRangeIstek
        {
            public string Code { get; set; }
            public string Name { get; set; }
            public string ItemCode { get; set; }
            public double? PhMin { get; set; }
            public double? PhMax { get; set; }
            public double? Visc1Min { get; set; }
            public double? Visc1Max { get; set; }
            public int? Spindle1 { get; set; }
            public int? RPM1 { get; set; }
            public double? Visc2Min { get; set; }
            public double? Visc2Max { get; set; }
            public int? Spindle2 { get; set; }
            public int? RPM2 { get; set; }
            public double? DensityMin { get; set; }
            public double? DensityMax { get; set; }
            public double? SolidMin { get; set; }
            public double? SolidMax { get; set; }
        }

        [HttpPost("SaveQCRange")]
        public JsonResult SaveQCRange([FromBody] QCRangeIstek m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("DeleteQCRange")]
        public JsonResult DeleteQCRange(string code)
 {return Json(new { success = true, message = "Silindi." });
}





        [HttpGet("GirisKK")]
        public IActionResult GirisKK()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("GirisKK");
}

        private string GizliJoinGKK(string kol, string alias)  {return default;
}
        private string AdExprGKK(string kol, string adAlan, string alias)  {return default;
}

        [HttpGet("GetGirisKKBekleyen")]
        public JsonResult GetGirisKKBekleyen(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetGirisKKDefter")]
        public JsonResult GetGirisKKDefter(string search = "", string sonuc = "", int page = 1, int pageSize = 50)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetGKKRanges")]
        public JsonResult GetGKKRanges(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetGKKRangeByItem")]
        public JsonResult GetGKKRangeByItem(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetGirisKKDepolar")]
        public JsonResult GetGirisKKDepolar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetGirisKKHammaddeler")]
        public JsonResult GetGirisKKHammaddeler(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class GirisKKPartiIstek { public string PartiNo { get; set; } public decimal Miktar { get; set; } }
        public class GirisKKKayitIstek
        {
            public int KaynakEntry { get; set; }
            public int KaynakLineNum { get; set; }
            public string KaynakNo { get; set; }
            public string Tarih { get; set; }
            public string CardCode { get; set; }
            public string Tedarikci { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string IrsaliyeNo { get; set; }
            public decimal Miktar { get; set; }
            public string Birim { get; set; }
            public string PartiNo { get; set; }
            public string Gorunus { get; set; }
            public string Renk { get; set; }
            public double? Viskozite { get; set; }
            public double? Ph { get; set; }
            public double? Yogunluk { get; set; }
            public double? Kati { get; set; }
            public string Sonuc { get; set; }
            public string Aciklama { get; set; }
            public string HedefDepo { get; set; }
            public List<GirisKKPartiIstek> Partiler { get; set; }
        }

        [HttpPost("SaveGirisKK")]
        public async Task<JsonResult> SaveGirisKK([FromBody] GirisKKKayitIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        public class GirisKKDuzenleIstek
        {
            public string Code { get; set; }
            public string Tarih { get; set; }
            public string Sonuc { get; set; }
            public string Gorunus { get; set; }
            public string Renk { get; set; }
            public double? Viskozite { get; set; }
            public double? Ph { get; set; }
            public double? Yogunluk { get; set; }
            public double? Kati { get; set; }
            public string Aciklama { get; set; }
        }

        [HttpPost("UpdateGirisKK")]
        public async Task<JsonResult> UpdateGirisKK([FromBody] GirisKKDuzenleIstek istek)
 {return Json(new { success = true, message = "KK kaydı güncellendi." });
}

        public class GKKRangeIstek
        {
            public string ItemCode { get; set; }
            public double? VisMin { get; set; }
            public double? VisMax { get; set; }
            public double? PhMin { get; set; }
            public double? PhMax { get; set; }
            public double? YogMin { get; set; }
            public double? YogMax { get; set; }
            public double? KatiMin { get; set; }
            public double? KatiMax { get; set; }
        }

        [HttpPost("SaveGKKRange")]
        public async Task<JsonResult> SaveGKKRange([FromBody] GKKRangeIstek istek)
 {return Json(new { success = true, message = "Aranan değer aralığı kaydedildi." });
}

        public class GKKRangeSilIstek { public string Code { get; set; } }

        [HttpPost("DeleteGKKRange")]
        public async Task<JsonResult> DeleteGKKRange([FromBody] GKKRangeSilIstek istek)
 {return Json(new { success = true, message = "Aranan değer aralığı silindi." });
}





        [HttpGet("SatinalmaKK")]
        public IActionResult SatinalmaKK()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("SatinalmaKK");
}

        [HttpGet("GetSatinalmaKKBekleyen")]
        public JsonResult GetSatinalmaKKBekleyen(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSatinalmaKKDefter")]
        public JsonResult GetSatinalmaKKDefter(string search = "", string sonuc = "", int page = 1, int pageSize = 50)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetKimEnvList")]
        public JsonResult GetKimEnvList(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetKimEnvByItem")]
        public JsonResult GetKimEnvByItem(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSatinalmaKKKalemAra")]
        public JsonResult GetSatinalmaKKKalemAra(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSatinalmaKKTedarikciler")]
        public JsonResult GetSatinalmaKKTedarikciler(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSatinalmaKKKalemler")]
        public JsonResult GetSatinalmaKKKalemler(string search = "", bool tumu = false)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class KimEnvIstek
        {
            public string Code { get; set; }
            public string ItemCode { get; set; }
            public string Uretici { get; set; }
            public string UrunAdi { get; set; }
            public string KimyasalTuru { get; set; }
            public string CasNo { get; set; }
            public string EcNo { get; set; }
            public string SdsTarihi { get; set; }
            public string Ghs { get; set; }
            public string DepoAlan { get; set; }
            public string HKodlari { get; set; }
            public string PKodlari { get; set; }
            public string KullanimYeri { get; set; }
        }

        [HttpPost("SaveKimEnv")]
        public async Task<JsonResult> SaveKimEnv([FromBody] KimEnvIstek istek)
 {return Json(new { success = true, message = "Kimyasal envanter kaydı kaydedildi." });
}

        [HttpPost("DeleteKimEnv")]
        public async Task<JsonResult> DeleteKimEnv([FromBody] KimEnvIstek istek)
 {return Json(new { success = true, message = "Silindi." });
}

        public class SatinalmaKKKayitIstek
        {
            public string Code { get; set; }
            public int KaynakEntry { get; set; }
            public int KaynakLineNum { get; set; }
            public string KaynakNo { get; set; }
            public string CardCode { get; set; }
            public string Tedarikci { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public decimal Miktar { get; set; }
            public string Birim { get; set; }
            public string Tarih { get; set; }
            public string Gorev { get; set; }
            public string Echa { get; set; }
            public string Zdhc { get; set; }
            public string ControlMeasures { get; set; }
            public string Aciklama { get; set; }
        }

        [HttpPost("SaveSatinalmaKK")]
        public async Task<JsonResult> SaveSatinalmaKK([FromBody] SatinalmaKKKayitIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<string>("data", 0) });
}





        [HttpGet("Tutanak")]
        public IActionResult Tutanak()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Tutanak");
}

        [HttpGet("GetTutanakList")]
        public JsonResult GetTutanakList()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private static string TutanakSelect()  {return default;
}

        [HttpGet("GetTutanakKalemler")]
        public JsonResult GetTutanakKalemler(string search = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetTutanakKalemDetay")]
        public JsonResult GetTutanakKalemDetay(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetTutanakDepolar")]
        public JsonResult GetTutanakDepolar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class TutanakIstek
        {
            public string Code { get; set; }
            public string FormNo { get; set; }
            public string Tarih { get; set; }
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public string PartiNo { get; set; }
            public decimal Miktar { get; set; }
            public string Birim { get; set; }
            public string KaynakDepo { get; set; }
            public string HedefDepo { get; set; }
            public string IslemTip { get; set; }
            public string Neden { get; set; }
            public string Aciklama { get; set; }
        }

        [HttpPost("AddTutanak")]
        public async Task<JsonResult> AddTutanak([FromBody] TutanakIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), code = global::WebApplication3.OrnekDoldurucu.Deger<string>("code", 0) });
}

        [HttpPost("UpdateTutanak")]
        public async Task<JsonResult> UpdateTutanak([FromBody] TutanakIstek istek)
 {return Json(new { success = true, message = "Tutanak güncellendi." });
}

        public class TutanakOnayIstek { public string Code { get; set; } public string Not { get; set; } }

        [HttpPost("ApproveTutanak")]
        public async Task<JsonResult> ApproveTutanak([FromBody] TutanakOnayIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<string>("data", 0) });
}

        [HttpPost("RejectTutanak")]
        public async Task<JsonResult> RejectTutanak([FromBody] TutanakOnayIstek istek)
 {return Json(new { success = true, message = "Tutanak reddedildi; stok hareketi yapılmadı." });
}





        [HttpGet("MusteriSikayet")]
        public IActionResult MusteriSikayet()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("MusteriSikayet");
}

        [HttpGet("GetSikayetList")]
        public JsonResult GetSikayetList()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetSikayetYorumlar")]
        public JsonResult GetSikayetYorumlar(string code)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class SikayetIstek
        {
            public string RaporNo { get; set; }
            public string Tarih { get; set; }
            public string Tip { get; set; }
            public string Kapsam { get; set; }
            public string Marka { get; set; }
            public string Musteri { get; set; }
            public string UrunLot { get; set; }
            public string BaskiSart { get; set; }
            public string BaskiRecete { get; set; }
            public string Fikse { get; set; }
            public string MiktarBilgi { get; set; }
            public string SevkTarih { get; set; }
            public string RakipNumune { get; set; }
            public string Detay { get; set; }
        }

        [HttpPost("AddSikayet")]
        public async Task<JsonResult> AddSikayet([FromBody] SikayetIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), code = global::WebApplication3.OrnekDoldurucu.Deger<string>("code", 0) });
}

        public class SikayetYorumIstek { public string Code { get; set; } public string Bolum { get; set; } public string Yorum { get; set; } }

        [HttpPost("AddSikayetYorum")]
        public async Task<JsonResult> AddSikayetYorum([FromBody] SikayetYorumIstek istek)
 {return Json(new { success = true, message = "Yorum eklendi." });
}

        public class SikayetMetaIstek
        {
            public string Code { get; set; }
            public string Durum { get; set; }
            public string Termin { get; set; }
            public string FaaliyetTip { get; set; }
            public string IadeTip { get; set; }
            public string Maliyet { get; set; }
        }

        [HttpPost("UpdateSikayetMeta")]
        public async Task<JsonResult> UpdateSikayetMeta([FromBody] SikayetMetaIstek istek)
 {return Json(new { success = true, message = "Kaydedildi." });
}













        private static object SlTarihGonder(DateTime? d)  {return default;
}


        private async Task<(bool basarili, string mesaj)> SlFormKaydet(HttpClient client, string entity, int? docEntry, Dictionary<string, object> govde)
 {return default;
}



        [HttpGet("UrunKaliteKontrol")]
        public IActionResult UrunKaliteKontrol()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("UrunKaliteKontrol");
}

        [HttpGet("GetQCFormsByItemCode")]
        public JsonResult GetQCFormsByItemCode(string itemCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetQCProductBoilers")]
        public JsonResult GetQCProductBoilers(string formTip = null)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private static List<Dictionary<string, object>> GruplaFormlar(List<Dictionary<string, object>> satirlar)
 {return default;
}

        public class QCRowIstek
        {
            public DateTime? Date { get; set; }
            public string BoilerNo { get; set; }
            public string LotNo { get; set; }
            public string Appearance { get; set; }
            public string Ph { get; set; }
            public string Temp { get; set; }
            public string ViscBefore { get; set; }
            public string ViscAfter { get; set; }
            public string Visc24h { get; set; }
            public string Density { get; set; }
            public string Solid { get; set; }
            public string ViscBeforeDekSiz { get; set; }
            public string ViscBeforeDekLi { get; set; }
            public string Visc24hDekSiz { get; set; }
            public string Visc24hDekLi { get; set; }
            public string Tork1 { get; set; }
            public string Tork2 { get; set; }
            public string Yapiskanlik { get; set; }
            public string Result { get; set; }
            public string Description { get; set; }
            public string Responsible { get; set; }
        }

        public class QCFormIstek
        {
            public int? DocEntry { get; set; }
            public DateTime? FormDate { get; set; }
            public int WorkOrderDocEntry { get; set; }
            public string ItemCode { get; set; }
            public string ProdName { get; set; }
            public string PartiNo { get; set; }
            public string Operator { get; set; }
            public string FormNo { get; set; }
            public string Rev { get; set; }
            public string Page { get; set; }
            public List<QCRowIstek> Rows { get; set; } = new();
        }

        [HttpPost("SaveQCFormUrunKK")]
        public async Task<JsonResult> SaveQCFormUrunKK([FromBody] QCFormIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}



        [HttpGet("Yaslandirma")]
        public IActionResult Yaslandirma()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Yaslandirma");
}

        [HttpGet("GetAgingForms")]
        public JsonResult GetAgingForms()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class AgingRowIstek
        {
            public string ProdName { get; set; }
            public string BoilerNo { get; set; }
            public DateTime? AgingDate { get; set; }
            public string Description { get; set; }
            public string Responsible { get; set; }
            public DateTime? M1Date { get; set; } public double? M1Visc { get; set; } public double? M1Temp { get; set; } public double? M1Ph { get; set; } public string M1Color { get; set; } public string M1Odor { get; set; }
            public DateTime? M2Date { get; set; } public double? M2Visc { get; set; } public double? M2Temp { get; set; } public double? M2Ph { get; set; } public string M2Color { get; set; } public string M2Odor { get; set; }
            public DateTime? D4Date { get; set; } public double? D4Visc { get; set; } public double? D4Temp { get; set; } public double? D4Ph { get; set; } public string D4Color { get; set; } public string D4Odor { get; set; }
            public DateTime? D9Date { get; set; } public double? D9Visc { get; set; } public double? D9Temp { get; set; } public double? D9Ph { get; set; } public string D9Color { get; set; } public string D9Odor { get; set; }
            public DateTime? D14Date { get; set; } public double? D14Visc { get; set; } public double? D14Temp { get; set; } public double? D14Ph { get; set; } public string D14Color { get; set; } public string D14Odor { get; set; }
        }

        public class AgingFormIstek
        {
            public int? DocEntry { get; set; }
            public DateTime? FormDate { get; set; }
            public string FormNo { get; set; }
            public string Rev { get; set; }
            public string Page { get; set; }
            public string ControlledBy { get; set; }
            public string ApprovedBy { get; set; }
            public List<AgingRowIstek> Rows { get; set; } = new();
        }

        [HttpPost("SaveAgingForm")]
        public async Task<JsonResult> SaveAgingForm([FromBody] AgingFormIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}



        [HttpGet("SuBazliKK")]
        public IActionResult SuBazliKK()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("SuBazliKK");
}

        [HttpGet("PlastikBazliKK")]
        public IActionResult PlastikBazliKK()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("PlastikBazliKK");
}

        [HttpGet("GetWBQCForms")]
        public JsonResult GetWBQCForms()  {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetPBQCForms")]
        public JsonResult GetPBQCForms()  {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private object GetirWbqcTipiFormlar(string headerTable, string linesTable)
 {return default;
}

        public class WBQCRowIstek
        {
            public DateTime? Date { get; set; }
            public string ProdName { get; set; }
            public string LotNo { get; set; }
            public string SpindleTip { get; set; }
            public double? RPM { get; set; }
            public double? ProdPh { get; set; }
            public string Torque { get; set; }
            public string ProdOutputCp { get; set; }
            public double? Prod24h { get; set; }
            public double? Witness24hCp { get; set; }
            public double? Witness15DayCp { get; set; }
            public string Aging { get; set; }
            public string DesiredRange { get; set; }
            public string BinderTracking { get; set; }
            public string RecipeChange { get; set; }
            public string Result { get; set; }
            public string NotlarJson { get; set; }
        }

        public class WBQCFormIstek
        {
            public int? DocEntry { get; set; }
            public DateTime? FormDate { get; set; }
            public string FormNo { get; set; }
            public string Rev { get; set; }
            public string Page { get; set; }
            public string ControlledBy { get; set; }
            public string ApprovedBy { get; set; }
            public List<WBQCRowIstek> Rows { get; set; } = new();
        }

        [HttpPost("SaveWBQCForm")]
        public async Task<JsonResult> SaveWBQCForm([FromBody] WBQCFormIstek model)  {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("SavePBQCForm")]
        public async Task<JsonResult> SavePBQCForm([FromBody] WBQCFormIstek model)  {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private async Task<JsonResult> KaydetWbqcTipiForm(WBQCFormIstek model, string udoCode)
 {return default;
}



        [HttpGet("PHKalibrasyon")]
        public IActionResult PHKalibrasyon()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("PHKalibrasyon");
}

        [HttpGet("GetPHCalForms")]
        public JsonResult GetPHCalForms()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class PHCalRowIstek
        {
            public DateTime? Date { get; set; }
            public double? Before4 { get; set; }
            public double? Before7 { get; set; }
            public double? Before10 { get; set; }
            public double? After4 { get; set; }
            public double? After7 { get; set; }
            public double? After10 { get; set; }
            public string Result { get; set; }
            public string Note { get; set; }
            public string Responsible { get; set; }
        }

        public class PHCalFormIstek
        {
            public int? DocEntry { get; set; }
            public DateTime? FormDate { get; set; }
            public string FormNo { get; set; }
            public string Rev { get; set; }
            public string Page { get; set; }
            public string ControlledBy { get; set; }
            public DateTime? ControlDate { get; set; }
            public List<PHCalRowIstek> Rows { get; set; } = new();
        }

        [HttpPost("SavePHCalForm")]
        public async Task<JsonResult> SavePHCalForm([FromBody] PHCalFormIstek model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}












        private static readonly string[] BelgeUzantilari = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv", ".msg", ".eml", ".zip" };
        private const long BelgeMaxByte = 20971520; // 20 MB

        private string BelgeKlasoru(string altKlasor, string kod)
 {return default;
}



        [HttpGet("GirisKKPdf")]
        public IActionResult GirisKKPdf(string code)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("GetGirisKKMailAlicilar")]
        public JsonResult GetGirisKKMailAlicilar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class GirisKKMailIstek { public string Code { get; set; } public string Alici { get; set; } public string Not { get; set; } }

        [HttpPost("SendGirisKKMail")]
        public JsonResult SendGirisKKMail([FromBody] GirisKKMailIstek istek)
 {return Json(new { success = true, message = "Mail gönderildi." });
}

        private class GirisKKPdfDocument : IDocument
        {
            private readonly IDictionary<string, object> _k;
            private readonly IDictionary<string, object> _aralik;
            public GirisKKPdfDocument(object kayit, object aralik)
 {}
            private string S(string alan) => _k.TryGetValue(alan, out var v) && v != null ? v.ToString() : null;
            private string A(string alan) => _aralik != null && _aralik.TryGetValue(alan, out var v) && v != null ? v.ToString() : null;
            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
            public DocumentSettings GetSettings() => DocumentSettings.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Column(c =>
                    {
                        c.Item().Text("GİRİŞ KALİTE KONTROL FORMU (URS-F09-01)").Bold().FontSize(15);
                        c.Item().PaddingTop(2).Text("Belge No: " + S("Code")).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                            void satir(string etiket, string deger)
                            {
                                t.Cell().PaddingVertical(2).Text(etiket).SemiBold().FontColor(Colors.Grey.Darken1);
                                t.Cell().PaddingVertical(2).Text(deger ?? "-");
                            }
                            satir("Tarih", S("Tarih"));
                            satir("Tedarikçi", S("Tedarikci"));
                            satir("Hammadde", S("ItemCode") + " - " + S("ItemName"));
                            satir("İrsaliye No", S("IrsaliyeNo"));
                            satir("Parti No", S("PartiNo"));
                            satir("Miktar", $"{Convert.ToDecimal(_k["Miktar"]):N2} {S("Birim")}");
                            satir("Kontrol Yapan", S("KKYapan"));
                        });

                        column.Item().PaddingTop(14).Text("Ölçüm Sonuçları").Bold().FontSize(12);
                        column.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(2); cd.RelativeColumn(); cd.RelativeColumn(2); });
                            t.Header(h =>
                            {
                                h.Cell().Element(HeaderStil).Text("Parametre");
                                h.Cell().Element(HeaderStil).Text("Sonuç");
                                h.Cell().Element(HeaderStil).Text("Aranan Aralık");
                            });
                            static IContainer HeaderStil(IContainer c) => c.Background(Colors.Grey.Lighten3).Padding(4).DefaultTextStyle(x => x.SemiBold());
                            void satir(string ad, string deger, string min, string max)
                            {
                                t.Cell().Padding(4).Text(ad);
                                t.Cell().Padding(4).Text(deger ?? "-");
                                t.Cell().Padding(4).Text(min != null && max != null ? $"{min} – {max}" : "-");
                            }
                            satir("Görünüş", S("Gorunus"), null, null);
                            satir("Renk", S("Renk"), null, null);
                            satir("Viskozite", S("Viskozite"), A("VisMin"), A("VisMax"));
                            satir("pH", S("Ph"), A("PhMin"), A("PhMax"));
                            satir("Yoğunluk", S("Yogunluk"), A("YogMin"), A("YogMax"));
                            satir("Katı Madde %", S("Kati"), A("KatiMin"), A("KatiMax"));
                        });

                        string sonuc = S("Sonuc");
                        string sonucAd = sonuc == "O" ? "ONAY" : (sonuc == "S" ? "ŞARTLI ONAY" : "RED");
                        column.Item().PaddingTop(16).Background(sonuc == "R" ? Colors.Red.Lighten4 : Colors.Green.Lighten4).Padding(8)
                            .Text("SONUÇ: " + sonucAd).Bold().FontSize(13);
                        string aciklama = S("Aciklama");
                        if (!string.IsNullOrWhiteSpace(aciklama))
                            column.Item().PaddingTop(8).Text("Açıklama: " + aciklama);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Uras Kimya Üretim Takip Sistemi — ").FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }



        [HttpGet("TutanakPdf")]
        public IActionResult TutanakPdf(string code)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private class TutanakPdfDocument : IDocument
        {
            private readonly IDictionary<string, object> _k;
            public TutanakPdfDocument(object kayit)  {}
            private string S(string alan) => _k.TryGetValue(alan, out var v) && v != null ? v.ToString() : null;
            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
            public DocumentSettings GetSettings() => DocumentSettings.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    string islemTip = S("IslemTip");
                    string islemAd = islemTip == "K" ? "KARANTİNA" : (islemTip == "R" ? "RED" : "İMHA");
                    page.Header().Column(c =>
                    {
                        c.Item().Text("KARANTİNA / RED / İMHA TUTANAĞI").Bold().FontSize(15);
                        c.Item().PaddingTop(2).Text("Form No: " + S("FormNo") + "  •  Tutanak No: " + S("Code")).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                            void satir(string etiket, string deger)
                            {
                                t.Cell().PaddingVertical(2).Text(etiket).SemiBold().FontColor(Colors.Grey.Darken1);
                                t.Cell().PaddingVertical(2).Text(deger ?? "-");
                            }
                            satir("Tarih", S("Tarih"));
                            satir("Kalem", S("ItemCode") + " - " + S("ItemName"));
                            satir("Parti No", S("PartiNo"));
                            satir("Miktar", $"{Convert.ToDecimal(_k["Miktar"]):N2} {S("Birim")}");
                            satir("İşlem Tipi", islemAd);
                            satir("Kaynak Depo", S("KaynakDepo"));
                            satir("Hedef Depo", S("HedefDepo"));
                            satir("Neden", S("Neden"));
                            satir("Oluşturan", S("Olusturan"));
                        });

                        string aciklama = S("Aciklama");
                        if (!string.IsNullOrWhiteSpace(aciklama))
                            column.Item().PaddingTop(10).Text("Açıklama: " + aciklama);

                        string durum = S("Durum");
                        string durumAd = durum == "O" ? "ONAYLANDI" : (durum == "X" ? "REDDEDİLDİ" : "ONAY BEKLİYOR");
                        column.Item().PaddingTop(16).Background(durum == "O" ? Colors.Green.Lighten4 : (durum == "X" ? Colors.Red.Lighten4 : Colors.Amber.Lighten4))
                            .Padding(8).Text("DURUM: " + durumAd).Bold().FontSize(13);

                        if (durum != "B")
                        {
                            column.Item().PaddingTop(10).Table(t =>
                            {
                                t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                                void satir(string etiket, string deger)
                                {
                                    t.Cell().PaddingVertical(2).Text(etiket).SemiBold().FontColor(Colors.Grey.Darken1);
                                    t.Cell().PaddingVertical(2).Text(deger ?? "-");
                                }
                                satir("Onaylayan/Reddeden", S("Onaylayan"));
                                satir("Tarih", S("OnayTarih"));
                                satir("Not", S("OnayNot"));
                                string transferNo = S("TransferNo");
                                if (!string.IsNullOrWhiteSpace(transferNo)) satir("Transfer Belge No", transferNo);
                            });
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Uras Kimya Üretim Takip Sistemi — ").FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }



        [HttpGet("GetSatinalmaKKBelgeler")]
        public JsonResult GetSatinalmaKKBelgeler(string kkCode)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("UploadSatinalmaKKBelge")]
        public async Task<JsonResult> UploadSatinalmaKKBelge(IFormFile dosya, [FromForm] string kkCode, [FromForm] string aciklama = "")
 {return Json(new { success = true, message = "Belge yüklendi." });
}

        [HttpGet("SatinalmaKKBelgeIndir")]
        public IActionResult SatinalmaKKBelgeIndir(string code)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        public class SatinalmaKKBelgeSilIstek { public string Code { get; set; } }

        [HttpPost("DeleteSatinalmaKKBelge")]
        public async Task<JsonResult> DeleteSatinalmaKKBelge([FromBody] SatinalmaKKBelgeSilIstek istek)
 {return Json(new { success = true, message = "Belge silindi." });
}

        [HttpGet("SatinalmaKKPdf")]
        public IActionResult SatinalmaKKPdf(string code)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("GetSatinalmaKKMailAlicilar")]
        public JsonResult GetSatinalmaKKMailAlicilar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        public class SatinalmaKKMailIstek { public string Code { get; set; } public string Alici { get; set; } public string Not { get; set; } }

        [HttpPost("SendSatinalmaKKMail")]
        public JsonResult SendSatinalmaKKMail([FromBody] SatinalmaKKMailIstek istek)
 {return Json(new { success = true, message = "Mail gönderildi." });
}

        private class SatinalmaKKPdfDocument : IDocument
        {
            private readonly IDictionary<string, object> _k;
            private readonly List<string> _belgeler;
            public SatinalmaKKPdfDocument(object kayit, List<string> belgeler)  {}
            private string S(string alan) => _k.TryGetValue(alan, out var v) && v != null ? v.ToString() : null;
            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
            public DocumentSettings GetSettings() => DocumentSettings.Default;

            public void Compose(IDocumentContainer container)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(Fonts.Arial));

                    page.Header().Column(c =>
                    {
                        c.Item().Text("SATINALMA KALİTE KONTROL RAPORU").Bold().FontSize(15);
                        c.Item().PaddingTop(2).Text("Belge No: " + S("Code")).FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingTop(10).Column(column =>
                    {
                        column.Item().Text("SİPARİŞ VE KALEM BİLGİLERİ").Bold().FontSize(12);
                        column.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                            void satir(string etiket, string deger)
                            {
                                t.Cell().PaddingVertical(2).Text(etiket).SemiBold().FontColor(Colors.Grey.Darken1);
                                t.Cell().PaddingVertical(2).Text(deger ?? "-");
                            }
                            satir("Tarih", S("Tarih"));
                            satir("Tedarikçi", S("Tedarikci"));
                            satir("Kalem (KALEM)", S("ItemCode") + " - " + S("ItemName"));
                            satir("Kaynak No", S("KaynakNo"));
                            satir("Miktar", $"{Convert.ToDecimal(_k["Miktar"]):N2} {S("Birim")}");
                            satir("Görev", S("Gorev"));
                            satir("Değerlendirmeyi Yapan", S("KKYapan"));
                        });

                        column.Item().PaddingTop(14).Text("ECHA'YA UYGUNLUK / ZDHC GATEWAY - MRSL SEVİYESİ").Bold().FontSize(12);
                        column.Item().PaddingTop(4).Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Table(t =>
                        {
                            t.ColumnsDefinition(cd => { cd.RelativeColumn(); cd.RelativeColumn(); });
                            void satir(string etiket, string deger)
                            {
                                t.Cell().PaddingVertical(2).Text(etiket).SemiBold().FontColor(Colors.Grey.Darken1);
                                t.Cell().PaddingVertical(2).Text(deger ?? "-");
                            }
                            satir("ECHA'ya Uygunluk", S("Echa") == "Y" ? "UYGUN" : (S("Echa") == "N" ? "UYGUN DEĞİL" : "-"));
                            satir("ZDHC Gateway - MRSL Seviyesi", S("Zdhc"));
                            satir("Kontrol Önlemleri", S("ControlMeasures"));
                        });

                        string aciklama = S("Aciklama");
                        if (!string.IsNullOrWhiteSpace(aciklama))
                            column.Item().PaddingTop(10).Text("Açıklama: " + aciklama);

                        if (_belgeler.Count > 0)
                        {
                            column.Item().PaddingTop(14).Text("EKLİ BELGELER (" + _belgeler.Count + ")").Bold().FontSize(12);
                            foreach (var b in _belgeler)
                                column.Item().PaddingTop(2).Text("• " + b);
                        }



                        string echa = S("Echa");
                        string sonucAd = echa == "Y" ? "UYGUN" : (echa == "N" ? "UYGUN DEĞİL" : "DEĞERLENDİRİLMEDİ");
                        column.Item().PaddingTop(16).Background(echa == "N" ? Colors.Red.Lighten4 : Colors.Green.Lighten4).Padding(8)
                            .Text("SONUÇ: " + sonucAd).Bold().FontSize(13);
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Uras Kimya Üretim Takip Sistemi — ").FontColor(Colors.Grey.Medium);
                        x.Span(DateTime.Now.ToString("dd.MM.yyyy HH:mm")).FontColor(Colors.Grey.Medium);
                    });
                });
            }
        }
    }
}
