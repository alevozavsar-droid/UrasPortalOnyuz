// ============================================================
// SATIŞ SİPARİŞİ — Yurtiçi (/Rapor94) ve Yurtdışı / İhracat (/Rapor94/Ihracat) — örnek veri ve kurallar
// Satıcı şirket Selvi Kimya'dır; satış URAS, ALV KİMYA, INKNOVATORS markasıyla ya da SELVİ etiketiyle yapılabilir: marka basitçe seçilir,
// belgeler (Proforma Invoice, Commercial Invoice, Packing List; yurtiçinde Sipariş Formu / Proforma Fatura) seçilen markanın adı, ünvanı ve
// adresiyle TEK TUŞLA üretilir (Data/DisaAktar.cs). Satışçılardan gelen SİPARİŞ TALEPLERİ bu ekrana düşer (Durum = Talep) ve burada siparişe
// dönüştürülür. Her siparişte YÜKLEME TARİHİ ve SAATİ (yükleme yeri, nakliye) belirtilir; satışçılar süreci (Talep → Sipariş → Hazırlanıyor →
// Yüklemeye Hazır → Yüklendi → Faturalandı) takip eder. Fiyatlar Muhataplar İçin Özel Fiyatlar'dan (Data/OzelFiyatOrnek.cs; yoksa Liste Fiyatı),
// ürünler Fiyat Yönetimi'nden (Data/FiyatOrnek.cs), müşteriler cari kartlardan (Data/CariKartOrnek.cs; Bölge = Yurtiçi / Yurtdışı).
// Ekran: Controllers/Rapor94Controller.cs + Views/Rapor94/Index.cshtml + wwwroot/js/satis-siparis.js
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Yüklemeye Hazır", "Mal Mukabili", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Satış markası: belgelerde görünecek ad, ünvan, adres, iletişim ve banka bilgisi.</summary>
    public class SsMarka { public string Kod { get; set; } public string Ad { get; set; } public string BelgeAdi { get; set; } public string Slogan { get; set; } public string Unvan { get; set; } public string Adres { get; set; } public string Eposta { get; set; } public string Web { get; set; } public string Tel { get; set; } public string Banka { get; set; } public string Renk { get; set; } }
    public class SsKalem { public int Sira { get; set; } public string UrunKod { get; set; } public string Urun { get; set; } public string Grup { get; set; } public string HsKodu { get; set; } public decimal Miktar { get; set; } public string Birim { get; set; } = "kg"; public decimal KoliKg { get; set; } = 25; public int Koli => KoliKg > 0 ? (int)Math.Ceiling(Miktar / KoliKg) : 0; public decimal NetKg => Miktar; public decimal BrutKg => Math.Round(Miktar * 1.04m + Koli * 0.4m, 1); public decimal BirimFiyat { get; set; } public decimal Tutar => Math.Round(Miktar * BirimFiyat, 2); public string Not { get; set; } = ""; }
    public class SsHareket { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }
    public class SsBelge { public string Tur { get; set; } public string No { get; set; } public DateTime Tarih { get; set; } public string Kullanici { get; set; } public string Marka { get; set; } }

    public class SsSiparis
    {
        public int Id { get; set; }
        public string No { get; set; }                  // SS-2026-0001 (yurtiçi) · İHR-2026-0001 (yurtdışı); talepte boş
        public string TalepNo { get; set; }             // ST-2026-0001 (satışçı talebi)
        public DateTime Tarih { get; set; }
        public string Bolge { get; set; }               // Yurtiçi | Yurtdışı
        public string Satici { get; set; } = SatisSiparisOrnek.Satici;
        public string Marka { get; set; }               // SsMarka.Kod
        public string CariKodu { get; set; } public string Cari { get; set; } public string Ulke { get; set; } public string Sehir { get; set; }
        public string Temsilci { get; set; }
        public string Kaynak { get; set; } = "Elle";   // Satışçı Talebi | Teklif | Elle
        public string TeklifNo { get; set; } = ""; public string MusteriSiparisNo { get; set; } = "";
        public string ParaBirimi { get; set; } = "USD"; public decimal Kur { get; set; } = 1;
        public List<SsKalem> Kalemler { get; set; } = new List<SsKalem>();
        public decimal Tutar => Kalemler.Sum(k => k.Tutar); public decimal TutarTl => Math.Round(Tutar * Kur, 2); public decimal ToplamKg => Kalemler.Sum(k => k.Miktar); public int ToplamKoli => Kalemler.Sum(k => k.Koli); public decimal BrutKg => Kalemler.Sum(k => k.BrutKg); public int Palet => (int)Math.Ceiling(ToplamKoli / 40m);
        public string OdemeSekli { get; set; } public string TeslimSekli { get; set; } public string SevkAdresi { get; set; } = "";
        public DateTime? YuklemeTarihi { get; set; } public string YuklemeSaati { get; set; } = ""; public string YuklemeYeri { get; set; } = ""; public string Nakliye { get; set; } = ""; public string NakliyeFirmasi { get; set; } = ""; public string YuklemeNotu { get; set; } = "";
        public string Durum { get; set; } = "Talep";    // Talep | Sipariş | Hazırlanıyor | Yüklemeye Hazır | Yüklendi | Faturalandı | Reddedildi | İptal
        public DateTime? SiparisTarihi { get; set; } public DateTime? YuklendiTarihi { get; set; } public string FaturaNo { get; set; } = ""; public DateTime? FaturaTarihi { get; set; }
        public List<SsBelge> Belgeler { get; set; } = new List<SsBelge>();
        public List<SsHareket> Hareketler { get; set; } = new List<SsHareket>();
        public string Olusturan { get; set; } public string Not { get; set; } = "";
        public bool Talep => Durum == "Talep";
        public bool Acik => Durum == "Sipariş" || Durum == "Hazırlanıyor" || Durum == "Yüklemeye Hazır";
        public bool Gecikti => Acik && YuklemeTarihi != null && YuklemeTarihi.Value.Date < DateTime.Today;
        public int Asama => Durum == "Talep" ? 0 : Durum == "Sipariş" ? 1 : Durum == "Hazırlanıyor" ? 2 : Durum == "Yüklemeye Hazır" ? 3 : Durum == "Yüklendi" ? 4 : Durum == "Faturalandı" ? 5 : -1;
    }

    public class SsKalemIstek { public string UrunKod { get; set; } public string Urun { get; set; } public decimal Miktar { get; set; } public decimal? BirimFiyat { get; set; } public decimal? KoliKg { get; set; } public string Not { get; set; } }
    public class SsKayitIstek { public string No { get; set; } public string Bolge { get; set; } public string Marka { get; set; } public string CariKodu { get; set; } public string Temsilci { get; set; } public string ParaBirimi { get; set; } public string OdemeSekli { get; set; } public string TeslimSekli { get; set; } public string SevkAdresi { get; set; } public string MusteriSiparisNo { get; set; } public string TeklifNo { get; set; } public string YuklemeTarihi { get; set; } public string YuklemeSaati { get; set; } public string YuklemeYeri { get; set; } public string Nakliye { get; set; } public string NakliyeFirmasi { get; set; } public string YuklemeNotu { get; set; } public string Not { get; set; } public bool TalepOlarak { get; set; } public List<SsKalemIstek> Kalemler { get; set; } = new List<SsKalemIstek>(); }
    public class SsYuklemeIstek { public string No { get; set; } public string YuklemeTarihi { get; set; } public string YuklemeSaati { get; set; } public string YuklemeYeri { get; set; } public string Nakliye { get; set; } public string NakliyeFirmasi { get; set; } public string YuklemeNotu { get; set; } }
    public class SsDurumIstek { public string No { get; set; } public string Durum { get; set; } public string Not { get; set; } public string FaturaNo { get; set; } }
    public class SsMarkaIstek { public string No { get; set; } public string Marka { get; set; } }

    public static class SatisSiparisOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public const string Satici = "Selvi Kimya";
        public static readonly string[] Bolgeler = { "Yurtiçi", "Yurtdışı" };
        public static readonly string[] Durumlar = { "Talep", "Sipariş", "Hazırlanıyor", "Yüklemeye Hazır", "Yüklendi", "Faturalandı", "Reddedildi", "İptal" };
        public static readonly string[] Asamalar = { "Talep", "Sipariş", "Hazırlanıyor", "Yüklemeye Hazır", "Yüklendi", "Faturalandı" };
        public static readonly string[] OdemeYurtici = { "Peşin", "Vade 30 Gün", "Vade 60 Gün", "Vade 90 Gün", "Çek (Vadeli)" };
        public static readonly string[] OdemeYurtdisi = { "Peşin (T/T Advance)", "Mal Mukabili (CAD)", "Vesaik Mukabili", "Akreditif (L/C)", "Vade 60 Gün (T/T)" };
        public static readonly string[] TeslimYurtici = { "Müşteri Adresine Teslim", "Depodan Teslim (EXW)" };
        public static readonly string[] TeslimYurtdisi = { "EXW Çorlu", "FOB İstanbul", "CFR", "CIF", "DAP" };
        public static readonly string[] YuklemeYerleri = { "Çorlu Depo", "Velimeşe Fabrika", "Çerkezköy Depo" };
        public static readonly string[] NakliyeYurtici = { "Kamyon", "Tır", "Kargo" };
        public static readonly string[] NakliyeYurtdisi = { "Konteyner 20'", "Konteyner 40'", "Tır (Karayolu)", "Hava Kargo" };
        public static readonly string[] NakliyeFirmalari = { "Ekol Lojistik", "Mars Logistics", "Arkas", "MSC", "DHL Freight", "Netlog", "Müşteri Nakliyesi" };
        public static readonly List<SsMarka> Markalar = new List<SsMarka>
        {
            new SsMarka { Kod = "uras", Ad = "Uras", BelgeAdi = "URAS", Slogan = "Textile Printing Chemicals", Unvan = "Uras Kimya San. ve Tic. A.Ş.", Adres = "Velimeşe OSB Mah. 105. Sok. No:3, Ergene / Tekirdağ, Türkiye", Eposta = "export@uraskimya.com", Web = "www.uraskimya.com", Tel = "+90 282 674 50 00", Banka = "Garanti BBVA · USD IBAN TR12 0006 2000 0000 0012 3456 78 · SWIFT TGBATRIS", Renk = "#1d4ed8" },
            new SsMarka { Kod = "alv", Ad = "Alv Kimya", BelgeAdi = "ALV KİMYA", Slogan = "Binders & Textile Auxiliaries", Unvan = "Alv Kimya San. ve Tic. A.Ş.", Adres = "Çerkezköy OSB, Kapaklı / Tekirdağ, Türkiye", Eposta = "sales@alvkimya.com", Web = "www.alvkimya.com", Tel = "+90 282 758 10 00", Banka = "İş Bankası · USD IBAN TR33 0006 4000 0020 0001 2345 67 · SWIFT ISBKTRIS", Renk = "#0f766e" },
            new SsMarka { Kod = "inknovators", Ad = "Inknovators", BelgeAdi = "INKNOVATORS", Slogan = "Digital Textile Inks", Unvan = "Inknovators — a brand of Selvi Kimya San. ve Tic. Ltd. Şti.", Adres = "Çorlu / Tekirdağ, Türkiye", Eposta = "hello@inknovators.com", Web = "www.inknovators.com", Tel = "+90 282 651 20 00", Banka = "Yapı Kredi · USD IBAN TR45 0006 7010 0000 0098 7654 32 · SWIFT YAPITRIS", Renk = "#7c3aed" },
            new SsMarka { Kod = "selvi", Ad = "Selvi", BelgeAdi = "SELVİ KİMYA", Slogan = "Chemicals & Sublimation Paper", Unvan = "Selvi Kimya San. ve Tic. Ltd. Şti.", Adres = "Çorlu / Tekirdağ, Türkiye", Eposta = "info@selvikimya.com", Web = "www.selvikimya.com", Tel = "+90 282 651 20 00", Banka = "Yapı Kredi · USD IBAN TR45 0006 7010 0000 0098 7654 32 · SWIFT YAPITRIS", Renk = "#b45309" }
        };
        public static SsMarka MarkaBul(string kod) => Markalar.FirstOrDefault(m => string.Equals(m.Kod, kod, StringComparison.OrdinalIgnoreCase)) ?? Markalar[3];
        public static string HsKodu(string grup) => (grup ?? "").StartsWith("Boya") ? "3204.17.00" : (grup ?? "").StartsWith("Binder") ? "3906.90.90" : "3809.91.00";

        private static readonly List<SsSiparis> _liste = new List<SsSiparis>();
        private static int _sonId, _sonTalep, _sonSs, _sonIhr, _sonPrf, _sonCi, _sonPl, _sonSf, _sonFat;
        private static bool _tohumlandi;

        // ---------- müşteriler, temsilciler, fiyat ----------
        public static List<CariKart> Musteriler(string bolge) => CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" && (string.IsNullOrEmpty(bolge) || k.Bolge == bolge) && k.Kod != "120.006").OrderBy(k => k.KisaAd, StringComparer.Create(Tr, false)).ToList();
        public static string Temsilci(CariKart k) => k.Faaliyetler.FirstOrDefault(f => f.AnaFaaliyet)?.SatisTemsilcisi ?? k.Faaliyetler.FirstOrDefault()?.SatisTemsilcisi ?? "";
        public static List<string> Temsilciler(string bolge) => Musteriler(bolge).SelectMany(k => k.Faaliyetler.Select(f => f.SatisTemsilcisi)).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x, StringComparer.Create(Tr, false)).ToList();
        public static decimal KurBul(string pb) => pb == "USD" ? FiyatOrnek.Kur.FiyatUsdTl : pb == "EUR" ? Math.Round(FiyatOrnek.Kur.FiyatUsdTl * FiyatOrnek.Kur.FiyatParite, 4) : 1m;
        /// <summary>Kalem birim fiyatı: müşterinin özel fiyatı (Selvi Kimya; Muhataplar İçin Özel Fiyatlar) yoksa Liste Fiyatı; USD → istenen para birimine fiyatlandırma kuruyla.</summary>
        public static (decimal fiyat, string kaynak) Fiyat(string cariKodu, FyUrun u, string pb)
        {
            var s = FiyatOrnek.SaticiBul(Satici); if (s == null || u == null) return (0, "—");
            var ozel = OzelFiyatOrnek.Fiyatlar(s.Ad, cariKodu).FirstOrDefault(f => f.UrunKod == u.Kod && f.Gecerli(DateTime.Today));
            decimal usd = ozel?.Fiyat ?? OzelFiyatOrnek.ListeFiyati(s, u); string kaynak = ozel != null ? "Özel Fiyat" : "Liste Fiyatı";
            decimal f = pb == "TL" ? Math.Round(usd * FiyatOrnek.Kur.FiyatUsdTl, 2) : pb == "EUR" ? FiyatOrnek.Kur.EurKarsiligi(usd) : usd;
            return (f, kaynak);
        }

        // ---------- tohum ----------
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var rnd = new Random(20261029); var bugun = DateTime.Today; var urunler = FiyatOrnek.Urunler;
                SsSiparis Yap(string bolge, CariKart k, string marka, string durum, int gunOnce, int yuklemeGun, string saat, string kaynak, string teklifNo, string olusturan)
                {
                    var pb = bolge == "Yurtiçi" ? "TL" : (k.Ulke == "Almanya" || k.Ulke == "İtalya" || k.Ulke == "Fransa" || k.Ulke == "İspanya" || k.Ulke == "Polonya" ? "EUR" : "USD");
                    var s = new SsSiparis { Id = ++_sonId, Tarih = bugun.AddDays(-gunOnce), Bolge = bolge, Marka = marka, CariKodu = k.Kod, Cari = k.KisaAd, Ulke = k.Ulke, Sehir = k.Sehir, Temsilci = Temsilci(k), Kaynak = kaynak, TeklifNo = teklifNo ?? "", ParaBirimi = pb, Kur = KurBul(pb), OdemeSekli = bolge == "Yurtiçi" ? (k.VadeSarti != null && OdemeYurtici.Contains(k.VadeSarti) ? k.VadeSarti : OdemeYurtici[rnd.Next(1, 4)]) : (OdemeYurtdisi.Contains(k.VadeSarti ?? "") ? k.VadeSarti : OdemeYurtdisi[rnd.Next(OdemeYurtdisi.Length)]), TeslimSekli = bolge == "Yurtiçi" ? TeslimYurtici[0] : TeslimYurtdisi[rnd.Next(TeslimYurtdisi.Length)], SevkAdresi = (k.Sehir ?? "") + (bolge == "Yurtiçi" ? " · müşteri deposu" : " · " + k.Ulke), YuklemeYeri = YuklemeYerleri[rnd.Next(YuklemeYerleri.Length)], Nakliye = bolge == "Yurtiçi" ? NakliyeYurtici[rnd.Next(2)] : NakliyeYurtdisi[rnd.Next(3)], NakliyeFirmasi = NakliyeFirmalari[rnd.Next(NakliyeFirmalari.Length)], Durum = durum, Olusturan = olusturan, MusteriSiparisNo = rnd.NextDouble() < 0.6 ? $"PO-{rnd.Next(1000, 9999)}" : "" };
                    s.YuklemeTarihi = bugun.AddDays(yuklemeGun); s.YuklemeSaati = saat;
                    // kalemler: kartın faaliyet gruplarındaki ürünler (2–4), fiyat özel fiyat / liste
                    var gruplar = new HashSet<string>(k.Faaliyetler.Select(f => f.AnaGrup + "|" + f.Grup)); var aday = urunler.Where(u => gruplar.Contains(u.GrupAnahtari)).ToList(); if (aday.Count < 2) aday = urunler;
                    int n = rnd.Next(2, 5); var secilen = aday.OrderBy(_ => rnd.Next()).Take(n).ToList(); int sira = 0;
                    foreach (var u in secilen) { var (f, _) = Fiyat(k.Kod, u, pb); s.Kalemler.Add(new SsKalem { Sira = ++sira, UrunKod = u.Kod, Urun = u.Ad, Grup = u.GrupAnahtari.Replace("|", " / "), HsKodu = HsKodu(u.AnaGrup), Miktar = rnd.Next(4, 60) * 25, BirimFiyat = f }); }
                    if (durum == "Talep") { s.TalepNo = $"ST-{bugun.Year}-{++_sonTalep:0000}"; s.Kaynak = "Satışçı Talebi"; s.Olusturan = s.Temsilci; }
                    else { s.No = bolge == "Yurtiçi" ? $"SS-{bugun.Year}-{++_sonSs:0000}" : $"İHR-{bugun.Year}-{++_sonIhr:0000}"; s.SiparisTarihi = s.Tarih; if (kaynak == "Satışçı Talebi") s.TalepNo = $"ST-{bugun.Year}-{++_sonTalep:0000}"; }
                    var z = s.Tarih.AddHours(9 + rnd.Next(0, 8));
                    s.Hareketler.Add(new SsHareket { Zaman = z, Kullanici = s.Olusturan, Islem = durum == "Talep" ? "Sipariş Talebi" : kaynak == "Satışçı Talebi" ? "Sipariş Talebi" : "Sipariş Açıldı", Aciklama = durum == "Talep" ? $"{s.Temsilci} sipariş talebi gönderdi · {s.Kalemler.Count} kalem · {s.ToplamKg:N0} kg" : $"{s.Kalemler.Count} kalem · {s.ToplamKg:N0} kg · {s.Tutar.ToString("N2", Tr)} {pb}" + (teklifNo != null ? $" · teklif {teklifNo}" : "") });
                    if (kaynak == "Satışçı Talebi" && durum != "Talep") s.Hareketler.Add(new SsHareket { Zaman = z.AddHours(3), Kullanici = "Deniz Çelik", Islem = "Siparişe Dönüştürüldü", Aciklama = $"{s.TalepNo} → {s.No} · marka {MarkaBul(marka).Ad} · yükleme {s.YuklemeTarihi:dd.MM.yyyy} {saat}" });
                    if (s.Asama >= 2) s.Hareketler.Add(new SsHareket { Zaman = z.AddDays(1), Kullanici = "Depo · Hakan Yıldız", Islem = "Hazırlanıyor", Aciklama = $"{s.YuklemeYeri} · {s.ToplamKoli} koli · {s.Palet} palet" });
                    if (s.Asama >= 3) s.Hareketler.Add(new SsHareket { Zaman = z.AddDays(2), Kullanici = "Depo · Hakan Yıldız", Islem = "Yüklemeye Hazır", Aciklama = $"{s.Nakliye} · {s.NakliyeFirmasi} · yükleme {s.YuklemeTarihi:dd.MM.yyyy} {saat}" });
                    if (s.Asama >= 4) { s.YuklendiTarihi = s.YuklemeTarihi; s.Hareketler.Add(new SsHareket { Zaman = s.YuklemeTarihi.Value.AddHours(int.Parse(saat.Substring(0, 2)) + 1), Kullanici = "Depo · Hakan Yıldız", Islem = "Yüklendi", Aciklama = $"{s.BrutKg:N0} kg brüt · {s.ToplamKoli} koli · {s.NakliyeFirmasi}" }); }
                    if (s.Asama >= 5) { s.FaturaNo = $"{(bolge == "Yurtiçi" ? "SLV" : "EXP")}{bugun.Year}{++_sonFat:00000}"; s.FaturaTarihi = s.YuklemeTarihi; s.Hareketler.Add(new SsHareket { Zaman = s.YuklemeTarihi.Value.AddHours(16), Kullanici = "Muhasebe · Ayşe Demir", Islem = "Faturalandı", Aciklama = $"{s.FaturaNo} · {s.Tutar.ToString("N2", Tr)} {pb}" }); }
                    if (bolge == "Yurtdışı" && s.Asama >= 3) { foreach (var tur in new[] { "Proforma Invoice", "Commercial Invoice", "Packing List" }) BelgeKaydet(s, tur, "Deniz Çelik", z.AddDays(2).AddMinutes(5)); }
                    else if (bolge == "Yurtdışı" && s.Asama >= 1) BelgeKaydet(s, "Proforma Invoice", "Deniz Çelik", z.AddHours(4));
                    else if (bolge == "Yurtiçi" && s.Asama >= 2) BelgeKaydet(s, "Sipariş Formu", "Deniz Çelik", z.AddHours(2));
                    return s;
                }
                var yi = Musteriler("Yurtiçi"); var yd = Musteriler("Yurtdışı");
                CariKart Yi(int i) => yi[i % yi.Count]; CariKart Yd(int i) => yd[i % yd.Count];
                var teklifler = TeklifOrnek.Liste.Where(t => t.SatisaDondu).OrderByDescending(t => t.SiparisTarihi).ToList();
                string Tk(string kod) => teklifler.FirstOrDefault(t => t.CariKodu == kod)?.No;
                // yurtiçi: faturalanmış, yüklenmiş, hazır, hazırlanıyor, sipariş; yükleme tarihleri geçmiş / bu hafta / gecikmiş
                _liste.Add(Yap("Yurtiçi", Yi(0), "uras", "Faturalandı", 18, -12, "09:00", "Teklif", Tk(Yi(0).Kod), "Deniz Çelik"));
                _liste.Add(Yap("Yurtiçi", Yi(1), "selvi", "Faturalandı", 12, -6, "10:30", "Satışçı Talebi", null, "Deniz Çelik"));
                _liste.Add(Yap("Yurtiçi", Yi(2), "alv", "Yüklendi", 7, -1, "14:00", "Satışçı Talebi", null, "Deniz Çelik"));
                _liste.Add(Yap("Yurtiçi", Yi(3), "uras", "Yüklemeye Hazır", 5, 1, "08:30", "Teklif", Tk(Yi(3).Kod), "Deniz Çelik"));
                _liste.Add(Yap("Yurtiçi", Yi(4), "inknovators", "Hazırlanıyor", 4, 3, "11:00", "Satışçı Talebi", null, "Deniz Çelik"));
                _liste.Add(Yap("Yurtiçi", Yi(5), "uras", "Sipariş", 2, 6, "09:30", "Elle", null, "Deniz Çelik"));
                _liste.Add(Yap("Yurtiçi", Yi(6), "selvi", "Hazırlanıyor", 9, -2, "13:00", "Satışçı Talebi", null, "Deniz Çelik"));   // gecikmiş yükleme
                _liste.Add(Yap("Yurtiçi", Yi(7), "alv", "Sipariş", 1, 9, "10:00", "Teklif", Tk(Yi(7).Kod), "Deniz Çelik"));
                _liste.Add(Yap("Yurtiçi", Yi(8), "uras", "Talep", 1, 5, "", "Satışçı Talebi", null, null));
                _liste.Add(Yap("Yurtiçi", Yi(9), "inknovators", "Talep", 0, 8, "", "Satışçı Talebi", null, null));
                _liste.Add(Yap("Yurtiçi", Yi(10), "selvi", "Talep", 0, 4, "", "Satışçı Talebi", null, null));
                // yurtdışı / ihracat
                _liste.Add(Yap("Yurtdışı", Yd(0), "uras", "Faturalandı", 25, -15, "08:00", "Teklif", Tk(Yd(0).Kod), "Esra Kaya"));
                _liste.Add(Yap("Yurtdışı", Yd(1), "inknovators", "Yüklendi", 10, -2, "09:00", "Satışçı Talebi", null, "Esra Kaya"));
                _liste.Add(Yap("Yurtdışı", Yd(2), "uras", "Yüklemeye Hazır", 8, 2, "08:00", "Teklif", Tk(Yd(2).Kod), "Esra Kaya"));
                _liste.Add(Yap("Yurtdışı", Yd(3), "alv", "Hazırlanıyor", 6, 4, "10:00", "Satışçı Talebi", null, "Esra Kaya"));
                _liste.Add(Yap("Yurtdışı", Yd(4), "selvi", "Sipariş", 3, 11, "09:00", "Elle", null, "Esra Kaya"));
                _liste.Add(Yap("Yurtdışı", Yd(5), "inknovators", "Hazırlanıyor", 12, -3, "08:30", "Satışçı Talebi", null, "Esra Kaya"));   // gecikmiş
                _liste.Add(Yap("Yurtdışı", Yd(6), "uras", "Talep", 1, 7, "", "Satışçı Talebi", null, null));
                _liste.Add(Yap("Yurtdışı", Yd(7), "inknovators", "Talep", 0, 12, "", "Satışçı Talebi", null, null));
            }
        }
        private static void BelgeKaydet(SsSiparis s, string tur, string kullanici, DateTime z)
        {
            string no = tur == "Proforma Invoice" || tur == "Proforma Fatura" ? $"PRF-{z.Year}-{++_sonPrf:0000}" : tur == "Commercial Invoice" ? $"CI-{z.Year}-{++_sonCi:0000}" : tur == "Packing List" ? $"PL-{z.Year}-{++_sonPl:0000}" : $"SF-{z.Year}-{++_sonSf:0000}";
            var mevcut = s.Belgeler.FirstOrDefault(b => b.Tur == tur);
            if (mevcut != null) { mevcut.Tarih = z; mevcut.Kullanici = kullanici; mevcut.Marka = s.Marka; return; }
            s.Belgeler.Add(new SsBelge { Tur = tur, No = no, Tarih = z, Kullanici = kullanici, Marka = s.Marka });
            s.Hareketler.Add(new SsHareket { Zaman = z, Kullanici = kullanici, Islem = "Belge Oluşturuldu", Aciklama = $"{tur} {no} · marka {MarkaBul(s.Marka).BelgeAdi}" });
        }

        // ---------- erişim ----------
        public static List<SsSiparis> Liste(string bolge) { Tohumla(); lock (Kilit) return _liste.Where(s => string.IsNullOrEmpty(bolge) || s.Bolge == bolge).OrderByDescending(s => s.Tarih).ThenByDescending(s => s.Id).ToList(); }
        public static SsSiparis Bul(string no) { Tohumla(); if (string.IsNullOrEmpty(no)) return null; lock (Kilit) return _liste.FirstOrDefault(s => string.Equals(s.No, no, StringComparison.OrdinalIgnoreCase) || string.Equals(s.TalepNo, no, StringComparison.OrdinalIgnoreCase)); }
        static void Log(SsSiparis s, string islem, string aciklama, string kullanici) => s.Hareketler.Add(new SsHareket { Zaman = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });

        // ---------- kayıt: yeni sipariş / talep ya da mevcut kaydın başlık ve kalem güncellemesi ----------
        public static (bool ok, string mesaj, SsSiparis s) Kaydet(SsKayitIstek i, string kullanici)
        {
            Tohumla(); if (i == null) return (false, "Geçersiz istek.", null);
            string bolge = Bolgeler.Contains(i.Bolge ?? "") ? i.Bolge : "Yurtiçi";
            var kart = CariKartOrnek.KartBul(i.CariKodu ?? ""); if (kart == null || kart.KartTipi != "Müşteri") return (false, "Müşteri cari kartı bulunamadı.", null);
            if (kart.Bolge != bolge) return (false, $"{kart.KisaAd} kartı {kart.Bolge} müşterisi; {bolge} siparişi açılamaz.", null);
            var marka = MarkaBul(i.Marka); if (!Markalar.Any(m => m.Kod == (i.Marka ?? "").ToLowerInvariant())) return (false, "Marka seçin (Uras, Alv Kimya, Inknovators ya da Selvi).", null);
            string pb = bolge == "Yurtiçi" ? "TL" : (i.ParaBirimi == "EUR" ? "EUR" : "USD");
            if (i.Kalemler == null || i.Kalemler.Count == 0) return (false, "En az bir kalem girin.", null);
            var kalemler = new List<SsKalem>(); int sira = 0;
            foreach (var x in i.Kalemler)
            {
                var u = FiyatOrnek.UrunBul(x.UrunKod ?? ""); if (u == null) return (false, $"Bilinmeyen ürün: {x.UrunKod}", null);
                if (x.Miktar <= 0) return (false, $"{u.Kod}: miktar sıfırdan büyük olmalıdır.", null);
                decimal fiyat = x.BirimFiyat.HasValue && x.BirimFiyat > 0 ? Math.Round(x.BirimFiyat.Value, 2) : Fiyat(kart.Kod, u, pb).fiyat;
                kalemler.Add(new SsKalem { Sira = ++sira, UrunKod = u.Kod, Urun = u.Ad, Grup = u.GrupAnahtari.Replace("|", " / "), HsKodu = HsKodu(u.AnaGrup), Miktar = x.Miktar, BirimFiyat = fiyat, KoliKg = x.KoliKg.HasValue && x.KoliKg > 0 ? x.KoliKg.Value : 25, Not = (x.Not ?? "").Trim() });
            }
            DateTime? yt = DateTime.TryParse(i.YuklemeTarihi, out var y0) ? y0.Date : (DateTime?)null; if (yt != null && yt < DateTime.Today && string.IsNullOrEmpty(i.No)) return (false, "Yükleme tarihi bugünden önce olamaz.", null);
            if (!i.TalepOlarak && yt == null) return (false, "Siparişte yükleme tarihi zorunludur (satışçılar süreci bu tarihten izler); talep olarak göndermek için \"Sipariş Talebi Gönder\" kullanın.", null);
            lock (Kilit)
            {
                var s = string.IsNullOrEmpty(i.No) ? null : _liste.FirstOrDefault(x => x.No == i.No || x.TalepNo == i.No);
                bool yeni = s == null;
                if (yeni) { s = new SsSiparis { Id = ++_sonId, Tarih = DateTime.Today, Bolge = bolge, Olusturan = kullanici, Kaynak = i.TalepOlarak ? "Satışçı Talebi" : (!string.IsNullOrEmpty(i.TeklifNo) ? "Teklif" : "Elle") }; _liste.Add(s); }
                else if (s.Asama >= 4) return (false, $"{s.No} yüklendi / faturalandı; kalemler değiştirilemez.", s);
                s.Marka = marka.Kod; s.CariKodu = kart.Kod; s.Cari = kart.KisaAd; s.Ulke = kart.Ulke; s.Sehir = kart.Sehir; s.Temsilci = string.IsNullOrWhiteSpace(i.Temsilci) ? Temsilci(kart) : i.Temsilci.Trim();
                s.ParaBirimi = pb; s.Kur = KurBul(pb); s.OdemeSekli = i.OdemeSekli ?? (bolge == "Yurtiçi" ? OdemeYurtici[1] : OdemeYurtdisi[0]); s.TeslimSekli = i.TeslimSekli ?? (bolge == "Yurtiçi" ? TeslimYurtici[0] : TeslimYurtdisi[0]); s.SevkAdresi = (i.SevkAdresi ?? "").Trim(); s.MusteriSiparisNo = (i.MusteriSiparisNo ?? "").Trim(); s.TeklifNo = (i.TeklifNo ?? "").Trim(); s.Not = (i.Not ?? "").Trim();
                s.Kalemler = kalemler; s.YuklemeTarihi = yt; s.YuklemeSaati = (i.YuklemeSaati ?? "").Trim(); s.YuklemeYeri = string.IsNullOrEmpty(i.YuklemeYeri) ? s.YuklemeYeri : i.YuklemeYeri; s.Nakliye = i.Nakliye ?? s.Nakliye; s.NakliyeFirmasi = i.NakliyeFirmasi ?? s.NakliyeFirmasi; s.YuklemeNotu = (i.YuklemeNotu ?? "").Trim();
                if (yeni)
                {
                    if (i.TalepOlarak) { s.Durum = "Talep"; s.TalepNo = $"ST-{DateTime.Today.Year}-{++_sonTalep:0000}"; Log(s, "Sipariş Talebi", $"{kullanici} sipariş talebi gönderdi · {s.Kalemler.Count} kalem · {s.ToplamKg:N0} kg · {s.Tutar.ToString("N2", Tr)} {pb}" + (yt != null ? $" · istenen yükleme {yt:dd.MM.yyyy} {s.YuklemeSaati}" : ""), kullanici); return (true, $"{s.TalepNo} sipariş talebi gönderildi; satış operasyonu siparişe dönüştürecek.", s); }
                    s.Durum = "Sipariş"; s.SiparisTarihi = DateTime.Today; s.No = bolge == "Yurtiçi" ? $"SS-{DateTime.Today.Year}-{++_sonSs:0000}" : $"İHR-{DateTime.Today.Year}-{++_sonIhr:0000}";
                    Log(s, "Sipariş Açıldı", $"{s.Kalemler.Count} kalem · {s.ToplamKg:N0} kg · {s.Tutar.ToString("N2", Tr)} {pb} · marka {marka.Ad} · yükleme {yt:dd.MM.yyyy} {s.YuklemeSaati}", kullanici);
                    return (true, $"{s.No} siparişi açıldı · marka {marka.Ad} · yükleme {yt:dd.MM.yyyy} {s.YuklemeSaati}.", s);
                }
                Log(s, "Güncellendi", $"{s.Kalemler.Count} kalem · {s.ToplamKg:N0} kg · {s.Tutar.ToString("N2", Tr)} {pb} · marka {marka.Ad}" + (yt != null ? $" · yükleme {yt:dd.MM.yyyy} {s.YuklemeSaati}" : ""), kullanici);
                return (true, $"{s.No ?? s.TalepNo} güncellendi.", s);
            }
        }

        /// <summary>Satışçı talebini siparişe dönüştürür: numara verilir, yükleme tarihi / saati kesinleşir.</summary>
        public static (bool ok, string mesaj, SsSiparis s) Donustur(string no, string yuklemeTarihi, string yuklemeSaati, string yuklemeYeri, string marka, string kullanici)
        {
            var s = Bul(no); if (s == null) return (false, "Talep bulunamadı.", null); if (!s.Talep) return (false, $"{s.No ?? s.TalepNo} zaten sipariş ({s.Durum}).", s);
            DateTime? yt = DateTime.TryParse(yuklemeTarihi, out var y0) ? y0.Date : s.YuklemeTarihi; if (yt == null) return (false, "Yükleme tarihi girin.", s); if (yt < DateTime.Today) return (false, "Yükleme tarihi bugünden önce olamaz.", s);
            lock (Kilit)
            {
                s.Durum = "Sipariş"; s.SiparisTarihi = DateTime.Today; s.YuklemeTarihi = yt; if (!string.IsNullOrWhiteSpace(yuklemeSaati)) s.YuklemeSaati = yuklemeSaati.Trim(); if (!string.IsNullOrEmpty(yuklemeYeri)) s.YuklemeYeri = yuklemeYeri; if (!string.IsNullOrEmpty(marka) && Markalar.Any(m => m.Kod == marka)) s.Marka = marka;
                s.No = s.Bolge == "Yurtiçi" ? $"SS-{DateTime.Today.Year}-{++_sonSs:0000}" : $"İHR-{DateTime.Today.Year}-{++_sonIhr:0000}";
                Log(s, "Siparişe Dönüştürüldü", $"{s.TalepNo} → {s.No} · marka {MarkaBul(s.Marka).Ad} · yükleme {yt:dd.MM.yyyy} {s.YuklemeSaati}", kullanici);
            }
            return (true, $"{s.TalepNo} talebi {s.No} siparişine dönüştü · yükleme {yt:dd.MM.yyyy} {s.YuklemeSaati}.", s);
        }
        public static (bool ok, string mesaj, SsSiparis s) YuklemePlanla(SsYuklemeIstek i, string kullanici)
        {
            var s = Bul(i?.No); if (s == null) return (false, "Kayıt bulunamadı.", null); if (s.Asama >= 4) return (false, $"{s.No} yüklendi; yükleme planı değiştirilemez.", s);
            DateTime? yt = DateTime.TryParse(i.YuklemeTarihi, out var y0) ? y0.Date : (DateTime?)null; if (yt == null) return (false, "Yükleme tarihi girin.", s);
            lock (Kilit)
            {
                string eski = s.YuklemeTarihi != null ? $"{s.YuklemeTarihi:dd.MM.yyyy} {s.YuklemeSaati}" : "—";
                s.YuklemeTarihi = yt; s.YuklemeSaati = (i.YuklemeSaati ?? "").Trim(); if (!string.IsNullOrEmpty(i.YuklemeYeri)) s.YuklemeYeri = i.YuklemeYeri; if (!string.IsNullOrEmpty(i.Nakliye)) s.Nakliye = i.Nakliye; if (i.NakliyeFirmasi != null) s.NakliyeFirmasi = i.NakliyeFirmasi; if (i.YuklemeNotu != null) s.YuklemeNotu = i.YuklemeNotu.Trim();
                Log(s, "Yükleme Planlandı", $"{eski} → {yt:dd.MM.yyyy} {s.YuklemeSaati} · {s.YuklemeYeri} · {s.Nakliye} {s.NakliyeFirmasi}" + (s.YuklemeNotu != "" ? " · " + s.YuklemeNotu : ""), kullanici);
            }
            return (true, $"{s.No ?? s.TalepNo}: yükleme {yt:dd.MM.yyyy} {s.YuklemeSaati} olarak planlandı; satışçı takip ekranında görünür.", s);
        }
        public static (bool ok, string mesaj, SsSiparis s) MarkaDegistir(SsMarkaIstek i, string kullanici)
        {
            var s = Bul(i?.No); if (s == null) return (false, "Kayıt bulunamadı.", null); var m = Markalar.FirstOrDefault(x => x.Kod == (i.Marka ?? "")); if (m == null) return (false, "Marka bulunamadı.", s);
            if (s.Asama >= 4) return (false, $"{s.No} yüklendi; marka değiştirilemez (belgeler kesildi).", s);
            lock (Kilit) { string eski = MarkaBul(s.Marka).Ad; s.Marka = m.Kod; Log(s, "Marka Değişti", $"{eski} → {m.Ad}; belgeler {m.BelgeAdi} adıyla üretilir" + (s.Belgeler.Count > 0 ? " · mevcut belgeler yeniden oluşturulmalı" : ""), kullanici); }
            return (true, $"Marka {m.Ad} olarak ayarlandı; belgeler {m.BelgeAdi} adı ve ünvanıyla çıkar.", s);
        }
        static readonly Dictionary<string, string[]> Gecisler = new Dictionary<string, string[]> { ["Talep"] = new[] { "Reddedildi", "İptal" }, ["Sipariş"] = new[] { "Hazırlanıyor", "İptal" }, ["Hazırlanıyor"] = new[] { "Yüklemeye Hazır", "Sipariş", "İptal" }, ["Yüklemeye Hazır"] = new[] { "Yüklendi", "Hazırlanıyor", "İptal" }, ["Yüklendi"] = new[] { "Faturalandı" }, ["Faturalandı"] = new string[0], ["Reddedildi"] = new string[0], ["İptal"] = new string[0] };
        public static string[] SonrakiDurumlar(SsSiparis s) => Gecisler.TryGetValue(s.Durum, out var g) ? g : new string[0];
        public static (bool ok, string mesaj, SsSiparis s) DurumDegistir(SsDurumIstek i, string kullanici)
        {
            var s = Bul(i?.No); if (s == null) return (false, "Kayıt bulunamadı.", null);
            if (!SonrakiDurumlar(s).Contains(i.Durum ?? "")) return (false, $"{s.Durum} → {i.Durum} geçişi yapılamaz.", s);
            if (i.Durum == "Yüklemeye Hazır" && s.YuklemeTarihi == null) return (false, "Önce yükleme tarihi ve saati planlayın.", s);
            if (s.Bolge == "Yurtdışı" && i.Durum == "Yüklendi" && !s.Belgeler.Any(b => b.Tur == "Packing List")) return (false, "İhracat yüklemesi için önce Packing List ve Commercial Invoice oluşturun (tek tuş: İhracat Belge Seti).", s);
            lock (Kilit)
            {
                string eski = s.Durum; s.Durum = i.Durum;
                if (i.Durum == "Yüklendi") s.YuklendiTarihi = DateTime.Now;
                if (i.Durum == "Faturalandı") { s.FaturaNo = string.IsNullOrWhiteSpace(i.FaturaNo) ? $"{(s.Bolge == "Yurtiçi" ? "SLV" : "EXP")}{DateTime.Today.Year}{++_sonFat:00000}" : i.FaturaNo.Trim(); s.FaturaTarihi = DateTime.Today; }
                Log(s, i.Durum, $"{eski} → {i.Durum}" + (i.Durum == "Faturalandı" ? $" · {s.FaturaNo}" : "") + (string.IsNullOrWhiteSpace(i.Not) ? "" : " · " + i.Not.Trim()), kullanici);
            }
            return (true, $"{s.No ?? s.TalepNo}: {i.Durum}" + (i.Durum == "Faturalandı" ? $" · fatura {s.FaturaNo}" : "") + ".", s);
        }

        // ---------- belgeler: seçilen markanın adı / ünvanı / adresiyle ----------
        public static readonly string[] BelgeTurleriYurtdisi = { "Proforma Invoice", "Commercial Invoice", "Packing List" };
        public static readonly string[] BelgeTurleriYurtici = { "Sipariş Formu", "Proforma Fatura" };
        public static (bool ok, string mesaj, DaBelge belge) BelgeOlustur(string no, string tur, string kullanici)
        {
            var s = Bul(no); if (s == null) return (false, "Kayıt bulunamadı.", null);
            bool set = tur == "set"; var turler = set ? (s.Bolge == "Yurtdışı" ? BelgeTurleriYurtdisi : BelgeTurleriYurtici) : new[] { tur };
            if (!set && !BelgeTurleriYurtdisi.Contains(tur) && !BelgeTurleriYurtici.Contains(tur)) return (false, "Belge türü bilinmiyor.", null);
            if (s.Talep) return (false, "Önce talebi siparişe dönüştürün.", null);
            lock (Kilit) foreach (var t in turler) BelgeKaydet(s, t, kullanici, DateTime.Now);
            var m = MarkaBul(s.Marka); var b = new DaBelge { Baslik = set ? (s.Bolge == "Yurtdışı" ? "EXPORT DOCUMENTS" : "SİPARİŞ BELGELERİ") : turler[0].ToUpperInvariant(), AltBaslik = $"{m.Slogan} · {m.Unvan} · {m.Adres} · {m.Eposta} · {m.Tel}", Sirket = m.BelgeAdi, Hazirlayan = kullanici, Yatay = false };
            bool en = s.Bolge == "Yurtdışı"; var kur = FiyatOrnek.Kur; string pbAd = s.ParaBirimi == "TL" ? "TRY" : s.ParaBirimi;
            if (!set) b.Ozet.AddRange(OzetSatirlari(s, turler[0], en));
            int idx = 0;
            foreach (var t in turler)
            {
                var belge = s.Belgeler.First(x => x.Tur == t); string baslik = $"{t.ToUpperInvariant()} · {belge.No} · {belge.Tarih:dd.MM.yyyy}";
                if (set) { var ozet = OzetSatirlari(s, t, en); b.Bloklar.Add(new DaBlok { Baslik = baslik, YeniSayfa = idx > 0, UstNot = string.Join("   |   ", ozet.Select(o => o.Etiket + ": " + o.Deger)) }); }
                if (t == "Packing List")
                {
                    var bl = new DaBlok { Baslik = set ? null : "Packages", Kolonlar = { "No", "Item Code", "Description", "HS Code", "Packages (boxes)", "Net Weight (kg)", "Gross Weight (kg)" }, Sag = { false, false, false, false, true, true, true } };
                    foreach (var k in s.Kalemler) bl.Satirlar.Add(new[] { k.Sira.ToString(), k.UrunKod, k.Urun + " · " + m.BelgeAdi, k.HsKodu, $"{k.Koli} × {k.KoliKg:0} kg", k.NetKg.ToString("N0", CultureInfo.InvariantCulture), k.BrutKg.ToString("N1", CultureInfo.InvariantCulture) });
                    bl.Toplam = new[] { "", "", "TOTAL", "", $"{s.ToplamKoli} boxes · {s.Palet} pallets", s.ToplamKg.ToString("N0", CultureInfo.InvariantCulture), s.BrutKg.ToString("N1", CultureInfo.InvariantCulture) };
                    bl.Not = $"Loading: {s.YuklemeTarihi:dd.MM.yyyy} {s.YuklemeSaati} at {s.YuklemeYeri} · Transport: {s.Nakliye} {s.NakliyeFirmasi} · Delivery terms: {s.TeslimSekli} · Marks: {m.BelgeAdi} / {s.Cari} / {s.No}" + (s.YuklemeNotu != "" ? " · " + s.YuklemeNotu : "");
                    b.Bloklar.Add(bl);
                }
                else
                {
                    var bl = new DaBlok { Baslik = set ? null : (en ? "Items" : "Kalemler"), Kolonlar = en ? new List<string> { "No", "Item Code", "Description", "HS Code", "Quantity", "Unit", $"Unit Price ({pbAd})", $"Amount ({pbAd})" } : new List<string> { "No", "Ürün Kodu", "Ürün", "GTİP", "Miktar", "Birim", "Birim Fiyat (₺)", "Tutar (₺)" }, Sag = { false, false, false, false, true, false, true, true } };
                    foreach (var k in s.Kalemler) bl.Satirlar.Add(new[] { k.Sira.ToString(), k.UrunKod, k.Urun + " · " + m.BelgeAdi, k.HsKodu, k.Miktar.ToString("N0", en ? CultureInfo.InvariantCulture : Tr), k.Birim, k.BirimFiyat.ToString("N2", en ? CultureInfo.InvariantCulture : Tr), k.Tutar.ToString("N2", en ? CultureInfo.InvariantCulture : Tr) });
                    bl.Toplam = new[] { "", "", en ? "TOTAL" : "TOPLAM", "", s.ToplamKg.ToString("N0", en ? CultureInfo.InvariantCulture : Tr), "kg", "", s.Tutar.ToString("N2", en ? CultureInfo.InvariantCulture : Tr) };
                    bl.Not = en ? (t == "Proforma Invoice" ? $"Validity: 30 days · Payment: {s.OdemeSekli} · Delivery: {s.TeslimSekli} · Origin: Türkiye · Bank: {m.Banka}" : $"We certify that this invoice is true and correct. Payment: {s.OdemeSekli} · Delivery: {s.TeslimSekli} · Origin: Türkiye · Gross weight {s.BrutKg:N1} kg · Net {s.ToplamKg:N0} kg · {s.ToplamKoli} boxes · Bank: {m.Banka}") : $"Fiyatlar KDV hariçtir. Ödeme: {s.OdemeSekli} · Teslim: {s.TeslimSekli} · Yükleme: {s.YuklemeTarihi:dd.MM.yyyy} {s.YuklemeSaati} {s.YuklemeYeri} · Banka: {m.Banka}";
                    b.Bloklar.Add(bl);
                }
                idx++;
            }
            b.Bloklar.Add(new DaBlok { Baslik = en ? "Seller / Exporter" : "Satıcı", Not = $"{m.Unvan} · {m.Adres} · {m.Eposta} · {m.Web} · {m.Tel}   —   {(en ? "Authorized signature" : "Yetkili imza")}: {kullanici}   ({(en ? "issued under the brand" : "marka")} {m.BelgeAdi})" });
            return (true, $"{string.Join(", ", turler)} oluşturuldu · marka {m.BelgeAdi}.", b);
        }
        static List<(string Etiket, string Deger)> OzetSatirlari(SsSiparis s, string tur, bool en)
        {
            var belge = s.Belgeler.FirstOrDefault(x => x.Tur == tur); string pbAd = s.ParaBirimi == "TL" ? "TRY" : s.ParaBirimi;
            var l = new List<(string, string)> { (en ? "Document No" : "Belge No", belge?.No ?? ""), (en ? "Date" : "Tarih", (belge?.Tarih ?? DateTime.Today).ToString("dd.MM.yyyy")), (en ? "Order No" : "Sipariş No", s.No + (s.MusteriSiparisNo != "" ? " / PO " + s.MusteriSiparisNo : "")), (en ? "Buyer" : "Alıcı", $"{s.Cari} · {s.Sehir} / {s.Ulke}"), (en ? "Payment" : "Ödeme", s.OdemeSekli ?? ""), (en ? "Delivery" : "Teslim", s.TeslimSekli ?? ""), (en ? "Currency" : "Para Birimi", pbAd) };
            if (tur == "Packing List" || tur == "Commercial Invoice") l.Add((en ? "Loading" : "Yükleme", $"{s.YuklemeTarihi:dd.MM.yyyy} {s.YuklemeSaati} · {s.Nakliye}"));
            return l;
        }

        // ---------- json ----------
        public static object Json(SsSiparis s)
        {
            var m = MarkaBul(s.Marka); int kalan = s.YuklemeTarihi != null ? (s.YuklemeTarihi.Value.Date - DateTime.Today).Days : int.MinValue;
            return new
            {
                s.Id, no = s.No, talepNo = s.TalepNo, tarih = s.Tarih.ToString("yyyy-MM-dd"), bolge = s.Bolge, satici = s.Satici, marka = m.Kod, markaAd = m.Ad, markaBelge = m.BelgeAdi, markaRenk = m.Renk,
                cariKodu = s.CariKodu, cari = s.Cari, ulke = s.Ulke, sehir = s.Sehir, temsilci = s.Temsilci, kaynak = s.Kaynak, teklifNo = s.TeklifNo, musteriSiparisNo = s.MusteriSiparisNo, pb = s.ParaBirimi, kur = s.Kur,
                kalemler = s.Kalemler.Select(k => new { k.Sira, k.UrunKod, k.Urun, k.Grup, k.HsKodu, k.Miktar, k.Birim, k.KoliKg, k.Koli, k.NetKg, k.BrutKg, k.BirimFiyat, k.Tutar, k.Not }).ToList(),
                kalem = s.Kalemler.Count, tutar = s.Tutar, tutarTl = s.TutarTl, toplamKg = s.ToplamKg, toplamKoli = s.ToplamKoli, brutKg = s.BrutKg, palet = s.Palet,
                odemeSekli = s.OdemeSekli, teslimSekli = s.TeslimSekli, sevkAdresi = s.SevkAdresi, yuklemeTarihi = s.YuklemeTarihi?.ToString("yyyy-MM-dd"), yuklemeSaati = s.YuklemeSaati, yuklemeYeri = s.YuklemeYeri, nakliye = s.Nakliye, nakliyeFirmasi = s.NakliyeFirmasi, yuklemeNotu = s.YuklemeNotu, yuklemeKalan = s.YuklemeTarihi != null ? kalan : (int?)null,
                durum = s.Durum, asama = s.Asama, acik = s.Acik, talep = s.Talep, gecikti = s.Gecikti, siparisTarihi = s.SiparisTarihi?.ToString("yyyy-MM-dd"), yuklendiTarihi = s.YuklendiTarihi?.ToString("yyyy-MM-dd HH:mm"), faturaNo = s.FaturaNo, faturaTarihi = s.FaturaTarihi?.ToString("yyyy-MM-dd"),
                belgeler = s.Belgeler.Select(b => new { b.Tur, b.No, tarih = b.Tarih.ToString("yyyy-MM-dd HH:mm"), b.Kullanici, markaBelge = MarkaBul(b.Marka).BelgeAdi, eskiMarka = b.Marka != s.Marka }).ToList(),
                hareketler = s.Hareketler.OrderByDescending(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("yyyy-MM-dd HH:mm"), h.Kullanici, h.Islem, h.Aciklama }).ToList(),
                sonraki = SonrakiDurumlar(s), olusturan = s.Olusturan, not = s.Not
            };
        }
        public static object Ozet(List<SsSiparis> l)
        {
            var bugun = DateTime.Today; var acik = l.Where(s => s.Acik).ToList();
            return new
            {
                talep = l.Count(s => s.Talep), talepTutar = l.Where(s => s.Talep).Sum(s => s.TutarTl), acik = acik.Count, acikTutar = acik.Sum(s => s.TutarTl), acikKg = acik.Sum(s => s.ToplamKg),
                buHafta = acik.Count(s => s.YuklemeTarihi != null && s.YuklemeTarihi.Value.Date >= bugun && s.YuklemeTarihi.Value.Date <= bugun.AddDays(7)), buHaftaKg = acik.Where(s => s.YuklemeTarihi != null && s.YuklemeTarihi.Value.Date >= bugun && s.YuklemeTarihi.Value.Date <= bugun.AddDays(7)).Sum(s => s.ToplamKg),
                gecikmis = acik.Count(s => s.Gecikti), hazir = l.Count(s => s.Durum == "Yüklemeye Hazır"), yuklendiAy = l.Count(s => s.YuklendiTarihi != null && s.YuklendiTarihi.Value.Month == bugun.Month && s.YuklendiTarihi.Value.Year == bugun.Year),
                faturalandiAy = l.Count(s => s.FaturaTarihi != null && s.FaturaTarihi.Value.Month == bugun.Month && s.FaturaTarihi.Value.Year == bugun.Year), faturaAyTutar = l.Where(s => s.FaturaTarihi != null && s.FaturaTarihi.Value.Month == bugun.Month).Sum(s => s.TutarTl),
                belgesiz = l.Count(s => s.Bolge == "Yurtdışı" && s.Acik && !s.Belgeler.Any(b => b.Tur == "Packing List")),
                markalar = Markalar.Select(m => new { m.Kod, m.Ad, adet = l.Count(s => s.Marka == m.Kod && !s.Talep && s.Durum != "İptal" && s.Durum != "Reddedildi"), tutar = l.Where(s => s.Marka == m.Kod && !s.Talep && s.Durum != "İptal" && s.Durum != "Reddedildi").Sum(s => s.TutarTl) }).ToList(),
                temsilciler = l.GroupBy(s => s.Temsilci).Select(g => new { ad = g.Key, talep = g.Count(s => s.Talep), acik = g.Count(s => s.Acik), gecikmis = g.Count(s => s.Gecikti), tutar = g.Where(s => !s.Talep).Sum(s => s.TutarTl) }).OrderByDescending(x => x.acik).ToList()
            };
        }
        public static object MarkaJson(SsMarka m) => new { m.Kod, m.Ad, m.BelgeAdi, m.Slogan, m.Unvan, m.Adres, m.Eposta, m.Web, m.Tel, m.Banka, m.Renk };
    }
}
