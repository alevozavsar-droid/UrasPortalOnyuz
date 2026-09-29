// ============================================================
// NUMUNE GÖNDERİM TAKİP RAPORU — konsolide örnek veri (Satış ve Operasyon › Müşteri ve CRM Yönetimi › Numune Yönetimi; Yönetim › Özel Raporlar)
// Amaç: Grup şirketlerinin müşterilere gönderdiği TÜM numuneleri (yurtiçi + yurtdışı) tek listede göstermek — numunenin gerçek bedeli,
// gönderim masrafları (kargo + gümrük / ihracat beyanı + sigorta), kargo firması (DHL Express, UPS, FedEx, TNT, Yurtiçi / Aras / MNG / Sürat
// Kargo, Şirket Aracı), gönderim (takip) numarası, gönderen satış temsilcisi, alıcı bilgileri, kargo hareketleri, teslim ve sonuç
// (geri bildirim → teklif → sipariş) — ve "ay ay kaç numune gönderildi, bedeli / masrafı ne, ne kadarı siparişe döndü" özetini beslemek.
// Her numune bir CARİ_MASTER müşteri kartına bağlıdır (Data/CariKartOrnek.cs); gönderen şirket kartın faaliyet ana grubuna göre bulunur
// (TeklifOrnek.GrupSirketleri). Numune ürünleri kartın faaliyet satırındaki ürün hiyerarşisinden, bedel aralığı ana gruba göre gelir.
// Sonuç bağlantısı: numune tesliminden sonraki 45 gün içinde aynı şirketin aynı müşteriye aynı faaliyet grubunda verdiği ilk teklif (Data/TeklifOrnek.cs) "İlgili Teklif" olarak bağlanır;
// teklif satışa döndüyse numune sonucu "Siparişe Döndü", değilse "Teklif Verildi" olur.
// Durumlar: Hazırlanıyor | Kargoya Verildi | Yolda | Gümrükte | Teslim Edildi | Teslim Edilemedi | İptal ("İptal" toplama girmez).
// Kayıtlar bellek içindedir; SQL / ERP / kargo API bağlantısı yoktur. Ekran: Controllers/NumuneGonderimController.cs + Views/NumuneGonderim/Index.cshtml
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Kargoya Verildi", "Teslim Edildi", "Siparişe Döndü", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Numune kalemi (gönderilen ürün ve gerçek bedeli).</summary>
    public class NmKalem
    {
        public int Sira { get; set; }
        public string UrunSinifi { get; set; }
        public string UrunGrubu { get; set; }
        public string UrunSerisi { get; set; }
        public string Urun { get; set; }
        public string Parti { get; set; }           // üretim parti / lot no
        public decimal Miktar { get; set; }
        public string Birim { get; set; }
        public decimal BirimBedel { get; set; }     // TL, KDV hariç liste fiyatı
        public decimal Bedel { get; set; }          // TL
    }

    /// <summary>Kargo hareketi (takip satırı).</summary>
    public class NmHareket
    {
        public DateTime Zaman { get; set; }
        public string Yer { get; set; }
        public string Aciklama { get; set; }
        public string Tip { get; set; }             // sis | kargo | gumruk | teslim | hata
    }

    /// <summary>Tek bir numune gönderimi (şirketten bağımsız aynı yapı).</summary>
    public class NmNumune
    {
        public int Id { get; set; }
        public string No { get; set; }              // NMN-2026-00042
        public DateTime Tarih { get; set; }         // gönderim (kargoya veriliş) tarihi — aylık özet bu aya yazılır
        public string Sirket { get; set; }          // gönderen grup şirketi
        public string CariKodu { get; set; }        // CARİ_MASTER kodu
        public string Cari { get; set; }
        public string Bolge { get; set; }           // Yurtiçi | Yurtdışı (cari kartından)
        public string Ulke { get; set; }
        public string Sehir { get; set; }
        public string AliciKisi { get; set; }
        public string AliciAdres { get; set; }
        public string AliciTelefon { get; set; }
        public string Temsilci { get; set; }        // gönderen satış temsilcisi
        public string AnaGrup { get; set; }
        public string FaaliyetGrubu { get; set; }
        public string Amac { get; set; }
        public string Konu { get; set; }
        public List<NmKalem> Kalemler { get; set; } = new List<NmKalem>();
        public decimal NumuneBedeli { get; set; }   // kalemlerin gerçek bedeli (TL, KDV hariç)
        public string ParaBirimi { get; set; } = "TL";   // yurtdışında proforma para birimi
        public decimal Kur { get; set; } = 1;
        public decimal ProformaBedeli { get; set; } // yurtdışı: proforma faturadaki bedel (döviz)
        // ---- kargo
        public string KargoFirmasi { get; set; }
        public string GonderimTuru { get; set; }    // Express Kurye | Standart Kargo | Havayolu Kargo | Şirket Aracı (Elden Teslim)
        public string GonderimNo { get; set; }      // takip numarası
        public string GonderimNoLink { get; set; }  // kargo firması takip sayfası
        public decimal Agirlik { get; set; }        // kg
        public decimal Desi { get; set; }
        public decimal KargoMasrafi { get; set; }   // TL
        public string MasrafPB { get; set; } = "TL";
        public decimal MasrafDoviz { get; set; }    // kargo faturasının döviz tutarı (yurtdışı)
        public decimal GumrukMasrafi { get; set; }  // gümrük müşavirliği / ihracat beyannamesi (TL)
        public decimal SigortaMasrafi { get; set; } // TL
        public decimal ToplamMasraf => KargoMasrafi + GumrukMasrafi + SigortaMasrafi;
        public string MasrafKime { get; set; }      // Gönderen | Alıcı
        public DateTime TahminiTeslim { get; set; }
        public DateTime? TeslimTarihi { get; set; }
        public string TeslimAlan { get; set; }
        public string Durum { get; set; }
        public string SonHareket { get; set; }
        public List<NmHareket> Hareketler { get; set; } = new List<NmHareket>();
        // ---- belgeler
        public string IrsaliyeNo { get; set; }      // bedelsiz numune irsaliyesi
        public string ProformaNo { get; set; }      // yurtdışı proforma fatura
        public string BeyannameNo { get; set; }     // ihracat beyannamesi (yurtdışı, teslim edilmişse)
        // ---- sonuç
        public string Sonuc { get; set; }           // Geri Bildirim Bekleniyor | Değerlendiriliyor | Onaylandı | Reddedildi | Teklif Verildi | Siparişe Döndü | Sonuçsuz
        public DateTime? GeriBildirimTarihi { get; set; }
        public string GeriBildirim { get; set; }
        public string IlgiliTeklifNo { get; set; }
        public string IlgiliSiparisNo { get; set; }
        public decimal IlgiliSatisTl { get; set; }
        public string Hazirlayan { get; set; }      // numune sorumlusu (Ar-Ge / laboratuvar)
        public string Onaylayan { get; set; }
        public string Not { get; set; }
        public string Kaynak { get; set; } = "ERP Numune Modülü";
        public bool Gecerli => Durum != "İptal";
        public bool Teslim => Durum == "Teslim Edildi";
        public bool Yolda => Durum == "Hazırlanıyor" || Durum == "Kargoya Verildi" || Durum == "Yolda" || Durum == "Gümrükte";
        public int? TeslimSuresi => TeslimTarihi.HasValue ? (int?)(TeslimTarihi.Value - Tarih).Days : null;
    }

    public static class NumuneOrnek
    {
        public static readonly object Kilit = new object();
        public static string[] Sirketler => TeklifOrnek.Sirketler;

        // ---------- Seçim listeleri ----------
        public static readonly string[] Durumlar = { "Hazırlanıyor", "Kargoya Verildi", "Yolda", "Gümrükte", "Teslim Edildi", "Teslim Edilemedi", "İptal" };
        public static readonly string[] Sonuclar = { "Geri Bildirim Bekleniyor", "Değerlendiriliyor", "Onaylandı", "Reddedildi", "Teklif Verildi", "Siparişe Döndü", "Sonuçsuz" };
        public static readonly string[] Amaclar = { "Yeni Ürün Denemesi", "Teklif Öncesi Deneme", "Sipariş Öncesi Onay Numunesi", "Kalite Şikayeti Karşılığı", "Fuar / Tanıtım", "Reçete Geliştirme (Ar-Ge)", "Rakip Ürün Karşılaştırması" };
        public static readonly string[] GonderimTurleri = { "Express Kurye", "Standart Kargo", "Havayolu Kargo", "Şirket Aracı (Elden Teslim)" };
        public static readonly string[] MasrafKimeler = { "Gönderen", "Alıcı" };
        /// <summary>Kargo firmaları: ad, bölge (Yurtiçi / Yurtdışı / Her İkisi), takip no ön eki, takip sayfası.</summary>
        public static readonly (string Ad, string Bolge, string Tur, string Link)[] KargoFirmalari =
        {
            ("DHL Express", "Yurtdışı", "Express Kurye", "https://www.dhl.com/tr-tr/home/tracking.html?tracking-id="),
            ("UPS", "Yurtdışı", "Express Kurye", "https://www.ups.com/track?tracknum="),
            ("FedEx", "Yurtdışı", "Express Kurye", "https://www.fedex.com/fedextrack/?trknbr="),
            ("TNT", "Yurtdışı", "Express Kurye", "https://www.tnt.com/express/tr_tr/site/shipping-tools/tracking.html?searchType=con&cons="),
            ("Turkish Cargo", "Yurtdışı", "Havayolu Kargo", "https://www.turkishcargo.com/tr/takip?awb="),
            ("Yurtiçi Kargo", "Yurtiçi", "Standart Kargo", "https://www.yurticikargo.com/tr/online-servisler/gonderi-sorgula?code="),
            ("Aras Kargo", "Yurtiçi", "Standart Kargo", "https://www.araskargo.com.tr/tr/cargo-tracking?code="),
            ("MNG Kargo", "Yurtiçi", "Standart Kargo", "https://www.mngkargo.com.tr/gonderi-takip?code="),
            ("Sürat Kargo", "Yurtiçi", "Standart Kargo", "https://www.suratkargo.com.tr/KargoTakip/?kargotakipno="),
            ("DHL Express", "Yurtiçi", "Express Kurye", "https://www.dhl.com/tr-tr/home/tracking.html?tracking-id="),
            ("Şirket Aracı", "Yurtiçi", "Şirket Aracı (Elden Teslim)", "")
        };
        public static string[] KargoAdlari => KargoFirmalari.Select(k => k.Ad).Distinct().ToArray();
        private static readonly string[] Hazirlayanlar = { "Numune Sorumlusu · Derya Kılıç", "Ar-Ge Laboratuvarı · Serkan Ateş", "Ar-Ge Laboratuvarı · Melis Uçar", "Sevkiyat · Kadir Öz" };
        private static readonly string[] AliciUnvanlari = { "Satın Alma Müdürü", "Ar-Ge Sorumlusu", "Üretim Müdürü", "Baskı Şefi", "Laboratuvar Şefi", "Genel Müdür" };
        private static readonly string[] GeriBildirimler = { "Renk ve haslık değerleri beklentiyi karşıladı.", "Viskozite yüksek bulundu; reçete revizesi istendi.", "Deneme baskısı başarılı, seri sipariş planlanıyor.", "Kuruma süresi uzun; rakip ürünle karşılaştırılacak.", "Kalite onaylandı, fiyat teklifi bekleniyor.", "Numune müşteri laboratuvarında test aşamasında.", "Müşteri farklı gramaj talep etti; re-numune gönderilecek.", "Fiyat / performans dengesi uygun bulunmadı." };
        private static readonly (string Ana, decimal MiktarMin, decimal MiktarMax, string Birim)[] NumuneMiktarlari =
        {
            ("Boya Grubu", 0.5m, 5m, "kg"), ("Kağıt Grubu", 2m, 15m, "kg"), ("Binder Grubu", 1m, 12m, "kg"), ("Yardımcı Kimyasallar", 0.5m, 6m, "kg"), ("Baskı Makineleri", 1m, 4m, "Adet")
        };
        private static decimal KurBul(string pb) => TeklifOrnek.Kurlar.TryGetValue(pb ?? "TL", out var k) ? k : 1m;
        private static readonly HashSet<string> EurUlkeleri = new HashSet<string> { "Almanya", "İtalya", "İspanya", "Fransa", "Hollanda", "Belçika", "Polonya", "Romanya", "Bulgaristan", "Yunanistan" };

        // ---------- Konsolide liste ----------
        private static readonly Lazy<List<NmNumune>> _liste = new Lazy<List<NmNumune>>(Uret);
        public static List<NmNumune> Liste => _liste.Value;
        public static NmNumune Bul(string no) => Liste.FirstOrDefault(t => string.Equals(t.No, no, StringComparison.OrdinalIgnoreCase));
        /// <summary>Numune gönderilen müşteri kartları (kod sırasıyla).</summary>
        public static IEnumerable<CariKart> Musteriler() { var kodlar = new HashSet<string>(Liste.Select(t => t.CariKodu)); return CariKartOrnek.Kartlar.Where(k => kodlar.Contains(k.Kod)).OrderBy(k => k.Kod); }

        private static List<NmNumune> Uret()
        {
            var rnd = new Random(20260929);
            var bugun = DateTime.Today; var l = new List<NmNumune>();
            DateTime Gun(DateTime ay, int g) => new DateTime(ay.Year, ay.Month, Math.Min(g, DateTime.DaysInMonth(ay.Year, ay.Month)));
            var musteriler = CariKartOrnek.Kartlar.Where(k => k.KartTipi == "Müşteri" && k.Faaliyetler.Count > 0).ToList();
            var teklifler = TeklifOrnek.Liste;   // sonuç bağlantısı için
            var aylikCarpan = new[] { 0.8, 0.9, 1.1, 1.0, 1.1, 0.95, 0.8, 0.7, 1.15, 1.2, 1.0, 0.7 };
            int irsaliye = 26000, proforma = 410, beyanname = 26340000;
            string TakipNo(string firma)
            {
                switch (firma)
                {
                    case "DHL Express": return rnd.Next(10, 99).ToString() + rnd.Next(10000000, 99999999);
                    case "UPS": return "1Z" + new string(Enumerable.Range(0, 16).Select(_ => "0123456789ABCDEFGHJKLMNPRSTUVWXYZ"[rnd.Next(33)]).ToArray());
                    case "FedEx": return rnd.Next(100000, 999999).ToString() + rnd.Next(100000, 999999);
                    case "TNT": return "GD" + rnd.Next(100000000, 999999999);
                    case "Turkish Cargo": return "235-" + rnd.Next(10000000, 99999999);
                    case "Yurtiçi Kargo": return rnd.Next(100000, 999999).ToString() + rnd.Next(1000000, 9999999);
                    case "Aras Kargo": return rnd.Next(100000, 999999).ToString() + rnd.Next(100000, 999999);
                    case "MNG Kargo": return rnd.Next(10000, 99999).ToString() + rnd.Next(10000, 99999);
                    case "Sürat Kargo": return "SK" + rnd.Next(100000000, 999999999);
                    default: return "";
                }
            }

            for (int k = bugun.Month - 1; k >= 0; k--)
            {
                var ay = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(-k);
                foreach (var sirket in Sirketler)
                {
                    var adaylar = musteriler.Select(m => (Kart: m, Faaliyetler: m.Faaliyetler.Where(f => TeklifOrnek.GrupSirketleri.TryGetValue(f.AnaGrup, out var ss) && ss.Contains(sirket)).ToList())).Where(x => x.Faaliyetler.Count > 0).ToList();
                    if (adaylar.Count == 0) continue;
                    int taban = sirket == "URS Makine" ? 1 : sirket == "Uras Kimya" ? 7 : 4;
                    int n = Math.Max(1, (int)Math.Round((taban + rnd.Next(-1, 3)) * aylikCarpan[ay.Month - 1]));
                    for (int i = 0; i < n; i++)
                    {
                        // içinde bulunulan ayda gönderimlerin yarısı son 10 güne düşer (yolda / gümrükte örnekleri görünsün)
                        var t = k == 0 && rnd.Next(2) == 0 ? Gun(ay, rnd.Next(Math.Max(1, bugun.Day - 9), bugun.Day + 1)) : Gun(ay, rnd.Next(1, 29)); if (t > bugun) continue;
                        var (kart, faaliyetler) = adaylar[rnd.Next(adaylar.Count)];
                        var f = faaliyetler[rnd.Next(faaliyetler.Count)];
                        if (TeklifOrnek.GrupSirketleri[f.AnaGrup][0] != sirket && rnd.Next(3) > 0) continue;
                        bool yurtdisi = kart.Bolge == "Yurtdışı";
                        var nm = new NmNumune
                        {
                            Tarih = t, Sirket = sirket, CariKodu = kart.Kod, Cari = kart.KisaAd, Bolge = kart.Bolge, Ulke = kart.Ulke, Sehir = kart.Sehir,
                            AliciKisi = $"{kart.Yetkili} ({AliciUnvanlari[rnd.Next(AliciUnvanlari.Length)]})", AliciAdres = $"{kart.ResmiUnvan} · {(string.IsNullOrEmpty(kart.Sehir) ? kart.Ulke : kart.Sehir + " / " + kart.Ulke)}", AliciTelefon = kart.Telefon,
                            Temsilci = string.IsNullOrEmpty(f.SatisTemsilcisi) ? "Satış Ofisi" : f.SatisTemsilcisi, AnaGrup = f.AnaGrup, FaaliyetGrubu = f.Grup,
                            Amac = Amaclar[rnd.Next(Amaclar.Length)], Hazirlayan = Hazirlayanlar[rnd.Next(Hazirlayanlar.Length)], MasrafKime = yurtdisi && rnd.Next(4) == 0 ? "Alıcı" : "Gönderen"
                        };
                        // ---- kalemler ve gerçek bedel
                        var fy = TeklifOrnek.Fiyatlar[f.AnaGrup]; var mk = NumuneMiktarlari.First(x => x.Ana == f.AnaGrup);
                        var urunler = TeklifOrnek.Urunler(f.AnaGrup, f.Grup);
                        int kn = Math.Min(urunler.Count, rnd.Next(1, 4));
                        var secilen = urunler.OrderBy(_ => rnd.Next()).Take(kn).ToList(); int sira = 0;
                        foreach (var u in secilen)
                        {
                            decimal miktar = mk.Birim == "Adet" ? rnd.Next((int)mk.MiktarMin, (int)mk.MiktarMax + 1) : Math.Round(mk.MiktarMin + (decimal)rnd.NextDouble() * (mk.MiktarMax - mk.MiktarMin), 1);
                            decimal bf = f.AnaGrup == "Baskı Makineleri" ? rnd.Next(750, 2500) : rnd.Next(fy.FiyatMin, fy.FiyatMax + 1) + rnd.Next(0, 100) / 100m;
                            string urun = f.AnaGrup == "Baskı Makineleri" ? u.Ad + " Deneme Baskı Çıktısı" : u.Ad;
                            nm.Kalemler.Add(new NmKalem { Sira = ++sira, UrunSinifi = u.Sinif, UrunGrubu = u.Grup, UrunSerisi = u.Seri, Urun = urun, Parti = $"L{t:yyMM}{rnd.Next(100, 999)}", Miktar = miktar, Birim = mk.Birim, BirimBedel = bf, Bedel = Math.Round(miktar * bf, 2) });
                        }
                        nm.NumuneBedeli = nm.Kalemler.Sum(x => x.Bedel);
                        nm.Konu = $"{f.Grup} · {secilen[0].Ad}{(kn > 1 ? $" +{kn - 1} Kalem" : "")}";
                        nm.Agirlik = Math.Round(nm.Kalemler.Where(x => x.Birim == "kg").Sum(x => x.Miktar) * 1.15m + nm.Kalemler.Where(x => x.Birim != "kg").Sum(x => x.Miktar) * 0.4m + 0.3m, 2);
                        nm.Desi = Math.Round(Math.Max(nm.Agirlik, nm.Agirlik * (0.9m + (decimal)rnd.NextDouble() * 0.8m)), 1);
                        if (yurtdisi) { nm.ParaBirimi = EurUlkeleri.Contains(kart.Ulke) ? "EUR" : "USD"; nm.Kur = Math.Round(KurBul(nm.ParaBirimi) * (1 + rnd.Next(-4, 5) / 100m), 4); nm.ProformaBedeli = Math.Round(nm.NumuneBedeli / nm.Kur, 2); nm.ProformaNo = $"PRF-{t:yyyy}-{proforma++:0000}"; }
                        nm.IrsaliyeNo = $"NİR{t:yyyy}{irsaliye++:000000}";
                        // ---- kargo
                        var firmalar = KargoFirmalari.Where(x => x.Bolge == kart.Bolge).ToList();
                        var kf = yurtdisi ? (rnd.Next(10) < 6 ? firmalar[0] : firmalar[rnd.Next(firmalar.Count)]) : (nm.Agirlik > 30 && rnd.Next(2) == 0 ? firmalar.Last() : (rnd.Next(12) == 0 ? firmalar.First(x => x.Ad == "DHL Express") : firmalar[rnd.Next(4)]));
                        nm.KargoFirmasi = kf.Ad; nm.GonderimTuru = kf.Tur; nm.GonderimNo = TakipNo(kf.Ad); nm.GonderimNoLink = string.IsNullOrEmpty(kf.Link) ? "" : kf.Link + nm.GonderimNo;
                        if (kf.Ad == "Şirket Aracı") nm.GonderimNo = "34 URS " + rnd.Next(100, 999) + " · " + t.ToString("dd.MM") + " Sevk";
                        int teslimGun;
                        if (yurtdisi)
                        {
                            teslimGun = kf.Tur == "Havayolu Kargo" ? rnd.Next(4, 9) : rnd.Next(2, 7);
                            decimal doviz = Math.Round(32m + nm.Desi * (kf.Ad == "DHL Express" ? 6.5m : kf.Ad == "Turkish Cargo" ? 3.8m : 5.9m) + rnd.Next(0, 25), 2);   // express kurye tarifesi (desi başına)
                            nm.MasrafPB = nm.ParaBirimi; nm.MasrafDoviz = doviz; nm.KargoMasrafi = Math.Round(doviz * nm.Kur, 2);
                            nm.GumrukMasrafi = Math.Round(900m + rnd.Next(0, 3600) + rnd.Next(0, 100) / 100m, 2);   // gümrük müşavirliği + ihracat beyannamesi
                            nm.SigortaMasrafi = nm.NumuneBedeli > 15000 ? Math.Round(nm.NumuneBedeli * 0.004m, 2) : 0;
                        }
                        else
                        {
                            teslimGun = kf.Ad == "Şirket Aracı" ? (rnd.Next(4) == 0 ? 1 : 0) : kf.Tur == "Express Kurye" ? 1 : rnd.Next(1, 4);
                            nm.KargoMasrafi = kf.Ad == "Şirket Aracı" ? Math.Round(400m + rnd.Next(0, 2100) + rnd.Next(0, 100) / 100m, 2) : Math.Round(160m + nm.Desi * (kf.Tur == "Express Kurye" ? 42m : 18m) + rnd.Next(0, 120), 2);
                            nm.SigortaMasrafi = nm.NumuneBedeli > 25000 && rnd.Next(2) == 0 ? Math.Round(nm.NumuneBedeli * 0.003m, 2) : 0;
                        }
                        nm.TahminiTeslim = t.AddDays(Math.Max(1, teslimGun));
                        // ---- hareketler ve durum
                        string cikis = sirket == "Avrupa Paper" ? "Çerkezköy" : sirket == "URS Makine" ? "Bursa" : "Gebze";
                        void H(DateTime z, string yer, string a, string tip) { if (z <= bugun.AddHours(23)) nm.Hareketler.Add(new NmHareket { Zaman = z, Yer = yer, Aciklama = a, Tip = tip }); }
                        int yas = (bugun - t).Days; double r = rnd.NextDouble();
                        H(t.AddDays(-rnd.Next(1, 3)).AddHours(10), cikis, $"Numune talebi açıldı · {nm.Amac} · {nm.Temsilci}", "sis");
                        H(t.AddDays(-1).AddHours(15), cikis, $"Numune hazırlandı ve paketlendi ({nm.Agirlik} kg) · {nm.Hazirlayan}", "sis");
                        if (r < 0.02) { nm.Durum = "İptal"; nm.Not = "Müşteri talebi geri çekti; gönderim iptal edildi."; H(t.AddHours(9), cikis, "Gönderim iptal edildi", "hata"); nm.Sonuc = "Sonuçsuz"; }
                        else if (yas == 0 && r < 0.5) { nm.Durum = "Hazırlanıyor"; nm.Sonuc = "Geri Bildirim Bekleniyor"; }
                        else
                        {
                            H(t.AddHours(11), cikis, kf.Ad == "Şirket Aracı" ? "Şirket aracına yüklendi · " + nm.GonderimNo : $"{kf.Ad} teslim aldı · takip no {nm.GonderimNo}", "kargo");
                            var teslim = t.AddDays(teslimGun).AddHours(rnd.Next(9, 18));
                            if (yurtdisi)
                            {
                                H(t.AddDays(1).AddHours(2), "İstanbul Havalimanı", "İhracat gümrük işlemleri başladı · " + (nm.ProformaNo ?? ""), "gumruk");
                                H(t.AddDays(1).AddHours(14), "İstanbul Havalimanı", "Gümrükten çıktı, uçağa yüklendi", "gumruk");
                                H(t.AddDays(Math.Max(2, teslimGun - 1)).AddHours(8), kf.Ad == "DHL Express" ? "Leipzig Hub" : kf.Ad == "UPS" ? "Köln Hub" : kf.Ad == "FedEx" ? "Paris CDG Hub" : kart.Ulke, "Varış ülkesi gümrüğünde", "gumruk");
                                H(t.AddDays(Math.Max(2, teslimGun - 1)).AddHours(16), kart.Sehir ?? kart.Ulke, "Gümrükten çıktı, dağıtıma verildi", "kargo");
                            }
                            else if (kf.Ad != "Şirket Aracı") { H(t.AddDays(1).AddHours(3), "Transfer Merkezi", "Transfer merkezine ulaştı", "kargo"); H(teslim.AddHours(-4), kart.Sehir, "Dağıtım şubesinde, kuryede", "kargo"); }
                            bool sorun = r > 0.96;
                            if (teslim > bugun)
                            {
                                var son = nm.Hareketler.Last(); nm.SonHareket = son.Aciklama;
                                nm.Durum = son.Tip == "gumruk" ? "Gümrükte" : (bugun - t).Days == 0 ? "Kargoya Verildi" : "Yolda";
                                nm.Sonuc = "Geri Bildirim Bekleniyor";
                            }
                            else if (sorun)
                            {
                                nm.Durum = "Teslim Edilemedi"; H(teslim, kart.Sehir ?? kart.Ulke, yurtdisi ? "Alıcı gümrük vergisini ödemedi; gönderi iade sürecinde" : "Adres bulunamadı / alıcı yok; gönderi şubede bekliyor", "hata"); nm.SonHareket = nm.Hareketler.Last().Aciklama; nm.Sonuc = "Sonuçsuz"; nm.Not = "Gönderi teslim edilemedi; satış temsilcisi alıcıyla iletişimde.";
                            }
                            else
                            {
                                nm.Durum = "Teslim Edildi"; nm.TeslimTarihi = teslim; nm.TeslimAlan = kart.Yetkili;
                                H(teslim, kart.Sehir ?? kart.Ulke, $"Teslim edildi · alan: {kart.Yetkili}", "teslim"); nm.SonHareket = nm.Hareketler.Last().Aciklama;
                                if (yurtdisi) nm.BeyannameNo = $"26341300EX{beyanname++:000000}";
                                // sonuç: numune tesliminden sonraki 45 gün içinde aynı şirketin aynı müşteriye aynı faaliyet grubunda verdiği ilk teklif
                                var tk = teklifler.Where(x => x.CariKodu == kart.Kod && x.Sirket == sirket && x.FaaliyetGrubu == f.Grup && x.Tarih >= teslim.Date && x.Tarih <= teslim.Date.AddDays(45) && x.Durum != "İptal").OrderBy(x => x.Tarih).FirstOrDefault();
                                int gb = rnd.Next(3, 25); var gbt = teslim.Date.AddDays(gb);
                                if (tk != null)
                                {
                                    nm.IlgiliTeklifNo = tk.No; nm.GeriBildirimTarihi = tk.Tarih.AddDays(-rnd.Next(1, 4)) < teslim.Date ? teslim.Date.AddDays(1) : tk.Tarih.AddDays(-rnd.Next(1, 4));
                                    if (tk.Durum == "Satışa Döndü") { nm.Sonuc = "Siparişe Döndü"; nm.IlgiliSiparisNo = tk.SiparisNo; nm.IlgiliSatisTl = tk.SatisTutariTl; nm.GeriBildirim = "Numune onaylandı; teklif verildi ve siparişe döndü."; }
                                    else { nm.Sonuc = "Teklif Verildi"; nm.GeriBildirim = "Numune onaylandı; fiyat teklifi verildi (" + tk.Durum + ")."; }
                                }
                                else if (gbt > bugun) nm.Sonuc = (bugun - teslim.Date).Days > 7 ? "Değerlendiriliyor" : "Geri Bildirim Bekleniyor";
                                else
                                {
                                    double r2 = rnd.NextDouble(); nm.GeriBildirimTarihi = gbt; nm.GeriBildirim = GeriBildirimler[rnd.Next(GeriBildirimler.Length)];
                                    nm.Sonuc = r2 < 0.35 ? "Onaylandı" : r2 < 0.6 ? "Reddedildi" : r2 < 0.8 ? "Değerlendiriliyor" : "Sonuçsuz";
                                }
                            }
                        }
                        nm.Onaylayan = nm.NumuneBedeli >= 25_000m || yurtdisi ? "Satış Müdürü" : nm.Temsilci;
                        l.Add(nm);
                    }
                }
            }
            int seq = 0;
            foreach (var t in l.OrderBy(x => x.Tarih).ThenBy(x => x.Sirket)) t.No = $"NMN-{t.Tarih:yyyy}-{++seq:00000}";
            int id = 0;
            var sonuc = l.OrderByDescending(x => x.Tarih).ThenBy(x => x.Sirket).ThenBy(x => x.No).ToList();
            foreach (var t in sonuc) t.Id = ++id;
            return sonuc;
        }
    }
}
