// ============================================================
// MÜŞTERİ FİYAT VE KÂR ORANLARI — örnek veri ve kurallar (Yönetim › Yönetim Özeti › Fiyat ve Kâr Yönetimi; YALNIZ Yönetim modülünde)
// İki katmanlı fiyatlandırma:
//   1) ÜRETİCİ → YETKİLİ SATICI : Uras Kimya (üretici) ürünleri yetkili satıcılarına (Selvi Kimya, Alv Kimya, Uras Kimya GmbH) kaç % kârla satar.
//      Oran kalem kalem (ürün bazında) ya da toplu (genel / ürün grubu bazında) tanımlanır. En özel tanım kazanır: Ürün > Grup > Genel.
//      ANA PARA BİRİMİ USD: üretim maliyetleri USD üzerinden tutulur; Satıcı Alış Fiyatı ($) = Üretim Maliyeti ($) × (1 + oran / 100).
//   2) YETKİLİ SATICI → MÜŞTERİ : yetkili satıcı (ör. Selvi Kimya) her müşteri carisine ürün ürün kaç % kârla satar. Aynı kademe kuralı.
//      Müşteri Satış Fiyatı ($) = Satıcı Alış Fiyatı ($) × (1 + oran / 100). Uçtan uca marj = Müşteri Fiyatı / Toplam Maliyet − 1.
//   13 Kas 2026: (a) ZİNCİR DİNAMİK — üretici de seçilir (Ureticiler); her üretici → satıcı çifti kendi oranlarını tutar (FySatici.UreticiOranlari).
//      (b) MALİYET BİLEŞENLERİ — Toplam Maliyet = (Üretim Maliyeti + Ambalaj Maliyeti) × (1 + Risk Payı %); kâr oranı toplam maliyete uygulanır.
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
        public decimal? AmbalajMaliyeti { get; set; }   // ambalaj maliyeti USD / birim (kaleme özel); null = toplu değer (FiyatOrnek.GenelAmbalaj)
        public decimal? RiskPayi { get; set; }          // risk payı % (beklenmeyen giderler için emniyet payı; kaleme özel); null = toplu değer (FiyatOrnek.GenelRisk)
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
        public List<FyOran> Oranlar { get; set; } = new List<FyOran>();   // varsayılan üretici (FiyatOrnek.Uretici = Uras Kimya) → bu satıcı
        /// <summary>13 Kas 2026 — zincir dinamik: başka bir üretici şirketten (ör. Alv Kimya → Selvi Kimya) alış oranları; anahtar üretici adı.</summary>
        public Dictionary<string, List<FyOran>> UreticiOranlari { get; set; } = new Dictionary<string, List<FyOran>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Müşteri fiyat tanımı (satıcı → müşteri).</summary>
    public class FyMusteri
    {
        public string Satici { get; set; }          // FySatici.Ad
        public string CariKodu { get; set; }
        public List<FyOran> Oranlar { get; set; } = new List<FyOran>();
        /// <summary>13 Kas 2026 — müşteri katmanında risk payı %: satıcı alış fiyatının üstüne, kâr oranından ÖNCE eklenir. RiskPayi müşterinin toplu değeri, UrunRisk kaleme özel (kalem ezer).</summary>
        public decimal? RiskPayi { get; set; }
        public Dictionary<string, decimal> UrunRisk { get; set; } = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        public DateTime Zaman { get; set; }
        public string Kullanici { get; set; }
    }

    public class FyAudit { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Katman { get; set; } public string Hedef { get; set; } public string Alan { get; set; } public string Eski { get; set; } public string Yeni { get; set; } }

    /// <summary>Kaydetme isteği (her iki katman): genel oran, grup oranları, kalem oranları. Boş / null oran → tanım silinir.</summary>
    public class FyKayit
    {
        public string Uretici { get; set; }         // yalnız satıcı katmanı: zincirin üreticisi (boş = varsayılan üretici)
        public string Satici { get; set; }
        public string CariKodu { get; set; }        // yalnız müşteri katmanı
        public decimal? Genel { get; set; }
        public List<FyOranGiris> Gruplar { get; set; } = new List<FyOranGiris>();
        public List<FyOranGiris> Urunler { get; set; } = new List<FyOranGiris>();
        // yalnız müşteri katmanı — risk payı: RiskGonderildi doğruysa Risk müşterinin toplu risk payıdır (null = siler); UrunRiskler değişen kalemler (Oran null = kalem değeri silinir)
        public bool RiskGonderildi { get; set; }
        public decimal? Risk { get; set; }
        public List<FyOranGiris> UrunRiskler { get; set; } = new List<FyOranGiris>();
    }
    public class FyOranGiris { public string Anahtar { get; set; } public decimal? Oran { get; set; } }
    /// <summary>Maliyet bileşenleri kaydı: toplu ambalaj maliyeti ve risk payı + değişen kalemler (null = kaleme özel değer silinir, toplu değer geçerli olur).</summary>
    public class FyMaliyetKayit { public string AmbalajTipi { get; set; } public decimal? GenelAmbalaj { get; set; } public decimal? GenelRisk { get; set; } public List<FyMaliyetGiris> Urunler { get; set; } = new List<FyMaliyetGiris>(); }
    public class FyMaliyetGiris { public string Kod { get; set; } public decimal? Ambalaj { get; set; } public decimal? Risk { get; set; } }
    /// <summary>Otomatik ambalaj maliyeti: ambalaj alım kalemi (Alım Onay son fatura fiyatı), KDV DAHİL tutar, kap kapasitesi ve $ / kg karşılığı.</summary>
    public class FyAmbalaj { public string Kod { get; set; } public string Ad { get; set; } public string Birim { get; set; } public decimal BirimFiyat { get; set; } public decimal Kdv { get; set; } public decimal KdvDahil { get; set; } public decimal KapasiteKg { get; set; } public decimal TlKg { get; set; } public decimal UsdKg { get; set; } public string Tedarikci { get; set; } public string Tarih { get; set; } public string FaturaNo { get; set; } public decimal Kur { get; set; } }
    /// <summary>Aylık üretim rakamı kaydı (Genel Gider Payı sekmesi): null = girilmedi.</summary>
    public class FyUretimKayit { public string Uretici { get; set; } public int Yil { get; set; } public List<decimal?> Aylar { get; set; } = new List<decimal?>(); }

    public static class FiyatOrnek
    {
        public static readonly object Kilit = new object();
        public const string Uretici = "Uras Kimya";   // varsayılan üretici (öbür modüller — Özel Fiyat, Satış Siparişi — bu zinciri kullanır)
        /// <summary>13 Kas 2026 — zincir dinamik: üretici olarak seçilebilen grup şirketleri. Seçilen üretici, kendisi dışındaki her şirkete (yetkili satıcı) oran tanımlayabilir.</summary>
        public static readonly string[] Ureticiler = { "Uras Kimya", "Alv Kimya", "Selvi Kimya", "Avrupa Paper", "Daf Kimya" };
        // ---------- Maliyet bileşenleri (13 Kas 2026) ----------
        // Toplam Maliyet = (Üretim Maliyeti + Ambalaj Maliyeti) × (1 + Risk Payı %). Kâr oranı toplam maliyetin üstüne uygulanır.
        // Ambalaj ve risk payı toplu (tüm ürünler) ya da kalem kalem girilir; kalem değeri toplu değeri ezer. Başlangıçta ikisi de 0'dır (fiyatlar değişmez).
        // Toplu ambalaj maliyeti OTOMATİK gelir (13 Kas 2026, "ambalaj fiyatı otomatik gelsin ama KDV dahil gelsin"): seçili ambalaj tipinin (alım kalemi) Alım Onay'daki
        // son fatura birim fiyatı × (1 + KDV) ÷ kap kapasitesi (kg) ÷ piyasa USD/TL kuru. Yönetici isterse toplu değeri elle girer (GenelAmbalajElle); kalem değeri hepsini ezer.
        public static string AmbalajTipi = "AM-TNK-06";      // Metal Bidon 25 L
        public static decimal? GenelAmbalajElle;             // USD / birim; null = otomatik
        private static (decimal Kur, List<FyAmbalaj> Liste) _ambalajOnbellek;
        /// <summary>Ambalaj tipleri ve güncel maliyetleri (kap birimiyle alınan kalemler; kapasite adındaki litre değerinden, koli için 25 kg).</summary>
        public static List<FyAmbalaj> AmbalajTipleri()
        {
            var kur = Kur.UsdTl; var c = _ambalajOnbellek; if (c.Liste != null && c.Kur == kur) return c.Liste;
            var l = new List<FyAmbalaj>();
            foreach (var a in AlimOnayOrnek.AmbalajFiyatlari().Where(x => x.Birim == "adet"))
            {
                var m = System.Text.RegularExpressions.Regex.Match(a.Ad ?? "", @"(\d+(?:[.,]\d+)?)\s*L\b"); decimal kap = m.Success ? decimal.Parse(m.Groups[1].Value.Replace(',', '.'), System.Globalization.CultureInfo.InvariantCulture) : 25m;
                decimal dahil = Math.Round(a.Fiyat * (1 + a.Kdv / 100m), 2), tlKg = kap > 0 ? Math.Round(dahil / kap, 4) : 0;
                l.Add(new FyAmbalaj { Kod = a.Kod, Ad = a.Ad, Birim = a.Birim, BirimFiyat = a.Fiyat, Kdv = a.Kdv, KdvDahil = dahil, KapasiteKg = kap, TlKg = tlKg, UsdKg = kur > 0 ? Math.Round(tlKg / kur, 4) : 0, Tedarikci = a.Tedarikci, Tarih = a.Tarih?.ToString("dd.MM.yyyy") ?? "", FaturaNo = a.FaturaNo ?? "", Kur = kur });
            }
            _ambalajOnbellek = (kur, l); return l;
        }
        public static FyAmbalaj AmbalajOto() { var l = AmbalajTipleri(); return l.FirstOrDefault(x => x.Kod == AmbalajTipi) ?? l.FirstOrDefault(); }
        public static decimal GenelAmbalaj => GenelAmbalajElle ?? AmbalajOto()?.UsdKg ?? 0m;   // USD / birim
        public static decimal GenelRisk;      // %
        /// <summary>Maliyet bileşenleri her değiştiğinde artar (maliyeti önbelleğe alan modüller — Konsolide Satış Raporu kalemleri — bununla tazelenir).</summary>
        public static int MaliyetSurum;
        public static decimal Ambalaj(FyUrun u) => u.AmbalajMaliyeti ?? GenelAmbalaj;
        public static decimal Risk(FyUrun u) => u.RiskPayi ?? GenelRisk;
        /// <summary>Gerçekleşen maliyet: üretim + ambalaj (risk payı hariç; satış kârlılığı bununla ölçülür).</summary>
        public static decimal GercekMaliyet(FyUrun u) => u.Maliyet + Ambalaj(u);
        /// <summary>Fiyatlandırma maliyeti: (üretim + ambalaj) × (1 + risk payı).</summary>
        public static decimal ToplamMaliyet(FyUrun u) => Math.Round(GercekMaliyet(u) * (1 + Risk(u) / 100m), 4);
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
        // Zincirde yetkili satıcı olarak seçilebilenler: tanımlı satıcılar + satıcı kaydı olmayan üretici şirketler (Uras Kimya, Avrupa Paper, Daf Kimya).
        // Saticilar listesi (öbür modüllerin gördüğü) değişmez; ek şirketler yalnız Müşteri Fiyat ve Kâr Oranları ekranında görünür.
        private static readonly List<FySatici> _ekSaticilar = new List<FySatici>();
        public static List<FySatici> ZincirSaticilar { get { var l = Saticilar.ToList(); lock (_ekSaticilar) { if (_ekSaticilar.Count == 0) foreach (var ad in Ureticiler.Where(x => !l.Any(s => string.Equals(s.Ad, x, StringComparison.OrdinalIgnoreCase)))) { var k = CariKartOrnek.Kartlar.FirstOrDefault(c => c.KartTipi == "Müşteri" && string.Equals(c.KisaAd, ad, StringComparison.OrdinalIgnoreCase)); _ekSaticilar.Add(new FySatici { Ad = ad, Sirket = ad, CariKodu = k?.Kod ?? "", Bolge = k?.Bolge ?? "Yurtiçi", Not = "Grup şirketi; zincirde yetkili satıcı olarak seçilebilir." }); } l.AddRange(_ekSaticilar); } return l; } }
        public static FySatici ZincirSaticiBul(string ad) => ZincirSaticilar.FirstOrDefault(s => string.Equals(s.Ad, ad, StringComparison.OrdinalIgnoreCase));
        /// <summary>Üretici → satıcı oran listesi: varsayılan üreticide FySatici.Oranlar, öbür üreticilerde UreticiOranlari (yoksa açılır).</summary>
        public static List<FyOran> OranListesi(FySatici s, string uretici) { if (string.IsNullOrWhiteSpace(uretici) || string.Equals(uretici, Uretici, StringComparison.OrdinalIgnoreCase)) return s.Oranlar; if (!s.UreticiOranlari.TryGetValue(uretici, out var l)) s.UreticiOranlari[uretici] = l = new List<FyOran>(); return l; }
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
        public static decimal SaticiFiyati(FySatici s, FyUrun u) => Math.Round(ToplamMaliyet(u) * (1 + OranCoz(s.Oranlar, u).Oran / 100m), 2);
        public static decimal MusteriRisk(FyMusteri m, FyUrun u) => m == null ? 0 : m.UrunRisk.TryGetValue(u.Kod, out var r) ? r : m.RiskPayi ?? 0;
        public static decimal MusteriFiyati(FySatici s, FyMusteri m, FyUrun u) => Math.Round(SaticiFiyati(s, u) * (1 + MusteriRisk(m, u) / 100m) * (1 + OranCoz(m.Oranlar, u).Oran / 100m), 2);

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
            var s = ZincirSaticiBul(g?.Satici ?? ""); if (s == null) return "Yetkili satıcı bulunamadı.";
            string uretici = string.IsNullOrWhiteSpace(g.Uretici) ? Uretici : Ureticiler.FirstOrDefault(x => string.Equals(x, g.Uretici.Trim(), StringComparison.OrdinalIgnoreCase));
            if (uretici == null) return "Üretici bulunamadı.";
            if (string.Equals(uretici, s.Ad, StringComparison.OrdinalIgnoreCase)) return "Üretici ile yetkili satıcı aynı şirket olamaz.";
            lock (Kilit) return OranlariUygula(OranListesi(s, uretici), g, "Satıcı", $"{uretici} → {s.Ad}", OrnekVeri.KullaniciAdi(kullaniciKodu), out degisen);
        }
        /// <summary>Maliyet bileşenlerini (toplu ambalaj maliyeti, toplu risk payı, kalem değerleri) günceller; yalnız Yönetici. Her değişiklik günlüğe yazılır.</summary>
        public static string MaliyetKaydet(FyMaliyetKayit g, string kullaniciKodu, out int degisen)
        {
            degisen = 0;
            if (!Yonetici(kullaniciKodu)) return "Maliyet bileşenlerini yalnız Yönetici yetkisi değiştirebilir.";
            if (g == null) return "Geçersiz istek.";
            // önce doğrulama (hiçbir değer değişmeden)
            string A(decimal? v, string alan) => v.HasValue && (v < 0 || v > 1000) ? $"{alan}: ambalaj maliyeti 0 ile 1.000 $ arasında olmalıdır." : null;
            string R(decimal? v, string alan) => v.HasValue && (v < 0 || v > 100) ? $"{alan}: risk payı 0 ile 100 arasında olmalıdır." : null;
            var hata = A(g.GenelAmbalaj, "Toplu") ?? R(g.GenelRisk, "Toplu"); if (hata != null) return hata;
            var plan = new List<(FyUrun U, decimal? Ambalaj, decimal? Risk)>();
            foreach (var x in g.Urunler ?? new List<FyMaliyetGiris>()) { var u = UrunBul(x.Kod ?? ""); if (u == null) return $"Bilinmeyen ürün: {x.Kod}"; hata = A(x.Ambalaj, u.Kod) ?? R(x.Risk, u.Kod); if (hata != null) return hata; plan.Add((u, x.Ambalaj, x.Risk)); }
            string k = OrnekVeri.KullaniciAdi(kullaniciKodu); var z = DateTime.Now; int sayac = 0;
            string M(decimal? v, string birim) => v.HasValue ? v.Value.ToString("0.####") + birim : "";
            void Yaz(string hedef, string alan, string eski, string yeni) { if (eski == yeni) return; Audit.Insert(0, new FyAudit { Zaman = z, Kullanici = k, Katman = "Maliyet", Hedef = hedef, Alan = alan, Eski = eski, Yeni = yeni }); sayac++; }
            lock (Kilit)
            {
                decimal? ga = g.GenelAmbalaj.HasValue ? Math.Round(g.GenelAmbalaj.Value, 4) : (decimal?)null; decimal gr = Math.Round(g.GenelRisk ?? 0, 2);
                string tip = AmbalajTipleri().Any(x => x.Kod == g.AmbalajTipi) ? g.AmbalajTipi : AmbalajTipi;
                Yaz("Tüm Ürünler", "Ambalaj Tipi", AmbalajTipi, tip); AmbalajTipi = tip;
                Yaz("Tüm Ürünler", "Toplu Ambalaj Maliyeti ($)", GenelAmbalajElle.HasValue ? M(GenelAmbalajElle, " $") : "Otomatik", ga.HasValue ? M(ga, " $") : "Otomatik"); GenelAmbalajElle = ga;
                Yaz("Tüm Ürünler", "Toplu Risk Payı %", M(GenelRisk, "%"), M(gr, "%")); GenelRisk = gr;
                foreach (var p in plan)
                {
                    decimal? a = p.Ambalaj.HasValue ? Math.Round(p.Ambalaj.Value, 4) : (decimal?)null, r = p.Risk.HasValue ? Math.Round(p.Risk.Value, 2) : (decimal?)null;
                    Yaz($"{p.U.Kod} {p.U.Ad}", "Ambalaj Maliyeti ($)", M(p.U.AmbalajMaliyeti, " $"), M(a, " $")); p.U.AmbalajMaliyeti = a;
                    Yaz($"{p.U.Kod} {p.U.Ad}", "Risk Payı %", M(p.U.RiskPayi, "%"), M(r, "%")); p.U.RiskPayi = r;
                }
            }
            degisen = sayac; return null;
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
            if (sayac > 0) MaliyetSurum++;
            degisen = sayac; return null;
        }
        // ---------- Genel gider payı (13 Kas 2026) ----------
        // Sabit ve genel gider kalemleri Bütçe Raporu'ndan (Data/ButceOrnek.cs; Tedarik Zinciri · Hammadde grubu hariç — o doğrudan maliyettir) okunur;
        // aylık ÜRETİM RAKAMI (kg) burada girilir (portalda üretim miktarı kaynağı yok). Gider ÷ üretim ÷ kur = ürün başına eklenmesi gereken $ / kg.
        public static readonly Dictionary<string, decimal?[]> Uretimler = new Dictionary<string, decimal?[]>(StringComparer.OrdinalIgnoreCase);   // "şirket|yıl" → 12 ay (kg)
        public static string ButceSirketi(string uretici) => string.Equals(uretici, "Selvi Kimya", StringComparison.OrdinalIgnoreCase) ? "Selvi" : uretici;
        public static object GenelGiderPaketi(string uretici, int yil)
        {
            uretici = Ureticiler.FirstOrDefault(x => string.Equals(x, uretici, StringComparison.OrdinalIgnoreCase)) ?? Uretici;
            if (!ButceOrnek.Yillar.Contains(yil)) yil = ButceOrnek.Yillar.Contains(DateTime.Today.Year) ? DateTime.Today.Year : ButceOrnek.Yillar.Last();
            string sirket = ButceSirketi(uretici);
            var kalemler = ButceOrnek.Kalemler.Where(k => k.Grup != ButceOrnek.Gruplar[1] && ButceOrnek.Uygulanir(k, sirket)).Select(k => { var s = ButceOrnek.Bul(sirket, yil, k.Kod); return new { kod = k.Kod, grup = k.Grup, altGrup = k.AltGrup, ad = k.Ad, butce = s?.Butce, gercek = s?.Gercek }; }).Where(x => x.butce != null).ToList();
            decimal?[] uretim; lock (Kilit) uretim = Uretimler.TryGetValue(sirket + "|" + yil, out var u) ? u.ToArray() : new decimal?[12];
            return new { success = true, uretici, sirket, yil, yillar = ButceOrnek.Yillar, aylar = ButceOrnek.Aylar, kapanmisAy = ButceOrnek.KapanmisAy(yil), kalemler, uretim, kur = Kur.UsdTl, ortMaliyet = Urunler.Count == 0 ? 0 : Math.Round(Urunler.Average(x => ToplamMaliyet(x)), 4) };
        }
        public static string UretimKaydet(FyUretimKayit g, string kullaniciKodu, out int degisen)
        {
            degisen = 0;
            if (!Yonetici(kullaniciKodu)) return "Üretim rakamlarını yalnız Yönetici yetkisi değiştirebilir.";
            if (g == null || g.Aylar == null || g.Aylar.Count != 12) return "Geçersiz istek.";
            var uretici = Ureticiler.FirstOrDefault(x => string.Equals(x, g.Uretici, StringComparison.OrdinalIgnoreCase)); if (uretici == null) return "Üretici bulunamadı.";
            if (!ButceOrnek.Yillar.Contains(g.Yil)) return "Yıl geçersiz.";
            if (g.Aylar.Any(a => a.HasValue && (a < 0 || a > 1_000_000_000m))) return "Üretim miktarı 0'dan küçük olamaz.";
            string k = OrnekVeri.KullaniciAdi(kullaniciKodu), anahtar = ButceSirketi(uretici) + "|" + g.Yil; var z = DateTime.Now; int sayac = 0;
            lock (Kilit)
            {
                if (!Uretimler.TryGetValue(anahtar, out var eski)) Uretimler[anahtar] = eski = new decimal?[12];
                for (int i = 0; i < 12; i++)
                {
                    decimal? y = g.Aylar[i].HasValue ? Math.Round(g.Aylar[i].Value, 0) : (decimal?)null; if (eski[i] == y) continue;
                    Audit.Insert(0, new FyAudit { Zaman = z, Kullanici = k, Katman = "Üretim", Hedef = $"{uretici} · {g.Yil}", Alan = ButceOrnek.Aylar[i] + " Üretimi (kg)", Eski = eski[i]?.ToString("#,0") ?? "", Yeni = y?.ToString("#,0") ?? "" });
                    eski[i] = y; sayac++;
                }
            }
            degisen = sayac; return null;
        }
        public static string MusteriKaydet(FyKayit g, string kullaniciKodu, out int degisen)
        {
            degisen = 0;
            if (!Yonetici(kullaniciKodu)) return "Fiyat ve kâr oranlarını yalnız Yönetici yetkisi değiştirebilir.";
            var s = ZincirSaticiBul(g?.Satici ?? ""); if (s == null) return "Yetkili satıcı bulunamadı.";
            var kart = CariKartOrnek.KartBul(g.CariKodu ?? ""); if (kart == null || kart.KartTipi != "Müşteri") return "Müşteri cari kartı bulunamadı.";
            // risk payı doğrulaması (hiçbir değer değişmeden)
            if (g.Risk.HasValue && (g.Risk < 0 || g.Risk > 100)) return "Risk payı 0 ile 100 arasında olmalıdır.";
            foreach (var x in g.UrunRiskler ?? new List<FyOranGiris>()) { if (UrunBul(x.Anahtar ?? "") == null) return $"Bilinmeyen ürün: {x.Anahtar}"; if (x.Oran.HasValue && (x.Oran < 0 || x.Oran > 100)) return $"{x.Anahtar}: risk payı 0 ile 100 arasında olmalıdır."; }
            lock (Kilit)
            {
                var m = MusteriBul(s.Ad, kart.Kod);
                if (m == null) { m = new FyMusteri { Satici = s.Ad, CariKodu = kart.Kod, Zaman = DateTime.Now, Kullanici = OrnekVeri.KullaniciAdi(kullaniciKodu) }; _musteriler.Add(m); }
                var h = OranlariUygula(m.Oranlar, g, "Müşteri", $"{s.Ad} → {kart.Kod} {kart.KisaAd}", OrnekVeri.KullaniciAdi(kullaniciKodu), out degisen);
                if (h == null)
                {
                    // risk payı (toplu + kalem): farklar günlüğe yazılır
                    string k = OrnekVeri.KullaniciAdi(kullaniciKodu), hedef = $"{s.Ad} → {kart.Kod} {kart.KisaAd}"; var z = DateTime.Now; int rs = 0;
                    string Y(decimal? v) => v.HasValue ? "%" + v.Value.ToString("0.##") : "";
                    void Yaz(string alan, decimal? eski, decimal? yeni) { if (Y(eski) == Y(yeni)) return; Audit.Insert(0, new FyAudit { Zaman = z, Kullanici = k, Katman = "Müşteri", Hedef = hedef, Alan = alan, Eski = Y(eski), Yeni = Y(yeni) }); rs++; }
                    if (g.RiskGonderildi) { decimal? yeni = g.Risk.HasValue ? Math.Round(g.Risk.Value, 2) : (decimal?)null; Yaz("Risk Payı (Toplu)", m.RiskPayi, yeni); m.RiskPayi = yeni; }
                    foreach (var x in g.UrunRiskler ?? new List<FyOranGiris>())
                    {
                        var u = UrunBul(x.Anahtar); decimal? eski = m.UrunRisk.TryGetValue(u.Kod, out var e) ? e : (decimal?)null, yeni = x.Oran.HasValue ? Math.Round(x.Oran.Value, 2) : (decimal?)null;
                        Yaz("Risk Payı · Kalem " + u.Kod + " " + u.Ad, eski, yeni); if (yeni.HasValue) m.UrunRisk[u.Kod] = yeni.Value; else m.UrunRisk.Remove(u.Kod);
                    }
                    degisen += rs;
                }
                if (h == null && degisen > 0) { m.Zaman = DateTime.Now; m.Kullanici = OrnekVeri.KullaniciAdi(kullaniciKodu); }
                if (m.Oranlar.Count == 0 && !m.RiskPayi.HasValue && m.UrunRisk.Count == 0) _musteriler.Remove(m);   // oran ve risk payı kalmadıysa tanım kalkar
                return h;
            }
        }
    }
}
