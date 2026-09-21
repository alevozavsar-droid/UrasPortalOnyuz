// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http;
using System.Net.Http.Headers;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

using WebApplication3.Services;
namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor107Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor107Controller> _logger;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA", DbName = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE", DbName = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER", DbName = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA", DbName = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA", DbName = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI", DbName = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO", DbName = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA", DbName = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA_KIMYA", DbName = "ASIA_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM", DbName = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING", DbName = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS", DbName = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS", DbName = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS", DbName = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S", DbName = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S", DbName = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN", DbName = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026", DbName = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026", DbName = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026", DbName = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS", DbName = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI", DbName = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS", DbName = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI", DbName = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026", DbName = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026", DbName = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026", DbName = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026", DbName = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026", DbName = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER", DbName = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER", DbName = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER", DbName = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER", DbName = "URAS_POWER" },
        };

        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string dbKey)  {return default;
}

        private HashSet<string> GetActiveUDFs(string dbKey, string tableName)
 {return default;
}

        [HttpGet]
        public IActionResult Index()
 {ViewBag.CurrentDbDisplay = "";
ViewBag.AccountsAndBPs = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor107Controller.AccountBPModel>(12);
ViewBag.Currencies = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.Projects = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor107Controller.ProjectModel>(12);
ViewBag.Indicators = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.VatGroups = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor107Controller.VatGroupModel>(12);
ViewBag.TransCodes = WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.Rapor107Controller.ProjectModel>(12);
ViewBag.DosyaNolar = WebApplication3.OrnekDoldurucu.Liste<string>(12);
ViewBag.ActiveHeaderUDFs = new System.Collections.Generic.HashSet<string>();
ViewBag.ActiveLineUDFs = new System.Collections.Generic.HashSet<string>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}







        [HttpPost("AktarmaAlaniniAc")]
        public async Task<IActionResult> AktarmaAlaniniAc(bool tumSirketler = false)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), acilan = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("acilan", i2)).ToList(), zatenVar = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("zatenVar", i2)).ToList(), hatali = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("hatali", i2)).ToList() });
}




        private static readonly string GiderKolonAdi = GiderGelirKatalog.SatirUdf;


        private string GiderKoduKontrol(string connectionString, List<JournalEntryLineModel> satirlar)
 {return default;
}


        private void GiderleriYaz(string connectionString, int transId, List<JournalEntryLineModel> satirlar)
 {}


        [HttpPost("GiderAlaniniHazirla")]
        public async Task<IActionResult> GiderAlaniniHazirla()
 {return Json(new { success = true, mevcut = true, olusturuldu = false });
}




        private static readonly (string Kolon, string Ad, string Aciklama, int Boy)[] _aracAlanlari =
        {
            ("U_BE1_PLAKA", "BE1_PLAKA", "Araç Plakası", 20),
            ("U_BE1_SASI",  "BE1_SASI",  "Şasi No", 40),
        };


        [HttpPost("AracAlanlariniHazirla")]
        public async Task<IActionResult> AracAlanlariniHazirla()
 {return Json(new { success = true, mevcut = true, olusturulan = global::System.Linq.Enumerable.Range(0, 12).Select(i2 => global::WebApplication3.OrnekDoldurucu.Deger<string>("olusturulan", i2)).ToList() });
}


        [HttpGet("AracListesi")]
        public IActionResult AracListesi()
 {return Json(new { success = true, araclar = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "U_BE1_PLAKA", "Sasi", "Son" }) });
}


        private void AracAlanlariniYaz(string connectionString, int transId, List<JournalEntryLineModel> satirlar)
 {}

        [HttpGet("GetAllExchangeRates")]
        public IActionResult GetAllExchangeRates(string date)
 {return Json(global::WebApplication3.OrnekDoldurucu.Yeni<global::System.Collections.Generic.Dictionary<string, decimal>>());
}

        [HttpPost("UpdateExchangeRates")]
        public IActionResult UpdateExchangeRates([FromBody] ExchangeRateUpdateModel request)
 {return Json(new { success = true, message = "Kurlar ORTT tablosunda başarıyla güncellendi!" });
}

        [HttpGet("GetVatDetail")]
        public IActionResult GetVatDetail(string vatCode)
 {return Json(new { success = true, account = global::WebApplication3.OrnekDoldurucu.Deger<string>("account", 0), rate = global::WebApplication3.OrnekDoldurucu.Deger<decimal>("rate", 0) });
}


        [HttpGet("GetJENavigation")]
        public IActionResult GetJENavigation(int currentTransId, string direction)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor107Controller.JournalEntryViewModel>() });
}

        [HttpPost("FindJournalEntry")]
        public IActionResult FindJournalEntry([FromBody] JESearchModel search)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Controllers.Rapor107Controller.JESearchResultModel>(12) });
}

        [HttpGet("GetJournalEntry")]
        public IActionResult GetJournalEntry(int transId)
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Yeni<global::WebApplication3.Controllers.Rapor107Controller.JournalEntryViewModel>() });
}

        private string GetTransTypeName(int transType)
 {return default;
}
        [HttpPost("UpdateJournalEntry")]
        public async Task<IActionResult> UpdateJournalEntry([FromBody] JEUpdateModel request)
 {return Json(new { success = true, message = "Belge başarıyla güncellendi!" });
}

        [HttpGet("GetOpenManualJEs")]
        public IActionResult GetOpenManualJEs()
 {return Json(new { success = true, data = global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "TransId", "Number", "RefDate", "Memo" }) });
}

        [HttpPost("CreateJournalEntry")]
        public async Task<IActionResult> CreateJournalEntry([FromBody] JournalEntryCreationModel request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0), newTransId = global::WebApplication3.OrnekDoldurucu.Deger<int>("newTransId", 0) });
}

        public class BulkCancelRequest { public List<int> TransIds { get; set; } }
        [HttpPost("BulkCancelJournalEntries")]
        public async Task<IActionResult> BulkCancelJournalEntries([FromBody] BulkCancelRequest req)
 {return Json(new { success = true, hasErrors = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private List<Dictionary<string, object>> BuildJournalLines_Saf_TL(List<JournalEntryLineModel> rows, Dictionary<string, (string Curr, string IsCtrl)> dictAcc, Dictionary<string, (string Curr, string DebPayAcct, string DflAccount)> dictBp, Dictionary<string, string> ctrlAccToBp, string localCurrency, HashSet<string> activeLUdfs)
 {return default;
}

        private List<Dictionary<string, object>> BuildJournalLines_Normal(List<JournalEntryLineModel> rows, Dictionary<string, (string Curr, string IsCtrl)> dictAcc, Dictionary<string, (string Curr, string DebPayAcct, string DflAccount)> dictBp, Dictionary<string, string> ctrlAccToBp, string localCurrency, HashSet<string> activeLUdfs)
 {return default;
}

        private List<Dictionary<string, object>> BuildJournalLines_Ozel(List<JournalEntryLineModel> rows, Dictionary<string, (string Curr, string IsCtrl)> dictAcc, Dictionary<string, (string Curr, string DebPayAcct, string DflAccount)> dictBp, Dictionary<string, string> ctrlAccToBp, string localCurrency, bool isKarmaYevmiye, HashSet<string> activeLUdfs)
 {return default;
}

        private void AddLineUDFs(Dictionary<string, object> dict, JournalEntryLineModel r, HashSet<string> activeLUdfs)
 {}

        [HttpPost("CancelJournalEntry")]
        public async Task<IActionResult> CancelJournalEntry(int transId)
 {return Json(new { success = true, message = "Belge başarıyla iptal edildi (Türkçe Ters Kayıt Oluşturuldu)." });
}

        private string GetFormattedCompanyName(string displayCode)
 {return default;
}







        [HttpPost("MahsupOlustur")]
        public async Task<IActionResult> MahsupOlustur([FromBody] List<int> selectedTransIds)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}

        private List<AccountBPModel> GetAccountsAndBPs(string dbKey)
 {return default;
}

        private (string LocalCurrency, string SystemCurrency) GetCompanyCurrencies(string dbKey)
 {return default;
}

        private List<string> GetCurrencies(string dbKey)
 {return default;
}

        private List<ProjectModel> GetProjects(string dbKey)
 {return default;
}

        private List<ProjectModel> GetTransCodes(string dbKey)
 {return default;
}

        private List<string> GetDosyaNolar(string dbKey)
 {return default;
}

        private List<string> GetIndicators(string dbKey)
 {return default;
}

        private List<VatGroupModel> GetVatGroups(string dbKey)
 {return default;
}

        public class DatabaseConfig { public string Key { get; set; } public string Display { get; set; } public string DbName { get; set; } }
        public class AccountBPModel { public string Code { get; set; } public string Name { get; set; } public bool IsBP { get; set; } }
        public class ProjectModel { public string Code { get; set; } public string Name { get; set; } }
        public class VatGroupModel { public string Code { get; set; } public string Name { get; set; } public decimal Rate { get; set; } }

        public class ExchangeRateUpdateModel
        {
            public DateTime Date { get; set; }
            public Dictionary<string, decimal> Rates { get; set; }
        }

        public class JESearchModel
        {
            public int? TransId { get; set; }
            public int? Number { get; set; }
            public string RefDate { get; set; }
            public string Memo { get; set; }
        }

        public class JESearchResultModel
        {
            public int TransId { get; set; }
            public int Number { get; set; }
            public string RefDate { get; set; }
            public string Memo { get; set; }
        }

        public class JEUpdateModel : JournalEntryCreationModel
        {
            public int TransId { get; set; }
        }

        public class JournalEntryCreationModel
        {
            public DateTime RefDate { get; set; }
            public DateTime DueDate { get; set; }
            public DateTime TaxDate { get; set; }
            public string Memo { get; set; }
            public string Ref1 { get; set; }
            public string Ref2 { get; set; }
            public string Ref3 { get; set; }
            public string TransCode { get; set; }
            public string Project { get; set; }
            public string Indicator { get; set; }
            public bool AutoVAT { get; set; }
            public decimal SystemRate { get; set; }

            public string U_BE1_AKTAR { get; set; }
            public string U_BE1_TARGETDOC { get; set; }
            public string U_BE1_TARGETTYPE { get; set; }
            public string U_BE1_KREDITAKSITNO { get; set; }
            public string U_BE1_ISTAHAKKUK { get; set; }
            public string U_BE1_FINDOC { get; set; }
            public string U_BE1_FINTIP { get; set; }
            public string U_BE1_AGENTNUM { get; set; }
            public string U_BE1_BABSFLAG { get; set; }
            public string U_BE1_TERSKYT { get; set; }

            public string U_BE1_AKTARMA { get; set; }
            public string U_BE1_BABSKDV { get; set; }
            public string U_BE1_KURFARKTRANS { get; set; }
            public string U_BE1_ISCHECKBOND { get; set; }
            public string U_BE1_TRAN_STATUS { get; set; }
            public string U_BE1_MANUELC { get; set; }
            public string U_BE1_DOVIZTIPI { get; set; }
            public string U_BE1_TAHAKKUKKREDINO { get; set; }
            public string U_BE1_TAHAKKUKKREDISATIRNO { get; set; }
            public string U_BE1_TransKind { get; set; }
            public string U_BE1_YEVIPTAL { get; set; }
            public string U_BE1_IHRDOSNO { get; set; }
            public string U_BE1_TAHSILAT { get; set; }
            public string U_BE1_YEVIPTALLOG { get; set; }
            public string U_BE1_DAGIT { get; set; }

            public List<JournalEntryLineModel> JournalEntryLines { get; set; }
        }

        public class JournalEntryViewModel : JournalEntryCreationModel
        {
            public int TransId { get; set; }
            public int Number { get; set; }
            public bool IsCanceled { get; set; }
            public int TransType { get; set; }
            public string OriginName { get; set; }
            public new string RefDate { get; set; }
            public new string DueDate { get; set; }
            public new string TaxDate { get; set; }
            public new List<JournalEntryLineViewModel> Lines { get; set; } = new List<JournalEntryLineViewModel>();
        }

        public class JournalEntryLineModel
        {
            public string AccountCode { get; set; }
            public bool IsBP { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }

            public decimal FCDebit { get; set; }
            public decimal FCCredit { get; set; }
            public string FCCurrency { get; set; }

            public decimal SysDebit { get; set; }
            public decimal SysCredit { get; set; }

            public string LineMemo { get; set; }
            public string TaxGroup { get; set; }
            public string ProjectCode { get; set; }

            public string U_BE1_DAGIT { get; set; }
            public string U_BE1_CHECKNO { get; set; }
            public string U_BE1_CHECKID { get; set; }
            public string U_BE1_CHECKPORTFOY { get; set; }
            public string U_BE1_CHCKTRNSNO { get; set; }
            public string U_BE1_CONTRAACT { get; set; }
            public string U_BE1_MHTANIM { get; set; }
            public string U_BE1_CHECKDUEDATE { get; set; }
            public string U_GiderKodu { get; set; }

            public string Gider { get; set; }

            public string Plaka { get; set; }

            public string Sasi { get; set; }
        }

        public class JournalEntryLineViewModel : JournalEntryLineModel { }

        public class MahsupFisiModel
        {
            public string CardName { get; set; }
            public string Account { get; set; }
            public string ShortName { get; set; }
            public string HesapAdi { get; set; }
            public decimal Debit { get; set; }
            public decimal Credit { get; set; }
            public decimal FCDebit { get; set; }
            public decimal FCCredit { get; set; }
            public int TransId { get; set; }
            public string AcctName { get; set; }
            public DateTime RefDate { get; set; }
            public string Memo { get; set; }
            public string U_BE1_AKTAR { get; set; }
            public string IslemTip { get; set; }
            public string FCCurrency { get; set; }
            public decimal? Kur { get; set; }
            public string TutarYaziyla { get; set; }
            public string Comments { get; set; }
            public string U_NAME { get; set; }
        }

        public class MahsupDocument : IDocument
        {
            private readonly List<IGrouping<int, MahsupFisiModel>> _mahsuplar;
            private readonly string _companyName;

            public MahsupDocument(List<IGrouping<int, MahsupFisiModel>> mahsuplar, string companyName)
 {}

            public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
            public DocumentSettings GetSettings() => DocumentSettings.Default;

            public void Compose(IDocumentContainer container)
            {
                foreach (var group in _mahsuplar)
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1, Unit.Centimetre);
                        page.PageColor(Colors.White);
                        page.DefaultTextStyle(x => x.FontSize(9).FontFamily(Fonts.Arial));
                        page.Content().Element(c => ComposeContent(c, group));
                        page.Footer().Element(ComposeFooter);
                    });
                }
            }

            void ComposeContent(IContainer container, IGrouping<int, MahsupFisiModel> mahsup)
            {
                var firstRow = mahsup.First();

                container.Column(column =>
                {
                    column.Item().Border(1).BorderColor(Colors.Grey.Lighten1).Padding(10).Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text(text => { text.Span("Firma Ünvanı\t: ").SemiBold(); text.Span(_companyName).SemiBold(); });
                            c.Item().PaddingTop(5).Text(text => { text.Span("Fiş Tipi\t\t: ").SemiBold(); text.Span("Mahsup"); });
                            c.Item().PaddingTop(5).Text(text => { text.Span("Fiş Tarihi\t\t: ").SemiBold(); text.Span(firstRow.RefDate.ToString("dd.MM.yyyy")); });
                        });
                        row.ConstantItem(150).AlignRight().Text(text => { text.Span("Yevmiye Kaydı:\t").SemiBold(); text.Span(firstRow.TransId.ToString()); });
                    });

                    column.Item().PaddingTop(10);
                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Border(1).BorderColor(Colors.Black).Padding(2).Text("Hesap Kodu").SemiBold().AlignCenter();
                            header.Cell().Border(1).BorderColor(Colors.Black).Padding(2).Text("Hesap Açıklaması").SemiBold().AlignCenter();
                            header.Cell().Border(1).BorderColor(Colors.Black).Padding(2).Text("DVZ").SemiBold().AlignCenter();
                            header.Cell().Border(1).BorderColor(Colors.Black).Padding(2).Text("Kur").SemiBold().AlignCenter();
                            header.Cell().Border(1).BorderColor(Colors.Black).Padding(2).Text("Borç (D)").SemiBold().AlignCenter();
                            header.Cell().Border(1).BorderColor(Colors.Black).Padding(2).Text("Alacak (D)").SemiBold().AlignCenter();
                            header.Cell().Border(1).BorderColor(Colors.Black).Padding(2).Text("Borç (Y)").SemiBold().AlignCenter();
                            header.Cell().Border(1).BorderColor(Colors.Black).Padding(2).Text("Alacak (Y)").SemiBold().AlignCenter();
                        });

                        decimal totalBorcD = 0, totalAlacakD = 0, totalBorcY = 0, totalAlacakY = 0;

                        foreach (var item in mahsup)
                        {
                            totalBorcD += item.FCDebit; totalAlacakD += item.FCCredit;
                            totalBorcY += item.Debit; totalAlacakY += item.Credit;

                            table.Cell().BorderLeft(1).BorderRight(1).Padding(2).Text(item.Account);
                            table.Cell().BorderRight(1).Padding(2).Text(item.AcctName);
                            table.Cell().BorderRight(1).Padding(2).Text(item.FCCurrency).AlignCenter();
                            table.Cell().BorderRight(1).Padding(2).Text(item.Kur?.ToString("N4") ?? "1.0000").AlignRight();
                            table.Cell().BorderRight(1).Padding(2).Text(item.FCDebit > 0 ? item.FCDebit.ToString("N2") : "0.00").AlignRight();
                            table.Cell().BorderRight(1).Padding(2).Text(item.FCCredit > 0 ? item.FCCredit.ToString("N2") : "0.00").AlignRight();
                            table.Cell().BorderRight(1).Padding(2).Text(item.Debit > 0 ? item.Debit.ToString("N2") : "0.00").AlignRight();
                            table.Cell().BorderRight(1).Padding(2).Text(item.Credit > 0 ? item.Credit.ToString("N2") : "0.00").AlignRight();

                            table.Cell().BorderLeft(1).BorderRight(1).BorderBottom(1).Padding(2);
                            table.Cell().ColumnSpan(7).BorderRight(1).BorderBottom(1).Padding(2).Text(t =>
                            {
                                t.Span("Satır Açıklaması: ").SemiBold().FontSize(8);
                                t.Span(item.Memo ?? "").FontSize(8);
                            });
                        }

                        table.Cell().ColumnSpan(4).Border(1).Padding(2).AlignRight().Text("Toplam").SemiBold();
                        table.Cell().Border(1).Padding(2).AlignRight().Text(totalBorcD.ToString("N2")).SemiBold();
                        table.Cell().Border(1).Padding(2).AlignRight().Text(totalAlacakD.ToString("N2")).SemiBold();
                        table.Cell().Border(1).Padding(2).AlignRight().Text(totalBorcY.ToString("N2")).SemiBold();
                        table.Cell().Border(1).Padding(2).AlignRight().Text(totalAlacakY.ToString("N2")).SemiBold();
                    });

                    column.Item().PaddingTop(15).Border(1).Table(table =>
                    {
                        table.ColumnsDefinition(col => { col.RelativeColumn(); col.RelativeColumn(); col.RelativeColumn(); });
                        table.Cell().BorderRight(1).Padding(5).Column(c => {
                            c.Item().Text("Fişi Düzenleyen:").SemiBold();
                            c.Item().PaddingTop(15).Text(firstRow.U_NAME ?? "");
                        });
                        table.Cell().BorderRight(1).Padding(5).Column(c => {
                            c.Item().Text("Kontrol Eden:").SemiBold();
                            c.Item().PaddingTop(15);
                        });
                        table.Cell().Padding(5).Column(c => {
                            c.Item().Text("Onay:").SemiBold();
                            c.Item().PaddingTop(15);
                        });
                    });
                });
            }

            void ComposeFooter(IContainer container)
            {
                container.AlignCenter().Text(x =>
                {
                    x.Span("SAP Business One tarafından yazdırıldı. Sayfa ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            }
        }

        private class AccountState { public string Code { get; set; } public string FrozenFor { get; set; } public string ValidFor { get; set; } }
    }
}