// ============================================================
// RESMİ BORDRO KİMLİK BİLGİLERİ (9 Kas 2026, kullanıcı: "bu bilgiler aslında sende var; tüm bilgileri kontrol et, kartları doğru şekilde doldur, eksik olmasın, hepsinde senkronize düzelt")
//   Kaynak: altı şirketin resmi Ekim 2026 ücret bordrosu (.xls; 5 Kas 2026 yüklemesi: ALV Kimya, Uras Kimya, Selvi Kimya, Uras Holding, Avrupa Paper, Alv Filo) — sütunlar: Departman Kodu,
//   Departman Açıklaması, Sicil Numarası, SGK Numarası, Adı-Soyadı. Bordroda TC kimlik, işe giriş tarihi, unvan ve görev YOKTUR (yalnız Alv Filo'da boş bir "İş Ünvanı Açıklaması" sütunu var).
//   Uygulama: her dönem listesi kurulurken (DonemleriKur / DonemOlustur) Tamamla(liste): ad eşleşmesiyle (Türkçe harf katlama; "Devrim / Hale Bisren" ↔ "HALE BİSREN" takma adı)
//   • Sicil Numarası ve SGK Numarası resmi bordrodan yazılır (tohumun ürettiği "ALV-0001" / 2600… biçimindeki değerlerin yerine; bordroda olmayan özel kişilerde dokunulmaz).
//   • Departman Kodu / Açıklaması boşsa panelin kendi düzeninden (Bölüm → D-kodu, "<Bölüm> Departmanı"; 6 Kas 2026 kuralı) doldurulur — bordronun şirkete özel kodları (ALV "11 SATIŞ PAZARLAMA" vb.)
//     ekrandaki D-düzenini bozmasın diye kullanılmaz, yalnız not alanı için saklanır.
//   • Ekim'de işe giren ve kimliği boş gelen kişiler (Yeniler): bordro departmanından bölüm / çatı, işe giriş = Ekim prim gününden (27 gün → 5 Ekim, 6 gün → 26 Ekim 2026); görev ve unvan VARSAYIMDIR,
//     kullanıcı kişi kartından düzeltir (nota yazılır).
//   Bordroda olmayan alanlar (TC kimlik, telefon, e-posta, gündelikçilerin işe giriş tarihi) boş kalır; uydurulmaz.
// ============================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApplication3.Data
{
    public class BordroKimlik { public string Sirket { get; set; } public string Ad { get; set; } public string DepartmanKodu { get; set; } public string Departman { get; set; } public string Sicil { get; set; } public string SgkNo { get; set; } }

    public static class PersonelKimlikBordro
    {
        public const string Kaynak = "Resmi Ekim 2026 ücret bordroları (6 şirket, 5 Kas 2026)";
        private const string ALV = "ALV Kimya", URS = "Uras Kimya", SLV = "Selvi Kimya", HLD = "Uras Holding", AVP = "Avrupa Paper", FLO = "Alv Filo";
        private static BordroKimlik K(string sirket, string dk, string dep, string sicil, string sgk, string ad) => new BordroKimlik { Sirket = sirket, DepartmanKodu = dk, Departman = dep, Sicil = sicil, SgkNo = sgk, Ad = ad };
        /// <summary>88 bordrolu satır (bordro sırası; adlar bordrodaki gibi büyük harf).</summary>
        public static readonly BordroKimlik[] Satirlar =
        {
            // ---- ALV Kimya (38)
            K(ALV, "1", "AVUKAT", "R007", "3409200639956", "CÜNEYT ARDA CEYLAN"), K(ALV, "11", "SATIŞ PAZARLAMA", "R-333", "3406199205353", "ÜMİT ÖZDEMİR"), K(ALV, "11", "SATIŞ PAZARLAMA", "R-337", "5929202501854", "ASLAN HUSEYNOV"),
            K(ALV, "13", "ÜRETİM", "R021", "0602200638882", "ÖZGÜR PAMİR"), K(ALV, "13", "ÜRETİM", "R055", "3402200638622", "SAVAŞ ÖZCAN"), K(ALV, "13", "ÜRETİM", "R-164", "3401200829370", "EMRE CAN YAVUZ"),
            K(ALV, "13", "ÜRETİM", "R-320", "5929202400384", "KAMİL MAHMUDOV"), K(ALV, "13", "ÜRETİM", "R-330", "5901200008258", "SONER İKİZOĞLU"), K(ALV, "13", "ÜRETİM", "R-332", "5921201604983", "GÖRKEM AKBULUT"),
            K(ALV, "13", "ÜRETİM", "R-418", "3501200351201", "SELÇUK MERT"), K(ALV, "16", "İNSAN KAYNAKLARI", "R-417", "5901200210499", "DİLBER GÜNEY"), K(ALV, "8", "YÖNETİM", "R-101", "5921201603140", "SEMİH EVİRGEN"),
            K(ALV, "9", "İÇ HİZMETLİ", "R054", "5501201003287", "ARSLAN ÇOŞKUN"), K(ALV, "9", "İÇ HİZMETLİ", "R096", "3921201301339", "BURHAN EVVEL"), K(ALV, "9", "İÇ HİZMETLİ", "R-323", "5901018194155", "ŞİRİN TUNÇ"),
            K(ALV, "11", "SATIŞ PAZARLAMA", "R-390", "3401202112106", "BERKAN ÖZKAN"), K(ALV, "11", "SATIŞ PAZARLAMA", "R-395", "2601200610125", "ERMAN KAVAL"), K(ALV, "11", "SATIŞ PAZARLAMA", "R-401", "3402201008790", "YUSUF ÖZTÜRK"),
            K(ALV, "11", "SATIŞ PAZARLAMA", "R-402", "3405200620416", "MUSTAFA BUDAK"), K(ALV, "11", "SATIŞ PAZARLAMA", "R-404", "3401200537435", "AKSEL GÜLER"), K(ALV, "11", "SATIŞ PAZARLAMA", "R-409", "3401202411176", "EFE AKÇAKALE"),
            K(ALV, "13", "ÜRETİM", "R-405", "2201201902734", "BERK YENİLMEZ"), K(ALV, "13", "ÜRETİM", "R-407", "3407200203488", "CAFER İPEK"), K(ALV, "15", "DEPO VE SEVKİYAT", "R-406", "3409200423607", "BURHAN GÜN"),
            K(ALV, "15", "DEPO VE SEVKİYAT", "R-408", "6701019913576", "DİNÇER ÖZDEMİR"), K(ALV, "15", "DEPO VE SEVKİYAT", "R-410", "3401199512457", "İSMAİL DUMAN"), K(ALV, "15", "DEPO VE SEVKİYAT", "R-411", "3401199834337", "MUSTAFA ULAŞ ERİKLİ"),
            K(ALV, "15", "DEPO VE SEVKİYAT", "R-414", "3409200201897", "YALÇIN AVCI"), K(ALV, "16", "İNSAN KAYNAKLARI", "R-412", "3601018639807", "NESLİHAN ULUDAĞ"), K(ALV, "3", "FİNANS VE MUHASEBE", "R-338", "3401200629234", "DİLEK GÜDEN"),
            K(ALV, "3", "FİNANS VE MUHASEBE", "R-389", "3401201133538", "NEVRUZ SARITAŞ"), K(ALV, "3", "FİNANS VE MUHASEBE", "R-400", "3401201908589", "MUSTAFA SAVAŞ"), K(ALV, "6", "KİMYAGER", "R059", "3405200012079", "ESER ŞENÜREK"),
            K(ALV, "8", "YÖNETİM", "R-001", "3401202004885", "ALEV ÖZAVŞAR EREN"), K(ALV, "8", "YÖNETİM", "R-398", "3501201256042", "HALE BİSREN"), K(ALV, "8", "YÖNETİM", "R-416", "3901201701160", "BURAK SERGEN ADA"),
            K(ALV, "9", "İÇ HİZMETLİ", "R-413", "3400016134083", "RABİYE DOĞAN"), K(ALV, "9", "İÇ HİZMETLİ", "R-415", "3400017101264", "ZAHİT ERCAN"),
            // ---- Avrupa Paper (24)
            K(AVP, "03", "FİNANS VE MUHASEBE", "R-1", "0601200843623", "AHMET ÖZKAN"), K(AVP, "07", "YÖNETİM", "R-4", "3406201234239", "BAHAR SEYREKOĞLU"), K(AVP, "06", "KALİTE KONTROL", "R-2", "1701201203599", "BEDİRHAN GENÇ"),
            K(AVP, "03", "FİNANS VE MUHASEBE", "R-3", "3426201400545", "BERK BERBEROĞLU"), K(AVP, "07", "YÖNETİM", "R-5", "3401201526321", "BURAK KAYA"), K(AVP, "05", "DEPO - SEVKİYAT", "R-8", "3401201643600", "CAN GÜRSEL"),
            K(AVP, "07", "YÖNETİM", "R-24", "3409200639956", "CÜNEYT ARDA CEYLAN"), K(AVP, "07", "YÖNETİM", "R-6", "5801201001167", "EDA BOZKURT"), K(AVP, "05", "DEPO - SEVKİYAT", "R-7", "3401200434082", "EMRAH ATEŞ"),
            K(AVP, "01", "SATIŞ VE PAZARLAMA", "R-9", "3409201046655", "EMRE KESKİN"), K(AVP, "06", "KALİTE KONTROL", "R-10", "1101201301508", "EZGİ DEMİR"), K(AVP, "06", "KALİTE KONTROL", "R-13", "3401201543367", "HACI MUSTAFA KORKMAZ"),
            K(AVP, "03", "FİNANS VE MUHASEBE", "R-11", "3401201236438", "HAKAN HAYDUT"), K(AVP, "07", "YÖNETİM", "R-14", "3405201910553", "İDİL AKEL"), K(AVP, "03", "FİNANS VE MUHASEBE", "R-12", "3401201248986", "MELİKE URAL KAYA"),
            K(AVP, "01", "SATIŞ VE PAZARLAMA", "R-15", "3402200638691", "MUSTAFA ÖZDENİZ"), K(AVP, "06", "KALİTE KONTROL", "R-16", "3426201513369", "OLGUN KÜSKÜ"), K(AVP, "01", "SATIŞ VE PAZARLAMA", "R-17", "3406200701168", "ÖMER KANTAR"),
            K(AVP, "01", "SATIŞ VE PAZARLAMA", "R-19", "3406201114877", "ÖMER YILDIRIM"), K(AVP, "07", "YÖNETİM", "R-18", "3405201216819", "ÖZGE İSEN"), K(AVP, "01", "SATIŞ VE PAZARLAMA", "R-20", "3401200828144", "SERHAT MUTLU"),
            K(AVP, "07", "YÖNETİM", "R-21", "3409200528572", "SERKAN GENÇ"), K(AVP, "07", "YÖNETİM", "R-22", "3409200922846", "SEVGİ KAYA"), K(AVP, "04", "İÇ HİZMETLİ", "R-23", "6701200607087", "YEŞİM KARAKÜTÜK"),
            // ---- Selvi Kimya (5)
            K(SLV, "002", "MUHASEBE", "R-52", "3401201343499", "BEYZA GİZEM ZAZOĞLU"), K(SLV, "003", "YÖNETİM", "R-56", "4102201004905", "BÜŞRA ALİŞARLI"), K(SLV, "003", "YÖNETİM", "R-57", "3432201628245", "DİLANUR SÖNMEZ"),
            K(SLV, "004", "SEVKİYAT", "R016", "3401200344521", "BİROL ŞEKER"), K(SLV, "006", "İÇ HİZMETLİ", "R-55", "3401202417288", "SEDA SARAÇ"),
            // ---- Uras Holding (1)
            K(HLD, "02", "YÖNETİM", "R-04", "3409200639956", "CÜNEYT ARDA CEYLAN"),
            // ---- Uras Kimya (19)
            K(URS, "20", "MUHASEBE", "R-461", "3401200541383", "AYFER ŞAHİN"), K(URS, "21", "TAHSİLAT", "R-508", "3401199221150", "AYŞEGÜL ZENT ULA"), K(URS, "11", "SATIŞ VE PAZARLAMA", "R-417", "3401202219817", "AZAD SHIRINOV"),
            K(URS, "1", "AVUKAT", "1-U-0010", "3409200639956", "CÜNEYT ARDA CEYLAN"), K(URS, "11", "SATIŞ VE PAZARLAMA", "R-591", "3401202508102", "ELMAN ABBASLI"), K(URS, "11", "SATIŞ VE PAZARLAMA", "1-8-123", "3401201842425", "ELMIN JABBAROV"),
            K(URS, "11", "SATIŞ VE PAZARLAMA", "R-489", "3406199714821", "ERDOĞAN POLAT"), K(URS, "9", "İÇ HİZMETLİ", "R-454", "5201013267005", "ERTAN YAVUZ"), K(URS, "8", "YÖNETİM", "R-602", "3401202303005", "JOAO PAULO ARAUJO FERNANDES"),
            K(URS, "13", "ÜRETİM", "R-700", "3401201926971", "KAMRAN CABBAROV"), K(URS, "8", "YÖNETİM", "R-608", "3402201501787", "MEDİNE ROZYKULOVA"), K(URS, "11", "SATIŞ VE PAZARLAMA", "R-590", "3401201936480", "MEHMAN BABAYEV"),
            K(URS, "11", "SATIŞ VE PAZARLAMA", "R-535", "3401202406832", "MURAD HUDAYBERGENOV"), K(URS, "11", "SATIŞ VE PAZARLAMA", "R-385", "3400016104072", "MÜJGAN YETGİN"), K(URS, "11", "SATIŞ VE PAZARLAMA", "R-540", "3401202412450", "RAHMAN BABAZADE"),
            K(URS, "11", "SATIŞ VE PAZARLAMA", "1-8-126", "3401201843194", "RAMIN MUSAYEV"), K(URS, "13", "ÜRETİM", "R-452", "3402199517774", "REFİK POLAT"), K(URS, "11", "SATIŞ VE PAZARLAMA", "R-488", "3401202319368", "TURAL ABBASOV"),
            K(URS, "10", "SATIN ALMA", "R-445", "3401202304548", "VU THI THUY HA"),
            // ---- Alv Filo (1)
            K(FLO, "2", "YÖNETİM", "R-003", "3501199571976", "NÜKHET ÖZAVŞAR")
        };
        /// <summary>Paneldeki ad ↔ bordrodaki ad farkları.</summary>
        private static readonly Dictionary<string, string> TakmaAd = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Devrim / Hale Bisren"] = "HALE BİSREN" };
        /// <summary>Ekim 2026'da işe giren ve kimliği kaynaklarda bulunmayan kişiler: bölüm / çatı bordro departmanından, işe giriş Ekim prim gününden türetildi; görev ve unvan varsayım (kullanıcı kartından düzeltir).</summary>
        private static readonly (string Ad, string Unvan, string Bolum, string Gorev, string Cati, string Giris, string Not)[] Yeniler =
        {
            ("Büşra Alişarlı", "Personel", "İdari İşler", "İdari Personel", "Kurumsal Hizmetler", "2026-10-05", "Ekim 2026 bordrosunda 003 YÖNETİM, 27 prim günü (işe giriş 5 Ekim 2026 varsayıldı); görev ve unvan varsayım, kaynakta yok."),
            ("Dilanur Sönmez", "Personel", "İdari İşler", "İdari Personel", "Kurumsal Hizmetler", "2026-10-05", "Ekim 2026 bordrosunda 003 YÖNETİM, 27 prim günü (işe giriş 5 Ekim 2026 varsayıldı); görev ve unvan varsayım, kaynakta yok."),
            ("Kamran Cabbarov", "Personel", "Üretim", "Üretim Personeli", "Üretim", "2026-10-26", "Ekim 2026 bordrosunda 13 ÜRETİM, 6 prim günü (işe giriş 26 Ekim 2026 varsayıldı).")
        };

        /// <summary>Ad anahtarı: Türkçe harfler ASCII'ye katlanır, küçük harf, yalnız harf ve rakam (bordro büyük harf, panel karışık).</summary>
        public static string Anahtar(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
            {
                char c = ch;
                switch (c) { case 'İ': case 'I': case 'ı': c = 'i'; break; case 'Ş': case 'ş': c = 's'; break; case 'Ğ': case 'ğ': c = 'g'; break; case 'Ü': case 'ü': c = 'u'; break; case 'Ö': case 'ö': c = 'o'; break; case 'Ç': case 'ç': c = 'c'; break; case 'Â': case 'â': c = 'a'; break; case 'Î': case 'î': c = 'i'; break; case 'Û': case 'û': c = 'u'; break; }
                c = char.ToLowerInvariant(c); if (char.IsLetterOrDigit(c)) sb.Append(c);
            }
            return sb.ToString();
        }
        static string SirketAnahtar(string s) => Anahtar(s).Replace("kimya", "").Replace("alvfilo", "alvfilo");
        /// <summary>Kişinin bordro satırı: ad (takma ad dahil) + SGK işvereni; işveren eşleşmezse yalnız ad (tek satırsa).</summary>
        public static BordroKimlik Bul(PmPersonel p)
        {
            var ad = Anahtar(TakmaAd.TryGetValue(p.AdSoyad ?? "", out var t) ? t : p.AdSoyad);
            var adaylar = Satirlar.Where(s => Anahtar(s.Ad) == ad).ToList(); if (adaylar.Count == 0) return null;
            return adaylar.FirstOrDefault(s => SirketAnahtar(s.Sirket) == SirketAnahtar(p.SgkIsvereni)) ?? (adaylar.Count == 1 ? adaylar[0] : null);
        }
        /// <summary>Listeyi resmi bordro kimliğiyle tamamlar (her dönem kurulurken). Sicil / SGK no bordrodan yazılır; departman kodu / açıklaması boşsa panel düzeninden; Yeniler için bölüm / çatı / işe giriş / görev varsayımı. Eşleşmeyen bordrolu kişiler konsola yazılır.</summary>
        public static void Tamamla(List<PmPersonel> liste)
        {
            if (liste == null) return;
            var eslesmedi = new List<string>();
            foreach (var p in liste)
            {
                var b = Bul(p);
                if (b != null)
                {
                    p.SicilNo = b.Sicil; p.SgkNo = b.SgkNo;
                    if (string.IsNullOrWhiteSpace(p.Not) || p.Not.IndexOf("bordro departmanı", StringComparison.OrdinalIgnoreCase) < 0)
                    {   // bordronun şirkete özel departmanı bilgi olarak nota (panel D-düzeni değişmez)
                        string bd = $"Resmi bordro departmanı {b.DepartmanKodu} {b.Departman}.";
                        if ((p.Not ?? "").IndexOf(bd, StringComparison.OrdinalIgnoreCase) < 0) p.Not = ((p.Not ?? "") + " " + bd).Trim();
                    }
                }
                else if (!p.Ozel && !p.SgkDetayYok) eslesmedi.Add($"{p.AdSoyad} ({p.SgkIsvereni})");
                var y = Array.Find(Yeniler, x => Anahtar(x.Ad) == Anahtar(p.AdSoyad));
                if (y.Ad != null)
                {
                    if (string.IsNullOrWhiteSpace(p.Unvan)) p.Unvan = y.Unvan; if (string.IsNullOrWhiteSpace(p.Bolum)) p.Bolum = y.Bolum; if (string.IsNullOrWhiteSpace(p.Gorev)) p.Gorev = y.Gorev; if (string.IsNullOrWhiteSpace(p.CatiDepartman)) p.CatiDepartman = y.Cati;
                    if (p.IseGiris == null && DateTime.TryParse(y.Giris, out var g)) p.IseGiris = g;
                    if ((p.Not ?? "").IndexOf("varsayıldı", StringComparison.OrdinalIgnoreCase) < 0) p.Not = ((p.Not ?? "") + " " + y.Not).Trim();
                }
                if (string.IsNullOrWhiteSpace(p.DepartmanKodu) && !string.IsNullOrWhiteSpace(p.Bolum)) p.DepartmanKodu = PersonelMaliyetOrnek.DepartmanKoduBul(p.Bolum);
                if (string.IsNullOrWhiteSpace(p.DepartmanAciklamasi) && !string.IsNullOrWhiteSpace(p.Bolum)) p.DepartmanAciklamasi = p.Bolum + " Departmanı";
            }
            if (eslesmedi.Count > 0) Console.WriteLine("[Kimlik] resmi bordroda eşleşmeyen bordrolu kişi: " + string.Join(", ", eslesmedi.Distinct()));
        }
    }
}
