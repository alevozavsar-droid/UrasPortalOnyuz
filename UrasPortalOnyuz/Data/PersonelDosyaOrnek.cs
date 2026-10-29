// ============================================================
// PERSONEL DOSYASI — her personel için tek dosya (İK ve İdari İşler › İnsan Kaynakları › Personel ve Organizasyon; /PersonelDosya)
// DİNAMİK: zimmetler Data/ZimmetOrnek.cs'den, araç Data/AracBakimOrnek.cs'den, açık avanslar Data/NakitAvansOrnek.cs'den canlı okunur; burada kimlik / iletişim, acil durum,
// ehliyet, ŞİRKET PASAPORTU (kullanıyor mu, şirket kasasında mı, no, bitiş, vizeler), belgeler (iş sözleşmesi, SGK giriş, sağlık raporu, İSG sertifikası … dosya yükleme, geçerlilik),
// eğitimler ve günlük tutulur. Personel ana verisi Data/NakitAvansOrnek.cs (Personel). Ekran: Controllers/PersonelDosyaController.cs + Views/PersonelDosya/Index.cshtml + wwwroot/js/personel-dosya.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class PdBelge { public int No { get; set; } public string Tur { get; set; } public string Dosya { get; set; } public string Tip { get; set; } public long Boyut { get; set; } public byte[] Veri { get; set; } public DateTime Tarih { get; set; } public string Kullanici { get; set; } public DateTime? GecerlilikBitis { get; set; } public string Durum => GecerlilikBitis == null ? "Süresiz" : GecerlilikBitis.Value.Date < DateTime.Today ? "Süresi Doldu" : (GecerlilikBitis.Value.Date - DateTime.Today).Days <= 60 ? "Süresi Yaklaşıyor" : "Geçerli"; }
    public class PdEgitim { public int No { get; set; } public string Ad { get; set; } public DateTime Tarih { get; set; } public DateTime? Bitis { get; set; } public string Kurum { get; set; } = ""; public string Sertifika { get; set; } = ""; }
    public class PdVize { public string Ulke { get; set; } public string Tur { get; set; } public DateTime Bitis { get; set; } }
    public class PdHareket { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }
    public class PdDosya
    {
        public string PersonelNo { get; set; } public string Telefon { get; set; } = ""; public string Eposta { get; set; } = ""; public string Adres { get; set; } = ""; public DateTime? DogumTarihi { get; set; } public DateTime? IseGiris { get; set; } public string KanGrubu { get; set; } = ""; public string AcilKisi { get; set; } = ""; public string AcilTelefon { get; set; } = "";
        public string EhliyetSinifi { get; set; } = ""; public DateTime? EhliyetBitis { get; set; }
        public string PasaportDurumu { get; set; } = "Yok"; public string PasaportNo { get; set; } = ""; public DateTime? PasaportBitis { get; set; } public bool PasaportKasada { get; set; } public List<PdVize> Vizeler { get; set; } = new List<PdVize>();
        public List<PdBelge> Belgeler { get; set; } = new List<PdBelge>(); public List<PdEgitim> Egitimler { get; set; } = new List<PdEgitim>(); public List<PdHareket> Hareketler { get; set; } = new List<PdHareket>(); public string Not { get; set; } = "";
    }
    public class PdKayitIstek { public string PersonelNo { get; set; } public string Telefon { get; set; } public string Eposta { get; set; } public string Adres { get; set; } public string DogumTarihi { get; set; } public string IseGiris { get; set; } public string KanGrubu { get; set; } public string AcilKisi { get; set; } public string AcilTelefon { get; set; } public string EhliyetSinifi { get; set; } public string EhliyetBitis { get; set; } public string PasaportDurumu { get; set; } public string PasaportNo { get; set; } public string PasaportBitis { get; set; } public bool? PasaportKasada { get; set; } public string Vizeler { get; set; } public string Not { get; set; } }
    public class PdEgitimIstek { public string PersonelNo { get; set; } public string Ad { get; set; } public string Tarih { get; set; } public string Bitis { get; set; } public string Kurum { get; set; } public string Sertifika { get; set; } }

    public static class PersonelDosyaOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public static readonly string[] PasaportDurumlari = { "Yok", "Kendi Pasaportu", "Şirket Sorumluluğunda", "Yeşil (Hususi) Pasaport" };
        public static readonly string[] BelgeTurleri = { "İş Sözleşmesi", "SGK İşe Giriş Bildirgesi", "Kimlik Fotokopisi", "İkametgâh", "Diploma", "Adli Sicil Kaydı", "Sağlık Raporu", "İSG Eğitim Sertifikası", "Askerlik Belgesi", "Ehliyet Fotokopisi", "Pasaport Fotokopisi", "Zimmet Tutanağı", "Gizlilik Taahhütnamesi", "KVKK Aydınlatma", "Diğer" };
        private static readonly Dictionary<string, PdDosya> _dosyalar = new Dictionary<string, PdDosya>(StringComparer.OrdinalIgnoreCase);
        private static int _sonBelge; private static bool _tohumlandi;

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var rnd = new Random(20261104); var bugun = DateTime.Today; int i = 0;
                foreach (var p in NakitAvansOrnek.Personel)
                {
                    var d = new PdDosya { PersonelNo = p.PersonelNo, Telefon = $"0532 {rnd.Next(200, 999)} {rnd.Next(10, 99)} {rnd.Next(10, 99)}", Eposta = Eposta(p), Adres = new[] { "Çorlu / Tekirdağ", "Çerkezköy / Tekirdağ", "Kadıköy / İstanbul", "Gebze / Kocaeli", "Ergene / Tekirdağ", "Ataşehir / İstanbul" }[i % 6], DogumTarihi = new DateTime(1975 + rnd.Next(0, 25), rnd.Next(1, 13), rnd.Next(1, 28)), IseGiris = bugun.AddDays(-rnd.Next(120, 4000)), KanGrubu = new[] { "A Rh+", "0 Rh+", "B Rh+", "AB Rh+", "A Rh-" }[rnd.Next(5)], AcilKisi = new[] { "Eş", "Anne", "Baba", "Kardeş" }[rnd.Next(4)] + " · " + new[] { "Ayşe", "Mehmet", "Fatma", "Ahmet", "Zeynep" }[rnd.Next(5)] + " " + p.AdSoyad.Split(' ').Last(), AcilTelefon = $"0533 {rnd.Next(200, 999)} {rnd.Next(10, 99)} {rnd.Next(10, 99)}", EhliyetSinifi = rnd.NextDouble() < 0.9 ? "B" : "", EhliyetBitis = rnd.NextDouble() < 0.9 ? bugun.AddDays(rnd.Next(-60, 3000)) : (DateTime?)null };
                    bool seyahat = p.Departman == "Satış" || p.Departman == "Dış Ticaret" || p.Rol == "Üst Yönetim" || p.Rol == "Yönetici" || p.Departman == "Satın Alma" || p.Departman == "Teknik Servis";
                    if (seyahat) { d.PasaportDurumu = p.Rol == "Üst Yönetim" ? "Yeşil (Hususi) Pasaport" : rnd.NextDouble() < 0.6 ? "Şirket Sorumluluğunda" : "Kendi Pasaportu"; d.PasaportNo = $"U{rnd.Next(10000000, 99999999)}"; d.PasaportBitis = bugun.AddDays(rnd.Next(-30, 2500)); d.PasaportKasada = d.PasaportDurumu == "Şirket Sorumluluğunda"; if (rnd.NextDouble() < 0.7) d.Vizeler.Add(new PdVize { Ulke = "Schengen", Tur = rnd.NextDouble() < 0.5 ? "Çok Girişli (C)" : "Tek Girişli", Bitis = bugun.AddDays(rnd.Next(-20, 700)) }); if (rnd.NextDouble() < 0.3) d.Vizeler.Add(new PdVize { Ulke = "ABD", Tur = "B1/B2", Bitis = bugun.AddDays(rnd.Next(100, 3000)) }); if (rnd.NextDouble() < 0.25) d.Vizeler.Add(new PdVize { Ulke = "Çin", Tur = "M (Ticari)", Bitis = bugun.AddDays(rnd.Next(-10, 300)) }); }
                    void B(string tur, int gunOnce, int? sureliGun) => d.Belgeler.Add(new PdBelge { No = ++_sonBelge, Tur = tur, Dosya = $"{p.PersonelNo.ToLowerInvariant()}-{tur.ToLowerInvariant().Replace(" ", "-").Replace("ş", "s").Replace("ğ", "g").Replace("ı", "i").Replace("ç", "c").Replace("ö", "o").Replace("ü", "u").Replace("İ", "i")}.pdf", Tip = "application/pdf", Boyut = rnd.Next(80, 900) * 1024L, Tarih = bugun.AddDays(-gunOnce), Kullanici = "İK · Derya Şen", GecerlilikBitis = sureliGun.HasValue ? bugun.AddDays(-gunOnce).AddDays(sureliGun.Value) : (DateTime?)null });
                    int giris = (int)(bugun - d.IseGiris.Value).TotalDays; B("İş Sözleşmesi", giris, null); B("SGK İşe Giriş Bildirgesi", giris, null); B("Kimlik Fotokopisi", giris, null); if (rnd.NextDouble() < 0.8) B("Diploma", giris, null); if (rnd.NextDouble() < 0.7) B("Adli Sicil Kaydı", giris, null); B("Sağlık Raporu", Math.Min(giris, rnd.Next(30, 400)), 365); if (rnd.NextDouble() < 0.85) B("İSG Eğitim Sertifikası", Math.Min(giris, rnd.Next(30, 800)), p.Departman == "Teknik Servis" || p.Departman == "Kalite" ? 365 : 1095); B("KVKK Aydınlatma", giris, null); if (d.PasaportDurumu != "Yok") B("Pasaport Fotokopisi", rnd.Next(30, 600), null); if (ZimmetOrnek.Kisinin(p.PersonelNo).Count > 0) B("Zimmet Tutanağı", rnd.Next(10, 400), null);
                    d.Egitimler.Add(new PdEgitim { No = 1, Ad = "Temel İSG Eğitimi", Tarih = bugun.AddDays(-rnd.Next(30, 700)), Bitis = bugun.AddDays(rnd.Next(-30, 700)), Kurum = "Güveniş OSGB", Sertifika = $"ISG-{rnd.Next(10000, 99999)}" }); if (p.Departman == "Teknik Servis" || p.Departman == "Kalite") d.Egitimler.Add(new PdEgitim { No = 2, Ad = "Yüksekte Çalışma", Tarih = bugun.AddDays(-rnd.Next(30, 500)), Bitis = bugun.AddDays(rnd.Next(0, 600)), Kurum = "Güveniş OSGB", Sertifika = $"YC-{rnd.Next(1000, 9999)}" }); if (p.Departman == "Bilgi Teknolojileri") d.Egitimler.Add(new PdEgitim { No = 2, Ad = "KVKK ve Bilgi Güvenliği", Tarih = bugun.AddDays(-rnd.Next(30, 300)), Kurum = "Kurum içi", Sertifika = "" });
                    d.Hareketler.Add(new PdHareket { Zaman = d.IseGiris.Value.AddHours(9), Kullanici = "İK · Derya Şen", Islem = "Dosya Açıldı", Aciklama = $"İşe giriş · {p.Unvan} · {p.Departman} · {p.Sirket}" });
                    _dosyalar[p.PersonelNo] = d; i++;
                }
            }
        }
        static string Eposta(NaKisi p) { var ad = p.AdSoyad.ToLowerInvariant().Replace("ı", "i").Replace("ş", "s").Replace("ğ", "g").Replace("ü", "u").Replace("ö", "o").Replace("ç", "c").Split(' '); string alan = p.Sirket == "Uras Holding" ? "urasholding.com" : p.Sirket == "Selvi" ? "selvikimya.com" : p.Sirket == "Alv Kimya" ? "alvkimya.com" : p.Sirket == "Avrupa Paper" ? "avrupapaper.com" : p.Sirket == "URS Makina" ? "ursmakine.com" : "uraskimya.com"; return $"{ad[0]}.{ad.Last()}@{alan}"; }
        public static PdDosya Dosya(string personelNo) { Tohumla(); lock (Kilit) { if (!_dosyalar.TryGetValue(personelNo ?? "", out var d)) { var p = NakitAvansOrnek.KisiBul(personelNo ?? ""); if (p == null) return null; d = new PdDosya { PersonelNo = p.PersonelNo }; _dosyalar[p.PersonelNo] = d; } return d; } }
        static void Log(PdDosya d, string islem, string aciklama, string kullanici) => d.Hareketler.Add(new PdHareket { Zaman = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });

        public static (bool ok, string mesaj) Kaydet(PdKayitIstek i, string kullanici)
        {
            var d = Dosya(i?.PersonelNo); if (d == null) return (false, "Personel bulunamadı.");
            lock (Kilit)
            {
                var degisen = new List<string>();
                void S(string ad, string eski, string yeni, Action ata) { if (yeni == null) return; yeni = yeni.Trim(); if (yeni == (eski ?? "")) return; ata(); degisen.Add($"{ad}: {(string.IsNullOrEmpty(eski) ? "—" : eski)} → {(yeni == "" ? "—" : yeni)}"); }
                S("Telefon", d.Telefon, i.Telefon, () => d.Telefon = i.Telefon.Trim()); S("E-posta", d.Eposta, i.Eposta, () => d.Eposta = i.Eposta.Trim()); S("Adres", d.Adres, i.Adres, () => d.Adres = i.Adres.Trim()); S("Kan Grubu", d.KanGrubu, i.KanGrubu, () => d.KanGrubu = i.KanGrubu.Trim()); S("Acil Kişi", d.AcilKisi, i.AcilKisi, () => d.AcilKisi = i.AcilKisi.Trim()); S("Acil Telefon", d.AcilTelefon, i.AcilTelefon, () => d.AcilTelefon = i.AcilTelefon.Trim()); S("Ehliyet", d.EhliyetSinifi, i.EhliyetSinifi, () => d.EhliyetSinifi = i.EhliyetSinifi.Trim()); S("Not", d.Not, i.Not, () => d.Not = i.Not.Trim());
                S("Doğum Tarihi", d.DogumTarihi?.ToString("yyyy-MM-dd"), i.DogumTarihi, () => d.DogumTarihi = DateTime.TryParse(i.DogumTarihi, out var x) ? x.Date : (DateTime?)null); S("İşe Giriş", d.IseGiris?.ToString("yyyy-MM-dd"), i.IseGiris, () => d.IseGiris = DateTime.TryParse(i.IseGiris, out var x) ? x.Date : (DateTime?)null); S("Ehliyet Bitiş", d.EhliyetBitis?.ToString("yyyy-MM-dd"), i.EhliyetBitis, () => d.EhliyetBitis = DateTime.TryParse(i.EhliyetBitis, out var x) ? x.Date : (DateTime?)null);
                if (i.PasaportDurumu != null && PasaportDurumlari.Contains(i.PasaportDurumu)) S("Pasaport", d.PasaportDurumu, i.PasaportDurumu, () => d.PasaportDurumu = i.PasaportDurumu); S("Pasaport No", d.PasaportNo, i.PasaportNo, () => d.PasaportNo = i.PasaportNo.Trim()); S("Pasaport Bitiş", d.PasaportBitis?.ToString("yyyy-MM-dd"), i.PasaportBitis, () => d.PasaportBitis = DateTime.TryParse(i.PasaportBitis, out var x) ? x.Date : (DateTime?)null);
                if (i.PasaportKasada.HasValue && i.PasaportKasada.Value != d.PasaportKasada) { d.PasaportKasada = i.PasaportKasada.Value; degisen.Add("Pasaport şirket kasasında: " + (d.PasaportKasada ? "Evet" : "Hayır")); }
                if (i.Vizeler != null) { var yeni = i.Vizeler.Split('\n', ';').Select(x => x.Trim()).Where(x => x != "").Select(x => { var p = x.Split('|').Select(y => y.Trim()).ToArray(); return new PdVize { Ulke = p[0], Tur = p.Length > 1 ? p[1] : "", Bitis = p.Length > 2 && DateTime.TryParse(p[2], out var b) ? b.Date : DateTime.Today.AddYears(1) }; }).ToList(); if (string.Join(",", yeni.Select(v => v.Ulke + v.Tur + v.Bitis.ToString("yyyyMMdd"))) != string.Join(",", d.Vizeler.Select(v => v.Ulke + v.Tur + v.Bitis.ToString("yyyyMMdd")))) { d.Vizeler = yeni; degisen.Add($"Vizeler: {yeni.Count} kayıt"); } }
                if (degisen.Count == 0) return (true, "Değişiklik yok.");
                Log(d, "Dosya Güncellendi", string.Join(" · ", degisen), kullanici); return (true, $"{degisen.Count} alan güncellendi.");
            }
        }
        public static (bool ok, string mesaj) BelgeYukle(string personelNo, string tur, string gecerlilik, string dosyaAdi, string icerikTipi, byte[] veri, string kullanici)
        {
            var d = Dosya(personelNo); if (d == null) return (false, "Personel bulunamadı."); if (veri == null || veri.Length == 0) return (false, "Dosya seçin."); if (veri.LongLength > 12_000_000) return (false, "Dosya 12 MB'ı aşamaz.");
            lock (Kilit) { d.Belgeler.Add(new PdBelge { No = ++_sonBelge, Tur = string.IsNullOrWhiteSpace(tur) ? "Diğer" : tur.Trim(), Dosya = dosyaAdi, Tip = icerikTipi ?? "application/octet-stream", Boyut = veri.LongLength, Veri = veri, Tarih = DateTime.Now, Kullanici = kullanici, GecerlilikBitis = DateTime.TryParse(gecerlilik, out var g) ? g.Date : (DateTime?)null }); Log(d, "Belge Yüklendi", $"{tur}: {dosyaAdi}" + (DateTime.TryParse(gecerlilik, out var g2) ? $" · geçerlilik {g2:dd.MM.yyyy}" : ""), kullanici); }
            return (true, $"{tur} yüklendi ({dosyaAdi}).");
        }
        public static (bool ok, string mesaj) EgitimEkle(PdEgitimIstek i, string kullanici)
        {
            var d = Dosya(i?.PersonelNo); if (d == null) return (false, "Personel bulunamadı."); if (string.IsNullOrWhiteSpace(i.Ad)) return (false, "Eğitim adı girin.");
            lock (Kilit) { d.Egitimler.Add(new PdEgitim { No = d.Egitimler.Count + 1, Ad = i.Ad.Trim(), Tarih = DateTime.TryParse(i.Tarih, out var t) ? t.Date : DateTime.Today, Bitis = DateTime.TryParse(i.Bitis, out var b) ? b.Date : (DateTime?)null, Kurum = (i.Kurum ?? "").Trim(), Sertifika = (i.Sertifika ?? "").Trim() }); Log(d, "Eğitim Eklendi", $"{i.Ad.Trim()} · {i.Kurum}", kullanici); }
            return (true, $"{i.Ad.Trim()} eğitimi eklendi.");
        }

        public static object Ozet(NaKisi p)
        {
            var d = Dosya(p.PersonelNo); var z = ZimmetOrnek.Kisinin(p.PersonelNo); var arac = AracBakimOrnek.Araclar.Where(a => (a.Zimmet ?? "").Contains(p.AdSoyad)).ToList(); var avans = NakitAvansOrnek.AcikAvanslar(p);
            return new { personelNo = p.PersonelNo, ad = p.AdSoyad, sirket = p.Sirket, departman = p.Departman, unvan = p.Unvan, rol = p.Rol, aktif = p.Aktif, telefon = d.Telefon, eposta = d.Eposta, iseGiris = d.IseGiris?.ToString("yyyy-MM-dd"), kidemYil = d.IseGiris.HasValue ? Math.Round((DateTime.Today - d.IseGiris.Value).TotalDays / 365.25, 1) : 0, zimmetAdet = z.Count, zimmetDeger = z.Sum(v => v.Deger), zimmetTurler = z.Select(v => v.Tur).Distinct().ToList(), araclar = arac.Select(a => new { a.Plaka, arac = $"{a.Marka} {a.Model}", a.Km, a.Durum }).ToList(), pasaport = d.PasaportDurumu, pasaportKasada = d.PasaportKasada, pasaportBitis = d.PasaportBitis?.ToString("yyyy-MM-dd"), pasaportKalan = d.PasaportBitis.HasValue ? (int?)(d.PasaportBitis.Value.Date - DateTime.Today).Days : null, vize = d.Vizeler.Count, vizeDolan = d.Vizeler.Count(v => v.Bitis < DateTime.Today), ehliyet = d.EhliyetSinifi, ehliyetDolmus = d.EhliyetBitis.HasValue && d.EhliyetBitis < DateTime.Today, belge = d.Belgeler.Count, belgeDolan = d.Belgeler.Count(b => b.Durum == "Süresi Doldu"), belgeYaklasan = d.Belgeler.Count(b => b.Durum == "Süresi Yaklaşıyor"), belgeEksik = new[] { "İş Sözleşmesi", "SGK İşe Giriş Bildirgesi", "Kimlik Fotokopisi", "Sağlık Raporu", "KVKK Aydınlatma" }.Count(t => !d.Belgeler.Any(b => b.Tur == t)), egitim = d.Egitimler.Count, egitimDolan = d.Egitimler.Count(e => e.Bitis.HasValue && e.Bitis < DateTime.Today), acikAvans = avans.Count, acikAvansTutar = avans.Sum(a => a.OdemeBekleyen + (a.TeslimEdilen - (a.Kapandi ? a.TeslimEdilen : 0))) };
        }
        public static object Json(NaKisi p)
        {
            var d = Dosya(p.PersonelNo); var z = ZimmetOrnek.Kisinin(p.PersonelNo); var arac = AracBakimOrnek.Araclar.Where(a => (a.Zimmet ?? "").Contains(p.AdSoyad)).ToList(); var avans = NakitAvansOrnek.AcikAvanslar(p); var bugun = DateTime.Today;
            return new
            {
                ozet = Ozet(p), telefon = d.Telefon, eposta = d.Eposta, adres = d.Adres, dogumTarihi = d.DogumTarihi?.ToString("yyyy-MM-dd"), iseGiris = d.IseGiris?.ToString("yyyy-MM-dd"), kanGrubu = d.KanGrubu, acilKisi = d.AcilKisi, acilTelefon = d.AcilTelefon, ehliyetSinifi = d.EhliyetSinifi, ehliyetBitis = d.EhliyetBitis?.ToString("yyyy-MM-dd"), masrafMerkezi = p.MasrafMerkezi, kullanici = p.KullaniciId, not = d.Not,
                pasaportDurumu = d.PasaportDurumu, pasaportNo = d.PasaportNo, pasaportBitis = d.PasaportBitis?.ToString("yyyy-MM-dd"), pasaportKasada = d.PasaportKasada, vizeler = d.Vizeler.Select(v => new { v.Ulke, v.Tur, bitis = v.Bitis.ToString("yyyy-MM-dd"), kalan = (v.Bitis.Date - bugun).Days }).ToList(),
                zimmetler = z.Select(v => new { v.Id, no = v.No, tur = v.Tur, marka = v.Marka, model = v.Model, seriNo = v.SeriNo, deger = v.Deger, zimmetTarihi = v.ZimmetTarihi?.ToString("yyyy-MM-dd"), garantiBitis = v.GarantiBitis?.ToString("yyyy-MM-dd") }).ToList(), zimmetDeger = z.Sum(v => v.Deger),
                araclar = arac.Select(a => { dynamic o = AracBakimOrnek.AracOzet(a); var s = SigortaOrnek.AracPoliceleri(a.Plaka); return new { a.Plaka, arac = $"{a.Marka} {a.Model} ({a.Yil})", a.Km, a.Durum, a.Lokasyon, bakimDurum = (string)o.bakimDurum, muayeneBitis = a.MuayeneBitis.ToString("yyyy-MM-dd"), sigorta = s }; }).ToList(),
                avanslar = avans.Select(a => new { a.No, a.TalepTuru, a.Konu, a.Tutar, a.ParaBirimi, a.OdemeBekleyen, a.TeslimEdilen, a.OdemeDurumu, a.KapatmaDurumu }).ToList(),
                belgeler = d.Belgeler.OrderBy(b => b.Tur).Select(b => new { b.No, b.Tur, b.Dosya, b.Boyut, tarih = b.Tarih.ToString("yyyy-MM-dd"), b.Kullanici, bitis = b.GecerlilikBitis?.ToString("yyyy-MM-dd"), durum = b.Durum, gercek = b.Veri != null }).ToList(), belgeTurleri = BelgeTurleri, eksikBelgeler = new[] { "İş Sözleşmesi", "SGK İşe Giriş Bildirgesi", "Kimlik Fotokopisi", "Sağlık Raporu", "KVKK Aydınlatma", "İSG Eğitim Sertifikası" }.Where(t => !d.Belgeler.Any(b => b.Tur == t)).ToList(),
                egitimler = d.Egitimler.OrderByDescending(e => e.Tarih).Select(e => new { e.No, e.Ad, tarih = e.Tarih.ToString("yyyy-MM-dd"), bitis = e.Bitis?.ToString("yyyy-MM-dd"), e.Kurum, e.Sertifika, dolmus = e.Bitis.HasValue && e.Bitis < bugun }).ToList(),
                hareketler = d.Hareketler.OrderByDescending(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("yyyy-MM-dd HH:mm"), h.Kullanici, h.Islem, h.Aciklama }).ToList()
            };
        }
        public static DaBelge DosyaPdf(NaKisi p, string hazirlayan)
        {
            var d = Dosya(p.PersonelNo); var z = ZimmetOrnek.Kisinin(p.PersonelNo); var arac = AracBakimOrnek.Araclar.Where(a => (a.Zimmet ?? "").Contains(p.AdSoyad)).ToList();
            var b = new DaBelge { Baslik = "Personel Dosyası · " + p.AdSoyad, AltBaslik = $"{p.Unvan} · {p.Departman} · {p.Sirket} · Personel No {p.PersonelNo} · {p.MasrafMerkezi}", Sirket = p.Sirket, Hazirlayan = hazirlayan, Yatay = false };
            b.Ozet.Add(("İşe Giriş", d.IseGiris?.ToString("dd.MM.yyyy") ?? "—")); b.Ozet.Add(("Telefon", d.Telefon)); b.Ozet.Add(("E-posta", d.Eposta)); b.Ozet.Add(("Pasaport", d.PasaportDurumu + (d.PasaportKasada ? " (kasada)" : ""))); b.Ozet.Add(("Zimmet", $"{z.Count} varlık · {z.Sum(v => v.Deger).ToString("N0", Tr)} ₺")); b.Ozet.Add(("Araç", arac.Count > 0 ? string.Join(", ", arac.Select(a => a.Plaka)) : "—"));
            var k = new DaBlok { Baslik = "Kimlik ve İletişim", Kolonlar = { "Alan", "Değer" } }; foreach (var (a, v) in new[] { ("Doğum Tarihi", d.DogumTarihi?.ToString("dd.MM.yyyy")), ("Adres", d.Adres), ("Kan Grubu", d.KanGrubu), ("Acil Durum", $"{d.AcilKisi} · {d.AcilTelefon}"), ("Ehliyet", d.EhliyetSinifi == "" ? "Yok" : $"{d.EhliyetSinifi} · {d.EhliyetBitis:dd.MM.yyyy}"), ("Pasaport No / Bitiş", d.PasaportDurumu == "Yok" ? "—" : $"{d.PasaportNo} · {d.PasaportBitis:dd.MM.yyyy}"), ("Vizeler", string.Join(" · ", d.Vizeler.Select(v => $"{v.Ulke} {v.Tur} ({v.Bitis:dd.MM.yyyy})"))), ("Not", d.Not) }) k.Satirlar.Add(new[] { a, v ?? "—" }); b.Bloklar.Add(k);
            var zb = new DaBlok { Baslik = "Zimmetler", Kolonlar = { "No", "Tür", "Marka / Model", "Seri No", "Zimmet Tarihi", "Değer (₺)" }, Sag = { false, false, false, false, false, true } }; foreach (var v in z) zb.Satirlar.Add(new[] { v.No, v.Tur, $"{v.Marka} {v.Model}", v.SeriNo, v.ZimmetTarihi?.ToString("dd.MM.yyyy") ?? "", v.Deger.ToString("N0", Tr) }); foreach (var a in arac) zb.Satirlar.Add(new[] { a.Plaka, "Araç", $"{a.Marka} {a.Model}", a.Sasi, "", "" }); if (zb.Satirlar.Count > 0) b.Bloklar.Add(zb);
            var bb = new DaBlok { Baslik = "Belgeler", Kolonlar = { "Tür", "Dosya", "Tarih", "Geçerlilik", "Durum" } }; foreach (var x in d.Belgeler) bb.Satirlar.Add(new[] { x.Tur, x.Dosya, x.Tarih.ToString("dd.MM.yyyy"), x.GecerlilikBitis?.ToString("dd.MM.yyyy") ?? "Süresiz", x.Durum }); b.Bloklar.Add(bb);
            var eb = new DaBlok { Baslik = "Eğitimler", Kolonlar = { "Eğitim", "Tarih", "Geçerlilik", "Kurum", "Sertifika" } }; foreach (var e in d.Egitimler) eb.Satirlar.Add(new[] { e.Ad, e.Tarih.ToString("dd.MM.yyyy"), e.Bitis?.ToString("dd.MM.yyyy") ?? "Süresiz", e.Kurum, e.Sertifika }); if (eb.Satirlar.Count > 0) b.Bloklar.Add(eb);
            return b;
        }
    }
}
