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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{






    public class FavoriIstek
    {
        public string DbKey { get; set; }
        public string AcctCode { get; set; }
        public bool Favori { get; set; }
    }

    public class DuzeltmeIstek
    {
        public string DbKey { get; set; }
        public string CardCode { get; set; }
        public string KarsiHesap { get; set; }
        public decimal Tutar { get; set; }

        public string Yon { get; set; }
        public string Tarih { get; set; }
        public string IslemTipi { get; set; }
        public string Aciklama { get; set; }

        public string Aktarilmasin { get; set; }
    }

    public class HareketsizSilKayit
    {
        public string Kod { get; set; }
        public string DbKey { get; set; }
    }

    public class HareketsizSilIstek
    {
        public List<HareketsizSilKayit> Kayitlar { get; set; }
    }

    public class CariMutabakatSatir
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }          // C = Müşteri, S = Tedarikçi, L = Potansiyel
        public string VergiNo { get; set; }           // OCRD.LicTradNum - konsolide eşleştirmede birincil anahtar


        public string Sirket1Adi { get; set; }
        public decimal Sirket1CariBakiye { get; set; }
        public decimal Sirket1HesapBakiye { get; set; }
        public string Sirket2Adi { get; set; }
        public decimal Sirket2CariBakiye { get; set; }
        public decimal Sirket2HesapBakiye { get; set; }
        public bool KonsolideMi { get; set; }


        public bool Sirket1Var { get; set; }
        public bool Sirket2Var { get; set; }


        public bool Sirket1HesapVar { get; set; }
        public bool Sirket2HesapVar { get; set; }


        public decimal CariBorc { get; set; }
        public decimal CariAlacak { get; set; }
        public decimal CariBakiye { get; set; }
        public int CariHareketSayisi { get; set; }


        public string AcctCode { get; set; }          // OACT.AcctCode (iç kod)
        public string HesapKodu { get; set; }         // OACT.FormatCode (görünen kod)
        public string HesapAdi { get; set; }


        public decimal HesapBorc { get; set; }
        public decimal HesapAlacak { get; set; }
        public decimal HesapBakiye { get; set; }



        public decimal CariPayiBakiye { get; set; }
        public int HesapHareketSayisi { get; set; }


        public int PaylasanCariSayisi { get; set; }






        public decimal HesapToplamBakiye { get; set; }





        public int BirlesenKartSayisi { get; set; } = 0;

        public decimal Fark => CariBakiye - HesapBakiye;
        public bool HesapVar => !string.IsNullOrEmpty(AcctCode);
        public bool Uyusmuyor => !HesapVar || Math.Abs(Fark) > 0.01m;

        public string CardTypeAdi =>
            CardType == "C" ? "Müşteri" : CardType == "S" ? "Tedarikçi" : CardType == "L" ? "Potansiyel" : "-";

        public string DurumAdi => !HesapVar ? "HESAP TANIMSIZ" : (Uyusmuyor ? "UYUŞMUYOR" : "Uyuşuyor");








        private const decimal Esik = 0.01m;

        public decimal Sirket1Fark => Sirket1CariBakiye - Sirket1HesapBakiye;
        public decimal Sirket2Fark => Sirket2CariBakiye - Sirket2HesapBakiye;

        public decimal KonsolideCariToplam => Sirket1CariBakiye + Sirket2CariBakiye;
        public decimal KonsolideHesapToplam => Sirket1HesapBakiye + Sirket2HesapBakiye;





        private List<decimal> KonsolideKolonlar()
        {
            var d = new List<decimal>();

            if (Sirket1Var)
            {
                d.Add(Sirket1CariBakiye);
                if (Sirket1HesapVar) d.Add(Sirket1HesapBakiye);
            }

            if (Sirket2Var)
            {
                d.Add(Sirket2CariBakiye);
                if (Sirket2HesapVar) d.Add(Sirket2HesapBakiye);
            }

            return d;
        }


        public decimal KonsolideFark
        {
            get
            {
                var d = KonsolideKolonlar();
                return d.Count < 2 ? 0m : d.Max() - d.Min();
            }
        }


        public string Sirket1Durum =>
            !Sirket1Var ? "YOK" :
            !Sirket1HesapVar ? "HESAPYOK" :
            Math.Abs(Sirket1Fark) > Esik ? "FARK" : "OK";

        public string Sirket2Durum =>
            !Sirket2Var ? "YOK" :
            !Sirket2HesapVar ? "HESAPYOK" :
            Math.Abs(Sirket2Fark) > Esik ? "FARK" : "OK";









        public string KonsolideDurum
        {
            get
            {
                if (!Sirket1Var && !Sirket2Var) return "YOK";

                var d = KonsolideKolonlar();
                if (d.Count < 2) return "HESAPYOK";

                if (d.Max() - d.Min() > Esik) return "FARK";


                bool dorduDeVar = Sirket1Var && Sirket1HesapVar && Sirket2Var && Sirket2HesapVar;
                return dorduDeVar ? "OK" : "TEKSIRKET";
            }
        }
    }


    public class BagliHesap
    {





        public string CardCode { get; set; }


        public decimal HesapToplamBakiye { get; set; }



        public decimal CariPayiBakiye { get; set; }

        public string AcctCode { get; set; }          // OACT.AcctCode (iç kod)
        public string HesapKodu { get; set; }         // OACT.FormatCode (görünen kod)
        public string HesapAdi { get; set; }
        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
        public decimal Bakiye { get; set; }
        public int HareketSayisi { get; set; }
    }


    public class IslemTipiOgesi
    {
        public string Kod { get; set; }
        public int Adet { get; set; }
    }


    public class EkstreDetaySatir
    {
        public DateTime? RefDate { get; set; }
        public int TransId { get; set; }
        public int Line_ID { get; set; }
        public int TransType { get; set; }
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public string Aciklama { get; set; }
        public string Ref1 { get; set; }
        public string AktarimTipi { get; set; }
        public string Account { get; set; }      // fark analizinde kontrol hesabıyla karşılaştırılır
        public string ShortName { get; set; }    // hesap tarafında satırın ait olduğu cari
        public decimal TRYB { get; set; }
        public decimal TRYA { get; set; }
    }

    public class Rapor141ViewModel
    {
        public DateTime BasTarih { get; set; }
        public DateTime BitTarih { get; set; }
        public List<string> SecilenTipler { get; set; } = new List<string>();
        public string CariTipi { get; set; } = "";           // "", "C", "S"
        public bool DevirDahil { get; set; }
        public bool SadeceUyusmayanlar { get; set; }
        public string Arama { get; set; } = "";

        public List<IslemTipiOgesi> IslemTipleri { get; set; } = new List<IslemTipiOgesi>();
        public List<CariMutabakatSatir> Satirlar { get; set; } = new List<CariMutabakatSatir>();
        public List<BagliHesap> EslesmeyenHesaplar { get; set; } = new List<BagliHesap>();

        public string SecilenDbKey { get; set; }
        public string SecilenDbAdi { get; set; }
        public string Hata { get; set; }


        public string KonsolideKodu { get; set; } = "";
        public string Sirket1Adi { get; set; }
        public string Sirket2Adi { get; set; }
        public List<KeyValuePair<string, string>> KonsolideSecenekleri { get; set; } = new List<KeyValuePair<string, string>>();
    }



    [Authorize]
    [Route("[controller]")]
    public class Rapor141Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor141Controller> _logger;


        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection",   Display = "URASKIMYA",         DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1",  Display = "URSMAKINE",         DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2",  Display = "AVRUPA_PAPER",      DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3",  Display = "ALV_KIMYA",         DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4",  Display = "DAF_KIMYA",         DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5",  Display = "SELVI",             DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6",  Display = "ALVFILO",           DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7",  Display = "AVRASYA",           DbName = "Avrasya" },
            new DatabaseConfig { Key = "DefaultConnection8",  Display = "ASIA_KIMYA",        DbName = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9",  Display = "DEKORLIM",          DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING",      DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS",        DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS",    DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS",       DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S",         DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S",   DbName = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN",               DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026",     DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026",    DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026",        DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS", DbName = "TESTURASKIMYA_A.S" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI",         DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS",      DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI",        DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026",        DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026",        DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026",        DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026",        DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026",        DbName = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection27",   Display = "DELTA_POWER",         DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28",   Display = "MORAL_POWER",         DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29",   Display = "SADE_POWER",         DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30",   Display = "URAS_POWER",         DbName = "URAS_POWER" },
        };


        private static readonly Dictionary<int, string> IslemTipiAdlari = new Dictionary<int, string>
        {
            { -2, "AB - Açılış Bakiyesi" },
            { -3, "Kapanış Kaydı" },
            { 13, "MF - Satış Faturası" },
            { 14, "MŞ - Satış İade / Alacak Dekontu" },
            { 15, "Teslimat" },
            { 16, "Satış İade İrsaliyesi" },
            { 18, "SF - Satınalma Faturası" },
            { 19, "MS - Satınalma İade / Borç Dekontu" },
            { 20, "Mal Girişi" },
            { 21, "Mal İadesi" },
            { 24, "TH - Tahsilat (Gelen Ödeme)" },
            { 25, "İB - İbraz / Depozito" },
            { 30, "YK - Yevmiye Kaydı" },
            { 46, "YÖ - Giden Ödeme" },
            { 57, "Çek Ödemesi" },
            { 58, "Stok Sayım / Fark" },
            { 59, "Mal Girişi (Stok)" },
            { 60, "Mal Çıkışı (Stok)" },
            { 67, "Depolar Arası Transfer" },
            { 68, "Alacak Kapatma" },
            { 69, "Navlun / İthalat Maliyeti" },
            { 76, "Bakiye Aktarım" },
            { 162, "Stok Maliyet Düzeltme" },
            { 203, "Peşin Satış Faturası (Avans)" },
            { 204, "Peşin Alım Faturası (Avans)" },
            { 321, "DM - İç Mutabakat" }
        };








        private static readonly Dictionary<string, (string KaynakKey, string HedefKey, string KaynakAd, string HedefAd)> KonsolideCiftler =
            new Dictionary<string, (string, string, string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            { "URAS",      ("DefaultConnection",   "DefaultConnection25", "URAS KİMYA",    "URAS KİMYA A.Ş.") },
            { "ALV",       ("DefaultConnection3",  "DefaultConnection20", "ALV KİMYA",     "ALV KİMYA A.Ş.") },
            { "ALV2026",   ("DefaultConnection3",  "DefaultConnection20", "ALV KİMYA",     "ALV KİMYA 2026") },
            { "URS",       ("DefaultConnection1",  "DefaultConnection12", "URS MAKİNE",    "URS MAKİNE A.Ş.") },
            { "URS2026",   ("DefaultConnection1",  "DefaultConnection21", "URS MAKİNE",    "URS MAKİNE 2026") },
            { "HOLDING",   ("DefaultConnection10", "DefaultConnection18", "URAS HOLDİNG",  "URAS HOLDİNG A.Ş.") },
            { "DAF",       ("DefaultConnection4",  "DefaultConnection16", "DAF KİMYA",     "DAF KİMYA A.Ş.") },
            { "SELVI",     ("DefaultConnection5",  "DefaultConnection17", "SELVİ",         "SELVİ A.Ş.") },
            { "SELVI2026", ("DefaultConnection5",  "DefaultConnection22", "SELVİ",         "SELVİ 2026") },
            { "AVRUPA",    ("DefaultConnection2",  "DefaultConnection15", "AVRUPA PAPER",  "AVRUPA PAPER A.Ş.") },
            { "FILO",      ("DefaultConnection6",  "DefaultConnection14", "ALV FİLO",      "ALV FİLO A.Ş.") }
        };

        private static List<KeyValuePair<string, string>> KonsolideSecenekListesi()
 {return default;
}

        private string GetSelectedDatabase()
 {return default;
}

        private static string IslemTipiAdi(int tip)
 {return default;
}

        private static DateTime ParseTarih(string deger, DateTime varsayilan)
 {return default;
}


        private static List<string> ParseTipler(string tipler)
 {return default;
}


        private static string TipFiltresi(List<string> tipler)
 {return default;
}









        private static string Rapor20EkstreCte(List<string> tipler, bool devirDahil, string cariTipi, string ekWhere)
 {return default;
}









        private static string Rapor39EkstreCte(List<string> tipler, bool devirDahil, string hesapFiltresi, string ekWhere)
 {return default;
}




        private static readonly string KontrolHesabiFiltresi =
            " AND T1.Account IN (SELECT DISTINCT DebPayAcct FROM OCRD WITH (NOLOCK) WHERE DebPayAcct IS NOT NULL AND DebPayAcct <> '') ";





        private class CariKontrolHesabi
        {
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string VergiNo { get; set; }
            public string DebPayAcct { get; set; }
            public string HesapKodu { get; set; }
            public string HesapAdi { get; set; }
            public int PaylasanCariSayisi { get; set; }
        }





        private static void HesaplariCarilereBagla(
            List<CariMutabakatSatir> satirlar,
            List<BagliHesap> hesaplar,
            List<CariKontrolHesabi> kontrolHesaplari,
            out List<BagliHesap> eslesmeyenler)
 {eslesmeyenler = default;
}




        private async Task<(List<CariMutabakatSatir> Satirlar, List<BagliHesap> EslesmeyenHesaplar)>
            SirketVerisiGetirAsync(string connectionString, Rapor141ViewModel m, bool islemTipleriniDoldur)
 {return default;
}









        private static string EslestirmeAnahtari(CariMutabakatSatir s)
 {return default;
}

        private static string EslestirmeAnahtari(string vergiNo, string cardName, string cardCode)
 {return default;
}


        private static string UnvanNormalize(string ad)
 {return default;
}





        private static bool GecerliVkn(string vkn)
 {return default;
}


        private static List<CariMutabakatSatir> Birlestir(
            List<CariMutabakatSatir> birinci, string birinciAd,
            List<CariMutabakatSatir> ikinci, string ikinciAd)
 {return default;
}

        [HttpGet]
        public async Task<IActionResult> Index(string bas, string bit, string tipler, string cariTipi,
            bool devirDahil = false, bool sadeceUyusmayanlar = false, string arama = "", string konsolide = "")
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor141ViewModel>());
}









        [HttpGet("Detay")]
        public async Task<IActionResult> Detay(string cardCode, string hesaplar, string bas, string bit, string tipler,
            bool devirDahil = false, string konsolide = "", string vergiNo = "", string unvan = "")
 {return Json(new { success = true, cardCode = global::WebApplication3.OrnekDoldurucu.Deger<string>("cardCode", 0), bolumler = new object[0] });
}


        private async Task<object> SirketDetayiAsync(string connectionString, string sirketAdi,
            string cardCode, string vergiNo, string unvan,
            DateTime basTarih, DateTime bitTarih, List<string> tipListe, bool devirDahil)
 {return default;
}


        private static string CariTarafiNedeni(EkstreDetaySatir satir, string kontrolHesabi)
 {return default;
}


        private static string HesapTarafiNedeni(EkstreDetaySatir satir, List<string> cariKodlari)
 {return default;
}

        private static object FarkJson(EkstreDetaySatir r, string neden)
 {return default;
}

        private static object DetayJson(EkstreDetaySatir r)
 {return default;
}






        private List<(string Key, string Ad)> FiltredekiSirketler(string konsolide)
 {return default;
}










        [HttpGet("FiltreSirketleri")]
        public IActionResult FiltreSirketleri(string konsolide = "")
 {return Json(new { success = true, liste = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => new { Key = global::WebApplication3.OrnekDoldurucu.Deger<string>("Key", i2), Ad = global::WebApplication3.OrnekDoldurucu.Deger<string>("Ad", i2) }).ToList() });
}

        private string GecerliKullanici()  {return default;
}





        [HttpGet("KarsiHesaplar")]
        public IActionResult KarsiHesaplar(string dbKey, string arama = "")
 {return Json(new { success = true, liste = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "AcctCode", "HesapKodu", "AcctName", "ParaBirimi" }) });
}

        [HttpPost("HesapFavoriDegistir")]
        public IActionResult HesapFavoriDegistir([FromBody] FavoriIstek istek)
 {return Json(new { success = true, favori = global::WebApplication3.OrnekDoldurucu.Deger<bool>("favori", 0) });
}





        private string KurEksikMi(string baglanti, string sirketAdi, DateTime tarih)
 {return default;
}

        [HttpPost("DuzeltmeYevmiyesi")]
        public async Task<IActionResult> DuzeltmeYevmiyesi([FromBody] DuzeltmeIstek istek)
 {return Json(new { success = true, fisNo = global::WebApplication3.OrnekDoldurucu.Deger<int>("fisNo", 0), transId = global::WebApplication3.OrnekDoldurucu.Deger<int>("transId", 0), message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}







        [HttpGet("OrtakHesaplar")]
        public IActionResult OrtakHesaplar(string konsolide = "", string cariTipi = "")
 {return Json(new { success = true, liste = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "AcctCode", "HesapKodu", "AcctName", "CariSayisi", "CarilerToplami" }) });
}








        [HttpGet("OrtakCariler")]
        public IActionResult OrtakCariler(string acctCode, string dbKey = "")
 {return Json(new { success = true, liste = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName", "CardType", "Bakiye", "HesapKodu", "HesapAdi" }), sirket = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirket", 0) });
}









        [HttpGet("HareketsizCariler")]
        public IActionResult HareketsizCariler(string konsolide = "", string cariTipi = "")
 {return Json(new { success = true, liste = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "CardCode", "CardName", "CardType", "Bakiye", "HesapKodu", "HesapAdi" }), sirket = global::WebApplication3.OrnekDoldurucu.Deger<string>("sirket", 0) });
}











        [HttpPost("HareketsizCarileriSil")]
        public async Task<IActionResult> HareketsizCarileriSil([FromBody] HareketsizSilIstek istek)
 {return Json(new { success = true, silinen = global::WebApplication3.OrnekDoldurucu.Deger<int>("silinen", 0), toplam = global::WebApplication3.OrnekDoldurucu.Deger<int>("toplam", 0), sonuclar = new object[0] });
}

        [HttpGet("ExcelExport")]
        public async Task<IActionResult> ExcelExport(string bas, string bit, string tipler, string cariTipi,
            bool devirDahil = false, bool sadeceUyusmayanlar = false, string arama = "", string konsolide = "")
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }
}
