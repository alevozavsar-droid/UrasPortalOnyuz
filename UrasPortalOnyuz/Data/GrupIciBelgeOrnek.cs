using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>
    /// Fatura sahibi ≠ ödeyen şirket olan her ödemede otomatik oluşan grup içi belge (Mahsup ya da Temlik). Ödeme gerçekleşince iki şirkette muhasebe fişi atılır:
    /// ödeyen (B) fişi ödeme fişidir (MuhasebeOrnek.OdemeFisi: 132 İştiraklerden Alacaklar ya da — B'nin A'ya borcu varsa, mahsup — 332 borç / 102 · 100 · 101 alacak);
    /// fatura sahibi (A) fişi burada atılır: 320 Satıcılar borç / 132 (B'den alacağı mahsup edildiyse) ya da 332 İştiraklere Borçlar alacak. Grup içi bakiye (CekAkibetOrnek.GrupBakiyeleri) güncellenir.
    /// Kasa Nakit Akışı › Mahsup / Temlik sekmelerinde listelenir; yazdırılabilir belge /Rapor79/GrupIciBelge?no=. Kilit sırası: NakitAkis → GrupIciBelge → CekAkibet → Muhasebe.
    /// </summary>
    public class GrupIciBelge
    {
        public int Id { get; set; } public string No { get; set; } public string Tur { get; set; } = "Mahsup"; public DateTime Tarih { get; set; }
        public string KaynakDb { get; set; } public int KaynakDocEntry { get; set; }
        public string FaturaSahibi { get; set; } public string FaturaSahibiDb { get; set; } public string Odeyen { get; set; } public string OdeyenDb { get; set; }
        public string Kurum { get; set; } = ""; public string BelgeTipi { get; set; } = ""; public string BelgeNo { get; set; } = ""; public string Aciklama { get; set; } = ""; public string Yontem { get; set; } = "";
        public decimal Tutar { get; set; } public string PB { get; set; } = "TRY"; public decimal Kur { get; set; } = 1; public decimal TutarTL { get; set; }
        public string Durum { get; set; } = "Hazırlandı";   // Hazırlandı | Muhasebeleşti | İptal
        public string Olusturan { get; set; } = ""; public DateTime OlusturmaZamani { get; set; } public DateTime? MuhasebeTarihi { get; set; }
        public string FaturaSahibiFisNo { get; set; } = ""; public string OdeyenFisNo { get; set; } = ""; public string MahsupSekli { get; set; } = "";   // "Karşılıklı bakiyeden mahsup" | "Grup içi borç" | "Temlik"
        public List<TalimatGunluk> Gunluk { get; set; } = new List<TalimatGunluk>();
        public string Baslik => Tur == "Temlik" ? "Grup İçi Temlik Belgesi" : "Grup İçi Mahsup Belgesi";
    }

    public static class GrupIciBelgeOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly List<GrupIciBelge> _belgeler = new List<GrupIciBelge>();
        static int _sonId;
        public static readonly string[] Turler = { "Mahsup", "Temlik" };

        public static List<GrupIciBelge> Listele(string dbName, string tur = null) { lock (Kilit) return _belgeler.Where(b => (string.IsNullOrEmpty(dbName) || b.FaturaSahibiDb == dbName || b.OdeyenDb == dbName || b.KaynakDb == dbName) && (string.IsNullOrEmpty(tur) || b.Tur == tur)).OrderByDescending(b => b.Tarih).ThenByDescending(b => b.Id).ToList(); }
        public static GrupIciBelge Bul(string no) { lock (Kilit) return _belgeler.FirstOrDefault(b => string.Equals(b.No, no, StringComparison.OrdinalIgnoreCase)); }
        public static GrupIciBelge BulSatir(int docEntry) { lock (Kilit) return _belgeler.Where(b => b.KaynakDocEntry == docEntry && b.Durum != "İptal").OrderByDescending(b => b.Id).FirstOrDefault(); }

        /// <summary>Satır için grup içi belge gerekir mi: fatura sahibi ≠ ödeyen, avans değil, yöntemin kendisi grup içi işlem değil, çek çıkış talebi değil.</summary>
        public static bool Gerekli(NakitAkisViewModel m) => m != null && !string.IsNullOrEmpty(m.U_FaturaSahibi) && !string.IsNullOrEmpty(m.U_OdeyenSirket) && m.U_FaturaSahibi != m.U_OdeyenSirket
            && string.IsNullOrEmpty(m.U_AvansNo) && m.U_OdemeYontemi != "Mahsup" && m.U_OdemeYontemi != "Temlik" && m.U_OdemeYontemi != "Yansıtma" && string.IsNullOrEmpty(m.U_CikisTalebi) && (m.U_Tutar ?? 0) > 0;

        /// <summary>Satır kaydedilince / değişince belge yaratır ya da günceller; gerek kalmadıysa hazır belgeyi iptal eder; satır ödenmişse muhasebeleştirir.</summary>
        public static GrupIciBelge Senkron(NakitAkisViewModel m, string kullanici)
        {
            if (m == null) return null;
            GrupIciBelge b;
            lock (Kilit)
            {
                b = _belgeler.Where(x => x.KaynakDocEntry == m.DocEntry && x.Durum != "İptal").OrderByDescending(x => x.Id).FirstOrDefault();
                if (!Gerekli(m))
                {
                    if (b != null && b.Durum == "Hazırlandı") { b.Durum = "İptal"; b.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = "İptal", Aciklama = "Fatura sahibi ve ödeyen şirket aynı oldu / satır grup içi ödeme olmaktan çıktı" }); }
                    m.U_GrupIciNo = b != null && b.Durum == "Muhasebeleşti" ? b.No : ""; if (b == null || b.Durum != "Muhasebeleşti") m.U_GrupIciTur = "";
                    return b != null && b.Durum == "Muhasebeleşti" ? b : null;
                }
                string tur = Turler.Contains(m.U_GrupIciTur ?? "") ? m.U_GrupIciTur : (b?.Tur ?? "Mahsup"); m.U_GrupIciTur = tur;
                if (b != null && b.Durum == "Muhasebeleşti") { m.U_GrupIciNo = b.No; m.U_GrupIciTur = b.Tur; return b; }   // muhasebeleşmiş belge değişmez
                if (b != null && b.Tur != tur) { b.Durum = "İptal"; b.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = "İptal", Aciklama = $"Belge türü {b.Tur} → {tur}; yeni belge açıldı" }); b = null; }
                bool yeni = b == null;
                if (yeni) b = new GrupIciBelge { Id = ++_sonId, No = $"{(tur == "Temlik" ? "GİT" : "GİM")}-{DateTime.Today.Year}-{_sonId:0000}", Tur = tur, Tarih = DateTime.Now, Olusturan = kullanici ?? "", OlusturmaZamani = DateTime.Now, KaynakDb = m.U_KaynakFirma, KaynakDocEntry = m.DocEntry };
                b.FaturaSahibi = m.U_FaturaSahibi; b.FaturaSahibiDb = CekAkibetOrnek.DbName(m.U_FaturaSahibi); b.Odeyen = m.U_OdeyenSirket; b.OdeyenDb = CekAkibetOrnek.DbName(m.U_OdeyenSirket);
                b.Kurum = m.U_Kurum ?? ""; b.BelgeTipi = m.U_BagliBelgeTipi ?? ""; b.BelgeNo = m.U_FaturaSiparisNo ?? ""; b.Aciklama = m.U_UrunHizmet ?? ""; b.Yontem = m.U_OdemeYontemi ?? "";
                b.Tutar = m.U_Tutar ?? 0; b.PB = m.U_ParaBirimi ?? "TRY"; b.Kur = m.U_Kur ?? 1; b.TutarTL = m.U_TutarTL ?? Math.Round(b.Tutar * b.Kur, 2);
                if (yeni) { _belgeler.Insert(0, b); b.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = "Hazırlandı", Aciklama = $"{b.Odeyen}, {b.FaturaSahibi} adına {b.Kurum} ödemesi yapıyor · {b.TutarTL.ToString("N2", Tr)} ₺ · {Sekil(b)}" }); }
                else b.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = "Güncellendi", Aciklama = $"{b.Kurum} · {b.TutarTL.ToString("N2", Tr)} ₺ · {b.Yontem}" });
                m.U_GrupIciNo = b.No;
            }
            if (m.U_Durum == "ÖDENDİ") Muhasebelestir(m, kullanici);
            return b;
        }
        /// <summary>Mahsupta karşılıklı bakiye varsa (ödeyenin fatura sahibine borcu ≥ tutar) borçtan düşülür; yoksa fatura sahibi ödeyene borçlanır. Temlikte her zaman borçlanma.</summary>
        public static bool KarsilikliMahsup(NakitAkisViewModel m) => m.U_GrupIciTur == "Mahsup" && CekAkibetOrnek.GrupBakiye(m.U_OdeyenSirket, m.U_FaturaSahibi) >= (m.U_TutarTL ?? 0) && (m.U_TutarTL ?? 0) > 0;
        static string Sekil(GrupIciBelge b) => b.Tur == "Temlik" ? "Temlik: fatura sahibinin tedarikçi borcu ödeyen şirkete devredilir (332)" : CekAkibetOrnek.GrupBakiye(b.Odeyen, b.FaturaSahibi) >= b.TutarTL ? "Mahsup: ödeyenin fatura sahibine olan borcundan düşülür (132 / 332 karşılıklı kapanır)" : "Mahsup: karşılıklı bakiye yok, fatura sahibi ödeyene borçlanır (332)";

        /// <summary>Fatura sahibi fişinin taslağı (simülasyon / kayıt): 320 (tedarikçi grup şirketiyse 332) borç · 132 (karşılıklı mahsup) ya da 332 alacak; grup içi bakiye etkisi.</summary>
        public static (MuhasebeOrnek.FisTaslagi fis, string sekil, string bakiyeEtkisi, bool karsilikli, decimal tl) FisTaslagi(NakitAkisViewModel m, GrupIciBelge b = null)
        {
            b ??= BulSatir(m.DocEntry);
            string tur = b?.Tur ?? (Turler.Contains(m.U_GrupIciTur ?? "") ? m.U_GrupIciTur : "Mahsup"), fs = b?.FaturaSahibi ?? m.U_FaturaSahibi, od = b?.Odeyen ?? m.U_OdeyenSirket, kurum = b?.Kurum ?? m.U_Kurum ?? "", no = b?.No ?? "(kaydedilince)", yontem = b?.Yontem ?? m.U_OdemeYontemi ?? "";
            decimal tl = b != null && b.TutarTL > 0 ? b.TutarTL : (m.U_TutarTL ?? 0);
            bool karsilikli = tur == "Mahsup" && CekAkibetOrnek.GrupBakiye(od, fs) >= tl && tl > 0; bool tedarikciGrup = CekAkibetOrnek.GrupSirketleri.Contains(kurum);
            string belge = string.IsNullOrEmpty(b?.BelgeNo ?? m.U_FaturaSiparisNo) ? (b?.BelgeTipi ?? m.U_BagliBelgeTipi ?? "") : (b?.BelgeNo ?? m.U_FaturaSiparisNo);
            var satirlar = new List<YevmiyeSatiri>
            {
                new YevmiyeSatiri { HesapKodu = tedarikciGrup ? "332" : "320", HesapAdi = MuhasebeOrnek.HesapPlani[tedarikciGrup ? "332" : "320"], Borc = tl, Alacak = 0, Aciklama = $"{kurum} · {belge} · {od} tarafından ödendi ({yontem})" },
                karsilikli ? new YevmiyeSatiri { HesapKodu = "132", HesapAdi = MuhasebeOrnek.HesapPlani["132"], Borc = 0, Alacak = tl, Aciklama = $"{od} şirketinden alacaktan mahsup · {no}" }
                           : new YevmiyeSatiri { HesapKodu = "332", HesapAdi = MuhasebeOrnek.HesapPlani["332"], Borc = 0, Alacak = tl, Aciklama = (tur == "Temlik" ? "Temlik: borç " + od + " şirketine devredildi · " : "Grup içi borç: " + od + " · ") + no }
            };
            var fis = new MuhasebeOrnek.FisTaslagi { DbName = CekAkibetOrnek.DbName(fs), Kaynak = $"{tur} {no}", Aciklama = $"{od}, {fs} adına {kurum} ödemesi · {belge} · {(tur == "Temlik" ? "Grup İçi Temlik Belgesi" : "Grup İçi Mahsup Belgesi")}", Satirlar = satirlar };
            string sekil = tur == "Temlik" ? "Temlik" : karsilikli ? "Karşılıklı bakiyeden mahsup" : "Grup içi borç";
            string etki = karsilikli ? $"{od} → {fs} grup içi borcu {tl.ToString("N2", Tr)} ₺ azalır" : $"{fs} → {od} grup içi borcu {tl.ToString("N2", Tr)} ₺ artar";
            return (fis, sekil, etki, karsilikli, tl);
        }
        /// <summary>Ödeme gerçekleşince fatura sahibi şirkette fiş: 320 Satıcılar borç / 132 ya da 332 alacak; grup içi bakiye güncellenir. Ödeyen fişi satırın ödeme fişidir.</summary>
        public static GrupIciBelge Muhasebelestir(NakitAkisViewModel m, string kullanici)
        {
            if (m == null) return null; GrupIciBelge b;
            lock (Kilit) { b = _belgeler.Where(x => x.KaynakDocEntry == m.DocEntry && x.Durum != "İptal").OrderByDescending(x => x.Id).FirstOrDefault(); if (b == null || b.Durum == "Muhasebeleşti") return b; }
            var t = FisTaslagi(m, b); if (t.tl <= 0) return b;
            var f = MuhasebeOrnek.Kaydet(t.fis, kullanici);
            if (t.karsilikli) CekAkibetOrnek.GrupBakiyeEkle(b.Odeyen, b.FaturaSahibi, -t.tl); else CekAkibetOrnek.GrupBakiyeEkle(b.FaturaSahibi, b.Odeyen, t.tl);
            lock (Kilit)
            {
                b.Durum = "Muhasebeleşti"; b.MuhasebeTarihi = DateTime.Now; b.FaturaSahibiFisNo = f.No; b.OdeyenFisNo = m.U_YevmiyeNo ?? ""; b.MahsupSekli = t.sekil;
                b.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = "Muhasebeleşti", Aciklama = $"{b.FaturaSahibi} fişi {f.No} ({t.fis.Satirlar[0].HesapKodu} / {t.fis.Satirlar[1].HesapKodu}) · ödeyen fişi {(string.IsNullOrEmpty(b.OdeyenFisNo) ? "dolaşım fişleri" : b.OdeyenFisNo)} · {b.MahsupSekli} · {t.bakiyeEtkisi}" });
            }
            m.U_GrupIciNo = b.No; m.U_GrupIciTur = b.Tur;
            return b;
        }
        /// <summary>Satır silinince hazır belge iptal edilir.</summary>
        public static void Iptal(int docEntry, string kullanici) { lock (Kilit) foreach (var b in _belgeler.Where(x => x.KaynakDocEntry == docEntry && x.Durum == "Hazırlandı")) { b.Durum = "İptal"; b.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = "İptal", Aciklama = "Kaynak ödeme satırı silindi" }); } }
        /// <summary>Hazır belgenin türü değiştirilir (Mahsup ↔ Temlik); muhasebeleşmiş belge değişmez.</summary>
        public static (bool ok, string mesaj) TurDegistir(NakitAkisViewModel m, string tur, string kullanici)
        {
            if (!Turler.Contains(tur ?? "")) return (false, "Tür Mahsup ya da Temlik olmalı.");
            var b = BulSatir(m.DocEntry); if (b != null && b.Durum == "Muhasebeleşti") return (false, $"{b.No} muhasebeleşmiş; türü değiştirilemez.");
            m.U_GrupIciTur = tur; var y = Senkron(m, kullanici);
            return (true, y == null ? "Grup içi belge gerekmiyor (fatura sahibi = ödeyen)." : $"{y.No} · {y.Baslik} hazırlandı; ödeme gerçekleşince iki şirkette fiş atılır.");
        }
        public static object Json(GrupIciBelge b) => b == null ? null : new
        {
            no = b.No, tur = b.Tur, baslik = b.Baslik, tarih = b.Tarih.ToString("yyyy-MM-dd HH:mm"), kaynakDb = b.KaynakDb, docEntry = b.KaynakDocEntry, faturaSahibi = b.FaturaSahibi, faturaSahibiDb = b.FaturaSahibiDb, odeyen = b.Odeyen, odeyenDb = b.OdeyenDb,
            kurum = b.Kurum, belgeTipi = b.BelgeTipi, belgeNo = b.BelgeNo, aciklama = b.Aciklama, yontem = b.Yontem, tutar = b.Tutar, pb = b.PB, kur = b.Kur, tutarTL = b.TutarTL, durum = b.Durum, sekil = string.IsNullOrEmpty(b.MahsupSekli) ? Sekil(b) : b.MahsupSekli,
            olusturan = b.Olusturan, muhasebeTarihi = b.MuhasebeTarihi?.ToString("yyyy-MM-dd HH:mm"), faturaSahibiFisNo = b.FaturaSahibiFisNo, odeyenFisNo = b.OdeyenFisNo, link = "/Rapor79/GrupIciBelge?no=" + Uri.EscapeDataString(b.No),
            gunluk = b.Gunluk.Select(g => new { tarih = g.Tarih.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, g.Islem, g.Aciklama })
        };
    }
}
