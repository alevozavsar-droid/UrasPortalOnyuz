using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Aylık Alım Onay Ekranı — Muhasebe'nin hazırladığı aylık alım (tedarikçi faturaları) raporunun satınalmacılar ve
    /// Tedarik Zinciri Direktörü tarafından ay sonunda onaylanması. Menü: Tedarik Zinciri › Satın Alma › Fiyat ve Alım Analizi
    /// ve Mali İşler › Muhasebe › Alış ve Satış Muhasebesi. Veri ve kurallar: Data/AlimOnayOrnek.cs (bellek içi).
    /// </summary>
    [Authorize]
    public class AlimOnayController : Controller
    {
        private static readonly CultureInfo Tr = AlimOnayOrnek.Tr;
        private string Ben() => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");

        public IActionResult Index()
        {
            ViewData["Title"] = "Aylık Alım Onay Ekranı";
            ViewBag.Kullanici = Ben();
            return View();
        }

        private static object Dto(AlimFatura f, bool detay = false)
        {
            var u = f.Uyarilar;
            return new
            {
                f.Id, f.FaturaNo, f.Ettn, f.FaturaTuru, Tarih = f.Tarih.ToString("dd.MM.yyyy"), TarihIso = f.Tarih.ToString("yyyy-MM-dd"), f.Donem,
                f.FaturaSirketi, f.KullanimSirketi, f.YansitmaDurumu, f.YansitmaNo, f.Tedarikci, f.TedarikciVkn, f.TalepEden, f.TalepEdenBirim, f.Satinalmaci, f.Kategori,
                f.SiparisNo, f.IrsaliyeNo, f.MasrafMerkezi, f.ParaBirimi, f.Kur, f.DovizNet, Vade = f.Vade.ToString("dd.MM.yyyy"), VadeGecti = f.Vade < DateTime.Today, f.OdemeDurumu, f.MuhasebeFisNo, f.Aciklama,
                f.Net, f.Kdv, f.Brut, KalemSayisi = f.Kalemler.Count,
                f.OnayDurumu, f.SiradakiAdim, UyariSayisi = u.Count, Uyarilar = u,
                SatinalmaciOnayi = Onay(f.SatinalmaciOnayi), DirektorOnayi = Onay(f.DirektorOnayi),
                Kalemler = detay ? f.Kalemler.Select(k => new { k.Sira, k.Kod, k.Aciklama, k.Miktar, k.Birim, k.BirimFiyat, k.Iskonto, k.KdvOrani, k.Tutar, k.KdvTutar, k.OncekiBirimFiyat, k.FiyatFarkiYuzde }) : null,
                DonemKapali = AlimOnayOrnek.KapatilanAylar.Contains(f.Donem)
            };
        }
        private static object Onay(AlimOnayKaydi o) => new { o.Durum, o.Kisi, Tarih = o.Tarih?.ToString("dd.MM.yyyy HH:mm"), o.Aciklama };

        /// <summary>Dönemin faturaları + özet. donem: yyyy-MM (boşsa en son dönem = geçen ay).</summary>
        [HttpGet]
        public IActionResult Liste(string donem)
        {
            lock (AlimOnayOrnek.Kilit)
            {
                var donemler = AlimOnayOrnek.Donemler.ToList();
                if (string.IsNullOrEmpty(donem) || !donemler.Contains(donem)) donem = donemler.Skip(1).FirstOrDefault() ?? donemler.First();   // varsayılan: ay sonu onayı yapılan geçen ay
                var l = AlimOnayOrnek.Faturalar.Where(f => f.Donem == donem).OrderBy(f => f.Tarih).ThenBy(f => f.Id).ToList();
                var gecerli = l.Where(f => f.OnayDurumu != "Reddedildi").ToList();
                var kategoriler = AlimOnayOrnek.Butceler.Keys.Select(k => new { ad = k, net = gecerli.Where(f => f.Kategori == k).Sum(f => f.Net), butce = AlimOnayOrnek.Butceler[k], adet = l.Count(f => f.Kategori == k) }).Where(x => x.adet > 0).OrderByDescending(x => x.net).ToList();
                var kapanis = AlimOnayOrnek.KapanisKayitlari.FirstOrDefault(k => k.Donem == donem);
                return Json(new
                {
                    success = true, donem, donemAdi = AlimOnayOrnek.DonemAdi(donem),
                    donemler = donemler.Select(d => new { kod = d, ad = AlimOnayOrnek.DonemAdi(d), kapali = AlimOnayOrnek.KapatilanAylar.Contains(d) }),
                    kapali = AlimOnayOrnek.KapatilanAylar.Contains(donem),
                    kapanis = kapanis.Donem == null ? null : new { kisi = kapanis.Kisi, tarih = kapanis.Tarih.ToString("dd.MM.yyyy HH:mm"), not = kapanis.Not },
                    satinalmacilar = AlimOnayOrnek.Satinalmacilar.Select(s => new { ad = s.Ad, kod = s.Kod, sirket = s.Sirket }),
                    direktor = new { ad = AlimOnayOrnek.Direktor.Ad, kod = AlimOnayOrnek.Direktor.Kod },
                    muhasebe = new { ad = AlimOnayOrnek.Muhasebe.Ad, kod = AlimOnayOrnek.Muhasebe.Kod },
                    ozet = new
                    {
                        adet = l.Count, net = gecerli.Sum(f => f.Net), kdv = gecerli.Sum(f => f.Kdv), brut = gecerli.Sum(f => f.Brut),
                        onayli = l.Count(f => f.OnayDurumu == "Onaylandı"), satinalmaciBekleyen = l.Count(f => f.OnayDurumu == "Satınalmacı Bekliyor"), direktorBekleyen = l.Count(f => f.OnayDurumu == "Direktör Bekliyor"), red = l.Count(f => f.OnayDurumu == "Reddedildi"),
                        uyarili = l.Count(f => f.Uyarilar.Count > 0), tedarikci = l.Select(f => f.Tedarikci).Distinct().Count(),
                        butce = kategoriler.Sum(k => k.butce), kategoriler
                    },
                    faturalar = l.Select(f => Dto(f))
                });
            }
        }

        [HttpGet]
        public IActionResult Detay(int id)
        {
            lock (AlimOnayOrnek.Kilit) { var f = AlimOnayOrnek.Bul(id); return f == null ? Json(new { success = false, message = "Fatura bulunamadı." }) : Json(new { success = true, fatura = Dto(f, true) }); }
        }

        /// <summary>eylem: Onayla | Reddet; rol: Satınalmacı | Direktör; kisi: onay veren (rol seçimine göre).</summary>
        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult Onay([FromBody] Dictionary<string, string> d)
        {
            int.TryParse(d.TryGetValue("id", out var i) ? i : "0", out int id);
            string hata = AlimOnayOrnek.Onayla(id, d.TryGetValue("rol", out var r) ? r : "", d.TryGetValue("kisi", out var k) && !string.IsNullOrWhiteSpace(k) ? k : Ben(), d.TryGetValue("eylem", out var e) ? e : "", d.TryGetValue("aciklama", out var a) ? a : "");
            if (hata != null) return Json(new { success = false, message = hata });
            var f = AlimOnayOrnek.Bul(id);
            return Json(new { success = true, message = $"{f.FaturaNo} — {(d["eylem"] == "Onayla" ? "onaylandı" : "reddedildi")} ({d["rol"]}). Durum: {f.OnayDurumu}", fatura = Dto(f, true) });
        }

        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult YenidenAc([FromBody] Dictionary<string, string> d)
        {
            int.TryParse(d.TryGetValue("id", out var i) ? i : "0", out int id);
            string hata = AlimOnayOrnek.YenidenAc(id, Ben());
            return hata != null ? Json(new { success = false, message = hata }) : Json(new { success = true, message = "Fatura yeniden onaya açıldı.", fatura = Dto(AlimOnayOrnek.Bul(id), true) });
        }

        [HttpPost, IgnoreAntiforgeryToken]
        public IActionResult AyKapat([FromBody] Dictionary<string, string> d)
        {
            string donem = d.TryGetValue("donem", out var dn) ? dn : "";
            string hata = AlimOnayOrnek.AyKapat(donem, d.TryGetValue("kisi", out var k) && !string.IsNullOrWhiteSpace(k) ? k : Ben(), d.TryGetValue("not", out var n) ? n : "");
            return hata != null ? Json(new { success = false, message = hata }) : Json(new { success = true, message = $"{AlimOnayOrnek.DonemAdi(donem)} alım onayı tamamlandı; dönem kapatıldı ve rapor yönetime sunuldu." });
        }

        /// <summary>Fatura görüntüsü (e-fatura / e-arşiv görünümü, örnek). Çerçeve içinde açılır.</summary>
        [HttpGet]
        public IActionResult Gorsel(int id)
        {
            var f = AlimOnayOrnek.Bul(id); if (f == null) return NotFound();
            string P(decimal v) => v.ToString("N2", Tr);
            var sb = new StringBuilder();
            sb.Append("<!doctype html><html lang=\"tr\"><head><meta charset=\"utf-8\"><title>").Append(f.FaturaNo).Append("</title><style>")
              .Append("body{margin:0;background:#e5e7eb;font-family:Arial,Helvetica,sans-serif;font-size:12px;color:#111}.kagit{width:min(794px,calc(100% - 20px));min-height:1000px;margin:10px auto;background:#fff;padding:28px 30px;box-shadow:0 4px 24px rgba(0,0,0,.18);position:relative;box-sizing:border-box}")
              .Append(".ust{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:2px solid #111;padding-bottom:10px}.logo{font-size:22px;font-weight:900;letter-spacing:.06em}.logo small{display:block;font-size:10px;font-weight:400;letter-spacing:.2em;color:#555}")
              .Append(".tip{text-align:right}.tip h1{margin:0;font-size:20px;letter-spacing:.1em}.tip div{font-size:11px;color:#333}.kare{width:74px;height:74px;border:1px solid #999;background:repeating-linear-gradient(45deg,#111 0 2px,#fff 2px 5px);opacity:.75}")
              .Append(".taraf{display:grid;grid-template-columns:1fr 1fr;gap:20px;margin:14px 0}.taraf div{border:1px solid #bbb;padding:8px 10px;min-height:96px}.taraf b{display:block;font-size:10px;color:#666;letter-spacing:.06em;margin-bottom:4px}.taraf .ad{font-size:13px;font-weight:700}")
              .Append("table{width:100%;border-collapse:collapse;margin-top:8px}th{background:#f3f4f6;border:1px solid #bbb;font-size:10px;padding:5px 6px;text-align:left}td{border:1px solid #bbb;padding:5px 6px;vertical-align:top}.s{text-align:right}")
              .Append(".top{width:340px;margin-left:auto;margin-top:12px}.top td{padding:4px 8px}.top tr:last-child td{font-weight:700;background:#f3f4f6}")
              .Append(".not{margin-top:16px;font-size:11px;color:#333;border-top:1px dashed #999;padding-top:8px}.filigran{position:absolute;left:0;right:0;top:44%;text-align:center;font-size:54px;font-weight:900;color:rgba(220,38,38,.08);transform:rotate(-18deg);pointer-events:none;letter-spacing:.2em}")
              .Append(".ettn{font-family:Consolas,monospace;font-size:10px}.imza{display:flex;justify-content:space-between;margin-top:30px;font-size:10px;color:#555}</style></head><body><div class=\"kagit\"><div class=\"filigran\">ÖRNEK GÖRÜNTÜ</div>");
            sb.Append("<div class=\"ust\"><div><div class=\"logo\">").Append(System.Net.WebUtility.HtmlEncode(f.Tedarikci.Split(' ')[0].ToUpper(Tr))).Append("<small>").Append(System.Net.WebUtility.HtmlEncode(f.Tedarikci)).Append("</small></div><div style=\"margin-top:8px;font-size:11px\">VKN: ").Append(f.TedarikciVkn).Append("<br>Vergi Dairesi: Büyük Mükellefler<br>Ticaret Sicil No: ").Append(180000 + f.Id * 7).Append("</div></div>")
              .Append("<div class=\"tip\"><h1>").Append(f.FaturaTuru == "e-Arşiv" ? "e-ARŞİV FATURA" : "e-FATURA").Append("</h1><div>Senaryo: ").Append(f.FaturaTuru == "e-Arşiv" ? "EARSIVFATURA" : "TEMELFATURA").Append(" · Tip: SATIS</div><div>Fatura No: <b>").Append(f.FaturaNo).Append("</b></div><div>Fatura Tarihi: ").Append(f.Tarih.ToString("dd.MM.yyyy")).Append(" · Saat: 10:").Append((f.Id * 7 % 60).ToString("00")).Append("</div><div class=\"ettn\">ETTN: ").Append(f.Ettn).Append("</div><div class=\"kare\" style=\"margin:8px 0 0 auto\"></div></div></div>");
            sb.Append("<div class=\"taraf\"><div><b>SATICI</b><span class=\"ad\">").Append(System.Net.WebUtility.HtmlEncode(f.Tedarikci)).Append("</span><br>Organize Sanayi Bölgesi 12. Cadde No: ").Append(f.Id % 40 + 1).Append("<br>Gebze / Kocaeli<br>VKN: ").Append(f.TedarikciVkn).Append("</div>")
              .Append("<div><b>ALICI</b><span class=\"ad\">").Append(System.Net.WebUtility.HtmlEncode(f.FaturaSirketi)).Append(f.FaturaSirketi.Contains("A.Ş.") ? "" : " San. ve Tic. A.Ş.").Append("</span><br>Hadımköy Mah. Sanayi Cad. No: 14<br>Arnavutköy / İstanbul<br>VKN: 8930041270 · Vergi Dairesi: Büyük Mükellefler<br>Sipariş No: ").Append(string.IsNullOrEmpty(f.SiparisNo) ? "—" : f.SiparisNo).Append(" · İrsaliye: ").Append(string.IsNullOrEmpty(f.IrsaliyeNo) ? "—" : f.IrsaliyeNo).Append("</div></div>");
            sb.Append("<table><thead><tr><th>Sıra</th><th>Mal / Hizmet Kodu</th><th>Mal / Hizmet Açıklaması</th><th class=\"s\">Miktar</th><th>Birim</th><th class=\"s\">Birim Fiyat</th><th class=\"s\">İskonto</th><th class=\"s\">KDV %</th><th class=\"s\">KDV Tutarı</th><th class=\"s\">Mal / Hizmet Tutarı</th></tr></thead><tbody>");
            foreach (var k in f.Kalemler) sb.Append("<tr><td>").Append(k.Sira).Append("</td><td>").Append(k.Kod).Append("</td><td>").Append(System.Net.WebUtility.HtmlEncode(k.Aciklama)).Append("</td><td class=\"s\">").Append(k.Miktar.ToString("N2", Tr)).Append("</td><td>").Append(k.Birim).Append("</td><td class=\"s\">").Append(P(k.BirimFiyat)).Append("</td><td class=\"s\">").Append(k.Iskonto > 0 ? "%" + k.Iskonto.ToString("0.#", Tr) : "—").Append("</td><td class=\"s\">").Append(k.KdvOrani.ToString("0", Tr)).Append("</td><td class=\"s\">").Append(P(k.KdvTutar)).Append("</td><td class=\"s\">").Append(P(k.Tutar)).Append("</td></tr>");
            decimal brutMal = f.Kalemler.Sum(k => k.Miktar * k.BirimFiyat), isk = brutMal - f.Net;
            sb.Append("</tbody></table><table class=\"top\"><tr><td>Mal / Hizmet Toplam Tutarı</td><td class=\"s\">").Append(P(brutMal)).Append(" ").Append(f.ParaBirimi == "TL" ? "TL" : "TL").Append("</td></tr><tr><td>Toplam İskonto</td><td class=\"s\">").Append(P(isk)).Append(" TL</td></tr><tr><td>KDV Matrahı</td><td class=\"s\">").Append(P(f.Net)).Append(" TL</td></tr><tr><td>Hesaplanan KDV (%20)</td><td class=\"s\">").Append(P(f.Kdv)).Append(" TL</td></tr><tr><td>Vergiler Dahil Toplam Tutar</td><td class=\"s\">").Append(P(f.Brut)).Append(" TL</td></tr><tr><td>Ödenecek Tutar</td><td class=\"s\">").Append(P(f.Brut)).Append(" TL</td></tr></table>");
            if (f.ParaBirimi != "TL") sb.Append("<div class=\"not\">Döviz: ").Append(P(f.DovizNet)).Append(" ").Append(f.ParaBirimi).Append(" (kur ").Append(f.Kur.ToString("N4", Tr)).Append(") — TL karşılığı fatura tarihindeki TCMB döviz alış kuruyla hesaplanmıştır.</div>");
            sb.Append("<div class=\"not\">Not: ").Append(System.Net.WebUtility.HtmlEncode(f.Aciklama)).Append(" Vade: ").Append(f.Vade.ToString("dd.MM.yyyy")).Append(". Yalnız ").Append(P(f.Brut)).Append(" TL'dir. Bu belge ").Append(f.FaturaTuru == "e-Arşiv" ? "e-Arşiv" : "e-Fatura").Append(" uygulaması kapsamında düzenlenmiştir; portalda gösterilen görüntü ÖRNEKTİR, gerçek tedarikçi ve tutarları temsil etmez.</div>")
              .Append("<div class=\"imza\"><span>Muhasebe fiş no: ").Append(f.MuhasebeFisNo).Append(" · Masraf merkezi: ").Append(System.Net.WebUtility.HtmlEncode(f.MasrafMerkezi)).Append("</span><span>Sayfa 1 / 1</span></div></div></body></html>");
            return Content(sb.ToString(), "text/html; charset=utf-8");
        }
    }
}
