// ============================================================
// CARİ MUTABAKAT YÖNETİMİ — veri, kurallar ve süreç (Mali İşler › Muhasebe › Cari Mutabakat ve Mali İşler › Cari İşler › Mutabakat ve Kapama; aynı ekran)
// "Cari Mutabakat Yönetimi — Otomatik Mutabakat, İç Kontrol ve Eski Bakiye Risk Yönetimi — IT Geliştirme Çalışması v1.0 (29.09.2026)" uyarlaması.
// Amaç: muhasebenin tüm müşteri / tedarikçi / çift rollü carilerde mutabakatı hızlı, kontrollü ve olabildiğince otomatik yapması; gönderimden önce
// iletişim, bakiye ve eski açık kalem (değerleme riski) hatalarının yakalanması.
//   • Aktif / Pasif sekmeleri: son AktifPasifAy (varsayılan 12) ay içinde hareket olan cariler Aktif, olmayanlar Pasif (parametrik; cari kartın
//     ana statüsünü DEĞİŞTİRMEZ, yalnız raporlama / kontrol amaçlıdır).
//   • 1. ekran (Kontrol Merkezi): §3'teki 16 kolon; tüm alanlar cari karttan (Data/CariKartOrnek.cs) ve muhasebe hareketlerinden otomatik.
//   • Süreç: Muhasebe cariyi seçer → "Temsilciye Gönder" (iç yetkili otomatik: Müşteri → satış temsilcisi, Tedarikçi → satın alma sorumlusu,
//     Çift Rol → ikisi birden / ikili onay) → temsilci (A) iletişim bilgileri kontrolü (birden fazla kişi, ana mutabakat yetkilisi) ve
//     (B) bakiye kontrolü (zorunlu sorular, fark bildirimi) → sistem "Gönderime Hazır" yapar → Muhasebe tekil / toplu mutabakat gönderir →
//     yanıt Mutabık / İtirazlı → son mutabakat sonrası yeni işlem oluşursa "Yeniden Mutabakat Gerekli".
//   • Mutabakat Durumu elle seçilmez; DurumHesapla() gerçek verilere ve tamamlanan kontrollere göre üretir (§9).
//   • Bakiye yaşlandırması: açık bakiyeyi oluşturan EN ESKİ açık faturanın tarihi; eşikten eski açık bakiye → Eski Bakiye Uyarısı; yabancı para
//     bakiyesi eskiyse → Değerleme Riski; kapanış kontrolü çözülmeden cari Gönderime Hazır OLAMAZ (§8, §10 blokaj).
//   • Log: tüm iletişim güncellemeleri, bakiye onayları, fark bildirimleri, gönderimler ve durum geçişleri (önceki / yeni değer, kullanıcı, zaman) (§11).
// Hareketler örnektir (cari koduna göre deterministik üretilir; aktif / pasif ayrımı Data/CariKartOrnek.SonHareket ile tutarlıdır). Kayıtlar bellek içindedir.
// Ekran: Controllers/CariMutabakatController.cs + Views/CariMutabakat/Index.cshtml (+ Ekstre.cshtml, Mektup.cshtml yazdırılabilir) + wwwroot/js/cari-mutabakat.js
// Yazım kuralı: liste değerleri ve etiketler Title Case ("İç Kontrolde", "Gönderime Hazır", "Kontrol Bekliyor", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Cari ilgili kişi (§5): aynı cariye birden fazla kişi; yalnız biri ana mutabakat yetkilisi.</summary>
    public class MtKisi
    {
        public int Id { get; set; }
        public string AdSoyad { get; set; }
        public string Unvan { get; set; }
        public string Departman { get; set; }
        public string Eposta { get; set; }
        public string Telefon { get; set; }
        public string CepTelefonu { get; set; }
        public bool MutabakatYetkilisi { get; set; }
        public bool AnaYetkili { get; set; }
        public bool EpostaGonder { get; set; } = true;
        public bool? Dogru { get; set; }             // "Bilgiler Doğru mu?" (temsilci; her süreçte yeniden sorulur)
        public string Not { get; set; } = "";
    }

    /// <summary>Cari hareketi (ekstre satırı, §7).</summary>
    public class MtHareket
    {
        public int Id { get; set; }
        public DateTime Tarih { get; set; }
        public string ParaBirimi { get; set; }
        public string BelgeTipi { get; set; }        // Fatura | Tahsilat | Ödeme | Çek | Senet | Mahsup | Virman | İade Faturası | Kur Farkı
        public string BelgeNo { get; set; }
        public string Aciklama { get; set; }
        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
        public decimal Bakiye { get; set; }          // hareket sonrası bakiye (borç − alacak, kümülatif)
        public DateTime? Vade { get; set; }
        public string KapamaDurumu { get; set; }     // Açık | Kapalı | Kısmi Kapalı | — (faturalarda; çekte vade bekliyor)
        public decimal Kalan { get; set; }           // faturada kapanmamış kısım
    }

    /// <summary>Döviz bazında bakiye özeti (§7 tablo).</summary>
    public class MtDoviz
    {
        public string ParaBirimi { get; set; }
        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
        public decimal Net => Borc - Alacak;
        public decimal Kur { get; set; }
        public decimal TlKarsiligi => Math.Round(Net * Kur, 2);
        public int AcikFaturaAdet { get; set; }
        public decimal AcikFaturaTutar { get; set; }
        public DateTime? EnEskiAcikFatura { get; set; }
        public int? BakiyeYasi => EnEskiAcikFatura.HasValue ? (int?)(DateTime.Today - EnEskiAcikFatura.Value.Date).Days : null;
        public bool EskiBakiyeUyari => BakiyeYasi.HasValue && BakiyeYasi.Value > MutabakatOrnek.EskiBakiyeGun;
        public bool DegerlemeRiski => ParaBirimi != "TL" && BakiyeYasi.HasValue && BakiyeYasi.Value > MutabakatOrnek.DegerlemeRiskiGun;
    }

    public class MtBekleyen { public string Tur { get; set; } public DateTime Tarih { get; set; } public decimal Tutar { get; set; } public string ParaBirimi { get; set; } public string Aciklama { get; set; } }

    /// <summary>Temsilcinin bakiye kontrolü (§6): zorunlu sorular + fark bildirimi + onaylar (çift rolde ikili onay).</summary>
    public class MtKontrol
    {
        public bool BakiyeDogru { get; set; } public bool AcikFaturalar { get; set; } public bool BekleyenOdeme { get; set; } public bool CekSenet { get; set; } public bool DovizDogru { get; set; } public bool GonderimeUygun { get; set; }
        public bool FarkVar { get; set; } public string FarkAciklama { get; set; } = ""; public DateTime? FarkZaman { get; set; } public string FarkBildiren { get; set; }
        public bool FarkCozuldu { get; set; } public string FarkCozum { get; set; } = ""; public DateTime? FarkCozumZaman { get; set; } public string FarkCozen { get; set; }
        public List<(string Temsilci, DateTime Zaman)> Onaylar { get; set; } = new List<(string, DateTime)>();
        public bool SorularTamam => BakiyeDogru && AcikFaturalar && BekleyenOdeme && CekSenet && DovizDogru && GonderimeUygun;
    }

    /// <summary>Eski bakiye / değerleme riski kapanış kontrolü (§8) — muhasebe çözer.</summary>
    public class MtKapanis
    {
        public bool Cozuldu { get; set; } public string Secim { get; set; } public string Aciklama { get; set; } = ""; public DateTime? Zaman { get; set; } public string Cozen { get; set; }
    }

    /// <summary>Mutabakat gönderimi ve yanıtı (§10).</summary>
    public class MtGonderim
    {
        public int Id { get; set; }
        public int SurecNo { get; set; }
        public DateTime Tarih { get; set; }
        public DateTime BakiyeTarihi { get; set; }
        public string Kime { get; set; }             // ana yetkili ad · e-posta
        public string Kanal { get; set; } = "E-posta (PDF)";
        public string Ozet { get; set; }             // döviz bazında net bakiyeler
        public decimal TlKarsiligi { get; set; }
        public string Sonuc { get; set; } = "Bekliyor";   // Bekliyor | Mutabık | İtirazlı
        public DateTime? YanitTarihi { get; set; }
        public string YanitAciklama { get; set; } = "";
        public string Gonderen { get; set; }
        public int Tekrar { get; set; }              // tekrar gönderim sayısı
        public string BelgeNo { get; set; }          // MTB-2026-000123
    }

    public class MtLog { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Onceki { get; set; } public string Yeni { get; set; } public string Aciklama { get; set; } public string DurumGecisi { get; set; } }

    /// <summary>Bir carinin mutabakat dosyası: hareketler, döviz özetleri, kişiler, süreç ve günlük.</summary>
    public class MtCari
    {
        public string CariKodu { get; set; }
        public CariKart Kart => CariKartOrnek.KartBul(CariKodu);
        public List<MtHareket> Hareketler { get; set; } = new List<MtHareket>();
        public List<MtDoviz> Dovizler { get; set; } = new List<MtDoviz>();
        public List<MtBekleyen> Bekleyenler { get; set; } = new List<MtBekleyen>();
        public int MahsupVirmanBekleyen { get; set; }
        public List<MtKisi> Kisiler { get; set; } = new List<MtKisi>();
        public DateTime? BakiyeZamani { get; set; }  // bakiyenin hesaplandığı an (ekran / yenileme)
        // ---- süreç
        public bool SurecAcik { get; set; }
        public int SurecNo { get; set; }
        public DateTime? TemsilciyeGonderim { get; set; }
        public string TemsilciyeGonderen { get; set; }
        public DateTime? IletisimOnayZamani { get; set; } public string IletisimOnaylayan { get; set; }
        public MtKontrol Kontrol { get; set; } = new MtKontrol();
        public MtKapanis Kapanis { get; set; } = new MtKapanis();
        public List<MtGonderim> Gonderimler { get; set; } = new List<MtGonderim>();
        public List<MtLog> Log { get; set; } = new List<MtLog>();
        // ---- türetilen
        public DateTime? SonHareket => Hareketler.Count == 0 ? (DateTime?)null : Hareketler.Max(h => h.Tarih);
        public bool Aktif => (SonHareket ?? Kart?.OlusturmaTarihi ?? DateTime.MinValue) >= DateTime.Today.AddMonths(-MutabakatOrnek.AktifPasifAy);
        public decimal BakiyeTl => Dovizler.Sum(d => d.TlKarsiligi);
        public bool CokDoviz => Dovizler.Count(d => d.Net != 0) > 1;
        public DateTime? EnEskiAcikFatura => Dovizler.Where(d => d.EnEskiAcikFatura.HasValue).Select(d => d.EnEskiAcikFatura).Min();
        public int? BakiyeYasi => EnEskiAcikFatura.HasValue ? (int?)(DateTime.Today - EnEskiAcikFatura.Value.Date).Days : null;
        public bool EskiBakiyeUyari => Dovizler.Any(d => d.EskiBakiyeUyari);
        public bool DegerlemeRiski => Dovizler.Any(d => d.DegerlemeRiski);
        public bool KapanisGerekli => EskiBakiyeUyari || DegerlemeRiski;
        public DateTime? SonMutabakat => Gonderimler.Where(g => g.Sonuc == "Mutabık").Select(g => (DateTime?)(g.YanitTarihi ?? g.Tarih)).Max();
        public bool SonMutabakatSonrasiIslem { get { var son = Gonderimler.Where(g => g.Sonuc == "Mutabık").OrderByDescending(g => g.Tarih).FirstOrDefault(); return son != null && Hareketler.Any(h => h.Tarih.Date > son.BakiyeTarihi.Date); } }
        public string IletisimDurumu
        {
            get
            {
                var ana = Kisiler.FirstOrDefault(k => k.AnaYetkili);
                if (ana == null || string.IsNullOrWhiteSpace(ana.Eposta) || !Kisiler.Any(k => k.MutabakatYetkilisi)) return "Eksik";
                return IletisimOnayZamani.HasValue && ana.Dogru == true ? "Tamamlandı" : "Kontrol Bekliyor";
            }
        }
        public List<string> Temsilciler => MutabakatOrnek.Temsilciler(Kart);
        public bool TemsilciOnayiTamam => Temsilciler.All(t => Kontrol.Onaylar.Any(o => o.Temsilci == t));
        public bool GonderimeHazirMi => SurecAcik && IletisimDurumu == "Tamamlandı" && Kontrol.SorularTamam && TemsilciOnayiTamam && (!Kontrol.FarkVar || Kontrol.FarkCozuldu) && (!KapanisGerekli || Kapanis.Cozuldu);
        public string Durum => MutabakatOrnek.DurumHesapla(this);
        /// <summary>Gönderim öncesi blokaj kontrolleri (§10): (Kontrol, İzin).</summary>
        public List<(string Kontrol, bool Izin)> Blokajlar => new List<(string, bool)>
        {
            ("İletişim bilgileri tamam", IletisimDurumu == "Tamamlandı"), ("Ana mutabakat yetkilisi belirlenmiş", Kisiler.Any(k => k.AnaYetkili && !string.IsNullOrWhiteSpace(k.Eposta))),
            ("Temsilci kontrolü tamamlanmış", Kontrol.SorularTamam && TemsilciOnayiTamam), ("Eski bakiye kapanış kontrolü çözülmüş", !KapanisGerekli || Kapanis.Cozuldu),
            ("Bakiye farkı / düzeltme bildirimi yok", !Kontrol.FarkVar || Kontrol.FarkCozuldu)
        };
    }

    public class MtKisiGiris { public int Id { get; set; } public string AdSoyad { get; set; } public string Unvan { get; set; } public string Departman { get; set; } public string Eposta { get; set; } public string Telefon { get; set; } public string CepTelefonu { get; set; } public bool MutabakatYetkilisi { get; set; } public bool AnaYetkili { get; set; } public bool EpostaGonder { get; set; } public bool? Dogru { get; set; } public string Not { get; set; } }
    public class MtIstek { public string Kod { get; set; } public List<string> Kodlar { get; set; } public string Rol { get; set; } public string Temsilci { get; set; } public string Aciklama { get; set; } public string Secim { get; set; } public string Sonuc { get; set; } public List<MtKisiGiris> Kisiler { get; set; } public bool? BakiyeDogru { get; set; } public bool? AcikFaturalar { get; set; } public bool? BekleyenOdeme { get; set; } public bool? CekSenet { get; set; } public bool? DovizDogru { get; set; } public bool? GonderimeUygun { get; set; } public bool? FarkVar { get; set; } public int? AktifPasifAy { get; set; } public int? EskiBakiyeGun { get; set; } public int? DegerlemeRiskiGun { get; set; } }

    public static class MutabakatOrnek
    {
        public static readonly object Kilit = new object();
        // ---------- Parametreler (§2 not, §8) ----------
        public static int AktifPasifAy = 12;          // aktif / pasif sekme kuralı (ay)
        public static int EskiBakiyeGun = 180;        // eski bakiye uyarısı eşiği (gün)
        public static int DegerlemeRiskiGun = 365;    // yabancı para bakiyesinde değerleme riski eşiği (gün)
        public static readonly string[] Durumlar = { "Henüz Başlanmadı", "İç Kontrolde", "Kapanış Kontrolü Gerekli", "Gönderime Hazır", "Gönderildi", "Mutabık", "İtirazlı", "Yeniden Mutabakat Gerekli" };
        public static readonly string[] IletisimDurumlari = { "Eksik", "Kontrol Bekliyor", "Tamamlandı" };
        public static readonly string[] Roller = { "Muhasebe", "Satış Temsilcisi", "Satın Alma Sorumlusu", "Yönetici" };
        public static readonly string[] KapanisSecenekleri = { "Mahsup", "Tahsilat", "Ödeme", "Virman", "Ters Kayıt", "İnceleme" };
        public static readonly string[] Unvanlar = { "Muhasebe Müdürü", "Finans Uzmanı", "Muhasebe Uzmanı", "Satın Alma Müdürü", "Genel Müdür", "Mali İşler Direktörü", "Finans Müdürü" };
        public static readonly string[] Departmanlar = { "Muhasebe", "Finans", "Yönetim", "Satın Alma", "Satış" };
        /// <summary>§11 yetki matrisi.</summary>
        public static readonly Dictionary<string, string[]> Yetkiler = new Dictionary<string, string[]>
        {
            ["Muhasebe"] = new[] { "temsilciyeGonder", "mutabakatGonder", "kapanisCoz", "farkCoz", "yanitIsle", "itirazCoz", "parametre" },
            ["Satış Temsilcisi"] = new[] { "iletisim", "bakiyeOnay", "farkBildir" },
            ["Satın Alma Sorumlusu"] = new[] { "iletisim", "bakiyeOnay", "farkBildir" },
            ["Yönetici"] = new[] { "temsilciyeGonder", "mutabakatGonder", "kapanisCoz", "farkCoz", "yanitIsle", "itirazCoz", "parametre", "iletisim", "bakiyeOnay", "farkBildir" }
        };
        public static bool Yetkili(string rol, string e) => Yetkiler.TryGetValue(rol ?? "", out var l) && l.Contains(e);
        public static Dictionary<string, decimal> Kurlar => TeklifOrnek.Kurlar;
        public static DateTime KurTarihi => DateTime.Today;
        private static readonly HashSet<string> EurUlkeleri = new HashSet<string> { "Almanya", "İtalya", "İspanya", "Fransa", "Hollanda", "Belçika", "Polonya", "Romanya", "Bulgaristan", "Yunanistan", "Fas" };

        /// <summary>İç yetkili atama (§4): Müşteri → satış temsilcisi (ana faaliyet); Tedarikçi → satın alma sorumlusu (ERP şirketine göre); Çift Rol → ikisi; Diğer → kartta tanımlı yetkili.</summary>
        public static List<string> Temsilciler(CariKart k)
        {
            var l = new List<string>(); if (k == null) return l;
            string satis = k.Faaliyetler.Where(f => f.AnaFaaliyet).Select(f => f.SatisTemsilcisi).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? k.Faaliyetler.Select(f => f.SatisTemsilcisi).FirstOrDefault(x => !string.IsNullOrEmpty(x));
            string satinalma = AlimOnayOrnek.Satinalmacilar.FirstOrDefault(s => s.Sirket == k.Sirket).Ad ?? AlimOnayOrnek.Satinalmacilar[0].Ad;
            // kartta satış temsilcisi yoksa şirketin diğer kartlarındaki en yaygın temsilci (deterministik), o da yoksa ilk satış temsilcisi
            string yedek = satis ?? CariKartOrnek.Kartlar.Where(x => x.Sirket == k.Sirket).SelectMany(x => x.Faaliyetler).Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).GroupBy(x => x).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).Select(g => g.Key).FirstOrDefault() ?? CariKartOrnek.SatisTemsilcileri.Values.SelectMany(x => x).First();
            if (k.Rol == "Çift Rol") { if (satis != null) l.Add(satis); l.Add(satinalma); }
            else if (k.KartTipi == "Müşteri") l.Add(yedek);
            else if (k.KartTipi == "Tedarikçi") l.Add(satinalma);
            else l.Add(string.IsNullOrEmpty(k.Yetkili) ? "Muhasebe" : k.Yetkili);
            return l.Distinct().ToList();
        }
        public static string TemsilciRolu(string ad) => AlimOnayOrnek.Satinalmacilar.Any(s => s.Ad == ad) ? "Satın Alma Sorumlusu" : "Satış Temsilcisi";
        /// <summary>Tüm temsilciler (satış temsilcileri + satın alma sorumluları) — temsilci ekranı seçimi.</summary>
        public static List<string> TumTemsilciler() => CariKartOrnek.SatisTemsilcileri.Values.SelectMany(x => x).Concat(AlimOnayOrnek.Satinalmacilar.Select(s => s.Ad)).Distinct().OrderBy(x => x, StringComparer.Create(CariKartOrnek.Tr, false)).ToList();

        // ---------- Durum makinesi (§9) ----------
        public static string DurumHesapla(MtCari c)
        {
            var son = c.Gonderimler.OrderByDescending(g => g.Id).FirstOrDefault();
            if (c.SurecAcik)
            {
                if (son != null && son.SurecNo == c.SurecNo)
                {
                    if (son.Sonuc == "Bekliyor") return "Gönderildi";
                    if (son.Sonuc == "İtirazlı") return "İtirazlı";
                }
                if ((c.KapanisGerekli && !c.Kapanis.Cozuldu) || (c.Kontrol.FarkVar && !c.Kontrol.FarkCozuldu)) return "Kapanış Kontrolü Gerekli";
                return c.GonderimeHazirMi ? "Gönderime Hazır" : "İç Kontrolde";
            }
            if (son != null && son.Sonuc == "Mutabık") return c.SonMutabakatSonrasiIslem ? "Yeniden Mutabakat Gerekli" : "Mutabık";
            if (son != null && son.Sonuc == "İtirazlı") return "İtirazlı";
            return "Henüz Başlanmadı";
        }

        // ---------- Kayıtlar ----------
        private static readonly Dictionary<string, MtCari> _cariler = new Dictionary<string, MtCari>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> SenaryoKodlari = new HashSet<string> { "120.001", "120.002", "120.004", "120.007", "320.001", "120.008", "320.009" };
        private static bool _tohumlandi; private static int _gonderimSayac = 0; private static int _kisiSayac = 0;
        public static readonly List<MtLog> GenelLog = new List<MtLog>();
        public static List<MtCari> Cariler { get { Tohumla(); lock (Kilit) return _cariler.Values.OrderBy(c => c.CariKodu).ToList(); } }
        public static MtCari Bul(string kod) { Tohumla(); lock (Kilit) return _cariler.TryGetValue(kod ?? "", out var c) ? c : null; }
        private static int Seed(string s) => s.Aggregate(17, (a, ch) => unchecked(a * 31 + ch));
        private static void Log(MtCari c, string kullanici, string islem, string onceki, string yeni, string aciklama, string durumOnce)
        {
            var l = new MtLog { Zaman = DateTime.Now, Kullanici = kullanici, Islem = islem, Onceki = onceki ?? "", Yeni = yeni ?? "", Aciklama = aciklama ?? "", DurumGecisi = durumOnce == c.Durum ? "" : $"{durumOnce} → {c.Durum}" };
            c.Log.Insert(0, l); GenelLog.Insert(0, new MtLog { Zaman = l.Zaman, Kullanici = l.Kullanici, Islem = $"{c.CariKodu} {c.Kart?.KisaAd} · {islem}", Onceki = l.Onceki, Yeni = l.Yeni, Aciklama = l.Aciklama, DurumGecisi = l.DurumGecisi });
        }

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                foreach (var k in CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Müşteri" || x.KartTipi == "Tedarikçi")) _cariler[k.Kod] = Uret(k);
                Senaryolar();
            }
        }

        // ---------- Hareket üretimi (cari koduna göre deterministik) ----------
        private static MtCari Uret(CariKart k)
        {
            var rnd = new Random(Seed(k.Kod)); var bugun = DateTime.Today;
            var c = new MtCari { CariKodu = k.Kod, BakiyeZamani = DateTime.Now };
            bool musteri = k.KartTipi == "Müşteri";
            var sh = CariKartOrnek.SonHareket(k); bool aktif = sh.Tarih >= bugun.AddMonths(-AktifPasifAy);
            // para birimleri
            var pbler = new List<string>();
            string doviz = k.Bolge == "Yurtdışı" ? (EurUlkeleri.Contains(k.Ulke) ? "EUR" : "USD") : null;
            if (k.Bolge == "Yurtdışı") { pbler.Add(doviz); if (k.IliskiDurumu == "Grup İçi") pbler.Add("TL"); }
            else { pbler.Add("TL"); if (k.Kod == "320.008" || k.Kod == "320.002" || k.Kod == "120.001" || k.Kod == "120.003") { doviz = k.Kod == "320.008" ? "EUR" : "USD"; pbler.Add(doviz); } }
            decimal olcek = (k.Sinif ?? "").Contains("Distribütör") || (k.Sinif ?? "").Contains("Bayi") ? 2.2m : k.Sinif == "Özel Takip" ? 0.6m : 1m;
            // eski bakiye senaryosu: aktif carilerin deterministik bir azınlığında 7–16 ay önceki bir fatura kısmen açık kalır (şartname senaryo kodları hariç)
            bool eskiKalem = aktif && !SenaryoKodlari.Contains(k.Kod) && Math.Abs(Seed(k.Kod) % 4) == 0;
            int id = 0;
            void KismiAc(MtHareket f)
            {
                if (f == null || f.Kalan > 0.005m) return;
                f.Kalan = Math.Round((f.Borc + f.Alacak) * 0.35m, 2); f.KapamaDurumu = "Kısmi Kapalı";
                var ters = c.Hareketler.FirstOrDefault(x => x.BelgeTipi != "Fatura" && x.ParaBirimi == f.ParaBirimi && x.Tarih >= (f.Vade ?? f.Tarih) && (musteri ? x.Alacak : x.Borc) >= f.Kalan) ?? c.Hareketler.LastOrDefault(x => x.BelgeTipi != "Fatura" && x.ParaBirimi == f.ParaBirimi && x.Tarih >= f.Tarih);
                if (ters != null) { if (musteri) ters.Alacak = Math.Max(0, Math.Round(ters.Alacak - f.Kalan, 2)); else ters.Borc = Math.Max(0, Math.Round(ters.Borc - f.Kalan, 2)); ters.Aciklama += " (kısmi)"; }
            }
            string Belge(string tip, DateTime t) => tip == "Fatura" ? $"URS{t:yyyy}{rnd.Next(100000, 999999):000000}" : tip == "Tahsilat" ? $"TH-{t:yyyy}-{rnd.Next(1000, 9999)}" : tip == "Ödeme" ? $"BÖ-{t:yyyy}-{rnd.Next(10000, 99999)}" : tip == "Çek" ? $"ÇK-{rnd.Next(1000000, 9999999)}" : $"{tip.Substring(0, 2).ToUpperInvariant()}-{rnd.Next(1000, 9999)}";
            foreach (var pb in pbler)
            {
                bool tl = pb == "TL"; decimal taban = tl ? 90_000m : 6_000m, tavan = tl ? 620_000m : 42_000m;
                DateTime bas = aktif ? bugun.AddMonths(-18) : bugun.AddMonths(-30), bit = aktif ? bugun : bugun.AddMonths(-(AktifPasifAy + 1)).AddDays(-rnd.Next(5, 40));
                var faturalar = new List<MtHareket>();
                var ay = new DateTime(bas.Year, bas.Month, 1); int gecikme = rnd.Next(0, 41); // cariye özgü ödeme gecikmesi (gün)
                while (ay <= bit)
                {
                    int n = rnd.Next(1, 3);
                    for (int i = 0; i < n; i++)
                    {
                        var t = ay.AddDays(rnd.Next(0, 27)); if (t > bit || t < bas) continue;
                        decimal tutar = Math.Round((taban + (decimal)rnd.NextDouble() * (tavan - taban)) * olcek, 2);
                        var f = new MtHareket { Id = ++id, Tarih = t, ParaBirimi = pb, BelgeTipi = "Fatura", BelgeNo = Belge("Fatura", t), Aciklama = musteri ? "Satış faturası" : "Alış faturası", Vade = t.AddDays(new[] { 30, 45, 60, 90 }[rnd.Next(4)]), KapamaDurumu = "Açık", Kalan = tutar };
                        if (musteri) f.Borc = tutar; else f.Alacak = tutar;
                        c.Hareketler.Add(f); faturalar.Add(f);
                    }
                    // tahsilat / ödeme: vadesi (+ cariye özgü gecikme) gelen açık faturalar FIFO kapatılır; dörtte birinde kısmi ödeme, kalan sonraki ay kapanır
                    var acik = faturalar.Where(f => f.Kalan > 0 && (f.Vade ?? f.Tarih).AddDays(gecikme) <= ay.AddDays(27)).OrderBy(f => f.Tarih).ToList();
                    var t2 = ay.AddDays(rnd.Next(3, 28));
                    if (acik.Any() && t2 <= bit)
                    {
                        decimal toplam = 0; var kapanan = new List<string>();
                        for (int j = 0; j < acik.Count; j++) { var f = acik[j]; bool kismi = j == acik.Count - 1 && f.Kalan == f.Borc + f.Alacak && rnd.Next(4) == 0; decimal ode = kismi ? Math.Round(f.Kalan * 0.6m, 2) : f.Kalan; f.Kalan = Math.Round(f.Kalan - ode, 2); f.KapamaDurumu = f.Kalan <= 0.005m ? "Kapalı" : "Kısmi Kapalı"; toplam += ode; kapanan.Add(f.BelgeNo); }
                        bool cek = tl && musteri && rnd.Next(3) == 0;
                        var h = new MtHareket { Id = ++id, Tarih = t2, ParaBirimi = pb, BelgeTipi = cek ? "Çek" : (musteri ? "Tahsilat" : "Ödeme"), BelgeNo = Belge(cek ? "Çek" : (musteri ? "Tahsilat" : "Ödeme"), t2), Aciklama = (cek ? "Alınan çek · " : (musteri ? "Havale / EFT · " : "Banka ödemesi · ")) + string.Join(", ", kapanan), KapamaDurumu = "—" };
                        if (cek) { h.Vade = t2.AddDays(rnd.Next(45, 120)); h.KapamaDurumu = h.Vade > bugun ? "Vade Bekliyor" : "Tahsil Edildi"; }
                        if (musteri) h.Alacak = Math.Round(toplam, 2); else h.Borc = Math.Round(toplam, 2);
                        c.Hareketler.Add(h);
                    }
                    ay = ay.AddMonths(1);
                }
                // pasif carilerde eski açık kalem: son faturanın bir kısmı açık kalır (eski bakiye / değerleme riski senaryosu)
                if (!aktif && faturalar.Any()) KismiAc(faturalar.OrderByDescending(x => x.Tarih).First());
                // aktif azınlıkta 7–16 ay önceki kapanmış bir fatura kısmen açık kalır (eski bakiye uyarısı; dövizde değerleme riski)
                if (eskiKalem && pb == pbler[0]) KismiAc(faturalar.Where(x => x.Tarih <= bugun.AddMonths(-7) && x.Tarih >= bugun.AddMonths(-16) && x.Kalan <= 0.005m).OrderBy(x => x.Tarih).Skip(1).FirstOrDefault() ?? faturalar.FirstOrDefault(x => x.Kalan <= 0.005m));
            }
            // kur farkı satırı (döviz carilerinde dönem sonu)
            if (doviz != null && aktif && rnd.Next(2) == 0) c.Hareketler.Add(new MtHareket { Id = ++id, Tarih = new DateTime(bugun.Year, 1, 5), ParaBirimi = "TL", BelgeTipi = "Kur Farkı", BelgeNo = $"KF-{bugun.Year}-{rnd.Next(100, 999)}", Aciklama = "Dönem sonu kur değerlemesi", Borc = musteri ? Math.Round((decimal)rnd.Next(5_000, 60_000), 2) : 0, Alacak = musteri ? 0 : Math.Round((decimal)rnd.Next(5_000, 60_000), 2), KapamaDurumu = "—" });
            // bekleyen ödeme / tahsilat ve mahsup
            if (aktif) { int b = rnd.Next(0, 3); for (int i = 0; i < b; i++) c.Bekleyenler.Add(new MtBekleyen { Tur = musteri ? "Tahsilat" : "Ödeme", Tarih = bugun.AddDays(rnd.Next(2, 40)), Tutar = Math.Round((decimal)rnd.Next(20_000, 300_000) * olcek, 2), ParaBirimi = pbler[0], Aciklama = musteri ? "Planlanan tahsilat (vade)" : "Planlı ödeme" }); c.MahsupVirmanBekleyen = rnd.Next(6) == 0 ? 1 : 0; }
            Hesapla(c);
            // kişiler (§5): kart yetkilisi ilk kişi; bir kısmında ana yetkili seçilmemiş / e-posta eksik → "Eksik"
            var kisi = new MtKisi { Id = ++_kisiSayac, AdSoyad = string.IsNullOrEmpty(k.Yetkili) ? "" : k.Yetkili, Unvan = musteri ? "Muhasebe Müdürü" : "Satın Alma Müdürü", Departman = musteri ? "Muhasebe" : "Satın Alma", Eposta = k.Eposta ?? "", Telefon = k.Telefon ?? "", CepTelefonu = rnd.Next(2) == 0 ? $"05{rnd.Next(30, 55)} {rnd.Next(100, 999)} {rnd.Next(10, 99)} {rnd.Next(10, 99)}" : "", MutabakatYetkilisi = true, AnaYetkili = rnd.Next(10) < 7, EpostaGonder = true };
            c.Kisiler.Add(kisi);
            if (rnd.Next(2) == 0) c.Kisiler.Add(new MtKisi { Id = ++_kisiSayac, AdSoyad = new[] { "Ayşe Yılmaz", "Mehmet Kaya", "Elif Demir", "Can Öztürk", "Zeynep Aydın", "Murat Şahin" }[rnd.Next(6)], Unvan = "Finans Uzmanı", Departman = "Finans", Eposta = "finans@" + (k.Eposta ?? "firma.com").Split('@').Last(), Telefon = k.Telefon ?? "", MutabakatYetkilisi = rnd.Next(3) > 0, AnaYetkili = false, EpostaGonder = true });
            if (string.IsNullOrEmpty(kisi.AdSoyad)) c.Kisiler.Remove(kisi);
            // önceki tamamlanmış mutabakat (aktif carilerin bir kısmında; yakın tarihli olanlar Mutabık, sonrasında hareket olanlar Yeniden Mutabakat Gerekli)
            if (aktif && rnd.Next(10) < 4)
            {
                var t = bugun.AddDays(-rnd.Next(8, 240)).AddHours(rnd.Next(9, 17)).AddMinutes(rnd.Next(0, 60)); var ana = c.Kisiler.FirstOrDefault(x => x.AnaYetkili) ?? c.Kisiler.FirstOrDefault();
                // o tarihteki bakiye: gönderim tarihine kadarki hareketlerden döviz bazında hesaplanır
                var oTarih = c.Hareketler.Where(h => h.Tarih <= t).GroupBy(h => h.ParaBirimi).Select(g => (Pb: g.Key, Net: Math.Round(g.Sum(h => h.Borc - h.Alacak), 2))).ToList();
                decimal tlK = Math.Round(oTarih.Sum(x => x.Net * (Kurlar.TryGetValue(x.Pb, out var kr) ? kr : 1m)), 2);
                c.Gonderimler.Add(new MtGonderim { Id = ++_gonderimSayac, SurecNo = 0, Tarih = t, BakiyeTarihi = t, Kime = ana == null ? "—" : $"{ana.AdSoyad} · {ana.Eposta}", Ozet = oTarih.Any() ? string.Join(" · ", oTarih.Select(x => $"{x.Pb} {x.Net.ToString("N2", CariKartOrnek.Tr)}")) : "Önceki dönem mutabakatı", TlKarsiligi = tlK, Sonuc = "Mutabık", YanitTarihi = t.AddDays(rnd.Next(2, 9)), Gonderen = "Nazlı Erdem", BelgeNo = $"MTB-{t:yyyy}-{_gonderimSayac:000000}" });
            }
            return c;
        }

        /// <summary>Döviz özetleri ve kümülatif bakiyeleri yeniden hesaplar (hareketler değişince).</summary>
        private static void Hesapla(MtCari c)
        {
            c.Hareketler = c.Hareketler.OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList(); c.Dovizler.Clear();
            foreach (var pb in c.Hareketler.Select(h => h.ParaBirimi).Distinct().OrderBy(p => p == "TL" ? "" : p))
            {
                decimal bak = 0; var d = new MtDoviz { ParaBirimi = pb, Kur = Kurlar.TryGetValue(pb, out var kur) ? kur : 1m };
                foreach (var h in c.Hareketler.Where(x => x.ParaBirimi == pb)) { bak += h.Borc - h.Alacak; h.Bakiye = Math.Round(bak, 2); d.Borc += h.Borc; d.Alacak += h.Alacak; }
                var acik = c.Hareketler.Where(x => x.ParaBirimi == pb && x.BelgeTipi == "Fatura" && x.Kalan > 0.005m).ToList();
                d.AcikFaturaAdet = acik.Count; d.AcikFaturaTutar = Math.Round(acik.Sum(x => x.Kalan), 2); d.EnEskiAcikFatura = acik.Any() ? acik.Min(x => x.Tarih) : (DateTime?)null;
                d.Borc = Math.Round(d.Borc, 2); d.Alacak = Math.Round(d.Alacak, 2);
                c.Dovizler.Add(d);
            }
            c.BakiyeZamani = DateTime.Now;
        }

        /// <summary>Şartname senaryoları: süreç adımlarının her birinden örnek cari.</summary>
        private static void Senaryolar()
        {
            var bugun = DateTime.Today; const string muh = "Nazlı Erdem";
            MtCari C(string kod) => _cariler.TryGetValue(kod, out var c) ? c : null;
            // senaryo kayıtları hem cari günlüğüne hem genel günlüğe (Günlük sekmesi açılışta dolu olsun)
            void L(MtCari c, MtLog l) { c.Log.Insert(0, l); GenelLog.Add(new MtLog { Zaman = l.Zaman, Kullanici = l.Kullanici, Islem = $"{c.CariKodu} {c.Kart?.KisaAd} · {l.Islem}", Onceki = l.Onceki, Yeni = l.Yeni, Aciklama = l.Aciklama, DurumGecisi = l.DurumGecisi }); }
            void Ac(MtCari c, int gunOnce) { c.SurecAcik = true; c.SurecNo++; c.TemsilciyeGonderim = bugun.AddDays(-gunOnce).AddHours(10); c.TemsilciyeGonderen = muh; c.Kontrol = new MtKontrol(); c.Kapanis = new MtKapanis(); c.IletisimOnayZamani = null; foreach (var k in c.Kisiler) k.Dogru = null; L(c, new MtLog { Zaman = c.TemsilciyeGonderim.Value, Kullanici = muh, Islem = "Temsilciye Gönder", Onceki = "Henüz Başlanmadı", Yeni = "İç Kontrolde", Aciklama = "İç yetkili: " + string.Join(" + ", c.Temsilciler), DurumGecisi = "Henüz Başlanmadı → İç Kontrolde" }); }
            void IletisimTamam(MtCari c, int gunOnce) { var ana = c.Kisiler.FirstOrDefault(k => k.AnaYetkili) ?? c.Kisiler.FirstOrDefault(); if (ana != null) { ana.AnaYetkili = true; ana.MutabakatYetkilisi = true; if (string.IsNullOrEmpty(ana.Eposta)) ana.Eposta = "muhasebe@firma.com"; } foreach (var k in c.Kisiler) k.Dogru = true; c.IletisimOnayZamani = bugun.AddDays(-gunOnce).AddHours(11); c.IletisimOnaylayan = c.Temsilciler[0]; L(c, new MtLog { Zaman = c.IletisimOnayZamani.Value, Kullanici = c.IletisimOnaylayan, Islem = "İletişim Bilgileri Tamamlandı", Onceki = "Kontrol Bekliyor", Yeni = "Tamamlandı", Aciklama = "Ana yetkili: " + (ana?.AdSoyad ?? "—") + " · " + (ana?.Eposta ?? ""), DurumGecisi = "" }); }
            void BakiyeOnay(MtCari c, int gunOnce) { c.Kontrol.BakiyeDogru = c.Kontrol.AcikFaturalar = c.Kontrol.BekleyenOdeme = c.Kontrol.CekSenet = c.Kontrol.DovizDogru = c.Kontrol.GonderimeUygun = true; foreach (var t in c.Temsilciler) c.Kontrol.Onaylar.Add((t, bugun.AddDays(-gunOnce).AddHours(15))); L(c, new MtLog { Zaman = bugun.AddDays(-gunOnce).AddHours(15), Kullanici = c.Temsilciler[0], Islem = "Kontrolü Tamamla ve Onayla", Onceki = "İç Kontrolde", Yeni = c.Durum, Aciklama = "Tüm kontrol soruları uygun", DurumGecisi = "İç Kontrolde → " + c.Durum }); }
            void Kapanis(MtCari c, int gunOnce, string secim, string aciklama) { if (!c.KapanisGerekli) return; c.Kapanis.Cozuldu = true; c.Kapanis.Secim = secim; c.Kapanis.Aciklama = aciklama; c.Kapanis.Zaman = bugun.AddDays(-gunOnce).AddHours(12); c.Kapanis.Cozen = muh; L(c, new MtLog { Zaman = c.Kapanis.Zaman.Value, Kullanici = muh, Islem = "Kapanış Kontrolü Sonuçlandırıldı", Onceki = "Kapanış Kontrolü Gerekli", Yeni = secim, Aciklama = aciklama, DurumGecisi = "Kapanış Kontrolü Gerekli → İç Kontrolde" }); }
            void Gonder(MtCari c, int gunOnce, string sonuc, string aciklama)
            {
                var ana = c.Kisiler.FirstOrDefault(k => k.AnaYetkili); var t = bugun.AddDays(-gunOnce).AddHours(14);
                var g = new MtGonderim { Id = ++_gonderimSayac, SurecNo = c.SurecNo, Tarih = t, BakiyeTarihi = t, Kime = ana == null ? "—" : $"{ana.AdSoyad} · {ana.Eposta}", Ozet = string.Join(" · ", c.Dovizler.Select(d => $"{d.ParaBirimi} {d.Net.ToString("N2", CariKartOrnek.Tr)}")), TlKarsiligi = c.BakiyeTl, Sonuc = sonuc, Gonderen = muh, BelgeNo = $"MTB-{t:yyyy}-{_gonderimSayac:000000}" };
                if (sonuc != "Bekliyor") { g.YanitTarihi = t.AddDays(2); g.YanitAciklama = aciklama; }
                c.Gonderimler.Add(g); L(c, new MtLog { Zaman = t, Kullanici = muh, Islem = "Mutabakat Gönder", Onceki = "Gönderime Hazır", Yeni = "Gönderildi", Aciklama = $"{g.BelgeNo} → {g.Kime}", DurumGecisi = "Gönderime Hazır → Gönderildi" });
                if (sonuc == "Mutabık") { c.SurecAcik = false; L(c, new MtLog { Zaman = g.YanitTarihi.Value, Kullanici = muh, Islem = "Cari Yanıtı: Mutabık", Onceki = "Gönderildi", Yeni = "Mutabık", Aciklama = aciklama, DurumGecisi = "Gönderildi → Mutabık" }); }
                if (sonuc == "İtirazlı") L(c, new MtLog { Zaman = g.YanitTarihi.Value, Kullanici = muh, Islem = "Cari Yanıtı: İtirazlı", Onceki = "Gönderildi", Yeni = "İtirazlı", Aciklama = aciklama, DurumGecisi = "Gönderildi → İtirazlı" });
            }
            // 120.001 ABC Tekstil → İç Kontrolde (temsilci henüz kontrol etmedi)
            var c1 = C("120.001"); if (c1 != null) { Ac(c1, 2); }
            // 120.002 Marmara Plastik → Gönderime Hazır
            var c2 = C("120.002"); if (c2 != null) { Ac(c2, 6); IletisimTamam(c2, 5); Kapanis(c2, 5, "İnceleme", "Eski kalem incelendi, gerçek açık bakiye"); BakiyeOnay(c2, 4); }
            // 120.004 Karadeniz Ambalaj → Gönderildi (yanıt bekleniyor)
            var c4 = C("120.004"); if (c4 != null) { Ac(c4, 9); IletisimTamam(c4, 8); Kapanis(c4, 7, "Mahsup", "Eski açık kalem mahsup edildi"); BakiyeOnay(c4, 7); Gonder(c4, 3, "Bekliyor", ""); }
            // 120.007 Anadolu Emprime → Mutabık (ya da sonrasında işlem varsa Yeniden Mutabakat Gerekli)
            var c7 = C("120.007"); if (c7 != null) { Ac(c7, 30); IletisimTamam(c7, 29); Kapanis(c7, 28, "İnceleme", "İncelendi"); BakiyeOnay(c7, 28); Gonder(c7, 26, "Mutabık", "Bakiye kabul edildi."); }
            // 320.001 Petkim → İtirazlı
            var c5 = C("320.001"); if (c5 != null) { Ac(c5, 14); IletisimTamam(c5, 13); Kapanis(c5, 12, "İnceleme", "İncelendi"); BakiyeOnay(c5, 12); Gonder(c5, 9, "İtirazlı", "Tedarikçi kayıtlarında 1 fatura eksik görünüyor; fark 148.500,00 TL."); }
            // 120.008 TextilDruck → eski EUR açık fatura (değerleme riski) → Kapanış Kontrolü Gerekli
            var c8 = C("120.008"); if (c8 != null)
            {
                var t = bugun.AddDays(-DegerlemeRiskiGun - 255); int id = c8.Hareketler.Count + 1;
                c8.Hareketler.Add(new MtHareket { Id = id, Tarih = t, ParaBirimi = "EUR", BelgeTipi = "Fatura", BelgeNo = $"URS{t:yyyy}000418", Aciklama = "Satış faturası (kapanmamış eski kalem)", Borc = 4_850m, Vade = t.AddDays(60), KapamaDurumu = "Açık", Kalan = 4_850m });
                Hesapla(c8); Ac(c8, 4); IletisimTamam(c8, 3);
                L(c8, new MtLog { Zaman = bugun.AddDays(-4).AddHours(10).AddMinutes(1), Kullanici = "Sistem", Islem = "Değerleme Riski İşaretlendi", Onceki = "", Yeni = $"EUR 4.850,00 · {t:dd.MM.yyyy}", Aciklama = $"Yabancı para açık kalemi {DegerlemeRiskiGun} günden eski; kapanış kontrolü çözülmeden gönderime hazır olamaz", DurumGecisi = "İç Kontrolde → Kapanış Kontrolü Gerekli" });
            }
            // 320.009 Uras Kimya GmbH (T) pasif: eski EUR açık kalem 2023'ten (şartname örnek senaryosu)
            var c9 = C("320.009"); if (c9 != null)
            {
                var t = new DateTime(2023, 3, 15); int id = c9.Hareketler.Count + 1;
                c9.Hareketler.Add(new MtHareket { Id = id, Tarih = t, ParaBirimi = "EUR", BelgeTipi = "Fatura", BelgeNo = $"DE{t:yyyy}-0871", Aciklama = "Şube hizmet faturası (kapanmamış eski kalem)", Alacak = 25_000m, Vade = t.AddDays(30), KapamaDurumu = "Açık", Kalan = 25_000m });
                Hesapla(c9);
                L(c9, new MtLog { Zaman = bugun.AddDays(-1).AddHours(6), Kullanici = "Sistem", Islem = "Eski Bakiye · Değerleme Riski İşaretlendi", Onceki = "", Yeni = "EUR 25.000,00 · 15.03.2023", Aciklama = "Pasif cari; her dönem değerlemeye giren kapanmamış kalem — muhasebe kapanış kontrolü bekliyor", DurumGecisi = "" });
            }
            GenelLog.Sort((a, b) => b.Zaman.CompareTo(a.Zaman));
        }

        // =====================================================================
        // AKSİYONLAR (hepsi loglanır; durum yeniden hesaplanır)
        // =====================================================================
        private static string Kim(string kullanici) => string.IsNullOrEmpty(kullanici) ? "Sistem" : kullanici;

        /// <summary>§4 Muhasebe → iç yetkiliye gönder (tekil / toplu). Yeni süreç açar, kontrolleri sıfırlar.</summary>
        public static (int Basarili, List<string> Hatalar) TemsilciyeGonder(IEnumerable<string> kodlar, string rol, string kullanici)
        {
            var h = new List<string>(); int n = 0;
            if (!Yetkili(rol, "temsilciyeGonder")) { h.Add("Bu rol temsilciye gönderemez."); return (0, h); }
            lock (Kilit)
                foreach (var kod in kodlar ?? new List<string>())
                {
                    var c = Bul(kod); if (c == null) { h.Add($"{kod}: cari bulunamadı."); continue; }
                    var d = c.Durum;
                    if (c.SurecAcik && d != "İtirazlı") { h.Add($"{kod}: süreç zaten açık ({d})."); continue; }
                    c.SurecAcik = true; c.SurecNo++; c.TemsilciyeGonderim = DateTime.Now; c.TemsilciyeGonderen = Kim(kullanici); c.Kontrol = new MtKontrol(); c.Kapanis = new MtKapanis(); c.IletisimOnayZamani = null; c.IletisimOnaylayan = null; foreach (var k in c.Kisiler) k.Dogru = null;
                    Log(c, Kim(kullanici), "Temsilciye Gönder", d, c.Durum, "İç yetkili: " + string.Join(" + ", c.Temsilciler), d); n++;
                }
            return (n, h);
        }

        /// <summary>§5 İletişim bilgileri kaydı (temsilci): kişi satırları güncellenir; ana yetkili tek olmalı.</summary>
        public static string IletisimKaydet(MtIstek g, string kullanici)
        {
            if (!Yetkili(g?.Rol, "iletisim")) return "Bu rol iletişim bilgilerini düzenleyemez.";
            var c = Bul(g.Kod); if (c == null) return "Cari bulunamadı.";
            if (!c.SurecAcik) return "Cari kontrole gönderilmemiş (süreç açık değil).";
            var giris = (g.Kisiler ?? new List<MtKisiGiris>()).Where(k => !string.IsNullOrWhiteSpace(k.AdSoyad)).ToList();
            if (giris.Count(k => k.AnaYetkili) > 1) return "Yalnız bir ana mutabakat yetkilisi seçilebilir.";
            foreach (var k in giris) { if (k.EpostaGonder && (string.IsNullOrWhiteSpace(k.Eposta) || !k.Eposta.Contains("@"))) return $"{k.AdSoyad}: e-posta gönderilecekse geçerli e-posta zorunludur."; if (k.AnaYetkili && !k.MutabakatYetkilisi) return $"{k.AdSoyad}: ana yetkili aynı zamanda mutabakat yetkilisi olmalıdır."; }
            lock (Kilit)
            {
                var d = c.Durum; var onceki = string.Join(" | ", c.Kisiler.Select(k => $"{k.AdSoyad}<{k.Eposta}>{(k.AnaYetkili ? "*" : "")}"));
                var yeni = new List<MtKisi>();
                foreach (var k in giris) { var m = c.Kisiler.FirstOrDefault(x => x.Id == k.Id) ?? new MtKisi { Id = ++_kisiSayac }; m.AdSoyad = k.AdSoyad.Trim(); m.Unvan = k.Unvan ?? ""; m.Departman = k.Departman ?? ""; m.Eposta = (k.Eposta ?? "").Trim(); m.Telefon = k.Telefon ?? ""; m.CepTelefonu = k.CepTelefonu ?? ""; m.MutabakatYetkilisi = k.MutabakatYetkilisi; m.AnaYetkili = k.AnaYetkili; m.EpostaGonder = k.EpostaGonder; m.Dogru = k.Dogru; m.Not = k.Not ?? ""; yeni.Add(m); }
                c.Kisiler = yeni; c.IletisimOnayZamani = null;   // kişi bilgisi değişti → yeniden tamamlanmalı
                Log(c, Kim(kullanici), "İletişim Bilgileri Güncellendi", onceki, string.Join(" | ", c.Kisiler.Select(k => $"{k.AdSoyad}<{k.Eposta}>{(k.AnaYetkili ? "*" : "")}")), g.Aciklama, d);
            }
            return null;
        }

        /// <summary>§5 "İletişim Bilgileri Tamamlandı" — ana yetkili + e-posta + "Bilgiler Doğru" zorunlu.</summary>
        public static string IletisimTamamla(MtIstek g, string kullanici)
        {
            if (!Yetkili(g?.Rol, "iletisim")) return "Bu rol iletişim kontrolü yapamaz.";
            var c = Bul(g.Kod); if (c == null) return "Cari bulunamadı."; if (!c.SurecAcik) return "Süreç açık değil.";
            var ana = c.Kisiler.FirstOrDefault(k => k.AnaYetkili); if (ana == null) return "Ana mutabakat yetkilisi seçilmedi."; if (string.IsNullOrWhiteSpace(ana.Eposta) || !ana.Eposta.Contains("@")) return "Ana yetkilinin e-posta adresi eksik / geçersiz.";
            if (c.Kisiler.Any(k => k.Dogru != true)) return "Her kişi için \"Bilgiler Doğru mu?\" Evet olmalı.";
            lock (Kilit) { var d = c.Durum; c.IletisimOnayZamani = DateTime.Now; c.IletisimOnaylayan = Kim(kullanici); Log(c, Kim(kullanici), "İletişim Bilgileri Tamamlandı", "Kontrol Bekliyor", "Tamamlandı", "Ana yetkili: " + ana.AdSoyad + " · " + ana.Eposta, d); }
            return null;
        }

        /// <summary>§6 Bakiye kontrolü onayı ("Kontrolü Tamamla ve Onayla") — iletişim tamamlanmadan onaylanamaz; fark varsa açıklama zorunlu.</summary>
        public static string BakiyeOnayla(MtIstek g, string kullanici)
        {
            if (!Yetkili(g?.Rol, "bakiyeOnay")) return "Bu rol bakiye kontrolü yapamaz.";
            var c = Bul(g.Kod); if (c == null) return "Cari bulunamadı."; if (!c.SurecAcik) return "Süreç açık değil.";
            if (c.IletisimDurumu != "Tamamlandı") return "İletişim bilgileri tamamlanmadan bakiye kontrolü nihai olarak onaylanamaz.";
            string temsilci = string.IsNullOrEmpty(g.Temsilci) ? Kim(kullanici) : g.Temsilci;
            if (!c.Temsilciler.Contains(temsilci) && g.Rol != "Yönetici") return $"{temsilci} bu carinin atanmış iç yetkilisi değil ({string.Join(" + ", c.Temsilciler)}).";
            lock (Kilit)
            {
                var d = c.Durum; var k = c.Kontrol;
                k.BakiyeDogru = g.BakiyeDogru == true; k.AcikFaturalar = g.AcikFaturalar == true; k.BekleyenOdeme = g.BekleyenOdeme == true; k.CekSenet = g.CekSenet == true; k.DovizDogru = g.DovizDogru == true; k.GonderimeUygun = g.GonderimeUygun == true;
                if (g.FarkVar == true)
                {
                    if (string.IsNullOrWhiteSpace(g.Aciklama)) return "Fark / düzeltme bildiriminde açıklama zorunludur.";
                    k.FarkVar = true; k.FarkAciklama = g.Aciklama.Trim(); k.FarkZaman = DateTime.Now; k.FarkBildiren = temsilci; k.FarkCozuldu = false;
                    Log(c, temsilci, "Fark / Düzeltme Bildir", "", k.FarkAciklama, "Fark çözülmeden cari Gönderime Hazır olamaz", d); return null;
                }
                if (!k.SorularTamam) return "Tüm zorunlu kontrol soruları Evet olmadan onay verilemez (fark varsa \"Fark / Düzeltme Bildir\" kullanın).";
                if (!k.Onaylar.Any(o => o.Temsilci == temsilci)) k.Onaylar.Add((temsilci, DateTime.Now));
                Log(c, temsilci, "Kontrolü Tamamla ve Onayla", d, c.Durum, c.TemsilciOnayiTamam ? "Tüm kontrol soruları uygun" : $"İkili onay: {k.Onaylar.Count}/{c.Temsilciler.Count} onay", d);
            }
            return null;
        }

        /// <summary>Muhasebe: temsilcinin bildirdiği farkı çözer.</summary>
        public static string FarkCoz(MtIstek g, string kullanici)
        {
            if (!Yetkili(g?.Rol, "farkCoz")) return "Bu rol fark çözemez.";
            var c = Bul(g.Kod); if (c == null) return "Cari bulunamadı."; if (!c.Kontrol.FarkVar || c.Kontrol.FarkCozuldu) return "Açık fark bildirimi yok.";
            if (string.IsNullOrWhiteSpace(g.Aciklama)) return "Çözüm açıklaması zorunludur.";
            lock (Kilit) { var d = c.Durum; c.Kontrol.FarkCozuldu = true; c.Kontrol.FarkCozum = g.Aciklama.Trim(); c.Kontrol.FarkCozumZaman = DateTime.Now; c.Kontrol.FarkCozen = Kim(kullanici); Log(c, Kim(kullanici), "Fark Çözüldü", c.Kontrol.FarkAciklama, c.Kontrol.FarkCozum, "", d); }
            return null;
        }

        /// <summary>§8 Kapanış kontrolü (muhasebe): seçim + zorunlu açıklama.</summary>
        public static string KapanisCoz(MtIstek g, string kullanici)
        {
            if (!Yetkili(g?.Rol, "kapanisCoz")) return "Bu rol kapanış kontrolü sonuçlandıramaz.";
            var c = Bul(g.Kod); if (c == null) return "Cari bulunamadı."; if (!c.KapanisGerekli) return "Bu caride eski bakiye / değerleme riski yok.";
            if (string.IsNullOrEmpty(g.Secim) || !KapanisSecenekleri.Contains(g.Secim)) return "Kapanış seçimi zorunludur (" + string.Join(" / ", KapanisSecenekleri) + ").";
            if (string.IsNullOrWhiteSpace(g.Aciklama)) return "Kapanış açıklaması zorunludur.";
            lock (Kilit) { var d = c.Durum; c.Kapanis.Cozuldu = true; c.Kapanis.Secim = g.Secim; c.Kapanis.Aciklama = g.Aciklama.Trim(); c.Kapanis.Zaman = DateTime.Now; c.Kapanis.Cozen = Kim(kullanici); Log(c, Kim(kullanici), "Kapanış Kontrolü Sonuçlandırıldı", "Kapanış Kontrolü Gerekli", g.Secim, c.Kapanis.Aciklama, d); }
            return null;
        }

        /// <summary>§10 Muhasebe: tekil / toplu mutabakat gönderimi — blokaj kontrolleri geçen carilere; ana yetkilinin e-postasına.</summary>
        public static (int Basarili, List<string> Hatalar) MutabakatGonder(IEnumerable<string> kodlar, string rol, string kullanici, bool tekrar = false)
        {
            var h = new List<string>(); int n = 0;
            if (!Yetkili(rol, "mutabakatGonder")) { h.Add("Bu rol mutabakat gönderemez."); return (0, h); }
            lock (Kilit)
                foreach (var kod in kodlar ?? new List<string>())
                {
                    var c = Bul(kod); if (c == null) { h.Add($"{kod}: cari bulunamadı."); continue; }
                    var d = c.Durum;
                    if (tekrar) { var sonG = c.Gonderimler.OrderByDescending(x => x.Id).FirstOrDefault(); if (sonG == null || sonG.Sonuc != "Bekliyor") { h.Add($"{kod}: tekrar gönderilecek bekleyen mutabakat yok."); continue; } sonG.Tekrar++; sonG.Tarih = DateTime.Now; Log(c, Kim(kullanici), "Tekrar Gönder", "", sonG.BelgeNo, $"{sonG.Tekrar}. tekrar → {sonG.Kime}", d); n++; continue; }
                    if (d != "Gönderime Hazır") { var b = c.Blokajlar.Where(x => !x.Izin).Select(x => x.Kontrol); h.Add($"{kod} {c.Kart?.KisaAd}: {d}" + (b.Any() ? " (eksik: " + string.Join("; ", b) + ")" : d == "Yeniden Mutabakat Gerekli" || d == "Henüz Başlanmadı" || d == "Mutabık" ? " (önce Temsilciye Gönder)" : "")); continue; }
                    var ana = c.Kisiler.First(k => k.AnaYetkili); var t = DateTime.Now; c.BakiyeZamani = t;
                    var g = new MtGonderim { Id = ++_gonderimSayac, SurecNo = c.SurecNo, Tarih = t, BakiyeTarihi = t, Kime = $"{ana.AdSoyad} · {ana.Eposta}", Kanal = ana.EpostaGonder ? "E-posta (PDF)" : "Elden / Kargo (PDF)", Ozet = string.Join(" · ", c.Dovizler.Select(x => $"{x.ParaBirimi} {x.Net.ToString("N2", CariKartOrnek.Tr)}")), TlKarsiligi = c.BakiyeTl, Sonuc = "Bekliyor", Gonderen = Kim(kullanici), BelgeNo = $"MTB-{t:yyyy}-{_gonderimSayac:000000}" };
                    c.Gonderimler.Add(g); Log(c, Kim(kullanici), "Mutabakat Gönder", d, c.Durum, $"{g.BelgeNo} → {g.Kime} · {g.Ozet}", d); n++;
                }
            return (n, h);
        }

        /// <summary>§10 Cari yanıtını işleme: Mutabık (süreç kapanır, son mutabakat tarihi) ya da İtirazlı (fark açıklaması zorunlu).</summary>
        public static string YanitIsle(MtIstek g, string kullanici)
        {
            if (!Yetkili(g?.Rol, "yanitIsle")) return "Bu rol cari yanıtı işleyemez.";
            var c = Bul(g.Kod); if (c == null) return "Cari bulunamadı.";
            var son = c.Gonderimler.OrderByDescending(x => x.Id).FirstOrDefault(); if (son == null || son.Sonuc != "Bekliyor") return "Yanıt bekleyen gönderim yok.";
            if (g.Sonuc != "Mutabık" && g.Sonuc != "İtirazlı") return "Sonuç Mutabık ya da İtirazlı olmalı.";
            if (g.Sonuc == "İtirazlı" && string.IsNullOrWhiteSpace(g.Aciklama)) return "İtirazda fark detayı / açıklama zorunludur.";
            lock (Kilit)
            {
                var d = c.Durum; son.Sonuc = g.Sonuc; son.YanitTarihi = DateTime.Now; son.YanitAciklama = (g.Aciklama ?? "").Trim();
                if (g.Sonuc == "Mutabık") c.SurecAcik = false;
                Log(c, Kim(kullanici), "Cari Yanıtı: " + g.Sonuc, "Gönderildi", g.Sonuc, son.YanitAciklama, d);
            }
            return null;
        }

        /// <summary>İtirazı çöz: fark kaydedilir, süreç yeniden iç kontrole açılır (temsilci yeniden kontrol eder, mutabakat tekrar gönderilir).</summary>
        public static string ItirazCoz(MtIstek g, string kullanici)
        {
            if (!Yetkili(g?.Rol, "itirazCoz")) return "Bu rol itiraz çözemez.";
            var c = Bul(g.Kod); if (c == null) return "Cari bulunamadı."; if (c.Durum != "İtirazlı") return "Cari itirazlı durumda değil.";
            if (string.IsNullOrWhiteSpace(g.Aciklama)) return "Çözüm açıklaması zorunludur (yapılan düzeltme / mutabık kalınan fark).";
            lock (Kilit)
            {
                var d = c.Durum; var son = c.Gonderimler.OrderByDescending(x => x.Id).First(); son.YanitAciklama += " · Çözüm: " + g.Aciklama.Trim();
                c.SurecAcik = true; c.SurecNo++; c.TemsilciyeGonderim = DateTime.Now; c.TemsilciyeGonderen = Kim(kullanici); c.Kontrol = new MtKontrol(); c.Kapanis = new MtKapanis(); c.IletisimOnayZamani = null; foreach (var k in c.Kisiler) k.Dogru = null;
                Log(c, Kim(kullanici), "İtiraz Çözüldü · Yeniden Kontrole Gönderildi", "İtirazlı", c.Durum, g.Aciklama.Trim(), d);
            }
            return null;
        }

        public static string ParametreKaydet(MtIstek g, string kullanici)
        {
            if (!Yetkili(g?.Rol, "parametre")) return "Parametreleri yalnız Muhasebe / Yönetici değiştirir.";
            if (g.AktifPasifAy.HasValue && (g.AktifPasifAy < 1 || g.AktifPasifAy > 60)) return "Aktif / pasif süresi 1–60 ay olmalı.";
            if (g.EskiBakiyeGun.HasValue && (g.EskiBakiyeGun < 30 || g.EskiBakiyeGun > 3650)) return "Eski bakiye eşiği 30–3650 gün olmalı.";
            if (g.DegerlemeRiskiGun.HasValue && (g.DegerlemeRiskiGun < 30 || g.DegerlemeRiskiGun > 3650)) return "Değerleme riski eşiği 30–3650 gün olmalı.";
            lock (Kilit)
            {
                string onceki = $"{AktifPasifAy} ay / {EskiBakiyeGun} gün / {DegerlemeRiskiGun} gün";
                if (g.AktifPasifAy.HasValue) AktifPasifAy = g.AktifPasifAy.Value; if (g.EskiBakiyeGun.HasValue) EskiBakiyeGun = g.EskiBakiyeGun.Value; if (g.DegerlemeRiskiGun.HasValue) DegerlemeRiskiGun = g.DegerlemeRiskiGun.Value;
                GenelLog.Insert(0, new MtLog { Zaman = DateTime.Now, Kullanici = Kim(kullanici), Islem = "Parametre Değişikliği", Onceki = onceki, Yeni = $"{AktifPasifAy} ay / {EskiBakiyeGun} gün / {DegerlemeRiskiGun} gün", Aciklama = "Aktif-pasif süresi / eski bakiye eşiği / değerleme riski eşiği", DurumGecisi = "" });
            }
            return null;
        }

        /// <summary>Bakiyeleri yeniler (bakiye zamanı = şimdi; kurlar güncel).</summary>
        public static void Yenile(MtCari c) { lock (Kilit) Hesapla(c); }
    }
}
