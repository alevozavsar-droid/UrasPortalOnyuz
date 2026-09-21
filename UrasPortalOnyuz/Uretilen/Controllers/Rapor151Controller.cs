// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
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
    public class Rapor151Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor151Controller> _logger;

        private static readonly string MerkezBaglantiAnahtari = "DefaultConnection";
        private static bool _semaHazir = false;




        private List<(string Key, string Ad)> Sirketler()
 {return default;
}



        private string SecilenDbKey()  {return default;
}

        private string SirketAdi(string key)  {return default;
}
        private string Baglanti(string key)  {return default;
}
        private string KullaniciKodu()  {return default;
}

        private static DateTime TarihCoz(string tarih)
 {return default;
}




        [HttpGet]
        public async Task<IActionResult> Index(string tarih = null)
 {ViewBag.Tarih = "";
ViewBag.TarihMetin = "";
ViewBag.BugunMu = false;
ViewBag.SirketAdi = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}




        [HttpGet("Hareketler")]
        public async Task<IActionResult> Hareketler(string tarih)
 {return Json(new { success = true, tarih = global::WebApplication3.OrnekDoldurucu.Deger<string>("tarih", 0), misafir = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor151Controller.Hareket>(12), calisan = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor151Controller.Hareket>(12), ozet = new { misafirToplam = global::WebApplication3.OrnekDoldurucu.Deger<int>("misafirToplam", 0), misafirIceride = global::WebApplication3.OrnekDoldurucu.Deger<int>("misafirIceride", 0), calisanToplam = global::WebApplication3.OrnekDoldurucu.Deger<int>("calisanToplam", 0), calisanDisarida = global::WebApplication3.OrnekDoldurucu.Deger<int>("calisanDisarida", 0) } });
}

        [HttpPost("Kaydet")]
        public async Task<IActionResult> Kaydet([FromBody] HareketIstek h)
 {return Json(new { success = true, id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0), message = "Güncellendi." });
}


        [HttpPost("SaatAta")]
        public async Task<IActionResult> SaatAta(int id, string alan, string saat = null)
 {return Json(new { success = true, saat = global::WebApplication3.OrnekDoldurucu.Deger<string>("saat", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("Sil")]
        public async Task<IActionResult> Sil(int id)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        private static readonly string[] VarsayilanCalisanlar =
        {
            "Burhan Evvel",
            "Ebru Tekin",
            "Şeyda Demir",
            "Personel 07",
            "Ertan Yavuz",
            "Hacı Personel 14",
            "Personel 05",
            "Yusuf Öztürk",
            "Personel 08",
            "Gizem Tan",
            "Burcu Saldere",
            "Ezgi Demir",
            "Elmın Jabbarov",
            "Mesut Akıncı",
            "Personel 21",
            "Emrah Ateş",
            "Nazlı Erdem",
            "Ramın Musayev",
            "Efe Akçakale",
            "Görkem Akbulut",
            "Ümit Özdemir",
            "Canan Su",
            "Personel 18",
            "Erdoğan Polat",
            "Soner İkizoğlu",
            "Vu Thı Thuy Ha",
            "Seda Işık Kaya",
            "Handan Yıldırım",
            "Personel 04",
            "Rahman Babazade",
            "Rabiye Doğan",
            "Personel 17",
            "Personel 23",
            "Selçuk Mert",
            "Burhan Gün",
            "Dinçer Özdemir",
            "İsmail Duman",
            "Şirin Tunç",
            "İnanç Bozkurt",
            "Arslan Coşkun",
            "Personel 16",
            "Cüneyt Arda Ceylan",
            "Joao Paulo Araujo Fernandes",
            "Personel 11",
            "Zahit Ercan",
            "Müjgan Yetgin",
            "Savaş Özcan",
            "Personel 19",
            "Berkan Özkan",
            "Yeşim Karakütük",
            "Olgun Küskü",
            "Sevgi Ay Eren",
            "Personel 02",
            "Mustafa Özdeniz",
            "Personel 15",
            "Duygu Öztürk",
            "Hülya Er",
            "Ömer Yıldırım",
            "Personel 22",
            "Nevruz Sarıtaş",
            "Ömer Kantar",
            "Dilber Güney",
            "Personel 09",
            "İdil Akel",
            "Murat Özavşar",
            "Personel 13",
            "Tural Abbasov",
            "Onur Bal",
            "Refik Polat",
            "Özlem Güleryüz",
            "Hale Bisren",
            "Kamil Mahmudov",
            "Serhat Mutlu",
            "Personel 20",
            "Kerem Aksoy",
            "Caner Eratak",
            "Personel 06",
            "Özgür Pamir",
            "Erman Kaval",
            "Nükhet Özavşar",
            "Yalçın Avcı",
            "Tolga Yaman",
            "Birol Şeker",
            "Özge İsen",
            "Talat Doğrul",
            "Cafer İpek",
            "Ali Veli",
            "Personel 10",
            "Azad Shırınov",
            "Esra Kaya",
            "Personel 03",
            "Perihan Karabuğday",
            "Devrim Bisren",
            "Mert Doğan",
            "Eser Şenürek",
            "Aykut Coşar",
            "Yücel Demirci",
            "ELMAN ABBASLI",
            "Personel 12",
            "Mehman Babayev",
            "Murad Hudaybergenov",
            "Mahmut Keskin",
        };
        private static bool _calisanlarTohumlandi;

        [HttpGet("Calisanlar")]
        public async Task<IActionResult> Calisanlar()
 {return Json(new { success = true, calisanlar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", i2), kod = global::WebApplication3.OrnekDoldurucu.Deger<string>("kod", i2), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", i2), bolum = global::WebApplication3.OrnekDoldurucu.Deger<string>("bolum", i2), kaynak = global::WebApplication3.OrnekDoldurucu.Deger<string>("kaynak", i2) }).ToList() });
}

        [HttpPost("CalisanEkle")]
        public async Task<IActionResult> CalisanEkle(string adSoyad, string bolum = "")
 {return Json(new { success = true, id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("CalisanGuncelle")]
        public async Task<IActionResult> CalisanGuncelle(int id, string adSoyad, string bolum = "")
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}


        [HttpPost("CalisanSil")]
        public async Task<IActionResult> CalisanSil(int id)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}




        private static TimeSpan? SaatCoz(string s)
 {return default;
}

        private async Task SemaHazirlaAsync()
 {}

        public class Hareket
        {
            public int Id { get; set; }
            public string SirketDb { get; set; }
            public DateTime Tarih { get; set; }
            public string Tip { get; set; }
            public string AdSoyad { get; set; }
            public string Firma { get; set; }
            public string KisiKodu { get; set; }
            public string Plaka { get; set; }
            public string ZiyaretEdilen { get; set; }
            public string Neden { get; set; }
            public string GirisSaati { get; set; }
            public string CikisSaati { get; set; }
            public string Aciklama { get; set; }
            public string Kaydeden { get; set; }
            public DateTime? KayitTarihi { get; set; }
            public string Guncelleyen { get; set; }
            public DateTime? GuncellemeTarihi { get; set; }
        }

        public class HareketIstek
        {
            public int Id { get; set; }
            public string Tarih { get; set; }
            public string Tip { get; set; }
            public string AdSoyad { get; set; }
            public string Firma { get; set; }
            public string KisiKodu { get; set; }
            public string Plaka { get; set; }
            public string ZiyaretEdilen { get; set; }
            public string Neden { get; set; }
            public string GirisSaati { get; set; }
            public string CikisSaati { get; set; }
            public string Aciklama { get; set; }
        }
    }
}
