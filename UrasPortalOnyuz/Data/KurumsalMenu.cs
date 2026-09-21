// Uras Holding Kurumsal Menü ve Rapor Mimarisi — Grup ERP Portalı'ndaki (WebApplication1/wwwroot/menu-config.js)
// MASTER yerleşim tablosunun C# karşılığı. Ana Menü → 1. Alt Menü → 2. Alt Menü → Rapor / Ekran.
//
// Rapor adları bu projedeki mevcut ekranlara (MenuKatalogu) ada göre otomatik eşlenir:
//   * eşleşen  → "hazır ekran" (Controller/Action dolu, modül sayfasında çerçeve içinde açılır)
//   * kısayol  → ana sahibi başka modülde olan kayıt (kisayol: "#modul/alt1/alt2")
//   * eşleşmeyen → "planlanıyor" (ekran henüz yok)
// Ad farkları (yazım/ek) için Takma adlar tablosuna bakın. Menüde yeri belirlenmemiş mevcut ekranlar
// Ayarlar › Sistem › "Menüde Yeri Belirlenmemiş Ekranlar" altında otomatik listelenir; hiçbir ekran kaybolmaz.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    public class KmRapor
    {
        public string Ad { get; set; }
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Kisayol { get; set; }
        public bool Hazir => !string.IsNullOrEmpty(Controller);
        public string Durum => Hazir ? "hazir" : !string.IsNullOrEmpty(Kisayol) ? "kisayol" : "plan";
    }

    public class KmGrup
    {
        public string Key { get; set; }
        public string Ad { get; set; }
        public string Not { get; set; }
        /// <summary>2. alt menüler (doluysa Raporlar boştur).</summary>
        public List<KmGrup> Alt { get; set; }
        public List<KmRapor> Raporlar { get; set; }
        /// <summary>Doğrudan açılan grup: sol menüde kırılım (alt liste) gösterilmez, başlığa tıklayınca ilk ekran açılır (ekran içinde tür sekmeleri vardır).</summary>
        public bool Dogrudan { get; set; }
        public int RaporSayisi => Raporlar != null ? Raporlar.Count : (Alt ?? new List<KmGrup>()).Sum(a => a.RaporSayisi);
        public int HazirSayisi => Raporlar != null ? Raporlar.Count(r => r.Hazir) : (Alt ?? new List<KmGrup>()).Sum(a => a.HazirSayisi);
    }

    public class KmModul
    {
        public string Key { get; set; }
        public string Ad { get; set; }
        /// <summary>KurumsalMenu.Ikonlar anahtarı (SVG).</summary>
        public string Ikon { get; set; }
        public string Amac { get; set; }
        public string[] Kpi { get; set; }
        /// <summary>Modül sayfası yerine doğrudan açılacak adres (ör. Ana Sayfa).</summary>
        public string Href { get; set; }
        public List<KmGrup> Alt { get; set; }
        public bool ModulMu => Alt != null;
        public int RaporSayisi => (Alt ?? new List<KmGrup>()).Sum(a => a.RaporSayisi);
        public int HazirSayisi => (Alt ?? new List<KmGrup>()).Sum(a => a.HazirSayisi);
    }

    public static class KurumsalMenu
    {
        // ---------- SVG ikonlar (menu-config.js › MENU_ICONS) ----------
        public static readonly Dictionary<string, string> Ikonlar = new Dictionary<string, string>
        {
            ["home"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"3\" y=\"3\" width=\"8\" height=\"8\" rx=\"2\"/><rect x=\"13\" y=\"3\" width=\"8\" height=\"8\" rx=\"2\"/><rect x=\"3\" y=\"13\" width=\"8\" height=\"8\" rx=\"2\"/><rect x=\"13\" y=\"13\" width=\"8\" height=\"8\" rx=\"2\"/></svg>",
            ["talep"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z\"/><path d=\"M14 3v6h6M12 12v6M9 15h6\"/></svg>",
            ["onay"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M9 11l3 3L22 4\"/><path d=\"M21 12v7a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h11\"/></svg>",
            ["mali"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"3\" y=\"5\" width=\"18\" height=\"14\" rx=\"2\"/><path d=\"M3 10h18M7 15h4\"/></svg>",
            ["satis"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M4 20V10M10 20V4M16 20v-7M22 20H2\"/></svg>",
            ["marka"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M4 11v2a1 1 0 0 0 1 1h2l5 4V6L7 10H5a1 1 0 0 0-1 1z\"/><path d=\"M16 9a4 4 0 0 1 0 6\"/></svg>",
            ["tedarik"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M3 4h2l2.5 11h11L21 7H6.5\"/><circle cx=\"9\" cy=\"19\" r=\"1.5\"/><circle cx=\"17\" cy=\"19\" r=\"1.5\"/></svg>",
            ["uretim"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M3 20V9l5 3V9l5 3V9l5 3v8H3z\"/><path d=\"M17 4h3v6\"/></svg>",
            ["ik"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><circle cx=\"9\" cy=\"8\" r=\"3.5\"/><path d=\"M2.5 20a6.5 6.5 0 0 1 13 0M16 4a3.5 3.5 0 0 1 0 7M21.5 20a6.5 6.5 0 0 0-5-6.3\"/></svg>",
            ["bt"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"3\" y=\"4\" width=\"18\" height=\"12\" rx=\"2\"/><path d=\"M8 20h8M12 16v4\"/></svg>",
            ["hukuk"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M12 3v18M5 7h14M5 7l-3 7a3 3 0 0 0 6 0L5 7zM19 7l-3 7a3 3 0 0 0 6 0l-3-7zM8 21h8\"/></svg>",
            ["yonetim"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><path d=\"M14 3H6a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V9z\"/><path d=\"M14 3v6h6M8 13h8M8 17h5\"/></svg>",
            ["takvim"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><rect x=\"3\" y=\"5\" width=\"18\" height=\"16\" rx=\"2\"/><path d=\"M3 10h18M8 3v4M16 3v4\"/></svg>",
            ["ayar"] = "<svg viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\"><circle cx=\"12\" cy=\"12\" r=\"3\"/><path d=\"M12 2v3M12 19v3M2 12h3M19 12h3M4.9 4.9l2.1 2.1M17 17l2.1 2.1M4.9 19.1L7 17M17 7l2.1-2.1\"/></svg>",
            ["chev"] = "<svg class=\"chev\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2.2\"><path d=\"m9 6 6 6-6 6\"/></svg>"
        };

        // ---------- Takma adlar: Grup ERP'deki ad → bu projedeki denetleyici ----------
        // (Yazım farkı, ek açıklama ya da farklı adla kayıtlı aynı ekran.)
        private static readonly Dictionary<string, string> TakmaAdlar = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Günlük Çek Nakit Tahsilat"] = "Rapor73",                                   // katalogda "Tahsiilat" yazımıyla
            ["Cari & Hesap Ekstre Karşılaştırma"] = "Rapor141",                          // "Cari Ekstre / Hesap Ekstresi Mutabakatı"
            ["İrsaliye / Fatura Aktarım Denetimi (URAS → SELVİ)"] = "Rapor145",
            ["Aktarım Robotu Kontrol Paneli - AVRASYA"] = "Rapor144",                    // "Aktarım Veri Denetimi (AVRASYA)"
            ["Aktarım Robotu Kontrol Paneli - DAF"] = "Rapor153",
            ["Aktarım Robotu Kontrol Paneli - AVRUPA PAPER"] = "Rapor154",
            ["Aktarım Robotu Kontrol Paneli - URAS HOLDİNG"] = "Rapor155",
            ["Aktarım Robotu Kontrol Paneli - ALV FİLO"] = "Rapor156",
            ["Aktarım Robotu Kontrol Paneli - URAS BASKI"] = "Rapor157",
            ["Robot Servis Durumları"] = "RobotServis",                                  // "Aktarım Robotu Servisleri"
            ["Satınalma Talebi"] = "Rapor159",                                           // "Satınalma Talebi Oluştur"
            ["Parti Yaşlandırma"] = "Rapor172",                                          // "Parti Yaşlandırma (Stok Yaşı)"
            // Grup ERP'de cari.html'e giden kartlar → buradaki Cari Bilgileri ekranı
            ["Müşteri Cari Kartları"] = "Rapor120",
            ["Tedarikçi Cari Kartları"] = "Rapor120",
            ["Yurtdışı Müşteri Kartları"] = "Rapor120",
            ["Yurtdışı Tedarikçiler"] = "Rapor120",
            ["Personel Carileri"] = "Rapor120",
            ["Banka ve Finans Kuruluşları"] = "Rapor120",
            ["Hukuki Süreçteki Cariler"] = "Rapor120",
        };

        // ---------- Yardımcılar ----------
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        /// <summary>Ad karşılaştırma anahtarı: Türkçe küçük harf, yalnızca harf ve rakam.</summary>
        public static string Anahtar(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (char c in s.ToLower(Tr)) if (char.IsLetterOrDigit(c)) sb.Append(c);
            return sb.ToString();
        }

        private static AppMenu EkranBul(string ad)
        {
            if (TakmaAdlar.TryGetValue(ad, out var ctrl))
                return OrnekVeri.Menuler.FirstOrDefault(m => string.Equals(m.ControllerName, ctrl, StringComparison.OrdinalIgnoreCase));
            string k = Anahtar(ad);
            return OrnekVeri.Menuler.FirstOrDefault(m => Anahtar(m.MenuTitle) == k);
        }

        /// <summary>Ada göre rapor (mevcut ekrana eşlenir; bulunamazsa planlanıyor).</summary>
        private static KmRapor R(string ad)
        {
            var e = EkranBul(ad);
            return new KmRapor { Ad = ad, Controller = e?.ControllerName, Action = e?.ActionName };
        }

        /// <summary>Belirli denetleyiciye bağlı rapor.</summary>
        private static KmRapor R(string ad, string controller, string action = null)
        {
            var e = OrnekVeri.Menuler.FirstOrDefault(m => string.Equals(m.ControllerName, controller, StringComparison.OrdinalIgnoreCase) && (action == null || string.Equals(m.ActionName, action, StringComparison.OrdinalIgnoreCase)));
            return new KmRapor { Ad = ad, Controller = e?.ControllerName ?? controller, Action = e?.ActionName ?? action ?? "Index" };
        }

        /// <summary>Ana sahibi başka yerde olan kayıt (kısayol).</summary>
        private static KmRapor K(string ad, string kisayol) => new KmRapor { Ad = ad, Kisayol = kisayol };

        /// <summary>Rapor taşıyan grup (2. alt menü ya da doğrudan rapor taşıyan 1. alt menü).</summary>
        private static KmGrup G(string key, string ad, params KmRapor[] raporlar) => new KmGrup { Key = key, Ad = ad, Raporlar = raporlar.ToList() };

        /// <summary>2. alt menüleri olan 1. alt menü.</summary>
        private static KmGrup U(string key, string ad, params KmGrup[] alt) => new KmGrup { Key = key, Ad = ad, Alt = alt.ToList() };

        private static KmGrup Not(KmGrup g, string not) { g.Not = not; return g; }

        /// <summary>Sol menüde kırılımsız: başlık tıklanınca ilk ekran açılır.</summary>
        private static KmGrup Dogrudan(KmGrup g) { g.Dogrudan = true; return g; }

        private static KmModul M(string key, string ad, string ikon, string amac, string[] kpi, params KmGrup[] alt) => new KmModul { Key = key, Ad = ad, Ikon = ikon, Amac = amac, Kpi = kpi, Alt = alt.ToList() };

        private static string[] Kpi(params string[] k) => k;

        // ---------- MASTER yerleşim tablosu ----------
        private static readonly Lazy<List<KmModul>> _moduller = new Lazy<List<KmModul>>(Olustur);
        public static List<KmModul> Moduller => _moduller.Value;

        public static KmModul Bul(string key) => Moduller.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

        private static List<KmModul> Olustur()
        {
            var liste = new List<KmModul>
            {
                new KmModul { Key = "ana", Ad = "Ana Sayfa", Ikon = "home", Href = "/" },

                M("talep", "Talep", "talep",
                  "Şirket genelindeki talep oluşturma ve talep takibi merkezi. Durumlar aynı ekranda sekme / filtre olarak çalışır.",
                  Kpi("Yeni Talepler", "Taleplerim", "Bekleyen Talepler", "Sonuçlanan Talepler"),
                  U("yeni-talep", "Yeni Talep",
                    G("satin-alma", "Satın Alma", R("Satınalma Talebi")),
                    G("tedarikci", "Tedarikçi", R("Tedarikçi Talep Formu"))),
                  // İK ve İdari İşler departmanına giden talepler (Controllers/IkTalepController.cs).
                  // Anahtar bilerek modül anahtarından ("ik-idari-isler") farklı: kurumsal-modul.js modül anahtarıyla başlayan hash'i o modüle yönlendirir.
                  // Sol menüde kırılım yok (Dogrudan): tıklayınca İzin Talebi açılır, diğer türler ekranın üstündeki sekmelerden seçilir.
                  Dogrudan(G("ik-idari-talepler", "İK ve İdari İşler",
                    R("İzin Talebi"), R("Mesai Talebi"), R("Avans Talebi"), R("Belge Talebi"), R("Seyahat Talebi"), R("Araç Talebi")))),

                M("onaylarim", "Onaylarım", "onay",
                  "Tüm onaylar tek merkezde. Bekleyen / Tamamlanan ayrı rapor değildir; aynı onay ekranında durum sekmeleri olarak çalışır.",
                  Kpi("Bekleyen Onay", "Bugün Gelen", "Geciken Onay", "Tamamlanan"),
                  U("genel-onaylar", "Genel Onaylar",
                    G("onay-merkezi", "Onay Merkezi", R("Onay Ekranı"))),
                  U("mali-isler-onaylari", "Mali İşler Onayları",
                    G("gider-kod-onaylari", "Gider ve Kod Onayları", R("Gider Onay Ekranı"), R("Gider / Gelir Kodu Talep Onayı")),
                    G("kasa-onaylari", "Kasa Onayları", R("Kasa Onay"))),
                  U("satin-alma-onaylari", "Satın Alma Onayları",
                    G("talep-onaylari", "Talep Onayları", R("Satınalma Talebi Onay Ekranı"), R("Tedarikçi Talebi Onayı", "Rapor134"), K("Aylık Alım Onay Ekranı", "#tedarik-zinciri/satin-alma/fiyat-alim-analizi"))),
                  U("satis-onaylari", "Satış Onayları",
                    G("donem-onaylari", "Dönem Onayları", R("Aylık Satış Onay Raporu"))),
                  U("ik-onaylari", "İK Onayları",
                    G("izin-onaylari", "İzin Onayları", R("İzin Onay Bekleyenler")))),

                M("mali-isler", "Mali İşler", "mali",
                  "Finans, Muhasebe, Cari İşler ve Grup İçi Özel Finansal İşlemler 1. alt menü olarak açılır; raporlar 2. alt menüler altında toplanır.",
                  Kpi("Nakit / Banka", "Ödeme Bekleyen", "Tahsilat Bekleyen", "Vadesi Geçen Alacak"),
                  Not(U("finans", "Finans",
                    G("nakit-banka", "Nakit ve Banka Yönetimi", R("Kasa / Banka Bakiye Raporu"), R("Finekra Hareketler"), R("İhracat Finekra Hareketler"), R("Banka ve Finans Kuruluşları")),
                    G("nakit-akisi-likidite", "Nakit Akışı ve Likidite", R("Kasa Nakit Akışı"), R("Kasa Nakit Akışı Görünüm"), R("Konsolide Günlük Finansal Hareketler"), R("Nakit Akışı (Ödeme Bekleyenler)"), R("Nakit Akışı (Ödenenler)")),
                    G("odeme-yonetimi", "Ödeme Yönetimi", R("Planlı Ödemeler"), R("Yapılan Ödemeler"), R("İthalat Ödeme Raporu"), R("Nakit Avans — Bekleyen Ödemeler"), R("Nakit Avans Takip Raporu")),
                    G("finansal-araclar", "Finansal Araçlar", R("Çek Akıbeti Raporu"), R("Çek Akıbeti Kontrol Raporu"), R("Çek Konsolide Raporu"), R("Çek Konsolide Kontrol Raporu"), R("Çek Ekstre Raporu"), R("Günlük Çek Nakit Tahsilat"), R("İbraz (Çek Depositi)"), R("Vadeli Çek İbrazı"), R("Tahsilat İbraz Çek Bordrosu"), R("Grup İçi Çek Devirleri"), R("Muhasebe Çek Akıbeti Raporu"), R("Muhasebe Çek İbraz Kontrolü")),
                    G("doviz-dis-ticaret", "Döviz ve Dış Ticaret Finansmanı", R("Kur Farkı"), R("İBKB Beyanname Takibi"), R("İhracat Tahsilat Raporu"))),
                    "Çek ve ibraz ekranları \"Finansal Araçlar\" çatısında toplanmıştır."),
                  U("muhasebe", "Muhasebe",
                    G("genel-muhasebe", "Genel Muhasebe", R("Mizan"), R("Mizan Karşılaştırma"), R("Hesap Ekstre Raporu"), R("Yevmiye Kaydı"), R("Açılış Kapanış Kayıt"), R("Virman İşlemleri"), R("Toplu Mahsup Oluştur")),
                    G("alis-satis-muhasebesi", "Alış ve Satış Muhasebesi", R("Aylık Alım Onay Ekranı", "AlimOnay", "Index"), R("Alış Raporu"), R("Alım Raporu"), R("Muhasebe Satış Raporu"), R("Muhasebe Belge Bazlı Satış Raporu"), R("Muhasebe Satış Raporu (İhracat)")),
                    G("vergi-kdv", "Vergi ve KDV", R("İndirilecek KDV Listesi"), R("KDV Durum Raporu"), R("Satış Alış İade KDV Raporu")),
                    G("fatura-belge", "Fatura ve Belge Yönetimi", R("E-Belge"), R("Fatura İnceleme"), R("Fatura Mutabakat Raporu - QNB"), R("İrsaliye Mutabakat Raporu - QNB"), R("Taslak Satıcı Faturaları Raporu"), R("Yansıtma Faturası")),
                    G("gider-gelir", "Gider ve Gelir Yönetimi", R("Gider / Gelir Seçim Ekranı"), R("Gider / Gelir Kod Yönetimi"), R("Gider / Gelir Analizi"), R("Gider Seçim Raporu"), R("Genel Gider Raporu Muavin"), R("Aylık Gider Raporu")),
                    G("stok-maliyet", "Stok ve Maliyet Muhasebesi", R("Stok Maliyeti"), R("Denge Malzeme Raporu")),
                    G("grup-ici", "Grup İçi İşlemler", R("Grup İçi Analiz Raporu"))),
                  U("cari-isler", "Cari İşler",
                    G("cari-hesap", "Cari Hesap Yönetimi", R("Cari Bilgileri"), R("Muhattap Ana Verileri"), R("Muhatap (Cari) Ana Verileri"), R("Cari Ekstre Raporu"), R("Cari Bakiye Raporu"), R("Adat Hesaplama")),
                    G("alacak-tahsilat", "Alacak ve Tahsilat Takibi", R("Tahsilat"), R("Fatura ve Tahsilat Takip Raporu"), R("Konsolide Tahsilat Takip Raporu"), R("Bekleyen Cari İşlemler Raporu")),
                    G("tedarikci-borc", "Tedarikçi ve Borç Takibi", R("Tedarikçi Fatura ve Ödeme Takip Raporu"), R("Tedarikçi Konsolide Ödeme Takip Raporu")),
                    G("mutabakat-kapama", "Mutabakat ve Kapama", R("Mutabakat"), R("E-Mutabakat"), R("Otomatik Kapama"), R("Cari & Hesap Ekstre Karşılaştırma")),
                    G("temlik-ozel", "Temlik ve Özel İşlemler", R("Temlik İşlemleri"), R("Hukuki Süreçteki Cariler"))),
                  // Grup şirketleri arası yansıtma, mahsup ve temlik işlemleri (Controllers/GrupIciController.cs; talep motoru)
                  G("grup-ici-ozel", "Grup İçi Özel Finansal İşlemler",
                    // Adlar Muhasebe › Cari İşler'deki Rapor121 "Temlik İşlemleri" ile çakışmasın diye denetleyiciye bağlı
                    R("Yansıtma İşlemleri", "GrupIci", "Yansitma"), R("Mahsup İşlemleri", "GrupIci", "Mahsup"), R("Temlik İşlemleri", "GrupIci", "Temlik"), R("Grup İçi Özel Finansal İşlemler Raporu", "GrupIci", "Rapor"))),

                M("satis-operasyon", "Satış ve Operasyon", "satis",
                  "Müşteri ilişkisinden satışa, sipariş hazırlığından sevkiyat ve satış sonrası teknik desteğe kadar uçtan uca ticari süreç.",
                  Kpi("Açık Sipariş", "Bekleyen Sevkiyat", "Aylık Satış", "Açık Teknik Talep"),
                  U("musteri-crm", "Müşteri ve CRM Yönetimi",
                    G("musteri-portfoyu", "Müşteri Portföyü", R("Müşteri Cari Kartları")),
                    G("fiyat-musteri-kosullari", "Fiyat ve Müşteri Koşulları", R("Muhattaplar İçn Özel Fiyatlar"))),
                  U("yurtici-satis", "Yurtiçi Satış",
                    G("satis-islemleri", "Satış İşlemleri", R("Satış Siparişi"), R("Taslak Faturalar"), R("Satış Faturası"), R("Müşteri İade Faturası"), R("Bedelsiz Satış Raporu"), R("Grup İçi Satış (Uras > Selvi > Müşteri)")),
                    G("satis-analizleri", "Satış Analizleri ve Performans", R("Satış Raporu"), R("Satış Ekstre Raporu"), R("Satış Analiz Raporu"), R("Satış Analiz Dashboard"), R("Satışçı Analiz Raporu"), R("Konsolide Prim Raporu"), R("Tahsilat Prim Tablosu"))),
                  U("yurtdisi-satis", "Yurtdışı Satış",
                    G("ihracat-operasyon", "İhracat Operasyon Takibi", R("İhracat Dosya Raporu"), K("İhracat Tahsilat Raporu", "#mali-isler/finans/doviz-dis-ticaret"), K("İBKB Beyanname Takibi", "#mali-isler/finans/doviz-dis-ticaret"), R("Yurtdışı Müşteri Kartları"))),
                  U("depo-siparis", "Depo ve Sipariş Yönetimi",
                    G("siparis-hazirlama", "Sipariş Hazırlama / Depo Süreçleri")),
                  U("sevkiyat-lojistik", "Sevkiyat ve Lojistik",
                    G("teslimat-yonetimi", "Teslimat Yönetimi", R("Teslimat")),
                    G("iade-sevkiyatlari", "İade Sevkiyatları", R("İade İrsaliyesi"))),
                  U("teknik-destek", "Teknik Destek",
                    G("satis-sonrasi", "Satış Sonrası Teknik Destek"))),

                M("marka-iletisim", "Marka ve İletişim", "marka",
                  "Marka, pazarlama, kurumsal iletişim ve dijital kanallar için ortak çalışma alanı. Mevcut rapor listesinde kayıt yok; ekranlar yeni geliştirilecek.",
                  Kpi("Aktif Kampanya", "Yaklaşan Etkinlik", "İçerik Takvimi", "Açık İletişim Aksiyonu"),
                  G("marka-yonetimi", "Marka Yönetimi"),
                  G("pazarlama", "Pazarlama"),
                  G("kurumsal-iletisim", "Kurumsal İletişim")),

                M("tedarik-zinciri", "Tedarik Zinciri", "tedarik",
                  "Tedarikçi yönetimi, satın alma, ithalat ve malzeme/stok süreçleri aynı sayfada süreç sırasıyla yönetilir.",
                  Kpi("Açık Satın Alma", "Bekleyen Mal Kabul", "Açık İthalat Dosyası", "Kritik Stok"),
                  U("tedarikci-yonetimi", "Tedarikçi Yönetimi",
                    G("tedarikci-kartlari", "Tedarikçi Kartları / Değerlendirme", R("Tedarikçi Cari Kartları"), K("Tedarikçi Talep Formu", "#talep/yeni-talep/tedarikci"))),
                  U("satin-alma", "Satın Alma",
                    G("talep-siparis", "Talep ve Sipariş Yönetimi", R("Direkt Satınalma Açık Talep ve Sipariş Raporu"), R("Satınalma Siparişi"), R("Toptan Alış Siparişleri Kontrol Listesi"), K("Satınalma Talebi", "#talep/yeni-talep/satin-alma"), K("Satınalma Talebi Onay Ekranı", "#onaylarim/satin-alma-onaylari/talep-onaylari")),
                    // Aylık Alım Onay Ekranı: Muhasebe'nin hazırladığı aylık alım raporunu satınalmacılar ve Tedarik Zinciri Direktörü ay sonunda onaylar (AlimOnayController). Muhasebe'de de aynı ekran.
                    G("fiyat-alim-analizi", "Fiyat ve Alım Analizi", R("Aylık Alım Onay Ekranı", "AlimOnay", "Index"), R("Satınalma Fiyat Analiz Raporu"), R("Hammadde Fiyat Raporu"), R("Alış Ekstre Raporu"), R("Konsolide Alım Raporu")),
                    G("fatura-iade", "Fatura ve İade", R("Satınalma Faturası"), R("Satınalma İade Faturası"))),
                  U("ithalat", "İthalat",
                    G("ithalat-siparis-maliyet", "İthalat Sipariş ve Maliyet", R("Satınalma Siparişi (İthalat)"), R("İthalat Maliyet Raporu"), K("İthalat Ödeme Raporu", "#mali-isler/finans/odeme-yonetimi"), R("Yurtdışı Tedarikçiler"))),
                  Not(U("malzeme-stok", "Malzeme ve Stok Yönetimi",
                    G("malzeme-ana-verileri", "Malzeme Ana Verileri", R("Kalem Ana Verileri")),
                    G("mal-kabul-iade", "Mal Kabul ve İade", R("Satınalma Mal Girişi"), R("Satınalma Mal Girişi Oluştur"), R("Satınalma İade İrsaliyesi"), R("İade Edilen Malzeme Raporu")),
                    G("malzeme-hareketleri", "Malzeme Hareketleri", R("Malzeme Ekstre Raporu"), R("Ürün Ekstre Raporu")),
                    G("stok-envanter", "Stok ve Envanter", R("Envanter Raporu"), R("Depo Bazlı Stok Raporu"), R("Stok Analiz Raporu"))),
                    "\"Satınalma Mal Girişi\" kaynak listede iki ayrı ekran olduğu için iki kayıt da korunmuştur.")),

                M("uretim-kalite", "Üretim ve Kalite", "uretim",
                  "Üretim ve kalite tek çatı altında; Üretim, Kalite Güvence ve Kalite Kontrol 1. alt menülerdir.",
                  Kpi("Planlanan Üretim", "Gerçekleşen Üretim", "Kontrol Bekleyen Parti", "Açık Uygunsuzluk"),
                  U("uretim", "Üretim",
                    G("ar-ge", "Ar-Ge ve Ürün Geliştirme"),
                    G("recete-urun", "Reçete ve Ürün Yönetimi", R("Ürün Detay Raporu"), R("Ürün Hammadde Ekstresi"), R("Ürün Hammadde (Ambalaj)"), R("Ürün İsimlendirme ve Kod Karşılaştırma")),
                    G("uretim-planlama", "Üretim Planlama", R("Urs Üretim Planlama"), R("Üretim Simülasyonu")),
                    G("uretim-operasyonlari", "Üretim Operasyonları", R("Üretim Miktarları Raporu (UR / YM)")),
                    G("uretim-stok", "Üretim Stok Yönetimi", R("Stok Raporu"), R("Parti Yaşlandırma"))),
                  U("kalite-guvence", "Kalite Güvence",
                    G("kalite-sistemleri", "Kalite Sistemleri ve Dokümantasyon"),
                    G("denetim-uygunluk", "Denetim ve Uygunluk"),
                    G("uygunsuzluk", "Uygunsuzluk ve Düzeltici Faaliyetler"),
                    G("surekli-iyilestirme", "Sürekli İyileştirme")),
                  U("kalite-kontrol", "Kalite Kontrol",
                    G("girdi-kontrol", "Girdi Kontrol"),
                    G("proses-kontrol", "Proses Kontrol"),
                    G("nihai-urun-kontrol", "Nihai Ürün Kontrol"),
                    G("laboratuvar", "Laboratuvar ve Analiz")),
                  // Uras Üretim Portalı (kendi kabuğu olan üretim uygulaması) Üretim ve Kalite altına alındı; ekranları alan bazında gruplanır.
                  Not(U("uras-uretim", "Uras Üretim Portalı",
                    G("uu-genel", "Genel ve Ayarlar", R("Dashboard", "UrasUretim", "Index"), R("Yetkilendirme", "UrasUretimAyarlar", "Index"), R("Mail Ayarları", "UrasUretimAyarlar", "MailAyarlari"), R("Arıza & Bakım", "UrasUretimBakim", "Index")),
                    G("uu-uretim", "Üretim", R("Üretim Siparişi", "UrasUretimUretim", "UretimSiparisi"), R("Saha Üretim", "UrasUretimUretim", "SahaUretim"), R("Silo Dolum", "UrasUretimUretim", "SiloDolum"), R("Numune", "UrasUretimUretim", "Numune"), R("Ürün Ayrıştırma", "UrasUretimUretim", "UrunAyristirma"), R("Ürün Dönüşümü", "UrasUretimUretim", "UrunDonusumu"), R("Seri/Parti Tanımları", "UrasUretimUretim", "SeriPartiTanim"), R("Kalem Ana Verileri", "UrasUretimUretim", "KalemAnaVerileri")),
                    G("uu-urun-agaci", "Ürün Ağacı", R("Ürün Ağacı", "UrasUretimUrunAgaci", "Index"), R("Reçete Birim Maliyet", "UrasUretimUrunAgaci", "ReceteMaliyet")),
                    G("uu-kalite", "Kalite Kontrol", R("Giriş Kalite Kontrol", "UrasUretimKalite", "GirisKK"), R("Satınalma Kalite Kontrol", "UrasUretimKalite", "SatinalmaKK"), R("Su Bazlı Kalite Kontrol", "UrasUretimKalite", "SuBazliKK"), R("Plastik Bazlı Kalite Kontrol", "UrasUretimKalite", "PlastikBazliKK"), R("Ürün Kalite Kontrol", "UrasUretimKalite", "UrunKaliteKontrol"), R("pH Kalibrasyon KK", "UrasUretimKalite", "PHKalibrasyon"), R("KK Kalem Değer Aralıkları", "UrasUretimKalite", "KaliteKontrol"), R("Yaşlandırma Formu", "UrasUretimKalite", "Yaslandirma"), R("Karantina/Red/İmha Tutanağı", "UrasUretimKalite", "Tutanak"), R("Müşteri Şikayet Formu", "UrasUretimKalite", "MusteriSikayet")),
                    G("uu-stok", "Stok", R("Mal Girişi", "UrasUretimStok", "MalGirisi"), R("Mal Çıkışı", "UrasUretimStok", "MalCikisi"), R("Depolar Arası Nakil", "UrasUretimStok", "DepoNakli"), R("Depo Stok Raporu", "UrasUretimStok", "DepoStokRaporu"), R("Stok Sayım", "UrasUretimStok", "StokSayim"), R("Sayılmayan Kalemler", "UrasUretimStok", "SayimEksik"), R("Stok Kayıt Listesi", "UrasUretimStok", "StokKayitListesi"), R("Etiket Yazdırma", "UrasUretimStok", "EtiketYazdir")),
                    G("uu-raporlar", "Raporlar", R("Uras Üretim Raporları", "UrasUretimRapor", "Index"), R("Üretim Raporu", "UrasUretimRapor", "Uretim"), R("Hammadde Tüketim", "UrasUretimRapor", "HammaddeTuketim"), R("İş Emri Performans", "UrasUretimRapor", "Performans"), R("Ürün İzlenebilirlik", "UrasUretimRapor", "Izlenebilirlik"), R("Lot Sıra Kontrol", "UrasUretimRapor", "LotSira"), R("Silo Raporu", "UrasUretimRapor", "Silo"), R("Stok Sayım Raporu", "UrasUretimRapor", "Sayim"), R("Parti Yaşlandırma", "UrasUretimRapor", "StokYasi"))),
                    "Uras Üretim Portalı ekranları; kendi menüsüyle çerçeve içinde açılır.")),

                M("ik-idari-isler", "İK ve İdari İşler", "ik",
                  "İnsan Kaynakları ile İdari İşler tek çatı altında iki ayrı 1. alt menü olarak çalışır.",
                  Kpi("Toplam Çalışan", "İzinde", "Açık Pozisyon", "Bugünkü Ziyaretçi"),
                  U("insan-kaynaklari", "İnsan Kaynakları",
                    G("personel-organizasyon", "Personel ve Organizasyon", R("Personel Carileri")),
                    G("izin-devamlilik", "İzin ve Devamlılık", R("Yaklaşan İzinler", "Home", "Index"), R("Ekip İzin Takvimi"), R("İzin Takip Raporu")),   // tabloda: index.html#yaklasanIzin (ana sayfa kartı)
                    G("performans-is-takibi", "Performans ve İş Takibi", R("Mesai Takip Raporu")),
                    G("personel-maliyet-bordro", "Personel Maliyet ve Bordro", R("Personel Maliyet Tablosu"))),
                  U("idari-isler", "İdari İşler",
                    G("danisma-giris-cikis", "Danışma ve Giriş-Çıkış", R("Danışma Giriş-Çıkış Defteri")),
                    G("arac-seyahat", "Araç ve Seyahat", R("Araç Takvimi (Filo)"), R("Seyahat Takvimi")))),

                M("bilgi-teknolojileri", "Bilgi Teknolojileri", "bt",
                  "BT entegrasyon, otomasyon, envanter ve yapay zeka asistanları için merkezi sayfa.",
                  Kpi("Açık Destek Kaydı", "Çalışan Robot", "Hatalı Entegrasyon", "Zimmetli Cihaz"),
                  U("entegrasyon-otomasyon", "Entegrasyon ve Otomasyon",
                    G("aktarim-robotlari", "Aktarım Robotları", R("Aktarım Robotu Kontrol Paneli - URS"), R("Aktarım Robotu Kontrol Paneli - ALV"), R("Aktarım Robotu Kontrol Paneli - SATINALMA TALEBİ"), R("Aktarım Robotu Kontrol Paneli - SELVİ"), R("Aktarım Robotu Kontrol Paneli - Uras"), R("Aktarım Robotu Kontrol Paneli - ASIA"), R("Aktarım Robotu Kontrol Paneli - AVRASYA"), R("Aktarım Robotu Kontrol Paneli - DAF"), R("Aktarım Robotu Kontrol Paneli - AVRUPA PAPER"), R("Aktarım Robotu Kontrol Paneli - URAS HOLDİNG"), R("Aktarım Robotu Kontrol Paneli - ALV FİLO"), R("Aktarım Robotu Kontrol Paneli - URAS BASKI")),
                    G("servis-aktarim-izleme", "Servis ve Aktarım İzleme", R("Robot Servis Durumları"), R("İrsaliye / Fatura Aktarım Denetimi (URAS → SELVİ)"))),
                  U("bt-envanter-zimmet", "BT Envanter ve Zimmet",
                    G("zimmet-takibi", "Zimmet Takibi", R("Zimmet Takip Raporu"))),
                  U("yapay-zeka", "Yapay Zeka ve Asistanlar",
                    G("kurumsal-ai", "Kurumsal AI", R("AI"), R("Gemini AI Asistan")))),

                // Hukuk: Grup ERP'de ayrı sayfa (hukuk.html) — 23 ana başlık beş üst grupta. Ekranlar henüz bu projede yok.
                M("hukuk", "Hukuk", "hukuk",
                  "Hukuki süreçler, dava ve icra takibi, sözleşmeler ve mevzuat uyumu. 23 ana başlık beş üst grupta toplanır; ekranlar bu portala taşınacak.",
                  Kpi("Devam Eden Süreç", "Bu Hafta Duruşma", "Kritik Süre", "Onay Bekleyen İşlem"),
                  U("genel", "Genel",
                    G("01", "Hukuk Özet Paneli", R("Genel Durum ve Dosya Özeti"), R("Alacak, Borç ve Risk Özeti"), R("Kritik Tarihler ve Bekleyen İşlemler"), R("Faaliyet ve Gider Özeti")),
                    G("02", "Hukuki Takip Raporları", R("Şirket Takip Raporu"), R("Yönetici Takip Raporu"), R("Müşteri Takip Raporu"), R("Tedarikçi Takip Raporu"), R("Personel Takip Raporu"), R("Diğer Kişi ve Kurumlar Raporu"))),
                  U("dava-takip", "Dava ve Takip",
                    G("03", "Dava, Tahkim ve Soruşturma", R("Dava Dosyaları"), R("Tahkim Dosyaları"), R("Soruşturma ve Ceza Dosyaları"), R("Karar, İstinaf ve Temyiz Takibi")),
                    G("04", "İcra, Haciz ve Tahsilat", R("İcra Dosyaları"), R("İtirazlar ve Bağlantılı Davalar"), R("Haciz ve Tedbir Dosyaları"), R("Tahsilat, Ödeme ve Protokoller"), R("İflas Kapsamındaki Alacaklar"), R("Dosya Hesabı ve Kapanış")),
                    G("07", "Çek ve Senet Takibi", R("Alınan ve Verilen Çek / Senetler"), R("Yazılan Çekler"), R("Çek ve Senet Hukuk Dosyaları"), R("Ödeme ve Dosya Kapanış Takibi")),
                    G("08", "Teminat ve Varlık İşlemleri", R("İpotek ve Rehinler"), R("Teminat, Kefalet ve Garantiler"), R("Süre, Fek ve İade Takibi"), R("İcradan Alım ve İhale Dosyaları"), R("Tapu, Tescil ve Teslim")),
                    G("10", "Arabuluculuk ve Sulh", R("Arabuluculuk Dosyaları"), R("Görüşmeler ve Anlaşmalar"), R("Protokol ve Yükümlülük Takibi"))),
                  U("sozlesme-uyum", "Sözleşme ve Uyum",
                    G("11", "Sözleşmeler", R("Sözleşme Listesi"), R("İnceleme, Onay ve İmza"), R("Süre, Yenileme ve Fesih"), R("Yükümlülükler ve Ek Protokoller")),
                    G("12", "Personel Hukuku", R("İşçilik Alacakları ve İşe İade"), R("İş Kazası ve Meslek Hastalığı"), R("Disiplin ve Fesih İşlemleri"), R("Dava, Arabuluculuk ve Ödemeler")),
                    G("13", "Gayrimenkul ve Kira", R("Kira ve Taşınmaz Dosyaları"), R("Kira Uyuşmazlıkları ve Tahliye")),
                    G("14", "Şirketler Hukuku", R("Yönetim Kurulu ve Genel Kurul"), R("Ticaret Sicili ve Ortaklık İşlemleri"), R("İmza Yetkileri ve Vekâletnameler")),
                    G("15", "Marka, Patent ve Fikrî Haklar", R("Başvuru ve Tesciller"), R("Yenileme ve Ücret Takibi"), R("İtiraz ve İhlal Dosyaları")),
                    G("16", "Mevzuat Uyumu ve İdari Süreçler", R("KVKK Süreçleri"), R("Denetimler, Cezalar ve İtirazlar"), R("Vergi, SGK ve Gümrük Dosyaları"), R("Ruhsat, İzin ve Uyum Takibi"))),
                  U("operasyon", "Operasyon",
                    G("17", "Tebligat, Süre ve Görev Takibi", R("İhtarnameler"), R("Tebligatlar ve Resmî Yazışmalar"), R("Duruşma ve Toplantı Takvimi"), R("Kritik Süreler ve Hatırlatmalar"), R("Görev ve İşlem Takibi")),
                    G("19", "Avukatlar ve Hukuk Giderleri", R("Avukat ve Büro Listesi"), R("Sözleşme, Ücret ve Masraflar"), R("Giderler ve Ödemeler"), R("Bütçe ve Maliyet Raporları"), R("Risk ve Karşılık Değerlendirmeleri")),
                    G("21", "Talepler ve Belgeler", R("Hukuk Talepleri"), R("Onay Bekleyen İşlemler"), R("Belge Listesi"), R("Eksik Belge ve İmza Takibi"))),
                  U("tanimlar", "Tanımlar ve Ayarlar",
                    G("23", "Tanımlar ve Ayarlar", R("Temel Tanımlar ve Eşleştirmeler"), R("Kullanıcı ve Erişim Yetkileri"), R("Onay ve Bildirim Ayarları"), R("Entegrasyon ve İşlem Kayıtları")))),

                M("yonetim", "Yönetim", "yonetim",
                  "Konsolide şirket görünümü ve üst yönetim analizleri. Operasyonel raporların kopyası değil, özet/kısayol mantığıyla çalışır.",
                  Kpi("Ciro", "Karlılık", "Gider", "Kritik Aksiyon"),
                  U("yonetim-ozeti", "Yönetim Özeti",
                    G("sirket-grup-performansi", "Şirket ve Grup Performansı", R("Şirket Ciro Raporu"), R("Al Sat Şirketleri Satış Analizi"), R("Üretim Şirketleri Kar Analizi"), R("Konsolide Gider Raporu"), K("Hukuk Yönetici Takip Raporu", "#hukuk/genel/02"))),
                  U("ozel-analizler", "Özel Analizler",
                    G("ozel-raporlar", "Özel Raporlar", R("Tahsilat Analiz Raporu", "Rapor35"), R("Genel Analiz Raporu", "GenelAnaliz", "Index"), R("Çalan İşlem Raporu")))),

                M("takvim", "Takvim", "takvim",
                  "Şirket ve departman takvimleri, toplantılar, görevler ve hatırlatmalar. Ekranlar yeni geliştirilecek.",
                  Kpi("Bugünkü Toplantı", "Bu Hafta Görev", "Geciken Aksiyon", "Yaklaşan Hatırlatma"),
                  G("ortak-takvim", "Ortak Takvim"),
                  G("toplantilar", "Toplantılar"),
                  G("gorevler-hatirlatmalar", "Görevler ve Hatırlatmalar", R("Takipteki Görevler", "Home", "Index"))),   // tabloda: index.html#gorev (ana sayfa kartı)

                M("ayarlar", "Ayarlar", "ayar",
                  "Kullanıcı, profil ve sistem ayarları.",
                  Kpi("Kullanıcı", "Rol", "Onay Akışı", "Aktif Entegrasyon"),
                  U("kullanici-profil", "Kullanıcı ve Profil",
                    G("profil-yonetimi", "Profil Yönetimi", R("Profilim"))),
                  // Bu grup Grup ERP yerleşim tablosunda YOK: bu portala özgü ekranlar (yetki, eski rapor paneli) ve
                  // tabloda yeri belirlenmemiş mevcut ekranlar burada toplanır; hiçbir ekran kaybolmaz.
                  U("sistem", "Sistem",
                    G("yetki-yonetimi", "Yetki Yönetimi", R("Sistem Yetki Yönetimi")),
                    G("rapor-paneli", "Rapor Paneli", R("Rapor Paneli (tüm ekranlar, modül bazında)", "Home", "RaporPaneli")),
                    G("degisiklik-gunlugu", "Değişiklik Günlüğü", R("Portal Değişiklik Günlüğü (yeni eklenenler ve son değişiklikler)", "Home", "Degisiklikler")),   // Data/PortalDegisiklikler.cs
                    G("eslenmemis", "Menüde Yeri Belirlenmemiş Ekranlar")))
            };

            // Menüde yeri belirlenmemiş mevcut ekranlar (Uras Üretim Portalı ekranları da dahil; yukarıda yerleştirilmeyen kalırsa burada görünür).
            var kullanilan = new HashSet<string>(liste.Where(m => m.ModulMu).SelectMany(TumRaporlar).Where(r => r.Hazir).Select(r => (r.Controller + "/" + r.Action).ToLowerInvariant()));
            var eslenmemis = liste.First(m => m.Key == "ayarlar").Alt.First(a => a.Key == "sistem").Alt.First(g => g.Key == "eslenmemis");
            foreach (var m in OrnekVeri.Menuler.Where(m => !kullanilan.Contains((m.ControllerName + "/" + m.ActionName).ToLowerInvariant())))
                eslenmemis.Raporlar.Add(new KmRapor { Ad = m.MenuTitle, Controller = m.ControllerName, Action = m.ActionName });

            return liste;
        }

        public static IEnumerable<KmRapor> TumRaporlar(KmModul m) => (m.Alt ?? new List<KmGrup>()).SelectMany(TumRaporlar);
        public static IEnumerable<KmRapor> TumRaporlar(KmGrup g) => g.Raporlar ?? (g.Alt ?? new List<KmGrup>()).SelectMany(TumRaporlar);
    }
}
