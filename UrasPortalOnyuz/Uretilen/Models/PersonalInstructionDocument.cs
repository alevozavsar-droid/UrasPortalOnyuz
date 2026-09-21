using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebApplication3.Models;
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using System.Collections.Generic;

namespace WebApplication3.Documents
{
    public class PersonalInstructionDocument : IDocument
    {
        private readonly PaymentInstructionRequest _request;
        private readonly string _userName;
        private readonly string _databaseDisplayName;
        private readonly IWebHostEnvironment _env;

        public PersonalInstructionDocument(PaymentInstructionRequest request, string userName, string databaseDisplayName, IWebHostEnvironment env)
        {
            _request = request;
            _userName = userName;
            _databaseDisplayName = databaseDisplayName;
            _env = env;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;


        static IContainer CellStyle(IContainer container)
        {
            return container.Border(1).Padding(2);
        }

        static IContainer FooterCellStyle(IContainer container)
        {
            return container.Border(1).Background(Colors.Grey.Lighten3).Padding(2);
        }


        private string GetLogoPathForCompany()
        {
            string logoFileName = "default_logo.png"; // Varsayılan bir logo varsa

            switch (_databaseDisplayName)
            {
                case "URASKIMYA": logoFileName = "uras.png"; break;
                case "URSMAKINE": logoFileName = "urs.png"; break;
                case "AVRUPA_PAPER": logoFileName = "avrupa.png"; break;
                case "ALV_KIMYA": logoFileName = "alv.png"; break;
                case "DAF_KIMYA": logoFileName = "daf.png"; break;
                case "SELVI":
                case "SELVI_KIMYA": logoFileName = "selvi.png"; break;
                case "URAS_HOLDING": logoFileName = "holding.png"; break; // Resimde holding.png de gördüğüm için ekledim

                default: logoFileName = "default_logo.png"; break;
            }


            return Path.Combine(_env.WebRootPath, "images", "talimat", logoFileName);
        }
        private string GetCompanyNameForHeader()
        {
            switch (_databaseDisplayName)
            {
                case "URASKIMYA": return "URAS KİMYA SAN. TİC. A.Ş.";
                case "URSMAKINE": return "URS MAKİNE SAN. TİC. A.Ş.";
                case "AVRUPA_PAPER": return "AVRUPA KAĞIT SAN. TİC. A.Ş.";
                case "ALV_KIMYA": return "ALV KİMYA SAN. TİC. A.Ş.";
                case "DAF_KIMYA": return "DAF KİMYA SAN. TİC. A.Ş.";
                case "SELVI_KIMYA": return "SELVİ KİMYA SAN. TİC. A.Ş.";
                default: return "ŞİRKET ADI BURAYA GELECEK";
            }
        }

        private string GetHeaderAddressDetailsForCompany()
        {
            string address = "";
            string tel = "";
            string website = GetWebAddressesForCompany().FirstOrDefault() ?? "www.website.com";

            switch (_databaseDisplayName)
            {
                case "URASKIMYA":
                case "ALV_KIMYA":
                case "DAF_KIMYA":
                case "SELVI_KIMYA":
                    address = "Çobançeşme Mah. Sanayi Cad. Gençosman 1 Sk. No:14\nYenibosna, Bahçelievler / İSTANBUL";
                    tel = "Tel: +90 212 489 70 70";
                    break;
                case "URSMAKINE":
                    address = "Yeni Bosna Merkez Mah. Çınar Cad. No:6 Ertin Plaza\nBahçelievler İstanbul / TÜRKİYE";
                    tel = "Tel: +90 212 489 70 70";
                    break;
                case "AVRUPA_PAPER":
                    address = "Yakuplu Mah. Beysan San. Sit. Birlik Cad. No:3\nBeylikdüzü, İstanbul / TÜRKİYE";
                    tel = "Tel: +90 212 489 70 70";
                    break;
                default:
                    address = "Şirket Adresi Buraya Gelecek";
                    tel = "Tel: XXX XXX XX XX";
                    break;
            }
            return $"{address}\n{tel}\n{website}";
        }

        private string[] GetWebAddressesForCompany()
        {
            switch (_databaseDisplayName)
            {
                case "URASKIMYA": return new string[] { "www.uraskimya.com", "www.uraschemical.com" };
                case "URSMAKINE": return new string[] { "www.ursmakine.com", "www.ursmachine.com" };
                case "AVRUPA_PAPER": return new string[] { "www.avrupapaper.com" };
                case "ALV_KIMYA": return new string[] { "www.alvkimya.com" };
                case "DAF_KIMYA": return new string[] { "www.dafkimya.com" };
                case "SELVI_KIMYA": return new string[] { "www.selvikimya.com" };
                default: return new string[] { "www.website.com" };
            }
        }

        private string GetSignatureNameForCompany()
        {
            return "";
        }

        public void Compose(IDocumentContainer container)
        {
            string displayCurrencyCode = (_request.CompanyCurrency ?? "TL").ToUpper();
            if (displayCurrencyCode == "TRY") displayCurrencyCode = "TL";
            else if (displayCurrencyCode == "EUR") displayCurrencyCode = "EURO";
            else if (displayCurrencyCode == "USD") displayCurrencyCode = "USD";
            else if (string.IsNullOrWhiteSpace(displayCurrencyCode)) displayCurrencyCode = "TL";

            var personalInstructionDetails = _request.PersonalManualPayments;

            if (personalInstructionDetails == null || !personalInstructionDetails.Any())
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Content().Text("Görüntülenecek veri bulunamadı.").FontSize(12).FontColor(Colors.Red.Medium).AlignCenter();
                });
                return;
            }

            container.Page(page =>
            {
                page.MarginHorizontal(30);
                page.MarginBottom(30);
                page.MarginTop(20);


                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {
                        row.RelativeItem(3).AlignMiddle().Column(column =>
                        {
                            string logoPath = GetLogoPathForCompany();
                            if (File.Exists(logoPath))
                            {
                                column.Item().MaxHeight(30).AlignLeft().Image(logoPath).FitArea();
                            }
                            else
                            {
                                column.Item().AlignLeft().Text("Logo Yok").FontSize(10).FontColor(Colors.Grey.Lighten1);
                            }
                        });

                        row.RelativeItem(2).AlignMiddle().AlignRight().Column(column =>
                        {
                            column.Item().Text(GetCompanyNameForHeader()).FontSize(12).Bold().FontColor(Colors.Black).AlignRight();
                            column.Item().PaddingTop(5).Text(GetHeaderAddressDetailsForCompany()).FontSize(8).FontColor(Colors.Grey.Darken2).AlignRight();
                        });
                    });
                    headerCol.Item().PaddingTop(15).LineHorizontal(0.7f).LineColor(Colors.Grey.Lighten3);


                    string paymentMethods = string.Join(" / ", _request.PaymentMethods.Select(m => m.ToUpper()));
                    headerCol.Item().PaddingTop(15).Column(column =>
                    {
                        column.Item().Text($"ÖDEME TALİMATI ({paymentMethods})").FontSize(14).Bold().AlignCenter();
                        column.Item().PaddingTop(5).Text($"Talimat Tarihi: {_request.PaymentDate?.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) ?? DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(9).FontColor(Colors.Grey.Darken1).AlignRight();
                    });
                });


                page.Content().Column(content =>
                {

                    content.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text(_request.CompanyBankName ?? "").FontSize(10).Bold().AlignLeft();
                            col.Item().Text(_request.CompanyBranchName ?? "").FontSize(9).AlignLeft();
                        });
                    });


                    content.Item().PaddingTop(20).Text(text =>
                    {
                        text.AlignLeft();
                        text.Span($"Şubeniz nezdinde bulunan ").FontSize(9);
                        text.Span($" {_request.CompanyAccountNumber ?? _request.CompanyIban ?? ""}").FontSize(9).Bold();
                        text.Span($" numaralı ").FontSize(9);
                        text.Span($" {displayCurrencyCode}").FontSize(9).Bold();
                        text.Span($" hesabımızdan, aşağıdaki kişi/kurumlara belirtilen tutarların ödenmesini rica ederiz.").FontSize(9);
                    });


                    content.Item().PaddingTop(20).Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2f); // Adı Soyadı
                            columns.RelativeColumn(1.5f); // TCKN
                            columns.RelativeColumn(1.5f); // Tutar
                        });

                        table.Header(header =>
                        {
                            header.Cell().Element(FooterCellStyle).Text("Alıcı Adı / Ünvanı").FontSize(8).Bold();
                            header.Cell().Element(FooterCellStyle).Text("TCKN / VKN").FontSize(8).Bold();
                            header.Cell().Element(FooterCellStyle).AlignRight().Text("Tutar").FontSize(8).Bold();
                        });


                        foreach (var item in personalInstructionDetails)
                        {
                            table.Cell().Element(CellStyle).Text(item.ReceiverName ?? "-").FontSize(8);
                            table.Cell().Element(CellStyle).Text(item.ReceiverIdNumber ?? "-").FontSize(8);


                            decimal currentAmount = item.Amount.GetValueOrDefault(0);

                            table.Cell().Element(CellStyle).AlignRight().Text($"{currentAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} {displayCurrencyCode}").FontSize(8);
                        }


                        table.Footer(footer =>
                        {

                            decimal total = personalInstructionDetails.Sum(x => x.Amount.GetValueOrDefault(0));

                            footer.Cell().ColumnSpan(2).Element(FooterCellStyle).AlignRight().Text("TOPLAM").FontSize(8).Bold();
                            footer.Cell().Element(FooterCellStyle).AlignRight().Text($"{total.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))} {displayCurrencyCode}").FontSize(8).Bold();
                        });
                    });


                    content.Item().PaddingTop(25).AlignCenter().Text("Saygılarımızla;").FontSize(10).Bold().FontColor(Colors.Black);

                    content.Item().PaddingBottom(40);

                    string signatureName = GetSignatureNameForCompany();
                    if (!string.IsNullOrEmpty(signatureName))
                    {
                        content.Item().AlignCenter().Text(signatureName).FontSize(10).Bold().FontColor(Colors.Black);
                        content.Item().PaddingBottom(40);
                    }

                    content.Item().PaddingTop(20).Text("Talimatı Oluşturan: " + _userName).FontSize(8);
                });


                page.Footer()
                .AlignRight()
                .Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(8));
                    text.Span("Oluşturulma Tarihi: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    text.Span(" | Sayfa ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }
    }
}