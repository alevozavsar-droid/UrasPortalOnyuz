using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Cari Mutabakat Yönetimi — Mali İşler › Muhasebe › Cari Mutabakat ve Mali İşler › Cari İşler › Mutabakat ve Kapama (aynı ekran).
    /// Sekmeler: Aktif Cariler | Pasif Cariler (kontrol merkezi, §3) | Kontrol Bekleyen Carilerim (temsilci ekranı, §5–6) | Kapanış Kontrolü (§8) |
    ///           Gönderimler ve Yanıtlar (§10) | Günlük ve Parametreler (§11, §2 not). Yazdırılabilir: Ekstre (döviz bazında) ve Mutabakat Mektubu (PDF).
    /// Veri ve süreç kuralları: Data/MutabakatOrnek.cs (cari kartlar Data/CariKartOrnek.cs). Rol demo amaçlı ekrandan seçilir (§11 yetki matrisi).
    /// </summary>
    [Authorize]
    public class CariMutabakatController : Controller
    {
        private string Kod() => User?.Identity?.Name ?? "";
        private string Ben() => OrnekVeri.KullaniciAdi(Kod());
        private string VarsayilanRol()
        {
            var y = OrnekVeri.Kullanicilar.FirstOrDefault(k => string.Equals(k.Kod, Kod(), StringComparison.OrdinalIgnoreCase)).Yetki;
            return y == "A" ? "Yönetici" : y == "ST" ? "Satış Temsilcisi" : "Muhasebe";
        }

        public IActionResult Index(string sekme, string kod)
        {
            ViewData["Title"] = "Cari Mutabakat Yönetimi";
            ViewBag.Sekme = new[] { "pasif", "temsilci", "kapanis", "gonderim", "gunluk" }.Contains(sekme) ? sekme : "aktif";
            ViewBag.Kod = kod ?? "";
            ViewBag.Ben = Ben(); ViewBag.Rol = VarsayilanRol();
            return View();
        }

        [HttpGet]
        public IActionResult Veri()
        {
            var cariler = MutabakatOrnek.Cariler;
            lock (MutabakatOrnek.Kilit)
            {
                var tumTemsilciler = MutabakatOrnek.TumTemsilciler();
                return Json(new
                {
                    success = true,
                    simdi = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
                    ben = new { ad = Ben(), kod = Kod(), rol = VarsayilanRol(), temsilci = tumTemsilciler.Contains(Ben()) ? Ben() : "" },
                    roller = MutabakatOrnek.Roller, yetkiler = MutabakatOrnek.Yetkiler,
                    parametreler = new { aktifPasifAy = MutabakatOrnek.AktifPasifAy, eskiBakiyeGun = MutabakatOrnek.EskiBakiyeGun, degerlemeRiskiGun = MutabakatOrnek.DegerlemeRiskiGun },
                    kurlar = MutabakatOrnek.Kurlar, kurTarihi = MutabakatOrnek.KurTarihi.ToString("dd.MM.yyyy"),
                    durumlar = MutabakatOrnek.Durumlar, iletisimDurumlari = MutabakatOrnek.IletisimDurumlari, kapanisSecenekleri = MutabakatOrnek.KapanisSecenekleri,
                    unvanlar = MutabakatOrnek.Unvanlar, departmanlar = MutabakatOrnek.Departmanlar,
                    temsilciler = tumTemsilciler.Select(t => new { ad = t, rol = MutabakatOrnek.TemsilciRolu(t) }),
                    sirketler = cariler.Select(c => c.Kart?.Sirket).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x),
                    kartTipleri = new[] { "Müşteri", "Tedarikçi" }, siniflar = cariler.Select(c => c.Kart?.Sinif).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x),
                    roller2 = CariKartOrnek.Roller, iliskiDurumlari = CariKartOrnek.IliskiDurumlari,
                    cariler = cariler.Select(CariDto),
                    genelLog = MutabakatOrnek.GenelLog.Take(300).Select(LogDto)
                });
            }
        }

        /// <summary>Ekstre satırları (döviz bazında; pb boşsa tümü).</summary>
        [HttpGet]
        public IActionResult Hareketler(string kod, string pb)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." });
            lock (MutabakatOrnek.Kilit)
                return Json(new { success = true, kod = c.CariKodu, bakiyeZamani = c.BakiyeZamani?.ToString("dd.MM.yyyy HH:mm"), hareketler = c.Hareketler.Where(h => string.IsNullOrEmpty(pb) || h.ParaBirimi == pb).Select(h => new { h.Id, tarih = h.Tarih.ToString("dd.MM.yyyy"), h.ParaBirimi, h.BelgeTipi, h.BelgeNo, h.Aciklama, h.Borc, h.Alacak, h.Bakiye, vade = h.Vade?.ToString("dd.MM.yyyy") ?? "", h.KapamaDurumu, h.Kalan }) });
        }

        /// <summary>Yazdırılabilir ekstre (seçilen döviz) — PDF: tarayıcı yazdır.</summary>
        public IActionResult Ekstre(string kod, string pb)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return NotFound();
            ViewBag.Pb = string.IsNullOrEmpty(pb) ? (c.Dovizler.FirstOrDefault()?.ParaBirimi ?? "TL") : pb;
            ViewBag.OlusturmaZamani = DateTime.Now;
            return View(c);
        }

        /// <summary>Yazdırılabilir mutabakat mektubu (PDF önizleme).</summary>
        public IActionResult Mektup(string kod)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return NotFound();
            ViewBag.OlusturmaZamani = DateTime.Now;
            return View(c);
        }

        // ---- aksiyonlar
        [HttpPost] public IActionResult TemsilciyeGonder([FromBody] MtIstek g) { var kodlar = g?.Kodlar ?? (string.IsNullOrEmpty(g?.Kod) ? new List<string>() : new List<string> { g.Kod }); var (n, h) = MutabakatOrnek.TemsilciyeGonder(kodlar, g?.Rol, Ben()); return Sonuc(n, h, "cari temsilciye gönderildi", kodlar); }
        [HttpPost] public IActionResult MutabakatGonder([FromBody] MtIstek g) { var kodlar = g?.Kodlar ?? (string.IsNullOrEmpty(g?.Kod) ? new List<string>() : new List<string> { g.Kod }); var (n, h) = MutabakatOrnek.MutabakatGonder(kodlar, g?.Rol, Ben()); return Sonuc(n, h, "cariye mutabakat gönderildi", kodlar); }
        [HttpPost] public IActionResult TekrarGonder([FromBody] MtIstek g) { var kodlar = new List<string> { g?.Kod }; var (n, h) = MutabakatOrnek.MutabakatGonder(kodlar, g?.Rol, Ben(), true); return Sonuc(n, h, "mutabakat tekrar gönderildi", kodlar); }
        [HttpPost] public IActionResult IletisimKaydet([FromBody] MtIstek g) => Tek(g, MutabakatOrnek.IletisimKaydet(g, Ben()), "İletişim bilgileri kaydedildi.");
        [HttpPost] public IActionResult IletisimTamamla([FromBody] MtIstek g) => Tek(g, MutabakatOrnek.IletisimTamamla(g, Ben()), "İletişim bilgileri tamamlandı.");
        [HttpPost] public IActionResult BakiyeOnayla([FromBody] MtIstek g) => Tek(g, MutabakatOrnek.BakiyeOnayla(g, Ben()), g?.FarkVar == true ? "Fark / düzeltme bildirimi kaydedildi; cari muhasebe çözümü bekliyor." : "Bakiye kontrolü onaylandı.");
        [HttpPost] public IActionResult FarkCoz([FromBody] MtIstek g) => Tek(g, MutabakatOrnek.FarkCoz(g, Ben()), "Fark çözüldü.");
        [HttpPost] public IActionResult KapanisCoz([FromBody] MtIstek g) => Tek(g, MutabakatOrnek.KapanisCoz(g, Ben()), "Kapanış kontrolü sonuçlandırıldı.");
        [HttpPost] public IActionResult YanitIsle([FromBody] MtIstek g) => Tek(g, MutabakatOrnek.YanitIsle(g, Ben()), "Cari yanıtı işlendi: " + g?.Sonuc + ".");
        [HttpPost] public IActionResult ItirazCoz([FromBody] MtIstek g) => Tek(g, MutabakatOrnek.ItirazCoz(g, Ben()), "İtiraz çözüldü; cari yeniden iç kontrole gönderildi.");
        [HttpPost] public IActionResult ParametreKaydet([FromBody] MtIstek g) { var h = MutabakatOrnek.ParametreKaydet(g, Ben()); return Json(new { success = h == null, message = h ?? "Parametreler kaydedildi; aktif / pasif ayrımı ve risk işaretleri yeniden hesaplandı.", parametreler = new { aktifPasifAy = MutabakatOrnek.AktifPasifAy, eskiBakiyeGun = MutabakatOrnek.EskiBakiyeGun, degerlemeRiskiGun = MutabakatOrnek.DegerlemeRiskiGun } }); }
        [HttpPost] public IActionResult Yenile([FromBody] MtIstek g) { var c = MutabakatOrnek.Bul(g?.Kod); if (c == null) return Json(new { success = false, message = "Cari bulunamadı." }); MutabakatOrnek.Yenile(c); return Json(new { success = true, message = "Bakiye yenilendi (" + c.BakiyeZamani?.ToString("dd.MM.yyyy HH:mm") + ").", cari = CariDto(c) }); }

        private IActionResult Tek(MtIstek g, string hata, string mesaj) { var c = MutabakatOrnek.Bul(g?.Kod); return Json(new { success = hata == null, message = hata ?? mesaj, cari = c == null ? null : CariDto(c) }); }
        private IActionResult Sonuc(int n, List<string> hatalar, string ne, List<string> kodlar)
        {
            var etkilenen = kodlar.Select(MutabakatOrnek.Bul).Where(c => c != null).Select(CariDto);
            string m = n > 0 ? $"{n} {ne}." : "İşlem yapılmadı."; if (hatalar.Count > 0) m += " " + string.Join(" ", hatalar);
            return Json(new { success = n > 0, message = m, cariler = etkilenen, hatalar });
        }

        // ---- DTO
        private static string Trh(DateTime? d) => d?.ToString("dd.MM.yyyy") ?? "";
        private static string Zmn(DateTime? d) => d?.ToString("dd.MM.yyyy HH:mm") ?? "";
        private static object LogDto(MtLog l) => new { zaman = Zmn(l.Zaman), l.Kullanici, l.Islem, l.Onceki, l.Yeni, l.Aciklama, l.DurumGecisi };
        private static object KisiDto(MtKisi k) => new { k.Id, k.AdSoyad, k.Unvan, k.Departman, k.Eposta, k.Telefon, k.CepTelefonu, k.MutabakatYetkilisi, k.AnaYetkili, k.EpostaGonder, k.Dogru, k.Not };
        /// <summary>Kontrol merkezi satırı (§3 kolonları) + detay (kişiler, dövizler, kontrol, kapanış, gönderimler, log) + cari kart (CARİ_MASTER standardı).</summary>
        private static object CariDto(MtCari c)
        {
            var k = c.Kart; var sh = k == null ? (Tarih: DateTime.Today, Kaynak: "") : CariKartOrnek.SonHareket(k);
            return new
            {
                kod = c.CariKodu, sirket = k?.Sirket ?? "", kisaAd = k?.KisaAd ?? "", resmiUnvan = k?.ResmiUnvan ?? "", kartTipi = k?.KartTipi ?? "", sinif = k?.Sinif ?? "", rol = k?.Rol ?? "", iliski = k?.IliskiDurumu ?? "", bolge = k?.Bolge ?? "", ulke = k?.Ulke ?? "",
                temsilciler = c.Temsilciler, temsilciRolleri = c.Temsilciler.Select(t => new { ad = t, rol = MutabakatOrnek.TemsilciRolu(t) }),
                aktif = c.Aktif, durum = c.Durum, iletisimDurumu = c.IletisimDurumu, gonderimeHazir = c.GonderimeHazirMi,
                bakiyeTl = c.BakiyeTl, bakiyeYonu = c.BakiyeTl > 0 ? "Borç" : c.BakiyeTl < 0 ? "Alacak" : "Sıfır", cokDoviz = c.CokDoviz,
                enEskiAcikFaturaTr = Trh(c.EnEskiAcikFatura), bakiyeYasi = c.BakiyeYasi, eskiBakiyeUyari = c.EskiBakiyeUyari, degerlemeRiski = c.DegerlemeRiski, kapanisGerekli = c.KapanisGerekli,
                sonMutabakatTr = Trh(c.SonMutabakat), sonMutabakatSonrasiIslem = c.SonMutabakatSonrasiIslem, sonHareketTr = Trh(c.SonHareket), kartSonHareketTr = Trh(sh.Tarih), kartSonHareketKaynak = sh.Kaynak,
                bakiyeZamani = Zmn(c.BakiyeZamani),
                acikFaturaAdet = c.Dovizler.Sum(d => d.AcikFaturaAdet), acikFaturaTl = Math.Round(c.Dovizler.Sum(d => d.AcikFaturaTutar * d.Kur), 2),
                acikCekAdet = c.Hareketler.Count(h => (h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.KapamaDurumu == "Vade Bekliyor"), acikCekTl = Math.Round(c.Hareketler.Where(h => (h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet") && h.KapamaDurumu == "Vade Bekliyor").Sum(h => (h.Alacak + h.Borc) * (MutabakatOrnek.Kurlar.TryGetValue(h.ParaBirimi, out var kr) ? kr : 1m)), 2),
                bekleyenAdet = c.Bekleyenler.Count, bekleyenTl = Math.Round(c.Bekleyenler.Sum(b => b.Tutar * (MutabakatOrnek.Kurlar.TryGetValue(b.ParaBirimi, out var kr2) ? kr2 : 1m)), 2), bekleyenler = c.Bekleyenler.Select(b => new { b.Tur, tarih = Trh(b.Tarih), b.Tutar, b.ParaBirimi, b.Aciklama }),
                mahsupVirmanBekleyen = c.MahsupVirmanBekleyen,
                dovizler = c.Dovizler.Select(d => new { d.ParaBirimi, d.Borc, d.Alacak, net = d.Net, d.Kur, tlKarsiligi = d.TlKarsiligi, d.AcikFaturaAdet, d.AcikFaturaTutar, enEskiAcikFaturaTr = Trh(d.EnEskiAcikFatura), bakiyeYasi = d.BakiyeYasi, eskiBakiyeUyari = d.EskiBakiyeUyari, degerlemeRiski = d.DegerlemeRiski }),
                kisiler = c.Kisiler.Select(KisiDto),
                surecAcik = c.SurecAcik, surecNo = c.SurecNo, temsilciyeGonderim = Zmn(c.TemsilciyeGonderim), temsilciyeGonderen = c.TemsilciyeGonderen ?? "", iletisimOnay = Zmn(c.IletisimOnayZamani), iletisimOnaylayan = c.IletisimOnaylayan ?? "",
                kontrol = new { c.Kontrol.BakiyeDogru, c.Kontrol.AcikFaturalar, c.Kontrol.BekleyenOdeme, c.Kontrol.CekSenet, c.Kontrol.DovizDogru, c.Kontrol.GonderimeUygun, sorularTamam = c.Kontrol.SorularTamam, c.Kontrol.FarkVar, c.Kontrol.FarkAciklama, farkZaman = Zmn(c.Kontrol.FarkZaman), farkBildiren = c.Kontrol.FarkBildiren ?? "", c.Kontrol.FarkCozuldu, c.Kontrol.FarkCozum, farkCozumZaman = Zmn(c.Kontrol.FarkCozumZaman), farkCozen = c.Kontrol.FarkCozen ?? "", onaylar = c.Kontrol.Onaylar.Select(o => new { temsilci = o.Temsilci, zaman = Zmn(o.Zaman) }), onayTamam = c.TemsilciOnayiTamam },
                kapanis = new { gerekli = c.KapanisGerekli, c.Kapanis.Cozuldu, secim = c.Kapanis.Secim ?? "", c.Kapanis.Aciklama, zaman = Zmn(c.Kapanis.Zaman), cozen = c.Kapanis.Cozen ?? "" },
                blokajlar = c.Blokajlar.Select(b => new { kontrol = b.Kontrol, izin = b.Izin }),
                gonderimler = c.Gonderimler.OrderByDescending(g => g.Id).Select(g => new { g.Id, g.SurecNo, g.BelgeNo, tarih = Zmn(g.Tarih), bakiyeTarihi = Zmn(g.BakiyeTarihi), g.Kime, g.Kanal, g.Ozet, g.TlKarsiligi, g.Sonuc, yanitTarihi = Zmn(g.YanitTarihi), g.YanitAciklama, g.Gonderen, g.Tekrar }),
                log = c.Log.Take(40).Select(LogDto),
                kart = k == null ? null : new { k.Kod, k.KisaAd, k.KisaltmaliUnvan, k.ResmiUnvan, k.VergiNo, k.VergiDairesi, k.KartTipi, k.Bolge, k.Ulke, k.Sehir, k.Sinif, k.IliskiDurumu, k.Rol, k.EsCariKodu, esKartAd = string.IsNullOrEmpty(k.EsCariKodu) ? "" : (CariKartOrnek.KartBul(k.EsCariKodu)?.KisaAd ?? ""), k.Yetkili, k.Telefon, k.Eposta, k.VadeSarti, k.Durum, durumKaynagi = k.DurumKaynagi, k.Not, k.Sirket, olusturmaTarihi = Trh(k.OlusturmaTarihi), k.Olusturan, anaFaaliyet = k.AnaFaaliyet, temsilciler = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()), faaliyetler = k.Faaliyetler.Select(f => new { f.Sira, f.AnaGrup, f.Grup, f.SatisTemsilcisi, f.AnaFaaliyet, f.UrunSinifi, f.UrunGrubu, f.UrunSerisi, f.UrunAdi, f.Durum, baslangicTr = Trh(f.BaslangicTarihi), f.Not }) }
            };
        }
    }
}
