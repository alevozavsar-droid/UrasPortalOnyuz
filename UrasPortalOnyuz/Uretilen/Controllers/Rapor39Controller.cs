// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor39Controller : Controller
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

        private List<CariViewModel> GetAccountList(string connectionString)
 {return default;
}

        private List<string> GetAktarimTipiList(string connectionString)
 {return default;
}

        private string GetAccountName(string connectionString, string acctCode)
 {return default;
}

        private List<string> GetLeafAccounts(string connectionString, List<string> selectedCodes)
 {return default;
}





        [HttpGet]
        public IActionResult Index(List<string> bpCode, DateTime? startDate, DateTime? endDate,
                                   [FromQuery] List<string> aktarimTipi, bool includeInitialBalance = false,
                                   string yevIptal = "All", bool detayliAciklama = true)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.SelectedYevIptal = "";
ViewBag.SelectedBpCodes = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.SelectedAktarimTipi = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.CariList = WebApplication3.OrnekDoldurucu.Liste<CariViewModel>(12);
ViewBag.AktarimTipiList = WebApplication3.OrnekDoldurucu.Liste<string>(12);
WebApplication3.OrnekDoldurucu.Hazirla(this);
// 19 Kas 2026: CARİ_MASTER kartı seçilince muhasebe hesap ekstresi gerçek kaynaktan — Cari Ekstre / Hesap Ekstresi Mutabakatı'nın hesap tarafı (120.01.xxx fiş satırları);
// Tahsilat raporu › Muhasebe Uyumu bu bakiyeyi karşılaştırır. Kart dışı seçimler eskisi gibi örnek satır.
var secilen = (bpCode ?? new List<string>()).SelectMany(x => (x ?? "").Split(',')).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();
var gercek = HesapEkstresi(secilen, startDate, endDate, aktarimTipi, includeInitialBalance);
ViewBag.SelectedBpCodes = secilen; ViewBag.SelectedAktarimTipi = aktarimTipi ?? new List<string>(); ViewBag.CariList = WebApplication3.Data.OrnekVeri.CarilerVeKartlar(); ViewBag.AktarimTipiList = new List<string> { "R", "X", "Y" }; ViewBag.IncludeInitialBalance = includeInitialBalance; ViewBag.DetayliAciklama = detayliAciklama; ViewBag.SelectedYevIptal = yevIptal ?? "All";
if (gercek != null) return View("Index", gercek);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.CariEkstreViewModel>(12));
}
        /// <summary>Seçilen kart carilerinin muhasebe hesap ekstresi satırları (EkstreKarsilastirmaOrnek.HesapSatirlari): kayıt tarihi, hesap kodu, belge / fiş, aktarım tipi, borç / alacak (TL), kümülatif bakiye. Kart carisi yoksa null.</summary>
        static List<CariEkstreViewModel> HesapEkstresi(List<string> kodlar, DateTime? startDate, DateTime? endDate, List<string> aktarimTipi, bool devirDahil)
        {
            var l = new List<CariEkstreViewModel>(); bool kartVar = false; var bas = startDate ?? new DateTime(DateTime.Today.Year, 1, 1); var bit = endDate ?? DateTime.Today; var tipler = (aktarimTipi ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            foreach (var kod in kodlar)
            {
                var c = WebApplication3.Data.MutabakatOrnek.Bul(kod); var k = c?.Kart; if (c == null || k == null) continue; kartVar = true;
                if (WebApplication3.Data.EkstreKarsilastirmaOrnek.HesapTanimsiz(c.CariKodu)) { l.Add(new CariEkstreViewModel { IslemNo = 0, SatirNo = 0, KayitTarihi = bas, MuhatapKodu = k.Kod, MuhatapAdi = k.ResmiUnvan, Aciklama = "Bu cari için muhasebe hesabı tanımlı değil (hesap açılmalı).", IslemTipi = "Bilgi", AktarimTipi = "", IslemPB = "TRY", Sirket = k.Sirket, TRYB = 0, TRYA = 0, TRY_KmlBky = 0 }); continue; }
                List<WebApplication3.Data.MtHareket> h; lock (WebApplication3.Data.MutabakatOrnek.Kilit) h = c.Hareketler.ToList(); if (h.Count == 0) continue;
                var cari = WebApplication3.Data.EkstreKarsilastirmaOrnek.CariSatirlari(c, h.Min(x => x.Tarih), DateTime.Today.AddYears(5)); var hesap = WebApplication3.Data.EkstreKarsilastirmaOrnek.HesapSatirlari(c, cari); string hk = WebApplication3.Data.EkstreKarsilastirmaOrnek.HesapKodu(k);
                decimal devir = Math.Round(hesap.Where(s => s.Tarih.Date < bas.Date).Sum(s => s.Borc - s.Alacak), 2), bakiye = devirDahil ? devir : 0; int no = 0;
                if (devirDahil) l.Add(new CariEkstreViewModel { IslemNo = 0, SatirNo = 0, KayitTarihi = bas, VadeTarihi = bas, MuhatapKodu = hk, MuhatapAdi = k.ResmiUnvan, Aciklama = $"Devir Bakiyesi · {k.Kod} {k.KisaAd}", IslemTipi = "Devir", AktarimTipi = "", IslemPB = "TRY", Sirket = k.Sirket, TRYB = devir > 0 ? devir : 0, TRYA = devir < 0 ? -devir : 0, IslemB = devir > 0 ? devir : 0, IslemA = devir < 0 ? -devir : 0, TRY_KmlBky = devir, Islem_KmlBky = devir });
                foreach (var s in hesap.Where(s => s.Tarih.Date >= bas.Date && s.Tarih.Date <= bit.Date && (tipler.Count == 0 || tipler.Contains(s.IslemTipi ?? ""))))
                {
                    bakiye = Math.Round(bakiye + s.Borc - s.Alacak, 2);
                    l.Add(new CariEkstreViewModel { IslemNo = ++no, SatirNo = 1, KayitTarihi = s.Tarih, VadeTarihi = s.Tarih, MuhatapKodu = hk, MuhatapAdi = k.ResmiUnvan, Aciklama = $"{s.Tip} - Belge No: {s.Belge} · Fiş {s.Fis}{(string.IsNullOrWhiteSpace(s.Aciklama) ? "" : " · " + s.Aciklama)}", IslemTipi = s.Tip, AktarimTipi = s.IslemTipi ?? "X", IslemPB = "TRY", Sirket = k.Sirket, TRYB = s.Borc, TRYA = s.Alacak, IslemB = s.Borc, IslemA = s.Alacak, TRY_KmlBky = bakiye, Islem_KmlBky = bakiye, CheckNum = s.Tip == "Çek" || s.Tip == "Senet" ? s.Belge : null });
                }
            }
            return kartVar ? l : null;
        }



















        private static readonly ConcurrentDictionary<string, bool> _udfVarMi =
            new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        private bool UdfVarMi(string connectionString, string tablo, string kolon)
 {return default;
}


        private string UdfIfadesi(string connectionString, string tablo, string kolon, string takmaAd)  {return default;
}

        private List<CariEkstreViewModel> GetCariEkstreData(string connectionString, string acctCode, DateTime? startDate, DateTime? endDate, List<string> aktarimTipi, bool includeInitialBalance, string exclusionKeyword = null, string yevIptal = "All", bool detayliAciklama = true)
 {return default;
}





        [HttpGet("downloadpdf")]
        [Authorize]
        public IActionResult DownloadPdf(List<string> bpCode, DateTime? startDate, DateTime? endDate,
                                         [FromQuery] List<string> aktarimTipi, bool includeInitialBalance = false,
                                         string orientation = "Portrait", string yevIptal = "All", bool detayliAciklama = true)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}





        [HttpGet("downloadexcel")]
        [Authorize]
        public IActionResult DownloadExcel(List<string> bpCode, DateTime? startDate, DateTime? endDate,
                                           [FromQuery] List<string> aktarimTipi, bool includeInitialBalance = false,
                                           string yevIptal = "All", bool detayliAciklama = true)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

    }
}