// ============================================================
// KREDİ YAPILANDIRMA RAPORU (9 Kas 2026, kullanıcı: "banka kredileri zamanında ödenmediği için yeni yapılandırmalar yapıyoruz … yapılandırma öncesi kredi bilgileri, yapılandırma sonrası,
//   ipotek vs. detayları girilebilsin; aylık taksit Nakit Akış Planlama ve Planlı Ödemelere akmalı")
//   Ekran: Controllers/KrediYapilandirmaController.cs + Views/KrediYapilandirma/Index.cshtml + wwwroot/js/kredi-yapilandirma.js (Mali İşler › Finans › Krediler ve Finansman).
//   Kayıt: şirket (Nakit Akış Planlama şirketleri), banka / şube, kredi no, tür; YAPILANDIRMA ÖNCESİ (kullanım tarihi, anapara, faiz, vade, taksit, kalan anapara / vade, son ödeme, geciken taksit
//   adedi ve tutarı, gecikme faizi, masraf, önceki durum) ve SONRASI (yapılandırma tarihi, protokol no, yeni anapara, yeni faiz, BSMV, yeni vade, ödemesiz ay, yeni taksit — boşsa anüite ile
//   hesaplanır —, ilk taksit tarihi, ödenen taksit, durum), TEMİNATLAR (ipotek: tapu bilgisi, derece, tutar, ekspertiz, malik; kefalet, rehin, teminat mektubu …), not.
//   Taksit takvimi: ödemesiz aylarda faiz anaparaya eklenir, sonra eşit taksit; aylık oran = yıllık faiz / 12 × (1 + BSMV); son taksit kalanı kapatır.
//   Peşinat (9 Kas 2026, "peşinat ödemesi oluyor"): protokolde peşin ödenen tutar; taksitlendirilen yeni anaparanın dışında, toplam geri ödemeye girer; Nakit Akış Planlama'ya "KRD-001 · Peşinat" satırı (anahtar "KRD-001|P"), ödendi işaretiyle ÖDENDİ.
//   Akış: NakitAkisaAktar açıkken her taksit Nakit Akış Planlama'ya Havale / EFT satırı olarak yazılır (NakitAkisOrnek.KrediTaksitiYaz; anahtar "KRD-001|003"; ödenen taksitler ÖDENDİ) ve kredi
//   Planlı Düzenli Ödemeler'e "Kredi" kategorisiyle kaydedilir (DuzenliOdemeOrnek; aylık otomatik akış kapalı (DisAkis) — taksit satırları buradan gelir, aylar AkisBagla ile bağlanır).
//   Yapılandırma değişince satırlar güncellenir, fazlası kaldırılır (ödenmişe dokunulmaz); kayıt silinince ödenmemiş satırlar kaldırılır, düzenli ödeme pasife alınır.
//   Kalıcılık: App_Data/kredi-yapilandirma.json (PersonelMaliyetKalici.Dizin; git dışı; kök KrediDepo: krediler + bağımsız rehin / ipotek kayıtları + teminat çekleri + belgeler; eski düz dizi tanınır).
//   ÖRNEK KAYIT YOK (10 Kas 2026, kullanıcı: "sen hiç örnek koyma, gerçek doğru verileri koyalım"): dosya yoksa boş başlar; tüm kayıtlar gerçek veridir (Ornek alanı yalnız eski dosyalarla uyum için kalır).
//   Belgeler (9 Kas 2026, "sözleşmelerin PDF'ini taratıp yükleyelim, GKS'leri yükleyelim … teminata verilen çekler"): sözleşme / protokol / GKS / ipotek / rehin / teminat mektubu / çek / ekspertiz / fek yazısı PDF-JPG-PNG, App_Data/kredi-belgeler altında;
//   krediye, teminat çekine ya da rehin kaydına bağlanabilir. Rehin · İpotek Takibi: kredi teminatları (Durum / tesis / fek) + eski krediler için bağımsız kayıtlar. Teminat Çekleri: bankaya teminat verilen müşteri çek / senetleri, durum ve iade takibi.
//   Nakit Akış Planlama satırlarının ödendi durumu bellek içidir (Kasa modülü kalıcı değil); açılışta satırlar yeniden yazılır, ödenen taksit sayısı kayıttan gelir.
//   BANKA BORÇ DÖKÜMÜ (11 Kas 2026, kullanıcı: "Kredi Raporuna Emlak Katılım Bankasına ait bu borçları da ekle, Alv Kimya ve Daf Kimya'ya ait"; EMLAK_KATILIM_BORÇ.xlsx): taksit bazlı güncel borç dökümü
//   (BankaBorcSatiri; yapılandırma ÖNCESİ borç). Kredi kaydı DEĞİLDİR: Nakit Akış Planlama / Planlı Düzenli Ödemeler'e satır yazmaz (yapılandırma protokolü imzalanınca kredi kaydı ayrıca girilir).
//   Tohum Data/Tohum/emlak-katilim-borc-2026-11.json (Alv Kimya 42 + Daf Kimya 8 satır; BorcTohumSurumu bayrağıyla bir kez uygulanır, silinen satır geri gelmez). Boş (null) tutar = dökümde o sütun yok:
//   Daf Kimya dökümünde kalan ana para, gecikme cezası ve toplam borç sütunları yoktur (uydurulmaz; ekranda "Dökümde Yok"). Toplamlar dosyanın ÖZET sayfasıyla aynıdır
//   (ana para ₺ 28.875.065,28 · tahsil tutarı ₺ 54.133.370,11; Alv Kimya toplam borç ₺ 46.236.533,23 = kalan ana para 21.711.813,11 + gecikme cezası 24.524.720,12; kâr ve BSMV bu toplamda yoktur).
//   PROTOKOL ÖDEME PLANI · DÖVİZ (11 Kas 2026, kullanıcı: "Emlak Katılım … güncel bakiye, ana para, yapılandırma ödeme detayı, ipotek tapusu ekte; tüm yapılandırma detaylarını Kredi Raporunda güncelle"; Borç Ödeme Protokolü 10.11.2026):
//   banka protokolü ödeme tablosu (tarih + tutar) verdiğinde anüite hesabı yerine OdemePlani satırları kullanılır (Tur Peşinat | Taksit; tutarlar ferileriyle protokol tutarıdır, faiz / anapara ayrımı yapılmaz, Faiz 0 yazılır).
//   Kayıt para birimi PB (TRY / USD / EUR / GBP; tüm tutarlar bu birimde), Kur = TL karşılığı için (0 → NakitAkisOrnek.Kurlar); Nakit Akış Planlama satırları kayıt para birimiyle ve kurla yazılır (U_Tutar PB, U_TutarTL = tutar × kur).
//   Borclular = protokoldeki diğer müşterek / müteselsil borçlular (bilgi). Peşinat satırları "KRD-001|P", "KRD-001|P2" … anahtarlarıyla; taksitler "KRD-001|001" …
//   Borç dökümü ÖZET sayfasının "Yapılandırma Güncel" sütunu (EMLAK_KATILIM_BORÇ_2.xlsx; BorcOzetSatiri, tohum sürüm 2): firma × döviz bazında protokolle yapılandırılan güncel tutar (Alv Kimya TL = 67.785.685,35 − Daf Kimya 7.896.836,88 dosyadaki formül; USD 22.869,60; Daf Kimya 7.896.836,88).
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
        /// <summary>Konkordato / bilirkişi raporu alanları (10 Kas 2026): tapu yevmiye no, malik türü (Asıl Borçlu | 3. Şahıs — 3. şahıs rehniyle teminatlanan alacak konkordatoda adi alacak sayılır), alacak statüsü, bağlı konkordato alacak kaydı, kaynak.</summary>
        public string YevmiyeNo { get; set; } = ""; public string MalikTuru { get; set; } = ""; public string AlacakStatusu { get; set; } = ""; public int AlacakId { get; set; } public string AlacakAnahtar { get; set; } public string Kaynak { get; set; } = "";
        public string Not { get; set; } = ""; public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
    }
    public class TeminatIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public string KrediRef { get; set; } public int KrediId { get; set; }
        public string Tur { get; set; } public string Aciklama { get; set; } public string TapuBilgisi { get; set; } public string Derece { get; set; } public string Malik { get; set; }
        public decimal Tutar { get; set; } public decimal Ekspertiz { get; set; } public string TesisTarihi { get; set; } public string Durum { get; set; } public string FekTarihi { get; set; } public string KrediKapanis { get; set; } public string Not { get; set; }
        public string YevmiyeNo { get; set; } public string MalikTuru { get; set; } public string AlacakStatusu { get; set; } public int AlacakId { get; set; }
    }
    /// <summary>Bankaya teminat olarak verilen müşteri çeki / senedi (9 Kas 2026 "teminata verilen çekler"): çek no, keşideci, çekin bankası, vade, tutar, veriliş, durum (Teminatta | İade Alındı | Tahsil Edildi | Karşılıksız).</summary>
    public class TeminatCeki
    {
        public int Id { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public int KrediId { get; set; } public string KrediRef { get; set; } = "";
        public string Belge { get; set; } = "Çek"; public string CekNo { get; set; } = ""; public string Kesideci { get; set; } = ""; public string CekBankasi { get; set; } = ""; public DateTime? Vade { get; set; } public decimal Tutar { get; set; } public string PB { get; set; } = "TRY";
        public DateTime? VerilisTarihi { get; set; } public string Durum { get; set; } = "Teminatta"; public DateTime? IadeTarihi { get; set; } public string Not { get; set; } = ""; public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
        /// <summary>Konkordato bağı (10 Kas 2026): bilirkişi raporundaki çekler alacak kaydına bağlı, Kaynak "Bilirkişi Raporu 19.02.2026".</summary>
        public int AlacakId { get; set; } public string AlacakAnahtar { get; set; } public string Kaynak { get; set; } = "";
        /// <summary>Çek bazında tahsilat (10 Kas 2026, "tahsil edilen bakiyeyi çek çek göster"): asıl borçlu (çekin teminat olduğu kredinin borçlusu), bankanın tahsil ettiği tutar, alacağa mahsup edildi mi.</summary>
        public string AsilBorclu { get; set; } = ""; public decimal TahsilEdilen { get; set; } public bool Mahsup { get; set; } = true;
    }
    public class CekIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public int KrediId { get; set; } public string KrediRef { get; set; }
        public string Belge { get; set; } public string CekNo { get; set; } public string Kesideci { get; set; } public string CekBankasi { get; set; } public string Vade { get; set; } public decimal Tutar { get; set; } public string PB { get; set; }
        public string VerilisTarihi { get; set; } public string Durum { get; set; } public string IadeTarihi { get; set; } public string Not { get; set; } public int AlacakId { get; set; }
        public string AsilBorclu { get; set; } public decimal TahsilEdilen { get; set; } public bool Mahsup { get; set; } = true;
    }
    /// <summary>Yüklenen belge (9 Kas 2026 "sözleşmelerin PDF'ini taratıp yükleyelim, GKS'leri yükleyelim"): tür (Kredi Sözleşmesi, Yapılandırma Protokolü, GKS, İpotek Belgesi, Rehin Sözleşmesi, Teminat Mektubu, Teminat Çeki, Ödeme Planı, Ekspertiz Raporu, Fek Yazısı, Diğer),
    /// kredi / çek / teminat bağı (isteğe bağlı), şirket, banka, belge tarihi; dosya App_Data/kredi-belgeler altında (PDF / JPG / PNG, en çok 20 MB).</summary>
    public class KrediBelge
    {
        public int Id { get; set; } public string Tur { get; set; } = "Kredi Sözleşmesi"; public string Baslik { get; set; } = ""; public int KrediId { get; set; } public string KrediNo { get; set; } = ""; public int CekId { get; set; } public int TeminatId { get; set; }
        public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public DateTime? Tarih { get; set; } public string Aciklama { get; set; } = "";
        public string DosyaAdi { get; set; } = ""; public string DosyaYolu { get; set; } = ""; public long Boyut { get; set; } public string IcerikTuru { get; set; } = ""; public string Yukleyen { get; set; } = ""; public DateTime Yukleme { get; set; }
    }
    /// <summary>Banka borç dökümü satırı (11 Kas 2026, "Kredi Raporuna Emlak Katılım Bankasına ait bu borçları da ekle"): bankanın taksit bazlı dökümündeki tek satır (yapılandırma ÖNCESİ güncel borç).
    /// Kredi kaydı değildir; nakit akışa satır yazmaz. Tutarlar satırın kendi para birimindedir (PB), "TL" ekli alanlar dökümdeki ₺ karşılığıdır. Boş (null) tutar = dökümde o sütun yok (uydurulmaz).
    /// Borc: Alv Kimya dökümünde "Borç" (kalan ana para + kâr + BSMV), Daf Kimya dökümünde "Taksit Tutarı" (ana para + kâr + BSMV). TahsilTutari / TahsilTutariTL: dökümdeki "Tahsil Tutarı" / "Tahsil Tutar TL"
    /// (Alv Kimya'da kalan ana paraya eşit, Daf Kimya'da 0). ToplamBorcTL: dökümdeki "Toplam Borç TL" (Alv Kimya: tahsil tutar ₺ + gecikme cezası ₺).</summary>
    public class BankaBorcSatiri
    {
        public int Id { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public int SiraNo { get; set; }
        public DateTime? Vade { get; set; } public string TaksitTuru { get; set; } = "Proje"; public string DetayTuru { get; set; } = ""; public string EkNo { get; set; } = ""; public string PB { get; set; } = "TRY";
        public decimal AnaPara { get; set; } public decimal TahsilEdilen { get; set; } public decimal? KalanAnaPara { get; set; } public decimal? KalanAnaParaTL { get; set; }
        public decimal Kar { get; set; } public decimal Bsmv { get; set; } public decimal? Borc { get; set; }
        public decimal? TahsilTutari { get; set; } public decimal? TahsilTutariTL { get; set; } public decimal? GecikmeCezasi { get; set; } public decimal? GecikmeCezasiTL { get; set; } public decimal? ToplamBorcTL { get; set; }
        public string Not { get; set; } = ""; public string Kaynak { get; set; } = ""; public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
    }
    /// <summary>Ekrandan ya da tohum dosyasından gelen borç satırı (vade yyyy-MM-dd ya da gg.aa.yyyy; boş tutar = dökümde yok).</summary>
    public class BorcIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public int SiraNo { get; set; }
        public string Vade { get; set; } public string TaksitTuru { get; set; } public string DetayTuru { get; set; } public string EkNo { get; set; } public string PB { get; set; }
        public decimal AnaPara { get; set; } public decimal TahsilEdilen { get; set; } public decimal? KalanAnaPara { get; set; } public decimal? KalanAnaParaTL { get; set; }
        public decimal Kar { get; set; } public decimal Bsmv { get; set; } public decimal? Borc { get; set; }
        public decimal? TahsilTutari { get; set; } public decimal? TahsilTutariTL { get; set; } public decimal? GecikmeCezasi { get; set; } public decimal? GecikmeCezasiTL { get; set; } public decimal? ToplamBorcTL { get; set; }
        public string Not { get; set; }
    }
    /// <summary>Borç dökümü firma × döviz özeti satırı (11 Kas 2026, EMLAK_KATILIM_BORÇ_2.xlsx ÖZET sayfası): dökümün satırlarından hesaplanmayan tek bilgi "Yapılandırma Güncel" (protokolle yapılandırılan güncel tutar, satırın para biriminde); diğer sütunlar satırlardan türetilir. Not = dosyadaki formül / açıklama.</summary>
    public class BorcOzetSatiri { public int Id { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public string PB { get; set; } = "TRY"; public decimal? YapilandirmaGuncel { get; set; } public string Not { get; set; } = ""; public string Kaynak { get; set; } = ""; }
    /// <summary>Borç dökümü tohum dosyasının kökü (Data/Tohum/emlak-katilim-borc-2026-11.json): satırlar + (sürüm 2) firma × döviz özeti.</summary>
    public class BorcTohum { public int Surum { get; set; } public string Kaynak { get; set; } public List<BorcIstek> Satirlar { get; set; } = new List<BorcIstek>(); public List<BorcOzetSatiri> Ozet { get; set; } = new List<BorcOzetSatiri>(); }
    /// <summary>Kalıcı dosyanın kökü (App_Data/kredi-yapilandirma.json): krediler + bağımsız teminatlar + teminat çekleri + belgeler + banka borç dökümü + borç özeti (11 Kas 2026; BorcTohumSurumu = uygulanan tohum sürümü). Eski dosya düz kredi dizisiydi; okunurken tanınır.</summary>
    public class KrediDepo { public List<KrediKaydi> Krediler { get; set; } = new List<KrediKaydi>(); public List<TeminatKaydi> Teminatlar { get; set; } = new List<TeminatKaydi>(); public List<TeminatCeki> Cekler { get; set; } = new List<TeminatCeki>(); public List<KrediBelge> Belgeler { get; set; } = new List<KrediBelge>(); public List<BankaBorcSatiri> Borclar { get; set; } = new List<BankaBorcSatiri>(); public List<BorcOzetSatiri> BorcOzet { get; set; } = new List<BorcOzetSatiri>(); public int BorcTohumSurumu { get; set; } }
    /// <summary>Protokol ödeme planı satırı (11 Kas 2026, Emlak Katılım Borç Ödeme Protokolü 10.11.2026): bankanın protokol tablosundaki ödeme (tarih + tutar, kayıt para biriminde). Tur Peşinat (taksitlendirilen anaparanın dışındaki peşin ödeme) | Taksit.
    /// Plan girildiyse anüite hesabı yerine bu satırlar kullanılır; tutarlar ferileriyle birlikte protokol tutarıdır (faiz / anapara ayrımı yapılmaz). Odendi: satır ödendi (Kasa satırı ÖDENDİ yazılır).</summary>
    public class ProtokolOdeme { public DateTime? Tarih { get; set; } public decimal Tutar { get; set; } public string Tur { get; set; } = "Taksit"; public bool Odendi { get; set; } public string Aciklama { get; set; } = ""; }
    public class ProtokolOdemeIstek { public string Tarih { get; set; } public decimal Tutar { get; set; } public string Tur { get; set; } public bool Odendi { get; set; } public string Aciklama { get; set; } }
    public class KrediKaydi
    {
        public int Id { get; set; } public string No { get; set; } = "";
        public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public string Sube { get; set; } = ""; public string KrediNo { get; set; } = ""; public string KrediTuru { get; set; } = "Taksitli Ticari Kredi";
        // ---- yapılandırma öncesi
        public DateTime? KullanimTarihi { get; set; } public decimal Anapara { get; set; } public decimal FaizOrani { get; set; } public int Vade { get; set; } public decimal Taksit { get; set; }
        public decimal KalanAnapara { get; set; } public int KalanVade { get; set; } public DateTime? SonOdemeTarihi { get; set; }
        public int GecikenTaksit { get; set; } public decimal GecikenTutar { get; set; } public decimal GecikmeFaizi { get; set; } public decimal Masraf { get; set; } public string OncekiDurum { get; set; } = "Gecikmede";
        /// <summary>Öncesi ayrıntı (11 Kas 2026, kullanıcı: "Kâr / Faiz, BSMV, Gecikme Cezası ayrı olmalı; Geciken Tutar dediğin aslında kâr"): Kar = vadesi geçen taksitlerin kâr payı / faizi, Bsmv = BSMV; GecikmeFaizi = gecikme cezası / temerrüt faizi.
        /// Toplam Borç = KalanAnapara + Kar + Bsmv + GecikmeFaizi + Masraf. GecikenTutar artık bilgi alanı (vadesi geçen taksitlerin toplam tutarı = kalan anapara + kâr + BSMV; toplama girmez).</summary>
        public decimal Kar { get; set; } public decimal Bsmv { get; set; }
        // ---- yapılandırma sonrası
        public DateTime? YapilandirmaTarihi { get; set; } public string ProtokolNo { get; set; } = ""; public decimal YeniAnapara { get; set; } public decimal YeniFaizOrani { get; set; } public decimal BsmvOrani { get; set; } = 5m;
        public int YeniVade { get; set; } public int OdemesizAy { get; set; } public decimal YeniTaksit { get; set; } public DateTime? IlkTaksitTarihi { get; set; } public int OdenenTaksit { get; set; } public string Durum { get; set; } = "Yapılandırıldı";
        /// <summary>Peşinat (9 Kas 2026, "peşinat ödemesi oluyor"): protokolde peşin ödenen tutar; taksitlendirilen yeni anaparanın dışındadır, toplam geri ödemeye girer, Nakit Akış Planlama'ya ayrı satır ("KRD-001 · Peşinat") olarak yazılır. Tarih boşsa yapılandırma tarihi.</summary>
        public decimal Pesinat { get; set; } public DateTime? PesinatTarihi { get; set; } public bool PesinatOdendi { get; set; }
        /// <summary>Para birimi ve kur (11 Kas 2026): tüm tutarlar PB'de (TRY / USD / EUR / GBP); Kur = TL karşılığı (0 → NakitAkisOrnek.Kurlar). Borclular = protokoldeki diğer müşterek / müteselsil borçlular. OdemePlani = banka protokolünün ödeme tablosu (varsa anüite yerine).</summary>
        public string PB { get; set; } = "TRY"; public decimal Kur { get; set; } public string Borclular { get; set; } = ""; public List<ProtokolOdeme> OdemePlani { get; set; } = new List<ProtokolOdeme>();
        public List<KrediTeminat> Teminatlar { get; set; } = new List<KrediTeminat>();
        public string Not { get; set; } = "";
        /// <summary>Taksitler Nakit Akış Planlama'ya satır olarak yazılır ve kredi Planlı Düzenli Ödemeler'de izlenir.</summary>
        public bool NakitAkisaAktar { get; set; } = true;
        public bool Ornek { get; set; }
        public int DuzenliOdemeId { get; set; } public string DuzenliOdemeNo { get; set; } = "";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = ""; public DateTime Guncelleme { get; set; } public string Guncelleyen { get; set; } = "";
        public List<TalimatGunluk> Gunluk { get; set; } = new List<TalimatGunluk>();
    }
    public class KrediTaksit { public int No { get; set; } public DateTime Tarih { get; set; } public decimal Tutar { get; set; } public decimal Anapara { get; set; } public decimal Faiz { get; set; } public decimal Kalan { get; set; } public bool Odendi { get; set; } public string Aciklama { get; set; } = ""; }
    /// <summary>Ekrandan gelen kayıt (tarihler yyyy-MM-dd ya da gg.aa.yyyy).</summary>
    public class KrediIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public string Sube { get; set; } public string KrediNo { get; set; } public string KrediTuru { get; set; }
        public string KullanimTarihi { get; set; } public decimal Anapara { get; set; } public decimal FaizOrani { get; set; } public int Vade { get; set; } public decimal Taksit { get; set; }
        public decimal KalanAnapara { get; set; } public int KalanVade { get; set; } public string SonOdemeTarihi { get; set; }
        public int GecikenTaksit { get; set; } public decimal GecikenTutar { get; set; } public decimal GecikmeFaizi { get; set; } public decimal Masraf { get; set; } public string OncekiDurum { get; set; } public decimal Kar { get; set; } public decimal Bsmv { get; set; }
        public string YapilandirmaTarihi { get; set; } public string ProtokolNo { get; set; } public decimal YeniAnapara { get; set; } public decimal YeniFaizOrani { get; set; } public decimal BsmvOrani { get; set; } = 5m;
        public int YeniVade { get; set; } public int OdemesizAy { get; set; } public decimal YeniTaksit { get; set; } public string IlkTaksitTarihi { get; set; } public int OdenenTaksit { get; set; } public string Durum { get; set; }
        public decimal Pesinat { get; set; } public string PesinatTarihi { get; set; } public bool PesinatOdendi { get; set; }
        public List<KrediTeminat> Teminatlar { get; set; } public string Not { get; set; } public bool NakitAkisaAktar { get; set; } = true;
        public string PB { get; set; } public decimal Kur { get; set; } public string Borclular { get; set; } public List<ProtokolOdemeIstek> OdemePlani { get; set; }
    }

    public static class KrediYapilandirmaOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly List<KrediKaydi> _liste = new List<KrediKaydi>();
        static readonly List<TeminatKaydi> _teminatlar = new List<TeminatKaydi>(); static readonly List<TeminatCeki> _cekler = new List<TeminatCeki>(); static readonly List<KrediBelge> _belgeler = new List<KrediBelge>();
        static int _sonId, _sonTeminatId, _sonCekId, _sonBelgeId; static bool _yuklendi;
        static readonly List<BankaBorcSatiri> _borclar = new List<BankaBorcSatiri>(); static readonly List<BorcOzetSatiri> _borcOzet = new List<BorcOzetSatiri>(); static int _sonBorcId, _sonBorcOzetId, _borcTohumSurumu;
        const int BorcTohumSurum = 2;   // 1: Emlak Katılım · Alv Kimya 42 + Daf Kimya 8 satır (11 Kas 2026, EMLAK_KATILIM_BORÇ.xlsx); 2: ÖZET sayfasının Yapılandırma Güncel sütunu (EMLAK_KATILIM_BORÇ_2.xlsx, aynı gün)
        public static readonly string[] BorcTaksitTurleri = { "Proje", "Masraf" };
        public static readonly string[] ParaBirimleri = { "TRY", "USD", "EUR", "GBP" };
        public static readonly string[] PlanTurleri = { "Peşinat", "Taksit" };
        /// <summary>Tesis Bekliyor (11 Kas 2026): protokolde taahhüt edilen, henüz tapuya tescil edilmemiş ipotek.</summary>
        public static readonly string[] TeminatDurumlari = { "Tesis Bekliyor", "Aktif", "Fek Bekliyor", "Fek Edildi", "Paraya Çevrildi" };
        public static readonly string[] CekDurumlari = { "Teminatta", "İade Alındı", "Tahsil Edildi", "Karşılıksız" };
        public static readonly string[] BelgeTurleri = { "Kredi Sözleşmesi", "Yapılandırma Protokolü", "Genel Kredi Sözleşmesi (GKS)", "İpotek Belgesi", "Rehin Sözleşmesi", "Teminat Mektubu", "Teminat Çeki", "Ödeme Planı", "Ekspertiz Raporu", "Fek Yazısı", "Bilirkişi Raporu", "Kat İhtarı", "Tebligat", "Mahkeme Kararı", "Hesap Ekstresi", "Diğer" };
        public static readonly string[] MalikTurleri = { "Asıl Borçlu", "3. Şahıs" };
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
        /// <summary>Yapılandırma öncesi toplam borç = kalan anapara + kâr / faiz + BSMV + gecikme cezası + masraf (11 Kas 2026; geciken taksit tutarı bilgi alanıdır, toplama girmez).</summary>
        public static decimal OncesiToplam(KrediKaydi k) => R2(k.KalanAnapara + k.Kar + k.Bsmv + k.GecikmeFaizi + k.Masraf);
        /// <summary>Protokol ödeme planı girilmiş mi (11 Kas 2026): varsa anüite yerine plan satırları.</summary>
        public static bool PlanVar(KrediKaydi k) => k.OdemePlani != null && k.OdemePlani.Count > 0;
        static IEnumerable<ProtokolOdeme> PlanTaksit(KrediKaydi k) => (k.OdemePlani ?? new List<ProtokolOdeme>()).Where(p => p.Tur != "Peşinat");
        static IEnumerable<ProtokolOdeme> PlanPesinat(KrediKaydi k) => (k.OdemePlani ?? new List<ProtokolOdeme>()).Where(p => p.Tur == "Peşinat");
        /// <summary>Uygulanan kur: kayıttaki kur, yoksa Nakit Akış Planlama kur tablosu (TRY = 1).</summary>
        public static decimal KurUyg(KrediKaydi k) => (k.PB ?? "TRY") == "TRY" ? 1m : k.Kur > 0 ? k.Kur : (NakitAkisOrnek.Kurlar.TryGetValue(k.PB, out var x) ? x : 1m);
        /// <summary>Peşinat toplamı: plan varsa Peşinat satırlarının toplamı, yoksa tek peşinat alanı.</summary>
        public static decimal PesinatToplam(KrediKaydi k) => PlanVar(k) ? R2(PlanPesinat(k).Sum(p => p.Tutar)) : k.Pesinat;
        /// <summary>Peşinat satırları (No 1..n): plan varsa Peşinat satırları, yoksa tek peşinat alanından bir satır (tutar &gt; 0 ise).</summary>
        public static List<KrediTaksit> Pesinatlar(KrediKaydi k)
        {
            var l = new List<KrediTaksit>();
            if (PlanVar(k)) { int i = 0; foreach (var p in PlanPesinat(k)) l.Add(new KrediTaksit { No = ++i, Tarih = p.Tarih ?? DateTime.Today, Tutar = p.Tutar, Anapara = p.Tutar, Faiz = 0, Kalan = k.YeniAnapara, Odendi = p.Odendi, Aciklama = p.Aciklama ?? "" }); }
            else if (k.Pesinat > 0) l.Add(new KrediTaksit { No = 1, Tarih = PesinatTarih(k), Tutar = k.Pesinat, Anapara = k.Pesinat, Faiz = 0, Kalan = k.YeniAnapara, Odendi = k.PesinatOdendi });
            return l;
        }
        public static int TaksitSayisi(KrediKaydi k) => PlanVar(k) ? PlanTaksit(k).Count() : Math.Max(0, k.YeniVade - Math.Max(0, k.OdemesizAy));
        /// <summary>Ödemesiz ayların faizi anaparaya eklenmiş başlangıç borcu.</summary>
        static decimal BaslangicBorcu(KrediKaydi k) => R2(k.YeniAnapara * (decimal)Math.Pow((double)(1 + AylikOran(k)), Math.Max(0, k.OdemesizAy)));
        /// <summary>Anüite (eşit taksit): P × r / (1 − (1 + r)^−n); faiz 0 ise P / n. Protokol planı varsa 0 (hesaplanmaz).</summary>
        public static decimal HesaplananTaksit(KrediKaydi k)
        {
            if (PlanVar(k)) return 0;
            int n = TaksitSayisi(k); if (n <= 0 || k.YeniAnapara <= 0) return 0;
            decimal r = AylikOran(k), p = BaslangicBorcu(k); if (r <= 0) return R2(p / n);
            double rr = (double)r; return R2((decimal)((double)p * rr / (1 - Math.Pow(1 + rr, -n))));
        }
        /// <summary>Taksit takvimi: protokol planı varsa plan satırları (tutar protokol tutarı, faiz 0, anapara = tutar, kalan = yeni anapara − ödenen taksitler); yoksa tutar = yeni taksit (banka) ya da hesaplanan; faiz = kalan × aylık oran, anapara = taksit − faiz; son taksit kalanı kapatır. Tarihler ilk taksitten aylık.</summary>
        public static List<KrediTaksit> Taksitler(KrediKaydi k)
        {
            var l = new List<KrediTaksit>();
            if (PlanVar(k))
            {
                decimal kalanP = k.YeniAnapara > 0 ? k.YeniAnapara : R2(PlanTaksit(k).Sum(p => p.Tutar)); int j = 0;
                foreach (var p in PlanTaksit(k)) { j++; kalanP = Math.Max(0, R2(kalanP - p.Tutar)); l.Add(new KrediTaksit { No = j, Tarih = p.Tarih ?? DateTime.Today, Tutar = p.Tutar, Anapara = p.Tutar, Faiz = 0, Kalan = kalanP, Odendi = p.Odendi || j <= k.OdenenTaksit, Aciklama = p.Aciklama ?? "" }); }
                return l;
            }
            int n = TaksitSayisi(k); if (n <= 0 || k.YeniAnapara <= 0) return l;
            decimal r = AylikOran(k), taksit = k.YeniTaksit > 0 ? k.YeniTaksit : HesaplananTaksit(k), kalan = BaslangicBorcu(k);
            var ilk = k.IlkTaksitTarihi ?? (k.YapilandirmaTarihi ?? DateTime.Today).AddMonths(1 + Math.Max(0, k.OdemesizAy));
            for (int i = 1; i <= n && kalan > 0; i++)
            {
                decimal faiz = R2(kalan * r), anapara = i == n ? kalan : Math.Max(0, R2(taksit - faiz));
                if (anapara > kalan) anapara = kalan;
                decimal tutar = i == n || anapara == kalan ? R2(anapara + faiz) : taksit;
                kalan = Math.Max(0, R2(kalan - anapara));
                l.Add(new KrediTaksit { No = i, Tarih = ilk.AddMonths(i - 1), Tutar = tutar, Anapara = anapara, Faiz = faiz, Kalan = kalan, Odendi = i <= k.OdenenTaksit });
            }
            return l;
        }

        // ---------- liste ve kalıcılık ----------
        public static List<KrediKaydi> Liste() { Yukle(); lock (Kilit) return _liste.OrderBy(k => k.Id).ToList(); }
        public static KrediKaydi Bul(int id) { Yukle(); lock (Kilit) return _liste.FirstOrDefault(k => k.Id == id); }
        /// <summary>Açılışta (OrnekIsinma, Nakit Akış Planlama ve Planlı Düzenli Ödemeler tohumlandıktan sonra): dosya okunur (yoksa boş; örnek kayıt üretilmez); her kayıt için akış satırları yeniden yazılır.</summary>
        public static void Yukle()
        {
            List<KrediKaydi> l;
            lock (Kilit)
            {
                if (_yuklendi) return; _yuklendi = true;
                var dosya = Dosya; bool dosyaSaglam = true;   // dosya okunamadıysa tohum uygulanmaz (bozuk dosyanın üstüne yazılmaz)
                if (dosya != null && File.Exists(dosya))
                {
                    try
                    {
                        string metin = File.ReadAllText(dosya);
                        using (var doc = JsonDocument.Parse(metin))
                        {
                            if (doc.RootElement.ValueKind == JsonValueKind.Array) _liste.AddRange(JsonSerializer.Deserialize<List<KrediKaydi>>(metin, Secenek) ?? new List<KrediKaydi>());   // eski biçim: yalnız krediler
                            else { var depo = JsonSerializer.Deserialize<KrediDepo>(metin, Secenek) ?? new KrediDepo(); _liste.AddRange(depo.Krediler ?? new List<KrediKaydi>()); _teminatlar.AddRange(depo.Teminatlar ?? new List<TeminatKaydi>()); _cekler.AddRange(depo.Cekler ?? new List<TeminatCeki>()); _belgeler.AddRange(depo.Belgeler ?? new List<KrediBelge>()); _borclar.AddRange(depo.Borclar ?? new List<BankaBorcSatiri>()); _borcOzet.AddRange(depo.BorcOzet ?? new List<BorcOzetSatiri>()); _borcTohumSurumu = depo.BorcTohumSurumu; }
                        }
                        _sonId = _liste.Select(k => k.Id).DefaultIfEmpty(0).Max(); _sonTeminatId = _teminatlar.Select(t => t.Id).DefaultIfEmpty(0).Max(); _sonCekId = _cekler.Select(c => c.Id).DefaultIfEmpty(0).Max(); _sonBelgeId = _belgeler.Select(b => b.Id).DefaultIfEmpty(0).Max(); _sonBorcId = _borclar.Select(b => b.Id).DefaultIfEmpty(0).Max(); _sonBorcOzetId = _borcOzet.Select(b => b.Id).DefaultIfEmpty(0).Max();
                        Console.WriteLine($"[Kredi] {_liste.Count} kredi, {_teminatlar.Count} rehin / ipotek, {_cekler.Count} teminat çeki, {_belgeler.Count} belge, {_borclar.Count} borç dökümü satırı yüklendi · {dosya}");
                    }
                    catch (Exception ex) { dosyaSaglam = false; Console.WriteLine("[Kredi] dosya okunamadı: " + ex.Message); }
                }
                else Console.WriteLine("[Kredi] kalıcı dosya yok; boş başlıyor (örnek kayıt üretilmez)");
                if (dosyaSaglam && _borcTohumSurumu < BorcTohumSurum) BorcTohumUygula();   // banka borç dökümü tohumu (gerçek veri; 11 Kas 2026)
                foreach (var k in _liste) { if (k.Teminatlar == null) k.Teminatlar = new List<KrediTeminat>(); if (k.Gunluk == null) k.Gunluk = new List<TalimatGunluk>(); if (k.OdemePlani == null) k.OdemePlani = new List<ProtokolOdeme>(); if (string.IsNullOrEmpty(k.PB)) k.PB = "TRY"; if (k.Borclular == null) k.Borclular = ""; foreach (var t in k.Teminatlar) if (string.IsNullOrEmpty(t.Durum)) t.Durum = "Aktif"; }
                l = _liste.ToList();
            }
            foreach (var k in l) { try { Senkron(k, "Sistem"); } catch (Exception ex) { Console.WriteLine($"[Kredi] {k.No} akış senkronu: {ex.Message}"); } }
        }
        static void Yaz()
        {
            var dosya = Dosya; if (dosya == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(dosya)); File.WriteAllText(dosya, JsonSerializer.Serialize(new KrediDepo { Krediler = _liste, Teminatlar = _teminatlar, Cekler = _cekler, Belgeler = _belgeler, Borclar = _borclar, BorcOzet = _borcOzet, BorcTohumSurumu = _borcTohumSurumu }, Secenek)); }
            catch (Exception ex) { Console.WriteLine("[Kredi] dosya yazılamadı: " + ex.Message); }
        }
        // (Örnek kayıt tohumu 10 Kas 2026'da kaldırıldı: Kredi Raporu yalnız gerçek veri taşır.)

        // ---------- kayıt ----------
        static string Temiz(string s) => (s ?? "").Trim();
        public static string Kaydet(KrediIstek d, string kullanici, out KrediKaydi kayit, out string ozet)
        {
            kayit = null; ozet = ""; if (d == null) return "Veri yok.";
            Yukle();
            string sirket = Temiz(d.Sirket), banka = Temiz(d.Banka);
            if (!CekAkibetOrnek.Sirketler.Any(s => s.Ad == sirket)) return "Şirket seçilmelidir (Nakit Akış Planlama şirketleri).";
            if (banka == "") return "Banka zorunludur.";
            if (d.Anapara < 0 || d.KalanAnapara < 0 || d.Taksit < 0 || d.GecikenTutar < 0 || d.GecikmeFaizi < 0 || d.Masraf < 0 || d.YeniAnapara < 0 || d.YeniTaksit < 0 || d.Pesinat < 0 || d.Kar < 0 || d.Bsmv < 0) return "Tutarlar negatif olamaz.";
            if (d.FaizOrani < 0 || d.FaizOrani > 300 || d.YeniFaizOrani < 0 || d.YeniFaizOrani > 300 || d.BsmvOrani < 0 || d.BsmvOrani > 100) return "Faiz / BSMV oranı geçersiz.";
            if (d.Vade < 0 || d.Vade > 600 || d.YeniVade < 0 || d.YeniVade > 600 || d.KalanVade < 0 || d.KalanVade > 600) return "Vade 0–600 ay arasında olmalıdır.";
            // protokol ödeme planı (11 Kas 2026): tarih + tutar zorunlu; tarihe göre sıralanır (aynı gün peşinat önce)
            var plan = new List<ProtokolOdeme>();
            foreach (var p in d.OdemePlani ?? new List<ProtokolOdemeIstek>())
            {
                if (p == null) continue; var pt = Tarih(p.Tarih); if (pt == null && p.Tutar <= 0 && Temiz(p.Aciklama) == "") continue;
                if (pt == null) return "Ödeme planında tarihi girilmemiş satır var."; if (p.Tutar <= 0) return $"Ödeme planında {pt:dd.MM.yyyy} satırının tutarı sıfırdan büyük olmalıdır.";
                plan.Add(new ProtokolOdeme { Tarih = pt, Tutar = R2(p.Tutar), Tur = p.Tur == "Peşinat" ? "Peşinat" : "Taksit", Odendi = p.Odendi, Aciklama = Temiz(p.Aciklama) });
            }
            plan = plan.OrderBy(p => p.Tarih).ThenBy(p => p.Tur == "Peşinat" ? 0 : 1).ToList(); bool planVar = plan.Count > 0; int planTaksit = plan.Count(p => p.Tur == "Taksit");
            if (planVar && planTaksit == 0) return "Ödeme planında en az bir Taksit satırı olmalıdır.";
            string pb = Temiz(d.PB).ToUpperInvariant(); if (pb == "TL" || pb == "") pb = "TRY"; if (!ParaBirimleri.Contains(pb)) return "Para birimi TRY / USD / EUR / GBP olmalıdır.";
            if (d.Kur < 0 || d.Kur > 100000) return "Kur geçersiz.";
            if (!planVar && (d.OdemesizAy < 0 || d.OdemesizAy >= Math.Max(1, d.YeniVade))) return "Ödemesiz dönem yeni vadeden kısa olmalıdır.";
            if (d.GecikenTaksit < 0 || d.OdenenTaksit < 0) return "Taksit adedi negatif olamaz.";
            if (!planVar && d.YeniAnapara <= 0 && d.Pesinat <= 0 && d.Durum != "Teklif Aşamasında" && d.Durum != "İptal") return "Yapılandırma sonrası yeni anapara girilmelidir (kalan anapara + geciken taksitler + gecikme faizi + masraflar − peşinat).";
            if (!planVar && d.YeniVade <= 0 && d.YeniAnapara > 0) return "Yeni vade (ay) girilmelidir.";
            if (!planVar && d.Pesinat > 0 && string.IsNullOrWhiteSpace(d.PesinatTarihi) && string.IsNullOrWhiteSpace(d.YapilandirmaTarihi)) return "Peşinat için peşinat tarihi ya da yapılandırma tarihi girilmelidir.";
            if (d.OdenenTaksit > (planVar ? planTaksit : Math.Max(0, d.YeniVade - d.OdemesizAy))) return "Ödenen taksit sayısı taksit sayısını aşamaz.";
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
                k.GecikenTaksit = d.GecikenTaksit; k.GecikenTutar = R2(d.GecikenTutar); k.GecikmeFaizi = R2(d.GecikmeFaizi); k.Masraf = R2(d.Masraf); k.OncekiDurum = onceki; k.Kar = R2(d.Kar); k.Bsmv = R2(d.Bsmv);
                k.YapilandirmaTarihi = Tarih(d.YapilandirmaTarihi); k.ProtokolNo = Temiz(d.ProtokolNo); k.YeniAnapara = R2(d.YeniAnapara); k.YeniFaizOrani = d.YeniFaizOrani; k.BsmvOrani = d.BsmvOrani; k.YeniVade = d.YeniVade; k.OdemesizAy = d.OdemesizAy; k.YeniTaksit = R2(d.YeniTaksit);
                k.IlkTaksitTarihi = Tarih(d.IlkTaksitTarihi); k.OdenenTaksit = d.OdenenTaksit; k.Durum = durum; k.Teminatlar = teminatlar; k.Not = Temiz(d.Not); k.NakitAkisaAktar = d.NakitAkisaAktar; k.Ornek = false;
                k.Pesinat = R2(d.Pesinat); k.PesinatTarihi = Tarih(d.PesinatTarihi); k.PesinatOdendi = d.PesinatOdendi && k.Pesinat > 0;
                k.PB = pb; k.Kur = d.Kur > 0 ? Math.Round(d.Kur, 4) : 0; k.Borclular = Temiz(d.Borclular); k.OdemePlani = plan;
                if (planVar)
                {   // protokol planı: vade = taksit satırı sayısı, ödemesiz / banka taksiti yok; peşinat alanları plan satırlarından; yeni anapara boşsa taksit satırlarının toplamı
                    k.OdemesizAy = 0; k.YeniVade = planTaksit; k.YeniTaksit = 0; if (k.YeniAnapara <= 0) k.YeniAnapara = R2(plan.Where(p => p.Tur == "Taksit").Sum(p => p.Tutar));
                    var pes = plan.Where(p => p.Tur == "Peşinat").ToList(); k.Pesinat = R2(pes.Sum(p => p.Tutar)); k.PesinatTarihi = pes.FirstOrDefault()?.Tarih; k.PesinatOdendi = pes.Count > 0 && pes.All(p => p.Odendi);
                    k.IlkTaksitTarihi = plan.First(p => p.Tur == "Taksit").Tarih;
                }
                k.Guncelleme = DateTime.Now; k.Guncelleyen = kullanici ?? "";
                var yeniTaksit = Taksitler(k).FirstOrDefault()?.Tutar ?? 0; string pbs = pb == "TRY" ? "₺" : pb;
                Log(k, yeni ? "Kayıt" : "Güncelleme", $"{banka} · {k.KrediNo} · {sirket} · yeni anapara {k.YeniAnapara.ToString("N2", Tr)} {pbs} · {k.YeniVade} ay · taksit {yeniTaksit.ToString("N2", Tr)} {pbs}" + (planVar ? $" · protokol ödeme planı {plan.Count} satır" : "") + (k.Pesinat > 0 ? $" · peşinat {k.Pesinat.ToString("N2", Tr)} {pbs} ({PesinatTarih(k):dd.MM.yyyy}{(k.PesinatOdendi ? ", ödendi" : "")})" : "") + (!yeni && eskiTaksit != yeniTaksit ? $" (önce {eskiTaksit.ToString("N2", Tr)})" : "") + (k.NakitAkisaAktar ? " · nakit akışa aktarılır" : " · nakit akışa aktarılmaz"), kullanici);
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
            ozet = $"{k.No} silindi; Nakit Akış Planlama'dan {kal} ödenmemiş taksit satırı kaldırıldı" + (odenmis > 0 ? $", {odenmis} ödenmiş satır kaldı" : "") + (k.DuzenliOdemeId > 0 ? $"; düzenli ödeme {k.DuzenliOdemeNo} pasife alındı" : "") + ".";
            return null;
        }
        public static string AktarDegistir(int id, bool aktar, string kullanici, out string ozet)
        {
            ozet = ""; var k = Bul(id); if (k == null) return "Kayıt bulunamadı.";
            lock (Kilit) { k.NakitAkisaAktar = aktar; k.Ornek = false; Log(k, aktar ? "Nakit Akışa Aktarım Açıldı" : "Nakit Akışa Aktarım Kapatıldı", aktar ? "taksitler Nakit Akış Planlama ve Planlı Ödemeler'e yazılır" : "ödenmemiş taksit satırları kaldırılır, düzenli ödeme pasife alınır", kullanici); Yaz(); }
            var s = Senkron(k, kullanici); ozet = $"{k.No}: {SenkronOzet(k, s)}"; return null;
        }

        // ---------- Nakit Akış Planlama ve Planlı Düzenli Ödemeler ----------
        /// <summary>Peşinat tarihi: girilen peşinat tarihi, yoksa yapılandırma tarihi, yoksa bugün.</summary>
        public static DateTime PesinatTarih(KrediKaydi k) => k.PesinatTarihi ?? k.YapilandirmaTarihi ?? DateTime.Today;
        /// <summary>Peşinat + taksit satırlarını yazar / günceller / fazlasını kaldırır ve düzenli ödeme kaydını eşler. Kilit dışında çağrılır (NakitAkis ve Duzenli kendi kilitlerini alır).</summary>
        public static (int eklendi, int guncellendi, int kaldirildi, int odenmis) Senkron(KrediKaydi k, string kullanici)
        {
            var taksitler = Taksitler(k); var pesinatlar = Pesinatlar(k); var anahtarlar = new HashSet<string>(); int e = 0, g = 0, kal = 0, od = 0;
            bool aktif = k.NakitAkisaAktar && k.Durum != "İptal" && k.Durum != "Teklif Aşamasında"; bool plan = PlanVar(k);
            string pb = string.IsNullOrEmpty(k.PB) ? "TRY" : k.PB, pbs = pb == "TRY" ? "₺" : pb; decimal kur = KurUyg(k);
            var db = CekAkibetOrnek.DbName(k.Sirket); if (!CekAkibetOrnek.Sirketler.Any(s => s.Ad == k.Sirket)) aktif = false;
            string protokol = $"Kredi Raporu {k.No} · {k.Banka}{(k.Sube != "" ? " " + k.Sube : "")}{(k.ProtokolNo != "" ? " · protokol " + k.ProtokolNo : "")}";
            if (aktif)
                foreach (var p in pesinatlar)
                {   // peşinat: protokol tarihinde tek seferlik Havale / EFT satırı (anahtar "KRD-001|P"; birden çok peşinatta "KRD-001|P2" …)
                    string anahtar = p.No == 1 ? $"{k.No}|P" : $"{k.No}|P{p.No}"; anahtarlar.Add(anahtar); string sira = pesinatlar.Count > 1 ? $" {p.No}/{pesinatlar.Count}" : "";
                    var (m, islem) = NakitAkisOrnek.KrediTaksitiYaz(db, anahtar, k.Banka, $"Kredi Peşinatı{sira} · {k.KrediTuru}", $"{k.No} · Peşinat{(pesinatlar.Count > 1 ? " " + p.No : "")}", p.Tutar, p.Tarih, $"{k.No} · peşin ödeme", kullanici, pb, kur);   // 13 Kas 2026 ("açıklama çok uzun, sadeleştir"): ürün / hizmet ve not kısa; ayrıntı (kredi no, protokol, anapara) Kredi Raporu'nda
                    if (islem == "eklendi") e++; else if (islem == "güncellendi") g++; else od++;
                    if (p.Odendi && m.U_Durum != "ÖDENDİ") NakitAkisOrnek.OdendiIsaretle(db, m.DocEntry, "Kredi peşinatı " + k.No, kullanici, false, p.Tarih);
                }
            if (aktif)
                foreach (var t in taksitler)
                {
                    string anahtar = $"{k.No}|{t.No:000}"; anahtarlar.Add(anahtar);
                    string belgeNo = $"{k.No} · {t.No:00}/{taksitler.Count:00}";
                    string urun = $"Kredi Taksidi {t.No}/{taksitler.Count} · {k.KrediTuru}";   // sade: ayrıntı (kredi no, ödeme planı açıklaması, anapara + faiz) Kredi Raporu'nda
                    string not = $"{k.No} · kalan anapara {t.Kalan.ToString("N2", Tr)} {pbs}";
                    var (m, islem) = NakitAkisOrnek.KrediTaksitiYaz(db, anahtar, k.Banka, urun, belgeNo, t.Tutar, t.Tarih, not, kullanici, pb, kur);
                    if (islem == "eklendi") e++; else if (islem == "güncellendi") g++; else od++;
                    if (t.Odendi && m.U_Durum != "ÖDENDİ") NakitAkisOrnek.OdendiIsaretle(db, m.DocEntry, "Kredi taksidi " + k.No, kullanici, false, t.Tarih);
                }
            foreach (var m in NakitAkisOrnek.KrediSatirlari(k.No + "|")) if (!anahtarlar.Contains(m.U_KrediTaksit) && NakitAkisOrnek.KrediTaksitiKaldir(m.U_KrediTaksit, kullanici) != null) kal++;
            DuzenliSenkron(k, aktif ? taksitler : new List<KrediTaksit>(), kullanici);
            return (e, g, kal, od);
        }
        static string SenkronOzet(KrediKaydi k, (int eklendi, int guncellendi, int kaldirildi, int odenmis) s)
        {
            if (!k.NakitAkisaAktar) return "Nakit akışa aktarım kapalı" + (s.kaldirildi > 0 ? $"; {s.kaldirildi} ödenmemiş satır kaldırıldı" : "") + ".";
            if (k.Durum == "İptal" || k.Durum == "Teklif Aşamasında") return $"Durum \"{k.Durum}\": taksitler nakit akışa yazılmaz" + (s.kaldirildi > 0 ? $"; {s.kaldirildi} satır kaldırıldı" : "") + ".";
            return $"Nakit Akış Planlama: {s.eklendi} taksit satırı eklendi, {s.guncellendi} güncellendi, {s.kaldirildi} kaldırıldı" + (s.odenmis > 0 ? $", {s.odenmis} ödenmiş satır değişmedi" : "") + (k.DuzenliOdemeNo != "" ? $" · Planlı Düzenli Ödemeler: {k.DuzenliOdemeNo}" : "") + ".";
        }
        /// <summary>Planlı Düzenli Ödemeler kaydı: banka = tedarikçi, "Kredi" kategorisi, Havale / EFT, ilk taksit günü, taksit tutarı; aylık otomatik akış kapalı (DisAkis), taksit ayları Kasa satırlarına bağlanır. Taksit yoksa pasif.</summary>
        static void DuzenliSenkron(KrediKaydi k, List<KrediTaksit> taksitler, string kullanici)
        {
            if (taksitler.Count == 0) { if (k.DuzenliOdemeId > 0) DuzenliOdemeOrnek.DurumDegistir(k.DuzenliOdemeId, "Pasif", kullanici); return; }
            var ilk = taksitler[0]; int gun = Math.Max(1, Math.Min(28, ilk.Tarih.Day)); string pb = string.IsNullOrEmpty(k.PB) ? "TRY" : k.PB; decimal kur = KurUyg(k);
            string urun = $"Kredi Taksidi · {(k.KrediNo != "" ? k.KrediNo : k.KrediTuru)} ({k.No})";
            string not = $"Kredi Raporu {k.No}: {taksitler.Count} taksit, ilk {ilk.Tarih:dd.MM.yyyy}, son {taksitler[taksitler.Count - 1].Tarih:dd.MM.yyyy}; taksit satırları rapordan Nakit Akış Planlama'ya yazılır (aylık otomatik akış bu kayıt için kapalı)" + (pb != "TRY" ? $"; taksit {ilk.Tutar.ToString("N2", Tr)} {pb} × kur {kur.ToString("N4", Tr)} = TL karşılığı" : "");
            var r = DuzenliOdemeOrnek.Kaydet(k.DuzenliOdemeId, k.Banka, urun, k.Sirket, "Havale / EFT", "Kredi", gun, R2(ilk.Tutar * kur), not, kullanici, "SG-12", true);
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
                t.YevmiyeNo = Temiz(d.YevmiyeNo); t.MalikTuru = MalikTurleri.Contains(d.MalikTuru ?? "") ? d.MalikTuru : ""; t.AlacakStatusu = KonkordatoOrnek.Statuler.Contains(d.AlacakStatusu ?? "") ? d.AlacakStatusu : ""; t.AlacakId = Math.Max(0, d.AlacakId);
                if (t.Durum == "Fek Edildi" && t.FekTarihi == null) t.FekTarihi = DateTime.Today;
                Yaz(); kayit = t;
            }
            return null;
        }
        public static string TeminatSil(int id)
        {
            Yukle(); lock (Kilit) { var t = _teminatlar.FirstOrDefault(x => x.Id == id); if (t == null) return "Kayıt bulunamadı."; _teminatlar.Remove(t); Yaz(); } return null;
        }
        /// <summary>Konkordato tohumundaki ipotekler (10 Kas 2026): aynı kaynaklı kayıt yoksa eklenir; AlacakAnahtar → konkordato alacak kaydı. Eklenen sayısı döner.</summary>
        public static int TeminatTohumEkle(List<TeminatKaydi> l, Dictionary<string, int> eslem)
        {
            Yukle(); int n = 0;
            lock (Kilit)
            {
                foreach (var i in l ?? new List<TeminatKaydi>())
                {
                    if (i == null || _teminatlar.Any(t => t.Kaynak == i.Kaynak && t.Banka == i.Banka && t.TapuBilgisi == i.TapuBilgisi && t.Derece == i.Derece && t.Tutar == i.Tutar)) continue;
                    i.Id = ++_sonTeminatId; i.Olusturma = DateTime.Today; if (string.IsNullOrEmpty(i.Durum)) i.Durum = "Aktif"; if (string.IsNullOrEmpty(i.Tur)) i.Tur = "İpotek";
                    if (i.AlacakId == 0 && i.AlacakAnahtar != null && eslem != null && eslem.TryGetValue(i.AlacakAnahtar, out var aid)) i.AlacakId = aid;
                    _teminatlar.Add(i); n++;
                }
                if (n > 0) Yaz();
            }
            return n;
        }
        /// <summary>Konkordato alacak kaydına bağlı ipotek / rehin sayısı.</summary>
        public static int TeminatSayisi(int alacakId) { if (alacakId <= 0) return 0; Yukle(); lock (Kilit) return _teminatlar.Count(t => t.AlacakId == alacakId); }
        /// <summary>Konkordato alacak kaydına bağlı teminat çeki sayısı (10 Kas 2026).</summary>
        public static int CekSayisi(int alacakId) { if (alacakId <= 0) return 0; Yukle(); lock (Kilit) return _cekler.Count(c => c.AlacakId == alacakId); }
        /// <summary>Konkordato tohumundaki teminat çekleri (10 Kas 2026, "Teminat Çekleri sekmesini raporun verilerine göre doldur"): aynı kaynak + banka + çek no + tutar + şirket varsa atlanır; AlacakAnahtar → alacak kaydı.</summary>
        public static int CekTohumEkle(List<TeminatCeki> l, Dictionary<string, int> eslem)
        {
            Yukle(); int n = 0;
            lock (Kilit)
            {
                bool degisti = false;
                foreach (var c in l ?? new List<TeminatCeki>())
                {
                    if (c == null || string.IsNullOrWhiteSpace(c.CekNo)) continue;
                    var m = _cekler.FirstOrDefault(x => x.Kaynak == c.Kaynak && x.Banka == c.Banka && x.CekNo == c.CekNo && x.Tutar == c.Tutar && x.Sirket == c.Sirket);
                    if (m != null)
                    {   // sürüm yükseltme: var olan bilirkişi çekinin eksik alanları tamamlanır (kullanıcının değiştirdiği not korunur)
                        if (m.TahsilEdilen == 0 && c.TahsilEdilen > 0) { m.TahsilEdilen = c.TahsilEdilen; degisti = true; }
                        if (string.IsNullOrEmpty(m.AsilBorclu) && !string.IsNullOrEmpty(c.AsilBorclu)) { m.AsilBorclu = c.AsilBorclu; degisti = true; }
                        if (string.IsNullOrEmpty(m.Kesideci) && !string.IsNullOrEmpty(c.Kesideci)) { m.Kesideci = c.Kesideci; degisti = true; }
                        if (m.Mahsup != c.Mahsup) { m.Mahsup = c.Mahsup; degisti = true; }
                        if (m.AlacakId == 0 && c.AlacakAnahtar != null && eslem != null && eslem.TryGetValue(c.AlacakAnahtar, out var aid0)) { m.AlacakId = aid0; degisti = true; }
                        if (!string.IsNullOrEmpty(c.Not) && (string.IsNullOrEmpty(m.Not) || m.Not.StartsWith("Bilirkişi raporu")) && m.Not != c.Not) { m.Not = c.Not; degisti = true; }
                        if (string.IsNullOrEmpty(m.KrediRef) && !string.IsNullOrEmpty(c.KrediRef)) { m.KrediRef = c.KrediRef; degisti = true; }
                        continue;
                    }
                    c.Id = ++_sonCekId; c.Olusturma = DateTime.Today; if (string.IsNullOrEmpty(c.Durum)) c.Durum = "Tahsil Edildi"; if (string.IsNullOrEmpty(c.Belge)) c.Belge = "Çek"; if (string.IsNullOrEmpty(c.PB)) c.PB = "TRY";
                    if (c.AlacakId == 0 && c.AlacakAnahtar != null && eslem != null && eslem.TryGetValue(c.AlacakAnahtar, out var aid)) c.AlacakId = aid;
                    _cekler.Add(c); n++;
                }
                if (n > 0 || degisti) Yaz();
            }
            return n;
        }
        // ---------- teminata verilen çekler ----------
        public static string CekKaydet(CekIstek d, string kullanici, out TeminatCeki kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string sirket = Temiz(d.Sirket), banka = Temiz(d.Banka);
            if (sirket == "") return "Şirket seçilmelidir."; if (banka == "") return "Teminatın verildiği banka zorunludur.";
            if (Temiz(d.CekNo) == "") return "Çek / senet no zorunludur."; if (d.Tutar < 0) return "Tutar negatif olamaz.";
            lock (Kilit)
            {
                var c = d.Id > 0 ? _cekler.FirstOrDefault(x => x.Id == d.Id) : null; if (d.Id > 0 && c == null) return "Kayıt bulunamadı.";
                if (c == null) { c = new TeminatCeki { Id = ++_sonCekId, Olusturma = DateTime.Today, Olusturan = kullanici ?? "", Kaynak = "Ekran" }; _cekler.Add(c); }
                if (d.Tutar <= 0 && c.Tutar <= 0 && c.Kaynak == "Ekran") return "Tutar sıfırdan büyük olmalıdır.";   // bilirkişi kaynaklı tutarı bilinmeyen çekler 0 kalabilir
                c.Sirket = sirket; c.Banka = banka; c.KrediId = d.KrediId > 0 && _liste.Any(k => k.Id == d.KrediId) ? d.KrediId : 0; c.KrediRef = Temiz(d.KrediRef); c.AlacakId = Math.Max(0, d.AlacakId);
                c.AsilBorclu = Temiz(d.AsilBorclu); c.TahsilEdilen = R2(Math.Max(0, d.TahsilEdilen)); c.Mahsup = d.Mahsup;
                if (c.TahsilEdilen == 0 && d.Tutar > 0 && (d.Durum == "Tahsil Edildi")) c.TahsilEdilen = R2(d.Tutar);   // tahsil edildi işaretlenip tutar girilmediyse tamamı tahsil sayılır
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
        // ---------- banka borç dökümü (11 Kas 2026: Emlak Katılım · Alv Kimya / Daf Kimya; kredi kaydı değildir, nakit akışa yazmaz) ----------
        static decimal? R2n(decimal? v) => v.HasValue ? R2(v.Value) : (decimal?)null;
        static void BorcDoldur(BankaBorcSatiri b, BorcIstek d, string sirket, string banka, DateTime? vade)
        {
            b.Sirket = sirket; b.Banka = banka; b.SiraNo = Math.Max(0, d.SiraNo); b.Vade = vade; b.TaksitTuru = BorcTaksitTurleri.Contains(d.TaksitTuru ?? "") ? d.TaksitTuru : "Proje"; b.DetayTuru = Temiz(d.DetayTuru); b.EkNo = Temiz(d.EkNo);
            b.PB = string.IsNullOrWhiteSpace(d.PB) ? "TRY" : Temiz(d.PB).ToUpperInvariant(); if (b.PB == "TL") b.PB = "TRY";
            b.AnaPara = R2(d.AnaPara); b.TahsilEdilen = R2(d.TahsilEdilen); b.KalanAnaPara = R2n(d.KalanAnaPara); b.KalanAnaParaTL = R2n(d.KalanAnaParaTL); b.Kar = R2(d.Kar); b.Bsmv = R2(d.Bsmv); b.Borc = R2n(d.Borc);
            b.TahsilTutari = R2n(d.TahsilTutari); b.TahsilTutariTL = R2n(d.TahsilTutariTL); b.GecikmeCezasi = R2n(d.GecikmeCezasi); b.GecikmeCezasiTL = R2n(d.GecikmeCezasiTL); b.ToplamBorcTL = R2n(d.ToplamBorcTL); b.Not = Temiz(d.Not);
        }
        /// <summary>Borç dökümü tohumu (Data/Tohum/emlak-katilim-borc-2026-11.json): sürüm bayrağıyla bir kez uygulanır (kullanıcının sildiği satır geri gelmez); aynı şirket + banka + sıra no + vade + ek no varsa atlanır.
        /// Yukle() kilidi içinde çağrılır. Tohum dosyası yoksa bayrak ilerlemez (sonraki açılışta yeniden denenir).</summary>
        static void BorcTohumUygula()
        {
            try
            {
                string yol = string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "Data", "Tohum", "emlak-katilim-borc-2026-11.json");
                if (yol == null || !File.Exists(yol)) { Console.WriteLine("[Kredi] borç dökümü tohumu bulunamadı: " + (yol ?? "(içerik kökü tanımsız)")); return; }
                var tohum = JsonSerializer.Deserialize<BorcTohum>(File.ReadAllText(yol), Secenek) ?? new BorcTohum(); int n = 0, no = 0;
                if (_borcTohumSurumu < 1)   // sürüm 1: satırlar (sürüm yükseltmede yeniden eklenmez; kullanıcının sildiği satır geri gelmez)
                    foreach (var d in tohum.Satirlar ?? new List<BorcIstek>())
                    {
                        if (d == null) continue;
                        string sirket = Temiz(d.Sirket), banka = Temiz(d.Banka), ekNo = Temiz(d.EkNo); var vade = Tarih(d.Vade);
                        if (sirket == "" || banka == "" || vade == null) continue;
                        if (_borclar.Any(x => x.Sirket == sirket && x.Banka == banka && x.SiraNo == d.SiraNo && x.Vade == vade && x.EkNo == ekNo)) continue;
                        var b = new BankaBorcSatiri { Id = ++_sonBorcId, Olusturma = DateTime.Today, Olusturan = "Sistem", Kaynak = Temiz(tohum.Kaynak) };
                        BorcDoldur(b, d, sirket, banka, vade); _borclar.Add(b); n++;
                    }
                foreach (var o in tohum.Ozet ?? new List<BorcOzetSatiri>())   // sürüm 2: firma × döviz özeti (Yapılandırma Güncel); aynı firma + banka + döviz varsa boş alanı tamamlar
                {
                    if (o == null || Temiz(o.Sirket) == "") continue; string pb = Temiz(o.PB).ToUpperInvariant(); if (pb == "TL" || pb == "") pb = "TRY"; string banka = Temiz(o.Banka) == "" ? "Emlak Katılım" : Temiz(o.Banka);
                    var m = _borcOzet.FirstOrDefault(x => x.Sirket == Temiz(o.Sirket) && x.Banka == banka && x.PB == pb);
                    if (m != null) { if (m.YapilandirmaGuncel == null && o.YapilandirmaGuncel != null) { m.YapilandirmaGuncel = R2n(o.YapilandirmaGuncel); m.Not = Temiz(o.Not); m.Kaynak = Temiz(o.Kaynak) != "" ? Temiz(o.Kaynak) : Temiz(tohum.Kaynak); } continue; }
                    _borcOzet.Add(new BorcOzetSatiri { Id = ++_sonBorcOzetId, Sirket = Temiz(o.Sirket), Banka = banka, PB = pb, YapilandirmaGuncel = R2n(o.YapilandirmaGuncel), Not = Temiz(o.Not), Kaynak = Temiz(o.Kaynak) != "" ? Temiz(o.Kaynak) : Temiz(tohum.Kaynak) }); no++;
                }
                _borcTohumSurumu = BorcTohumSurum; Yaz();
                Console.WriteLine($"[Kredi] borç dökümü tohumu uygulandı (sürüm {BorcTohumSurum}): {n} satır, {no} özet satırı eklendi, toplam {_borclar.Count} satır / {_borcOzet.Count} özet · {Path.GetFileName(yol)}");
            }
            catch (Exception ex) { Console.WriteLine("[Kredi] borç dökümü tohumu okunamadı: " + ex.Message); }
        }
        public static string BorcKaydet(BorcIstek d, string kullanici, out BankaBorcSatiri kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string sirket = Temiz(d.Sirket), banka = Temiz(d.Banka); var vade = Tarih(d.Vade);
            if (sirket == "") return "Firma seçilmelidir."; if (banka == "") return "Banka zorunludur."; if (vade == null) return "Vade tarihi zorunludur.";
            decimal?[] tutarlar = { d.AnaPara, d.TahsilEdilen, d.KalanAnaPara, d.KalanAnaParaTL, d.Kar, d.Bsmv, d.Borc, d.TahsilTutari, d.TahsilTutariTL, d.GecikmeCezasi, d.GecikmeCezasiTL, d.ToplamBorcTL };
            if (tutarlar.Any(t => t < 0)) return "Tutarlar negatif olamaz.";
            if (d.AnaPara <= 0 && (d.Borc ?? 0) <= 0) return "Ana para ya da borç tutarı girilmelidir.";
            if (d.TahsilEdilen > d.AnaPara && d.AnaPara > 0) return "Tahsil edilmiş tutar ana parayı aşamaz.";
            lock (Kilit)
            {
                var b = d.Id > 0 ? _borclar.FirstOrDefault(x => x.Id == d.Id) : null; if (d.Id > 0 && b == null) return "Kayıt bulunamadı.";
                if (b == null) { b = new BankaBorcSatiri { Id = ++_sonBorcId, Olusturma = DateTime.Today, Olusturan = kullanici ?? "", Kaynak = "Ekran" }; _borclar.Add(b); }
                BorcDoldur(b, d, sirket, banka, vade);
                Yaz(); kayit = b;
            }
            return null;
        }
        public static string BorcSil(int id)
        {
            Yukle(); lock (Kilit) { var b = _borclar.FirstOrDefault(x => x.Id == id); if (b == null) return "Kayıt bulunamadı."; _borclar.Remove(b); Yaz(); } return null;
        }
        /// <summary>Firma × döviz özetinin Yapılandırma Güncel tutarını kaydeder (11 Kas 2026): firma + banka + döviz anahtarıyla; tutar null ise satır silinir.</summary>
        public static string BorcOzetKaydet(BorcOzetSatiri d, string kullanici, out BorcOzetSatiri kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string sirket = Temiz(d.Sirket), banka = Temiz(d.Banka) == "" ? "Emlak Katılım" : Temiz(d.Banka), pb = Temiz(d.PB).ToUpperInvariant(); if (pb == "TL" || pb == "") pb = "TRY";
            if (sirket == "") return "Firma zorunludur."; if (d.YapilandirmaGuncel < 0) return "Tutar negatif olamaz.";
            lock (Kilit)
            {
                var m = _borcOzet.FirstOrDefault(x => x.Sirket == sirket && x.Banka == banka && x.PB == pb);
                if (d.YapilandirmaGuncel == null) { if (m != null) { _borcOzet.Remove(m); Yaz(); } return null; }
                if (m == null) { m = new BorcOzetSatiri { Id = ++_sonBorcOzetId, Sirket = sirket, Banka = banka, PB = pb, Kaynak = "Ekran" }; _borcOzet.Add(m); }
                m.YapilandirmaGuncel = R2(d.YapilandirmaGuncel.Value); m.Not = Temiz(d.Not); if (m.Kaynak != "Ekran") m.Kaynak = (m.Kaynak ?? "") + " · düzenlendi (" + (kullanici ?? "") + ")";
                Yaz(); kayit = m;
            }
            return null;
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
            var l = Liste(); List<TeminatKaydi> tl; List<TeminatCeki> cl; List<KrediBelge> bl; List<BankaBorcSatiri> brl; List<BorcOzetSatiri> bol; lock (Kilit) { tl = _teminatlar.ToList(); cl = _cekler.ToList(); bl = _belgeler.ToList(); brl = _borclar.ToList(); bol = _borcOzet.ToList(); }
            var belgeSayac = bl.GroupBy(b => b.KrediId).ToDictionary(g => g.Key, g => g.Count());
            var konkordato = KonkordatoOrnek.Paket();   // kilit dışında (Konkordato → Kredi kilit sırası korunur)
            return new
            {
                success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), kalici = Dosya != null, dosya = Dosya, belgeKlasoru = BelgeKlasoru,
                sirketler = CekAkibetOrnek.Sirketler.Select(s => new { ad = s.Ad, db = s.DbName }).ToList(), bankalar = Bankalar, krediTurleri = KrediTurleri, teminatTurleri = TeminatTurleri, durumlar = Durumlar, oncekiDurumlar = OncekiDurumlar,
                teminatDurumlari = TeminatDurumlari, cekDurumlari = CekDurumlari, belgeTurleri = BelgeTurleri, malikTurleri = MalikTurleri, borcTaksitTurleri = BorcTaksitTurleri, paraBirimleri = ParaBirimleri, planTurleri = PlanTurleri, kurlar = NakitAkisOrnek.Kurlar,
                krediler = l.Select(k => Dto(k, belgeSayac.TryGetValue(k.Id, out var n) ? n : 0, cl.Count(c => c.KrediId == k.Id))).ToList(),
                teminatlar = tl.OrderBy(t => t.Durum == "Fek Edildi" ? 1 : 0).ThenBy(t => t.Banka).Select(t => new { t.Id, t.Sirket, t.Banka, t.KrediRef, t.KrediId, krediNo = l.FirstOrDefault(k => k.Id == t.KrediId)?.No ?? "", t.Tur, t.Aciklama, t.TapuBilgisi, t.Derece, t.Malik, t.Tutar, t.Ekspertiz, tesisTarihi = D(t.TesisTarihi), t.Durum, fekTarihi = D(t.FekTarihi), krediKapanis = D(t.KrediKapanis), t.Not, olusturma = D(t.Olusturma), t.Olusturan, belgeSayisi = bl.Count(b => b.TeminatId == t.Id), yevmiyeNo = t.YevmiyeNo ?? "", malikTuru = t.MalikTuru ?? "", alacakStatusu = t.AlacakStatusu ?? "", t.AlacakId, kaynak = t.Kaynak ?? "" }).ToList(),
                konkordato,
                cekler = cl.OrderBy(c => c.Vade).ThenBy(c => c.Id).Select(c => new { c.Id, c.Sirket, c.Banka, c.KrediId, krediNo = l.FirstOrDefault(k => k.Id == c.KrediId)?.No ?? "", c.KrediRef, c.Belge, c.CekNo, c.Kesideci, c.CekBankasi, vade = D(c.Vade), c.Tutar, c.PB, verilisTarihi = D(c.VerilisTarihi), c.Durum, iadeTarihi = D(c.IadeTarihi), c.Not, olusturma = D(c.Olusturma), c.Olusturan, belgeSayisi = bl.Count(b => b.CekId == c.Id), c.AlacakId, kaynak = c.Kaynak ?? "", asilBorclu = c.AsilBorclu ?? "", c.TahsilEdilen, kalan = Math.Max(0, c.Tutar - c.TahsilEdilen), c.Mahsup }).ToList(),
                belgeler = bl.OrderByDescending(b => b.Yukleme).Select(BelgeDto).ToList(),
                // banka borç dökümü (11 Kas 2026): boş (null) tutar = dökümde o sütun yok; "Tl" ekli alanlar dökümdeki ₺ karşılığı
                borclar = brl.OrderBy(b => b.Sirket).ThenBy(b => b.Banka).ThenBy(b => b.Vade).ThenBy(b => b.SiraNo).ThenBy(b => b.Id).Select(b => new { b.Id, b.Sirket, b.Banka, b.SiraNo, vade = D(b.Vade), b.TaksitTuru, b.DetayTuru, b.EkNo, b.PB, b.AnaPara, b.TahsilEdilen, b.KalanAnaPara, kalanAnaParaTl = b.KalanAnaParaTL, b.Kar, b.Bsmv, b.Borc, b.TahsilTutari, tahsilTutariTl = b.TahsilTutariTL, b.GecikmeCezasi, gecikmeCezasiTl = b.GecikmeCezasiTL, toplamBorcTl = b.ToplamBorcTL, not = b.Not ?? "", kaynak = b.Kaynak ?? "", olusturma = D(b.Olusturma), b.Olusturan }).ToList(),
                // firma × döviz özeti (11 Kas 2026): yalnız satırlardan türetilemeyen Yapılandırma Güncel tutarı (ÖZET sayfası); kalan sütunlar ekranda satırlardan hesaplanır
                borcOzet = bol.Select(o => new { o.Id, o.Sirket, o.Banka, o.PB, o.YapilandirmaGuncel, not = o.Not ?? "", kaynak = o.Kaynak ?? "" }).ToList(),
                kasaLink ="/Rapor79?sekme=havale", duzenliLink = "/Rapor123?sekme=duzenli"
            };
        }
        static object Dto(KrediKaydi k, int belgeSayisi = 0, int cekSayisi = 0)
        {
            var taksitler = Taksitler(k); var pesinatlar = Pesinatlar(k); var db = CekAkibetOrnek.DbName(k.Sirket); bool plan = PlanVar(k); decimal kur = KurUyg(k), pesinatToplam = PesinatToplam(k);
            var kasa = NakitAkisOrnek.KrediSatirlari(k.No + "|").GroupBy(m => m.U_KrediTaksit).ToDictionary(g => g.Key, g => g.First());
            decimal oncesiBorc = OncesiToplam(k), taksitToplam = taksitler.Sum(t => t.Tutar), geriOdeme = taksitToplam + pesinatToplam, faiz = taksitler.Sum(t => t.Faiz);
            decimal uygulanan = taksitler.Count > 0 ? taksitler[0].Tutar : 0;
            object KasaJson(NakitAkisViewModel m) => m == null ? null : new { docEntry = m.DocEntry, durum = m.U_Durum, odenme = D(m.U_OdenmeTarihi), db = m.U_KaynakFirma, link = $"/Rapor79?sirket={m.U_KaynakFirma}&sekme=havale" };
            var tl = taksitler.Select(t => { kasa.TryGetValue($"{k.No}|{t.No:000}", out var m); return new { no = t.No, tarih = D(t.Tarih), tutar = t.Tutar, anapara = t.Anapara, faiz = t.Faiz, kalan = t.Kalan, odendi = t.Odendi, aciklama = t.Aciklama ?? "", kasa = KasaJson(m) }; }).ToList();
            var pl = pesinatlar.Select(p => { kasa.TryGetValue(p.No == 1 ? $"{k.No}|P" : $"{k.No}|P{p.No}", out var m); return new { no = p.No, tarih = D(p.Tarih), tutar = p.Tutar, odendi = p.Odendi, aciklama = p.Aciklama ?? "", kasa = KasaJson(m) }; }).ToList();   // peşinat satırları (11 Kas 2026: protokolde birden çok peşin ödeme olabilir)
            kasa.TryGetValue($"{k.No}|P", out var pm);
            return new
            {
                id = k.Id, no = k.No, sirket = k.Sirket, db, banka = k.Banka, sube = k.Sube, krediNo = k.KrediNo, krediTuru = k.KrediTuru,
                pb = string.IsNullOrEmpty(k.PB) ? "TRY" : k.PB, kur, kurGirilen = k.Kur, borclular = k.Borclular ?? "", planVar = plan, odemePlani = (k.OdemePlani ?? new List<ProtokolOdeme>()).Select(p => new { tarih = D(p.Tarih), p.Tutar, p.Tur, p.Odendi, aciklama = p.Aciklama ?? "" }).ToList(), pesinatlar = pl,
                kullanimTarihi = D(k.KullanimTarihi), anapara = k.Anapara, faizOrani = k.FaizOrani, vade = k.Vade, taksit = k.Taksit, kalanAnapara = k.KalanAnapara, kalanVade = k.KalanVade, sonOdemeTarihi = D(k.SonOdemeTarihi),
                gecikenTaksit = k.GecikenTaksit, gecikenTutar = k.GecikenTutar, gecikmeFaizi = k.GecikmeFaizi, masraf = k.Masraf, oncekiDurum = k.OncekiDurum, oncesiToplamBorc = oncesiBorc, kar = k.Kar, bsmv = k.Bsmv,
                yapilandirmaTarihi = D(k.YapilandirmaTarihi), protokolNo = k.ProtokolNo, yeniAnapara = k.YeniAnapara, yeniFaizOrani = k.YeniFaizOrani, bsmvOrani = k.BsmvOrani, yeniVade = k.YeniVade, odemesizAy = k.OdemesizAy, yeniTaksit = k.YeniTaksit,
                hesaplananTaksit = HesaplananTaksit(k), uygulananTaksit = uygulanan, ilkTaksitTarihi = D(taksitler.Count > 0 ? taksitler[0].Tarih : k.IlkTaksitTarihi), sonTaksitTarihi = D(taksitler.Count > 0 ? taksitler[taksitler.Count - 1].Tarih : (DateTime?)null), odenenTaksit = k.OdenenTaksit, taksitSayisi = taksitler.Count, durum = k.Durum,
                pesinat = pesinatToplam, pesinatTarihi = pesinatlar.Count > 0 ? D(pesinatlar[0].Tarih) : null, pesinatOdendi = pesinatlar.Count > 0 && pesinatlar.All(p => p.Odendi), pesinatKasa = KasaJson(pm),
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
