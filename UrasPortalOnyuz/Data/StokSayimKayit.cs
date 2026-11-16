// ============================================================
// FİİLİ SAYIM KAYITLARI (15 Kas 2026) — Konsolide Stok Raporu › Sayım (sayan kişi girer) ve Sayım Onayı (yetkili onaylar) sekmeleri.
// Sayım kaydı: sayılan miktar (zorunlu), bloke / karantina miktarı, stok konumu, son kullanma tarihi, sayım tarihi, not; sayan = oturumdaki kullanıcı.
// Kullanıcı isteği: "muhasebesel değerler zaten otomatik gelir ama fiili sayımı yapıp onay verilmeli; sistemden buraya verileri girip
// sayım raporundaki tüm sekmeler ile senkronize çalışmalı."
// Akış: sayım girilir (Onay Bekliyor) → onaylanır (Onaylandı) ya da reddedilir (Reddedildi). YALNIZ ONAYLI sayım fiili stoku değiştirir:
//   rapor (wwwroot/js/stok-raporu.js) onaylı sayımı ilgili stok satırının sayılan miktarı olarak kullanır; Stok Listesi, Tüketim ve Yeterlilik,
//   Sayım ve Risk, Stok ve Uyum, Tüketim Verisi, Aylık Stok Hareketleri, Ürün Hiyerarşisi ve özetler aynı değerden hesaplanır.
// Muhasebe (R) değerleri buradan DEĞİŞTİRİLMEZ; muhasebe kaydından gelir.
// Satır anahtarı: şirket | depo kodu | stok kodu | parti / lot.
// Saklama: App_Data/stok-sayim.json (PersonelMaliyetKalici.Dizin altında; PM_KALICI_DIZIN ile değiştirilebilir). Dizin yoksa kayıtlar yalnız bellekte durur.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WebApplication3.Data
{
    public static class StokSayimKayit
    {
        public const string Bekliyor = "Onay Bekliyor", Onayli = "Onaylandı", Red = "Reddedildi";

        public class Sayim
        {
            public int Id { get; set; }
            public string Anahtar { get; set; }
            public decimal Sayilan { get; set; }
            /// <summary>Sayım tarihi (yyyy-AA-gg).</summary>
            public string Tarih { get; set; }
            public string Not { get; set; }
            /// <summary>Bloke / karantina miktarı (boş = değişmedi).</summary>
            public decimal? Bloke { get; set; }
            /// <summary>Stok konumu: Depoda · Dış Depoda · Transferde · Konsinyede (boş = değişmedi).</summary>
            public string Konum { get; set; }
            /// <summary>Son kullanma tarihi (yyyy-AA-gg; boş = değişmedi).</summary>
            public string Skt { get; set; }
            public string Durum { get; set; }
            public string Belge { get; set; }
            public string Giren { get; set; }
            public DateTime GirisZamani { get; set; }
            public string Onaylayan { get; set; }
            public DateTime? OnayZamani { get; set; }
            public string RedNedeni { get; set; }
        }

        /// <summary>Ekrandan gelen sayım girişi.</summary>
        public class Giris { public string Anahtar { get; set; } public decimal? Sayilan { get; set; } public decimal? Bloke { get; set; } public string Konum { get; set; } public string Skt { get; set; } public string Tarih { get; set; } public string Not { get; set; } }
        public static readonly string[] Konumlar = { "Depoda", "Dış Depoda", "Transferde", "Konsinyede" };
        /// <summary>Ekrandan gelen karar: onay · red · geriAl (onayı geri al) · sil (onaylanmamış kaydı sil).</summary>
        public class Karar { public List<int> Idler { get; set; } public string Islem { get; set; } public string Neden { get; set; } }

        static readonly object Kilit = new object();
        static List<Sayim> _liste;
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, WriteIndented = true };
        static string Dosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data", "stok-sayim.json");

        static List<Sayim> Yukle()
        {
            if (_liste != null) return _liste;
            _liste = new List<Sayim>();
            try { var d = Dosya; if (d != null && File.Exists(d)) _liste = JsonSerializer.Deserialize<List<Sayim>>(File.ReadAllText(d), Secenek) ?? new List<Sayim>(); }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] Stok sayım kayıtları okunamadı: {ex.Message}"); }
            return _liste;
        }

        static void Yaz()
        {
            var d = Dosya; if (d == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(d)); File.WriteAllText(d, JsonSerializer.Serialize(_liste, Secenek)); }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] Stok sayım kayıtları yazılamadı: {ex.Message}"); }
        }

        public static List<Sayim> Liste() { lock (Kilit) return Yukle().ToList(); }

        /// <summary>Sayım girişlerini kaydeder: satırın bekleyen kaydı varsa günceller, yoksa yeni "Onay Bekliyor" kaydı açar. Dönen değer: (kaydedilen sayısı, hata iletisi).</summary>
        public static (int Adet, string Hata) Kaydet(IEnumerable<Giris> girisler, string kisi)
        {
            var bugun = DateTime.Today; int adet = 0;
            lock (Kilit)
            {
                var l = Yukle();
                foreach (var g in girisler ?? Enumerable.Empty<Giris>())
                {
                    if (g == null || string.IsNullOrWhiteSpace(g.Anahtar) || g.Anahtar.Length > 200 || g.Anahtar.Split('|').Length != 4) return (adet, "Stok satırı tanınmadı.");
                    if (!g.Sayilan.HasValue || g.Sayilan < 0 || g.Sayilan > 1_000_000_000m) return (adet, "Sayılan miktar 0 ya da daha büyük bir sayı olmalı.");
                    if (!DateTime.TryParseExact(g.Tarih, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)) t = bugun;
                    if (t > bugun) return (adet, "Sayım tarihi bugünden ileri olamaz.");
                    if (g.Bloke.HasValue && (g.Bloke < 0 || g.Bloke > g.Sayilan)) return (adet, "Bloke / karantina miktarı 0 ile sayılan miktar arasında olmalı.");
                    string konum = string.IsNullOrWhiteSpace(g.Konum) ? null : g.Konum.Trim(); if (konum != null && !Konumlar.Contains(konum)) return (adet, "Stok konumu tanınmadı.");
                    string skt = null; if (!string.IsNullOrWhiteSpace(g.Skt)) { if (!DateTime.TryParseExact(g.Skt, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sk)) return (adet, "Son kullanma tarihi geçersiz."); skt = sk.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }
                    string not = (g.Not ?? "").Trim(); if (not.Length > 300) not = not.Substring(0, 300);
                    var k = l.LastOrDefault(x => x.Anahtar == g.Anahtar && x.Durum == Bekliyor);
                    if (k == null) { k = new Sayim { Id = l.Count == 0 ? 1 : l.Max(x => x.Id) + 1, Anahtar = g.Anahtar, Durum = Bekliyor }; k.Belge = $"SYM-{t:yyMM}-{k.Id:0000}"; l.Add(k); }
                    k.Sayilan = Math.Round(g.Sayilan.Value, 3); k.Tarih = t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); k.Not = not; k.Bloke = g.Bloke.HasValue ? Math.Round(g.Bloke.Value, 3) : (decimal?)null; k.Konum = konum; k.Skt = skt; k.Giren = kisi; k.GirisZamani = DateTime.Now;
                    adet++;
                }
                if (adet > 0) Yaz();
            }
            return (adet, null);
        }

        /// <summary>Bir stok satırının bütün sayım kayıtlarını siler (kullanıcının eklediği ürün / demirbaş silinince). Dönen değer: silinen kayıt sayısı.</summary>
        public static int AnahtarSil(string anahtar)
        {
            lock (Kilit) { var l = Yukle(); int n = l.RemoveAll(x => x.Anahtar == anahtar); if (n > 0) Yaz(); return n; }
        }

        /// <summary>Onay · red · onayı geri alma · silme. Dönen değer: (işlenen kayıt sayısı, hata iletisi).</summary>
        public static (int Adet, string Hata) Karara(Karar g, string kisi)
        {
            if (g?.Idler == null || g.Idler.Count == 0) return (0, "Kayıt seçilmedi.");
            int adet = 0;
            lock (Kilit)
            {
                var l = Yukle();
                foreach (int id in g.Idler.Distinct())
                {
                    var k = l.FirstOrDefault(x => x.Id == id); if (k == null) continue;
                    switch (g.Islem)
                    {
                        case "onay": if (k.Durum != Bekliyor) continue; k.Durum = Onayli; k.Onaylayan = kisi; k.OnayZamani = DateTime.Now; k.RedNedeni = null; break;
                        case "red": if (k.Durum != Bekliyor) continue; k.Durum = Red; k.Onaylayan = kisi; k.OnayZamani = DateTime.Now; k.RedNedeni = (g.Neden ?? "").Trim(); break;
                        case "geriAl": if (k.Durum != Onayli && k.Durum != Red) continue; k.Durum = Bekliyor; k.Onaylayan = null; k.OnayZamani = null; k.RedNedeni = null; break;
                        case "sil": if (k.Durum == Onayli) continue; l.Remove(k); break;
                        default: return (adet, "İşlem tanınmadı.");
                    }
                    adet++;
                }
                if (adet > 0) Yaz();
            }
            return (adet, adet == 0 ? "İşlenecek uygun kayıt bulunamadı." : null);
        }
    }
}
