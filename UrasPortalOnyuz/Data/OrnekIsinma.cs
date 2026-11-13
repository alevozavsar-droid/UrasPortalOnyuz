using System;
using System.Diagnostics;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>
    /// ÖRNEK VERİ ISINMASI — uygulama dinlemeye başlamadan önce (Startup.Configure sonunda) bellek içi örnek listeler TEK iş parçacığında, sabit sırada tohumlanır.
    /// Neden: örnek sınıfların Tohumla() yordamları birbirini çağırır (NakitAkis → Düzenli Ödeme → Çek Akıbeti → Cari Kart / Bütçe; Ödeme Talebi → Talep Motoru → Nakit Avans …) ve her biri kendi kilidini tutarken
    /// ötekini tohumlar. İlk isteklerde iki ekran / kabuk uç noktası aynı anda farklı köklerden tohumlamaya girince kilit sırası çakışıyor ve uygulama kilitleniyordu (ters sıralı iç içe kilit).
    /// Tohumlama bir kez, tek iş parçacığında tamamlanınca _tohumlandi bayrakları dolu olur; isteklerde iç içe tohumlama ve uzun kilit zinciri oluşmaz.
    /// </summary>
    public static class OrnekIsinma
    {
        static bool _yapildi; static readonly object Kilit = new object();

        public static void Calistir()
        {
            lock (Kilit)
            {
                if (_yapildi) return; _yapildi = true; var sw = Stopwatch.StartNew(); int hata = 0;
                void D(string ad, Action a) { try { a(); } catch (Exception ex) { hata++; Console.WriteLine($"[Isınma] {ad}: {ex.Message}"); } }
                D("Cari Kart", () => { var _ = CariKartOrnek.Kartlar; });
                D("Bütçe", () => { var _ = ButceOrnek.Cekler; });
                D("Verilen Çek", () => { var _ = CekOrnek.Verilen; });
                D("Çek Akıbeti", () => { var _ = CekAkibetOrnek.Kayitlar; });
                D("Nakit Akış Planlama", () => { foreach (var s in CekAkibetOrnek.Sirketler) NakitAkisOrnek.Odemeler(s.DbName); });   // düzenli ödemeler + çek planları da burada tohumlanır
                D("Planlı Düzenli Ödemeler", () => { var _ = DuzenliOdemeOrnek.Listele(); });
                D("Kredi Yapılandırma", KrediYapilandirmaOrnek.Yukle);   // kalıcı kredi kayıtları (App_Data/kredi-yapilandirma.json) + taksit satırları Nakit Akış Planlama ve düzenli ödemelere (9 Kas 2026); Kasa ve Düzenli tohumundan SONRA
                D("Konkordato", KonkordatoOrnek.Yukle);
                D("İBKB", IbkbOrnek.Yukle);   // İBKB Beyanname Takibi kalıcı dosyalar (App_Data/ibkb.json; ilk açılışta Excel tohumu) — 11 Kas 2026   // Kredi Raporu konkordato kümesi (App_Data/konkordato.json; ilk açılışta bilirkişi raporu tohumu, ipotekler Rehin · İpotek Takibi'ne) — Kredi Yapılandırma'dan SONRA (10 Kas 2026)
                D("Nakit Avans", () => { var _ = NakitAvansOrnek.Talepler.Count; });
                D("Ödeme Talepleri", () => { var _ = OdemeTalepOrnek.Listele(null, false); });
                D("Tahsilat / Ödeme", () => { var _ = TahsilatOrnek.Tahsilatlar; var __ = TahsilatOrnek.Odemeler; });
                D("Finekra", () => { var _ = FinekraOrnek.GunlukListesi(null); });
                D("Gündelikçi kişi kayıtları", PersonelMaliyetEkOrnek.GundelikciKisileriTamamla);   // SGK'sız gündelikçilere bordro dışı kişi kaydı (335.9xx; 9 Kas 2026) — kalıcı günlükten ÖNCE: günlükteki kişi sıraları (Ekim 88–93) değişmez
                D("Personel Maliyet kalıcı günlüğü", PersonelMaliyetKalici.Yukle);
            D("Nakit Akış Planlama kalıcı günlüğü", NakitAkisKalici.Yukle);   // 13 Kas 2026: tohumdan ve öbür modüllerin açtığı satırlardan (düzenli ödeme, kredi taksiti, personel ödemesi, avans) sonra — ekrandan yapılan girişler aynı sırayla yeniden uygulanır   // tohumdan sonra: kullanıcı girişleri ve "Yeni Bordro" dönemleri aynı sırayla yeniden uygulanır (6 Kas 2026)
                Console.WriteLine($"[Isınma] örnek veriler tohumlandı · {sw.ElapsedMilliseconds} ms" + (hata > 0 ? $" · {hata} uyarı" : ""));
            }
        }
    }
}
