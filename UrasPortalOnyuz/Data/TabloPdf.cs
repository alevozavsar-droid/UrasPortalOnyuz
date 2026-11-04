// ============================================================
// TABLO → PDF (ortak): TabloExcel ile aynı belge (TxBelge) PDF'e çevrilir (QuestPDF, Community lisansı). Yatay A4; her sayfada sekme başlığı ve sayfa no.
//   KPI kutuları ve açıklama notu sayfa sayfa ilk grubun başında. Geniş tablolar sütun gruplarına bölünür: sola sabit sütunlar her grupta yinelenir,
//   başlık satırları her sayfada yinelenir. Hücre zemini, yazı rengi, kalın / italik / çizili, hizalama, kenarlık, zengin metin (R üstte / X altta) ve
//   sayı biçimi (₺ / %) korunur. İstemci: wwwroot/js/tablo-excel.js (TabloExcel.pdf) → POST /TabloExcel/Pdf (Controllers/TabloExcelController.cs).
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace WebApplication3.Data
{
    public static class TabloPdf
    {
        static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        const string Cizgi = "#e2e8f0", Gri = "#64748b", Koyu = "#0f172a", Baslik = "#134e4a", Alt = "#475569";
        const double KapasiteBirim = 150;   // A4 yatay kullanılabilir genişlik ≈ 800 pt; Excel sütun birimi ≈ 5,3 pt → bir sayfa grubuna ≈ 150 birim
        static TabloPdf() { QuestPDF.Settings.License = LicenseType.Community; QuestPDF.Settings.EnableDebugging = false; }

        public static byte[] Uret(TxBelge b, string hazirlayan)
        {
            var sayfalar = (b.Sayfalar ?? new List<TxSayfa>()).ToList();
            return Document.Create(container =>
            {
                if (sayfalar.Count == 0) { container.Page(p => { p.Size(PageSizes.A4.Landscape()); p.Margin(18); p.Content().Text(b.Dosya ?? "Rapor"); }); return; }
                foreach (var s in sayfalar) Sayfa(container, s, b.Dosya, hazirlayan);
            }).GeneratePdf();
        }

        /// <summary>Bir sayfa tanımı: sütun grupları kadar PDF sayfa dizisi (her grup kendi başlığıyla, tablo sayfalara akar).</summary>
        static void Sayfa(IDocumentContainer container, TxSayfa s, string dosya, string hazirlayan)
        {
            var satirlar = s.Satirlar ?? new List<TxSatir>();
            int n = Math.Max(1, Math.Max(s.Genislik?.Count ?? 0, satirlar.SelectMany(r => r.H ?? new List<TxHucre>()).Select(h => h.C + Math.Max(1, h.Cs)).DefaultIfEmpty(0).Max()));
            var birim = Enumerable.Range(0, n).Select(i => s.Genislik != null && i < s.Genislik.Count ? Math.Min(80, Math.Max(3, s.Genislik[i])) : 12.0).ToList();
            int sabit = Math.Min(Math.Min(Math.Max(0, s.DondurSutun), 3), n - 1);
            var gruplar = Gruplar(n, birim, sabit);
            string bas = string.IsNullOrWhiteSpace(s.Baslik) ? (dosya ?? "Rapor") : s.Baslik;
            string alt = string.Join(" · ", new[] { s.AltBaslik, DateTime.Now.ToString("dd.MM.yyyy HH:mm", Tr), string.IsNullOrWhiteSpace(hazirlayan) ? null : "Hazırlayan: " + hazirlayan }.Where(x => !string.IsNullOrWhiteSpace(x)));
            int basSatir = Math.Min(Math.Max(0, s.BasSatir), satirlar.Count);
            var basliklar = satirlar.Take(basSatir).ToList(); var govde = satirlar.Skip(basSatir).ToList();

            for (int g = 0; g < gruplar.Count; g++)
            {
                var grup = gruplar[g]; int gi = g; bool ilk = g == 0;
                var konum = new Dictionary<int, int>(); for (int p = 0; p < grup.Count; p++) konum[grup[p]] = p;
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape()); page.Margin(18); page.DefaultTextStyle(x => x.FontSize(6.8f).FontColor(Koyu));
                    page.Header().Column(c =>
                    {
                        c.Item().Text(gruplar.Count > 1 ? bas + "   ·   sütun grubu " + (gi + 1) + " / " + gruplar.Count : bas).FontSize(10).Bold().FontColor(Baslik);
                        if (!string.IsNullOrWhiteSpace(alt)) c.Item().PaddingBottom(4).Text(alt).FontSize(6.5f).FontColor(Alt);
                    });
                    page.Content().Column(col =>
                    {
                        if (ilk && s.Kpi != null && s.Kpi.Count > 0) col.Item().PaddingBottom(6).Row(r => { foreach (var k in s.Kpi) Kutu(r, k); });
                        if (satirlar.Count == 0) col.Item().Text("Veri yok.").FontColor(Gri);
                        else col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(cd => { foreach (var i in grup) cd.RelativeColumn((float)birim[i]); });
                            if (basliklar.Count > 0) t.Header(h => { foreach (var sat in basliklar) Satir(sp => h.Cell().ColumnSpan(sp), sat, grup, konum, true); });
                            foreach (var sat in govde) Satir(sp => t.Cell().ColumnSpan(sp), sat, grup, konum, false);
                        });
                        if (ilk && !string.IsNullOrWhiteSpace(s.Not)) col.Item().PaddingTop(4).Text(s.Not).FontSize(6.3f).FontColor(Alt);
                    });
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Uras Portal · sayfa ").FontSize(6.3f).FontColor("#94a3b8"); x.CurrentPageNumber().FontSize(6.3f).FontColor("#94a3b8");
                        x.Span(" / ").FontSize(6.3f).FontColor("#94a3b8"); x.TotalPages().FontSize(6.3f).FontColor("#94a3b8");
                    });
                });
            }
        }

        /// <summary>Sütunları sayfa genişliğine göre gruplar; sola sabit sütunlar her grupta yinelenir.</summary>
        static List<List<int>> Gruplar(int n, List<double> birim, int sabit)
        {
            var sabitListe = Enumerable.Range(0, sabit).ToList(); double sabitBirim = sabitListe.Sum(i => birim[i]);
            var gruplar = new List<List<int>>(); var cur = new List<int>(sabitListe); double w = sabitBirim;
            for (int i = sabit; i < n; i++)
            {
                if (cur.Count > sabit && w + birim[i] > KapasiteBirim) { gruplar.Add(cur); cur = new List<int>(sabitListe); w = sabitBirim; }
                cur.Add(i); w += birim[i];
            }
            gruplar.Add(cur);
            return gruplar;
        }

        /// <summary>Bir satırı kurar: hücreler grup içindeki konumuna yerleşir (birleşik hücre ColumnSpan ile); boş konumlar boş hücre olur.</summary>
        static void Satir(Func<uint, IContainer> hucre, TxSatir sat, List<int> grup, Dictionary<int, int> konum, bool baslik)
        {
            var yer = new (TxHucre h, int span)[grup.Count];
            foreach (var h in sat.H ?? new List<TxHucre>())
            {
                var pozlar = Enumerable.Range(h.C, Math.Max(1, h.Cs)).Where(konum.ContainsKey).Select(i => konum[i]).OrderBy(p => p).ToList();
                if (pozlar.Count == 0 || yer[pozlar[0]].h != null) continue;
                yer[pozlar[0]] = (h, pozlar.Count);
            }
            for (int p = 0; p < grup.Count;)
            {
                int sp = Math.Min(yer[p].h != null ? Math.Max(1, yer[p].span) : 1, grup.Count - p);
                var kap = hucre((uint)sp);
                if (yer[p].h != null) Icerik(kap, yer[p].h, baslik); else kap = Kenar(kap, null, null, null, null);
                p += sp;
            }
        }

        /// <summary>Hücre içeriği ve biçimi: zemin, hizalama, kenarlık, yazı (zengin metin ya da sayı / metin).</summary>
        static void Icerik(IContainer kap, TxHucre h, bool baslik)
        {
            var c = kap.PaddingHorizontal(3).PaddingVertical(1.5f).AlignMiddle();
            var bg = Renk(h.Bg); if (bg != null) c = c.Background(bg);
            if (h.Hz == "r") c = c.AlignRight(); else if (h.Hz == "c") c = c.AlignCenter();
            c = Kenar(c, h.Sol, h.Ust, h.Sag, h.Alt);
            float pt = Yazi(h.Pt, baslik);
            if (h.Z != null && h.Z.Count > 0)
            {
                c.Text(t =>
                {
                    foreach (var z in h.Z)
                    {
                        if (z.Br) { t.EmptyLine(); continue; }
                        var sp = t.Span(z.T ?? ""); sp.FontSize(Yazi(z.Pt ?? h.Pt, baslik));
                        var fg = Renk(z.Fg); if (fg != null) sp.FontColor(fg);
                        if (z.B) sp.Bold(); if (z.I) sp.Italic(); if (z.U) sp.Strikethrough();
                    }
                });
                return;
            }
            var metin = h.N.HasValue ? Bicim(h.N.Value, h.F) : (h.V ?? "");
            if (metin.Length == 0) return;
            var fgRenk = Renk(h.Fg);
            c.Text(t =>
            {
                var sp = t.Span(metin); sp.FontSize(pt);
                if (fgRenk != null) sp.FontColor(fgRenk);
                if (h.B) sp.Bold(); if (h.I) sp.Italic(); if (h.U) sp.Strikethrough();
            });
        }

        /// <summary>Kenarlıklar: ekrandaki ince çizgiler ("t#rrggbb" ince, "m#rrggbb" kalın); alt kenar her hücrede açık gri ayırıcı.</summary>
        static IContainer Kenar(IContainer c, string sol, string ust, string sag, string alt)
        {
            var s = Cizgi1(sol); var u = Cizgi1(ust); var r = Cizgi1(sag); var a = Cizgi1(alt);
            c = c.BorderBottom(a?.w ?? 0.4f);
            if (s.HasValue) c = c.BorderLeft(s.Value.w);
            if (u.HasValue) c = c.BorderTop(u.Value.w);
            if (r.HasValue) c = c.BorderRight(r.Value.w);
            return c.BorderColor((a ?? r ?? s ?? u)?.renk ?? Cizgi);
        }

        static (float w, string renk)? Cizgi1(string k)
        {
            if (string.IsNullOrEmpty(k) || k.Length < 2) return null;
            var renk = Renk(k.Substring(1)); if (renk == null) return null;
            return (k[0] == 'm' ? 1.2f : 0.4f, renk);
        }

        /// <summary>KPI kutusu: renk çubuğu, etiket, değer, alt bilgi.</summary>
        static void Kutu(RowDescriptor r, TxKpi k)
        {
            var renk = Renk(k.Renk) ?? "#94a3b8";
            r.RelativeItem().Background("#f8fafc").BorderLeft(2).BorderColor(renk).Padding(4).Column(c =>
            {
                c.Item().Text((k.L ?? "").ToUpper(Tr)).FontSize(5.5f).Bold().FontColor(Gri);
                c.Item().Text(k.V ?? "").FontSize(9).Bold();
                if (!string.IsNullOrWhiteSpace(k.S)) c.Item().Text(k.S).FontSize(6).FontColor(Gri);
            });
        }

        /// <summary>"1.234,56 ₺" / "% 12,0" / "7,5%" biçimleri (Excel sayı biçimiyle aynı kurallar).</summary>
        static string Bicim(double n, string f)
        {
            if (string.IsNullOrEmpty(f)) return n.ToString("#,##0.##", Tr);
            int dec = 0; int nokta = f.IndexOf('.');
            if (nokta >= 0) for (int i = nokta + 1; i < f.Length && f[i] == '0'; i++) dec++;
            var s = n.ToString("N" + dec, Tr);
            if (f.StartsWith("\"% \"")) s = "% " + s;
            if (f.EndsWith("\"%\"")) s += "%";
            if (f.Contains("₺")) s += " ₺";
            return s;
        }

        static float Yazi(double? pt, bool baslik) => pt.HasValue && pt.Value > 0 ? (float)Math.Min(7.5, Math.Max(5.5, pt.Value * 0.85)) : (baslik ? 6.4f : 6.8f);

        /// <summary>Renk kodu (#rrggbb) → geçerliyse aynen, değilse null (QuestPDF geçersiz rengi reddeder).</summary>
        static string Renk(string h) => !string.IsNullOrWhiteSpace(h) && Regex.IsMatch(h.Trim(), "^#[0-9a-fA-F]{6}$") ? h.Trim() : null;
    }
}
