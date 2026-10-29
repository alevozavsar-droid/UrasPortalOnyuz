// ============================================================
// ZİMMET RAPORU — IT ve idari varlıkların personele zimmeti (İK ve İdari İşler › IT › Zimmet ve Envanter; /Zimmet)
// Varlık: dizüstü / masaüstü, monitör, cep telefonu, SIM hattı, tablet, yazıcı, erişim kartı, anahtar / kasa, yazılım lisansı, şirket kredi kartı, dijital imza;
// seri no, şirket, alış tarihi, değer, garanti, durum (Zimmetli | Depoda | Arızalı | Serviste | Hurda | Kayıp), zimmetli personel ve tarihi, dosyalar (zimmet tutanağı,
// fatura), hareketler. Personel ana verisi Data/NakitAvansOrnek.cs (Personel). Araç zimmeti Data/AracBakimOrnek.cs'de tutulur; kişi bazında görünümde araç da listelenir.
// Personel Dosyası (Data/PersonelDosyaOrnek.cs) kişinin zimmetlerini buradan canlı okur. Ekran: Controllers/ZimmetController.cs + Views/Zimmet/Index.cshtml + wwwroot/js/zimmet.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class ZmHareket { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }
    public class ZmDosya { public int No { get; set; } public string Tur { get; set; } public string Dosya { get; set; } public string Tip { get; set; } public long Boyut { get; set; } public byte[] Veri { get; set; } public DateTime Tarih { get; set; } public string Kullanici { get; set; } }
    public class ZmVarlik
    {
        public int Id { get; set; } public string No { get; set; } public string Tur { get; set; } public string Marka { get; set; } = ""; public string Model { get; set; } = ""; public string SeriNo { get; set; } = ""; public string Ozellik { get; set; } = ""; public string Sirket { get; set; }
        public DateTime AlisTarihi { get; set; } public decimal Deger { get; set; } public DateTime? GarantiBitis { get; set; } public string Durum { get; set; } = "Depoda"; public string PersonelNo { get; set; } = ""; public DateTime? ZimmetTarihi { get; set; } public string Lokasyon { get; set; } = "BT Depo"; public string Not { get; set; } = "";
        public List<ZmDosya> Dosyalar { get; set; } = new List<ZmDosya>(); public List<ZmHareket> Hareketler { get; set; } = new List<ZmHareket>();
        public string PersonelAd => string.IsNullOrEmpty(PersonelNo) ? "" : (NakitAvansOrnek.KisiBul(PersonelNo)?.AdSoyad ?? PersonelNo);
        public bool Zimmetli => Durum == "Zimmetli" && !string.IsNullOrEmpty(PersonelNo);
    }
    public class ZmVarlikIstek { public int? Id { get; set; } public string Tur { get; set; } public string Marka { get; set; } public string Model { get; set; } public string SeriNo { get; set; } public string Ozellik { get; set; } public string Sirket { get; set; } public string AlisTarihi { get; set; } public decimal Deger { get; set; } public string GarantiBitis { get; set; } public string Lokasyon { get; set; } public string Not { get; set; } public string PersonelNo { get; set; } }
    public class ZmZimmetIstek { public int Id { get; set; } public string PersonelNo { get; set; } public string Tarih { get; set; } public string Not { get; set; } }
    public class ZmIadeIstek { public int Id { get; set; } public string Durum { get; set; } public string Not { get; set; } }

    public static class ZimmetOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public static readonly string[] Turler = { "Dizüstü Bilgisayar", "Masaüstü Bilgisayar", "Monitör", "Cep Telefonu", "SIM Hattı", "Tablet", "Yazıcı", "Erişim Kartı", "Anahtar / Kasa", "Yazılım Lisansı", "Şirket Kredi Kartı", "Dijital İmza / Token", "Diğer" };
        public static readonly string[] Durumlar = { "Zimmetli", "Depoda", "Arızalı", "Serviste", "Hurda", "Kayıp" };
        public static readonly string[] Lokasyonlar = { "BT Depo", "Merkez", "Velimeşe Fabrika", "Çerkezköy", "Çorlu", "Gebze", "İstanbul Ofis", "Personelde" };
        public static readonly string[] Sirketler = { "Uras Kimya", "Alv Kimya", "Selvi", "Uras Holding", "Avrupa Paper", "URS Makina", "Alv Filo", "DRN", "Uras Baskı Teknolojileri" };
        private static readonly List<ZmVarlik> _varliklar = new List<ZmVarlik>();
        private static int _sonId, _sonDosya; private static bool _tohumlandi;

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var rnd = new Random(20261102); var bugun = DateTime.Today; int seri = 4100;
                ZmVarlik V(string tur, string marka, string model, string ozellik, string sirket, decimal deger, int alisGunOnce, int garantiAy, string personelNo, int zimmetGunOnce, string lokasyon = null)
                {
                    var alis = bugun.AddDays(-alisGunOnce); var v = new ZmVarlik { Id = ++_sonId, No = $"ZMT-{_sonId:0000}", Tur = tur, Marka = marka, Model = model, Ozellik = ozellik, SeriNo = tur == "SIM Hattı" ? $"0532 {rnd.Next(100, 999)} {rnd.Next(10, 99)} {rnd.Next(10, 99)}" : tur == "Erişim Kartı" ? $"KART-{rnd.Next(1000, 9999)}" : tur == "Yazılım Lisansı" ? $"LIC-{rnd.Next(100000, 999999)}" : tur == "Şirket Kredi Kartı" ? $"**** {rnd.Next(1000, 9999)}" : $"SN{++seri}{rnd.Next(100, 999)}", Sirket = sirket, AlisTarihi = alis, Deger = deger, GarantiBitis = garantiAy > 0 ? alis.AddMonths(garantiAy) : (DateTime?)null, Lokasyon = lokasyon ?? (personelNo != "" ? "Personelde" : "BT Depo") };
                    if (personelNo != "") { v.Durum = "Zimmetli"; v.PersonelNo = personelNo; v.ZimmetTarihi = bugun.AddDays(-zimmetGunOnce); v.Hareketler.Add(new ZmHareket { Zaman = alis.AddHours(10), Kullanici = "Ali Veli", Islem = "Envantere Alındı", Aciklama = $"{marka} {model} · {deger.ToString("N0", Tr)} ₺" }); v.Hareketler.Add(new ZmHareket { Zaman = v.ZimmetTarihi.Value.AddHours(11), Kullanici = "Ali Veli", Islem = "Zimmet Verildi", Aciklama = $"{v.PersonelAd} · tutanak imzalandı" }); v.Dosyalar.Add(new ZmDosya { No = ++_sonDosya, Tur = "Zimmet Tutanağı", Dosya = $"zimmet-tutanagi-{v.No}.pdf", Tip = "application/pdf", Boyut = rnd.Next(60, 180) * 1024L, Tarih = v.ZimmetTarihi.Value.AddHours(11), Kullanici = "Ali Veli" }); }
                    else { v.Hareketler.Add(new ZmHareket { Zaman = alis.AddHours(10), Kullanici = "Ali Veli", Islem = "Envantere Alındı", Aciklama = $"{marka} {model} · {deger.ToString("N0", Tr)} ₺ · depo" }); }
                    _varliklar.Add(v); return v;
                }
                foreach (var p in NakitAvansOrnek.Personel.Where(p => p.Aktif))
                {
                    int gun = rnd.Next(60, 900); bool it = p.Departman == "Bilgi Teknolojileri", yonetim = p.Rol == "Yönetici" || p.Rol == "Üst Yönetim" || p.Rol == "Finans", saha = p.Departman == "Teknik Servis" || p.Departman == "Satış" || p.Departman == "Kalite";
                    bool lenovo = rnd.NextDouble() < 0.6, apple = rnd.NextDouble() < 0.5;
                    if (saha || yonetim || it || rnd.NextDouble() < 0.6) V("Dizüstü Bilgisayar", lenovo ? "Lenovo" : "Dell", lenovo ? "ThinkPad T14 Gen 4" : "Latitude 5540", "i7 · 16 GB · 512 GB SSD", p.Sirket, rnd.Next(42000, 68000), gun, 36, p.PersonelNo, gun - rnd.Next(0, 5));
                    else V("Masaüstü Bilgisayar", "HP", "ProDesk 400 G9", "i5 · 16 GB · 512 GB", p.Sirket, rnd.Next(28000, 36000), gun, 36, p.PersonelNo, gun - 2);
                    if (!saha || rnd.NextDouble() < 0.5) V("Monitör", "Dell", "P2422H 24\"", "FHD · IPS", p.Sirket, rnd.Next(6500, 9500), gun + rnd.Next(0, 200), 36, p.PersonelNo, gun - 2);
                    if (saha || yonetim || rnd.NextDouble() < 0.7) { V("Cep Telefonu", apple ? "Apple" : "Samsung", apple ? "iPhone 15" : "Galaxy S24", "256 GB", p.Sirket, rnd.Next(32000, 58000), rnd.Next(30, 500), 24, p.PersonelNo, rnd.Next(10, 400)); V("SIM Hattı", "Turkcell", "Kurumsal Hat", "20 GB · sınırsız konuşma", p.Sirket, 0, rnd.Next(300, 1500), 0, p.PersonelNo, rnd.Next(10, 600)); }
                    V("Erişim Kartı", "HID", "Proximity", p.Sirket == "Uras Kimya" ? "Velimeşe + Merkez" : "Merkez", p.Sirket, 150, rnd.Next(100, 1200), 0, p.PersonelNo, rnd.Next(10, 900));
                    if (it) V("Tablet", "Apple", "iPad Air", "256 GB · Wi-Fi", p.Sirket, 28000, 320, 24, p.PersonelNo, 310);
                    if (p.Rol == "Finans" || p.Rol == "Üst Yönetim" || p.Departman == "Hukuk") V("Dijital İmza / Token", "e-Güven", "Nitelikli E-İmza", "3 yıl", p.Sirket, 1800, rnd.Next(100, 700), 36, p.PersonelNo, rnd.Next(10, 600));
                    if (yonetim || p.Departman == "Satış" || p.Departman == "Satın Alma") V("Şirket Kredi Kartı", "Garanti BBVA", "Kurumsal Kart", $"aylık limit {(yonetim ? "150.000" : "40.000")} ₺", p.Sirket, 0, rnd.Next(100, 900), 0, p.PersonelNo, rnd.Next(10, 800));
                    V("Yazılım Lisansı", "Microsoft", "Microsoft 365 Business Standard", "E-posta · Office · Teams", p.Sirket, 3600, rnd.Next(30, 365), 12, p.PersonelNo, rnd.Next(5, 360));
                }
                // depo ve arızalı varlıklar
                V("Dizüstü Bilgisayar", "Lenovo", "ThinkPad E15", "i5 · 8 GB · 256 GB", "Uras Kimya", 31000, 700, 36, "", 0); V("Dizüstü Bilgisayar", "Dell", "Latitude 3540", "i5 · 16 GB · 512 GB", "Uras Holding", 38000, 90, 36, "", 0); V("Cep Telefonu", "Samsung", "Galaxy A54", "128 GB", "Uras Kimya", 16000, 400, 24, "", 0); V("Monitör", "Samsung", "27\" Curved", "FHD", "Alv Kimya", 7500, 500, 36, "", 0);
                var ar = V("Dizüstü Bilgisayar", "HP", "EliteBook 840 G8", "i7 · 16 GB · 512 GB", "Selvi", 45000, 800, 36, "", 0); ar.Durum = "Arızalı"; ar.Not = "Ekran kırık; onarım teklifi bekleniyor"; ar.Hareketler.Add(new ZmHareket { Zaman = bugun.AddDays(-12).AddHours(9), Kullanici = "Ali Veli", Islem = "İade", Aciklama = "Emre Taş iade etti · ekran hasarlı · arızalı" });
                var sv = V("Yazıcı", "HP", "LaserJet Pro M428", "Çok fonksiyonlu", "Uras Kimya", 14500, 600, 24, "", 0); sv.Durum = "Serviste"; sv.Lokasyon = "Yetkili servis"; sv.Not = "Drum değişimi";
                var ky = V("Cep Telefonu", "Apple", "iPhone 13", "128 GB", "Uras Kimya", 22000, 900, 24, "", 0); ky.Durum = "Kayıp"; ky.Not = "Selin Kara ayrılışında teslim edilmedi; tutanak tutuldu"; ky.Hareketler.Add(new ZmHareket { Zaman = bugun.AddDays(-45).AddHours(9), Kullanici = "Ali Veli", Islem = "Kayıp Bildirimi", Aciklama = "işten ayrılış · teslim alınamadı" });
            }
        }

        public static List<ZmVarlik> Varliklar { get { Tohumla(); lock (Kilit) return _varliklar.OrderBy(v => v.Durum == "Zimmetli" ? 0 : v.Durum == "Depoda" ? 1 : 2).ThenBy(v => v.PersonelAd, StringComparer.Create(Tr, false)).ThenBy(v => v.Tur).ToList(); } }
        public static ZmVarlik Bul(int id) { Tohumla(); lock (Kilit) return _varliklar.FirstOrDefault(v => v.Id == id); }
        public static List<ZmVarlik> Kisinin(string personelNo) { Tohumla(); lock (Kilit) return _varliklar.Where(v => v.Durum == "Zimmetli" && string.Equals(v.PersonelNo, personelNo, StringComparison.OrdinalIgnoreCase)).ToList(); }
        static void Log(ZmVarlik v, string islem, string aciklama, string kullanici) => v.Hareketler.Add(new ZmHareket { Zaman = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });

        public static (bool ok, string mesaj, ZmVarlik v) Kaydet(ZmVarlikIstek i, string kullanici)
        {
            Tohumla(); if (i == null) return (false, "Geçersiz istek.", null); if (!Turler.Contains(i.Tur ?? "")) return (false, "Varlık türü seçin.", null); if (!Sirketler.Contains(i.Sirket ?? "")) return (false, "Şirket seçin.", null);
            if (!DateTime.TryParse(i.AlisTarihi, out var alis)) alis = DateTime.Today;
            lock (Kilit)
            {
                var v = i.Id.HasValue ? _varliklar.FirstOrDefault(x => x.Id == i.Id.Value) : null; bool yeni = v == null;
                if (yeni) { v = new ZmVarlik { Id = ++_sonId, No = $"ZMT-{_sonId:0000}" }; _varliklar.Add(v); }
                v.Tur = i.Tur; v.Marka = (i.Marka ?? "").Trim(); v.Model = (i.Model ?? "").Trim(); v.SeriNo = (i.SeriNo ?? "").Trim(); v.Ozellik = (i.Ozellik ?? "").Trim(); v.Sirket = i.Sirket; v.AlisTarihi = alis.Date; v.Deger = Math.Max(0, i.Deger); v.GarantiBitis = DateTime.TryParse(i.GarantiBitis, out var g) ? g.Date : (DateTime?)null; v.Lokasyon = string.IsNullOrWhiteSpace(i.Lokasyon) ? v.Lokasyon : i.Lokasyon.Trim(); v.Not = (i.Not ?? "").Trim();
                Log(v, yeni ? "Envantere Alındı" : "Güncellendi", $"{v.Tur} · {v.Marka} {v.Model} · {v.SeriNo} · {v.Deger.ToString("N0", Tr)} ₺", kullanici);
                if (yeni && !string.IsNullOrEmpty(i.PersonelNo)) { var k = NakitAvansOrnek.KisiBul(i.PersonelNo); if (k != null) { v.Durum = "Zimmetli"; v.PersonelNo = k.PersonelNo; v.ZimmetTarihi = DateTime.Today; v.Lokasyon = "Personelde"; Log(v, "Zimmet Verildi", $"{k.AdSoyad} · {k.Departman}", kullanici); } }
                return (true, $"{v.No} {(yeni ? "envantere alındı" : "güncellendi")}: {v.Tur} · {v.Marka} {v.Model}" + (v.Zimmetli ? $" · zimmet {v.PersonelAd}" : "") + ".", v);
            }
        }
        public static (bool ok, string mesaj, ZmVarlik v) ZimmetVer(ZmZimmetIstek i, string kullanici)
        {
            var v = Bul(i?.Id ?? 0); if (v == null) return (false, "Varlık bulunamadı.", null); var k = NakitAvansOrnek.KisiBul(i.PersonelNo ?? ""); if (k == null) return (false, "Personel seçin.", v);
            if (v.Durum == "Hurda" || v.Durum == "Kayıp") return (false, $"{v.No} {v.Durum.ToLower(Tr)} durumunda; zimmet verilemez.", v);
            lock (Kilit) { string eski = v.Zimmetli ? v.PersonelAd : v.Durum; v.Durum = "Zimmetli"; v.PersonelNo = k.PersonelNo; v.ZimmetTarihi = DateTime.TryParse(i.Tarih, out var t) ? t.Date : DateTime.Today; v.Lokasyon = "Personelde"; Log(v, "Zimmet Verildi", $"{eski} → {k.AdSoyad} · {k.Sirket} · {k.Departman}" + (string.IsNullOrWhiteSpace(i.Not) ? "" : " · " + i.Not.Trim()), kullanici); }
            return (true, $"{v.No} {v.Tur} {k.AdSoyad} adına zimmetlendi.", v);
        }
        public static (bool ok, string mesaj, ZmVarlik v) Iade(ZmIadeIstek i, string kullanici)
        {
            var v = Bul(i?.Id ?? 0); if (v == null) return (false, "Varlık bulunamadı.", null); string durum = Durumlar.Contains(i.Durum ?? "") && i.Durum != "Zimmetli" ? i.Durum : "Depoda";
            lock (Kilit) { string eski = v.Zimmetli ? v.PersonelAd : v.Durum; v.Durum = durum; v.PersonelNo = ""; v.ZimmetTarihi = null; v.Lokasyon = durum == "Serviste" ? "Yetkili servis" : "BT Depo"; Log(v, durum == "Depoda" ? "İade Alındı" : durum, $"{eski} → {durum}" + (string.IsNullOrWhiteSpace(i.Not) ? "" : " · " + i.Not.Trim()), kullanici); }
            return (true, $"{v.No} iade alındı · {durum}.", v);
        }
        public static (bool ok, string mesaj, ZmVarlik v) DosyaEkle(int id, string tur, string dosyaAdi, string icerikTipi, byte[] veri, string kullanici)
        {
            var v = Bul(id); if (v == null) return (false, "Varlık bulunamadı.", null); if (veri == null || veri.Length == 0) return (false, "Dosya seçin.", v); if (veri.LongLength > 12_000_000) return (false, "Dosya 12 MB'ı aşamaz.", v);
            lock (Kilit) { v.Dosyalar.Add(new ZmDosya { No = ++_sonDosya, Tur = string.IsNullOrWhiteSpace(tur) ? "Belge" : tur.Trim(), Dosya = dosyaAdi, Tip = icerikTipi ?? "application/octet-stream", Boyut = veri.LongLength, Veri = veri, Tarih = DateTime.Now, Kullanici = kullanici }); Log(v, "Dosya Yüklendi", $"{tur}: {dosyaAdi}", kullanici); }
            return (true, $"{v.No}: {dosyaAdi} yüklendi.", v);
        }

        public static object Json(ZmVarlik v) => new { v.Id, no = v.No, tur = v.Tur, marka = v.Marka, model = v.Model, seriNo = v.SeriNo, ozellik = v.Ozellik, sirket = v.Sirket, alisTarihi = v.AlisTarihi.ToString("yyyy-MM-dd"), deger = v.Deger, garantiBitis = v.GarantiBitis?.ToString("yyyy-MM-dd"), garantiDolmus = v.GarantiBitis.HasValue && v.GarantiBitis < DateTime.Today, durum = v.Durum, personelNo = v.PersonelNo, personel = v.PersonelAd, departman = NakitAvansOrnek.KisiBul(v.PersonelNo)?.Departman, zimmetTarihi = v.ZimmetTarihi?.ToString("yyyy-MM-dd"), lokasyon = v.Lokasyon, not = v.Not, dosyalar = v.Dosyalar.Select(d => new { d.No, d.Tur, d.Dosya, d.Boyut, tarih = d.Tarih.ToString("yyyy-MM-dd HH:mm"), d.Kullanici, gercek = d.Veri != null }).ToList(), hareketler = v.Hareketler.OrderByDescending(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("yyyy-MM-dd HH:mm"), h.Kullanici, h.Islem, h.Aciklama }).ToList() };
        public static object KisiOzet(NaKisi k) { var l = Kisinin(k.PersonelNo); var arac = AracBakimOrnek.Araclar.Where(a => (a.Zimmet ?? "").Contains(k.AdSoyad)).ToList(); return new { personelNo = k.PersonelNo, ad = k.AdSoyad, sirket = k.Sirket, departman = k.Departman, unvan = k.Unvan, aktif = k.Aktif, adet = l.Count, deger = l.Sum(v => v.Deger), turler = l.Select(v => v.Tur).Distinct().ToList(), araclar = arac.Select(a => a.Plaka).ToList(), sonZimmet = l.OrderByDescending(v => v.ZimmetTarihi).FirstOrDefault()?.ZimmetTarihi?.ToString("yyyy-MM-dd") }; }
        public static object Ozet() { var l = Varliklar; return new { toplam = l.Count, zimmetli = l.Count(v => v.Durum == "Zimmetli"), depoda = l.Count(v => v.Durum == "Depoda"), arizali = l.Count(v => v.Durum == "Arızalı" || v.Durum == "Serviste"), kayip = l.Count(v => v.Durum == "Kayıp" || v.Durum == "Hurda"), deger = l.Where(v => v.Durum != "Hurda" && v.Durum != "Kayıp").Sum(v => v.Deger), zimmetliDeger = l.Where(v => v.Durum == "Zimmetli").Sum(v => v.Deger), kisi = l.Where(v => v.Zimmetli).Select(v => v.PersonelNo).Distinct().Count(), garantiDolan = l.Count(v => v.GarantiBitis.HasValue && v.GarantiBitis < DateTime.Today && v.Durum != "Hurda"), turler = Turler.Select(t => new { tur = t, adet = l.Count(v => v.Tur == t), zimmetli = l.Count(v => v.Tur == t && v.Zimmetli), deger = l.Where(v => v.Tur == t).Sum(v => v.Deger) }).Where(x => x.adet > 0).ToList(), sirketler = Sirketler.Select(s => new { sirket = s, adet = l.Count(v => v.Sirket == s), deger = l.Where(v => v.Sirket == s).Sum(v => v.Deger) }).Where(x => x.adet > 0).ToList() }; }

        /// <summary>Zimmet tutanağı: kişinin üzerindeki tüm varlıklar (+ araç) — imza alanlı PDF.</summary>
        public static DaBelge Tutanak(NaKisi k, string hazirlayan)
        {
            var l = Kisinin(k.PersonelNo); var arac = AracBakimOrnek.Araclar.Where(a => (a.Zimmet ?? "").Contains(k.AdSoyad)).ToList();
            var d = new DaBelge { Baslik = "Zimmet Tutanağı", AltBaslik = $"{k.AdSoyad} · {k.Unvan} · {k.Departman} · {k.Sirket} · Personel No {k.PersonelNo}", Sirket = k.Sirket, Hazirlayan = hazirlayan, Yatay = false };
            d.Ozet.Add(("Varlık", (l.Count + arac.Count).ToString())); d.Ozet.Add(("Toplam Değer", l.Sum(v => v.Deger).ToString("N0", Tr) + " ₺")); d.Ozet.Add(("Tarih", DateTime.Today.ToString("dd.MM.yyyy"))); d.Ozet.Add(("Düzenleyen", hazirlayan));
            var bl = new DaBlok { Baslik = "Zimmetli Varlıklar", Kolonlar = { "No", "Tür", "Marka / Model", "Seri No", "Özellik", "Zimmet Tarihi", "Değer (₺)" }, Sag = { false, false, false, false, false, false, true } };
            foreach (var v in l.OrderBy(v => v.Tur)) bl.Satirlar.Add(new[] { v.No, v.Tur, $"{v.Marka} {v.Model}".Trim(), v.SeriNo, v.Ozellik, v.ZimmetTarihi?.ToString("dd.MM.yyyy") ?? "", v.Deger.ToString("N0", Tr) });
            foreach (var a in arac) bl.Satirlar.Add(new[] { a.Plaka, "Araç", $"{a.Marka} {a.Model} ({a.Yil})", a.Sasi, $"{a.Km:N0} km · {a.Tip}", "", "" });
            bl.Toplam = new[] { "", "", "", "", "", "Toplam", l.Sum(v => v.Deger).ToString("N0", Tr) }; d.Bloklar.Add(bl);
            d.Bloklar.Add(new DaBlok { Baslik = "Taahhüt ve İmza", Not = "Yukarıda listelenen varlıkları çalışır durumda teslim aldım; işten ayrılışta ya da talep edildiğinde eksiksiz iade edeceğimi, hasar / kayıp durumunda şirket prosedürünün uygulanacağını kabul ederim.\n\nTeslim Alan (Personel): " + k.AdSoyad + "   İmza: ______________        Teslim Eden (IT): " + hazirlayan + "   İmza: ______________        Tarih: " + DateTime.Today.ToString("dd.MM.yyyy") });
            return d;
        }
    }
}
