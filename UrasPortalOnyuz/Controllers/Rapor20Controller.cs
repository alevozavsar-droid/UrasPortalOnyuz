using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    /// <summary>Cari Ekstre — örnek verilerle.</summary>
    [Authorize]
    [Route("[controller]")]
    public class Rapor20Controller : Controller
    {
        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index([FromQuery] List<string> bpCode, DateTime? startDate, DateTime? endDate, [FromQuery] List<string> aktarimTipi, [FromQuery] List<string> bakiyeDurumu,
            bool includeInitialBalance = true, bool includeConnectedBp = false, bool tlEkstre = false, bool isConsolidated = false, bool useBpDueDate = false, bool includeOpenOrders = false, bool isEnglish = false)
        {
            var secilen = (bpCode ?? new List<string>()).SelectMany(x => (x ?? "").Split(',')).Select(x => x.Trim()).Where(x => x.Length > 0).Distinct().ToList();
            var bas = startDate ?? new DateTime(DateTime.Today.Year, 1, 1); var bit = endDate ?? DateTime.Today;
            string db = Request.Query["db"].ToString(); if (string.IsNullOrEmpty(db)) db = Request.Cookies["SelectedDatabase"] ?? "DefaultConnection";

            ViewBag.CurrentDbDisplay = OrnekVeri.SirketAdi(db);
            ViewBag.SelectedBpCode = secilen.FirstOrDefault(); ViewBag.SelectedBpCodes = secilen;
            ViewBag.StartDate = bas.ToString("yyyy-MM-dd"); ViewBag.EndDate = bit.ToString("yyyy-MM-dd");
            ViewBag.SelectedAktarimTipi = aktarimTipi ?? new List<string>(); ViewBag.SelectedBakiyeDurumu = bakiyeDurumu ?? new List<string>();
            ViewBag.IncludeInitialBalance = includeInitialBalance; ViewBag.IncludeConnectedBp = includeConnectedBp; ViewBag.TlEkstre = tlEkstre; ViewBag.IsConsolidated = isConsolidated;
            ViewBag.UseBpDueDate = useBpDueDate; ViewBag.IncludeOpenOrders = includeOpenOrders; ViewBag.IsEnglish = isEnglish;
            ViewBag.CariList = OrnekVeri.Cariler; ViewBag.AktarimTipiList = new List<string> { "R", "X", "Y" };
            ViewBag.HasNotes = false; ViewBag.MutabakatUyarilari = new List<UyariViewModel>(); ViewBag.BpRelatedAccountBalances = new List<AccountBalanceViewModel>();
            ViewBag.SelectedBpNames = secilen.ToDictionary(k => k, k => OrnekVeri.Cariler.FirstOrDefault(c => c.CardCode == k)?.CardName ?? k);
            ViewBag.SelectedBpName = secilen.Count == 1 ? (string)ViewBag.SelectedBpNames[secilen[0]] : secilen.Count + " cari seçildi";
            ViewBag.ConnectedBpCode = null; ViewBag.ConnectedBpName = null; ViewBag.SorguHatasi = null; ViewBag.MutabakatUyariMesaji = null; ViewBag.MutabakatUyariTipi = null;

            var liste = new List<Rapor20CariEkstreViewModel>();
            foreach (var k in secilen) liste.AddRange(OrnekVeri.CariEkstre(k, bas, bit));
            if (secilen.Count > 0)
            {
                ViewBag.BpRelatedAccountBalances = new List<AccountBalanceViewModel> { new AccountBalanceViewModel { CompanyName = OrnekVeri.SirketAdi(db), AccountCode = "120.01.001", AccountName = "Müşteriler", TLBalance = liste.LastOrDefault()?.TRY_KmlBky ?? 0, Currency = "TRY" } };
                ViewBag.MutabakatUyarilari = new List<UyariViewModel> { new UyariViewModel { UyariTipi = "info", AnalizSonucu = "Son mutabakat 31.08.2026 tarihinde yapıldı (örnek)." } };
                ViewBag.MutabakatUyariMesaji = "Bu cari için mutabakat formu hazır (örnek veri)."; ViewBag.MutabakatUyariTipi = "success";
            }
            return View(liste);
        }

        [HttpGet("downloadexcel")] public IActionResult DownloadExcel() => Content("Ön yüz örneğinde Excel üretimi yok.");
        [HttpGet("downloadpdf")] public IActionResult DownloadPdf() => Content("Ön yüz örneğinde PDF üretimi yok.");
        [HttpGet("GetCariNotlar")] public IActionResult GetCariNotlar() => Json(new { success = true, data = new object[0] });
        [HttpGet("GetOpenTransactions")] public IActionResult GetOpenTransactions(string cardCode) => Json(new { success = true, data = OrnekVeri.CariEkstre(cardCode ?? "M0001", new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today).Where(x => x.BakiyeDurumu == "Açık" && x.IslemNo > 0).Take(15).Select(x => new { transId = x.IslemNo, lineId = x.SatirNo, refDate = x.KayitTarihi, dueDate = x.VadeTarihi, memo = x.Aciklama, debit = x.TRYB, credit = x.TRYA, balance = (x.TRYB ?? 0) - (x.TRYA ?? 0), currency = "TRY", isBond = false }) });
        [HttpPost("SaveManualMutabakat")] public IActionResult SaveManualMutabakat() => Json(new { success = true, message = "Örnek: mutabakat kaydedildi (bellek içi)." });
        [HttpGet("CheckUnreconciledForMutabakat")] public IActionResult CheckUnreconciledForMutabakat() => Json(new { success = true, acikVar = false, message = "" });
    }
}
