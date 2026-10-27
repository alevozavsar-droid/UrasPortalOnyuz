// ============================================================
// E-MUTABAKAT — kayıtlı iletişim bilgileriyle (ana mutabakat yetkilisinin e-postası / cep telefonu) sistem üzerinden otomatik mutabakat (Views/Rapor59/Index.cshtml, js/e-mutabakat.js)
//   Kanallar: E-posta (PDF ekstre + yanıt bağlantısı) ve WhatsApp (kısa mesaj + yanıt bağlantısı); ikisi birden gönderilebilir. Mesajlar şablondan üretilir ({cari} {bakiye} {tarih} {sirket} {link} …).
//   Gönderim MtGonderim olarak cariye yazılır (Kanal: E-posta | WhatsApp), yanıt Mutabık / İtirazlı; hatırlatma tekrar sayısını artırır. Ön yüz örneğinde gönderim ve yanıt simüle edilir
//   (gerçek sistemde SMTP / WhatsApp Business API); mailto: ve wa.me bağlantıları yine de cihazdaki posta / WhatsApp uygulamasını açar.
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    public class EmSablon { public string MailKonu { get; set; } public string MailGovde { get; set; } public string WaGovde { get; set; } public string HatirlatmaEk { get; set; } }

    public static class EMutabakatOrnek
    {
        private static readonly object Kilit = new object(); private static bool _tohumlandi;
        public static readonly string[] Kanallar = { "E-posta", "WhatsApp" };
        public static int HatirlatmaGunu = 5;    // yanıtsız gönderimde hatırlatma eşiği (gün)
        public static string YanitAdresi = "https://portal.urasgrup.com/mutabakat/yanit/";
        public static EmSablon Sablon = new EmSablon
        {
            MailKonu = "{sirket} · {tarih} tarihli cari hesap mutabakatı ({cari})",
            MailGovde = "Sayın {yetkili},\n\n{sirket} nezdindeki {kod} {cari} cari hesabınızın {tarih} tarihi itibarıyla bakiyesi aşağıdaki gibidir:\n\n{bakiyeler}\n{acik}\nEkte ayrıntılı cari hesap ekstreniz yer almaktadır. Kayıtlarınızla uyuşuyorsa \"Mutabıkız\", fark varsa \"İtiraz\" seçeneğiyle aşağıdaki bağlantıdan yanıt vermenizi rica ederiz:\n{link}\n\nSaygılarımızla,\n{gonderen}\n{sirket} · Muhasebe",
            WaGovde = "Merhaba {yetkili}, {sirket} olarak {kod} {cari} cari hesabınızın {tarih} itibarıyla bakiyesi: {bakiyeler}. {acikKisa}Mutabakat için lütfen yanıt bağlantısını kullanın: {link} · {gonderen}",
            HatirlatmaEk = "\n\nHatırlatma: {ilkTarih} tarihli mutabakat talebimize henüz yanıt alamadık; yanıtınızı bekliyoruz."
        };
        public static readonly string[] Alanlar = { "{yetkili}", "{kod}", "{cari}", "{sirket}", "{tarih}", "{bakiyeler}", "{acik}", "{acikKisa}", "{link}", "{gonderen}", "{ilkTarih}" };

        static int Seed(string s) => s.Aggregate(17, (a, ch) => unchecked(a * 31 + ch));
        public static MtKisi AnaYetkili(MtCari c) => c.Kisiler.FirstOrDefault(k => k.AnaYetkili) ?? c.Kisiler.FirstOrDefault(k => k.MutabakatYetkilisi) ?? c.Kisiler.FirstOrDefault();
        public static string Cep(MtKisi k) => string.IsNullOrWhiteSpace(k?.CepTelefonu) ? "" : k.CepTelefonu.Trim();
        /// <summary>wa.me için uluslararası biçim (05xx … → 905xx…).</summary>
        public static string WaNumara(string cep) { var d = new string((cep ?? "").Where(char.IsDigit).ToArray()); if (d.StartsWith("0")) d = "9" + d; else if (d.Length == 10) d = "90" + d; return d; }
        public static bool MailUygun(MtCari c) { var k = AnaYetkili(c); return k != null && !string.IsNullOrWhiteSpace(k.Eposta) && k.Eposta.Contains("@"); }
        public static bool WaUygun(MtCari c) => Cep(AnaYetkili(c)) != "";
        /// <summary>Carinin e-mutabakat gönderimleri (yalnız bu ekrandan gidenler: Kanal E-posta / WhatsApp).</summary>
        public static List<MtGonderim> Gonderimler(MtCari c) => c.Gonderimler.Where(g => Kanallar.Contains(g.Kanal)).OrderByDescending(g => g.Id).ToList();
        public static MtGonderim Son(MtCari c) => Gonderimler(c).FirstOrDefault();
        /// <summary>E-mutabakat durumu: Yanıt Bekliyor · Mutabık · İtirazlı · Gönderilmedi (+ Hatırlatma Zamanı bilgisi ayrı).</summary>
        public static string Durum(MtCari c) { var s = Son(c); return s == null ? "Gönderilmedi" : s.Sonuc == "Bekliyor" ? "Yanıt Bekliyor" : s.Sonuc; }
        public static bool HatirlatmaZamani(MtCari c) { var s = Son(c); return s != null && s.Sonuc == "Bekliyor" && (DateTime.Today - s.Tarih.Date).Days >= HatirlatmaGunu; }

        public static string BakiyeMetni(MtCari c) { var l = c.Dovizler.Where(d => d.Net != 0).Select(d => $"{d.ParaBirimi} {Math.Abs(d.Net).ToString("N2", CariKartOrnek.Tr)} {(d.Net > 0 ? "borç" : "alacak")}").ToList(); return l.Count == 0 ? "0,00 TL (bakiye yok)" : string.Join(" · ", l) + $" (TL karşılığı {Math.Abs(c.BakiyeTl).ToString("N2", CariKartOrnek.Tr)} ₺)"; }
        /// <summary>Şablonu doldurur: (konu, gövde). Kanal E-posta ya da WhatsApp; hatirlatma=true ise hatırlatma eki.</summary>
        public static (string Konu, string Govde) Metin(MtCari c, string kanal, string gonderen, bool hatirlatma = false)
        {
            var k = AnaYetkili(c); var kart = c.Kart; var acik = c.Dovizler.Sum(d => d.AcikFaturaAdet); var ilk = Son(c);
            string Doldur(string s) => (s ?? "").Replace("{yetkili}", string.IsNullOrWhiteSpace(k?.AdSoyad) ? "Yetkili" : k.AdSoyad).Replace("{kod}", c.CariKodu).Replace("{cari}", kart?.ResmiUnvan ?? kart?.KisaAd ?? c.CariKodu).Replace("{sirket}", kart?.Sirket ?? "Uras Grup")
                .Replace("{tarih}", DateTime.Today.ToString("dd.MM.yyyy")).Replace("{bakiyeler}", BakiyeMetni(c)).Replace("{acik}", acik > 0 ? $"Açık fatura: {acik} adet ({c.Dovizler.Where(d => d.AcikFaturaAdet > 0).Select(d => $"{d.ParaBirimi} {d.AcikFaturaTutar.ToString("N2", CariKartOrnek.Tr)}").Aggregate((a, b) => a + " · " + b)}).\n" : "")
                .Replace("{acikKisa}", acik > 0 ? $"Açık fatura {acik} adet. " : "").Replace("{link}", YanitAdresi + c.CariKodu.Replace(".", "") + "-" + Math.Abs(Seed(c.CariKodu + DateTime.Today.ToString("yyyyMMdd")) % 100000).ToString("00000")).Replace("{gonderen}", gonderen ?? "Muhasebe").Replace("{ilkTarih}", ilk?.Tarih.ToString("dd.MM.yyyy") ?? "");
            var g = Doldur(kanal == "WhatsApp" ? Sablon.WaGovde : Sablon.MailGovde) + (hatirlatma ? Doldur(kanal == "WhatsApp" ? Sablon.HatirlatmaEk.Trim() : Sablon.HatirlatmaEk) : "");
            return (kanal == "WhatsApp" ? "" : Doldur(Sablon.MailKonu), g);
        }

        /// <summary>Gönderim: kanal E-posta | WhatsApp | İkisi. Kanal için iletişim bilgisi yoksa hata. (Başarılı sayısı, hatalar, oluşan gönderimler.)</summary>
        public static (int Basarili, List<string> Hatalar, List<MtGonderim> Gidenler) Gonder(IEnumerable<string> kodlar, string kanal, string kullanici)
        {
            Tohumla(); var h = new List<string>(); var giden = new List<MtGonderim>(); int n = 0;
            var kanallar = kanal == "İkisi" ? Kanallar : new[] { Kanallar.Contains(kanal) ? kanal : "E-posta" };
            foreach (var kod in (kodlar ?? new List<string>()).Distinct())
            {
                var c = MutabakatOrnek.Bul(kod); if (c == null) { h.Add($"{kod}: cari bulunamadı."); continue; }
                var k = AnaYetkili(c); if (k == null) { h.Add($"{kod} {c.Kart?.KisaAd}: kayıtlı mutabakat yetkilisi yok."); continue; }
                var son = Son(c); if (son != null && son.Sonuc == "Bekliyor" && son.Tarih.Date == DateTime.Today) { h.Add($"{kod} {c.Kart?.KisaAd}: bugün zaten gönderildi ({son.BelgeNo}); hatırlatma kullanın."); continue; }
                bool biri = false;
                foreach (var kn in kanallar)
                {
                    if (kn == "E-posta" && !MailUygun(c)) { h.Add($"{kod} {c.Kart?.KisaAd}: e-posta adresi yok."); continue; }
                    if (kn == "WhatsApp" && !WaUygun(c)) { h.Add($"{kod} {c.Kart?.KisaAd}: cep telefonu yok."); continue; }
                    var m = Metin(c, kn, kullanici);
                    var g = MutabakatOrnek.GonderimEkle(c, new MtGonderim { Kanal = kn, Kime = kn == "E-posta" ? $"{k.AdSoyad} · {k.Eposta}" : $"{k.AdSoyad} · {Cep(k)}", Sonuc = "Bekliyor", YanitAciklama = "" }, kullanici);
                    giden.Add(g); biri = true;
                }
                if (biri) n++;
            }
            return (n, h, giden);
        }
        /// <summary>Hatırlatma: bekleyen son gönderimin tekrar sayısı artar, tarih yenilenir (aynı kanaldan).</summary>
        public static string Hatirlat(string kod, string kullanici)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return "Cari bulunamadı."; var s = Son(c); if (s == null || s.Sonuc != "Bekliyor") return "Yanıt bekleyen e-mutabakat yok.";
            lock (MutabakatOrnek.Kilit) { s.Tekrar++; s.Tarih = DateTime.Now; }
            MutabakatOrnek.LogYaz(c, kullanici, "E-Mutabakat Hatırlatma · " + s.Kanal, "", s.BelgeNo, $"{s.Tekrar}. hatırlatma → {s.Kime}");
            return null;
        }
        /// <summary>Cari yanıtı (bağlantıdan gelen ya da elle işlenen): Mutabık / İtirazlı (+ açıklama).</summary>
        public static string Yanit(string kod, int gonderimId, string sonuc, string aciklama, string kullanici)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return "Cari bulunamadı.";
            var g = c.Gonderimler.FirstOrDefault(x => x.Id == gonderimId) ?? Son(c); if (g == null) return "Gönderim bulunamadı."; if (g.Sonuc != "Bekliyor") return "Bu gönderim zaten yanıtlanmış.";
            if (sonuc != "Mutabık" && sonuc != "İtirazlı") return "Sonuç Mutabık ya da İtirazlı olmalı."; if (sonuc == "İtirazlı" && string.IsNullOrWhiteSpace(aciklama)) return "İtirazda fark açıklaması zorunludur.";
            lock (MutabakatOrnek.Kilit) { g.Sonuc = sonuc; g.YanitTarihi = DateTime.Now; g.YanitAciklama = (aciklama ?? "").Trim(); foreach (var e in c.Gonderimler.Where(x => x.Id != g.Id && Kanallar.Contains(x.Kanal) && x.Sonuc == "Bekliyor" && x.Tarih.Date == g.Tarih.Date)) { e.Sonuc = sonuc; e.YanitTarihi = g.YanitTarihi; e.YanitAciklama = "(" + g.Kanal + " yanıtıyla birlikte)"; } }
            MutabakatOrnek.LogYaz(c, kullanici, "E-Mutabakat Yanıtı · " + sonuc, "Bekliyor", sonuc, $"{g.BelgeNo} · {g.Kanal} · {g.YanitAciklama}");
            return null;
        }
        /// <summary>Yanıt senkronizasyonu (simülasyon): yanıt bağlantısından gelen cevaplar çekilir — en az 1 gün önce gönderilmiş bekleyenlerin bir kısmı deterministik olarak Mutabık / İtirazlı olur.</summary>
        public static (int Mutabik, int Itirazli) YanitlariCek(string kullanici)
        {
            Tohumla(); int m = 0, i = 0;
            foreach (var c in MutabakatOrnek.Cariler)
            {
                var s = Son(c); if (s == null || s.Sonuc != "Bekliyor" || s.Tarih.Date >= DateTime.Today) continue;
                int z = Math.Abs(Seed(c.CariKodu + s.Id)) % 10; if (z < 6) { Yanit(c.CariKodu, s.Id, "Mutabık", "Yanıt bağlantısından: Mutabıkız.", "Cari (" + s.Kanal + ")"); m++; }
                else if (z < 8) { Yanit(c.CariKodu, s.Id, "İtirazlı", "Yanıt bağlantısından: " + new[] { "Son fatura kayıtlarımızda yok.", "Ödememiz hesabınıza işlenmemiş görünüyor.", "Kur farkı kaydını kabul etmiyoruz.", "İade faturamız düşülmemiş." }[Math.Abs(Seed(c.CariKodu)) % 4], "Cari (" + s.Kanal + ")"); i++; }
            }
            return (m, i);
        }
        public static string KisiGuncelle(string kod, int kisiId, string eposta, string cep, bool? anaYetkili, string kullanici)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return "Cari bulunamadı."; var k = c.Kisiler.FirstOrDefault(x => x.Id == kisiId) ?? AnaYetkili(c); if (k == null) return "Kişi bulunamadı.";
            eposta = (eposta ?? "").Trim(); cep = (cep ?? "").Trim(); if (eposta != "" && !eposta.Contains("@")) return "E-posta adresi geçersiz."; if (cep != "" && WaNumara(cep).Length < 12) return "Cep telefonu geçersiz (05xx xxx xx xx).";
            string eski = $"{k.Eposta} / {k.CepTelefonu}{(k.AnaYetkili ? " / ana" : "")}";
            lock (MutabakatOrnek.Kilit) { k.Eposta = eposta; k.CepTelefonu = cep; if (anaYetkili == true) { foreach (var x in c.Kisiler) x.AnaYetkili = false; k.AnaYetkili = true; k.MutabakatYetkilisi = true; } }
            MutabakatOrnek.LogYaz(c, kullanici, "İletişim Bilgisi Güncellendi", eski, $"{k.Eposta} / {k.CepTelefonu}{(k.AnaYetkili ? " / ana" : "")}", k.AdSoyad);
            return null;
        }
        public static string SablonKaydet(EmSablon s, string kullanici)
        {
            if (s == null || string.IsNullOrWhiteSpace(s.MailGovde) || string.IsNullOrWhiteSpace(s.WaGovde)) return "E-posta ve WhatsApp gövdesi boş olamaz."; if (!s.MailGovde.Contains("{link}") || !s.WaGovde.Contains("{link}")) return "Şablonlarda {link} yer tutucusu bulunmalı (cari yanıt bağlantısı).";
            lock (Kilit) Sablon = new EmSablon { MailKonu = s.MailKonu ?? Sablon.MailKonu, MailGovde = s.MailGovde, WaGovde = s.WaGovde, HatirlatmaEk = s.HatirlatmaEk ?? Sablon.HatirlatmaEk };
            MutabakatOrnek.GenelLog.Insert(0, new MtLog { Zaman = DateTime.Now, Kullanici = kullanici ?? "", Islem = "E-Mutabakat Şablonu Güncellendi", Onceki = "", Yeni = "", Aciklama = "E-posta / WhatsApp şablonları", DurumGecisi = "" });
            return null;
        }

        /// <summary>Örnek geçmiş: bazı carilere e-posta / WhatsApp gönderimleri (Mutabık, İtirazlı, bekleyen, hatırlatma zamanı gelmiş).</summary>
        static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var bugun = DateTime.Today;
                foreach (var c in MutabakatOrnek.Cariler)
                {
                    int z = Math.Abs(Seed("em" + c.CariKodu)) % 10; var k = AnaYetkili(c); if (k == null || z >= 5) continue;
                    bool wa = WaUygun(c) && z % 2 == 1; string kn = wa ? "WhatsApp" : MailUygun(c) ? "E-posta" : wa ? "WhatsApp" : null; if (kn == null) continue;
                    var t = bugun.AddDays(-(z == 0 ? 2 : z == 1 ? 9 : z == 2 ? 14 : z == 3 ? 6 : 21)).AddHours(10 + z);
                    var g = MutabakatOrnek.GonderimEkle(c, new MtGonderim { Kanal = kn, Kime = kn == "E-posta" ? $"{k.AdSoyad} · {k.Eposta}" : $"{k.AdSoyad} · {Cep(k)}", Sonuc = "Bekliyor" }, "Nazlı Erdem", t);
                    if (z == 2 || z == 4) { g.Sonuc = "Mutabık"; g.YanitTarihi = t.AddDays(2); g.YanitAciklama = "Yanıt bağlantısından: Mutabıkız."; }
                    else if (z == 3) { g.Sonuc = "İtirazlı"; g.YanitTarihi = t.AddDays(1); g.YanitAciklama = "Yanıt bağlantısından: " + new[] { "Son fatura kayıtlarımızda yok.", "Ödememiz hesabınıza işlenmemiş görünüyor.", "Kur farkı kaydını kabul etmiyoruz." }[Math.Abs(Seed(c.CariKodu)) % 3]; }
                    else if (z == 1) g.Tekrar = 1;
                }
            }
        }
        public static void Hazirla() => Tohumla();
    }
}
