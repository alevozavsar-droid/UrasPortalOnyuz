// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{


























































    [Authorize]
    [Route("[controller]")]
    public class UrasUretimBakimController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<UrasUretimBakimController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly EmailService _emailService;

        private JsonResult Veri(Func<object> f)
 {return default;
}

        private static readonly string MakineSelect = @"
SELECT M.Code, ISNULL(M.Name,'') AS Ad, ISNULL(M.U_BE1_TUR,'') AS Tur, ISNULL(M.U_BE1_MARKA,'') AS Marka, ISNULL(M.U_BE1_MODEL,'') AS Model,
       ISNULL(M.U_BE1_SERINO,'') AS SeriNo, ISNULL(M.U_BE1_KONUM,'') AS Konum, ISNULL(M.U_BE1_AKTIF,'Y') AS Aktif,
       CASE WHEN ISNULL(M.U_BE1_DURUM,'') <> '' THEN M.U_BE1_DURUM WHEN ISNULL(M.U_BE1_AKTIF,'Y') = 'Y' THEN 'Aktif' ELSE 'Pasif' END AS Durum,
       ISNULL(M.U_BE1_KRITIK,'') AS Kritiklik, ISNULL(M.U_BE1_SORUMLU,'') AS SorumluKod, ISNULL(M.U_BE1_SORUMLUAD,'') AS SorumluAd, ISNULL(M.U_BE1_SORUMLUEML,'') AS SorumluEmail,
       ISNULL(M.U_BE1_SERVISFRM,'') AS ServisFirma, ISNULL(M.U_BE1_SERVISAD,'') AS ServisFirmaAd,
       CONVERT(VARCHAR(10), M.U_BE1_DEVREYE, 120) AS DevreyeAlma, CONVERT(VARCHAR(10), M.U_BE1_GARANTI, 120) AS GarantiBitis,
       ISNULL(M.U_BE1_KAPASITE,'') AS Kapasite, ISNULL(M.U_BE1_GUC,'') AS Guc, ISNULL(CAST(M.U_BE1_TEKNIK AS NVARCHAR(MAX)),'') AS Teknik, ISNULL(M.U_BE1_ACIKLAMA,'') AS Aciklama,
       'M' AS Kaynak,
       (SELECT COUNT(*) FROM [@BE1_ARIZABKM] A WHERE A.U_BE1_RESCODE = M.Code AND ISNULL(A.U_BE1_DURUM,'Beklemede') NOT IN ('Kapatildi','Tamamlandi','Iptal')) AS AcikAriza,
       (SELECT COUNT(*) FROM [@BE1_MAKBAK] H INNER JOIN [@BE1_MAKBAKD] D ON D.DocEntry = H.DocEntry WHERE H.U_BE1_RESCODE = M.Code AND ISNULL(D.U_BE1_DURUM,'N') IN ('N','D')) AS BekleyenBakim,
       (SELECT COUNT(*) FROM [@BE1_MAKBAK] H INNER JOIN [@BE1_MAKBAKD] D ON D.DocEntry = H.DocEntry WHERE H.U_BE1_RESCODE = M.Code AND ISNULL(D.U_BE1_DURUM,'N') = 'N' AND D.U_BE1_DATE < CAST(GETDATE() AS DATE)) AS GecikmisBakim,
       (SELECT COUNT(*) FROM [@BE1_BKMDOC] B WHERE B.U_BE1_MAKINE = M.Code AND ISNULL(B.U_BE1_DOCENTRY,0) = 0) AS BelgeSayisi,
       (SELECT CONVERT(VARCHAR(10), MAX(A.U_BE1_BITTARIHI), 104) FROM [@BE1_ARIZABKM] A WHERE A.U_BE1_RESCODE = M.Code AND A.U_BE1_TIP IN ('BAKIM','PLANLI') AND ISNULL(A.U_BE1_DURUM,'') IN ('Kapatildi','Tamamlandi')) AS SonBakim,
       (SELECT CONVERT(VARCHAR(10), MAX(A.U_BE1_BILDTARIH), 104) FROM [@BE1_ARIZABKM] A WHERE A.U_BE1_RESCODE = M.Code AND A.U_BE1_TIP = 'ARIZA') AS SonAriza
FROM [@BE1_MAKINE] M";

        private static readonly string ArizaSelect = @"
SELECT A.DocEntry, 0 AS LineId,
       ISNULL(A.U_BE1_ARIZANO,'') AS ArizaNo, ISNULL(A.U_BE1_TIP,'ARIZA') AS Tip, ISNULL(A.U_BE1_DURUM,'Beklemede') AS Durum,
       ISNULL(A.U_BE1_ONCELIK,'Normal') AS Oncelik, ISNULL(A.U_BE1_KATEGORI,'') AS Kategori, ISNULL(A.U_BE1_NEDEN,'') AS Neden, ISNULL(A.U_BE1_BAKIMTIP,'') AS BakimTipi,
       ISNULL(A.U_BE1_ICDIS,'I') AS IcDis, ISNULL(A.U_BE1_SERVISFRM,'') AS ServisFirma, ISNULL(A.U_BE1_SERVISAD,'') AS ServisFirmaAd,
       ISNULL(A.U_BE1_ATANAN,'') AS AtananKod, ISNULL(A.U_BE1_ATANANAD,'') AS AtananAd,
       ISNULL(A.U_BE1_RESCODE,'') AS ResCode, ISNULL(A.U_BE1_RESNAME,'') AS ResName, ISNULL(A.U_BE1_WORKORDER,'') AS WorkOrder,
       CONVERT(VARCHAR(10), A.U_BE1_BILDTARIH, 120) AS BildTarih, ISNULL(A.U_BE1_BILDSAAT,'') AS BildSaat,
       ISNULL(A.U_BE1_BILDUSER,'') AS BildUser, ISNULL(A.U_BE1_BILDUSERNAME,'') AS BildUserName,
       CONVERT(VARCHAR(10), A.U_BE1_PLANTARIH, 120) AS PlanTarih,
       ISNULL(A.U_BE1_BKMUSER,'') AS BkmUser, ISNULL(A.U_BE1_BKMUSERNAME,'') AS BkmUserName,
       CONVERT(VARCHAR(10), A.U_BE1_BASTARIHI, 120) AS BasTarihi, ISNULL(A.U_BE1_BASSAATI,'') AS BasSaati,
       CONVERT(VARCHAR(10), A.U_BE1_BITTARIHI, 120) AS BitTarihi, ISNULL(A.U_BE1_BITSAATI,'') AS BitSaati,
       CAST(ISNULL(A.U_BE1_SURE,0) AS INT) AS Sure,
       ISNULL(A.U_BE1_URETIMDUR,'N') AS UretimDurdu, ISNULL(A.U_BE1_DURUSBAS,'') AS DurusBas, ISNULL(A.U_BE1_DURUSBIT,'') AS DurusBit, CAST(ISNULL(A.U_BE1_DURUSDK,0) AS INT) AS DurusDk,
       ISNULL(CAST(A.U_BE1_ACIKLAMA AS NVARCHAR(MAX)),'') AS Aciklama, ISNULL(CAST(A.U_BE1_YAPILANISLEM AS NVARCHAR(MAX)),'') AS YapilanIslem, ISNULL(CAST(A.U_BE1_SONUC AS NVARCHAR(MAX)),'') AS Sonuc,
       ISNULL(A.U_BE1_ONAYDURUM,'') AS OnayDurum, ISNULL(A.U_BE1_ONAYLAYAN,'') AS Onaylayan, ISNULL(A.U_BE1_ONAYZAMAN,'') AS OnayZaman, ISNULL(CAST(A.U_BE1_ONAYNOT AS NVARCHAR(MAX)),'') AS OnayNot,
       ISNULL(A.U_BE1_KAPATAN,'') AS Kapatan, ISNULL(A.U_BE1_KAPATMAZAM,'') AS KapatmaZaman,
       CAST(ISNULL(A.U_BE1_MLZTUTAR,0) AS FLOAT) AS MalzemeTutar, CAST(ISNULL(A.U_BE1_SRVTUTAR,0) AS FLOAT) AS ServisTutar,
       CAST(ISNULL(A.U_BE1_MLZTUTAR,0) + ISNULL(A.U_BE1_SRVTUTAR,0) AS FLOAT) AS ToplamTutar, ISNULL(A.U_BE1_PB,'') AS ParaBirimi,
       ISNULL(A.U_BE1_BKMPERIODENTRY,'') AS BkmPeriodEntry, CAST(ISNULL(A.U_BE1_MAKBAKDOC,0) AS INT) AS MakBakDoc, CAST(ISNULL(A.U_BE1_MAKBAKLINE,0) AS INT) AS MakBakLine,
       CASE WHEN A.U_BE1_PLANTARIH IS NULL THEN 0 ELSE DATEDIFF(DAY, GETDATE(), A.U_BE1_PLANTARIH) END AS KalanGun,
       (SELECT COUNT(*) FROM [@BE1_ARIZABKMD] D WHERE D.DocEntry = A.DocEntry) AS MalzemeSayisi,
       (SELECT COUNT(*) FROM [@BE1_BKMISLEM] I WHERE I.U_BE1_DOCENTRY = A.DocEntry) AS IslemSayisi,
       (SELECT COUNT(*) FROM [@BE1_BKMDOC] B WHERE B.U_BE1_DOCENTRY = A.DocEntry) AS BelgeSayisi,
       (SELECT COUNT(*) FROM [@BE1_BKMPRS] P WHERE P.U_BE1_DOCENTRY = A.DocEntry) AS PersonelSayisi
FROM [@BE1_ARIZABKM] A";

        private static readonly string BakimSatirSelect = @"
SELECT T0.DocEntry, T1.LineId, '' AS ArizaNo, 'BAKIM' AS Tip,
       CASE WHEN ISNULL(T1.U_BE1_DURUM,'N') = 'Y' THEN 'Tamamlandi' WHEN ISNULL(T1.U_BE1_DURUM,'N') = 'D' THEN 'DevamEdiyor' ELSE 'Beklemede' END AS Durum,
       CASE WHEN T1.U_BE1_DATE IS NULL OR ISNULL(T1.U_BE1_DURUM,'N') = 'Y' THEN 'Normal' WHEN DATEDIFF(DAY, GETDATE(), T1.U_BE1_DATE) <= 0 THEN 'Acil' WHEN DATEDIFF(DAY, GETDATE(), T1.U_BE1_DATE) <= 7 THEN 'Yuksek' ELSE 'Normal' END AS Oncelik,
       '' AS Kategori, ISNULL(T0.U_BE1_RESCODE,'') AS ResCode, ISNULL(T0.U_BE1_RESNAME,'') AS ResName, '' AS WorkOrder,
       CASE WHEN ISNULL(T1.U_BE1_DURUM,'N') = 'Y' THEN CONVERT(VARCHAR(10), COALESCE(T1.U_BE1_BITTARIHI, T1.U_BE1_DATE), 120) ELSE CONVERT(VARCHAR(10), T1.U_BE1_DATE, 120) END AS BildTarih,
       CONVERT(VARCHAR(10), T1.U_BE1_DATE, 120) AS PlanTarih,
       ISNULL(T1.U_BE1_BKMUSER,'') AS BkmUser, ISNULL(T1.U_BE1_BKMUSERNAME,'') AS BkmUserName,
       CONVERT(VARCHAR(10), T1.U_BE1_BASTARIHI, 120) AS BasTarihi, ISNULL(T1.U_BE1_BASSAATI,'') AS BasSaati,
       CONVERT(VARCHAR(10), T1.U_BE1_BITTARIHI, 120) AS BitTarihi, ISNULL(T1.U_BE1_BITSAATI,'') AS BitSaati, CAST(ISNULL(T1.U_BE1_SURE,0) AS INT) AS Sure,
       COALESCE(T2.U_BE1_ISLEMLER, CAST(T1.U_BE1_ISLEMLER AS NVARCHAR(MAX)), '') AS Aciklama, ISNULL(T2.U_BE1_BAKIMTIP,'') AS BakimTipi, ISNULL(T2.U_BE1_ICDIS,'I') AS IcDis,
       ISNULL(CAST(T1.U_BE1_BKMPERIODENTRY AS NVARCHAR(50)),'') AS BkmPeriodEntry, T0.DocEntry AS MakBakDoc, T1.LineId AS MakBakLine,
       ISNULL(DATEDIFF(DAY, GETDATE(), T1.U_BE1_DATE),0) AS KalanGun, CAST(ISNULL(T1.U_BE1_BAKIMGUN,0) AS INT) AS PeriyotGun,
       ISNULL(CAST(T1.U_BE1_TEXT AS NVARCHAR(MAX)),'') AS BakimAciklama, ISNULL(T1.U_BE1_CARDNAME,'') AS CardName, ISNULL(T1.U_BE1_USERNAME,'') AS UserName,
       (SELECT COUNT(*) FROM [@BE1_MAKBAKI] I WHERE I.DocEntry = T0.DocEntry AND ISNULL(I.U_BE1_LINEID,0) = T1.LineId) AS MalzemeSayisi,
       CAST(ISNULL(T1.U_BE1_ISEMRI,0) AS INT) AS IsEmri
FROM [@BE1_MAKBAK] T0
INNER JOIN [@BE1_MAKBAKD] T1 ON T1.DocEntry = T0.DocEntry
LEFT JOIN [@BE1_BKMPERIOD] T2 ON T2.DocEntry = TRY_CAST(CAST(T1.U_BE1_ISLEMLER AS NVARCHAR(100)) AS INT)";

        [HttpGet("")]
        [HttpGet("Index")]
        public IActionResult Index()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpGet("GetOzet")]
        public JsonResult GetOzet()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMakineler")]
        public JsonResult GetMakineler(bool tumu = false)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMakine")]
        public JsonResult GetMakine(string code)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetKodlar")]
        public JsonResult GetKodlar(string tip = "", bool tumu = false)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetBakimIslemleri")]
        public JsonResult GetBakimIslemleri(bool tumu = true)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetPersoneller")]
        public JsonResult GetPersoneller(bool tumu = false)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetCalisanlar")]
        public JsonResult GetCalisanlar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetFirmalar")]
        public JsonResult GetFirmalar(string q = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetDepolar")]
        public JsonResult GetDepolar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetAyar")]
        public JsonResult GetAyar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMailAlicilar")]
        public JsonResult GetMailAlicilar()
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetChecklistSablonlar")]
        public JsonResult GetChecklistSablonlar(string resCode = null, string tip = null)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetAnaliz")]
        public JsonResult GetAnaliz(string bas = "", string bit = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetArizalar")]
        public JsonResult GetArizalar(string durum = "Acik", string resCode = "", string bas = "", string bit = "", string oncelik = "", string tip = "", string atanan = "", string q = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        private List<Dictionary<string, object>> ArizalariGetir(string durum, string resCode, string bas, string bit, string oncelik = "", string tip = "", string atanan = "", string q = "")
 {return default;
}

        [HttpGet("GetArizaDetay")]
        public JsonResult GetArizaDetay(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetPeriyodikBakimlar")]
        public JsonResult GetPeriyodikBakimlar(string resCode = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMakBakDetay")]
        public JsonResult GetMakBakDetay(int docEntry)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("GetMakineDetay")]
        public JsonResult GetMakineDetay(string code)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpGet("MalzemeAra")]
        public JsonResult MalzemeAra(string q = "", string whs = "", int page = 1, int pageSize = 40)
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}





        private bool TamYetkiliMi()  {return default;
}

        private async Task<HttpClient> SlOturumAc()
 {return default;
}

        private static async Task SlOturumKapat(HttpClient client)
 {}

        private static string SlHataMesaji(string govde)
 {return default;
}




        private async Task<string> SlIstek(HttpClient client, HttpMethod metot, string yol, object govde = null, bool tumKoleksiyonuDegistir = false)
 {return default;
}

        private static string YeniZamanDamgaliKod(string onEk)  {return default;
}


        private async Task LogYaz(HttpClient client, int docEntry, string islem, string eski, string yeni, string not, string kullanici)
 {}


        private async Task MalzemeSatirlariniAyarla(HttpClient client, int docEntry, List<MalzemeSatiriDto> malzemeler)
 {}


        private (bool OnayZorunlu, bool OnayDisServis, double OnayTutar) OnayAyarlariGetir()
 {return default;
}

        private static DateTime? OpcTarihOku(JsonElement kok, string prop)
 {return default;
}





        [HttpPost("ArizaOlustur")]
        public async Task<JsonResult> ArizaOlustur([FromBody] ArizaOlusturDto istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<int>("data", 0) });
}

        [HttpPost("Ata")]
        public async Task<JsonResult> Ata([FromBody] AtaDto istek)
 {return Json(new { success = true, message = "Atama kaydedildi." });
}

        [HttpPost("Baslat")]
        public async Task<JsonResult> Baslat([FromBody] BaslatDto istek)
 {return Json(new { success = true, message = "Müdahale başlatıldı." });
}

        [HttpPost("Tamamla")]
        public async Task<JsonResult> Tamamla([FromBody] TamamlaDto istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<string>("data", 0) });
}

        [HttpPost("MalzemeKaydet")]
        public async Task<JsonResult> MalzemeKaydet([FromBody] MalzemeKaydetDto istek)
 {return Json(new { success = true, message = "Malzemeler kaydedildi. (Not: stoktan otomatik mal çıkışı bu ekranda oluşturulmuyor.)" });
}





        [HttpPost("OnayaGonder")]
        public async Task<JsonResult> OnayaGonder([FromBody] BaslatDto istek)
 {return Json(new { success = true, message = "Onaya gönderildi." });
}

        [HttpPost("OnayKarar")]
        public async Task<JsonResult> OnayKarar([FromBody] OnayKararDto istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("Iptal")]
        public async Task<JsonResult> Iptal([FromBody] IptalDto istek)
 {return Json(new { success = true, message = "Kayıt iptal edildi." });
}

        [HttpPost("Ertele")]
        public async Task<JsonResult> Ertele([FromBody] ErteleDto istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("ArizaSil")]
        public async Task<JsonResult> ArizaSil([FromBody] ArizaSilDto istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("DurusKaydet")]
        public async Task<JsonResult> DurusKaydet([FromBody] DurusKaydetDto istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [HttpPost("PeriyodikBakimOlustur")]
        public async Task<JsonResult> PeriyodikBakimOlustur([FromBody] PeriyodikBakimDto istek)
 {return Json(new { success = true, message = "Bakım planına yeni satır eklendi.", data = global::WebApplication3.OrnekDoldurucu.Deger<int>("data", 0) });
}

        [HttpPost("PlanSatirIsEmriOlustur")]
        public async Task<JsonResult> PlanSatirIsEmriOlustur([FromBody] PlanSatirIsEmriDto istek)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), data = global::WebApplication3.OrnekDoldurucu.Deger<int>("data", 0) });
}

        [HttpPost("BakimSatirSil")]
        public async Task<JsonResult> BakimSatirSil([FromBody] BakimSatirSilDto istek)
 {return Json(new { success = true, message = "Bakım satırı silindi." });
}





        [HttpPost("IslemKaydet")]
        public async Task<JsonResult> IslemKaydet([FromBody] IslemSatiriDto istek)
 {return Json(new { success = true, message = "İşlem kaydedildi." });
}

        [HttpPost("IslemSil")]
        public async Task<JsonResult> IslemSil([FromForm] string code)
 {return Json(new { success = true, message = "İşlem silindi." });
}





        [HttpPost("PersonelSatirKaydet")]
        public async Task<JsonResult> PersonelSatirKaydet([FromBody] PersonelSatiriDto istek)
 {return Json(new { success = true, message = "Personel / servis kaydedildi." });
}

        [HttpPost("PersonelSatirSil")]
        public async Task<JsonResult> PersonelSatirSil([FromForm] string code)
 {return Json(new { success = true, message = "Silindi." });
}

        [HttpPost("MakineKaydet")]
        public async Task<JsonResult> MakineKaydet([FromBody] MakineKaydetDto istek)
 {return Json(new { success = true, message = "Makine kaydedildi." });
}

        [HttpPost("MakineSil")]
        public async Task<JsonResult> MakineSil([FromForm] string code)
 {return Json(new { success = true, message = "Makine silindi." });
}

        [HttpPost("KodKaydet")]
        public async Task<JsonResult> KodKaydet([FromBody] KodKaydetDto istek)
 {return Json(new { success = true, message = "Kod kaydedildi." });
}

        [HttpPost("KodSil")]
        public async Task<JsonResult> KodSil([FromForm] string code)
 {return Json(new { success = true, message = "Kod silindi." });
}

        [HttpPost("PersonelKaydet")]
        public async Task<JsonResult> PersonelKaydet([FromBody] PersonelKaydetDto istek)
 {return Json(new { success = true, message = "Personel kaydedildi." });
}

        [HttpPost("PersonelSil")]
        public async Task<JsonResult> PersonelSil([FromForm] string code)
 {return Json(new { success = true, message = "Personel silindi." });
}





        [HttpPost("BakimIslemKaydet")]
        public async Task<JsonResult> BakimIslemKaydet([FromBody] BakimIslemKaydetDto istek)
 {return Json(new { success = true, message = "Bakım işlemi kaydedildi.", data = global::WebApplication3.OrnekDoldurucu.Deger<int>("data", 0) });
}

        [HttpPost("BakimIslemSil")]
        public async Task<JsonResult> BakimIslemSil([FromForm] int docEntry)
 {return Json(new { success = true, message = "Bakım işlemi silindi." });
}





        [HttpPost("ChecklistSablonKaydet")]
        public async Task<JsonResult> ChecklistSablonKaydet([FromBody] ChecklistSablonDto istek)
 {return Json(new { success = true, message = "Checklist şablonu kaydedildi." });
}

        [HttpPost("ChecklistSablonSil")]
        public async Task<JsonResult> ChecklistSablonSil([FromForm] int docEntry)
 {return Json(new { success = true, message = "Checklist şablonu silindi." });
}

        [HttpPost("AyarKaydet")]
        public async Task<JsonResult> AyarKaydet([FromBody] BakimAyarDto istek)
 {return Json(new { success = true, message = "Ayarlar kaydedildi." });
}

        [HttpPost("MailAliciKaydet")]
        public async Task<JsonResult> MailAliciKaydet([FromBody] MailAliciDto istek)
 {return Json(new { success = true, message = "Mail alıcısı kaydedildi." });
}

        [HttpPost("MailAliciSil")]
        public async Task<JsonResult> MailAliciSil([FromForm] string code)
 {return Json(new { success = true, message = "Mail alıcısı silindi." });
}

        [HttpPost("HatirlatmaGonder")]
        public JsonResult HatirlatmaGonder()
 {return Json(new { success = true, message = "Yaklaşan/gecikmiş bakım bulunmadığı için mail gönderilmedi." });
}

        [HttpPost("TestMail")]
        public JsonResult TestMail([FromForm] string email)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        private static readonly string BelgeKlasoru = "BakimBelge";
        private static readonly HashSet<string> BelgeUzantilari = new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp", ".xlsx", ".xls", ".docx", ".doc", ".txt" };
        private const long BelgeMaxBoyut = 26214400; // 25 MB

        [HttpGet("GetBelgeler")]
        public JsonResult GetBelgeler(int docEntry = 0, string makineKod = "")
 {return Json(new { success = true, ornek = true, message = "Ön yüz örneği: bu işlem için veri yok / kayıt yapılmaz.", data = new object[0], liste = new object[0], items = new object[0], results = new object[0], rows = new object[0], toplam = 0, total = 0, recordsTotal = 0, recordsFiltered = 0 });
}

        [HttpPost("BelgeYukle")]
        public async Task<JsonResult> BelgeYukle(IFormFile dosya, int docEntry = 0, string makineKod = "", string belgeTuru = "Diger", string aciklama = "")
 {return Json(new { success = true, message = "Belge yüklendi.", data = new { code = global::WebApplication3.OrnekDoldurucu.Deger<string>("code", 0), yol = global::WebApplication3.OrnekDoldurucu.Deger<string>("yol", 0) } });
}

        [HttpGet("BelgeIndir")]
        public IActionResult BelgeIndir(string code, bool indir = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpPost("BelgeSil")]
        public async Task<JsonResult> BelgeSil([FromForm] string code)
 {return Json(new { success = true, message = "Belge silindi." });
}





        private (Dictionary<string, object> Kayit, Dictionary<string, object> Makine, List<Dictionary<string, object>> Malzemeler, List<Dictionary<string, object>> Islemler, List<Dictionary<string, object>> Personel) RaporVerisi(int docEntry)
 {return default;
}

        private byte[] IsEmriRaporuPdfUret(int docEntry)
 {return default;
}

        [HttpGet("Rapor")]
        public IActionResult Rapor(int docEntry, bool indir = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private byte[] MakineGecmisiPdfUret(string code)
 {return default;
}

        [HttpGet("MakineRapor")]
        public IActionResult MakineRapor(string code, bool indir = false)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        [HttpPost("RaporMail")]
        public JsonResult RaporMail([FromForm] int docEntry, [FromForm] string alici, [FromForm] string not)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}





        public class ArizaOlusturDto
        {
            public string ResCode { get; set; }
            public string ResName { get; set; }
            public string Tip { get; set; } = "ARIZA";
            public string Oncelik { get; set; }
            public string Kategori { get; set; }
            public string Neden { get; set; }
            public string BakimTipi { get; set; }
            public string WorkOrder { get; set; }
            public string Aciklama { get; set; }
            public string IcDis { get; set; } = "I";
            public string ServisFirma { get; set; }
            public string ServisFirmaAd { get; set; }
            public string AtananKod { get; set; }
            public string AtananAd { get; set; }
            public string PlanTarih { get; set; }
            public bool UretimDurdu { get; set; }
        }

        public class AtaDto
        {
            public int DocEntry { get; set; }
            public string IcDis { get; set; }
            public string ServisFirma { get; set; }
            public string ServisFirmaAd { get; set; }
            public string AtananKod { get; set; }
            public string AtananAd { get; set; }
            public string PlanTarih { get; set; }
        }

        public class BaslatDto
        {
            public int DocEntry { get; set; }
        }

        public class TamamlaDto
        {
            public int DocEntry { get; set; }
            public string YapilanIslem { get; set; }
            public string Sonuc { get; set; }
            public List<MalzemeSatiriDto> Malzemeler { get; set; }
            public double ServisTutar { get; set; }
            public bool OnayaGonder { get; set; }
        }

        public class MalzemeKaydetDto
        {
            public int DocEntry { get; set; }
            public List<MalzemeSatiriDto> Malzemeler { get; set; }
        }

        public class OnayKararDto
        {
            public int DocEntry { get; set; }
            public bool Onay { get; set; }
            public string Not { get; set; }
        }

        public class IptalDto
        {
            public int DocEntry { get; set; }
            public string Not { get; set; }
        }

        public class ErteleDto
        {
            public int DocEntry { get; set; }
            public int LineId { get; set; }
            public int Gun { get; set; }
            public string Aciklama { get; set; }
            public string Tip { get; set; }
        }

        public class ArizaSilDto
        {
            public int DocEntry { get; set; }
        }

        public class BakimSatirSilDto
        {
            public int DocEntry { get; set; }
            public int LineId { get; set; }
        }

        public class DurusKaydetDto
        {
            public int DocEntry { get; set; }
            public bool UretimDurdu { get; set; }
            public string DurusBas { get; set; }
            public string DurusBit { get; set; }
        }

        public class PeriyodikBakimDto
        {
            public string ResCode { get; set; }
            public string ResName { get; set; }
            public int BkmPeriodEntry { get; set; }
            public string PlanlananTarih { get; set; }
            public int BakimGun { get; set; }
            public double Tonaj { get; set; }
            public string Aciklama { get; set; }
        }

        public class PlanSatirIsEmriDto
        {
            public int MakBakDoc { get; set; }
            public int LineId { get; set; }
        }

        public class MalzemeSatiriDto
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public double Quantity { get; set; }
            public string ItemUom { get; set; }
            public double OnHand { get; set; }
            public string Durum { get; set; }
            public string Kaynak { get; set; }
            public string WhsCode { get; set; }
            public double Fiyat { get; set; }
            public double Tutar { get; set; }
            public string ParaBirimi { get; set; }
            public string SeriNo { get; set; }
            public string Tedarikci { get; set; }
            public string Aciklama { get; set; }
        }

        public class IslemSatiriDto
        {
            public string Code { get; set; }
            public int DocEntry { get; set; }
            public string IslemTip { get; set; }
            public string Aciklama { get; set; }
            public string Bas { get; set; }
            public string Bit { get; set; }
            public int SureDk { get; set; }
            public string Yapan { get; set; }
            public string YapanTip { get; set; }
            public string Sonuc { get; set; }
        }

        public class PersonelSatiriDto
        {
            public string Code { get; set; }
            public int DocEntry { get; set; }
            public string Tip { get; set; }
            public string PersonelKod { get; set; }
            public string Ad { get; set; }
            public string Firma { get; set; }
            public string FirmaAd { get; set; }
            public string Rol { get; set; }
            public double Saat { get; set; }
            public double Ucret { get; set; }
            public string ParaBirimi { get; set; }
            public string Aciklama { get; set; }
        }

        public class MakineKaydetDto
        {
            public string Code { get; set; }
            public string Ad { get; set; }
            public string Tur { get; set; }
            public string Marka { get; set; }
            public string Model { get; set; }
            public string SeriNo { get; set; }
            public string Konum { get; set; }
            public bool Aktif { get; set; } = true;
            public string Durum { get; set; }
            public string Aciklama { get; set; }
            public string Kritiklik { get; set; }
            public string SorumluKod { get; set; }
            public string SorumluAd { get; set; }
            public string SorumluEmail { get; set; }
            public string ServisFirma { get; set; }
            public string ServisFirmaAd { get; set; }
            public string Kapasite { get; set; }
            public string Guc { get; set; }
            public string Teknik { get; set; }
            public string DevreyeAlma { get; set; }
            public string GarantiBitis { get; set; }
        }

        public class KodKaydetDto
        {
            public string Code { get; set; }
            public string Tip { get; set; }
            public string Ad { get; set; }
            public int Sira { get; set; }
            public bool Aktif { get; set; } = true;
            public string Aciklama { get; set; }
        }

        public class PersonelKaydetDto
        {
            public string Code { get; set; }
            public string Ad { get; set; }
            public string Tip { get; set; } = "I";
            public int EmpId { get; set; }
            public string CardCode { get; set; }
            public string CardName { get; set; }
            public string Telefon { get; set; }
            public string Email { get; set; }
            public string Uzmanlik { get; set; }
            public bool Aktif { get; set; } = true;
        }

        public class BakimIslemKaydetDto
        {
            public int DocEntry { get; set; }
            public string IslemAdi { get; set; }
            public int Gun { get; set; }
            public double Tonaj { get; set; }
            public string MakineTuru { get; set; }
            public string BakimTipi { get; set; }
            public int TahminiSureDk { get; set; }
            public string IcDis { get; set; } = "I";
            public string Talimat { get; set; }
            public bool Aktif { get; set; } = true;
            public List<BakimIslemMalzemeDto> Malzemeler { get; set; }
        }

        public class BakimIslemMalzemeDto
        {
            public string ItemCode { get; set; }
            public string ItemName { get; set; }
            public double Quantity { get; set; }
            public string ItemUom { get; set; }
        }

        public class ChecklistSablonDto
        {
            public int DocEntry { get; set; }
            public string ResCode { get; set; }
            public string ResName { get; set; }
            public string Tip { get; set; }
            public int? IslemEntry { get; set; }
            public string IslemAdi { get; set; }
            public string Name { get; set; }
            public bool Aktif { get; set; } = true;
            public List<ChecklistSoruDto> Sorular { get; set; }
        }

        public class ChecklistSoruDto
        {
            public int Order { get; set; }
            public string Question { get; set; }
            public bool Zorunlu { get; set; }
        }

        public class BakimAyarDto
        {
            public bool OnayZorunlu { get; set; }
            public bool OnayDisServis { get; set; }
            public double OnayTutar { get; set; }
            public string MesaiBas { get; set; }
            public string MesaiBit { get; set; }
            public int HatirlatmaGun { get; set; } = 7;
            public string HatirlatmaMail { get; set; }
            public string ArizaOnek { get; set; }
            public string BakimOnek { get; set; }
            public string VarsayilanDepo { get; set; }
            public string ParaBirimi { get; set; }
        }

        public class MailAliciDto
        {
            public string Code { get; set; }
            public string Olay { get; set; }
            public string Email { get; set; }
            public bool Aktif { get; set; } = true;
        }
    }
}
