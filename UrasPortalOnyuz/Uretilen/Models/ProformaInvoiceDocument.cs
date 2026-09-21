using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class ProformaInvoiceDocument : IDocument
    {
        public ProformaFaturaViewModel Model { get; }

        private static readonly string PrimaryColor = Colors.Blue.Darken2;
        private const int GeneralFontSize = 8;
        private const int HeaderTitleFontSize = 10;
        private const int ItemDescriptionFontSize = 7;
        private const int FooterTextFontSize = 7;

        public ProformaInvoiceDocument(ProformaFaturaViewModel model)
        {
            Model = model;


            Model.SellerCompanyName = "URS MAKİNA SANAYİ VE TİCARET A.Ş.";
            Model.SellerAddressLine1 = "ÇOBANÇEŞME MAH. SANAYİ CAD. GENÇ OSMAN 1 SK. NO:14";
            Model.SellerCityCountry = "BAHÇELİEVLER / İSTANBUL / TÜRKİYE";
            Model.SellerCityInvoice = "İSTANBUL / TÜRKİYE";
        }


        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        public void Compose(IDocumentContainer container)
        {
            container.Page(page =>
            {
                page.MarginHorizontal(35);
                page.MarginVertical(25);
                page.Size(PageSizes.A4);

                page.Header().Element(ComposeHeader);
                page.Content().Element(ComposeContent);
                page.Footer().Element(ComposeFooter);
            });
        }

        private void ComposeHeader(IContainer container)
 {}

        private void ComposeContent(IContainer container)
        {
            container.Column(column =>
            {
                column.Spacing(20);

                column.Item().Row(row =>
                {
                    row.RelativeItem(1).PaddingRight(15).Element(ComposeSellerBlock);
                    row.RelativeItem(1).PaddingLeft(15).Element(ComposeBuyerBlock);
                });

                column.Item().PaddingTop(15).Element(ComposeItemsTable);

                column.Item().PaddingTop(15).Element(ComposeShipmentNotes);
            });
        }

        private void ComposeSellerBlock(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(2);
                col.Item().Text("SELLER").FontSize(GeneralFontSize).Bold();

                col.Item().Text(Model.SellerCompanyName).FontSize(GeneralFontSize).SemiBold();
                col.Item().Text(Model.SellerAddressLine1).FontSize(GeneralFontSize);
                col.Item().Text(Model.SellerCityCountry).FontSize(GeneralFontSize);
                col.Item().Text($"CITY: {Model.SellerCityInvoice}").FontSize(GeneralFontSize);
            });
        }

        private void ComposeBuyerBlock(IContainer container)
        {
            container.Column(col =>
            {
                col.Spacing(2);
                col.Item().Text("BUYER").FontSize(GeneralFontSize).Bold();

                col.Item().Text(Model.CardName).FontSize(GeneralFontSize).SemiBold();
                col.Item().Text(Model.AddressName).FontSize(GeneralFontSize);
                col.Item().Text($"{Model.AddressLine2} {Model.AddressLine3}").FontSize(GeneralFontSize);
                col.Item().Text($"{Model.ZipCode} {Model.StateName}").FontSize(GeneralFontSize);
                col.Item().Text(Model.CountryName).FontSize(GeneralFontSize);
                col.Item().PaddingTop(5).Text("HSN/CODE: 641606").FontSize(GeneralFontSize).SemiBold();
                col.Item().Text("GSTIN:").FontSize(GeneralFontSize).SemiBold();
            });
        }




        private void ComposeShipmentNotes(IContainer container)
        {
            container.Column(col =>
            {

                string shipmentNote1Text = string.IsNullOrEmpty(Model.ShipmentNote1)
                    ? "14 weeks after or first payment"
                    : Model.ShipmentNote1;

                string shipmentNote2Text = Model.ShipmentNote2;


                col.Item().Row(row =>
                {

                    string title = (string.IsNullOrEmpty(shipmentNote2Text) || shipmentNote2Text == "-") ? "Shipment:" : "Shipment 1:";

                    row.ConstantItem(70).Text(title).FontSize(GeneralFontSize).Bold();
                    row.RelativeItem().Text(shipmentNote1Text).FontSize(GeneralFontSize);
                });


                if (!string.IsNullOrEmpty(shipmentNote2Text) && shipmentNote2Text != "-")
                {
                    col.Item().PaddingTop(2).Row(row =>
                    {
                        row.ConstantItem(70).Text("Shipment 2:").FontSize(GeneralFontSize).Bold();
                        row.RelativeItem().Text(shipmentNote2Text).FontSize(GeneralFontSize);
                    });
                }
            });
        }
        private void ComposeItemsTable(IContainer container)
        {
            var culture = new CultureInfo("en-US");
            var itemTextStyle = TextStyle.Default.FontSize(ItemDescriptionFontSize).FontFamily(Fonts.Arial);
            var headerTextStyle = TextStyle.Default.FontSize(GeneralFontSize).FontFamily(Fonts.Arial).Bold().FontColor(Colors.Black);
            var headerBackgroundColor = Colors.Grey.Lighten4;

            container.Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(50);
                    columns.RelativeColumn(15);
                    columns.RelativeColumn(15);
                    columns.RelativeColumn(20);
                });

                table.Header(header =>
                {
                    header.Cell().Border(1).BorderColor(Colors.Black).Background(headerBackgroundColor).Padding(3).Text("DESCRIPTION").Style(headerTextStyle);
                    header.Cell().Border(1).BorderColor(Colors.Black).Background(headerBackgroundColor).Padding(3).AlignCenter().Text("QUANTITY").Style(headerTextStyle);
                    header.Cell().Border(1).BorderColor(Colors.Black).Background(headerBackgroundColor).Padding(3).AlignRight().Text($"UNIT PRICE {Model.DocCur}").Style(headerTextStyle);
                    header.Cell().Border(1).BorderColor(Colors.Black).Background(headerBackgroundColor).Padding(3).AlignRight().Text($"TOTAL AMOUNT {Model.DocCur}").Style(headerTextStyle);
                });

                foreach (var item in Model.Lines)
                {
                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).Element(content =>
                    {
                        content.Column(col =>
                        {
                            col.Item().Text(item.ItemDescription).Style(itemTextStyle);
                            if (!string.IsNullOrEmpty(item.ItemCode))
                            {
                                col.Item().Text($"HSN/CODE : {item.ItemCode}").Style(itemTextStyle).FontSize(ItemDescriptionFontSize - 1).FontColor(Colors.Grey.Darken1);
                            }
                            if (!string.IsNullOrEmpty(item.LineDescription) || !string.IsNullOrEmpty(item.SatirAciklama))
                            {
                                col.Item().PaddingTop(5).Text("Standard features :").Style(itemTextStyle);
                                foreach (var feature in (item.LineDescription ?? item.SatirAciklama).Split('\n'))
                                {
                                    col.Item().Text(feature).Style(itemTextStyle);
                                }
                            }
                        });
                    });

                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignCenter().Text(item.Quantity.ToString("N0", culture)).Style(itemTextStyle);
                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(item.UnitPrice.ToString("N2", culture)).Style(itemTextStyle);
                    table.Cell().Border(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(item.TotalLineAmount.ToString("N2", culture)).Style(itemTextStyle);
                }

                table.Cell().ColumnSpan(2).BorderLeft(1).BorderBottom(1).BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text("FREIGHT COST").FontSize(GeneralFontSize);
                table.Cell().BorderBottom(1).BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text($"{Model.FreightCost.ToString("N2", culture)} {Model.DocCur}").FontSize(GeneralFontSize);
                table.Cell().BorderBottom(1).BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(" ").FontSize(GeneralFontSize);

                table.Cell().ColumnSpan(2).BorderLeft(1).BorderBottom(1).BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text("TOTAL AMOUNT (CPT)").FontSize(GeneralFontSize).Bold();
                table.Cell().BorderBottom(1).BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text($"{Model.TotalDocAmountWithFreight.ToString("N2", culture)} {Model.DocCur}").FontSize(GeneralFontSize).Bold();
                table.Cell().BorderBottom(1).BorderRight(1).BorderColor(Colors.Black).Padding(3).AlignRight().Text(" ").FontSize(GeneralFontSize);
            });
        }

        private void ComposeFooter(IContainer container)
        {
            container.PaddingVertical(10).Column(column =>
            {
                column.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Darken1);
                column.Spacing(10);

                column.Item().Row(row =>
                {
                    row.RelativeItem(3).Column(col =>
                    {
                        col.Spacing(10);

                        col.Item().Row(infoRow =>
                        {
                            infoRow.ConstantItem(80).AlignTop().Text("BANK INFORMATION").FontSize(FooterTextFontSize).Bold();
                            infoRow.RelativeItem().Column(detailCol =>
                            {
                                detailCol.Item().Text("Turkey Garanti Bankası A.Ş.").FontSize(FooterTextFontSize);
                                detailCol.Item().Text($"SWIFT: {Model.Swift}").FontSize(FooterTextFontSize);
                                detailCol.Item().Text($"IBAN: {Model.IBAN}").FontSize(FooterTextFontSize);
                                detailCol.Item().Text($"BRANCH: {Model.Branch ?? "TOPKAPI TİCARİ"}").FontSize(FooterTextFontSize);
                            });
                        });

                        col.Item().Row(infoRow =>
                        {
                            infoRow.ConstantItem(80).AlignTop().Text("VAT INFORMATION").FontSize(FooterTextFontSize).Bold();
                            infoRow.RelativeItem().Column(detailCol =>
                            {
                                detailCol.Item().Text("URS MAKİNE BASKI TEKNOLOJİLERİ").FontSize(FooterTextFontSize);
                                detailCol.Item().Text("VAT NUMBER: 89 30 556 287").FontSize(FooterTextFontSize);
                            });
                        });
                    });

                    row.RelativeItem(2).AlignBottom().Column(col =>
                    {
                        col.Spacing(5);

                        col.Item().Row(iconRow =>
                        {
                            iconRow.ConstantItem(20).AlignMiddle().Text("🌐").FontSize(FooterTextFontSize + 3);
                            iconRow.RelativeItem(1).Text("WEB SITE").FontSize(FooterTextFontSize).SemiBold();
                            iconRow.RelativeItem(2).Text("www.ursmakine.com").FontSize(FooterTextFontSize);
                        });

                        col.Item().Row(iconRow =>
                        {
                            iconRow.ConstantItem(20).AlignMiddle().Text("📞").FontSize(FooterTextFontSize + 3);
                            iconRow.RelativeItem(1).Text("COMPANY PHONE").FontSize(FooterTextFontSize).SemiBold();
                            iconRow.RelativeItem(2).Text("+90 212 552 2021").FontSize(FooterTextFontSize);
                        });

                        col.Item().Row(iconRow =>
                        {
                            iconRow.ConstantItem(20).AlignMiddle().Text("📧").FontSize(FooterTextFontSize + 3);
                            iconRow.RelativeItem(1).Text("CONTACT PERSON").FontSize(FooterTextFontSize).SemiBold();

                            iconRow.RelativeItem(2).Column(nameCol =>
                            {
                                nameCol.Item().Text(Model.SalesEmployee ?? "JOAO PAULO ARANJO JO FERNANDES").FontSize(FooterTextFontSize);
                                nameCol.Item().Text(Model.SalesEmployeeEmail ?? "").FontSize(FooterTextFontSize).FontColor(Colors.Grey.Darken2);
                            });
                        });
                    });
                });
            });
        }
    }
}
