// ============================================================
// TALEP MODÜLLERİ — Mesai, Belge, Seyahat ve Araç talepleri (iş kuralları, master veriler, kaynak sistem olayları, örnek kayıtlar)
// Ortak çekirdek: Data/TalepMotoru.cs. Her modül: türler, form alanları, sistem hesabı (kilitli), 2. ve 3. statü, olaylar, KPI'lar, takip sütunları.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public static class TalepModulleri
    {
        public static DateTime? OlayZamani;                       // tohum olaylarında geçmiş tarih
        private static DateTime Simdi => OlayZamani ?? DateTime.Now;
        // TalepMotoru.Tr KULLANILMAZ: TalepMotoru'nun statik kurucusu Kaydet()'i çağırır; bu sınıfa ilk erişim TalepModulleri
        // üzerinden olursa (ör. GrupIci/Rapor → GrupBakiyeleri) döngüsel başlatma yüzünden Filo / Ehliyetler gibi alanlar
        // henüz null'ken Arac() çalışır ve tip başlatıcı kalıcı olarak hata verirdi (500).
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private static string N(decimal d, int b = 0) => d.ToString("N" + b, Tr);
        private static string S(decimal d) => d.ToString("0.#", Tr);
        private static TmAlan A(string ad, string etiket, string tip = "text", bool zorunlu = false, int col = 4, string[] sec = null, string ipucu = null, string vars = null, string grup = "detay") => new TmAlan { Ad = ad, Etiket = etiket, Tip = tip, Zorunlu = zorunlu, Col = col, Secenekler = sec, Ipucu = ipucu, Varsayilan = vars, Grup = grup };
        private static TmTur T(string kod, string ad, string akis, string aciklama, string ek = null) => new TmTur { Kod = kod, Ad = ad, Akis = akis, Aciklama = aciklama, Ek = ek };
        private static TmOlay O(string kod, string ad, string kaynak, string aciklama, params string[] girdiler) => new TmOlay { Kod = kod, Ad = ad, Kaynak = kaynak, Aciklama = aciklama, Girdiler = girdiler };
        private static string G(Dictionary<string, string> g, string k) => g.TryGetValue(k, out var v) ? (v ?? "").Trim() : "";
        private static decimal GD(Dictionary<string, string> g, string k) => decimal.TryParse(G(g, k), NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0;
        private static readonly string[] Sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct().ToArray();
        private static void H(TmTalep a, string h, string k) => TalepMotoru.Hareket(a, Simdi, h, k);
        private static void L(TmTalep a, string alan, string eski, string yeni, string kaynak) => TalepMotoru.Log(a, Simdi, a.Modul.NoOnEk + "_EVENT", alan, eski, yeni, "Sistem", "Event", kaynak);
        private static bool HaftaSonu(DateTime d) => d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
        private static decimal SaatFarki(string bas, string bit) { if (!TimeSpan.TryParse(bas, out var b) || !TimeSpan.TryParse(bit, out var e)) return 0; var f = e - b; if (f < TimeSpan.Zero) f += TimeSpan.FromHours(24); return (decimal)f.TotalHours; }
        private static List<TmTalep> Liste(string modul) => TalepMotoru.Talepler.Where(t => t.ModulKod == modul).ToList();
        private static TmKpi K(string renk, string baslik, string deger, string alt, string etiket) => new TmKpi { Renk = renk, Baslik = baslik, Deger = deger, Alt = alt, Etiket = etiket };
        private static string Adlar(IEnumerable<TmTalep> l) { var s = string.Join(", ", l.Select(t => t.Kullanan.AdSoyad.Split(' ')[0]).Distinct().Take(6)); return s == "" ? "—" : s; }
        private static string Bos(string s) => string.IsNullOrEmpty(s) ? "—" : s;

        public static void Kaydet()
        {
            TalepMotoru.Moduller["MESAI"] = Mesai();
            TalepMotoru.Moduller["BELGE"] = Belge();
            TalepMotoru.Moduller["SEYAHAT"] = Seyahat();
            TalepMotoru.Moduller["ARAC"] = Arac();
            TalepMotoru.Moduller["YANSITMA"] = Yansitma();
            TalepMotoru.Moduller["MAHSUP"] = Mahsup();
            TalepMotoru.Moduller["TEMLIK"] = Temlik();
            MesaiTohum(); BelgeTohum(); SeyahatTohum(); AracTohum(); GrupIciTohum();
        }

        // =====================================================================
        // GRUP İÇİ ÖZEL FİNANSAL İŞLEMLER — ortak master: şirketler arası cari bakiyeler (A|B → A'nın B'ye borcu, TL)
        // =====================================================================
        public static readonly Dictionary<string, decimal> GrupBakiyeleri = new Dictionary<string, decimal>
        {
            ["Uras Kimya|Uras Holding"] = 1850000m, ["Uras Holding|Uras Kimya"] = 420000m, ["Selvi|Uras Kimya"] = 965000m, ["Uras Kimya|Selvi"] = 310000m,
            ["Avrupa Paper|Uras Holding"] = 275000m, ["Uras Holding|Avrupa Paper"] = 640000m, ["Alv Kimya|Uras Kimya"] = 128000m, ["Uras Kimya|Alv Kimya"] = 512000m,
            ["Daf Kimya|Uras Holding"] = 89000m, ["Uras Holding|Daf Kimya"] = 233000m, ["Uras Power|Uras Holding"] = 1120000m, ["Uras Holding|Uras Power"] = 95000m,
            ["Uras Kimya A.Ş.|Uras Kimya"] = 47000m, ["Uras Kimya|Uras Kimya A.Ş."] = 158000m
        };
        public static decimal GrupBakiye(string a, string b) => GrupBakiyeleri.TryGetValue(a + "|" + b, out var v) ? v : 0;
        private static readonly string[] KdvOranlari = { "0", "1", "10", "20" };
        private static string Donem(int ayOnce) => DateTime.Today.AddMonths(-ayOnce).ToString("yyyy-MM");

        private static TmModul Yansitma()
        {
            var m = new TmModul
            {
                Kod = "YANSITMA", Ad = "Yansıtma İşlemleri", NoOnEk = "YN", Ikon = "fa-right-left", Renk = "#0d9488",
                Aciklama = "Bir grup şirketinin yaptığı masrafın, masrafın ait olduğu diğer grup şirketine yansıtılması. KDV ve toplam sistem hesaplar; e-fatura, karşı taraf kabulü, muhasebe fişleri ve mutabakat kaynak sistemlerden gelir.",
                AkisOzeti = "Talep → Muhasebe → (Direktör) → Finans → E-Fatura → Karşı Kabul → Fişler → Mutabakat → Kapanış",
                KullananEtiket = "İşlemi yapan personel", Durum2Ad = "Fatura Durumu (E-Fatura)", Durum3Ad = "Muhasebe / Mutabakat",
                Turler = { T("MASRAF", "Masraf Yansıtması", "M,F", "Ortak gider, sigorta, IT lisans gibi masrafların dağıtım anahtarıyla yansıtılması."), T("HIZMET", "Hizmet Bedeli Yansıtması", "M,F", "Yönetim / danışmanlık hizmet bedeli; transfer fiyatlandırması dokümantasyonu."), T("PERSONEL", "Personel Maliyeti Yansıtması", "M,I,F", "Görevlendirilen personelin maliyeti; İK onayı gerekir."), T("KIRA", "Kira / Ortak Alan Yansıtması", "M,F", "Kira ve ortak alan giderleri; metrekare anahtarı.") },
                Alanlar = { A("yansitan", "Yansıtan Şirket (masrafı yapan)", "select", true, 3, Sirketler, vars: "Uras Holding"), A("yansitilan", "Yansıtılan Şirket (masrafın sahibi)", "select", true, 3, Sirketler), A("masrafTuru", "Masraf Türü", "select", true, 3, new[] { "Ortak Gider Payı", "Personel Maliyeti", "Kira", "Sigorta", "IT Lisans / Hizmet", "Yönetim Hizmet Bedeli", "Nakliye", "Diğer" }), A("donem", "Dönem (AAAA-AA)", "text", true, 3, vars: Donem(1)),
                            A("tutar", "Matrah (TL)", "number", true, 3), A("kdvOrani", "KDV Oranı (%)", "select", true, 2, KdvOranlari, vars: "20"), A("dagitimAnahtari", "Dağıtım Anahtarı", "select", false, 3, new[] { "Ciro Oranı", "Personel Sayısı", "Metrekare", "Kullanım / Sayaç", "Sabit Tutar" }), A("dayanak", "Dayanak (fatura no / sözleşme)", "text", false, 4, ipucu: "Kaynak fatura ya da sözleşme referansı"),
                            A("masrafMerkezi", "Masraf Merkezi (yansıtılan tarafta)", "text", false, 4, ipucu: "CC…"), A("aciklama", "Açıklama", "textarea", true, 8) },
                Olaylar = { O("FaturaKesildi", "FaturaKesildi — E-Fatura düzenlendi", "E-Fatura", "Yansıtma faturası GİB'e iletildi.", "faturaNo|Fatura No|text"), O("KarsiKabul", "KarsiKabul — Karşı şirket faturayı kabul etti", "E-Fatura", "Ticari fatura yanıtı: kabul."), O("KarsiRed", "KarsiRed — Karşı şirket faturayı reddetti", "E-Fatura", "Ticari fatura yanıtı: red; revize gerekir.", "neden|Red Nedeni|text"), O("MuhasebeFisi", "MuhasebeFisi — Muhasebe fişi oluştu", "Muhasebe", "Yansıtan (gelir) ya da yansıtılan (gider) tarafta fiş.", "taraf|Taraf|select:Yansıtan;Yansıtılan", "fisNo|Fiş No|text"), O("Mutabakat", "Mutabakat — Cari mutabakat sağlandı", "Muhasebe", "İki şirketin cari hesapları eşleşti."), O("Kapanis", "Kapanis — İşlem kapatıldı", "Muhasebe", "Mutabakat sonrası kapanış.") },
                HesapEtiketler = { ["kdvTutar"] = "KDV Tutarı (TL)", ["toplam"] = "Fatura Toplamı (TL)", ["mevcutBakiye"] = "Yansıtılanın Yansıtana Mevcut Borcu (TL)", ["yeniBakiye"] = "İşlem Sonrası Borç (TL)" },
                EkEtiketler = { ["faturaNo"] = "Fatura No", ["faturaTarihi"] = "Fatura Tarihi", ["karsiYanit"] = "Karşı Taraf Yanıtı", ["redNedeni"] = "Red Nedeni", ["fisYansitan"] = "Fiş (Yansıtan)", ["fisYansitilan"] = "Fiş (Yansıtılan)", ["mutabakatTarihi"] = "Mutabakat", ["kapanisTarihi"] = "Kapanış" },
                Parametreler = { ["direktorEsik"] = "100000", ["gecYansitmaAy"] = "3" },
                ParametreEtiketler = { ["direktorEsik"] = "Bu tutarı aşan yansıtmada Direktör onayı (TL)", ["gecYansitmaAy"] = "Bu aydan eski dönem için geç yansıtma uyarısı" },
                Kurallar = { "Yansıtan ve yansıtılan şirket farklı olmalıdır; aynı dönem / tür / şirket çifti için mükerrer yansıtma engellenir.", "KDV ve fatura toplamı sistem hesabıdır; matrah dağıtım anahtarıyla belgelenir.", "Eşiği aşan tutarlarda Direktör, personel maliyetinde İK onayı eklenir.", "Fatura no, karşı taraf yanıtı, muhasebe fişleri ve mutabakat e-fatura / muhasebe sisteminden gelir; elle girilmez.", "Karşı taraf reddederse talep revizyona döner; mutabakat olmadan kapanış yapılamaz." },
                TakipSutunlar = { ("Talep No", "no"), ("Tür", "tur"), ("Yansıtan", "d.yansitan"), ("Yansıtılan", "d.yansitilan"), ("Dönem", "d.donem"), ("Masraf Türü", "d.masrafTuru"), ("Matrah (TL)", "d.tutar"), ("KDV %", "d.kdvOrani"), ("Toplam (TL)", "h.toplam"), ("Anahtar", "d.dagitimAnahtari"), ("Fatura No", "e.faturaNo"), ("Talep Durumu", "talepDurumu"), ("Fatura", "durum2"), ("Muhasebe", "durum3"), ("İşlemi Yapan", "kullanan"), ("Muhasebe", "onay.Muhasebe Onayı"), ("Finans", "onay.Finans Onayı") }
            };
            m.Hesapla = a =>
            {
                var s = new TmSonuc(); var m2 = a.Modul;
                if (a.V("yansitan") == a.V("yansitilan")) { s.Hata = "Yansıtan ve yansıtılan şirket aynı olamaz."; return s; }
                if (a.D("tutar") <= 0) { s.Hata = "Matrah sıfırdan büyük olmalıdır."; return s; }
                if (!System.Text.RegularExpressions.Regex.IsMatch(a.V("donem"), @"^\d{4}-(0[1-9]|1[0-2])$")) { s.Hata = "Dönem AAAA-AA biçiminde olmalıdır."; return s; }
                decimal kdv = Math.Round(a.D("tutar") * a.D("kdvOrani") / 100m, 2), toplam = a.D("tutar") + kdv, mevcut = GrupBakiye(a.V("yansitilan"), a.V("yansitan"));
                a.Hesap["kdvTutar"] = N(kdv, 2); a.Hesap["toplam"] = N(toplam, 2); a.Hesap["toplamSayi"] = toplam.ToString("0.00", CultureInfo.InvariantCulture); a.Hesap["mevcutBakiye"] = N(mevcut); a.Hesap["yeniBakiye"] = N(mevcut + toplam);
                if (string.CompareOrdinal(a.V("donem"), DateTime.Today.ToString("yyyy-MM")) > 0) { s.Hata = "Gelecek dönem için yansıtma yapılamaz."; return s; }
                if (string.CompareOrdinal(a.V("donem"), Donem((int)m2.P("gecYansitmaAy"))) < 0) s.Uyarilar.Add($"Geç yansıtma: dönem {a.V("donem")} {S(m2.P("gecYansitmaAy"))} aydan eski; dönem kapanışı kontrol edilmeli.");
                if (Liste("YANSITMA").Any(t => t.Id != a.Id && !t.Iptal && t.TalepDurumu != "Reddedildi" && t.V("yansitan") == a.V("yansitan") && t.V("yansitilan") == a.V("yansitilan") && t.V("masrafTuru") == a.V("masrafTuru") && t.V("donem") == a.V("donem"))) { s.Hata = "Aynı dönem, aynı masraf türü ve aynı şirket çifti için yansıtma zaten var (mükerrer)."; return s; }
                if (toplam > m2.P("direktorEsik")) { s.EkAdimlar.Add("D"); s.Uyarilar.Add($"Fatura toplamı {N(toplam)} TL, {N(m2.P("direktorEsik"))} TL eşiğini aşıyor: Direktör onayı eklendi."); }
                if (a.TurKod == "HIZMET" && a.V("dayanak") == "") s.Uyarilar.Add("Hizmet bedeli yansıtmasında sözleşme / transfer fiyatlandırması dayanağı beklenir.");
                if (a.TurKod == "PERSONEL" && a.D("kdvOrani") != 20) s.Uyarilar.Add("Personel maliyeti yansıtmalarında genel KDV oranı uygulanır.");
                return s;
            };
            m.Durum2 = a => a.Ek.ContainsKey("faturaNo") ? (a.E("karsiYanit") == "Kabul" ? "Karşı Tarafça Kabul Edildi" : a.E("karsiYanit") == "Red" ? "Karşı Tarafça Reddedildi" : "Fatura Kesildi") : "Fatura Bekliyor";
            m.Durum3 = a => a.Ek.ContainsKey("kapanisTarihi") ? "Kapandı" : a.Ek.ContainsKey("mutabakatTarihi") ? "Mutabık" : a.Ek.ContainsKey("fisYansitan") && a.Ek.ContainsKey("fisYansitilan") ? "İki Tarafta Muhasebeleşti" : a.Ek.ContainsKey("fisYansitan") || a.Ek.ContainsKey("fisYansitilan") ? "Tek Tarafta Kayıtlı" : "Kayıt Bekliyor";
            m.Olay = (a, kod, g) =>
            {
                switch (kod)
                {
                    case "FaturaKesildi": { if (a.Ek.ContainsKey("faturaNo") && a.E("karsiYanit") != "Red") return "Fatura zaten kesilmiş."; a.Ek["faturaNo"] = G(g, "faturaNo") == "" ? $"URS{Simdi:yyyy}{(100000 + a.Id * 7):000000000}" : G(g, "faturaNo"); a.Ek["faturaTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); a.Ek.Remove("karsiYanit"); a.Ek.Remove("redNedeni"); H(a, $"E-Fatura: yansıtma faturası düzenlendi ({a.E("faturaNo")}, {a.Hesap["toplam"]} TL)", "E-Fatura"); L(a, "INVOICE_NO", "", a.E("faturaNo"), "E-Fatura"); return null; }
                    case "KarsiKabul": { if (!a.Ek.ContainsKey("faturaNo")) return "Önce fatura kesilmeli."; a.Ek["karsiYanit"] = "Kabul"; H(a, $"E-Fatura: {a.V("yansitilan")} faturayı kabul etti", "E-Fatura"); L(a, "INVOICE_RESPONSE", "", "Kabul", "E-Fatura"); return null; }
                    case "KarsiRed": { if (!a.Ek.ContainsKey("faturaNo")) return "Önce fatura kesilmeli."; a.Ek["karsiYanit"] = "Red"; a.Ek["redNedeni"] = G(g, "neden") == "" ? "Tutar / dönem itirazı" : G(g, "neden"); H(a, $"E-Fatura: {a.V("yansitilan")} faturayı reddetti ({a.E("redNedeni")}); revize edilmeli", "E-Fatura"); L(a, "INVOICE_RESPONSE", "", "Red", "E-Fatura"); return null; }
                    case "MuhasebeFisi": { if (!a.Ek.ContainsKey("faturaNo")) return "Fatura kesilmeden fiş oluşamaz."; string taraf = G(g, "taraf") == "Yansıtılan" ? "fisYansitilan" : "fisYansitan"; if (a.Ek.ContainsKey(taraf)) return "Bu tarafta fiş zaten var."; a.Ek[taraf] = G(g, "fisNo") == "" ? "MF-" + (8100 + a.Id * 3 + a.Ek.Count) : G(g, "fisNo"); H(a, $"Muhasebe: {(taraf == "fisYansitan" ? a.V("yansitan") + " (gelir)" : a.V("yansitilan") + " (gider)")} tarafında fiş {a.Ek[taraf]}", "Muhasebe"); L(a, "JOURNAL_" + (taraf == "fisYansitan" ? "A" : "B"), "", a.Ek[taraf], "Muhasebe"); return null; }
                    case "Mutabakat": { if (!a.Ek.ContainsKey("fisYansitan") || !a.Ek.ContainsKey("fisYansitilan")) return "İki tarafta da fiş olmadan mutabakat yapılamaz."; a.Ek["mutabakatTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, "Muhasebe: cari mutabakat sağlandı", "Muhasebe"); L(a, "RECONCILED", "false", "true", "Muhasebe"); return null; }
                    case "Kapanis": { if (!a.Ek.ContainsKey("mutabakatTarihi")) return "Mutabakat olmadan kapanış yapılamaz."; if (a.Ek.ContainsKey("kapanisTarihi")) return "Zaten kapanmış."; a.Ek["kapanisTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, "İşlem kapatıldı", "Sistem"); L(a, "STATUS", "Açık", "Kapandı", "Sistem"); return null; }
                }
                return "Tanımsız olay.";
            };
            m.Ozet = a => $"{a.V("yansitan")} → {a.V("yansitilan")} · {a.V("donem")} · {a.V("masrafTuru")} · {a.Hesap["toplam"]} TL";
            m.Etiketle = a => { var l = new List<string>(); bool ok = a.Onaylandi && !a.Iptal; if (a.TalepDurumu.EndsWith("Bekliyor") && !a.TalepDurumu.StartsWith("Revizyon")) l.Add("onay"); if (ok && a.Durum2 == "Fatura Bekliyor") l.Add("fatura"); if (ok && a.Durum2 == "Fatura Kesildi") l.Add("kabul"); if (ok && a.Durum2 == "Karşı Tarafça Reddedildi") l.Add("red"); if (ok && a.Durum2.StartsWith("Karşı Tarafça Kabul") && (a.Durum3 == "Kayıt Bekliyor" || a.Durum3 == "Tek Tarafta Kayıtlı")) l.Add("fis"); if (ok && a.Durum3 == "İki Tarafta Muhasebeleşti") l.Add("mutabakat"); if (ok && a.V("donem") == Donem(1)) l.Add("donem"); return l; };
            m.Kpiler = l => new List<TmKpi> { K("#0d9488", "Fatura Bekleyen", $"{l.Count(t => t.Etiketler.Contains("fatura"))} İşlem", N(l.Where(t => t.Etiketler.Contains("fatura")).Sum(t => decimal.Parse(t.Hesap["toplamSayi"], CultureInfo.InvariantCulture))) + " TL", "fatura"), K("#f59e0b", "Karşı Kabul Bekleyen", $"{l.Count(t => t.Etiketler.Contains("kabul"))} Fatura", Adlar(l.Where(t => t.Etiketler.Contains("kabul"))), "kabul"), K("#dc2626", "Reddedilen Fatura", $"{l.Count(t => t.Etiketler.Contains("red"))} Fatura", "revize gerekir", "red"), K("#6366f1", "Fiş Bekleyen", $"{l.Count(t => t.Etiketler.Contains("fis"))} İşlem", "muhasebe kaydı eksik", "fis"), K("#0ea5e9", "Mutabakat Bekleyen", $"{l.Count(t => t.Etiketler.Contains("mutabakat"))} İşlem", "iki tarafta kayıtlı", "mutabakat"), K("#16a34a", "Geçen Dönem Toplamı", N(l.Where(t => t.Etiketler.Contains("donem") && t.Onaylandi).Sum(t => decimal.Parse(t.Hesap["toplamSayi"], CultureInfo.InvariantCulture))) + " TL", $"{Donem(1)} · {l.Count(t => t.Etiketler.Contains("donem"))} yansıtma", "donem") };
            return m;
        }

        private static TmModul Mahsup()
        {
            var m = new TmModul
            {
                Kod = "MAHSUP", Ad = "Mahsup İşlemleri", NoOnEk = "MH", Ikon = "fa-scale-balanced", Renk = "#7c3aed",
                Aciklama = "Grup şirketleri arasındaki karşılıklı borç ve alacakların nakit akışı olmadan mahsuplaşması. Cari bakiyeler ERP'den okunur; mahsup tutarı iki taraftaki bakiyenin küçüğünü aşamaz. Fişler ve mutabakat muhasebe sisteminden gelir.",
                AkisOzeti = "Talep → Muhasebe → (Direktör) → Finans → A Fişi → B Fişi → Mutabakat → Kapanış",
                KullananEtiket = "İşlemi yapan personel", Durum2Ad = "Muhasebe Kayıt Durumu", Durum3Ad = "Mutabakat / Kapanış",
                Turler = { T("KARSILIKLI", "Karşılıklı Borç-Alacak Mahsubu", "M,F", "İki şirketin birbirine olan borçlarının netleştirilmesi."), T("UCLU", "Üçlü Mahsup (Alacak Devri)", "M,D,F", "A'nın B'den alacağının C'ye devri ile üç taraflı kapatma; Direktör onayı."), T("AVANS", "Avans Mahsubu", "M,F", "Verilen grup içi avansın fatura / harcama ile kapatılması."), T("FATURA", "Fatura Karşılığı Mahsup", "M,F", "Karşılıklı faturaların birbirine mahsubu.") },
                Alanlar = { A("sirketA", "Şirket A (borçlu)", "select", true, 3, Sirketler, vars: "Uras Kimya"), A("sirketB", "Şirket B (alacaklı)", "select", true, 3, Sirketler, vars: "Uras Holding"), A("tutar", "Mahsup Tutarı (TL)", "number", true, 3), A("tarih", "Mahsup Tarihi", "date", true, 3),
                            A("sirketC", "Şirket C (üçlü mahsupta)", "select", false, 3, Sirketler), A("dayanak", "Dayanak / Protokol No", "text", false, 4, ipucu: "Mahsup protokolü, fatura numaraları"), A("kapanisTuru", "Kapanış Türü", "select", false, 2, new[] { "Tam Kapanış", "Kısmi" }, vars: "Kısmi"), A("aciklama", "Açıklama", "textarea", true, 12) },
                Olaylar = { O("FisKaydi", "FisKaydi — Muhasebe fişi oluştu", "Muhasebe", "A ya da B tarafında mahsup fişi.", "taraf|Taraf|select:A;B", "fisNo|Fiş No|text"), O("Mutabakat", "Mutabakat — Cari mutabakat sonucu", "Muhasebe", "İki tarafın cari bakiyeleri karşılaştırıldı.", "sonuc|Sonuç|select:Mutabık;Fark Var", "fark|Fark (TL)|number"), O("Kapanis", "Kapanis — İşlem kapatıldı", "Muhasebe", "Mutabık sonrası kapanış.") },
                HesapEtiketler = { ["aBorcu"] = "A'nın B'ye Borcu (ERP, TL)", ["bBorcu"] = "B'nin A'ya Borcu (ERP, TL)", ["mahsupUst"] = "Mahsup Edilebilir Azami (TL)", ["kalanA"] = "İşlem Sonrası A→B (TL)", ["kalanB"] = "İşlem Sonrası B→A (TL)" },
                EkEtiketler = { ["fisA"] = "Fiş (A)", ["fisB"] = "Fiş (B)", ["mutabakatSonuc"] = "Mutabakat Sonucu", ["mutabakatFark"] = "Mutabakat Farkı (TL)", ["mutabakatTarihi"] = "Mutabakat Tarihi", ["kapanisTarihi"] = "Kapanış" },
                Parametreler = { ["direktorEsik"] = "500000" },
                ParametreEtiketler = { ["direktorEsik"] = "Bu tutarı aşan mahsupta Direktör onayı (TL)" },
                Kurallar = { "Mahsup tutarı iki taraftaki karşılıklı bakiyenin küçüğünü aşamaz; bakiyeler ERP'den okunur, elle girilmez.", "Şirket A ve B farklı olmalıdır; üçlü mahsupta C zorunludur ve Direktör onayı eklenir.", "Eşiği aşan tutarlarda Direktör onayı; her mahsup Muhasebe ve Finans onayından geçer.", "İki tarafta fiş oluşmadan mutabakat, mutabık olmadan kapanış yapılamaz.", "Mutabakat farkı varsa işlem kapanmaz; fark düzeltme kaydıyla giderilir." },
                TakipSutunlar = { ("Talep No", "no"), ("Tür", "tur"), ("Şirket A", "d.sirketA"), ("Şirket B", "d.sirketB"), ("Şirket C", "d.sirketC"), ("Tarih", "d.tarih"), ("Tutar (TL)", "d.tutar"), ("A→B Bakiye (TL)", "h.aBorcu"), ("B→A Bakiye (TL)", "h.bBorcu"), ("Fiş A", "e.fisA"), ("Fiş B", "e.fisB"), ("Talep Durumu", "talepDurumu"), ("Kayıt", "durum2"), ("Mutabakat", "durum3"), ("Fark (TL)", "e.mutabakatFark"), ("İşlemi Yapan", "kullanan"), ("Finans", "onay.Finans Onayı") }
            };
            m.Hesapla = a =>
            {
                var s = new TmSonuc(); var m2 = a.Modul; string A1 = a.V("sirketA"), B1 = a.V("sirketB");
                if (A1 == B1) { s.Hata = "Şirket A ve Şirket B aynı olamaz."; return s; }
                decimal ab = GrupBakiye(A1, B1), ba = GrupBakiye(B1, A1), t = a.D("tutar");
                decimal ust = a.TurKod == "KARSILIKLI" ? Math.Min(ab, ba) : a.TurKod == "UCLU" ? ab : Math.Max(ab, ba);   // karşılıklı: iki bakiyenin küçüğü; üçlü: A'nın borcu (C'nin alacağıyla kapanır); avans/fatura: büyük bakiye
                a.Hesap["aBorcu"] = N(ab); a.Hesap["bBorcu"] = N(ba); a.Hesap["mahsupUst"] = N(ust); a.Hesap["kalanA"] = N(Math.Max(0, ab - t)); a.Hesap["kalanB"] = N(Math.Max(0, ba - t));
                if (t <= 0) { s.Hata = "Mahsup tutarı sıfırdan büyük olmalıdır."; return s; }
                if (ab == 0 && ba == 0) { s.Hata = $"{A1} ile {B1} arasında ERP'de karşılıklı cari bakiye yok."; return s; }
                if (t > ust) { s.Hata = $"Mahsup tutarı ({N(t)} TL) mahsup edilebilir azami tutarı ({N(ust)} TL) aşıyor."; return s; }
                if (a.TurKod == "UCLU") { if (a.V("sirketC") == "" || a.V("sirketC") == A1 || a.V("sirketC") == B1) { s.Hata = "Üçlü mahsupta A ve B'den farklı bir Şirket C seçilmelidir."; return s; } }
                if (t > m2.P("direktorEsik")) { s.EkAdimlar.Add("D"); s.Uyarilar.Add($"Tutar {N(t)} TL, {N(m2.P("direktorEsik"))} TL eşiğini aşıyor: Direktör onayı eklendi."); }
                var tarih = a.T("tarih"); if (tarih.HasValue && tarih.Value.Date > DateTime.Today) s.Uyarilar.Add("İleri tarihli mahsup: fişler tarih geldiğinde oluşur.");
                if (tarih.HasValue && tarih.Value < new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)) s.Uyarilar.Add("Geçmiş dönem tarihi: dönem kapanışı kontrol edilmeli.");
                if (a.V("kapanisTuru") == "Tam Kapanış" && t < ust) s.Uyarilar.Add($"Tam kapanış seçildi ama tutar azami mahsup tutarından ({N(ust)} TL) düşük.");
                return s;
            };
            m.Durum2 = a => a.Ek.ContainsKey("fisA") && a.Ek.ContainsKey("fisB") ? "İki Tarafta Kaydedildi" : a.Ek.ContainsKey("fisA") ? "A Tarafında Kaydedildi" : a.Ek.ContainsKey("fisB") ? "B Tarafında Kaydedildi" : "Kayıt Bekliyor";
            m.Durum3 = a => a.Ek.ContainsKey("kapanisTarihi") ? "Kapandı" : a.E("mutabakatSonuc") == "Fark Var" ? "Mutabakat Farkı Var" : a.E("mutabakatSonuc") == "Mutabık" ? "Mutabık" : a.Ek.ContainsKey("fisA") && a.Ek.ContainsKey("fisB") ? "Mutabakat Bekliyor" : "—";
            m.Olay = (a, kod, g) =>
            {
                switch (kod)
                {
                    case "FisKaydi": { string taraf = G(g, "taraf") == "B" ? "fisB" : "fisA"; if (a.Ek.ContainsKey(taraf)) return "Bu tarafta fiş zaten var."; a.Ek[taraf] = G(g, "fisNo") == "" ? "MH-" + (9100 + a.Id * 3 + a.Ek.Count) : G(g, "fisNo"); H(a, $"Muhasebe: {(taraf == "fisA" ? a.V("sirketA") : a.V("sirketB"))} tarafında mahsup fişi {a.Ek[taraf]}", "Muhasebe"); L(a, "JOURNAL_" + taraf.Last(), "", a.Ek[taraf], "Muhasebe"); return null; }
                    case "Mutabakat": { if (!a.Ek.ContainsKey("fisA") || !a.Ek.ContainsKey("fisB")) return "İki tarafta da fiş olmadan mutabakat yapılamaz."; string sonuc = G(g, "sonuc") == "Fark Var" ? "Fark Var" : "Mutabık"; a.Ek["mutabakatSonuc"] = sonuc; a.Ek["mutabakatTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); if (sonuc == "Fark Var") a.Ek["mutabakatFark"] = N(GD(g, "fark") == 0 ? 1250 : GD(g, "fark"), 2); else a.Ek.Remove("mutabakatFark"); H(a, sonuc == "Mutabık" ? "Muhasebe: cari mutabakat sağlandı" : $"Muhasebe: mutabakat farkı {a.E("mutabakatFark")} TL; düzeltme kaydı bekleniyor", "Muhasebe"); L(a, "RECONCILIATION", "", sonuc, "Muhasebe"); return null; }
                    case "Kapanis": { if (a.E("mutabakatSonuc") != "Mutabık") return "Mutabık olmadan kapanış yapılamaz."; if (a.Ek.ContainsKey("kapanisTarihi")) return "Zaten kapanmış."; a.Ek["kapanisTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, "İşlem kapatıldı; cari bakiyeler güncellendi", "Sistem"); L(a, "STATUS", "Açık", "Kapandı", "Sistem"); return null; }
                }
                return "Tanımsız olay.";
            };
            m.Ozet = a => $"{a.V("sirketA")} ⇄ {a.V("sirketB")}{(a.V("sirketC") != "" ? " ⇄ " + a.V("sirketC") : "")} · {N(a.D("tutar"))} TL · {a.T("tarih"):dd.MM.yyyy}";
            m.Etiketle = a => { var l = new List<string>(); bool ok = a.Onaylandi && !a.Iptal; if (a.TalepDurumu.EndsWith("Bekliyor") && !a.TalepDurumu.StartsWith("Revizyon")) l.Add("onay"); if (ok && a.Durum2 != "İki Tarafta Kaydedildi") l.Add("kayit"); if (ok && a.Durum3 == "Mutabakat Bekliyor") l.Add("mutabakat"); if (ok && a.Durum3 == "Mutabakat Farkı Var") l.Add("fark"); if (ok && (a.T("tarih")?.ToString("yyyy-MM") ?? "") == DateTime.Today.ToString("yyyy-MM")) l.Add("ay"); if (a.Durum3 == "Kapandı") l.Add("kapandi"); return l; };
            m.Kpiler = l => new List<TmKpi> { K("#7c3aed", "Kayıt Bekleyen", $"{l.Count(t => t.Etiketler.Contains("kayit"))} Mahsup", "en az bir tarafta fiş yok", "kayit"), K("#0ea5e9", "Mutabakat Bekleyen", $"{l.Count(t => t.Etiketler.Contains("mutabakat"))} Mahsup", "iki tarafta kayıtlı", "mutabakat"), K("#dc2626", "Mutabakat Farkı", $"{l.Count(t => t.Etiketler.Contains("fark"))} Mahsup", N(l.Where(t => t.Etiketler.Contains("fark")).Sum(t => decimal.TryParse(t.E("mutabakatFark"), NumberStyles.Any, Tr, out var f) ? f : 0), 2) + " TL", "fark"), K("#f59e0b", "Onay Bekleyen", $"{l.Count(t => t.Etiketler.Contains("onay"))} Talep", N(l.Where(t => t.Etiketler.Contains("onay")).Sum(t => t.D("tutar"))) + " TL", "onay"), K("#16a34a", "Bu Ay Mahsup Toplamı", N(l.Where(t => t.Etiketler.Contains("ay")).Sum(t => t.D("tutar"))) + " TL", $"{l.Count(t => t.Etiketler.Contains("ay"))} işlem", "ay"), K("#64748b", "Kapanan", $"{l.Count(t => t.Etiketler.Contains("kapandi"))} Mahsup", N(l.Where(t => t.Etiketler.Contains("kapandi")).Sum(t => t.D("tutar"))) + " TL", "kapandi") };
            return m;
        }

        private static TmModul Temlik()
        {
            var m = new TmModul
            {
                Kod = "TEMLIK", Ad = "Temlik İşlemleri", NoOnEk = "TM", Ikon = "fa-file-contract", Renk = "#b45309",
                Aciklama = "Bir grup şirketinin üçüncü taraftan alacağının diğer bir grup şirketine devri (TBK m.183 vd.). Sözleşme, borçluya bildirim, borçlu teyidi ve tahsilat kaynak sistemlerden gelir; Hukuk onayı zorunludur.",
                AkisOzeti = "Talep → Muhasebe → Hukuk → (Direktör) → Finans → Sözleşme → Borçluya Bildirim → Teyit → Tahsilat → Kapanış",
                KullananEtiket = "İşlemi yapan personel", Durum2Ad = "Sözleşme / Bildirim Durumu", Durum3Ad = "Tahsilat / Kapanış",
                Turler = { T("TAM", "Tam Temlik", "M,H,F", "Alacağın tamamı devredilir."), T("KISMI", "Kısmi Temlik", "M,H,F", "Alacağın bir bölümü devredilir; kalan alacak temlik edende kalır."), T("TEMINAT", "Teminat Amaçlı Temlik", "M,H,D,F", "Kredi / borç teminatı olarak; Direktör onayı."), T("RUCU", "Rücu Edilebilir Temlik", "M,H,F", "Borçlu ödemezse temlik edene rücu hakkı saklıdır.") },
                Alanlar = { A("temlikEden", "Temlik Eden Şirket (alacaklı)", "select", true, 3, Sirketler, vars: "Uras Kimya"), A("temlikAlan", "Temlik Alan Şirket", "select", true, 3, Sirketler, vars: "Uras Holding"), A("borclu", "Borçlu Cari (3. taraf)", "text", true, 3, ipucu: "Müşteri / cari adı"), A("alacakBelgesi", "Alacak Belgesi (fatura / çek no)", "text", true, 3),
                            A("alacakTutari", "Alacak Tutarı (TL)", "number", true, 3), A("tutar", "Temlik Tutarı (TL)", "number", true, 3), A("bedel", "Temlik Bedeli (TL)", "number", false, 3, ipucu: "boşsa temlik tutarına eşit"), A("vade", "Alacağın Vadesi", "date", true, 3),
                            A("bildirim", "Borçluya ihbarname gönderilecek", "checkbox", false, 4, vars: "true"), A("rucu", "Rücu hakkı saklı", "checkbox", false, 4), A("sozlesmeNo", "Sözleşme / Protokol No", "text", false, 4), A("aciklama", "Açıklama / Gerekçe", "textarea", true, 12) },
                Olaylar = { O("SozlesmeImzalandi", "SozlesmeImzalandi — Temlik sözleşmesi imzalandı", "Hukuk", "Noter ya da e-imza ile sözleşme.", "noterNo|Noter / Sözleşme No|text"), O("BorcluyaBildirildi", "BorcluyaBildirildi — İhbarname gönderildi", "Hukuk", "Borçluya temlik bildirimi (TBK m.186).", "yontem|Yöntem|select:Noter İhbarnamesi;KEP;İadeli Taahhütlü"), O("BorcluTeyit", "BorcluTeyit — Borçlu temliği teyit etti", "Portal", "Borçlu ödemeyi temlik alana yapacağını bildirdi."), O("Tahsilat", "Tahsilat — Temlik alan şirket tahsilat yaptı", "Finans/Kasa", "Kısmi ya da tam tahsilat.", "tutar|Tutar (TL)|number"), O("Kapanis", "Kapanis — İşlem kapatıldı", "Finans", "Tahsilat tamamlandı; cari kayıtlar kapatıldı.") },
                HesapEtiketler = { ["temlikOrani"] = "Temlik Oranı", ["iskonto"] = "İskonto / Fark (TL)", ["kalanAlacak"] = "Temlik Edende Kalan Alacak (TL)", ["vadeGun"] = "Vadeye Kalan Gün" },
                EkEtiketler = { ["noterNo"] = "Sözleşme / Noter No", ["sozlesmeTarihi"] = "Sözleşme Tarihi", ["bildirimYontemi"] = "Bildirim Yöntemi", ["bildirimTarihi"] = "Bildirim Tarihi", ["teyitTarihi"] = "Borçlu Teyidi", ["tahsilEdilen"] = "Tahsil Edilen (TL)", ["sonTahsilat"] = "Son Tahsilat", ["kapanisTarihi"] = "Kapanış" },
                Parametreler = { ["direktorEsik"] = "250000", ["vadeUyariGun"] = "7" },
                ParametreEtiketler = { ["direktorEsik"] = "Bu tutarı aşan temlikte Direktör onayı (TL)", ["vadeUyariGun"] = "Vadeye bu kadar gün kala uyarı" },
                Kurallar = { "Temlik tutarı alacak tutarını aşamaz; tam temlikte tutar alacağa eşit, kısmi temlikte küçük olmalıdır.", "Her temlik Muhasebe, Hukuk ve Finans onayından geçer; teminat amaçlı temlikte Direktör eklenir.", "Borçluya bildirim yapılmazsa borçlu eski alacaklıya ödeyerek borçtan kurtulabilir (TBK m.186); bildirim yapılmayan temlikler uyarı alır.", "Vadesi geçmiş alacak temlik edilemez; vadeye yakın alacaklarda uyarı verilir.", "Sözleşme, bildirim, teyit ve tahsilat Hukuk / Finans sistemlerinden gelir; tahsilat temlik tutarını aşamaz." },
                TakipSutunlar = { ("Talep No", "no"), ("Tür", "tur"), ("Temlik Eden", "d.temlikEden"), ("Temlik Alan", "d.temlikAlan"), ("Borçlu", "d.borclu"), ("Belge", "d.alacakBelgesi"), ("Alacak (TL)", "d.alacakTutari"), ("Temlik (TL)", "d.tutar"), ("Bedel (TL)", "d.bedel"), ("Vade", "d.vade"), ("Oran", "h.temlikOrani"), ("Tahsil (TL)", "e.tahsilEdilen"), ("Talep Durumu", "talepDurumu"), ("Sözleşme / Bildirim", "durum2"), ("Tahsilat", "durum3"), ("Sözleşme No", "e.noterNo"), ("Hukuk", "onay.Hukuk Onayı"), ("İşlemi Yapan", "kullanan") }
            };
            m.Hesapla = a =>
            {
                var s = new TmSonuc(); var m2 = a.Modul;
                if (a.V("temlikEden") == a.V("temlikAlan")) { s.Hata = "Temlik eden ve temlik alan şirket aynı olamaz."; return s; }
                decimal alacak = a.D("alacakTutari"), t = a.D("tutar"), bedel = a.D("bedel") <= 0 ? t : a.D("bedel");
                if (alacak <= 0 || t <= 0) { s.Hata = "Alacak ve temlik tutarı sıfırdan büyük olmalıdır."; return s; }
                if (t > alacak) { s.Hata = "Temlik tutarı alacak tutarını aşamaz."; return s; }
                if (a.TurKod == "KISMI" && t >= alacak) { s.Hata = "Kısmi temlikte temlik tutarı alacaktan küçük olmalıdır."; return s; }
                if ((a.TurKod == "TAM" || a.TurKod == "TEMINAT") && t < alacak) s.Uyarilar.Add("Tam / teminat temlikinde tutar alacağın tamamı olmalı; kısmi tutar girildi.");
                if (bedel > t) { s.Hata = "Temlik bedeli temlik tutarını aşamaz."; return s; }
                var vade = a.T("vade"); if (vade == null) { s.Hata = "Vade tarihi geçersiz."; return s; }
                int gun = (vade.Value.Date - DateTime.Today).Days;
                a.Hesap["temlikOrani"] = "%" + (t / alacak * 100).ToString("0.#", Tr); a.Hesap["iskonto"] = N(t - bedel, 2); a.Hesap["kalanAlacak"] = N(alacak - t, 2); a.Hesap["vadeGun"] = gun.ToString(); a.Hesap["bedelSayi"] = bedel.ToString("0.00", CultureInfo.InvariantCulture);
                if (gun < 0) { s.Hata = "Vadesi geçmiş alacak temlik edilemez; önce hukuki takip değerlendirilmeli."; return s; }
                if (gun <= m2.P("vadeUyariGun")) s.Uyarilar.Add($"Vadeye {gun} gün kaldı; bildirim borçluya vadeden önce ulaşmalı.");
                if (a.V("bildirim") != "true") s.Uyarilar.Add("Borçluya bildirim işaretlenmedi: borçlu eski alacaklıya ödeyebilir (TBK m.186).");
                if (t > m2.P("direktorEsik")) { s.EkAdimlar.Add("D"); s.Uyarilar.Add($"Temlik tutarı {N(t)} TL, {N(m2.P("direktorEsik"))} TL eşiğini aşıyor: Direktör onayı eklendi."); }
                if (a.TurKod == "RUCU" && a.V("rucu") != "true") s.Uyarilar.Add("Rücu edilebilir temlik seçildi; 'Rücu hakkı saklı' işaretlenmeli.");
                if (Liste("TEMLIK").Any(x => x.Id != a.Id && !x.Iptal && x.TalepDurumu != "Reddedildi" && x.V("alacakBelgesi") == a.V("alacakBelgesi") && x.V("temlikEden") == a.V("temlikEden"))) s.Uyarilar.Add("Aynı alacak belgesi için başka temlik kaydı var; kısmi temliklerin toplamı alacağı aşmamalı.");
                return s;
            };
            m.Durum2 = a => a.Ek.ContainsKey("teyitTarihi") ? "Borçlu Teyit Etti" : a.Ek.ContainsKey("bildirimTarihi") ? "Borçluya Bildirildi" : a.Ek.ContainsKey("sozlesmeTarihi") ? "Sözleşme İmzalandı" : "Sözleşme Bekliyor";
            m.Durum3 = a => { if (a.Ek.ContainsKey("kapanisTarihi")) return "Kapandı"; decimal th = decimal.TryParse(a.E("tahsilEdilen"), NumberStyles.Any, Tr, out var v) ? v : 0; return th >= a.D("tutar") && th > 0 ? "Tahsil Edildi" : th > 0 ? "Kısmi Tahsil" : a.Ek.ContainsKey("sozlesmeTarihi") ? "Tahsilat Bekliyor" : "—"; };
            m.Olay = (a, kod, g) =>
            {
                switch (kod)
                {
                    case "SozlesmeImzalandi": { if (a.Ek.ContainsKey("sozlesmeTarihi")) return "Sözleşme zaten imzalanmış."; a.Ek["noterNo"] = G(g, "noterNo") == "" ? (a.V("sozlesmeNo") == "" ? $"NT-{Simdi:yyyy}-{(3000 + a.Id):0000}" : a.V("sozlesmeNo")) : G(g, "noterNo"); a.Ek["sozlesmeTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, $"Hukuk: temlik sözleşmesi imzalandı ({a.E("noterNo")})", "Hukuk"); L(a, "CONTRACT", "", a.E("noterNo"), "Hukuk"); return null; }
                    case "BorcluyaBildirildi": { if (!a.Ek.ContainsKey("sozlesmeTarihi")) return "Sözleşme imzalanmadan bildirim yapılamaz."; a.Ek["bildirimYontemi"] = G(g, "yontem") == "" ? "Noter İhbarnamesi" : G(g, "yontem"); a.Ek["bildirimTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, $"Hukuk: borçlu {a.V("borclu")} bilgilendirildi ({a.E("bildirimYontemi")})", "Hukuk"); L(a, "NOTIFIED", "false", "true", "Hukuk"); return null; }
                    case "BorcluTeyit": { if (!a.Ek.ContainsKey("bildirimTarihi")) return "Bildirim yapılmadan teyit alınamaz."; a.Ek["teyitTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, $"Borçlu {a.V("borclu")} temliği teyit etti; ödeme {a.V("temlikAlan")} şirketine yapılacak", "Portal"); L(a, "DEBTOR_CONFIRMED", "false", "true", "Portal"); return null; }
                    case "Tahsilat": { if (!a.Ek.ContainsKey("sozlesmeTarihi")) return "Sözleşme olmadan tahsilat kaydı işlenemez."; decimal t = GD(g, "tutar"); decimal mevcut = decimal.TryParse(a.E("tahsilEdilen"), NumberStyles.Any, Tr, out var mv) ? mv : 0; if (t <= 0) t = a.D("tutar") - mevcut; if (t <= 0 || mevcut + t > a.D("tutar")) return $"Tahsilat 0 ile kalan temlik tutarı ({N(a.D("tutar") - mevcut, 2)} TL) arasında olmalı."; a.Ek["tahsilEdilen"] = N(mevcut + t, 2); a.Ek["sonTahsilat"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, $"Finans/Kasa: {a.V("temlikAlan")} {N(t, 2)} TL tahsil etti (toplam {a.E("tahsilEdilen")} / {N(a.D("tutar"), 2)} TL)", "Finans/Kasa"); L(a, "COLLECTED", N(mevcut, 2), a.E("tahsilEdilen"), "Finans/Kasa"); return null; }
                    case "Kapanis": { if (a.Durum3 != "Tahsil Edildi") return "Temlik tutarının tamamı tahsil edilmeden kapanış yapılamaz."; if (a.Ek.ContainsKey("kapanisTarihi")) return "Zaten kapanmış."; a.Ek["kapanisTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, "Finans: temlik kapatıldı; cari ve alacak kayıtları güncellendi", "Finans"); L(a, "STATUS", "Açık", "Kapandı", "Finans"); return null; }
                }
                return "Tanımsız olay.";
            };
            m.Ozet = a => $"{a.V("temlikEden")} → {a.V("temlikAlan")} · borçlu {a.V("borclu")} · {N(a.D("tutar"))} / {N(a.D("alacakTutari"))} TL · vade {a.T("vade"):dd.MM.yyyy}";
            m.Etiketle = a => { var l = new List<string>(); bool ok = a.Onaylandi && !a.Iptal && a.Durum3 != "Kapandı"; if (a.TalepDurumu.EndsWith("Bekliyor") && !a.TalepDurumu.StartsWith("Revizyon")) l.Add("onay"); if (ok && a.Durum2 == "Sözleşme Bekliyor") l.Add("sozlesme"); if (ok && a.Durum2 == "Sözleşme İmzalandı") l.Add("bildirim"); if (ok && (a.Durum3 == "Tahsilat Bekliyor" || a.Durum3 == "Kısmi Tahsil")) l.Add("tahsilat"); var v = a.T("vade"); if (ok && v.HasValue && (v.Value.Date - DateTime.Today).Days <= 7 && (v.Value.Date - DateTime.Today).Days >= 0) l.Add("vade"); if (ok) l.Add("acik"); return l; };
            m.Kpiler = l => new List<TmKpi> { K("#b45309", "Sözleşme Bekleyen", $"{l.Count(t => t.Etiketler.Contains("sozlesme"))} Temlik", Adlar(l.Where(t => t.Etiketler.Contains("sozlesme"))), "sozlesme"), K("#7c3aed", "Bildirim Bekleyen", $"{l.Count(t => t.Etiketler.Contains("bildirim"))} Temlik", "sözleşme var, ihbarname yok", "bildirim"), K("#0ea5e9", "Tahsilat Bekleyen", $"{l.Count(t => t.Etiketler.Contains("tahsilat"))} Temlik", N(l.Where(t => t.Etiketler.Contains("tahsilat")).Sum(t => t.D("tutar") - (decimal.TryParse(t.E("tahsilEdilen"), NumberStyles.Any, Tr, out var th) ? th : 0))) + " TL kalan", "tahsilat"), K("#dc2626", "Vadesi Yaklaşan", $"{l.Count(t => t.Etiketler.Contains("vade"))} Alacak", "7 gün içinde", "vade"), K("#16a34a", "Toplam Açık Temlik", N(l.Where(t => t.Etiketler.Contains("acik")).Sum(t => t.D("tutar"))) + " TL", $"{l.Count(t => t.Etiketler.Contains("acik"))} işlem", "acik"), K("#f59e0b", "Onay Bekleyen", $"{l.Count(t => t.Etiketler.Contains("onay"))} Talep", N(l.Where(t => t.Etiketler.Contains("onay")).Sum(t => t.D("tutar"))) + " TL", "onay") };
            return m;
        }

        private static void GrupIciTohum()
        {
            // ---- Yansıtma ----
            Dictionary<string, string> Y(string yansitan, string yansitilan, string tur, string donem, string tutar, string kdv, string anahtar, string dayanak, string aciklama) => new Dictionary<string, string> { ["yansitan"] = yansitan, ["yansitilan"] = yansitilan, ["masrafTuru"] = tur, ["donem"] = donem, ["tutar"] = tutar, ["kdvOrani"] = kdv, ["dagitimAnahtari"] = anahtar, ["dayanak"] = dayanak, ["masrafMerkezi"] = "CC10 - Bilgi Teknolojileri", ["aciklama"] = aciklama };
            var y1 = TalepMotoru.Tohum("YANSITMA", "Nazlı Erdem", "Nazlı Erdem", "MASRAF", Y("Uras Holding", "Uras Kimya", "IT Lisans / Hizmet", Donem(2), "184000", "20", "Kullanım / Sayaç", "MS-2026-118 (Microsoft lisans)", "Grup lisans sözleşmesinin Uras Kimya kullanıcı payı."), 40);
            TalepMotoru.TohumOlay(y1, "FaturaKesildi", null, 36); TalepMotoru.TohumOlay(y1, "KarsiKabul", null, 34); TalepMotoru.TohumOlay(y1, "MuhasebeFisi", new Dictionary<string, string> { ["taraf"] = "Yansıtan" }, 33); TalepMotoru.TohumOlay(y1, "MuhasebeFisi", new Dictionary<string, string> { ["taraf"] = "Yansıtılan" }, 31); TalepMotoru.TohumOlay(y1, "Mutabakat", null, 28); TalepMotoru.TohumOlay(y1, "Kapanis", null, 27);
            var y2 = TalepMotoru.Tohum("YANSITMA", "Nazlı Erdem", "Nazlı Erdem", "KIRA", Y("Uras Holding", "Selvi", "Kira", Donem(1), "96000", "20", "Metrekare", "Kira sözleşmesi 2024/07", "Merkez bina 3. kat ortak alan payı."), 12);
            TalepMotoru.TohumOlay(y2, "FaturaKesildi", null, 10); TalepMotoru.TohumOlay(y2, "KarsiKabul", null, 8); TalepMotoru.TohumOlay(y2, "MuhasebeFisi", new Dictionary<string, string> { ["taraf"] = "Yansıtan" }, 7);
            var y3 = TalepMotoru.Tohum("YANSITMA", "Onur Bal", "Onur Bal", "PERSONEL", Y("Uras Kimya", "Alv Kimya", "Personel Maliyeti", Donem(1), "142500", "20", "Personel Sayısı", "Görevlendirme yazısı İK-2026-41", "Bakım ekibinin Alv Kimya sahasında 3 haftalık görevlendirmesi."), 9);
            TalepMotoru.TohumOlay(y3, "FaturaKesildi", null, 6); TalepMotoru.TohumOlay(y3, "KarsiRed", new Dictionary<string, string> { ["neden"] = "Görevlendirme günü 15 değil 12; tutar revize edilmeli" }, 4);
            var y4 = TalepMotoru.Tohum("YANSITMA", "Nazlı Erdem", "Nazlı Erdem", "MASRAF", Y("Uras Holding", "Avrupa Paper", "Sigorta", Donem(1), "58400", "0", "Sabit Tutar", "Grup sigorta poliçesi 7781", "Grup poliçesinde Avrupa Paper payı; sigorta KDV istisnalı."), 5);
            TalepMotoru.TohumOlay(y4, "FaturaKesildi", null, 3);
            TalepMotoru.Tohum("YANSITMA", "Hülya Er", "Hülya Er", "HIZMET", Y("Uras Holding", "Daf Kimya", "Yönetim Hizmet Bedeli", Donem(1), "75000", "20", "Ciro Oranı", "Hizmet sözleşmesi 2025/03", "Aylık yönetim hizmet bedeli."), 3);
            var y6 = TalepMotoru.Tohum("YANSITMA", "Nazlı Erdem", "Nazlı Erdem", "MASRAF", Y("Uras Kimya", "Uras Holding", "Ortak Gider Payı", Donem(0), "312000", "20", "Ciro Oranı", "Ortak gider dağıtım tablosu", "Merkez bina ortak gider payı (elektrik, güvenlik, temizlik)."), 1, 1);
            var y7 = TalepMotoru.Tohum("YANSITMA", "Onur Bal", "Onur Bal", "MASRAF", Y("Uras Kimya", "Selvi", "Nakliye", Donem(4), "21500", "20", "Kullanım / Sayaç", "Nakliye irsaliyeleri", "Selvi adına yapılan sevkiyatların nakliye payı."), 2, 0); TalepMotoru.TohumRed(y7, "Dönem kapanmış; düzeltme faturası ile ilerleyin.");

            // ---- Mahsup ----
            Dictionary<string, string> Mh(string a, string b, string tutar, int gun, string dayanak, string aciklama, string c = "", string kap = "Kısmi") => new Dictionary<string, string> { ["sirketA"] = a, ["sirketB"] = b, ["sirketC"] = c, ["tutar"] = tutar, ["tarih"] = DateTime.Today.AddDays(gun).ToString("yyyy-MM-dd"), ["dayanak"] = dayanak, ["kapanisTuru"] = kap, ["aciklama"] = aciklama };
            var h1 = TalepMotoru.Tohum("MAHSUP", "Nazlı Erdem", "Nazlı Erdem", "KARSILIKLI", Mh("Uras Kimya", "Uras Holding", "400000", -30, "Mahsup protokolü 2026/09-1", "Ay sonu karşılıklı bakiye netleştirmesi."), 33);
            TalepMotoru.TohumOlay(h1, "FisKaydi", new Dictionary<string, string> { ["taraf"] = "A" }, 29); TalepMotoru.TohumOlay(h1, "FisKaydi", new Dictionary<string, string> { ["taraf"] = "B" }, 29, 14); TalepMotoru.TohumOlay(h1, "Mutabakat", new Dictionary<string, string> { ["sonuc"] = "Mutabık" }, 26); TalepMotoru.TohumOlay(h1, "Kapanis", null, 25);
            var h2 = TalepMotoru.Tohum("MAHSUP", "Onur Bal", "Onur Bal", "KARSILIKLI", Mh("Selvi", "Uras Kimya", "300000", -6, "Mahsup protokolü 2026/09-2", "Selvi alış faturaları ile Uras Kimya hizmet faturalarının netleştirilmesi."), 8);
            TalepMotoru.TohumOlay(h2, "FisKaydi", new Dictionary<string, string> { ["taraf"] = "A" }, 5); TalepMotoru.TohumOlay(h2, "FisKaydi", new Dictionary<string, string> { ["taraf"] = "B" }, 4); TalepMotoru.TohumOlay(h2, "Mutabakat", new Dictionary<string, string> { ["sonuc"] = "Fark Var", ["fark"] = "2350" }, 2);
            var h3 = TalepMotoru.Tohum("MAHSUP", "Nazlı Erdem", "Nazlı Erdem", "AVANS", Mh("Uras Holding", "Avrupa Paper", "275000", -3, "Avans protokolü", "Verilen grup içi avansın ay sonu faturalarıyla kapatılması.", "", "Tam Kapanış"), 5);
            TalepMotoru.TohumOlay(h3, "FisKaydi", new Dictionary<string, string> { ["taraf"] = "A" }, 2);
            var h4 = TalepMotoru.Tohum("MAHSUP", "Hülya Er", "Hülya Er", "UCLU", Mh("Uras Power", "Uras Holding", "600000", 4, "Üçlü mahsup protokolü 2026/09-3", "Uras Power'ın Holding'e borcunun Uras Kimya alacağıyla üçlü kapatılması.", "Uras Kimya"), 2, 1);
            TalepMotoru.Tohum("MAHSUP", "Nazlı Erdem", "Nazlı Erdem", "FATURA", Mh("Alv Kimya", "Uras Kimya", "120000", 2, "Faturalar 2026-09", "Karşılıklı hizmet faturalarının mahsubu."), 1, 0);
            var h6 = TalepMotoru.Tohum("MAHSUP", "Onur Bal", "Onur Bal", "KARSILIKLI", Mh("Daf Kimya", "Uras Holding", "89000", -12, "Mahsup protokolü 2026/09-4", "Daf Kimya küçük bakiye kapatma.", "", "Tam Kapanış"), 14, 0); TalepMotoru.TohumRevizyon(h6, "Tutar 89.000 değil 85.000; Holding tarafında kayıt eksik.");

            // ---- Temlik ----
            Dictionary<string, string> Tk(string eden, string alan, string borclu, string belge, string alacak, string tutar, string bedel, int vadeGun, bool bildirim, string aciklama, bool rucu = false) => new Dictionary<string, string> { ["temlikEden"] = eden, ["temlikAlan"] = alan, ["borclu"] = borclu, ["alacakBelgesi"] = belge, ["alacakTutari"] = alacak, ["tutar"] = tutar, ["bedel"] = bedel, ["vade"] = DateTime.Today.AddDays(vadeGun).ToString("yyyy-MM-dd"), ["bildirim"] = bildirim ? "true" : "false", ["rucu"] = rucu ? "true" : "false", ["sozlesmeNo"] = "", ["aciklama"] = aciklama };
            var t1 = TalepMotoru.Tohum("TEMLIK", "Nazlı Erdem", "Nazlı Erdem", "TAM", Tk("Uras Kimya", "Uras Holding", "AKSA TEKSTİL SAN. VE TİC. A.Ş.", "URS2026000000842", "185000", "185000", "185000", 25, true, "Holding'in Aksa'dan tahsilatı; Uras Kimya'nın Holding'e borcuna karşılık."), 20);
            TalepMotoru.TohumOlay(t1, "SozlesmeImzalandi", null, 16); TalepMotoru.TohumOlay(t1, "BorcluyaBildirildi", new Dictionary<string, string> { ["yontem"] = "Noter İhbarnamesi" }, 14); TalepMotoru.TohumOlay(t1, "BorcluTeyit", null, 10);
            var t2 = TalepMotoru.Tohum("TEMLIK", "Onur Bal", "Onur Bal", "KISMI", Tk("Selvi", "Uras Kimya", "DENİZ BOYA KİMYA LTD. ŞTİ.", "SLV2026000000317", "240000", "150000", "150000", 40, true, "Selvi alacağının bir bölümünün Uras Kimya'ya devri."), 12);
            TalepMotoru.TohumOlay(t2, "SozlesmeImzalandi", null, 9); TalepMotoru.TohumOlay(t2, "BorcluyaBildirildi", new Dictionary<string, string> { ["yontem"] = "KEP" }, 8); TalepMotoru.TohumOlay(t2, "BorcluTeyit", null, 6); TalepMotoru.TohumOlay(t2, "Tahsilat", new Dictionary<string, string> { ["tutar"] = "60000" }, 3);
            var t3 = TalepMotoru.Tohum("TEMLIK", "Nazlı Erdem", "Nazlı Erdem", "TAM", Tk("Avrupa Paper", "Uras Holding", "EGE MAKROSEL AMBALAJ A.Ş.", "ÇEK 0004521 / Garanti", "98000", "98000", "98000", 60, true, "Müşteri çekinin Holding'e temliki (ay sonu kapanış)."), 45);
            TalepMotoru.TohumOlay(t3, "SozlesmeImzalandi", null, 42); TalepMotoru.TohumOlay(t3, "BorcluyaBildirildi", null, 40); TalepMotoru.TohumOlay(t3, "BorcluTeyit", null, 38); TalepMotoru.TohumOlay(t3, "Tahsilat", null, 20); TalepMotoru.TohumOlay(t3, "Kapanis", null, 19);
            TalepMotoru.Tohum("TEMLIK", "Hülya Er", "Hülya Er", "TEMINAT", Tk("Daf Kimya", "Uras Holding", "KARADENİZ EMPRİME SAN. LTD.", "DAF2026000000118", "320000", "320000", "300000", 55, true, "Holding'in Daf Kimya'ya kullandırdığı kredinin teminatı."), 2, 1);
            TalepMotoru.Tohum("TEMLIK", "Nazlı Erdem", "Nazlı Erdem", "RUCU", Tk("Uras Kimya", "Uras Power", "MAVİ ÖRME KUMAŞ SAN. TİC.", "URS2026000000901", "64000", "64000", "60000", 5, false, "Uras Power nakit ihtiyacı; rücu hakkı saklı.", true), 1, 0);
            var t6 = TalepMotoru.Tohum("TEMLIK", "Onur Bal", "Onur Bal", "KISMI", Tk("Selvi", "Uras Holding", "ANADOLU LOJİSTİK A.Ş.", "SLV2026000000290", "410000", "200000", "200000", 30, true, "Lojistik alacağının kısmi temliki."), 6, 0); TalepMotoru.TohumRed(t6, "Borçlu ile ödeme planı görüşülüyor; temlik ertelendi.");
        }

        // =====================================================================
        // MESAİ
        // =====================================================================
        private static TmModul Mesai()
        {
            var m = new TmModul
            {
                Kod = "MESAI", Ad = "Mesai Talebi", NoOnEk = "MS", Ikon = "fa-business-time", Renk = "#6366f1",
                Aciklama = "Fazla mesai, hafta sonu, resmi tatil ve gece çalışması için önceden talep açılır; onaysız mesai puantaja işlenmez. Fiili saat PDKS giriş-çıkışından, puantaj bordro sisteminden gelir.",
                AkisOzeti = "Talep → Yönetici → (Direktör) → (İK) → PDKS gerçekleşme → Puantaj (ücret / serbest zaman)",
                KullananEtiket = "Mesai yapacak personel", Durum2Ad = "Gerçekleşme Durumu (PDKS)", Durum3Ad = "Puantaj Durumu (Bordro)",
                TakvimSatir = "kisi", TakvimBas = "tarih", TakvimBit = "tarih", TakvimAd = "Mesai Takvimi",
                Turler = { T("HAFTAICI", "Hafta İçi Fazla Mesai", "Y", "Günlük 11 saat sınırı; ücret %50 zamlı ya da 1,5 kat serbest zaman."), T("HAFTASONU", "Hafta Sonu Çalışması", "Y,I", "Hafta tatili çalışması; İK onayı gerekir."), T("TATIL", "Resmi Tatil Çalışması", "Y,D,I", "Ulusal bayram / genel tatil; Direktör ve İK onayı, ücret 2 kat."), T("GECE", "Gece Vardiyası (20:00–06:00)", "Y,I", "Gece çalışması 7,5 saati aşamaz.") },
                Alanlar = { A("tarih", "Mesai Tarihi", "date", true, 3), A("baslangicSaat", "Başlangıç Saati", "time", true, 2, vars: "18:00"), A("bitisSaat", "Bitiş Saati", "time", true, 2, vars: "21:00"), A("mola", "Mola (dk)", "number", false, 2, vars: "30"), A("karsilik", "Karşılığı", "select", true, 3, new[] { "Ücret", "Serbest Zaman (İzin)" }, vars: "Ücret"),
                            A("proje", "İş / Proje / İş Emri", "text", false, 6, ipucu: "Örn. Hat 2 bakım, ay sonu kapanış"), A("servis", "Servis gerekli", "checkbox", false, 3), A("yemek", "Yemek gerekli", "checkbox", false, 3), A("gerekce", "Yapılacak İş / Gerekçe", "textarea", true, 12) },
                Olaylar = { O("PdksGiris", "PdksGiris — PDKS giriş / çıkış kaydı", "PDKS", "Fiili çalışma saati giriş-çıkıştan hesaplanır (mola düşülür).", "giris|Fiili Giriş Saati|time", "cikis|Fiili Çıkış Saati|time"), O("PuantajAktarildi", "PuantajAktarildi — Bordro dönem kapanışı", "Bordro", "Fiili saat ücret ya da serbest zaman bakiyesi olarak aktarılır.", "donem|Puantaj Dönemi (AAAA-AA)|text"), O("PuantajTersKayit", "PuantajTersKayit — Bordro ters kayıt", "Bordro", "Aktarım geri alınır; durum yeniden hesaplanır.") },
                HesapEtiketler = { ["planlananSaat"] = "Planlanan Saat", ["carpan"] = "Ücret Çarpanı", ["serbestZaman"] = "Serbest Zaman Karşılığı (saat)", ["yillikToplam"] = "Bu Yıl Onaylı Mesai (saat)", ["kalanSinir"] = "270 Saat Sınırına Kalan" },
                EkEtiketler = { ["fiiliGiris"] = "Fiili Giriş", ["fiiliCikis"] = "Fiili Çıkış", ["fiiliSaat"] = "Fiili Saat", ["puantajDonemi"] = "Puantaj Dönemi", ["puantajSaat"] = "Puantaja Aktarılan (saat)", ["puantajTur"] = "Aktarım Türü", ["puantajTarihi"] = "Puantaj Tarihi" },
                Parametreler = { ["yillikSinir"] = "270", ["gunlukSinir"] = "3", ["direktorSaat"] = "4", ["carpanHaftaici"] = "1.5", ["carpanHaftasonu"] = "2", ["carpanTatil"] = "2", ["carpanGece"] = "1.5", ["serbestKat"] = "1.5" },
                ParametreEtiketler = { ["yillikSinir"] = "Yıllık fazla mesai sınırı (saat, 4857 m.41)", ["gunlukSinir"] = "Günlük azami fazla mesai (saat)", ["direktorSaat"] = "Bu saatten uzun mesaide Direktör onayı", ["carpanHaftaici"] = "Hafta içi çarpan", ["carpanHaftasonu"] = "Hafta sonu çarpan", ["carpanTatil"] = "Resmi tatil çarpan", ["carpanGece"] = "Gece çarpan", ["serbestKat"] = "Serbest zaman katsayısı" },
                Kurallar = { "Onaysız mesai puantaja işlenmez; talep mesai gününden önce açılır.", "Fiili saat PDKS'den gelir, elle girilmez; planlanan ile fiili farkı ekranda görünür.", "Yıllık 270 saat sınırı kişi bazında canlı izlenir; sınıra yaklaşınca uyarı, aşınca engel.", "Resmi tatil ve 4 saati aşan mesailerde Direktör onayı; hafta sonu ve gece çalışmasında İK onayı eklenir.", "Karşılık 'Serbest Zaman' ise 1 saat = 1,5 saat izin bakiyesine eklenir." },
                TakipSutunlar = { ("Talep No", "no"), ("Personel", "kullanan"), ("Departman", "departman"), ("Tür", "tur"), ("Tarih", "d.tarih"), ("Saat", "h.aralik"), ("Planlanan (sa)", "h.planlananSaat"), ("Fiili (sa)", "e.fiiliSaat"), ("Çarpan", "h.carpan"), ("Karşılık", "d.karsilik"), ("İş / Proje", "d.proje"), ("Talep Durumu", "talepDurumu"), ("Gerçekleşme", "durum2"), ("Puantaj", "durum3"), ("Dönem", "e.puantajDonemi"), ("Yönetici", "onay.Yönetici Onayı") }
            };
            m.Hesapla = a =>
            {
                var s = new TmSonuc(); var m2 = a.Modul;
                var tarih = a.T("tarih"); if (tarih == null) { s.Hata = "Mesai tarihi geçersiz."; return s; }
                decimal saat = SaatFarki(a.V("baslangicSaat"), a.V("bitisSaat")) - a.D("mola") / 60m; if (saat <= 0) { s.Hata = "Bitiş saati başlangıçtan sonra olmalı (mola düşülünce süre kalmıyor)."; return s; }
                saat = Math.Round(saat, 2);
                decimal carpan = a.TurKod == "TATIL" ? m2.P("carpanTatil") : a.TurKod == "HAFTASONU" ? m2.P("carpanHaftasonu") : a.TurKod == "GECE" ? m2.P("carpanGece") : m2.P("carpanHaftaici");
                var yil = Liste("MESAI").Where(t => t.Id != a.Id && t.Kullanan.PersonelNo == a.Kullanan.PersonelNo && t.Onaylandi && !t.Iptal && (t.T("tarih")?.Year ?? 0) == DateTime.Today.Year).Sum(t => decimal.TryParse(t.E("fiiliSaat"), NumberStyles.Any, CultureInfo.InvariantCulture, out var f) ? f : (decimal.TryParse(t.Hesap.TryGetValue("planlananSaat", out var ps) ? ps : "0", NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0));
                a.Hesap["planlananSaat"] = saat.ToString("0.##", CultureInfo.InvariantCulture); a.Hesap["carpan"] = "x" + carpan.ToString("0.##", Tr); a.Hesap["serbestZaman"] = a.V("karsilik").StartsWith("Serbest") ? (saat * m2.P("serbestKat")).ToString("0.##", Tr) : "—";
                a.Hesap["yillikToplam"] = yil.ToString("0.#", Tr); a.Hesap["kalanSinir"] = Math.Max(0, m2.P("yillikSinir") - yil - saat).ToString("0.#", Tr); a.Hesap["aralik"] = a.V("baslangicSaat") + "–" + a.V("bitisSaat");
                if (yil + saat > m2.P("yillikSinir")) { s.Hata = $"Yıllık fazla mesai sınırı aşılıyor: bu yıl {S(yil)} saat + talep {S(saat)} saat > {S(m2.P("yillikSinir"))} saat (4857 m.41)."; return s; }
                if (yil + saat > m2.P("yillikSinir") * 0.9m) s.Uyarilar.Add($"Yıllık sınıra yaklaşıldı: bu yıl {S(yil)} saat, kalan {S(m2.P("yillikSinir") - yil - saat)} saat.");
                if (tarih.Value.Date < DateTime.Today) s.Uyarilar.Add("Geçmiş tarihli mesai: onay sonrası PDKS kaydıyla eşleştirilir.");
                if (a.TurKod == "HAFTAICI" && saat > m2.P("gunlukSinir")) s.Uyarilar.Add($"Günlük 11 saat çalışma sınırı: hafta içi fazla mesai en fazla {S(m2.P("gunlukSinir"))} saat olmalı.");
                if (a.TurKod == "GECE" && saat > 7.5m) s.Uyarilar.Add("Gece çalışması 7,5 saati aşamaz (4857 m.69).");
                if (a.TurKod == "HAFTASONU" && !HaftaSonu(tarih.Value)) s.Uyarilar.Add("Seçilen tarih hafta sonu değil; tür 'Hafta İçi Fazla Mesai' olmalı.");
                if (a.TurKod == "TATIL" && !IzinOrnek.Tatiller.ContainsKey(tarih.Value.Date)) s.Uyarilar.Add("Seçilen tarih resmi tatil değil.");
                if (a.TurKod == "HAFTAICI" && (HaftaSonu(tarih.Value) || IzinOrnek.TatilMi(tarih.Value))) s.Uyarilar.Add("Tarih hafta sonu / tatil; tür buna göre seçilmeli (çarpan değişir).");
                if (IzinOrnek.Cakisanlar(a.Kullanan, tarih.Value, tarih.Value).Any(x => x.Onaylandi)) { s.Hata = "Personel bu tarihte izinli görünüyor; mesai açılamaz."; return s; }
                if (Liste("MESAI").Any(t => t.Id != a.Id && !t.Iptal && t.TalepDurumu != "Reddedildi" && t.Kullanan.PersonelNo == a.Kullanan.PersonelNo && t.V("tarih") == a.V("tarih"))) { s.Hata = "Aynı tarihte bu personel için başka mesai talebi var."; return s; }
                if (saat > m2.P("direktorSaat")) { s.EkAdimlar.Add("D"); s.Uyarilar.Add($"{S(m2.P("direktorSaat"))} saati aşan mesai: Direktör onayı eklendi."); }
                return s;
            };
            m.Durum2 = a =>
            {
                var tarih = a.T("tarih")?.Date ?? DateTime.Today; decimal plan = a.D("planlananSaat") == 0 ? decimal.Parse(a.Hesap["planlananSaat"], CultureInfo.InvariantCulture) : 0;
                if (a.Ek.ContainsKey("fiiliSaat")) { decimal f = a.D("fiiliSaat", true); return f <= 0 ? "Gerçekleşmedi" : f >= plan * 0.9m ? "Gerçekleşti" : "Kısmen Gerçekleşti"; }
                return tarih < DateTime.Today ? "PDKS Kaydı Bekleniyor" : tarih == DateTime.Today ? "Bugün Planlı" : "Planlandı";
            };
            m.Durum3 = a => a.Ek.ContainsKey("puantajTarihi") ? (a.E("puantajTur").StartsWith("Serbest") ? "Serbest Zaman Bakiyesine Eklendi" : "Ücret Olarak Aktarıldı") : a.Ek.ContainsKey("fiiliSaat") ? (a.D("fiiliSaat", true) > 0 ? "Puantaj Bekliyor" : "Aktarılacak Saat Yok") : "Gerçekleşme Bekleniyor";
            m.Olay = (a, kod, g) =>
            {
                switch (kod)
                {
                    case "PdksGiris":
                        { string gi = G(g, "giris"), ci = G(g, "cikis"); if (gi == "" || ci == "") return "Giriş ve çıkış saati zorunludur."; string mesaiBas = TimeSpan.TryParse(gi, out var gts) && TimeSpan.TryParse(a.V("baslangicSaat"), out var bts) && gts < bts && a.TurKod == "HAFTAICI" ? a.V("baslangicSaat") : gi; decimal f = Math.Max(0, Math.Round(SaatFarki(mesaiBas, ci) - a.D("mola") / 60m, 2)); a.Ek["fiiliGiris"] = gi; a.Ek["fiiliCikis"] = ci; a.Ek["fiiliSaat"] = f.ToString("0.##", CultureInfo.InvariantCulture); a.Ek.Remove("puantajTarihi"); H(a, $"PDKS: giriş {gi} – çıkış {ci}; fiili {S(f)} saat (planlanan {a.Hesap["planlananSaat"]})", "PDKS"); L(a, "ACTUAL_HOURS", "", S(f), "PDKS"); return null; }
                    case "PuantajAktarildi":
                        { if (!a.Ek.ContainsKey("fiiliSaat")) return "PDKS kaydı gelmeden puantaja aktarılamaz."; if (a.D("fiiliSaat", true) <= 0) return "Fiili saat sıfır; aktarılacak mesai yok."; if (a.Ek.ContainsKey("puantajTarihi")) return "Zaten aktarılmış."; string d = G(g, "donem"); if (d == "") d = a.T("tarih")?.ToString("yyyy-MM") ?? Simdi.ToString("yyyy-MM"); a.Ek["puantajDonemi"] = d; a.Ek["puantajSaat"] = a.E("fiiliSaat"); a.Ek["puantajTur"] = a.V("karsilik"); a.Ek["puantajTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, $"Bordro: {d} dönemi puantajına {S(a.D("fiiliSaat", true))} saat {(a.V("karsilik").StartsWith("Serbest") ? "serbest zaman (" + S(a.D("fiiliSaat", true) * a.Modul.P("serbestKat")) + " saat izin)" : "fazla mesai ücreti (" + a.Hesap["carpan"] + ")")} olarak aktarıldı", "Bordro"); L(a, "PAYROLL_STATUS", "Bekliyor", "Aktarıldı", "Bordro"); return null; }
                    case "PuantajTersKayit":
                        { if (!a.Ek.ContainsKey("puantajTarihi")) return "Aktarılmış puantaj yok."; a.Ek.Remove("puantajTarihi"); a.Ek.Remove("puantajSaat"); a.Ek.Remove("puantajDonemi"); H(a, "Bordro: puantaj ters kaydı işlendi; durum yeniden hesaplandı", "Bordro"); L(a, "PAYROLL_STATUS", "Aktarıldı", "Bekliyor", "Bordro"); return null; }
                }
                return "Tanımsız olay.";
            };
            m.Ozet = a => $"{a.T("tarih"):dd.MM.yyyy} {a.V("baslangicSaat")}–{a.V("bitisSaat")} · {a.Hesap["planlananSaat"]} sa · {a.V("karsilik")}";
            m.Etiketle = a => { var l = new List<string>(); var d = a.T("tarih")?.Date; if (a.Onaylandi && !a.Iptal && d == DateTime.Today) l.Add("bugun"); if (a.Onaylandi && !a.Iptal && d > DateTime.Today && d <= DateTime.Today.AddDays(7)) l.Add("hafta"); if (a.Durum2 == "PDKS Kaydı Bekleniyor") l.Add("pdks"); if (a.Durum3 == "Puantaj Bekliyor") l.Add("puantaj"); if (a.TalepDurumu.EndsWith("Bekliyor") && !a.TalepDurumu.StartsWith("Revizyon")) l.Add("onay"); if (decimal.TryParse(a.Hesap.TryGetValue("kalanSinir", out var k) ? k : "999", NumberStyles.Any, Tr, out var ks) && ks < 30 && a.Onaylandi) l.Add("sinir"); return l; };
            m.Kpiler = l =>
            {
                var bugun = l.Where(t => t.Etiketler.Contains("bugun")).ToList(); var hafta = l.Where(t => t.Etiketler.Contains("hafta")).ToList();
                decimal saat(IEnumerable<TmTalep> x) => x.Sum(t => decimal.Parse(t.Hesap["planlananSaat"], CultureInfo.InvariantCulture));
                var sinir = l.Where(t => t.Etiketler.Contains("sinir")).Select(t => t.Kullanan.AdSoyad).Distinct().ToList();
                return new List<TmKpi> { K("#6366f1", "Bugün Mesaide", $"{bugun.Count} Kişi", $"{S(saat(bugun))} saat · {Adlar(bugun)}", "bugun"), K("#7c3aed", "Bu Hafta Planlanan", $"{hafta.Count} Mesai", $"{S(saat(hafta))} saat", "hafta"), K("#f59e0b", "Onay Bekleyen", $"{l.Count(t => t.Etiketler.Contains("onay"))} Talep", $"{S(saat(l.Where(t => t.Etiketler.Contains("onay"))))} saat", "onay"), K("#0ea5e9", "PDKS Kaydı Bekleyen", $"{l.Count(t => t.Etiketler.Contains("pdks"))} Mesai", "tarihi geçti, giriş-çıkış yok", "pdks"), K("#16a34a", "Puantaj Bekleyen", $"{l.Count(t => t.Etiketler.Contains("puantaj"))} Mesai", $"{S(l.Where(t => t.Etiketler.Contains("puantaj")).Sum(t => t.D("fiiliSaat", true)))} fiili saat", "puantaj"), K("#dc2626", "270 Saat Sınırına Yaklaşan", $"{sinir.Count} Kişi", Bos(string.Join(", ", sinir)), "sinir") };
            };
            return m;
        }
        private static void MesaiTohum()
        {
            Dictionary<string, string> D(string tarih, string bas, string bit, string kars, string proje, string gerekce, string mola = "30") => new Dictionary<string, string> { ["tarih"] = DateTime.Today.AddDays(int.Parse(tarih)).ToString("yyyy-MM-dd"), ["baslangicSaat"] = bas, ["bitisSaat"] = bit, ["mola"] = mola, ["karsilik"] = kars, ["proje"] = proje, ["gerekce"] = gerekce, ["servis"] = "true" };
            var a1 = TalepMotoru.Tohum("MESAI", "Ali Veli", "Ali Veli", "HAFTAICI", D("-9", "18:00", "21:00", "Ücret", "Sunucu yedekleme geçişi", "Yeni NAS'a taşıma mesai saatleri dışında yapılmalı."), 12);
            TalepMotoru.TohumOlay(a1, "PdksGiris", new Dictionary<string, string> { ["giris"] = "08:12", ["cikis"] = "21:08" }, 9, 21); TalepMotoru.TohumOlay(a1, "PuantajAktarildi", new Dictionary<string, string> { ["donem"] = DateTime.Today.AddDays(-9).ToString("yyyy-MM") }, 3, 17);
            var a2 = TalepMotoru.Tohum("MESAI", "Ali Veli", "Ali Veli", "HAFTASONU", D("-1", "09:00", "15:00", "Serbest Zaman (İzin)", "Ay sonu kapanış desteği", "Muhasebe ay sonu kapanışı için sistem desteği.", "60"), 6);
            TalepMotoru.TohumOlay(a2, "PdksGiris", new Dictionary<string, string> { ["giris"] = "09:04", ["cikis"] = "14:10" }, 1, 15);
            TalepMotoru.Tohum("MESAI", "Ali Veli", "Ali Veli", "HAFTAICI", D("2", "18:00", "21:00", "Ücret", "ERP güncellemesi", "Yıl sonu yaması; kullanıcılar çıktıktan sonra uygulanacak."), 1);
            var a4 = TalepMotoru.Tohum("MESAI", "Mert Doğan", "Mert Doğan", "HAFTAICI", D("1", "17:30", "22:30", "Ücret", "Hat 2 acil bakım", "Kompresör arızası; vardiya sonrası onarım."), 1);
            TalepMotoru.Tohum("MESAI", "Mert Doğan", "Esra Kaya", "TATIL", new Dictionary<string, string> { ["tarih"] = "2026-10-29", ["baslangicSaat"] = "08:00", ["bitisSaat"] = "16:00", ["mola"] = "60", ["karsilik"] = "Ücret", ["proje"] = "Bayram nöbeti", ["gerekce"] = "Cumhuriyet Bayramı teknik servis nöbeti.", ["yemek"] = "true" }, 2, 1);
            var a6 = TalepMotoru.Tohum("MESAI", "Tolga Yaman", "Gizem Tan", "HAFTAICI", D("-16", "18:00", "20:00", "Ücret", "Fuar hazırlığı", "Katalog ve numune paketleme."), 18);
            TalepMotoru.TohumOlay(a6, "PdksGiris", new Dictionary<string, string> { ["giris"] = "08:30", ["cikis"] = "18:20" }, 16, 20);
            var a7 = TalepMotoru.Tohum("MESAI", "Ali Veli", "Kerem Aksoy", "GECE", D("5", "20:00", "03:00", "Ücret", "Veri merkezi taşıma", "Gece elektrik kesintisi penceresi.", "30"), 1, 0);
            var a8 = TalepMotoru.Tohum("MESAI", "Ali Veli", "Ali Veli", "HAFTAICI", D("-30", "18:00", "23:30", "Ücret", "Felaket kurtarma tatbikatı", "DR tatbikatı."), 33, 0);
            TalepMotoru.TohumRed(a8, "5,5 saat günlük sınırı aşıyor; iki güne bölün.");
            var a9 = TalepMotoru.Tohum("MESAI", "Onur Bal", "Nazlı Erdem", "HAFTAICI", D("-2", "18:00", "20:30", "Serbest Zaman (İzin)", "KDV beyannamesi", "Beyanname son gün."), 3);
            var a10 = TalepMotoru.Tohum("MESAI", "Ali Veli", "Ali Veli", "HAFTAICI", D("8", "18:00", "20:00", "Ücret", "Yazıcı sunucusu", "Sürücü güncellemesi."), 0, 0); TalepMotoru.TohumRevizyon(a10, "Saat aralığını 18:00–19:30 yapın; 2 saat gerekmiyor.");
        }

        // =====================================================================
        // BELGE
        // =====================================================================
        private static TmModul Belge()
        {
            var m = new TmModul
            {
                Kod = "BELGE", Ad = "Belge Talebi", NoOnEk = "BL", Ikon = "fa-file-signature", Renk = "#f59e0b",
                Aciklama = "Çalışma belgesi, bordro, SGK dökümü, vize yazısı gibi İK belgeleri talep edilir. Hazırlık, imza ve teslimat aşamaları İK sisteminden izlenir; SLA hedefi otomatik hesaplanır.",
                AkisOzeti = "Talep → (Yönetici) → İK → Hazırlık → İmza → Teslim → Teyit",
                KullananEtiket = "Belge sahibi personel", Durum2Ad = "Hazırlık Durumu (İK)", Durum3Ad = "Teslimat Durumu",
                Turler = { T("CALISMA", "Çalışma Belgesi", "I", "Görev, ünvan, işe giriş tarihi; SLA 2 iş günü."), T("BORDRO", "Maaş Bordrosu", "I", "Seçilen dönem bordrosu; maaş bilgisi içerir (KVKK rızası).", "bordro"), T("SGK", "SGK Hizmet Dökümü", "I", "e-Devlet dökümü kaşeli; SLA 2 iş günü."), T("VIZE", "Vize / Konsolosluk Yazısı", "Y,I", "Kurum formatına uygun, maaş bilgisi isteğe bağlı; SLA 5 iş günü.", "uzun"), T("GOREV", "Görev Yazısı", "Y,I", "Seyahat / saha görevi için resmi yazı.", "uzun"), T("GIRIS", "İşe Giriş Bildirgesi", "I", "SGK işe giriş bildirgesi kopyası."), T("REFERANS", "Referans Mektubu", "Y,I", "Yönetici görüşü gerekir; SLA 5 iş günü.", "uzun"), T("YETKI", "İmza Sirküleri / Yetki Belgesi", "Y,D,I", "Noter onaylı; Direktör onayı gerekir; SLA 7 iş günü.", "cokuzun") },
                Alanlar = { A("dil", "Dil", "select", true, 3, new[] { "Türkçe", "İngilizce", "Türkçe + İngilizce" }, vars: "Türkçe"), A("adet", "Adet", "number", true, 2, vars: "1"), A("amac", "Kullanım Amacı", "select", true, 3, new[] { "Banka / Kredi", "Vize", "Okul / Burs", "Ev Kiralama", "Resmi Kurum", "Diğer" }), A("kurum", "İbraz Edilecek Kurum", "text", true, 4, ipucu: "Banka, konsolosluk, okul…"),
                            A("teslim", "Teslim Şekli", "select", true, 3, new[] { "E-posta (PDF)", "Islak İmzalı (İK'dan teslim)", "Kargo" }, vars: "E-posta (PDF)"), A("adres", "Kargo Adresi", "text", false, 5, ipucu: "kargo seçildiyse zorunlu"), A("sonTarih", "En Geç Teslim", "date", false, 2), A("donem", "Bordro Dönemi", "text", false, 2, ipucu: "AAAA-AA"),
                            A("maasBilgisi", "Maaş bilgisi belgede yer alsın", "checkbox", false, 4), A("kvkk", "KVKK açık rıza: kişisel / mali verilerimin belgede yer almasını onaylıyorum", "checkbox", false, 8), A("ekBilgi", "Ek Bilgi", "textarea", false, 12, ipucu: "Kurumun istediği özel ifade, form numarası vb.") },
                Olaylar = { O("HazirlikBasladi", "HazirlikBasladi — İK belgeyi hazırlamaya başladı", "İK", "Hazırlayan uzman atanır.", "hazirlayan|Hazırlayan|text"), O("ImzayaGonderildi", "ImzayaGonderildi — İmza / e-imza", "İK", "Yetkili imzasına sunuldu.", "imzaTuru|İmza Türü|select:e-İmza;Islak İmza;Noter", "imzalayan|İmzalayan|text"), O("BelgeHazir", "BelgeHazir — Belge numarası verildi", "İK", "Belge arşive kaydedildi, PDF üretildi.", "belgeNo|Belge No|text"), O("Teslim", "Teslim — Teslimat kanalı", "İK", "E-posta gönderimi, kargo ya da elden teslim.", "kanal|Kanal|select:E-posta;Kargo;Elden", "takip|Kargo Takip No / E-posta|text"), O("TeslimTeyidi", "TeslimTeyidi — Personel teslim aldı", "Portal", "Belge sahibi dijital teyit verir.") },
                HesapEtiketler = { ["slaGun"] = "SLA (iş günü)", ["hedefTeslim"] = "Hedef Teslim Tarihi", ["kvkkDurum"] = "KVKK Rızası" },
                EkEtiketler = { ["hazirlayan"] = "Hazırlayan", ["hazirlikTarihi"] = "Hazırlık Başlangıcı", ["imzaTuru"] = "İmza Türü", ["imzalayan"] = "İmzalayan", ["belgeNo"] = "Belge No", ["belgeTarihi"] = "Belge Tarihi", ["kanal"] = "Teslim Kanalı", ["takip"] = "Takip No / Adres", ["teslimTarihi"] = "Teslim Tarihi", ["teyitTarihi"] = "Teyit Tarihi" },
                Parametreler = { ["slaStandart"] = "2", ["slaUzun"] = "5", ["slaCokUzun"] = "7", ["adetUst"] = "5" },
                ParametreEtiketler = { ["slaStandart"] = "Standart belge SLA (iş günü)", ["slaUzun"] = "Yazı / mektup SLA (iş günü)", ["slaCokUzun"] = "Noter onaylı belge SLA (iş günü)", ["adetUst"] = "Uyarı verilecek adet" },
                Kurallar = { "Maaş bilgisi içeren belgelerde KVKK açık rıza onayı zorunludur.", "Kargo tesliminde adres zorunludur; kargo takip no İK sisteminden gelir.", "SLA hedefi talep tarihine iş günü eklenerek hesaplanır; 'en geç' tarihi SLA'dan erkense uyarı verilir.", "Vize, görev, referans yazıları yönetici; noter onaylı yetki belgesi Direktör onayı gerektirir.", "Aynı belge türünde 30 gün içinde açık talep varsa uyarı verilir." },
                TakipSutunlar = { ("Talep No", "no"), ("Personel", "kullanan"), ("Departman", "departman"), ("Belge", "tur"), ("Dil", "d.dil"), ("Adet", "d.adet"), ("Kurum", "d.kurum"), ("Amaç", "d.amac"), ("Teslim Şekli", "d.teslim"), ("En Geç", "d.sonTarih"), ("Hedef (SLA)", "h.hedefTeslim"), ("Talep Durumu", "talepDurumu"), ("Hazırlık", "durum2"), ("Teslimat", "durum3"), ("Belge No", "e.belgeNo"), ("Kanal / Takip", "e.takip"), ("İK", "onay.İK Onayı") }
            };
            m.Hesapla = a =>
            {
                var s = new TmSonuc(); var m2 = a.Modul;
                decimal sla = a.Tur.Ek == "uzun" ? m2.P("slaUzun") : a.Tur.Ek == "cokuzun" ? m2.P("slaCokUzun") : m2.P("slaStandart");
                var hedef = (a.Tarih == default ? DateTime.Today : a.Tarih.Date); for (int i = 0; i < sla; ) { hedef = hedef.AddDays(1); if (IzinOrnek.IsGunuMu(hedef)) i++; }
                a.Hesap["slaGun"] = S(sla); a.Hesap["hedefTeslim"] = hedef.ToString("dd.MM.yyyy", Tr); a.Hesap["hedefIso"] = hedef.ToString("yyyy-MM-dd");
                bool maas = a.V("maasBilgisi") == "true" || a.TurKod == "BORDRO"; a.Hesap["kvkkDurum"] = maas ? (a.V("kvkk") == "true" ? "Alındı" : "Gerekli") : "Gerekmiyor";
                if (maas && a.V("kvkk") != "true") { s.Hata = "Maaş bilgisi içeren belge için KVKK açık rıza onayı zorunludur."; return s; }
                if (a.V("teslim") == "Kargo" && a.V("adres") == "") { s.Hata = "Kargo tesliminde adres zorunludur."; return s; }
                if (a.TurKod == "BORDRO" && a.V("donem") == "") { s.Hata = "Bordro talebinde dönem (AAAA-AA) zorunludur."; return s; }
                if (a.D("adet") <= 0) { s.Hata = "Adet en az 1 olmalı."; return s; }
                if (a.D("adet") > m2.P("adetUst")) s.Uyarilar.Add($"{S(a.D("adet"))} adet: {S(m2.P("adetUst"))} üzeri taleplerde İK gerekçe isteyebilir.");
                var son = a.T("sonTarih"); if (son != null && son.Value.Date < hedef) s.Uyarilar.Add($"İstenen teslim ({son:dd.MM.yyyy}) SLA hedefinden ({hedef:dd.MM.yyyy}) erken; acil işaretlenir.");
                if (son != null && son.Value.Date < DateTime.Today) { s.Hata = "En geç teslim tarihi geçmişte olamaz."; return s; }
                if (Liste("BELGE").Any(t => t.Id != a.Id && !t.Iptal && t.Kullanan.PersonelNo == a.Kullanan.PersonelNo && t.TurKod == a.TurKod && t.Durum3 != "Teslim Alındı" && (DateTime.Today - t.Tarih.Date).Days <= 30 && t.TalepDurumu != "Reddedildi")) s.Uyarilar.Add("Aynı belge türünde son 30 günde açık talep var.");
                if (a.TurKod == "VIZE" && a.V("dil") == "Türkçe") s.Uyarilar.Add("Konsolosluk yazıları genellikle İngilizce istenir.");
                return s;
            };
            m.Durum2 = a => a.Ek.ContainsKey("teslimTarihi") ? "Teslim Edildi" : a.Ek.ContainsKey("belgeNo") ? "Hazır" : a.Ek.ContainsKey("imzaTuru") ? "İmza Bekliyor" : a.Ek.ContainsKey("hazirlayan") ? "Hazırlanıyor" : "Sırada";
            m.Durum3 = a => a.Ek.ContainsKey("teyitTarihi") ? "Teslim Alındı" : a.Ek.ContainsKey("teslimTarihi") ? (a.E("kanal") == "E-posta" ? "E-posta Gönderildi" : a.E("kanal") == "Kargo" ? "Kargoda" : "Elden Teslim Edildi") : a.Ek.ContainsKey("belgeNo") ? "Teslim Bekliyor" : "—";
            m.Olay = (a, kod, g) =>
            {
                switch (kod)
                {
                    case "HazirlikBasladi": { if (a.Ek.ContainsKey("hazirlayan")) return "Hazırlık zaten başlamış."; a.Ek["hazirlayan"] = G(g, "hazirlayan") == "" ? "Derya Şen" : G(g, "hazirlayan"); a.Ek["hazirlikTarihi"] = Simdi.ToString("dd.MM.yyyy HH:mm", Tr); H(a, $"İK: belge hazırlanmaya başlandı ({a.E("hazirlayan")})", "İK"); L(a, "PREP_STATUS", "Sırada", "Hazırlanıyor", "İK"); return null; }
                    case "ImzayaGonderildi": { if (!a.Ek.ContainsKey("hazirlayan")) return "Önce hazırlık başlamalı."; a.Ek["imzaTuru"] = G(g, "imzaTuru") == "" ? "e-İmza" : G(g, "imzaTuru"); a.Ek["imzalayan"] = G(g, "imzalayan") == "" ? "Sevgi Ay" : G(g, "imzalayan"); H(a, $"İK: {a.E("imzaTuru")} için {a.E("imzalayan")} onayına sunuldu", "İK"); L(a, "PREP_STATUS", "Hazırlanıyor", "İmza Bekliyor", "İK"); return null; }
                    case "BelgeHazir": { if (!a.Ek.ContainsKey("imzaTuru")) return "İmza aşaması tamamlanmadan belge hazır yapılamaz."; a.Ek["belgeNo"] = G(g, "belgeNo") == "" ? $"İK-{Simdi:yyyy}-{a.Id + 4100}" : G(g, "belgeNo"); a.Ek["belgeTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); a.Ekler.Add(new NaEk { Ad = a.E("belgeNo") + ".pdf", Tur = "Belge", Boyut = "184 KB", Tarih = Simdi, Yukleyen = a.E("hazirlayan") }); H(a, $"İK: belge hazır ({a.E("belgeNo")}), PDF arşive kaydedildi", "İK"); L(a, "DOCUMENT_NO", "", a.E("belgeNo"), "İK"); return null; }
                    case "Teslim": { if (!a.Ek.ContainsKey("belgeNo")) return "Belge hazır olmadan teslim edilemez."; a.Ek["kanal"] = G(g, "kanal") == "" ? (a.V("teslim").StartsWith("E-posta") ? "E-posta" : a.V("teslim") == "Kargo" ? "Kargo" : "Elden") : G(g, "kanal"); a.Ek["takip"] = G(g, "takip") == "" ? (a.E("kanal") == "Kargo" ? "YK" + (100200300 + a.Id * 17) : a.E("kanal") == "E-posta" ? a.Kullanan.KullaniciId + "@urasholding.com" : "İK ofisi") : G(g, "takip"); a.Ek["teslimTarihi"] = Simdi.ToString("dd.MM.yyyy HH:mm", Tr); H(a, $"Teslim: {a.E("kanal")} ({a.E("takip")})", "İK"); L(a, "DELIVERY_STATUS", "Bekliyor", a.E("kanal"), "İK"); return null; }
                    case "TeslimTeyidi": { if (!a.Ek.ContainsKey("teslimTarihi")) return "Henüz teslim edilmedi."; if (a.Ek.ContainsKey("teyitTarihi")) return "Zaten teyit edilmiş."; a.Ek["teyitTarihi"] = Simdi.ToString("dd.MM.yyyy HH:mm", Tr); H(a, "Teslim teyidi: \"Belgeyi teslim aldım.\"", a.Kullanan.AdSoyad); L(a, "DELIVERY_CONFIRMED", "false", "true", "Portal"); return null; }
                }
                return "Tanımsız olay.";
            };
            m.Ozet = a => $"{a.Tur.Ad} ({a.V("dil")}) · {a.V("adet")} adet · {a.V("kurum")} · {a.V("teslim")}";
            m.Etiketle = a => { var l = new List<string>(); bool acik = a.Onaylandi && !a.Iptal && a.Durum3 != "Teslim Alındı"; string d2 = a.Durum2; if (acik && d2 == "Sırada") l.Add("sirada"); if (acik && d2 == "Hazırlanıyor") l.Add("hazir"); if (acik && d2 == "İmza Bekliyor") l.Add("imza"); if (acik && !a.Ek.ContainsKey("teslimTarihi") && a.Hesap.TryGetValue("hedefIso", out var h) && string.CompareOrdinal(h, DateTime.Today.ToString("yyyy-MM-dd")) < 0) l.Add("sla"); if (acik && a.Hesap.TryGetValue("hedefIso", out var h2) && h2 == DateTime.Today.ToString("yyyy-MM-dd")) l.Add("bugun"); if (a.Durum3 != "—" && a.Durum3 != "Teslim Bekliyor" && a.Durum3 != "Teslim Alındı") l.Add("teyit"); if (a.TalepDurumu.EndsWith("Bekliyor") && !a.TalepDurumu.StartsWith("Revizyon")) l.Add("onay"); return l; };
            m.Kpiler = l => new List<TmKpi> { K("#f59e0b", "Sırada", $"{l.Count(t => t.Etiketler.Contains("sirada"))} Belge", Adlar(l.Where(t => t.Etiketler.Contains("sirada"))), "sirada"), K("#0ea5e9", "Hazırlanıyor", $"{l.Count(t => t.Etiketler.Contains("hazir"))} Belge", Adlar(l.Where(t => t.Etiketler.Contains("hazir"))), "hazir"), K("#7c3aed", "İmza Bekleyen", $"{l.Count(t => t.Etiketler.Contains("imza"))} Belge", Adlar(l.Where(t => t.Etiketler.Contains("imza"))), "imza"), K("#dc2626", "SLA'sı Geçen", $"{l.Count(t => t.Etiketler.Contains("sla"))} Belge", Adlar(l.Where(t => t.Etiketler.Contains("sla"))), "sla"), K("#16a34a", "Bugün Teslim Hedefi", $"{l.Count(t => t.Etiketler.Contains("bugun"))} Belge", Adlar(l.Where(t => t.Etiketler.Contains("bugun"))), "bugun"), K("#0891b2", "Teyit Bekleyen", $"{l.Count(t => t.Etiketler.Contains("teyit"))} Belge", "teslim edildi, personel teyidi yok", "teyit") };
            return m;
        }
        private static void BelgeTohum()
        {
            Dictionary<string, string> D(string dil, string adet, string amac, string kurum, string teslim, string son = null, string donem = null, bool maas = false, string adres = null) { var d = new Dictionary<string, string> { ["dil"] = dil, ["adet"] = adet, ["amac"] = amac, ["kurum"] = kurum, ["teslim"] = teslim }; if (son != null) d["sonTarih"] = DateTime.Today.AddDays(int.Parse(son)).ToString("yyyy-MM-dd"); if (donem != null) d["donem"] = donem; if (maas) { d["maasBilgisi"] = "true"; d["kvkk"] = "true"; } if (adres != null) d["adres"] = adres; return d; }
            var b1 = TalepMotoru.Tohum("BELGE", "Ali Veli", "Ali Veli", "CALISMA", D("İngilizce", "1", "Vize", "Almanya Konsolosluğu", "Islak İmzalı (İK'dan teslim)", "7"), 9);
            TalepMotoru.TohumOlay(b1, "HazirlikBasladi", null, 8); TalepMotoru.TohumOlay(b1, "ImzayaGonderildi", new Dictionary<string, string> { ["imzaTuru"] = "Islak İmza" }, 8, 14); TalepMotoru.TohumOlay(b1, "BelgeHazir", null, 7); TalepMotoru.TohumOlay(b1, "Teslim", new Dictionary<string, string> { ["kanal"] = "Elden" }, 6); TalepMotoru.TohumOlay(b1, "TeslimTeyidi", null, 6, 15);
            var b2 = TalepMotoru.Tohum("BELGE", "Ali Veli", "Ali Veli", "BORDRO", D("Türkçe", "3", "Banka / Kredi", "Ziraat Bankası", "E-posta (PDF)", null, DateTime.Today.AddMonths(-1).ToString("yyyy-MM"), true), 2);
            TalepMotoru.TohumOlay(b2, "HazirlikBasladi", null, 1);
            var b3 = TalepMotoru.Tohum("BELGE", "Tolga Yaman", "Tolga Yaman", "VIZE", D("İngilizce", "1", "Vize", "İtalya Konsolosluğu", "Kargo", "10", null, true, "Selvi Fabrika, Gebze OSB"), 4);
            TalepMotoru.TohumOlay(b3, "HazirlikBasladi", null, 3); TalepMotoru.TohumOlay(b3, "ImzayaGonderildi", new Dictionary<string, string> { ["imzaTuru"] = "e-İmza", ["imzalayan"] = "Neslihan Uludağ" }, 2);
            var b4 = TalepMotoru.Tohum("BELGE", "Gizem Tan", "Gizem Tan", "SGK", D("Türkçe", "1", "Ev Kiralama", "Emlak ofisi", "E-posta (PDF)"), 6);
            TalepMotoru.TohumOlay(b4, "HazirlikBasladi", null, 5); TalepMotoru.TohumOlay(b4, "ImzayaGonderildi", null, 5, 15); TalepMotoru.TohumOlay(b4, "BelgeHazir", null, 4); TalepMotoru.TohumOlay(b4, "Teslim", new Dictionary<string, string> { ["kanal"] = "E-posta" }, 4, 16);
            TalepMotoru.Tohum("BELGE", "Esra Kaya", "Esra Kaya", "REFERANS", D("İngilizce", "1", "Okul / Burs", "Yüksek lisans başvurusu", "E-posta (PDF)", "20"), 1, 0);
            TalepMotoru.Tohum("BELGE", "Mert Doğan", "Mert Doğan", "GOREV", D("Türkçe", "2", "Resmi Kurum", "OSB Müdürlüğü", "Islak İmzalı (İK'dan teslim)", "5"), 1, 1);
            var b7 = TalepMotoru.Tohum("BELGE", "Ali Veli", "Ali Veli", "CALISMA", D("Türkçe", "1", "Diğer", "Spor salonu kurumsal üyelik", "E-posta (PDF)"), 12);
            var b8 = TalepMotoru.Tohum("BELGE", "Canan Su", "Canan Su", "YETKI", D("Türkçe", "2", "Resmi Kurum", "Noter / Vergi dairesi", "Islak İmzalı (İK'dan teslim)", "12"), 3, 1);
            var b9 = TalepMotoru.Tohum("BELGE", "Hülya Er", "Hülya Er", "GIRIS", D("Türkçe", "1", "Banka / Kredi", "Garanti BBVA", "E-posta (PDF)"), 15, 0); TalepMotoru.TohumRed(b9, "Bildirge e-Devlet'ten alınabilir; İK kopyası gerekmiyor.");
        }

        // =====================================================================
        // SEYAHAT
        // =====================================================================
        private static TmModul Seyahat()
        {
            var m = new TmModul
            {
                Kod = "SEYAHAT", Ad = "Seyahat Talebi", NoOnEk = "SY", Ikon = "fa-plane-departure", Renk = "#0891b2",
                Aciklama = "Yurt içi / yurt dışı iş seyahatleri için ulaşım, konaklama ve harcırah talebi. Bilet ve otel İdari İşler tarafından alınır; avans ve masraf kapanışı Finans'tan gelir.",
                AkisOzeti = "Talep → Yönetici → (Direktör) → (İK) → Rezervasyon → Seyahat → Masraf Formu → Kapanış",
                KullananEtiket = "Seyahat edecek personel", Durum2Ad = "Organizasyon Durumu (İdari İşler)", Durum3Ad = "Masraf / Kapatma (Finans)",
                TakvimSatir = "kisi", TakvimBas = "gidis", TakvimBit = "donus", TakvimAd = "Seyahat Takvimi",
                Turler = { T("YURTICI", "Yurt İçi Seyahat", "Y", "Günlük harcırah parametrik; en az 5 gün önce."), T("YURTDISI", "Yurt Dışı Seyahat", "Y,D", "Direktör onayı; pasaport en az 6 ay geçerli; vize süreci.", "dis"), T("MUSTERI", "Müşteri Ziyareti", "Y", "Ziyaret raporu dönüşte istenir."), T("FUAR", "Fuar / Kongre", "Y,D", "Katılım ücreti ve stant giderleri bütçeye dahil.", "dis"), T("EGITIM", "Eğitim / Sertifika", "Y,I", "İK eğitim bütçesinden; sertifika dönüşte İK'ya iletilir."), T("SAHA", "Saha / Servis Ziyareti", "Y", "Teknik servis; araç talebiyle birlikte açılabilir.") },
                Alanlar = { A("gidis", "Gidiş Tarihi", "date", true, 3), A("donus", "Dönüş Tarihi", "date", true, 3), A("nereden", "Nereden", "text", true, 3, vars: "İstanbul"), A("nereye", "Nereye (şehir / ülke)", "text", true, 3),
                            A("ulasim", "Ulaşım", "select", true, 3, new[] { "Uçak", "Şirket Aracı", "Kiralık Araç", "Otobüs / Tren", "Kendi Aracım (km ödemeli)" }, vars: "Uçak"), A("konaklama", "Konaklama gerekli", "checkbox", false, 2), A("otel", "Otel Tercihi", "text", false, 4, ipucu: "anlaşmalı otel listesi öncelikli"), A("sirket", "Masraf Şirketi", "select", true, 3, Sirketler, vars: "Uras Kimya"),
                            A("tahminiUcak", "Tahmini Ulaşım (TL)", "number", false, 3, vars: "0"), A("tahminiOtel", "Tahmini Konaklama (TL)", "number", false, 3, vars: "0"), A("diger", "Diğer Giderler (TL)", "number", false, 3, vars: "0"), A("harcirah", "Harcırah / avans istiyorum", "checkbox", false, 3),
                            A("kisiler", "Birlikte Seyahat Edenler", "multiselect", false, 6), A("vize", "Vize gerekli", "checkbox", false, 3), A("pasaport", "Pasaportum dönüşten itibaren 6 ay geçerli", "checkbox", false, 3),
                            A("amac", "Seyahat Amacı / Ziyaret Edilecek Firma", "textarea", true, 12) },
                Olaylar = { O("BiletAlindi", "BiletAlindi — Ulaşım rezervasyonu", "İdari İşler", "PNR ve tutar acente sisteminden gelir.", "pnr|PNR / Bilet No|text", "tutar|Tutar (TL)|number"), O("OtelOnaylandi", "OtelOnaylandi — Konaklama rezervasyonu", "İdari İşler", "Otel ve tutar acente sisteminden gelir.", "otel|Otel|text", "tutar|Tutar (TL)|number"), O("AvansOdendi", "AvansOdendi — Nakit avans teslimi", "Finans/Kasa", "Nakit Avans modülünden Talep No referansıyla gelir.", "avansNo|Avans Talep No|text", "tutar|Tutar (TL)|number"), O("MasrafFormuGonderildi", "MasrafFormuGonderildi — Dönüş sonrası masraf formu", "Portal", "Gerçekleşen toplam ve belgeler.", "tutar|Gerçekleşen Toplam (TL)|number"), O("MasrafOnaylandi", "MasrafOnaylandi — Finans masraf onayı ve kapanış", "Finans", "Fark avans mahsubuna gider; seyahat kapanır.") },
                HesapEtiketler = { ["gun"] = "Gün Sayısı", ["gece"] = "Gece Sayısı", ["harcirahGunluk"] = "Günlük Harcırah", ["harcirahToplam"] = "Harcırah Toplamı (TL)", ["butce"] = "Tahmini Bütçe (TL)" },
                EkEtiketler = { ["pnr"] = "PNR / Bilet", ["biletTutar"] = "Bilet Tutarı (TL)", ["otelAd"] = "Otel", ["otelTutar"] = "Otel Tutarı (TL)", ["avansNo"] = "Avans Talep No", ["avansTutar"] = "Avans (TL)", ["gerceklesen"] = "Gerçekleşen Toplam (TL)", ["masrafTarihi"] = "Masraf Formu Tarihi", ["kapanisTarihi"] = "Kapanış Tarihi", ["fark"] = "Bütçe Farkı (TL)" },
                Parametreler = { ["harcirahIc"] = "1500", ["harcirahDis"] = "120", ["eurKur"] = "47.9", ["direktorButce"] = "50000", ["bildirimIc"] = "5", ["bildirimDis"] = "10", ["otelTavan"] = "4000" },
                ParametreEtiketler = { ["harcirahIc"] = "Yurt içi günlük harcırah (TL)", ["harcirahDis"] = "Yurt dışı günlük harcırah (EUR)", ["eurKur"] = "EUR kuru", ["direktorButce"] = "Bu bütçeyi aşan seyahatte Direktör onayı (TL)", ["bildirimIc"] = "Yurt içi en az bildirim (gün)", ["bildirimDis"] = "Yurt dışı en az bildirim (gün)", ["otelTavan"] = "Gecelik otel tavanı (TL)" },
                Kurallar = { "Harcırah gün sayısı × parametre ile hesaplanır; yurt dışında EUR bazlıdır.", "Tahmini bütçe Direktör eşiğini aşarsa onay adımı eklenir.", "Yurt dışı seyahatte pasaport geçerliliği ve vize süreci kontrol edilir.", "Seyahat tarihleri onaylı izin ya da başka seyahatle çakışamaz.", "Dönüş sonrası masraf formu Finans onayından geçmeden seyahat kapanmaz; fark avans mahsubuna gider." },
                TakipSutunlar = { ("Talep No", "no"), ("Personel", "kullanan"), ("Departman", "departman"), ("Tür", "tur"), ("Güzergâh", "h.guzergah"), ("Gidiş", "d.gidis"), ("Dönüş", "d.donus"), ("Gün", "h.gun"), ("Ulaşım", "d.ulasim"), ("Konaklama", "d.konaklama"), ("Harcırah (TL)", "h.harcirahToplam"), ("Bütçe (TL)", "h.butce"), ("Gerçekleşen (TL)", "e.gerceklesen"), ("Talep Durumu", "talepDurumu"), ("Organizasyon", "durum2"), ("Masraf", "durum3"), ("PNR", "e.pnr"), ("Masraf Şirketi", "d.sirket"), ("Direktör", "onay.Direktör Onayı") }
            };
            m.Hesapla = a =>
            {
                var s = new TmSonuc(); var m2 = a.Modul;
                var g = a.T("gidis"); var d = a.T("donus"); if (g == null || d == null) { s.Hata = "Gidiş ve dönüş tarihi zorunludur."; return s; }
                if (d.Value.Date < g.Value.Date) { s.Hata = "Dönüş tarihi gidişten önce olamaz."; return s; }
                int gun = (d.Value.Date - g.Value.Date).Days + 1, gece = Math.Max(0, gun - 1); bool dis = a.Tur.Ek == "dis";
                decimal gunluk = dis ? m2.P("harcirahDis") * m2.P("eurKur") : m2.P("harcirahIc"); decimal harc = a.V("harcirah") == "true" ? gun * gunluk : 0;
                decimal otel = a.D("tahminiOtel"); if (a.V("konaklama") == "true" && otel == 0) otel = gece * m2.P("otelTavan");
                decimal butce = a.D("tahminiUcak") + otel + harc + a.D("diger");
                a.Hesap["gun"] = gun.ToString(); a.Hesap["gece"] = a.V("konaklama") == "true" ? gece.ToString() : "0"; a.Hesap["harcirahGunluk"] = dis ? $"{S(m2.P("harcirahDis"))} EUR (≈{N(gunluk)} TL)" : $"{N(gunluk)} TL"; a.Hesap["harcirahToplam"] = N(harc); a.Hesap["butce"] = N(butce); a.Hesap["butceSayi"] = butce.ToString("0", CultureInfo.InvariantCulture); a.Hesap["guzergah"] = a.V("nereden") + " → " + a.V("nereye");
                int bildirim = (int)(dis ? m2.P("bildirimDis") : m2.P("bildirimIc")); if ((g.Value.Date - DateTime.Today).Days < bildirim) s.Uyarilar.Add($"{a.Tur.Ad} en az {bildirim} gün önce talep edilmelidir (kalan {(g.Value.Date - DateTime.Today).Days} gün).");
                if (butce > m2.P("direktorButce")) { s.EkAdimlar.Add("D"); s.Uyarilar.Add($"Tahmini bütçe {N(butce)} TL, {N(m2.P("direktorButce"))} TL eşiğini aşıyor: Direktör onayı eklendi."); }
                if (dis && a.V("pasaport") != "true") s.Uyarilar.Add("Yurt dışı seyahat: pasaportun dönüşten itibaren 6 ay geçerli olduğu teyit edilmedi.");
                if (a.V("vize") == "true") { if ((g.Value.Date - DateTime.Today).Days < 15) s.Uyarilar.Add("Vize süreci en az 15 gün sürer; tarih riskli."); }
                if (a.V("konaklama") == "true" && gece > 0 && a.D("tahminiOtel") > gece * m2.P("otelTavan")) s.Uyarilar.Add($"Konaklama tahmini gecelik tavanı ({N(m2.P("otelTavan"))} TL) aşıyor.");
                if (IzinOrnek.Cakisanlar(a.Kullanan, g.Value, d.Value).Any(x => x.Onaylandi)) { s.Hata = "Personel bu tarihlerde onaylı izinli; seyahat planlanamaz."; return s; }
                if (Liste("SEYAHAT").Any(t => t.Id != a.Id && !t.Iptal && t.TalepDurumu != "Reddedildi" && t.Kullanan.PersonelNo == a.Kullanan.PersonelNo && t.T("gidis") <= d && t.T("donus") >= g)) { s.Hata = "Aynı tarihlerde bu personelin başka seyahati var."; return s; }
                foreach (var pn in a.V("kisiler").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) { var k = TalepMotoru.KisiBul(pn); if (k != null && IzinOrnek.Cakisanlar(k, g.Value, d.Value).Any(x => x.Onaylandi)) s.Uyarilar.Add($"{k.AdSoyad} bu tarihlerde izinli görünüyor."); }
                if (a.V("ulasim").StartsWith("Kendi")) s.Uyarilar.Add("Kendi aracıyla seyahat: km ödemesi masraf formunda hesaplanır (ruhsat ve sigorta kontrolü).");
                return s;
            };
            m.Durum2 = a =>
            {
                var g = a.T("gidis")?.Date ?? DateTime.Today; var d = a.T("donus")?.Date ?? g; bool bilet = a.Ek.ContainsKey("pnr") || !a.V("ulasim").Contains("Uçak") && !a.V("ulasim").Contains("Otobüs"); bool otel = a.V("konaklama") != "true" || a.Ek.ContainsKey("otelAd");
                if (DateTime.Today > d) return "Tamamlandı"; if (DateTime.Today >= g) return "Seyahatte";
                return bilet && otel ? "Hazır" : !bilet ? "Rezervasyon Bekliyor" : "Otel Bekleniyor";
            };
            m.Durum3 = a => a.Ek.ContainsKey("kapanisTarihi") ? "Kapandı" : a.Ek.ContainsKey("masrafTarihi") ? "Masraf Formu Onayda" : (DateTime.Today > (a.T("donus")?.Date ?? DateTime.Today)) ? "Masraf Formu Bekleniyor" : a.V("harcirah") == "true" && !a.Ek.ContainsKey("avansNo") ? "Avans Bekliyor" : a.Ek.ContainsKey("avansNo") ? "Avans Ödendi" : "—";
            m.Olay = (a, kod, g) =>
            {
                switch (kod)
                {
                    case "BiletAlindi": { a.Ek["pnr"] = G(g, "pnr") == "" ? "PNR" + (Simdi.Ticks % 100000).ToString("00000") : G(g, "pnr"); a.Ek["biletTutar"] = N(GD(g, "tutar")); H(a, $"İdari İşler: ulaşım rezervasyonu yapıldı ({a.E("pnr")}, {a.E("biletTutar")} TL)", "İdari İşler"); L(a, "TICKET", "", a.E("pnr"), "Acente"); return null; }
                    case "OtelOnaylandi": { if (a.V("konaklama") != "true") return "Bu talepte konaklama istenmemiş."; a.Ek["otelAd"] = G(g, "otel") == "" ? (a.V("otel") == "" ? "Anlaşmalı otel" : a.V("otel")) : G(g, "otel"); a.Ek["otelTutar"] = N(GD(g, "tutar")); H(a, $"İdari İşler: konaklama onaylandı ({a.E("otelAd")}, {a.E("otelTutar")} TL)", "İdari İşler"); L(a, "HOTEL", "", a.E("otelAd"), "Acente"); return null; }
                    case "AvansOdendi": { if (a.V("harcirah") != "true") return "Bu talepte harcırah / avans istenmemiş."; if (a.Ek.ContainsKey("avansNo")) return "Avans zaten ödenmiş."; a.Ek["avansNo"] = G(g, "avansNo") == "" ? $"NA-{Simdi:yyyy}-{(a.Id + 160):000000}" : G(g, "avansNo"); decimal tutar = GD(g, "tutar"); if (tutar <= 0) tutar = decimal.Parse(a.Hesap["harcirahToplam"], NumberStyles.Any, Tr); a.Ek["avansTutar"] = N(tutar); H(a, $"Finans/Kasa: {N(tutar)} TL seyahat avansı teslim edildi ({a.E("avansNo")})", "Finans/Kasa"); L(a, "ADVANCE", "", a.E("avansNo"), "Finans/Kasa"); return null; }
                    case "MasrafFormuGonderildi": { if (DateTime.Today <= (a.T("donus")?.Date ?? DateTime.Today) && OlayZamani == null) return "Masraf formu dönüş tarihinden sonra gönderilir."; decimal t = GD(g, "tutar"); if (t <= 0) return "Gerçekleşen toplam sıfırdan büyük olmalı."; a.Ek["gerceklesen"] = N(t); a.Ek["masrafTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); decimal b = decimal.Parse(a.Hesap["butceSayi"], CultureInfo.InvariantCulture); a.Ek["fark"] = N(t - b) + (t > b ? " (aşım)" : ""); H(a, $"Masraf formu gönderildi: gerçekleşen {N(t)} TL (bütçe {N(b)} TL)", a.Kullanan.AdSoyad); L(a, "EXPENSE_TOTAL", "", N(t), "Portal"); return null; }
                    case "MasrafOnaylandi": { if (!a.Ek.ContainsKey("masrafTarihi")) return "Önce masraf formu gönderilmeli."; if (a.Ek.ContainsKey("kapanisTarihi")) return "Zaten kapanmış."; a.Ek["kapanisTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, "Finans: masraf formu onaylandı; " + (a.Ek.ContainsKey("avansNo") ? $"avans {a.E("avansNo")} mahsup edildi; " : "") + "seyahat kapatıldı", "Finans"); L(a, "SETTLEMENT_STATUS", "Onayda", "Kapandı", "Finans"); return null; }
                }
                return "Tanımsız olay.";
            };
            m.Ozet = a => $"{a.V("nereden")} → {a.V("nereye")} · {a.T("gidis"):dd.MM.yyyy} – {a.T("donus"):dd.MM.yyyy} ({a.Hesap["gun"]} gün) · bütçe {a.Hesap["butce"]} TL";
            m.Etiketle = a => { var l = new List<string>(); var g = a.T("gidis")?.Date; var d = a.T("donus")?.Date; bool ok = a.Onaylandi && !a.Iptal; if (ok && a.Durum2 == "Seyahatte") l.Add("seyahatte"); if (ok && g > DateTime.Today && g <= DateTime.Today.AddDays(7)) l.Add("hafta"); if (a.TalepDurumu.EndsWith("Bekliyor") && !a.TalepDurumu.StartsWith("Revizyon")) l.Add("onay"); if (ok && (a.Durum2 == "Rezervasyon Bekliyor" || a.Durum2 == "Otel Bekleniyor")) l.Add("rezervasyon"); if (ok && a.Durum3 == "Masraf Formu Bekleniyor") l.Add("masraf"); if (ok && a.V("vize") == "true" && g > DateTime.Today) l.Add("vize"); return l; };
            m.Kpiler = l => new List<TmKpi> { K("#0891b2", "Bugün Seyahatte", $"{l.Count(t => t.Etiketler.Contains("seyahatte"))} Kişi", Adlar(l.Where(t => t.Etiketler.Contains("seyahatte"))), "seyahatte"), K("#7c3aed", "Bu Hafta Yola Çıkan", $"{l.Count(t => t.Etiketler.Contains("hafta"))} Seyahat", Adlar(l.Where(t => t.Etiketler.Contains("hafta"))), "hafta"), K("#f59e0b", "Onay Bekleyen", $"{l.Count(t => t.Etiketler.Contains("onay"))} Talep", $"{N(l.Where(t => t.Etiketler.Contains("onay")).Sum(t => decimal.Parse(t.Hesap["butceSayi"], CultureInfo.InvariantCulture)))} TL bütçe", "onay"), K("#dc2626", "Rezervasyon Bekleyen", $"{l.Count(t => t.Etiketler.Contains("rezervasyon"))} Seyahat", Adlar(l.Where(t => t.Etiketler.Contains("rezervasyon"))), "rezervasyon"), K("#16a34a", "Masraf Formu Bekleyen", $"{l.Count(t => t.Etiketler.Contains("masraf"))} Seyahat", Adlar(l.Where(t => t.Etiketler.Contains("masraf"))), "masraf"), K("#0ea5e9", "Vize Sürecinde", $"{l.Count(t => t.Etiketler.Contains("vize"))} Seyahat", Adlar(l.Where(t => t.Etiketler.Contains("vize"))), "vize") };
            return m;
        }
        private static void SeyahatTohum()
        {
            Dictionary<string, string> D(int gid, int don, string nereden, string nereye, string ulasim, bool konak, string ucak, string otel, string diger, bool harc, string amac, string sirket = "Uras Kimya", bool vize = false, bool pasaport = true, string kisiler = "") => new Dictionary<string, string> { ["gidis"] = DateTime.Today.AddDays(gid).ToString("yyyy-MM-dd"), ["donus"] = DateTime.Today.AddDays(don).ToString("yyyy-MM-dd"), ["nereden"] = nereden, ["nereye"] = nereye, ["ulasim"] = ulasim, ["konaklama"] = konak ? "true" : "false", ["tahminiUcak"] = ucak, ["tahminiOtel"] = otel, ["diger"] = diger, ["harcirah"] = harc ? "true" : "false", ["amac"] = amac, ["sirket"] = sirket, ["vize"] = vize ? "true" : "false", ["pasaport"] = pasaport ? "true" : "false", ["kisiler"] = kisiler };
            var s1 = TalepMotoru.Tohum("SEYAHAT", "Ali Veli", "Ali Veli", "YURTICI", D(-20, -18, "İstanbul", "Ankara", "Uçak", true, "6500", "7000", "1500", true, "Kamu ihalesi teknik sunum (DMO)"), 32);
            TalepMotoru.TohumOlay(s1, "BiletAlindi", new Dictionary<string, string> { ["pnr"] = "TK7QX2A", ["tutar"] = "6280" }, 30); TalepMotoru.TohumOlay(s1, "OtelOnaylandi", new Dictionary<string, string> { ["otel"] = "Ankara Anlaşmalı Otel", ["tutar"] = "6800" }, 29); TalepMotoru.TohumOlay(s1, "AvansOdendi", new Dictionary<string, string> { ["tutar"] = "4500" }, 21); TalepMotoru.TohumOlay(s1, "MasrafFormuGonderildi", new Dictionary<string, string> { ["tutar"] = "19240" }, 15); TalepMotoru.TohumOlay(s1, "MasrafOnaylandi", null, 12);
            var s2 = TalepMotoru.Tohum("SEYAHAT", "Ali Veli", "Ali Veli", "FUAR", D(30, 33, "İstanbul", "Frankfurt / Almanya", "Uçak", true, "24000", "18000", "9000", true, "Chemspec Europe fuarı; tedarikçi görüşmeleri", "Uras Holding", true, true, "P1004"), 3);
            var s3 = TalepMotoru.Tohum("SEYAHAT", "Gizem Tan", "Gizem Tan", "MUSTERI", D(2, 4, "İstanbul", "İzmir", "Şirket Aracı", true, "0", "5600", "2000", true, "Ege bölgesi bayi ziyaretleri (4 bayi)", "Avrupa Paper"), 8);
            TalepMotoru.TohumOlay(s3, "OtelOnaylandi", new Dictionary<string, string> { ["otel"] = "İzmir Anlaşmalı Otel", ["tutar"] = "5400" }, 6);
            var s4 = TalepMotoru.Tohum("SEYAHAT", "Ertan Yavuz", "Ertan Yavuz", "SAHA", D(-1, 1, "Gebze", "Bursa", "Şirket Aracı", true, "0", "3200", "800", true, "Müşteri sahasında kompresör devreye alma", "Uras Holding"), 9);
            TalepMotoru.TohumOlay(s4, "AvansOdendi", new Dictionary<string, string> { ["tutar"] = "4500" }, 2);
            var s5 = TalepMotoru.Tohum("SEYAHAT", "Gizem Tan", "Gizem Tan", "EGITIM", D(12, 14, "İstanbul", "Ankara", "Otobüs / Tren", true, "2400", "7500", "12000", false, "SAP Business One ileri raporlama eğitimi (sertifikalı)", "Avrupa Paper"), 2, 1);
            var s6 = TalepMotoru.Tohum("SEYAHAT", "Mert Doğan", "Mert Doğan", "YURTDISI", D(45, 52, "İstanbul", "Milano / İtalya", "Uçak", true, "28000", "32000", "6000", true, "Ekstrüder hattı kabul testleri (FAT) — makine üreticisi", "Uras Kimya", false, false), 1, 1);
            var s7 = TalepMotoru.Tohum("SEYAHAT", "Ali Veli", "Kerem Aksoy", "YURTICI", D(-8, -7, "İstanbul", "Konya", "Uçak", true, "5800", "3500", "600", false, "Veri merkezi tedarikçi denetimi", "Uras Kimya"), 16);
            TalepMotoru.TohumOlay(s7, "BiletAlindi", new Dictionary<string, string> { ["pnr"] = "PC3M8KL", ["tutar"] = "5620" }, 14); TalepMotoru.TohumOlay(s7, "OtelOnaylandi", new Dictionary<string, string> { ["otel"] = "Konya Anlaşmalı Otel", ["tutar"] = "3300" }, 14, 15);
            var s8 = TalepMotoru.Tohum("SEYAHAT", "Ali Veli", "Ali Veli", "YURTICI", D(20, 21, "İstanbul", "Antalya", "Uçak", true, "7000", "9000", "0", false, "Bilişim zirvesi", "Uras Kimya"), 5, 0); TalepMotoru.TohumRed(s8, "Etkinlik çevrim içi izlenebilir; bütçe onayı yok.");
            var s9 = TalepMotoru.Tohum("SEYAHAT", "Ertan Yavuz", "Ertan Yavuz", "SAHA", D(6, 6, "Gebze", "Adapazarı", "Kendi Aracım (km ödemeli)", false, "1200", "0", "300", false, "Servis: kompresör bakım", "Uras Holding"), 1, 0);
        }

        // =====================================================================
        // ARAÇ
        // =====================================================================
        public class AracKaydi { public string Plaka { get; set; } public string Marka { get; set; } public string Tip { get; set; } public int Kapasite { get; set; } public string Yakit { get; set; } public int Km { get; set; } public string Durum { get; set; } public string Ehliyet { get; set; } = "B"; public string Lokasyon { get; set; } }
        public static readonly List<AracKaydi> Filo = new List<AracKaydi>
        {
            new AracKaydi { Plaka = "34 URS 101", Marka = "Toyota Corolla Hybrid", Tip = "Binek", Kapasite = 5, Yakit = "Benzin/Hibrit", Km = 48200, Durum = "Müsait", Lokasyon = "Merkez" }, new AracKaydi { Plaka = "34 URS 102", Marka = "Renault Megane", Tip = "Binek", Kapasite = 5, Yakit = "Dizel", Km = 91500, Durum = "Müsait", Lokasyon = "Merkez" },
            new AracKaydi { Plaka = "34 URS 103", Marka = "Fiat Egea", Tip = "Binek", Kapasite = 5, Yakit = "Dizel", Km = 63100, Durum = "Serviste", Lokasyon = "Yetkili servis" }, new AracKaydi { Plaka = "41 URS 210", Marka = "VW Passat", Tip = "Şoförlü", Kapasite = 4, Yakit = "Dizel", Km = 120400, Durum = "Müsait", Lokasyon = "Gebze" },
            new AracKaydi { Plaka = "41 URS 305", Marka = "Ford Transit Custom", Tip = "Kamyonet", Kapasite = 3, Yakit = "Dizel", Km = 156800, Durum = "Müsait", Lokasyon = "Gebze" }, new AracKaydi { Plaka = "41 URS 306", Marka = "Fiat Doblo Cargo", Tip = "Kamyonet", Kapasite = 2, Yakit = "Dizel", Km = 88700, Durum = "Müsait", Lokasyon = "Gebze" },
            new AracKaydi { Plaka = "34 URS 401", Marka = "Mercedes Sprinter (16+1)", Tip = "Servis", Kapasite = 16, Yakit = "Dizel", Km = 210300, Durum = "Müsait", Ehliyet = "D1", Lokasyon = "Merkez" }, new AracKaydi { Plaka = "34 URS 104", Marka = "Peugeot 3008", Tip = "Binek", Kapasite = 5, Yakit = "Dizel", Km = 32900, Durum = "Müsait", Lokasyon = "Merkez" }
        };
        public class Ehliyet { public string Sinif { get; set; } public DateTime Gecerlilik { get; set; } public int CezaPuani { get; set; } }
        public static readonly Dictionary<string, Ehliyet> Ehliyetler = new Dictionary<string, Ehliyet>
        {
            ["P1001"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2031, 4, 12), CezaPuani = 0 }, ["P1002"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2029, 8, 3), CezaPuani = 20 }, ["P1004"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2028, 1, 20), CezaPuani = 55 },
            // P1005: "yakında doluyor" uyarısı örneği; sabit tarih olunca tohum (r7, teslim +12 gün) zamanla hataya dönüyordu
            ["P1005"] = new Ehliyet { Sinif = "B", Gecerlilik = DateTime.Today.AddDays(45), CezaPuani = 0 }, ["P1009"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2030, 6, 30), CezaPuani = 0 }, ["P1010"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2027, 3, 15), CezaPuani = 10 },
            ["P1011"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2032, 9, 9), CezaPuani = 0 }, ["P1013"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2026, 8, 1), CezaPuani = 0 }, ["P1014"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2030, 2, 2), CezaPuani = 0 }, ["P1017"] = new Ehliyet { Sinif = "B", Gecerlilik = new DateTime(2033, 1, 1), CezaPuani = 0 }
        };
        private static string TipKodu(string tur) => tur == "SOFORLU" ? "Şoförlü" : tur == "KAMYONET" ? "Kamyonet" : tur == "SERVIS" ? "Servis" : tur == "KIRALIK" ? "Kiralık" : "Binek";
        public static List<AracKaydi> MusaitAraclar(string tur, DateTime b, DateTime e, int? haricId = null)
        {
            string tip = TipKodu(tur); if (tip == "Kiralık") return new List<AracKaydi>();
            var dolu = Liste("ARAC").Where(t => t.Id != haricId && !t.Iptal && t.Onaylandi && t.Ek.ContainsKey("plaka") && !t.Ek.ContainsKey("iadeTarihi") && (t.T("baslangic")?.Date ?? DateTime.MinValue) <= e.Date && (t.T("bitis")?.Date ?? DateTime.MaxValue) >= b.Date).Select(t => t.E("plaka")).ToHashSet();
            return Filo.Where(x => x.Tip == tip && x.Durum == "Müsait" && !dolu.Contains(x.Plaka)).ToList();
        }
        private static TmModul Arac()
        {
            var m = new TmModul
            {
                Kod = "ARAC", Ad = "Araç Talebi", NoOnEk = "AR", Ikon = "fa-car-side", Renk = "#dc2626",
                Aciklama = "Havuz aracı, şoförlü araç, kamyonet, servis ya da kiralık araç talebi. Araç tahsisi, anahtar teslimi, iade km / yakıt ve HGS-ceza kayıtları filo sisteminden gelir; ehliyet ve müsaitlik kontrolü otomatiktir.",
                AkisOzeti = "Talep → Yönetici → (Direktör) → (İdari İşler) → Tahsis → Anahtar Teslim → İade → Km / Yakıt / HGS Kontrolü → Kapanış",
                KullananEtiket = "Talep eden personel", Durum2Ad = "Tahsis Durumu (İdari İşler)", Durum3Ad = "Kapanış Durumu (Filo / Finans)", IkinciKisiAlan = "surucu", IkinciKisiEtiket = "Kullanacak Kişi (Sürücü)",
                TakvimSatir = "arac", TakvimBas = "baslangic", TakvimBit = "bitis", TakvimAd = "Araç Takvimi",
                Turler = { T("HAVUZ", "Havuz Aracı (Binek)", "Y", "Sürücünün B sınıfı geçerli ehliyeti olmalı."), T("SOFORLU", "Şoförlü Araç", "Y,A", "Şirket şoförü İdari İşler tarafından atanır."), T("KIRALIK", "Kiralık Araç", "Y,D,A", "Havuzda uygun araç yoksa; Direktör ve İdari İşler onayı."), T("KAMYONET", "Kamyonet / Panelvan", "Y,A", "Yük taşıma; ehliyet kontrolü."), T("SERVIS", "Servis Aracı (Toplu)", "Y,A", "16+1 servis; şoförlü.") },
                Alanlar = { A("baslangic", "Alış Tarihi", "date", true, 3), A("baslangicSaat", "Alış Saati", "time", true, 2, vars: "08:30"), A("bitis", "Teslim Tarihi", "date", true, 3), A("bitisSaat", "Teslim Saati", "time", true, 2, vars: "18:00"), A("kisiSayisi", "Yolcu Sayısı", "number", true, 2, vars: "1"),
                            A("guzergah", "Güzergâh", "text", true, 6, ipucu: "Örn. Fabrika → Gebze OSB → Fabrika"), A("tahminiKm", "Tahmini Km", "number", true, 2, vars: "50"), A("surucu", "Kullanacak Kişi (Sürücü)", "personel", false, 4),
                            A("yakitKarti", "Yakıt kartı istiyorum", "checkbox", false, 3), A("hgs", "HGS / OGS istiyorum", "checkbox", false, 3), A("sehirDisi", "Şehir dışı kullanım", "checkbox", false, 3), A("amac", "Kullanım Amacı", "textarea", true, 12) },
                Olaylar = { O("AracTahsis", "AracTahsis — Filo aracı atadı", "Filo", "Plaka müsait araçlar arasından seçilir.", "plaka|Plaka|filo"), O("AnahtarTeslim", "AnahtarTeslim — Çıkış km ve yakıt", "Filo", "Anahtar teslim formu; çıkış km ve yakıt seviyesi.", "cikisKm|Çıkış Km|number", "yakit|Yakıt Seviyesi (%)|number"), O("AracIade", "AracIade — Dönüş km, yakıt, hasar", "Filo", "İade formu; kullanılan km ve hasar kaydı.", "donusKm|Dönüş Km|number", "yakit|Yakıt Seviyesi (%)|number", "hasar|Hasar|select:Yok;Var"), O("HgsCezaBildirimi", "HgsCezaBildirimi — HGS / trafik cezası", "HGS/EGM", "Kullanım tarihlerine düşen geçiş ücreti ve ceza.", "tutar|Tutar (TL)|number", "aciklama|Açıklama|text"), O("HasarKapatildi", "HasarKapatildi — Ekspertiz / sigorta sonucu", "Filo", "Hasar dosyası kapatılır.", "not|Sonuç|text"), O("KapanisOnayi", "KapanisOnayi — Filo kapanış onayı", "Filo", "Km, yakıt ve ceza kontrolleri tamamlandı; talep kapanır.") },
                HesapEtiketler = { ["gun"] = "Gün Sayısı", ["ehliyet"] = "Sürücü Ehliyeti", ["musaitArac"] = "Uygun Müsait Araç", ["haftaSonu"] = "Hafta Sonu Kullanımı" },
                EkEtiketler = { ["plaka"] = "Tahsis Edilen Araç", ["arac"] = "Marka / Model", ["tahsisTarihi"] = "Tahsis Tarihi", ["cikisKm"] = "Çıkış Km", ["cikisYakit"] = "Çıkış Yakıt (%)", ["teslimTarihi"] = "Anahtar Teslim", ["donusKm"] = "Dönüş Km", ["donusYakit"] = "Dönüş Yakıt (%)", ["iadeTarihi"] = "İade Tarihi", ["kullanilanKm"] = "Kullanılan Km", ["hasar"] = "Hasar", ["cezaTutar"] = "HGS / Ceza (TL)", ["cezaAciklama"] = "Ceza Açıklaması", ["kapanisTarihi"] = "Kapanış" },
                Parametreler = { ["direktorGun"] = "3", ["cezaPuaniUyari"] = "50", ["kmUyari"] = "500", ["bildirimGun"] = "1", ["yakitFarkTl"] = "60" },
                ParametreEtiketler = { ["direktorGun"] = "Bu günden uzun tahsiste Direktör onayı", ["cezaPuaniUyari"] = "Sürücü ceza puanı uyarı eşiği", ["kmUyari"] = "Tahmini km uyarı eşiği (konaklama)", ["bildirimGun"] = "En az bildirim (gün)", ["yakitFarkTl"] = "Yakıt seviyesi farkı birim maliyeti (TL / %)" },
                Kurallar = { "Havuz, kamyonet ve kiralık araçta sürücünün B sınıfı geçerli ehliyeti zorunludur; ceza puanı eşiği aşınca uyarı verilir.", "Aynı araç aynı tarih aralığında iki talebe tahsis edilemez; müsaitlik canlı hesaplanır.", "3 günü aşan tahsislerde Direktör, hafta sonu kullanımında İdari İşler onayı eklenir.", "Çıkış / dönüş km, yakıt seviyesi ve hasar kaydı filo sisteminden gelir; elle girilmez.", "HGS ve ceza kontrolü tamamlanmadan talep kapanmaz; ceza kullanan kişiye yansıtılır." },
                Master = { ["filo"] = Filo, ["ehliyetler"] = Ehliyetler.ToDictionary(k => k.Key, k => new { k.Value.Sinif, Gecerlilik = k.Value.Gecerlilik.ToString("dd.MM.yyyy", Tr), k.Value.CezaPuani }) },
                TakipSutunlar = { ("Talep No", "no"), ("Talep Eden", "kullanan"), ("Sürücü", "kisi2"), ("Departman", "departman"), ("Tür", "tur"), ("Alış", "d.baslangic"), ("Teslim", "d.bitis"), ("Gün", "h.gun"), ("Güzergâh", "d.guzergah"), ("Tahmini Km", "d.tahminiKm"), ("Plaka", "e.plaka"), ("Çıkış Km", "e.cikisKm"), ("Dönüş Km", "e.donusKm"), ("Kullanılan Km", "e.kullanilanKm"), ("Talep Durumu", "talepDurumu"), ("Tahsis", "durum2"), ("Kapanış", "durum3"), ("Hasar", "e.hasar"), ("Ceza (TL)", "e.cezaTutar"), ("İdari İşler", "onay.İdari İşler Onayı") }
            };
            m.Hesapla = a =>
            {
                var s = new TmSonuc(); var m2 = a.Modul;
                var b = a.T("baslangic"); var e = a.T("bitis"); if (b == null || e == null) { s.Hata = "Alış ve teslim tarihi zorunludur."; return s; }
                if (e.Value.Date < b.Value.Date || (e.Value.Date == b.Value.Date && SaatFarki(a.V("baslangicSaat"), a.V("bitisSaat")) <= 0)) { s.Hata = "Teslim, alıştan sonra olmalı."; return s; }
                int gun = (e.Value.Date - b.Value.Date).Days + 1; a.Hesap["gun"] = gun.ToString();
                bool hs = Enumerable.Range(0, gun).Any(i => HaftaSonu(b.Value.AddDays(i))); a.Hesap["haftaSonu"] = hs ? "Evet" : "Hayır";
                var surucu = string.IsNullOrEmpty(a.V("surucu")) ? a.Kullanan : (TalepMotoru.KisiBul(a.V("surucu")) ?? a.Kullanan);
                bool ehliyetGerekli = a.TurKod == "HAVUZ" || a.TurKod == "KAMYONET" || a.TurKod == "KIRALIK";
                if (ehliyetGerekli)
                {
                    if (!Ehliyetler.TryGetValue(surucu.PersonelNo, out var eh)) { a.Hesap["ehliyet"] = "Kayıt yok"; s.Hata = $"{surucu.AdSoyad} için İK'da ehliyet kaydı yok; havuz aracı kullanamaz."; return s; }
                    a.Hesap["ehliyet"] = $"{eh.Sinif} · {eh.Gecerlilik:dd.MM.yyyy} · ceza {eh.CezaPuani}";
                    if (eh.Gecerlilik < e.Value.Date) { s.Hata = $"{surucu.AdSoyad} ehliyeti {eh.Gecerlilik:dd.MM.yyyy} tarihinde doluyor; teslim tarihinden önce yenilenmeli."; return s; }
                    if (eh.Gecerlilik < e.Value.Date.AddMonths(2)) s.Uyarilar.Add($"Sürücü ehliyeti {eh.Gecerlilik:dd.MM.yyyy} tarihinde doluyor.");
                    if (eh.CezaPuani >= m2.P("cezaPuaniUyari")) s.Uyarilar.Add($"Sürücü ceza puanı {eh.CezaPuani} (eşik {S(m2.P("cezaPuaniUyari"))}); İdari İşler değerlendirmesi.");
                    if (IzinOrnek.Cakisanlar(surucu, b.Value, e.Value).Any(x => x.Onaylandi)) { s.Hata = $"{surucu.AdSoyad} bu tarihlerde izinli; araç kullanamaz."; return s; }
                }
                else a.Hesap["ehliyet"] = "Şirket şoförü";
                var musait = MusaitAraclar(a.TurKod, b.Value, e.Value, a.Id); a.Hesap["musaitArac"] = a.TurKod == "KIRALIK" ? "Kiralama (acente)" : musait.Count == 0 ? "Yok" : $"{musait.Count} araç ({string.Join(", ", musait.Select(x => x.Plaka).Take(3))})";
                if (a.TurKod != "KIRALIK" && musait.Count == 0) s.Uyarilar.Add("Seçilen tarihlerde uygun müsait araç yok; İdari İşler kiralık araç değerlendirir.");
                if (gun > m2.P("direktorGun")) { s.EkAdimlar.Add("D"); s.Uyarilar.Add($"{S(m2.P("direktorGun"))} günden uzun tahsis: Direktör onayı eklendi."); }
                if (hs) { s.EkAdimlar.Add("A"); s.Uyarilar.Add("Hafta sonu kullanımı: İdari İşler onayı eklendi."); }
                if (a.D("tahminiKm") > m2.P("kmUyari")) s.Uyarilar.Add($"Tahmini {S(a.D("tahminiKm"))} km: uzun yol; şehir dışı kullanım ve konaklama seyahat talebiyle birlikte planlanmalı.");
                if ((b.Value.Date - DateTime.Today).Days < m2.P("bildirimGun")) s.Uyarilar.Add($"Araç talebi en az {S(m2.P("bildirimGun"))} gün önce açılmalıdır.");
                int kap = a.TurKod == "SERVIS" ? 16 : a.TurKod == "KAMYONET" ? 3 : a.TurKod == "SOFORLU" ? 4 : 5; if (a.D("kisiSayisi") > kap) s.Uyarilar.Add($"Yolcu sayısı {S(a.D("kisiSayisi"))} araç kapasitesini ({kap}) aşıyor; servis aracı düşünün.");
                return s;
            };
            m.Durum2 = a =>
            {
                if (a.Ek.ContainsKey("iadeTarihi")) return "İade Edildi";
                if (a.Ek.ContainsKey("teslimTarihi")) return DateTime.Today > (a.T("bitis")?.Date ?? DateTime.Today) ? "İade Bekleniyor (gecikmiş)" : "Kullanımda";
                if (a.Ek.ContainsKey("plaka")) return "Araç Tahsis Edildi";
                return "Araç Bekleniyor";
            };
            m.Durum3 = a => a.Ek.ContainsKey("kapanisTarihi") ? "Kapandı" : a.E("hasar") == "Var" && !a.Ek.ContainsKey("hasarKapanis") ? "Hasar Kaydı Açık" : a.Ek.ContainsKey("cezaTutar") && !a.Ek.ContainsKey("cezaYansitildi") ? "HGS / Ceza Kontrolü" : a.Ek.ContainsKey("iadeTarihi") ? "Km & Yakıt Kontrolü" : "—";
            m.Olay = (a, kod, g) =>
            {
                switch (kod)
                {
                    case "AracTahsis":
                        { string p = G(g, "plaka"); var b = a.T("baslangic") ?? DateTime.Today; var e = a.T("bitis") ?? b; var musait = MusaitAraclar(a.TurKod, b, e, a.Id); if (p == "") p = musait.FirstOrDefault()?.Plaka ?? ""; if (a.TurKod == "KIRALIK") { p = p == "" ? "34 KRL " + (500 + a.Id) : p; a.Ek["plaka"] = p; a.Ek["arac"] = "Kiralık (acente)"; } else { var ar = Filo.FirstOrDefault(x => x.Plaka == p); if (ar == null) return "Plaka filoda yok."; if (!musait.Any(x => x.Plaka == p)) return $"{p} seçilen tarihlerde müsait değil."; a.Ek["plaka"] = p; a.Ek["arac"] = ar.Marka; } a.Ek["tahsisTarihi"] = Simdi.ToString("dd.MM.yyyy HH:mm", Tr); H(a, $"Filo: {a.E("plaka")} ({a.E("arac")}) tahsis edildi", "Filo"); L(a, "VEHICLE", "", a.E("plaka"), "Filo"); return null; }
                    case "AnahtarTeslim":
                        { if (!a.Ek.ContainsKey("plaka")) return "Önce araç tahsis edilmeli."; if (a.Ek.ContainsKey("teslimTarihi")) return "Anahtar zaten teslim edilmiş."; var ar = Filo.FirstOrDefault(x => x.Plaka == a.E("plaka")); decimal km = GD(g, "cikisKm"); if (km <= 0) km = ar?.Km ?? 0; a.Ek["cikisKm"] = N(km); a.Ek["cikisYakit"] = (GD(g, "yakit") <= 0 ? 75 : GD(g, "yakit")).ToString("0"); a.Ek["teslimTarihi"] = Simdi.ToString("dd.MM.yyyy HH:mm", Tr); H(a, $"Filo: anahtar teslim edildi — çıkış {a.E("cikisKm")} km, yakıt %{a.E("cikisYakit")}", "Filo"); L(a, "KM_OUT", "", a.E("cikisKm"), "Filo"); return null; }
                    case "AracIade":
                        { if (!a.Ek.ContainsKey("teslimTarihi")) return "Anahtar teslim edilmeden iade alınamaz."; if (a.Ek.ContainsKey("iadeTarihi")) return "Araç zaten iade edilmiş."; decimal ck = decimal.Parse(a.E("cikisKm"), NumberStyles.Any, Tr), dk = GD(g, "donusKm"); if (dk <= 0) dk = ck + a.D("tahminiKm"); if (dk < ck) return "Dönüş km çıkış km'den küçük olamaz."; a.Ek["donusKm"] = N(dk); a.Ek["donusYakit"] = (GD(g, "yakit") <= 0 ? 40 : GD(g, "yakit")).ToString("0"); a.Ek["kullanilanKm"] = N(dk - ck); a.Ek["hasar"] = G(g, "hasar") == "Var" ? "Var" : "Yok"; a.Ek["iadeTarihi"] = Simdi.ToString("dd.MM.yyyy HH:mm", Tr); var ar = Filo.FirstOrDefault(x => x.Plaka == a.E("plaka")); if (ar != null) ar.Km = (int)dk; decimal yakitFark = decimal.Parse(a.E("cikisYakit")) - decimal.Parse(a.E("donusYakit")); if (yakitFark > 0) a.Ek["yakitMaliyet"] = N(yakitFark * a.Modul.P("yakitFarkTl")); H(a, $"Filo: araç iade alındı — dönüş {a.E("donusKm")} km (kullanılan {a.E("kullanilanKm")} km), yakıt %{a.E("donusYakit")}, hasar {a.E("hasar")}", "Filo"); L(a, "KM_IN", "", a.E("donusKm"), "Filo"); if (a.E("hasar") == "Var") { H(a, "Filo: hasar dosyası açıldı; ekspertiz bekleniyor", "Filo"); } return null; }
                    case "HgsCezaBildirimi":
                        { if (!a.Ek.ContainsKey("teslimTarihi")) return "Araç kullanılmadan ceza bildirimi olamaz."; decimal t = GD(g, "tutar"); if (t <= 0) return "Tutar sıfırdan büyük olmalı."; decimal mevcut = a.Ek.ContainsKey("cezaTutar") ? decimal.Parse(a.E("cezaTutar"), NumberStyles.Any, Tr) : 0; a.Ek["cezaTutar"] = N(mevcut + t); a.Ek["cezaAciklama"] = ((a.E("cezaAciklama") + "; " + (G(g, "aciklama") == "" ? "HGS geçiş" : G(g, "aciklama"))).TrimStart(';', ' ')); a.Ek.Remove("cezaYansitildi"); H(a, $"HGS/EGM: {N(t)} TL bildirim ({(G(g, "aciklama") == "" ? "HGS geçiş" : G(g, "aciklama"))}); kullanan kişiye yansıtılacak", "HGS/EGM"); L(a, "TOLL_FINE", N(mevcut), N(mevcut + t), "HGS/EGM"); return null; }
                    case "HasarKapatildi":
                        { if (a.E("hasar") != "Var") return "Açık hasar kaydı yok."; a.Ek["hasarKapanis"] = Simdi.ToString("dd.MM.yyyy", Tr) + (G(g, "not") == "" ? "" : " · " + G(g, "not")); H(a, "Filo: hasar dosyası kapatıldı" + (G(g, "not") == "" ? "" : " (" + G(g, "not") + ")"), "Filo"); L(a, "DAMAGE", "Açık", "Kapandı", "Filo"); return null; }
                    case "KapanisOnayi":
                        { if (!a.Ek.ContainsKey("iadeTarihi")) return "Araç iade edilmeden kapanış yapılamaz."; if (a.E("hasar") == "Var" && !a.Ek.ContainsKey("hasarKapanis")) return "Hasar dosyası açıkken kapanış yapılamaz."; if (a.Ek.ContainsKey("cezaTutar")) a.Ek["cezaYansitildi"] = "Evet"; a.Ek["kapanisTarihi"] = Simdi.ToString("dd.MM.yyyy", Tr); H(a, "Filo: km, yakıt ve ceza kontrolleri tamamlandı; talep kapatıldı" + (a.Ek.ContainsKey("cezaTutar") ? $" ({a.E("cezaTutar")} TL ceza bordroya yansıtıldı)" : ""), "Filo"); L(a, "CLOSE_STATUS", "Açık", "Kapandı", "Filo"); return null; }
                }
                return "Tanımsız olay.";
            };
            m.Ozet = a => $"{a.Tur.Ad} · {a.T("baslangic"):dd.MM.yyyy} {a.V("baslangicSaat")} – {a.T("bitis"):dd.MM.yyyy} {a.V("bitisSaat")} · {a.V("guzergah")}";
            m.Etiketle = a => { var l = new List<string>(); bool ok = a.Onaylandi && !a.Iptal; var b = a.T("baslangic")?.Date; var e = a.T("bitis")?.Date; if (ok && b <= DateTime.Today && e >= DateTime.Today && !a.Ek.ContainsKey("iadeTarihi")) l.Add("bugun"); if (a.TalepDurumu.EndsWith("Bekliyor") && !a.TalepDurumu.StartsWith("Revizyon")) l.Add("onay"); if (ok && a.Durum2 == "Araç Bekleniyor") l.Add("tahsis"); if (ok && a.Durum2.StartsWith("İade Bekleniyor")) l.Add("gecikmis"); if (ok && (a.Durum3 == "Hasar Kaydı Açık" || a.Durum3 == "HGS / Ceza Kontrolü")) l.Add("hasar"); return l; };
            m.Kpiler = l =>
            {
                var tahsisli = l.Where(t => t.Etiketler.Contains("bugun")).Select(t => t.E("plaka")).Where(p => p != "").Distinct().ToList();
                int musait = Filo.Count(x => x.Durum == "Müsait" && !tahsisli.Contains(x.Plaka)), serviste = Filo.Count(x => x.Durum == "Serviste");
                return new List<TmKpi> { K("#dc2626", "Bugün Tahsisli", $"{tahsisli.Count} Araç", Bos(string.Join(", ", tahsisli)), "bugun"), K("#16a34a", "Müsait Araç", $"{musait} / {Filo.Count}", $"{serviste} serviste", "musait"), K("#f59e0b", "Onay Bekleyen", $"{l.Count(t => t.Etiketler.Contains("onay"))} Talep", Adlar(l.Where(t => t.Etiketler.Contains("onay"))), "onay"), K("#0ea5e9", "Tahsis Bekleyen", $"{l.Count(t => t.Etiketler.Contains("tahsis"))} Talep", "onaylı, plaka atanmadı", "tahsis"), K("#7c3aed", "İade Gecikmiş", $"{l.Count(t => t.Etiketler.Contains("gecikmis"))} Araç", Adlar(l.Where(t => t.Etiketler.Contains("gecikmis"))), "gecikmis"), K("#0891b2", "Hasar / Ceza Kaydı", $"{l.Count(t => t.Etiketler.Contains("hasar"))} Talep", Adlar(l.Where(t => t.Etiketler.Contains("hasar"))), "hasar") };
            };
            return m;
        }
        private static void AracTohum()
        {
            Dictionary<string, string> D(int bas, int bit, string guz, string km, string surucu, string amac, string kisi = "1", string bs = "08:30", string bts = "18:00", bool yakit = false, bool hgs = false) => new Dictionary<string, string> { ["baslangic"] = DateTime.Today.AddDays(bas).ToString("yyyy-MM-dd"), ["baslangicSaat"] = bs, ["bitis"] = DateTime.Today.AddDays(bit).ToString("yyyy-MM-dd"), ["bitisSaat"] = bts, ["kisiSayisi"] = kisi, ["guzergah"] = guz, ["tahminiKm"] = km, ["surucu"] = surucu, ["amac"] = amac, ["yakitKarti"] = yakit ? "true" : "false", ["hgs"] = hgs ? "true" : "false" };
            var r1 = TalepMotoru.Tohum("ARAC", "Ali Veli", "Ali Veli", "HAVUZ", D(-12, -12, "Merkez → Gebze OSB → Merkez", "120", "P1001", "Fabrika sunucu odası kontrolü", "2", "08:30", "17:30", false, true), 14);
            TalepMotoru.TohumOlay(r1, "AracTahsis", new Dictionary<string, string> { ["plaka"] = "34 URS 101" }, 13); TalepMotoru.TohumOlay(r1, "AnahtarTeslim", new Dictionary<string, string> { ["cikisKm"] = "48050", ["yakit"] = "80" }, 12, 8); TalepMotoru.TohumOlay(r1, "AracIade", new Dictionary<string, string> { ["donusKm"] = "48200", ["yakit"] = "55", ["hasar"] = "Yok" }, 12, 18); TalepMotoru.TohumOlay(r1, "HgsCezaBildirimi", new Dictionary<string, string> { ["tutar"] = "96", ["aciklama"] = "Osmangazi Köprüsü HGS" }, 9); TalepMotoru.TohumOlay(r1, "KapanisOnayi", null, 8);
            var r2 = TalepMotoru.Tohum("ARAC", "Ali Veli", "Ali Veli", "HAVUZ", D(0, 0, "Merkez → Ümraniye müşteri → Merkez", "60", "P1001", "Müşteri sahasında ağ kurulumu"), 2);
            TalepMotoru.TohumOlay(r2, "AracTahsis", new Dictionary<string, string> { ["plaka"] = "34 URS 104" }, 1); TalepMotoru.TohumOlay(r2, "AnahtarTeslim", new Dictionary<string, string> { ["cikisKm"] = "32900", ["yakit"] = "70" }, 0, 8);
            var r3 = TalepMotoru.Tohum("ARAC", "Gizem Tan", "Gizem Tan", "HAVUZ", D(2, 4, "İstanbul → İzmir bayiler → İstanbul", "1100", "P1005", "Ege bölgesi bayi ziyaretleri (seyahat talebiyle birlikte)", "1", "07:00", "20:00", true, true), 8);
            TalepMotoru.TohumOlay(r3, "AracTahsis", new Dictionary<string, string> { ["plaka"] = "34 URS 102" }, 6);
            var r4 = TalepMotoru.Tohum("ARAC", "Esra Kaya", "Esra Kaya", "KAMYONET", D(-3, -3, "Gebze → Bursa müşteri → Gebze", "320", "P1010", "Kalibrasyon ekipmanı taşıma", "2", "07:30", "19:00", true, true), 6);
            TalepMotoru.TohumOlay(r4, "AracTahsis", new Dictionary<string, string> { ["plaka"] = "41 URS 305" }, 5); TalepMotoru.TohumOlay(r4, "AnahtarTeslim", new Dictionary<string, string> { ["cikisKm"] = "156470", ["yakit"] = "65" }, 3, 7); TalepMotoru.TohumOlay(r4, "AracIade", new Dictionary<string, string> { ["donusKm"] = "156800", ["yakit"] = "30", ["hasar"] = "Var" }, 3, 19);
            var r5 = TalepMotoru.Tohum("ARAC", "Mert Doğan", "Mert Doğan", "SOFORLU", D(5, 5, "Merkez → Sabiha Gökçen → Merkez", "90", "", "Yurt dışı müşteri heyeti karşılama", "3", "09:00", "14:00"), 2, 1);
            var r6 = TalepMotoru.Tohum("ARAC", "Kerem Aksoy", "Kerem Aksoy", "HAVUZ", D(-1, 3, "Merkez → Ankara → Merkez", "950", "P1002", "Veri merkezi denetimi", "1", "07:00", "19:00", true, true), 6);
            TalepMotoru.TohumOlay(r6, "AracTahsis", new Dictionary<string, string> { ["plaka"] = "34 URS 101" }, 2); TalepMotoru.TohumOlay(r6, "AnahtarTeslim", new Dictionary<string, string> { ["cikisKm"] = "48200", ["yakit"] = "85" }, 1, 7);
            var r7 = TalepMotoru.Tohum("ARAC", "Gizem Tan", "Gizem Tan", "KIRALIK", D(10, 12, "İzmir Havalimanı → Denizli → İzmir", "450", "P1005", "Fuar sonrası müşteri ziyaretleri", "2"), 1, 0);
            var r8 = TalepMotoru.Tohum("ARAC", "Onur Bal", "Onur Bal", "SERVIS", D(7, 7, "Merkez → Gebze fabrika → Merkez", "110", "", "Denetim ekibi transferi", "12", "08:00", "18:00"), 1, 1);
            var r9 = TalepMotoru.Tohum("ARAC", "Ali Veli", "Ali Veli", "HAVUZ", D(-28, -28, "Merkez → Tuzla → Merkez", "70", "P1001", "Donanım teslim"), 27, 0); TalepMotoru.TohumRed(r9, "Kargo ile gönderilebilir; araç gerekmiyor.");
            var r10 = TalepMotoru.Tohum("ARAC", "Yasin Sezgin", "Yasin Sezgin", "KAMYONET", D(-6, -5, "Gebze → Eskişehir müşteri → Gebze", "560", "P1014", "Kompresör parça sevkiyatı", "2", "06:30", "20:00", true, true), 9);
            TalepMotoru.TohumOlay(r10, "AracTahsis", new Dictionary<string, string> { ["plaka"] = "41 URS 306" }, 8); TalepMotoru.TohumOlay(r10, "AnahtarTeslim", new Dictionary<string, string> { ["cikisKm"] = "88120", ["yakit"] = "90" }, 6, 6); TalepMotoru.TohumOlay(r10, "AracIade", new Dictionary<string, string> { ["donusKm"] = "88700", ["yakit"] = "35", ["hasar"] = "Yok" }, 5, 20); TalepMotoru.TohumOlay(r10, "HgsCezaBildirimi", new Dictionary<string, string> { ["tutar"] = "1180", ["aciklama"] = "Hız ihlali (Bolu Dağı) — EGM" }, 2);
        }
    }
}
