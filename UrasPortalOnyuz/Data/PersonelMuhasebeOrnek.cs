// ============================================================
// PERSONEL MALİYET RAPORU — "Muhasebe Kayıtları" sekmesi (IT Analiz 01.11.2026: Simülasyon · Onay · Muhasebeleştirme)
//   Akış zorunlu: VERİYİ HAZIRLA → SİMÜLASYON OLUŞTUR → KONTROL / ANALİZ → ONAYA GÖNDER → ONAYLA → MUHASEBE FİŞİ → İZLEME / TERS KAYIT. Onaysız simülasyondan gerçek fiş oluşmaz.
//   Kaynak: seçili dönemin Personel Maliyet Tablosu satırları (PmPersonel + PmBordro). Şirketler: SGK İşvereni (tahakkuk), Fiili Görev Yeri (yansıtma), Ödeme Şirketi (net ödeme).
//   İşlem grupları: A Bordro Tahakkuku (SGK işvereni) · B Fiili Görev Yeri Yansıtması (SGK ≠ fiili şirket; genel maliyet) · C Net Personel Ödemesi (ödeme şirketi = SGK) ·
//                   D Başka Şirket Tarafından Ödeme (SGK ≠ ödeme şirketi; iki şirkette karşılıklı dengeli set) · E Ters Kayıt (muhasebeleştirilmiş fiş referanslı).
//   Hesaplar personel satırına yazılmaz: Hesap Eşleştirme tablosundan (şirket + işlem türü + kaynak kalem + X/R + yön + karşı şirket) seçilir; eşleşme yoksa satır "[Eşleşme Yok]" hatası alır.
//   Her tüzel şirket için ayrı fiş; her simülasyon SIM no + versiyon alır; kaynak veri imzası (snapshot) saklanır, veri değişirse "Güncelliğini Yitirdi".
//   11 kontrol (fiş dengesi, şirket-hesap, aktif/posting, X/R mutabakatı, genel maliyet mutabakatı, yansıtma, ödeme şirketi, mükerrer, dönem, açıklama standardı, istisna notu);
//   kritik hata varsa onay kapalı. Muhasebeleştirme MuhasebeOrnek (bellek içi yevmiye fişleri) üzerinden; ters kayıt orijinal fişe referanslı.
// Ekran: Controllers/PersonelMaliyetController.cs (Paket: muhasebe; MuhasebeSimule / MuhasebeIslem / MuhasebeEsleme) + Views/PersonelMaliyet/Index.cshtml + wwwroot/js/personel-maliyet.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WebApplication3.Data
{
    /// <summary>Hesap eşleştirme satırı (muhasebe birimi tanımlar; sistem sonraki dönemlerde otomatik getirir). Sirket / KarsiSirket "*" = tüm şirketler; XR "*" = her ikisi.</summary>
    public class PmhEsleme
    {
        public int Id { get; set; }
        public string Sirket { get; set; } = "*"; public string KarsiSirket { get; set; } = "*";
        public string IslemTuru { get; set; } public string KaynakKalem { get; set; } public string XR { get; set; } = "*"; public string Yon { get; set; }   // Borç | Alacak
        public string HesapKodu { get; set; } public string HesapAdi { get; set; }
        public bool Varsayilan { get; set; } = true; public string GecerlilikBas { get; set; } public string GecerlilikBit { get; set; } public bool Aktif { get; set; } = true;
        public int Versiyon { get; set; } = 1; public string Degistiren { get; set; } public DateTime? Degisme { get; set; }
        public bool Posting => !string.IsNullOrEmpty(HesapKodu) && HesapKodu.Contains(".");   // alt (detay) hesap; üst seviye hesaba kayıt atılamaz
        public bool Gecerli(string donem) => Aktif && (string.IsNullOrEmpty(GecerlilikBas) || string.Compare(donem, GecerlilikBas, StringComparison.Ordinal) >= 0) && (string.IsNullOrEmpty(GecerlilikBit) || string.Compare(donem, GecerlilikBit, StringComparison.Ordinal) <= 0);
    }
    public class PmhSatir
    {
        public int Id { get; set; } public string FisKodu { get; set; } public string Sirket { get; set; } public string IslemGrubu { get; set; }
        public int PersonelSira { get; set; } public string AdSoyad { get; set; } public string IslemTuru { get; set; } public string XR { get; set; } public string KaynakKalem { get; set; }
        public string SgkIsvereni { get; set; } public string FiiliGorevYeri { get; set; } public string OdemeSirketi { get; set; }   // personelin üç şirket alanı: Personel Muhasebe İşlemleri'nde her personel tablosunda sütun + ortak filtre
        public string HesapKodu { get; set; } public string HesapAdi { get; set; } public decimal Borc { get; set; } public decimal Alacak { get; set; } public string KarsiSirket { get; set; } = "";
        public string Aciklama { get; set; } public int? EslemeId { get; set; } public List<string> Hatalar { get; set; } = new List<string>();
        public string Kontrol => Hatalar.Count == 0 ? "✓" : "✗";
    }
    public class PmhFis
    {
        public string Kod { get; set; } public string Sirket { get; set; } public string IslemGrubu { get; set; } public string Baslik { get; set; }
        public List<PmhSatir> Satirlar { get; set; } = new List<PmhSatir>();
        public decimal ToplamBorc => Math.Round(Satirlar.Sum(s => s.Borc), 2); public decimal ToplamAlacak => Math.Round(Satirlar.Sum(s => s.Alacak), 2);
        public bool Dengeli => Math.Abs(ToplamBorc - ToplamAlacak) < 0.01m; public decimal Fark => Math.Round(ToplamBorc - ToplamAlacak, 2);
        public int HataliSatir => Satirlar.Count(s => s.Hatalar.Count > 0);
        public string MuhasebeFisNo { get; set; } public DateTime? KayitTarihi { get; set; } public string TersFisNo { get; set; } public DateTime? TersTarihi { get; set; }
    }
    public class PmhKontrol { public int No { get; set; } public string Ad { get; set; } public string Seviye { get; set; } public bool Gecti { get; set; } public int Adet { get; set; } public string Mesaj { get; set; } }
    public class PmhAudit { public DateTime Tarih { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Detay { get; set; } }
    public class PmhSimulasyon
    {
        public int Id { get; set; } public string No { get; set; } public int Versiyon { get; set; } = 1; public string Donem { get; set; } public string Kapsam { get; set; } = "*";   // şirket kapsamı (* = tümü)
        public int PersonelSayisi { get; set; } public string Durum { get; set; }
        public string Olusturan { get; set; } public DateTime Olusturma { get; set; } public string OnayaGonderen { get; set; } public DateTime? OnayaGonderme { get; set; }
        public string Onaylayan { get; set; } public DateTime? OnayTarihi { get; set; } public string Muhasebelestiren { get; set; } public DateTime? MuhasebeTarihi { get; set; } public string TersKayitNedeni { get; set; }
        public string Imza { get; set; } public int EslemeVersiyonu { get; set; }
        public decimal GenelMaliyet { get; set; } public decimal AraToplam { get; set; } public decimal YansitmaGereken { get; set; } public decimal BaskaSirketOdeme { get; set; }
        public List<PmhFis> Fisler { get; set; } = new List<PmhFis>(); public List<PmhKontrol> Kontroller { get; set; } = new List<PmhKontrol>(); public List<PmhAudit> Audit { get; set; } = new List<PmhAudit>();
        public decimal ToplamBorc => Fisler.Sum(f => f.ToplamBorc); public decimal ToplamAlacak => Fisler.Sum(f => f.ToplamAlacak);
        public int DengesizFis => Fisler.Count(f => !f.Dengeli); public int HataliSatir => Fisler.Sum(f => f.HataliSatir);
        public bool KritikHata => Kontroller.Any(k => k.Seviye == "Kritik" && !k.Gecti);
        public bool Kilitli => Durum == "Onaylandı" || Durum == "Muhasebeleştirildi" || Durum == "Ters Kayıt Yapıldı";
        public bool Guncel => Imza == PersonelMuhasebeOrnek.KaynakImzasi(Donem, Kapsam);
    }

    public static class PersonelMuhasebeOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        public static readonly string[] IslemTurleri = { "Bordro Tahakkuku", "Fiili Görev Yeri Yansıtması", "Net Personel Ödemesi", "Başka Şirket Tarafından Ödeme", "Ters Kayıt" };
        public static readonly string[] Durumlar = { "Taslak", "Simülasyon Oluşturuldu", "Kontrol Gerekiyor", "Kontrole Hazır", "Onaya Gönderildi", "Onaylandı", "Muhasebeleştirildi", "Ters Kayıt Yapıldı", "Güncelliğini Yitirdi", "İptal" };
        /// <summary>Tüzel şirketler (SGK işvereni / ödeme şirketi seçenekleri).</summary>
        public static readonly string[] Sirketler = { "ALV Kimya", "Uras Kimya", "Selvi Kimya", "Uras Holding", "Avrupa Paper", "Alv Filo", "URS Makine", "Uras Power" };
        /// <summary>Hesap planı (ad): MuhasebeOrnek.HesapPlani + personel muhasebesi alt hesapları. Kodlar sabit değildir; eşleştirme tablosundan gelir.</summary>
        public static readonly Dictionary<string, string> Hesaplar = new Dictionary<string, string>
        {
            ["770.01"] = "Personel Ücret Giderleri", ["770.02"] = "Fazla Mesai Giderleri", ["770.03"] = "Sosyal Yardım Giderleri", ["770.04"] = "Prim ve Ek Kazanç Giderleri", ["770.05"] = "Yan Hak Giderleri", ["770.06"] = "Kıdem / İhbar / Yasal Ödeme Giderleri",
            ["770.07"] = "SGK İşveren Payı Giderleri", ["770.08"] = "Yansıtılan Personel Giderleri (Grup İçi)", ["770.09"] = "Bordro Dışı Personel Giderleri (X)", ["770.10"] = "Diğer Ek Ödeme Giderleri (R)",
            ["720.01"] = "Direkt İşçilik Ücret Giderleri", ["720.02"] = "Direkt İşçilik Fazla Mesai Giderleri",
            ["335.01"] = "Personele Borçlar (R · Banka)", ["335.02"] = "Personele Borçlar (X · Elden)", ["360.01"] = "Ödenecek Gelir Vergisi", ["360.02"] = "Ödenecek Damga Vergisi", ["361.01"] = "Ödenecek SGK İşçi Primi", ["361.02"] = "Ödenecek SGK İşveren Primi",
            ["196.01"] = "Personel Avansları (Maaştan Mahsup)", ["196.02"] = "Personel Avansları (X · Elden)", ["369.01"] = "Ödenecek İcra Kesintileri", ["369.02"] = "Ödenecek BES / Sendika / Özel Sigorta Kesintileri", ["136.01"] = "Personelden Alacaklar (Borç Kesintisi)",
            ["132.01"] = "İştiraklerden Alacaklar (Grup İçi Personel)", ["332.01"] = "İştiraklere Borçlar (Grup İçi Personel)", ["649.01"] = "Personel Gideri Yansıtma Gelirleri", ["102.01"] = "Bankalar — Maaş Hesabı", ["100.01"] = "Merkez Kasa (Elden Ödeme)"
        };
        public static string HesapAdi(string kod) => Hesaplar.TryGetValue(kod ?? "", out var a) ? a : (MuhasebeOrnek.HesapPlani.TryGetValue(kod ?? "", out var b) ? b : kod);
        public static IEnumerable<object> HesapListesi => Hesaplar.Select(h => new { kod = h.Key, ad = h.Value, ust = MuhasebeOrnek.HesapPlani.TryGetValue(h.Key.Split('.')[0], out var u) ? u : "" }).Concat(MuhasebeOrnek.HesapPlani.Select(h => new { kod = h.Key, ad = h.Value, ust = "" })).OrderBy(h => h.kod, StringComparer.Ordinal);

        // ---------- şirket eşlemesi ----------
        private static readonly Dictionary<string, string> FiiliMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["ALV Kimya İst."] = "ALV Kimya", ["ALV Kimya Çorlu"] = "ALV Kimya", ["ALV Kimya"] = "ALV Kimya", ["Uras Kimya"] = "Uras Kimya", ["Uras Holding"] = "Uras Holding", ["Selvi Kimya"] = "Selvi Kimya", ["Avrupa Paper"] = "Avrupa Paper", ["URS Makine"] = "URS Makine", ["Alv Filo"] = "Alv Filo", ["Uras Power"] = "Uras Power" };
        /// <summary>Fiili görev yerinin tüzel şirketi; yurt dışı / lokasyon adları (Asia vb.) SGK işvereninin şirketidir (yansıtma yok).</summary>
        public static string FiiliSirket(PmPersonel p) => FiiliMap.TryGetValue(p.FiiliGorevYeri ?? "", out var s) ? s : p.SgkIsvereni;
        /// <summary>R (banka) net ödemelerinin tamamı varsayılan olarak Uras Power'dan yapılır (kullanıcı kuralı); kartta kişi bazında değiştirilebilir. X (elden) ödemesi SGK işvereninin kasasından.</summary>
        public const string VarsayilanOdemeSirketi = "Uras Power";
        public static string OdemeSirketi(PmPersonel p) => string.IsNullOrWhiteSpace(p.OdemeSirketi) ? VarsayilanOdemeSirketi : p.OdemeSirketi.Trim();
        private static string MasterSirket(string s) => s == "ALV Kimya" || s == "Alv Filo" ? "Alv Kimya" : s == "Selvi Kimya" ? "Selvi" : s;
        public static string DbName(string sirket) => CekAkibetOrnek.DbName(MasterSirket(sirket));
        public static bool OdemeYapilmiyor(PmPersonel p) => (p.Not ?? "").IndexOf("Ödeme Yapılmıyor", StringComparison.OrdinalIgnoreCase) >= 0;

        // ---------- hesap eşleştirme tablosu ----------
        public static readonly List<PmhEsleme> Eslemeler = new List<PmhEsleme>();
        private static int _eslemeId, _eslemeVersiyon = 1;
        private static void E(string islem, string kalem, string xr, string yon, string kod, string sirket = "*", string karsi = "*") => Eslemeler.Add(new PmhEsleme { Id = ++_eslemeId, Sirket = sirket, KarsiSirket = karsi, IslemTuru = islem, KaynakKalem = kalem, XR = xr, Yon = yon, HesapKodu = kod, HesapAdi = HesapAdi(kod), GecerlilikBas = "2026-01", Degistiren = "Muhasebe (Nazlı Erdem)", Degisme = new DateTime(2026, 10, 15, 10, 0, 0) });
        static PersonelMuhasebeOrnek()
        {
            const string A = "Bordro Tahakkuku", B = "Fiili Görev Yeri Yansıtması", C = "Net Personel Ödemesi", D = "Başka Şirket Tarafından Ödeme";
            // A — borç (gider)
            E(A, "Ücret (Brüt Esas)", "R", "Borç", "770.01"); E(A, "Fazla Mesai", "R", "Borç", "770.02"); E(A, "Sosyal Yardımlar", "R", "Borç", "770.03"); E(A, "Prim ve Ek Kazançlar", "R", "Borç", "770.04"); E(A, "Yan Haklar", "R", "Borç", "770.05");
            E(A, "Yasal Ödemeler", "R", "Borç", "770.06"); E(A, "SGK İşveren Payı", "R", "Borç", "770.07"); E(A, "Diğer Ek Ödemeler (R)", "R", "Borç", "770.10"); E(A, "Bordro Dışı Ödeme (X)", "X", "Borç", "770.09");
            // A — alacak (yükümlülük)
            E(A, "Net-R Bordro", "R", "Alacak", "335.01"); E(A, "Net-X Bordro", "X", "Alacak", "335.02"); E(A, "Gelir Vergisi", "R", "Alacak", "360.01"); E(A, "Damga Vergisi", "R", "Alacak", "360.02"); E(A, "SGK İşçi Payı", "R", "Alacak", "361.01"); E(A, "SGK İşveren Payı", "R", "Alacak", "361.02");
            E(A, "Avans Kesintisi", "R", "Alacak", "196.01"); E(A, "İcra Kesintisi", "R", "Alacak", "369.01"); E(A, "Borç Kesintisi", "R", "Alacak", "136.01"); E(A, "Diğer Kesintiler", "R", "Alacak", "369.02"); E(A, "Kesinti (X)", "X", "Alacak", "196.02");
            // B — yansıtma
            E(B, "Grup İçi Alacak", "*", "Borç", "132.01"); E(B, "Yansıtma Geliri", "*", "Alacak", "649.01"); E(B, "Yansıtılan Personel Gideri", "*", "Borç", "770.08"); E(B, "Grup İçi Borç", "*", "Alacak", "332.01");
            // C — net ödeme
            E(C, "Net-R Bordro", "R", "Borç", "335.01"); E(C, "Banka", "R", "Alacak", "102.01"); E(C, "Net-X Bordro", "X", "Borç", "335.02"); E(C, "Kasa", "X", "Alacak", "100.01");
            // D — başka şirket tarafından ödeme
            E(D, "Net-R Bordro", "R", "Borç", "335.01"); E(D, "Net-X Bordro", "X", "Borç", "335.02"); E(D, "Grup İçi Borç", "*", "Alacak", "332.01"); E(D, "Grup İçi Alacak", "*", "Borç", "132.01"); E(D, "Banka", "R", "Alacak", "102.01"); E(D, "Kasa", "X", "Alacak", "100.01");
            // şirkete özel örnek: Avrupa Paper üretim şirketi — ücret ve mesai direkt işçilik hesabında
            E(A, "Ücret (Brüt Esas)", "R", "Borç", "720.01", "Avrupa Paper"); E(A, "Fazla Mesai", "R", "Borç", "720.02", "Avrupa Paper");
        }
        public static PmhEsleme EslemeBul(string sirket, string islem, string kalem, string xr, string yon, string donem, string karsi = null)
        {
            lock (Kilit) return Eslemeler.Where(e => e.IslemTuru == islem && e.KaynakKalem == kalem && e.Yon == yon && (e.XR == "*" || e.XR == xr) && (e.Sirket == "*" || e.Sirket == sirket) && (e.KarsiSirket == "*" || e.KarsiSirket == karsi) && e.Gecerli(donem))
                .OrderByDescending(e => e.Sirket != "*").ThenByDescending(e => e.KarsiSirket != "*").ThenByDescending(e => e.XR != "*").ThenByDescending(e => e.Varsayilan).FirstOrDefault();
        }
        public static string EslemeKaydet(int? id, string sirket, string karsi, string islem, string kalem, string xr, string yon, string kod, bool varsayilan, string gecBas, string gecBit, bool aktif, string kullanici, out PmhEsleme kayit)
        {
            kayit = null;
            if (!IslemTurleri.Contains(islem ?? "")) return "İşlem türü geçersiz.";
            if (string.IsNullOrWhiteSpace(kalem)) return "Kaynak kalem zorunludur.";
            if (yon != "Borç" && yon != "Alacak") return "Kayıt yönü Borç ya da Alacak olmalıdır.";
            if (string.IsNullOrWhiteSpace(kod)) return "Hesap kodu zorunludur.";
            kod = kod.Trim(); if (!Hesaplar.ContainsKey(kod) && !MuhasebeOrnek.HesapPlani.ContainsKey(kod)) return $"{kod} hesap planında yok (aktif hesap planından seçin).";
            if (!kod.Contains(".")) return $"{kod} üst seviye hesaptır; yalnız detay (posting) hesabına kayıt atılabilir.";
            sirket = string.IsNullOrWhiteSpace(sirket) ? "*" : sirket.Trim(); karsi = string.IsNullOrWhiteSpace(karsi) ? "*" : karsi.Trim(); xr = string.IsNullOrWhiteSpace(xr) ? "*" : xr.Trim();
            if (sirket != "*" && !Sirketler.Contains(sirket)) return "Şirket listede yok.";
            lock (Kilit)
            {
                var e = id.HasValue ? Eslemeler.FirstOrDefault(x => x.Id == id.Value) : null;
                if (id.HasValue && e == null) return "Eşleştirme bulunamadı.";
                if (e == null) { e = new PmhEsleme { Id = ++_eslemeId }; Eslemeler.Add(e); }
                e.Sirket = sirket; e.KarsiSirket = karsi; e.IslemTuru = islem; e.KaynakKalem = kalem.Trim(); e.XR = xr; e.Yon = yon; e.HesapKodu = kod; e.HesapAdi = HesapAdi(kod); e.Varsayilan = varsayilan; e.GecerlilikBas = string.IsNullOrWhiteSpace(gecBas) ? null : gecBas; e.GecerlilikBit = string.IsNullOrWhiteSpace(gecBit) ? null : gecBit; e.Aktif = aktif;
                e.Versiyon++; e.Degistiren = kullanici; e.Degisme = DateTime.Now; _eslemeVersiyon++;
                kayit = e;
            }
            return null;
        }
        public static object EslemeDto(PmhEsleme e) => new { e.Id, e.Sirket, e.KarsiSirket, e.IslemTuru, e.KaynakKalem, e.XR, e.Yon, e.HesapKodu, e.HesapAdi, e.Varsayilan, e.GecerlilikBas, e.GecerlilikBit, e.Aktif, e.Versiyon, e.Degistiren, degisme = e.Degisme?.ToString("dd.MM.yyyy HH:mm"), posting = e.Posting };

        // ---------- kaynak imzası (snapshot) ----------
        private static IEnumerable<PmPersonel> Kaynak(string donem, string kapsam) { var l = PersonelMaliyetOrnek.ListeDonem(donem) ?? PersonelMaliyetOrnek.Liste; return l.Where(p => p.Durum == "Aktif" && (kapsam == "*" || string.IsNullOrEmpty(kapsam) || p.SgkIsvereni == kapsam)).OrderBy(p => p.Sira); }
        public static string KaynakImzasi(string donem, string kapsam)
        {
            var sb = new StringBuilder();
            lock (PersonelMaliyetOrnek.Kilit) foreach (var p in Kaynak(donem, kapsam)) { var h = p.Hesap; sb.Append(p.Sira).Append('|').Append(h.GenelMaliyet).Append('|').Append(h.NetRBordro).Append('|').Append(h.NetXBordro).Append('|').Append(h.ToplamBrutKazanc).Append('|').Append(h.SgkIsverenToplam).Append('|').Append(p.SgkIsvereni).Append('|').Append(FiiliSirket(p)).Append('|').Append(OdemeSirketi(p)).Append('|').Append(OdemeYapilmiyor(p)).Append(';'); }
            lock (Kilit) sb.Append("#E").Append(_eslemeVersiyon);
            using var sha = SHA256.Create(); return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()))).Substring(0, 16);
        }

        // ---------- simülasyonlar ----------
        public static readonly List<PmhSimulasyon> Simulasyonlar = new List<PmhSimulasyon>();
        private static int _simId, _satirId;
        private static void Iz(PmhSimulasyon s, string kullanici, string islem, string detay) => s.Audit.Add(new PmhAudit { Tarih = DateTime.Now, Kullanici = kullanici, Islem = islem, Detay = detay });

        /// <summary>Simülasyon oluşturur: gerçek fiş atmaz; tüm fişleri ve satırları önizleme olarak üretir, kontrolleri çalıştırır. Aynı dönem + kapsam için önceki açık simülasyonun versiyonu artar.</summary>
        public static string Olustur(string donem, string kapsam, string kullanici, out PmhSimulasyon sim)
        {
            sim = null;
            if (PersonelMaliyetOrnek.ListeDonem(donem) == null) return "Dönem bulunamadı.";
            kapsam = string.IsNullOrWhiteSpace(kapsam) ? "*" : kapsam;
            var s = new PmhSimulasyon { Donem = donem, Kapsam = kapsam, Olusturan = kullanici, Olusturma = DateTime.Now, EslemeVersiyonu = _eslemeVersiyon };
            List<PmPersonel> kisiler; lock (PersonelMaliyetOrnek.Kilit) kisiler = Kaynak(donem, kapsam).ToList();
            if (kisiler.Count == 0) return "Kapsamda aktif personel yok.";
            var fisler = new Dictionary<string, PmhFis>(); int fisNo = 0;
            PmhFis Fis(string sirket, string grup, string baslik) { string k = sirket + "|" + grup; if (!fisler.TryGetValue(k, out var f)) fisler[k] = f = new PmhFis { Kod = "F" + (++fisNo).ToString("00"), Sirket = sirket, IslemGrubu = grup, Baslik = baslik }; return f; }
            string dAd = PersonelMaliyetOrnek.DonemAd(donem);
            void Satir(PmhFis f, PmPersonel p, string islem, string kalem, string xr, string yon, decimal tutar, string karsi, string acik)
            {
                if (tutar <= 0) return; tutar = Math.Round(tutar, 2);
                var e = EslemeBul(f.Sirket, islem, kalem, xr, yon, donem, karsi);
                var st = new PmhSatir { Id = ++_satirId, FisKodu = f.Kod, Sirket = f.Sirket, IslemGrubu = f.IslemGrubu, PersonelSira = p.Sira, AdSoyad = p.AdSoyad, SgkIsvereni = p.SgkIsvereni, FiiliGorevYeri = p.FiiliGorevYeri, OdemeSirketi = OdemeSirketi(p), IslemTuru = islem, XR = xr, KaynakKalem = kalem, KarsiSirket = karsi ?? "", Borc = yon == "Borç" ? tutar : 0, Alacak = yon == "Alacak" ? tutar : 0, EslemeId = e?.Id,
                    HesapKodu = e?.HesapKodu ?? "[Eşleşme Yok]", HesapAdi = e?.HesapAdi ?? "Hesap eşleştirmesi tanımlı değil", Aciklama = $"{p.AdSoyad} · {dAd} · {islem} · {kalem}{(string.IsNullOrEmpty(karsi) ? "" : " · karşı şirket " + karsi)}{(string.IsNullOrEmpty(acik) ? "" : " · " + acik)}" };
                if (e == null) st.Hatalar.Add($"Eksik eşleştirme: {f.Sirket} / {islem} / {kalem} / {xr} / {yon}");
                else { if (!e.Posting) st.Hatalar.Add($"{e.HesapKodu} üst seviye hesap; posting hesabı değil"); if (e.Sirket != "*" && e.Sirket != f.Sirket) st.Hatalar.Add("Hesap başka şirketin planından"); }
                f.Satirlar.Add(st);
            }
            decimal yansitma = 0, baskaOdeme = 0, genel = 0, ara = 0; int odemeYok = 0, xrHata = 0, genelHata = 0, yansitmaHata = 0;
            foreach (var p in kisiler)
            {
                var h = p.Hesap; string sgk = p.SgkIsvereni, fiili = FiiliSirket(p), ode = OdemeSirketi(p);
                genel += h.GenelMaliyet; ara += h.AraToplam;
                if (Math.Abs(h.NetXBordro + h.NetRBordro - h.AraToplam) > 0.01m) xrHata++;
                if (Math.Abs(h.AraToplam + h.ToplamBrutMasraflar + h.ToplamIsveren - h.GenelMaliyet) > 0.01m) genelHata++;
                if (!FiiliMap.ContainsKey(p.FiiliGorevYeri ?? "") && !string.Equals(p.FiiliGorevYeri, sgk, StringComparison.OrdinalIgnoreCase) && !(p.FiiliGorevYeri ?? "").StartsWith("Asia")) yansitmaHata++;
                // A) Bordro tahakkuku — SGK işvereni
                var fa = Fis(sgk, "A", "Bordro Tahakkuku");
                decimal kazanclar = h.NormalKazanc + h.HaftaSonuKazanc + h.ResmiTatilKazanc + h.BayramKazanc + h.UcretliIzinKazanc;
                const string A = "Bordro Tahakkuku";
                Satir(fa, p, A, "Ücret (Brüt Esas)", "R", "Borç", kazanclar, null, null); Satir(fa, p, A, "Fazla Mesai", "R", "Borç", h.ToplamEkCalisma, null, null); Satir(fa, p, A, "Sosyal Yardımlar", "R", "Borç", h.ToplamSosyal, null, null);
                Satir(fa, p, A, "Prim ve Ek Kazançlar", "R", "Borç", h.ToplamPrim, null, null); Satir(fa, p, A, "Yan Haklar", "R", "Borç", h.ToplamYanHaklar, null, null); Satir(fa, p, A, "Yasal Ödemeler", "R", "Borç", h.ToplamYasal, null, null);
                Satir(fa, p, A, "SGK İşveren Payı", "R", "Borç", h.SgkIsverenToplam, null, null); Satir(fa, p, A, "Diğer Ek Ödemeler (R)", "R", "Borç", p.DigerEkOdemelerR, null, null); Satir(fa, p, A, "Bordro Dışı Ödeme (X)", "X", "Borç", h.NetXBordro, null, "X-Net + X tarafı kalemler");
                Satir(fa, p, A, "Net-R Bordro", "R", "Alacak", h.NetRBordro, null, "bankaya yatacak net"); Satir(fa, p, A, "Net-X Bordro", "X", "Alacak", h.KalanNetElden, null, "elden ödenecek net");
                Satir(fa, p, A, "Gelir Vergisi", "R", "Alacak", h.GvTutari, null, null); Satir(fa, p, A, "Damga Vergisi", "R", "Alacak", h.DvTutari, null, null); Satir(fa, p, A, "SGK İşçi Payı", "R", "Alacak", h.SgkIsciToplam, null, null); Satir(fa, p, A, "SGK İşveren Payı", "R", "Alacak", h.SgkIsverenToplam, null, null);
                Satir(fa, p, A, "Avans Kesintisi", "R", "Alacak", p.Avans, null, "maaştan mahsup"); Satir(fa, p, A, "İcra Kesintisi", "R", "Alacak", p.Icra, null, null); Satir(fa, p, A, "Borç Kesintisi", "R", "Alacak", p.Borc, null, null); Satir(fa, p, A, "Diğer Kesintiler", "R", "Alacak", p.Bes + p.SendikaKatkisi + p.OzelSigortaKesintisi, null, "BES / sendika / özel sigorta"); Satir(fa, p, A, "Kesinti (X)", "X", "Alacak", p.KesintilerToplamiX, null, null);
                // B) Fiili görev yeri yansıtması — genel maliyet bazında (bileşenler ayrıca gider yazılmaz; çifte kayıt engeli)
                if (!string.Equals(fiili, sgk, StringComparison.OrdinalIgnoreCase) && h.GenelMaliyet > 0)
                {
                    yansitma += h.GenelMaliyet; const string B = "Fiili Görev Yeri Yansıtması"; string xr = h.NetXBordro > 0 && h.NetRBordro > 0 ? "X+R" : h.NetXBordro > 0 ? "X" : "R";
                    var fb1 = Fis(sgk, "B", "Fiili Görev Yeri Yansıtması (Yansıtan)"); Satir(fb1, p, B, "Grup İçi Alacak", xr, "Borç", h.GenelMaliyet, fiili, "genel maliyet"); Satir(fb1, p, B, "Yansıtma Geliri", xr, "Alacak", h.GenelMaliyet, fiili, "genel maliyet");
                    var fb2 = Fis(fiili, "B", "Fiili Görev Yeri Yansıtması (Yansıtılan)"); Satir(fb2, p, B, "Yansıtılan Personel Gideri", xr, "Borç", h.GenelMaliyet, sgk, "genel maliyet"); Satir(fb2, p, B, "Grup İçi Borç", xr, "Alacak", h.GenelMaliyet, sgk, "genel maliyet");
                }
                // C / D) Net personel ödemesi — R (banka) ödeme şirketinden (varsayılan Uras Power), X (elden) SGK işvereni kasasından; "Ödeme Yapılmıyor" notu ödeme kaydını engeller (kontrol 11)
                if (OdemeYapilmiyor(p)) { odemeYok++; continue; }
                const string C = "Net Personel Ödemesi", D = "Başka Şirket Tarafından Ödeme";
                if (h.KalanNetElden > 0) { var fcx = Fis(sgk, "C", "Net Personel Ödemesi"); Satir(fcx, p, C, "Net-X Bordro", "X", "Borç", h.KalanNetElden, null, "personel borcu kapatma"); Satir(fcx, p, C, "Kasa", "X", "Alacak", h.KalanNetElden, null, "elden ödeme"); }
                if (h.NetRBordro > 0)
                {
                    if (string.Equals(ode, sgk, StringComparison.OrdinalIgnoreCase))
                    {
                        var fc = Fis(sgk, "C", "Net Personel Ödemesi"); Satir(fc, p, C, "Net-R Bordro", "R", "Borç", h.NetRBordro, null, "personel borcu kapatma"); Satir(fc, p, C, "Banka", "R", "Alacak", h.NetRBordro, null, "maaş hesabından ödeme");
                    }
                    else
                    {
                        baskaOdeme += h.NetRBordro;
                        var fd1 = Fis(sgk, "D", "Başka Şirket Tarafından Ödeme (SGK İşvereni)"); Satir(fd1, p, D, "Net-R Bordro", "R", "Borç", h.NetRBordro, ode, "personel borcu kapatma"); Satir(fd1, p, D, "Grup İçi Borç", "*", "Alacak", h.NetRBordro, ode, "ödeme şirketine borç");
                        var fd2 = Fis(ode, "D", "Başka Şirket Tarafından Ödeme (Ödeme Şirketi)"); Satir(fd2, p, D, "Grup İçi Alacak", "*", "Borç", h.NetRBordro, sgk, sgk + " adına maaş ödemesi"); Satir(fd2, p, D, "Banka", "R", "Alacak", h.NetRBordro, sgk, "maaş hesabından ödeme");
                    }
                }
            }
            s.Fisler = fisler.Values.OrderBy(f => f.Kod).ToList(); s.PersonelSayisi = kisiler.Count; s.GenelMaliyet = Math.Round(genel, 2); s.AraToplam = Math.Round(ara, 2); s.YansitmaGereken = Math.Round(yansitma, 2); s.BaskaSirketOdeme = Math.Round(baskaOdeme, 2);
            s.Imza = KaynakImzasi(donem, kapsam);   // kilit sırası: PersonelMaliyet → Muhasebe (imza önce, kendi kilidimiz sonra)
            lock (Kilit)
            {
                var onceki = Simulasyonlar.Where(x => x.Donem == donem && x.Kapsam == kapsam).OrderByDescending(x => x.Versiyon).FirstOrDefault();
                foreach (var x in Simulasyonlar.Where(x => x.Donem == donem && x.Kapsam == kapsam && !x.Kilitli && x.Durum != "İptal")) { x.Durum = "İptal"; Iz(x, kullanici, "İptal", "Yeni versiyon oluşturuldu: " + (onceki?.No ?? "")); }
                s.Id = ++_simId; s.No = onceki?.No ?? $"SIM-{DateTime.Today.Year}-{_simId:0000}"; s.Versiyon = (onceki?.Versiyon ?? 0) + 1;
                KontrolEt(s, odemeYok, xrHata, genelHata, yansitmaHata);
                Iz(s, kullanici, "Simülasyon Oluşturuldu", $"v{s.Versiyon} · {s.PersonelSayisi} personel · {s.Fisler.Count} fiş · borç {s.ToplamBorc.ToString("N2", Tr)} / alacak {s.ToplamAlacak.ToString("N2", Tr)} · durum {s.Durum}");
                Simulasyonlar.Add(s);
            }
            sim = s; return null;
        }
        private static void KontrolEt(PmhSimulasyon s, int odemeYok, int xrHata, int genelHata, int yansitmaHata)
        {
            var k = new List<PmhKontrol>();
            void K(int no, string ad, string seviye, int adet, string ok, string hata) => k.Add(new PmhKontrol { No = no, Ad = ad, Seviye = seviye, Gecti = adet == 0, Adet = adet, Mesaj = adet == 0 ? ok : hata });
            int dengesiz = s.DengesizFis, baskaSirket = s.Fisler.SelectMany(f => f.Satirlar).Count(x => x.Hatalar.Any(h => h.Contains("başka şirketin"))), eksik = s.Fisler.SelectMany(f => f.Satirlar).Count(x => x.EslemeId == null), posting = s.Fisler.SelectMany(f => f.Satirlar).Count(x => x.Hatalar.Any(h => h.Contains("posting")));
            K(1, "Fiş Dengesi", "Kritik", dengesiz, "Her fişte borç = alacak.", $"{dengesiz} fişte borç ≠ alacak.");
            K(2, "Şirket-Hesap Uyumu", "Kritik", baskaSirket + eksik, "Tüm hesaplar kaydın atılacağı şirketin eşleştirmesinden.", $"{eksik} satırda eşleştirme yok, {baskaSirket} satırda hesap başka şirketin.");
            K(3, "Aktif / Posting Hesap", "Kritik", posting, "Tüm hesaplar aktif detay (posting) hesabı.", $"{posting} satırda üst seviye / pasif hesap.");
            K(4, "X/R Mutabakatı", "Kritik", xrHata, "Net-X Bordro + Net-R Bordro = Ara Toplam (tüm personel).", $"{xrHata} personelde X/R toplamı Ara Toplam'dan farklı.");
            K(5, "Genel Maliyet Mutabakatı", "Kritik", genelHata, "Ara Toplam + Toplam Brüt Masraflar + Toplam İşveren = Genel Maliyet.", $"{genelHata} personelde genel maliyet mutabakatı tutmuyor.");
            K(6, "Yansıtma Kontrolü", "Kritik", yansitmaHata, "SGK işvereni ve fiili görev yeri şirket eşlemesi tanımlı.", $"{yansitmaHata} personelde fiili görev yeri şirkete eşlenemedi.");
            int dEksik = 0; foreach (var g in s.Fisler.Where(f => f.IslemGrubu == "D").SelectMany(f => f.Satirlar).GroupBy(x => x.PersonelSira)) { var sirketler = g.Select(x => x.Sirket).Distinct().Count(); if (sirketler < 2) dEksik++; }
            K(7, "Ödeme Şirketi Kontrolü", "Kritik", dEksik, "SGK işvereni ≠ ödeme şirketi olan personelde iki şirket için karşılıklı set oluştu.", $"{dEksik} personelde karşı şirket kaydı eksik.");
            int mukerrer; lock (Kilit) mukerrer = Simulasyonlar.Count(x => x.Id != s.Id && x.Donem == s.Donem && (x.Kapsam == s.Kapsam || x.Kapsam == "*" || s.Kapsam == "*") && x.Durum == "Muhasebeleştirildi");
            K(8, "Mükerrer Kayıt", "Kritik", mukerrer, "Aynı dönem + kapsam daha önce muhasebeleştirilmemiş.", $"Aynı dönem için {mukerrer} muhasebeleştirilmiş simülasyon var (önce ters kayıt gerekir).");
            bool donemUygun = string.Compare(s.Donem, PersonelMaliyetOrnek.GuncelDonem, StringComparison.Ordinal) <= 0;
            K(9, "Dönem Kontrolü", "Kritik", donemUygun ? 0 : 1, "Muhasebe dönemi açık, belge tarihi uygun.", "Dönem henüz açılmadı (güncel dönemden sonra).");
            int acikEksik = s.Fisler.SelectMany(f => f.Satirlar).Count(x => string.IsNullOrWhiteSpace(x.Aciklama) || !x.Aciklama.Contains(x.AdSoyad));
            K(10, "Açıklama Standardı", "Uyarı", acikEksik, "Fiş açıklamaları personel, dönem, işlem türü ve karşı şirketi içeriyor.", $"{acikEksik} satırda açıklama standardı eksik.");
            K(11, "İstisna / Not", odemeYok > 0 ? "Uyarı" : "Kritik", odemeYok, "Ödeme engelleyen not yok.", $"{odemeYok} personelde \"Ödeme Yapılmıyor\" notu: ödeme kaydı üretilmedi (tahakkuk ve yansıtma yapıldı).");
            s.Kontroller = k; s.Durum = s.KritikHata ? "Kontrol Gerekiyor" : "Kontrole Hazır";
        }

        /// <summary>Onaya gönder / onayla / muhasebeleştir / ters kayıt / iptal. Hata metni döner (null = başarılı).</summary>
        public static string Islem(int id, string islem, string not, string kullanici, string yetki, out PmhSimulasyon sim)
        {
            sim = null; PmhSimulasyon s; lock (Kilit) s = Simulasyonlar.FirstOrDefault(x => x.Id == id);
            if (s == null) return "Simülasyon bulunamadı.";
            bool yetkili = yetki == "A" || yetki == "B" || yetki == "FN" || yetki == "MUH";
            switch (islem)
            {
                case "onayaGonder":
                    if (s.Durum != "Kontrole Hazır") return $"Onaya göndermek için durum 'Kontrole Hazır' olmalı (şu an {s.Durum}).";
                    if (!s.Guncel) { s.Durum = "Güncelliğini Yitirdi"; Iz(s, "Sistem", "Güncelliğini Yitirdi", "Kaynak bordro verisi ya da hesap eşleştirmesi değişti; yeniden simülasyon gerekir."); return "Kaynak veri simülasyondan sonra değişti; yeniden simülasyon oluşturun."; }
                    s.Durum = "Onaya Gönderildi"; s.OnayaGonderen = kullanici; s.OnayaGonderme = DateTime.Now; Iz(s, kullanici, "Onaya Gönderildi", $"v{s.Versiyon} · borç {s.ToplamBorc.ToString("N2", Tr)} / alacak {s.ToplamAlacak.ToString("N2", Tr)}"); break;
                case "onayla":
                    if (s.Durum != "Onaya Gönderildi") return $"Onaylamak için durum 'Onaya Gönderildi' olmalı (şu an {s.Durum}).";
                    if (!yetkili) return "Onay yetkiniz yok (yönetim, finans ya da muhasebe yetkisi gerekir).";
                    if (s.OnayaGonderen == kullanici && yetki != "A") return "Simülasyonu onaya gönderen kullanıcı onaylayamaz (ayrı yetki seviyesi).";
                    if (s.KritikHata) return "Kritik hata varken onay verilemez.";
                    if (!s.Guncel) { s.Durum = "Güncelliğini Yitirdi"; Iz(s, "Sistem", "Güncelliğini Yitirdi", "Kaynak veri değişti."); return "Kaynak veri değişti; yeniden simülasyon oluşturun."; }
                    s.Durum = "Onaylandı"; s.Onaylayan = kullanici; s.OnayTarihi = DateTime.Now; Iz(s, kullanici, "Onaylandı", $"v{s.Versiyon} içeriği kilitlendi · borç {s.ToplamBorc.ToString("N2", Tr)} / alacak {s.ToplamAlacak.ToString("N2", Tr)}" + (string.IsNullOrWhiteSpace(not) ? "" : " · " + not)); break;
                case "muhasebelestir":
                    if (s.Durum != "Onaylandı") return $"Muhasebeleştirmek için durum 'Onaylandı' olmalı (şu an {s.Durum}).";
                    if (!yetkili) return "Muhasebeleştirme yetkiniz yok.";
                    if (!s.Guncel) return "Onaydan sonra kaynak veri değişti; onay iptal edilip yeni versiyon oluşturulmalı.";
                    foreach (var f in s.Fisler)
                    {
                        // gizlilik: gerçek fiş hesap + X/R + karşı şirket bazında toplanmış atılır; personel adı fişe yazılmaz (detay simülasyonda, yetkiyle görünür)
                        var satirlar = f.Satirlar.GroupBy(x => new { x.HesapKodu, x.HesapAdi, x.KarsiSirket, x.XR }).Select(g => new YevmiyeSatiri { HesapKodu = g.Key.HesapKodu, HesapAdi = g.Key.HesapAdi, Borc = Math.Round(g.Sum(x => x.Borc), 2), Alacak = Math.Round(g.Sum(x => x.Alacak), 2), Aciklama = $"{f.Baslik} · {PersonelMaliyetOrnek.DonemAd(s.Donem)} · {g.Select(x => x.PersonelSira).Distinct().Count()} personel · {g.Key.XR}{(string.IsNullOrEmpty(g.Key.KarsiSirket) ? "" : " · karşı şirket " + g.Key.KarsiSirket)} · {s.No} v{s.Versiyon}" }).ToList();
                        var fis = MuhasebeOrnek.Olustur(DbName(f.Sirket), "Personel Maliyet · " + s.No, $"{f.Sirket} · {f.Baslik} · {PersonelMaliyetOrnek.DonemAd(s.Donem)} · {s.No} v{s.Versiyon} · {f.Kod}", satirlar, kullanici);
                        f.MuhasebeFisNo = fis?.No ?? $"PM-{s.Id:000}-{f.Kod}"; f.KayitTarihi = DateTime.Now;
                    }
                    s.Durum = "Muhasebeleştirildi"; s.Muhasebelestiren = kullanici; s.MuhasebeTarihi = DateTime.Now; Iz(s, kullanici, "Muhasebeleştirildi", string.Join(" · ", s.Fisler.Select(f => $"{f.Kod} {f.Sirket} → {f.MuhasebeFisNo}"))); break;
                case "tersKayit":
                    if (s.Durum != "Muhasebeleştirildi") return "Yalnız muhasebeleştirilmiş simülasyon için ters kayıt oluşturulabilir.";
                    if (!yetkili) return "Ters kayıt yetkiniz yok.";
                    if (string.IsNullOrWhiteSpace(not)) return "Ters kayıt nedeni zorunludur.";
                    foreach (var f in s.Fisler)
                    {
                        var kaynak = MuhasebeOrnek.Bul(f.MuhasebeFisNo);
                        var satirlar = (kaynak?.Satirlar ?? f.Satirlar.Select(x => new YevmiyeSatiri { HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Borc = x.Borc, Alacak = x.Alacak, Aciklama = x.Aciklama }).ToList()).Select(x => new YevmiyeSatiri { HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, Borc = x.Alacak, Alacak = x.Borc, Aciklama = "TERS KAYIT · " + x.Aciklama }).ToList();
                        var fis = MuhasebeOrnek.Olustur(DbName(f.Sirket), "Personel Maliyet · Ters Kayıt · " + s.No, $"TERS KAYIT · ref {f.MuhasebeFisNo} · {f.Sirket} · {f.Baslik} · {not}", satirlar, kullanici);
                        f.TersFisNo = fis?.No ?? "R-" + f.MuhasebeFisNo; f.TersTarihi = DateTime.Now;
                    }
                    s.Durum = "Ters Kayıt Yapıldı"; s.TersKayitNedeni = not; Iz(s, kullanici, "Ters Kayıt Oluşturuldu", not + " · " + string.Join(" · ", s.Fisler.Select(f => $"{f.MuhasebeFisNo} → {f.TersFisNo}"))); break;
                case "iptal":
                    if (s.Durum == "Muhasebeleştirildi" || s.Durum == "Ters Kayıt Yapıldı") return "Muhasebeleştirilmiş simülasyon iptal edilemez; ters kayıt oluşturun.";
                    if (s.Durum == "Onaylandı" && !yetkili) return "Onaylı simülasyonun iptali için yetki gerekir.";
                    s.Durum = "İptal"; Iz(s, kullanici, "İptal", string.IsNullOrWhiteSpace(not) ? "Kullanıcı iptal etti." : not); break;
                default: return "Tanımsız işlem.";
            }
            sim = s; return null;
        }

        // ---------- DTO ----------
        private static object SatirDto(PmhSatir x, string simNo, Dictionary<int, string> kodlar) => new { x.Id, simNo, x.FisKodu, x.Sirket, x.IslemGrubu, x.PersonelSira, cariKodu = kodlar != null && kodlar.TryGetValue(x.PersonelSira, out var ck) ? ck : null, x.AdSoyad, x.SgkIsvereni, x.FiiliGorevYeri, x.OdemeSirketi, x.IslemTuru, x.XR, x.KaynakKalem, x.HesapKodu, x.HesapAdi, x.Borc, x.Alacak, x.KarsiSirket, x.Aciklama, x.EslemeId, x.Hatalar, x.Kontrol };
        /// <summary>Dönem personelinin sıra → cari kodu sözlüğü (satır detayından Personel Cari Kartı bağlantısı için).</summary>
        private static Dictionary<int, string> CariKodlari(string donem) { lock (PersonelMaliyetOrnek.Kilit) return (PersonelMaliyetOrnek.ListeDonem(donem) ?? PersonelMaliyetOrnek.Liste).GroupBy(p => p.Sira).ToDictionary(g => g.Key, g => g.First().CariKodu); }
        public static object SimDto(PmhSimulasyon s, bool detay)
        {
            var kodlar = detay ? CariKodlari(s.Donem) : null;
            string durum = s.Durum; if (!s.Kilitli && s.Durum != "İptal" && !s.Guncel) durum = "Güncelliğini Yitirdi";
            return new
            {
                s.Id, s.No, s.Versiyon, s.Donem, donemAd = PersonelMaliyetOrnek.DonemAd(s.Donem), s.Kapsam, kapsamAd = s.Kapsam == "*" ? "Tüm Şirketler" : s.Kapsam, s.PersonelSayisi, durum, guncel = s.Guncel, kritikHata = s.KritikHata,
                s.Olusturan, olusturma = s.Olusturma.ToString("dd.MM.yyyy HH:mm"), s.OnayaGonderen, onayaGonderme = s.OnayaGonderme?.ToString("dd.MM.yyyy HH:mm"), s.Onaylayan, onayTarihi = s.OnayTarihi?.ToString("dd.MM.yyyy HH:mm"), s.Muhasebelestiren, muhasebeTarihi = s.MuhasebeTarihi?.ToString("dd.MM.yyyy HH:mm"), s.TersKayitNedeni,
                s.GenelMaliyet, s.AraToplam, s.YansitmaGereken, s.BaskaSirketOdeme, toplamBorc = s.ToplamBorc, toplamAlacak = s.ToplamAlacak, dengesizFis = s.DengesizFis, hataliSatir = s.HataliSatir, mukerrerRisk = s.Kontroller.FirstOrDefault(k => k.No == 8)?.Adet ?? 0, fisSayisi = s.Fisler.Count, satirSayisi = s.Fisler.Sum(f => f.Satirlar.Count), s.EslemeVersiyonu,
                kontroller = s.Kontroller.Select(k => new { k.No, k.Ad, k.Seviye, k.Gecti, k.Adet, k.Mesaj }).ToList(),
                fisler = s.Fisler.Select(f => new { f.Kod, f.Sirket, f.IslemGrubu, f.Baslik, satir = f.Satirlar.Count, personel = f.Satirlar.Select(x => x.PersonelSira).Distinct().Count(), toplamBorc = f.ToplamBorc, toplamAlacak = f.ToplamAlacak, fark = f.Fark, dengeli = f.Dengeli, hataliSatir = f.HataliSatir, f.MuhasebeFisNo, kayitTarihi = f.KayitTarihi?.ToString("dd.MM.yyyy HH:mm"), f.TersFisNo, tersTarihi = f.TersTarihi?.ToString("dd.MM.yyyy HH:mm"), db = DbName(f.Sirket),
                    // özet (gizlilik): muhasebeye atılacak hâl — hesap + X/R + karşı şirket bazında toplam, personel adı yok
                    hesapOzeti = f.Satirlar.GroupBy(x => new { x.HesapKodu, x.HesapAdi, x.XR, x.KarsiSirket }).Select(g => new { g.Key.HesapKodu, g.Key.HesapAdi, xr = g.Key.XR, karsiSirket = g.Key.KarsiSirket, borc = Math.Round(g.Sum(x => x.Borc), 2), alacak = Math.Round(g.Sum(x => x.Alacak), 2), satir = g.Count(), personel = g.Select(x => x.PersonelSira).Distinct().Count(), hatali = g.Count(x => x.Hatalar.Count > 0) }).OrderBy(g => g.alacak > 0 && g.borc == 0).ThenBy(g => g.HesapKodu, StringComparer.Ordinal).ToList() }).ToList(),
                satirlar = detay ? s.Fisler.SelectMany(f => f.Satirlar).Select(x => SatirDto(x, s.No, kodlar)).ToList() : null,
                audit = s.Audit.OrderBy(a => a.Tarih).Select(a => new { tarih = a.Tarih.ToString("dd.MM.yyyy HH:mm:ss"), a.Kullanici, a.Islem, a.Detay }).ToList()
            };
        }
        /// <summary>Otomatik simülasyon: ekran açılınca düğmeye basmadan güncel simülasyon hazır olur. Dönem için hiç simülasyon yoksa, en yenisi iptal / ters kayıt ise ya da açık simülasyon güncelliğini yitirdiyse yeni versiyon üretilir; onaylı / muhasebeleştirilmiş simülasyona dokunulmaz.</summary>
        public static void OtomatikSimulasyon(string donem, string kullanici)
        {
            if (PersonelMaliyetOrnek.ListeDonem(donem) == null) return;
            PmhSimulasyon son; lock (Kilit) son = Simulasyonlar.Where(x => x.Donem == donem && x.Kapsam == "*").OrderByDescending(x => x.Id).FirstOrDefault();
            bool gerek = son == null || son.Durum == "İptal" || son.Durum == "Ters Kayıt Yapıldı" || (!son.Kilitli && !son.Guncel);
            if (gerek) Olustur(donem, "*", "Sistem (otomatik)", out _);
        }
        /// <summary>Sekme paketi: dönemin simülasyonları (en yenisi detaylı), hesap eşleştirmeleri, hesap planı, şirketler, kullanıcının yetkisi.</summary>
        public static object Paket(string donem, string kullanici, string yetki)
        {
            OtomatikSimulasyon(donem, kullanici);
            List<PmhSimulasyon> l; lock (Kilit) l = Simulasyonlar.Where(x => x.Donem == donem).OrderByDescending(x => x.Id).ToList();
            var secili = l.FirstOrDefault(x => x.Durum != "İptal") ?? l.FirstOrDefault();
            List<object> eslemeler; lock (Kilit) eslemeler = Eslemeler.OrderBy(e => e.IslemTuru == "Bordro Tahakkuku" ? 0 : e.IslemTuru == "Fiili Görev Yeri Yansıtması" ? 1 : e.IslemTuru == "Net Personel Ödemesi" ? 2 : 3).ThenBy(e => e.Yon == "Borç" ? 0 : 1).ThenBy(e => e.Sirket).ThenBy(e => e.Id).Select(EslemeDto).ToList();
            bool yetkili = yetki == "A" || yetki == "B" || yetki == "FN" || yetki == "MUH";
            return new
            {
                donem, simulasyonlar = l.Select(x => SimDto(x, x.Id == secili?.Id)).ToList(), seciliId = secili?.Id, eslemeler, hesaplar = HesapListesi.ToList(), sirketler = Sirketler, islemTurleri = IslemTurleri, durumlar = Durumlar,
                kullanici, yetki, yetkili, eslemeVersiyonu = _eslemeVersiyon, imza = KaynakImzasi(donem, "*"), muhasebeEkrani = "/Muhasebe",
                kaynakOzet = KaynakOzeti(donem)
            };
        }
        /// <summary>Ödemeler sekmesi (Personel Muhasebe İşlemleri): dönemin güncel simülasyonundaki C / D grubu banka ve kasa satırları — personel bazında kim, hangi şirketten, hangi kanaldan ne kadar; "Ödeme Yapılmıyor" notlu personel ayrı listelenir.</summary>
        public class PmhOdeme { public int PersonelSira { get; set; } public string AdSoyad { get; set; } public string Odeyen { get; set; } public string SgkIsvereni { get; set; } public string FiiliGorevYeri { get; set; } public string OdemeSirketi { get; set; } public string Kanal { get; set; } public string XR { get; set; } public decimal Tutar { get; set; } public string HesapKodu { get; set; } public string HesapAdi { get; set; } public string FisKodu { get; set; } public string IslemGrubu { get; set; } public string MuhasebeFisNo { get; set; } public string TersFisNo { get; set; } public bool Hatali { get; set; } public string Db { get; set; } }
        public static object OdemePaketi(string donem)
        {
            PmhSimulasyon s; lock (Kilit) s = Simulasyonlar.Where(x => x.Donem == donem && x.Durum != "İptal").OrderByDescending(x => x.Id).FirstOrDefault() ?? Simulasyonlar.Where(x => x.Donem == donem).OrderByDescending(x => x.Id).FirstOrDefault();
            List<PmPersonel> k; lock (PersonelMaliyetOrnek.Kilit) k = Kaynak(donem, "*").Where(OdemeYapilmiyor).ToList();
            var odemeYok = k.Select(p => new { sira = p.Sira, adSoyad = p.AdSoyad, cariKodu = p.CariKodu, sgkIsvereni = p.SgkIsvereni, fiiliGorevYeri = p.FiiliGorevYeri, odemeSirketi = OdemeSirketi(p), not = p.Not, netR = p.Hesap.NetRBordro, netX = p.Hesap.KalanNetElden }).ToList();
            if (s == null) return new { simNo = (string)null, versiyon = 0, durum = "Simülasyon Yok", odenebilir = false, satirlar = new List<PmhOdeme>(), odemeYok };
            var satirlar = s.Fisler.Where(f => f.IslemGrubu == "C" || f.IslemGrubu == "D")
                .SelectMany(f => f.Satirlar.Where(x => x.KaynakKalem == "Banka" || x.KaynakKalem == "Kasa").Select(x => new PmhOdeme
                {
                    PersonelSira = x.PersonelSira, AdSoyad = x.AdSoyad, Odeyen = f.Sirket, SgkIsvereni = x.SgkIsvereni ?? (f.IslemGrubu == "D" && !string.IsNullOrEmpty(x.KarsiSirket) ? x.KarsiSirket : f.Sirket), FiiliGorevYeri = x.FiiliGorevYeri, OdemeSirketi = x.OdemeSirketi,
                    Kanal = x.KaynakKalem == "Banka" ? "Banka (R)" : "Kasa / Elden (X)", XR = x.XR, Tutar = x.Alacak, HesapKodu = x.HesapKodu, HesapAdi = x.HesapAdi, FisKodu = f.Kod, IslemGrubu = f.IslemGrubu,
                    MuhasebeFisNo = f.MuhasebeFisNo, TersFisNo = f.TersFisNo, Hatali = x.Hatalar.Count > 0, Db = DbName(f.Sirket)
                })).OrderBy(x => x.Odeyen).ThenBy(x => x.Kanal).ThenBy(x => x.AdSoyad, StringComparer.Create(Tr, true)).ToList();
            return new { simNo = s.No, versiyon = s.Versiyon, durum = s.Durum, odenebilir = s.Durum == "Muhasebeleştirildi", satirlar, odemeYok };
        }
        /// <summary>Fiş Arşivi: tüm dönemlerin simülasyonları (özet; satırlar yok).</summary>
        public static List<object> Arsiv() { List<PmhSimulasyon> l; lock (Kilit) l = Simulasyonlar.OrderByDescending(x => x.Id).ToList(); return l.Select(x => SimDto(x, false)).ToList(); }
        /// <summary>Personel Muhasebe İşlemleri ortak filtre seçenekleri: dönemin aktif personelindeki SGK İşvereni / Fiili Görev Yeri / Ödeme Şirketi değerleri (aynı üç alan her personel tablosunda sütundur).</summary>
        public static object Filtreler(string donem)
        {
            List<PmPersonel> k; lock (PersonelMaliyetOrnek.Kilit) k = Kaynak(donem, "*").ToList();
            List<string> Sirala(IEnumerable<string> l) => l.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().OrderBy(x => x, StringComparer.Create(Tr, true)).ToList();
            return new { sgkIsverenleri = Sirala(k.Select(p => p.SgkIsvereni)), fiiliGorevYerleri = Sirala(k.Select(p => p.FiiliGorevYeri)), odemeSirketleri = Sirala(k.Select(OdemeSirketi)) };
        }
        private static object KaynakOzeti(string donem)
        {
            List<PmPersonel> k; lock (PersonelMaliyetOrnek.Kilit) k = Kaynak(donem, "*").ToList();
            return new { personel = k.Count, genelMaliyet = Math.Round(k.Sum(p => p.Hesap.GenelMaliyet), 2), araToplam = Math.Round(k.Sum(p => p.Hesap.AraToplam), 2), yansitma = Math.Round(k.Where(p => !string.Equals(FiiliSirket(p), p.SgkIsvereni, StringComparison.OrdinalIgnoreCase)).Sum(p => p.Hesap.GenelMaliyet), 2), baskaSirket = Math.Round(k.Where(p => !string.Equals(OdemeSirketi(p), p.SgkIsvereni, StringComparison.OrdinalIgnoreCase) && !OdemeYapilmiyor(p)).Sum(p => p.Hesap.NetRBordro), 2), odemeYok = k.Count(OdemeYapilmiyor), sirketler = k.Select(p => p.SgkIsvereni).Distinct().OrderBy(x => x).ToList(), varsayilanOdemeSirketi = VarsayilanOdemeSirketi };
        }
    }
}
