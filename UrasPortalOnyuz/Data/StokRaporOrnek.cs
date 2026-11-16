// ============================================================
// KONSOLİDE STOK RAPORU (/StokRaporu, 13 Kas 2026) — "URAS Fiili ve Resmî Stok Raporu" Excel şablonunun (Stok_Verisi, Stok_Raporu, Tuketim_Raporu, Sayim_ve_Risk,
// Stok_Paneli, Tuketim_Paneli, Ayarlar) portal karşılığı. Kullanıcı isteği: hammadde stoğu ve bitmiş ürün stoğu ayrı izlensin; ambalaj ve öbür stoklar
// (sarf, yedek parça, kırtasiye — "A4'e kadar") da takip edilsin; stoklar ve tüketim R (resmî, kayıtlı) ve X (kayıt dışı fark) olarak görünsün.
//
// Tanımlar (şablondaki kurallar):
//   R Miktar  = resmî (muhasebe / ERP) stok miktarı.                Fiili Miktar = sayılan + sayım sonrası giriş − sayım sonrası çıkış.
//   X Miktar  = Fiili − R (pozitif: fiili fazla, negatif: fiili eksik). Değer farkı = miktar etkisi (X × resmî maliyet) + maliyet etkisi (fiili × maliyet farkı).
//   Kullanılabilir = fiili − bloke / karantina − rezerve; yalnız Depoda / Dış Depoda ve Gruba Ait stokta (Transferde / Konsinyede stok kullanılabilir sayılmaz).
//   Tüketim: dönem tüketimi R + X; günlük ortalama = tüketim ÷ dönem günü; yeterlilik = kullanılabilir ÷ günlük; hedef stok = günlük × hedef gün + güvenlik stoku;
//   önerilen alım = en yüksek (0; hedef − (kullanılabilir + açık alım + yoldaki alım)). Öncelik: Tüketim Yok · Acil Alım · Kritik · Yakın Takip · Yeterli.
// Türetilen bütün alanlar tarayıcıda hesaplanır (wwwroot/js/stok-raporu.js); buradan ham kayıtlar gider.
//
// VERİ: portalda gerçek stok kaynağı bağlı değildir; kayıtlar ÖRNEKTİR. Ürünler portalın öbür ekranlarındaki tanımlardan alınır: bitmiş ürünler Fiyat Yönetimi
// (Data/FiyatOrnek.cs), ambalaj ve bazı hammadde / sarf / yedek parça kalemleri Alım Onay fiyat listesinden; kalanı aşağıdaki katalogdan. Miktar, sayım ve tüketim
// değerleri stok koduna göre deterministik üretilir (her açılışta aynı).
// Ekran: Controllers/StokRaporuController.cs + Views/StokRaporu/Index.cshtml + wwwroot/js/stok-raporu.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public static class StokRaporOrnek
    {
        /// <summary>Şablondaki Ayarlar sayfasının başlangıç değerleri (ekranda değiştirilebilir).</summary>
        public const int HedefStokGunu = 90, SktUyariGunu = 90, YavasGun = 90, AtilGun = 180, TuketimDonemGunu = 90;
        public static readonly string[] StokTipleri = { "Hammadde", "Yarı Mamul", "Mamul", "Ticari Mal", "Ambalaj Malzemesi", "Sarf Malzeme", "Kırtasiye / Ofis", "Yedek Parça", "Makine / Ekipman", "Küçük Demirbaş", "Numune", "Hurda / Fire" };

        private static string Gun(DateTime? t) => t.HasValue ? t.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";
        private static int Tohum(string s) { unchecked { uint h = 2166136261; foreach (char c in s ?? "") { h ^= c; h *= 16777619; } return (int)(h & 0x7fffffff); } }

        private class Kalem { public string Tip, Kod, Ad, Birim, AnaGrup = "", Grup = "", Sinif = "", UrunGrubu = "", Seri = "", Tedarikci = ""; public decimal Maliyet, Olcek; public bool Skt; public string[] Sirketler; }

        // (tip, kod, ad, birim, birim maliyet TL, tipik stok miktarı, SKT takibi, tedarikçi, şirketler)
        private static Kalem K(string tip, string kod, string ad, string birim, decimal maliyet, decimal olcek, bool skt, string ted, params string[] sirketler)
            => new Kalem { Tip = tip, Kod = kod, Ad = ad, Birim = birim, Maliyet = maliyet, Olcek = olcek, Skt = skt, Tedarikci = ted, Sirketler = sirketler.Length == 0 ? new[] { "Uras Kimya" } : sirketler };

        private static List<Kalem> Katalog()
        {
            const string U = "Uras Kimya", A = "Alv Kimya", S = "Selvi", P = "Avrupa Paper", D = "Daf Kimya";
            var l = new List<Kalem>
            {
                // ---- HAMMADDE
                K("Hammadde", "HM-PGM-01", "Pigment Mavi 15:3", "kg", 412m, 6_500, true, "Heubach Pigment", U, D), K("Hammadde", "HM-PGM-02", "Pigment Kırmızı 146", "kg", 468m, 4_200, true, "Heubach Pigment", U, D),
                K("Hammadde", "HM-PGM-03", "Pigment Sarı 14", "kg", 388m, 3_800, true, "Heubach Pigment", U), K("Hammadde", "HM-PGM-04", "Karbon Siyahı N330", "kg", 96m, 9_000, false, "Orion Carbons", U, D),
                K("Hammadde", "HM-TIO2-22", "Titanyum Dioksit Rutil", "kg", 118m, 22_000, false, "Brenntag Kimya", U, A, D), K("Hammadde", "HM-AKR-11", "Akrilik Emülsiyon (Binder Reçine)", "kg", 54m, 48_000, true, "BASF Türk", U, A),
                K("Hammadde", "HM-STR-12", "Stiren Akrilik Kopolimer", "kg", 49m, 36_000, true, "Organik Kimya", A, U), K("Hammadde", "HM-KVM-13", "Kıvamlaştırıcı (Sentetik)", "kg", 86m, 12_000, true, "BASF Türk", U, A),
                K("Hammadde", "HM-AMN-14", "Amonyak Çözeltisi %25", "kg", 14.5m, 8_000, false, "Brenntag Kimya", U, A), K("Hammadde", "HM-GLK-15", "Monoetilen Glikol", "kg", 38m, 14_000, false, "Petkim Petrokimya", U, D),
                K("Hammadde", "HM-EA-02", "Etil Asetat Teknik", "kg", 38.5m, 11_000, false, "Petkim Petrokimya", U, D), K("Hammadde", "HM-SLK-16", "Silikon Yumuşatıcı Bazı", "kg", 142m, 5_500, true, "Wacker Kimya", U, A),
                K("Hammadde", "HM-DSP-17", "Dispergatör (Islatıcı)", "kg", 128m, 3_200, true, "BYK Kimya", U), K("Hammadde", "HM-NAOH-21", "Sodyum Hidroksit %50 Çözelti", "kg", 14.25m, 18_000, false, "Brenntag Kimya", U, A, D),
                K("Hammadde", "HM-KGT-31", "Baz Kağıt Bobin (Kaplamasız)", "kg", 34m, 120_000, false, "Mondi Kağıt", P), K("Hammadde", "HM-KPL-32", "Süblime Kaplama Kimyasalı", "kg", 72m, 16_000, true, "Uras Kimya", P),
                // ---- YARI MAMUL
                K("Yarı Mamul", "YM-PST-01", "Emprime Baz Pasta", "kg", 62m, 14_000, true, "", U), K("Yarı Mamul", "YM-DSP-02", "Pigment Dispersiyonu Mavi", "kg", 185m, 3_600, true, "", U),
                K("Yarı Mamul", "YM-DSP-03", "Pigment Dispersiyonu Kırmızı", "kg", 205m, 2_900, true, "", U), K("Yarı Mamul", "YM-KGT-04", "Kaplanmış Jumbo Bobin", "kg", 58m, 40_000, false, "", P),
                // ---- AMBALAJ
                K("Ambalaj Malzemesi", "AM-TNK-06", "Metal Bidon 25 L", "adet", 265m, 4_200, false, "Sarten Ambalaj", U, A, D), K("Ambalaj Malzemesi", "AM-BDN-07", "Plastik Bidon 10 L", "adet", 84m, 3_000, false, "Sarten Ambalaj", U, A),
                K("Ambalaj Malzemesi", "AM-BDN-08", "Plastik Bidon 5 L", "adet", 52m, 2_600, false, "Sarten Ambalaj", U, S), K("Ambalaj Malzemesi", "AM-IBC-05", "IBC Tank 1000 L (yeni)", "adet", 4_150m, 180, false, "Sarten Ambalaj", U, A),
                K("Ambalaj Malzemesi", "AM-VRL-09", "Çelik Varil 200 L", "adet", 1_280m, 320, false, "Sarten Ambalaj", U, D), K("Ambalaj Malzemesi", "AM-KOLI-01", "Oluklu Mukavva Koli 40x30x30", "adet", 18.4m, 14_000, false, "Korozo Ambalaj", U, S, P),
                K("Ambalaj Malzemesi", "AM-STRC-02", "Streç Film 50cm 17µ", "rulo", 214m, 620, false, "Korozo Ambalaj", U, A, S, P), K("Ambalaj Malzemesi", "AM-PLT-10", "Ahşap Palet 100x120", "adet", 310m, 900, false, "Palet Dünyası", U, A, P, S),
                K("Ambalaj Malzemesi", "AM-ETK-11", "Ürün Etiketi (Rulo 1000'li)", "rulo", 148m, 260, false, "Etiket Matbaa", U, A, S), K("Ambalaj Malzemesi", "AM-KPK-12", "Bidon Kapağı Emniyetli", "adet", 6.8m, 9_500, false, "Sarten Ambalaj", U, A),
                K("Ambalaj Malzemesi", "AM-MSR-13", "Karton Masura 76 mm", "adet", 21m, 5_400, false, "Korozo Ambalaj", P),
                // ---- SARF
                K("Sarf Malzeme", "SF-ELD-02", "İş Eldiveni Nitril", "çift", 42m, 1_800, false, "Ofix Ofis", U, A, P, D), K("Sarf Malzeme", "SF-MSK-03", "Toz Maskesi FFP2", "adet", 11.5m, 2_400, true, "Ofix Ofis", U, A, P),
                K("Sarf Malzeme", "SF-FLT-04", "Filtre Torbası 100 µ", "adet", 96m, 420, false, "Filtre Teknik", U, A), K("Sarf Malzeme", "SF-TMZ-05", "Endüstriyel Temizlik Kimyasalı", "lt", 58m, 600, true, "Anadolu Temizlik", U, A, P, S),
                K("Sarf Malzeme", "SF-BNT-06", "Koli Bandı 45 mm", "adet", 24m, 1_500, false, "Ofix Ofis", U, S, P), K("Sarf Malzeme", "SF-YAG-07", "Makine Yağı ISO 46", "lt", 132m, 380, false, "Petrol Ofisi", U, P),
                K("Sarf Malzeme", "SF-LAB-08", "Laboratuvar Sarf Seti (pH, Beher)", "takım", 1_850m, 24, false, "Lab Market", U, A),
                // ---- KIRTASİYE / OFİS ("A4'e kadar")
                K("Kırtasiye / Ofis", "SF-KRT-01", "Fotokopi Kağıdı A4 80 gr", "koli", 1_180m, 60, false, "Ofix Ofis", U, A, S, P, D), K("Kırtasiye / Ofis", "KR-TNR-02", "Yazıcı Toneri (Siyah)", "adet", 2_450m, 18, false, "Ofix Ofis", U, A, S, P),
                K("Kırtasiye / Ofis", "KR-KLS-03", "Geniş Klasör", "adet", 68m, 240, false, "Ofix Ofis", U, S, P), K("Kırtasiye / Ofis", "KR-KLM-04", "Tükenmez Kalem (50'li Kutu)", "kutu", 320m, 22, false, "Ofix Ofis", U, A, S),
                K("Kırtasiye / Ofis", "KR-ZMB-05", "Zımba Teli 24/6 (Kutu)", "kutu", 38m, 90, false, "Ofix Ofis", U, S), K("Kırtasiye / Ofis", "KR-A3-06", "Fotokopi Kağıdı A3 80 gr", "koli", 2_360m, 12, false, "Ofix Ofis", U, P),
                // ---- YEDEK PARÇA
                K("Yedek Parça", "YP-RUL-6205", "Rulman 6205-2RS", "adet", 312m, 64, false, "SKF Türk", U, P), K("Yedek Parça", "YP-RUL-22218", "Rulman 22218 E", "adet", 4_820m, 8, false, "SKF Türk", U, P),
                K("Yedek Parça", "YP-HID-01", "Hidrolik Pompa A10VSO", "adet", 68_500m, 2, false, "Bosch Rexroth", U), K("Yedek Parça", "YP-VLF-02", "Pnömatik Valf 5/2", "adet", 2_140m, 14, false, "Bosch Rexroth", U, P, A),
                K("Yedek Parça", "YP-KYS-03", "V Kayışı SPB 2500", "adet", 480m, 36, false, "Kayış Dünyası", U, P), K("Yedek Parça", "YP-KRS-04", "Mikser Karıştırıcı Bıçağı", "adet", 7_600m, 6, false, "Makine Teknik", U, A),
                // ---- MAKİNE / EKİPMAN, NUMUNE, HURDA
                K("Makine / Ekipman", "MK-TRP-01", "Transpalet 2,5 Ton", "adet", 18_500m, 6, false, "Jungheinrich", U, A, P, S), K("Makine / Ekipman", "MK-TRZ-02", "Platform Terazi 600 kg", "adet", 24_000m, 3, false, "Tartı Sistemleri", U, P),
                K("Numune", "NM-EMP-01", "Emprime Boya Numune Seti", "takım", 640m, 85, true, "", U, S), K("Numune", "NM-KGT-02", "Süblime Kağıt Numune Rulosu", "rulo", 410m, 40, false, "", P),
                K("Hurda / Fire", "HR-BYA-01", "Standart Dışı Boya (Fire)", "kg", 12m, 2_800, false, "", U), K("Hurda / Fire", "HR-KGT-02", "Kağıt Kenar Firesi", "kg", 3.2m, 9_500, false, "", P)
            };
            // ---- BİTMİŞ ÜRÜN: Fiyat Yönetimi ürünleri (maliyet = üretim + ambalaj, TL) — Uras Kimya'da Mamul, Selvi ve Alv Kimya'da Ticari Mal
            decimal kur = FiyatOrnek.Kur.UsdTl;
            foreach (var u in FiyatOrnek.Urunler)
            {
                var mamul = K("Mamul", u.Kod, u.Ad, u.Birim ?? "kg", Math.Round(FiyatOrnek.GercekMaliyet(u) * kur, 2), 6_000, true, "", "Uras Kimya");
                mamul.AnaGrup = u.AnaGrup; mamul.Grup = u.Grup; mamul.Sinif = u.UrunSinifi; mamul.UrunGrubu = u.UrunGrubu; mamul.Seri = u.Seri; l.Add(mamul);
                int t = Tohum(u.Kod) % 5;
                if (t <= 2) { var tm = K("Ticari Mal", u.Kod, u.Ad, u.Birim ?? "kg", Math.Round(FiyatOrnek.SaticiFiyati(FiyatOrnek.Saticilar[t == 2 ? 1 : 0], u) * kur, 2), 1_800, true, "Uras Kimya", t == 2 ? "Alv Kimya" : "Selvi"); tm.AnaGrup = u.AnaGrup; tm.Grup = u.Grup; tm.Sinif = u.UrunSinifi; tm.UrunGrubu = u.UrunGrubu; tm.Seri = u.Seri; l.Add(tm); }
            }
            foreach (var gr in new[] { 45, 57, 70, 90, 100 })
            {
                var m = K("Mamul", $"KG-SK-{gr:000}", $"Süblime Kağıt {gr} gr", "kg", 46m + gr * 0.18m, 55_000, false, "", "Avrupa Paper"); m.AnaGrup = "Kağıt Grubu"; m.Grup = "Süblime Kağıt"; m.Sinif = "Süblimasyon"; m.UrunGrubu = gr <= 57 ? "Hızlı Kuruyan" : "Standart"; m.Seri = "SK Serisi"; l.Add(m);
            }
            return l;
        }

        private static readonly Lazy<object> _paket = new Lazy<object>(Olustur);
        public static object Paket() => _paket.Value;

        private static object Olustur()
        {
            var bugun = DateTime.Today; var satirlar = new List<object>(); var planlar = new List<object>(); int no = 0, planNo = 0;
            string[] sorumlular = { "Hakan Yıldız", "Deniz Çelik", "Ayşe Demir", "Emre Taş", "Gizem Tan" };
            string[] nedenler = { "Sayım Farkı", "Kayıt Dışı Giriş", "Fire / Zayi", "Geciken Fatura", "Birim Dönüşümü", "Üretim Sarf Kaydı Eksik" };
            string[] aksiyonlar = { "Yeniden sayım yapılacak", "Giriş belgesi tamamlanacak", "Fire tutanağı düzenlenecek", "Tedarikçi faturası beklenecek", "Birim çevrimi düzeltilecek", "Sarf fişi kesilecek" };
            // ---- UYUM KONTROLÜ GİRDİLERİ (14 Kas 2026: Stok ve Tüketim Uyum Raporu bu rapora katıldı) ----
            // Tüketim belgeleri: planın dönem tüketimi (R muhasebe çıkışı, R + X fiili, reçete) dönemin aylarına gün sayısıyla, ay içinde 1 – 2 belgeye bölünür;
            //   belgelerin toplamı plan satırındaki dönem tüketimine eşittir (Tüketim ve Yeterlilik ↔ Tüketim Verisi tutarlı). Her belgede iki reçete vardır:
            //   recete = gerçek reçete (fiili tüketimle karşılaştırılır), receteR = muhasebe reçetesi (muhasebe çıkışıyla karşılaştırılır). İki formül birbirinden bağımsızdır.
            // Aylık hareket: ay sonu stok zinciri bugünkü stoktan geriye kurulur — önceki ay sonu = ay sonu + tüketim − giriş − fark. Giriş ve fark örnek değerlerdir.
            var belgeler = new List<object>(); var hareketler = new List<object>(); int belgeNo = 0;
            // GİRİŞ BELGELERİ (15 Kas 2026, "stok koduna tıkladığımızda o ürünün geçmişini görelim: ne zaman gelmiş, hangi tedarikçi"): her stok kartının giriş kayıtları —
            //   tarih, giriş türü (Satınalma · Grup İçi Alım · Üretimden Giriş), tedarikçi, irsaliye / üretim fişi no, fatura no, parti / lot, fiili miktar, muhasebe (faturalı) miktar, birim fiyat.
            //   Dönem içi girişlerin ay toplamı aylık hareketteki "Ay İçi Giriş" ile aynıdır (fiili ve muhasebe ayrı ayrı). Stok satırındaki partinin geliş tarihi ve sayım sonrası giriş de
            //   birer giriş belgesidir (zorunlu girişler); dönem öncesi girişler örnek geçmiş kayıtlardır. ÖRNEKTİR — portalda gerçek alım / irsaliye kaynağı bağlı değildir.
            var girisler = new List<object>(); int girisNo = 0; DateTime donemBasT = bugun.AddDays(-TuketimDonemGunu), ilkAyBasi = new DateTime(donemBasT.Year, donemBasT.Month, 1);
            string[] grupSirketleri = { "Uras Kimya", "Alv Kimya", "Selvi", "Avrupa Paper", "Daf Kimya", "Uras Holding", "Uras Power" };
            DateTime donemBas = bugun.AddDays(-TuketimDonemGunu), donemBit = bugun.AddDays(-1);
            var dilimler = new List<(string Ay, DateTime Bas, DateTime Bit)>();
            for (var a = new DateTime(donemBas.Year, donemBas.Month, 1); a <= donemBit; a = a.AddMonths(1))
            { var b1 = a < donemBas ? donemBas : a; var b2 = a.AddMonths(1).AddDays(-1); if (b2 > donemBit) b2 = donemBit; dilimler.Add((a.ToString("yyyy-MM", CultureInfo.InvariantCulture), b1, b2)); }
            void Uyum(Kalem k, string sirket, string depoKod, bool tamBirim, decimal fiili, decimal muh, decimal tR, decimal tX, decimal? recete, decimal? receteR, Random u, decimal olcekK, List<(DateTime T, string Lot, decimal M)> zorunlu, DateTime sayimMin)
            {
                decimal Y(decimal v) => tamBirim ? Math.Round(v, 0) : Math.Round(v, 1);
                // giriş belgeleri ayrı rastgele dizi kullanır (öbür örnek değerler kaymaz)
                var ug = new Random(Tohum(k.Kod + "|" + sirket + "|" + k.Tip + "|giris")); bool uretimGirisi = string.IsNullOrEmpty(k.Tedarikci);
                string girisTuru = uretimGirisi ? "Üretimden Giriş" : grupSirketleri.Contains(k.Tedarikci) ? "Grup İçi Alım" : "Satınalma";
                bool xYogunTip = k.Tip == "Hammadde" || k.Tip == "Yarı Mamul" || k.Tip == "Mamul" || k.Tip == "Ambalaj Malzemesi" || k.Tip == "Ticari Mal";
                void GirisEkle(DateTime gt, string lotKodu, decimal gf, decimal gm, bool donemIci)
                {
                    if (gf <= 0 && gm <= 0) return;
                    decimal ayOnce = (decimal)(bugun - gt).Days / 30m;   // eski girişlerde birim fiyat daha düşük
                    decimal fiyat = Math.Round(k.Maliyet * (1m - Math.Min(0.25m, 0.012m * ayOnce)) * (decimal)(0.97 + ug.NextDouble() * 0.06), 2);
                    girisNo++;
                    girisler.Add(new { no = girisNo, tarih = Gun(gt), tur = girisTuru, tedarikci = k.Tedarikci, belge = $"{(uretimGirisi ? "URE" : "IRS")}-{gt:yyMM}-{2000 + girisNo}", fatura = !uretimGirisi && gm > 0 ? $"FTR-{gt:yy}-{ug.Next(10000, 99999)}" : "",
                        sirket, depoKod, kod = k.Kod, lot = lotKodu ?? "", fiili = gf, muhasebe = gm, fiyat, donemIci });
                }
                int n = dilimler.Count; decimal tF = tR + tX, rc = recete ?? 0, rr = receteR ?? 0, kalanF = tF, kalanM = tR, kalanRc = rc, kalanRr = rr; var ayF = new decimal[n]; var ayM = new decimal[n];
                for (int i = 0; i < n; i++)
                {
                    var d = dilimler[i]; int gun = (d.Bit - d.Bas).Days + 1; bool son = i == n - 1; decimal pay = (decimal)gun / TuketimDonemGunu;
                    decimal aF = son ? kalanF : Math.Min(Y(tF * pay), kalanF), aM = son ? kalanM : Math.Min(Y(tR * pay), kalanM), aRc = son ? kalanRc : Math.Min(Y(rc * pay), kalanRc), aRr = son ? kalanRr : Math.Min(Y(rr * pay), kalanRr);
                    kalanF -= aF; kalanM -= aM; kalanRc -= aRc; kalanRr -= aRr; ayF[i] = aF; ayM[i] = aM;
                    if (aF == 0 && aM == 0 && aRc == 0 && aRr == 0) continue;
                    int adet = gun >= 20 && u.Next(100) < 55 ? 2 : 1; decimal bol = (decimal)(0.35 + u.NextDouble() * 0.3);
                    for (int j = 0; j < adet; j++)
                    {
                        bool ilk = adet == 2 && j == 0;
                        decimal bF = ilk ? Y(aF * bol) : adet == 2 ? aF - Y(aF * bol) : aF, bM = ilk ? Y(aM * bol) : adet == 2 ? aM - Y(aM * bol) : aM, bRc = ilk ? Y(aRc * bol) : adet == 2 ? aRc - Y(aRc * bol) : aRc, bRr = ilk ? Y(aRr * bol) : adet == 2 ? aRr - Y(aRr * bol) : aRr;
                        var tarih = d.Bas.AddDays(u.Next(gun));
                        if (tarih >= sayimMin && sayimMin > d.Bas) tarih = d.Bas.AddDays((tarih - d.Bas).Days % (sayimMin - d.Bas).Days);   // sayım tarihinden sonraya belge düşmesin (sayım sonrası çıkış stok satırında ayrı tutulur)
                        belgeler.Add(new { no = ++belgeNo, tarih = Gun(tarih), donemTipi = u.Next(100) < 40 ? "Anlık" : "Aylık", sirket, depoKod, kod = k.Kod, belge = $"TUK-{tarih:yyMM}-{1000 + belgeNo}", recete = recete.HasValue ? bRc : 0m, receteR = receteR.HasValue ? bRr : 0m, fiili = bF, muhasebe = bM });
                    }
                }
                // SATIŞ (15 Kas 2026, "muhasebe stoku satışlar ile uyumlu mu düşüyor"): bitmiş üründe (mamul, ticari mal) ayın faturalı satış miktarı. ÖRNEKTİR —
                // portalda ürün bazında gerçek fatura kalemi yok; ayların dörtte üçünde muhasebe çıkışına eşit, kalanında sapmalı üretilir. Ayrı rastgele dizi kullanır (öbür örnek değerler kaymaz).
                bool satilir = k.Tip == "Mamul" || k.Tip == "Ticari Mal"; var us = new Random(Tohum(k.Kod + "|" + sirket + "|" + k.Tip + "|satis"));
                decimal sF = fiili, sM = muh; bool zorlandi = false;
                for (int i = n - 1; i >= 0; i--)
                {
                    decimal gir = ayF[i] > 0 && u.Next(100) < 55 ? Y(ayF[i] * (decimal)(0.6 + u.NextDouble() * 0.8)) : 0, fF = 0, fM = 0; int z = u.Next(100); double oran = 0.02 + u.NextDouble() * 0.06; int yon = u.Next(2) == 0 ? 1 : -1;
                    if (z < 10 && ayF[i] > 0) fF = Y(ayF[i] * (decimal)oran) * yon; else if (z < 15 && ayM[i] > 0) fM = Y(ayM[i] * (decimal)oran) * yon;
                    decimal girM = gir == 0 ? 0 : u.Next(100) < 35 ? gir : Y(gir * (decimal)(0.55 + u.NextDouble() * 0.4));   // muhasebe girişi (faturalı alım) fiili girişten az olabilir
                    // Zorunlu girişler (stoktaki partinin gelişi, sayım sonrası giriş) bu aya düşüyorsa ayın girişi en az onların toplamıdır. Sayım tarihinden sonraki giriş yalnız
                    // "sayım sonrası giriş" olabilir: öbür girişler sayımdan önceki günlere yazılır, ayda sayımdan önce gün yoksa ayın girişi zorunlu girişlerden ibarettir.
                    // Girişi değişen kartta ay başı stoku eksiye düşürülmez (zorlandi); dokunulmayan kartların değerleri eskisiyle aynıdır.
                    var dl = dilimler[i]; var ayZorunlu = zorunlu.Where(w => w.T.ToString("yyyy-MM", CultureInfo.InvariantCulture) == dl.Ay).OrderBy(w => w.T).ToList(); decimal zT = ayZorunlu.Sum(w => w.M);
                    var sonGun = sayimMin.AddDays(-1) < dl.Bit ? sayimMin.AddDays(-1) : dl.Bit; bool yerYok = sonGun < dl.Bas; decimal hedefGir = yerYok ? zT : Math.Max(gir, zT);
                    if (hedefGir != gir || zorlandi)
                    {
                        decimal gir0 = gir, girM0 = girM, tavanF = Math.Max(0, sF + ayF[i] - fF), tavanM = Math.Max(0, sM + ayM[i] - fM);
                        decimal mOran = gir0 > 0 ? girM0 / gir0 : !xYogunTip || ug.Next(100) < 35 ? 1m : (decimal)(0.55 + ug.NextDouble() * 0.4);
                        gir = Math.Min(hedefGir, tavanF); girM = gir == gir0 ? Math.Min(girM0, tavanM) : Math.Min(Y(gir * mOran), tavanM);
                        if (gir != gir0 || girM != girM0) zorlandi = true;
                    }
                    if (gir > 0)
                    {
                        var parca = new List<(DateTime T, string Lot, decimal F)>(); decimal kalanGir = gir;
                        foreach (var q in ayZorunlu) { decimal pf = Math.Min(q.M, kalanGir); if (pf <= 0) continue; parca.Add((q.T < dl.Bas ? dl.Bas : q.T, q.Lot, pf)); kalanGir -= pf; }
                        if (kalanGir > 0)
                        {
                            int gunS = Math.Max(1, (sonGun - dl.Bas).Days + 1), parcaAdet = gunS >= 20 && kalanGir >= (tamBirim ? 2 : 1) && ug.Next(100) < 40 ? 2 : 1; decimal ilkPay = parcaAdet == 2 ? Y(kalanGir * (decimal)(0.35 + ug.NextDouble() * 0.3)) : kalanGir;
                            for (int pj = 0; pj < parcaAdet; pj++)
                            {
                                var pt = dl.Bas.AddDays(ug.Next(gunS)); decimal pf = pj == 0 ? ilkPay : kalanGir - ilkPay; if (pf <= 0) continue;
                                parca.Add((pt, k.Skt ? $"L{pt:yyMM}-{ug.Next(100, 999)}" : "", pf));
                            }
                        }
                        decimal kalanGirM = girM; parca = parca.OrderBy(w => w.T).ToList();
                        for (int pj = 0; pj < parca.Count; pj++)
                        {
                            decimal pm = pj == parca.Count - 1 ? kalanGirM : Math.Min(Y(parca[pj].F * girM / gir), kalanGirM); kalanGirM -= pm;
                            GirisEkle(parca[pj].T, parca[pj].Lot, parca[pj].F, pm, true);
                        }
                    }
                    decimal oF = sF + ayF[i] - gir - fF, oM = sM + ayM[i] - girM - fM;
                    decimal? satis = satilir ? (us.Next(100) < 75 ? ayM[i] : Y(ayM[i] * (decimal)(0.85 + us.NextDouble() * 0.27))) : (decimal?)null;
                    hareketler.Add(new { ay = dilimler[i].Ay, sirket, depoKod, kod = k.Kod, oncekiFiili = oF, girisFiili = gir, sonFiili = sF, oncekiMuh = oM, girisMuh = girM, sonMuh = sM, satis });
                    sF = oF; sM = oM;
                }
                // dönem öncesi girişler: stoktaki partilerin geliş kayıtları + örnek geçmiş alımlar (aylık zincirin dışında)
                Func<decimal, decimal> eskiMuh = gf => !xYogunTip || ug.Next(100) < 35 ? gf : Y(gf * (decimal)(0.55 + ug.NextDouble() * 0.4));
                foreach (var q in zorunlu.Where(w => w.T < ilkAyBasi).OrderBy(w => w.T)) GirisEkle(q.T, q.Lot, q.M, eskiMuh(q.M), false);
                int eskiAdet = k.Tip == "Makine / Ekipman" ? 1 : ug.Next(2, 5);
                for (int ej = 0; ej < eskiAdet; ej++)
                {
                    var et = ilkAyBasi.AddDays(-ug.Next(1, 300)); decimal ef = Y(olcekK * (decimal)(0.12 + ug.NextDouble() * 0.45)); if (ef <= 0) ef = tamBirim ? 1 : 0.5m;
                    GirisEkle(et, k.Skt ? $"L{et:yyMM}-{ug.Next(100, 999)}" : "", ef, eskiMuh(ef), false);
                }
            }
            string Depo(string tip, string sirket, out string kod)
            {
                bool uretim = sirket == "Uras Kimya" || sirket == "Avrupa Paper" || sirket == "Alv Kimya" || sirket == "Daf Kimya";
                switch (tip)
                {
                    case "Hammadde": case "Yarı Mamul": kod = "01"; return uretim ? "Hammadde Deposu" : "Ana Depo";
                    case "Mamul": case "Ticari Mal": case "Numune": kod = "02"; return "Mamul Deposu";
                    case "Ambalaj Malzemesi": kod = "03"; return "Ambalaj Deposu";
                    case "Hurda / Fire": kod = "05"; return "Hurda Sahası";
                    default: kod = "04"; return "Sarf ve Genel Depo";
                }
            }
            foreach (var k in Katalog())
                foreach (var sirket in k.Sirketler)
                {
                    var rnd = new Random(Tohum(k.Kod + "|" + sirket + "|" + k.Tip)); string depoAdi = Depo(k.Tip, sirket, out var depoKod);
                    bool ana = sirket == k.Sirketler[0]; decimal olcek = k.Olcek * (ana ? 1m : 0.35m);
                    int lot = k.Skt && (k.Tip == "Hammadde" || k.Tip == "Mamul") && rnd.Next(3) == 0 ? 2 : 1; decimal toplamKullanilabilir = 0, toplamFiili = 0, toplamR = 0; var urnd = new Random(Tohum(k.Kod + "|" + sirket + "|" + k.Tip + "|uyum")); bool tamBirim = k.Birim != "kg" && k.Birim != "lt";
                    var zorunluGiris = new List<(DateTime T, string Lot, decimal M)>(); DateTime sayimEnErken = bugun; var grnd = new Random(Tohum(k.Kod + "|" + sirket + "|" + k.Tip + "|parti"));
                    for (int li = 0; li < lot; li++)
                    {
                        decimal Y(decimal v) => tamBirim ? Math.Round(v, 0) : Math.Round(v, 1);
                        decimal r = Y(olcek * (decimal)(0.25 + rnd.NextDouble() * 1.1) / lot);
                        // MUHASEBE STOKU (R) ve FİİLİ STOK AYRI ŞEYLERDİR (kullanıcı, 14 Kas 2026: "muhasebe stoğu ve fiili stok tamamen farklı; muhasebe formülü ve gerçek formül tamamen farklı").
                        // Üretim / satış stoklarında (hammadde, yarı mamul, mamul, ambalaj, ticari mal) fiili miktar muhasebe miktarından hemen her satırda farklıdır; X = fiili − muhasebe.
                        int z = rnd.Next(100); bool xYogun = k.Tip == "Hammadde" || k.Tip == "Yarı Mamul" || k.Tip == "Mamul" || k.Tip == "Ambalaj Malzemesi" || k.Tip == "Ticari Mal";
                        decimal x = z < (xYogun ? 62 : 8) ? Y(r * (decimal)(0.04 + rnd.NextDouble() * (xYogun ? 0.40 : 0.22))) : z < (xYogun ? 97 : 20) ? -Y(r * (decimal)(0.02 + rnd.NextDouble() * (xYogun ? 0.28 : 0.10))) : 0;
                        if (z == 99) { r = -Y(olcek * 0.01m); x = 0; }   // negatif stok senaryosu (kayıt hatası)
                        decimal fiili = r + x;
                        decimal giris = rnd.Next(100) < 18 ? Y(Math.Abs(r) * (decimal)(0.02 + rnd.NextDouble() * 0.08)) : 0, cikis = rnd.Next(100) < 24 ? Y(Math.Abs(r) * (decimal)(0.02 + rnd.NextDouble() * 0.1)) : 0;
                        decimal sayilan = fiili - giris + cikis;
                        decimal fiiliMaliyet = rnd.Next(100) < 22 ? Math.Round(k.Maliyet * (decimal)(0.95 + rnd.NextDouble() * 0.12), 2) : k.Maliyet;
                        int kz = rnd.Next(100); string konum = kz < 88 ? "Depoda" : kz < 94 ? "Dış Depoda" : kz < 98 ? "Transferde" : "Konsinyede", mulkiyet = rnd.Next(100) < 97 ? "Gruba Ait" : "Üçüncü Kişiye Ait";
                        int hz = rnd.Next(100); var sonHareket = bugun.AddDays(-(hz < 62 ? rnd.Next(0, 45) : hz < 82 ? rnd.Next(45, 110) : hz < 93 ? rnd.Next(110, 200) : rnd.Next(200, 420)));
                        DateTime? skt = k.Skt ? bugun.AddDays(rnd.Next(100) < 7 ? -rnd.Next(5, 90) : rnd.Next(100) < 16 ? rnd.Next(5, 88) : rnd.Next(120, 620)) : (DateTime?)null;
                        decimal bloke = fiili > 0 && rnd.Next(100) < 7 ? Y(fiili * (decimal)(0.05 + rnd.NextDouble() * 0.3)) : 0, rezerve = fiili > 0 && rnd.Next(100) < 12 ? Y((fiili - bloke) * (decimal)(0.05 + rnd.NextDouble() * 0.25)) : 0;
                        var sayimTarihi = bugun.AddDays(-rnd.Next(1, 20)); bool farkli = x != 0 || fiiliMaliyet != k.Maliyet || r < 0;
                        int ni = rnd.Next(nedenler.Length); int oz = rnd.Next(100); string onay = !farkli ? "" : oz < 40 ? "Onaylandı" : oz < 75 ? "İncelemede" : "Bekliyor";
                        // parti / lot: geliş tarihi parti kodundadır (rastgele sırası eskisiyle aynı). Partinin gelişi ve sayım sonrası giriş Ürün Geçmişi'nde giriş belgesi olarak görünür.
                        DateTime lotTarih = default; string lotKod = "";
                        if (k.Skt || lot > 1) { lotTarih = bugun.AddDays(-rnd.Next(20, 300)); lotKod = $"L{lotTarih:yyMM}-{rnd.Next(100, 999)}"; }
                        if (lotKod != "" && fiili > 0) zorunluGiris.Add((lotTarih, lotKod, Y(fiili * (decimal)(1.05 + grnd.NextDouble() * 0.5))));
                        if (sayimTarihi < sayimEnErken) sayimEnErken = sayimTarihi;
                        if (giris > 0) { int sonraGun = (bugun.AddDays(-1) - sayimTarihi).Days; zorunluGiris.Add((sonraGun <= 0 ? sayimTarihi : sayimTarihi.AddDays(1 + grnd.Next(sonraGun)), lotKod, giris)); }
                        satirlar.Add(new
                        {
                            no = ++no, sirket, depoKod, depo = depoAdi, mulkiyet, konum, kod = k.Kod, ad = k.Ad, tip = k.Tip, anaGrup = k.AnaGrup, grup = k.Grup, sinif = k.Sinif, urunGrubu = k.UrunGrubu, seri = k.Seri, birim = k.Birim,
                            lot = lotKod, sktTakibi = k.Skt, skt = Gun(skt), sonHareket = Gun(sonHareket),
                            rMiktar = r, rMaliyet = k.Maliyet, sayimTarihi = Gun(sayimTarihi), sayilan, giris, cikis, fMaliyet = fiiliMaliyet, bloke, rezerve,
                            sayimBelge = $"SYM-{sayimTarihi:yyMM}-{rnd.Next(1000, 9999)}", farkNedeni = x != 0 || r < 0 ? nedenler[r < 0 ? 4 : x > 0 ? (ni % 2 == 0 ? 1 : 3) : (ni % 2 == 0 ? 0 : 2)] : "", sorumlu = farkli ? sorumlular[rnd.Next(sorumlular.Length)] : "",
                            aksiyon = x != 0 || r < 0 ? aksiyonlar[r < 0 ? 4 : x > 0 ? 1 : 0] : "", hedefTarih = farkli && onay != "Onaylandı" ? Gun(bugun.AddDays(rnd.Next(-6, 25))) : "", onay, duzeltmeBelge = onay == "Onaylandı" ? $"DZL-{bugun:yy}-{rnd.Next(100, 999)}" : "", tedarikci = k.Tedarikci
                        });
                        bool kullanilabilirMi = (konum == "Depoda" || konum == "Dış Depoda") && mulkiyet == "Gruba Ait";
                        toplamFiili += fiili; toplamR += r; if (kullanilabilirMi) toplamKullanilabilir += Math.Max(0, fiili - bloke - rezerve);
                    }
                    // tüketim / yeterlilik planı: şirket + depo + ürün başına tek satır (makine, numune, hurda hariç)
                    if (k.Tip == "Makine / Ekipman" || k.Tip == "Numune" || k.Tip == "Hurda / Fire") { Uyum(k, sirket, depoKod, tamBirim, toplamFiili, toplamR, 0, 0, null, null, urnd, olcek, zorunluGiris, sayimEnErken); continue; }
                    {
                        int tz = rnd.Next(100); decimal yeterlilikGun = tz < 6 ? 0 : tz < 20 ? rnd.Next(4, 25) : tz < 45 ? rnd.Next(25, 80) : tz < 85 ? rnd.Next(80, 220) : rnd.Next(220, 600);
                        decimal baz = Math.Max(toplamFiili, olcek * 0.3m), gunluk = yeterlilikGun == 0 ? 0 : baz / yeterlilikGun, tuketim = Math.Round(gunluk * TuketimDonemGunu, tamBirim ? 0 : 1);
                        // tüketim de iki ayrı formülle oluşur: muhasebe çıkışı (R formülü) ≠ fiili tüketim (gerçek formül); fark (X) artı ya da eksi olabilir
                        bool xTip = k.Tip == "Hammadde" || k.Tip == "Yarı Mamul" || k.Tip == "Ambalaj Malzemesi" || k.Tip == "Mamul" || k.Tip == "Ticari Mal"; decimal xPay = 0;
                        if (xTip) { double xd = rnd.Next(100) < 45 ? rnd.NextDouble() : urnd.NextDouble(); xPay = (decimal)(-0.20 + xd * 0.60); if (Math.Abs(xPay) < 0.03m) xPay = 0.06m; }
                        decimal tX = Math.Round(tuketim * xPay, tamBirim ? 0 : 1), tR = tuketim - tX; int tedarik = k.Tip == "Hammadde" ? rnd.Next(14, 60) : k.Tip == "Yedek Parça" ? rnd.Next(20, 75) : rnd.Next(3, 25);
                        bool acik = tuketim > 0 && rnd.Next(100) < 34, yolda = tuketim > 0 && rnd.Next(100) < 16; decimal? recete = null, receteR = null;   // recete = gerçek reçete (fiili formül), receteR = muhasebe reçetesi (R formülü)
                        Func<decimal?> receteHesap = () => { double j = rnd.NextDouble(); return urnd.Next(100) < 60 ? tuketim : Math.Round(tuketim * (decimal)(0.94 + j * 0.1), tamBirim ? 0 : 1); };
                        planlar.Add(new
                        {
                            no = ++planNo, sirket, depoKod, depo = depoAdi, kod = k.Kod, ad = k.Ad, tip = k.Tip, anaGrup = k.AnaGrup, grup = k.Grup, sinif = k.Sinif, urunGrubu = k.UrunGrubu, seri = k.Seri, birim = k.Birim,
                            tuketimR = tR, tuketimX = tX, recete = (recete = k.Tip == "Hammadde" || k.Tip == "Yarı Mamul" || k.Tip == "Ambalaj Malzemesi" ? receteHesap() : (decimal?)null),
                            receteR = (receteR = recete.HasValue ? (urnd.Next(100) < 60 ? tR : Math.Round(tR * (decimal)(0.95 + urnd.NextDouble() * 0.1), tamBirim ? 0 : 1)) : (decimal?)null),
                            guvenlik = Math.Round(gunluk * rnd.Next(7, 21), tamBirim ? 0 : 1), tedarikGun = tedarik,
                            acikAlim = acik ? Math.Round(gunluk * rnd.Next(20, 70), tamBirim ? 0 : 1) : 0m, acikTarih = acik ? Gun(bugun.AddDays(rnd.Next(-8, 45))) : "",
                            yoldaki = yolda ? Math.Round(gunluk * rnd.Next(10, 40), tamBirim ? 0 : 1) : 0m, yoldakiTarih = yolda ? Gun(bugun.AddDays(rnd.Next(1, 20))) : "",
                            tedarikci = k.Tedarikci, sorumlu = sorumlular[rnd.Next(sorumlular.Length)], maliyet = k.Maliyet
                        });
                        Uyum(k, sirket, depoKod, tamBirim, toplamFiili, toplamR, tR, tX, recete, receteR, urnd, olcek, zorunluGiris, sayimEnErken);
                    }
                }
            return new
            {
                bugun = Gun(bugun), ornek = true, tipler = StokTipleri,
                ayar = new { hedefGun = HedefStokGunu, sktGun = SktUyariGunu, yavasGun = YavasGun, atilGun = AtilGun, donemGun = TuketimDonemGunu, donemBas = Gun(bugun.AddDays(-TuketimDonemGunu)), donemBit = Gun(bugun.AddDays(-1)),
                    kontrolAyi = dilimler[Math.Max(0, dilimler.Count - 2)].Ay, tolerans = 2m, riskYuzdeYuksek = 10m, riskYuzdeOrta = 3m, riskBedelYuksek = 50000m, riskBedelOrta = 10000m },
                satirlar, planlar, belgeler, hareketler, girisler
            };
        }
    }
}
