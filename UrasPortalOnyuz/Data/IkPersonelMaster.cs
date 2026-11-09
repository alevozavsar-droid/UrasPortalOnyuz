// ============================================================
// İK PERSONEL ANA LİSTESİ (9 Kas 2026, kullanıcı: "İnsan Kaynakları alt menüsünde bulunan tüm raporlar Konsolide İK Paneli ile senkronize olmalı")
//   İnsan Kaynakları alt menüsündeki ekranlar — Personel Carileri (CariKart/Personel), Personel Dosyası, Ekip İzin Takvimi / İzin Takip Raporu (IzinOrnek),
//   Mesai Takip Raporu (TalepMotoru MESAI) ve ana sayfadaki Yaklaşan İzinler kartı — personel listesini BURADAN, yani Konsolide İK Paneli'nin
//   (Data/PersonelMaliyetOrnek.cs) kişi kayıtlarından alır. Ayrı bir personel listesi tutulmaz; panelde kişi eklenince / değişince hepsi aynı anda değişir.
//   Kişi anahtarı cari kodu (335.xxx = PmPersonel.Sira; talep modüllerinde NaKisi.PersonelNo). Kimlik / iletişim alanı (telefon, e-posta, işe giriş)
//   Personel Dosyası'ndan değiştirilirse KimlikYaz ile kişinin TÜM dönemlerdeki kaydına yazılır ve kalıcı günlüğe "kimlik" satırı düşer (PersonelMaliyetKalici).
//   NakitAvansOrnek.Personel (P1001… örnek kullanıcılar) talep / onay akışlarının rol listesi olarak kalır; İK ekranlarında listelenmez.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public static class IkPersonelMaster
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

        /// <summary>Konsolide İK Paneli kişileri: her sıra (cari kodu) için EN YENİ dönemdeki kayıt (Ekim'de işe girenler dahil; Durum ve kimlik en yeni dönemden). Sıra sırasıyla.</summary>
        public static List<PmPersonel> Kisiler()
        {
            lock (PersonelMaliyetOrnek.Kilit)
            {
                var d = new Dictionary<int, PmPersonel>();
                foreach (var donem in PersonelMaliyetOrnek.Donemler.Keys.OrderByDescending(k => k, StringComparer.Ordinal))
                    foreach (var p in PersonelMaliyetOrnek.Donemler[donem]) if (!d.ContainsKey(p.Sira)) d[p.Sira] = p;
                return d.Values.OrderBy(p => p.Sira).ToList();
            }
        }
        /// <summary>Durumu Aktif olan kişiler (bordrolu + özel + SGK'lı gündelikçi).</summary>
        public static List<PmPersonel> Aktifler() => Kisiler().Where(p => p.Durum == "Aktif").ToList();
        /// <summary>Talep modüllerinin kişi tipi (NaKisi; PersonelNo = cari kodu 335.xxx, Sirket = master şirket adı, Departman = Bölüm, Unvan = Görev).</summary>
        public static List<NaKisi> NaKisiler() => Kisiler().Select(PersonelKesintiOrnek.NaKisi).ToList();
        /// <summary>Cari kodu (335.xxx), ad soyad ya da sicil no ile kişi; yoksa null.</summary>
        public static PmPersonel Bul(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null; s = s.Trim();
            return Kisiler().FirstOrDefault(p => p.CariKodu == s || string.Equals(p.AdSoyad, s, StringComparison.OrdinalIgnoreCase) || (!string.IsNullOrEmpty(p.SicilNo) && string.Equals(p.SicilNo, s, StringComparison.OrdinalIgnoreCase)));
        }
        public static PmPersonel Bul(int sira) => Kisiler().FirstOrDefault(p => p.Sira == sira);
        /// <summary>Fiili görev yerinden il (cari kartı Şehir alanı): Çorlu → Tekirdağ, diğerleri İstanbul.</summary>
        public static string Il(string fiiliGorevYeri) => (fiiliGorevYeri ?? "").IndexOf("Çorlu", StringComparison.OrdinalIgnoreCase) >= 0 ? "Tekirdağ" : "İstanbul";
        /// <summary>Konsolide İK Paneli › Personel Cari Kartı derin bağlantısı (kişi kartı açık).</summary>
        public static string PanelBaglanti(PmPersonel p) => $"/PersonelMaliyet?sekme=cari&kisi={p.Sira}";

        /// <summary>Kimlik / iletişim yazımı (Personel Dosyası'ndan): null alan değişmez; boş metin alanı siler; IseGiris yyyy-MM-dd (boş = tarih yok).</summary>
        public class KimlikIstek { public int Sira { get; set; } public string Telefon { get; set; } public string Eposta { get; set; } public string IseGiris { get; set; } public string TcKimlik { get; set; } }
        /// <summary>Kişinin TÜM dönemlerdeki kayıtlarına kimlik / iletişim alanlarını yazar (kişi düzeyinde bilgi; Konsolide İK Paneli'nin tüm sekmeleri ve kişi kartı aynı anda değişir). Hata metni döner (null = başarılı).</summary>
        public static string KimlikYaz(KimlikIstek d, string kisi)
        {
            if (d == null || d.Sira <= 0) return "Personel bulunamadı.";
            if (!string.IsNullOrEmpty(d.Eposta) && !d.Eposta.Contains("@")) return "E-posta biçimi geçersiz.";
            DateTime? giris = null; bool girisVar = d.IseGiris != null;
            if (girisVar && d.IseGiris.Trim().Length > 0) { if (!DateTime.TryParse(d.IseGiris, CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) && !DateTime.TryParse(d.IseGiris, Tr, DateTimeStyles.None, out t)) return "İşe giriş tarihi geçersiz."; giris = t.Date; }
            lock (PersonelMaliyetOrnek.Kilit)
            {
                int n = 0;
                foreach (var liste in PersonelMaliyetOrnek.Donemler.Values)
                    foreach (var p in liste.Where(x => x.Sira == d.Sira))
                    {
                        if (d.Telefon != null) p.Telefon = d.Telefon.Trim();
                        if (d.Eposta != null) p.Eposta = d.Eposta.Trim();
                        if (d.TcKimlik != null) p.TcKimlik = d.TcKimlik.Trim();
                        if (girisVar) p.IseGiris = giris;
                        n++;
                    }
                return n == 0 ? "Personel bulunamadı." : null;
            }
        }
    }
}
