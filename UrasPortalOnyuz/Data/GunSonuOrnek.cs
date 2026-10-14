using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>
    /// GÜN SONU MUTABAKATI (Konsolide Finansal Takip Paneli › Bakiyeler · Gün Sonu) — bellek içi örnek.
    /// Akış (şirket × gün):
    ///   1) Gün İçi İşlemler Onayı — Ahmet Özkan (Finans Sorumlusu): günün kasa / banka / çek hareketlerini onaylar (onaysız satırlar toplu onaylanır, eksik / kontrol işaretliler hariç);
    ///      onayla birlikte kalan bakiyeler kesinleşir (anlık görüntü alınır).
    ///   2) Kalan Bakiyeler — otomatik: kasa (nakit), banka hesapları (Finekra günü hareketleri), çek ve senet portföyü şirket şirket X ve R işlem tipine göre adet · tutar;
    ///      açılış = kalan − gün içi giriş + gün içi çıkış.
    ///   3) Fiziksel Sayım — Berk Berberoğlu (Kasa Sorumlusu): gün sonunda fiziksel çek / senetleri (ve kasayı) sayar; sistemle birebir eşleşiyorsa kalan bakiyeleri onaylar,
    ///      eksik ya da fazla belge varsa buradan bildirir (tür, eksik / fazla, adet, tutar, belge no'ları); bildirim çözülünce yeniden sayım yapılır.
    /// Kaynaklar: Data/CekAkibetOrnek.cs (portföy, giriş / ibraz tarihleri), Data/NakitAkisOrnek.cs (kasa / banka bakiyeleri), Data/FinekraOrnek.cs (banka günü hareketleri, hareket onayları).
    /// </summary>
    public class GsBakiye
    {
        public string Grup { get; set; }          // Kasa | Banka | Çek | Senet
        public string Tur { get; set; }           // Kasa (Nakit) | Ziraat Bankası | Çek X | Çek R | Senet X | Senet R
        public string Kod { get; set; }           // 100.01.001 | 102.01.001 | 101 | 121
        public string PB { get; set; } = "TRY";
        public int AcilisAdet { get; set; } public decimal Acilis { get; set; }
        public int GirisAdet { get; set; } public decimal Giris { get; set; }
        public int CikisAdet { get; set; } public decimal Cikis { get; set; }
        public int Adet { get; set; } public decimal Tutar { get; set; }   // gün sonu kalan
        public bool Sayilir { get; set; }         // fiziksel sayım yapılır (çek / senet / kasa nakit)
        public string Not { get; set; } = "";
    }
    public class GsSayim { public string Tur { get; set; } public int? Adet { get; set; } public decimal? Tutar { get; set; } }
    public class GsBildirim
    {
        public int Id { get; set; } public DateTime Tarih { get; set; } public string Kullanici { get; set; }
        public string DbName { get; set; } public string Sirket { get; set; } public DateTime Gun { get; set; }
        public string Tur { get; set; } public string Yon { get; set; }           // Eksik | Fazla
        public int Adet { get; set; } public decimal Tutar { get; set; }
        public int SistemAdet { get; set; } public decimal SistemTutar { get; set; } public int SayilanAdet { get; set; } public decimal SayilanTutar { get; set; }
        public string BelgeNolar { get; set; } = ""; public string Aciklama { get; set; } = "";
        public string Durum { get; set; } = "Açık";                                   // Açık | Çözüldü
        public string Cozum { get; set; } = ""; public string Cozen { get; set; } = ""; public DateTime? CozumTarihi { get; set; }
    }
    public class GunSonu
    {
        public string DbName { get; set; } public string Sirket { get; set; } public DateTime Gun { get; set; }
        // 1) gün içi işlemler onayı
        public string GunIciDurum { get; set; } = "Bekliyor"; public string GunIciOnaylayan { get; set; } = ""; public DateTime? GunIciOnayTarihi { get; set; } public string GunIciNot { get; set; } = "";
        public int GunIciHareket { get; set; } public int GunIciOnaylanan { get; set; } public int GunIciEksik { get; set; } public int FinekraBekleyen { get; set; }
        // 2) kalan bakiyeler (onayda anlık görüntü; onay öncesi canlı hesaplanır)
        public List<GsBakiye> Bakiyeler { get; set; } = new List<GsBakiye>();
        // 3) fiziksel sayım
        public string SayimDurumu { get; set; } = "Bekliyor"; public string SayimYapan { get; set; } = ""; public DateTime? SayimTarihi { get; set; } public string SayimNot { get; set; } = "";
        public List<GsSayim> Sayim { get; set; } = new List<GsSayim>();
        public string Kaydeden { get; set; } = "";
        public List<TalimatGunluk> Gunluk { get; set; } = new List<TalimatGunluk>();
    }

    public static class GunSonuOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly Dictionary<string, GunSonu> _kayitlar = new Dictionary<string, GunSonu>();
        static readonly List<GsBildirim> _bildirimler = new List<GsBildirim>();
        static int _bildirimSayac;
        /// <summary>Gün içi işlemleri onaylayan finans sorumlusu ve fiziksel sayımı yapan kasa sorumlusu (Data/NakitAvansOrnek.cs personel listesi, OrnekVeri kullanıcıları).</summary>
        public static readonly (string Ad, string Unvan, string Kod) GunIciSorumlu = ("Ahmet Özkan", "Finans Sorumlusu", "aoz");
        public static readonly (string Ad, string Unvan, string Kod) KasaSorumlu = ("Berk Berberoğlu", "Kasa Sorumlusu", "bbb");
        public static readonly string[] SayimTurleri = { "Çek X", "Çek R", "Senet X", "Senet R", "Kasa (Nakit)" };
        static string Anahtar(string dbName, DateTime gun) => dbName + "|" + gun.ToString("yyyy-MM-dd");
        static void Log(GunSonu g, string islem, string aciklama, string kullanici) => g.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });

        public static GunSonu Bul(string dbName, DateTime gun) { lock (Kilit) return _kayitlar.TryGetValue(Anahtar(dbName, gun.Date), out var g) ? g : null; }
        public static GunSonu Getir(string dbName, DateTime gun)
        {
            lock (Kilit)
            {
                if (_kayitlar.TryGetValue(Anahtar(dbName, gun.Date), out var g)) return g;
                g = new GunSonu { DbName = dbName, Sirket = CekAkibetOrnek.SirketAdi(dbName), Gun = gun.Date }; _kayitlar[Anahtar(dbName, gun.Date)] = g; return g;
            }
        }

        /// <summary>Kalan bakiyeler: kasa (SAP kasa bakiyesi; gün içi nakit giriş / çıkış dışarıdan), banka hesapları (Finekra günü hareketleri), çek / senet portföyü X · R (gün sonu itibarıyla portföyde olan belgeler; o gün giren / çıkanlar).</summary>
        public static List<GsBakiye> Hesapla(string dbName, DateTime gun, int kasaGirisAdet, decimal kasaGiris, int kasaCikisAdet, decimal kasaCikis)
        {
            gun = gun.Date; string sirket = CekAkibetOrnek.SirketAdi(dbName); var l = new List<GsBakiye>();
            var bak = NakitAkisOrnek.Bakiyeler(dbName);
            foreach (var b in bak.Where(b => b.BankaKasa == "KASA"))
                l.Add(new GsBakiye { Grup = "Kasa", Tur = "Kasa (Nakit)", Kod = "100.01.001", PB = b.PB, Tutar = b.TutarTL, GirisAdet = kasaGirisAdet, Giris = kasaGiris, CikisAdet = kasaCikisAdet, Cikis = kasaCikis, Acilis = b.TutarTL - kasaGiris + kasaCikis, Sayilir = true, Not = b.Detay ?? "" });
            var fnk = FinekraOrnek.Listele(dbName, gun, gun); int i = 0;
            foreach (var b in bak.Where(b => b.BankaKasa == "BANKA"))
            {
                i++; string kod = "102.01." + i.ToString("000"); var hs = fnk.Where(h => h.HesapKodu == kod).ToList();
                decimal gi = hs.Where(h => h.Giris).Sum(h => h.Tutar), ci = hs.Where(h => !h.Giris).Sum(h => h.Tutar);
                l.Add(new GsBakiye { Grup = "Banka", Tur = (b.Detay ?? "Banka").Split(" · ")[0], Kod = kod, PB = b.PB, Tutar = b.TutarTL, GirisAdet = hs.Count(h => h.Giris), Giris = gi, CikisAdet = hs.Count(h => !h.Giris), Cikis = ci, Acilis = b.TutarTL - gi + ci, Sayilir = false, Not = b.Detay ?? "" });
            }
            var kayitlar = CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == sirket && k.IslemTuru != "İade Edildi" && k.IslemTuru != "İptal Edildi").ToList();
            // gün sonu itibarıyla portföyde: bugün / ileri gün için mevcut akıbet esas (Portföyde / Planlandı); geçmiş gün için o günden sonra çıkmış belgeler de o gün portföydeydi
            bool GunSonuPortfoyde(CekKaydi k) => k.PortfoyGiris.Date <= gun && (k.Portfoyde || (gun < DateTime.Today && k.IbrazTarihi != null && k.IbrazTarihi.Value.Date > gun));
            foreach (var belge in new[] { "Çek", "Senet" })
                foreach (var tip in new[] { "X", "R" })
                {
                    var kl = kayitlar.Where(k => k.Belge == belge && k.IslemTipi == tip).ToList();
                    var port = kl.Where(GunSonuPortfoyde).ToList(); var giris = kl.Where(k => k.PortfoyGiris.Date == gun).ToList(); var cikis = kl.Where(k => k.IbrazTarihi != null && k.IbrazTarihi.Value.Date == gun).ToList();
                    l.Add(new GsBakiye
                    {
                        Grup = belge, Tur = $"{belge} {tip}", Kod = belge == "Çek" ? "101" : "121", PB = "TRY", Adet = port.Count, Tutar = port.Sum(k => k.Tutar),
                        GirisAdet = giris.Count, Giris = giris.Sum(k => k.Tutar), CikisAdet = cikis.Count, Cikis = cikis.Sum(k => k.Tutar),
                        AcilisAdet = port.Count - giris.Count + cikis.Count, Acilis = port.Sum(k => k.Tutar) - giris.Sum(k => k.Tutar) + cikis.Sum(k => k.Tutar), Sayilir = true,
                        Not = port.Count == 0 ? "" : $"{port.Count(k => k.IslemTuru == "Planlandı")} planlı · {port.Count(k => k.KullanimDurumu == "Kullanılabilir")} kullanılabilir"
                    });
                }
            return l;
        }
        /// <summary>Sayım yardımcısı: gün sonu itibarıyla portföydeki belgeler (tür bazında) — Berk fiziksel belgeleri bu listeyle karşılaştırır.</summary>
        public static List<object> Portfoy(string dbName, DateTime gun, string tur)
        {
            gun = gun.Date; string sirket = CekAkibetOrnek.SirketAdi(dbName); var p = (tur ?? "").Split(' '); if (p.Length < 2) return new List<object>();
            // gün sonu itibarıyla portföyde: bugün / ileri gün için mevcut akıbet esas (Portföyde / Planlandı); geçmiş gün için o günden sonra çıkmış belgeler de o gün portföydeydi
            bool GunSonuPortfoyde(CekKaydi k) => k.PortfoyGiris.Date <= gun && (k.Portfoyde || (gun < DateTime.Today && k.IbrazTarihi != null && k.IbrazTarihi.Value.Date > gun));
            return CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == sirket && k.Belge == p[0] && k.IslemTipi == p[1] && k.IslemTuru != "İade Edildi" && k.IslemTuru != "İptal Edildi" && GunSonuPortfoyde(k)).OrderBy(k => k.Vade)
                .Select(k => (object)new { docEntry = k.DocEntry, no = k.No, banka = k.Banka, musteri = k.Musteri, asilBorclu = k.AsilBorclu, vade = k.Vade.ToString("yyyy-MM-dd"), tutar = k.Tutar, islemTuru = k.IslemTuru, kullanim = k.KullanimDurumu, planlanan = k.PlanlananTedarikci, girisGunu = k.PortfoyGiris.Date == gun }).ToList();
        }

        /// <summary>1) Gün içi işlemler onayı (Ahmet Özkan). Onayla birlikte kalan bakiyelerin anlık görüntüsü alınır.</summary>
        public static (bool ok, string mesaj, GunSonu g) GunIciOnayla(string dbName, DateTime gun, string onaylayan, string not, string kullanici, int hareket, int onaylanan, int eksik, int finekraBekleyen, List<GsBakiye> bakiyeler)
        {
            if (!string.Equals((onaylayan ?? "").Trim(), GunIciSorumlu.Ad, StringComparison.OrdinalIgnoreCase)) return (false, $"Gün içi işlemleri yalnız {GunIciSorumlu.Ad} ({GunIciSorumlu.Unvan}) onaylar.", null);
            if (gun.Date > DateTime.Today) return (false, "İleri tarih için gün sonu onayı verilemez.", null);
            var g = Getir(dbName, gun);
            lock (Kilit)
            {
                if (g.GunIciDurum == "Onaylandı") return (false, $"{g.Sirket} {g.Gun:dd.MM.yyyy} gün içi işlemleri zaten onaylı ({g.GunIciOnaylayan}, {g.GunIciOnayTarihi:dd.MM.yyyy HH:mm}).", g);
                g.GunIciDurum = "Onaylandı"; g.GunIciOnaylayan = GunIciSorumlu.Ad; g.GunIciOnayTarihi = DateTime.Now; g.GunIciNot = (not ?? "").Trim(); g.GunIciHareket = hareket; g.GunIciOnaylanan = onaylanan; g.GunIciEksik = eksik; g.FinekraBekleyen = finekraBekleyen;
                g.Bakiyeler = bakiyeler ?? new List<GsBakiye>(); g.Kaydeden = kullanici ?? "";
                string ozet = string.Join(" · ", g.Bakiyeler.Where(b => b.Sayilir).Select(b => b.Grup == "Kasa" ? $"{b.Tur} {b.Tutar.ToString("N2", Tr)} ₺" : $"{b.Tur} {b.Adet} adet / {b.Tutar.ToString("N0", Tr)} ₺"));
                Log(g, "Gün İçi Onay", $"{hareket} hareket · {onaylanan} satır toplu onaylandı" + (eksik > 0 ? $" · {eksik} eksik / kontrol işaretli hariç" : "") + (finekraBekleyen > 0 ? $" · Finekra'da {finekraBekleyen} onay bekleyen" : "") + " · kalan bakiyeler kesinleşti: " + ozet + (g.GunIciNot == "" ? "" : " · " + g.GunIciNot), GunIciSorumlu.Ad + (string.IsNullOrEmpty(kullanici) || kullanici == GunIciSorumlu.Ad ? "" : $" (kaydeden {kullanici})"));
            }
            return (true, $"{g.Sirket} {g.Gun:dd.MM.yyyy} gün içi işlemleri {GunIciSorumlu.Ad} tarafından onaylandı: {onaylanan} satır onaylandı" + (eksik > 0 ? $", {eksik} eksik / kontrol işaretli satır hariç tutuldu" : "") + $". Kalan bakiyeler kesinleşti; fiziksel sayım {KasaSorumlu.Ad} onayına hazır.", g);
        }
        /// <summary>Gün içi onayını geri alır (yeniden hesaplanır; sayım yapılmışsa sıfırlanır).</summary>
        public static (bool ok, string mesaj, GunSonu g) GunIciGeriAl(string dbName, DateTime gun, string kullanici)
        {
            var g = Bul(dbName, gun); if (g == null || g.GunIciDurum != "Onaylandı") return (false, "Geri alınacak onay yok.", g);
            lock (Kilit) { g.GunIciDurum = "Bekliyor"; g.GunIciOnaylayan = ""; g.GunIciOnayTarihi = null; g.SayimDurumu = "Bekliyor"; g.SayimYapan = ""; g.SayimTarihi = null; g.Sayim = new List<GsSayim>(); Log(g, "Gün İçi Onay Geri Alındı", "bakiyeler yeniden canlı hesaplanır; sayım sıfırlandı", kullanici); }
            return (true, $"{g.Sirket} {g.Gun:dd.MM.yyyy} gün içi onayı geri alındı.", g);
        }

        /// <summary>3) Fiziksel sayım (Berk Berberoğlu): onayla=true → sayım sistemle birebir eşleşmeli; onayla=false → eksik / fazla bildirimi (farklı her tür için bir kayıt).</summary>
        public static (bool ok, string mesaj, GunSonu g) SayimKaydet(string dbName, DateTime gun, string sayimYapan, List<GsSayim> sayim, bool onayla, string not, string belgeNolar, string kullanici)
        {
            if (!string.Equals((sayimYapan ?? "").Trim(), KasaSorumlu.Ad, StringComparison.OrdinalIgnoreCase)) return (false, $"Fiziksel sayımı ve bakiye onayını yalnız {KasaSorumlu.Ad} ({KasaSorumlu.Unvan}) yapar.", null);
            var g = Bul(dbName, gun);
            if (g == null || g.GunIciDurum != "Onaylandı") return (false, $"Önce {GunIciSorumlu.Ad} ({GunIciSorumlu.Unvan}) gün içi işlemleri onaylamalı; fiziksel sayım ondan sonra yapılır.", g);
            if (g.SayimDurumu == "Onaylandı") return (false, $"{g.Sirket} {g.Gun:dd.MM.yyyy} bakiyeleri zaten onaylı ({g.SayimYapan}, {g.SayimTarihi:dd.MM.yyyy HH:mm}).", g);
            sayim = sayim ?? new List<GsSayim>(); var farklar = new List<(GsBakiye b, GsSayim s, int adetFark, decimal tutarFark)>();
            foreach (var b in g.Bakiyeler.Where(b => b.Sayilir))
            {
                var s = sayim.FirstOrDefault(x => string.Equals(x.Tur, b.Tur, StringComparison.OrdinalIgnoreCase));
                if (s == null || (s.Adet == null && s.Tutar == null)) { if (b.Grup == "Kasa") continue; return (false, $"{b.Tur} sayımı girilmedi (adet ve tutar).", g); }
                int adetFark = b.Grup == "Kasa" ? 0 : (s.Adet ?? b.Adet) - b.Adet; decimal tutarFark = Math.Round((s.Tutar ?? b.Tutar) - b.Tutar, 2);
                if (adetFark != 0 || tutarFark != 0) farklar.Add((b, s, adetFark, tutarFark));
            }
            lock (Kilit)
            {
                g.Sayim = sayim; g.SayimYapan = KasaSorumlu.Ad; g.SayimTarihi = DateTime.Now; g.SayimNot = (not ?? "").Trim(); g.Kaydeden = kullanici ?? "";
                string kim = KasaSorumlu.Ad + (string.IsNullOrEmpty(kullanici) || kullanici == KasaSorumlu.Ad ? "" : $" (kaydeden {kullanici})");
                if (onayla)
                {
                    if (farklar.Count > 0) return (false, "Sayım sistemle eşleşmiyor: " + string.Join(" · ", farklar.Select(f => $"{f.b.Tur} {(f.adetFark < 0 || (f.adetFark == 0 && f.tutarFark < 0) ? "eksik" : "fazla")} {Math.Abs(f.adetFark)} adet / {Math.Abs(f.tutarFark).ToString("N2", Tr)} ₺")) + ". Onay yerine \"Eksik / Fazla Bildir\" kullanın.", g);
                    g.SayimDurumu = "Onaylandı";
                    Log(g, "Bakiye Onayı", "fiziksel sayım sistemle birebir eşleşti: " + string.Join(" · ", g.Bakiyeler.Where(b => b.Sayilir).Select(b => b.Grup == "Kasa" ? $"{b.Tur} {b.Tutar.ToString("N2", Tr)} ₺" : $"{b.Tur} {b.Adet} adet")) + (g.SayimNot == "" ? "" : " · " + g.SayimNot), kim);
                    return (true, $"{g.Sirket} {g.Gun:dd.MM.yyyy} fiziksel sayım sistemle eşleşti; kalan bakiyeler {KasaSorumlu.Ad} tarafından onaylandı.", g);
                }
                if (farklar.Count == 0) return (false, "Sayım sistemle birebir eşleşiyor; bildirim yerine \"Eşleşti → Bakiyeleri Onayla\" kullanın.", g);
                foreach (var f in farklar)
                    _bildirimler.Add(new GsBildirim
                    {
                        Id = ++_bildirimSayac, Tarih = DateTime.Now, Kullanici = KasaSorumlu.Ad, DbName = dbName, Sirket = g.Sirket, Gun = g.Gun, Tur = f.b.Tur,
                        Yon = f.adetFark < 0 || (f.adetFark == 0 && f.tutarFark < 0) ? "Eksik" : "Fazla", Adet = Math.Abs(f.adetFark), Tutar = Math.Abs(f.tutarFark),
                        SistemAdet = f.b.Adet, SistemTutar = f.b.Tutar, SayilanAdet = f.s.Adet ?? f.b.Adet, SayilanTutar = f.s.Tutar ?? f.b.Tutar, BelgeNolar = (belgeNolar ?? "").Trim(), Aciklama = g.SayimNot
                    });
                g.SayimDurumu = "Eksik / Fazla";
                Log(g, "Eksik / Fazla Bildirimi", string.Join(" · ", farklar.Select(f => $"{f.b.Tur}: sistem {f.b.Adet} adet / {f.b.Tutar.ToString("N2", Tr)} ₺, sayılan {(f.s.Adet ?? f.b.Adet)} adet / {(f.s.Tutar ?? f.b.Tutar).ToString("N2", Tr)} ₺")) + (string.IsNullOrEmpty(belgeNolar) ? "" : " · belgeler: " + belgeNolar) + (g.SayimNot == "" ? "" : " · " + g.SayimNot), kim);
                return (true, $"{farklar.Count} eksik / fazla bildirimi kaydedildi ({string.Join(", ", farklar.Select(f => f.b.Tur))}); {GunIciSorumlu.Ad} ve finans ekibi bilgilendirildi. Bildirim çözülünce yeniden sayım yapılır.", g);
            }
        }
        /// <summary>Eksik / fazla bildirimi çözümü (finans): açıklama yazılır; günün açık bildirimi kalmazsa sayım yeniden yapılabilir.</summary>
        public static (bool ok, string mesaj, GsBildirim b) BildirimCoz(int id, string cozum, string kullanici)
        {
            lock (Kilit)
            {
                var b = _bildirimler.FirstOrDefault(x => x.Id == id); if (b == null) return (false, "Bildirim bulunamadı.", null);
                if (b.Durum == "Çözüldü") return (false, "Bildirim zaten çözülmüş.", b);
                if (string.IsNullOrWhiteSpace(cozum)) return (false, "Çözüm açıklaması yazın (ör. belge kasada bulundu, kayıt düzeltildi).", b);
                b.Durum = "Çözüldü"; b.Cozum = cozum.Trim(); b.Cozen = kullanici ?? ""; b.CozumTarihi = DateTime.Now;
                var g = _kayitlar.TryGetValue(Anahtar(b.DbName, b.Gun), out var x) ? x : null;
                if (g != null) { Log(g, "Bildirim Çözüldü", $"{b.Tur} {b.Yon} {b.Adet} adet / {b.Tutar.ToString("N2", Tr)} ₺ · {b.Cozum}", kullanici); if (!_bildirimler.Any(y => y.DbName == b.DbName && y.Gun == b.Gun && y.Durum == "Açık") && g.SayimDurumu == "Eksik / Fazla") { g.SayimDurumu = "Bekliyor"; Log(g, "Yeniden Sayım", "açık bildirim kalmadı; fiziksel sayım yeniden yapılır", kullanici); } }
                return (true, $"Bildirim #{b.Id} çözüldü" + (g != null && g.SayimDurumu == "Bekliyor" ? "; günün açık bildirimi kalmadı, yeniden sayım yapılabilir." : "."), b);
            }
        }
        public static List<GsBildirim> Bildirimler(string dbName, DateTime? bas = null, DateTime? bit = null) { lock (Kilit) return _bildirimler.Where(b => (string.IsNullOrEmpty(dbName) || b.DbName == dbName) && (bas == null || b.Gun >= bas) && (bit == null || b.Gun <= bit)).OrderByDescending(b => b.Tarih).ToList(); }

        public static object BakiyeJson(GsBakiye b) => new { b.Grup, b.Tur, b.Kod, b.PB, b.AcilisAdet, b.Acilis, b.GirisAdet, b.Giris, b.CikisAdet, b.Cikis, b.Adet, b.Tutar, b.Sayilir, b.Not };
        public static object BildirimJson(GsBildirim b) => new { b.Id, tarih = b.Tarih.ToString("yyyy-MM-dd HH:mm"), b.Kullanici, b.DbName, b.Sirket, gun = b.Gun.ToString("yyyy-MM-dd"), b.Tur, b.Yon, b.Adet, b.Tutar, b.SistemAdet, b.SistemTutar, b.SayilanAdet, b.SayilanTutar, b.BelgeNolar, b.Aciklama, b.Durum, b.Cozum, b.Cozen, cozumTarihi = b.CozumTarihi?.ToString("yyyy-MM-dd HH:mm") };
        /// <summary>Panel verisi: onaylıysa anlık görüntü, değilse canlı bakiyeler (dışarıdan verilir).</summary>
        public static object Json(GunSonu g, List<GsBakiye> canli, int hareket, int onaysiz, int eksik, int finekraBekleyen)
        {
            bool onayli = g != null && g.GunIciDurum == "Onaylandı"; var bak = onayli ? g.Bakiyeler : canli ?? new List<GsBakiye>();
            List<GsBildirim> bl; lock (Kilit) bl = g == null ? new List<GsBildirim>() : _bildirimler.Where(b => b.DbName == g.DbName && b.Gun == g.Gun).OrderByDescending(b => b.Tarih).ToList();
            return new
            {
                dbName = g?.DbName, sirket = g?.Sirket, gun = g?.Gun.ToString("yyyy-MM-dd"),
                gunIci = new { durum = g?.GunIciDurum ?? "Bekliyor", onaylayan = g?.GunIciOnaylayan ?? "", tarih = g?.GunIciOnayTarihi?.ToString("yyyy-MM-dd HH:mm"), not = g?.GunIciNot ?? "", hareket = onayli ? g.GunIciHareket : hareket, onaysiz = onayli ? 0 : onaysiz, onaylanan = onayli ? g.GunIciOnaylanan : 0, eksik = onayli ? g.GunIciEksik : eksik, finekraBekleyen = onayli ? g.FinekraBekleyen : finekraBekleyen, sorumlu = GunIciSorumlu.Ad, unvan = GunIciSorumlu.Unvan },
                bakiyeler = bak.Select(BakiyeJson).ToList(), kesin = onayli,
                sayim = new { durum = g?.SayimDurumu ?? "Bekliyor", yapan = g?.SayimYapan ?? "", tarih = g?.SayimTarihi?.ToString("yyyy-MM-dd HH:mm"), not = g?.SayimNot ?? "", satirlar = (g?.Sayim ?? new List<GsSayim>()).Select(s => new { s.Tur, s.Adet, s.Tutar }).ToList(), sorumlu = KasaSorumlu.Ad, unvan = KasaSorumlu.Unvan, acikBildirim = bl.Count(b => b.Durum == "Açık") },
                bildirimler = bl.Select(BildirimJson).ToList(),
                gunluk = (g?.Gunluk ?? new List<TalimatGunluk>()).OrderByDescending(x => x.Tarih).Select(x => new { tarih = x.Tarih.ToString("yyyy-MM-dd HH:mm"), x.Kullanici, x.Islem, x.Aciklama }).ToList()
            };
        }
    }
}
