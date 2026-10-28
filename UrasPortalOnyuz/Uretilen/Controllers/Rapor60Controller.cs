// ÇEK EKSTRE RAPORU — Mali İşler › Finans › Finansal Araçlar › Çek Ekstre Raporu (/Rapor60?checkNum=…)
// Veri: Data/CekAkibetOrnek.cs (Çek Akıbeti kayıtları, günlük ve dolaşım hareketleri) — Kasa Nakit Akışı planı, Takip Paneli ibrazı, imzalı ibraz belgesi ve Çek Akıbeti ile AYNI KAYIT.
// Seçilen belge için: ana kart (tutar, kimden geldi, keşideci, banka, vade, güncel konum), "serüven" (zaman çizelgesi), hareket tablosu (portföye giriş → planlandı → ciro / ibraz → tahsil …),
// imzalı ibraz belgesi (Görüntüle / Dosya Yükle: Data/CekAkibetOrnek.cs › IbrazBelgesiBagla). Görünüm: Views/Rapor60/Index.cshtml (eski ERP ekstre düzeni korunur).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor60Controller : Controller
    {
        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        private string Kullanici => NakitAvansOrnek.KisiBul(User?.Identity?.Name)?.AdSoyad ?? (User?.Identity?.Name ?? "Finans");

        [HttpGet]
        public IActionResult Index(string checkNum = "", bool konsolide = false, string db = null)
        {
            string sirket = string.IsNullOrEmpty(db) ? null : CekAkibetOrnek.SirketAdi(db);
            var kayitlar = CekAkibetOrnek.Kayitlar.Where(k => string.IsNullOrEmpty(sirket) || konsolide || k.Sirket == sirket || k.DbName == db).OrderByDescending(k => k.Vade).ToList();
            ViewBag.CekListesi = kayitlar.Select(k => new CekListeItem { CheckKey = k.DocEntry.ToString(), CheckNum = k.No, BankCode = k.Banka, CheckSum = k.Tutar, Currency = k.PB }).ToList();
            ViewBag.SelectedCheckNum = checkNum ?? ""; ViewBag.CurrentDbDisplay = sirket ?? ""; ViewBag.Konsolide = konsolide;
            ViewBag.CekDegisiklikleri = new Dictionary<string, CekDegisiklikOzeti>();
            var model = new CekEkstreSonucModel { CekNo = checkNum };
            if (!string.IsNullOrEmpty(checkNum))
            {
                var k = CekAkibetOrnek.Bul(konsolide ? null : db, checkNum) ?? CekAkibetOrnek.Bul(null, checkNum);
                if (k != null)
                {
                    model.MainRecord = Satir(k, k.IslemTuru, k.IslemTuru, k.IbrazTarihi ?? k.PortfoyGiris, Kullanici, 0);
                    model.Hareketler = Hareketler(k); model.HikayeHtml = Hikaye(k, model.Hareketler);
                    if (k.IbrazBelgeImzali) ViewBag.FizikselBelgeYolu = k.IbrazBelgeYolu;
                    ViewBag.KritikUyari = k.IbrazBelgeImzali
                        ? $"<div class='alert alert-success py-2 shadow-sm border-0'><i class='fas fa-file-signature me-2'></i><b>İmzalı ibraz / ciro belgesi yüklü:</b> <a href='{k.IbrazBelgeYolu}' target='_blank'>{System.Net.WebUtility.HtmlEncode(k.IbrazBelgeDosya)}</a> · {k.IbrazBelgeYukleme:dd.MM.yyyy HH:mm} {System.Net.WebUtility.HtmlEncode(k.IbrazBelgeYukleyen)}" + (k.IbrazBelgeNo != "" ? $" · belge {System.Net.WebUtility.HtmlEncode(k.IbrazBelgeNo)}" : "") + "</div>"
                        : k.IbrazYeri != "" ? $"<div class='alert alert-warning py-2 shadow-sm border-0'><i class='fas fa-file-signature me-2'></i><b>İmzalı ibraz belgesi bekleniyor:</b> belge {System.Net.WebUtility.HtmlEncode(k.IbrazTuru)} → {System.Net.WebUtility.HtmlEncode(k.IbrazYeri)} ({k.IbrazTarihi:dd.MM.yyyy}, {System.Net.WebUtility.HtmlEncode(k.IbrazBelgeNo)}); imzalı nüshayı buradan <b>Dosya Yükle</b> ile ya da Konsolide Finansal Takip Paneli › Çek sekmesinden yükleyin. <a href='/Rapor123/IbrazBelgesi?cekDocEntry={k.DocEntry}' target='_blank'>Belge PDF'ini yeniden oluştur</a></div>"
                        : k.IslemTuru == "Planlandı" ? $"<div class='alert alert-info py-2 shadow-sm border-0'><i class='fas fa-clipboard-check me-2'></i><b>Kasa Nakit Akışı'nda planlı</b> → {System.Net.WebUtility.HtmlEncode(k.PlanlananTedarikci)}" + (k.PlanDocEntry != null ? $" · satır #{k.PlanDocEntry}" : "") + $". Çıkış ve imzalı belge: <a href='/Rapor123?sekme=cek&sirket={k.DbName}' target='_blank'>Konsolide Finansal Takip Paneli › Çek</a> · <a href='/Rapor123/IbrazBelgesi?cekDocEntry={k.DocEntry}' target='_blank'>Ciro bordrosu PDF'i oluştur</a></div>" : null;
                }
            }
            return View("Index", model);
        }

        /// <summary>İmzalı ibraz / ciro belgesi (ya da belge görüntüsü) yükleme: Çek Akıbeti kaydına bağlanır; plan satırı varsa Kasa Nakit Akışı ekine de yazılır.</summary>
        [HttpPost("DosyaYukle")]
        public IActionResult DosyaYukle(IFormFile belgeDosyasi, string checkNum, string db)
        {
            var k = CekAkibetOrnek.Bul(null, checkNum);
            if (k == null || belgeDosyasi == null || belgeDosyasi.Length == 0) { TempData["ErrorMessage"] = "Belge bulunamadı ya da dosya seçilmedi."; return RedirectToAction("Index", new { checkNum, db }); }
            byte[] veri; using (var ms = new MemoryStream()) { belgeDosyasi.CopyTo(ms); veri = ms.ToArray(); }
            var r = CekAkibetOrnek.IbrazBelgesiBagla(k.DocEntry, belgeDosyasi.FileName, belgeDosyasi.ContentType, veri, Kullanici, "Çek Ekstre Raporu'ndan yüklendi");
            if (r.ok && k.PlanDocEntry != null) { var plan = NakitAkisOrnek.Bul(k.PlanDocEntry.Value); if (plan != null) NakitAkisOrnek.EkEkle(plan.U_KaynakFirma, plan.DocEntry, belgeDosyasi.FileName, "Ciro Bordrosu", $"İmzalı ibraz belgesi · {k.No}", belgeDosyasi.ContentType, veri, Kullanici); }
            if (r.ok) FinekraOrnek.GunlukEkle(k.Sirket, k.No, "İmzalı İbraz Belgesi", belgeDosyasi.FileName + " (Çek Ekstre Raporu)", Kullanici);
            if (r.ok) TempData["SuccessMessage"] = r.mesaj; else TempData["ErrorMessage"] = r.mesaj;
            return RedirectToAction("Index", new { checkNum, db });
        }

        // ---------- Çek Akıbeti kaydından ekstre satırları ----------
        static CekEkstreViewModel Satir(CekKaydi k, string islem, string aciklama, DateTime tarih, string kullanici, int sira)
        {
            string tahsil = islem.Contains("Tahsil Edildi") ? k.TahsilatBelgeNo : "", ibraz = islem == "Ciro Edildi" || islem == "Planlandı" || islem == "Teminata Verildi" || islem.Contains("Ciro") ? k.IbrazBelgeNo : "", vadeli = islem.Contains("Bankaya") || islem.Contains("Bankadan") ? k.IbrazBelgeNo : "";
            string yer = islem == "Planlandı" ? k.PlanlananTedarikci : islem.Contains("Ciro") || islem.Contains("Bankaya") || islem.Contains("Teminat") || islem.Contains("Tahsil") ? k.IbrazYeri : islem == "Portföye Giriş" ? k.Musteri : "";
            return new CekEkstreViewModel
            {
                BelgeTipi = k.Belge, DocEntry = k.DocEntry, SequenceID = sira, IslemTarihiObj = tarih, IbrazTarihi = tarih.ToString("dd.MM.yyyy"), CekNumarasi = k.No, BankaAdi = k.Banka, Sube = k.Sube, CekKimdenGeldi = k.Musteri, CekKimdenGeldiKod = k.MusteriKod, AsilBorclu = k.AsilBorclu,
                CekIslemTuru = islem, CekKimeVerildi = yer, CekKimeVerildiKod = "", IslemTipi = k.IslemTipi, HareketDurumu = aciklama, KullanimDurumu = k.KonumDurumu, CekTutari = k.Tutar, ParaBirimi = k.PB, VadeTarihi = k.Vade.ToString("dd.MM.yyyy"), TLKarsiligi = k.Tutar,
                IslemiYapan = string.IsNullOrEmpty(kullanici) ? "Sistem" : kullanici, IptalMi = islem == "İptal Edildi", BagliTahsilatNo = tahsil, BagliIbrazNo = ibraz, BagliVadeliIbrazNo = vadeli, SirketAdi = k.Sirket
            };
        }
        static List<CekEkstreViewModel> Hareketler(CekKaydi k)
        {
            var l = new List<CekEkstreViewModel>(); int sira = 0;
            l.Add(Satir(k, "Portföye Giriş", $"{k.Musteri} tarafından ciro edilen {k.Belge.ToLower(Tr)} portföye alındı · keşideci {k.AsilBorclu} · {k.Banka} · vade {k.Vade:dd.MM.yyyy}", k.PortfoyGiris, "Sistem", ++sira));
            foreach (var g in CekAkibetOrnek.Gunluk.Where(g => g.No == k.No).OrderBy(g => g.Tarih)) l.Add(Satir(k, g.Islem, g.Aciklama, g.Tarih, g.Kullanici, ++sira));
            foreach (var h in CekAkibetOrnek.Hareketler.Where(h => h.CekDocEntry == k.DocEntry).OrderBy(h => h.Tarih)) l.Add(Satir(k, h.Tur + (h.PlanaUygun ? "" : " (plana aykırı)"), $"Dolaşım adımı {h.AdimSira}/{h.AdimToplam}: {h.Eden} → {h.Alan}" + (string.IsNullOrEmpty(h.BelgeNo) ? "" : $" · {h.BelgeNo}"), h.Tarih, h.Kullanici, ++sira));
            if (k.IbrazYeri != "" && !l.Any(x => x.CekIslemTuru == k.IslemTuru)) l.Add(Satir(k, k.IslemTuru, $"{k.IbrazTuru} → {k.IbrazYeri}" + (k.IbrazBelgeNo != "" ? $" · belge {k.IbrazBelgeNo}" : ""), k.IbrazTarihi ?? DateTime.Today, "Sistem", ++sira));
            return l.OrderBy(x => x.IslemTarihiObj).ThenBy(x => x.SequenceID).ToList();
        }
        static string Hikaye(CekKaydi k, List<CekEkstreViewModel> l)
        {
            string H(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");
            var sb = new System.Text.StringBuilder("<div class='story-timeline'>");
            foreach (var h in l)
            {
                string ikon = h.IptalMi ? "fa-ban text-danger" : h.CekIslemTuru.Contains("Tahsil") ? "fa-check-double text-success" : h.CekIslemTuru.Contains("Giriş") ? "fa-arrow-right text-primary" : h.CekIslemTuru.Contains("Ciro") ? "fa-hand-holding-usd text-warning" : h.CekIslemTuru.Contains("Planlandı") ? "fa-clipboard-check text-info" : h.CekIslemTuru.Contains("İmzalı") ? "fa-file-signature text-success" : "fa-circle text-secondary";
                sb.Append($"<div class='story-item'><span class='story-icon'><i class='fas {ikon}'></i></span><b>{H(h.IbrazTarihi)}</b> · <b>{H(h.CekIslemTuru)}</b>{(string.IsNullOrEmpty(h.CekKimeVerildi) ? "" : " → " + H(h.CekKimeVerildi))} <span class='text-muted'>{H(h.HareketDurumu)}</span> <small class='text-muted'>({H(h.IslemiYapan)})</small></div>");
            }
            string son = k.IbrazBelgeImzali ? $"<div class='story-item'><span class='story-icon'><i class='fas fa-file-signature text-success'></i></span><b>İmzalı ibraz belgesi:</b> <a href='{k.IbrazBelgeYolu}' target='_blank'>{H(k.IbrazBelgeDosya)}</a></div>" : k.IbrazYeri != "" ? "<div class='story-item'><span class='story-icon'><i class='fas fa-file-signature text-warning'></i></span><b>İmzalı ibraz belgesi bekleniyor</b> — Dosya Yükle ile ekleyin.</div>" : "";
            sb.Append(son).Append("</div>").Append($"<div class='small text-muted mt-2'>Kaynak: Çek Akıbeti kaydı #{k.DocEntry} · güncel akıbet <b>{H(k.IslemTuru)}</b> · konum {H(k.KonumDurumu)} · vade durumu {H(k.VadeDurumu)}" + (k.PlanDocEntry != null ? $" · Kasa Nakit Akışı #{k.PlanDocEntry}" : "") + "</div>");
            return sb.ToString();
        }
    }
}
