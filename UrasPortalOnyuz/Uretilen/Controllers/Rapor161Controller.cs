// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{






    [Authorize]
    [Route("[controller]")]
    public class Rapor161Controller : Controller
    {
        private readonly IConfiguration _configuration;

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
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private static readonly string SORGU = @"
            WITH CombinedData AS (
                SELECT
                    OINV.U_BE1_IHRDOSNO AS DosyaNo,
                    COALESCE(OSLP.SlpName, 'Bilinmiyor') AS Satisci,
                    OINV.NumAtCard AS FaturaNo,
                    FORMAT(OINV.U_BE1_NAKLIYETARIHI, 'dd.MM.yyyy') AS FaturaTarihi,
                    OINV.DocDate AS RawDocDate,
                    OINV.CardName AS Musteri,
                    CRY.Name AS Ulke,
                    INV1.ItemCode AS KalemKodu,
                    INV1.Dscription AS KalemAdi,
                    INV1.Quantity AS Miktar,
                    INV1.unitMsr AS Olcu,
                    CAST(INV1.LineTotal / ISNULL((SELECT TOP 1 T0.Rate FROM ORTT T0 WHERE T0.Currency = OINV.DocCur AND T0.RateDate <= OINV.DocDate ORDER BY T0.RateDate DESC), 1) AS DECIMAL(19,2)) AS TutarBelge,
                    OINV.DocCur AS ParaBirimi,
                    FORMAT(OINV.DocDate, 'yyyy MMMM', 'tr-TR') AS YilAy,
                    T1.U_BE1_GTIP AS GTIP,
                    N'Fatura' AS BelgeTuru
                FROM OINV WITH(NOLOCK)
                LEFT JOIN OCRD C WITH(NOLOCK) ON C.CardCode = OINV.CardCode
                LEFT JOIN OCRY CRY WITH(NOLOCK) ON CRY.Code = C.Country
                LEFT JOIN INV1 WITH(NOLOCK) ON OINV.DocEntry = INV1.DocEntry
                LEFT JOIN OITM T1 WITH(NOLOCK) ON INV1.ItemCode = T1.ItemCode
                LEFT JOIN OSLP WITH(NOLOCK) ON OINV.SlpCode = OSLP.SlpCode
                WHERE OINV.Canceled = 'N' AND OINV.U_BE1_IHRDOSNO IS NOT NULL

                UNION ALL

                SELECT
                    ODRF.U_BE1_IHRDOSNO AS DosyaNo,
                    COALESCE(OSLP.SlpName, 'Bilinmiyor') AS Satisci,
                    ODRF.NumAtCard AS FaturaNo,
                    FORMAT(ODRF.U_BE1_NAKLIYETARIHI, 'dd.MM.yyyy') AS FaturaTarihi,
                    ODRF.DocDate AS RawDocDate,
                    ODRF.CardName AS Musteri,
                    CRY.Name AS Ulke,
                    DRF1.ItemCode AS KalemKodu,
                    DRF1.Dscription AS KalemAdi,
                    DRF1.Quantity AS Miktar,
                    DRF1.unitMsr AS Olcu,
                    CAST(DRF1.LineTotal / ISNULL((SELECT TOP 1 T0.Rate FROM ORTT T0 WHERE T0.Currency = ODRF.DocCur AND T0.RateDate <= ODRF.DocDate ORDER BY T0.RateDate DESC), 1) AS DECIMAL(19,2)) AS TutarBelge,
                    ODRF.DocCur AS ParaBirimi,
                    FORMAT(ODRF.DocDate, 'yyyy MMMM', 'tr-TR') AS YilAy,
                    T1.U_BE1_GTIP AS GTIP,
                    N'Taslak' AS BelgeTuru
                FROM ODRF WITH(NOLOCK)
                LEFT JOIN OCRD C WITH(NOLOCK) ON C.CardCode = ODRF.CardCode
                LEFT JOIN OCRY CRY WITH(NOLOCK) ON CRY.Code = C.Country
                LEFT JOIN DRF1 WITH(NOLOCK) ON ODRF.DocEntry = DRF1.DocEntry
                LEFT JOIN OITM T1 WITH(NOLOCK) ON DRF1.ItemCode = T1.ItemCode
                LEFT JOIN OSLP WITH(NOLOCK) ON ODRF.SlpCode = OSLP.SlpCode
                WHERE ODRF.Canceled = 'N' AND ODRF.DocStatus = 'O' AND ODRF.U_BE1_IHRDOSNO IS NOT NULL
            )
            SELECT DosyaNo, Satisci, FaturaNo, FaturaTarihi, RawDocDate, Musteri, Ulke,
                   KalemKodu, KalemAdi, Miktar, Olcu, TutarBelge, ParaBirimi, YilAy, GTIP, BelgeTuru
            FROM CombinedData
            ORDER BY RawDocDate DESC, FaturaNo;";

        [HttpGet]
        public IActionResult Index()
 {ViewBag.CurrentDbKey = "";
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor161Controller.IhracatDosyaSatir>(12));
}

        public class IhracatDosyaSatir
        {
            public string DosyaNo { get; set; }
            public string Satisci { get; set; }
            public string FaturaNo { get; set; }
            public string FaturaTarihi { get; set; }
            public DateTime? RawDocDate { get; set; }
            public string Musteri { get; set; }
            public string Ulke { get; set; }
            public string KalemKodu { get; set; }
            public string KalemAdi { get; set; }
            public decimal Miktar { get; set; }
            public string Olcu { get; set; }
            public decimal TutarBelge { get; set; }
            public string ParaBirimi { get; set; }
            public string YilAy { get; set; }
            public string GTIP { get; set; }
            public string BelgeTuru { get; set; }
        }
    }
}
