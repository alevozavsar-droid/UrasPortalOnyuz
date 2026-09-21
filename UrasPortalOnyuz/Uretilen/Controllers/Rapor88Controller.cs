// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{

    public class TopluSorguIstekModel
    {
        public string Vkn { get; set; }
        public string CardCode { get; set; }
    }

    public class Rapor88FaturaModel
    {
        public int DocEntry { get; set; }
        public string ObjType { get; set; }
        public string SapBelgeNo { get; set; }
        public string BelgeNo { get; set; }
        public DateTime Tarih { get; set; }
        public string CardCode { get; set; }
        public string AliciUnvan { get; set; }
        public string AliciVkn { get; set; }
        public string AliciTckn { get; set; }
        public decimal ToplamTutar { get; set; }
        public string ParaBirimi { get; set; }
        public string BelgeTipi { get; set; }
        public string EBelgeMukellefi { get; set; }

        public string SonDurum { get; set; }
        public string SonMesaj { get; set; }
        public string SonOid { get; set; }
        public string SonGibNo { get; set; }
        public DateTime? SonTarih { get; set; }
    }

    public class Rapor88CariModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Vkn { get; set; }
        public string Tckn { get; set; }
        public string EBelgeMukellefi { get; set; }
    }

    public class Rapor88DashboardViewModel
    {
        public int BekleyenSayisi { get; set; }
        public int HataliSayisi { get; set; }
        public int BasariliSayisi { get; set; }
        public List<Rapor88FaturaModel> BekleyenFaturalar { get; set; } = new List<Rapor88FaturaModel>();
    }

    public class Rapor88DatabaseConfig
    {
        public string Key { get; set; }
        public string Display { get; set; }
        public string DbName { get; set; }
    }
    public class QnbBelgeDurumModel
    {
        public int DurumKodu { get; set; }
        public int GonderimDurumu { get; set; }
        public int GonderimCevabiKodu { get; set; }
        public int YanitDurumu { get; set; }
        public string Aciklama { get; set; }
        public string GonderimCevabiDetayi { get; set; }
        public string YanitDetayi { get; set; }
        public bool IsError { get; set; }





        public bool SorguHatasi { get; set; }

        public string FinalMesaj { get; set; }
    }
    public class SoforModel
    {
        public string Plaka { get; set; }
        public string SoforAdSoyad { get; set; }
        public string SoforTckn { get; set; }
    }


    public class BelgeKalemModel
    {
        public int SiraNo { get; set; }
        public int LineNum { get; set; }
        public string UrunKodu { get; set; }
        public string UrunAdi { get; set; }
        public decimal Miktar { get; set; }
        public string MevcutBirim { get; set; }
        public string YeniBirim { get; set; }
    }

    public class KalemBirimUpdateModel
    {
        public int LineNum { get; set; }
        public string YeniBirim { get; set; }
    }

    public class UpdateBirimRequest
    {
        public int DocEntry { get; set; }
        public string ObjType { get; set; }
        public List<KalemBirimUpdateModel> Kalemler { get; set; }
    }



    [Authorize]
    [Route("[controller]")]
    public class Rapor88Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly string _qnbUrl = "https://efaturaconnector.qnbesolutions.com.tr/connector/ws/connectorService?wsdl";


        private bool TestOrtamiYetkisi {get {return default;
}
}
        private bool IsTestEnvironment
        {
            get
 {return default;
}
        }

        [HttpPost("ToggleTestMode")]
        public IActionResult ToggleTestMode(bool isTest)
 {return Json(new { success = true, isTest = global::WebApplication3.OrnekDoldurucu.Deger<bool>("isTest", 0) });
}

        private string GetQnbUrl()
 {return default;
}

        private readonly List<Rapor88DatabaseConfig> _databases = new List<Rapor88DatabaseConfig>
        {
            new Rapor88DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "AVRASYA" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection12", Display = "URSMAKINE__A.S", DbName = "URSMAKINE__A.S" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya", DbName = "TestUrasKimya" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection13", Display = "ALVKIMYA_A.S", DbName = "ALVKIMYA_A.S" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new Rapor88DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" }
        };

        private readonly WebApplication3.Services.QnbEArsivServisi _eArsiv;

        private string GetSelectedDatabase()
 {return default;
}

        private string GetCompanyLogoPath(string dbKey)
 {return default;
}

        private (string headerTable, string lineTable, string addrTable) GetTablesByObjType(string objType)
 {return default;
}

        private string GetFaturaTipiMetin(string faturaTipiKodu)
 {return default;
}

        private string SafeCdata(string value)
 {return default;
}

        private string SafeXmlString(string value)
 {return default;
}

        private string GetUblUnitCode(string sapBirim)
 {return default;
}






        private string AltGorselleriUret(string dbKey, bool isIrsaliye)
 {return default;
}

        private string InjectBase64IntoXslt(string content, string placeholder, string rawBase64, string mimeType)
 {return default;
}

        private async Task LogIslemGecmisiAsync(string dbKey, int docEntry, string objType, string sapBelgeNo, string gibFaturaNo, string oid, string islemTipi, string durum, string mesaj,
            string uuid = null, string kanal = null)
 {}

        private async Task CheckAndCreateUdfAsync(string dbKey)
 {}


        [HttpGet("Index")]
        public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.IsTestEnvironment = false;
ViewBag.TestOrtamiYetkisi = false;
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor88DashboardViewModel>());
}


        [HttpGet("GetBekleyenBelgelerJson")]
        public async Task<IActionResult> GetBekleyenBelgelerJson(DateTime? startDate, DateTime? endDate)
 {return Json(new { success = true, bekleyenSayisi = global::WebApplication3.OrnekDoldurucu.Deger<int>("bekleyenSayisi", 0), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor88FaturaModel>(12) });
}


        [HttpGet("GetBelgeKalemleri")]
        public async Task<IActionResult> GetBelgeKalemleri(int docEntry, string objType)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.BelgeKalemModel>(12) });
}

        [HttpPost("UpdateKalemBirimleri")]
        public async Task<IActionResult> UpdateKalemBirimleri([FromBody] UpdateBirimRequest req)
 {return Json(new { success = true, message = "Belge kalem birimleri başarıyla güncellendi. Artık önizleme veya gönderim yapabilirsiniz." });
}

        [HttpGet("AraCari")]
        public async Task<IActionResult> AraCari(string q)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor88CariModel>(12) });
}

        [HttpGet("GetAdresBilgileri")]
        public async Task<IActionResult> GetAdresBilgileri(int docEntry, string objType, string cardCode)
 {return Json(new { success = true, belgeAdres = new { sokak = global::WebApplication3.OrnekDoldurucu.Deger<string>("sokak", 0), ilce = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("ilce", 0), il = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("il", 0), postaKodu = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("postaKodu", 0), ulke = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("ulke", 0), isDolu = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("isDolu", 0) }, cariAdres = new { sokak = global::WebApplication3.OrnekDoldurucu.Deger<string>("sokak", 0), ilce = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("ilce", 0), il = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("il", 0), postaKodu = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("postaKodu", 0), ulke = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("ulke", 0) } });
}

        [HttpPost("UpdateCariAdres")]
        public async Task<IActionResult> UpdateCariAdres(string cardCode, string ilce, string il, string sokak, string postaKodu, string ulke, string objType = "15")
 {return Json(new { success = true, message = "Adres Muhatap Ana Verisi'ne başarıyla kaydedildi. İsterseniz GİB'e gönderirken bu adresi kullanabilirsiniz." });
}

        [HttpGet("GetBelgeSofor")]
        public async Task<IActionResult> GetBelgeSofor(int docEntry, string objType)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.SoforModel>() });
}

        [HttpGet("GetKayitliSoforler")]
        public async Task<IActionResult> GetKayitliSoforler(string q)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.SoforModel>(12) });
}

        [HttpPost("YeniSoforKaydet")]
        public async Task<IActionResult> YeniSoforKaydet(string adSoyad, string plaka, string tckn, string tel = "")
 {return Json(new { success = true, message = "Şoför bilgileri havuza başarıyla eklendi." });
}

        [HttpPost("UpdateBelgeSofor")]
        public async Task<IActionResult> UpdateBelgeSofor(int docEntry, string objType, string plaka, string soforAdSoyad, string soforTckn)
 {return Json(new { success = true, message = "Şoför ve Araç bilgileri belgeye başarıyla kaydedildi. Belgeyi yeniden gönderebilirsiniz." });
}

        [HttpPost("MukellefSorgulaVeGuncelle")]
        public async Task<IActionResult> MukellefSorgulaVeGuncelle(string vkn, string cardCode)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), yeniDurum = global::WebApplication3.OrnekDoldurucu.Deger<string>("yeniDurum", 0) });
}

        [HttpPost("TopluMukellefSorgula")]
        public async Task<IActionResult> TopluMukellefSorgula([FromBody] List<TopluSorguIstekModel> istekListesi)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Vkn", "Tckn" }) });
}







        private static decimal SayiyaCevir(object deger, string alanAdi, string belgeBilgisi = "")
 {return default;
}








        public class IadeReferansBilgisi { public bool IadeMi { get; set; } public string FaturaNo { get; set; } public string Tarih { get; set; } public string BelgeTarihi { get; set; } }

        [HttpGet("IadeReferans")]
        public async Task<IActionResult> IadeReferans(int docEntry, string objType)
 {return Json(new { success = true, iadeMi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("iadeMi", 0), faturaNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("faturaNo", 0), tarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("tarih", 0), belgeTarihi = global::WebApplication3.OrnekDoldurucu.Deger<string>("belgeTarihi", 0), gecerli = global::WebApplication3.OrnekDoldurucu.Deger<bool>("gecerli", 0) });
}


        private async Task<string> IadeReferansYazAsync(string dbKey, int docEntry, string objType, string faturaNo, string tarih)
 {return default;
}

        [HttpGet("PreviewFatura")]
        public async Task<IActionResult> PreviewFatura(int docEntry, string objType, bool useCariAddress = false)
 {return Json(new { success = true, html = global::WebApplication3.OrnekDoldurucu.Deger<string>("html", 0), xml = global::WebApplication3.OrnekDoldurucu.Deger<string>("xml", 0) });
}





        [HttpGet("XmlIndir")]
        public async Task<IActionResult> XmlIndir(int docEntry, string objType, bool useCariAddress = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("DownloadPreview")]
        public async Task<IActionResult> DownloadPreview(int docEntry, string objType, bool useCariAddress = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("GetSeriesStatus")]
        public async Task<IActionResult> GetSeriesStatus(string prefixesStr)
 {return Json(new { success = true, data = new object[0] });
}

        [HttpGet("GetNextSequence")]
        public async Task<IActionResult> GetNextSequence(string prefix)
 {return Json(new { success = true, nextNumber = global::WebApplication3.OrnekDoldurucu.Deger<string>("nextNumber", 0) });
}

        private async Task<(bool isSuccess, string errorMessage)> UpdateNumAtCardServiceLayerAsync(int docEntry, string objType, string gibFaturaNo, string dbKey, string uuid = null)
 {return default;
}

        private async Task<QnbBelgeDurumModel> CheckQnbStatusAsync(string oid, string vkn, string user, string pass)
 {return default;
}
        [HttpPost("FaturaGonderOtomatik")]
        public async Task<IActionResult> FaturaGonderOtomatik(int docEntry, string objType, string gibFaturaNo, bool useCariAddress = false, string iadeFaturaNo = null, string iadeFaturaTarihi = null)
 {return Json(new { success = true, dogrulandi = false, beklemede = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), oid = global::WebApplication3.OrnekDoldurucu.Deger<string>("oid", 0), tur = global::WebApplication3.OrnekDoldurucu.Deger<string>("tur", 0), gibNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("gibNo", 0) });
}













        private sealed class OnayBekleyenKayit
        {
            public int DocEntry { get; set; }
            public string ObjType { get; set; }
            public string SapBelgeNo { get; set; }
            public string GibFaturaNo { get; set; }
            public string Oid { get; set; }
            public string Uuid { get; set; }
            public string Durum { get; set; }
            public string Mesaj { get; set; }
            public DateTime Tarih { get; set; }
        }

        private (string vkn, string user, string pass) QnbKimlik(string dbKey)
 {return default;
}



        private async Task<(string sonuc, string mesaj, QnbBelgeDurumModel durum)> OnayBekleyenTamamlaAsync(string dbKey, OnayBekleyenKayit k)
 {return default;
}


        [HttpGet("OnayBekleyenleriKontrol")]
        public async Task<IActionResult> OnayBekleyenleriKontrol()
 {return Json(new { success = true, kontrol = global::WebApplication3.OrnekDoldurucu.Deger<int>("kontrol", 0), tamamlanan = global::WebApplication3.OrnekDoldurucu.Deger<int>("tamamlanan", 0), hatali = global::WebApplication3.OrnekDoldurucu.Deger<int>("hatali", 0), bekleyen = global::WebApplication3.OrnekDoldurucu.Deger<int>("bekleyen", 0), sorguHatasi = global::WebApplication3.OrnekDoldurucu.Deger<int>("sorguHatasi", 0), detay = new object[0] });
}

        [HttpGet("DurumSorgula")]
        public async Task<IActionResult> DurumSorgula(string oid, int? docEntry = null, string objType = null)
 {return Json(new { success = true, sorguHatasi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("sorguHatasi", 0), hatali = global::WebApplication3.OrnekDoldurucu.Deger<bool>("hatali", 0), kesin = global::WebApplication3.OrnekDoldurucu.Deger<bool>("kesin", 0), durumKodu = global::WebApplication3.OrnekDoldurucu.Deger<int>("durumKodu", 0), mesaj = global::WebApplication3.OrnekDoldurucu.Deger<string>("mesaj", 0), ortam = global::WebApplication3.OrnekDoldurucu.Deger<string>("ortam", 0), tamamlandi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("tamamlandi", 0), sapYazildi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("sapYazildi", 0), reddedildi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("reddedildi", 0), tamamlandiMesaj = global::WebApplication3.OrnekDoldurucu.Deger<string>("tamamlandiMesaj", 0) });
}






        [HttpGet("EArsivAyarlari")]
        public async Task<IActionResult> EArsivAyarlari()
 {return Json(new { success = true, ortam = global::WebApplication3.OrnekDoldurucu.Deger<string>("ortam", 0), sirket = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirket", 0), vkn = global::WebApplication3.OrnekDoldurucu.Deger<string>("vkn", 0), kullanici = global::WebApplication3.OrnekDoldurucu.Deger<string>("kullanici", 0), gonderilen = new { sube = global::WebApplication3.OrnekDoldurucu.Deger<string>("sube", 0), kasa = global::WebApplication3.OrnekDoldurucu.Deger<string>("kasa", 0), erpKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("erpKodu", 0) }, sonucKodu = global::WebApplication3.OrnekDoldurucu.Deger<string>("sonucKodu", 0), mesaj = global::WebApplication3.OrnekDoldurucu.Deger<string>("mesaj", 0), ekler = global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, string>>(), ham = global::WebApplication3.OrnekDoldurucu.Deger<string>("ham", 0) });
}

        [HttpGet("GetGunlukIslemGecmisi")]
        public async Task<IActionResult> GetGunlukIslemGecmisi()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "ObjType", "SapBelgeNo", "GibFaturaNo", "Oid", "IslemTipi", "Durum", "Mesaj", "Tarih" }) });
}








        [HttpGet("GetGidenBelgeler")]
        public async Task<IActionResult> GetGidenBelgeler(string tur = "", string ara = "", string bas = "", string bit = "")
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "SapBelgeNo", "GibNo", "Uuid", "Oid", "Kanal", "BelgeTuru", "Yon", "Senaryo", "Durum", "Mesaj", "Tarih", "QnbTuru", "Kaynak", "CariAdi" }), mesaj = "Bu şirkette e-belge log tablosu yok." });
}













        [HttpGet("GidenBelgeIndirTek")]
        public async Task<IActionResult> GidenBelgeIndirTek(string oid, string belgeTuru = "FATURA", string format = "PDF",
            string kanal = null, string faturaNo = null, string uuid = null)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}





        private static byte[] ZipIcindekiTekDosya(byte[] veri)
 {return default;
}

        [HttpPost("QnbGidenBelgeIndir")]
        public async Task<IActionResult> QnbGidenBelgeIndir([FromBody] string[] belgeOidListesi, string belgeTuru = "IRSALIYE", string belgeFormati = "UBL")
 {return Json(new { success = true, message = "Lütfen indirilecek belgelerin OID'lerini belirtin." });
}





        private static List<string> NakliyeNotSatirlari(dynamic header)
 {return default;
}

        private string GetXsltBase64(bool isIrsaliye, string dbKey)
 {return default;
}

        private async Task<string> GenerateUblXmlFromSapAsync(int docEntry, string objType, string dbKey, string gibFaturaNo = null, bool useCariAddress = false)
 {return default;
}
        private async Task<string> TransformXmlWithXsltAsync(string xmlContent, bool isIrsaliye, string dbKey)
 {return default;
}

        private string GetWsSecurityHeader(string user, string pass)  {return default;
}

        private string GetMD5Hash(byte[] gelen)
 {return default;
}

        private string SayiyiYaziyaCevir(decimal tutar, string paraBirimi)
 {return default;
}









        public class EBelgeIbanSatiri
        {
            public int Id { get; set; }
            public int Sira { get; set; }
            public string BankaAdi { get; set; }
            public string SubeAdi { get; set; }
            public string ParaBirimi { get; set; }
            public string Iban { get; set; }
            public bool FaturadaGoster { get; set; }
        }

        private static readonly string[] IbanParaBirimleri = { "TRY", "USD", "EUR", "GBP", "CHF" };



        private static string IbanNormallestir(string iban)
 {return default;
}

        private List<EBelgeIbanSatiri> EBelgeIbanlariOku(string dbKey, bool yalnizGosterilecek)
 {return default;
}



        private string BankaHesaplariXmlUret(string dbKey)
 {return default;
}

        [HttpGet("IbanListesi")]
        public IActionResult IbanListesi()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor88Controller.EBelgeIbanSatiri>(12) });
}

        [HttpPost("IbanKaydet")]
        public IActionResult IbanKaydet(int? id, string bankaAdi, string subeAdi, string paraBirimi, string iban, bool faturadaGoster = true)
 {return Json(new { success = true });
}


        [HttpPost("IbanGosterAyarla")]
        public IActionResult IbanGosterAyarla(int id, bool faturadaGoster)
 {return Json(new { success = true });
}

        [HttpPost("IbanSil")]
        public IActionResult IbanSil(int id)
 {return Json(new { success = true });
}

        [HttpGet("GetActiveSeriesList")]
        public async Task<IActionResult> GetActiveSeriesList(int docEntry, string objType)
 {return Json(new { success = true, data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("data", i2)).ToList() });
}


        private bool SeriKullaniciyaAcikMi(string yetkiliKullanicilar)
 {return default;
}

        [HttpGet("SeriYonetimListe")]
        public async Task<IActionResult> SeriYonetimListe()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Id", "Seri", "BelgeTipi", "Yon", "Aciklama", "Aktif", "YetkiliKullanicilar" }) });
}

        [HttpPost("SeriKaydet")]
        public async Task<IActionResult> SeriKaydet([FromBody] SeriKayitModel m)
 {return Json(new { success = true });
}


        private static string KullaniciListesiNormalize(string liste)
 {return default;
}

        public class SeriYetkiModel { public int Id { get; set; } public string YetkiliKullanicilar { get; set; } }


        [HttpPost("SeriYetkiGuncelle")]
        public async Task<IActionResult> SeriYetkiGuncelle([FromBody] SeriYetkiModel m)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SeriSilDb")]
        public async Task<IActionResult> SeriSilDb([FromBody] SeriSilModel m)
 {return Json(new { success = true });
}


        [HttpGet("SeriGonderimListe")]
        public async Task<IActionResult> SeriGonderimListe(int docEntry, string objType)
 {return Json(new { success = true, belgeTipi = global::WebApplication3.OrnekDoldurucu.Deger<string>("belgeTipi", 0), yon = global::WebApplication3.OrnekDoldurucu.Deger<string>("yon", 0), data = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { prefix = global::WebApplication3.OrnekDoldurucu.Deger<string>("prefix", i2), desc = global::WebApplication3.OrnekDoldurucu.Deger<string>("desc", i2) }).ToList() });
}

        public class SeriKayitModel { public string Seri { get; set; } public string BelgeTipi { get; set; } public string Yon { get; set; } public string Aciklama { get; set; } public string YetkiliKullanicilar { get; set; } }
        public class SeriSilModel { public int Id { get; set; } }

        private string SayiOku(long sayi)
 {return default;
}
    }
}