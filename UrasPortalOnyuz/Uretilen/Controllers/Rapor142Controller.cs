// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{



    public class ZimmetPersonel
    {
        public int Id { get; set; }
        public string SicilNo { get; set; }
        public string AdSoyad { get; set; }
        public string Departman { get; set; }
        public string Gorev { get; set; }
        public string Sirket { get; set; }
        public string Lokasyon { get; set; }
        public string Email { get; set; }
        public string Telefon { get; set; }
        public DateTime? IseGiris { get; set; }
        public DateTime? Cikis { get; set; }
        public bool Aktif { get; set; }
        public string Notlar { get; set; }


        public int ZimmetliAdet { get; set; }
        public decimal ZimmetliDeger { get; set; }
        public string VarlikOzeti { get; set; }

        public string BasHarfler
        {
            get
            {
                var parcalar = (AdSoyad ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parcalar.Length >= 2) return (parcalar[0].Substring(0, 1) + parcalar[1].Substring(0, 1)).ToUpper();
                return string.IsNullOrWhiteSpace(AdSoyad) ? "?" : AdSoyad.Trim().Substring(0, Math.Min(2, AdSoyad.Trim().Length)).ToUpper();
            }
        }
    }


    public class ZimmetVarlik
    {
        public int Id { get; set; }
        public string VarlikTipi { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public string SeriNo { get; set; }
        public string DemirbasNo { get; set; }


        public string Imei { get; set; }
        public string HatNumarasi { get; set; }
        public string Operator { get; set; }


        public string MacAdresi { get; set; }
        public string IsletimSistemi { get; set; }
        public string Islemci { get; set; }
        public string Ram { get; set; }
        public string Disk { get; set; }
        public string EkranBoyutu { get; set; }


        public DateTime? AlisTarihi { get; set; }
        public string FaturaNo { get; set; }
        public decimal? AlisTutari { get; set; }
        public string ParaBirimi { get; set; }
        public DateTime? GarantiBitis { get; set; }

        public string Durum { get; set; }        // Depoda / Zimmetli / Bakımda / Hurda / Kayıp
        public string Lokasyon { get; set; }
        public string Sirket { get; set; }
        public string Notlar { get; set; }


        public int? HareketId { get; set; }
        public int? PersonelId { get; set; }
        public string PersonelAdi { get; set; }
        public DateTime? ZimmetTarihi { get; set; }

        public string Baslik => string.Join(" ", new[] { Marka, Model }.Where(x => !string.IsNullOrWhiteSpace(x)));

        public bool GarantiDoldu => GarantiBitis.HasValue && GarantiBitis.Value.Date < DateTime.Today;
    }


    public class ZimmetHareket
    {
        public int Id { get; set; }
        public int VarlikId { get; set; }
        public int PersonelId { get; set; }
        public DateTime? ZimmetTarihi { get; set; }
        public DateTime? IadeTarihi { get; set; }
        public string TeslimEden { get; set; }
        public string TeslimAlan { get; set; }
        public string Durum { get; set; }
        public string Aciklama { get; set; }

        public string VarlikTipi { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public string SeriNo { get; set; }
        public string PersonelAdi { get; set; }
    }

    public class ZimmetIstatistik
    {
        public int ToplamVarlik { get; set; }
        public int Zimmetli { get; set; }
        public int Depoda { get; set; }
        public int Bakimda { get; set; }
        public int PersonelSayisi { get; set; }
        public decimal ToplamDeger { get; set; }
        public int GarantisiBiten { get; set; }
    }

    public class Rapor142ViewModel
    {
        public List<ZimmetPersonel> Personeller { get; set; } = new List<ZimmetPersonel>();
        public List<ZimmetVarlik> Varliklar { get; set; } = new List<ZimmetVarlik>();
        public ZimmetIstatistik Istatistik { get; set; } = new ZimmetIstatistik();

        public string[] VarlikTipleri { get; set; } = new string[0];
        public string[] Durumlar { get; set; } = new string[0];
        public List<string> Departmanlar { get; set; } = new List<string>();


        public List<string> Sirketler { get; set; } = new List<string>();


        public string AktifSirket { get; set; }

        public string Arama { get; set; } = "";
        public string Hata { get; set; }
        public bool Yetkili { get; set; }
        public string ImportBilgisi { get; set; }
    }












    [Authorize]
    [Route("ZimmetTakip")]
    public class Rapor142Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor142Controller> _logger;



        private readonly ApplicationDbContext _context;

        private static bool _semaHazir = false;

        public static readonly string[] VarlikTipleri =
        {
            "Laptop", "Masaüstü Bilgisayar", "Monitör", "Cep Telefonu", "GSM Hattı",
            "Tablet", "Yazıcı / Tarayıcı", "Telsiz", "Araç", "Anahtar / Kart",
            "Aksesuar", "Diğer"
        };

        public static readonly string[] Durumlar = { "Depoda", "Zimmetli", "Bakımda", "Hurda", "Kayıp" };


        private List<string> SirketListesi()
 {return default;
}


        private string AktifSirketAdi()
 {return default;
}

        private string Baglanti()  {return default;
}

        private string KullaniciKodu()  {return default;
}

        private bool Yetkili()
 {return default;
}





        [HttpGet]
        public async Task<IActionResult> Index(string arama = "")
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor142ViewModel>());
}


        [HttpGet("PersonelDetay")]
        public async Task<IActionResult> PersonelDetay(int id)
 {return Json(new { success = true, personel = new { Id = global::WebApplication3.OrnekDoldurucu.Deger<int>("Id", 0), SicilNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("SicilNo", 0), AdSoyad = global::WebApplication3.OrnekDoldurucu.Deger<string>("AdSoyad", 0), Departman = global::WebApplication3.OrnekDoldurucu.Deger<string>("Departman", 0), Gorev = global::WebApplication3.OrnekDoldurucu.Deger<string>("Gorev", 0), Sirket = global::WebApplication3.OrnekDoldurucu.Deger<string>("Sirket", 0), Lokasyon = global::WebApplication3.OrnekDoldurucu.Deger<string>("Lokasyon", 0), Email = global::WebApplication3.OrnekDoldurucu.Deger<string>("Email", 0), Telefon = global::WebApplication3.OrnekDoldurucu.Deger<string>("Telefon", 0), Aktif = global::WebApplication3.OrnekDoldurucu.Deger<bool>("Aktif", 0), Notlar = global::WebApplication3.OrnekDoldurucu.Deger<string>("Notlar", 0), iseGiris = global::WebApplication3.OrnekDoldurucu.Deger<string>("iseGiris", 0), cikis = global::WebApplication3.OrnekDoldurucu.Deger<string>("cikis", 0) }, varliklar = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "HareketId", "PersonelId", "ZimmetTarihi" }), gecmis = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { Id = global::WebApplication3.OrnekDoldurucu.Deger<int>("Id", i2), VarlikTipi = global::WebApplication3.OrnekDoldurucu.Deger<string>("VarlikTipi", i2), Marka = global::WebApplication3.OrnekDoldurucu.Deger<string>("Marka", i2), Model = global::WebApplication3.OrnekDoldurucu.Deger<string>("Model", i2), SeriNo = global::WebApplication3.OrnekDoldurucu.Deger<string>("SeriNo", i2), zimmet = global::WebApplication3.OrnekDoldurucu.Deger<string>("zimmet", i2), iade = global::WebApplication3.OrnekDoldurucu.Deger<string>("iade", i2), Aciklama = global::WebApplication3.OrnekDoldurucu.Deger<string>("Aciklama", i2) }).ToList() });
}

        private static object VarlikJson(ZimmetVarlik v)
 {return default;
}





        [HttpPost("PersonelKaydet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PersonelKaydet(ZimmetPersonel model)
 {return Json(new { success = true, message = "Kişi güncellendi.", id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0) });
}

        [HttpPost("VarlikKaydet")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VarlikKaydet(ZimmetVarlik model)
 {return Json(new { success = true, message = "Varlık güncellendi.", id = global::WebApplication3.OrnekDoldurucu.Deger<int>("id", 0) });
}

        [HttpPost("Zimmetle")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Zimmetle(int varlikId, int personelId, DateTime? zimmetTarihi, string teslimEden, string aciklama)
 {return Json(new { success = true, message = "Zimmet kaydı oluşturuldu." });
}

        [HttpPost("IadeAl")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IadeAl(int hareketId, DateTime? iadeTarihi, string yeniDurum, string aciklama)
 {return Json(new { success = true, message = "İade alındı." });
}

        [HttpPost("VarlikSil")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VarlikSil(int id)
 {return Json(new { success = true, message = "Varlık silindi." });
}






        [HttpGet("Tutanak")]
        public async Task<IActionResult> Tutanak(int personelId)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpGet("ExcelExport")]
        public async Task<IActionResult> ExcelExport()
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}


    }
}
