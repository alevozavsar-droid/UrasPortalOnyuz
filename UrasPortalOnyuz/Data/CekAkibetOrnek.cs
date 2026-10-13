using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>
    /// ÇEK AKIBETİ KAYDI — müşteri çek / senet portföyü (Data/ButceOrnek.cs › Cekler + Senetler ile aynı belgeler) + Kasa Nakit Akışı planı + ibraz / ciro (gerçek çıkış) bilgisi.
    /// Üç ekranın ORTAK kaynağı:
    ///   Kasa Nakit Akışı (Rapor79)                 : Çek / Senet sekmesinde ödeme satırına portföyden belge seçilir → belge burada "Planlandı" olur, planlanan tedarikçi = ödeme satırının kurumu.
    ///   Çek Akıbeti Raporu (Rapor2 / CekAkibeti)   : işlem türü (Portföyde, Planlandı, Bankaya Tahsile Verildi, Ciro Edildi …), planlanan tedarikçi, ibraz belgesi / tarihi buradan okunur ve güncellenir.
    ///   Konsolide Finansal Takip Paneli (Rapor123) : çek no, planlanan tedarikçi, ibraz bilgisi ve Kontrol (plan ↔ ibraz aynı mı farklı mı) — plana aykırı çıkan çekler buradan izlenir.
    /// Kontrol: plan yok + ibraz yok → "—"; plan var + ibraz yok → "İbraz Bekliyor"; plan yok + ibraz var → "Plansız İbraz"; plan = ibraz yeri → "Uyumlu"; farklı → "Farklı".
    /// </summary>
    public class CekKaydi
    {
        public int DocEntry { get; set; }
        public string Sirket { get; set; }           // "Uras Kimya" (Data/TeklifOrnek.cs › Sirketler)
        public string DbName { get; set; }           // Kasa Nakit Akışı şirket anahtarı (URASKIMYA …)
        public string DbKey { get; set; }            // DefaultConnection … (OrnekVeri.Sirketler)
        public string No { get; set; }               // ÇK-2610-123456 | SN-2610-123456
        public string Belge { get; set; }            // Çek | Senet
        public string IslemTipi { get; set; } = "X"; // X | R — belgenin işlem tipi (portföy X / R ayrı izlenir; Kasa Nakit Akışı İşlem Tipi ile aynı kodlama)
        public string Musteri { get; set; }          // çekin geldiği müşteri (ciro eden / kimden geldi)
        public string MusteriKod { get; set; }
        public string AsilBorclu { get; set; }       // keşideci
        public string Banka { get; set; }            // çekin bankası
        public string Sube { get; set; }
        public DateTime Vade { get; set; }
        public decimal Tutar { get; set; }
        public string PB { get; set; } = "TRY";
        public DateTime PortfoyGiris { get; set; }
        public string IslemTuru { get; set; }        // Portföyde | Planlandı | Bankaya Tahsile Verildi | Bankadan Tahsil Edildi | Ciro Edildi | Karşılıksız | Teminata Verildi | İade Edildi | İptal Edildi
        public string KullanimDurumu { get; set; }   // Kullanılabilir | Riskli | Teminat | Bloke
        // --- plan (Kasa Nakit Akışı)
        public string PlanlananTedarikci { get; set; } = "";
        public DateTime? PlanlamaTarihi { get; set; }
        public int? PlanDocEntry { get; set; }       // Kasa Nakit Akışı ödeme satırı (NakitAkisOrnek)
        public string PlanSirket { get; set; } = "";
        public string Planlayan { get; set; } = "";
        // --- ibraz / ciro (gerçek çıkış)
        public string IbrazYeri { get; set; } = "";  // ciro edilen tedarikçi ya da tahsile verilen banka
        public string IbrazTuru { get; set; } = "";  // Ciro | Bankaya İbraz | Teminat | İade
        public DateTime? IbrazTarihi { get; set; }
        public string IbrazBelgeNo { get; set; } = "";
        public string TahsilatBelgeNo { get; set; } = "";
        // --- grup içi dolaşım senaryosu (Kasa Nakit Akışı › Çek / Senet › Dolaşım Planı): belge grup şirketleri arasında ciro edilerek grup içi bakiyeleri kapatır, sonunda tedarikçiye ciro ya da bankaya ibraz
        public string KaynakSirket { get; set; } = "";       // belgenin ilk portföy şirketi (dolaşımda Sirket = güncel sahip)
        public List<CekDolasimAdim> Dolasim { get; set; } = new List<CekDolasimAdim>();
        public int? DolasimDocEntry { get; set; }            // planı yapan Kasa Nakit Akışı satırı
        public string DolasimOzeti => Dolasim.Count == 0 ? "" : string.Join(" → ", new[] { Dolasim[0].Eden }.Concat(Dolasim.Select(a => a.Alan)));
        public int GerceklesenAdim => Dolasim.Count(a => a.Durum == "Gerçekleşti");
        public CekDolasimAdim SonrakiAdim => Dolasim.FirstOrDefault(a => a.Durum == "Planlandı");

        public bool Portfoyde => IslemTuru == "Portföyde" || IslemTuru == "Planlandı";
        public bool Secilebilir => IslemTuru == "Portföyde" && KullanimDurumu == "Kullanılabilir";
        public string KonumDurumu => IslemTuru == "Ciro Edildi" ? "Tedarikçide" : IslemTuru == "Bankaya Tahsile Verildi" || IslemTuru == "Bankadan Tahsil Edildi" || IslemTuru == "Karşılıksız" ? "Bankada" : IslemTuru == "Teminata Verildi" ? "Teminatta" : IslemTuru == "İade Edildi" || IslemTuru == "İptal Edildi" ? "Çıktı" : "Portföyde";
        public string VadeDurumu { get { int g = (Vade - DateTime.Today).Days; return g < 0 ? "Vadesi Geçti" : g == 0 ? "Bugün" : g <= 7 ? "Bu Hafta" : g <= 30 ? "Bu Ay" : "İleri Vadeli"; } }
        /// <summary>Plan ↔ ibraz karşılaştırması (finans kontrolü).</summary>
        public string Kontrol
        {
            get
            {
                bool plan = !string.IsNullOrEmpty(PlanlananTedarikci), ibraz = !string.IsNullOrEmpty(IbrazYeri);
                if (!plan && !ibraz) return "—";
                if (plan && !ibraz) return "İbraz Bekliyor";
                if (!plan) return "Plansız İbraz";
                return CekAkibetOrnek.AyniMi(PlanlananTedarikci, IbrazYeri) ? "Uyumlu" : "Farklı";
            }
        }
    }

    /// <summary>Çek akıbeti günlüğü (kim, ne zaman, hangi belge, ne yaptı).</summary>
    public class CekAkibetGunluk { public DateTime Tarih { get; set; } public string Kullanici { get; set; } public string Sirket { get; set; } public string No { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }
    /// <summary>Dolaşım planı adımı: Eden → Alan; Tur = Grup İçi Ciro | Tedarikçiye Ciro | Bankaya İbraz.</summary>
    public class CekDolasimAdim { public int Sira { get; set; } public string Eden { get; set; } public string Alan { get; set; } public string Tur { get; set; } public DateTime PlanTarih { get; set; } public string Durum { get; set; } = "Planlandı"; public DateTime? GercekTarih { get; set; } public string BelgeNo { get; set; } = ""; public decimal KapananBakiye { get; set; } public decimal YeniBorc { get; set; } public string Not { get; set; } = ""; }
    /// <summary>Gerçekleşen çek hareketi (Konsolide Finansal Takip Paneli › Çek): Eden → Alan ciro / ibraz.</summary>
    public class CekHareket { public DateTime Tarih { get; set; } public int CekDocEntry { get; set; } public string No { get; set; } public string Belge { get; set; } public string Eden { get; set; } public string Alan { get; set; } public string Tur { get; set; } public string BelgeNo { get; set; } public decimal Tutar { get; set; } public int AdimSira { get; set; } public int AdimToplam { get; set; } public bool PlanaUygun { get; set; } = true; public string Kullanici { get; set; } = ""; }
    public class DolasimAdimSonuc { public int Sira { get; set; } public string Eden { get; set; } public string Alan { get; set; } public string Tur { get; set; } public decimal Bakiye { get; set; } public decimal Kapanan { get; set; } public decimal YeniBorc { get; set; } public string Sonuc { get; set; } public string Not { get; set; } public string Durum { get; set; } = "Öneri"; }
    public class DolasimSonuc { public List<DolasimAdimSonuc> Adimlar { get; set; } = new List<DolasimAdimSonuc>(); public bool Uygun { get; set; } public string Derece { get; set; } = "Uygun"; public decimal ToplamKapanan { get; set; } public decimal ToplamYeniBorc { get; set; } public List<string> Uyarilar { get; set; } = new List<string>(); public string SonSahip { get; set; } public string Ozet { get; set; } public decimal Tutar { get; set; } }

    public static class CekAkibetOrnek
    {
        public static readonly object Kilit = new object();
        static readonly System.Globalization.CultureInfo Tr = System.Globalization.CultureInfo.GetCultureInfo("tr-TR");
        /// <summary>Şirket adı ↔ Kasa Nakit Akışı DbName ↔ bağlantı anahtarı.</summary>
        public static readonly (string Ad, string DbName, string DbKey)[] Sirketler =
        {
            ("Uras Kimya", "URASKIMYA", "DefaultConnection"), ("Selvi", "SELVI", "DefaultConnection5"), ("Avrupa Paper", "AVRUPA_PAPER", "DefaultConnection2"),
            ("Alv Kimya", "ALV_KIMYA", "DefaultConnection3"), ("Daf Kimya", "DAF_KIMYA", "DefaultConnection4"), ("URS Makine", "URSMAKINE", "DefaultConnection1"), ("Uras Holding", "URAS_HOLDING", "DefaultConnection10")
        };
        public static string[] GrupSirketleri => Sirketler.Select(s => s.Ad).ToArray();
        /// <summary>Grup içi cari bakiyeler (A|B → A'nın B'ye borcu, TL; Data/TalepModulleri.cs ile aynı değerler + dolaşım örneği için zincir). Dolaşım adımı gerçekleşince güncellenir.</summary>
        public static readonly Dictionary<string, decimal> GrupBakiyeleri = new Dictionary<string, decimal>
        {
            // örnek senaryo zinciri (Selvi → Uras Kimya → URS Makine → Uras Holding → Alv Kimya) boyunca borçlar çek tutarını (≈1,75 Mn) karşılar; tohumda ilk iki adım gerçekleştiğinde Selvi ve Uras Kimya borçları o kadar azalır
            ["Uras Kimya|Uras Holding"] = 1_850_000m, ["Uras Holding|Uras Kimya"] = 420_000m, ["Selvi|Uras Kimya"] = 2_450_000m, ["Uras Kimya|Selvi"] = 310_000m,
            ["Avrupa Paper|Uras Holding"] = 275_000m, ["Uras Holding|Avrupa Paper"] = 640_000m, ["Alv Kimya|Uras Kimya"] = 128_000m, ["Uras Kimya|Alv Kimya"] = 512_000m,
            ["Daf Kimya|Uras Holding"] = 89_000m, ["Uras Holding|Daf Kimya"] = 233_000m,
            ["Uras Kimya|URS Makine"] = 2_180_000m, ["URS Makine|Uras Holding"] = 1_960_000m, ["Uras Holding|Alv Kimya"] = 1_840_000m, ["Selvi|Alv Kimya"] = 35_000m, ["Daf Kimya|Uras Kimya"] = 210_000m, ["URS Makine|Daf Kimya"] = 95_000m, ["Selvi|Daf Kimya"] = 640_000m, ["Daf Kimya|Alv Kimya"] = 420_000m
        };
        public static decimal GrupBakiye(string a, string b) { lock (Kilit) return GrupBakiyeleri.TryGetValue(a + "|" + b, out var v) ? v : 0; }
        /// <summary>Grup içi bakiye değişikliği (borçlu → alacaklı): + borç artar, − borç kapanır (sıfırın altına inmez; fazlası ters yönde yeni bakiye olur). Grup içi mahsup / temlik belgeleri (Data/GrupIciBelgeOrnek.cs) kullanır.</summary>
        public static void GrupBakiyeEkle(string borclu, string alacakli, decimal tutar)
        {
            if (string.IsNullOrEmpty(borclu) || string.IsNullOrEmpty(alacakli) || borclu == alacakli || tutar == 0) return;
            lock (Kilit)
            {
                string key = borclu + "|" + alacakli, ters = alacakli + "|" + borclu; decimal mevcut = GrupBakiyeleri.TryGetValue(key, out var v) ? v : 0;
                if (tutar < 0 && -tutar > mevcut) { GrupBakiyeleri[key] = 0; GrupBakiyeleri[ters] = (GrupBakiyeleri.TryGetValue(ters, out var t) ? t : 0) + (-tutar - mevcut); }
                else GrupBakiyeleri[key] = mevcut + tutar;
            }
        }
        public static string DbName(string sirket) => Sirketler.FirstOrDefault(s => s.Ad == sirket).DbName ?? sirket;
        public static string SirketAdi(string dbNameOrKey) { var s = Sirketler.FirstOrDefault(x => x.DbName == dbNameOrKey || x.DbKey == dbNameOrKey || x.Ad == dbNameOrKey); return s.Ad ?? dbNameOrKey; }
        /// <summary>Çek Akıbeti Raporu işlem türleri (kod, açıklama) — ana projedeki sıra.</summary>
        public static readonly (string Kod, string Ad)[] IslemTurleri =
        {
            ("1", "Portföyde"), ("2", "Planlandı"), ("3", "Erteleme Yapıldı - Portföyde"), ("4", "Erteleme Yapıldı - Bankada"), ("5", "Bankaya Tahsile Verildi"), ("6", "Bankadan Tahsil Edildi"),
            ("7", "Elden Tahsil Edildi"), ("8", "Ciro Edildi"), ("9", "Teminata Verildi"), ("10", "Karşılıksız"), ("11", "Protesto Edildi"), ("12", "Konkordato - Portföyde"),
            ("13", "Konkordato - Bankada"), ("14", "Konkordato - Tedarikçide"), ("15", "İade Edildi"), ("16", "İptal Edildi")
        };
        public static string IslemKodu(string ad) => IslemTurleri.FirstOrDefault(t => t.Ad == ad).Kod ?? "1";
        /// <summary>Belgenin işlem tipi (X / R): belge numarasının rakamlarından deterministik, yaklaşık %78 X.</summary>
        public static string IslemTipiHesapla(string no) { var d = new string((no ?? "").Where(char.IsDigit).ToArray()); if (d.Length == 0) return "X"; long n = long.Parse(d.Length > 9 ? d.Substring(d.Length - 9) : d); return n % 100 < 78 ? "X" : "R"; }
        /// <summary>Belgenin şirketten çıktığı işlem türleri: Kasa Nakit Akışı'nda planlanmamış belge için bu türlere geçiş GMY onayına düşer (NakitAkisOrnek.CekCikisKontrollu).</summary>
        public static bool CikisTuruMu(string ad) => ad == "Ciro Edildi" || ad == "Bankaya Tahsile Verildi" || ad == "Teminata Verildi" || ad == "Konkordato - Tedarikçide" || ad == "Konkordato - Bankada" || ad == "Erteleme Yapıldı - Bankada";
        public static string IslemAdi(string kod) => IslemTurleri.FirstOrDefault(t => t.Kod == kod).Ad ?? kod;
        public static readonly string[] KullanimDurumlari = { "Kullanılabilir", "Riskli", "Teminat", "Bloke" };
        static readonly string[] Subeler = { "Merkez", "Gebze", "Çorlu", "İkitelli", "Bursa OSB", "Kadıköy", "Bornova", "Çerkezköy", "Kartal", "Ümraniye" };
        static readonly string[] UcuncuTaraf = { "Marmara Tekstil", "Ege Boya", "Anadolu Ambalaj", "Kuzey Plastik", "Trakya Kimyevi", "Akdeniz Dağıtım", "Doğu Lojistik", "Yıldız Tekstil" };

        private static readonly List<CekKaydi> _kayitlar = new List<CekKaydi>();
        private static readonly List<CekAkibetGunluk> _gunluk = new List<CekAkibetGunluk>();
        private static readonly List<CekHareket> _hareketler = new List<CekHareket>();
        public static List<CekHareket> Hareketler { get { Tohumla(); lock (Kilit) return _hareketler.ToList(); } }
        private static bool _tohumlandi;
        private static int Seed(string s) => s.Aggregate(23, (a, ch) => unchecked(a * 31 + ch));

        public static List<CekKaydi> Kayitlar { get { Tohumla(); lock (Kilit) return _kayitlar.ToList(); } }
        public static List<CekAkibetGunluk> Gunluk { get { Tohumla(); lock (Kilit) return _gunluk.OrderByDescending(g => g.Tarih).ToList(); } }
        public static CekKaydi Bul(int docEntry) { Tohumla(); lock (Kilit) return _kayitlar.FirstOrDefault(k => k.DocEntry == docEntry); }
        public static CekKaydi Bul(string sirket, string no) { Tohumla(); lock (Kilit) return _kayitlar.FirstOrDefault(k => k.No == no && (string.IsNullOrEmpty(sirket) || k.Sirket == sirket || k.DbName == sirket || k.DbKey == sirket)); }
        /// <summary>Kasa Nakit Akışı çek / senet seçimi için: şirketin portföyündeki, planlanmamış, kullanılabilir belgeler (vade sırasıyla). hepsi = planlı ve kullanılamaz olanlar da (portföy görünümü).</summary>
        public static List<CekKaydi> Secilebilirler(string sirket, string belge, bool hepsi = false) { Tohumla(); lock (Kilit) return _kayitlar.Where(k => (hepsi ? k.Portfoyde : k.Secilebilir) && k.Belge == belge && (string.IsNullOrEmpty(sirket) || k.Sirket == sirket || k.DbName == sirket || k.DbKey == sirket)).OrderBy(k => k.Vade).ThenBy(k => k.No).ToList(); }
        /// <summary>Ad karşılaştırması: büyük / küçük harf, noktalama ve "A.Ş. / Ltd." ekleri yok sayılır.</summary>
        public static bool AyniMi(string a, string b)
        {
            string N(string s) => new string((s ?? "").ToUpperInvariant().Replace("İ", "I").Replace("Ş", "S").Replace("Ğ", "G").Replace("Ü", "U").Replace("Ö", "O").Replace("Ç", "C").Where(char.IsLetterOrDigit).ToArray()).Replace("AS", "").Replace("LTD", "").Replace("STI", "");
            return N(a) == N(b);
        }

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var bugun = DateTime.Today; int docEntry = 5000;
                var tedarikciler = CekOrnek.TedarikciAdlari;
                var kartlar = CariKartOrnek.Kartlar;
                foreach (var c in ButceOrnek.Cekler.Concat(ButceOrnek.Senetler).OrderBy(c => c.Vade).ThenBy(c => c.No))
                {
                    var rnd = new Random(Seed("akibet|" + c.Sirket + "|" + c.No));
                    var s = Sirketler.FirstOrDefault(x => x.Ad == c.Sirket);
                    var k = new CekKaydi
                    {
                        DocEntry = docEntry++, Sirket = c.Sirket, KaynakSirket = c.Sirket, DbName = s.DbName ?? c.Sirket, DbKey = s.DbKey ?? "DefaultConnection", No = c.No, Belge = c.Belge, IslemTipi = IslemTipiHesapla(c.No), Musteri = c.Musteri, MusteriKod = c.MusteriKod,
                        AsilBorclu = rnd.NextDouble() < 0.8 ? c.Musteri : UcuncuTaraf[rnd.Next(UcuncuTaraf.Length)], Banka = c.Banka, Sube = Subeler[rnd.Next(Subeler.Length)],
                        Vade = c.Vade, Tutar = c.Tutar, PB = "TRY", PortfoyGiris = c.Vade.AddDays(-rnd.Next(20, 90)),
                        KullanimDurumu = c.Durum == "Riskli" ? "Riskli" : rnd.Next(40) == 0 ? "Teminat" : "Kullanılabilir"
                    };
                    switch (c.Durum)
                    {
                        case "Tahsilde": k.IslemTuru = "Bankaya Tahsile Verildi"; break;
                        case "Tahsil Edildi": k.IslemTuru = "Bankadan Tahsil Edildi"; break;
                        case "Ciro Edildi": k.IslemTuru = "Ciro Edildi"; break;
                        case "Karşılıksız": k.IslemTuru = "Karşılıksız"; break;
                        default: k.IslemTuru = "Portföyde"; break;   // Portföyde, Riskli
                    }
                    if (k.KullanimDurumu == "Teminat" && k.IslemTuru == "Portföyde") k.IslemTuru = "Teminata Verildi";
                    // gerçek çıkış: ciro → tedarikçi, tahsil → banka
                    if (k.IslemTuru == "Ciro Edildi")
                    {
                        k.IbrazYeri = tedarikciler[rnd.Next(tedarikciler.Length)]; k.IbrazTuru = "Ciro";
                        var t = k.Vade.AddDays(-rnd.Next(5, 45)); if (t < k.PortfoyGiris) t = k.PortfoyGiris.AddDays(1); k.IbrazTarihi = t;
                        k.IbrazBelgeNo = $"CRO-{t:yyMM}-{rnd.Next(1000, 9999)}";
                    }
                    else if (c.TahsileGonderildi)
                    {
                        k.IbrazYeri = c.TahsilBankasi; k.IbrazTuru = "Bankaya İbraz";
                        var t = k.Vade.AddDays(-rnd.Next(2, 12)); if (t < k.PortfoyGiris) t = k.PortfoyGiris.AddDays(1); k.IbrazTarihi = t;
                        k.IbrazBelgeNo = $"İBZ-{t:yyMM}-{rnd.Next(1000, 9999)}";
                        if (k.IslemTuru == "Bankadan Tahsil Edildi") k.TahsilatBelgeNo = $"{k.Vade:yyMM}{rnd.Next(1000, 9999)}";   // sayısal (Çek Akıbeti ekranı parseInt ile gönderir)
                    }
                    else if (k.IslemTuru == "Teminata Verildi") { k.IbrazYeri = "Teminat · " + (rnd.NextDouble() < 0.5 ? "Ziraat Bankası" : "Halk Bankası"); k.IbrazTuru = "Teminat"; k.IbrazTarihi = k.PortfoyGiris.AddDays(rnd.Next(1, 10)); k.IbrazBelgeNo = $"TMN-{k.PortfoyGiris:yyMM}-{rnd.Next(1000, 9999)}"; }
                    // örnek plan senaryoları (yalnız içinde bulunulan yıl):
                    //   portföyde, vadesi yakın belgelerin bir kısmı Kasa Nakit Akışı'nda planlanmış (plan satırı NakitAkisOrnek bağlar);
                    //   ciroda / bankaya verilen yakın tarihli belgelerin bir kısmında plan vardı: çoğu uyumlu, bir kısmı FARKLI yere çıkmış (finans kontrolü örneği)
                    if (k.Vade.Year == bugun.Year)
                    {
                        if (k.IslemTuru == "Portföyde" && k.KullanimDurumu == "Kullanılabilir" && k.Vade >= bugun.AddDays(-5) && k.Vade <= bugun.AddDays(75) && rnd.NextDouble() < 0.35)
                        { k.IslemTuru = "Planlandı"; k.PlanlananTedarikci = tedarikciler[rnd.Next(tedarikciler.Length)]; k.PlanlamaTarihi = bugun.AddDays(-rnd.Next(1, 20)); k.PlanSirket = k.Sirket; k.Planlayan = "Kerem Aksoy"; }
                        else if (k.IslemTuru == "Ciro Edildi" && k.IbrazTarihi >= bugun.AddDays(-60) && rnd.NextDouble() < 0.6)
                        { k.PlanlananTedarikci = rnd.NextDouble() < 0.75 ? k.IbrazYeri : tedarikciler[rnd.Next(tedarikciler.Length)]; k.PlanlamaTarihi = k.IbrazTarihi.Value.AddDays(-rnd.Next(2, 15)); k.PlanSirket = k.Sirket; k.Planlayan = "Kerem Aksoy"; }
                        else if (k.IslemTuru == "Bankaya Tahsile Verildi" && k.IbrazTarihi >= bugun.AddDays(-45) && rnd.NextDouble() < 0.3)
                        { k.PlanlananTedarikci = tedarikciler[rnd.Next(tedarikciler.Length)]; k.PlanlamaTarihi = k.IbrazTarihi.Value.AddDays(-rnd.Next(2, 15)); k.PlanSirket = k.Sirket; k.Planlayan = "Pınar Ak"; }
                    }
                    _kayitlar.Add(k);
                }
                foreach (var k in _kayitlar.Where(x => x.PlanlamaTarihi != null).OrderBy(x => x.PlanlamaTarihi))
                    _gunluk.Add(new CekAkibetGunluk { Tarih = k.PlanlamaTarihi.Value.AddHours(10), Kullanici = k.Planlayan, Sirket = k.Sirket, No = k.No, Islem = "Planlandı", Aciklama = $"Kasa Nakit Akışı: {k.PlanlananTedarikci} ödemesi için planlandı" });
                foreach (var k in _kayitlar.Where(x => x.IbrazTarihi != null && x.IbrazTarihi >= bugun.AddDays(-90)).OrderBy(x => x.IbrazTarihi))
                    _gunluk.Add(new CekAkibetGunluk { Tarih = k.IbrazTarihi.Value.AddHours(14), Kullanici = "Nazlı Erdem", Sirket = k.Sirket, No = k.No, Islem = k.IslemTuru, Aciklama = $"{k.IbrazTuru}: {k.IbrazYeri} · belge {k.IbrazBelgeNo}" + (k.Kontrol == "Farklı" ? $" · DİKKAT: plan {k.PlanlananTedarikci}" : "") });
            }
        }

        public static List<CekKaydi> BulHepsi(IEnumerable<int> docEntryler) { Tohumla(); var set = new HashSet<int>(docEntryler ?? Enumerable.Empty<int>()); lock (Kilit) return _kayitlar.Where(k => set.Contains(k.DocEntry)).ToList(); }
        /// <summary>Tohumlanmış plan senaryosunu Kasa Nakit Akışı satırına bağlar (Data/NakitAkisOrnek.cs tohumlarken).</summary>
        public static void PlanKaynakBagla(int cekDocEntry, int planDocEntry) { Tohumla(); lock (Kilit) { var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k != null) k.PlanDocEntry = planDocEntry; } }

        // ---------- işlemler (bellek içi; uygulama yeniden başlayınca sıfırlanır) ----------
        /// <summary>Kasa Nakit Akışı'ndan belge seçimi: Portföyde → Planlandı; planlanan tedarikçi ödeme satırının kurumu.</summary>
        public static (bool ok, string mesaj) Planla(int cekDocEntry, string tedarikci, int? planDocEntry, string planSirket, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k == null) return (false, "Belge bulunamadı.");
                if (k.IslemTuru != "Portföyde" && !(k.IslemTuru == "Planlandı" && k.PlanDocEntry == planDocEntry)) return (false, $"{k.No} seçilemez: işlem türü {k.IslemTuru}" + (k.PlanlananTedarikci != "" ? $" ({k.PlanlananTedarikci} için planlı)" : "") + ".");
                if (k.KullanimDurumu != "Kullanılabilir") return (false, $"{k.No} kullanım durumu {k.KullanimDurumu}; planlanamaz.");
                k.IslemTuru = "Planlandı"; k.PlanlananTedarikci = tedarikci ?? ""; k.PlanDocEntry = planDocEntry; k.PlanSirket = planSirket ?? k.Sirket; k.PlanlamaTarihi = DateTime.Now; k.Planlayan = kullanici ?? "";
                _gunluk.Add(new CekAkibetGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = k.Sirket, No = k.No, Islem = "Planlandı", Aciklama = $"Kasa Nakit Akışı: {tedarikci} ödemesi için planlandı (kayıt {planDocEntry})" });
                return (true, $"{k.No} planlandı → {tedarikci}. Çek Akıbeti Raporu'nda işlem türü \"Planlandı\" oldu.");
            }
        }
        /// <summary>Plan kaldırma: Planlandı → Portföyde.</summary>
        public static (bool ok, string mesaj) PlanKaldir(int cekDocEntry, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k == null) return (false, "Belge bulunamadı.");
                if (k.IslemTuru != "Planlandı") return (false, $"{k.No} planlı değil (işlem türü {k.IslemTuru}).");
                _gunluk.Add(new CekAkibetGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = k.Sirket, No = k.No, Islem = "Plan Kaldırıldı", Aciklama = $"{k.PlanlananTedarikci} planı kaldırıldı; belge portföye döndü" });
                k.IslemTuru = "Portföyde"; k.PlanlananTedarikci = ""; k.PlanDocEntry = null; k.PlanSirket = ""; k.PlanlamaTarihi = null; k.Planlayan = "";
                return (true, $"{k.No} planı kaldırıldı; portföye döndü.");
            }
        }
        /// <summary>Çek Akıbeti Raporu: işlem türü değişikliği (kod ya da ad). Ciro / tahsil türlerinde ibraz bilgisi verilmemişse plan yeri ya da banka yazılır.</summary>
        public static (bool ok, string mesaj) IslemTuruDegistir(int cekDocEntry, string kodVeyaAd, string ibrazYeri, string ibrazBelgeNo, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k == null) return (false, "Belge bulunamadı.");
                string ad = IslemTurleri.Any(t => t.Kod == kodVeyaAd) ? IslemAdi(kodVeyaAd) : kodVeyaAd; if (string.IsNullOrEmpty(ad) || !IslemTurleri.Any(t => t.Ad == ad)) return (false, "Geçersiz işlem türü.");
                string eski = k.IslemTuru; k.IslemTuru = ad;
                if (ad == "Ciro Edildi" || ad == "Konkordato - Tedarikçide") { k.IbrazTuru = "Ciro"; k.IbrazYeri = !string.IsNullOrEmpty(ibrazYeri) ? ibrazYeri : k.IbrazYeri != "" ? k.IbrazYeri : k.PlanlananTedarikci; k.IbrazTarihi ??= DateTime.Today; k.IbrazBelgeNo = !string.IsNullOrEmpty(ibrazBelgeNo) ? ibrazBelgeNo : k.IbrazBelgeNo != "" ? k.IbrazBelgeNo : $"CRO-{DateTime.Today:yyMM}-{k.DocEntry % 9000 + 1000}"; }
                else if (ad == "Bankaya Tahsile Verildi" || ad == "Bankadan Tahsil Edildi" || ad == "Karşılıksız" || ad == "Erteleme Yapıldı - Bankada" || ad == "Konkordato - Bankada") { k.IbrazTuru = "Bankaya İbraz"; k.IbrazYeri = !string.IsNullOrEmpty(ibrazYeri) ? ibrazYeri : k.IbrazYeri != "" ? k.IbrazYeri : ButceOrnek.TahsilBankalari[k.DocEntry % 2]; k.IbrazTarihi ??= DateTime.Today; k.IbrazBelgeNo = !string.IsNullOrEmpty(ibrazBelgeNo) ? ibrazBelgeNo : k.IbrazBelgeNo != "" ? k.IbrazBelgeNo : $"İBZ-{DateTime.Today:yyMM}-{k.DocEntry % 9000 + 1000}"; }
                else if (ad == "Teminata Verildi") { k.IbrazTuru = "Teminat"; k.IbrazYeri = !string.IsNullOrEmpty(ibrazYeri) ? ibrazYeri : "Teminat"; k.IbrazTarihi ??= DateTime.Today; }
                else if (ad == "Portföyde" || ad == "Erteleme Yapıldı - Portföyde" || ad == "Konkordato - Portföyde") { k.IbrazYeri = ""; k.IbrazTuru = ""; k.IbrazTarihi = null; k.IbrazBelgeNo = ""; if (ad == "Portföyde") { k.PlanlananTedarikci = ""; k.PlanDocEntry = null; k.PlanlamaTarihi = null; } }
                _gunluk.Add(new CekAkibetGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = k.Sirket, No = k.No, Islem = ad, Aciklama = $"İşlem türü {eski} → {ad}" + (k.IbrazYeri != "" ? $" · {k.IbrazTuru}: {k.IbrazYeri}" : "") + (k.Kontrol == "Farklı" ? $" · DİKKAT: plan {k.PlanlananTedarikci}" : "") });
                return (true, $"{k.No}: {eski} → {ad}" + (k.Kontrol == "Farklı" ? $" — DİKKAT: Kasa Nakit Akışı planı {k.PlanlananTedarikci}, çıkış {k.IbrazYeri}." : "."));
            }
        }
        /// <summary>Çek Akıbeti Raporu: planlanan tedarikçi elle değişikliği.</summary>
        public static (bool ok, string mesaj) PlanlananTedarikciDegistir(int cekDocEntry, string tedarikci, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k == null) return (false, "Belge bulunamadı.");
                string eski = k.PlanlananTedarikci; k.PlanlananTedarikci = tedarikci ?? ""; if (k.PlanlananTedarikci != "" && k.IslemTuru == "Portföyde") k.IslemTuru = "Planlandı"; if (k.PlanlananTedarikci == "" && k.IslemTuru == "Planlandı") k.IslemTuru = "Portföyde";
                if (k.PlanlananTedarikci != "") { k.PlanlamaTarihi ??= DateTime.Now; if (string.IsNullOrEmpty(k.Planlayan)) k.Planlayan = kullanici ?? ""; }
                _gunluk.Add(new CekAkibetGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = k.Sirket, No = k.No, Islem = "Planlanan Tedarikçi", Aciklama = $"{(eski == "" ? "—" : eski)} → {(k.PlanlananTedarikci == "" ? "—" : k.PlanlananTedarikci)}" });
                return (true, "Planlanan tedarikçi güncellendi.");
            }
        }
        // ---------- grup içi dolaşım senaryosu ----------
        /// <summary>Uygunluk simülasyonu: belge zincir boyunca ciro edilirken her adımda eden şirketin alan şirkete borcu kapanır; borç yoksa / yetmezse yeni grup içi bakiye oluşur (bakiye şişer → uyarı).</summary>
        public static DolasimSonuc DolasimSimule(int cekDocEntry, List<string> zincir, string sonAdim, string tedarikci, string faturaSahibi)
        {
            var k = Bul(cekDocEntry); var s = new DolasimSonuc(); if (k == null) { s.Uyarilar.Add("Belge bulunamadı."); return s; }
            zincir = (zincir ?? new List<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).ToList(); s.Tutar = k.Tutar; decimal t = k.Tutar; string sahip = k.Sirket; int sira = 0; bool uygun = true;
            foreach (var alan in zincir)
            {
                if (alan == sahip) { s.Uyarilar.Add($"{alan}: belge zaten bu şirkette; adım atlanmalı."); continue; }
                decimal bakiye = GrupBakiye(sahip, alan), kapanan = Math.Min(t, bakiye), yeni = t - kapanan;
                var a = new DolasimAdimSonuc { Sira = ++sira, Eden = sahip, Alan = alan, Tur = "Grup İçi Ciro", Bakiye = bakiye, Kapanan = kapanan, YeniBorc = yeni, Sonuc = bakiye <= 0 ? "Uyarı" : yeni > 0 ? "Kısmi" : "Uygun" };
                a.Not = bakiye <= 0 ? $"{sahip}'nin {alan}'ya borcu yok: ciro {alan}'nın {sahip}'ya {(yeni).ToString("N0", Tr)} TL yeni borcu olur (bakiye şişer)" : yeni > 0 ? $"{sahip}'nin {alan}'ya borcu {(bakiye).ToString("N0", Tr)} TL kapanır; {(yeni).ToString("N0", Tr)} TL yeni bakiye {alan} → {sahip}" : $"{sahip}'nin {alan}'ya {(bakiye).ToString("N0", Tr)} TL borcundan {(kapanan).ToString("N0", Tr)} TL kapanır; kalan borç {(bakiye - kapanan).ToString("N0", Tr)} TL";
                if (a.Sonuc == "Uyarı") uygun = false; s.Adimlar.Add(a); s.ToplamKapanan += kapanan; s.ToplamYeniBorc += yeni; sahip = alan;
            }
            if (sonAdim == "Bankaya İbraz") s.Adimlar.Add(new DolasimAdimSonuc { Sira = ++sira, Eden = sahip, Alan = ButceOrnek.TahsilBankalari[k.DocEntry % 2] + " (Tahsil)", Tur = "Bankaya İbraz", Sonuc = "Uygun", Not = $"{sahip} belgeyi tahsil için bankaya verir; nakit {sahip} hesabına girer" });
            else { string ted = string.IsNullOrWhiteSpace(tedarikci) ? k.PlanlananTedarikci : tedarikci; s.Adimlar.Add(new DolasimAdimSonuc { Sira = ++sira, Eden = sahip, Alan = string.IsNullOrEmpty(ted) ? "Tedarikçi" : ted, Tur = "Tedarikçiye Ciro", Sonuc = string.IsNullOrEmpty(faturaSahibi) || faturaSahibi == sahip ? "Uygun" : "Uyarı", Not = string.IsNullOrEmpty(faturaSahibi) || faturaSahibi == sahip ? $"{sahip} faturasını {ted} tedarikçisine çekle öder" : $"Faturanın sahibi {faturaSahibi}; belge {sahip} şirketinde kalıyor — zincir {faturaSahibi} ile bitmeli" }); if (!string.IsNullOrEmpty(faturaSahibi) && faturaSahibi != sahip) { uygun = false; s.Uyarilar.Add($"Zincirin son şirketi {sahip}, fatura sahibi {faturaSahibi}: belge fatura sahibine ulaşmıyor."); } }
            s.Uygun = uygun && !s.Adimlar.Any(a => a.Sonuc == "Uyarı"); s.SonSahip = sahip;
            // derece: Uygun = her adımda borç tamamen kapanıyor; Kısmi = borç var ama belge tutarından küçük (fark yeni bakiye); Uyarı = borç ilişkisi olmayan adım (bakiye şişer)
            s.Derece = !s.Uygun ? "Uyarı" : s.ToplamYeniBorc > 0 ? "Kısmi" : "Uygun";
            if (s.ToplamYeniBorc > 0) s.Uyarilar.Add($"Toplam {(s.ToplamYeniBorc).ToString("N0", Tr)} TL yeni grup içi bakiye oluşur" + (s.Derece == "Kısmi" ? " (belge tutarı mevcut borçlardan büyük; daha küçük belge ya da farklı zincir değerlendirin)." : "."));
            if (zincir.Count == 0 && sonAdim != "Bankaya İbraz" && !string.IsNullOrEmpty(faturaSahibi) && faturaSahibi != k.Sirket) { s.Uyarilar.Add($"Belge {k.Sirket} portföyünde, fatura sahibi {faturaSahibi}: zincir kurulmadan doğrudan tedarikçiye ciro edilirse {k.Sirket}'nin {faturaSahibi} adına ödemesi grup içi bakiyeye yazılır."); if (s.Derece == "Uygun") s.Derece = "Kısmi"; }
            s.Ozet = $"{k.No} · {(k.Tutar).ToString("N0", Tr)} TL · {string.Join(" → ", new[] { k.Sirket }.Concat(zincir))}{(sonAdim == "Bankaya İbraz" ? " → banka" : " → " + (string.IsNullOrWhiteSpace(tedarikci) ? k.PlanlananTedarikci : tedarikci))} · kapanan grup içi borç {(s.ToplamKapanan).ToString("N0", Tr)} TL" + (s.Derece == "Uygun" ? " · UYGUN" : s.Derece == "Kısmi" ? " · KISMİ UYGUN" : " · UYARI");
            return s;
        }
        /// <summary>Öneri: sahipten hedef şirkete, mevcut borç ilişkileri (A → B: A'nın B'ye borcu) üzerinden en uygun zincir: oluşacak yeni grup içi bakiye (Σ max(0, tutar − borç)) en az; eşitlikte daha az adım, sonra daha çok kapanan borç. Grup 7 şirket olduğundan tüm basit yollar taranır.</summary>
        public static List<string> DolasimOner(int cekDocEntry, string hedef)
        {
            var k = Bul(cekDocEntry); if (k == null || string.IsNullOrEmpty(hedef) || hedef == k.Sirket) return new List<string>();
            var sirketler = GrupSirketleri; List<string> enIyi = null; decimal enIyiYeni = decimal.MaxValue, enIyiKapanan = -1;
            void Tara(string u, List<string> yol, decimal yeni, decimal kapanan)
            {
                if (yeni > enIyiYeni) return;
                if (u == hedef) { if (yeni < enIyiYeni || (yeni == enIyiYeni && (yol.Count < enIyi.Count || (yol.Count == enIyi.Count && kapanan > enIyiKapanan)))) { enIyi = new List<string>(yol); enIyiYeni = yeni; enIyiKapanan = kapanan; } return; }
                if (yol.Count >= sirketler.Count()) return;
                foreach (var v in sirketler) { if (v == u || v == k.Sirket || yol.Contains(v)) continue; var b = GrupBakiye(u, v); if (b <= 0) continue; yol.Add(v); Tara(v, yol, yeni + Math.Max(0, k.Tutar - b), kapanan + Math.Min(b, k.Tutar)); yol.RemoveAt(yol.Count - 1); }
            }
            Tara(k.Sirket, new List<string>(), 0, 0);
            return enIyi ?? new List<string> { hedef };
        }
        /// <summary>Planı belgeye yazar (adımlar Planlandı; plan tarihleri 2'şer gün arayla).</summary>
        public static (bool ok, string mesaj, CekKaydi kayit) DolasimKaydet(int cekDocEntry, List<string> zincir, string sonAdim, string tedarikci, int? planDocEntry, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k == null) return (false, "Belge bulunamadı.", null);
                if (!k.Portfoyde) return (false, $"{k.No} portföyde değil ({k.IslemTuru}); dolaşım planlanamaz.", k);
                var sim = DolasimSimule(cekDocEntry, zincir, sonAdim, tedarikci, null);
                k.Dolasim = sim.Adimlar.Select((a, i) => new CekDolasimAdim { Sira = a.Sira, Eden = a.Eden, Alan = a.Alan, Tur = a.Tur, PlanTarih = DateTime.Today.AddDays(2 * (i + 1)), KapananBakiye = a.Kapanan, YeniBorc = a.YeniBorc, Not = a.Not }).ToList();
                k.DolasimDocEntry = planDocEntry ?? k.PlanDocEntry; if (!string.IsNullOrWhiteSpace(tedarikci)) k.PlanlananTedarikci = tedarikci; if (k.IslemTuru == "Portföyde") k.IslemTuru = "Planlandı"; k.PlanlamaTarihi ??= DateTime.Now; if (string.IsNullOrEmpty(k.Planlayan)) k.Planlayan = kullanici ?? "";
                _gunluk.Add(new CekAkibetGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = k.Sirket, No = k.No, Islem = "Dolaşım Planı", Aciklama = sim.Ozet });
                return (true, $"{k.No} dolaşım planı kaydedildi: {k.DolasimOzeti} ({k.Dolasim.Count} adım)" + (sim.Uygun ? "" : " · simülasyon uyarı veriyor"), k);
            }
        }
        /// <summary>Sıradaki planlı adımı gerçekleştirir: grup içi ciro → belge alan şirketin portföyüne geçer, bakiyeler güncellenir; tedarikçiye ciro / bankaya ibraz → akıbet kapanır.</summary>
        public static (bool ok, string mesaj, CekKaydi kayit) DolasimAdimGerceklestir(int cekDocEntry, int sira, string kullanici, DateTime? zaman = null)
        {
            Tohumla();
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k == null) return (false, "Belge bulunamadı.", null);
                var adim = k.SonrakiAdim; if (adim == null) return (false, "Gerçekleştirilecek planlı adım yok.", k); if (sira > 0 && adim.Sira != sira) return (false, $"Önce {adim.Sira}. adım gerçekleşmeli ({adim.Eden} → {adim.Alan}).", k);
                var simdi = zaman ?? DateTime.Now; adim.Durum = "Gerçekleşti"; adim.GercekTarih = simdi; adim.BelgeNo = (adim.Tur == "Bankaya İbraz" ? "İBZ" : "CRO") + $"-{simdi:yyMM}-{k.DocEntry % 9000 + 1000 + adim.Sira}";
                if (adim.Tur == "Grup İçi Ciro")
                {
                    string key = adim.Eden + "|" + adim.Alan, ters = adim.Alan + "|" + adim.Eden; decimal bakiye = GrupBakiyeleri.TryGetValue(key, out var b) ? b : 0, kapanan = Math.Min(k.Tutar, bakiye), yeni = k.Tutar - kapanan;
                    GrupBakiyeleri[key] = bakiye - kapanan; if (yeni > 0) GrupBakiyeleri[ters] = (GrupBakiyeleri.TryGetValue(ters, out var tb) ? tb : 0) + yeni; adim.KapananBakiye = kapanan; adim.YeniBorc = yeni;
                    var s = Sirketler.FirstOrDefault(x => x.Ad == adim.Alan); k.Sirket = adim.Alan; k.DbName = s.DbName ?? adim.Alan; k.DbKey = s.DbKey ?? k.DbKey; k.IbrazYeri = ""; k.IbrazTuru = ""; k.IbrazTarihi = null; k.IbrazBelgeNo = "";
                    _hareketler.Add(new CekHareket { Tarih = simdi, CekDocEntry = k.DocEntry, No = k.No, Belge = k.Belge, Eden = adim.Eden, Alan = adim.Alan, Tur = "Grup İçi Ciro", BelgeNo = adim.BelgeNo, Tutar = k.Tutar, AdimSira = adim.Sira, AdimToplam = k.Dolasim.Count, Kullanici = kullanici ?? "" });
                    _gunluk.Add(new CekAkibetGunluk { Tarih = simdi, Kullanici = kullanici ?? "", Sirket = adim.Eden, No = k.No, Islem = "Grup İçi Ciro", Aciklama = $"{adim.Eden} → {adim.Alan} · {adim.BelgeNo} · kapanan {(kapanan).ToString("N0", Tr)} TL" + (yeni > 0 ? $" · yeni bakiye {(yeni).ToString("N0", Tr)} TL" : "") + $" · adım {adim.Sira}/{k.Dolasim.Count}" });
                    var fisler = MuhasebeOrnek.CiroFisi(DbName(adim.Eden), s.DbName, adim.Eden, adim.Alan, "Grup İçi Ciro", adim.BelgeNo, k.No, k.Tutar, kullanici); foreach (var f in fisler) f.Tarih = simdi;
                    return (true, $"{k.No}: {adim.Eden} → {adim.Alan} ciro edildi ({adim.BelgeNo}); belge artık {adim.Alan} portföyünde. Kapanan grup içi borç {(kapanan).ToString("N0", Tr)} TL." + (yeni > 0 ? $" Yeni bakiye {(yeni).ToString("N0", Tr)} TL." : "") + $" Muhasebe fişleri: {string.Join(", ", fisler.Select(f => f.No))}.", k);
                }
                _hareketler.Add(new CekHareket { Tarih = simdi, CekDocEntry = k.DocEntry, No = k.No, Belge = k.Belge, Eden = adim.Eden, Alan = adim.Alan, Tur = adim.Tur, BelgeNo = adim.BelgeNo, Tutar = k.Tutar, AdimSira = adim.Sira, AdimToplam = k.Dolasim.Count, Kullanici = kullanici ?? "" });
                string ad = adim.Tur == "Bankaya İbraz" ? "Bankaya Tahsile Verildi" : "Ciro Edildi"; string eski = k.IslemTuru; k.IslemTuru = ad; k.IbrazTuru = adim.Tur == "Bankaya İbraz" ? "Bankaya İbraz" : "Ciro"; k.IbrazYeri = adim.Tur == "Bankaya İbraz" ? ButceOrnek.TahsilBankalari[k.DocEntry % 2] : adim.Alan; k.IbrazTarihi = simdi; k.IbrazBelgeNo = adim.BelgeNo;
                _gunluk.Add(new CekAkibetGunluk { Tarih = simdi, Kullanici = kullanici ?? "", Sirket = k.Sirket, No = k.No, Islem = ad, Aciklama = $"{adim.Tur}: {k.IbrazYeri} · {adim.BelgeNo} · dolaşım tamamlandı ({k.DolasimOzeti})" + (k.Kontrol == "Farklı" ? $" · DİKKAT: plan {k.PlanlananTedarikci}" : "") });
                var sonFis = MuhasebeOrnek.CiroFisi(k.DbName, null, k.Sirket, k.IbrazYeri, adim.Tur, adim.BelgeNo, k.No, k.Tutar, kullanici); foreach (var f in sonFis) f.Tarih = simdi;
                return (true, $"{k.No}: {adim.Tur} gerçekleşti → {k.IbrazYeri} ({adim.BelgeNo}). Dolaşım tamamlandı; akıbet {ad}. Muhasebe fişi {string.Join(", ", sonFis.Select(f => f.No))}.", k);
            }
        }
        public static (bool ok, string mesaj) DolasimIptal(int cekDocEntry, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k == null) return (false, "Belge bulunamadı.");
                if (k.GerceklesenAdim > 0) return (false, "Gerçekleşmiş adımı olan plan iptal edilemez; kalan adımlar planlı kalır.");
                k.Dolasim.Clear(); k.DolasimDocEntry = null; _gunluk.Add(new CekAkibetGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = k.Sirket, No = k.No, Islem = "Dolaşım Planı İptal", Aciklama = "plan silindi" }); return (true, "Dolaşım planı iptal edildi.");
            }
        }
        public static (bool ok, string mesaj) KullanimDurumuDegistir(int cekDocEntry, string durum, string kullanici)
        {
            Tohumla();
            lock (Kilit)
            {
                var k = _kayitlar.FirstOrDefault(x => x.DocEntry == cekDocEntry); if (k == null) return (false, "Belge bulunamadı.");
                if (!KullanimDurumlari.Contains(durum ?? "")) return (false, "Geçersiz kullanım durumu.");
                _gunluk.Add(new CekAkibetGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Sirket = k.Sirket, No = k.No, Islem = "Kullanım Durumu", Aciklama = $"{k.KullanimDurumu} → {durum}" });
                k.KullanimDurumu = durum; return (true, "Kullanım durumu güncellendi.");
            }
        }
    }
}
