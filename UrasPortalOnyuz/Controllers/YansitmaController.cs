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
    /// Yansıtma Modülü veri uçları (ekran: /GrupIci/Yansitma → Views/GrupIci/Yansitma.cshtml, wwwroot/js/yansitma.js; kurallar Data/YansitmaOrnek.cs).
    /// Rol demo amaçlı ekrandan seçilir ve her istekte gönderilir (§10 yetki matrisi sunucuda uygulanır).
    /// </summary>
    [Authorize]
    public class YansitmaController : Controller
    {
        private static readonly CultureInfo Tr = YansitmaOrnek.Tr;
        private NaKisi Ben() => NakitAvansOrnek.KisiBul(User?.Identity?.Name) ?? NakitAvansOrnek.Personel[0];
        private string Kullanici() => Ben().AdSoyad;
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static string Zmn(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm", Tr) : "";

        public class Eylem { public string No { get; set; } = ""; public string Rol { get; set; } public string Not { get; set; } public string Neden { get; set; } public string Senaryo { get; set; } public string EylemAdi { get; set; } }

        // ---------------- veri ----------------
        [HttpGet]
        public IActionResult Veri()
        {
            var ben = Ben();
            return Json(new
            {
                ben = new { ben.AdSoyad, ben.Sirket, ben.Departman, ben.Rol, ben.MasrafMerkezi },
                roller = YansitmaOrnek.Yetkiler.Keys, yetkiler = YansitmaOrnek.Yetkiler,
                sirketler = YansitmaOrnek.Sirketler, turler = YansitmaOrnek.Turler, kaynakBelgeTurleri = YansitmaOrnek.KaynakBelgeTurleri, paraBirimleri = YansitmaOrnek.ParaBirimleri,
                masrafMerkezleri = YansitmaOrnek.MasrafMerkezleri, projeler = YansitmaOrnek.Projeler, durumlar = YansitmaOrnek.Durumlar, kontrolDurumlari = YansitmaOrnek.KontrolDurumlari,
                parametreler = YansitmaOrnek.Parametreler, parametreEtiketler = YansitmaOrnek.ParametreEtiketler,
                esleme = YansitmaOrnek.Esleme.Select(e => new { e.Id, e.Sirket, e.YansitmaTuru, e.Kategori, e.GiderHesap, e.GiderHesapAdi, e.GelirHesap, e.GelirHesapAdi, e.KdvKodu, e.MasrafMerkeziZorunlu, e.ProjeZorunlu, gecerlilik = Trh(e.GecerlilikBas) + (e.GecerlilikBit.HasValue ? " – " + Trh(e.GecerlilikBit) : " →"), e.Aktif }),
                cariKodlar = YansitmaOrnek.Sirketler.Select(s => new { sirket = s, kisa = YansitmaOrnek.Kisa(s), alacak = YansitmaOrnek.AlacakCari(s), borc = YansitmaOrnek.BorcCari(s) }),
                islemler = YansitmaOrnek.Islemler.Select(Dto)
            });
        }

        /// <summary>Kaynak Fatura Seç penceresi: yansıtan şirkete göre filtreli; sunucu tarafı arama ve sayfalama.</summary>
        [HttpGet]
        public IActionResult KaynakFaturalar(string yansitan, string ara, int haric = 0, int sayfa = 1, int boy = 12)
        {
            if (string.IsNullOrEmpty(yansitan)) return Json(new { toplam = 0, sayfa = 1, boy, faturalar = new object[0] });
            var l = YansitmaOrnek.KaynakFaturalar(yansitan, ara, haric).ToList();
            return Json(new { toplam = l.Count, sayfa, boy, faturalar = l.Skip((Math.Max(1, sayfa) - 1) * boy).Take(boy) });
        }

        /// <summary>Canlı önizleme: satır hesapları, özet, validasyon, onay akışı ve muhasebe simülasyonu (kaydetmez).</summary>
        [HttpPost]
        public IActionResult Onizleme([FromBody] YnIslemGiris g)
        {
            if (g == null) return Json(new { success = false, hatalar = new[] { "Geçersiz istek." } });
            var s = new YnSonuc(); var ben = Ben();
            var i = new YnIslem { Id = g.Id, No = g.Id > 0 ? YansitmaOrnek.Bul(g.Id)?.No : null, Yansitan = g.Yansitan, Yansitilan = g.Yansitilan, Tur = string.IsNullOrEmpty(g.Tur) ? "Fatura" : g.Tur, KaynakBelgeTuru = g.KaynakBelgeTuru, MasrafMerkezi = g.MasrafMerkezi ?? "", Proje = g.Proje ?? "", Aciklama = g.Aciklama ?? "", KaynakFaturaId = g.KaynakFaturaId };
            i.Tarih = DateTime.TryParseExact(g.Tarih ?? "", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : DateTime.Today;
            var f = AlimOnayOrnek.Bul(g.KaynakFaturaId); if (f != null) { i.KaynakFaturaNo = f.FaturaNo; i.KaynakTedarikci = f.Tedarikci; i.KaynakFaturaTarihi = f.Tarih; i.ParaBirimi = YansitmaOrnek.ParaBirimi(f); i.Kur = i.ParaBirimi == "TRY" ? 1 : (g.Kur ?? f.Kur); }
            i.Kalemler = (g.Kalemler ?? new List<YnKalemGiris>()).Select(k => YansitmaOrnek.KalemKur(k, g.Rol, g.Id, s)).Where(k => k != null).ToList();
            var d = YansitmaOrnek.Dogrula(i, g.Rol, false); var (sim, ss) = YansitmaOrnek.Simule(i);
            return Json(new
            {
                success = s.Gecerli && d.Gecerli, hatalar = s.Hatalar.Concat(d.Hatalar), uyarilar = d.Uyarilar.Concat(ss.Uyarilar).Distinct(), simHatalar = ss.Hatalar,
                kalemler = i.Kalemler.Select(KalemDto), ozet = Ozet(i), akis = i.Kalemler.Count > 0 && !string.IsNullOrEmpty(i.Yansitan) ? YansitmaOrnek.OnayAkisi(i).Select(o => new { o.Sira, o.Adim, o.Kisi }) : null, akisNedeni = i.Kalemler.Count > 0 ? YansitmaOrnek.OnayNedeni(i) : "",
                simulasyon = sim.Select(FisDto), simTarihi = Zmn(DateTime.Now), paraBirimi = i.ParaBirimi, kur = i.Kur
            });
        }

        // ---------------- eylemler ----------------
        [HttpPost]
        public IActionResult Kaydet([FromBody] YnIslemGiris g)
        {
            if (g == null) return Json(new { success = false, message = "Geçersiz istek." });
            if (!YansitmaOrnek.Yetkili(g.Rol, "taslak") && g.Id == 0) return Json(new { success = false, message = "Bu rol yeni yansıtma açamaz." });
            var (i, s) = YansitmaOrnek.Kaydet(g, Kullanici(), Ben());
            if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar), uyarilar = s.Uyarilar, islem = i == null ? null : Dto(i) });
            return Json(new { success = true, message = $"{i.No} taslak olarak kaydedildi.", uyarilar = s.Uyarilar, no = i.No, islem = Dto(i) });
        }

        [HttpPost]
        public IActionResult OnayaGonder([FromBody] YnIslemGiris g)
        {
            if (g == null) return Json(new { success = false, message = "Geçersiz istek." });
            if (!YansitmaOrnek.Yetkili(g.Rol, "onayaGonder")) return Json(new { success = false, message = "Bu rol onaya gönderemez." });
            YnIslem i; YnSonuc s;
            if (g.Kalemler != null) { (i, s) = YansitmaOrnek.Kaydet(g, Kullanici(), Ben()); if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar), islem = i == null ? null : Dto(i) }); }
            else { i = YansitmaOrnek.Bul(g.Id); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); }
            var r = YansitmaOrnek.OnayaGonder(i, g.Rol, Kullanici());
            if (!r.Gecerli) return Json(new { success = false, message = string.Join(" ", r.Hatalar), hatalar = r.Hatalar, uyarilar = r.Uyarilar, islem = Dto(i) });
            return Json(new { success = true, message = $"{i.No} onaya gönderildi: {string.Join(" → ", i.Onaylar.Select(o => o.Adim))} ({YansitmaOrnek.OnayNedeni(i)}). Simülasyon snapshot'ı saklandı.", uyarilar = r.Uyarilar, no = i.No, islem = Dto(i) });
        }

        [HttpPost]
        public IActionResult Onay([FromBody] Eylem e)
        {
            var i = YansitmaOrnek.Bul(e.No ?? ""); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." });
            var adim = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor");
            var h = YansitmaOrnek.Onayla(i, e.EylemAdi, e.Not, e.Rol, adim?.Kisi ?? Kullanici());
            return Json(new { success = h == null, message = h ?? (e.EylemAdi == "Reddet" ? $"{i.No} reddedildi; talep sahibi taslağa revize edebilir." : i.Durum == "Onaylandı" ? $"{i.No} onay akışı tamamlandı: ERP yansıtma faturası oluşturulabilir." : $"{i.No}: {adim?.Adim} verildi; sıradaki adım {i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Adim}."), islem = Dto(i) });
        }

        [HttpPost]
        public IActionResult GeriCek([FromBody] Eylem e) { var i = YansitmaOrnek.Bul(e.No ?? ""); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); var h = YansitmaOrnek.GeriCek(i, e.Rol, Kullanici()); return Json(new { success = h == null, message = h ?? $"{i.No} geri çekildi; taslak olarak düzenlenebilir (v{i.Versiyon}).", islem = Dto(i) }); }

        [HttpPost]
        public IActionResult FaturaOlustur([FromBody] Eylem e) { var i = YansitmaOrnek.Bul(e.No ?? ""); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); var (h, m) = YansitmaOrnek.FaturaOlustur(i, e.Rol, Kullanici()); return Json(new { success = h == null, message = h ?? m, islem = Dto(i) }); }

        [HttpPost]
        public IActionResult Iptal([FromBody] Eylem e) { var i = YansitmaOrnek.Bul(e.No ?? ""); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); if (!YansitmaOrnek.Yetkili(e.Rol, "iptal")) return Json(new { success = false, message = "Bu rol iptal edemez." }); var h = YansitmaOrnek.Iptal(i, e.Neden, e.Rol, Kullanici()); return Json(new { success = h == null, message = h ?? $"{i.No} iptal edildi.", islem = Dto(i) }); }

        /// <summary>Kaynak sistem olayı: ERP'den gerçek muhasebe fişi satırlarını çeker (demo senaryo ile sapma eklenebilir) ve kontrolü çalıştırır.</summary>
        [HttpPost]
        public IActionResult FisAl([FromBody] Eylem e) { var i = YansitmaOrnek.Bul(e.No ?? ""); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); if (!YansitmaOrnek.Yetkili(e.Rol, "fisAl")) return Json(new { success = false, message = "Gerçek fiş okuma yalnız Muhasebe / Sistem Yöneticisi tarafından tetiklenir." }); var h = YansitmaOrnek.FisAl(i, e.Senaryo, "SAP B1 Entegrasyon"); return Json(new { success = h == null, message = h ?? (i.Gercek.Count == 0 ? $"{i.No}: ERP'de muhasebe fişi bulunamadı → {i.KontrolDurumu}." : $"{i.No}: {i.Gercek.Select(x => x.FisNo).Distinct().Count()} fiş okundu, kontrol sonucu {i.KontrolDurumu}."), islem = Dto(i) }); }

        [HttpPost]
        public IActionResult Kontrol([FromBody] Eylem e) { var i = YansitmaOrnek.Bul(e.No ?? ""); if (i == null) return Json(new { success = false, message = "İşlem bulunamadı." }); if (!YansitmaOrnek.Yetkili(e.Rol, "kontrol")) return Json(new { success = false, message = "Kontrolü yalnız Muhasebe / Sistem Yöneticisi çalıştırır; diğer roller görüntüler." }); var h = YansitmaOrnek.Kontrol(i, Kullanici()); return Json(new { success = h == null, message = h ?? $"{i.No} kontrol edildi: {i.KontrolDurumu} ({i.Kontroller.Count(k => k.Sonuc != "UYUMLU")} fark).", islem = Dto(i) }); }

        [HttpPost]
        public IActionResult Parametre([FromBody] Dictionary<string, string> d)
        {
            lock (YansitmaOrnek.Kilit) foreach (var k in YansitmaOrnek.Parametreler.Keys.ToList()) if (d != null && d.TryGetValue(k, out var v) && decimal.TryParse((v ?? "").Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out var x)) YansitmaOrnek.Parametreler[k] = x;
            return Json(new { success = true, parametreler = YansitmaOrnek.Parametreler });
        }

        // ---------------- DTO ----------------
        private static object Ozet(YnIslem i) => new { kalemSayisi = i.Kalemler.Count, toplamMaliyet = i.ToplamMaliyet, toplamKar = i.ToplamKar, toplamYansitma = i.ToplamYansitma, kdv = i.Kdv, genelToplam = i.GenelToplam, agirlikliKarPct = i.AgirlikliKarPct, toplamMaliyetTl = Math.Round(i.ToplamMaliyet * i.Kur, 2), toplamYansitmaTl = Math.Round(i.ToplamYansitma * i.Kur, 2), kdvTl = Math.Round(i.Kdv * i.Kur, 2), genelToplamTl = Math.Round(i.GenelToplam * i.Kur, 2), hedefler = i.HedefSirketler };
        private static object KalemDto(YnKalem k) => new
        {
            k.Id, k.KaynakFaturaId, k.KaynakFaturaNo, k.SatirNo, k.KalemKod, k.KalemAd, k.Kategori, k.Birim, k.FaturaMiktari, k.OncedenYansitilan, k.KalanMiktar, k.YansitilacakMiktar, k.BirimMaliyet, k.ToplamMaliyet, k.KarPct, k.BirimKar, k.BirimYansitma, k.ManuelFiyat,
            k.ToplamYansitma, k.ToplamKar, k.KdvPct, k.KdvTutar, k.KdvDahil, k.MasrafMerkezi, k.Proje, k.Gerekce, k.EslemeId, dagitim = k.Dagitim.Select(d => new { d.Sirket, d.Miktar, d.KarPct }), islemSonrasiKalan = Math.Max(0, k.KalanMiktar - k.YansitilacakMiktar)
        };
        private static object FisDto(YnFisSatir x) => new { x.Sirket, x.FisNo, x.HesapKodu, x.HesapAdi, x.Borc, x.Alacak, x.ParaBirimi, x.Kur, x.DovizTutar, x.CariKod, x.MasrafMerkezi, x.Proje, x.KalemRef, x.Aciklama, x.Kural, x.Tip, x.Referans };
        private static object Dto(YnIslem i) => new
        {
            i.Id, i.No, tarih = i.Tarih.ToString("yyyy-MM-dd"), tarihTr = Trh(i.Tarih), i.Yansitan, i.Yansitilan, hedefler = i.HedefSirketler, i.Tur, i.KaynakBelgeTuru, i.ParaBirimi, i.Kur, i.MasrafMerkezi, i.Proje, i.Departman, i.Aciklama, i.Durum,
            i.KaynakFaturaId, i.KaynakFaturaNo, i.KaynakTedarikci, kaynakFaturaTarihi = Trh(i.KaynakFaturaTarihi), kalemler = i.Kalemler.Select(KalemDto), ozet = Ozet(i),
            simulasyon = i.Simulasyon.Select(FisDto), simulasyonTarihi = Zmn(i.SimulasyonTarihi), onaylar = i.Onaylar.Select(o => new { o.Sira, o.Adim, o.Kisi, o.Durum, tarih = Zmn(o.Tarih), o.Not }), onayNedeni = i.Onaylar.Count > 0 ? YansitmaOrnek.OnayNedeni(i) : "",
            audit = i.Audit.OrderByDescending(a => a.Zaman).Select(a => new { zaman = Zmn(a.Zaman), a.Kullanici, a.Alan, a.Eski, a.Yeni, a.Kaynak, a.Gerekce }),
            i.YansitmaFaturaNo, i.ErpDocEntry, entegrasyon = new { i.Entegrasyon.Durum, i.Entegrasyon.Istek, i.Entegrasyon.Cevap, zaman = Zmn(i.Entegrasyon.Zaman), i.Entegrasyon.IdempotencyKey },
            gercek = i.Gercek.Select(FisDto), gercekTarihi = Zmn(i.GercekTarihi), fisler = i.Gercek.Select(x => x.FisNo).Distinct(), i.KontrolDurumu, kontroller = i.Kontroller, kontrolTarihi = Zmn(i.KontrolTarihi),
            i.RedNedeni, i.IptalNedeni, i.Olusturan, olusturmaZamani = Zmn(i.OlusturmaZamani), i.Guncelleyen, guncellemeZamani = Zmn(i.GuncellemeZamani), i.Versiyon, i.OlusturanSirket, i.OlusturanDepartman, i.Aktif, i.Onaylandi,
            bekleyenAdim = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Adim, bekleyenKisi = i.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Kisi
        };
    }
}
