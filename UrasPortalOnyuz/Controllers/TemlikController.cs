using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Temlik Modülü veri uçları (ekran: /GrupIci/Temlik → Views/GrupIci/Temlik.cshtml, wwwroot/js/temlik.js; kurallar Data/TemlikOrnek.cs).
    /// Rol demo amaçlı ekrandan seçilir ve her istekte gönderilir (§14 yetki matrisi sunucuda uygulanır).
    /// </summary>
    [Authorize]
    public class TemlikController : Controller
    {
        private static readonly CultureInfo Tr = TemlikOrnek.Tr;
        private NaKisi Ben() => NakitAvansOrnek.KisiBul(User?.Identity?.Name) ?? NakitAvansOrnek.Personel[0];
        private string Kullanici() => Ben().AdSoyad;
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static string Zmn(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm", Tr) : "";
        public class Eylem { public string No { get; set; } = ""; public string Rol { get; set; } public string Not { get; set; } public string Neden { get; set; } public string Senaryo { get; set; } public string EylemAdi { get; set; } public int Sira { get; set; } public string Yontem { get; set; } public string Dosya { get; set; } public bool Docx { get; set; } public string Gerekce { get; set; } }

        [HttpGet]
        public IActionResult Veri()
        {
            var ben = Ben();
            return Json(new
            {
                ben = new { ben.AdSoyad, ben.Sirket, ben.Departman, ben.Rol },
                roller = TemlikOrnek.Yetkiler.Keys, yetkiler = TemlikOrnek.Yetkiler, sirketler = TemlikOrnek.Sirketler, tarafTipleri = TemlikOrnek.TarafTipleri,
                taraflar = TemlikOrnek.Taraflar.Select(t => new { t.Ad, t.Tip, t.Unvan, t.Vkn, t.VergiDairesi, t.Mersis, t.Adres, t.Yetkili, t.YetkiliUnvan, eksik = string.IsNullOrEmpty(t.Vkn) || string.IsNullOrEmpty(t.Adres) || string.IsNullOrEmpty(t.Yetkili) }),
                turler = TemlikOrnek.Turler, kaynakBelgeTurleri = TemlikOrnek.KaynakBelgeTurleri, nedenKodlari = TemlikOrnek.NedenKodlari, durumlar = TemlikOrnek.Durumlar, kontrolDurumlari = TemlikOrnek.KontrolDurumlari,
                masrafMerkezleri = TemlikOrnek.MasrafMerkezleri, projeler = TemlikOrnek.Projeler, minTaraf = TemlikOrnek.MinTaraf, maxTaraf = TemlikOrnek.MaxTaraf, sablonVersiyonu = TemlikOrnek.SablonVersiyonu,
                parametreler = TemlikOrnek.Parametreler, parametreEtiketler = TemlikOrnek.ParametreEtiketler,
                esleme = TemlikOrnek.Esleme.Select(e => new { e.Id, e.Sirket, e.Yon, e.HesapRolu, e.TarafTipi, e.KaynakBelgeTuru, e.Hesap, e.HesapAdi, e.BpMode, gecerlilik = Trh(e.GecerlilikBas) + " →", e.Aktif }),
                islemler = TemlikOrnek.Islemler.Select(Dto)
            });
        }

        [HttpGet]
        public IActionResult KaynakAlacaklar(string kaynakSirket, string ara, int haric = 0, int sayfa = 1, int boy = 10)
        {
            var l = TemlikOrnek.KaynakListesi(kaynakSirket, ara, haric).ToList();
            return Json(new { toplam = l.Count, sayfa, boy, kaynaklar = l.Skip((Math.Max(1, sayfa) - 1) * boy).Take(boy) });
        }

        /// <summary>Canlı önizleme: zincir / step hesabı, validasyon, onay akışı, simülasyon ve konsolide görünüm (kaydetmez).</summary>
        [HttpPost]
        public IActionResult Onizleme([FromBody] TmlIslemGiris g)
        {
            if (g == null) return Json(new { success = false, hatalar = new[] { "Geçersiz istek." } });
            var s = new YnSonuc(); var mevcut = g.Id > 0 ? TemlikOrnek.Bul(g.Id) : null;
            var i = new TmlIslem { Id = g.Id, No = mevcut?.No, ZincirNo = mevcut?.ZincirNo, KaynakSirket = g.KaynakSirket, Tur = string.IsNullOrEmpty(g.Tur) ? "Alacak Temliki" : g.Tur, KaynakBelgeTuru = string.IsNullOrEmpty(g.KaynakBelgeTuru) ? "Fatura" : g.KaynakBelgeTuru, NedenKodu = g.NedenKodu ?? "", Neden = g.Neden ?? "", Aciklama = g.Aciklama ?? "", IstisnaGerekce = g.Gerekce ?? "", Versiyon = mevcut?.Versiyon ?? 1 };
            i.Tarih = DateTime.TryParseExact(g.Tarih ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : DateTime.Today;
            i.EtkinlikTarihi = DateTime.TryParseExact(g.EtkinlikTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var et) ? et : i.Tarih;
            i.Kaynaklar = (g.Kaynaklar ?? new List<TmlKaynakGiris>()).Select(k => { var ky = TemlikOrnek.KaynakBul(k.KaynakId); var kl = ky?.Kalemler.FirstOrDefault(x => x.Sira == k.Sira); if (kl == null) { s.Hatalar.Add($"Kaynak satır bulunamadı ({k.KaynakId}/{k.Sira})."); return null; } return new TmlKaynakSatir { KaynakId = ky.Id, BelgeNo = ky.BelgeNo, Sira = kl.Sira, KalemAd = kl.Ad, Nominal = kl.Nominal, Kapanan = kl.Kapanan, OncedenTemlik = TemlikOrnek.OncedenTemlik(ky.Id, kl.Sira, g.Id), BuIslem = Math.Round(k.BuIslem, 2) }; }).Where(k => k != null).ToList();
            var ilk = i.Kaynaklar.Count > 0 ? TemlikOrnek.KaynakBul(i.Kaynaklar[0].KaynakId) : null;
            i.AsilBorclu = string.IsNullOrEmpty(g.AsilBorclu) ? (ilk?.AsilBorclu ?? "") : g.AsilBorclu; if (string.IsNullOrEmpty(i.KaynakSirket)) i.KaynakSirket = ilk?.KaynakSirket;
            i.ParaBirimi = ilk?.ParaBirimi ?? "TRY"; i.Kur = i.ParaBirimi == "TRY" ? 1 : (g.Kur ?? ilk?.Kur ?? 1); i.KurTarihi = i.ParaBirimi == "TRY" ? (DateTime?)null : (DateTime.TryParseExact(g.KurTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var kt) ? kt : i.Tarih);
            i.Taraflar = (g.Taraflar ?? new List<TmlTarafGiris>()).Select((tg, n) => new TmlTaraf { Sira = n + 1, Ad = (tg.Ad ?? "").Trim(), Tip = string.IsNullOrEmpty(tg.Tip) ? TemlikOrnek.TarafTipi(tg.Ad) : tg.Tip, ImzaGerekli = tg.ImzaGerekli }).ToList();
            TemlikOrnek.ZinciriKur(i, g.Stepler);
            var d = TemlikOrnek.Dogrula(i, g.Rol, false); var (sim, ss) = TemlikOrnek.Simule(i);
            return Json(new
            {
                success = s.Gecerli && d.Gecerli, hatalar = s.Hatalar.Concat(d.Hatalar), uyarilar = d.Uyarilar.Concat(ss.Uyarilar).Distinct(), simHatalar = ss.Hatalar,
                taraflar = i.Taraflar.Select(TarafDto), stepler = i.Stepler.Select(StepDto), kaynaklar = i.Kaynaklar.Select(KaynakDto), ozet = Ozet(i),
                akis = i.Taraflar.Count >= 2 && i.Kaynaklar.Count > 0 ? TemlikOrnek.OnayAkisi(i).Select(o => new { o.Sira, o.Adim, o.Kisi }) : null, akisNedeni = i.Kaynaklar.Count > 0 ? TemlikOrnek.OnayNedeni(i) : "",
                simulasyon = sim.Select(FisDto), konsolide = TemlikOrnek.Konsolide(i), simTarihi = Zmn(DateTime.Now), paraBirimi = i.ParaBirimi, kur = i.Kur
            });
        }

        [HttpPost] public IActionResult Kaydet([FromBody] TmlIslemGiris g) { if (g == null) return Json(new { success = false, message = "Geçersiz istek." }); if (!TemlikOrnek.Yetkili(g.Rol, "taslak")) return Json(new { success = false, message = "Bu rol temlik taslağı oluşturamaz (Hukuk yalnız görüntüler)." }); var (i, s) = TemlikOrnek.Kaydet(g, Kullanici()); if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar), uyarilar = s.Uyarilar, islem = i == null ? null : Dto(i) }); return Json(new { success = true, message = $"{i.No} taslak olarak kaydedildi; {i.Kaynaklar.Count} kaynak satırı için {i.BuTemlikTutari.ToString("N2", Tr)} {i.ParaBirimi} rezervasyon yapıldı.", uyarilar = s.Uyarilar, no = i.No, islem = Dto(i) }); }
        [HttpPost]
        public IActionResult OnayaGonder([FromBody] TmlIslemGiris g)
        {
            if (g == null) return Json(new { success = false, message = "Geçersiz istek." }); if (!TemlikOrnek.Yetkili(g.Rol, "onayaGonder")) return Json(new { success = false, message = "Bu rol onaya gönderemez." });
            TmlIslem i; YnSonuc s;
            if (g.Taraflar != null) { (i, s) = TemlikOrnek.Kaydet(g, Kullanici()); if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar), islem = i == null ? null : Dto(i) }); }
            else { i = TemlikOrnek.Bul(g.Id); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); }
            var r = TemlikOrnek.OnayaGonder(i, g.Rol, Kullanici());
            if (!r.Gecerli) return Json(new { success = false, message = string.Join(" ", r.Hatalar), hatalar = r.Hatalar, uyarilar = r.Uyarilar, islem = Dto(i) });
            return Json(new { success = true, message = $"{i.No} onaya gönderildi: {string.Join(" → ", i.Onaylar.Select(o => o.Adim))} ({TemlikOrnek.OnayNedeni(i)}). Simülasyon v{i.SimulasyonVersiyonu} snapshot'ı saklandı (hash {i.SimulasyonHash}).", uyarilar = r.Uyarilar, no = i.No, islem = Dto(i) });
        }
        private IActionResult Islem(Eylem e, Func<TmlIslem, string> f, Func<TmlIslem, string> mesaj) { var i = TemlikOrnek.Bul(e.No ?? ""); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); var h = f(i); return Json(new { success = h == null, message = h ?? mesaj(i), islem = Dto(i) }); }
        [HttpPost] public IActionResult Onay([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.Onayla(i, e.EylemAdi, e.Not, e.Rol, i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Kisi ?? Kullanici()), i => e.EylemAdi == "Reddet" ? $"{i.No} reddedildi; taslağa revize edilebilir." : i.Durum == "Onaylandı" ? $"{i.No} onay akışı tamamlandı: Temlik Belgesi oluşturulabilir." : $"{i.No}: adım onaylandı; sıradaki {i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Adim}.");
        [HttpPost] public IActionResult GeriCek([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.GeriCek(i, e.Rol, Kullanici()), i => $"{i.No} geri çekildi; taslak (v{i.Versiyon}).");
        [HttpPost] public IActionResult BelgeOlustur([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.BelgeOlustur(i, e.Rol, Kullanici(), e.Docx).Hata, i => { var b = i.AktifBelge; return $"Temlik Belgesi üretildi: {b.BelgeNo} v{b.Versiyon} · {b.Imzalar.Count} dinamik imza bloğu · hash {b.Hash} · {b.SablonVersiyonu}{(b.Docx ? " · DOCX taslak da üretildi" : "")}."; });
        [HttpPost] public IActionResult ImzayaGonder([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.ImzayaGonder(i, e.Rol, Kullanici()), i => $"{i.AktifBelge.BelgeNo} imzaya gönderildi: {i.AktifBelge.Imzalar.Count} taraf / imzacıya e-imza talebi iletildi.");
        [HttpPost] public IActionResult Imzala([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.Imzala(i, e.Sira, e.EylemAdi, e.Yontem, e.Not, Kullanici()), i => $"İmza durumu {i.ImzaDurumu} · {i.BelgeDurumu}.");
        [HttpPost] public IActionResult BelgeYukle([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.ImzaliBelgeYukle(i, e.Dosya, e.Rol, Kullanici()), i => $"Islak imzalı nüsha {i.AktifBelge.YuklenenDosya} yüklendi; dosya hash {i.AktifBelge.DosyaHash}; imza {i.ImzaDurumu}.");
        [HttpPost] public IActionResult BelgeGecersiz([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.BelgeGecersizKil(i, e.Neden, e.Rol, Kullanici()), i => "Belge geçersiz kılındı; yeni versiyon üretilmeli.");
        [HttpPost] public IActionResult Bildirim([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.BildirimOlustur(i, e.Rol, Kullanici()), i => $"Asıl borçlu {i.AsilBorclu} için temlik bildirimi üretildi.");
        [HttpPost] public IActionResult Muhasebelestir([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.Muhasebelestir(i, e.Senaryo, e.Rol, Kullanici(), e.Gerekce), i => $"{i.No}: ERP {i.ErpDurumu} · {i.Stepler.Count(s => s.ErpDurum == "Posted")}/{i.StepSayisi} step muhasebeleşti · kontrol {i.KontrolDurumu}.");
        [HttpPost] public IActionResult TersKayit([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.TersKayit(i, e.Neden, e.Rol, Kullanici()), i => $"{i.No}: ters kayıt son stepten ilk stepe oluşturuldu; zincir kapatıldı.");
        [HttpPost] public IActionResult Iptal([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.Yetkili(e.Rol, "iptal") ? TemlikOrnek.Iptal(i, e.Neden, e.Rol, Kullanici()) : "Bu rol iptal edemez.", i => $"{i.No} iptal edildi; rezervasyon serbest bırakıldı.");
        [HttpPost] public IActionResult Kontrol([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.Yetkili(e.Rol, "kontrol") ? TemlikOrnek.Kontrol(i, Kullanici()) : "Kontrolü yalnız Muhasebe / Yönetici çalıştırır.", i => $"{i.No} kontrol edildi: {i.KontrolDurumu} ({i.Kontroller.Count(k => k.Sonuc != "Uyumlu")} fark).");
        [HttpPost] public IActionResult FarkKapat([FromBody] Eylem e) => Islem(e, i => TemlikOrnek.FarkKapat(i, e.Not, e.Rol, Kullanici()), i => $"{i.No}: kontrol farkı açıklamayla kapatıldı.");
        [HttpPost] public IActionResult Parametre([FromBody] Dictionary<string, string> d) { lock (TemlikOrnek.Kilit) foreach (var k in TemlikOrnek.Parametreler.Keys.ToList()) if (d != null && d.TryGetValue(k, out var v) && decimal.TryParse((v ?? "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var x)) TemlikOrnek.Parametreler[k] = x; return Json(new { success = true, parametreler = TemlikOrnek.Parametreler }); }

        // ---- DTO ----
        private static object Ozet(TmlIslem i) => new { kaynakToplam = i.KaynakToplam, oncedenTemlik = i.OncedenTemlik, edilebilir = i.Edilebilir, buTemlik = i.BuTemlikTutari, buTemlikTl = Math.Round(i.BuTemlikTutari * i.Kur, 2), nihaiTutar = i.NihaiTutar, araTarafKalani = i.AraTarafKalani, tarafSayisi = i.TarafSayisi, stepSayisi = i.StepSayisi, ilkEden = i.IlkEden, nihaiAlan = i.NihaiAlan, imzaDurumu = i.ImzaDurumu, belgeDurumu = i.BelgeDurumu, muhasebeDurumu = i.MuhasebeDurumu, erpDurumu = i.ErpDurumu, posted = i.Stepler.Count(s => s.ErpDurum == "Posted") };
        private static object TarafDto(TmlTaraf t) { var m = TemlikOrnek.Master(t.Ad); return new { t.Sira, t.Ad, t.Tip, t.Rol, t.Giren, t.Cikan, t.Kalan, t.ImzaGerekli, t.GrupSirketi, unvan = m?.Unvan, vkn = m?.Vkn, vergiDairesi = m?.VergiDairesi, mersis = m?.Mersis, adres = m?.Adres, yetkili = m?.Yetkili, yetkiliUnvan = m?.YetkiliUnvan, masterEksik = m == null || string.IsNullOrEmpty(m.Vkn) || string.IsNullOrEmpty(m.Adres) }; }
        private static object StepDto(TmlStep s) => new { s.Sira, s.Eden, s.Alan, s.Tutar, s.Kur, s.TutarTl, etkinlikTarihi = s.EtkinlikTarihi.ToString("yyyy-MM-dd"), etkinlikTarihiTr = Trh(s.EtkinlikTarihi), s.Durum, s.ErpDurum, s.ErpRef, s.ErpCevap, erpZamani = Zmn(s.ErpZamani), s.IdempotencyKey, s.CorrelationId };
        private static object KaynakDto(TmlKaynakSatir k) { var ky = TemlikOrnek.KaynakBul(k.KaynakId); return new { k.KaynakId, k.BelgeNo, k.Sira, k.KalemAd, k.Nominal, k.Kapanan, k.OncedenTemlik, k.Edilebilir, k.BuIslem, k.SonrasiKalan, belgeTuru = ky?.BelgeTuru, tarih = Trh(ky?.Tarih), vade = Trh(ky?.Vade), asilBorclu = ky?.AsilBorclu, alacakSahibi = ky?.AlacakSahibi, paraBirimi = ky?.ParaBirimi, kur = ky?.Kur, referans = ky?.Referans, kaynakSirket = ky?.KaynakSirket }; }
        private static object FisDto(TmlFisSatir x) => new { x.Sirket, x.Step, x.Erp, x.FisNo, x.Yon, x.HesapRolu, x.HesapKodu, x.HesapAdi, x.Borc, x.Alacak, x.ParaBirimi, x.Kur, x.DovizTutar, x.KarsiTaraf, x.KaynakRef, x.Aciklama, x.Kural, x.Referans };
        private static object BelgeDto(TmlBelge b) => new { b.Versiyon, b.BelgeNo, olusturmaZamani = Zmn(b.OlusturmaZamani), b.Olusturan, b.SablonVersiyonu, b.Hash, b.Durum, imzalar = b.Imzalar.Select(i => new { i.Sira, i.Taraf, i.Rol, i.Imzaci, i.Unvan, i.Durum, tarih = Zmn(i.Tarih), i.Yontem, i.Zorunlu }), imzayaGonderim = Zmn(b.ImzayaGonderim), b.YuklenenDosya, b.DosyaHash, b.GecersizNedeni, b.BorcluBildirimi, bildirimTarihi = Zmn(b.BildirimTarihi), b.Docx, imzaDurumu = $"{b.Imzalar.Count(x => x.Zorunlu && x.Durum == "İmzalandı")}/{b.Imzalar.Count(x => x.Zorunlu)}" };
        private static object Dto(TmlIslem i) => new
        {
            i.Id, i.No, i.ZincirNo, tarih = i.Tarih.ToString("yyyy-MM-dd"), tarihTr = Trh(i.Tarih), etkinlikTarihi = i.EtkinlikTarihi.ToString("yyyy-MM-dd"), etkinlikTarihiTr = Trh(i.EtkinlikTarihi), i.KaynakSirket, i.AsilBorclu, i.Tur, i.KaynakBelgeTuru, i.ParaBirimi, i.Kur, kurTarihi = i.KurTarihi?.ToString("yyyy-MM-dd"), i.NedenKodu, i.Neden, i.Proje, i.MasrafMerkezi, i.Aciklama, i.Durum,
            kaynaklar = i.Kaynaklar.Select(KaynakDto), taraflar = i.Taraflar.Select(TarafDto), stepler = i.Stepler.Select(StepDto), ozet = Ozet(i),
            simulasyon = i.Simulasyon.Select(FisDto), simulasyonTarihi = Zmn(i.SimulasyonTarihi), i.SimulasyonVersiyonu, i.SimulasyonHash, konsolide = TemlikOrnek.Konsolide(i),
            belgeler = i.Belgeler.Select(BelgeDto), aktifBelge = i.AktifBelge == null ? null : BelgeDto(i.AktifBelge), tamImzali = i.TamImzali,
            onaylar = i.Onaylar.Select(o => new { o.Sira, o.Adim, o.Kisi, o.Durum, tarih = Zmn(o.Tarih), o.Not }), onayNedeni = i.Onaylar.Count > 0 ? TemlikOrnek.OnayNedeni(i) : "", onaylandi = i.Onaylandi,
            audit = i.Audit.OrderByDescending(a => a.Zaman).Select(a => new { zaman = Zmn(a.Zaman), a.Kullanici, a.Alan, a.Eski, a.Yeni, a.Kaynak, a.Gerekce }),
            gercek = i.Gercek.Select(FisDto), gercekTarihi = Zmn(i.GercekTarihi), i.KontrolDurumu, kontroller = i.Kontroller, kontrolTarihi = Zmn(i.KontrolTarihi), i.FarkKapatma,
            i.RedNedeni, i.IptalNedeni, i.IstisnaGerekce, i.Olusturan, olusturmaZamani = Zmn(i.OlusturmaZamani), i.Guncelleyen, guncellemeZamani = Zmn(i.GuncellemeZamani), i.Versiyon, i.Aktif, i.Duzenlenebilir,
            bekleyenAdim = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Adim, bekleyenKisi = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Kisi
        };
    }
}
