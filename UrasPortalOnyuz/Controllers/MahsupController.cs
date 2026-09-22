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
    /// Mahsup Modülü veri uçları (ekran: /GrupIci/Mahsup → Views/GrupIci/Mahsup.cshtml, wwwroot/js/mahsup.js; kurallar Data/MahsupOrnek.cs).
    /// Rol demo amaçlı ekrandan seçilir ve her istekte gönderilir (§10 yetki matrisi sunucuda uygulanır).
    /// </summary>
    [Authorize]
    public class MahsupController : Controller
    {
        private static readonly CultureInfo Tr = MahsupOrnek.Tr;
        private NaKisi Ben() => NakitAvansOrnek.KisiBul(User?.Identity?.Name) ?? NakitAvansOrnek.Personel[0];
        private string Kullanici() => Ben().AdSoyad;
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static string Zmn(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm", Tr) : "";
        public class Eylem { public string No { get; set; } = ""; public string Rol { get; set; } public string Not { get; set; } public string Neden { get; set; } public string Senaryo { get; set; } public string EylemAdi { get; set; } }

        [HttpGet]
        public IActionResult Veri()
        {
            var ben = Ben();
            return Json(new
            {
                ben = new { ben.AdSoyad, ben.Sirket, ben.Departman, ben.Rol }, roller = MahsupOrnek.Yetkiler.Keys, yetkiler = MahsupOrnek.Yetkiler, sirketler = MahsupOrnek.Sirketler,
                turler = MahsupOrnek.Turler.Select(t => new { kod = t.Kod, ad = t.Ad, aciklama = t.Aciklama, sonuc = t.Sonuc }), kapsamlar = MahsupOrnek.Kapsamlar, yontemler = MahsupOrnek.EslestirmeYontemleri, nedenKodlari = MahsupOrnek.NedenKodlari, durumlar = MahsupOrnek.Durumlar, kontrolDurumlari = MahsupOrnek.KontrolDurumlari,
                masrafMerkezleri = MahsupOrnek.MasrafMerkezleri, projeler = MahsupOrnek.Projeler, parametreler = MahsupOrnek.Parametreler, parametreEtiketler = MahsupOrnek.ParametreEtiketler,
                cariler = MahsupOrnek.Cariler(null),
                esleme = MahsupOrnek.Esleme.Select(e => new { e.Id, e.Sirket, e.MahsupTuru, e.HesapRolu, e.CariGrubu, e.ParaBirimi, e.Hesap, e.HesapAdi, e.BpMode, gecerlilik = Trh(e.GecerlilikBas) + " →", e.Aktif }),
                islemler = MahsupOrnek.Islemler.Select(Dto)
            });
        }
        /// <summary>Açık Kalemleri Getir (§4.3 / §4.4): sunucu tarafı filtre ve sayfalama; kapalı belgeler opsiyonla.</summary>
        [HttpGet]
        public IActionResult AcikKalemler(string sirket, string karsiSirket, string cari, string tur, string yon, string ara, bool kapali = false, int haric = 0, int sayfa = 1, int boy = 40)
        {
            if (string.IsNullOrEmpty(sirket)) return Json(new { toplam = 0, sayfa = 1, boy, kalemler = new object[0] });
            var l = MahsupOrnek.AcikKalemListesi(sirket, karsiSirket, cari, tur, yon, ara, kapali, haric).ToList();
            return Json(new { toplam = l.Count, sayfa, boy, kalemler = l.Skip((Math.Max(1, sayfa) - 1) * boy).Take(boy) });
        }
        private MhIslem Kur(MhIslemGiris g, YnSonuc s)
        {
            var mevcut = g.Id > 0 ? MahsupOrnek.Bul(g.Id) : null;
            var i = new MhIslem { Id = g.Id, No = mevcut?.No, GrupMahsupNo = mevcut?.GrupMahsupNo ?? (g.Tur == "M06" ? "GM-TASLAK" : null), Sirket = g.Sirket, KarsiSirket = g.Tur == "M06" ? g.KarsiSirket : null, Kapsam = g.Tur == "M06" ? "Grup Şirketleri Arası" : (g.Kapsam ?? "Şirket İçi"), Tur = string.IsNullOrEmpty(g.Tur) ? "M01" : g.Tur, MasrafMerkezi = g.MasrafMerkezi ?? "", Proje = g.Proje ?? "", NedenKodu = g.NedenKodu ?? "", Neden = g.Neden ?? "", Aciklama = g.Aciklama ?? "", EslestirmeYontemi = g.EslestirmeYontemi ?? "Manuel", Versiyon = mevcut?.Versiyon ?? 1 };
            i.Tarih = DateTime.TryParseExact(g.Tarih ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : DateTime.Today; i.KayitTarihi = DateTime.TryParseExact(g.KayitTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var kt) ? kt : i.Tarih;
            i.Kalemler = (g.Kalemler ?? new List<MhKalemGiris>()).Select(k => MahsupOrnek.KalemKur(k, i.Tur, g.Id, s)).Where(k => k != null).GroupBy(k => k.KalemId).Select(x => x.First()).ToList();
            var ilk = i.Kalemler.FirstOrDefault(); i.ParaBirimi = ilk?.ParaBirimi ?? "TRY"; i.Kur = i.ParaBirimi == "TRY" ? 1 : (g.Kur ?? ilk?.BelgeKuru ?? 1); i.KurTarihi = i.ParaBirimi == "TRY" ? (DateTime?)null : (DateTime.TryParseExact(g.KurTarihi ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var kd) ? kd : i.Tarih);
            i.BorcCari = g.BorcCari ?? i.Borclar.FirstOrDefault()?.CariAd; i.AlacakCari = g.AlacakCari ?? i.Alacaklar.FirstOrDefault()?.CariAd;
            return i;
        }
        /// <summary>Canlı önizleme: kalem hesapları, dağılım, özet, validasyon, onay akışı ve simülasyon (kaydetmez).</summary>
        [HttpPost]
        public IActionResult Onizleme([FromBody] MhIslemGiris g)
        {
            if (g == null) return Json(new { success = false, hatalar = new[] { "Geçersiz istek." } }); var s = new YnSonuc(); var i = Kur(g, s);
            MahsupOrnek.DagilimKur(i, g.Dagilimlar);
            var d = MahsupOrnek.Dogrula(i, g.Rol, false); var (sim, tip, ss) = MahsupOrnek.Simule(i);
            return Json(new { success = s.Gecerli && d.Gecerli, hatalar = s.Hatalar.Concat(d.Hatalar), uyarilar = d.Uyarilar.Concat(ss.Uyarilar).Distinct(), simHatalar = ss.Hatalar, kalemler = i.Kalemler.Select(KalemDto), dagilimlar = i.Dagilimlar, ozet = Ozet(i), akis = i.Kalemler.Count > 0 ? MahsupOrnek.OnayAkisi(i).Select(o => new { o.Sira, o.Adim, o.Kisi }) : null, akisNedeni = i.Kalemler.Count > 0 ? MahsupOrnek.OnayNedeni(i) : "", simulasyon = sim.Select(FisDto), simTipi = tip, yevmiye = sim.Any(x => x.Tip == "YEVMIYE" || x.Tip == "MIRROR"), simTarihi = Zmn(DateTime.Now), paraBirimi = i.ParaBirimi, kur = i.Kur });
        }
        /// <summary>Otomatik Eşleştir (§4.6): yönteme göre tutar ve dağılım ön izlemesi (kalıcı değil).</summary>
        [HttpPost]
        public IActionResult Eslestir([FromBody] MhIslemGiris g)
        {
            if (g == null) return Json(new { success = false, message = "Geçersiz istek." }); var s = new YnSonuc(); var i = Kur(g, s); if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar) });
            MahsupOrnek.OtomatikEslestir(i, g.EslestirmeYontemi ?? "En Eski Vade Önce");
            return Json(new { success = true, message = $"{i.EslestirmeYontemi}: {i.Dagilimlar.Count} dağılım · borç {i.BorcToplam.ToString("N2", Tr)} / alacak {i.AlacakToplam.ToString("N2", Tr)} {i.ParaBirimi} (ön izleme; Taslak Kaydet ile kalıcı olur).", kalemler = i.Kalemler.Select(KalemDto), dagilimlar = i.Dagilimlar, ozet = Ozet(i) });
        }
        [HttpPost] public IActionResult Kaydet([FromBody] MhIslemGiris g) { if (g == null) return Json(new { success = false, message = "Geçersiz istek." }); if (!MahsupOrnek.Yetkili(g.Rol, "taslak")) return Json(new { success = false, message = "Bu rol mahsup taslağı oluşturamaz." }); var (i, s) = MahsupOrnek.Kaydet(g, Kullanici()); if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar), uyarilar = s.Uyarilar, islem = i == null ? null : Dto(i) }); return Json(new { success = true, message = $"{i.No} taslak olarak kaydedildi; {i.Kalemler.Count} açık kalem için {i.MahsupTutari.ToString("N2", Tr)} {i.ParaBirimi} rezervasyon yapıldı.", uyarilar = s.Uyarilar, no = i.No, islem = Dto(i) }); }
        [HttpPost]
        public IActionResult OnayaGonder([FromBody] MhIslemGiris g)
        {
            if (g == null) return Json(new { success = false, message = "Geçersiz istek." }); if (!MahsupOrnek.Yetkili(g.Rol, "onayaGonder")) return Json(new { success = false, message = "Bu rol onaya gönderemez." });
            MhIslem i; YnSonuc s;
            if (g.Kalemler != null) { (i, s) = MahsupOrnek.Kaydet(g, Kullanici()); if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar), islem = i == null ? null : Dto(i) }); }
            else { i = MahsupOrnek.Bul(g.Id); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); }
            var r = MahsupOrnek.OnayaGonder(i, g.Rol, Kullanici());
            if (!r.Gecerli) return Json(new { success = false, message = string.Join(" ", r.Hatalar), hatalar = r.Hatalar, uyarilar = r.Uyarilar, islem = Dto(i) });
            return Json(new { success = true, message = $"{i.No} onaya gönderildi: {string.Join(" → ", i.Onaylar.Select(o => o.Adim))} ({MahsupOrnek.OnayNedeni(i)}). Simülasyon v{i.SimulasyonVersiyonu} ({i.SimulasyonTipi}) snapshot'ı saklandı.", uyarilar = r.Uyarilar, no = i.No, islem = Dto(i) });
        }
        private IActionResult Islem(Eylem e, Func<MhIslem, string> f, Func<MhIslem, string> mesaj) { var i = MahsupOrnek.Bul(e.No ?? ""); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); var h = f(i); return Json(new { success = h == null, message = h ?? mesaj(i), islem = Dto(i) }); }
        [HttpPost] public IActionResult Onay([FromBody] Eylem e) => Islem(e, i => MahsupOrnek.Onayla(i, e.EylemAdi, e.Not, e.Rol, i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Kisi ?? Kullanici()), i => e.EylemAdi == "Reddet" ? $"{i.No} reddedildi; taslağa revize edilebilir." : i.Durum == "Onaylandı" ? $"{i.No} onay akışı tamamlandı: ERP muhasebeleştirme / iç mutabakat tetiklenebilir." : $"{i.No}: adım onaylandı; sıradaki {i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Adim}.");
        [HttpPost] public IActionResult GeriCek([FromBody] Eylem e) => Islem(e, i => MahsupOrnek.GeriCek(i, Kullanici()), i => $"{i.No} geri çekildi; onay düştü, taslak (v{i.Versiyon}).");
        [HttpPost] public IActionResult Muhasebelestir([FromBody] Eylem e) => Islem(e, i => MahsupOrnek.Muhasebelestir(i, e.Senaryo, e.Rol, Kullanici()), i => $"{i.No}: ERP {i.ErpDurumu} · {string.Join(" · ", i.Erp.Select(x => x.Sirket + ": " + x.Durum + (x.YevmiyeNo != null ? " " + x.YevmiyeNo : "") + (x.MutabakatNo != null ? " " + x.MutabakatNo : "")))} · kontrol {i.KontrolDurumu}.");
        [HttpPost] public IActionResult TersKayit([FromBody] Eylem e) => Islem(e, i => MahsupOrnek.TersKayit(i, e.Neden, e.Rol, Kullanici()), i => $"{i.No}: ters yevmiye / unreconcile oluşturuldu; açık kalemler yeniden açıldı (kayıt silinmedi).");
        [HttpPost] public IActionResult Iptal([FromBody] Eylem e) => Islem(e, i => MahsupOrnek.Yetkili(e.Rol, "iptal") ? MahsupOrnek.Iptal(i, e.Neden, e.Rol, Kullanici()) : "Bu rol iptal edemez.", i => $"{i.No} iptal edildi; rezervasyonlar serbest bırakıldı.");
        [HttpPost] public IActionResult Kontrol([FromBody] Eylem e) => Islem(e, i => MahsupOrnek.Yetkili(e.Rol, "kontrol") ? MahsupOrnek.Kontrol(i, Kullanici()) : "Kontrolü yalnız Muhasebe / Yönetici / Sistem Admin çalıştırır.", i => $"{i.No} kontrol edildi: {i.KontrolDurumu} ({i.Kontroller.Count(k => k.Sonuc != "Uyumlu")} fark).");
        [HttpPost] public IActionResult FarkKapat([FromBody] Eylem e) => Islem(e, i => MahsupOrnek.FarkKapat(i, e.Not, e.Rol, Kullanici()), i => $"{i.No}: kontrol farkı açıklamayla kapatıldı.");
        [HttpPost] public IActionResult Parametre([FromBody] Dictionary<string, string> d) { lock (MahsupOrnek.Kilit) foreach (var k in MahsupOrnek.Parametreler.Keys.ToList()) if (d != null && d.TryGetValue(k, out var v) && decimal.TryParse((v ?? "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var x)) MahsupOrnek.Parametreler[k] = x; return Json(new { success = true, parametreler = MahsupOrnek.Parametreler }); }

        // ---- DTO ----
        private static object Ozet(MhIslem i) => new { borcToplam = i.BorcToplam, alacakToplam = i.AlacakToplam, fark = i.Fark, mahsupTutari = i.MahsupTutari, mahsupTutariTl = Math.Round(i.MahsupTutari * i.Kur, 2), kurFarki = i.Simulasyon.Count > 0 ? i.KurFarki : MahsupOrnek.KurFarkiHesapla(i), yuvarlamaFarki = i.YuvarlamaFarki, tamKapanan = i.TamKapanan, kismiKapanan = i.KismiKapanan, sonrasiAcik = i.SonrasiAcik, belgeSayisi = i.BelgeSayisi, borcBelge = i.Borclar.Count(), alacakBelge = i.Alacaklar.Count(), muhasebeDurumu = i.MuhasebeDurumu, erpDurumu = i.ErpDurumu, kapamaTipi = i.Kalemler.Count == 0 ? "—" : i.KismiKapanan == 0 ? "Tam" : i.TamKapanan == 0 ? "Kısmi" : "Karma" };
        private static object KalemDto(MhKalem k) => new { k.KalemId, k.Yon, k.Sirket, k.CariKod, k.CariAd, k.CariTip, k.BelgeTuru, k.BelgeNo, k.DocEntry, tarih = Trh(k.Tarih), vade = Trh(k.Vade), k.ParaBirimi, k.BelgeKuru, k.OrijinalTutar, k.Kapanan, k.Rezerve, k.AcikTutar, k.MahsupTutari, k.SonrasiKalan, k.KapamaTipi, tlKarsiligi = Math.Round(k.MahsupTutari * k.BelgeKuru, 2), k.Proje, k.MasrafMerkezi, k.Referans, k.HesapRolu, k.Hesap };
        private static object FisDto(MhFisSatir x) => new { x.Sirket, x.Tip, x.FisNo, x.HesapRolu, x.HesapKodu, x.HesapAdi, x.Cari, x.Borc, x.Alacak, x.ParaBirimi, x.Kur, x.DovizTutar, x.Proje, x.MasrafMerkezi, x.Aciklama, x.Kural, x.Referans };
        private static object Dto(MhIslem i) => new
        {
            i.Id, i.No, i.GrupMahsupNo, tarih = i.Tarih.ToString("yyyy-MM-dd"), tarihTr = Trh(i.Tarih), kayitTarihi = i.KayitTarihi.ToString("yyyy-MM-dd"), kayitTarihiTr = Trh(i.KayitTarihi), i.Sirket, i.KarsiSirket, i.Kapsam, i.Tur, turAd = i.TurAd, i.BorcCari, i.AlacakCari, i.ParaBirimi, i.Kur, kurTarihi = i.KurTarihi?.ToString("yyyy-MM-dd"), i.MasrafMerkezi, i.Proje, i.Departman, i.NedenKodu, i.Neden, i.Aciklama, i.EslestirmeYontemi, i.Durum,
            kalemler = i.Kalemler.Select(KalemDto), dagilimlar = i.Dagilimlar, ozet = Ozet(i),
            simulasyon = i.Simulasyon.Select(FisDto), i.SimulasyonTipi, i.SimulasyonVersiyonu, i.SimulasyonHash, simulasyonTarihi = Zmn(i.SimulasyonTarihi), yevmiye = i.Simulasyon.Any(x => x.Tip == "YEVMIYE" || x.Tip == "MIRROR"),
            onaylar = i.Onaylar.Select(o => new { o.Sira, o.Adim, o.Kisi, o.Durum, tarih = Zmn(o.Tarih), o.Not }), onayNedeni = i.Onaylar.Count > 0 ? MahsupOrnek.OnayNedeni(i) : "", onaylandi = i.Onaylandi,
            audit = i.Audit.OrderByDescending(a => a.Zaman).Select(a => new { zaman = Zmn(a.Zaman), a.Kullanici, a.Alan, a.Eski, a.Yeni, a.Kaynak, a.Gerekce }),
            erp = i.Erp.Select(e => new { e.Sirket, e.Durum, e.YevmiyeNo, e.MutabakatNo, e.Cevap, zaman = Zmn(e.Zaman), e.IdempotencyKey, e.CorrelationId }), erpRef = string.Join(", ", i.Erp.Where(e => e.Durum != "Failed").SelectMany(e => new[] { e.YevmiyeNo, e.MutabakatNo }).Where(x => x != null)),
            gercek = i.Gercek.Select(FisDto), gercekTarihi = Zmn(i.GercekTarihi), erpKalan = i.ErpKalan, i.KontrolDurumu, kontroller = i.Kontroller, acikKalemKontrol = i.AcikKalemKontrol.Select(a => new { a.Belge, a.OncekiAcik, a.BeklenenMahsup, a.BeklenenKalan, a.ErpKalan, a.Sonuc }), kontrolTarihi = Zmn(i.KontrolTarihi), i.FarkKapatma,
            i.RedNedeni, i.IptalNedeni, i.Olusturan, olusturmaZamani = Zmn(i.OlusturmaZamani), i.Guncelleyen, guncellemeZamani = Zmn(i.GuncellemeZamani), i.Versiyon, i.Aktif, i.Duzenlenebilir,
            bekleyenAdim = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Adim, bekleyenKisi = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Kisi
        };
    }
}
