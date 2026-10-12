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
        public static readonly string[] OdemeYontemleri = { "Nakit", "Çek", "Senet", "Havale / EFT", "Kredi Kartı / Banka Kartı", "Mahsup", "Temlik", "Yansıtma", "Diğer" };
        public static readonly string[] Durumlar = { "Bekliyor", "Planlandı", "Özel Onay", "ÖDENDİ", "Gecikti" };
        /// <summary>Yönteme özel alanlar (yöntem sekmesi sütunları; YontemAlanKaydet yalnız bunları yazar).</summary>
        public static readonly Dictionary<string, string[]> YontemAlanlari = new Dictionary<string, string[]>
        {
            ["Havale / EFT"] = new[] { "U_BankaHesabi", "U_AliciIban", "U_DekontNo" }, ["Nakit"] = new[] { "U_Kasa", "U_MakbuzNo", "U_TeslimAlan" },
            ["Kredi Kartı / Banka Kartı"] = new[] { "U_Kart", "U_Taksit", "U_EkstreDonemi" }, ["Mahsup"] = new[] { "U_MahsupFisNo", "U_KarsiBelge" }, ["Temlik"] = new[] { "U_TemlikNo", "U_TemlikEden" }, ["Yansıtma"] = new[] { "U_YansitmaNo" }
        };
        /// <summary>Modern ekranın satır içi / çekmece düzenlemesinde yazılabilen ana alanlar (AlanKaydet).</summary>
        public static readonly string[] AnaAlanlar = { "U_Durum", "U_PlanlananTarih", "U_Kurum", "U_UrunHizmet", "U_Tutar", "U_ParaBirimi", "U_Kur", "U_OdemeYontemi", "U_OdenmeTarihi", "U_OdenenTutar", "U_OdenenTutarPB", "U_Notlar", "U_Satinalmaci", "U_MutabakatDurumu", "U_FaturaSiparisNo", "U_IslemTipi", "U_BagliBelgeTipi", "MuafiyetSebebi" };
        public static readonly string[] Satinalmacilar = { "Tolga Yaman", "Gizem Tan", "Canan Su", "Mert Doğan" };
        public static readonly string[] BankaHesaplari = { "Ziraat Bankası · TR33 0001 0001 2345 6789 0123 45", "Halk Bankası · TR12 0001 2009 4560 0010 2600 01", "İş Bankası · TR64 0006 4000 0011 2340 5678 90", "Garanti BBVA · TR22 0006 2000 1230 0006 2971 50" };
        public static readonly string[] Kasalar = { "Merkez Kasa", "Fabrika Kasa" };
        static readonly string[] Urunler = { "Hammadde alımı", "Ambalaj malzemesi", "Nakliye hizmeti", "Bakım onarım", "Danışmanlık", "Sigorta primi", "Elektrik faturası", "Doğalgaz faturası", "Yemek hizmeti", "Gümrük müşavirliği", "Reklam ve tanıtım", "Laboratuvar sarf", "Makine yedek parça", "Personel servis", "Yazılım lisansı", "Katalizör alımı" };
        public static readonly string[] Kartlar = { "Garanti Bonus **** 4512", "İş Bankası Maximum **** 8821", "Ziraat Bankkart **** 1907" };
        public static readonly string[] Taksitler = { "Tek Çekim", "Tek Çekim", "3 Taksit", "6 Taksit" };
        public static readonly string[] TaksitSecenekleri = { "Tek Çekim", "2 Taksit", "3 Taksit", "6 Taksit", "9 Taksit", "12 Taksit" };
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
                    // 3) Grup içi modüllerle CANLI bağlı satırlar: Mahsup (Data/MahsupOrnek.cs), Temlik (Data/TemlikOrnek.cs), Yansıtma (Data/YansitmaOrnek.cs) kayıtları —
                    //    kurum / tutar / tarih kayıttan; durum kaydın aşamasından (muhasebeleşti → ÖDENDİ, onaylandı → Planlandı, diğer → Bekliyor); okuma anında GrupIciBilgi ile güncel durum gelir
                    DateTime PlanTarihi(DateTime t) { var p = t.AddDays(10); return p < bugun.AddDays(-28) ? bugun.AddDays(-rnd.Next(0, 25)) : p; }
                    foreach (var mh in MahsupOrnek.Islemler.Where(x => x.Sirket == s.Ad && x.Aktif).OrderByDescending(x => x.Tarih).Take(5))
                    {
                        var m = Yeni(PlanTarihi(mh.Tarih), "Mahsup", mh.Kapsam == "Grup Şirketleri Arası" && !string.IsNullOrEmpty(mh.KarsiSirket) ? mh.KarsiSirket : (mh.AlacakCari ?? mh.BorcCari ?? "Mahsup"), mh.MahsupTutari > 0 ? mh.MahsupTutari : Math.Round((decimal)(rnd.NextDouble() * 400_000 + 50_000) / 1000m) * 1000m, mh.ParaBirimi ?? "TRY");
                        m.U_MahsupFisNo = mh.No; m.U_KarsiBelge = mh.Alacaklar.FirstOrDefault()?.BelgeNo ?? mh.Kalemler.FirstOrDefault()?.BelgeNo ?? ""; m.U_UrunHizmet = mh.TurAd + " · " + (mh.Neden ?? mh.NedenKodu ?? "mahsup"); m.U_BagliBelgeTipi = "Mahsup"; m.U_KayitTipi = "Bakiye"; m.U_FaturaSiparisNo = mh.No;
                        GrupIciDurum(m, mh.Durum == "Muhasebeleşti" || mh.Durum == "Mutabakat Oluştu" || mh.Durum == "Uyumlu", mh.Durum == "Onaylandı" || mh.Durum == "Kısmi Entegrasyon", mh.GercekTarihi); liste.Add(m);
                    }
                    foreach (var tm in TemlikOrnek.Islemler.Where(x => (x.KaynakSirket == s.Ad || x.Taraflar.Any(t => t.Ad == s.Ad)) && x.Aktif).OrderByDescending(x => x.Tarih).Take(4))
                    {
                        var m = Yeni(PlanTarihi(tm.EtkinlikTarihi == default ? tm.Tarih : tm.EtkinlikTarihi), "Temlik", tm.NihaiAlan ?? "Temlik Alan", tm.NihaiTutar > 0 ? tm.NihaiTutar : tm.BuTemlikTutari > 0 ? tm.BuTemlikTutari : Math.Round((decimal)(rnd.NextDouble() * 600_000 + 80_000) / 1000m) * 1000m, tm.ParaBirimi ?? "TRY");
                        m.U_TemlikNo = tm.No; m.U_TemlikEden = tm.IlkEden ?? ""; m.U_UrunHizmet = tm.Tur + " · " + (tm.Neden ?? tm.NedenKodu ?? "temlik"); m.U_BagliBelgeTipi = "Temlik"; m.U_KayitTipi = "Bakiye"; m.U_FaturaSiparisNo = tm.No;
                        GrupIciDurum(m, tm.Durum == "Tamamlandı" || tm.MuhasebeDurumu == "Tamamlandı", tm.Durum == "Onaylandı" || tm.Durum == "Tam İmzalı" || tm.Durum == "Kısmi Muhasebeleşti" || tm.Durum == "Belge Üretildi" || tm.Durum == "İmzaya Gönderildi" || tm.Durum == "Kısmi İmzalı", tm.GercekTarihi); liste.Add(m);
                    }
                    foreach (var yn in YansitmaOrnek.Islemler.Where(x => (x.Yansitan == s.Ad || x.Yansitilan == s.Ad) && x.Aktif).OrderByDescending(x => x.Tarih).Take(5))
                    {
                        var m = Yeni(PlanTarihi(yn.Tarih), "Yansıtma", yn.Yansitan == s.Ad ? yn.Yansitilan : yn.Yansitan, yn.GenelToplam > 0 ? Math.Round(yn.GenelToplam, 2) : Math.Round((decimal)(rnd.NextDouble() * 300_000 + 30_000) / 1000m) * 1000m, yn.ParaBirimi ?? "TRY");
                        m.U_YansitmaNo = yn.No; m.U_UrunHizmet = yn.Tur + " yansıtması · " + (yn.Aciklama ?? yn.KaynakFaturaNo ?? ""); m.U_BagliBelgeTipi = "Yansıtma"; m.U_KayitTipi = "Fatura"; m.U_FaturaSiparisNo = yn.YansitmaFaturaNo ?? yn.No;
                        m.U_Notlar = yn.Yansitan == s.Ad ? "Yansıtan taraf: karşı şirketten tahsil edilecek" : "Yansıtılan taraf: grup şirketine ödenecek";
                        GrupIciDurum(m, yn.Durum == "Muhasebeleşti" || yn.Durum == "Uyumlu", yn.Durum == "Onaylandı" || yn.Durum == "Fatura Oluştu", yn.GercekTarihi); liste.Add(m);
                    }
                    // 4) Diğer yöntemler (Mahsup / Temlik / Yansıtma yukarıda modül kayıtlarından)
                    int n = 14 + (int)Math.Round(50 * olcek);
                    for (int i = 0; i < n; i++)
                    {
                        var plan = bugun.AddDays(-30 + rnd.Next(0, 91)); double z = rnd.NextDouble();
                        string yontem = z < 0.66 ? "Havale / EFT" : z < 0.80 ? "Nakit" : z < 0.93 ? "Kredi Kartı / Banka Kartı" : "Diğer";
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
                    void GrupIciDurum(NakitAkisViewModel m, bool odendi, bool planli, DateTime? gercek)
                    {
                        if (odendi) { m.U_Durum = "ÖDENDİ"; m.U_OdenmeTarihi = gercek ?? m.U_PlanlananTarih; m.U_OdenenTutar = m.U_Tutar; m.U_MutabakatDurumu = "Mutabık"; m.SonMutabakatTarihi = (m.U_OdenmeTarihi ?? bugun).AddDays(-2); }
                        else if (planli) { m.U_Durum = m.U_PlanlananTarih < bugun ? "Gecikti" : "Planlandı"; if (m.U_Durum == "Planlandı") { m.U_MutabakatDurumu = "Mutabık"; m.SonMutabakatTarihi = bugun.AddDays(-rnd.Next(1, 15)); } }
                        else m.U_Durum = "Bekliyor";
                        m.U_EskiDurum = m.U_Durum;
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
        /// <summary>Modern ekran: ana alan ya da yöntem alanı tek tek kaydı (satır içi düzenleme). Tarih yyyy-MM-dd, tutar noktalı ondalık.</summary>
        public static (bool ok, string mesaj) AlanKaydet(string dbName, int docEntry, string alan, string deger, string kullanici)
        {
            Tohumla();
            bool yontemAlani = YontemAlanlari.Values.SelectMany(x => x).Contains(alan ?? ""); if (!yontemAlani && !AnaAlanlar.Contains(alan ?? "")) return (false, "Geçersiz alan.");
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                var p = typeof(NakitAkisViewModel).GetProperty(alan); var t = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType; object v;
                try
                {
                    if (t == typeof(string)) v = deger ?? "";
                    else if (string.IsNullOrWhiteSpace(deger)) v = null;
                    else if (t == typeof(DateTime)) v = DateTime.Parse(deger, CultureInfo.InvariantCulture);
                    else if (t == typeof(decimal)) v = decimal.Parse(deger.Replace(',', '.'), CultureInfo.InvariantCulture);
                    else if (t == typeof(int)) v = int.Parse(deger);
                    else v = Convert.ChangeType(deger, t, CultureInfo.InvariantCulture);
                }
                catch { return (false, $"{alan} için geçersiz değer: {deger}"); }
                if (v == null && p.PropertyType == typeof(string)) v = "";
                if (alan == "U_Durum" && !Durumlar.Contains((string)v)) return (false, "Geçersiz durum.");
                if (alan == "U_OdemeYontemi" && !OdemeYontemleri.Contains((string)v)) return (false, "Geçersiz ödeme yöntemi.");
                object eski = p.GetValue(m); p.SetValue(m, v);
                if (alan == "U_Tutar" || alan == "U_Kur" || alan == "U_ParaBirimi") { if (alan == "U_ParaBirimi") m.U_Kur = Kurlar.TryGetValue(m.U_ParaBirimi ?? "TRY", out var kk) ? kk : m.U_Kur; m.U_TutarTL = Math.Round((m.U_Tutar ?? 0) * (m.U_Kur ?? 1), 2); }
                if (alan == "U_Durum") { if (m.U_Durum == "ÖDENDİ") { m.U_OdenmeTarihi ??= DateTime.Today; if ((m.U_OdenenTutar ?? 0) == 0) m.U_OdenenTutar = m.U_Tutar; } m.U_EskiDurum = m.U_Durum; }
                Log(dbName, docEntry, alan == "U_Durum" ? "Durum" : "Güncelleme", $"{alan}: {(eski is DateTime ed ? ed.ToString("dd.MM.yyyy") : eski)} → {(v is DateTime vd ? vd.ToString("dd.MM.yyyy") : v)}", kullanici);
                return (true, "Kaydedildi.");
            }
        }
        /// <summary>Grup içi kaydın (Mahsup / Temlik / Yansıtma) güncel durumu — ilgili modülden okuma anında (ekranlar canlı bağlı).</summary>
        public static object GrupIciBilgi(NakitAkisViewModel m)
        {
            if (m.U_OdemeYontemi == "Mahsup" && !string.IsNullOrEmpty(m.U_MahsupFisNo))
            {
                var r = MahsupOrnek.Bul(m.U_MahsupFisNo); if (r == null) return new { tur = "Mahsup", no = m.U_MahsupFisNo, link = "/GrupIci/Mahsup", durum = "Kayıt bulunamadı", bulundu = false };
                return new { tur = "Mahsup", no = r.No, link = "/GrupIci/Mahsup?no=" + Uri.EscapeDataString(r.No), bulundu = true, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.TurAd + " · " + r.Kapsam, karsi = !string.IsNullOrEmpty(r.KarsiSirket) ? r.KarsiSirket : r.AlacakCari, taraflar = $"{r.BorcCari} ↔ {r.AlacakCari}", tutar = r.MahsupTutari, pb = r.ParaBirimi, durum = r.Durum, muhasebe = r.MuhasebeDurumu, kontrol = r.KontrolDurumu ?? "", ek = $"{r.BelgeSayisi} belge · {r.TamKapanan} tam / {r.KismiKapanan} kısmi kapanan · kur farkı {r.KurFarki:N2}", onay = r.Onaylandi, duzenlenebilir = r.Duzenlenebilir };
            }
            if (m.U_OdemeYontemi == "Temlik" && !string.IsNullOrEmpty(m.U_TemlikNo))
            {
                var r = TemlikOrnek.Bul(m.U_TemlikNo); if (r == null) return new { tur = "Temlik", no = m.U_TemlikNo, link = "/GrupIci/Temlik", durum = "Kayıt bulunamadı", bulundu = false };
                return new { tur = "Temlik", no = r.No, link = "/GrupIci/Temlik?no=" + Uri.EscapeDataString(r.No), bulundu = true, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.Tur, karsi = r.NihaiAlan, taraflar = $"{r.IlkEden} → {r.NihaiAlan}", tutar = r.NihaiTutar > 0 ? r.NihaiTutar : r.BuTemlikTutari, pb = r.ParaBirimi, durum = r.Durum, muhasebe = r.MuhasebeDurumu, kontrol = r.KontrolDurumu ?? "", ek = $"asıl borçlu {r.AsilBorclu} · belge {r.BelgeDurumu} · imza {r.ImzaDurumu} · {r.StepSayisi} adım", imza = r.ImzaDurumu, belgeDurumu = r.BelgeDurumu, onay = r.Onaylandi, duzenlenebilir = r.Duzenlenebilir };
            }
            if (m.U_OdemeYontemi == "Yansıtma" && !string.IsNullOrEmpty(m.U_YansitmaNo))
            {
                var r = YansitmaOrnek.Bul(m.U_YansitmaNo); if (r == null) return new { tur = "Yansıtma", no = m.U_YansitmaNo, link = "/GrupIci/Yansitma", durum = "Kayıt bulunamadı", bulundu = false };
                return new { tur = "Yansıtma", no = r.No, link = "/GrupIci/Yansitma?no=" + Uri.EscapeDataString(r.No), bulundu = true, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.Tur + " yansıtması", karsi = r.Yansitilan, taraflar = $"{r.Yansitan} → {r.Yansitilan}", tutar = r.GenelToplam, pb = r.ParaBirimi, durum = r.Durum, muhasebe = r.Entegrasyon?.Durum == "TAMAM" || !string.IsNullOrEmpty(r.YansitmaFaturaNo) ? "Fatura " + r.YansitmaFaturaNo : r.Entegrasyon?.Durum ?? "Bekliyor", kontrol = r.KontrolDurumu ?? "", ek = $"kaynak {r.KaynakFaturaNo} · {r.KaynakTedarikci} · yansıtma {r.ToplamYansitma:N2} + KDV {r.Kdv:N2} · kâr %{r.AgirlikliKarPct}", kaynakFatura = r.KaynakFaturaNo, onay = r.Onaylandi, duzenlenebilir = r.Durum == "Taslak" };
            }
            return null;
        }
        /// <summary>Seçilebilir grup içi kayıtlar (modülden canlı): aktif, henüz bir nakit akış satırına bağlanmamış.</summary>
        public static List<object> GrupIciKayitlar(string dbName, string tur)
        {
            Tohumla(); string sirket = SirketAdi(dbName); List<string> bagli;
            lock (Kilit) bagli = _odemeler.Values.SelectMany(l => l).Select(m => tur == "Mahsup" ? m.U_MahsupFisNo : tur == "Temlik" ? m.U_TemlikNo : m.U_YansitmaNo).Where(x => !string.IsNullOrEmpty(x)).ToList();
            if (tur == "Mahsup") return MahsupOrnek.Islemler.Where(r => r.Aktif && (r.Sirket == sirket || r.KarsiSirket == sirket)).OrderByDescending(r => r.Tarih).Select(r => (object)new { no = r.No, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.TurAd + " · " + r.Kapsam, karsi = !string.IsNullOrEmpty(r.KarsiSirket) ? r.KarsiSirket : r.AlacakCari, taraflar = $"{r.BorcCari} ↔ {r.AlacakCari}", tutar = r.MahsupTutari, pb = r.ParaBirimi, durum = r.Durum, muhasebe = r.MuhasebeDurumu, link = "/GrupIci/Mahsup?no=" + Uri.EscapeDataString(r.No), bagli = bagli.Contains(r.No) }).ToList();
            if (tur == "Temlik") return TemlikOrnek.Islemler.Where(r => r.Aktif && (r.KaynakSirket == sirket || r.Taraflar.Any(t => t.Ad == sirket))).OrderByDescending(r => r.Tarih).Select(r => (object)new { no = r.No, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.Tur, karsi = r.NihaiAlan, taraflar = $"{r.IlkEden} → {r.NihaiAlan}", tutar = r.NihaiTutar > 0 ? r.NihaiTutar : r.BuTemlikTutari, pb = r.ParaBirimi, durum = r.Durum, muhasebe = r.MuhasebeDurumu + " · imza " + r.ImzaDurumu, link = "/GrupIci/Temlik?no=" + Uri.EscapeDataString(r.No), bagli = bagli.Contains(r.No) }).ToList();
            return YansitmaOrnek.Islemler.Where(r => r.Aktif && (r.Yansitan == sirket || r.Yansitilan == sirket)).OrderByDescending(r => r.Tarih).Select(r => (object)new { no = r.No, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.Tur + " yansıtması", karsi = r.Yansitan == sirket ? r.Yansitilan : r.Yansitan, taraflar = $"{r.Yansitan} → {r.Yansitilan}", tutar = r.GenelToplam, pb = r.ParaBirimi, durum = r.Durum, muhasebe = string.IsNullOrEmpty(r.YansitmaFaturaNo) ? "Fatura yok" : "Fatura " + r.YansitmaFaturaNo, link = "/GrupIci/Yansitma?no=" + Uri.EscapeDataString(r.No), bagli = bagli.Contains(r.No) }).ToList();
        }
        /// <summary>Nakit akış satırını grup içi kayda bağlar: kurum, tutar, tarih kayıttan; yöntem kaydın türü.</summary>
        public static (bool ok, string mesaj, NakitAkisViewModel satir) GrupIciBagla(string dbName, int docEntry, string tur, string no, string kullanici)
        {
            Tohumla(); string sirket = SirketAdi(dbName);
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.", null);
                if (tur == "Mahsup") { var r = MahsupOrnek.Bul(no); if (r == null) return (false, "Mahsup kaydı bulunamadı.", m); m.U_OdemeYontemi = "Mahsup"; m.U_MahsupFisNo = r.No; m.U_KarsiBelge = r.Alacaklar.FirstOrDefault()?.BelgeNo ?? ""; m.U_Kurum = !string.IsNullOrEmpty(r.KarsiSirket) ? r.KarsiSirket : r.AlacakCari; if (r.MahsupTutari > 0) { m.U_Tutar = r.MahsupTutari; m.U_ParaBirimi = r.ParaBirimi; m.U_Kur = r.Kur; m.U_TutarTL = Math.Round(r.MahsupTutari * r.Kur, 2); } m.U_PlanlananTarih ??= r.Tarih; m.U_UrunHizmet = r.TurAd + " · " + (r.Neden ?? ""); m.U_FaturaSiparisNo = r.No; m.U_BagliBelgeTipi = "Mahsup"; }
                else if (tur == "Temlik") { var r = TemlikOrnek.Bul(no); if (r == null) return (false, "Temlik kaydı bulunamadı.", m); m.U_OdemeYontemi = "Temlik"; m.U_TemlikNo = r.No; m.U_TemlikEden = r.IlkEden ?? ""; m.U_Kurum = r.NihaiAlan ?? m.U_Kurum; decimal t = r.NihaiTutar > 0 ? r.NihaiTutar : r.BuTemlikTutari; if (t > 0) { m.U_Tutar = t; m.U_ParaBirimi = r.ParaBirimi; m.U_Kur = r.Kur; m.U_TutarTL = Math.Round(t * r.Kur, 2); } m.U_PlanlananTarih ??= r.EtkinlikTarihi == default ? r.Tarih : r.EtkinlikTarihi; m.U_UrunHizmet = r.Tur + " · " + (r.Neden ?? ""); m.U_FaturaSiparisNo = r.No; m.U_BagliBelgeTipi = "Temlik"; }
                else if (tur == "Yansıtma") { var r = YansitmaOrnek.Bul(no); if (r == null) return (false, "Yansıtma kaydı bulunamadı.", m); m.U_OdemeYontemi = "Yansıtma"; m.U_YansitmaNo = r.No; m.U_Kurum = r.Yansitan == sirket ? r.Yansitilan : r.Yansitan; if (r.GenelToplam > 0) { m.U_Tutar = Math.Round(r.GenelToplam, 2); m.U_ParaBirimi = r.ParaBirimi; m.U_Kur = r.Kur; m.U_TutarTL = Math.Round(r.GenelToplam * r.Kur, 2); } m.U_PlanlananTarih ??= r.Tarih; m.U_UrunHizmet = r.Tur + " yansıtması · " + (r.Aciklama ?? ""); m.U_FaturaSiparisNo = r.YansitmaFaturaNo ?? r.No; m.U_BagliBelgeTipi = "Yansıtma"; }
                else return (false, "Geçersiz tür.", m);
                if (m.U_Durum == "Bekliyor") { m.U_Durum = "Planlandı"; m.U_EskiDurum = "Planlandı"; }
                Log(dbName, docEntry, "Grup İçi Bağlandı", $"{tur} {no} satıra bağlandı · {m.U_Kurum} · {m.U_Tutar:N2} {m.U_ParaBirimi}", kullanici);
                return (true, $"{tur} kaydı {no} satıra bağlandı; kurum, tutar ve tarih kayıttan alındı.", m);
            }
        }
        public static (bool ok, string mesaj, NakitAkisViewModel satir) GrupIciKaldir(string dbName, int docEntry, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.", null);
                string eski = m.U_MahsupFisNo + m.U_TemlikNo + m.U_YansitmaNo; m.U_MahsupFisNo = ""; m.U_TemlikNo = ""; m.U_TemlikEden = ""; m.U_YansitmaNo = ""; m.U_KarsiBelge = "";
                Log(dbName, docEntry, "Grup İçi Bağ Kaldırıldı", eski, kullanici); return (true, "Kayıt bağı kaldırıldı.", m);
            }
        }
        /// <summary>Çekmece: dekont ekleme simülasyonu (dosya saklanmaz).</summary>
        public static (bool ok, string mesaj, NakitAkisViewModel satir) DekontEkle(string dbName, int docEntry, string ad, string aciklama, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.", null);
                string dosya = string.IsNullOrWhiteSpace(ad) ? $"dekont_{docEntry}_{m.DekontListesi.Count + 1}.pdf" : ad.Trim();
                m.DekontListesi.Add(new DekontKaydi { Id = ++_sonDekontId, SirketDb = dbName, DocEntry = docEntry, DosyaAdi = dosya, DosyaYolu = "/Dekontlar/" + dosya, Aciklama = aciklama ?? "", Yukleyen = kullanici ?? "", Tarih = DateTime.Now });
                Log(dbName, docEntry, "Dekont", dosya + (string.IsNullOrEmpty(aciklama) ? "" : " · " + aciklama), kullanici); return (true, "Dekont eklendi (ön yüz örneği: dosya saklanmaz).", m);
            }
        }
        /// <summary>Grup içi özet (Mahsup / Temlik / Yansıtma modüllerinden canlı): açık ve onay bekleyen işlem sayısı / tutarı.</summary>
        public static object GrupIciOzet(string dbName)
        {
            string sirket = string.IsNullOrEmpty(dbName) ? null : SirketAdi(dbName);
            var mh = MahsupOrnek.Islemler.Where(r => r.Aktif && (sirket == null || r.Sirket == sirket || r.KarsiSirket == sirket)).ToList();
            var tm = TemlikOrnek.Islemler.Where(r => r.Aktif && (sirket == null || r.KaynakSirket == sirket || r.Taraflar.Any(t => t.Ad == sirket))).ToList();
            var yn = YansitmaOrnek.Islemler.Where(r => r.Aktif && (sirket == null || r.Yansitan == sirket || r.Yansitilan == sirket)).ToList();
            return new
            {
                mahsup = new { toplam = mh.Count, acik = mh.Count(r => r.Durum != "Muhasebeleşti" && r.Durum != "Mutabakat Oluştu" && r.Durum != "Uyumlu"), acikTutar = mh.Where(r => r.Durum != "Muhasebeleşti" && r.Durum != "Mutabakat Oluştu" && r.Durum != "Uyumlu").Sum(r => r.MahsupTutari), onayBekleyen = mh.Count(r => r.Durum == "Onay Bekliyor"), kontrol = mh.Count(r => !string.IsNullOrEmpty(r.KontrolDurumu) && r.KontrolDurumu != "Uyumlu"), link = "/GrupIci/Mahsup" },
                temlik = new { toplam = tm.Count, acik = tm.Count(r => r.Durum != "Tamamlandı"), acikTutar = tm.Where(r => r.Durum != "Tamamlandı").Sum(r => r.NihaiTutar > 0 ? r.NihaiTutar : r.BuTemlikTutari), onayBekleyen = tm.Count(r => r.Durum == "Onay Bekliyor"), imzaBekleyen = tm.Count(r => r.Durum == "İmzaya Gönderildi" || r.Durum == "Kısmi İmzalı"), link = "/GrupIci/Temlik" },
                yansitma = new { toplam = yn.Count, acik = yn.Count(r => r.Durum != "Muhasebeleşti" && r.Durum != "Uyumlu"), acikTutar = yn.Where(r => r.Durum != "Muhasebeleşti" && r.Durum != "Uyumlu").Sum(r => r.GenelToplam), onayBekleyen = yn.Count(r => r.Durum == "Onay Bekliyor"), faturaBekleyen = yn.Count(r => r.Durum == "Onaylandı"), link = "/GrupIci/Yansitma" },
                rapor = "/GrupIci/Rapor"
            };
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
