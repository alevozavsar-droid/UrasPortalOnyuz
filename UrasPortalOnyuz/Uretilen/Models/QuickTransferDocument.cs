// WebApplication3/Documents/QuickTransferDocument.cs
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebApplication3.Models; // Models namespace'i gerekli
using System;
using System.Globalization;
using System.IO;
using System.Linq; // FirstOrDefault için gerekli
using Microsoft.AspNetCore.Hosting; // IWebHostEnvironment için gerekli
using System.Text.RegularExpressions; // Regex için gerekli

namespace WebApplication3.Documents
{
    public class QuickTransferDocument : IDocument
    {
        private readonly PaymentInstructionRequest _request;
        private readonly string _userName;
        private readonly string _databaseDisplayName;
        private readonly IWebHostEnvironment _env;

        public QuickTransferDocument(PaymentInstructionRequest request, string userName, string databaseDisplayName, IWebHostEnvironment env)
        {
            _request = request;
            _userName = userName;
            _databaseDisplayName = databaseDisplayName;
            _env = env;
        }

        public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
        public DocumentSettings GetSettings() => DocumentSettings.Default;

        private string GetLogoPathForCompany()
        {
            string logoFileName = "default_logo.png";
            switch (_databaseDisplayName)
            {
                case "URASKIMYA": logoFileName = "uras1.jpg"; break;
                case "URSMAKINE": logoFileName = "urs.jpg"; break;
                case "AVRUPA_PAPER": logoFileName = "avrupa.jpg"; break;
                case "ALV_KIMYA": logoFileName = "alv2.jpg"; break;
                case "DAF_KIMYA": logoFileName = "daf.jpg"; break;
                case "SELVI_KIMYA": logoFileName = "selvi.jpg"; break;
                default: logoFileName = "default_logo.png"; break;
            }
            return Path.Combine(_env.WebRootPath, "images", logoFileName);
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

        private string[] GetCommissionersForCompany()
        {
            switch (_databaseDisplayName)
            {
                case "URASKIMYA": return new string[] { "Müjgan TUNÇ YÜCEL", "Haluk AKSU", "Suat ÖZYALÇIN" };
                case "URSMAKINE": return new string[] { "Müjgan TUNÇ YÜCEL", "Haluk AKSU", "Suat ÖZYALÇIN" };
                case "AVRUPA_PAPER": return new string[] { "Müjgan TUNÇ YÜCEL", "Haluk AKSU", "Suat ÖZYALÇIN" };
                case "ALV_KIMYA": return new string[] { "Özkan YILMAZ", "Engin GÜL", "Simla TURGUTLUGİL" };
                case "DAF_KIMYA": return new string[] { "Burak GÜL", "Elif AVCI" };
                case "SELVI_KIMYA": return new string[] { "Serkan KORKMAZ", "Pınar TEKİN" };
                default: return new string[] { "Komiser 1", "Komiser 2" };
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
            else if (displayCurrencyCode == "USD") displayCurrencyCode = "USD"; // USD de eklendi


            PersonalManualPaymentDetail quickTransferDetail = _request.PersonalManualPayments?.FirstOrDefault();

            if (quickTransferDetail == null)
            {

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
                });


                page.Content().Column(content =>
                {

                    content.Item().PaddingTop(25).Row(row =>
                    {
                        row.RelativeItem(3).Column(col => // Sol taraf: Banka Adı ve Şube Kodu
                        {

                            col.Item().Text(_request.CompanyBankName ?? "").FontSize(10).Bold().AlignLeft();





                            string branchInfo = _request.CompanyBranchName; // Varsayılan olarak şirket şubesi



                            if (!string.IsNullOrWhiteSpace(quickTransferDetail.Description) &&
                                (quickTransferDetail.Description.Equals("ORTAKLARDAN ÖDENEN", StringComparison.OrdinalIgnoreCase) ||
                                 quickTransferDetail.Description.Contains("ŞUBE", StringComparison.OrdinalIgnoreCase) ||
                                 Regex.IsMatch(quickTransferDetail.Description, @"^\d{5}$"))) // Sadece 5 haneli sayıysa şube kodu olabilir
                            {
                                branchInfo = quickTransferDetail.Description; // Açıklamayı şube bilgisi olarak kullan
                            }

                            if (!string.IsNullOrWhiteSpace(branchInfo))
                            {
                                col.Item().Text(branchInfo).FontSize(9).AlignLeft();
                            }
                        });

                        row.RelativeItem(2).Column(col => // Sağ taraf: Talimat Tarihi
                        {
                            col.Item().Text($"Talimat Tarihi: {_request.PaymentDate?.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) ?? DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(9).FontColor(Colors.Grey.Darken1).AlignRight();
                        });
                    });


                    content.Item().PaddingTop(20).Text(text =>
                    {
                        text.AlignLeft();
                        text.Span($"Şubeniz nezdinde bulunan ").FontSize(9);
                        text.Span($" {_request.CompanyAccountNumber ?? ""}").FontSize(9).Bold();
                        text.Span($" {displayCurrencyCode}").FontSize(9).Bold();


                        if (displayCurrencyCode == "TL")
                        {
                            text.Span($" hesabımızdan aşağıda bulunan havale - eft işleminin yapılmasını rica ederiz.").FontSize(9);
                        }
                        else
                        {

                            text.Span($" hesabımızdan aşağıda bulunan transfer işleminin aynı gün valörlü olarak yapılmasını rica ederiz.").FontSize(9);
                        }
                    });

                    content.Item().PaddingTop(25).AlignCenter()
                        .Text("Saygılarımızla;").FontSize(10).Bold().FontColor(Colors.Black);


                    content.Item().PaddingBottom(40);

                    string signatureName = GetSignatureNameForCompany();
                    if (!string.IsNullOrEmpty(signatureName))
                    {
                        content.Item().PaddingTop(10);
                        content.Item().AlignCenter().Text(signatureName).FontSize(10).Bold().FontColor(Colors.Black);
                    }






                    content.Item().PaddingVertical(15)
                        .LineHorizontal(0.7f)
                        .LineColor(Colors.Grey.Lighten3);



                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(50); // Label sütunu için sabit genişlik
                            columns.RelativeColumn(); // Değer sütunu için esnek genişlik
                        });


                        table.Cell().Padding(2).Text("Alıcı").FontSize(9);
                        table.Cell().Padding(2).Text($": {_request.SupplierName ?? quickTransferDetail.ReceiverName ?? ""}").FontSize(9).Bold();


                        table.Cell().Padding(2).Text("Banka").FontSize(9);
                        table.Cell().Padding(2).Text($": {_request.SupplierBankName ?? quickTransferDetail.BankName ?? ""}").FontSize(9).Bold();


                        table.Cell().Padding(2).Text("Iban").FontSize(9);
                        table.Cell().Padding(2).Text($": {_request.SupplierIban ?? quickTransferDetail.Iban ?? ""}").FontSize(9).Bold();


                        table.Cell().Padding(2).Text("Açıklama").FontSize(9);
                        table.Cell().Padding(2).Text($": {quickTransferDetail.Description ?? ""}").FontSize(9).Bold();


                        table.Cell().Padding(2).Text("Tutar").FontSize(9);
                        table.Cell().Padding(2).Text($": {quickTransferDetail.Amount?.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) ?? "0.00"} {displayCurrencyCode}").FontSize(9).Bold();
                    });


                    content.Item().PaddingTop(25).LineHorizontal(0.7f).LineColor(Colors.Grey.Lighten3);



                    content.Item().PaddingTop(25).AlignCenter()
                        .Text("KONKORDATO KOMİSER HEYETİ").FontSize(10).Bold().FontColor(Colors.Black);

                    content.Item().PaddingTop(5).Row(row =>
                    {
                        foreach (var commissioner in GetCommissionersForCompany())
                        {
                            row.RelativeItem().AlignCenter().Text(commissioner).FontSize(9).FontColor(Colors.Black);
                        }
                    });
                });
            });
        }
    }
}