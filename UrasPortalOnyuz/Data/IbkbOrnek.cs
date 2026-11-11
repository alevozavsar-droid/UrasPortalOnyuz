// ============================================================
// İBKB BEYANNAME TAKİBİ — kalıcı veri (Mali İşler › Finans › Döviz ve Dış Ticaret Finansmanı; Yurtdışı Satış › İhracat Operasyon Takibi'nde kısayol)
// 11 Kas 2026 ("finans ve ihracat arasında dinamik bir İBKB raporu olmalı; eksikler varsa ekle, raporu geliştir"): üretilmiş Rapor168 iskeleti yerine gerçek kayıt.
// Kaynak: ihracat departmanının IMKB_LISTELERI.xlsx dosyası (2025-URAS 186, 2026-URAS 23, 2026-SELVİ 125 dosya) → Data/Tohum/ibkb-listeleri-2026-11.json (ilk açılışta uygulanır).
// Kayıt = İHRACAT DOSYASI: şirket, yıl, dosya no, müşteri (kod + ad), başlangıç / kapanış tarihi, gümrük çıkış beyannamesi (GÇB) no, beyanname tutarı + döviz,
//   ülke gerekliliği (% — bedelin ne kadarı yurda getirilmeli), DAB / İBKB durumu (Bekliyor | Yapıldı | Peşin Yapıldı | Kısmi | Gerek Yok | İptal), banka,
//   kısmi kalan, bağlanan DAB satırları (tutar + döviz + banka + tarih), not. Hesaplanan: gereken = tutar × oran, getirilen, kalan, vade = başlangıç + 180 gün
//   (ihracat bedelinin yurda getirilme süresi), kalan gün, takip durumu (Açık | Vade Yaklaşıyor | Vadesi Geçti | Kısmi | Kapandı | Muaf | İptal).
// Kalıcılık: App_Data/ibkb.json (git dışı). Ekran: Controllers/IbkbController.cs + Views/Ibkb/Index.cshtml + wwwroot/js/ibkb.js.
// Yazım: liste değerleri Title Case; tutarlar kendi dövizinde ($ € £ ₽); dövizi Excel'de yazılmayan satır "Döviz Belirsiz" rozetiyle gösterilir (uydurulmaz).
// ============================================================
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebApplication3.Data
{
    /// <summary>Dosyaya bağlanan DAB / İBKB satırı (bankadan gelen döviz alım belgesi).</summary>
    public class IbkbDab { public decimal Tutar { get; set; } public string Doviz { get; set; } = "USD"; public DateTime? Tarih { get; set; } public string Banka { get; set; } = ""; public string No { get; set; } = ""; public string Not { get; set; } = ""; }
    public class IbkbDabIstek { public decimal Tutar { get; set; } public string Doviz { get; set; } public string Tarih { get; set; } public string Banka { get; set; } public string No { get; set; } public string Not { get; set; } }

    /// <summary>İhracat dosyası (beyanname) kaydı.</summary>
    public class IbkbDosya
    {
        public int Id { get; set; } public string Anahtar { get; set; } = "";
        public string Sirket { get; set; } = ""; public int Yil { get; set; } public string DosyaNo { get; set; } = ""; public string Aciklama { get; set; } = "";
        public string MusteriKodu { get; set; } = ""; public string Musteri { get; set; } = ""; public string Ulke { get; set; } = "";
        public string FaturaNo { get; set; } = ""; public string SiparisNo { get; set; } = "";   // İhracat Satışları (Yurtdışı Satış Siparişi) bağı (11 Kas 2026)
        public DateTime? Baslangic { get; set; } public DateTime? Kapanis { get; set; }
        public string BeyannameNo { get; set; } = ""; public string Doviz { get; set; } = ""; public decimal Tutar { get; set; }
        /// <summary>Gerçek ihracat bedeli (proforma / fatura; müşteriyle anlaşılan tutar) — beyanname tutarından farklı olabilir (11 Kas 2026: "beyanname 100.000 $ ama proforma 50.000 $, müşteri talebi"). 0 = beyanname ile aynı. Fark nedeni: Müşteri Talebi, İskonto …</summary>
        public decimal GercekBedel { get; set; } public string FarkNedeni { get; set; } = "";
        public decimal UlkeOran { get; set; } = 100m;
        public string DabDurumu { get; set; } = "Bekliyor"; public string DabBanka { get; set; } = ""; public decimal DabKalan { get; set; } public string DabNotu { get; set; } = "";
        public List<IbkbDab> Dablar { get; set; } = new List<IbkbDab>();
        public string Not { get; set; } = ""; public string Kaynak { get; set; } = "Ekran";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = ""; public DateTime? Guncelleme { get; set; } public string Guncelleyen { get; set; } = "";
    }
    public class IbkbIstek
    {
        public int Id { get; set; } public string Sirket { get; set; } public int Yil { get; set; } public string DosyaNo { get; set; } public string Aciklama { get; set; }
        public string MusteriKodu { get; set; } public string Musteri { get; set; } public string Ulke { get; set; } public string FaturaNo { get; set; } public string SiparisNo { get; set; } public string Baslangic { get; set; } public string Kapanis { get; set; }
        public string BeyannameNo { get; set; } public string Doviz { get; set; } public decimal Tutar { get; set; } public decimal GercekBedel { get; set; } public string FarkNedeni { get; set; } public decimal UlkeOran { get; set; } = 100m;
        public string DabDurumu { get; set; } public string DabBanka { get; set; } public decimal DabKalan { get; set; } public string DabNotu { get; set; }
        public List<IbkbDabIstek> Dablar { get; set; } public string Not { get; set; }
    }
    /// <summary>Bankadan gelen ihracat tahsilatı (döviz girişi). Kaynak: Banka (Otomatik) — banka / ERP entegrasyonu bağlanınca; Elle; Excel (Bağlanan DAB). Durum: Eşleşmedi → Eşleşti (dosyaya bağlandı, DAB satırı yazıldı) → İBKB Düzenlendi.</summary>
    public class IhracatTahsilat
    {
        public int Id { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = ""; public DateTime? Tarih { get; set; }
        public string Gonderen { get; set; } = ""; public string MusteriKodu { get; set; } = ""; public decimal Tutar { get; set; } public string Doviz { get; set; } = "USD";
        public string Referans { get; set; } = ""; public string Aciklama { get; set; } = ""; public string Kaynak { get; set; } = "Elle";
        public string Durum { get; set; } = "Eşleşmedi"; public int DosyaId { get; set; } public int IbkbBelgeId { get; set; }
        public DateTime? EslestirmeTarihi { get; set; } public string Eslestiren { get; set; } = "";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
    }
    public class IhracatTahsilatIstek { public int Id { get; set; } public string Sirket { get; set; } public string Banka { get; set; } public string Tarih { get; set; } public string Gonderen { get; set; } public string MusteriKodu { get; set; } public decimal Tutar { get; set; } public string Doviz { get; set; } public string Referans { get; set; } public string Aciklama { get; set; } }
    public class EslestirIstek { public int TahsilatId { get; set; } public int DosyaId { get; set; } public bool IbkbOlustur { get; set; } }
    /// <summary>Bankaya verilecek İBKB başvuru belgesi: tahsilat ↔ ihracat dosyası eşleşmesinden üretilir; banka İBKB numarasını verince kapanır.</summary>
    public class IbkbBelge
    {
        public int Id { get; set; } public string No { get; set; } = ""; public DateTime Tarih { get; set; } public string Sirket { get; set; } = ""; public string Banka { get; set; } = "";
        public int DosyaId { get; set; } public string DosyaNo { get; set; } = ""; public string BeyannameNo { get; set; } = ""; public string FaturaNo { get; set; } = "";
        public string MusteriKodu { get; set; } = ""; public string Musteri { get; set; } = ""; public string Ulke { get; set; } = "";
        public decimal BeyannameTutar { get; set; } public string BeyannameDoviz { get; set; } = ""; public decimal Tutar { get; set; } public string Doviz { get; set; } = "";
        public int TahsilatId { get; set; } public string TahsilatRef { get; set; } = ""; public DateTime? TahsilatTarihi { get; set; } public DateTime? FiiliIhrac { get; set; }
        public string Durum { get; set; } = "Hazırlandı"; public string BankaIbkbNo { get; set; } = ""; public DateTime? BankaTarihi { get; set; } public string Not { get; set; } = "";
        public DateTime Olusturma { get; set; } public string Olusturan { get; set; } = "";
    }
    public class IbkbBelgeIstek { public int Id { get; set; } public string Durum { get; set; } public string BankaIbkbNo { get; set; } public string BankaTarihi { get; set; } public string Not { get; set; } }
    public class IbkbDepo { public int TohumSurumu { get; set; } public List<IbkbDosya> Dosyalar { get; set; } = new List<IbkbDosya>(); public List<IhracatTahsilat> Tahsilatlar { get; set; } = new List<IhracatTahsilat>(); public List<IbkbBelge> Belgeler { get; set; } = new List<IbkbBelge>(); }
    public class IbkbTohum { public int Surum { get; set; } public string Kaynak { get; set; } = ""; public List<IbkbDosya> Dosyalar { get; set; } = new List<IbkbDosya>(); }

    public static class IbkbOrnek
    {
        public static readonly object Kilit = new object();
        static IbkbDepo _depo = new IbkbDepo(); static bool _yuklendi;
        public const int TohumSurum = 2;   // 2 (11 Kas 2026): Excel'deki bağlanan DAB satırları banka tahsilatı kaydı olarak da türetilir (Eşleşti)
        public static readonly string[] TahsilatDurumlari = { "Eşleşmedi", "Eşleşti", "İBKB Düzenlendi" };
        public static readonly string[] BelgeDurumlari = { "Hazırlandı", "Bankaya Gönderildi", "Banka Onayladı", "İptal" };
        public static readonly string[] TahsilatKaynaklari = { "Banka (Otomatik)", "Elle", "Excel (Bağlanan DAB)" };
        public const int VadeGun = 180;   // ihracat bedelinin fiili ihraçtan itibaren yurda getirilme süresi (gün)
        public static readonly string[] Sirketler = { "Uras Kimya", "Selvi Kimya" };
        public static readonly string[] DabDurumlari = { "Bekliyor", "Yapıldı", "Peşin Yapıldı", "Kısmi", "Gerek Yok", "İptal" };
        public static readonly string[] TakipDurumlari = { "Açık", "Vade Yaklaşıyor", "Vadesi Geçti", "Kısmi", "Bedel Farkı", "Kapandı", "Muaf", "İptal" };   // Bedel Farkı: gerçek bedel tahsil edildi, yalnız beyanname fazlası açık
        public static readonly string[] FarkNedenleri = { "Müşteri Talebi", "İskonto / Fiyat Farkı", "Kısmi Sevkiyat", "Numune / Bedelsiz", "Kur Farkı", "Beyanname Hatası", "Diğer" };
        public static readonly string[] Dovizler = { "USD", "EUR", "GBP", "RUB" };
        /// <summary>DAB / İBKB eşiği (ABD doları karşılığı): TCMB İhracat Genelgesi md. 28/1-a — 15.000 ABD doları veya eşitine kadar noksanlığı olan ihracat hesapları DAB aranmadan doğrudan bankaca kapatılır (terkin);
        /// ihracat departmanı uygulaması: bu tutar ve altındaki dosyalarda DAB / İBKB "Gerek Yok". Genelgede "İBKB düzenlenmez" diye ayrı bir tutar maddesi yoktur (11 Kas 2026 araştırması); eşik değişirse burası güncellenir.</summary>
        public const decimal EsikUsd = 15000m;
        /// <summary>1 birim döviz = kaç USD (NakitAkisOrnek.Kurlar TL kurlarından çapraz; RUB kur tablosunda yok → yaklaşık 0,0125).</summary>
        public static Dictionary<string, decimal> UsdKur()
        {
            var k = NakitAkisOrnek.Kurlar; decimal usd = k.TryGetValue("USD", out var u) && u > 0 ? u : 1m;
            var m = new Dictionary<string, decimal> { ["USD"] = 1m };
            foreach (var pb in new[] { "EUR", "GBP" }) m[pb] = k.TryGetValue(pb, out var v) && v > 0 ? Math.Round(v / usd, 4) : 1m;
            m["RUB"] = 0.0125m; return m;
        }
        public static decimal UsdKarsiligi(IbkbDosya d) => string.IsNullOrEmpty(d.Doviz) ? 0 : R2(d.Tutar * (UsdKur().TryGetValue(d.Doviz, out var r) ? r : 1m));
        public static bool EsikAlti(IbkbDosya d) => d.Tutar > 0 && !string.IsNullOrEmpty(d.Doviz) && UsdKarsiligi(d) <= EsikUsd;
        /// <summary>İhracat Genelgesi Ek-2: bedelin yurda getirilmesi zorunlu olmayan ülkeler (kaynaklarda 36 ülke; burada adı geçen 22'si — TCMB resmi metninden tamamlanmalı) · Ek-3: beyanname tutarının %50'si serbest (9 ülke, 01.08.2025 yürürlük).</summary>
        public static readonly string[] UlkeMuaf = { "Afganistan", "Angola", "Belarus", "Benin", "Burkina Faso", "Cibuti", "Çad", "Etiyopya", "Fildişi Sahili", "Filistin", "Gine", "İran", "Kenya", "Kırgızistan", "Kongo Cumhuriyeti", "Kuzey Kore", "Küba", "Lübnan", "Mozambik", "Tanzanya", "Venezuela", "Yemen" };
        public static readonly string[] UlkeYarim = { "Azerbaycan", "Cezayir", "Fas", "Kazakistan", "Libya", "Özbekistan", "Tunus", "Türkmenistan", "Ukrayna" };
        static readonly JsonSerializerOptions Secenek = new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, PropertyNameCaseInsensitive = true, WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
        public static string Dosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "App_Data", "ibkb.json");
        public static string TohumDosya => string.IsNullOrEmpty(PersonelMaliyetKalici.Dizin) ? null : Path.Combine(PersonelMaliyetKalici.Dizin, "Data", "Tohum", "ibkb-listeleri-2026-11.json");
        static string D(DateTime? t) => t?.ToString("yyyy-MM-dd");
        static decimal R2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        static string Temiz(string s) => (s ?? "").Trim();
        static string Sec(string v, string[] liste, string varsayilan) => liste.Contains(v ?? "") ? v : varsayilan;
        static DateTime? Tarih(string s) => DateTime.TryParse(s, out var t) ? t.Date : (DateTime?)null;

        // ---------- kalıcılık ----------
        public static void Yukle()
        {
            lock (Kilit)
            {
                if (_yuklendi) return; _yuklendi = true;
                var dosya = Dosya;
                if (dosya != null && File.Exists(dosya))
                {
                    try { _depo = JsonSerializer.Deserialize<IbkbDepo>(File.ReadAllText(dosya), Secenek) ?? new IbkbDepo(); }
                    catch (Exception ex) { Console.WriteLine("[İBKB] dosya okunamadı: " + ex.Message); _depo = new IbkbDepo(); }
                }
                _depo.Dosyalar ??= new List<IbkbDosya>(); foreach (var d in _depo.Dosyalar) d.Dablar ??= new List<IbkbDab>(); _depo.Tahsilatlar ??= new List<IhracatTahsilat>(); _depo.Belgeler ??= new List<IbkbBelge>();
                if (_depo.TohumSurumu < TohumSurum) TohumUygula();
                Console.WriteLine($"[İBKB] {_depo.Dosyalar.Count} ihracat dosyası ({string.Join(", ", _depo.Dosyalar.GroupBy(x => x.Sirket + " " + x.Yil).Select(g => g.Key + ": " + g.Count()))})" + (dosya != null ? $" · {dosya}" : " (kalıcılık kapalı)"));
            }
        }
        static void Yaz()
        {
            var dosya = Dosya; if (dosya == null) return;
            try { Directory.CreateDirectory(Path.GetDirectoryName(dosya)); File.WriteAllText(dosya, JsonSerializer.Serialize(_depo, Secenek)); }
            catch (Exception ex) { Console.WriteLine("[İBKB] dosya yazılamadı: " + ex.Message); }
        }
        /// <summary>Excel listesi tohumu: anahtarı (şirket|dosya no) olmayan satırlar eklenir; daha önce eklenmiş ya da kullanıcının girdiği kayıt ezilmez.</summary>
        static void TohumUygula()
        {
            var yol = TohumDosya; if (yol == null || !File.Exists(yol)) { Console.WriteLine("[İBKB] tohum dosyası yok: " + yol); return; }
            try
            {
                var t = JsonSerializer.Deserialize<IbkbTohum>(File.ReadAllText(yol), Secenek); if (t == null) return;
                int id = _depo.Dosyalar.Select(x => x.Id).DefaultIfEmpty(0).Max() + 1, ek = 0;
                foreach (var d in t.Dosyalar ?? new List<IbkbDosya>())
                {
                    if (string.IsNullOrWhiteSpace(d.Anahtar) || _depo.Dosyalar.Any(x => x.Anahtar == d.Anahtar)) continue;
                    d.Id = id++; d.Dablar ??= new List<IbkbDab>(); d.Olusturma = DateTime.Today; d.Olusturan = "Excel Tohumu"; if (string.IsNullOrWhiteSpace(d.Kaynak)) d.Kaynak = t.Kaynak;
                    d.DabDurumu = Sec(d.DabDurumu, DabDurumlari, "Bekliyor"); if (d.UlkeOran <= 0) d.UlkeOran = 100m;
                    _depo.Dosyalar.Add(d); ek++;
                }
                // sürüm 2: Excel'deki bağlanan DAB satırları → banka tahsilatı kaydı (Eşleşti); yalnız hiç tahsilat yokken
                int ekT = 0;
                if (_depo.TohumSurumu < 2 && _depo.Tahsilatlar.Count == 0)
                {
                    int idT = 1;
                    foreach (var d in _depo.Dosyalar) foreach (var p in d.Dablar)
                        _depo.Tahsilatlar.Add(new IhracatTahsilat { Id = idT++, Sirket = d.Sirket, Banka = p.Banka ?? "", Tarih = p.Tarih, Gonderen = d.Musteri, MusteriKodu = d.MusteriKodu, Tutar = p.Tutar, Doviz = p.Doviz, Referans = p.No ?? "", Aciklama = p.Not ?? "", Kaynak = "Excel (Bağlanan DAB)", Durum = "Eşleşti", DosyaId = d.Id, EslestirmeTarihi = DateTime.Today, Eslestiren = "Excel Tohumu", Olusturma = DateTime.Today, Olusturan = "Excel Tohumu" });
                    ekT = _depo.Tahsilatlar.Count;
                }
                _depo.TohumSurumu = TohumSurum; Yaz();
                Console.WriteLine($"[İBKB] tohum uygulandı: {ek} dosya, {ekT} tahsilat ({t.Kaynak})");
            }
            catch (Exception ex) { Console.WriteLine("[İBKB] tohum uygulanamadı: " + ex.Message); }
        }

        // ---------- hesaplar ----------
        public static bool Muaf(IbkbDosya d) => d.DabDurumu == "Gerek Yok" || d.DabDurumu == "İptal";
        public static decimal Gereken(IbkbDosya d) => Muaf(d) ? 0 : R2(d.Tutar * (d.UlkeOran <= 0 ? 100m : d.UlkeOran) / 100m);
        /// <summary>Getirilen: bağlı DAB satırlarının aynı dövizdeki toplamı; satır yoksa duruma göre (Yapıldı / Peşin → gereken, Kısmi → gereken − kalan).</summary>
        public static decimal Getirilen(IbkbDosya d)
        {
            if (Muaf(d)) return 0;
            var ayni = d.Dablar.Where(x => string.Equals(x.Doviz, d.Doviz, StringComparison.OrdinalIgnoreCase)).Sum(x => x.Tutar);
            if (d.DabDurumu == "Yapıldı" || d.DabDurumu == "Peşin Yapıldı") return Gereken(d);   // banka DAB'ı tamamlandı: gereken tutarın tamamı getirilmiş sayılır
            if (d.DabDurumu == "Kısmi") return d.DabKalan > 0 ? R2(Math.Max(0, d.Tutar - d.DabKalan)) : R2(ayni);   // bankanın "X kaldı" bildirimi beyanname tutarına göredir: getirilen = tutar − kalan
            return R2(ayni);
        }
        public static decimal Kalan(IbkbDosya d) => Muaf(d) || d.DabDurumu == "Yapıldı" || d.DabDurumu == "Peşin Yapıldı" ? 0 : R2(Math.Max(0, Gereken(d) - Getirilen(d)));
        // ---- gerçek ihracat bedeli (proforma / fatura) ↔ beyanname farkı (11 Kas 2026)
        public static decimal GercekBedelEtkin(IbkbDosya d) => d.GercekBedel > 0 ? d.GercekBedel : d.Tutar;
        public static decimal BedelFarki(IbkbDosya d) => d.GercekBedel > 0 ? R2(d.Tutar - d.GercekBedel) : 0;   // + beyanname fazlası · − beyanname eksiği
        public static decimal GerekenGercek(IbkbDosya d) => Muaf(d) ? 0 : R2(GercekBedelEtkin(d) * (d.UlkeOran <= 0 ? 100m : d.UlkeOran) / 100m);
        public static decimal KalanGercek(IbkbDosya d) => Muaf(d) || d.DabDurumu == "Yapıldı" || d.DabDurumu == "Peşin Yapıldı" ? 0 : R2(Math.Max(0, GerekenGercek(d) - Getirilen(d)));
        public static decimal BeyannameFazlasi(IbkbDosya d) => Muaf(d) ? 0 : R2(Math.Max(0, Gereken(d) - GerekenGercek(d)));   // hiç tahsil edilemeyecek kısım (terkin / beyanname düzeltme konusu)
        public static DateTime? Vade(IbkbDosya d) => d.Baslangic?.AddDays(VadeGun);
        public static int? KalanGun(IbkbDosya d) => Vade(d).HasValue ? (int?)(Vade(d).Value.Date - DateTime.Today).Days : null;
        public static string Takip(IbkbDosya d)
        {
            if (d.DabDurumu == "İptal") return "İptal"; if (d.DabDurumu == "Gerek Yok") return "Muaf";
            if (d.DabDurumu == "Yapıldı" || d.DabDurumu == "Peşin Yapıldı") return "Kapandı";
            if (BedelFarki(d) > 0 && KalanGercek(d) <= 0.005m && Kalan(d) > 0.005m) return "Bedel Farkı";   // gerçek bedel tamamen geldi, yalnız beyanname fazlası açık
            if (d.DabDurumu == "Kısmi") return "Kısmi";
            var g = KalanGun(d); if (g.HasValue && g.Value < 0) return "Vadesi Geçti"; if (g.HasValue && g.Value <= 30) return "Vade Yaklaşıyor"; return "Açık";
        }

        // ---------- kayıt ----------
        public static string Kaydet(IbkbIstek d, string kullanici, out IbkbDosya kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            string sirket = Temiz(d.Sirket), no = Temiz(d.DosyaNo);
            if (sirket == "") return "Şirket seçilmelidir."; if (no == "") return "Dosya no zorunludur.";
            if (d.Tutar < 0 || d.DabKalan < 0 || d.GercekBedel < 0) return "Tutarlar negatif olamaz."; if (d.UlkeOran < 0 || d.UlkeOran > 100) return "Ülke gerekliliği 0 – 100 arasında olmalıdır.";
            lock (Kilit)
            {
                var x = d.Id > 0 ? _depo.Dosyalar.FirstOrDefault(y => y.Id == d.Id) : null; if (d.Id > 0 && x == null) return "Kayıt bulunamadı.";
                if (_depo.Dosyalar.Any(y => y.Id != d.Id && y.Sirket == sirket && string.Equals(y.DosyaNo, no, StringComparison.OrdinalIgnoreCase))) return $"{sirket} · {no} dosya numarası zaten kayıtlı.";
                if (x == null) { x = new IbkbDosya { Id = _depo.Dosyalar.Select(y => y.Id).DefaultIfEmpty(0).Max() + 1, Olusturma = DateTime.Today, Olusturan = kullanici ?? "", Kaynak = "Ekran" }; _depo.Dosyalar.Add(x); }
                x.Sirket = sirket; x.DosyaNo = no; x.Anahtar = sirket + "|" + no; x.Yil = d.Yil > 0 ? d.Yil : (int.TryParse(no.Split('-')[0], out var y0) && y0 > 2000 ? y0 : DateTime.Today.Year);
                x.Aciklama = Temiz(d.Aciklama); x.MusteriKodu = Temiz(d.MusteriKodu); x.Musteri = Temiz(d.Musteri); x.Ulke = Temiz(d.Ulke); x.FaturaNo = Temiz(d.FaturaNo); x.SiparisNo = Temiz(d.SiparisNo);
                x.Baslangic = Tarih(d.Baslangic); x.Kapanis = Tarih(d.Kapanis); x.BeyannameNo = Temiz(d.BeyannameNo); x.Doviz = Temiz(d.Doviz).ToUpperInvariant(); x.Tutar = R2(d.Tutar);
                x.GercekBedel = Math.Abs(R2(d.GercekBedel) - x.Tutar) < 0.005m ? 0 : R2(d.GercekBedel); x.FarkNedeni = x.GercekBedel > 0 ? Temiz(d.FarkNedeni) : "";   // beyanname ile aynıysa fark yok
                x.UlkeOran = d.UlkeOran <= 0 ? 100m : d.UlkeOran; x.DabDurumu = Sec(d.DabDurumu, DabDurumlari, "Bekliyor"); x.DabBanka = Temiz(d.DabBanka); x.DabKalan = R2(d.DabKalan); x.DabNotu = Temiz(d.DabNotu);
                x.Dablar = (d.Dablar ?? new List<IbkbDabIstek>()).Where(p => p != null && p.Tutar > 0).Select(p => new IbkbDab { Tutar = R2(p.Tutar), Doviz = Temiz(p.Doviz).ToUpperInvariant() == "" ? x.Doviz : Temiz(p.Doviz).ToUpperInvariant(), Tarih = Tarih(p.Tarih), Banka = Temiz(p.Banka), No = Temiz(p.No), Not = Temiz(p.Not) }).ToList();
                x.Not = Temiz(d.Not); x.Guncelleme = DateTime.Now; x.Guncelleyen = kullanici ?? "";
                // ülke kuralı: Ek-2 ülkesi → bedel getirme zorunlu değil (oran 0, Gerek Yok); Ek-3 → %50 (kullanıcı oran girmediyse)
                if (x.Ulke != "" && UlkeMuaf.Contains(x.Ulke)) { x.UlkeOran = 0; if (x.DabDurumu == "Bekliyor") { x.DabDurumu = "Gerek Yok"; x.DabNotu = Temiz("Ek-2 ülkesi (bedel getirme zorunlu değil) · otomatik " + x.DabNotu); } }
                else if (x.Ulke != "" && UlkeYarim.Contains(x.Ulke) && d.UlkeOran >= 100) x.UlkeOran = 50;
                // eşik: ABD doları karşılığı 15.000 ve altı → DAB / İBKB gerekmez (otomatik; kullanıcı başka durum seçtiyse dokunulmaz)
                if (x.DabDurumu == "Bekliyor" && EsikAlti(x)) { x.DabDurumu = "Gerek Yok"; x.DabNotu = Temiz($"Eşik altı (≤ {EsikUsd:N0} $ karşılığı) · otomatik " + x.DabNotu); }
                Yaz(); kayit = x;
            }
            return null;
        }
        public static string Sil(int id) { Yukle(); lock (Kilit) { var x = _depo.Dosyalar.FirstOrDefault(y => y.Id == id); if (x == null) return "Kayıt bulunamadı."; if (_depo.Tahsilatlar.Any(t => t.DosyaId == id)) return "Bu dosyaya bağlı banka tahsilatı var; önce eşleşmeyi kaldırın."; _depo.Dosyalar.Remove(x); Yaz(); } return null; }

        // ---------- banka tahsilatları (11 Kas 2026: "tahsilat raporu bankalardan otomatik geliyor; hangi ihracat belge numarası ile eşleşecek seçimi") ----------
        public static string TahsilatKaydet(IhracatTahsilatIstek d, string kullanici, out IhracatTahsilat kayit)
        {
            kayit = null; if (d == null) return "Veri yok."; Yukle();
            if (Temiz(d.Sirket) == "") return "Şirket seçilmelidir."; if (d.Tutar <= 0) return "Tutar sıfırdan büyük olmalıdır.";
            lock (Kilit)
            {
                var x = d.Id > 0 ? _depo.Tahsilatlar.FirstOrDefault(y => y.Id == d.Id) : null; if (d.Id > 0 && x == null) return "Kayıt bulunamadı.";
                if (x != null && x.Durum != "Eşleşmedi") return "Eşleşmiş tahsilat düzenlenemez; önce eşleşmeyi kaldırın.";
                if (x == null) { x = new IhracatTahsilat { Id = _depo.Tahsilatlar.Select(y => y.Id).DefaultIfEmpty(0).Max() + 1, Olusturma = DateTime.Today, Olusturan = kullanici ?? "", Kaynak = "Elle" }; _depo.Tahsilatlar.Add(x); }
                x.Sirket = Temiz(d.Sirket); x.Banka = Temiz(d.Banka); x.Tarih = Tarih(d.Tarih); x.Gonderen = Temiz(d.Gonderen); x.MusteriKodu = Temiz(d.MusteriKodu); x.Tutar = R2(d.Tutar); x.Doviz = Temiz(d.Doviz).ToUpperInvariant() == "" ? "USD" : Temiz(d.Doviz).ToUpperInvariant(); x.Referans = Temiz(d.Referans); x.Aciklama = Temiz(d.Aciklama);
                Yaz(); kayit = x;
            }
            return null;
        }
        public static string TahsilatSil(int id) { Yukle(); lock (Kilit) { var x = _depo.Tahsilatlar.FirstOrDefault(y => y.Id == id); if (x == null) return "Kayıt bulunamadı."; if (x.Durum != "Eşleşmedi") return "Eşleşmiş tahsilat silinemez; önce eşleşmeyi kaldırın."; _depo.Tahsilatlar.Remove(x); Yaz(); } return null; }
        /// <summary>Tahsilat ↔ ihracat dosyası eşleşmesi: dosyaya DAB satırı yazılır, DAB durumu güncellenir (gereken karşılandıysa Yapıldı, yoksa Kısmi); istenirse İBKB belgesi üretilir.</summary>
        public static string Eslestir(EslestirIstek d, string kullanici, out IbkbBelge belge)
        {
            belge = null; if (d == null) return "Veri yok."; Yukle();
            lock (Kilit)
            {
                var t = _depo.Tahsilatlar.FirstOrDefault(y => y.Id == d.TahsilatId); if (t == null) return "Tahsilat bulunamadı.";
                if (t.Durum != "Eşleşmedi") return "Tahsilat zaten eşleşmiş.";
                var x = _depo.Dosyalar.FirstOrDefault(y => y.Id == d.DosyaId); if (x == null) return "İhracat dosyası bulunamadı.";
                if (x.DabDurumu == "İptal") return "İptal dosyaya eşleştirilemez.";
                x.Dablar.Add(new IbkbDab { Tutar = t.Tutar, Doviz = t.Doviz, Tarih = t.Tarih, Banka = t.Banka, No = t.Referans, Not = "Banka tahsilatı #" + t.Id + (t.Gonderen != "" ? " · " + t.Gonderen : "") });
                if (string.IsNullOrEmpty(x.DabBanka)) x.DabBanka = t.Banka;
                if (x.DabDurumu == "Bekliyor" || x.DabDurumu == "Kısmi" || x.DabDurumu == "Gerek Yok") { x.DabKalan = 0; x.DabDurumu = "Kısmi"; if (Kalan(x) <= 0.005m) x.DabDurumu = "Yapıldı"; }
                x.Guncelleme = DateTime.Now; x.Guncelleyen = kullanici ?? "";
                t.Durum = "Eşleşti"; t.DosyaId = x.Id; t.EslestirmeTarihi = DateTime.Today; t.Eslestiren = kullanici ?? "";
                if (d.IbkbOlustur)
                {
                    var kart = string.IsNullOrEmpty(x.MusteriKodu) ? null : CariKartOrnek.KartBul(x.MusteriKodu);
                    int yil = DateTime.Today.Year, sira = _depo.Belgeler.Count(b => b.Tarih.Year == yil) + 1;
                    belge = new IbkbBelge
                    {
                        Id = _depo.Belgeler.Select(b => b.Id).DefaultIfEmpty(0).Max() + 1, No = $"İBKB-{yil}-{sira:0000}", Tarih = DateTime.Today, Sirket = x.Sirket, Banka = t.Banka,
                        DosyaId = x.Id, DosyaNo = x.DosyaNo, BeyannameNo = x.BeyannameNo, FaturaNo = x.FaturaNo, MusteriKodu = x.MusteriKodu, Musteri = x.Musteri, Ulke = x.Ulke != "" ? x.Ulke : (kart?.Ulke ?? ""),
                        BeyannameTutar = x.Tutar, BeyannameDoviz = x.Doviz, Tutar = t.Tutar, Doviz = t.Doviz, TahsilatId = t.Id, TahsilatRef = t.Referans, TahsilatTarihi = t.Tarih, FiiliIhrac = x.Baslangic,
                        Durum = "Hazırlandı", Olusturma = DateTime.Now, Olusturan = kullanici ?? ""
                    };
                    _depo.Belgeler.Add(belge); t.Durum = "İBKB Düzenlendi"; t.IbkbBelgeId = belge.Id;
                }
                Yaz();
            }
            return null;
        }
        /// <summary>Eşleşmeyi kaldır: dosyadaki DAB satırı silinir, belge iptal olur, tahsilat yeniden Eşleşmedi.</summary>
        public static string EslesmeKaldir(int tahsilatId, string kullanici)
        {
            Yukle();
            lock (Kilit)
            {
                var t = _depo.Tahsilatlar.FirstOrDefault(y => y.Id == tahsilatId); if (t == null) return "Tahsilat bulunamadı."; if (t.Durum == "Eşleşmedi") return "Tahsilat zaten eşleşmemiş.";
                var x = _depo.Dosyalar.FirstOrDefault(y => y.Id == t.DosyaId);
                if (x != null) { x.Dablar.RemoveAll(p => p.Not != null && p.Not.StartsWith("Banka tahsilatı #" + t.Id)); if (x.DabDurumu == "Yapıldı" || x.DabDurumu == "Kısmi") { x.DabDurumu = x.Dablar.Count == 0 ? "Bekliyor" : (Kalan(x) <= 0.005m ? "Yapıldı" : "Kısmi"); } x.Guncelleme = DateTime.Now; x.Guncelleyen = kullanici ?? ""; }
                var b = _depo.Belgeler.FirstOrDefault(y => y.Id == t.IbkbBelgeId); if (b != null && b.Durum != "Banka Onayladı") b.Durum = "İptal";
                t.Durum = "Eşleşmedi"; t.DosyaId = 0; t.IbkbBelgeId = 0; t.EslestirmeTarihi = null; t.Eslestiren = "";
                Yaz();
            }
            return null;
        }
        public static string BelgeDurum(IbkbBelgeIstek d, string kullanici)
        {
            if (d == null) return "Veri yok."; Yukle();
            lock (Kilit)
            {
                var b = _depo.Belgeler.FirstOrDefault(y => y.Id == d.Id); if (b == null) return "Belge bulunamadı.";
                b.Durum = Sec(d.Durum, BelgeDurumlari, b.Durum); b.BankaIbkbNo = Temiz(d.BankaIbkbNo); b.BankaTarihi = Tarih(d.BankaTarihi); b.Not = Temiz(d.Not);
                Yaz();
            }
            return null;
        }
        /// <summary>Yalnız İptal durumundaki belge silinir (düzenlenmiş / bankaya gitmiş belge iz bırakır).</summary>
        public static string BelgeSil(int id) { Yukle(); lock (Kilit) { var b = _depo.Belgeler.FirstOrDefault(y => y.Id == id); if (b == null) return "Belge bulunamadı."; if (b.Durum != "İptal") return "Yalnız iptal edilmiş belge silinebilir."; _depo.Belgeler.Remove(b); foreach (var t in _depo.Tahsilatlar.Where(t => t.IbkbBelgeId == id)) t.IbkbBelgeId = 0; Yaz(); } return null; }
        public static IbkbBelge BelgeBul(int id) { Yukle(); lock (Kilit) return _depo.Belgeler.FirstOrDefault(b => b.Id == id); }
        public static IbkbDosya DosyaBul(int id) { Yukle(); lock (Kilit) return _depo.Dosyalar.FirstOrDefault(b => b.Id == id); }

        // ---------- paket ----------
        public static object Paket()
        {
            Yukle();
            lock (Kilit)
            {
                var L = _depo.Dosyalar.OrderBy(x => x.Sirket).ThenByDescending(x => x.Yil).ThenBy(x => x.DosyaNo, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false)).ToList();
                return new
                {
                    success = true, bugun = DateTime.Today.ToString("yyyy-MM-dd"), kalici = Dosya != null, vadeGun = VadeGun, esikUsd = EsikUsd, usdKur = UsdKur(), ulkeMuaf = UlkeMuaf, ulkeYarim = UlkeYarim,
                    sirketler = Sirketler.Concat(SatisSiparisOrnek.Sirketler.Select(s => s.Ad)).Distinct().ToList(), dabDurumlari = DabDurumlari, takipDurumlari = TakipDurumlari, dovizler = Dovizler, tahsilatDurumlari = TahsilatDurumlari, belgeDurumlari = BelgeDurumlari, tahsilatKaynaklari = TahsilatKaynaklari, farkNedenleri = FarkNedenleri,
                    yillar = L.Select(x => x.Yil).Distinct().OrderByDescending(x => x).ToList(),
                    bankalar = L.SelectMany(x => new[] { x.DabBanka }.Concat(x.Dablar.Select(p => p.Banka))).Concat(_depo.Tahsilatlar.Select(t => t.Banka)).Where(b => !string.IsNullOrEmpty(b)).Distinct().OrderBy(b => b).ToList(),
                    // İhracat Satışları: Yurtdışı Satış Siparişi'nden canlı (yüklenmiş / faturalanmış siparişler); dosya bağı fatura no ya da sipariş no ile, yoksa cari + tutar yakınlığı ile
                    satislar = SatisSiparisOrnek.Liste("Yurtdışı").Where(s => s.Durum == "Yüklendi" || s.Durum == "Faturalandı").Select(s =>
                    {
                        var dsy = L.FirstOrDefault(x => (!string.IsNullOrEmpty(s.FaturaNo) && string.Equals(x.FaturaNo, s.FaturaNo, StringComparison.OrdinalIgnoreCase)) || (!string.IsNullOrEmpty(s.No) && string.Equals(x.SiparisNo, s.No, StringComparison.OrdinalIgnoreCase)))
                               ?? L.FirstOrDefault(x => x.DabDurumu != "İptal" && !string.IsNullOrEmpty(x.MusteriKodu) && x.MusteriKodu == s.CariKodu && Math.Abs(x.Tutar - s.Tutar) < 0.01m && string.Equals(x.Doviz, s.ParaBirimi, StringComparison.OrdinalIgnoreCase));
                        return new { s.Id, s.No, s.Durum, tarih = D(s.Tarih), s.FaturaNo, faturaTarihi = D(s.FaturaTarihi), yukleme = D(s.YuklendiTarihi ?? s.YuklemeTarihi), sirket = s.FaturaKesen, s.CariKodu, s.Cari, s.Ulke, s.Temsilci, s.OdemeSekli, s.TeslimSekli, tutar = s.Tutar, doviz = s.ParaBirimi == "TL" ? "TRY" : s.ParaBirimi, tutarTl = s.TutarTl, kg = s.ToplamKg, dosyaId = dsy?.Id ?? 0, dosyaNo = dsy?.DosyaNo ?? "", dosyaTakip = dsy == null ? "" : Takip(dsy), esikAlti = s.Tutar * (UsdKur().TryGetValue(s.ParaBirimi, out var r) ? r : 1m) <= EsikUsd };
                    }).ToList(),
                    tahsilatlar = _depo.Tahsilatlar.OrderByDescending(t => t.Tarih).ThenByDescending(t => t.Id).Select(t => { var dsy = L.FirstOrDefault(x => x.Id == t.DosyaId); return new { t.Id, t.Sirket, t.Banka, tarih = D(t.Tarih), t.Gonderen, t.MusteriKodu, t.Tutar, t.Doviz, t.Referans, t.Aciklama, t.Kaynak, t.Durum, t.DosyaId, dosyaNo = dsy?.DosyaNo ?? "", beyannameNo = dsy?.BeyannameNo ?? "", t.IbkbBelgeId, belgeNo = _depo.Belgeler.FirstOrDefault(b => b.Id == t.IbkbBelgeId)?.No ?? "", eslestirme = D(t.EslestirmeTarihi), t.Eslestiren, olusturma = D(t.Olusturma), t.Olusturan }; }).ToList(),
                    belgeler = _depo.Belgeler.OrderByDescending(b => b.Tarih).ThenByDescending(b => b.Id).Select(b => new { b.Id, b.No, tarih = D(b.Tarih), b.Sirket, b.Banka, b.DosyaId, b.DosyaNo, b.BeyannameNo, b.FaturaNo, b.MusteriKodu, b.Musteri, b.Ulke, b.BeyannameTutar, b.BeyannameDoviz, b.Tutar, b.Doviz, b.TahsilatId, b.TahsilatRef, tahsilatTarihi = D(b.TahsilatTarihi), fiiliIhrac = D(b.FiiliIhrac), b.Durum, b.BankaIbkbNo, bankaTarihi = D(b.BankaTarihi), b.Not, olusturma = b.Olusturma.ToString("yyyy-MM-dd HH:mm"), b.Olusturan }).ToList(),
                    dosyalar = L.Select(x => new
                    {
                        x.Id, x.Sirket, x.Yil, x.DosyaNo, x.Aciklama, x.MusteriKodu, x.Musteri, ulke = x.Ulke != "" ? x.Ulke : (string.IsNullOrEmpty(x.MusteriKodu) ? "" : CariKartOrnek.KartBul(x.MusteriKodu)?.Ulke ?? ""), ulkeKaynak = x.Ulke != "" ? "kayıt" : "cari kart", x.FaturaNo, x.SiparisNo, baslangic = D(x.Baslangic), kapanis = D(x.Kapanis), x.BeyannameNo, x.Doviz, x.Tutar, x.UlkeOran,
                        tahsilatSayisi = _depo.Tahsilatlar.Count(t => t.DosyaId == x.Id), belgeSayisi = _depo.Belgeler.Count(b => b.DosyaId == x.Id && b.Durum != "İptal"),
                        x.DabDurumu, x.DabBanka, x.DabKalan, x.DabNotu, dablar = x.Dablar.Select(p => new { p.Tutar, p.Doviz, tarih = D(p.Tarih), p.Banka, p.No, p.Not }).ToList(),
                        gereken = Gereken(x), getirilen = Getirilen(x), kalan = Kalan(x), vade = D(Vade(x)), kalanGun = KalanGun(x), takip = Takip(x), usdKarsiligi = UsdKarsiligi(x), esikAlti = EsikAlti(x),
                        x.GercekBedel, x.FarkNedeni, gercekBedelEtkin = GercekBedelEtkin(x), bedelFarki = BedelFarki(x), gerekenGercek = GerekenGercek(x), kalanGercek = KalanGercek(x), beyannameFazlasi = BeyannameFazlasi(x),
                        x.Not, x.Kaynak, olusturma = D(x.Olusturma), x.Olusturan, guncelleme = x.Guncelleme?.ToString("yyyy-MM-dd HH:mm"), x.Guncelleyen
                    }).ToList()
                };
            }
        }
    }
}
