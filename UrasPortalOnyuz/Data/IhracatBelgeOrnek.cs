// ============================================================
// İHRACAT BELGE GÖNDERİM TAKİBİ — konsolide örnek veri (Satış ve Operasyon › Yurtdışı Satış › İhracat Operasyon Takibi; Yönetim › Özel Raporlar)
// Amaç: İhracat operasyonunun kargo firmalarıyla (DHL Express, UPS, FedEx, TNT; yurtiçi adreslere Yurtiçi Kargo / motorlu kurye) gönderdiği
// ihracat belgelerini — akreditif ibraz seti (orijinal konşimento, fatura, çeki listesi, menşe, poliçe …), vesaik tahsili evrakları, müşteriye
// giden orijinal evraklar, konsolosluk / ticaret odası tasdik gönderileri — tek listede takip etmek: ihracat dosyası, fatura, ödeme şekli,
// akreditif no ve İBRAZ SON TARİHİ, alıcı (amir banka / muhabir banka / müşteri / forwarder / gümrük müşaviri / konsolosluk / ticaret odası),
// belge seti, kargo firması, gönderim (takip) numarası, masraf, gönderen operasyon uzmanı, kargo hareketleri, teslim, banka sonucu
// (uygun / rezerv / ödendi) ve ibraz riski (kalan gün, geç ibraz).
// Her gönderim yurtdışı CARİ_MASTER müşteri kartına bağlıdır (Data/CariKartOrnek.cs); ihracatçı şirket kartın faaliyet ana grubuna göre bulunur.
// Durumlar: Hazırlanıyor | Kargoya Verildi | Yolda | Gümrükte | Teslim Edildi | Teslim Edilemedi | İptal ("İptal" toplama girmez).
// Kayıtlar bellek içindedir; SQL / ERP / kargo API bağlantısı yoktur. Ekran: Controllers/IhracatBelgeController.cs + Views/IhracatBelge/Index.cshtml
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Akreditif İbrazı", "Rezerv Bildirildi", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Gönderilen belge (belge seti satırı).</summary>
    public class IbBelge
    {
        public int Sira { get; set; }
        public string Tur { get; set; }             // Orijinal Konşimento (B/L) …
        public string No { get; set; }
        public int Adet { get; set; }
        public string Nitelik { get; set; }         // Orijinal | Kopya | Tasdikli Orijinal
        public string Not { get; set; }
    }

    /// <summary>Tek bir belge gönderimi.</summary>
    public class IbGonderim
    {
        public int Id { get; set; }
        public string No { get; set; }              // İBG-2026-00042
        public DateTime Tarih { get; set; }         // kargoya veriliş
        public string Sirket { get; set; }          // ihracatçı grup şirketi
        public string DosyaNo { get; set; }         // ihracat dosyası
        public string FaturaNo { get; set; }
        public DateTime FaturaTarihi { get; set; }
        public decimal FaturaTutari { get; set; }   // döviz
        public string ParaBirimi { get; set; }
        public decimal Kur { get; set; }
        public decimal FaturaTutariTl { get; set; }
        public string OdemeSekli { get; set; }      // Akreditif (L/C) | Vesaik Mukabili | Mal Mukabili | Peşin
        public string TeslimSekli { get; set; }     // FOB | CIF | CFR | EXW | DAP
        public DateTime YuklemeTarihi { get; set; } // konşimento tarihi
        public string CariKodu { get; set; }
        public string Cari { get; set; }
        public string Ulke { get; set; }
        public string Sehir { get; set; }
        // ---- alıcı
        public string AliciTipi { get; set; }       // Amir Banka (Akreditif) | Muhabir / Tahsil Bankası | Müşteri | Forwarder / Nakliyeci | Gümrük Müşaviri | Konsolosluk | Ticaret Odası
        public string Alici { get; set; }
        public string AliciAdres { get; set; }
        public string AliciUlke { get; set; }
        public string AliciKisi { get; set; }
        public string Amac { get; set; }            // Akreditif İbrazı | Vesaik Tahsili | Müşteriye Orijinal Evrak | Forwarder'a Evrak | Konsolosluk Tasdiki | Ticaret Odası Onayı | Gümrük Müşavirine Evrak
        // ---- akreditif
        public string AkreditifNo { get; set; }
        public string AmirBanka { get; set; }
        public string IhbarBankasi { get; set; }    // Türkiye'deki ihbar / teyit bankası
        public DateTime? IbrazSonTarihi { get; set; }   // belgelerin bankaya ibraz son günü (yükleme + 21 gün, akreditif vadesini aşamaz)
        public DateTime? AkreditifVadesi { get; set; }
        public string BankaDurumu { get; set; }     // İnceleniyor | Uygun Bulundu | Rezerv Bildirildi | Rezerv Kaldırıldı | Ödendi | —
        public string RezervNotu { get; set; }
        public DateTime? OdemeTarihi { get; set; }
        // ---- belge seti
        public List<IbBelge> Belgeler { get; set; } = new List<IbBelge>();
        public int BelgeAdedi => Belgeler.Sum(b => b.Adet);
        // ---- kargo
        public string KargoFirmasi { get; set; }
        public string GonderimTuru { get; set; }    // Express Kurye (Doküman) | Standart Kargo | Motorlu Kurye
        public string GonderimNo { get; set; }
        public string GonderimNoLink { get; set; }
        public int ZarfAdedi { get; set; }
        public decimal Agirlik { get; set; }
        public decimal Masraf { get; set; }         // TL
        public string MasrafPB { get; set; } = "TL";
        public decimal MasrafDoviz { get; set; }
        public string MasrafKime { get; set; }      // Gönderen | Alıcı
        public DateTime TahminiTeslim { get; set; }
        public DateTime? TeslimTarihi { get; set; }
        public string TeslimAlan { get; set; }
        public string Durum { get; set; }
        public string SonHareket { get; set; }
        public List<NmHareket> Hareketler { get; set; } = new List<NmHareket>();
        public string Gonderen { get; set; }        // ihracat operasyon uzmanı
        public string Onaylayan { get; set; }
        public string Not { get; set; }
        public string Kaynak { get; set; } = "ERP İhracat Modülü";
        public bool Gecerli => Durum != "İptal";
        public bool Teslim => Durum == "Teslim Edildi";
        public bool Yolda => Durum == "Hazırlanıyor" || Durum == "Kargoya Verildi" || Durum == "Yolda" || Durum == "Gümrükte";
        public int? TeslimSuresi => TeslimTarihi.HasValue ? (int?)(TeslimTarihi.Value - Tarih).Days : null;
        /// <summary>İbraz riski: Geç İbraz (teslim &gt; ibraz son tarihi) | Kritik (kalan ≤ 3 gün, teslim yok) | Süresinde | — (akreditif değil)</summary>
        public string IbrazRiski
        {
            get
            {
                if (!IbrazSonTarihi.HasValue) return "—";
                if (TeslimTarihi.HasValue) return TeslimTarihi.Value.Date > IbrazSonTarihi.Value.Date ? "Geç İbraz" : "Süresinde";
                if (!Gecerli) return "—";
                int kalan = (IbrazSonTarihi.Value.Date - DateTime.Today).Days;
                return kalan < 0 ? "Geç İbraz" : kalan <= 3 ? "Kritik" : "Süresinde";
            }
        }
        public int? IbrazKalanGun => IbrazSonTarihi.HasValue ? (int?)(IbrazSonTarihi.Value.Date - (TeslimTarihi?.Date ?? DateTime.Today)).Days : null;
    }

    public static class IhracatBelgeOrnek
    {
        public static readonly object Kilit = new object();
        public static string[] Sirketler => TeklifOrnek.Sirketler;

        // ---------- Seçim listeleri ----------
        public static readonly string[] Durumlar = NumuneOrnek.Durumlar;
        public static readonly string[] OdemeSekilleri = { "Akreditif (L/C)", "Vesaik Mukabili", "Mal Mukabili", "Peşin" };
        public static readonly string[] AliciTipleri = { "Amir Banka (Akreditif)", "Muhabir / Tahsil Bankası", "Müşteri", "Forwarder / Nakliyeci", "Gümrük Müşaviri", "Konsolosluk", "Ticaret Odası" };
        public static readonly string[] Amaclar = { "Akreditif İbrazı", "Vesaik Tahsili", "Müşteriye Orijinal Evrak", "Forwarder'a Evrak", "Gümrük Müşavirine Evrak", "Konsolosluk Tasdiki", "Ticaret Odası Onayı" };
        public static readonly string[] BankaDurumlari = { "İnceleniyor", "Uygun Bulundu", "Rezerv Bildirildi", "Rezerv Kaldırıldı", "Ödendi" };
        public static readonly string[] IbrazRiskleri = { "Süresinde", "Kritik", "Geç İbraz" };
        public static readonly string[] BelgeTurleri = { "Orijinal Konşimento (B/L)", "Ticari Fatura", "Çeki Listesi", "Menşe Şahadetnamesi", "A.TR Dolaşım Belgesi", "EUR.1 Dolaşım Belgesi", "Sigorta Poliçesi", "Analiz Sertifikası", "Poliçe (Draft)", "Sağlık Sertifikası", "Fumigasyon Sertifikası", "Konsolosluk Tasdikli Fatura", "Gümrük Beyannamesi (Nüsha)", "Navlun Faturası" };
        public static readonly string[] Gonderenler = { "Esra Kaya", "Deniz Çelik", "Buse Arslan" };
        public static readonly string[] GonderimTurleri = { "Express Kurye (Doküman)", "Standart Kargo", "Motorlu Kurye" };
        /// <summary>Kargo firmaları: ad, bölge, tür, takip sayfası (NumuneOrnek ile aynı bağlantılar).</summary>
        public static readonly (string Ad, string Bolge, string Tur, string Link)[] KargoFirmalari =
        {
            ("DHL Express", "Yurtdışı", "Express Kurye (Doküman)", "https://www.dhl.com/tr-tr/home/tracking.html?tracking-id="),
            ("UPS", "Yurtdışı", "Express Kurye (Doküman)", "https://www.ups.com/track?tracknum="),
            ("FedEx", "Yurtdışı", "Express Kurye (Doküman)", "https://www.fedex.com/fedextrack/?trknbr="),
            ("TNT", "Yurtdışı", "Express Kurye (Doküman)", "https://www.tnt.com/express/tr_tr/site/shipping-tools/tracking.html?searchType=con&cons="),
            ("Yurtiçi Kargo", "Yurtiçi", "Standart Kargo", "https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code="),
            ("MNG Kargo", "Yurtiçi", "Standart Kargo", "https://www.mngkargo.com.tr/gonderi-takip?code="),
            ("Motorlu Kurye", "Yurtiçi", "Motorlu Kurye", "")
        };
        public static string[] KargoAdlari => KargoFirmalari.Select(k => k.Ad).Distinct().ToArray();
        private static readonly Dictionary<string, (string Banka, string Sehir)> AmirBankalar = new Dictionary<string, (string, string)>
        {
            ["Mısır"] = ("Banque Misr", "Kahire"), ["Pakistan"] = ("Habib Bank Ltd.", "Karaçi"), ["Bangladeş"] = ("Eastern Bank Ltd.", "Dakka"), ["Almanya"] = ("Commerzbank AG", "Frankfurt"), ["İtalya"] = ("Intesa Sanpaolo", "Milano"),
            ["ABD"] = ("JPMorgan Chase", "New York"), ["Polonya"] = ("PKO Bank Polski", "Varşova"), ["Fas"] = ("Attijariwafa Bank", "Kazablanka")
        };
        private static readonly string[] IhbarBankalari = { "Garanti BBVA", "İş Bankası", "Yapı Kredi", "Akbank" };
        private static readonly string[] Forwarderlar = { "Kuehne + Nagel", "DSV", "DB Schenker", "Arkas Lojistik", "Ekol Lojistik" };
        private static readonly string[] RezervNotlari = { "Konşimento tarihi son yükleme tarihinden sonra", "Fatura tutarı akreditif tutarını aşıyor", "Menşe şahadetnamesinde ünvan farkı", "Geç ibraz (21 günlük süre aşıldı)", "Çeki listesinde net / brüt ağırlık tutarsız", "Sigorta poliçesi tutarı fatura değerinin %110'undan az", "Poliçe (draft) imzasız" };
        private static readonly Dictionary<string, string> TeslimSekilleri = new Dictionary<string, string> { ["Akreditif (L/C)"] = "CIF", ["Vesaik Mukabili"] = "CFR", ["Mal Mukabili"] = "FOB", ["Peşin"] = "EXW" };
        private static readonly HashSet<string> EurUlkeleri = new HashSet<string> { "Almanya", "İtalya", "İspanya", "Fransa", "Hollanda", "Belçika", "Polonya", "Romanya", "Bulgaristan", "Yunanistan", "Fas" };
        private static readonly HashSet<string> AbUlkeleri = new HashSet<string> { "Almanya", "İtalya", "İspanya", "Fransa", "Hollanda", "Belçika", "Polonya", "Romanya", "Bulgaristan", "Yunanistan" };
        private static decimal KurBul(string pb) => TeklifOrnek.Kurlar.TryGetValue(pb ?? "TL", out var k) ? k : 1m;

        // ---------- Konsolide liste ----------
        private static readonly Lazy<List<IbGonderim>> _liste = new Lazy<List<IbGonderim>>(Uret);
        public static List<IbGonderim> Liste => _liste.Value;
        public static IbGonderim Bul(string no) => Liste.FirstOrDefault(t => string.Equals(t.No, no, StringComparison.OrdinalIgnoreCase));
        public static IEnumerable<CariKart> Musteriler() { var kodlar = new HashSet<string>(Liste.Select(t => t.CariKodu)); return CariKartOrnek.Kartlar.Where(k => kodlar.Contains(k.Kod)).OrderBy(k => k.Kod); }

        private static string OdemeSekli(string vade) => vade == "Akreditif (L/C)" ? "Akreditif (L/C)" : vade == "Vesaik Mukabili" ? "Vesaik Mukabili" : vade == "Peşin" ? "Peşin" : "Mal Mukabili";

        private static List<IbGonderim> Uret()
        {
            var rnd = new Random(20260930);
            var bugun = DateTime.Today; var l = new List<IbGonderim>();
            DateTime Gun(DateTime ay, int g) => new DateTime(ay.Year, ay.Month, Math.Min(g, DateTime.DaysInMonth(ay.Year, ay.Month)));
            var musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" && k.Bolge == "Yurtdışı" && k.Faaliyetler.Count > 0).ToList();
            int dosya = 240, fatura = 26000100, lc = 0;
            string TakipNo(string firma)
            {
                switch (firma)
                {
                    case "DHL Express": return rnd.Next(10, 99).ToString() + rnd.Next(10000000, 99999999);
                    case "UPS": return "1Z" + new string(Enumerable.Range(0, 16).Select(_ => "0123456789ABCDEFGHJKLMNPRSTUVWXYZ"[rnd.Next(33)]).ToArray());
                    case "FedEx": return rnd.Next(100000, 999999).ToString() + rnd.Next(100000, 999999);
                    case "TNT": return "GD" + rnd.Next(100000000, 999999999);
                    case "Yurtiçi Kargo": return rnd.Next(100000, 999999).ToString() + rnd.Next(1000000, 9999999);
                    case "MNG Kargo": return rnd.Next(10000, 99999).ToString() + rnd.Next(10000, 99999);
                    default: return "MK-" + rnd.Next(10000, 99999);
                }
            }

            for (int k = bugun.Month - 1; k >= 0; k--)
            {
                var ay = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-k);
                foreach (var sirket in Sirketler)
                {
                    var adaylar = musteriler.Where(m => m.Faaliyetler.Any(f => TeklifOrnek.GrupSirketleri.TryGetValue(f.AnaGrup, out var ss) && ss[0] == sirket)).ToList();
                    if (adaylar.Count == 0) continue;
                    int n = sirket == "URS Makine" ? rnd.Next(0, 2) : rnd.Next(2, 5);
                    for (int i = 0; i < n; i++)
                    {
                        var kart = adaylar[rnd.Next(adaylar.Count)];
                        // ---- ihracat dosyası ve fatura
                        var yukleme = k == 0 && rnd.Next(2) == 0 ? Gun(ay, rnd.Next(Math.Max(1, bugun.Day - 12), bugun.Day + 1)) : Gun(ay, rnd.Next(1, 27));
                        if (yukleme > bugun) continue;
                        string pb = EurUlkeleri.Contains(kart.Ulke) ? "EUR" : "USD"; decimal kur = Math.Round(KurBul(pb) * (1 + rnd.Next(-3, 4) / 100m), 4);
                        string odeme = OdemeSekli(kart.VadeSarti);
                        var d = new { DosyaNo = $"IHR-{yukleme:yyyy}-{dosya++:0000}", FaturaNo = $"URS{yukleme:yyyy}{fatura++:000000000}", FaturaTarihi = yukleme.AddDays(-rnd.Next(0, 3)), Tutar = Math.Round((decimal)rnd.Next(15_000, 260_000) + rnd.Next(0, 100) / 100m, 2) };
                        // ---- gönderimler: akreditifte banka ibrazı (+ müşteriye kopya seti), vesaikte tahsil bankası, diğerlerinde müşteri / forwarder; ara sıra konsolosluk / ticaret odası
                        var gonderimler = new List<(string AliciTipi, string Amac, int GunSonra)>();
                        if (odeme == "Akreditif (L/C)") { gonderimler.Add(("Amir Banka (Akreditif)", "Akreditif İbrazı", rnd.Next(2, 9))); if (rnd.Next(3) == 0) gonderimler.Add(("Müşteri", "Müşteriye Orijinal Evrak", rnd.Next(2, 6))); }
                        else if (odeme == "Vesaik Mukabili") gonderimler.Add(("Muhabir / Tahsil Bankası", "Vesaik Tahsili", rnd.Next(1, 6)));
                        else gonderimler.Add((rnd.Next(4) == 0 ? "Forwarder / Nakliyeci" : "Müşteri", rnd.Next(4) == 0 ? "Forwarder'a Evrak" : "Müşteriye Orijinal Evrak", rnd.Next(1, 5)));
                        if (!AbUlkeleri.Contains(kart.Ulke) && rnd.Next(4) == 0) gonderimler.Insert(0, (rnd.Next(2) == 0 ? "Konsolosluk" : "Ticaret Odası", rnd.Next(2) == 0 ? "Konsolosluk Tasdiki" : "Ticaret Odası Onayı", -rnd.Next(1, 4)));
                        foreach (var g in gonderimler)
                        {
                            var t = yukleme.AddDays(g.GunSonra); if (t > bugun || t < new DateTime(bugun.Year, 1, 1)) continue;
                            bool yurtdisiAdres = g.AliciTipi != "Konsolosluk" && g.AliciTipi != "Ticaret Odası" && g.AliciTipi != "Gümrük Müşaviri";
                            var ib = new IbGonderim
                            {
                                Tarih = t, Sirket = sirket, DosyaNo = d.DosyaNo, FaturaNo = d.FaturaNo, FaturaTarihi = d.FaturaTarihi, FaturaTutari = d.Tutar, ParaBirimi = pb, Kur = kur, FaturaTutariTl = Math.Round(d.Tutar * kur, 2),
                                OdemeSekli = odeme, TeslimSekli = TeslimSekilleri[odeme], YuklemeTarihi = yukleme, CariKodu = kart.Kod, Cari = kart.KisaAd, Ulke = kart.Ulke, Sehir = kart.Sehir,
                                AliciTipi = g.AliciTipi, Amac = g.Amac, Gonderen = Gonderenler[rnd.Next(Gonderenler.Length)], Onaylayan = "İhracat Operasyon Müdürü", MasrafKime = "Gönderen", ZarfAdedi = 1
                            };
                            // alıcı
                            if (g.AliciTipi == "Amir Banka (Akreditif)") { var b = AmirBankalar.TryGetValue(kart.Ulke, out var ab) ? ab : (Banka: "Amir Banka", Sehir: kart.Sehir); ib.Alici = b.Banka; ib.AliciAdres = $"{b.Banka} · Trade Finance Dept. · {b.Sehir}"; ib.AliciUlke = kart.Ulke; ib.AliciKisi = "Trade Finance / L/C Department"; }
                            else if (g.AliciTipi == "Muhabir / Tahsil Bankası") { var b = AmirBankalar.TryGetValue(kart.Ulke, out var ab) ? ab : (Banka: "Tahsil Bankası", Sehir: kart.Sehir); ib.Alici = b.Banka; ib.AliciAdres = $"{b.Banka} · Documentary Collections · {b.Sehir}"; ib.AliciUlke = kart.Ulke; ib.AliciKisi = "Collections Department"; }
                            else if (g.AliciTipi == "Müşteri") { ib.Alici = kart.ResmiUnvan; ib.AliciAdres = $"{kart.ResmiUnvan} · {kart.Sehir} / {kart.Ulke}"; ib.AliciUlke = kart.Ulke; ib.AliciKisi = kart.Yetkili; }
                            else if (g.AliciTipi == "Forwarder / Nakliyeci") { var fw = Forwarderlar[rnd.Next(Forwarderlar.Length)]; ib.Alici = fw; ib.AliciAdres = $"{fw} · {kart.Sehir} Ofisi"; ib.AliciUlke = kart.Ulke; ib.AliciKisi = "Import Operations"; }
                            else if (g.AliciTipi == "Konsolosluk") { ib.Alici = $"{kart.Ulke} Başkonsolosluğu"; ib.AliciAdres = $"{kart.Ulke} Başkonsolosluğu Ticaret Ataşeliği · İstanbul"; ib.AliciUlke = "Türkiye"; ib.AliciKisi = "Tasdik Servisi"; }
                            else { ib.Alici = "İstanbul Ticaret Odası"; ib.AliciAdres = "İTO Dış Ticaret Belgeleri Servisi · Eminönü / İstanbul"; ib.AliciUlke = "Türkiye"; ib.AliciKisi = "Belge Onay Servisi"; }
                            // akreditif bilgileri
                            if (odeme == "Akreditif (L/C)")
                            {
                                var b = AmirBankalar.TryGetValue(kart.Ulke, out var ab) ? ab : (Banka: "Amir Banka", Sehir: kart.Sehir);
                                ib.AkreditifNo = $"LC{yukleme:yy}{(kart.Ulke.Length > 2 ? kart.Ulke.Substring(0, 2).ToUpperInvariant() : "XX")}{100450 + (lc += rnd.Next(1, 4)):000000}"; ib.AmirBanka = b.Banka; ib.IhbarBankasi = IhbarBankalari[rnd.Next(IhbarBankalari.Length)];
                                ib.AkreditifVadesi = yukleme.AddDays(rnd.Next(28, 50));
                                if (g.Amac == "Akreditif İbrazı") ib.IbrazSonTarihi = yukleme.AddDays(21) < ib.AkreditifVadesi ? yukleme.AddDays(21) : ib.AkreditifVadesi;   // ibraz süresi yalnız bankaya giden set için izlenir; müşteriye giden kopya sette risk yok
                            }
                            // belge seti
                            int s = 0; void B(string tur, string no, int adet, string nitelik, string not = "") => ib.Belgeler.Add(new IbBelge { Sira = ++s, Tur = tur, No = no, Adet = adet, Nitelik = nitelik, Not = not });
                            string bl = $"MSCU{rnd.Next(1000000, 9999999)}";
                            if (g.Amac == "Akreditif İbrazı" || g.Amac == "Vesaik Tahsili")
                            {
                                B("Orijinal Konşimento (B/L)", bl, 3, "Orijinal", "3/3 orijinal + 3 kopya"); B("Ticari Fatura", d.FaturaNo, 3, "Orijinal", "İmzalı, kaşeli"); B("Çeki Listesi", "PL-" + d.FaturaNo.Substring(9), 3, "Orijinal");
                                B("Menşe Şahadetnamesi", $"TR-{yukleme:yy}-{rnd.Next(100000, 999999)}", 1, "Tasdikli Orijinal", "İTO onaylı"); if (odeme == "Akreditif (L/C)") { B("Sigorta Poliçesi", $"POL-{rnd.Next(1000000, 9999999)}", 2, "Orijinal", "Fatura değerinin %110'u"); B("Poliçe (Draft)", ib.AkreditifNo, 1, "Orijinal", "Amir banka üzerine"); }
                                if (rnd.Next(2) == 0) B("Analiz Sertifikası", $"COA-{rnd.Next(10000, 99999)}", 1, "Orijinal");
                            }
                            else if (g.Amac == "Müşteriye Orijinal Evrak" || g.Amac == "Forwarder'a Evrak")
                            {
                                B("Orijinal Konşimento (B/L)", bl, odeme == "Akreditif (L/C)" ? 0 : 3, odeme == "Akreditif (L/C)" ? "Kopya" : "Orijinal", odeme == "Akreditif (L/C)" ? "Orijinaller amir bankada" : "3/3 orijinal"); if (ib.Belgeler.Last().Adet == 0) ib.Belgeler.Last().Adet = 1;
                                B("Ticari Fatura", d.FaturaNo, 2, "Orijinal"); B("Çeki Listesi", "PL-" + d.FaturaNo.Substring(9), 2, "Orijinal");
                                if (AbUlkeleri.Contains(kart.Ulke)) B("A.TR Dolaşım Belgesi", $"ATR-{rnd.Next(100000, 999999)}", 1, "Tasdikli Orijinal", "Gümrük vizeli"); else B("Menşe Şahadetnamesi", $"TR-{yukleme:yy}-{rnd.Next(100000, 999999)}", 1, "Tasdikli Orijinal");
                                if (rnd.Next(3) == 0) B("Analiz Sertifikası", $"COA-{rnd.Next(10000, 99999)}", 1, "Orijinal"); if (rnd.Next(4) == 0) B("Navlun Faturası", $"NF-{rnd.Next(10000, 99999)}", 1, "Kopya");
                            }
                            else { B("Ticari Fatura", d.FaturaNo, 2, "Orijinal", g.Amac == "Konsolosluk Tasdiki" ? "Konsolosluk tasdiki için" : "Menşe onayı için"); B("Menşe Şahadetnamesi", $"TR-{yukleme:yy}-{rnd.Next(100000, 999999)}", 1, "Orijinal", "Onaya sunulan"); if (g.Amac == "Konsolosluk Tasdiki") B("Çeki Listesi", "PL-" + d.FaturaNo.Substring(9), 1, "Orijinal"); }
                            ib.Agirlik = Math.Round(0.25m + ib.BelgeAdedi * 0.02m + (decimal)rnd.NextDouble() * 0.2m, 2);
                            // kargo
                            var firmalar = KargoFirmalari.Where(x => x.Bolge == (yurtdisiAdres ? "Yurtdışı" : "Yurtiçi")).ToList();
                            var kf = yurtdisiAdres ? (rnd.Next(10) < 7 ? firmalar[0] : firmalar[rnd.Next(firmalar.Count)]) : (rnd.Next(2) == 0 ? firmalar.Last() : firmalar[rnd.Next(2)]);
                            ib.KargoFirmasi = kf.Ad; ib.GonderimTuru = kf.Tur; ib.GonderimNo = TakipNo(kf.Ad); ib.GonderimNoLink = string.IsNullOrEmpty(kf.Link) ? "" : kf.Link + ib.GonderimNo;
                            int teslimGun;
                            if (yurtdisiAdres) { teslimGun = rnd.Next(2, 6); ib.MasrafPB = pb; ib.MasrafDoviz = Math.Round(26m + ib.Agirlik * 18m + rnd.Next(0, 20), 2); ib.Masraf = Math.Round(ib.MasrafDoviz * kur, 2); }
                            else { teslimGun = kf.Ad == "Motorlu Kurye" ? 0 : 1; ib.Masraf = Math.Round(kf.Ad == "Motorlu Kurye" ? 250m + rnd.Next(0, 300) : 140m + rnd.Next(0, 160), 2); }
                            ib.TahminiTeslim = t.AddDays(Math.Max(1, teslimGun));
                            // hareketler ve durum
                            string cikis = sirket == "Avrupa Paper" ? "Çerkezköy" : sirket == "URS Makine" ? "Bursa" : "Gebze";
                            void H(DateTime z, string yer, string a, string tip) { if (z <= bugun.AddHours(23)) ib.Hareketler.Add(new NmHareket { Zaman = z, Yer = yer, Aciklama = a, Tip = tip }); }
                            H(t.AddDays(-1).AddHours(14), cikis, $"Belge seti hazırlandı ({ib.BelgeAdedi} belge, {ib.Belgeler.Count} tür) · {ib.Gonderen}", "sis");
                            if (odeme == "Akreditif (L/C)" && g.Amac == "Akreditif İbrazı") H(t.AddDays(-1).AddHours(16), cikis, $"Akreditif şartları kontrol edildi · {ib.AkreditifNo} · ibraz son tarihi {ib.IbrazSonTarihi:dd.MM.yyyy}", "sis");
                            int yas = (bugun - t).Days; double r = rnd.NextDouble();
                            if (r < 0.015) { ib.Durum = "İptal"; ib.Not = "Belge setinde eksik tespit edildi; gönderim iptal edilip yeniden hazırlandı."; H(t.AddHours(9), cikis, "Gönderim iptal edildi", "hata"); }
                            else if (yas == 0 && r < 0.5) { ib.Durum = "Hazırlanıyor"; }
                            else
                            {
                                H(t.AddHours(11), cikis, kf.Ad == "Motorlu Kurye" ? "Motorlu kuryeye teslim edildi · " + ib.GonderimNo : $"{kf.Ad} teslim aldı · takip no {ib.GonderimNo} · {ib.ZarfAdedi} doküman zarfı", "kargo");
                                var teslim = t.AddDays(teslimGun).AddHours(rnd.Next(9, 17));
                                if (yurtdisiAdres) { H(t.AddHours(20), "İstanbul Havalimanı", "Uçağa yüklendi (doküman gönderisi, gümrük beyanı gerekmez)", "kargo"); H(t.AddDays(Math.Max(1, teslimGun - 1)).AddHours(7), kf.Ad == "DHL Express" ? "Leipzig Hub" : kf.Ad == "UPS" ? "Köln Hub" : kf.Ad == "FedEx" ? "Paris CDG Hub" : "Liège Hub", "Transit merkezinden çıktı", "kargo"); H(teslim.AddHours(-3), ib.AliciUlke == "Türkiye" ? "İstanbul" : (AmirBankalar.TryGetValue(kart.Ulke, out var ab2) && ib.AliciTipi.Contains("Banka") ? ab2.Sehir : kart.Sehir), "Dağıtımda, kuryede", "kargo"); }
                                bool sorun = r > 0.97;
                                if (teslim > bugun) { ib.Durum = (bugun - t).Days == 0 ? "Kargoya Verildi" : "Yolda"; ib.SonHareket = ib.Hareketler.Last().Aciklama; if (ib.AkreditifNo != null && g.Amac == "Akreditif İbrazı") ib.BankaDurumu = "—"; }
                                else if (sorun) { ib.Durum = "Teslim Edilemedi"; H(teslim, kart.Sehir, "Alıcı adreste bulunamadı; gönderi kargo şubesinde bekliyor", "hata"); ib.SonHareket = ib.Hareketler.Last().Aciklama; ib.Not = "Teslim edilemedi; operasyon uzmanı alıcıyla iletişimde, yeniden dağıtım talep edildi."; }
                                else
                                {
                                    ib.Durum = "Teslim Edildi"; ib.TeslimTarihi = teslim; ib.TeslimAlan = ib.AliciKisi; H(teslim, ib.AliciUlke == "Türkiye" ? "İstanbul" : ib.AliciAdres.Split('·').Last().Trim(), $"Teslim edildi · alan: {ib.AliciKisi}", "teslim"); ib.SonHareket = ib.Hareketler.Last().Aciklama;
                                    if (g.Amac == "Akreditif İbrazı")
                                    {
                                        int inceleme = rnd.Next(3, 8); var karar = teslim.Date.AddDays(inceleme); double r2 = rnd.NextDouble();
                                        bool gec = teslim.Date > ib.IbrazSonTarihi;
                                        if (karar > bugun) ib.BankaDurumu = "İnceleniyor";
                                        else if (gec || r2 < 0.2) { ib.BankaDurumu = rnd.Next(3) == 0 ? "Rezerv Bildirildi" : "Rezerv Kaldırıldı"; ib.RezervNotu = gec ? "Geç ibraz (21 günlük süre aşıldı)" : RezervNotlari[rnd.Next(RezervNotlari.Length)]; if (ib.BankaDurumu == "Rezerv Kaldırıldı" && karar.AddDays(12) <= bugun) { ib.BankaDurumu = "Ödendi"; ib.OdemeTarihi = karar.AddDays(12); } }
                                        else { ib.BankaDurumu = "Uygun Bulundu"; if (karar.AddDays(rnd.Next(5, 15)) <= bugun) { ib.BankaDurumu = "Ödendi"; ib.OdemeTarihi = karar.AddDays(rnd.Next(5, 15)); } }
                                        H(karar <= bugun ? karar.AddHours(10) : bugun.AddHours(25), ib.AmirBanka, "Amir banka belge incelemesi: " + (ib.BankaDurumu == "İnceleniyor" ? "devam ediyor" : ib.RezervNotu != null ? "rezerv — " + ib.RezervNotu : "belgeler uygun bulundu"), ib.RezervNotu != null ? "hata" : "sis");
                                        if (ib.OdemeTarihi.HasValue) H(ib.OdemeTarihi.Value.AddHours(11), ib.IhbarBankasi, $"Akreditif bedeli tahsil edildi · {ib.FaturaTutari:N2} {pb}", "teslim");
                                    }
                                    else if (g.Amac == "Vesaik Tahsili") { var od = teslim.Date.AddDays(rnd.Next(7, 30)); if (od <= bugun) { ib.BankaDurumu = "Ödendi"; ib.OdemeTarihi = od; H(od.AddHours(11), ib.Alici, $"Vesaik bedeli ödendi, belgeler alıcıya serbest bırakıldı · {ib.FaturaTutari:N2} {pb}", "teslim"); } else ib.BankaDurumu = "İnceleniyor"; }
                                }
                            }
                            l.Add(ib);
                        }
                    }
                }
            }
            int seq = 0;
            foreach (var t in l.OrderBy(x => x.Tarih).ThenBy(x => x.Sirket)) t.No = $"İBG-{t.Tarih:yyyy}-{++seq:00000}";
            int id = 0;
            var sonuc = l.OrderByDescending(x => x.Tarih).ThenBy(x => x.Sirket).ThenBy(x => x.No).ToList();
            foreach (var t in sonuc) t.Id = ++id;
            return sonuc;
        }
    }
}
