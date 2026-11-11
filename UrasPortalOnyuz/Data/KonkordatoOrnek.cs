// ============================================================
// KONKORDATO · ALACAKLI BANKALAR (10 Kas 2026, kullanıcı: "Bakırköy 2. ATM bilirkişi raporundaki tüm detaylar kredi raporunda olmalı")
//   Kredi Raporu'nun (Controllers/KrediYapilandirmaController.cs, Views/KrediYapilandirma/Index.cshtml, wwwroot/js/kredi-yapilandirma.js) Konkordato sekme kümesi:
//   Konkordato · Alacaklı Bankalar (banka × borçlu alacak kaydı: kat ihtarı / tebliğ / temerrüt, geçici mühlet tarihi (05.11.2024) itibariyle nakdi alacak, kat ihtarına itibar edilen tutar,
//   tazmin bedelleri, mühlet sonrası tahsilat, bakiye; rapor tanzim tarihi (19.02.2026) faiz hesabı: akdi faiz, temerrüt faizi, dönem satırları (tutar × gün × oran / 36000), BSMV %5;
//   gayrinakdi çek taahhüt bedeli (yaprak × 9.270 ₺); rehin miktarı ve alacak statüsü (adi / imtiyazlı-rüçhanlı); hukuki değerlendirme) + Kredi Sözleşmeleri · Kefiller (GKS / finansal kiralama,
//   müteselsil kefiller ve kefalet limitleri, dava içi kefil işareti) + Mühlet Tarihi Bakiyeleri (bankanın kredi / hesap satırları: kredi no, tür, akdi faiz, anapara, döviz, faiz başlangıcı,
//   işlemiş faiz / komisyon, BSMV + KKDF, KDV, temerrüt faizi, bakiye) + Mühlet Sonrası Tahsilatlar (çek / havale / şube / mevduat rehni / teminat çeki / haricen; çek no, çekin bankası, ödeyen,
//   firma payı, mahsup işareti) + Teminat Mektupları (mektup no, KKB no, veriliş, geçerlilik, tutar, kur, tazmin tarihi ve bedeli, durum; 10 Kas 2026: muhatap, istinaden / teminat konusu, rapor sayfası).
//   İpotekler Rehin · İpotek Takibi'ne (KrediYapilandirmaOrnek.TeminatKaydi: yevmiye no, malik türü asıl borçlu / 3. şahıs, alacak statüsü) yazılır.
//   Kalıcılık: App_Data/konkordato.json (KonkordatoDepo; git dışı). İlk açılışta Data/Tohum/konkordato-bilirkisi-2026-02.json (Bakırköy 2. Asliye Ticaret Mahkemesi 2024/894 E. bilirkişi raporu,
//   Mehmet Erdem, 19.02.2026; 62 sayfa taranmış PDF'ten aktarıldı) bir kez uygulanır (TohumSurumu); kayıtlar sonra ekranda düzenlenir / silinir.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication3.Data
{
    /// <summary>Konkordato dava dosyası (tek kayıt): mahkeme, dosya no, davacılar, geçici mühlet ve bilirkişi raporu tarihleri, kur bilgisi.</summary>
    public class KonkordatoDosya
    {
        public string Mahkeme { get; set; } = ""; public string DosyaNo { get; set; } = ""; public string DavaTuru { get; set; } = ""; public DateTime? DavaTarihi { get; set; } public string Davali { get; set; } = "Hasımsız";
        public List<string> Davacilar { get; set; } = new List<string>(); public string Vekil { get; set; } = "";
        public DateTime? AraKararTarihi { get; set; } public string AraKararNot { get; set; } = "";
        public DateTime? GeciciMuhletTarihi { get; set; } public int GeciciMuhletAy { get; set; } = 3; public DateTime? RaporTarihi { get; set; } public string Bilirkisi { get; set; } = ""; public string BilirkisiUnvan { get; set; } = "";
        /// <summary>Döviz alacaklarının TL karşılığı için kur (Yargıtay uygulaması: konkordato kayıt davet ilanının 15. günü TCMB kuru).</summary>
        public DateTime? KurTarihi { get; set; } public decimal UsdKur { get; set; } public decimal EurKur { get; set; }
        public string Not { get; set; } = "";
    }
    /// <summary>Rapor tanzim tarihi faiz hesabı satırı: tutar = matrah × gün × oran / 36000 (akdi / temerrüt), BSMV = matrah × oran / 100, tazmin bedeli / diğer = girilen tutar.</summary>
    public class KonkordatoFaiz { public string Tur { get; set; } = "Akdi Faiz"; public string Aciklama { get; set; } = ""; public DateTime? Baslangic { get; set; } public DateTime? Bitis { get; set; } public int Gun { get; set; } public decimal Oran { get; set; } public decimal Matrah { get; set; } public decimal Tutar { get; set; } }
    public class KonkordatoKefil { public string Ad { get; set; } = ""; public decimal Limit { get; set; } public bool Davaci { get; set; } }
    /// <summary>Genel kredi sözleşmesi / finansal kiralama sözleşmesi: tarih, tutar (limit), müteselsil kefiller ve kefalet limitleri.</summary>
    public class KonkordatoSozlesme
    {
        public int Id { get; set; } public int AlacakId { get; set; } public string AlacakAnahtar { get; set; } public string Banka { get; set; } = ""; public string Borclu { get; set; } = "";
        public string Tur { get; set; } = "Genel Kredi Sözleşmesi"; public string SozlesmeNo { get; set; } = ""; public DateTime? Tarih { get; set; } public decimal Tutar { get; set; } public string PB { get; set; } = "TRY";
        public List<KonkordatoKefil> Kefiller { get; set; } = new List<KonkordatoKefil>(); public string Not { get; set; } = ""; public string Kaynak { get; set; } = "";
    }
    /// <summary>Geçici mühlet tarihi (saat 24:00) itibariyle bankanın kredi / hesap satırı (ekstre): bakiye TL (döviz satırında kur ile).</summary>
    public class KonkordatoHesap
    {
        public int Id { get; set; } public int AlacakId { get; set; } public string AlacakAnahtar { get; set; } public string Banka { get; set; } = ""; public string Borclu { get; set; } = ""; public int Sira { get; set; }
        public string KrediNo { get; set; } = ""; public string Tur { get; set; } = ""; public string Nitelik { get; set; } = "Nakdi";
        public decimal FaizOrani { get; set; } public string FaizDonemi { get; set; } = "Yıllık"; public decimal GecikmeOrani { get; set; }
        /// <summary>11 Kas 2026: dosyadaki toplam anapara (kredi tutarı); Anapara = o dosyadan KALAN anapara, ödenen = KrediTutari − Anapara (0 = bilinmiyor).</summary>
        public decimal KrediTutari { get; set; }
        public decimal Anapara { get; set; } public string PB { get; set; } = "TRY"; public decimal Kur { get; set; } public DateTime? FaizBaslangic { get; set; }
        public decimal IslemisFaiz { get; set; } public decimal Komisyon { get; set; } public decimal Bsmv { get; set; } public decimal Kdv { get; set; } public decimal TemerrutFaizi { get; set; }
        public decimal BakiyeDoviz { get; set; } public decimal Bakiye { get; set; } public string Not { get; set; } = ""; public string Kaynak { get; set; } = "";
    }
    /// <summary>Geçici mühlet tarihinden sonra bankaya ulaşan tahsilat (çek, havale, şube, mevduat rehni, teminat çeki, haricen); Mahsup = alacağa mahsup edildi.</summary>
    public class KonkordatoTahsilat
    {
        public int Id { get; set; } public int AlacakId { get; set; } public string AlacakAnahtar { get; set; } public string Banka { get; set; } = ""; public string Borclu { get; set; } = "";
        public DateTime? Tarih { get; set; } public DateTime? Vade { get; set; } public DateTime? Valor { get; set; } public string Kanal { get; set; } = "Çek"; public string CekNo { get; set; } = ""; public string CekBankasi { get; set; } = "";
        public string Odeyen { get; set; } = ""; public string Firma { get; set; } = ""; public decimal Tutar { get; set; } public string PB { get; set; } = "TRY"; public bool Mahsup { get; set; } = true; public string Aciklama { get; set; } = ""; public string Kaynak { get; set; } = "";
    }
    /// <summary>Teminat mektubu: veriliş / geçerlilik, tutar, tazmin tarihi ve bedeli (TL; dövizde kur ile), durum.</summary>
    public class KonkordatoMektup
    {
        public int Id { get; set; } public int AlacakId { get; set; } public string AlacakAnahtar { get; set; } public string Banka { get; set; } = ""; public string Borclu { get; set; } = "";
        public string MektupNo { get; set; } = ""; public string KkbNo { get; set; } = ""; public DateTime? Verilis { get; set; } public DateTime? Gecerlilik { get; set; } public decimal Tutar { get; set; } public string PB { get; set; } = "TRY"; public decimal Kur { get; set; }
        public DateTime? TazminTarihi { get; set; } public decimal TazminBedeli { get; set; } public string Durum { get; set; } = "Tazmin Edildi"; public string Not { get; set; } = ""; public string Kaynak { get; set; } = "";
        /// <summary>Muhatap = mektubun verildiği kurum (lehine verilen şirketin alacaklısı); Konu = hangi borç / iş için verildiği (istinaden); Sayfa = bilirkişi raporu sayfası. Rapor muhatabı ve konuyu vermez → ekranda "Raporda Yok", mektup suretinden girilir (10 Kas 2026).</summary>
        public string Muhatap { get; set; } = ""; public string Konu { get; set; } = ""; public string Sayfa { get; set; } = "";
    }
    /// <summary>Banka × borçlu alacak kaydı (bilirkişi raporundaki her "… Yönünden / … Bakımından" bölümü).</summary>
    public class KonkordatoAlacak
    {
        public int Id { get; set; } public string Anahtar { get; set; } public int Sira { get; set; } public string Banka { get; set; } = ""; public string Borclu { get; set; } = "";
        /// <summary>Asaleten (borçlu davacı şirket) | Kefaleten (dava dışı asıl borçlunun borcuna davacı şirketler kefil).</summary>
        public string Nitelik { get; set; } = "Asaleten"; public string Kefiller { get; set; } = "";
        public string Durum { get; set; } = "Hesaplandı"; public string AlacakStatusu { get; set; } = "Adi Alacak"; public decimal RehinMiktari { get; set; } public string RehinNot { get; set; } = "";
        // ---- kat ihtarı · tebliğ · temerrüt
        public string KatNoter { get; set; } = ""; public DateTime? KatIhtarTarihi { get; set; } public string KatYevmiye { get; set; } = ""; public DateTime? KatTarihi { get; set; }
        public decimal KatNakdi { get; set; } public string KatPb { get; set; } = "TRY"; public decimal KatGayrinakdiTl { get; set; } public decimal KatGayrinakdiUsd { get; set; } public int MehilGun { get; set; } = 1;
        public string TebligYontemi { get; set; } = ""; public DateTime? TebligTarihi { get; set; } public DateTime? YasalTebligTarihi { get; set; } public string TebligAlan { get; set; } = ""; public DateTime? TemerrutTarihi { get; set; } public string KatNot { get; set; } = "";
        // ---- geçici mühlet tarihi itibariyle alacak
        /// <summary>Ekstreye göre nakdi alacak (işlemiş faiz ve ferileri dahil); MuhletItibar &gt; 0 ise hesapta ona (kat ihtarındaki tutara) itibar edilir.</summary>
        public decimal MuhletNakdi { get; set; } public decimal MuhletItibar { get; set; } public decimal MuhletGayrinakdi { get; set; }
        /// <summary>Mühlet sonrası tazmin edilen teminat mektubu bedelleri (bakiyeye eklenir); TahsilatToplam 0 ise mahsup işaretli tahsilat kayıtlarının toplamı alınır.</summary>
        public decimal TazminToplam { get; set; } public decimal TahsilatToplam { get; set; }
        // ---- rapor tanzim tarihi itibariyle
        public decimal AkdiFaiz { get; set; } public decimal TemerrutFaizi { get; set; } public string TemerrutMaddesi { get; set; } = ""; public decimal BsmvOrani { get; set; } = 5m;
        public List<KonkordatoFaiz> FaizDonemleri { get; set; } = new List<KonkordatoFaiz>();
        /// <summary>Rapordaki rapor tanzim tarihi toplamı (0 ise bakiye + faiz dönemleri toplamı hesaplanır).</summary>
        public decimal RaporNakdi { get; set; } public decimal RaporGayrinakdi { get; set; }
        // ---- gayrinakdi çek taahhüt bedeli
        public int CekYaprak { get; set; } public decimal CekYaprakBedeli { get; set; } = 9270m; public decimal CekTaahhut { get; set; } public string CekEkNo { get; set; } = "";
        public string HukukiDegerlendirme { get; set; } = ""; public string Not { get; set; } = ""; public string Kaynak { get; set; } = "";
        /// <summary>Ödeme durumu (11 Kas 2026, kullanıcı: "Emlak Katılım Avrupa Paper ve Uras Kimya yeşil yap, Ödendi sütunu oluştur"): Açık | Ödendi (banka borcun kapandığını bildirdi) | Yapılandırıldı (protokol imzalandı; KrediNo = bağlı Kredi Raporu kaydı).
        /// GuncelBorc / GuncelTarih: bankanın güncel bildirimi (borç dökümü); bilirkişi raporunda olmayan dava dışı borçlular (Alv Kimya, Daf Kimya) için Durum "Rapor Dışı" ile birlikte kullanılır.</summary>
        public string OdemeDurumu { get; set; } = "Açık"; public DateTime? OdemeTarihi { get; set; } public string OdemeNot { get; set; } = ""; public string KrediNo { get; set; } = ""; public decimal GuncelBorc { get; set; } public DateTime? GuncelTarih { get; set; }
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = ""; public DateTime Guncelleme { get; set; } public string Guncelleyen { get; set; } = "";
    }
    /// <summary>Banka × şirket veri kontrol listesi (11 Kas 2026, "tüm bilgileri yüklediysem OK olarak işaretleyebileyim, sıra ile gidiyorum"): kullanıcı bir banka × şirket çiftinin bilgilerini tamamlayınca OK, eksik varsa Eksik işaretler; sayılar ekranda kayıtlardan hesaplanır, burada yalnız işaret + not tutulur.</summary>
    public class KonkordatoKontrol { public string Banka { get; set; } = ""; public string Sirket { get; set; } = ""; public string Durum { get; set; } = "Bekliyor"; public string Not { get; set; } = ""; public DateTime? Tarih { get; set; } public string Kullanici { get; set; } = ""; }
    public class KonkordatoDepo
    {
        public KonkordatoDosya Dosya { get; set; } = new KonkordatoDosya(); public int TohumSurumu { get; set; }
        public List<KonkordatoKontrol> Kontroller { get; set; } = new List<KonkordatoKontrol>();
        public List<KonkordatoAlacak> Alacaklar { get; set; } = new List<KonkordatoAlacak>(); public List<KonkordatoSozlesme> Sozlesmeler { get; set; } = new List<KonkordatoSozlesme>(); public List<KonkordatoHesap> Hesaplar { get; set; } = new List<KonkordatoHesap>();
        public List<KonkordatoTahsilat> Tahsilatlar { get; set; } = new List<KonkordatoTahsilat>(); public List<KonkordatoMektup> Mektuplar { get; set; } = new List<KonkordatoMektup>();
    }
    /// <summary>Tohum dosyası: depo alanları + Rehin · İpotek Takibi'ne yazılacak ipotekler + Teminat Çekleri'ne yazılacak ek çekler (çek no'lu çek tahsilatlarından türetilenlere ek: tutarı / numarası toplu verilenler, haricen ödenen çekler).</summary>
    public class KonkordatoTohum : KonkordatoDepo { public List<TeminatKaydi> Ipotekler { get; set; } = new List<TeminatKaydi>(); public List<TeminatCeki> Cekler { get; set; } = new List<TeminatCeki>(); public string Kaynak { get; set; } = ""; }
    public class KonkordatoIdIstek { public int Id { get; set; } }

    public static class KonkordatoOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static KonkordatoDepo _depo = new KonkordatoDepo(); static bool _yuklendi;
        const int TohumSurum = 4;   // 1: alacak / sözleşme / hesap / tahsilat / mektup / ipotek · 2: teminat çekleri · 3: çeklerde asıl borçlu / tahsil edilen / mahsup / rapor sayfası · 4: mektuplarda muhatap / istinaden / rapor sayfası (boş alanlar yerinde tamamlanır) (10 Kas 2026)
        /// <summary>"Rapor Dışı" (11 Kas 2026): bilirkişi raporunda bulunmayan, bankanın güncel bildirimiyle listeye eklenen alacak (dava dışı borçlu: Alv Kimya, Daf Kimya).</summary>
        public static readonly string[] Durumlar = { "Hesaplandı", "Belge Temin Edilemedi", "Ödenmiş", "Fazla Ödenmiş", "Değerlendirme Dışı", "Rapor Dışı" };
        public static readonly string[] OdemeDurumlari = { "Açık", "Ödendi", "Yapılandırıldı" };
        public static readonly string[] Statuler = { "Adi Alacak", "İmtiyazlı / Rüçhanlı Alacak", "Belirsiz" };
        public static readonly string[] Nitelikler = { "Asaleten", "Kefaleten" };
        public static readonly string[] SozlesmeTurleri = { "Genel Kredi Sözleşmesi", "Finansal Kiralama Sözleşmesi", "Kredi Sözleşmesi", "Diğer" };
        public static readonly string[] HesapNitelikleri = { "Nakdi", "Gayrinakdi", "Tenzil", "Masraf" };
        public static readonly string[] FaizDonemleri = { "Yıllık", "Aylık" };
        public static readonly string[] TahsilatKanallari = { "Çek", "Havale / EFT", "Şube", "Mevduat Rehni", "Teminat Çeki", "Haricen", "Diğer" };
        public static readonly string[] MektupDurumlari = { "Tazmin Edildi", "Devam Ediyor", "İade Edildi", "Değerlendirme Dışı" };
        public static readonly string[] KontrolDurumlari = { "Bekliyor", "Eksik", "OK" };   // banka × şirket veri kontrol listesi (11 Kas 2026)
        public static readonly string[] FaizTurleri = { "Akdi Faiz", "Temerrüt Faizi", "BSMV", "Tazmin Bedeli", "Diğer" };
        public static readonly string[] TebligYontemleri = { "E-Tebligat", "Noter", "Elden", "Diğer" };
        public static readonly string[] ParaBirimleri = { "TRY", "USD", "EUR", "GBP" };
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        public static string Dosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data", "konkordato.json");
        public static string TohumDosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "Data", "Tohum", "konkordato-bilirkisi-2026-02.json");
        static string D(DateTime? t) => t?.ToString("yyyy-MM-dd");
        static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        static string Temiz(string s) => (s ?? "").Trim();
        static string Sec(string v, string[] liste, string varsayilan) => liste.Contains(v ?? "") ? v : varsayilan;

        // ---------- kalıcılık ----------
        /// <summary>Açılışta (OrnekIsinma, Kredi Yapılandırma'dan SONRA): dosya okunur; tohum sürümü eskiyse bilirkişi raporu tohumu uygulanır (ipotekler Rehin · İpotek Takibi'ne).</summary>
        public static void Yukle()
        {
            lock (Kilit)
            {
                if (_yuklendi) return; _yuklendi = true;
                var dosya = Dosya;
                if (dosya != null && File.Exists(dosya))
                {
                    try { _depo = JsonSerializer.Deserialize<KonkordatoDepo>(File.ReadAllText(dosya), Secenek) ?? new KonkordatoDepo(); }
                    catch (Exception ex) { Console.WriteLine("[Konkordato] dosya okunamadı: " + ex.Message); _depo = new KonkordatoDepo(); }
                }
                Normallestir();
                if (_depo.TohumSurumu < TohumSurum) TohumUygula();
                Console.WriteLine($"[Konkordato] {_depo.Alacaklar.Count} alacak kaydı, {_depo.Sozlesmeler.Count} sözleşme, {_depo.Hesaplar.Count} hesap satırı, {_depo.Tahsilatlar.Count} tahsilat, {_depo.Mektuplar.Count} teminat mektubu" + (dosya != null ? $" · {dosya}" : " (kalıcılık kapalı)"));
            }
        }
        static void Normallestir()
        {
            _depo.Dosya ??= new KonkordatoDosya(); _depo.Dosya.Davacilar ??= new List<string>();
            _depo.Alacaklar ??= new List<KonkordatoAlacak>(); _depo.Sozlesmeler ??= new List<KonkordatoSozlesme>(); _depo.Hesaplar ??= new List<KonkordatoHesap>(); _depo.Tahsilatlar ??= new List<KonkordatoTahsilat>(); _depo.Mektuplar ??= new List<KonkordatoMektup>();
            foreach (var a in _depo.Alacaklar) a.FaizDonemleri ??= new List<KonkordatoFaiz>();
            foreach (var s in _depo.Sozlesmeler) s.Kefiller ??= new List<KonkordatoKefil>();
        }
        static void Yaz()
        {
            var dosya = Dosya; if (dosya == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(dosya)); File.WriteAllText(dosya, JsonSerializer.Serialize(_depo, Secenek)); }
            catch (Exception ex) { Console.WriteLine("[Konkordato] dosya yazılamadı: " + ex.Message); }
        }
        static int YeniId<T>(List<T> l, Func<T, int> id) => l.Select(id).DefaultIfEmpty(0).Max() + 1;
        /// <summary>Bilirkişi raporu tohumu: dosya bilgisi (boşsa), alacak kayıtları ve bağlı kayıtlar (anahtarla eşlenir), ipotekler Rehin · İpotek Takibi'ne (aynı kaynaklı kayıt varsa atlanır).</summary>
        static void TohumUygula()
        {
            var yol = TohumDosya; if (yol == null || !File.Exists(yol)) { Console.WriteLine("[Konkordato] tohum dosyası yok: " + yol); return; }
            try
            {
                var t = JsonSerializer.Deserialize<KonkordatoTohum>(File.ReadAllText(yol), Secenek); if (t == null) return;
                string kaynak = string.IsNullOrWhiteSpace(t.Kaynak) ? "Bilirkişi Raporu" : t.Kaynak;
                if (string.IsNullOrWhiteSpace(_depo.Dosya.DosyaNo) && t.Dosya != null) _depo.Dosya = t.Dosya;
                var eslem = new Dictionary<string, int>(); var yeni = new HashSet<string>(); int ekA = 0, ekS = 0, ekH = 0, ekT = 0, ekM = 0, ekI = 0, ekC = 0;
                int idA = YeniId(_depo.Alacaklar, a => a.Id), idS = YeniId(_depo.Sozlesmeler, s => s.Id), idH = YeniId(_depo.Hesaplar, h => h.Id), idT = YeniId(_depo.Tahsilatlar, x => x.Id), idM = YeniId(_depo.Mektuplar, m => m.Id);
                foreach (var a in t.Alacaklar ?? new List<KonkordatoAlacak>())
                {
                    if (string.IsNullOrWhiteSpace(a.Anahtar)) continue;
                    var mevcut = _depo.Alacaklar.FirstOrDefault(x => x.Anahtar == a.Anahtar); if (mevcut != null) { eslem[a.Anahtar] = mevcut.Id; continue; }   // daha önce uygulanmış (sürüm yükseltme): bağlı kayıtlar yeniden eklenmez
                    a.Id = idA++; a.Kaynak = kaynak; a.Olusturma = DateTime.Today; a.Olusturan = "Bilirkişi Raporu"; a.FaizDonemleri ??= new List<KonkordatoFaiz>(); a.Banka = Temiz(a.Banka); a.Borclu = Temiz(a.Borclu); _depo.Alacaklar.Add(a); eslem[a.Anahtar] = a.Id; yeni.Add(a.Anahtar); ekA++;
                }
                int Bag(string anahtar) => anahtar != null && eslem.TryGetValue(anahtar, out var id) ? id : 0;
                bool Yeni(string anahtar) => anahtar == null ? _depo.TohumSurumu < 1 : yeni.Contains(anahtar);   // bağlı kayıtlar yalnız bu uygulamada eklenen alacaklar için
                foreach (var s in t.Sozlesmeler ?? new List<KonkordatoSozlesme>()) { if (!Yeni(s.AlacakAnahtar)) continue; s.Id = idS++; s.AlacakId = Bag(s.AlacakAnahtar); s.Kaynak = kaynak; s.Kefiller ??= new List<KonkordatoKefil>(); _depo.Sozlesmeler.Add(s); ekS++; }
                foreach (var h in t.Hesaplar ?? new List<KonkordatoHesap>()) { if (!Yeni(h.AlacakAnahtar)) continue; h.Id = idH++; h.AlacakId = Bag(h.AlacakAnahtar); h.Kaynak = kaynak; if (h.BakiyeDoviz == 0) h.BakiyeDoviz = h.Bakiye; if (h.Kur == 0) h.Kur = 1; _depo.Hesaplar.Add(h); ekH++; }
                foreach (var x in t.Tahsilatlar ?? new List<KonkordatoTahsilat>()) { if (!Yeni(x.AlacakAnahtar)) continue; x.Id = idT++; x.AlacakId = Bag(x.AlacakAnahtar); x.Kaynak = kaynak; _depo.Tahsilatlar.Add(x); ekT++; }
                foreach (var m in t.Mektuplar ?? new List<KonkordatoMektup>()) { if (!Yeni(m.AlacakAnahtar)) continue; m.Id = idM++; m.AlacakId = Bag(m.AlacakAnahtar); m.Kaynak = kaynak; _depo.Mektuplar.Add(m); ekM++; }
                // sürüm 4 (10 Kas 2026): daha önce uygulanmış bilirkişi mektuplarının BOŞ alanları tohumdan tamamlanır (muhatap, istinaden, rapor sayfası, KKB no, not) — kullanıcının girdiği değer ezilmez; eşleşme alacak + mektup no + tutar + tazmin tarihi
                int gM = 0;
                foreach (var m in t.Mektuplar ?? new List<KonkordatoMektup>())
                {
                    if (Yeni(m.AlacakAnahtar)) continue; int aid = Bag(m.AlacakAnahtar); if (aid == 0) continue;
                    var x = _depo.Mektuplar.FirstOrDefault(y => y.AlacakId == aid && Temiz(y.MektupNo) == Temiz(m.MektupNo) && y.Tutar == m.Tutar && y.TazminTarihi == m.TazminTarihi); if (x == null) continue;
                    bool degisti = false;
                    if (string.IsNullOrWhiteSpace(x.Muhatap) && !string.IsNullOrWhiteSpace(m.Muhatap)) { x.Muhatap = Temiz(m.Muhatap); degisti = true; }
                    if (string.IsNullOrWhiteSpace(x.Konu) && !string.IsNullOrWhiteSpace(m.Konu)) { x.Konu = Temiz(m.Konu); degisti = true; }
                    if (string.IsNullOrWhiteSpace(x.Sayfa) && !string.IsNullOrWhiteSpace(m.Sayfa)) { x.Sayfa = Temiz(m.Sayfa); degisti = true; }
                    if (string.IsNullOrWhiteSpace(x.KkbNo) && !string.IsNullOrWhiteSpace(m.KkbNo)) { x.KkbNo = Temiz(m.KkbNo); degisti = true; }
                    if (string.IsNullOrWhiteSpace(x.Not) && !string.IsNullOrWhiteSpace(m.Not)) { x.Not = Temiz(m.Not); degisti = true; }
                    if (string.IsNullOrWhiteSpace(x.Kaynak)) { x.Kaynak = kaynak; degisti = true; }
                    if (degisti) gM++;
                }
                // ipotekler → Rehin · İpotek Takibi (aynı kaynaklı kayıt varsa atlanır)
                var ipotekler = (t.Ipotekler ?? new List<TeminatKaydi>()).Select(i => { i.Kaynak = kaynak; i.Olusturan = "Bilirkişi Raporu"; return i; }).ToList();
                if (ipotekler.Count > 0) ekI = KrediYapilandirmaOrnek.TeminatTohumEkle(ipotekler, eslem);
                // teminat çekleri → Teminat Çekleri (sürüm 2): çek no'lu çek tahsilatlarından türetilir (bankaya teminat verilen müşteri çeki, mühlet sonrası tahsil edildi) + tohumdaki ek çekler
                var anahtarlar = _depo.Alacaklar.Where(a => !string.IsNullOrWhiteSpace(a.Anahtar)).ToDictionary(a => a.Id, a => a.Anahtar);
                var alacakBorclu = _depo.Alacaklar.ToDictionary(a => a.Id, a => a.Borclu ?? "");
                bool SirketMi(string ad) => !string.IsNullOrWhiteSpace(ad) && CekAkibetOrnek.Sirketler.Any(s => s.Ad == ad);
                string Sayfa(string banka) => banka == "Anadolubank" ? "rapor s. 33–39" : banka == "Kuveyt Türk" ? "rapor s. 54–55" : banka == "Akbank" ? "rapor s. 5" : "";   // çek listelerinin bilirkişi raporundaki sayfaları
                var cekler = _depo.Tahsilatlar.Where(x => x.Kaynak == kaynak && x.Kanal == "Çek" && !string.IsNullOrWhiteSpace(x.CekNo)).Select(x => new TeminatCeki
                {
                    Sirket = SirketMi(x.Odeyen) ? x.Odeyen : x.Borclu, Banka = x.Banka, AlacakId = x.AlacakId, AlacakAnahtar = anahtarlar.TryGetValue(x.AlacakId, out var an) ? an : null, KrediRef = "Konkordato alacak kaydı · mühlet sonrası çek tahsilatı",
                    AsilBorclu = x.Banka == "Kuveyt Türk" ? "Grup firmaları (dağıtım tablosu)" : (alacakBorclu.TryGetValue(x.AlacakId, out var ab) && ab != "" ? ab : x.Borclu),
                    Belge = "Çek", CekNo = x.CekNo, Kesideci = SirketMi(x.Odeyen) ? "" : (x.Odeyen ?? ""), CekBankasi = x.CekBankasi, Vade = x.Vade ?? x.Tarih, Tutar = x.Tutar, PB = string.IsNullOrEmpty(x.PB) ? "TRY" : x.PB,
                    TahsilEdilen = x.Tutar, Mahsup = x.Mahsup, Durum = "Tahsil Edildi", IadeTarihi = x.Valor ?? x.Tarih,
                    Not = "Bilirkişi raporu (" + Sayfa(x.Banka) + "): bankaya teminat verilen müşteri çeki, geçici mühlet sonrası banka tarafından tahsil edildi" + (x.Mahsup ? "; tamamı alacağa mahsup edildi" : "; tahsilat firma payı dağıtım tablosuyla grup borçlarına dağıtıldı") + (SirketMi(x.Odeyen) ? " · çeki bankaya veren: " + x.Odeyen : "") + " · keşideci raporda belirtilmemiş" + (x.Valor != null ? " · valör " + x.Valor.Value.ToString("dd.MM.yyyy") : "") + (string.IsNullOrWhiteSpace(x.Aciklama) ? "" : " · " + x.Aciklama),
                    Kaynak = kaynak, Olusturan = "Bilirkişi Raporu"
                }).ToList();
                cekler.AddRange((t.Cekler ?? new List<TeminatCeki>()).Select(c => { c.Kaynak = kaynak; c.Olusturan = "Bilirkişi Raporu"; if (c.AlacakId == 0) c.AlacakId = Bag(c.AlacakAnahtar); if (c.TahsilEdilen == 0 && c.Durum != "Teminatta" && c.Tutar > 0) c.TahsilEdilen = c.Tutar; if (string.IsNullOrEmpty(c.AsilBorclu) && alacakBorclu.TryGetValue(c.AlacakId, out var ab2)) c.AsilBorclu = ab2; return c; }));
                if (cekler.Count > 0) ekC = KrediYapilandirmaOrnek.CekTohumEkle(cekler, eslem);
                _depo.TohumSurumu = TohumSurum; Yaz();
                Console.WriteLine($"[Konkordato] bilirkişi raporu tohumu uygulandı (sürüm {TohumSurum}): {ekA} alacak kaydı, {ekS} sözleşme, {ekH} hesap satırı, {ekT} tahsilat, {ekM} teminat mektubu ({gM} mektup tamamlandı), {ekI} ipotek (Rehin · İpotek Takibi), {ekC} teminat çeki (Teminat Çekleri) · {kaynak}");
            }
            catch (Exception ex) { Console.WriteLine("[Konkordato] tohum uygulanamadı: " + ex.Message); }
        }

        // ---------- hesap yardımcıları ----------
        /// <summary>Faiz satırının formül değeri: akdi / temerrüt = matrah × gün × oran / 36000; BSMV = matrah × oran / 100; diğerleri girilen tutar.</summary>
        public static decimal FaizHesapla(KonkordatoFaiz f)
        {
            if (f == null) return 0;
            if (f.Tur == "Akdi Faiz" || f.Tur == "Temerrüt Faizi") return R2(f.Matrah * f.Gun * f.Oran / 36000m);
            if (f.Tur == "BSMV") return R2(f.Matrah * f.Oran / 100m);
            return f.Tutar;
        }
        static decimal TahsilatKayit(KonkordatoAlacak a) => _depo.Tahsilatlar.Where(t => t.AlacakId == a.Id && t.Mahsup).Sum(t => t.Tutar);
        static decimal TahsilatEtkin(KonkordatoAlacak a) => a.TahsilatToplam > 0 ? a.TahsilatToplam : TahsilatKayit(a);
        static decimal Itibar(KonkordatoAlacak a) => a.MuhletItibar > 0 ? a.MuhletItibar : a.MuhletNakdi;
        /// <summary>Geçici mühlet tarihi itibariyle bakiye alacak = itibar edilen nakdi alacak + tazmin bedelleri − mühlet sonrası tahsilat (tahsilat alacağı aşarsa 0; fazlası FazlaOdeme).</summary>
        public static decimal Bakiye(KonkordatoAlacak a) => Math.Max(0, R2(Itibar(a) + a.TazminToplam - TahsilatEtkin(a)));
        public static decimal FazlaOdeme(KonkordatoAlacak a) => Math.Max(0, R2(TahsilatEtkin(a) - Itibar(a) - a.TazminToplam));
        static decimal RaporHesaplanan(KonkordatoAlacak a) => R2(Bakiye(a) + a.FaizDonemleri.Sum(f => f.Tutar));
        static decimal RaporEtkin(KonkordatoAlacak a) => a.RaporNakdi > 0 ? a.RaporNakdi : (a.Durum == "Hesaplandı" ? RaporHesaplanan(a) : 0);
        static decimal CekTaahhutEtkin(KonkordatoAlacak a) => a.CekTaahhut > 0 ? a.CekTaahhut : R2(a.CekYaprak * a.CekYaprakBedeli);

        // ---------- kayıt ----------
        public static string DosyaKaydet(KonkordatoDosya d, string kullanici)
        {
            if (d == null) return "Veri yok."; Yukle();
            lock (Kilit)
            {
                d.Mahkeme = Temiz(d.Mahkeme); d.DosyaNo = Temiz(d.DosyaNo); d.DavaTuru = Temiz(d.DavaTuru); d.Davali = Temiz(d.Davali); d.Vekil = Temiz(d.Vekil); d.AraKararNot = Temiz(d.AraKararNot); d.Bilirkisi = Temiz(d.Bilirkisi); d.BilirkisiUnvan = Temiz(d.BilirkisiUnvan); d.Not = Temiz(d.Not);
                d.Davacilar = (d.Davacilar ?? new List<string>()).Select(Temiz).Where(x => x != "").ToList(); if (d.GeciciMuhletAy < 0) d.GeciciMuhletAy = 0;
                _depo.Dosya = d; Yaz();
            }
            return null;
        }
        /// <summary>Banka × şirket kontrol işareti (11 Kas 2026): aynı çift varsa güncellenir, yoksa eklenir; tarih ve kullanıcı sunucuda yazılır.</summary>
        public static string KontrolKaydet(KonkordatoKontrol d, string kullanici, out KonkordatoKontrol kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string banka = Temiz(d.Banka), sirket = Temiz(d.Sirket); if (banka == "" || sirket == "") return "Banka ve şirket gerekli.";
            lock (Kilit)
            {
                if (_depo.Kontroller == null) _depo.Kontroller = new List<KonkordatoKontrol>();
                var x = _depo.Kontroller.FirstOrDefault(k => string.Equals(k.Banka, banka, StringComparison.OrdinalIgnoreCase) && string.Equals(k.Sirket, sirket, StringComparison.OrdinalIgnoreCase));
                if (x == null) { x = new KonkordatoKontrol { Banka = banka, Sirket = sirket }; _depo.Kontroller.Add(x); }
                x.Durum = Sec(d.Durum, KontrolDurumlari, "Bekliyor"); x.Not = Temiz(d.Not); x.Tarih = DateTime.Now; x.Kullanici = kullanici ?? "";
                Yaz(); kayit = x;
            }
            return null;
        }
        public static string AlacakKaydet(KonkordatoAlacak d, string kullanici, out KonkordatoAlacak kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string banka = Temiz(d.Banka); if (banka == "") return "Alacaklı banka zorunludur.";
            if (d.MuhletNakdi < 0 || d.MuhletItibar < 0 || d.MuhletGayrinakdi < 0 || d.TazminToplam < 0 || d.TahsilatToplam < 0 || d.RaporNakdi < 0 || d.RaporGayrinakdi < 0 || d.RehinMiktari < 0 || d.KatNakdi < 0 || d.CekTaahhut < 0) return "Tutarlar negatif olamaz.";
            if (d.AkdiFaiz < 0 || d.AkdiFaiz > 500 || d.TemerrutFaizi < 0 || d.TemerrutFaizi > 500 || d.BsmvOrani < 0 || d.BsmvOrani > 100) return "Faiz / BSMV oranı geçersiz.";
            lock (Kilit)
            {
                var a = d.Id > 0 ? _depo.Alacaklar.FirstOrDefault(x => x.Id == d.Id) : null; if (d.Id > 0 && a == null) return "Kayıt bulunamadı.";
                bool yeni = a == null; if (yeni) { a = new KonkordatoAlacak { Id = YeniId(_depo.Alacaklar, x => x.Id), Olusturma = DateTime.Today, Olusturan = kullanici ?? "", Kaynak = "Ekran" }; _depo.Alacaklar.Add(a); }
                a.Sira = Math.Max(0, d.Sira); a.Banka = banka; a.Borclu = Temiz(d.Borclu); a.Nitelik = Sec(d.Nitelik, Nitelikler, "Asaleten"); a.Kefiller = Temiz(d.Kefiller);
                a.Durum = Sec(d.Durum, Durumlar, "Hesaplandı"); a.AlacakStatusu = (d.AlacakStatusu ?? "") == "" ? "" : Sec(d.AlacakStatusu, Statuler, "Adi Alacak"); a.RehinMiktari = R2(d.RehinMiktari); a.RehinNot = Temiz(d.RehinNot);
                a.KatNoter = Temiz(d.KatNoter); a.KatIhtarTarihi = d.KatIhtarTarihi?.Date; a.KatYevmiye = Temiz(d.KatYevmiye); a.KatTarihi = d.KatTarihi?.Date; a.KatNakdi = R2(d.KatNakdi); a.KatPb = Sec(Temiz(d.KatPb).ToUpperInvariant(), ParaBirimleri, "TRY"); a.KatGayrinakdiTl = R2(d.KatGayrinakdiTl); a.KatGayrinakdiUsd = R2(d.KatGayrinakdiUsd); a.MehilGun = Math.Max(0, d.MehilGun);
                a.TebligYontemi = Temiz(d.TebligYontemi); a.TebligTarihi = d.TebligTarihi?.Date; a.YasalTebligTarihi = d.YasalTebligTarihi?.Date; a.TebligAlan = Temiz(d.TebligAlan); a.TemerrutTarihi = d.TemerrutTarihi?.Date; a.KatNot = Temiz(d.KatNot);
                a.MuhletNakdi = R2(d.MuhletNakdi); a.MuhletItibar = R2(d.MuhletItibar); a.MuhletGayrinakdi = R2(d.MuhletGayrinakdi); a.TazminToplam = R2(d.TazminToplam); a.TahsilatToplam = R2(d.TahsilatToplam);
                a.AkdiFaiz = d.AkdiFaiz; a.TemerrutFaizi = d.TemerrutFaizi; a.TemerrutMaddesi = Temiz(d.TemerrutMaddesi); a.BsmvOrani = d.BsmvOrani; a.RaporNakdi = R2(d.RaporNakdi); a.RaporGayrinakdi = R2(d.RaporGayrinakdi);
                a.FaizDonemleri = (d.FaizDonemleri ?? new List<KonkordatoFaiz>()).Where(f => f != null && (f.Tutar != 0 || f.Matrah != 0 || Temiz(f.Aciklama) != "")).Select(f => new KonkordatoFaiz { Tur = Sec(f.Tur, FaizTurleri, "Diğer"), Aciklama = Temiz(f.Aciklama), Baslangic = f.Baslangic?.Date, Bitis = f.Bitis?.Date, Gun = f.Gun > 0 ? f.Gun : (f.Baslangic != null && f.Bitis != null ? Math.Max(0, (int)(f.Bitis.Value.Date - f.Baslangic.Value.Date).TotalDays) : 0), Oran = f.Oran, Matrah = R2(f.Matrah), Tutar = R2(f.Tutar) }).ToList();
                foreach (var f in a.FaizDonemleri) if (f.Tutar == 0) f.Tutar = FaizHesapla(f);   // tutar boş bırakılırsa formülle
                a.CekYaprak = Math.Max(0, d.CekYaprak); a.CekYaprakBedeli = d.CekYaprakBedeli > 0 ? d.CekYaprakBedeli : 9270m; a.CekTaahhut = R2(d.CekTaahhut); a.CekEkNo = Temiz(d.CekEkNo);
                a.HukukiDegerlendirme = Temiz(d.HukukiDegerlendirme); a.Not = Temiz(d.Not); a.Guncelleme = DateTime.Now; a.Guncelleyen = kullanici ?? "";
                a.OdemeDurumu = Sec(d.OdemeDurumu, OdemeDurumlari, "Açık"); a.OdemeTarihi = d.OdemeTarihi?.Date; a.OdemeNot = Temiz(d.OdemeNot); a.KrediNo = Temiz(d.KrediNo); a.GuncelBorc = R2(Math.Max(0, d.GuncelBorc)); a.GuncelTarih = d.GuncelTarih?.Date;   // ödeme durumu (11 Kas 2026)
                foreach (var s in _depo.Sozlesmeler.Where(x => x.AlacakId == a.Id)) { s.Banka = a.Banka; }   // bağlı kayıtların bankası alacak kaydını izler
                Yaz(); kayit = a;
            }
            return null;
        }
        public static string AlacakSil(int id, out string ozet)
        {
            ozet = ""; Yukle();
            lock (Kilit)
            {
                var a = _depo.Alacaklar.FirstOrDefault(x => x.Id == id); if (a == null) return "Kayıt bulunamadı.";
                int s = _depo.Sozlesmeler.RemoveAll(x => x.AlacakId == id), h = _depo.Hesaplar.RemoveAll(x => x.AlacakId == id), t = _depo.Tahsilatlar.RemoveAll(x => x.AlacakId == id), m = _depo.Mektuplar.RemoveAll(x => x.AlacakId == id);
                _depo.Alacaklar.Remove(a); Yaz(); ozet = $"{a.Banka} · {a.Borclu} alacak kaydı silindi ({s} sözleşme, {h} hesap satırı, {t} tahsilat, {m} teminat mektubu ile birlikte).";
            }
            return null;
        }
        static string BagKontrol(ref int alacakId, ref string banka, ref string borclu)
        {
            int id = alacakId; var a = id > 0 ? _depo.Alacaklar.FirstOrDefault(x => x.Id == id) : null; if (id > 0 && a == null) alacakId = 0;
            if (a != null) { if (banka == "") banka = a.Banka; if (borclu == "") borclu = a.Borclu; }
            return banka == "" ? "Banka zorunludur (ya da alacak kaydı seçin)." : null;
        }
        public static string SozlesmeKaydet(KonkordatoSozlesme d, string kullanici, out KonkordatoSozlesme kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            if (d.Tutar < 0) return "Sözleşme tutarı negatif olamaz.";
            lock (Kilit)
            {
                int alacakId = d.AlacakId; string banka = Temiz(d.Banka), borclu = Temiz(d.Borclu); var h = BagKontrol(ref alacakId, ref banka, ref borclu); if (h != null) return h;
                var s = d.Id > 0 ? _depo.Sozlesmeler.FirstOrDefault(x => x.Id == d.Id) : null; if (d.Id > 0 && s == null) return "Kayıt bulunamadı.";
                if (s == null) { s = new KonkordatoSozlesme { Id = YeniId(_depo.Sozlesmeler, x => x.Id), Kaynak = "Ekran" }; _depo.Sozlesmeler.Add(s); }
                s.AlacakId = alacakId; s.Banka = banka; s.Borclu = borclu; s.Tur = Sec(d.Tur, SozlesmeTurleri, "Diğer"); s.SozlesmeNo = Temiz(d.SozlesmeNo); s.Tarih = d.Tarih?.Date; s.Tutar = R2(d.Tutar); s.PB = Sec(Temiz(d.PB).ToUpperInvariant(), ParaBirimleri, "TRY");
                s.Kefiller = (d.Kefiller ?? new List<KonkordatoKefil>()).Where(k => k != null && Temiz(k.Ad) != "").Select(k => new KonkordatoKefil { Ad = Temiz(k.Ad), Limit = R2(Math.Max(0, k.Limit)), Davaci = k.Davaci }).ToList(); s.Not = Temiz(d.Not);
                Yaz(); kayit = s;
            }
            return null;
        }
        public static string SozlesmeSil(int id) { Yukle(); lock (Kilit) { var s = _depo.Sozlesmeler.FirstOrDefault(x => x.Id == id); if (s == null) return "Kayıt bulunamadı."; _depo.Sozlesmeler.Remove(s); Yaz(); } return null; }
        public static string HesapKaydet(KonkordatoHesap d, string kullanici, out KonkordatoHesap kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            lock (Kilit)
            {
                int alacakId = d.AlacakId; string banka = Temiz(d.Banka), borclu = Temiz(d.Borclu); var h = BagKontrol(ref alacakId, ref banka, ref borclu); if (h != null) return h;
                if (Temiz(d.KrediNo) == "" && Temiz(d.Tur) == "") return "Kredi / hesap no ya da kredi türü girilmelidir.";
                var x = d.Id > 0 ? _depo.Hesaplar.FirstOrDefault(y => y.Id == d.Id) : null; if (d.Id > 0 && x == null) return "Kayıt bulunamadı.";
                if (x == null) { x = new KonkordatoHesap { Id = YeniId(_depo.Hesaplar, y => y.Id), Kaynak = "Ekran" }; _depo.Hesaplar.Add(x); }
                x.AlacakId = alacakId; x.Banka = banka; x.Borclu = borclu; x.Sira = Math.Max(0, d.Sira); x.KrediNo = Temiz(d.KrediNo); x.Tur = Temiz(d.Tur); x.Nitelik = Sec(d.Nitelik, HesapNitelikleri, "Nakdi");
                x.FaizOrani = d.FaizOrani; x.FaizDonemi = Sec(d.FaizDonemi, FaizDonemleri, "Yıllık"); x.GecikmeOrani = d.GecikmeOrani; x.KrediTutari = R2(Math.Max(0, d.KrediTutari)); x.Anapara = R2(d.Anapara); x.PB = Sec(Temiz(d.PB).ToUpperInvariant(), ParaBirimleri, "TRY"); x.Kur = d.Kur > 0 ? d.Kur : 1; x.FaizBaslangic = d.FaizBaslangic?.Date;
                x.IslemisFaiz = R2(d.IslemisFaiz); x.Komisyon = R2(d.Komisyon); x.Bsmv = R2(d.Bsmv); x.Kdv = R2(d.Kdv); x.TemerrutFaizi = R2(d.TemerrutFaizi);
                x.BakiyeDoviz = d.BakiyeDoviz != 0 ? R2(d.BakiyeDoviz) : R2(x.Anapara + x.IslemisFaiz + x.Komisyon + x.Bsmv + x.Kdv + x.TemerrutFaizi);
                x.Bakiye = d.Bakiye != 0 ? R2(d.Bakiye) : R2(x.BakiyeDoviz * (x.PB == "TRY" ? 1 : x.Kur)); x.Not = Temiz(d.Not);
                Yaz(); kayit = x;
            }
            return null;
        }
        public static string HesapSil(int id) { Yukle(); lock (Kilit) { var x = _depo.Hesaplar.FirstOrDefault(y => y.Id == id); if (x == null) return "Kayıt bulunamadı."; _depo.Hesaplar.Remove(x); Yaz(); } return null; }
        public static string TahsilatKaydet(KonkordatoTahsilat d, string kullanici, out KonkordatoTahsilat kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            if (d.Tutar <= 0) return "Tahsilat tutarı sıfırdan büyük olmalıdır.";
            lock (Kilit)
            {
                int alacakId = d.AlacakId; string banka = Temiz(d.Banka), borclu = Temiz(d.Borclu); var h = BagKontrol(ref alacakId, ref banka, ref borclu); if (h != null) return h;
                var x = d.Id > 0 ? _depo.Tahsilatlar.FirstOrDefault(y => y.Id == d.Id) : null; if (d.Id > 0 && x == null) return "Kayıt bulunamadı.";
                if (x == null) { x = new KonkordatoTahsilat { Id = YeniId(_depo.Tahsilatlar, y => y.Id), Kaynak = "Ekran" }; _depo.Tahsilatlar.Add(x); }
                x.AlacakId = alacakId; x.Banka = banka; x.Borclu = borclu; x.Tarih = d.Tarih?.Date ?? d.Valor?.Date; x.Vade = d.Vade?.Date; x.Valor = d.Valor?.Date; x.Kanal = Sec(d.Kanal, TahsilatKanallari, "Diğer"); x.CekNo = Temiz(d.CekNo); x.CekBankasi = Temiz(d.CekBankasi);
                x.Odeyen = Temiz(d.Odeyen); x.Firma = Temiz(d.Firma); x.Tutar = R2(d.Tutar); x.PB = Sec(Temiz(d.PB).ToUpperInvariant(), ParaBirimleri, "TRY"); x.Mahsup = d.Mahsup; x.Aciklama = Temiz(d.Aciklama);
                Yaz(); kayit = x;
            }
            return null;
        }
        public static string TahsilatSil(int id) { Yukle(); lock (Kilit) { var x = _depo.Tahsilatlar.FirstOrDefault(y => y.Id == id); if (x == null) return "Kayıt bulunamadı."; _depo.Tahsilatlar.Remove(x); Yaz(); } return null; }
        public static string MektupKaydet(KonkordatoMektup d, string kullanici, out KonkordatoMektup kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            if (d.Tutar < 0 || d.TazminBedeli < 0) return "Tutarlar negatif olamaz.";
            lock (Kilit)
            {
                int alacakId = d.AlacakId; string banka = Temiz(d.Banka), borclu = Temiz(d.Borclu); var h = BagKontrol(ref alacakId, ref banka, ref borclu); if (h != null) return h;
                var x = d.Id > 0 ? _depo.Mektuplar.FirstOrDefault(y => y.Id == d.Id) : null; if (d.Id > 0 && x == null) return "Kayıt bulunamadı.";
                if (x == null) { x = new KonkordatoMektup { Id = YeniId(_depo.Mektuplar, y => y.Id), Kaynak = "Ekran" }; _depo.Mektuplar.Add(x); }
                x.AlacakId = alacakId; x.Banka = banka; x.Borclu = borclu; x.MektupNo = Temiz(d.MektupNo); x.KkbNo = Temiz(d.KkbNo); x.Verilis = d.Verilis?.Date; x.Gecerlilik = d.Gecerlilik?.Date; x.Tutar = R2(d.Tutar); x.PB = Sec(Temiz(d.PB).ToUpperInvariant(), ParaBirimleri, "TRY"); x.Kur = d.Kur;
                x.Muhatap = Temiz(d.Muhatap); x.Konu = Temiz(d.Konu); x.Sayfa = Temiz(d.Sayfa);
                x.TazminTarihi = d.TazminTarihi?.Date; x.Durum = Sec(d.Durum, MektupDurumlari, "Tazmin Edildi"); x.TazminBedeli = d.TazminBedeli > 0 ? R2(d.TazminBedeli) : (x.Durum == "Tazmin Edildi" ? R2(x.Tutar * (x.PB == "TRY" || x.Kur <= 0 ? 1 : x.Kur)) : 0); x.Not = Temiz(d.Not);
                Yaz(); kayit = x;
            }
            return null;
        }
        public static string MektupSil(int id) { Yukle(); lock (Kilit) { var x = _depo.Mektuplar.FirstOrDefault(y => y.Id == id); if (x == null) return "Kayıt bulunamadı."; _depo.Mektuplar.Remove(x); Yaz(); } return null; }

        // ---------- ekran paketi ----------
        public static object Paket()
        {
            Yukle();
            lock (Kilit)
            {
                var A = _depo.Alacaklar.OrderBy(a => a.Nitelik == "Kefaleten" ? 1 : 0).ThenBy(a => a.Sira).ThenBy(a => a.Id).ToList(); var ds = _depo.Dosya;
                var borclular = ds.Davacilar.Concat(_depo.Alacaklar.Select(a => a.Borclu)).Concat(_depo.Sozlesmeler.Select(s => s.Borclu)).Concat(_depo.Hesaplar.Select(h => h.Borclu)).Concat(_depo.Tahsilatlar.Select(t => t.Borclu)).Concat(_depo.Mektuplar.Select(m => m.Borclu)).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x, StringComparer.Create(Tr, false)).ToList();
                object Alacak(KonkordatoAlacak a)
                {
                    decimal itibar = Itibar(a), tahsilatKayit = TahsilatKayit(a), tahsilat = TahsilatEtkin(a), bakiye = Bakiye(a), fazlaOdeme = FazlaOdeme(a), raporH = RaporHesaplanan(a), rapor = RaporEtkin(a), cek = CekTaahhutEtkin(a);
                    var hesaplar = _depo.Hesaplar.Where(h => h.AlacakId == a.Id).ToList(); var mektuplar = _depo.Mektuplar.Where(m => m.AlacakId == a.Id).ToList(); var sozlesmeler = _depo.Sozlesmeler.Where(s => s.AlacakId == a.Id).ToList(); var tahsilatlar = _depo.Tahsilatlar.Where(t => t.AlacakId == a.Id).ToList();
                    return new
                    {
                        a.Id, a.Anahtar, a.Sira, a.Banka, a.Borclu, a.Nitelik, a.Kefiller, a.Durum, a.AlacakStatusu, a.RehinMiktari, a.RehinNot,
                        a.KatNoter, katIhtarTarihi = D(a.KatIhtarTarihi), a.KatYevmiye, katTarihi = D(a.KatTarihi), a.KatNakdi, a.KatPb, a.KatGayrinakdiTl, a.KatGayrinakdiUsd, a.MehilGun, a.TebligYontemi, tebligTarihi = D(a.TebligTarihi), yasalTebligTarihi = D(a.YasalTebligTarihi), a.TebligAlan, temerrutTarihi = D(a.TemerrutTarihi), a.KatNot,
                        a.MuhletNakdi, a.MuhletItibar, itibar, a.MuhletGayrinakdi, a.TazminToplam, tazminKayit = mektuplar.Where(m => m.Durum == "Tazmin Edildi").Sum(m => m.TazminBedeli), a.TahsilatToplam, tahsilatKayit, tahsilat, bakiye, fazlaOdeme,
                        hesapToplam = hesaplar.Sum(h => h.Bakiye), hesapNakdi = hesaplar.Where(h => h.Nitelik != "Gayrinakdi").Sum(h => h.Bakiye), hesapGayrinakdi = hesaplar.Where(h => h.Nitelik == "Gayrinakdi").Sum(h => h.Bakiye),
                        a.AkdiFaiz, a.TemerrutFaizi, a.TemerrutMaddesi, a.BsmvOrani, faizDonemleri = a.FaizDonemleri.Select(f => new { f.Tur, f.Aciklama, baslangic = D(f.Baslangic), bitis = D(f.Bitis), f.Gun, f.Oran, f.Matrah, f.Tutar, hesaplanan = FaizHesapla(f) }).ToList(), faizToplam = a.FaizDonemleri.Sum(f => f.Tutar),
                        a.RaporNakdi, raporHesaplanan = raporH, rapor, a.RaporGayrinakdi, a.CekYaprak, a.CekYaprakBedeli, a.CekTaahhut, cekTaahhutEtkin = cek, a.CekEkNo,   // camelCase çakışması olmasın diye ayrı ad
                        a.HukukiDegerlendirme, a.Not, a.Kaynak, olusturma = D(a.Olusturma), a.Olusturan, guncelleme = a.Guncelleme == default ? null : a.Guncelleme.ToString("yyyy-MM-dd HH:mm"), a.Guncelleyen,
                        odemeDurumu = string.IsNullOrEmpty(a.OdemeDurumu) ? "Açık" : a.OdemeDurumu, odemeTarihi = D(a.OdemeTarihi), odemeNot = a.OdemeNot ?? "", krediNo = a.KrediNo ?? "", a.GuncelBorc, guncelTarih = D(a.GuncelTarih),   // ödeme durumu (11 Kas 2026)
                        sozlesmeSayisi = sozlesmeler.Count, sozlesmeToplam = sozlesmeler.Where(s => s.PB == "TRY").Sum(s => s.Tutar), sozlesmeDoviz = sozlesmeler.Where(s => s.PB != "TRY").GroupBy(s => s.PB).Select(g => new { pb = g.Key, tutar = g.Sum(s => s.Tutar) }).ToList(),
                        hesapSayisi = hesaplar.Count, tahsilatSayisi = tahsilatlar.Count, mektupSayisi = mektuplar.Count, ipotekSayisi = KrediYapilandirmaOrnek.TeminatSayisi(a.Id), cekSayisi = KrediYapilandirmaOrnek.CekSayisi(a.Id)
                    };
                }
                return new
                {
                    dosya = new { ds.Mahkeme, ds.DosyaNo, ds.DavaTuru, davaTarihi = D(ds.DavaTarihi), ds.Davali, ds.Davacilar, ds.Vekil, araKararTarihi = D(ds.AraKararTarihi), ds.AraKararNot, geciciMuhletTarihi = D(ds.GeciciMuhletTarihi), ds.GeciciMuhletAy, raporTarihi = D(ds.RaporTarihi), ds.Bilirkisi, ds.BilirkisiUnvan, kurTarihi = D(ds.KurTarihi), ds.UsdKur, ds.EurKur, ds.Not },
                    durumlar = Durumlar, odemeDurumlari = OdemeDurumlari, statuler = Statuler, nitelikler = Nitelikler, sozlesmeTurleri = SozlesmeTurleri, hesapNitelikleri = HesapNitelikleri, faizDonemTurleri = FaizDonemleri, tahsilatKanallari = TahsilatKanallari, mektupDurumlari = MektupDurumlari, faizTurleri = FaizTurleri, tebligYontemleri = TebligYontemleri, paraBirimleri = ParaBirimleri, borclular,
                    alacaklar = A.Select(Alacak).ToList(),
                    sozlesmeler = _depo.Sozlesmeler.OrderBy(s => s.Banka, StringComparer.Create(Tr, false)).ThenBy(s => s.Tarih).Select(s => new { s.Id, s.AlacakId, s.Banka, s.Borclu, s.Tur, s.SozlesmeNo, tarih = D(s.Tarih), s.Tutar, s.PB, kefiller = s.Kefiller.Select(k => new { k.Ad, k.Limit, k.Davaci }).ToList(), kefilToplam = s.Kefiller.Sum(k => k.Limit), davaciKefil = s.Kefiller.Count(k => k.Davaci), s.Not, s.Kaynak }).ToList(),
                    hesaplar = _depo.Hesaplar.OrderBy(h => h.Banka, StringComparer.Create(Tr, false)).ThenBy(h => h.Borclu).ThenBy(h => h.Sira).ThenBy(h => h.Id).Select(h => new { h.Id, h.AlacakId, h.Banka, h.Borclu, h.Sira, h.KrediNo, h.Tur, h.Nitelik, h.FaizOrani, h.FaizDonemi, h.GecikmeOrani, h.KrediTutari, odenenAnapara = h.KrediTutari > 0 ? R2(h.KrediTutari - h.Anapara) : 0m, h.Anapara, h.PB, h.Kur, faizBaslangic = D(h.FaizBaslangic), h.IslemisFaiz, h.Komisyon, h.Bsmv, h.Kdv, h.TemerrutFaizi, h.BakiyeDoviz, h.Bakiye, h.Not, h.Kaynak }).ToList(),
                    tahsilatlar = _depo.Tahsilatlar.OrderBy(t => t.Tarih ?? t.Valor ?? t.Vade).ThenBy(t => t.Id).Select(t => new { t.Id, t.AlacakId, t.Banka, t.Borclu, tarih = D(t.Tarih), vade = D(t.Vade), valor = D(t.Valor), t.Kanal, t.CekNo, t.CekBankasi, t.Odeyen, t.Firma, t.Tutar, t.PB, t.Mahsup, t.Aciklama, t.Kaynak }).ToList(),
                    mektuplar = _depo.Mektuplar.OrderBy(m => m.Banka, StringComparer.Create(Tr, false)).ThenBy(m => m.TazminTarihi).ThenBy(m => m.Id).Select(m => new { m.Id, m.AlacakId, m.Banka, m.Borclu, m.MektupNo, m.KkbNo, verilis = D(m.Verilis), gecerlilik = D(m.Gecerlilik), m.Tutar, m.PB, m.Kur, tazminTarihi = D(m.TazminTarihi), m.TazminBedeli, m.Durum, m.Muhatap, m.Konu, m.Sayfa, m.Not, m.Kaynak }).ToList(),
                    kontroller = (_depo.Kontroller ?? new List<KonkordatoKontrol>()).Select(k => new { k.Banka, k.Sirket, k.Durum, k.Not, tarih = k.Tarih.HasValue ? k.Tarih.Value.ToString("yyyy-MM-dd HH:mm") : null, k.Kullanici }).ToList(), kontrolDurumlari = KontrolDurumlari,   // banka × şirket veri kontrol listesi (11 Kas 2026)
                    toplam = new
                    {
                        alacak = A.Count, banka = A.Select(a => a.Banka).Distinct().Count(), asaleten = A.Count(a => a.Nitelik != "Kefaleten"), kefaleten = A.Count(a => a.Nitelik == "Kefaleten"),
                        muhletNakdi = A.Where(a => a.Nitelik != "Kefaleten").Sum(Bakiye), muhletGayrinakdi = A.Where(a => a.Nitelik != "Kefaleten").Sum(a => a.MuhletGayrinakdi), raporNakdi = A.Where(a => a.Nitelik != "Kefaleten").Sum(RaporEtkin), raporGayrinakdi = A.Where(a => a.Nitelik != "Kefaleten").Sum(a => a.RaporGayrinakdi),
                        tahsilat = A.Sum(TahsilatEtkin), tazmin = _depo.Mektuplar.Where(m => m.Durum == "Tazmin Edildi").Sum(m => m.TazminBedeli), rehin = A.Sum(a => a.RehinMiktari), imtiyazli = A.Where(a => a.AlacakStatusu == "İmtiyazlı / Rüçhanlı Alacak").Sum(Bakiye),
                        kefaletenNakdi = A.Where(a => a.Nitelik == "Kefaleten").Sum(Bakiye), kefaletenGayrinakdi = A.Where(a => a.Nitelik == "Kefaleten").Sum(a => a.MuhletGayrinakdi)
                    }
                };
            }
        }
    }
}
