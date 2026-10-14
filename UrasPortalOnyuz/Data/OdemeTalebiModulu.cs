// ============================================================
// ÖDEME TALEBİ MODÜLÜ — Talep › Yeni Talep › Ödeme Talebi (talep motoru: Data/TalepMotoru.cs; ekran Views/IkTalep/Modul.cshtml + js/talep-modulu.js;
// sayfa Controllers/OdemeTalebiController.cs; veri uçları IkTalep/Tm*). Onaylanan talep Kasa Nakit Akışı › Yeni Ödeme › Departman Talepleri'ne
// (Data/OdemeTalepOrnek.cs) düşer; finans "Ekle" deyince nakit akış satırı olur, ödeme gerçekleşince talepte "Ödendi" görünür.
//
// CARİ KAPSAMI (kim hangi cariye ödeme talebi açabilir):
//   Satın Alma   → satınalmacısı olduğu şirketin tedarikçileri (Data/AlimOnayOrnek.cs Satinalmacilar) + cari kartta temsilcisi olduğu tedarikçiler
//   İdari İşler  → cari kartta temsilcisi olduğu cariler (kira, temizlik, güvenlik, danışmanlık …)
//   Muhasebe     → resmi kurumlar (vergi dairesi, SGK, gümrük) ve finansal kuruluşlar — konular: KDV, muhtasar, SGK primi …
//   Finans       → finansal kuruluşlar (banka, leasing, faktoring, sigorta) ve resmi kurumlar — konular: kredi taksidi, kart ekstresi, masraf …
//   Yönetici / Üst Yönetim (ya da portal Yönetici yetkisi) → tüm cariler; diğer birimler → yalnız temsilcisi olduğu cariler.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public static class OdemeTalebiModulu
    {
        public const string Kod = "ODEME";
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private static string N(decimal d, int b = 2) => d.ToString("N" + b, Tr);
        private static TmAlan A(string ad, string etiket, string tip = "text", bool zorunlu = false, int col = 4, string[] sec = null, string ipucu = null, string vars = null, string kaynak = null, string anahtar = null, Dictionary<string, string> doldur = null)
            => new TmAlan { Ad = ad, Etiket = etiket, Tip = tip, Zorunlu = zorunlu, Col = col, Secenekler = sec, Ipucu = ipucu, Varsayilan = vars, Kaynak = kaynak, Anahtar = anahtar, Doldur = doldur };
        private static TmTur T(string kod, string ad, string akis, string aciklama) => new TmTur { Kod = kod, Ad = ad, Akis = akis, Aciklama = aciklama };
        private static TmOlay O(string kod, string ad, string kaynak, string aciklama, params string[] girdiler) => new TmOlay { Kod = kod, Ad = ad, Kaynak = kaynak, Aciklama = aciklama, Girdiler = girdiler };
        private static TmKpi K(string renk, string baslik, string deger, string alt, string etiket) => new TmKpi { Renk = renk, Baslik = baslik, Deger = deger, Alt = alt, Etiket = etiket };
        private static string G(Dictionary<string, string> g, string k) => g != null && g.TryGetValue(k, out var v) ? (v ?? "").Trim() : "";

        public static readonly string[] ParaBirimleri = { "TRY", "USD", "EUR", "GBP" };
        public static readonly string[] Yontemler = { "Havale / EFT", "Çek", "Senet", "Nakit", "Kredi Kartı / Banka Kartı" };
        public static readonly string[] BelgeTipleri = { "Fatura", "Sipariş", "Cari Bakiye", "Vergi", "SGK", "Gümrük", "Kira", "Kredi Taksidi", "Kredi Kartı Ekstresi", "Sigorta", "Diğer" };
        public static readonly string[] Oncelikler = { "Normal", "Acil", "Çok Acil" };
        /// <summary>Birime göre konu listesi ("*": diğer birimler).</summary>
        public static readonly Dictionary<string, string[]> Konular = new Dictionary<string, string[]>
        {
            ["Satın Alma"] = new[] { "Hammadde Alımı", "Ambalaj / Yardımcı Malzeme", "Yedek Parça / Bakım", "Hizmet Alımı", "Sipariş Avansı (Ön Ödeme)", "Sarf Malzeme", "Lojistik / Nakliye", "Diğer Satın Alma" },
            ["İdari İşler"] = new[] { "Kira", "Aidat / Ortak Gider", "Temizlik / Güvenlik Hizmeti", "Ofis Giderleri", "Abonelik (Telefon / İnternet / Enerji)", "Araç / Servis", "Yemek Hizmeti", "Danışmanlık / Denetim", "Diğer İdari Gider" },
            ["Muhasebe"] = new[] { "KDV Beyannamesi", "Muhtasar ve Prim Hizmet Beyannamesi", "Geçici Vergi", "Kurumlar Vergisi", "Damga Vergisi", "SGK Primi", "Gümrük Vergisi / KKDF", "Harç / Noter / Resmi Ücret", "Mali Müşavirlik / Denetim", "Diğer Muhasebe" },
            ["Finans"] = new[] { "Kredi Taksidi", "Kredi Kartı Ekstresi", "Banka Masrafı / Komisyon", "Faktoring / Leasing", "Sigorta Primi", "Teminat Mektubu Komisyonu", "Grup İçi Borç Ödemesi", "Diğer Finansman" },
            ["*"] = new[] { "Fatura Ödemesi", "Hizmet Alımı", "Diğer" }
        };
        public static readonly Dictionary<string, string> KapsamKurallari = new Dictionary<string, string>
        {
            ["Satın Alma"] = "Satınalmacısı olduğunuz şirketin tedarikçileri ve cari kartta temsilcisi olduğunuz tedarikçiler",
            ["İdari İşler"] = "Cari kartta temsilcisi olduğunuz cariler (kira, temizlik, güvenlik, danışmanlık …)",
            ["Muhasebe"] = "Resmi kurumlar (vergi dairesi, SGK, gümrük) ve finansal kuruluşlar",
            ["Finans"] = "Finansal kuruluşlar (banka, leasing, faktoring, sigorta) ve resmi kurumlar",
            ["Yönetici"] = "Yönetici / üst yönetim: tüm cariler",
            ["*"] = "Yalnız cari kartta temsilcisi olduğunuz cariler (satın alma kapsamı için satın alma birimine iletin)"
        };

        static string[] SirketAdlari => CekAkibetOrnek.Sirketler.Select(s => s.Ad).ToArray();
        static bool Yonetici(NaKisi k) => k != null && (k.Rol == "Yönetici" || k.Rol == "Üst Yönetim" || OrnekVeri.Kullanicilar.Any(u => string.Equals(u.Kod, k.KullaniciId, StringComparison.OrdinalIgnoreCase) && u.Yetki == "A"));
        static bool Odenebilir(CariKart x) => x.KartTipi == "Tedarikçi" || x.KartTipi == "Finansal Kuruluşlar" || x.KartTipi == "Resmi Kurumlar";
        static bool Temsilci(CariKart x, NaKisi k) => x.Faaliyetler.Any(f => string.Equals(f.SatisTemsilcisi, k.AdSoyad, StringComparison.OrdinalIgnoreCase));
        public static string KapsamKurali(NaKisi k) => k == null ? "" : Yonetici(k) ? KapsamKurallari["Yönetici"] : KapsamKurallari.TryGetValue(k.Departman ?? "", out var s) ? s : KapsamKurallari["*"];
        public static string[] KisiKonulari(NaKisi k) => k == null ? Konular["*"] : Yonetici(k) ? Konular.Values.SelectMany(x => x).Distinct().ToArray() : Konular.TryGetValue(k.Departman ?? "", out var l) ? l : Konular["*"];

        /// <summary>Kişinin ödeme talebi açabileceği cariler (yukarıdaki kapsam kuralı).</summary>
        public static List<CariKart> Kapsam(NaKisi k)
        {
            if (k == null) return new List<CariKart>();
            var kartlar = CariKartOrnek.Kartlar.Where(Odenebilir).ToList();
            if (Yonetici(k)) return kartlar;
            switch (k.Departman)
            {
                case "Satın Alma":
                    var sirketler = AlimOnayOrnek.Satinalmacilar.Where(s => string.Equals(s.Ad, k.AdSoyad, StringComparison.OrdinalIgnoreCase)).Select(s => s.Sirket).ToList(); if (sirketler.Count == 0) sirketler.Add(k.Sirket);
                    return kartlar.Where(x => x.KartTipi == "Tedarikçi" && (sirketler.Contains(x.Sirket) || Temsilci(x, k))).ToList();
                case "Muhasebe": return kartlar.Where(x => x.KartTipi == "Resmi Kurumlar" || x.KartTipi == "Finansal Kuruluşlar").ToList();
                case "Finans": return kartlar.Where(x => x.KartTipi == "Finansal Kuruluşlar" || x.KartTipi == "Resmi Kurumlar").ToList();
                default: return kartlar.Where(x => Temsilci(x, k)).ToList();   // İdari İşler ve diğer birimler: temsilcisi olduğu cariler
            }
        }
        static string KapsamOzeti(NaKisi k) { var l = Kapsam(k); return $"{(Yonetici(k) ? "Yönetici" : k.Departman)} · {KapsamKurali(k)} · {l.Count} cari" + (l.Count == 0 ? " — kapsamınızda cari yok" : ""); }
        static int VadeGunu(string vadeSarti) { var m = System.Text.RegularExpressions.Regex.Match(vadeSarti ?? "", @"(\d+)"); return m.Success ? int.Parse(m.Groups[1].Value) : 30; }

        /// <summary>İstemciye giden canlı master: kişi bazında cari kapsamı, birim bazında konular, cari bazında açık faturalar (Cari Mutabakat ekstresi).</summary>
        static Dictionary<string, object> MasterCanli()
        {
            var bugun = DateTime.Today;
            var cariKapsam = new Dictionary<string, object>(); var ozet = new Dictionary<string, object>();
            foreach (var k in NakitAvansOrnek.Personel)
            {
                cariKapsam[k.PersonelNo] = Kapsam(k).OrderBy(x => x.KartTipi == "Tedarikçi" ? 0 : x.KartTipi == "Resmi Kurumlar" ? 1 : 2).ThenBy(x => x.KisaAd, StringComparer.Create(Tr, false))
                    .Select(x => new { deger = x.KisaAd, etiket = $"{x.KisaAd} · {x.Kod} · {x.KartTipi} · {x.Sirket}", ek = new { kod = x.Kod, kartTipi = x.KartTipi, sirket = x.Sirket, vadeSarti = x.VadeSarti ?? "", sonOdeme = bugun.AddDays(VadeGunu(x.VadeSarti)).ToString("yyyy-MM-dd"), temsilci = string.Join(", ", MutabakatOrnek.Temsilciler(x)) } }).ToList();
                ozet[k.PersonelNo] = KapsamOzeti(k);
            }
            var konular = new Dictionary<string, object>(); foreach (var kv in Konular) konular[kv.Key] = kv.Value.Select(x => new { deger = x, etiket = x }).ToList();
            foreach (var k in NakitAvansOrnek.Personel.Where(Yonetici)) konular[k.PersonelNo] = KisiKonulari(k).Select(x => new { deger = x, etiket = x }).ToList();   // yöneticide tüm konular (kişi anahtarı birimden önce aranır)
            var acik = new Dictionary<string, object>();
            foreach (var c in MutabakatOrnek.Cariler.Where(c => c.Kart != null && Odenebilir(c.Kart)))
            {
                var l = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Kalan > 0 && h.KapamaDurumu != "Kapalı").OrderBy(h => h.Vade ?? h.Tarih)
                    .Select(h =>
                    {
                        string pb = h.ParaBirimi == "TL" || string.IsNullOrEmpty(h.ParaBirimi) ? "TRY" : h.ParaBirimi; var vade = h.Vade ?? h.Tarih.AddDays(30); int gecikme = (bugun - vade).Days;
                        string gec = gecikme > 0 ? " (" + gecikme + " g gecikmiş)" : "";
                        return new { deger = h.BelgeNo, etiket = h.BelgeNo + " · " + N(h.Kalan) + " " + pb + " · vade " + vade.ToString("dd.MM.yyyy") + gec, ek = new { belgeNo = h.BelgeNo, tutar = h.Kalan.ToString("0.00", CultureInfo.InvariantCulture), pb, sonOdeme = vade.ToString("yyyy-MM-dd"), belgeTipi = "Fatura", aciklama = h.Aciklama } };
                    }).ToList();
                if (l.Count > 0) acik[c.Kart.KisaAd] = l;
            }
            return new Dictionary<string, object> { ["cariKapsam"] = cariKapsam, ["cariKapsamOzet"] = ozet, ["konular"] = konular, ["acikFaturalar"] = acik, ["kapsamKurallari"] = KapsamKurallari };
        }

        public static TmModul Modul()
        {
            var m = new TmModul
            {
                Kod = Kod, Ad = "Ödeme Talebi", NoOnEk = "ÖT", Ikon = "fa-money-bill-transfer", Renk = "#0f766e",
                Aciklama = "Birimlerin ödeme talebi: satın alma satınalmacısı olduğu tedarikçiler, idari işler temsilcisi olduğu cariler, muhasebe ve finans konularına göre resmi kurumlar ve finansal kuruluşlar için talep açar. Onaylanan talep Kasa Nakit Akışı › Yeni Ödeme › Departman Talepleri'ne düşer; finans nakit akışa alır, ödeme gerçekleşince talepte görünür.",
                AkisOzeti = "Talep → (Yönetici) → (Direktör) → Finans → Kasa Nakit Akışı (Departman Talepleri) → Nakit Akış Satırı → Ödeme",
                KullananEtiket = "Talep eden personel (birim ve cari kapsamı bu kişiye göre)", Durum2Ad = "Nakit Akış Durumu (Finans)", Durum3Ad = "Ödeme Durumu (Kasa Nakit Akışı)",
                Turler = { T("FATURA", "Tedarikçi Faturası", "Y,F", "Tedarikçi faturasının ödenmesi; açık fatura listesinden seçilebilir."), T("SIPARIS", "Sipariş Avansı / Ön Ödeme", "Y,F", "Sipariş öncesi avans ya da peşin ödeme indirimi için ön ödeme."), T("BAKIYE", "Cari Bakiye Ödemesi", "Y,F", "Carinin açık bakiyesine toplu ödeme."),
                           T("RESMI", "Vergi / SGK / Resmi Ödeme", "F", "Beyanname, prim, gümrük ve harç ödemeleri (muhasebe hazırlar, finans onaylar)."), T("FINANSAL", "Kredi / Kart / Banka / Sigorta", "F,D", "Kredi taksidi, kart ekstresi, banka masrafı, leasing, sigorta primi; Direktör onayı eklenir."), T("KIRA", "Kira / Aidat / Abonelik", "D,F", "Kira, aidat ve abonelik ödemeleri; Direktör ve Finans onayı."), T("DIGER", "Diğer Ödeme", "Y,F", "Listedekilere girmeyen ödemeler.") },
                Alanlar = { A("sirket", "Ödeyen Şirket (Fatura Sahibi)", "select", true, 3, SirketAdlari, vars: "@kullanan.sirket"),
                            A("cari", "Cari (Ödenecek)", "bagli", true, 5, ipucu: "Liste talep eden kişinin birimine ve temsilciliğine göre süzülür", kaynak: "cariKapsam", anahtar: "personel"),
                            A("konu", "Konu", "bagli", true, 4, ipucu: "Birime göre konu listesi", kaynak: "konular", anahtar: "departman"),
                            A("fatura", "Açık Fatura (seçilirse belge, tutar ve vade dolar)", "bagli", false, 6, ipucu: "Cari Mutabakat ekstresindeki kapanmamış faturalar", kaynak: "acikFaturalar", anahtar: "alan:cari", doldur: new Dictionary<string, string> { ["belgeNo"] = "belgeNo", ["tutar"] = "tutar", ["pb"] = "pb", ["sonOdeme"] = "sonOdeme", ["belgeTipi"] = "belgeTipi" }),
                            A("belgeTipi", "Belge Tipi", "select", true, 3, BelgeTipleri, vars: "Fatura"), A("belgeNo", "Belge No", "text", false, 3, ipucu: "Fatura / sipariş / beyanname no"),
                            A("tutar", "Tutar", "number", true, 2), A("pb", "Para Birimi", "select", true, 2, ParaBirimleri, vars: "TRY"), A("sonOdeme", "Son Ödeme Tarihi", "date", true, 2),
                            A("yontem", "Önerilen Ödeme Yöntemi", "select", true, 3, Yontemler, vars: "Havale / EFT"), A("oncelik", "Öncelik", "select", true, 3, Oncelikler, vars: "Normal"),
                            A("aciklama", "Açıklama / Gerekçe", "textarea", true, 12, ipucu: "Ne için ödeniyor; sipariş, sözleşme ya da beyanname referansı") },
                Olaylar = { O("NakitAkisaAlindi", "NakitAkisaAlindi — Finans talebi nakit akışa aldı", "Kasa Nakit Akışı", "Kasa Nakit Akışı › Yeni Ödeme › Departman Talepleri'nde \"Ekle\": talep nakit akış satırı olur (nakit yöntemde satır onaya düşer)."),
                            O("Odendi", "Odendi — Ödeme gerçekleşti", "Kasa Nakit Akışı", "Nakit akış satırı ÖDENDİ oldu (talimat gerçekleşti / dekont).", "dekontNo|Dekont No|text") },
                HesapEtiketler = { ["tutarTL"] = "TL Karşılığı", ["cariKod"] = "Cari Kodu", ["kartTipi"] = "Cari Tipi", ["vadeSarti"] = "Cari Vade Şartı", ["kapsam"] = "Cari Kapsamı", ["onayci"] = "Birim Onaycısı" },
                EkEtiketler = { ["nakitAkisNo"] = "Nakit Akış Kaydı", ["alinmaTarihi"] = "Nakit Akışa Alınma", ["odemeTarihi"] = "Ödeme Tarihi", ["dekontNo"] = "Dekont No", ["odenenTutar"] = "Ödenen Tutar" },
                Parametreler = { ["direktorEsik"] = "500000", ["acilGun"] = "3", ["vadeUyariGun"] = "7" },
                ParametreEtiketler = { ["direktorEsik"] = "Bu TL tutarı aşan talepte Direktör onayı", ["acilGun"] = "Son ödemeye bu kadar gün kalmışsa acil uyarısı", ["vadeUyariGun"] = "Son ödeme bu kadar günden yakınsa vade uyarısı" },
                Kurallar = { "Cari kapsamı talep eden kişinin birimine göre süzülür: satın alma satınalmacısı olduğu tedarikçiler, idari işler temsilcisi olduğu cariler, muhasebe / finans resmi kurumlar ve finansal kuruluşlar; yönetici tüm cariler.", "Konu listesi birime göredir; belge tipi ve cari tipi talep türüyle uyumlu olmalıdır (ör. Vergi / SGK türünde resmi kurum).", "Aynı cari + belge no ile açık talep varsa mükerrer sayılır; nakit akışta planlı aynı belge uyarı verir.", "Eşiği aşan TL tutarda Direktör onayı eklenir; nakit yöntemde GMY nakit onayı Kasa Nakit Akışı'nda alınır.", "Onaylanan talep Kasa Nakit Akışı › Departman Talepleri'ne düşer; nakit akışa alınma ve ödeme bilgisi oradan gelir, elle girilmez." },
                TakipSutunlar = { ("Talep No", "no"), ("Talep Eden", "kullanan"), ("Birim", "departman"), ("Tür", "tur"), ("Şirket", "d.sirket"), ("Cari", "d.cari"), ("Konu", "d.konu"), ("Belge", "d.belgeTipi"), ("Belge No", "d.belgeNo"), ("Tutar", "d.tutar"), ("PB", "d.pb"), ("TL Karşılığı (TL)", "h.tutarTL"), ("Son Ödeme", "d.sonOdeme"), ("Yöntem", "d.yontem"), ("Öncelik", "d.oncelik"), ("Talep Durumu", "talepDurumu"), ("Nakit Akış", "durum2"), ("Ödeme", "durum3"), ("Nakit Akış Kaydı", "e.nakitAkisNo"), ("Yönetici", "onay.Yönetici Onayı"), ("Finans", "onay.Finans Onayı") },
                MasterCanli = MasterCanli
            };
            m.Hesapla = a =>
            {
                var s = new TmSonuc(); var m2 = a.Modul; var k = a.Kullanan;
                string cari = a.V("cari"), konu = a.V("konu"), belgeTipi = a.V("belgeTipi"), belgeNo = a.V("belgeNo"), pb = a.V("pb") == "" ? "TRY" : a.V("pb");
                a.Hesap["kapsam"] = KapsamKurali(k);
                if (cari == "") { s.Hata = "Cari seçilmelidir."; return s; }
                var kapsam = Kapsam(k); var kart = kapsam.FirstOrDefault(x => string.Equals(x.KisaAd, cari, StringComparison.OrdinalIgnoreCase));
                if (kart == null) { s.Hata = $"{cari}, {k.AdSoyad} ({(Yonetici(k) ? "Yönetici" : k.Departman)}) için cari kapsamında değil: {KapsamKurali(k)}."; return s; }
                a.Hesap["cariKod"] = kart.Kod; a.Hesap["kartTipi"] = kart.KartTipi; a.Hesap["vadeSarti"] = kart.VadeSarti ?? "—";
                if (!SirketAdlari.Contains(a.V("sirket"))) { s.Hata = "Ödeyen şirket seçilmelidir."; return s; }
                if (a.D("tutar") <= 0) { s.Hata = "Tutar sıfırdan büyük olmalıdır."; return s; }
                if (!ParaBirimleri.Contains(pb)) { s.Hata = "Para birimi geçersiz."; return s; }
                var sonOdeme = a.T("sonOdeme"); if (sonOdeme == null) { s.Hata = "Son ödeme tarihi zorunludur."; return s; }
                decimal kur = NakitAkisOrnek.Kurlar.TryGetValue(pb, out var kk) ? kk : 1m, tl = Math.Round(a.D("tutar") * kur, 2);
                a.Hesap["tutarTL"] = N(tl); a.Hesap["tutarTLSayi"] = tl.ToString("0.00", CultureInfo.InvariantCulture);
                a.Hesap["onayci"] = a.Tur.Akis.Contains("Y") ? NakitAvansOrnek.Yonetici(k).AdSoyad + " (yönetici) → " + NakitAvansOrnek.Finans().AdSoyad + " (finans)" : NakitAvansOrnek.Finans().AdSoyad + " (finans)";
                var konular = KisiKonulari(k); if (konu != "" && !konular.Contains(konu)) s.Uyarilar.Add($"\"{konu}\" {(Yonetici(k) ? "konu listesinde" : k.Departman + " birimi konu listesinde")} yok.");
                // tür ↔ belge tipi ↔ cari tipi uyumu
                if (a.TurKod == "RESMI" && kart.KartTipi != "Resmi Kurumlar") s.Uyarilar.Add("Vergi / SGK / Resmi Ödeme türünde cari bir resmi kurum olmalıdır.");
                if (a.TurKod == "RESMI" && belgeTipi != "Vergi" && belgeTipi != "SGK" && belgeTipi != "Gümrük") s.Uyarilar.Add("Resmi ödemede belge tipi Vergi, SGK ya da Gümrük olmalıdır.");
                if (a.TurKod == "FINANSAL" && kart.KartTipi != "Finansal Kuruluşlar") s.Uyarilar.Add("Kredi / kart / banka / sigorta ödemesinde cari bir finansal kuruluş olmalıdır.");
                if ((a.TurKod == "FATURA" || a.TurKod == "SIPARIS" || a.TurKod == "BAKIYE") && kart.KartTipi != "Tedarikçi") s.Uyarilar.Add("Tedarikçi ödemesi türlerinde cari bir tedarikçi olmalıdır.");
                if (a.TurKod == "KIRA" && belgeTipi != "Kira") s.Uyarilar.Add("Kira türünde belge tipi Kira seçilmelidir.");
                if (a.TurKod == "FATURA" && belgeNo == "") s.Uyarilar.Add("Fatura ödemesinde belge no beklenir (açık fatura listesinden seçilebilir).");
                // mükerrer: aynı cari + belge no ile açık talep; nakit akışta planlı aynı belge
                if (belgeNo != "")
                {
                    if (TalepMotoru.Talepler.Any(t => t.ModulKod == Kod && t.Id != a.Id && !t.Iptal && t.TalepDurumu != "Reddedildi" && string.Equals(t.V("cari"), cari, StringComparison.OrdinalIgnoreCase) && string.Equals(t.V("belgeNo"), belgeNo, StringComparison.OrdinalIgnoreCase))) { s.Hata = $"{cari} · {belgeNo} için açık ödeme talebi zaten var (mükerrer)."; return s; }
                    if (OdemeTalepOrnek.Listele(null).Any(t => t.No != a.No && string.Equals(t.Kurum, cari, StringComparison.OrdinalIgnoreCase) && string.Equals(t.BelgeNo, belgeNo, StringComparison.OrdinalIgnoreCase))) s.Uyarilar.Add($"{belgeNo} için departman talebi zaten Kasa Nakit Akışı'nda bekliyor.");
                    if (CekAkibetOrnek.Sirketler.Any(x => NakitAkisOrnek.Odemeler(x.DbName).Any(o => o.U_Durum != "ÖDENDİ" && string.Equals(o.U_FaturaSiparisNo, belgeNo, StringComparison.OrdinalIgnoreCase)))) s.Uyarilar.Add($"{belgeNo} nakit akışta zaten planlı görünüyor; mükerrer ödeme kontrolü yapın.");
                }
                int gun = (sonOdeme.Value.Date - DateTime.Today).Days;
                if (gun < 0) s.Uyarilar.Add($"Son ödeme tarihi {Math.Abs(gun)} gün geçmiş; gecikme faizi / cezası olabilir.");
                else if (gun <= m2.P("acilGun") && a.V("oncelik") == "Normal") s.Uyarilar.Add($"Son ödemeye {gun} gün var; öncelik Acil olmalı.");
                else if (gun <= m2.P("vadeUyariGun")) s.Uyarilar.Add($"Son ödemeye {gun} gün var; finans planlaması için erken iletin.");
                if (tl > m2.P("direktorEsik")) { s.EkAdimlar.Add("D"); s.Uyarilar.Add($"TL karşılığı {N(tl, 0)} TL, {N(m2.P("direktorEsik"), 0)} TL eşiğini aşıyor: Direktör onayı eklendi."); }
                if (a.V("yontem") == "Nakit") s.Uyarilar.Add("Nakit ödeme: Kasa Nakit Akışı'nda GMY nakit onayı ve kasa tediye makbuzu gerekir.");
                if (a.V("yontem") == "Çek" || a.V("yontem") == "Senet") s.Uyarilar.Add("Çek / senet ödemesi: finans portföyden belge planlar; vade son ödeme tarihine yakın seçilir.");
                return s;
            };
            m.Durum2 = a =>
            {
                var t = OdemeTalepOrnek.Bul(a.No);
                if (t == null) return a.Onaylandi ? "Finansa İletildi" : "Finansa İletilmedi";
                if (t.Durum == "Nakit Akışa Alındı") { var r = t.NakitAkisDocEntry != null ? NakitAkisOrnek.Bul(t.DbName, t.NakitAkisDocEntry.Value) : null; return $"Nakit Akışa Alındı · #{t.NakitAkisDocEntry}" + (r != null ? " · " + r.U_Durum : ""); }
                if (t.Durum == "Reddedildi") return "Reddedildi";
                return a.Onaylandi ? "Finansa İletildi (Departman Talepleri)" : "Finansa İletilmedi";
            };
            m.Durum3 = a =>
            {
                var t = OdemeTalepOrnek.Bul(a.No); var r = t?.NakitAkisDocEntry != null ? NakitAkisOrnek.Bul(t.DbName, t.NakitAkisDocEntry.Value) : null;
                if (r != null && r.U_Durum == "ÖDENDİ") return $"Ödendi {r.U_OdenmeTarihi:dd.MM.yyyy} · {r.U_OdemeYontemi}";
                if (r != null) return $"Ödeme Bekliyor (planlanan {r.U_PlanlananTarih:dd.MM.yyyy})";
                return a.Onaylandi ? "Nakit Akışa Alınmadı" : "—";
            };
            m.Olay = (a, kod, g) =>
            {
                switch (kod)
                {
                    case "NakitAkisaAlindi": { var r = OdemeTalepOrnek.NakitAkisaAl(a.No, "Finans (olay)"); return r.ok ? null : r.mesaj; }
                    case "Odendi":
                        {
                            var t = OdemeTalepOrnek.Bul(a.No); if (t == null || t.NakitAkisDocEntry == null) return "Önce talep nakit akışa alınmalı (NakitAkisaAlindi).";
                            var r = NakitAkisOrnek.Bul(t.DbName, t.NakitAkisDocEntry.Value); if (r == null) return "Nakit akış satırı bulunamadı."; if (r.U_Durum == "ÖDENDİ") return "Satır zaten ödenmiş.";
                            NakitAkisOrnek.OdendiIsaretle(t.DbName, r.DocEntry, "Ödeme talebi " + a.No, "Finans (olay)"); if (G(g, "dekontNo") != "") NakitAkisOrnek.AlanKaydet(t.DbName, r.DocEntry, "U_DekontNo", G(g, "dekontNo"), "Finans (olay)");
                            var simdi = TalepModulleri.OlayZamani ?? DateTime.Now;
                            a.Ek["odemeTarihi"] = simdi.ToString("dd.MM.yyyy", Tr); a.Ek["odenenTutar"] = $"{N(r.U_OdenenTutar ?? r.U_Tutar ?? 0)} {r.U_OdenenTutarPB ?? r.U_ParaBirimi}"; if (G(g, "dekontNo") != "") a.Ek["dekontNo"] = G(g, "dekontNo");
                            TalepMotoru.Hareket(a, simdi, $"Kasa Nakit Akışı: #{r.DocEntry} ödendi ({a.Ek["odenenTutar"]}, {r.U_OdemeYontemi})", "Kasa Nakit Akışı"); TalepMotoru.Log(a, simdi, "ÖT_EVENT", "PAYMENT", "", a.Ek["odenenTutar"], "Sistem", "Event", "Kasa Nakit Akışı");
                            return null;
                        }
                }
                return "Tanımsız olay.";
            };
            m.Ozet = a => $"{a.V("cari")} · {a.V("konu")} · {N(a.D("tutar"))} {a.V("pb")} · son ödeme {a.T("sonOdeme"):dd.MM.yyyy}";
            m.Etiketle = a =>
            {
                var l = new List<string>(); bool ok = a.Onaylandi && !a.Iptal; string d2 = a.Durum2, d3 = a.Durum3;
                if (a.TalepDurumu.EndsWith("Bekliyor") && !a.TalepDurumu.StartsWith("Revizyon")) l.Add("onay");
                if (ok && d2.StartsWith("Finansa İletildi")) l.Add("finans");
                if (ok && d2.StartsWith("Nakit Akışa Alındı") && !d3.StartsWith("Ödendi")) l.Add("nakit");
                if (d3.StartsWith("Ödendi")) l.Add("odendi");
                var so = a.T("sonOdeme"); if (!a.Iptal && a.TalepDurumu != "Reddedildi" && !d3.StartsWith("Ödendi") && so.HasValue && (so.Value.Date - DateTime.Today).Days <= 7) l.Add("vade");
                if (a.TalepDurumu == "Reddedildi" || a.Iptal) l.Add("red");
                return l;
            };
            m.Kpiler = l =>
            {
                decimal Tl(IEnumerable<TmTalep> x) => x.Sum(t => decimal.TryParse(t.Hesap.TryGetValue("tutarTLSayi", out var v) ? v : "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0);
                var onay = l.Where(t => t.Etiketler.Contains("onay")).ToList(); var fin = l.Where(t => t.Etiketler.Contains("finans")).ToList(); var nak = l.Where(t => t.Etiketler.Contains("nakit")).ToList(); var vade = l.Where(t => t.Etiketler.Contains("vade")).ToList(); var od = l.Where(t => t.Etiketler.Contains("odendi")).ToList();
                return new List<TmKpi>
                {
                    K("#f59e0b", "Onay Bekleyen", $"{onay.Count} Talep", N(Tl(onay), 0) + " TL", "onay"), K("#0f766e", "Finansa İletilen", $"{fin.Count} Talep", N(Tl(fin), 0) + " TL · nakit akışa alınacak", "finans"),
                    K("#0ea5e9", "Nakit Akışta", $"{nak.Count} Talep", N(Tl(nak), 0) + " TL · ödeme bekliyor", "nakit"), K("#dc2626", "Vadesi Yaklaşan", $"{vade.Count} Talep", "7 gün içinde son ödeme", "vade"),
                    K("#16a34a", "Ödenen", $"{od.Count} Talep", N(Tl(od), 0) + " TL", "odendi"), K("#64748b", "Red / İptal", $"{l.Count(t => t.Etiketler.Contains("red"))} Talep", "yeniden açılabilir", "red")
                };
            };
            return m;
        }

        /// <summary>Örnek kayıtlar (talep motoru tohumu): her birimden bir talep; onaylı olanlar Kasa Nakit Akışı › Departman Talepleri'nde görünür.</summary>
        public static void Tohum()
        {
            var bugun = DateTime.Today;
            Dictionary<string, string> D(string sirket, string cari, string konu, string belgeTipi, string belgeNo, string tutar, string pb, int sonOdemeGun, string yontem, string oncelik, string aciklama) => new Dictionary<string, string>
            { ["sirket"] = sirket, ["cari"] = cari, ["konu"] = konu, ["fatura"] = "", ["belgeTipi"] = belgeTipi, ["belgeNo"] = belgeNo, ["tutar"] = tutar, ["pb"] = pb, ["sonOdeme"] = bugun.AddDays(sonOdemeGun).ToString("yyyy-MM-dd"), ["yontem"] = yontem, ["oncelik"] = oncelik, ["aciklama"] = aciklama };
            // Satın alma (Selvi): tedarikçi faturası, onaylı → finansa iletildi
            TalepMotoru.Tohum(Kod, "Emre Taş", "Emre Taş", "FATURA", D("Selvi", "Korozo Ambalaj", "Ambalaj / Yardımcı Malzeme", "Fatura", "FT-2026-55120", "148000", "TRY", 6, "Çek", "Normal", "Ambalaj film partisi faturası; vade sonu ödemesi."), 3);
            // Muhasebe: KDV beyannamesi, finans onayı bekliyor
            TalepMotoru.Tohum(Kod, "Nazlı Erdem", "Nazlı Erdem", "RESMI", D("Uras Kimya", "Gebze Vergi Dairesi", "KDV Beyannamesi", "Vergi", "KDV-2026-10", "1284500", "TRY", 12, "Havale / EFT", "Acil", "Ekim 2026 1 no.lu KDV beyannamesi tahakkuku."), 1, 0);
            // Finans: kredi taksidi, onaylı (Finans + Direktör) → nakit akışa alındı
            var f1 = TalepMotoru.Tohum(Kod, "Pınar Ak", "Pınar Ak", "FINANSAL", D("Uras Holding", "Ziraat Bankası", "Kredi Taksidi", "Kredi Taksidi", "KRD-2024-0871/7", "412600", "TRY", 5, "Havale / EFT", "Acil", "Taksitli ticari kredi 7/24 taksidi."), 4);
            TalepMotoru.TohumOlay(f1, "NakitAkisaAlindi", null, 2, 11);
            // İdari işler (Alv Kimya): kira, Direktör onayı bekliyor
            TalepMotoru.Tohum(Kod, "Canan Su", "Canan Su", "KIRA", D("Alv Kimya", "Gençosman Gayrimenkul", "Kira", "Kira", "KR-2026-11", "185000", "TRY", 18, "Havale / EFT", "Normal", "Kasım 2026 ofis kirası (stopaj dahil)."), 2, 0);
            // Satın alma (Avrupa Paper): hizmet faturası, yönetici reddetti
            var r1 = TalepMotoru.Tohum(Kod, "Burcu Aydın", "Burcu Aydın", "FATURA", D("Avrupa Paper", "Anadolu Temizlik", "Hizmet Alımı", "Fatura", "FT-2026-77410", "42000", "TRY", 9, "Havale / EFT", "Normal", "Eylül temizlik hizmet faturası."), 5, 0);
            TalepMotoru.TohumRed(r1, "Sözleşme dışı ek hizmet kalemi var; fatura düzeltilsin.");
            // Finans (Ahmet Özkan): leasing taksidi, onaylı → nakit akışa alındı → ödendi
            var f2 = TalepMotoru.Tohum(Kod, "Ahmet Özkan", "Ahmet Özkan", "FINANSAL", D("Uras Kimya", "Yapı Kredi Leasing", "Faktoring / Leasing", "Sigorta", "LS-2025-114/18", "96800", "TRY", -2, "Havale / EFT", "Normal", "Forklift leasing taksidi 18/36."), 9);
            TalepMotoru.TohumOlay(f2, "NakitAkisaAlindi", null, 7, 10); TalepMotoru.TohumOlay(f2, "Odendi", new Dictionary<string, string> { ["dekontNo"] = "DK-2026-10-0418" }, 2, 15);
        }
    }
}
