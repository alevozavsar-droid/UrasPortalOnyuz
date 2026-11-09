// ============================================================
// PERSONEL MALİYET RAPORU — ek sekmeler (Üretim Personeli · Prim Ödemeleri). Özel Kişiler sekmesi ayrı liste değildir: PmPersonel.Ozel = true satırlar aynı bordro modeliyle
//   Personel Maliyet Tablosu'nun yapısında gösterilir (PersonelMaliyetOrnek). Birleşik Tablo ekranda Bordro (Ozel = false) + Özel (Ozel = true) + Üretim satırlarından kurulur.
//   Üretim Personeli : gündelikçi üretim personeli ("Gündelikçi Ücretleri 2026 – 09.11.2026" Excel'i; adlar bu listedeki gibi, 9 Kas 2026): günlük ücret × çalışılan gün = hak ediş; fazla mesai saat × (günlük / 7,5) × 1,5; tatil günü × günlük × 2;
//                      SGK'sız gündelikçinin de Konsolide İK Paneli'nde kişi kaydı vardır (9 Kas 2026, "bu kişilerin de hepsinin personel carileri ve dosyaları olsun"): bordro dışı kayıt (SgkDetayYok, Özel Durum Gündelikçi, D30, cari kodu 335.901…;
//                      GundelikciKisileriTamamla) → Personel Carileri, Personel Dosyası, kişi kartı, Ödeme Planı; tutarları UretimYansit her okumada Üretim satırından yazar (günlük × gün + tatil + diğer → X-Net, mesai → Fazla Mesai-X, yemek / yol / kira → X).
//                      yemek / yol / kira / diğer sosyal yardım eklenir. SGK'lı gündelikçi (ana tabloda bordrolu): bordro Net-R mahsup edilir, kalan fark elden nakit X olarak ödenir ve kişinin ana
//                      tablodaki X-Net'ine yazılır (MahsupFarki; 9 Kas 2026 kullanıcı onayı, önce kaynak v5_2 gereği 0'dı); R kısımları resmi bordrodan gelir ve sabittir; UretimYansit her veri okumasında. TEK KAYNAK (9 Kas 2026, "tüm sekmeler senkronize olmalı … Kamil Mahmudov yol
//                      ücreti sadece bir tarafta yazıyor"): SGK'lı gündelikçinin yemek, yol, kira ve fazla mesaisi seçili dönemin bordro kaydından okunur (R + X; BordroKalemleri); Üretim satırından
//                      girilen yemek / yol / kira toplamı bordro kaydına yazılır (R aynen kalır, fazlası X; R'nin altı hata) → Personel Maliyet Tablosu, kişi kartı ve Üretim aynı değeri gösterir.
//                      SGK'sız gündelikçi: tamamı elden X, Birleşik Tablo'ya "Üretim" satırı olarak girer.
//   Prim Ödemeleri   : Personel Maliyet Tablosu'ndaki Prim-X / Prim-R tutarlarının detayı. Dönem (ay) · tür (Ciro Primi, Tahsilat Primi, Hedef Primi, Özel Ödeme) · baz (ciro / tahsilat) × oran ya da sabit tutar.
//                      Eylül 2026 satırlarının kişi toplamı ana tablodaki Prim-X ile birebir (Berkan Özkan 66.793,28 · Erdoğan Polat 139.037,06 · Müjgan Yetgin 50.308,64 · Serhat Mutlu 41.397,86 · Sevgi Kaya 14.906 · Yusuf Öztürk 48.120,57).
//   Sütun tanımları PmSutun ile (tip: sira | metin | ad | sayi | hesap | genel | not); ekran bu tanımdan kurulur, Dto yansımayla aynı adlı özellikleri okur.
// Ekran: Controllers/PersonelMaliyetController.cs (Paket) + Views/PersonelMaliyet/Index.cshtml + wwwroot/js/personel-maliyet.js (ek sekmeler bölümü)
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
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
        /// <summary>Konsolide İK Paneli'ndeki kişi kaydının sırası (cari kodu 335.xxx): bordrolu (SGK'lı) gündelikçide bordro satırı, SGK'sız gündelikçide bordro dışı kayıt (335.9xx; 9 Kas 2026). 0 = kişi kaydı henüz yok, -1 = tohumda ada göre bağlanacak.</summary>
        public int PersonelSira { get; set; }
        /// <summary>Bağlı kişi kaydı (en yeni dönemde) bordro dışı (SgkDetayYok): SGK'sız gündelikçi — tamamı elden X, R mahsubu yok. UretimYansit yazar.</summary>
        public bool BordroDisi { get; set; }
        public decimal GunlukUcret { get; set; } public decimal CalisilanGun { get; set; } public decimal GunlukSaat { get; set; } = 7.5m;
        public decimal FazlaMesaiSaat { get; set; } public decimal TatilGun { get; set; }
        /// <summary>Yemek / Yol: SGK'sız gündelikçide satırdan girilir; SGK'lı gündelikçide tek kaynak bordro kaydıdır (R + X; UretimYansit güncel dönemden yazar, UretimDto seçili dönemden okur), satırdan girilen toplam bordro kaydına X olarak yazılır (R sabit).</summary>
        public decimal Yemek { get; set; } public decimal Yol { get; set; } public decimal DigerSosyal { get; set; } public decimal Kesinti { get; set; }
        /// <summary>Kira (X, 9 Kas 2026): SGK'sız gündelikçide satırdan girilir ve toplam hak edişe girer; SGK'lı gündelikçide bordro kaydındaki Kira Desteği (R + X)'ten gelir (UretimYansit / UretimDto), R mahsubuyla netleştirilmez, Elden Ödenecek (X)'e eklenir (Kamil Mahmudov 23.520, Elmin Jabbarov 25.400 ₺).</summary>
        public decimal Kira { get; set; }
        public string Not { get; set; } = "";
        /// <summary>Bordrodan bankaya yatan R net (Net-R Bordro); UretimYansit ile ana tablodan okunur.</summary>
        public decimal RMahsup { get; set; }
        // ---- hesaplanan
        public bool SgkLi => PersonelSira > 0 && !BordroDisi;
        public string SgkDurumu => SgkLi ? "SGK'lı (R mahsup)" : "SGK'sız (tamamı X)";
        public decimal SaatUcreti => GunlukSaat > 0 ? Math.Round(GunlukUcret / GunlukSaat, 2) : 0;
        public decimal HakEdis => BordroParametre.Yuvarla(GunlukUcret * CalisilanGun);
        public decimal FazlaMesaiKazanci => BordroParametre.Yuvarla(FazlaMesaiSaat * SaatUcreti * 1.5m);
        public decimal TatilKazanci => BordroParametre.Yuvarla(TatilGun * GunlukUcret * 2m);
        public decimal ToplamKazanc => HakEdis + FazlaMesaiKazanci + TatilKazanci;
        public decimal ToplamSosyal => Yemek + Yol + Kira + DigerSosyal;
        public decimal ToplamHakedis => ToplamKazanc + ToplamSosyal;
        public decimal EldenX => SgkLi ? 0m : Math.Max(0, BordroParametre.Yuvarla(ToplamHakedis - RMahsup));   // SGK'lı: UretimDto hesaplar (kira mahsup dışı)
        public decimal NetOdenen => EldenX - Kesinti;
        /// <summary>Birleşik tabloya giren ek maliyet: SGK'sız → tamamı; SGK'lı → yalnız elden X (R ve X zaten kişinin bordro satırında).</summary>
        public decimal ToplamMaliyet => SgkLi ? 0 : ToplamHakedis;
        /// <summary>SGK'sız gündelikçinin SGK işvereni ve X ödeme şirketi: çalıştığı şirket (Uras Kimya / ALV Kimya); eşleşmezse boş.</summary>
        public string FiiliSirket => PersonelMuhasebeOrnek.FiiliSirketAdi(FiiliGorevYeri);
    }

    /// <summary>Prim ödemesi satırı — Personel Maliyet Tablosu'ndaki prim tutarının detayı.</summary>
    public class PrimOdeme
    {
        public int Sira { get; set; }
        public string Donem { get; set; }          // 2026-09
        public string DonemAd { get; set; }        // Eylül 2026
        public string AdSoyad { get; set; } public string SgkIsvereni { get; set; } public string Gorev { get; set; }
        public string PrimTuru { get; set; }       // Ciro Primi | Tahsilat Primi | Hedef Primi | Özel Ödeme
        public decimal Baz { get; set; }           // ciro / tahsilat tutarı (₺)
        public decimal Oran { get; set; }          // %
        public decimal SabitTutar { get; set; }    // oran yerine sabit tutar (hedef primi, özel ödeme)
        public string Odeme { get; set; } = "X";   // X (bordro dışı) | R (bordrolu)
        public string OdemeAyi { get; set; }       // Eylül 2026
        public string OdemeDonem { get; set; }     // 2026-09 — primin bordroya girdiği dönem (Personel Maliyet Tablosu dönemi; prim ve bordro aynı ay)
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
            S("Yemek", "yemek", "rx"), S("Yol", "yol", "rx"), S("Kira", "kira", "rx"), S("Diğer Sosyal Yardım", "digerSosyal", "sayi"), S("Toplam Sosyal Yardımlar", "toplamSosyal", "hesap"), S("Toplam Hak Ediş", "toplamHakedis", "hesap"),   // yemek / yol / kira rx (9 Kas 2026, "kiralar X"): R üstte / X altta, Personel Maliyet Tablosu ile aynı; SGK'sızda tamamı X
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
        /// <summary>Üretim personeli satırları; SGK'lı gündelikçide R mahsup / elden X / net ödenen seçili dönemin bordro satırına göre hesaplanır (dönem yoksa güncel). Çağıran PersonelMaliyetOrnek.Kilit altındadır.</summary>
        public static List<Dictionary<string, object>> UretimDto(string donem = null)
        {
            var liste = PersonelMaliyetOrnek.ListeDonem(donem) ?? PersonelMaliyetOrnek.Liste;
            lock (Kilit) return Uretim.Select(u =>
            {
                var d = Dto(u, UretimSutunlar);
                // Yemek / Yol / Kira sütunları R üstte / X altta (9 Kas 2026, kullanıcı: "Elmin Jabbarov ve Kamil Mahmudov kiralar X"): SGK'sız gündelikçide tamamı X (R 0); SGK'lıda bordro kaydının R ve X'i
                d["yemek"] = 0m; d["yemekX"] = u.Yemek; d["yol"] = 0m; d["yolX"] = u.Yol; d["kira"] = 0m; d["kiraX"] = u.Kira;
                // SGK durumu seçili döneme göre (9 Kas 2026): o dönemde bordro kaydı varsa SGK'lı (R mahsup); bordro dışı kayıt ya da o dönemde kayıt yoksa SGK'sız gösterim (tamamı X) — ör. Kamran Cabbarov Eylül'de SGK'sız, Ekim'de bordrolu
                var p = u.PersonelSira > 0 ? liste.FirstOrDefault(x => x.Sira == u.PersonelSira) : null;
                bool sgkli = p != null && !p.SgkDetayYok;
                d["sgkLi"] = sgkli; d["sgkDurumu"] = sgkli ? "SGK'lı (R mahsup)" : "SGK'sız (tamamı X)";
                if (sgkli)
                {   // seçili dönemin bordrosu (tek kaynak): R mahsup = Net-R; yemek / yol / kira / fazla mesai = bordro kaydı R + X (R resmi bordro, sabit); kira mahsupla netleştirilmez, elden X'e eklenir
                    var b = BordroKalemleri(p);
                    decimal mahsup = p.Hesap.NetRBordro, kazanc = u.HakEdis + b.Mesai + u.TatilKazanci, sosyal = b.Yemek + b.Yol + b.Kira + u.DigerSosyal, hakedis = kazanc + sosyal, elden = MahsupFarki(u, p) + b.Kira;   // fark kişinin X-Net'inde, kira Kira Desteği-X'te
                    d["rMahsup"] = mahsup; d["yemek"] = p.Yemek; d["yemekX"] = p.YemekX; d["yol"] = p.Yol; d["yolX"] = p.YolX; d["kira"] = p.KiraDestegi; d["kiraX"] = p.KiraDestegiX; d["fazlaMesaiKazanci"] = b.Mesai; d["fazlaMesaiSaat"] = u.SaatUcreti > 0 ? Math.Round(b.Mesai / (u.SaatUcreti * 1.5m), 2) : 0m;   // saat: bordro mesai tutarından türetilir (gösterim)
                    d["toplamKazanc"] = kazanc; d["toplamSosyal"] = sosyal; d["toplamHakedis"] = hakedis; d["eldenX"] = elden; d["netOdenen"] = elden - u.Kesinti; d["toplamMaliyet"] = 0m;
                }
                else { d["rMahsup"] = 0m; d["eldenX"] = u.ToplamHakedis; d["netOdenen"] = u.ToplamHakedis - u.Kesinti; d["toplamMaliyet"] = u.ToplamHakedis; }   // SGK'sız (bu dönemde): tamamı elden X
                return d;
            }).ToList();
        }
        public static List<Dictionary<string, object>> PrimDto() { lock (Kilit) return Primler.Select(p => Dto(p, PrimSutunlar)).ToList(); }

        // ---------- düzenleme (satıra tıklayınca çekmece): üretim personeli ve prim satırları; değişiklik bordro satırına, karta ve cariye yansır ----------
        public static readonly object Kilit = new object();
        public static readonly string[] Vardiyalar = { "Gündüz", "Akşam", "Gece" };
        public static readonly string[] PrimTurleri = { "Ciro Primi", "Tahsilat Primi", "Hedef Primi", "Özel Ödeme" };
        public class UretimIstek { public int Sira { get; set; } public string FiiliGorevYeri { get; set; } public string AdSoyad { get; set; } public string Gorev { get; set; } public string Vardiya { get; set; } public int PersonelSira { get; set; } public decimal GunlukUcret { get; set; } public decimal CalisilanGun { get; set; } public decimal GunlukSaat { get; set; } = 7.5m; public decimal FazlaMesaiSaat { get; set; } public decimal TatilGun { get; set; } public decimal Yemek { get; set; } public decimal Yol { get; set; } public decimal Kira { get; set; } public decimal DigerSosyal { get; set; } public decimal Kesinti { get; set; } public string Not { get; set; } public string Donem { get; set; } }
        public class PrimIstek { public int Sira { get; set; } public string Donem { get; set; } public string AdSoyad { get; set; } public string PrimTuru { get; set; } public decimal Baz { get; set; } public decimal Oran { get; set; } public decimal SabitTutar { get; set; } public string Odeme { get; set; } public string Aciklama { get; set; } public string BordroDonem { get; set; } }
        public static string UretimKaydet(UretimIstek d, out UretimPersonel kayit)
        {
            kayit = null; if (d == null) return "Veri yok.";
            if (string.IsNullOrWhiteSpace(d.AdSoyad)) return "Adı-Soyadı zorunludur.";
            if (string.IsNullOrWhiteSpace(d.FiiliGorevYeri)) return "Fiili Görev Yeri zorunludur.";
            if (d.GunlukUcret < 0) return "Günlük ücret negatif olamaz.";
            if (d.CalisilanGun < 0 || d.CalisilanGun > 31) return "Çalışılan gün 0–31 arasında olmalıdır.";
            if (d.GunlukSaat < 0 || d.GunlukSaat > 12) return "Günlük saat 0–12 arasında olmalıdır.";
            if (d.FazlaMesaiSaat < 0 || d.TatilGun < 0 || d.Yemek < 0 || d.Yol < 0 || d.Kira < 0 || d.DigerSosyal < 0 || d.Kesinti < 0) return "Tutarlar, günler ve saatler negatif olamaz.";
            // SGK'lı gündelikçi (tek kaynak, 9 Kas 2026): yemek / yol / kira seçili dönemin bordro kaydına yazılır — R (resmi bordro) sabit kalır, girilen toplamın R'yi aşan kısmı X; R'nin altı hata
            PmPersonel bordro = null; decimal yemekX = 0, yolX = 0, kiraX = 0;
            if (d.PersonelSira > 0)
                lock (PersonelMaliyetOrnek.Kilit)
                {
                    var liste = PersonelMaliyetOrnek.ListeDonem(d.Donem) ?? PersonelMaliyetOrnek.Liste;
                    bordro = liste.FirstOrDefault(p => p.Sira == d.PersonelSira) ?? PersonelMaliyetOrnek.Liste.FirstOrDefault(p => p.Sira == d.PersonelSira);
                    if (bordro == null) return "Bağlanacak bordrolu personel bulunamadı.";
                    var h = Fark("Yemek", d.Yemek, bordro.Yemek, out yemekX) ?? Fark("Yol", d.Yol, bordro.Yol, out yolX) ?? Fark("Kira", d.Kira, bordro.KiraDestegi, out kiraX); if (h != null) return h;
                }
            int eskiPersonel = 0;
            lock (Kilit)
            {
                var u = d.Sira > 0 ? Uretim.FirstOrDefault(x => x.Sira == d.Sira) : null;
                if (d.Sira > 0 && u == null) return "Üretim personeli bulunamadı.";
                if (u == null) { u = new UretimPersonel { Sira = ++_u }; Uretim.Add(u); } else eskiPersonel = u.PersonelSira;
                u.FiiliGorevYeri = d.FiiliGorevYeri.Trim(); u.AdSoyad = d.AdSoyad.Trim(); u.Gorev = (d.Gorev ?? "").Trim(); u.Vardiya = string.IsNullOrWhiteSpace(d.Vardiya) ? "Gündüz" : d.Vardiya.Trim(); u.PersonelSira = Math.Max(0, d.PersonelSira);
                u.GunlukUcret = d.GunlukUcret; u.CalisilanGun = d.CalisilanGun; u.GunlukSaat = d.GunlukSaat; u.FazlaMesaiSaat = d.FazlaMesaiSaat; u.TatilGun = d.TatilGun; u.Yemek = d.Yemek; u.Yol = d.Yol; u.Kira = d.Kira; u.DigerSosyal = d.DigerSosyal; u.Kesinti = d.Kesinti; u.Not = (d.Not ?? "").Trim();   // SGK'lı gündelikçide yemek / yol / kira UretimYansit ile bordrodan yeniden yazılır
                kayit = u;
            }
            if (bordro != null) lock (PersonelMaliyetOrnek.Kilit) { bordro.YemekX = yemekX; bordro.YolX = yolX; bordro.KiraDestegiX = kiraX; }   // bordro kaydı (X tarafı): brüt, SGK ve vergiye girmez; ana tablo, kişi kartı, Birleşik ve Ödeme aynı anda değişir
            if (eskiPersonel > 0 && eskiPersonel != kayit.PersonelSira) XSifirla(eskiPersonel);   // bağ koptu: eski kişinin elden X'i sıfırlanır
            if (kayit.PersonelSira == 0) GundelikciKisileriTamamla();   // kişi kaydı olmayan (SGK'sız) satır: bordro dışı kişi kaydı açılır → cari, dosya, kart (9 Kas 2026)
            UretimYansit();
            return null;
        }
        /// <summary>Üretim formundan girilen toplam (R + X) → X = toplam − R; toplam R'nin altındaysa hata metni (R resmi bordrodan gelir, Üretim'den düşürülemez).</summary>
        private static string Fark(string ad, decimal toplam, decimal r, out decimal x)
        {
            x = Math.Max(0, BordroParametre.Yuvarla(toplam - r));
            return toplam < r - 0.005m ? $"{ad} {toplam.ToString("N2", Tr)} ₺ bordro R tutarının ({r.ToString("N2", Tr)} ₺) altına inemez: R resmi bordrodan gelir ve sabittir; R'yi kişi kartından değiştirin." : null;
        }
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        /// <summary>SGK'lı gündelikçinin bordro kaydından gelen kalemler (tek kaynak): yemek, yol, kira ve fazla mesai = R + X (R resmi bordro, sabit; fark X). Üretim satırı bunları gösterir, formdan girilen toplam → X = toplam − R.</summary>
        public static (decimal Yemek, decimal Yol, decimal Kira, decimal Mesai) BordroKalemleri(PmPersonel p) => (p.Yemek + p.YemekX, p.Yol + p.YolX, p.KiraDestegi + p.KiraDestegiX, p.FazlaMesai + p.FazlaMesaiX);
        public static string UretimSil(int sira) { int ps; lock (Kilit) { var u = Uretim.FirstOrDefault(x => x.Sira == sira); if (u == null) return "Üretim personeli bulunamadı."; ps = u.PersonelSira; Uretim.Remove(u); } if (ps > 0) XSifirla(ps); return null; }
        /// <summary>Bağ koptu / satır silindi: tüm dönemlerde elden X sıfırlanır; bordro dışı gündelikçi kaydında Üretim'den yazılan X kalemleri (mesai, yemek, yol, kira, kesinti) de sıfırlanır (kişi kaydı kalır; silmek kullanıcı kararı).</summary>
        private static void XSifirla(int personelSira) { lock (PersonelMaliyetOrnek.Kilit) foreach (var l in PersonelMaliyetOrnek.Donemler.Values) { var p = l.FirstOrDefault(x => x.Sira == personelSira); if (p == null || p.Dovizli) continue; p.XNet = 0; if (p.SgkDetayYok && p.OzelDurum == "Gündelikçi") p.FazlaMesaiX = p.YemekX = p.YolX = p.KiraDestegiX = p.KesintilerToplamiX = 0; } }
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
        private const string LISTE_ADI = "Gündelikçi Ücretleri 2026 – 09.11.2026", G_EMIN = "Uras Kimya - Emin", G_DEPO = "Depo - Refik", G_PLASTIK = "Plastik - Rahman", G_CORLU = "Çorlu";
        private static int _u, _p;
        /// <summary>Gündelikçi Ücretleri 2026 listesinden: günlük ücret; çalışılan gün varsayılan 26 (listede yok — sekmeden düzeltilir). sgkli = true → Konsolide İK Paneli'ndeki aynı adlı bordrolu kişiye bağlanır (R mahsup); grup = listedeki sütun başlığı (nota yazılır).</summary>
        private static UretimPersonel U(string yer, string ad, decimal gunluk, bool sgkli = false, decimal gun = 26, string gorev = "Üretim Personeli", string vardiya = "Gündüz", string not = null, string grup = null)
            => new UretimPersonel { Sira = ++_u, FiiliGorevYeri = yer, AdSoyad = ad, Gorev = gorev, Vardiya = vardiya, GunlukUcret = gunluk, CalisilanGun = gun, PersonelSira = sgkli ? -1 : 0,
                Not = (not ?? (sgkli ? "SGK'lı gündelikçi: bordro R neti mahsup edilir, fark elden X" : "SGK'sız gündelikçi: tamamı elden X")) + (grup != null ? $" · {LISTE_ADI}: {grup}" : "") };
        /// <summary>"Gündelikçi Ücretleri 2026 – 09.11.2026" (kullanıcı Excel'i, 9 Kas 2026; adlar listedeki gibi ad-soyad): Uras Kimya 16 (Uras Kimya - Emin 7, Depo - Refik 4, Plastik - Rahman 5) + Alv Kimya Çorlu 6 = 22 kişi; günlük ücretler 20.10.2026 listesiyle aynı.
        /// Adı bordroda olan 11 kişi SGK'lı (bordrolu; R mahsup): Kamran Cabbarov (önceki listede "Kamuran") 26 Ekim 2026'dan itibaren Uras Kimya bordrosunda — Ekim'de SGK'lı, Eylül'de SGK'sız (bordro dışı dönem kopyası). Elman Abbaslı 09.11.2026 listesinde yok;
        /// Ekim 2026 bordrosunda olduğundan satırı korundu (ayrıldıysa Durum İşten Ayrıldı yapılır). SGK'sız 11 kişi bordro dışı kişi kaydıyla (335.9xx) panelde, Personel Carileri'nde ve Personel Dosyası'nda yer alır (GundelikciKisileriTamamla).
        /// Kamil Mahmudov (ALV Kimya Çorlu, bordrolu üretim personeli) kullanıcı isteğiyle (6 Kas 2026) SGK'lı gündelikçi; günlük 1.700 ₺, çalışılan gün 30 varsayıldı (bordro R-Net 51.000,01 = 30 × 1.700); yol ve fazla mesai bordro kaydından → elden yalnız kira 23.520 ₺.</summary>
        public static readonly List<UretimPersonel> Uretim = new List<UretimPersonel>
        {
            // Uras Kimya - Emin
            U(URS, "Muharrem Abdullayev", 2200, grup: G_EMIN), U(URS, "Azad Shirinov", 2200, true, grup: G_EMIN), U(URS, "Elmin Jabbarov", 3500, true, grup: G_EMIN), U(URS, "Sinan Cabbarzade", 2200, grup: G_EMIN),
            U(URS, "Kamran Cabbarov", 2400, true, not: "SGK'lı gündelikçi: 26 Ekim 2026'dan itibaren Uras Kimya bordrosunda (Ekim bordrosu 6 prim günü, Net-R 8.422,65); hak edişten bordro R neti mahsup edilir, fark elden X. Eylül 2026'da SGK'sız (tamamı elden X). 20.10.2026 listesinde \"Kamuran\".", grup: G_EMIN),
            U(URS, "Elşad Necefov", 2200, grup: G_EMIN), U(URS, "Çoşkun Cabbarzade", 2200, grup: G_EMIN),
            // Depo - Refik
            U(URS, "Ali Askerov", 2200, grup: G_DEPO), U(URS, "Ramin Musayev", 2500, true, grup: G_DEPO), U(URS, "Tural Abbasov", 2400, true, grup: G_DEPO), U(URS, "Araz Nagiyev", 2200, grup: G_DEPO),
            U(URS, "Elman Abbaslı", 2200, true, not: "SGK'lı gündelikçi: bordro R neti mahsup edilir, fark elden X. 09.11.2026 tarihli gündelikçi listesinde yer almıyor (20.10.2026 listesinde Depo - Refik); Ekim 2026 bordrosunda olduğundan satır korundu — ayrıldıysa kişi kartından Durum: İşten Ayrıldı yapın."),
            // Plastik - Rahman
            U(URS, "Mehman Babayev", 2500, true, grup: G_PLASTIK), U(URS, "Rahman Babazade", 2600, true, grup: G_PLASTIK), U(URS, "Savalan Ağazade", 2300, grup: G_PLASTIK), U(URS, "Murad Hudaybergenov", 2200, true, grup: G_PLASTIK), U(URS, "Ayhan Musayev", 2300, grup: G_PLASTIK),
            // Çorlu
            U(COR, "Reşat Aliyev", 2000, grup: G_CORLU), U(COR, "Aslan Hüseynov", 2050, true, grup: G_CORLU), U(COR, "Ali Mahmudov", 2000, grup: G_CORLU), U(COR, "Naim Şoev", 2000, grup: G_CORLU), U(COR, "Ruslan Hüseynov", 2100, grup: G_CORLU),
            U(COR, "Kamil Mahmudov", 1700, true, gun: 30, not: "SGK'lı gündelikçi: bordro R neti mahsup edilir, fark elden X. Günlük 1.700 ₺; çalışılan gün 30 varsayıldı (bordro R-Net 51.000,01 = 30 × 1.700). Yol ve fazla mesai bordro kaydından.", grup: G_CORLU)
        };
        /// <summary>SGK'lı gündelikçileri Personel Maliyet Tablosu'na bağlar (ad eşleşmesi) ve HER DÖNEMDE o dönemin bordro Net-R'sini mahsup olarak hesaplar; kalan fark (MahsupFarki) kişinin o dönemdeki
        /// X-Net'ine yazılır (9 Kas 2026, kullanıcı onayı "evet"; önce kaynak v5_2 gereği 0 tutuluyordu — "kalan kısım mahsup edilerek elden alınıyor"). Kira ayrıca Kira Desteği-X'tedir.
        /// Üretim sekmesindeki R mahsup / elden X seçili döneme göre UretimDto(donem) ile gelir. Her veri okumasında çalışır (dinamik).</summary>
        public static void UretimYansit()
        {
            lock (PersonelMaliyetOrnek.Kilit)
            {
                var donemler = PersonelMaliyetOrnek.Donemler;
                var sirali = donemler.Keys.OrderByDescending(k => k, StringComparer.Ordinal).ToList();   // en yeni dönem önce: bağ ve SGK durumu en yeni kayda göre (kişi yalnız sonraki dönemde olabilir; Kamran Cabbarov Ekim 2026)
                lock (Kilit)
                    foreach (var u in Uretim)
                    {
                        if (u.PersonelSira == -1) { var e = KisiBulAd(u.AdSoyad, sirali, donemler); if (e == null) { u.RMahsup = 0; u.BordroDisi = false; continue; } u.PersonelSira = e.Sira; }   // tohum: ada göre; kişi henüz yoksa (dönem sonra kurulur) bekler
                        if (u.PersonelSira <= 0) { u.RMahsup = 0; u.BordroDisi = false; continue; }
                        var guncel = KisiBul(u.PersonelSira, sirali, donemler);
                        if (guncel == null) { u.PersonelSira = 0; u.RMahsup = 0; u.BordroDisi = false; continue; }   // kişi kaydı silinmiş: bağ kopar (yeni kayıt GundelikciKisileriTamamla ile açılır)
                        u.BordroDisi = guncel.SgkDetayYok;
                        if (u.BordroDisi) u.RMahsup = 0;
                        else { var b = BordroKalemleri(guncel); u.RMahsup = guncel.Hesap.NetRBordro; u.Yemek = b.Yemek; u.Yol = b.Yol; u.Kira = b.Kira; }   // SGK'lı: yemek / yol / kira bordro kaydı R + X (tek kaynak); seçili dönem için UretimDto(donem) yeniden okur
                        foreach (var kv in donemler)
                        {
                            var p = kv.Value.FirstOrDefault(x => x.Sira == u.PersonelSira); if (p == null || p.Dovizli) continue;   // o dönemde yok
                            if (p.SgkDetayYok)
                            {   // bordro dışı dönem (SGK'sız gündelikçi): Üretim satırı tek kaynak → tamamı X: hak ediş + tatil + diğer sosyal → X-Net, mesai → Fazla Mesai-X, yemek / yol / kira → X, avans / kesinti → Kesintiler Toplamı-X
                                p.XNet = BordroParametre.Yuvarla(u.HakEdis + u.TatilKazanci + u.DigerSosyal); p.FazlaMesaiX = u.FazlaMesaiKazanci; p.YemekX = u.Yemek; p.YolX = u.Yol; p.KiraDestegiX = u.Kira; p.KesintilerToplamiX = u.Kesinti;
                                p.RNet = 0; p.Yemek = 0; p.Yol = 0; p.KiraDestegi = 0; p.FazlaMesai = 0;
                            }
                            else p.XNet = MahsupFarki(u, p);   // bordrolu dönem: elden mahsup farkı → X-Net (9 Kas 2026); kartta girilen X-Net bu kişilerde her okumada buradan yeniden yazılır
                        }
                    }
            }
        }
        private static PmPersonel KisiBul(int sira, List<string> sirali, SortedDictionary<string, List<PmPersonel>> d) { foreach (var k in sirali) { var p = d[k].FirstOrDefault(x => x.Sira == sira); if (p != null) return p; } return null; }
        private static PmPersonel KisiBulAd(string ad, List<string> sirali, SortedDictionary<string, List<PmPersonel>> d) { foreach (var k in sirali) { var p = d[k].FirstOrDefault(x => string.Equals(x.AdSoyad, ad, StringComparison.OrdinalIgnoreCase)); if (p != null) return p; } return null; }

        /// <summary>SGK'sız gündelikçi kişi kayıtlarının sıra aralığı (cari kodu 335.901…). Bordrolu / özel kişilerin sırası bu aralığa girmez: yeni kişi sırası aralığın altından verilir (PersonelMaliyetOrnek.YeniSira),
        /// böylece kalıcı günlük yeniden yüklenirken (Ekim 2026 dönemi günlükten kurulur) sıralar değişmez.</summary>
        public const int GundelikciSiraBasi = 900;
        /// <summary>SGK'sız gündelikçilere Konsolide İK Paneli kişi kaydı (9 Kas 2026, kullanıcı: "bu kişilerin de hepsinin personel carileri ve dosyaları olsun"): kişi kaydı olmayan (PersonelSira 0) her Üretim satırı için TÜM dönemlerde
        /// bordro dışı kayıt açılır (SgkDetayYok, Ozel = false, Özel Durum Gündelikçi, Üretim / Üretim Personeli / D30, SGK işvereni = fiili şirket, cari kodu 335.9xx) → Personel Carileri, Personel Dosyası, kişi kartı, Ödeme Planı (elden X), Özet;
        /// tutarlar UretimYansit ile Üretim satırından yazılır. Aynı adlı bordro dışı kayıt varsa ona bağlanır (aynı adlı bordrolu kişi varsa bağ Üretim formundan seçilir). Bağlı (SGK'lı) gündelikçinin kaydı olmayan dönemine bordro dışı kopya
        /// açılır (Kamran Cabbarov Eylül 2026: o ay SGK'sız çalıştı); bağlı kişide Özel Durum boşsa Gündelikçi yazılır. Tohumdan sonra (OrnekIsinma, kalıcı günlükten ÖNCE), yeni dönem kurulunca (DonemKur) ve yeni Üretim satırı kaydedilince çalışır; idempotent.</summary>
        public static void GundelikciKisileriTamamla()
        {
            lock (PersonelMaliyetOrnek.Kilit)
            {
                var donemler = PersonelMaliyetOrnek.Donemler; if (donemler.Count == 0) return;
                var sirali = donemler.Keys.OrderByDescending(k => k, StringComparer.Ordinal).ToList();
                lock (Kilit)
                    foreach (var u in Uretim)
                    {
                        if (u.PersonelSira == -1) { var e = KisiBulAd(u.AdSoyad, sirali, donemler); if (e == null) continue; u.PersonelSira = e.Sira; }   // SGK'lı tohum satırı: bordrolu kişi henüz kurulmadıysa (Ekim günlükten gelir) bekler, kayıt AÇILMAZ
                        if (u.PersonelSira == 0)
                        {
                            var mevcut = KisiBulAd(u.AdSoyad, sirali, donemler);
                            if (mevcut != null && mevcut.SgkDetayYok) u.PersonelSira = mevcut.Sira;
                            else if (mevcut == null)
                            {
                                int sira = Math.Max(GundelikciSiraBasi, donemler.Values.SelectMany(l => l).Select(x => x.Sira).DefaultIfEmpty(0).Max()) + 1;
                                foreach (var kv in donemler) kv.Value.Add(YeniGundelikciKaydi(u, sira, null, kv.Key));
                                u.PersonelSira = sira;
                            }
                            else continue;   // aynı adlı bordrolu kişi var: SGK'lı bağ kullanıcı kararıdır (Üretim formundan seçilir)
                        }
                        var ornek = KisiBul(u.PersonelSira, sirali, donemler); if (ornek == null) continue;
                        foreach (var kv in donemler) if (!kv.Value.Any(x => x.Sira == u.PersonelSira)) kv.Value.Add(YeniGundelikciKaydi(u, u.PersonelSira, ornek, kv.Key));   // kaydı olmayan dönem: bordro dışı kopya (o ay SGK'sız)
                        foreach (var kv in donemler) foreach (var p in kv.Value.Where(x => x.Sira == u.PersonelSira && string.IsNullOrWhiteSpace(x.OzelDurum))) p.OzelDurum = "Gündelikçi";
                    }
            }
        }
        /// <summary>Bordro dışı gündelikçi kişi kaydı: ornek verilirse kimlik ondan kopyalanır (kaydı olmayan dönem), yoksa Üretim satırından kurulur; tutarlar sıfır (UretimYansit yazar), SGK / brüt detayı yok.</summary>
        private static PmPersonel YeniGundelikciKaydi(UretimPersonel u, int sira, PmPersonel ornek, string donem)
        {
            string sirket = string.IsNullOrEmpty(u.FiiliSirket) ? u.FiiliGorevYeri : u.FiiliSirket;
            PmPersonel p;
            if (ornek != null) { p = ornek.Kopya(); PersonelMaliyetOrnek.TutarlariSifirla(p); p.Not = ((p.Not ?? "") + $" {PersonelMaliyetOrnek.DonemAd(donem)} döneminde bordro kaydı yok: SGK'sız gündelikçi (tutarlar Üretim Personeli satırından, tamamı elden X).").Trim(); }
            else p = new PmPersonel
            {
                AdSoyad = u.AdSoyad, SgkIsvereni = sirket, FiiliGorevYeri = u.FiiliGorevYeri, Unvan = "Personel", Bolum = "Üretim", Gorev = string.IsNullOrWhiteSpace(u.Gorev) ? "Üretim Personeli" : u.Gorev, CatiDepartman = "Üretim",
                DepartmanKodu = "D30", DepartmanAciklamasi = "Üretim Departmanı", OzelDurum = "Gündelikçi", SicilNo = "", SgkNo = "",
                Not = "SGK'sız gündelikçi (bordro dışı kişi kaydı, 9 Kas 2026): tutarlar Üretim Personeli satırından gelir (günlük ücret × gün + mesai + tatil + yemek + yol + kira, tamamı elden X) ve her okumada oradan yazılır; kartta girilen tutar korunmaz." + (string.IsNullOrEmpty(u.Not) ? "" : " " + u.Not)
            };
            p.Sira = sira; p.SgkDetayYok = true; p.Ozel = false; p.BankaBlokeli = false; p.OzelDurum = "Gündelikçi";
            p.PrimGun = 0; p.NormalGun = 0; p.HaftaSonuGun = 0; p.ResmiTatilGun = 0; p.BayramGun = 0; p.UcretliIzinGun = 0;
            p.BrutKaynak = null; p.IsverenKaynak = null; p.HazineKaynak = 0; p.SgkDisi = 0;
            return p;
        }
        /// <summary>Elden mahsup farkı (SGK'lı gündelikçi): hak ediş (günlük × gün) + bordro fazla mesaisi (R + X) + tatil kazancı + yemek + yol (bordro R + X) + diğer sosyal − bordro Net-R; negatif olmaz.
        /// Kişinin o dönemdeki X-Net'i budur (UretimYansit); Üretim sekmesindeki Elden Ödenecek (X) = fark + kira (kira ayrıca Kira Desteği-X'te, mahsupla netleştirilmez).</summary>
        public static decimal MahsupFarki(UretimPersonel u, PmPersonel p)
        {
            var b = BordroKalemleri(p);
            return Math.Max(0, BordroParametre.Yuvarla(u.HakEdis + b.Mesai + u.TatilKazanci + b.Yemek + b.Yol + u.DigerSosyal - p.Hesap.NetRBordro));
        }
        /// <summary>Elden ödenecek X (SGK'lı gündelikçi): (kazançlar + yemek + yol + diğer sosyal) − verilen dönemin bordro Net-R mahsubu (negatif olmaz) + kira (X'ten ödenen kira mahsupla netleştirilmez; 9 Kas 2026).</summary>
        public static decimal EldenX(decimal kazanc, decimal mahsuplananSosyal, decimal mahsup, decimal kira) => Math.Max(0, BordroParametre.Yuvarla(kazanc + mahsuplananSosyal - mahsup)) + kira;
        public static decimal EldenX(UretimPersonel u, decimal mahsup, decimal kira) => EldenX(u.ToplamKazanc, u.Yemek + u.Yol + u.DigerSosyal, mahsup, kira);
        public static decimal EldenX(UretimPersonel u, decimal mahsup) => EldenX(u, mahsup, u.Kira);

        private static PrimOdeme Pr(string donem, string ad, string sgk, string gorev, string tur, decimal baz = 0, decimal oran = 0, decimal sabit = 0, string aciklama = "")
        {
            int y = int.Parse(donem.Substring(0, 4)), m = int.Parse(donem.Substring(5, 2));
            string[] aylar = { "Ocak", "Şubat", "Mart", "Nisan", "Mayıs", "Haziran", "Temmuz", "Ağustos", "Eylül", "Ekim", "Kasım", "Aralık" };
            return new PrimOdeme { Sira = ++_p, Donem = donem, DonemAd = aylar[m - 1] + " " + y, AdSoyad = ad, SgkIsvereni = sgk, Gorev = gorev, PrimTuru = tur, Baz = baz, Oran = oran, SabitTutar = sabit, Odeme = "X", OdemeAyi = aylar[m - 1] + " " + y, OdemeDonem = donem, Aciklama = aciklama };
        }
        /// <summary>Eylül 2026 prim satırları: kişi toplamı ana tablodaki Prim-X ile birebir.</summary>
        public static readonly List<PrimOdeme> Primler = new List<PrimOdeme>
        {
            Pr("2026-09", "Berkan Özkan", "ALV Kimya", "İhracat Müdürü", "Ciro Primi", 2226442.67m, 3, aciklama: "İhracat cirosu %3"),
            Pr("2026-09", "Erdoğan Polat", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Ciro Primi", 4500000, 2, aciklama: "Emprime iç piyasa cirosu %2"),
            Pr("2026-09", "Erdoğan Polat", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Tahsilat Primi", 3269137.33m, 1.5m, aciklama: "Vadesinde tahsilat %1,5"),
            Pr("2026-09", "Müjgan Yetgin", "Uras Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Tahsilat Primi", 2515432, 2, aciklama: "Tahsilat %2"),
            Pr("2026-09", "Serhat Mutlu", "Avrupa Paper", "İç Piyasa Satış Uzmanı – Dijital", "Ciro Primi", 1655914.40m, 2.5m, aciklama: "Dijital baskı cirosu %2,5"),
            Pr("2026-09", "Sevgi Kaya", "Avrupa Paper", "İhracat Müdürü", "Hedef Primi", sabit: 14906, aciklama: "Aylık ihracat hedefi sabit prim"),
            Pr("2026-09", "Yusuf Öztürk", "ALV Kimya", "İç Piyasa Satış Uzmanı – Emprime", "Ciro Primi", 1604019, 3, aciklama: "Emprime cirosu %3")
        };
        /// <summary>Yeni bordro dönemi (5 Kas 2026): dönemde Prim-X / Prim-R tutarı olan ve o dönemde prim satırı bulunmayan her kişi için sabit tutarlı bir prim satırı açar; Prim Ödemeleri sekmesi ve Prim Uyum tablosu bordroyla birebir kalır. Tür ve baz kaynakta olmadığından "Hedef Primi" + sabit tutar yazılır. PrimX bordroda zaten yazılı olduğundan PrimYansit çağrılmaz.</summary>
        public static void PrimleriDonemdenTuret(string donem)
        {
            if (string.IsNullOrEmpty(donem) || donem.Length != 7) return;
            List<PmPersonel> liste; lock (PersonelMaliyetOrnek.Kilit) liste = (PersonelMaliyetOrnek.ListeDonem(donem) ?? new List<PmPersonel>()).Where(p => p.Durum == "Aktif").OrderBy(p => p.Sira).ToList();
            string donemAd = AylarTr[int.Parse(donem.Substring(5, 2)) - 1] + " " + donem.Substring(0, 4), kaynak = PersonelMaliyetOrnek.KaynakDonem(donem);
            lock (Kilit)
                foreach (var p in liste)
                {
                    if (Primler.Any(t => t.OdemeDonem == donem && t.AdSoyad == p.AdSoyad)) continue;
                    foreach (var (odeme, tutar) in new[] { ("X", p.PrimX), ("R", p.Prim) })
                    {
                        if (tutar <= 0) continue;
                        Primler.Add(new PrimOdeme { Sira = ++_p, Donem = donem, DonemAd = donemAd, AdSoyad = p.AdSoyad, SgkIsvereni = p.SgkIsvereni, Gorev = p.Gorev, PrimTuru = "Hedef Primi", SabitTutar = tutar, Odeme = odeme, OdemeAyi = donemAd, OdemeDonem = donem, Aciklama = "Bordro kaynağındaki Prim-" + odeme + " tutarı (" + kaynak + "); prim türü ve baz kaynakta yok." });
                    }
                }
        }
    }
}
