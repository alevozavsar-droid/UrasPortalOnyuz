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
    /// İK ve İdari İşler talep modülleri (Talep › İK ve İdari İşler): İzin (Data/IzinOrnek.cs), Nakit Avans (Data/NakitAvansOrnek.cs),
    /// Mesai / Belge / Seyahat / Araç (Data/TalepMotoru.cs + TalepModulleri.cs). Kayıtlar bellek içindedir; SQL / SAP / e-posta yoktur.
    /// </summary>
    [Authorize]
    public class IkTalepController : Controller
    {
        // ---------- Sayfalar ----------
        // =====================================================================
        // TALEP MOTORU — Mesai, Belge, Seyahat, Araç (Data/TalepMotoru.cs + TalepModulleri.cs; Views/IkTalep/Modul.cshtml; js/talep-modulu.js)
        // =====================================================================
        public IActionResult Mesai(string sekme, string no) => TmSayfa("MESAI", sekme, no);
        public IActionResult Belge(string sekme, string no) => TmSayfa("BELGE", sekme, no);
        public IActionResult Seyahat(string sekme, string no) => TmSayfa("SEYAHAT", sekme, no);
        public IActionResult Arac(string sekme, string no) => TmSayfa("ARAC", sekme, no);
        public IActionResult MesaiTakip() => TmSayfa("MESAI", "takip", null);
        public IActionResult SeyahatTakvim() => TmSayfa("SEYAHAT", "takvim", null);
        public IActionResult AracTakvim() => TmSayfa("ARAC", "takvim", null);
        public IActionResult TalepOnay(string kod) => TmSayfa(string.IsNullOrEmpty(kod) ? "MESAI" : kod, "onay", null);
        private IActionResult TmSayfa(string kod, string sekme, string no)
        {
            var m = TalepMotoru.Modul(kod); if (m == null) return NotFound();
            ViewData["Title"] = m.Ad; ViewBag.Kod = m.Kod; ViewBag.Sekme = string.IsNullOrEmpty(sekme) ? (string.IsNullOrEmpty(no) ? "yeni" : "detay") : sekme; ViewBag.No = no ?? "";
            ViewBag.Sekmeler = new[] { ("/IkTalep/Izin", "İzin Talebi", "fa-umbrella-beach", "IZIN"), ("/IkTalep/Mesai", "Mesai Talebi", "fa-business-time", "MESAI"), ("/IkTalep/Avans", "Avans Talebi", "fa-hand-holding-dollar", "AVANS"), ("/IkTalep/Belge", "Belge Talebi", "fa-file-signature", "BELGE"), ("/IkTalep/Seyahat", "Seyahat Talebi", "fa-plane-departure", "SEYAHAT"), ("/IkTalep/Arac", "Araç Talebi", "fa-car-side", "ARAC") };
            return View("Modul");
        }

        private static object TmDto(TmTalep a)
        {
            var m = a.Modul; NaKisi kisi2 = string.IsNullOrEmpty(m.IkinciKisiAlan) ? null : TalepMotoru.KisiBul(a.V(m.IkinciKisiAlan));
            return new
            {
                a.Id, a.No, Tarih = a.Tarih.ToString("dd.MM.yyyy", Tr), Saat = a.Tarih.ToString("HH:mm"), TarihIso = a.Tarih.ToString("yyyy-MM-dd"), Sirket = a.Kullanan.Sirket, Departman = a.Kullanan.Departman,
                Olusturan = Kisi(a.Olusturan), Kullanan = Kisi(a.Kullanan), Kisi2 = Kisi(kisi2), a.TurKod, TurAdi = a.Tur?.Ad, a.Degerler, a.Hesap, a.Ek,
                a.TalepDurumu, a.Durum2, a.Durum3, a.Onaylandi, a.Iptal, a.IptalNedeni, a.Revizyon, a.Uyarilar, a.Etiketler, a.Ozet, a.EntegrasyonDurumu, SonSenkron = a.SonSenkron?.ToString("dd.MM.yyyy HH:mm", Tr), OnayTarihi = a.OnayTarihi?.ToString("dd.MM.yyyy", Tr),
                Onay = a.Onaylar.Where(o => o.Sira > 1).ToDictionary(o => o.Adim, o => o.Kisi),
                Ekler = a.Ekler.Select(e => new { e.Ad, e.Tur, e.Boyut, Tarih = e.Tarih.ToString("dd.MM.yyyy HH:mm", Tr), e.Yukleyen }),
                Onaylar = a.Onaylar.Select(o => new { o.Sira, o.Adim, o.Kisi, o.Durum, Tarih = o.Tarih?.ToString("dd.MM.yyyy HH:mm", Tr), o.Not }),
                Zaman = a.Zaman.OrderBy(z => z.Tarih).Select(z => new { Tarih = z.Tarih.ToString("dd.MM.yyyy HH:mm", Tr), z.Hareket, z.Kaynak }),
                Audit = a.Audit.OrderByDescending(x => x.Tarih).Select(x => new { Tarih = x.Tarih.ToString("dd.MM.yyyy", Tr), Saat = x.Tarih.ToString("HH:mm:ss"), x.Tablo, x.Alan, x.Eski, x.Yeni, x.Kullanici, x.IslemTipi, x.KaynakSistem })
            };
        }
        private static object TmModulDto(TmModul m) => new
        {
            m.Kod, m.Ad, m.NoOnEk, m.Ikon, m.Renk, m.Aciklama, m.AkisOzeti, m.KullananEtiket, m.Durum2Ad, m.Durum3Ad, m.IkinciKisiAlan, m.IkinciKisiEtiket, m.TakvimSatir, m.TakvimBas, m.TakvimBit, m.TakvimAd,
            m.Turler, m.Alanlar, m.Olaylar, m.HesapEtiketler, m.EkEtiketler, TakipSutunlar = m.TakipSutunlar.Select(s => new { s.Baslik, s.Anahtar }), m.Parametreler, m.ParametreEtiketler, m.Kurallar, m.Master
        };

        [HttpGet]
        public IActionResult TmVeri(string kod)
        {
            var m = TalepMotoru.Modul(kod); if (m == null) return Json(new { success = false, message = "Modül yok." });
            lock (TalepMotoru.Kilit)
            {
                var l = TalepMotoru.Talepler.Where(t => t.ModulKod == m.Kod).ToList();
                return Json(new { ben = Kisi(Ben()), personel = NakitAvansOrnek.Personel.Select(Kisi), sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct(), modul = TmModulDto(m), kpiler = m.Kpiler(l), talepler = l.OrderByDescending(t => t.Id).Select(TmDto) });
            }
        }

        private TmTalep TmOku(Dictionary<string, string> d, TmTalep mevcut = null)
        {
            string V(string k) => d != null && d.TryGetValue(k, out var v) ? (v ?? "").Trim() : "";
            var ben = Ben(); var m = TalepMotoru.Modul(V("kod")) ?? mevcut?.Modul;
            var a = mevcut ?? new TmTalep { ModulKod = m?.Kod, Olusturan = ben };
            if (mevcut == null) { a.Kullanan = TalepMotoru.KisiBul(V("kullanan")) ?? ben; a.TurKod = V("_tur"); foreach (var al in m.Alanlar) a.Degerler[al.Ad] = V(al.Ad); }
            return a;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult TmOnizleme([FromBody] Dictionary<string, string> d)
        {
            var a = TmOku(d); if (a.Modul == null) return Json(new { success = false, message = "Modül yok." });
            TmSonuc s; lock (TalepMotoru.Kilit) s = TalepMotoru.Hazirla(a, true);
            var akis = s.Hata == null ? TalepMotoru.OnayAkisiOlustur(a, DateTime.Now, s.EkAdimlar).Select(o => new { o.Adim, o.Kisi }) : null;
            return Json(new { success = s.Hata == null, message = s.Hata, hesap = a.Hesap, uyarilar = s.Uyarilar, akis, ozet = s.Hata == null ? a.Ozet : null });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult TmOlustur([FromBody] Dictionary<string, string> d)
        {
            var a = TmOku(d); if (a.Modul == null) return Json(new { success = false, message = "Modül yok." });
            foreach (var ek in (d.TryGetValue("ekler", out var eks) ? eks : "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) a.Ekler.Add(new NaEk { Ad = ek, Tur = "Belge", Boyut = "—", Tarih = DateTime.Now, Yukleyen = a.Olusturan.AdSoyad });
            string hata; TmTalep kayit; lock (TalepMotoru.Kilit) hata = TalepMotoru.Olustur(a, out kayit);
            if (hata != null) return Json(new { success = false, message = hata });
            return Json(new { success = true, no = kayit.No, talep = TmDto(kayit), message = $"{kayit.No} oluşturuldu ({kayit.Tur.Ad}). Onay akışı: {string.Join(" → ", kayit.Onaylar.Where(o => o.Sira > 1).Select(o => o.Adim))}." + (kayit.Uyarilar.Count > 0 ? " Uyarı: " + string.Join(" ", kayit.Uyarilar) : "") });
        }

        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult TmOnay([FromBody] Dictionary<string, string> d) => TmIslem(d, a => TalepMotoru.OnayEylemi(a, d["eylem"], d.TryGetValue("not", out var n) ? n : "", User?.Identity?.Name));
        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult TmRevize([FromBody] Dictionary<string, string> d) => TmIslem(d, a => TalepMotoru.Revize(a, d.Where(kv => kv.Key != "no" && kv.Key != "kod").ToDictionary(kv => kv.Key, kv => kv.Value), User?.Identity?.Name));
        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult TmIptal([FromBody] Dictionary<string, string> d) => TmIslem(d, a => TalepMotoru.IptalEt(a, d.TryGetValue("neden", out var n) ? n : "", OrnekVeri.KullaniciAdi(User?.Identity?.Name), t =>
        {
            if (t.Ek.Count > 0) return "Kaynak sistemde hareket başlamış talep iptal edilemez; ilgili birim (İdari İşler / İK / Filo) kapatır.";
            var basAlan = t.Modul.TakvimBas ?? "tarih"; var b = t.T(basAlan);
            return t.Onaylandi && b.HasValue && b.Value.Date <= DateTime.Today ? "Başlamış talep iptal edilemez." : null;
        }));
        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult TmOlay([FromBody] Dictionary<string, string> d) => TmIslem(d, a => TalepMotoru.OlayIsle(a, d.TryGetValue("olay", out var o) ? o : "", d.TryGetValue("islemId", out var i) ? i : "", d.Where(kv => kv.Key.StartsWith("g_")).ToDictionary(kv => kv.Key.Substring(2), kv => kv.Value), OrnekVeri.KullaniciAdi(User?.Identity?.Name)));

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult TmParametre([FromBody] Dictionary<string, string> d)
        {
            var m = TalepMotoru.Modul(d.TryGetValue("kod", out var k) ? k : ""); if (m == null) return Json(new { success = false, message = "Modül yok." });
            foreach (var kv in d.Where(x => x.Key.StartsWith("p_"))) { var ad = kv.Key.Substring(2); if (m.Parametreler.ContainsKey(ad) && !string.IsNullOrWhiteSpace(kv.Value)) m.Parametreler[ad] = kv.Value.Replace(',', '.'); }
            return Json(new { success = true, parametreler = m.Parametreler });
        }

        private IActionResult TmIslem(Dictionary<string, string> d, Func<TmTalep, string> f)
        {
            if (d == null || !d.TryGetValue("no", out var no)) return Json(new { success = false, message = "Talep No eksik." });
            TmTalep a; lock (TalepMotoru.Kilit) a = TalepMotoru.Talepler.FirstOrDefault(x => x.No == no);
            if (a == null) return Json(new { success = false, message = "Talep bulunamadı: " + no });
            string hata; object kpi;
            lock (TalepMotoru.Kilit) { hata = f(a); kpi = a.Modul.Kpiler(TalepMotoru.Talepler.Where(t => t.ModulKod == a.ModulKod).ToList()); }
            return hata != null ? Json(new { success = false, message = hata, talep = TmDto(a), kpiler = kpi }) : Json(new { success = true, talep = TmDto(a), kpiler = kpi });
        }

        // =====================================================================
        // İZİN MODÜLÜ (Views/IkTalep/Izin.cshtml + wwwroot/js/izin-talep.js; kurallar ve örnek veri Data/IzinOrnek.cs)
        // =====================================================================
        public IActionResult Izin(string sekme, string no)
        {
            ViewData["Title"] = "İzin Talebi";
            ViewBag.Sekme = string.IsNullOrEmpty(sekme) ? (string.IsNullOrEmpty(no) ? "yeni" : "detay") : sekme;
            ViewBag.No = no ?? "";
            return View("Izin");
        }
        public IActionResult IzinTakvim() { ViewData["Title"] = "Ekip İzin Takvimi"; ViewBag.Sekme = "takvim"; ViewBag.No = ""; return View("Izin"); }
        public IActionResult IzinOnay() { ViewData["Title"] = "İzin Onay Bekleyenler"; ViewBag.Sekme = "onay"; ViewBag.No = ""; return View("Izin"); }
        public IActionResult IzinTakip() { ViewData["Title"] = "İzin Takip Raporu"; ViewBag.Sekme = "takip"; ViewBag.No = ""; return View("Izin"); }

        private static object IzDto(IzinTalebi a) => new
        {
            a.Id, a.No, Tarih = a.Tarih.ToString("dd.MM.yyyy", Tr), Saat = a.Tarih.ToString("HH:mm"), a.Sirket, a.Departman,
            Olusturan = Kisi(a.Olusturan), Kullanan = Kisi(a.Kullanan), Vekil = Kisi(a.Vekil),
            a.IzinTuru, TurAdi = a.Tur?.Ad, Ucretli = a.Tur?.Ucretli, BakiyedenDuser = a.Tur?.BakiyedenDuser, BelgeGerekli = a.Tur?.BelgeGerekli,
            Baslangic = a.Baslangic.ToString("dd.MM.yyyy", Tr), Bitis = a.Bitis.ToString("dd.MM.yyyy", Tr), BaslangicIso = a.Baslangic.ToString("yyyy-MM-dd"), BitisIso = a.Bitis.ToString("yyyy-MM-dd"),
            a.YarimGun, a.IsGunu, a.TakvimGunu, a.KullanilanGun, a.Iletisim, a.Adres, a.Aciklama, a.Revizyon, a.Iptal, a.IptalNedeni, a.Uyarilar,
            a.TalepDurumu, a.KullanimDurumu, a.PuantajDurumu, a.Onaylandi, a.Izinde, a.Gecmis, a.BelgeEksik, a.DonusTeyidi, DonusTeyitTarihi = a.DonusTeyitTarihi?.ToString("dd.MM.yyyy HH:mm", Tr),
            FiiliBaslangic = a.FiiliBaslangic?.ToString("dd.MM.yyyy", Tr), FiiliDonus = a.FiiliDonus?.ToString("dd.MM.yyyy", Tr), a.SgkRaporNo, a.BelgeAlindi, a.PuantajDonemi, PuantajTarihi = a.PuantajTarihi?.ToString("dd.MM.yyyy", Tr), a.PuantajGun,
            a.EntegrasyonDurumu, SonSenkron = a.SonSenkron?.ToString("dd.MM.yyyy HH:mm", Tr), a.Yonetici, a.Direktor, a.Ik, OnayTarihi = a.OnayTarihi?.ToString("dd.MM.yyyy", Tr),
            Ekler = a.Ekler.Select(e => new { e.Ad, e.Tur, e.Boyut, Tarih = e.Tarih.ToString("dd.MM.yyyy HH:mm", Tr), e.Yukleyen }),
            Onaylar = a.Onaylar.Select(o => new { o.Sira, o.Adim, o.Kisi, o.Durum, Tarih = o.Tarih?.ToString("dd.MM.yyyy HH:mm", Tr), o.Not }),
            Zaman = a.Zaman.OrderBy(z => z.Tarih).Select(z => new { Tarih = z.Tarih.ToString("dd.MM.yyyy HH:mm", Tr), z.Hareket, z.Kaynak }),
            Audit = a.Audit.OrderByDescending(x => x.Tarih).Select(x => new { Tarih = x.Tarih.ToString("dd.MM.yyyy", Tr), Saat = x.Tarih.ToString("HH:mm:ss"), x.Tablo, x.Alan, x.Eski, x.Yeni, x.Kullanici, x.IslemTipi, x.KaynakSistem })
        };

        private static object BakiyeDto(IzBakiye b) => new
        {
            PersonelNo = b.Kisi.PersonelNo, IseGiris = b.IseGiris.ToString("dd.MM.yyyy", Tr), b.KidemYil, b.HakEdis, b.Devir, b.Kullanilan, b.Planlanan, b.OnayBekleyen, b.Kalan, b.KullanilabilirNet,
            SonrakiHakEdis = b.SonrakiHakEdis.ToString("dd.MM.yyyy", Tr), b.SonrakiHakEdisGun, b.MazeretKullanilan, b.RaporluGun,
            Hareketler = b.Hareketler.Select(h => new { Tarih = h.Tarih.ToString("dd.MM.yyyy", Tr), h.Tur, h.Gun, h.Ref, h.Aciklama })
        };

        [HttpGet]
        public IActionResult IzinVeri()
        {
            lock (IzinOrnek.Kilit)
                return Json(new
                {
                    ben = Kisi(Ben()), personel = NakitAvansOrnek.Personel.Select(Kisi),
                    turler = IzinOrnek.Turler, yarimGun = new[] { "Yok", "Sabah", "Öğleden Sonra" },
                    tatiller = IzinOrnek.Tatiller.Select(t => new { Tarih = t.Key.ToString("yyyy-MM-dd"), Ad = t.Value }),
                    bakiyeler = NakitAvansOrnek.Personel.Select(p => BakiyeDto(IzinOrnek.Bakiye(p))),
                    politika = IzinOrnek.BakiyePolitikasi, dolulukEsigi = IzinOrnek.DolulukEsigi, uzunIzinEsigi = IzinOrnek.UzunIzinEsigi, devirUstSinir = IzinOrnek.DevirUstSinir,
                    talepler = IzinOrnek.Talepler.OrderByDescending(t => t.Id).Select(IzDto)
                });
        }

        /// <summary>Form önizlemesi: iş günü, tatiller, dönüş günü, onay akışı ve kural uyarıları (kayıt yazmaz).</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult IzinOnizleme([FromBody] Dictionary<string, string> d)
        {
            var a = IzinOku(d, out var hata);
            if (hata != null) return Json(new { success = false, message = hata });
            a.IsGunu = IzinOrnek.IsGunuSay(a.Baslangic, a.Bitis, a.YarimGun); a.TakvimGunu = (a.Bitis.Date - a.Baslangic.Date).Days + 1;
            var donus = a.Bitis.AddDays(1); while (!IzinOrnek.IsGunuMu(donus)) donus = donus.AddDays(1);
            List<string> uy; List<IzOnay> akis;
            lock (IzinOrnek.Kilit) akis = IzinOrnek.OnayAkisiOlustur(a, DateTime.Now, out uy);
            var cak = IzinOrnek.Cakisanlar(a.Kullanan, a.Baslangic, a.Bitis);
            var dep = IzinOrnek.DepartmanIzinlileri(a.Kullanan, a.Baslangic, a.Bitis);
            return Json(new
            {
                success = true, a.IsGunu, a.TakvimGunu, Donus = donus.ToString("dd.MM.yyyy dddd", Tr),
                Tatiller = IzinOrnek.Tatiller.Where(t => t.Key >= a.Baslangic.Date && t.Key <= a.Bitis.Date).Select(t => t.Key.ToString("dd.MM") + " " + t.Value),
                HaftaSonu = Enumerable.Range(0, a.TakvimGunu).Select(i => a.Baslangic.AddDays(i)).Count(g => g.DayOfWeek == DayOfWeek.Saturday || g.DayOfWeek == DayOfWeek.Sunday),
                Akis = akis.Select(o => new { o.Adim, o.Kisi }), Uyarilar = uy, Cakisma = cak.Select(c => c.No), Mevcut = IzinOrnek.DepartmanMevcudu(a.Kullanan),
                DepartmanIzinli = dep.Select(x => new { x.Kullanan.AdSoyad, Tur = x.Tur?.Ad, Baslangic = x.Baslangic.ToString("dd.MM"), Bitis = x.Bitis.ToString("dd.MM") }),
                Bakiye = BakiyeDto(IzinOrnek.Bakiye(a.Kullanan))
            });
        }

        private IzinTalebi IzinOku(Dictionary<string, string> d, out string hata)
        {
            hata = null;
            string V(string k) => d != null && d.TryGetValue(k, out var v) ? (v ?? "").Trim() : "";
            var ben = Ben();
            var a = new IzinTalebi { Olusturan = ben, Kullanan = IzinOrnek.KisiBul(V("kullanan")) ?? ben, IzinTuru = V("izinTuru"), YarimGun = string.IsNullOrEmpty(V("yarimGun")) ? "Yok" : V("yarimGun"), Iletisim = V("iletisim"), Adres = V("adres"), Aciklama = V("aciklama"), Vekil = string.IsNullOrEmpty(V("vekil")) ? null : IzinOrnek.KisiBul(V("vekil")) };
            if (!DateTime.TryParse(V("baslangic"), out var b) || !DateTime.TryParse(V("bitis"), out var e)) { hata = "Başlangıç ve bitiş tarihi zorunludur."; return a; }
            a.Baslangic = b.Date; a.Bitis = e.Date;
            if (a.Tur == null) hata = "İzin türü seçilmelidir.";
            return a;
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult IzinOlustur([FromBody] Dictionary<string, string> d)
        {
            var a = IzinOku(d, out var hata);
            if (hata != null) return Json(new { success = false, message = hata });
            if (string.IsNullOrEmpty(a.Aciklama) && (a.Tur.Kod == "MAZERET" || a.Tur.Kod == "UCRETSIZ")) return Json(new { success = false, message = "Bu izin türünde açıklama / gerekçe zorunludur." });
            foreach (var ek in (d.TryGetValue("ekler", out var eks) ? eks : "").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                a.Ekler.Add(new IzEk { Ad = ek, Tur = "Belge", Boyut = "—", Tarih = DateTime.Now, Yukleyen = a.Olusturan.AdSoyad });
            if (a.Ekler.Count > 0 && a.Tur.BelgeGerekli) a.BelgeAlindi = true;
            hata = IzinOrnek.Olustur(a, out var kayit);
            if (hata != null) return Json(new { success = false, message = hata });
            return Json(new { success = true, no = kayit.No, talep = IzDto(kayit), message = $"{kayit.No} oluşturuldu ({kayit.Tur.Ad}, {kayit.IsGunu:0.#} iş günü). Onay akışı: {string.Join(" → ", kayit.Onaylar.Where(o => o.Sira > 1).Select(o => o.Adim))}." + (kayit.Uyarilar.Count > 0 ? " Uyarı: " + string.Join(" ", kayit.Uyarilar) : "") });
        }

        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult IzinOnayla([FromBody] Dictionary<string, string> d) => IzIslem(d, a => IzinOrnek.OnayEylemi(a, d["eylem"], d.TryGetValue("not", out var n) ? n : "", User?.Identity?.Name));
        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult IzinRevize([FromBody] Dictionary<string, string> d) => IzIslem(d, a => DateTime.TryParse(d.TryGetValue("baslangic", out var b) ? b : "", out var bd) && DateTime.TryParse(d.TryGetValue("bitis", out var e) ? e : "", out var ed) ? IzinOrnek.Revize(a, bd, ed, d.TryGetValue("yarimGun", out var y) ? y : "Yok", d.TryGetValue("aciklama", out var ac) ? ac : null, User?.Identity?.Name) : "Tarihler geçersiz.");
        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult IzinIptal([FromBody] Dictionary<string, string> d) => IzIslem(d, a => IzinOrnek.IptalEt(a, d.TryGetValue("neden", out var n) ? n : "", OrnekVeri.KullaniciAdi(User?.Identity?.Name)));
        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult IzinOlay([FromBody] Dictionary<string, string> d) => IzIslem(d, a => IzinOrnek.Olay(a, d.TryGetValue("olay", out var o) ? o : "", d.TryGetValue("islemId", out var i) ? i : "", d.TryGetValue("ek1", out var e1) ? e1 : null, d.TryGetValue("ek2", out var e2) ? e2 : null, OrnekVeri.KullaniciAdi(User?.Identity?.Name)));
        [HttpPost] [IgnoreAntiforgeryToken] public IActionResult IzinDonusTeyidi([FromBody] Dictionary<string, string> d) => IzIslem(d, a => IzinOrnek.DonusTeyidi(a, User?.Identity?.Name));

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult IzinParametre([FromBody] Dictionary<string, string> d)
        {
            if (d.TryGetValue("politika", out var p) && !string.IsNullOrEmpty(p)) IzinOrnek.BakiyePolitikasi = p;
            if (d.TryGetValue("doluluk", out var dl) && decimal.TryParse(dl, NumberStyles.Any, CultureInfo.InvariantCulture, out var dv) && dv > 0 && dv <= 1) IzinOrnek.DolulukEsigi = dv;
            if (d.TryGetValue("uzun", out var uz) && decimal.TryParse(uz, NumberStyles.Any, CultureInfo.InvariantCulture, out var uv) && uv > 0) IzinOrnek.UzunIzinEsigi = uv;
            return Json(new { success = true, politika = IzinOrnek.BakiyePolitikasi, dolulukEsigi = IzinOrnek.DolulukEsigi, uzunIzinEsigi = IzinOrnek.UzunIzinEsigi });
        }

        private IActionResult IzIslem(Dictionary<string, string> d, Func<IzinTalebi, string> f)
        {
            if (d == null || !d.TryGetValue("no", out var no)) return Json(new { success = false, message = "Talep No eksik." });
            IzinTalebi a;
            lock (IzinOrnek.Kilit) a = IzinOrnek.Talepler.FirstOrDefault(x => x.No == no);
            if (a == null) return Json(new { success = false, message = "Talep bulunamadı: " + no });
            string hata;
            lock (IzinOrnek.Kilit) hata = f(a);
            object bak; lock (IzinOrnek.Kilit) bak = BakiyeDto(IzinOrnek.Bakiye(a.Kullanan));
            return hata != null ? Json(new { success = false, message = hata, talep = IzDto(a), bakiye = bak }) : Json(new { success = true, talep = IzDto(a), bakiye = bak });
        }
        /// <summary>Varsayılan: izin formu.</summary>
        public IActionResult Index() => RedirectToAction(nameof(Izin));

        // =====================================================================
        // NAKİT AVANS MODÜLÜ (Views/IkTalep/Avans.cshtml + wwwroot/js/nakit-avans.js; veri ve kurallar Data/NakitAvansOrnek.cs)
        // Şartname: "Nakit Avans Talep, Ödeme, Mahsup ve Kapatma Modülü — IT Analiz ve Geliştirme Gereksinimleri v1.0"
        // =====================================================================
        public IActionResult Avans(string sekme, string no)
        {
            ViewData["Title"] = "Nakit Avans Talebi";
            ViewBag.Sekme = string.IsNullOrEmpty(sekme) ? (string.IsNullOrEmpty(no) ? "yeni" : "detay") : sekme;
            ViewBag.No = no ?? "";
            return View("Avans");
        }
        /// <summary>§10 Finans — Bekleyen Ödemeler ekranı (aynı görünüm, sekme).</summary>
        public IActionResult AvansBekleyenOdemeler() { ViewData["Title"] = "Nakit Avans — Bekleyen Ödemeler"; ViewBag.Sekme = "bekleyen"; ViewBag.No = ""; return View("Avans"); }
        /// <summary>§25 Nakit Avans Takip Raporu.</summary>
        public IActionResult AvansTakip() { ViewData["Title"] = "Nakit Avans Takip Raporu"; ViewBag.Sekme = "takip"; ViewBag.No = ""; return View("Avans"); }

        private NaKisi Ben() => NakitAvansOrnek.KisiBul(User?.Identity?.Name) ?? NakitAvansOrnek.Personel[0];

        private static object Kisi(NaKisi k) => k == null ? null : new { k.PersonelNo, k.AdSoyad, k.Sirket, k.Departman, k.Unvan, k.MasrafMerkezi, k.Aktif, k.KullaniciId, k.Rol };

        private static object Dto(NakitAvans a) => new
        {
            a.Id, a.No, Tarih = a.Tarih.ToString("dd.MM.yyyy", Tr), Saat = a.Tarih.ToString("HH:mm"), TarihIso = a.Tarih.ToString("s"),
            a.Sirket, a.Departman, a.MasrafMerkezi, Olusturan = Kisi(a.Olusturan), Kullanan = Kisi(a.Kullanan),
            a.TalepTuru, a.Konu, a.Aciklama, a.Tutar, a.ParaBirimi, TutarTl = NakitAvansOrnek.TlKarsiligi(a.Tutar, a.ParaBirimi),
            KullanimTarihi = a.KullanimTarihi.ToString("dd.MM.yyyy", Tr), KapatmaTarihi = a.KapatmaTarihi.ToString("dd.MM.yyyy", Tr),
            KullanimIso = a.KullanimTarihi.ToString("yyyy-MM-dd"), KapatmaIso = a.KapatmaTarihi.ToString("yyyy-MM-dd"),
            a.FaturaDurumu, a.BelgeTurleri, a.HarcamaKategorisi, a.Oncelik, a.Revizyon,
            Ekler = a.Ekler.Select(e => new { e.Ad, e.Tur, e.Boyut, Tarih = e.Tarih.ToString("dd.MM.yyyy HH:mm", Tr), e.Yukleyen }),
            a.TalepDurumu, a.OdemeDurumu, a.KapatmaDurumu, a.Onaylandi, a.Kapandi, a.Gecikmis, a.GecikenGun, a.BekleyenGun, a.GrupIci,
            a.OnaylananTutar, a.TeslimEdilen, a.MahsupEdilen, a.KontrolBekleyen, a.IadeEdilen, a.AcikTutar, a.OdemeBekleyen,
            SonIslem = a.SonIslem?.ToString("dd.MM.yyyy HH:mm", Tr), FiiliKapatma = a.FiiliKapatma?.ToString("dd.MM.yyyy", Tr), OnayTarihi = a.OnayTarihi?.ToString("dd.MM.yyyy", Tr),
            a.Yonetici, a.UstOnay, a.FinansOnayi, a.MuhasebeFisNo, a.EntegrasyonDurumu, SonSenkron = a.SonSenkron?.ToString("dd.MM.yyyy HH:mm", Tr),
            Onaylar = a.Onaylar.Select(o => new { o.Sira, o.Adim, o.Kisi, o.Durum, Tarih = o.Tarih?.ToString("dd.MM.yyyy HH:mm", Tr), o.Not }),
            Odemeler = a.Odemeler.Select(o => new { o.IslemId, Tarih = o.Tarih.ToString("dd.MM.yyyy HH:mm", Tr), o.Tutar, o.ParaBirimi, o.Kasa, o.FisNo, o.TeslimEden, o.TeslimAlan, o.TeslimSekli, o.Durum, o.TeslimAlanOnayi, TeslimOnayTarihi = o.TeslimOnayTarihi?.ToString("dd.MM.yyyy HH:mm", Tr) }),
            Harcamalar = a.Harcamalar.Select(h => new { h.BelgeId, Tarih = h.Tarih.ToString("dd.MM.yyyy", Tr), h.BelgeNo, h.Tedarikci, h.BelgeTuru, h.Tutar, h.MahsupDurumu, h.MuhasebeFisNo, h.YevmiyeNo, h.HesapKodu }),
            Iadeler = a.Iadeler.Select(i => new { i.IslemId, Tarih = i.Tarih.ToString("dd.MM.yyyy HH:mm", Tr), i.Tutar, i.Kasa, i.FisNo, i.IadeyiAlan }),
            Yansitma = a.Yansitma == null ? null : new { a.Yansitma.IslemTipi, a.Yansitma.OdeyenSirket, a.Yansitma.MasrafSirketi, a.Yansitma.Tutar, a.Yansitma.BelgeNo, a.Yansitma.Durum, Kullanan = a.Kullanan.AdSoyad, a.MasrafMerkezi },
            Zaman = a.Zaman.OrderBy(z => z.Tarih).Select(z => new { Tarih = z.Tarih.ToString("dd.MM.yyyy HH:mm", Tr), z.Hareket, z.Kaynak }),
            Audit = a.Audit.OrderByDescending(x => x.Tarih).Select(x => new { Tarih = x.Tarih.ToString("dd.MM.yyyy", Tr), Saat = x.Tarih.ToString("HH:mm:ss"), x.Tablo, x.Alan, x.Eski, x.Yeni, x.Kullanici, x.IslemTipi, x.KaynakSistem })
        };

        /// <summary>Ekranın tüm verisi: kullanıcı, personel master, parametreler, talepler.</summary>
        [HttpGet]
        public IActionResult AvansVeri()
        {
            lock (NakitAvansOrnek.Kilit)
                return Json(new
                {
                    ben = Kisi(Ben()),
                    personel = NakitAvansOrnek.Personel.Select(Kisi),
                    sirketler = OrnekVeri.Sirketler.Select(s => s.Display).Distinct(),
                    paraBirimleri = NakitAvansOrnek.ParaBirimleri, oncelikler = NakitAvansOrnek.Oncelikler, faturaDurumlari = NakitAvansOrnek.FaturaDurumlari,
                    belgeTurleri = NakitAvansOrnek.BelgeTurleri, kategoriler = NakitAvansOrnek.HarcamaKategorileri, kasalar = NakitAvansOrnek.Kasalar,
                    limitler = NakitAvansOrnek.LimitMaster.Select(l => new { l.Min, l.Max, l.Yonetici, l.UstYonetim, l.Finans }),
                    ustOnayEsigi = NakitAvansOrnek.UstOnayEsigi, politika = NakitAvansOrnek.AcikAvansPolitikasi,
                    talepler = NakitAvansOrnek.Talepler.OrderByDescending(t => t.Id).Select(Dto)
                });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AvansOlustur([FromBody] Dictionary<string, string> d)
        {
            string V(string k) => d != null && d.TryGetValue(k, out var v) ? (v ?? "").Trim() : "";
            var ben = Ben();
            var kullanan = NakitAvansOrnek.KisiBul(V("kullanan")) ?? ben;
            var a = new NakitAvans
            {
                Olusturan = ben, Kullanan = kullanan, Sirket = string.IsNullOrEmpty(V("sirket")) ? ben.Sirket : V("sirket"),
                Konu = V("konu"), Aciklama = V("aciklama"), ParaBirimi = string.IsNullOrEmpty(V("paraBirimi")) ? "TL" : V("paraBirimi"),
                FaturaDurumu = V("faturaDurumu"), HarcamaKategorisi = V("kategori"), Oncelik = string.IsNullOrEmpty(V("oncelik")) ? "Normal" : V("oncelik"),
                BelgeTurleri = V("belgeTurleri").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries).ToList()
            };
            decimal.TryParse(V("tutar"), NumberStyles.Any, CultureInfo.InvariantCulture, out var tutar); a.Tutar = tutar;
            DateTime.TryParse(V("kullanimTarihi"), out var kt); DateTime.TryParse(V("kapatmaTarihi"), out var kp);
            a.KullanimTarihi = kt == default ? DateTime.Today : kt; a.KapatmaTarihi = kp == default ? a.KullanimTarihi.AddDays(7) : kp;
            var eksik = new List<string>();
            if (string.IsNullOrEmpty(a.Konu)) eksik.Add("Talep Konusu"); if (string.IsNullOrEmpty(a.Aciklama)) eksik.Add("Detaylı Açıklama");
            if (tutar <= 0) eksik.Add("Talep Tutarı"); if (string.IsNullOrEmpty(a.FaturaDurumu)) eksik.Add("Fatura Durumu");
            if (a.BelgeTurleri.Count == 0) eksik.Add("Beklenen Belge Türü"); if (string.IsNullOrEmpty(a.HarcamaKategorisi)) eksik.Add("Harcama Kategorisi");
            if (eksik.Count > 0) return Json(new { success = false, message = "Zorunlu alanlar boş: " + string.Join(", ", eksik) });
            foreach (var ek in V("ekler").Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
                a.Ekler.Add(new NaEk { Ad = ek, Tur = ek.EndsWith(".pdf") ? "Teklif" : ek.EndsWith(".xlsx") ? "Liste" : ek.EndsWith(".png") || ek.EndsWith(".jpg") ? "Görsel" : "Belge", Boyut = "—", Tarih = DateTime.Now, Yukleyen = ben.AdSoyad });
            var hata = NakitAvansOrnek.Olustur(a, out var kayit);
            if (hata != null) return Json(new { success = false, message = hata });
            var uyari = NakitAvansOrnek.AcikAvanslar(kullanan).Where(x => x.Id != kayit.Id).ToList();
            return Json(new { success = true, no = kayit.No, talep = Dto(kayit), message = $"{kayit.No} oluşturuldu. Onay akışı: {string.Join(" → ", kayit.Onaylar.Where(o => o.Sira > 1).Select(o => o.Adim))}." + (uyari.Count > 0 ? $" Uyarı: {kullanan.AdSoyad} adına {uyari.Count} açık avans var." : "") });
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AvansOnay([FromBody] Dictionary<string, string> d) => Islem(d, a => NakitAvansOrnek.OnayEylemi(a, d["eylem"], d.TryGetValue("not", out var n) ? n : "", User?.Identity?.Name));

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AvansRevize([FromBody] Dictionary<string, string> d) => Islem(d, a =>
        {
            decimal.TryParse(d.TryGetValue("tutar", out var t) ? t : "", NumberStyles.Any, CultureInfo.InvariantCulture, out var tutar);
            return NakitAvansOrnek.Revize(a, tutar, d.TryGetValue("konu", out var k) ? k : null, d.TryGetValue("aciklama", out var ac) ? ac : null, User?.Identity?.Name);
        });

        /// <summary>§30 kaynak sistem olayı (Finans/Kasa/Muhasebe/ERP taklidi). Ekranda "olay simülasyonu" olarak kullanılır.</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AvansOlay([FromBody] Dictionary<string, string> d) => Islem(d, a =>
        {
            decimal.TryParse(d.TryGetValue("tutar", out var t) ? t : "", NumberStyles.Any, CultureInfo.InvariantCulture, out var tutar);
            string G(string k) => d.TryGetValue(k, out var v) ? v : null;
            return NakitAvansOrnek.Olay(a, G("olay"), G("islemId"), tutar, G("ek1"), G("ek2"), G("ek3"), User?.Identity?.Name);
        });

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AvansTeslimTeyidi([FromBody] Dictionary<string, string> d) => Islem(d, a => NakitAvansOrnek.TeslimTeyidi(a, d.TryGetValue("islemId", out var i) ? i : "", User?.Identity?.Name));

        /// <summary>§18.1 / §8.2 parametreler (açık avans politikası, üst onay eşiği).</summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public IActionResult AvansParametre([FromBody] Dictionary<string, string> d)
        {
            if (d.TryGetValue("politika", out var p) && !string.IsNullOrEmpty(p)) NakitAvansOrnek.AcikAvansPolitikasi = p;
            if (d.TryGetValue("esik", out var e) && decimal.TryParse(e, NumberStyles.Any, CultureInfo.InvariantCulture, out var esik) && esik > 0) NakitAvansOrnek.UstOnayEsigi = esik;
            return Json(new { success = true, politika = NakitAvansOrnek.AcikAvansPolitikasi, ustOnayEsigi = NakitAvansOrnek.UstOnayEsigi });
        }

        private IActionResult Islem(Dictionary<string, string> d, Func<NakitAvans, string> f)
        {
            if (d == null || !d.TryGetValue("no", out var no)) return Json(new { success = false, message = "Talep No eksik." });
            NakitAvans a;
            lock (NakitAvansOrnek.Kilit) a = NakitAvansOrnek.Talepler.FirstOrDefault(x => x.No == no);
            if (a == null) return Json(new { success = false, message = "Talep bulunamadı: " + no });
            string hata;
            lock (NakitAvansOrnek.Kilit) hata = f(a);
            return hata != null ? Json(new { success = false, message = hata, talep = Dto(a) }) : Json(new { success = true, talep = Dto(a) });
        }

        // ---------- Yardımcılar ----------
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");

    }
}
