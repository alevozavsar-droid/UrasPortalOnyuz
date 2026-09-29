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
    /// Personel Maliyet Yansıtması — İK ve İdari İşler › İnsan Kaynakları › Personel Maliyet ve Bordro.
    /// SGK işvereni ≠ fiili görev yeri olan personelin aylık maliyeti, Gider Kataloğu 13 Personel ve İşçilik Giderleri kalemleriyle
    /// (GDR.13.AA.KK ↔ YNS.13.AA.KK) SGK işvereni şirketten fiili görev yeri şirkete yansıtılır; kayıt iki şirkete birden atılır.
    /// Kurallar ve veri: Data/PersonelYansitmaOrnek.cs (kaynak: Data/PersonelMaliyetOrnek.cs). Betik: wwwroot/js/personel-yansitma.js
    /// </summary>
    [Authorize]
    public class PersonelYansitmaController : Controller
    {
        private static readonly CultureInfo Tr = new CultureInfo("tr-TR");
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");

        /// <param name="sekme">hazirlik (varsayılan) | kayitlar | esleme</param>
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Personel Maliyet Yansıtması";
            ViewBag.Sekme = sekme == "kayitlar" || sekme == "esleme" ? sekme : "hazirlik";
            ViewBag.Donem = PersonelYansitmaOrnek.DonemBugun;
            return View();
        }

        /// <summary>Dönemin adayları (otomatik tespit), tüm kayıtlar, eşleme, katalog ve parametreler.</summary>
        [HttpGet]
        public IActionResult Veri(string donem)
        {
            if (string.IsNullOrWhiteSpace(donem)) donem = PersonelYansitmaOrnek.DonemBugun;
            lock (PersonelYansitmaOrnek.Kilit) return Json(Paket(donem, true, null));
        }

        public class OlusturIstek { public string Donem { get; set; } public List<int> Siralar { get; set; } }
        /// <summary>Seçilen personel için iki taraflı yansıtma kayıtlarını oluşturur.</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Olustur([FromBody] OlusturIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var (olusan, atlanan, kalemsiz, hata) = PersonelYansitmaOrnek.Olustur(d.Donem, d.Siralar, Ben());
            if (hata != null) return Json(new { success = false, message = hata });
            var notlar = new List<string>();
            if (atlanan > 0) notlar.Add($"{atlanan} kayıt bu dönemde zaten mevcut");
            if (kalemsiz > 0) notlar.Add($"{kalemsiz} personelde maliyet kalemi yok (tutarlar 0)");
            string msg = olusan.Count == 0 ? $"Yeni kayıt oluşmadı; {string.Join(", ", notlar)}." :
                $"{olusan.Count} yansıtma kaydı oluşturuldu ({olusan.Select(k => k.KaynakSirket + " → " + k.HedefSirket).Distinct().Count()} şirket çifti, {olusan.Sum(k => k.Toplam).ToString("N2", Tr)} TL); her kayıt SGK işvereni ve fiili görev yeri şirketine fiş olarak atıldı{(notlar.Count > 0 ? "; atlanan: " + string.Join(", ", notlar) : "")}.";
            lock (PersonelYansitmaOrnek.Kilit) return Json(Paket(d.Donem, true, msg, olusan.Select(k => k.No).ToList()));
        }

        public class NoIstek { public string No { get; set; } public string Neden { get; set; } public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult FaturaOlustur([FromBody] NoIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.FaturaOlustur(d.No, Ben()); if (h != null) return Json(new { success = false, message = h });
            lock (PersonelYansitmaOrnek.Kilit) { var k = PersonelYansitmaOrnek.Kayitlar.First(x => x.No == d.No); return Json(Paket(d.Donem ?? k.Donem, true, $"{k.No}: yansıtma faturası {k.FaturaNo} oluşturuldu ({k.KaynakSirket} → {k.HedefSirket}).")); }
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Iptal([FromBody] NoIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.Iptal(d.No, d.Neden, Ben()); if (h != null) return Json(new { success = false, message = h });
            lock (PersonelYansitmaOrnek.Kilit) { var k = PersonelYansitmaOrnek.Kayitlar.First(x => x.No == d.No); return Json(Paket(d.Donem ?? k.Donem, true, $"{k.No} iptal edildi; iki şirketteki fişler ters kayıtla kapatıldı.")); }
        }

        public class EslemeIstek { public string Alan { get; set; } public string GiderKodu { get; set; } public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult EslemeKaydet([FromBody] EslemeIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.EslemeKaydet(d.Alan, d.GiderKodu); if (h != null) return Json(new { success = false, message = h });
            var k = PersonelYansitmaOrnek.KatalogBul(d.GiderKodu);
            lock (PersonelYansitmaOrnek.Kilit) return Json(Paket(d.Donem ?? PersonelYansitmaOrnek.DonemBugun, true, $"Eşleme güncellendi: {d.Alan} → {k.GiderKodu} {k.Kalem} ↔ {k.YansitmaKodu} {k.YansitmaKalem}. Yeni kayıtlarda geçerlidir."));
        }

        public class DagilimIstek { public int Sira { get; set; } public string Donem { get; set; } public List<DagilimSatir> Dagilim { get; set; } }
        public class DagilimSatir { public string Sirket { get; set; } public decimal Pay { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult DagilimKaydet([FromBody] DagilimIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.DagilimKaydet(d.Sira, d.Dagilim?.Select(x => (x.Sirket, x.Pay)).ToList()); if (h != null) return Json(new { success = false, message = h });
            lock (PersonelYansitmaOrnek.Kilit) return Json(Paket(d.Donem ?? PersonelYansitmaOrnek.DonemBugun, true, d.Dagilim == null || d.Dagilim.Count == 0 ? "Dağılım varsayılana döndürüldü." : "Görev yeri dağılımı kaydedildi; hedef tutarlar yeniden hesaplandı."));
        }

        public class KdvIstek { public decimal Kdv { get; set; } public string Donem { get; set; } }
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult KdvKaydet([FromBody] KdvIstek d)
        {
            if (d == null) return Json(new { success = false, message = "Veri yok." });
            var h = PersonelYansitmaOrnek.KdvKaydet(d.Kdv); if (h != null) return Json(new { success = false, message = h });
            lock (PersonelYansitmaOrnek.Kilit) return Json(Paket(d.Donem ?? PersonelYansitmaOrnek.DonemBugun, true, $"Yansıtma faturası KDV oranı %{d.Kdv.ToString("0.##", Tr)} olarak kaydedildi (yeni kayıtlarda geçerli)."));
        }

        // ---- paket / DTO ----
        private object Paket(string donem, bool ok, string message, List<string> yeniler = null)
        {
            var kayitlar = PersonelYansitmaOrnek.Kayitlar.ToList();
            var adaylar = PersonelYansitmaOrnek.Adaylar();
            return new
            {
                success = ok, message, yeniler, donem, donemAdi = PersonelYansitmaOrnek.DonemAdi(donem), bugunDonem = PersonelYansitmaOrnek.DonemBugun, ben = Ben(),
                kdvOrani = PersonelYansitmaOrnek.KdvOrani, sirketler = PersonelYansitmaOrnek.Sirketler,
                donemler = kayitlar.Select(k => k.Donem).Concat(new[] { donem, PersonelYansitmaOrnek.DonemBugun }).Distinct().OrderByDescending(x => x).Select(x => new { kod = x, ad = PersonelYansitmaOrnek.DonemAdi(x) }),
                katalog = PersonelYansitmaOrnek.Katalog.Select(k => new { k.GiderKodu, k.AltGrupNo, k.AltGrup, k.KalemNo, k.Kalem, k.YansitmaKodu, k.YansitmaAltGrup, k.YansitmaKalem }),
                esleme = PersonelYansitmaOrnek.Esleme.Select(e => { var k = PersonelYansitmaOrnek.KatalogBul(e.GiderKodu); return new { e.Alan, e.AlanAdi, e.AktarimTipi, e.GiderKodu, giderKalem = k?.Kalem, giderAltGrup = k?.AltGrup, yansitmaKodu = k?.YansitmaKodu, yansitmaKalem = k?.YansitmaKalem }; }),
                adaylar = adaylar.Select(a => new
                {
                    sira = a.Personel.Sira, cariKodu = a.Personel.CariKodu, adSoyad = a.Personel.AdSoyad, unvan = a.Personel.Unvan, gorev = a.Personel.Gorev, bolum = a.Personel.Bolum, catiDepartman = a.Personel.CatiDepartman,
                    sgkIsvereni = a.Personel.SgkIsvereni, fiiliGorevYeri = a.Personel.FiiliGorevYeri, kaynakSirket = a.KaynakSirket,
                    dagilim = a.Dagilim.Select(d => new { sirket = d.Sirket, pay = d.Pay }), hedefler = a.Hedefler.Select(h => new { sirket = h.Sirket, pay = h.Pay, tutar = a.HedefNet(h.Pay), kayit = kayitlar.FirstOrDefault(k => k.Donem == donem && k.PersonelSira == a.Personel.Sira && k.HedefSirket == h.Sirket && k.Durum != "İptal")?.No }),
                    dagilimOzel = a.DagilimOzel, maliyet = a.Maliyet, yansitilacakPay = a.YansitilacakPay, yansitilacak = a.Yansitilacak,
                    kalemler = a.Kalemler.Select(KalemDto),
                    durum = a.Kalemler.Count == 0 ? "Kalem Yok" : a.Hedefler.All(h => kayitlar.Any(k => k.Donem == donem && k.PersonelSira == a.Personel.Sira && k.HedefSirket == h.Sirket && k.Durum != "İptal")) ? "Kaydedildi" : a.Hedefler.Any(h => kayitlar.Any(k => k.Donem == donem && k.PersonelSira == a.Personel.Sira && k.HedefSirket == h.Sirket && k.Durum != "İptal")) ? "Kısmen Kaydedildi" : "Hazır"
                }),
                kayitlar = kayitlar.OrderByDescending(k => k.Id).Select(KayitDto)
            };
        }
        private static object KalemDto(PyKalem k) => new { k.Alan, k.AlanAdi, k.AktarimTipi, k.GiderKodu, k.GiderKalem, k.YansitmaKodu, k.YansitmaKalem, k.Tutar };
        /// <summary>Muhasebe fiş satırı (simülasyon).</summary>
        public class FisSatir { public string Hesap { get; set; } public string Ad { get; set; } public decimal Borc { get; set; } public decimal Alacak { get; set; } public string Kod { get; set; } = ""; }
        /// <summary>SGK işvereni (kaynak) şirket fişi: hedef şirket carisi borç / yansıtma gelirleri (YNS) alacak (+ hesaplanan KDV).</summary>
        private static List<FisSatir> KaynakFis(PyKayit k)
        {
            var l = new List<FisSatir> { new FisSatir { Hesap = "120." + SirketKod(k.HedefSirket), Ad = k.HedefSirket + " Cari Hesabı", Borc = k.Toplam } };
            l.AddRange(k.Kalemler.Select(x => new FisSatir { Hesap = "649." + x.YansitmaKodu.Replace("YNS.", ""), Ad = x.YansitmaKalem, Alacak = x.Tutar, Kod = x.YansitmaKodu }));
            if (k.Kdv > 0) l.Add(new FisSatir { Hesap = "391.01", Ad = "Hesaplanan KDV", Alacak = k.Kdv });
            return l;
        }
        /// <summary>Fiili görev yeri (hedef) şirket fişi: personel giderleri (GDR) borç (+ indirilecek KDV) / kaynak şirket carisi alacak.</summary>
        private static List<FisSatir> HedefFis(PyKayit k)
        {
            var l = k.Kalemler.Select(x => new FisSatir { Hesap = "770." + x.GiderKodu.Replace("GDR.", ""), Ad = x.GiderKalem, Borc = x.Tutar, Kod = x.GiderKodu }).ToList();
            if (k.Kdv > 0) l.Add(new FisSatir { Hesap = "191.01", Ad = "İndirilecek KDV", Borc = k.Kdv });
            l.Add(new FisSatir { Hesap = "320." + SirketKod(k.KaynakSirket), Ad = k.KaynakSirket + " Cari Hesabı", Alacak = k.Toplam });
            return l;
        }
        private static object KayitDto(PyKayit k) => new
        {
            k.Id, k.No, k.Donem, donemAdi = PersonelYansitmaOrnek.DonemAdi(k.Donem), k.PersonelSira, k.CariKodu, k.AdSoyad, k.Gorev, k.KaynakSirket, k.HedefSirket, k.Pay, k.Net, k.KdvOrani, k.Kdv, k.Toplam, k.Durum,
            k.KaynakFisNo, k.HedefFisNo, k.FaturaNo, k.Olusturan, zaman = k.Zaman.ToString("dd.MM.yyyy HH:mm"), k.IptalNedeni, k.Gecmis, kalemler = k.Kalemler.Select(KalemDto),
            kaynakFis = KaynakFis(k), hedefFis = HedefFis(k)
        };
        private static string SirketKod(string s) => new string((s ?? "").ToUpper(Tr).Where(char.IsLetterOrDigit).Take(6).ToArray());
    }
}
