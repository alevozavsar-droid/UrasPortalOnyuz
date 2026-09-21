// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{































    [Authorize]
    [Route("[controller]")]
    public class Rapor150Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor150Controller> _logger;

        private static readonly string MerkezBaglantiAnahtari = "DefaultConnection";
        private static bool _semaHazir = false;

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

        private string GetConnectionString(string dbKey)  {return default;
}


        private static List<Dictionary<string, object>> Sozluk(IEnumerable<dynamic> satirlar)
 {return default;
}

        private string KullaniciKodu()  {return default;
}
        private bool YoneticiMi()
 {return default;
}








        public static DateTime OnayGunuHesapla(int yil, int ay, string kanal)
 {return default;
}







        public class SegmentTanimi { public string Kod { get; set; } public string Ad { get; set; } public string Desen { get; set; } }

        public static readonly List<SegmentTanimi> YiSegmentleri = new List<SegmentTanimi>
        {
            new SegmentTanimi { Kod = "AVRASYA",   Ad = "Avrasya",         Desen = "AVRASYA" },
            new SegmentTanimi { Kod = "ASIA",      Ad = "Asia",            Desen = "ASIA DM" },   // "ASIA CHEMICAL/CHEMIQUE" gibi yabancı cariler değil, ASIA DM Kimya
            new SegmentTanimi { Kod = "OPERASYON", Ad = "Operasyon (MO)",  Desen = null },      // CardCode LIKE 'MO%'
            new SegmentTanimi { Kod = "DIGER",     Ad = "Diğer Satışlar",  Desen = null }
        };

        public static readonly List<SegmentTanimi> YdSegmentleri = new List<SegmentTanimi>
        {
            new SegmentTanimi { Kod = "DRN",   Ad = "DRN",           Desen = "DRN" },
            new SegmentTanimi { Kod = "DIGER", Ad = "Diğer İhracat", Desen = null }
        };

        private static readonly System.Globalization.CultureInfo TrKultur = new System.Globalization.CultureInfo("tr-TR");

        private static string SegmentBul(string kanal, string cardCode, string cardName, bool operasyon)
 {return default;
}

        private static bool DonemCoz(string donem, out int yil, out int ay)
 {yil = default;
ay = default;
return default;
}




        [HttpGet]
        public async Task<IActionResult> Index()
 {ViewBag.CurrentDbDisplay = "";
ViewBag.YoneticiMi = false;
ViewBag.VarsayilanDonem = "";
ViewBag.Cariler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor150Controller.CariModel>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}




        [HttpPost("RunReport")]
        public async Task<IActionResult> RunReport([FromBody] FiltreModel filtre)
 {return Json(new { success = true, donem = global::WebApplication3.OrnekDoldurucu.Deger<string>("donem", 0), donemAdi = global::WebApplication3.OrnekDoldurucu.Deger<string>("donemAdi", 0), yoneticiMi = global::WebApplication3.OrnekDoldurucu.Deger<bool>("yoneticiMi", 0), grupDahil = global::WebApplication3.OrnekDoldurucu.Deger<bool>("grupDahil", 0), yurtIci = new { satirlar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor150Controller.BelgeSatiri>(12), toplam = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor150Controller.ToplamModel>(), tumToplam = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor150Controller.ToplamModel>(), onay = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor150Controller.OnayBilgisi>(), segmentler = global::System.Linq.Enumerable.Range(0, 12).Select(i3 => new { kod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kod", i3), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", i3) }).ToList() }, yurtDisi = new { satirlar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor150Controller.BelgeSatiri>(12), toplam = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor150Controller.ToplamModel>(), tumToplam = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor150Controller.ToplamModel>(), onay = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor150Controller.OnayBilgisi>(), segmentler = global::System.Linq.Enumerable.Range(0, 12).Select(i3 => new { kod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kod", i3), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", i3) }).ToList() }, ozet = new { kanallar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor150Controller.ToplamModel>(12), grupIciToplam = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor150Controller.ToplamModel>(), grupDisiToplam = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor150Controller.ToplamModel>(), topCariler = global::System.Linq.Enumerable.Range(0, 12).Select(i3 => new { CardCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("CardCode", i3), CardName = global::WebApplication3.OrnekDoldurucu.Deger<string>("CardName", i3), Kanal = global::WebApplication3.OrnekDoldurucu.Deger<string>("Kanal", i3), netTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("netTL", i3), netUSD = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("netUSD", i3), netEUR = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("netEUR", i3), adet = global::WebApplication3.OrnekDoldurucu.Deger<int>("adet", i3) }).ToList(), paraBirimleri = global::System.Linq.Enumerable.Range(0, 12).Select(i3 => new { paraBirimi = global::WebApplication3.OrnekDoldurucu.Deger<string>("paraBirimi", i3), adet = global::WebApplication3.OrnekDoldurucu.Deger<int>("adet", i3), netTL = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("netTL", i3) }).ToList() } });
}






        private static ToplamModel Toplam(string ad, string kod, IEnumerable<BelgeSatiri> satirlar, bool grupDahil = false)
 {return default;
}

        private OnayBilgisi OnayBilgisiOlustur(int yil, int ay, string kanal, Dictionary<string, DonemOnayKaydi> kayitlar)
 {return default;
}




        private async Task<List<BelgeSatiri>> BelgeleriOkuAsync(string dbKey, int yil, int ay, List<GrupDeseni> grupDesenleri)
 {return default;
}




        [HttpGet("FaturaDetay")]
        public async Task<IActionResult> FaturaDetay(string objType, int docEntry)
 {return Json(new { success = true, baslik = global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, object>>(), kalemler = global::WebApplication3.OrnekDoldurucu.Liste<global::System.Collections.Generic.Dictionary<string, object>>(12) });
}




        [HttpPost("FaturaOnay")]
        public async Task<IActionResult> FaturaOnay([FromBody] FaturaOnayIstek istek)
 {return Json(new { success = true, kullanici = global::WebApplication3.OrnekDoldurucu.Deger<string>("kullanici", 0), tarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("tarih", 0) });
}




        [HttpPost("DonemOnay")]
        public async Task<IActionResult> DonemOnay([FromBody] DonemOnayIstek istek)
 {return Json(new { success = true, kullanici = global::WebApplication3.OrnekDoldurucu.Deger<string>("kullanici", 0), tarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("tarih", 0) });
}


        [HttpGet("OnayGecmisi")]
        public async Task<IActionResult> OnayGecmisi()
 {return Json(new { success = true, liste = global::WebApplication3.OrnekDoldurucu.Liste<global::System.Collections.Generic.Dictionary<string, object>>(12) });
}




        [HttpGet("GrupSirketleri")]
        public async Task<IActionResult> GrupSirketleri()
 {return Json(new { success = true, liste = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor150Controller.GrupDeseni>(12), eslesen = global::WebApplication3.OrnekDoldurucu.Liste<global::System.Collections.Generic.Dictionary<string, object>>(12) });
}

        [HttpPost("GrupSirketEkle")]
        public async Task<IActionResult> GrupSirketEkle([FromBody] GrupDeseni istek)
 {return Json(new { success = true });
}

        [HttpPost("GrupSirketSil")]
        public async Task<IActionResult> GrupSirketSil(int id)
 {return Json(new { success = true });
}

        [HttpPost("GrupSirketAktif")]
        public async Task<IActionResult> GrupSirketAktif(int id, bool aktif)
 {return Json(new { success = true });
}




        private async Task<List<GrupDeseni>> GrupDesenleriniOkuAsync(bool yalnizAktif)
 {return default;
}

        private async Task<Dictionary<string, HaricKaydi>> HaricKayitlariOkuAsync(string dbKey, string donemKodu)
 {return default;
}

        private async Task<Dictionary<string, DonemOnayKaydi>> DonemOnaylariniOkuAsync(string dbKey, string donemKodu)
 {return default;
}




        private async Task SemaHazirlaAsync()
 {}




        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class CariModel { public string CardCode { get; set; } public string CardName { get; set; } }

        public class FiltreModel
        {
            public string Donem { get; set; }              // yyyy-MM
            public bool IncFatura { get; set; } = true;
            public bool IncIade { get; set; } = true;
            public bool IncTaslak { get; set; } = true;
            public string DocTur { get; set; } = "H";      // H: hepsi, I: kalem, S: hizmet
            public string GrupIci { get; set; } = "H";     // H / HARIC / SADECE
            public string Operasyon { get; set; } = "H";   // H / HARIC / SADECE
            public string OnayDurumu { get; set; } = "H";  // H / ONAYLI / HARIC
            public string Ara { get; set; }
            public List<string> SelectedCustomers { get; set; }
        }

        public class BelgeSatiri
        {
            public string ObjType { get; set; }
            public string BelgeTipi { get; set; }
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            [System.Text.Json.Serialization.JsonIgnore] public DateTime TarihHam { get; set; }
            public string Tarih { get; set; }
            public string Vade { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string MusteriGrubu { get; set; }
            public string Ulke { get; set; }
            public string SatisTemsilcisi { get; set; }
            public string IslemTipi { get; set; }
            public string DocType { get; set; }
            public string MusteriBelgeNo { get; set; }
            public string Aciklama { get; set; }
            public string Kanal { get; set; }
            public bool Operasyon { get; set; }
            public bool GrupIci { get; set; }

            public string Segment { get; set; }
            public string ParaBirimi { get; set; }
            public decimal KurUSD { get; set; }
            public decimal KurEUR { get; set; }
            public decimal NetTL { get; set; }
            public decimal KdvTL { get; set; }
            public decimal BrutTL { get; set; }
            public decimal NetUSD { get; set; }
            public decimal NetEUR { get; set; }
            public decimal OrjNet { get; set; }
            public bool Haric { get; set; }
            public string HaricAciklama { get; set; }
            public string HaricKullanici { get; set; }
            public string HaricTarih { get; set; }
        }

        public class ToplamModel
        {
            public string Ad { get; set; }
            public string Kod { get; set; }
            public int Adet { get; set; }
            public int FaturaAdet { get; set; }
            public int IadeAdet { get; set; }
            public int TaslakAdet { get; set; }
            public int HaricAdet { get; set; }

            public bool GrupDahil { get; set; }
            public int GrupIciAdet { get; set; }
            public decimal GrupIciNetTL { get; set; }
            public decimal GrupIciNetUSD { get; set; }
            public decimal GrupIciNetEUR { get; set; }
            public decimal NetTL { get; set; }
            public decimal KdvTL { get; set; }
            public decimal BrutTL { get; set; }
            public decimal NetUSD { get; set; }
            public decimal NetEUR { get; set; }
            public decimal HaricNetTL { get; set; }
            public decimal HaricNetUSD { get; set; }
            public decimal HaricNetEUR { get; set; }

            public decimal BrutNetTL { get; set; }
        }

        public class OnayBilgisi
        {
            public string Kanal { get; set; }
            public string OnayGunu { get; set; }
            public string OnayGunuAdi { get; set; }
            public bool Acik { get; set; }
            public int KalanGun { get; set; }
            public bool Onayli { get; set; }
            public string OnaylayanKullanici { get; set; }
            public string OnayTarihi { get; set; }
            public string OnayAciklama { get; set; }
            public decimal OnayNetTL { get; set; }
            public decimal OnayNetUSD { get; set; }
            public decimal OnayNetEUR { get; set; }
            public int OnayFaturaSayisi { get; set; }
        }

        public class FaturaOnayIstek
        {
            public string Donem { get; set; }
            public string Kanal { get; set; }
            public string ObjType { get; set; }
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public string CardCode { get; set; }
            public bool Haric { get; set; }
            public string Aciklama { get; set; }
        }

        public class DonemOnayIstek
        {
            public string Donem { get; set; }
            public string Kanal { get; set; }
            public bool Onayli { get; set; }
            public decimal NetTL { get; set; }
            public decimal NetUSD { get; set; }
            public decimal NetEUR { get; set; }
            public int FaturaSayisi { get; set; }
            public int HaricSayisi { get; set; }
            public string Aciklama { get; set; }
        }

        public class GrupDeseni
        {
            public int Id { get; set; }
            public string Tip { get; set; }
            public string Deger { get; set; }
            public string Aciklama { get; set; }
            public bool Aktif { get; set; } = true;
            public string KullaniciKodu { get; set; }
        }

        public class HaricKaydi
        {
            public string ObjType { get; set; }
            public int DocEntry { get; set; }
            public bool Haric { get; set; }
            public string Aciklama { get; set; }
            public string KullaniciKodu { get; set; }
            public DateTime? IslemTarihi { get; set; }
        }

        public class DonemOnayKaydi
        {
            public string Kanal { get; set; }
            public bool Onayli { get; set; }
            public decimal? NetTL { get; set; }
            public decimal? NetUSD { get; set; }
            public decimal? NetEUR { get; set; }
            public int? FaturaSayisi { get; set; }
            public int? HaricSayisi { get; set; }
            public string Aciklama { get; set; }
            public string KullaniciKodu { get; set; }
            public DateTime? IslemTarihi { get; set; }
        }
    }
}
