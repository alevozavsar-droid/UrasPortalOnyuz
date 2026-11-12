// ============================================================
// İBKB BEYANNAME TAKİBİ — kalıcı veri (Mali İşler › Finans › Döviz ve Dış Ticaret Finansmanı; Yurtdışı Satış › İhracat Operasyon Takibi'nde kısayol)
// 11 Kas 2026 ("finans ve ihracat arasında dinamik bir İBKB raporu olmalı; eksikler varsa ekle, raporu geliştir"): üretilmiş Rapor168 iskeleti yerine gerçek kayıt.
// Kaynak: ihracat departmanının IMKB_LISTELERI.xlsx dosyası (2025-URAS 186, 2026-URAS 23, 2026-SELVİ 125 dosya) → Data/Tohum/ibkb-listeleri-2026-11.json (ilk açılışta uygulanır).
// Kayıt = İHRACAT DOSYASI: şirket, yıl, dosya no, müşteri (kod + ad), başlangıç / kapanış tarihi, gümrük çıkış beyannamesi (GÇB) no, beyanname tutarı + döviz,
//   ülke gerekliliği (% — bedelin ne kadarı yurda getirilmeli), DAB / İBKB durumu (Bekliyor | Yapıldı | Peşin Yapıldı | Kısmi | Gerek Yok | İptal), banka,
//   kısmi kalan, bağlanan DAB satırları (tutar + döviz + banka + tarih), not. Hesaplanan: gereken = tutar × oran, getirilen, kalan, vade = başlangıç + 180 gün
//   (ihracat bedelinin yurda getirilme süresi), kalan gün, takip durumu (Açık | Vade Yaklaşıyor | Vadesi Geçti | Kısmi | Kapandı | Muaf | İptal).
// Kalıcılık: App_Data/ibkb.json (git dışı). Ekran: Controllers/IbkbController.cs + Views/Ibkb/Index.cshtml + wwwroot/js/ibkb.js.
// Yazım: liste değerleri Title Case; tutarlar kendi dövizinde ($ € £ ₽); dövizi Excel'de yazılmayan satır "Döviz Belirsiz" rozetiyle gösterilir (uydurulmaz).
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication3.Data
{
    /// <summary>Dosyaya bağlanan DAB / İBKB satırı (bankadan gelen döviz alım belgesi).</summary>
    public class IbkbDab { public decimal Tutar { get; set; } public string Doviz { get; set; } = "USD"; public DateTime? Tarih { get; set; } public string Banka { get; set; } = ""; public string No { get; set; } = ""; public string Not { get; set; } = ""; }
    public class IbkbDabIstek { public decimal Tutar { get; set; } public string Doviz { get; set; } public string Tarih { get; set; } public string Banka { get; set; } public string No { get; set; } public string Not { get; set; } }

    /// <summary>İhracat dosyası (beyanname) kaydı.</summary>
    public class IbkbDosya
    {
        public int Id { get; set; } public string Anahtar { get; set; } = "";
        public string Sirket { get; set; } = ""; public int Yil { get; set; } public string DosyaNo { get; set; } = ""; public string Aciklama { get; set; } = "";
        public string MusteriKodu { get; set; } = ""; public string Musteri { get; set; } = ""; public string Ulke { get; set; } = "";
        public string FaturaNo { get; set; } = ""; public string SiparisNo { get; set; } = "";   // İhracat Satışları (Yurtdışı Satış Siparişi) bağı (11 Kas 2026)
        public string Temsilci { get; set; } = "";   // satış temsilcisi (boşsa cari kartın satış temsilcisi; Özet › Yıl × Satış Temsilcisi; 11 Kas 2026)
        public DateTime? Baslangic { get; set; } public DateTime? Kapanis { get; set; }
        public string BeyannameNo { get; set; } = ""; public string Doviz { get; set; } = ""; public decimal Tutar { get; set; }
        /// <summary>Gerçek ihracat bedeli (proforma / fatura; müşteriyle anlaşılan tutar) — beyanname tutarından farklı olabilir (11 Kas 2026: "beyanname 100.000 $ ama proforma 50.000 $, müşteri talebi"). 0 = beyanname ile aynı. Fark nedeni: Müşteri Talebi, İskonto …</summary>
        public decimal GercekBedel { get; set; } public string FarkNedeni { get; set; } = "";
        public decimal UlkeOran { get; set; } = 100m;
        public string DabDurumu { get; set; } = "Bekliyor"; public string DabBanka { get; set; } = ""; public decimal DabKalan { get; set; } public string DabNotu { get; set; } = "";
        public List<IbkbDab> Dablar { get; set; } = new List<IbkbDab>();
        public string Not { get; set; } = ""; public string Kaynak { get; set; } = "Ekran";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = ""; public DateTime? Guncelleme { get; set; } public string Guncelleyen { get; set; } = "";
    }
    public class IbkbIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public int Yil { get; set; } public string DosyaNo { get; set; } public string Aciklama { get; set; }
        public string MusteriKodu { get; set; } public string Musteri { get; set; } public string Ulke { get; set; } public string FaturaNo { get; set; } public string SiparisNo { get; set; } public string Temsilci { get; set; } public string Baslangic { get; set; } public string Kapanis { get; set; }
        public string BeyannameNo { get; set; } public string Doviz { get; set; } public decimal Tutar { get; set; } public decimal GercekBedel { get; set; } public string FarkNedeni { get; set; } public decimal UlkeOran { get; set; } = 100m;
        public string DabDurumu { get; set; } public string DabBanka { get; set; } public decimal DabKalan { get; set; } public string DabNotu { get; set; }
        public List<IbkbDabIstek> Dablar { get; set; } public string Not { get; set; }
    }
    /// <summary>Bankadan gelen ihracat tahsilatı (döviz girişi). Kaynak: Banka (Otomatik) — banka / ERP entegrasyonu bağlanınca; Elle; Excel (Bağlanan DAB). Durum: Eşleşmedi → Eşleşti (dosyaya bağlandı, DAB satırı yazıldı) → İBKB Düzenlendi.</summary>
    public class IhracatTahsilat
    {
        public int Id { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public DateTime? Tarih { get; set; }
        public string Gonderen { get; set; } = ""; public string MusteriKodu { get; set; } = ""; public decimal Tutar { get; set; } public string Doviz { get; set; } = "USD";
        public string Referans { get; set; } = ""; public string Aciklama { get; set; } = ""; public string Kaynak { get; set; } = "Elle";
        public string Durum { get; set; } = "Eşleşmedi"; public int DosyaId { get; set; } public int IbkbBelgeId { get; set; }
        public DateTime? EslestirmeTarihi { get; set; } public string Eslestiren { get; set; } = "";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
    }
    public class IhracatTahsilatIstek { public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public string Tarih { get; set; } public string Gonderen { get; set; } public string MusteriKodu { get; set; } public decimal Tutar { get; set; } public string Doviz { get; set; } public string Referans { get; set; } public string Aciklama { get; set; } }
    public class EslestirIstek { public int TahsilatId { get; set; } public int DosyaId { get; set; } public bool IbkbOlustur { get; set; } }
    /// <summary>Bankaya verilecek İBKB başvuru belgesi: tahsilat ↔ ihracat dosyası eşleşmesinden üretilir; banka İBKB numarasını verince kapanır.</summary>
    public class IbkbBelge
    {
        public int Id { get; set; } public string No { get; set; } = ""; public DateTime Tarih { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = "";
        public int DosyaId { get; set; } public string DosyaNo { get; set; } = ""; public string BeyannameNo { get; set; } = ""; public string FaturaNo { get; set; } = "";
        public string MusteriKodu { get; set; } = ""; public string Musteri { get; set; } = ""; public string Ulke { get; set; } = "";
        public decimal BeyannameTutar { get; set; } public string BeyannameDoviz { get; set; } = ""; public decimal Tutar { get; set; } public string Doviz { get; set; } = "";
        public int TahsilatId { get; set; } public string TahsilatRef { get; set; } = ""; public DateTime? TahsilatTarihi { get; set; } public DateTime? FiiliIhrac { get; set; }
        public string Durum { get; set; } = "Hazırlandı"; public string BankaIbkbNo { get; set; } = ""; public DateTime? BankaTarihi { get; set; } public string Not { get; set; } = "";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
    }
    public class IbkbBelgeIstek { public int Id { get; set; } public string Durum { get; set; } public string BankaIbkbNo { get; set; } public string BankaTarihi { get; set; } public string Not { get; set; } }
    /// <summary>İhracat cari bakiyesi kontrol kaydı (11 Kas 2026, "muhasebe mizanından ihracat cari bakiyelerini getir; bakiyeler düzenli kontrol edilsin"): kullanıcı carinin bakiyesini İBKB dosyalarıyla karşılaştırıp işaretler; son kontrol 30 günü geçince "Kontrol Gecikti".</summary>
    public class IbkbBakiyeKontrol { public int Id { get; set; } public string CariKodu { get; set; } = ""; public DateTime Tarih { get; set; } public string Kullanici { get; set; } = ""; public string Durum { get; set; } = "Uyumlu"; public string Not { get; set; } = ""; public string BakiyeOzet { get; set; } = ""; public string FarkOzet { get; set; } = ""; }
    public class IbkbBakiyeKontrolIstek { public string CariKodu { get; set; } public string Durum { get; set; } public string Not { get; set; } public string BakiyeOzet { get; set; } public string FarkOzet { get; set; } public IbkbGorevIstek Gorev { get; set; } }
    /// <summary>
    /// CARİ BAKİYE DÜZELTME GÖREVİ (12 Kas 2026; kullanıcı: "Kontrol Et'te açılan kısımda düzeltmeyi yapacak kişiye atama yapabilelim, mailine düşsün, rapor sayfamızda bekleyen görevler olsun,
    /// düzelttikten sonra muhasebeci onay versin; böylece performans takibi yapılır"). Akış: Atandı → (atanan) Düzeltildi → (muhasebeci) Onaylandı ya da İade Edildi → yeniden Düzeltildi …
    /// Bildirim: atananın e-postasına (cihazdaki posta uygulaması; SMTP bağlanınca doğrudan) + portal mesaj kutusuna (atanan portal kullanıcısıysa). Performans: kişi bazında açık / tamamlanan / ortalama süre / termin aşımı / iade.
    /// </summary>
    public class IbkbGorevAdim { public DateTime Tarih { get; set; } public string Kullanici { get; set; } = ""; public string Islem { get; set; } = ""; public string Not { get; set; } = ""; }
    public class IbkbGorev
    {
        public int Id { get; set; } public string CariKodu { get; set; } = ""; public string CariAd { get; set; } = ""; public string Sirket { get; set; } = "";
        public string Konu { get; set; } = ""; public string Aciklama { get; set; } = ""; public string BakiyeOzet { get; set; } = ""; public string FarkOzet { get; set; } = "";
        public string Atanan { get; set; } = ""; public string AtananKod { get; set; } = ""; public string AtananEposta { get; set; } = ""; public string Atayan { get; set; } = "";
        public DateTime Tarih { get; set; } public DateTime? Termin { get; set; } public string Oncelik { get; set; } = "Normal"; public string Durum { get; set; } = "Atandı";
        public DateTime? DuzeltmeTarihi { get; set; } public string Duzelten { get; set; } = ""; public string DuzeltmeNotu { get; set; } = "";
        public DateTime? OnayTarihi { get; set; } public string Onaylayan { get; set; } = ""; public string OnayNotu { get; set; } = ""; public int IadeSayisi { get; set; }
        public int KontrolId { get; set; } public int GonderimId { get; set; } public string Bildirim { get; set; } = "";
        public List<IbkbGorevAdim> Gecmis { get; set; } = new List<IbkbGorevAdim>();
    }
    /// <summary>Islem: ata (yeni) · guncelle (yeniden atama / termin) · duzeltildi · onayla · iade · iptal.</summary>
    public class IbkbGorevIstek { public int Id { get; set; } public string Islem { get; set; } public string CariKodu { get; set; } public string Konu { get; set; } public string Aciklama { get; set; } public string Atanan { get; set; } public string AtananKod { get; set; } public string AtananEposta { get; set; } public string Termin { get; set; } public string Oncelik { get; set; } public string Not { get; set; } public int GonderimId { get; set; } }
    /// <summary>Müşteriye mutabakat gönderimi (12 Kas 2026; "Cari Bakiyeler'den müşteriye otomatik mutabakat gönderebilelim, ekstreyi veya bakiye olarak WhatsApp veya mail"): kayıt burada + Cari Mutabakat Yönetimi günlüğünde (MtGonderim). Icerik: Bakiye | Ekstre.</summary>
    public class CariGonderim { public int Id { get; set; } public string CariKodu { get; set; } = ""; public string CariAd { get; set; } = ""; public string Kanal { get; set; } = "E-posta"; public string Icerik { get; set; } = "Bakiye"; public string Alici { get; set; } = ""; public string AliciAd { get; set; } = ""; public DateTime Tarih { get; set; } public string Kullanici { get; set; } = ""; public string Konu { get; set; } = ""; public string Mesaj { get; set; } = ""; public string BakiyeOzet { get; set; } = ""; public string Durum { get; set; } = "Gönderildi"; public DateTime? YanitTarihi { get; set; } public string Not { get; set; } = ""; public int MtGonderimId { get; set; } public string MtBelgeNo { get; set; } = ""; }
    public class CariGonderimIstek { public int Id { get; set; } public string CariKodu { get; set; } public string Kanal { get; set; } public string Icerik { get; set; } public string Alici { get; set; } public string AliciAd { get; set; } public string Konu { get; set; } public string Mesaj { get; set; } public string BakiyeOzet { get; set; } public string Durum { get; set; } public string Not { get; set; } }
    /// <summary>
    /// YURTDIŞI ŞUBE CARİLERİ (11 Kas 2026; kullanıcı: "Fas, Tunus, Cezayir, Amerika, Meksika, Mısır, Portekiz, Özbekistan ve Dubai'de kendimize ait şubelerimiz var; tek sayfada bakiye takibi, mutabakat için
    /// mail / WhatsApp ile ekstre gönderimi, şubeye tıklayıp detay"). Kaynak: satış temsilcilerinin "Şube Carileri" tabloları (PORT.pdf, VUSAL.pdf, ALİ SON.pdf, ER SON.pdf).
    /// Yıl satırı: Sermaye · Resmi Fatura · Gerçek Mal Bedeli · İade Faturası · Komisyon · Resmi Ödeme · Diğer Ödeme · Nakliye · Diğer · Düzeltme · Not.
    /// Hesap: Satış Esası = Gerçek Mal Bedeli (varsa) yoksa Resmi Fatura − İade; Toplam Ödeme = Resmi + Diğer Ödeme + Nakliye + Diğer + Komisyon; Ödeme − Satış = Sermaye + Toplam Ödeme + Düzeltme − Satış Esası; Devir = yıllar boyunca birikimli (şubenin bize borcu eksi).
    /// </summary>
    public class SubeYil
    {
        public string Yil { get; set; } = "";   // "2024" ya da "2016-2019" (kaynakta birleşik yıllar)
        public decimal Sermaye { get; set; } public decimal ResmiFatura { get; set; } public decimal GercekMalBedeli { get; set; } public decimal IadeFaturasi { get; set; } public decimal Komisyon { get; set; }
        public decimal ResmiOdeme { get; set; } public decimal DigerOdeme { get; set; } public decimal Nakliye { get; set; } public decimal Diger { get; set; } public decimal Duzeltme { get; set; }
        public string Not { get; set; } = "";
        public int YilSira => int.TryParse((Yil ?? "").Trim().Length >= 4 ? Yil.Trim().Substring(0, 4) : "0", out var y) ? y : 0;
    }
    /// <summary>Şube yıl satırındaki tutarı oluşturan kalem (12 Kas 2026; kullanıcı: "70.091,50 $ tıkladım, bu bakiyeyi oluşturan ihracatlar satır satır; ödeme detayları kalem kalem açılmalı"). Alan: resmiFatura | gercekMalBedeli | iadeFaturasi | komisyon | resmiOdeme | digerOdeme | nakliye | diger. Kaynak PDF'lerde yalnız yıllık toplam var; kalemler ekrandan ya da ihracat dosyası / banka tahsilatından eklenir.</summary>
    public class SubeKalem { public int Id { get; set; } public string Yil { get; set; } = ""; public string Alan { get; set; } = "resmiFatura"; public DateTime? Tarih { get; set; } public string BelgeNo { get; set; } = ""; public string Aciklama { get; set; } = ""; public decimal Tutar { get; set; } public string Not { get; set; } = ""; public string Kaynak { get; set; } = ""; public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = ""; }
    public class SubeKalemIstek { public int SubeId { get; set; } public int Id { get; set; } public string Yil { get; set; } public string Alan { get; set; } public string Tarih { get; set; } public string BelgeNo { get; set; } public string Aciklama { get; set; } public decimal Tutar { get; set; } public string Not { get; set; } public string Kaynak { get; set; } }
    public class SubeCari
    {
        public List<SubeKalem> Kalemler { get; set; } = new List<SubeKalem>();
        public int Id { get; set; } public string Kod { get; set; } = ""; public string Sube { get; set; } = ""; public string Ulke { get; set; } = "";
        public string Sirket { get; set; } = ""; public string UrunGrubu { get; set; } = ""; public string Temsilci { get; set; } = ""; public string Sorumlu { get; set; } = "";
        public string ParaBirimi { get; set; } = "USD"; public string Eposta { get; set; } = ""; public string Telefon { get; set; } = ""; public string Adres { get; set; } = ""; public string Not { get; set; } = "";
        public List<SubeYil> Yillar { get; set; } = new List<SubeYil>();
        public string Kaynak { get; set; } = ""; public DateTime Olusturma { get; set; } public DateTime? Guncelleme { get; set; } public string Guncelleyen { get; set; } = "";
    }
    public class SubeYilIstek { public string Yil { get; set; } public decimal Sermaye { get; set; } public decimal ResmiFatura { get; set; } public decimal GercekMalBedeli { get; set; } public decimal IadeFaturasi { get; set; } public decimal Komisyon { get; set; } public decimal ResmiOdeme { get; set; } public decimal DigerOdeme { get; set; } public decimal Nakliye { get; set; } public decimal Diger { get; set; } public decimal Duzeltme { get; set; } public string Not { get; set; } }
    public class SubeIstek { public int Id { get; set; } public string Kod { get; set; } public string Sube { get; set; } public string Ulke { get; set; } public string Sirket { get; set; } public string UrunGrubu { get; set; } public string Temsilci { get; set; } public string Sorumlu { get; set; } public string ParaBirimi { get; set; } public string Eposta { get; set; } public string Telefon { get; set; } public string Adres { get; set; } public string Not { get; set; } public List<SubeYilIstek> Yillar { get; set; } }
    /// <summary>Şubeye ekstre / mutabakat gönderimi (E-posta ya da WhatsApp): kayıt tutulur, cihazdaki posta / WhatsApp uygulaması mailto: / wa.me bağlantısıyla açılır (SMTP / WhatsApp Business API bağlanınca doğrudan gider).</summary>
    public class SubeGonderim { public int Id { get; set; } public int SubeId { get; set; } public string Kanal { get; set; } = "E-posta"; public string Alici { get; set; } = ""; public DateTime Tarih { get; set; } public string Kullanici { get; set; } = ""; public string Donem { get; set; } = ""; public string Konu { get; set; } = ""; public string Mesaj { get; set; } = ""; public string Bakiye { get; set; } = ""; public string Durum { get; set; } = "Gönderildi"; public string Not { get; set; } = ""; }
    public class SubeGonderimIstek { public int Id { get; set; } public int SubeId { get; set; } public string Kanal { get; set; } public string Alici { get; set; } public string Donem { get; set; } public string Konu { get; set; } public string Mesaj { get; set; } public string Bakiye { get; set; } public string Durum { get; set; } public string Not { get; set; } }
    public class IbkbDepo { public int TohumSurumu { get; set; } public int SubeTohumSurumu { get; set; } public List<IbkbDosya> Dosyalar { get; set; } = new List<IbkbDosya>(); public List<IhracatTahsilat> Tahsilatlar { get; set; } = new List<IhracatTahsilat>(); public List<IbkbBelge> Belgeler { get; set; } = new List<IbkbBelge>(); public List<IbkbBakiyeKontrol> BakiyeKontrolleri { get; set; } = new List<IbkbBakiyeKontrol>(); public List<SubeCari> Subeler { get; set; } = new List<SubeCari>(); public List<SubeGonderim> SubeGonderimleri { get; set; } = new List<SubeGonderim>(); public List<IbkbGorev> Gorevler { get; set; } = new List<IbkbGorev>(); public List<CariGonderim> CariGonderimleri { get; set; } = new List<CariGonderim>(); }
    public class IbkbTohum { public int Surum { get; set; } public string Kaynak { get; set; } = ""; public List<IbkbDosya> Dosyalar { get; set; } = new List<IbkbDosya>(); }

    public static class IbkbOrnek
    {
        public static readonly object Kilit = new object();
        static IbkbDepo _depo = new IbkbDepo(); static bool _yuklendi;
        public const int TohumSurum = 2;   // 2 (11 Kas 2026): Excel'deki bağlanan DAB satırları banka tahsilatı kaydı olarak da türetilir (Eşleşti)
        public static readonly string[] TahsilatDurumlari = { "Eşleşmedi", "Eşleşti", "İBKB Düzenlendi" };
        public static readonly string[] BelgeDurumlari = { "Hazırlandı", "Bankaya Gönderildi", "Banka Onayladı", "İptal" };
        public static readonly string[] TahsilatKaynaklari = { "Banka (Otomatik)", "Elle", "Excel (Bağlanan DAB)" };
        public static readonly string[] BakiyeKontrolDurumlari = { "Uyumlu", "Fark Var", "Açıklama Bekliyor" };
        /// <summary>İhracat satış temsilcileri (kullanıcı listesi, 11 Kas 2026).</summary>
        public static readonly string[] Temsilciler = { "Erman Kaval", "Ali Kemal Gündoğan", "Sevgi Kaya", "Berkan Özkan", "Aksel Güler" };
        public static readonly string[] SubeKanallar = { "E-posta", "WhatsApp" };
        public static readonly string[] SubeGonderimDurumlari = { "Gönderildi", "Mutabık", "İtirazlı", "Yanıt Bekleniyor" };
        public static readonly string[] GorevDurumlari = { "Atandı", "Düzeltildi", "Onaylandı", "İade Edildi", "İptal" };
        public static readonly string[] GorevOncelikleri = { "Normal", "Yüksek", "Acil" };
        public static readonly string[] CariIcerikler = { "Bakiye", "Ekstre" };
        public const int SubeTohumSurum = 1;
        public const int BakiyeKontrolGun = 30;   // cari bakiye kontrolü bu kadar günde bir yenilenir; geçerse "Kontrol Gecikti"
        public const int VadeGun = 180;   // ihracat bedelinin fiili ihraçtan itibaren yurda getirilme süresi (gün)
        public static readonly string[] Sirketler = { "Uras Kimya", "Selvi Kimya" };
        public static readonly string[] DabDurumlari = { "Bekliyor", "Yapıldı", "Peşin Yapıldı", "Kısmi", "Gerek Yok", "İptal" };
        public static readonly string[] TakipDurumlari = { "Açık", "Vade Yaklaşıyor", "Vadesi Geçti", "Kısmi", "Bedel Farkı", "Kapandı", "Muaf", "İptal" };   // Bedel Farkı: gerçek bedel tahsil edildi, yalnız beyanname fazlası açık
        public static readonly string[] FarkNedenleri = { "Müşteri Talebi", "İskonto / Fiyat Farkı", "Kısmi Sevkiyat", "Numune / Bedelsiz", "Kur Farkı", "Beyanname Hatası", "Diğer" };
        public static readonly string[] Dovizler = { "USD", "EUR", "GBP", "RUB" };
        /// <summary>DAB / İBKB eşiği (ABD doları karşılığı): TCMB İhracat Genelgesi md. 28/1-a — 15.000 ABD doları veya eşitine kadar noksanlığı olan ihracat hesapları DAB aranmadan doğrudan bankaca kapatılır (terkin);
        /// ihracat departmanı uygulaması: bu tutar ve altındaki dosyalarda DAB / İBKB "Gerek Yok". Genelgede "İBKB düzenlenmez" diye ayrı bir tutar maddesi yoktur (11 Kas 2026 araştırması); eşik değişirse burası güncellenir.</summary>
        public const decimal EsikUsd = 15000m;
        /// <summary>1 birim döviz = kaç USD (NakitAkisOrnek.Kurlar TL kurlarından çapraz; RUB kur tablosunda yok → yaklaşık 0,0125).</summary>
        public static Dictionary<string, decimal> UsdKur()
        {
            var k = NakitAkisOrnek.Kurlar; decimal usd = k.TryGetValue("USD", out var u) && u > 0 ? u : 1m;
            var m = new Dictionary<string, decimal> { ["USD"] = 1m };
            foreach (var pb in new[] { "EUR", "GBP" }) m[pb] = k.TryGetValue(pb, out var v) && v > 0 ? Math.Round(v / usd, 4) : 1m;
            m["RUB"] = 0.0125m; return m;
        }
        /// <summary>Dosyanın satış temsilcisi: dosyada yazılıysa o; yoksa müşteri cari kartının (kod ya da kısa ad / resmi unvan eşleşmesi) ana faaliyet satış temsilcisi; yoksa boş ("Atanmadı").</summary>
        public static (string ad, string kaynak) TemsilciBul(IbkbDosya d)
        {
            if (!string.IsNullOrWhiteSpace(d.Temsilci)) return (d.Temsilci.Trim(), "dosya");
            var k = string.IsNullOrEmpty(d.MusteriKodu) ? null : CariKartOrnek.KartBul(d.MusteriKodu);
            if (k == null && !string.IsNullOrWhiteSpace(d.Musteri)) { var ad = d.Musteri.Trim(); k = CariKartOrnek.Kartlar.FirstOrDefault(c => c.KartTipi == "Müşteri" && (string.Equals(c.KisaAd?.Trim(), ad, StringComparison.OrdinalIgnoreCase) || string.Equals(c.ResmiUnvan?.Trim(), ad, StringComparison.OrdinalIgnoreCase))); }
            var t = k == null ? "" : SatisSiparisOrnek.Temsilci(k);
            return (t ?? "", string.IsNullOrEmpty(t) ? "" : "cari kart");
        }
        public static decimal UsdKarsiligi(IbkbDosya d) => string.IsNullOrEmpty(d.Doviz) ? 0 : R2(d.Tutar * (UsdKur().TryGetValue(d.Doviz, out var r) ? r : 1m));
        public static bool EsikAlti(IbkbDosya d) => d.Tutar > 0 && !string.IsNullOrEmpty(d.Doviz) && UsdKarsiligi(d) <= EsikUsd;
        /// <summary>İhracat Genelgesi Ek-2: bedelin yurda getirilmesi zorunlu olmayan ülkeler (kaynaklarda 36 ülke; burada adı geçen 22'si — TCMB resmi metninden tamamlanmalı) · Ek-3: beyanname tutarının %50'si serbest (9 ülke, 01.08.2025 yürürlük).</summary>
        public static readonly string[] UlkeMuaf = { "Afganistan", "Angola", "Belarus", "Benin", "Burkina Faso", "Cibuti", "Çad", "Etiyopya", "Fildişi Sahili", "Filistin", "Gine", "İran", "Kenya", "Kırgızistan", "Kongo Cumhuriyeti", "Kuzey Kore", "Küba", "Lübnan", "Mozambik", "Tanzanya", "Venezuela", "Yemen" };
        public static readonly string[] UlkeYarim = { "Azerbaycan", "Cezayir", "Fas", "Kazakistan", "Libya", "Özbekistan", "Tunus", "Türkmenistan", "Ukrayna" };
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        public static string Dosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data", "ibkb.json");
        public static string TohumDosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "Data", "Tohum", "ibkb-listeleri-2026-11.json");
        static string D(DateTime? t) => t?.ToString("yyyy-MM-dd");
        static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        static string Temiz(string s) => (s ?? "").Trim();
        static string Sec(string v, string[] liste, string varsayilan) => liste.Contains(v ?? "") ? v : varsayilan;
        static DateTime? Tarih(string s) => DateTime.TryParse(s, out var t) ? t.Date : (DateTime?)null;

        // ---------- kalıcılık ----------
        public static void Yukle()
        {
            lock (Kilit)
            {
                if (_yuklendi) return; _yuklendi = true;
                var dosya = Dosya;
                if (dosya != null && File.Exists(dosya))
                {
                    try { _depo = JsonSerializer.Deserialize<IbkbDepo>(File.ReadAllText(dosya), Secenek) ?? new IbkbDepo(); }
                    catch (Exception ex) { Console.WriteLine("[İBKB] dosya okunamadı: " + ex.Message); _depo = new IbkbDepo(); }
                }
                _depo.Dosyalar ??= new List<IbkbDosya>(); foreach (var d in _depo.Dosyalar) d.Dablar ??= new List<IbkbDab>(); _depo.Tahsilatlar ??= new List<IhracatTahsilat>(); _depo.Belgeler ??= new List<IbkbBelge>(); _depo.BakiyeKontrolleri ??= new List<IbkbBakiyeKontrol>();
                _depo.Subeler ??= new List<SubeCari>(); foreach (var s in _depo.Subeler) { s.Yillar ??= new List<SubeYil>(); s.Kalemler ??= new List<SubeKalem>(); } _depo.SubeGonderimleri ??= new List<SubeGonderim>();
                _depo.Gorevler ??= new List<IbkbGorev>(); foreach (var g in _depo.Gorevler) g.Gecmis ??= new List<IbkbGorevAdim>(); _depo.CariGonderimleri ??= new List<CariGonderim>();
                if (_depo.TohumSurumu < TohumSurum) TohumUygula();
                if (_depo.SubeTohumSurumu < SubeTohumSurum) SubeTohumUygula();
                Console.WriteLine($"[İBKB] {_depo.Dosyalar.Count} ihracat dosyası ({string.Join(", ", _depo.Dosyalar.GroupBy(x => x.Sirket + " " + x.Yil).Select(g => g.Key + ": " + g.Count()))})" + (dosya != null ? $" · {dosya}" : " (kalıcılık kapalı)"));
            }
        }
        static void Yaz()
        {
            var dosya = Dosya; if (dosya == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(dosya)); File.WriteAllText(dosya, JsonSerializer.Serialize(_depo, Secenek)); }
            catch (Exception ex) { Console.WriteLine("[İBKB] dosya yazılamadı: " + ex.Message); }
        }
        /// <summary>Excel listesi tohumu: anahtarı (şirket|dosya no) olmayan satırlar eklenir; daha önce eklenmiş ya da kullanıcının girdiği kayıt ezilmez.</summary>
        // ---------- yurtdışı şube carileri ----------
        public static decimal SubeSatisEsas(SubeYil y) => y.GercekMalBedeli > 0 ? y.GercekMalBedeli : y.ResmiFatura - y.IadeFaturasi;
        public static decimal SubeToplamOdeme(SubeYil y) => y.ResmiOdeme + y.DigerOdeme + y.Nakliye + y.Diger + y.Komisyon;
        public static decimal SubeFark(SubeYil y) => R2(y.Sermaye + SubeToplamOdeme(y) + y.Duzeltme - SubeSatisEsas(y));
        public static List<SubeYil> SubeYillarSirali(SubeCari s) => s.Yillar.OrderBy(y => y.YilSira).ThenBy(y => y.Yil).ToList();
        public static decimal SubeBakiye(SubeCari s) => R2(s.Yillar.Sum(SubeFark));
        public static SubeCari SubeBul(int id) { Yukle(); lock (Kilit) return _depo.Subeler.FirstOrDefault(s => s.Id == id); }
        public static List<SubeGonderim> SubeGonderimler(int subeId) { Yukle(); lock (Kilit) return _depo.SubeGonderimleri.Where(g => g.SubeId == subeId).OrderByDescending(g => g.Tarih).ToList(); }
        public static string SubeKaydet(SubeIstek d, string kullanici, out SubeCari kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string ad = Temiz(d.Sube), ulke = Temiz(d.Ulke); if (ad == "") return "Şube adı zorunludur."; if (ulke == "") return "Ülke zorunludur.";
            lock (Kilit)
            {
                var x = d.Id > 0 ? _depo.Subeler.FirstOrDefault(s => s.Id == d.Id) : null; if (d.Id > 0 && x == null) return "Şube bulunamadı.";
                if (x == null) { x = new SubeCari { Id = _depo.Subeler.Select(s => s.Id).DefaultIfEmpty(0).Max() + 1, Olusturma = DateTime.Today, Kaynak = "Ekran" }; _depo.Subeler.Add(x); }
                x.Kod = Temiz(d.Kod) == "" ? $"SB-{x.Id:00}" : Temiz(d.Kod); x.Sube = ad; x.Ulke = ulke; x.Sirket = Temiz(d.Sirket); x.UrunGrubu = Temiz(d.UrunGrubu); x.Temsilci = Temiz(d.Temsilci); x.Sorumlu = Temiz(d.Sorumlu);
                x.ParaBirimi = Temiz(d.ParaBirimi).ToUpperInvariant() == "" ? "USD" : Temiz(d.ParaBirimi).ToUpperInvariant(); x.Eposta = Temiz(d.Eposta); x.Telefon = Temiz(d.Telefon); x.Adres = Temiz(d.Adres); x.Not = Temiz(d.Not);
                if (d.Yillar != null)
                {
                    var yl = d.Yillar.Where(y => y != null && Temiz(y.Yil) != "").Select(y => new SubeYil { Yil = Temiz(y.Yil), Sermaye = R2(y.Sermaye), ResmiFatura = R2(y.ResmiFatura), GercekMalBedeli = R2(y.GercekMalBedeli), IadeFaturasi = R2(y.IadeFaturasi), Komisyon = R2(y.Komisyon), ResmiOdeme = R2(y.ResmiOdeme), DigerOdeme = R2(y.DigerOdeme), Nakliye = R2(y.Nakliye), Diger = R2(y.Diger), Duzeltme = R2(y.Duzeltme), Not = Temiz(y.Not) }).ToList();
                    if (yl.GroupBy(y => y.Yil).Any(g => g.Count() > 1)) return "Aynı yıl iki kez girilemez.";
                    x.Yillar = yl;
                }
                x.Guncelleme = DateTime.Now; x.Guncelleyen = kullanici ?? "";
                Yaz(); kayit = x; return null;
            }
        }
        // ---------- şube yıl kalemleri (12 Kas 2026)
        public static readonly string[] SubeAlanlar = { "resmiFatura", "gercekMalBedeli", "iadeFaturasi", "komisyon", "resmiOdeme", "digerOdeme", "nakliye", "diger" };
        static decimal SubeAlanDeger(SubeYil y, string alan) => alan switch { "resmiFatura" => y.ResmiFatura, "gercekMalBedeli" => y.GercekMalBedeli, "iadeFaturasi" => y.IadeFaturasi, "komisyon" => y.Komisyon, "resmiOdeme" => y.ResmiOdeme, "digerOdeme" => y.DigerOdeme, "nakliye" => y.Nakliye, "diger" => y.Diger, _ => 0 };
        static void SubeAlanYaz(SubeYil y, string alan, decimal v) { switch (alan) { case "resmiFatura": y.ResmiFatura = v; break; case "gercekMalBedeli": y.GercekMalBedeli = v; break; case "iadeFaturasi": y.IadeFaturasi = v; break; case "komisyon": y.Komisyon = v; break; case "resmiOdeme": y.ResmiOdeme = v; break; case "digerOdeme": y.DigerOdeme = v; break; case "nakliye": y.Nakliye = v; break; case "diger": y.Diger = v; break; } }
        /// <summary>Kalem ekle / düzenle (Id &gt; 0). Yıl satırı yoksa açılır (tutarı kalemden gelmez; "Eşitle" ile aktarılır).</summary>
        public static string SubeKalemKaydet(SubeKalemIstek d, string kullanici, out SubeCari sube)
        {
            sube = null; if (d == null) return "Veri yok."; Yukle();
            string yil = Temiz(d.Yil), alan = Temiz(d.Alan); if (yil == "") return "Yıl gerekli."; if (!SubeAlanlar.Contains(alan)) return "Alan geçersiz."; if (d.Tutar == 0) return "Tutar sıfır olamaz.";
            lock (Kilit)
            {
                var s = _depo.Subeler.FirstOrDefault(x => x.Id == d.SubeId); if (s == null) return "Şube bulunamadı.";
                var k = d.Id > 0 ? s.Kalemler.FirstOrDefault(x => x.Id == d.Id) : null; if (d.Id > 0 && k == null) return "Kalem bulunamadı.";
                if (k == null) { k = new SubeKalem { Id = s.Kalemler.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1, Olusturma = DateTime.Now, Olusturan = Kim(kullanici), Kaynak = Temiz(d.Kaynak) == "" ? "Ekran" : Temiz(d.Kaynak) }; s.Kalemler.Add(k); }
                k.Yil = yil; k.Alan = alan; k.Tarih = Tarih(d.Tarih); k.BelgeNo = Temiz(d.BelgeNo); k.Aciklama = Temiz(d.Aciklama); k.Tutar = R2(d.Tutar); k.Not = Temiz(d.Not);
                if (!s.Yillar.Any(y => y.Yil == yil)) s.Yillar.Add(new SubeYil { Yil = yil, Not = "Kalem girişiyle açıldı" });
                s.Guncelleme = DateTime.Now; s.Guncelleyen = kullanici ?? ""; Yaz(); sube = s; return null;
            }
        }
        public static string SubeKalemSil(int subeId, int id)
        {
            Yukle(); lock (Kilit) { var s = _depo.Subeler.FirstOrDefault(x => x.Id == subeId); if (s == null) return "Şube bulunamadı."; var k = s.Kalemler.FirstOrDefault(x => x.Id == id); if (k == null) return "Kalem bulunamadı."; s.Kalemler.Remove(k); Yaz(); return null; }
        }
        /// <summary>Yıl satırındaki alanı kalem toplamıyla eşitler (kaynak PDF toplamı yerine kalem kalem girilen tutar esas olur).</summary>
        public static string SubeYilEsitle(int subeId, string yil, string alan, string kullanici, out decimal yeni)
        {
            yeni = 0; Yukle(); yil = Temiz(yil); alan = Temiz(alan); if (!SubeAlanlar.Contains(alan)) return "Alan geçersiz.";
            lock (Kilit)
            {
                var s = _depo.Subeler.FirstOrDefault(x => x.Id == subeId); if (s == null) return "Şube bulunamadı."; var y = s.Yillar.FirstOrDefault(x => x.Yil == yil); if (y == null) return "Yıl satırı bulunamadı.";
                var kl = s.Kalemler.Where(k => k.Yil == yil && k.Alan == alan).ToList(); if (kl.Count == 0) return "Bu yıl ve alanda kalem yok.";
                yeni = R2(kl.Sum(k => k.Tutar)); var eski = SubeAlanDeger(y, alan); SubeAlanYaz(y, alan, yeni);
                y.Not = string.IsNullOrEmpty(y.Not) ? $"{alan}: {eski:N2} → kalem toplamı {yeni:N2} ({DateTime.Today:dd.MM.yyyy})" : y.Not + $" · {alan}: {eski:N2} → {yeni:N2}";
                s.Guncelleme = DateTime.Now; s.Guncelleyen = kullanici ?? ""; Yaz(); return null;
            }
        }
        public static string SubeSil(int id)
        {
            Yukle(); lock (Kilit) { var x = _depo.Subeler.FirstOrDefault(s => s.Id == id); if (x == null) return "Şube bulunamadı."; _depo.Subeler.Remove(x); _depo.SubeGonderimleri.RemoveAll(g => g.SubeId == id); Yaz(); return null; }
        }
        public static string SubeGonderimKaydet(SubeGonderimIstek d, string kullanici, out SubeGonderim kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            lock (Kilit)
            {
                if (d.Id > 0)
                {   // yanıt / durum güncelleme
                    var g = _depo.SubeGonderimleri.FirstOrDefault(x => x.Id == d.Id); if (g == null) return "Gönderim bulunamadı.";
                    g.Durum = Sec(d.Durum, SubeGonderimDurumlari, g.Durum); g.Not = Temiz(d.Not); Yaz(); kayit = g; return null;
                }
                var s = _depo.Subeler.FirstOrDefault(x => x.Id == d.SubeId); if (s == null) return "Şube bulunamadı.";
                string kanal = Sec(d.Kanal, SubeKanallar, "E-posta"), alici = Temiz(d.Alici); if (alici == "") return kanal == "WhatsApp" ? "WhatsApp için telefon numarası gerekli." : "E-posta adresi gerekli.";
                kayit = new SubeGonderim { Id = _depo.SubeGonderimleri.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1, SubeId = s.Id, Kanal = kanal, Alici = alici, Tarih = DateTime.Now, Kullanici = kullanici ?? "", Donem = Temiz(d.Donem), Konu = Temiz(d.Konu), Mesaj = d.Mesaj ?? "", Bakiye = Temiz(d.Bakiye), Durum = "Gönderildi", Not = Temiz(d.Not) };
                _depo.SubeGonderimleri.Add(kayit);
                if (kanal == "E-posta" && s.Eposta == "") s.Eposta = alici; if (kanal == "WhatsApp" && s.Telefon == "") s.Telefon = alici;
                Yaz(); return null;
            }
        }
        static SubeYil Y(string yil, decimal resmiFatura, decimal resmiOdeme, decimal digerOdeme = 0, decimal gercek = 0, decimal sermaye = 0, string not = "") => new SubeYil { Yil = yil, ResmiFatura = resmiFatura, ResmiOdeme = resmiOdeme, DigerOdeme = digerOdeme, GercekMalBedeli = gercek, Sermaye = sermaye, Not = not };
        /// <summary>Tohum: satış temsilcilerinin şube carileri tabloları (PDF, Kasım 2026). Yalnız ilk kurulumda; sonrası ekrandan düzenlenir.</summary>
        static void SubeTohumUygula()
        {
            if (_depo.Subeler.Count == 0)
            {
                var ak = "Ali Kemal Gündoğan"; var er = "Erman Kaval"; var k = "Şube carileri tablosu (PDF, Kasım 2026)";
                var L = new List<SubeCari>
                {
                    new SubeCari { Id = 1, Kod = "SB-01", Sube = "Fas · Boya", Ulke = "Fas", Sirket = "Uras Kimya", UrunGrubu = "Boya", Temsilci = ak, ParaBirimi = "USD", Kaynak = k, Yillar = new List<SubeYil> { Y("2017", 75870.98m, 0), Y("2018", 93845.70m, 62959, 13183), Y("2019", 195545.75m, 190423), Y("2020", 154126.51m, 159344, 1250), Y("2021", 232612.79m, 147200, 113820), Y("2022", 313522.47m, 249450.22m, 46333), Y("2023", 323415.44m, 263433, 16500), Y("2024", 445396.48m, 326455, 10562), Y("2025", 228429.22m, 447250), Y("2026", 185032.66m, 168000) } },
                    new SubeCari { Id = 2, Kod = "SB-02", Sube = "Fas · Kağıt", Ulke = "Fas", Sirket = "Avrupa Paper", UrunGrubu = "Kağıt", Temsilci = ak, ParaBirimi = "USD", Kaynak = k, Yillar = new List<SubeYil> { Y("2019", 70091.50m, 9716.01m), Y("2020", 129282, 123249), Y("2021", 208256, 247365), Y("2022", 267285, 201199), Y("2023", 323065, 295403), Y("2024", 133480, 139334), Y("2025", 22176, 99150) } },
                    new SubeCari { Id = 3, Kod = "SB-03", Sube = "Tunus · Boya + Kağıt", Ulke = "Tunus", Sirket = "Uras Kimya", UrunGrubu = "Boya + Kağıt", Temsilci = ak, ParaBirimi = "USD", Kaynak = k, Yillar = new List<SubeYil> { Y("2023", 83683.99m, 0), Y("2024", 123735.15m, 39983), Y("2025", 40931, 35748.87m, 0, 0, 0, "Kaynakta resmi ödeme 35.749 (yuvarlak); toplam 75.769,87 ile uyum için 35.748,87"), Y("2026", 81690, 38) } },
                    new SubeCari { Id = 4, Kod = "SB-04", Sube = "Cezayir · Boya + Kağıt", Ulke = "Cezayir", Sirket = "Uras Kimya", UrunGrubu = "Boya + Kağıt", Temsilci = ak, ParaBirimi = "USD", Kaynak = k, Yillar = new List<SubeYil> { Y("2022", 41996.56m, 0), Y("2023", 318575, 184945), Y("2024", 420830, 391978), Y("2025", 305626, 298750) } },
                    new SubeCari { Id = 5, Kod = "SB-05", Sube = "ABD", Ulke = "ABD", Sirket = "", UrunGrubu = "", Temsilci = er, ParaBirimi = "USD", Kaynak = k, Not = "Kaynak tabloda şirket belirtilmemiş; Resmi Fatura ve Gerçek Mal Bedeli ayrı (bakiye gerçek mal bedeline göre).", Yillar = new List<SubeYil> { Y("2024", 215959, 0, 0, 146239), Y("2025", 0, 0, 0, 0, 0, "Hareket yok") } },
                    new SubeCari { Id = 6, Kod = "SB-06", Sube = "Meksika", Ulke = "Meksika", Sirket = "", UrunGrubu = "", Temsilci = er, ParaBirimi = "USD", Kaynak = k, Not = "Kaynak tabloda şirket belirtilmemiş; bakiye gerçek mal bedeline göre.", Yillar = new List<SubeYil> { Y("2021", 16745, 16745, 0, 16745), Y("2022", 955, 0, 0, 955), Y("2023", 112331, 0, 0, 112831), Y("2024", 398450, 23634, 0, 398450), Y("2025", 384746, 289930, 0, 362450), Y("2026", 136974, 99989, 0, 136974) } },
                    new SubeCari { Id = 7, Kod = "SB-07", Sube = "Mısır", Ulke = "Mısır", Sirket = "", UrunGrubu = "", Temsilci = er, ParaBirimi = "USD", Kaynak = k,
                        Not = "DİKKAT: kaynak tablodaki ara toplamlar sütunlarla uyuşmuyor (PDF devir −74.321 $; sütunlardan hesaplanan devir −1.056.592 $). Gizli sütun / düzeltme olabilir; yıl satırındaki Düzeltme alanıyla düzeltilmeli.",
                        Yillar = new List<SubeYil> { Y("2016-2019", 0, 1420957, 942900, 3506300, 0, "PDF: ödeme − satış −1.038.663 (sütunlardan −1.142.443)"), Y("2020", 0, 965502.47m, 882000, 684510, 0, "PDF: toplam ödeme 1.783.738, ödeme − satış +1.502.732 (sütunlardan 1.847.502,47 / +1.162.992,47)"), Y("2021-2022", 0, 1601629, 890500, 3372697, 0, "PDF: toplam ödeme 2.078.113, ödeme − satış −645.548 (sütunlardan 2.492.129 / −880.568)"), Y("2023", 0, 291341, 355450, 820065, 0, "PDF: toplam ödeme 548.508, ödeme − satış +33.976 (sütunlardan 646.791 / −173.274)"), Y("2024", 0, 215821, 155450, 432533, 0, "PDF: ödeme − satış +35.221 (sütunlardan −61.262)"), Y("2025", 0, 355500, 155650, 502213), Y("2026", 0, 237922, 253000, 461897) } },
                    new SubeCari { Id = 8, Kod = "SB-08", Sube = "Portekiz", Ulke = "Portekiz", Sirket = "", UrunGrubu = "", Temsilci = "", ParaBirimi = "EUR", Kaynak = k, Not = "Kaynak tabloda şirket ve satış temsilcisi belirtilmemiş. 2019 sermaye 61.130 €.", Yillar = new List<SubeYil> { Y("2019", 308423, 0, 0, 0, 61130), Y("2020", 594350, 0, 360224), Y("2021", 167350, 102989), Y("2022", 245450, 40375), Y("2023", 278350, 214745), Y("2024", 320057, 316441), Y("2025", 151442, 141166) } },
                    new SubeCari { Id = 9, Kod = "SB-09", Sube = "Özbekistan", Ulke = "Özbekistan", Sirket = "Uras Kimya", UrunGrubu = "", Temsilci = "", Sorumlu = "Vusal", ParaBirimi = "USD", Kaynak = k, Not = "Vusal tablosu; şirket kaynakta yalnız 2026 bloğunda (Selvi Kimya) yazılı, 2022–2025 Uras Kimya varsayıldı.", Yillar = new List<SubeYil> { Y("2022", 66707, 30004), Y("2023", 145801, 107120), Y("2024", 129985, 84179), Y("2025", 246781, 225117) } },
                    new SubeCari { Id = 10, Kod = "SB-10", Sube = "Özbekistan · Selvi Kimya", Ulke = "Özbekistan", Sirket = "Selvi Kimya", UrunGrubu = "", Temsilci = "", Sorumlu = "Vusal", ParaBirimi = "USD", Kaynak = k, Yillar = new List<SubeYil> { Y("2026", 218782, 141565) } },
                    new SubeCari { Id = 11, Kod = "SB-11", Sube = "Dubai", Ulke = "BAE", Sirket = "", UrunGrubu = "", Temsilci = "", ParaBirimi = "USD", Kaynak = "Kullanıcı (11 Kas 2026)", Not = "Yıl satırları girilecek." }
                };
                foreach (var s in L) s.Olusturma = DateTime.Today;
                _depo.Subeler = L;
            }
            _depo.SubeTohumSurumu = SubeTohumSurum; Yaz();
            Console.WriteLine($"[İBKB] şube tohumu uygulandı: {_depo.Subeler.Count} şube");
        }
        static List<object> SubePaket()
        {
            return _depo.Subeler.OrderBy(s => s.Ulke, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false)).ThenBy(s => s.Id).Select(s =>
            {
                var yl = SubeYillarSirali(s); decimal devir = 0;
                var yillar = yl.Select(y => { var f = SubeFark(y); devir = R2(devir + f); return new { y.Yil, y.Sermaye, y.ResmiFatura, y.GercekMalBedeli, y.IadeFaturasi, y.Komisyon, y.ResmiOdeme, y.DigerOdeme, y.Nakliye, y.Diger, y.Duzeltme, y.Not, satisEsas = SubeSatisEsas(y), toplamOdeme = SubeToplamOdeme(y), fark = f, devir }; }).ToList();
                var g = _depo.SubeGonderimleri.Where(x => x.SubeId == s.Id).OrderByDescending(x => x.Tarih).ToList(); var son = g.FirstOrDefault();
                return (object)new
                {
                    s.Id, s.Kod, s.Sube, s.Ulke, s.Sirket, s.UrunGrubu, s.Temsilci, s.Sorumlu, pb = s.ParaBirimi, s.Eposta, s.Telefon, s.Adres, s.Not, s.Kaynak, olusturma = D(s.Olusturma), guncelleme = s.Guncelleme?.ToString("yyyy-MM-dd HH:mm"), s.Guncelleyen,
                    yillar, ilkYil = yl.FirstOrDefault()?.Yil ?? "", sonYil = yl.LastOrDefault()?.Yil ?? "", yilSayisi = yl.Count,
                    sermaye = yl.Sum(y => y.Sermaye), resmiFatura = yl.Sum(y => y.ResmiFatura), gercekMalBedeli = yl.Sum(y => y.GercekMalBedeli), satisEsas = yl.Sum(SubeSatisEsas), iadeFaturasi = yl.Sum(y => y.IadeFaturasi), komisyon = yl.Sum(y => y.Komisyon), resmiOdeme = yl.Sum(y => y.ResmiOdeme), digerOdeme = yl.Sum(y => y.DigerOdeme), nakliye = yl.Sum(y => y.Nakliye), diger = yl.Sum(y => y.Diger), duzeltme = yl.Sum(y => y.Duzeltme), toplamOdeme = yl.Sum(SubeToplamOdeme), bakiye = SubeBakiye(s),
                    sonYilFark = yl.Count > 0 ? SubeFark(yl.Last()) : 0,
                    gonderimSayisi = g.Count, sonGonderim = son == null ? null : new { son.Id, son.Kanal, son.Alici, tarih = son.Tarih.ToString("yyyy-MM-dd HH:mm"), son.Kullanici, son.Donem, son.Durum, son.Bakiye, son.Not },
                    gonderimler = g.Select(x => new { x.Id, x.Kanal, x.Alici, tarih = x.Tarih.ToString("yyyy-MM-dd HH:mm"), x.Kullanici, x.Donem, x.Konu, x.Mesaj, x.Bakiye, x.Durum, x.Not }).ToList(),
                    kalemler = s.Kalemler.OrderBy(k => k.Yil).ThenBy(k => k.Tarih ?? DateTime.MinValue).ThenBy(k => k.Id).Select(k => new { k.Id, k.Yil, k.Alan, tarih = D(k.Tarih), k.BelgeNo, k.Aciklama, k.Tutar, k.Not, k.Kaynak, olusturma = k.Olusturma.ToString("yyyy-MM-dd"), k.Olusturan }).ToList()
                };
            }).ToList();
        }

        static void TohumUygula()
        {
            var yol = TohumDosya; if (yol == null || !File.Exists(yol)) { Console.WriteLine("[İBKB] tohum dosyası yok: " + yol); return; }
            try
            {
                var t = JsonSerializer.Deserialize<IbkbTohum>(File.ReadAllText(yol), Secenek); if (t == null) return;
                int id = _depo.Dosyalar.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1, ek = 0;
                foreach (var d in t.Dosyalar ?? new List<IbkbDosya>())
                {
                    if (string.IsNullOrWhiteSpace(d.Anahtar) || _depo.Dosyalar.Any(x => x.Anahtar == d.Anahtar)) continue;
                    d.Id = id++; d.Dablar ??= new List<IbkbDab>(); d.Olusturma = DateTime.Today; d.Olusturan = "Excel Tohumu"; if (string.IsNullOrWhiteSpace(d.Kaynak)) d.Kaynak = t.Kaynak;
                    d.DabDurumu = Sec(d.DabDurumu, DabDurumlari, "Bekliyor"); if (d.UlkeOran <= 0) d.UlkeOran = 100m;
                    _depo.Dosyalar.Add(d); ek++;
                }
                // sürüm 2: Excel'deki bağlanan DAB satırları → banka tahsilatı kaydı (Eşleşti); yalnız hiç tahsilat yokken
                int ekT = 0;
                if (_depo.TohumSurumu < 2 && _depo.Tahsilatlar.Count == 0)
                {
                    int idT = 1;
                    foreach (var d in _depo.Dosyalar) foreach (var p in d.Dablar)
                        _depo.Tahsilatlar.Add(new IhracatTahsilat { Id = idT++, Sirket = d.Sirket, Banka = p.Banka ?? "", Tarih = p.Tarih, Gonderen = d.Musteri, MusteriKodu = d.MusteriKodu, Tutar = p.Tutar, Doviz = p.Doviz, Referans = p.No ?? "", Aciklama = p.Not ?? "", Kaynak = "Excel (Bağlanan DAB)", Durum = "Eşleşti", DosyaId = d.Id, EslestirmeTarihi = DateTime.Today, Eslestiren = "Excel Tohumu", Olusturma = DateTime.Today, Olusturan = "Excel Tohumu" });
                    ekT = _depo.Tahsilatlar.Count;
                }
                _depo.TohumSurumu = TohumSurum; Yaz();
                Console.WriteLine($"[İBKB] tohum uygulandı: {ek} dosya, {ekT} tahsilat ({t.Kaynak})");
            }
            catch (Exception ex) { Console.WriteLine("[İBKB] tohum uygulanamadı: " + ex.Message); }
        }

        // ---------- hesaplar ----------
        public static bool Muaf(IbkbDosya d) => d.DabDurumu == "Gerek Yok" || d.DabDurumu == "İptal";
        public static decimal Gereken(IbkbDosya d) => Muaf(d) ? 0 : R2(d.Tutar * (d.UlkeOran <= 0 ? 100m : d.UlkeOran) / 100m);
        /// <summary>Getirilen: bağlı DAB satırlarının aynı dövizdeki toplamı; satır yoksa duruma göre (Yapıldı / Peşin → gereken, Kısmi → gereken − kalan).</summary>
        public static decimal Getirilen(IbkbDosya d)
        {
            if (Muaf(d)) return 0;
            var ayni = d.Dablar.Where(x => string.Equals(x.Doviz, d.Doviz, StringComparison.OrdinalIgnoreCase)).Sum(x => x.Tutar);
            if (d.DabDurumu == "Yapıldı" || d.DabDurumu == "Peşin Yapıldı") return Gereken(d);   // banka DAB'ı tamamlandı: gereken tutarın tamamı getirilmiş sayılır
            if (d.DabDurumu == "Kısmi") return d.DabKalan > 0 ? R2(Math.Max(0, d.Tutar - d.DabKalan)) : R2(ayni);   // bankanın "X kaldı" bildirimi beyanname tutarına göredir: getirilen = tutar − kalan
            return R2(ayni);
        }
        public static decimal Kalan(IbkbDosya d) => Muaf(d) || d.DabDurumu == "Yapıldı" || d.DabDurumu == "Peşin Yapıldı" ? 0 : R2(Math.Max(0, Gereken(d) - Getirilen(d)));
        // ---- gerçek ihracat bedeli (proforma / fatura) ↔ beyanname farkı (11 Kas 2026)
        public static decimal GercekBedelEtkin(IbkbDosya d) => d.GercekBedel > 0 ? d.GercekBedel : d.Tutar;
        public static decimal BedelFarki(IbkbDosya d) => d.GercekBedel > 0 ? R2(d.Tutar - d.GercekBedel) : 0;   // + beyanname fazlası · − beyanname eksiği
        public static decimal GerekenGercek(IbkbDosya d) => Muaf(d) ? 0 : R2(GercekBedelEtkin(d) * (d.UlkeOran <= 0 ? 100m : d.UlkeOran) / 100m);
        public static decimal KalanGercek(IbkbDosya d) => Muaf(d) || d.DabDurumu == "Yapıldı" || d.DabDurumu == "Peşin Yapıldı" ? 0 : R2(Math.Max(0, GerekenGercek(d) - Getirilen(d)));
        public static decimal BeyannameFazlasi(IbkbDosya d) => Muaf(d) ? 0 : R2(Math.Max(0, Gereken(d) - GerekenGercek(d)));   // hiç tahsil edilemeyecek kısım (terkin / beyanname düzeltme konusu)
        public static DateTime? Vade(IbkbDosya d) => d.Baslangic?.AddDays(VadeGun);
        public static int? KalanGun(IbkbDosya d) => Vade(d).HasValue ? (int?)(Vade(d).Value.Date - DateTime.Today).Days : null;
        public static string Takip(IbkbDosya d)
        {
            if (d.DabDurumu == "İptal") return "İptal"; if (d.DabDurumu == "Gerek Yok") return "Muaf";
            if (d.DabDurumu == "Yapıldı" || d.DabDurumu == "Peşin Yapıldı") return "Kapandı";
            if (BedelFarki(d) > 0 && KalanGercek(d) <= 0.005m && Kalan(d) > 0.005m) return "Bedel Farkı";   // gerçek bedel tamamen geldi, yalnız beyanname fazlası açık
            if (d.DabDurumu == "Kısmi") return "Kısmi";
            var g = KalanGun(d); if (g.HasValue && g.Value < 0) return "Vadesi Geçti"; if (g.HasValue && g.Value <= 30) return "Vade Yaklaşıyor"; return "Açık";
        }

        // ---------- kayıt ----------
        public static string Kaydet(IbkbIstek d, string kullanici, out IbkbDosya kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string sirket = Temiz(d.Sirket), no = Temiz(d.DosyaNo);
            if (sirket == "") return "Şirket seçilmelidir."; if (no == "") return "Dosya no zorunludur.";
            if (d.Tutar < 0 || d.DabKalan < 0 || d.GercekBedel < 0) return "Tutarlar negatif olamaz."; if (d.UlkeOran < 0 || d.UlkeOran > 100) return "Ülke gerekliliği 0 – 100 arasında olmalıdır.";
            lock (Kilit)
            {
                var x = d.Id > 0 ? _depo.Dosyalar.FirstOrDefault(y => y.Id == d.Id) : null; if (d.Id > 0 && x == null) return "Kayıt bulunamadı.";
                if (_depo.Dosyalar.Any(y => y.Id != d.Id && y.Sirket == sirket && string.Equals(y.DosyaNo, no, StringComparison.OrdinalIgnoreCase))) return $"{sirket} · {no} dosya numarası zaten kayıtlı.";
                if (x == null) { x = new IbkbDosya { Id = _depo.Dosyalar.Select(y => y.Id).DefaultIfEmpty(0).Max() + 1, Olusturma = DateTime.Today, Olusturan = kullanici ?? "", Kaynak = "Ekran" }; _depo.Dosyalar.Add(x); }
                x.Sirket = sirket; x.DosyaNo = no; x.Anahtar = sirket + "|" + no; x.Yil = d.Yil > 0 ? d.Yil : (int.TryParse(no.Split('-')[0], out var y0) && y0 > 2000 ? y0 : DateTime.Today.Year);
                x.Aciklama = Temiz(d.Aciklama); x.MusteriKodu = Temiz(d.MusteriKodu); x.Musteri = Temiz(d.Musteri); x.Ulke = Temiz(d.Ulke); x.FaturaNo = Temiz(d.FaturaNo); x.SiparisNo = Temiz(d.SiparisNo); x.Temsilci = Temiz(d.Temsilci);
                x.Baslangic = Tarih(d.Baslangic); x.Kapanis = Tarih(d.Kapanis); x.BeyannameNo = Temiz(d.BeyannameNo); x.Doviz = Temiz(d.Doviz).ToUpperInvariant(); x.Tutar = R2(d.Tutar);
                x.GercekBedel = Math.Abs(R2(d.GercekBedel) - x.Tutar) < 0.005m ? 0 : R2(d.GercekBedel); x.FarkNedeni = x.GercekBedel > 0 ? Temiz(d.FarkNedeni) : "";   // beyanname ile aynıysa fark yok
                x.UlkeOran = d.UlkeOran <= 0 ? 100m : d.UlkeOran; x.DabDurumu = Sec(d.DabDurumu, DabDurumlari, "Bekliyor"); x.DabBanka = Temiz(d.DabBanka); x.DabKalan = R2(d.DabKalan); x.DabNotu = Temiz(d.DabNotu);
                x.Dablar = (d.Dablar ?? new List<IbkbDabIstek>()).Where(p => p != null && p.Tutar > 0).Select(p => new IbkbDab { Tutar = R2(p.Tutar), Doviz = Temiz(p.Doviz).ToUpperInvariant() == "" ? x.Doviz : Temiz(p.Doviz).ToUpperInvariant(), Tarih = Tarih(p.Tarih), Banka = Temiz(p.Banka), No = Temiz(p.No), Not = Temiz(p.Not) }).ToList();
                x.Not = Temiz(d.Not); x.Guncelleme = DateTime.Now; x.Guncelleyen = kullanici ?? "";
                // ülke kuralı: Ek-2 ülkesi → bedel getirme zorunlu değil (oran 0, Gerek Yok); Ek-3 → %50 (kullanıcı oran girmediyse)
                if (x.Ulke != "" && UlkeMuaf.Contains(x.Ulke)) { x.UlkeOran = 0; if (x.DabDurumu == "Bekliyor") { x.DabDurumu = "Gerek Yok"; x.DabNotu = Temiz("Ek-2 ülkesi (bedel getirme zorunlu değil) · otomatik " + x.DabNotu); } }
                else if (x.Ulke != "" && UlkeYarim.Contains(x.Ulke) && d.UlkeOran >= 100) x.UlkeOran = 50;
                // eşik: ABD doları karşılığı 15.000 ve altı → DAB / İBKB gerekmez (otomatik; kullanıcı başka durum seçtiyse dokunulmaz)
                if (x.DabDurumu == "Bekliyor" && EsikAlti(x)) { x.DabDurumu = "Gerek Yok"; x.DabNotu = Temiz($"Eşik altı (≤ {EsikUsd:N0} $ karşılığı) · otomatik " + x.DabNotu); }
                Yaz(); kayit = x;
            }
            return null;
        }
        public static string Sil(int id) { Yukle(); lock (Kilit) { var x = _depo.Dosyalar.FirstOrDefault(y => y.Id == id); if (x == null) return "Kayıt bulunamadı."; if (_depo.Tahsilatlar.Any(t => t.DosyaId == id)) return "Bu dosyaya bağlı banka tahsilatı var; önce eşleşmeyi kaldırın."; _depo.Dosyalar.Remove(x); Yaz(); } return null; }

        // ---------- banka tahsilatları (11 Kas 2026: "tahsilat raporu bankalardan otomatik geliyor; hangi ihracat belge numarası ile eşleşecek seçimi") ----------
        public static string TahsilatKaydet(IhracatTahsilatIstek d, string kullanici, out IhracatTahsilat kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            if (Temiz(d.Sirket) == "") return "Şirket seçilmelidir."; if (d.Tutar <= 0) return "Tutar sıfırdan büyük olmalıdır.";
            lock (Kilit)
            {
                var x = d.Id > 0 ? _depo.Tahsilatlar.FirstOrDefault(y => y.Id == d.Id) : null; if (d.Id > 0 && x == null) return "Kayıt bulunamadı.";
                if (x != null && x.Durum != "Eşleşmedi") return "Eşleşmiş tahsilat düzenlenemez; önce eşleşmeyi kaldırın.";
                if (x == null) { x = new IhracatTahsilat { Id = _depo.Tahsilatlar.Select(y => y.Id).DefaultIfEmpty(0).Max() + 1, Olusturma = DateTime.Today, Olusturan = kullanici ?? "", Kaynak = "Elle" }; _depo.Tahsilatlar.Add(x); }
                x.Sirket = Temiz(d.Sirket); x.Banka = Temiz(d.Banka); x.Tarih = Tarih(d.Tarih); x.Gonderen = Temiz(d.Gonderen); x.MusteriKodu = Temiz(d.MusteriKodu); x.Tutar = R2(d.Tutar); x.Doviz = Temiz(d.Doviz).ToUpperInvariant() == "" ? "USD" : Temiz(d.Doviz).ToUpperInvariant(); x.Referans = Temiz(d.Referans); x.Aciklama = Temiz(d.Aciklama);
                Yaz(); kayit = x;
            }
            return null;
        }
        public static string TahsilatSil(int id) { Yukle(); lock (Kilit) { var x = _depo.Tahsilatlar.FirstOrDefault(y => y.Id == id); if (x == null) return "Kayıt bulunamadı."; if (x.Durum != "Eşleşmedi") return "Eşleşmiş tahsilat silinemez; önce eşleşmeyi kaldırın."; _depo.Tahsilatlar.Remove(x); Yaz(); } return null; }
        /// <summary>Tahsilat ↔ ihracat dosyası eşleşmesi: dosyaya DAB satırı yazılır, DAB durumu güncellenir (gereken karşılandıysa Yapıldı, yoksa Kısmi); istenirse İBKB belgesi üretilir.</summary>
        public static string Eslestir(EslestirIstek d, string kullanici, out IbkbBelge belge)
        {
            belge = null; if (d == null) return "Veri yok."; Yukle();
            lock (Kilit)
            {
                var t = _depo.Tahsilatlar.FirstOrDefault(y => y.Id == d.TahsilatId); if (t == null) return "Tahsilat bulunamadı.";
                if (t.Durum != "Eşleşmedi") return "Tahsilat zaten eşleşmiş.";
                var x = _depo.Dosyalar.FirstOrDefault(y => y.Id == d.DosyaId); if (x == null) return "İhracat dosyası bulunamadı.";
                if (x.DabDurumu == "İptal") return "İptal dosyaya eşleştirilemez.";
                x.Dablar.Add(new IbkbDab { Tutar = t.Tutar, Doviz = t.Doviz, Tarih = t.Tarih, Banka = t.Banka, No = t.Referans, Not = "Banka tahsilatı #" + t.Id + (t.Gonderen != "" ? " · " + t.Gonderen : "") });
                if (string.IsNullOrEmpty(x.DabBanka)) x.DabBanka = t.Banka;
                if (x.DabDurumu == "Bekliyor" || x.DabDurumu == "Kısmi" || x.DabDurumu == "Gerek Yok") { x.DabKalan = 0; x.DabDurumu = "Kısmi"; if (Kalan(x) <= 0.005m) x.DabDurumu = "Yapıldı"; }
                x.Guncelleme = DateTime.Now; x.Guncelleyen = kullanici ?? "";
                t.Durum = "Eşleşti"; t.DosyaId = x.Id; t.EslestirmeTarihi = DateTime.Today; t.Eslestiren = kullanici ?? "";
                if (d.IbkbOlustur)
                {
                    var kart = string.IsNullOrEmpty(x.MusteriKodu) ? null : CariKartOrnek.KartBul(x.MusteriKodu);
                    int yil = DateTime.Today.Year, sira = _depo.Belgeler.Count(b => b.Tarih.Year == yil) + 1;
                    belge = new IbkbBelge
                    {
                        Id = _depo.Belgeler.Select(b => b.Id).DefaultIfEmpty(0).Max() + 1, No = $"İBKB-{yil}-{sira:0000}", Tarih = DateTime.Today, Sirket = x.Sirket, Banka = t.Banka,
                        DosyaId = x.Id, DosyaNo = x.DosyaNo, BeyannameNo = x.BeyannameNo, FaturaNo = x.FaturaNo, MusteriKodu = x.MusteriKodu, Musteri = x.Musteri, Ulke = x.Ulke != "" ? x.Ulke : (kart?.Ulke ?? ""),
                        BeyannameTutar = x.Tutar, BeyannameDoviz = x.Doviz, Tutar = t.Tutar, Doviz = t.Doviz, TahsilatId = t.Id, TahsilatRef = t.Referans, TahsilatTarihi = t.Tarih, FiiliIhrac = x.Baslangic,
                        Durum = "Hazırlandı", Olusturma = DateTime.Now, Olusturan = kullanici ?? ""
                    };
                    _depo.Belgeler.Add(belge); t.Durum = "İBKB Düzenlendi"; t.IbkbBelgeId = belge.Id;
                }
                Yaz();
            }
            return null;
        }
        /// <summary>Eşleşmeyi kaldır: dosyadaki DAB satırı silinir, belge iptal olur, tahsilat yeniden Eşleşmedi.</summary>
        public static string EslesmeKaldir(int tahsilatId, string kullanici)
        {
            Yukle();
            lock (Kilit)
            {
                var t = _depo.Tahsilatlar.FirstOrDefault(y => y.Id == tahsilatId); if (t == null) return "Tahsilat bulunamadı."; if (t.Durum == "Eşleşmedi") return "Tahsilat zaten eşleşmemiş.";
                var x = _depo.Dosyalar.FirstOrDefault(y => y.Id == t.DosyaId);
                if (x != null) { x.Dablar.RemoveAll(p => p.Not != null && p.Not.StartsWith("Banka tahsilatı #" + t.Id)); if (x.DabDurumu == "Yapıldı" || x.DabDurumu == "Kısmi") { x.DabDurumu = x.Dablar.Count == 0 ? "Bekliyor" : (Kalan(x) <= 0.005m ? "Yapıldı" : "Kısmi"); } x.Guncelleme = DateTime.Now; x.Guncelleyen = kullanici ?? ""; }
                var b = _depo.Belgeler.FirstOrDefault(y => y.Id == t.IbkbBelgeId); if (b != null && b.Durum != "Banka Onayladı") b.Durum = "İptal";
                t.Durum = "Eşleşmedi"; t.DosyaId = 0; t.IbkbBelgeId = 0; t.EslestirmeTarihi = null; t.Eslestiren = "";
                Yaz();
            }
            return null;
        }
        public static string BelgeDurum(IbkbBelgeIstek d, string kullanici)
        {
            if (d == null) return "Veri yok."; Yukle();
            lock (Kilit)
            {
                var b = _depo.Belgeler.FirstOrDefault(y => y.Id == d.Id); if (b == null) return "Belge bulunamadı.";
                b.Durum = Sec(d.Durum, BelgeDurumlari, b.Durum); b.BankaIbkbNo = Temiz(d.BankaIbkbNo); b.BankaTarihi = Tarih(d.BankaTarihi); b.Not = Temiz(d.Not);
                Yaz();
            }
            return null;
        }
        /// <summary>Yalnız İptal durumundaki belge silinir (düzenlenmiş / bankaya gitmiş belge iz bırakır).</summary>
        public static string BelgeSil(int id) { Yukle(); lock (Kilit) { var b = _depo.Belgeler.FirstOrDefault(y => y.Id == id); if (b == null) return "Belge bulunamadı."; if (b.Durum != "İptal") return "Yalnız iptal edilmiş belge silinebilir."; _depo.Belgeler.Remove(b); foreach (var t in _depo.Tahsilatlar.Where(t => t.IbkbBelgeId == id)) t.IbkbBelgeId = 0; Yaz(); } return null; }
        /// <summary>Cari bakiye kontrolü: her kayıt tarihçede kalır; son kayıt ekranda gösterilir.</summary>
        public static string BakiyeKontrolKaydet(IbkbBakiyeKontrolIstek d, string kullanici, out IbkbBakiyeKontrol kayit, out IbkbGorev gorev)
        {
            kayit = null; gorev = null; if (d == null || Temiz(d.CariKodu) == "") return "Cari kodu gerekli."; Yukle();
            lock (Kilit)
            {
                kayit = new IbkbBakiyeKontrol { Id = _depo.BakiyeKontrolleri.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1, CariKodu = Temiz(d.CariKodu), Tarih = DateTime.Now, Kullanici = kullanici ?? "", Durum = Sec(d.Durum, BakiyeKontrolDurumlari, "Uyumlu"), Not = Temiz(d.Not), BakiyeOzet = Temiz(d.BakiyeOzet), FarkOzet = Temiz(d.FarkOzet) };
                _depo.BakiyeKontrolleri.Add(kayit);
                if (d.Gorev != null && Temiz(d.Gorev.Atanan) != "")
                {   // kontrolle birlikte düzeltme görevi ataması
                    d.Gorev.Islem = "ata"; d.Gorev.CariKodu = kayit.CariKodu; if (Temiz(d.Gorev.Aciklama) == "") d.Gorev.Aciklama = kayit.Not;
                    var h = GorevIslem(d.Gorev, kullanici, kayit, out gorev); if (h != null) { _depo.BakiyeKontrolleri.Remove(kayit); kayit = null; return h; }
                }
                Yaz();
            }
            return null;
        }
        // ---------- cari bakiye düzeltme görevleri (12 Kas 2026)
        static string Kim(string kod) => string.IsNullOrEmpty(kod) ? "" : OrnekVeri.KullaniciAdi(kod);
        public static IbkbGorev GorevBul(int id) { Yukle(); lock (Kilit) return _depo.Gorevler.FirstOrDefault(g => g.Id == id); }
        public static bool GorevAcik(IbkbGorev g) => g.Durum == "Atandı" || g.Durum == "Düzeltildi" || g.Durum == "İade Edildi";
        public static string GorevKaydet(IbkbGorevIstek d, string kullanici, out IbkbGorev kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            lock (Kilit) { var h = GorevIslem(d, kullanici, null, out kayit); if (h == null) Yaz(); return h; }
        }
        /// <summary>Görev akışı (Kilit içinde çağrılır): ata → Atandı; duzeltildi (atanan) → Düzeltildi; onayla (muhasebeci, kendi düzeltmesini onaylayamaz) → Onaylandı; iade → İade Edildi (yeniden düzeltilir); guncelle → yeniden atama / termin; iptal.</summary>
        static string GorevIslem(IbkbGorevIstek d, string kullanici, IbkbBakiyeKontrol kontrol, out IbkbGorev g)
        {
            g = null; string islem = Temiz(d.Islem).ToLowerInvariant(), kim = Kim(kullanici), not = Temiz(d.Not); var simdi = DateTime.Now;
            if (islem == "ata" || d.Id == 0)
            {
                string kod = Temiz(d.CariKodu); if (kod == "") return "Cari kodu gerekli."; if (Temiz(d.Atanan) == "") return "Görevi yapacak kişi seçilmeli.";
                var c = MutabakatOrnek.Bul(kod); var k = c?.Kart ?? CariKartOrnek.KartBul(kod);
                var ozet = kontrol?.BakiyeOzet ?? _depo.BakiyeKontrolleri.Where(x => x.CariKodu == kod).OrderByDescending(x => x.Tarih).FirstOrDefault()?.BakiyeOzet ?? "";
                var fark = kontrol?.FarkOzet ?? _depo.BakiyeKontrolleri.Where(x => x.CariKodu == kod).OrderByDescending(x => x.Tarih).FirstOrDefault()?.FarkOzet ?? "";
                g = new IbkbGorev
                {
                    Id = _depo.Gorevler.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1, CariKodu = kod, CariAd = k?.KisaAd ?? kod, Sirket = k?.Sirket ?? "", Konu = Temiz(d.Konu) == "" ? $"{kod} {k?.KisaAd} · cari bakiye düzeltme" : Temiz(d.Konu), Aciklama = Temiz(d.Aciklama),
                    BakiyeOzet = ozet, FarkOzet = fark, Atanan = Temiz(d.Atanan), AtananKod = Temiz(d.AtananKod), AtananEposta = Temiz(d.AtananEposta), Atayan = kim, Tarih = simdi, Termin = Tarih(d.Termin), Oncelik = Sec(d.Oncelik, GorevOncelikleri, "Normal"), Durum = "Atandı",
                    KontrolId = kontrol?.Id ?? 0, GonderimId = d.GonderimId
                };
                g.Gecmis.Add(new IbkbGorevAdim { Tarih = simdi, Kullanici = kim, Islem = "Atandı → " + g.Atanan, Not = not });
                g.Bildirim = GorevBildir(g, kullanici, "Yeni görev"); _depo.Gorevler.Add(g); return null;
            }
            g = _depo.Gorevler.FirstOrDefault(x => x.Id == d.Id); if (g == null) return "Görev bulunamadı.";
            switch (islem)
            {
                case "duzeltildi":
                    if (g.Durum != "Atandı" && g.Durum != "İade Edildi") return $"Görev {g.Durum} durumunda; düzeltildi işaretlenemez.";
                    g.Durum = "Düzeltildi"; g.DuzeltmeTarihi = simdi; g.Duzelten = kim; g.DuzeltmeNotu = not; g.Gecmis.Add(new IbkbGorevAdim { Tarih = simdi, Kullanici = kim, Islem = "Düzeltildi · onay bekliyor", Not = not }); return null;
                case "onayla":
                    if (g.Durum != "Düzeltildi") return "Önce atanan kişi düzeltmeyi işaretlemeli (Düzeltildi).";
                    if (!string.IsNullOrEmpty(g.AtananKod) && string.Equals(g.AtananKod, kullanici, StringComparison.OrdinalIgnoreCase)) return "Kendi düzeltmenizi onaylayamazsınız; muhasebe onayı başka kullanıcıdan gelmeli.";
                    g.Durum = "Onaylandı"; g.OnayTarihi = simdi; g.Onaylayan = kim; g.OnayNotu = not; g.Gecmis.Add(new IbkbGorevAdim { Tarih = simdi, Kullanici = kim, Islem = "Onaylandı (muhasebe)", Not = not }); return null;
                case "iade":
                    if (g.Durum != "Düzeltildi") return "Yalnız Düzeltildi durumundaki görev iade edilir."; if (not == "") return "İade nedeni zorunludur.";
                    g.Durum = "İade Edildi"; g.IadeSayisi++; g.OnayNotu = not; g.Gecmis.Add(new IbkbGorevAdim { Tarih = simdi, Kullanici = kim, Islem = $"İade edildi ({g.IadeSayisi}.)", Not = not }); g.Bildirim = GorevBildir(g, kullanici, "Görev iade edildi"); return null;
                case "iptal":
                    if (g.Durum == "Onaylandı") return "Onaylanmış görev iptal edilemez."; g.Durum = "İptal"; g.Gecmis.Add(new IbkbGorevAdim { Tarih = simdi, Kullanici = kim, Islem = "İptal", Not = not }); return null;
                case "guncelle":
                    if (!GorevAcik(g)) return "Kapanmış görev değiştirilemez.";
                    string eski = g.Atanan; if (Temiz(d.Atanan) != "") { g.Atanan = Temiz(d.Atanan); g.AtananKod = Temiz(d.AtananKod); g.AtananEposta = Temiz(d.AtananEposta); }
                    if (d.Termin != null) g.Termin = Tarih(d.Termin); if (!string.IsNullOrEmpty(d.Oncelik)) g.Oncelik = Sec(d.Oncelik, GorevOncelikleri, g.Oncelik); if (d.Aciklama != null) g.Aciklama = Temiz(d.Aciklama); if (d.Konu != null && Temiz(d.Konu) != "") g.Konu = Temiz(d.Konu);
                    if (g.Durum == "Düzeltildi" && g.Atanan != eski) g.Durum = "Atandı";
                    g.Gecmis.Add(new IbkbGorevAdim { Tarih = simdi, Kullanici = kim, Islem = g.Atanan != eski ? $"Yeniden atandı {eski} → {g.Atanan}" : "Güncellendi", Not = not }); if (g.Atanan != eski) g.Bildirim = GorevBildir(g, kullanici, "Görev size atandı"); return null;
                default: return "Geçersiz işlem.";
            }
        }
        /// <summary>Bildirim: atanan portal kullanıcısıysa mesaj kutusuna düşer; e-posta cihazdaki posta uygulamasıyla açılır (GorevEposta). Dönen metin kayıtta "Bildirim" alanı.</summary>
        static string GorevBildir(IbkbGorev g, string kullanici, string baslik)
        {
            var l = new List<string>();
            if (!string.IsNullOrEmpty(g.AtananKod) && OrnekVeri.Kullanicilar.Any(k => string.Equals(k.Kod, g.AtananKod, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    lock (OrnekVeri.Mesajlar) OrnekVeri.Mesajlar.Add(new OrnekVeri.Mesaj { Id = OrnekVeri.SonMesajId() + 1, Gonderen = string.IsNullOrEmpty(kullanici) ? "it02" : kullanici, Alici = g.AtananKod, Tarih = DateTime.Now, Metin = $"[{baslik}] {g.Konu}\n{g.Aciklama}\nBakiye: {g.BakiyeOzet} · fark: {g.FarkOzet}\nTermin: {(g.Termin.HasValue ? g.Termin.Value.ToString("dd.MM.yyyy") : "—")} · öncelik {g.Oncelik}\nGörev: /IhracatFinans?sekme=bakiye&gorev={g.Id}" });
                    l.Add("Portal mesajı → " + g.Atanan);
                }
                catch (Exception ex) { Console.WriteLine("[İBKB] görev mesajı yazılamadı: " + ex.Message); }
            }
            if (!string.IsNullOrEmpty(g.AtananEposta)) l.Add("E-posta → " + g.AtananEposta);
            return string.Join(" · ", l);
        }
        /// <summary>Atanan kişiye gidecek e-posta (konu, gövde); {portal} ekranda adresle değiştirilir.</summary>
        public static (string Konu, string Govde) GorevEposta(IbkbGorev g)
        {
            string konu = $"[Görev] {g.Konu} · termin {(g.Termin.HasValue ? g.Termin.Value.ToString("dd.MM.yyyy") : "—")}";
            string govde = $"Sayın {g.Atanan},\n\n{g.CariKodu} {g.CariAd}{(g.Sirket != "" ? " (" + g.Sirket + ")" : "")} carisinde bakiye düzeltme görevi size atandı.\n\nMizan bakiyesi: {g.BakiyeOzet}\nİBKB dosyalarıyla fark: {g.FarkOzet}\nYapılacak: {(g.Aciklama == "" ? "—" : g.Aciklama)}\nÖncelik: {g.Oncelik} · Termin: {(g.Termin.HasValue ? g.Termin.Value.ToString("dd.MM.yyyy") : "—")}\n\nDüzeltmeyi yaptıktan sonra görevi \"Düzeltildi\" olarak işaretleyin; muhasebe onayıyla kapanır:\n{{portal}}/IhracatFinans?sekme=bakiye&gorev={g.Id}\n\n{g.Atayan} · İhracat Finansal Takibi";
            return (konu, govde);
        }
        /// <summary>Atanabilecek kişiler: portal kullanıcıları (muhasebe / finans önde) + Konsolide İK Paneli aktif personeli (ad · bölüm · e-posta).</summary>
        static List<object> GorevKisiler()
        {
            var l = new List<(string ad, string kod, string bolum, string eposta, int sira)>();
            foreach (var k in OrnekVeri.Kullanicilar) l.Add((k.Ad, k.Kod, k.Yetki == "MUH" ? "Muhasebe" : k.Yetki == "FN" ? "Finans" : k.Yetki == "ST" ? "Satış" : k.Yetki == "A" ? "Yönetici" : "Portal", "", k.Yetki == "MUH" ? 0 : k.Yetki == "FN" ? 1 : 2));
            try
            {
                foreach (var p in IkPersonelMaster.Aktifler())
                {
                    if (string.IsNullOrWhiteSpace(p.AdSoyad)) continue; var ad = p.AdSoyad.Trim(); var b = (p.Bolum ?? "").Trim();
                    bool mali = b.IndexOf("Muhasebe", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("Finans", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("Mali", StringComparison.OrdinalIgnoreCase) >= 0;
                    var i = l.FindIndex(x => string.Equals(x.ad, ad, StringComparison.OrdinalIgnoreCase));
                    if (i >= 0) { var x = l[i]; l[i] = (x.ad, x.kod, b != "" ? b : x.bolum, p.Eposta ?? "", mali ? 0 : x.sira); }
                    else l.Add((ad, "", b, p.Eposta ?? "", mali ? 0 : 3));
                }
            }
            catch (Exception ex) { Console.WriteLine("[İBKB] personel listesi okunamadı: " + ex.Message); }
            var tr = StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false);
            return l.OrderBy(x => x.sira).ThenBy(x => x.ad, tr).Select(x => (object)new { x.ad, x.kod, x.bolum, x.eposta }).ToList();
        }
        static object GorevDto(IbkbGorev g)
        {
            int? gun = g.Termin.HasValue ? (int?)(g.Termin.Value.Date - DateTime.Today).Days : null;
            bool gecikti = GorevAcik(g) && gun.HasValue && gun < 0 || g.Durum == "Onaylandı" && g.Termin.HasValue && g.DuzeltmeTarihi.HasValue && g.DuzeltmeTarihi.Value.Date > g.Termin.Value.Date;
            var e = GorevEposta(g);
            return new
            {
                g.Id, g.CariKodu, g.CariAd, g.Sirket, g.Konu, g.Aciklama, g.BakiyeOzet, g.FarkOzet, g.Atanan, g.AtananKod, g.AtananEposta, g.Atayan, tarih = g.Tarih.ToString("yyyy-MM-dd HH:mm"), termin = D(g.Termin), terminGun = gun, gecikti, g.Oncelik, g.Durum, acik = GorevAcik(g),
                duzeltme = g.DuzeltmeTarihi?.ToString("yyyy-MM-dd HH:mm"), g.Duzelten, g.DuzeltmeNotu, onay = g.OnayTarihi?.ToString("yyyy-MM-dd HH:mm"), g.Onaylayan, g.OnayNotu, g.IadeSayisi, g.KontrolId, g.GonderimId, g.Bildirim,
                sureGun = g.DuzeltmeTarihi.HasValue ? Math.Round((g.DuzeltmeTarihi.Value - g.Tarih).TotalDays, 1) : (double?)null, bekleyenGun = Math.Round((DateTime.Now - (g.Durum == "Düzeltildi" ? g.DuzeltmeTarihi ?? g.Tarih : g.Tarih)).TotalDays, 1),
                epostaKonu = e.Konu, epostaGovde = e.Govde, gecmis = g.Gecmis.OrderByDescending(a => a.Tarih).Select(a => new { tarih = a.Tarih.ToString("yyyy-MM-dd HH:mm"), a.Kullanici, a.Islem, a.Not }).ToList()
            };
        }
        /// <summary>Kişi bazında performans: açık, onay bekleyen, onaylanan, iade, termin aşımı, ortalama düzeltme süresi (gün), zamanında %.</summary>
        static List<object> GorevPerformans()
        {
            return _depo.Gorevler.Where(g => g.Durum != "İptal").GroupBy(g => g.Atanan).Select(gr =>
            {
                var l = gr.ToList(); var biten = l.Where(g => g.DuzeltmeTarihi.HasValue).ToList(); var terminli = l.Where(g => g.Termin.HasValue && (g.DuzeltmeTarihi.HasValue || GorevAcik(g))).ToList();
                int zamaninda = terminli.Count(g => g.DuzeltmeTarihi.HasValue ? g.DuzeltmeTarihi.Value.Date <= g.Termin.Value.Date : DateTime.Today <= g.Termin.Value.Date);
                return new
                {
                    atanan = gr.Key, toplam = l.Count, acik = l.Count(g => g.Durum == "Atandı" || g.Durum == "İade Edildi"), onayBekleyen = l.Count(g => g.Durum == "Düzeltildi"), onaylanan = l.Count(g => g.Durum == "Onaylandı"), iade = l.Sum(g => g.IadeSayisi),
                    gecikmis = l.Count(g => GorevAcik(g) && g.Termin.HasValue && g.Termin.Value.Date < DateTime.Today) + l.Count(g => g.Durum == "Onaylandı" && g.Termin.HasValue && g.DuzeltmeTarihi.HasValue && g.DuzeltmeTarihi.Value.Date > g.Termin.Value.Date),
                    ortSure = biten.Count == 0 ? (double?)null : Math.Round(biten.Average(g => (g.DuzeltmeTarihi.Value - g.Tarih).TotalDays), 1),
                    ortOnay = l.Any(g => g.OnayTarihi.HasValue && g.DuzeltmeTarihi.HasValue) ? Math.Round(l.Where(g => g.OnayTarihi.HasValue && g.DuzeltmeTarihi.HasValue).Average(g => (g.OnayTarihi.Value - g.DuzeltmeTarihi.Value).TotalDays), 1) : (double?)null,
                    zamanindaYuzde = terminli.Count == 0 ? (int?)null : (int)Math.Round(100.0 * zamaninda / terminli.Count), sonGorev = l.Max(g => g.Tarih).ToString("yyyy-MM-dd")
                };
            }).OrderByDescending(x => x.acik).ThenByDescending(x => x.onayBekleyen).ThenBy(x => x.atanan).Select(x => (object)x).ToList();
        }
        // ---------- müşteriye mutabakat gönderimi (12 Kas 2026)
        /// <summary>Müşteriye giden ekstre bağlantısının jetonu (bağlantı portal girişi istemez; cari koduna bağlı sabit özet).</summary>
        public static string EkstreJeton(string kod) { using var sha = System.Security.Cryptography.SHA256.Create(); return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes("UrasIhracatCariEkstre|" + (kod ?? "")))).Substring(0, 16).ToLowerInvariant(); }
        public static string CariGonderimKaydet(CariGonderimIstek d, string kullanici, out CariGonderim kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            lock (Kilit)
            {
                if (d.Id > 0)
                {   // yanıt / durum
                    var g = _depo.CariGonderimleri.FirstOrDefault(x => x.Id == d.Id); if (g == null) return "Gönderim bulunamadı.";
                    var yeni = Sec(d.Durum, SubeGonderimDurumlari, g.Durum); if ((yeni == "Mutabık" || yeni == "İtirazlı") && g.Durum != yeni) g.YanitTarihi = DateTime.Now; g.Durum = yeni; g.Not = Temiz(d.Not);
                    if ((yeni == "Mutabık" || yeni == "İtirazlı") && g.MtGonderimId > 0) { try { EMutabakatOrnek.Yanit(g.CariKodu, g.MtGonderimId, yeni, yeni == "İtirazlı" && g.Not == "" ? "İtiraz (İhracat Finansal Takibi)" : g.Not, kullanici); } catch (Exception ex) { Console.WriteLine("[İBKB] mutabakat yanıtı yazılamadı: " + ex.Message); } }
                    Yaz(); kayit = g; return null;
                }
                string kod = Temiz(d.CariKodu); var c = MutabakatOrnek.Bul(kod); var k = c?.Kart ?? CariKartOrnek.KartBul(kod); if (k == null) return "Cari bulunamadı.";
                string kanal = Sec(d.Kanal, SubeKanallar, "E-posta"), alici = Temiz(d.Alici); if (alici == "") return kanal == "WhatsApp" ? "WhatsApp için cep telefonu gerekli." : "E-posta adresi gerekli.";
                if (kanal == "E-posta" && !alici.Contains("@")) return "E-posta adresi geçersiz.";
                kayit = new CariGonderim { Id = _depo.CariGonderimleri.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1, CariKodu = kod, CariAd = k.KisaAd ?? kod, Kanal = kanal, Icerik = Sec(d.Icerik, CariIcerikler, "Bakiye"), Alici = alici, AliciAd = Temiz(d.AliciAd), Tarih = DateTime.Now, Kullanici = Kim(kullanici), Konu = Temiz(d.Konu), Mesaj = d.Mesaj ?? "", BakiyeOzet = Temiz(d.BakiyeOzet), Durum = "Gönderildi", Not = Temiz(d.Not) };
                if (c != null)
                {   // Cari Mutabakat Yönetimi günlüğüne de yazılır (MtGonderim; yanıt orada da işlenir)
                    try { var mg = MutabakatOrnek.GonderimEkle(c, new MtGonderim { Kanal = kanal, Kime = (kayit.AliciAd != "" ? kayit.AliciAd + " · " : "") + alici, Sonuc = "Bekliyor", YanitAciklama = "", Ozet = kayit.BakiyeOzet }, kullanici); kayit.MtGonderimId = mg.Id; kayit.MtBelgeNo = mg.BelgeNo; }
                    catch (Exception ex) { Console.WriteLine("[İBKB] mutabakat günlüğü yazılamadı: " + ex.Message); }
                    // yetkilinin iletişim bilgisi boşsa cari kartına işlenir
                    var y = EMutabakatOrnek.AnaYetkili(c); if (y != null) { if (kanal == "E-posta" && string.IsNullOrWhiteSpace(y.Eposta)) y.Eposta = alici; if (kanal == "WhatsApp" && string.IsNullOrWhiteSpace(y.CepTelefonu)) y.CepTelefonu = alici; }
                }
                _depo.CariGonderimleri.Add(kayit); Yaz(); return null;
            }
        }
        public static List<CariGonderim> CariGonderimler(string kod) { Yukle(); lock (Kilit) return _depo.CariGonderimleri.Where(g => g.CariKodu == kod).OrderByDescending(g => g.Tarih).ToList(); }
        /// <summary>Carinin ihracat dosyaları (cari kodu ya da kısa ad / resmi unvan eşleşmesi; iptal hariç) — müşteri ekstresinde açık dosya listesi.</summary>
        public static List<IbkbDosya> CariDosyalar(string kod, CariKart k)
        {
            Yukle();
            bool AdAyni(string ad) => k != null && !string.IsNullOrWhiteSpace(ad) && ((!string.IsNullOrEmpty(k.KisaAd) && string.Equals(ad.Trim(), k.KisaAd.Trim(), StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(k.ResmiUnvan) && string.Equals(ad.Trim(), k.ResmiUnvan.Trim(), StringComparison.OrdinalIgnoreCase)));
            lock (Kilit) return _depo.Dosyalar.Where(x => x.DabDurumu != "İptal" && (x.MusteriKodu == kod || AdAyni(x.Musteri))).OrderByDescending(x => x.Yil).ThenBy(x => x.DosyaNo).ToList();
        }
        static object CariGonderimDto(CariGonderim g) => new { g.Id, g.CariKodu, g.CariAd, g.Kanal, g.Icerik, g.Alici, g.AliciAd, tarih = g.Tarih.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, g.Konu, g.Mesaj, g.BakiyeOzet, g.Durum, yanit = g.YanitTarihi?.ToString("yyyy-MM-dd HH:mm"), g.Not, g.MtBelgeNo, gun = (DateTime.Today - g.Tarih.Date).Days };
        /// <summary>İhracat cari bakiyeleri (muhasebe cari / mizan 120 hesapları; portalda Cari Mutabakat Yönetimi'nin bakiye kaynağı) + İBKB dosyalarıyla karşılaştırma + son kontrol.</summary>
        static List<object> CariBakiyeler(List<IbkbDosya> L)
        {
            var sonuc = new List<object>();
            foreach (var c in MutabakatOrnek.Cariler)
            {
                var k = c.Kart; if (k == null || k.KartTipi != "Müşteri" || k.Bolge != "Yurtdışı") continue;
                // dosya bağı: cari kodu eşit ya da (Excel'deki 120.02.xxx kodları kartla eşleşmediğinden) müşteri adı kısa ad / resmi unvanla aynı
                bool AdAyni(string ad) => !string.IsNullOrWhiteSpace(ad) && ((!string.IsNullOrEmpty(k.KisaAd) && string.Equals(ad.Trim(), k.KisaAd.Trim(), StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(k.ResmiUnvan) && string.Equals(ad.Trim(), k.ResmiUnvan.Trim(), StringComparison.OrdinalIgnoreCase)));
                var dosyalar = L.Where(x => x.DabDurumu != "İptal" && (x.MusteriKodu == c.CariKodu || AdAyni(x.Musteri))).ToList();
                var acik = dosyalar.Where(x => KalanGercek(x) > 0.005m).ToList();
                var kalanPb = acik.GroupBy(x => string.IsNullOrEmpty(x.Doviz) ? "?" : x.Doviz).Select(g => new { pb = g.Key, kalan = R2(g.Sum(KalanGercek)) }).ToList();
                var dovizler = c.Dovizler.Where(dv => dv.Net != 0 || dv.AcikFaturaAdet > 0).Select(dv =>
                {
                    string pb = dv.ParaBirimi == "TL" ? "TRY" : dv.ParaBirimi; var dk = kalanPb.FirstOrDefault(x => x.pb == pb);
                    return new { pb, dv.Borc, dv.Alacak, net = dv.Net, dv.Kur, tl = dv.TlKarsiligi, dv.AcikFaturaAdet, dv.AcikFaturaTutar, enEskiAcikFatura = D(dv.EnEskiAcikFatura), dv.BakiyeYasi, dv.EskiBakiyeUyari, dosyaKalan = dk?.kalan ?? 0, fark = R2(dv.Net - (dk?.kalan ?? 0)) };
                }).ToList();
                var son = _depo.BakiyeKontrolleri.Where(x => x.CariKodu == c.CariKodu).OrderByDescending(x => x.Tarih).FirstOrDefault();
                int? gun = son == null ? (int?)null : (DateTime.Today - son.Tarih.Date).Days;
                string kontrolDurumu = son == null ? "Kontrol Yok" : gun > BakiyeKontrolGun ? "Kontrol Gecikti" : son.Durum;
                // düzeltme görevi (açık olan) + müşteriye mutabakat gönderimleri + ana mutabakat yetkilisi (Cari Mutabakat Yönetimi kişileri)
                var gorevler = _depo.Gorevler.Where(g => g.CariKodu == c.CariKodu).OrderByDescending(g => g.Tarih).ToList(); var acikGorev = gorevler.FirstOrDefault(GorevAcik);
                var gonderimler = _depo.CariGonderimleri.Where(g => g.CariKodu == c.CariKodu).OrderByDescending(g => g.Tarih).ToList(); var sonG = gonderimler.FirstOrDefault();
                var yetkili = EMutabakatOrnek.AnaYetkili(c);
                sonuc.Add(new
                {
                    kod = c.CariKodu, ad = k.KisaAd, k.ResmiUnvan, k.Ulke, sirket = k.Sirket ?? "", k.VadeSarti, mutabakatDurumu = c.Durum, k.IliskiDurumu,
                    gorev = acikGorev == null ? null : GorevDto(acikGorev), gorevSayisi = gorevler.Count(g => g.Durum != "İptal"), onaylananGorev = gorevler.Count(g => g.Durum == "Onaylandı"),
                    yetkili = yetkili == null ? null : new { ad = yetkili.AdSoyad ?? "", yetkili.Unvan, eposta = yetkili.Eposta ?? "", cep = EMutabakatOrnek.Cep(yetkili), telefon = yetkili.Telefon ?? "" },
                    gonderimler = gonderimler.Take(20).Select(CariGonderimDto).ToList(), gonderimSayisi = gonderimler.Count, sonGonderim = sonG == null ? null : CariGonderimDto(sonG), ekstreJeton = EkstreJeton(c.CariKodu),
                    dovizler, bakiyeTl = c.BakiyeTl, c.BakiyeYasi, enEskiAcikFatura = D(c.EnEskiAcikFatura), c.EskiBakiyeUyari, c.DegerlemeRiski, sonHareket = D(c.SonHareket), sonMutabakat = D(c.SonMutabakat), acikFaturaAdet = c.Dovizler.Sum(dv => dv.AcikFaturaAdet),
                    dosyaSayisi = dosyalar.Count, acikDosya = acik.Count, dosyaKalan = kalanPb, farkVar = dovizler.Any(dv => Math.Abs(dv.fark) > 0.005m && dv.pb != "TRY"),
                    kontrol = son == null ? null : new { son.Id, tarih = son.Tarih.ToString("yyyy-MM-dd HH:mm"), son.Kullanici, son.Durum, son.Not, son.BakiyeOzet, son.FarkOzet, gun }, kontrolDurumu,
                    kontrolSayisi = _depo.BakiyeKontrolleri.Count(x => x.CariKodu == c.CariKodu)
                });
            }
            return sonuc;
        }
        public static IbkbBelge BelgeBul(int id) { Yukle(); lock (Kilit) return _depo.Belgeler.FirstOrDefault(b => b.Id == id); }
        public static IbkbDosya DosyaBul(int id) { Yukle(); lock (Kilit) return _depo.Dosyalar.FirstOrDefault(b => b.Id == id); }

        // ---------- paket ----------
        public static object Paket()
        {
            Yukle();
            lock (Kilit)
            {
                var L = _depo.Dosyalar.OrderBy(x => x.Sirket).ThenByDescending(x => x.Yil).ThenBy(x => x.DosyaNo, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false)).ToList();
                return new
                {
                    success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), kalici = Dosya != null, vadeGun = VadeGun, esikUsd = EsikUsd, usdKur = UsdKur(), ulkeMuaf = UlkeMuaf, ulkeYarim = UlkeYarim,
                    sirketler = Sirketler.Concat(SatisSiparisOrnek.Sirketler.Select(s => s.Ad)).Distinct().ToList(), dabDurumlari = DabDurumlari, takipDurumlari = TakipDurumlari, dovizler = Dovizler, tahsilatDurumlari = TahsilatDurumlari, belgeDurumlari = BelgeDurumlari, tahsilatKaynaklari = TahsilatKaynaklari, farkNedenleri = FarkNedenleri,
                    yillar = L.Select(x => x.Yil).Distinct().OrderByDescending(x => x).ToList(),
                    // kullanıcının ihracat satış temsilcisi listesi + dosyalarda / şubelerde yazılı adlar (Yurtdışı Satış Siparişi örnek adları listeye girmez)
                    temsilciler = Temsilciler.Concat(L.Select(x => TemsilciBul(x).ad)).Concat(_depo.Subeler.Select(s => s.Temsilci)).Where(t => !string.IsNullOrEmpty(t)).Distinct().OrderBy(t => t, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false)).ToList(),
                    subeler = SubePaket(), subeKanallar = SubeKanallar, subeGonderimDurumlari = SubeGonderimDurumlari,
                    bankalar = L.SelectMany(x => new[] { x.DabBanka }.Concat(x.Dablar.Select(p => p.Banka))).Concat(_depo.Tahsilatlar.Select(t => t.Banka)).Where(b => !string.IsNullOrEmpty(b)).Distinct().OrderBy(b => b).ToList(),
                    // İhracat Satışları: Yurtdışı Satış Siparişi'nden canlı (yüklenmiş / faturalanmış siparişler); dosya bağı fatura no ya da sipariş no ile, yoksa cari + tutar yakınlığı ile
                    satislar = SatisSiparisOrnek.Liste("Yurtdışı").Where(s => s.Durum == "Yüklendi" || s.Durum == "Faturalandı").Select(s =>
                    {
                        var dsy = L.FirstOrDefault(x => (!string.IsNullOrEmpty(s.FaturaNo) && string.Equals(x.FaturaNo, s.FaturaNo, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(s.No) && string.Equals(x.SiparisNo, s.No, StringComparison.OrdinalIgnoreCase)))
                               ?? L.FirstOrDefault(x => x.DabDurumu != "İptal" && !string.IsNullOrEmpty(x.MusteriKodu) && x.MusteriKodu == s.CariKodu && Math.Abs(x.Tutar - s.Tutar) < 0.01m && string.Equals(x.Doviz, s.ParaBirimi, StringComparison.OrdinalIgnoreCase));
                        return new { s.Id, s.No, s.Durum, tarih = D(s.Tarih), s.FaturaNo, faturaTarihi = D(s.FaturaTarihi), yukleme = D(s.YuklendiTarihi ?? s.YuklemeTarihi), sirket = s.FaturaKesen, s.CariKodu, s.Cari, s.Ulke, s.Temsilci, s.OdemeSekli, s.TeslimSekli, tutar = s.Tutar, doviz = s.ParaBirimi == "TL" ? "TRY" : s.ParaBirimi, tutarTl = s.TutarTl, kg = s.ToplamKg, dosyaId = dsy?.Id ?? 0, dosyaNo = dsy?.DosyaNo ?? "", dosyaTakip = dsy == null ? "" : Takip(dsy), esikAlti = s.Tutar * (UsdKur().TryGetValue(s.ParaBirimi, out var r) ? r : 1m) <= EsikUsd };
                    }).ToList(),
                    tahsilatlar = _depo.Tahsilatlar.OrderByDescending(t => t.Tarih).ThenByDescending(t => t.Id).Select(t => { var dsy = L.FirstOrDefault(x => x.Id == t.DosyaId); return new { t.Id, t.Sirket, t.Banka, tarih = D(t.Tarih), t.Gonderen, t.MusteriKodu, t.Tutar, t.Doviz, t.Referans, t.Aciklama, t.Kaynak, t.Durum, t.DosyaId, dosyaNo = dsy?.DosyaNo ?? "", beyannameNo = dsy?.BeyannameNo ?? "", t.IbkbBelgeId, belgeNo = _depo.Belgeler.FirstOrDefault(b => b.Id == t.IbkbBelgeId)?.No ?? "", eslestirme = D(t.EslestirmeTarihi), t.Eslestiren, olusturma = D(t.Olusturma), t.Olusturan }; }).ToList(),
                    cariBakiyeler = CariBakiyeler(L), bakiyeKontrolDurumlari = BakiyeKontrolDurumlari, bakiyeKontrolGun = BakiyeKontrolGun, bakiyeZamani = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    // düzeltme görevleri + müşteri mutabakat gönderimleri (12 Kas 2026)
                    gorevler = _depo.Gorevler.OrderByDescending(g => GorevAcik(g)).ThenByDescending(g => g.Tarih).Select(GorevDto).ToList(), gorevDurumlari = GorevDurumlari, gorevOncelikleri = GorevOncelikleri, gorevKisiler = GorevKisiler(), gorevPerformans = GorevPerformans(),
                    cariGonderimler = _depo.CariGonderimleri.OrderByDescending(g => g.Tarih).Take(200).Select(CariGonderimDto).ToList(), cariIcerikler = CariIcerikler,
                    belgeler = _depo.Belgeler.OrderByDescending(b => b.Tarih).ThenByDescending(b => b.Id).Select(b => new { b.Id, b.No, tarih = D(b.Tarih), b.Sirket, b.Banka, b.DosyaId, b.DosyaNo, b.BeyannameNo, b.FaturaNo, b.MusteriKodu, b.Musteri, b.Ulke, b.BeyannameTutar, b.BeyannameDoviz, b.Tutar, b.Doviz, b.TahsilatId, b.TahsilatRef, tahsilatTarihi = D(b.TahsilatTarihi), fiiliIhrac = D(b.FiiliIhrac), b.Durum, b.BankaIbkbNo, bankaTarihi = D(b.BankaTarihi), b.Not, olusturma = b.Olusturma.ToString("yyyy-MM-dd HH:mm"), b.Olusturan }).ToList(),
                    dosyalar = L.Select(x =>
                    {
                        var tm = TemsilciBul(x);
                        return new
                        {
                        x.Id, x.Sirket, x.Yil, x.DosyaNo, x.Aciklama, x.MusteriKodu, x.Musteri, ulke = x.Ulke != "" ? x.Ulke : (string.IsNullOrEmpty(x.MusteriKodu) ? "" : CariKartOrnek.KartBul(x.MusteriKodu)?.Ulke ?? ""), ulkeKaynak = x.Ulke != "" ? "kayıt" : "cari kart", x.FaturaNo, x.SiparisNo, baslangic = D(x.Baslangic), kapanis = D(x.Kapanis), x.BeyannameNo, x.Doviz, x.Tutar, x.UlkeOran,
                        temsilci = tm.ad, temsilciKaynak = tm.kaynak, temsilciKayit = x.Temsilci,
                        tahsilatSayisi = _depo.Tahsilatlar.Count(t => t.DosyaId == x.Id), belgeSayisi = _depo.Belgeler.Count(b => b.DosyaId == x.Id && b.Durum != "İptal"),
                        x.DabDurumu, x.DabBanka, x.DabKalan, x.DabNotu, dablar = x.Dablar.Select(p => new { p.Tutar, p.Doviz, tarih = D(p.Tarih), p.Banka, p.No, p.Not }).ToList(),
                        gereken = Gereken(x), getirilen = Getirilen(x), kalan = Kalan(x), vade = D(Vade(x)), kalanGun = KalanGun(x), takip = Takip(x), usdKarsiligi = UsdKarsiligi(x), esikAlti = EsikAlti(x),
                        x.GercekBedel, x.FarkNedeni, gercekBedelEtkin = GercekBedelEtkin(x), bedelFarki = BedelFarki(x), gerekenGercek = GerekenGercek(x), kalanGercek = KalanGercek(x), beyannameFazlasi = BeyannameFazlasi(x),
                        x.Not, x.Kaynak, olusturma = D(x.Olusturma), x.Olusturan, guncelleme = x.Guncelleme?.ToString("yyyy-MM-dd HH:mm"), x.Guncelleyen
                        };
                    }).ToList()
                };
            }
        }
    }
}
