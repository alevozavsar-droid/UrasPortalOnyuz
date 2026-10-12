using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>Talimat satırı: bir Kasa Nakit Akışı ödeme kaydı (fatura sahibi ve ödeyen şirket ayrı; çek / senet belgeleri ve dolaşım özeti ile).</summary>
    public class TalimatSatiri
    {
        public int DocEntry { get; set; } public string DbName { get; set; } public string FaturaSahibi { get; set; } public string OdeyenSirket { get; set; } public string Kurum { get; set; } public string FaturaNo { get; set; } public string Aciklama { get; set; }
        public decimal Tutar { get; set; } public string PB { get; set; } public decimal TutarTL { get; set; } public string AliciIban { get; set; } = ""; public string AliciBanka { get; set; } = ""; public string GrupIciNo { get; set; } = ""; public string AvansNo { get; set; } = "";
        public List<TalimatBelge> Belgeler { get; set; } = new List<TalimatBelge>();
    }
    public class TalimatBelge { public int CekDocEntry { get; set; } public string No { get; set; } public string Belge { get; set; } public string Banka { get; set; } public string CiroEden { get; set; } public string AsilBorclu { get; set; } public decimal Tutar { get; set; } public string PB { get; set; } public DateTime Vade { get; set; } public string Sahip { get; set; } public string Dolasim { get; set; } = ""; public int DolasimAdim { get; set; } public int DolasimToplam { get; set; } }
    public class TalimatGunluk { public DateTime Tarih { get; set; } public string Kullanici { get; set; } public string Islem { get; set; } public string Aciklama { get; set; } }

    /// <summary>
    /// ÖDEME TALİMATI — Kasa Nakit Akışı'ndan seçilen satırlardan, ödeyen şirket + ödeme yöntemi bazında hazırlanır (ana projedeki PaymentInstructionDocument /
    /// SupplierPaymentDocument / QuickTransferDocument içerikleriyle aynı dil: "Şubeniz nezdinde bulunan … hesabımızdan aşağıda bulunan işlemlerin yapılmasını rica ederiz").
    /// Durum akışı: Hazırlandı → Onaylandı → Bankaya İletildi / Teslim Edildi → Gerçekleşti (satırlar ÖDENDİ, çekler dolaşım planına göre ciro / ibraz, avanslar teslim) | İptal.
    /// </summary>
    public class OdemeTalimati
    {
        public int Id { get; set; } public string No { get; set; } public DateTime Tarih { get; set; } public string DbName { get; set; } public string Sirket { get; set; } public string Yontem { get; set; }
        public string BankaHesabi { get; set; } = ""; public string Kasa { get; set; } = ""; public string Durum { get; set; } = "Hazırlandı"; public string Hazirlayan { get; set; } public string Onaylayan { get; set; } = ""; public DateTime? OnayTarihi { get; set; } public DateTime? IletimTarihi { get; set; } public DateTime? GerceklesmeTarihi { get; set; } public string IptalNedeni { get; set; } = "";
        public List<TalimatSatiri> Satirlar { get; set; } = new List<TalimatSatiri>(); public List<TalimatGunluk> Gunluk { get; set; } = new List<TalimatGunluk>();
        public decimal ToplamTL => Satirlar.Sum(s => s.TutarTL); public decimal Toplam => Satirlar.Sum(s => s.Tutar); public string PB => Satirlar.Select(s => s.PB).Distinct().Count() == 1 ? Satirlar[0].PB : "TRY";
        public string Baslik => Yontem == "Havale / EFT" ? "Banka Ödeme Talimatı" : Yontem == "Çek" ? "Çek Teslim / Ciro Talimatı" : Yontem == "Senet" ? "Senet Teslim / Ciro Talimatı" : Yontem == "Nakit" ? "Kasa Ödeme Talimatı" : Yontem == "Kredi Kartı / Banka Kartı" ? "Kart Ödeme Talimatı" : Yontem == "Mahsup" ? "Mahsup Talimatı" : Yontem == "Temlik" ? "Temlik Talimatı" : Yontem == "Yansıtma" ? "Grup İçi Yansıtma Ödeme Talimatı" : "Ödeme Talimatı";
        public string SonrakiDurum => Durum == "Hazırlandı" ? "Onaylandı" : Durum == "Onaylandı" ? (Yontem == "Havale / EFT" || Yontem == "Kredi Kartı / Banka Kartı" ? "Bankaya İletildi" : "Teslim Edildi") : Durum == "Bankaya İletildi" || Durum == "Teslim Edildi" ? "Gerçekleşti" : null;
        public bool Kapandi => Durum == "Gerçekleşti" || Durum == "İptal";
    }

    public static class TalimatOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        private static readonly List<OdemeTalimati> _talimatlar = new List<OdemeTalimati>();
        private static int _sonId = 0;
        public static readonly string[] Durumlar = { "Hazırlandı", "Onaylandı", "Bankaya İletildi", "Teslim Edildi", "Gerçekleşti", "İptal" };
        /// <summary>Talimat başlığındaki şirket unvanı / adresi (ana projedeki belge sınıflarıyla aynı) ve konkordato komiser heyeti.</summary>
        public static readonly Dictionary<string, (string Unvan, string Adres, string Web, string[] Komiserler)> SirketBilgisi = new Dictionary<string, (string, string, string, string[])>
        {
            ["URASKIMYA"] = ("URAS KİMYA SAN. VE TİC. A.Ş.", "Çobançeşme Mah. Sanayi Cad. Gençosman 1 Sk. No:14 Yenibosna, Bahçelievler / İSTANBUL · Tel: +90 212 489 70 70", "www.uraskimya.com", new[] { "Müjgan TUNÇ YÜCEL", "Haluk AKSU", "Suat ÖZYALÇIN" }),
            ["URSMAKINE"] = ("URS MAKİNE BASKI TEKNOLOJİLERİ A.Ş.", "Yeni Bosna Merkez Mah. Çınar Cad. No:6 Ertin Plaza Bahçelievler / İSTANBUL · Tel: +90 212 489 70 70", "www.ursmakine.com", new[] { "Müjgan TUNÇ YÜCEL", "Haluk AKSU", "Suat ÖZYALÇIN" }),
            ["AVRUPA_PAPER"] = ("AVRUPA PAPER MAKİNE SAN. VE DIŞ TİC. A.Ş.", "Yakuplu Mah. Beysan San. Sit. Birlik Cad. No:3 Beylikdüzü / İSTANBUL · Tel: +90 212 489 70 70", "www.avrupapaper.com", new[] { "Müjgan TUNÇ YÜCEL", "Haluk AKSU", "Suat ÖZYALÇIN" }),
            ["ALV_KIMYA"] = ("ALV KİMYA SAN. İÇ VE DIŞ TİC. A.Ş.", "Çobançeşme Mah. Sanayi Cad. Gençosman 1 Sk. No:14 Yenibosna, Bahçelievler / İSTANBUL · Tel: +90 212 489 70 70", "www.alvkimya.com", new[] { "Özkan YILMAZ", "Engin GÜL", "Simla TURGUTLUGİL" }),
            ["DAF_KIMYA"] = ("DAF KİMYA SAN. TİC. A.Ş.", "Çobançeşme Mah. Sanayi Cad. Gençosman 1 Sk. No:14 Yenibosna, Bahçelievler / İSTANBUL · Tel: +90 212 489 70 70", "www.dafkimya.com", new[] { "Burak GÜL", "Elif AVCI" }),
            ["SELVI"] = ("SELVİ KİMYA TURİZM OTOMOTİV SANAYİ VE TİCARET LTD. ŞTİ.", "Çobançeşme Mah. Sanayi Cad. Gençosman 1 Sk. No:14 Yenibosna, Bahçelievler / İSTANBUL · Tel: +90 212 489 70 70", "www.selvikimya.com", new[] { "Serkan KORKMAZ", "Pınar TEKİN" }),
            ["URAS_HOLDING"] = ("URAS HOLDİNG A.Ş.", "Çobançeşme Mah. Sanayi Cad. Gençosman 1 Sk. No:14 Yenibosna, Bahçelievler / İSTANBUL · Tel: +90 212 489 70 70", "www.urasholding.com", new[] { "Müjgan TUNÇ YÜCEL", "Haluk AKSU" })
        };
        public static List<OdemeTalimati> Talimatlar { get { lock (Kilit) return _talimatlar.OrderByDescending(t => t.Tarih).ToList(); } }
        public static OdemeTalimati Bul(string no) { lock (Kilit) return _talimatlar.FirstOrDefault(t => string.Equals(t.No, no, StringComparison.OrdinalIgnoreCase)); }
        public static List<OdemeTalimati> Listele(string dbName) { lock (Kilit) return _talimatlar.Where(t => string.IsNullOrEmpty(dbName) || t.DbName == dbName || t.Satirlar.Any(s => s.DbName == dbName)).OrderByDescending(t => t.Tarih).ToList(); }

        /// <summary>Seçilen satırlardan talimat(lar): ödeyen şirket + yöntem bazında ayrı talimat; ÖDENDİ ya da açık talimatlı satırlar atlanır. Satır Bekliyor ise Planlandı olur.</summary>
        public static (List<OdemeTalimati> talimatlar, List<string> atlananlar) Olustur(string dbName, IEnumerable<int> docEntryler, string kullanici, string bankaHesabi = null, string kasa = null)
        {
            var sonuc = new List<OdemeTalimati>(); var atlanan = new List<string>();
            var satirlar = (docEntryler ?? Enumerable.Empty<int>()).Distinct().Select(d => NakitAkisOrnek.Bul(dbName, d) ?? NakitAkisOrnek.Bul(d)).Where(m => m != null).ToList();
            foreach (var m in satirlar.Where(x => x.U_Durum == "ÖDENDİ")) atlanan.Add($"#{m.DocEntry} {m.U_Kurum}: zaten ödendi");
            foreach (var m in satirlar.Where(x => !string.IsNullOrEmpty(x.U_TalimatNo) && x.U_Durum != "ÖDENDİ")) { var t = Bul(m.U_TalimatNo); if (t != null && !t.Kapandi) atlanan.Add($"#{m.DocEntry} {m.U_Kurum}: {m.U_TalimatNo} talimatında ({t.Durum})"); }
            var uygun = satirlar.Where(x => x.U_Durum != "ÖDENDİ" && (string.IsNullOrEmpty(x.U_TalimatNo) || (Bul(x.U_TalimatNo)?.Kapandi ?? true))).ToList();
            // finans kontrolü: satıra bağlı belge plan dışı çıkmışsa (ciro / ibraz edilmiş, karşılıksız …) talimata alınmaz; Takip Paneli'nde kontrol gerekir
            foreach (var m in uygun.ToList())
            {
                NakitAkisOrnek.BelgeleriDoldur(m);
                var cikmis = m.Belgeler.Where(b => b.Akibet != "Planlandı" && b.Akibet != "Portföyde").ToList();
                if (cikmis.Count > 0) { atlanan.Add($"#{m.DocEntry} {m.U_Kurum}: {string.Join(", ", cikmis.Select(b => $"{b.No} {b.Akibet}{(string.IsNullOrEmpty(b.IbrazYeri) ? "" : " → " + b.IbrazYeri)}"))} (belge plan dışı çıkmış; Takip Paneli'nde kontrol edin)"); uygun.Remove(m); continue; }
                // çek / senet talimatı belgesiz olmaz: önce portföyden belge seçilip planlanmalı
                if ((m.U_OdemeYontemi == "Çek" || m.U_OdemeYontemi == "Senet") && m.Belgeler.Count == 0) { atlanan.Add($"#{m.DocEntry} {m.U_Kurum}: {m.U_OdemeYontemi.ToLower(Tr)} seçilmemiş (önce Portföyden Seç)"); uygun.Remove(m); }
            }
            lock (Kilit)
            {
                // banka talimatı para birimi hesabına göre ayrı yazılır (TL hesabı / USD hesabı); diğer yöntemlerde para birimi gruplamaya girmez
                foreach (var g in uygun.GroupBy(x => (Odeyen: string.IsNullOrEmpty(x.U_OdeyenSirket) ? NakitAkisOrnek.SirketAdi(x.U_KaynakFirma) : x.U_OdeyenSirket, Yontem: x.U_OdemeYontemi ?? "Diğer", PB: x.U_OdemeYontemi == "Havale / EFT" || x.U_OdemeYontemi == "Kredi Kartı / Banka Kartı" ? (x.U_ParaBirimi ?? "TRY") : "")))
                {
                    var t = new OdemeTalimati { Id = ++_sonId, No = $"TLM-{DateTime.Today.Year}-{_sonId:0000}", Tarih = DateTime.Now, Sirket = g.Key.Odeyen, DbName = CekAkibetOrnek.DbName(g.Key.Odeyen), Yontem = g.Key.Yontem, Hazirlayan = kullanici ?? "" };
                    t.BankaHesabi = g.Key.Yontem == "Havale / EFT" || g.Key.Yontem == "Kredi Kartı / Banka Kartı" ? (bankaHesabi ?? g.Select(x => x.U_BankaHesabi).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? NakitAkisOrnek.BankaHesaplari[0]) : "";
                    t.Kasa = g.Key.Yontem == "Nakit" ? (kasa ?? g.Select(x => x.U_Kasa).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? NakitAkisOrnek.Kasalar[0]) : "";
                    foreach (var m in g)
                    {
                        NakitAkisOrnek.BelgeleriDoldur(m);
                        var s = new TalimatSatiri { DocEntry = m.DocEntry, DbName = m.U_KaynakFirma, FaturaSahibi = string.IsNullOrEmpty(m.U_FaturaSahibi) ? NakitAkisOrnek.SirketAdi(m.U_KaynakFirma) : m.U_FaturaSahibi, OdeyenSirket = g.Key.Odeyen, Kurum = m.U_Kurum, FaturaNo = m.U_FaturaSiparisNo ?? "", Aciklama = AciklamaUret(m), Tutar = m.U_Tutar ?? 0, PB = m.U_ParaBirimi ?? "TRY", TutarTL = m.U_TutarTL ?? 0, AliciIban = m.U_AliciIban ?? "", GrupIciNo = m.U_MahsupFisNo + m.U_TemlikNo + m.U_YansitmaNo, AvansNo = m.U_AvansNo ?? "" };
                        foreach (var b in m.Belgeler) { var k = CekAkibetOrnek.Bul(b.CekDocEntry); s.Belgeler.Add(new TalimatBelge { CekDocEntry = b.CekDocEntry, No = b.No, Belge = b.Belge, Banka = b.Banka, CiroEden = b.KimdenGeldi, AsilBorclu = b.AsilBorclu, Tutar = b.Tutar, PB = b.PB, Vade = b.Vade, Sahip = k?.Sirket ?? "", Dolasim = k?.DolasimOzeti ?? "", DolasimAdim = k?.GerceklesenAdim ?? 0, DolasimToplam = k?.Dolasim.Count ?? 0 }); }
                        t.Satirlar.Add(s); m.U_TalimatNo = t.No; if (m.U_Durum == "Bekliyor") { m.U_Durum = "Planlandı"; m.U_EskiDurum = "Planlandı"; }
                        NakitAkisOrnek.Gunluge(m.U_KaynakFirma, m.DocEntry, "Talimat", $"{t.No} ({t.Baslik}) hazırlandı · ödeyen {t.Sirket}", kullanici);
                    }
                    t.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = "Hazırlandı", Aciklama = $"{t.Satirlar.Count} satır · {(t.ToplamTL).ToString("N2", Tr)} ₺" });
                    _talimatlar.Add(t); sonuc.Add(t);
                }
            }
            return (sonuc, atlanan);
        }
        static string AciklamaUret(NakitAkisViewModel m)
        {
            string belge = !string.IsNullOrEmpty(m.U_FaturaSiparisNo) ? $"{m.U_FaturaSiparisNo} NOLU {(m.U_BagliBelgeTipi == "Sipariş" ? "SİPARİŞ" : m.U_BagliBelgeTipi == "Avans" ? "AVANS" : m.U_BagliBelgeTipi == "Mahsup" || m.U_BagliBelgeTipi == "Temlik" || m.U_BagliBelgeTipi == "Yansıtma" ? m.U_BagliBelgeTipi.ToUpper(CultureInfo.GetCultureInfo("tr-TR")) : "FATURA")} ÖDEMESİ" : $"{(m.U_UrunHizmet ?? "ÖDEME").ToUpper(CultureInfo.GetCultureInfo("tr-TR"))}";
            return belge + (string.IsNullOrEmpty(m.U_FaturaSahibi) || m.U_FaturaSahibi == m.U_OdeyenSirket ? "" : $" ({m.U_FaturaSahibi} ADINA)");
        }
        /// <summary>Durum ilerletme / iptal. Gerçekleşti: satırlar ÖDENDİ, belgeler ciro / ibraz, avanslar teslim (NakitAkisOrnek.OdendiIsaretle).</summary>
        public static (bool ok, string mesaj) DurumDegistir(string no, string yeniDurum, string not, string kullanici)
        {
            var t = Bul(no); if (t == null) return (false, "Talimat bulunamadı.");
            if (t.Kapandi) return (false, $"{t.No} kapanmış ({t.Durum}).");
            if (yeniDurum == "İptal")
            {
                lock (Kilit) { t.Durum = "İptal"; t.IptalNedeni = not ?? ""; t.Gunluk.Add(new TalimatGunluk { Tarih = DateTime.Now, Kullanici = kullanici ?? "", Islem = "İptal", Aciklama = not ?? "" }); }
                foreach (var s in t.Satirlar) { var m = NakitAkisOrnek.Bul(s.DbName, s.DocEntry); if (m != null && m.U_TalimatNo == t.No) m.U_TalimatNo = ""; NakitAkisOrnek.Gunluge(s.DbName, s.DocEntry, "Talimat", $"{t.No} iptal edildi" + (string.IsNullOrEmpty(not) ? "" : " · " + not), kullanici); }
                return (true, $"{t.No} iptal edildi; satırlar yeniden talimata alınabilir.");
            }
            string beklenen = t.SonrakiDurum; if (yeniDurum != beklenen) return (false, $"{t.Durum} durumundan {yeniDurum} geçilemez; sıradaki adım: {beklenen}.");
            lock (Kilit)
            {
                t.Durum = yeniDurum; var z = DateTime.Now;
                if (yeniDurum == "Onaylandı") { t.Onaylayan = kullanici ?? ""; t.OnayTarihi = z; } else if (yeniDurum == "Bankaya İletildi" || yeniDurum == "Teslim Edildi") t.IletimTarihi = z; else if (yeniDurum == "Gerçekleşti") t.GerceklesmeTarihi = z;
                t.Gunluk.Add(new TalimatGunluk { Tarih = z, Kullanici = kullanici ?? "", Islem = yeniDurum, Aciklama = not ?? "" });
            }
            if (yeniDurum == "Gerçekleşti") foreach (var s in t.Satirlar) NakitAkisOrnek.OdendiIsaretle(s.DbName, s.DocEntry, t.No, kullanici);
            else foreach (var s in t.Satirlar) NakitAkisOrnek.Gunluge(s.DbName, s.DocEntry, "Talimat", $"{t.No} → {yeniDurum}", kullanici);
            return (true, yeniDurum == "Gerçekleşti" ? $"{t.No} gerçekleşti: {t.Satirlar.Count} satır ÖDENDİ" + (t.Satirlar.Any(s => s.Belgeler.Count > 0) ? "; belgeler dolaşım planına göre ciro / ibraz edildi (Çek Akıbeti ve Takip Paneli güncellendi)" : "") + (t.Satirlar.Any(s => s.AvansNo != "") ? "; avanslar teslim edildi" : "") + "." : $"{t.No} → {yeniDurum}.");
        }
    }
}
