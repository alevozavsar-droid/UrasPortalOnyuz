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
                { var d = new DuzenliOdeme { Id = ++_sonId, No = $"DÖ-{_sonId:000}", Tedarikci = tedarikci, UrunHizmet = urun, Sirket = sirket, DbName = CekAkibetOrnek.DbName(sirket), OdemeSekli = sekil, Kategori = kategori, Gun = gun, AylikTutar = tutar, Olusturma = b.AddDays(-200), Olusturan = Sorumlu }; d.Gunluk.Add(new TalimatGunluk { Tarih = d.Olusturma.AddHours(10), Kullanici = Sorumlu, Islem = "Kayıt", Aciklama = "Aylık ödeme takvim listesinden alındı" }); _liste.Add(d); }
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
        }

        public static List<DuzenliOdeme> Listele(string dbName = null) { Tohumla(); lock (Kilit) return _liste.Where(d => string.IsNullOrEmpty(dbName) || d.DbName == dbName).OrderBy(d => d.Sirket, StringComparer.Create(Tr, false)).ThenBy(d => d.Gun).ThenBy(d => d.Id).ToList(); }
        public static DuzenliOdeme Bul(int id) { Tohumla(); lock (Kilit) return _liste.FirstOrDefault(d => d.Id == id); }

        /// <summary>Bir ayın düzenli ödemelerini Kasa Nakit Akışı'na akıtır (aktif kayıtlar; o aya akmış olanlar atlanır). odenmisSay: geçmiş ay tohumu — satır ödendi. panelGunlugu: panel günlüğüne de yaz (tohumda kapalı: kilit sırası).</summary>
        public static (bool ok, string mesaj, int eklenen, int atlanan) Akit(string ay, string kullanici, string dbName = null, int? id = null, bool odenmisSay = false, bool panelGunlugu = true)
        {
            Tohumla(); if (!AyCoz(ay, out var ayBas)) return (false, "Ay AAAA-AA biçiminde olmalı.", 0, 0);
            if (ayBas > DateTime.Today.AddMonths(1)) return (false, "En fazla gelecek ay planlanabilir.", 0, 0);
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

        public static (bool ok, string mesaj, DuzenliOdeme d) Kaydet(int id, string tedarikci, string urunHizmet, string sirket, string odemeSekli, string kategori, int gun, decimal aylikTutar, string not, string kullanici)
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
                    var degisen = new List<string>(); if (d.AylikTutar != aylikTutar) degisen.Add($"tutar {d.AylikTutar.ToString("N0", Tr)} → {aylikTutar.ToString("N0", Tr)}"); if (d.Gun != gun) degisen.Add($"gün {d.Gun} → {gun}"); if (d.OdemeSekli != odemeSekli) degisen.Add($"ödeme şekli {d.OdemeSekli} → {odemeSekli}"); if (d.Sirket != sirket) degisen.Add($"şirket {d.Sirket} → {sirket}"); if (d.Tedarikci != tedarikci || d.UrunHizmet != urunHizmet) degisen.Add("ad / hizmet");
                    Log(d, "Güncelleme", degisen.Count == 0 ? "Değişiklik yok" : string.Join(" · ", degisen) + " · bir sonraki akışta geçerli", kullanici);
                }
                d.Tedarikci = tedarikci; d.UrunHizmet = urunHizmet; d.Sirket = sirket; d.DbName = CekAkibetOrnek.DbName(sirket); d.OdemeSekli = odemeSekli; d.Kategori = kategori; d.Gun = gun; d.AylikTutar = aylikTutar; d.Not = (not ?? "").Trim();
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
            return new
            {
                d.Id, d.No, d.Tedarikci, d.UrunHizmet, d.Sirket, d.DbName, d.OdemeSekli, yontem = Yontem(d.OdemeSekli), d.Kategori, d.Gun, d.AylikTutar, d.PB, d.Sorumlu, d.Durum, d.Not, olusturma = d.Olusturma.ToString("yyyy-MM-dd"), d.Olusturan,
                gecenAy = AkisJson(ak.FirstOrDefault(a => a.Ay == gecen)), buAy = AkisJson(ak.FirstOrDefault(a => a.Ay == BuAy)), sonrakiAy = AkisJson(ak.FirstOrDefault(a => a.Ay == SonrakiAy)),
                akisSayisi = ak.Count, toplamAkan = ak.Sum(a => a.Tutar), akislar = ak.OrderByDescending(a => a.Ay).Select(AkisJson).ToList(),
                gunluk = g.OrderByDescending(x => x.Tarih).Take(12).Select(x => new { tarih = x.Tarih.ToString("yyyy-MM-dd HH:mm"), x.Kullanici, x.Islem, x.Aciklama }).ToList()
            };
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
                sirketler = aktif.GroupBy(d => d.Sirket).Select(g => new { sirket = g.Key, adet = g.Count(), tutar = g.Sum(d => d.AylikTutar) }).OrderByDescending(x => x.tutar).ToList(),
                kategoriler = aktif.GroupBy(d => d.Kategori).Select(g => new { kategori = g.Key, adet = g.Count(), tutar = g.Sum(d => d.AylikTutar) }).OrderByDescending(x => x.tutar).ToList()
            };
        }
    }
}
