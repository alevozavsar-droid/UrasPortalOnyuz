// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Net.Http;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]


    public class Rapor154Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor154Controller> _logger;


        private readonly string _connectionString = "";
        private readonly DateTime _minDate = new DateTime(2026, 5, 1); // 01.05.2026 Milat Tarihi


        private static readonly string HedefCompanyDb = "AVRUPAPAPER_2026";

        [HttpGet]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string activeTab = "sayim", int? yil = null, int? ay = null)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Controllers.Rapor154ViewModel>());
}








        private string SlBaseUrl()  {return default;
}

        private HttpClient SlClient()
 {return default;
}


        private async Task<string> SlLoginAsync(HttpClient client, string companyDb)
 {return default;
}

        private static string SlHata(string govde)
 {return default;
}



        private class DogruSatir
        {
            public string ItemCode { get; set; }
            public decimal Quantity { get; set; }

            public decimal Price { get; set; }
            public string Currency { get; set; }
            public string WhsCode { get; set; }
        }


        private async Task<List<DogruSatir>> KaynakSatirlariniOkuAsync(string objType, string belgeNo)
 {return default;
}


        private static void KullaniciAlanlariniKopyala(JObject kaynak, JObject hedef, params string[] haricTut)
 {}









        [HttpPost("FiyatFarkiniDuzelt")]
        public async Task<IActionResult> FiyatFarkiniDuzelt(string hedefTip, string hedefKod, int hedefDocEntry,
                                                            string objType, string kaynakBelgeNo)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private class DuzeltmeSonucu
        {
            public bool Basarili { get; set; }
            public string Mesaj { get; set; }
        }

        public class TopluDuzeltmeOgesi
        {
            public string HedefTip { get; set; }
            public string HedefKod { get; set; }
            public int HedefDocEntry { get; set; }
            public string ObjType { get; set; }
            public string KaynakBelgeNo { get; set; }
        }






        [HttpPost("FiyatFarkiniTopluDuzelt")]
        public async Task<IActionResult> FiyatFarkiniTopluDuzelt([FromBody] List<TopluDuzeltmeOgesi> ogeler)
 {return Json(new { success = true, data = new object[0] });
}





        private async Task<DuzeltmeSonucu> FiyatDuzeltmeAsync(HttpClient client, string hedefTip, string hedefKod,
                                                              int hedefDocEntry, string objType, string kaynakBelgeNo)
 {return default;
}






        [HttpPost("IptalDamgasiniKaldir")]
        public async Task<IActionResult> IptalDamgasiniKaldir(string hedefTip, string hedefKod, int docEntry)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}








        [HttpPost("StokBelgesiniIptalEt")]
        public async Task<IActionResult> StokBelgesiniIptalEt(string hedefTip, string hedefKod, int docEntry)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), tersDocNum = global::WebApplication3.OrnekDoldurucu.Deger<int>("tersDocNum", 0) });
}




        private string GetSummaryQuery()
 {return default;
}

        private string GetSayimQuery()
 {return default;
}

        private string GetKimliksizQuery()
 {return default;
}

        private string GetManuelQuery()
 {return default;
}

        private string GetManuelIslemlerQuery()
 {return default;
}

        private string GetDuplicateQuery()
 {return default;
}







        private string GetCrossCheckQuery()
 {return default;
}





        private string GetIptalFarkQuery()
 {return default;
}







        private string GetStokAkisQuery()
 {return default;
}











        private string GetDetailQuery()
 {return default;
}

        private string GetStokRaporQuery()
 {return default;
}

        private string GetYoksayQuery()
 {return default;
}

        private string GetFazlaKayitlarQuery()
 {return default;
}

        private string GetStokBelgeKontrolQuery()
 {return default;
}






        [HttpGet("GetOzetData")]
        public async Task<IActionResult> GetOzetData()
 {return Json(new { success = true, summary = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.R154_AktarimOzet>(), sayim = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.R154_YevmiyeSayim>() });
}

        [HttpGet("GetKuyrukData")]
        public async Task<IActionResult> GetKuyrukData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_AktarimDetay>(12) });
}

        [HttpGet("GetKimliksizData")]
        public async Task<IActionResult> GetKimliksizData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_KimliksizYevmiye>(12) });
}

        [HttpGet("GetManuelData")]
        public async Task<IActionResult> GetManuelData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_ManuelYevmiye>(12) });
}

        [HttpGet("GetMukerrerData")]
        public async Task<IActionResult> GetMukerrerData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_MukerrerYevmiye>(12) });
}





        private string GetKapaliDonemQuery()
 {return default;
}






        private string GetIsaretDegismisQuery()
 {return default;
}

        [HttpGet("GetIsaretDegismisData")]
        public async Task<IActionResult> GetIsaretDegismisData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_IsaretDegismisYevmiye>(12) });
}

        [HttpGet("GetKapaliDonemData")]
        public async Task<IActionResult> GetKapaliDonemData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_KapaliDonemYevmiye>(12) });
}

        [HttpGet("GetEksikData")]
        public async Task<IActionResult> GetEksikData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_EksikYevmiye>(12) });
}

        [HttpGet("GetIptalData")]
        public async Task<IActionResult> GetIptalData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_YevmiyeIptalFark>(12) });
}

        [HttpGet("GetStokData")]
        public async Task<IActionResult> GetStokData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_StokHareketRaporu>(12) });
}

        [HttpGet("GetYoksayilanData")]
        public async Task<IActionResult> GetYoksayilanData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_YoksayilanYevmiye>(12) });
}

        [HttpGet("GetFazlaData")]
        public async Task<IActionResult> GetFazlaData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_FazlaYevmiye>(12) });
}

        [HttpGet("GetManuelAnalizData")]
        public async Task<IActionResult> GetManuelAnalizData()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_ManuelIslemDurumu>(12) });
}








        private string GetFiyatFarkQuery()
 {return default;
}

        [HttpGet("GetFiyatFarkData")]
        public async Task<IActionResult> GetFiyatFarkData(int? yil, int? ay)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_FiyatFark>(12) });
}










        private string GetAlisSatisQuery()
 {return default;
}



        private string GetAlisSatisDetayQuery(string objType)
 {return default;
}

        [HttpGet("GetAlisSatisDetay")]
        public async Task<IActionResult> GetAlisSatisDetay(int? yil, int? ay, string objType, string stok)
 {return Json(new { success = true, data = new object[0] });
}

        [HttpGet("GetAlisSatisData")]
        public async Task<IActionResult> GetAlisSatisData(int? yil, int? ay)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_AlisSatis>(12) });
}

        [HttpGet("GetStokAkisData")]
        public async Task<IActionResult> GetStokAkisData(int? yil, int? ay)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_StokAkis>(12) });
}


        public class KuyrukOgesi
        {
            public string ObjType { get; set; }
            public int DocEntry { get; set; }
            public string BelgeNo { get; set; }
        }









        [HttpPost("KuyrugaEkle")]
        public async Task<IActionResult> KuyrugaEkle([FromBody] List<KuyrukOgesi> ogeler)
 {return Json(new { success = true, data = new object[0] });
}

        [HttpGet("GetStokBelgeData")]
        public async Task<IActionResult> GetStokBelgeData(int? yil, int? ay)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.R154_StokBelgeKontrolItem>(12) });
}





        [HttpPost("KopanBaglariLehimle")]
        public async Task<IActionResult> KopanBaglariLehimle()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("TumEksikleriKuyrugaEkle")]
        public async Task<IActionResult> TumEksikleriKuyrugaEkle()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("TekrarKuyrugaEkle")]
        public async Task<IActionResult> TekrarKuyrugaEkle(int id)
 {return Json(new { success = true });
}

        [HttpPost("TumHatalilariKuyrugaEkle")]
        public async Task<IActionResult> TumHatalilariKuyrugaEkle()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("TumKayiplariKuyrugaEkle")]
        public async Task<IActionResult> TumKayiplariKuyrugaEkle()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("IptalDurumlariniEsitle")]
        public async Task<IActionResult> IptalDurumlariniEsitle()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("FazlaliklariIptalEt")]
        public async Task<IActionResult> FazlaliklariIptalEt()
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SecilenleriYoksay")]
        public async Task<IActionResult> SecilenleriYoksay([FromBody] List<string> yevmiyeNolar)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("SecilenEksikleriKuyrugaEkle")]
        public async Task<IActionResult> SecilenEksikleriKuyrugaEkle([FromBody] List<string> yevmiyeNolar)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("YoksaymaktanCikar")]
        public async Task<IActionResult> YoksaymaktanCikar([FromBody] List<string> yevmiyeNolar)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        [HttpPost("FazlaliklaraIptalYaz")]
        public async Task<IActionResult> FazlaliklaraIptalYaz([FromQuery] int yil, [FromQuery] int ay)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("IptalleriEsitleStok")]
        public async Task<IActionResult> IptalleriEsitleStok([FromQuery] int yil, [FromQuery] int ay)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private async Task<IActionResult> StokHareketleriniEsitle(string caller, int yil, int ay)
 {return default;
}

        [HttpPost("EksikleriKuyrugaItStok")]
        public async Task<IActionResult> EksikleriKuyrugaItStok([FromQuery] int yil, [FromQuery] int ay)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}
    }





    public class Rapor154ViewModel
    {
        public R154_AktarimOzet Summary { get; set; }
        public R154_YevmiyeSayim Sayim { get; set; }
        public List<R154_AktarimDetay> KuyrukListesi { get; set; }
        public List<R154_MukerrerYevmiye> MukerrerKayitlar { get; set; }
        public List<R154_EksikYevmiye> EksikKayitlar { get; set; }
        public List<R154_KimliksizYevmiye> KimliksizKayitlar { get; set; }
        public List<R154_ManuelYevmiye> ManuelKayitlar { get; set; }
        public List<R154_YevmiyeIptalFark> IptalFarkliKayitlar { get; set; }
        public List<R154_StokHareketRaporu> StokHareketleri { get; set; }
        public List<R154_YoksayilanYevmiye> YoksayilanKayitlar { get; set; }
        public List<R154_FazlaYevmiye> FazlaKayitlar { get; set; }
        public List<R154_ManuelIslemDurumu> ManuelIslemDurumlari { get; set; }
        public List<R154_EksikHataliStokHareketi> EksikHataliStokHareketleri { get; set; } = new List<R154_EksikHataliStokHareketi>();
        public List<R154_StokBelgeKontrolItem> StokBelgeKontrolListesi { get; set; } = new List<R154_StokBelgeKontrolItem>();
    }

    public class R154_StokBelgeKontrolItem
    {
        public string AnaSirketObjType { get; set; }
        public string AnaSirketDocEntry { get; set; }
        public string AnaSirketBelgeTuru { get; set; }
        public string AnaSirketFisNo { get; set; }
        public string IliskiliIptalNo { get; set; }
        public string FaturaDurumu { get; set; }
        public string AnaSirketTarih { get; set; }
        public string CariAdi { get; set; }
        public string AktarimStatusu { get; set; }
        public string BagliIrsaliyeNo { get; set; }
        public string Kaynak { get; set; }
        public string AkisDurumu { get; set; }
        public string HedefBelgeTipi { get; set; }
        public string HedefFisNo { get; set; }
        public string HedefFisTarihi { get; set; }
        public string HedefU_BE1_FRMADI { get; set; }
        public string HedefAciklama { get; set; }
    }

    public class R154_AktarimOzet
    {
        public int BekleyenSayisi { get; set; }
        public int BasariliSayisi { get; set; }
        public int HataliSayisi { get; set; }
        public int KayipSayisi { get; set; }
    }

    public class R154_YevmiyeSayim
    {
        public int KaynakSayi { get; set; }
        public int HedefSayi { get; set; }
        public int Fark => KaynakSayi - HedefSayi;

        public int KapaliDonemSayi { get; set; }

        public int KapaliDonemEksikSayi { get; set; }

        public int IsaretDegismisSayi { get; set; }
    }

    public class R154_IsaretDegismisYevmiye
    {
        public string HedefYevmiyeNo { get; set; }
        public string KaynakYevmiyeNo { get; set; }
        public string Tarih { get; set; }
        public string BelgeTipi { get; set; }
        public string KaynakBelgeNo { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
        public string KaynakIsaret { get; set; }     // kaynaktaki guncel U_BE1_AKTAR
        public string Neden { get; set; }            // kapsam disi kalma sebebi
        public string HedefOlusturma { get; set; }
    }

    public class R154_KapaliDonemYevmiye
    {
        public string KaynakYevmiyeNo { get; set; }
        public string Tarih { get; set; }
        public string BelgeTipi { get; set; }
        public string KaynakBelgeNo { get; set; }
        public string Aciklama { get; set; }
        public string DonemKodu { get; set; }
        public string DonemDurum { get; set; }      // Y kilitli, C kapanis donemi
        public string HedefYevmiyeNo { get; set; }  // bos ise hedefte yok
        public decimal Tutar { get; set; }
    }

    public class R154_KimliksizYevmiye
    {
        public int HedefYevmiyeNo { get; set; }
        public string OlusanBelgeNo { get; set; }
        public string Tarih { get; set; }
        public string BelgeTipi { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
    }

    public class R154_ManuelYevmiye
    {
        public int HedefYevmiyeNo { get; set; }
        public string OlusanBelgeNo { get; set; }
        public string Tarih { get; set; }
        public string BelgeTipi { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
        public string OlusturanKullanici { get; set; }
        public string GuncelleyenKullanici { get; set; }
        public string KopyaKaynakId { get; set; }
        public string MudahaleTipi { get; set; }
    }

    public class R154_MukerrerYevmiye
    {
        public int HedefYevmiyeNo { get; set; }
        public string KaynakYevmiyeNo { get; set; }
        public string OlusanBelgeNo { get; set; }
        public string Tarih { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
    }

    public class R154_EksikYevmiye
    {
        public string Kategori { get; set; }          // Yevmiye | Stok Belgesi
        public string KaynakYevmiyeNo { get; set; }
        public string Tarih { get; set; }
        public string BelgeTipi { get; set; }
        public string KaynakBelgeNo { get; set; }
        public string Aciklama { get; set; }


        public string Neden { get; set; }


        public string DurumKod { get; set; }


        public string HamHata { get; set; }
    }


    public class R154_AlisSatis
    {
        public string ObjType { get; set; }
        public string BelgeTipi { get; set; }
        public string Yon { get; set; }
        public string StokKodu { get; set; }
        public string StokAdi { get; set; }
        public decimal KaynakMiktar { get; set; }
        public decimal KaynakTutar { get; set; }


        public decimal KaynakTutarBrut { get; set; }

        public decimal HedefMiktar { get; set; }
        public decimal HedefTutar { get; set; }

        public decimal KaynakBirimTL { get; set; }


        public decimal KaynakBirimBrutTL { get; set; }


        public decimal HedefBirimTL { get; set; }

        public decimal MiktarFarki { get; set; }
        public decimal TutarFarki { get; set; }
        public decimal TutarFarkiBrut { get; set; }
        public decimal BirimFarki { get; set; }

        public bool MiktarUyusuyor { get; set; }
        public bool TutarUyusuyor { get; set; }

        public string Durum { get; set; }
    }


    public class R154_FiyatFark
    {
        public string ObjType { get; set; }
        public int HedefDocEntry { get; set; }


        public string HedefKod { get; set; }

        public string BelgeTipi { get; set; }
        public string BelgeNo { get; set; }
        public string Tarih { get; set; }
        public string CariAdi { get; set; }
        public string StokKodu { get; set; }
        public string StokAdi { get; set; }
        public decimal KaynakMiktar { get; set; }
        public decimal KaynakFiyat { get; set; }


        public decimal KaynakFiyatTL { get; set; }


        public decimal KaynakBrutFiyatTL { get; set; }


        public decimal Iskonto { get; set; }

        public string KaynakDoviz { get; set; }
        public decimal KaynakTutar { get; set; }
        public string HedefTip { get; set; }
        public string HedefBelgeNo { get; set; }
        public decimal HedefMiktar { get; set; }
        public decimal HedefFiyat { get; set; }
        public decimal HedefFiyatTL { get; set; }
        public string HedefDoviz { get; set; }
        public decimal HedefTutar { get; set; }
        public string FarkNedeni { get; set; }
    }


    public class R154_StokAkis
    {
        public string ObjType { get; set; }
        public string BelgeTipi { get; set; }
        public string BelgeNo { get; set; }
        public string Tarih { get; set; }
        public string CariAdi { get; set; }
        public string IptalDurumu { get; set; }      // N / Y / C
        public string BagliBelgeNo { get; set; }     // ters kaydin belge numarasi
        public string HedefBelgeler { get; set; }    // "Mal Çıkışı #123 [İPTAL] | ..."
        public int HedefAdet { get; set; }
        public int HedefIptalVar { get; set; }
        public int HedefAktifVar { get; set; }
        public string AkisDurumu { get; set; }


        public string HedefTip { get; set; }


        public string HedefKod { get; set; }

        public int HedefDocEntry { get; set; }
        public string HedefDocNum { get; set; }


        public string Neden { get; set; }


        public string HedefKimlikler { get; set; }


        public int KaynakDocEntry { get; set; }


        public string KuyrukDurum { get; set; }


        public string KuyrukHata { get; set; }
    }

    public class R154_YevmiyeIptalFark
    {
        public string Kategori { get; set; }          // Yevmiye | Stok Belgesi
        public string Neden { get; set; }
        public string KaynakYevmiyeNo { get; set; }
        public string HedefYevmiyeNo { get; set; }
        public string KaynakBelgeNo { get; set; }
        public string HedefBelgeNo { get; set; }
        public string Tarih { get; set; }
        public string Aciklama { get; set; }
        public string KaynakIptalDurum { get; set; }
        public string HedefIptalDurum { get; set; }


        public string HedefDonemDurum { get; set; }


        public string HedefDonemKodu { get; set; }


        public string Engel { get; set; }
    }

    public class R154_AktarimDetay
    {
        public int KuyrukNo { get; set; }
        public string BelgeTipi { get; set; }
        public string KaynakBelgeNo { get; set; }
        public string KaynakYevmiyeNo { get; set; }
        public string HedefYevmiyeNo { get; set; }
        public string HedefBelgeNo { get; set; }
        public string IslemTuru { get; set; }
        public string AktarimDurumuKodu { get; set; }
        public string KayitZamani { get; set; }
        public string HataMesaji { get; set; }

        public string DurumBadgeClass => AktarimDurumuKodu == "E" ? "bg-danger" : AktarimDurumuKodu == "M" ? "bg-dark" : AktarimDurumuKodu == "P" ? "bg-warning text-dark" : "bg-success";
        public string DurumIkonu => AktarimDurumuKodu == "E" ? "fa-times-circle" : AktarimDurumuKodu == "M" ? "fa-question-circle" : AktarimDurumuKodu == "P" ? "fa-hourglass-half" : "fa-check-circle";
        public string DurumMetni => AktarimDurumuKodu == "E" ? "Hata" : AktarimDurumuKodu == "M" ? "Kayıp" : AktarimDurumuKodu == "P" ? "Bekliyor" : "Aktarıldı";
    }

    public class R154_StokHareketRaporu
    {
        public int KuyrukNo { get; set; }
        public string KaynakBelgeTipi { get; set; }
        public string KaynakBelgeNo { get; set; }
        public string KaynakTarih { get; set; }
        public string HedefDurumu { get; set; }
        public string HedefBelgeNo { get; set; }
        public string KuyrukDurumu { get; set; }
        public string KategoriTipi { get; set; }
    }

    public class R154_YoksayilanYevmiye
    {
        public string KaynakYevmiyeNo { get; set; }
        public string EklenmeTarihi { get; set; }
        public string Tarih { get; set; }
        public string KaynakBelgeNo { get; set; }
        public string Aciklama { get; set; }
    }

    public class R154_FazlaYevmiye
    {
        public string HedefYevmiyeNo { get; set; }
        public string KaynakYevmiyeNo { get; set; }
        public string Tarih { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
        public string EkleyenKullanici { get; set; }
        public string Sebep { get; set; }
    }

    public class R154_ManuelIslemDurumu
    {
        public string HedefYevmiyeNo { get; set; }
        public string KaynakYevmiyeNo { get; set; }
        public string Tarih { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
        public string EkleyenGuncelleyen { get; set; }
        public string MudahaleNedeni { get; set; }
    }

    public class R154_EksikHataliStokHareketi
    {
        public string KaynakBelgeTipi { get; set; }
        public string KaynakBelgeNo { get; set; }
        public string KaynakTarih { get; set; }
        public string StokKodu { get; set; }
        public decimal KaynakMiktar { get; set; }
        public decimal KaynakFiyat { get; set; }
        public string KaynakDoviz { get; set; }
        public decimal KaynakTutar { get; set; }
        public string HedefBelgeNo { get; set; }
        public decimal HedefFiyat { get; set; }
        public string HedefDoviz { get; set; }
        public decimal HedefTutar { get; set; }
        public string HataNedeni { get; set; }
    }
}