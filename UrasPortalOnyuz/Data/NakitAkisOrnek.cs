using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>
    /// KASA NAKİT AKIŞI (Rapor79) örnek verisi — şirket bazında ödeme planı satırları (NakitAkisViewModel) ve kasa / banka / portföy bakiyeleri.
    /// Çek / Senet yöntemli satırlar Data/CekAkibetOrnek.cs kayıtlarına bağlıdır: satıra belge seçilince belge "Planlandı" olur (planlanan tedarikçi = satırın kurumu);
    /// belgenin gerçek çıkışı (ciro / bankaya ibraz) Çek Akıbeti Raporu ve Konsolide Finansal Takip Paneli'nde planla karşılaştırılır.
    /// Tohum senaryoları: planlı belgeler için Çek / Senet satırları (uyumlu çıkanlar ÖDENDİ, farklı yere çıkanlar hâlâ Planlandı / Gecikti → kontrol örneği),
    /// belge seçilmemiş Çek / Senet satırları (Bekliyor; "Belge Seç" ile planlanır) ve diğer yöntemler (Havale / EFT, Nakit, Kredi Kartı, Mahsup, Temlik, Diğer) yönteme özel alanlarıyla.
    /// Bellek içi; uygulama yeniden başlayınca sıfırlanır.
    /// </summary>
    public static class NakitAkisOrnek
    {
        public static readonly object Kilit = new object();
        public static readonly string[] OdemeYontemleri = { "Nakit", "Çek", "Senet", "Havale / EFT", "Kredi Kartı / Banka Kartı", "Mahsup", "Temlik", "Diğer" };
        public static readonly string[] Durumlar = { "Bekliyor", "Planlandı", "Özel Onay", "ÖDENDİ", "Gecikti" };
        /// <summary>Yönteme özel alanlar (yöntem sekmesi sütunları; YontemAlanKaydet yalnız bunları yazar).</summary>
        public static readonly Dictionary<string, string[]> YontemAlanlari = new Dictionary<string, string[]>
        {
            ["Havale / EFT"] = new[] { "U_BankaHesabi", "U_AliciIban", "U_DekontNo" }, ["Nakit"] = new[] { "U_Kasa", "U_MakbuzNo", "U_TeslimAlan" },
            ["Kredi Kartı / Banka Kartı"] = new[] { "U_Kart", "U_Taksit", "U_EkstreDonemi" }, ["Mahsup"] = new[] { "U_MahsupFisNo", "U_KarsiBelge" }, ["Temlik"] = new[] { "U_TemlikNo", "U_TemlikEden" }
        };
        public static readonly string[] Satinalmacilar = { "Tolga Yaman", "Gizem Tan", "Canan Su", "Mert Doğan" };
        public static readonly string[] BankaHesaplari = { "Ziraat Bankası · TR33 0001 0001 2345 6789 0123 45", "Halk Bankası · TR12 0001 2009 4560 0010 2600 01", "İş Bankası · TR64 0006 4000 0011 2340 5678 90", "Garanti BBVA · TR22 0006 2000 1230 0006 2971 50" };
        public static readonly string[] Kasalar = { "Merkez Kasa", "Fabrika Kasa" };
        static readonly string[] Urunler = { "Hammadde alımı", "Ambalaj malzemesi", "Nakliye hizmeti", "Bakım onarım", "Danışmanlık", "Sigorta primi", "Elektrik faturası", "Doğalgaz faturası", "Yemek hizmeti", "Gümrük müşavirliği", "Reklam ve tanıtım", "Laboratuvar sarf", "Makine yedek parça", "Personel servis", "Yazılım lisansı", "Katalizör alımı" };
        static readonly string[] Kartlar = { "Garanti Bonus **** 4512", "İş Bankası Maximum **** 8821", "Ziraat Bankkart **** 1907" };
        static readonly string[] Taksitler = { "Tek Çekim", "Tek Çekim", "3 Taksit", "6 Taksit" };
        static readonly string[] TeslimAlanlar = { "Kurye", "Tedarikçi yetkilisi", "Şoför", "Muhasebe elemanı" };
        public static readonly Dictionary<string, decimal> Kurlar = new Dictionary<string, decimal> { ["TRY"] = 1m, ["USD"] = 41.20m, ["EUR"] = 47.90m, ["GBP"] = 55.10m, ["CHF"] = 51.60m };
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

        private static readonly Dictionary<string, List<NakitAkisViewModel>> _odemeler = new Dictionary<string, List<NakitAkisViewModel>>();
        private static readonly Dictionary<string, List<KasaBakiyeViewModel>> _bakiyeler = new Dictionary<string, List<KasaBakiyeViewModel>>();
        private static readonly List<NakitAkisLog> _gunluk = new List<NakitAkisLog>();
        private static int _sonDocEntry = 1000, _sonLogId = 0, _sonDekontId = 0;
        private static bool _tohumlandi;
        private static int Seed(string s) => s.Aggregate(29, (a, ch) => unchecked(a * 31 + ch));

        public static List<(string Ad, string DbName, string DbKey)> Sirketler => CekAkibetOrnek.Sirketler.ToList();
        public static string GorunenAd(string dbName) => CekAkibetOrnek.SirketAdi(dbName).ToUpper(Tr);
        public static string SirketAdi(string dbName) => CekAkibetOrnek.SirketAdi(dbName);

        /// <summary>Şirketin ödeme satırları (kopya; seçilen belgelerin akıbeti Çek Akıbeti kaydından güncel okunur).</summary>
        public static List<NakitAkisViewModel> Odemeler(string dbName) { Tohumla(); List<NakitAkisViewModel> l; lock (Kilit) l = (_odemeler.TryGetValue(dbName, out var x) ? x : new List<NakitAkisViewModel>()).ToList(); foreach (var m in l) BelgeleriDoldur(m); return l; }
        public static NakitAkisViewModel Bul(string dbName, int docEntry) { Tohumla(); lock (Kilit) return _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(m => m.DocEntry == docEntry) : null; }
        public static List<KasaBakiyeViewModel> Bakiyeler(string dbName) { Tohumla(); lock (Kilit) return _bakiyeler.TryGetValue(dbName, out var l) ? l.ToList() : new List<KasaBakiyeViewModel>(); }
        public static List<NakitAkisLog> Gunluk(string dbName, int? docEntry = null) { Tohumla(); lock (Kilit) return _gunluk.Where(g => (string.IsNullOrEmpty(dbName) || g.DbName == dbName) && (docEntry == null || g.DocEntry == docEntry)).OrderByDescending(g => g.IslemTarihi).ToList(); }
        public static List<CekKaydi> SecilebilirBelgeler(string dbName, string belge) => CekAkibetOrnek.Secilebilirler(dbName, string.IsNullOrEmpty(belge) ? "Çek" : belge);

        /// <summary>Satırın seçili belgelerini Çek Akıbeti kaydından okur (akıbet, ibraz yeri, kontrol).</summary>
        public static void BelgeleriDoldur(NakitAkisViewModel m)
        {
            var idler = (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => int.TryParse(s.Trim(), out var n) ? n : 0).Where(n => n > 0).ToList();
            var kayitlar = idler.Count == 0 ? new List<CekKaydi>() : CekAkibetOrnek.BulHepsi(idler);
            m.Belgeler = idler.Select(id => kayitlar.FirstOrDefault(k => k.DocEntry == id)).Where(k => k != null)
                .Select(k => new NakitAkisBelge { CekDocEntry = k.DocEntry, No = k.No, Belge = k.Belge, Banka = k.Banka, AsilBorclu = k.AsilBorclu, KimdenGeldi = k.Musteri, Vade = k.Vade, Tutar = k.Tutar, PB = k.PB, Akibet = k.IslemTuru, IbrazYeri = k.IbrazYeri, Kontrol = k.Kontrol, PlanlananTedarikci = k.PlanlananTedarikci }).ToList();
            m.U_CekNolar = string.Join(", ", m.Belgeler.Select(b => b.No));
        }

        private static void Log(string dbName, int docEntry, string tip, string aciklama, string kullanici) => _gunluk.Add(new NakitAkisLog { LogId = ++_sonLogId, IslemTarihi = DateTime.Now, Kullanici = kullanici ?? "", DbName = dbName, DocEntry = docEntry, IslemTipi = tip, Aciklama = aciklama });

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var bugun = DateTime.Today; var tedarikciler = CekOrnek.TedarikciAdlari;
                foreach (var s in CekAkibetOrnek.Sirketler)
                {
                    var rnd = new Random(Seed("nakit|" + s.DbName)); decimal olcek = ButceOrnek.Olcek.TryGetValue(s.Ad, out var o) ? o : 0.3m;
                    var liste = new List<NakitAkisViewModel>();
                    var kartlar = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Tedarikçi" && k.Sirket == s.Ad).ToList();
                    NakitAkisViewModel Yeni(DateTime plan, string yontem, string kurum, decimal tutar, string pb)
                    {
                        decimal kur = Kurlar.TryGetValue(pb, out var kk) ? kk : 1m; string belgeTipi = rnd.NextDouble() < 0.7 ? "Fatura" : rnd.NextDouble() < 0.5 ? "Sipariş" : "Manuel";
                        var m = new NakitAkisViewModel
                        {
                            DocEntry = ++_sonDocEntry, DocNum = _sonDocEntry, U_PlanlananTarih = plan, BelgeTarihi = plan.AddDays(-rnd.Next(15, 60)), U_Kurum = kurum, U_UrunHizmet = Urunler[rnd.Next(Urunler.Length)],
                            U_ParaBirimi = pb, U_Tutar = tutar, U_Kur = kur, U_TutarTL = Math.Round(tutar * kur, 2), U_OdemeYontemi = yontem, U_OdenenTutarPB = pb, U_KaynakFirma = s.DbName,
                            U_BagliBelgeTipi = belgeTipi, U_KayitTipi = belgeTipi == "Manuel" ? "Bakiye" : belgeTipi, U_BagliDocEntry = belgeTipi == "Manuel" ? null : 40000 + rnd.Next(1, 9999),
                            U_FaturaSiparisNo = belgeTipi == "Fatura" ? $"FT-{plan.Year}-{rnd.Next(10000, 99999)}" : belgeTipi == "Sipariş" ? $"SP-{plan.Year}-{rnd.Next(1000, 9999)}" : "",
                            U_IslemTipi = rnd.NextDouble() < 0.7 ? "X" : rnd.NextDouble() < 0.85 ? "R" : "Y", U_Satinalmaci = Satinalmacilar[rnd.Next(Satinalmacilar.Length)], U_Durum = "Bekliyor", U_Notlar = ""
                        };
                        m.U_EskiDurum = m.U_Durum; return m;
                    }
                    void DurumVer(NakitAkisViewModel m, DateTime plan)
                    {
                        double z = rnd.NextDouble();
                        if (plan < bugun.AddDays(-3)) { if (z < 0.85) { m.U_Durum = "ÖDENDİ"; m.U_OdenmeTarihi = plan.AddDays(rnd.Next(0, 4)); m.U_OdenenTutar = m.U_Tutar; } else m.U_Durum = "Gecikti"; }
                        else if (plan <= bugun.AddDays(7)) m.U_Durum = z < 0.6 ? "Planlandı" : z < 0.85 ? "Bekliyor" : "Özel Onay";
                        else m.U_Durum = z < 0.55 ? "Bekliyor" : "Planlandı";
                        if (m.U_Durum == "Planlandı" && rnd.NextDouble() < 0.6) { m.U_MutabakatDurumu = "Mutabık"; m.SonMutabakatTarihi = bugun.AddDays(-rnd.Next(1, 30)); }
                        else if (m.U_Durum == "ÖDENDİ") { m.U_MutabakatDurumu = "Mutabık"; m.SonMutabakatTarihi = m.U_OdenmeTarihi.Value.AddDays(-rnd.Next(1, 10)); }
                        m.U_EskiDurum = m.U_Durum;
                    }
                    void YontemAlanlariDoldur(NakitAkisViewModel m)
                    {
                        bool odendi = m.U_Durum == "ÖDENDİ"; string y = m.U_OdemeYontemi;
                        if (y == "Havale / EFT") { m.U_BankaHesabi = BankaHesaplari[rnd.Next(BankaHesaplari.Length)]; m.U_AliciIban = $"TR{rnd.Next(10, 99)} {rnd.Next(1000, 9999)} {rnd.Next(1000, 9999)} {rnd.Next(1000, 9999)} {rnd.Next(1000, 9999)} {rnd.Next(1000, 9999)} {rnd.Next(10, 99)}"; m.U_DekontNo = odendi ? $"DK-{m.U_OdenmeTarihi:yyMMdd}-{rnd.Next(100, 999)}" : ""; }
                        else if (y == "Nakit") { m.U_Kasa = Kasalar[rnd.Next(Kasalar.Length)]; m.U_MakbuzNo = odendi ? $"MK-{rnd.Next(10000, 99999)}" : ""; m.U_TeslimAlan = odendi ? TeslimAlanlar[rnd.Next(TeslimAlanlar.Length)] : ""; }
                        else if (y == "Kredi Kartı / Banka Kartı") { m.U_Kart = Kartlar[rnd.Next(Kartlar.Length)]; m.U_Taksit = Taksitler[rnd.Next(Taksitler.Length)]; var d = m.U_PlanlananTarih ?? bugun; m.U_EkstreDonemi = ButceOrnek.Aylar[d.Month - 1] + " " + d.Year; }
                        else if (y == "Mahsup") { m.U_MahsupFisNo = $"MH-{(m.U_PlanlananTarih ?? bugun):yyMM}-{rnd.Next(100, 999)}"; m.U_KarsiBelge = $"Satış Faturası SF-{(m.U_PlanlananTarih ?? bugun).Year}-{rnd.Next(10000, 99999)}"; }
                        else if (y == "Temlik") { var musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri").ToList(); m.U_TemlikNo = $"TM-{(m.U_PlanlananTarih ?? bugun):yyMM}-{rnd.Next(100, 999)}"; m.U_TemlikEden = musteriler.Count > 0 ? musteriler[rnd.Next(musteriler.Count)].KisaAd : "Müşteri"; }
                    }
                    // 1) Çek / Senet satırları: Çek Akıbeti kaydındaki plan senaryoları (tedarikçi + plan günü bazında 1–3 belge)
                    var planli = CekAkibetOrnek.Kayitlar.Where(k => k.PlanSirket == s.Ad && k.PlanlananTedarikci != "" && k.PlanlamaTarihi != null).OrderBy(k => k.PlanlamaTarihi).ThenBy(k => k.Vade).ToList();
                    foreach (var grup in planli.GroupBy(k => k.PlanlananTedarikci + "|" + k.PlanlamaTarihi.Value.Date.ToString("yyyyMMdd") + "|" + k.Belge))
                    {
                        foreach (var parca in grup.Select((k, i) => (k, i)).GroupBy(x => x.i / 3).Select(g => g.Select(x => x.k).ToList()))
                        {
                            var ilk = parca[0]; bool cikmis = parca.Any(k => k.IbrazYeri != ""), uyumlu = parca.All(k => k.Kontrol == "Uyumlu");
                            var plan = cikmis ? parca.Min(k => k.IbrazTarihi ?? k.Vade) : parca.Min(k => k.Vade);
                            var m = Yeni(plan, ilk.Belge, ilk.PlanlananTedarikci, parca.Sum(k => k.Tutar), "TRY");
                            m.BelgeTarihi = ilk.PlanlamaTarihi.Value.AddDays(-rnd.Next(5, 25)); m.U_CekDocEntryler = string.Join(",", parca.Select(k => k.DocEntry)); m.U_Notlar = "Belge(ler): " + string.Join(", ", parca.Select(k => k.No));
                            if (cikmis && uyumlu) { m.U_Durum = "ÖDENDİ"; m.U_OdenmeTarihi = plan; m.U_OdenenTutar = m.U_Tutar; m.U_MutabakatDurumu = "Mutabık"; m.SonMutabakatTarihi = plan.AddDays(-3); }
                            else { m.U_Durum = plan < bugun ? "Gecikti" : "Planlandı"; if (cikmis) m.U_Notlar += " · DİKKAT: belge plandan farklı yere çıktı (Konsolide Finansal Takip Paneli › Çek)"; if (m.U_Durum == "Planlandı" && rnd.NextDouble() < 0.5) { m.U_MutabakatDurumu = "Mutabık"; m.SonMutabakatTarihi = bugun.AddDays(-rnd.Next(1, 20)); } }
                            m.U_EskiDurum = m.U_Durum; BelgeleriDoldur(m); liste.Add(m);
                            foreach (var k in parca) CekAkibetOrnek.PlanKaynakBagla(k.DocEntry, m.DocEntry);
                        }
                    }
                    // 2) Belge seçilmemiş Çek / Senet satırları (Bekliyor): ekrandan "Belge Seç" ile planlanır
                    int bosBelgeli = 3 + (int)Math.Round(4 * olcek);
                    for (int i = 0; i < bosBelgeli; i++)
                    {
                        var plan = bugun.AddDays(rnd.Next(3, 50)); var kurum = tedarikciler[rnd.Next(tedarikciler.Length)];
                        var m = Yeni(plan, rnd.NextDouble() < 0.75 ? "Çek" : "Senet", kurum, Math.Round((decimal)(rnd.NextDouble() * 1_400_000 + 150_000) / 1000m) * 1000m, "TRY");
                        m.U_Durum = "Bekliyor"; m.U_EskiDurum = "Bekliyor"; m.U_Notlar = "Belge seçilecek"; liste.Add(m);
                    }
                    // 3) Diğer yöntemler
                    int n = 14 + (int)Math.Round(50 * olcek);
                    for (int i = 0; i < n; i++)
                    {
                        var plan = bugun.AddDays(-30 + rnd.Next(0, 91)); double z = rnd.NextDouble();
                        string yontem = z < 0.58 ? "Havale / EFT" : z < 0.70 ? "Nakit" : z < 0.80 ? "Kredi Kartı / Banka Kartı" : z < 0.88 ? "Mahsup" : z < 0.94 ? "Temlik" : "Diğer";
                        string pb = rnd.NextDouble() < 0.85 ? "TRY" : rnd.NextDouble() < 0.65 ? "USD" : "EUR";
                        decimal tutar = pb == "TRY" ? Math.Round((decimal)Math.Exp(10.5 + rnd.NextDouble() * 3.2) / 100m) * 100m : Math.Round((decimal)(rnd.NextDouble() * 60_000 + 2_000) / 100m) * 100m;
                        if (yontem == "Nakit") tutar = Math.Min(tutar, 150_000m); if (yontem == "Kredi Kartı / Banka Kartı") tutar = Math.Min(tutar, 250_000m);
                        var kart = kartlar.Count > 0 && rnd.NextDouble() < 0.35 ? kartlar[rnd.Next(kartlar.Count)] : null;
                        var m = Yeni(plan, yontem, kart != null ? kart.KisaAd : tedarikciler[rnd.Next(tedarikciler.Length)], tutar, pb);
                        DurumVer(m, plan); YontemAlanlariDoldur(m);
                        if (m.U_Durum == "ÖDENDİ" && yontem == "Havale / EFT" && rnd.NextDouble() < 0.5) m.DekontListesi.Add(new DekontKaydi { Id = ++_sonDekontId, SirketDb = s.DbName, DocEntry = m.DocEntry, DosyaAdi = $"dekont_{m.DocEntry}.pdf", DosyaYolu = $"/Dekontlar/dekont_{m.DocEntry}.pdf", Aciklama = "Banka dekontu", Yukleyen = "Kerem Aksoy", Tarih = m.U_OdenmeTarihi.Value.AddHours(11) });
                        if (m.U_Durum == "ÖDENDİ" && rnd.NextDouble() < 0.1) m.MuafiyetSebebi = "Fatura (Elektrik/Su/Doğalgaz)";
                        liste.Add(m);
                    }
                    liste = liste.OrderBy(m => m.U_PlanlananTarih).ThenBy(m => m.DocEntry).ToList(); for (int i = 0; i < liste.Count; i++) liste[i].U_Sira = i + 1;
                    _odemeler[s.DbName] = liste;
                    foreach (var m in liste.Where(x => x.U_Durum == "ÖDENDİ")) Log(s.DbName, m.DocEntry, "Ödeme", $"{m.U_Kurum} · {m.U_OdemeYontemi} · {m.U_OdenenTutar:N2} {m.U_OdenenTutarPB} ödendi", "Kerem Aksoy");
                    foreach (var m in liste.Where(x => x.U_CekDocEntryler != "")) Log(s.DbName, m.DocEntry, "Belge Planlandı", $"{m.U_Kurum} için {m.U_CekNolar}", "Kerem Aksoy");
                    // 4) bakiyeler: banka hesapları (BANKA), nakit kasa (KASA), portföydeki müşteri çekleri / senetleri (projeksiyonda "Müşteri Çekleri")
                    var cekler = CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == s.Ad && k.Portfoyde).ToList();
                    var bak = new List<KasaBakiyeViewModel>
                    {
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "BANKA", Detay = "Ziraat Bankası · Vadesiz TL", PB = "TRY", Tutar = Math.Round(18_500_000m * olcek / 1000m) * 1000m },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "BANKA", Detay = "Halk Bankası · Vadesiz TL", PB = "TRY", Tutar = Math.Round(9_200_000m * olcek / 1000m) * 1000m },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "BANKA", Detay = "İş Bankası · USD Hesabı", PB = "USD", Tutar = Math.Round(420_000m * olcek / 100m) * 100m },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "BANKA", Detay = "Garanti BBVA · EUR Hesabı", PB = "EUR", Tutar = Math.Round(180_000m * olcek / 100m) * 100m },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "KASA", Detay = "Merkez Kasa · Nakit", PB = "TRY", Tutar = Math.Round(850_000m * olcek / 1000m) * 1000m },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "ÇEK PORTFÖYÜ", Detay = "MÜŞTERİ ÇEKLERİ (Portföyde + Planlandı)", PB = "TRY", Tutar = cekler.Where(k => k.Belge == "Çek").Sum(k => k.Tutar) },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "SENET PORTFÖYÜ", Detay = "MÜŞTERİ SENETLERİ (Portföyde + Planlandı)", PB = "TRY", Tutar = cekler.Where(k => k.Belge == "Senet").Sum(k => k.Tutar) }
                    };
                    foreach (var b in bak) b.TutarTL = Math.Round(b.Tutar * (Kurlar.TryGetValue(b.PB, out var kk) ? kk : 1m), 2);
                    _bakiyeler[s.DbName] = bak;
                }
            }
        }

        // ---------- işlemler ----------
        static readonly string[] KopyalananAlanlar = { "U_Sira", "U_Renk", "U_KayitTipi", "U_FaturaSiparisNo", "U_PlanlananTarih", "U_Kurum", "U_Satinalmaci", "U_UrunHizmet", "U_ParaBirimi", "U_Kur", "U_Tutar", "U_TutarTL", "U_OdemeYontemi", "U_OdenmeTarihi", "U_OdenenTutar", "U_OdenenTutarPB", "U_Durum", "U_Notlar", "U_BagliBelgeTipi", "U_BagliDocEntry", "U_IslemTipi", "U_MutabakatDurumu" };
        /// <summary>Satır güncelleme (AjaxRowGuncelle / Guncelle): yalnız ekrandan gelen alanlar; belge bağları ve yönteme özel alanlar korunur.</summary>
        public static bool Guncelle(string dbName, NakitAkisViewModel gelen, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == gelen.DocEntry) : null; if (m == null) return false;
                string eskiDurum = m.U_Durum; var t = typeof(NakitAkisViewModel);
                foreach (var ad in KopyalananAlanlar) { var p = t.GetProperty(ad); var v = p.GetValue(gelen); if (v == null && p.PropertyType == typeof(string)) continue; if (p.PropertyType == typeof(string) && (string)v == "" && (ad == "U_Kurum" || ad == "U_OdemeYontemi")) continue; p.SetValue(m, v); }
                if (m.U_ParaBirimi == "EURO") m.U_ParaBirimi = "EUR";
                if (m.U_Tutar.HasValue && (m.U_TutarTL ?? 0) == 0) m.U_TutarTL = Math.Round(m.U_Tutar.Value * (m.U_Kur ?? 1), 2);
                if (m.U_Durum == "ÖDENDİ" && m.U_OdenmeTarihi == null) m.U_OdenmeTarihi = DateTime.Today;
                if (m.U_Durum == "ÖDENDİ" && (m.U_OdenenTutar ?? 0) == 0) m.U_OdenenTutar = m.U_Tutar;
                m.U_EskiDurum = m.U_Durum;
                Log(dbName, m.DocEntry, eskiDurum != m.U_Durum ? "Durum" : "Güncelleme", eskiDurum != m.U_Durum ? $"{eskiDurum} → {m.U_Durum}" : "Satır güncellendi", kullanici);
                return true;
            }
        }
        public static NakitAkisViewModel Ekle(string dbName, NakitAkisViewModel gelen, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                if (!_odemeler.ContainsKey(dbName)) _odemeler[dbName] = new List<NakitAkisViewModel>();
                var m = new NakitAkisViewModel { DocEntry = ++_sonDocEntry, U_KaynakFirma = dbName, U_Durum = "Bekliyor", BelgeTarihi = DateTime.Today, U_ParaBirimi = "TRY", U_Kur = 1, U_OdenenTutarPB = "TRY", U_Notlar = "" };
                var t = typeof(NakitAkisViewModel); foreach (var ad in KopyalananAlanlar) { var p = t.GetProperty(ad); var v = p.GetValue(gelen); if (v != null && !(v is string sv && sv == "")) p.SetValue(m, v); }
                m.DocNum = m.DocEntry; if (m.U_ParaBirimi == "EURO") m.U_ParaBirimi = "EUR"; if (string.IsNullOrEmpty(m.U_Durum)) m.U_Durum = "Bekliyor"; m.U_EskiDurum = m.U_Durum;
                if ((m.U_TutarTL ?? 0) == 0 && m.U_Tutar.HasValue) m.U_TutarTL = Math.Round(m.U_Tutar.Value * (m.U_Kur ?? 1), 2);
                if (string.IsNullOrEmpty(m.U_BagliBelgeTipi)) m.U_BagliBelgeTipi = "Manuel"; m.U_Sira = _odemeler[dbName].Count + 1;
                _odemeler[dbName].Add(m); Log(dbName, m.DocEntry, "Ekleme", $"{m.U_Kurum} · {m.U_OdemeYontemi} · {m.U_Tutar:N2} {m.U_ParaBirimi}", kullanici);
                return m;
            }
        }
        public static bool Sil(string dbName, int docEntry, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return false;
                foreach (var id in (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)) if (int.TryParse(id, out var n)) CekAkibetOrnek.PlanKaldir(n, kullanici);
                l.Remove(m); Log(dbName, docEntry, "Silme", $"{m.U_Kurum} · {m.U_Tutar:N2} {m.U_ParaBirimi} silindi", kullanici); return true;
            }
        }
        /// <summary>Çek / Senet sekmesinden belge seçimi: belgeler Çek Akıbeti'nde "Planlandı" olur; satır tutarı belge toplamı, vade en erken belge vadesi.</summary>
        public static (bool ok, string mesaj, NakitAkisViewModel satir) BelgePlanla(string dbName, int docEntry, IEnumerable<int> cekDocEntryler, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.", null);
                var mevcut = (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList();
                var eklenen = new List<string>(); var hatalar = new List<string>();
                foreach (var id in (cekDocEntryler ?? Enumerable.Empty<int>()).Distinct())
                {
                    if (mevcut.Contains(id)) continue;
                    var r = CekAkibetOrnek.Planla(id, m.U_Kurum, m.DocEntry, SirketAdi(dbName), kullanici);
                    if (r.ok) { mevcut.Add(id); eklenen.Add(CekAkibetOrnek.Bul(id)?.No ?? id.ToString()); } else hatalar.Add(r.mesaj);
                }
                m.U_CekDocEntryler = string.Join(",", mevcut); BelgeleriDoldur(m);
                if (m.Belgeler.Count > 0)
                {
                    var belge = m.Belgeler[0].Belge; if (m.U_OdemeYontemi != "Çek" && m.U_OdemeYontemi != "Senet") m.U_OdemeYontemi = belge;
                    m.U_Tutar = m.Belgeler.Sum(b => b.Tutar); m.U_ParaBirimi = "TRY"; m.U_Kur = 1; m.U_TutarTL = m.U_Tutar; m.U_OdenenTutarPB = "TRY";
                    if (m.U_PlanlananTarih == null || m.U_PlanlananTarih > m.Belgeler.Min(b => b.Vade)) m.U_PlanlananTarih = m.Belgeler.Min(b => b.Vade);
                    if (m.U_Durum == "Bekliyor") { m.U_Durum = "Planlandı"; m.U_EskiDurum = "Planlandı"; }
                    string not = "Belge(ler): " + string.Join(", ", m.Belgeler.Select(b => b.No)); var parcalar = (m.U_Notlar ?? "").Split(" | ", StringSplitOptions.RemoveEmptyEntries).Where(p => !p.StartsWith("Belge(ler):") && p != "Belge seçilecek").ToList(); parcalar.Insert(0, not); m.U_Notlar = string.Join(" | ", parcalar);
                }
                if (eklenen.Count > 0) Log(dbName, m.DocEntry, "Belge Planlandı", $"{m.U_Kurum} için {string.Join(", ", eklenen)} seçildi → Çek Akıbeti: Planlandı", kullanici);
                string mesaj = (eklenen.Count > 0 ? $"{string.Join(", ", eklenen)} planlandı → {m.U_Kurum}. Çek Akıbeti Raporu'nda işlem türü \"Planlandı\" oldu." : "") + (hatalar.Count > 0 ? " " + string.Join(" ", hatalar) : "");
                return (eklenen.Count > 0, mesaj.Trim(), m);
            }
        }
        public static (bool ok, string mesaj, NakitAkisViewModel satir) BelgePlanKaldir(string dbName, int docEntry, int cekDocEntry, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.", null);
                var mevcut = (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.Parse(x)).ToList(); if (!mevcut.Contains(cekDocEntry)) return (false, "Belge bu satırda değil.", m);
                var r = CekAkibetOrnek.PlanKaldir(cekDocEntry, kullanici); if (!r.ok) return (false, r.mesaj, m);
                mevcut.Remove(cekDocEntry); m.U_CekDocEntryler = string.Join(",", mevcut); BelgeleriDoldur(m);
                if (m.Belgeler.Count > 0) { m.U_Tutar = m.Belgeler.Sum(b => b.Tutar); m.U_TutarTL = m.U_Tutar; m.U_Notlar = "Belge(ler): " + string.Join(", ", m.Belgeler.Select(b => b.No)); }
                else { m.U_Notlar = "Belge seçilecek"; if (m.U_Durum == "Planlandı") { m.U_Durum = "Bekliyor"; m.U_EskiDurum = "Bekliyor"; } }
                Log(dbName, m.DocEntry, "Belge Plan Kaldırıldı", r.mesaj, kullanici); return (true, r.mesaj, m);
            }
        }
        /// <summary>Yöntem sekmesindeki alan (IBAN, dekont no, kasa, makbuz, kart, taksit, mahsup fişi, temlik …) kaydı.</summary>
        public static (bool ok, string mesaj) YontemAlanKaydet(string dbName, int docEntry, string alan, string deger, string kullanici)
        {
            Tohumla();
            if (!YontemAlanlari.Values.SelectMany(x => x).Contains(alan ?? "")) return (false, "Geçersiz alan.");
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                typeof(NakitAkisViewModel).GetProperty(alan).SetValue(m, deger ?? ""); Log(dbName, docEntry, "Yöntem Alanı", $"{alan} = {deger}", kullanici); return (true, "Kaydedildi.");
            }
        }
        /// <summary>Portföyden toplu ekleme: her belge için ayrı Çek / Senet satırı (kurum sonra girilir), belge planlanır.</summary>
        public static int TopluBelgeEkle(string dbName, IEnumerable<int> cekDocEntryler, string kullanici)
        {
            Tohumla(); int n = 0;
            foreach (var id in (cekDocEntryler ?? Enumerable.Empty<int>()).Distinct())
            {
                var k = CekAkibetOrnek.Bul(id); if (k == null || !k.Secilebilir) continue;
                var m = Ekle(dbName, new NakitAkisViewModel { U_PlanlananTarih = k.Vade, U_Kurum = "Tedarikçi Belirlenecek", U_UrunHizmet = $"{k.Belge} ile ödeme", U_ParaBirimi = "TRY", U_Tutar = k.Tutar, U_Kur = 1, U_TutarTL = k.Tutar, U_OdemeYontemi = k.Belge, U_Durum = "Bekliyor", U_BagliBelgeTipi = "Manuel", U_KayitTipi = "Bakiye", U_IslemTipi = "X" }, kullanici);
                var r = BelgePlanla(dbName, m.DocEntry, new[] { id }, kullanici); if (r.ok) n++;
            }
            return n;
        }
    }

    /// <summary>Kasa Nakit Akışı satır / sistem günlüğü (GetRowLogs, GetSistemLoglari).</summary>
    public class NakitAkisLog { public int LogId { get; set; } public DateTime IslemTarihi { get; set; } public string Kullanici { get; set; } public string DbName { get; set; } public int DocEntry { get; set; } public string IslemTipi { get; set; } public string Aciklama { get; set; } }
}
