using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>
    /// VADE TARİHİNE GÖRE ÇEK ÖZETİ örnek verisi — Mali İşler › Finans › Finansal Araçlar (Yönetim › Özel Raporlar'da aynı ekran).
    /// "2026 Vade Tarihine Göre Tedarikçi Aylık Çek Özeti" tablosunun uyarlaması: şirketin VERDİĞİ çekler (tedarikçi — sigorta, OSB aidatı vb. dahil —, maaş, kira)
    /// vade ayına göre cari bazında; müşteriden ALINAN ve tahsile gönderilen çekler Data/ButceOrnek.cs › Cekler listesinden (Bütçe Raporu ile aynı kaynak).
    /// Uras Kimya 2026 tedarikçi çekleri "2026 Bütçe Analizi" tablosuyla (Vade Tarihine Göre Tedarikçi Aylık Çek Özeti) BİREBİR: Gercek2026 (cari × ay, kuruşlu) ve
    /// GercekAdet2026 (ayın çek adedi) — dolu hücreler o ayın adedine ulaşacak şekilde çeke bölünür. Diğer şirket / yıllar aynı tablodan türetilen havuzla üretilir:
    /// yıllık toplam × şirket ölçeği × yıl çarpanı, aktif aylara ağırlıklı dağıtım, ayda 1–3 çek; maaş çeki bütçedeki brüt maaş kaleminin %30'u, kira çeki bütçedeki
    /// ofis kirası kalemi (Data/ButceOrnek.cs). Durum vadeye göre: Ödendi / Ödenecek / İade Alındı (birebir tabloda iade yok).
    /// </summary>
    public class VcCek
    {
        public string Sirket { get; set; }
        public string No { get; set; }           // GAR-1234567 (keşideci banka kısaltması)
        public string CariKod { get; set; }      // cari kart kodu (320.xxx) ya da boş (kartı olmayan alıcı)
        public string Cari { get; set; }
        public string Tur { get; set; }          // Tedarikçi | Maaş | Kira
        public string Banka { get; set; }        // çekin keşide edildiği banka (şirket hesabı)
        public DateTime Keside { get; set; }
        public DateTime Vade { get; set; }
        public decimal Tutar { get; set; }
        public string Durum { get; set; }        // Ödenecek | Ödendi | İade Alındı
        public string Aciklama { get; set; }
        public string Belge { get; set; } = "Çek";   // Çek | Senet (verilen senetler: tedarikçi alıcılarının bir kısmına, çek hacminin ~%30'u)
    }

    public static class CekOrnek
    {
        public static readonly object Kilit = new object();
        public static readonly string[] Turler = { "Tedarikçi", "Maaş", "Kira" };   // sigorta, OSB aidatı vb. alıcılar tedarikçi listesindedir
        public static readonly string[] Durumlar = { "Ödenecek", "Ödendi", "İade Alındı" };
        public static string[] Sirketler => TeklifOrnek.Sirketler;
        public static int[] Yillar => ButceOrnek.Yillar;
        /// <summary>Açılışta seçili şirket (kullanıcının şirketi): 2026 verilen tedarikçi çekleri Gercek2026 tablosuyla birebirdir.</summary>
        public static string VarsayilanSirket => Array.IndexOf(Sirketler, "Uras Kimya") >= 0 ? "Uras Kimya" : Sirketler[0];
        /// <summary>Birebir tablonun yılı (2026 Bütçe Analizi).</summary>
        public const int GercekYil = 2026;
        static readonly string[] Bankalar = { "Garanti BBVA", "İş Bankası", "Ziraat Bankası", "QNB", "Yapı Kredi", "Akbank" };
        static readonly string[] BankaKisa = { "GAR", "ISB", "ZRT", "QNB", "YKB", "AKB" };

        /// <summary>
        /// "2026 Bütçe Analizi" › 2026 Vade Tarihine Göre Tedarikçi Aylık Çek Özeti — Uras Kimya, BİREBİR (TL, kuruşlu; 0 = o ay çek yok; tablodaki sıra).
        /// Aylık toplamlar: 7.617.764,71 · 10.221.192,56 · 5.722.460,05 · 9.588.940,51 · 10.423.370,11 · 8.503.477,37 · 15.002.030,84 · 15.518.506,46 · 12.656.418,22 ·
        /// 12.003.190,65 · 5.995.023,26 · 3.254.151,13 = 116.506.525,87. Öztürk Plastik satırının tablodaki 12 aylık toplamı (16.077.249,99) hücreleriyle (16.554.524,49)
        /// uyuşmaz; aylık toplamlar ve genel toplam hücrelerle tutarlı olduğundan hücreler esas alındı. Çeksiz 18 cari (Armağan Boya … STL) tabloda var, çek üretmez.
        /// </summary>
        static readonly (string Ad, decimal[] Aylar)[] Gercek2026 =
        {
            ("Öztürk Plastik", new decimal[] { 1587000, 782064, 817871.64m, 1042062.4m, 1784939.45m, 445000, 2316000, 1290000, 1993000, 2240000, 880000, 1376587 }),
            ("Arslan Güneydoğu", new decimal[] { 3165000, 3528067.14m, 450000, 2350000, 1000000, 0, 0, 0, 0, 0, 0, 0 }),
            ("Plastay", new decimal[] { 700000, 300000, 789876, 1849000, 60000, 2010000, 1170000, 0, 943757.27m, 700000, 52505, 0 }),
            ("Değişim Sigorta", new decimal[] { 60000, 1400000, 646483.11m, 1038803.54m, 375383.2m, 0, 544677.6m, 1466707.82m, 300000, 300000, 709808.26m, 660000 }),
            ("Kimteks", new decimal[] { 0, 1480183.82m, 0, 0, 1550000, 0, 1300000, 600000, 0, 1000000, 151000, 0 }),
            ("Derin Kimya", new decimal[] { 425000, 595000, 493428.7m, 670000, 856500, 460000, 0, 150000, 80000, 136952, 1058992, 0 }),
            ("Melkim Kimya", new decimal[] { 154438.25m, 481627.6m, 0, 744000, 0, 220000, 1281000, 0, 380000, 740000, 0, 0 }),
            ("Globus", new decimal[] { 0, 0, 400000, 30102, 700000, 0, 250000, 0, 969000, 35641, 0, 400000 }),
            ("Chemplast", new decimal[] { 0, 338000, 0, 0, 230601.37m, 350000, 1334755, 425000, 0, 0, 0, 0 }),
            ("Proses", new decimal[] { 0, 0, 500000, 190000, 90000, 320000, 0, 550000, 90000, 520000, 200000, 0 }),
            ("Atlaschem", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 1000000, 250000, 1000000, 0, 0 }),
            ("Karınca Lojistik", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 69624, 226379.33m, 0, 1600000, 0 }),
            ("Opera Catering", new decimal[] { 0, 0, 0, 0, 0, 0, 300000, 419599.2m, 367000, 690000, 0, 0 }),
            ("Gökaş Yemek", new decimal[] { 350000, 0, 350000, 403242.6m, 412119.9m, 200000, 0, 0, 0, 0, 0, 0 }),
            ("Narcon", new decimal[] { 250000, 250000, 0, 0, 0, 0, 0, 0, 600000, 520000, 0, 0 }),
            ("Plastiksan", new decimal[] { 0, 185000, 400000, 0, 226704.83m, 0, 0, 227000, 325919, 200000, 0, 0 }),
            ("Veskim", new decimal[] { 0, 0, 0, 0, 400000, 209680.87m, 700000, 0, 0, 0, 0, 0 }),
            ("CC Mekotranik", new decimal[] { 0, 0, 0, 0, 640000, 600000, 0, 0, 0, 0, 0, 0 }),
            ("Kuzey Personel", new decimal[] { 0, 0, 0, 0, 0, 504000, 0, 329000, 400000, 0, 0, 0 }),
            ("Ventera", new decimal[] { 23550, 580000, 0, 120000, 0, 200000, 0, 0, 295000, 0, 0, 0 }),
            ("CRN Boya", new decimal[] { 0, 0, 0, 0, 0, 0, 810000, 300000, 0, 0, 0, 0 }),
            ("Bulutpark", new decimal[] { 0, 0, 0, 0, 0, 0, 140000, 0, 0, 600000, 161246, 122305.3m }),
            ("Balkan Geri Dönüşüm", new decimal[] { 0, 0, 65765, 0, 105000, 86220.6m, 200000, 71452.8m, 172000, 0, 192000, 0 }),
            ("Arkem", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 887151, 0, 0, 0, 0 }),
            ("HCC İstanbul", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 858498.94m, 0, 0, 0, 0 }),
            ("ATS", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 834530.31m, 0, 0, 0, 0 }),
            ("TF Gümrük", new decimal[] { 210062, 0, 0, 0, 0, 100000, 203000, 150000, 0, 122916, 0, 0 }),
            ("Nora Çevre", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 525000, 256920 }),
            ("Deri OSB", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 760000, 0, 0, 0, 0 }),
            ("Bizim Emlak", new decimal[] { 0, 0, 0, 0, 0, 545000, 0, 176000, 0, 0, 0, 0 }),
            ("A3 Gıda Catering", new decimal[] { 0, 0, 0, 0, 0, 100000, 185000, 0, 0, 220000, 0, 213338.83m }),
            ("Azelis", new decimal[] { 0, 0, 0, 0, 0, 471000, 200047.99m, 0, 0, 0, 0, 0 }),
            ("Mars Lojistik", new decimal[] { 0, 0, 670935.6m, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
            ("Egesil Kimya", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 600000, 70000, 0, 0, 0 }),
            ("Cantat Tabldot", new decimal[] { 0, 0, 0, 0, 0, 0, 386000, 280305, 0, 0, 0, 0 }),
            ("HT Zirai", new decimal[] { 0, 0, 0, 0, 0, 65000, 134566, 457195.02m, 0, 0, 0, 0 }),
            ("Erdal Özcan", new decimal[] { 0, 0, 0, 0, 0, 0, 540000, 0, 97000, 0, 0, 0 }),
            ("Çınar Orman Ürünleri", new decimal[] { 157181.16m, 0, 0, 8405.39m, 124270, 114000, 0, 0, 80000, 117960, 0, 0 }),
            ("Makrosel", new decimal[] { 0, 0, 0, 300000, 0, 0, 88339.66m, 0, 0, 60000, 103438, 0 }),
            ("Güveniş", new decimal[] { 0, 0, 0, 0, 75186.36m, 163950.6m, 88000, 0, 100268.4m, 107281.2m, 0, 0 }),
            ("Aymek Matbaa", new decimal[] { 0, 126250, 0, 0, 65270, 0, 75000, 191411.82m, 0, 40000, 26000, 0 }),
            ("İsmail Uluğ", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 500000, 0, 0 }),
            ("Papex", new decimal[] { 150000, 0, 0, 158842.78m, 137008, 0, 40000, 0, 0, 0, 0, 0 }),
            ("Express Ofis", new decimal[] { 0, 0, 0, 0, 125000, 200000, 0, 0, 29268, 95554.15m, 0, 0 }),
            ("Deniz Boya", new decimal[] { 0, 0, 0, 0, 0, 0, 231372, 0, 200400, 0, 0, 0 }),
            ("Doğuş Palet", new decimal[] { 0, 25000, 0, 93000, 0, 94630, 0, 0, 113849.94m, 40000, 61000, 0 }),
            ("CTS", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 427170.58m, 0, 0, 0 }),
            ("Garanti İstif", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 268370.67m, 140000, 0, 0 }),
            ("Onur Kompresör", new decimal[] { 0, 0, 0, 0, 0, 0, 400000, 0, 0, 0, 0, 0 }),
            ("Flomak Tekstil", new decimal[] { 0, 0, 0, 0, 100000, 0, 281186.21m, 0, 0, 0, 0, 0 }),
            ("Cevatoğlu", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 380377.96m, 0, 0, 0, 0 }),
            ("Şahin Oto", new decimal[] { 0, 0, 138100, 0, 0, 0, 0, 0, 180000, 60000, 0, 0 }),
            ("Deniz Grup Kimya", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 25000, 350000, 0, 0, 0 }),
            ("Türkmen Lojistik", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 371500, 0, 0, 0 }),
            ("HT Agro", new decimal[] { 0, 0, 0, 352553.8m, 0, 0, 0, 0, 0, 0, 0, 0 }),
            ("Cem Zengin", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 350000, 0, 0, 0 }),
            ("Elite Mühendislik", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 250000, 0, 100000, 0, 0 }),
            ("Yeditepe Gıda", new decimal[] { 0, 0, 0, 0, 350000, 0, 0, 0, 0, 0, 0, 0 }),
            ("Abdullah Tanış", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 75485, 165502.4m, 105140, 0 }),
            ("Cevdet Şefik", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 206000, 137191.6m, 0, 0, 0 }),
            ("Ümit Atbakar", new decimal[] { 0, 0, 0, 50000, 0, 0, 250000, 40000, 0, 0, 0, 0 }),
            ("Azelya", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 335000, 0, 0, 0 }),
            ("Ülker Anadolu", new decimal[] { 0, 0, 0, 0, 0, 0, 334667.19m, 0, 0, 0, 0, 0 }),
            ("Cargo Partner", new decimal[] { 0, 0, 0, 0, 330000, 0, 0, 0, 0, 0, 0, 0 }),
            ("Kenan Yılmaz", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 250000, 77000, 0, 0, 0 }),
            ("Meka Nakliyat", new decimal[] { 0, 0, 0, 0, 0, 0, 100000, 225000, 0, 0, 0, 0 }),
            ("Mustafa Sert", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 279590, 39612.25m, 0, 0, 0 }),
            ("Fix Antrepo", new decimal[] { 0, 0, 0, 0, 85000, 0, 0, 0, 0, 230214.8m, 0, 0 }),
            ("Sedef Yılmaz", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 300570.38m, 0, 0 }),
            ("Nurcan Çiçekbaşı", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 282000, 0, 0, 0 }),
            ("Över Petrol", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 276628.72m, 0, 0 }),
            ("Kardeşler Elekt", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 29231.17m, 244970, 0, 0 }),
            ("Logist Kargo", new decimal[] { 0, 0, 0, 0, 0, 102760.09m, 0, 0, 150000, 0, 0, 0 }),
            ("Belgin Aras", new decimal[] { 150000, 100000, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
            ("Ertan Demir", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 250000, 0, 0, 0, 0 }),
            ("Omya Sigorta Şirketi", new decimal[] { 0, 0, 0, 0, 0, 250000, 0, 0, 0, 0, 0, 0 }),
            ("Veli Meşe OSB", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 249000, 0, 0 }),
            ("Kimpaz", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 239697.79m, 0, 0, 0 }),
            ("Likit Kimya", new decimal[] { 235533.3m, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }),
            ("Frimpeks", new decimal[] { 0, 0, 0, 0, 224000, 0, 0, 0, 0, 0, 0, 0 }),
            ("Çaya Nakliyat", new decimal[] { 0, 0, 0, 0, 40229, 0, 0, 0, 172956, 0, 0, 0 }),
            ("Cemre Yaz Alacahanlı", new decimal[] { 0, 0, 0, 0, 0, 0, 80405.6m, 129711, 0, 0, 0, 0 }),
            ("HGL Gümrük", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 150000, 55000, 0, 0, 0 }),
            ("Hakel Ambalaj", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 202405.59m, 0, 0, 0, 0 }),
            ("Özçiftlik", new decimal[] { 0, 0, 0, 0, 0, 0, 201937.65m, 0, 0, 0, 0, 0 }),
            ("CYC", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 200000, 0, 0, 0 }),
            ("Hira Oto", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 200000, 0, 0, 0 }),
            ("IMCD", new decimal[] { 0, 0, 0, 0, 0, 100000, 0, 100000, 0, 0, 0, 0 }),
            ("SG Bant", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 200000, 0, 0, 0, 0 }),
            ("Uzun Otomotiv", new decimal[] { 0, 0, 0, 0, 0, 200000, 0, 0, 0, 0, 0, 0 }),
            ("Elze Kurumsal", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 41500, 150000, 0, 0, 0 }),
            ("Koray Kimya", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 35000, 145000 }),
            ("Tarkan Reklam", new decimal[] { 0, 0, 0, 0, 100000, 79453.6m, 0, 0, 0, 0, 0, 0 }),
            ("Dozak", new decimal[] { 0, 0, 0, 65000, 0, 0, 62575, 50000, 0, 0, 0, 0 }),
            ("Section Turizm", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 88446, 85294.6m, 0, 0, 0 }),
            ("Çelik Ambalaj", new decimal[] { 0, 0, 0, 0, 70224, 0, 0, 0, 46227.12m, 50000, 0, 0 }),
            ("Zekeriya Aydın", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 157000, 0, 0, 0, 0 }),
            ("Yalın Grup", new decimal[] { 0, 50000, 0, 23928, 0, 0, 0, 0, 80000, 0, 0, 0 }),
            ("Eykim Gıda ve Kimya San. Dış Tic. Ltd.", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 150000, 0, 0, 0, 0 }),
            ("Metin Karatekin", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 150000, 0, 0, 0, 0 }),
            ("Tayfun Akel", new decimal[] { 0, 0, 0, 0, 0, 0, 150000, 0, 0, 0, 0, 0 }),
            ("Yıldıray Toprak", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 150000, 0, 0 }),
            ("Aytekin Beyhan", new decimal[] { 0, 0, 0, 0, 0, 0, 140000, 0, 0, 0, 0, 0 }),
            ("MSF Vidanjör", new decimal[] { 0, 0, 0, 0, 0, 0, 140000, 0, 0, 0, 0, 0 }),
            ("Gelişim Solar", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 50000, 0, 80000 }),
            ("Qural Denetim", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 130000, 0, 0, 0 }),
            ("Muhteşem Vinç", new decimal[] { 0, 0, 0, 0, 0, 122288.3m, 0, 0, 0, 0, 0, 0 }),
            ("Koza", new decimal[] { 0, 0, 0, 0, 0, 50000, 70621.9m, 0, 0, 0, 0, 0 }),
            ("Turkuaz Toner", new decimal[] { 0, 0, 0, 0, 60000, 51011, 0, 0, 0, 0, 0, 0 }),
            ("MG Elektronik", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 103894, 0 }),
            ("AS Rekor", new decimal[] { 0, 0, 0, 100000, 0, 0, 0, 0, 0, 0, 0, 0 }),
            ("Felxuapp", new decimal[] { 0, 0, 0, 0, 0, 0, 100000, 0, 0, 0, 0, 0 }),
            ("Rotabel", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 100000, 0, 0, 0, 0 }),
            ("Arı Grup", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 91839.5m, 0, 0, 0 }),
            ("Beach Soccer", new decimal[] { 0, 0, 0, 0, 0, 89482.31m, 0, 0, 0, 0, 0, 0 }),
            ("Ünal Soğutma", new decimal[] { 0, 0, 0, 0, 75000, 0, 0, 0, 0, 0, 0, 0 }),
            ("Detay Oto", new decimal[] { 0, 0, 0, 0, 0, 0, 69599.04m, 0, 0, 0, 0, 0 }),
            ("SZU Test", new decimal[] { 0, 0, 0, 0, 0, 0, 58370, 0, 0, 0, 0, 0 }),
            ("Dalgakıran", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 50000, 0, 0, 0 }),
            ("Keçe Test", new decimal[] { 0, 0, 0, 0, 0, 0, 44910, 0, 0, 0, 0, 0 }),
            ("General Kargo", new decimal[] { 0, 0, 0, 0, 30934, 0, 0, 0, 0, 0, 0, 0 }),
            ("Nadir Etiket", new decimal[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 30000, 0 }),
            ("Armağan Boya", new decimal[12]), ("Beyhan Teknik", new decimal[12]), ("Bilgin Mühendislik", new decimal[12]), ("Deepchem", new decimal[12]), ("Depar", new decimal[12]),
            ("Doğan Klişe", new decimal[12]), ("Global IT", new decimal[12]), ("Günalsan Pompa", new decimal[12]), ("HTChem", new decimal[12]), ("Kater Masura", new decimal[12]),
            ("Kimya Park", new decimal[12]), ("Makro Ambalaj", new decimal[12]), ("Metropol Yangın", new decimal[12]), ("NHR", new decimal[12]), ("Origin KS", new decimal[12]),
            ("Orijin KÖ", new decimal[12]), ("Ozan Ambalaj", new decimal[12]), ("STL", new decimal[12])
        };
        /// <summary>Tablonun AYLIK ÇEK ADEDİ satırı (toplam 557): her ay dolu hücreler bu adede ulaşacak şekilde çeke bölünür.</summary>
        static readonly int[] GercekAdet2026 = { 32, 39, 31, 42, 53, 45, 66, 70, 70, 61, 30, 18 };

        /// <summary>Alıcı havuzu (diğer şirket / yıllar için): birebir tablodan türetilir — ad, tür, Uras Kimya ölçeğinde yıllık çek tutarı, çek verilen ay sayısı. Kartı olan tedarikçiler cari koduyla eklenir.</summary>
        static readonly (string Ad, string Tur, decimal Yillik, int AyAdedi)[] Havuz = Gercek2026.Select(r => (r.Ad, "Tedarikçi", r.Aylar.Sum(), r.Aylar.Count(x => x > 0))).ToArray();
        /// <summary>Kira alıcıları: ofis kirası (bütçe SG-07) çeki her ay (tablodaki tedarikçi adlarıyla çakışmayan alıcı).</summary>
        static readonly (string Ad, string ButceKalemi)[] Kiralar = { ("Ofis Kirası · Mal Sahibi", "SG-07") };

        private static readonly List<VcCek> _verilen = new List<VcCek>();
        private static bool _tohumlandi;
        private static int Seed(string s) => s.Aggregate(17, (a, ch) => unchecked(a * 31 + ch));
        public static List<VcCek> Verilen { get { Tohumla(); lock (Kilit) return _verilen.ToList(); } }

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                foreach (var yil in Yillar) foreach (var sirket in Sirketler) _verilen.AddRange(Uret(sirket, yil));
                _verilen.Sort((a, b) => a.Vade != b.Vade ? a.Vade.CompareTo(b.Vade) : string.CompareOrdinal(a.No, b.No));
            }
        }

        private static string Durum(DateTime vade, Random rnd, DateTime bugun) => vade < bugun ? (rnd.Next(40) == 0 ? "İade Alındı" : "Ödendi") : "Ödenecek";

        private static IEnumerable<VcCek> Uret(string sirket, int yil)
        {
            var rnd = new Random(Seed("verilen|" + sirket + "|" + yil)); var bugun = DateTime.Today;
            decimal olcek = ButceOrnek.Olcek.TryGetValue(sirket, out var o) ? o : 0.3m, yilC = ButceOrnek.YilCarpani.TryGetValue(yil, out var y) ? y : 1m;
            var liste = new List<VcCek>();
            int bankaBas = Array.IndexOf(Sirketler, sirket) % Bankalar.Length;
            string Banka(int i) => Bankalar[(bankaBas + i) % Bankalar.Length];
            string No(int bi, string belge) => belge == "Senet" ? $"SN-{rnd.Next(100000, 999999)}" : $"{BankaKisa[(bankaBas + bi) % Bankalar.Length]}-{rnd.Next(1000000, 9999999)}";
            void Ekle(string cariKod, string cari, string tur, DateTime vade, decimal tutar, string aciklama, string belge = "Çek", string durum = null)
            {
                int bi = rnd.Next(3); var keside = vade.AddDays(-rnd.Next(45, 121));
                liste.Add(new VcCek { Sirket = sirket, No = No(bi, belge), CariKod = cariKod, Cari = cari, Tur = tur, Banka = belge == "Senet" ? "—" : Banka(bi), Keside = keside, Vade = vade, Tutar = tutar, Durum = durum ?? Durum(vade, rnd, bugun), Aciklama = aciklama, Belge = belge });
            }
            // 0) Birebir tablo (Uras Kimya 2026): her ay dolu hücreler en az 1 çek; tablodaki aylık adede ulaşana dek fazladan çekler tutar / çek oranı en yüksek hücreye verilir.
            //    Hücre tutarı ağırlıklı parçalara bölünür (1.000 / 10 / 0,01 TL yuvarlama, kuruş son çekte) — satır, ay, toplam ve adet tabloyla aynıdır; iade yok.
            bool gercek = sirket == VarsayilanSirket && yil == GercekYil;
            if (gercek)
                for (int ay = 1; ay <= 12; ay++)
                {
                    var hucreler = Gercek2026.Where(r => r.Aylar[ay - 1] > 0).Select(r => (r.Ad, Tutar: r.Aylar[ay - 1], N: 1)).ToList();
                    for (int k = hucreler.Count; k < GercekAdet2026[ay - 1]; k++)
                    {
                        int en = 0; for (int i = 1; i < hucreler.Count; i++) if (hucreler[i].Tutar / hucreler[i].N > hucreler[en].Tutar / hucreler[en].N) en = i;
                        hucreler[en] = (hucreler[en].Ad, hucreler[en].Tutar, hucreler[en].N + 1);
                    }
                    int gunSayisi = DateTime.DaysInMonth(yil, ay);
                    foreach (var h in hucreler)
                    {
                        var gunler = Enumerable.Range(1, gunSayisi).OrderBy(_ => rnd.Next()).Take(Math.Min(h.N, gunSayisi)).OrderBy(x => x).ToList();
                        var par = Enumerable.Range(0, h.N).Select(_ => 0.6 + rnd.NextDouble()).ToArray(); double parTop = par.Sum();
                        decimal birim = h.Tutar >= h.N * 10_000m ? 1000m : h.Tutar >= h.N * 100m ? 10m : 0.01m, kalan = h.Tutar;
                        for (int j = 0; j < h.N; j++)
                        {
                            decimal t = j == h.N - 1 ? kalan : Math.Floor(h.Tutar * (decimal)(par[j] / parTop) / birim) * birim; kalan -= t;
                            var vade = new DateTime(yil, ay, gunler[j % gunler.Count]);
                            Ekle("", h.Ad, "Tedarikçi", vade, t, "Fatura ödemesi", "Çek", vade < bugun ? "Ödendi" : "Ödenecek");
                        }
                    }
                }
            // 1) Tedarikçi / sigorta alıcıları: havuz (Uras Kimya tamamı, diğer şirketler deterministik alt küme) + şirketin kartlı tedarikçileri (birebir yılda yalnız tablo)
            var alicilar = new List<(string Kod, string Ad, string Tur, decimal Yillik, int AyAdedi)>();
            int adim = sirket == "Uras Kimya" ? 1 : 2 + Array.IndexOf(Sirketler, sirket) % 2;
            for (int i = 0; i < Havuz.Length; i++) if (sirket == "Uras Kimya" || (i + Array.IndexOf(Sirketler, sirket)) % adim == 0) alicilar.Add(("", Havuz[i].Ad, Havuz[i].Tur, Havuz[i].Yillik, Havuz[i].AyAdedi));
            if (!gercek)
                foreach (var k in CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Tedarikçi" && x.Sirket == sirket))
                    alicilar.Add((k.Kod, k.KisaAd, "Tedarikçi", k.Sinif == "Hammadde" ? 6_500_000m : k.Sinif == "Hizmetler" || k.Sinif == "Danışmanlık" ? 900_000m : 2_200_000m, k.Sinif == "Hammadde" ? 10 : 6));
            // alıcıya ait çek (ya da senet) dağıtımı: yıllık tutar aktif aylara ağırlıklı dağıtılır, ayda 1–3 belge
            void AliciBelgeleri((string Kod, string Ad, string Tur, decimal Yillik, int AyAdedi) a, string belge, decimal carpan, int ayAdedi)
            {
                decimal yillik = Math.Round(a.Yillik * carpan * olcek * yilC * (decimal)(0.85 + rnd.NextDouble() * 0.3) / 1000m) * 1000m;
                var aylar = Enumerable.Range(1, 12).OrderBy(_ => rnd.Next()).Take(Math.Min(12, ayAdedi)).OrderBy(x => x).ToList();
                var agirlik = aylar.Select(_ => 0.5 + rnd.NextDouble()).ToArray(); double top = agirlik.Sum();
                for (int i = 0; i < aylar.Count; i++)
                {
                    decimal ayTutar = Math.Round(yillik * (decimal)(agirlik[i] / top) / 1000m) * 1000m; if (ayTutar <= 0) continue;
                    int n = belge == "Senet" ? 1 : ayTutar > 1_500_000m ? rnd.Next(2, 4) : rnd.Next(1, 3);
                    var par = Enumerable.Range(0, n).Select(_ => 0.6 + rnd.NextDouble()).ToArray(); double parTop = par.Sum(); decimal kalan = ayTutar;
                    for (int j = 0; j < n; j++)
                    {
                        decimal t = j == n - 1 ? kalan : Math.Round(ayTutar * (decimal)(par[j] / parTop) / 1000m) * 1000m; kalan -= t; if (t <= 0) continue;
                        var vade = new DateTime(yil, aylar[i], 1 + rnd.Next(DateTime.DaysInMonth(yil, aylar[i])));
                        Ekle(a.Kod, a.Ad, a.Tur, vade, t, belge == "Senet" ? "Senetle ödeme" : "Fatura ödemesi", belge);
                    }
                }
            }
            for (int ai = 0; ai < alicilar.Count; ai++)
            {
                if (alicilar[ai].AyAdedi == 0) continue;   // tabloda çeksiz cari
                if (!gercek) AliciBelgeleri(alicilar[ai], "Çek", 1m, alicilar[ai].AyAdedi);   // birebir yılda çekler yukarıda tablodan
                // tedarikçilerin yaklaşık üçte birine senet de verilir (çek hacminin %30'u, en çok 6 ayda)
                if (alicilar[ai].Tur == "Tedarikçi" && ai % 3 == 0) AliciBelgeleri(alicilar[ai], "Senet", 0.3m, Math.Min(alicilar[ai].AyAdedi, 6));
            }
            // 2) Maaş: her ay brüt maaş bütçesinin %30'u çekle (bütçe satırı yoksa taban)
            for (int ay = 1; ay <= 12; ay++)
            {
                var sg1 = ButceOrnek.Bul(sirket, yil, "SG-01"); decimal maas = Math.Round((sg1 != null ? sg1.Butce[ay - 1] : 9_500_000m * olcek * yilC) * 0.30m / 1000m) * 1000m;
                Ekle("", "Personel Maaş Ödemesi", "Maaş", new DateTime(yil, ay, 5), maas, $"{ButceOrnek.Aylar[ay - 1]} maaşları · çekle ödenen kısım (%30)");
            }
            // 3) Kira: ofis kirası (SG-07) çeki her ayın başında
            foreach (var k in Kiralar)
                for (int ay = 1; ay <= 12; ay++)
                {
                    var s = ButceOrnek.Bul(sirket, yil, k.ButceKalemi); if (s == null) continue;
                    Ekle("", k.Ad, "Kira", new DateTime(yil, ay, 1 + rnd.Next(3)), s.Butce[ay - 1], "Ofis kirası · " + ButceOrnek.Aylar[ay - 1]);
                }
            return liste;
        }
    }
}
