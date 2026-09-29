// ============================================================
// MÜŞTERİ TEKLİFLERİ — konsolide örnek veri (Satış ve Operasyon › Müşteri ve CRM Yönetimi › Teklif Yönetimi; Yönetim › Özel Raporlar)
// Amaç: Grup şirketlerinin müşterilere verdiği TÜM teklifleri tek listede göstermek ve "ay ay ne kadar teklif verildi,
// ne kadarı satışa döndü" özetini (12 ay + Toplam + Aylık Ort.) beslemek. Her teklif bir CARİ_MASTER müşteri kartına bağlıdır;
// müşteri bilgileri (kod, ünvan, sınıf, bölge, ilişki durumu, temsilci, vade şartı …) Data/CariKartOrnek.cs kartlarından okunur.
// Kaynak: ERP Teklif Modülü örnekleri — yılbaşından bugüne, şirket × faaliyet grubu bazında deterministik üretilir.
//   • Şirket eşlemesi: teklifi veren şirket müşteri kartındaki faaliyet ana grubuna göre bulunur (GrupSirketleri).
//   • Kalemler: kartın faaliyet satırındaki ürün hiyerarşisinden (CariKartOrnek.UrunHiyerarsisi) seçilir; fiyat aralığı ana gruba göre.
//   • Yurtdışı müşterilere EUR / USD teklif verilir; TL karşılığı teklif günü kuruyla hesaplanır (YapilanOdemelerOrnek.Kurlar).
//   • Durumlar: Taslak | Gönderildi | Revize Edildi | Satışa Döndü | Reddedildi | Süresi Doldu | İptal ("İptal" toplama girmez).
//   • Satışa dönen teklif: sipariş no / tarihi, satış tutarı (çoğu tam, bir kısmı kısmi), fatura kesildiyse fatura no.
//   • Dönüşüm olasılığı cari sınıfı / ilişki durumuna göre değişir (grup içi yüksek, özel takip düşük, makine teklifleri düşük).
// Kayıtlar bellek içindedir; SQL / ERP bağlantısı yoktur. Ekran: Controllers/MusteriTeklifController.cs + Views/MusteriTeklif/Index.cshtml
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Satışa Döndü", "Süresi Doldu", "Fiyat Yüksek Bulundu", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Teklif kalemi.</summary>
    public class TkKalem
    {
        public int Sira { get; set; }
        public string UrunSinifi { get; set; }
        public string UrunGrubu { get; set; }
        public string UrunSerisi { get; set; }
        public string Urun { get; set; }
        public decimal Miktar { get; set; }
        public string Birim { get; set; }
        public decimal BirimFiyat { get; set; }     // teklif para biriminde
        public decimal Tutar { get; set; }          // teklif para biriminde
    }

    /// <summary>Müşteriye verilen tek bir teklif (şirketten bağımsız aynı yapı).</summary>
    public class TkTeklif
    {
        public int Id { get; set; }
        public string No { get; set; }              // TKL-2026-00042
        public DateTime Tarih { get; set; }         // teklif tarihi (aylık özet bu aya yazılır)
        public string Sirket { get; set; }          // teklifi veren grup şirketi
        public string CariKodu { get; set; }        // CARİ_MASTER kodu (120.xxx)
        public string Cari { get; set; }            // cari kısa adı
        public string Temsilci { get; set; }        // faaliyet grubu bazında satış temsilcisi
        public string AnaGrup { get; set; }         // faaliyet ana grubu
        public string FaaliyetGrubu { get; set; }   // faaliyet grubu
        public string Konu { get; set; }
        public decimal Tutar { get; set; }          // teklif para biriminde (KDV hariç)
        public string ParaBirimi { get; set; } = "TL";
        public decimal Kur { get; set; } = 1;
        public decimal TutarTl { get; set; }
        public DateTime Gecerlilik { get; set; }    // teklif geçerlilik sonu
        public string Durum { get; set; }
        public int Revizyon { get; set; }
        public DateTime? KararTarihi { get; set; }  // sipariş / red / süre dolum tarihi
        public string SiparisNo { get; set; }
        public DateTime? SiparisTarihi { get; set; }
        public decimal SatisTutariTl { get; set; }  // satışa dönen tutar (TL); kısmi sipariş olabilir
        public string FaturaNo { get; set; }
        public string KaybetmeNedeni { get; set; }
        public string Aciklama { get; set; }
        public string Hazirlayan { get; set; }
        public string Onaylayan { get; set; }
        public string Kaynak { get; set; } = "ERP Teklif Modülü";
        public List<TkKalem> Kalemler { get; set; } = new List<TkKalem>();
        public bool Gecerli => Durum != "İptal";
        public bool Acik => Durum == "Gönderildi" || Durum == "Revize Edildi";
        public bool SatisaDondu => Durum == "Satışa Döndü";
    }

    public static class TeklifOrnek
    {
        public static readonly object Kilit = new object();

        // ---------- Şirketler (teklif veren grup şirketleri; rapordaki sıra) ----------
        public static readonly string[] Sirketler = { "Uras Kimya", "Selvi", "Avrupa Paper", "Alv Kimya", "Daf Kimya", "URS Makine" };
        /// <summary>Faaliyet ana grubu → o ürün ailesinde teklif veren şirketler (ilk şirket ana satıcı).</summary>
        public static readonly Dictionary<string, string[]> GrupSirketleri = new Dictionary<string, string[]>
        {
            ["Boya Grubu"] = new[] { "Uras Kimya", "Alv Kimya" }, ["Kağıt Grubu"] = new[] { "Avrupa Paper", "Selvi" }, ["Binder Grubu"] = new[] { "Alv Kimya", "Daf Kimya" },
            ["Yardımcı Kimyasallar"] = new[] { "Daf Kimya", "Uras Kimya" }, ["Baskı Makineleri"] = new[] { "URS Makine" }
        };
        /// <summary>Ana grup → birim, birim fiyat aralığı (TL) ve kalem miktar aralığı.</summary>
        internal static readonly Dictionary<string, (string Birim, int FiyatMin, int FiyatMax, int MiktarMin, int MiktarMax)> Fiyatlar = new Dictionary<string, (string, int, int, int, int)>
        {
            ["Boya Grubu"] = ("kg", 180, 420, 200, 3000), ["Kağıt Grubu"] = ("kg", 45, 90, 2000, 24000), ["Binder Grubu"] = ("kg", 95, 160, 500, 12000),
            ["Yardımcı Kimyasallar"] = ("kg", 60, 140, 200, 5000), ["Baskı Makineleri"] = ("Adet", 850_000, 4_200_000, 1, 3)
        };

        // ---------- Seçim listeleri ----------
        public static readonly string[] Durumlar = { "Taslak", "Gönderildi", "Revize Edildi", "Satışa Döndü", "Reddedildi", "Süresi Doldu", "İptal" };
        public static readonly string[] KaybetmeNedenleri = { "Fiyat Yüksek Bulundu", "Rakip Tercih Edildi", "Proje Ertelendi", "Vade Şartı Uyuşmadı", "Teknik Şartname Uyuşmadı", "Müşteri Geri Dönmedi" };
        public static readonly string[] AnaGruplar = CariKartOrnek.FaaliyetHiyerarsisi.Keys.ToArray();
        public static Dictionary<string, decimal> Kurlar => YapilanOdemelerOrnek.Kurlar;
        private static readonly HashSet<string> EurUlkeleri = new HashSet<string> { "Almanya", "İtalya", "İspanya", "Fransa", "Hollanda", "Belçika", "Polonya", "Romanya", "Bulgaristan", "Yunanistan" };
        private static decimal KurBul(string pb) => Kurlar.TryGetValue(pb ?? "TL", out var k) ? k : 1m;

        // ---------- Konsolide liste ----------
        private static readonly Lazy<List<TkTeklif>> _liste = new Lazy<List<TkTeklif>>(Uret);
        /// <summary>Tüm şirketlerin teklifleri, tarihe göre yeniden eskiye.</summary>
        public static List<TkTeklif> Liste => _liste.Value;
        public static TkTeklif Bul(string no) => Liste.FirstOrDefault(t => string.Equals(t.No, no, StringComparison.OrdinalIgnoreCase));
        /// <summary>Teklif verilen müşteri kartları (kod sırasıyla).</summary>
        public static IEnumerable<CariKart> Musteriler() { var kodlar = new HashSet<string>(Liste.Select(t => t.CariKodu)); return CariKartOrnek.Kartlar.Where(k => kodlar.Contains(k.Kod)).OrderBy(k => k.Kod); }

        /// <summary>"Ana|Grup" ürün hiyerarşisini düz listeye çevirir (sınıf, grup, seri, ad).</summary>
        internal static List<(string Sinif, string Grup, string Seri, string Ad)> Urunler(string ana, string grup)
        {
            var l = new List<(string, string, string, string)>();
            if (CariKartOrnek.UrunHiyerarsisi.TryGetValue(ana + "|" + grup, out var siniflar))
                foreach (var s in siniflar) foreach (var g in s.Value) foreach (var se in g.Value) foreach (var ad in se.Value) l.Add((s.Key, g.Key, se.Key, ad));
            if (l.Count == 0) l.Add((grup, "Standart", "Genel", grup + " Ürünü"));
            return l;
        }

        private static List<TkTeklif> Uret()
        {
            var rnd = new Random(20260928);
            var bugun = DateTime.Today; var l = new List<TkTeklif>();
            DateTime Gun(DateTime ay, int g) => new DateTime(ay.Year, ay.Month, Math.Min(g, DateTime.DaysInMonth(ay.Year, ay.Month)));
            // Müşteri kartları: faaliyet satırı olan aktif / pasif tüm müşteri kartları (pasif kartın eski teklifleri de listelenir)
            var musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" && k.Faaliyetler.Count > 0).ToList();
            var aylikCarpan = new[] { 0.8, 0.9, 1.1, 1.0, 1.05, 0.95, 0.85, 0.75, 1.1, 1.15, 1.0, 0.7 };   // mevsimsellik (Ağustos düşük, Eylül–Ekim yüksek)
            int siparisSayac = 3100, faturaSayac = 41000;

            for (int k = bugun.Month - 1; k >= 0; k--)
            {
                var ay = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-k);
                foreach (var sirket in Sirketler)
                {
                    // bu şirketin sattığı ürün ailelerinde faaliyeti olan müşteriler
                    var adaylar = musteriler.Select(m => (Kart: m, Faaliyetler: m.Faaliyetler.Where(f => GrupSirketleri.TryGetValue(f.AnaGrup, out var ss) && ss.Contains(sirket)).ToList())).Where(x => x.Faaliyetler.Count > 0).ToList();
                    if (adaylar.Count == 0) continue;
                    int taban = sirket == "URS Makine" ? 2 : sirket == "Uras Kimya" ? 8 : 5;
                    int n = Math.Max(1, (int)Math.Round((taban + rnd.Next(-1, 3)) * aylikCarpan[ay.Month - 1]));
                    for (int i = 0; i < n; i++)
                    {
                        var t = Gun(ay, rnd.Next(1, 29)); if (t > bugun) continue;
                        var (kart, faaliyetler) = adaylar[rnd.Next(adaylar.Count)];
                        var f = faaliyetler[rnd.Next(faaliyetler.Count)];
                        // ana satıcı olmayan şirket bu ürün ailesinde daha seyrek teklif verir
                        if (GrupSirketleri[f.AnaGrup][0] != sirket && rnd.Next(3) > 0) continue;
                        string pb = kart.Bolge == "Yurtdışı" ? (EurUlkeleri.Contains(kart.Ulke) ? "EUR" : "USD") : "TL";
                        decimal kur = pb == "TL" ? 1m : Math.Round(KurBul(pb) * (1 + rnd.Next(-4, 5) / 100m), 4);
                        var fy = Fiyatlar[f.AnaGrup];
                        var urunler = Urunler(f.AnaGrup, f.Grup);
                        int kn = fy.Birim == "Adet" ? 1 : Math.Min(urunler.Count, rnd.Next(1, 5));
                        var secilen = urunler.OrderBy(_ => rnd.Next()).Take(kn).ToList();
                        var teklif = new TkTeklif
                        {
                            Tarih = t, Sirket = sirket, CariKodu = kart.Kod, Cari = kart.KisaAd, Temsilci = string.IsNullOrEmpty(f.SatisTemsilcisi) ? "Satış Ofisi" : f.SatisTemsilcisi,
                            AnaGrup = f.AnaGrup, FaaliyetGrubu = f.Grup, ParaBirimi = pb, Kur = kur, Gecerlilik = t.AddDays(new[] { 15, 30, 30, 45 }[rnd.Next(4)])
                        };
                        int sira = 0;
                        foreach (var u in secilen)
                        {
                            decimal miktar = fy.Birim == "Adet" ? rnd.Next(fy.MiktarMin, fy.MiktarMax + 1) : rnd.Next(fy.MiktarMin, fy.MiktarMax + 1) / 10 * 10;
                            decimal bfTl = rnd.Next(fy.FiyatMin, fy.FiyatMax + 1) + (fy.Birim == "Adet" ? 0 : rnd.Next(0, 100) / 100m);
                            decimal bf = pb == "TL" ? bfTl : Math.Round(bfTl / kur, 2);
                            teklif.Kalemler.Add(new TkKalem { Sira = ++sira, UrunSinifi = u.Sinif, UrunGrubu = u.Grup, UrunSerisi = u.Seri, Urun = u.Ad, Miktar = miktar, Birim = fy.Birim, BirimFiyat = bf, Tutar = Math.Round(miktar * bf, 2) });
                        }
                        teklif.Tutar = teklif.Kalemler.Sum(x => x.Tutar); teklif.TutarTl = Math.Round(teklif.Tutar * kur, 2);
                        teklif.Konu = $"{f.Grup} · {secilen[0].Ad}{(kn > 1 ? $" +{kn - 1} Kalem" : "")}";
                        teklif.Hazirlayan = teklif.Temsilci; teklif.Onaylayan = teklif.TutarTl >= 1_500_000m ? "Satış Direktörü" : "Satış Müdürü";
                        teklif.Aciklama = $"{kart.VadeSarti} · {(pb == "TL" ? "KDV hariç TL" : pb + " teklif, kur " + kur.ToString("0.00"))} · geçerlilik {teklif.Gecerlilik:dd.MM.yyyy}";
                        // ---- durum
                        double donusum = kart.Sinif == "Özel Takip" ? 0.22 : kart.IliskiDurumu == "Grup İçi" ? 0.80 : kart.Sinif.Contains("Bayi") || kart.Sinif.Contains("Distribütör") ? 0.55 : 0.42;
                        if (f.AnaGrup == "Baskı Makineleri") donusum *= 0.6;
                        double r = rnd.NextDouble(); int yas = (bugun - t).Days;
                        if (r < 0.03) { teklif.Durum = "İptal"; teklif.Aciklama = "Mükerrer / hatalı teklif; iptal edildi. " + teklif.Aciklama; }
                        else if (r < 0.12 && yas < 20) teklif.Durum = "Taslak";   // son 20 günde hazırlanıp henüz gönderilmemiş teklifler
                        else
                        {
                            var karar = t.AddDays(rnd.Next(3, 36));
                            if (karar > bugun) { teklif.Durum = rnd.Next(4) == 0 ? "Revize Edildi" : "Gönderildi"; }
                            else
                            {
                                double r2 = rnd.NextDouble();
                                if (r2 < donusum)
                                {
                                    teklif.Durum = "Satışa Döndü"; teklif.KararTarihi = karar; teklif.SiparisTarihi = karar; teklif.SiparisNo = $"SS-{karar:yyyy}-{siparisSayac++:00000}";
                                    decimal oran = rnd.Next(10) < 7 ? 1m : rnd.Next(55, 100) / 100m;
                                    teklif.SatisTutariTl = Math.Round(teklif.TutarTl * oran, 2);
                                    if (karar.AddDays(12) <= bugun) teklif.FaturaNo = $"URS{karar:yyyy}{faturaSayac++:000000000}";
                                    if (oran < 1) teklif.Aciklama = $"Kısmi sipariş (%{oran * 100:0}). " + teklif.Aciklama;
                                }
                                else if (r2 < donusum + 0.22) { teklif.Durum = "Reddedildi"; teklif.KararTarihi = karar; teklif.KaybetmeNedeni = KaybetmeNedenleri[rnd.Next(KaybetmeNedenleri.Length)]; }
                                else if (teklif.Gecerlilik < bugun) { teklif.Durum = "Süresi Doldu"; teklif.KararTarihi = teklif.Gecerlilik; teklif.KaybetmeNedeni = "Müşteri Geri Dönmedi"; }
                                else teklif.Durum = rnd.Next(3) == 0 ? "Revize Edildi" : "Gönderildi";
                            }
                        }
                        if (teklif.Durum == "Revize Edildi") teklif.Revizyon = rnd.Next(1, 3);
                        l.Add(teklif);
                    }
                }
            }
            // Numara kronolojik, liste yeniden eskiye
            int seq = 0;
            foreach (var t in l.OrderBy(x => x.Tarih).ThenBy(x => x.Sirket)) t.No = $"TKL-{t.Tarih:yyyy}-{++seq:00000}";
            int id = 0;
            var sonuc = l.OrderByDescending(x => x.Tarih).ThenBy(x => x.Sirket).ThenBy(x => x.No).ToList();
            foreach (var t in sonuc) t.Id = ++id;
            return sonuc;
        }
    }
}
