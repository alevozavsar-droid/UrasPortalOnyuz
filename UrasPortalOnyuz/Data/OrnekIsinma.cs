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
                D("Kasa Nakit Akışı", () => { foreach (var s in CekAkibetOrnek.Sirketler) NakitAkisOrnek.Odemeler(s.DbName); });   // düzenli ödemeler + çek planları da burada tohumlanır
                D("Planlı Düzenli Ödemeler", () => { var _ = DuzenliOdemeOrnek.Listele(); });
                D("Nakit Avans", () => { var _ = NakitAvansOrnek.Talepler.Count; });
                D("Ödeme Talepleri", () => { var _ = OdemeTalepOrnek.Listele(null, false); });
                D("Tahsilat / Ödeme", () => { var _ = TahsilatOrnek.Tahsilatlar; var __ = TahsilatOrnek.Odemeler; });
                D("Finekra", () => { var _ = FinekraOrnek.GunlukListesi(null); });
                Console.WriteLine($"[Isınma] örnek veriler tohumlandı · {sw.ElapsedMilliseconds} ms" + (hata > 0 ? $" · {hata} uyarı" : ""));
            }
        }
    }
}
