// ============================================================
// YAPILAN ÖDEMELER — konsolide örnek veri (Yönetim › Özel Analizler › Özel Raporlar)
// Amaç: Grup şirketlerinin yaptığı TÜM ödemeleri tek listede göstermek — Banka (Havale / EFT), Çek, Nakit (Kasa) ve
// Nakit Avans teslimleri — ve "2026 Şirket Bazlı Ödeme Raporu" / "2026 Konsolide Ödeme Raporu" Excel şablonlarını
// (BANKA X/R • ÇEK X/R • NAKİT X/R • aylık şirket detayı; ödeme türü konsolidesi + şirket konsolidesi) beslemek.
// Kaynaklar:
//   • Nakit Avans modülü  : Data/NakitAvansOrnek.cs › Talepler › Odemeler (modülün canlı verisi; yeni teslimler anında görünür)
//   • Aylık Alım Onay     : Data/AlimOnayOrnek.cs › Faturalar (OdemeDurumu = Ödendi olan tedarikçi faturaları → banka ya da çek ödemesi)
//   • ERP Kasa / Banka / Çek bordrosu : bu dosyada üretilen deterministik örnek kayıtlar (yılbaşından bugüne; maaş, vergi / SGK, kira,
//     kredi taksiti, enerji ve hizmet, küçük ve büyük kasa ödemeleri, grup içi ödemeler, vadeli tedarikçi çekleri, USD / EUR ithalat)
// Şirket listesi ve sırası şablondan gelir (10 şirket). Şablona göre BANKA satırı yalnız "tanımlı" şirketlerde bulunur
// (BankaSirketleri); diğer şirketler çek ve nakit ile öder. X / R = ERP aktarım tipi (Cari Ekstre'deki AktarimTipi ile aynı kavram).
// Kayıtlar bellek içindedir; SQL / ERP bağlantısı yoktur. Ekran: Controllers/YapilanOdemelerController.cs + Views/YapilanOdemeler/Index.cshtml
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Banka (Havale / EFT)", "Vade Bekliyor", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Tek bir ödeme hareketi (kaynağı ne olursa olsun aynı yapıda).</summary>
    public class YoOdeme
    {
        public int Id { get; set; }
        public string No { get; set; }                 // Kasa çıkış fiş no (KÇ-…), banka ödeme no (BÖ-…), çek no (ÇK-…)
        public DateTime Tarih { get; set; }            // ödeme / teslim / çek veriliş tarihi
        public string Sirket { get; set; }             // ödemeyi yapan grup şirketi (şablon adı)
        public string Tur { get; set; }                // Banka (Havale / EFT) | Çek | Nakit (Kasa) | Nakit Avans
        public string Kanal { get; set; }              // Banka | Cek | Kasa | Avans (KPI / matris anahtarı)
        public string AktarimTipi { get; set; } = "X"; // X | R — ERP aktarım tipi (şablondaki X/R ayrımı)
        public string KaynakHesap { get; set; }        // kasa adı, banka hesabı ya da çek bilgisi
        public string Banka { get; set; }
        public string Alici { get; set; }
        public string AliciTipi { get; set; }          // Tedarikçi | Personel | Kurum | Banka | Grup Şirketi | Diğer
        public string Kategori { get; set; }
        public decimal Tutar { get; set; }             // işlem para birimindeki tutar
        public string ParaBirimi { get; set; } = "TL";
        public decimal Kur { get; set; } = 1;
        public decimal TutarTl { get; set; }
        public string Belge { get; set; }              // fatura no, avans no, bordro / beyanname
        public DateTime? Vade { get; set; }            // çek vadesi
        public string Durum { get; set; }              // Ödendi | Vade Bekliyor | İptal
        public string Aciklama { get; set; }
        public string Kaynak { get; set; }             // ERP Banka | ERP Kasa | Çek Bordrosu | Nakit Avans Modülü | Aylık Alım Onay / …
        public string Onaylayan { get; set; }
        public string IslemiYapan { get; set; }
        public string Link { get; set; }               // kaynak ekrana derin bağlantı (varsa)
    }

    public static class YapilanOdemelerOrnek
    {
        public static readonly object Kilit = new object();

        // ---------- Şirketler (2026 Şirket Bazlı / Konsolide Ödeme Raporu şablonundaki sıra) ----------
        public static readonly string[] Sirketler = { "Uras Kimya", "Uras Baskı", "Alv Kimya", "Avrupa Paper", "DRN", "Selvi", "Alv Filo", "Uras Holding", "Uras Power", "URS Makine" };
        /// <summary>Şablonda BANKA satırı bulunan ("tanımlı") şirketler: banka ödemeleri yalnız bunlarda izlenir; diğerleri çek ve nakit ile öder.</summary>
        public static readonly string[] BankaSirketleri = { "Uras Baskı", "DRN", "Selvi", "Uras Power" };
        /// <summary>Diğer modüllerdeki şirket adlarını şablon adına çevirir; eşleşmeyen ad olduğu gibi kalır ve tablolarda ek satır olarak görünür.</summary>
        private static readonly Dictionary<string, string> SirketEslestirme = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["URS Makina"] = "URS Makine", ["Uras Kimya A.Ş."] = "Uras Kimya", ["Selvi Kimya"] = "Selvi", ["ALV Kimya"] = "Alv Kimya", ["ALV Filo"] = "Alv Filo", ["Uras Holdİng"] = "Uras Holding"
        };
        public static string SirketAdi(string ad) => string.IsNullOrWhiteSpace(ad) ? "—" : (SirketEslestirme.TryGetValue(ad.Trim(), out var e) ? e : ad.Trim());

        // ---------- Seçim listeleri ----------
        /// <summary>Kanal anahtarı → ekran adı.</summary>
        public static readonly (string Kanal, string Ad)[] Turler =
        {
            ("Banka", "Banka (Havale / EFT)"), ("Cek", "Çek"), ("Kasa", "Nakit (Kasa)"), ("Avans", "Nakit Avans")
        };
        public static readonly string[] AktarimTipleri = { "X", "R" };
        public static readonly string[] Kategoriler = { "Tedarikçi Ödemesi", "Personel Avansı", "Maaş ve Ücret", "Vergi ve SGK", "Kira", "Kredi Taksiti", "Enerji ve Hizmet", "Genel Gider", "Grup İçi Ödeme" };
        public static readonly string[] AliciTipleri = { "Tedarikçi", "Personel", "Kurum", "Banka", "Grup Şirketi", "Diğer" };
        public static readonly string[] Durumlar = { "Ödendi", "Vade Bekliyor", "İptal" };
        public static readonly string[] Kaynaklar = { "ERP Banka", "ERP Kasa", "Çek Bordrosu", "Nakit Avans Modülü", "Aylık Alım Onay / ERP Banka", "Aylık Alım Onay / Çek Bordrosu" };
        public static readonly Dictionary<string, decimal> Kurlar = new Dictionary<string, decimal> { ["TL"] = 1m, ["USD"] = 41.35m, ["EUR"] = 48.70m, ["GBP"] = 55.80m, ["AED"] = 11.26m };
        public static readonly string[] Bankalar = { "Garanti BBVA", "İş Bankası", "Yapı Kredi", "Akbank", "Ziraat Bankası", "QNB", "Halkbank" };

        private static string TurAdi(string kanal) => Turler.First(t => t.Kanal == kanal).Ad;
        private static decimal KurBul(string pb) => Kurlar.TryGetValue(pb ?? "TL", out var k) ? k : 1m;

        // ---------- Konsolide liste ----------
        /// <summary>Tüm kaynakların birleşimi, tarihe göre yeniden eskiye. Avans ve fatura kaynakları her çağrıda canlı okunur; ERP örnekleri bir kez üretilir.</summary>
        public static List<YoOdeme> Liste()
        {
            var l = new List<YoOdeme>();
            l.AddRange(ErpOrnekleri.Value);
            l.AddRange(AvansTeslimleri());
            l.AddRange(FaturaOdemeleri());
            int id = 0;
            foreach (var o in l.OrderByDescending(x => x.Tarih).ThenBy(x => x.Sirket).ThenBy(x => x.No)) o.Id = ++id;
            return l.OrderBy(x => x.Id).ToList();
        }

        /// <summary>Nakit Avans modülündeki teslimler (Durum ≠ İptal). Kasa teslimi kasadan, havale teslimi bankadan çıkar; tür yine "Nakit Avans"dır.</summary>
        private static IEnumerable<YoOdeme> AvansTeslimleri()
        {
            var l = new List<YoOdeme>();
            lock (NakitAvansOrnek.Kilit)
            {
                foreach (var a in NakitAvansOrnek.Talepler)
                    foreach (var o in a.Odemeler.Where(x => x.Durum != "İptal"))
                    {
                        bool havale = o.TeslimSekli == "Havale";
                        decimal kur = KurBul(o.ParaBirimi);
                        l.Add(new YoOdeme
                        {
                            No = o.FisNo, Tarih = o.Tarih.Date, Sirket = SirketAdi(a.Sirket), Tur = TurAdi("Avans"), Kanal = "Avans", AktarimTipi = "X",
                            KaynakHesap = havale ? "Banka · Havale" : o.Kasa, Banka = havale ? Bankalar[0] : "",
                            Alici = a.Kullanan?.AdSoyad ?? "—", AliciTipi = "Personel", Kategori = "Personel Avansı",
                            Tutar = o.Tutar, ParaBirimi = o.ParaBirimi ?? "TL", Kur = kur, TutarTl = Math.Round(o.Tutar * kur, 2),
                            Belge = a.No, Durum = "Ödendi", Aciklama = $"{a.Konu} · {a.Kullanan?.Departman} · {a.HarcamaKategorisi}",
                            Kaynak = "Nakit Avans Modülü", Onaylayan = a.FinansOnayi ?? a.Yonetici ?? "—", IslemiYapan = o.TeslimEden,
                            Link = "/IkTalep/Avans?no=" + a.No
                        });
                    }
            }
            return l;
        }

        /// <summary>Aylık Alım Onay ekranındaki ödenmiş tedarikçi faturaları: banka izlenen şirketlerde beş faturadan ikisi çekle, diğer şirketlerde tamamı çekle ödenmiş sayılır.</summary>
        private static IEnumerable<YoOdeme> FaturaOdemeleri()
        {
            var bugun = DateTime.Today; var l = new List<YoOdeme>();
            foreach (var f in AlimOnayOrnek.Faturalar.Where(x => x.OdemeDurumu == "Ödendi"))
            {
                string sirket = SirketAdi(f.FaturaSirketi);
                bool cek = !BankaSirketleri.Contains(sirket) || f.Id % 5 < 2;
                string xr = f.Id % 4 == 0 ? "R" : "X";
                decimal kur = f.ParaBirimi == "TL" ? 1m : (f.Kur > 0 ? f.Kur : KurBul(f.ParaBirimi));
                decimal tutar = f.ParaBirimi == "TL" ? f.Brut : Math.Round(f.Brut / kur, 2);
                string banka = Bankalar[f.Id % Bankalar.Length];
                if (cek)
                {
                    var veril = f.Tarih.AddDays(5) > bugun ? bugun : f.Tarih.AddDays(5);
                    l.Add(new YoOdeme
                    {
                        No = "ÇK-" + (1000000 + (f.Id * 7919) % 8999999), Tarih = veril, Sirket = sirket, Tur = TurAdi("Cek"), Kanal = "Cek", AktarimTipi = xr,
                        KaynakHesap = $"{banka} · Çek", Banka = banka, Alici = f.Tedarikci, AliciTipi = "Tedarikçi", Kategori = "Tedarikçi Ödemesi",
                        Tutar = tutar, ParaBirimi = f.ParaBirimi, Kur = kur, TutarTl = f.Brut, Belge = f.FaturaNo, Vade = f.Vade,
                        Durum = f.Vade <= bugun ? "Ödendi" : "Vade Bekliyor", Aciklama = $"{f.Kategori} · sipariş {f.SiparisNo} · fiş {f.MuhasebeFisNo}",
                        Kaynak = "Aylık Alım Onay / Çek Bordrosu", Onaylayan = f.Satinalmaci, IslemiYapan = "Finans", Link = "/AlimOnay"
                    });
                }
                else
                {
                    var t = f.Vade > bugun ? bugun : f.Vade;
                    l.Add(new YoOdeme
                    {
                        No = "BÖ-" + t.Year + "-" + (4000 + f.Id).ToString("00000"), Tarih = t, Sirket = sirket, Tur = TurAdi("Banka"), Kanal = "Banka", AktarimTipi = xr,
                        KaynakHesap = $"{banka} · {(f.ParaBirimi == "TL" ? "TL" : f.ParaBirimi)} Hesabı", Banka = banka, Alici = f.Tedarikci, AliciTipi = "Tedarikçi", Kategori = "Tedarikçi Ödemesi",
                        Tutar = tutar, ParaBirimi = f.ParaBirimi, Kur = kur, TutarTl = f.Brut, Belge = f.FaturaNo, Durum = "Ödendi",
                        Aciklama = $"{f.Kategori} · sipariş {f.SiparisNo} · fiş {f.MuhasebeFisNo}", Kaynak = "Aylık Alım Onay / ERP Banka",
                        Onaylayan = f.Satinalmaci, IslemiYapan = "Finans", Link = "/AlimOnay"
                    });
                }
            }
            return l;
        }

        // ---------- ERP kasa / banka / çek örnekleri (deterministik; yılbaşından bugüne) ----------
        private static readonly Lazy<List<YoOdeme>> ErpOrnekleri = new Lazy<List<YoOdeme>>(Uret);

        /// <summary>Banka izlenen şirketlerin aylık net maaş tabanı (vergi / SGK / enerji bu tabana oranlanır).</summary>
        private static readonly Dictionary<string, decimal> MaasTabani = new Dictionary<string, decimal> { ["Uras Baskı"] = 1_950_000m, ["DRN"] = 1_120_000m, ["Selvi"] = 1_640_000m, ["Uras Power"] = 540_000m };
        /// <summary>Çek / nakit ile ödeyen şirketlerin hacim çarpanı.</summary>
        private static readonly Dictionary<string, decimal> HacimCarpani = new Dictionary<string, decimal> { ["Uras Kimya"] = 1.0m, ["Alv Kimya"] = 0.45m, ["Avrupa Paper"] = 0.40m, ["Alv Filo"] = 0.20m, ["Uras Holding"] = 0.35m, ["URS Makine"] = 0.25m };
        private static readonly string[] CekTedarikcileri = { "Petkim Petrokimya Holding A.Ş.", "Brenntag Kimya Tic. Ltd. Şti.", "Korozo Ambalaj San. ve Tic. A.Ş.", "Sarten Ambalaj San. ve Tic. A.Ş.", "Öz Fason Baskı ve Konfeksiyon Ltd. Şti.", "Karadeniz Ambalaj San. ve Tic. Ltd. Şti.", "Dost Kimya San. Ltd. Şti.", "Anadolu Endüstriyel Temizlik Hiz. Ltd. Şti." };
        private static readonly (string Alici, string Kategori, string Tip, int Min, int Max)[] KasaGiderleri =
        {
            ("Yurtiçi Kargo", "Genel Gider", "Tedarikçi", 350, 2_400), ("Öğle Yemeği (Personel)", "Genel Gider", "Diğer", 1_200, 6_500), ("Noter Ücreti", "Genel Gider", "Kurum", 900, 4_800),
            ("Yakıt (Şirket Aracı)", "Genel Gider", "Tedarikçi", 1_500, 7_500), ("Temsil ve Ağırlama", "Genel Gider", "Diğer", 2_000, 18_000), ("Kırtasiye", "Genel Gider", "Tedarikçi", 400, 3_200),
            ("Taksi / Ulaşım", "Genel Gider", "Diğer", 300, 1_900), ("Küçük Onarım (Usta)", "Genel Gider", "Diğer", 2_500, 14_000)
        };
        /// <summary>Banka izlenmeyen şirketlerde kasadan yapılan büyük ödemeler.</summary>
        private static readonly (string Alici, string Kategori, string Tip, int Min, int Max, string Belge)[] KasaBuyuk =
        {
            ("Personel Ücret Ödemesi", "Maaş ve Ücret", "Personel", 180_000, 900_000, "Ücret Bordrosu"), ("Tedarikçi Nakit Ödemesi", "Tedarikçi Ödemesi", "Tedarikçi", 25_000, 220_000, "Tedarikçi Faturası"),
            ("Gebze OSB Kiralama A.Ş.", "Kira", "Diğer", 60_000, 260_000, "Kira Makbuzu"), ("Vergi Dairesi Veznesi", "Vergi ve SGK", "Kurum", 40_000, 420_000, "Tahakkuk Fişi"),
            ("Enerjisa", "Enerji ve Hizmet", "Tedarikçi", 30_000, 260_000, "Elektrik Faturası"), ("Uras Holding", "Grup İçi Ödeme", "Grup Şirketi", 50_000, 300_000, "Yönetim Hizmet Faturası")
        };
        private static readonly (string Alici, string Aciklama, int Min, int Max)[] EnerjiHizmet =
        {
            ("Enerjisa", "Elektrik Faturası", 180_000, 1_400_000), ("İGDAŞ", "Doğalgaz Faturası", 60_000, 520_000), ("Türk Telekom", "Telefon / İnternet", 9_000, 42_000), ("İSKİ", "Su Faturası", 6_000, 38_000)
        };

        private static List<YoOdeme> Uret()
        {
            var rnd = new Random(20260922);
            var bugun = DateTime.Today; var l = new List<YoOdeme>();
            int bo = 7100, kc = 3100, ck = 4200000;
            DateTime Gun(DateTime ay, int g) => new DateTime(ay.Year, ay.Month, Math.Min(g, DateTime.DaysInMonth(ay.Year, ay.Month)));
            decimal Oyna(decimal v, int yuzde) => Math.Round(v * (1 + (rnd.Next(-yuzde, yuzde + 1) / 100m)), 2);
            string Xr() => rnd.Next(100) < 75 ? "X" : "R";   // ERP aktarım tipi: çoğunluk X
            void Ekle(DateTime t, string sirket, string kanal, string hesap, string banka, string alici, string tip, string kategori, decimal tutar, string belge, string aciklama, DateTime? vade = null, string durum = "Ödendi", string onaylayan = "Mali İşler Direktörü", string yapan = "Finans", string xr = null)
            {
                if (t > bugun) return;
                string no = kanal == "Kasa" ? "KÇ-" + (kc++) : kanal == "Cek" ? "ÇK-" + (ck += rnd.Next(11, 97)) : "BÖ-" + t.Year + "-" + (bo++).ToString("00000");
                l.Add(new YoOdeme
                {
                    No = no, Tarih = t, Sirket = sirket, Tur = TurAdi(kanal), Kanal = kanal, AktarimTipi = xr ?? Xr(), KaynakHesap = hesap, Banka = banka ?? "", Alici = alici, AliciTipi = tip, Kategori = kategori,
                    Tutar = tutar, ParaBirimi = "TL", Kur = 1, TutarTl = tutar, Belge = belge, Vade = vade, Durum = durum, Aciklama = aciklama,
                    Kaynak = kanal == "Kasa" ? "ERP Kasa" : kanal == "Cek" ? "Çek Bordrosu" : "ERP Banka", Onaylayan = onaylayan, IslemiYapan = yapan
                });
            }
            // Döviz ödemesi: işlem günü kuru sabit kur etrafında küçük sapma; TL karşılığı o kurla hesaplanır
            void DovizEkle(DateTime t, string sirket, string banka, string alici, string pb, decimal tutar, string belge, string aciklama)
            {
                if (t > bugun) return;
                decimal kur = Math.Round(KurBul(pb) * (1 + rnd.Next(-3, 4) / 100m), 4);
                l.Add(new YoOdeme
                {
                    No = "BÖ-" + t.Year + "-" + (bo++).ToString("00000"), Tarih = t, Sirket = sirket, Tur = TurAdi("Banka"), Kanal = "Banka", AktarimTipi = "X", KaynakHesap = $"{banka} · {pb} Hesabı", Banka = banka,
                    Alici = alici, AliciTipi = "Tedarikçi", Kategori = "Tedarikçi Ödemesi", Tutar = tutar, ParaBirimi = pb, Kur = kur, TutarTl = Math.Round(tutar * kur, 2), Belge = belge, Durum = "Ödendi",
                    Aciklama = aciklama, Kaynak = "ERP Banka", Onaylayan = "Mali İşler Direktörü", IslemiYapan = "Finans"
                });
            }
            var ithalat = new[] { ("BASF SE", "EUR", 38_000, 210_000, "Binder Hammaddesi"), ("Nordic Paper Sublimation AB", "EUR", 22_000, 95_000, "Süblime Kağıt"), ("Dow Chemical International", "USD", 30_000, 180_000, "Akrilik Monomer"), ("Kemira Oyj", "EUR", 15_000, 70_000, "Kağıt Kimyasalı"), ("Huntsman Textile Effects", "USD", 18_000, 90_000, "Tekstil Yardımcı Kimyasalı") };

            // Yılbaşından bugüne her ay, her şirket
            for (int k = bugun.Month - 1; k >= 0; k--)
            {
                var ay = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-k);
                string donem = ay.ToString("MM.yyyy");
                foreach (var s in Sirketler)
                {
                    bool bankali = BankaSirketleri.Contains(s);
                    string anaBanka = Bankalar[Array.IndexOf(Sirketler, s) % Bankalar.Length];
                    string hesap = $"{anaBanka} · TL Hesabı";
                    if (bankali)
                    {
                        decimal maas = MaasTabani[s];
                        // Maaş ve ücret (ayın 5'i), vergi (26'sı), SGK (ay sonu) — resmi kayıtlar X
                        Ekle(Gun(ay, 5), s, "Banka", hesap, anaBanka, "Personel Bordrosu", "Personel", "Maaş ve Ücret", Oyna(maas, 4), $"Bordro {donem}", $"{donem} maaş ve ücret ödemesi (net)", xr: "X");
                        Ekle(Gun(ay, 26), s, "Banka", hesap, anaBanka, "Gelir İdaresi Başkanlığı", "Kurum", "Vergi ve SGK", Oyna(maas * 0.21m, 15), $"KDV / Muhtasar {donem}", "KDV ve muhtasar beyannamesi tahakkuku", xr: "X");
                        Ekle(Gun(ay, DateTime.DaysInMonth(ay.Year, ay.Month)), s, "Banka", hesap, anaBanka, "Sosyal Güvenlik Kurumu", "Kurum", "Vergi ve SGK", Oyna(maas * 0.34m, 6), $"SGK Prim {donem}", "Aylık SGK primi ve işsizlik sigortası", xr: "X");
                        // Kira ve kredi taksiti
                        if (s == "Selvi" || s == "Uras Baskı")
                            Ekle(Gun(ay, 1), s, "Banka", hesap, anaBanka, "Gebze OSB Kiralama A.Ş.", "Diğer", "Kira", Oyna(265_000m, 2), $"Kira {donem}", "Aylık kira ödemesi (stopaj dahil)");
                        if (s == "Selvi") Ekle(Gun(ay, 15), s, "Banka", hesap, "Garanti BBVA", "Garanti BBVA", "Banka", "Kredi Taksiti", 640_000m, "Makine Kredisi 2024-11", "Makine yatırım kredisi taksiti", xr: "X");
                        if (s == "Uras Power") Ekle(Gun(ay, 20), s, "Banka", hesap, "Ziraat Bankası", "Ziraat Bankası", "Banka", "Kredi Taksiti", 980_000m, "Yatırım Kredisi 2025-03", "Enerji yatırım kredisi taksiti", xr: "X");
                        if (s == "DRN") Ekle(Gun(ay, 12), s, "Banka", hesap, "Akbank", "Akbank", "Banka", "Kredi Taksiti", 410_000m, "İşletme Kredisi 2025-09", "İşletme kredisi aylık taksit", xr: "X");
                        // Enerji ve hizmet (ayın 10'u civarı)
                        foreach (var e in EnerjiHizmet)
                            if (rnd.Next(10) < 8) Ekle(Gun(ay, 10 + rnd.Next(0, 4)), s, "Banka", hesap, anaBanka, e.Alici, "Tedarikçi", "Enerji ve Hizmet", Oyna(rnd.Next(e.Min, e.Max) * (maas / 1_500_000m), 3), $"{e.Aciklama} {donem}", e.Aciklama);
                        // Grup içi: holdinge yönetim hizmet bedeli
                        if (rnd.Next(10) < 7)
                            Ekle(Gun(ay, 8), s, "Banka", hesap, anaBanka, "Uras Holding", "Grup Şirketi", "Grup İçi Ödeme", Oyna(maas * 0.06m, 10), $"Yönetim Hizmet Faturası {donem}", "Holding yönetim hizmet bedeli (grup içi)");
                        // Döviz: ithalat tedarikçilerine banka ödemeleri (USD / EUR) — üretim şirketleri
                        if (s != "Uras Power")
                        {
                            int d = rnd.Next(1, 3);
                            for (int i = 0; i < d; i++)
                            {
                                var it = ithalat[rnd.Next(ithalat.Length)];
                                DovizEkle(Gun(ay, rnd.Next(3, 27)), s, anaBanka, it.Item1, it.Item2, rnd.Next(it.Item3, it.Item4) + rnd.Next(0, 100) / 100m, $"İthalat Faturası {it.Item2}-{ay:yyyyMM}-{rnd.Next(100, 999)}", $"{it.Item5} ithalat ödemesi (akreditif / mal mukabili)");
                            }
                        }
                    }
                    else
                    {
                        // Banka izlenmeyen şirketler: büyük ödemeler kasadan (nakit) ve çekle
                        decimal carpan = HacimCarpani.TryGetValue(s, out var hc) ? hc : 0.3m;
                        int n = rnd.Next(2, 5);
                        for (int i = 0; i < n; i++)
                        {
                            var g = KasaBuyuk[rnd.Next(KasaBuyuk.Length)];
                            if (g.Alici == "Uras Holding" && s == "Uras Holding") continue;
                            Ekle(Gun(ay, rnd.Next(1, 29)), s, "Kasa", "KS01 - Merkez TL Kasası", null, g.Alici, g.Tip, g.Kategori, Math.Round(rnd.Next(g.Min, g.Max) * carpan + rnd.Next(0, 100) / 100m, 2), $"{g.Belge} {donem}", "Kasa çıkışı — " + g.Alici, null, "Ödendi", "Muhasebe Şefi", "Kasa Sorumlusu", g.Kategori == "Maaş ve Ücret" || g.Kategori == "Vergi ve SGK" ? "X" : null);
                        }
                    }
                    // Kasa: küçük giderler (ayda 3–6) — tüm şirketler
                    int kn = rnd.Next(3, 7);
                    for (int i = 0; i < kn; i++)
                    {
                        var g = KasaGiderleri[rnd.Next(KasaGiderleri.Length)];
                        string kasa = rnd.Next(4) == 0 ? "KS02 - Fabrika Kasası" : "KS01 - Merkez TL Kasası";
                        bool iptal = rnd.Next(40) == 0;
                        Ekle(Gun(ay, rnd.Next(1, 29)), s, "Kasa", kasa, null, g.Alici, g.Tip, g.Kategori, rnd.Next(g.Min, g.Max) + rnd.Next(0, 100) / 100m, "Kasa Fişi", iptal ? "Mükerrer giriş; fiş iptal edildi" : "Kasa çıkışı — " + g.Alici, null, iptal ? "İptal" : "Ödendi", "Muhasebe Şefi", "Kasa Sorumlusu");
                    }
                    // Çek: tedarikçilere verilen vadeli çekler; banka izlenmeyen şirketlerde daha çok ve daha büyük
                    int c = bankali ? rnd.Next(1, 4) : rnd.Next(3, 7);
                    decimal cekCarpan = bankali ? 1m : Math.Max(0.5m, (HacimCarpani.TryGetValue(s, out var hc2) ? hc2 : 0.3m) * 2.2m);
                    for (int i = 0; i < c; i++)
                    {
                        var t = Gun(ay, rnd.Next(2, 28)); if (t > bugun) continue;
                        var vade = t.AddDays(new[] { 60, 90, 120 }[rnd.Next(3)]);
                        string banka = Bankalar[rnd.Next(Bankalar.Length)];
                        Ekle(t, s, "Cek", $"{banka} · Çek", banka, CekTedarikcileri[rnd.Next(CekTedarikcileri.Length)], "Tedarikçi", "Tedarikçi Ödemesi", Math.Round((rnd.Next(45, 900) * 1000m + rnd.Next(0, 999)) * cekCarpan, 2), "Fatura URS" + t.Year + t.Month.ToString("00") + rnd.Next(10000, 99999).ToString("00000"), $"Vadeli çek · vade {vade:dd.MM.yyyy}", vade, vade <= bugun ? "Ödendi" : "Vade Bekliyor");
                    }
                }
            }
            return l;
        }
    }
}
