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
    /// Cari Kart ve Faaliyet Eşleştirme: Müşteri / Tedarikçi Cari Kartları (liste + detay) ve Müşteri / Tedarikçi Talep Formu (cari açılış talebi).
    /// Kaynak: "Cari Kart ve Faaliyet Eşleştirme Yapısı — IT Uygulama Raporu v6". Kurallar ve örnek veri Data/CariKartOrnek.cs; betik wwwroot/js/cari-kart.js.
    /// </summary>
    [Authorize]
    public class CariKartController : Controller
    {
        private static readonly CultureInfo Tr = CariKartOrnek.Tr;
        private NaKisi Ben() => NakitAvansOrnek.KisiBul(User?.Identity?.Name) ?? NakitAvansOrnek.Personel[0];
        private static string Trh(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy", Tr) : "";
        private static string Zmn(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy HH:mm", Tr) : "";
        public class Eylem { public string No { get; set; } = ""; public string Rol { get; set; } public string Not { get; set; } public string Neden { get; set; } public string EylemAdi { get; set; } public string Kod { get; set; } public string Durum { get; set; } }
        public class LimitIstek { public string Kod { get; set; } public string Rol { get; set; } public string Tur { get; set; } public decimal ManuelLimit { get; set; } public int DinamikAy { get; set; } = 3; public decimal DinamikCarpan { get; set; } = 2; public string Not { get; set; } }

        private IActionResult Kartlar(string tip) { ViewData["Title"] = tip + " Cari Kartları"; ViewBag.Tip = tip; ViewBag.Sayfa = "kartlar"; return View("~/Views/CariKart/Kartlar.cshtml"); }
        private IActionResult Talep(string tip, string no) { ViewData["Title"] = tip + " Talep Formu"; ViewBag.Tip = tip; ViewBag.Sayfa = "talep"; ViewBag.No = no ?? ""; return View("~/Views/CariKart/Talep.cshtml"); }
        public IActionResult Musteri() => Kartlar("Müşteri");
        public IActionResult Tedarikci() => Kartlar("Tedarikçi");
        public IActionResult MusteriTalep(string no) => Talep("Müşteri", no);
        public IActionResult TedarikciTalep(string no) => Talep("Tedarikçi", no);

        /// <summary>Seçim listeleri (§14), kartlar ve talepler.</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            var ben = Ben();
            return Json(new
            {
                ben = new { ben.AdSoyad, ben.Sirket, ben.Departman, ben.Rol }, roller = CariKartOrnek.Yetkiler.Keys, yetkiler = CariKartOrnek.Yetkiler, sirketler = CariKartOrnek.Sirketler,
                kartTipleri = CariKartOrnek.KartTipleri, bolgeler = CariKartOrnek.Bolgeler, ulkeler = CariKartOrnek.Ulkeler, iller = CariKartOrnek.TurkiyeIlleri, yurtdisiSehirler = CariKartOrnek.YurtdisiSehirler, sinifMatrisi = CariKartOrnek.SinifMatrisi,
                iliskiDurumlari = CariKartOrnek.IliskiDurumlari, iliskiAciklama = CariKartOrnek.IliskiAciklama, roller2 = CariKartOrnek.Roller, vadeSartlari = CariKartOrnek.VadeSartlari, durumlar = CariKartOrnek.Durumlar, talepDurumlari = CariKartOrnek.TalepDurumlari,
                faaliyetHiyerarsisi = CariKartOrnek.FaaliyetHiyerarsisi, anaGrupAciklama = CariKartOrnek.AnaGrupAciklama, urunHiyerarsisi = CariKartOrnek.UrunHiyerarsisi, satisTemsilcileri = CariKartOrnek.SatisTemsilcileri, kodOnEkleri = CariKartOrnek.KodOnEkleri,
                limitTurleri = CariKartOrnek.LimitTurleri, limitTuruAciklama = CariKartOrnek.LimitTuruAciklama,
                kartlar = CariKartOrnek.Kartlar.Select(KartDto), talepler = CariKartOrnek.Talepler.Select(TalepDto)
            });
        }
        /// <summary>Canlı doğrulama (§13) — kaydetmez.</summary>
        [HttpPost]
        public IActionResult Dogrula([FromBody] CariTalepGiris g)
        {
            if (g == null) return Json(new { success = false, hatalar = new[] { "Geçersiz istek." } });
            var mevcut = g.Id > 0 ? CariKartOrnek.Talepler.FirstOrDefault(t => t.Id == g.Id) : null;
            var duzenlenen = string.IsNullOrEmpty(g.Kod) ? null : CariKartOrnek.KartBul(g.Kod); if (duzenlenen != null) g.KartTipi = duzenlenen.KartTipi;   // doğrudan kart düzenleme: tip sabit, mükerrer VKN kontrolünde kendisi hariç
            var k = CariKartOrnek.KartKur(g); var s = CariKartOrnek.Dogrula(k, false, duzenlenen?.Kod ?? mevcut?.OlasiKod());
            var es = k.Rol == "Çift Rol" && !string.IsNullOrEmpty(k.EsCariKodu) ? CariKartOrnek.KartBul(k.EsCariKodu) : null;
            return Json(new { success = s.Gecerli, hatalar = s.Hatalar, uyarilar = s.Uyarilar, siniflar = CariKartOrnek.Siniflar(k.KartTipi ?? "", k.Bolge ?? ""), olasiKod = duzenlenen?.Kod ?? mevcut?.OlasiKod() ?? CariKartOrnek.YeniKod(k.KartTipi), esKart = es == null ? null : new { es.Kod, es.KisaAd, es.KartTipi, es.Sinif, es.Rol, es.EsCariKodu }, akis = (k.IliskiDurumu == "Grup İçi" || k.IliskiDurumu == "İlişkili Cari" || k.Sinif == "Özel Takip") ? new[] { "Muhasebe — Cari Açılış Onayı", "Mali İşler Direktörü" } : new[] { "Muhasebe — Cari Açılış Onayı" } });
        }
        [HttpPost] public IActionResult Kaydet([FromBody] CariTalepGiris g) { if (g == null) return Json(new { success = false, message = "Geçersiz istek." }); if (!CariKartOrnek.Yetkili(g.Rol, "taslak")) return Json(new { success = false, message = "Bu rol talep oluşturamaz." }); var ben = Ben(); var (t, s) = CariKartOrnek.Kaydet(g, ben.AdSoyad, ben.Departman); if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar), uyarilar = s.Uyarilar, talep = t == null ? null : TalepDto(t) }); return Json(new { success = true, message = $"{t.No} taslak olarak kaydedildi.", uyarilar = s.Uyarilar, no = t.No, talep = TalepDto(t) }); }
        [HttpPost]
        public IActionResult OnayaGonder([FromBody] CariTalepGiris g)
        {
            if (g == null) return Json(new { success = false, message = "Geçersiz istek." }); if (!CariKartOrnek.Yetkili(g.Rol, "onayaGonder")) return Json(new { success = false, message = "Bu rol onaya gönderemez." }); var ben = Ben(); CariTalep t; YnSonuc s;
            if (g.KartTipi != null) { (t, s) = CariKartOrnek.Kaydet(g, ben.AdSoyad, ben.Departman); if (!s.Gecerli) return Json(new { success = false, message = string.Join(" ", s.Hatalar), talep = t == null ? null : TalepDto(t) }); }
            else { t = CariKartOrnek.Talepler.FirstOrDefault(x => x.Id == g.Id); if (t == null) return Json(new { success = false, message = "Talep bulunamadı." }); }
            var r = CariKartOrnek.OnayaGonder(t, ben.AdSoyad); if (!r.Gecerli) return Json(new { success = false, message = string.Join(" ", r.Hatalar), hatalar = r.Hatalar, uyarilar = r.Uyarilar, talep = TalepDto(t) });
            return Json(new { success = true, message = $"{t.No} onaya gönderildi: {string.Join(" → ", t.Onaylar.Select(o => o.Adim))}. Onaylanınca {t.Kart.KartTipi} kartı ({CariKartOrnek.YeniKod(t.Kart.KartTipi)} sırası) otomatik açılır.", uyarilar = r.Uyarilar, no = t.No, talep = TalepDto(t) });
        }
        [HttpPost] public IActionResult Onay([FromBody] Eylem e) { var t = CariKartOrnek.TalepBul(e.No); if (t == null) return Json(new { success = false, message = "Talep bulunamadı." }); var h = CariKartOrnek.Onayla(t, e.EylemAdi, e.Not, e.Rol, t.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Kisi ?? Ben().AdSoyad); return Json(new { success = h == null, message = h ?? (e.EylemAdi == "Reddet" ? $"{t.No} reddedildi." : t.Durum == "Onaylandı" ? $"{t.No} onaylandı; CARİ_MASTER kartı açıldı: {t.OlasanKod()} {t.Kart.KisaAd} ({t.Kart.Faaliyetler.Count} faaliyet satırı)." : $"{t.No}: adım onaylandı; sıradaki {t.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Adim}."), talep = TalepDto(t), kart = t.Durum == "Onaylandı" ? KartDto(t.Kart) : null }); }
        [HttpPost] public IActionResult Iptal([FromBody] Eylem e) { var t = CariKartOrnek.TalepBul(e.No); if (t == null) return Json(new { success = false, message = "Talep bulunamadı." }); if (!CariKartOrnek.Yetkili(e.Rol, "iptal")) return Json(new { success = false, message = "Bu rol iptal edemez." }); var h = CariKartOrnek.Iptal(t, e.Neden, Ben().AdSoyad); return Json(new { success = h == null, message = h ?? $"{t.No} iptal edildi.", talep = TalepDto(t) }); }
        /// <summary>Kredi limiti tanımı (Muhasebe / Yönetici): Yok · Manuel (TL) · Dinamik (son N ay ortalaması × çarpan). Dinamik limit her Veri çağrısında güncel faturalarla yeniden hesaplanır.</summary>
        [HttpPost] public IActionResult KartLimit([FromBody] LimitIstek m) { var k = CariKartOrnek.KartBul(m?.Kod ?? ""); if (k == null) return Json(new { success = false, message = "Kart bulunamadı." }); if (!CariKartOrnek.Yetkili(m.Rol, "limit")) return Json(new { success = false, message = "Kredi limitini yalnız Muhasebe / Yönetici tanımlar." }); var h = CariKartOrnek.LimitDegistir(k, m.Tur, m.ManuelLimit, m.DinamikAy, m.DinamikCarpan, m.Not, Ben().AdSoyad); var l = CariKartOrnek.LimitHesapla(k); return Json(new { success = h == null, message = h ?? $"{k.Kod} {k.KisaAd} kredi limiti: {(l.Tur == "Yok" ? "limit izlenmiyor" : l.Limit.ToString("N0", Tr) + " ₺ (" + l.Tur + ")")} · {l.Durum}.", kart = KartDto(k) }); }
        /// <summary>Kartta doğrudan düzeltme (yeni talep açmadan): CARİ_MASTER alanları + faaliyet satırları; §13 doğrulaması, değişiklik günlüğü. Yetki "duzenle".</summary>
        [HttpPost] public IActionResult KartDuzenle([FromBody] CariTalepGiris g) { if (g == null) return Json(new { success = false, message = "Geçersiz istek." }); var k = CariKartOrnek.KartBul(g.Kod ?? ""); if (k == null) return Json(new { success = false, message = "Kart bulunamadı." }); if (!CariKartOrnek.Yetkili(g.Rol, "duzenle")) return Json(new { success = false, message = "Bu rol kartı düzenleyemez." }); var s = CariKartOrnek.KartDuzenle(k, g, Ben().AdSoyad); var n = k.Degisiklikler.Count(a => (DateTime.Now - a.Zaman).TotalSeconds < 5); return Json(new { success = s.Gecerli, message = s.Gecerli ? (n == 0 ? $"{k.Kod} {k.KisaAd}: değişiklik yok." : $"{k.Kod} {k.KisaAd} güncellendi ({n} alan: {string.Join(", ", k.Degisiklikler.Take(n).Select(a => a.Alan))}).") : string.Join(" ", s.Hatalar), hatalar = s.Hatalar, uyarilar = s.Uyarilar, kart = KartDto(k) }); }
        [HttpPost] public IActionResult KartDurum([FromBody] Eylem e) { var k = CariKartOrnek.KartBul(e.Kod ?? ""); if (k == null) return Json(new { success = false, message = "Kart bulunamadı." }); if (!CariKartOrnek.Yetkili(e.Rol, "kartAc")) return Json(new { success = false, message = "Kart durumunu yalnız Muhasebe / Yönetici değiştirir." }); var h = CariKartOrnek.KartDurum(k, e.Durum, Ben().AdSoyad); return Json(new { success = h == null, message = h ?? $"{k.Kod} {k.KisaAd} → {k.Durum} ({k.DurumKaynagi}).", kart = KartDto(k) }); }

        // ---- DTO ----
        private static object FaaliyetDto(CariFaaliyet f) => new { f.Sira, f.AnaGrup, f.Grup, f.SatisTemsilcisi, f.AnaFaaliyet, f.UrunSinifi, f.UrunGrubu, f.UrunSerisi, f.UrunAdi, f.Durum, baslangicTarihi = f.BaslangicTarihi?.ToString("yyyy-MM-dd"), baslangicTr = Trh(f.BaslangicTarihi), f.Not, hiyerarsi = string.Join(" > ", new[] { f.AnaGrup, f.Grup, f.UrunSinifi, f.UrunGrubu, f.UrunSerisi, f.UrunAdi }.Where(x => !string.IsNullOrEmpty(x))) };
        /// <summary>CARİ_MASTER + faaliyet satırları. Durum otomatiktir (son 1 yılda hareket → Aktif); durumKaynagi / sonHareket* alanları nedenini gösterir.</summary>
        private static string Tl(decimal v) => v.ToString("N0", Tr) + " ₺";
        /// <summary>Kredi limiti: tanım (tür / manuel tutar / dinamik ay-çarpan), güncel hesap, cari risk, kullanım ve geçmiş.</summary>
        private static object LimitDto(CariKart k)
        {
            if (k.KartTipi != "Müşteri" && k.KartTipi != "Tedarikçi") return null;
            var l = CariKartOrnek.LimitHesapla(k);
            return new { tur = l.Tur, manuelLimit = k.ManuelLimit, dinamikAy = l.Ay, dinamikCarpan = l.Carpan, limit = l.Limit, limitTr = l.Tur == "Yok" ? "" : Tl(l.Limit), alimToplam = l.AlimToplam, alimToplamTr = Tl(l.AlimToplam), aylikOrtalama = l.AylikOrtalama, aylikOrtalamaTr = Tl(l.AylikOrtalama), aylar = l.Aylar.Select(a => new { ay = a.Ay, tutar = a.Tutar, tutarTr = Tl(a.Tutar), adet = a.Adet }), bakiye = l.Bakiye, bakiyeTr = l.Bakiye.ToString("N0", Tr) + " ₺", bakiyeKaynak = l.BakiyeKaynak, risk = l.Risk, kullanimYuzde = l.KullanimYuzde, kalan = l.Kalan, kalanTr = l.Tur == "Yok" ? "" : Tl(l.Kalan), durum = l.Durum, aciklama = l.Aciklama, not = k.LimitNot, kim = k.LimitKim, zaman = Zmn(k.LimitZaman), gecmis = k.LimitGecmisi.Select(g => new { zaman = Zmn(g.Zaman), g.Kullanici, g.Eski, g.Yeni, g.Not }) };
        }
        private static object KartDto(CariKart k)
        {
            var sh = CariKartOrnek.SonHareket(k);
            return new { limit = LimitDto(k), degisiklikler = k.Degisiklikler.Select(a => new { zaman = Zmn(a.Zaman), a.Kullanici, a.Alan, a.Eski, a.Yeni }), k.Kod, k.KisaAd, k.KisaltmaliUnvan, k.ResmiUnvan, k.VergiNo, k.VergiDairesi, k.KartTipi, k.Bolge, k.Ulke, k.Sehir, k.Sinif, k.IliskiDurumu, k.Rol, k.EsCariKodu, esKartAd = string.IsNullOrEmpty(k.EsCariKodu) ? "" : (CariKartOrnek.KartBul(k.EsCariKodu)?.KisaAd ?? ""), k.Yetkili, k.Telefon, k.Eposta, k.VadeSarti, k.Durum, durumKaynagi = k.DurumKaynagi, elleDurum = k.ElleDurum ?? "", sonHareketTr = Trh(sh.Tarih), sonHareketGun = (DateTime.Today - sh.Tarih).Days, sonHareketKaynak = sh.Kaynak, k.Not, k.Sirket, olusturmaTarihi = Trh(k.OlusturmaTarihi), k.Olusturan, k.KaynakTalepNo, anaFaaliyet = k.AnaFaaliyet, faaliyetler = k.Faaliyetler.Select(FaaliyetDto), temsilciler = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), anaGruplar = string.Join(", ", k.Faaliyetler.Select(f => f.AnaGrup).Distinct()) };
        }
        private static object TalepDto(CariTalep t) => new { t.Id, t.No, tarih = t.Tarih.ToString("yyyy-MM-dd"), tarihTr = Trh(t.Tarih), t.TalepTuru, t.TalepEden, t.Departman, t.Sirket, t.Gerekce, t.Durum, t.OlusanKod, t.RedNedeni, kart = KartDto(t.Kart), onaylar = t.Onaylar.Select(o => new { o.Sira, o.Adim, o.Kisi, o.Durum, tarih = Zmn(o.Tarih), o.Not }), audit = t.Audit.OrderByDescending(a => a.Zaman).Select(a => new { zaman = Zmn(a.Zaman), a.Kullanici, a.Alan, a.Eski, a.Yeni, a.Kaynak, a.Gerekce }), guncellemeZamani = Zmn(t.GuncellemeZamani), t.Aktif, duzenlenebilir = t.Durum == "Taslak" || t.Durum == "Reddedildi", bekleyenAdim = t.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Adim, bekleyenKisi = t.Onaylar.FirstOrDefault(o => o.Durum == "Bekliyor")?.Kisi };
    }
    internal static class CariTalepUzanti { public static string OlasiKod(this CariTalep t) => string.IsNullOrEmpty(t.OlusanKod) ? CariKartOrnek.YeniKod(t.Kart.KartTipi) : t.OlusanKod; public static string OlasanKod(this CariTalep t) => t.OlusanKod; }
}
