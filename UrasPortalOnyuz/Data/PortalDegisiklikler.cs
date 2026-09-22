// Portal değişiklik günlüğü — ana sayfadaki "Yeni Eklenenler" ve "Son Değişiklikler" kartlarının kaynağı.
//
// Portala yeni bir rapor / ekran eklendiğinde ya da mevcut bir ekran taşındığında, güncellendiğinde veya
// kaldırıldığında buraya BİR SATIR eklenir; kartlar ve /Home/Degisiklikler sayfası bu listeden beslenir.
//   Yeni(...)        → "Yeni Eklenenler" kartı (Tur = yeni)
//   Tasima(...)      → "Son Değişiklikler" kartı (Tur = tasima): ekranın menüdeki yeri değişti
//   Guncelleme(...)  → "Son Değişiklikler" kartı (Tur = guncelleme): ekranda işlev / görünüm değişikliği
//   Kaldirma(...)    → "Son Değişiklikler" kartı (Tur = kaldirma)
// Ekran adı KurumsalMenu'daki rapor adıyla aynıysa bağlantı ve menü yolu otomatik bulunur; menüde olmayan
// ekranlar (ör. Ana Sayfa, tema) için Href / Yol elle verilir.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class PortalDegisiklik
    {
        public DateTime Tarih { get; set; }
        /// <summary>yeni | tasima | guncelleme | kaldirma</summary>
        public string Tur { get; set; }
        public string Ekran { get; set; }
        /// <summary>Menü yolu metni (ör. "Mali İşler › Finans › Ödeme Yönetimi").</summary>
        public string Yol { get; set; }
        public string Aciklama { get; set; }
        public string Href { get; set; }
        public string Kim { get; set; }

        public string TurAdi => Tur == "yeni" ? "Yeni" : Tur == "tasima" ? "Taşındı" : Tur == "kaldirma" ? "Kaldırıldı" : "Güncellendi";
        public string TurRenk => Tur == "yeni" ? "green" : Tur == "tasima" ? "amber" : Tur == "kaldirma" ? "red" : "blue";
        public string Modul => string.IsNullOrEmpty(Yol) ? "" : Yol.Split('›')[0].Trim();

        public string TarihMetni
        {
            get
            {
                int fark = (DateTime.Today - Tarih.Date).Days;
                if (fark == 0) return "Bugün";
                if (fark == 1) return "Dün";
                return Tarih.ToString("d MMM", PortalDegisiklikler.Tr);
            }
        }
    }

    public static class PortalDegisiklikler
    {
        internal static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        private static readonly Lazy<List<PortalDegisiklik>> _liste = new Lazy<List<PortalDegisiklik>>(Olustur);
        public static List<PortalDegisiklik> Liste => _liste.Value;

        public static IEnumerable<PortalDegisiklik> YeniEklenenler => Liste.Where(x => x.Tur == "yeni").OrderByDescending(x => x.Tarih);
        public static IEnumerable<PortalDegisiklik> SonDegisiklikler => Liste.Where(x => x.Tur != "yeni").OrderByDescending(x => x.Tarih);

        // ---------- Günlük ----------
        private static List<PortalDegisiklik> Olustur()
        {
            var l = new List<PortalDegisiklik>();
            DateTime T(int gun, int ay) => new DateTime(DateTime.Today.Year, ay, gun);

            // --- Yeni eklenen rapor / ekranlar ---
            Yeni(l, T(22, 9), "Yapılan Ödemeler", "Yönetim › Özel Raporlar altında tüm grup şirketlerinin yaptığı ödemelerin konsolide listesi: Banka (Havale / EFT), Çek (vadeli çekler; Ödendi / Vade Bekliyor), Nakit (Kasa) çıkışları ve Nakit Avans teslimleri. Kaynaklar: ERP banka / kasa, çek bordrosu, Nakit Avans modülü ve Aylık Alım Onay'daki ödenmiş tedarikçi faturaları. Dönem, şirket, ödeme türü, kategori, alıcı tipi, para birimi, durum ve kaynak filtreleri; KPI kutuları; Şirket × Ödeme Türü ve Ay × Ödeme Türü matrisleri; kategori dağılımı; en büyük 10 alıcı; satır detayından kaynak belgeye geçiş; Excel.", "/YapilanOdemeler", "Yönetim › Özel Analizler › Özel Raporlar");
            Yeni(l, T(21, 9), "Genel Analiz Raporu", "Yönetim › Özel Raporlar altında, \"2026 Aylık Analiz Raporu\" Excel şablonuyla aynı düzende: Ciro (şirket alt toplamları), Alımlar, Giderler, Ara Sonuç, Amortisman, Kur Farkı ve Sonuç satırları; 12 ay + Toplam + Aylık Ort.; yazılabilir hücreler, otomatik alt toplam, kaydet ve Excel (CSV) çıktısı.");
            Yeni(l, T(21, 9), "Aylık Alım Onay Ekranı", "Muhasebe'nin hazırladığı aylık alım raporu (tedarikçi faturaları, kalem detayı ve fatura görüntüsü); satınalmacı ve Tedarik Zinciri Direktörü ay sonu onayı, uyarılar, ay kapanışı. Tedarik Zinciri ve Muhasebe menülerinde.", "/Modul/tedarik-zinciri?yol=satin-alma/fiyat-alim-analizi/r0", "Tedarik Zinciri › Satın Alma › Fiyat ve Alım Analizi");
            Yeni(l, T(20, 9), "Grup İçi Özel Finansal İşlemler Raporu", "Yansıtma, Mahsup ve Temlik işlemlerinin birleşik listesi; şirket bazında açık tutarlar ve ERP grup içi bakiye tablosu.");
            Yeni(l, T(20, 9), "Temlik İşlemleri", "Alacağın grup şirketine devri: Muhasebe → Hukuk → Finans onayı, sözleşme, borçluya bildirim, tahsilat ve kapanış.", "/Modul/mali-isler?yol=grup-ici-ozel/r2", "Mali İşler › Grup İçi Özel Finansal İşlemler");   // aynı ad Muhasebe'de de var (Rapor121); adres elle
            Yeni(l, T(20, 9), "Mahsup İşlemleri", "ERP cari bakiyeleriyle sınırlı karşılıklı / üçlü / avans / fatura mahsubu; A-B fişleri ve mutabakat.");
            Yeni(l, T(19, 9), "Yansıtma İşlemleri", "Şirketler arası masraf, hizmet, personel ve kira yansıtması; KDV hesabı, e-fatura ve karşı kabul akışı.");
            Yeni(l, T(18, 9), "Araç Talebi", "Filo master, ehliyet ve ceza puanı kontrolü, müsaitlik, tahsis → anahtar teslim → iade → HGS/ceza → kapanış.");
            Yeni(l, T(18, 9), "Seyahat Talebi", "Gün / gece, parametrik harcırah, bütçe eşiğinde Direktör onayı, bilet / otel / avans / masraf formu.");
            Yeni(l, T(18, 9), "Belge Talebi", "SLA hedefli belge hazırlama: hazırlık → imza → belge no → teslim → teyit; KVKK rızası ve kargo adresi.");
            Yeni(l, T(18, 9), "Mesai Talebi", "Planlanan saat, çarpan, 270 saat yıllık sınır, PDKS gerçekleşme ve bordro puantajı.");
            Yeni(l, T(18, 9), "Mesai Takip Raporu", "Mesai taleplerinin onay, gerçekleşme ve puantaj durumu.");
            Yeni(l, T(17, 9), "İzin Talebi", "İzin türü master'ı, kıdeme göre hak ediş, iş günü hesabı, çakışma / vekil kontrolleri ve parametrik onay akışı.");
            Yeni(l, T(17, 9), "Ekip İzin Takvimi", "Ekip bazında planlanan ve onaylanan izinlerin aylık takvim görünümü.");
            Yeni(l, T(17, 9), "İzin Takip Raporu", "İzin taleplerinin talep / kullanım / puantaj statüleriyle takibi.");
            Yeni(l, T(16, 9), "Avans Talebi", "Nakit Avans Talep, Ödeme, Mahsup ve Kapatma Modülü (şartname v1.0): gösterge kutucukları, onay akışı ve kaynak sistem olayları.");
            Yeni(l, T(16, 9), "Nakit Avans — Bekleyen Ödemeler", "Finans için teslim bekleyen avansların listesi ve teslim teyidi.");
            Yeni(l, T(16, 9), "Nakit Avans Takip Raporu", "Açık = Teslim − Mahsup − İade özetiyle tüm avansların takibi.");

            // --- Taşıma, güncelleme, kaldırma ---
            Yeni(l, T(22, 9), "Müşteri Cari Kartları", "\"Cari Kart ve Faaliyet Eşleştirme Yapısı\" IT raporuna göre: CARİ_MASTER listesi (kod, ünvan, VKN, tip, bölge / ülke / şehir, sınıf, ilişki durumu, rol / eş cari, vade, durum), faaliyet grubu bazında satış temsilcisi, detay drawer'ında CARİ_FAALİYET satırları ve ürün hiyerarşisi; tip / bölge / sınıf matrisi.", "/CariKart/Musteri", "Satış ve Operasyon › Müşteri ve CRM Yönetimi › Müşteri Portföyü");
            Yeni(l, T(22, 9), "Müşteri Talep Formu", "Cari kart açılış talebi: CARİ_MASTER alanları bağımlı seçimlerle (kart tipi → bölge → ülke → şehir, tip + bölge → cari sınıfı, ilişki durumu, rol, çift rolde eş cari kodu), CARİ_FAALİYET satırları (faaliyet ana grubu → grubu → satış temsilcisi, ana faaliyet, ürün sınıfı › grubu › serisi › adı), canlı validasyon, Muhasebe onayı sonrası kart otomatik açılır.", "/CariKart/MusteriTalep", "Talep › Yeni Talep › Müşteri");
            Yeni(l, T(22, 9), "Tedarikçi Cari Kartları", "Tedarikçi carileri için aynı CARİ_MASTER / CARİ_FAALİYET yapısı (hammadde, yardımcı malzeme, fason, hizmet, danışmanlık, özel takip sınıfları; yurtdışı tedarikçiler; çift rol eş kartları).", "/CariKart/Tedarikci", "Tedarik Zinciri › Tedarikçi Yönetimi › Tedarikçi Kartları");
            Yeni(l, T(22, 9), "Tedarikçi Talep Formu", "Tedarikçi cari kart açılış talebi; Müşteri Talep Formu ile aynı bağımlı seçim ve validasyon kuralları.", "/CariKart/TedarikciTalep", "Talep › Yeni Talep › Tedarikçi");
            Guncelleme(l, T(22, 9), "Modül Sayfası", "Rapor rozetleri: son 30 günde eklenen raporlar şemada, sol menüde ve listede \"(Yeni)\", güncellenen ya da taşınan raporlar \"(Düzenlendi)\" rozetiyle işaretlenir. Eşleme bu günlükteki adrese göre yapılır; modül derin bağlantısı yanında ekranın kendi adresi de (ör. /CariKart/Tedarikci) tanınır.", "/Modul/tedarik-zinciri", "Tedarik Zinciri");
            Guncelleme(l, T(22, 9), "Tedarikçi Cari Kartları", "Satın alma / tedarik ekranlarında \"Satış Temsilcisi\" yerine \"Satınalma Temsilcisi\" yazılır: Tedarikçi Cari Kartları listesi, filtre, faaliyet bazlı temsilci dağılımı, detay drawer'ı ve Excel başlıkları ile Tedarikçi Talep Formu'ndaki faaliyet satırı başlığı.", "/CariKart/Tedarikci", "Tedarik Zinciri › Tedarikçi Yönetimi › Tedarikçi Kartları");
            Guncelleme(l, T(22, 9), "Rapor Yazım Kuralı", "Yansıtma, Temlik, Mahsup ve Cari Kart ekranlarında seçim listesi değerleri, filtre seçenekleri ve etiketlerde her kelime büyük harfle başlar: \"Mal Mukabili\", \"Vesaik Mukabili\", \"Çek (Vadeli)\", \"Vade 60 Gün\", \"TN01 Grup İçi Finansman\", \"MN02 İade / Credit Note Kapama\", \"Tümü\". KPI alt satırları ve arama kutusu ipuçları da büyük harfle başlar.", "/CariKart/MusteriTalep", "Satış ve Operasyon › Müşteri ve CRM Yönetimi › Müşteri Portföyü");
            Guncelleme(l, T(22, 9), "Müşteri ve CRM Yönetimi", "Müşteri Portföyü altına \"Müşteri Talep Formu\" eklendi. Müşteri ve Tedarikçi Talep Formu hem Talep › Yeni Talep altından hem Müşteri Portföyü / Tedarikçi Kartları altından doğrudan aynı ekran olarak açılır (kısayol yönlendirmesi kaldırıldı).", "/Modul/satis-operasyon?yol=musteri-crm/musteri-portfoyu/r1", "Satış ve Operasyon › Müşteri ve CRM Yönetimi");
            Guncelleme(l, T(22, 9), "Mahsup İşlemleri", "Mahsup Modülü IT gereksinim dokümanına (v1.0) göre yeniden yazıldı: Açık Kalemleri Getir, yan yana borç / alacak açık kalem panelleri (kalemler tek tek seçilir, kısmi mahsup, rezervasyon), belge → belge dağılım tablosu ve otomatik eşleştirme (en eski vade / belge, aynı tutar, referans), borç = alacak eşitlik kontrolü, iç mutabakat / yevmiye simülasyonu (yeni fiş EVET / HAYIR, kur ve yuvarlama farkı, M06 mirror), parametrik onay, şirket bazlı idempotent ERP kaydı ile kısmi entegrasyon / tekrar deneme / ters kayıt; Raporu sekmesinde şartname filtreleri, KPI'lar, detay drawer ve Excel; Muhasebe Kontrolü sekmesinde beklenen–gerçekleşen, açık kalem kapanma ve mirror kontrolleri.", "/Modul/mali-isler?yol=grup-ici-ozel/r1", "Mali İşler › Grup İçi Özel Finansal İşlemler");
            Guncelleme(l, T(21, 9), "Temlik İşlemleri", "Temlik Modülü IT gereksinim dokümanına (v1.0) göre yeniden yazıldı: Kaynak Alacak Seç penceresi ve belge satırlarının tek tek seçimi (kısmi temlik, satır bazında temlik edilebilir bakiye ve rezervasyon), 2–10 taraflı zincir (+ Taraf Ekle / Çıkar, sıra, rol, aldığı / devrettiği / kalan hak), N−1 transfer step, step bazlı muhasebe simülasyonu ve konsolide görünüm, parametrik onay, Temlik Belgesi Oluştur (dinamik imza blokları, versiyon, hash, ön izleme), imza akışı ve imzalı belge yükleme, step bazlı idempotent ERP kaydı ile kısmi entegrasyon / tekrar deneme / ters kayıt; Raporu sekmesinde şartname filtreleri, KPI'lar, zincir detay drawer ve Excel; Muhasebe Kontrolü sekmesinde beklenen–gerçekleşen ve zincir bütünlüğü kontrolleri.", "/Modul/mali-isler?yol=grup-ici-ozel/r2", "Mali İşler › Grup İçi Özel Finansal İşlemler");
            Guncelleme(l, T(21, 9), "Yansıtma İşlemleri", "Yansıtma Modülü IT gereksinim dokümanına (v1.0) göre yeniden yazıldı: Kaynak Fatura Seç penceresi, fatura kalemlerinin tek tek seçilmesi, satır bazında kalan miktar ve kısmi yansıtma, kâr % / birim yansıtma çift yönlü hesap, birden fazla hedef şirkete dağıtım, sağ panelde özet ve validasyon, parametrik onay akışı, muhasebe kaydı simülasyonu (eşleme tablosu), idempotent ERP fatura oluşturma, audit log; Raporu sekmesinde şartname filtreleri / KPI'lar / detay drawer / Excel; Muhasebe Kontrolü sekmesinde beklenen–gerçekleşen karşılaştırması, kontrol durumları ve şirketler arası cari mutabakat.", "/Modul/mali-isler?yol=grup-ici-ozel/r0", "Mali İşler › Grup İçi Özel Finansal İşlemler");
            Kaldirma(l, T(21, 9), "Modül Gösterge Panelleri", "Modül, alt menü ve süreç grubu sayfalarındaki örnek KPI kartları, trend grafiği, bekleyen işler ve hızlı erişim panelleri tüm departmanlarda kaldırıldı.", "/Modul/mali-isler", "Mali İşler");
            Guncelleme(l, T(21, 9), "Modül Sayfası", "Modüle tıklayınca gösterge paneli yerine organizasyon şeması açılır: modül kaça ayrılıyor, hangi süreç grubunda hangi rapor var. Kutulara tıklayınca ilgili bölüm / rapor açılır. Son 30 günde eklenen raporlar şemada, sol menüde ve listede \"(Yeni)\" rozetiyle işaretlenir.", "/Modul/mali-isler", "Mali İşler");
            Guncelleme(l, T(21, 9), "Tahsilat Analiz Raporu", "Yönetim › Özel Raporlar altındaki \"Analiz Raporu\" ekranı \"Tahsilat Analiz Raporu\" olarak yeniden adlandırıldı (ekran başlığıyla aynı). Grup sırası: Tahsilat Analiz Raporu, Genel Analiz Raporu, Çalan İşlem Raporu.");   // adres ve yol menüden otomatik bulunur
            Guncelleme(l, T(21, 9), "Aylık Alım Onay Ekranı", "Liste sütunları: \"Kullanım Şirketi\" → \"Kullanıcı Firma\"; sağına \"Yansıtılma Durumu\" (Gerekmiyor / Yansıtma Bekliyor / Yansıtıldı + yansıtma talep no) eklendi; \"Vade / Ödeme\" listeden kaldırıldı (detayda duruyor).", "/Modul/tedarik-zinciri?yol=satin-alma/fiyat-alim-analizi/r0", "Tedarik Zinciri › Satın Alma › Fiyat ve Alım Analizi");
            Guncelleme(l, T(21, 9), "Aylık Alım Onay Ekranı", "Mükerrer fatura kontrolü: aynı tedarikçi + aynı fatura no + aynı tutarla ikinci kayıt varsa kesin uyarı verilir ve fatura numarasının yanında kırmızı MÜKERRER rozeti çıkar; aynı tutarlı farklı numaralı faturalar \"şüphe\" olarak kalır.", "/Modul/tedarik-zinciri?yol=satin-alma/fiyat-alim-analizi/r0", "Tedarik Zinciri › Satın Alma › Fiyat ve Alım Analizi");
            Guncelleme(l, T(21, 9), "Rapor Tabloları", "Tüm raporlarda sütun hizası düzeltildi: sayısal sütunların başlığı ve toplam hücresi de sağa yaslanır, sıralama ikonu başlık metninin yanında durur; başlık ile rakamlar aynı hizada.", "/Rapor20", "Cari Ekstre");
            Guncelleme(l, T(21, 9), "Aylık Alım Onay Ekranı", "Gider / Gelir Seçim Ekranı düzenine geçildi: satır sonunda Detay sütunu — \"Fatura\" kalem detayı, uyarılar ve onayı pencerede, \"Görüntü\" e-fatura görüntüsünü tam ekran pencerede açar; sağ panel kaldırıldı, liste tam genişlik.", "/Modul/tedarik-zinciri?yol=satin-alma/fiyat-alim-analizi/r0", "Tedarik Zinciri › Satın Alma › Fiyat ve Alım Analizi");
            Guncelleme(l, T(21, 9), "Grup İçi Özel Finansal İşlemler", "Sekme yapısı şartnameye göre kuruldu: Yansıtma, Mahsup ve Temlik ekranlarının üstünde aynı sayfada üç sekme — İşlem Ekranı (varsayılan), Raporu, Muhasebe Kontrolü. Rapor ve kontrol ayrı menü değildir.", "/Modul/mali-isler?yol=grup-ici-ozel/r0", "Mali İşler › Grup İçi Özel Finansal İşlemler");
            Guncelleme(l, T(21, 9), "Grup İçi Özel Finansal İşlemler", "Yansıtma, Mahsup, Temlik ve Rapor ekranları birbirinden ayrıldı: ekran üstündeki tür sekmeleri ve \"tek listede\" rozeti kaldırıldı; ekranlar arası geçiş sol menüden yapılır.", "/Modul/mali-isler?yol=grup-ici-ozel/r0", "Mali İşler › Grup İçi Özel Finansal İşlemler");
            Guncelleme(l, T(21, 9), "Grup İçi Özel Finansal İşlemler", "Ekran adları sadeleştirildi: \"Yansıtma / Mahsup / Temlik İşlem Ekranı\" → \"Yansıtma İşlemleri\", \"Mahsup İşlemleri\", \"Temlik İşlemleri\".", "/Modul/mali-isler?yol=grup-ici-ozel/r0", "Mali İşler › Grup İçi Özel Finansal İşlemler");
            Guncelleme(l, T(21, 9), "Grup İçi Özel Finansal İşlemler Raporu", "Uygulama açıldıktan sonra ilk açılan ekran bu rapor olduğunda talep motoru başlatılamıyor ve tüm talep ekranları 500 veriyordu; düzeltildi. Araç tohum verisindeki sabit ehliyet tarihi de bugüne göre hesaplanır oldu.");
            Guncelleme(l, T(21, 9), "Ana Sayfa", "Yeni Eklenenler ve Son Değişiklikler kartları eklendi; tam liste Ayarlar › Sistem altından da açılır.", "/", "Ana Sayfa");
            Guncelleme(l, T(19, 9), "Modül Sayfası", "Çerçevede açılan raporda \"Menüleri gizle\" ile sol menüler kapanıp rapor tam genişlik alır; tercih hatırlanır.", "/Modul/mali-isler", "Mali İşler");
            Tasima(l, T(18, 9), "Talep › İK ve İdari İşler", "Sol menüdeki İzin / Mesai / Avans / Belge / Seyahat / Araç kırılımı kaldırıldı; başlık İzin Talebi'ni açar, diğer türler ekranın üstündeki sekmelerden seçilir.", "/Modul/talep?yol=yeni-talep/ik-idari-talepler/r0", "Talep");
            Tasima(l, T(16, 9), "Nakit Avans — Bekleyen Ödemeler", "Talep › İK ve İdari İşler altından Mali İşler › Finans › Ödeme Yönetimi altına taşındı (finansın çalıştığı ekran).");
            Guncelleme(l, T(15, 9), "Rapor Tabloları", "Tüm raporlarda başlığa tıklayınca sıralama ve başlık altında sütun filtreleri (≤40 farklı değerde liste, aksi halde arama).", "/Rapor20", "Cari Ekstre");
            Guncelleme(l, T(15, 9), "Rapor Teması", "Inter yazı tipi, tek marka paleti, durum sözcükleri renkli rozet, sayısal hücreler sağa yaslı, eksi tutarlar kırmızı.", "/Rapor28", "Mizan");
            Tasima(l, T(15, 9), "Çek ve İbraz Ekranları", "Çek Akıbeti, Çek Konsolide, İbraz ve Bordro ekranları Mali İşler › Finans › Finansal Araçlar çatısında toplandı.", "/Modul/mali-isler?yol=finans/finansal-araclar/r0", "Mali İşler");
            Tasima(l, T(14, 9), "Rapor Paneli", "Eski ana sayfa rapor kartları Ayarlar › Sistem › Rapor Paneli adresine taşındı; raporlar Grup ERP modüllerine göre gruplanır.", "/Home/RaporPaneli", "Rapor Paneli");
            Guncelleme(l, T(14, 9), "Ana Sayfa", "Grup ERP kart düzeni: profil, izin bilgileri, yaklaşan izinler, doğum günleri, mesajlar (sohbet + AI asistan), görevler, performans.", "/", "Ana Sayfa");
            Guncelleme(l, T(13, 9), "Sol Menü", "Grup ERP Portalı modül listesi ve üst çubuk uyarlandı; menü yapısında arama (\"/\" tuşu odaklar).", "/Modul/mali-isler", "Mali İşler");

            return l;
        }

        // ---------- Kayıt yardımcıları ----------
        private static void Yeni(List<PortalDegisiklik> l, DateTime tarih, string ekran, string aciklama, string href = null, string sekme = null)
            => Ekle(l, "yeni", tarih, ekran, aciklama, href, sekme);
        private static void Tasima(List<PortalDegisiklik> l, DateTime tarih, string ekran, string aciklama, string href = null, string sekme = null)
            => Ekle(l, "tasima", tarih, ekran, aciklama, href, sekme);
        private static void Guncelleme(List<PortalDegisiklik> l, DateTime tarih, string ekran, string aciklama, string href = null, string sekme = null)
            => Ekle(l, "guncelleme", tarih, ekran, aciklama, href, sekme);
        private static void Kaldirma(List<PortalDegisiklik> l, DateTime tarih, string ekran, string aciklama, string href = null, string sekme = null)
            => Ekle(l, "kaldirma", tarih, ekran, aciklama, href, sekme);

        private static void Ekle(List<PortalDegisiklik> l, string tur, DateTime tarih, string ekran, string aciklama, string href, string yol)
        {
            var k = MenudeBul(ekran);
            l.Add(new PortalDegisiklik
            {
                Tur = tur, Tarih = tarih, Ekran = ekran, Aciklama = aciklama,
                Href = href ?? k?.Href,
                Yol = yol ?? k?.Yol ?? ""
            });
        }

        /// <summary>Ekranı KurumsalMenu'da ada göre bulur; modül sayfası derin bağlantısı ve menü yolu metni döner.</summary>
        private static MenuKonum MenudeBul(string ad)
        {
            string anahtar = KurumsalMenu.Anahtar(ad);
            foreach (var m in KurumsalMenu.Moduller.Where(x => x.ModulMu))
                foreach (var a in m.Alt)
                {
                    var gruplar = a.Alt != null
                        ? a.Alt.Select(b => new { Grup = b, Yol = $"{m.Ad} › {a.Ad} › {b.Ad}", Key = $"{a.Key}/{b.Key}" })
                        : new[] { new { Grup = a, Yol = $"{m.Ad} › {a.Ad}", Key = a.Key } };
                    foreach (var g in gruplar)
                    {
                        var raporlar = g.Grup.Raporlar ?? new List<KmRapor>();
                        for (int i = 0; i < raporlar.Count; i++)
                            if (KurumsalMenu.Anahtar(raporlar[i].Ad) == anahtar && string.IsNullOrEmpty(raporlar[i].Kisayol))
                                return new MenuKonum { Href = $"/Modul/{m.Key}?yol={g.Key}/r{i}", Yol = g.Yol };
                    }
                }
            return null;
        }

        private class MenuKonum { public string Href; public string Yol; }
    }
}
