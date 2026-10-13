using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>
    /// TCMB günlük kur serisi (ön yüz örneği): bugünkü baz kurlar NakitAkisOrnek.Kurlar; geçmiş günler deterministik (TL'nin zaman içinde değer kaybı + küçük dalgalanma),
    /// hafta sonu / tatilde bir önceki iş günü kuru; ileri tarihler bugünkü kurla "tahmini" döner (çek tahsil günü kuru vade gelince kesinleşir).
    /// Kasa Nakit Akışı dövizli ödemelerde kur kaynağı (U_KurKaynagi) TCMB türlerinden biriyse kur buradan otomatik gelir; anlaşmalı / fatura kurunda elle yazılır.
    /// </summary>
    public static class KurOrnek
    {
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public static readonly string[] Kaynaklar = { "TCMB Döviz Alış", "TCMB Döviz Satış", "TCMB Efektif Alış", "Anlaşmalı Kur", "Fatura Kuru", "Çek Tahsil Günü Kuru" };
        public static readonly string[] KurFarkiSecenekleri = { "Kesilmeyecek", "Kesilecek", "Belirsiz" };
        /// <summary>Kur TCMB serisinden okunur (elle yazılmaz).</summary>
        public static bool TcmbKaynagi(string kaynak) => kaynak == "TCMB Döviz Alış" || kaynak == "TCMB Döviz Satış" || kaynak == "TCMB Efektif Alış" || kaynak == "Çek Tahsil Günü Kuru";

        /// <summary>İş günü (hafta sonu → önceki cuma).</summary>
        public static DateTime IsGunu(DateTime t) { t = t.Date; while (t.DayOfWeek == DayOfWeek.Saturday || t.DayOfWeek == DayOfWeek.Sunday) t = t.AddDays(-1); return t; }

        /// <summary>O günün TCMB kurları (alış / satış / efektif alış). İleri tarih → bugünün kuru, tahmini = true.</summary>
        public static (decimal alis, decimal satis, decimal efektifAlis, bool tahmini, DateTime tarih) Tcmb(string pb, DateTime tarih)
        {
            pb = pb == "TL" || string.IsNullOrEmpty(pb) ? "TRY" : pb == "EURO" ? "EUR" : pb;
            if (pb == "TRY") return (1, 1, 1, false, tarih.Date);
            decimal baz = NakitAkisOrnek.Kurlar.TryGetValue(pb, out var b) ? b : 1m;
            var bugun = DateTime.Today; bool tahmini = tarih.Date > bugun; var gun = IsGunu(tahmini ? bugun : tarih);
            int d = (gun - bugun).Days;   // ≤ 0
            double x = d;
            // TL yılda ~%16 değer kaybeder (günde ~%0,045); üstüne küçük dalga
            decimal alis = baz * (decimal)(1 + 0.00045 * x) * (decimal)(1 + 0.004 * Math.Sin(x / 3.1) + 0.002 * Math.Cos(x / 7.3));
            alis = Math.Round(alis, 4);
            return (alis, Math.Round(alis * 1.0045m, 4), Math.Round(alis * 0.9975m, 4), tahmini, gun);
        }
        /// <summary>Kaynağa göre kur (TCMB türlerinde seriden; anlaşmalı / fatura kurunda baz kur).</summary>
        public static decimal Kur(string pb, DateTime? tarih, string kaynak)
        {
            var t = Tcmb(pb, tarih ?? DateTime.Today);
            return kaynak == "TCMB Döviz Satış" ? t.satis : kaynak == "TCMB Efektif Alış" ? t.efektifAlis : t.alis;
        }
        public static object Json(string pb, DateTime tarih)
        {
            var t = Tcmb(pb, tarih);
            return new { pb = pb == "TL" || string.IsNullOrEmpty(pb) ? "TRY" : pb, istenen = tarih.ToString("yyyy-MM-dd"), tarih = t.tarih.ToString("yyyy-MM-dd"), alis = t.alis, satis = t.satis, efektifAlis = t.efektifAlis, tahmini = t.tahmini, kaynak = "TCMB gösterge kuru (örnek seri)", aciklama = t.tahmini ? $"{tarih:dd.MM.yyyy} ileri tarih: bugünün ({t.tarih:dd.MM.yyyy}) kuru tahmini olarak alındı; gün gelince kesinleşir" : t.tarih != tarih.Date ? $"{tarih:dd.MM.yyyy} tatil / hafta sonu: {t.tarih:dd.MM.yyyy} iş günü kuru" : $"{t.tarih:dd.MM.yyyy} TCMB kuru" };
        }
        public static string Ozet(string pb, DateTime? tarih, string kaynak, decimal? kur) => pb == "TRY" || string.IsNullOrEmpty(pb) ? "" : $"{(kur ?? 1).ToString("N4", Tr)} · {(string.IsNullOrEmpty(kaynak) ? "kur" : kaynak)}{(tarih != null ? " · " + tarih.Value.ToString("dd.MM.yyyy") : "")}";
    }
}
