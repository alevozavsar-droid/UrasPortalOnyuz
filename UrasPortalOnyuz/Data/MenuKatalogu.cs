// Ana projedeki menu tohumu + gorunum klasorlerinden OTOMATIK uretildi (224 ekran).
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    public static class MenuKatalogu
    {
        private static readonly (string C, string A, string I, string T, string K)[] _ekranlar = new (string, string, string, string, string)[]
        {
            ("Rapor", "Index", "fas fa-file-alt", "Nakit Akışı (Ödeme Bekleyenler)", null),
            ("Rapor3", "Index", "fas fa-file-invoice-dollar", "Nakit Akışı (Ödenenler)", null),
            ("Rapor2", "Index2", "fas fa-money-check-alt", "Çek Akıbeti Raporu", null),
            ("Rapor15", "Index", "fas fa-receipt", "Fatura ve Tahsilat Takip Raporu", null),
            ("Rapor17", "Index", "fas fa-receipt", "Konsolide Tahsilat Takip Raporu", null),
            ("Rapor20", "Index", "fas fa-chart-line", "Cari Ekstre Raporu", null),
            ("Rapor24", "Index", "fas fa-university", "Kasa Onay", null),
            ("Rapor4", "Index", "fas fa-shopping-bag", "Satış Raporu", null),
            ("Rapor27", "Index", "fas fa-shopping-bag", "Ürün Ekstre Raporu", null),
            ("Rapor28", "Index", "fas fa-shopping-bag", "Mizan", null),
            ("Rapor29", "Index", "fas fa-shopping-bag", "Al Sat Şirketleri Satış Analizi", null),
            ("Rapor18", "Index", "fas fa-shopping-bag", "Urs Üretim Planlama", null),
            ("Rapor31", "Index", "fas fa-shopping-bag", "Ürün Detay Raporu", null),
            ("Rapor12", "Index", "fas fa-shopping-bag", "Şirket Ciro Raporu", null),
            ("Rapor33", "Index", "fas fa-shopping-bag", "Çalan İşlem Raporu", null),
            ("Rapor5", "Index", "fas fa-shopping-bag", "Grup İçi Analiz Raporu", null),
            ("Rapor21", "Index", "fas fa-shopping-bag", "Adat Hesaplama", null),
            ("Rapor35", "Index", "fas fa-chart-pie", "Tahsilat Analiz Raporu", null),
            ("Rapor22", "Index", "fas fa-user-tag", "Çek Konsolide Raporu", null),
            ("Rapor37", "Index", "fas fa-user-tag", "Çek Akıbeti Kontrol Raporu", null),
            ("Rapor38", "Index", "fas fa-user-tag", "Çek Konsolide Kontrol Raporu", null),
            ("Rapor39", "Index", "fas fa-chart-line", "Hesap Ekstre Raporu", null),
            ("Rapor40", "Index", "fas fa-chart-line", "İndirilecek Kdv Listesi", null),
            ("Rapor41", "Index", "fas fa-chart-line", "Alış Raporu", null),
            ("Rapor42", "Index", "fas fa-chart-line", "Genel Gider Raporu Muavin", null),
            ("Rapor44", "Index", "fas fa-chart-line", "Aylık Gider Raporu", null),
            ("Rapor43", "Index", "fas fa-chart-line", "Muhasebe Satış Raporu", null),
            ("Rapor45", "Index", "fas fa-chart-line", "Muhasebe Belge Bazlı Satış Raporu", null),
            ("Rapor47", "Index", "fas fa-chart-line", "Taslak Satıcı Faturaları Raporu", null),
            ("Rapor48", "Index", "fas fa-chart-line", "İade Edilen Malzeme Raporu", null),
            ("Rapor49", "Index", "fas fa-chart-line", "Alım Raporu", null),
            ("Rapor50", "Index", "fas fa-chart-line", "Satış Alış İade Kdv Raporu", null),
            ("Rapor51", "Index", "fas fa-chart-line", "Direkt Satınalma Açık Talep ve Sipariş Raporu", null),
            ("Rapor52", "Index", "fas fa-chart-line", "Tedarikçi Fatura ve Ödeme Takip Raporu", null),
            ("Rapor53", "Index", "fas fa-chart-line", "Tedarikçi Konsolide Ödeme Takip Raporu", null),
            ("Rapor56", "Index", "fas fa-chart-line", "Bedelsiz Satış Raporu", null),
            ("Rapor6", "Index", "fas fa-chart-line", "Satınalma Fiyat Analiz Raporu", null),
            ("Rapor10", "Index", "fas fa-chart-line", "İthalat Maliyet Raporu", null),
            ("YapayZeka", "Index", "fas fa-chart-line", "AI", null),
            ("Gemini", "Index", "fas fa-magic text-primary", "Gemini AI Asistan", null),
            ("Rapor58", "Index", "fas fa-chart-line", "Muhasebe Satış Raporu(İhracat)", null),
            ("Rapor59", "Index", "fas fa-chart-line", "E-Mutabakat", null),
            ("Rapor60", "Index", "fas fa-shopping-bag", "Çek Ekstre Raporu", null),
            ("Rapor61", "Index", "fas fa-shopping-bag", "Malzeme Ekstre Raporu", null),
            ("Rapor62", "Index", "fas fa-shopping-bag", "Toptan Alış Siparişleri Kontrol Listesi", null),
            ("Rapor63", "Index", "fas fa-shopping-bag", "Konsolide Alım Raporu", null),
            ("Rapor64", "Index", "fas fa-shopping-bag", "Mutabakat", null),
            ("Rapor66", "Index", "fas fa-shopping-bag", "Hammadde Fiyat Raporu", null),
            ("Rapor67", "Index", "fas fa-shopping-bag", "KDV Durum Raporu", null),
            ("Rapor65", "Index", "fas fa-shopping-bag", "Cari Bakiye Raporu", null),
            ("Rapor68", "Index", "fas fa-shopping-bag", "İhracat Tahsilat Raporu", null),
            ("Rapor69", "Index", "fas fa-shopping-bag", "İthalat Ödeme Raporu", null),
            ("Rapor70", "Index", "fas fa-shopping-bag", "Mizan Karşılaştırma", null),
            ("Rapor71", "Index2", "fas fa-money-check-alt", "Muhasebe Çek Akıbeti Raporu", null),
            ("Rapor8", "Index", "fas fa-chart-line", "Üretim Şirketleri Kar Analizi", null),
            ("Rapor73", "Index", "fas fa-chart-line", "Günlük Çek Nakit Tahsiilat", null),
            ("Rapor74", "Index", "fas fa-chart-line", "Satış Ekstre Raporu", null),
            ("Rapor92", "Index", "fas fa-chart-line", "Alış Ekstre Raporu", null),
            ("Rapor75", "Index", "fas fa-chart-line", "Stok Raporu", null),
            ("Rapor148", "Index", "fas fa-warehouse text-primary", "Depo Bazlı Stok Raporu", null),
            ("Rapor150", "Index", "fas fa-clipboard-check text-success", "Aylık Satış Onay Raporu", null),
            ("Rapor151", "Index", "fas fa-door-open text-primary", "Danışma Giriş-Çıkış Defteri", null),
            ("Rapor152", "Index", "fas fa-people-arrows text-danger", "Grup İçi Satış (Uras > Selvi > Müşteri)", null),
            ("Rapor7C", "Index", "fas fa-chart-line", "Ürün Hammadde Ekstresi", null),
            ("Rapor77", "Index", "fas fa-chart-line", "Ürün Hammadde (Ambalaj)", null),
            ("Rapor79", "Index", "fas fa-chart-line", "Kasa Nakit Akışı", null),
            ("Rapor80", "Index", "fas fa-chart-line", "Zimmet Takip Raporu", null),
            ("Rapor81", "Index", "fas fa-chart-line", "Satış Analiz Dashboard", null),
            ("Rapor82", "Index", "fas fa-chart-line", "Aktarım Robotu Kontrol Paneli-URS", null),
            ("Rapor84", "Index", "fas fa-chart-line", "Aktarım Robotu Kontrol Paneli-ALV", null),
            ("Rapor85", "Index", "fas fa-chart-line", "Aktarım Robotu Kontrol Paneli-SATINALMA TALEBİ", null),
            ("Rapor86", "Index", "fas fa-chart-line", "Satınalma Talebi Onay Ekranı", null),
            ("Rapor57", "Index", "fas fa-chart-line", "Personel Maliyet Tablosu", null),
            ("Rapor83", "Index", "fas fa-chart-line", "Satışçı Analiz Raporu", null),
            ("Rapor87", "Index", "fas fa-chart-line", "Fatura Mutabakat Raporu - QNB", null),
            ("Rapor135", "Index", "fas fa-chart-line", "İrsaliye Mutabakat Raporu - QNB", null),
            ("Rapor88", "Index", "fas fa-chart-line", "E-Belge", null),
            ("Rapor89", "Index", "fas fa-chart-line", "Stok Analiz Raporu", null),
            ("Rapor90", "Index", "fas fa-chart-line", "Aktarım Robotu Kontrol Paneli-URAS -> SELVİ", null),
            ("Rapor93", "Index", "fas fa-chart-line", "Toplu Mahsup Oluştur", null),
            ("Rapor94", "Index", "fas fa-cart-plus", "Satış Siparişi", null),
            ("Rapor108", "Index", "fas-cart-plus", "Teslimat", null),
            ("Rapor109", "Index", "fas fa-file-invoice-dollar", "Satış Faturası", null),
            ("Rapor110", "Index", "fas fa-undo text-danger", "İade İrsaliyesi", null),
            ("Rapor111", "Index", "fas fa-file-invoice-dollar text-danger", "Müşteri İade Faturası", null),
            ("Rapor112", "Index", "fas fa-shopping-cart text-primary", "Satınalma Talebi / Siparişi", null),
            ("Rapor113", "Index", "fas fa-box-open text-primary", "Satınalma Mal Girişi", null),
            ("Rapor114", "Index", "fas fa-file-invoice-dollar text-success", "Satınalma Faturası", null),
            ("Rapor115", "Index", "fas fa-undo text-danger", "Satınalma İade İrsaliyesi", null),
            ("Rapor116", "Index", "fas fa-file-invoice-dollar text-danger", "Satınalma İade Faturası", null),
            ("Rapor117", "Index", "fas fa-file-invoice-dollar text-danger", "Tahsilat", null),
            ("Rapor134", "Index", "fas fa-file-invoice-dollar text-danger", "Tedarikçi Talep Formu", null),
            ("Rapor138", "Index", "fas fa-file-invoice-dollar text-danger", "Üretim Miktarları Raporu (UR / YM)", null),
            ("Rapor118", "Index", "fas fa-file-invoice-dollar text-danger", "Yapılan Ödemeler", null),
            ("Rapor119", "Index", "fas fa-money-check-alt text-danger", "İbraz (Çek Depositi)", null),
            ("Rapor149", "Index", "fas fa-calendar-check text-danger", "Vadeli Çek İbrazı", null),
            ("Rapor173", "Index", "fas fa-boxes-stacked", "Toplu Kalem Ana Verisi Güncelleme", null),
            ("Mesaj", "Index", "fas fa-comment-dots text-primary", "Mesajlar", null),
            ("Rapor130", "Index", "fas fa-money-check-alt text-danger", "Kasa Nakit Akışı Görünüm", null),
            ("Rapor131", "Index", "fas fa-money-check-alt text-danger", "Satınalma Siparişi", null),
            ("Rapor132", "Index", "fas fa-money-check-alt text-danger", "Tahsilat İbraz Çek Bordrosu", null),
            ("Rapor120", "Index", "fas fa-file-invoice-dollar text-danger", "Cari Bilgileri", null),
            ("Rapor121", "Index", "fas fa-file-invoice-dollar text-danger", "Temlik İşlemleri", null),
            ("Rapor122", "Index", "fas fa-file-invoice-dollar text-danger", "İhracat Finekra Hareketler", null),
            ("Rapor123", "Index", "fas fa-file-invoice-dollar text-danger", "Konsolide Günlük Finansal Hareketler", null),
            ("Rapor125", "Index", "fas fa-file-invoice-dollar text-danger", "Denge Malzeme Raporu", null),
            ("Rapor129", "Index", "fas fa-file-invoice-dollar text-danger", "Grup İçi Çek Devirleri", null),
            ("Rapor126", "Index", "fas fa-file-invoice-dollar text-danger", "Muhasebe Çek İbraz Kontrolü", null),
            ("Rapor127", "Index", "fas fa-file-invoice-dollar text-danger", "Gider Seçim Raporu", null),
            ("Rapor95", "Index", "fas fa-cart-plus", "Muhattaplar İçn Özel Fiyatlar", null),
            ("Rapor96", "Index", "fas fa-cart-plus", "Satış Analiz Raporu", null),
            ("Rapor97", "Index", "fas fa-cart-plus", "Muhattap Ana Verileri", null),
            ("Rapor100", "Index", "fas fa-cart-plus", "Kalem Ana Verileri", null),
            ("Rapor98", "Index", "fas fa-chart-line", "Aktarım Robotu Kontrol Paneli-SELVİ", null),
            ("Rapor137", "Index", "fas fa-chart-line", "Aktarım Robotu Kontrol Paneli-Uras", null),
            ("Rapor99", "Index", "fas fa-chart-line", "Aktarım Robotu Kontrol Paneli-ASIA", null),
            ("Rapor101", "Index", "fas fa-chart-line", "Açılış Kapanış Kayıt", null),
            ("Rapor102", "Index", "fas fa-chart-line", "Konsolide Gider Raporu", null),
            ("Rapor103", "Index", "fas fa-chart-line", "Otomatik Kapama", null),
            ("Rapor107", "Index", "fas fa-chart-line", "Yevmiye Kaydı", null),
            ("Rapor106", "Index", "fas fa-chart-line", "Üretim Simülasyonu", null),
            ("Rapor105", "Index", "fas fa-chart-line", "Finekra Hareketler", null),
            ("Rapor128", "Index", "fas fa-chart-line", "Virman İşlemleri", null),
            ("Rapor133", "Index", "fas fa-chart-line", "Bekleyen Cari İşlemler Raporu", null),
            ("Rapor136", "Index", "fas fa-chart-line", "Konsolide Prim Raporu", null),
            ("Rapor140", "Index", "fas fa-chart-line", "Stok Maliyeti", null),
            ("Rapor139", "Index", "fas fa-tags text-warning", "Ürün İsimlendirme ve Kod Karşılaştırma", null),
            ("Rapor161", "Index", "fas fa-ship", "İhracat Dosya Raporu", null),
            ("Rapor162", "Index", "fas fa-address-book", "Muhatap (Cari) Ana Verileri", null),
            ("Rapor163", "Index", "fas fa-scale-balanced", "Gider / Gelir Seçim Ekranı", null),
            ("Envanter", "Index", "fas fa-table-list", "Envanter Raporu", null),
            ("MobilSiparis", "Index", "fas fa-table-list", "Mobil Sipariş Portali", null),
            ("OzelFiyat", "Index", "fas fa-table-list", "Muhatap Özel Fiyatları", null),
            ("Permission", "Index", "fas fa-table-list", "Sistem Yetki Yönetimi", null),
            ("PlanliOdeme", "Index", "fas fa-table-list", "Planlı Ödemeler", null),
            ("Profil", "Index", "fas fa-table-list", "Profilim", null),
            ("Rapor104", "Index", "fas fa-table-list", "Maliyet ve Stok Raporu", null),
            ("Rapor11", "Index", "fas fa-table-list", "2025 Yılı Şirket Ciro Raporu", null),
            ("Rapor124", "Index", "fas fa-table-list", "Çek Akıbeti Detaylı Raporu (Rapor 124)", null),
            ("Rapor13", "Index", "fas fa-table-list", "İhracat Dosya Takip Raporu", null),
            ("Rapor141", "Index", "fas fa-table-list", "Cari Ekstre / Hesap Ekstresi Mutabakatı", null),
            ("Rapor142", "Index", "fas fa-table-list", "Zimmet Takip", null),
            ("Rapor143", "Index", "fas fa-table-list", "Onay Ekranı", null),
            ("Rapor144", "Index", "fas fa-table-list", "Aktarım Veri Denetimi (AVRASYA)", null),
            ("Rapor145", "Index", "fas fa-table-list", "İrsaliye / Fatura Aktarım Denetimi", null),
            ("Rapor146", "Index", "fas fa-table-list", "Tahsilat Prim Tablosu", null),
            ("Rapor147", "Index", "fas fa-table-list", "Kasa / Banka Bakiye Raporu", null),
            ("Rapor153", "Index", "fas fa-table-list", "Aktarım Veri Denetimi (DAF)", null),
            ("Rapor154", "Index", "fas fa-table-list", "Aktarım Veri Denetimi (AVRUPA PAPER)", null),
            ("Rapor155", "Index", "fas fa-table-list", "Aktarım Veri Denetimi (URAS HOLDİNG)", null),
            ("Rapor156", "Index", "fas fa-table-list", "Aktarım Veri Denetimi (ALV FİLO)", null),
            ("Rapor157", "Index", "fas fa-table-list", "Aktarım Veri Denetimi (URAS BASKI)", null),
            ("Rapor158", "Index", "fas fa-table-list", "Satınalma Mal Girişi Oluştur", null),
            ("Rapor159", "Index", "fas fa-table-list", "Satınalma Talebi Oluştur", null),
            ("Rapor16", "Index", "fas fa-table-list", "ALV Fiyat", null),
            ("Rapor160", "Index", "fas fa-table-list", "Kur Farkı", null),
            ("Rapor164", "Index", "fas fa-table-list", "Gider / Gelir Kod Yönetimi", null),
            ("Rapor165", "Index", "fas fa-table-list", "Gider / Gelir Kodu Talep Onayı", null),
            ("Rapor166", "Index", "fas fa-table-list", "Fatura İnceleme", null),
            ("Rapor167", "Index", "fas fa-table-list", "Gider Onay Ekranı", null),
            ("Rapor168", "Index", "fas fa-table-list", "İBKB Beyanname Takibi", null),
            ("Rapor169", "Index", "fas fa-table-list", "Taslak Faturalar", null),
            ("Rapor170", "Index", "fas fa-table-list", "Gider / Gelir Analizi", null),
            ("Rapor171", "Index", "fas fa-table-list", "Yansıtma Faturası", null),
            ("Rapor172", "Index", "fas fa-table-list", "Parti Yaşlandırma (Stok Yaşı)", null),
            ("Rapor19", "Index", "fas fa-table-list", "Mizan Raporu", null),
            ("Rapor25", "Index", "fas fa-table-list", "Nakit Akışı ve Ödeme Talimatı", null),
            ("Rapor26", "Index", "fas fa-table-list", "R-X Müşteri Bakiye Raporu", null),
            ("Rapor30", "Index", "fas fa-table-list", "Kredi ve Teminat Takip Raporu (Rapor30)", null),
            ("Rapor32", "Index", "fas fa-table-list", "Kredi ve Teminat Takip Raporu (Rapor32)", null),
            ("Rapor34", "Index", "fas fa-table-list", "Müşteri Risk Raporu (Rapor 34)", null),
            ("Rapor36", "Index", "fas fa-table-list", "Müşteri Risk Raporu (Rapor 36)", null),
            ("Rapor46", "Index", "fas fa-table-list", "Gider Hesapları Kategorizasyon Raporu", null),
            ("Rapor54", "Index", "fas fa-table-list", "Araç Envanteri Yönetimi", null),
            ("Rapor55", "Index", "fas fa-table-list", "Konsolide Alım Raporu", null),
            ("Rapor7", "Index", "fas fa-table-list", "Stok Hareket Analizi Raporu", null),
            ("Rapor72", "Index", "fas fa-table-list", "Hesap Ekstre Mutabakatı", null),
            ("Rapor76", "Index", "fas fa-table-list", "Tahsilat & Risk Takip Raporu", null),
            ("Rapor9", "Index", "fas fa-table-list", "Ülke Bazında Finansal Rapor", null),
            ("Rapor91", "Index", "fas fa-table-list", "SAP Mizan vs Excel Kıyaslama Raporu", null),
            ("RobotServis", "Index", "fas fa-table-list", "Aktarım Robotu Servisleri", null),
            ("SapBilgi", "Index", "fas fa-table-list", "SAP Bilgilerim", null),
            ("Test", "Index", "fas fa-table-list", "Test Sayfası", null),
            ("UrasUretim", "Index", "fas fa-table-list", "Dashboard", "Uras Üretim"),
            ("UrasUretimAyarlar", "Index", "fas fa-table-list", "Yetkilendirme", "Uras Üretim"),
            ("UrasUretimAyarlar", "MailAyarlari", "fas fa-table-list", "Mail Ayarları", "Uras Üretim"),
            ("UrasUretimBakim", "Index", "fas fa-table-list", "Arıza & Bakım", "Uras Üretim"),
            ("UrasUretimKalite", "GirisKK", "fas fa-table-list", "Giriş Kalite Kontrol", "Uras Üretim"),
            ("UrasUretimKalite", "KaliteKontrol", "fas fa-table-list", "KK Kalem Değer Aralıkları", "Uras Üretim"),
            ("UrasUretimKalite", "MusteriSikayet", "fas fa-table-list", "Müşteri Şikayet Formu", "Uras Üretim"),
            ("UrasUretimKalite", "PHKalibrasyon", "fas fa-table-list", "pH Kalibrasyon KK", "Uras Üretim"),
            ("UrasUretimKalite", "PlastikBazliKK", "fas fa-table-list", "Plastik Bazlı Kalite Kontrol", "Uras Üretim"),
            ("UrasUretimKalite", "SatinalmaKK", "fas fa-table-list", "Satınalma Kalite Kontrol", "Uras Üretim"),
            ("UrasUretimKalite", "SuBazliKK", "fas fa-table-list", "Su Bazlı Kalite Kontrol", "Uras Üretim"),
            ("UrasUretimKalite", "Tutanak", "fas fa-table-list", "Karantina/Red/İmha Tutanağı", "Uras Üretim"),
            ("UrasUretimKalite", "UrunKaliteKontrol", "fas fa-table-list", "Ürün Kalite Kontrol", "Uras Üretim"),
            ("UrasUretimKalite", "Yaslandirma", "fas fa-table-list", "Yaşlandırma Formu", "Uras Üretim"),
            ("UrasUretimRapor", "HammaddeTuketim", "fas fa-table-list", "Hammadde Tüketim", "Uras Üretim"),
            ("UrasUretimRapor", "Index", "fas fa-table-list", "Uras Üretim Raporları", "Uras Üretim"),
            ("UrasUretimRapor", "Izlenebilirlik", "fas fa-table-list", "Ürün İzlenebilirlik", "Uras Üretim"),
            ("UrasUretimRapor", "LotSira", "fas fa-table-list", "Lot Sıra Kontrol", "Uras Üretim"),
            ("UrasUretimRapor", "Performans", "fas fa-table-list", "İş Emri Performans", "Uras Üretim"),
            ("UrasUretimRapor", "Sayim", "fas fa-table-list", "Stok Sayım Raporu", "Uras Üretim"),
            ("UrasUretimRapor", "Silo", "fas fa-table-list", "Silo Raporu", "Uras Üretim"),
            ("UrasUretimRapor", "StokYasi", "fas fa-table-list", "Parti Yaşlandırma", "Uras Üretim"),
            ("UrasUretimRapor", "Uretim", "fas fa-table-list", "Üretim Raporu", "Uras Üretim"),
            ("UrasUretimStok", "DepoNakli", "fas fa-table-list", "Depolar Arası Nakil", "Uras Üretim"),
            ("UrasUretimStok", "DepoStokRaporu", "fas fa-table-list", "Depo Stok Raporu", "Uras Üretim"),
            ("UrasUretimStok", "EtiketYazdir", "fas fa-table-list", "Etiket Yazdırma", "Uras Üretim"),
            ("UrasUretimStok", "MalCikisi", "fas fa-table-list", "Mal Çıkışı", "Uras Üretim"),
            ("UrasUretimStok", "MalGirisi", "fas fa-table-list", "Mal Girişi", "Uras Üretim"),
            ("UrasUretimStok", "SayimEksik", "fas fa-table-list", "Sayılmayan Kalemler", "Uras Üretim"),
            ("UrasUretimStok", "StokKayitListesi", "fas fa-table-list", "Stok Kayıt Listesi", "Uras Üretim"),
            ("UrasUretimStok", "StokSayim", "fas fa-table-list", "Stok Sayım", "Uras Üretim"),
            ("UrasUretimUretim", "KalemAnaVerileri", "fas fa-table-list", "Kalem Ana Verileri", "Uras Üretim"),
            ("UrasUretimUretim", "Numune", "fas fa-table-list", "Numune", "Uras Üretim"),
            ("UrasUretimUretim", "SahaUretim", "fas fa-table-list", "Saha Üretim", "Uras Üretim"),
            ("UrasUretimUretim", "SeriPartiTanim", "fas fa-table-list", "Seri/Parti Tanımları", "Uras Üretim"),
            ("UrasUretimUretim", "SiloDolum", "fas fa-table-list", "Silo Dolum", "Uras Üretim"),
            ("UrasUretimUretim", "UretimSiparisi", "fas fa-table-list", "Üretim Siparişi", "Uras Üretim"),
            ("UrasUretimUretim", "UrunAyristirma", "fas fa-table-list", "Ürün Ayrıştırma", "Uras Üretim"),
            ("UrasUretimUretim", "UrunDonusumu", "fas fa-table-list", "Ürün Dönüşümü", "Uras Üretim"),
            ("UrasUretimUrunAgaci", "Index", "fas fa-table-list", "Ürün Ağacı", "Uras Üretim"),
            ("UrasUretimUrunAgaci", "ReceteMaliyet", "fas fa-table-list", "Reçete Birim Maliyet", "Uras Üretim"),
            // İK ve İdari İşler talep formları (Talep › İK ve İdari İşler); Controllers/IkTalepController.cs
            ("IkTalep", "Izin", "fas fa-umbrella-beach", "İzin Talebi", "İK ve İdari İşler"),
            ("IkTalep", "Mesai", "fas fa-business-time", "Mesai Talebi", "İK ve İdari İşler"),
            ("IkTalep", "Avans", "fas fa-hand-holding-dollar", "Avans Talebi", "İK ve İdari İşler"),
            ("IkTalep", "MesaiTakip", "fas fa-business-time", "Mesai Takip Raporu", "İK ve İdari İşler"),
            ("IkTalep", "SeyahatTakvim", "fas fa-plane", "Seyahat Takvimi", "İK ve İdari İşler"),
            ("IkTalep", "AracTakvim", "fas fa-car", "Araç Takvimi (Filo)", "İK ve İdari İşler"),
            ("IkTalep", "IzinTakvim", "fas fa-calendar-days", "Ekip İzin Takvimi", "İK ve İdari İşler"),
            ("IkTalep", "IzinOnay", "fas fa-clipboard-check", "İzin Onay Bekleyenler", "İK ve İdari İşler"),
            ("IkTalep", "IzinTakip", "fas fa-calendar-check", "İzin Takip Raporu", "İK ve İdari İşler"),
            ("GrupIci", "Yansitma", "fas fa-right-left", "Yansıtma İşlemleri", "Mali İşler"),
            ("GrupIci", "Mahsup", "fas fa-scale-balanced", "Mahsup İşlemleri", "Mali İşler"),
            ("GrupIci", "Temlik", "fas fa-file-contract", "Temlik İşlemleri", "Mali İşler"),
            ("GrupIci", "Rapor", "fas fa-diagram-project", "Grup İçi Özel Finansal İşlemler Raporu", "Mali İşler"),
            ("IkTalep", "AvansBekleyenOdemeler", "fas fa-money-bill-wave", "Nakit Avans — Bekleyen Ödemeler", "Finans"),
            ("IkTalep", "AvansTakip", "fas fa-list-check", "Nakit Avans Takip Raporu", "Finans"),
            ("IkTalep", "Belge", "fas fa-file-signature", "Belge Talebi", "İK ve İdari İşler"),
            ("IkTalep", "Seyahat", "fas fa-plane-departure", "Seyahat Talebi", "İK ve İdari İşler"),
            ("IkTalep", "Arac", "fas fa-car-side", "Araç Talebi", "İK ve İdari İşler"),
            ("AlimOnay", "Index", "fas fa-cart-shopping", "Aylık Alım Onay Ekranı", "Tedarik Zinciri"),
            ("GenelAnaliz", "Index", "fas fa-chart-line", "Genel Analiz Raporu", "Yönetim"),
        };

        public static List<AppMenu> Olustur()
        {
            int id = 0;
            var denetleyiciler = new HashSet<string>(typeof(MenuKatalogu).Assembly.GetTypes().Where(t => typeof(Microsoft.AspNetCore.Mvc.ControllerBase).IsAssignableFrom(t) && !t.IsAbstract).Select(t => t.Name), System.StringComparer.OrdinalIgnoreCase);
            return _ekranlar.Where(e => denetleyiciler.Contains(e.C + "Controller")).Select(e => new AppMenu { Id = ++id, Category = e.K ?? Kategori(e.C), MenuTitle = e.T, ControllerName = e.C, ActionName = e.A, Icon = e.I, DisplayOrder = id, IsActive = true }).ToList();
        }

        public static string Kategori(string controllerName)
        {
            switch (controllerName)
            {
                case "Rapor94":
                case "Rapor95":
                case "Rapor97":
                case "Rapor96":
                case "Rapor100":
                case "Rapor107":
                case "Rapor108":
                case "Rapor109":
                case "Rapor110":
                case "Rapor111":
                case "Rapor150":
                    return "Sipariş";

                case "Rapor117":
                case "Rapor132":
                    return "Tahsilat";

                case "Rapor118":
                case "Rapor119":
                case "Rapor149":
                    return "Yapılan Ödemeler";

                case "Rapor173":
                    return "Stok";

                case "Mesaj":
                    return "Diğer Ekranlar";

                case "Rapor":
                case "Rapor3":
                case "Rapor2":
                case "Rapor24":
                case "Rapor25":
                case "Rapor32":
                case "Rapor37":
                case "Rapor38":
                case "Rapor72":
                case "Rapor79":
                case "Rapor123":
                case "Rapor124":
                case "Rapor129":
                case "Rapor130":
                case "Rapor163":
                    return "Finans";

                case "Rapor20":
                case "Rapor21":
                case "Rapor15":
                case "Rapor17":
                case "Rapor65":
                case "Rapor36":
                case "Rapor22":
                case "Rapor52":
                case "Rapor53":
                case "Rapor60":
                case "Rapor73":
                case "Rapor103":
                case "Rapor105":
                case "Rapor120":
                case "Rapor121":
                case "Rapor133":
                case "Rapor134":
                case "Rapor162":
                    return "Cari İşler";

                case "Rapor9":
                case "Rapor101":
                case "Rapor67":
                case "Rapor104":
                case "Rapor16":
                case "Rapor128":
                case "Rapor28":
                case "Rapor5":
                case "Rapor39":
                case "Rapor40":
                case "Rapor41":
                case "Rapor42":
                case "Rapor44":
                case "Rapor43":
                case "Rapor125":
                case "Rapor126":
                case "Rapor127":
                case "Rapor45":
                case "Rapor46":
                case "Rapor135":
                case "Rapor47":
                case "Rapor48":
                case "Rapor49":
                case "Rapor50":
                case "Rapor55":
                case "Rapor58":
                case "Rapor70":
                case "Rapor71":
                case "Rapor87":
                case "Rapor91":
                case "Rapor93":
                    return "Muhasebe";

                case "Rapor4":
                case "Rapor74":
                case "Rapor76":
                case "Rapor136":
                    return "Satış";

                case "Rapor13":
                case "Rapor68":
                case "Rapor122":
                case "Rapor161":
                    return "İhracat";

                case "Rapor10":
                case "Rapor61":
                case "Rapor62":
                case "Rapor63":
                case "Rapor66":
                case "Rapor69":
                    return "İthalat";

                case "Rapor80":
                    return "Bilgi İşlem";

                case "Rapor151":
                    return "Danışman";

                case "Rapor18":
                case "Rapor106":
                case "Rapor31":
                case "Rapor75":
                case "Rapor138":
                case "Rapor139":
                case "Rapor140":
                    return "Üretim";

                case "Rapor6":
                case "Rapor7":
                case "Rapor51":
                case "Rapor86":
                case "Rapor92":
                case "Rapor112":
                case "Rapor113":
                case "Rapor114":
                case "Rapor115":
                case "Rapor116":
                case "Rapor131":
                    return "Satınalma";

                case "Rapor82":
                case "Rapor84":
                case "Rapor98":
                case "Rapor85":
                case "Rapor90":
                case "Rapor99":
                case "Rapor137":
                    return "Aktarım";

                case "Rapor88":
                    return "E-Belge";

                default:
                    return "Özel Raporlar";
            }
        }
    }
}
