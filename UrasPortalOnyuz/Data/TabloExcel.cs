// ============================================================
// TABLO → EXCEL (ortak): ekrandaki tablonun GÖRÜNDÜĞÜ GİBİ .xlsx karşılığı. İstemci (wwwroot/js/tablo-excel.js) tabloyu hücre hücre okur — değer ya da sayı + Excel biçimi,
//   zemin ve yazı rengi, kalın / italik / çizili, punto, hizalama, kenarlık ("t" ince / "m" kalın + renk), birleşik hücre (cs / rs), zengin metin (R üstte / X altta hücreler,
//   rozetler, alt notlar) — ve JSON olarak POST /TabloExcel/Indir ucuna gönderir (Controllers/TabloExcelController.cs). Burada ClosedXML ile sayfalar kurulur:
//   başlık satırı, alt başlık (sayaç · tarih · hazırlayan), KPI kutuları (ekrandaki renk çubuğuyla), tablo (başlık satırları ve sola sabit sütunlar dondurulur),
//   açıklama notu; yatay A4, tek sayfa genişliğine sığdırılır, başlık satırları her sayfada yinelenir.
// Kullanan ekranlar: Personel Maliyet Raporu (tüm sekmelerin Excel düğmeleri), Personel Muhasebe İşlemleri.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;

namespace WebApplication3.Data
{
    /// <summary>İstemciden gelen belge: dosya adı + sayfalar.</summary>
    public class TxBelge { public string Dosya { get; set; } public List<TxSayfa> Sayfalar { get; set; } = new List<TxSayfa>(); }
    /// <summary>Bir sayfa: başlık, alt başlık, KPI kutuları, sütun genişlikleri (Excel karakter birimi), başlık satırı sayısı (dondurulur), sola dondurulacak sütun sayısı, satırlar, açıklama notu.</summary>
    public class TxSayfa { public string Ad { get; set; } public string Baslik { get; set; } public string AltBaslik { get; set; } public List<TxKpi> Kpi { get; set; } public string Not { get; set; } public List<double> Genislik { get; set; } public int BasSatir { get; set; } public int DondurSutun { get; set; } public List<TxSatir> Satirlar { get; set; } = new List<TxSatir>(); }
    public class TxKpi { public string L { get; set; } public string V { get; set; } public string S { get; set; } public string Renk { get; set; } }
    /// <summary>Satır: hücreler (H) ve isteğe bağlı yükseklik (Yk, punto; çok satırlı R/X hücreleri için).</summary>
    public class TxSatir { public double? Yk { get; set; } public List<TxHucre> H { get; set; } = new List<TxHucre>(); }
    /// <summary>Hücre: C sütun (0 tabanlı), Cs / Rs birleşik hücre, V metin ya da N sayı + F biçim, Bg zemin, Fg yazı, B / I / U kalın / italik / çizili, Hz l / c / r, Pt punto, Sar satır kaydır, Sol / Ust / Sag / Alt kenarlık, Z zengin metin parçaları.</summary>
    public class TxHucre { public int C { get; set; } public int Cs { get; set; } = 1; public int Rs { get; set; } = 1; public string V { get; set; } public double? N { get; set; } public string F { get; set; } public string Bg { get; set; } public string Fg { get; set; } public bool B { get; set; } public bool I { get; set; } public bool U { get; set; } public string Hz { get; set; } public double? Pt { get; set; } public bool Sar { get; set; } public string Sol { get; set; } public string Ust { get; set; } public string Sag { get; set; } public string Alt { get; set; } public List<TxParca> Z { get; set; } }
    /// <summary>Zengin metin parçası (Br = satır sonu).</summary>
    public class TxParca { public string T { get; set; } public string Fg { get; set; } public bool B { get; set; } public bool I { get; set; } public bool U { get; set; } public double? Pt { get; set; } public bool Br { get; set; } }

    public static class TabloExcel
    {
        static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        static readonly XLColor Cizgi = XLColor.FromHtml("#e2e8f0"), Gri = XLColor.FromHtml("#64748b"), Koyu = XLColor.FromHtml("#0f172a"), Zemin = XLColor.FromHtml("#f8fafc");

        public static byte[] Uret(TxBelge b, string hazirlayan)
        {
            using var wb = new XLWorkbook(); var adlar = new HashSet<string>(); int n = 0;
            foreach (var s in b.Sayfalar ?? new List<TxSayfa>()) { n++; var ws = wb.Worksheets.Add(SayfaAdi(s.Ad, n, adlar)); Sayfa(ws, s, hazirlayan); }
            if (wb.Worksheets.Count == 0) wb.Worksheets.Add("Rapor").Cell(1, 1).Value = b.Dosya ?? "Rapor";
            using var ms = new MemoryStream(); wb.SaveAs(ms); return ms.ToArray();
        }

        /// <summary>Excel sayfa adı: yasak karakterler atılır, 31 karaktere kısaltılır, yinelenen ada sıra eklenir.</summary>
        static string SayfaAdi(string ad, int n, HashSet<string> adlar)
        {
            var t = new string((ad ?? "").Where(ch => !"[]:*?/\\'".Contains(ch)).ToArray()).Trim(); if (t.Length == 0) t = "Sayfa" + n; if (t.Length > 31) t = t.Substring(0, 31);
            var son = t; int i = 1; while (!adlar.Add(son)) { var ek = " " + (++i); son = (t.Length + ek.Length > 31 ? t.Substring(0, 31 - ek.Length) : t) + ek; }
            return son;
        }
        static XLColor Renk(string h) { if (string.IsNullOrWhiteSpace(h)) return null; try { return XLColor.FromHtml(h.Trim()); } catch { return null; } }
        static double Punto(double? p) => p.HasValue && p.Value > 0 ? Math.Min(40, Math.Max(6, p.Value)) : 9;

        static void Sayfa(IXLWorksheet ws, TxSayfa s, string hazirlayan)
        {
            ws.Style.Font.FontName = "Calibri"; ws.Style.Font.FontSize = 9; ws.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            ws.ShowGridLines = false; ws.TabColor = XLColor.FromHtml("#0f766e");
            var satirlar = s.Satirlar ?? new List<TxSatir>();
            int sutun = Math.Max(1, Math.Max(s.Genislik?.Count ?? 0, satirlar.SelectMany(r => r.H ?? new List<TxHucre>()).Select(h => h.C + Math.Max(1, h.Cs)).DefaultIfEmpty(1).Max()));
            int birlesik = Math.Min(sutun, 40), r = 1; double toplamGenislik = 0;
            for (int i = 0; i < sutun; i++) { double g = s.Genislik != null && i < s.Genislik.Count ? Math.Min(80, Math.Max(3, s.Genislik[i])) : 12; ws.Column(i + 1).Width = g; if (i < birlesik) toplamGenislik += g; }
            // ---- başlık + alt başlık (ekrandaki şerit: sekme adı · dönem; sayaç · tarih · hazırlayan)
            if (!string.IsNullOrWhiteSpace(s.Baslik)) { var c = ws.Cell(r, 1); c.Value = s.Baslik; c.Style.Font.Bold = true; c.Style.Font.FontSize = 12; c.Style.Font.FontColor = XLColor.FromHtml("#134e4a"); if (birlesik > 1) ws.Range(r, 1, r, birlesik).Merge(); ws.Row(r).Height = 21; r++; }
            var alt = string.Join(" · ", new[] { s.AltBaslik, DateTime.Now.ToString("dd.MM.yyyy HH:mm", Tr), string.IsNullOrWhiteSpace(hazirlayan) ? null : "Hazırlayan: " + hazirlayan }.Where(x => !string.IsNullOrWhiteSpace(x)));
            { var c = ws.Cell(r, 1); c.Value = alt; c.Style.Font.FontSize = 8; c.Style.Font.FontColor = Gri; if (birlesik > 1) ws.Range(r, 1, r, birlesik).Merge(); ws.Row(r).Height = 12; r++; }
            // ---- KPI kutuları: her kutu ~22 karakter genişliğinde sütun grubuna yayılır (etiket / değer / alt bilgi; soldaki renk çubuğu ekrandaki gibi)
            if (s.Kpi != null && s.Kpi.Count > 0)
            {
                int c0 = 1;
                foreach (var k in s.Kpi)
                {
                    if (c0 > sutun) break;
                    int c1 = c0; double g = ws.Column(c1).Width; while (g < 22 && c1 < sutun) { c1++; g += ws.Column(c1).Width; }
                    var renk = Renk(k.Renk) ?? XLColor.FromHtml("#94a3b8");
                    void Kutu(int rr, string v, double pt, bool kalin, XLColor fg)
                    {
                        ws.Cell(rr, c0).Value = v ?? ""; var rg = ws.Range(rr, c0, rr, c1); if (c1 > c0) rg.Merge();
                        rg.Style.Font.FontSize = pt; rg.Style.Font.Bold = kalin; rg.Style.Font.FontColor = fg; rg.Style.Fill.BackgroundColor = Zemin; rg.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                        rg.Style.Border.LeftBorder = XLBorderStyleValues.Medium; rg.Style.Border.LeftBorderColor = renk; rg.Style.Border.RightBorder = XLBorderStyleValues.Thin; rg.Style.Border.RightBorderColor = Cizgi;
                    }
                    Kutu(r, (k.L ?? "").ToUpper(Tr), 7, true, Gri); Kutu(r + 1, k.V, 11, true, Koyu); Kutu(r + 2, k.S, 7, false, Gri);
                    var ust = ws.Range(r, c0, r, c1); ust.Style.Border.TopBorder = XLBorderStyleValues.Thin; ust.Style.Border.TopBorderColor = Cizgi;
                    var dip = ws.Range(r + 2, c0, r + 2, c1); dip.Style.Border.BottomBorder = XLBorderStyleValues.Thin; dip.Style.Border.BottomBorderColor = Cizgi;
                    c0 = c1 + 1;
                }
                ws.Row(r).Height = 10.5; ws.Row(r + 1).Height = 17; ws.Row(r + 2).Height = 10.5; r += 3;
            }
            // ---- tablo
            int tabloBas = r;
            foreach (var sat in satirlar)
            {
                foreach (var h in sat.H ?? new List<TxHucre>())
                {
                    int c = h.C + 1, cs = Math.Max(1, h.Cs), rs = Math.Max(1, h.Rs); if (c < 1 || c + cs - 1 > 16000) continue;
                    var cell = ws.Cell(r, c); IXLStyle st;
                    if (cs > 1 || rs > 1) { var rg = ws.Range(r, c, r + rs - 1, c + cs - 1); rg.Merge(); st = rg.Style; } else st = cell.Style;
                    if (h.Z != null && h.Z.Count > 0)
                    {
                        var rt = cell.CreateRichText();
                        foreach (var z in h.Z) { if (z.Br) { rt.AddNewLine(); continue; } var p = rt.AddText(z.T ?? ""); var fg = Renk(z.Fg); if (fg != null) p.SetFontColor(fg); if (z.B) p.SetBold(); if (z.I) p.SetItalic(); if (z.U) p.SetStrikethrough(); p.SetFontSize(Punto(z.Pt ?? h.Pt)); }
                        st.Alignment.WrapText = true;
                    }
                    else if (h.N.HasValue) { cell.Value = h.N.Value; if (!string.IsNullOrWhiteSpace(h.F) && h.F.Length < 80) st.NumberFormat.Format = h.F; }
                    else cell.Value = h.V ?? "";
                    var bg = Renk(h.Bg); if (bg != null) st.Fill.BackgroundColor = bg;
                    var yazi = Renk(h.Fg); if (yazi != null) st.Font.FontColor = yazi;
                    if (h.B) st.Font.Bold = true; if (h.I) st.Font.Italic = true; if (h.U) st.Font.Strikethrough = true; if (h.Pt.HasValue) st.Font.FontSize = Punto(h.Pt);
                    st.Alignment.Horizontal = h.Hz == "r" ? XLAlignmentHorizontalValues.Right : h.Hz == "c" ? XLAlignmentHorizontalValues.Center : XLAlignmentHorizontalValues.Left;
                    if (h.Sar) st.Alignment.WrapText = true;
                    Kenar(st, h.Sol, 0); Kenar(st, h.Ust, 1); Kenar(st, h.Sag, 2); Kenar(st, h.Alt, 3);
                }
                if (sat.Yk.HasValue && sat.Yk.Value > 0) ws.Row(r).Height = Math.Min(400, sat.Yk.Value);
                r++;
            }
            // ---- açıklama notu (ekrandaki tablo altı açıklama), birleşik ve kaydırmalı
            if (!string.IsNullOrWhiteSpace(s.Not))
            {
                r++; var c = ws.Cell(r, 1); c.Value = s.Not; c.Style.Font.FontSize = 8; c.Style.Font.FontColor = XLColor.FromHtml("#475569"); c.Style.Alignment.WrapText = true; c.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                if (birlesik > 1) ws.Range(r, 1, r, birlesik).Merge();
                int satirSayisi = (int)Math.Ceiling(s.Not.Length / Math.Max(20.0, toplamGenislik * 1.15)) + 1; ws.Row(r).Height = Math.Min(400, satirSayisi * 11 + 4);
            }
            // ---- dondurma (başlık satırları + sola sabit sütunlar), yazdırma
            var son = ws.LastRowUsed(); int donSatir = tabloBas + Math.Max(0, s.BasSatir) - 1;
            if (s.BasSatir > 0 && son != null && donSatir < son.RowNumber()) ws.SheetView.Freeze(donSatir, Math.Max(0, Math.Min(s.DondurSutun, 10)));
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape; ws.PageSetup.PaperSize = XLPaperSize.A4Paper; ws.PageSetup.FitToPages(1, 0);
            ws.PageSetup.Margins.Left = 0.3; ws.PageSetup.Margins.Right = 0.3; ws.PageSetup.Margins.Top = 0.4; ws.PageSetup.Margins.Bottom = 0.4;
            if (s.BasSatir > 0 && satirlar.Count >= s.BasSatir) ws.PageSetup.SetRowsToRepeatAtTop(tabloBas, tabloBas + s.BasSatir - 1);
        }

        /// <summary>Kenarlık kodu: ilk karakter t (ince) / m (kalın), kalanı renk (#rrggbb). yon: 0 sol, 1 üst, 2 sağ, 3 alt.</summary>
        static void Kenar(IXLStyle st, string k, int yon)
        {
            if (string.IsNullOrEmpty(k) || k.Length < 2) return;
            var stil = k[0] == 'm' ? XLBorderStyleValues.Medium : XLBorderStyleValues.Thin; var renk = Renk(k.Substring(1)) ?? Cizgi;
            switch (yon)
            {
                case 0: st.Border.LeftBorder = stil; st.Border.LeftBorderColor = renk; break;
                case 1: st.Border.TopBorder = stil; st.Border.TopBorderColor = renk; break;
                case 2: st.Border.RightBorder = stil; st.Border.RightBorderColor = renk; break;
                default: st.Border.BottomBorder = stil; st.Border.BottomBorderColor = renk; break;
            }
        }
    }
}
