using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>
    /// Finekra (banka entegrasyonu) hareketi: bankadan otomatik akan satır. Konsolide Finansal Takip Paneli › Finekra sekmesinde onaylanıp cari / hesap seçilince
    /// muhasebe fişi atılır (giriş: 102 borç / cari alacak; çıkış: cari borç / 102 alacak). Her hareket Kasa Nakit Akışı satırıyla eşlenir (otomatik: tutar ± 1 ₺, tarih ± 7 gün;
    /// elle: aday seçimi); nakit akışta karşılığı olmayan çıkış "Nakit Akışa Gönder" ile ÖDENDİ satır olarak açılır. Bellek içi; uygulama yeniden başlayınca tohumlanır.
    /// </summary>
    public class BankaHareketi
    {
        public int Id { get; set; } public string No { get; set; } public string DbName { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public string HesapKodu { get; set; } public string Iban { get; set; }
        public DateTime Tarih { get; set; } public string Yon { get; set; }
        public decimal Tutar { get; set; }                 // TL karşılığı — eşleşme, fiş, toplamlar ve bakiyeler TL üzerinden
        public string PB { get; set; } = "TRY"; public decimal DovizTutar { get; set; } public decimal Kur { get; set; } = 1; public string KurKaynagi { get; set; } = ""; public DateTime? KurTarihi { get; set; }   // dövizli hesapta hareket dövizdir: döviz tutarı · kur (TCMB alış, hareket günü) → TL karşılığı
        public string IslemTuru { get; set; } = ""; public string KarsiAd { get; set; } = ""; public string KarsiIban { get; set; } = ""; public string Aciklama { get; set; } = ""; public string Referans { get; set; } = "";
        public string Durum { get; set; } = "Bekliyor";   // Bekliyor | Onaylandı (muhasebeleşti)
        public string Onaylayan { get; set; } = ""; public DateTime? OnayTarihi { get; set; } public string OnayNotu { get; set; } = "";
        public string CariKod { get; set; } = ""; public string CariAd { get; set; } = ""; public string KarsiHesap { get; set; } = ""; public string YevmiyeNo { get; set; } = "";
        public int? NakitAkisDocEntry { get; set; } public string NakitAkisDb { get; set; } = ""; public string Eslesme { get; set; } = "";   // Otomatik | Elle | Nakit Akışa Gönderildi | ""
        public string EslesmeNotu { get; set; } = ""; public DateTime AkisZamani { get; set; } public string Kaynak { get; set; } = "Finekra API";
        public List<TalimatGunluk> Gunluk { get; set; } = new List<TalimatGunluk>();
        public bool Giris => Yon == "Giriş";
    }
    /// <summary>Kasa / Banka / Çek hareket satırının (üretilmiş) onay durumu — Takip Paneli'nde elle verilen onay / eksik işareti.</summary>
    public class HareketOnay { public string Durum { get; set; } = ""; public string Kullanici { get; set; } = ""; public DateTime Tarih { get; set; } public string Not { get; set; } = ""; }
    public class FinekraGunluk { public DateTime Tarih { get; set; } public string Kullanici { get; set; } public string Sirket { get; set; } public string No { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }

    public static class FinekraOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly List<BankaHareketi> _hareketler = new List<BankaHareketi>();
        static readonly List<FinekraGunluk> _gunluk = new List<FinekraGunluk>();
        public static readonly Dictionary<string, HareketOnay> HareketOnaylari = new Dictionary<string, HareketOnay>();
        static int _sonId; static bool _tohumlandi; static DateTime _sonAkis;
        static int Seed(string s) => s.Aggregate(37, (a, ch) => unchecked(a * 31 + ch));
        public static readonly string[] IslemTurleri = { "Havale / EFT", "Müşteri Havalesi", "POS Tahsilatı", "Çek Tahsilatı", "Otomatik Ödeme", "Vergi / SGK", "Banka Masrafı", "Kredi Taksidi", "Kredi Kartı Ekstresi", "Kira", "Virman" };
        /// <summary>Onayda seçilen karşı muhasebe hesabı (Data/MuhasebeOrnek.cs hesap planı).</summary>
        public static readonly (string Kod, string Ad)[] KarsiHesaplar = { ("320", "Satıcılar"), ("120", "Alıcılar"), ("360", "Ödenecek Vergi ve Fonlar"), ("361", "Ödenecek Sosyal Güvenlik Kesintileri"), ("300", "Banka Kredileri"), ("770", "Genel Yönetim Giderleri"), ("780", "Finansman Giderleri"), ("335", "Personele Borçlar"), ("101", "Alınan Çekler"), ("102", "Bankalar (Virman)"), ("332", "İştiraklere Borçlar (Grup İçi)"), ("132", "İştiraklerden Alacaklar (Grup İçi)") };
        static string HesapAdi(string kod) => MuhasebeOrnek.HesapPlani.TryGetValue(kod ?? "", out var a) ? a : (KarsiHesaplar.FirstOrDefault(k => k.Kod == kod).Ad ?? kod);
        static YevmiyeSatiri S(string kod, decimal borc, decimal alacak, string aciklama) => new YevmiyeSatiri { HesapKodu = kod, HesapAdi = HesapAdi(kod), Borc = Math.Round(borc, 2), Alacak = Math.Round(alacak, 2), Aciklama = aciklama ?? "" };
        static void Log(BankaHareketi h, string islem, string aciklama, string kullanici, DateTime? zaman = null) { var z = zaman ?? DateTime.Now; h.Gunluk.Add(new TalimatGunluk { Tarih = z, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama }); _gunluk.Add(new FinekraGunluk { Tarih = z, Kullanici = kullanici ?? "", Sirket = h.Sirket, No = h.No, Islem = islem, Aciklama = aciklama }); }
        public static DateTime SonAkis { get { Tohumla(); return _sonAkis; } }

        public static List<BankaHareketi> Listele(string dbName, DateTime? bas = null, DateTime? bit = null) { Tohumla(); Senkron(); lock (Kilit) return _hareketler.Where(h => (string.IsNullOrEmpty(dbName) || h.DbName == dbName) && (bas == null || h.Tarih.Date >= bas) && (bit == null || h.Tarih.Date <= bit)).OrderByDescending(h => h.Tarih).ThenBy(h => h.Id).ToList(); }
        public static BankaHareketi Bul(int id) { Tohumla(); lock (Kilit) return _hareketler.FirstOrDefault(h => h.Id == id); }
        public static List<FinekraGunluk> GunlukListesi(string dbName) { Tohumla(); lock (Kilit) return _gunluk.Where(g => string.IsNullOrEmpty(dbName) || g.Sirket == CekAkibetOrnek.SirketAdi(dbName)).OrderByDescending(g => g.Tarih).Take(300).ToList(); }

        // ---------- tohum: bankadan akan hareketler (ödenmiş havaleler, bankanın ödediği ama nakit akışta güncellenmemiş planlılar, nakit akışta olmayan otomatik ödemeler, müşteri tahsilatları, çek tahsilatları, virman) ----------
        static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var bugun = DateTime.Today; _sonAkis = DateTime.Now.AddMinutes(-17);
                var hesaplar = NakitAkisOrnek.BankaHesaplari;
                foreach (var s in CekAkibetOrnek.Sirketler)
                {
                    var rnd = new Random(Seed("finekra|" + s.DbName)); decimal olcek = ButceOrnek.Olcek.TryGetValue(s.Ad, out var o) ? o : 0.3m;
                    int HesapIdx(string bankaHesabi) => Math.Max(0, Array.IndexOf(hesaplar, bankaHesabi ?? ""));
                    string HesapPB(int idx) { var bl = NakitAkisOrnek.Bakiyeler(s.DbName).Where(b => b.BankaKasa == "BANKA").ToList(); return idx < bl.Count ? (bl[idx].PB ?? "TRY") : "TRY"; }
                    // tutar: TL karşılığı. Dövizli hesapta (USD / EUR) ya da dövizli ödemede hareket dövizdir: dovizTutar · kur verilir ya da hareket günü TCMB alış kuruyla hesaplanır
                    BankaHareketi Yeni(DateTime t, string yon, decimal tutar, string tur, string karsiAd, string aciklama, int idx, string pb = null, decimal? dovizTutar = null, decimal? kur = null, string kurKaynagi = null)
                    {
                        idx = Math.Max(0, Math.Min(hesaplar.Length - 1, idx)); var p = hesaplar[idx].Split(" · "); pb = string.IsNullOrEmpty(pb) || pb == "TRY" ? HesapPB(idx) : pb;
                        var h = new BankaHareketi { Id = ++_sonId, No = $"FNK-{t.Year}-{_sonId:000000}", DbName = s.DbName, Sirket = s.Ad, Banka = p[0], HesapKodu = "102.01." + (idx + 1).ToString("000"), Iban = p.Length > 1 ? p[1] : "", Tarih = t.Date.AddHours(9 + rnd.Next(9)).AddMinutes(rnd.Next(60)), Yon = yon, Tutar = Math.Round(tutar, 2), IslemTuru = tur, KarsiAd = karsiAd ?? "", Aciklama = aciklama ?? "", Referans = $"{t:yyyyMMdd}{rnd.Next(100000, 999999)}", AkisZamani = t.Date.AddHours(18) };
                        if (pb != "TRY")
                        {
                            h.PB = pb; h.KurTarihi = KurOrnek.IsGunu(t); h.KurKaynagi = string.IsNullOrEmpty(kurKaynagi) ? "TCMB Döviz Alış" : kurKaynagi; h.Kur = kur ?? KurOrnek.Kur(pb, h.KurTarihi, "TCMB Döviz Alış"); if (h.Kur <= 0) h.Kur = 1;
                            if (dovizTutar != null) { h.DovizTutar = Math.Round(dovizTutar.Value, 2); h.Tutar = Math.Round(h.DovizTutar * h.Kur, 2); } else h.DovizTutar = Math.Round(h.Tutar / h.Kur, 2);
                        }
                        else { h.DovizTutar = h.Tutar; h.Kur = 1; }
                        _hareketler.Add(h); Log(h, "Akış", $"{h.Banka} · {h.Referans} · {yon} {h.Tutar.ToString("N2", Tr)} ₺" + (h.PB != "TRY" ? $" ({h.DovizTutar.ToString("N2", Tr)} {h.PB} × {h.Kur.ToString("N4", Tr)} {h.KurKaynagi})" : "") + $" · {tur}", "Finekra", h.AkisZamani); return h;
                    }
                    void Onayla(BankaHareketi h, string cariKod, string cariAd, string hesap, string yevmiye, DateTime z)
                    {
                        h.Durum = "Onaylandı"; h.Onaylayan = "Nazlı Erdem"; h.OnayTarihi = z; h.CariKod = cariKod ?? ""; h.CariAd = cariAd ?? ""; h.KarsiHesap = hesap; h.YevmiyeNo = yevmiye ?? "";
                        if (string.IsNullOrEmpty(h.YevmiyeNo)) { var f = Fis(h, hesap, cariAd, "Nazlı Erdem", z); h.YevmiyeNo = f.No; }
                        Log(h, "Onay", $"{cariAd} · {hesap} {HesapAdi(hesap)} · fiş {h.YevmiyeNo}", "Nazlı Erdem", z);
                    }
                    var odemeler = NakitAkisOrnek.Odemeler(s.DbName);
                    // 1) ödenmiş havaleler → banka çıkışı (eşleşmiş; çoğu onaylı)
                    foreach (var m in odemeler.Where(m => m.U_OdemeYontemi == "Havale / EFT" && m.U_Durum == "ÖDENDİ" && m.U_OdenmeTarihi != null && m.U_OdenmeTarihi >= bugun.AddDays(-75)).OrderBy(m => m.U_OdenmeTarihi))
                    {
                        string pb1 = m.U_OdenenTutarPB ?? m.U_ParaBirimi ?? "TRY"; // dövizli ödeme: hareket döviz, TL karşılığı satırın kuruyla
                        var h = Yeni(m.U_OdenmeTarihi.Value, "Çıkış", m.U_TutarTL ?? 0, "Havale / EFT", (m.U_AliciHesapAdi ?? m.U_Kurum ?? "").ToUpper(Tr), $"EFT {m.U_FaturaSiparisNo} {m.U_UrunHizmet}".Trim(), HesapIdx(m.U_BankaHesabi), pb1, pb1 == "TRY" ? null : (m.U_OdenenTutar ?? m.U_Tutar), pb1 == "TRY" ? null : m.U_Kur, pb1 == "TRY" ? null : m.U_KurKaynagi);
                        h.KarsiIban = m.U_AliciIban ?? ""; h.NakitAkisDocEntry = m.DocEntry; h.NakitAkisDb = s.DbName; h.Eslesme = "Otomatik"; h.EslesmeNotu = "tutar ve tarih eşleşti";
                        bool grup = CekAkibetOrnek.GrupSirketleri.Contains(m.U_Kurum ?? "");
                        if (rnd.NextDouble() < 0.7) Onayla(h, CariKod(s.Ad, m.U_Kurum, grup ? "332" : "320"), m.U_Kurum, grup ? "332" : "320", m.U_YevmiyeNo, h.Tarih.AddHours(3));
                    }
                    // 2) bankanın ödediği ama nakit akışta hâlâ Planlandı görünen havaleler (onaylanınca satır ÖDENDİ olur)
                    foreach (var m in odemeler.Where(m => m.U_OdemeYontemi == "Havale / EFT" && (m.U_Durum == "Planlandı" || m.U_Durum == "Gecikti") && m.U_PlanlananTarih >= bugun.AddDays(-12) && m.U_PlanlananTarih <= bugun.AddDays(-1)).OrderBy(m => m.U_PlanlananTarih).Take(2))
                    {
                        string pb2 = m.U_ParaBirimi ?? "TRY";
                        var h = Yeni(m.U_PlanlananTarih.Value, "Çıkış", m.U_TutarTL ?? 0, "Havale / EFT", (m.U_AliciHesapAdi ?? m.U_Kurum ?? "").ToUpper(Tr), $"EFT {m.U_FaturaSiparisNo} {m.U_UrunHizmet}".Trim(), HesapIdx(m.U_BankaHesabi), pb2, pb2 == "TRY" ? null : m.U_Tutar, pb2 == "TRY" ? null : m.U_Kur, pb2 == "TRY" ? null : m.U_KurKaynagi);
                        h.KarsiIban = m.U_AliciIban ?? ""; h.NakitAkisDocEntry = m.DocEntry; h.NakitAkisDb = s.DbName; h.Eslesme = "Otomatik"; h.EslesmeNotu = "nakit akışta Planlandı: onaylanınca satır ÖDENDİ olur";
                    }
                    // 3) nakit akışta karşılığı olmayan çıkışlar (otomatik ödeme, vergi / SGK, masraf, kredi, kart, kira) → "Nakit Akışa Gönder" adayları
                    var sablon = new (string tur, string karsi, string aciklama, decimal tutar, int gunOnce)[]
                    {
                        ("Otomatik Ödeme", "ENERJİSA ELEKTRİK PERAKENDE", "OTOMATİK FATURA ÖDEMESİ ELEKTRİK 2026/09", Math.Round(184_320m * olcek / 10) * 10, 4), ("Otomatik Ödeme", "İGDAŞ", "DOĞALGAZ OTOMATİK ÖDEME 2026/09", Math.Round(96_450m * olcek / 10) * 10, 9),
                        ("Vergi / SGK", "SGK GEBZE SGM", "SGK PRİM TAHAKKUK 2026/09", Math.Round(2_150_000m * olcek / 100) * 100, 2), ("Vergi / SGK", "GİB YENİBOSNA VD", "KDV BEYANNAME ÖDEMESİ 2026/09", Math.Round(1_284_500m * olcek / 100) * 100, 2),
                        ("Banka Masrafı", "ZİRAAT BANKASI", "EFT MASRAFI + BSMV", 1_240m, 1), ("Kredi Taksidi", "ZİRAAT BANKASI", "TAKSİTLİ TİCARİ KREDİ TAKSİT 7/24", Math.Round(412_600m * olcek / 100) * 100, 6),
                        ("Kredi Kartı Ekstresi", "GARANTİ BONUS BUSINESS", "KURUMSAL KART EKSTRE ÖDEMESİ", Math.Round(86_300m * olcek / 10) * 10, 12), ("Kira", "GENÇOSMAN GAYRİMENKUL", "EKİM 2026 KİRA ÖDEMESİ", Math.Round(185_000m * olcek / 100) * 100, 3)
                    };
                    int sablonSay = 4 + (int)Math.Round(4 * olcek);
                    foreach (var t in sablon.Take(sablonSay)) { var h = Yeni(bugun.AddDays(-t.gunOnce), "Çıkış", t.tutar, t.tur, t.karsi, t.aciklama, rnd.Next(2)); h.EslesmeNotu = "nakit akışta karşılığı yok"; if (t.tur == "Banka Masrafı") Onayla(h, "", t.karsi, "780", null, h.Tarih.AddHours(2)); }
                    // 4) müşteri havaleleri / POS tahsilatları (giriş; nakit akış dışı — tahsilat)
                    var musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" && k.Sirket == s.Ad).ToList(); if (musteriler.Count == 0) musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").ToList();
                    for (int i = 0; i < 7 + (int)(9 * olcek); i++)
                    {
                        var mu = musteriler[rnd.Next(musteriler.Count)]; bool pos = rnd.NextDouble() < 0.2; var t = bugun.AddDays(-rnd.Next(0, 45));
                        // müşteri havalelerinin bir kısmı dövizli hesaba (USD / EUR) döviz olarak gelir; TL karşılığı hareket günü TCMB alış kuruyla
                        bool doviz = !pos && rnd.NextDouble() < 0.3; int idx4 = doviz ? 2 + rnd.Next(2) : rnd.Next(2); string pb4 = doviz ? (idx4 == 2 ? "USD" : "EUR") : "TRY"; decimal dt4 = doviz ? rnd.Next(5, 90) * 1000m : 0;
                        var h = doviz ? Yeni(t, "Giriş", 0, "Müşteri Havalesi", mu.KisaAd.ToUpper(Tr), $"{mu.KisaAd.ToUpper(Tr)} FATURA ÖDEMESİ {pb4}", idx4, pb4, dt4, null, "TCMB Döviz Alış")
                            : Yeni(t, "Giriş", rnd.Next(20, 900) * 1000m, pos ? "POS Tahsilatı" : "Müşteri Havalesi", pos ? "POS GÜNSONU" : mu.KisaAd.ToUpper(Tr), pos ? $"POS GÜN SONU TAHSİLATI {t:dd.MM.yyyy}" : $"{mu.KisaAd.ToUpper(Tr)} FATURA ÖDEMESİ", idx4);
                        h.EslesmeNotu = "tahsilat: nakit akış dışı";
                        if (rnd.NextDouble() < 0.6) Onayla(h, pos ? "" : mu.Kod, pos ? "POS tahsilatı (müşteriler)" : mu.KisaAd, "120", null, h.Tarih.AddHours(4));
                    }
                    // 5) bankadan tahsil edilen çekler (giriş; onaylı 102 / 101)
                    foreach (var k in CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == s.Ad && k.IslemTuru == "Bankadan Tahsil Edildi" && k.Vade >= bugun.AddDays(-75) && k.Vade <= bugun).OrderBy(k => k.Vade).Take(4))
                    { var h = Yeni(k.Vade, "Giriş", k.Tutar, "Çek Tahsilatı", (k.Musteri ?? "").ToUpper(Tr), $"ÇEK TAHSİLATI {k.No} {k.Banka}", k.IbrazYeri == "Halk Bankası" ? 1 : 0); h.EslesmeNotu = "çek tahsilatı: Çek Akıbeti kaydı"; Onayla(h, k.MusteriKod, k.Musteri, "101", null, h.Tarih.AddHours(2)); }
                    // 6) virman (hesaplar arası; 102 / 102)
                    { var t = bugun.AddDays(-rnd.Next(1, 20)); var h = Yeni(t, "Çıkış", Math.Round(1_500_000m * olcek / 1000) * 1000, "Virman", hesaplar[1].Split(" · ")[0] + " VADESİZ TL", "VİRMAN " + hesaplar[0].Split(" · ")[0].ToUpper(Tr) + " → " + hesaplar[1].Split(" · ")[0].ToUpper(Tr), 0); h.EslesmeNotu = "virman: nakit akış dışı"; Onayla(h, "", hesaplar[1].Split(" · ")[0] + " vadesiz", "102", null, h.Tarih.AddHours(1)); }
                }
            }
        }
        static string CariKod(string sirket, string ad, string varsayilanHesap) { var k = CariKartOrnek.Kartlar.FirstOrDefault(x => string.Equals(x.KisaAd, ad ?? "", StringComparison.OrdinalIgnoreCase) && x.Sirket == sirket) ?? CariKartOrnek.Kartlar.FirstOrDefault(x => string.Equals(x.KisaAd, ad ?? "", StringComparison.OrdinalIgnoreCase)); return k?.Kod ?? (string.IsNullOrEmpty(ad) ? "" : varsayilanHesap + "." + (Math.Abs(Seed(ad)) % 900 + 100).ToString("000")); }
        static YevmiyeFisi Fis(BankaHareketi h, string karsiHesap, string cariAd, string kullanici, DateTime? tarih = null)
        {
            string banka = $"{h.Banka} {h.HesapKodu} · {h.No} · {h.Referans}" + (h.PB != "TRY" ? $" · {h.DovizTutar.ToString("N2", Tr)} {h.PB} × {h.Kur.ToString("N4", Tr)} ({h.KurKaynagi} {h.KurTarihi:dd.MM.yyyy}) = TL karşılığı" : ""); var s = new List<YevmiyeSatiri>();
            if (h.Giris) { s.Add(S("102", h.Tutar, 0, banka)); s.Add(S(karsiHesap, 0, h.Tutar, (cariAd ?? h.KarsiAd) + " · " + h.Aciklama)); }
            else { s.Add(S(karsiHesap, h.Tutar, 0, (cariAd ?? h.KarsiAd) + " · " + h.Aciklama)); s.Add(S("102", 0, h.Tutar, banka)); }
            return MuhasebeOrnek.Olustur(h.DbName, "Banka Hareketi " + h.No, $"{h.IslemTuru} · {h.KarsiAd} · {h.Aciklama}", s, kullanici, tarih);
        }

        /// <summary>Otomatik eşleşme (her okumada): nakit akışla bağlanmamış çıkışlar için aynı şirkette Havale / EFT satırı — tutar ± 1 ₺, ödenme ya da planlanan tarih ± 7 gün, başka harekete bağlı değil.</summary>
        static void Senkron()
        {
            List<BankaHareketi> acik; lock (Kilit) acik = _hareketler.Where(h => h.NakitAkisDocEntry == null && !h.Giris && h.IslemTuru != "Virman").ToList();
            if (acik.Count == 0) return;
            foreach (var db in acik.Select(h => h.DbName).Distinct())
            {
                HashSet<int> bagli; lock (Kilit) bagli = new HashSet<int>(_hareketler.Where(h => h.NakitAkisDb == db && h.NakitAkisDocEntry != null).Select(h => h.NakitAkisDocEntry.Value));
                var satirlar = NakitAkisOrnek.Odemeler(db).Where(m => m.U_OdemeYontemi == "Havale / EFT" && !bagli.Contains(m.DocEntry)).ToList();
                foreach (var h in acik.Where(x => x.DbName == db))
                {
                    var aday = satirlar.Where(m => Math.Abs((m.U_TutarTL ?? 0) - h.Tutar) < 1m && Math.Abs(((m.U_OdenmeTarihi ?? m.U_PlanlananTarih ?? DateTime.MinValue).Date - h.Tarih.Date).TotalDays) <= 7).OrderBy(m => Math.Abs(((m.U_OdenmeTarihi ?? m.U_PlanlananTarih ?? DateTime.MinValue).Date - h.Tarih.Date).TotalDays)).FirstOrDefault();
                    if (aday == null) continue;
                    lock (Kilit) { h.NakitAkisDocEntry = aday.DocEntry; h.NakitAkisDb = db; h.Eslesme = "Otomatik"; h.EslesmeNotu = $"tutar ve tarih eşleşti (#{aday.DocEntry} {aday.U_Kurum})"; Log(h, "Eşleşme", $"Nakit akış #{aday.DocEntry} {aday.U_Kurum} ile otomatik eşleşti", "Sistem"); }
                    satirlar.Remove(aday); bagli.Add(aday.DocEntry);
                }
            }
        }

        // ---------- işlemler ----------
        /// <summary>Onay: cari / hesap seçilir, muhasebe fişi atılır. Nakit akışla eşleşmişse satır ÖDENDİ olur (fişi nakit akıştan gelir, iki kez atılmaz); dekont no satıra yazılır.</summary>
        public static (bool ok, string mesaj, BankaHareketi h) Onayla(int id, string cariKod, string cariAd, string karsiHesap, string not, string kullanici)
        {
            var h = Bul(id); if (h == null) return (false, "Hareket bulunamadı.", null);
            if (h.Durum == "Onaylandı") return (false, $"{h.No} zaten onaylı (fiş {h.YevmiyeNo}).", h);
            karsiHesap = string.IsNullOrWhiteSpace(karsiHesap) ? (h.Giris ? "120" : "320") : karsiHesap.Trim(); if (!KarsiHesaplar.Any(k => k.Kod == karsiHesap) && !MuhasebeOrnek.HesapPlani.ContainsKey(karsiHesap)) return (false, "Geçersiz muhasebe hesabı.", h);
            cariAd = (cariAd ?? "").Trim(); cariKod = (cariKod ?? "").Trim(); string yev = ""; var ek = new List<string>();
            if (h.NakitAkisDocEntry != null)
            {
                var m = NakitAkisOrnek.Bul(h.NakitAkisDb, h.NakitAkisDocEntry.Value);
                if (m != null)
                {
                    // cari hesaplarda (320 / 332 / 132) fiş nakit akışın kendi mantığıyla atılır (grup içi mahsup / temlik dâhil); başka hesap seçildiyse fiş onayda seçilen hesaba atılıp satıra yazılır
                    bool cariHesap = karsiHesap == "320" || karsiHesap == "332" || karsiHesap == "132";
                    if (m.U_Durum != "ÖDENDİ") { NakitAkisOrnek.OdendiIsaretle(h.NakitAkisDb, m.DocEntry, "Banka hareketi " + h.No, kullanici, cariHesap); ek.Add($"nakit akış #{m.DocEntry} ÖDENDİ oldu"); }
                    if (string.IsNullOrEmpty(m.U_DekontNo)) NakitAkisOrnek.AlanKaydet(h.NakitAkisDb, m.DocEntry, "U_DekontNo", h.No, kullanici);
                    yev = m.U_YevmiyeNo ?? ""; if (cariAd == "") cariAd = m.U_Kurum; if (cariKod == "") cariKod = CariKod(h.Sirket, m.U_Kurum, karsiHesap);
                    if (!string.IsNullOrEmpty(yev)) ek.Add($"fiş nakit akıştan ({yev}; mükerrer fiş atılmadı)");
                    else { var f = Fis(h, karsiHesap, cariAd, kullanici); yev = f.No; NakitAkisOrnek.FisBagla(h.NakitAkisDb, m.DocEntry, f.No, kullanici); ek.Add($"fiş {f.No} atıldı ({(h.Giris ? "102 / " + karsiHesap : karsiHesap + " / 102")}) ve nakit akış #{m.DocEntry} satırına yazıldı"); }
                }
            }
            if (cariAd == "") cariAd = h.KarsiAd;
            if (string.IsNullOrEmpty(yev)) { var f = Fis(h, karsiHesap, cariAd, kullanici); yev = f.No; ek.Add($"fiş {f.No} atıldı ({(h.Giris ? "102 / " + karsiHesap : karsiHesap + " / 102")})"); }
            lock (Kilit)
            {
                h.Durum = "Onaylandı"; h.Onaylayan = kullanici ?? ""; h.OnayTarihi = DateTime.Now; h.OnayNotu = (not ?? "").Trim(); h.CariKod = cariKod; h.CariAd = cariAd; h.KarsiHesap = karsiHesap; h.YevmiyeNo = yev;
                Log(h, "Onay", $"{cariAd} ({cariKod}) · {karsiHesap} {HesapAdi(karsiHesap)} · {string.Join(" · ", ek)}" + (h.OnayNotu == "" ? "" : " · " + h.OnayNotu), kullanici);
            }
            return (true, $"{h.No} onaylandı: {cariAd} · {karsiHesap} {HesapAdi(karsiHesap)} · {string.Join(" · ", ek)}.", h);
        }
        /// <summary>Nakit akışta karşılığı olmayan çıkış → Kasa Nakit Akışı'nda ÖDENDİ havale satırı (banka bilgileri, dekont = hareket no); fiş zaten atıldıysa numarası taşınır.</summary>
        public static (bool ok, string mesaj, BankaHareketi h, NakitAkisViewModel satir) NakitAkisaGonder(int id, string kullanici)
        {
            var h = Bul(id); if (h == null) return (false, "Hareket bulunamadı.", null, null);
            if (h.Giris) return (false, $"{h.No} tahsilat (giriş) hareketi: Kasa Nakit Akışı ödeme planıdır, giriş satırı açılmaz. Hareketi onaylayıp müşteri carisine (120) kaydedin.", h, null);
            if (h.NakitAkisDocEntry != null) return (false, $"{h.No} zaten nakit akış #{h.NakitAkisDocEntry} ile eşleşmiş.", h, null);
            var hesaplar = NakitAkisOrnek.BankaHesaplari; var hesap = hesaplar.FirstOrDefault(x => x.StartsWith(h.Banka + " ")) ?? hesaplar[0];
            bool virman = h.IslemTuru == "Virman"; // hesaplar arası aktarım: nakit akışa "Diğer" satırı olarak gider (karşı taraf şirketin kendi banka hesabı)
            string kurum = !string.IsNullOrEmpty(h.CariAd) ? h.CariAd : BaslikDuzelt(h.KarsiAd); string belgeTipi = h.IslemTuru == "Vergi / SGK" ? "Resmi Ödeme" : virman || h.IslemTuru == "Kira" || h.IslemTuru == "Otomatik Ödeme" ? "Diğer" : "Cari Bakiye";
            var gelen = new NakitAkisViewModel
            {
                U_Kurum = kurum, U_UrunHizmet = BaslikDuzelt(h.Aciklama), U_OdemeYontemi = "Havale / EFT", U_BagliBelgeTipi = belgeTipi, U_KayitTipi = belgeTipi == "Cari Bakiye" ? "Bakiye" : belgeTipi, U_PlanlananTarih = h.Tarih.Date, U_SonOdemeTarihi = h.Tarih.Date,
                U_Tutar = h.PB == "TRY" ? h.Tutar : h.DovizTutar, U_ParaBirimi = h.PB, U_Kur = h.PB == "TRY" ? 1 : h.Kur, U_TutarTL = h.Tutar, U_KurKaynagi = h.PB == "TRY" ? "" : (KurOrnek.Kaynaklar.Contains(h.KurKaynagi) ? h.KurKaynagi : "TCMB Döviz Alış"), U_KurTarihi = h.PB == "TRY" ? null : h.KurTarihi, U_IslemTipi = "X", U_TalepEden = "Finekra · banka hareketi", U_FaturaSahibi = h.Sirket, U_OdeyenSirket = h.Sirket,
                U_Notlar = $"Banka hareketinden alındı ({h.No} · {h.Banka} · ref {h.Referans}) · {h.IslemTuru}", U_BankaHesabi = hesap, U_DekontNo = h.No, U_AliciIban = h.KarsiIban, U_AliciHesapAdi = h.KarsiAd, U_Satinalmaci = ""
            };
            // fiş: hareket onaylıysa numarası taşınır; onaylı değilse fiş Takip Paneli onayında (cari / hesap seçilince) atılıp satıra yazılır
            var m = NakitAkisOrnek.BankaHareketindenEkle(h.DbName, gelen, h.Tarih, h.YevmiyeNo, kullanici, h.Durum == "Onaylandı");
            string fisNotu = string.IsNullOrEmpty(m.U_YevmiyeNo) ? " · fiş onayda atılır" : " · fiş " + m.U_YevmiyeNo;
            lock (Kilit) { h.NakitAkisDocEntry = m.DocEntry; h.NakitAkisDb = h.DbName; h.Eslesme = "Nakit Akışa Gönderildi"; h.EslesmeNotu = $"bu hareketten açıldı (#{m.DocEntry})"; if (string.IsNullOrEmpty(h.YevmiyeNo)) h.YevmiyeNo = m.U_YevmiyeNo ?? ""; Log(h, "Nakit Akışa Gönderildi", $"Kasa Nakit Akışı #{m.DocEntry} · {m.U_Kurum} · {h.Tutar.ToString("N2", Tr)} ₺ · ÖDENDİ{fisNotu}", kullanici); }
            return (true, $"{h.No} nakit akışa gönderildi: Kasa Nakit Akışı #{m.DocEntry} ({m.U_Kurum}, {h.Tutar.ToString("N2", Tr)} ₺, ÖDENDİ){fisNotu}.", h, m);
        }
        /// <summary>Elle eşleştirme: seçilen nakit akış satırına bağlanır; hareket onaylıysa satır ÖDENDİ olur.</summary>
        public static (bool ok, string mesaj, BankaHareketi h) Eslestir(int id, string dbName, int docEntry, string kullanici)
        {
            var h = Bul(id); if (h == null) return (false, "Hareket bulunamadı.", null);
            var m = NakitAkisOrnek.Bul(dbName, docEntry); if (m == null) return (false, "Nakit akış satırı bulunamadı.", h);
            lock (Kilit) { var baska = _hareketler.FirstOrDefault(x => x.Id != id && x.NakitAkisDb == dbName && x.NakitAkisDocEntry == docEntry); if (baska != null) return (false, $"#{docEntry} zaten {baska.No} ile eşleşmiş.", h); }
            var ek = new List<string>(); decimal fark = Math.Round((m.U_TutarTL ?? 0) - h.Tutar, 2); if (fark != 0) ek.Add($"tutar farkı {fark.ToString("N2", Tr)} ₺");
            if (h.Durum == "Onaylandı" && m.U_Durum != "ÖDENDİ") { NakitAkisOrnek.OdendiIsaretle(dbName, docEntry, "Banka hareketi " + h.No, kullanici); ek.Add($"#{docEntry} ÖDENDİ oldu"); }
            if (string.IsNullOrEmpty(m.U_DekontNo)) NakitAkisOrnek.AlanKaydet(dbName, docEntry, "U_DekontNo", h.No, kullanici);
            lock (Kilit) { h.NakitAkisDocEntry = docEntry; h.NakitAkisDb = dbName; h.Eslesme = "Elle"; h.EslesmeNotu = $"elle eşleştirildi (#{docEntry} {m.U_Kurum})" + (fark != 0 ? $" · fark {fark.ToString("N2", Tr)} ₺" : ""); if (string.IsNullOrEmpty(h.CariAd)) h.CariAd = m.U_Kurum; Log(h, "Eşleşme", $"Nakit akış #{docEntry} {m.U_Kurum} ile elle eşleştirildi" + (ek.Count > 0 ? " · " + string.Join(" · ", ek) : ""), kullanici); }
            return (true, $"{h.No} ↔ nakit akış #{docEntry} ({m.U_Kurum}) eşleştirildi" + (ek.Count > 0 ? " · " + string.Join(" · ", ek) : "") + ".", h);
        }
        public static (bool ok, string mesaj, BankaHareketi h) EslesmeKaldir(int id, string kullanici)
        {
            var h = Bul(id); if (h == null) return (false, "Hareket bulunamadı.", null); if (h.NakitAkisDocEntry == null) return (false, "Eşleşme yok.", h);
            lock (Kilit) { int d = h.NakitAkisDocEntry.Value; h.NakitAkisDocEntry = null; h.NakitAkisDb = ""; h.Eslesme = ""; h.EslesmeNotu = "eşleşme kaldırıldı"; Log(h, "Eşleşme Kaldırıldı", $"Nakit akış #{d} bağı kaldırıldı", kullanici); }
            return (true, $"{h.No} eşleşmesi kaldırıldı.", h);
        }
        /// <summary>Elle eşleştirme adayları: aynı şirkette bağlanmamış Havale / EFT satırları, tutar yakınlığına göre.</summary>
        public static List<object> Adaylar(int id)
        {
            var h = Bul(id); if (h == null) return new List<object>();
            HashSet<int> bagli; lock (Kilit) bagli = new HashSet<int>(_hareketler.Where(x => x.NakitAkisDb == h.DbName && x.NakitAkisDocEntry != null).Select(x => x.NakitAkisDocEntry.Value));
            return NakitAkisOrnek.Odemeler(h.DbName).Where(m => m.U_OdemeYontemi == "Havale / EFT" && !bagli.Contains(m.DocEntry) && !string.IsNullOrEmpty(m.U_Kurum))
                .Select(m => new { m, fark = Math.Abs((m.U_TutarTL ?? 0) - h.Tutar), gun = Math.Abs(((m.U_OdenmeTarihi ?? m.U_PlanlananTarih ?? DateTime.MinValue).Date - h.Tarih.Date).TotalDays) })
                .OrderBy(x => x.fark).ThenBy(x => x.gun).Take(15)
                .Select(x => (object)new { docEntry = x.m.DocEntry, dbName = x.m.U_KaynakFirma, kurum = x.m.U_Kurum, belge = $"{x.m.U_BagliBelgeTipi} {x.m.U_FaturaSiparisNo}".Trim(), planlanan = x.m.U_PlanlananTarih?.ToString("yyyy-MM-dd"), odenme = x.m.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), durum = x.m.U_Durum, tutarTL = x.m.U_TutarTL ?? 0, tutar = x.m.U_Tutar ?? 0, pb = x.m.U_ParaBirimi, fark = Math.Round((x.m.U_TutarTL ?? 0) - h.Tutar, 2), gun = (int)x.gun, iban = x.m.U_AliciIban, yevmiyeNo = x.m.U_YevmiyeNo }).ToList();
        }
        /// <summary>Onay penceresi için öneri: eşleşmiş nakit akış satırının carisi, cari karttan ad benzerliği ya da işlem türüne göre hesap.</summary>
        public static object Oneri(BankaHareketi h)
        {
            if (h.NakitAkisDocEntry != null) { var m = NakitAkisOrnek.Bul(h.NakitAkisDb, h.NakitAkisDocEntry.Value); if (m != null) { bool grup = CekAkibetOrnek.GrupSirketleri.Contains(m.U_Kurum ?? ""); return new { cariKod = CariKod(h.Sirket, m.U_Kurum, grup ? "332" : "320"), cariAd = m.U_Kurum, karsiHesap = grup ? "332" : "320", kaynak = $"nakit akış #{m.DocEntry}" }; } }
            string ad = BaslikDuzelt(h.KarsiAd);
            if (h.IslemTuru == "Vergi / SGK") return new { cariKod = h.KarsiAd.Contains("SGK") ? "361.001" : "360.001", cariAd = ad, karsiHesap = h.KarsiAd.Contains("SGK") ? "361" : "360", kaynak = "işlem türü" };
            if (h.IslemTuru == "Banka Masrafı") return new { cariKod = "", cariAd = ad, karsiHesap = "780", kaynak = "işlem türü" };
            if (h.IslemTuru == "Kredi Taksidi") return new { cariKod = "", cariAd = ad, karsiHesap = "300", kaynak = "işlem türü" };
            if (h.IslemTuru == "Kredi Kartı Ekstresi") return new { cariKod = "", cariAd = ad, karsiHesap = "300", kaynak = "işlem türü" };
            if (h.IslemTuru == "Virman") return new { cariKod = "", cariAd = ad, karsiHesap = "102", kaynak = "işlem türü" };
            if (h.IslemTuru == "Çek Tahsilatı") return new { cariKod = "", cariAd = ad, karsiHesap = "101", kaynak = "işlem türü" };
            var kart = CariKartOrnek.Kartlar.Where(k => k.Sirket == h.Sirket || true).FirstOrDefault(k => AdBenzer(k.KisaAd, h.KarsiAd));
            if (kart != null) return new { cariKod = kart.Kod, cariAd = kart.KisaAd, karsiHesap = kart.KartTipi == "Müşteri" ? "120" : "320", kaynak = "cari kart (ad benzerliği)" };
            var ted = CekOrnek.TedarikciAdlari.FirstOrDefault(t => AdBenzer(t, h.KarsiAd)); if (ted != null) return new { cariKod = CariKod(h.Sirket, ted, "320"), cariAd = ted, karsiHesap = "320", kaynak = "tedarikçi listesi" };
            return new { cariKod = "", cariAd = ad, karsiHesap = h.Giris ? "120" : h.IslemTuru == "Kira" || h.IslemTuru == "Otomatik Ödeme" ? "770" : "320", kaynak = "varsayılan" };
        }
        static bool AdBenzer(string a, string b) { if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false; string x = a.ToUpper(Tr), y = b.ToUpper(Tr); var ilk = x.Split(' ')[0]; return y.Contains(x) || x.Contains(y) || (ilk.Length >= 4 && y.Contains(ilk)); }
        static readonly HashSet<string> Kisaltmalar = new HashSet<string> { "GİB", "VD", "VDB", "SGK", "SGM", "A.Ş.", "A.Ş", "AŞ", "LTD.", "LTD", "ŞTİ.", "ŞTİ", "TİC.", "TİC", "SAN.", "SAN", "PAZ.", "PAZ", "EFT", "POS", "KDV", "BSMV", "TL", "TC", "T.C.", "İGDAŞ", "BEDAŞ", "AYEDAŞ", "İSKİ", "PTT", "TEB", "QNB", "ING", "HSBC", "OSB" };
        /// <summary>Bankanın büyük harfli adını başlık düzenine çevirir ("GİB YENİBOSNA VD" → "GİB Yenibosna VD"); bilinen kısaltmalar büyük kalır.</summary>
        static string BaslikDuzelt(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "";
            return string.Join(" ", s.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(k => { var u = k.ToUpper(Tr); if (Kisaltmalar.Contains(u)) return u; var l = k.ToLower(Tr); return char.ToUpper(l[0], Tr) + l.Substring(1); }));
        }
        /// <summary>Onayda seçilebilecek cariler: şirketin cari kartları (müşteri / tedarikçi) + resmi kurumlar + bankalar.</summary>
        public static List<object> Cariler(string dbName)
        {
            string sirket = CekAkibetOrnek.SirketAdi(dbName);
            var l = CariKartOrnek.Kartlar.Where(k => string.IsNullOrEmpty(dbName) || k.Sirket == sirket).Select(k => new { kod = k.Kod, ad = k.KisaAd, tip = k.KartTipi, hesap = k.KartTipi == "Müşteri" ? "120" : k.KartTipi == "Tedarikçi" ? "320" : k.KartTipi == "Finansal Kuruluşlar" ? "102" : k.KartTipi == "Resmi Kurumlar" ? "360" : "335" }).ToList();
            l.AddRange(new[] { new { kod = "360.001", ad = "Vergi Dairesi (GİB)", tip = "Resmi Kurum", hesap = "360" }, new { kod = "361.001", ad = "SGK", tip = "Resmi Kurum", hesap = "361" } });
            l.AddRange(CekAkibetOrnek.GrupSirketleri.Where(g => g != sirket).Select(g => new { kod = "332." + YansitmaOrnek.Kisa(g), ad = g, tip = "Grup Şirketi", hesap = "332" }));
            return l.GroupBy(x => x.ad).Select(g => (object)g.First()).ToList();
        }
        /// <summary>Panel günlüğüne dış kayıt (gün sonu mutabakatı adımları — Data/GunSonuOrnek.cs).</summary>
        public static void GunlukEkle(string sirket, string no, string islem, string aciklama, string kullanici) { lock (Kilit) _gunluk.Add(new FinekraGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = sirket ?? "", No = no ?? "", Islem = islem ?? "", Aciklama = aciklama ?? "" }); }
        /// <summary>Kasa / Banka / Çek hareket satırı onayı (Takip Paneli): anahtar DbKey|HesapTipi|IslemNo. gunluk=false: toplu onayda satır satır günlük yazılmaz.</summary>
        public static void HareketOnayla(string key, string durum, string not, string kullanici, bool gunluk = true)
        {
            lock (Kilit)
            {
                HareketOnaylari[key] = new HareketOnay { Durum = durum ?? "", Kullanici = kullanici ?? "", Tarih = DateTime.Now, Not = not ?? "" };
                if (!gunluk) return;
                var p = (key ?? "").Split('|'); string tip = p.Length > 1 ? p[1] : "", no = p.Length > 2 ? p[2] : key;
                string islem = durum == "ONAYLANDI" ? "Onay" : durum == "EKSİK" ? "Eksik / Kontrol" : "Onay Kaldırıldı";
                string varsayilan = durum == "ONAYLANDI" ? "Hareket onaylandı." : durum == "EKSİK" ? "Eksik / kontrol gerekli olarak işaretlendi." : "Onay kaldırıldı.";
                _gunluk.Add(new FinekraGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = CekAkibetOrnek.SirketAdi(p[0]), No = $"{(tip == "CEK" ? "Çek" : tip == "KASA" ? "Kasa" : tip == "BANKA" ? "Banka" : tip)} #{no}", Islem = islem, Aciklama = string.IsNullOrEmpty(not) ? varsayilan : not });
            }
        }
        /// <summary>Nakit akış ↔ banka eşleşme görünümü: dönemin Havale / EFT satırları ve bağlı banka hareketi.</summary>
        public static List<object> NakitAkisEslesme(string dbName, DateTime bas, DateTime bit)
        {
            Tohumla(); Senkron(); Dictionary<string, BankaHareketi> bagli; lock (Kilit) bagli = _hareketler.Where(h => h.NakitAkisDocEntry != null).GroupBy(h => h.NakitAkisDb + "|" + h.NakitAkisDocEntry).ToDictionary(g => g.Key, g => g.First());
            var dbler = string.IsNullOrEmpty(dbName) ? CekAkibetOrnek.Sirketler.Select(s => s.DbName).ToList() : new List<string> { dbName };
            return dbler.SelectMany(db => NakitAkisOrnek.Odemeler(db).Where(m => m.U_OdemeYontemi == "Havale / EFT" && m.U_Durum != "Bekliyor" && (m.U_PlanlananTarih ?? m.U_OdenmeTarihi) >= bas && (m.U_PlanlananTarih ?? m.U_OdenmeTarihi) <= bit).Select(m =>
            {
                bagli.TryGetValue(db + "|" + m.DocEntry, out var h);
                return (object)new { docEntry = m.DocEntry, dbName = db, sirket = CekAkibetOrnek.SirketAdi(db), kurum = m.U_Kurum, belge = $"{m.U_BagliBelgeTipi} {m.U_FaturaSiparisNo}".Trim(), planlanan = m.U_PlanlananTarih?.ToString("yyyy-MM-dd"), odenme = m.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), durum = m.U_Durum, tutar = m.U_Tutar ?? 0, pb = m.U_ParaBirimi, kur = m.U_Kur ?? 1, tutarTL = m.U_TutarTL ?? 0, yevmiyeNo = m.U_YevmiyeNo, dekontNo = m.U_DekontNo, bankaHesabi = m.U_BankaHesabi, iban = m.U_AliciIban, talimatNo = m.U_TalimatNo, link = $"/Rapor79?sirket={db}&sekme=havale",
                    fnk = h == null ? null : new { id = h.Id, no = h.No, tarih = h.Tarih.ToString("yyyy-MM-dd"), banka = h.Banka, tutar = h.Tutar, durum = h.Durum, eslesme = h.Eslesme, fark = Math.Round((m.U_TutarTL ?? 0) - h.Tutar, 2) } };
            })).OrderByDescending(x => ((dynamic)x).planlanan ?? "").ToList();
        }
        public static object Json(BankaHareketi h)
        {
            if (h == null) return null; NakitAkisViewModel m = h.NakitAkisDocEntry != null ? NakitAkisOrnek.Bul(h.NakitAkisDb, h.NakitAkisDocEntry.Value) : null;
            return new
            {
                id = h.Id, no = h.No, dbName = h.DbName, sirket = h.Sirket, banka = h.Banka, hesapKodu = h.HesapKodu, iban = h.Iban, tarih = h.Tarih.ToString("yyyy-MM-dd HH:mm"), gun = h.Tarih.ToString("yyyy-MM-dd"), yon = h.Yon, giris = h.Giris, tutar = h.Tutar, pb = h.PB, dovizTutar = h.DovizTutar, kur = h.Kur, kurOzet = h.PB == "TRY" ? "" : KurOrnek.Ozet(h.PB, h.KurTarihi, h.KurKaynagi, h.Kur), sap = !string.IsNullOrEmpty(h.YevmiyeNo), islemTuru = h.IslemTuru, karsiAd = h.KarsiAd, karsiIban = h.KarsiIban, aciklama = h.Aciklama, referans = h.Referans,
                durum = h.Durum, onaylayan = h.Onaylayan, onayTarihi = h.OnayTarihi?.ToString("yyyy-MM-dd HH:mm"), onayNotu = h.OnayNotu, cariKod = h.CariKod, cariAd = h.CariAd, karsiHesap = h.KarsiHesap, karsiHesapAdi = string.IsNullOrEmpty(h.KarsiHesap) ? "" : HesapAdi(h.KarsiHesap), yevmiyeNo = h.YevmiyeNo,
                eslesme = h.Eslesme, eslesmeNotu = h.EslesmeNotu, akisZamani = h.AkisZamani.ToString("yyyy-MM-dd HH:mm"), kaynak = h.Kaynak, oneri = h.Durum == "Onaylandı" ? null : Oneri(h),
                nakitAkis = m == null ? (h.NakitAkisDocEntry == null ? null : new { docEntry = h.NakitAkisDocEntry.Value, dbName = h.NakitAkisDb, bulundu = false, kurum = "", durum = "", tutarTL = 0m, planlanan = (string)null, odenme = (string)null, yevmiyeNo = "", fark = 0m, link = "" })
                    : new { docEntry = m.DocEntry, dbName = m.U_KaynakFirma, bulundu = true, kurum = m.U_Kurum, durum = m.U_Durum, tutarTL = m.U_TutarTL ?? 0, planlanan = m.U_PlanlananTarih?.ToString("yyyy-MM-dd"), odenme = m.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), yevmiyeNo = m.U_YevmiyeNo ?? "", fark = Math.Round((m.U_TutarTL ?? 0) - h.Tutar, 2), link = $"/Rapor79?sirket={m.U_KaynakFirma}&sekme=havale" },
                gunluk = h.Gunluk.OrderByDescending(g => g.Tarih).Select(g => new { tarih = g.Tarih.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, g.Islem, g.Aciklama })
            };
        }
    }
}
