// ============================================================
// PLANLI DÜZENLİ ÖDEMELER — Konsolide Finansal Takip Paneli › Düzenli Ödemeler sekmesi (Views/Rapor123/Index.cshtml, js/takip-paneli.js, Rapor123Controller Duzenli*)
// Kaynak: "AYLIK ÖDEME TAKVİM LİSTESİ 2026" (Serkan Genç — düzenli ödemeler listesi): tedarikçi, ürün / hizmet, şirket, ödeme şekli, ortalama aylık tutar.
// Her ay otomatik: aktif her kayıt için o ayın ödeme gününe planlanmış bir Kasa Nakit Akışı satırı açılır (Data/NakitAkisOrnek.cs › Ekle; kayıt tipi
// "Düzenli Ödeme", belge no "DÖ-xxx · AAAA-AA", talep eden Serkan Genç). Aynı kayıt aynı aya iki kez akmaz. Geçmiş aylar tohumda ödenmiş olarak üretilir;
// bu ayın günü geçen ödemeleri ödenmiş, kalanlar planlı; gelecek ay "Gelecek Ayı Planla" ile ya da ay başında otomatik akar (panel her açılışta eksikleri tamamlar).
// Kilit sırası: NakitAkis → Duzenli (tohum) ve Duzenli kilidi tutulurken NakitAkisOrnek çağrılmaz (Akit: adaylar kilitte toplanır, satırlar kilit dışında açılır).
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    public class DuzenliAkis { public string Ay { get; set; } public int DocEntry { get; set; } public string DbName { get; set; } public DateTime Zaman { get; set; } public string Kullanici { get; set; } public decimal Tutar { get; set; } public string Yontem { get; set; } public DateTime PlanlananTarih { get; set; } }
    /// <summary>
    /// Bir ayın düzenli ödemesi için planlanan portföy müşteri çeki / senedi (Data/CekAkibetOrnek.cs). Kullanım: "Ciro" = belge tedarikçiye verilir (ödeme şekli çek; Kasa Nakit Akışı satırının belgesi olur),
    /// "Tahsil" = belge vadesinde bankaya tahsile verilir, ödeme nakit / havale yapılır (örn. maaş: o tarihe uygun vadeli çek planlanır, çek tahsil edilince bankadan maaş ödenir).
    /// </summary>
    public class DuzenliCekPlani { public string Ay { get; set; } public int CekDocEntry { get; set; } public string CekNo { get; set; } public string Belge { get; set; } = "Çek"; public string CekSirket { get; set; } public decimal Tutar { get; set; } public DateTime Vade { get; set; } public string Kullanim { get; set; } = "Tahsil"; public DateTime Zaman { get; set; } public string Kullanici { get; set; } = ""; }
    public class DuzenliOdeme
    {
        public int Id { get; set; } public string No { get; set; }
        public string Tedarikci { get; set; } public string UrunHizmet { get; set; } public string Sirket { get; set; } public string DbName { get; set; }
        public string OdemeSekli { get; set; } = "Çek";      // Nakit | Çek | Nakit / Çek | Havale / EFT (listedeki ödeme şekli)
        public string Kategori { get; set; } = "Hizmet";     // Enerji | Hizmet | Malzeme | Kira | Abonelik | Diğer
        public int Gun { get; set; } = 10;                   // ayın ödeme günü (1–28)
        public decimal AylikTutar { get; set; } public string PB { get; set; } = "TRY";
        public string Sorumlu { get; set; } = DuzenliOdemeOrnek.Sorumlu; public string Durum { get; set; } = "Aktif";   // Aktif | Pasif
        public string Not { get; set; } = ""; public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
        public List<DuzenliAkis> Akislar { get; set; } = new List<DuzenliAkis>();
        public List<TalimatGunluk> Gunluk { get; set; } = new List<TalimatGunluk>();
        /// <summary>Bütçe Raporu kalemi (Data/ButceOrnek.cs Kalemler kodu; kategori ve ürün / hizmete göre otomatik önerilir, ekranda değiştirilir) — düzenli ödeme bütçeye otomatik gelir.</summary>
        public string ButceKalem { get; set; } = "";
        /// <summary>Ay bazında planlanan portföy çekleri / senetleri (hangi aya kadar, ne kadarlık çek planlandı).</summary>
        public List<DuzenliCekPlani> CekPlanlari { get; set; } = new List<DuzenliCekPlani>();
    }

    public static class DuzenliOdemeOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly List<DuzenliOdeme> _liste = new List<DuzenliOdeme>();
        static int _sonId; static bool _tohumlandi;
        public const string Sorumlu = "Serkan Genç";
        public static readonly string[] OdemeSekilleri = { "Nakit", "Çek", "Nakit / Çek", "Havale / EFT" };
        public static readonly string[] Kategoriler = { "Enerji", "Hizmet", "Malzeme", "Kira", "Abonelik", "Diğer" };
        /// <summary>Çek planı kullanımı: Tahsil (bankaya tahsile verilir, ödeme nakit / havale) | Ciro (tedarikçiye verilir).</summary>
        public static readonly string[] Kullanimlar = { "Tahsil", "Ciro" };
        /// <summary>Çek planı ufku: bugünden kaç ay ileriye çek planlanabilir.</summary>
        public const int CekPlanUfku = 12;
        public static string KullanimOner(DuzenliOdeme d) => Yontem(d.OdemeSekli) == "Çek" ? "Ciro" : "Tahsil";
        public static DateTime OdemeTarihi(string ay, int gun) => AyCoz(ay, out var b) ? Tarih(b, gun) : DateTime.Today;
        public static bool AyGecerli(string ay) => AyCoz(ay, out _);
        /// <summary>Bütçe kalemi önerisi: ürün / hizmet metni ve kategoriye göre (Data/ButceOrnek.cs kodları).</summary>
        public static string KalemOner(string kategori, string urun)
        {
            string u = (urun ?? "").ToLower(Tr);
            if (u.Contains("elektrik")) return "GG-01"; if (u.Contains("doğalgaz") || u.Contains("buhar")) return "GG-02";
            if (u.Contains("damacana") || u.Contains("kırtasiye") || u.Contains("ofis")) return "GG-10";
            if (u.Contains("motorin") || u.Contains("yakıt") || u.Contains("akaryakıt") || u.Contains("nakliye")) return "TZ-09";
            if (u.Contains("catering") || u.Contains("yemek") || u.Contains("personel servis")) return "SG-04";
            if (u.Contains("güvenlik") || u.Contains("temizl")) return "GG-12";
            if (u.Contains("asansör") || u.Contains("bakım") || u.Contains("onarım")) return "GG-04";
            if (u.Contains("isg") || u.Contains("doktor") || u.Contains("danışman") || u.Contains("çevre") || u.Contains("it ")) return "GG-05";
            if (u.Contains("bulut") || u.Contains("lisans") || u.Contains("yazılım") || u.Contains("abonelik")) return "SG-11";
            if (u.Contains("forklift") || u.Contains("araç")) return "SG-08"; if (u.Contains("kira")) return "SG-06";
            if (u.Contains("ambalaj") || u.Contains("palet") || u.Contains("kafes") || u.Contains("ibc")) return "TZ-08";
            if (u.Contains("filtre") || u.Contains("mdf") || u.Contains("yedek")) return "TZ-12";
            if (u.Contains("osb") || u.Contains("atık") || u.Contains("aidat")) return "SG-13";
            if (u.Contains("telefon") || u.Contains("internet")) return "GG-11";
            switch (kategori) { case "Enerji": return "GG-01"; case "Kira": return "SG-06"; case "Abonelik": return "SG-11"; case "Malzeme": return "TZ-08"; default: return "GG-12"; }
        }
        public static string AyKodu(DateTime t) => t.ToString("yyyy-MM");
        public static string BuAy => AyKodu(DateTime.Today);
        public static string SonrakiAy => AyKodu(DateTime.Today.AddMonths(1));
        public static string AyAdi(string ay) => AyCoz(ay, out var t) ? t.ToString("MMMM yyyy", Tr) : ay;
        static bool AyCoz(string ay, out DateTime bas) => DateTime.TryParseExact((ay ?? "") + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out bas);
        static DateTime Tarih(DateTime ayBas, int gun) => ayBas.AddDays(Math.Max(1, Math.Min(28, gun)) - 1);
        /// <summary>Listedeki ödeme şeklinin nakit akış yöntemi karşılığı ("Nakit / Çek" → Çek, notta belirtilir).</summary>
        public static string Yontem(string odemeSekli) => odemeSekli == "Nakit" ? "Nakit" : odemeSekli == "Havale / EFT" ? "Havale / EFT" : "Çek";
        static void Log(DuzenliOdeme d, string islem, string aciklama, string kullanici) => d.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = islem, Aciklama = aciklama });

        /// <summary>Tohum: "AYLIK ÖDEME TAKVİM LİSTESİ 2026" satırları; geçmiş iki ay ödenmiş, bu ay akmış (günü geçenler ödenmiş). NakitAkisOrnek.Tohumla sonunda çağrılır.</summary>
        public static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var b = DateTime.Today;
                void S(string tedarikci, string urun, string sirket, string sekil, decimal tutar, string kategori, int gun)
                { var d = new DuzenliOdeme { Id = ++_sonId, No = $"DÖ-{_sonId:000}", Tedarikci = tedarikci, UrunHizmet = urun, Sirket = sirket, DbName = CekAkibetOrnek.DbName(sirket), OdemeSekli = sekil, Kategori = kategori, Gun = gun, AylikTutar = tutar, Olusturma = b.AddDays(-200), Olusturan = Sorumlu, ButceKalem = KalemOner(kategori, urun) }; d.Gunluk.Add(new TalimatGunluk { Tarih = d.Olusturma.AddHours(10), Kullanici = Sorumlu, Islem = "Kayıt", Aciklama = "Aylık ödeme takvim listesinden alındı" }); _liste.Add(d); }
                S("CK Boğaziçi", "Elektrik Ödemesi", "Uras Kimya", "Nakit", 1_200_000m, "Enerji", 20); S("CK Boğaziçi", "Elektrik Ödemesi", "Alv Kimya", "Nakit", 900_000m, "Enerji", 20);
                S("YNS Asansör", "Asansör Bakımı", "Uras Kimya", "Nakit", 4_500m, "Hizmet", 10); S("Opera Catering", "Catering Hizmeti", "Uras Kimya", "Çek", 450_000m, "Hizmet", 10);
                S("Elze Kurumsal", "Kırtasiye Malzemesi", "Uras Kimya", "Çek", 35_000m, "Malzeme", 15); S("Han Grup", "Damacana Su", "Alv Kimya", "Nakit", 4_500m, "Malzeme", 15); S("Karahan Su", "Damacana Su", "Uras Kimya", "Nakit", 22_000m, "Malzeme", 15);
                S("Doğuş Palet", "Palet Alımı", "Uras Kimya", "Çek", 100_000m, "Malzeme", 25); S("Plastiksan", "Ambalaj Alımı", "Uras Kimya", "Çek", 230_000m, "Malzeme", 25);
                S("Güveniş İş Sağlığı", "İSG ve Doktor Hizmeti", "Uras Kimya", "Çek", 22_000m, "Hizmet", 10); S("Güveniş İş Sağlığı", "İSG ve Doktor Hizmeti", "Alv Kimya", "Çek", 47_000m, "Hizmet", 10); S("Güveniş İş Sağlığı", "İSG ve Doktor Hizmeti", "Selvi", "Çek", 55_000m, "Hizmet", 10);
                S("Proses", "Filtre", "Alv Kimya", "Çek", 500_000m, "Malzeme", 25); S("Yıldıray Toprak", "Forklift Kira Bedeli", "Selvi", "Nakit", 48_000m, "Kira", 5); S("Arı Group", "Personel Servisi", "Selvi", "Nakit", 80_000m, "Hizmet", 5);
                S("Kuzey Personel", "Güvenlik Hizmeti", "Alv Kimya", "Çek", 185_000m, "Hizmet", 10); S("A3 Catering", "Catering Hizmeti", "Alv Kimya", "Çek", 150_000m, "Hizmet", 10); S("Hakel Ambalaj", "Kafes", "Selvi", "Nakit", 50_000m, "Malzeme", 15);
                S("Çınar Orman", "MDF Alımı", "Uras Kimya", "Çek", 90_000m, "Malzeme", 25); S("Globus", "Güvenlik Hizmeti", "Uras Kimya", "Çek", 86_000m, "Hizmet", 10); S("Uzun Otomotiv", "Motorin Alımı", "Alv Kimya", "Nakit", 250_000m, "Enerji", 20);
                S("Bulutpark", "Bulut Sistemi", "Selvi", "Nakit / Çek", 140_000m, "Abonelik", 1); S("Bulutpark", "Bulut Sistemi", "Uras Holding", "Nakit / Çek", 30_000m, "Abonelik", 1);
                S("Öztürk Plastik", "Ambalaj Alımı", "Uras Kimya", "Çek", 1_200_000m, "Malzeme", 25); S("Öztürk Plastik", "Ambalaj Alımı", "Alv Kimya", "Çek", 300_000m, "Malzeme", 25); S("Makrosel", "IT Danışmanlığı", "Uras Kimya", "Çek", 85_000m, "Hizmet", 10);
                S("Velimeşe OSB", "OSB Katılım Ücreti ve Atık Gönderim", "Alv Kimya", "Çek", 35_000m, "Hizmet", 15); S("Nora Çevre", "Çevre Danışmanlığı", "Uras Kimya", "Çek", 13_609m, "Hizmet", 10); S("Nora Çevre", "Çevre Danışmanlığı", "Alv Kimya", "Çek", 13_609m, "Hizmet", 10);
                S("Balkan Geri Dönüşüm", "IBC Temizleme", "Uras Kimya", "Çek", 75_000m, "Hizmet", 15); S("Çorlu Deri OSB", "Atık Gönderimi", "Alv Kimya", "Çek", 100_000m, "Hizmet", 15);
            }
            var bugun = DateTime.Today;
            foreach (int i in new[] { 2, 1 }) Akit(AyKodu(bugun.AddMonths(-i)), "Sistem", null, null, odenmisSay: true, panelGunlugu: false);
            Akit(BuAy, "Sistem", null, null, odenmisSay: false, panelGunlugu: false);
            CekPlaniTohumla();
        }
        /// <summary>Örnek çek planları: birkaç kayda gelecek iki ay için portföyden vadesi ödeme gününe uygun müşteri çekleri planlanır (nakit ödemeler bankaya tahsil, çek ödemeleri ciro).</summary>
        private static void CekPlaniTohumla()
        {
            try
            {
                var hedefler = Listele().Where(d => d.Durum == "Aktif" && (d.UrunHizmet.Contains("Elektrik") || d.Tedarikci == "Opera Catering" || (d.Tedarikci == "Öztürk Plastik" && d.Sirket == "Uras Kimya") || d.Tedarikci == "Kuzey Personel")).Take(5).ToList();
                foreach (var d in hedefler)
                    foreach (var ay in new[] { SonrakiAy, AyKodu(DateTime.Today.AddMonths(2)) })
                    {
                        var tarih = OdemeTarihi(ay, d.Gun); decimal kalan = d.AylikTutar; var sec = new List<int>();
                        // ihtiyaca en yakın: önce kalanı aşmayan en büyük çekler (vadesi ödeme gününe yakın), sonra kalanı kapatan en küçük çek (ihtiyacın 1,5 katına kadar)
                        var adaylar = CekAkibetOrnek.Secilebilirler(d.Sirket, "Çek").Where(k => k.Vade.Date <= tarih && k.Vade.Date >= tarih.AddDays(-45)).ToList();
                        foreach (var k in adaylar.Where(k => k.Tutar <= kalan * 1.1m).OrderByDescending(k => k.Tutar)) { if (kalan <= 0) break; sec.Add(k.DocEntry); kalan -= k.Tutar; }
                        if (kalan > 0) { var kapat = adaylar.Where(k => !sec.Contains(k.DocEntry) && k.Tutar >= kalan && k.Tutar <= kalan * 1.5m).OrderBy(k => k.Tutar).FirstOrDefault(); if (kapat != null) { sec.Add(kapat.DocEntry); kalan -= kapat.Tutar; } }
                        if (sec.Count > 0) CekPlanla(d.Id, ay, sec, null, "Sistem");
                    }
            }
            catch { /* örnek çek planı isteğe bağlı */ }
        }

        public static List<DuzenliOdeme> Listele(string dbName = null) { Tohumla(); lock (Kilit) return _liste.Where(d => string.IsNullOrEmpty(dbName) || d.DbName == dbName).OrderBy(d => d.Sirket, StringComparer.Create(Tr, false)).ThenBy(d => d.Gun).ThenBy(d => d.Id).ToList(); }
        public static DuzenliOdeme Bul(int id) { Tohumla(); lock (Kilit) return _liste.FirstOrDefault(d => d.Id == id); }

        /// <summary>Bir ayın düzenli ödemelerini Kasa Nakit Akışı'na akıtır (aktif kayıtlar; o aya akmış olanlar atlanır). odenmisSay: geçmiş ay tohumu — satır ödendi. panelGunlugu: panel günlüğüne de yaz (tohumda kapalı: kilit sırası).</summary>
        public static (bool ok, string mesaj, int eklenen, int atlanan) Akit(string ay, string kullanici, string dbName = null, int? id = null, bool odenmisSay = false, bool panelGunlugu = true, bool uzak = false)
        {
            Tohumla(); if (!AyCoz(ay, out var ayBas)) return (false, "Ay AAAA-AA biçiminde olmalı.", 0, 0);
            if (ayBas > DateTime.Today.AddMonths(uzak ? CekPlanUfku : 1)) return (false, uzak ? $"En fazla {CekPlanUfku} ay ileriye planlanabilir." : "En fazla gelecek ay planlanabilir.", 0, 0);
            List<DuzenliOdeme> adaylar; int atlanan;
            lock (Kilit)
            {
                var kapsam = _liste.Where(d => d.Durum == "Aktif" && (string.IsNullOrEmpty(dbName) || d.DbName == dbName) && (id == null || d.Id == id)).ToList();
                adaylar = kapsam.Where(d => !d.Akislar.Any(a => a.Ay == ay)).ToList(); atlanan = kapsam.Count - adaylar.Count;
            }
            var bugun = DateTime.Today; int eklenen = 0;
            foreach (var d in adaylar)
            {
                var tarih = Tarih(ayBas, d.Gun); string yontem = Yontem(d.OdemeSekli);
                var gelen = new NakitAkisViewModel
                {
                    U_Kurum = d.Tedarikci, U_UrunHizmet = $"{d.UrunHizmet} · {AyAdi(ay)}", U_OdemeYontemi = yontem, U_BagliBelgeTipi = "Diğer", U_KayitTipi = "Düzenli Ödeme", U_FaturaSiparisNo = $"{d.No} · {ay}",
                    U_PlanlananTarih = tarih, U_SonOdemeTarihi = tarih, BelgeTarihi = tarih.AddDays(-5), U_Tutar = d.AylikTutar, U_ParaBirimi = "TRY", U_Kur = 1, U_TutarTL = d.AylikTutar, U_FaturaTutari = d.AylikTutar, U_IslemTipi = "X",
                    U_TalepEden = d.Sorumlu, U_Satinalmaci = "", U_FaturaSahibi = d.Sirket, U_OdeyenSirket = d.Sirket, U_Durum = "Planlandı", U_TeslimAlan = yontem == "Nakit" ? d.Tedarikci : null,
                    U_Notlar = $"Planlı düzenli ödeme {d.No} ({d.Kategori}) · her ay otomatik nakit akışa akar · ort. aylık {d.AylikTutar.ToString("N0", Tr)} ₺ · sorumlu {d.Sorumlu}" + (d.OdemeSekli == "Nakit / Çek" ? " · nakit ya da çek" : "") + (string.IsNullOrEmpty(d.Not) ? "" : " · " + d.Not)
                };
                var m = NakitAkisOrnek.Ekle(d.DbName, gelen, kullanici);
                // o ay için önceden planlanmış çekler satıra bağlanır: Ciro → satırın belgesi olur (Kasa Nakit Akışı çek satırı); Tahsil → bilgi bağı (belge bankaya tahsile verilir, ödeme nakit / havale)
                List<DuzenliCekPlani> planlar; lock (Kilit) planlar = d.CekPlanlari.Where(p => p.Ay == ay).ToList();
                foreach (var p in planlar)
                {
                    try
                    {
                        if (p.Kullanim == "Ciro") { var k = CekAkibetOrnek.Bul(p.CekDocEntry); if (k != null && k.IslemTuru == "Planlandı" && k.PlanDocEntry != m.DocEntry) CekAkibetOrnek.PlanKaldir(p.CekDocEntry, kullanici); NakitAkisOrnek.BelgePlanla(d.DbName, m.DocEntry, new[] { p.CekDocEntry }, kullanici); }
                        else CekAkibetOrnek.PlanKaynakBagla(p.CekDocEntry, m.DocEntry);
                    }
                    catch { /* çek bağlanamazsa satır yine açılır */ }
                }
                bool odendi = odenmisSay || (ay == BuAy && tarih < bugun);
                if (odendi) { NakitAkisOrnek.OdendiIsaretle(d.DbName, m.DocEntry, "Düzenli ödeme " + d.No, kullanici, true, tarih); if (yontem == "Nakit") NakitAkisOrnek.AlanKaydet(d.DbName, m.DocEntry, "U_MakbuzNo", $"KT-{tarih:yyMM}-{d.Id:000}", kullanici); }
                lock (Kilit) { d.Akislar.Add(new DuzenliAkis { Ay = ay, DocEntry = m.DocEntry, DbName = d.DbName, Zaman = DateTime.Now, Kullanici = kullanici ?? "", Tutar = d.AylikTutar, Yontem = yontem, PlanlananTarih = tarih }); Log(d, "Nakit Akışa Aktı", $"{AyAdi(ay)} · Kasa Nakit Akışı #{m.DocEntry} · {yontem} · {d.AylikTutar.ToString("N2", Tr)} ₺ · planlanan {tarih:dd.MM.yyyy}" + (odendi ? " · ödendi" : ""), kullanici); }
                if (panelGunlugu) FinekraOrnek.GunlukEkle(d.Sirket, d.No, "Düzenli Ödeme Aktı", $"{d.Tedarikci} · {d.UrunHizmet} · {AyAdi(ay)} · Kasa Nakit Akışı #{m.DocEntry} · {d.AylikTutar.ToString("N2", Tr)} ₺ · {yontem}", kullanici);
                eklenen++;
            }
            string mesaj = eklenen == 0 ? (atlanan > 0 ? $"{AyAdi(ay)} için {atlanan} düzenli ödeme zaten nakit akışta; yeni satır açılmadı." : "Akıtılacak aktif düzenli ödeme yok.") : $"{AyAdi(ay)}: {eklenen} düzenli ödeme Kasa Nakit Akışı'na aktı" + (atlanan > 0 ? $" ({atlanan} zaten akmıştı)" : "") + ".";
            return (true, mesaj, eklenen, atlanan);
        }
        /// <summary>Panel her açılışta: bu ay için akmamış aktif kayıtları tamamlar (ay başı otomatik işinin karşılığı).</summary>
        public static (int eklenen, string mesaj) OtomatikAkit(string kullanici) { var r = Akit(BuAy, kullanici, null, null, false, true); return (r.eklenen, r.eklenen > 0 ? r.mesaj : ""); }

        public static (bool ok, string mesaj, DuzenliOdeme d) Kaydet(int id, string tedarikci, string urunHizmet, string sirket, string odemeSekli, string kategori, int gun, decimal aylikTutar, string not, string kullanici, string butceKalem = null)
        {
            Tohumla();
            tedarikci = (tedarikci ?? "").Trim(); urunHizmet = (urunHizmet ?? "").Trim(); sirket = (sirket ?? "").Trim();
            if (tedarikci == "") return (false, "Tedarikçi zorunludur.", null); if (urunHizmet == "") return (false, "Ürün / hizmet zorunludur.", null);
            if (!CekAkibetOrnek.Sirketler.Any(s => s.Ad == sirket)) return (false, "Şirket seçilmelidir.", null);
            if (!OdemeSekilleri.Contains(odemeSekli ?? "")) return (false, "Ödeme şekli geçersiz.", null);
            if (!Kategoriler.Contains(kategori ?? "")) kategori = "Diğer";
            if (gun < 1 || gun > 28) return (false, "Ödeme günü 1–28 arasında olmalıdır.", null); if (aylikTutar <= 0) return (false, "Ortalama aylık tutar sıfırdan büyük olmalıdır.", null);
            lock (Kilit)
            {
                var d = id > 0 ? _liste.FirstOrDefault(x => x.Id == id) : null;
                if (id > 0 && d == null) return (false, "Kayıt bulunamadı.", null);
                if (d == null)
                {
                    if (_liste.Any(x => x.Durum == "Aktif" && string.Equals(x.Tedarikci, tedarikci, StringComparison.OrdinalIgnoreCase) && string.Equals(x.UrunHizmet, urunHizmet, StringComparison.OrdinalIgnoreCase) && x.Sirket == sirket)) return (false, $"{tedarikci} · {urunHizmet} · {sirket} zaten listede.", null);
                    d = new DuzenliOdeme { Id = ++_sonId, No = $"DÖ-{_sonId:000}", Olusturma = DateTime.Today, Olusturan = kullanici ?? "" }; _liste.Add(d);
                    Log(d, "Kayıt", $"{tedarikci} · {urunHizmet} · {sirket} · {odemeSekli} · {aylikTutar.ToString("N2", Tr)} ₺ · gün {gun}", kullanici);
                }
                else
                {
                    var degisen = new List<string>(); if (d.AylikTutar != aylikTutar) degisen.Add($"tutar {d.AylikTutar.ToString("N0", Tr)} → {aylikTutar.ToString("N0", Tr)}"); if (d.Gun != gun) degisen.Add($"gün {d.Gun} → {gun}"); if (d.OdemeSekli != odemeSekli) degisen.Add($"ödeme şekli {d.OdemeSekli} → {odemeSekli}"); if (d.Sirket != sirket) degisen.Add($"şirket {d.Sirket} → {sirket}"); if (d.Tedarikci != tedarikci || d.UrunHizmet != urunHizmet) degisen.Add("ad / hizmet"); if (!string.IsNullOrEmpty(butceKalem) && butceKalem != d.ButceKalem) degisen.Add($"bütçe kalemi {d.ButceKalem} → {butceKalem}");
                    Log(d, "Güncelleme", degisen.Count == 0 ? "Değişiklik yok" : string.Join(" · ", degisen) + " · bir sonraki akışta geçerli", kullanici);
                }
                d.Tedarikci = tedarikci; d.UrunHizmet = urunHizmet; d.Sirket = sirket; d.DbName = CekAkibetOrnek.DbName(sirket); d.OdemeSekli = odemeSekli; d.Kategori = kategori; d.Gun = gun; d.AylikTutar = aylikTutar; d.Not = (not ?? "").Trim();
                // bütçe kalemi: verildiyse (geçerli kod) o; yoksa kategori / ürüne göre otomatik öneri (düzenli ödeme bütçeye otomatik gelir)
                if (!string.IsNullOrEmpty(butceKalem) && ButceOrnek.KalemBul(butceKalem) != null) d.ButceKalem = butceKalem; else if (string.IsNullOrEmpty(d.ButceKalem) || ButceOrnek.KalemBul(d.ButceKalem) == null) d.ButceKalem = KalemOner(kategori, urunHizmet);
                return (true, (id > 0 ? $"{d.No} güncellendi" : $"{d.No} eklendi") + "; bu aya henüz akmadıysa \"Bu Ayı Akıt\" ile nakit akışa alınır.", d);
            }
        }
        public static (bool ok, string mesaj, DuzenliOdeme d) DurumDegistir(int id, string durum, string kullanici)
        {
            var d = Bul(id); if (d == null) return (false, "Kayıt bulunamadı.", null); durum = durum == "Pasif" ? "Pasif" : "Aktif";
            lock (Kilit) { if (d.Durum == durum) return (false, $"{d.No} zaten {durum.ToLower(Tr)}.", d); d.Durum = durum; Log(d, durum == "Pasif" ? "Pasife Alındı" : "Aktif Yapıldı", durum == "Pasif" ? "sonraki aylara akmaz (açılmış satırlar nakit akışta kalır)" : "sonraki aylara yeniden akar", kullanici); }
            return (true, $"{d.No} {d.Tedarikci} {(durum == "Pasif" ? "pasife alındı; sonraki aylara akmaz." : "aktif yapıldı; sonraki aylara akar.")}", d);
        }

        static object AkisJson(DuzenliAkis a)
        {
            if (a == null) return null; var m = NakitAkisOrnek.Bul(a.DbName, a.DocEntry);
            return new { a.Ay, a.DocEntry, durum = m?.U_Durum ?? "—", onay = m?.U_OnayDurumu, odenme = m?.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), planlanan = (m?.U_PlanlananTarih ?? a.PlanlananTarih).ToString("yyyy-MM-dd"), tutar = m?.U_TutarTL ?? a.Tutar, yontem = m?.U_OdemeYontemi ?? a.Yontem, makbuz = m?.U_MakbuzNo, yevmiye = m?.U_YevmiyeNo, zaman = a.Zaman.ToString("yyyy-MM-dd HH:mm"), a.Kullanici, link = $"/Rapor79?sirket={a.DbName}&sekme={(a.Yontem == "Nakit" ? "nakit" : a.Yontem == "Çek" ? "cek" : "havale")}" };
        }
        public static object Json(DuzenliOdeme d)
        {
            var bugun = DateTime.Today; string gecen = AyKodu(bugun.AddMonths(-1));
            List<DuzenliAkis> ak; List<TalimatGunluk> g; lock (Kilit) { ak = d.Akislar.ToList(); g = d.Gunluk.ToList(); }
            var cp = CekPlanHesapla(d); string kalem = string.IsNullOrEmpty(d.ButceKalem) ? KalemOner(d.Kategori, d.UrunHizmet) : d.ButceKalem;
            return new
            {
                d.Id, d.No, d.Tedarikci, d.UrunHizmet, d.Sirket, d.DbName, d.OdemeSekli, yontem = Yontem(d.OdemeSekli), d.Kategori, d.Gun, d.AylikTutar, d.PB, d.Sorumlu, d.Durum, d.Not, olusturma = d.Olusturma.ToString("yyyy-MM-dd"), d.Olusturan,
                butceKalem = kalem, butceKalemAd = ButceOrnek.KalemBul(kalem)?.Ad ?? "",
                gecenAy = AkisJson(ak.FirstOrDefault(a => a.Ay == gecen)), buAy = AkisJson(ak.FirstOrDefault(a => a.Ay == BuAy)), sonrakiAy = AkisJson(ak.FirstOrDefault(a => a.Ay == SonrakiAy)),
                akisSayisi = ak.Count, toplamAkan = ak.Sum(a => a.Tutar), akislar = ak.OrderByDescending(a => a.Ay).Select(AkisJson).ToList(),
                // çek planı: bugünden ufka kadar her ay — ihtiyaç, planlanan çekler, karşılama, nakit akış satırı; hangi aya kadar çek planlandı / tam karşılandı
                cekPlani = new { aylar = cp.Aylar, sonPlanliAy = cp.SonPlanli, sonPlanliAyAdi = cp.SonPlanli == null ? "" : AyAdi(cp.SonPlanli), tamSonAy = cp.TamSon, tamSonAyAdi = cp.TamSon == null ? "" : AyAdi(cp.TamSon), planliAy = cp.PlanliAy, toplamPlanlanan = cp.Toplam, planliCekAdet = cp.Adet, sonAkanAy = cp.SonAkan ?? "", sonAkanAyAdi = cp.SonAkan == null ? "" : AyAdi(cp.SonAkan), kullanimOneri = KullanimOner(d), ozet = cp.Ozet },
                gunluk = g.OrderByDescending(x => x.Tarih).Take(12).Select(x => new { tarih = x.Tarih.ToString("yyyy-MM-dd HH:mm"), x.Kullanici, x.Islem, x.Aciklama }).ToList()
            };
        }

        // ---------- ÇEK PLANI: ay bazında portföy çeki / senedi planlama (Ciro: tedarikçiye verilir · Tahsil: bankaya tahsile verilir, ödeme nakit / havale) ----------
        class CekPlanBilgi { public List<object> Aylar = new List<object>(); public string SonPlanli, TamSon, SonAkan; public int PlanliAy, Adet; public decimal Toplam; public string Ozet = ""; }
        static CekPlanBilgi CekPlanHesapla(DuzenliOdeme d)
        {
            var bugun = DateTime.Today; List<DuzenliCekPlani> cp; List<DuzenliAkis> ak; lock (Kilit) { cp = d.CekPlanlari.ToList(); ak = d.Akislar.ToList(); }
            var b = new CekPlanBilgi { Adet = cp.Count, Toplam = cp.Sum(p => p.Tutar), SonAkan = ak.Count == 0 ? null : ak.Max(a => a.Ay) }; bool zincir = true;
            for (int i = 0; i <= CekPlanUfku; i++)
            {
                var ayBas = new DateTime(bugun.Year, bugun.Month, 1).AddMonths(i); string ayK = AyKodu(ayBas); var tarih = Tarih(ayBas, d.Gun);
                var pl = cp.Where(p => p.Ay == ayK).OrderBy(p => p.Vade).ToList(); var a = ak.FirstOrDefault(x => x.Ay == ayK); var m = a == null ? null : NakitAkisOrnek.Bul(a.DbName, a.DocEntry);
                decimal planlanan = pl.Sum(p => p.Tutar); bool tam = d.AylikTutar > 0 && planlanan >= d.AylikTutar - 0.005m; bool odenmis = m != null && m.U_Durum == "ÖDENDİ";
                if (pl.Count > 0) { b.PlanliAy++; b.SonPlanli = ayK; }
                // "tam karşılanan son ay": bugünden itibaren kesintisiz — ödenmiş ay (nakit akış satırı ÖDENDİ) ya da çeklerle tam karşılanan ay zinciri bozmaz
                if (zincir && (tam || odenmis)) b.TamSon = ayK; else if (!(tam || odenmis)) zincir = false;
                b.Aylar.Add(new
                {
                    ay = ayK, ayAdi = AyAdi(ayK), tarih = tarih.ToString("yyyy-MM-dd"), ihtiyac = d.AylikTutar, planlanan, kapsama = d.AylikTutar > 0 ? Math.Round(planlanan / d.AylikTutar, 3) : 0m, tam,
                    akis = a == null ? null : new { a.DocEntry, durum = m?.U_Durum ?? "—", yontem = m?.U_OdemeYontemi ?? a.Yontem, tutar = m?.U_TutarTL ?? a.Tutar, link = $"/Rapor79?sirket={a.DbName}&sekme={(a.Yontem == "Nakit" ? "nakit" : a.Yontem == "Çek" ? "cek" : "havale")}&doc={a.DocEntry}" },
                    cekler = pl.Select(p => { var k = CekAkibetOrnek.Bul(p.CekDocEntry); return new { p.CekDocEntry, no = p.CekNo, belge = p.Belge, sirket = p.CekSirket, vade = p.Vade.ToString("yyyy-MM-dd"), tutar = p.Tutar, kullanim = p.Kullanim, durum = k?.IslemTuru ?? "—", musteri = k?.Musteri ?? "", banka = k?.Banka ?? "", uygun = p.Vade.Date <= tarih, gunFark = (tarih - p.Vade.Date).Days, zaman = p.Zaman.ToString("yyyy-MM-dd HH:mm"), p.Kullanici }; }).ToList()
                });
            }
            b.Ozet = b.PlanliAy == 0 ? "" : $"→ {AyAdi(b.SonPlanli)} · {b.PlanliAy} ay · {b.Toplam.ToString("N0", Tr)} ₺" + (b.TamSon != null ? $" · tam: {AyAdi(b.TamSon)}" : " · tam karşılanan ay yok");
            return b;
        }
        /// <summary>
        /// Bir ayın düzenli ödemesi için portföyden çek / senet planlar. Ciro: o ayın Kasa Nakit Akışı satırı yoksa şimdi açılır (en fazla CekPlanUfku ay ileri) ve belge satıra bağlanır (Çek Akıbeti: Planlandı → tedarikçi);
        /// Tahsil: belge Çek Akıbeti'nde "Planlandı (Tahsil → …)" olur, vadesinde bankaya tahsile verilir, ödeme nakit / havale yapılır. Aynı belge iki plana giremez (Secilebilir kuralı).
        /// </summary>
        public static (bool ok, string mesaj, DuzenliOdeme d) CekPlanla(int id, string ay, IEnumerable<int> cekler, string kullanim, string kullanici)
        {
            var d = Bul(id); if (d == null) return (false, "Kayıt bulunamadı.", null);
            if (!AyCoz(ay, out var ayBas)) return (false, "Ay AAAA-AA biçiminde olmalı.", d);
            if (string.CompareOrdinal(ay, BuAy) < 0) return (false, "Geçmiş aya çek planlanamaz.", d); if (ayBas > DateTime.Today.AddMonths(CekPlanUfku)) return (false, $"En fazla {CekPlanUfku} ay ileriye çek planlanabilir.", d);
            if (d.Durum != "Aktif") return (false, $"{d.No} pasif; önce aktif yapın.", d);
            kullanim = kullanim == "Ciro" ? "Ciro" : kullanim == "Tahsil" ? "Tahsil" : KullanimOner(d);
            var tarih = Tarih(ayBas, d.Gun); var liste = (cekler ?? Enumerable.Empty<int>()).Distinct().ToList(); if (liste.Count == 0) return (false, "Çek / senet seçin.", d);
            DuzenliAkis akis; List<int> mevcut; lock (Kilit) { akis = d.Akislar.FirstOrDefault(a => a.Ay == ay); mevcut = d.CekPlanlari.Where(p => p.Ay == ay).Select(p => p.CekDocEntry).ToList(); }
            if (kullanim == "Ciro" && akis == null)
            {
                var r0 = Akit(ay, kullanici, null, d.Id, false, true, true); lock (Kilit) akis = d.Akislar.FirstOrDefault(a => a.Ay == ay);
                if (akis == null) return (false, "Ödeme satırı açılamadı: " + r0.mesaj, d);
            }
            var eklenen = new List<string>(); var hatalar = new List<string>(); string etiket = $"{d.Tedarikci} · {d.UrunHizmet} · {AyAdi(ay)}";
            foreach (var cid in liste)
            {
                if (mevcut.Contains(cid)) continue; var k = CekAkibetOrnek.Bul(cid); if (k == null) { hatalar.Add($"#{cid} bulunamadı"); continue; }
                bool ok; string mesajR;
                if (kullanim == "Ciro") { var b = NakitAkisOrnek.BelgePlanla(akis.DbName, akis.DocEntry, new[] { cid }, kullanici); ok = b.ok; mesajR = b.mesaj; }
                else { var r = CekAkibetOrnek.Planla(cid, "Tahsil → " + etiket, akis?.DocEntry, d.Sirket, kullanici); ok = r.ok; mesajR = r.mesaj; }
                if (!ok) { hatalar.Add(mesajR); continue; }
                var p = new DuzenliCekPlani { Ay = ay, CekDocEntry = cid, CekNo = k.No, Belge = k.Belge, CekSirket = k.Sirket, Tutar = k.Tutar, Vade = k.Vade, Kullanim = kullanim, Zaman = DateTime.Now, Kullanici = kullanici ?? "" };
                lock (Kilit) { d.CekPlanlari.Add(p); Log(d, "Çek Planlandı", $"{AyAdi(ay)} · {k.No} · vade {k.Vade:dd.MM.yyyy} · {k.Tutar.ToString("N2", Tr)} ₺ · {(kullanim == "Ciro" ? "tedarikçiye ciro" : "bankaya tahsil")}" + (k.Vade.Date > tarih ? $" · DİKKAT: vade ödeme gününden ({tarih:dd.MM.yyyy}) sonra" : ""), kullanici); }
                eklenen.Add(k.No);
            }
            decimal planlanan; lock (Kilit) planlanan = d.CekPlanlari.Where(p => p.Ay == ay).Sum(p => p.Tutar);
            if (eklenen.Count > 0) try { FinekraOrnek.GunlukEkle(d.Sirket, d.No, "Düzenli Ödeme Çek Planı", $"{etiket} · {string.Join(", ", eklenen)} · planlanan {planlanan.ToString("N2", Tr)} ₺ / ihtiyaç {d.AylikTutar.ToString("N2", Tr)} ₺ · {(kullanim == "Ciro" ? "ciro" : "tahsil")}", kullanici); } catch { }
            string mesaj = eklenen.Count == 0 ? "Çek planlanamadı: " + string.Join(" · ", hatalar)
                : $"{AyAdi(ay)} için {eklenen.Count} belge planlandı ({string.Join(", ", eklenen)}) · planlanan {planlanan.ToString("N0", Tr)} ₺ / ihtiyaç {d.AylikTutar.ToString("N0", Tr)} ₺ ({(d.AylikTutar > 0 ? Math.Round(planlanan / d.AylikTutar * 100) : 0)}%) · {(kullanim == "Ciro" ? "tedarikçiye ciro edilecek; Kasa Nakit Akışı satırına bağlandı" : "vadesinde bankaya tahsile verilecek; ödeme nakit / havale")}" + (hatalar.Count > 0 ? " · " + string.Join(" · ", hatalar) : "");
            return (eklenen.Count > 0, mesaj, d);
        }
        /// <summary>Çek planından belge çıkarır: belge portföye döner (Planlandı → Portföyde); ciro planında Kasa Nakit Akışı satırından da çıkar.</summary>
        public static (bool ok, string mesaj, DuzenliOdeme d) CekPlanKaldir(int id, string ay, int cekDocEntry, string kullanici)
        {
            var d = Bul(id); if (d == null) return (false, "Kayıt bulunamadı.", null);
            DuzenliCekPlani p; DuzenliAkis akis; lock (Kilit) { p = d.CekPlanlari.FirstOrDefault(x => x.Ay == ay && x.CekDocEntry == cekDocEntry); akis = d.Akislar.FirstOrDefault(a => a.Ay == ay); }
            if (p == null) return (false, "Plan bulunamadı.", d);
            var k = CekAkibetOrnek.Bul(cekDocEntry); string sonuc;
            if (k != null && k.IslemTuru != "Planlandı") sonuc = $"{p.CekNo} artık planlı değil (işlem türü {k.IslemTuru}); plan kaydı listeden kaldırıldı.";
            else
            {
                bool ok; string m2;
                if (p.Kullanim == "Ciro" && akis != null) { var b = NakitAkisOrnek.BelgePlanKaldir(akis.DbName, akis.DocEntry, cekDocEntry, kullanici); ok = b.ok; m2 = b.mesaj; if (!ok && b.mesaj == "Belge bu satırda değil.") { var r = CekAkibetOrnek.PlanKaldir(cekDocEntry, kullanici); ok = r.ok; m2 = r.mesaj; } }
                else { var r = CekAkibetOrnek.PlanKaldir(cekDocEntry, kullanici); ok = r.ok; m2 = r.mesaj; }
                if (!ok) return (false, m2, d); sonuc = $"{p.CekNo} {AyAdi(ay)} çek planından çıkarıldı; portföye döndü.";
            }
            lock (Kilit) { d.CekPlanlari.Remove(p); Log(d, "Çek Planı Kaldırıldı", $"{AyAdi(ay)} · {p.CekNo} · {p.Tutar.ToString("N2", Tr)} ₺", kullanici); }
            return (true, sonuc, d);
        }
        /// <summary>
        /// Bütçe Raporu'na otomatik katkı (Data/ButceOrnek.cs kalemleri): şirket × yıl × kalem için 12 aylık PLAN (aktif kayıtların aylık tutarı; akmış aylarda Kasa Nakit Akışı satır tutarı) ve ÖDENEN (ÖDENDİ satırlar);
        /// kayıt listesi (kalem eşlemesi, çek planı özeti). Kayıt tarihi öncesi aylara plan yazılmaz.
        /// </summary>
        public static object ButceKatki(IEnumerable<int> yillar)
        {
            var l = Listele(); var bugun = DateTime.Today; var buAyBas = new DateTime(bugun.Year, bugun.Month, 1); var katkilar = new List<object>();
            foreach (int yil in yillar)
                foreach (var g in l.GroupBy(d => new { d.Sirket, Kalem = string.IsNullOrEmpty(d.ButceKalem) ? KalemOner(d.Kategori, d.UrunHizmet) : d.ButceKalem }))
                {
                    var plan = new decimal[12]; var odenen = new decimal[12]; var kayitlar = new List<object>(); bool dolu = false;
                    foreach (var d in g)
                    {
                        List<DuzenliAkis> ak; lock (Kilit) ak = d.Akislar.ToList();
                        for (int i = 0; i < 12; i++)
                        {
                            var ayBas = new DateTime(yil, i + 1, 1); string ayK = AyKodu(ayBas); var a = ak.FirstOrDefault(x => x.Ay == ayK);
                            if (a != null) { var m = NakitAkisOrnek.Bul(a.DbName, a.DocEntry); decimal t = m?.U_TutarTL ?? a.Tutar; plan[i] += t; if (m?.U_Durum == "ÖDENDİ") odenen[i] += t; dolu = true; }
                            else if (d.Durum == "Aktif" && ayBas >= buAyBas) { plan[i] += d.AylikTutar; dolu = true; }
                        }
                        kayitlar.Add(new { d.No, d.Tedarikci, d.UrunHizmet, d.OdemeSekli, d.Gun, d.AylikTutar, d.Durum });
                    }
                    if (dolu) katkilar.Add(new { sirket = g.Key.Sirket, yil, kalemKod = g.Key.Kalem, plan, odenen, kayitlar });
                }
            var kayit = l.Select(d => { var b = CekPlanHesapla(d); string kalem = string.IsNullOrEmpty(d.ButceKalem) ? KalemOner(d.Kategori, d.UrunHizmet) : d.ButceKalem; return new { d.Id, d.No, d.Tedarikci, d.UrunHizmet, d.Sirket, d.OdemeSekli, d.Kategori, d.Gun, d.AylikTutar, d.Durum, butceKalem = kalem, butceKalemAd = ButceOrnek.KalemBul(kalem)?.Ad ?? "", cekPlaniOzet = b.Ozet, sonPlanliAyAdi = b.SonPlanli == null ? "" : AyAdi(b.SonPlanli), toplamPlanlanan = b.Toplam }; }).ToList();
            return new { katkilar, kayitlar = kayit };
        }
        /// <summary>Sekme özeti: aylık yük, nakit / çek dağılımı, bu ay akan / ödenen / planlı / akmamış, gelecek ay, şirket ve kategori toplamları.</summary>
        public static object Ozet(List<DuzenliOdeme> l)
        {
            string ay = BuAy, sonraki = SonrakiAy; var aktif = l.Where(d => d.Durum == "Aktif").ToList();
            var buAy = aktif.Select(d => d.Akislar.FirstOrDefault(a => a.Ay == ay)).Where(a => a != null).Select(a => new { a, m = NakitAkisOrnek.Bul(a.DbName, a.DocEntry) }).ToList();
            return new
            {
                ay, ayAdi = AyAdi(ay), sonrakiAy = sonraki, sonrakiAyAdi = AyAdi(sonraki), aktif = aktif.Count, pasif = l.Count - aktif.Count, aylikYuk = aktif.Sum(d => d.AylikTutar),
                nakit = aktif.Where(d => d.OdemeSekli == "Nakit").Sum(d => d.AylikTutar), cek = aktif.Where(d => d.OdemeSekli.Contains("Çek")).Sum(d => d.AylikTutar), havale = aktif.Where(d => d.OdemeSekli == "Havale / EFT").Sum(d => d.AylikTutar),
                akan = buAy.Count, akanTutar = buAy.Sum(x => x.m?.U_TutarTL ?? x.a.Tutar), odenen = buAy.Count(x => x.m?.U_Durum == "ÖDENDİ"), odenenTutar = buAy.Where(x => x.m?.U_Durum == "ÖDENDİ").Sum(x => x.m.U_TutarTL ?? x.a.Tutar),
                planli = buAy.Count(x => x.m != null && x.m.U_Durum != "ÖDENDİ"), planliTutar = buAy.Where(x => x.m != null && x.m.U_Durum != "ÖDENDİ").Sum(x => x.m.U_TutarTL ?? x.a.Tutar),
                eksik = aktif.Count(d => !d.Akislar.Any(a => a.Ay == ay)), eksikTutar = aktif.Where(d => !d.Akislar.Any(a => a.Ay == ay)).Sum(d => d.AylikTutar),
                sonrakiAyAkan = aktif.Count(d => d.Akislar.Any(a => a.Ay == sonraki)),
                // çek planı özeti: kaç kayıtta çek planı var, toplam planlanan, en ileri ay, bu ay / gelecek ay karşılama
                cekPlanliKayit = aktif.Count(d => d.CekPlanlari.Count > 0), cekPlanToplam = aktif.Sum(d => d.CekPlanlari.Sum(p => p.Tutar)), cekPlanAdet = aktif.Sum(d => d.CekPlanlari.Count),
                cekEnIleriAy = aktif.SelectMany(d => d.CekPlanlari).Select(p => p.Ay).DefaultIfEmpty("").Max(), cekEnIleriAyAdi = aktif.SelectMany(d => d.CekPlanlari).Any() ? AyAdi(aktif.SelectMany(d => d.CekPlanlari).Max(p => p.Ay)) : "",
                buAyCekPlanlanan = aktif.Sum(d => d.CekPlanlari.Where(p => p.Ay == ay).Sum(p => p.Tutar)), buAyCekTam = aktif.Count(d => d.CekPlanlari.Where(p => p.Ay == ay).Sum(p => p.Tutar) >= d.AylikTutar - 0.005m),
                sonrakiAyCekPlanlanan = aktif.Sum(d => d.CekPlanlari.Where(p => p.Ay == sonraki).Sum(p => p.Tutar)), sonrakiAyCekTam = aktif.Count(d => d.CekPlanlari.Where(p => p.Ay == sonraki).Sum(p => p.Tutar) >= d.AylikTutar - 0.005m),
                sirketler = aktif.GroupBy(d => d.Sirket).Select(g => new { sirket = g.Key, adet = g.Count(), tutar = g.Sum(d => d.AylikTutar) }).OrderByDescending(x => x.tutar).ToList(),
                kategoriler = aktif.GroupBy(d => d.Kategori).Select(g => new { kategori = g.Key, adet = g.Count(), tutar = g.Sum(d => d.AylikTutar) }).OrderByDescending(x => x.tutar).ToList()
            };
        }
    }
}
