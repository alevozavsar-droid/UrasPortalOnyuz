// ============================================================
// MÜŞTERİ FİYAT VE KÂR ORANLARI — örnek veri ve kurallar (Yönetim › Yönetim Özeti › Fiyat ve Kâr Yönetimi; YALNIZ Yönetim modülünde)
// İki katmanlı fiyatlandırma:
//   1) ÜRETİCİ → YETKİLİ SATICI : Uras Kimya (üretici) ürünleri yetkili satıcılarına (Selvi Kimya, Alv Kimya, Uras Kimya GmbH) kaç % kârla satar.
//      Oran kalem kalem (ürün bazında) ya da toplu (genel / ürün grubu bazında) tanımlanır. En özel tanım kazanır: Ürün > Grup > Genel.
//      ANA PARA BİRİMİ USD: üretim maliyetleri USD üzerinden tutulur; Satıcı Alış Fiyatı ($) = Üretim Maliyeti ($) × (1 + oran / 100).
//   2) YETKİLİ SATICI → MÜŞTERİ : yetkili satıcı (ör. Selvi Kimya) her müşteri carisine ürün ürün kaç % kârla satar. Aynı kademe kuralı.
//      Müşteri Satış Fiyatı ($) = Satıcı Alış Fiyatı ($) × (1 + oran / 100). Uçtan uca marj = Müşteri Fiyatı / Üretim Maliyeti − 1.
//      Müşteride USD fiyatın EUR ve TL karşılığı "fiyatlandırma kuru" ile gösterilir: piyasa kuru (TCMB döviz satış) + döviz kâr marjı (%),
//      yani kur hareketinden zarar edilmemesi için piyasanın üstünde kur alınır (FyKur: UsdTl, EurTl, Marj; TlKarsiligi / EurKarsiligi).
// Ürünler CariKartOrnek.UrunHiyerarsisi'ndeki Uras Kimya ürün ailelerinden (Boya, Yardımcı Kimyasallar, Tekstil Binderleri) türetilir; üretim
// maliyeti (USD) örnektir. Ayrı bir "liste fiyatı" kavramı yoktur; fiyatlar yalnız maliyet + kâr oranından türer. Müşteriler CARİ_MASTER müşteri kartlarıdır (Data/CariKartOrnek.cs). Kayıtlar bellek içindedir; her
// değişiklik (oran ve kur) audit günlüğüne (kim, ne zaman, eski → yeni) yazılır. Yalnız Yönetici yetkisi (OrnekVeri.Kullanicilar Yetki = "A") kaydedebilir.
// Ekran: Controllers/FiyatYonetimiController.cs + Views/FiyatYonetimi/Index.cshtml + wwwroot/js/fiyat-yonetimi.js
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Genel Oran", "Ürün Grubu", "Kalem").
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Üretici ürünü (fiyat listesi satırı).</summary>
    public class FyUrun
    {
        public string Kod { get; set; }
        public string Ad { get; set; }
        public string AnaGrup { get; set; }
        public string Grup { get; set; }
        public string UrunSinifi { get; set; }
        public string UrunGrubu { get; set; }
        public string Seri { get; set; }
        public string Birim { get; set; } = "kg";
        public string ParaBirimi { get; set; } = "USD";   // ana para birimi: maliyet ve fiyatlar USD
        public decimal Maliyet { get; set; }        // üretim maliyeti USD / birim (fiyatlar yalnız maliyet + kâr oranından türer; ayrı liste fiyatı kavramı yoktur)
        public string Durum { get; set; } = "Aktif";
        public string GrupAnahtari => AnaGrup + "|" + Grup;
    }

    /// <summary>Kur parametreleri: piyasa kuru + döviz kâr marjı → fiyatlandırma kuru. USD fiyatın müşteri için EUR / TL karşılığı bununla hesaplanır.</summary>
    public class FyKur
    {
        public decimal UsdTl { get; set; }          // piyasa USD/TL (TCMB döviz satış)
        public decimal EurTl { get; set; }          // piyasa EUR/TL
        public decimal Marj { get; set; }           // döviz kâr marjı % — fiyatlandırma kuru piyasanın bu kadar üstünde alınır
        public DateTime Tarih { get; set; }
        public string Kaynak { get; set; } = "TCMB Döviz Satış Kuru";
        public string Guncelleyen { get; set; }
        public decimal FiyatUsdTl => Math.Round(UsdTl * (1 + Marj / 100m), 4);          // TL karşılığı için kullanılan kur
        public decimal Parite => UsdTl > 0 ? Math.Round(EurTl / UsdTl, 4) : 0;          // piyasa EUR/USD paritesi (1 EUR = x USD)
        public decimal FiyatParite => Math.Round(Parite / (1 + Marj / 100m), 4);        // EUR karşılığı için kullanılan parite (marj kadar düşük → EUR fiyat yüksek)
        public decimal TlKarsiligi(decimal usd) => Math.Round(usd * FiyatUsdTl, 2);
        public decimal EurKarsiligi(decimal usd) => FiyatParite > 0 ? Math.Round(usd / FiyatParite, 2) : 0;
    }
    public class FyKurGiris { public decimal? UsdTl { get; set; } public decimal? EurTl { get; set; } public decimal? Marj { get; set; } }

    /// <summary>Tek bir kâr oranı tanımı. Kapsam: Genel (anahtar boş) | Grup (anahtar "AnaGrup|Grup") | Urun (anahtar ürün kodu).</summary>
    public class FyOran
    {
        public string Kapsam { get; set; }
        public string Anahtar { get; set; } = "";
        public decimal Oran { get; set; }           // % kâr
        public DateTime Zaman { get; set; }
        public string Kullanici { get; set; }
    }

    /// <summary>Yetkili satıcı fiyat tanımı (üretici → satıcı).</summary>
    public class FySatici
    {
        public string Ad { get; set; }              // Selvi Kimya
        public string Sirket { get; set; }          // grup şirketi kısa adı (Selvi)
        public string CariKodu { get; set; }        // üreticideki müşteri cari kodu
        public string Bolge { get; set; }
        public string Not { get; set; }
        public List<FyOran> Oranlar { get; set; } = new List<FyOran>();
    }

    /// <summary>Müşteri fiyat tanımı (satıcı → müşteri).</summary>
    public class FyMusteri
    {
        public string Satici { get; set; }          // FySatici.Ad
        public string CariKodu { get; set; }
        public List<FyOran> Oranlar { get; set; } = new List<FyOran>();
        public DateTime Zaman { get; set; }
        public string Kullanici { get; set; }
    }

    public class FyAudit { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Katman { get; set; } public string Hedef { get; set; } public string Alan { get; set; } public string Eski { get; set; } public string Yeni { get; set; } }

    /// <summary>Kaydetme isteği (her iki katman): genel oran, grup oranları, kalem oranları. Boş / null oran → tanım silinir.</summary>
    public class FyKayit
    {
        public string Satici { get; set; }
        public string CariKodu { get; set; }        // yalnız müşteri katmanı
        public decimal? Genel { get; set; }
        public List<FyOranGiris> Gruplar { get; set; } = new List<FyOranGiris>();
        public List<FyOranGiris> Urunler { get; set; } = new List<FyOranGiris>();
    }
    public class FyOranGiris { public string Anahtar { get; set; } public decimal? Oran { get; set; } }

    public static class FiyatOrnek
    {
        public static readonly object Kilit = new object();
        public const string Uretici = "Uras Kimya";
        public static readonly string[] Kapsamlar = { "Genel", "Grup", "Urun" };
        public static readonly List<FyAudit> Audit = new List<FyAudit>();

        /// <summary>Yönetici yetkisi: OrnekVeri.Kullanicilar Yetki = "A".</summary>
        public static bool Yonetici(string kullaniciKodu) => OrnekVeri.Kullanicilar.Any(k => string.Equals(k.Kod, kullaniciKodu, StringComparison.OrdinalIgnoreCase) && k.Yetki == "A");

        // ---------- Ürünler (Uras Kimya üretimi) ----------
        private static readonly Lazy<List<FyUrun>> _urunler = new Lazy<List<FyUrun>>(UrunleriUret);
        public static List<FyUrun> Urunler => _urunler.Value;
        public static FyUrun UrunBul(string kod) => Urunler.FirstOrDefault(u => string.Equals(u.Kod, kod, StringComparison.OrdinalIgnoreCase));
        /// <summary>Üreticinin ürettiği ürün aileleri ("AnaGrup|Grup") ve kod ön ekleri, üretim maliyeti aralığı (USD / kg).</summary>
        private static readonly (string Anahtar, string OnEk, decimal MaliyetMin, decimal MaliyetMax)[] Aileler =
        {
            ("Boya Grubu|Emprime", "BOY-EMP", 2.90m, 6.30m), ("Boya Grubu|Dijital", "BOY-DIJ", 5.30m, 11.60m), ("Yardımcı Kimyasallar|Tekstil Yardımcı Kimyasalları", "YRD-TKS", 1.10m, 2.70m), ("Binder Grubu|Tekstil Binderleri", "BND-TKS", 1.70m, 3.00m)
        };
        /// <summary>Kur parametreleri (bellek içi; yönetici değiştirir). Başlangıç: Yapılan Ödemeler'deki örnek kurlar + %3 döviz kâr marjı.</summary>
        public static readonly FyKur Kur = new FyKur { UsdTl = YapilanOdemelerOrnek.Kurlar["USD"], EurTl = YapilanOdemelerOrnek.Kurlar["EUR"], Marj = 3m, Tarih = DateTime.Today, Guncelleyen = "Ali Veli" };
        public static IEnumerable<(string Anahtar, string Ad)> Gruplar => Aileler.Select(a => (a.Anahtar, a.Anahtar.Replace("|", " / ")));
        private static List<FyUrun> UrunleriUret()
        {
            var rnd = new Random(20261001); var l = new List<FyUrun>();
            foreach (var a in Aileler)
            {
                var p = a.Anahtar.Split('|'); int n = 0;
                if (!CariKartOrnek.UrunHiyerarsisi.TryGetValue(a.Anahtar, out var siniflar)) continue;
                foreach (var s in siniflar) foreach (var g in s.Value) foreach (var se in g.Value) foreach (var ad in se.Value)
                {
                    decimal maliyet = Math.Round(a.MaliyetMin + (decimal)rnd.NextDouble() * (a.MaliyetMax - a.MaliyetMin), 2);   // USD / kg
                    l.Add(new FyUrun { Kod = $"{a.OnEk}-{++n:000}", Ad = ad, AnaGrup = p[0], Grup = p[1], UrunSinifi = s.Key, UrunGrubu = g.Key, Seri = se.Key, Maliyet = maliyet });
                }
            }
            return l;
        }

        // ---------- Yetkili satıcılar ----------
        private static readonly List<FySatici> _saticilar = new List<FySatici>();
        private static readonly List<FyMusteri> _musteriler = new List<FyMusteri>();
        private static bool _tohumlandi;
        public static List<FySatici> Saticilar { get { Tohumla(); return _saticilar; } }
        public static List<FyMusteri> MusteriTanimlari { get { Tohumla(); return _musteriler; } }
        public static FySatici SaticiBul(string ad) => Saticilar.FirstOrDefault(s => string.Equals(s.Ad, ad, StringComparison.OrdinalIgnoreCase));
        public static FyMusteri MusteriBul(string satici, string kod) => MusteriTanimlari.FirstOrDefault(m => string.Equals(m.Satici, satici, StringComparison.OrdinalIgnoreCase) && string.Equals(m.CariKodu, kod, StringComparison.OrdinalIgnoreCase));
        /// <summary>Müşteri fiyatı tanımlanabilecek cariler: aktif müşteri kartları (yetkili satıcıların kendi kartları hariç).</summary>
        public static IEnumerable<CariKart> Musteriler() { var satKod = new HashSet<string>(Saticilar.Select(s => s.CariKodu)); return CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" && !satKod.Contains(k.Kod)).OrderBy(k => k.Kod); }

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var z = DateTime.Today.AddDays(-40).AddHours(10); const string k = "Ali Veli";
                FyOran O(string kapsam, string anahtar, decimal oran, int gun = 0) => new FyOran { Kapsam = kapsam, Anahtar = anahtar, Oran = oran, Zaman = z.AddDays(gun), Kullanici = k };
                _saticilar.Add(new FySatici { Ad = "Selvi Kimya", Sirket = "Selvi", CariKodu = "120.006", Bolge = "Yurtiçi", Not = "Ana yetkili satıcı; yurtiçi tekstil ve emprime pazarı.", Oranlar = { O("Genel", "", 12), O("Grup", "Boya Grubu|Emprime", 10), O("Grup", "Boya Grubu|Dijital", 15), O("Urun", "BOY-EMP-001", 8, 5), O("Urun", "BOY-DIJ-002", 18, 12) } });
                _saticilar.Add(new FySatici { Ad = "Alv Kimya", Sirket = "Alv Kimya", CariKodu = "120.005", Bolge = "Yurtiçi", Not = "Grup şirketi; binder ve yardımcı kimyasal ağırlıklı.", Oranlar = { O("Genel", "", 8), O("Grup", "Binder Grubu|Tekstil Binderleri", 6) } });
                _saticilar.Add(new FySatici { Ad = "Uras Kimya GmbH", Sirket = "Uras Kimya GmbH", CariKodu = "120.009", Bolge = "Yurtdışı", Not = "Yurtdışı şube; transfer fiyatı (grup içi).", Oranlar = { O("Genel", "", 6), O("Grup", "Boya Grubu|Dijital", 9) } });
                FyMusteri M(string satici, string kod, int gun, params FyOran[] o) => new FyMusteri { Satici = satici, CariKodu = kod, Oranlar = o.ToList(), Zaman = z.AddDays(gun), Kullanici = k };
                _musteriler.Add(M("Selvi Kimya", "120.001", 2, O("Genel", "", 18), O("Grup", "Boya Grubu|Emprime", 20), O("Urun", "BOY-EMP-001", 22, 20)));     // ABC Tekstil
                _musteriler.Add(M("Selvi Kimya", "120.003", 3, O("Genel", "", 12), O("Grup", "Boya Grubu|Emprime", 11)));                                  // Ege Boya (distribütör)
                _musteriler.Add(M("Selvi Kimya", "120.007", 4, O("Genel", "", 16), O("Urun", "BOY-EMP-004", 14)));                                         // Anadolu Emprime (ilişkili)
                _musteriler.Add(M("Selvi Kimya", "120.011", 6, O("Genel", "", 30)));                                                                       // Nil Tekstil (özel takip, peşin)
                _musteriler.Add(M("Selvi Kimya", "120.002", 8, O("Genel", "", 15)));                                                                       // Marmara Plastik
                _musteriler.Add(M("Alv Kimya", "120.004", 9, O("Genel", "", 14), O("Grup", "Binder Grubu|Tekstil Binderleri", 12)));                     // Karadeniz Ambalaj
                _musteriler.Add(M("Uras Kimya GmbH", "120.008", 10, O("Genel", "", 25), O("Grup", "Boya Grubu|Dijital", 28)));                          // TextilDruck
                Audit.Add(new FyAudit { Zaman = z, Kullanici = k, Katman = "Satıcı", Hedef = "Selvi Kimya", Alan = "Genel Oran", Eski = "", Yeni = "%12" });
                Audit.Add(new FyAudit { Zaman = z.AddDays(5), Kullanici = k, Katman = "Satıcı", Hedef = "Selvi Kimya", Alan = "Kalem BOY-EMP-001", Eski = "%10 (grup)", Yeni = "%8" });
                Audit.Add(new FyAudit { Zaman = z.AddDays(2), Kullanici = k, Katman = "Müşteri", Hedef = "Selvi Kimya → 120.001 ABC Tekstil", Alan = "Genel Oran", Eski = "", Yeni = "%18" });
            }
        }

        // ---------- Oran çözümleme ----------
        /// <summary>Kademeli oran: Ürün > Grup > Genel. Kaynak "Kalem" | "Grup" | "Genel" | "Tanımsız".</summary>
        public static (decimal Oran, string Kaynak) OranCoz(List<FyOran> oranlar, FyUrun u)
        {
            var o = oranlar.FirstOrDefault(x => x.Kapsam == "Urun" && string.Equals(x.Anahtar, u.Kod, StringComparison.OrdinalIgnoreCase)); if (o != null) return (o.Oran, "Kalem");
            o = oranlar.FirstOrDefault(x => x.Kapsam == "Grup" && x.Anahtar == u.GrupAnahtari); if (o != null) return (o.Oran, "Grup");
            o = oranlar.FirstOrDefault(x => x.Kapsam == "Genel"); if (o != null) return (o.Oran, "Genel");
            return (0, "Tanımsız");
        }
        public static decimal SaticiFiyati(FySatici s, FyUrun u) => Math.Round(u.Maliyet * (1 + OranCoz(s.Oranlar, u).Oran / 100m), 2);
        public static decimal MusteriFiyati(FySatici s, FyMusteri m, FyUrun u) => Math.Round(SaticiFiyati(s, u) * (1 + OranCoz(m.Oranlar, u).Oran / 100m), 2);

        // ---------- Kaydetme ----------
        private static string OranMetni(FyOran o) => o == null ? "" : "%" + o.Oran.ToString("0.##");
        /// <summary>Oran listesini istekle eşitler; farkları audit'e yazar. Hata metni döner (null = başarılı).</summary>
        private static string OranlariUygula(List<FyOran> oranlar, FyKayit g, string katman, string hedef, string kullanici, out int degisen)
        {
            degisen = 0; var z = DateTime.Now;
            // 1) ÖNCE tüm doğrulama — hiçbir oran değişmeden. Aksi halde ilk alanlar uygulanıp günlüğe yazıldıktan sonra hata dönüyor,
            //    ekran "kaydedilmedi" sanıp eski veriyle kalıyordu (atomik olmayan kayıt).
            string Kontrol(decimal? oran, string alan) => oran.HasValue && (oran < -50 || oran > 500) ? $"{alan}: oran −50 ile 500 arasında olmalıdır." : null;
            var plan = new List<(string Kapsam, string Anahtar, decimal? Oran, string Alan)> { ("Genel", "", g.Genel, "Genel Oran") };
            foreach (var x in g.Gruplar ?? new List<FyOranGiris>()) { if (!Aileler.Any(a => a.Anahtar == x.Anahtar)) return $"Bilinmeyen ürün grubu: {x.Anahtar}"; plan.Add(("Grup", x.Anahtar, x.Oran, "Grup " + x.Anahtar.Replace("|", " / "))); }
            foreach (var x in g.Urunler ?? new List<FyOranGiris>()) { var u = UrunBul(x.Anahtar); if (u == null) return $"Bilinmeyen ürün: {x.Anahtar}"; plan.Add(("Urun", u.Kod, x.Oran, "Kalem " + u.Kod + " " + u.Ad)); }
            foreach (var p in plan) { var h = Kontrol(p.Oran, p.Alan); if (h != null) return h; }
            // 2) Uygulama — doğrulama geçtiği için artık hata dönmez; her fark günlüğe yazılır
            int sayac = 0;   // out parametresi yerel değişkende sayılır (yerel işlev içinde out kullanılamaz)
            foreach (var p in plan)
            {
                var mevcut = oranlar.FirstOrDefault(x => x.Kapsam == p.Kapsam && string.Equals(x.Anahtar, p.Anahtar ?? "", StringComparison.OrdinalIgnoreCase));
                string eski = OranMetni(mevcut), yeni = p.Oran.HasValue ? "%" + p.Oran.Value.ToString("0.##") : "";
                if (eski == yeni) continue;
                if (mevcut != null) oranlar.Remove(mevcut);
                if (p.Oran.HasValue) oranlar.Add(new FyOran { Kapsam = p.Kapsam, Anahtar = p.Anahtar ?? "", Oran = Math.Round(p.Oran.Value, 2), Zaman = z, Kullanici = kullanici });
                Audit.Insert(0, new FyAudit { Zaman = z, Kullanici = kullanici, Katman = katman, Hedef = hedef, Alan = p.Alan, Eski = eski, Yeni = yeni }); sayac++;
            }
            degisen = sayac; return null;
        }
        public static string SaticiKaydet(FyKayit g, string kullaniciKodu, out int degisen)
        {
            degisen = 0;
            if (!Yonetici(kullaniciKodu)) return "Fiyat ve kâr oranlarını yalnız Yönetici yetkisi değiştirebilir.";
            var s = SaticiBul(g?.Satici ?? ""); if (s == null) return "Yetkili satıcı bulunamadı.";
            lock (Kilit) return OranlariUygula(s.Oranlar, g, "Satıcı", s.Ad, OrnekVeri.KullaniciAdi(kullaniciKodu), out degisen);
        }
        /// <summary>Kur parametrelerini günceller (yalnız Yönetici); her alan değişimi audit'e yazılır.</summary>
        public static string KurKaydet(FyKurGiris g, string kullaniciKodu, out int degisen)
        {
            degisen = 0;
            if (!Yonetici(kullaniciKodu)) return "Kur parametrelerini yalnız Yönetici yetkisi değiştirebilir.";
            if (g == null) return "Geçersiz istek.";
            if (g.UsdTl.HasValue && g.UsdTl <= 0 || g.EurTl.HasValue && g.EurTl <= 0) return "Kur sıfırdan büyük olmalıdır.";
            if (g.Marj.HasValue && (g.Marj < 0 || g.Marj > 50)) return "Döviz kâr marjı 0 ile 50 arasında olmalıdır.";
            string k = OrnekVeri.KullaniciAdi(kullaniciKodu); var z = DateTime.Now; int sayac = 0;
            lock (Kilit)
            {
                // hane: kurlar 4, marj 2 ondalık — karşılaştırma, saklama ve günlük metni aynı hassasiyette (aksi halde günlükte "3 → 3" görünüyordu)
                void Ayarla(string alan, decimal eski, decimal? yeni, Action<decimal> ata, int hane)
                {
                    if (!yeni.HasValue) return; var y = Math.Round(yeni.Value, hane); if (y == eski) return;
                    string bicim = hane == 4 ? "0.0000" : "0.##";
                    Audit.Insert(0, new FyAudit { Zaman = z, Kullanici = k, Katman = "Kur", Hedef = "Fiyatlandırma Kuru", Alan = alan, Eski = eski.ToString(bicim), Yeni = y.ToString(bicim) });
                    ata(y); sayac++;
                }
                Ayarla("USD/TL Piyasa Kuru", Kur.UsdTl, g.UsdTl, v => Kur.UsdTl = v, 4);
                Ayarla("EUR/TL Piyasa Kuru", Kur.EurTl, g.EurTl, v => Kur.EurTl = v, 4);
                Ayarla("Döviz Kâr Marjı %", Kur.Marj, g.Marj, v => Kur.Marj = v, 2);
                if (sayac > 0) { Kur.Tarih = DateTime.Today; Kur.Guncelleyen = k; }
            }
            degisen = sayac; return null;
        }
        public static string MusteriKaydet(FyKayit g, string kullaniciKodu, out int degisen)
        {
            degisen = 0;
            if (!Yonetici(kullaniciKodu)) return "Fiyat ve kâr oranlarını yalnız Yönetici yetkisi değiştirebilir.";
            var s = SaticiBul(g?.Satici ?? ""); if (s == null) return "Yetkili satıcı bulunamadı.";
            var kart = CariKartOrnek.KartBul(g.CariKodu ?? ""); if (kart == null || kart.KartTipi != "Müşteri") return "Müşteri cari kartı bulunamadı.";
            lock (Kilit)
            {
                var m = MusteriBul(s.Ad, kart.Kod);
                if (m == null) { m = new FyMusteri { Satici = s.Ad, CariKodu = kart.Kod, Zaman = DateTime.Now, Kullanici = OrnekVeri.KullaniciAdi(kullaniciKodu) }; _musteriler.Add(m); }
                var h = OranlariUygula(m.Oranlar, g, "Müşteri", $"{s.Ad} → {kart.Kod} {kart.KisaAd}", OrnekVeri.KullaniciAdi(kullaniciKodu), out degisen);
                if (h == null && degisen > 0) { m.Zaman = DateTime.Now; m.Kullanici = OrnekVeri.KullaniciAdi(kullaniciKodu); }
                if (m.Oranlar.Count == 0) _musteriler.Remove(m);   // tüm oranlar silindiyse tanım kalkar
                return h;
            }
        }
    }
}
