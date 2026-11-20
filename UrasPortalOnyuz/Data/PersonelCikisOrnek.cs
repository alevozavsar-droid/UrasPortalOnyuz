// ============================================================
// İŞTEN ÇIKIŞ SENARYOSU — ORTAK SEÇİMLER (20 Kas 2026, kullanıcı: "raporu açan kişi bu seçimli hali ile mi görecek … seçimler herkes için tek ortak senaryo olsun")
//   Konsolide İK Paneli › İşten Çıkış Senaryosu'ndaki "İşten Çıkacak" işaretleri eskiden yalnız işaretleyen kişinin tarayıcısında (localStorage) duruyordu.
//   Artık sunucuda TEK ORTAK senaryo: raporu açan herkes aynı işaretleri görür, biri değiştirince herkes için değişir.
//   Anahtar: "Kaynak:Sıra" (Bordro:33 · Özel:5 · Üretim:7 — JS cikisAnahtar ile aynı); dönemden bağımsız (kişi sırası dönemler arasında aynı).
//   Kayıtlar bellekte; kalıcı günlük tipi "cikis" (PersonelMaliyetKalici) → yeniden başlatınca aynı sırayla uygulanır.
//   Senaryo veriyi değiştirmez: bordro, ödeme ve muhasebe kayıtlarına dokunmaz; işaretler yalnız bu sekmenin hesabına girer.
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Ekrandan gelen değişiklik (ve kalıcı günlük satırı): Anahtarlar işaretlenir (Cikacak = true) ya da kaldırılır (false); Temizle = true tüm senaryoyu boşaltır.</summary>
    public class PcIstek { public List<string> Anahtarlar { get; set; } public bool Cikacak { get; set; } public bool Temizle { get; set; } }

    public static class PersonelCikisOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly HashSet<string> _secim = new HashSet<string>(StringComparer.Ordinal);
        private static string _sonKisi = ""; private static DateTime? _sonZaman;

        /// <summary>Senaryoyu günceller. Hata metni döner (null = başarılı); degisen = fiilen değişen işaret sayısı.</summary>
        public static string Kaydet(PcIstek d, string kisi, out int degisen, DateTime? zaman = null)   // zaman: kalıcı günlükten yeniden uygularken kaydın asıl zamanı
        {
            degisen = 0; if (d == null) return "Veri yok.";
            lock (Kilit)
            {
                if (d.Temizle) { degisen = _secim.Count; _secim.Clear(); }
                else
                {
                    var anahtarlar = (d.Anahtarlar ?? new List<string>()).Select(a => (a ?? "").Trim()).Where(a => a.Length > 0).Distinct().ToList();
                    if (anahtarlar.Count == 0) return "İşaretlenecek kişi yok.";
                    if (anahtarlar.Any(a => a.Length > 40 || !a.Contains(":"))) return "Satır anahtarı geçersiz.";
                    foreach (var a in anahtarlar) if (d.Cikacak ? _secim.Add(a) : _secim.Remove(a)) degisen++;
                }
                _sonKisi = kisi ?? ""; _sonZaman = zaman ?? DateTime.Now;
                return null;
            }
        }
        /// <summary>Ekran paketi: ortak senaryodaki anahtarlar + son değiştiren.</summary>
        public static object Paket() { lock (Kilit) return new { anahtarlar = _secim.OrderBy(x => x, StringComparer.Ordinal).ToList(), sonKisi = _sonKisi, sonZaman = _sonZaman?.ToString("dd.MM.yyyy HH:mm") ?? "" }; }
    }
}
