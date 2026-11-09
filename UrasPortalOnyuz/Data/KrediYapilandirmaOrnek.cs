// ============================================================
// KREDİ YAPILANDIRMA RAPORU (9 Kas 2026, kullanıcı: "banka kredileri zamanında ödenmediği için yeni yapılandırmalar yapıyoruz … yapılandırma öncesi kredi bilgileri, yapılandırma sonrası,
//   ipotek vs. detayları girilebilsin; aylık taksit Kasa Nakit Akışı ve Planlı Ödemelere akmalı")
//   Ekran: Controllers/KrediYapilandirmaController.cs + Views/KrediYapilandirma/Index.cshtml + wwwroot/js/kredi-yapilandirma.js (Mali İşler › Finans › Krediler ve Finansman).
//   Kayıt: şirket (Kasa Nakit Akışı şirketleri), banka / şube, kredi no, tür; YAPILANDIRMA ÖNCESİ (kullanım tarihi, anapara, faiz, vade, taksit, kalan anapara / vade, son ödeme, geciken taksit
//   adedi ve tutarı, gecikme faizi, masraf, önceki durum) ve SONRASI (yapılandırma tarihi, protokol no, yeni anapara, yeni faiz, BSMV, yeni vade, ödemesiz ay, yeni taksit — boşsa anüite ile
//   hesaplanır —, ilk taksit tarihi, ödenen taksit, durum), TEMİNATLAR (ipotek: tapu bilgisi, derece, tutar, ekspertiz, malik; kefalet, rehin, teminat mektubu …), not.
//   Taksit takvimi: ödemesiz aylarda faiz anaparaya eklenir, sonra eşit taksit; aylık oran = yıllık faiz / 12 × (1 + BSMV); son taksit kalanı kapatır.
//   Peşinat (9 Kas 2026, "peşinat ödemesi oluyor"): protokolde peşin ödenen tutar; taksitlendirilen yeni anaparanın dışında, toplam geri ödemeye girer; Kasa Nakit Akışı'na "KRD-001 · Peşinat" satırı (anahtar "KRD-001|P"), ödendi işaretiyle ÖDENDİ.
//   Akış: NakitAkisaAktar açıkken her taksit Kasa Nakit Akışı'na Havale / EFT satırı olarak yazılır (NakitAkisOrnek.KrediTaksitiYaz; anahtar "KRD-001|003"; ödenen taksitler ÖDENDİ) ve kredi
//   Planlı Düzenli Ödemeler'e "Kredi" kategorisiyle kaydedilir (DuzenliOdemeOrnek; aylık otomatik akış kapalı (DisAkis) — taksit satırları buradan gelir, aylar AkisBagla ile bağlanır).
//   Yapılandırma değişince satırlar güncellenir, fazlası kaldırılır (ödenmişe dokunulmaz); kayıt silinince ödenmemiş satırlar kaldırılır, düzenli ödeme pasife alınır.
//   Kalıcılık: App_Data/kredi-yapilandirma.json (PersonelMaliyetKalici.Dizin; git dışı). Dosya yoksa tek ÖRNEK kayıt (akış kapalı) gösterilir; kullanıcı silebilir.
//   Kasa Nakit Akışı satırlarının ödendi durumu bellek içidir (Kasa modülü kalıcı değil); açılışta satırlar yeniden yazılır, ödenen taksit sayısı kayıttan gelir.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>Teminat: İpotek (tapu bilgisi, derece, ipotek tutarı, ekspertiz, malik), Kefalet (kefil), Araç / Ticari İşletme Rehni, Teminat Mektubu, Çek / Senet, Nakit Blokaj, Diğer.</summary>
    public class KrediTeminat
    {
        public string Tur { get; set; } = "İpotek";
        public string Aciklama { get; set; } = "";
        public string TapuBilgisi { get; set; } = "";
        public string Derece { get; set; } = "";
        public string Malik { get; set; } = "";
        public decimal Tutar { get; set; }
        public decimal Ekspertiz { get; set; }
        public string Not { get; set; } = "";
    }
    public class KrediKaydi
    {
        public int Id { get; set; } public string No { get; set; } = "";
        public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public string Sube { get; set; } = ""; public string KrediNo { get; set; } = ""; public string KrediTuru { get; set; } = "Taksitli Ticari Kredi";
        // ---- yapılandırma öncesi
        public DateTime? KullanimTarihi { get; set; } public decimal Anapara { get; set; } public decimal FaizOrani { get; set; } public int Vade { get; set; } public decimal Taksit { get; set; }
        public decimal KalanAnapara { get; set; } public int KalanVade { get; set; } public DateTime? SonOdemeTarihi { get; set; }
        public int GecikenTaksit { get; set; } public decimal GecikenTutar { get; set; } public decimal GecikmeFaizi { get; set; } public decimal Masraf { get; set; } public string OncekiDurum { get; set; } = "Gecikmede";
        // ---- yapılandırma sonrası
        public DateTime? YapilandirmaTarihi { get; set; } public string ProtokolNo { get; set; } = ""; public decimal YeniAnapara { get; set; } public decimal YeniFaizOrani { get; set; } public decimal BsmvOrani { get; set; } = 5m;
        public int YeniVade { get; set; } public int OdemesizAy { get; set; } public decimal YeniTaksit { get; set; } public DateTime? IlkTaksitTarihi { get; set; } public int OdenenTaksit { get; set; } public string Durum { get; set; } = "Yapılandırıldı";
        /// <summary>Peşinat (9 Kas 2026, "peşinat ödemesi oluyor"): protokolde peşin ödenen tutar; taksitlendirilen yeni anaparanın dışındadır, toplam geri ödemeye girer, Kasa Nakit Akışı'na ayrı satır ("KRD-001 · Peşinat") olarak yazılır. Tarih boşsa yapılandırma tarihi.</summary>
        public decimal Pesinat { get; set; } public DateTime? PesinatTarihi { get; set; } public bool PesinatOdendi { get; set; }
        public List<KrediTeminat> Teminatlar { get; set; } = new List<KrediTeminat>();
        public string Not { get; set; } = "";
        /// <summary>Taksitler Kasa Nakit Akışı'na satır olarak yazılır ve kredi Planlı Düzenli Ödemeler'de izlenir.</summary>
        public bool NakitAkisaAktar { get; set; } = true;
        public bool Ornek { get; set; }
        public int DuzenliOdemeId { get; set; } public string DuzenliOdemeNo { get; set; } = "";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = ""; public DateTime Guncelleme { get; set; } public string Guncelleyen { get; set; } = "";
        public List<TalimatGunluk> Gunluk { get; set; } = new List<TalimatGunluk>();
    }
    public class KrediTaksit { public int No { get; set; } public DateTime Tarih { get; set; } public decimal Tutar { get; set; } public decimal Anapara { get; set; } public decimal Faiz { get; set; } public decimal Kalan { get; set; } }
    /// <summary>Ekrandan gelen kayıt (tarihler yyyy-MM-dd ya da gg.aa.yyyy).</summary>
    public class KrediIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public string Sube { get; set; } public string KrediNo { get; set; } public string KrediTuru { get; set; }
        public string KullanimTarihi { get; set; } public decimal Anapara { get; set; } public decimal FaizOrani { get; set; } public int Vade { get; set; } public decimal Taksit { get; set; }
        public decimal KalanAnapara { get; set; } public int KalanVade { get; set; } public string SonOdemeTarihi { get; set; }
        public int GecikenTaksit { get; set; } public decimal GecikenTutar { get; set; } public decimal GecikmeFaizi { get; set; } public decimal Masraf { get; set; } public string OncekiDurum { get; set; }
        public string YapilandirmaTarihi { get; set; } public string ProtokolNo { get; set; } public decimal YeniAnapara { get; set; } public decimal YeniFaizOrani { get; set; } public decimal BsmvOrani { get; set; } = 5m;
        public int YeniVade { get; set; } public int OdemesizAy { get; set; } public decimal YeniTaksit { get; set; } public string IlkTaksitTarihi { get; set; } public int OdenenTaksit { get; set; } public string Durum { get; set; }
        public decimal Pesinat { get; set; } public string PesinatTarihi { get; set; } public bool PesinatOdendi { get; set; }
        public List<KrediTeminat> Teminatlar { get; set; } public string Not { get; set; } public bool NakitAkisaAktar { get; set; } = true;
    }

    public static class KrediYapilandirmaOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly List<KrediKaydi> _liste = new List<KrediKaydi>();
        static int _sonId; static bool _yuklendi;
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };
        public static readonly string[] KrediTurleri = { "Taksitli Ticari Kredi", "Rotatif Kredi", "Spot Kredi", "İşletme Kredisi", "Yatırım Kredisi", "Kredili Mevduat Hesabı", "Döviz Kredisi", "Leasing", "Diğer" };
        public static readonly string[] TeminatTurleri = { "İpotek", "Kefalet", "Araç Rehni", "Ticari İşletme Rehni", "Teminat Mektubu", "Çek / Senet", "Nakit Blokaj", "Alacak Temliki", "Diğer" };
        public static readonly string[] Durumlar = { "Teklif Aşamasında", "Protokol İmzalandı", "Yapılandırıldı", "Tamamlandı", "İptal" };
        public static readonly string[] OncekiDurumlar = { "Düzenli", "Gecikmede", "Yakın İzleme", "Yasal Takipte" };
        public static readonly string[] Bankalar = { "Ziraat Bankası", "Halkbank", "VakıfBank", "İş Bankası", "Garanti BBVA", "Yapı Kredi", "Akbank", "QNB", "DenizBank", "TEB", "Şekerbank", "ING", "Fibabanka", "Odeabank", "Kuveyt Türk", "Albaraka Türk", "Türkiye Finans", "Vakıf Katılım", "Ziraat Katılım", "Emlak Katılım" };
        /// <summary>Kalıcı dosya (PersonelMaliyetKalici.Dizin altında App_Data); null ise kalıcılık kapalı.</summary>
        public static string Dosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data", "kredi-yapilandirma.json");

        static void Log(KrediKaydi k, string islem, string aciklama, string kullanici) => k.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });
        static DateTime? Tarih(string s)
        {
            s = (s ?? "").Trim(); if (s == "") return null;
            if (DateTime.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) || DateTime.TryParseExact(s, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out t) || DateTime.TryParse(s, Tr, DateTimeStyles.None, out t)) return t.Date;
            return null;
        }
        static string D(DateTime? t) => t?.ToString("yyyy-MM-dd");
        static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);

        // ---------- taksit hesabı ----------
        /// <summary>Aylık oran: yıllık faiz / 12 × (1 + BSMV %) — ticari kredide BSMV faize uygulanır.</summary>
        public static decimal AylikOran(KrediKaydi k) => k.YeniFaizOrani / 100m / 12m * (1 + k.BsmvOrani / 100m);
        public static int TaksitSayisi(KrediKaydi k) => Math.Max(0, k.YeniVade - Math.Max(0, k.OdemesizAy));
        /// <summary>Ödemesiz ayların faizi anaparaya eklenmiş başlangıç borcu.</summary>
        static decimal BaslangicBorcu(KrediKaydi k) => R2(k.YeniAnapara * (decimal)Math.Pow((double)(1 + AylikOran(k)), Math.Max(0, k.OdemesizAy)));
        /// <summary>Anüite (eşit taksit): P × r / (1 − (1 + r)^−n); faiz 0 ise P / n.</summary>
        public static decimal HesaplananTaksit(KrediKaydi k)
        {
            int n = TaksitSayisi(k); if (n <= 0 || k.YeniAnapara <= 0) return 0;
            decimal r = AylikOran(k), p = BaslangicBorcu(k); if (r <= 0) return R2(p / n);
            double rr = (double)r; return R2((decimal)((double)p * rr / (1 - Math.Pow(1 + rr, -n))));
        }
        /// <summary>Taksit takvimi: tutar = yeni taksit (banka) ya da hesaplanan; faiz = kalan × aylık oran, anapara = taksit − faiz; son taksit kalanı kapatır. Tarihler ilk taksitten aylık.</summary>
        public static List<KrediTaksit> Taksitler(KrediKaydi k)
        {
            var l = new List<KrediTaksit>(); int n = TaksitSayisi(k); if (n <= 0 || k.YeniAnapara <= 0) return l;
            decimal r = AylikOran(k), taksit = k.YeniTaksit > 0 ? k.YeniTaksit : HesaplananTaksit(k), kalan = BaslangicBorcu(k);
            var ilk = k.IlkTaksitTarihi ?? (k.YapilandirmaTarihi ?? DateTime.Today).AddMonths(1 + Math.Max(0, k.OdemesizAy));
            for (int i = 1; i <= n && kalan > 0; i++)
            {
                decimal faiz = R2(kalan * r), anapara = i == n ? kalan : Math.Max(0, R2(taksit - faiz));
                if (anapara > kalan) anapara = kalan;
                decimal tutar = i == n || anapara == kalan ? R2(anapara + faiz) : taksit;
                kalan = Math.Max(0, R2(kalan - anapara));
                l.Add(new KrediTaksit { No = i, Tarih = ilk.AddMonths(i - 1), Tutar = tutar, Anapara = anapara, Faiz = faiz, Kalan = kalan });
            }
            return l;
        }

        // ---------- liste ve kalıcılık ----------
        public static List<KrediKaydi> Liste() { Yukle(); lock (Kilit) return _liste.OrderBy(k => k.Id).ToList(); }
        public static KrediKaydi Bul(int id) { Yukle(); lock (Kilit) return _liste.FirstOrDefault(k => k.Id == id); }
        /// <summary>Açılışta (OrnekIsinma, Kasa Nakit Akışı ve Planlı Düzenli Ödemeler tohumlandıktan sonra): dosya okunur, yoksa örnek kayıt; her kayıt için akış satırları yeniden yazılır.</summary>
        public static void Yukle()
        {
            List<KrediKaydi> l;
            lock (Kilit)
            {
                if (_yuklendi) return; _yuklendi = true;
                var dosya = Dosya;
                if (dosya != null && File.Exists(dosya))
                {
                    try { _liste.AddRange(JsonSerializer.Deserialize<List<KrediKaydi>>(File.ReadAllText(dosya), Secenek) ?? new List<KrediKaydi>()); _sonId = _liste.Select(k => k.Id).DefaultIfEmpty(0).Max(); Console.WriteLine($"[Kredi] {_liste.Count} kredi yapılandırma kaydı yüklendi · {dosya}"); }
                    catch (Exception ex) { Console.WriteLine("[Kredi] dosya okunamadı: " + ex.Message); }
                }
                else OrnekTohum();
                foreach (var k in _liste) { if (k.Teminatlar == null) k.Teminatlar = new List<KrediTeminat>(); if (k.Gunluk == null) k.Gunluk = new List<TalimatGunluk>(); }
                l = _liste.ToList();
            }
            foreach (var k in l) { try { Senkron(k, "Sistem"); } catch (Exception ex) { Console.WriteLine($"[Kredi] {k.No} akış senkronu: {ex.Message}"); } }
        }
        static void Yaz()
        {
            var dosya = Dosya; if (dosya == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(dosya)); File.WriteAllText(dosya, JsonSerializer.Serialize(_liste.Where(k => true).ToList(), Secenek)); }
            catch (Exception ex) { Console.WriteLine("[Kredi] dosya yazılamadı: " + ex.Message); }
        }
        /// <summary>Dosya yokken tek örnek kayıt (ekranı göstermek için; akış kapalı, kullanıcı silebilir). Dosya ilk kayıtta yazılır.</summary>
        static void OrnekTohum()
        {
            var k = new KrediKaydi
            {
                Id = ++_sonId, No = "KRD-001", Sirket = "Uras Kimya", Banka = "Halkbank", Sube = "Çorlu Şubesi", KrediNo = "ÖRNEK-2024-0001", KrediTuru = "Taksitli Ticari Kredi",
                KullanimTarihi = new DateTime(2024, 3, 15), Anapara = 5_000_000m, FaizOrani = 42m, Vade = 36, Taksit = 240_000m, KalanAnapara = 2_750_000m, KalanVade = 14, SonOdemeTarihi = new DateTime(2026, 7, 15),
                GecikenTaksit = 3, GecikenTutar = 720_000m, GecikmeFaizi = 95_000m, Masraf = 25_000m, OncekiDurum = "Gecikmede",
                YapilandirmaTarihi = new DateTime(2026, 11, 1), ProtokolNo = "ÖRNEK/2026-11", YeniAnapara = 3_590_000m, YeniFaizOrani = 45m, BsmvOrani = 5m, YeniVade = 36, OdemesizAy = 3, YeniTaksit = 0m, IlkTaksitTarihi = new DateTime(2027, 2, 15), OdenenTaksit = 0, Durum = "Yapılandırıldı",
                Teminatlar = new List<KrediTeminat>
                {
                    new KrediTeminat { Tur = "İpotek", Aciklama = "Çorlu fabrika binası ve arsası", TapuBilgisi = "Tekirdağ / Çorlu / Velimeşe · ada 102 parsel 7", Derece = "1. derece", Malik = "Uras Kimya", Tutar = 6_000_000m, Ekspertiz = 9_500_000m },
                    new KrediTeminat { Tur = "Kefalet", Aciklama = "Şirket ortağı müşterek borçlu ve müteselsil kefil", Malik = "Şirket ortağı", Tutar = 3_590_000m }
                },
                Not = "ÖRNEK KAYIT: ekranı göstermek için eklendi; gerçek kredilerinizi girince silin. Nakit akışa aktarılmaz.", NakitAkisaAktar = false, Ornek = true, Olusturma = DateTime.Today, Olusturan = "Sistem"
            };
            Log(k, "Kayıt", "Örnek kayıt (dosya yok)", "Sistem"); _liste.Add(k);
        }

        // ---------- kayıt ----------
        static string Temiz(string s) => (s ?? "").Trim();
        public static string Kaydet(KrediIstek d, string kullanici, out KrediKaydi kayit, out string ozet)
        {
            kayit = null; ozet = ""; if (d == null) return "Veri yok.";
            Yukle();
            string sirket = Temiz(d.Sirket), banka = Temiz(d.Banka);
            if (!CekAkibetOrnek.Sirketler.Any(s => s.Ad == sirket)) return "Şirket seçilmelidir (Kasa Nakit Akışı şirketleri).";
            if (banka == "") return "Banka zorunludur.";
            if (d.Anapara < 0 || d.KalanAnapara < 0 || d.Taksit < 0 || d.GecikenTutar < 0 || d.GecikmeFaizi < 0 || d.Masraf < 0 || d.YeniAnapara < 0 || d.YeniTaksit < 0 || d.Pesinat < 0) return "Tutarlar negatif olamaz.";
            if (d.FaizOrani < 0 || d.FaizOrani > 300 || d.YeniFaizOrani < 0 || d.YeniFaizOrani > 300 || d.BsmvOrani < 0 || d.BsmvOrani > 100) return "Faiz / BSMV oranı geçersiz.";
            if (d.Vade < 0 || d.Vade > 600 || d.YeniVade < 0 || d.YeniVade > 600 || d.KalanVade < 0 || d.KalanVade > 600) return "Vade 0–600 ay arasında olmalıdır.";
            if (d.OdemesizAy < 0 || d.OdemesizAy >= Math.Max(1, d.YeniVade)) return "Ödemesiz dönem yeni vadeden kısa olmalıdır.";
            if (d.GecikenTaksit < 0 || d.OdenenTaksit < 0) return "Taksit adedi negatif olamaz.";
            if (d.YeniAnapara <= 0 && d.Pesinat <= 0 && d.Durum != "Teklif Aşamasında" && d.Durum != "İptal") return "Yapılandırma sonrası yeni anapara girilmelidir (kalan anapara + geciken taksitler + gecikme faizi + masraflar − peşinat).";
            if (d.YeniVade <= 0 && d.YeniAnapara > 0) return "Yeni vade (ay) girilmelidir.";
            if (d.Pesinat > 0 && string.IsNullOrWhiteSpace(d.PesinatTarihi) && string.IsNullOrWhiteSpace(d.YapilandirmaTarihi)) return "Peşinat için peşinat tarihi ya da yapılandırma tarihi girilmelidir.";
            if (d.OdenenTaksit > Math.Max(0, d.YeniVade - d.OdemesizAy)) return "Ödenen taksit sayısı taksit sayısını aşamaz.";
            var durum = Durumlar.Contains(d.Durum ?? "") ? d.Durum : "Yapılandırıldı"; var onceki = OncekiDurumlar.Contains(d.OncekiDurum ?? "") ? d.OncekiDurum : "Gecikmede";
            var teminatlar = (d.Teminatlar ?? new List<KrediTeminat>()).Where(t => t != null && (Temiz(t.Aciklama) != "" || t.Tutar > 0 || Temiz(t.TapuBilgisi) != "" || Temiz(t.Malik) != ""))
                .Select(t => new KrediTeminat { Tur = TeminatTurleri.Contains(t.Tur ?? "") ? t.Tur : "Diğer", Aciklama = Temiz(t.Aciklama), TapuBilgisi = Temiz(t.TapuBilgisi), Derece = Temiz(t.Derece), Malik = Temiz(t.Malik), Tutar = Math.Max(0, t.Tutar), Ekspertiz = Math.Max(0, t.Ekspertiz), Not = Temiz(t.Not) }).ToList();
            KrediKaydi k; bool yeni;
            lock (Kilit)
            {
                k = d.Id > 0 ? _liste.FirstOrDefault(x => x.Id == d.Id) : null;
                if (d.Id > 0 && k == null) return "Kayıt bulunamadı.";
                yeni = k == null;
                if (yeni) { k = new KrediKaydi { Id = ++_sonId, No = $"KRD-{_sonId:000}", Olusturma = DateTime.Today, Olusturan = kullanici ?? "" }; _liste.Add(k); }
                var eskiTaksit = yeni ? 0 : (Taksitler(k).FirstOrDefault()?.Tutar ?? 0);
                k.Sirket = sirket; k.Banka = banka; k.Sube = Temiz(d.Sube); k.KrediNo = Temiz(d.KrediNo); k.KrediTuru = KrediTurleri.Contains(d.KrediTuru ?? "") ? d.KrediTuru : (Temiz(d.KrediTuru) == "" ? "Diğer" : Temiz(d.KrediTuru));
                k.KullanimTarihi = Tarih(d.KullanimTarihi); k.Anapara = R2(d.Anapara); k.FaizOrani = d.FaizOrani; k.Vade = d.Vade; k.Taksit = R2(d.Taksit); k.KalanAnapara = R2(d.KalanAnapara); k.KalanVade = d.KalanVade; k.SonOdemeTarihi = Tarih(d.SonOdemeTarihi);
                k.GecikenTaksit = d.GecikenTaksit; k.GecikenTutar = R2(d.GecikenTutar); k.GecikmeFaizi = R2(d.GecikmeFaizi); k.Masraf = R2(d.Masraf); k.OncekiDurum = onceki;
                k.YapilandirmaTarihi = Tarih(d.YapilandirmaTarihi); k.ProtokolNo = Temiz(d.ProtokolNo); k.YeniAnapara = R2(d.YeniAnapara); k.YeniFaizOrani = d.YeniFaizOrani; k.BsmvOrani = d.BsmvOrani; k.YeniVade = d.YeniVade; k.OdemesizAy = d.OdemesizAy; k.YeniTaksit = R2(d.YeniTaksit);
                k.IlkTaksitTarihi = Tarih(d.IlkTaksitTarihi); k.OdenenTaksit = d.OdenenTaksit; k.Durum = durum; k.Teminatlar = teminatlar; k.Not = Temiz(d.Not); k.NakitAkisaAktar = d.NakitAkisaAktar; k.Ornek = false;
                k.Pesinat = R2(d.Pesinat); k.PesinatTarihi = Tarih(d.PesinatTarihi); k.PesinatOdendi = d.PesinatOdendi && k.Pesinat > 0;
                k.Guncelleme = DateTime.Now; k.Guncelleyen = kullanici ?? "";
                var yeniTaksit = Taksitler(k).FirstOrDefault()?.Tutar ?? 0;
                Log(k, yeni ? "Kayıt" : "Güncelleme", $"{banka} · {k.KrediNo} · {sirket} · yeni anapara {k.YeniAnapara.ToString("N2", Tr)} ₺ · {k.YeniVade} ay · taksit {yeniTaksit.ToString("N2", Tr)} ₺" + (k.Pesinat > 0 ? $" · peşinat {k.Pesinat.ToString("N2", Tr)} ₺ ({PesinatTarih(k):dd.MM.yyyy}{(k.PesinatOdendi ? ", ödendi" : "")})" : "") + (!yeni && eskiTaksit != yeniTaksit ? $" (önce {eskiTaksit.ToString("N2", Tr)})" : "") + (k.NakitAkisaAktar ? " · nakit akışa aktarılır" : " · nakit akışa aktarılmaz"), kullanici);
                Yaz();
            }
            var s = Senkron(k, kullanici);
            kayit = k; ozet = $"{k.No} {(yeni ? "eklendi" : "güncellendi")}. {SenkronOzet(k, s)}";
            return null;
        }
        public static string Sil(int id, string kullanici, out string ozet)
        {
            ozet = ""; var k = Bul(id); if (k == null) return "Kayıt bulunamadı.";
            lock (Kilit) { _liste.Remove(k); Yaz(); }
            int kal = 0, odenmis = 0;
            foreach (var m in NakitAkisOrnek.KrediSatirlari(k.No + "|")) { if (NakitAkisOrnek.KrediTaksitiKaldir(m.U_KrediTaksit, kullanici) != null) kal++; else odenmis++; }
            if (k.DuzenliOdemeId > 0) DuzenliOdemeOrnek.DurumDegistir(k.DuzenliOdemeId, "Pasif", kullanici);
            ozet = $"{k.No} silindi; Kasa Nakit Akışı'ndan {kal} ödenmemiş taksit satırı kaldırıldı" + (odenmis > 0 ? $", {odenmis} ödenmiş satır kaldı" : "") + (k.DuzenliOdemeId > 0 ? $"; düzenli ödeme {k.DuzenliOdemeNo} pasife alındı" : "") + ".";
            return null;
        }
        public static string AktarDegistir(int id, bool aktar, string kullanici, out string ozet)
        {
            ozet = ""; var k = Bul(id); if (k == null) return "Kayıt bulunamadı.";
            lock (Kilit) { k.NakitAkisaAktar = aktar; k.Ornek = false; Log(k, aktar ? "Nakit Akışa Aktarım Açıldı" : "Nakit Akışa Aktarım Kapatıldı", aktar ? "taksitler Kasa Nakit Akışı ve Planlı Ödemeler'e yazılır" : "ödenmemiş taksit satırları kaldırılır, düzenli ödeme pasife alınır", kullanici); Yaz(); }
            var s = Senkron(k, kullanici); ozet = $"{k.No}: {SenkronOzet(k, s)}"; return null;
        }

        // ---------- Kasa Nakit Akışı ve Planlı Düzenli Ödemeler ----------
        /// <summary>Peşinat tarihi: girilen peşinat tarihi, yoksa yapılandırma tarihi, yoksa bugün.</summary>
        public static DateTime PesinatTarih(KrediKaydi k) => k.PesinatTarihi ?? k.YapilandirmaTarihi ?? DateTime.Today;
        /// <summary>Peşinat + taksit satırlarını yazar / günceller / fazlasını kaldırır ve düzenli ödeme kaydını eşler. Kilit dışında çağrılır (NakitAkis ve Duzenli kendi kilitlerini alır).</summary>
        public static (int eklendi, int guncellendi, int kaldirildi, int odenmis) Senkron(KrediKaydi k, string kullanici)
        {
            var taksitler = Taksitler(k); var anahtarlar = new HashSet<string>(); int e = 0, g = 0, kal = 0, od = 0;
            bool aktif = k.NakitAkisaAktar && k.Durum != "İptal" && k.Durum != "Teklif Aşamasında";
            var db = CekAkibetOrnek.DbName(k.Sirket); if (!CekAkibetOrnek.Sirketler.Any(s => s.Ad == k.Sirket)) aktif = false;
            if (aktif && k.Pesinat > 0)
            {   // peşinat: protokol tarihinde tek seferlik Havale / EFT satırı (anahtar "KRD-001|P")
                string anahtar = $"{k.No}|P"; anahtarlar.Add(anahtar); var pt = PesinatTarih(k);
                var (m, islem) = NakitAkisOrnek.KrediTaksitiYaz(db, anahtar, k.Banka, $"Kredi yapılandırma peşinatı · {k.KrediTuru}{(k.KrediNo != "" ? " · kredi no " + k.KrediNo : "")} · taksitlendirilen anapara {k.YeniAnapara.ToString("N2", Tr)} ₺", $"{k.No} · Peşinat", k.Pesinat, pt, $"Kredi Yapılandırma Raporu {k.No} · {k.Banka}{(k.Sube != "" ? " " + k.Sube : "")}{(k.ProtokolNo != "" ? " · protokol " + k.ProtokolNo : "")} · peşin ödeme, taksitlerin dışında", kullanici);
                if (islem == "eklendi") e++; else if (islem == "güncellendi") g++; else od++;
                if (k.PesinatOdendi && m.U_Durum != "ÖDENDİ") NakitAkisOrnek.OdendiIsaretle(db, m.DocEntry, "Kredi peşinatı " + k.No, kullanici, false, pt);
            }
            if (aktif)
                foreach (var t in taksitler)
                {
                    string anahtar = $"{k.No}|{t.No:000}"; anahtarlar.Add(anahtar);
                    string belgeNo = $"{k.No} · {t.No:00}/{taksitler.Count:00}";
                    string urun = $"Kredi yapılandırma taksidi {t.No}/{taksitler.Count} · {k.KrediTuru}{(k.KrediNo != "" ? " · kredi no " + k.KrediNo : "")} · anapara {t.Anapara.ToString("N2", Tr)} + faiz {t.Faiz.ToString("N2", Tr)}";
                    string not = $"Kredi Yapılandırma Raporu {k.No} · {k.Banka}{(k.Sube != "" ? " " + k.Sube : "")}{(k.ProtokolNo != "" ? " · protokol " + k.ProtokolNo : "")} · taksit sonrası kalan anapara {t.Kalan.ToString("N2", Tr)} ₺";
                    var (m, islem) = NakitAkisOrnek.KrediTaksitiYaz(db, anahtar, k.Banka, urun, belgeNo, t.Tutar, t.Tarih, not, kullanici);
                    if (islem == "eklendi") e++; else if (islem == "güncellendi") g++; else od++;
                    if (t.No <= k.OdenenTaksit && m.U_Durum != "ÖDENDİ") NakitAkisOrnek.OdendiIsaretle(db, m.DocEntry, "Kredi taksidi " + k.No, kullanici, false, t.Tarih);
                }
            foreach (var m in NakitAkisOrnek.KrediSatirlari(k.No + "|")) if (!anahtarlar.Contains(m.U_KrediTaksit) && NakitAkisOrnek.KrediTaksitiKaldir(m.U_KrediTaksit, kullanici) != null) kal++;
            DuzenliSenkron(k, aktif ? taksitler : new List<KrediTaksit>(), kullanici);
            return (e, g, kal, od);
        }
        static string SenkronOzet(KrediKaydi k, (int eklendi, int guncellendi, int kaldirildi, int odenmis) s)
        {
            if (!k.NakitAkisaAktar) return "Nakit akışa aktarım kapalı" + (s.kaldirildi > 0 ? $"; {s.kaldirildi} ödenmemiş satır kaldırıldı" : "") + ".";
            if (k.Durum == "İptal" || k.Durum == "Teklif Aşamasında") return $"Durum \"{k.Durum}\": taksitler nakit akışa yazılmaz" + (s.kaldirildi > 0 ? $"; {s.kaldirildi} satır kaldırıldı" : "") + ".";
            return $"Kasa Nakit Akışı: {s.eklendi} taksit satırı eklendi, {s.guncellendi} güncellendi, {s.kaldirildi} kaldırıldı" + (s.odenmis > 0 ? $", {s.odenmis} ödenmiş satır değişmedi" : "") + (k.DuzenliOdemeNo != "" ? $" · Planlı Düzenli Ödemeler: {k.DuzenliOdemeNo}" : "") + ".";
        }
        /// <summary>Planlı Düzenli Ödemeler kaydı: banka = tedarikçi, "Kredi" kategorisi, Havale / EFT, ilk taksit günü, taksit tutarı; aylık otomatik akış kapalı (DisAkis), taksit ayları Kasa satırlarına bağlanır. Taksit yoksa pasif.</summary>
        static void DuzenliSenkron(KrediKaydi k, List<KrediTaksit> taksitler, string kullanici)
        {
            if (taksitler.Count == 0) { if (k.DuzenliOdemeId > 0) DuzenliOdemeOrnek.DurumDegistir(k.DuzenliOdemeId, "Pasif", kullanici); return; }
            var ilk = taksitler[0]; int gun = Math.Max(1, Math.Min(28, ilk.Tarih.Day));
            string urun = $"Kredi Taksidi · {(k.KrediNo != "" ? k.KrediNo : k.KrediTuru)} ({k.No})";
            string not = $"Kredi Yapılandırma Raporu {k.No}: {taksitler.Count} taksit, ilk {ilk.Tarih:dd.MM.yyyy}, son {taksitler[taksitler.Count - 1].Tarih:dd.MM.yyyy}; taksit satırları rapordan Kasa Nakit Akışı'na yazılır (aylık otomatik akış bu kayıt için kapalı)";
            var r = DuzenliOdemeOrnek.Kaydet(k.DuzenliOdemeId, k.Banka, urun, k.Sirket, "Havale / EFT", "Kredi", gun, ilk.Tutar, not, kullanici, "SG-12", true);
            if (!r.ok || r.d == null) { if (k.DuzenliOdemeId > 0 && r.d == null) { lock (Kilit) { k.DuzenliOdemeId = 0; k.DuzenliOdemeNo = ""; } } return; }
            bool degisti = false; lock (Kilit) { if (k.DuzenliOdemeId != r.d.Id || k.DuzenliOdemeNo != r.d.No) { k.DuzenliOdemeId = r.d.Id; k.DuzenliOdemeNo = r.d.No; degisti = true; } if (degisti) Yaz(); }
            if (r.d.Durum != "Aktif") DuzenliOdemeOrnek.DurumDegistir(r.d.Id, "Aktif", kullanici);
            foreach (var t in taksitler) { var m = NakitAkisOrnek.KrediSatiri($"{k.No}|{t.No:000}"); if (m != null) DuzenliOdemeOrnek.AkisBagla(r.d.Id, t.Tarih.ToString("yyyy-MM"), m, kullanici); }
            if (taksitler[taksitler.Count - 1].Tarih.ToString("yyyy-MM").CompareTo(DateTime.Today.ToString("yyyy-MM")) < 0) DuzenliOdemeOrnek.DurumDegistir(r.d.Id, "Pasif", kullanici);   // takvim bitti
        }

        // ---------- ekran paketi ----------
        public static object Paket()
        {
            var l = Liste();
            return new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), kalici = Dosya != null, dosya = Dosya,
                sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, db = s.DbName }).ToList(), bankalar = Bankalar, krediTurleri = KrediTurleri, teminatTurleri = TeminatTurleri, durumlar = Durumlar, oncekiDurumlar = OncekiDurumlar,
                krediler = l.Select(Dto).ToList(), kasaLink = "/Rapor79?sekme=havale", duzenliLink = "/Rapor123?sekme=duzenli"
            };
        }
        static object Dto(KrediKaydi k)
        {
            var taksitler = Taksitler(k); var db = CekAkibetOrnek.DbName(k.Sirket);
            var kasa = NakitAkisOrnek.KrediSatirlari(k.No + "|").GroupBy(m => m.U_KrediTaksit).ToDictionary(g => g.Key, g => g.First());
            decimal oncesiBorc = k.KalanAnapara + k.GecikenTutar + k.GecikmeFaizi + k.Masraf, taksitToplam = taksitler.Sum(t => t.Tutar), geriOdeme = taksitToplam + k.Pesinat, faiz = taksitler.Sum(t => t.Faiz);
            decimal uygulanan = taksitler.Count > 0 ? taksitler[0].Tutar : 0;
            object KasaJson(NakitAkisViewModel m) => m == null ? null : new { docEntry = m.DocEntry, durum = m.U_Durum, odenme = D(m.U_OdenmeTarihi), db = m.U_KaynakFirma, link = $"/Rapor79?sirket={m.U_KaynakFirma}&sekme=havale" };
            var tl = taksitler.Select(t => { kasa.TryGetValue($"{k.No}|{t.No:000}", out var m); return new { no = t.No, tarih = D(t.Tarih), tutar = t.Tutar, anapara = t.Anapara, faiz = t.Faiz, kalan = t.Kalan, odendi = t.No <= k.OdenenTaksit, kasa = KasaJson(m) }; }).ToList();
            kasa.TryGetValue($"{k.No}|P", out var pm);
            return new
            {
                id = k.Id, no = k.No, sirket = k.Sirket, db, banka = k.Banka, sube = k.Sube, krediNo = k.KrediNo, krediTuru = k.KrediTuru,
                kullanimTarihi = D(k.KullanimTarihi), anapara = k.Anapara, faizOrani = k.FaizOrani, vade = k.Vade, taksit = k.Taksit, kalanAnapara = k.KalanAnapara, kalanVade = k.KalanVade, sonOdemeTarihi = D(k.SonOdemeTarihi),
                gecikenTaksit = k.GecikenTaksit, gecikenTutar = k.GecikenTutar, gecikmeFaizi = k.GecikmeFaizi, masraf = k.Masraf, oncekiDurum = k.OncekiDurum, oncesiToplamBorc = oncesiBorc,
                yapilandirmaTarihi = D(k.YapilandirmaTarihi), protokolNo = k.ProtokolNo, yeniAnapara = k.YeniAnapara, yeniFaizOrani = k.YeniFaizOrani, bsmvOrani = k.BsmvOrani, yeniVade = k.YeniVade, odemesizAy = k.OdemesizAy, yeniTaksit = k.YeniTaksit,
                hesaplananTaksit = HesaplananTaksit(k), uygulananTaksit = uygulanan, ilkTaksitTarihi = D(taksitler.Count > 0 ? taksitler[0].Tarih : k.IlkTaksitTarihi), sonTaksitTarihi = D(taksitler.Count > 0 ? taksitler[taksitler.Count - 1].Tarih : (DateTime?)null), odenenTaksit = k.OdenenTaksit, taksitSayisi = taksitler.Count, durum = k.Durum,
                pesinat = k.Pesinat, pesinatTarihi = k.Pesinat > 0 ? D(PesinatTarih(k)) : null, pesinatOdendi = k.PesinatOdendi, pesinatKasa = KasaJson(pm),
                toplamGeriOdeme = geriOdeme, taksitToplam, toplamFaiz = faiz, toplamAnapara = taksitler.Sum(t => t.Anapara), aylikFark = uygulanan - k.Taksit, vadeFarki = k.YeniVade - k.KalanVade, borcFarki = geriOdeme - oncesiBorc,
                teminatlar = k.Teminatlar, teminatToplam = k.Teminatlar.Sum(t => t.Tutar), ekspertizToplam = k.Teminatlar.Sum(t => t.Ekspertiz), ipotekToplam = k.Teminatlar.Where(t => t.Tur == "İpotek").Sum(t => t.Tutar),
                not = k.Not, nakitAkisaAktar = k.NakitAkisaAktar, ornek = k.Ornek, duzenli = k.DuzenliOdemeId > 0 ? new { id = k.DuzenliOdemeId, no = k.DuzenliOdemeNo, link = "/Rapor123?sekme=duzenli" } : null,
                kasaOzet = new { satir = kasa.Count, odendi = kasa.Values.Count(m => m.U_Durum == "ÖDENDİ"), odenenTutar = kasa.Values.Where(m => m.U_Durum == "ÖDENDİ").Sum(m => m.U_TutarTL ?? 0), planli = kasa.Values.Count(m => m.U_Durum != "ÖDENDİ"), planliTutar = kasa.Values.Where(m => m.U_Durum != "ÖDENDİ").Sum(m => m.U_TutarTL ?? 0), link = $"/Rapor79?sirket={db}&sekme=havale" },
                taksitler = tl, olusturma = D(k.Olusturma), olusturan = k.Olusturan, guncelleme = k.Guncelleme == default ? null : k.Guncelleme.ToString("yyyy-MM-dd HH:mm"), guncelleyen = k.Guncelleyen,
                gunluk = k.Gunluk.OrderByDescending(g => g.Tarih).Take(20).Select(g => new { tarih = g.Tarih.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, g.Islem, g.Aciklama }).ToList()
            };
        }
    }
}
