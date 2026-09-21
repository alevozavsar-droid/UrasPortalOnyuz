// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
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
using System.Threading.Tasks;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{















    [Authorize]
    [Route("Rapor171")]
    public class Rapor171Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor171Controller> _logger;

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } }




        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };

        private string GetConnectionString(string dbKey)  {return default;
}
        private string DbAdi(string dbKey)  {return default;
}
        private string DbKeyFromAdi(string dbAdi)  {return default;
}
        private string Kullanici()  {return default;
}

        [HttpGet]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
 {ViewBag.Sirketler = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor171Controller.DatabaseConfig>(12);
ViewBag.AlanMesajlari = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        private async Task<List<string>> HesapAlanlariniAcAsync(string dbKey, string dbDisplay, List<(string Kolon, string Aciklama)> eksik)
 {return default;
}


        public class AlisFaturaSatir
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public DateTime DocDate { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public decimal DocTotal { get; set; }
            public string DocCur { get; set; }
        }

        [HttpGet("AlisFaturalari")]
        public IActionResult AlisFaturalari(string db, string ara)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor171Controller.AlisFaturaSatir>(12) });
}

        public class YansitmaSatir
        {
            public int LineNum { get; set; }
            public string TipiHizmet { get; set; }
            public string ItemCode { get; set; }
            public string AcctCode { get; set; }
            public string Aciklama { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            public string Currency { get; set; }
            public double LineTotal { get; set; }
            public string VatGroup { get; set; }
            public string WhsCode { get; set; }
        }

        public class FaturaBaslik
        {
            public int DocEntry { get; set; }
            public int DocNum { get; set; }
            public DateTime DocDate { get; set; }
            public DateTime DocDueDate { get; set; }
            public DateTime TaxDate { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string DocCur { get; set; }
            public decimal DocRate { get; set; }
            public string NumAtCard { get; set; }
            public decimal DocTotal { get; set; }
        }

        [HttpGet("FaturaDetay")]
        public IActionResult FaturaDetay(string db, int docEntry)
 {return Json(new { success = true, baslik = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor171Controller.FaturaBaslik>(), satirlar = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor171Controller.YansitmaSatir>(12) });
}


        [HttpGet("KarsiCari")]
        public IActionResult KarsiCari(string kaynakDb, string hedefDb)
 {return Json(new { success = true, kaynakCari = global::WebApplication3.OrnekDoldurucu.Deger<string>("kaynakCari", 0), hedefCari = global::WebApplication3.OrnekDoldurucu.Deger<string>("hedefCari", 0) });
}

        [HttpPost("KarsiCariKaydet")]
        public IActionResult KarsiCariKaydet([FromBody] KarsiCariIstek req)
 {return Json(new { success = true });
}
        public class KarsiCariIstek
        {
            public string KaynakDb { get; set; }
            public string HedefDb { get; set; }
            public string KaynakCari { get; set; }
            public string HedefCari { get; set; }
        }

        public class CariAramaSonucu { public string CardCode { get; set; } public string CardName { get; set; } public string Vkn { get; set; } }

        [HttpGet("CariAra")]
        public IActionResult CariAra(string db, string ara, string tur = "C")   // C = müşteri, S = tedarikçi
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor171Controller.CariAramaSonucu>(12) });
}


        [HttpGet("HesapKarsiligi")]
        public IActionResult HesapKarsiligi(string kaynakDb, string hedefDb, string hesapKodu)
 {return Json(new { success = true, bulundu = global::WebApplication3.OrnekDoldurucu.Deger<bool>("bulundu", 0), hesap = global::WebApplication3.OrnekDoldurucu.Deger<string>("hesap", 0), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", 0) });
}

        [HttpPost("HesapKarsiligiKaydet")]
        public IActionResult HesapKarsiligiKaydet([FromBody] HesapEslesmeIstek req)
 {return Json(new { success = true });
}
        public class HesapEslesmeIstek
        {
            public string KaynakDb { get; set; }
            public string HedefDb { get; set; }
            public string KaynakHesap { get; set; }
            public string HedefHesap { get; set; }
            public string HedefHesapAdi { get; set; }
        }

        public class HesapAramaSonucu { public string AcctCode { get; set; } public string AcctName { get; set; } }

        [HttpGet("HesapAra")]
        public IActionResult HesapAra(string db, string ara)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor171Controller.HesapAramaSonucu>(12) });
}

        [HttpGet("KalemKarsiligi")]
        public IActionResult KalemKarsiligi(string kaynakDb, string hedefDb, string itemCode)
 {return Json(new { success = true, bulundu = global::WebApplication3.OrnekDoldurucu.Deger<bool>("bulundu", 0), kalem = global::WebApplication3.OrnekDoldurucu.Deger<string>("kalem", 0), ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("ad", 0) });
}

        [HttpPost("KalemKarsiligiKaydet")]
        public IActionResult KalemKarsiligiKaydet([FromBody] KalemEslesmeIstek req)
 {return Json(new { success = true });
}
        public class KalemEslesmeIstek
        {
            public string KaynakDb { get; set; }
            public string HedefDb { get; set; }
            public string KaynakKalem { get; set; }
            public string HedefKalem { get; set; }
            public string HedefKalemAdi { get; set; }
        }

        public class KalemAramaSonucu { public string ItemCode { get; set; } public string ItemName { get; set; } }

        [HttpGet("KalemAra")]
        public IActionResult KalemAra(string db, string ara)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor171Controller.KalemAramaSonucu>(12) });
}


        public class AdresModel
        {
            public string Street { get; set; }
            public string Block { get; set; }
            public string ZipCode { get; set; }
            public string City { get; set; }
            public string County { get; set; }
            public string State { get; set; }
            public string Country { get; set; }
        }

        [HttpGet("CariAdres")]
        public IActionResult CariAdres(string db, string cardCode)
 {return Json(new { success = true, bill = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor171Controller.AdresModel>(), ship = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor171Controller.AdresModel>() });
}

        public class DepoModel { public string WhsCode { get; set; } public string WhsName { get; set; } }

        [HttpGet("Depolar")]
        public IActionResult Depolar(string db)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor171Controller.DepoModel>(12) });
}


        public class SatirIstek
        {
            public int LineNum { get; set; }
            public string TipiHizmet { get; set; }
            public string ItemCode { get; set; }
            public string AcctCode { get; set; }
            public string Aciklama { get; set; }
            public double Quantity { get; set; }
            public double UnitPrice { get; set; }
            public string Currency { get; set; }
            public double LineTotal { get; set; }
            public string VatGroup { get; set; }
            public string WhsCode { get; set; }
        }
        public class AdresIstek
        {
            public string BillToStreet { get; set; }
            public string BillToStreetNo { get; set; }
            public string BillToBlock { get; set; }
            public string BillToZipCode { get; set; }
            public string BillToCity { get; set; }
            public string BillToCounty { get; set; }
            public string BillToState { get; set; }
            public string BillToCountry { get; set; }
            public string ShipToStreet { get; set; }
            public string ShipToStreetNo { get; set; }
            public string ShipToBlock { get; set; }
            public string ShipToZipCode { get; set; }
            public string ShipToCity { get; set; }
            public string ShipToCounty { get; set; }
            public string ShipToState { get; set; }
            public string ShipToCountry { get; set; }
        }
        public class SatisOlusturIstek
        {
            public string KaynakDb { get; set; }
            public string HedefDb { get; set; }
            public int KaynakDocEntry { get; set; }
            public string KaynakCari { get; set; }   // kaynak şirkette hedefi temsil eden müşteri
            public DateTime DocDate { get; set; }
            public DateTime DocDueDate { get; set; }
            public DateTime TaxDate { get; set; }
            public string DocCurrency { get; set; }
            public decimal DocRate { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_SEND { get; set; }
            public string U_BE1_MUAFCODE { get; set; }
            public string U_BE1_NAKLIYETARIHI { get; set; }
            public string U_BE1_NAKLIYESAATI { get; set; }
            public string U_BE1_SOFORADSOYAD { get; set; }
            public string U_BE1_ARACPLAKASI { get; set; }
            public string U_BE1_SOFORUNVANI { get; set; }
            public string U_BE1_SOFORKIMLIK { get; set; }
            public string U_BE1_SOFORTEL { get; set; }
            public string U_BE1_FRMNO { get; set; }
            public string U_BE1_FRMADI { get; set; }
            public string U_BE1_FRMVKN { get; set; }
            public string PayToCode { get; set; }
            public string ShipToCode { get; set; }
            public AdresIstek AddressExtension { get; set; }
            public List<SatirIstek> DocumentLines { get; set; }
        }

        [HttpPost("OlusturSatis")]
        public async Task<IActionResult> OlusturSatis([FromBody] SatisOlusturIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("docNum", 0) });
}


        [HttpGet("SatisBilgi")]
        public IActionResult SatisBilgi(string db, int docEntry)
 {return Json(new { success = true, numAtCard = global::WebApplication3.OrnekDoldurucu.Deger<string>("numAtCard", 0) });
}


        public class AlisOlusturIstek
        {
            public string KaynakDb { get; set; }
            public string HedefDb { get; set; }
            public int KaynakDocEntry { get; set; }
            public int KaynakInvDocEntry { get; set; }
            public string GibFaturaNo { get; set; }
            public string HedefCari { get; set; }   // hedef şirkette kaynağı temsil eden tedarikçi
            public DateTime DocDate { get; set; }
            public DateTime DocDueDate { get; set; }
            public DateTime TaxDate { get; set; }
            public string DocCurrency { get; set; }
            public decimal DocRate { get; set; }
            public List<SatirIstek> DocumentLines { get; set; }   // TipiHizmet + hedef AcctCode/ItemCode zaten çözülmüş halde gelir
        }

        [HttpPost("OlusturAlis")]
        public async Task<IActionResult> OlusturAlis([FromBody] AlisOlusturIstek req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), docEntry = global::WebApplication3.OrnekDoldurucu.Deger<int>("docEntry", 0), docNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("docNum", 0) });
}

        private static string SlHata(string cevap)
 {return default;
}


        [HttpGet("Gecmis")]
        public IActionResult Gecmis(DateTime? baslangic, DateTime? bitis)
 {return Json(new { success = true, data = new object[0] });
}
    }
}
