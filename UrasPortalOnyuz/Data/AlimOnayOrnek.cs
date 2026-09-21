// ============================================================
// AYLIK ALIM ONAY EKRANI — örnek veri ve iş kuralları
// Amaç: Muhasebe, yönetime sunulacak aylık alım raporunu (tedarikçi faturaları) hazırlar; satınalmacılar kendi
// faturalarını, Tedarik Zinciri Direktörü de ay sonunda tamamını onaylar. Onay tamamlanınca ay kapatılır.
// Akış: Fatura (muhasebe kaydı) → Satınalmacı onayı → Tedarik Zinciri Direktörü onayı → Ay kapanışı.
// Uyarılar (siparişsiz fatura, fiyat artışı, bütçe aşımı, mükerrer, vade, grup içi kullanım, irsaliyesiz, eşik) sistemce üretilir.
// Kayıtlar bellek içindedir; SQL / SAP / e-fatura bağlantısı yoktur. Ekran: Controllers/AlimOnayController.cs + Views/AlimOnay/Index.cshtml
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class AlimKalem
    {
        public int Sira { get; set; }
        public string Kod { get; set; }
        public string Aciklama { get; set; }
        public decimal Miktar { get; set; }
        public string Birim { get; set; }
        public decimal BirimFiyat { get; set; }
        public decimal Iskonto { get; set; }          // %
        public decimal KdvOrani { get; set; }         // %
        public decimal OncekiBirimFiyat { get; set; } // önceki alımdaki birim fiyat (fiyat farkı uyarısı)
        public decimal Tutar => Math.Round(Miktar * BirimFiyat * (1 - Iskonto / 100m), 2);
        public decimal KdvTutar => Math.Round(Tutar * KdvOrani / 100m, 2);
        public decimal FiyatFarkiYuzde => OncekiBirimFiyat > 0 ? Math.Round((BirimFiyat - OncekiBirimFiyat) / OncekiBirimFiyat * 100m, 1) : 0;
    }

    public class AlimOnayKaydi
    {
        public string Durum { get; set; } = "Bekliyor";   // Bekliyor | Onaylandı | Reddedildi
        public string Kisi { get; set; }
        public DateTime? Tarih { get; set; }
        public string Aciklama { get; set; }
    }

    public class AlimFatura
    {
        public int Id { get; set; }
        public string FaturaNo { get; set; }
        public string Ettn { get; set; }
        public string FaturaTuru { get; set; }        // e-Fatura | e-Arşiv
        public DateTime Tarih { get; set; }
        public string Donem => Tarih.ToString("yyyy-MM");
        public string FaturaSirketi { get; set; }     // faturanın kesildiği grup şirketi
        public string KullanimSirketi { get; set; }   // kullanıcı firma: malı / hizmeti fiilen kullanan grup şirketi
        /// <summary>Fatura şirketi ≠ kullanıcı firma ise masrafın kullanıcı firmaya yansıtıldığı Yansıtma İşlemleri talep no'su (boşsa yansıtma bekliyor).</summary>
        public string YansitmaNo { get; set; }
        public string YansitmaDurumu => FaturaSirketi == KullanimSirketi ? "Gerekmiyor" : !string.IsNullOrEmpty(YansitmaNo) ? "Yansıtıldı" : "Yansıtma Bekliyor";
        public string Tedarikci { get; set; }
        public string TedarikciVkn { get; set; }
        public string TalepEden { get; set; }
        public string TalepEdenBirim { get; set; }
        public string Satinalmaci { get; set; }
        public string Kategori { get; set; }
        public string SiparisNo { get; set; }
        public string IrsaliyeNo { get; set; }
        public string MasrafMerkezi { get; set; }
        public string ParaBirimi { get; set; } = "TL";
        public decimal Kur { get; set; } = 1;
        public DateTime Vade { get; set; }
        public string OdemeDurumu { get; set; }       // Ödendi | Planlandı | Bekliyor
        public string MuhasebeFisNo { get; set; }
        public string Aciklama { get; set; }
        public List<AlimKalem> Kalemler { get; set; } = new List<AlimKalem>();
        public AlimOnayKaydi SatinalmaciOnayi { get; set; } = new AlimOnayKaydi();
        public AlimOnayKaydi DirektorOnayi { get; set; } = new AlimOnayKaydi();

        public decimal Net => Kalemler.Sum(k => k.Tutar);
        public decimal Kdv => Kalemler.Sum(k => k.KdvTutar);
        public decimal Brut => Net + Kdv;
        public decimal DovizNet => ParaBirimi == "TL" ? Net : Math.Round(Net / Kur, 2);
        public string OnayDurumu =>
            SatinalmaciOnayi.Durum == "Reddedildi" || DirektorOnayi.Durum == "Reddedildi" ? "Reddedildi" :
            SatinalmaciOnayi.Durum == "Bekliyor" ? "Satınalmacı Bekliyor" :
            DirektorOnayi.Durum == "Bekliyor" ? "Direktör Bekliyor" : "Onaylandı";
        /// <summary>Sıradaki onay adımı: Satınalmacı | Direktör | null (tamamlandı / reddedildi)</summary>
        public string SiradakiAdim => OnayDurumu == "Satınalmacı Bekliyor" ? "Satınalmacı" : OnayDurumu == "Direktör Bekliyor" ? "Direktör" : null;
        public List<string> Uyarilar => AlimOnayOrnek.Uyarilar(this);
    }

    public static class AlimOnayOrnek
    {
        public static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        public static readonly object Kilit = new object();

        // ---- Roller (örnek) ----
        public static readonly (string Ad, string Kod, string Sirket)[] Satinalmacilar = { ("Selin Kara", "skr", "Uras Kimya"), ("Emre Taş", "ets", "Selvi"), ("Burcu Aydın", "bay", "Avrupa Paper") };
        public static readonly (string Ad, string Kod) Direktor = ("Kerem Yıldız", "kyz");
        public static readonly (string Ad, string Kod) Muhasebe = ("Nazlı Erdem", "muh36");

        // ---- Kategori aylık bütçeleri (TL, net) — bütçe aşımı uyarısı ----
        public static readonly Dictionary<string, decimal> Butceler = new Dictionary<string, decimal>
        {
            ["Hammadde"] = 9_500_000m, ["Ambalaj"] = 1_400_000m, ["Yedek Parça"] = 650_000m, ["Hizmet"] = 900_000m, ["Enerji"] = 2_600_000m,
            ["Sarf Malzeme"] = 180_000m, ["Lojistik"] = 1_100_000m, ["IT / Lisans"] = 420_000m, ["Demirbaş"] = 700_000m
        };
        public static readonly decimal DirektorEsik = 1_000_000m;   // brüt; bilgilendirme uyarısı
        public static readonly decimal FiyatArtisEsik = 10m;        // %

        public static readonly HashSet<string> KapatilanAylar = new HashSet<string>();
        public static readonly List<(string Donem, string Kisi, DateTime Tarih, string Not)> KapanisKayitlari = new List<(string, string, DateTime, string)>();

        private static readonly Lazy<List<AlimFatura>> _faturalar = new Lazy<List<AlimFatura>>(Olustur);
        public static List<AlimFatura> Faturalar => _faturalar.Value;
        public static IEnumerable<string> Donemler => Faturalar.Select(f => f.Donem).Distinct().OrderByDescending(d => d);
        public static AlimFatura Bul(int id) => Faturalar.FirstOrDefault(f => f.Id == id);

        // ---- Tedarikçi / kalem şablonları: (tedarikçi, vkn, kategori, kalemler[(kod, açıklama, birim, fiyat, kdv)]) ----
        private static readonly (string Ted, string Vkn, string Kat, (string Kod, string Ad, string Birim, decimal Fiyat, decimal Kdv)[] Kalemler)[] Sablon =
        {
            ("Petkim Petrokimya Holding A.Ş.", "7290030050", "Hammadde", new[] { ("HM-LDPE-01", "Polietilen LDPE Granül (G03-5)", "kg", 41.80m, 20m), ("HM-EA-02", "Etil Asetat Teknik", "kg", 38.50m, 20m) }),
            ("Sasa Polyester Sanayi A.Ş.", "7440066020", "Hammadde", new[] { ("HM-PET-11", "PET Cips Şişelik", "kg", 34.20m, 20m), ("HM-PTA-12", "Saf Tereftalik Asit", "kg", 29.90m, 20m) }),
            ("Brenntag Kimya Tic. Ltd. Şti.", "1850035940", "Hammadde", new[] { ("HM-NAOH-21", "Sodyum Hidroksit %50 Çözelti", "ton", 14_250m, 20m), ("HM-TIO2-22", "Titanyum Dioksit Rutil", "kg", 118.00m, 20m) }),
            ("Korozo Ambalaj San. ve Tic. A.Ş.", "5780012030", "Ambalaj", new[] { ("AM-KOLI-01", "Oluklu Mukavva Koli 40x30x30", "adet", 18.40m, 20m), ("AM-STRC-02", "Streç Film 50cm 17µ", "rulo", 214.00m, 20m) }),
            ("Sarten Ambalaj San. ve Tic. A.Ş.", "7460018800", "Ambalaj", new[] { ("AM-IBC-05", "IBC Tank 1000 L (yeni)", "adet", 4_150m, 20m), ("AM-TNK-06", "Metal Bidon 25 L", "adet", 265m, 20m) }),
            ("SKF Türk San. ve Tic. Ltd. Şti.", "7670019880", "Yedek Parça", new[] { ("YP-RUL-6205", "Rulman 6205-2RS", "adet", 312m, 20m), ("YP-RUL-22218", "Rulman 22218 E", "adet", 4_820m, 20m) }),
            ("Bosch Rexroth Otomasyon San. A.Ş.", "1810049790", "Yedek Parça", new[] { ("YP-HID-01", "Hidrolik Pompa A10VSO", "adet", 68_500m, 20m), ("YP-VLF-02", "Pnömatik Valf 5/2", "adet", 2_140m, 20m) }),
            ("Anadolu Endüstriyel Temizlik Hiz. Ltd.", "0690043220", "Hizmet", new[] { ("HZ-TEM-01", "Aylık Endüstriyel Temizlik Hizmeti", "ay", 168_000m, 20m) }),
            ("Bureau Veritas Gözetim Hizmetleri", "1900024110", "Hizmet", new[] { ("HZ-DEN-02", "ISO 9001 Gözetim Denetimi", "adam-gün", 18_500m, 20m) }),
            ("Enerjisa Enerji A.Ş.", "3350044170", "Enerji", new[] { ("EN-ELK-01", "Elektrik Enerjisi (OSB tarifesi)", "kWh", 3.94m, 20m) }),
            ("İGDAŞ İstanbul Gaz Dağıtım A.Ş.", "4780025880", "Enerji", new[] { ("EN-GAZ-01", "Doğalgaz (endüstriyel)", "m³", 12.60m, 20m) }),
            ("Ofix Ofis Malzemeleri A.Ş.", "6340089010", "Sarf Malzeme", new[] { ("SF-KRT-01", "Fotokopi Kağıdı A4 80gr", "koli", 1_180m, 20m), ("SF-ELD-02", "İş Eldiveni Nitril", "çift", 42m, 20m) }),
            ("Ekol Lojistik A.Ş.", "3270052200", "Lojistik", new[] { ("LJ-NAK-01", "Tam Tır Nakliye Gebze–Adana", "sefer", 38_500m, 20m), ("LJ-NAK-02", "Parsiyel Nakliye", "palet", 1_450m, 20m) }),
            ("Microsoft Türkiye Ltd. Şti.", "6220041240", "IT / Lisans", new[] { ("IT-M365-E3", "Microsoft 365 E3 Lisans (aylık)", "kullanıcı", 1_240m, 20m) }),
            ("Logo Yazılım San. ve Tic. A.Ş.", "6090021900", "IT / Lisans", new[] { ("IT-LGO-01", "ERP Yıllık Bakım ve Destek", "yıl", 245_000m, 20m) }),
            ("Arçelik Kurumsal Satış A.Ş.", "0790009180", "Demirbaş", new[] { ("DM-KLM-01", "VRF Klima Dış Ünite 28 kW", "adet", 186_000m, 20m), ("DM-LPT-02", "Dizüstü Bilgisayar i7 32GB", "adet", 58_900m, 20m) })
        };
        private static readonly string[] Sirketler = { "Uras Kimya", "Selvi", "Avrupa Paper", "Alv Kimya", "Daf Kimya", "Uras Holding" };
        private static readonly (string Ad, string Birim)[] TalepEdenler = { ("Mehmet Arslan", "Üretim"), ("Ayşe Demir", "Bakım"), ("Fatih Koç", "Depo"), ("Zeynep Uçar", "Kalite"), ("Ali Veli", "Bilgi Teknolojileri"), ("Canan Su", "İdari İşler"), ("Hakan Ayyürek", "Finans"), ("Sedef Yılmaz", "Operasyon") };
        private static readonly string[] MasrafMerkezleri = { "CC40 - Üretim", "CC41 - Bakım", "CC42 - Depo", "CC43 - Kalite", "CC10 - Bilgi Teknolojileri", "CC05 - İdari İşler", "CC20 - Finans", "CC30 - Operasyon" };

        private static List<AlimFatura> Olustur()
        {
            var rnd = new Random(20260921);
            var l = new List<AlimFatura>();
            var bugun = DateTime.Today;
            var gecenAy = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-1);   // rapor dönemi: geçen ay (ay sonu onayı)
            var buAy = new DateTime(bugun.Year, bugun.Month, 1);
            int id = 0, fisNo = 41200;

            AlimFatura Yap(DateTime tarih, int sablonIdx, string satinalmaci, decimal carpan, string sirket = null, string kullanim = null, bool siparissiz = false, bool irsaliyesiz = false, decimal fiyatArtis = 0, string paraBirimi = "TL", decimal kur = 1)
            {
                var s = Sablon[sablonIdx];
                var te = TalepEdenler[rnd.Next(TalepEdenler.Length)];
                var f = new AlimFatura
                {
                    Id = ++id, Tarih = tarih, Tedarikci = s.Ted, TedarikciVkn = s.Vkn, Kategori = s.Kat, Satinalmaci = satinalmaci,
                    FaturaSirketi = sirket ?? Sirketler[rnd.Next(3)], TalepEden = te.Ad, TalepEdenBirim = te.Birim,
                    MasrafMerkezi = MasrafMerkezleri[Array.FindIndex(TalepEdenler, x => x.Ad == te.Ad)],
                    FaturaTuru = s.Kat == "Hizmet" || s.Kat == "Enerji" || s.Kat == "IT / Lisans" ? "e-Arşiv" : "e-Fatura",
                    ParaBirimi = paraBirimi, Kur = kur, Vade = tarih.AddDays(s.Kat == "Enerji" ? 15 : 60),
                    MuhasebeFisNo = "AF-" + (fisNo++),
                    Aciklama = s.Kat == "Hammadde" ? "Üretim planı kapsamında dönem hammadde alımı." : s.Kat == "Hizmet" ? "Sözleşme kapsamı aylık hizmet bedeli." : s.Kat == "Enerji" ? "Aylık tüketim faturası." : "Talep formuna istinaden alım."
                };
                f.KullanimSirketi = kullanim ?? f.FaturaSirketi;
                string vknKisa = s.Vkn.Substring(0, 3);
                f.FaturaNo = (f.FaturaTuru == "e-Arşiv" ? "EAR" : "URS") + tarih.ToString("yyyy") + (100000000 + id * 1379 + rnd.Next(999)).ToString();
                f.Ettn = $"{vknKisa}{id:D5}-{tarih:MMdd}-4a1b-8c2d-{tarih:yyyyMMdd}{id:D4}".ToLowerInvariant();   // e-fatura ETTN görünümü (örnek)
                f.SiparisNo = siparissiz ? "" : $"SAS-{tarih:yyyy}-{(4100 + id * 3):D5}";
                bool mal = s.Kat == "Hammadde" || s.Kat == "Ambalaj" || s.Kat == "Yedek Parça" || s.Kat == "Sarf Malzeme" || s.Kat == "Demirbaş";
                f.IrsaliyeNo = mal && !irsaliyesiz ? $"IRS-{tarih:yyyyMM}-{(700 + id * 2):D4}" : "";
                f.OdemeDurumu = f.Vade < bugun ? (rnd.Next(4) == 0 ? "Bekliyor" : "Ödendi") : (rnd.Next(3) == 0 ? "Planlandı" : "Bekliyor");
                int kalemSayisi = Math.Min(s.Kalemler.Length, 1 + rnd.Next(2));
                for (int i = 0; i < kalemSayisi; i++)
                {
                    var k = s.Kalemler[i];
                    decimal miktar = k.Birim == "kWh" ? 180_000 + rnd.Next(120_000) : k.Birim == "m³" ? 40_000 + rnd.Next(30_000) : k.Birim == "kg" ? 5_000 + rnd.Next(20_000) : k.Birim == "ton" ? 20 + rnd.Next(60) : k.Birim == "adet" && k.Fiyat > 50_000 ? 1 + rnd.Next(3) : k.Birim == "ay" || k.Birim == "yıl" ? 1 : k.Birim == "kullanıcı" ? 40 + rnd.Next(120) : k.Birim == "sefer" ? 4 + rnd.Next(12) : k.Birim == "adam-gün" ? 2 + rnd.Next(4) : 20 + rnd.Next(400);
                    decimal fiyat = Math.Round(k.Fiyat * carpan * (i == 0 && fiyatArtis > 0 ? 1 + fiyatArtis / 100m : 1), 2);
                    f.Kalemler.Add(new AlimKalem { Sira = i + 1, Kod = k.Kod, Aciklama = k.Ad, Miktar = miktar, Birim = k.Birim, BirimFiyat = fiyat, Iskonto = rnd.Next(5) == 0 ? 3 : 0, KdvOrani = k.Kdv, OncekiBirimFiyat = Math.Round(k.Fiyat * carpan, 2) });
                }
                l.Add(f);
                return f;
            }
            AlimFatura Onayli(AlimFatura f, int gunOnce, bool direktor = true) { f.SatinalmaciOnayi = new AlimOnayKaydi { Durum = "Onaylandı", Kisi = f.Satinalmaci, Tarih = bugun.AddDays(-gunOnce), Aciklama = "Sipariş ve irsaliye ile uyumlu." }; if (direktor) f.DirektorOnayi = new AlimOnayKaydi { Durum = "Onaylandı", Kisi = Direktor.Ad, Tarih = bugun.AddDays(-gunOnce + 1), Aciklama = "Uygundur." }; return f; }
            AlimFatura Red(AlimFatura f, int gunOnce, string neden) { f.SatinalmaciOnayi = new AlimOnayKaydi { Durum = "Reddedildi", Kisi = f.Satinalmaci, Tarih = bugun.AddDays(-gunOnce), Aciklama = neden }; return f; }

            // ---- Geçen ay (rapor dönemi): 34 fatura ----
            string SK = Satinalmacilar[0].Ad, ET = Satinalmacilar[1].Ad, BA = Satinalmacilar[2].Ad;
            DateTime G(int gun) => gecenAy.AddDays(gun - 1);
            Onayli(Yap(G(2), 0, SK, 1.00m, "Uras Kimya"), 12);
            Onayli(Yap(G(3), 3, ET, 1.00m, "Selvi"), 11);
            Onayli(Yap(G(4), 9, SK, 1.00m, "Uras Kimya"), 10);
            Onayli(Yap(G(4), 10, ET, 1.00m, "Selvi"), 10);
            Onayli(Yap(G(5), 7, BA, 1.00m, "Avrupa Paper"), 9);
            Onayli(Yap(G(6), 5, SK, 1.00m, "Uras Kimya"), 9, false);
            Onayli(Yap(G(7), 1, BA, 1.02m, "Avrupa Paper"), 8, false);
            Yap(G(8), 2, SK, 1.00m, "Uras Kimya", fiyatArtis: 14);                          // fiyat artışı uyarısı
            Onayli(Yap(G(9), 12, ET, 1.00m, "Selvi"), 7);
            Yap(G(10), 4, BA, 1.00m, "Avrupa Paper", siparissiz: true);                       // siparişsiz
            Onayli(Yap(G(11), 13, SK, 1.00m, "Uras Holding", "Uras Kimya"), 6).YansitmaNo = "YN-2026-000339";   // grup içi kullanım; masraf Uras Kimya'ya yansıtıldı
            Yap(G(12), 0, SK, 1.03m, "Uras Kimya");
            Onayli(Yap(G(12), 11, ET, 1.00m, "Selvi"), 6);
            Red(Yap(G(13), 6, BA, 1.00m, "Avrupa Paper"), 5, "Fatura tutarı sipariş tutarından yüksek; tedarikçiden düzeltme istendi.");
            Yap(G(14), 3, ET, 1.00m, "Selvi", irsaliyesiz: true);                             // irsaliyesiz
            Onayli(Yap(G(15), 8, SK, 1.00m, "Uras Kimya"), 5);
            Yap(G(16), 1, SK, 1.00m, "Uras Kimya");
            Yap(G(17), 15, BA, 1.00m, "Avrupa Paper");                                        // demirbaş, eşik üstü olabilir
            Onayli(Yap(G(18), 12, SK, 1.00m, "Uras Kimya"), 4, false);
            Yap(G(19), 2, ET, 1.00m, "Selvi", "Alv Kimya");                                   // grup içi kullanım; yansıtma bekliyor
            Yap(G(20), 14, SK, 1.00m, "Uras Holding", paraBirimi: "EUR", kur: 47.85m);        // döviz (TL karşılığı)
            Onayli(Yap(G(21), 5, BA, 1.00m, "Avrupa Paper"), 3, false);
            Yap(G(22), 0, SK, 1.00m, "Uras Kimya", fiyatArtis: 6);                           // eşik altı artış (uyarı yok)
            Yap(G(23), 4, ET, 1.00m, "Selvi");
            Yap(G(24), 7, SK, 1.00m, "Uras Kimya");
            var mk = Yap(G(25), 3, ET, 1.00m, "Selvi");                                       // KESİN mükerrer: aynı tedarikçi + aynı fatura no + aynı tutar (aynı fatura iki kez kaydedilmiş)
            var mk2 = Yap(G(26), 3, ET, 1.00m, "Selvi"); mk2.FaturaNo = mk.FaturaNo; mk2.Ettn = mk.Ettn; mk2.SiparisNo = mk.SiparisNo; mk2.IrsaliyeNo = mk.IrsaliyeNo; mk2.Kalemler = mk.Kalemler.Select(k => new AlimKalem { Sira = k.Sira, Kod = k.Kod, Aciklama = k.Aciklama, Miktar = k.Miktar, Birim = k.Birim, BirimFiyat = k.BirimFiyat, Iskonto = k.Iskonto, KdvOrani = k.KdvOrani, OncekiBirimFiyat = k.OncekiBirimFiyat }).ToList();
            var mk3 = Yap(G(27), 0, SK, 1.00m, "Uras Kimya");                                 // yalnız şüphe: aynı tedarikçi + aynı tutar, farklı fatura no
            var mk4 = Yap(G(28), 0, SK, 1.00m, "Uras Kimya"); mk4.Kalemler = mk3.Kalemler.Select(k => new AlimKalem { Sira = k.Sira, Kod = k.Kod, Aciklama = k.Aciklama, Miktar = k.Miktar, Birim = k.Birim, BirimFiyat = k.BirimFiyat, Iskonto = k.Iskonto, KdvOrani = k.KdvOrani, OncekiBirimFiyat = k.OncekiBirimFiyat }).ToList();
            Yap(G(26), 1, BA, 1.00m, "Avrupa Paper");
            Red(Yap(G(27), 12, SK, 1.00m, "Uras Kimya"), 2, "Nakliye sefer sayısı irsaliyelerle uyuşmuyor (12 yerine 9 sefer).");
            Yap(G(28), 6, ET, 1.00m, "Selvi");
            Yap(G(28), 0, SK, 1.01m, "Uras Kimya");                                           // hammadde bütçesini aşan son alımlar
            Yap(G(29), 2, SK, 1.00m, "Uras Kimya");
            Yap(G(30), 9, BA, 1.00m, "Avrupa Paper");
            Yap(G(30), 11, SK, 1.00m, "Uras Kimya");

            // ---- Bu ay (henüz kapanmamış; ay sonu onayı başlamadı): 10 fatura ----
            DateTime B(int gun) => buAy.AddDays(gun - 1) > bugun ? bugun : buAy.AddDays(gun - 1);
            Yap(B(1), 0, SK, 1.04m, "Uras Kimya"); Yap(B(2), 3, ET, 1.00m, "Selvi"); Yap(B(3), 9, SK, 1.00m, "Uras Kimya"); Yap(B(4), 10, ET, 1.00m, "Selvi");
            Yap(B(5), 7, BA, 1.00m, "Avrupa Paper"); Yap(B(8), 5, SK, 1.00m, "Uras Kimya", siparissiz: true); Yap(B(10), 12, ET, 1.00m, "Selvi"); Yap(B(12), 13, SK, 1.00m, "Uras Holding", "Selvi");
            Yap(B(15), 1, BA, 1.00m, "Avrupa Paper", fiyatArtis: 12); Yap(B(18), 4, SK, 1.00m, "Uras Kimya");

            // Geçen ayın öncesi (kapatılmış örnek ay): 6 fatura, tamamı onaylı
            var ikiAyOnce = gecenAy.AddMonths(-1);
            foreach (var (gun, idx, sa) in new[] { (3, 0, SK), (7, 3, ET), (10, 9, SK), (14, 7, BA), (20, 12, ET), (26, 1, SK) }) Onayli(Yap(ikiAyOnce.AddDays(gun - 1), idx, sa, 0.97m), 40);
            KapatilanAylar.Add(ikiAyOnce.ToString("yyyy-MM"));
            KapanisKayitlari.Add((ikiAyOnce.ToString("yyyy-MM"), Direktor.Ad, gecenAy.AddDays(4), "Ay sonu alım onayı tamamlandı; rapor yönetime sunuldu."));
            return l;
        }

        // ---- Uyarılar ----
        public static List<string> Uyarilar(AlimFatura f)
        {
            var u = new List<string>();
            // Kesin mükerrer: aynı tedarikçi + aynı fatura no + aynı tutar (dönemden bağımsız; aynı fatura iki kez kaydedilmiş)
            var ayni = Faturalar.FirstOrDefault(x => x.Id != f.Id && x.Tedarikci == f.Tedarikci && string.Equals(x.FaturaNo, f.FaturaNo, StringComparison.OrdinalIgnoreCase) && x.Brut == f.Brut);
            if (ayni != null) u.Add($"Mükerrer fatura: aynı tedarikçi, aynı fatura no ({f.FaturaNo}) ve aynı tutar ({f.Brut.ToString("N2", Tr)} TL) ile ikinci kayıt var (muhasebe fişi {ayni.MuhasebeFisNo}, {ayni.Tarih:dd.MM.yyyy}); biri iptal edilmeli.");
            if (string.IsNullOrEmpty(f.SiparisNo)) u.Add("Siparişsiz fatura: satınalma siparişi (SAS) bulunamadı.");
            bool mal = f.Kategori == "Hammadde" || f.Kategori == "Ambalaj" || f.Kategori == "Yedek Parça" || f.Kategori == "Sarf Malzeme" || f.Kategori == "Demirbaş";
            if (mal && string.IsNullOrEmpty(f.IrsaliyeNo)) u.Add("İrsaliyesiz mal faturası: mal kabul kaydı eşleşmedi.");
            foreach (var k in f.Kalemler.Where(k => k.FiyatFarkiYuzde >= FiyatArtisEsik)) u.Add($"Fiyat artışı: {k.Aciklama} birim fiyatı önceki alıma göre %{k.FiyatFarkiYuzde.ToString("0.#", Tr)} yüksek ({k.OncekiBirimFiyat.ToString("N2", Tr)} → {k.BirimFiyat.ToString("N2", Tr)}).");
            if (f.YansitmaDurumu == "Yansıtma Bekliyor") u.Add($"Grup içi kullanım: fatura {f.FaturaSirketi} adına, kullanıcı firma {f.KullanimSirketi}; yansıtma işlemi henüz yapılmadı.");
            if (ayni == null && Faturalar.Any(x => x.Id != f.Id && x.Donem == f.Donem && x.Tedarikci == f.Tedarikci && x.Brut == f.Brut)) u.Add("Mükerrer fatura şüphesi: aynı dönemde aynı tedarikçiden aynı tutarlı (farklı numaralı) başka fatura var.");
            if (f.Vade < DateTime.Today && f.OdemeDurumu != "Ödendi") u.Add($"Vadesi geçmiş ({f.Vade:dd.MM.yyyy}) ve ödenmemiş.");
            if (f.Brut >= DirektorEsik) u.Add($"Eşik üstü tutar: brüt {f.Brut.ToString("N0", Tr)} TL ≥ {DirektorEsik.ToString("N0", Tr)} TL; Direktör onayı zorunlu.");
            if (Butceler.TryGetValue(f.Kategori, out var butce))
            {
                decimal kumule = Faturalar.Where(x => x.Donem == f.Donem && x.Kategori == f.Kategori && x.OnayDurumu != "Reddedildi" && (x.Tarih < f.Tarih || (x.Tarih == f.Tarih && x.Id <= f.Id))).Sum(x => x.Net);
                if (kumule > butce) u.Add($"Bütçe aşımı: {f.Kategori} kategorisinde dönem kümülatif net alım {kumule.ToString("N0", Tr)} TL, aylık bütçe {butce.ToString("N0", Tr)} TL.");
            }
            return u;
        }

        // ---- Onay işlemleri ----
        public static string Onayla(int id, string rol, string kisi, string eylem, string aciklama)
        {
            lock (Kilit)
            {
                var f = Bul(id); if (f == null) return "Fatura bulunamadı.";
                if (KapatilanAylar.Contains(f.Donem)) return $"{DonemAdi(f.Donem)} dönemi kapatılmış; onay değiştirilemez.";
                if (string.IsNullOrWhiteSpace(aciklama) && eylem == "Reddet") return "Red için açıklama zorunludur.";
                var kayit = new AlimOnayKaydi { Durum = eylem == "Onayla" ? "Onaylandı" : "Reddedildi", Kisi = kisi, Tarih = DateTime.Now, Aciklama = aciklama ?? "" };
                rol = (rol ?? "").StartsWith("Sat", StringComparison.OrdinalIgnoreCase) ? "Satınalmacı" : (rol ?? "").StartsWith("Dir", StringComparison.OrdinalIgnoreCase) ? "Direktör" : rol;   // aksan / kodlama farkına dayanıklı
                if (rol == "Satınalmacı")
                {
                    if (f.SatinalmaciOnayi.Durum != "Bekliyor" && eylem == "Onayla" && f.SatinalmaciOnayi.Durum == "Onaylandı") return "Satınalmacı onayı zaten verilmiş.";
                    f.SatinalmaciOnayi = kayit; if (eylem == "Reddet") f.DirektorOnayi = new AlimOnayKaydi();
                    return null;
                }
                if (rol == "Direktör")
                {
                    if (f.SatinalmaciOnayi.Durum != "Onaylandı" && eylem == "Onayla") return "Önce satınalmacı onayı gerekir.";
                    f.DirektorOnayi = kayit;
                    return null;
                }
                return "Bu rol onay veremez (Muhasebe raporu hazırlar; onay satınalmacı ve direktöre aittir).";
            }
        }

        /// <summary>Satınalmacı reddini geri alıp faturayı yeniden onaya açar (muhasebe düzeltme sonrası).</summary>
        public static string YenidenAc(int id, string kisi)
        {
            lock (Kilit)
            {
                var f = Bul(id); if (f == null) return "Fatura bulunamadı.";
                if (KapatilanAylar.Contains(f.Donem)) return "Dönem kapatılmış.";
                f.SatinalmaciOnayi = new AlimOnayKaydi(); f.DirektorOnayi = new AlimOnayKaydi();
                return null;
            }
        }

        public static string AyKapat(string donem, string kisi, string not)
        {
            lock (Kilit)
            {
                var l = Faturalar.Where(f => f.Donem == donem).ToList();
                if (l.Count == 0) return "Dönemde fatura yok.";
                if (KapatilanAylar.Contains(donem)) return "Dönem zaten kapatılmış.";
                int bekleyen = l.Count(f => f.SiradakiAdim != null);
                if (bekleyen > 0) return $"{bekleyen} fatura hâlâ onay bekliyor; tüm faturalar onaylanmadan (ya da reddedilmeden) ay kapatılamaz.";
                KapatilanAylar.Add(donem); KapanisKayitlari.Add((donem, kisi, DateTime.Now, not ?? ""));
                return null;
            }
        }

        public static string DonemAdi(string donem) => DateTime.TryParseExact(donem + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d.ToString("MMMM yyyy", Tr) : donem;
    }
}
