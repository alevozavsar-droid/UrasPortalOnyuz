// ============================================================
// PERSONEL MALİYET RAPORU — "Ek Çalışmalar" ve "Diğer Kesintiler" sekmeleri (dinamik: talep modüllerinden ve Kasa Nakit Akışı'ndan okunur)
//   Ek Çalışmalar   : Mesai talepleri (Data/TalepMotoru.cs, modül MESAI) — kim mesai yapmış, kim onaylamış, ne zaman; PDKS gerçekleşmesi, puantaj aktarımı ve
//                     taleplerden hesaplanan tutar (fiili saat × saat ücreti × çarpan) ↔ Personel Maliyet Tablosu'ndaki Fazla Mesai (R + X) karşılaştırması.
//   Diğer Kesintiler: Şahsi (maaş) avans talepleri (Data/NakitAvansOrnek.cs) — onay adımları, kasa ödemesi, Kasa Nakit Akışı (finans konsolide) senkron satırı;
//                     kesinti planları (PkKesinti: avans mahsubu, icra, borç, BES …) — toplam, aylık taksit, ödenen / kalan, başlangıç ve bitiş dönemi ("ne zamana kadar"), PDF ekleri (icra dosyası).
//   Personel eşlemesi: Personel Maliyet Tablosu kişileri talep modüllerinde NaKisi olarak tanınır (PersonelNo = cari kodu 335.xxx; NakitAvansOrnek.KisiBul bu listeye de bakar).
//   Tohum: güncel dönemde Fazla Mesai (R / X) olan kişiler için Ekim 2026 mesai talepleri; kesintisi olanlar için şahsi avans + mahsup planı, iki örnek icra dosyası, borç planları.
//   Yeni kesinti / icra kaydı (KesintiEkle) ilgili kişinin seçili dönemdeki kesinti sütununa yansır (PersonelMaliyetOrnek.KesintiYansit).
// Ekran: Controllers/PersonelMaliyetController.cs (Paket: ekCalisma, kesinti; KesintiEkle, KesintiEkYukle, KesintiEk) + Views/PersonelMaliyet/Index.cshtml + wwwroot/js/personel-maliyet.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Kesinti planı eki (icra dosyası PDF'i, ödeme planı, senet …); dosya bellek içinde tutulur.</summary>
    public class PkEk { public int No { get; set; } public string Ad { get; set; } public string Tip { get; set; } public long Boyut { get; set; } public DateTime Tarih { get; set; } public string Yukleyen { get; set; } public byte[] Veri { get; set; } }

    /// <summary>Kesinti planı: aylık taksitle bordrodan düşülen tutar (avans mahsubu, icra, personel borcu, BES, özel sigorta, sendika).</summary>
    public class PkKesinti
    {
        public int Id { get; set; }
        public int PersonelSira { get; set; } public string AdSoyad { get; set; }
        public string Tur { get; set; }                 // Avans Kesintisi | İcra Kesintisi | Borç Kesintisi | BES | Özel Sigorta Kesintisi | Sendika Katkısı
        public string Kaynak { get; set; }              // avans talep no (NA-…), icra dosya no, "Personel borç senedi" …
        public string IcraDairesi { get; set; } = ""; public string Alacakli { get; set; } = ""; public string DosyaNo { get; set; } = "";
        public decimal ToplamTutar { get; set; } public decimal AylikTaksit { get; set; } public int TaksitSayisi { get; set; } public int OdenenTaksit { get; set; }
        public string BaslangicDonem { get; set; }      // yyyy-MM (ilk kesinti dönemi)
        public string Aciklama { get; set; } = "";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; }
        public List<PkEk> Ekler { get; set; } = new List<PkEk>();
        // ---- hesaplanan
        public decimal Odenen => Math.Min(ToplamTutar, AylikTaksit * OdenenTaksit);
        public decimal Kalan => Math.Max(0, ToplamTutar - Odenen);
        public int KalanTaksit => Math.Max(0, TaksitSayisi - OdenenTaksit);
        public string BitisDonem => PersonelKesintiOrnek.AyEkle(BaslangicDonem, Math.Max(0, TaksitSayisi - 1));
        public string Durum => Kalan <= 0 ? "Tamamlandı" : string.Compare(BaslangicDonem, PersonelMaliyetOrnek.GuncelDonem, StringComparison.Ordinal) > 0 ? "Başlayacak" : "Devam Ediyor";
        public bool DonemdeAktif(string donem) => string.Compare(donem, BaslangicDonem, StringComparison.Ordinal) >= 0 && string.Compare(donem, BitisDonem, StringComparison.Ordinal) <= 0;
        /// <summary>Seçili dönemde düşülecek taksit (son taksitte kalan tutar).</summary>
        public decimal DonemTaksiti(string donem) { if (!DonemdeAktif(donem)) return 0; int n = PersonelKesintiOrnek.AyFarki(BaslangicDonem, donem); return n == TaksitSayisi - 1 ? Math.Max(0, ToplamTutar - AylikTaksit * n) : AylikTaksit; }
    }

    public static class PersonelKesintiOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        public static readonly string[] KesintiTurleri = { "Avans Kesintisi", "İcra Kesintisi", "Borç Kesintisi", "BES", "Özel Sigorta Kesintisi", "Sendika Katkısı" };
        /// <summary>Kesinti türü → Personel Maliyet Tablosu sütunu (PmPersonel özelliği).</summary>
        public static readonly Dictionary<string, string> TurSutun = new Dictionary<string, string> { ["Avans Kesintisi"] = "Avans", ["İcra Kesintisi"] = "Icra", ["Borç Kesintisi"] = "Borc", ["BES"] = "Bes", ["Özel Sigorta Kesintisi"] = "OzelSigortaKesintisi", ["Sendika Katkısı"] = "SendikaKatkisi" };
        private static readonly string[] Aylar = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        public const decimal AylikSaat = 225m;   // saat ücreti = aylık net ücret / 225 (30 gün × 7,5 saat)

        public static string AyEkle(string donem, int ay) { if (string.IsNullOrEmpty(donem) || donem.Length < 7) return donem; int y = int.Parse(donem.Substring(0, 4)), m = int.Parse(donem.Substring(5, 2)) - 1 + ay; return $"{y + (m >= 0 ? m / 12 : (m - 11) / 12):0000}-{((m % 12) + 12) % 12 + 1:00}"; }
        public static int AyFarki(string a, string b) => (int.Parse(b.Substring(0, 4)) - int.Parse(a.Substring(0, 4))) * 12 + int.Parse(b.Substring(5, 2)) - int.Parse(a.Substring(5, 2));
        public static string DonemAd(string kod) => PersonelMaliyetOrnek.DonemAd(kod);

        // ---------- Personel Maliyet kişileri → talep modüllerinin NaKisi'si ----------
        private static readonly Dictionary<int, NaKisi> _kisiler = new Dictionary<int, NaKisi>();
        private static string MasterSirket(string sgk) => sgk == "ALV Kimya" || sgk == "Alv Filo" ? "Alv Kimya" : sgk == "Selvi Kimya" ? "Selvi" : sgk == "URS Makine" ? "URS Makina" : sgk;
        public static NaKisi NaKisi(PmPersonel p)
        {
            lock (_kisiler)
            {
                if (!_kisiler.TryGetValue(p.Sira, out var k)) _kisiler[p.Sira] = k = new NaKisi { PersonelNo = p.CariKodu, Rol = "Talep Sahibi" };
                k.AdSoyad = p.AdSoyad; k.Sirket = MasterSirket(p.SgkIsvereni); k.Departman = p.Bolum; k.Unvan = p.Gorev; k.MasrafMerkezi = p.DepartmanKodu + " - " + p.Bolum; k.KullaniciId = p.SicilNo; k.Aktif = p.Durum == "Aktif";
                return k;
            }
        }
        /// <summary>Ad, cari kodu (335.xxx) ya da sicil no ile Personel Maliyet kişisi (güncel dönem).</summary>
        public static NaKisi KisiBul(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            PmPersonel p; lock (PersonelMaliyetOrnek.Kilit) p = PersonelMaliyetOrnek.Liste.FirstOrDefault(x => string.Equals(x.AdSoyad, s, StringComparison.OrdinalIgnoreCase) || x.CariKodu == s || string.Equals(x.SicilNo, s, StringComparison.OrdinalIgnoreCase));
            return p == null ? null : NaKisi(p);
        }
        public static bool PmKisiMi(NaKisi k) => k != null && (k.PersonelNo ?? "").StartsWith("335.");
        private static PmPersonel PmBul(NaKisi k) { if (k == null) return null; lock (PersonelMaliyetOrnek.Kilit) return PersonelMaliyetOrnek.Liste.FirstOrDefault(x => x.CariKodu == k.PersonelNo) ?? PersonelMaliyetOrnek.Liste.FirstOrDefault(x => string.Equals(x.AdSoyad, k.AdSoyad, StringComparison.OrdinalIgnoreCase)); }
        /// <summary>Personel Maliyet kişisinin birim yöneticisi: aynı bölümde Müdür / Yönetici unvanlı kişi; yoksa Bordro ve Özlük İşleri Yöneticisi.</summary>
        public static NaKisi Yonetici(NaKisi k)
        {
            var p = PmBul(k); if (p == null) return null;
            PmPersonel y; lock (PersonelMaliyetOrnek.Kilit) y = PersonelMaliyetOrnek.Liste.FirstOrDefault(x => x.Sira != p.Sira && x.Durum == "Aktif" && x.Bolum == p.Bolum && (x.Unvan == "Müdür" || x.Unvan == "Yönetici")) ?? PersonelMaliyetOrnek.Liste.FirstOrDefault(x => x.Sira != p.Sira && x.Gorev.StartsWith("Bordro ve Özlük"));
            return y == null ? null : NaKisi(y);
        }
        public static NaKisi UstYonetim() { PmPersonel g; lock (PersonelMaliyetOrnek.Kilit) g = PersonelMaliyetOrnek.Liste.FirstOrDefault(x => x.AdSoyad.StartsWith("Alev Özavşar")); return g == null ? null : NaKisi(g); }
        public static NaKisi Ik() { PmPersonel g; lock (PersonelMaliyetOrnek.Kilit) g = PersonelMaliyetOrnek.Liste.FirstOrDefault(x => x.Gorev.StartsWith("Bordro ve Özlük")); return g == null ? null : NaKisi(g); }

        // ---------- kayıtlar ----------
        public static readonly List<PkKesinti> Kesintiler = new List<PkKesinti>();
        private static int _sonId, _sonEk;
        private static readonly Lazy<bool> _kurulum = new Lazy<bool>(() => { Tohum(); return true; });
        public static void Hazirla() { var _ = _kurulum.Value; }

        // ---------- Ek Çalışmalar: mesai talepleri ----------
        public static decimal SaatUcreti(PmPersonel p) { var h = p.Hesap; decimal net = h.XNetTl + p.RNet; return net > 0 ? Math.Round(net / AylikSaat, 2) : 0; }
        private static decimal Carpan(TmTalep t) => decimal.TryParse((t.Hesap.TryGetValue("carpan", out var c) ? c : "x1,5").TrimStart('x'), NumberStyles.Any, TalepMotoru.Tr, out var d) ? d : 1.5m;
        private static decimal Saat(TmTalep t, string anahtar, bool ek) => decimal.TryParse(ek ? t.E(anahtar) : (t.Hesap.TryGetValue(anahtar, out var v) ? v : "0"), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
        private static string MesaiDonemi(TmTalep t) => t.Ek.ContainsKey("puantajDonemi") ? t.E("puantajDonemi") : t.T("tarih")?.ToString("yyyy-MM") ?? "";
        /// <summary>Seçili bordro dönemine düşen mesai talepleri (Personel Maliyet kişileri): bir önceki ayın puantajı bu ayın bordrosuna girer (prim gibi) — puantaj dönemi / mesai ayı = dönem − 1;
        /// ayrıca henüz puantaja aktarılmamış, bu ay tarihli (planlı / onay bekleyen) talepler de gösterilir. Dönem boşsa hepsi.</summary>
        public static List<TmTalep> Mesailer(string donem)
        {
            Hazirla(); string onceki = string.IsNullOrEmpty(donem) ? "" : AyEkle(donem, -1);
            lock (TalepMotoru.Kilit) return TalepMotoru.Talepler.Where(t => t.ModulKod == "MESAI" && PmBul(t.Kullanan) != null && (string.IsNullOrEmpty(donem) || MesaiDonemi(t) == onceki || (!t.Ek.ContainsKey("puantajTarihi") && (t.T("tarih")?.ToString("yyyy-MM") ?? "") == donem))).OrderByDescending(t => t.T("tarih")).ThenBy(t => t.Kullanan.AdSoyad).ToList();
        }
        private static object Onay(NaOnay o) => new { o.Sira, o.Adim, o.Kisi, o.Durum, tarih = o.Tarih?.ToString("dd.MM.yyyy HH:mm"), o.Not };
        private static object Zaman(NaHareket z) => new { tarih = z.Tarih.ToString("dd.MM.yyyy HH:mm"), z.Hareket, z.Kaynak };
        public static object MesaiDto(TmTalep t)
        {
            var p = PmBul(t.Kullanan); decimal su = p == null ? 0 : SaatUcreti(p), plan = Saat(t, "planlananSaat", false), fiili = t.Ek.ContainsKey("fiiliSaat") ? Saat(t, "fiiliSaat", true) : 0, carpan = Carpan(t);
            bool puantaj = t.Ek.ContainsKey("puantajTarihi"); decimal esas = fiili > 0 ? fiili : plan;
            var yon = t.Onaylar.FirstOrDefault(o => o.Adim == "Yönetici Onayı"); var digerOnay = t.Onaylar.Where(o => o.Sira > 1 && o.Adim != "Yönetici Onayı").ToList();
            return new
            {
                id = t.Id, no = t.No, adSoyad = t.Kullanan.AdSoyad, personelSira = p?.Sira, sgkIsvereni = p?.SgkIsvereni, bolum = p?.Bolum, gorev = p?.Gorev, olusturan = t.Olusturan?.AdSoyad, talepTarihi = t.Tarih.ToString("dd.MM.yyyy HH:mm"),
                tur = t.Tur?.Ad, turKod = t.TurKod, tarih = t.T("tarih")?.ToString("dd.MM.yyyy"), donem = MesaiDonemi(t), aralik = t.V("baslangicSaat") + "–" + t.V("bitisSaat"), mola = t.V("mola"), planlanan = plan, fiili = t.Ek.ContainsKey("fiiliSaat") ? fiili : (decimal?)null,
                fiiliGiris = t.E("fiiliGiris"), fiiliCikis = t.E("fiiliCikis"), carpan, saatUcreti = su, tutar = t.Onaylandi && !t.Iptal && t.V("karsilik").StartsWith("Ücret") ? Math.Round(esas * su * carpan, 2) : 0, puantajaAktarilan = puantaj && t.V("karsilik").StartsWith("Ücret") ? Math.Round(Saat(t, "puantajSaat", true) * su * carpan, 2) : 0,
                karsilik = t.V("karsilik"), serbestZaman = t.Hesap.TryGetValue("serbestZaman", out var sz) ? sz : "—", proje = t.V("proje"), gerekce = t.V("gerekce"), servis = t.V("servis") == "true", yemek = t.V("yemek") == "true",
                talepDurumu = t.TalepDurumu, onaylandi = t.Onaylandi, iptal = t.Iptal, onayTarihi = t.OnayTarihi?.ToString("dd.MM.yyyy HH:mm"), yonetici = yon?.Kisi, yoneticiTarih = yon?.Tarih?.ToString("dd.MM.yyyy HH:mm"), yoneticiDurum = yon?.Durum,
                digerOnaylar = string.Join(" · ", digerOnay.Select(o => $"{o.Adim.Replace(" Onayı", "")}: {o.Kisi} ({o.Durum}{(o.Tarih.HasValue ? " " + o.Tarih.Value.ToString("dd.MM") : "")})")),
                gerceklesme = t.Durum2, puantaj = t.Durum3, puantajDonemi = t.E("puantajDonemi"), puantajTarihi = t.E("puantajTarihi"), yillikToplam = t.Hesap.TryGetValue("yillikToplam", out var yt) ? yt : "", kalanSinir = t.Hesap.TryGetValue("kalanSinir", out var ks) ? ks : "",
                onaylar = t.Onaylar.Select(Onay).ToList(), zaman = t.Zaman.OrderBy(z => z.Tarih).Select(Zaman).ToList(), uyarilar = t.Uyarilar
            };
        }
        public static object EkCalismaPaketi(string donem)
        {
            var l = Mesailer(donem);
            return new { donem, mesailer = l.Select(MesaiDto).ToList(), talepEkrani = "/IkTalep/Mesai", aylikSaat = AylikSaat };
        }

        // ---------- Diğer Kesintiler: şahsi avanslar ----------
        /// <summary>Personel Maliyet kişilerinin şahsi (maaş) avansları; dönem verilince o dönemde açık olan ya da o ayda açılan / ödenen talepler.</summary>
        public static List<NakitAvans> Avanslar(string donem)
        {
            Hazirla();
            lock (NakitAvansOrnek.Kilit) return NakitAvansOrnek.Talepler.Where(a => NakitAvansOrnek.SahsiMi(a) && PmBul(a.Kullanan) != null && (string.IsNullOrEmpty(donem) || AvansDonemde(a, donem))).OrderByDescending(a => a.Tarih).ToList();
        }
        private static bool AvansDonemde(NakitAvans a, string donem)
        {
            string talepAy = a.Tarih.ToString("yyyy-MM"); if (talepAy == donem) return true;
            if (a.Odemeler.Any(o => o.Tarih.ToString("yyyy-MM") == donem)) return true;
            var plan = Kesintiler.FirstOrDefault(k => k.Kaynak == a.No); if (plan != null && plan.DonemdeAktif(donem)) return true;
            return string.Compare(talepAy, donem, StringComparison.Ordinal) < 0 && !a.Kapandi && a.TalepDurumu != "Reddedildi";
        }
        public static object AvansDto(NakitAvans a, string donem)
        {
            var p = PmBul(a.Kullanan); var plan = Kesintiler.FirstOrDefault(k => k.Kaynak == a.No); var na = NakitAkisOrnek.AvansSatiri(a.No);
            object onay(string adim) { var o = a.Onaylar.FirstOrDefault(x => x.Adim == adim); return o == null ? null : new { o.Kisi, o.Durum, tarih = o.Tarih?.ToString("dd.MM.yyyy HH:mm"), o.Not }; }
            return new
            {
                id = a.Id, no = a.No, adSoyad = a.Kullanan.AdSoyad, personelSira = p?.Sira, sgkIsvereni = p?.SgkIsvereni, bolum = p?.Bolum, sirket = a.Sirket, talepTuru = a.TalepTuru, konu = a.Konu, aciklama = a.Aciklama,
                tarih = a.Tarih.ToString("dd.MM.yyyy HH:mm"), tutar = a.Tutar, paraBirimi = a.ParaBirimi, onaylananTutar = a.OnaylananTutar, kullanimTarihi = a.KullanimTarihi.ToString("dd.MM.yyyy"), kapatmaTarihi = a.KapatmaTarihi.ToString("dd.MM.yyyy"),
                talepDurumu = a.TalepDurumu, onaylandi = a.Onaylandi, onayTarihi = a.OnayTarihi?.ToString("dd.MM.yyyy HH:mm"), yonetici = onay("Yönetici Onayı"), ustYonetim = onay("Üst Yönetim Onayı"), finans = onay("Finans Uygunluk Onayı"),
                odemeler = a.Odemeler.Select(o => new { tarih = o.Tarih.ToString("dd.MM.yyyy HH:mm"), o.Tutar, o.Kasa, o.FisNo, o.TeslimEden, o.TeslimAlan, o.TeslimSekli, o.Durum, o.TeslimAlanOnayi }).ToList(),
                teslimEdilen = a.TeslimEdilen, acikTutar = a.AcikTutar, odemeDurumu = a.OdemeDurumu, kapatmaDurumu = a.KapatmaDurumu, gecikenGun = a.GecikenGun,
                nakitAkis = na == null ? null : new { docNum = na.DocNum, sirket = NakitAkisOrnek.SirketAdi(na.U_KaynakFirma), durum = na.U_Durum, planlanan = na.U_PlanlananTarih?.ToString("dd.MM.yyyy"), odenme = na.U_OdenmeTarihi?.ToString("dd.MM.yyyy"), kasa = na.U_Kasa, makbuz = na.U_MakbuzNo, tutarTl = na.U_TutarTL, kurum = na.U_Kurum },
                plan = plan == null ? null : new { plan.Id, plan.AylikTaksit, plan.TaksitSayisi, plan.OdenenTaksit, plan.Kalan, plan.BaslangicDonem, baslangicAd = DonemAd(plan.BaslangicDonem), plan.BitisDonem, bitisAd = DonemAd(plan.BitisDonem), plan.Durum, donemTaksiti = plan.DonemTaksiti(donem ?? PersonelMaliyetOrnek.GuncelDonem) },
                onaylar = a.Onaylar.Select(Onay).ToList(), zaman = a.Zaman.OrderBy(z => z.Tarih).Select(Zaman).ToList()
            };
        }

        // ---------- kesinti planları ----------
        public static object KesintiDto(PkKesinti k, string donem) => new
        {
            k.Id, k.PersonelSira, k.AdSoyad, k.Tur, k.Kaynak, k.IcraDairesi, k.Alacakli, k.DosyaNo, k.ToplamTutar, k.AylikTaksit, k.TaksitSayisi, k.OdenenTaksit, k.Odenen, k.Kalan, k.KalanTaksit,
            k.BaslangicDonem, baslangicAd = DonemAd(k.BaslangicDonem), k.BitisDonem, bitisAd = DonemAd(k.BitisDonem), k.Durum, donemTaksiti = k.DonemTaksiti(donem ?? PersonelMaliyetOrnek.GuncelDonem), aktif = k.DonemdeAktif(donem ?? PersonelMaliyetOrnek.GuncelDonem),
            k.Aciklama, olusturma = k.Olusturma.ToString("dd.MM.yyyy HH:mm"), k.Olusturan, ekler = k.Ekler.Select(e => new { e.No, e.Ad, e.Tip, boyut = e.Boyut < 1024 * 1024 ? (e.Boyut / 1024m).ToString("0") + " KB" : (e.Boyut / 1048576m).ToString("0.0", Tr) + " MB", tarih = e.Tarih.ToString("dd.MM.yyyy"), e.Yukleyen }).ToList()
        };
        public static object KesintiPaketi(string donem)
        {
            Hazirla(); var avanslar = Avanslar(donem);
            List<PkKesinti> planlar; lock (Kilit) planlar = Kesintiler.OrderBy(k => k.Durum == "Tamamlandı").ThenBy(k => k.AdSoyad, StringComparer.Create(Tr, true)).ToList();
            return new { donem, avanslar = avanslar.Select(a => AvansDto(a, donem)).ToList(), planlar = planlar.Select(k => KesintiDto(k, donem)).ToList(), turler = KesintiTurleri, avansEkrani = "/IkTalep/Avans", nakitAkisEkrani = "/Rapor79" };
        }
        /// <summary>Yeni kesinti / icra planı; başlangıç dönemi yüklü bir dönemse kişinin o dönemdeki ilgili kesinti sütununa taksit yansıtılır. Hata metni döner (null = başarılı).</summary>
        public static string KesintiEkle(int personelSira, string tur, string kaynak, string icraDairesi, string alacakli, string dosyaNo, decimal toplam, decimal taksit, string baslangic, string aciklama, string kisi, (string ad, string tip, byte[] veri)? ek, out PkKesinti kayit)
        {
            kayit = null; Hazirla();
            PmPersonel p; lock (PersonelMaliyetOrnek.Kilit) p = PersonelMaliyetOrnek.Liste.FirstOrDefault(x => x.Sira == personelSira);
            if (p == null) return "Personel bulunamadı.";
            if (!KesintiTurleri.Contains(tur ?? "")) return "Kesinti türü geçersiz.";
            if (toplam <= 0) return "Toplam tutar sıfırdan büyük olmalıdır.";
            if (taksit <= 0) return "Aylık taksit sıfırdan büyük olmalıdır.";
            if (taksit > toplam) taksit = toplam;
            if (string.IsNullOrWhiteSpace(baslangic) || baslangic.Length != 7 || !int.TryParse(baslangic.Substring(5, 2), out int ay) || ay < 1 || ay > 12) return "Başlangıç dönemi AAAA-AA biçiminde olmalıdır.";
            if (tur == "İcra Kesintisi" && string.IsNullOrWhiteSpace(dosyaNo)) return "İcra kesintisinde dosya numarası zorunludur.";
            var k = new PkKesinti
            {
                PersonelSira = p.Sira, AdSoyad = p.AdSoyad, Tur = tur, Kaynak = string.IsNullOrWhiteSpace(kaynak) ? (tur == "İcra Kesintisi" ? dosyaNo : tur == "Borç Kesintisi" ? "Personel borç senedi" : "Bordro") : kaynak.Trim(),
                IcraDairesi = (icraDairesi ?? "").Trim(), Alacakli = (alacakli ?? "").Trim(), DosyaNo = (dosyaNo ?? "").Trim(), ToplamTutar = toplam, AylikTaksit = taksit, TaksitSayisi = (int)Math.Ceiling(toplam / taksit), OdenenTaksit = 0,
                BaslangicDonem = baslangic, Aciklama = (aciklama ?? "").Trim(), Olusturma = DateTime.Now, Olusturan = kisi
            };
            lock (Kilit) { k.Id = ++_sonId; if (ek.HasValue && ek.Value.veri != null && ek.Value.veri.Length > 0) k.Ekler.Add(new PkEk { No = ++_sonEk, Ad = ek.Value.ad, Tip = ek.Value.tip, Boyut = ek.Value.veri.LongLength, Tarih = DateTime.Now, Yukleyen = kisi, Veri = ek.Value.veri }); Kesintiler.Add(k); }
            KesintileriYansit(p.Sira);
            kayit = k; return null;
        }
        /// <summary>Kesinti planını düzenle (satıra tıklayınca çekmece). Değişiklik kişinin yüklü tüm dönemlerdeki ilgili kesinti sütununa yansır (sütun = o türdeki aktif planların dönem taksiti toplamı).</summary>
        public class KesintiIstek { public int Id { get; set; } public string Tur { get; set; } public string Kaynak { get; set; } public string IcraDairesi { get; set; } public string Alacakli { get; set; } public string DosyaNo { get; set; } public decimal ToplamTutar { get; set; } public decimal AylikTaksit { get; set; } public int OdenenTaksit { get; set; } public string BaslangicDonem { get; set; } public string Aciklama { get; set; } public string Donem { get; set; } }
        public static string KesintiGuncelle(KesintiIstek d, string kisi, out PkKesinti kayit)
        {
            kayit = null; Hazirla(); if (d == null) return "Veri yok.";
            if (!KesintiTurleri.Contains(d.Tur ?? "")) return "Kesinti türü geçersiz.";
            if (d.ToplamTutar <= 0) return "Toplam tutar sıfırdan büyük olmalıdır.";
            if (d.AylikTaksit <= 0) return "Aylık taksit sıfırdan büyük olmalıdır.";
            if (string.IsNullOrWhiteSpace(d.BaslangicDonem) || d.BaslangicDonem.Length != 7) return "Başlangıç dönemi AAAA-AA biçiminde olmalıdır.";
            if (d.Tur == "İcra Kesintisi" && string.IsNullOrWhiteSpace(d.DosyaNo)) return "İcra kesintisinde dosya numarası zorunludur.";
            PkKesinti k; int eskiSira;
            lock (Kilit)
            {
                k = Kesintiler.FirstOrDefault(x => x.Id == d.Id); if (k == null) return "Kesinti planı bulunamadı."; eskiSira = k.PersonelSira;
                decimal taksit = Math.Min(d.AylikTaksit, d.ToplamTutar);
                k.Tur = d.Tur; k.Kaynak = string.IsNullOrWhiteSpace(d.Kaynak) ? k.Kaynak : d.Kaynak.Trim(); k.IcraDairesi = (d.IcraDairesi ?? "").Trim(); k.Alacakli = (d.Alacakli ?? "").Trim(); k.DosyaNo = (d.DosyaNo ?? "").Trim();
                k.ToplamTutar = d.ToplamTutar; k.AylikTaksit = taksit; k.TaksitSayisi = (int)Math.Ceiling(d.ToplamTutar / taksit); k.OdenenTaksit = Math.Max(0, Math.Min(d.OdenenTaksit, k.TaksitSayisi)); k.BaslangicDonem = d.BaslangicDonem; k.Aciklama = (d.Aciklama ?? "").Trim();
                k.Olusturan = (k.Olusturan ?? "") + (k.Olusturan != null && k.Olusturan.Contains("düzenledi") ? "" : $" · {kisi} düzenledi");
            }
            KesintileriYansit(eskiSira); kayit = k; return null;
        }
        public static string KesintiSil(int id)
        {
            int sira; lock (Kilit) { var k = Kesintiler.FirstOrDefault(x => x.Id == id); if (k == null) return "Kesinti planı bulunamadı."; sira = k.PersonelSira; Kesintiler.Remove(k); }
            KesintileriYansit(sira); return null;
        }
        /// <summary>Kişinin yüklü tüm dönemlerinde kesinti sütunları = o türdeki planların dönem taksiti toplamı (plan yoksa sütun sıfırlanır).</summary>
        public static void KesintileriYansit(int personelSira)
        {
            List<PkKesinti> planlar; lock (Kilit) planlar = Kesintiler.Where(k => k.PersonelSira == personelSira).ToList();
            lock (PersonelMaliyetOrnek.Kilit)
                foreach (var kv in PersonelMaliyetOrnek.Donemler)
                    foreach (var tur in TurSutun) PersonelMaliyetOrnek.KesintiAyarla(kv.Key, personelSira, tur.Value, planlar.Where(k => k.Tur == tur.Key).Sum(k => k.DonemTaksiti(kv.Key)));
        }
        public static string EkYukle(int kesintiId, string ad, string tip, byte[] veri, string kisi)
        {
            Hazirla(); if (veri == null || veri.Length == 0) return "Dosya boş.";
            if (veri.Length > 8 * 1024 * 1024) return "Dosya 8 MB'tan büyük olamaz.";
            lock (Kilit) { var k = Kesintiler.FirstOrDefault(x => x.Id == kesintiId); if (k == null) return "Kesinti planı bulunamadı."; k.Ekler.Add(new PkEk { No = ++_sonEk, Ad = ad ?? "ek.pdf", Tip = tip ?? "application/octet-stream", Boyut = veri.LongLength, Tarih = DateTime.Now, Yukleyen = kisi, Veri = veri }); }
            return null;
        }
        public static PkEk Ek(int kesintiId, int no) { Hazirla(); lock (Kilit) return Kesintiler.FirstOrDefault(x => x.Id == kesintiId)?.Ekler.FirstOrDefault(e => e.No == no); }

        // ---------- tohum ----------
        private static int _tohumSayac;
        private static void Tohum()
        {
            List<PmPersonel> liste; lock (PersonelMaliyetOrnek.Kilit) liste = PersonelMaliyetOrnek.Liste.Where(p => p.Durum == "Aktif").OrderBy(p => p.Sira).ToList();
            string guncel = PersonelMaliyetOrnek.GuncelDonem, onceki = AyEkle(guncel, -1);
            int ay = int.Parse(onceki.Substring(5, 2)), yil = int.Parse(onceki.Substring(0, 4));
            // --- mesai talepleri: Fazla Mesai (R + X) tutarını fiili saat × saat ücreti × 1,5 ile karşılayan hafta içi talepler (önceki ay = bordro dönemi), PDKS + puantaj işlenmiş
            var projeler = new[] { "Ay sonu kapanış", "Sevkiyat yoğunluğu", "Hat 2 bakım", "İhracat yüklemesi", "Stok sayımı", "Dolum hattı arıza", "Fuar hazırlığı", "Yıl sonu planlama" };
            foreach (var p in liste)
            {
                decimal mesai = p.FazlaMesai + p.FazlaMesaiX, su = SaatUcreti(p); if (mesai <= 0 || su <= 0) continue;
                decimal saat = Math.Round(mesai / (su * 1.5m), 2); int parca = (int)Math.Ceiling(saat / 3m); if (parca < 1) parca = 1; if (parca > 8) parca = 8;
                decimal kalanSaat = saat; int gunBas = 3 + (p.Sira % 9);   // ayın farklı günleri
                for (int i = 0; i < parca; i++)
                {
                    decimal s = i == parca - 1 ? Math.Round(kalanSaat, 2) : Math.Min(3m, Math.Round(saat / parca, 2)); kalanSaat -= s; if (s <= 0) break;
                    var gun = new DateTime(yil, ay, Math.Min(DateTime.DaysInMonth(yil, ay), gunBas + i * 3)); while (gun.DayOfWeek == DayOfWeek.Saturday || gun.DayOfWeek == DayOfWeek.Sunday || IzinOrnek.TatilMi(gun)) gun = gun.AddDays(1);
                    int gunOnce = (DateTime.Today - gun).Days + 2;   // talep mesaiden 2 gün önce açılmış
                    string bitis = TimeSpan.FromHours(18 + (double)s).ToString(@"hh\:mm");
                    var d = new Dictionary<string, string> { ["tarih"] = gun.ToString("yyyy-MM-dd"), ["baslangicSaat"] = "18:00", ["bitisSaat"] = bitis, ["mola"] = "0", ["karsilik"] = "Ücret", ["proje"] = projeler[(p.Sira + i) % projeler.Length], ["gerekce"] = (p.FazlaMesaiX > 0 && p.FazlaMesai == 0 ? "Bordro dışı (X) ödenen mesai. " : "") + "Vardiya sonrası ek çalışma; iş yoğunluğu nedeniyle.", ["servis"] = "true" };
                    var t = TalepMotoru.Tohum("MESAI", p.AdSoyad, p.AdSoyad, "HAFTAICI", d, Math.Max(1, gunOnce));
                    if (t == null) continue;
                    TalepMotoru.TohumOlay(t, "PdksGiris", new Dictionary<string, string> { ["giris"] = "08:0" + (p.Sira % 10), ["cikis"] = bitis }, Math.Max(0, gunOnce - 2), 22);
                    if (!(i == parca - 1 && p.Sira % 4 == 0)) TalepMotoru.TohumOlay(t, "PuantajAktarildi", new Dictionary<string, string> { ["donem"] = onceki }, 1, 9);   // bazı son parçalar puantaj bekliyor (örnek)
                    _tohumSayac++;
                }
            }
            // bu ay açılmış, onay bekleyen birkaç talep (dinamik örnek)
            foreach (var p in liste.Where(x => x.FazlaMesai > 0).Take(3))
            {
                var d = new Dictionary<string, string> { ["tarih"] = DateTime.Today.AddDays(2 + p.Sira % 3).ToString("yyyy-MM-dd"), ["baslangicSaat"] = "18:00", ["bitisSaat"] = "20:30", ["mola"] = "0", ["karsilik"] = "Ücret", ["proje"] = "Kasım sevkiyat planı", ["gerekce"] = "Müşteri termini için ek çalışma.", ["servis"] = "true" };
                TalepMotoru.Tohum("MESAI", p.AdSoyad, p.AdSoyad, "HAFTAICI", d, 1, 0);
            }
            // --- kesintiler: Avans ≥ 10.000 → şahsi avans talebi + mahsup planı · İcra → icra dosyası (örnek PDF) · Borç → borç planı
            foreach (var p in liste)
            {
                if (p.Avans > 0)
                {
                    int taksit = 2 + p.Sira % 2; decimal toplam = Math.Round(p.Avans * taksit, 2);
                    var a = NakitAvansOrnek.SahsiAvansTohumu(NaKisi(p), toplam, "Maaş avansı (şahsi ihtiyaç)", $"{taksit} ay maaştan mahsup edilecek; aylık {p.Avans.ToString("N2", Tr)} ₺.", 40 + p.Sira % 7, 37 + p.Sira % 7);
                    Kesintiler.Add(new PkKesinti { Id = ++_sonId, PersonelSira = p.Sira, AdSoyad = p.AdSoyad, Tur = "Avans Kesintisi", Kaynak = a.No, ToplamTutar = toplam, AylikTaksit = p.Avans, TaksitSayisi = taksit, OdenenTaksit = 1, BaslangicDonem = onceki, Aciklama = $"Nakit Avans {a.No} mahsubu; kasa ödemesi {a.IlkTeslim:dd.MM.yyyy}.", Olusturma = a.Tarih, Olusturan = "Sistem (Nakit Avans)" });
                }
                if (p.Icra > 0)
                {
                    int taksit = 8; decimal toplam = Math.Round(p.Icra * taksit, 2); string dosya = $"2026/{1200 + p.Sira * 7} E.";
                    var k = new PkKesinti { Id = ++_sonId, PersonelSira = p.Sira, AdSoyad = p.AdSoyad, Tur = "İcra Kesintisi", Kaynak = dosya, DosyaNo = dosya, IcraDairesi = p.Sira % 2 == 0 ? "İstanbul Anadolu 7. İcra Dairesi" : "Çorlu 2. İcra Dairesi", Alacakli = p.Sira % 2 == 0 ? "X Bankası A.Ş." : "Y Finansman A.Ş.", ToplamTutar = toplam, AylikTaksit = p.Icra, TaksitSayisi = taksit, OdenenTaksit = 1, BaslangicDonem = onceki, Aciklama = "Maaş haczi — net ücretin 1/4'ü sınırında aylık kesinti; dosya kapanınca durur. Örnek kayıt.", Olusturma = DateTime.Today.AddDays(-45).AddHours(11), Olusturan = "Hukuk (Selçuk Arı)" };
                    k.Ekler.Add(OrnekIcraPdf(k, p));
                    Kesintiler.Add(k);
                }
                if (p.Borc > 0)
                {
                    int taksit = 2 + p.Sira % 3; decimal toplam = Math.Round(p.Borc * taksit, 2);
                    Kesintiler.Add(new PkKesinti { Id = ++_sonId, PersonelSira = p.Sira, AdSoyad = p.AdSoyad, Tur = "Borç Kesintisi", Kaynak = "Personel borç senedi", ToplamTutar = toplam, AylikTaksit = p.Borc, TaksitSayisi = taksit, OdenenTaksit = 1, BaslangicDonem = onceki, Aciklama = "Personel borç kesintisi (kaynak tablo: KESİNTİLER TOPLAMI).", Olusturma = DateTime.Today.AddDays(-35).AddHours(10), Olusturan = "İK (Dilber Güney)" });
                }
            }
        }
        /// <summary>İcra dosyası için örnek PDF (gerçek dosya yüklenene kadar kapak sayfası).</summary>
        private static PkEk OrnekIcraPdf(PkKesinti k, PmPersonel p)
        {
            var b = new DaBelge { Baslik = "İcra Dosyası — Maaş Haczi Bildirimi", AltBaslik = $"{k.DosyaNo} · {k.IcraDairesi}", Sirket = p.SgkIsvereni, Hazirlayan = "Hukuk", Yatay = false };
            b.Ozet.Add(("Borçlu", p.AdSoyad)); b.Ozet.Add(("Sicil No", p.SicilNo)); b.Ozet.Add(("Alacaklı", k.Alacakli)); b.Ozet.Add(("Toplam Borç", k.ToplamTutar.ToString("N2", Tr) + " ₺")); b.Ozet.Add(("Aylık Kesinti", k.AylikTaksit.ToString("N2", Tr) + " ₺")); b.Ozet.Add(("Başlangıç", DonemAd(k.BaslangicDonem))); b.Ozet.Add(("Tahmini Bitiş", DonemAd(k.BitisDonem)));
            b.Bloklar.Add(new DaBlok { Baslik = "Not", Not = "Ön yüz örneği: icra dairesi yazısının taranmış aslı yüklenmediği için kapak sayfası üretildi. Gerçek PDF yüklendiğinde bu ek onunla birlikte listelenir." });
            var veri = DisaAktar.Pdf(b);
            return new PkEk { No = ++_sonEk, Ad = $"icra-{k.DosyaNo.Replace("/", "-").Replace(" E.", "")}.pdf", Tip = "application/pdf", Boyut = veri.LongLength, Tarih = DateTime.Today.AddDays(-45), Yukleyen = "Hukuk (Selçuk Arı)", Veri = veri };
        }
    }
}
