// ============================================================
// ŞİRKET BAZINDA TOPLU PERSONEL ÖDEMESİ → KASA NAKİT AKIŞI (9 Kas 2026, kullanıcı: "doğrudan ödemeyi toplu olarak finansa göndereceğiz; fiili çalışma yerine göre yansıtmalar yapılıyor ya,
//   buna göre firma firma görebilelim: ALV Kimya R ödeme şu kadar, X ödeme şu kadar … buradan da Kasa Nakit Akışı'na toplu olarak gönderebilelim")
//   Konsolide İK Paneli › Ödeme Planı › "Şirket Bazında Toplu Ödeme" kartı: ödenecek satırlar ekranda fiili görev yeri şirketine göre toplanır (Ödenecek / Ödenmeyecek seçimi tarayıcıda olduğundan
//   toplamlar istemciden gelir), şirket başına Kasa Nakit Akışı'na üç satır gönderilir: R (banka) → Havale / EFT, İşlem Tipi R · X (elden, gündelikçi dışı) → Nakit, İşlem Tipi X (nakit onayı: NakitAkisOrnek.NakitOnaycilari)
//   · UX (üretim personeli / gündelikçi elden; 9 Kas 2026 "üretim personeli X ödemesi ayrı gün olabiliyor") → Nakit, İşlem Tipi X, kendi planlanan tarihi (PlanTarihUretim).
//   Aynı dönem / şirket / kanal için TEK satır (NakitAkisViewModel.U_PersonelOdeme = "2026-10|ALV Kimya|R" / "…|UX"): yeniden gönderince ödenmemiş satırın tutarı güncellenir, ödenmişe dokunulmaz,
//   tutarı 0'a düşen kanalın ödenmemiş satırı kaldırılır. "Geri Al" dönemin ödenmemiş satırlarını kaldırır. Kalıcı günlük tipleri "finansGonder" / "finansGeriAl" (PersonelMaliyetKalici).
//   Şirket eşlemesi: Personel Maliyet adları → Kasa Nakit Akışı şirketleri (ALV Kimya → Alv Kimya, Selvi Kimya → Selvi); Alv Filo ve Uras Power Kasa Nakit Akışı'nda tanımlı değil → gönderilmez, ekranda uyarı.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    /// <summary>Şirket satırı: Banka = R (Havale / EFT), Elden = gündelikçi dışı X (Nakit), Uretim = üretim personeli (gündelikçi) elden X (Nakit, ayrı tarih; 9 Kas 2026); KisiUretim = gündelikçi sayısı.</summary>
    public class PfSatir { public string Sirket { get; set; } public int Kisi { get; set; } public int KisiUretim { get; set; } public decimal Banka { get; set; } public decimal Elden { get; set; } public decimal Uretim { get; set; } }
    /// <summary>PlanTarih: R ve X satırlarının planlanan tarihi; PlanTarihUretim: üretim personeli X satırının tarihi (boşsa PlanTarih).</summary>
    public class PfIstek { public string Donem { get; set; } public string PlanTarih { get; set; } public string PlanTarihUretim { get; set; } public List<PfSatir> Satirlar { get; set; } }
    public class PfSonuc { public string Sirket { get; set; } public string Kanal { get; set; } public string Islem { get; set; } public int DocEntry { get; set; } public decimal Tutar { get; set; } public string Durum { get; set; } public string Onay { get; set; } public string Db { get; set; } public string Mesaj { get; set; } }

    public static class PersonelFinansOrnek
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private static readonly Dictionary<string, string> Esleme = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["ALV Kimya"] = "Alv Kimya", ["Alv Kimya"] = "Alv Kimya", ["Selvi Kimya"] = "Selvi", ["Selvi"] = "Selvi", ["Uras Kimya"] = "Uras Kimya", ["Uras Holding"] = "Uras Holding", ["Avrupa Paper"] = "Avrupa Paper", ["URS Makine"] = "URS Makine", ["URS Makina"] = "URS Makine", ["Daf Kimya"] = "Daf Kimya" };
        /// <summary>Personel Maliyet şirket adının Kasa Nakit Akışı şirketi (CekAkibetOrnek.Sirketler); tanımlı değilse null (Alv Filo, Uras Power).</summary>
        public static string KasaSirketi(string sirket) { if (string.IsNullOrWhiteSpace(sirket)) return null; var ad = Esleme.TryGetValue(sirket.Trim(), out var e) ? e : sirket.Trim(); return CekAkibetOrnek.Sirketler.Any(s => s.Ad == ad) ? ad : null; }
        public static string Anahtar(string donem, string sirket, string kanal) => $"{donem}|{sirket}|{kanal}";

        /// <summary>Toplu gönderim: her şirket için R (banka, Havale / EFT) ve X (elden, Nakit) satırı yazar / günceller; tutarı 0 olan kanalın ödenmemiş satırını kaldırır. Hata metni döner (null = başarılı).</summary>
        public static string Gonder(PfIstek d, string kullanici, out List<PfSonuc> sonuc)
        {
            sonuc = new List<PfSonuc>(); if (d == null || d.Satirlar == null || d.Satirlar.Count == 0) return "Gönderilecek satır yok.";
            if (string.IsNullOrWhiteSpace(d.Donem) || d.Donem.Length != 7) return "Dönem AAAA-AA biçiminde olmalıdır.";
            DateTime plan = DateTime.Today;
            if (!string.IsNullOrWhiteSpace(d.PlanTarih) && !DateTime.TryParseExact(d.PlanTarih.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out plan)) return "Planlanan ödeme tarihi geçersiz (yyyy-aa-gg).";
            DateTime planUretim = plan;   // üretim personeli (gündelikçi) elden ödemesi ayrı günde olabilir (9 Kas 2026)
            if (!string.IsNullOrWhiteSpace(d.PlanTarihUretim) && !DateTime.TryParseExact(d.PlanTarihUretim.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out planUretim)) return "Üretim personeli ödeme tarihi geçersiz (yyyy-aa-gg).";
            string donemAd = PersonelMaliyetOrnek.DonemAd(d.Donem);
            foreach (var s in d.Satirlar)
            {
                if (s == null || string.IsNullOrWhiteSpace(s.Sirket)) continue;
                var kasa = KasaSirketi(s.Sirket);
                if (kasa == null) { if (s.Banka > 0 || s.Elden > 0 || s.Uretim > 0) sonuc.Add(new PfSonuc { Sirket = s.Sirket, Kanal = "", Islem = "gönderilmedi", Mesaj = "Kasa Nakit Akışı'nda şirket tanımlı değil" }); continue; }
                var db = CekAkibetOrnek.DbName(kasa);
                // kanal: R banka (Havale / EFT) · X elden (Nakit) · UX üretim personeli elden (Nakit, kendi planlanan tarihi); İşlem Tipi R / X
                foreach (var (kanal, tutar, yontem, tarih, kisi) in new[] { ("R", s.Banka, "Havale / EFT", plan, s.Kisi - s.KisiUretim), ("X", s.Elden, "Nakit", plan, s.Kisi - s.KisiUretim), ("UX", s.Uretim, "Nakit", planUretim, s.KisiUretim) })
                {
                    var anahtar = Anahtar(d.Donem, s.Sirket, kanal);
                    if (tutar <= 0) { var k = NakitAkisOrnek.PersonelOdemesiKaldir(anahtar, kullanici); if (k != null) sonuc.Add(new PfSonuc { Sirket = s.Sirket, Kanal = kanal, Islem = "kaldırıldı", DocEntry = k.DocEntry, Db = db, Mesaj = "tutar 0; ödenmemiş satır kaldırıldı" }); continue; }
                    int n = kanal == "R" ? s.Kisi : Math.Max(0, kisi);   // R: bordrolu gündelikçinin R'si de bankadan → tüm kişiler; X: gündelikçi dışı; UX: gündelikçiler
                    string kurum = kanal == "R" ? $"Personel Maaş Ödemesi · {donemAd} · Banka (R)" : kanal == "X" ? $"Personel Maaş Ödemesi · {donemAd} · Elden (X)" : $"Üretim Personeli Ödemesi · {donemAd} · Elden (X)";
                    string urun = kanal == "R" ? $"{n} personel · R-Net + ek ödemeler − kesintiler, bankaya · fiili görev yeri {s.Sirket} (Konsolide İK Paneli › Ödeme Planı)"
                        : kanal == "X" ? $"{n} personel · X-Net + ek ödemeler − kesintiler, elden · fiili görev yeri {s.Sirket} (Konsolide İK Paneli › Ödeme Planı)"
                        : $"{n} gündelikçi üretim personeli · günlük ücret × gün (SGK'lıda bordro Net-R mahsubundan kalan fark + kira), elden · fiili görev yeri {s.Sirket} (Konsolide İK Paneli › Üretim Personeli / Ödeme Planı)";
                    var (m, islem) = NakitAkisOrnek.PersonelOdemesiYaz(db, anahtar, kurum, urun, yontem, kanal == "R" ? "R" : "X", BordroParametre.Yuvarla(tutar), tarih, $"Konsolide İK Paneli · {donemAd} {(kanal == "UX" ? "üretim personeli (gündelikçi) elden ödemesi" : "toplu personel ödemesi")} · {n} kişi · {s.Sirket}", kullanici);
                    sonuc.Add(new PfSonuc { Sirket = s.Sirket, Kanal = kanal, Islem = islem, DocEntry = m.DocEntry, Tutar = m.U_Tutar ?? 0, Durum = m.U_Durum, Onay = m.U_OnayDurumu, Db = db, Mesaj = islem == "ödenmiş" ? "satır ödenmiş; tutar değiştirilmedi" : "" });
                }
            }
            return null;
        }
        /// <summary>Dönemin ödenmemiş toplu personel ödemesi satırlarını Kasa Nakit Akışı'ndan kaldırır (yanlış gönderim); ödenmişler kalır. Kaldırılan sayı döner.</summary>
        public static int GeriAl(string donem, string kullanici)
        {
            int n = 0; foreach (var m in NakitAkisOrnek.PersonelSatirlari((donem ?? "") + "|")) if (NakitAkisOrnek.PersonelOdemesiKaldir(m.U_PersonelOdeme, kullanici) != null) n++;
            return n;
        }
        /// <summary>Dönemin Kasa Nakit Akışı'ndaki toplu personel ödemesi satırları (ekran: şirket / kanal durumu).</summary>
        public static object Durum(string donem) => NakitAkisOrnek.PersonelSatirlari((donem ?? "") + "|").Select(m =>
        {
            var p = (m.U_PersonelOdeme ?? "").Split('|');
            return new { anahtar = m.U_PersonelOdeme, sirket = p.Length > 1 ? p[1] : "", kanal = p.Length > 2 ? p[2] : "", m.DocEntry, db = m.U_KaynakFirma, kasaSirket = NakitAkisOrnek.SirketAdi(m.U_KaynakFirma), tutar = m.U_Tutar ?? 0, durum = m.U_Durum, onay = m.U_OnayDurumu ?? "", yontem = m.U_OdemeYontemi, plan = m.U_PlanlananTarih?.ToString("yyyy-MM-dd"), odenme = m.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), odenen = m.U_OdenenTutar };
        }).ToList();
        public static string Ozet(List<PfSonuc> s) => $"{s.Count(x => x.Islem == "eklendi")} satır eklendi · {s.Count(x => x.Islem == "güncellendi")} güncellendi · {s.Count(x => x.Islem == "kaldırıldı")} kaldırıldı · {s.Count(x => x.Islem == "ödenmiş")} ödenmiş (değişmedi) · {s.Count(x => x.Islem == "gönderilmedi")} gönderilmedi" + (s.Any(x => x.Islem == "gönderilmedi") ? $" ({string.Join(", ", s.Where(x => x.Islem == "gönderilmedi").Select(x => x.Sirket))}: Kasa Nakit Akışı'nda şirket yok)" : "") + ".";
    }
}
