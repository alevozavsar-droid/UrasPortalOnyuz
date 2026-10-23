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
            ["Havale / EFT"] = new[] { "U_BankaHesabi", "U_AliciIban", "U_DekontNo", "U_AliciBanka", "U_AliciHesapAdi" }, ["Nakit"] = new[] { "U_Kasa", "U_MakbuzNo", "U_TeslimAlan" },
            ["Kredi Kartı / Banka Kartı"] = new[] { "U_Kart", "U_Taksit", "U_EkstreDonemi" }, ["Mahsup"] = new[] { "U_MahsupFisNo", "U_KarsiBelge" }, ["Temlik"] = new[] { "U_TemlikNo", "U_TemlikEden" }, ["Yansıtma"] = new[] { "U_YansitmaNo" }
        };
        /// <summary>Modern ekranın satır içi / çekmece düzenlemesinde yazılabilen ana alanlar (AlanKaydet).</summary>
        public static readonly string[] AnaAlanlar = { "U_Durum", "U_PlanlananTarih", "U_Kurum", "U_UrunHizmet", "U_Tutar", "U_ParaBirimi", "U_Kur", "U_OdemeYontemi", "U_OdenmeTarihi", "U_OdenenTutar", "U_OdenenTutarPB", "U_Notlar", "U_Satinalmaci", "U_MutabakatDurumu", "U_FaturaSiparisNo", "U_IslemTipi", "U_BagliBelgeTipi", "MuafiyetSebebi", "U_FaturaSahibi", "U_OdeyenSirket", "U_SonOdemeTarihi", "U_FaturaTutari", "U_TeslimAlan", "U_TalepEden", "U_KurKaynagi", "U_KurTarihi", "U_KurNotu", "U_KurFarkiFaturasi", "U_GrupIciTur" };
        /// <summary>Nakit ödeme onaycıları: nakit, ancak bu kişilerden biri onaylarsa verilebilir (talep → onay → makbuz).</summary>
        public static readonly string[] NakitOnaycilari = { "Alev Özavşar", "Neslihan Uludağ", "Özge İsen" };
        /// <summary>Plansız çek çıkışı onaycısı (GMY): Kasa Nakit Akışı'nda planlanmamış hiçbir çek, onaylanmadan şirketten çıkmaz.</summary>
        public static readonly string[] CekCikisOnaycilari = { "Alev Özavşar" };
        public static readonly string[] BelgeTipleri = { "Fatura", "Sipariş", "Cari Bakiye", "Resmi Ödeme", "Avans", "Mahsup", "Temlik", "Yansıtma", "Diğer" };   // Resmi Ödeme: vergi, SGK, gümrük, harç (muhasebeden gelen); Diğer: kira vb.
        public static readonly string[] Satinalmacilar = { "Tolga Yaman", "Gizem Tan", "Canan Su", "Mert Doğan" };
        public static readonly string[] BankaHesaplari = { "Ziraat Bankası · TR33 0001 0001 2345 6789 0123 45", "Halk Bankası · TR12 0001 2009 4560 0010 2600 01", "İş Bankası · TR64 0006 4000 0011 2340 5678 90", "Garanti BBVA · TR22 0006 2000 1230 0006 2971 50" };
        public static readonly string[] Kasalar = { "Merkez Kasa", "Fabrika Kasa" };
        public static readonly string[] AliciBankalari = { "Garanti BBVA", "İş Bankası", "Yapı Kredi", "Akbank", "Ziraat Bankası", "QNB", "Halk Bankası", "Vakıfbank", "Denizbank" };
        /// <summary>Satır ekleri (çekmece: dosya yüklenir, bellek içinde tutulur): dekont, imzalı makbuz / talimat, ciro bordrosu, çek fotokopisi.</summary>
        public static readonly string[] EkTurleri = { "Dekont", "İmzalı Makbuz", "İmzalı Talimat", "Ciro Bordrosu", "Çek Fotokopisi", "Diğer" };
        /// <summary>Tedarikçi banka bilgisi (cari kart / son ödemeden; ön yüz örneğinde cari adından deterministik): banka, IBAN, hesap adı — banka talimatına yazılır.</summary>
        public static (string banka, string iban, string hesapAdi) TedarikciBanka(string kurum)
        {
            if (string.IsNullOrWhiteSpace(kurum)) return ("", "", "");
            var r = new Random(Seed("banka|" + kurum.Trim().ToUpper(Tr)));
            var kart = CariKartOrnek.Kartlar.FirstOrDefault(k => string.Equals(k.KisaAd, kurum.Trim(), StringComparison.OrdinalIgnoreCase));
            string banka = AliciBankalari[r.Next(AliciBankalari.Length)];
            string iban = $"TR{r.Next(10, 99)} {r.Next(1000, 9999)} {r.Next(1000, 9999)} {r.Next(1000, 9999)} {r.Next(1000, 9999)} {r.Next(1000, 9999)} {r.Next(10, 99)}";
            return (banka, iban, kart?.ResmiUnvan ?? kurum.Trim());
        }
        /// <summary>Cari kart vade şartından gün ("Vade 60 Gün" → 60; yoksa 30).</summary>
        public static int VadeGunu(string vadeSarti) { var d = new string((vadeSarti ?? "").Where(char.IsDigit).ToArray()); return int.TryParse(d, out var n) && n > 0 && n <= 365 ? n : 30; }
        /// <summary>Cari bilgisi (Yeni Ödeme / çekmece: cari girilince vade şartı → son ödeme tarihi ve banka bilgisi otomatik gelir; çek talebinde doğru vadeli çek seçilsin).</summary>
        public static object CariBilgi(string dbName, string kurum)
        {
            Tohumla(); string sirket = SirketAdi(dbName);
            var kart = CariKartOrnek.Kartlar.Where(k => string.Equals(k.KisaAd, (kurum ?? "").Trim(), StringComparison.OrdinalIgnoreCase)).OrderByDescending(k => k.Sirket == sirket).FirstOrDefault();
            var tb = TedarikciBanka(kurum); int vadeGun = VadeGunu(kart?.VadeSarti);
            List<NakitAkisViewModel> son; lock (Kilit) son = _odemeler.Values.SelectMany(l => l).Where(m => string.Equals(m.U_Kurum, (kurum ?? "").Trim(), StringComparison.OrdinalIgnoreCase)).OrderByDescending(m => m.U_OdenmeTarihi ?? m.U_PlanlananTarih ?? DateTime.MinValue).ToList();
            var sonHavale = son.FirstOrDefault(m => !string.IsNullOrEmpty(m.U_AliciIban));
            return new
            {
                kurum = (kurum ?? "").Trim(), kod = kart?.Kod, unvan = kart?.ResmiUnvan ?? (kurum ?? "").Trim(), kartTipi = kart?.KartTipi, vadeSarti = kart?.VadeSarti ?? $"Vade {vadeGun} Gün (varsayılan)", vadeGun, sonOdeme = DateTime.Today.AddDays(vadeGun).ToString("yyyy-MM-dd"),
                banka = sonHavale?.U_AliciBanka ?? tb.banka, iban = sonHavale?.U_AliciIban ?? tb.iban, hesapAdi = sonHavale?.U_AliciHesapAdi ?? tb.hesapAdi, bankaKaynagi = sonHavale != null ? $"son ödeme kaydı #{sonHavale.DocEntry}" : kart != null ? "cari kart" : "örnek",
                sonOdemeTarihi = son.FirstOrDefault(m => m.U_Durum == "ÖDENDİ")?.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), acikSatir = son.Count(m => m.U_Durum != "ÖDENDİ"), acikTutarTL = son.Where(m => m.U_Durum != "ÖDENDİ").Sum(m => m.U_TutarTL ?? 0)
            };
        }
        static readonly string[] Urunler = { "Hammadde alımı", "Ambalaj malzemesi", "Nakliye hizmeti", "Bakım onarım", "Danışmanlık", "Sigorta primi", "Elektrik faturası", "Doğalgaz faturası", "Yemek hizmeti", "Gümrük müşavirliği", "Reklam ve tanıtım", "Laboratuvar sarf", "Makine yedek parça", "Personel servis", "Yazılım lisansı", "Katalizör alımı" };
        public static readonly string[] Kartlar = { "Garanti Bonus **** 4512", "İş Bankası Maximum **** 8821", "Ziraat Bankkart **** 1907" };
        public static readonly string[] Taksitler = { "Tek Çekim", "Tek Çekim", "3 Taksit", "6 Taksit" };
        public static readonly string[] TaksitSecenekleri = { "Tek Çekim", "2 Taksit", "3 Taksit", "6 Taksit", "9 Taksit", "12 Taksit" };
        static readonly string[] TeslimAlanlar = { "Kurye", "Tedarikçi yetkilisi", "Şoför", "Muhasebe elemanı" };
        public static readonly Dictionary<string, decimal> Kurlar = new Dictionary<string, decimal> { ["TRY"] = 1m, ["USD"] = 41.20m, ["EUR"] = 47.90m, ["GBP"] = 55.10m, ["CHF"] = 51.60m, ["AED"] = 11.20m };
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

        /// <summary>Şirketin ödeme satırları (kopya; seçilen belgelerin akıbeti Çek Akıbeti kaydından, onaylı avanslar Nakit Avans modülünden güncel okunur).</summary>
        public static List<NakitAkisViewModel> Odemeler(string dbName) { Tohumla(); AvanslariSenkronla(); List<NakitAkisViewModel> l; lock (Kilit) l = (_odemeler.TryGetValue(dbName, out var x) ? x : new List<NakitAkisViewModel>()).ToList(); foreach (var m in l) BelgeleriDoldur(m); return l; }
        public static NakitAkisViewModel Bul(string dbName, int docEntry) { Tohumla(); AvanslariSenkronla(); lock (Kilit) return _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(m => m.DocEntry == docEntry) : null; }
        public static NakitAkisViewModel Bul(int docEntry) { Tohumla(); lock (Kilit) return _odemeler.Values.SelectMany(l => l).FirstOrDefault(m => m.DocEntry == docEntry); }
        /// <summary>Dış modüller (Talimat) için günlük kaydı.</summary>
        public static void Gunluge(string dbName, int docEntry, string tip, string aciklama, string kullanici) { Tohumla(); lock (Kilit) Log(dbName, docEntry, tip, aciklama, kullanici); }

        // ---------- NAKİT AVANS senkronu: onaylanan avanslar otomatik satır olur; ödenince avans modülünde teslim edilmiş sayılır (Bekleyen Ödemeler listesinden düşer) ----------
        static string AvansSirketi(NakitAvans a) => a.Sirket == "URS Makina" ? "URS Makine" : a.Sirket;
        public static void AvanslariSenkronla()
        {
            lock (Kilit)
            {
                foreach (var a in NakitAvansOrnek.Talepler.ToList())
                {
                    string sirket = AvansSirketi(a), db = CekAkibetOrnek.DbName(sirket); if (!_odemeler.ContainsKey(db)) _odemeler[db] = new List<NakitAkisViewModel>();
                    var m = _odemeler[db].FirstOrDefault(x => x.U_AvansNo == a.No);
                    bool uygun = a.Onaylandi && a.OnaylananTutar > 0 && (a.OdemeBekleyen > 0 || (a.IlkTeslim != null && a.IlkTeslim >= DateTime.Today.AddDays(-60)));
                    if (m == null && !uygun) continue;
                    if (m != null && a.TalepDurumu == "Reddedildi") { _odemeler[db].Remove(m); Log(db, m.DocEntry, "Avans", $"{a.No} reddedildi; satır kaldırıldı", "Sistem"); continue; }
                    decimal kur = Kurlar.TryGetValue(a.ParaBirimi == "TL" ? "TRY" : a.ParaBirimi, out var kk) ? kk : 1m; string pb = a.ParaBirimi == "TL" ? "TRY" : a.ParaBirimi;
                    if (m == null)
                    {
                        var plan = a.KullanimTarihi < DateTime.Today ? DateTime.Today : a.KullanimTarihi.Date; bool sahsi = NakitAvansOrnek.SahsiMi(a);
                        // şahsi (maaş) avansında personel adı ve konu ekranda gizlenir; makbuz belgesinde teslim alan gerçek ad kalır
                        m = new NakitAkisViewModel
                        {
                            DocEntry = ++_sonDocEntry, DocNum = _sonDocEntry, U_KaynakFirma = db, U_FaturaSahibi = AvansSirketi(a) == a.Kullanan.Sirket ? sirket : (a.Kullanan.Sirket == "URS Makina" ? "URS Makine" : a.Kullanan.Sirket), U_OdeyenSirket = sirket,
                            // tabloda personel adı yazılmaz (şirket masrafı avansının ayrıntısı çekmecede; şahsi avansta hiçbir yerde)
                            U_AvansNo = a.No, U_Kurum = sahsi ? "Personel Avansı (Şahsi) · " + a.Kullanan.Departman : "Nakit Avans · " + a.Kullanan.Departman, U_UrunHizmet = sahsi ? "Şahsi avans · ayrıntı gizli (İK)" : a.Konu, U_OdemeYontemi = "Nakit", U_Kasa = Kasalar[0], U_TeslimAlan = a.Kullanan.AdSoyad,
                            U_TalepEden = sahsi ? "Personel (gizli)" : (a.Olusturan?.AdSoyad ?? a.Kullanan.AdSoyad),
                            U_ParaBirimi = pb, U_Kur = kur, U_Tutar = a.OnaylananTutar, U_TutarTL = Math.Round(a.OnaylananTutar * kur, 2), U_OdenenTutarPB = pb, U_PlanlananTarih = plan, BelgeTarihi = a.Tarih.Date,
                            U_BagliBelgeTipi = "Avans", U_KayitTipi = "Avans", U_FaturaSiparisNo = a.No, U_IslemTipi = "X", U_Satinalmaci = "", U_Durum = "Planlandı", U_EskiDurum = "Planlandı",
                            U_SonOdemeTarihi = a.KullanimTarihi.Date, U_FaturaTutari = a.OnaylananTutar, U_OnayDurumu = "Onaylandı", U_Onaylayan = "Neslihan Uludağ", U_OnayTarihi = a.Tarih, U_OnayNotu = "Nakit Avans modülünde onaylandı (" + a.No + ")",
                            U_Notlar = $"{a.Oncelik} · {a.HarcamaKategorisi}" + (a.GrupIci ? " · grup içi avans (masraf " + a.Kullanan.Sirket + ")" : ""), U_Sira = _odemeler[db].Count + 1
                        };
                        if (pb != "TRY") { m.U_KurKaynagi = "TCMB Döviz Alış"; m.U_KurTarihi = plan; m.U_Kur = KurOrnek.Kur(pb, plan, m.U_KurKaynagi); m.U_TutarTL = Math.Round(a.OnaylananTutar * m.U_Kur.Value, 2); m.U_KurFarkiFaturasi = "Kesilmeyecek"; }
                        _odemeler[db].Add(m); Log(db, m.DocEntry, "Avans", $"{a.No} onaylı avans nakit akışa alındı · {(sahsi ? "Personel (gizli · şahsi avans)" : a.Kullanan.AdSoyad)} · {(a.OnaylananTutar).ToString("N0", Tr)} {a.ParaBirimi}", "Sistem");
                    }
                    if (a.OdemeBekleyen <= 0 && a.TeslimEdilen > 0)
                    {
                        if (m.U_Durum != "ÖDENDİ")
                        {
                            var son = a.Odemeler.Where(o => o.Durum != "İptal").OrderBy(o => o.Tarih).Last();
                            m.U_Durum = "ÖDENDİ"; m.U_EskiDurum = "ÖDENDİ"; m.U_OdenmeTarihi = son.Tarih.Date; m.U_OdenenTutar = a.TeslimEdilen; m.U_MakbuzNo = son.FisNo; m.U_Kasa = Kasalar[0]; m.U_TeslimAlan = son.TeslimAlan;
                            if (son.TeslimSekli == "Havale") m.U_OdemeYontemi = "Havale / EFT";
                            Log(db, m.DocEntry, "Avans Ödendi", $"{a.No} Nakit Avans modülünde teslim edildi ({son.FisNo}); satır ÖDENDİ", "Sistem");
                        }
                    }
                    else if (m.U_Durum == "ÖDENDİ" && a.OdemeBekleyen > 0) { m.U_Durum = "Planlandı"; m.U_EskiDurum = "Planlandı"; m.U_OdenmeTarihi = null; m.U_OdenenTutar = null; Log(db, m.DocEntry, "Avans", $"{a.No} ödeme ters kaydı; satır yeniden Planlandı", "Sistem"); }
                    if (m.U_Durum != "ÖDENDİ") { m.U_Tutar = a.OnaylananTutar; m.U_TutarTL = Math.Round(a.OnaylananTutar * (m.U_Kur ?? kur), 2); m.U_OdenenTutar = a.TeslimEdilen > 0 ? a.TeslimEdilen : (decimal?)null; m.U_OdenenTutarPB = pb; }
                }
            }
        }
        /// <summary>Satır ÖDENDİ olunca (satır içi, çekmece ya da talimat gerçekleşmesi) avans modülüne CashPaymentCreated olayı: avans teslim edilmiş sayılır.</summary>
        public static string AvansOdemesiIsle(NakitAkisViewModel m, string kullanici)
        {
            if (m == null || string.IsNullOrEmpty(m.U_AvansNo)) return null;
            var a = NakitAvansOrnek.Talepler.FirstOrDefault(x => x.No == m.U_AvansNo); if (a == null || a.OdemeBekleyen <= 0) return null;
            string hata = NakitAvansOrnek.Olay(a, "CashPaymentCreated", $"KNA-{m.DocEntry}-{DateTime.Now:yyMMddHHmmss}", a.OdemeBekleyen, NakitAvansOrnek.Kasalar[0], m.U_OdemeYontemi == "Havale / EFT" ? "Havale" : "Nakit", null, kullanici);
            lock (Kilit) Log(m.U_KaynakFirma, m.DocEntry, hata == null ? "Avans Ödendi" : "Avans Hata", hata == null ? $"{a.No} teslim edildi; Nakit Avans › Bekleyen Ödemeler listesinden düştü" : hata, kullanici);
            if (hata == null) { var son = a.Odemeler.OrderBy(o => o.Tarih).Last(); m.U_MakbuzNo = son.FisNo; m.U_OdenenTutar = a.TeslimEdilen; }
            return hata;
        }
        public static object AvansBilgi(NakitAkisViewModel m)
        {
            if (string.IsNullOrEmpty(m.U_AvansNo)) return null;
            var a = NakitAvansOrnek.Talepler.FirstOrDefault(x => x.No == m.U_AvansNo); if (a == null) return new { no = m.U_AvansNo, link = "/IkTalep/AvansBekleyenOdemeler", bulundu = false, talepDurumu = "Kayıt bulunamadı" };
            bool sahsi = NakitAvansOrnek.SahsiMi(a);
            return new { no = a.No, link = "/IkTalep/Avans?no=" + Uri.EscapeDataString(a.No), bulundu = true, sahsi, tur = a.TalepTuru, kullanan = sahsi ? "Personel (gizli · şahsi avans)" : a.Kullanan.AdSoyad, departman = a.Kullanan.Departman, kullananSirket = a.Kullanan.Sirket, odeyenSirket = a.Sirket, talepDurumu = a.TalepDurumu, odemeDurumu = a.OdemeDurumu, kapatmaDurumu = a.KapatmaDurumu, onaylanan = a.OnaylananTutar, odemeBekleyen = a.OdemeBekleyen, teslimEdilen = a.TeslimEdilen, pb = a.ParaBirimi, grupIci = a.GrupIci, oncelik = a.Oncelik, konu = sahsi ? "Şahsi avans · ayrıntı gizli (İK)" : a.Konu, kategori = sahsi ? "" : a.HarcamaKategorisi, kullanimTarihi = a.KullanimTarihi.ToString("yyyy-MM-dd"), finansOnayi = a.FinansOnayi };
        }
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
                        decimal kur = Kurlar.TryGetValue(pb, out var kk) ? kk : 1m; string belgeTipi = rnd.NextDouble() < 0.7 ? "Fatura" : rnd.NextDouble() < 0.5 ? "Sipariş" : "Cari Bakiye";
                        var belgeTarihi = plan.AddDays(-rnd.Next(15, 60)); int vadeGun = new[] { 30, 45, 60, 90 }[rnd.Next(4)];
                        var m = new NakitAkisViewModel
                        {
                            DocEntry = ++_sonDocEntry, DocNum = _sonDocEntry, U_FaturaSahibi = s.Ad, U_OdeyenSirket = s.Ad, U_PlanlananTarih = plan, BelgeTarihi = belgeTarihi, U_SonOdemeTarihi = belgeTipi == "Cari Bakiye" ? plan : belgeTarihi.AddDays(vadeGun), U_Kurum = kurum, U_UrunHizmet = Urunler[rnd.Next(Urunler.Length)],
                            U_ParaBirimi = pb, U_Tutar = tutar, U_Kur = kur, U_TutarTL = Math.Round(tutar * kur, 2), U_OdemeYontemi = yontem, U_OdenenTutarPB = pb, U_KaynakFirma = s.DbName,
                            U_BagliBelgeTipi = belgeTipi, U_KayitTipi = belgeTipi == "Cari Bakiye" ? "Bakiye" : belgeTipi, U_BagliDocEntry = belgeTipi == "Cari Bakiye" ? null : 40000 + rnd.Next(1, 9999), U_FaturaTutari = belgeTipi == "Cari Bakiye" ? (decimal?)null : tutar,
                            U_FaturaSiparisNo = belgeTipi == "Fatura" ? $"FT-{plan.Year}-{rnd.Next(10000, 99999)}" : belgeTipi == "Sipariş" ? $"SP-{plan.Year}-{rnd.Next(1000, 9999)}" : "",
                            U_IslemTipi = rnd.NextDouble() < 0.7 ? "X" : rnd.NextDouble() < 0.85 ? "R" : "Y", U_Satinalmaci = Satinalmacilar[rnd.Next(Satinalmacilar.Length)], U_Durum = "Bekliyor", U_Notlar = ""
                        };
                        m.U_TalepEden = belgeTipi == "Cari Bakiye" ? "Kerem Aksoy" : m.U_Satinalmaci; m.U_EskiDurum = m.U_Durum; return m;
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
                        if (y == "Havale / EFT") { m.U_BankaHesabi = BankaHesaplari[rnd.Next(BankaHesaplari.Length)]; var tb = TedarikciBanka(m.U_Kurum); m.U_AliciBanka = tb.banka; m.U_AliciIban = tb.iban; m.U_AliciHesapAdi = tb.hesapAdi; m.U_DekontNo = odendi ? $"DK-{m.U_OdenmeTarihi:yyMMdd}-{rnd.Next(100, 999)}" : ""; }
                        else if (y == "Nakit")
                        {
                            m.U_Kasa = Kasalar[rnd.Next(Kasalar.Length)]; m.U_MakbuzNo = odendi ? $"MK-{rnd.Next(10000, 99999)}" : ""; m.U_TeslimAlan = odendi ? TeslimAlanlar[rnd.Next(TeslimAlanlar.Length)] : "";
                            // ödenmiş nakitlerin bir kısmında imzalı makbuz kopyası rapor sayfasına eklenmiş (ön yüz örneği: dosya içeriği yok)
                            if (odendi && rnd.NextDouble() < 0.5) m.DekontListesi.Add(new DekontKaydi { Id = ++_sonDekontId, SirketDb = m.U_KaynakFirma, DocEntry = m.DocEntry, Tur = "İmzalı Makbuz", DosyaAdi = $"imzali_{m.U_MakbuzNo}.pdf", DosyaYolu = $"/Dekontlar/imzali_{m.U_MakbuzNo}.pdf", Aciklama = "Teslim alan imzalı makbuz (tarama)", Yukleyen = "Kerem Aksoy", Tarih = m.U_OdenmeTarihi.Value.AddHours(15) });
                            // nakit talep / onay: ödenmişler onaylı; bekleyenlerin bir kısmı onay bekliyor (Alev Özavşar / Neslihan Uludağ / Özge İsen onaylar)
                            bool onayli = odendi || rnd.NextDouble() < 0.55; m.U_OnayDurumu = onayli ? "Onaylandı" : "Onay Bekliyor";
                            if (onayli) { m.U_Onaylayan = NakitOnaycilari[rnd.Next(NakitOnaycilari.Length)]; m.U_OnayTarihi = (odendi ? m.U_OdenmeTarihi.Value : (m.U_PlanlananTarih ?? bugun)).AddDays(-rnd.Next(1, 6)).AddHours(10 + rnd.Next(7)); m.U_OnayNotu = odendi ? "" : "Nakit ödeme talebi onaylandı"; }
                            if (!onayli && m.U_Durum == "Planlandı") { m.U_Durum = "Bekliyor"; m.U_EskiDurum = "Bekliyor"; }
                        }
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
                        m.U_MahsupFisNo = mh.No; m.U_KarsiBelge = mh.Alacaklar.FirstOrDefault()?.BelgeNo ?? mh.Kalemler.FirstOrDefault()?.BelgeNo ?? ""; m.U_UrunHizmet = mh.TurAd + " · " + (mh.Neden ?? mh.NedenKodu ?? "mahsup"); m.U_BagliBelgeTipi = "Mahsup"; m.U_KayitTipi = "Bakiye"; m.U_FaturaSiparisNo = mh.No; m.U_TalepEden = string.IsNullOrEmpty(mh.Olusturan) ? m.U_TalepEden : mh.Olusturan;
                        GrupIciDurum(m, mh.Durum == "Muhasebeleşti" || mh.Durum == "Mutabakat Oluştu" || mh.Durum == "Uyumlu", mh.Durum == "Onaylandı" || mh.Durum == "Kısmi Entegrasyon", mh.GercekTarihi); liste.Add(m);
                    }
                    foreach (var tm in TemlikOrnek.Islemler.Where(x => (x.KaynakSirket == s.Ad || x.Taraflar.Any(t => t.Ad == s.Ad)) && x.Aktif).OrderByDescending(x => x.Tarih).Take(4))
                    {
                        var m = Yeni(PlanTarihi(tm.EtkinlikTarihi == default ? tm.Tarih : tm.EtkinlikTarihi), "Temlik", tm.NihaiAlan ?? "Temlik Alan", tm.NihaiTutar > 0 ? tm.NihaiTutar : tm.BuTemlikTutari > 0 ? tm.BuTemlikTutari : Math.Round((decimal)(rnd.NextDouble() * 600_000 + 80_000) / 1000m) * 1000m, tm.ParaBirimi ?? "TRY");
                        m.U_TemlikNo = tm.No; m.U_TemlikEden = tm.IlkEden ?? ""; m.U_UrunHizmet = tm.Tur + " · " + (tm.Neden ?? tm.NedenKodu ?? "temlik"); m.U_BagliBelgeTipi = "Temlik"; m.U_KayitTipi = "Bakiye"; m.U_FaturaSiparisNo = tm.No; m.U_TalepEden = string.IsNullOrEmpty(tm.Olusturan) ? m.U_TalepEden : tm.Olusturan;
                        GrupIciDurum(m, tm.Durum == "Tamamlandı" || tm.MuhasebeDurumu == "Tamamlandı", tm.Durum == "Onaylandı" || tm.Durum == "Tam İmzalı" || tm.Durum == "Kısmi Muhasebeleşti" || tm.Durum == "Belge Üretildi" || tm.Durum == "İmzaya Gönderildi" || tm.Durum == "Kısmi İmzalı", tm.GercekTarihi); liste.Add(m);
                    }
                    foreach (var yn in YansitmaOrnek.Islemler.Where(x => x.Yansitilan == s.Ad && x.Aktif).OrderByDescending(x => x.Tarih).Take(5))
                    {
                        var m = Yeni(PlanTarihi(yn.Tarih), "Yansıtma", yn.Yansitan, yn.GenelToplam > 0 ? Math.Round(yn.GenelToplam, 2) : Math.Round((decimal)(rnd.NextDouble() * 300_000 + 30_000) / 1000m) * 1000m, yn.ParaBirimi ?? "TRY");
                        m.U_YansitmaNo = yn.No; m.U_UrunHizmet = yn.Tur + " yansıtması · " + (yn.Aciklama ?? yn.KaynakFaturaNo ?? ""); m.U_BagliBelgeTipi = "Yansıtma"; m.U_KayitTipi = "Fatura"; m.U_FaturaSiparisNo = yn.YansitmaFaturaNo ?? yn.No; m.U_TalepEden = string.IsNullOrEmpty(yn.Olusturan) ? m.U_TalepEden : yn.Olusturan;
                        m.U_Notlar = "Yansıtılan taraf: grup şirketine ödenecek (kaynak fatura " + (yn.KaynakFaturaNo ?? "") + ")";
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
                    // 5) Kısmi ödeme / taksit senaryosu: dövizli bir faturanın %70'i bu satır, %30'u sonraki tarihte ayrı satır (aynı belge no; tutarlar net izlenir)
                    foreach (var m in liste.Where(x => x.U_BagliBelgeTipi == "Fatura" && x.U_ParaBirimi != "TRY" && x.U_OdemeYontemi == "Havale / EFT").OrderBy(x => x.DocEntry).Take(2).ToList())
                    {
                        decimal fatura = Math.Round((m.U_Tutar ?? 0) / 0.7m / 100m) * 100m; m.U_FaturaTutari = fatura; m.U_Tutar = Math.Round(fatura * 0.7m, 2); m.U_TutarTL = Math.Round(m.U_Tutar.Value * (m.U_Kur ?? 1), 2); m.U_Notlar = "Kısmi ödeme 1/2 · %70 peşin";
                        var k2 = Yeni((m.U_PlanlananTarih ?? bugun).AddDays(30), m.U_OdemeYontemi, m.U_Kurum, Math.Round(fatura * 0.3m, 2), m.U_ParaBirimi);
                        k2.U_FaturaSiparisNo = m.U_FaturaSiparisNo; k2.U_BagliBelgeTipi = "Fatura"; k2.U_KayitTipi = "Fatura"; k2.U_BagliDocEntry = m.U_BagliDocEntry; k2.BelgeTarihi = m.BelgeTarihi; k2.U_SonOdemeTarihi = (m.U_SonOdemeTarihi ?? bugun).AddDays(30); k2.U_FaturaTutari = fatura; k2.U_UstDocEntry = m.DocEntry; k2.U_UrunHizmet = m.U_UrunHizmet; k2.U_Satinalmaci = m.U_Satinalmaci;
                        k2.U_Durum = "Bekliyor"; k2.U_EskiDurum = "Bekliyor"; k2.U_Notlar = "Kısmi ödeme 2/2 · %30 bakiye (vade sonrası)"; YontemAlanlariDoldur(k2); liste.Add(k2);
                    }
                    // 6) Mükerrer kayıt senaryosu (yalnız Uras Kimya): aynı cari, aynı fatura no, aynı tutar ikinci kez girilmiş — sistem uyarır
                    if (s.DbName == "URASKIMYA")
                    {
                        var asil = liste.FirstOrDefault(x => x.U_BagliBelgeTipi == "Fatura" && x.U_OdemeYontemi == "Havale / EFT" && x.U_ParaBirimi == "TRY" && x.U_Durum == "Bekliyor" && x.U_UstDocEntry == null && x.U_FaturaTutari == x.U_Tutar);
                        if (asil != null)
                        {
                            var kopya = Yeni((asil.U_PlanlananTarih ?? bugun).AddDays(2), asil.U_OdemeYontemi, asil.U_Kurum, asil.U_Tutar ?? 0, asil.U_ParaBirimi);
                            kopya.U_FaturaSiparisNo = asil.U_FaturaSiparisNo; kopya.U_BagliBelgeTipi = "Fatura"; kopya.U_KayitTipi = "Fatura"; kopya.U_BagliDocEntry = asil.U_BagliDocEntry; kopya.BelgeTarihi = asil.BelgeTarihi; kopya.U_SonOdemeTarihi = asil.U_SonOdemeTarihi; kopya.U_FaturaTutari = asil.U_FaturaTutari; kopya.U_UrunHizmet = asil.U_UrunHizmet; kopya.U_Satinalmaci = "Gizem Tan";
                            kopya.U_Durum = "Bekliyor"; kopya.U_EskiDurum = "Bekliyor"; kopya.U_Notlar = "Satınalma tarafından ikinci kez girildi"; YontemAlanlariDoldur(kopya); liste.Add(kopya);
                        }
                    }
                    // 6b) grup içi ödeme senaryosu (yalnız Uras Kimya): fatura sahibi başka grup şirketi, ödeyen Uras Kimya → otomatik Mahsup / Temlik belgesi (biri ödenmiş = muhasebeleşmiş)
                    if (s.DbName == "URASKIMYA")
                    {
                        var adaylar = liste.Where(x => x.U_OdemeYontemi == "Havale / EFT" && x.U_UstDocEntry == null && x.U_BagliBelgeTipi == "Fatura" && x.U_ParaBirimi == "TRY").OrderBy(x => x.DocEntry).ToList();
                        var sahipler = new[] { ("Alv Kimya", "Mahsup"), ("Selvi", "Mahsup"), ("Daf Kimya", "Temlik") }; int gi = 0;
                        foreach (var m in new[] { adaylar.FirstOrDefault(x => x.U_Durum == "ÖDENDİ"), adaylar.FirstOrDefault(x => x.U_Durum == "Planlandı"), adaylar.LastOrDefault(x => x.U_Durum == "Bekliyor") }.Where(x => x != null).Distinct())
                        { var (fs, tur) = sahipler[gi++]; m.U_FaturaSahibi = fs; m.U_GrupIciTur = tur; m.U_Notlar = $"{fs} adına ödeme (grup içi · otomatik {tur.ToLower(Tr)} belgesi)"; }
                    }
                    // 6c) dövizli satırlarda kur bilgisi: kaynak (TCMB alış / satış, anlaşmalı), kur günü (ödenme ya da planlanan tarih; çekte tahsil = vade), kur serisinden kur, kur farkı faturası
                    foreach (var m in liste.Where(x => x.U_ParaBirimi != "TRY" && string.IsNullOrEmpty(x.U_KurKaynagi)))
                    {
                        double z = rnd.NextDouble(); m.U_KurTarihi = (m.U_Durum == "ÖDENDİ" ? m.U_OdenmeTarihi : m.U_PlanlananTarih) ?? bugun;
                        m.U_KurKaynagi = m.U_OdemeYontemi == "Çek" || m.U_OdemeYontemi == "Senet" ? "Çek Tahsil Günü Kuru" : z < 0.65 ? "TCMB Döviz Alış" : z < 0.85 ? "TCMB Döviz Satış" : "Anlaşmalı Kur";
                        if (m.U_KurKaynagi == "Anlaşmalı Kur") { m.U_Kur = Math.Round(KurOrnek.Kur(m.U_ParaBirimi, m.U_KurTarihi, "TCMB Döviz Alış") * 0.985m, 2); m.U_KurNotu = $"Tedarikçiyle sabit kur anlaşması {m.U_Kur.Value.ToString("N2", Tr)} ({m.U_KurTarihi.Value.AddDays(-rnd.Next(3, 20)):dd.MM.yyyy} e-posta)"; }
                        else m.U_Kur = KurOrnek.Kur(m.U_ParaBirimi, m.U_KurTarihi, m.U_KurKaynagi);
                        m.U_TutarTL = Math.Round((m.U_Tutar ?? 0) * (m.U_Kur ?? 1), 2);
                        m.U_KurFarkiFaturasi = m.U_Durum == "ÖDENDİ" ? (rnd.NextDouble() < 0.3 ? "Kesilecek" : "Kesilmeyecek") : "Belirsiz";
                    }
                    liste = liste.OrderBy(m => m.U_PlanlananTarih).ThenBy(m => m.DocEntry).ToList(); for (int i = 0; i < liste.Count; i++) liste[i].U_Sira = i + 1;
                    _odemeler[s.DbName] = liste;
                    foreach (var m in liste.Where(x => x.U_Durum == "ÖDENDİ")) Log(s.DbName, m.DocEntry, "Ödeme", $"{m.U_Kurum} · {m.U_OdemeYontemi} · {(m.U_OdenenTutar ?? 0).ToString("N2", Tr)} {m.U_OdenenTutarPB} ödendi", "Kerem Aksoy");
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
                        // portföy X / R işlem tipine göre ayrı izlenir
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "ÇEK PORTFÖYÜ (X)", Detay = "MÜŞTERİ ÇEKLERİ · X (Portföyde + Planlandı)", PB = "TRY", Tutar = cekler.Where(k => k.Belge == "Çek" && k.IslemTipi == "X").Sum(k => k.Tutar) },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "ÇEK PORTFÖYÜ (R)", Detay = "MÜŞTERİ ÇEKLERİ · R (Portföyde + Planlandı)", PB = "TRY", Tutar = cekler.Where(k => k.Belge == "Çek" && k.IslemTipi == "R").Sum(k => k.Tutar) },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "SENET PORTFÖYÜ (X)", Detay = "MÜŞTERİ SENETLERİ · X (Portföyde + Planlandı)", PB = "TRY", Tutar = cekler.Where(k => k.Belge == "Senet" && k.IslemTipi == "X").Sum(k => k.Tutar) },
                        new KasaBakiyeViewModel { GuncellenmeTarihi = bugun, BankaKasa = "SENET PORTFÖYÜ (R)", Detay = "MÜŞTERİ SENETLERİ · R (Portföyde + Planlandı)", PB = "TRY", Tutar = cekler.Where(k => k.Belge == "Senet" && k.IslemTipi == "R").Sum(k => k.Tutar) }
                    };
                    foreach (var b in bak) b.TutarTL = Math.Round(b.Tutar * (Kurlar.TryGetValue(b.PB, out var kk) ? kk : 1m), 2);
                    _bakiyeler[s.DbName] = bak;
                }
                ZincirSenaryosu(bugun);
                // 7) ödenmiş satırların muhasebe fişleri (tohum): yönteme göre 320 / 332 / 196 borç, 100 / 102 / 101 … alacak
                foreach (var l in _odemeler.Values) foreach (var m in l.Where(x => x.U_Durum == "ÖDENDİ" && string.IsNullOrEmpty(x.U_YevmiyeNo))) { var f = MuhasebeOrnek.OdemeFisi(m, m.U_OdemeYontemi == "Nakit" && !string.IsNullOrEmpty(m.U_MakbuzNo) ? "Kasa Tediye " + m.U_MakbuzNo : "Ödeme", "Nazlı Erdem"); if (f != null) { f.Tarih = (m.U_OdenmeTarihi ?? bugun).AddHours(16); m.U_YevmiyeNo = f.No; } }
                // 8) fatura sahibi ≠ ödeyen satırların otomatik grup içi Mahsup / Temlik belgeleri (ödenmişlerde fatura sahibi fişi de atılır, grup içi bakiye güncellenir)
                foreach (var l in _odemeler.Values) foreach (var m in l.Where(GrupIciBelgeOrnek.Gerekli).ToList()) GrupIciBelgeOrnek.Senkron(m, "Sistem");
                // 9) planlı düzenli ödemeler (Data/DuzenliOdemeOrnek.cs): geçmiş iki ay ödenmiş, bu ay akmış satırlar (kilit yeniden girilebilir; Duzenli kilidi NakitAkis çağrılırken tutulmaz)
                DuzenliOdemeOrnek.Tohumla();
            }
        }
        /// <summary>
        /// Örnek dolaşım senaryosu: Alv Kimya'nın Öztürk Plastik faturası Selvi portföyündeki çekle ödenir; çek Selvi → Uras Kimya → URS Makine → Uras Holding → Alv Kimya
        /// zinciriyle ciro edilerek grup içi borçları kapatır, sonunda Alv Kimya tedarikçiye ciro eder (grup içi bakiyeler şişmez). İlk iki adım gerçekleşmiş durumda.
        /// </summary>
        private static void ZincirSenaryosu(DateTime bugun)
        {
            var cek = CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == "Selvi" && k.Secilebilir && k.Belge == "Çek" && k.Vade > bugun.AddDays(20)).OrderByDescending(k => k.Tutar).FirstOrDefault();
            if (cek == null) return; const string db = "ALV_KIMYA"; if (!_odemeler.ContainsKey(db)) _odemeler[db] = new List<NakitAkisViewModel>();
            var m = new NakitAkisViewModel
            {
                DocEntry = ++_sonDocEntry, DocNum = _sonDocEntry, U_KaynakFirma = db, U_FaturaSahibi = "Alv Kimya", U_OdeyenSirket = "Selvi", U_Kurum = "Öztürk Plastik", U_UrunHizmet = "Hammadde alımı · grup içi çek dolaşımıyla ödeme",
                U_ParaBirimi = "TRY", U_Tutar = cek.Tutar, U_TutarTL = cek.Tutar, U_Kur = 1, U_OdenenTutarPB = "TRY", U_OdemeYontemi = "Çek", U_PlanlananTarih = cek.Vade.AddDays(-3), BelgeTarihi = bugun.AddDays(-12),
                U_BagliBelgeTipi = "Fatura", U_KayitTipi = "Fatura", U_FaturaSiparisNo = $"FT-{bugun.Year}-71455", U_BagliDocEntry = 48811, U_IslemTipi = "X", U_Satinalmaci = "Canan Su", U_Durum = "Planlandı", U_EskiDurum = "Planlandı", U_SonOdemeTarihi = cek.Vade, U_FaturaTutari = cek.Tutar,
                U_MutabakatDurumu = "Mutabık", SonMutabakatTarihi = bugun.AddDays(-5), U_Notlar = "Grup içi çek dolaşımı: Selvi → Uras Kimya → URS Makine → Uras Holding → Alv Kimya → Öztürk Plastik", U_Sira = _odemeler[db].Count + 1, U_TalepEden = "Canan Su"
            };
            CekAkibetOrnek.Planla(cek.DocEntry, "Öztürk Plastik", m.DocEntry, "Alv Kimya", "Kerem Aksoy");
            m.U_CekDocEntryler = cek.DocEntry.ToString(); _odemeler[db].Add(m); BelgeleriDoldur(m);
            CekAkibetOrnek.DolasimKaydet(cek.DocEntry, new List<string> { "Uras Kimya", "URS Makine", "Uras Holding", "Alv Kimya" }, "Tedarikçiye Ciro", "Öztürk Plastik", m.DocEntry, "Kerem Aksoy");
            CekAkibetOrnek.DolasimAdimGerceklestir(cek.DocEntry, 1, "Kerem Aksoy", bugun.AddDays(-6).AddHours(11)); CekAkibetOrnek.DolasimAdimGerceklestir(cek.DocEntry, 2, "Pınar Ak", bugun.AddDays(-3).AddHours(15));
            BelgeleriDoldur(m);
            Log(db, m.DocEntry, "Belge Planlandı", $"{cek.No} (Selvi portföyü) Öztürk Plastik ödemesi için planlandı; dolaşım: Selvi → Uras Kimya → URS Makine → Uras Holding → Alv Kimya", "Kerem Aksoy");
            Log(db, m.DocEntry, "Dolaşım Adımı", $"{cek.No} Selvi → Uras Kimya ciro edildi (1/5)", "Kerem Aksoy"); Log(db, m.DocEntry, "Dolaşım Adımı", $"{cek.No} Uras Kimya → URS Makine ciro edildi (2/5)", "Pınar Ak");
        }

        // ---------- işlemler ----------
        static readonly string[] KopyalananAlanlar = { "U_Sira", "U_Renk", "U_KayitTipi", "U_FaturaSiparisNo", "U_PlanlananTarih", "U_Kurum", "U_Satinalmaci", "U_UrunHizmet", "U_ParaBirimi", "U_Kur", "U_Tutar", "U_TutarTL", "U_OdemeYontemi", "U_OdenmeTarihi", "U_OdenenTutar", "U_OdenenTutarPB", "U_Durum", "U_Notlar", "U_BagliBelgeTipi", "U_BagliDocEntry", "U_IslemTipi", "U_MutabakatDurumu", "U_FaturaSahibi", "U_OdeyenSirket", "MuafiyetSebebi", "U_SonOdemeTarihi", "U_FaturaTutari", "U_UstDocEntry", "U_TeslimAlan", "U_Kasa", "U_TalepEden", "U_KurKaynagi", "U_KurTarihi", "U_KurNotu", "U_KurFarkiFaturasi", "U_GrupIciTur", "U_AliciBanka", "U_AliciIban", "U_AliciHesapAdi" };
        /// <summary>Şahsi avans satırı (gizlilik: personel adı ekranda gösterilmez).</summary>
        public static bool AvansSahsiMi(NakitAkisViewModel m) => m != null && !string.IsNullOrEmpty(m.U_AvansNo) && NakitAvansOrnek.SahsiMi(NakitAvansOrnek.Talepler.FirstOrDefault(x => x.No == m.U_AvansNo));
        static bool NakitOnaysiz(NakitAkisViewModel m) => m.U_OdemeYontemi == "Nakit" && m.U_OnayDurumu != "Onaylandı";
        static string NakitOnayMesaji => $"Nakit ödeme, onaylanmadan ödenemez / makbuz kesilemez (onaycılar: {string.Join(" / ", NakitOnaycilari)}).";
        /// <summary>Satır güncelleme (AjaxRowGuncelle / Guncelle): yalnız ekrandan gelen alanlar; belge bağları ve yönteme özel alanlar korunur.</summary>
        public static bool Guncelle(string dbName, NakitAkisViewModel gelen, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == gelen.DocEntry) : null; if (m == null) return false;
                string eskiDurum = m.U_Durum; var t = typeof(NakitAkisViewModel);
                if (gelen.U_Durum == "ÖDENDİ" && eskiDurum != "ÖDENDİ" && (NakitOnaysiz(m) || !string.IsNullOrEmpty(m.U_CikisTalebi))) gelen.U_Durum = eskiDurum;   // onaysız nakit / çek çıkış talebi elle ödendi yapılamaz
                if (!string.IsNullOrEmpty(m.U_AvansNo) && ((gelen.U_TeslimAlan ?? "").StartsWith("Personel (") || (gelen.U_TeslimAlan ?? "").StartsWith("Avans sahibi"))) gelen.U_TeslimAlan = null;   // avans satırında maskelenmiş teslim alan gerçek adın üzerine yazılmaz (makbuz için)
                foreach (var ad in KopyalananAlanlar) { var p = t.GetProperty(ad); var v = p.GetValue(gelen); if (v == null && p.PropertyType == typeof(string)) continue; if (p.PropertyType == typeof(string) && (string)v == "" && (ad == "U_Kurum" || ad == "U_OdemeYontemi")) continue; p.SetValue(m, v); }
                if (m.U_BagliBelgeTipi == "Cari Bakiye") { m.U_KayitTipi = "Bakiye"; m.U_FaturaSiparisNo = ""; }
                if (m.U_OdemeYontemi == "Nakit" && string.IsNullOrEmpty(m.U_OnayDurumu)) m.U_OnayDurumu = "Onay Bekliyor";
                if (m.U_ParaBirimi == "EURO") m.U_ParaBirimi = "EUR";
                if (m.U_Tutar.HasValue && (m.U_TutarTL ?? 0) == 0) m.U_TutarTL = Math.Round(m.U_Tutar.Value * (m.U_Kur ?? 1), 2);
                if (m.U_Durum == "ÖDENDİ" && m.U_OdenmeTarihi == null) m.U_OdenmeTarihi = DateTime.Today;
                if (m.U_Durum == "ÖDENDİ" && (m.U_OdenenTutar ?? 0) == 0) m.U_OdenenTutar = m.U_Tutar;
                if (string.IsNullOrEmpty(m.U_FaturaSahibi)) m.U_FaturaSahibi = SirketAdi(dbName); if (string.IsNullOrEmpty(m.U_OdeyenSirket)) m.U_OdeyenSirket = SirketAdi(dbName);
                KurTamamla(m); if (GrupIciBelgeOrnek.Gerekli(m) && string.IsNullOrEmpty(m.U_GrupIciTur)) m.U_GrupIciTur = "Mahsup";
                m.U_EskiDurum = m.U_Durum;
                Log(dbName, m.DocEntry, eskiDurum != m.U_Durum ? "Durum" : "Güncelleme", eskiDurum != m.U_Durum ? $"{eskiDurum} → {m.U_Durum}" : "Satır güncellendi", kullanici);
                if (eskiDurum != "ÖDENDİ" && m.U_Durum == "ÖDENDİ") { AvansOdemesiIsle(m, kullanici); FisAt(m, "Ödeme (elle)", kullanici); }
                var gib = GrupIciBelgeOrnek.Senkron(m, kullanici); if (gib != null && gib.Durum == "Hazırlandı" && gib.Gunluk.Count == 1) Log(dbName, m.DocEntry, "Grup İçi Belge", $"{gib.No} ({gib.Baslik}) hazırlandı: {gib.Odeyen}, {gib.FaturaSahibi} adına ödüyor", kullanici);
                return true;
            }
        }
        /// <summary>Dövizli satırda kur bilgisi: kaynak boşsa TCMB Döviz Alış (çek / senette Çek Tahsil Günü Kuru = en erken belge vadesi), kur günü boşsa ödenme / planlanan tarih; TCMB kaynaklarında kur seriden (Data/KurOrnek.cs) okunur, anlaşmalı / fatura kurunda elle yazılan kalır. TL'de kur alanları temizlenir.</summary>
        static void KurTamamla(NakitAkisViewModel m)
        {
            string pb = m.U_ParaBirimi == "EURO" ? "EUR" : string.IsNullOrEmpty(m.U_ParaBirimi) || m.U_ParaBirimi == "TL" ? "TRY" : m.U_ParaBirimi; m.U_ParaBirimi = pb;
            if (pb == "TRY") { m.U_Kur = 1; m.U_KurKaynagi = ""; m.U_KurTarihi = null; m.U_KurFarkiFaturasi = ""; m.U_TutarTL = Math.Round(m.U_Tutar ?? 0, 2); return; }
            bool cek = m.U_OdemeYontemi == "Çek" || m.U_OdemeYontemi == "Senet";
            if (string.IsNullOrEmpty(m.U_KurKaynagi) || !KurOrnek.Kaynaklar.Contains(m.U_KurKaynagi)) m.U_KurKaynagi = cek ? "Çek Tahsil Günü Kuru" : "TCMB Döviz Alış";
            if (m.U_KurKaynagi == "Çek Tahsil Günü Kuru") { var vade = m.Belgeler.Count > 0 ? m.Belgeler.Min(b => b.Vade) : (DateTime?)null; m.U_KurTarihi = vade ?? m.U_KurTarihi ?? m.U_PlanlananTarih ?? DateTime.Today; }
            else if (m.U_KurTarihi == null) m.U_KurTarihi = (m.U_Durum == "ÖDENDİ" ? m.U_OdenmeTarihi : null) ?? m.U_PlanlananTarih ?? DateTime.Today;
            if (KurOrnek.TcmbKaynagi(m.U_KurKaynagi) || (m.U_Kur ?? 0) <= 0) m.U_Kur = KurOrnek.Kur(pb, m.U_KurTarihi, m.U_KurKaynagi);
            if (string.IsNullOrEmpty(m.U_KurFarkiFaturasi) || !KurOrnek.KurFarkiSecenekleri.Contains(m.U_KurFarkiFaturasi)) m.U_KurFarkiFaturasi = "Belirsiz";
            m.U_TutarTL = Math.Round((m.U_Tutar ?? 0) * (m.U_Kur ?? 1), 2);
        }
        /// <summary>Ödeme muhasebe fişi (bir kez): dolaşım planlı çek satırlarında fişler adım adım Çek Akıbeti tarafından atılır.</summary>
        static void FisAt(NakitAkisViewModel m, string kaynak, string kullanici)
        {
            if (!string.IsNullOrEmpty(m.U_YevmiyeNo)) return;
            bool dolasimli = (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out var n) ? CekAkibetOrnek.Bul(n) : null).Any(k => k != null && k.Dolasim.Count > 0);
            if (dolasimli) return;
            var f = MuhasebeOrnek.OdemeFisi(m, kaynak, kullanici); if (f != null) { m.U_YevmiyeNo = f.No; Log(m.U_KaynakFirma, m.DocEntry, "Muhasebe Fişi", $"{f.No} · {string.Join(" / ", f.Satirlar.Select(s => s.HesapKodu + " " + s.HesapAdi + (s.Borc > 0 ? " B " + s.Borc.ToString("N2", Tr) : " A " + s.Alacak.ToString("N2", Tr))))}", kullanici); }
        }
        /// <summary>Talimat gerçekleşince: satır ÖDENDİ, avans olayı; çek / senet belgeleri dolaşım planı varsa kalan adımlar, yoksa tedarikçiye ciro.</summary>
        public static void OdendiIsaretle(string dbName, int docEntry, string kaynak, string kullanici, bool fisAt = true, DateTime? odenmeTarihi = null)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return;
                if (m.U_Durum != "ÖDENDİ") { m.U_Durum = "ÖDENDİ"; m.U_EskiDurum = "ÖDENDİ"; m.U_OdenmeTarihi = odenmeTarihi?.Date ?? m.U_IbrazTarihi ?? DateTime.Today; m.U_OdenenTutar = m.U_Tutar; m.U_OdenenTutarPB = m.U_ParaBirimi; Log(dbName, docEntry, "Ödeme", $"{kaynak} ile ödendi · {m.U_Kurum} · {(m.U_Tutar ?? 0).ToString("N2", Tr)} {m.U_ParaBirimi}", kullanici); }
                foreach (var id in (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out var n) ? n : 0).Where(n => n > 0))
                {
                    var k = CekAkibetOrnek.Bul(id); if (k == null) continue;
                    if (k.Dolasim.Count > 0) { int guvenlik = 0; while (k.SonrakiAdim != null && guvenlik++ < 20) { var r = CekAkibetOrnek.DolasimAdimGerceklestir(id, 0, kullanici); if (!r.ok) break; } }
                    else if (k.IslemTuru == "Planlandı" || k.IslemTuru == "Portföyde") CekAkibetOrnek.IslemTuruDegistir(id, "Ciro Edildi", m.U_Kurum, $"CRO-{DateTime.Today:yyMM}-{docEntry % 9000 + 1000}", kullanici);
                }
                AvansOdemesiIsle(m, kullanici); if (fisAt) FisAt(m, kaynak, kullanici); // fisAt=false: fiş Takip Paneli onayında seçilen hesaba atılır (FisBagla ile satıra yazılır)
                var gib = GrupIciBelgeOrnek.Senkron(m, kullanici); if (gib != null && gib.Durum == "Muhasebeleşti" && !string.IsNullOrEmpty(gib.FaturaSahibiFisNo)) Log(dbName, docEntry, "Grup İçi Belge", $"{gib.No} muhasebeleşti: {gib.FaturaSahibi} fişi {gib.FaturaSahibiFisNo} · {gib.MahsupSekli}", kullanici);
            }
        }
        /// <summary>
        /// Ekrandan çek / senet ibrazı ("Çeki Ver / İbraz Et"): satırdaki planlı belgeler tedarikçiye ciro edilir (ciro bordrosu) ya da bankaya tahsile verilir; dolaşım planı olan belgede kalan adımlar
        /// gerçekleştirilir; satır ÖDENDİ olur, muhasebe fişleri atılır. Satır Kasa Nakit Akışı'nda planlı olduğundan GMY onayı gerekmez (plansız çıkış kuralı Çek Akıbeti tarafındadır).
        /// </summary>
        public static (bool ok, string mesaj) CekIbrazEt(string dbName, int docEntry, string tur, string ibrazYeri, DateTime? tarih, string teslimAlan, string belgeNo, string not, string kullanici, bool simOnay = false)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                if (m.U_OdemeYontemi != "Çek" && m.U_OdemeYontemi != "Senet") return (false, "İbraz yalnız çek / senet satırında yapılır.");
                if (m.U_Durum == "ÖDENDİ") return (false, "Satır zaten ödenmiş.");
                if (!string.IsNullOrEmpty(m.U_CikisTalebi) && m.U_OnayDurumu != "Onaylandı") return (false, "Çek çıkış talebi GMY onayı bekliyor; onaylanmadan ibraz yapılamaz.");
                BelgeleriDoldur(m); if (m.Belgeler.Count == 0) return (false, "Önce portföyden belge seçin.");
                var kayitlar = m.Belgeler.Select(b => CekAkibetOrnek.Bul(b.CekDocEntry)).Where(k => k != null).ToList();
                if (!kayitlar.Any(k => k.IslemTuru == "Planlandı" || k.IslemTuru == "Portföyde" || k.SonrakiAdim != null)) return (false, "Satırdaki belgeler zaten çıkmış: " + string.Join(", ", kayitlar.Select(k => k.No + " " + k.IslemTuru)) + ".");
                // grup içi işlem (dolaşım zincirinde grup içi ciro adımı ya da fatura sahibi ≠ ödeyen belgesi): muhasebe kayıtları önce simüle edilip onaylanmalı
                bool grupIci = kayitlar.Any(k => k.Dolasim.Any(a => a.Durum != "Gerçekleşti" && a.Tur == "Grup İçi Ciro")) || (GrupIciBelgeOrnek.Gerekli(m) && GrupIciBelgeOrnek.BulSatir(m.DocEntry)?.Durum != "Muhasebeleşti");
                if (grupIci && !simOnay) return (false, "Grup içi işlem: muhasebe kayıtları önce simüle edilip onaylanmalı (Çeki Ver penceresinde simülasyonu inceleyip onay kutusunu işaretleyin).");
                bool bankaya = tur == "Bankaya Tahsile Ver"; string yer = !string.IsNullOrWhiteSpace(ibrazYeri) ? ibrazYeri.Trim() : bankaya ? ButceOrnek.TahsilBankalari[docEntry % 2] : m.U_Kurum;
                string no = !string.IsNullOrWhiteSpace(belgeNo) ? belgeNo.Trim() : bankaya ? $"İBZ-{DateTime.Today:yyMM}-{docEntry % 9000 + 1000}" : $"CRO-{DateTime.Today:yyMM}-{docEntry % 9000 + 1000}";
                var zaman = (tarih ?? DateTime.Today).Date.AddHours(11); var sonuclar = new List<string>();
                if (simOnay) Log(dbName, docEntry, "Simülasyon Onayı", $"İbraz öncesi muhasebe simülasyonu incelendi ve onaylandı ({(grupIci ? "grup içi işlem" : "standart çıkış")}) · {(bankaya ? "bankaya tahsil" : "tedarikçiye ciro")} → {yer}", kullanici);
                foreach (var k in kayitlar)
                {
                    if (k.Dolasim.Count > 0) { int g = 0; bool tamam = true; while (k.SonrakiAdim != null && g++ < 20) { var r = CekAkibetOrnek.DolasimAdimGerceklestir(k.DocEntry, 0, kullanici, zaman); if (!r.ok) { sonuclar.Add(r.mesaj); tamam = false; break; } } if (tamam) sonuclar.Add($"{k.No} dolaşım planı tamamlandı ({k.DolasimOzeti})"); }
                    else if (k.IslemTuru == "Planlandı" || k.IslemTuru == "Portföyde") { var r = CekAkibetOrnek.IslemTuruDegistir(k.DocEntry, bankaya ? "Bankaya Tahsile Verildi" : "Ciro Edildi", yer, no, kullanici); sonuclar.Add(r.mesaj); }
                    else sonuclar.Add($"{k.No} zaten {k.IslemTuru}.");
                }
                m.U_IbrazBelgeNo = no; m.U_IbrazTarihi = zaman.Date; if (!string.IsNullOrWhiteSpace(teslimAlan)) m.U_TeslimAlan = teslimAlan.Trim();
                if (!string.IsNullOrWhiteSpace(not)) m.U_Notlar = string.IsNullOrEmpty(m.U_Notlar) ? not.Trim() : m.U_Notlar + " | " + not.Trim();
                Log(dbName, docEntry, "Çek İbrazı", $"{(bankaya ? "Bankaya tahsile verildi" : "Tedarikçiye ciro edildi")} → {yer} · {no} · {m.U_CekNolar}" + (string.IsNullOrWhiteSpace(teslimAlan) ? "" : " · teslim alan " + teslimAlan.Trim()) + $" · {zaman:dd.MM.yyyy}", kullanici);
                OdendiIsaretle(dbName, docEntry, (bankaya ? "Bankaya ibraz " : "Çek cirosu ") + no, kullanici);
                return (true, $"{m.U_CekNolar} {(bankaya ? "bankaya tahsile verildi" : "tedarikçiye ciro edildi")} → {yer} · {no}; satır ÖDENDİ" + (string.IsNullOrEmpty(m.U_YevmiyeNo) ? "" : $" · fiş {m.U_YevmiyeNo}") + ". " + string.Join(" ", sonuclar));
            }
        }
        /// <summary>
        /// İbraz öncesi muhasebe simülasyonu (kaydedilmez): belge çıkış adımları ve fişleri (dolaşım zincirinde grup içi ciro: eden 332/101, alan 101/132; son adım tedarikçi 320/101 ya da banka 102/101),
        /// dolaşımsız belgede ödeme fişi, fatura sahibi ≠ ödeyen ise grup içi belge fişi ve grup içi bakiye etkileri. Kullanıcı onaylayınca CekIbrazEt(simOnay: true) kayıtları atar.
        /// </summary>
        public static object CekIbrazSimule(string dbName, int docEntry, string tur, string ibrazYeri)
        {
            Tohumla(); var m = Bul(dbName, docEntry); if (m == null) return null; BelgeleriDoldur(m);
            bool bankaya = tur == "Bankaya Tahsile Ver"; string yer = !string.IsNullOrWhiteSpace(ibrazYeri) ? ibrazYeri.Trim() : bankaya ? ButceOrnek.TahsilBankalari[docEntry % 2] : m.U_Kurum;
            string no = bankaya ? $"İBZ-{DateTime.Today:yyMM}-{docEntry % 9000 + 1000}" : $"CRO-{DateTime.Today:yyMM}-{docEntry % 9000 + 1000}";
            var fisler = new List<object>(); var adimlar = new List<object>(); var uyarilar = new List<string>(); var delta = new Dictionary<string, decimal>(); bool grupIci = false, dolasimli = false;
            void Delta(string borclu, string alacakli, decimal v) { if (v == 0 || string.IsNullOrEmpty(borclu) || string.IsNullOrEmpty(alacakli)) return; string k = borclu + "|" + alacakli; delta[k] = (delta.TryGetValue(k, out var x) ? x : 0) + v; }
            foreach (var b in m.Belgeler)
            {
                var k = CekAkibetOrnek.Bul(b.CekDocEntry); if (k == null) continue;
                if (k.Dolasim.Count > 0)
                {
                    dolasimli = true;
                    foreach (var a in k.Dolasim.Where(a => a.Durum != "Gerçekleşti"))
                    {
                        string adimNo = (a.Tur == "Bankaya İbraz" ? "İBZ" : "CRO") + $"-{DateTime.Today:yyMM}-{k.DocEntry % 9000 + 1000 + a.Sira}";
                        if (a.Tur == "Grup İçi Ciro")
                        {
                            grupIci = true; decimal mevcut = CekAkibetOrnek.GrupBakiye(a.Eden, a.Alan) + (delta.TryGetValue(a.Eden + "|" + a.Alan, out var d0) ? d0 : 0), kapanan = Math.Max(0, Math.Min(k.Tutar, mevcut)), yeni = k.Tutar - kapanan;
                            adimlar.Add(new { belge = k.No, sira = a.Sira, eden = a.Eden, alan = a.Alan, tur = a.Tur, tutar = k.Tutar, mevcutBorc = mevcut, kapanan, yeniBorc = yeni, belgeNo = adimNo });
                            Delta(a.Eden, a.Alan, -kapanan); if (yeni > 0) { Delta(a.Alan, a.Eden, yeni); uyarilar.Add($"{k.No} · {a.Eden} → {a.Alan}: borç ilişkisi yetersiz, {yeni.ToString("N2", Tr)} ₺ yeni grup içi bakiye oluşur."); }
                            fisler.AddRange(MuhasebeOrnek.CiroFisiTaslak(CekAkibetOrnek.DbName(a.Eden), CekAkibetOrnek.DbName(a.Alan), a.Eden, a.Alan, "Grup İçi Ciro", adimNo, k.No, k.Tutar).Select(t => MuhasebeOrnek.Json(t)));
                        }
                        else
                        {
                            string hedef = a.Tur == "Bankaya İbraz" ? ButceOrnek.TahsilBankalari[k.DocEntry % 2] : a.Alan;
                            adimlar.Add(new { belge = k.No, sira = a.Sira, eden = a.Eden, alan = hedef, tur = a.Tur, tutar = k.Tutar, mevcutBorc = 0m, kapanan = 0m, yeniBorc = 0m, belgeNo = adimNo });
                            fisler.AddRange(MuhasebeOrnek.CiroFisiTaslak(CekAkibetOrnek.DbName(a.Eden), null, a.Eden, hedef, a.Tur, adimNo, k.No, k.Tutar).Select(t => MuhasebeOrnek.Json(t)));
                        }
                    }
                }
                else if (k.IslemTuru == "Planlandı" || k.IslemTuru == "Portföyde") adimlar.Add(new { belge = k.No, sira = 1, eden = k.Sirket, alan = yer, tur = bankaya ? "Bankaya İbraz" : "Tedarikçiye Ciro", tutar = k.Tutar, mevcutBorc = 0m, kapanan = 0m, yeniBorc = 0m, belgeNo = no });
                else uyarilar.Add($"{k.No} zaten {k.IslemTuru}; yeniden çıkmaz.");
            }
            if (!dolasimli && string.IsNullOrEmpty(m.U_YevmiyeNo)) { var t = MuhasebeOrnek.OdemeFisiTaslak(m, (bankaya ? "Bankaya ibraz " : "Çek cirosu ") + no); if (t != null) fisler.Add(MuhasebeOrnek.Json(t)); }
            object gib = null;
            if (GrupIciBelgeOrnek.Gerekli(m))
            {
                var mevcutB = GrupIciBelgeOrnek.BulSatir(m.DocEntry);
                if (mevcutB == null || mevcutB.Durum != "Muhasebeleşti")
                {
                    grupIci = true; var g = GrupIciBelgeOrnek.FisTaslagi(m, mevcutB); fisler.Add(MuhasebeOrnek.Json(g.fis));
                    if (g.karsilikli) Delta(m.U_OdeyenSirket, m.U_FaturaSahibi, -g.tl); else Delta(m.U_FaturaSahibi, m.U_OdeyenSirket, g.tl);
                    gib = new { no = mevcutB?.No ?? "(kaydedilince)", tur = mevcutB?.Tur ?? m.U_GrupIciTur, sekil = g.sekil, etki = g.bakiyeEtkisi, faturaSahibi = m.U_FaturaSahibi, odeyen = m.U_OdeyenSirket };
                }
            }
            var bakiyeler = delta.Select(kv => { var p = kv.Key.Split('|'); decimal mevcut = CekAkibetOrnek.GrupBakiye(p[0], p[1]); return new { borclu = p[0], alacakli = p[1], mevcut, degisim = kv.Value, sonra = Math.Max(0, mevcut + kv.Value) }; }).ToList();
            return new { docEntry, grupIci, dolasimli, cikisTuru = bankaya ? "Bankaya Tahsile Ver" : "Tedarikçiye Ciro", yer, belgeNo = no, adimlar, fisler, bakiyeler, grupIciBelge = gib, uyarilar, ozet = $"{adimlar.Count} çıkış adımı · {fisler.Count} fiş · {bakiyeler.Count} grup içi bakiye etkisi" + (grupIci ? " · grup içi işlem: onay gerekir" : " · standart çıkış") };
        }
        /// <summary>Talimat hazırlanırken dövizli satırın baz kuru (TCMB türlerinde seriden, fatura üstü / anlaşmalı kurda verilen kur) satıra yazılır.</summary>
        public static (bool ok, string mesaj) KurGuncelle(string dbName, int docEntry, string kaynak, decimal? kur, DateTime? tarih, string not, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                if (string.IsNullOrEmpty(m.U_ParaBirimi) || m.U_ParaBirimi == "TRY") return (true, "TL satırı; kur yok.");
                if (KurOrnek.Kaynaklar.Contains(kaynak ?? "")) m.U_KurKaynagi = kaynak; if (tarih != null) m.U_KurTarihi = tarih.Value.Date; if (not != null) m.U_KurNotu = not.Trim();
                if (!KurOrnek.TcmbKaynagi(m.U_KurKaynagi) && (kur ?? 0) > 0) m.U_Kur = kur;
                KurTamamla(m);
                Log(dbName, docEntry, "Kur", $"Talimat için baz kur: {m.U_KurKaynagi} · {m.U_KurTarihi:dd.MM.yyyy} · {(m.U_Kur ?? 1).ToString("N4", Tr)} · {(m.U_TutarTL ?? 0).ToString("N2", Tr)} ₺" + (string.IsNullOrEmpty(m.U_KurNotu) ? "" : " · " + m.U_KurNotu), kullanici);
                return (true, "Kur yazıldı.");
            }
        }
        /// <summary>Fatura sahibi ≠ ödeyen satırında otomatik grup içi belgenin türü (Mahsup ↔ Temlik).</summary>
        public static (bool ok, string mesaj) GrupIciTurDegistir(string dbName, int docEntry, string tur, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                var r = GrupIciBelgeOrnek.TurDegistir(m, tur, kullanici); if (r.ok) Log(dbName, docEntry, "Grup İçi Belge", r.mesaj, kullanici); return r;
            }
        }
        /// <summary>Satıra ek (dekont, imzalı makbuz / talimat, ciro bordrosu, çek fotokopisi): dosya bellek içinde tutulur, /Rapor79/Ek ile indirilir.</summary>
        public static (bool ok, string mesaj, NakitAkisViewModel satir) EkEkle(string dbName, int docEntry, string ad, string tur, string aciklama, string icerikTipi, byte[] veri, string kullanici)
        {
            Tohumla();
            if (veri != null && veri.LongLength > 8_000_000) return (false, "Dosya 8 MB'ı aşamaz.", null);
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.", null);
                tur = EkTurleri.Contains(tur ?? "") ? tur : "Dekont";
                string dosya = string.IsNullOrWhiteSpace(ad) ? $"{tur.ToLower(Tr).Replace(' ', '_')}_{docEntry}_{m.DekontListesi.Count + 1}.pdf" : ad.Trim();
                var e = new DekontKaydi { Id = ++_sonDekontId, SirketDb = dbName, DocEntry = docEntry, DosyaAdi = dosya, Aciklama = aciklama ?? "", Yukleyen = kullanici ?? "", Tarih = DateTime.Now, Tur = tur, IcerikTipi = icerikTipi ?? "", Veri = veri, Boyut = veri?.LongLength ?? 0 };
                e.DosyaYolu = veri != null ? $"/Rapor79/Ek?dbName={Uri.EscapeDataString(dbName)}&docEntry={docEntry}&id={e.Id}" : "/Dekontlar/" + dosya;
                m.DekontListesi.Add(e); Log(dbName, docEntry, tur == "Dekont" ? "Dekont" : "Ek", $"{tur}: {dosya}" + (veri != null ? $" ({Math.Max(1, e.Boyut / 1024)} KB)" : "") + (string.IsNullOrEmpty(aciklama) ? "" : " · " + aciklama), kullanici);
                return (true, $"{tur} eklendi: {dosya}" + (veri == null ? " (ön yüz örneği: dosya içeriği yok)" : $" ({Math.Max(1, e.Boyut / 1024)} KB; rapor sayfasından açılır)") + ".", m);
            }
        }
        public static DekontKaydi EkBul(string dbName, int docEntry, int id) { var m = Bul(dbName, docEntry); lock (Kilit) return m?.DekontListesi.FirstOrDefault(d => d.Id == id); }
        /// <summary>Makbuz kesilince (Data/MakbuzOrnek.cs): satıra makbuz no / kasa / teslim alan yazılır, satır ÖDENDİ olur (fiş + avans olayı).</summary>
        public static void MakbuzIsle(string dbName, int docEntry, string makbuzNo, string kasa, string teslimAlan, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return;
                m.U_MakbuzNo = makbuzNo; m.U_Kasa = kasa; m.U_TeslimAlan = teslimAlan; Log(dbName, docEntry, "Makbuz", $"{makbuzNo} kesildi · {kasa} · teslim alan {teslimAlan}", kullanici);
                OdendiIsaretle(dbName, docEntry, "Kasa Tediye " + makbuzNo, kullanici);
            }
        }

        // ---------- NAKİT TALEP / ONAY: nakit yalnız onaycılardan biri onaylarsa verilir ----------
        public static (bool ok, string mesaj) NakitOnayla(string dbName, int docEntry, string onaylayan, string not, string kullanici)
        {
            Tohumla();
            if (!NakitOnaycilari.Contains(onaylayan ?? "")) return (false, $"Nakit onayı yalnız {string.Join(" / ", NakitOnaycilari)} tarafından verilebilir.");
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                if (m.U_OdemeYontemi != "Nakit") return (false, "Onay mekanizması nakit ödemeler içindir.");
                m.U_OnayDurumu = "Onaylandı"; m.U_Onaylayan = onaylayan; m.U_OnayTarihi = DateTime.Now; m.U_OnayNotu = not ?? ""; if (m.U_Durum == "Bekliyor" || m.U_Durum == "Özel Onay") { m.U_Durum = "Planlandı"; m.U_EskiDurum = "Planlandı"; }
                Log(dbName, docEntry, "Nakit Onayı", $"Nakit ödeme talebi onaylandı · onaylayan {onaylayan}" + (string.IsNullOrEmpty(not) ? "" : " · " + not), kullanici);
                return (true, $"Nakit ödeme talebi onaylandı ({onaylayan}); makbuz kesilebilir / talimata alınabilir.");
            }
        }
        public static (bool ok, string mesaj) NakitReddet(string dbName, int docEntry, string onaylayan, string not, string kullanici)
        {
            Tohumla();
            if (!NakitOnaycilari.Contains(onaylayan ?? "")) return (false, $"Nakit talebini yalnız {string.Join(" / ", NakitOnaycilari)} reddedebilir.");
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                if (m.U_Durum == "ÖDENDİ") return (false, "Ödenmiş satır reddedilemez.");
                m.U_OnayDurumu = "Reddedildi"; m.U_Onaylayan = onaylayan; m.U_OnayTarihi = DateTime.Now; m.U_OnayNotu = not ?? ""; m.U_Durum = "Bekliyor"; m.U_EskiDurum = "Bekliyor";
                Log(dbName, docEntry, "Nakit Reddi", $"Nakit ödeme talebi reddedildi · {onaylayan}" + (string.IsNullOrEmpty(not) ? "" : " · " + not), kullanici);
                return (true, $"Nakit ödeme talebi reddedildi ({onaylayan}).");
            }
        }

        // ---------- PLANSIZ ÇEK ÇIKIŞI KONTROLÜ: Çek Akıbeti'nden Kasa Nakit Akışı'nda planlanmamış bir çek çıkarılmak istenirse çıkış yapılmaz, GMY onayına düşer ----------
        /// <summary>Çek Akıbeti Raporu işlem türü değişikliği: çıkış türü (ciro, bankaya tahsil, teminat, konkordato-tedarikçi) ve belge planlı değilse Kasa Nakit Akışı'na "Özel Onay" satırı açılır; planlıysa çıkış işlenir.</summary>
        public static (bool ok, string mesaj, bool onayaDustu, int? docEntry) CekCikisKontrollu(int cekDocEntry, string kodVeyaAd, string ibrazYeri, string ibrazBelgeNo, string kullanici, string konum = null)
        {
            Tohumla();
            var k = CekAkibetOrnek.Bul(cekDocEntry); if (k == null) return (false, "Belge bulunamadı.", false, null);
            var ak = CekAkibetOrnek.AdVeKonum(kodVeyaAd, konum); string ad = ak.ad; konum = CekAkibetOrnek.KonumSecenekleri(ad).Length == 1 ? "" : ak.konum;
            // çıkış = ciro / bankaya tahsil / teminat türü ya da portföydeki belgenin konumunun Bankada / Tedarikçide'ye alınması (erteleme, tahsil riski, konkordato …); belge zaten portföy dışındaysa konum değişikliği çıkış sayılmaz
            bool cikis = CekAkibetOrnek.CikisTuruMu(ad, konum) && k.KonumDurumu == "Portföyde";
            if (cikis && k.PlanDocEntry == null && k.Dolasim.Count == 0)
            {
                var m = CekCikisTalebi(k, ad + (string.IsNullOrEmpty(konum) ? "" : " · " + konum), ibrazYeri, ibrazBelgeNo, kullanici);
                return (true, $"DİKKAT: {k.No} Kasa Nakit Akışı'nda planlanmamış; çıkış ({ad}{(string.IsNullOrEmpty(konum) ? "" : " · " + konum)}{(string.IsNullOrEmpty(ibrazYeri) ? "" : " → " + ibrazYeri)}) yapılmadı, GMY onayına düştü (Kasa Nakit Akışı #{m.DocEntry}, {string.Join(" / ", CekCikisOnaycilari)}). Onaylanınca çıkış işlenir.", true, m.DocEntry);
            }
            if (cikis && k.PlanDocEntry != null) { var plan = Bul(k.PlanDocEntry.Value); if (plan != null && !string.IsNullOrEmpty(plan.U_CikisTalebi) && plan.U_OnayDurumu != "Onaylandı") return (false, $"{k.No} için çıkış talebi GMY onayı bekliyor (Kasa Nakit Akışı #{plan.DocEntry}); onaylanmadan çıkış yapılamaz.", true, plan.DocEntry); }
            var r = CekAkibetOrnek.IslemTuruDegistir(cekDocEntry, ad, ibrazYeri, ibrazBelgeNo, kullanici, konum);
            return (r.ok, r.mesaj, false, k.PlanDocEntry);
        }
        /// <summary>Plansız çıkış talebi satırı: belge planlanır (Çek Akıbeti: Planlandı → kontrol "İbraz Bekliyor"), satır "Özel Onay" + "Onay Bekliyor"; onaylanınca çıkış işlenir, reddedilince belge portföye döner. islemTuru "Ad · Konum" biçiminde olabilir.</summary>
        static NakitAkisViewModel CekCikisTalebi(CekKaydi k, string islemTuru, string ibrazYeri, string ibrazBelgeNo, string kullanici)
        {
            // bankaya ibraz türlerinde yer belirtilmemişse Çek Akıbeti'nin kullanacağı tahsil bankası (plan ↔ çıkış kontrolü "Uyumlu" kalsın diye)
            var ak = CekAkibetOrnek.AdVeKonum(islemTuru, null);
            bool bankaya = ak.ad == "Bankaya Tahsile Verildi" || ak.konum == "Bankada";
            if (string.IsNullOrEmpty(ibrazYeri) && bankaya) ibrazYeri = ButceOrnek.TahsilBankalari[k.DocEntry % 2];
            string db = k.DbName; string kurum = !string.IsNullOrEmpty(ibrazYeri) ? (bankaya && !ibrazYeri.Contains("(Tahsil)") ? ibrazYeri + " (Tahsil)" : ibrazYeri) : islemTuru == "Teminata Verildi" ? "Teminat" : "Tedarikçi (belirtilmedi)";
            lock (Kilit)
            {
                if (!_odemeler.ContainsKey(db)) _odemeler[db] = new List<NakitAkisViewModel>();
                var m = new NakitAkisViewModel
                {
                    DocEntry = ++_sonDocEntry, DocNum = _sonDocEntry, U_KaynakFirma = db, U_FaturaSahibi = k.Sirket, U_OdeyenSirket = k.Sirket, U_Kurum = kurum, U_UrunHizmet = $"{k.Belge} çıkış talebi · {islemTuru}", U_OdemeYontemi = k.Belge, U_PlanlananTarih = DateTime.Today, U_SonOdemeTarihi = k.Vade, BelgeTarihi = DateTime.Today,
                    U_ParaBirimi = "TRY", U_Tutar = k.Tutar, U_TutarTL = k.Tutar, U_Kur = 1, U_OdenenTutarPB = "TRY", U_FaturaTutari = k.Tutar, U_BagliBelgeTipi = "Cari Bakiye", U_KayitTipi = "Bakiye", U_IslemTipi = "X", U_Satinalmaci = "", U_Durum = "Özel Onay", U_EskiDurum = "Özel Onay",
                    U_OnayDurumu = "Onay Bekliyor", U_CikisTalebi = islemTuru, U_CikisIbrazYeri = ibrazYeri ?? "", U_CikisBelgeNo = ibrazBelgeNo ?? "", U_CikisTalepEden = kullanici ?? "", U_TalepEden = kullanici ?? "", U_CekDocEntryler = k.DocEntry.ToString(),
                    U_Notlar = $"Çek Akıbeti'nden plansız çıkış talebi ({islemTuru}{(string.IsNullOrEmpty(ibrazYeri) ? "" : " → " + ibrazYeri)}) · talep eden {kullanici} · GMY onayı bekliyor", U_Sira = _odemeler[db].Count + 1
                };
                _odemeler[db].Add(m);
                CekAkibetOrnek.Planla(k.DocEntry, kurum, m.DocEntry, k.Sirket, kullanici); BelgeleriDoldur(m);
                Log(db, m.DocEntry, "Çek Çıkış Talebi", $"{k.No} · {k.Tutar.ToString("N2", Tr)} TL · {islemTuru}{(string.IsNullOrEmpty(ibrazYeri) ? "" : " → " + ibrazYeri)} · Kasa Nakit Akışı'nda planlanmamıştı; GMY onayı bekliyor", kullanici);
                return m;
            }
        }
        public static (bool ok, string mesaj) CekCikisOnayla(string dbName, int docEntry, string onaylayan, string not, string kullanici)
        {
            Tohumla();
            if (!CekCikisOnaycilari.Contains(onaylayan ?? "")) return (false, $"Çek çıkışını yalnız GMY ({string.Join(" / ", CekCikisOnaycilari)}) onaylayabilir.");
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                if (string.IsNullOrEmpty(m.U_CikisTalebi)) return (false, "Bu satırda çek çıkış talebi yok.");
                if (m.U_OnayDurumu == "Onaylandı") return (false, "Talep zaten onaylanmış.");
                m.U_OnayDurumu = "Onaylandı"; m.U_Onaylayan = onaylayan; m.U_OnayTarihi = DateTime.Now; m.U_OnayNotu = not ?? "";
                var sonuclar = new List<string>();
                foreach (var id in (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => int.TryParse(x, out var n) ? n : 0).Where(n => n > 0))
                { var r = CekAkibetOrnek.IslemTuruDegistir(id, m.U_CikisTalebi, m.U_CikisIbrazYeri, m.U_CikisBelgeNo, kullanici); sonuclar.Add(r.mesaj); }
                Log(dbName, docEntry, "Çek Çıkış Onayı", $"GMY onayı: {onaylayan} · {m.U_CikisTalebi}{(string.IsNullOrEmpty(m.U_CikisIbrazYeri) ? "" : " → " + m.U_CikisIbrazYeri)}" + (string.IsNullOrEmpty(not) ? "" : " · " + not), kullanici);
                OdendiIsaretle(dbName, docEntry, "Çek çıkış onayı (" + onaylayan + ")", kullanici);
                return (true, $"Çıkış onaylandı ({onaylayan}): {string.Join(" ", sonuclar)}");
            }
        }
        public static (bool ok, string mesaj) CekCikisReddet(string dbName, int docEntry, string onaylayan, string not, string kullanici)
        {
            Tohumla();
            if (!CekCikisOnaycilari.Contains(onaylayan ?? "")) return (false, $"Çek çıkış talebini yalnız GMY ({string.Join(" / ", CekCikisOnaycilari)}) reddedebilir.");
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.");
                if (string.IsNullOrEmpty(m.U_CikisTalebi) || m.U_OnayDurumu == "Onaylandı") return (false, "Reddedilecek bekleyen çıkış talebi yok.");
                string nolar = m.U_CekNolar;
                foreach (var id in (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)) if (int.TryParse(id, out var n)) CekAkibetOrnek.PlanKaldir(n, kullanici);
                l.Remove(m); Log(dbName, docEntry, "Çek Çıkış Reddi", $"GMY reddetti: {onaylayan} · {nolar} portföyde kaldı · {m.U_CikisTalebi}{(string.IsNullOrEmpty(m.U_CikisIbrazYeri) ? "" : " → " + m.U_CikisIbrazYeri)}" + (string.IsNullOrEmpty(not) ? "" : " · " + not), kullanici);
                return (true, $"Çıkış talebi reddedildi ({onaylayan}); {nolar} portföyde kaldı, satır kaldırıldı.");
            }
        }

        // ---------- KISMİ ÖDEME / TAKSİT: belge tutarı sabit, satır tutarı oran; kardeş satırlar aynı belge no ile izlenir ----------
        public static (bool ok, string mesaj, List<NakitAkisViewModel> satirlar) Taksitlendir(string dbName, int docEntry, List<(decimal oran, DateTime plan)> parcalar, string kullanici)
        {
            Tohumla();
            parcalar = (parcalar ?? new List<(decimal, DateTime)>()).Where(p => p.oran > 0).ToList();
            if (parcalar.Count < 2) return (false, "En az iki parça girin (ör. %70 + %30).", null);
            decimal toplamOran = parcalar.Sum(p => p.oran); if (Math.Abs(toplamOran - 100) > 0.5m) return (false, $"Oranların toplamı %100 olmalı (girilen %{toplamOran.ToString("N1", Tr)}).", null);
            var m0 = Bul(dbName, docEntry); if (m0 == null) return (false, "Ödeme satırı bulunamadı.", null);
            if (!string.IsNullOrEmpty(m0.U_TalimatNo) && !(TalimatOrnek.Bul(m0.U_TalimatNo)?.Kapandi ?? true)) return (false, $"Satır {m0.U_TalimatNo} talimatında; önce talimatı iptal edin.", null);   // kilit sırası: Talimat → NakitAkis (burada kilit dışında)
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return (false, "Ödeme satırı bulunamadı.", null);
                if (m.U_Durum == "ÖDENDİ") return (false, "Ödenmiş satır taksitlendirilemez.", null);
                decimal toplam = m.U_Tutar ?? 0; if (toplam <= 0) return (false, "Satır tutarı sıfır.", null);
                m.U_FaturaTutari ??= toplam; int n = parcalar.Count; var sonuc = new List<NakitAkisViewModel> { m };
                string eskiNot = m.U_Notlar ?? "";
                for (int i = 0; i < n; i++)
                {
                    decimal tutar = i == n - 1 ? Math.Round(toplam - sonuc.Take(i).Sum(x => x.U_Tutar ?? 0), 2) : Math.Round(toplam * parcalar[i].oran / 100m, 2);
                    NakitAkisViewModel h;
                    if (i == 0) h = m;
                    else
                    {
                        h = new NakitAkisViewModel { DocEntry = ++_sonDocEntry, U_KaynakFirma = dbName, U_UstDocEntry = m.U_UstDocEntry ?? m.DocEntry, U_Durum = "Bekliyor", U_EskiDurum = "Bekliyor", BelgeTarihi = m.BelgeTarihi, U_Sira = l.Count + 1, U_OdenenTutarPB = m.U_ParaBirimi, U_Notlar = "" };
                        var t = typeof(NakitAkisViewModel); foreach (var ad in new[] { "U_Kurum", "U_UrunHizmet", "U_ParaBirimi", "U_Kur", "U_OdemeYontemi", "U_BagliBelgeTipi", "U_BagliDocEntry", "U_KayitTipi", "U_FaturaSiparisNo", "U_IslemTipi", "U_Satinalmaci", "U_FaturaSahibi", "U_OdeyenSirket", "U_SonOdemeTarihi", "U_FaturaTutari", "U_BankaHesabi", "U_AliciIban", "U_AliciBanka", "U_AliciHesapAdi", "U_Kasa", "U_Kart", "U_TalepEden", "U_KurKaynagi", "U_KurNotu", "U_KurFarkiFaturasi", "U_GrupIciTur" }) { var p = t.GetProperty(ad); p.SetValue(h, p.GetValue(m)); }
                        h.DocNum = h.DocEntry; if (h.U_OdemeYontemi == "Nakit") h.U_OnayDurumu = "Onay Bekliyor"; l.Add(h);
                    }
                    h.U_Tutar = tutar; h.U_TutarTL = Math.Round(tutar * (h.U_Kur ?? 1), 2); h.U_PlanlananTarih = parcalar[i].plan.Date; if (h.U_SonOdemeTarihi != null && h.U_PlanlananTarih > h.U_SonOdemeTarihi && i > 0) h.U_SonOdemeTarihi = h.U_PlanlananTarih;
                    decimal oranBelge = (m.U_FaturaTutari ?? toplam) > 0 ? Math.Round(tutar / (m.U_FaturaTutari ?? toplam) * 100m, 1) : parcalar[i].oran;
                    h.U_Notlar = $"Taksit {i + 1}/{n} · %{oranBelge.ToString("N1", Tr)} (belge {(m.U_FaturaTutari ?? toplam).ToString("N2", Tr)} {m.U_ParaBirimi})" + (i == 0 && eskiNot != "" && !eskiNot.StartsWith("Taksit") && !eskiNot.StartsWith("Kısmi") ? " · " + eskiNot : "");
                    if (i > 0) { h.U_KurTarihi = h.U_PlanlananTarih; KurTamamla(h); GrupIciBelgeOrnek.Senkron(h, kullanici); }
                    if (i > 0) sonuc.Add(h);
                }
                Log(dbName, m.DocEntry, "Taksit", $"{n} parçaya bölündü: {string.Join(" + ", sonuc.Select(x => (x.U_Tutar ?? 0).ToString("N2", Tr) + " " + x.U_ParaBirimi + " (" + (x.U_PlanlananTarih ?? DateTime.Today).ToString("dd.MM.yyyy") + ")"))}", kullanici);
                foreach (var h in sonuc.Skip(1)) Log(dbName, h.DocEntry, "Ekleme", $"Taksit satırı · {h.U_Kurum} · {(h.U_Tutar ?? 0).ToString("N2", Tr)} {h.U_ParaBirimi} · belge {m.U_FaturaSiparisNo}", kullanici);
                return (true, $"{m.U_Kurum} · {m.U_FaturaSiparisNo}: {n} parça — " + string.Join(" + ", sonuc.Select(x => (x.U_Tutar ?? 0).ToString("N2", Tr) + " " + x.U_ParaBirimi)), sonuc);
            }
        }

        // ---------- BELGE / ÖDEME PLANI / MÜKERRER ----------
        static string BelgeAnahtari(NakitAkisViewModel m) => string.IsNullOrEmpty(m.U_FaturaSiparisNo) ? "" : (string.IsNullOrEmpty(m.U_FaturaSahibi) ? SirketAdi(m.U_KaynakFirma) : m.U_FaturaSahibi) + "|" + m.U_FaturaSiparisNo.Trim().ToUpper(Tr);
        /// <summary>Aynı belgenin satırları (kısmi ödeme / taksit / mükerrer): tüm şirketlerde aynı fatura sahibi + belge no.</summary>
        public static List<NakitAkisViewModel> BelgeSatirlari(NakitAkisViewModel m) { Tohumla(); string key = BelgeAnahtari(m); if (key == "") return new List<NakitAkisViewModel> { m }; lock (Kilit) return _odemeler.Values.SelectMany(l => l).Where(x => BelgeAnahtari(x) == key).OrderBy(x => x.U_PlanlananTarih).ThenBy(x => x.DocEntry).ToList(); }
        /// <summary>Satırın belge özeti: belge tutarı, bu satırın oranı, planlanan toplam, ödenen, kalan, taksit sırası ve mükerrer uyarıları.</summary>
        public static object BelgeOzeti(NakitAkisViewModel m)
        {
            var kardes = BelgeSatirlari(m); decimal fatura = m.U_FaturaTutari ?? 0; decimal tutar = m.U_Tutar ?? 0;
            decimal planlanan = kardes.Sum(x => x.U_Tutar ?? 0), odenen = kardes.Where(x => x.U_Durum == "ÖDENDİ").Sum(x => x.U_OdenenTutar ?? x.U_Tutar ?? 0);
            var uyarilar = new List<object>();
            var ayni = kardes.Where(x => x.DocEntry != m.DocEntry && x.U_Tutar == m.U_Tutar && x.U_ParaBirimi == m.U_ParaBirimi && !string.IsNullOrEmpty(m.U_FaturaSiparisNo) && x.U_UstDocEntry == null && m.U_UstDocEntry == null).ToList();
            if (ayni.Count > 0) uyarilar.Add(new { tip = "Mükerrer Kayıt", aciklama = $"Aynı belge ({m.U_FaturaSiparisNo}), aynı tutar ({tutar.ToString("N2", Tr)} {m.U_ParaBirimi}) {ayni.Count + 1} kez girilmiş", docEntryler = ayni.Select(x => x.DocEntry).ToList() });
            if (fatura > 0 && planlanan > fatura * 1.001m) uyarilar.Add(new { tip = "Fazla Ödeme", aciklama = $"Belge tutarı {fatura.ToString("N2", Tr)} {m.U_ParaBirimi}, planlanan toplam {planlanan.ToString("N2", Tr)} (%{(planlanan / fatura * 100).ToString("N0", Tr)})", docEntryler = kardes.Where(x => x.DocEntry != m.DocEntry).Select(x => x.DocEntry).ToList() });
            int sira = kardes.FindIndex(x => x.DocEntry == m.DocEntry) + 1;
            return new
            {
                faturaTutari = m.U_FaturaTutari, pb = m.U_ParaBirimi, oran = fatura > 0 ? Math.Round(tutar / fatura * 100m, 1) : (decimal?)null, planlanan, odenen, kalan = fatura > 0 ? Math.Round(fatura - planlanan, 2) : (decimal?)null, odenmemis = Math.Round(fatura > 0 ? fatura - odenen : planlanan - odenen, 2),
                taksit = kardes.Count > 1 ? $"{sira}/{kardes.Count}" : "", kardesler = kardes.Where(x => x.DocEntry != m.DocEntry).Select(x => new { docEntry = x.DocEntry, dbName = x.U_KaynakFirma, sirket = SirketAdi(x.U_KaynakFirma), planlanan = x.U_PlanlananTarih?.ToString("yyyy-MM-dd"), tutar = x.U_Tutar, pb = x.U_ParaBirimi, oran = fatura > 0 ? Math.Round((x.U_Tutar ?? 0) / fatura * 100m, 1) : (decimal?)null, durum = x.U_Durum, yontem = x.U_OdemeYontemi, odenme = x.U_OdenmeTarihi?.ToString("yyyy-MM-dd") }).ToList(),
                uyarilar
            };
        }
        /// <summary>Kayıt öncesi mükerrer kontrolü: aynı fatura sahibi + belge no + tutar + para birimi ya da aynı cari + tutar + planlanan tarih.</summary>
        public static List<NakitAkisViewModel> MukerrerAdaylar(string dbName, NakitAkisViewModel gelen)
        {
            Tohumla(); string key = BelgeAnahtari(new NakitAkisViewModel { U_FaturaSiparisNo = gelen.U_FaturaSiparisNo, U_FaturaSahibi = string.IsNullOrEmpty(gelen.U_FaturaSahibi) ? SirketAdi(dbName) : gelen.U_FaturaSahibi, U_KaynakFirma = dbName });
            lock (Kilit) return _odemeler.Values.SelectMany(l => l).Where(x => x.DocEntry != gelen.DocEntry)
                .Where(x => (key != "" && BelgeAnahtari(x) == key && x.U_Tutar == gelen.U_Tutar && (x.U_ParaBirimi ?? "TRY") == (gelen.U_ParaBirimi ?? "TRY")) || (string.Equals(x.U_Kurum, gelen.U_Kurum, StringComparison.OrdinalIgnoreCase) && x.U_Tutar == gelen.U_Tutar && (x.U_ParaBirimi ?? "TRY") == (gelen.U_ParaBirimi ?? "TRY") && x.U_PlanlananTarih?.Date == gelen.U_PlanlananTarih?.Date)).Take(5).ToList();
        }
        /// <summary>Belge detayı (belge no tıklanınca): fatura / sipariş başlık + kalemleri (deterministik örnek), ödeme planı, mükerrer uyarıları; Cari Bakiye satırında carinin açık kalemleri.</summary>
        public static object BelgeDetay(string dbName, int docEntry)
        {
            var m = Bul(dbName, docEntry); if (m == null) return null;
            string tip = m.U_BagliBelgeTipi ?? ""; var rnd = new Random(Seed("belge|" + (m.U_BagliDocEntry?.ToString() ?? m.U_FaturaSiparisNo ?? m.DocEntry.ToString())));
            var kalemAdlari = new[] { "Sodyum Hidroksit %50", "Polietilen Granül", "Etiket Rulosu 100x50", "Nakliye Hizmeti", "Bakım Hizmeti", "Solvent Bazlı Reçine", "Titanyum Dioksit", "Oluklu Mukavva Kutu", "Kalsiyum Karbonat", "Forklift Kiralama" };
            decimal belgeTutar = m.U_FaturaTutari ?? m.U_Tutar ?? 0; decimal kdvOran = tip == "Yansıtma" || tip == "Mahsup" || tip == "Temlik" || tip == "Avans" ? 0 : 0.20m; decimal net = Math.Round(belgeTutar / (1 + kdvOran), 2);
            var kalemler = new List<object>();
            if (tip == "Fatura" || tip == "Sipariş")
            {
                int n = 2 + rnd.Next(3); decimal kalan = net; var paylar = Enumerable.Range(0, n).Select(_ => (decimal)rnd.Next(10, 60)).ToList(); decimal pt = paylar.Sum();
                for (int i = 0; i < n; i++) { decimal tutar = i == n - 1 ? kalan : Math.Round(net * paylar[i] / pt, 2); kalan -= tutar; decimal miktar = rnd.Next(2, 40) * 25; kalemler.Add(new { kod = $"HM-{1000 + rnd.Next(900)}", ad = kalemAdlari[rnd.Next(kalemAdlari.Length)], miktar, birim = i % 3 == 2 ? "Hizmet" : "kg", birimFiyat = miktar > 0 ? Math.Round(tutar / miktar, 4) : tutar, tutar, kdv = Math.Round(tutar * kdvOran, 2) }); }
            }
            else if (tip == "Avans") kalemler.Add(new { kod = m.U_AvansNo, ad = m.U_UrunHizmet, miktar = 1m, birim = "Avans", birimFiyat = belgeTutar, tutar = belgeTutar, kdv = 0m });
            else if (tip != "Cari Bakiye") kalemler.Add(new { kod = m.U_FaturaSiparisNo, ad = m.U_UrunHizmet, miktar = 1m, birim = tip, birimFiyat = belgeTutar, tutar = belgeTutar, kdv = 0m });
            List<object> acikKalemler = null; decimal? cariBakiye = null;
            if (tip == "Cari Bakiye")
            {
                var r2 = new Random(Seed("cari|" + dbName + "|" + m.U_Kurum)); int n = 3 + r2.Next(3); decimal toplam = 0; acikKalemler = new List<object>();
                for (int i = 0; i < n; i++) { var t = DateTime.Today.AddDays(-r2.Next(10, 120)); decimal tutar = Math.Round((decimal)(r2.NextDouble() * 400_000 + 20_000) / 100m) * 100m; decimal odenen = r2.NextDouble() < 0.4 ? Math.Round(tutar * (decimal)r2.NextDouble() / 100m) * 100m : 0; toplam += tutar - odenen; acikKalemler.Add(new { belge = i % 4 == 3 ? "İrsaliyeli Fatura" : "Fatura", no = $"FT-{t.Year}-{r2.Next(10000, 99999)}", tarih = t.ToString("yyyy-MM-dd"), vade = t.AddDays(new[] { 30, 45, 60, 90 }[r2.Next(4)]).ToString("yyyy-MM-dd"), tutar, odenen, kalan = tutar - odenen, pb = "TRY" }); }
                cariBakiye = toplam;
            }
            return new
            {
                docEntry = m.DocEntry, dbName, sirket = SirketAdi(dbName), tip, no = m.U_FaturaSiparisNo, bagliDocEntry = m.U_BagliDocEntry, tarih = m.BelgeTarihi?.ToString("yyyy-MM-dd"), sonOdeme = m.U_SonOdemeTarihi?.ToString("yyyy-MM-dd"), kurum = m.U_Kurum, faturaSahibi = m.U_FaturaSahibi, odeyenSirket = m.U_OdeyenSirket, satinalmaci = m.U_Satinalmaci, urun = m.U_UrunHizmet,
                pb = m.U_ParaBirimi, kur = m.U_Kur, belgeTutari = belgeTutar, net, kdv = Math.Round(belgeTutar - net, 2), belgeTutariTL = Math.Round(belgeTutar * (m.U_Kur ?? 1), 2), kalemler, acikKalemler, cariBakiye, ozet = BelgeOzeti(m),
                link = tip == "Mahsup" ? "/GrupIci/Mahsup?no=" + Uri.EscapeDataString(m.U_MahsupFisNo ?? "") : tip == "Temlik" ? "/GrupIci/Temlik?no=" + Uri.EscapeDataString(m.U_TemlikNo ?? "") : tip == "Yansıtma" ? "/GrupIci/Yansitma?no=" + Uri.EscapeDataString(m.U_YansitmaNo ?? "") : tip == "Avans" ? "/IkTalep/Avans?no=" + Uri.EscapeDataString(m.U_AvansNo ?? "") : ""
            };
        }
        /// <summary>
        /// Yeni Ödeme › Ödenmemiş Faturalar / Cari Bakiyeleri: şirketin carileri Cari Mutabakat ekstresinden (Data/MutabakatOrnek.cs) — açık faturalar (kalan, vade, gecikme),
        /// vadesi geçmiş / gelecek toplamları (TL), döviz özeti, cari ekstre bağlantıları (Cari Ekstre Raporu /Rapor20?bpCode=…, yazdırılabilir /CariMutabakat/Ekstre?kod=…).
        /// </summary>
        public static object CariAcikFaturalar(string dbName)
        {
            Tohumla(); string sirket = SirketAdi(dbName); var bugun = DateTime.Today;
            List<NakitAkisViewModel> mevcut; lock (Kilit) mevcut = _odemeler.Values.SelectMany(l => l).Where(m => m.U_Durum != "ÖDENDİ").ToList();
            decimal Tl(decimal v, string pb) => Math.Round(v * (Kurlar.TryGetValue(pb, out var kk) ? kk : 1m), 2);
            var cariler = MutabakatOrnek.Cariler.Where(c => c.Kart != null && c.Kart.Sirket == sirket && c.Kart.KartTipi != "Müşteri").Select(c =>
            {
                var acik = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Kalan > 0 && h.KapamaDurumu != "Kapalı").OrderBy(h => h.Vade ?? h.Tarih).ToList();
                var faturalar = acik.Select(h => { string pb = h.ParaBirimi == "TL" || string.IsNullOrEmpty(h.ParaBirimi) ? "TRY" : h.ParaBirimi; var vade = h.Vade ?? h.Tarih.AddDays(30); return new { belgeNo = h.BelgeNo, tarih = h.Tarih.ToString("yyyy-MM-dd"), vade = vade.ToString("yyyy-MM-dd"), tutar = h.Borc > 0 ? h.Borc : h.Alacak, kalan = h.Kalan, pb, kalanTL = Tl(h.Kalan, pb), aciklama = h.Aciklama, gecikme = (bugun - vade).Days, kapama = h.KapamaDurumu, planlandi = mevcut.Any(m => string.Equals(m.U_FaturaSiparisNo, h.BelgeNo, StringComparison.OrdinalIgnoreCase)) }; }).ToList();
                decimal toplam = faturalar.Sum(f => f.kalanTL), gecmis = faturalar.Where(f => f.gecikme > 0).Sum(f => f.kalanTL);
                var tb = TedarikciBanka(c.Kart.KisaAd); int vadeGun = VadeGunu(c.Kart.VadeSarti);
                return new
                {
                    kod = c.CariKodu, ad = c.Kart.KisaAd, unvan = c.Kart.ResmiUnvan, kartTipi = c.Kart.KartTipi, sinif = c.Kart.Sinif, temsilciler = MutabakatOrnek.Temsilciler(c.Kart), adet = faturalar.Count, toplam, gecmis, gelecek = toplam - gecmis,
                    vadeSarti = c.Kart.VadeSarti ?? $"Vade {vadeGun} Gün", vadeGun, sonOdemeOnerisi = bugun.AddDays(vadeGun).ToString("yyyy-MM-dd"), banka = tb.banka, iban = tb.iban, hesapAdi = tb.hesapAdi,
                    gecmisAdet = faturalar.Count(f => f.gecikme > 0), enEskiVade = faturalar.Where(f => f.gecikme > 0).Select(f => f.vade).DefaultIfEmpty("").Min(), ilkVade = faturalar.Where(f => f.gecikme <= 0).Select(f => f.vade).DefaultIfEmpty("").Min(), enUzunGecikme = faturalar.Select(f => f.gecikme).DefaultIfEmpty(0).Max(),
                    dovizler = c.Dovizler.Select(d => new { pb = d.ParaBirimi, acikAdet = d.AcikFaturaAdet, acikTutar = d.AcikFaturaTutar, enEski = d.EnEskiAcikFatura?.ToString("yyyy-MM-dd") }), bakiyeZamani = c.BakiyeZamani?.ToString("yyyy-MM-dd HH:mm"),
                    planlanan = mevcut.Where(m => string.Equals(m.U_Kurum, c.Kart.KisaAd, StringComparison.OrdinalIgnoreCase)).Sum(m => m.U_TutarTL ?? 0),
                    ekstre = "/Rapor20/Index?bpCode=" + Uri.EscapeDataString(c.CariKodu), ekstreYazdir = "/CariMutabakat/Ekstre?kod=" + Uri.EscapeDataString(c.CariKodu), faturalar
                };
            }).OrderByDescending(x => x.toplam).ThenBy(x => x.ad).ToList();
            var talepler = OdemeTalepOrnek.Listele(dbName).Select(OdemeTalepOrnek.Json).ToList();
            var temsilciler = cariler.SelectMany(x => x.temsilciler).Concat(OdemeTalepOrnek.Listele(dbName).Select(t => t.TalepEden)).Distinct().OrderBy(x => x, StringComparer.Create(Tr, false)).ToList();
            return new { sirket, cariler, toplam = cariler.Sum(x => x.toplam), gecmis = cariler.Sum(x => x.gecmis), gelecek = cariler.Sum(x => x.gelecek), adet = cariler.Sum(x => x.adet), talepler, temsilciler, departmanlar = OdemeTalepOrnek.Departmanlar };
        }
        /// <summary>Çekmecede "Belge Seç": carinin açık faturaları / siparişleri (deterministik örnek; Kasa Nakit Akışı'ndaki satırlarla eşleşen belgeler "planlanan" ile gelir) ya da cari bakiyesi.</summary>
        public static object CariBelgeler(string dbName, string kurum, string tip)
        {
            Tohumla(); var rnd = new Random(Seed("cari|" + dbName + "|" + kurum)); var liste = new List<object>();
            List<NakitAkisViewModel> mevcut; lock (Kilit) mevcut = _odemeler.Values.SelectMany(l => l).Where(x => string.Equals(x.U_Kurum, kurum, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var m in mevcut.Where(x => !string.IsNullOrEmpty(x.U_FaturaSiparisNo) && (x.U_BagliBelgeTipi == "Fatura" || x.U_BagliBelgeTipi == "Sipariş")).GroupBy(x => x.U_FaturaSiparisNo).Select(g => g.First()).Where(x => string.IsNullOrEmpty(tip) || x.U_BagliBelgeTipi == tip))
            {
                var ozet = BelgeOzeti(m); decimal fatura = m.U_FaturaTutari ?? m.U_Tutar ?? 0; var kardes = BelgeSatirlari(m); decimal planlanan = kardes.Sum(x => x.U_Tutar ?? 0);
                liste.Add(new { tip = m.U_BagliBelgeTipi, no = m.U_FaturaSiparisNo, bagliDocEntry = m.U_BagliDocEntry, tarih = m.BelgeTarihi?.ToString("yyyy-MM-dd"), sonOdeme = m.U_SonOdemeTarihi?.ToString("yyyy-MM-dd"), tutar = fatura, pb = m.U_ParaBirimi, kur = m.U_Kur, planlanan, kalan = Math.Round(fatura - planlanan, 2), satirSayisi = kardes.Count, mevcutDocEntry = m.DocEntry, satinalmaci = m.U_Satinalmaci, urun = m.U_UrunHizmet, kaynak = "Nakit akışta" });
            }
            int n = 3 + rnd.Next(3);
            for (int i = 0; i < n; i++)
            {
                string t = string.IsNullOrEmpty(tip) ? (i % 3 == 2 ? "Sipariş" : "Fatura") : tip; if (t == "Cari Bakiye") break;
                var tarih = DateTime.Today.AddDays(-rnd.Next(3, 75)); string pb = rnd.NextDouble() < 0.75 ? "TRY" : rnd.NextDouble() < 0.6 ? "USD" : "EUR"; decimal tutar = pb == "TRY" ? Math.Round((decimal)(rnd.NextDouble() * 900_000 + 40_000) / 100m) * 100m : Math.Round((decimal)(rnd.NextDouble() * 90_000 + 5_000) / 100m) * 100m;
                liste.Add(new { tip = t, no = (t == "Fatura" ? "FT-" : "SP-") + tarih.Year + "-" + rnd.Next(10000, 99999), bagliDocEntry = 40000 + rnd.Next(1, 9999), tarih = tarih.ToString("yyyy-MM-dd"), sonOdeme = tarih.AddDays(new[] { 30, 45, 60, 90 }[rnd.Next(4)]).ToString("yyyy-MM-dd"), tutar, pb, kur = Kurlar.TryGetValue(pb, out var kk) ? kk : 1m, planlanan = 0m, kalan = tutar, satirSayisi = 0, mevcutDocEntry = (int?)null, satinalmaci = Satinalmacilar[rnd.Next(Satinalmacilar.Length)], urun = Urunler[rnd.Next(Urunler.Length)], kaynak = t == "Sipariş" ? "Açık sipariş" : "Açık fatura" });
            }
            var r2 = new Random(Seed("cari|" + dbName + "|" + kurum)); int ak = 3 + r2.Next(3); decimal bakiye = 0, vadesiGecmis = 0; var acik = new List<object>();
            for (int i = 0; i < ak; i++) { var t = DateTime.Today.AddDays(-r2.Next(10, 120)); var vade = t.AddDays(new[] { 30, 45, 60, 90 }[r2.Next(4)]); decimal tutar = Math.Round((decimal)(r2.NextDouble() * 400_000 + 20_000) / 100m) * 100m; decimal odenen = r2.NextDouble() < 0.4 ? Math.Round(tutar * (decimal)r2.NextDouble() / 100m) * 100m : 0; bakiye += tutar - odenen; if (vade < DateTime.Today) vadesiGecmis += tutar - odenen; acik.Add(new { belge = i % 4 == 3 ? "İrsaliyeli Fatura" : "Fatura", no = $"FT-{t.Year}-{r2.Next(10000, 99999)}", tarih = t.ToString("yyyy-MM-dd"), vade = vade.ToString("yyyy-MM-dd"), tutar, odenen, kalan = tutar - odenen, pb = "TRY" }); }
            decimal planlananBakiye = mevcut.Where(x => x.U_BagliBelgeTipi == "Cari Bakiye" && x.U_Durum != "ÖDENDİ").Sum(x => x.U_TutarTL ?? 0);
            return new { kurum, belgeler = liste, cariBakiye = new { toplam = bakiye, pb = "TRY", acikKalemler = acik, planlanan = planlananBakiye, kalan = bakiye - planlananBakiye, vadesiGecmis } };
        }
        public static NakitAkisViewModel Ekle(string dbName, NakitAkisViewModel gelen, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                if (!_odemeler.ContainsKey(dbName)) _odemeler[dbName] = new List<NakitAkisViewModel>();
                var m = new NakitAkisViewModel { DocEntry = ++_sonDocEntry, U_KaynakFirma = dbName, U_Durum = "Bekliyor", BelgeTarihi = DateTime.Today, U_ParaBirimi = "TRY", U_Kur = 1, U_OdenenTutarPB = "TRY", U_Notlar = "" };
                var t = typeof(NakitAkisViewModel); foreach (var ad in KopyalananAlanlar) { var p = t.GetProperty(ad); var v = p.GetValue(gelen); if (v != null && !(v is string sv && sv == "")) p.SetValue(m, v); }
                m.DocNum = m.DocEntry; if (m.U_ParaBirimi == "EURO") m.U_ParaBirimi = "EUR"; if (string.IsNullOrEmpty(m.U_Durum) || m.U_Durum == "ÖDENDİ") m.U_Durum = "Bekliyor"; m.U_EskiDurum = m.U_Durum;
                if ((m.U_TutarTL ?? 0) == 0 && m.U_Tutar.HasValue) m.U_TutarTL = Math.Round(m.U_Tutar.Value * (m.U_Kur ?? 1), 2);
                if (string.IsNullOrEmpty(m.U_BagliBelgeTipi) || m.U_BagliBelgeTipi == "Manuel") m.U_BagliBelgeTipi = "Cari Bakiye"; if (m.U_BagliBelgeTipi == "Cari Bakiye") { m.U_KayitTipi = "Bakiye"; m.U_FaturaSiparisNo = ""; } else if (string.IsNullOrEmpty(m.U_KayitTipi)) m.U_KayitTipi = m.U_BagliBelgeTipi;
                if (m.U_FaturaTutari == null && m.U_BagliBelgeTipi != "Cari Bakiye") m.U_FaturaTutari = m.U_Tutar; if (m.U_SonOdemeTarihi == null) m.U_SonOdemeTarihi = m.U_PlanlananTarih;
                if (string.IsNullOrEmpty(m.U_FaturaSahibi)) m.U_FaturaSahibi = SirketAdi(dbName); if (string.IsNullOrEmpty(m.U_OdeyenSirket)) m.U_OdeyenSirket = SirketAdi(dbName);
                if (string.IsNullOrEmpty(m.U_TalepEden)) m.U_TalepEden = kullanici ?? "";
                // nakit ödeme = talep: onaycılardan biri onaylamadan ödenmez
                if (m.U_OdemeYontemi == "Nakit") { m.U_OnayDurumu = "Onay Bekliyor"; m.U_Kasa = string.IsNullOrEmpty(m.U_Kasa) ? Kasalar[0] : m.U_Kasa; }
                // havale: alıcı banka bilgisi boşsa cari karttan / son ödemeden; dövizli: kur kaynağı ve günü; fatura sahibi ≠ ödeyen: otomatik grup içi belge
                if (m.U_OdemeYontemi == "Havale / EFT" && string.IsNullOrEmpty(m.U_AliciIban)) { var tb = TedarikciBanka(m.U_Kurum); m.U_AliciBanka = tb.banka; m.U_AliciIban = tb.iban; m.U_AliciHesapAdi = tb.hesapAdi; }
                KurTamamla(m); if (GrupIciBelgeOrnek.Gerekli(m) && string.IsNullOrEmpty(m.U_GrupIciTur)) m.U_GrupIciTur = "Mahsup";
                m.U_Sira = _odemeler[dbName].Count + 1;
                _odemeler[dbName].Add(m); Log(dbName, m.DocEntry, m.U_OdemeYontemi == "Nakit" ? "Nakit Talebi" : "Ekleme", $"{m.U_Kurum} · {m.U_OdemeYontemi} · {(m.U_Tutar ?? 0).ToString("N2", Tr)} {m.U_ParaBirimi}" + (m.U_OdemeYontemi == "Nakit" ? $" · onay bekliyor ({string.Join(" / ", NakitOnaycilari)})" : "") + (m.U_ParaBirimi != "TRY" ? $" · kur {(m.U_Kur ?? 1).ToString("N4", Tr)} ({m.U_KurKaynagi} {m.U_KurTarihi:dd.MM.yyyy})" : ""), kullanici);
                var gib = GrupIciBelgeOrnek.Senkron(m, kullanici); if (gib != null) Log(dbName, m.DocEntry, "Grup İçi Belge", $"{gib.No} ({gib.Baslik}) hazırlandı: {gib.Odeyen}, {gib.FaturaSahibi} adına ödüyor; ödeme gerçekleşince iki şirkette fiş atılır", kullanici);
                return m;
            }
        }
        /// <summary>Finekra banka hareketinden nakit akış satırı (Takip Paneli › Nakit Akışa Gönder): satır ÖDENDİ olarak açılır; fiş zaten atıldıysa numarası yazılır, yoksa atılır.</summary>
        public static NakitAkisViewModel BankaHareketindenEkle(string dbName, NakitAkisViewModel gelen, DateTime odenmeTarihi, string yevmiyeNo, string kullanici, bool fisAt = true)
        {
            var m = Ekle(dbName, gelen, kullanici);
            lock (Kilit)
            {
                m.U_BankaHesabi = gelen.U_BankaHesabi ?? ""; m.U_DekontNo = gelen.U_DekontNo ?? ""; m.U_Durum = "ÖDENDİ"; m.U_EskiDurum = "ÖDENDİ"; m.U_OdenmeTarihi = odenmeTarihi.Date; m.U_OdenenTutar = m.U_Tutar; m.U_OdenenTutarPB = m.U_ParaBirimi;
                if (!string.IsNullOrEmpty(yevmiyeNo)) m.U_YevmiyeNo = yevmiyeNo; else if (fisAt) FisAt(m, "Banka hareketi " + m.U_DekontNo, kullanici);
                Log(dbName, m.DocEntry, "Ödeme", $"Banka hareketinden ÖDENDİ · {m.U_Kurum} · {(m.U_Tutar ?? 0).ToString("N2", Tr)} ₺ · {m.U_DekontNo}" + (string.IsNullOrEmpty(m.U_YevmiyeNo) ? " · fiş Takip Paneli onayında (cari / hesap seçilince) atılır" : " · fiş " + m.U_YevmiyeNo), kullanici);
                GrupIciBelgeOrnek.Senkron(m, kullanici);
            }
            return m;
        }
        /// <summary>Takip Paneli onayında atılan fiş numarasını nakit akış satırına yazar (satırda fiş yoksa).</summary>
        public static bool FisBagla(string dbName, int docEntry, string yevmiyeNo, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null;
                if (m == null || string.IsNullOrEmpty(yevmiyeNo) || !string.IsNullOrEmpty(m.U_YevmiyeNo)) return false;
                m.U_YevmiyeNo = yevmiyeNo; Log(dbName, docEntry, "Muhasebe Fişi", $"{yevmiyeNo} · Takip Paneli onayında atıldı", kullanici); return true;
            }
        }
        public static bool Sil(string dbName, int docEntry, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var m = _odemeler.TryGetValue(dbName, out var l) ? l.FirstOrDefault(x => x.DocEntry == docEntry) : null; if (m == null) return false;
                foreach (var id in (m.U_CekDocEntryler ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries)) if (int.TryParse(id, out var n)) CekAkibetOrnek.PlanKaldir(n, kullanici);
                GrupIciBelgeOrnek.Iptal(docEntry, kullanici);
                l.Remove(m); Log(dbName, docEntry, "Silme", $"{m.U_Kurum} · {(m.U_Tutar ?? 0).ToString("N2", Tr)} {m.U_ParaBirimi} silindi", kullanici); return true;
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
                    // belge başka grup şirketinin portföyündense ödeyen şirket o şirkettir (fatura sahibi değişmez → dolaşım planı gerekir)
                    var ilk = CekAkibetOrnek.Bul(mevcut[0]); if (ilk != null && !string.IsNullOrEmpty(ilk.Sirket) && ilk.Sirket != m.U_OdeyenSirket) { m.U_OdeyenSirket = ilk.Sirket; if (string.IsNullOrEmpty(m.U_FaturaSahibi)) m.U_FaturaSahibi = SirketAdi(dbName); }
                    // dövizli fatura TL çekle ödeniyorsa: belge toplamı TL, satır tutarı çekin tahsil (vade) günü TCMB kuruyla döviz karşılığı; kur farkı faturası bu cariye kesilir
                    if (!string.IsNullOrEmpty(m.U_ParaBirimi) && m.U_ParaBirimi != "TRY")
                    {
                        m.U_KurKaynagi = "Çek Tahsil Günü Kuru"; m.U_KurTarihi = m.Belgeler.Min(b => b.Vade); m.U_Kur = KurOrnek.Kur(m.U_ParaBirimi, m.U_KurTarihi, m.U_KurKaynagi);
                        m.U_TutarTL = m.Belgeler.Sum(b => b.Tutar); m.U_Tutar = Math.Round(m.U_TutarTL.Value / m.U_Kur.Value, 2); m.U_OdenenTutarPB = m.U_ParaBirimi;
                        if (string.IsNullOrEmpty(m.U_KurFarkiFaturasi) || m.U_KurFarkiFaturasi == "Belirsiz") m.U_KurFarkiFaturasi = "Kesilecek";
                        if (string.IsNullOrEmpty(m.U_KurNotu)) m.U_KurNotu = $"Dövizli fatura TL çekle ödeniyor: çekin tahsil günü ({m.U_KurTarihi:dd.MM.yyyy}) TCMB kuru baz; fark için cariye kur farkı faturası";
                    }
                    else { m.U_Tutar = m.Belgeler.Sum(b => b.Tutar); m.U_ParaBirimi = "TRY"; m.U_Kur = 1; m.U_TutarTL = m.U_Tutar; m.U_OdenenTutarPB = "TRY"; }
                    if (m.U_PlanlananTarih == null || m.U_PlanlananTarih > m.Belgeler.Min(b => b.Vade)) m.U_PlanlananTarih = m.Belgeler.Min(b => b.Vade);
                    if (m.U_Durum == "Bekliyor") { m.U_Durum = "Planlandı"; m.U_EskiDurum = "Planlandı"; }
                    string not = "Belge(ler): " + string.Join(", ", m.Belgeler.Select(b => b.No)); var parcalar = (m.U_Notlar ?? "").Split(" | ", StringSplitOptions.RemoveEmptyEntries).Where(p => !p.StartsWith("Belge(ler):") && p != "Belge seçilecek").ToList(); parcalar.Insert(0, not); m.U_Notlar = string.Join(" | ", parcalar);
                }
                if (eklenen.Count > 0) Log(dbName, m.DocEntry, "Belge Planlandı", $"{m.U_Kurum} için {string.Join(", ", eklenen)} seçildi → Çek Akıbeti: Planlandı" + (m.U_ParaBirimi != "TRY" ? $" · kur {(m.U_Kur ?? 1).ToString("N4", Tr)} (çek tahsil günü {m.U_KurTarihi:dd.MM.yyyy})" : ""), kullanici);
                GrupIciBelgeOrnek.Senkron(m, kullanici);
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
        /// <summary>
        /// Çek Akıbeti / Takip Paneli'nden "Nakit Akışa Gönder": portföydeki belge için Kasa Nakit Akışı'nda çek / senet ödeme satırı açılır ve belge satıra planlanır
        /// (Çek Akıbeti: Planlandı, planlanan tedarikçi = kurum). Planlanan tarih boşsa belge vadesi; belge tipi boşsa Cari Bakiye.
        /// </summary>
        public static (bool ok, string mesaj, NakitAkisViewModel satir) CekIcinSatirAc(int cekDocEntry, string tedarikci, DateTime? planTarih, string belgeTipi, string belgeNo, string urunHizmet, string kullanici)
        {
            Tohumla(); var k = CekAkibetOrnek.Bul(cekDocEntry); if (k == null) return (false, "Belge bulunamadı.", null);
            if (!k.Secilebilir) return (false, $"{k.No} nakit akışa gönderilemez: işlem türü {k.IslemTuru}" + (k.PlanDocEntry != null ? $" (Kasa Nakit Akışı #{k.PlanDocEntry})" : "") + (k.KullanimDurumu != "Kullanılabilir" ? $" · kullanım {k.KullanimDurumu}" : "") + ".", null);
            if (string.IsNullOrWhiteSpace(tedarikci)) return (false, "Ödenecek tedarikçi (cari) girin.", null);
            belgeTipi = string.IsNullOrWhiteSpace(belgeTipi) ? "Cari Bakiye" : belgeTipi.Trim(); if (!BelgeTipleri.Contains(belgeTipi)) belgeTipi = "Cari Bakiye";
            var gelen = new NakitAkisViewModel { U_Kurum = tedarikci.Trim(), U_UrunHizmet = string.IsNullOrWhiteSpace(urunHizmet) ? $"{k.Belge} ile ödeme · {k.No}" : urunHizmet.Trim(), U_OdemeYontemi = k.Belge, U_BagliBelgeTipi = belgeTipi, U_FaturaSiparisNo = belgeTipi == "Cari Bakiye" ? "" : (belgeNo ?? "").Trim(), U_PlanlananTarih = (planTarih ?? k.Vade).Date, U_SonOdemeTarihi = (planTarih ?? k.Vade).Date, U_Tutar = k.Tutar, U_ParaBirimi = "TRY", U_Kur = 1, U_TutarTL = k.Tutar, U_IslemTipi = k.IslemTipi, U_TalepEden = kullanici ?? "", U_FaturaSahibi = k.Sirket, U_OdeyenSirket = k.Sirket, U_Notlar = $"Çek Akıbeti'nden gönderildi ({k.No} · {k.Musteri} · vade {k.Vade:dd.MM.yyyy})", U_Satinalmaci = "" };
            var m = Ekle(k.DbName, gelen, kullanici);
            var r = BelgePlanla(k.DbName, m.DocEntry, new[] { k.DocEntry }, kullanici);
            if (!r.ok) { Sil(k.DbName, m.DocEntry, kullanici); return (false, r.mesaj, null); }
            return (true, $"{k.No} Kasa Nakit Akışı'na gönderildi: #{m.DocEntry} {m.U_Kurum} · {k.Tutar.ToString("N2", Tr)} ₺ · planlanan {m.U_PlanlananTarih:dd.MM.yyyy} (Planlandı). Çek Akıbeti'nde işlem türü \"Planlandı\" oldu; talimatla ya da \"Tedarikçiye Çık\" ile ciro edilir.", m);
        }
        /// <summary>
        /// Belgeyi tedarikçiye çıkar (ciro) ya da bankaya tahsile verir — Çek Akıbeti ve Takip Paneli'ndeki tek düğme. Kasa Nakit Akışı'nda planlı belge: plan satırı üzerinden ibraz (CekIbrazEt; satır ÖDENDİ, fiş atılır);
        /// planlanmamış belge: çıkış işlenmez, GMY onayına düşer (CekCikisKontrollu). İbraz yeri boşsa planlanan tedarikçi.
        /// </summary>
        public static (bool ok, string mesaj, bool onayaDustu, int? docEntry) CekCikisYap(int cekDocEntry, string tur, string ibrazYeri, string belgeNo, string not, string kullanici)
        {
            Tohumla(); var k = CekAkibetOrnek.Bul(cekDocEntry); if (k == null) return (false, "Belge bulunamadı.", false, null);
            bool bankaya = tur == "Bankaya Tahsile Ver" || tur == "Bankaya İbraz" || tur == "Bankaya Tahsile Verildi";
            if (!k.Portfoyde) return (false, $"{k.No} portföyde değil (işlem türü {k.IslemTuru}" + (k.IbrazYeri != "" ? $" → {k.IbrazYeri}" : "") + ").", false, k.PlanDocEntry);
            string yer = !string.IsNullOrWhiteSpace(ibrazYeri) ? ibrazYeri.Trim() : bankaya ? ButceOrnek.TahsilBankalari[k.DocEntry % 2] : k.PlanlananTedarikci;
            if (k.PlanDocEntry != null)
            {
                var plan = Bul(k.PlanDocEntry.Value);
                if (plan != null)
                {
                    if (!string.IsNullOrEmpty(plan.U_CikisTalebi) && plan.U_OnayDurumu != "Onaylandı") return (false, $"{k.No} için çıkış talebi GMY onayı bekliyor (Kasa Nakit Akışı #{plan.DocEntry}); onaylanmadan çıkış yapılamaz.", true, plan.DocEntry);
                    if (!bankaya && string.IsNullOrEmpty(yer)) yer = plan.U_Kurum;
                    if (!bankaya && !CekAkibetOrnek.AyniMi(yer, plan.U_Kurum)) return (false, $"DİKKAT: {k.No} Kasa Nakit Akışı'nda {plan.U_Kurum} için planlı; {yer} tedarikçisine çıkış plana aykırı olur. Önce planı (#{plan.DocEntry}) düzeltin ya da planlanan tedarikçiye çıkın.", false, plan.DocEntry);
                    var r = CekIbrazEt(plan.U_KaynakFirma, plan.DocEntry, bankaya ? "Bankaya Tahsile Ver" : "Tedarikçiye Ciro", yer, DateTime.Today, null, belgeNo, not, kullanici, true);
                    return (r.ok, r.mesaj, false, plan.DocEntry);
                }
            }
            if (string.IsNullOrEmpty(yer)) return (false, "Tedarikçi (ciro edilecek cari) girin.", false, null);
            var c = CekCikisKontrollu(k.DocEntry, bankaya ? "Bankaya Tahsile Verildi" : "Ciro Edildi", yer, belgeNo, kullanici);
            return (c.ok, c.mesaj, c.onayaDustu, c.docEntry);
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
                if (alan == "U_Durum" && (string)v == "ÖDENDİ" && m.U_Durum != "ÖDENDİ") { if (NakitOnaysiz(m)) return (false, NakitOnayMesaji); if (!string.IsNullOrEmpty(m.U_CikisTalebi) && m.U_OnayDurumu != "Onaylandı") return (false, "Çek çıkış talebi GMY onayı bekliyor; onaylanmadan ödendi yapılamaz."); }
                object eski = p.GetValue(m); p.SetValue(m, v);
                if (alan == "U_OdemeYontemi" && (string)v == "Nakit" && string.IsNullOrEmpty(m.U_OnayDurumu)) m.U_OnayDurumu = "Onay Bekliyor";
                if (alan == "U_BagliBelgeTipi" && (string)v == "Cari Bakiye") { m.U_KayitTipi = "Bakiye"; m.U_FaturaSiparisNo = ""; }
                if (alan == "U_Tutar" || alan == "U_Kur" || alan == "U_ParaBirimi" || alan == "U_KurKaynagi" || alan == "U_KurTarihi" || alan == "U_PlanlananTarih" || alan == "U_OdemeYontemi")
                {
                    if (alan == "U_ParaBirimi") { m.U_KurKaynagi = ""; m.U_KurTarihi = null; }   // para birimi değişince kur kaynağı / günü yeniden kurulur
                    if (alan == "U_PlanlananTarih" && m.U_KurKaynagi != "Çek Tahsil Günü Kuru" && m.U_Durum != "ÖDENDİ") m.U_KurTarihi = m.U_PlanlananTarih;
                    if (alan == "U_Kur" && KurOrnek.TcmbKaynagi(m.U_KurKaynagi)) m.U_KurKaynagi = "Anlaşmalı Kur";   // elle kur yazıldıysa artık TCMB değil
                    KurTamamla(m);
                }
                if (alan == "U_Durum") { if (m.U_Durum == "ÖDENDİ") { m.U_OdenmeTarihi ??= DateTime.Today; if ((m.U_OdenenTutar ?? 0) == 0) m.U_OdenenTutar = m.U_Tutar; } m.U_EskiDurum = m.U_Durum; }
                if (GrupIciBelgeOrnek.Gerekli(m) && string.IsNullOrEmpty(m.U_GrupIciTur)) m.U_GrupIciTur = "Mahsup";
                Log(dbName, docEntry, alan == "U_Durum" ? "Durum" : "Güncelleme", $"{alan}: {(eski is DateTime ed ? ed.ToString("dd.MM.yyyy") : eski)} → {(v is DateTime vd ? vd.ToString("dd.MM.yyyy") : v)}", kullanici);
                string ek = "";
                if (alan == "U_Durum" && (string)eski != "ÖDENDİ" && m.U_Durum == "ÖDENDİ") { var h = AvansOdemesiIsle(m, kullanici); if (!string.IsNullOrEmpty(m.U_AvansNo)) ek = h == null ? $" Avans {m.U_AvansNo} teslim edildi; Bekleyen Ödemeler listesinden düştü." : " Avans olayı: " + h; FisAt(m, "Ödeme (satır içi)", kullanici); if (!string.IsNullOrEmpty(m.U_YevmiyeNo)) ek += $" Muhasebe fişi {m.U_YevmiyeNo}."; }
                var gib = GrupIciBelgeOrnek.Senkron(m, kullanici); if (gib != null && alan == "U_Durum" && gib.Durum == "Muhasebeleşti" && !string.IsNullOrEmpty(gib.FaturaSahibiFisNo)) ek += $" {gib.No}: {gib.FaturaSahibi} fişi {gib.FaturaSahibiFisNo} ({gib.MahsupSekli}).";
                if (alan == "U_ParaBirimi" || alan == "U_KurKaynagi" || alan == "U_KurTarihi") ek += m.U_ParaBirimi == "TRY" ? "" : $" Kur {(m.U_Kur ?? 1).ToString("N4", Tr)} ({m.U_KurKaynagi} · {m.U_KurTarihi:dd.MM.yyyy}).";
                return (true, "Kaydedildi." + ek);
            }
        }
        /// <summary>Grup içi kaydın (Mahsup / Temlik / Yansıtma) güncel durumu — ilgili modülden okuma anında (ekranlar canlı bağlı).</summary>
        public static object GrupIciBilgi(NakitAkisViewModel m)
        {
            if (m.U_OdemeYontemi == "Mahsup" && !string.IsNullOrEmpty(m.U_MahsupFisNo))
            {
                var r = MahsupOrnek.Bul(m.U_MahsupFisNo); if (r == null) return new { tur = "Mahsup", no = m.U_MahsupFisNo, link = "/GrupIci/Mahsup", durum = "Kayıt bulunamadı", bulundu = false };
                return new { tur = "Mahsup", no = r.No, link = "/GrupIci/Mahsup?no=" + Uri.EscapeDataString(r.No), bulundu = true, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.TurAd + " · " + r.Kapsam, karsi = !string.IsNullOrEmpty(r.KarsiSirket) ? r.KarsiSirket : r.AlacakCari, taraflar = $"{r.BorcCari} ↔ {r.AlacakCari}", tutar = r.MahsupTutari, pb = r.ParaBirimi, durum = r.Durum, muhasebe = r.MuhasebeDurumu, kontrol = r.KontrolDurumu ?? "", ek = $"{r.BelgeSayisi} belge · {r.TamKapanan} tam / {r.KismiKapanan} kısmi kapanan · kur farkı {(r.KurFarki).ToString("N2", Tr)}", onay = r.Onaylandi, duzenlenebilir = r.Duzenlenebilir };
            }
            if (m.U_OdemeYontemi == "Temlik" && !string.IsNullOrEmpty(m.U_TemlikNo))
            {
                var r = TemlikOrnek.Bul(m.U_TemlikNo); if (r == null) return new { tur = "Temlik", no = m.U_TemlikNo, link = "/GrupIci/Temlik", durum = "Kayıt bulunamadı", bulundu = false };
                return new { tur = "Temlik", no = r.No, link = "/GrupIci/Temlik?no=" + Uri.EscapeDataString(r.No), bulundu = true, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.Tur, karsi = r.NihaiAlan, taraflar = $"{r.IlkEden} → {r.NihaiAlan}", tutar = r.NihaiTutar > 0 ? r.NihaiTutar : r.BuTemlikTutari, pb = r.ParaBirimi, durum = r.Durum, muhasebe = r.MuhasebeDurumu, kontrol = r.KontrolDurumu ?? "", ek = $"asıl borçlu {r.AsilBorclu} · belge {r.BelgeDurumu} · imza {r.ImzaDurumu} · {r.StepSayisi} adım", imza = r.ImzaDurumu, belgeDurumu = r.BelgeDurumu, onay = r.Onaylandi, duzenlenebilir = r.Duzenlenebilir };
            }
            if (m.U_OdemeYontemi == "Yansıtma" && !string.IsNullOrEmpty(m.U_YansitmaNo))
            {
                var r = YansitmaOrnek.Bul(m.U_YansitmaNo); if (r == null) return new { tur = "Yansıtma", no = m.U_YansitmaNo, link = "/GrupIci/Yansitma", durum = "Kayıt bulunamadı", bulundu = false };
                return new { tur = "Yansıtma", no = r.No, link = "/GrupIci/Yansitma?no=" + Uri.EscapeDataString(r.No), bulundu = true, tarih = r.Tarih.ToString("yyyy-MM-dd"), baslik = r.Tur + " yansıtması", karsi = r.Yansitilan, taraflar = $"{r.Yansitan} → {r.Yansitilan}", tutar = r.GenelToplam, pb = r.ParaBirimi, durum = r.Durum, muhasebe = r.Entegrasyon?.Durum == "TAMAM" || !string.IsNullOrEmpty(r.YansitmaFaturaNo) ? "Fatura " + r.YansitmaFaturaNo : r.Entegrasyon?.Durum ?? "Bekliyor", kontrol = r.KontrolDurumu ?? "", ek = $"kaynak {r.KaynakFaturaNo} · {r.KaynakTedarikci} · yansıtma {(r.ToplamYansitma).ToString("N2", Tr)} + KDV {(r.Kdv).ToString("N2", Tr)} · kâr %{r.AgirlikliKarPct}", kaynakFatura = r.KaynakFaturaNo, onay = r.Onaylandi, duzenlenebilir = r.Durum == "Taslak" };
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
                Log(dbName, docEntry, "Grup İçi Bağlandı", $"{tur} {no} satıra bağlandı · {m.U_Kurum} · {(m.U_Tutar ?? 0).ToString("N2", Tr)} {m.U_ParaBirimi}", kullanici);
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
        /// <summary>Çekmece: yalnız adla dekont kaydı (dosyasız); dosyalı ekleme EkEkle.</summary>
        public static (bool ok, string mesaj, NakitAkisViewModel satir) DekontEkle(string dbName, int docEntry, string ad, string aciklama, string kullanici) => EkEkle(dbName, docEntry, ad, "Dekont", aciklama, null, null, kullanici);
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
