using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>Departmanlardan gelen ödeme talebi: fatura, sipariş, muhasebeden gelen vergi / SGK / gümrük ödemesi, kira … — Kasa Nakit Akışı › Yeni Ödeme › Departman Talepleri sekmesinde seçilip doğrudan satır olur.</summary>
    public class OdemeTalebi
    {
        public int Id { get; set; } public string No { get; set; } public DateTime Tarih { get; set; } public string DbName { get; set; } public string Sirket { get; set; }
        public string Departman { get; set; } public string TalepEden { get; set; } public string Kurum { get; set; } public string BelgeTipi { get; set; }   // Fatura | Sipariş | Vergi | SGK | Gümrük | Kira | Diğer
        public string BelgeNo { get; set; } = ""; public string Aciklama { get; set; } = ""; public decimal Tutar { get; set; } public string PB { get; set; } = "TRY"; public DateTime SonOdeme { get; set; }
        public string Oncelik { get; set; } = "Normal"; public string YontemOnerisi { get; set; } = "Havale / EFT"; public string Durum { get; set; } = "Onay Bekliyor";   // Onay Bekliyor | Onaylandı | Nakit Akışa Alındı | Reddedildi
        public string Onaylayan { get; set; } = ""; public DateTime? OnayTarihi { get; set; } public int? NakitAkisDocEntry { get; set; } public DateTime? AlinmaZamani { get; set; } public string Alan { get; set; } = "";
        public decimal TutarTL => Math.Round(Tutar * (NakitAkisOrnek.Kurlar.TryGetValue(PB, out var k) ? k : 1m), 2);
        /// <summary>Kasa Nakit Akışı belge tipi karşılığı (Vergi / SGK / Gümrük → Resmi Ödeme, Kira → Diğer).</summary>
        public string NakitAkisBelgeTipi => BelgeTipi == "Fatura" || BelgeTipi == "Sipariş" ? BelgeTipi : BelgeTipi == "Vergi" || BelgeTipi == "SGK" || BelgeTipi == "Gümrük" ? "Resmi Ödeme" : "Diğer";
    }

    public static class OdemeTalepOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly List<OdemeTalebi> _talepler = new List<OdemeTalebi>();
        static int _sonId; static bool _tohumlandi;
        public static readonly string[] Departmanlar = { "Satınalma", "Muhasebe", "İdari İşler", "Lojistik", "Üretim", "İnsan Kaynakları", "Bilgi Teknolojileri" };

        static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var bugun = DateTime.Today;
                OdemeTalebi T(string db, string departman, string talepEden, string kurum, string belgeTipi, string belgeNo, string aciklama, decimal tutar, string pb, int sonOdemeGun, string oncelik, string yontem, string durum, string onaylayan, int gunOnce)
                {
                    var t = new OdemeTalebi { Id = ++_sonId, No = $"ÖT-{bugun.Year}-{_sonId:0000}", Tarih = bugun.AddDays(-gunOnce).AddHours(9 + _sonId % 7), DbName = db, Sirket = NakitAkisOrnek.SirketAdi(db), Departman = departman, TalepEden = talepEden, Kurum = kurum, BelgeTipi = belgeTipi, BelgeNo = belgeNo, Aciklama = aciklama, Tutar = tutar, PB = pb, SonOdeme = bugun.AddDays(sonOdemeGun), Oncelik = oncelik, YontemOnerisi = yontem, Durum = durum, Onaylayan = onaylayan, OnayTarihi = durum == "Onaylandı" ? bugun.AddDays(-gunOnce + 1).AddHours(14) : (DateTime?)null };
                    _talepler.Add(t); return t;
                }
                // Uras Kimya
                T("URASKIMYA", "Muhasebe", "Nazlı Erdem", "Vergi Dairesi · Yenibosna", "Vergi", "KDV-2026-10", "Ekim 2026 KDV beyannamesi (1 no.lu KDV)", 1_284_500m, "TRY", 13, "Acil", "Havale / EFT", "Onaylandı", "Neslihan Uludağ", 3);
                T("URASKIMYA", "Muhasebe", "Nazlı Erdem", "SGK Gebze", "SGK", "SGK-2026-10", "Ekim 2026 SGK prim ve işsizlik primi (aylık bildirge)", 2_150_000m, "TRY", 18, "Normal", "Havale / EFT", "Onay Bekliyor", "", 2);
                T("URASKIMYA", "Muhasebe", "Onur Bal", "Vergi Dairesi · Yenibosna", "Vergi", "MUH-2026-10", "Ekim 2026 muhtasar ve prim hizmet beyannamesi", 486_300m, "TRY", 13, "Normal", "Havale / EFT", "Onaylandı", "Neslihan Uludağ", 2);
                T("URASKIMYA", "Satınalma", "Selin Kara", "Brenntag", "Sipariş", "SP-2026-4471", "Sodyum hidroksit %50 · 2 tanker · peşin ödeme indirimi için", 418_600m, "TRY", 5, "Acil", "Havale / EFT", "Onaylandı", "Özge İsen", 1);
                T("URASKIMYA", "Satınalma", "Selin Kara", "BASF SE", "Sipariş", "SP-2026-4480", "Katalizör ön ödemesi (%30 avans)", 24_600m, "EUR", 9, "Normal", "Havale / EFT", "Onay Bekliyor", "", 1);
                T("URASKIMYA", "İdari İşler", "Canan Su", "Ofis Kirası · Mal Sahibi", "Kira", "KR-2026-11", "Kasım 2026 ofis kirası (stopaj dahil)", 185_000m, "TRY", 19, "Normal", "Havale / EFT", "Onaylandı", "Alev Özavşar", 4);
                T("URASKIMYA", "Lojistik", "Tolga Yaman", "TF Gümrük", "Gümrük", "GB-2026-77412", "İthalat gümrük vergisi + KKDF (beyanname 77412)", 312_900m, "TRY", 2, "Çok Acil", "Havale / EFT", "Onaylandı", "Neslihan Uludağ", 0);
                T("URASKIMYA", "Üretim", "Mert Doğan", "Onur Kompresör", "Fatura", "FT-2026-70915", "Kompresör acil bakım ve yedek parça", 73_500m, "TRY", 7, "Acil", "Nakit", "Onay Bekliyor", "", 0);
                T("URASKIMYA", "Bilgi Teknolojileri", "Ali Veli", "Microsoft İrlanda", "Fatura", "INV-MS-884120", "Microsoft 365 yıllık lisans yenileme", 14_200m, "USD", 11, "Normal", "Kredi Kartı / Banka Kartı", "Onaylandı", "Özge İsen", 5);
                T("URASKIMYA", "İnsan Kaynakları", "Hülya Er", "Anadolu Sigorta", "Fatura", "PL-2026-55817", "Tamamlayıcı sağlık sigortası Kasım taksidi", 96_800m, "TRY", 15, "Normal", "Havale / EFT", "Onaylandı", "Alev Özavşar", 6);
                // Selvi
                T("SELVI", "Muhasebe", "Nazlı Erdem", "Vergi Dairesi · Gebze", "Vergi", "KDV-2026-10", "Ekim 2026 KDV beyannamesi", 212_400m, "TRY", 13, "Acil", "Havale / EFT", "Onaylandı", "Neslihan Uludağ", 3);
                T("SELVI", "Satınalma", "Emre Taş", "Korozo Ambalaj", "Sipariş", "SP-2026-1190", "Ambalaj film siparişi ön ödeme", 148_000m, "TRY", 6, "Normal", "Çek", "Onay Bekliyor", "", 1);
                T("SELVI", "Muhasebe", "Onur Bal", "SGK Gebze", "SGK", "SGK-2026-10", "Ekim 2026 SGK prim", 364_000m, "TRY", 18, "Normal", "Havale / EFT", "Onaylandı", "Neslihan Uludağ", 2);
                // Avrupa Paper · Alv Kimya · Daf Kimya · URS Makine · Uras Holding
                T("AVRUPA_PAPER", "Satınalma", "Burcu Aydın", "Mondi Group", "Sipariş", "SP-2026-2207", "Kağıt bobin siparişi · akreditif dışı peşin kısım", 38_500m, "EUR", 8, "Normal", "Havale / EFT", "Onaylandı", "Özge İsen", 2);
                T("AVRUPA_PAPER", "Muhasebe", "Nazlı Erdem", "Vergi Dairesi · Avcılar", "Vergi", "KDV-2026-10", "Ekim 2026 KDV beyannamesi", 97_300m, "TRY", 13, "Acil", "Havale / EFT", "Onay Bekliyor", "", 1);
                T("ALV_KIMYA", "Muhasebe", "Nazlı Erdem", "SGK İstanbul", "SGK", "SGK-2026-10", "Ekim 2026 SGK prim", 128_600m, "TRY", 18, "Normal", "Havale / EFT", "Onaylandı", "Neslihan Uludağ", 2);
                T("ALV_KIMYA", "İdari İşler", "Canan Su", "Depo Kirası · Mal Sahibi", "Kira", "KR-2026-11", "Kasım 2026 depo kirası", 64_000m, "TRY", 19, "Normal", "Havale / EFT", "Onay Bekliyor", "", 3);
                T("DAF_KIMYA", "Satınalma", "Selin Kara", "Dalgakıran", "Fatura", "FT-2026-83130", "Hava kompresörü bakım sözleşmesi", 52_900m, "TRY", 10, "Normal", "Havale / EFT", "Onaylandı", "Özge İsen", 4);
                T("URSMAKINE", "Muhasebe", "Onur Bal", "Vergi Dairesi · Gebze", "Vergi", "MUH-2026-10", "Ekim 2026 muhtasar beyannamesi", 58_700m, "TRY", 13, "Normal", "Havale / EFT", "Onaylandı", "Neslihan Uludağ", 2);
                T("URAS_HOLDING", "İdari İşler", "Sevgi Ay", "Yönetim Binası Kirası · Mal Sahibi", "Kira", "KR-2026-11", "Kasım 2026 yönetim binası kirası", 240_000m, "TRY", 19, "Normal", "Havale / EFT", "Onaylandı", "Alev Özavşar", 5);
            }
        }

        public static List<OdemeTalebi> Listele(string dbName, bool sadeceAcik = true) { Tohumla(); lock (Kilit) return _talepler.Where(t => (string.IsNullOrEmpty(dbName) || t.DbName == dbName) && (!sadeceAcik || t.Durum == "Onay Bekliyor" || t.Durum == "Onaylandı")).OrderBy(t => t.SonOdeme).ThenBy(t => t.Id).ToList(); }
        public static OdemeTalebi Bul(string no) { Tohumla(); lock (Kilit) return _talepler.FirstOrDefault(t => string.Equals(t.No, no, StringComparison.OrdinalIgnoreCase)); }

        /// <summary>Talebi Kasa Nakit Akışı satırı yapar (doğrudan ekleme): kurum, belge tipi / no, tutar, son ödeme, önerilen yöntem, talep eden; nakit yöntemde satır onaya düşer.</summary>
        public static (bool ok, string mesaj, NakitAkisViewModel satir) NakitAkisaAl(string no, string kullanici)
        {
            var t = Bul(no); if (t == null) return (false, "Ödeme talebi bulunamadı.", null);
            if (t.Durum == "Nakit Akışa Alındı") return (false, $"{t.No} zaten nakit akışta (#{t.NakitAkisDocEntry}).", NakitAkisOrnek.Bul(t.DbName, t.NakitAkisDocEntry ?? 0));
            if (t.Durum == "Reddedildi") return (false, $"{t.No} reddedilmiş.", null);
            var bugun = DateTime.Today;
            var m = new NakitAkisViewModel
            {
                U_Kurum = t.Kurum, U_UrunHizmet = t.Aciklama, U_OdemeYontemi = t.YontemOnerisi, U_BagliBelgeTipi = t.NakitAkisBelgeTipi, U_KayitTipi = t.NakitAkisBelgeTipi, U_FaturaSiparisNo = t.BelgeNo, BelgeTarihi = t.Tarih.Date,
                U_SonOdemeTarihi = t.SonOdeme, U_PlanlananTarih = t.SonOdeme < bugun ? bugun : t.SonOdeme, U_Tutar = t.Tutar, U_ParaBirimi = t.PB, U_Kur = NakitAkisOrnek.Kurlar.TryGetValue(t.PB, out var k) ? k : 1m, U_TutarTL = t.TutarTL, U_FaturaTutari = t.Tutar,
                U_TalepEden = t.TalepEden, U_IslemTipi = "X", U_Satinalmaci = t.Departman == "Satınalma" ? t.TalepEden : "", U_Durum = t.Durum == "Onaylandı" ? "Planlandı" : "Bekliyor",
                U_Notlar = $"Departman talebi {t.No} · {t.Departman} · {t.BelgeTipi}" + (t.Oncelik != "Normal" ? " · " + t.Oncelik : "") + (t.Durum == "Onaylandı" ? $" · onay {t.Onaylayan}" : " · departman onayı bekliyor")
            };
            var satir = NakitAkisOrnek.Ekle(t.DbName, m, kullanici);
            lock (Kilit) { t.Durum = "Nakit Akışa Alındı"; t.NakitAkisDocEntry = satir.DocEntry; t.AlinmaZamani = DateTime.Now; t.Alan = kullanici ?? ""; }
            NakitAkisOrnek.Gunluge(t.DbName, satir.DocEntry, "Departman Talebi", $"{t.No} ({t.Departman} · {t.TalepEden}) nakit akışa alındı · {t.BelgeTipi} {t.BelgeNo} · {t.Tutar.ToString("N2", Tr)} {t.PB}", kullanici);
            return (true, $"{t.No} nakit akışa alındı (#{satir.DocEntry}): {t.Kurum} · {t.Tutar.ToString("N2", Tr)} {t.PB} · {t.YontemOnerisi}" + (t.YontemOnerisi == "Nakit" ? " · nakit onayı bekliyor" : "") + ".", satir);
        }

        public static object Json(OdemeTalebi t) => new { no = t.No, tarih = t.Tarih.ToString("yyyy-MM-dd"), dbName = t.DbName, sirket = t.Sirket, departman = t.Departman, talepEden = t.TalepEden, kurum = t.Kurum, belgeTipi = t.BelgeTipi, nakitAkisBelgeTipi = t.NakitAkisBelgeTipi, belgeNo = t.BelgeNo, aciklama = t.Aciklama, tutar = t.Tutar, pb = t.PB, tutarTL = t.TutarTL, sonOdeme = t.SonOdeme.ToString("yyyy-MM-dd"), gecikme = (DateTime.Today - t.SonOdeme).Days, oncelik = t.Oncelik, yontem = t.YontemOnerisi, durum = t.Durum, onaylayan = t.Onaylayan, onayTarihi = t.OnayTarihi?.ToString("yyyy-MM-dd"), nakitAkisDocEntry = t.NakitAkisDocEntry };
    }
}
