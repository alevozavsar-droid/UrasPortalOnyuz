// ============================================================
// ŞİRKET DÖKÜMANLARI — tüm grup şirketlerinin resmî belgeleri tek yerde (Yönetim › Şirket Dökümanları; /SirketDokuman)
// Şirketler: Uras Baskı Teknolojileri, Uras Kimya, Alv Kimya, Selvi Kimya, Uras Holding, Avrupa Paper, Alv Filo, URS Makine, DRN.
// Kırılımlar (kategoriler; menüde her biri ayrı rapor): Kuruluş ve Sicil · Vergi ve Mali · İmza ve Yetki · Ruhsat, İzin ve Sertifika · Sözleşmeler ve Kiralar ·
//   Sigorta Poliçeleri · Banka ve Finans · Marka, Patent ve Tescil · Araç, Taşınmaz ve Demirbaş. Her kategoride belge TÜRLERİ tanımlıdır: zorunlu mu (her şirkette
//   bulunmalı), süreli mi (geçerlilik ayı), düzenleyen kurum, yalnız bazı şirketlerde mi (ör. araç ruhsatı Alv Filo).
// Belge: şirket × tür; belge no, düzenleyen kurum, düzenleme tarihi, geçerlilik bitişi, sorumlu, gizlilik, sürümler (dosya), günlük.
// Durum otomatik: Süresiz | Geçerli | Süresi Yaklaşıyor (≤ 60 gün) | Süresi Doldu; zorunlu türü olmayan şirket × tür = EKSİK.
// Dosyalar bellek içindedir; dosyası yüklenmemiş örnek belgeler için kapak sayfası PDF'i üretilir (Data/DisaAktar.cs).
// Ekran: Controllers/SirketDokumanController.cs + Views/SirketDokuman/Index.cshtml + wwwroot/js/sirket-dokuman.js
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Süresi Yaklaşıyor", "İmza Sirküleri", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class SdSirket { public string Kod { get; set; } public string Ad { get; set; } public string Unvan { get; set; } public string VergiNo { get; set; } public string VergiDairesi { get; set; } public string SicilNo { get; set; } public string Mersis { get; set; } public string Adres { get; set; } public int KurulusYili { get; set; } public string Faaliyet { get; set; } public string Yetkili { get; set; } public string Renk { get; set; } }
    public class SdKategori { public string Kod { get; set; } public string Ad { get; set; } public string Ikon { get; set; } public string Aciklama { get; set; } public string Action { get; set; } public string Sorumlu { get; set; } }
    public class SdTur { public string Kod { get; set; } public string Ad { get; set; } public string Kategori { get; set; } public bool Zorunlu { get; set; } public int? SureliAy { get; set; } public string Kurum { get; set; } public string[] Sadece { get; set; } public string Aciklama { get; set; } }
    public class SdSurum { public int No { get; set; } public DateTime Tarih { get; set; } public string Kullanici { get; set; } public string Dosya { get; set; } public long Boyut { get; set; } public string Tip { get; set; } public byte[] Veri { get; set; } public string Not { get; set; } = ""; }
    public class SdHareket { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }
    public class SdBelge
    {
        public int Id { get; set; } public string No { get; set; } public string SirketKod { get; set; } public string Kategori { get; set; } public string TurKod { get; set; } public string Ad { get; set; } public string BelgeNo { get; set; } = ""; public string Kurum { get; set; } = "";
        public DateTime DuzenlemeTarihi { get; set; } public DateTime? GecerlilikBitis { get; set; } public string Sorumlu { get; set; } = ""; public string Gizlilik { get; set; } = "Genel"; public string Not { get; set; } = ""; public bool Arsiv { get; set; }
        public List<SdSurum> Surumler { get; set; } = new List<SdSurum>(); public List<SdHareket> Hareketler { get; set; } = new List<SdHareket>();
        public int? KalanGun => GecerlilikBitis.HasValue ? (int?)(GecerlilikBitis.Value.Date - DateTime.Today).Days : null;
        public string Durum => Arsiv ? "Arşiv" : !GecerlilikBitis.HasValue ? "Süresiz" : KalanGun < 0 ? "Süresi Doldu" : KalanGun <= 60 ? "Süresi Yaklaşıyor" : "Geçerli";
        public SdSurum SonSurum => Surumler.OrderByDescending(s => s.No).FirstOrDefault();
    }
    public class SdBelgeIstek { public int? Id { get; set; } public string SirketKod { get; set; } public string TurKod { get; set; } public string Ad { get; set; } public string BelgeNo { get; set; } public string Kurum { get; set; } public string DuzenlemeTarihi { get; set; } public string GecerlilikBitis { get; set; } public string Sorumlu { get; set; } public string Gizlilik { get; set; } public string Not { get; set; } }
    public class SdYenileIstek { public int Id { get; set; } public string BelgeNo { get; set; } public string DuzenlemeTarihi { get; set; } public string GecerlilikBitis { get; set; } public string Not { get; set; } }

    public static class SirketDokumanOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public const int YaklasanGun = 60;
        public static readonly string[] Durumlar = { "Geçerli", "Süresi Yaklaşıyor", "Süresi Doldu", "Süresiz", "Arşiv" };
        public static readonly string[] Gizlilikler = { "Genel", "Kısıtlı", "Gizli" };

        public static readonly List<SdSirket> Sirketler = new List<SdSirket>
        {
            new SdSirket { Kod = "URASBASKI", Ad = "Uras Baskı Teknolojileri", Unvan = "Uras Baskı Teknolojileri San. ve Tic. A.Ş.", VergiNo = "8920041122", VergiDairesi = "Çorlu", SicilNo = "21455", Mersis = "0892004112200015", Adres = "Çorlu / Tekirdağ", KurulusYili = 2015, Faaliyet = "Dijital baskı makineleri ve sarf malzemeleri", Yetkili = "Mehmet Kaya", Renk = "#0f766e" },
            new SdSirket { Kod = "URASKIMYA", Ad = "Uras Kimya", Unvan = "Uras Kimya San. ve Tic. A.Ş.", VergiNo = "8920034567", VergiDairesi = "Çorlu", SicilNo = "11208", Mersis = "0892003456700011", Adres = "Velimeşe OSB, Ergene / Tekirdağ", KurulusYili = 1998, Faaliyet = "Tekstil baskı kimyasalları üretimi", Yetkili = "Alev Özavşar", Renk = "#1d4ed8" },
            new SdSirket { Kod = "ALVKIMYA", Ad = "Alv Kimya", Unvan = "Alv Kimya San. ve Tic. A.Ş.", VergiNo = "0560045678", VergiDairesi = "Çerkezköy", SicilNo = "14870", Mersis = "0056004567800012", Adres = "Çerkezköy OSB, Kapaklı / Tekirdağ", KurulusYili = 2006, Faaliyet = "Binder ve tekstil yardımcı kimyasalları üretimi", Yetkili = "Kerem Aksoy", Renk = "#0e7490" },
            new SdSirket { Kod = "SELVI", Ad = "Selvi Kimya", Unvan = "Selvi Kimya San. ve Tic. Ltd. Şti.", VergiNo = "7590012345", VergiDairesi = "Çorlu", SicilNo = "13302", Mersis = "0759001234500019", Adres = "Çorlu / Tekirdağ", KurulusYili = 2003, Faaliyet = "Kimyasal ve süblimasyon kağıdı ticareti", Yetkili = "Neslihan Uludağ", Renk = "#b45309" },
            new SdSirket { Kod = "URASHOLDING", Ad = "Uras Holding", Unvan = "Uras Holding A.Ş.", VergiNo = "8920098765", VergiDairesi = "Kadıköy", SicilNo = "745120", Mersis = "0892009876500017", Adres = "Kadıköy / İstanbul", KurulusYili = 2010, Faaliyet = "Holding; grup şirketlerine yönetim hizmeti", Yetkili = "Alev Özavşar", Renk = "#4338ca" },
            new SdSirket { Kod = "AVRUPAPAPER", Ad = "Avrupa Paper", Unvan = "Avrupa Paper Kağıt San. ve Tic. A.Ş.", VergiNo = "1230056789", VergiDairesi = "Çorlu", SicilNo = "18990", Mersis = "0123005678900013", Adres = "Çorlu / Tekirdağ", KurulusYili = 2012, Faaliyet = "Süblimasyon kağıdı üretimi", Yetkili = "Gizem Tan", Renk = "#7c3aed" },
            new SdSirket { Kod = "ALVFILO", Ad = "Alv Filo", Unvan = "Alv Filo Araç Kiralama Ltd. Şti.", VergiNo = "0560087654", VergiDairesi = "Çerkezköy", SicilNo = "19877", Mersis = "0056008765400014", Adres = "Çerkezköy / Tekirdağ", KurulusYili = 2016, Faaliyet = "Araç kiralama ve filo yönetimi", Yetkili = "Serkan Genç", Renk = "#be123c" },
            new SdSirket { Kod = "URSMAKINE", Ad = "URS Makine", Unvan = "URS Makine San. ve Tic. A.Ş.", VergiNo = "8920076543", VergiDairesi = "Çorlu", SicilNo = "15644", Mersis = "0892007654300016", Adres = "Çorlu / Tekirdağ", KurulusYili = 2008, Faaliyet = "Baskı makineleri imalatı", Yetkili = "Ali Yılmaz", Renk = "#475569" },
            new SdSirket { Kod = "DRN", Ad = "DRN", Unvan = "DRN Dış Ticaret Ltd. Şti.", VergiNo = "3110065432", VergiDairesi = "Beşiktaş", SicilNo = "812330", Mersis = "0311006543200018", Adres = "Beşiktaş / İstanbul", KurulusYili = 2019, Faaliyet = "Dış ticaret ve aracılık", Yetkili = "Esra Kaya", Renk = "#15803d" }
        };
        public static SdSirket SirketBul(string kod) => Sirketler.FirstOrDefault(s => string.Equals(s.Kod, kod, StringComparison.OrdinalIgnoreCase) || string.Equals(s.Ad, kod, StringComparison.OrdinalIgnoreCase));

        public static readonly List<SdKategori> Kategoriler = new List<SdKategori>
        {
            new SdKategori { Kod = "kurulus", Ad = "Kuruluş ve Sicil Belgeleri", Ikon = "fa-landmark", Action = "Kurulus", Sorumlu = "Hukuk · Av. Deniz Arslan", Aciklama = "Ana sözleşme, ticaret sicil gazeteleri, faaliyet ve oda kayıt belgeleri, genel kurul / yönetim kurulu kararları." },
            new SdKategori { Kod = "vergi", Ad = "Vergi ve Mali Belgeler", Ikon = "fa-file-invoice-dollar", Action = "Vergi", Sorumlu = "Muhasebe · Ayşe Demir", Aciklama = "Vergi levhası, borcu yoktur yazıları, mükellefiyet, bağımsız denetim ve YMM raporları, beyannameler, e-defter beratları." },
            new SdKategori { Kod = "imza", Ad = "İmza ve Yetki Belgeleri", Ikon = "fa-signature", Action = "Imza", Sorumlu = "Hukuk · Av. Deniz Arslan", Aciklama = "İmza sirküleri, temsil ve ilzam kararları, vekâletnameler, yetki belgeleri." },
            new SdKategori { Kod = "ruhsat", Ad = "Ruhsat, İzin ve Sertifikalar", Ikon = "fa-certificate", Action = "Ruhsat", Sorumlu = "Kalite · Burak Şen", Aciklama = "İş yeri açma ruhsatı, çevre izni, kapasite raporu, sanayi sicil, ISO sertifikaları, gümrük / ihracat statü belgeleri." },
            new SdKategori { Kod = "sozlesme", Ad = "Sözleşmeler ve Kiralar", Ikon = "fa-file-contract", Action = "Sozlesme", Sorumlu = "İdari İşler · Serkan Genç", Aciklama = "Kira, tedarik, bayilik, hizmet, enerji ve gizlilik sözleşmeleri." },
            new SdKategori { Kod = "sigorta", Ad = "Sigorta Poliçeleri", Ikon = "fa-shield-halved", Action = "Sigorta", Sorumlu = "Finans · Kerem Aksoy", Aciklama = "İş yeri paket, nakliyat, sorumluluk, makine kırılması, alacak sigortası, araç poliçeleri." },
            new SdKategori { Kod = "banka", Ad = "Banka ve Finans Belgeleri", Ikon = "fa-building-columns", Action = "Banka", Sorumlu = "Finans · Kerem Aksoy", Aciklama = "Banka imza yetki formları, kredi ve leasing sözleşmeleri, teminat mektupları, Findeks raporu." },
            new SdKategori { Kod = "marka", Ad = "Marka, Patent ve Tescil Belgeleri", Ikon = "fa-trademark", Action = "Marka", Sorumlu = "Hukuk · Av. Deniz Arslan", Aciklama = "Marka tescilleri, tasarım ve patent belgeleri, alan adları, GS1 barkod üyeliği." },
            new SdKategori { Kod = "varlik", Ad = "Araç, Taşınmaz ve Demirbaş Belgeleri", Ikon = "fa-warehouse", Action = "Varlik", Sorumlu = "İdari İşler · Serkan Genç", Aciklama = "Tapular, yapı kullanma izni, enerji kimlik belgesi, araç ruhsat ve muayeneleri." }
        };
        public static SdKategori KategoriBul(string kod) => Kategoriler.FirstOrDefault(k => string.Equals(k.Kod, kod, StringComparison.OrdinalIgnoreCase) || string.Equals(k.Action, kod, StringComparison.OrdinalIgnoreCase));
        static readonly string[] Uretici = { "URASKIMYA", "ALVKIMYA", "AVRUPAPAPER", "URSMAKINE", "URASBASKI" };
        static readonly string[] Ihracatci = { "URASKIMYA", "ALVKIMYA", "SELVI", "AVRUPAPAPER", "URSMAKINE", "DRN", "URASBASKI" };
        public static readonly List<SdTur> Turler = new List<SdTur>
        {
            // kuruluş ve sicil
            T("ana-sozlesme", "Ana Sözleşme (Güncel)", "kurulus", true, null, "Ticaret Sicil Müdürlüğü"), T("sicil-gazetesi-kurulus", "Ticaret Sicil Gazetesi · Kuruluş", "kurulus", true, null, "Türkiye Ticaret Sicili Gazetesi"), T("sicil-gazetesi-son", "Ticaret Sicil Gazetesi · Son Değişiklik", "kurulus", true, null, "Türkiye Ticaret Sicili Gazetesi"),
            T("faaliyet-belgesi", "Faaliyet Belgesi", "kurulus", true, 12, "Ticaret / Sanayi Odası"), T("oda-kayit", "Oda Kayıt Belgesi", "kurulus", true, 12, "Ticaret / Sanayi Odası"), T("genel-kurul", "Genel Kurul Kararı ve Hazirun Cetveli", "kurulus", true, 12, "Şirket"), T("yk-karari", "Yönetim Kurulu / Ortaklar Kurulu Karar Defteri", "kurulus", true, null, "Şirket"),
            // vergi ve mali
            T("vergi-levhasi", "Vergi Levhası", "vergi", true, 12, "Gelir İdaresi Başkanlığı"), T("vergi-borcu-yok", "Vergi Borcu Yoktur Yazısı", "vergi", true, 1, "Vergi Dairesi"), T("sgk-borcu-yok", "SGK Borcu Yoktur Yazısı", "vergi", true, 1, "SGK"), T("mukellefiyet", "Mükellefiyet Yazısı", "vergi", false, 12, "Vergi Dairesi"),
            T("bagimsiz-denetim", "Bağımsız Denetim Raporu", "vergi", false, 12, "Bağımsız Denetim Kuruluşu", new[] { "URASKIMYA", "URASHOLDING", "ALVKIMYA", "AVRUPAPAPER" }), T("ymm-tasdik", "YMM Tam Tasdik Raporu", "vergi", false, 12, "YMM", Uretici), T("kurumlar-beyan", "Kurumlar Vergisi Beyannamesi ve Tahakkuk", "vergi", true, 12, "GİB"), T("e-defter-berat", "E-Defter Beratları", "vergi", true, 12, "GİB"),
            // imza ve yetki
            T("imza-sirkuleri", "İmza Sirküleri", "imza", true, 36, "Noter"), T("temsil-ilzam", "Temsil ve İlzam Kararı", "imza", true, null, "Şirket / Ticaret Sicil"), T("vekaletname-gumruk", "Gümrük Müşavirliği Vekâletnamesi", "imza", false, 24, "Noter", Ihracatci), T("vekaletname-genel", "Genel Vekâletname", "imza", false, 24, "Noter"), T("yetkili-kimlik", "Yetkili Kimlik ve İkametgâh", "imza", true, null, "Nüfus Müdürlüğü"),
            // ruhsat, izin, sertifika
            T("isyeri-ruhsat", "İş Yeri Açma ve Çalışma Ruhsatı", "ruhsat", true, null, "Belediye / OSB", Uretici), T("cevre-izin", "Çevre İzin ve Lisans Belgesi", "ruhsat", true, 60, "Çevre, Şehircilik ve İklim Değişikliği Bakanlığı", new[] { "URASKIMYA", "ALVKIMYA", "AVRUPAPAPER" }), T("kapasite-raporu", "Kapasite Raporu", "ruhsat", true, 24, "Sanayi Odası", Uretici), T("sanayi-sicil", "Sanayi Sicil Belgesi", "ruhsat", true, 24, "Sanayi ve Teknoloji Bakanlığı", Uretici),
            T("iso-9001", "ISO 9001 Kalite Yönetim Sertifikası", "ruhsat", false, 36, "Belgelendirme Kuruluşu", Uretici), T("iso-14001", "ISO 14001 Çevre Yönetim Sertifikası", "ruhsat", false, 36, "Belgelendirme Kuruluşu", new[] { "URASKIMYA", "ALVKIMYA", "AVRUPAPAPER" }), T("iso-45001", "ISO 45001 İSG Sertifikası", "ruhsat", false, 36, "Belgelendirme Kuruluşu", Uretici),
            T("emisyon-olcum", "Emisyon Ölçüm Raporu", "ruhsat", false, 24, "Yetkili Laboratuvar", new[] { "URASKIMYA", "ALVKIMYA", "AVRUPAPAPER" }), T("itfaiye-raporu", "Yangın Yeterlilik / İtfaiye Raporu", "ruhsat", true, 12, "İtfaiye", Uretici), T("tmfb", "Tehlikeli Madde Faaliyet Belgesi", "ruhsat", false, 60, "Ulaştırma Bakanlığı", new[] { "URASKIMYA", "ALVKIMYA" }),
            T("ihracatci-birligi", "İhracatçı Birliği Üyelik Belgesi", "ruhsat", false, 12, "İhracatçı Birlikleri", Ihracatci), T("yys", "Yetkilendirilmiş Yükümlü / OKS Belgesi", "ruhsat", false, 36, "Ticaret Bakanlığı", new[] { "URASKIMYA", "SELVI" }), T("isg-sozlesme", "İSG Uzmanı ve İşyeri Hekimi Sözleşmesi", "ruhsat", true, 12, "OSGB"), T("k1-yetki", "K1 / K2 Yetki Belgesi", "ruhsat", true, 60, "Ulaştırma Bakanlığı", new[] { "ALVFILO" }),
            // sözleşmeler ve kiralar
            T("kira-fabrika", "Kira Sözleşmesi · Fabrika / Depo", "sozlesme", false, 36, "Kiraya Veren", new[] { "SELVI", "AVRUPAPAPER", "URSMAKINE", "URASBASKI", "ALVFILO" }), T("kira-ofis", "Kira Sözleşmesi · Ofis", "sozlesme", false, 24, "Kiraya Veren", new[] { "URASHOLDING", "DRN", "URASBASKI" }), T("enerji-sozlesme", "Elektrik / Doğalgaz Tedarik Sözleşmesi", "sozlesme", true, 12, "Enerji Tedarikçisi", Uretici),
            T("hizmet-guvenlik", "Güvenlik Hizmeti Sözleşmesi", "sozlesme", false, 12, "Güvenlik Firması", Uretici), T("hizmet-catering", "Catering Hizmeti Sözleşmesi", "sozlesme", false, 12, "Catering Firması", Uretici), T("hizmet-servis", "Personel Servisi Sözleşmesi", "sozlesme", false, 12, "Servis Firması", Uretici), T("hizmet-it", "Bilgi Sistemleri / Bulut Hizmet Sözleşmesi", "sozlesme", true, 12, "Hizmet Sağlayıcı"),
            T("bayilik", "Bayilik / Distribütörlük Sözleşmeleri", "sozlesme", false, 24, "Karşı Taraf", new[] { "SELVI", "URASKIMYA", "URASBASKI", "DRN" }), T("nda", "Gizlilik Sözleşmeleri (NDA)", "sozlesme", false, null, "Karşı Taraf"), T("tedarik-cerceve", "Tedarik Çerçeve Sözleşmeleri", "sozlesme", false, 12, "Tedarikçi", Uretici),
            // sigorta
            T("isyeri-paket", "İş Yeri Paket / Yangın Sigortası", "sigorta", true, 12, "Sigorta Şirketi", Uretici), T("isveren-sorumluluk", "İşveren Mali Sorumluluk Sigortası", "sigorta", true, 12, "Sigorta Şirketi"), T("urun-sorumluluk", "Ürün Sorumluluk Sigortası", "sigorta", false, 12, "Sigorta Şirketi", new[] { "URASKIMYA", "ALVKIMYA", "URSMAKINE" }),
            T("nakliyat-abonman", "Nakliyat Abonman Poliçesi", "sigorta", false, 12, "Sigorta Şirketi", Ihracatci), T("makine-kirilmasi", "Makine Kırılması Sigortası", "sigorta", false, 12, "Sigorta Şirketi", Uretici), T("alacak-sigortasi", "Ticari Alacak (İhracat) Sigortası", "sigorta", false, 12, "Türk Eximbank / Sigorta Şirketi", new[] { "URASKIMYA", "SELVI", "AVRUPAPAPER" }),
            T("kasko-trafik", "Kasko ve Trafik Poliçeleri (Filo)", "sigorta", true, 12, "Sigorta Şirketi", new[] { "ALVFILO" }), T("cevre-sorumluluk", "Çevre Kirliliği Sorumluluk Sigortası", "sigorta", false, 12, "Sigorta Şirketi", new[] { "URASKIMYA", "ALVKIMYA" }),
            // banka ve finans
            T("banka-imza-yetki", "Banka İmza Yetki Formları", "banka", true, null, "Banka"), T("kredi-sozlesme", "Genel Kredi Sözleşmeleri", "banka", false, null, "Banka", new[] { "URASKIMYA", "ALVKIMYA", "SELVI", "AVRUPAPAPER", "URSMAKINE", "ALVFILO" }), T("teminat-mektubu", "Teminat Mektupları", "banka", false, 12, "Banka", Uretici),
            T("leasing", "Leasing Sözleşmeleri", "banka", false, 48, "Leasing Şirketi", new[] { "ALVFILO", "URSMAKINE", "AVRUPAPAPER" }), T("findeks", "Findeks Risk Raporu", "banka", false, 3, "KKB", null), T("banka-teyit", "Banka Teyit / Referans Yazısı", "banka", false, 6, "Banka"),
            // marka, patent, tescil
            T("marka-tescil", "Marka Tescil Belgeleri", "marka", false, 120, "Türk Patent ve Marka Kurumu", new[] { "URASKIMYA", "ALVKIMYA", "SELVI", "AVRUPAPAPER", "URASBASKI", "URSMAKINE" }), T("tasarim-tescil", "Endüstriyel Tasarım Tescili", "marka", false, 60, "Türk Patent ve Marka Kurumu", new[] { "URSMAKINE", "URASBASKI" }), T("patent", "Patent / Faydalı Model", "marka", false, 240, "Türk Patent ve Marka Kurumu", new[] { "URASKIMYA", "URSMAKINE" }),
            T("alan-adi", "Alan Adı Kayıtları", "marka", true, 12, "Kayıt Operatörü"), T("gs1", "GS1 Barkod Üyeliği", "marka", false, 12, "GS1 Türkiye", Uretici),
            // araç, taşınmaz, demirbaş
            T("tapu", "Tapu Senetleri", "varlik", false, null, "Tapu Müdürlüğü", new[] { "URASKIMYA", "ALVKIMYA", "URASHOLDING" }), T("yapi-kullanma", "Yapı Kullanma İzin Belgesi", "varlik", false, null, "Belediye / OSB", new[] { "URASKIMYA", "ALVKIMYA" }), T("enerji-kimlik", "Enerji Kimlik Belgesi", "varlik", false, 120, "Yetkili Kuruluş", new[] { "URASKIMYA", "ALVKIMYA", "URASHOLDING" }),
            T("arac-ruhsat", "Araç Ruhsatları (Filo)", "varlik", true, null, "Emniyet / Noter", new[] { "ALVFILO" }), T("arac-muayene", "Araç Muayene Belgeleri", "varlik", true, 12, "TÜVTÜRK", new[] { "ALVFILO" }), T("demirbas-listesi", "Demirbaş ve Sabit Kıymet Listesi", "varlik", true, 12, "Muhasebe")
        };
        static SdTur T(string kod, string ad, string kategori, bool zorunlu, int? sureliAy, string kurum, string[] sadece = null) => new SdTur { Kod = kod, Ad = ad, Kategori = kategori, Zorunlu = zorunlu, SureliAy = sureliAy, Kurum = kurum, Sadece = sadece };
        public static SdTur TurBul(string kod) => Turler.FirstOrDefault(t => string.Equals(t.Kod, kod, StringComparison.OrdinalIgnoreCase));
        public static bool TurGecerli(SdTur t, string sirketKod) => t.Sadece == null || t.Sadece.Contains(sirketKod);

        private static readonly List<SdBelge> _belgeler = new List<SdBelge>();
        private static int _sonId; private static bool _tohumlandi;

        // ---------- tohum ----------
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var rnd = new Random(20261030); var bugun = DateTime.Today;
                string[] kisiler = { "Av. Deniz Arslan", "Ayşe Demir", "Burak Şen", "Serkan Genç", "Kerem Aksoy", "Ali Veli" };
                foreach (var s in Sirketler)
                    foreach (var t in Turler.Where(t => TurGecerli(t, s.Kod)))
                    {
                        // zorunlu türlerin ~%8'i bilerek eksik (ekranda EKSİK uyarısı); isteğe bağlı türler %60
                        if (t.Zorunlu ? rnd.NextDouble() < 0.08 : rnd.NextDouble() > 0.6) continue;
                        var kat = KategoriBul(t.Kategori); var duz = bugun.AddDays(-rnd.Next(30, t.SureliAy.HasValue ? Math.Max(60, t.SureliAy.Value * 30 + 40) : 1500));
                        DateTime? bitis = t.SureliAy.HasValue ? duz.AddMonths(t.SureliAy.Value) : (DateTime?)null;
                        var b = new SdBelge { Id = ++_sonId, No = $"DOK-{bugun.Year}-{_sonId:0000}", SirketKod = s.Kod, Kategori = t.Kategori, TurKod = t.Kod, Ad = t.Ad, BelgeNo = BelgeNoUret(t, s, rnd), Kurum = t.Kurum, DuzenlemeTarihi = duz, GecerlilikBitis = bitis, Sorumlu = kat.Sorumlu, Gizlilik = t.Kategori == "banka" || t.Kategori == "imza" ? "Kısıtlı" : "Genel", Not = "" };
                        int surum = rnd.NextDouble() < 0.3 ? 2 : 1;
                        for (int v = 1; v <= surum; v++) b.Surumler.Add(new SdSurum { No = v, Tarih = v == surum ? duz.AddDays(rnd.Next(0, 6)).AddHours(10) : duz.AddMonths(-(t.SureliAy ?? 12)).AddHours(11), Kullanici = kisiler[rnd.Next(kisiler.Length)], Dosya = DosyaAdi(b, v), Boyut = rnd.Next(80, 2400) * 1024L, Tip = "application/pdf", Not = v == surum && surum > 1 ? "Yenilenen belge" : "İlk yükleme" });
                        foreach (var v in b.Surumler.OrderBy(x => x.No)) b.Hareketler.Add(new SdHareket { Zaman = v.Tarih, Kullanici = v.Kullanici, Islem = v.No == 1 ? "Belge Eklendi" : "Yeni Sürüm", Aciklama = $"{v.Dosya} · {(v.Boyut / 1024)} KB" + (bitis != null && v.No == surum ? $" · geçerlilik {bitis:dd.MM.yyyy}" : "") });
                        _belgeler.Add(b);
                    }
            }
        }
        static string BelgeNoUret(SdTur t, SdSirket s, Random rnd) => t.Kategori == "kurulus" && t.Kod.StartsWith("sicil") ? $"TTSG {rnd.Next(9800, 11200)} / {rnd.Next(100, 999)}" : t.Kod == "vergi-levhasi" ? s.VergiNo : t.Kod == "imza-sirkuleri" ? $"Noter {rnd.Next(1, 40)} · Y.No {rnd.Next(10000, 49999)}" : t.Kategori == "sigorta" ? $"POL-{rnd.Next(100000000, 999999999)}" : t.Kategori == "marka" ? $"{DateTime.Today.Year - rnd.Next(1, 9)}/{rnd.Next(10000, 99999)}" : t.Kategori == "ruhsat" ? $"{rnd.Next(1000, 9999)}-{rnd.Next(10, 99)}" : $"{s.Kod.Substring(0, Math.Min(3, s.Kod.Length))}-{rnd.Next(1000, 9999)}";
        static string DosyaAdi(SdBelge b, int v) => $"{b.SirketKod.ToLowerInvariant()}-{b.TurKod}-v{v}.pdf";

        // ---------- erişim ----------
        public static List<SdBelge> Belgeler { get { Tohumla(); lock (Kilit) return _belgeler.ToList(); } }
        public static SdBelge Bul(int id) { Tohumla(); lock (Kilit) return _belgeler.FirstOrDefault(b => b.Id == id); }
        /// <summary>Zorunlu olduğu halde belgesi bulunmayan şirket × tür çiftleri (arşivdekiler sayılmaz).</summary>
        public static List<(SdSirket Sirket, SdTur Tur)> Eksikler()
        {
            var l = Belgeler.Where(b => !b.Arsiv).ToList(); var r = new List<(SdSirket, SdTur)>();
            foreach (var s in Sirketler) foreach (var t in Turler.Where(t => t.Zorunlu && TurGecerli(t, s.Kod))) if (!l.Any(b => b.SirketKod == s.Kod && b.TurKod == t.Kod)) r.Add((s, t));
            return r;
        }
        static void Log(SdBelge b, string islem, string aciklama, string kullanici) => b.Hareketler.Add(new SdHareket { Zaman = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });

        public static (bool ok, string mesaj, SdBelge b) Kaydet(SdBelgeIstek i, string dosyaAdi, string icerikTipi, byte[] veri, string kullanici)
        {
            Tohumla(); if (i == null) return (false, "Geçersiz istek.", null);
            var s = SirketBul(i.SirketKod ?? ""); if (s == null) return (false, "Şirket seçin.", null);
            var t = TurBul(i.TurKod ?? ""); if (t == null) return (false, "Belge türü seçin.", null); if (!TurGecerli(t, s.Kod)) return (false, $"{t.Ad} türü {s.Ad} için tanımlı değil.", null);
            if (!DateTime.TryParse(i.DuzenlemeTarihi, out var duz)) return (false, "Düzenleme tarihi girin.", null);
            DateTime? bit = DateTime.TryParse(i.GecerlilikBitis, out var b0) ? b0.Date : (DateTime?)null; if (bit != null && bit < duz.Date) return (false, "Geçerlilik bitişi düzenleme tarihinden önce olamaz.", null);
            if (veri != null && veri.LongLength > 12_000_000) return (false, "Dosya 12 MB'ı aşamaz.", null);
            lock (Kilit)
            {
                var b = i.Id.HasValue ? _belgeler.FirstOrDefault(x => x.Id == i.Id.Value) : null; bool yeni = b == null;
                if (yeni) { b = new SdBelge { Id = ++_sonId, No = $"DOK-{DateTime.Today.Year}-{_sonId:0000}" }; _belgeler.Add(b); }
                b.SirketKod = s.Kod; b.Kategori = t.Kategori; b.TurKod = t.Kod; b.Ad = string.IsNullOrWhiteSpace(i.Ad) ? t.Ad : i.Ad.Trim(); b.BelgeNo = (i.BelgeNo ?? "").Trim(); b.Kurum = string.IsNullOrWhiteSpace(i.Kurum) ? t.Kurum : i.Kurum.Trim(); b.DuzenlemeTarihi = duz.Date; b.GecerlilikBitis = bit; b.Sorumlu = string.IsNullOrWhiteSpace(i.Sorumlu) ? KategoriBul(t.Kategori).Sorumlu : i.Sorumlu.Trim(); b.Gizlilik = Gizlilikler.Contains(i.Gizlilik ?? "") ? i.Gizlilik : "Genel"; b.Not = (i.Not ?? "").Trim();
                if (veri != null && veri.Length > 0) { int no = (b.SonSurum?.No ?? 0) + 1; b.Surumler.Add(new SdSurum { No = no, Tarih = DateTime.Now, Kullanici = kullanici, Dosya = dosyaAdi, Boyut = veri.LongLength, Tip = icerikTipi ?? "application/octet-stream", Veri = veri, Not = yeni ? "İlk yükleme" : "Yeni sürüm" }); }
                Log(b, yeni ? "Belge Eklendi" : "Güncellendi", $"{s.Ad} · {b.Ad}" + (b.BelgeNo != "" ? " · " + b.BelgeNo : "") + (bit != null ? $" · geçerlilik {bit:dd.MM.yyyy}" : " · süresiz") + (veri != null && veri.Length > 0 ? $" · dosya {dosyaAdi}" : ""), kullanici);
                return (true, $"{b.No} {(yeni ? "eklendi" : "güncellendi")}: {s.Ad} · {b.Ad}" + (veri != null && veri.Length > 0 ? $" · {dosyaAdi} yüklendi" : "") + ".", b);
            }
        }
        public static (bool ok, string mesaj, SdBelge b) SurumEkle(int id, string dosyaAdi, string icerikTipi, byte[] veri, string not, string kullanici)
        {
            var b = Bul(id); if (b == null) return (false, "Belge bulunamadı.", null); if (veri == null || veri.Length == 0) return (false, "Dosya seçin.", b); if (veri.LongLength > 12_000_000) return (false, "Dosya 12 MB'ı aşamaz.", b);
            lock (Kilit) { int no = (b.SonSurum?.No ?? 0) + 1; b.Surumler.Add(new SdSurum { No = no, Tarih = DateTime.Now, Kullanici = kullanici, Dosya = dosyaAdi, Boyut = veri.LongLength, Tip = icerikTipi ?? "application/octet-stream", Veri = veri, Not = (not ?? "").Trim() }); Log(b, "Yeni Sürüm", $"v{no} · {dosyaAdi} · {veri.LongLength / 1024} KB" + (string.IsNullOrWhiteSpace(not) ? "" : " · " + not.Trim()), kullanici); }
            return (true, $"{b.No}: v{b.SonSurum.No} yüklendi ({dosyaAdi}).", b);
        }
        /// <summary>Yenileme: yeni belge no / düzenleme / geçerlilik; eski geçerlilik günlüğe yazılır.</summary>
        public static (bool ok, string mesaj, SdBelge b) Yenile(SdYenileIstek i, string kullanici)
        {
            var b = Bul(i?.Id ?? 0); if (b == null) return (false, "Belge bulunamadı.", null);
            if (!DateTime.TryParse(i.DuzenlemeTarihi, out var duz)) duz = DateTime.Today; DateTime? bit = DateTime.TryParse(i.GecerlilikBitis, out var b0) ? b0.Date : (DateTime?)null;
            var t = TurBul(b.TurKod); if (bit == null && t?.SureliAy != null) bit = duz.Date.AddMonths(t.SureliAy.Value); if (bit != null && bit < duz.Date) return (false, "Geçerlilik bitişi düzenleme tarihinden önce olamaz.", b);
            lock (Kilit) { string eski = b.GecerlilikBitis?.ToString("dd.MM.yyyy") ?? "süresiz"; b.DuzenlemeTarihi = duz.Date; b.GecerlilikBitis = bit; if (!string.IsNullOrWhiteSpace(i.BelgeNo)) b.BelgeNo = i.BelgeNo.Trim(); b.Arsiv = false; Log(b, "Yenilendi", $"geçerlilik {eski} → {(bit?.ToString("dd.MM.yyyy") ?? "süresiz")}" + (string.IsNullOrWhiteSpace(i.BelgeNo) ? "" : " · belge no " + i.BelgeNo.Trim()) + (string.IsNullOrWhiteSpace(i.Not) ? "" : " · " + i.Not.Trim()), kullanici); }
            return (true, $"{b.No} yenilendi · geçerlilik {(bit?.ToString("dd.MM.yyyy") ?? "süresiz")}.", b);
        }
        public static (bool ok, string mesaj, SdBelge b) Arsivle(int id, bool arsiv, string kullanici)
        {
            var b = Bul(id); if (b == null) return (false, "Belge bulunamadı.", null);
            lock (Kilit) { b.Arsiv = arsiv; Log(b, arsiv ? "Arşivlendi" : "Arşivden Çıkarıldı", arsiv ? "belge arşive alındı (eksik listesinde sayılır)" : "belge yeniden aktif", kullanici); }
            return (true, $"{b.No} {(arsiv ? "arşivlendi" : "aktif")}.", b);
        }

        // ---------- özetler ----------
        public static object Ozet(List<SdBelge> l)
        {
            var a = l.Where(b => !b.Arsiv).ToList(); var eksik = Eksikler();
            return new { toplam = a.Count, gecerli = a.Count(b => b.Durum == "Geçerli"), suresiz = a.Count(b => b.Durum == "Süresiz"), yaklasan = a.Count(b => b.Durum == "Süresi Yaklaşıyor"), dolan = a.Count(b => b.Durum == "Süresi Doldu"), eksik = eksik.Count, arsiv = l.Count(b => b.Arsiv), sirket = Sirketler.Count, kategori = Kategoriler.Count, tur = Turler.Count, buAyDolan = a.Count(b => b.GecerlilikBitis != null && b.GecerlilikBitis.Value.Month == DateTime.Today.Month && b.GecerlilikBitis.Value.Year == DateTime.Today.Year) };
        }
        public static List<object> Matris(List<SdBelge> l)
        {
            var a = l.Where(b => !b.Arsiv).ToList(); var eksik = Eksikler();
            return Sirketler.Select(s => (object)new
            {
                s.Kod, s.Ad, s.Renk, toplam = a.Count(b => b.SirketKod == s.Kod), yaklasan = a.Count(b => b.SirketKod == s.Kod && b.Durum == "Süresi Yaklaşıyor"), dolan = a.Count(b => b.SirketKod == s.Kod && b.Durum == "Süresi Doldu"), eksik = eksik.Count(e => e.Sirket.Kod == s.Kod),
                kategoriler = Kategoriler.Select(k => new { k.Kod, toplam = a.Count(b => b.SirketKod == s.Kod && b.Kategori == k.Kod), yaklasan = a.Count(b => b.SirketKod == s.Kod && b.Kategori == k.Kod && b.Durum == "Süresi Yaklaşıyor"), dolan = a.Count(b => b.SirketKod == s.Kod && b.Kategori == k.Kod && b.Durum == "Süresi Doldu"), eksik = eksik.Count(e => e.Sirket.Kod == s.Kod && e.Tur.Kategori == k.Kod), beklenen = Turler.Count(t => t.Kategori == k.Kod && TurGecerli(t, s.Kod)) }).ToList()
            }).ToList();
        }
        public static object Json(SdBelge b)
        {
            var s = SirketBul(b.SirketKod); var k = KategoriBul(b.Kategori); var t = TurBul(b.TurKod); var son = b.SonSurum;
            return new
            {
                b.Id, no = b.No, sirketKod = b.SirketKod, sirket = s?.Ad, sirketRenk = s?.Renk, kategori = b.Kategori, kategoriAd = k?.Ad, turKod = b.TurKod, tur = t?.Ad, zorunlu = t?.Zorunlu ?? false, sureliAy = t?.SureliAy, ad = b.Ad, belgeNo = b.BelgeNo, kurum = b.Kurum,
                duzenleme = b.DuzenlemeTarihi.ToString("yyyy-MM-dd"), bitis = b.GecerlilikBitis?.ToString("yyyy-MM-dd"), kalan = b.KalanGun, durum = b.Durum, sorumlu = b.Sorumlu, gizlilik = b.Gizlilik, not = b.Not, arsiv = b.Arsiv,
                surum = son?.No ?? 0, dosya = son?.Dosya, boyut = son?.Boyut, yukleme = son?.Tarih.ToString("yyyy-MM-dd HH:mm"), yukleyen = son?.Kullanici, dosyaVar = son?.Veri != null,
                surumler = b.Surumler.OrderByDescending(x => x.No).Select(x => new { x.No, tarih = x.Tarih.ToString("yyyy-MM-dd HH:mm"), x.Kullanici, x.Dosya, boyut = x.Boyut, x.Tip, x.Not, gercek = x.Veri != null }).ToList(),
                hareketler = b.Hareketler.OrderByDescending(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("yyyy-MM-dd HH:mm"), h.Kullanici, h.Islem, h.Aciklama }).ToList()
            };
        }
        public static object SirketJson(SdSirket s) => new { s.Kod, s.Ad, s.Unvan, s.VergiNo, s.VergiDairesi, s.SicilNo, s.Mersis, s.Adres, s.KurulusYili, s.Faaliyet, s.Yetkili, s.Renk };
        public static object KategoriJson(SdKategori k) => new { k.Kod, k.Ad, k.Ikon, k.Aciklama, k.Action, k.Sorumlu, url = "/SirketDokuman/" + k.Action, turSayisi = Turler.Count(t => t.Kategori == k.Kod) };
        public static object TurJson(SdTur t) => new { t.Kod, t.Ad, t.Kategori, t.Zorunlu, t.SureliAy, t.Kurum, sadece = t.Sadece, t.Aciklama };

        // ---------- belge kapak sayfası (dosyası yüklenmemiş örnek belgeler) ve şirket dosya dizini ----------
        public static DaBelge KapakSayfasi(SdBelge b)
        {
            var s = SirketBul(b.SirketKod); var k = KategoriBul(b.Kategori); var son = b.SonSurum;
            var d = new DaBelge { Baslik = b.Ad, AltBaslik = $"{s?.Unvan} · {k?.Ad}", Sirket = s?.Ad, Hazirlayan = b.Sorumlu, Yatay = false };
            d.Ozet.Add(("Belge No", b.BelgeNo == "" ? "—" : b.BelgeNo)); d.Ozet.Add(("Düzenleyen", b.Kurum)); d.Ozet.Add(("Düzenleme", b.DuzenlemeTarihi.ToString("dd.MM.yyyy"))); d.Ozet.Add(("Geçerlilik", b.GecerlilikBitis?.ToString("dd.MM.yyyy") ?? "Süresiz")); d.Ozet.Add(("Durum", b.Durum)); d.Ozet.Add(("Sürüm", son == null ? "—" : $"v{son.No} · {son.Tarih:dd.MM.yyyy}"));
            var bl = new DaBlok { Baslik = "Şirket Bilgileri", Kolonlar = { "Alan", "Değer" } };
            foreach (var (a, v) in new[] { ("Ünvan", s?.Unvan), ("Vergi No · Dairesi", $"{s?.VergiNo} · {s?.VergiDairesi}"), ("Ticaret Sicil No", s?.SicilNo), ("MERSİS", s?.Mersis), ("Adres", s?.Adres), ("Kuruluş", s?.KurulusYili.ToString()), ("Faaliyet", s?.Faaliyet), ("Yetkili", s?.Yetkili), ("Portal Kaydı", b.No), ("Sorumlu", b.Sorumlu), ("Gizlilik", b.Gizlilik), ("Not", b.Not == "" ? "—" : b.Not) }) bl.Satirlar.Add(new[] { a, v ?? "" });
            d.Bloklar.Add(bl);
            var h = new DaBlok { Baslik = "Sürüm ve İşlem Geçmişi", Kolonlar = { "Zaman", "Kullanıcı", "İşlem", "Açıklama" } }; foreach (var x in b.Hareketler.OrderByDescending(x => x.Zaman)) h.Satirlar.Add(new[] { x.Zaman.ToString("dd.MM.yyyy HH:mm"), x.Kullanici, x.Islem, x.Aciklama }); h.Not = "Ön yüz örneği: belgenin taranmış aslı yüklenmediği için kapak sayfası üretildi. Gerçek dosya yüklendiğinde bu bağlantı dosyayı açar."; d.Bloklar.Add(h);
            return d;
        }
        public static DaBelge SirketDosyasi(SdSirket s, string hazirlayan)
        {
            var l = Belgeler.Where(b => b.SirketKod == s.Kod && !b.Arsiv).ToList(); var eksik = Eksikler().Where(e => e.Sirket.Kod == s.Kod).ToList();
            var d = new DaBelge { Baslik = s.Ad + " · Şirket Dosyası (Belge Dizini)", AltBaslik = $"{s.Unvan} · VKN {s.VergiNo} ({s.VergiDairesi}) · Ticaret Sicil {s.SicilNo} · MERSİS {s.Mersis} · {s.Adres}", Sirket = s.Ad, Hazirlayan = hazirlayan, Yatay = true };
            d.Ozet.Add(("Belge", l.Count.ToString())); d.Ozet.Add(("Geçerli", l.Count(b => b.Durum == "Geçerli" || b.Durum == "Süresiz").ToString())); d.Ozet.Add(("Süresi Yaklaşan", l.Count(b => b.Durum == "Süresi Yaklaşıyor").ToString())); d.Ozet.Add(("Süresi Dolan", l.Count(b => b.Durum == "Süresi Doldu").ToString())); d.Ozet.Add(("Eksik (Zorunlu)", eksik.Count.ToString())); d.Ozet.Add(("Yetkili", s.Yetkili));
            foreach (var k in Kategoriler)
            {
                var kl = l.Where(b => b.Kategori == k.Kod).OrderBy(b => b.Ad).ToList(); var ke = eksik.Where(e => e.Tur.Kategori == k.Kod).ToList(); if (kl.Count == 0 && ke.Count == 0) continue;
                var bl = new DaBlok { Baslik = $"{k.Ad} · {kl.Count} belge{(ke.Count > 0 ? $" · {ke.Count} eksik" : "")}", Kolonlar = { "Portal No", "Belge", "Belge No", "Düzenleyen", "Düzenleme", "Geçerlilik", "Kalan Gün", "Durum", "Sorumlu", "Sürüm" }, Sag = { false, false, false, false, false, false, true, false, false, false } };
                foreach (var b in kl) bl.Satirlar.Add(new[] { b.No, b.Ad, b.BelgeNo, b.Kurum, b.DuzenlemeTarihi.ToString("dd.MM.yyyy"), b.GecerlilikBitis?.ToString("dd.MM.yyyy") ?? "Süresiz", b.KalanGun?.ToString() ?? "", b.Durum, b.Sorumlu, b.SonSurum == null ? "" : "v" + b.SonSurum.No });
                foreach (var e in ke) bl.Satirlar.Add(new[] { "—", e.Tur.Ad, "", e.Tur.Kurum, "", "", "", "EKSİK", k.Sorumlu, "" });
                d.Bloklar.Add(bl);
            }
            return d;
        }
    }
}
