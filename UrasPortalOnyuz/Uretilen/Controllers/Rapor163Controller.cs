// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication3.Services;
using ClosedXML.Excel;
using System.IO;

namespace WebApplication3.Controllers
{







    [Authorize]
    [Route("[controller]")]
    public class Rapor163Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor163Controller> _logger;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA", DbName = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection11", Display = "TestUrasKimya", DbName = "TestUrasKimya" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
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
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS", DbName = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
            new DatabaseConfig { Key = "DefaultConnection37", Display = "RGB_TEKSTIL", DbName = "RGB_TEKSTIL" },
        };

        private readonly QnbEArsivServisi _eArsiv;
        private readonly EmailService _email;

        private string PortalUrl()  {return default;
}
        private static string H(string s)  {return default;
}

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}


        private string DbKeyForDbName(string dbName)
 {return default;
}


        private static string TurBelirle(string hesapKodu, Dictionary<string, string> ov)
 {return default;
}

        [HttpGet]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        private bool Onaylayici()
 {return default;
}


        [HttpGet("Kullanicilar")]
        public IActionResult Kullanicilar()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "kod", "ad", "mail" }) });
}

        public class AtamaIstek { public List<string> Idler { get; set; } public string Kullanici { get; set; } public string Not { get; set; } }


        [HttpPost("AtamaYap")]
        public IActionResult AtamaYap([FromBody] AtamaIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), mailUyari = global::WebApplication3.OrnekDoldurucu.Deger<bool>("mailUyari", 0) });
}


        [HttpGet("Atamalar")]
        public IActionResult Atamalar()
 {return Json(new { success = true, onaylayici = global::WebApplication3.OrnekDoldurucu.Deger<bool>("onaylayici", 0), kullanici = global::WebApplication3.OrnekDoldurucu.Deger<string>("kullanici", 0), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Services.GiderGelirKatalog.SatirAtama>(12) });
}

        public class AtamaSilIstek { public int Id { get; set; } public string Not { get; set; } }


        [HttpPost("AtamaSil")]
        public IActionResult AtamaSil([FromBody] AtamaSilIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), mailUyari = global::WebApplication3.OrnekDoldurucu.Deger<bool>("mailUyari", 0) });
}

        public class AtamaKodIstek { public int Id { get; set; } public string Kod { get; set; } }





        [HttpPost("AtamaKodKaydet")]
        public async Task<IActionResult> AtamaKodKaydet([FromBody] AtamaKodIstek req)
 {return Json(new { success = true, kod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kod", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("OrtakCariTara")]
        public IActionResult OrtakCariTara()
 {return Json(new { success = true, toplam = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplam", 0), sirketler = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName", "CardType", "LicTradNum" }), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpGet("OrtakCariler")]
        public IActionResult OrtakCariler(string q = null, bool sadeceSabitsiz = false, int enAz = 2)
 {return Json(new { success = true, taramaZamani = global::WebApplication3.OrnekDoldurucu.Deger<string>("taramaZamani", 0), toplam = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplam", 0), data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Id", "SirketKodu", "CardCode", "Tip", "KokKod", "Durum", "KalemAdi" }), onaylayici = global::WebApplication3.OrnekDoldurucu.Deger<bool>("onaylayici", 0) });
}

        public class OrtakSabitIstek { public string Vkn { get; set; } public string Tip { get; set; } public string Kod { get; set; } public string Aciklama { get; set; } public List<OrtakHedef> Hedefler { get; set; } }
        public class OrtakHedef { public string SirketKodu { get; set; } public string DbName { get; set; } public string CardCode { get; set; } public string CardName { get; set; } }


        [HttpPost("OrtakSabitle")]
        public IActionResult OrtakSabitle([FromBody] OrtakSabitIstek req)
 {return Json(new { success = true, onayli = global::WebApplication3.OrnekDoldurucu.Deger<bool>("onayli", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), yazilan = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("yazilan", i2)).ToList(), hatalar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatalar", i2)).ToList() });
}


        [HttpGet("Cariler")]
        public IActionResult Cariler(string q)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName", "CardType" }) });
}





        private int SabitleriUygula(List<BelgeSatir> satirlar)
 {return default;
}


        [HttpGet("Veri")]
        public IActionResult Veri(int yil = 0, int ay = 0)
 {return Json(new { success = true, sablon = false, yil = global::WebApplication3.OrnekDoldurucu.Deger<int>("yil", 0), ay = global::WebApplication3.OrnekDoldurucu.Deger<int>("ay", 0), kodsuz = global::WebApplication3.OrnekDoldurucu.Deger<int>("kodsuz", 0), sabitUygulanan = global::WebApplication3.OrnekDoldurucu.Deger<int>("sabitUygulanan", 0), gelir = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor163Controller.BelgeSatir>(12), gider = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor163Controller.BelgeSatir>(12) });
}




        [HttpGet("Excel")]
        public IActionResult Excel(int yil = 0, int ay = 0)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        public class GelmeyenSatir
        {
            public string Id { get; set; }
            public int TransId { get; set; }
            public int LineId { get; set; }
            public string Tarih { get; set; }
            public string BelgeTuru { get; set; }
            public string BelgeNo { get; set; }
            public string Cari { get; set; }
            public string HesapKodu { get; set; }
            public string HesapAdi { get; set; }
            public decimal Borc { get; set; }
            public decimal Alacak { get; set; }
            public string Sebep { get; set; }
        }





        [HttpGet("Gelmeyenler")]
        public IActionResult Gelmeyenler(int yil = 0, int ay = 0)
 {return Json(new { success = true, yil = global::WebApplication3.OrnekDoldurucu.Deger<int>("yil", 0), ay = global::WebApplication3.OrnekDoldurucu.Deger<int>("ay", 0), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor163Controller.GelmeyenSatir>(12) });
}

        private List<GelmeyenSatir> GelmeyenSatirlariYukle(DateTime bas, DateTime bit)
 {return default;
}





        [HttpGet("KodaGore")]
        public IActionResult KodaGore(string kod, DateTime? baslangic, DateTime? bitis)
 {return Json(new { success = true, kok = global::WebApplication3.OrnekDoldurucu.Deger<string>("kok", 0), data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor163Controller.BelgeSatir>(12), toplamBorc = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("toplamBorc", 0), toplamAlacak = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("toplamAlacak", 0), adet = global::WebApplication3.OrnekDoldurucu.Deger<int>("adet", 0) });
}

        [HttpGet("KodaGoreExcel")]
        public IActionResult KodaGoreExcel(string kod, DateTime? baslangic, DateTime? bitis)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private List<BelgeSatir> KodaGoreSatirlar(string kod, DateTime bas, DateTime bit)
 {return default;
}


        private static byte[] SatirlariExcelYap(IEnumerable<(string Ad, List<BelgeSatir> Satirlar)> sayfalar)
 {return default;
}





        private List<BelgeSatir> SatirlariYukle(DateTime bas, DateTime bit, out int sabitUygulanan, string kokKod = null)
 {sabitUygulanan = default;
return default;
}

        private static string FaturaSatirTablosu(int tt)  {return default;
}
        private static string CariTipAdi(string t)  {return default;
}




        [HttpPost("KodKaydet")]
        public async Task<IActionResult> KodKaydet([FromBody] KodKayitIstek req)
 {return Json(new { success = true, kismi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("kismi", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), yazilan = global::WebApplication3.OrnekDoldurucu.Deger<int>("yazilan", 0), hatalar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatalar", i2)).ToList(), hatali = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatali", i2)).ToList() });
}


        [HttpGet("AlanDurumu")]
        public IActionResult AlanDurumu()
 {return Json(new { success = true, jdt1 = global::WebApplication3.OrnekDoldurucu.Deger<bool>("jdt1", 0), inv1 = global::WebApplication3.OrnekDoldurucu.Deger<bool>("inv1", 0) });
}

        [HttpPost("GiderAlaniniHazirla")]
        public async Task<IActionResult> GiderAlaniniHazirla()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), olusturuldu = global::WebApplication3.OrnekDoldurucu.Deger<bool>("olusturuldu", 0) });
}





        private async Task<(bool Ok, string Mesaj, bool Olusturuldu)> AlaniAcAsync(string dbKey)
 {return default;
}


        [HttpGet("BelgeDetay")]
        public IActionResult BelgeDetay(int transId)
 {return Json(new { success = true, baslik = global::WebApplication3.OrnekDoldurucu.Deger<string>("baslik", 0), belgeNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("belgeNo", 0), tarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("tarih", 0), cari = global::WebApplication3.OrnekDoldurucu.Deger<string>("cari", 0), transType = global::WebApplication3.OrnekDoldurucu.Deger<int>("transType", 0), faturaMi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("faturaMi", 0), belge = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("belge", 0), kisiler = (object)global::WebApplication3.OrnekDoldurucu.Deger<string>("kisiler", 0), cariler = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate", "CardCode", "CardName", "NumAtCard", "DocTotal", "VatSum", "DocCur", "DocRate", "Comments", "DocStatus", "CANCELED", "CreateDate", "CreateTS", "UpdateDate", "draftKey", "JrnlMemo", "DocType", "EkleyenKod", "EkleyenAd", "GuncelleyenKod", "GuncelleyenAd", "TaslakKod", "TaslakAd", "TaslakTarih", "TaslakSaat", "TaslakEntry", "TaslakAciklama", "SatinalmaciKod", "SatinalmaciAd", "Satici" }), fisSatirlari = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate", "CardCode", "CardName", "NumAtCard", "DocTotal", "VatSum", "DocCur", "DocRate", "Comments", "DocStatus", "CANCELED", "CreateDate", "CreateTS", "UpdateDate", "draftKey", "JrnlMemo", "DocType", "EkleyenKod", "EkleyenAd", "GuncelleyenKod", "GuncelleyenAd", "TaslakKod", "TaslakAd", "TaslakTarih", "TaslakSaat", "TaslakEntry", "TaslakAciklama", "SatinalmaciKod", "SatinalmaciAd", "Satici" }), kalemler = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "DocEntry", "DocNum", "DocDate", "DocDueDate", "TaxDate", "CardCode", "CardName", "NumAtCard", "DocTotal", "VatSum", "DocCur", "DocRate", "Comments", "DocStatus", "CANCELED", "CreateDate", "CreateTS", "UpdateDate", "draftKey", "JrnlMemo", "DocType", "EkleyenKod", "EkleyenAd", "GuncelleyenKod", "GuncelleyenAd", "TaslakKod", "TaslakAd", "TaslakTarih", "TaslakSaat", "TaslakEntry", "TaslakAciklama", "SatinalmaciKod", "SatinalmaciAd", "Satici" }) });
}






        private class FaturaBilgi
        {
            public int TransType, DocEntry, DocNum;
            public string Tablo, CardCode, CardName, NumAtCard, Uuid, EfatNo, VknCari;
            public DateTime? DocDate;
            public string Yon => (TransType == 18 || TransType == 19) ? "GELEN" : "GIDEN";
        }
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _faturaEttn = new System.Collections.Concurrent.ConcurrentDictionary<string, string>();
        private static string FaturaBaslikTablosu(int tt)  {return default;
}


        private async Task<(FaturaBilgi F, string Ettn, string Hata, string Ipucu)> FaturaEttnCozAsync(string dbKey, int transId)
 {return default;
}


        [HttpGet("FaturaGoruntuDurum")]
        public async Task<IActionResult> FaturaGoruntuDurum(int transId)
 {return Json(new { success = true, bulundu = global::WebApplication3.OrnekDoldurucu.Deger<bool>("bulundu", 0), yon = global::WebApplication3.OrnekDoldurucu.Deger<string>("yon", 0), belgeNo = global::WebApplication3.OrnekDoldurucu.Deger<int>("belgeNo", 0), faturaNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("faturaNo", 0), kanal = global::WebApplication3.OrnekDoldurucu.Deger<string>("kanal", 0), mesaj = global::WebApplication3.OrnekDoldurucu.Deger<string>("mesaj", 0), ipucu = global::WebApplication3.OrnekDoldurucu.Deger<string>("ipucu", 0) });
}


        [HttpGet("FaturaGoruntu")]
        public async Task<IActionResult> FaturaGoruntu(int transId, string format = "HTML")
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
        private static bool FaturaTipiMi(int tt)  {return default;
}

        private static string BelgeTuruAdi(int tt)
 {return default;
}



        private static string KalemSorgusu(int tt)
 {return default;
}


        [HttpGet("Hesaplar")]
        public IActionResult Hesaplar()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "FormatCode", "AcctName" }) });
}


        [HttpGet("Override")]
        public IActionResult Override()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "HesapKodu", "HesapAdi", "Tur", "Aciklama" }) });
}


        [HttpPost("OverrideKaydet")]
        public IActionResult OverrideKaydet([FromBody] OverrideDto req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("OverrideSil")]
        public IActionResult OverrideSil([FromBody] OverrideDto req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("Onayla")]
        public IActionResult Onayla([FromBody] OnayIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class OnayIstek { public List<string> Idler { get; set; } }
        public class KodKayitIstek { public List<KodKayitSatir> Satirlar { get; set; } }
        public class KodKayitSatir { public int TransId { get; set; } public int LineId { get; set; } public string Kod { get; set; } }
        public class OverrideDto { public string HesapKodu { get; set; } public string HesapAdi { get; set; } public string Tur { get; set; } public string Aciklama { get; set; } }

        public class BelgeSatir
        {
            public string Id { get; set; }          // TransId-LineId
            public int TransId { get; set; }
            public int LineId { get; set; }
            public int TransType { get; set; }
            public string Kod { get; set; }         // gider/gelir kodu (U_BE1_GIDER); boşsa uyarı
            public bool KodKarisik { get; set; }    // fatura kalemleri farklı kodlanmış
            public string Tur { get; set; }
            public string Tarih { get; set; }
            public string BelgeTuru { get; set; }
            public string BelgeNo { get; set; }
            public string Cari { get; set; }
            public int FisCariSayisi { get; set; }   // manuel fişte cari satırı sayısı (kur farkı fişlerinde birden fazla olabilir)
            public string FisCariTip { get; set; }   // S tedarikçi / C müşteri (ilk cari)
            public string CardCode { get; set; }
            public List<string> OnerilenKodlar { get; set; }   // VKN bazlı geçmişten: carinin en son kullandığı en fazla 3 kod
            public bool SabitMi { get; set; }       // kod sabit cari tanımından otomatik yazıldı
            public string Atanan { get; set; }      // satır atanan kullanıcı (kod · ad)
            public string OnayDurum { get; set; }   // BEKLIYOR / ONAYLANDI / RED
            public string OnayAciklama { get; set; }
            public string OnayTarihi { get; set; }
            public string OnayKod { get; set; }
            public string HesapKodu { get; set; }
            public string HesapAdi { get; set; }
            public decimal Borc { get; set; }
            public decimal Alacak { get; set; }
            public decimal Tutar { get; set; }
            public bool FaturaMi { get; set; }
            public string Aciklama { get; set; }
        }
    }
}
