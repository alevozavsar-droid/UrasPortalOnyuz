// ============================================================
// TAHSİLAT TAKİP KAYITLARI (17 Kas 2026) — Tahsilat › Aylık Süreç sekmesinde tahsilat sorumlusunun girdiği kayıtlar. Kullanıcı isteği:
//   "Tahsilat her ayın sonunda müşteri diyaloglarını not girsin, kendi de okuyabilmeli. Benim süreç akışım şöyle: 2. – 3. hafta müşterileri arasın,
//    ayın son haftası ödeme yapmayanlara otomatik vade farkı robotu çalıştırılsın, seçilen 1 – 2 müşteri dışında."
//   (1) GÖRÜŞME NOTLARI: müşteri + dönem (yyyy-MM) başına istenen sayıda not — kanal, görüşülen kişi, sonuç, ödeme sözü (tarih / tutar), not metni, giren ve zamanı.
//       Notu giren kendi notunu silebilir; notlar müşteri detayında ve Aylık Süreç › Görüşme Notları görünümünde okunur.
//   (2) ROBOT DIŞI MÜŞTERİLER: vade farkı robotunun (ay sonu vade farkı faturası; Data/TahsilatOrnek.cs) o ay ya da sürekli atlayacağı müşteriler, nedeni ve kimin işaretlediği.
// Saklama: App_Data/tahsilat-takip.json (PersonelMaliyetKalici.Dizin altında; PM_KALICI_DIZIN ile değiştirilebilir). Dizin yoksa kayıtlar yalnız bellekte durur.
// ÖRNEK NOTLAR: ekran boş görünmesin diye birkaç örnek not bellekte üretilir (Ornek = true, "Örnek" etiketiyle gösterilir); dosyaya YAZILMAZ, silinemez. Kullanıcının notları dosyadadır.
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WebApplication3.Data
{
    public static class TahsilatTakipKayit
    {
        public static readonly string[] Kanallar = { "Telefon", "E-posta", "WhatsApp", "Ziyaret", "Diğer" };
        public static readonly string[] Sonuclar = { "Ödeme Sözü Alındı", "Ödeme Yapıldı", "Tekrar Aranacak", "Ulaşılamadı", "İtiraz Var", "Bilgi Verildi" };

        public class Not
        {
            public int Id { get; set; }
            public string CariKodu { get; set; }
            /// <summary>Notun ait olduğu dönem (yyyy-MM): görüşme tarihinin ayı.</summary>
            public string Donem { get; set; }
            public DateTime Zaman { get; set; }
            public string Kullanici { get; set; }
            public string Kanal { get; set; }
            public string Gorusulen { get; set; }
            public string Sonuc { get; set; }
            /// <summary>Ödeme sözü tarihi (yyyy-MM-dd) ve tutarı (₺); söz alınmadıysa boş / 0.</summary>
            public string SozTarihi { get; set; }
            public decimal SozTutar { get; set; }
            public string Metin { get; set; }
            /// <summary>Bellekte üretilen örnek not (dosyaya yazılmaz).</summary>
            public bool Ornek { get; set; }
        }
        public class Haric
        {
            public string CariKodu { get; set; }
            /// <summary>Dönem (yyyy-MM); boş = sürekli (her ay robot dışı).</summary>
            public string Donem { get; set; }
            public string Neden { get; set; }
            public string Kullanici { get; set; }
            public DateTime Zaman { get; set; }
        }
        // ---------- MÜŞTERİ İLETİŞİMİ (19 Kas 2026) ----------
        // Kullanıcı: "Aylık Süreç'e dinamik bir yapı: WhatsApp mı, mail mi, ikisi mi seçeyim; mutabakat yapsın, müşteri ödeyeceği tutarı yazsın buraya otomatik gelsin ya da itiraz etsin;
        //   cari ekstreyi, ödeme planı tahsilat simülasyonunu, ödeme planı faturalarını buradan göndereyim; müşteri WhatsApp'tan talep edince bana uyarı düşsün, otomatik göndersin."
        //   Iletisim: giden / gelen her mesaj (kanal, içerik türleri, metin, bağlantılar, yanıt türü, ödeme sözü). Talep: müşterinin istediği belge (Bekliyor → Gönderildi / Otomatik Gönderildi).
        //   OtoYanit: talep gelince belge kendiliğinden gönderilsin mi. WhatsApp tarafı simülasyondur: gelen mesaj /Tahsilat/WhatsAppGelen ucuyla işlenir (gerçek webhook aynı uca bağlanır);
        //   giden mesaj için wa.me / mailto bağlantısı açılır ve kayıt düşer.
        public static readonly string[] IcerikTurleri = { "Mutabakat", "Cari Ekstre", "Ödeme Planı · Tahsilat Simülasyonu", "Ödeme Planı · Faturalar" };
        public class Iletisim
        {
            public int Id { get; set; }
            public string CariKodu { get; set; }
            public DateTime Zaman { get; set; }
            /// <summary>Giden | Gelen</summary>
            public string Yon { get; set; }
            /// <summary>WhatsApp | E-posta</summary>
            public string Kanal { get; set; }
            public string Kullanici { get; set; }
            /// <summary>Giden: IcerikTurleri'nden seçilenler (+ "Mesaj"); Gelen: yanıt türü — Ödeme Sözü | İtiraz | Belge Talebi | Mutabık | Mesaj.</summary>
            public List<string> Icerik { get; set; } = new List<string>();
            public string Metin { get; set; }
            /// <summary>Gönderilen belgelerin bağlantıları ("ad|url").</summary>
            public List<string> Baglantilar { get; set; } = new List<string>();
            public string SozTarihi { get; set; }
            public decimal SozTutar { get; set; }
            public bool Otomatik { get; set; }
            public int TalepId { get; set; }
            /// <summary>Gönderilen / yazan yetkili kişi (MtKisi.Id) ve adı · adresi ("Stefan Weber · 0554 …"); son / sık kullanılan kişi sıralaması buradan.</summary>
            public int KisiId { get; set; }
            public string Kime { get; set; } = "";
            /// <summary>Bellekte üretilen örnek yazışma (dosyaya yazılmaz).</summary>
            public bool Ornek { get; set; }
        }
        public class Talep
        {
            public int Id { get; set; }
            public string CariKodu { get; set; }
            public DateTime Zaman { get; set; }
            public string Kanal { get; set; }
            /// <summary>İstenen içerik (IcerikTurleri'nden).</summary>
            public string Tur { get; set; }
            public string Metin { get; set; }
            /// <summary>Bekliyor | Gönderildi | Otomatik Gönderildi</summary>
            public string Durum { get; set; } = "Bekliyor";
            public int IletisimId { get; set; }
            public DateTime? GonderimZamani { get; set; }
        }
        class Depo_ { public int SonId { get; set; } public List<Not> Notlar { get; set; } = new List<Not>(); public List<Haric> Haricler { get; set; } = new List<Haric>(); public List<Iletisim> Iletisimler { get; set; } = new List<Iletisim>(); public List<Talep> Talepler { get; set; } = new List<Talep>(); public bool OtoYanit { get; set; } = true;
            /// <summary>Müşteriye özel aylık vade farkı oranı (%); yoksa TahsilatOrnek.VadeFarkiAylikOran (19 Kas 2026: Vade Farkı Takibi › VF Oranı sütunu).</summary>
            public Dictionary<string, decimal> Oranlar { get; set; } = new Dictionary<string, decimal>(); }
        public static decimal? MusteriOran(string kod) { lock (Kilit) return Yukle().Oranlar.TryGetValue(kod ?? "", out var o) && o > 0 ? o : (decimal?)null; }
        /// <summary>oran null ya da 0 → müşteriye özel oran silinir (varsayılana döner).</summary>
        public static void OranKaydet(string kod, decimal? oran) { lock (Kilit) { var d = Yukle(); if (oran == null || oran <= 0) d.Oranlar.Remove(kod ?? ""); else d.Oranlar[kod ?? ""] = Math.Round(oran.Value, 2); Yaz(); } }

        static readonly object Kilit = new object();
        static Depo_ _d; static List<Not> _ornek;
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, WriteIndented = true };
        static string Dosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data", "tahsilat-takip.json");

        static Depo_ Yukle()
        {
            if (_d != null) return _d;
            _d = new Depo_();
            try { var f = Dosya; if (f != null && File.Exists(f)) _d = JsonSerializer.Deserialize<Depo_>(File.ReadAllText(f), Secenek) ?? new Depo_(); }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] Tahsilat takip kayıtları okunamadı: {ex.Message}"); }
            _d.Notlar ??= new List<Not>(); _d.Haricler ??= new List<Haric>(); _d.Iletisimler ??= new List<Iletisim>(); _d.Talepler ??= new List<Talep>(); _d.Oranlar ??= new Dictionary<string, decimal>();
            return _d;
        }
        static void Yaz()
        {
            var f = Dosya; if (f == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(f)); File.WriteAllText(f, JsonSerializer.Serialize(_d, Secenek)); }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] Tahsilat takip kayıtları yazılamadı: {ex.Message}"); }
        }

        /// <summary>Örnek notlar (bellekte; bu ay arama haftalarında ve geçen ay yapılmış görüşmeler). Bugünden ileri tarihli olan üretilmez.</summary>
        static List<Not> Ornekler()
        {
            if (_ornek != null) return _ornek;
            var l = new List<Not>(); var b = DateTime.Today; var ay = new DateTime(b.Year, b.Month, 1); var gecen = ay.AddMonths(-1); int id = 0;
            void N(DateTime gun, int saat, string kod, string kanal, string kisi, string sonuc, string metin, int sozGun = 0, decimal soz = 0)
            {
                var z = gun.AddHours(saat).AddMinutes(10 + id * 7 % 45); if (z.Date > b) return;
                l.Add(new Not { Id = -(++id), CariKodu = kod, Donem = z.ToString("yyyy-MM"), Zaman = z, Kullanici = "Nazlı Erdem", Kanal = kanal, Gorusulen = kisi, Sonuc = sonuc, SozTarihi = sozGun > 0 ? gun.AddDays(sozGun).ToString("yyyy-MM-dd") : "", SozTutar = soz, Metin = metin, Ornek = true });
            }
            N(ay.AddDays(8), 10, "120.008", "Telefon", "Stefan Weber", "Ödeme Sözü Alındı", "Eski açık kalem için mutabakat istedi; güncel faturaları ay sonundan önce ödeyeceğini söyledi.", 16, 1_500_000m);
            N(ay.AddDays(9), 11, "120.014", "Telefon", "Karen Miller", "Ulaşılamadı", "İki kez arandı, yanıt yok; e-posta ile ekstre gönderildi.");
            N(ay.AddDays(10), 14, "120.012", "E-posta", "Ahmed Hassan", "Tekrar Aranacak", "Akreditif tahsilatının bankada beklediğini yazdı; hafta sonunda yeniden aranacak.");
            N(ay.AddDays(11), 10, "120.010", "Telefon", "Marco Rossi", "Ödeme Sözü Alındı", "Vadesi geçen fatura için haftaya havale yapacak.", 9, 1_750_000m);
            N(ay.AddDays(12), 15, "120.007", "Telefon", "Murat Güler", "İtiraz Var", "Vade farkı faturasına itiraz ediyor; satış temsilcisiyle görüşmek istiyor.");
            N(ay.AddDays(14), 9, "120.004", "WhatsApp", "Nihat Yavuz", "Ödeme Yapıldı", "Gecikmiş faturanın kalanını nakit ödedi; makbuz iletildi.");
            N(gecen.AddDays(13), 11, "120.008", "Telefon", "Stefan Weber", "Tekrar Aranacak", "Muhasebe sorumlusu izinde; dönünce ödeme planı bildirecek.");
            N(gecen.AddDays(16), 14, "120.014", "E-posta", "Karen Miller", "Bilgi Verildi", "Ekstre ve vade farkı hesabı gönderildi.");
            N(gecen.AddDays(17), 10, "120.006", "Telefon", "Pınar Ak", "Ödeme Sözü Alındı", "Grup içi mahsup ay sonunda yapılacak.", 12, 300_000m);
            return _ornek = l;
        }

        /// <summary>Bütün notlar (kullanıcı + örnek), en yeni başta.</summary>
        public static List<Not> Notlar() { lock (Kilit) return Yukle().Notlar.Concat(Ornekler()).OrderByDescending(n => n.Zaman).ToList(); }
        /// <summary>Not ekler. Dönüş: hata metni ya da null.</summary>
        public static string NotEkle(string kod, string kanal, string gorusulen, string sonuc, string sozTarihi, decimal sozTutar, string metin, string kullanici, out Not kayit)
        {
            kayit = null; metin = (metin ?? "").Trim(); if (string.IsNullOrEmpty(kod)) return "Müşteri seçin."; if (metin.Length < 3) return "Görüşme notunu yazın."; if (metin.Length > 2000) return "Not en çok 2.000 karakter olabilir.";
            if (!Kanallar.Contains(kanal ?? "")) kanal = Kanallar[0]; if (!Sonuclar.Contains(sonuc ?? "")) return "Görüşme sonucunu seçin.";
            DateTime soz = default; bool sozVar = !string.IsNullOrWhiteSpace(sozTarihi) && DateTime.TryParseExact(sozTarihi, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out soz);
            if (sonuc == "Ödeme Sözü Alındı" && !sozVar) return "Ödeme sözü tarihini girin."; if (sozTutar < 0) return "Söz tutarı sıfırdan küçük olamaz.";
            lock (Kilit)
            {
                var d = Yukle(); var z = DateTime.Now;
                kayit = new Not { Id = ++d.SonId, CariKodu = kod, Donem = z.ToString("yyyy-MM"), Zaman = z, Kullanici = kullanici ?? "", Kanal = kanal, Gorusulen = (gorusulen ?? "").Trim(), Sonuc = sonuc, SozTarihi = sozVar ? soz.ToString("yyyy-MM-dd") : "", SozTutar = sozVar ? Math.Round(sozTutar, 2) : 0, Metin = metin };
                d.Notlar.Add(kayit); Yaz();
            }
            return null;
        }
        /// <summary>Notu siler: yalnız notu giren kişi; örnek not silinmez. Dönüş: hata metni ya da null.</summary>
        public static string NotSil(int id, string kullanici)
        {
            lock (Kilit)
            {
                var d = Yukle(); var n = d.Notlar.FirstOrDefault(x => x.Id == id); if (n == null) return id < 0 ? "Örnek not silinemez." : "Not bulunamadı.";
                if (!string.Equals(n.Kullanici, kullanici ?? "", StringComparison.OrdinalIgnoreCase)) return $"Notu yalnız giren kişi ({n.Kullanici}) silebilir.";
                d.Notlar.Remove(n); Yaz(); return null;
            }
        }

        // ---------- iletişim kayıtları ----------
        public static bool OtoYanit { get { lock (Kilit) return Yukle().OtoYanit; } }
        public static void OtoYanitAyarla(bool acik) { lock (Kilit) { Yukle().OtoYanit = acik; Yaz(); } }
        public static List<Iletisim> Iletisimler(string kod = null) { lock (Kilit) return Yukle().Iletisimler.Concat(OrnekIletisimler()).Where(i => kod == null || i.CariKodu == kod).OrderByDescending(i => i.Zaman).ThenByDescending(i => i.Id).ToList(); }
        static List<Iletisim> _ornekIlt;
        /// <summary>Örnek yazışma (kullanıcı 19 Kas 2026: "WhatsApp / mail ayrı, 120.008 TextilDruck örnek ver"): WhatsApp ve e-posta iplikleri; bellekte, Ornek = true.</summary>
        static List<Iletisim> OrnekIletisimler()
        {
            if (_ornekIlt != null) return _ornekIlt;
            var l = new List<Iletisim>(); var b = DateTime.Now.Date; int id = 0;
            Iletisim I(int gunOnce, int saat, int dk, string yon, string kanal, string kullanici, string[] icerik, string metin, int kisiId, string kime, string[] bag = null, string sozT = "", decimal soz = 0, bool oto = false)
            { var i = new Iletisim { Id = -(++id), CariKodu = "120.008", Zaman = b.AddDays(-gunOnce).AddHours(saat).AddMinutes(dk), Yon = yon, Kanal = kanal, Kullanici = kullanici, Icerik = icerik.ToList(), Metin = metin, Baglantilar = (bag ?? new string[0]).ToList(), SozTarihi = sozT, SozTutar = soz, Otomatik = oto, KisiId = kisiId, Kime = kime, Ornek = true }; l.Add(i); return i; }
            string wa = "Stefan Weber · 0554 944 83 92", ml = "Mehmet Kaya · finans@textildruck.de";
            // WhatsApp ipliği (Stefan Weber)
            I(9, 10, 5, "Giden", "WhatsApp", "Nazlı Erdem", new[] { "Mutabakat", "Cari Ekstre" }, "Sayın Stefan Weber,\nTextilDruck Handels GmbH (120.008) cari hesabınızın bakiyesi ve güncel ekstreniz ektedir. Mutabakat yanıtınız için bağlantıyı kullanabilirsiniz.", 11, wa, new[] { "Cari Ekstre (PDF)|/Tahsilat/EkstrePdf?kod=120.008" });
            I(9, 11, 40, "Gelen", "WhatsApp", "Müşteri (WhatsApp)", new[] { "Ödeme Sözü" }, "Merhaba, ekstre elimize ulaştı. 1.500.000 TL'yi " + b.AddDays(6).ToString("dd.MM.yyyy") + " tarihinde havale edeceğiz.", 11, wa, null, b.AddDays(6).ToString("yyyy-MM-dd"), 1_500_000m);
            I(5, 9, 15, "Gelen", "WhatsApp", "Müşteri (WhatsApp)", new[] { "Belge Talebi" }, "Ödeme planı simülasyonunu gönderebilir misiniz?", 11, wa);
            I(5, 9, 15, "Giden", "WhatsApp", "Otomatik Yanıt", new[] { "Ödeme Planı · Tahsilat Simülasyonu" }, "Talebiniz üzerine Ödeme Planı · Tahsilat Simülasyonu bağlantıdadır.", 11, wa, new[] { "Ödeme Planı · Tahsilat Simülasyonu (PDF)|/Tahsilat/OdemePlaniPdf?kod=120.008&tur=simulasyon" }, "", 0, true);
            I(2, 16, 20, "Gelen", "WhatsApp", "Müşteri (WhatsApp)", new[] { "İtiraz" }, "İtiraz: VFF-2026-0024 vade farkı faturası kayıtlarımızda yok, kontrol eder misiniz?", 11, wa);
            // E-posta ipliği (Mehmet Kaya · finans)
            I(12, 14, 0, "Giden", "E-posta", "Nazlı Erdem", new[] { "Cari Ekstre", "Ödeme Planı · Faturalar" }, "Sayın Mehmet Kaya,\nEkte cari ekstreniz ve açık faturalarınızın ödeme planı yer almaktadır. Vadesi geçen kalemler için ay sonuna kadar ödeme planınızı bildirmenizi rica ederiz.", 12, ml, new[] { "Cari Ekstre (PDF)|/Tahsilat/EkstrePdf?kod=120.008", "Ödeme Planı · Faturalar (PDF)|/Tahsilat/OdemePlaniPdf?kod=120.008&tur=faturalar" });
            I(11, 9, 30, "Gelen", "E-posta", "Müşteri (E-posta)", new[] { "Mesaj" }, "Ekstre alındı, muhasebemiz kontrol ediyor; farklı kalem çıkarsa döneceğiz.", 12, ml);
            I(4, 15, 10, "Giden", "E-posta", "Nazlı Erdem", new[] { "Mutabakat" }, "Sayın Mehmet Kaya,\n" + b.AddDays(-4).ToString("dd.MM.yyyy") + " itibarıyla bakiye mutabakatımız ektedir; yanıtınızı bağlantıdan iletebilirsiniz.", 12, ml);
            return _ornekIlt = l;
        }
        public static List<Talep> Talepler(string kod = null) { lock (Kilit) return Yukle().Talepler.Where(t => kod == null || t.CariKodu == kod).OrderByDescending(t => t.Zaman).ToList(); }
        public static Talep TalepBul(int id) { lock (Kilit) return Yukle().Talepler.FirstOrDefault(t => t.Id == id); }
        /// <summary>İletişim kaydı (giden ya da gelen). Not metni 4.000 karakterle sınırlıdır.</summary>
        public static Iletisim IletisimEkle(string kod, string yon, string kanal, string kullanici, IEnumerable<string> icerik, string metin, IEnumerable<string> baglantilar = null, string sozTarihi = "", decimal sozTutar = 0, bool otomatik = false, int talepId = 0, int kisiId = 0, string kime = "")
        {
            lock (Kilit)
            {
                var d = Yukle(); var i = new Iletisim { Id = ++d.SonId, CariKodu = kod, Zaman = DateTime.Now, Yon = yon, Kanal = kanal, Kullanici = kullanici ?? "", Icerik = (icerik ?? new string[0]).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().ToList(), Metin = (metin ?? "").Trim(), Baglantilar = (baglantilar ?? new string[0]).ToList(), SozTarihi = sozTarihi ?? "", SozTutar = sozTutar, Otomatik = otomatik, TalepId = talepId, KisiId = kisiId, Kime = kime ?? "" };
                if (i.Metin.Length > 4000) i.Metin = i.Metin.Substring(0, 4000);
                d.Iletisimler.Add(i); Yaz(); return i;
            }
        }
        public static Talep TalepEkle(string kod, string kanal, string tur, string metin)
        {
            lock (Kilit) { var d = Yukle(); var t = new Talep { Id = ++d.SonId, CariKodu = kod, Zaman = DateTime.Now, Kanal = kanal, Tur = tur, Metin = (metin ?? "").Trim() }; d.Talepler.Add(t); Yaz(); return t; }
        }
        public static void TalepGonderildi(int id, int iletisimId, bool otomatik)
        {
            lock (Kilit) { var t = Yukle().Talepler.FirstOrDefault(x => x.Id == id); if (t == null) return; t.Durum = otomatik ? "Otomatik Gönderildi" : "Gönderildi"; t.IletisimId = iletisimId; t.GonderimZamani = DateTime.Now; Yaz(); }
        }

        public static List<Haric> Haricler() { lock (Kilit) return Yukle().Haricler.ToList(); }
        /// <summary>Müşteri bu dönemde robot dışı mı (o döneme özel ya da sürekli kayıt). Yoksa null.</summary>
        public static Haric HaricMi(string kod, string donem) { lock (Kilit) return Yukle().Haricler.FirstOrDefault(h => h.CariKodu == kod && (string.IsNullOrEmpty(h.Donem) || h.Donem == donem)); }
        /// <summary>Müşteriyi robot dışı işaretler / işareti kaldırır (o dönem için ya da sürekli). Dışarıda bırakırken neden zorunlu. Dönüş: hata metni ya da null.</summary>
        public static string HaricAyarla(string kod, string donem, bool haric, string neden, bool surekli, string kullanici)
        {
            neden = (neden ?? "").Trim(); if (string.IsNullOrEmpty(kod)) return "Müşteri seçin."; if (haric && neden == "") return "Robot dışında bırakma nedenini yazın.";
            lock (Kilit)
            {
                var d = Yukle(); d.Haricler.RemoveAll(h => h.CariKodu == kod && (string.IsNullOrEmpty(h.Donem) || h.Donem == donem));
                if (haric) d.Haricler.Add(new Haric { CariKodu = kod, Donem = surekli ? "" : donem, Neden = neden, Kullanici = kullanici ?? "", Zaman = DateTime.Now });
                Yaz(); return null;
            }
        }
    }
}
