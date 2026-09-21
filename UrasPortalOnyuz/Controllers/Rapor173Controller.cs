using WebApplication3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>Toplu Kalem Ana Verisi Güncelleme — alan kataloğu ve kalem listesi örnek; Excel/SAP işlemleri yok.</summary>
    [Authorize]
    [Route("[controller]")]
    public class Rapor173Controller : Controller
    {
        public sealed class KalemAlani { public string Kolon { get; set; } public string SlOzellik { get; set; } public string Etiket { get; set; } public string Tur { get; set; } public string Grup { get; set; } public bool Udf { get; set; } }

        private static readonly List<KalemAlani> _alanlar = new List<KalemAlani>
        {
            new KalemAlani { Kolon = "ItemName", SlOzellik = "ItemName", Etiket = "Kalem Adı", Tur = "text", Grup = "Genel" },
            new KalemAlani { Kolon = "FrgnName", SlOzellik = "ForeignName", Etiket = "Yabancı Ad", Tur = "text", Grup = "Genel" },
            new KalemAlani { Kolon = "ItmsGrpCod", SlOzellik = "ItemsGroupCode", Etiket = "Kalem Grubu Kodu", Tur = "int", Grup = "Genel" },
            new KalemAlani { Kolon = "CodeBars", SlOzellik = "BarCode", Etiket = "Barkod", Tur = "text", Grup = "Genel" },
            new KalemAlani { Kolon = "validFor", SlOzellik = "Valid", Etiket = "Aktif (E/H)", Tur = "yesno", Grup = "Genel" },
            new KalemAlani { Kolon = "BuyUnitMsr", SlOzellik = "PurchaseUnit", Etiket = "Satınalma Birimi", Tur = "text", Grup = "Satınalma" },
            new KalemAlani { Kolon = "LeadTime", SlOzellik = "LeadTime", Etiket = "Tedarik Süresi (gün)", Tur = "int", Grup = "Satınalma" },
            new KalemAlani { Kolon = "SalUnitMsr", SlOzellik = "SalesUnit", Etiket = "Satış Birimi", Tur = "text", Grup = "Satış" },
            new KalemAlani { Kolon = "VatGourpSa", SlOzellik = "SalesVATGroup", Etiket = "Satış KDV Grubu", Tur = "text", Grup = "Satış" },
            new KalemAlani { Kolon = "InvntryUom", SlOzellik = "InventoryUOM", Etiket = "Stok Birimi", Tur = "text", Grup = "Stok" },
            new KalemAlani { Kolon = "MinLevel", SlOzellik = "MinInventory", Etiket = "Minimum Stok", Tur = "dec", Grup = "Stok" },
            new KalemAlani { Kolon = "SWeight1", SlOzellik = "SalesUnitWeight", Etiket = "Satış Birimi Ağırlığı", Tur = "dec", Grup = "Stok" },
            new KalemAlani { Kolon = "U_BE1_MENSEI", SlOzellik = "U_BE1_MENSEI", Etiket = "İhracat Menşei No Alanı", Tur = "text", Grup = "Kullanıcı Tanımlı Alanlar", Udf = true },
            new KalemAlani { Kolon = "U_BE1_GTIP", SlOzellik = "U_BE1_GTIP", Etiket = "İhracat Gtip No Alanı", Tur = "text", Grup = "Kullanıcı Tanımlı Alanlar", Udf = true },
            new KalemAlani { Kolon = "U_BE1_ZDHC", SlOzellik = "U_BE1_ZDHC", Etiket = "ZDHC Belgesi", Tur = "text", Grup = "Kullanıcı Tanımlı Alanlar", Udf = true },
            new KalemAlani { Kolon = "U_BE1_KRTSTK", SlOzellik = "U_BE1_KRTSTK", Etiket = "Kritik Stok", Tur = "dec", Grup = "Kullanıcı Tanımlı Alanlar", Udf = true },
        };

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index() { ViewBag.CurrentDbDisplay = OrnekVeri.SirketAdi(Request.Cookies["SelectedDatabase"]); ViewBag.Alanlar = _alanlar; return View(); }

        [HttpGet("Gruplar")] public IActionResult Gruplar() => Json(new { success = true, gruplar = new object[] { new { kod = "100", ad = "Boyalar" }, new { kod = "107", ad = "Ambalaj Malzemeleri" }, new { kod = "120", ad = "Yardımcı Kimyasallar" } } });
        [HttpGet("TumKalemler")] public IActionResult TumKalemler() => Json(new { success = true, gruplar = new object[] { new { kod = "100", ad = "Boyalar" }, new { kod = "107", ad = "Ambalaj Malzemeleri" } }, kalemler = OrnekVeri.Kalemler.Select((k, i) => new object[] { k.Kod, k.Ad, i % 2 == 0 ? "100" : "107", 1, k.Birim }) });
        [HttpGet("KalemAra")] public IActionResult KalemAra(string q) => Json(new { success = true, kalemler = OrnekVeri.Kalemler.Where(k => string.IsNullOrEmpty(q) || (k.Kod + k.Ad).ToLower().Contains(q.ToLower())).Select(k => new { kod = k.Kod, ad = k.Ad, grup = "100", aktif = true, birim = k.Birim }), sinir = false });
        [HttpPost("KalemSay")] public IActionResult KalemSay(string kodlar, string onek) { var l = (kodlar ?? "").Split('\n', ',', ';').Select(x => x.Trim()).Where(x => x.Length > 0).ToList(); var var = OrnekVeri.Kalemler.Where(k => l.Contains(k.Kod) || (!string.IsNullOrEmpty(onek) && k.Kod.StartsWith(onek))).ToList(); return Json(new { success = true, bulunan = var.Count, ornek = var.Take(10).Select(k => new { kod = k.Kod, ad = k.Ad }), bulunamayan = l.Where(x => !OrnekVeri.Kalemler.Any(k => k.Kod == x)).Take(20) }); }
        [HttpPost("KodlariOku")] public IActionResult KodlariOku(IFormFile dosya) => Json(new { success = true, kodlar = OrnekVeri.Kalemler.Take(5).Select(k => k.Kod), adet = 5 });
        [HttpPost("SablonIndir")] public IActionResult SablonIndir() => StatusCode(500, "Ön yüz örneğinde Excel şablonu üretilmez.");
        [HttpPost("Onizle")] public IActionResult Onizle() => Json(new { success = true, toplam = 3, degisenKalem = 2, degisenAlan = 3, bulunamayan = 1, satirlar = new object[] {
            new { itemCode = OrnekVeri.Kalemler[0].Kod, kalemAdi = OrnekVeri.Kalemler[0].Ad, bulundu = true, alanlar = new object[] { new { kolon = "ItemName", etiket = "Kalem Adı", slOzellik = "ItemName", tur = "text", eski = OrnekVeri.Kalemler[0].Ad, yeni = OrnekVeri.Kalemler[0].Ad + " ZDHC LEVEL 3" }, new { kolon = "MinLevel", etiket = "Minimum Stok", slOzellik = "MinInventory", tur = "dec", eski = "0", yeni = "25" } } },
            new { itemCode = OrnekVeri.Kalemler[1].Kod, kalemAdi = OrnekVeri.Kalemler[1].Ad, bulundu = true, alanlar = new object[] { new { kolon = "U_BE1_ZDHC", etiket = "ZDHC Belgesi", slOzellik = "U_BE1_ZDHC", tur = "text", eski = "", yeni = "LEVEL 3" } } },
            new { itemCode = "XX.YOK.001", kalemAdi = "", bulundu = false, alanlar = new object[0] } } });
        [HttpPost("Uygula")] public IActionResult Uygula() => Json(new { success = false, message = "Ön yüz örneği: Service Layer güncellemesi yapılmaz." });
    }
}
