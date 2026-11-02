// ============================================================
// PERSONEL MALİYET RAPORU — ek sekmeler (Üretim Personeli · Prim Ödemeleri). Özel Kişiler sekmesi ayrı liste değildir: PmPersonel.Ozel = true satırlar aynı bordro modeliyle
//   Personel Maliyet Tablosu'nun yapısında gösterilir (PersonelMaliyetOrnek). Birleşik Tablo ekranda Bordro (Ozel = false) + Özel (Ozel = true) + Üretim satırlarından kurulur.
//   Üretim Personeli : gündelikçi üretim personeli ("Gündelikçi Ücretleri 2026 - 20.10.2026"): günlük ücret × çalışılan gün = hak ediş; fazla mesai saat × (günlük / 7,5) × 1,5; tatil günü × günlük × 2;
//                      yemek / yol / diğer sosyal yardım eklenir. SGK'lı gündelikçi (ana tabloda bordrolu): bordro Net-R mahsup edilir, kalan fark elden nakit X olarak ödenir ve kişinin ana tablodaki
//                      X-Net'ine yazılır (R kısımları aynen kalır; UretimYansit her veri okumasında). SGK'sız gündelikçi: tamamı elden X, Birleşik Tablo'ya "Üretim" satırı olarak girer.
//   Prim Ödemeleri   : Personel Maliyet Tablosu'ndaki Prim-X / Prim-R tutarlarının detayı. Dönem (ay) · tür (Ciro Primi, Tahsilat Primi, Hedef Primi, Özel Ödeme) · baz (ciro / tahsilat) × oran ya da sabit tutar.
//                      Ekim 2026 satırlarının kişi toplamı ana tablodaki Prim-X ile birebir (Berkan Özkan 66.793,28 · Erdoğan Polat 139.037,06 · Müjgan Yetgin 50.308,64 · Serhat Mutlu 41.397,86 · Sevgi Kaya 14.906 · Yusuf Öztürk 48.120,57).
//   Sütun tanımları PmSutun ile (tip: sira | metin | ad | sayi | hesap | genel | not); ekran bu tanımdan kurulur, Dto yansımayla aynı adlı özellikleri okur.
// Ekran: Controllers/PersonelMaliyetController.cs (Paket) + Views/PersonelMaliyet/Index.cshtml + wwwroot/js/personel-maliyet.js (ek sekmeler bölümü)
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace WebApplication3.Data
{
    /// <summary>Gündelikçi üretim personeli — günlük ücret × çalışılan gün. SGK'lı olanlar (PersonelSira ile Personel Maliyet Tablosu'na bağlı) bordro R netini mahsup eder,
    /// kalan fark elden nakit X olarak ödenir ve kişinin ana tablodaki X-Net'ine yazılır; SGK'sız olanların tamamı elden X'tir. Tüm tutarlar aylık ₺.</summary>
    public class UretimPersonel
    {
        public int Sira { get; set; }
        public string FiiliGorevYeri { get; set; } public string AdSoyad { get; set; } public string Gorev { get; set; } public string Vardiya { get; set; }
        /// <summary>Bordrolu (SGK'lı) ise Personel Maliyet Tablosu'ndaki sıra; 0 = SGK'sız gündelikçi.</summary>
        public int PersonelSira { get; set; }
        public decimal GunlukUcret { get; set; } public decimal CalisilanGun { get; set; } public decimal GunlukSaat { get; set; } = 7.5m;
        public decimal FazlaMesaiSaat { get; set; } public decimal TatilGun { get; set; }
        public decimal Yemek { get; set; } public decimal Yol { get; set; } public decimal DigerSosyal { get; set; } public decimal Kesinti { get; set; }
        public string Not { get; set; } = "";
        /// <summary>Bordrodan bankaya yatan R net (Net-R Bordro); UretimYansit ile ana tablodan okunur.</summary>
        public decimal RMahsup { get; set; }
        // ---- hesaplanan
        public bool SgkLi => PersonelSira > 0;
        public string SgkDurumu => SgkLi ? "SGK'lı (R mahsup)" : "SGK'sız (tamamı X)";
        public decimal SaatUcreti => GunlukSaat > 0 ? Math.Round(GunlukUcret / GunlukSaat, 2) : 0;
        public decimal HakEdis => BordroParametre.Yuvarla(GunlukUcret * CalisilanGun);
        public decimal FazlaMesaiKazanci => BordroParametre.Yuvarla(FazlaMesaiSaat * SaatUcreti * 1.5m);
        public decimal TatilKazanci => BordroParametre.Yuvarla(TatilGun * GunlukUcret * 2m);
        public decimal ToplamKazanc => HakEdis + FazlaMesaiKazanci + TatilKazanci;
        public decimal ToplamSosyal => Yemek + Yol + DigerSosyal;
        public decimal ToplamHakedis => ToplamKazanc + ToplamSosyal;
        public decimal EldenX => Math.Max(0, BordroParametre.Yuvarla(ToplamHakedis - RMahsup));
        public decimal NetOdenen => EldenX - Kesinti;
        /// <summary>Birleşik tabloya giren ek maliyet: SGK'sız → tamamı; SGK'lı → yalnız elden X (R ve X zaten kişinin bordro satırında).</summary>
        public decimal ToplamMaliyet => SgkLi ? 0 : ToplamHakedis;
    }

    /// <summary>Prim ödemesi satırı — Personel Maliyet Tablosu'ndaki prim tutarının detayı.</summary>
    public class PrimOdeme
    {
        public int Sira { get; set; }
        public string Donem { get; set; }          // 2026-10
        public string DonemAd { get; set; }        // Ekim 2026
        public string AdSoyad { get; set; } public string SgkIsvereni { get; set; } public string Gorev { get; set; }
        public string PrimTuru { get; set; }       // Ciro Primi | Tahsilat Primi | Hedef Primi | Özel Ödeme
        public decimal Baz { get; set; }           // ciro / tahsilat tutarı (₺)
        public decimal Oran { get; set; }          // %
        public decimal SabitTutar { get; set; }    // oran yerine sabit tutar (hedef primi, özel ödeme)
        public string Odeme { get; set; } = "X";   // X (bordro dışı) | R (bordrolu)
        public string OdemeAyi { get; set; }       // Kasım 2026
        public string OdemeDonem { get; set; }     // 2026-11 — primin bordroya girdiği dönem (Personel Maliyet Tablosu dönemi)
        public string Aciklama { get; set; } = "";
        public decimal HesaplananPrim => SabitTutar > 0 ? SabitTutar : BordroParametre.Yuvarla(Baz * Oran / 100m);
    }

    public static class PersonelMaliyetEkOrnek
    {
        private static PmSutun S(string ad, string anahtar, string tip) => new PmSutun { Ad = ad, Anahtar = anahtar, Tip = tip };
        private static object Paket(PmSutun[] l) => l.Select(s => new { ad = s.Ad, anahtar = s.Anahtar, tip = s.Tip, liste = s.Liste });

        public static readonly PmSutun[] UretimSutunlar =
        {
            S("Sıra", "sira", "sira"), S("Fiili Görev Yeri", "fiiliGorevYeri", "metin"), S("Adı-Soyadı", "adSoyad", "ad"), S("Görev", "gorev", "metin"), S("Vardiya", "vardiya", "metin"), S("SGK Durumu", "sgkDurumu", "metin"),
            S("Günlük Ücret", "gunlukUcret", "sayi"), S("Çalışılan Gün", "calisilanGun", "gun"), S("Hak Ediş (Gün × Ücret)", "hakEdis", "hesap"), S("Günlük Saat", "gunlukSaat", "gun"), S("Saat Ücreti", "saatUcreti", "hesap"),
            S("Fazla Mesai (Saat)", "fazlaMesaiSaat", "gun"), S("Fazla Mesai Kazancı (×1,5)", "fazlaMesaiKazanci", "hesap"), S("Tatil (Gün)", "tatilGun", "gun"), S("Tatil Kazancı (×2)", "tatilKazanci", "hesap"), S("Toplam Kazanç", "toplamKazanc", "hesap"),
            S("Yemek", "yemek", "sayi"), S("Yol", "yol", "sayi"), S("Diğer Sosyal Yardım", "digerSosyal", "sayi"), S("Toplam Sosyal Yardımlar", "toplamSosyal", "hesap"), S("Toplam Hak Ediş", "toplamHakedis", "hesap"),
            S("R Mahsup (Bordro Net-R)", "rMahsup", "hesap"), S("Elden Ödenecek (X)", "eldenX", "genel"), S("Avans / Kesinti", "kesinti", "sayi"), S("Net Ödenen (Elden)", "netOdenen", "hesap"), S("Not", "not", "not")
        };
        public static readonly PmSutun[] PrimSutunlar =
        {
            S("Sıra", "sira", "sira"), S("Dönem", "donemAd", "metin"), S("Adı-Soyadı", "adSoyad", "ad"), S("SGK İşvereni", "sgkIsvereni", "metin"), S("Görev", "gorev", "metin"), S("Prim Türü", "primTuru", "metin"),
            S("Baz (Ciro / Tahsilat)", "baz", "sayi"), S("Oran %", "oran", "gun"), S("Sabit Tutar", "sabitTutar", "sayi"), S("Hesaplanan Prim", "hesaplananPrim", "genel"), S("Ödeme (R / X)", "odeme", "metin"), S("Ödeme Ayı", "odemeAyi", "metin"), S("Açıklama", "aciklama", "not")
        };
        public static object SutunPaketi => new { uretim = Paket(UretimSutunlar), prim = Paket(PrimSutunlar), vardiyalar = Vardiyalar, primTurleri = PrimTurleri };

        // ---- Dto: sütun anahtarı → aynı adlı özellik (yansıma)
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> Ozellikler = new Dictionary<Type, Dictionary<string, PropertyInfo>>();
        private static Dictionary<string, object> Dto(object o, PmSutun[] sutunlar)
        {
            var t = o.GetType();
            if (!Ozellikler.TryGetValue(t, out var oz)) Ozellikler[t] = oz = t.GetProperties().ToDictionary(p => char.ToLowerInvariant(p.Name[0]) + p.Name.Substring(1), p => p);
            var d = new Dictionary<string, object>();
            foreach (var s in sutunlar) if (oz.TryGetValue(s.Anahtar, out var p)) d[s.Anahtar] = p.GetValue(o);
            foreach (var p in oz) if (!d.ContainsKey(p.Key)) d[p.Key] = p.Value.GetValue(o);   // sütunu olmayan alanlar da (donem, dovizli …)
            return d;
        }
        public static List<Dictionary<string, object>> UretimDto() { lock (Kilit) return Uretim.Select(u => Dto(u, UretimSutunlar)).ToList(); }
        public static List<Dictionary<string, object>> PrimDto() { lock (Kilit) return Primler.Select(p => Dto(p, PrimSutunlar)).ToList(); }

        // ---------- düzenleme (satıra tıklayınca çekmece): üretim personeli ve prim satırları; değişiklik bordro satırına, karta ve cariye yansır ----------
        public static readonly object Kilit = new object();
        public static readonly string[] Vardiyalar = { "Gündüz", "Akşam", "Gece" };
        public static readonly string[] PrimTurleri = { "Ciro Primi", "Tahsilat Primi", "Hedef Primi", "Özel Ödeme" };
        public class UretimIstek { public int Sira { get; set; } public string FiiliGorevYeri { get; set; } public string AdSoyad { get; set; } public string Gorev { get; set; } public string Vardiya { get; set; } public int PersonelSira { get; set; } public decimal GunlukUcret { get; set; } public decimal CalisilanGun { get; set; } public decimal GunlukSaat { get; set; } = 7.5m; public decimal FazlaMesaiSaat { get; set; } public decimal TatilGun { get; set; } public decimal Yemek { get; set; } public decimal Yol { get; set; } public decimal DigerSosyal { get; set; } public decimal Kesinti { get; set; } public string Not { get; set; } public string Donem { get; set; } }
        public class PrimIstek { public int Sira { get; set; } public string Donem { get; set; } public string AdSoyad { get; set; } public string PrimTuru { get; set; } public decimal Baz { get; set; } public decimal Oran { get; set; } public decimal SabitTutar { get; set; } public string Odeme { get; set; } public string Aciklama { get; set; } public string BordroDonem { get; set; } }
        public static string UretimKaydet(UretimIstek d, out UretimPersonel kayit)
        {
            kayit = null; if (d == null) return "Veri yok.";
            if (string.IsNullOrWhiteSpace(d.AdSoyad)) return "Adı-Soyadı zorunludur.";
            if (string.IsNullOrWhiteSpace(d.FiiliGorevYeri)) return "Fiili Görev Yeri zorunludur.";
            if (d.GunlukUcret < 0) return "Günlük ücret negatif olamaz.";
            if (d.CalisilanGun < 0 || d.CalisilanGun > 31) return "Çalışılan gün 0–31 arasında olmalıdır.";
            if (d.GunlukSaat < 0 || d.GunlukSaat > 12) return "Günlük saat 0–12 arasında olmalıdır.";
            if (d.FazlaMesaiSaat < 0 || d.TatilGun < 0 || d.Yemek < 0 || d.Yol < 0 || d.DigerSosyal < 0 || d.Kesinti < 0) return "Tutarlar, günler ve saatler negatif olamaz.";
            if (d.PersonelSira > 0) { bool var; lock (PersonelMaliyetOrnek.Kilit) var = PersonelMaliyetOrnek.Liste.Any(p => p.Sira == d.PersonelSira); if (!var) return "Bağlanacak bordrolu personel bulunamadı."; }
            int eskiPersonel = 0;
            lock (Kilit)
            {
                var u = d.Sira > 0 ? Uretim.FirstOrDefault(x => x.Sira == d.Sira) : null;
                if (d.Sira > 0 && u == null) return "Üretim personeli bulunamadı.";
                if (u == null) { u = new UretimPersonel { Sira = ++_u }; Uretim.Add(u); } else eskiPersonel = u.PersonelSira;
                u.FiiliGorevYeri = d.FiiliGorevYeri.Trim(); u.AdSoyad = d.AdSoyad.Trim(); u.Gorev = (d.Gorev ?? "").Trim(); u.Vardiya = string.IsNullOrWhiteSpace(d.Vardiya) ? "Gündüz" : d.Vardiya.Trim(); u.PersonelSira = Math.Max(0, d.PersonelSira);
                u.GunlukUcret = d.GunlukUcret; u.CalisilanGun = d.CalisilanGun; u.GunlukSaat = d.GunlukSaat; u.FazlaMesaiSaat = d.FazlaMesaiSaat; u.TatilGun = d.TatilGun; u.Yemek = d.Yemek; u.Yol = d.Yol; u.DigerSosyal = d.DigerSosyal; u.Kesinti = d.Kesinti; u.Not = (d.Not ?? "").Trim();
                kayit = u;
            }
            if (eskiPersonel > 0 && eskiPersonel != kayit.PersonelSira) XSifirla(eskiPersonel);   // bağ koptu: eski kişinin elden X'i sıfırlanır
            UretimYansit();
            return null;
        }
        public static string UretimSil(int sira) { int ps; lock (Kilit) { var u = Uretim.FirstOrDefault(x => x.Sira == sira); if (u == null) return "Üretim personeli bulunamadı."; ps = u.PersonelSira; Uretim.Remove(u); } if (ps > 0) XSifirla(ps); return null; }
        private static void XSifirla(int personelSira) { lock (PersonelMaliyetOrnek.Kilit) { var p = PersonelMaliyetOrnek.Liste.FirstOrDefault(x => x.Sira == personelSira); if (p != null && !p.Dovizli) p.XNet = 0; } }
        private static readonly string[] AylarTr = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
        /// <summary>Prim satırı ekle / düzenle. Hesaplanan prim = Baz × Oran / 100 ya da sabit tutar. Kaydedilince kişinin ilgili bordro dönemindeki Prim-X / Prim (R) tutarı prim satırlarının toplamından yeniden yazılır.</summary>
        public static string PrimKaydet(PrimIstek d, out PrimOdeme kayit)
        {
            kayit = null; if (d == null) return "Veri yok.";
            if (string.IsNullOrWhiteSpace(d.Donem) || d.Donem.Length != 7 || !int.TryParse(d.Donem.Substring(0, 4), out int y) || !int.TryParse(d.Donem.Substring(5, 2), out int m) || m < 1 || m > 12) return "Dönem AAAA-AA biçiminde olmalıdır.";
            if (string.IsNullOrWhiteSpace(d.AdSoyad)) return "Personel zorunludur.";
            if (!PrimTurleri.Contains(d.PrimTuru ?? "")) return "Prim türü geçersiz.";
            if (d.Odeme != "X" && d.Odeme != "R") return "Ödeme X ya da R olmalıdır.";
            if (d.Baz < 0 || d.Oran < 0 || d.SabitTutar < 0) return "Baz, oran ve sabit tutar negatif olamaz.";
            if (d.SabitTutar <= 0 && (d.Baz <= 0 || d.Oran <= 0)) return "Baz ve oran ya da sabit tutar girilmelidir.";
            PmPersonel p; lock (PersonelMaliyetOrnek.Kilit) p = PersonelMaliyetOrnek.Liste.FirstOrDefault(x => string.Equals(x.AdSoyad, d.AdSoyad.Trim(), StringComparison.OrdinalIgnoreCase) && x.Durum == "Aktif") ?? PersonelMaliyetOrnek.Liste.FirstOrDefault(x => string.Equals(x.AdSoyad, d.AdSoyad.Trim(), StringComparison.OrdinalIgnoreCase));
            if (p == null) return $"{d.AdSoyad} Personel Maliyet Tablosu'nda yok.";
            int om = m == 12 ? 1 : m + 1, oy = m == 12 ? y + 1 : y; string eskiAd = null, eskiOdemeDonem = null;
            lock (Kilit)
            {
                var r = d.Sira > 0 ? Primler.FirstOrDefault(x => x.Sira == d.Sira) : null;
                if (d.Sira > 0 && r == null) return "Prim satırı bulunamadı.";
                if (r == null) { r = new PrimOdeme { Sira = ++_p }; Primler.Add(r); } else { eskiAd = r.AdSoyad; eskiOdemeDonem = r.OdemeDonem; }
                r.Donem = d.Donem; r.DonemAd = AylarTr[m - 1] + " " + y; r.OdemeAyi = AylarTr[om - 1] + " " + oy; r.OdemeDonem = $"{oy}-{om:00}";
                r.AdSoyad = p.AdSoyad; r.SgkIsvereni = p.SgkIsvereni; r.Gorev = p.Gorev; r.PrimTuru = d.PrimTuru; r.Baz = d.Baz; r.Oran = d.Oran; r.SabitTutar = d.SabitTutar; r.Odeme = d.Odeme; r.Aciklama = (d.Aciklama ?? "").Trim();
                kayit = r;
            }
            PrimYansit(p.AdSoyad, kayit.OdemeDonem); if (eskiAd != null && (eskiAd != p.AdSoyad || eskiOdemeDonem != kayit.OdemeDonem)) PrimYansit(eskiAd, eskiOdemeDonem);
            return null;
        }
        public static string PrimSil(int sira)
        {
            PrimOdeme r; lock (Kilit) { r = Primler.FirstOrDefault(x => x.Sira == sira); if (r == null) return "Prim satırı bulunamadı."; Primler.Remove(r); }
            PrimYansit(r.AdSoyad, r.OdemeDonem); return null;
        }
        /// <summary>Kişinin bordro dönemindeki Prim-X ve Prim (R) = o döneme giren prim satırlarının toplamı (prim bir ay sonraki bordroya girer). R primi brüte girdiğinden kaynak değerler düşer, model yeniden hesaplar.</summary>
        public static void PrimYansit(string adSoyad, string bordroDonem)
        {
            decimal x, r; lock (Kilit) { var l = Primler.Where(t => t.AdSoyad == adSoyad && t.OdemeDonem == bordroDonem).ToList(); x = l.Where(t => t.Odeme == "X").Sum(t => t.HesaplananPrim); r = l.Where(t => t.Odeme == "R").Sum(t => t.HesaplananPrim); }
            lock (PersonelMaliyetOrnek.Kilit)
            {
                var liste = PersonelMaliyetOrnek.ListeDonem(bordroDonem); if (liste == null) return;
                foreach (var p in liste.Where(q => q.AdSoyad == adSoyad && q.Durum == "Aktif")) { p.PrimX = x; if (p.Prim != r) { p.Prim = r; p.BrutKaynak = null; p.IsverenKaynak = null; } }
            }
        }

        // ---- örnek veri
        private const string COR = "ALV Kimya Çorlu", URS = "Uras Kimya";
        private static int _u, _p;
        /// <summary>Gündelik Ücretleri 2026 (20.10.2026) listesinden: günlük ücret; çalışılan gün varsayılan 26 (listede yok — sekmeden düzeltilir). sgkli = true → Personel Maliyet Tablosu'ndaki aynı adlı kişiye bağlanır (R mahsup).</summary>
        private static UretimPersonel U(string yer, string ad, decimal gunluk, bool sgkli = false, decimal gun = 26, string gorev = "Üretim Personeli", string vardiya = "Gündüz")
            => new UretimPersonel { Sira = ++_u, FiiliGorevYeri = yer, AdSoyad = ad, Gorev = gorev, Vardiya = vardiya, GunlukUcret = gunluk, CalisilanGun = gun, PersonelSira = sgkli ? -1 : 0, Not = sgkli ? "SGK'lı gündelikçi: bordro R neti mahsup edilir, fark elden X" : "SGK'sız gündelikçi: tamamı elden X" };
        /// <summary>"Gündelikçi Ücretleri 2026 - 20.10.2026" (kullanıcı PDF'i): Uras Kimya 17, Alv Kimya Çorlu 5 kişi. Adı tam verilen 9 kişi SGK'lı (ana tabloda bordrolu).</summary>
        public static readonly List<UretimPersonel> Uretim = new List<UretimPersonel>
        {
            U(URS, "Muharrem", 2200), U(URS, "Ali Askerov", 2200), U(URS, "Mehman Babayev", 2500, true),
            U(URS, "Azad Shirinov", 2200, true), U(URS, "Ramin Musayev", 2500, true), U(URS, "Rahman Babazade", 2600, true),
            U(URS, "Elmin Jabbarov", 3500, true), U(URS, "Tural Abbasov", 2400, true), U(URS, "Savalan", 2300),
            U(URS, "Sinan", 2200), U(URS, "Elman Abbaslı", 2200, true), U(URS, "Murad Hudaybergenov", 2200, true),
            U(URS, "Kamuran", 2400), U(URS, "Araz", 2200), U(URS, "Ayhan", 2300),
            U(URS, "Elşad", 2200), U(URS, "Çoşkun", 2200),
            U(COR, "Reşat", 2000), U(COR, "Aslan Huseynov", 2050, true), U(COR, "Ali", 2000), U(COR, "Naim", 2000), U(COR, "Ruslan", 2100)
        };
        /// <summary>SGK'lı gündelikçileri Personel Maliyet Tablosu'na bağlar (ad eşleşmesi), bordro Net-R'yi mahsup olarak okur ve elden X farkını kişinin güncel dönem X-Net'ine yazar. Her veri okumasında çalışır (dinamik).</summary>
        public static void UretimYansit()
        {
            lock (PersonelMaliyetOrnek.Kilit)
            {
                var liste = PersonelMaliyetOrnek.Liste;
                lock (Kilit)
                    foreach (var u in Uretim)
                    {
                        if (u.PersonelSira == -1) { var e = liste.FirstOrDefault(p => string.Equals(p.AdSoyad, u.AdSoyad, StringComparison.OrdinalIgnoreCase)); u.PersonelSira = e?.Sira ?? 0; }
                        if (u.PersonelSira <= 0) { u.RMahsup = 0; continue; }
                        var p = liste.FirstOrDefault(x => x.Sira == u.PersonelSira); if (p == null) { u.PersonelSira = 0; u.RMahsup = 0; continue; }
                        u.RMahsup = p.Hesap.NetRBordro;
                        if (!p.Dovizli) p.XNet = u.EldenX;   // elden ödenen fark → X-Net (R kısmı aynen kalır; X brüte girmez)
                    }
            }
        }

        private static PrimOdeme Pr(string donem, string ad, string sgk, string gorev, string tur, decimal baz = 0, decimal oran = 0, decimal sabit = 0, string aciklama = "")
        {
            int y = int.Parse(donem.Substring(0, 4)), m = int.Parse(donem.Substring(5, 2));
            string[] aylar = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            int om = m == 12 ? 1 : m + 1, oy = m == 12 ? y + 1 : y;
            return new PrimOdeme { Sira = ++_p, Donem = donem, DonemAd = aylar[m - 1] + " " + y, AdSoyad = ad, SgkIsvereni = sgk, Gorev = gorev, PrimTuru = tur, Baz = baz, Oran = oran, SabitTutar = sabit, Odeme = "X", OdemeAyi = aylar[om - 1] + " " + oy, OdemeDonem = $"{oy}-{om:00}", Aciklama = aciklama };
        }
        /// <summary>Ekim 2026 satırları ana tablodaki Prim-X ile birebir; Ağustos / Eylül geçmiş dönem örneği.</summary>
        public static readonly List<PrimOdeme> Primler = new List<PrimOdeme>
        {
            Pr("2026-08", "Berkan Özkan", "ALV Kimya", "İhracat Müdürü", "Ciro Primi", 1980500, 3, aciklama: "İhracat cirosu %3"),
            Pr("2026-09", "Berkan Özkan", "ALV Kimya", "İhracat Müdürü", "Ciro Primi", 2104300, 3, aciklama: "İhracat cirosu %3"),
            Pr("2026-10", "Berkan Özkan", "ALV Kimya", "İhracat Müdürü", "Ciro Primi", 2226442.67m, 3, aciklama: "İhracat cirosu %3"),
            Pr("2026-08", "Erdoğan Polat", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Ciro Primi", 4120000, 2, aciklama: "Emprime iç piyasa cirosu %2"),
            Pr("2026-08", "Erdoğan Polat", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Tahsilat Primi", 2950000, 1.5m, aciklama: "Vadesinde tahsilat %1,5"),
            Pr("2026-09", "Erdoğan Polat", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Ciro Primi", 4380000, 2, aciklama: "Emprime iç piyasa cirosu %2"),
            Pr("2026-09", "Erdoğan Polat", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Tahsilat Primi", 3105000, 1.5m, aciklama: "Vadesinde tahsilat %1,5"),
            Pr("2026-10", "Erdoğan Polat", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Ciro Primi", 4500000, 2, aciklama: "Emprime iç piyasa cirosu %2"),
            Pr("2026-10", "Erdoğan Polat", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Tahsilat Primi", 3269137.33m, 1.5m, aciklama: "Vadesinde tahsilat %1,5"),
            Pr("2026-08", "Müjgan Yetgin", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Tahsilat Primi", 2210000, 2, aciklama: "Tahsilat %2"),
            Pr("2026-09", "Müjgan Yetgin", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Tahsilat Primi", 2385000, 2, aciklama: "Tahsilat %2"),
            Pr("2026-10", "Müjgan Yetgin", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Tahsilat Primi", 2515432, 2, aciklama: "Tahsilat %2"),
            Pr("2026-08", "Serhat Mutlu", "Avrupa Paper", "İç Piyasa Satış Uzmanı – Dijital", "Ciro Primi", 1480000, 2.5m, aciklama: "Dijital baskı cirosu %2,5"),
            Pr("2026-09", "Serhat Mutlu", "Avrupa Paper", "İç Piyasa Satış Uzmanı – Dijital", "Ciro Primi", 1562000, 2.5m, aciklama: "Dijital baskı cirosu %2,5"),
            Pr("2026-10", "Serhat Mutlu", "Avrupa Paper", "İç Piyasa Satış Uzmanı – Dijital", "Ciro Primi", 1655914.40m, 2.5m, aciklama: "Dijital baskı cirosu %2,5"),
            Pr("2026-08", "Sevgi Kaya", "Avrupa Paper", "İhracat Müdürü", "Hedef Primi", sabit: 14906, aciklama: "Aylık ihracat hedefi sabit prim"),
            Pr("2026-09", "Sevgi Kaya", "Avrupa Paper", "İhracat Müdürü", "Hedef Primi", sabit: 14906, aciklama: "Aylık ihracat hedefi sabit prim"),
            Pr("2026-10", "Sevgi Kaya", "Avrupa Paper", "İhracat Müdürü", "Hedef Primi", sabit: 14906, aciklama: "Aylık ihracat hedefi sabit prim"),
            Pr("2026-08", "Yusuf Öztürk", "ALV Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Ciro Primi", 1395000, 3, aciklama: "Emprime cirosu %3"),
            Pr("2026-09", "Yusuf Öztürk", "ALV Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Ciro Primi", 1512500, 3, aciklama: "Emprime cirosu %3"),
            Pr("2026-10", "Yusuf Öztürk", "ALV Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Ciro Primi", 1604019, 3, aciklama: "Emprime cirosu %3")
        };
    }
}
