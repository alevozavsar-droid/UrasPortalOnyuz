// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FinansRaporlama.Controllers
{



    public class Rapor93FilterModel
    {
        public DateTime StartDate { get; set; } = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        public DateTime EndDate { get; set; } = DateTime.Today;
        public string AktarTipi { get; set; } // X, R, Y vb.
        public int? TransId { get; set; } // Direkt Yevmiye Arama
        public List<string> SourceTypes { get; set; } = new List<string> { "ALL" }; // Çoklu Seçim İçin List Yapıldı
    }

    public class Rapor93ListModel
    {
        public int TransId { get; set; }
        public DateTime RefDate { get; set; }
        public int? DocNum { get; set; }
        public string CardName { get; set; }
        public string U_BE1_AKTAR { get; set; }
        public string Memo { get; set; }
        public decimal TotalDebit { get; set; }
        public int TransType { get; set; }
        public string DetailedSourceType { get; set; } // Detaylı Kaynak Tipi (ALIS_FAT, YURTICI_SATIS vb.)
    }

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




    [Authorize]
    [Route("[controller]")]
    public class Rapor93Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor93Controller> _logger;

        private string GetSelectedDatabase()
 {return default;
}

        [HttpGet("Index")]
        public async Task<IActionResult> Index()
 {ViewBag.CurrentDbDisplay = "";
ViewBag.Filter = WebApplication3.OrnekDoldurucu.Yeni<FinansRaporlama.Controllers.Rapor93FilterModel>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<FinansRaporlama.Controllers.Rapor93ListModel>(12));
}

        [HttpPost("Index")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(Rapor93FilterModel filter)
 {ViewBag.CurrentDbDisplay = "";
ViewBag.Filter = WebApplication3.OrnekDoldurucu.Yeni<FinansRaporlama.Controllers.Rapor93FilterModel>();
WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<FinansRaporlama.Controllers.Rapor93ListModel>(12));
}

        [HttpPost("MahsupOlustur")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MahsupOlustur([FromForm] List<int> selectedTransIds)
 {return Content("Ön yüz örneği: dosya üretilmez.", "text/plain; charset=utf-8");
}
    }




    public class MahsupDocument : IDocument
    {
        private readonly List<IGrouping<int, MahsupFisiModel>> _mahsuplar;

        public MahsupDocument(List<IGrouping<int, MahsupFisiModel>> mahsuplar)
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
                        c.Item().Text(text => { text.Span("Firma Ünvanı\t: ").SemiBold(); text.Span(firstRow.CardName ?? "BİLGİ YOK").SemiBold(); });
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
                        table.Cell().BorderRight(1).Padding(2).Text(item.Kur?.ToString("N4") ?? "").AlignRight();
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

                column.Item().PaddingTop(10).Text(t =>
                {
                    t.Span("Yalnız: ").SemiBold();
                    t.Span(firstRow.TutarYaziyla ?? "");
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
}