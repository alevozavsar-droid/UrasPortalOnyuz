// ============================================================
// PERSONEL MALİYET RAPORU — Ekim 2026 RESMİ ücret bordroları (6 şirket; kullanıcı 5 Kas 2026: bordro_birlesik.xlsx)
// Her satır: SGK işvereni, ad, Toplam Brüt Kazanç, SGK İşveren Toplam, Hazine İndirimi, Prim Gün (= normal çalışma günü; hafta sonu / tatil / izin 0), SGK Matrahı, Gelir Vergisi, Damga Vergisi, SGK İşçi Toplam, Net Tutar, Kasa Tazminatı.
// Kullanım: Ekim bordrosu oluşturulurken (PersonelMaliyetOrnek.EkimListesi) brüt, işveren, hazine ve günler kaynak değer olur; net ve ek kalemler kazanç dosyasından (Eylül kuralı: kalemde net, brütte resmi bordro).
// SGK matrahı, GV, DV, işçi ve net yalnız doğrulama içindir (model hesaplar; GV artık). Aynı kişi birden fazla işverende ayrı satırdadır (Cüneyt Arda Ceylan 4).
// ============================================================
namespace WebApplication3.Data
{
    /// <summary>Ekim 2026 resmi bordro satırı (bir kişinin bir SGK işverenindeki bordrosu).</summary>
    public sealed class EkimBordroSatir
    {
        public string Sgk { get; set; }
        public string Ad { get; set; }
        public decimal Brut, Isveren, Hazine, PrimGun, SgkMatrah, Gv, Dv, Isci, Net, KasaTazminati;
    }

    public static class PersonelMaliyetEkimBordro
    {
        public const string Kaynak = "bordro_birlesik.xlsx (resmi Ekim 2026 bordroları, 5 Kas 2026)";
        public static readonly EkimBordroSatir[] Satirlar =
        {
            B("ALV Kimya", "Aksel Güler", 137357.98m, 32622.52m, 0m, 30m, 137357.98m, 27312.33m, 791.85m, 20603.7m, 88650.1m, 0m),
            B("ALV Kimya", "Alev Özavşar Eren", 88323.25m, 20976.77m, 0m, 30m, 88323.25m, 14655.09m, 419.68m, 13248.48m, 60000m, 0m),
            B("ALV Kimya", "Arslan Çoşkun", 44022.06m, 10455.24m, 0m, 30m, 44022.06m, 1868.65m, 83.43m, 6603.31m, 35466.67m, 0m),
            B("ALV Kimya", "Aslan Hüseynov", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("ALV Kimya", "Berk Yenilmez", 104726.76m, 24872.61m, 0m, 30m, 104726.76m, 19823.47m, 544.18m, 15709.01m, 68650.1m, 0m),
            B("ALV Kimya", "Berkan Özkan", 157879.11m, 37496.3m, 0m, 30m, 157879.11m, 30618.16m, 947.6m, 23681.86m, 102631.49m, 0m),
            B("ALV Kimya", "Burak Sergen Ada", 53778.56m, 12772.41m, 0m, 30m, 53778.56m, 3114.29m, 157.48m, 8066.79m, 42440m, 0m),
            B("ALV Kimya", "Burhan Evvel", 50764.12m, 12056.48m, 0m, 30m, 50764.12m, 3014.8m, 134.6m, 7614.62m, 40000.1m, 0m),
            B("ALV Kimya", "Burhan Gün", 70273.48m, 16689.95m, 0m, 30m, 70273.48m, 11916.44m, 282.68m, 10541.03m, 47533.33m, 0m),
            B("ALV Kimya", "Cafer İpek", 5765.76m, 1427.03m, 0m, 4m, 5765.76m, 0m, 0m, 432.43m, 5333.33m, 0m),
            B("ALV Kimya", "Cüneyt Arda Ceylan", 9791.44m, 2325.47m, 0m, 3m, 9791.44m, 1248.41m, 74.32m, 1468.71m, 7000m, 0m),
            B("ALV Kimya", "Dilber Güney", 129200.18m, 30685.05m, 0m, 30m, 129200.18m, 25440.12m, 729.93m, 19380.03m, 83650.1m, 0m),
            B("ALV Kimya", "Dilek Güden", 143225.42m, 34016.04m, 0m, 30m, 143225.42m, 27255.13m, 836.38m, 21483.81m, 93650.1m, 0m),
            B("ALV Kimya", "Dinçer Özdemir", 4612.62m, 1141.62m, 0m, 4m, 4612.62m, 0m, 0m, 345.95m, 4266.67m, 0m),
            B("ALV Kimya", "Efe Akçakale", 60022.98m, 12474.17m, 0m, 30m, 52522.76m, 5846.3m, 147.95m, 7878.41m, 46150.32m, 0m),
            B("ALV Kimya", "Emre Can Yavuz", 99319.96m, 23588.49m, 0m, 30m, 99319.96m, 17178.83m, 503.14m, 14897.99m, 66740m, 0m),
            B("ALV Kimya", "Erman Kaval", 33066.44m, 7853.28m, 0m, 30m, 33066.44m, 6.2m, 0.28m, 4959.96m, 28100m, 0m),
            B("ALV Kimya", "Eser Şenürek", 110594.21m, 26266.13m, 0m, 30m, 110594.21m, 19766.27m, 588.71m, 16589.13m, 73650.1m, 0m),
            B("ALV Kimya", "Görkem Akbulut", 67112.95m, 15939.33m, 0m, 30m, 67112.95m, 9787.32m, 258.69m, 10066.94m, 47000m, 0m),
            B("ALV Kimya", "Devrim / Hale Bisren", 33066.44m, 7853.28m, 0m, 30m, 33066.44m, 6.2m, 0.28m, 4959.96m, 28100m, 0m),
            B("ALV Kimya", "Kamil Mahmudov", 85973.93m, 20418.81m, 0m, 30m, 85973.93m, 14115.91m, 401.84m, 12896.08m, 58560.1m, 0m),
            B("ALV Kimya", "Mustafa Budak", 91831.26m, 21809.93m, 0m, 30m, 91831.26m, 15460.17m, 446.3m, 13774.69m, 62150.1m, 0m),
            B("ALV Kimya", "Mustafa Savaş", 33066.44m, 7853.28m, 0m, 30m, 33066.44m, 6.2m, 0.28m, 4959.96m, 28100m, 0m),
            B("ALV Kimya", "Mustafa Ulaş Erikli", 8414.52m, 2082.6m, 0m, 4m, 8414.52m, 0m, 0m, 631.09m, 7783.43m, 0m),
            B("ALV Kimya", "Neslihan Uludağ", 5765.76m, 1427.03m, 0m, 4m, 5765.76m, 0m, 0m, 432.43m, 5333.33m, 0m),
            B("ALV Kimya", "Nevruz Sarıtaş", 81226.12m, 19291.21m, 0m, 30m, 81226.12m, 13026.29m, 365.81m, 12183.92m, 55650.1m, 0m),
            B("ALV Kimya", "Rabiye Doğan", 8558.67m, 2118.27m, 0m, 4m, 8558.67m, 0m, 0m, 641.9m, 7916.77m, 0m),
            B("ALV Kimya", "Savaş Özcan", 147341.69m, 34993.65m, 0m, 30m, 147341.69m, 28199.82m, 867.62m, 22101.25m, 96173m, 0m),
            B("ALV Kimya", "Selçuk Mert", 5765.76m, 1427.03m, 0m, 4m, 5765.76m, 0m, 0m, 432.43m, 5333.33m, 0m),
            B("ALV Kimya", "Semih Evirgen", 96481.05m, 22914.24m, 0m, 30m, 96481.05m, 16527.3m, 481.59m, 14472.16m, 65000m, 0m),
            B("ALV Kimya", "Soner İkizoğlu", 97393.11m, 23130.86m, 0m, 30m, 97393.11m, 16736.62m, 488.52m, 14608.97m, 65559m, 0m),
            B("ALV Kimya", "Yalçın Avcı", 74053.41m, 17587.68m, 0m, 30m, 74053.41m, 12783.93m, 311.37m, 11108.01m, 49850.1m, 0m),
            B("ALV Kimya", "Yusuf Öztürk", 44879.5m, 8877.58m, 0m, 30m, 37379.28m, 739.38m, 33.01m, 5606.89m, 38500.22m, 0m),
            B("ALV Kimya", "Zahit Ercan", 8558.67m, 2118.27m, 0m, 4m, 8558.67m, 0m, 0m, 641.9m, 7916.77m, 0m),
            B("ALV Kimya", "Özgür Pamir", 153585.69m, 36476.6m, 0m, 30m, 153585.69m, 29632.81m, 915.02m, 23037.86m, 100000m, 0m),
            B("ALV Kimya", "Ümit Özdemir", 4612.62m, 1141.62m, 0m, 4m, 4612.62m, 0m, 0m, 345.95m, 4266.67m, 0m),
            B("ALV Kimya", "İsmail Duman", 4612.62m, 1141.62m, 0m, 4m, 4612.62m, 0m, 0m, 345.95m, 4266.67m, 0m),
            B("ALV Kimya", "Şirin Tunç", 38866.47m, 9230.79m, 0m, 30m, 38866.47m, 992.2m, 44.3m, 5829.97m, 32000m, 0m),
            B("Avrupa Paper", "Ahmet Özkan", 121042.36m, 28747.56m, 0m, 30m, 121042.36m, 23567.9m, 668.01m, 18156.35m, 78650.1m, 0m),
            B("Avrupa Paper", "Bahar Seyrekoğlu", 44549.28m, 10580.45m, 0m, 20m, 44549.28m, 6012.74m, 87.42m, 6682.38m, 31766.74m, 0m),
            B("Avrupa Paper", "Bedirhan Genç", 128955.45m, 30626.91m, 0m, 30m, 128955.45m, 25383.95m, 728.08m, 19343.32m, 83500.1m, 0m),
            B("Avrupa Paper", "Berk Berberoğlu", 104726.76m, 24872.61m, 0m, 30m, 104726.76m, 19823.47m, 544.18m, 15709.01m, 68650.1m, 0m),
            B("Avrupa Paper", "Burak Kaya", 89759.88m, 21317.97m, 0m, 30m, 89759.88m, 16388.56m, 430.57m, 13463.98m, 59476.77m, 0m),
            B("Avrupa Paper", "Can Gürsel", 60451.63m, 14357.25m, 0m, 30m, 60451.63m, 9662.32m, 208.13m, 9067.75m, 41513.43m, 0m),
            B("Avrupa Paper", "Cüneyt Arda Ceylan", 2745.09m, 651.95m, 0m, 1m, 2745.09m, 0m, 0m, 411.76m, 2333.33m, 0m),
            B("Avrupa Paper", "Eda Bozkurt", 107989.87m, 25647.6m, 0m, 30m, 107989.87m, 20572.35m, 568.94m, 16198.48m, 70650.1m, 0m),
            B("Avrupa Paper", "Emrah Ateş", 80253.34m, 19060.17m, 0m, 30m, 80253.34m, 14206.82m, 358.42m, 12038m, 53650.1m, 0m),
            B("Avrupa Paper", "Emre Keskin", 114429.43m, 25395.69m, 0m, 30m, 106929.21m, 20328.93m, 560.9m, 16039.38m, 77500.22m, 0m),
            B("Avrupa Paper", "Ezgi Demir", 85778.88m, 20372.47m, 0m, 30m, 85778.88m, 15474.93m, 400.36m, 12866.82m, 57036.77m, 0m),
            B("Avrupa Paper", "Hacı Mustafa Korkmaz", 88411.14m, 20997.66m, 0m, 30m, 88411.14m, 16079.03m, 420.34m, 13261.67m, 58650.1m, 0m),
            B("Avrupa Paper", "Hakan Haydut", 88411.14m, 20997.66m, 0m, 30m, 88411.14m, 16079.03m, 420.34m, 13261.67m, 58650.1m, 0m),
            B("Avrupa Paper", "Melike Ural Kaya", 73151.91m, 17373.58m, 0m, 30m, 73151.91m, 8224.5m, 304.52m, 10972.79m, 53650.1m, 0m),
            B("Avrupa Paper", "Mustafa Özdeniz", 98113.82m, 21520.73m, 0m, 30m, 90613.6m, 16584.5m, 437.06m, 13592.04m, 67500.22m, 0m),
            B("Avrupa Paper", "Olgun Küskü", 66956.13m, 15902.08m, 0m, 30m, 66956.13m, 11155.11m, 257.5m, 10043.42m, 45500.1m, 0m),
            B("Avrupa Paper", "Serhat Mutlu", 98113.82m, 21520.73m, 0m, 30m, 90613.6m, 16584.5m, 437.06m, 13592.04m, 67500.22m, 0m),
            B("Avrupa Paper", "Serkan Genç", 137357.98m, 32622.52m, 0m, 30m, 137357.98m, 27312.33m, 791.85m, 20603.7m, 88650.1m, 0m),
            B("Avrupa Paper", "Sevgi Kaya", 205059.01m, 48701.52m, 0m, 30m, 205059.01m, 45311.36m, 1305.69m, 30758.86m, 127683.1m, 0m),
            B("Avrupa Paper", "Yeşim Karakütük", 53940.47m, 12810.87m, 0m, 30m, 53940.47m, 7040.59m, 158.71m, 8091.07m, 38650.1m, 0m),
            B("Avrupa Paper", "Ömer Kantar", 98113.82m, 21520.73m, 0m, 30m, 90613.6m, 16584.5m, 437.06m, 13592.04m, 67500.22m, 0m),
            B("Avrupa Paper", "Ömer Yıldırım", 110024.2m, 24349.44m, 0m, 30m, 102523.98m, 19317.93m, 527.46m, 15378.59m, 74800.22m, 0m),
            B("Avrupa Paper", "Özge İsen", 98771.56m, 23458.24m, 0m, 30m, 98771.56m, 18456.75m, 498.98m, 14815.73m, 65000.1m, 0m),
            B("Avrupa Paper", "İdil Akel", 88411.14m, 20997.66m, 0m, 30m, 88411.14m, 16079.03m, 420.34m, 13261.67m, 58650.1m, 0m),
            B("Uras Kimya", "Ayfer Şahin", 143225.42m, 34016.04m, 0m, 30m, 143225.42m, 27255.13m, 836.38m, 21483.81m, 93650.1m, 0m),
            B("Uras Kimya", "Ayşegül Zent Ula", 8558.67m, 2118.27m, 0m, 4m, 8558.67m, 0m, 0m, 641.9m, 7916.77m, 0m),
            B("Uras Kimya", "Azad Shirinov", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("Uras Kimya", "Cüneyt Arda Ceylan", 66569.1m, 15810.15m, 0m, 20m, 66569.1m, 9662.51m, 254.56m, 9985.36m, 46666.67m, 0m),
            B("Uras Kimya", "Elman Abbaslı", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("Uras Kimya", "Elmin Jabbarov", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("Uras Kimya", "Erdoğan Polat", 11968.69m, 1105.95m, 0m, 4m, 4468.47m, 0m, 0m, 335.14m, 11633.55m, 0m),
            B("Uras Kimya", "Ertan Yavuz", 4612.62m, 1141.62m, 0m, 4m, 4612.62m, 0m, 0m, 345.95m, 4266.67m, 0m),
            B("Uras Kimya", "Joao Paulo Araujo Fernandes", 100000m, 23750m, 0m, 30m, 100000m, 17334.9m, 508.3m, 15000m, 67156.8m, 0m),
            B("Uras Kimya", "Kamran Cabbarov", 9909m, 2353.39m, 0m, 6m, 9909m, 0m, 0m, 1486.35m, 8422.65m, 0m),
            B("Uras Kimya", "Medine Rozykulova", 36929.08m, 8770.65m, 0m, 30m, 36929.08m, 2860.13m, 29.59m, 5539.36m, 28500m, 0m),
            B("Uras Kimya", "Mehman Babayev", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("Uras Kimya", "Murad Hudaybergenov", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("Uras Kimya", "Müjgan Yetgin", 11968.69m, 1105.95m, 0m, 4m, 4468.47m, 0m, 0m, 335.14m, 11633.55m, 0m),
            B("Uras Kimya", "Rahman Babazade", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("Uras Kimya", "Ramin Musayev", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("Uras Kimya", "Refik Polat", 4468.47m, 1105.95m, 0m, 4m, 4468.47m, 0m, 0m, 335.14m, 4133.33m, 0m),
            B("Uras Kimya", "Tural Abbasov", 49545m, 11766.93m, 0m, 30m, 49545m, 4286.83m, 125.35m, 7431.75m, 37701.07m, 0m),
            B("Uras Kimya", "Vu Thi Thuy Ha", 67000m, 15912.5m, 0m, 30m, 67000m, 9761.4m, 257.83m, 10050m, 46930.77m, 0m),
            B("Uras Holding", "Cüneyt Arda Ceylan", 6527.63m, 1550.31m, 0m, 2m, 6527.63m, 832.27m, 49.54m, 979.15m, 4666.67m, 0m),
            B("Selvi Kimya", "Beyza Gizem Zazoğlu", 107331.08m, 25491.14m, 5366.55m, 30m, 107331.08m, 19017.38m, 563.94m, 16099.66m, 71650.1m, 0m),
            B("Selvi Kimya", "Büşra Alişarlı", 70654.12m, 16780.34m, 3532.71m, 27m, 70654.12m, 4797.07m, 285.56m, 10598.13m, 54973.36m, 0m),
            B("Selvi Kimya", "Birol Şeker", 81226.12m, 19291.21m, 4061.31m, 30m, 81226.12m, 13026.29m, 365.81m, 12183.92m, 55650.1m, 0m),
            B("Selvi Kimya", "Dilanur Sönmez", 29752.94m, 7066.32m, 1487.65m, 27m, 29752.94m, 0m, 0m, 4462.94m, 25290m, 0m),
            B("Selvi Kimya", "Seda Saraç", 42922.67m, 10194.14m, 2146.13m, 30m, 42922.67m, 2759.1m, 75.08m, 6438.39m, 33650.1m, 0m),
            B("Alv Filo", "Nükhet Özavşar", 317858.35m, 70601.63m, 0m, 30m, 297270m, 91106m, 2161.85m, 44590.5m, 180000m, 0m),
        };
        private static EkimBordroSatir B(string sgk, string ad, decimal brut, decimal isveren, decimal hazine, decimal primGun, decimal sgkMatrah, decimal gv, decimal dv, decimal isci, decimal net, decimal kasa)
            => new EkimBordroSatir { Sgk = sgk, Ad = ad, Brut = brut, Isveren = isveren, Hazine = hazine, PrimGun = primGun, SgkMatrah = sgkMatrah, Gv = gv, Dv = dv, Isci = isci, Net = net, KasaTazminati = kasa };
    }
}