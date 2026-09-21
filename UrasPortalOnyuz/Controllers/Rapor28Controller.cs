using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    /// <summary>Mizan — örnek hesap planı ve tutarlarla.</summary>
    [Authorize]
    [Route("[controller]")]
    public class Rapor28Controller : Controller
    {
        [HttpGet(""), HttpGet("Index"), HttpGet("IndexIslemTipiHaric")]
        public IActionResult Index(DateTime? startDate, DateTime? endDate, [FromQuery] List<string> aktarimTipi, [FromQuery] List<string> hesapKodlari, [FromQuery] List<string> anaHesap,
            bool kebirMizan = false, bool includeZeroBalance = false, string bakiyeTipi = "TRY", string mizanHesaplamaTuru = "SAP")
        {
            string db = Request.Query["db"].ToString(); if (string.IsNullOrEmpty(db)) db = Request.Cookies["SelectedDatabase"] ?? "DefaultConnection";
            var bas = startDate ?? new DateTime(DateTime.Today.Year, 1, 1); var bit = endDate ?? DateTime.Today;
            ViewBag.CurrentDbDisplay = OrnekVeri.SirketAdi(db);
            ViewBag.StartDate = bas.ToString("yyyy-MM-dd"); ViewBag.EndDate = bit.ToString("yyyy-MM-dd");
            ViewBag.SelectedAktarimTipi = aktarimTipi ?? new List<string>(); ViewBag.SelectedHesapKodlari = hesapKodlari ?? new List<string>(); ViewBag.SelectedAnaHesap = anaHesap;
            ViewBag.KebirMizan = kebirMizan; ViewBag.IncludeZeroBalance = includeZeroBalance; ViewBag.BakiyeTipi = bakiyeTipi; ViewBag.IslemTipiHaric = Request.Path.Value.EndsWith("IndexIslemTipiHaric", StringComparison.OrdinalIgnoreCase); ViewBag.MizanHesaplamaTuru = mizanHesaplamaTuru;
            ViewBag.AktarimTipiList = new List<string> { "R", "X", "Y" };
            ViewBag.HesapListWithLevels = OrnekVeri.HesapPlani.ToDictionary(h => h.HesapKodu, h => h.Level);
            ViewBag.HesapList = OrnekVeri.HesapPlani.Where(h => h.Postable == "Y").ToList();
            ViewBag.MainHesapList = OrnekVeri.HesapPlani.Where(h => h.Level < 6).ToList();

            var model = OrnekVeri.Mizan();
            if (Request.Query["calistir"] != "1") { ViewBag.RaporCalistirilmadi = true; return View("Index", new AktarimRaporuViewModel { HesapHareketleri = new List<HesapOzeti>(), DovizliHesapHareketleri = new List<HesapOzetiDovizli>() }); }
            if (kebirMizan) model.HesapHareketleri = model.HesapHareketleri.Where(h => h.DistinctBy <= 2).ToList();
            if (hesapKodlari != null && hesapKodlari.Count > 0) model.HesapHareketleri = model.HesapHareketleri.Where(h => hesapKodlari.Any(k => h.HesapKodu.StartsWith(k))).ToList();
            if (anaHesap != null && anaHesap.Count > 0) model.HesapHareketleri = model.HesapHareketleri.Where(h => anaHesap.Any(a => h.HesapKodu.StartsWith(a))).ToList();
            return View("Index", model);
        }

        [HttpGet("downloadexcel")] public IActionResult DownloadExcel() => Content("Ön yüz örneğinde Excel üretimi yok.");
        [HttpGet("GetMuavinDetay")] public IActionResult GetMuavinDetay(string hesapKodu) => Json(new { success = true, data = Enumerable.Range(1, 12).Select(i => new { tarih = DateTime.Today.AddDays(-i * 6), islemNo = 4000 + i, aciklama = $"Örnek muavin hareketi {i} · {hesapKodu}", borc = i % 2 == 0 ? 12500m * i : 0m, alacak = i % 2 == 1 ? 9800m * i : 0m }) });
        [HttpGet("GetKiyaslamaRaporu")] public IActionResult GetKiyaslamaRaporu() => Json(new { success = false, message = "Ön yüz örneğinde şirketler arası kıyas yok." });
    }
}
