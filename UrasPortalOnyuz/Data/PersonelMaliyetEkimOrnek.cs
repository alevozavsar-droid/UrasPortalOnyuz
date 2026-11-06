// ============================================================
// PERSONEL MALİYET RAPORU — Ekim 2026 bordro kaynağı (5 Kas 2026)
// Kaynak: personel_kazanc_birlesik_2.xlsx, Sayfa1 (92 satır). Toplamlar: Net Maaş-R 3.973.172,71 · Net Maaş-X 2.776.724,39 · Banka 4.142.970,36.
//   Net Maaş-R → x: r · Maaş Farkı-R → maasFarki · Prim-R → prim · Yol-R → yol · Yemek-R → yemek · Yıllık İzin-R → yillikIzin · Mesailer-R → mesai · Kıdem-R → kidem · İhbar-R → ihbar
//   Kira Yardımı-R → kira · Çocuk Yardımı-R → cocuk · Kasa Tazminatı-R → kasa · Avans-R → avans · Borç-R → borc · Özel Sigorta-R → ozelSigorta · BES-R → bes
//   Net Maaş-X → x · Prim-X → primX · Yol-X → yolX · Yemek-X → yemekX · Mesailer-X → mesaiX
//   Dosyada yok: İcra (Eylül planı korunur); Mazot / Sendika / İhbar kesintisi ve X kesintileri sıfır.
//   SGK İŞVERENİ "—" = SGK'sız (X) kişi: sgk null. Dövizli maaşlar (Joao Paulo, Vu Thi Thuy Ha, Orazgül Jumayeva, Sevgi Kaya, Berkan Özkan) PersonelMaliyetOrnek.DovizliDuzen ile yeni bordro kurundan hesaplanır.
// Kimlik (unvan, bölüm, görev, çatı, işe giriş, fiili görev yeri, sicil), notlar ve özel kişi bayrağı Eylül kaydından ad eşleşmesiyle alınır (PersonelMaliyetOrnek.EkimListesi).
// ============================================================
namespace WebApplication3.Data
{
    /// <summary>Ekim 2026 bordrosunun kaynak satırı: bir kişinin bir SGK işvereni altındaki R ve X tutarları (aynı kişi birden fazla işverenden ayrı satırda olabilir).</summary>
    public sealed class EkimSatir
    {
        public string Sgk { get; set; }
        public string Ad { get; set; }
        public decimal RNet, MaasFarki, Prim, Yol, Yemek, YillikIzin, Mesai, Kidem, Ihbar, Kira, Cocuk, Kasa, Avans, Borc, OzelSigorta, Bes, XNet, PrimX, YolX, YemekX, MesaiX;
    }

    public static class PersonelMaliyetEkimOrnek
    {
        /// <summary>Bu dönem dosyadan kurulur (Yeni Bordro); Eylül kopyası ve kesinti planı eşitlemesi uygulanmaz (plansız kesintiler dosyadaki gibi kalır).</summary>
        public const string Donem = "2026-10";
        public const string Kaynak = "personel_kazanc_birlesik_2.xlsx + resmi Ekim bordroları (5 Kas 2026)";
        public static readonly EkimSatir[] Satirlar =
        {
            K("Avrupa Paper", "Ahmet Özkan", r: 75000m, yol: 3650.1m),
            K("ALV Kimya", "Aksel Güler", r: 85000m, yol: 3650.1m, avans: 14000m, bes: 4120m),
            K("ALV Kimya", "Alev Özavşar Eren", r: 60000.01m, bes: 2649m, x: 180359.48m),
            K("ALV Kimya", "Arslan Çoşkun", r: 35000.01m, mesai: 466.67m),
            K("ALV Kimya", "Aslan Huseynov", r: 37701.07m),
            K("Uras Kimya", "Ayfer Şahin", r: 90000m, yol: 3650.1m),
            K("ALV Kimya", "Berkan Özkan", r: 99346.4m, yol: 3285.09m, x: 0.01m, primX: 41134m),
            K("Uras Kimya", "Azad Shirinov", r: 37701.07m),
            K("Avrupa Paper", "Bahar Seyrekoğlu", r: 26666.67m, maasFarki: 2666.67m, yol: 2433.4m),
            K("Avrupa Paper", "Bedirhan Genç", r: 78500m, yol: 5000.1m, bes: 3868m),
            K("Avrupa Paper", "Berk Berberoğlu", r: 65000m, yol: 3650.1m, avans: 5000m),
            K("ALV Kimya", "Berk Yenilmez", r: 65000m, yol: 3650.1m),
            K("Avrupa Paper", "Serhat Mutlu", r: 60000m, yemek: 7500.22m, bes: 2718m, x: 0.01m, primX: 28902.64m),
            K("Selvi Kimya", "Beyza Gizem Zazoğlu", r: 68000m, yol: 3650.1m),
            K("Selvi Kimya", "Birol Şeker", r: 52000.01m, yol: 3650.1m),
            K("Avrupa Paper", "Burak Kaya", r: 53000m, yol: 3650.1m, mesai: 2826.67m, avans: 13300m, bes: 2692m),
            K("ALV Kimya", "Burak Sergen Ada", r: 40000m, mesai: 2440m),
            K("ALV Kimya", "Burhan Evvel", r: 35000.01m, yol: 5000.1m),
            K("ALV Kimya", "Burhan Gün", r: 46000m, mesai: 1533.33m, bes: 2108m),
            K("Selvi Kimya", "Büşra Alişarlı", r: 54000m, yol: 973.36m),
            K("Avrupa Paper", "Sevgi Kaya", r: 124183m, yol: 3500.1m, x: 0.01m, primX: 53674m),
            K("Avrupa Paper", "Can Gürsel", r: 37000m, yol: 3650.1m, mesai: 863.33m),
            K("Selvi Kimya", "Dilanur Sönmez", r: 25290m),
            K("ALV Kimya", "Dilber Güney", r: 80000m, yol: 3650.1m, avans: 13000m, bes: 3876m),
            K("ALV Kimya", "Dilek Güden", r: 90000m, yol: 3650.1m),
            K("ALV Kimya", "Dinçer Özdemir", r: 4266.67m, x: 9533.33m, yolX: 365.01m),
            K("Avrupa Paper", "Eda Bozkurt", r: 67000m, yol: 3650.1m),
            K("ALV Kimya", "Efe Akçakale", r: 35000m, yol: 3650.1m, yemek: 7500.22m, bes: 1575m),
            K("Uras Kimya", "Elman Abbaslı", r: 37701.07m),
            K("Uras Kimya", "Elmin Jabbarov", r: 37701.07m),
            K("ALV Kimya", "Yusuf Öztürk", r: 31000m, yemek: 7500.22m, x: 0.01m, primX: 41134.62m),
            K("Avrupa Paper", "Emrah Ateş", r: 50000m, yol: 3650.1m),
            K("ALV Kimya", "Emre Can Yavuz", r: 54000.01m, kira: 12740m, bes: 2979m),
            K("Avrupa Paper", "Emre Keskin", r: 70000m, yemek: 7500.22m, bes: 3207m),
            K(null, "Emine Tanış", x: 4000m),
            K("ALV Kimya", "Erman Kaval", r: 28100m, bes: 991m),
            K(null, "Mesut Akıncı", x: 15000m),
            K("ALV Kimya", "Eser Şenürek", r: 70000m, yol: 3650.1m),
            K("Avrupa Paper", "Ezgi Demir", r: 52000m, yol: 3650.1m, mesai: 1386.67m),
            K("ALV Kimya", "Görkem Akbulut", r: 47000m),
            K("Avrupa Paper", "Hacı Mustafa Korkmaz", r: 55000m, yol: 3650.1m),
            K("Avrupa Paper", "Hakan Haydut", r: 55000m, yol: 3650.1m, bes: 2652m),
            K("ALV Kimya", "Devrim / Hale Bisren", r: 28100m, x: 286900m),
            K("Avrupa Paper", "İdil Akel", r: 55000m, yol: 3650.1m, avans: 7500m, bes: 2652m),
            K("Uras Kimya", "Erdoğan Polat", r: 4133.33m, yemek: 7500.22m, x: 26866.67m, primX: 108786.25m),
            K("Uras Kimya", "Müjgan Yetgin", r: 4133.33m, yemek: 7500.22m, x: 26866.67m, primX: 70450.14m),
            K("ALV Kimya", "Kamil Mahmudov", r: 51000.01m, yol: 3650.1m, mesai: 3910m),
            K("Uras Kimya", "Kamran Cabbarov", r: 8422.65m),
            K("Uras Kimya", "Medine Rozykulova", r: 28500m, bes: 1107m, yolX: 5000m),
            K("Uras Kimya", "Mehman Babayev", r: 37701.07m),
            K("Avrupa Paper", "Melike Ural Kaya", r: 50000m, yol: 3650.1m, bes: 2194m),
            K("ALV Kimya", "Rabiye Doğan", r: 4266.67m, yol: 3650.1m, x: 30733.33m),
            K("Uras Kimya", "Murad Hudaybergenov", r: 37701.07m),
            K("ALV Kimya", "Zahit Ercan", r: 4266.67m, yol: 3650.1m, x: 35733.32m),
            K("ALV Kimya", "Mustafa Budak", r: 58500m, yol: 3650.1m),
            K("Avrupa Paper", "Mustafa Özdeniz", r: 60000m, yemek: 7500.22m, bes: 2718m),
            K("ALV Kimya", "Mustafa Savaş", r: 28100m, bes: 991m),
            K("ALV Kimya", "Mustafa Ulaş Erikli", r: 4133.33m, yol: 3650.1m, x: 38866.67m, mesaiX: 2580m),
            K("ALV Kimya", "İsmail Duman", r: 4266.67m, x: 41733.33m, mesaiX: 1686.67m),
            K("Uras Kimya", "Ayşegül Zent Ula", r: 4266.67m, yol: 3650.1m, x: 46733.33m),
            K("ALV Kimya", "Nevruz Sarıtaş", r: 52000m, yol: 3650.1m),
            K("Alv Filo", "Nükhet Özavşar", r: 180000m, x: 470000m),
            K("Avrupa Paper", "Olgun Küskü", r: 40500m, yol: 3650.1m, mesai: 1350m),
            K("ALV Kimya", "Ümit Özdemir", r: 4266.67m, x: 50733.33m),
            K("Avrupa Paper", "Ömer Kantar", r: 60000m, yemek: 7500.22m, bes: 2718m),
            K("Avrupa Paper", "Ömer Yıldırım", r: 60000m, yol: 7300m, yemek: 7500.22m),
            K("Avrupa Paper", "Özge İsen", r: 60000m, yol: 5000.1m, avans: 5000m),
            K("Uras Kimya", "Ertan Yavuz", r: 4266.67m, x: 55733.32m, mesaiX: 9200m),
            K(null, "Orazgül Jumayeva", x: 64575.16m, yolX: 2000m),
            K("Uras Kimya", "Rahman Babazade", r: 37701.07m),
            K("Uras Kimya", "Ramin Musayev", r: 37701.07m),
            K("Uras Kimya", "Vu Thi Thuy Ha", r: 46930.78m, x: 64377.03m),
            K("ALV Kimya", "Savaş Özcan", r: 68000.01m, mesai: 3173m, kira: 25000m),
            K("Selvi Kimya", "Seda Saraç", r: 30000m, yol: 3650.1m),
            K("Uras Kimya", "Refik Polat", r: 4133.33m, x: 70866.67m, mesaiX: 9250m),
            K("ALV Kimya", "Semih Evirgen", r: 65000.01m),
            K("ALV Kimya", "Cafer İpek", r: 5333.33m, x: 114666.67m),
            K("Avrupa Paper", "Serkan Genç", r: 85000m, yol: 3650.1m),
            K("ALV Kimya", "Selçuk Mert", r: 5333.34m, x: 129666.67m),
            K("ALV Kimya", "Soner İkizoğlu", r: 58500m, mesai: 7059m),
            K("ALV Kimya", "Şirin Tunç", r: 32000m),
            K("Uras Kimya", "Tural Abbasov", r: 37701.07m),
            K("ALV Kimya", "Özgür Pamir", r: 100000.01m, x: 140000m),
            K("Uras Kimya", "Joao Paulo Araujo Fernandes", r: 67156.8m, x: 211112.7m),
            K("ALV Kimya", "Yalçın Avcı", r: 45000m, yol: 3650.1m, mesai: 1200m, avans: 10000m),
            K("Avrupa Paper", "Yeşim Karakütük", r: 35000m, yol: 3650.1m),
            K(null, "Murat Özavşar", x: 277000m),
            K("ALV Kimya", "Neslihan Uludağ", r: 5333.33m, x: 384666.67m),
            K("Avrupa Paper", "Cüneyt Arda Ceylan", r: 2333.33m),
            K("Uras Holding", "Cüneyt Arda Ceylan", r: 4666.67m),
            K("Uras Kimya", "Cüneyt Arda Ceylan", r: 46666.68m),
            K("ALV Kimya", "Cüneyt Arda Ceylan", r: 7000m),
        };
        private static EkimSatir K(string sgk, string ad, decimal r = 0, decimal maasFarki = 0, decimal prim = 0, decimal yol = 0, decimal yemek = 0, decimal yillikIzin = 0, decimal mesai = 0, decimal kidem = 0, decimal ihbar = 0, decimal kira = 0, decimal cocuk = 0, decimal kasa = 0, decimal avans = 0, decimal borc = 0, decimal ozelSigorta = 0, decimal bes = 0, decimal x = 0, decimal primX = 0, decimal yolX = 0, decimal yemekX = 0, decimal mesaiX = 0)
            => new EkimSatir { Sgk = sgk, Ad = ad, RNet = r, MaasFarki = maasFarki, Prim = prim, Yol = yol, Yemek = yemek, YillikIzin = yillikIzin, Mesai = mesai, Kidem = kidem, Ihbar = ihbar, Kira = kira, Cocuk = cocuk, Kasa = kasa, Avans = avans, Borc = borc, OzelSigorta = ozelSigorta, Bes = bes, XNet = x, PrimX = primX, YolX = yolX, YemekX = yemekX, MesaiX = mesaiX };
    }
}