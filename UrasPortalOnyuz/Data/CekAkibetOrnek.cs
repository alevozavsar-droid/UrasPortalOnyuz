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

    public static class CekAkibetOrnek
    {
        public static readonly object Kilit = new object();
        /// <summary>Şirket adı ↔ Kasa Nakit Akışı DbName ↔ bağlantı anahtarı.</summary>
        public static readonly (string Ad, string DbName, string DbKey)[] Sirketler =
        {
            ("Uras Kimya", "URASKIMYA", "DefaultConnection"), ("Selvi", "SELVI", "DefaultConnection5"), ("Avrupa Paper", "AVRUPA_PAPER", "DefaultConnection2"),
            ("Alv Kimya", "ALV_KIMYA", "DefaultConnection3"), ("Daf Kimya", "DAF_KIMYA", "DefaultConnection4"), ("URS Makine", "URSMAKINE", "DefaultConnection1")
        };
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
        public static string IslemAdi(string kod) => IslemTurleri.FirstOrDefault(t => t.Kod == kod).Ad ?? kod;
        public static readonly string[] KullanimDurumlari = { "Kullanılabilir", "Riskli", "Teminat", "Bloke" };
        static readonly string[] Subeler = { "Merkez", "Gebze", "Çorlu", "İkitelli", "Bursa OSB", "Kadıköy", "Bornova", "Çerkezköy", "Kartal", "Ümraniye" };
        static readonly string[] UcuncuTaraf = { "Marmara Tekstil", "Ege Boya", "Anadolu Ambalaj", "Kuzey Plastik", "Trakya Kimyevi", "Akdeniz Dağıtım", "Doğu Lojistik", "Yıldız Tekstil" };

        private static readonly List<CekKaydi> _kayitlar = new List<CekKaydi>();
        private static readonly List<CekAkibetGunluk> _gunluk = new List<CekAkibetGunluk>();
        private static bool _tohumlandi;
        private static int Seed(string s) => s.Aggregate(23, (a, ch) => unchecked(a * 31 + ch));

        public static List<CekKaydi> Kayitlar { get { Tohumla(); lock (Kilit) return _kayitlar.ToList(); } }
        public static List<CekAkibetGunluk> Gunluk { get { Tohumla(); lock (Kilit) return _gunluk.OrderByDescending(g => g.Tarih).ToList(); } }
        public static CekKaydi Bul(int docEntry) { Tohumla(); lock (Kilit) return _kayitlar.FirstOrDefault(k => k.DocEntry == docEntry); }
        public static CekKaydi Bul(string sirket, string no) { Tohumla(); lock (Kilit) return _kayitlar.FirstOrDefault(k => k.No == no && (string.IsNullOrEmpty(sirket) || k.Sirket == sirket || k.DbName == sirket || k.DbKey == sirket)); }
        /// <summary>Kasa Nakit Akışı çek / senet seçimi için: şirketin portföyündeki, planlanmamış, kullanılabilir belgeler (vade sırasıyla).</summary>
        public static List<CekKaydi> Secilebilirler(string sirket, string belge) { Tohumla(); lock (Kilit) return _kayitlar.Where(k => k.Secilebilir && k.Belge == belge && (string.IsNullOrEmpty(sirket) || k.Sirket == sirket || k.DbName == sirket || k.DbKey == sirket)).OrderBy(k => k.Vade).ThenBy(k => k.No).ToList(); }
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
                        DocEntry = docEntry++, Sirket = c.Sirket, DbName = s.DbName ?? c.Sirket, DbKey = s.DbKey ?? "DefaultConnection", No = c.No, Belge = c.Belge, Musteri = c.Musteri, MusteriKod = c.MusteriKod,
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
