using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Linq;

namespace WebApplication3.Models
{



    public static class PackingListExtensions
    {
        public static IContainer ContentStyle(this IContainer container) =>
            container.Border(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(2).PaddingHorizontal(3).DefaultTextStyle(x => x.FontSize(9));

        public static IContainer HeaderStyle(this IContainer container) =>
            container.Background(Colors.Grey.Lighten3).Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().DefaultTextStyle(x => x.FontSize(9).SemiBold());

        public static IContainer ItemDescriptionStyle(this IContainer container) =>
            container.Border(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(2).PaddingLeft(15).DefaultTextStyle(x => x.FontSize(9));
    }




    public class PackingListDocument : IDocument
    {
        private readonly PackingListViewModel _model;

        public PackingListDocument(PackingListViewModel model)
        {
            _model = model;
        }

        public DocumentSettings GetSettings() => new DocumentSettings();
        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;

        public void Compose(IDocumentContainer container)
        {
            container
                .Page(page =>
                {
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontFamily(Fonts.Arial).FontSize(10));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(ComposeContent);
                    page.Footer().Element(ComposeFooter); // DÜZELTME 3 İÇİN METOT İMZASI DEĞİŞTİRİLDİ
                });
        }

        void ComposeHeader(IContainer container)
        {
            container.Column(column =>
            {
                column.Item().Text("PACKING LIST / AMBALAJ LİSTESİ")
                    .Bold().FontSize(14).SemiBold().FontColor(Colors.Blue.Medium).AlignCenter();

                column.Item().PaddingTop(5).Row(row =>
                {

                    row.RelativeItem(1).Column(col =>
                    {
                        col.Item().Text("Shipper / Gönderici").Bold().Underline();
                        col.Item().Text(_model.GondericiAdi ?? "URAS MAKİNA A.Ş.");
                        col.Item().Text(_model.GondericiAdresi ?? "Gönderici Adresi Bilinmiyor").FontSize(8);
                        col.Item().Container().PaddingTop(5).AlignLeft().Text($"Invoice No: {_model.FaturaNo}").Bold();
                        col.Item().Text($"Date: {_model.Tarih?.ToString("dd.MM.yyyy") ?? ""}");
                    });


                    row.RelativeItem(1).Column(col =>
                    {
                        col.Item().Text("Consignee / Alıcı").Bold().Underline();
                        col.Item().Text(_model.AliciAdi ?? "Bilinmiyor");
                        col.Item().Text(_model.AliciAdresi ?? "Alıcı Adresi Bilinmiyor").FontSize(8);
                        col.Item().Container().PaddingTop(5).AlignLeft().Text($"Main Description: {_model.AnaAciklama ?? ""}");
                    });
                });
            });
        }

        void ComposeContent(IContainer container)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(30);
                        columns.RelativeColumn(3.5f);
                        columns.ConstantColumn(50);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(80);
                        columns.ConstantColumn(60);
                        columns.ConstantColumn(60);
                        columns.RelativeColumn(1.2f);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("NO.");
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("MAIN DESCRIPTION");
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("QUANTITY");
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("NET WEIGHT (KG)");
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("GROSS WEIGHT (KG)");
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("DIMENSION (CM)");
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("TRUCK NO");
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("TRUCK ID");
                        header.Cell().Element(PackingListExtensions.HeaderStyle).Text("NOTE");




                    });

                    foreach (var line in _model.Lines)
                    {
                        bool isVolume = string.Equals(line.VolumeNo, line.DetailNo, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(line.VolumeNo);


                        table.Cell().Element(PackingListExtensions.ContentStyle).Text(line.DetailNo ?? line.VolumeNo).AlignCenter();

                        if (isVolume)
                        {

                            table.Cell().Element(PackingListExtensions.ContentStyle).Text($"VOLUME {line.VolumeNo}").Bold();
                            table.Cell().Element(PackingListExtensions.ContentStyle).Text(line.Quantity.ToString("N0")).AlignCenter();
                            table.Cell().Element(PackingListExtensions.ContentStyle).Text(line.NetAgirlik?.ToString("N3") ?? "-").AlignRight();
                            table.Cell().Element(PackingListExtensions.ContentStyle).Text(line.BrutAgirlik?.ToString("N3") ?? "-").AlignRight();
                            table.Cell().Element(PackingListExtensions.ContentStyle).Text(line.OlculerCM ?? "-").AlignCenter();
                            table.Cell().Element(PackingListExtensions.ContentStyle).Text(_model.Nakliyeci ?? "-").AlignCenter();
                            table.Cell().Element(PackingListExtensions.ContentStyle).Text(_model.TruckID ?? "-").AlignCenter();
                            table.Cell().Element(PackingListExtensions.ContentStyle).Text(line.SatirNotu ?? "-").AlignLeft();
                        }
                        else
                        {

                            table.Cell().Element(PackingListExtensions.ItemDescriptionStyle).Text(line.ItemDescription);
                            table.Cell().Element(PackingListExtensions.ContentStyle).Text(line.Quantity.ToString("N0")).AlignCenter();


                            table.Cell().Element(i => i.Border(1).BorderColor(Colors.Grey.Lighten1)); // Net Weight
                            table.Cell().Element(i => i.Border(1).BorderColor(Colors.Grey.Lighten1)); // Gross Weight
                            table.Cell().Element(i => i.Border(1).BorderColor(Colors.Grey.Lighten1)); // Dimension
                            table.Cell().Element(i => i.Border(1).BorderColor(Colors.Grey.Lighten1)); // Truck No
                            table.Cell().Element(i => i.Border(1).BorderColor(Colors.Grey.Lighten1)); // Truck ID
                            table.Cell().Element(i => i.Border(1).BorderColor(Colors.Grey.Lighten1)); // Note
                            table.Cell().Element(i => i.Border(1).BorderColor(Colors.Grey.Lighten1)); // Boş sütun (Toplam 9 sütun tamamlanmalı)
                        }
                    }


                    table.Cell().ColumnSpan(9).PaddingTop(10).Element(ComposeSummary);
                });
            });
        }





        IContainer ComposeSummary(IContainer container)
        {
            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(7);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                IContainer Style(IContainer styleContainer) => styleContainer
                    .Border(1)
                    .BorderColor(Colors.Black)
                    .Padding(3)
                    .DefaultTextStyle(x => x.FontSize(9));


                table.Cell().Element(Style).Text("Total Net Weight / Kg").Bold().AlignLeft();
                table.Cell().Element(Style).Text(_model.TotalNetAgirlik.ToString("N3")).Bold().AlignRight();
                table.Cell().Element(Style).Text("Kg").Bold();


                table.Cell().Element(Style).Text("Total Gross Weight / Kg").Bold().AlignLeft();
                table.Cell().Element(Style).Text(_model.TotalBrutAgirlik.ToString("N3")).Bold().AlignRight();
                table.Cell().Element(Style).Text("Kg").Bold();


                table.Cell().ColumnSpan(2).Element(Style).Text("Total Iron Transportation Platform").Bold().AlignLeft();
                table.Cell().Element(Style).Text(_model.TotalDemirPlatform.ToString()).Bold().AlignRight();
            });

            return container; // ✅ Container'ı geri döndür, Table metodunu değil
        }



        IContainer ComposeFooter(IContainer container)
        {
            container.AlignCenter().Text(x =>
            {
                x.CurrentPageNumber();
                x.Span(" / ");
                x.TotalPages();
            });

            return container; // ✅ Text işlemi yapılır, container geri döndürülür
        }
    }
}
