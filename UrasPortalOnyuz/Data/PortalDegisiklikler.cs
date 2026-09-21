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
            Yeni(l, T(21, 9), "Genel Analiz Raporu", "Yönetim › Yönetim Özeti › Özel Analizler › Özel Raporlar altında ayrı ekran: şirket ve yıla göre aylık satış / alım / brüt kâr / tahsilat / ödeme seyri ve dönem özeti (örnek veri).");
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
            Kaldirma(l, T(21, 9), "Modül Gösterge Panelleri", "Modül, alt menü ve süreç grubu sayfalarındaki örnek KPI kartları, trend grafiği, bekleyen işler ve hızlı erişim panelleri tüm departmanlarda kaldırıldı.", "/Modul/mali-isler", "Mali İşler");
            Guncelleme(l, T(21, 9), "Modül Sayfası", "Modüle tıklayınca gösterge paneli yerine organizasyon şeması açılır: modül kaça ayrılıyor, hangi süreç grubunda hangi rapor var. Kutulara tıklayınca ilgili bölüm / rapor açılır. Son 30 günde eklenen raporlar şemada, sol menüde ve listede \"(yeni)\" rozetiyle işaretlenir.", "/Modul/mali-isler", "Mali İşler");
            Guncelleme(l, T(21, 9), "Tahsilat Analiz Raporu", "Yönetim › Özel Raporlar altındaki \"Analiz Raporu\" ekranı \"Tahsilat Analiz Raporu\" olarak yeniden adlandırıldı (ekran başlığıyla aynı). Grup sırası: Tahsilat Analiz Raporu, Genel Analiz Raporu, Çalan İşlem Raporu.", "/Modul/yonetim?yol=yonetim-ozeti/ozel-analizler/ozel-raporlar/r0", "Yönetim › Yönetim Özeti › Özel Analizler › Özel Raporlar");
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
