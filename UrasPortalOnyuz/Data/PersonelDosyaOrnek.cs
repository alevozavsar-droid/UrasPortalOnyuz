// ============================================================
// PERSONEL DOSYASI — her personel için tek dosya (İK ve İdari İşler › İnsan Kaynakları › Personel ve Organizasyon; /PersonelDosya)
// KİŞİLER (9 Kas 2026, kullanıcı: "Personel Dosyası da Konsolide İK Paneli'nden çeksin, senkronize olsun"): personel listesi ve kimlik / iletişim (ad, şirket = SGK işvereni,
//   bölüm, görev, işe giriş, telefon, e-posta, sicil / SGK / TC no, durum) Konsolide İK Paneli kayıtlarından okunur (Data/IkPersonelMaster.cs; dosya anahtarı cari kodu 335.xxx).
//   Dosyadan girilen telefon / e-posta / işe giriş Konsolide İK Paneli'ne yazılır (IkPersonelMaster.KimlikYaz; kalıcı günlük "kimlik") → kişi kartı ve tüm sekmeler aynı anda değişir.
//   Önceki örnek kullanıcı dosyaları (P1001… rastgele telefon, doğum tarihi, pasaport, belge) kaldırıldı: gerçek personel için kişisel veri uydurulmaz; adres, doğum tarihi, kan grubu,
//   acil durum, ehliyet, ŞİRKET PASAPORTU (kullanıyor mu, kasada mı, no, bitiş, vizeler), belgeler (dosya yükleme, geçerlilik), eğitimler ve günlük bu ekranda girilir ve burada tutulur.
// DİNAMİK: zimmetler Data/ZimmetOrnek.cs'den (cari koduyla), araç Data/AracBakimOrnek.cs'den (ad eşleşmesi), açık avanslar Data/NakitAvansOrnek.cs'den (NaKisi = cari kodu) canlı okunur.
// Ekran: Controllers/PersonelDosyaController.cs + Views/PersonelDosya/Index.cshtml + wwwroot/js/personel-dosya.js
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
    /// <summary>Dosyada tutulan alanlar (kimlik / iletişim Konsolide İK Paneli'ndedir; Telefon / Eposta / IseGiris alanları eski uyumluluk için durur, PM kişisinde kullanılmaz).</summary>
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
        /// <summary>Her dosyada bulunması beklenen belgeler (eksik sayısı bunlara göre).</summary>
        public static readonly string[] ZorunluBelgeler = { "İş Sözleşmesi", "SGK İşe Giriş Bildirgesi", "Kimlik Fotokopisi", "Sağlık Raporu", "KVKK Aydınlatma" };
        private static readonly Dictionary<string, PdDosya> _dosyalar = new Dictionary<string, PdDosya>(StringComparer.OrdinalIgnoreCase);
        private static int _sonBelge;

        /// <summary>Kişi: Konsolide İK Paneli kaydı (cari kodu 335.xxx, ad soyad ya da sicil no); yoksa null.</summary>
        public static PmPersonel Kisi(string no) => IkPersonelMaster.Bul(no);
        /// <summary>Kişinin dosyası (cari koduyla); ilk erişimde boş dosya açılır (günlükte "Dosya Açıldı").</summary>
        public static PdDosya Dosya(PmPersonel p)
        {
            if (p == null) return null;
            lock (Kilit)
            {
                if (!_dosyalar.TryGetValue(p.CariKodu, out var d))
                {
                    d = new PdDosya { PersonelNo = p.CariKodu };
                    d.Hareketler.Add(new PdHareket { Zaman = DateTime.Now, Kullanici = "Sistem", Islem = "Dosya Açıldı", Aciklama = $"Konsolide İK Paneli kaydından · {p.Gorev} · {p.Bolum} · {p.SgkIsvereni}" });
                    _dosyalar[p.CariKodu] = d;
                }
                return d;
            }
        }
        public static PdDosya Dosya(string personelNo) => Dosya(Kisi(personelNo));
        static void Log(PdDosya d, string islem, string aciklama, string kullanici) => d.Hareketler.Add(new PdHareket { Zaman = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });
        static string Bos(string s) => string.IsNullOrEmpty(s) ? "—" : s;

        /// <summary>Dosya kaydı: telefon / e-posta / işe giriş Konsolide İK Paneli'ne yazılır (tek kaynak; kimlik out parametresi kalıcı günlük için döner), diğer alanlar dosyada tutulur.</summary>
        public static (bool ok, string mesaj) Kaydet(PdKayitIstek i, string kullanici, out IkPersonelMaster.KimlikIstek kimlik)
        {
            kimlik = null; var p = Kisi(i?.PersonelNo); var d = Dosya(p); if (d == null) return (false, "Personel bulunamadı.");
            var degisen = new List<string>();
            // kimlik / iletişim → Konsolide İK Paneli (kişinin tüm dönem kayıtları)
            var k = new IkPersonelMaster.KimlikIstek { Sira = p.Sira };
            if (i.Telefon != null && i.Telefon.Trim() != (p.Telefon ?? "")) { k.Telefon = i.Telefon.Trim(); degisen.Add($"Telefon: {Bos(p.Telefon)} → {Bos(k.Telefon)}"); }
            if (i.Eposta != null && i.Eposta.Trim() != (p.Eposta ?? "")) { k.Eposta = i.Eposta.Trim(); degisen.Add($"E-posta: {Bos(p.Eposta)} → {Bos(k.Eposta)}"); }
            if (i.IseGiris != null && i.IseGiris.Trim() != (p.IseGiris?.ToString("yyyy-MM-dd") ?? "")) { k.IseGiris = i.IseGiris.Trim(); degisen.Add($"İşe Giriş: {Bos(p.IseGiris?.ToString("dd.MM.yyyy"))} → {(k.IseGiris == "" ? "—" : k.IseGiris)}"); }
            if (k.Telefon != null || k.Eposta != null || k.IseGiris != null) { var h = IkPersonelMaster.KimlikYaz(k, kullanici); if (h != null) return (false, h); kimlik = k; }
            lock (Kilit)
            {
                void S(string ad, string eski, string yeni, Action ata) { if (yeni == null) return; yeni = yeni.Trim(); if (yeni == (eski ?? "")) return; ata(); degisen.Add($"{ad}: {Bos(eski)} → {(yeni == "" ? "—" : yeni)}"); }
                S("Adres", d.Adres, i.Adres, () => d.Adres = i.Adres.Trim()); S("Kan Grubu", d.KanGrubu, i.KanGrubu, () => d.KanGrubu = i.KanGrubu.Trim()); S("Acil Kişi", d.AcilKisi, i.AcilKisi, () => d.AcilKisi = i.AcilKisi.Trim()); S("Acil Telefon", d.AcilTelefon, i.AcilTelefon, () => d.AcilTelefon = i.AcilTelefon.Trim()); S("Ehliyet", d.EhliyetSinifi, i.EhliyetSinifi, () => d.EhliyetSinifi = i.EhliyetSinifi.Trim()); S("Not", d.Not, i.Not, () => d.Not = i.Not.Trim());
                S("Doğum Tarihi", d.DogumTarihi?.ToString("yyyy-MM-dd"), i.DogumTarihi, () => d.DogumTarihi = DateTime.TryParse(i.DogumTarihi, out var x) ? x.Date : (DateTime?)null); S("Ehliyet Bitiş", d.EhliyetBitis?.ToString("yyyy-MM-dd"), i.EhliyetBitis, () => d.EhliyetBitis = DateTime.TryParse(i.EhliyetBitis, out var x) ? x.Date : (DateTime?)null);
                if (i.PasaportDurumu != null && PasaportDurumlari.Contains(i.PasaportDurumu)) S("Pasaport", d.PasaportDurumu, i.PasaportDurumu, () => d.PasaportDurumu = i.PasaportDurumu); S("Pasaport No", d.PasaportNo, i.PasaportNo, () => d.PasaportNo = i.PasaportNo.Trim()); S("Pasaport Bitiş", d.PasaportBitis?.ToString("yyyy-MM-dd"), i.PasaportBitis, () => d.PasaportBitis = DateTime.TryParse(i.PasaportBitis, out var x) ? x.Date : (DateTime?)null);
                if (i.PasaportKasada.HasValue && i.PasaportKasada.Value != d.PasaportKasada) { d.PasaportKasada = i.PasaportKasada.Value; degisen.Add("Pasaport şirket kasasında: " + (d.PasaportKasada ? "Evet" : "Hayır")); }
                if (i.Vizeler != null) { var yeni = i.Vizeler.Split('\n', ';').Select(x => x.Trim()).Where(x => x != "").Select(x => { var v = x.Split('|').Select(y => y.Trim()).ToArray(); return new PdVize { Ulke = v[0], Tur = v.Length > 1 ? v[1] : "", Bitis = v.Length > 2 && DateTime.TryParse(v[2], out var b) ? b.Date : DateTime.Today.AddYears(1) }; }).ToList(); if (string.Join(",", yeni.Select(v => v.Ulke + v.Tur + v.Bitis.ToString("yyyyMMdd"))) != string.Join(",", d.Vizeler.Select(v => v.Ulke + v.Tur + v.Bitis.ToString("yyyyMMdd")))) { d.Vizeler = yeni; degisen.Add($"Vizeler: {yeni.Count} kayıt"); } }
                if (degisen.Count == 0) return (true, "Değişiklik yok.");
                Log(d, "Dosya Güncellendi", string.Join(" · ", degisen) + (kimlik != null ? " · kimlik / iletişim Konsolide İK Paneli'ne yazıldı" : ""), kullanici); return (true, $"{degisen.Count} alan güncellendi." + (kimlik != null ? " Telefon / e-posta / işe giriş Konsolide İK Paneli'nde de değişti." : ""));
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

        // ---- canlı bağlar: zimmet (cari kodu), araç (ad eşleşmesi), açık avanslar (NaKisi = cari kodu)
        static List<ZmVarlik> Zimmetler(PmPersonel p) => ZimmetOrnek.Kisinin(p.CariKodu);
        static List<ArArac> Araclar(PmPersonel p) => AracBakimOrnek.Araclar.Where(a => (a.Zimmet ?? "").IndexOf(p.AdSoyad, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        static List<NakitAvans> Avanslar(PmPersonel p) => NakitAvansOrnek.AcikAvanslar(PersonelKesintiOrnek.NaKisi(p));

        /// <summary>Liste satırı: kimlik Konsolide İK Paneli'nden, dosya özetleri buradan, zimmet / araç / avans canlı.</summary>
        public static object Ozet(PmPersonel p)
        {
            var d = Dosya(p); var z = Zimmetler(p); var arac = Araclar(p); var avans = Avanslar(p);
            return new
            {
                personelNo = p.CariKodu, sira = p.Sira, ad = p.AdSoyad, sirket = p.SgkIsvereni, departman = p.Bolum, unvan = p.Gorev, rol = p.Unvan, aktif = p.Durum == "Aktif", durum = p.Durum, ayrilis = p.AyrilisTarihi?.ToString("yyyy-MM-dd"), fiiliGorevYeri = p.FiiliGorevYeri, catiDepartman = p.CatiDepartman, ozelDurum = p.OzelDurum, ozel = p.Ozel, panel = IkPersonelMaster.PanelBaglanti(p),
                telefon = p.Telefon ?? "", eposta = p.Eposta ?? "", iseGiris = p.IseGiris?.ToString("yyyy-MM-dd"), kidemYil = p.KidemYil ?? 0,
                zimmetAdet = z.Count, zimmetDeger = z.Sum(v => v.Deger), zimmetTurler = z.Select(v => v.Tur).Distinct().ToList(), araclar = arac.Select(a => new { a.Plaka, arac = $"{a.Marka} {a.Model}", a.Km, a.Durum }).ToList(),
                pasaport = d.PasaportDurumu, pasaportKasada = d.PasaportKasada, pasaportBitis = d.PasaportBitis?.ToString("yyyy-MM-dd"), pasaportKalan = d.PasaportBitis.HasValue ? (int?)(d.PasaportBitis.Value.Date - DateTime.Today).Days : null, vize = d.Vizeler.Count, vizeDolan = d.Vizeler.Count(v => v.Bitis < DateTime.Today),
                ehliyet = d.EhliyetSinifi, ehliyetDolmus = d.EhliyetBitis.HasValue && d.EhliyetBitis < DateTime.Today,
                belge = d.Belgeler.Count, belgeDolan = d.Belgeler.Count(b => b.Durum == "Süresi Doldu"), belgeYaklasan = d.Belgeler.Count(b => b.Durum == "Süresi Yaklaşıyor"), belgeEksik = ZorunluBelgeler.Count(t => !d.Belgeler.Any(b => b.Tur == t)),
                egitim = d.Egitimler.Count, egitimDolan = d.Egitimler.Count(e => e.Bitis.HasValue && e.Bitis < DateTime.Today),
                acikAvans = avans.Count, acikAvansTutar = avans.Sum(a => a.OdemeBekleyen + (a.TeslimEdilen - (a.Kapandi ? a.TeslimEdilen : 0)))
            };
        }
        /// <summary>Tek dosya (çekmece): kimlik Konsolide İK Paneli'nden (telefon / e-posta / işe giriş formdan değişince oraya yazılır), diğer alanlar dosyadan.</summary>
        public static object Json(PmPersonel p)
        {
            var d = Dosya(p); var z = Zimmetler(p); var arac = Araclar(p); var avans = Avanslar(p); var bugun = DateTime.Today;
            return new
            {
                ozet = Ozet(p), telefon = p.Telefon ?? "", eposta = p.Eposta ?? "", adres = d.Adres, dogumTarihi = d.DogumTarihi?.ToString("yyyy-MM-dd"), iseGiris = p.IseGiris?.ToString("yyyy-MM-dd"), kanGrubu = d.KanGrubu, acilKisi = d.AcilKisi, acilTelefon = d.AcilTelefon, ehliyetSinifi = d.EhliyetSinifi, ehliyetBitis = d.EhliyetBitis?.ToString("yyyy-MM-dd"),
                masrafMerkezi = string.IsNullOrEmpty(p.DepartmanKodu) ? p.Bolum : p.DepartmanKodu + " - " + (string.IsNullOrEmpty(p.DepartmanAciklamasi) ? p.Bolum : p.DepartmanAciklamasi), kullanici = p.SicilNo ?? "", cariKodu = p.CariKodu, sicilNo = p.SicilNo ?? "", sgkNo = p.SgkNo ?? "", tcKimlik = p.TcKimlik ?? "", sgkIsvereni = p.SgkIsvereni, fiiliGorevYeri = p.FiiliGorevYeri, unvan = p.Unvan, gorev = p.Gorev, bolum = p.Bolum, catiDepartman = p.CatiDepartman, durum = p.Durum, panel = IkPersonelMaster.PanelBaglanti(p), not = d.Not,
                pasaportDurumu = d.PasaportDurumu, pasaportNo = d.PasaportNo, pasaportBitis = d.PasaportBitis?.ToString("yyyy-MM-dd"), pasaportKasada = d.PasaportKasada, vizeler = d.Vizeler.Select(v => new { v.Ulke, v.Tur, bitis = v.Bitis.ToString("yyyy-MM-dd"), kalan = (v.Bitis.Date - bugun).Days }).ToList(),
                zimmetler = z.Select(v => new { v.Id, no = v.No, tur = v.Tur, marka = v.Marka, model = v.Model, seriNo = v.SeriNo, deger = v.Deger, zimmetTarihi = v.ZimmetTarihi?.ToString("yyyy-MM-dd"), garantiBitis = v.GarantiBitis?.ToString("yyyy-MM-dd") }).ToList(), zimmetDeger = z.Sum(v => v.Deger),
                araclar = arac.Select(a => { dynamic o = AracBakimOrnek.AracOzet(a); var s = SigortaOrnek.AracPoliceleri(a.Plaka); return new { a.Plaka, arac = $"{a.Marka} {a.Model} ({a.Yil})", a.Km, a.Durum, a.Lokasyon, bakimDurum = (string)o.bakimDurum, muayeneBitis = a.MuayeneBitis.ToString("yyyy-MM-dd"), sigorta = s }; }).ToList(),
                avanslar = avans.Select(a => new { a.No, a.TalepTuru, a.Konu, a.Tutar, a.ParaBirimi, a.OdemeBekleyen, a.TeslimEdilen, a.OdemeDurumu, a.KapatmaDurumu }).ToList(),
                belgeler = d.Belgeler.OrderBy(b => b.Tur).Select(b => new { b.No, b.Tur, b.Dosya, b.Boyut, tarih = b.Tarih.ToString("yyyy-MM-dd"), b.Kullanici, bitis = b.GecerlilikBitis?.ToString("yyyy-MM-dd"), durum = b.Durum, gercek = b.Veri != null }).ToList(), belgeTurleri = BelgeTurleri, eksikBelgeler = ZorunluBelgeler.Concat(new[] { "İSG Eğitim Sertifikası" }).Where(t => !d.Belgeler.Any(b => b.Tur == t)).ToList(),
                egitimler = d.Egitimler.OrderByDescending(e => e.Tarih).Select(e => new { e.No, e.Ad, tarih = e.Tarih.ToString("yyyy-MM-dd"), bitis = e.Bitis?.ToString("yyyy-MM-dd"), e.Kurum, e.Sertifika, dolmus = e.Bitis.HasValue && e.Bitis < bugun }).ToList(),
                hareketler = d.Hareketler.OrderByDescending(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("yyyy-MM-dd HH:mm"), h.Kullanici, h.Islem, h.Aciklama }).ToList()
            };
        }
        public static DaBelge DosyaPdf(PmPersonel p, string hazirlayan)
        {
            var d = Dosya(p); var z = Zimmetler(p); var arac = Araclar(p);
            var b = new DaBelge { Baslik = "Personel Dosyası · " + p.AdSoyad, AltBaslik = $"{p.Gorev} · {p.Bolum} · {p.SgkIsvereni} · Cari Kodu {p.CariKodu}{(string.IsNullOrEmpty(p.SicilNo) ? "" : " · Sicil " + p.SicilNo)} · {p.FiiliGorevYeri}", Sirket = p.SgkIsvereni, Hazirlayan = hazirlayan, Yatay = false };
            b.Ozet.Add(("İşe Giriş", p.IseGiris?.ToString("dd.MM.yyyy") ?? "—")); b.Ozet.Add(("Telefon", Bos(p.Telefon))); b.Ozet.Add(("E-posta", Bos(p.Eposta))); b.Ozet.Add(("Pasaport", d.PasaportDurumu + (d.PasaportKasada ? " (kasada)" : ""))); b.Ozet.Add(("Zimmet", $"{z.Count} varlık · {z.Sum(v => v.Deger).ToString("N0", Tr)} ₺")); b.Ozet.Add(("Araç", arac.Count > 0 ? string.Join(", ", arac.Select(a => a.Plaka)) : "—"));
            var k = new DaBlok { Baslik = "Kimlik ve İletişim (Konsolide İK Paneli)", Kolonlar = { "Alan", "Değer" } }; foreach (var (a, v) in new[] { ("Durum", p.Durum + (p.AyrilisTarihi.HasValue ? $" · {p.AyrilisTarihi:dd.MM.yyyy}" : "")), ("Unvan · Görev", $"{p.Unvan} · {p.Gorev}"), ("Çatı Departman · Bölüm", $"{p.CatiDepartman} · {p.Bolum}"), ("SGK No · TC Kimlik", $"{Bos(p.SgkNo)} · {Bos(p.TcKimlik)}"), ("Doğum Tarihi", d.DogumTarihi?.ToString("dd.MM.yyyy")), ("Adres", d.Adres), ("Kan Grubu", d.KanGrubu), ("Acil Durum", string.IsNullOrEmpty(d.AcilKisi) && string.IsNullOrEmpty(d.AcilTelefon) ? "" : $"{d.AcilKisi} · {d.AcilTelefon}"), ("Ehliyet", d.EhliyetSinifi == "" ? "Yok" : $"{d.EhliyetSinifi} · {d.EhliyetBitis:dd.MM.yyyy}"), ("Pasaport No / Bitiş", d.PasaportDurumu == "Yok" ? "—" : $"{d.PasaportNo} · {d.PasaportBitis:dd.MM.yyyy}"), ("Vizeler", string.Join(" · ", d.Vizeler.Select(v => $"{v.Ulke} {v.Tur} ({v.Bitis:dd.MM.yyyy})"))), ("Not", d.Not) }) k.Satirlar.Add(new[] { a, string.IsNullOrEmpty(v) ? "—" : v }); b.Bloklar.Add(k);
            var zb = new DaBlok { Baslik = "Zimmetler", Kolonlar = { "No", "Tür", "Marka / Model", "Seri No", "Zimmet Tarihi", "Değer (₺)" }, Sag = { false, false, false, false, false, true } }; foreach (var v in z) zb.Satirlar.Add(new[] { v.No, v.Tur, $"{v.Marka} {v.Model}", v.SeriNo, v.ZimmetTarihi?.ToString("dd.MM.yyyy") ?? "", v.Deger.ToString("N0", Tr) }); foreach (var a in arac) zb.Satirlar.Add(new[] { a.Plaka, "Araç", $"{a.Marka} {a.Model}", a.Sasi, "", "" }); if (zb.Satirlar.Count > 0) b.Bloklar.Add(zb);
            var bb = new DaBlok { Baslik = "Belgeler", Kolonlar = { "Tür", "Dosya", "Tarih", "Geçerlilik", "Durum" } }; foreach (var x in d.Belgeler) bb.Satirlar.Add(new[] { x.Tur, x.Dosya, x.Tarih.ToString("dd.MM.yyyy"), x.GecerlilikBitis?.ToString("dd.MM.yyyy") ?? "Süresiz", x.Durum }); if (bb.Satirlar.Count == 0) bb.Not = "Dosyaya belge yüklenmedi. Eksik: " + string.Join(", ", ZorunluBelgeler) + "."; b.Bloklar.Add(bb);
            var eb = new DaBlok { Baslik = "Eğitimler", Kolonlar = { "Eğitim", "Tarih", "Geçerlilik", "Kurum", "Sertifika" } }; foreach (var e in d.Egitimler) eb.Satirlar.Add(new[] { e.Ad, e.Tarih.ToString("dd.MM.yyyy"), e.Bitis?.ToString("dd.MM.yyyy") ?? "Süresiz", e.Kurum, e.Sertifika }); if (eb.Satirlar.Count > 0) b.Bloklar.Add(eb);
            return b;
        }
    }
}
