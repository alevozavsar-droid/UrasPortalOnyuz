// ============================================================
// MUHATAPLAR İÇİN ÖZEL FİYATLAR — örnek veri ve kurallar (Satış ve Operasyon › Müşteri ve CRM Yönetimi › Fiyat ve Müşteri Koşulları; /Rapor95)
// Amaç: her müşteri carisi için TÜM ürünlerde satış fiyatı belirlemek (SAP B1 "muhatap özel fiyatları" karşılığı).
//   • Müşterinin daha önce aldığı ürünlerde fiyat çoğunlukla tanımlıdır (son satış fiyatından ya da elle).
//   • Hiç almadığı / fiyat verilmemiş kalemler için MİN FİYAT LİSTESİ üzerine % eklenerek fiyat toplu oluşturulur
//     (genel % ya da ürün grubu bazında %; taban: Min Fiyat, Liste Fiyatı ya da son satış; yuvarlama 0,01 / 0,05 / 0,10).
//   • Min Fiyat Listesi: satıcının alış fiyatı (Fiyat Yönetimi, Data/FiyatOrnek.cs: üretim maliyeti × (1 + üretici → satıcı oranı)) × (1 + asgari kâr %5).
//     Liste Fiyatı: alış × (1 + %30). Özel fiyat min fiyatın altına inemez (yalnız Yönetici yetkisi istisna girebilir).
//   • Para birimi USD; ₺ / EUR karşılığı Fiyat Yönetimi'ndeki fiyatlandırma kuruyla (FiyatOrnek.Kur).
// Satıcı şirket × müşteri carisi × ürün → fiyat; başlangıç / bitiş geçerliliği; her değişiklik günlüğe (kim, ne zaman, eski → yeni, kaynak).
// Ekran: Controllers/Rapor95Controller.cs + Views/Rapor95/Index.cshtml + wwwroot/js/ozel-fiyat.js
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Min + %", "Son Satış", "Elle").
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Müşteri özel fiyatı: satıcı şirket × cari × ürün (USD / birim).</summary>
    public class OzelFiyat
    {
        public int Id { get; set; }
        public string Satici { get; set; }          // FySatici.Ad (Selvi Kimya …)
        public string CariKodu { get; set; }
        public string UrunKod { get; set; }
        public decimal Fiyat { get; set; }
        public string PB { get; set; } = "USD";
        public string Kaynak { get; set; } = "Elle"; // Elle | Min + % | Liste + % | Son Satış + % | Son Satış | Kopya
        public decimal? Oran { get; set; }           // taban üzerine eklenen % (Min + % vb.)
        public DateTime Baslangic { get; set; }
        public DateTime? Bitis { get; set; }
        public DateTime Zaman { get; set; }
        public string Kullanici { get; set; }
        public string Not { get; set; } = "";
        public bool Gecerli(DateTime gun) => Baslangic.Date <= gun && (Bitis == null || Bitis.Value.Date >= gun);
    }
    /// <summary>Müşterinin ürün bazında satış geçmişi (son satış fiyatı / miktarı için).</summary>
    public class OzelFiyatSatis { public string Satici { get; set; } public string CariKodu { get; set; } public string UrunKod { get; set; } public DateTime Tarih { get; set; } public decimal Fiyat { get; set; } public decimal Miktar { get; set; } public string BelgeNo { get; set; } }
    public class OzelFiyatGunluk { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Satici { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string UrunKod { get; set; } public string UrunAd { get; set; } public string Islem { get; set; } public string Eski { get; set; } public string Yeni { get; set; } public string Kaynak { get; set; } }

    public class OzelFiyatSatirIstek { public string UrunKod { get; set; } public decimal? Fiyat { get; set; } public string Kaynak { get; set; } public decimal? Oran { get; set; } public string Not { get; set; } }
    public class OzelFiyatKayitIstek { public string Satici { get; set; } public string CariKodu { get; set; } public List<OzelFiyatSatirIstek> Satirlar { get; set; } = new List<OzelFiyatSatirIstek>(); public string Baslangic { get; set; } public string Bitis { get; set; } public bool MinAltiOnay { get; set; } }
    public class OzelFiyatTopluIstek { public string Satici { get; set; } public string CariKodu { get; set; } public decimal Oran { get; set; } public string Kapsam { get; set; } = "fiyatsiz"; public string Grup { get; set; } public Dictionary<string, decimal?> GrupOranlari { get; set; } public decimal Yuvarlama { get; set; } = 0.01m; public string Taban { get; set; } = "min"; public List<string> Urunler { get; set; } }
    public class OzelFiyatKopyaIstek { public string Satici { get; set; } public string KaynakCari { get; set; } public string HedefCari { get; set; } public bool SadeceFiyatsiz { get; set; } = true; public decimal? Oran { get; set; } }

    public static class OzelFiyatOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public const decimal MinMarj = 5m;      // Min Fiyat Listesi: satıcı alış fiyatı × (1 + %5)
        public const decimal ListeMarj = 30m;   // Liste Fiyatı: satıcı alış fiyatı × (1 + %30)
        public static readonly string[] Kaynaklar = { "Elle", "Min + %", "Liste + %", "Son Satış + %", "Son Satış", "Kopya" };
        public static readonly decimal[] Yuvarlamalar = { 0.01m, 0.05m, 0.10m };

        private static readonly List<OzelFiyat> _fiyatlar = new List<OzelFiyat>();
        private static readonly List<OzelFiyatSatis> _satislar = new List<OzelFiyatSatis>();
        private static readonly List<OzelFiyatGunluk> _gunluk = new List<OzelFiyatGunluk>();
        private static int _sonId; private static bool _tohumlandi;

        // ---------- fiyat listeleri ----------
        public static decimal MinFiyat(FySatici s, FyUrun u) => Math.Round(FiyatOrnek.SaticiFiyati(s, u) * (1 + MinMarj / 100m), 2);
        public static decimal ListeFiyati(FySatici s, FyUrun u) => Math.Round(FiyatOrnek.SaticiFiyati(s, u) * (1 + ListeMarj / 100m), 2);
        public static decimal Yuvarla(decimal v, decimal adim) { if (adim <= 0) adim = 0.01m; return Math.Round(Math.Round(v / adim, MidpointRounding.AwayFromZero) * adim, 2); }

        public static List<FySatici> Saticilar => FiyatOrnek.Saticilar;
        public static FySatici SaticiBul(string ad) => FiyatOrnek.SaticiBul(ad ?? "") ?? Saticilar.FirstOrDefault();
        /// <summary>Satıcının müşterileri: kartı satıcının grup şirketine ait olanlar + Fiyat Yönetimi'nde satıcıya bağlı tanımı olanlar + özel fiyatı / satışı olanlar; hiçbiri yoksa tüm müşteri kartları.</summary>
        public static List<CariKart> Musteriler(FySatici s)
        {
            Tohumla(); var tum = FiyatOrnek.Musteriler().ToList(); if (s == null) return tum;
            var bagli = new HashSet<string>(FiyatOrnek.MusteriTanimlari.Where(m => m.Satici == s.Ad).Select(m => m.CariKodu));
            lock (Kilit) { foreach (var f in _fiyatlar.Where(f => f.Satici == s.Ad)) bagli.Add(f.CariKodu); foreach (var x in _satislar.Where(x => x.Satici == s.Ad)) bagli.Add(x.CariKodu); }
            var l = tum.Where(k => bagli.Contains(k.Kod) || k.Sirket == s.Sirket || k.Sirket == s.Ad).ToList();
            return l.Count > 0 ? l : tum;
        }
        public static List<OzelFiyat> Fiyatlar(string satici, string cariKodu) { Tohumla(); lock (Kilit) return _fiyatlar.Where(f => f.Satici == satici && f.CariKodu == cariKodu).ToList(); }
        public static List<OzelFiyatGunluk> Gunluk(string satici = null, string cariKodu = null) { Tohumla(); lock (Kilit) return _gunluk.Where(g => (string.IsNullOrEmpty(satici) || g.Satici == satici) && (string.IsNullOrEmpty(cariKodu) || g.CariKodu == cariKodu)).OrderByDescending(g => g.Zaman).ToList(); }
        public static OzelFiyatSatis SonSatis(string satici, string cariKodu, string urunKod) { Tohumla(); lock (Kilit) return _satislar.Where(x => x.Satici == satici && x.CariKodu == cariKodu && x.UrunKod == urunKod).OrderByDescending(x => x.Tarih).FirstOrDefault(); }

        // ---------- tohum: Fiyat Yönetimi'ndeki satıcı → müşteri tanımlarından satış geçmişi ve özel fiyatlar ----------
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var rnd = new Random(20261028); var bugun = DateTime.Today; var urunler = FiyatOrnek.Urunler;
                foreach (var m in FiyatOrnek.MusteriTanimlari)
                {
                    var s = FiyatOrnek.SaticiBul(m.Satici); var kart = CariKartOrnek.KartBul(m.CariKodu); if (s == null || kart == null) continue;
                    // aldığı ürünler: cari kartındaki faaliyet gruplarındaki ürünlerin bir kısmı (yoksa deterministik bir alt küme)
                    var gruplar = new HashSet<string>(kart.Faaliyetler.Select(f => f.AnaGrup + "|" + f.Grup));
                    var aday = urunler.Where(u => gruplar.Count == 0 || gruplar.Contains(u.GrupAnahtari)).ToList(); if (aday.Count == 0) aday = urunler;
                    var alinan = aday.Where((u, i) => rnd.NextDouble() < (gruplar.Count > 0 ? 0.55 : 0.25)).ToList();
                    int n = 0;
                    foreach (var u in alinan)
                    {
                        decimal taban = FiyatOrnek.MusteriFiyati(s, m, u); int adet = rnd.Next(1, 4); DateTime son = bugun;
                        for (int i = 0; i < adet; i++)
                        {
                            var t = bugun.AddDays(-rnd.Next(5, 330)); decimal f = Math.Round(taban * (decimal)(0.96 + rnd.NextDouble() * 0.08), 2);
                            _satislar.Add(new OzelFiyatSatis { Satici = s.Ad, CariKodu = kart.Kod, UrunKod = u.Kod, Tarih = t, Fiyat = f, Miktar = rnd.Next(2, 40) * 25, BelgeNo = $"FT-{t:yyyy}-{rnd.Next(10000, 99999)}" });
                        }
                        // aldığı ürünlerin ~%80'inde özel fiyat tanımlı (son satış fiyatı ya da elle), kalanı "aldığı ama fiyatı tanımsız" (ekranda kritik uyarı)
                        if (rnd.NextDouble() < 0.8)
                        {
                            var sonSatis = _satislar.Where(x => x.CariKodu == kart.Kod && x.UrunKod == u.Kod).OrderByDescending(x => x.Tarih).First();
                            bool elle = rnd.NextDouble() < 0.35; decimal fiyat = elle ? Math.Round(taban, 2) : sonSatis.Fiyat; decimal min = MinFiyat(s, u); if (fiyat < min) fiyat = min;
                            _fiyatlar.Add(new OzelFiyat { Id = ++_sonId, Satici = s.Ad, CariKodu = kart.Kod, UrunKod = u.Kod, Fiyat = fiyat, Kaynak = elle ? "Elle" : "Son Satış", Baslangic = new DateTime(bugun.Year, 1, 1), Zaman = m.Zaman.AddDays(n % 7), Kullanici = m.Kullanici, Not = elle ? "Yıllık anlaşma" : "" }); n++;
                        }
                    }
                    // birkaç kalemde süresi dolmuş / dolacak fiyat (geçerlilik örneği)
                    var ilk = _fiyatlar.Where(f => f.CariKodu == kart.Kod).Take(2).ToList(); if (ilk.Count == 2) { ilk[0].Bitis = bugun.AddDays(12); ilk[1].Bitis = bugun.AddDays(-20); ilk[1].Not = "Kampanya fiyatı (süresi doldu)"; }
                }
                foreach (var f in _fiyatlar.OrderBy(f => f.Zaman)) { var kart = CariKartOrnek.KartBul(f.CariKodu); var u = FiyatOrnek.UrunBul(f.UrunKod); _gunluk.Add(new OzelFiyatGunluk { Zaman = f.Zaman, Kullanici = f.Kullanici, Satici = f.Satici, CariKodu = f.CariKodu, CariAd = kart?.KisaAd, UrunKod = f.UrunKod, UrunAd = u?.Ad, Islem = "Fiyat Tanımlandı", Eski = "", Yeni = f.Fiyat.ToString("N2", Tr) + " $", Kaynak = f.Kaynak }); }
            }
        }

        // ---------- liste (müşteri × tüm ürünler) ----------
        public class Satir
        {
            public FyUrun Urun; public decimal MinFiyat, ListeFiyati; public OzelFiyat Ozel; public OzelFiyatSatis SonSatis; public decimal Son12AyMiktar; public int Son12AySatis;
            public bool Aldi => Son12AySatis > 0 || SonSatis != null;
            public bool Fiyatli => Ozel != null;
            public string Durum => Ozel == null ? (Aldi ? "Aldığı · Fiyatsız" : "Fiyatsız") : !Ozel.Gecerli(DateTime.Today) ? (Ozel.Bitis != null && Ozel.Bitis.Value.Date < DateTime.Today ? "Süresi Doldu" : "Henüz Başlamadı") : Ozel.Fiyat < MinFiyat ? "Min Altı" : "Tanımlı";
        }
        public static List<Satir> Liste(FySatici s, string cariKodu)
        {
            Tohumla(); var bugun = DateTime.Today; var l = new List<Satir>();
            List<OzelFiyat> fiyatlar; List<OzelFiyatSatis> satislar;
            lock (Kilit) { fiyatlar = _fiyatlar.Where(f => f.Satici == s.Ad && f.CariKodu == cariKodu).ToList(); satislar = _satislar.Where(x => x.Satici == s.Ad && x.CariKodu == cariKodu).ToList(); }
            foreach (var u in FiyatOrnek.Urunler.Where(u => u.Durum == "Aktif"))
            {
                var us = satislar.Where(x => x.UrunKod == u.Kod).ToList(); var son12 = us.Where(x => x.Tarih >= bugun.AddMonths(-12)).ToList();
                l.Add(new Satir { Urun = u, MinFiyat = MinFiyat(s, u), ListeFiyati = ListeFiyati(s, u), Ozel = fiyatlar.FirstOrDefault(f => f.UrunKod == u.Kod), SonSatis = us.OrderByDescending(x => x.Tarih).FirstOrDefault(), Son12AyMiktar = son12.Sum(x => x.Miktar), Son12AySatis = son12.Count });
            }
            return l;
        }
        public static object SatirJson(Satir r)
        {
            var kur = FiyatOrnek.Kur; var o = r.Ozel;
            return new
            {
                kod = r.Urun.Kod, ad = r.Urun.Ad, anaGrup = r.Urun.AnaGrup, grup = r.Urun.Grup, grupAnahtari = r.Urun.GrupAnahtari, seri = r.Urun.Seri, birim = r.Urun.Birim,
                minFiyat = r.MinFiyat, listeFiyati = r.ListeFiyati, minTl = kur.TlKarsiligi(r.MinFiyat),
                sonSatis = r.SonSatis == null ? null : new { tarih = r.SonSatis.Tarih.ToString("yyyy-MM-dd"), fiyat = r.SonSatis.Fiyat, miktar = r.SonSatis.Miktar, belge = r.SonSatis.BelgeNo },
                son12AyMiktar = r.Son12AyMiktar, son12AySatis = r.Son12AySatis, aldi = r.Aldi,
                ozel = o == null ? null : new { o.Id, fiyat = o.Fiyat, o.Kaynak, oran = o.Oran, baslangic = o.Baslangic.ToString("yyyy-MM-dd"), bitis = o.Bitis?.ToString("yyyy-MM-dd"), zaman = o.Zaman.ToString("yyyy-MM-dd HH:mm"), o.Kullanici, o.Not, gecerli = o.Gecerli(DateTime.Today), tl = kur.TlKarsiligi(o.Fiyat), eur = kur.EurKarsiligi(o.Fiyat), minUstu = r.MinFiyat > 0 ? Math.Round((o.Fiyat / r.MinFiyat - 1) * 100, 1) : 0 },
                durum = r.Durum
            };
        }
        public static object Ozet(List<Satir> l)
        {
            var tanimli = l.Where(r => r.Ozel != null).ToList(); var gecerli = tanimli.Where(r => r.Ozel.Gecerli(DateTime.Today)).ToList();
            return new
            {
                toplamUrun = l.Count, fiyatli = tanimli.Count, gecerli = gecerli.Count, fiyatsiz = l.Count(r => r.Ozel == null), aldigi = l.Count(r => r.Aldi), aldigiFiyatsiz = l.Count(r => r.Aldi && r.Ozel == null),
                minAlti = tanimli.Count(r => r.Ozel.Fiyat < r.MinFiyat), suresiDolan = tanimli.Count(r => r.Ozel.Bitis != null && r.Ozel.Bitis.Value.Date < DateTime.Today), yakindaDolan = tanimli.Count(r => r.Ozel.Bitis != null && r.Ozel.Bitis.Value.Date >= DateTime.Today && r.Ozel.Bitis.Value.Date <= DateTime.Today.AddDays(30)),
                ortMinUstu = gecerli.Count > 0 ? Math.Round(gecerli.Average(r => r.MinFiyat > 0 ? (r.Ozel.Fiyat / r.MinFiyat - 1) * 100 : 0), 1) : 0,
                son12AyMiktar = l.Sum(r => r.Son12AyMiktar), gruplar = l.GroupBy(r => r.Urun.GrupAnahtari).Select(g => new { anahtar = g.Key, ad = g.Key.Replace("|", " / "), urun = g.Count(), fiyatli = g.Count(r => r.Ozel != null), aldigi = g.Count(r => r.Aldi), ortMinUstu = g.Any(r => r.Ozel != null) ? Math.Round(g.Where(r => r.Ozel != null).Average(r => r.MinFiyat > 0 ? (r.Ozel.Fiyat / r.MinFiyat - 1) * 100 : 0), 1) : (decimal?)null }).ToList()
            };
        }

        // ---------- toplu hesaplama (önizleme; kaydetmez) ----------
        public static (string hata, List<object> satirlar) TopluHesapla(OzelFiyatTopluIstek i)
        {
            var s = SaticiBul(i?.Satici); if (s == null) return ("Satıcı bulunamadı.", null);
            if (string.IsNullOrEmpty(i.CariKodu)) return ("Müşteri seçin.", null);
            if (i.Oran < -50 || i.Oran > 500) return ("Oran −50 ile 500 arasında olmalıdır.", null);
            if (!Yuvarlamalar.Contains(i.Yuvarlama)) i.Yuvarlama = 0.01m;
            var secili = i.Urunler != null && i.Urunler.Count > 0 ? new HashSet<string>(i.Urunler, StringComparer.OrdinalIgnoreCase) : null;
            var l = Liste(s, i.CariKodu); var sonuc = new List<object>();
            foreach (var r in l)
            {
                bool kapsamda = secili != null ? secili.Contains(r.Urun.Kod) : i.Kapsam == "tumu" || (i.Kapsam == "grup" ? r.Urun.GrupAnahtari == i.Grup && r.Ozel == null : i.Kapsam == "aldigi" ? r.Aldi && r.Ozel == null : r.Ozel == null);
                if (!kapsamda) continue;
                decimal oran = i.GrupOranlari != null && i.GrupOranlari.TryGetValue(r.Urun.GrupAnahtari, out var go) && go.HasValue ? go.Value : i.Oran;
                decimal taban = i.Taban == "liste" ? r.ListeFiyati : i.Taban == "sonSatis" ? (r.SonSatis?.Fiyat ?? r.MinFiyat) : r.MinFiyat;
                string kaynak = i.Taban == "liste" ? "Liste + %" : i.Taban == "sonSatis" && r.SonSatis != null ? "Son Satış + %" : "Min + %";
                decimal fiyat = Yuvarla(taban * (1 + oran / 100m), i.Yuvarlama); if (fiyat < r.MinFiyat) fiyat = r.MinFiyat;   // toplu üretimde min altı çıkmaz
                sonuc.Add(new { kod = r.Urun.Kod, fiyat, oran, kaynak, taban, tabanAdi = i.Taban == "liste" ? "Liste Fiyatı" : i.Taban == "sonSatis" && r.SonSatis != null ? "Son Satış" : "Min Fiyat", minFiyat = r.MinFiyat, eski = r.Ozel?.Fiyat });
            }
            return (null, sonuc);
        }

        // ---------- kaydetme ----------
        public static (bool ok, string mesaj, int eklenen, int guncellenen, int silinen) Kaydet(OzelFiyatKayitIstek i, string kullanici, bool yonetici)
        {
            Tohumla(); var s = SaticiBul(i?.Satici); if (s == null) return (false, "Satıcı bulunamadı.", 0, 0, 0);
            var kart = CariKartOrnek.KartBul(i.CariKodu ?? ""); if (kart == null || kart.KartTipi != "Müşteri") return (false, "Müşteri cari kartı bulunamadı.", 0, 0, 0);
            if (i.Satirlar == null || i.Satirlar.Count == 0) return (false, "Kaydedilecek satır yok.", 0, 0, 0);
            DateTime bas = DateTime.TryParse(i.Baslangic, out var b0) ? b0.Date : DateTime.Today; DateTime? bit = DateTime.TryParse(i.Bitis, out var b1) ? b1.Date : (DateTime?)null;
            if (bit != null && bit < bas) return (false, "Bitiş tarihi başlangıçtan önce olamaz.", 0, 0, 0);
            var hatalar = new List<string>(); var minAlti = new List<string>();
            foreach (var x in i.Satirlar)
            {
                var u = FiyatOrnek.UrunBul(x.UrunKod ?? ""); if (u == null) { hatalar.Add($"Bilinmeyen ürün: {x.UrunKod}"); continue; }
                if (x.Fiyat.HasValue) { if (x.Fiyat <= 0 || x.Fiyat > 100000) hatalar.Add($"{u.Kod}: fiyat 0'dan büyük olmalıdır."); else if (x.Fiyat < MinFiyat(s, u)) minAlti.Add($"{u.Kod} {u.Ad}: {x.Fiyat.Value.ToString("N2", Tr)} $ < min {MinFiyat(s, u).ToString("N2", Tr)} $"); }
            }
            if (hatalar.Count > 0) return (false, string.Join(" · ", hatalar.Take(5)), 0, 0, 0);
            if (minAlti.Count > 0 && !(yonetici && i.MinAltiOnay)) return (false, (yonetici ? "Min fiyatın altında satır var; \"Min altı fiyatı onaylıyorum\" işaretleyin: " : "Min Fiyat Listesi'nin altına fiyat girilemez (yalnız Yönetici yetkisi onaylayarak girebilir): ") + string.Join(" · ", minAlti.Take(4)) + (minAlti.Count > 4 ? $" · +{minAlti.Count - 4}" : ""), 0, 0, 0);
            int ekl = 0, gnc = 0, sil = 0; var z = DateTime.Now;
            lock (Kilit)
            {
                foreach (var x in i.Satirlar)
                {
                    var u = FiyatOrnek.UrunBul(x.UrunKod); var mevcut = _fiyatlar.FirstOrDefault(f => f.Satici == s.Ad && f.CariKodu == kart.Kod && f.UrunKod == u.Kod);
                    if (!x.Fiyat.HasValue)
                    {
                        if (mevcut == null) continue;
                        _fiyatlar.Remove(mevcut); sil++;
                        _gunluk.Insert(0, new OzelFiyatGunluk { Zaman = z, Kullanici = kullanici, Satici = s.Ad, CariKodu = kart.Kod, CariAd = kart.KisaAd, UrunKod = u.Kod, UrunAd = u.Ad, Islem = "Fiyat Silindi", Eski = mevcut.Fiyat.ToString("N2", Tr) + " $", Yeni = "", Kaynak = mevcut.Kaynak });
                        continue;
                    }
                    decimal fiyat = Math.Round(x.Fiyat.Value, 2); string kaynak = Kaynaklar.Contains(x.Kaynak ?? "") ? x.Kaynak : "Elle";
                    if (mevcut == null)
                    {
                        _fiyatlar.Add(new OzelFiyat { Id = ++_sonId, Satici = s.Ad, CariKodu = kart.Kod, UrunKod = u.Kod, Fiyat = fiyat, Kaynak = kaynak, Oran = x.Oran, Baslangic = bas, Bitis = bit, Zaman = z, Kullanici = kullanici, Not = (x.Not ?? "").Trim() }); ekl++;
                        _gunluk.Insert(0, new OzelFiyatGunluk { Zaman = z, Kullanici = kullanici, Satici = s.Ad, CariKodu = kart.Kod, CariAd = kart.KisaAd, UrunKod = u.Kod, UrunAd = u.Ad, Islem = "Fiyat Tanımlandı", Eski = "", Yeni = fiyat.ToString("N2", Tr) + " $" + (x.Oran.HasValue ? $" ({kaynak} {x.Oran.Value:0.##})" : ""), Kaynak = kaynak });
                    }
                    else
                    {
                        bool degisti = mevcut.Fiyat != fiyat || mevcut.Baslangic != bas || mevcut.Bitis != bit || (x.Not != null && mevcut.Not != x.Not.Trim());
                        if (!degisti) continue;
                        string eski = mevcut.Fiyat.ToString("N2", Tr) + " $" + (mevcut.Bitis != null ? $" (→ {mevcut.Bitis:dd.MM.yyyy})" : "");
                        mevcut.Fiyat = fiyat; mevcut.Kaynak = kaynak; mevcut.Oran = x.Oran; mevcut.Baslangic = bas; mevcut.Bitis = bit; mevcut.Zaman = z; mevcut.Kullanici = kullanici; if (x.Not != null) mevcut.Not = x.Not.Trim(); gnc++;
                        _gunluk.Insert(0, new OzelFiyatGunluk { Zaman = z, Kullanici = kullanici, Satici = s.Ad, CariKodu = kart.Kod, CariAd = kart.KisaAd, UrunKod = u.Kod, UrunAd = u.Ad, Islem = "Fiyat Güncellendi", Eski = eski, Yeni = fiyat.ToString("N2", Tr) + " $" + (bit != null ? $" (→ {bit:dd.MM.yyyy})" : ""), Kaynak = kaynak });
                    }
                }
            }
            if (ekl + gnc + sil == 0) return (true, "Değişiklik yok.", 0, 0, 0);
            CariKartOrnek.Gunluk(kart, z, kullanici, "Özel Fiyat", "", $"{ekl} yeni · {gnc} güncel · {sil} silindi ({s.Ad})", "Özel Fiyatlar");
            return (true, $"{kart.KisaAd}: {ekl} fiyat tanımlandı, {gnc} güncellendi, {sil} silindi" + (minAlti.Count > 0 ? $" · {minAlti.Count} satır min altı (Yönetici onayı)" : "") + ".", ekl, gnc, sil);
        }

        /// <summary>Bir müşterinin özel fiyatlarını başka müşteriye kopyalar (yalnız fiyatsız kalemlere ya da tümüne; isteğe bağlı % fark).</summary>
        public static (bool ok, string mesaj, int adet) Kopyala(OzelFiyatKopyaIstek i, string kullanici)
        {
            Tohumla(); var s = SaticiBul(i?.Satici); if (s == null) return (false, "Satıcı bulunamadı.", 0);
            var kaynak = CariKartOrnek.KartBul(i.KaynakCari ?? ""); var hedef = CariKartOrnek.KartBul(i.HedefCari ?? ""); if (kaynak == null || hedef == null) return (false, "Kaynak ya da hedef müşteri bulunamadı.", 0);
            if (kaynak.Kod == hedef.Kod) return (false, "Kaynak ve hedef aynı müşteri olamaz.", 0);
            if (i.Oran.HasValue && (i.Oran < -50 || i.Oran > 500)) return (false, "Oran −50 ile 500 arasında olmalıdır.", 0);
            var kayitlar = Fiyatlar(s.Ad, kaynak.Kod).Where(f => f.Gecerli(DateTime.Today)).ToList(); if (kayitlar.Count == 0) return (false, $"{kaynak.KisaAd} için geçerli özel fiyat yok.", 0);
            var istek = new OzelFiyatKayitIstek { Satici = s.Ad, CariKodu = hedef.Kod, Baslangic = DateTime.Today.ToString("yyyy-MM-dd") };
            var mevcut = new HashSet<string>(Fiyatlar(s.Ad, hedef.Kod).Select(f => f.UrunKod));
            foreach (var f in kayitlar)
            {
                if (i.SadeceFiyatsiz && mevcut.Contains(f.UrunKod)) continue; var u = FiyatOrnek.UrunBul(f.UrunKod); if (u == null) continue;
                decimal fiyat = Math.Round(f.Fiyat * (1 + (i.Oran ?? 0) / 100m), 2); decimal min = MinFiyat(s, u); if (fiyat < min) fiyat = min;
                istek.Satirlar.Add(new OzelFiyatSatirIstek { UrunKod = f.UrunKod, Fiyat = fiyat, Kaynak = "Kopya", Oran = i.Oran, Not = $"{kaynak.KisaAd} fiyatından kopya" + (i.Oran.HasValue ? $" ({(i.Oran >= 0 ? "+" : "")}{i.Oran:0.##} %)" : "") });
            }
            if (istek.Satirlar.Count == 0) return (false, "Kopyalanacak kalem yok (hedefte tüm kalemler zaten fiyatlı).", 0);
            var r = Kaydet(istek, kullanici, true); return (r.ok, r.ok ? $"{kaynak.KisaAd} → {hedef.KisaAd}: {r.eklenen + r.guncellenen} fiyat kopyalandı." : r.mesaj, r.eklenen + r.guncellenen);
        }

        // ---------- ürün bazında: bir ürünün tüm müşterilerdeki fiyatları ----------
        public static List<object> UrunBazinda(FySatici s, string urunKod)
        {
            Tohumla(); var u = FiyatOrnek.UrunBul(urunKod ?? ""); if (s == null || u == null) return new List<object>();
            decimal min = MinFiyat(s, u), liste = ListeFiyati(s, u); var kur = FiyatOrnek.Kur; var bugun = DateTime.Today; var l = new List<(bool fiyatli, string ad, object dto)>();
            foreach (var k in Musteriler(s))
            {
                OzelFiyat f; OzelFiyatSatis son; decimal m12; lock (Kilit) { f = _fiyatlar.FirstOrDefault(x => x.Satici == s.Ad && x.CariKodu == k.Kod && x.UrunKod == u.Kod); var ss = _satislar.Where(x => x.Satici == s.Ad && x.CariKodu == k.Kod && x.UrunKod == u.Kod).ToList(); son = ss.OrderByDescending(x => x.Tarih).FirstOrDefault(); m12 = ss.Where(x => x.Tarih >= bugun.AddMonths(-12)).Sum(x => x.Miktar); }
                var tanim = FiyatOrnek.MusteriBul(s.Ad, k.Kod); decimal? oranFiyat = tanim == null ? (decimal?)null : FiyatOrnek.MusteriFiyati(s, tanim, u);
                l.Add((f != null, k.KisaAd, new { kod = k.Kod, ad = k.KisaAd, sirket = k.Sirket, sinif = k.Sinif, bolge = k.Bolge, temsilci = k.Faaliyetler.FirstOrDefault(x => x.AnaFaaliyet)?.SatisTemsilcisi ?? k.Faaliyetler.FirstOrDefault()?.SatisTemsilcisi ?? "", fiyat = f?.Fiyat, kaynak = f?.Kaynak, gecerli = f?.Gecerli(bugun), bitis = f?.Bitis?.ToString("yyyy-MM-dd"), minUstu = f != null && min > 0 ? Math.Round((f.Fiyat / min - 1) * 100, 1) : (decimal?)null, tl = f == null ? (decimal?)null : kur.TlKarsiligi(f.Fiyat), sonSatis = son == null ? null : new { tarih = son.Tarih.ToString("yyyy-MM-dd"), fiyat = son.Fiyat, miktar = son.Miktar }, son12AyMiktar = m12, oranFiyat, oranKaynak = tanim == null ? null : FiyatOrnek.OranCoz(tanim.Oranlar, u).Kaynak }));
            }
            return l.OrderByDescending(x => x.fiyatli).ThenBy(x => x.ad, StringComparer.Create(Tr, false)).Select(x => x.dto).ToList();
        }
        public static object UrunJson(FySatici s, FyUrun u) => new { kod = u.Kod, ad = u.Ad, grup = u.GrupAnahtari.Replace("|", " / "), grupAnahtari = u.GrupAnahtari, seri = u.Seri, birim = u.Birim, minFiyat = MinFiyat(s, u), listeFiyati = ListeFiyati(s, u), minTl = FiyatOrnek.Kur.TlKarsiligi(MinFiyat(s, u)) };
        public static object GunlukJson(OzelFiyatGunluk g) => new { zaman = g.Zaman.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, g.Satici, g.CariKodu, g.CariAd, g.UrunKod, g.UrunAd, g.Islem, g.Eski, g.Yeni, g.Kaynak };
    }
}
