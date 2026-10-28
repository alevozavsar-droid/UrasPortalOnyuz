// ============================================================
// ARAÇ BAKIM VE ONARIM TAKİBİ — tüm grup araçları (İK ve İdari İşler › İdari İşler › Araç ve Seyahat; Yönetim › Şirket Dökümanları › Araç, Taşınmaz; /AracBakim)
// Araç ana verisi (plaka, marka / model, yıl, şirket, zimmet, km, muayene, kasko / trafik poliçesi) + bakım / onarım kayıtları: periyodik bakım, arıza, kaza onarımı,
// lastik, akü, fren, klima, kaporta; servis (yetkili / özel), giriş-çıkış, km, parça ve işçilik kalemleri, KDV, fatura, ödeme, garanti, sonraki bakım (km / tarih),
// dosyalar (servis fişi, fatura PDF), günlük. Durum: Planlandı → Serviste → Tamamlandı → Faturalandı (İptal). Sonraki bakım: 15.000 km ya da 12 ay (hangisi önce).
// Sigorta poliçeleri Data/SigortaOrnek.cs'de (plaka ile bağlı). Ekran: Controllers/AracBakimController.cs + Views/AracBakim/Index.cshtml + wwwroot/js/arac-bakim.js
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Periyodik Bakım", "Kaza Onarımı", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class ArArac
    {
        public string Plaka { get; set; } public string Marka { get; set; } public string Model { get; set; } public int Yil { get; set; } public string Sirket { get; set; } public string Tip { get; set; } public string Yakit { get; set; } public int Km { get; set; }
        public string Zimmet { get; set; } public string Departman { get; set; } public string Lokasyon { get; set; } public string Durum { get; set; } = "Aktif"; public string Sasi { get; set; } public DateTime MuayeneBitis { get; set; } public string Renk { get; set; }
        public int BakimAralikKm { get; set; } = 15000; public int BakimAralikAy { get; set; } = 12; public string Not { get; set; } = "";
    }
    public class ArKalem { public int Sira { get; set; } public string Ad { get; set; } public string Tur { get; set; } = "Parça"; public decimal Miktar { get; set; } = 1; public decimal BirimFiyat { get; set; } public decimal Tutar => Math.Round(Miktar * BirimFiyat, 2); }
    public class ArDosya { public int No { get; set; } public string Tur { get; set; } public string Dosya { get; set; } public string Tip { get; set; } public long Boyut { get; set; } public byte[] Veri { get; set; } public DateTime Tarih { get; set; } public string Kullanici { get; set; } }
    public class ArHareket { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }
    public class ArBakim
    {
        public int Id { get; set; } public string No { get; set; } public string Plaka { get; set; } public DateTime Tarih { get; set; } public string Tur { get; set; } public string Servis { get; set; } public string ServisTuru { get; set; } = "Yetkili Servis"; public string ServisSehir { get; set; } = ""; public int Km { get; set; }
        public string Aciklama { get; set; } = ""; public string Sikayet { get; set; } = ""; public List<ArKalem> Kalemler { get; set; } = new List<ArKalem>();
        public decimal ParcaTutar => Kalemler.Where(k => k.Tur == "Parça").Sum(k => k.Tutar); public decimal IscilikTutar => Kalemler.Where(k => k.Tur != "Parça").Sum(k => k.Tutar); public decimal Tutar => Kalemler.Sum(k => k.Tutar); public decimal Kdv => Math.Round(Tutar * 0.20m, 2); public decimal Toplam => Tutar + Kdv;
        public string FaturaNo { get; set; } = ""; public DateTime? FaturaTarihi { get; set; } public string Odeme { get; set; } = "Şirket Kredi Kartı"; public string Durum { get; set; } = "Planlandı"; public DateTime? GirisTarihi { get; set; } public DateTime? CikisTarihi { get; set; }
        public int? SonrakiBakimKm { get; set; } public DateTime? SonrakiBakimTarihi { get; set; } public int GarantiAy { get; set; } public string HasarDosyaNo { get; set; } = ""; public string Sorumlu { get; set; } = ""; public string Not { get; set; } = "";
        public List<ArDosya> Dosyalar { get; set; } = new List<ArDosya>(); public List<ArHareket> Hareketler { get; set; } = new List<ArHareket>();
        public bool Acik => Durum == "Planlandı" || Durum == "Serviste";
        public int Asama => Durum == "Planlandı" ? 0 : Durum == "Serviste" ? 1 : Durum == "Tamamlandı" ? 2 : Durum == "Faturalandı" ? 3 : -1;
    }
    public class ArKalemIstek { public string Ad { get; set; } public string Tur { get; set; } public decimal Miktar { get; set; } public decimal BirimFiyat { get; set; } }
    public class ArBakimIstek { public int? Id { get; set; } public string Plaka { get; set; } public string Tarih { get; set; } public string Tur { get; set; } public string Servis { get; set; } public string ServisTuru { get; set; } public string ServisSehir { get; set; } public int Km { get; set; } public string Sikayet { get; set; } public string Aciklama { get; set; } public List<ArKalemIstek> Kalemler { get; set; } = new List<ArKalemIstek>(); public string Odeme { get; set; } public string FaturaNo { get; set; } public int? SonrakiBakimKm { get; set; } public string SonrakiBakimTarihi { get; set; } public int? GarantiAy { get; set; } public string HasarDosyaNo { get; set; } public string Sorumlu { get; set; } public string Not { get; set; } public string Durum { get; set; } }
    public class ArDurumIstek { public int Id { get; set; } public string Durum { get; set; } public string FaturaNo { get; set; } public int? CikisKm { get; set; } public string Not { get; set; } }
    public class ArAracIstek { public string Plaka { get; set; } public int? Km { get; set; } public string Zimmet { get; set; } public string Lokasyon { get; set; } public string Durum { get; set; } public string MuayeneBitis { get; set; } public string Not { get; set; } }

    public static class AracBakimOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public static readonly string[] Turler = { "Periyodik Bakım", "Arıza Onarımı", "Kaza Onarımı", "Lastik", "Akü", "Fren", "Klima", "Cam", "Kaporta / Boya", "Muayene", "Egzoz Emisyon", "Diğer" };
        public static readonly string[] Durumlar = { "Planlandı", "Serviste", "Tamamlandı", "Faturalandı", "İptal" };
        public static readonly string[] ServisTurleri = { "Yetkili Servis", "Özel Servis", "Lastikçi", "Kaportacı", "Muayene İstasyonu" };
        public static readonly string[] Odemeler = { "Şirket Kredi Kartı", "Havale / EFT", "Filo Firması Faturası", "Sigorta (Hasar)", "Nakit (Avans)" };
        public static readonly string[] KalemTurleri = { "Parça", "İşçilik", "Hizmet" };
        public static readonly (string Ad, string Tur, string Sehir)[] Servisler = { ("Toyota Plaza Çorlu", "Yetkili Servis", "Çorlu"), ("Renault Mais Tekirdağ", "Yetkili Servis", "Tekirdağ"), ("Tofaş Fiat Yetkili Servis Çorlu", "Yetkili Servis", "Çorlu"), ("VW Doğuş Oto Çerkezköy", "Yetkili Servis", "Çerkezköy"), ("Ford Otosan Yetkili Servis Çorlu", "Yetkili Servis", "Çorlu"), ("Mercedes Hases Tekirdağ", "Yetkili Servis", "Tekirdağ"), ("Peugeot Yetkili Servis Çorlu", "Yetkili Servis", "Çorlu"), ("BMW Borusan Kadıköy", "Yetkili Servis", "İstanbul"), ("Usta Oto Özel Servis", "Özel Servis", "Çorlu"), ("Lastik Dünyası Çorlu", "Lastikçi", "Çorlu"), ("Mega Kaporta Boya", "Kaportacı", "Çorlu"), ("TÜVTÜRK Çorlu", "Muayene İstasyonu", "Çorlu"), ("Oto Elektrik Yıldız", "Özel Servis", "Çerkezköy") };
        public static readonly string[] Sorumlular = { "İdari İşler · Serkan Genç", "Filo · Hakan Yıldız", "Filo · Burak Şen" };

        public static readonly List<ArArac> Araclar = new List<ArArac>
        {
            A("34 URS 101", "Toyota", "Corolla 1.8 Hybrid", 2022, "Uras Kimya", "Binek", "Benzin / Hibrit", 48200, "Selin Kara", "Satış", "Merkez", "WVWZZZ1KZ6W000101", 2027, 3, "Beyaz"),
            A("34 URS 102", "Renault", "Megane 1.5 dCi", 2020, "Uras Kimya", "Binek", "Dizel", 91500, "Mehmet Kaya", "Satış", "Merkez", "VF1RFB00X63000102", 2026, 12, "Gri"),
            A("34 URS 103", "Fiat", "Egea 1.3 Multijet", 2021, "Uras Kimya", "Binek", "Dizel", 63100, "Ayşe Demir", "Muhasebe", "Yetkili servis", "NM432300000000103", 2027, 5, "Siyah"),
            A("41 URS 210", "Volkswagen", "Passat 1.6 TDI", 2019, "Uras Holding", "Şoförlü", "Dizel", 120400, "Şoför · Hasan Çetin", "Yönetim", "Gebze", "WVWZZZ3CZKE000210", 2026, 11, "Siyah"),
            A("41 URS 305", "Ford", "Transit Custom 2.0", 2018, "Uras Kimya", "Kamyonet", "Dizel", 156800, "Depo · Hakan Yıldız", "Lojistik", "Gebze", "WF0YXXTTGYJ000305", 2027, 1, "Beyaz"),
            A("41 URS 306", "Fiat", "Doblo Cargo 1.6", 2020, "Alv Kimya", "Kamyonet", "Dizel", 88700, "Depo · Emre Taş", "Lojistik", "Gebze", "ZFA26300000000306", 2027, 4, "Beyaz"),
            A("34 URS 401", "Mercedes-Benz", "Sprinter 16+1", 2017, "Uras Kimya", "Servis", "Dizel", 210300, "Servis · Arı Group", "İdari İşler", "Merkez", "WDB9066331S000401", 2026, 12, "Beyaz"),
            A("34 URS 104", "Peugeot", "3008 1.5 BlueHDi", 2023, "Uras Holding", "Binek", "Dizel", 32900, "Alev Özavşar", "Yönetim", "Merkez", "VR3MCYHZRNT000104", 2028, 2, "Mavi"),
            A("59 ALV 110", "Toyota", "Hilux 2.4 4x4", 2021, "Alv Filo", "Pickup", "Dizel", 77400, "Saha · Burak Şen", "Kalite", "Çerkezköy", "MR0FB3CD700000110", 2027, 6, "Gümüş"),
            A("59 ALV 111", "Ford", "Transit 350L Panelvan", 2022, "Alv Filo", "Panelvan", "Dizel", 54300, "Depo · Kerem Aksoy", "Lojistik", "Çerkezköy", "WF0XXXTTGXN000111", 2027, 8, "Beyaz"),
            A("59 ALV 112", "Renault", "Clio 1.0 TCe", 2023, "Alv Filo", "Binek", "Benzin", 21800, "Gizem Tan", "Satış", "Çorlu", "VF1RJA00X69000112", 2028, 3, "Kırmızı"),
            A("59 ALV 113", "Mercedes-Benz", "Vito Tourer 116", 2022, "Alv Filo", "Minibüs", "Dizel", 68900, "Servis · İdari İşler", "İdari İşler", "Çerkezköy", "W1V44760300000113", 2027, 7, "Siyah"),
            A("34 DRN 500", "BMW", "320i", 2022, "DRN", "Binek", "Benzin", 41200, "Esra Kaya", "Dış Ticaret", "İstanbul", "WBA5R1100M000500", 2027, 9, "Lacivert"),
            A("34 URS 700", "Mercedes-Benz", "Actros 1845 Çekici", 2019, "Selvi Kimya", "Tır", "Dizel", 412000, "Şoför · Ramazan Öz", "Lojistik", "Çorlu", "WDB9634031L000700", 2026, 11, "Beyaz")
        };
        static ArArac A(string plaka, string marka, string model, int yil, string sirket, string tip, string yakit, int km, string zimmet, string dep, string lok, string sasi, int my, int mm, string renk) => new ArArac { Plaka = plaka, Marka = marka, Model = model, Yil = yil, Sirket = sirket, Tip = tip, Yakit = yakit, Km = km, Zimmet = zimmet, Departman = dep, Lokasyon = lok, Sasi = sasi, MuayeneBitis = new DateTime(my, mm, Math.Min(28, 10 + yil % 15)), Renk = renk, BakimAralikKm = tip == "Tır" ? 30000 : 15000 };
        public static ArArac AracBul(string plaka) => Araclar.FirstOrDefault(a => string.Equals(a.Plaka, plaka, StringComparison.OrdinalIgnoreCase));
        public static string[] Sirketler => Araclar.Select(a => a.Sirket).Distinct().ToArray();

        private static readonly List<ArBakim> _bakimlar = new List<ArBakim>();
        private static int _sonId, _sonDosya; private static bool _tohumlandi;

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var rnd = new Random(20261031); var bugun = DateTime.Today;
                (string Ad, string Tur, string Sehir) Servis(ArArac a, string tur)
                {
                    if (tur == "Lastik") return Servisler[9]; if (tur == "Kaporta / Boya" || tur == "Kaza Onarımı") return Servisler[10]; if (tur == "Muayene" || tur == "Egzoz Emisyon") return Servisler[11];
                    var yetkili = Servisler.FirstOrDefault(s => s.Tur == "Yetkili Servis" && s.Ad.StartsWith(a.Marka.Split('-')[0].Replace("Volkswagen", "VW").Replace("Fiat", "Tofaş").Replace("Mercedes", "Mercedes")));
                    if (a.Marka == "Mercedes-Benz") yetkili = Servisler[5]; if (a.Marka == "Volkswagen") yetkili = Servisler[3]; if (a.Marka == "Fiat") yetkili = Servisler[2];
                    return rnd.NextDouble() < 0.75 && yetkili.Ad != null ? yetkili : Servisler[8];
                }
                void Kalem(ArBakim b, string ad, string tur, decimal miktar, decimal fiyat) => b.Kalemler.Add(new ArKalem { Sira = b.Kalemler.Count + 1, Ad = ad, Tur = tur, Miktar = miktar, BirimFiyat = fiyat });
                foreach (var a in Araclar)
                {
                    int n = a.Tip == "Tır" || a.Km > 150000 ? 5 : a.Km > 60000 ? 4 : 3; int kmAdim = a.Km / (n + 1);
                    for (int i = 0; i < n; i++)
                    {
                        int gunOnce = (int)((n - i) * 150 + rnd.Next(-30, 30)); if (i == n - 1) gunOnce = rnd.Next(20, 90);
                        var tarih = bugun.AddDays(-gunOnce); int km = Math.Max(1000, a.Km - kmAdim * (n - i) + rnd.Next(-800, 800));
                        string tur = i % 2 == 0 ? "Periyodik Bakım" : new[] { "Arıza Onarımı", "Lastik", "Fren", "Akü", "Klima", "Kaza Onarımı", "Cam" }[rnd.Next(7)];
                        var sv = Servis(a, tur);
                        var b = new ArBakim { Id = ++_sonId, No = $"BKM-{tarih.Year}-{_sonId:0000}", Plaka = a.Plaka, Tarih = tarih, Tur = tur, Servis = sv.Ad, ServisTuru = sv.Tur, ServisSehir = sv.Sehir, Km = km, Durum = "Faturalandı", GirisTarihi = tarih.AddHours(9), CikisTarihi = tarih.AddHours(tur == "Kaza Onarımı" || tur == "Kaporta / Boya" ? 24 * rnd.Next(3, 10) + 17 : 17), Odeme = a.Sirket == "Alv Filo" ? "Filo Firması Faturası" : "Şirket Kredi Kartı", Sorumlu = Sorumlular[rnd.Next(Sorumlular.Length)], FaturaNo = $"SRV{tarih:yyMM}{rnd.Next(1000, 9999)}", FaturaTarihi = tarih.AddDays(1) };
                        switch (tur)
                        {
                            case "Periyodik Bakım": Kalem(b, "Motor yağı 5W-30 (5 lt)", "Parça", 1, a.Tip == "Tır" ? 3200 : 1850); Kalem(b, "Yağ filtresi", "Parça", 1, 420); Kalem(b, "Hava filtresi", "Parça", 1, 560); Kalem(b, "Polen filtresi", "Parça", 1, 480); if (i % 4 == 2) Kalem(b, "Yakıt filtresi", "Parça", 1, 890); Kalem(b, "Periyodik bakım işçiliği", "İşçilik", 1, a.Tip == "Tır" ? 4500 : 2400); b.Aciklama = $"{km:N0} km periyodik bakım; yağ, filtreler, genel kontrol"; b.SonrakiBakimKm = km + a.BakimAralikKm; b.SonrakiBakimTarihi = tarih.AddMonths(a.BakimAralikAy); b.GarantiAy = 6; break;
                            case "Arıza Onarımı": Kalem(b, "Triger seti / aktüatör", "Parça", 1, rnd.Next(4000, 12000)); Kalem(b, "Arıza tespiti ve onarım işçiliği", "İşçilik", rnd.Next(2, 5), 1200); b.Sikayet = "Motor arıza lambası, güç kaybı"; b.Aciklama = "Arıza teşhisi, parça değişimi ve yol testi"; b.GarantiAy = 12; break;
                            case "Lastik": Kalem(b, $"Lastik {(a.Tip == "Tır" ? "315/80 R22.5" : "205/55 R16")} ({(a.Tip == "Tır" ? "Michelin" : "Bridgestone")})", "Parça", 4, a.Tip == "Tır" ? 11500 : 3400); Kalem(b, "Balans ve rot ayarı", "Hizmet", 1, 900); b.Aciklama = "4 lastik değişimi, balans, rot ayarı"; b.GarantiAy = 24; break;
                            case "Fren": Kalem(b, "Ön fren balatası", "Parça", 1, 1650); Kalem(b, "Ön fren diski (2 adet)", "Parça", 2, 1900); Kalem(b, "Fren işçiliği", "İşçilik", 1, 1100); b.Sikayet = "Frende titreme, ses"; b.GarantiAy = 12; break;
                            case "Akü": Kalem(b, "Akü 72 Ah (Varta)", "Parça", 1, 3900); Kalem(b, "Montaj", "İşçilik", 1, 250); b.Sikayet = "Araç çalışmıyor"; b.GarantiAy = 24; break;
                            case "Klima": Kalem(b, "Klima gazı dolumu ve kaçak testi", "Hizmet", 1, 1400); Kalem(b, "Kurutucu filtre", "Parça", 1, 850); b.Sikayet = "Klima soğutmuyor"; b.GarantiAy = 6; break;
                            case "Kaza Onarımı": Kalem(b, "Ön tampon", "Parça", 1, 6800); Kalem(b, "Far (sağ)", "Parça", 1, 7400); Kalem(b, "Kaporta / boya işçiliği", "İşçilik", 1, 9500); b.Sikayet = "Park halinde çarpma"; b.Aciklama = "Kasko hasar dosyası ile onarım; muafiyet şirket"; b.Odeme = "Sigorta (Hasar)"; b.HasarDosyaNo = $"HSR-{tarih:yyyy}-{rnd.Next(10000, 99999)}"; b.GarantiAy = 12; break;
                            case "Cam": Kalem(b, "Ön cam (ısıtmalı, sensörlü)", "Parça", 1, 5200); Kalem(b, "Cam montaj", "İşçilik", 1, 700); b.Sikayet = "Taş çarpması, çatlak"; b.Odeme = "Sigorta (Hasar)"; b.HasarDosyaNo = $"HSR-{tarih:yyyy}-{rnd.Next(10000, 99999)}"; b.GarantiAy = 12; break;
                        }
                        b.Hareketler.Add(new ArHareket { Zaman = b.GirisTarihi.Value, Kullanici = b.Sorumlu, Islem = "Servise Giriş", Aciklama = $"{b.Servis} · {km:N0} km · {tur}" });
                        b.Hareketler.Add(new ArHareket { Zaman = b.CikisTarihi.Value, Kullanici = b.Sorumlu, Islem = "Tamamlandı", Aciklama = $"{b.Kalemler.Count} kalem · {b.Toplam.ToString("N2", Tr)} ₺ (KDV dahil)" });
                        b.Hareketler.Add(new ArHareket { Zaman = b.FaturaTarihi.Value.AddHours(11), Kullanici = "Muhasebe · Ayşe Demir", Islem = "Faturalandı", Aciklama = $"{b.FaturaNo} · {b.Odeme}" });
                        b.Dosyalar.Add(new ArDosya { No = ++_sonDosya, Tur = "Servis Faturası", Dosya = $"{b.FaturaNo}.pdf", Tip = "application/pdf", Boyut = rnd.Next(90, 400) * 1024L, Tarih = b.FaturaTarihi.Value, Kullanici = b.Sorumlu });
                        if (tur == "Periyodik Bakım") b.Dosyalar.Add(new ArDosya { No = ++_sonDosya, Tur = "Servis Fişi / Kontrol Listesi", Dosya = $"servis-fisi-{b.No}.pdf", Tip = "application/pdf", Boyut = rnd.Next(60, 200) * 1024L, Tarih = b.CikisTarihi.Value, Kullanici = b.Sorumlu });
                        _bakimlar.Add(b);
                    }
                }
                // sonraki bakım dağılımı gerçekçi olsun: araçların çoğu uygun, birkaçı yaklaşan (≤ 1.500 km), ikisi gecikmiş
                int idx = 0;
                foreach (var a in Araclar)
                {
                    var son = _bakimlar.Where(b => b.Plaka == a.Plaka && b.Tur == "Periyodik Bakım").OrderByDescending(b => b.Tarih).FirstOrDefault(); if (son == null) { idx++; continue; }
                    int fark = idx % 5 == 0 ? -(300 + rnd.Next(0, 900)) : idx % 5 == 1 ? 600 + rnd.Next(0, 800) : 2500 + rnd.Next(0, 9000);
                    son.SonrakiBakimKm = a.Km + fark; son.SonrakiBakimTarihi = idx % 5 == 0 ? bugun.AddDays(-rnd.Next(5, 40)) : bugun.AddDays(rnd.Next(60, 330)); idx++;
                }
                // açık kayıtlar: bir araç serviste, iki planlı bakım, bir yaklaşan muayene
                var s1 = AracBul("34 URS 103"); var bs = new ArBakim { Id = ++_sonId, No = $"BKM-{bugun.Year}-{_sonId:0000}", Plaka = s1.Plaka, Tarih = bugun.AddDays(-1), Tur = "Arıza Onarımı", Servis = Servisler[2].Ad, ServisTuru = "Yetkili Servis", ServisSehir = "Çorlu", Km = s1.Km, Durum = "Serviste", GirisTarihi = bugun.AddDays(-1).AddHours(9), Sikayet = "Şanzıman vites geçişlerinde sertlik", Aciklama = "Teşhis sürüyor; şanzıman yağı ve valf gövdesi kontrolü", Sorumlu = Sorumlular[0], Odeme = "Şirket Kredi Kartı" }; s1.Durum = "Serviste"; s1.Lokasyon = "Yetkili servis";
                bs.Kalemler.Add(new ArKalem { Sira = 1, Ad = "Şanzıman yağı değişimi (tahmini)", Tur = "Hizmet", Miktar = 1, BirimFiyat = 3800 }); bs.Hareketler.Add(new ArHareket { Zaman = bs.GirisTarihi.Value, Kullanici = bs.Sorumlu, Islem = "Servise Giriş", Aciklama = $"{bs.Servis} · {bs.Km:N0} km · arıza" }); _bakimlar.Add(bs);
                foreach (var (plaka, gun, tur) in new[] { ("34 URS 102", 6, "Periyodik Bakım"), ("41 URS 305", 12, "Periyodik Bakım"), ("34 URS 401", 20, "Muayene") })
                {
                    var a = AracBul(plaka); var sv = Servis(a, tur);
                    var bp = new ArBakim { Id = ++_sonId, No = $"BKM-{bugun.Year}-{_sonId:0000}", Plaka = plaka, Tarih = bugun.AddDays(gun), Tur = tur, Servis = sv.Ad, ServisTuru = sv.Tur, ServisSehir = sv.Sehir, Km = a.Km, Durum = "Planlandı", Sorumlu = Sorumlular[1], Aciklama = tur == "Muayene" ? "Periyodik araç muayenesi randevusu" : $"{a.Km + 900:N0} km'de periyodik bakım randevusu", Odeme = "Şirket Kredi Kartı" };
                    if (tur == "Periyodik Bakım") { bp.Kalemler.Add(new ArKalem { Sira = 1, Ad = "Periyodik bakım paketi (tahmini)", Tur = "Hizmet", Miktar = 1, BirimFiyat = 5200 }); } else bp.Kalemler.Add(new ArKalem { Sira = 1, Ad = "Muayene ücreti", Tur = "Hizmet", Miktar = 1, BirimFiyat = 1600 });
                    bp.Hareketler.Add(new ArHareket { Zaman = bugun.AddDays(-2).AddHours(14), Kullanici = bp.Sorumlu, Islem = "Planlandı", Aciklama = $"{bp.Servis} · {bp.Tarih:dd.MM.yyyy}" }); _bakimlar.Add(bp);
                }
            }
        }

        // ---------- erişim ----------
        public static List<ArBakim> Bakimlar { get { Tohumla(); lock (Kilit) return _bakimlar.OrderByDescending(b => b.Tarih).ThenByDescending(b => b.Id).ToList(); } }
        public static ArBakim Bul(int id) { Tohumla(); lock (Kilit) return _bakimlar.FirstOrDefault(b => b.Id == id); }
        static void Log(ArBakim b, string islem, string aciklama, string kullanici) => b.Hareketler.Add(new ArHareket { Zaman = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });

        /// <summary>Aracın bakım özeti: son bakım, sonraki bakım (km / tarih), kalan km / gün, toplam maliyet, yıl maliyeti, km başına maliyet, kayıt sayısı, muayene kalan.</summary>
        public static object AracOzet(ArArac a)
        {
            var l = Bakimlar.Where(b => b.Plaka == a.Plaka && b.Durum != "İptal").ToList(); var bugun = DateTime.Today;
            var sonBakim = l.Where(b => b.Tur == "Periyodik Bakım" && b.Asama >= 2).OrderByDescending(b => b.Tarih).FirstOrDefault();
            int? sonrakiKm = sonBakim?.SonrakiBakimKm; DateTime? sonrakiTarih = sonBakim?.SonrakiBakimTarihi; int? kalanKm = sonrakiKm.HasValue ? sonrakiKm - a.Km : null; int? kalanGun = sonrakiTarih.HasValue ? (int?)(sonrakiTarih.Value.Date - bugun).Days : null;
            string bakimDurum = a.Durum == "Serviste" ? "Serviste" : (kalanKm.HasValue && kalanKm <= 0) || (kalanGun.HasValue && kalanGun < 0) ? "Bakımı Gecikti" : (kalanKm.HasValue && kalanKm <= 1500) || (kalanGun.HasValue && kalanGun <= 30) ? "Bakım Yaklaşıyor" : l.Any(b => b.Durum == "Planlandı") ? "Bakım Planlı" : "Uygun";
            var yil = l.Where(b => b.Tarih.Year == bugun.Year && b.Asama >= 2).Sum(b => b.Toplam); var toplam = l.Where(b => b.Asama >= 2).Sum(b => b.Toplam);
            int muayeneKalan = (a.MuayeneBitis.Date - bugun).Days;
            return new { plaka = a.Plaka, kayit = l.Count, acik = l.Count(b => b.Acik), sonBakimTarihi = sonBakim?.Tarih.ToString("yyyy-MM-dd"), sonBakimKm = sonBakim?.Km, sonrakiBakimKm = sonrakiKm, sonrakiBakimTarihi = sonrakiTarih?.ToString("yyyy-MM-dd"), kalanKm, kalanGun, bakimDurum, yilMaliyet = yil, toplamMaliyet = toplam, kmBasina = a.Km > 0 ? Math.Round(toplam / a.Km, 2) : 0, kaza = l.Count(b => b.Tur == "Kaza Onarımı"), muayeneKalan, muayeneDurum = muayeneKalan < 0 ? "Muayene Gecikti" : muayeneKalan <= 30 ? "Muayene Yaklaşıyor" : "Muayene Geçerli", planli = l.Where(b => b.Durum == "Planlandı").OrderBy(b => b.Tarih).Select(b => new { b.Id, b.No, tarih = b.Tarih.ToString("yyyy-MM-dd"), b.Tur, b.Servis }).FirstOrDefault() };
        }
        public static object AracJson(ArArac a) => new { a.Plaka, a.Marka, a.Model, a.Yil, a.Sirket, a.Tip, a.Yakit, a.Km, a.Zimmet, a.Departman, a.Lokasyon, a.Durum, a.Sasi, muayeneBitis = a.MuayeneBitis.ToString("yyyy-MM-dd"), a.Renk, a.BakimAralikKm, a.BakimAralikAy, a.Not, ozet = AracOzet(a), sigorta = SigortaOrnek.AracPoliceleri(a.Plaka) };

        public static (bool ok, string mesaj, ArBakim b) Kaydet(ArBakimIstek i, string kullanici)
        {
            Tohumla(); if (i == null) return (false, "Geçersiz istek.", null);
            var a = AracBul(i.Plaka ?? ""); if (a == null) return (false, "Araç seçin.", null); if (!Turler.Contains(i.Tur ?? "")) return (false, "Bakım türü seçin.", null);
            if (!DateTime.TryParse(i.Tarih, out var tarih)) return (false, "Tarih girin.", null); if (string.IsNullOrWhiteSpace(i.Servis)) return (false, "Servis girin.", null);
            var kalemler = (i.Kalemler ?? new List<ArKalemIstek>()).Where(k => !string.IsNullOrWhiteSpace(k.Ad)).Select((k, idx) => new ArKalem { Sira = idx + 1, Ad = k.Ad.Trim(), Tur = KalemTurleri.Contains(k.Tur ?? "") ? k.Tur : "Parça", Miktar = k.Miktar <= 0 ? 1 : k.Miktar, BirimFiyat = Math.Max(0, k.BirimFiyat) }).ToList();
            lock (Kilit)
            {
                var b = i.Id.HasValue ? _bakimlar.FirstOrDefault(x => x.Id == i.Id.Value) : null; bool yeni = b == null;
                if (yeni) { b = new ArBakim { Id = ++_sonId, No = $"BKM-{tarih.Year}-{_sonId:0000}", Durum = Durumlar.Contains(i.Durum ?? "") ? i.Durum : "Planlandı" }; _bakimlar.Add(b); }
                else if (b.Asama >= 3) return (false, $"{b.No} faturalandı; kalemler değiştirilemez.", b);
                b.Plaka = a.Plaka; b.Tarih = tarih.Date; b.Tur = i.Tur; b.Servis = i.Servis.Trim(); b.ServisTuru = ServisTurleri.Contains(i.ServisTuru ?? "") ? i.ServisTuru : b.ServisTuru; b.ServisSehir = (i.ServisSehir ?? b.ServisSehir ?? "").Trim(); b.Km = i.Km > 0 ? i.Km : a.Km; b.Sikayet = (i.Sikayet ?? "").Trim(); b.Aciklama = (i.Aciklama ?? "").Trim(); b.Kalemler = kalemler; b.Odeme = Odemeler.Contains(i.Odeme ?? "") ? i.Odeme : b.Odeme; if (!string.IsNullOrWhiteSpace(i.FaturaNo)) b.FaturaNo = i.FaturaNo.Trim();
                b.SonrakiBakimKm = i.SonrakiBakimKm ?? (i.Tur == "Periyodik Bakım" ? b.Km + a.BakimAralikKm : b.SonrakiBakimKm); b.SonrakiBakimTarihi = DateTime.TryParse(i.SonrakiBakimTarihi, out var sb) ? sb.Date : (i.Tur == "Periyodik Bakım" ? tarih.AddMonths(a.BakimAralikAy) : b.SonrakiBakimTarihi); b.GarantiAy = i.GarantiAy ?? b.GarantiAy; b.HasarDosyaNo = (i.HasarDosyaNo ?? b.HasarDosyaNo ?? "").Trim(); b.Sorumlu = string.IsNullOrWhiteSpace(i.Sorumlu) ? (b.Sorumlu == "" ? kullanici : b.Sorumlu) : i.Sorumlu.Trim(); b.Not = (i.Not ?? "").Trim();
                if (b.Km > a.Km) a.Km = b.Km;
                Log(b, yeni ? "Kayıt Açıldı" : "Güncellendi", $"{a.Plaka} · {b.Tur} · {b.Servis} · {b.Km:N0} km · {b.Kalemler.Count} kalem · {b.Toplam.ToString("N2", Tr)} ₺", kullanici);
                return (true, $"{b.No} {(yeni ? "açıldı" : "güncellendi")}: {a.Plaka} · {b.Tur} · {b.Servis}.", b);
            }
        }
        static readonly Dictionary<string, string[]> Gecisler = new Dictionary<string, string[]> { ["Planlandı"] = new[] { "Serviste", "İptal" }, ["Serviste"] = new[] { "Tamamlandı", "Planlandı", "İptal" }, ["Tamamlandı"] = new[] { "Faturalandı", "Serviste" }, ["Faturalandı"] = new string[0], ["İptal"] = new[] { "Planlandı" } };
        public static string[] Sonraki(ArBakim b) => Gecisler.TryGetValue(b.Durum, out var g) ? g : new string[0];
        public static (bool ok, string mesaj, ArBakim b) DurumDegistir(ArDurumIstek i, string kullanici)
        {
            var b = Bul(i?.Id ?? 0); if (b == null) return (false, "Kayıt bulunamadı.", null); if (!Sonraki(b).Contains(i.Durum ?? "")) return (false, $"{b.Durum} → {i.Durum} geçişi yapılamaz.", b);
            var a = AracBul(b.Plaka);
            lock (Kilit)
            {
                string eski = b.Durum; b.Durum = i.Durum;
                if (i.Durum == "Serviste") { b.GirisTarihi = DateTime.Now; if (a != null) { a.Durum = "Serviste"; a.Lokasyon = b.Servis; } }
                if (i.Durum == "Tamamlandı") { b.CikisTarihi = DateTime.Now; if (i.CikisKm.HasValue && i.CikisKm > 0) { b.Km = i.CikisKm.Value; if (a != null && a.Km < b.Km) a.Km = b.Km; } if (a != null) { a.Durum = "Aktif"; a.Lokasyon = a.Departman == "Lojistik" ? "Gebze" : "Merkez"; } }
                if (i.Durum == "Faturalandı") { b.FaturaNo = string.IsNullOrWhiteSpace(i.FaturaNo) ? $"SRV{DateTime.Today:yyMM}{b.Id:0000}" : i.FaturaNo.Trim(); b.FaturaTarihi = DateTime.Today; }
                if ((i.Durum == "İptal" || i.Durum == "Planlandı") && a != null && a.Durum == "Serviste" && !_bakimlar.Any(x => x.Plaka == a.Plaka && x.Id != b.Id && x.Durum == "Serviste")) { a.Durum = "Aktif"; }
                Log(b, i.Durum, $"{eski} → {i.Durum}" + (i.Durum == "Faturalandı" ? $" · {b.FaturaNo}" : "") + (i.CikisKm.HasValue ? $" · çıkış {i.CikisKm:N0} km" : "") + (string.IsNullOrWhiteSpace(i.Not) ? "" : " · " + i.Not.Trim()), kullanici);
            }
            return (true, $"{b.No}: {i.Durum}.", b);
        }
        public static (bool ok, string mesaj, ArBakim b) DosyaEkle(int id, string tur, string dosyaAdi, string icerikTipi, byte[] veri, string kullanici)
        {
            var b = Bul(id); if (b == null) return (false, "Kayıt bulunamadı.", null); if (veri == null || veri.Length == 0) return (false, "Dosya seçin.", b); if (veri.LongLength > 12_000_000) return (false, "Dosya 12 MB'ı aşamaz.", b);
            lock (Kilit) { b.Dosyalar.Add(new ArDosya { No = ++_sonDosya, Tur = string.IsNullOrWhiteSpace(tur) ? "Belge" : tur.Trim(), Dosya = dosyaAdi, Tip = icerikTipi ?? "application/octet-stream", Boyut = veri.LongLength, Veri = veri, Tarih = DateTime.Now, Kullanici = kullanici }); Log(b, "Dosya Yüklendi", $"{tur}: {dosyaAdi} · {veri.LongLength / 1024} KB", kullanici); }
            return (true, $"{b.No}: {dosyaAdi} yüklendi.", b);
        }
        public static (bool ok, string mesaj) AracGuncelle(ArAracIstek i, string kullanici)
        {
            var a = AracBul(i?.Plaka ?? ""); if (a == null) return (false, "Araç bulunamadı.");
            lock (Kilit) { if (i.Km.HasValue && i.Km > 0) a.Km = i.Km.Value; if (!string.IsNullOrWhiteSpace(i.Zimmet)) a.Zimmet = i.Zimmet.Trim(); if (!string.IsNullOrWhiteSpace(i.Lokasyon)) a.Lokasyon = i.Lokasyon.Trim(); if (!string.IsNullOrWhiteSpace(i.Durum)) a.Durum = i.Durum; if (DateTime.TryParse(i.MuayeneBitis, out var m)) a.MuayeneBitis = m.Date; if (i.Not != null) a.Not = i.Not.Trim(); }
            return (true, $"{a.Plaka} güncellendi · {a.Km:N0} km · {a.Zimmet}.");
        }

        public static object Json(ArBakim b)
        {
            var a = AracBul(b.Plaka);
            return new
            {
                b.Id, no = b.No, plaka = b.Plaka, arac = a == null ? "" : $"{a.Marka} {a.Model}", sirket = a?.Sirket, zimmet = a?.Zimmet, tarih = b.Tarih.ToString("yyyy-MM-dd"), tur = b.Tur, servis = b.Servis, servisTuru = b.ServisTuru, servisSehir = b.ServisSehir, km = b.Km, sikayet = b.Sikayet, aciklama = b.Aciklama,
                kalemler = b.Kalemler.Select(k => new { k.Sira, k.Ad, k.Tur, k.Miktar, k.BirimFiyat, k.Tutar }).ToList(), parca = b.ParcaTutar, iscilik = b.IscilikTutar, tutar = b.Tutar, kdv = b.Kdv, toplam = b.Toplam,
                faturaNo = b.FaturaNo, faturaTarihi = b.FaturaTarihi?.ToString("yyyy-MM-dd"), odeme = b.Odeme, durum = b.Durum, asama = b.Asama, acik = b.Acik, giris = b.GirisTarihi?.ToString("yyyy-MM-dd HH:mm"), cikis = b.CikisTarihi?.ToString("yyyy-MM-dd HH:mm"), sureSaat = b.GirisTarihi != null && b.CikisTarihi != null ? Math.Round((b.CikisTarihi.Value - b.GirisTarihi.Value).TotalHours, 1) : (double?)null,
                sonrakiBakimKm = b.SonrakiBakimKm, sonrakiBakimTarihi = b.SonrakiBakimTarihi?.ToString("yyyy-MM-dd"), garantiAy = b.GarantiAy, garantiBitis = b.GarantiAy > 0 && b.CikisTarihi != null ? b.CikisTarihi.Value.AddMonths(b.GarantiAy).ToString("yyyy-MM-dd") : null, hasarDosyaNo = b.HasarDosyaNo, sorumlu = b.Sorumlu, not = b.Not, sonraki = Sonraki(b),
                dosyalar = b.Dosyalar.Select(d => new { d.No, d.Tur, d.Dosya, d.Tip, d.Boyut, tarih = d.Tarih.ToString("yyyy-MM-dd HH:mm"), d.Kullanici, gercek = d.Veri != null }).ToList(),
                hareketler = b.Hareketler.OrderByDescending(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("yyyy-MM-dd HH:mm"), h.Kullanici, h.Islem, h.Aciklama }).ToList()
            };
        }
        public static object Ozet()
        {
            var l = Bakimlar.Where(b => b.Durum != "İptal").ToList(); var bugun = DateTime.Today; var ozetler = Araclar.Select(a => (dynamic)AracOzet(a)).ToList();
            return new
            {
                arac = Araclar.Count, serviste = Araclar.Count(a => a.Durum == "Serviste"), planli = l.Count(b => b.Durum == "Planlandı"), bakimYaklasan = ozetler.Count(o => (string)o.bakimDurum == "Bakım Yaklaşıyor"), bakimGeciken = ozetler.Count(o => (string)o.bakimDurum == "Bakımı Gecikti"), muayeneYaklasan = Araclar.Count(a => (a.MuayeneBitis.Date - bugun).Days <= 30),
                yilMaliyet = l.Where(b => b.Tarih.Year == bugun.Year && b.Asama >= 2).Sum(b => b.Toplam), ayMaliyet = l.Where(b => b.Tarih.Year == bugun.Year && b.Tarih.Month == bugun.Month && b.Asama >= 2).Sum(b => b.Toplam), kaza = l.Count(b => b.Tur == "Kaza Onarımı" && b.Tarih.Year == bugun.Year), toplamKm = Araclar.Sum(a => a.Km),
                aylar = Enumerable.Range(0, 12).Select(i => { var ay = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-11 + i); return new { ay = ay.ToString("yyyy-MM"), ad = ay.ToString("MMM yy", Tr), tutar = l.Where(b => b.Asama >= 2 && b.Tarih.Year == ay.Year && b.Tarih.Month == ay.Month).Sum(b => b.Toplam), adet = l.Count(b => b.Tarih.Year == ay.Year && b.Tarih.Month == ay.Month) }; }).ToList(),
                turler = Turler.Select(t => new { tur = t, adet = l.Count(b => b.Tur == t), tutar = l.Where(b => b.Tur == t && b.Asama >= 2).Sum(b => b.Toplam) }).Where(x => x.adet > 0).OrderByDescending(x => x.tutar).ToList()
            };
        }
    }
}
