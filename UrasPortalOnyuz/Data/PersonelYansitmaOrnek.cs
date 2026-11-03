// ============================================================
// PERSONEL MALİYET YANSITMASI — İK › İnsan Kaynakları › Personel Maliyet ve Bordro
// Kural (İK notları, 25.09.2026):
//   1. Personel cari kartında SGK İşvereni ile Fiili Görev Yeri ayrı alanlardır; farklıysa SGK işvereni şirket, çalışanın maliyetini
//      fiili görev yeri şirkete YANSITMA FATURASI ile aktarır.
//   2. Yansıtılacak kalemler Gider / Gelir Kataloğu › 13 PERSONEL VE İŞÇİLİK GİDERLERİ (GDR.13.AA.KK) kalemleridir.
//   3. Her gider kalemi bire bir yansıtma gelir kalemiyle eşleşir: GDR.13.AA.KK ↔ YNS.13.AA.KK ("… Yansıtma Geliri").
//   4. Bordro dönemi seçilince yansıtma gereken personel ve tutarlar OTOMATİK bulunur, kayıtlar toplu atılır.
//   5. Kayıt iki şirkete birden atılır: SGK işvereni şirkette yansıtma geliri (YNS) + hedef şirket carisi alacağı; fiili görev yeri
//      şirkette personel gideri (GDR) + kaynak şirket carisi borcu.
// Veri kaynağı: Data/PersonelMaliyetOrnek.cs (personel kartları ve aylık maliyet kalemleri). Kayıtlar bellek içidir; ERP / e-fatura bağlantısı yoktur.
// Ekran: Controllers/PersonelYansitmaController.cs + Views/PersonelYansitma/Index.cshtml + wwwroot/js/personel-yansitma.js
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Kaydedildi", "Faturalandı", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Gider / Gelir Kataloğu › 13 Personel ve İşçilik Giderleri kalemi ve bire bir yansıtma gelir karşılığı.</summary>
    public class PyKatalogKalem
    {
        public string GiderKodu { get; set; }        // GDR.13.AA.KK
        public int AltGrupNo { get; set; }
        public string AltGrup { get; set; }
        public int KalemNo { get; set; }
        public string Kalem { get; set; }
        public string YansitmaKodu => "YNS" + GiderKodu.Substring(3);
        public string YansitmaAltGrup => AltGrup.EndsWith(" Giderleri") ? AltGrup.Substring(0, AltGrup.Length - 10) + " Yansıtma Gelirleri" : AltGrup + " Yansıtma Gelirleri";
        public string YansitmaKalem => (Kalem.EndsWith(" Giderleri") ? Kalem.Substring(0, Kalem.Length - 10) : Kalem.EndsWith(" Gideri") ? Kalem.Substring(0, Kalem.Length - 7) : Kalem) + " Yansıtma Geliri";
        public const string AnaGrup = "PERSONEL VE İŞÇİLİK GİDERLERİ";
        public const string YansitmaAnaGrup = "PERSONEL VE İŞÇİLİK YANSITMA GELİRLERİ";
    }

    /// <summary>Personel maliyet tablosu alanı → katalog gider kodu eşlemesi (yansıtma kodu otomatik).</summary>
    public class PyEsleme
    {
        public string Alan { get; set; }              // PmPersonel alan adı (XNet, Yemek …)
        public string AlanAdi { get; set; }
        public string AktarimTipi { get; set; } = ""; // X / R (yalnız net ücret satırlarında)
        public string GiderKodu { get; set; }
    }

    /// <summary>Yansıtma kaydındaki tek kalem (hedef payıyla çarpılmış tutar).</summary>
    public class PyKalem
    {
        public string Alan { get; set; }
        public string AlanAdi { get; set; }
        public string AktarimTipi { get; set; }
        public string GiderKodu { get; set; }
        public string GiderKalem { get; set; }
        public string YansitmaKodu { get; set; }
        public string YansitmaKalem { get; set; }
        public decimal Tutar { get; set; }
    }

    /// <summary>Bir personel için bir dönemde bir hedef şirkete yapılan yansıtma; iki şirkete de kaydı vardır.</summary>
    public class PyKayit
    {
        public int Id { get; set; }
        public string No { get; set; }                // PYN-2026-09-0001
        public string Donem { get; set; }             // yyyy-MM
        public int PersonelSira { get; set; }
        public string CariKodu { get; set; }
        public string AdSoyad { get; set; }
        public string Gorev { get; set; }
        public string KaynakSirket { get; set; }      // SGK işvereni (yansıtan)
        public string HedefSirket { get; set; }       // fiili görev yeri (yansıtılan)
        public string FiiliGorevYeri { get; set; }    // kayıt anındaki fiili görev yeri (kart değeri; HedefSirket bunun hukuki şirketidir)
        public string OdemeSirketi { get; set; } public string OdemeSirketiX { get; set; }      // kayıt anındaki ödeme şirketleri (R: Uras Power · X: fiili görev yeri şirketi) — Personel Muhasebe İşlemleri'nde her sekmede sütun
        public decimal Pay { get; set; }              // % (çok şirketli görev yerinde dağılım payı)
        public List<PyKalem> Kalemler { get; set; } = new List<PyKalem>();
        public decimal Net => Kalemler.Sum(k => k.Tutar);
        public decimal KdvOrani { get; set; }
        public decimal Kdv => Math.Round(Net * KdvOrani / 100m, 2);
        public decimal Toplam => Net + Kdv;
        public string Durum { get; set; }             // Kaydedildi | Faturalandı | İptal
        public string KaynakFisNo { get; set; }       // SGK işvereni şirketteki yansıtma gelir fişi
        public string HedefFisNo { get; set; }        // fiili görev yeri şirketteki gider fişi
        public string FaturaNo { get; set; }
        public string Olusturan { get; set; }
        public DateTime Zaman { get; set; }
        public string IptalNedeni { get; set; }
        public List<string> Gecmis { get; set; } = new List<string>();
    }

    /// <summary>Hazırlık ekranındaki aday satır: personel + hedef dağılımı + kalemler (kayıt öncesi hesap).</summary>
    public class PyAday
    {
        public PmPersonel Personel { get; set; }
        public string KaynakSirket { get; set; }
        public List<(string Sirket, decimal Pay)> Dagilim { get; set; }      // fiili görev yerinin şirket dağılımı (kaynak dahil)
        public List<(string Sirket, decimal Pay)> Hedefler => Dagilim.Where(d => d.Sirket != KaynakSirket && d.Pay > 0).ToList();
        public List<PyKalem> Kalemler { get; set; }                          // %100 tutarlar
        public decimal Maliyet => Kalemler.Sum(k => k.Tutar);
        public decimal YansitilacakPay => Hedefler.Sum(h => h.Pay);
        /// <summary>Bir hedefin net tutarı: kalemler tek tek payla çarpılıp yuvarlanır (kayıttaki hesapla birebir aynı yol).</summary>
        public decimal HedefNet(decimal pay) => Kalemler.Sum(k => PersonelYansitmaOrnek.KalemPay(k.Tutar, pay));
        public decimal Yansitilacak => Hedefler.Sum(h => HedefNet(h.Pay));
        public bool DagilimOzel { get; set; }
    }

    public static class PersonelYansitmaOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        // ---------- Katalog: 13 Personel ve İşçilik Giderleri (Gider_Gelir_Katalog_GIDER_20260925 › GİDER) ----------
        private static readonly string[] AltGruplar = { "", "Brüt Ek Çalışma Ücretleri", "Brüt Sosyal Yardımlar", "Brüt Ücret", "İşveren Yasal Payları", "Personel Kesintileri", "Prim ve Bonus", "Yan Haklar", "Yasal Brüt Ödemeler", "Direkt İşçilik Giderleri" };
        private static readonly (int Alt, int No, string Kalem)[] KalemTanimlari =
        {
            (1, 1, "Bayram Çalışması"), (1, 2, "Fazla Mesai Ücreti"), (1, 3, "Gece Vardiya Farkı"), (1, 4, "Hafta Sonu Çalışması"), (1, 5, "Resmî Tatil Çalışması"),
            (2, 1, "Aile Yardımı"), (2, 2, "Çocuk Yardımı"), (2, 3, "Çocuk Yardımı (0-6 Yaş)"), (2, 4, "Eğitim Yardımı"), (2, 5, "Mesai Yemek Gideri"), (2, 6, "Münferit Personel Ulaşım Giderleri"), (2, 7, "Münferit Personel Yemek Giderleri"),
            (2, 8, "Personel Kira Desteği (Brüt Dışı)"), (2, 9, "Personel Servis Gideri"), (2, 10, "Toplu Personel Yemek Hizmeti"), (2, 11, "Ulaşım Kartı Gideri"), (2, 12, "Yakıt Desteği"), (2, 13, "Yemek Kartı Gideri"),
            (3, 1, "Bordro Kaynaklı Damga Vergisi"), (3, 2, "Bordro Kaynaklı Gelir Vergisi"), (3, 3, "Genel Sağlık Sigortası İşçi Payı"), (3, 4, "İşsizlik Sigortası İşçi Payı (%1)"), (3, 5, "Maaş Farkı Ödemesi"), (3, 6, "Malullük, Yaşlılık ve Ölüm Sigortası İşçi Payı"),
            (3, 7, "Net Ücret"), (3, 8, "Normal Çalışma Ücreti"), (3, 9, "Sosyal Güvenlik Destek Primi (SGDP) İşçi Payı"), (3, 10, "Ücretli İzin Ücreti"), (3, 11, "Toplam Brüt Ücret"), (3, 12, "Brüt Ücrete Eklenen Kira Desteği"), (3, 14, "Brüt Ücrete Eklenen Yemek Yardımı"), (3, 15, "Brüt Ücrete Eklenen Yol Yardımı"),
            (4, 1, "Genel Sağlık Sigortası İşveren Payı"), (4, 2, "İşsizlik Sigortası İşveren Payı"), (4, 3, "Kısa Vadeli Sigorta Kolları Primi"), (4, 4, "Malullük, Yaşlılık ve Ölüm Sigortası İşveren Payı"), (4, 5, "Sosyal Güvenlik Destek Primi (SGDP) İşveren Payı"), (4, 6, "SGK İşveren Payı"),
            (5, 1, "Avans Kesintisi"), (5, 2, "Bireysel Emeklilik Sistemi (BES) Kesintisi"), (5, 3, "İcra Kesintisi"), (5, 4, "İhbar Kesintisi"), (5, 5, "Özel Sigorta Kesintisi"), (5, 6, "Personel Borç Kesintisi"), (5, 7, "Personel Yakıt Kesintisi"), (5, 8, "Sendika Kesintisi"),
            (6, 1, "Personel Prim Ödemesi"), (6, 2, "Satış Primi"), (6, 3, "Yönetim Onaylı Özel Ödemeler"), (6, 4, "Kasa Tazminatı"),
            (7, 1, "Bireysel Emeklilik Sistemi (BES) Katkısı"), (7, 2, "Doğum Yardımları"), (7, 3, "Evlilik Yardımları"), (7, 4, "Kıyafet ve Üniforma"), (7, 5, "Koruyucu Ekipman"), (7, 6, "Özel Sağlık Sigortası"),
            (8, 1, "Doğum İzni Farkı"), (8, 2, "İhbar Tazminatı"), (8, 3, "İşe İade Tazminatı"), (8, 4, "Kıdem Tazminatı"), (8, 5, "Kullanılmayan Yıllık İzin Ücretleri"), (8, 6, "Rapor Farkı"),
            (9, 1, "Üretim Personeli / Gündelikçi")
        };
        public static readonly List<PyKatalogKalem> Katalog = KalemTanimlari.Select(k => new PyKatalogKalem { GiderKodu = $"GDR.13.{k.Alt:00}.{k.No:00}", AltGrupNo = k.Alt, AltGrup = AltGruplar[k.Alt], KalemNo = k.No, Kalem = k.Kalem }).ToList();
        public static PyKatalogKalem KatalogBul(string kod) => Katalog.FirstOrDefault(k => string.Equals(k.GiderKodu, kod, StringComparison.OrdinalIgnoreCase));

        // ---------- Alan → kalem eşlemesi (varsayılan; Kalem Eşleme sekmesinden değiştirilir) ----------
        public static readonly List<PyEsleme> Esleme = new List<PyEsleme>
        {
            new PyEsleme { Alan = "XNet", AlanAdi = "X-Net Ücret", AktarimTipi = "X", GiderKodu = "GDR.13.03.07" },
            new PyEsleme { Alan = "RNet", AlanAdi = "R-Net Ücret", AktarimTipi = "R", GiderKodu = "GDR.13.03.07" },
            new PyEsleme { Alan = "ToplamBrutMasraflar", AlanAdi = "Toplam Brüt Masraflar (Vergi ve SGK İşçi Payları)", GiderKodu = "GDR.13.03.02" },
            new PyEsleme { Alan = "SgkIsverenToplam", AlanAdi = "SGK İşveren Toplam Prim", GiderKodu = "GDR.13.04.06" },
            new PyEsleme { Alan = "ToplamEkCalisma", AlanAdi = "Toplam Ek Çalışma", GiderKodu = "GDR.13.01.02" },
            new PyEsleme { Alan = "Yemek", AlanAdi = "Yemek", GiderKodu = "GDR.13.02.13" },
            new PyEsleme { Alan = "Yol", AlanAdi = "Yol", GiderKodu = "GDR.13.02.11" },
            new PyEsleme { Alan = "Servis", AlanAdi = "Servis", GiderKodu = "GDR.13.02.09" },
            new PyEsleme { Alan = "KiraDestegi", AlanAdi = "Kira Desteği", GiderKodu = "GDR.13.02.08" },
            new PyEsleme { Alan = "Egitim", AlanAdi = "Eğitim", GiderKodu = "GDR.13.02.04" },
            new PyEsleme { Alan = "CocukYardimi", AlanAdi = "Çocuk Yardımı", GiderKodu = "GDR.13.02.02" },
            new PyEsleme { Alan = "MaasFarki", AlanAdi = "Maaş Farkı", GiderKodu = "GDR.13.03.05" },
            new PyEsleme { Alan = "Prim", AlanAdi = "Prim", GiderKodu = "GDR.13.06.02" },
            new PyEsleme { Alan = "OzelOdeme", AlanAdi = "Özel Ödeme", GiderKodu = "GDR.13.06.03" },
            new PyEsleme { Alan = "KasaTazminati", AlanAdi = "Kasa Tazminatı", GiderKodu = "GDR.13.06.04" },
            new PyEsleme { Alan = "ToplamYanHaklar", AlanAdi = "Toplam Yan Haklar", GiderKodu = "GDR.13.07.06" },
            new PyEsleme { Alan = "ToplamYasal", AlanAdi = "Toplam Yasal Ödemeler", GiderKodu = "GDR.13.08.04" }
        };
        public static string EslemeKaydet(string alan, string giderKodu)
        {
            var e = Esleme.FirstOrDefault(x => x.Alan == alan); if (e == null) return "Alan bulunamadı.";
            var k = KatalogBul(giderKodu); if (k == null) return "Gider kodu katalogda yok (yalnız GDR.13.* kalemleri).";
            lock (Kilit) e.GiderKodu = k.GiderKodu;
            return null;
        }

        // ---------- Parametreler ----------
        /// <summary>Yansıtma faturası KDV oranı (%). Personel maliyet yansıtmasında varsayılan 0; Mali İşler kararına göre değiştirilir.</summary>
        public static decimal KdvOrani { get; private set; } = 0m;
        public static string KdvKaydet(decimal oran) { if (oran < 0 || oran > 50) return "KDV oranı 0–50 arasında olmalıdır."; lock (Kilit) KdvOrani = oran; return null; }

        // ---------- Görev yeri → şirket ----------
        /// <summary>Fiili görev yeri adını (lokasyon) hukuki şirket adına çevirir; "+" ile ayrılmış çok şirketli görev yerleri eşit pay alır.</summary>
        private static readonly Dictionary<string, string> GorevYeriSirketi = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Alv Kimya Çorlu"] = "ALV Kimya", ["ALV Kimya İst."] = "ALV Kimya", ["Alv Kimya İst."] = "ALV Kimya", ["Alv Kimya"] = "ALV Kimya",
            ["Urs Makine"] = "URS Makine", ["URS Makina"] = "URS Makine", ["Uras Kimya"] = "Uras Kimya", ["Selvi Kimya"] = "Selvi Kimya", ["Selvi"] = "Selvi Kimya",
            ["Asia"] = "Asia", ["Uras Holding"] = "Uras Holding", ["Uras Baskı"] = "Uras Baskı", ["Uras Power"] = "Uras Power", ["DRN"] = "DRN", ["Alv Filo"] = "Alv Filo", ["Avrupa Paper"] = "Avrupa Paper"
        };
        public static string SirketAdi(string ad) { ad = (ad ?? "").Trim(); return GorevYeriSirketi.TryGetValue(ad, out var s) ? s : ad; }
        /// <summary>Yurt dışı / lokasyon adları (Asia) tüzel şirket değildir: SGK işvereninin kendi görev yeri sayılır, yansıtma hedefi olmaz (Personel Maliyet Raporu › Muhasebe Kayıtları simülasyonuyla aynı kural; kullanıcı: Asia = İstanbul).</summary>
        private static bool LokasyonMu(string ad) => string.Equals(ad, "Asia", StringComparison.OrdinalIgnoreCase);
        public static List<(string Sirket, decimal Pay)> VarsayilanDagilim(string gorevYeri)
        {
            var parcalar = (gorevYeri ?? "").Split('+').Select(x => SirketAdi(x)).Where(x => x.Length > 0 && !LokasyonMu(x)).Distinct().ToList();
            if (parcalar.Count == 0) return new List<(string, decimal)>();
            decimal pay = Math.Round(100m / parcalar.Count, 2);
            var l = parcalar.Select(p => (p, pay)).ToList();
            l[l.Count - 1] = (l[l.Count - 1].p, 100m - pay * (parcalar.Count - 1));   // yuvarlama farkı son paya
            return l;
        }
        /// <summary>Personel bazında elle düzenlenmiş dağılım (sira → şirket, pay).</summary>
        private static readonly Dictionary<int, List<(string Sirket, decimal Pay)>> OzelDagilim = new Dictionary<int, List<(string, decimal)>>();
        public static string DagilimKaydet(int sira, List<(string Sirket, decimal Pay)> dagilim)
        {
            PmPersonel p; lock (PersonelMaliyetOrnek.Kilit) p = PersonelMaliyetOrnek.Liste.FirstOrDefault(x => x.Sira == sira);
            if (p == null) return "Personel bulunamadı.";
            if (dagilim == null || dagilim.Count == 0) { lock (Kilit) OzelDagilim.Remove(sira); return null; }   // boş → varsayılana dön
            var temiz = dagilim.Where(d => !string.IsNullOrWhiteSpace(d.Sirket)).Select(d => (SirketAdi(d.Sirket), Math.Round(d.Pay, 2))).ToList();
            if (temiz.Any(d => d.Item2 < 0 || d.Item2 > 100)) return "Paylar 0–100 arasında olmalıdır.";
            if (Math.Abs(temiz.Sum(d => d.Item2) - 100m) > 0.01m) return $"Payların toplamı %100 olmalıdır (şu an %{temiz.Sum(d => d.Item2).ToString("0.##", Tr)}).";
            if (temiz.Select(d => d.Item1).Distinct().Count() != temiz.Count) return "Aynı şirket birden fazla kez girilemez.";
            string kaynak = SirketAdi(p.SgkIsvereni);
            if (!temiz.Any(d => d.Item1 != kaynak && d.Item2 > 0)) return $"En az bir hedef şirkete (SGK işvereni {kaynak} dışında) %0'dan büyük pay verilmelidir; personel yansıtılmayacaksa kartındaki fiili görev yerini düzeltin.";
            // kuruş yuvarlama farkı (ör. 33,33 × 3) son paya yüklenir; toplam tam %100 olur
            decimal fark = 100m - temiz.Sum(d => d.Item2); if (fark != 0) temiz[temiz.Count - 1] = (temiz[temiz.Count - 1].Item1, temiz[temiz.Count - 1].Item2 + fark);
            lock (Kilit) OzelDagilim[sira] = temiz.Select(d => (d.Item1, d.Item2)).ToList();
            return null;
        }
        /// <summary>Kalem tutarının hedef payı (kuruşa yuvarlı) — önizleme ve kayıt aynı yolu kullanır.</summary>
        public static decimal KalemPay(decimal tutar, decimal pay) => Math.Round(tutar * pay / 100m, 2);
        /// <summary>Personel listesinin anlık kopyası (Personel Maliyet kilidi altında alınır; Ekle / Sil ile yarışmaz).</summary>
        private static List<PmPersonel> PersonelKopyasi() { lock (PersonelMaliyetOrnek.Kilit) return PersonelMaliyetOrnek.Liste.ToList(); }
        public static string[] Sirketler => PersonelKopyasi().SelectMany(p => VarsayilanDagilim(p.FiiliGorevYeri).Select(d => d.Sirket).Concat(new[] { SirketAdi(p.SgkIsvereni) })).Distinct().OrderBy(x => x, StringComparer.Create(Tr, true)).ToArray();

        // ---------- Adaylar (dönem bazında otomatik tespit) ----------
        /// <summary>Personel maliyet tablosundaki alanın tutarı (girilen alanlar PmPersonel'den, hesaplananlar bordro hesabından).</summary>
        private static decimal AlanDegeri(PmPersonel p, PmBordro h, string alan) => alan switch
        {
            "XNet" => p.XNet, "RNet" => p.RNet, "ToplamBrutMasraflar" => h.ToplamBrutMasraflar, "SgkIsverenToplam" => h.SgkIsverenToplam, "ToplamEkCalisma" => h.ToplamEkCalisma,
            "Yemek" => p.Yemek, "Yol" => p.Yol, "Servis" => p.Servis, "KiraDestegi" => p.KiraDestegi, "Egitim" => p.Egitim, "CocukYardimi" => p.CocukYardimi,
            "MaasFarki" => p.MaasFarki, "Prim" => p.Prim, "OzelOdeme" => p.OzelOdeme, "KasaTazminati" => p.KasaTazminati, "ToplamYanHaklar" => h.ToplamYanHaklar, "ToplamYasal" => h.ToplamYasal, _ => 0m
        };
        private static List<PyKalem> KalemleriKur(PmPersonel p)
        {
            var l = new List<PyKalem>(); var h = p.Hesap;
            foreach (var e in Esleme)
            {
                decimal t = AlanDegeri(p, h, e.Alan); if (t <= 0) continue;
                var k = KatalogBul(e.GiderKodu);
                l.Add(new PyKalem { Alan = e.Alan, AlanAdi = e.AlanAdi, AktarimTipi = e.AktarimTipi, GiderKodu = k?.GiderKodu ?? e.GiderKodu, GiderKalem = k?.Kalem ?? "", YansitmaKodu = k?.YansitmaKodu ?? "", YansitmaKalem = k?.YansitmaKalem ?? "", Tutar = t });
            }
            return l;
        }
        /// <summary>Aktif personel içinden SGK işvereni ≠ fiili görev yeri (ya da çok şirketli görev yeri) olanlar.</summary>
        public static List<PyAday> Adaylar()
        {
            var l = new List<PyAday>();
            foreach (var p in PersonelKopyasi().Where(x => x.Durum != "İşten Ayrıldı").OrderBy(x => x.Sira))
            {
                string kaynak = SirketAdi(p.SgkIsvereni);
                bool ozel = OzelDagilim.TryGetValue(p.Sira, out var d);
                var dagilim = ozel ? d : VarsayilanDagilim(p.FiiliGorevYeri);
                var aday = new PyAday { Personel = p, KaynakSirket = kaynak, Dagilim = dagilim, Kalemler = KalemleriKur(p), DagilimOzel = ozel };
                if (aday.Hedefler.Count == 0) continue;
                l.Add(aday);
            }
            return l;
        }

        // ---------- Kayıtlar ----------
        private static readonly List<PyKayit> _kayitlar = new List<PyKayit>();
        public static IReadOnlyList<PyKayit> Kayitlar => _kayitlar;
        private static int _sonId = 0, _sonFis = 0, _sonFatura = 0;
        public static string GuncelDonem => PersonelMaliyetOrnek.GuncelDonem;
        public static string DonemAdi(string donem) => DateTime.TryParseExact(donem + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t.ToString("MMMM yyyy", Tr) : donem;
        private static bool DonemGecerli(string donem) => DateTime.TryParseExact((donem ?? "") + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

        /// <summary>Seçilen personel için dönem kayıtlarını oluşturur (personel + hedef şirket + dönem bazında tekil; mevcut aktif kayıt varsa atlanır).</summary>
        public static (List<PyKayit> Olusan, int Atlanan, int Kalemsiz, string Hata) Olustur(string donem, List<int> siralar, string kullanici)
        {
            var olusan = new List<PyKayit>(); int atlanan = 0, kalemsiz = 0;
            if (!DonemGecerli(donem)) return (olusan, 0, 0, "Dönem geçersiz (yyyy-AA).");
            if (siralar == null || siralar.Count == 0) return (olusan, 0, 0, "Yansıtılacak personel seçilmedi.");
            lock (Kilit)
            {
                var adaylar = Adaylar().Where(a => siralar.Contains(a.Personel.Sira)).ToList();
                if (adaylar.Count == 0) return (olusan, 0, 0, "Seçilen personelde yansıtma gerektiren kayıt yok.");
                foreach (var a in adaylar)
                {
                    if (a.Kalemler.Count == 0) { kalemsiz++; continue; }   // tutarları 0 olan personel: kayıt açılmaz, ayrı sayılır
                    foreach (var h in a.Hedefler)
                    {
                        if (_kayitlar.Any(k => k.Donem == donem && k.PersonelSira == a.Personel.Sira && k.HedefSirket == h.Sirket && k.Durum != "İptal")) { atlanan++; continue; }
                        var t = DateTime.Now; int id = ++_sonId; int fis = ++_sonFis;
                        var k = new PyKayit
                        {
                            Id = id, No = $"PYN-{donem}-{id:0000}", Donem = donem, PersonelSira = a.Personel.Sira, CariKodu = a.Personel.CariKodu, AdSoyad = a.Personel.AdSoyad, Gorev = a.Personel.Gorev, FiiliGorevYeri = a.Personel.FiiliGorevYeri, OdemeSirketi = PersonelMuhasebeOrnek.OdemeSirketiR, OdemeSirketiX = PersonelMuhasebeOrnek.OdemeSirketiX(a.Personel),
                            KaynakSirket = a.KaynakSirket, HedefSirket = h.Sirket, Pay = h.Pay, KdvOrani = KdvOrani, Durum = "Kaydedildi", Olusturan = kullanici, Zaman = t,
                            KaynakFisNo = $"YGF-{donem.Replace("-", "")}-{fis:0000}", HedefFisNo = $"PGF-{donem.Replace("-", "")}-{fis:0000}",
                            Kalemler = a.Kalemler.Select(x => new PyKalem { Alan = x.Alan, AlanAdi = x.AlanAdi, AktarimTipi = x.AktarimTipi, GiderKodu = x.GiderKodu, GiderKalem = x.GiderKalem, YansitmaKodu = x.YansitmaKodu, YansitmaKalem = x.YansitmaKalem, Tutar = KalemPay(x.Tutar, h.Pay) }).ToList()
                        };
                        k.Gecmis.Add($"{t:dd.MM.yyyy HH:mm} · {kullanici} · Kayıt oluşturuldu: {k.KaynakSirket} → {k.HedefSirket} (%{h.Pay.ToString("0.##", Tr)}) · kaynak fişi {k.KaynakFisNo}, hedef fişi {k.HedefFisNo}");
                        _kayitlar.Add(k); olusan.Add(k);
                    }
                }
            }
            return (olusan, atlanan, kalemsiz, null);
        }

        /// <summary>Yansıtma faturasını (e-fatura) oluşturur; idempotent.</summary>
        public static string FaturaOlustur(string no, string kullanici)
        {
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.No == no); if (k == null) return "Kayıt bulunamadı.";
                if (k.Durum == "İptal") return "İptal edilmiş kayıt için fatura oluşturulamaz.";
                if (k.Durum == "Faturalandı") return null;
                k.FaturaNo = $"URS{k.Donem.Replace("-", "")}Y{++_sonFatura:0000}"; k.Durum = "Faturalandı";
                k.Gecmis.Add($"{DateTime.Now:dd.MM.yyyy HH:mm} · {kullanici} · Yansıtma faturası oluşturuldu: {k.FaturaNo} ({k.KaynakSirket} → {k.HedefSirket}, {k.Toplam.ToString("N2", Tr)} TL)");
            }
            return null;
        }

        /// <summary>Kaydı iptal eder (iki şirketteki fişler ters kayıtla kapanır); faturalıysa iptal faturası düzenlenir.</summary>
        public static string Iptal(string no, string neden, string kullanici)
        {
            if (string.IsNullOrWhiteSpace(neden)) return "İptal nedeni zorunludur.";
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.No == no); if (k == null) return "Kayıt bulunamadı.";
                if (k.Durum == "İptal") return "Kayıt zaten iptal.";
                k.IptalNedeni = neden.Trim(); string eski = k.Durum; k.Durum = "İptal";
                k.Gecmis.Add($"{DateTime.Now:dd.MM.yyyy HH:mm} · {kullanici} · İptal ({eski}): {k.IptalNedeni}. {k.KaynakFisNo} ve {k.HedefFisNo} ters kayıtla kapatıldı{(string.IsNullOrEmpty(k.FaturaNo) ? "" : ", " + k.FaturaNo + " için iptal faturası düzenlendi")}.");
            }
            return null;
        }

        // ---------- Paket / DTO — Personel Muhasebe İşlemleri (Controllers/PersonelMuhasebeController.cs) ve eski Personel Maliyet Yansıtması uçları ortak kullanır ----------
        /// <summary>Dönemin adayları (otomatik tespit), tüm kayıtlar, eşleme, katalog ve parametreler. Çağıran Kilit altında çağırır; listeler kilit içinde maddeleştirilir.</summary>
        public static object Paket(string donem, string ben, bool ok, string message, List<string> yeniler = null)
        {
            var kayitlar = Kayitlar.ToList();
            var adaylar = Adaylar();
            return new
            {
                success = ok, message, yeniler, donem, donemAdi = DonemAdi(donem), guncelDonem = GuncelDonem, ben,
                kdvOrani = KdvOrani, sirketler = Sirketler,
                donemler = kayitlar.Select(k => k.Donem).Concat(new[] { donem, GuncelDonem }).Distinct().OrderByDescending(x => x).Select(x => new { kod = x, ad = DonemAdi(x) }).ToList(),
                katalog = Katalog.Select(k => new { k.GiderKodu, k.AltGrupNo, k.AltGrup, k.KalemNo, k.Kalem, k.YansitmaKodu, k.YansitmaAltGrup, k.YansitmaKalem }).ToList(),
                esleme = Esleme.Select(e => { var k = KatalogBul(e.GiderKodu); return new { e.Alan, e.AlanAdi, e.AktarimTipi, e.GiderKodu, giderKalem = k?.Kalem, giderAltGrup = k?.AltGrup, yansitmaKodu = k?.YansitmaKodu, yansitmaKalem = k?.YansitmaKalem }; }).ToList(),
                adaylar = adaylar.Select(a => new
                {
                    sira = a.Personel.Sira, cariKodu = a.Personel.CariKodu, adSoyad = a.Personel.AdSoyad, unvan = a.Personel.Unvan, gorev = a.Personel.Gorev, bolum = a.Personel.Bolum, catiDepartman = a.Personel.CatiDepartman,
                    sgkIsvereni = a.Personel.SgkIsvereni, fiiliGorevYeri = a.Personel.FiiliGorevYeri, odemeSirketi = PersonelMuhasebeOrnek.OdemeSirketiR, odemeSirketiX = PersonelMuhasebeOrnek.OdemeSirketiX(a.Personel), kaynakSirket = a.KaynakSirket,
                    dagilim = a.Dagilim.Select(d => new { sirket = d.Sirket, pay = d.Pay }).ToList(), hedefler = a.Hedefler.Select(h => new { sirket = h.Sirket, pay = h.Pay, tutar = a.HedefNet(h.Pay), kayit = kayitlar.FirstOrDefault(k => k.Donem == donem && k.PersonelSira == a.Personel.Sira && k.HedefSirket == h.Sirket && k.Durum != "İptal")?.No }).ToList(),
                    dagilimOzel = a.DagilimOzel, maliyet = a.Maliyet, yansitilacakPay = a.YansitilacakPay, yansitilacak = a.Yansitilacak,
                    kalemler = a.Kalemler.Select(KalemDto).ToList(),
                    durum = a.Kalemler.Count == 0 ? "Kalem Yok" : a.Hedefler.All(h => kayitlar.Any(k => k.Donem == donem && k.PersonelSira == a.Personel.Sira && k.HedefSirket == h.Sirket && k.Durum != "İptal")) ? "Kaydedildi" : a.Hedefler.Any(h => kayitlar.Any(k => k.Donem == donem && k.PersonelSira == a.Personel.Sira && k.HedefSirket == h.Sirket && k.Durum != "İptal")) ? "Kısmen Kaydedildi" : "Hazır"
                }).ToList(),
                kayitlar = kayitlar.OrderByDescending(k => k.Id).Select(KayitDto).ToList()
            };
        }
        private static object KalemDto(PyKalem k) => new { k.Alan, k.AlanAdi, k.AktarimTipi, k.GiderKodu, k.GiderKalem, k.YansitmaKodu, k.YansitmaKalem, k.Tutar };
        /// <summary>Muhasebe fiş satırı (yansıtma kaydının iki taraflı fiş önizlemesi).</summary>
        public class FisSatir { public string Hesap { get; set; } public string Ad { get; set; } public decimal Borc { get; set; } public decimal Alacak { get; set; } public string Kod { get; set; } = ""; }
        /// <summary>SGK işvereni (kaynak) şirket fişi: hedef şirket carisi borç / yansıtma gelirleri (YNS) alacak (+ hesaplanan KDV).</summary>
        public static List<FisSatir> KaynakFis(PyKayit k)
        {
            var l = new List<FisSatir> { new FisSatir { Hesap = "120." + SirketKod(k.HedefSirket), Ad = k.HedefSirket + " Cari Hesabı", Borc = k.Toplam } };
            l.AddRange(k.Kalemler.Select(x => new FisSatir { Hesap = "649." + x.YansitmaKodu.Replace("YNS.", ""), Ad = x.YansitmaKalem, Alacak = x.Tutar, Kod = x.YansitmaKodu }));
            if (k.Kdv > 0) l.Add(new FisSatir { Hesap = "391.01", Ad = "Hesaplanan KDV", Alacak = k.Kdv });
            return l;
        }
        /// <summary>Fiili görev yeri (hedef) şirket fişi: personel giderleri (GDR) borç (+ indirilecek KDV) / kaynak şirket carisi alacak.</summary>
        public static List<FisSatir> HedefFis(PyKayit k)
        {
            var l = k.Kalemler.Select(x => new FisSatir { Hesap = "770." + x.GiderKodu.Replace("GDR.", ""), Ad = x.GiderKalem, Borc = x.Tutar, Kod = x.GiderKodu }).ToList();
            if (k.Kdv > 0) l.Add(new FisSatir { Hesap = "191.01", Ad = "İndirilecek KDV", Borc = k.Kdv });
            l.Add(new FisSatir { Hesap = "320." + SirketKod(k.KaynakSirket), Ad = k.KaynakSirket + " Cari Hesabı", Alacak = k.Toplam });
            return l;
        }
        private static object KayitDto(PyKayit k) => new
        {
            k.Id, k.No, k.Donem, donemAdi = DonemAdi(k.Donem), k.PersonelSira, k.CariKodu, k.AdSoyad, k.Gorev, k.KaynakSirket, k.HedefSirket, sgkIsvereni = k.KaynakSirket, k.FiiliGorevYeri, k.OdemeSirketi, k.OdemeSirketiX, k.Pay, k.Net, k.KdvOrani, k.Kdv, k.Toplam, k.Durum,
            k.KaynakFisNo, k.HedefFisNo, k.FaturaNo, k.Olusturan, zaman = k.Zaman.ToString("dd.MM.yyyy HH:mm"), k.IptalNedeni, k.Gecmis, kalemler = k.Kalemler.Select(KalemDto).ToList(),
            kaynakFis = KaynakFis(k), hedefFis = HedefFis(k)
        };
        private static string SirketKod(string s) => new string((s ?? "").ToUpper(Tr).Where(char.IsLetterOrDigit).Take(6).ToArray());
    }
}
