// ============================================================
// TALEP MOTORU — Mesai, Belge, Seyahat ve Araç talepleri için ortak çekirdek
// Nakit Avans ve İzin modülleriyle aynı mimari: tek Talep No, parametrik onay akışı, üç ayrı statü
// (Talep Durumu = workflow; 2. ve 3. statü modüle özel kaynak sistemden hesaplanır), kaynak sistem olayları
// (Transaction ID ile idempotent), timeline, audit log, revizyon, iptal, takip raporu ve gösterge kutucukları.
// Modül tanımları Data/TalepModulleri.cs içindedir; ekran Views/IkTalep/Modul.cshtml + js/talep-modulu.js.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class TmAlan
    {
        public string Ad { get; set; }
        public string Etiket { get; set; }
        public string Tip { get; set; } = "text";       // text | date | time | number | select | textarea | checkbox | personel | multiselect
        public string[] Secenekler { get; set; }
        public bool Zorunlu { get; set; }
        public int Col { get; set; } = 4;
        public string Ipucu { get; set; }
        public string Varsayilan { get; set; }
        public string Grup { get; set; } = "detay";      // ust | detay
    }

    public class TmTur { public string Kod { get; set; } public string Ad { get; set; } public string Akis { get; set; } public string Aciklama { get; set; } public string Ek { get; set; } }

    /// <summary>Kaynak sistem olayı tanımı. Girdiler: "anahtar|Etiket|tip" (tip: text | number | date | time | select:a;b;c)</summary>
    public class TmOlay { public string Kod { get; set; } public string Ad { get; set; } public string Kaynak { get; set; } public string Aciklama { get; set; } public string[] Girdiler { get; set; } = new string[0]; }

    public class TmKpi { public string Renk { get; set; } public string Baslik { get; set; } public string Deger { get; set; } public string Alt { get; set; } public string Etiket { get; set; } }

    public class TmSonuc { public string Hata { get; set; } public List<string> Uyarilar { get; set; } = new List<string>(); public List<string> EkAdimlar { get; set; } = new List<string>(); }

    public class TmTalep
    {
        public int Id { get; set; }
        public string No { get; set; }
        public string ModulKod { get; set; }
        public DateTime Tarih { get; set; }
        public NaKisi Olusturan { get; set; }
        public NaKisi Kullanan { get; set; }
        public string TurKod { get; set; }
        public Dictionary<string, string> Degerler { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> Hesap { get; set; } = new Dictionary<string, string>();     // sistem hesabı (kilitli)
        public Dictionary<string, string> Ek { get; set; } = new Dictionary<string, string>();        // kaynak sistemden gelen (kilitli)
        public List<NaOnay> Onaylar { get; set; } = new List<NaOnay>();
        public List<NaHareket> Zaman { get; set; } = new List<NaHareket>();
        public List<NaAudit> Audit { get; set; } = new List<NaAudit>();
        public List<NaEk> Ekler { get; set; } = new List<NaEk>();
        public int Revizyon { get; set; }
        public bool Iptal { get; set; }
        public string IptalNedeni { get; set; }
        public List<string> Uyarilar { get; set; } = new List<string>();
        public DateTime? SonSenkron { get; set; }
        public string EntegrasyonDurumu { get; set; } = "Başarılı";

        public TmModul Modul => TalepMotoru.Modul(ModulKod);
        public TmTur Tur => Modul?.Turler.FirstOrDefault(t => t.Kod == TurKod);
        public string V(string ad) => Degerler.TryGetValue(ad, out var v) ? (v ?? "") : "";
        public string E(string ad) => Ek.TryGetValue(ad, out var v) ? (v ?? "") : "";
        public decimal D(string ad, bool ek = false) => decimal.TryParse(ek ? E(ad) : V(ad), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
        public DateTime? T(string ad, bool ek = false) => DateTime.TryParse(ek ? E(ad) : V(ad), out var d) ? d : (DateTime?)null;
        public string TalepDurumu
        {
            get
            {
                if (Iptal) return "İptal Edildi";
                if (Onaylar.Any(o => o.Durum == "Reddedildi")) return "Reddedildi";
                if (Onaylar.Any(o => o.Durum == "Revizyon")) return "Revizyon Bekliyor";
                var b = Onaylar.FirstOrDefault(o => o.Durum == "Onay Bekliyor");
                return b != null ? b.Adim + " Bekliyor" : "Onaylandı";
            }
        }
        public bool Onaylandi => TalepDurumu == "Onaylandı";
        public string Durum2 => Iptal ? "İptal" : !Onaylandi ? "—" : Modul.Durum2(this);
        public string Durum3 => Iptal || !Onaylandi ? "—" : Modul.Durum3(this);
        public string Ozet => Modul.Ozet(this);
        public List<string> Etiketler => Modul.Etiketle(this);
        public DateTime? OnayTarihi => Onaylandi ? Onaylar.Where(o => o.Tarih.HasValue).Select(o => o.Tarih).Max() : null;
        public string Onayci(string adim) => Onaylar.FirstOrDefault(o => o.Adim == adim)?.Kisi;
    }

    public class TmModul
    {
        public string Kod { get; set; }
        public string Ad { get; set; }
        public string NoOnEk { get; set; }
        public string Ikon { get; set; }
        public string Renk { get; set; }
        public string Aciklama { get; set; }
        public string AkisOzeti { get; set; }
        public string KullananEtiket { get; set; } = "İlgili personel";
        public string Durum2Ad { get; set; }
        public string Durum3Ad { get; set; }
        public string IkinciKisiAlan { get; set; }              // değeri personel no olan alan (sürücü, vekil…)
        public string IkinciKisiEtiket { get; set; }
        public string TakvimSatir { get; set; }                 // kisi | arac | null
        public string TakvimBas { get; set; }
        public string TakvimBit { get; set; }
        public string TakvimAd { get; set; } = "Takvim";
        public List<TmTur> Turler { get; set; } = new List<TmTur>();
        public List<TmAlan> Alanlar { get; set; } = new List<TmAlan>();
        public List<TmOlay> Olaylar { get; set; } = new List<TmOlay>();
        public Dictionary<string, string> HesapEtiketler { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> EkEtiketler { get; set; } = new Dictionary<string, string>();
        public List<(string Baslik, string Anahtar)> TakipSutunlar { get; set; } = new List<(string, string)>();
        public Dictionary<string, string> Parametreler { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> ParametreEtiketler { get; set; } = new Dictionary<string, string>();
        public List<string> Kurallar { get; set; } = new List<string>();                              // ekranda "Kritik Kurallar" listesi
        public Dictionary<string, object> Master { get; set; } = new Dictionary<string, object>();     // istemciye giden ek master veri (araç filosu vb.)
        public Func<TmTalep, TmSonuc> Hesapla { get; set; }
        public Func<TmTalep, string> Durum2 { get; set; }
        public Func<TmTalep, string> Durum3 { get; set; }
        public Func<TmTalep, string, Dictionary<string, string>, string> Olay { get; set; }
        public Func<TmTalep, string> Ozet { get; set; }
        public Func<List<TmTalep>, List<TmKpi>> Kpiler { get; set; }
        public Func<TmTalep, List<string>> Etiketle { get; set; } = t => new List<string>();
        public decimal P(string ad) => decimal.TryParse(Parametreler.TryGetValue(ad, out var v) ? v : "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
        public string PS(string ad) => Parametreler.TryGetValue(ad, out var v) ? v : "";
    }

    public static class TalepMotoru
    {
        public static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        public static readonly object Kilit = new object();
        public static readonly Dictionary<string, TmModul> Moduller = new Dictionary<string, TmModul>(StringComparer.OrdinalIgnoreCase);
        public static readonly List<TmTalep> Talepler = new List<TmTalep>();
        public static readonly HashSet<string> IslenenIslemler = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static int _sonId = 0;
        public static TmModul Modul(string kod) => Moduller.TryGetValue(kod ?? "", out var m) ? m : null;
        public static NaKisi KisiBul(string s) => NakitAvansOrnek.KisiBul(s);

        static TalepMotoru() { TalepModulleri.Kaydet(); }

        public static void Hareket(TmTalep a, DateTime t, string h, string k) => a.Zaman.Add(new NaHareket { Tarih = t, Hareket = h, Kaynak = k });
        public static void Log(TmTalep a, DateTime t, string tablo, string alan, string eski, string yeni, string kul, string islem, string kaynak) => a.Audit.Add(new NaAudit { Tarih = t, Tablo = tablo, Alan = alan, Eski = eski, Yeni = yeni, Kullanici = kul, IslemTipi = islem, KaynakSistem = kaynak });

        public static NaKisi AdimKisisi(string kod, NaKisi k)
        {
            var P = NakitAvansOrnek.Personel;
            switch (kod)
            {
                case "Y": return NakitAvansOrnek.Yonetici(k);
                case "D": return P.First(p => p.Unvan == "Genel Müdür Yardımcısı");
                case "I": return P.First(p => p.Rol == "İK");
                case "A": return P.First(p => p.Departman == "İdari İşler");
                case "F": return NakitAvansOrnek.Finans();
                case "M": return P.First(p => p.Rol == "Muhasebe" && p.Unvan.Contains("Şef"));
                case "H": return P.First(p => p.Rol == "Hukuk");
                default: return NakitAvansOrnek.Yonetici(k);
            }
        }
        public static string AdimAdi(string kod) => kod == "Y" ? "Yönetici Onayı" : kod == "D" ? "Direktör Onayı" : kod == "I" ? "İK Onayı" : kod == "A" ? "İdari İşler Onayı" : kod == "F" ? "Finans Onayı" : kod == "M" ? "Muhasebe Onayı" : kod == "H" ? "Hukuk Onayı" : "Onay";

        public static List<NaOnay> OnayAkisiOlustur(TmTalep a, DateTime t, List<string> ekAdimlar)
        {
            var adimlar = new List<string>((a.Tur?.Akis ?? "Y").Split(','));
            foreach (var e in ekAdimlar) if (!adimlar.Contains(e)) adimlar.Add(e);
            // sıralama: Y, D, A, I, F
            var sira = new[] { "Y", "M", "H", "D", "A", "I", "F" }; adimlar = adimlar.Distinct().OrderBy(x => Array.IndexOf(sira, x)).ToList();
            var liste = new List<NaOnay> { new NaOnay { Sira = 1, Adim = "Talep Oluşturma", Kisi = a.Olusturan.AdSoyad, Durum = "Tamamlandı", Tarih = t } };
            foreach (var ad in adimlar) liste.Add(new NaOnay { Sira = liste.Count + 1, Adim = AdimAdi(ad), Kisi = AdimKisisi(ad, a.Kullanan).AdSoyad, Durum = liste.Count == 1 ? "Onay Bekliyor" : "Pasif" });
            return liste;
        }

        /// <summary>Formdan gelen değerleri doğrular, hesaplar, akışı kurar; kaydetmeden döner (önizleme) ya da kaydeder.</summary>
        public static TmSonuc Hazirla(TmTalep a, bool onizleme = false)
        {
            var m = a.Modul; var s = new TmSonuc();
            if (m == null) { s.Hata = "Modül tanımsız."; return s; }
            if (a.Kullanan == null) { s.Hata = "İlgili personel seçilmelidir."; return s; }
            if (!a.Kullanan.Aktif) { s.Hata = "Aktif olmayan personel için talep açılamaz."; return s; }
            if (a.Tur == null) { s.Hata = "Talep türü seçilmelidir."; return s; }
            var eksik = m.Alanlar.Where(x => x.Zorunlu && string.IsNullOrWhiteSpace(a.V(x.Ad))).Select(x => x.Etiket).ToList();
            if (eksik.Count > 0 && !onizleme) { s.Hata = "Zorunlu alanlar boş: " + string.Join(", ", eksik); return s; }
            TmSonuc r; try { r = m.Hesapla(a); } catch (Exception ex) when (onizleme) { r = new TmSonuc { Hata = "Hesap için eksik alan: " + string.Join(", ", eksik.DefaultIfEmpty(ex.Message)) }; }
            if (eksik.Count > 0 && r.Hata == null) r.Uyarilar.Insert(0, "Zorunlu alanlar boş: " + string.Join(", ", eksik));
            return r;
        }

        public static string Olustur(TmTalep a, out TmTalep kayit)
        {
            kayit = null;
            var s = Hazirla(a); if (s.Hata != null) return s.Hata;
            var t = DateTime.Now;
            lock (Kilit) { a.Id = ++_sonId; a.No = $"{a.Modul.NoOnEk}-{t.Year}-{(_sonId + 300):000000}"; }
            a.Tarih = t; a.Uyarilar = s.Uyarilar; a.Onaylar = OnayAkisiOlustur(a, t, s.EkAdimlar);
            Hareket(a, t, $"Talep oluşturuldu ({a.Tur.Ad}) — {a.Ozet}", a.Olusturan.AdSoyad);
            Log(a, t, a.Modul.NoOnEk + "_REQUEST", "REQUEST_STATUS", "", a.TalepDurumu, a.Olusturan.KullaniciId, "Insert", "Portal");
            a.SonSenkron = t;
            lock (Kilit) Talepler.Add(a);
            kayit = a; return null;
        }

        public static string OnayEylemi(TmTalep a, string eylem, string not, string kullanici)
        {
            if (a.Iptal) return "İptal edilmiş talepte işlem yapılamaz.";
            var adim = a.Onaylar.FirstOrDefault(o => o.Durum == "Onay Bekliyor");
            if (adim == null) return "Bekleyen onay adımı yok.";
            var t = DateTime.Now; string tablo = a.Modul.NoOnEk + "_APPROVAL";
            if (eylem == "Reddet") { if (string.IsNullOrWhiteSpace(not)) return "Red gerekçesi zorunludur."; adim.Durum = "Reddedildi"; adim.Tarih = t; adim.Not = not; foreach (var o in a.Onaylar.Where(o => o.Durum == "Pasif")) o.Durum = "İptal"; Hareket(a, t, $"{adim.Adim} — reddedildi ({not})", adim.Kisi); Log(a, t, tablo, "STATUS", "Onay Bekliyor", "Reddedildi", kullanici, "Update", "Workflow"); return null; }
            if (eylem == "Revizyon") { if (string.IsNullOrWhiteSpace(not)) return "Revizyon açıklaması zorunludur."; adim.Durum = "Revizyon"; adim.Tarih = t; adim.Not = not; Hareket(a, t, $"{adim.Adim} — revizyona gönderildi ({not})", adim.Kisi); Log(a, t, tablo, "STATUS", "Onay Bekliyor", "Revizyon", kullanici, "Update", "Workflow"); return null; }
            if (eylem == "Onayla")
            {
                adim.Durum = "Onaylandı"; adim.Tarih = t; adim.Not = not;
                var sonraki = a.Onaylar.FirstOrDefault(o => o.Durum == "Pasif");
                if (sonraki != null) sonraki.Durum = "Onay Bekliyor";
                Hareket(a, t, adim.Adim.Replace(" Onayı", "") + " onayladı" + (string.IsNullOrEmpty(not) ? "" : " (" + not + ")"), adim.Kisi);
                if (sonraki == null) Hareket(a, t.AddSeconds(1), "Talep onaylandı; ilgili birime ve kaynak sisteme iletildi", "Sistem");
                Log(a, t, tablo, "STATUS", "Onay Bekliyor", "Onaylandı", kullanici, "Update", "Workflow");
                return null;
            }
            return "Tanımsız eylem.";
        }

        public static string Revize(TmTalep a, Dictionary<string, string> yeni, string kullanici)
        {
            if (a.TalepDurumu != "Revizyon Bekliyor") return "Talep revizyon durumunda değil.";
            var eskiOzet = a.Ozet; var eski = new Dictionary<string, string>(a.Degerler); string eskiTur = a.TurKod;
            foreach (var kv in yeni) { if (kv.Key == "_tur") a.TurKod = kv.Value; else if (a.Modul.Alanlar.Any(x => x.Ad == kv.Key)) a.Degerler[kv.Key] = kv.Value; }
            var s = Hazirla(a);
            if (s.Hata != null) { a.Degerler = eski; a.TurKod = eskiTur; return s.Hata; }
            var t = DateTime.Now; a.Revizyon++; a.Uyarilar = s.Uyarilar; a.Onaylar = OnayAkisiOlustur(a, t, s.EkAdimlar);
            Hareket(a, t, $"Talep revize edildi: {eskiOzet} → {a.Ozet}; onay akışı yeniden hesaplandı", a.Olusturan.AdSoyad);
            foreach (var kv in a.Degerler) { eski.TryGetValue(kv.Key, out var ev); if ((ev ?? "") != (kv.Value ?? "")) Log(a, t, a.Modul.NoOnEk + "_REQUEST", kv.Key.ToUpperInvariant(), ev, kv.Value, kullanici, "Update", "Portal"); }
            return null;
        }

        public static string IptalEt(TmTalep a, string neden, string kullanici, Func<TmTalep, string> engel = null)
        {
            if (a.Iptal) return "Zaten iptal edilmiş.";
            if (string.IsNullOrWhiteSpace(neden)) return "İptal nedeni zorunludur.";
            var e = engel?.Invoke(a); if (e != null) return e;
            var t = DateTime.Now; a.Iptal = true; a.IptalNedeni = neden;
            foreach (var o in a.Onaylar.Where(o => o.Durum == "Onay Bekliyor" || o.Durum == "Pasif")) o.Durum = "İptal";
            Hareket(a, t, $"Talep iptal edildi ({neden})", kullanici);
            Log(a, t, a.Modul.NoOnEk + "_REQUEST", "REQUEST_STATUS", "Onaylandı", "İptal Edildi", kullanici, "Update", "Portal");
            return null;
        }

        public static string OlayIsle(TmTalep a, string olay, string islemId, Dictionary<string, string> g, string kullanici)
        {
            if (string.IsNullOrWhiteSpace(islemId)) return "Transaction ID zorunludur.";
            lock (Kilit) { if (IslenenIslemler.Contains(islemId)) return $"'{islemId}' daha önce işlendi; mükerrer olay yok sayıldı."; }
            if (a.Iptal) return "İptal edilmiş talebe olay işlenemez.";
            var tanim = a.Modul.Olaylar.FirstOrDefault(o => o.Kod == olay); if (tanim == null) return "Tanımsız olay: " + olay;
            if (!a.Onaylandi) return "Talep onaylanmadan kaynak sistem olayı işlenemez.";
            var hata = a.Modul.Olay(a, olay, g ?? new Dictionary<string, string>());
            if (hata != null) return hata;
            lock (Kilit) IslenenIslemler.Add(islemId);
            a.SonSenkron = DateTime.Now; return null;
        }

        // ---- tohum yardımcıları (modül tanımlarından çağrılır) ----
        public static TmTalep Tohum(string modul, string olusturan, string kullanan, string tur, Dictionary<string, string> degerler, int gunOnce, int onayAdet = 9)
        {
            var t = DateTime.Today.AddDays(-gunOnce).AddHours(9).AddMinutes(5 + _sonId);
            var a = new TmTalep { ModulKod = modul, Olusturan = KisiBul(olusturan), Kullanan = KisiBul(kullanan), TurKod = tur, Degerler = degerler, Tarih = t };
            var s = Hazirla(a); if (s.Hata != null) throw new InvalidOperationException($"{modul} tohumu hatalı ({kullanan}, {tur}): {s.Hata}");
            a.Id = ++_sonId; a.No = $"{a.Modul.NoOnEk}-{DateTime.Today.Year}-{(_sonId + 300):000000}"; a.Tarih = t; a.Uyarilar = s.Uyarilar; a.Onaylar = OnayAkisiOlustur(a, t, s.EkAdimlar);
            Hareket(a, t, $"Talep oluşturuldu ({a.Tur.Ad}) — {a.Ozet}", a.Olusturan.AdSoyad);
            Log(a, t, a.Modul.NoOnEk + "_REQUEST", "REQUEST_STATUS", "", a.TalepDurumu, a.Olusturan.KullaniciId, "Insert", "Portal");
            for (int i = 0; i < onayAdet; i++)
            {
                var adim = a.Onaylar.FirstOrDefault(x => x.Durum == "Onay Bekliyor"); if (adim == null) break;
                var tt = t.AddHours(2.5 * (i + 1)); adim.Durum = "Onaylandı"; adim.Tarih = tt;
                var son = a.Onaylar.FirstOrDefault(x => x.Durum == "Pasif"); if (son != null) son.Durum = "Onay Bekliyor";
                Hareket(a, tt, adim.Adim.Replace(" Onayı", "") + " onayladı", adim.Kisi); Log(a, tt, a.Modul.NoOnEk + "_APPROVAL", "STATUS", "Onay Bekliyor", "Onaylandı", adim.Kisi, "Update", "Workflow");
                if (son == null) Hareket(a, tt.AddSeconds(1), "Talep onaylandı; ilgili birime ve kaynak sisteme iletildi", "Sistem");
            }
            a.SonSenkron = a.Zaman.Last().Tarih;
            Talepler.Add(a); return a;
        }
        public static void TohumRed(TmTalep a, string not) { var adim = a.Onaylar.First(x => x.Durum == "Onay Bekliyor"); adim.Durum = "Reddedildi"; adim.Tarih = a.Tarih.AddHours(3); adim.Not = not; foreach (var o in a.Onaylar.Where(o => o.Durum == "Pasif")) o.Durum = "İptal"; Hareket(a, adim.Tarih.Value, adim.Adim + " — reddedildi (" + not + ")", adim.Kisi); }
        public static void TohumRevizyon(TmTalep a, string not) { var adim = a.Onaylar.First(x => x.Durum == "Onay Bekliyor"); adim.Durum = "Revizyon"; adim.Tarih = a.Tarih.AddHours(2); adim.Not = not; Hareket(a, adim.Tarih.Value, adim.Adim + " — revizyona gönderildi (" + not + ")", adim.Kisi); }
        public static void TohumOlay(TmTalep a, string olay, Dictionary<string, string> g, int gunOnce, int saat = 10)
        {
            var t = DateTime.Today.AddDays(-gunOnce).AddHours(saat).AddMinutes(a.Id * 3 % 50);
            TalepModulleri.OlayZamani = t;
            string hata; try { hata = a.Modul.Olay(a, olay, g ?? new Dictionary<string, string>()); } finally { TalepModulleri.OlayZamani = null; }
            if (hata != null) throw new InvalidOperationException($"{a.No} {olay}: {hata}");
            IslenenIslemler.Add($"SEED-{a.No}-{olay}-{a.Zaman.Count}"); a.SonSenkron = t;
        }
    }
}
