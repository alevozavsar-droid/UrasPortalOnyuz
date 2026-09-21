// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Xml.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{



    public class OnayTaslak
    {
        public int DocEntry { get; set; }
        public int? DocNum { get; set; }
        public string ObjType { get; set; }
        public string BelgeTipiAdi { get; set; }

        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string NumAtCard { get; set; }

        public DateTime? DocDate { get; set; }
        public DateTime? DocDueDate { get; set; }
        public DateTime? TaxDate { get; set; }
        public DateTime? CreateDate { get; set; }

        public decimal DocTotal { get; set; }
        public decimal VatSum { get; set; }
        public decimal DiscSum { get; set; }
        public string DocCur { get; set; }
        public decimal DocRate { get; set; }

        public string Comments { get; set; }
        public int OwnerCode { get; set; }
        public string SahipAdi { get; set; }
        public int SatirSayisi { get; set; }

        public string ParaBirimi => string.IsNullOrWhiteSpace(DocCur) ? "TRY" : DocCur;


        public string SirketDb { get; set; }
        public string SirketAdi { get; set; }

        public int BeklemeGunu => CreateDate.HasValue ? (int)(DateTime.Today - CreateDate.Value.Date).TotalDays : 0;

        public string BeklemeDurumu =>
            BeklemeGunu >= 7 ? "gecikmis" : BeklemeGunu >= 3 ? "bekliyor" : "yeni";
    }


    public class OnayTaslakSatir
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string Dscription { get; set; }
        public decimal Quantity { get; set; }
        public string UnitMsr { get; set; }
        public decimal Price { get; set; }
        public decimal DiscPrcnt { get; set; }
        public decimal LineTotal { get; set; }
        public decimal VatPrcnt { get; set; }
        public string VatGroup { get; set; }
        public string WhsCode { get; set; }
        public string AcctCode { get; set; }
        public string HesapAdi { get; set; }
        public string OcrCode { get; set; }
    }


    public class OnayLogKayit
    {
        public int Id { get; set; }
        public string SirketDb { get; set; }
        public string ObjType { get; set; }
        public int TaslakDocEntry { get; set; }
        public int? TaslakDocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal? DocTotal { get; set; }
        public string Islem { get; set; }
        public int? SonucDocEntry { get; set; }
        public int? SonucDocNum { get; set; }
        public string Aciklama { get; set; }
        public string KullaniciKodu { get; set; }
        public DateTime? IslemTarihi { get; set; }

        public string BelgeTipiAdi { get; set; }
        public string SahipAdi { get; set; }
    }


    public class OnaySahip
    {
        public int OwnerCode { get; set; }
        public string SahipAdi { get; set; }
        public int Adet { get; set; }
        public decimal Tutar { get; set; }
    }

    public class OnayIstatistik
    {
        public int Bekleyen { get; set; }
        public decimal ToplamTutar { get; set; }
        public int Gecikmis { get; set; }
        public int BugunGelen { get; set; }
        public int SahipSayisi { get; set; }
    }

    public class Rapor143ViewModel
    {
        public List<OnayTaslak> Taslaklar { get; set; } = new List<OnayTaslak>();
        public OnayIstatistik Istatistik { get; set; } = new OnayIstatistik();

        public string KullaniciKodu { get; set; }
        public string KullaniciAdi { get; set; }
        public int? PersonelKodu { get; set; }     // OHEM.empID
        public string SecilenDbAdi { get; set; }
        public string BelgeTipi { get; set; } = "18";
        public string Hata { get; set; }
        public string Uyari { get; set; }


        public bool YoneticiMi { get; set; }


        public List<OnaySahip> Sahipler { get; set; } = new List<OnaySahip>();


        public int? SahipFiltre { get; set; }


        public bool Konsolide { get; set; }


        public List<OnayLogKayit> Gecmis { get; set; } = new List<OnayLogKayit>();
    }














    [Authorize]
    [Route("OnayEkrani")]
    public class Rapor143Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor143Controller> _logger;

        private static bool _semaHazir = false;


        private static readonly string LogBaglantiAnahtari = "DefaultConnection";
        private static readonly string LogDbAdi = "URASKIMYA";


        private static readonly Dictionary<string, (string Ad, string Servis)> BelgeTipleri =
            new Dictionary<string, (string, string)>
        {
            { "18", ("Satıcı Faturası",   "PurchaseInvoices") },
            { "13", ("Müşteri Faturası",  "Invoices") },
            { "22", ("Satınalma Siparişi", "PurchaseOrders") },
            { "17", ("Satış Siparişi",     "Orders") },
            { "19", ("Satıcı İade / Borç Dekontu", "PurchaseCreditNotes") },
            { "14", ("Müşteri İade / Alacak Dekontu", "CreditNotes") }
        };

        private static List<DatabaseConfig> _sirketOnbellek;
        private static readonly object _sirketKilit = new object();







        private List<DatabaseConfig> Sirketler()
 {return default;
}


        private static int SirketSirasi(string key)
 {return default;
}



        private string KullaniciKodu()  {return default;
}


        private static readonly HashSet<string> YoneticiKodlari =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "IT02", "IT01" };


        private static readonly HashSet<string> YoneticiYetkileri =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "A", "S", "Y" };





        private bool YoneticiMi()
 {return default;
}

        private string SecilenDbKey()
 {return default;
}

        private string SecilenDbAdi(string key)  {return default;
}

        private string SecilenDbName(string key)  {return default;
}

        private string Baglanti(string key)  {return default;
}







        private async Task SemaHazirlaAsync()
 {}









        private string SlBaseUrl()  {return default;
}





        private HttpClient SlClient()
 {return default;
}






        private async Task<string> SlLoginAsync(HttpClient client, string companyDb)
 {return default;
}


        private static string KokHata(Exception ex)
 {return default;
}

        private static async Task SlLogoutAsync(HttpClient client)
 {}


        private static string SlHataMetni(string govde)
 {return default;
}






        private List<DatabaseConfig> YetkiliSirketler()
 {return default;
}

        [HttpGet]
        public async Task<IActionResult> Index(string belgeTipi = "18", int? sahip = null, bool konsolide = false)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor143ViewModel>());
}


        [HttpGet("Detay")]
        public async Task<IActionResult> Detay(int docEntry)
 {return Json(new { success = true, baslik = new { DocEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("DocEntry", 0), DocNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("DocNum", 0), ObjType = global::WebApplication3.OrnekDoldurucu.Deger<string>("ObjType", 0), CardCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("CardCode", 0), CardName = global::WebApplication3.OrnekDoldurucu.Deger<string>("CardName", 0), NumAtCard = global::WebApplication3.OrnekDoldurucu.Deger<string>("NumAtCard", 0), Comments = global::WebApplication3.OrnekDoldurucu.Deger<string>("Comments", 0), belgeTipi = global::WebApplication3.OrnekDoldurucu.Deger<string>("belgeTipi", 0), sirketDb = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirketDb", 0), sirketAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirketAdi", 0), docDate = global::WebApplication3.OrnekDoldurucu.Deger<string>("docDate", 0), docDueDate = global::WebApplication3.OrnekDoldurucu.Deger<string>("docDueDate", 0), taxDate = global::WebApplication3.OrnekDoldurucu.Deger<string>("taxDate", 0), createDate = global::WebApplication3.OrnekDoldurucu.Deger<string>("createDate", 0), docTotal = global::WebApplication3.OrnekDoldurucu.Deger<string>("docTotal", 0), vatSum = global::WebApplication3.OrnekDoldurucu.Deger<string>("vatSum", 0), discSum = global::WebApplication3.OrnekDoldurucu.Deger<string>("discSum", 0), araToplam = global::WebApplication3.OrnekDoldurucu.Deger<string>("araToplam", 0), paraBirimi = global::WebApplication3.OrnekDoldurucu.Deger<string>("paraBirimi", 0), OwnerCode = global::WebApplication3.OrnekDoldurucu.Deger<int>("OwnerCode", 0), sahipAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("sahipAdi", 0), benimMi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("benimMi", 0), yonetici = global::WebApplication3.OrnekDoldurucu.Deger<bool>("yonetici", 0) }, satirlar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { LineNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("LineNum", i2), ItemCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("ItemCode", i2), Dscription = global::WebApplication3.OrnekDoldurucu.Deger<string>("Dscription", i2), miktar = global::WebApplication3.OrnekDoldurucu.Deger<string>("miktar", i2), UnitMsr = global::WebApplication3.OrnekDoldurucu.Deger<string>("UnitMsr", i2), fiyat = global::WebApplication3.OrnekDoldurucu.Deger<string>("fiyat", i2), iskonto = global::WebApplication3.OrnekDoldurucu.Deger<string>("iskonto", i2), tutar = global::WebApplication3.OrnekDoldurucu.Deger<string>("tutar", i2), kdv = global::WebApplication3.OrnekDoldurucu.Deger<string>("kdv", i2), VatGroup = global::WebApplication3.OrnekDoldurucu.Deger<string>("VatGroup", i2), WhsCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("WhsCode", i2), hesap = global::WebApplication3.OrnekDoldurucu.Deger<string>("hesap", i2), OcrCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("OcrCode", i2) }).ToList() });
}





        [HttpGet("BekleyenSayisi")]
        public async Task<IActionResult> BekleyenSayisi()
 {return Json(new { success = true, yonetici = global::WebApplication3.OrnekDoldurucu.Deger<bool>("yonetici", 0), seciliSirket = global::WebApplication3.OrnekDoldurucu.Deger<string>("seciliSirket", 0), rozet = global::WebApplication3.OrnekDoldurucu.Deger<int>("rozet", 0), benim = global::WebApplication3.OrnekDoldurucu.Deger<int>("benim", 0), havuz = global::WebApplication3.OrnekDoldurucu.Deger<int>("havuz", 0), toplamBenim = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplamBenim", 0), toplamHavuz = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplamHavuz", 0), sirketler = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Havuz", "Benim" }) });
}





        [HttpGet("Onizleme")]
        public async Task<IActionResult> Onizleme(int docEntry)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}


        [HttpGet("Ekler")]
        public async Task<IActionResult> Ekler(int docEntry)
 {return Json(new { success = true, ekler = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { line = global::WebApplication3.OrnekDoldurucu.Deger<int>("line", i2), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", i2), uzanti = global::WebApplication3.OrnekDoldurucu.Deger<string>("uzanti", i2), tarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("tarih", i2), mevcut = global::WebApplication3.OrnekDoldurucu.Deger<bool>("mevcut", i2), goruntulenebilir = global::WebApplication3.OrnekDoldurucu.Deger<bool>("goruntulenebilir", i2) }).ToList() });
}


        [HttpGet("EkGoster")]
        public async Task<IActionResult> EkGoster(int docEntry, int line)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}









        private static readonly string QnbCanliAdres = "https://efaturaconnector.qnbesolutions.com.tr/connector/ws/connectorService?wsdl";
        private static readonly string QnbAdAlani = "http://service.connector.uut.cs.com.tr/";

        private static string HataSayfasi(string baslik, string mesaj, string ipucu = null)
 {return default;
}

        private string QnbZarf(string user, string pass, string govde)  {return default;
}

        private async Task<(XDocument Doc, string Hata)> QnbGonderAsync(string url, string zarf)
 {return default;
}

        private static byte[] Base64Coz(string raw)
 {return default;
}





        private IActionResult BelgeVerisiniGoster(string raw, string dosyaAdi)
 {return default;
}


        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _ettnOnbellek =
            new System.Collections.Concurrent.ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);












        private async Task<(string Ettn, string Hata)> QnbEttnBulAsync(string url, string user, string pass, string vkn, string dbKey,
                                                                        List<string> faturaNolari, DateTime? belgeTarihi)
 {return default;
}


        [HttpGet("QnbGoruntuDurum")]
        public async Task<IActionResult> QnbGoruntuDurum(int docEntry)
 {return Json(new { success = true, bulundu = global::WebApplication3.OrnekDoldurucu.Deger<bool>("bulundu", 0), mesaj = global::WebApplication3.OrnekDoldurucu.Deger<string>("mesaj", 0), ipucu = global::WebApplication3.OrnekDoldurucu.Deger<string>("ipucu", 0) });
}

        [HttpGet("QnbGoruntu")]
        public async Task<IActionResult> QnbGoruntu(int docEntry, string format = "HTML")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}









        public static (bool Ok, string Mesaj) FaturaNoKontrol(string no)
 {return default;
}

        private static readonly System.Text.RegularExpressions.Regex MailDeseni =
            new System.Text.RegularExpressions.Regex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", System.Text.RegularExpressions.RegexOptions.Compiled);





        [HttpGet("MailAdresleri")]
        public async Task<IActionResult> MailAdresleri(int docEntry)
 {return Json(new { success = true, kullaniciMail = global::WebApplication3.OrnekDoldurucu.Deger<string>("kullaniciMail", 0), adresler = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "EMail", "Notes", "FreeText" }), konu = global::WebApplication3.OrnekDoldurucu.Deger<string>("konu", 0), govde = global::WebApplication3.OrnekDoldurucu.Deger<string>("govde", 0), kontrol = new { ok = global::WebApplication3.OrnekDoldurucu.Deger<bool>("ok", 0), mesaj = global::WebApplication3.OrnekDoldurucu.Deger<string>("mesaj", 0) }, numAtCard = global::WebApplication3.OrnekDoldurucu.Deger<string>("numAtCard", 0), cardName = global::WebApplication3.OrnekDoldurucu.Deger<string>("cardName", 0) });
}

        public class FaturaNoBildirIstek
        {
            public int DocEntry { get; set; }
            public List<string> Alicilar { get; set; }
            public string KendiMail { get; set; }
            public string Konu { get; set; }
            public string Govde { get; set; }
        }


        [HttpPost("FaturaNoBildir")]
        public async Task<IActionResult> FaturaNoBildir([FromBody] FaturaNoBildirIstek istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        private static string EkYoluBul(string trgtPath, string srcPath, string fileName, string fileExt)
 {return default;
}










        [HttpPost("Onayla")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Onayla(int docEntry, string onayNotu)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("docNum", 0) });
}





        [HttpPost("Reddet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reddet(int docEntry, string retNedeni)
 {return Json(new { success = true, message = "Taslak reddedildi ve açıklamasına not eklendi. Taslak silinmedi." });
}

        private async Task LoglaAsync(string dbKey, string companyDb, OnayTaslak taslak,
            string islem, int? sonucDocEntry, int? sonucDocNum, string aciklama)
 {}






        private static JObject HedefBelgeGovdesi(JObject taslak, string onayNotu)
 {return default;
}


    }
}
