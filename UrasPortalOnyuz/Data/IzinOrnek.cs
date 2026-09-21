// ============================================================
// İZİN TALEP, ONAY, KULLANIM VE PUANTAJ MODÜLÜ — örnek veri ve iş kuralları
// Nakit Avans modülüyle aynı mimari (Data/NakitAvansOrnek.cs): tek Talep No (IZ-2026-000xxx), üç ayrı statü
// (Talep Durumu = workflow, Kullanım Durumu = PDKS, Puantaj Durumu = bordro), parametrik onay akışı, bakiye hareketleri,
// kaynak sistem olayları (PDKS giriş-çıkış, SGK rapor bildirimi, bordro puantaj aktarımı) Talep No ile işlenir ve ekran
// yeniden hesaplanır. Bakiye, kullanılan gün, fiili dönüş, puantaj durumu ekrana elle girilmez. Kayıtlar bellek içindedir.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class IzTuru
    {
        public string Kod { get; set; }
        public string Ad { get; set; }
        public bool Ucretli { get; set; }
        public bool BakiyedenDuser { get; set; }       // yıllık izin bakiyesinden düşer
        public decimal MaxGun { get; set; }             // 0 = sınırsız
        public bool BelgeGerekli { get; set; }          // rapor / evrak
        public string Akis { get; set; }                // Y = Yönetici, D = Direktör, I = İK  (örn. "Y,I")
        public string Dayanak { get; set; }
        public string Aciklama { get; set; }
        public int EnAzOnceBildirim { get; set; }       // gün (0 = yok)
    }

    public class IzOnay
    {
        public int Sira { get; set; }
        public string Adim { get; set; }
        public string Kisi { get; set; }
        public string Durum { get; set; }               // Tamamlandı | Onaylandı | Onay Bekliyor | Pasif | Reddedildi | Revizyon | İptal
        public DateTime? Tarih { get; set; }
        public string Not { get; set; }
    }

    public class IzHareket { public DateTime Tarih { get; set; } public string Hareket { get; set; } public string Kaynak { get; set; } }
    public class IzAudit { public DateTime Tarih { get; set; } public string Tablo { get; set; } public string Alan { get; set; } public string Eski { get; set; } public string Yeni { get; set; } public string Kullanici { get; set; } public string IslemTipi { get; set; } public string KaynakSistem { get; set; } }
    public class IzEk { public string Ad { get; set; } public string Tur { get; set; } public string Boyut { get; set; } public DateTime Tarih { get; set; } public string Yukleyen { get; set; } }

    /// <summary>Bakiye hareketi (hak ediş, devir, kullanım, iptal iadesi).</summary>
    public class IzBakiyeHareketi { public DateTime Tarih { get; set; } public string Tur { get; set; } public decimal Gun { get; set; } public string Ref { get; set; } public string Aciklama { get; set; } }

    public class IzinTalebi
    {
        public int Id { get; set; }
        public string No { get; set; }
        public DateTime Tarih { get; set; }
        public string Sirket { get; set; }
        public string Departman { get; set; }
        public NaKisi Olusturan { get; set; }
        public NaKisi Kullanan { get; set; }            // izni kullanacak personel
        public string IzinTuru { get; set; }            // kod
        public DateTime Baslangic { get; set; }
        public DateTime Bitis { get; set; }
        public string YarimGun { get; set; } = "Yok";   // Yok | Sabah | Öğleden Sonra (tek günlük izinlerde)
        public decimal IsGunu { get; set; }             // hesaplanır (hafta sonu + resmi tatil hariç)
        public int TakvimGunu { get; set; }
        public NaKisi Vekil { get; set; }
        public string Iletisim { get; set; }
        public string Adres { get; set; }
        public string Aciklama { get; set; }
        public List<IzEk> Ekler { get; set; } = new List<IzEk>();
        public List<IzOnay> Onaylar { get; set; } = new List<IzOnay>();
        public List<IzHareket> Zaman { get; set; } = new List<IzHareket>();
        public List<IzAudit> Audit { get; set; } = new List<IzAudit>();
        public int Revizyon { get; set; }
        public bool Iptal { get; set; }
        public string IptalNedeni { get; set; }
        // ---- PDKS / SGK / Bordro'dan gelen (elle girilmez) ----
        public DateTime? FiiliBaslangic { get; set; }
        public DateTime? FiiliDonus { get; set; }
        public bool DonusTeyidi { get; set; }
        public DateTime? DonusTeyitTarihi { get; set; }
        public string SgkRaporNo { get; set; }
        public bool BelgeAlindi { get; set; }
        public string PuantajDonemi { get; set; }
        public DateTime? PuantajTarihi { get; set; }
        public decimal? PuantajGun { get; set; }
        public string EntegrasyonDurumu { get; set; } = "Başarılı";
        public DateTime? SonSenkron { get; set; }
        public List<string> Uyarilar { get; set; } = new List<string>();   // oluşturma anındaki kural uyarıları

        public IzTuru Tur => IzinOrnek.TurBul(IzinTuru);
        public string TalepDurumu
        {
            get
            {
                if (Iptal) return "İptal Edildi";
                if (Onaylar.Any(o => o.Durum == "Reddedildi")) return "Reddedildi";
                if (Onaylar.Any(o => o.Durum == "Revizyon")) return "Revizyon Bekliyor";
                var b = Onaylar.FirstOrDefault(o => o.Durum == "Onay Bekliyor");
                return b != null ? b.Adim + " Bekliyor" : "Onaylandı";
            }
        }
        public bool Onaylandi => TalepDurumu == "Onaylandı";
        public string KullanimDurumu
        {
            get
            {
                if (Iptal) return "İptal";
                if (!Onaylandi) return "Planlanmadı";
                var bugun = DateTime.Today;
                if (FiiliDonus.HasValue) return FiiliDonus.Value.Date <= Bitis.Date ? "Erken Döndü" : "Tamamlandı";
                if (bugun > Bitis.Date) return DonusTeyidi ? "Tamamlandı" : "Dönüş Teyidi Bekliyor";
                if (bugun >= Baslangic.Date) return FiiliBaslangic.HasValue || bugun > Baslangic.Date ? "İzinde" : "İzinde (PDKS teyidi yok)";
                return "Planlandı";
            }
        }
        public bool Izinde => KullanimDurumu.StartsWith("İzinde");
        public string PuantajDurumu
        {
            get
            {
                if (Iptal || !Onaylandi) return "—";
                if (Tur != null && Tur.BelgeGerekli && !BelgeAlindi) return "Belge Bekleniyor";
                if (PuantajTarihi.HasValue) return Tur != null && Tur.Ucretli ? "Puantaja Aktarıldı" : "Ücretsiz Kesinti İşlendi";
                return DateTime.Today > Bitis.Date ? "Puantaj Bekliyor" : "Dönem Kapanışında";
            }
        }
        public decimal KullanilanGun => Onaylandi && !Iptal ? (FiiliDonus.HasValue ? IzinOrnek.IsGunuSay(Baslangic, FiiliDonus.Value.AddDays(-1), YarimGun) : IsGunu) : 0;
        public bool Gecmis => DateTime.Today > Bitis.Date;
        public bool BelgeEksik => Onaylandi && !Iptal && Tur != null && Tur.BelgeGerekli && !BelgeAlindi;
        public string Yonetici => Onaylar.FirstOrDefault(o => o.Adim == "Yönetici Onayı")?.Kisi;
        public string Direktor => Onaylar.FirstOrDefault(o => o.Adim == "Direktör Onayı")?.Kisi;
        public string Ik => Onaylar.FirstOrDefault(o => o.Adim == "İK Onayı")?.Kisi;
        public DateTime? OnayTarihi => Onaylandi ? Onaylar.Where(o => o.Tarih.HasValue).Select(o => o.Tarih).Max() : null;
    }

    public class IzBakiye
    {
        public NaKisi Kisi { get; set; }
        public DateTime IseGiris { get; set; }
        public int KidemYil { get; set; }
        public decimal HakEdis { get; set; }
        public decimal Devir { get; set; }
        public decimal Kullanilan { get; set; }
        public decimal Planlanan { get; set; }
        public decimal OnayBekleyen { get; set; }
        public decimal Kalan => HakEdis + Devir - Kullanilan - Planlanan;
        public decimal KullanilabilirNet => Kalan - OnayBekleyen;
        public DateTime SonrakiHakEdis { get; set; }
        public decimal SonrakiHakEdisGun { get; set; }
        public List<IzBakiyeHareketi> Hareketler { get; set; } = new List<IzBakiyeHareketi>();
        public decimal MazeretKullanilan { get; set; }
        public decimal RaporluGun { get; set; }
    }

    public static class IzinOrnek
    {
        public static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        public static readonly object Kilit = new object();

        // ---------- İzin türü master ----------
        public static readonly List<IzTuru> Turler = new List<IzTuru>
        {
            new IzTuru { Kod = "YILLIK", Ad = "Yıllık İzin", Ucretli = true, BakiyedenDuser = true, MaxGun = 0, Akis = "Y,I", Dayanak = "4857 s. İş K. m.53–60", Aciklama = "Kıdeme göre 14 / 20 / 26 iş günü; hafta sonu ve resmi tatiller sayılmaz.", EnAzOnceBildirim = 3 },
            new IzTuru { Kod = "MAZERET", Ad = "Mazeret İzni (Ücretli)", Ucretli = true, BakiyedenDuser = false, MaxGun = 3, Akis = "Y", Dayanak = "İşyeri yönetmeliği", Aciklama = "Yılda en fazla 3 gün; gerekçe zorunlu.", EnAzOnceBildirim = 0 },
            new IzTuru { Kod = "HASTALIK", Ad = "Hastalık (Raporlu)", Ucretli = true, BakiyedenDuser = false, MaxGun = 0, BelgeGerekli = true, Akis = "I", Dayanak = "5510 s. SSGSSK", Aciklama = "SGK e-rapor bildirimi gelmeden puantaja işlenmez.", EnAzOnceBildirim = 0 },
            new IzTuru { Kod = "UCRETSIZ", Ad = "Ücretsiz İzin", Ucretli = false, BakiyedenDuser = false, MaxGun = 0, Akis = "Y,D,I", Dayanak = "4857 m.56/7 ve sözleşme", Aciklama = "Direktör ve İK onayı gerekir; bordroda kesinti işlenir.", EnAzOnceBildirim = 7 },
            new IzTuru { Kod = "DOGUM", Ad = "Doğum İzni", Ucretli = true, BakiyedenDuser = false, MaxGun = 112, BelgeGerekli = true, Akis = "I", Dayanak = "4857 m.74", Aciklama = "Doğum öncesi 8 + sonrası 8 hafta; rapor zorunlu.", EnAzOnceBildirim = 0 },
            new IzTuru { Kod = "BABALIK", Ad = "Babalık İzni", Ucretli = true, BakiyedenDuser = false, MaxGun = 5, BelgeGerekli = true, Akis = "Y", Dayanak = "4857 Ek m.2", Aciklama = "5 gün; doğum belgesi.", EnAzOnceBildirim = 0 },
            new IzTuru { Kod = "EVLILIK", Ad = "Evlilik İzni", Ucretli = true, BakiyedenDuser = false, MaxGun = 3, BelgeGerekli = true, Akis = "Y", Dayanak = "4857 Ek m.2", Aciklama = "3 gün; evlilik cüzdanı.", EnAzOnceBildirim = 0 },
            new IzTuru { Kod = "VEFAT", Ad = "Vefat İzni", Ucretli = true, BakiyedenDuser = false, MaxGun = 3, BelgeGerekli = false, Akis = "Y", Dayanak = "4857 Ek m.2", Aciklama = "1. derece yakın vefatında 3 gün.", EnAzOnceBildirim = 0 },
            new IzTuru { Kod = "SUT", Ad = "Süt İzni", Ucretli = true, BakiyedenDuser = false, MaxGun = 0, Akis = "Y", Dayanak = "4857 m.74/7", Aciklama = "Günde 1,5 saat; ekranda tam/yarım gün olarak planlanır.", EnAzOnceBildirim = 0 },
            new IzTuru { Kod = "IDARI", Ad = "İdari İzin", Ucretli = true, BakiyedenDuser = false, MaxGun = 0, Akis = "D", Dayanak = "Yönetim kararı", Aciklama = "Şirket kararıyla verilen toplu / bireysel izin.", EnAzOnceBildirim = 0 }
        };
        public static IzTuru TurBul(string kod) => Turler.FirstOrDefault(t => string.Equals(t.Kod, kod, StringComparison.OrdinalIgnoreCase)) ?? Turler.FirstOrDefault(t => string.Equals(t.Ad, kod, StringComparison.OrdinalIgnoreCase));

        // ---------- Parametreler ----------
        /// <summary>Bakiye yetersizse: Sadece uyar | Avans izin (eksi bakiye, ek yönetici onayı) | Talebi engelle</summary>
        public static string BakiyePolitikasi = "Avans izin (eksi bakiye, ek yönetici onayı)";
        /// <summary>Aynı departmanda aynı gün izinli oranı bu eşiği aşarsa Direktör onayı eklenir.</summary>
        public static decimal DolulukEsigi = 0.34m;
        /// <summary>Bu iş gününden uzun yıllık izinlerde Direktör onayı eklenir.</summary>
        public static decimal UzunIzinEsigi = 10;
        public static int DevirUstSinir = 10;   // yıl sonu devredebilecek en fazla gün (örnek politika)

        // ---------- 2026 resmi tatiller (Türkiye) ----------
        public static readonly Dictionary<DateTime, string> Tatiller = new Dictionary<DateTime, string>
        {
            [new DateTime(2026, 1, 1)] = "Yılbaşı", [new DateTime(2026, 3, 19)] = "Ramazan Bayramı Arifesi (yarım)", [new DateTime(2026, 3, 20)] = "Ramazan Bayramı 1. gün", [new DateTime(2026, 3, 21)] = "Ramazan Bayramı 2. gün", [new DateTime(2026, 3, 22)] = "Ramazan Bayramı 3. gün",
            [new DateTime(2026, 4, 23)] = "Ulusal Egemenlik ve Çocuk Bayramı", [new DateTime(2026, 5, 1)] = "Emek ve Dayanışma Günü", [new DateTime(2026, 5, 19)] = "Atatürk'ü Anma, Gençlik ve Spor Bayramı",
            [new DateTime(2026, 5, 26)] = "Kurban Bayramı Arifesi (yarım)", [new DateTime(2026, 5, 27)] = "Kurban Bayramı 1. gün", [new DateTime(2026, 5, 28)] = "Kurban Bayramı 2. gün", [new DateTime(2026, 5, 29)] = "Kurban Bayramı 3. gün", [new DateTime(2026, 5, 30)] = "Kurban Bayramı 4. gün",
            [new DateTime(2026, 7, 15)] = "Demokrasi ve Millî Birlik Günü", [new DateTime(2026, 8, 30)] = "Zafer Bayramı", [new DateTime(2026, 10, 28)] = "Cumhuriyet Bayramı Arifesi (yarım)", [new DateTime(2026, 10, 29)] = "Cumhuriyet Bayramı",
            [new DateTime(2027, 1, 1)] = "Yılbaşı"
        };
        public static bool TatilMi(DateTime g) => Tatiller.ContainsKey(g.Date) && !Tatiller[g.Date].Contains("yarım");
        public static bool IsGunuMu(DateTime g) => g.DayOfWeek != DayOfWeek.Saturday && g.DayOfWeek != DayOfWeek.Sunday && !TatilMi(g);
        /// <summary>İş günü sayısı: hafta sonu ve resmi tatil hariç; arife yarım gün; tek günlük yarım izin 0,5.</summary>
        public static decimal IsGunuSay(DateTime b, DateTime e, string yarimGun = "Yok")
        {
            if (e < b) return 0;
            decimal n = 0;
            for (var g = b.Date; g <= e.Date; g = g.AddDays(1))
            {
                if (!IsGunuMu(g)) continue;
                n += Tatiller.ContainsKey(g) && Tatiller[g].Contains("yarım") ? 0.5m : 1;
            }
            if (b.Date == e.Date && yarimGun != "Yok" && n > 0) n = 0.5m;
            return n;
        }

        // ---------- Personel master: işe giriş, roller ----------
        public static readonly Dictionary<string, DateTime> IseGiris = new Dictionary<string, DateTime>
        {
            ["P1001"] = new DateTime(2019, 3, 11), ["P1002"] = new DateTime(2012, 6, 1), ["P1003"] = new DateTime(2021, 9, 6), ["P1004"] = new DateTime(2023, 2, 13), ["P1005"] = new DateTime(2020, 10, 5),
            ["P1006"] = new DateTime(2009, 1, 19), ["P1007"] = new DateTime(2005, 4, 4), ["P1008"] = new DateTime(2022, 7, 18), ["P1009"] = new DateTime(2018, 5, 21), ["P1010"] = new DateTime(2016, 11, 14),
            ["P1011"] = new DateTime(2014, 8, 25), ["P1012"] = new DateTime(2019, 12, 2), ["P1013"] = new DateTime(2024, 3, 4), ["P1014"] = new DateTime(2010, 2, 8), ["P1015"] = new DateTime(2008, 9, 15), ["P1016"] = new DateTime(2013, 5, 6), ["P1017"] = new DateTime(2025, 11, 3), ["P1018"] = new DateTime(2017, 2, 20)
        };
        public static NaKisi KisiBul(string s) => NakitAvansOrnek.KisiBul(s);
        public static NaKisi Yonetici(NaKisi k) => NakitAvansOrnek.Yonetici(k);
        public static NaKisi Direktor(NaKisi k) => NakitAvansOrnek.Personel.First(p => p.Unvan == "Genel Müdür Yardımcısı");
        public static NaKisi IkUzmani() => NakitAvansOrnek.Personel.First(p => p.Rol == "İK");
        public static int KidemYil(NaKisi k) { var g = IseGiris.TryGetValue(k.PersonelNo, out var d) ? d : DateTime.Today.AddYears(-1); int y = DateTime.Today.Year - g.Year; if (g.AddYears(y) > DateTime.Today) y--; return y; }
        /// <summary>4857 m.53: 1–5 yıl 14, 5–15 yıl 20, 15+ yıl 26 gün; 1 yılı doldurmayan hak etmez; 18 yaş altı / 50 yaş üstü en az 20 (örnekte uygulanmadı).</summary>
        public static decimal YillikHak(int kidem) => kidem < 1 ? 0 : kidem < 5 ? 14 : kidem < 15 ? 20 : 26;

        // ---------- Kayıtlar ----------
        private static int _sonId = 0;
        public static readonly List<IzinTalebi> Talepler = new List<IzinTalebi>();
        public static readonly Dictionary<string, decimal> Devirler = new Dictionary<string, decimal> { ["P1001"] = 4, ["P1002"] = 9, ["P1006"] = 10, ["P1007"] = 6, ["P1010"] = 2, ["P1011"] = 7, ["P1012"] = 3, ["P1014"] = 5 };
        public static readonly HashSet<string> IslenenIslemler = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static IzinOrnek() { Tohum(); }

        private static void Hareket(IzinTalebi a, DateTime t, string h, string k) => a.Zaman.Add(new IzHareket { Tarih = t, Hareket = h, Kaynak = k });
        private static void Log(IzinTalebi a, DateTime t, string tablo, string alan, string eski, string yeni, string kul, string islem, string kaynak) => a.Audit.Add(new IzAudit { Tarih = t, Tablo = tablo, Alan = alan, Eski = eski, Yeni = yeni, Kullanici = kul, IslemTipi = islem, KaynakSistem = kaynak });

        // ---------- Bakiye (canlı hesap) ----------
        public static IzBakiye Bakiye(NaKisi k, int? haricId = null)
        {
            var b = new IzBakiye { Kisi = k, IseGiris = IseGiris.TryGetValue(k.PersonelNo, out var g) ? g : DateTime.Today.AddYears(-1), KidemYil = KidemYil(k) };
            b.HakEdis = YillikHak(b.KidemYil); b.Devir = Devirler.TryGetValue(k.PersonelNo, out var d) ? d : 0;
            b.SonrakiHakEdis = b.IseGiris.AddYears(b.KidemYil + 1); b.SonrakiHakEdisGun = YillikHak(b.KidemYil + 1);
            if (b.Devir > 0) b.Hareketler.Add(new IzBakiyeHareketi { Tarih = new DateTime(DateTime.Today.Year, 1, 1), Tur = "Devir", Gun = b.Devir, Ref = (DateTime.Today.Year - 1).ToString(), Aciklama = "Önceki yıldan devreden" });
            if (b.HakEdis > 0) b.Hareketler.Add(new IzBakiyeHareketi { Tarih = b.IseGiris.AddYears(b.KidemYil) > DateTime.Today ? b.IseGiris.AddYears(b.KidemYil - 1) : b.IseGiris.AddYears(b.KidemYil), Tur = "Hak Ediş", Gun = b.HakEdis, Ref = b.KidemYil + ". yıl", Aciklama = $"Kıdem {b.KidemYil} yıl → {b.HakEdis} iş günü" });
            foreach (var t in Talepler.Where(t => t.Kullanan.PersonelNo == k.PersonelNo && t.Id != haricId).OrderBy(t => t.Baslangic))
            {
                if (t.Tur == null) continue;
                if (t.Tur.Kod == "MAZERET" && t.Onaylandi && !t.Iptal) b.MazeretKullanilan += t.IsGunu;
                if (t.Tur.Kod == "HASTALIK" && t.Onaylandi && !t.Iptal) b.RaporluGun += t.IsGunu;
                if (!t.Tur.BakiyedenDuser) continue;
                if (t.Iptal || t.TalepDurumu == "Reddedildi") { if (t.Iptal && t.Onaylar.All(o => o.Durum != "Onay Bekliyor")) b.Hareketler.Add(new IzBakiyeHareketi { Tarih = t.Zaman.LastOrDefault()?.Tarih ?? t.Tarih, Tur = "İptal İadesi", Gun = t.IsGunu, Ref = t.No, Aciklama = "İptal edilen izin bakiyeye iade" }); continue; }
                if (t.Onaylandi) { var g2 = t.KullanilanGun; if (t.Gecmis || t.FiiliDonus.HasValue) b.Kullanilan += g2; else b.Planlanan += g2; b.Hareketler.Add(new IzBakiyeHareketi { Tarih = t.Baslangic, Tur = t.Gecmis || t.FiiliDonus.HasValue ? "Kullanım" : "Planlanan", Gun = -g2, Ref = t.No, Aciklama = $"{t.Baslangic:dd.MM} – {t.Bitis:dd.MM}" + (t.FiiliDonus.HasValue && t.KullanimDurumu == "Erken Döndü" ? " (erken dönüş)" : "") }); }
                else if (t.TalepDurumu != "Reddedildi") b.OnayBekleyen += t.IsGunu;
            }
            b.Hareketler = b.Hareketler.OrderBy(h => h.Tarih).ToList();
            return b;
        }

        // ---------- Kurallar ----------
        public static List<IzinTalebi> Cakisanlar(NaKisi k, DateTime b, DateTime e, int? haricId = null) => Talepler.Where(t => t.Kullanan.PersonelNo == k.PersonelNo && t.Id != haricId && !t.Iptal && t.TalepDurumu != "Reddedildi" && t.Baslangic.Date <= e.Date && t.Bitis.Date >= b.Date).ToList();
        public static List<IzinTalebi> DepartmanIzinlileri(NaKisi k, DateTime b, DateTime e, int? haricId = null) => Talepler.Where(t => t.Kullanan.PersonelNo != k.PersonelNo && t.Kullanan.Departman == k.Departman && t.Kullanan.Sirket == k.Sirket && t.Id != haricId && !t.Iptal && t.Onaylandi && t.Baslangic.Date <= e.Date && t.Bitis.Date >= b.Date).ToList();
        public static int DepartmanMevcudu(NaKisi k) => NakitAvansOrnek.Personel.Count(p => p.Departman == k.Departman && p.Sirket == k.Sirket && p.Aktif);

        public static List<IzOnay> OnayAkisiOlustur(IzinTalebi a, DateTime t, out List<string> uyarilar)
        {
            uyarilar = new List<string>();
            var tur = a.Tur; var adimlar = new List<string>(tur.Akis.Split(','));
            var bak = Bakiye(a.Kullanan, a.Id);
            if (tur.BakiyedenDuser && a.IsGunu > bak.KullanilabilirNet)
            {
                uyarilar.Add($"Bakiye yetersiz: kullanılabilir {bak.KullanilabilirNet:0.#} gün, talep {a.IsGunu:0.#} gün (avans izin: {a.IsGunu - bak.KullanilabilirNet:0.#} gün).");
                if (BakiyePolitikasi.StartsWith("Avans") && !adimlar.Contains("D")) adimlar.Insert(adimlar.IndexOf("Y") + 1, "D");
            }
            if (tur.Kod == "YILLIK" && a.IsGunu > UzunIzinEsigi && !adimlar.Contains("D")) { adimlar.Insert(adimlar.IndexOf("Y") + 1, "D"); uyarilar.Add($"{UzunIzinEsigi:0} iş gününden uzun izin: Direktör onayı eklendi."); }
            var dep = DepartmanIzinlileri(a.Kullanan, a.Baslangic, a.Bitis, a.Id); int mevcut = DepartmanMevcudu(a.Kullanan);
            // Doluluk kuralı: aynı tarihlerde en az bir kişi daha izinliyse ve izinli oranı eşiği aşıyorsa (3 kişiden küçük ekipler muaf)
            if (dep.Count > 0 && mevcut >= 3 && (decimal)(dep.Count + 1) / mevcut > DolulukEsigi) { if (!adimlar.Contains("D")) adimlar.Insert(adimlar.IndexOf("Y") + 1, "D"); uyarilar.Add($"Departman doluluğu: aynı tarihlerde {dep.Count} kişi daha izinli ({dep.Count + 1}/{mevcut}, eşik %{DolulukEsigi * 100:0}); Direktör onayı eklendi."); }
            else if (dep.Count > 0) uyarilar.Add($"Bilgi: aynı tarihlerde departmanda {dep.Count} kişi daha izinli ({string.Join(", ", dep.Select(x => x.Kullanan.AdSoyad))}).");
            if (tur.MaxGun > 0 && a.IsGunu > tur.MaxGun) uyarilar.Add($"{tur.Ad} en fazla {tur.MaxGun} gün olabilir; talep {a.IsGunu:0.#} gün.");
            if (tur.Kod == "MAZERET" && bak.MazeretKullanilan + a.IsGunu > 3) uyarilar.Add($"Yıllık ücretli mazeret hakkı 3 gün; bu yıl kullanılan {bak.MazeretKullanilan:0.#} gün.");
            if (tur.EnAzOnceBildirim > 0 && (a.Baslangic.Date - t.Date).TotalDays < tur.EnAzOnceBildirim) uyarilar.Add($"{tur.Ad} en az {tur.EnAzOnceBildirim} gün önceden talep edilmelidir.");
            if (a.Vekil != null && Cakisanlar(a.Vekil, a.Baslangic, a.Bitis).Any(x => x.Onaylandi)) uyarilar.Add($"Vekil {a.Vekil.AdSoyad} aynı tarihlerde izinli görünüyor.");
            var liste = new List<IzOnay> { new IzOnay { Sira = 1, Adim = "Talep Oluşturma", Kisi = a.Olusturan.AdSoyad, Durum = "Tamamlandı", Tarih = t } };
            foreach (var ad in adimlar)
            {
                var (adim, kisi) = ad == "Y" ? ("Yönetici Onayı", Yonetici(a.Kullanan).AdSoyad) : ad == "D" ? ("Direktör Onayı", Direktor(a.Kullanan).AdSoyad) : ("İK Onayı", IkUzmani().AdSoyad);
                liste.Add(new IzOnay { Sira = liste.Count + 1, Adim = adim, Kisi = kisi, Durum = liste.Count == 1 ? "Onay Bekliyor" : "Pasif" });
            }
            return liste;
        }

        public static string Olustur(IzinTalebi a, out IzinTalebi kayit)
        {
            kayit = null;
            if (a.Kullanan == null) return "İzni kullanacak personel seçilmelidir.";
            if (!a.Kullanan.Aktif) return "Aktif olmayan personele izin açılamaz.";
            if (a.Tur == null) return "İzin türü seçilmelidir.";
            if (a.Bitis.Date < a.Baslangic.Date) return "Bitiş tarihi başlangıçtan önce olamaz.";
            a.IsGunu = IsGunuSay(a.Baslangic, a.Bitis, a.YarimGun); a.TakvimGunu = (a.Bitis.Date - a.Baslangic.Date).Days + 1;
            if (a.IsGunu <= 0) return "Seçilen aralıkta iş günü yok (hafta sonu / resmi tatil).";
            var cak = Cakisanlar(a.Kullanan, a.Baslangic, a.Bitis);
            if (cak.Count > 0) return $"Bu tarihlerde çakışan izin var: {string.Join(", ", cak.Select(c => c.No))}.";
            if (a.Tur.Kod == "MAZERET" && string.IsNullOrWhiteSpace(a.Aciklama)) return "Mazeret izninde gerekçe zorunludur.";
            if (a.Vekil != null && a.Vekil.PersonelNo == a.Kullanan.PersonelNo) return "Kişi kendisine vekil olamaz.";
            var t = DateTime.Now;
            lock (Kilit) { a.Id = ++_sonId; a.No = $"IZ-{t.Year}-{(_sonId + 200):000000}"; }
            a.Tarih = t; a.Departman = a.Kullanan.Departman; a.Sirket = a.Kullanan.Sirket;
            var bak = Bakiye(a.Kullanan);
            if (a.Tur.BakiyedenDuser && a.IsGunu > bak.KullanilabilirNet && BakiyePolitikasi == "Talebi engelle") { lock (Kilit) _sonId--; return $"Bakiye yetersiz (kullanılabilir {bak.KullanilabilirNet:0.#} gün); politika gereği talep engellendi."; }
            a.Onaylar = OnayAkisiOlustur(a, t, out var uy); a.Uyarilar = uy;
            Hareket(a, t, $"Talep oluşturuldu ({a.Tur.Ad}, {a.IsGunu:0.#} iş günü)", a.Olusturan.AdSoyad);
            Log(a, t, "LEAVE_REQUEST", "REQUEST_STATUS", "", a.TalepDurumu, a.Olusturan.KullaniciId, "Insert", "Portal");
            Log(a, t, "LEAVE_REQUEST", "WORK_DAYS", "", a.IsGunu.ToString("0.#"), "Sistem", "Insert", "Sistem");
            a.SonSenkron = t;
            lock (Kilit) Talepler.Add(a);
            kayit = a; return null;
        }

        public static string OnayEylemi(IzinTalebi a, string eylem, string not, string kullanici)
        {
            if (a.Iptal) return "İptal edilmiş talepte işlem yapılamaz.";
            var adim = a.Onaylar.FirstOrDefault(o => o.Durum == "Onay Bekliyor");
            if (adim == null) return "Bekleyen onay adımı yok.";
            var t = DateTime.Now;
            if (eylem == "Reddet") { if (string.IsNullOrWhiteSpace(not)) return "Red gerekçesi zorunludur."; adim.Durum = "Reddedildi"; adim.Tarih = t; adim.Not = not; foreach (var o in a.Onaylar.Where(o => o.Durum == "Pasif")) o.Durum = "İptal"; Hareket(a, t, $"{adim.Adim} — reddedildi ({not})", adim.Kisi); Log(a, t, "LEAVE_APPROVAL", "STATUS", "Onay Bekliyor", "Reddedildi", kullanici, "Update", "Workflow"); return null; }
            if (eylem == "Revizyon") { if (string.IsNullOrWhiteSpace(not)) return "Revizyon açıklaması zorunludur."; adim.Durum = "Revizyon"; adim.Tarih = t; adim.Not = not; Hareket(a, t, $"{adim.Adim} — revizyona gönderildi ({not})", adim.Kisi); Log(a, t, "LEAVE_APPROVAL", "STATUS", "Onay Bekliyor", "Revizyon", kullanici, "Update", "Workflow"); return null; }
            if (eylem == "Onayla")
            {
                adim.Durum = "Onaylandı"; adim.Tarih = t; adim.Not = not;
                var sonraki = a.Onaylar.FirstOrDefault(o => o.Durum == "Pasif");
                if (sonraki != null) sonraki.Durum = "Onay Bekliyor";
                else { Hareket(a, t, $"Talep onaylandı; {a.IsGunu:0.#} iş günü bakiyeden planlandı, takvime ve PDKS'ye iletildi", "Sistem"); Log(a, t, "LEAVE_BALANCE", "PLANNED_DAYS", "", a.IsGunu.ToString("0.#"), "Sistem", "Event", "Sistem"); }
                Hareket(a, t, adim.Adim.Replace(" Onayı", "") + " onayladı" + (string.IsNullOrEmpty(not) ? "" : " (" + not + ")"), adim.Kisi);
                Log(a, t, "LEAVE_APPROVAL", "STATUS", "Onay Bekliyor", "Onaylandı", kullanici, "Update", "Workflow");
                return null;
            }
            return "Tanımsız eylem.";
        }

        public static string Revize(IzinTalebi a, DateTime b, DateTime e, string yarim, string aciklama, string kullanici)
        {
            if (a.TalepDurumu != "Revizyon Bekliyor") return "Talep revizyon durumunda değil.";
            if (e.Date < b.Date) return "Bitiş başlangıçtan önce olamaz.";
            var t = DateTime.Now; string eski = $"{a.Baslangic:dd.MM.yyyy} – {a.Bitis:dd.MM.yyyy} ({a.IsGunu:0.#} g)";
            a.Baslangic = b; a.Bitis = e; a.YarimGun = yarim ?? "Yok"; a.IsGunu = IsGunuSay(b, e, a.YarimGun); a.TakvimGunu = (e.Date - b.Date).Days + 1;
            if (!string.IsNullOrWhiteSpace(aciklama)) a.Aciklama = aciklama;
            var cak = Cakisanlar(a.Kullanan, b, e, a.Id); if (cak.Count > 0) return $"Yeni tarihler çakışıyor: {string.Join(", ", cak.Select(c => c.No))}.";
            a.Revizyon++; a.Onaylar = OnayAkisiOlustur(a, t, out var uy); a.Uyarilar = uy;
            Hareket(a, t, $"Talep revize edildi: {eski} → {b:dd.MM.yyyy} – {e:dd.MM.yyyy} ({a.IsGunu:0.#} g); onay akışı yeniden hesaplandı", a.Olusturan.AdSoyad);
            Log(a, t, "LEAVE_REQUEST", "DATE_RANGE", eski, $"{b:dd.MM.yyyy} – {e:dd.MM.yyyy}", kullanici, "Update", "Portal");
            return null;
        }

        /// <summary>İptal: başlamamış izin talep sahibi / yönetici tarafından iptal edilebilir; başlamışsa yalnızca İK (erken dönüşle).</summary>
        public static string IptalEt(IzinTalebi a, string neden, string kullanici)
        {
            if (a.Iptal) return "Zaten iptal edilmiş.";
            if (string.IsNullOrWhiteSpace(neden)) return "İptal nedeni zorunludur.";
            if (a.Onaylandi && DateTime.Today >= a.Baslangic.Date) return "Başlamış izin iptal edilemez; erken dönüş PDKS'den (IzinBitti olayı) işlenir.";
            var t = DateTime.Now; a.Iptal = true; a.IptalNedeni = neden;
            foreach (var o in a.Onaylar.Where(o => o.Durum == "Onay Bekliyor" || o.Durum == "Pasif")) o.Durum = "İptal";
            Hareket(a, t, $"Talep iptal edildi ({neden})" + (a.Onaylar.Count(o => o.Durum == "Onaylandı") == a.Onaylar.Count - 1 ? $"; {a.IsGunu:0.#} gün bakiyeye iade edildi" : ""), kullanici);
            Log(a, t, "LEAVE_REQUEST", "REQUEST_STATUS", "Onaylandı", "İptal Edildi", kullanici, "Update", "Portal");
            return null;
        }

        /// <summary>Kaynak sistem olayları: PDKS (IzinBasladi / IzinBitti), SGK (RaporBildirimi), Bordro (PuantajAktarildi), İK (BelgeAlindi). Idempotent.</summary>
        public static string Olay(IzinTalebi a, string olay, string islemId, string ek1, string ek2, string kullanici)
        {
            if (string.IsNullOrWhiteSpace(islemId)) return "Transaction ID zorunludur.";
            lock (Kilit) { if (IslenenIslemler.Contains(islemId)) return $"'{islemId}' daha önce işlendi; mükerrer olay yok sayıldı."; }
            if (!a.Onaylandi && olay != "RaporBildirimi") return "Onaylanmamış izin için PDKS / bordro olayı işlenemez.";
            var t = DateTime.Now;
            switch (olay)
            {
                case "IzinBasladi":
                    a.FiiliBaslangic = DateTime.TryParse(ek1, out var fb) ? fb : a.Baslangic;
                    Hareket(a, t, $"PDKS: izin başladı ({a.FiiliBaslangic:dd.MM.yyyy}); giriş kaydı yok", "PDKS"); Log(a, t, "LEAVE_REQUEST", "ACTUAL_START", "", a.FiiliBaslangic.Value.ToString("dd.MM.yyyy"), "Sistem", "Event", "PDKS"); break;
                case "IzinBitti":
                    var fd = DateTime.TryParse(ek1, out var d1) ? d1 : a.Bitis.AddDays(1);
                    if (fd.Date <= a.Baslangic.Date) return "Dönüş tarihi izin başlangıcından sonra olmalıdır.";
                    string eskiG = a.KullanilanGun.ToString("0.#"); a.FiiliDonus = fd;
                    Hareket(a, t, $"PDKS: işe dönüş {fd:dd.MM.yyyy}" + (fd.Date <= a.Bitis.Date ? $" — erken dönüş, kullanılan {a.KullanilanGun:0.#} gün, {a.IsGunu - a.KullanilanGun:0.#} gün bakiyeye iade" : " — planlandığı gibi"), "PDKS");
                    Log(a, t, "LEAVE_BALANCE", "USED_DAYS", eskiG, a.KullanilanGun.ToString("0.#"), "Sistem", "Event", "PDKS"); break;
                case "RaporBildirimi":
                    a.SgkRaporNo = string.IsNullOrEmpty(ek1) ? "SGK-" + (400000 + a.Id * 13) : ek1; a.BelgeAlindi = true;
                    Hareket(a, t, $"SGK e-rapor bildirimi alındı ({a.SgkRaporNo}); belge şartı karşılandı", "SGK"); Log(a, t, "LEAVE_REQUEST", "SGK_REPORT_NO", "", a.SgkRaporNo, "Sistem", "Event", "SGK"); break;
                case "BelgeAlindi":
                    a.BelgeAlindi = true; a.Ekler.Add(new IzEk { Ad = string.IsNullOrEmpty(ek1) ? "belge.pdf" : ek1, Tur = "Belge", Boyut = "—", Tarih = t, Yukleyen = kullanici ?? "İK" });
                    Hareket(a, t, "İK: belge teslim alındı", "İK"); Log(a, t, "LEAVE_ATTACHMENT", "DOCUMENT", "", ek1, kullanici, "Insert", "Portal"); break;
                case "PuantajAktarildi":
                    if (a.BelgeEksik) return "Belge alınmadan puantaja aktarılamaz.";
                    if (!a.Gecmis && !a.FiiliDonus.HasValue) return "İzin tamamlanmadan puantaj aktarımı yapılamaz (dönem kapanışında).";
                    a.PuantajTarihi = t; a.PuantajDonemi = string.IsNullOrEmpty(ek1) ? a.Bitis.ToString("yyyy-MM") : ek1; a.PuantajGun = a.KullanilanGun;
                    Hareket(a, t, $"Bordro: {a.PuantajDonemi} dönemi puantajına {a.PuantajGun:0.#} gün {(a.Tur.Ucretli ? "ücretli izin" : "ücretsiz kesinti")} olarak aktarıldı", "Bordro");
                    Log(a, t, "LEAVE_PAYROLL", "PAYROLL_STATUS", "Bekliyor", a.PuantajDurumu, "Sistem", "Event", "Bordro"); break;
                case "PuantajTersKayit":
                    if (!a.PuantajTarihi.HasValue) return "Aktarılmış puantaj yok.";
                    a.PuantajTarihi = null; a.PuantajGun = null; Hareket(a, t, "Bordro: puantaj ters kaydı işlendi; durum yeniden hesaplandı", "Bordro"); Log(a, t, "LEAVE_PAYROLL", "PAYROLL_STATUS", "Aktarıldı", a.PuantajDurumu, "Sistem", "Event", "Bordro"); break;
                default: return "Tanımsız olay: " + olay;
            }
            lock (Kilit) IslenenIslemler.Add(islemId);
            a.SonSenkron = t; return null;
        }

        public static string DonusTeyidi(IzinTalebi a, string kullanici)
        {
            if (!a.Onaylandi || a.Iptal) return "Onaylı izin değil.";
            if (a.DonusTeyidi) return "Dönüş zaten teyit edilmiş.";
            if (DateTime.Today < a.Baslangic.Date) return "İzin henüz başlamadı.";
            a.DonusTeyidi = true; a.DonusTeyitTarihi = DateTime.Now; if (!a.FiiliDonus.HasValue && a.Gecmis) a.FiiliDonus = a.Bitis.AddDays(1);
            Hareket(a, a.DonusTeyitTarihi.Value, "Dönüş teyidi: \"İzinden döndüm, görevime başladım.\"", a.Kullanan.AdSoyad);
            Log(a, a.DonusTeyitTarihi.Value, "LEAVE_REQUEST", "RETURN_CONFIRMED", "false", "true", kullanici, "Update", "Portal");
            return null;
        }

        // ---------- Örnek tohum ----------
        private static void Tohum()
        {
            var bugun = DateTime.Today;
            IzinTalebi T(string olusturan, string kullanan, string tur, int basGun, int bitGun, string aciklama, string vekil = null, int gunOnce = 0, string yarim = "Yok", string adres = null)
            {
                var o = KisiBul(olusturan); var k = KisiBul(kullanan);
                var a = new IzinTalebi { Id = ++_sonId, Olusturan = o, Kullanan = k, IzinTuru = tur, Baslangic = bugun.AddDays(basGun), Bitis = bugun.AddDays(bitGun), YarimGun = yarim, Aciklama = aciklama, Vekil = vekil == null ? null : KisiBul(vekil), Iletisim = "0 532 000 " + (10 + _sonId) + " 00", Adres = adres, Sirket = k.Sirket, Departman = k.Departman };
                a.No = $"IZ-{bugun.Year}-{(_sonId + 200):000000}"; a.Tarih = bugun.AddDays(-(gunOnce > 0 ? gunOnce : Math.Max(1, -basGun + 5))).AddHours(9).AddMinutes(15 + _sonId);
                a.IsGunu = IsGunuSay(a.Baslangic, a.Bitis, yarim); a.TakvimGunu = (a.Bitis.Date - a.Baslangic.Date).Days + 1;
                a.Onaylar = OnayAkisiOlustur(a, a.Tarih, out var uy); a.Uyarilar = uy;
                Hareket(a, a.Tarih, $"Talep oluşturuldu ({a.Tur.Ad}, {a.IsGunu:0.#} iş günü)", o.AdSoyad);
                Log(a, a.Tarih, "LEAVE_REQUEST", "REQUEST_STATUS", "", a.TalepDurumu, o.KullaniciId, "Insert", "Portal");
                a.SonSenkron = a.Tarih; Talepler.Add(a); return a;
            }
            void Onayla(IzinTalebi a, int adet = 9)
            {
                for (int i = 0; i < adet; i++)
                {
                    var adim = a.Onaylar.FirstOrDefault(x => x.Durum == "Onay Bekliyor"); if (adim == null) break;
                    var t = a.Tarih.AddHours(3 * (i + 1)); adim.Durum = "Onaylandı"; adim.Tarih = t;
                    var s = a.Onaylar.FirstOrDefault(x => x.Durum == "Pasif"); if (s != null) s.Durum = "Onay Bekliyor";
                    Hareket(a, t, adim.Adim.Replace(" Onayı", "") + " onayladı", adim.Kisi); Log(a, t, "LEAVE_APPROVAL", "STATUS", "Onay Bekliyor", "Onaylandı", adim.Kisi, "Update", "Workflow");
                    if (s == null) Hareket(a, t.AddMinutes(1), $"Talep onaylandı; {a.IsGunu:0.#} iş günü bakiyeden planlandı, takvime ve PDKS'ye iletildi", "Sistem");
                }
            }
            void Pdks(IzinTalebi a, bool bitti = true, int? erkenDonusGun = null)
            {
                var t1 = a.Baslangic.AddHours(8).AddMinutes(30); a.FiiliBaslangic = a.Baslangic; IslenenIslemler.Add("PDKS-B-" + a.Id);
                Hareket(a, t1, $"PDKS: izin başladı ({a.Baslangic:dd.MM.yyyy}); giriş kaydı yok", "PDKS"); Log(a, t1, "LEAVE_REQUEST", "ACTUAL_START", "", a.Baslangic.ToString("dd.MM.yyyy"), "Sistem", "Event", "PDKS");
                if (bitti) { var d = erkenDonusGun.HasValue ? a.Baslangic.AddDays(erkenDonusGun.Value) : a.Bitis.AddDays(1); while (!IsGunuMu(d)) d = d.AddDays(1); a.FiiliDonus = d; IslenenIslemler.Add("PDKS-D-" + a.Id); var t2 = d.AddHours(8).AddMinutes(41); Hareket(a, t2, $"PDKS: işe dönüş {d:dd.MM.yyyy}" + (erkenDonusGun.HasValue ? $" — erken dönüş, kullanılan {a.KullanilanGun:0.#} gün, {a.IsGunu - a.KullanilanGun:0.#} gün bakiyeye iade" : " — planlandığı gibi"), "PDKS"); a.DonusTeyidi = true; a.DonusTeyitTarihi = t2.AddMinutes(20); Hareket(a, a.DonusTeyitTarihi.Value, "Dönüş teyidi: \"İzinden döndüm, görevime başladım.\"", a.Kullanan.AdSoyad); }
                a.SonSenkron = a.Zaman.Last().Tarih;
            }
            void Puantaj(IzinTalebi a) { var t = a.Bitis.AddDays(3).AddHours(17); a.PuantajTarihi = t; a.PuantajDonemi = a.Bitis.ToString("yyyy-MM"); a.PuantajGun = a.KullanilanGun; IslenenIslemler.Add("PAY-" + a.Id); Hareket(a, t, $"Bordro: {a.PuantajDonemi} dönemi puantajına {a.PuantajGun:0.#} gün {(a.Tur.Ucretli ? "ücretli izin" : "ücretsiz kesinti")} olarak aktarıldı", "Bordro"); Log(a, t, "LEAVE_PAYROLL", "PAYROLL_STATUS", "Bekliyor", "Aktarıldı", "Sistem", "Event", "Bordro"); a.SonSenkron = t; }

            // Ali Veli (it02): geçmiş yıllık izin (kullanıldı, puantajlandı), yaklaşan onaylı izin, onay bekleyen mazeret, reddedilen, revizyon bekleyen
            var a1 = T("Ali Veli", "Ali Veli", "YILLIK", -62, -56, "Yaz tatili, aile ziyareti", "Kerem Aksoy", 75, "Yok", "Muğla / Fethiye"); Onayla(a1); Pdks(a1); Puantaj(a1);
            var a2 = T("Ali Veli", "Ali Veli", "YILLIK", -25, -24, "Kısa tatil", "Kerem Aksoy", 32); Onayla(a2); Pdks(a2); Puantaj(a2);
            var a3 = T("Ali Veli", "Ali Veli", "YILLIK", 15, 19, "Ekim tatili", "Kerem Aksoy", 4, "Yok", "İstanbul"); Onayla(a3);
            var a4 = T("Ali Veli", "Ali Veli", "MAZERET", 8, 8, "Çocuğun okul veli toplantısı (öğleden sonra)", "Kerem Aksoy", 1, "Öğleden Sonra");
            var a5 = T("Ali Veli", "Ali Veli", "YILLIK", -40, -38, "Uzatılmış hafta sonu", "Kerem Aksoy", 48); { var adim = a5.Onaylar.First(x => x.Durum == "Onay Bekliyor"); adim.Durum = "Reddedildi"; adim.Tarih = a5.Tarih.AddHours(2); adim.Not = "Sunucu geçişi haftası; başka tarih önerin."; foreach (var o in a5.Onaylar.Where(o => o.Durum == "Pasif")) o.Durum = "İptal"; Hareket(a5, adim.Tarih.Value, "Yönetici Onayı — reddedildi (" + adim.Not + ")", adim.Kisi); }
            var a6 = T("Ali Veli", "Ali Veli", "YILLIK", 40, 53, "Yılbaşı öncesi uzun tatil", "Kerem Aksoy", 2, "Yok", "Antalya"); { var adim = a6.Onaylar.First(x => x.Durum == "Onay Bekliyor"); adim.Durum = "Revizyon"; adim.Tarih = a6.Tarih.AddHours(5); adim.Not = "10 iş gününü aşıyor; ikiye bölerek planlayın."; Hareket(a6, adim.Tarih.Value, "Yönetici Onayı — revizyona gönderildi (" + adim.Not + ")", adim.Kisi); }
            // Bugün izinde olanlar (takvim): Tolga Yaman yıllık, Esra Kaya raporlu (belge geldi), Gizem Tan raporlu (belge bekleniyor)
            var a7 = T("Tolga Yaman", "Tolga Yaman", "YILLIK", -2, 4, "Aile ziyareti", "Gizem Tan", 12, "Yok", "İzmir"); Onayla(a7); Pdks(a7, false);
            var a8 = T("Derya Şen", "Esra Kaya", "HASTALIK", -1, 2, "Grip, 4 gün istirahat", null, 1); Onayla(a8); a8.SgkRaporNo = "SGK-" + (400000 + a8.Id * 13); a8.BelgeAlindi = true; IslenenIslemler.Add("SGK-" + a8.Id); Hareket(a8, a8.Tarih.AddHours(6), $"SGK e-rapor bildirimi alındı ({a8.SgkRaporNo}); belge şartı karşılandı", "SGK"); Pdks(a8, false);
            var a9 = T("Gizem Tan", "Gizem Tan", "HASTALIK", -3, -1, "Diş operasyonu", null, 3); Onayla(a9); Pdks(a9);
            // Ücretsiz izin: direktör onayı bekliyor
            var a10 = T("Mert Doğan", "Mert Doğan", "UCRETSIZ", 20, 34, "Yurt dışı aile işleri", "Ertan Yavuz", 2); Onayla(a10, 1);
            // Babalık izni: belge bekleniyor, İK'da
            var a11 = T("Ertan Yavuz", "Ertan Yavuz", "BABALIK", -8, -2, "Doğum", "Yasin Sezgin", 9); Onayla(a11); Pdks(a11);
            // Departman doluluğu: Bilgi Teknolojileri'nde Kerem Aksoy aynı tarihlerde izinli → Ali Veli'nin a3'ü ile çakışan onaylı yönetici izni
            var a12 = T("Kerem Aksoy", "Kerem Aksoy", "YILLIK", 16, 18, "Kısa tatil", "Ali Veli", 6); Onayla(a12);
            // Erken dönüş örneği
            var a13 = T("Canan Su", "Canan Su", "YILLIK", -15, -9, "Tatil", null, 20); Onayla(a13); Pdks(a13, true, 3); Puantaj(a13);
            // Evlilik izni, gelecek hafta, İK onayı yok (yalnızca yönetici) → onaylı
            var a14 = T("Hülya Er", "Hülya Er", "EVLILIK", 9, 11, "Evlilik", null, 5); Onayla(a14); a14.BelgeAlindi = true;
            // Onay bekleyen: Yasin Sezgin'e gelen Ertan yıllık talebi
            var a15 = T("Ertan Yavuz", "Ertan Yavuz", "YILLIK", 30, 36, "Memleket ziyareti", "Yasin Sezgin", 1, "Yok", "Trabzon");
            // Süt izni (planlı, tamamlanmış)
            var a16 = T("Derya Şen", "Nazlı Erdem", "SUT", -12, -12, "Süt izni (yarım gün)", null, 14, "Öğleden Sonra"); Onayla(a16); Pdks(a16); Puantaj(a16);
        }
    }
}
