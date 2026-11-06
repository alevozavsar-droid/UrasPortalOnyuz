// ============================================================
// PERSONEL MALİYET RAPORU — Şirket Teşvik ve İndirimleri sekmesi (kullanıcı, 5 Kas 2026): bordroda uygulanan SGK işveren teşvikleri, destekler ve gelir / damga vergisi istisnalarının kataloğu;
//   her satır için şirket bazında "faydalanıyor" seçimi (seçili değilse muaf / faydalanamıyor). Hesap (seçime göre, kişi bazında): Teşvik İndirimi = SGK matrahı × puan (+ asgari ücret desteği prim günüyle,
//   engelli teşviki prime esas kazanç alt sınırı üzerinden). Toplam İşveren resmi bordro gibi brüt kalır; "Teşvik Sonrası İşveren" ve "Genel Maliyet (Teşvik Sonrası)" ayrı sütunlardır.
//   Gelir / damga vergisi istisnası seçili değilse model hesabında istisna düşülmez (kaynak brütlü satırda GV artık olduğundan yalnız dağılım değişir).
//   Varsayılan: GV ve damga istisnası tüm şirketlerde; 5 puan indirimi yalnız Selvi Kimya (resmi Ekim 2026 bordrolarında İstihdam Hazine İndirimi yalnız Selvi'de, oran %5).
// Ekran: Views/PersonelMaliyet/Index.cshtml (data-p="tesvik") + wwwroot/js/personel-maliyet.js (tesvikCiz) + Controllers/PersonelMaliyetController.cs (TesvikKaydet)
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Bordro teşviki / indirimi / istisnası tanımı.</summary>
    public class PmTesvik
    {
        public string Kod { get; set; } public string Ad { get; set; } public string Tur { get; set; } public string Dayanak { get; set; } public string Aciklama { get; set; }
        /// <summary>SGK matrahının yüzdesi (puan); 0 = tutar / istisna / bilgi.</summary>
        public decimal Puan { get; set; }
        /// <summary>Kişi başı aylık sabit destek (30 prim günü için; prim günüyle orantılı).</summary>
        public decimal AylikTutar { get; set; }
        /// <summary>Evet = teşvik indirimine girer · İstisna = vergi istisnası (model hesabı) · Bilgi = kişi / süre şartlı, hesaba girmez.</summary>
        public string Hesap { get; set; }
        /// <summary>Emekli (SGDP) sigortalıya da uygulanır mı.</summary>
        public bool Emekliye { get; set; }
        /// <summary>Tümü | Engelli (yalnız Özel Durumu = Engelli kişiler; alt sınır üzerinden).</summary>
        public string Kapsam { get; set; } = "Tümü";
    }

    public static class PersonelTesvikOrnek
    {
        public static readonly object Kilit = new object();
        public const string GvIstisna = "GVISTISNA", DvIstisna = "DVISTISNA", BesPuan = "5PUAN";
        private static PmTesvik T(string kod, string ad, string tur, string dayanak, string aciklama, string hesap, decimal puan = 0, decimal aylik = 0, bool emekliye = false, string kapsam = "Tümü")
            => new PmTesvik { Kod = kod, Ad = ad, Tur = tur, Dayanak = dayanak, Aciklama = aciklama, Hesap = hesap, Puan = puan, AylikTutar = aylik, Emekliye = emekliye, Kapsam = kapsam };
        /// <summary>Katalog (2026 mevzuatı; tutar ve oranlar değişince burası güncellenir).</summary>
        public static readonly PmTesvik[] Tesvikler =
        {
            T(BesPuan, "5 Puanlık İşveren Hissesi İndirimi", "SGK İşveren", "5510 s.K. md. 81/ı", "Malullük, yaşlılık ve ölüm sigortası işveren hissesinden 5 puan Hazine'ce karşılanır. Şart: SGK prim borcu olmaması, bildirgenin süresinde verilmesi. Emekli (SGDP) sigortalıya uygulanmaz.", "Evet", 5m),
            T("4PUAN", "İlave 4 Puan Bölgesel Teşvik", "SGK İşveren", "5510 s.K. md. 81/ı (2. fıkra) · Cumhurbaşkanı kararıyla belirlenen 51 il", "5 puan indirimine ek 4 puan; yalnız kapsam illerindeki iş yerleri. Emekliye uygulanmaz.", "Evet", 4m),
            T("ASGDESTEK", "Asgari Ücret Desteği", "Destek", "5510 s.K. geçici madde (her yıl uzatılır)", "Prim gününe göre günlük sabit tutar, işveren prim borcundan mahsup edilir. 2025 tutarı 1.000 ₺/ay (33,33 ₺/gün) alındı; 2026 tutarı yayımlanınca güncellenir.", "Evet", 0m, 1000m),
            T("ENGELLI", "Engelli İstihdamı Teşviki", "SGK İşveren", "4857 s.K. md. 30 · 5510 s.K. md. 81", "Engelli sigortalının prime esas kazanç alt sınırı (asgari ücret) üzerinden hesaplanan işveren hissesinin tamamı (işsizlik %2 + MYÖ %12 + GSS %7,5 = 21,5 puan) Hazine'ce karşılanır. Yalnız Özel Durumu = Engelli kişiler.", "Evet", 21.5m, 0m, false, "Engelli"),
            T("6111", "Genç, Kadın ve Mesleki Belgeli İlave İstihdam Teşviki", "SGK İşveren", "4447 s.K. geçici md. 10", "Şartları sağlayan ilave istihdam için işveren hissesinin tamamı; kişi ve süre şartlı olduğundan bu raporda bilgi amaçlıdır, hesaba girmez.", "Bilgi"),
            T("7103", "İlave İstihdam Teşviki (Prim + Vergi)", "SGK İşveren + Vergi", "4447 s.K. geçici md. 19 · 193 s.K. geçici md. 80", "İlave istihdam edilen sigortalı için işçi + işveren primi ile gelir / damga vergisi desteği; kişi bazlı, bilgi amaçlı.", "Bilgi"),
            T(GvIstisna, "Asgari Ücret Gelir Vergisi İstisnası", "Gelir Vergisi", "193 s.K. md. 23/18", "Asgari ücrete isabet eden gelir vergisi istisnası; tüm ücretliler. Seçili değilse model hesabında istisna düşülmez (birden fazla işverende çalışanda yalnız bir işverende uygulanır).", "İstisna", 0m, 0m, true),
            T(DvIstisna, "Asgari Ücret Damga Vergisi İstisnası", "Damga Vergisi", "488 s.K. (2) sayılı tablo IV/34", "Asgari ücrete isabet eden damga vergisi istisnası; seçili değilse model hesabında istisna düşülmez.", "İstisna", 0m, 0m, true)
        };
        /// <summary>Şirket → faydalanılan kodlar. Varsayılan: istisnalar; Selvi Kimya'da ayrıca 5 puan (resmi bordro). Bellekte tutulur.</summary>
        private static readonly Dictionary<string, HashSet<string>> Secim = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        private static HashSet<string> Sirket(string sirket)
        {
            string s = (sirket ?? "").Trim(); if (s.Length == 0) s = "—";
            if (!Secim.TryGetValue(s, out var set))
            {
                set = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { GvIstisna, DvIstisna };
                if (string.Equals(s, "Selvi Kimya", StringComparison.OrdinalIgnoreCase)) set.Add(BesPuan);
                Secim[s] = set;
            }
            return set;
        }
        /// <summary>Şirket bu teşvikten faydalanıyor mu (seçili değilse muaf / faydalanamıyor).</summary>
        public static bool Uygulanir(string sirket, string kod) { lock (Kilit) return Sirket(sirket).Contains(kod ?? ""); }
        /// <summary>Kişinin seçime göre teşvik indirimi (₺): SGK matrahı × puan (emekliye uygulanmayanlar hariç), asgari ücret desteği prim günüyle, engelli teşviki alt sınır üzerinden.</summary>
        public static decimal Indirim(PmPersonel p, decimal sgkMatrahi, bool emekli)
        {
            if (p == null || sgkMatrahi <= 0) return 0m;
            decimal t = 0m;
            lock (Kilit)
            {
                var set = Sirket(p.SgkIsvereni);
                foreach (var k in Tesvikler)
                {
                    if (k.Hesap != "Evet" || !set.Contains(k.Kod)) continue;
                    if (emekli && !k.Emekliye) continue;
                    if (k.Kapsam == "Engelli" && p.OzelDurum != "Engelli") continue;
                    if (k.Puan > 0) t += (k.Kapsam == "Engelli" ? Math.Min(sgkMatrahi, BordroParametre.AsgariBrut) : sgkMatrahi) * k.Puan / 100m;
                    if (k.AylikTutar > 0) t += k.AylikTutar * Math.Min(30m, Math.Max(0m, p.PrimGun)) / 30m;
                }
            }
            return BordroParametre.Yuvarla(t);
        }
        /// <summary>Seçimi kaydeder; hata metni döner (null = başarılı).</summary>
        public static string Kaydet(string sirket, string kod, bool secili)
        {
            if (string.IsNullOrWhiteSpace(sirket)) return "Şirket seçilmedi.";
            if (!Tesvikler.Any(t => t.Kod == kod)) return "Teşvik kodu geçersiz.";
            lock (Kilit) { var set = Sirket(sirket); if (secili) set.Add(kod); else set.Remove(kod); }
            return null;
        }
        /// <summary>Ekrana giden paket: katalog, şirketler (SGK işverenleri), şirket → seçili kodlar, istemci hesabı için şirket → toplam puan / aylık destek / engelli puanı / istisna bayrakları.</summary>
        public static object Paket()
        {
            var sirketler = PersonelMaliyetOrnek.SgkIsverenleri;
            lock (Kilit)
            {
                return new
                {
                    tesvikler = Tesvikler.Select(t => new { kod = t.Kod, ad = t.Ad, tur = t.Tur, dayanak = t.Dayanak, aciklama = t.Aciklama, puan = t.Puan, aylikTutar = t.AylikTutar, hesap = t.Hesap, emekliye = t.Emekliye, kapsam = t.Kapsam }).ToList(),
                    sirketler,
                    secim = sirketler.ToDictionary(s => s, s => Tesvikler.Where(t => Sirket(s).Contains(t.Kod)).Select(t => t.Kod).ToArray()),
                    sirketOran = sirketler.ToDictionary(s => s, s =>
                    {
                        var set = Sirket(s); var evet = Tesvikler.Where(t => t.Hesap == "Evet" && set.Contains(t.Kod)).ToList();
                        return new
                        {
                            puan = evet.Where(t => t.Kapsam == "Tümü").Sum(t => t.Puan), puanEmekli = evet.Where(t => t.Kapsam == "Tümü" && t.Emekliye).Sum(t => t.Puan),
                            aylik = evet.Where(t => t.Kapsam == "Tümü").Sum(t => t.AylikTutar), aylikEmekli = evet.Where(t => t.Kapsam == "Tümü" && t.Emekliye).Sum(t => t.AylikTutar),
                            engelliPuan = evet.Where(t => t.Kapsam == "Engelli").Sum(t => t.Puan), gvIstisna = set.Contains(GvIstisna), dvIstisna = set.Contains(DvIstisna)
                        };
                    })
                };
            }
        }
    }
}
