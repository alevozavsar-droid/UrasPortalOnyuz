// KASA NAKİT AKIŞI GÖRÜNÜM — "Ödeme Takibim" (/Rapor130; Mali İşler › Finans › Nakit Akışı ve Likidite › Kasa Nakit Akışı Görünüm)
// Amaç: Kasa Nakit Akışı'nı herkes görmez; satın almacılar ve ödeme talebi açan kişiler YALNIZ KENDİ ödemelerinin sürecini izler
// (talep → onay → nakit akışa planlandı → talimat / çek / nakit onayı → ödendi → dekont / makbuz). Kasa / banka bakiyeleri, diğer kişilerin ödemeleri ve toplamlar görünmez.
// Veri: Data/NakitAkisOrnek.cs (satırlar: U_TalepEden ya da U_Satinalmaci = kişi; Ödeme Talebi modülünden gelen satırlar), Data/OdemeTalepOrnek.cs (kişinin talepleri).
// Ekran: Views/Rapor130/Index.cshtml · betik: wwwroot/js/odeme-takibim.js. Görünüm kişisi: giriş yapan kullanıcı (örnekte kişi seçilebilir).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor130Controller : Controller
    {
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        private string Kullanici => NakitAvansOrnek.KisiBul(User?.Identity?.Name)?.AdSoyad ?? (User?.Identity?.Name ?? "Kullanıcı");
        static readonly string[] Sistem = { "Sistem", "Sistem (otomatik)", "Finans", "Muhasebe", "" };
        class Adim { public string Ad; public bool Ok; public string Not; public string Tarih; }

        [HttpGet("")]
        public IActionResult Index(string kisi)
        {
            ViewData["Title"] = "Kasa Nakit Akışı Görünüm"; ViewBag.Kisi = kisi ?? ""; ViewBag.Ben = Kullanici;
            return View("Index");
        }

        /// <summary>Kişinin ödemeleri (talep ettiği / satınalmacısı olduğu nakit akış satırları), talepleri ve kişisel özet; görünüm kişisi listesi.</summary>
        [HttpGet("Veri")]
        public IActionResult Veri(string kisi)
        {
            var bugun = DateTime.Today; var talepler = OdemeTalepOrnek.Listele(null, false);
            var tum = CekAkibetOrnek.Sirketler.SelectMany(s => NakitAkisOrnek.Odemeler(s.DbName)).ToList();
            var kisiler = NakitAkisOrnek.Satinalmacilar.Select(a => new { ad = a, rol = "Satınalmacı" })
                .Concat(talepler.Select(t => new { ad = t.TalepEden, rol = t.Departman })).Concat(tum.Select(m => new { ad = m.U_TalepEden, rol = "Talep Eden" }))
                .Where(x => !string.IsNullOrEmpty(x.ad) && !Sistem.Contains(x.ad)).GroupBy(x => x.ad)
                .Select(g => new { ad = g.Key, rol = g.First().rol, adet = tum.Count(m => m.U_TalepEden == g.Key || m.U_Satinalmaci == g.Key) + talepler.Count(t => t.TalepEden == g.Key) })
                .Where(x => x.adet > 0).OrderByDescending(x => x.adet).ThenBy(x => x.ad, StringComparer.Create(Tr, false)).ToList();
            string ben = Kullanici; string secili = !string.IsNullOrEmpty(kisi) ? kisi : (kisiler.Any(x => x.ad == ben) ? ben : (kisiler.FirstOrDefault()?.ad ?? ben));
            var benimTalepler = talepler.Where(t => t.TalepEden == secili).OrderByDescending(t => t.Tarih).ToList();
            var talepDoc = new HashSet<int>(benimTalepler.Where(t => t.NakitAkisDocEntry != null).Select(t => t.NakitAkisDocEntry.Value));
            bool Benim(NakitAkisViewModel m) => m.U_TalepEden == secili || m.U_Satinalmaci == secili || talepDoc.Contains(m.DocEntry);
            var satirlar = tum.Where(Benim).OrderBy(m => m.U_Durum == "ÖDENDİ" ? 1 : 0).ThenBy(m => m.U_PlanlananTarih).ThenBy(m => m.DocEntry).Select(m => Dto(m, benimTalepler.FirstOrDefault(t => t.NakitAkisDocEntry == m.DocEntry), secili, bugun)).ToList();
            var acik = tum.Where(m => Benim(m) && m.U_Durum != "ÖDENDİ").ToList(); var odenen = tum.Where(m => Benim(m) && m.U_Durum == "ÖDENDİ").ToList();
            bool Gec(NakitAkisViewModel m) => m.U_PlanlananTarih != null && m.U_PlanlananTarih.Value.Date < bugun; bool Hafta(NakitAkisViewModel m) => m.U_PlanlananTarih != null && m.U_PlanlananTarih.Value.Date >= bugun && m.U_PlanlananTarih.Value.Date <= bugun.AddDays(7);
            var ozet = new
            {
                acik = acik.Count, acikTutar = acik.Sum(m => m.U_TutarTL ?? 0), gecikmis = acik.Count(Gec), gecikmisTutar = acik.Where(Gec).Sum(m => m.U_TutarTL ?? 0), buHafta = acik.Count(Hafta), buHaftaTutar = acik.Where(Hafta).Sum(m => m.U_TutarTL ?? 0),
                onayBekleyen = acik.Count(m => m.U_OnayDurumu == "Onay Bekliyor") + benimTalepler.Count(t => t.Durum == "Onay Bekliyor"),
                odenen30 = odenen.Count(m => m.U_OdenmeTarihi != null && m.U_OdenmeTarihi.Value.Date >= bugun.AddDays(-30)), odenen30Tutar = odenen.Where(m => m.U_OdenmeTarihi != null && m.U_OdenmeTarihi.Value.Date >= bugun.AddDays(-30)).Sum(m => m.U_TutarTL ?? 0),
                odenenToplam = odenen.Count, talep = benimTalepler.Count, talepAcik = benimTalepler.Count(t => t.Durum == "Onay Bekliyor" || t.Durum == "Onaylandı")
            };
            return Json(new { success = true, bugun = bugun.ToString("yyyy-MM-dd"), ben, secili, benimMi = secili == ben, kisiler, satirlar, talepler = benimTalepler.Select(OdemeTalepOrnek.Json).ToList(), ozet });
        }

        /// <summary>Satır günlüğü, ekler (dekont / makbuz / ciro bordrosu) ve belgeler (çek / senet akıbeti, imzalı ibraz belgesi) — kişi yalnız kendi satırını açabilir.</summary>
        [HttpGet("Satir")]
        public IActionResult Satir(string dbName, int docEntry, string kisi)
        {
            var m = NakitAkisOrnek.Bul(dbName, docEntry); if (m == null) return Json(new { success = false, message = "Satır bulunamadı." });
            string secili = string.IsNullOrEmpty(kisi) ? Kullanici : kisi; var talep = OdemeTalepOrnek.Listele(null, false).FirstOrDefault(t => t.NakitAkisDocEntry == docEntry);
            if (m.U_TalepEden != secili && m.U_Satinalmaci != secili && talep?.TalepEden != secili) return Json(new { success = false, message = "Bu ödeme sizin talebiniz / sorumluluğunuzda değil; görüntülenemez." });
            NakitAkisOrnek.BelgeleriDoldur(m);
            return Json(new
            {
                success = true, satir = Dto(m, talep, secili, DateTime.Today),
                gunluk = NakitAkisOrnek.Gunluk(dbName, docEntry).Select(g => new { tarih = g.IslemTarihi.ToString("yyyy-MM-dd HH:mm"), g.Kullanici, tip = g.IslemTipi, g.Aciklama }).ToList(),
                ekler = m.DekontListesi.Select(e => new { e.Id, ad = e.DosyaAdi, tur = e.Tur, yol = e.DosyaYolu, tarih = e.Tarih.ToString("yyyy-MM-dd HH:mm"), e.Yukleyen, e.Aciklama }).ToList(),
                belgeler = m.Belgeler.Select(b => new { b.No, b.Belge, vade = b.Vade.ToString("yyyy-MM-dd"), b.Tutar, akibet = b.Akibet, ibrazYeri = b.IbrazYeri, ibrazBelgeNo = b.IbrazBelgeNo, ibrazBelgeYolu = b.IbrazBelgeYolu, b.KimdenGeldi, b.Banka }).ToList(),
                talep = talep == null ? null : OdemeTalepOrnek.Json(talep)
            });
        }

        static object Dto(NakitAkisViewModel m, OdemeTalebi t, string kisi, DateTime bugun)
        {
            bool odendi = m.U_Durum == "ÖDENDİ", cek = m.U_OdemeYontemi == "Çek" || m.U_OdemeYontemi == "Senet", nakit = m.U_OdemeYontemi == "Nakit";
            var a = new List<Adim>();
            a.Add(new Adim { Ad = "Talep", Ok = t != null || !string.IsNullOrEmpty(m.U_TalepEden), Not = t != null ? $"{t.No} · {t.Departman} · {t.TalepEden}" : string.IsNullOrEmpty(m.U_TalepEden) ? "doğrudan nakit akış kaydı" : $"talep eden {m.U_TalepEden}", Tarih = t?.Tarih.ToString("yyyy-MM-dd") ?? m.BelgeTarihi?.ToString("yyyy-MM-dd") });
            bool onayVar = !string.IsNullOrEmpty(m.U_OnayDurumu) || t != null; bool onayOk = string.IsNullOrEmpty(m.U_OnayDurumu) ? (t == null || t.Durum != "Onay Bekliyor") : m.U_OnayDurumu == "Onaylandı";
            a.Add(new Adim { Ad = "Onay", Ok = onayOk, Not = !onayVar ? "onay gerekmez" : !string.IsNullOrEmpty(m.U_OnayDurumu) ? $"{m.U_OnayDurumu}{(string.IsNullOrEmpty(m.U_Onaylayan) ? "" : " · " + m.U_Onaylayan)}" : $"talep {t.Durum}{(string.IsNullOrEmpty(t.Onaylayan) ? "" : " · " + t.Onaylayan)}", Tarih = m.U_OnayTarihi?.ToString("yyyy-MM-dd") ?? t?.OnayTarihi?.ToString("yyyy-MM-dd") });
            a.Add(new Adim { Ad = "Nakit Akışa Planlandı", Ok = m.U_Durum != "Bekliyor" || odendi, Not = $"{m.U_Durum} · planlanan {m.U_PlanlananTarih:dd.MM.yyyy}" + (string.IsNullOrEmpty(m.U_OdemeYontemi) ? "" : " · " + m.U_OdemeYontemi), Tarih = m.U_PlanlananTarih?.ToString("yyyy-MM-dd") });
            bool arac = !string.IsNullOrEmpty(m.U_TalimatNo) || !string.IsNullOrEmpty(m.U_CekDocEntryler) || (nakit && m.U_OnayDurumu == "Onaylandı") || odendi;
            a.Add(new Adim { Ad = cek ? "Çek / Senet" : nakit ? "Nakit Onayı" : "Banka Talimatı", Ok = arac, Not = cek ? (string.IsNullOrEmpty(m.U_CekNolar) ? "belge seçilecek" : m.U_CekNolar + (string.IsNullOrEmpty(m.U_IbrazBelgeNo) ? "" : " · ibraz " + m.U_IbrazBelgeNo)) : nakit ? (!string.IsNullOrEmpty(m.U_MakbuzNo) ? "makbuz " + m.U_MakbuzNo : m.U_OnayDurumu == "Onaylandı" ? "onaylandı · kasadan ödenecek" : "nakit onayı bekliyor") : (string.IsNullOrEmpty(m.U_TalimatNo) ? "talimat hazırlanacak" : "talimat " + m.U_TalimatNo), Tarih = null });
            bool gecikti = !odendi && m.U_PlanlananTarih != null && m.U_PlanlananTarih.Value.Date < bugun;
            a.Add(new Adim { Ad = "Ödendi", Ok = odendi, Not = odendi ? $"{m.U_OdenmeTarihi:dd.MM.yyyy} · {(m.U_OdenenTutar ?? m.U_Tutar ?? 0).ToString("N2", Tr)} {m.U_ParaBirimi}" : gecikti ? $"GECİKTİ · planlanan {m.U_PlanlananTarih:dd.MM.yyyy}" : "bekleniyor", Tarih = m.U_OdenmeTarihi?.ToString("yyyy-MM-dd") });
            bool dekont = m.DekontListesi.Count > 0 || !string.IsNullOrEmpty(m.U_DekontNo) || !string.IsNullOrEmpty(m.U_MakbuzNo) || !string.IsNullOrEmpty(m.U_IbrazBelgeNo);
            a.Add(new Adim { Ad = "Dekont / Makbuz", Ok = dekont, Not = m.DekontListesi.Count > 0 ? $"{m.DekontListesi.Count} ek" : !string.IsNullOrEmpty(m.U_DekontNo) ? "dekont " + m.U_DekontNo : !string.IsNullOrEmpty(m.U_MakbuzNo) ? "makbuz " + m.U_MakbuzNo : !string.IsNullOrEmpty(m.U_IbrazBelgeNo) ? "ibraz belgesi " + m.U_IbrazBelgeNo : "henüz yok", Tarih = null });
            string asama = a.FirstOrDefault(x => !x.Ok)?.Ad ?? "Tamamlandı";
            string rol = m.U_Satinalmaci == kisi && m.U_TalepEden == kisi ? "Talep Eden · Satınalmacı" : m.U_Satinalmaci == kisi ? "Satınalmacı" : m.U_TalepEden == kisi ? "Talep Eden" : t != null ? "Talep Eden" : "İlgili";
            return new
            {
                docEntry = m.DocEntry, dbName = m.U_KaynakFirma, sirket = NakitAkisOrnek.SirketAdi(m.U_KaynakFirma), kurum = m.U_Kurum, urun = m.U_UrunHizmet, belgeTipi = m.U_BagliBelgeTipi, belgeNo = m.U_FaturaSiparisNo, tutar = m.U_Tutar ?? 0, pb = m.U_ParaBirimi == "TRY" ? "TL" : m.U_ParaBirimi, tutarTL = m.U_TutarTL ?? 0,
                planlanan = m.U_PlanlananTarih?.ToString("yyyy-MM-dd"), sonOdeme = m.U_SonOdemeTarihi?.ToString("yyyy-MM-dd"), yontem = m.U_OdemeYontemi, durum = m.U_Durum, onay = m.U_OnayDurumu, talimat = m.U_TalimatNo, cekler = m.U_CekNolar, odenme = m.U_OdenmeTarihi?.ToString("yyyy-MM-dd"), odenen = m.U_OdenenTutar,
                dekont = m.U_DekontNo, makbuz = m.U_MakbuzNo, ibraz = m.U_IbrazBelgeNo, ekAdet = m.DekontListesi.Count, talepEden = m.U_TalepEden, satinalmaci = m.U_Satinalmaci, talepNo = t?.No, talepDurum = t?.Durum, talepLink = t == null ? null : "/OdemeTalebi?no=" + Uri.EscapeDataString(t.No), not = m.U_Notlar,
                adimlar = a.Select(x => new { ad = x.Ad, ok = x.Ok, not = x.Not, tarih = x.Tarih }).ToList(), asama, gecikti, odendi, rol
            };
        }
    }
}
