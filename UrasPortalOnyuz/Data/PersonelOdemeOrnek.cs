// ============================================================
// GERÇEKLEŞEN ÖDEMELER (9 Kas 2026, kullanıcı: "Ödeme Planı'nda aynı rakam mı ödendi bankadan veya elden, ne kadar ödendiğini anlayayım; farklı rakam ödendiyse manuel girebileyim")
//   Konsolide İK Paneli › Ödeme Planı: her satırın (kişi / dönem) planlanan banka ve elden tutarının karşısına FİİLEN ÖDENEN tutar, ödeme tarihi ve not girilir.
//   Fark = ödenen − planlanan (banka ve elden ayrı); durum: Girilmedi · Tam Ödendi · Eksik Ödendi · Fazla Ödendi (ekranda hesaplanır).
//   Anahtar: dönem (yyyy-MM) + satır anahtarı "Kaynak:Sıra" (Bordro:33 · Özel:5 · Üretim:7 — JS cikisAnahtar ile aynı). Kayıtlar bellekte; kalıcı günlük tipi "odeme" (PersonelMaliyetKalici).
//   Planlamayı (Ödenecek / Ödenmeyecek seçimi, tarayıcıda) ve muhasebe kayıtlarını değiştirmez.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class PoOdeme
    {
        public string Donem { get; set; } public string Anahtar { get; set; }
        /// <summary>null = girilmedi; 0 = hiç ödenmedi.</summary>
        public decimal? OdenenBanka { get; set; } public decimal? OdenenElden { get; set; }
        public string BankaTarih { get; set; } = ""; public string EldenTarih { get; set; } = "";   // yyyy-MM-dd ya da boş
        public string Not { get; set; } = ""; public string Kullanici { get; set; } = ""; public DateTime Zaman { get; set; }
    }
    /// <summary>Ekrandan gelen giriş (ve kalıcı günlük satırı): Sil = true kaydı kaldırır.</summary>
    public class PoIstek { public string Donem { get; set; } public string Anahtar { get; set; } public decimal? OdenenBanka { get; set; } public decimal? OdenenElden { get; set; } public string BankaTarih { get; set; } public string EldenTarih { get; set; } public string Not { get; set; } public bool Sil { get; set; } }

    public static class PersonelOdemeOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly List<PoOdeme> _liste = new List<PoOdeme>();

        static string Tarih(string s, string ad, out string hata)
        {
            hata = null; s = (s ?? "").Trim(); if (s == "") return "";
            if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) || DateTime.TryParse(s, CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out t)) return t.ToString("yyyy-MM-dd");
            hata = ad + " tarihi geçersiz (yyyy-aa-gg)."; return "";
        }
        /// <summary>Gerçekleşen ödeme kaydet / güncelle / sil. Hata metni döner (null = başarılı).</summary>
        public static string Kaydet(PoIstek d, string kisi, out PoOdeme kayit)
        {
            kayit = null; if (d == null) return "Veri yok.";
            if (string.IsNullOrWhiteSpace(d.Donem) || d.Donem.Length != 7) return "Dönem AAAA-AA biçiminde olmalıdır.";
            if (string.IsNullOrWhiteSpace(d.Anahtar) || !d.Anahtar.Contains(":")) return "Satır anahtarı geçersiz.";
            string anahtar = d.Anahtar.Trim();
            lock (Kilit)
            {
                var k = _liste.FirstOrDefault(x => x.Donem == d.Donem && x.Anahtar == anahtar);
                if (d.Sil) { if (k == null) return "Silinecek ödeme kaydı yok."; _liste.Remove(k); kayit = k; return null; }
                if (d.OdenenBanka == null && d.OdenenElden == null) return "Bankadan ya da elden ödenen tutarı girin.";
                if ((d.OdenenBanka ?? 0) < 0 || (d.OdenenElden ?? 0) < 0) return "Ödenen tutar negatif olamaz.";
                var bt = Tarih(d.BankaTarih, "Banka ödeme", out var h1); if (h1 != null) return h1;
                var et = Tarih(d.EldenTarih, "Elden ödeme", out var h2); if (h2 != null) return h2;
                if (k == null) { k = new PoOdeme { Donem = d.Donem, Anahtar = anahtar }; _liste.Add(k); }
                k.OdenenBanka = d.OdenenBanka.HasValue ? BordroParametre.Yuvarla(d.OdenenBanka.Value) : (decimal?)null; k.OdenenElden = d.OdenenElden.HasValue ? BordroParametre.Yuvarla(d.OdenenElden.Value) : (decimal?)null;
                k.BankaTarih = bt; k.EldenTarih = et; k.Not = (d.Not ?? "").Trim(); k.Kullanici = kisi ?? ""; k.Zaman = DateTime.Now;
                kayit = k; return null;
            }
        }
        /// <summary>Dönemin gerçekleşen ödemeleri (ekran paketi).</summary>
        public static object Paket(string donem) { lock (Kilit) return _liste.Where(x => x.Donem == donem).Select(x => new { x.Anahtar, x.OdenenBanka, x.OdenenElden, x.BankaTarih, x.EldenTarih, x.Not, x.Kullanici, zaman = x.Zaman.ToString("yyyy-MM-dd HH:mm") }).ToList(); }
    }
}
