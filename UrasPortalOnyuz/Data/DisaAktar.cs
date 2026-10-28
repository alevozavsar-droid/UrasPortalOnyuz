// ============================================================
// DIŞA AKTARIM — PDF (QuestPDF) ve Excel (ClosedXML) için ortak tablo belgesi: başlık, alt başlık, özet kutuları, bir ya da daha çok tablo bloğu (kolonlar, sağa yaslı sayısal kolonlar, satırlar, toplam satırı).
//   Kullanan ekranlar: Tahsilat (ekstre, tahsilat makbuzu / simülasyon), Adat Hesaplama (adat raporu), Tedarikçi raporları.
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApplication3.Data
{
    public class DaBlok { public string Baslik { get; set; } public List<string> Kolonlar { get; set; } = new List<string>(); public List<bool> Sag { get; set; } = new List<bool>(); public List<string[]> Satirlar { get; set; } = new List<string[]>(); public string[] Toplam { get; set; } public string Not { get; set; } }
    public class DaBelge { public string Baslik { get; set; } public string AltBaslik { get; set; } public string Sirket { get; set; } = "Uras Grup"; public List<(string Etiket, string Deger)> Ozet { get; set; } = new List<(string, string)>(); public List<DaBlok> Bloklar { get; set; } = new List<DaBlok>(); public bool Yatay { get; set; } = true; public string Hazirlayan { get; set; } = ""; }

    public static class DisaAktar
    {
        static DisaAktar() { QuestPDF.Settings.License = LicenseType.Community; QuestPDF.Settings.EnableDebugging = false; }

        public static byte[] Pdf(DaBelge b)
        {
            var mavi = "#0369a1"; var gri = "#f1f5f9"; var cizgi = "#e2e8f0";
            return Document.Create(d => d.Page(p =>
            {
                p.Size(b.Yatay ? PageSizes.A4.Landscape() : PageSizes.A4); p.Margin(22); p.DefaultTextStyle(x => x.FontSize(7.5f).FontColor("#0f172a"));
                p.Header().Column(c =>
                {
                    c.Item().Row(r => { r.RelativeItem().Column(cc => { cc.Item().Text(b.Baslik).FontSize(14).Bold().FontColor(mavi); if (!string.IsNullOrEmpty(b.AltBaslik)) cc.Item().Text(b.AltBaslik).FontSize(8).FontColor("#475569"); }); r.ConstantItem(170).AlignRight().Column(cc => { cc.Item().Text(b.Sirket).Bold().FontSize(9); cc.Item().Text($"{DateTime.Now:dd.MM.yyyy HH:mm}").FontColor("#64748b"); if (!string.IsNullOrEmpty(b.Hazirlayan)) cc.Item().Text("Hazırlayan: " + b.Hazirlayan).FontColor("#64748b"); }); });
                    c.Item().PaddingTop(4).LineHorizontal(1).LineColor(mavi);
                    if (b.Ozet.Count > 0) c.Item().PaddingTop(5).Row(r => { foreach (var (e, v) in b.Ozet) r.RelativeItem().Background(gri).Padding(4).Column(cc => { cc.Item().Text(e.ToUpperInvariant()).FontSize(5.8f).Bold().FontColor("#94a3b8"); cc.Item().Text(v ?? "").FontSize(8.5f).Bold(); }); });
                });
                p.Content().PaddingTop(6).Column(col =>
                {
                    foreach (var bl in b.Bloklar)
                    {
                        if (!string.IsNullOrEmpty(bl.Baslik)) col.Item().PaddingTop(6).PaddingBottom(2).Text(bl.Baslik).FontSize(9).Bold().FontColor(mavi);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd => { for (int i = 0; i < bl.Kolonlar.Count; i++) cd.RelativeColumn(i == 0 ? 1.4f : 1f); });
                            t.Header(h => { for (int i = 0; i < bl.Kolonlar.Count; i++) { var sag = i < bl.Sag.Count && bl.Sag[i]; var cell = h.Cell().Background(mavi).Padding(3); if (sag) cell = cell.AlignRight(); cell.Text(bl.Kolonlar[i]).FontColor("#ffffff").Bold().FontSize(6.5f); } });
                            int n = 0;
                            foreach (var s in bl.Satirlar) { n++; for (int i = 0; i < bl.Kolonlar.Count; i++) { var sag = i < bl.Sag.Count && bl.Sag[i]; var cell = t.Cell().BorderBottom(0.5f).BorderColor(cizgi).Background(n % 2 == 0 ? "#fafafa" : "#ffffff").Padding(2.5f); if (sag) cell = cell.AlignRight(); cell.Text(i < s.Length ? s[i] ?? "" : ""); } }
                            if (bl.Toplam != null) for (int i = 0; i < bl.Kolonlar.Count; i++) { var sag = i < bl.Sag.Count && bl.Sag[i]; var cell = t.Cell().Background(gri).BorderTop(1).BorderColor("#94a3b8").Padding(3); if (sag) cell = cell.AlignRight(); cell.Text(i < bl.Toplam.Length ? bl.Toplam[i] ?? "" : "").Bold(); }
                        });
                        if (!string.IsNullOrEmpty(bl.Not)) col.Item().PaddingTop(2).Text(bl.Not).FontSize(6.5f).FontColor("#64748b");
                    }
                });
                p.Footer().AlignCenter().Text(x => { x.Span("Uras Portal · ön yüz örneği · sayfa ").FontColor("#94a3b8").FontSize(6.5f); x.CurrentPageNumber().FontColor("#94a3b8").FontSize(6.5f); x.Span(" / ").FontColor("#94a3b8").FontSize(6.5f); x.TotalPages().FontColor("#94a3b8").FontSize(6.5f); });
            })).GeneratePdf();
        }

        public static byte[] Excel(DaBelge b)
        {
            using var wb = new XLWorkbook(); int sayfa = 0;
            foreach (var bl in b.Bloklar)
            {
                sayfa++; var ad = string.IsNullOrEmpty(bl.Baslik) ? $"Sayfa{sayfa}" : new string(bl.Baslik.Where(ch => !"[]:*?/\\".Contains(ch)).ToArray()); if (ad.Length > 28) ad = ad.Substring(0, 28); if (wb.Worksheets.Any(w => w.Name == ad)) ad += sayfa;
                var ws = wb.Worksheets.Add(ad); int r = 1;
                ws.Cell(r, 1).Value = b.Baslik; ws.Cell(r, 1).Style.Font.Bold = true; ws.Cell(r, 1).Style.Font.FontSize = 13; r++;
                if (!string.IsNullOrEmpty(b.AltBaslik)) { ws.Cell(r, 1).Value = b.AltBaslik; r++; }
                ws.Cell(r, 1).Value = $"{b.Sirket} · {DateTime.Now:dd.MM.yyyy HH:mm}" + (string.IsNullOrEmpty(b.Hazirlayan) ? "" : " · " + b.Hazirlayan); r += 2;
                if (sayfa == 1 && b.Ozet.Count > 0) { foreach (var (e, v) in b.Ozet) { ws.Cell(r, 1).Value = e; ws.Cell(r, 1).Style.Font.Bold = true; ws.Cell(r, 2).Value = v ?? ""; r++; } r++; }
                if (!string.IsNullOrEmpty(bl.Baslik)) { ws.Cell(r, 1).Value = bl.Baslik; ws.Cell(r, 1).Style.Font.Bold = true; r++; }
                for (int i = 0; i < bl.Kolonlar.Count; i++) { var c = ws.Cell(r, i + 1); c.Value = bl.Kolonlar[i]; c.Style.Font.Bold = true; c.Style.Fill.BackgroundColor = XLColor.FromHtml("#0369a1"); c.Style.Font.FontColor = XLColor.White; }
                r++;
                foreach (var s in bl.Satirlar) { for (int i = 0; i < bl.Kolonlar.Count; i++) { var c = ws.Cell(r, i + 1); var v = i < s.Length ? s[i] ?? "" : ""; if (i < bl.Sag.Count && bl.Sag[i] && SayiCoz(v, out var d)) { c.Value = d; c.Style.NumberFormat.Format = "#,##0.00"; } else c.Value = v; } r++; }
                if (bl.Toplam != null) { for (int i = 0; i < bl.Kolonlar.Count; i++) { var c = ws.Cell(r, i + 1); var v = i < bl.Toplam.Length ? bl.Toplam[i] ?? "" : ""; if (i < bl.Sag.Count && bl.Sag[i] && SayiCoz(v, out var d)) { c.Value = d; c.Style.NumberFormat.Format = "#,##0.00"; } else c.Value = v; c.Style.Font.Bold = true; c.Style.Fill.BackgroundColor = XLColor.FromHtml("#f1f5f9"); } r++; }
                if (!string.IsNullOrEmpty(bl.Not)) { r++; ws.Cell(r, 1).Value = bl.Not; ws.Cell(r, 1).Style.Font.Italic = true; }
                ws.Columns().AdjustToContents(1, Math.Min(r, 200));
            }
            if (b.Bloklar.Count == 0) wb.Worksheets.Add("Rapor").Cell(1, 1).Value = b.Baslik;
            using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
        }
        /// <summary>"1.234,56 ₺" / "1.234,56 USD" / "12 gün" → sayı (yalnız para / sayı biçimli metinler).</summary>
        static bool SayiCoz(string s, out double d)
        {
            d = 0; if (string.IsNullOrWhiteSpace(s)) return false; var t = s.Replace("₺", "").Replace("TL", "").Replace("USD", "").Replace("EUR", "").Replace("GBP", "").Replace("%", "").Trim();
            if (t.EndsWith(" gün")) t = t.Replace(" gün", ""); if (t.Any(ch => char.IsLetter(ch))) return false;
            return double.TryParse(t, System.Globalization.NumberStyles.Any, CariKartOrnek.Tr, out d);
        }
    }
}
