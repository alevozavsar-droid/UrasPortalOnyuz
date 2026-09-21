using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WebApplication3.Models;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;
using System;
using System.Globalization;
using System.Collections.Generic;

namespace WebApplication3.Documents
{
    public class PaymentInstructionDocument : IDocument
    {
        private readonly PaymentInstructionRequest _request;
        private readonly string _userName;
        private readonly string _databaseDisplayName;
        private readonly IWebHostEnvironment _env;

        public PaymentInstructionDocument(PaymentInstructionRequest request, string userName, string databaseDisplayName, IWebHostEnvironment env)
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
            return container.Border(1).BorderColor(Colors.Black).Padding(3);
        }

        static IContainer HeaderCellStyle(IContainer container)
        {
            return container.Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).Padding(5);
        }

        static IContainer FooterCellStyle(IContainer container)
        {
            return container.Border(1).BorderColor(Colors.Black).Background(Colors.Grey.Lighten4).Padding(3);
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
                case "URAS_HOLDING": logoFileName = "holding.png"; break;
                default: logoFileName = "default_logo.png"; break;
            }

            return Path.Combine(_env.WebRootPath, "images", "talimat", logoFileName);
        }

        private string GetCompanyNameForHeader()
        {
            switch (_databaseDisplayName)
            {
                case "URASKIMYA": return "URAS KİMYA SAN. VE TİC. A.Ş.";
                case "URSMAKINE": return "URS MAKİNE BASKI TEKNOLOJİLERİ A.Ş.";
                case "AVRUPA_PAPER": return "AVRUPA PAPER MAKİNE SAN. VE DIŞ TİC. A.Ş.";
                case "ALV_KIMYA": return "ALV KİMYA SAN. İÇ VE DIŞ TİC. A.Ş.";
                case "DAF_KIMYA": return "ALV FİLO HİZMETLERİ VE TAŞIMACILIK A.Ş.";
                case "SELVI": return "SELVİ KİMYA TURİZM OTOMOTİV SANAYİ VE TİCARET LTD. ŞTİ.";
                case "SELVI_KIMYA": return "SELVİ KİMYA TURİZM OTOMOTİV SANAYİ VE TİCARET LTD. ŞTİ.";
                default: return "ŞİRKET ADI BURAYA GELECEK";
            }
        }

        private string GetHeaderAddressDetailsForCompany()
        {
            string address = "";
            string tel = "";
            string website = "www.website.com";

            var sites = GetWebAddressesForCompany();
            if (sites.Length > 0) website = sites[0];

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
                case "URASKIMYA": return new string[] { "www.uraskimya.com" };
                case "URSMAKINE": return new string[] { "www.ursmakine.com" };
                case "AVRUPA_PAPER": return new string[] { "www.avrupapaper.com" };
                case "ALV_KIMYA": return new string[] { "www.alvkimya.com" };
                case "DAF_KIMYA": return new string[] { "www.dafkimya.com" };
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

            bool hasDocuments = _request.SupplierPayments != null && _request.SupplierPayments.Any(sp => sp.Documents != null && sp.Documents.Any());
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


                page.MarginTop(15);


                page.Header().Column(headerCol =>
                {
                    headerCol.Item().Row(row =>
                    {

                        row.RelativeItem(3).AlignBottom().Column(column =>
                        {
                            string logoPath = GetLogoPathForCompany();
                            if (System.IO.File.Exists(logoPath))
                            {

                                column.Item().Width(150).AlignLeft().Image(logoPath);
                            }
                            else
                            {
                                column.Item().AlignLeft().Text("Logo Yok").FontSize(10).FontColor(Colors.Black);
                            }
                        });


                        row.RelativeItem(2).AlignBottom().AlignRight().Column(column =>
                        {
                            column.Item().Text(GetCompanyNameForHeader()).FontSize(12).Bold().FontColor(Colors.Black).AlignRight();
                            column.Item().PaddingTop(2).Text(GetHeaderAddressDetailsForCompany()).FontSize(8).FontColor(Colors.Black).AlignRight();
                        });
                    });


                    headerCol.Item().PaddingTop(2).LineHorizontal(1).LineColor(Colors.Black);
                });


                page.Content().Column(content =>
                {
                    content.Item().PaddingTop(10).Row(row =>
                    {
                        row.RelativeItem(3).Column(col =>
                        {
                            col.Item().Text(formattedBankName).FontSize(10).Bold().AlignLeft().FontColor(Colors.Black);
                            col.Item().Text(formattedBranchName).FontSize(9).AlignLeft().FontColor(Colors.Black);
                        });

                        row.RelativeItem(2).Column(col =>
                        {
                            col.Item().Text($"Talimat Tarihi: {_request.PaymentDate?.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR")) ?? DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.GetCultureInfo("tr-TR"))}")
                               .FontSize(9).FontColor(Colors.Black).AlignRight();
                        });
                    });

                    string currencyCode = _request.CompanyCurrency ?? displayCurrencyCode;
                    bool isValuedCurrency = new[] { "USD", "EUR", "GBP" }.Contains(currencyCode.ToUpper());

                    string endPhrase = isValuedCurrency
                        ? "hesabımızdan aşağıda bulunan işlemlerin aynı gün valörlü olarak yapılmasını rica ederiz."
                        : "hesabımızdan aşağıda bulunan işlemlerin yapılmasını rica ederiz.";

                    content.Item().PaddingTop(10).Text(text =>
                    {
                        text.AlignLeft();
                        text.Span($"Şubeniz nezdinde bulunan ").FontSize(9).FontColor(Colors.Black);
                        text.Span($" {_request.CompanyAccountNumber ?? "-"}").FontSize(9).Bold().FontColor(Colors.Black);

                        text.Span(" nolu ").FontSize(9).FontColor(Colors.Black);

                        text.Span($" {currencyCode}").FontSize(9).Bold().FontColor(Colors.Black);
                        text.Span($" {endPhrase}").FontSize(9).FontColor(Colors.Black);
                    });


                    if (hasDocuments)
                    {
                        content.Item().PaddingTop(15).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(4f);   // Tedarikçi
                                columns.RelativeColumn(3f);   // IBAN
                                columns.RelativeColumn(2.5f); // Tutar
                                columns.RelativeColumn(6f);   // Açıklama
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Tedarikçi Adı").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).Text("IBAN").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).AlignRight().Text($"Ödeme Tutarı ({displayCurrencyCode})").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).Text("Açıklama").FontSize(9).Bold().FontColor(Colors.Black);
                            });

                            foreach (var supplierPayment in _request.SupplierPayments)
                            {
                                if (supplierPayment.Documents != null && supplierPayment.Documents.Any())
                                {
                                    decimal totalLineAmount = supplierPayment.Documents.Sum(d => d.TalimatTutari ?? 0);

                                    var documentNumbers = new List<string>();
                                    bool containsSiparis = false;
                                    bool containsFatura = false;

                                    foreach (var doc in supplierPayment.Documents)
                                    {
                                        if (doc.ObjType == "18" || doc.ObjType == "19" || doc.ObjType == "13")
                                        {
                                            containsFatura = true;
                                        }
                                        else if (doc.ObjType == "22" || doc.ObjType == "17")
                                        {
                                            containsSiparis = true;
                                        }
                                        else if (!string.IsNullOrEmpty(doc.BelgeTipi))
                                        {
                                            var belgeTipiUpper = doc.BelgeTipi.ToUpper(new CultureInfo("tr-TR"));
                                            if (belgeTipiUpper.Contains("FATURA")) containsFatura = true;
                                            else if (belgeTipiUpper.Contains("SIPARIS") || belgeTipiUpper.Contains("SİPARİŞ")) containsSiparis = true;
                                        }

                                        string refNo = !string.IsNullOrEmpty(doc.FaturaNo) ? doc.FaturaNo : doc.DocNum;
                                        if (!string.IsNullOrEmpty(refNo))
                                        {
                                            documentNumbers.Add(refNo);
                                        }
                                    }

                                    string joinedNos = string.Join(", ", documentNumbers.Distinct());

                                    string suffix = "BELGE";
                                    if (containsFatura) suffix = "FATURA";
                                    else if (containsSiparis) suffix = "SİPARİŞ";

                                    string finalDescription = supplierPayment.Description;

                                    if (string.IsNullOrWhiteSpace(finalDescription) || finalDescription.Contains("BELGE ODEMESI"))
                                    {
                                        if (!string.IsNullOrEmpty(joinedNos))
                                        {
                                            finalDescription = $"{joinedNos} NOLU {suffix} ÖDEMESİ";
                                        }
                                        else
                                        {
                                            finalDescription = $"{suffix} ÖDEMESİ";
                                        }
                                    }

                                    table.Cell().Element(CellStyle).Text(supplierPayment.SupplierName ?? "-").FontSize(8).FontColor(Colors.Black);
                                    table.Cell().Element(CellStyle).Text(supplierPayment.SupplierIban ?? "-").FontSize(8).FontColor(Colors.Black);
                                    table.Cell().Element(CellStyle).AlignRight().Text($"{totalLineAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(8).FontColor(Colors.Black);
                                    table.Cell().Element(CellStyle).Text(finalDescription).FontSize(8).FontColor(Colors.Black);
                                }
                            }

                            table.Footer(footer =>
                            {
                                decimal totalAmount = _request.SupplierPayments
                                    .Where(sp => sp.Documents != null)
                                    .SelectMany(sp => sp.Documents)
                                    .Sum(d => d.TalimatTutari ?? 0);

                                footer.Cell().ColumnSpan(2).Element(FooterCellStyle).AlignRight().Text("TOPLAM").FontSize(8).Bold().FontColor(Colors.Black);
                                footer.Cell().Element(FooterCellStyle).AlignRight().Text($"{totalAmount.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(8).Bold().FontColor(Colors.Black);
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
                                header.Cell().Element(HeaderCellStyle).Text("Ödeme Tipi").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).Text("Açıklama").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).AlignRight().Text($"Tutar ({displayCurrencyCode})").FontSize(9).Bold().FontColor(Colors.Black);
                            });
                            table.Cell().Element(CellStyle).Text("Nakit").FontSize(8).FontColor(Colors.Black);
                            table.Cell().Element(CellStyle).Text("Portföyden Nakit Ödeme").FontSize(8).FontColor(Colors.Black);
                            table.Cell().Element(CellStyle).AlignRight().Text($"{_request.NakitAmount.GetValueOrDefault(0).ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(8).FontColor(Colors.Black);
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
                                header.Cell().Element(HeaderCellStyle).Text("Çek No").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).Text("Banka Adı").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).Text("Ciro Eden").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).Text("Asıl Borçlu").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).AlignRight().Text("Tutar").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).Text("P. Birimi").FontSize(9).Bold().FontColor(Colors.Black);
                                header.Cell().Element(HeaderCellStyle).Text("Vade Tarihi").FontSize(9).Bold().FontColor(Colors.Black);
                            });

                            foreach (var check in _request.Checks)
                            {
                                table.Cell().Element(CellStyle).Text(check.CekNumarasi ?? "-").FontSize(8).FontColor(Colors.Black);
                                table.Cell().Element(CellStyle).Text(check.BankaAdi ?? "-").FontSize(8).FontColor(Colors.Black);
                                table.Cell().Element(CellStyle).Text(check.CiroEden ?? "-").FontSize(8).FontColor(Colors.Black);
                                table.Cell().Element(CellStyle).Text(check.AsilBorclu ?? "-").FontSize(8).FontColor(Colors.Black);
                                table.Cell().Element(CellStyle).AlignRight().Text(check.Tutar?.ToString("N2", CultureInfo.GetCultureInfo("tr-TR")) ?? "0.00").FontSize(8).FontColor(Colors.Black);
                                table.Cell().Element(CellStyle).Text(check.ParaBirimi ?? "-").FontSize(8).FontColor(Colors.Black);
                                table.Cell().Element(CellStyle).Text(check.VadeTarihi?.ToString("dd.MM.yyyy") ?? "-").FontSize(8).FontColor(Colors.Black);
                            }

                            table.Footer(footer =>
                            {
                                decimal totalChecks = _request.Checks.Sum(c => c.Tutar ?? 0);

                                footer.Cell().ColumnSpan(4).Element(FooterCellStyle).AlignRight().Text("TOPLAM").FontSize(8).Bold().FontColor(Colors.Black);
                                footer.Cell().Element(FooterCellStyle).AlignRight().Text($"{totalChecks.ToString("N2", CultureInfo.GetCultureInfo("tr-TR"))}").FontSize(8).Bold().FontColor(Colors.Black);
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