// ============================================================
// SİGORTA VE KASKO POLİÇE TAKİBİ — tüm grup şirketlerinin poliçeleri (Yönetim › Şirket Dökümanları › Sigorta; İdari İşler › Araç ve Seyahat kısayolu; /Sigorta)
// Poliçe türleri: Kasko, Trafik, İş Yeri Paket / Yangın, DASK, Nakliyat Abonman, İşveren Mali Sorumluluk, Ürün Sorumluluk, Makine Kırılması, Ticari Alacak, Çevre Sorumluluk,
//   Grup Sağlık, Ferdi Kaza. Araç poliçeleri plaka ile Data/AracBakimOrnek.cs araçlarına bağlıdır.
// Her poliçe: sigortacı, acente, sigortalı şirket, konu (araç / tesis / kapsam), teminatlar, başlangıç-bitiş, prim, TAKSİT PLANI (1–12 taksit: vade, tutar, ödendi / bekliyor / gecikti,
//   ödeme tarihi, yöntem, dekont), hasar kayıtları, dosyalar (poliçe PDF, zeyilname, dekontlar), günlük. Durum: Aktif | Yenileme Yaklaşıyor (≤ 45 gün) | Süresi Doldu | İptal | Yenilendi.
// Ekran: Controllers/SigortaController.cs + Views/Sigorta/Index.cshtml + wwwroot/js/sigorta.js — 12 aylık taksit takvimi (poliçe × ay), poliçe listesi, araç poliçeleri, yenileme takvimi.
// Yazım kuralı: liste değerleri ve etiketler Title Case ("Yenileme Yaklaşıyor", "Kredi Kartı", "Tümü").
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class SgTaksit { public int No { get; set; } public DateTime Vade { get; set; } public decimal Tutar { get; set; } public DateTime? OdemeTarihi { get; set; } public string Yontem { get; set; } = ""; public string Dekont { get; set; } = ""; public string Durum => OdemeTarihi.HasValue ? "Ödendi" : Vade.Date < DateTime.Today ? "Gecikti" : Vade.Date <= DateTime.Today.AddDays(7) ? "Bu Hafta" : "Bekliyor"; }
    public class SgHasar { public int No { get; set; } public DateTime Tarih { get; set; } public string DosyaNo { get; set; } public string Aciklama { get; set; } public decimal Tutar { get; set; } public string Durum { get; set; } = "İnceleniyor"; }
    public class SgDosya { public int No { get; set; } public string Tur { get; set; } public string Dosya { get; set; } public string Tip { get; set; } public long Boyut { get; set; } public byte[] Veri { get; set; } public DateTime Tarih { get; set; } public string Kullanici { get; set; } }
    public class SgHareket { public DateTime Zaman { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }
    public class SgPolice
    {
        public int Id { get; set; } public string No { get; set; } public string Tur { get; set; } public string Sirket { get; set; } public string Sigortaci { get; set; } public string Acente { get; set; } = ""; public string Plaka { get; set; } = ""; public string Konu { get; set; } = ""; public string Adres { get; set; } = "";
        public DateTime Baslangic { get; set; } public DateTime Bitis { get; set; } public decimal Prim { get; set; } public string ParaBirimi { get; set; } = "TL"; public decimal SigortaBedeli { get; set; } public List<string> Teminatlar { get; set; } = new List<string>(); public string Muafiyet { get; set; } = ""; public int HasarsizlikKademesi { get; set; }
        public int TaksitSayisi { get; set; } = 1; public string OdemeYontemi { get; set; } = "Kredi Kartı"; public List<SgTaksit> Taksitler { get; set; } = new List<SgTaksit>(); public List<SgHasar> Hasarlar { get; set; } = new List<SgHasar>();
        public string Durum { get; set; } = "Aktif"; public string Sorumlu { get; set; } = ""; public string Not { get; set; } = ""; public int? YenilenenId { get; set; } public int? OncekiId { get; set; }
        public List<SgDosya> Dosyalar { get; set; } = new List<SgDosya>(); public List<SgHareket> Hareketler { get; set; } = new List<SgHareket>();
        public int KalanGun => (Bitis.Date - DateTime.Today).Days;
        public string HesapDurum => Durum == "İptal" || Durum == "Yenilendi" ? Durum : KalanGun < 0 ? "Süresi Doldu" : KalanGun <= 45 ? "Yenileme Yaklaşıyor" : "Aktif";
        public decimal Odenen => Taksitler.Where(t => t.OdemeTarihi.HasValue).Sum(t => t.Tutar); public decimal Kalan => Prim - Odenen; public int GecikenTaksit => Taksitler.Count(t => t.Durum == "Gecikti");
        public bool AracPolicesi => Tur == "Kasko" || Tur == "Trafik";
    }
    public class SgPoliceIstek { public int? Id { get; set; } public string No { get; set; } public string Tur { get; set; } public string Sirket { get; set; } public string Sigortaci { get; set; } public string Acente { get; set; } public string Plaka { get; set; } public string Konu { get; set; } public string Adres { get; set; } public string Baslangic { get; set; } public string Bitis { get; set; } public decimal Prim { get; set; } public decimal? SigortaBedeli { get; set; } public string Teminatlar { get; set; } public string Muafiyet { get; set; } public int? HasarsizlikKademesi { get; set; } public int TaksitSayisi { get; set; } = 1; public string OdemeYontemi { get; set; } public string IlkTaksitVadesi { get; set; } public string Sorumlu { get; set; } public string Not { get; set; } }
    public class SgTaksitIstek { public int Id { get; set; } public int No { get; set; } public string OdemeTarihi { get; set; } public string Yontem { get; set; } public string Dekont { get; set; } public bool GeriAl { get; set; } }
    public class SgHasarIstek { public int Id { get; set; } public string Tarih { get; set; } public string DosyaNo { get; set; } public string Aciklama { get; set; } public decimal Tutar { get; set; } public string Durum { get; set; } }
    public class SgYenileIstek { public int Id { get; set; } public string No { get; set; } public decimal Prim { get; set; } public int TaksitSayisi { get; set; } = 12; public string Sigortaci { get; set; } public string Baslangic { get; set; } public string OdemeYontemi { get; set; } }

    public static class SigortaOrnek
    {
        public static readonly object Kilit = new object();
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        public const int YenilemeGun = 45;
        public static readonly string[] Turler = { "Kasko", "Trafik", "İş Yeri Paket / Yangın", "DASK", "Nakliyat Abonman", "İşveren Mali Sorumluluk", "Ürün Sorumluluk", "Makine Kırılması", "Ticari Alacak", "Çevre Sorumluluk", "Grup Sağlık", "Ferdi Kaza" };
        public static readonly string[] Sigortacilar = { "Allianz", "Anadolu Sigorta", "Axa", "Mapfre", "Türkiye Sigorta", "HDI", "Sompo", "Türk Eximbank", "Doğa Sigorta" };
        public static readonly string[] Acenteler = { "Yıldız Sigorta Acentesi", "Trakya Sigorta Aracılık", "Marmara Broker", "Doğrudan (Banka)" };
        public static readonly string[] OdemeYontemleri = { "Kredi Kartı", "Havale / EFT", "Otomatik Ödeme Talimatı", "Çek" };
        public static readonly string[] Durumlar = { "Aktif", "Yenileme Yaklaşıyor", "Süresi Doldu", "Yenilendi", "İptal" };
        public static readonly string[] HasarDurumlari = { "İnceleniyor", "Ekspertiz", "Onaylandı", "Ödendi", "Reddedildi" };
        public static readonly string[] Sorumlular = { "Finans · Kerem Aksoy", "İdari İşler · Serkan Genç", "Filo · Hakan Yıldız" };
        public static readonly string[] Sirketler = { "Uras Baskı Teknolojileri", "Uras Kimya", "Alv Kimya", "Selvi Kimya", "Uras Holding", "Avrupa Paper", "Alv Filo", "URS Makine", "DRN" };

        private static readonly List<SgPolice> _policeler = new List<SgPolice>();
        private static int _sonId, _sonDosya; private static bool _tohumlandi;

        public static List<SgTaksit> TaksitPlani(decimal prim, int adet, DateTime ilkVade)
        {
            adet = Math.Max(1, Math.Min(12, adet)); var l = new List<SgTaksit>(); decimal taban = Math.Round(prim / adet, 2); decimal toplam = 0;
            for (int i = 1; i <= adet; i++) { decimal t = i == adet ? Math.Round(prim - toplam, 2) : taban; toplam += t; l.Add(new SgTaksit { No = i, Vade = ilkVade.AddMonths(i - 1), Tutar = t }); }
            return l;
        }

        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                var rnd = new Random(20261101); var bugun = DateTime.Today;
                SgPolice P(string tur, string sirket, string konu, decimal prim, int taksit, int basGunOnce, string plaka = "", decimal bedel = 0, string adres = "", string[] teminat = null, string muafiyet = "", int kademe = 0)
                {
                    var bas = bugun.AddDays(-basGunOnce); var p = new SgPolice { Id = ++_sonId, No = $"{(tur == "Kasko" ? "KSK" : tur == "Trafik" ? "TRF" : "POL")}-{bas.Year}-{rnd.Next(100000, 999999)}", Tur = tur, Sirket = sirket, Sigortaci = Sigortacilar[rnd.Next(7)], Acente = Acenteler[rnd.Next(3)], Plaka = plaka, Konu = konu, Adres = adres, Baslangic = bas, Bitis = bas.AddYears(1).AddDays(-1), Prim = prim, SigortaBedeli = bedel, Teminatlar = (teminat ?? new string[0]).ToList(), Muafiyet = muafiyet, HasarsizlikKademesi = kademe, TaksitSayisi = taksit, OdemeYontemi = taksit > 1 ? "Kredi Kartı" : rnd.NextDouble() < 0.5 ? "Havale / EFT" : "Otomatik Ödeme Talimatı", Sorumlu = plaka != "" ? Sorumlular[2] : Sorumlular[0] };
                    p.Taksitler = TaksitPlani(prim, taksit, bas.AddDays(5));
                    foreach (var t in p.Taksitler) if (t.Vade < bugun.AddDays(-3) && !(p.Id % 7 == 0 && t.Vade > bugun.AddDays(-40))) { t.OdemeTarihi = t.Vade.AddDays(rnd.Next(0, 3)); t.Yontem = p.OdemeYontemi; t.Dekont = $"DK{t.OdemeTarihi:yyMMdd}{rnd.Next(100, 999)}"; }   // her 7. poliçede son taksit gecikmiş
                    p.Hareketler.Add(new SgHareket { Zaman = bas.AddHours(10), Kullanici = p.Sorumlu, Islem = "Poliçe Kaydı", Aciklama = $"{p.Sigortaci} · {p.Acente} · prim {prim.ToString("N2", Tr)} ₺ · {taksit} taksit · {p.OdemeYontemi}" });
                    p.Dosyalar.Add(new SgDosya { No = ++_sonDosya, Tur = "Poliçe", Dosya = $"{p.No}-police.pdf", Tip = "application/pdf", Boyut = rnd.Next(120, 600) * 1024L, Tarih = bas.AddHours(10), Kullanici = p.Sorumlu });
                    foreach (var t in p.Taksitler.Where(t => t.OdemeTarihi.HasValue).Take(2)) p.Dosyalar.Add(new SgDosya { No = ++_sonDosya, Tur = "Dekont", Dosya = $"{t.Dekont}.pdf", Tip = "application/pdf", Boyut = rnd.Next(40, 120) * 1024L, Tarih = t.OdemeTarihi.Value.AddHours(12), Kullanici = p.Sorumlu });
                    if (plaka != "" && tur == "Kasko" && rnd.NextDouble() < 0.3) { var h = new SgHasar { No = 1, Tarih = bas.AddDays(rnd.Next(30, 200)), DosyaNo = $"HSR-{bas.Year}-{rnd.Next(10000, 99999)}", Aciklama = "Park halinde çarpma; tampon ve far", Tutar = rnd.Next(12000, 40000), Durum = "Ödendi" }; if (h.Tarih <= bugun) p.Hasarlar.Add(h); }
                    _policeler.Add(p); return p;
                }
                // araç poliçeleri: her araç kasko (12 taksit) + trafik (peşin ya da 3 taksit); başlangıçlar yıl içine dağılmış, birkaçı 45 gün içinde yenilenecek, biri süresi dolmuş
                int k = 0;
                foreach (var a in AracBakimOrnek.Araclar)
                {
                    int basGun = new[] { 320, 40, 200, 150, 330, 90, 10, 260, 345, 120, 60, 300, 180, 370 }[k % 14]; k++;
                    decimal bedel = a.Tip == "Tır" ? 3_200_000 : a.Tip == "Binek" && a.Yil >= 2022 ? 1_450_000 : a.Tip == "Binek" ? 900_000 : 1_100_000; decimal kaskoPrim = Math.Round(bedel * 0.028m, 2);
                    P("Kasko", a.Sirket, $"{a.Marka} {a.Model} · {a.Yil} · şasi {a.Sasi}", kaskoPrim, 12, basGun, a.Plaka, bedel, "", new[] { "Çarpma / Çarpışma", "Hırsızlık", "Yangın", "Doğal Afet (Sel, Dolu, Deprem)", "Cam Kırılması (muafiyetsiz)", "İkame Araç (30 gün)", "Ferdi Kaza (Sürücü / Yolcu)", "Hukuksal Koruma", "Mini Onarım", "Anahtar Kaybı" }, "Hasar başına 2.500 ₺ (cam hariç)", 1 + rnd.Next(0, 5));
                    P("Trafik", a.Sirket, $"{a.Marka} {a.Model} · {a.Yil} · zorunlu trafik", a.Tip == "Tır" ? 18_500 : a.Tip == "Binek" ? 6_800 + rnd.Next(0, 2500) : 9_900, rnd.NextDouble() < 0.5 ? 1 : 3, basGun, a.Plaka, 0, "", new[] { "Maddi Hasar (araç başına 300.000 ₺)", "Sağlık Giderleri", "Sakatlanma ve Ölüm", "İMM (İhtiyari Mali Mesuliyet 5.000.000 ₺)" }, "", 4);
                }
                // kurumsal poliçeler
                P("İş Yeri Paket / Yangın", "Uras Kimya", "Velimeşe fabrika · bina, makine, emtia", 486_000, 12, 95, "", 185_000_000, "Velimeşe OSB, Ergene / Tekirdağ", new[] { "Yangın, Yıldırım, İnfilak", "Deprem", "Sel / Su Baskını", "Hırsızlık", "Makine Kırılması (ek)", "Kâr Kaybı (6 ay)", "Cam Kırılması", "Kiracı Sorumluluk" }, "Deprem %2", 0);
                P("İş Yeri Paket / Yangın", "Alv Kimya", "Çerkezköy tesis · bina, makine, emtia", 312_000, 12, 210, "", 120_000_000, "Çerkezköy OSB, Kapaklı / Tekirdağ", new[] { "Yangın, Yıldırım, İnfilak", "Deprem", "Sel / Su Baskını", "Hırsızlık", "Kâr Kaybı (6 ay)" }, "Deprem %2", 0);
                P("İş Yeri Paket / Yangın", "Avrupa Paper", "Çorlu fabrika · bina, makine, kağıt stoku", 228_000, 12, 30, "", 85_000_000, "Çorlu / Tekirdağ", new[] { "Yangın, Yıldırım, İnfilak", "Deprem", "Hırsızlık", "Kâr Kaybı (3 ay)" }, "Deprem %2", 0);
                P("İş Yeri Paket / Yangın", "URS Makine", "Çorlu atölye · makine ve emtia", 96_000, 6, 150, "", 32_000_000, "Çorlu / Tekirdağ", new[] { "Yangın", "Deprem", "Hırsızlık" }, "", 0);
                P("İş Yeri Paket / Yangın", "Uras Baskı Teknolojileri", "Çorlu showroom ve depo", 54_000, 6, 280, "", 18_000_000, "Çorlu / Tekirdağ", new[] { "Yangın", "Hırsızlık", "Cam" }, "", 0);
                P("DASK", "Uras Kimya", "Velimeşe idari bina (zorunlu deprem)", 9_800, 1, 95, "", 1_272_000, "Velimeşe OSB", new[] { "Deprem (DASK azami teminat)" }, "", 0);
                P("DASK", "Uras Holding", "Kadıköy genel müdürlük ofisi", 4_200, 1, 25, "", 1_272_000, "Kadıköy / İstanbul", new[] { "Deprem (DASK azami teminat)" }, "", 0);
                P("Nakliyat Abonman", "Uras Kimya", "Yurtiçi / yurtdışı emtia nakliyatı (abonman)", 142_000, 12, 160, "", 60_000_000, "", new[] { "Geniş Teminat (All Risks)", "Harp ve Grev (ek)", "Konteyner / Tır / Hava" }, "Sevkiyat başına 5.000 ₺", 0);
                P("Nakliyat Abonman", "Selvi Kimya", "İhracat sevkiyatları (abonman)", 88_000, 12, 70, "", 30_000_000, "", new[] { "Geniş Teminat (All Risks)", "Harp ve Grev (ek)" }, "", 0);
                P("İşveren Mali Sorumluluk", "Uras Kimya", "İş kazası sorumluluğu · 210 çalışan", 118_000, 12, 95, "", 10_000_000, "", new[] { "Kişi başı 2.000.000 ₺", "Kaza başı 10.000.000 ₺", "Manevi Tazminat" }, "", 0);
                P("İşveren Mali Sorumluluk", "Alv Kimya", "İş kazası sorumluluğu · 120 çalışan", 72_000, 12, 210, "", 6_000_000, "", new[] { "Kişi başı 1.500.000 ₺", "Kaza başı 6.000.000 ₺" }, "", 0);
                P("Ürün Sorumluluk", "Uras Kimya", "Tekstil baskı kimyasalları · yurtiçi + AB", 164_000, 12, 95, "", 25_000_000, "", new[] { "Üçüncü şahıs bedeni / maddi zarar", "Geri çağırma (ek)", "AB coğrafi kapsam" }, "Olay başı 25.000 ₺", 0);
                P("Makine Kırılması", "Uras Kimya", "Reaktör, dolum ve paketleme hatları", 96_000, 12, 95, "", 48_000_000, "Velimeşe OSB", new[] { "Elektriksel / mekanik arıza", "Operatör hatası", "Ek masraflar" }, "Hasar başı 15.000 ₺", 0);
                P("Makine Kırılması", "Avrupa Paper", "Kağıt kaplama hattı", 58_000, 12, 30, "", 26_000_000, "Çorlu", new[] { "Elektriksel / mekanik arıza" }, "Hasar başı 10.000 ₺", 0);
                P("Ticari Alacak", "Uras Kimya", "İhracat alacakları · Türk Eximbank", 210_000, 4, 240, "", 40_000_000, "", new[] { "Ticari risk %90", "Politik risk %90", "Alıcı limitleri" }, "", 0);
                P("Çevre Kirliliği Sorumluluk", "Alv Kimya", "Ani ve kazaen kirlilik", 44_000, 6, 210, "", 5_000_000, "Çerkezköy OSB", new[] { "Temizleme masrafları", "Üçüncü şahıs zararları" }, "", 0);
                P("Grup Sağlık", "Uras Holding", "Grup tamamlayıcı sağlık · 180 çalışan", 2_160_000, 12, 50, "", 0, "", new[] { "Yatarak tedavi (limitsiz)", "Ayakta tedavi (yıllık 20 vaka)", "Doğum", "Diş (ek)" }, "", 0);
                P("Ferdi Kaza", "Selvi Kimya", "Saha ve sevkiyat personeli · 24 kişi", 18_600, 1, 300, "", 500_000, "", new[] { "Ölüm 500.000 ₺", "Sürekli sakatlık 500.000 ₺", "Tedavi 50.000 ₺" }, "", 0);
                // süresi dolmuş bir poliçe (yenilendi) ve onun yenisi
                var eski = _policeler.First(p => p.Plaka == "34 URS 700" && p.Tur == "Kasko"); eski.Durum = "Yenilendi"; eski.Hareketler.Add(new SgHareket { Zaman = eski.Bitis.AddDays(-5).AddHours(9), Kullanici = eski.Sorumlu, Islem = "Yenilendi", Aciklama = "Yeni dönem poliçesi açıldı" });
                var yeniP = P("Kasko", eski.Sirket, eski.Konu, Math.Round(eski.Prim * 1.35m, 2), 12, 4, eski.Plaka, eski.SigortaBedeli * 1.2m, "", eski.Teminatlar.ToArray(), eski.Muafiyet, eski.HasarsizlikKademesi + 1); yeniP.OncekiId = eski.Id; eski.YenilenenId = yeniP.Id;
            }
        }

        // ---------- erişim ----------
        public static List<SgPolice> Policeler { get { Tohumla(); lock (Kilit) return _policeler.OrderBy(p => p.HesapDurum == "Süresi Doldu" ? 0 : p.HesapDurum == "Yenileme Yaklaşıyor" ? 1 : p.HesapDurum == "Aktif" ? 2 : 3).ThenBy(p => p.Bitis).ToList(); } }
        public static SgPolice Bul(int id) { Tohumla(); lock (Kilit) return _policeler.FirstOrDefault(p => p.Id == id); }
        /// <summary>Aracın geçerli kasko / trafik poliçeleri (Araç Bakım ekranı için).</summary>
        public static object AracPoliceleri(string plaka)
        {
            var l = Policeler.Where(p => p.Plaka == plaka && p.Durum != "Yenilendi" && p.Durum != "İptal").ToList();
            object K(string tur) { var p = l.FirstOrDefault(x => x.Tur == tur); return p == null ? null : new { p.Id, no = p.No, p.Sigortaci, bitis = p.Bitis.ToString("yyyy-MM-dd"), kalan = p.KalanGun, durum = p.HesapDurum, prim = p.Prim, geciken = p.GecikenTaksit }; }
            return new { kasko = K("Kasko"), trafik = K("Trafik") };
        }
        static void Log(SgPolice p, string islem, string aciklama, string kullanici) => p.Hareketler.Add(new SgHareket { Zaman = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });

        public static (bool ok, string mesaj, SgPolice p) Kaydet(SgPoliceIstek i, string dosyaAdi, string icerikTipi, byte[] veri, string kullanici)
        {
            Tohumla(); if (i == null) return (false, "Geçersiz istek.", null);
            if (!Turler.Contains(i.Tur ?? "")) return (false, "Poliçe türü seçin.", null); if (!Sirketler.Contains(i.Sirket ?? "")) return (false, "Sigortalı şirket seçin.", null); if (string.IsNullOrWhiteSpace(i.Sigortaci)) return (false, "Sigorta şirketi girin.", null);
            if (!DateTime.TryParse(i.Baslangic, out var bas)) return (false, "Başlangıç tarihi girin.", null); DateTime bit = DateTime.TryParse(i.Bitis, out var b0) ? b0.Date : bas.AddYears(1).AddDays(-1); if (bit <= bas) return (false, "Bitiş başlangıçtan sonra olmalıdır.", null);
            if (i.Prim <= 0) return (false, "Prim sıfırdan büyük olmalıdır.", null); int taksit = Math.Max(1, Math.Min(12, i.TaksitSayisi));
            if ((i.Tur == "Kasko" || i.Tur == "Trafik") && AracBakimOrnek.AracBul(i.Plaka ?? "") == null) return (false, "Kasko / trafik poliçesi için araç (plaka) seçin.", null);
            if (veri != null && veri.LongLength > 12_000_000) return (false, "Dosya 12 MB'ı aşamaz.", null);
            lock (Kilit)
            {
                var p = i.Id.HasValue ? _policeler.FirstOrDefault(x => x.Id == i.Id.Value) : null; bool yeni = p == null;
                if (yeni) { p = new SgPolice { Id = ++_sonId }; _policeler.Add(p); }
                p.No = string.IsNullOrWhiteSpace(i.No) ? (p.No ?? $"POL-{bas.Year}-{_sonId:000000}") : i.No.Trim(); p.Tur = i.Tur; p.Sirket = i.Sirket; p.Sigortaci = i.Sigortaci.Trim(); p.Acente = (i.Acente ?? "").Trim(); p.Plaka = (i.Plaka ?? "").Trim(); p.Konu = (i.Konu ?? "").Trim(); p.Adres = (i.Adres ?? "").Trim(); p.Baslangic = bas.Date; p.Bitis = bit; p.Prim = Math.Round(i.Prim, 2); p.SigortaBedeli = i.SigortaBedeli ?? p.SigortaBedeli;
                p.Teminatlar = (i.Teminatlar ?? "").Split('\n', ',', ';').Select(x => x.Trim()).Where(x => x != "").ToList(); p.Muafiyet = (i.Muafiyet ?? "").Trim(); p.HasarsizlikKademesi = i.HasarsizlikKademesi ?? p.HasarsizlikKademesi; p.OdemeYontemi = OdemeYontemleri.Contains(i.OdemeYontemi ?? "") ? i.OdemeYontemi : p.OdemeYontemi; p.Sorumlu = string.IsNullOrWhiteSpace(i.Sorumlu) ? (p.Sorumlu == "" ? kullanici : p.Sorumlu) : i.Sorumlu.Trim(); p.Not = (i.Not ?? "").Trim();
                if (yeni || p.TaksitSayisi != taksit || p.Taksitler.Count == 0 || Math.Abs(p.Taksitler.Sum(t => t.Tutar) - p.Prim) > 0.01m)
                {
                    var odenmis = p.Taksitler.Where(t => t.OdemeTarihi.HasValue).ToList(); if (odenmis.Count > 0 && !yeni && p.TaksitSayisi != taksit) return (false, "Ödenmiş taksiti olan poliçede taksit sayısı değiştirilemez.", p);
                    p.TaksitSayisi = taksit; p.Taksitler = TaksitPlani(p.Prim, taksit, DateTime.TryParse(i.IlkTaksitVadesi, out var iv) ? iv.Date : bas.Date.AddDays(5));
                }
                if (veri != null && veri.Length > 0) p.Dosyalar.Add(new SgDosya { No = ++_sonDosya, Tur = "Poliçe", Dosya = dosyaAdi, Tip = icerikTipi ?? "application/octet-stream", Boyut = veri.LongLength, Veri = veri, Tarih = DateTime.Now, Kullanici = kullanici });
                Log(p, yeni ? "Poliçe Kaydı" : "Güncellendi", $"{p.Tur} · {p.Sirket}{(p.Plaka != "" ? " · " + p.Plaka : "")} · {p.Sigortaci} · prim {p.Prim.ToString("N2", Tr)} ₺ · {p.TaksitSayisi} taksit" + (veri != null && veri.Length > 0 ? $" · {dosyaAdi} yüklendi" : ""), kullanici);
                return (true, $"{p.No} {(yeni ? "kaydedildi" : "güncellendi")}: {p.Tur} · {p.Sirket} · {p.TaksitSayisi} taksit.", p);
            }
        }
        public static (bool ok, string mesaj, SgPolice p) TaksitOde(SgTaksitIstek i, string kullanici)
        {
            var p = Bul(i?.Id ?? 0); if (p == null) return (false, "Poliçe bulunamadı.", null); var t = p.Taksitler.FirstOrDefault(x => x.No == i.No); if (t == null) return (false, "Taksit bulunamadı.", p);
            lock (Kilit)
            {
                if (i.GeriAl) { if (!t.OdemeTarihi.HasValue) return (false, "Taksit zaten ödenmemiş.", p); t.OdemeTarihi = null; t.Yontem = ""; t.Dekont = ""; Log(p, "Taksit Ödemesi Geri Alındı", $"{t.No}. taksit · {t.Tutar.ToString("N2", Tr)} ₺", kullanici); return (true, $"{p.No} {t.No}. taksit ödemesi geri alındı.", p); }
                if (t.OdemeTarihi.HasValue) return (false, $"{t.No}. taksit zaten ödendi ({t.OdemeTarihi:dd.MM.yyyy}).", p);
                t.OdemeTarihi = DateTime.TryParse(i.OdemeTarihi, out var d) ? d.Date : DateTime.Today; t.Yontem = OdemeYontemleri.Contains(i.Yontem ?? "") ? i.Yontem : p.OdemeYontemi; t.Dekont = (i.Dekont ?? "").Trim();
                Log(p, "Taksit Ödendi", $"{t.No}/{p.TaksitSayisi}. taksit · {t.Tutar.ToString("N2", Tr)} ₺ · {t.Yontem}" + (t.Dekont != "" ? " · " + t.Dekont : "") + $" · kalan {p.Kalan.ToString("N2", Tr)} ₺", kullanici);
            }
            return (true, $"{p.No}: {t.No}. taksit ödendi · kalan {p.Kalan.ToString("N2", Tr)} ₺.", p);
        }
        public static (bool ok, string mesaj, SgPolice p) HasarEkle(SgHasarIstek i, string kullanici)
        {
            var p = Bul(i?.Id ?? 0); if (p == null) return (false, "Poliçe bulunamadı.", null); if (string.IsNullOrWhiteSpace(i.Aciklama)) return (false, "Hasar açıklaması girin.", p);
            lock (Kilit) { var h = new SgHasar { No = p.Hasarlar.Count + 1, Tarih = DateTime.TryParse(i.Tarih, out var d) ? d.Date : DateTime.Today, DosyaNo = string.IsNullOrWhiteSpace(i.DosyaNo) ? $"HSR-{DateTime.Today.Year}-{p.Id:000}{p.Hasarlar.Count + 1}" : i.DosyaNo.Trim(), Aciklama = i.Aciklama.Trim(), Tutar = Math.Max(0, i.Tutar), Durum = HasarDurumlari.Contains(i.Durum ?? "") ? i.Durum : "İnceleniyor" }; p.Hasarlar.Add(h); Log(p, "Hasar Kaydı", $"{h.DosyaNo} · {h.Aciklama} · {h.Tutar.ToString("N2", Tr)} ₺ · {h.Durum}", kullanici); }
            return (true, $"{p.No}: hasar kaydı eklendi.", p);
        }
        public static (bool ok, string mesaj, SgPolice p) DosyaEkle(int id, string tur, string dosyaAdi, string icerikTipi, byte[] veri, string kullanici)
        {
            var p = Bul(id); if (p == null) return (false, "Poliçe bulunamadı.", null); if (veri == null || veri.Length == 0) return (false, "Dosya seçin.", p); if (veri.LongLength > 12_000_000) return (false, "Dosya 12 MB'ı aşamaz.", p);
            lock (Kilit) { p.Dosyalar.Add(new SgDosya { No = ++_sonDosya, Tur = string.IsNullOrWhiteSpace(tur) ? "Belge" : tur.Trim(), Dosya = dosyaAdi, Tip = icerikTipi ?? "application/octet-stream", Boyut = veri.LongLength, Veri = veri, Tarih = DateTime.Now, Kullanici = kullanici }); Log(p, "Dosya Yüklendi", $"{tur}: {dosyaAdi} · {veri.LongLength / 1024} KB", kullanici); }
            return (true, $"{p.No}: {dosyaAdi} yüklendi.", p);
        }
        /// <summary>Yenileme: eski poliçe "Yenilendi", yeni dönem poliçesi (aynı konu / teminat, yeni prim ve taksit planı) açılır.</summary>
        public static (bool ok, string mesaj, SgPolice p) Yenile(SgYenileIstek i, string kullanici)
        {
            var eski = Bul(i?.Id ?? 0); if (eski == null) return (false, "Poliçe bulunamadı.", null); if (eski.Durum == "Yenilendi") return (false, "Poliçe zaten yenilendi.", eski); if (i.Prim <= 0) return (false, "Yeni prim girin.", eski);
            var bas = DateTime.TryParse(i.Baslangic, out var b0) ? b0.Date : eski.Bitis.AddDays(1);
            lock (Kilit)
            {
                var p = new SgPolice { Id = ++_sonId, No = string.IsNullOrWhiteSpace(i.No) ? $"{(eski.Tur == "Kasko" ? "KSK" : eski.Tur == "Trafik" ? "TRF" : "POL")}-{bas.Year}-{_sonId:000000}" : i.No.Trim(), Tur = eski.Tur, Sirket = eski.Sirket, Sigortaci = string.IsNullOrWhiteSpace(i.Sigortaci) ? eski.Sigortaci : i.Sigortaci.Trim(), Acente = eski.Acente, Plaka = eski.Plaka, Konu = eski.Konu, Adres = eski.Adres, Baslangic = bas, Bitis = bas.AddYears(1).AddDays(-1), Prim = Math.Round(i.Prim, 2), SigortaBedeli = eski.SigortaBedeli, Teminatlar = eski.Teminatlar.ToList(), Muafiyet = eski.Muafiyet, HasarsizlikKademesi = eski.Hasarlar.Count == 0 ? Math.Min(4, eski.HasarsizlikKademesi + 1) : Math.Max(0, eski.HasarsizlikKademesi - 1), TaksitSayisi = Math.Max(1, Math.Min(12, i.TaksitSayisi)), OdemeYontemi = OdemeYontemleri.Contains(i.OdemeYontemi ?? "") ? i.OdemeYontemi : eski.OdemeYontemi, Sorumlu = eski.Sorumlu, OncekiId = eski.Id };
                p.Taksitler = TaksitPlani(p.Prim, p.TaksitSayisi, bas.AddDays(5)); eski.Durum = "Yenilendi"; eski.YenilenenId = p.Id; _policeler.Add(p);
                Log(eski, "Yenilendi", $"yeni poliçe {p.No} · prim {p.Prim.ToString("N2", Tr)} ₺ · {p.TaksitSayisi} taksit", kullanici); Log(p, "Poliçe Kaydı", $"{eski.No} poliçesinin yenilemesi · {p.Sigortaci} · prim {p.Prim.ToString("N2", Tr)} ₺ · {p.TaksitSayisi} taksit · hasarsızlık kademesi {p.HasarsizlikKademesi}", kullanici);
                return (true, $"{eski.No} yenilendi → {p.No} · {p.Baslangic:dd.MM.yyyy} – {p.Bitis:dd.MM.yyyy} · {p.TaksitSayisi} taksit.", p);
            }
        }
        public static (bool ok, string mesaj, SgPolice p) Iptal(int id, string neden, string kullanici)
        {
            var p = Bul(id); if (p == null) return (false, "Poliçe bulunamadı.", null); if (string.IsNullOrWhiteSpace(neden)) return (false, "İptal nedeni girin.", p);
            lock (Kilit) { p.Durum = "İptal"; Log(p, "İptal", neden.Trim(), kullanici); }
            return (true, $"{p.No} iptal edildi.", p);
        }

        public static object Json(SgPolice p)
        {
            var a = p.Plaka != "" ? AracBakimOrnek.AracBul(p.Plaka) : null;
            return new
            {
                p.Id, no = p.No, tur = p.Tur, sirket = p.Sirket, sigortaci = p.Sigortaci, acente = p.Acente, plaka = p.Plaka, arac = a == null ? "" : $"{a.Marka} {a.Model} · {a.Yil}", zimmet = a?.Zimmet, konu = p.Konu, adres = p.Adres, baslangic = p.Baslangic.ToString("yyyy-MM-dd"), bitis = p.Bitis.ToString("yyyy-MM-dd"), kalan = p.KalanGun,
                prim = p.Prim, pb = p.ParaBirimi, bedel = p.SigortaBedeli, teminatlar = p.Teminatlar, muafiyet = p.Muafiyet, kademe = p.HasarsizlikKademesi, taksitSayisi = p.TaksitSayisi, odemeYontemi = p.OdemeYontemi, odenen = p.Odenen, kalanTutar = p.Kalan, gecikenTaksit = p.GecikenTaksit, odenenTaksit = p.Taksitler.Count(t => t.OdemeTarihi.HasValue),
                sonrakiTaksit = p.Taksitler.Where(t => !t.OdemeTarihi.HasValue).OrderBy(t => t.Vade).Select(t => new { t.No, vade = t.Vade.ToString("yyyy-MM-dd"), t.Tutar, t.Durum }).FirstOrDefault(),
                taksitler = p.Taksitler.Select(t => new { t.No, vade = t.Vade.ToString("yyyy-MM-dd"), ay = t.Vade.ToString("yyyy-MM"), t.Tutar, odemeTarihi = t.OdemeTarihi?.ToString("yyyy-MM-dd"), t.Yontem, t.Dekont, t.Durum }).ToList(),
                hasarlar = p.Hasarlar.Select(h => new { h.No, tarih = h.Tarih.ToString("yyyy-MM-dd"), h.DosyaNo, h.Aciklama, h.Tutar, h.Durum }).ToList(), durum = p.HesapDurum, kayitDurum = p.Durum, sorumlu = p.Sorumlu, not = p.Not, oncekiId = p.OncekiId, yenilenenId = p.YenilenenId, aracPolicesi = p.AracPolicesi,
                dosyalar = p.Dosyalar.Select(d => new { d.No, d.Tur, d.Dosya, d.Tip, d.Boyut, tarih = d.Tarih.ToString("yyyy-MM-dd HH:mm"), d.Kullanici, gercek = d.Veri != null }).ToList(),
                hareketler = p.Hareketler.OrderByDescending(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("yyyy-MM-dd HH:mm"), h.Kullanici, h.Islem, h.Aciklama }).ToList()
            };
        }
        public static object Ozet()
        {
            var l = Policeler.Where(p => p.Durum != "Yenilendi" && p.Durum != "İptal").ToList(); var bugun = DateTime.Today; var tumTaksit = l.SelectMany(p => p.Taksitler.Select(t => (p, t))).ToList();
            return new
            {
                police = l.Count, aktif = l.Count(p => p.HesapDurum == "Aktif"), yaklasan = l.Count(p => p.HesapDurum == "Yenileme Yaklaşıyor"), dolan = l.Count(p => p.HesapDurum == "Süresi Doldu"), arac = l.Count(p => p.AracPolicesi), yillikPrim = l.Sum(p => p.Prim), odenen = l.Sum(p => p.Odenen), kalan = l.Sum(p => p.Kalan),
                gecikenTaksit = tumTaksit.Count(x => x.t.Durum == "Gecikti"), gecikenTutar = tumTaksit.Where(x => x.t.Durum == "Gecikti").Sum(x => x.t.Tutar), buAyTaksit = tumTaksit.Count(x => !x.t.OdemeTarihi.HasValue && x.t.Vade.Year == bugun.Year && x.t.Vade.Month == bugun.Month), buAyTutar = tumTaksit.Where(x => !x.t.OdemeTarihi.HasValue && x.t.Vade.Year == bugun.Year && x.t.Vade.Month == bugun.Month).Sum(x => x.t.Tutar), hasar = l.Sum(p => p.Hasarlar.Count), hasarTutar = l.Sum(p => p.Hasarlar.Sum(h => h.Tutar)),
                aylar = Enumerable.Range(0, 12).Select(i => { var ay = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(i); return new { ay = ay.ToString("yyyy-MM"), ad = ay.ToString("MMM yy", Tr), tutar = tumTaksit.Where(x => x.t.Vade.Year == ay.Year && x.t.Vade.Month == ay.Month).Sum(x => x.t.Tutar), odenen = tumTaksit.Where(x => x.t.OdemeTarihi.HasValue && x.t.Vade.Year == ay.Year && x.t.Vade.Month == ay.Month).Sum(x => x.t.Tutar), adet = tumTaksit.Count(x => x.t.Vade.Year == ay.Year && x.t.Vade.Month == ay.Month), yenileme = l.Count(p => p.Bitis.Year == ay.Year && p.Bitis.Month == ay.Month) }; }).ToList(),
                turler = Turler.Select(t => new { tur = t, adet = l.Count(p => p.Tur == t), prim = l.Where(p => p.Tur == t).Sum(p => p.Prim) }).Where(x => x.adet > 0).ToList(),
                sirketler = Sirketler.Select(s => new { sirket = s, adet = l.Count(p => p.Sirket == s), prim = l.Where(p => p.Sirket == s).Sum(p => p.Prim), geciken = l.Where(p => p.Sirket == s).Sum(p => p.GecikenTaksit) }).Where(x => x.adet > 0).ToList(),
                sigortacilar = l.GroupBy(p => p.Sigortaci).Select(g => new { sigortaci = g.Key, adet = g.Count(), prim = g.Sum(p => p.Prim) }).OrderByDescending(x => x.prim).ToList()
            };
        }

        // ---------- poliçe özeti PDF (dosya yoksa) ----------
        public static DaBelge PoliceOzeti(SgPolice p, string hazirlayan)
        {
            var d = new DaBelge { Baslik = $"{p.Tur} Poliçesi · {p.No}", AltBaslik = $"{p.Sirket} · {p.Sigortaci} · {p.Acente}", Sirket = p.Sirket, Hazirlayan = hazirlayan, Yatay = false };
            d.Ozet.Add(("Konu", p.Konu)); d.Ozet.Add(("Dönem", $"{p.Baslangic:dd.MM.yyyy} – {p.Bitis:dd.MM.yyyy}")); d.Ozet.Add(("Prim", p.Prim.ToString("N2", Tr) + " ₺")); d.Ozet.Add(("Taksit", $"{p.TaksitSayisi} · {p.OdemeYontemi}")); d.Ozet.Add(("Durum", p.HesapDurum)); d.Ozet.Add(("Ödenen / Kalan", $"{p.Odenen.ToString("N0", Tr)} / {p.Kalan.ToString("N0", Tr)} ₺"));
            var t = new DaBlok { Baslik = "Teminatlar", Kolonlar = { "No", "Teminat" } }; int n = 0; foreach (var x in p.Teminatlar) t.Satirlar.Add(new[] { (++n).ToString(), x }); t.Not = (p.SigortaBedeli > 0 ? $"Sigorta bedeli {p.SigortaBedeli.ToString("N0", Tr)} ₺ · " : "") + (p.Muafiyet != "" ? "Muafiyet: " + p.Muafiyet + " · " : "") + (p.HasarsizlikKademesi > 0 ? $"Hasarsızlık kademesi {p.HasarsizlikKademesi}" : "") + (p.Plaka != "" ? $" · Plaka {p.Plaka}" : "") + (p.Adres != "" ? $" · Adres {p.Adres}" : ""); d.Bloklar.Add(t);
            var tk = new DaBlok { Baslik = "Taksit Planı", Kolonlar = { "No", "Vade", "Tutar (₺)", "Durum", "Ödeme Tarihi", "Yöntem", "Dekont" }, Sag = { false, false, true, false, false, false, false } }; foreach (var x in p.Taksitler) tk.Satirlar.Add(new[] { $"{x.No}/{p.TaksitSayisi}", x.Vade.ToString("dd.MM.yyyy"), x.Tutar.ToString("N2", Tr), x.Durum, x.OdemeTarihi?.ToString("dd.MM.yyyy") ?? "", x.Yontem, x.Dekont }); tk.Toplam = new[] { "", "Toplam", p.Prim.ToString("N2", Tr), $"ödenen {p.Odenen.ToString("N2", Tr)}", "", "", "" }; d.Bloklar.Add(tk);
            if (p.Hasarlar.Count > 0) { var h = new DaBlok { Baslik = "Hasar Kayıtları", Kolonlar = { "Tarih", "Dosya No", "Açıklama", "Tutar (₺)", "Durum" }, Sag = { false, false, false, true, false } }; foreach (var x in p.Hasarlar) h.Satirlar.Add(new[] { x.Tarih.ToString("dd.MM.yyyy"), x.DosyaNo, x.Aciklama, x.Tutar.ToString("N2", Tr), x.Durum }); d.Bloklar.Add(h); }
            var g = new DaBlok { Baslik = "Günlük", Kolonlar = { "Zaman", "Kullanıcı", "İşlem", "Açıklama" } }; foreach (var x in p.Hareketler.OrderByDescending(x => x.Zaman)) g.Satirlar.Add(new[] { x.Zaman.ToString("dd.MM.yyyy HH:mm"), x.Kullanici, x.Islem, x.Aciklama }); g.Not = "Ön yüz örneği: poliçenin aslı yüklenmediği için özet sayfası üretildi."; d.Bloklar.Add(g);
            return d;
        }
    }
}
