// ============================================================
// URAS PORTEKİZ RAPORU (20 Kas 2026, kullanıcı: "bu raporu Türkçeye çevirerek URAS PORTEKİZ raporu hazırla")
//   Kaynak: Uras Portekiz'in gönderdiği Excel (REPORT_PORTUGAL_gg.aa.yyyy.xls) — App_Data/uras-portekiz klasöründeki EN YENİ dosya okunur.
//   Sayfalar: SALES (fatura satırları) · CASH EUR (kasa) · BANK DETAILED MURAT PRIVATE (Murat Bey özel hesap) · BANK SAVINGS (tasarruf hesabı) ·
//             BANK DETAILED COMPANY (şirket banka hesabı) · PAYMENTS FORECAST NEXT WEEKS (ödeme öngörüsü).
//   Veri GERÇEKTİR, dosyadan olduğu gibi gelir; yalnız başlıklar ve açıklamalar Türkçeye çevrilir (UrasPortekizCeviri), asıl İngilizce metin de saklanır.
//   Tutarlar EUR. Kasa / özel hesap / tasarruf sayfalarında bakiye sütunu yoktur → yürüyen bakiye burada hesaplanır (giriş − çıkış birikimli).
//   Yeni rapor geldiğinde ekrandaki "Yeni Rapor Yükle" ile dosya klasöre eklenir; en yeni dosya rapor olur (eskiler silinmez).
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NPOI.SS.UserModel;

namespace WebApplication3.Data
{
    public static class UrasPortekizOrnek
    {
        static readonly object Kilit = new object();
        static object _paket; static string _yol; static DateTime _zaman;

        public static string Klasor(string kok) => Path.Combine(kok, "App_Data", "uras-portekiz");
        /// <summary>Klasördeki en yeni rapor dosyası (yazma zamanına göre); yoksa null.</summary>
        public static string DosyaYolu(string kok)
        {
            var k = Klasor(kok); if (!Directory.Exists(k)) return null;
            return Directory.GetFiles(k).Where(f => { var u = Path.GetExtension(f).ToLowerInvariant(); return u == ".xls" || u == ".xlsx"; }).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        }

        /// <summary>Ekran paketi (önbellekli: dosya değişmedikçe yeniden okunmaz).</summary>
        public static object Paket(string kok)
        {
            lock (Kilit)
            {
                string yol = DosyaYolu(kok);
                if (yol == null) return new { success = false, message = "Rapor dosyası yok. \"Yeni Rapor Yükle\" ile Uras Portekiz Excel dosyasını yükleyin." };
                var z = File.GetLastWriteTimeUtc(yol);
                if (_paket != null && _yol == yol && _zaman == z) return _paket;
                try { _paket = Oku(yol); _yol = yol; _zaman = z; return _paket; }
                catch (Exception ex) { return new { success = false, message = "Rapor dosyası okunamadı (" + Path.GetFileName(yol) + "): " + ex.Message }; }
            }
        }
        public static void OnbellekTemizle() { lock (Kilit) { _paket = null; _yol = null; } }

        /// <summary>Yüklenen dosya geçerli bir Uras Portekiz raporu mu (SALES ve banka sayfaları var mı)? Hata metni döner (null = geçerli).</summary>
        public static string Dogrula(string yol)
        {
            try
            {
                using var fs = File.OpenRead(yol); using var wb = WorkbookFactory.Create(fs);
                var adlar = Enumerable.Range(0, wb.NumberOfSheets).Select(i => wb.GetSheetName(i).ToUpperInvariant()).ToList();
                if (!adlar.Any(a => a.Contains("SALES"))) return "Dosyada SALES sayfası yok; bu bir Uras Portekiz raporu değil.";
                if (!adlar.Any(a => a.Contains("COMPANY"))) return "Dosyada BANK DETAILED COMPANY sayfası yok; bu bir Uras Portekiz raporu değil.";
                return null;
            }
            catch (Exception ex) { return "Excel dosyası açılamadı: " + ex.Message; }
        }

        // ---- hücre okuma
        static string Metin(ICell c)
        {
            if (c == null) return "";
            try
            {
                var tip = c.CellType == CellType.Formula ? c.CachedFormulaResultType : c.CellType;
                if (tip == CellType.String) return (c.StringCellValue ?? "").Trim();
                if (tip == CellType.Numeric) return DateUtil.IsCellDateFormatted(c) ? (c.DateCellValue?.ToString("yyyy-MM-dd") ?? "") : c.NumericCellValue.ToString("0.############", CultureInfo.InvariantCulture);
                if (tip == CellType.Boolean) return c.BooleanCellValue ? "1" : "0";
            }
            catch { /* bozuk hücre: boş say */ }
            return "";
        }
        static decimal Sayi(ICell c)
        {
            if (c == null) return 0m;
            try
            {
                var tip = c.CellType == CellType.Formula ? c.CachedFormulaResultType : c.CellType;
                if (tip == CellType.Numeric) { double d = c.NumericCellValue; return double.IsNaN(d) || double.IsInfinity(d) ? 0m : Math.Round((decimal)d, 6); }
                if (tip == CellType.String && decimal.TryParse((c.StringCellValue ?? "").Trim().Replace(" ", ""), NumberStyles.Any, CultureInfo.InvariantCulture, out var v)) return v;
            }
            catch { /* yoksay */ }
            return 0m;
        }
        static DateTime? Tarih(ICell c)
        {
            if (c == null) return null;
            try
            {
                var tip = c.CellType == CellType.Formula ? c.CachedFormulaResultType : c.CellType;
                if (tip == CellType.Numeric && DateUtil.IsCellDateFormatted(c)) return c.DateCellValue;
                if (tip == CellType.String && DateTime.TryParse(c.StringCellValue, new CultureInfo("pt-PT"), DateTimeStyles.None, out var t)) return t;
            }
            catch { /* yoksay */ }
            return null;
        }
        static string Iso(DateTime? t) => t?.ToString("yyyy-MM-dd") ?? "";
        static decimal R2(decimal v) => Math.Round(v, 2);
        static ISheet Sayfa(IWorkbook wb, params string[] anahtar)
        {
            for (int i = 0; i < wb.NumberOfSheets; i++) { string ad = wb.GetSheetName(i).ToUpperInvariant(); if (anahtar.All(a => ad.Contains(a))) return wb.GetSheetAt(i); }
            return null;
        }

        static object Oku(string yol)
        {
            using var fs = File.OpenRead(yol); using var wb = WorkbookFactory.Create(fs);
            // ---- SATIŞLAR: A belge no · B tarih · C müşteri · D ürün · E ambalaj (kg) · F adet · G net kg · H kg fiyatı · I tutar · J KDV oranı · K KDV · L KDV dahil · M vade
            //      çok satırlı faturada belge no / tarih / müşteri yalnız ilk satırdadır (birleşik hücre) → sonraki satırlar üsttekini alır
            var musteriler = new List<string>(); var urunler = new List<string>(); var vadeler = new List<string>();
            var mIdx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); var uIdx = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase); var vIdx = new Dictionary<string, int>();
            int Idx(Dictionary<string, int> d, List<string> l, string ad) { if (!d.TryGetValue(ad, out int i)) { i = l.Count; l.Add(ad); d[ad] = i; } return i; }
            var satis = new List<object[]>();
            var ss = Sayfa(wb, "SALES");
            if (ss != null)
            {
                string belge = "", tarih = "", musteri = "", vade = "";
                for (int r = 2; r <= ss.LastRowNum; r++)
                {
                    var row = ss.GetRow(r); if (row == null) continue;
                    string b = Metin(row.GetCell(0)), m = Metin(row.GetCell(2)), urun = Metin(row.GetCell(3)); var t = Tarih(row.GetCell(1)); decimal tutar = Sayi(row.GetCell(8)), toplam = Sayi(row.GetCell(11));
                    if (b != "") { belge = b; tarih = Iso(t); musteri = Regex.Replace(m, @"\s+", " "); vade = UrasPortekizCeviri.Vade(Metin(row.GetCell(12))); }
                    bool iptal = Regex.IsMatch(m, @"ANNUL|CANCEL", RegexOptions.IgnoreCase);
                    if (urun == "" && tutar == 0m && toplam == 0m && !iptal) continue;   // sondaki boş (formüllü) satırlar
                    if (iptal) { satis.Add(new object[] { belge, tarih, Idx(mIdx, musteriler, "Fatura İptal"), Idx(uIdx, urunler, "—"), 0m, 0m, 0m, 0m, 0m, Sayi(row.GetCell(9)), 0m, 0m, Idx(vIdx, vadeler, ""), 1 }); continue; }
                    satis.Add(new object[] { belge, tarih, Idx(mIdx, musteriler, musteri == "" ? "—" : musteri), Idx(uIdx, urunler, urun == "" ? "—" : Regex.Replace(urun, @"\s+", " ")),
                        Sayi(row.GetCell(4)), Sayi(row.GetCell(5)), R2(Sayi(row.GetCell(6))), Math.Round(Sayi(row.GetCell(7)), 4), R2(tutar), Sayi(row.GetCell(9)), R2(Sayi(row.GetCell(10))), R2(toplam), Idx(vIdx, vadeler, vade), 0 });
                }
            }
            // ---- hesap hareketleri: tarih · (belge no) · açıklama · giriş · çıkış · (bakiye)
            List<object[]> Hareket(ISheet s, int kolAciklama, int kolGiris, int kolBelge, int kolBakiye)
            {
                var l = new List<object[]>(); if (s == null) return l; decimal yuruyen = 0m;
                for (int r = 0; r <= s.LastRowNum; r++)
                {
                    var row = s.GetRow(r); if (row == null) continue; var t = Tarih(row.GetCell(0)); if (t == null) continue;
                    string asil = Regex.Replace(Metin(row.GetCell(kolAciklama)), @"\s+", " "); decimal g = R2(Sayi(row.GetCell(kolGiris))), c = R2(Sayi(row.GetCell(kolGiris + 1)));
                    if (asil == "" && g == 0m && c == 0m) continue;
                    yuruyen += g - c; decimal bakiye = kolBakiye >= 0 && row.GetCell(kolBakiye) != null && Metin(row.GetCell(kolBakiye)) != "" ? R2(Sayi(row.GetCell(kolBakiye))) : R2(yuruyen);
                    l.Add(new object[] { Iso(t), kolBelge >= 0 ? Metin(row.GetCell(kolBelge)).Replace("N/A", "—") : "", UrasPortekizCeviri.Cevir(asil), asil, g, c, bakiye, UrasPortekizCeviri.Tur(asil, g > 0m && c == 0m), UrasPortekizCeviri.Kalem(asil, g > 0m && c == 0m) });   // [8] kalem: Aylık Giderler sekmesinin ince ayrımı (maaş, otoyol, yakıt …)
                }
                return l;
            }
            var sirket = Hareket(Sayfa(wb, "COMPANY"), 2, 3, 1, 5);
            var ozel = Hareket(Sayfa(wb, "MURAT"), 2, 3, -1, -1);
            var tasarruf = Hareket(Sayfa(wb, "SAVINGS"), 2, 3, -1, -1);
            var kasa = Hareket(Sayfa(wb, "CASH"), 2, 3, -1, -1);
            // ---- ödeme öngörüsü: tarih · açıklama · giriş · çıkış · bakiye (dosyadaki yürüyen bakiye)
            var ongoru = new List<object[]>(); var os = Sayfa(wb, "FORECAST");
            if (os != null)
                for (int r = 0; r <= os.LastRowNum; r++)
                {
                    var row = os.GetRow(r); if (row == null) continue; var t = Tarih(row.GetCell(0)); if (t == null) continue;
                    string asil = Regex.Replace(Metin(row.GetCell(1)), @"\s+", " "); if (asil == "") continue;
                    decimal g = R2(Sayi(row.GetCell(2))), c = R2(Sayi(row.GetCell(3))); string bk = Metin(row.GetCell(4));
                    ongoru.Add(new object[] { Iso(t), UrasPortekizCeviri.Cevir(asil), asil, g, c, bk == "" ? (object)null : R2(Sayi(row.GetCell(4))), UrasPortekizCeviri.Tur(asil, g > 0m && c == 0m), Regex.IsMatch(asil, "ACCOUNT BALANCE", RegexOptions.IgnoreCase) ? 1 : 0 });
                }
            decimal Son(List<object[]> l) => l.Count == 0 ? 0m : (decimal)l[l.Count - 1][6];
            string dosya = Path.GetFileName(yol); var dm = Regex.Match(dosya, @"(\d{1,2})[._-](\d{1,2})[._-](\d{4})");
            string raporTarihi = dm.Success && DateTime.TryParseExact($"{dm.Groups[1].Value.PadLeft(2, '0')}.{dm.Groups[2].Value.PadLeft(2, '0')}.{dm.Groups[3].Value}", "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var rt) ? rt.ToString("yyyy-MM-dd") : "";
            // son hareket: hesap hareketleri ile satışların en yeni tarihi; dosyadaki yazım hatalı ileri tarihler (ör. 3019) sayılmaz (dosyanın yazıldığı günden sonrası olamaz)
            string ustSinir = File.GetLastWriteTime(yol).AddDays(1).ToString("yyyy-MM-dd");
            string sonHareket = new[] { sirket, ozel, tasarruf, kasa }.SelectMany(x => x).Select(x => (string)x[0]).Concat(satis.Select(x => (string)x[1])).Where(x => x != "" && string.CompareOrdinal(x, ustSinir) <= 0).DefaultIfEmpty("").Max();
            return new
            {
                success = true, dosya = Regex.Replace(dosya, @"^[0-9a-f]{8}-", ""), raporTarihi, sonHareket, yuklenme = File.GetLastWriteTime(yol).ToString("dd.MM.yyyy HH:mm"),
                musteriler, urunler, vadeler, satis, sirket, ozel, tasarruf, kasa, ongoru,
                bakiye = new { sirket = Son(sirket), ozel = Son(ozel), tasarruf = Son(tasarruf), kasa = Son(kasa) }
            };
        }
    }
}
