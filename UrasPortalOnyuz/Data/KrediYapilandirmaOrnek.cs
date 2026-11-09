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
//   Kalıcılık: App_Data/kredi-yapilandirma.json (PersonelMaliyetKalici.Dizin; git dışı; kök KrediDepo: krediler + bağımsız rehin / ipotek kayıtları + teminat çekleri + belgeler; eski düz dizi tanınır). Dosya yoksa tek ÖRNEK kayıt (akış kapalı).
//   Belgeler (9 Kas 2026, "sözleşmelerin PDF'ini taratıp yükleyelim, GKS'leri yükleyelim … teminata verilen çekler"): sözleşme / protokol / GKS / ipotek / rehin / teminat mektubu / çek / ekspertiz / fek yazısı PDF-JPG-PNG, App_Data/kredi-belgeler altında;
//   krediye, teminat çekine ya da rehin kaydına bağlanabilir. Rehin · İpotek Takibi: kredi teminatları (Durum / tesis / fek) + eski krediler için bağımsız kayıtlar. Teminat Çekleri: bankaya teminat verilen müşteri çek / senetleri, durum ve iade takibi.
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
        /// <summary>Rehin · ipotek takibi (9 Kas 2026): Aktif | Fek Bekliyor (kredi kapandı, kaldırılmadı) | Fek Edildi | Paraya Çevrildi; tesis ve fek tarihleri.</summary>
        public string Durum { get; set; } = "Aktif"; public DateTime? TesisTarihi { get; set; } public DateTime? FekTarihi { get; set; }
    }
    /// <summary>Bağımsız rehin / ipotek kaydı (eski ya da yapılandırılmamış kredilerin teminatları; 9 Kas 2026 "eski kredilerin rehin ve ipotek takibi"): hangi banka / kredi için tesis edildi, durumu, fek.</summary>
    public class TeminatKaydi
    {
        public int Id { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public string KrediRef { get; set; } = ""; public int KrediId { get; set; }
        public string Tur { get; set; } = "İpotek"; public string Aciklama { get; set; } = ""; public string TapuBilgisi { get; set; } = ""; public string Derece { get; set; } = ""; public string Malik { get; set; } = "";
        public decimal Tutar { get; set; } public decimal Ekspertiz { get; set; } public DateTime? TesisTarihi { get; set; } public string Durum { get; set; } = "Aktif"; public DateTime? FekTarihi { get; set; } public DateTime? KrediKapanis { get; set; }
        public string Not { get; set; } = ""; public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
    }
    public class TeminatIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public string KrediRef { get; set; } public int KrediId { get; set; }
        public string Tur { get; set; } public string Aciklama { get; set; } public string TapuBilgisi { get; set; } public string Derece { get; set; } public string Malik { get; set; }
        public decimal Tutar { get; set; } public decimal Ekspertiz { get; set; } public string TesisTarihi { get; set; } public string Durum { get; set; } public string FekTarihi { get; set; } public string KrediKapanis { get; set; } public string Not { get; set; }
    }
    /// <summary>Bankaya teminat olarak verilen müşteri çeki / senedi (9 Kas 2026 "teminata verilen çekler"): çek no, keşideci, çekin bankası, vade, tutar, veriliş, durum (Teminatta | İade Alındı | Tahsil Edildi | Karşılıksız).</summary>
    public class TeminatCeki
    {
        public int Id { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public int KrediId { get; set; } public string KrediRef { get; set; } = "";
        public string Belge { get; set; } = "Çek"; public string CekNo { get; set; } = ""; public string Kesideci { get; set; } = ""; public string CekBankasi { get; set; } = ""; public DateTime? Vade { get; set; } public decimal Tutar { get; set; } public string PB { get; set; } = "TRY";
        public DateTime? VerilisTarihi { get; set; } public string Durum { get; set; } = "Teminatta"; public DateTime? IadeTarihi { get; set; } public string Not { get; set; } = ""; public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
    }
    public class CekIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public int KrediId { get; set; } public string KrediRef { get; set; }
        public string Belge { get; set; } public string CekNo { get; set; } public string Kesideci { get; set; } public string CekBankasi { get; set; } public string Vade { get; set; } public decimal Tutar { get; set; } public string PB { get; set; }
        public string VerilisTarihi { get; set; } public string Durum { get; set; } public string IadeTarihi { get; set; } public string Not { get; set; }
    }
    /// <summary>Yüklenen belge (9 Kas 2026 "sözleşmelerin PDF'ini taratıp yükleyelim, GKS'leri yükleyelim"): tür (Kredi Sözleşmesi, Yapılandırma Protokolü, GKS, İpotek Belgesi, Rehin Sözleşmesi, Teminat Mektubu, Teminat Çeki, Ödeme Planı, Ekspertiz Raporu, Fek Yazısı, Diğer),
    /// kredi / çek / teminat bağı (isteğe bağlı), şirket, banka, belge tarihi; dosya App_Data/kredi-belgeler altında (PDF / JPG / PNG, en çok 20 MB).</summary>
    public class KrediBelge
    {
        public int Id { get; set; } public string Tur { get; set; } = "Kredi Sözleşmesi"; public string Baslik { get; set; } = ""; public int KrediId { get; set; } public string KrediNo { get; set; } = ""; public int CekId { get; set; } public int TeminatId { get; set; }
        public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public DateTime? Tarih { get; set; } public string Aciklama { get; set; } = "";
        public string DosyaAdi { get; set; } = ""; public string DosyaYolu { get; set; } = ""; public long Boyut { get; set; } public string IcerikTuru { get; set; } = ""; public string Yukleyen { get; set; } = ""; public DateTime Yukleme { get; set; }
    }
    /// <summary>Kalıcı dosyanın kökü (App_Data/kredi-yapilandirma.json): krediler + bağımsız teminatlar + teminat çekleri + belgeler. Eski dosya düz kredi dizisiydi; okunurken tanınır.</summary>
    public class KrediDepo { public List<KrediKaydi> Krediler { get; set; } = new List<KrediKaydi>(); public List<TeminatKaydi> Teminatlar { get; set; } = new List<TeminatKaydi>(); public List<TeminatCeki> Cekler { get; set; } = new List<TeminatCeki>(); public List<KrediBelge> Belgeler { get; set; } = new List<KrediBelge>(); }
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
        static readonly List<TeminatKaydi> _teminatlar = new List<TeminatKaydi>(); static readonly List<TeminatCeki> _cekler = new List<TeminatCeki>(); static readonly List<KrediBelge> _belgeler = new List<KrediBelge>();
        static int _sonId, _sonTeminatId, _sonCekId, _sonBelgeId; static bool _yuklendi;
        public static readonly string[] TeminatDurumlari = { "Aktif", "Fek Bekliyor", "Fek Edildi", "Paraya Çevrildi" };
        public static readonly string[] CekDurumlari = { "Teminatta", "İade Alındı", "Tahsil Edildi", "Karşılıksız" };
        public static readonly string[] BelgeTurleri = { "Kredi Sözleşmesi", "Yapılandırma Protokolü", "Genel Kredi Sözleşmesi (GKS)", "İpotek Belgesi", "Rehin Sözleşmesi", "Teminat Mektubu", "Teminat Çeki", "Ödeme Planı", "Ekspertiz Raporu", "Fek Yazısı", "Diğer" };
        public static readonly string[] BelgeUzantilari = { ".pdf", ".jpg", ".jpeg", ".png" };
        public const long BelgeEnCok = 20 * 1024 * 1024;
        /// <summary>Belge dosyalarının klasörü (App_Data/kredi-belgeler); kalıcılık kapalıysa null.</summary>
        public static string BelgeKlasoru => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data", "kredi-belgeler");
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
                    try
                    {
                        string metin = File.ReadAllText(dosya);
                        using (var doc = JsonDocument.Parse(metin))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.Array) _liste.AddRange(JsonSerializer.Deserialize<List<KrediKaydi>>(metin, Secenek) ?? new List<KrediKaydi>());   // eski biçim: yalnız krediler
                            else { var depo = JsonSerializer.Deserialize<KrediDepo>(metin, Secenek) ?? new KrediDepo(); _liste.AddRange(depo.Krediler ?? new List<KrediKaydi>()); _teminatlar.AddRange(depo.Teminatlar ?? new List<TeminatKaydi>()); _cekler.AddRange(depo.Cekler ?? new List<TeminatCeki>()); _belgeler.AddRange(depo.Belgeler ?? new List<KrediBelge>()); }
                        }
                        _sonId = _liste.Select(k => k.Id).DefaultIfEmpty(0).Max(); _sonTeminatId = _teminatlar.Select(t => t.Id).DefaultIfEmpty(0).Max(); _sonCekId = _cekler.Select(c => c.Id).DefaultIfEmpty(0).Max(); _sonBelgeId = _belgeler.Select(b => b.Id).DefaultIfEmpty(0).Max();
                        Console.WriteLine($"[Kredi] {_liste.Count} kredi, {_teminatlar.Count} rehin / ipotek, {_cekler.Count} teminat çeki, {_belgeler.Count} belge yüklendi · {dosya}");
                    }
                    catch (Exception ex) { Console.WriteLine("[Kredi] dosya okunamadı: " + ex.Message); }
                }
                else OrnekTohum();
                foreach (var k in _liste) { if (k.Teminatlar == null) k.Teminatlar = new List<KrediTeminat>(); if (k.Gunluk == null) k.Gunluk = new List<TalimatGunluk>(); foreach (var t in k.Teminatlar) if (string.IsNullOrEmpty(t.Durum)) t.Durum = "Aktif"; }
                l = _liste.ToList();
            }
            foreach (var k in l) { try { Senkron(k, "Sistem"); } catch (Exception ex) { Console.WriteLine($"[Kredi] {k.No} akış senkronu: {ex.Message}"); } }
        }
        static void Yaz()
        {
            var dosya = Dosya; if (dosya == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(dosya)); File.WriteAllText(dosya, JsonSerializer.Serialize(new KrediDepo { Krediler = _liste, Teminatlar = _teminatlar, Cekler = _cekler, Belgeler = _belgeler }, Secenek)); }
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
                .Select(t => new KrediTeminat { Tur = TeminatTurleri.Contains(t.Tur ?? "") ? t.Tur : "Diğer", Aciklama = Temiz(t.Aciklama), TapuBilgisi = Temiz(t.TapuBilgisi), Derece = Temiz(t.Derece), Malik = Temiz(t.Malik), Tutar = Math.Max(0, t.Tutar), Ekspertiz = Math.Max(0, t.Ekspertiz), Not = Temiz(t.Not),
                    Durum = TeminatDurumlari.Contains(t.Durum ?? "") ? t.Durum : "Aktif", TesisTarihi = t.TesisTarihi?.Date, FekTarihi = t.FekTarihi?.Date }).ToList();
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
                var (m, islem) = NakitAkisOrnek.KrediTaksitiYaz(db, anahtar, k.Banka, $"Kredi yapılandırma peşinatı · {k.KrediTuru}{(k.KrediNo != "" ? " · kredi no " + k.KrediNo : "")} · taksitlendirilen anapara {k.YeniAnapara.ToString("N2", Tr)} ₺", $"{k.No} · Peşinat", k.Pesinat, pt, $"Kredi Raporu {k.No} · {k.Banka}{(k.Sube != "" ? " " + k.Sube : "")}{(k.ProtokolNo != "" ? " · protokol " + k.ProtokolNo : "")} · peşin ödeme, taksitlerin dışında", kullanici);
                if (islem == "eklendi") e++; else if (islem == "güncellendi") g++; else od++;
                if (k.PesinatOdendi && m.U_Durum != "ÖDENDİ") NakitAkisOrnek.OdendiIsaretle(db, m.DocEntry, "Kredi peşinatı " + k.No, kullanici, false, pt);
            }
            if (aktif)
                foreach (var t in taksitler)
                {
                    string anahtar = $"{k.No}|{t.No:000}"; anahtarlar.Add(anahtar);
                    string belgeNo = $"{k.No} · {t.No:00}/{taksitler.Count:00}";
                    string urun = $"Kredi yapılandırma taksidi {t.No}/{taksitler.Count} · {k.KrediTuru}{(k.KrediNo != "" ? " · kredi no " + k.KrediNo : "")} · anapara {t.Anapara.ToString("N2", Tr)} + faiz {t.Faiz.ToString("N2", Tr)}";
                    string not = $"Kredi Raporu {k.No} · {k.Banka}{(k.Sube != "" ? " " + k.Sube : "")}{(k.ProtokolNo != "" ? " · protokol " + k.ProtokolNo : "")} · taksit sonrası kalan anapara {t.Kalan.ToString("N2", Tr)} ₺";
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
            string not = $"Kredi Raporu {k.No}: {taksitler.Count} taksit, ilk {ilk.Tarih:dd.MM.yyyy}, son {taksitler[taksitler.Count - 1].Tarih:dd.MM.yyyy}; taksit satırları rapordan Kasa Nakit Akışı'na yazılır (aylık otomatik akış bu kayıt için kapalı)";
            var r = DuzenliOdemeOrnek.Kaydet(k.DuzenliOdemeId, k.Banka, urun, k.Sirket, "Havale / EFT", "Kredi", gun, ilk.Tutar, not, kullanici, "SG-12", true);
            if (!r.ok || r.d == null) { if (k.DuzenliOdemeId > 0 && r.d == null) { lock (Kilit) { k.DuzenliOdemeId = 0; k.DuzenliOdemeNo = ""; } } return; }
            bool degisti = false; lock (Kilit) { if (k.DuzenliOdemeId != r.d.Id || k.DuzenliOdemeNo != r.d.No) { k.DuzenliOdemeId = r.d.Id; k.DuzenliOdemeNo = r.d.No; degisti = true; } if (degisti) Yaz(); }
            if (r.d.Durum != "Aktif") DuzenliOdemeOrnek.DurumDegistir(r.d.Id, "Aktif", kullanici);
            foreach (var t in taksitler) { var m = NakitAkisOrnek.KrediSatiri($"{k.No}|{t.No:000}"); if (m != null) DuzenliOdemeOrnek.AkisBagla(r.d.Id, t.Tarih.ToString("yyyy-MM"), m, kullanici); }
            if (taksitler[taksitler.Count - 1].Tarih.ToString("yyyy-MM").CompareTo(DateTime.Today.ToString("yyyy-MM")) < 0) DuzenliOdemeOrnek.DurumDegistir(r.d.Id, "Pasif", kullanici);   // takvim bitti
        }

        // ---------- rehin / ipotek takibi (bağımsız kayıtlar: eski krediler) ----------
        public static string TeminatKaydet(TeminatIstek d, string kullanici, out TeminatKaydi kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string sirket = Temiz(d.Sirket), banka = Temiz(d.Banka);
            if (sirket == "") return "Şirket seçilmelidir."; if (banka == "") return "Banka zorunludur.";
            if (Temiz(d.Aciklama) == "" && Temiz(d.TapuBilgisi) == "") return "Teminat açıklaması ya da tapu / kimlik bilgisi girilmelidir.";
            if (d.Tutar < 0 || d.Ekspertiz < 0) return "Tutarlar negatif olamaz.";
            lock (Kilit)
            {
                var t = d.Id > 0 ? _teminatlar.FirstOrDefault(x => x.Id == d.Id) : null; if (d.Id > 0 && t == null) return "Kayıt bulunamadı.";
                if (t == null) { t = new TeminatKaydi { Id = ++_sonTeminatId, Olusturma = DateTime.Today, Olusturan = kullanici ?? "" }; _teminatlar.Add(t); }
                t.Sirket = sirket; t.Banka = banka; t.KrediRef = Temiz(d.KrediRef); t.KrediId = d.KrediId > 0 && _liste.Any(k => k.Id == d.KrediId) ? d.KrediId : 0;
                t.Tur = TeminatTurleri.Contains(d.Tur ?? "") ? d.Tur : "Diğer"; t.Aciklama = Temiz(d.Aciklama); t.TapuBilgisi = Temiz(d.TapuBilgisi); t.Derece = Temiz(d.Derece); t.Malik = Temiz(d.Malik);
                t.Tutar = R2(d.Tutar); t.Ekspertiz = R2(d.Ekspertiz); t.TesisTarihi = Tarih(d.TesisTarihi); t.Durum = TeminatDurumlari.Contains(d.Durum ?? "") ? d.Durum : "Aktif"; t.FekTarihi = Tarih(d.FekTarihi); t.KrediKapanis = Tarih(d.KrediKapanis); t.Not = Temiz(d.Not);
                if (t.Durum == "Fek Edildi" && t.FekTarihi == null) t.FekTarihi = DateTime.Today;
                Yaz(); kayit = t;
            }
            return null;
        }
        public static string TeminatSil(int id)
        {
            Yukle(); lock (Kilit) { var t = _teminatlar.FirstOrDefault(x => x.Id == id); if (t == null) return "Kayıt bulunamadı."; _teminatlar.Remove(t); Yaz(); } return null;
        }
        // ---------- teminata verilen çekler ----------
        public static string CekKaydet(CekIstek d, string kullanici, out TeminatCeki kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string sirket = Temiz(d.Sirket), banka = Temiz(d.Banka);
            if (sirket == "") return "Şirket seçilmelidir."; if (banka == "") return "Teminatın verildiği banka zorunludur.";
            if (Temiz(d.CekNo) == "") return "Çek / senet no zorunludur."; if (d.Tutar <= 0) return "Tutar sıfırdan büyük olmalıdır.";
            lock (Kilit)
            {
                var c = d.Id > 0 ? _cekler.FirstOrDefault(x => x.Id == d.Id) : null; if (d.Id > 0 && c == null) return "Kayıt bulunamadı.";
                if (c == null) { c = new TeminatCeki { Id = ++_sonCekId, Olusturma = DateTime.Today, Olusturan = kullanici ?? "" }; _cekler.Add(c); }
                c.Sirket = sirket; c.Banka = banka; c.KrediId = d.KrediId > 0 && _liste.Any(k => k.Id == d.KrediId) ? d.KrediId : 0; c.KrediRef = Temiz(d.KrediRef);
                c.Belge = Temiz(d.Belge) == "Senet" ? "Senet" : "Çek"; c.CekNo = Temiz(d.CekNo); c.Kesideci = Temiz(d.Kesideci); c.CekBankasi = Temiz(d.CekBankasi); c.Vade = Tarih(d.Vade); c.Tutar = R2(d.Tutar); c.PB = string.IsNullOrWhiteSpace(d.PB) ? "TRY" : Temiz(d.PB).ToUpperInvariant();
                c.VerilisTarihi = Tarih(d.VerilisTarihi); c.Durum = CekDurumlari.Contains(d.Durum ?? "") ? d.Durum : "Teminatta"; c.IadeTarihi = Tarih(d.IadeTarihi); c.Not = Temiz(d.Not);
                if (c.Durum == "İade Alındı" && c.IadeTarihi == null) c.IadeTarihi = DateTime.Today;
                Yaz(); kayit = c;
            }
            return null;
        }
        public static string CekSil(int id)
        {
            Yukle(); lock (Kilit) { var c = _cekler.FirstOrDefault(x => x.Id == id); if (c == null) return "Kayıt bulunamadı."; _cekler.Remove(c); Yaz(); } return null;
        }
        // ---------- belgeler (sözleşme, GKS, ipotek, çek fotokopisi …) ----------
        public static string BelgeEkle(string tur, string baslik, int krediId, int cekId, int teminatId, string sirket, string banka, string tarih, string aciklama, string dosyaAdi, string icerikTuru, byte[] veri, string kullanici, out KrediBelge kayit)
        {
            kayit = null; Yukle();
            var klasor = BelgeKlasoru; if (klasor == null) return "Belge klasörü tanımlı değil (kalıcılık kapalı).";
            if (veri == null || veri.Length == 0) return "Dosya seçilmedi."; if (veri.Length > BelgeEnCok) return "Dosya 20 MB'tan büyük olamaz.";
            string uz = Path.GetExtension(dosyaAdi ?? "").ToLowerInvariant(); if (!BelgeUzantilari.Contains(uz)) return "Yalnız PDF, JPG ve PNG yüklenebilir.";
            if (!BelgeTurleri.Contains(tur ?? "")) tur = "Diğer";
            lock (Kilit)
            {
                var kredi = krediId > 0 ? _liste.FirstOrDefault(k => k.Id == krediId) : null;
                var b = new KrediBelge { Id = ++_sonBelgeId, Tur = tur, Baslik = Temiz(baslik) != "" ? Temiz(baslik) : Path.GetFileNameWithoutExtension(dosyaAdi), KrediId = kredi?.Id ?? 0, KrediNo = kredi?.No ?? "", CekId = cekId > 0 && _cekler.Any(c => c.Id == cekId) ? cekId : 0, TeminatId = teminatId > 0 && _teminatlar.Any(t => t.Id == teminatId) ? teminatId : 0,
                    Sirket = Temiz(sirket) != "" ? Temiz(sirket) : kredi?.Sirket ?? "", Banka = Temiz(banka) != "" ? Temiz(banka) : kredi?.Banka ?? "", Tarih = Tarih(tarih), Aciklama = Temiz(aciklama), DosyaAdi = Path.GetFileName(dosyaAdi), Boyut = veri.Length, IcerikTuru = uz == ".pdf" ? "application/pdf" : uz == ".png" ? "image/png" : "image/jpeg", Yukleyen = kullanici ?? "", Yukleme = DateTime.Now };
                string guvenli = new string(Path.GetFileNameWithoutExtension(b.DosyaAdi).Select(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_').ToArray()); if (guvenli.Length > 60) guvenli = guvenli.Substring(0, 60);
                b.DosyaYolu = $"{b.Id:0000}_{guvenli}{uz}";
                try { Directory.CreateDirectory(klasor); File.WriteAllBytes(Path.Combine(klasor, b.DosyaYolu), veri); } catch (Exception ex) { return "Dosya yazılamadı: " + ex.Message; }
                _belgeler.Add(b); if (kredi != null) Log(kredi, "Belge Yüklendi", $"{b.Tur} · {b.Baslik} · {b.DosyaAdi} ({Math.Max(1, b.Boyut / 1024)} KB)", kullanici);
                Yaz(); kayit = b;
            }
            return null;
        }
        public static KrediBelge BelgeBul(int id) { Yukle(); lock (Kilit) return _belgeler.FirstOrDefault(b => b.Id == id); }
        /// <summary>Belgenin diskteki tam yolu (yoksa null).</summary>
        public static string BelgeYolu(KrediBelge b) { var klasor = BelgeKlasoru; if (b == null || klasor == null || string.IsNullOrEmpty(b.DosyaYolu)) return null; var yol = Path.Combine(klasor, b.DosyaYolu); return File.Exists(yol) ? yol : null; }
        public static string BelgeSil(int id, string kullanici)
        {
            Yukle();
            lock (Kilit)
            {
                var b = _belgeler.FirstOrDefault(x => x.Id == id); if (b == null) return "Belge bulunamadı.";
                var yol = BelgeYolu(b); if (yol != null) { try { File.Delete(yol); } catch { /* dosya silinemezse kayıt yine kalkar */ } }
                _belgeler.Remove(b); var kredi = b.KrediId > 0 ? _liste.FirstOrDefault(k => k.Id == b.KrediId) : null; if (kredi != null) Log(kredi, "Belge Silindi", $"{b.Tur} · {b.Baslik} · {b.DosyaAdi}", kullanici);
                Yaz();
            }
            return null;
        }

        // ---------- ekran paketi ----------
        static object BelgeDto(KrediBelge b) => new { b.Id, b.Tur, b.Baslik, b.KrediId, b.KrediNo, b.CekId, b.TeminatId, b.Sirket, b.Banka, tarih = D(b.Tarih), b.Aciklama, b.DosyaAdi, b.Boyut, b.IcerikTuru, b.Yukleyen, yukleme = b.Yukleme.ToString("yyyy-MM-dd HH:mm"), link = $"/KrediYapilandirma/Belge/{b.Id}", indir = $"/KrediYapilandirma/Belge/{b.Id}?indir=1", var = BelgeYolu(b) != null };
        public static object Paket()
        {
            var l = Liste(); List<TeminatKaydi> tl; List<TeminatCeki> cl; List<KrediBelge> bl; lock (Kilit) { tl = _teminatlar.ToList(); cl = _cekler.ToList(); bl = _belgeler.ToList(); }
            var belgeSayac = bl.GroupBy(b => b.KrediId).ToDictionary(g => g.Key, g => g.Count());
            return new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), kalici = Dosya != null, dosya = Dosya, belgeKlasoru = BelgeKlasoru,
                sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, db = s.DbName }).ToList(), bankalar = Bankalar, krediTurleri = KrediTurleri, teminatTurleri = TeminatTurleri, durumlar = Durumlar, oncekiDurumlar = OncekiDurumlar,
                teminatDurumlari = TeminatDurumlari, cekDurumlari = CekDurumlari, belgeTurleri = BelgeTurleri,
                krediler = l.Select(k => Dto(k, belgeSayac.TryGetValue(k.Id, out var n) ? n : 0, cl.Count(c => c.KrediId == k.Id))).ToList(),
                teminatlar = tl.OrderBy(t => t.Durum == "Fek Edildi" ? 1 : 0).ThenBy(t => t.Banka).Select(t => new { t.Id, t.Sirket, t.Banka, t.KrediRef, t.KrediId, krediNo = l.FirstOrDefault(k => k.Id == t.KrediId)?.No ?? "", t.Tur, t.Aciklama, t.TapuBilgisi, t.Derece, t.Malik, t.Tutar, t.Ekspertiz, tesisTarihi = D(t.TesisTarihi), t.Durum, fekTarihi = D(t.FekTarihi), krediKapanis = D(t.KrediKapanis), t.Not, olusturma = D(t.Olusturma), t.Olusturan, belgeSayisi = bl.Count(b => b.TeminatId == t.Id) }).ToList(),
                cekler = cl.OrderBy(c => c.Vade).Select(c => new { c.Id, c.Sirket, c.Banka, c.KrediId, krediNo = l.FirstOrDefault(k => k.Id == c.KrediId)?.No ?? "", c.KrediRef, c.Belge, c.CekNo, c.Kesideci, c.CekBankasi, vade = D(c.Vade), c.Tutar, c.PB, verilisTarihi = D(c.VerilisTarihi), c.Durum, iadeTarihi = D(c.IadeTarihi), c.Not, olusturma = D(c.Olusturma), c.Olusturan, belgeSayisi = bl.Count(b => b.CekId == c.Id) }).ToList(),
                belgeler = bl.OrderByDescending(b => b.Yukleme).Select(BelgeDto).ToList(),
                kasaLink = "/Rapor79?sekme=havale", duzenliLink = "/Rapor123?sekme=duzenli"
            };
        }
        static object Dto(KrediKaydi k, int belgeSayisi = 0, int cekSayisi = 0)
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
                teminatlar = k.Teminatlar.Select(t => new { t.Tur, t.Aciklama, t.TapuBilgisi, t.Derece, t.Malik, t.Tutar, t.Ekspertiz, t.Not, durum = string.IsNullOrEmpty(t.Durum) ? "Aktif" : t.Durum, tesisTarihi = D(t.TesisTarihi), fekTarihi = D(t.FekTarihi) }).ToList(),
                teminatToplam = k.Teminatlar.Where(t => t.Durum != "Fek Edildi" && t.Durum != "Paraya Çevrildi").Sum(t => t.Tutar), ekspertizToplam = k.Teminatlar.Sum(t => t.Ekspertiz), ipotekToplam = k.Teminatlar.Where(t => t.Tur == "İpotek" && t.Durum != "Fek Edildi" && t.Durum != "Paraya Çevrildi").Sum(t => t.Tutar), belgeSayisi, cekSayisi,
                not = k.Not, nakitAkisaAktar = k.NakitAkisaAktar, ornek = k.Ornek, duzenli = k.DuzenliOdemeId > 0 ? new { id = k.DuzenliOdemeId, no = k.DuzenliOdemeNo, link = "/Rapor123?sekme=duzenli" } : null,
                kasaOzet = new { satir = kasa.Count, odendi = kasa.Values.Count(m => m.U_Durum == "ÖDENDİ"), odenenTutar = kasa.Values.Where(m => m.U_Durum == "ÖDENDİ").Sum(m => m.U_TutarTL ?? 0), planli = kasa.Values.Count(m => m.U_Durum != "ÖDENDİ"), planliTutar = kasa.Values.Where(m => m.U_Durum != "ÖDENDİ").Sum(m => m.U_TutarTL ?? 0), link = $"/Rapor79?sirket={db}&sekme=havale" },
                taksitler = tl, olusturma = D(k.Olusturma), olusturan = k.Olusturan, guncelleme = k.Guncelleme == default ? null : k.Guncelleme.ToString("yyyy-MM-dd HH:mm"), guncelleyen = k.Guncelleyen,
                gunluk = k.Gunluk.OrderByDescending(g => g.Tarih).Take(20).Select(g => new { tarih = g.Tarih.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, g.Islem, g.Aciklama }).ToList()
            };
        }
    }
}
