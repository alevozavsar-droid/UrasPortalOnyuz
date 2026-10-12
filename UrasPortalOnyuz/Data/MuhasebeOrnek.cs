using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>Yevmiye fişi satırı (tek düzen hesap planı kodu + adı, borç / alacak TL).</summary>
    public class YevmiyeSatiri { public string HesapKodu { get; set; } public string HesapAdi { get; set; } public decimal Borc { get; set; } public decimal Alacak { get; set; } public string Aciklama { get; set; } = ""; }
    /// <summary>Ödeme muhasebe fişi: Kasa Nakit Akışı'ndan makbuz kesilince, talimat gerçekleşince ya da çek ciro / ibraz adımı gerçekleşince otomatik atılır.</summary>
    public class YevmiyeFisi
    {
        public int Id { get; set; } public string No { get; set; } public DateTime Tarih { get; set; } public string DbName { get; set; } public string Sirket { get; set; }
        public string Kaynak { get; set; } = ""; public string Aciklama { get; set; } = ""; public string Kullanici { get; set; } = ""; public string Durum { get; set; } = "Kaydedildi";
        public List<YevmiyeSatiri> Satirlar { get; set; } = new List<YevmiyeSatiri>();
        public decimal ToplamBorc => Satirlar.Sum(s => s.Borc); public decimal ToplamAlacak => Satirlar.Sum(s => s.Alacak);
    }

    /// <summary>
    /// Bellek içi muhasebe fişleri (ön yüz örneği). Kasa Nakit Akışı ödemeleri (Data/NakitAkisOrnek.cs) ÖDENDİ olunca yönteme ve karşı tarafa göre fiş üretilir:
    /// tedarikçi 320 Satıcılar, grup şirketi 332 İştiraklere Borçlar, nakit avans 196 Personel Avansları borç; karşılığı yönteme göre 100 Kasa / 102 Bankalar / 300 Kredi Kartı /
    /// 101 Alınan Çekler / 121 Alacak Senetleri / 120 Alıcılar (mahsup, temlik, yansıtma). Grup içi çek dolaşım adımlarında ciro eden ve alan şirket için ayrı fiş.
    /// Kilit sırası: diğer modüller → Muhasebe (yaprak; Muhasebe başka kilide girmez).
    /// </summary>
    public static class MuhasebeOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly List<YevmiyeFisi> _fisler = new List<YevmiyeFisi>();
        static int _sonId;
        public static readonly Dictionary<string, string> HesapPlani = new Dictionary<string, string>
        {
            ["100"] = "Kasa", ["101"] = "Alınan Çekler", ["102"] = "Bankalar", ["103"] = "Verilen Çekler ve Ödeme Emirleri", ["120"] = "Alıcılar", ["121"] = "Alacak Senetleri",
            ["132"] = "İştiraklerden Alacaklar (Grup İçi)", ["196"] = "Personel Avansları", ["300"] = "Banka Kredileri (Kredi Kartı)", ["320"] = "Satıcılar", ["332"] = "İştiraklere Borçlar (Grup İçi)", ["770"] = "Genel Yönetim Giderleri"
        };
        static YevmiyeSatiri S(string kod, decimal borc, decimal alacak, string aciklama) => new YevmiyeSatiri { HesapKodu = kod, HesapAdi = HesapPlani.TryGetValue(kod, out var ad) ? ad : kod, Borc = Math.Round(borc, 2), Alacak = Math.Round(alacak, 2), Aciklama = aciklama ?? "" };

        public static List<YevmiyeFisi> Listele(string dbName) { lock (Kilit) return _fisler.Where(f => string.IsNullOrEmpty(dbName) || f.DbName == dbName).OrderByDescending(f => f.Tarih).ToList(); }
        public static YevmiyeFisi Bul(string no) { lock (Kilit) return _fisler.FirstOrDefault(f => f.No == no); }
        public static YevmiyeFisi Olustur(string dbName, string kaynak, string aciklama, IEnumerable<YevmiyeSatiri> satirlar, string kullanici, DateTime? tarih = null)
        {
            lock (Kilit)
            {
                var f = new YevmiyeFisi { Id = ++_sonId, No = $"YEV-{DateTime.Today.Year}-{_sonId:000000}", Tarih = tarih ?? DateTime.Now, DbName = dbName, Sirket = CekAkibetOrnek.SirketAdi(dbName), Kaynak = kaynak ?? "", Aciklama = aciklama ?? "", Kullanici = kullanici ?? "", Satirlar = satirlar.ToList() };
                _fisler.Add(f); return f;
            }
        }
        /// <summary>Ödeme satırının fişi: borç tarafı karşı taraf (satıcı / grup şirketi / personel avansı), alacak tarafı ödeme aracı.</summary>
        public static YevmiyeFisi OdemeFisi(NakitAkisViewModel m, string kaynak, string kullanici)
        {
            if (m == null) return null; decimal tl = m.U_TutarTL ?? Math.Round((m.U_Tutar ?? 0) * (m.U_Kur ?? 1), 2); if (tl <= 0) return null;
            string y = m.U_OdemeYontemi ?? "", belge = string.IsNullOrEmpty(m.U_FaturaSiparisNo) ? (m.U_BagliBelgeTipi ?? "") : m.U_FaturaSiparisNo;
            string doviz = m.U_ParaBirimi == "TRY" || string.IsNullOrEmpty(m.U_ParaBirimi) ? "" : $" ({(m.U_Tutar ?? 0).ToString("N2", Tr)} {m.U_ParaBirimi} × {(m.U_Kur ?? 1).ToString("N4", Tr)})";
            bool grupIci = CekAkibetOrnek.GrupSirketleri.Contains(m.U_Kurum ?? "");
            string borcHesap = !string.IsNullOrEmpty(m.U_AvansNo) ? "196" : grupIci ? "332" : "320";
            string alacakHesap = y == "Nakit" ? "100" : y == "Havale / EFT" ? "102" : y == "Kredi Kartı / Banka Kartı" ? "300" : y == "Çek" ? "101" : y == "Senet" ? "121" : y == "Mahsup" || y == "Temlik" || y == "Yansıtma" ? "120" : "102";
            string aciklama = $"{m.U_Kurum} · {belge} · {y}{doviz}" + (string.IsNullOrEmpty(m.U_FaturaSahibi) || m.U_FaturaSahibi == m.U_OdeyenSirket ? "" : $" · {m.U_FaturaSahibi} adına");
            var satirlar = new List<YevmiyeSatiri> { S(borcHesap, tl, 0, (borcHesap == "196" ? "Nakit avans " + m.U_AvansNo + " · " : "") + m.U_Kurum), S(alacakHesap, 0, tl, y == "Nakit" ? (m.U_Kasa ?? "Kasa") + (string.IsNullOrEmpty(m.U_MakbuzNo) ? "" : " · " + m.U_MakbuzNo) : y == "Havale / EFT" ? (m.U_BankaHesabi ?? "Banka") : y == "Çek" || y == "Senet" ? "Ciro: " + m.U_CekNolar : y == "Kredi Kartı / Banka Kartı" ? (m.U_Kart ?? "Kart") : y) };
            // fatura sahibi ödeyenden farklıysa (grup içi ödeme): ödeyen şirkette borç tarafı grup şirketinden alacak olur
            if (!string.IsNullOrEmpty(m.U_FaturaSahibi) && !string.IsNullOrEmpty(m.U_OdeyenSirket) && m.U_FaturaSahibi != m.U_OdeyenSirket && string.IsNullOrEmpty(m.U_AvansNo)) satirlar[0] = S("132", tl, 0, $"{m.U_FaturaSahibi} adına ödeme · {m.U_Kurum}");
            return Olustur(m.U_KaynakFirma, kaynak, aciklama, satirlar, kullanici);
        }
        /// <summary>Çek ciro / ibraz adımı fişi. Grup içi ciro: eden şirkette 332 borç / 101 alacak, alan şirkette 101 borç / 132 alacak. Tedarikçiye ciro: 320 / 101. Bankaya ibraz: 102 / 101.</summary>
        public static List<YevmiyeFisi> CiroFisi(string edenDb, string alanDb, string eden, string alan, string tur, string belgeNo, string cekNo, decimal tutar, string kullanici)
        {
            var l = new List<YevmiyeFisi>(); string a = $"{cekNo} · {belgeNo} · {eden} → {alan}";
            if (tur == "Grup İçi Ciro")
            {
                l.Add(Olustur(edenDb, "Çek Ciro " + belgeNo, a, new[] { S("332", tutar, 0, "Grup içi borç kapama: " + alan), S("101", 0, tutar, cekNo + " ciro") }, kullanici));
                if (!string.IsNullOrEmpty(alanDb)) l.Add(Olustur(alanDb, "Çek Giriş " + belgeNo, a, new[] { S("101", tutar, 0, cekNo + " portföye giriş"), S("132", 0, tutar, "Grup içi alacak kapama: " + eden) }, kullanici));
            }
            else if (tur == "Bankaya İbraz") l.Add(Olustur(edenDb, "Çek İbraz " + belgeNo, a, new[] { S("102", tutar, 0, "Tahsil: " + alan), S("101", 0, tutar, cekNo + " bankaya ibraz") }, kullanici));
            else l.Add(Olustur(edenDb, "Çek Ciro " + belgeNo, a, new[] { S("320", tutar, 0, "Tedarikçi: " + alan), S("101", 0, tutar, cekNo + " ciro") }, kullanici));
            return l;
        }
        public static object Json(YevmiyeFisi f) => f == null ? null : new { no = f.No, tarih = f.Tarih.ToString("yyyy-MM-dd HH:mm"), sirket = f.Sirket, kaynak = f.Kaynak, aciklama = f.Aciklama, kullanici = f.Kullanici, durum = f.Durum, toplamBorc = f.ToplamBorc, toplamAlacak = f.ToplamAlacak, satirlar = f.Satirlar.Select(s => new { hesapKodu = s.HesapKodu, hesapAdi = s.HesapAdi, borc = s.Borc, alacak = s.Alacak, aciklama = s.Aciklama }) };
    }
}
