using WebApplication3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>Satış Faturası Oluştur — listeler ve yardımcı uçlar örnek veriyle; SAP'ye yazma yok.</summary>
    [Authorize]
    [Route("[controller]")]
    public class Rapor109Controller : Controller
    {
        public class AccountModel { public string AcctCode { get; set; } public string FormatCode { get; set; } public string AcctName { get; set; } }
        public class EmployeeModel { public int EmpId { get; set; } public string Ad { get; set; } }
        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class CustomerModel { public string CardCode { get; set; } public string CardName { get; set; } }
        public class ItemModel { public string ItemCode { get; set; } public string ItemName { get; set; } }
        public class SalesEmployeeModel { public int SlpCode { get; set; } public string SlpName { get; set; } }
        public class VatGroupModel { public string Code { get; set; } public string Name { get; set; } public decimal Rate { get; set; } }
        public class MuafCodeModel { public string Code { get; set; } public string Name { get; set; } public string Type { get; set; } }
        public class CountryModel { public string Code { get; set; } public string Name { get; set; } }
        public class StateModel { public string Code { get; set; } public string Country { get; set; } public string Name { get; set; } }
        public class WarehouseModel { public string WhsCode { get; set; } public string WhsName { get; set; } }
        public class DriverModel { public string DocEntry { get; set; } public string Name { get; set; } public string Plate { get; set; } public string Title { get; set; } public string VKN { get; set; } public string KVKK { get; set; } public string FirmaNo { get; set; } public string Tel { get; set; } }

        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index(int baseEntry = 0, int baseType = 0)
        {
            string db = Request.Query["db"].ToString(); if (string.IsNullOrEmpty(db)) db = Request.Cookies["SelectedDatabase"] ?? "DefaultConnection";
            ViewBag.CurrentDbDisplay = OrnekVeri.SirketAdi(db);
            ViewBag.Databases = OrnekVeri.Sirketler.Select(s => new DatabaseConfig { Key = s.DbKey, Display = s.Display, DbName = s.Display }).ToList();
            ViewBag.Customers = OrnekVeri.Cariler.Where(c => c.CardCode.StartsWith("M")).Select(c => new CustomerModel { CardCode = c.CardCode, CardName = c.CardName }).ToList();
            ViewBag.Items = OrnekVeri.Kalemler.Select(k => new ItemModel { ItemCode = k.Kod, ItemName = k.Ad }).ToList();
            ViewBag.SalesEmployees = new List<SalesEmployeeModel> { new SalesEmployeeModel { SlpCode = 1, SlpName = "Tolga Yaman" }, new SalesEmployeeModel { SlpCode = 2, SlpName = "Gizem Tan" }, new SalesEmployeeModel { SlpCode = 3, SlpName = "Esra Kaya" } };
            ViewBag.VatGroups = new List<VatGroupModel> { new VatGroupModel { Code = "S20", Name = "KDV %20", Rate = 20 }, new VatGroupModel { Code = "S10", Name = "KDV %10", Rate = 10 }, new VatGroupModel { Code = "S0", Name = "KDV %0 (İhracat)", Rate = 0 } };
            ViewBag.Currencies = new List<string> { "TRY", "USD", "EUR" };
            ViewBag.MuafCodes = new List<MuafCodeModel> { new MuafCodeModel { Code = "301", Name = "Mal İhracatı", Type = "IHR" }, new MuafCodeModel { Code = "350", Name = "Diğer Muafiyet", Type = "MUAF" } };
            ViewBag.Countries = new List<CountryModel> { new CountryModel { Code = "TR", Name = "Türkiye" }, new CountryModel { Code = "DE", Name = "Almanya" } };
            ViewBag.States = new List<StateModel> { new StateModel { Code = "34", Country = "TR", Name = "İstanbul" }, new StateModel { Code = "16", Country = "TR", Name = "Bursa" }, new StateModel { Code = "35", Country = "TR", Name = "İzmir" } };
            ViewBag.Warehouses = new List<WarehouseModel> { new WarehouseModel { WhsCode = "01", WhsName = "Merkez Depo" }, new WarehouseModel { WhsCode = "02", WhsName = "Bursa Depo" } };
            ViewBag.Accounts = new List<AccountModel> { new AccountModel { AcctCode = "_SYS00000000123", FormatCode = "600.01.001", AcctName = "Boya Satışları" }, new AccountModel { AcctCode = "_SYS00000000124", FormatCode = "649.01.001", AcctName = "Diğer Gelirler" } };
            ViewBag.Employees = OrnekVeri.Kullanicilar.Select((k, i) => new EmployeeModel { EmpId = i + 1, Ad = k.Ad }).ToList();
            ViewBag.Drivers = new List<DriverModel> { new DriverModel { DocEntry = "1", Name = "Mehmet Yılmaz", Plate = "34ABC123", Title = "Uras Lojistik", VKN = "1234567890", Tel = "0532 000 00 00" } };
            ViewBag.BaseEntry = baseEntry; ViewBag.BaseType = baseType;
            return View();
        }

        [HttpGet("GetAllExchangeRates")] public IActionResult GetAllExchangeRates() => Json(new { success = true, rates = new Dictionary<string, decimal> { ["USD"] = 41.2350m, ["EUR"] = 44.9810m, ["TRY"] = 1m }, data = new Dictionary<string, decimal> { ["USD"] = 41.2350m, ["EUR"] = 44.9810m, ["TRY"] = 1m } });
        [HttpPost("GiderAlaniniHazirla")] public IActionResult GiderAlaniniHazirla() => Json(new { success = true, mevcut = true, olusturuldu = false });
        [HttpGet("KalemAra")] public IActionResult KalemAra(string q) => Json(new { results = OrnekVeri.Kalemler.Where(k => string.IsNullOrEmpty(q) || (k.Kod + " " + k.Ad).ToLower().Contains(q.ToLower())).Take(30).Select(k => new { id = k.Kod, text = k.Kod + " - " + k.Ad }) });
        [HttpGet("HesapAra")] public IActionResult HesapAra(string q) => Json(new { results = new object[] { new { id = "_SYS00000000123", text = "600.01.001 - Boya Satışları" }, new { id = "_SYS00000000124", text = "649.01.001 - Diğer Gelirler" } } });
        [HttpGet("GetItemPrice")] public IActionResult GetItemPrice(string itemCode, string cardCode, string currency) { var k = OrnekVeri.Kalemler.FirstOrDefault(x => x.Kod == itemCode); return Json(new { success = true, price = k.Fiyat, currency = "TRY", uom = k.Birim }); }
        [HttpGet("GetItemStock")] public IActionResult GetItemStock(string itemCode, string whsCode) => Json(new { success = true, onHand = 1250m, committed = 120m, available = 1130m, stock = 1250m });
        [HttpGet("GetCustomerInfo")] public IActionResult GetCustomerInfo(string cardCode) { var c = OrnekVeri.Cariler.FirstOrDefault(x => x.CardCode == cardCode); return Json(new { success = true, cardName = c?.CardName, currency = "TRY", balance = 125340.50m, slpCode = 1, paymentTerms = "60 gün", vatGroup = "S20", phone = c?.Phone, address = c?.Address }); }
        [HttpGet("GetCustomerAddresses")] public IActionResult GetCustomerAddresses(string cardCode) { var c = OrnekVeri.Cariler.FirstOrDefault(x => x.CardCode == cardCode); return Json(new object[] { new { addressName = "MERKEZ", addressType = "B", street = c?.Address, city = "İstanbul", county = "Merkez", state = "34", country = "TR", zipCode = "34000" }, new { addressName = "FABRİKA", addressType = "S", street = c?.Address, city = "Bursa", county = "Nilüfer", state = "16", country = "TR", zipCode = "16000" } }); }
        [HttpGet("SatirHesabi")] public IActionResult SatirHesabi() => Json(new { success = true, hesapKodu = "600.01.001", hesapAdi = "Boya Satışları" });
        [HttpGet("GetOpenBaseDocuments")]
        public IActionResult GetOpenBaseDocuments(string cardCode, int baseType)
        {
            var cariler = OrnekVeri.Cariler.Where(c => c.CardCode.StartsWith("M") && (string.IsNullOrEmpty(cardCode) || c.CardCode == cardCode)).ToList();
            var liste = cariler.SelectMany((c, i) => Enumerable.Range(1, 3).Select(j => new { docEntry = 1000 + i * 10 + j, docNum = 1000 + i * 10 + j, docDate = DateTime.Today.AddDays(-j * 4).ToString("dd.MM.yyyy"), cardCode = c.CardCode, cardName = c.CardName, docTotal = 48123.54m * j, docCurrency = "TRY" })).ToList();
            return Json(new { success = true, data = liste });
        }
        [HttpGet("GetBaseDocument")]
        public IActionResult GetBaseDocument(int baseEntry, int baseType)
        {
            var cari = OrnekVeri.Cariler[(baseEntry / 10) % 5];
            var rnd = new Random(baseEntry);
            var lines = OrnekVeri.Kalemler.OrderBy(x => rnd.Next()).Take(4).Select((k, i) => new { itemCode = k.Kod, itemName = k.Ad, quantity = (decimal)rnd.Next(1, 12), unitPrice = k.Fiyat, currency = "TRY", vatGroup = "S20", warehouseCode = "01", lineTotal = k.Fiyat, baseType, baseEntry, baseLine = i, secUnitName = "", secUnitMultiplier = 0m, accountCode = (string)null, accountName = (string)null, description = k.Ad, gider = (string)null }).ToList();
            return Json(new { success = true, baseType, baseEntry, baseTypeName = baseType == 15 ? "Teslimat (İrsaliye)" : "Satış Siparişi", data = new { docEntry = baseEntry, docNum = baseEntry, cardCode = cari.CardCode, cardName = cari.CardName, docType = "I", docDate = DateTime.Today.ToString("yyyy-MM-dd"), docDueDate = DateTime.Today.AddDays(60).ToString("yyyy-MM-dd"), taxDate = DateTime.Today.ToString("yyyy-MM-dd"), slpCode = 1, ownerCode = 1, docCurrency = "TRY", docRate = 1m, comments = "Örnek baz belge", numAtCard = $"IUR2026{baseEntry:0000000}", shipToCode = "FABRİKA", payToCode = "MERKEZ", u_BE1_SEND = "1", u_BE1_AKTAR = "", addressExtension = new { }, lines } });
        }
        [HttpPost("CreateInvoice")] public IActionResult CreateInvoice() => Json(new { success = false, message = "Ön yüz örneği: SAP'ye fatura yazılmaz. Form ve hesaplamalar çalışır, kaydetme ana projede." });
        [HttpPost("FindInvoice")] public IActionResult FindInvoice() => Json(new { success = true, data = new object[] { new { docEntry = 17328, docNum = 17328, cardCode = "M0001", cardName = OrnekVeri.Cariler[0].CardName, docDate = DateTime.Today.ToString("dd.MM.yyyy"), docCur = "TRY" } } });
        [HttpGet("GetInvoiceDetails"), HttpGet("GetInvoiceNavigation")]
        public IActionResult GetInvoiceDetails(int docEntry = 17328) { var d = GetBaseDocument(docEntry, 15) as JsonResult; return Json(new { success = true, data = ((dynamic)d.Value).data, docStatus = "O", canceled = "N" }); }
        [HttpPost("AddDriver")] public IActionResult AddDriver() => Json(new { success = false, message = "Ön yüz örneğinde şoför kaydı yok." });
    }
}
