// ============================================================
// STOK TANIM KAYITLARI (15 Kas 2026) — Konsolide Stok Raporu'nda kullanıcının girdiği tanımlar. Üç kullanıcı isteği:
//   (1) "Tüketim ve Yeterlilik sekmesine kritik stok vs girelim"  → STOK EŞİKLERİ: stok kartı (şirket | depo kodu | stok kodu) başına
//       Kritik Stok, Minimum Stok, Tedarik Süresi (gün), Hedef Stok. Girilen değer hesaplanan / örnek değerin yerine geçer; boş bırakılan alan otomatik kalır.
//   (2) "Sayım sekmesine listede olmayan ürünü girebilelim"       → EK ÜRÜN (Kaynak = "Sayım"): stok listesinde olmayan ürün tanımı. Miktarı sayım akışından gelir
//       (Sayım → Onay Bekliyor → Sayım Onayı); onaylanana kadar fiili stoku 0'dır.
//   (3) "Demirbaş (üretim makineleri bilgileri) ve küçük demirbaşlar da girilebilsin" → DEMİRBAŞ (Kaynak = "Demirbaş"): Makine / Ekipman ve Küçük Demirbaş kayıtları;
//       marka, model, seri no, üretim yılı, kapasite, alım tarihi / bedeli, durum, zimmet, garanti ve bakım tarihleri. Adet doğrudan fiili miktardır (envanter kaydı);
//       sonradan Sayım sekmesinden sayılıp onaylanırsa onaylı sayım geçerli olur.
// Ek ürünler raporda sıradan stok satırı gibi davranır (rapor: wwwroot/js/stok-raporu.js, hazirla()). Muhasebe (R) miktarı muhasebe kaynağı bağlanana kadar
// yalnız demirbaş formundaki "Muhasebe Adedi (R)" alanından gelir; boşsa 0'dır (muhasebe ve fiili ayrı tutulur).
// Satır anahtarı: şirket | depo kodu | stok kodu | parti / lot — kayıt açıldıktan sonra değişmez (sayım kayıtları bu anahtara bağlıdır).
// Saklama: App_Data/stok-tanim.json (PersonelMaliyetKalici.Dizin altında; PM_KALICI_DIZIN ile değiştirilebilir). Dizin yoksa kayıtlar yalnız bellekte durur.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace WebApplication3.Data
{
    public static class StokTanimKayit
    {
        public const string KaynakSayim = "Sayım", KaynakDemirbas = "Demirbaş";
        public static readonly string[] DemirbasTipleri = { "Makine / Ekipman", "Küçük Demirbaş" };
        public static readonly string[] Durumlar = { "Aktif", "Bakımda", "Arızalı", "Atıl", "Hurda" };

        public class Urun
        {
            public int Id { get; set; }
            public string Kaynak { get; set; }
            public string Sirket { get; set; }
            public string DepoKod { get; set; }
            public string Depo { get; set; }
            public string Kod { get; set; }
            public string Ad { get; set; }
            public string Tip { get; set; }
            public string Birim { get; set; }
            public string Lot { get; set; }
            public string Tedarikci { get; set; }
            /// <summary>Birim maliyet / birim alım bedeli (₺).</summary>
            public decimal Maliyet { get; set; }
            /// <summary>Fiili başlangıç miktarı: demirbaşta adet; sayımdan eklenen üründe 0 (miktar onaylı sayımla oluşur).</summary>
            public decimal Miktar { get; set; }
            /// <summary>Muhasebede kayıtlı miktar (R); muhasebe kaynağı bağlanana kadar elle girilir, boşsa 0.</summary>
            public decimal MuhMiktar { get; set; }
            // ---- demirbaş bilgileri
            public string Marka { get; set; }
            public string Model { get; set; }
            public string SeriNo { get; set; }
            public int? UretimYili { get; set; }
            public string Kapasite { get; set; }
            /// <summary>Bölüm / hat (serbest metin).</summary>
            public string Yer { get; set; }
            public string AlimTarihi { get; set; }
            public string GarantiBitis { get; set; }
            public string SonBakim { get; set; }
            public string SonrakiBakim { get; set; }
            public string Durum { get; set; }
            public string Zimmet { get; set; }
            public string Aciklama { get; set; }
            public string Giren { get; set; }
            public DateTime GirisZamani { get; set; }
            public string Guncelleyen { get; set; }
            public DateTime? GuncellemeZamani { get; set; }
        }

        /// <summary>Ekrandan gelen ürün / demirbaş kaydı; listede olmayan ürün sayım sırasında ekleniyorsa sayılan miktar da birlikte gelir.</summary>
        public class UrunIstek : Urun
        {
            public decimal? Sayilan { get; set; }
            public string SayimTarihi { get; set; }
            public string SayimNot { get; set; }
        }

        public class Esik
        {
            /// <summary>şirket | depo kodu | stok kodu</summary>
            public string Anahtar { get; set; }
            public decimal? Kritik { get; set; }
            public decimal? Minimum { get; set; }
            public int? TedarikGun { get; set; }
            public decimal? Hedef { get; set; }
            public string Giren { get; set; }
            public DateTime Zaman { get; set; }
        }

        class Depo_ { public List<Urun> Urunler { get; set; } = new List<Urun>(); public List<Esik> Esikler { get; set; } = new List<Esik>(); }

        static readonly object Kilit = new object();
        static Depo_ _d;
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, WriteIndented = true };
        static string Dosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data", "stok-tanim.json");

        static Depo_ Yukle()
        {
            if (_d != null) return _d;
            _d = new Depo_();
            try { var f = Dosya; if (f != null && File.Exists(f)) _d = JsonSerializer.Deserialize<Depo_>(File.ReadAllText(f), Secenek) ?? new Depo_(); }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] Stok tanım kayıtları okunamadı: {ex.Message}"); }
            _d.Urunler ??= new List<Urun>(); _d.Esikler ??= new List<Esik>();
            return _d;
        }

        static void Yaz()
        {
            var f = Dosya; if (f == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(f)); File.WriteAllText(f, JsonSerializer.Serialize(_d, Secenek)); }
            catch (Exception ex) { Console.WriteLine($"[Kalıcı] Stok tanım kayıtları yazılamadı: {ex.Message}"); }
        }

        public static List<Urun> Urunler() { lock (Kilit) return Yukle().Urunler.ToList(); }
        public static List<Esik> Esikler() { lock (Kilit) return Yukle().Esikler.ToList(); }
        public static string Anahtar(Urun u) => $"{u.Sirket}|{u.DepoKod}|{u.Kod}|{u.Lot ?? ""}";

        static string Kirp(string s, int uzunluk) { s = (s ?? "").Trim(); return s.Length > uzunluk ? s.Substring(0, uzunluk) : s; }
        static bool TarihMi(string s, out string duz)
        {
            duz = ""; if (string.IsNullOrWhiteSpace(s)) return true;
            if (!DateTime.TryParseExact(s.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t)) return false;
            duz = t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); return true;
        }

        /// <summary>Ürün / demirbaş kaydı ekler ya da günceller. Güncellemede anahtar alanları (şirket, depo, stok kodu, parti) değişmez. Dönen değer: (kayıt, hata iletisi).</summary>
        public static (Urun Kayit, string Hata) UrunKaydet(Urun g, string kisi)
        {
            if (g == null) return (null, "Kayıt okunamadı.");
            string kaynak = g.Kaynak == KaynakDemirbas ? KaynakDemirbas : KaynakSayim; bool demirbas = kaynak == KaynakDemirbas;
            string sirket = Kirp(g.Sirket, 60), depoKod = Kirp(g.DepoKod, 10), depo = Kirp(g.Depo, 60), kod = Kirp(g.Kod, 40), ad = Kirp(g.Ad, 120), tip = Kirp(g.Tip, 40), birim = Kirp(g.Birim, 12), lot = Kirp(g.Lot, 40);
            string kodAdi = demirbas ? "Demirbaş kodu" : "Stok kodu";
            if (sirket == "") return (null, "Şirket seçilmeli.");
            if (depoKod == "" || depo == "") return (null, demirbas ? "Bulunduğu yer seçilmeli." : "Depo seçilmeli.");
            if (kod == "") return (null, kodAdi + " girilmeli.");
            if (ad == "") return (null, (demirbas ? "Demirbaş adı" : "Ürün adı") + " girilmeli.");
            if (!StokRaporOrnek.StokTipleri.Contains(tip)) return (null, "Stok tipi tanınmadı.");
            if (demirbas != DemirbasTipleri.Contains(tip)) return (null, demirbas ? "Demirbaş türü Makine / Ekipman ya da Küçük Demirbaş olmalı." : "Demirbaşlar Demirbaş grubundan girilir.");
            if (birim == "") return (null, "Birim seçilmeli.");
            if (new[] { sirket, depoKod, kod, lot }.Any(x => x.Contains('|'))) return (null, "Şirket, depo, kod ve parti alanlarında | karakteri kullanılamaz.");
            if (g.Maliyet < 0 || g.Maliyet > 100_000_000_000m) return (null, "Birim maliyet 0 ya da daha büyük olmalı.");
            if (g.Miktar < 0 || g.Miktar > 1_000_000_000m) return (null, "Miktar 0 ya da daha büyük olmalı.");
            if (g.MuhMiktar < 0 || g.MuhMiktar > 1_000_000_000m) return (null, "Muhasebe miktarı 0 ya da daha büyük olmalı.");
            if (g.UretimYili.HasValue && (g.UretimYili < 1900 || g.UretimYili > DateTime.Today.Year + 1)) return (null, "Üretim yılı geçersiz.");
            if (!TarihMi(g.AlimTarihi, out var alim)) return (null, "Alım tarihi geçersiz.");
            if (!TarihMi(g.GarantiBitis, out var garanti)) return (null, "Garanti bitiş tarihi geçersiz.");
            if (!TarihMi(g.SonBakim, out var sonBakim)) return (null, "Son bakım tarihi geçersiz.");
            if (!TarihMi(g.SonrakiBakim, out var sonrakiBakim)) return (null, "Sonraki bakım tarihi geçersiz.");
            string durum = Kirp(g.Durum, 20); if (demirbas && durum == "") durum = Durumlar[0];
            if (durum != "" && !Durumlar.Contains(durum)) return (null, "Durum tanınmadı.");
            lock (Kilit)
            {
                var d = Yukle(); Urun k;
                if (g.Id > 0)
                {
                    k = d.Urunler.FirstOrDefault(x => x.Id == g.Id); if (k == null) return (null, "Kayıt bulunamadı.");
                    k.Guncelleyen = kisi; k.GuncellemeZamani = DateTime.Now;
                }
                else
                {
                    k = new Urun { Id = d.Urunler.Count == 0 ? 1 : d.Urunler.Max(x => x.Id) + 1, Kaynak = kaynak, Sirket = sirket, DepoKod = depoKod, Depo = depo, Kod = kod, Lot = lot, Giren = kisi, GirisZamani = DateTime.Now };
                    if (d.Urunler.Any(x => Anahtar(x) == Anahtar(k))) return (null, $"{kodAdi} bu şirket ve {(demirbas ? "yerde" : "depoda")} zaten kayıtlı.");
                    d.Urunler.Add(k);
                }
                k.Ad = ad; k.Tip = tip; k.Birim = birim; k.Tedarikci = Kirp(g.Tedarikci, 80); k.Maliyet = Math.Round(g.Maliyet, 2); k.Miktar = Math.Round(g.Miktar, 3); k.MuhMiktar = Math.Round(g.MuhMiktar, 3);
                k.Marka = Kirp(g.Marka, 60); k.Model = Kirp(g.Model, 60); k.SeriNo = Kirp(g.SeriNo, 60); k.UretimYili = g.UretimYili; k.Kapasite = Kirp(g.Kapasite, 60); k.Yer = Kirp(g.Yer, 60);
                k.AlimTarihi = alim; k.GarantiBitis = garanti; k.SonBakim = sonBakim; k.SonrakiBakim = sonrakiBakim; k.Durum = durum; k.Zimmet = Kirp(g.Zimmet, 60); k.Aciklama = Kirp(g.Aciklama, 300);
                Yaz();
                return (k, null);
            }
        }

        /// <summary>Ürün / demirbaş kaydını siler; dönen değer silinen kaydın satır anahtarıdır (sayım kayıtları da onunla silinir).</summary>
        public static (string Anahtar, string Hata) UrunSil(int id)
        {
            lock (Kilit)
            {
                var d = Yukle(); var k = d.Urunler.FirstOrDefault(x => x.Id == id); if (k == null) return (null, "Kayıt bulunamadı.");
                d.Urunler.Remove(k); string a = Anahtar(k); d.Esikler.RemoveAll(e => e.Anahtar == $"{k.Sirket}|{k.DepoKod}|{k.Kod}" && !d.Urunler.Any(x => $"{x.Sirket}|{x.DepoKod}|{x.Kod}" == e.Anahtar));
                Yaz(); return (a, null);
            }
        }

        /// <summary>Stok eşiklerini kaydeder: her kayıt kartın bütün eşik alanlarını taşır; hepsi boşsa kartın kaydı silinir (otomatik değerlere döner). Dönen değer: (işlenen kart sayısı, hata iletisi).</summary>
        public static (int Adet, string Hata) EsikKaydet(IEnumerable<Esik> girisler, string kisi)
        {
            var l = (girisler ?? Enumerable.Empty<Esik>()).Where(x => x != null).ToList();
            foreach (var g in l)
            {
                if (string.IsNullOrWhiteSpace(g.Anahtar) || g.Anahtar.Length > 160 || g.Anahtar.Split('|').Length != 3) return (0, "Stok kartı tanınmadı.");
                foreach (var v in new[] { g.Kritik, g.Minimum, g.Hedef }) if (v.HasValue && (v < 0 || v > 1_000_000_000m)) return (0, "Stok eşikleri 0 ya da daha büyük olmalı.");
                if (g.TedarikGun.HasValue && (g.TedarikGun < 0 || g.TedarikGun > 3650)) return (0, "Tedarik süresi 0 ile 3650 gün arasında olmalı.");
                if (g.Kritik.HasValue && g.Minimum.HasValue && g.Minimum < g.Kritik) return (0, "Minimum stok kritik stoktan küçük olamaz.");
                if (g.Minimum.HasValue && g.Hedef.HasValue && g.Hedef < g.Minimum) return (0, "Hedef stok minimum stoktan küçük olamaz.");
            }
            lock (Kilit)
            {
                var d = Yukle();
                foreach (var g in l)
                {
                    d.Esikler.RemoveAll(e => e.Anahtar == g.Anahtar);
                    if (g.Kritik.HasValue || g.Minimum.HasValue || g.TedarikGun.HasValue || g.Hedef.HasValue)
                        d.Esikler.Add(new Esik { Anahtar = g.Anahtar, Kritik = g.Kritik.HasValue ? Math.Round(g.Kritik.Value, 3) : (decimal?)null, Minimum = g.Minimum.HasValue ? Math.Round(g.Minimum.Value, 3) : (decimal?)null, TedarikGun = g.TedarikGun, Hedef = g.Hedef.HasValue ? Math.Round(g.Hedef.Value, 3) : (decimal?)null, Giren = kisi, Zaman = DateTime.Now });
                }
                if (l.Count > 0) Yaz();
            }
            return (l.Count, null);
        }
    }
}
