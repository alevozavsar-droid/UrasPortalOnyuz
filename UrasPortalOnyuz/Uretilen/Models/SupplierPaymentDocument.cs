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
    public class SupplierPaymentDocument : IDocument
    {
        private readonly PaymentInstructionRequest _request;
        private readonly string _userName;
        private readonly string _databaseDisplayName;
        private readonly IWebHostEnvironment _env;

        public SupplierPaymentDocument(PaymentInstructionRequest request, string userName, string databaseDisplayName, IWebHostEnvironment env)
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
            return container.Border(1).BorderColor(Colors.Grey.Lighten4).Padding(3);
        }

        static IContainer HeaderCellStyle(IContainer container)
        {
            return container.Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Grey.Lighten4).Padding(5);
        }

        static IContainer FooterCellStyle(IContainer container)
        {
            return container.Border(1).BorderColor(Colors.Grey.Lighten3).Background(Colors.Grey.Lighten4).Padding(3);
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
            string paymentCurrencyForTable = (_request.CompanyCurrency ?? "TRY").ToUpper();
            string displayCurrencyCode = paymentCurrencyForTable == "TRY" ? "TL" : paymentCurrencyForTable;


            bool hasDocuments = _request.SupplierPayments != null && _request.SupplierPayments.Any();
            bool hasNakit = _request.NakitAmount.GetValueOrDefault(0) > 0;
            bool hasChecks = _request.Checks != null && _request.Checks.Any();


            string rawBankName = _request.CompanyBankName ?? "-";
            string formattedBankName = rawBankName;
            if (!string.IsNullOrWhiteSpace(rawBankName) && rawBankName.Contains(" "))
            {
                var parts = rawBankName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 1)
                {
                    formattedBankName = string.Join(" ", parts.Take(parts.Length - 1));
                }
            }


            string rawBranchName = _request.CompanyBranchName;
            string formattedBranchName = !string.IsNullOrEmpty(rawBranchName)
                ? $"{rawBranchName} Şubesi Müdürlüğüne"
                : "-";

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
                            if (System.IO.File.Exists(logoPath))
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
                        row.RelativeItem(3).Column(col =>
                        {
                            col.Item().Text(formattedBankName).FontSize(10).Bold().AlignLeft();
                            col.Item().Text(formattedBranchName).FontSize(9).AlignLeft();
                        });

                        row.RelativeItem(2).Column(col =>
                        {
                            col.Item().Text($"Talimat Tarihi: {_request.PaymentDate?.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) ?? DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(9).FontColor(Colors.Grey.Darken1).AlignRight();
                        });
                    });


                    content.Item().PaddingTop(20).Text(text =>
                    {
                        text.AlignLeft();
                        text.Span($"Şubeniz nezdinde bulunan ").FontSize(9);
                        text.Span($" {_request.CompanyAccountNumber ?? "-"}").FontSize(9).Bold();
                        text.Span($" {_request.CompanyCurrency ?? displayCurrencyCode}").FontSize(9).Bold();
                        text.Span($" hesabımızdan aşağıda bulunan işlemlerin yapılmasını rica ederiz.").FontSize(9);
                    });


                    if (hasDocuments)
                    {
                        content.Item().PaddingTop(15).Table(table =>
                        {

                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3.8f);   // Tedarikçi Adı
                                columns.RelativeColumn(3.7f);   // IBAN
                                columns.RelativeColumn(2.5f);   // Ödeme Tutarı
                                columns.RelativeColumn(5.5f);   // Açıklama
                            });


                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Tedarikçi Adı").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("IBAN").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).AlignRight().Text($"Ödeme Tutarı ({displayCurrencyCode})").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("Açıklama").FontSize(9).Bold();
                            });


                            foreach (var supplierPayment in _request.SupplierPayments)
                            {
                                decimal totalLineAmount = 0;
                                string finalDescription = "";

                                if (supplierPayment.Documents != null && supplierPayment.Documents.Any())
                                {
                                    totalLineAmount = supplierPayment.Documents.Sum(d => d.TalimatTutari ?? 0);

                                    if (!string.IsNullOrWhiteSpace(supplierPayment.Description))
                                    {
                                        finalDescription = supplierPayment.Description;
                                    }
                                    else if (!string.IsNullOrEmpty(supplierPayment.Documents.First().Notes))
                                    {
                                        finalDescription = supplierPayment.Documents.First().Notes;
                                    }
                                    else
                                    {
                                        finalDescription = "Ödeme";
                                    }
                                }
                                else
                                {
                                    totalLineAmount = supplierPayment.TotalAmount.GetValueOrDefault(0);
                                    finalDescription = supplierPayment.Description ?? "Ödeme";
                                }

                                table.Cell().Element(CellStyle).Text(supplierPayment.SupplierName ?? "-").FontSize(8);


                                table.Cell().Element(CellStyle).Text(supplierPayment.SupplierIban ?? "-").FontSize(7).SemiBold();

                                table.Cell().Element(CellStyle).AlignRight().Text($"{totalLineAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(8);
                                table.Cell().Element(CellStyle).Text(finalDescription).FontSize(8);
                            }


                            table.Footer(footer =>
                            {
                                decimal totalAmount = _request.SupplierPayments
                                    .Where(sp => sp.Documents != null)
                                    .SelectMany(sp => sp.Documents)
                                    .Sum(d => d.TalimatTutari ?? 0);

                                if (totalAmount == 0)
                                {
                                    totalAmount = _request.SupplierPayments.Sum(sp => sp.TotalAmount.GetValueOrDefault(0));
                                }

                                footer.Cell().ColumnSpan(2).Element(FooterCellStyle).AlignRight().Text("TOPLAM").FontSize(8).Bold();
                                footer.Cell().Element(FooterCellStyle).AlignRight().Text($"{totalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(8).Bold();
                                footer.Cell().Element(FooterCellStyle).Text("");
                            });
                        });
                    }


                    if (hasNakit)
                    {
                        content.Item().PaddingTop(15).Text("Nakit Ödemesi").FontSize(10).Bold().FontColor(Colors.Black);
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4);
                                columns.RelativeColumn(8);
                                columns.RelativeColumn(4);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Ödeme Tipi").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("Açıklama").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).AlignRight().Text($"Tutar ({displayCurrencyCode})").FontSize(9).Bold();
                            });
                            table.Cell().Element(CellStyle).Text("Nakit").FontSize(8);
                            table.Cell().Element(CellStyle).Text("Portföyden Nakit Ödeme").FontSize(8);
                            table.Cell().Element(CellStyle).AlignRight().Text($"{_request.NakitAmount.GetValueOrDefault(0).ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(8);
                        });
                    }


                    if (hasChecks)
                    {
                        content.Item().PaddingTop(15).Text("Ödeme Yapılacak Çekler").FontSize(10).Bold().FontColor(Colors.Black);
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2.5f);
                            });
                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Çek No").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("Banka Adı").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("Ciro Eden").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("Asıl Borçlu").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Tutar").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("P. Birimi").FontSize(9).Bold();
                                header.Cell().Element(HeaderCellStyle).Text("Vade Tarihi").FontSize(9).Bold();
                            });

                            foreach (var check in _request.Checks)
                            {
                                table.Cell().Element(CellStyle).Text(check.CekNumarasi ?? "-").FontSize(8);
                                table.Cell().Element(CellStyle).Text(check.BankaAdi ?? "-").FontSize(8);
                                table.Cell().Element(CellStyle).Text(check.CiroEden ?? "-").FontSize(8);
                                table.Cell().Element(CellStyle).Text(check.AsilBorclu ?? "-").FontSize(8);
                                table.Cell().Element(CellStyle).AlignRight().Text(check.Tutar?.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) ?? "0.00").FontSize(8);
                                table.Cell().Element(CellStyle).Text(check.ParaBirimi ?? "-").FontSize(8);
                                table.Cell().Element(CellStyle).Text(check.VadeTarihi?.ToString("dd.MM.yyyy") ?? "-").FontSize(8);
                            }


                            table.Footer(footer =>
                            {
                                decimal totalChecks = _request.Checks.Sum(c => c.Tutar ?? 0);
                                footer.Cell().ColumnSpan(4).Element(FooterCellStyle).AlignRight().Text("TOPLAM").FontSize(8).Bold();
                                footer.Cell().Element(FooterCellStyle).AlignRight().Text($"{totalChecks.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(8).Bold();
                                footer.Cell().ColumnSpan(2).Element(FooterCellStyle).Text("");
                            });
                        });
                    }


                    content.Item().PaddingTop(30).AlignCenter().Text("Saygılarımızla;").FontSize(10).Bold().FontColor(Colors.Black);
                    content.Item().PaddingBottom(50);

                    string signatureName = GetSignatureNameForCompany();
                    if (!string.IsNullOrEmpty(signatureName))
                    {
                        content.Item().AlignCenter().Text(signatureName).FontSize(10).Bold().FontColor(Colors.Black);
                    }
                });
            });
        }
    }
}