using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FinansRaporlama.Controllers;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// ÇEK AKIBETİ RAPORU (Rapor2 › Index2, adres /CekAkibeti) — elle yazılmış; veri Data/CekAkibetOrnek.cs (Kasa Nakit Akışı ve Konsolide Finansal Takip Paneli ile ortak).
    /// Kasa Nakit Akışı'nda satıra seçilen belge burada "Planlandı" işlem türüyle ve planlanan tedarikçiyle görünür; ibraz / ciro bilgisi plana karşı kontrol edilir (açıklama sütunu + ibraz detayı).
    /// Üretilmiş sürüm Uretilen/Controllers/Rapor2Controller.cs içinde #if ile kapalı. Görünüm ana projeden (Views/Rapor2/Index2.cshtml; küçük eklemeler: Planlananlar kartı, kayıtla işlem türü güncelleme).
    /// </summary>
    [Authorize]
    [Route("CekAkibeti")]
    public class Rapor2Controller : Controller
    {
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        string Kullanici => OrnekVeri.KullaniciAdi(User?.Identity?.Name ?? "");
        string Sirket() { string db = Request.Query["db"].ToString(); if (string.IsNullOrEmpty(db)) db = Request.Cookies["SelectedDatabase"] ?? "DefaultConnection"; return CekAkibetOrnek.SirketAdi(db); }

        /// <summary>Şirketin portföyündeki belgeler + grup içi dolaşımla bu şirketten geçip ciro edilmiş belgeler (şirket açısından "Ciro Edildi"); vade yılına göre süzülür (yil = "all" → hepsi).</summary>
        static List<Rapor2ViewModel> Liste(string sirket, string yil, out List<string> yillar, out string secilen)
        {
            NakitAkisOrnek.Odemeler(CekAkibetOrnek.DbName(sirket));   // Kasa Nakit Akışı örnek verisi (plan satırları, dolaşım planları) listeden önce tohumlanır
            var hepsi = CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == sirket || k.Dolasim.Any(a => a.Durum == "Gerçekleşti" && a.Eden == sirket)).ToList();
            yillar = hepsi.Select(k => k.Vade.Year).Distinct().OrderByDescending(y => y).Select(y => y.ToString()).ToList();
            string s = !string.IsNullOrEmpty(yil) && yil != "all" ? yil : DateTime.Today.Year.ToString(); secilen = s;
            return hepsi.Where(k => yil == "all" || k.Vade.Year.ToString() == s).OrderBy(k => k.Vade).ThenBy(k => k.No).Select(k => k.Sirket == sirket ? Satir(k) : GecisSatiri(k, sirket)).ToList();
        }
        static readonly Newtonsoft.Json.JsonSerializerSettings JsonAyar = new Newtonsoft.Json.JsonSerializerSettings { DateFormatString = "yyyy-MM-dd" };

        [HttpGet(""), HttpGet("Index"), HttpGet("Index2")]
        public IActionResult Index2(string yil = null)
        {
            string sirket = Sirket(); var liste = Liste(sirket, yil, out var yillar, out var secilen);
            ViewBag.CheckTransactionTypes = new[] { new CheckTransactionType { Code = "0", Description = "Seçiniz" } }.Concat(CekAkibetOrnek.IslemTurleri.Select(t => new CheckTransactionType { Code = t.Kod, Description = t.Ad })).ToList();
            ViewBag.MevcutYillar = yillar; ViewBag.SecilenYil = secilen; ViewData["CurrentDbDisplay"] = sirket;
            OrnekDoldurucu.Hazirla(this);
            return View("Index2", liste);
        }
        /// <summary>Dinamik tazeleme: Kasa Nakit Akışı / Takip Paneli'nde belge planlanıp ibraz edilince (sürüm sayacı değişince) ekran listeyi sayfa yenilemeden buradan çeker.</summary>
        [HttpGet("Veri")]
        public IActionResult Veri(string yil = null) { string sirket = Sirket(); var liste = Liste(sirket, yil, out _, out var secilen); return Content(Newtonsoft.Json.JsonConvert.SerializeObject(new { success = true, sirket, yil = secilen, surum = Surum(), satirlar = liste }, JsonAyar), "application/json; charset=utf-8"); }
        static int Surum() => CekAkibetOrnek.Gunluk.Count + CekAkibetOrnek.Hareketler.Count + NakitAkisOrnek.LogSayisi;

        /// <summary>
        /// Çek Akıbeti kaydı → ekran satırı (ana projedeki alan adları). Durum parametreleri (Hareket, Konum, Vade Durumu, Uygunluk) Çek / Senet Durum Parametreleri Standart Raporu kuralıyla
        /// CekKaydi üzerinde otomatik türetilir; burada yalnız okunur (Data/CekAkibetOrnek.cs › KonumDurumu, HareketDurumu, VadeDurumu, UygunlukNedeni).
        /// </summary>
        static Rapor2ViewModel Satir(CekKaydi k)
        {
            string aciklama = k.IslemTuru == "Planlandı" ? $"Kasa Nakit Akışı #{k.PlanDocEntry} · {k.PlanlananTedarikci} için planlandı" + (k.PlanlamaTarihi != null ? $" ({k.PlanlamaTarihi:dd.MM.yyyy}, {k.Planlayan})" : "")
                : k.IbrazYeri != "" ? $"{k.IbrazTuru}: {k.IbrazYeri}" + (k.Kontrol == "Farklı" ? $" · DİKKAT: plan {k.PlanlananTedarikci} (farklı)" : k.Kontrol == "Uyumlu" ? " · plana uygun" : k.Kontrol == "Plansız İbraz" ? " · plansız" : "") : "";
            if (CekAkibetOrnek.IslemTuruAciklamalari.TryGetValue(k.IslemTuru, out var tanim)) aciklama = tanim + (aciklama == "" ? "" : " · " + aciklama);
            if (k.Dolasim.Count > 0) aciklama = $"Grup içi dolaşım {k.GerceklesenAdim}/{k.Dolasim.Count}: {k.DolasimOzeti}" + (k.Sirket != k.KaynakSirket ? $" · şu an {k.Sirket} portföyünde" : "") + " · " + aciklama;
            else if (k.Portfoyde && !string.IsNullOrEmpty(k.KaynakSirket) && k.Sirket != k.KaynakSirket) aciklama += $" · {k.Sirket} portföyünde";
            var m = new Rapor2ViewModel
            {
                DocEntry = k.DocEntry, CekNumarasi = k.No, BankaAdi = k.Banka, Sube = k.Sube, HareketDurumu = k.HareketDurumu, IslemTuru = k.IslemTuru, KonumDurumu = k.KonumDurumu, VadeDurumu = k.VadeDurumu, VadeYakinligi = k.VadeYakinligi, Aciklama = aciklama,
                UygunlukDurumu = k.UygunlukDurumu, UygunlukNedeni = k.UygunlukNedeni, CekKimdenGeldi = k.Musteri, AsilBorclu = k.AsilBorclu, Tutar = k.Tutar, ParaBirimi = k.PB, VadeTarihi = k.Vade,
                VadeAyiVeYili = ButceOrnek.Aylar[k.Vade.Month - 1] + " " + k.Vade.Year, VadeliCekIbrazi = k.IbrazTuru == "Bankaya İbraz" ? "Evet" : "", PortfoyVadeAyiYili = ButceOrnek.Aylar[k.PortfoyGiris.Month - 1] + " " + k.PortfoyGiris.Year,
                PortfoyeGirisTarihi = k.PortfoyGiris, IbrazTarihi = k.IbrazTarihi, TahsilatBelgeNo = k.TahsilatBelgeNo, IbrazBelgeNo = k.IbrazBelgeNo, PlanlananTedarikci = k.PlanlananTedarikci, BelgeTipi = k.Belge,
                CekIslemTuru = CekAkibetOrnek.IslemKodu(k.IslemTuru), IslemTipiAdi = k.IslemTuru, IbrazTuru = k.IbrazTuru, CekKimeVerildi = k.IbrazYeri, TlKarsiligi = k.Tutar, KullanımDurumu = k.KullanimDurumu,
                CekinBankasi = k.Banka, GelmeNedeni = "Tahsilat", SirketAdi = k.Sirket, SirlamaTarihi = k.Vade, CekBankaAdi = k.Banka, CekTipi = k.IslemTipi
            };
            PanelAlanlari(m, k); return m;
        }

        /// <summary>
        /// Konsolide Finansal Takip Paneli › Çek · Senet sekmesiyle ortak sütunlar: Dolaşım (plan zinciri, gerçekleşen adım, sıradaki adım), Satır Kaynağı · İşlem
        /// (belgenin geldiği kayıt ve yapılacak iş), Nakit Akış Eşleşmesi (plan satırı), Kontrol (plan ↔ ibraz) ve Fiş (panelle aynı muhasebe fişi numarası).
        /// </summary>
        static void PanelAlanlari(Rapor2ViewModel m, CekKaydi k)
        {
            var plan = k.PlanDocEntry != null ? NakitAkisOrnek.Bul(k.PlanDocEntry.Value) : null;
            m.PlanDocEntry = k.PlanDocEntry; m.PlanDurum = plan?.U_Durum ?? ""; m.PlanTalimat = plan?.U_TalimatNo ?? ""; m.CikisTalebi = plan?.U_CikisTalebi ?? ""; m.PlanOnay = plan?.U_OnayDurumu ?? ""; m.PlanLink = plan == null ? null : OdemeLink(plan);
            m.Kontrol = k.Kontrol ?? "";
            m.Dolasim = k.DolasimOzeti; m.DolasimGerceklesen = k.GerceklesenAdim; m.DolasimToplam = k.Dolasim.Count;
            var sonraki = k.Dolasim.FirstOrDefault(a => a.Durum != "Gerçekleşti"); m.DolasimSiradaki = sonraki == null ? "" : $"{sonraki.Tur} → {sonraki.Alan} ({sonraki.PlanTarih:dd.MM.yyyy})";
            m.DolasimAdimlar = k.Dolasim.Select(a => new Rapor2DolasimAdim { Sira = a.Sira, Tur = a.Tur, Eden = a.Eden, Alan = a.Alan, Durum = a.Durum, Tarih = (a.GercekTarih ?? a.PlanTarih).ToString("dd.MM.yyyy"), BelgeNo = a.BelgeNo ?? "" }).ToList();
            bool gmy = plan != null && !string.IsNullOrEmpty(plan.U_CikisTalebi) && plan.U_OnayDurumu != "Onaylandı" && plan.U_Durum != "ÖDENDİ";
            if (k.IbrazYeri != "")
            {
                m.Kaynak = k.IbrazTuru == "Bankaya İbraz" ? "Çek Akıbeti · Bankaya İbraz" : k.IbrazTuru == "Teminat" ? "Çek Akıbeti · Teminat" : k.IbrazTuru == "İade" ? "Çek Akıbeti · İade" : "Çek Akıbeti · Ciro";
                m.KaynakDetay = $"{k.IbrazBelgeNo} · {k.IbrazTarihi:dd.MM.yyyy} · {k.IslemTuru}" + (k.PlanDocEntry != null ? $" · plan Kasa Nakit Akışı #{k.PlanDocEntry}" : k.Kontrol == "Plansız İbraz" ? " · plansız" : ""); m.SatirIslem = "yok";
            }
            else if (k.IslemTuru == "Planlandı")
            {
                if (k.PlanDocEntry != null) { m.Kaynak = "Kasa Nakit Akışı · Plan"; m.KaynakDetay = $"#{k.PlanDocEntry} · {k.PlanlananTedarikci} · {k.PlanlamaTarihi:dd.MM.yyyy}" + (k.Planlayan != "" ? " · " + k.Planlayan : ""); }
                else { m.Kaynak = "Çek Akıbeti · Plan"; m.KaynakDetay = $"{k.PlanlananTedarikci} · {k.PlanlamaTarihi:dd.MM.yyyy} · nakit akış satırı yok"; }
                m.SatirIslem = gmy ? "gmy" : "planli";
            }
            // portföydeki belge: Uygun → Nakit Akışa Ekle / İbraz Et; vadesi bugün / geçmiş ya da ertelenmiş kullanılabilir belge → yalnız İbraz Et (başka tedarikçiye verilemez; standart rapor); riskli → işlem gerekmez
            else if (k.IslemTuru == "Portföyde" || k.IslemTuru == "Erteleme Yapıldı") { m.Kaynak = k.IslemTuru == "Portföyde" ? "Çek Akıbeti · Portföy Girişi" : "Çek Akıbeti · Erteleme"; m.KaynakDetay = $"{k.Musteri} · giriş {k.PortfoyGiris:dd.MM.yyyy} · {k.Banka}" + (k.IslemTuru == "Erteleme Yapıldı" ? " · vade ötelendi" : ""); m.SatirIslem = gmy ? "gmy" : k.Secilebilir ? "portfoy" : "ibraz"; }
            else { m.Kaynak = "Çek Akıbeti · " + k.IslemTuru; m.KaynakDetay = $"{k.Musteri} · giriş {k.PortfoyGiris:dd.MM.yyyy} · {k.KonumDurumu} · {k.KullanimDurumu}"; m.SatirIslem = "yok"; }
            Fis(m, Rapor123Controller.CekFisNo(k, k.Sirket));
        }
        static void Fis(Rapor2ViewModel m, string fisNo) { var f = fisNo == null ? null : MuhasebeOrnek.Bul(fisNo); if (f == null) return; m.FisNo = f.No; m.FisKaynak = f.Kaynak; m.FisTarih = f.Tarih.ToString("dd.MM.yyyy"); }

        /// <summary>Dolaşım zincirinde bu şirketten geçmiş belge: şirket açısından grup içi ciro ile çıkmıştır (alan şirket, ciro belge no, tarih); belge şu an başka şirketin portföyündedir.</summary>
        static Rapor2ViewModel GecisSatiri(CekKaydi k, string sirket)
        {
            var s = Satir(k); var adim = k.Dolasim.Last(a => a.Durum == "Gerçekleşti" && a.Eden == sirket);
            // standart parametreler şirket açısından: ciro edildi → konum Tedarikçide (grup şirketi), hareket İşlem Yapıldı, vade durumu tarihten, uygunluk Uygun Değil
            s.HareketDurumu = "İşlem Yapıldı"; s.IslemTuru = "Ciro Edildi"; s.IslemTipiAdi = "Ciro Edildi"; s.CekIslemTuru = CekAkibetOrnek.IslemKodu("Ciro Edildi"); s.KonumDurumu = "Tedarikçide"; s.IbrazTuru = "Ciro"; s.CekKimeVerildi = adim.Alan; s.IbrazTarihi = adim.GercekTarih; s.IbrazBelgeNo = adim.BelgeNo ?? ""; s.SirketAdi = sirket;
            int g = (k.Vade.Date - DateTime.Today).Days; s.VadeDurumu = g > 0 ? "Vade Bekleniyor" : g == 0 ? "Vadesi Bugün" : "Vadesi Geçti";
            s.Aciklama = $"Grup içi dolaşım {k.GerceklesenAdim}/{k.Dolasim.Count}: {k.DolasimOzeti} · {sirket} → {adim.Alan} ciro edildi ({adim.BelgeNo}, {adim.GercekTarih:dd.MM.yyyy}, adım {adim.Sira}) · şu an {k.Sirket} portföyünde · plana uygun";
            // panel sütunları: dolaşım geçiş satırı — kaynak gerçekleşen dolaşım adımı, kontrol uyumlu, fiş adımın ciro fişi (eden şirkette)
            s.Kontrol = "Uyumlu"; s.Kaynak = "Dolaşım Planı · Gerçekleşen Adım"; s.KaynakDetay = $"{adim.Tur} · {adim.BelgeNo} · adım {adim.Sira}/{k.Dolasim.Count} · {adim.GercekTarih:dd.MM.yyyy}"; s.SatirIslem = "yok";
            s.UygunlukDurumu = "Uygun Değil"; s.UygunlukNedeni = "Grup içi ciro edildi · " + adim.Alan; s.FisNo = null; s.FisKaynak = null; s.FisTarih = null; Fis(s, Rapor123Controller.CekFisNo(k, sirket, adim));
            return s;
        }

        static string Html(string s) => System.Net.WebUtility.HtmlEncode(s ?? "");
        static string DetayHtml(CekKaydi k, string baslik)
        {
            string kontrolRozet = k.Kontrol == "Uyumlu" ? "<span class='badge bg-success'>Uyumlu</span>" : k.Kontrol == "Farklı" ? "<span class='badge bg-danger'>FARKLI — plana aykırı çıkış</span>" : k.Kontrol == "İbraz Bekliyor" ? "<span class='badge bg-warning text-dark'>İbraz Bekliyor</span>" : k.Kontrol == "Plansız İbraz" ? "<span class='badge bg-info text-dark'>Plansız İbraz</span>" : "<span class='text-muted'>—</span>";
            return $@"<div class='p-2'><h6 class='fw-bold mb-2'>{Html(baslik)} · {Html(k.No)}</h6><table class='table table-sm mb-2'><tbody>
<tr><th class='text-muted' style='width:200px'>Belge</th><td>{Html(k.Belge)} · {Html(k.Banka)} / {Html(k.Sube)}</td></tr>
<tr><th class='text-muted'>Keşideci / Kimden Geldi</th><td>{Html(k.AsilBorclu)} / {Html(k.Musteri)}</td></tr>
<tr><th class='text-muted'>Vade · Tutar</th><td>{k.Vade:dd.MM.yyyy} · {k.Tutar.ToString("N2", Tr)} {Html(k.PB)}</td></tr>
<tr><th class='text-muted'>İşlem Türü</th><td>{Html(k.IslemTuru)}</td></tr>
<tr><th class='text-muted'>Kasa Nakit Akışı Planı</th><td>{(k.PlanlananTedarikci == "" ? "<span class='text-muted'>plan yok</span>" : Html(k.PlanlananTedarikci) + (k.PlanDocEntry != null ? $" · kayıt #{k.PlanDocEntry}" : "") + (k.PlanlamaTarihi != null ? $" · {k.PlanlamaTarihi:dd.MM.yyyy} {Html(k.Planlayan)}" : ""))}</td></tr>
<tr><th class='text-muted'>İbraz / Ciro</th><td>{(k.IbrazYeri == "" ? "<span class='text-muted'>henüz çıkış yok</span>" : Html(k.IbrazTuru) + ": " + Html(k.IbrazYeri) + (k.IbrazTarihi != null ? $" · {k.IbrazTarihi:dd.MM.yyyy}" : "") + (k.IbrazBelgeNo != "" ? " · belge " + Html(k.IbrazBelgeNo) : "") + (k.TahsilatBelgeNo != "" ? " · tahsilat " + Html(k.TahsilatBelgeNo) : ""))}</td></tr>
<tr><th class='text-muted'>Plan ↔ İbraz Kontrolü</th><td>{kontrolRozet}</td></tr></tbody></table>
<div class='small text-muted'>Kaynak: Kasa Nakit Akışı (Rapor79) › Çek / Senet sekmesi · Konsolide Finansal Takip Paneli › Çek: plana aykırı çıkışlar.</div></div>";
        }
        /// <summary>Çek kaydı değişim sayacı: Kasa Nakit Akışı'nda plan / ibraz yapılıp bu sekmeye dönülünce sayfa kendini tazeler (Index2.cshtml visibilitychange).</summary>
        [HttpGet("Versiyon")] public IActionResult Versiyon() => Json(new { v = Surum() });

        // ---------- DURUM ÖZETİ · SENKRON (Kasa Nakit Akışı ↔ Çek Akıbeti ↔ Takip Paneli) ----------
        static string OdemeLink(NakitAkisViewModel m) => $"/Rapor79?sirket={m.U_KaynakFirma}&sekme={(m.U_OdemeYontemi == "Senet" ? "senet" : m.U_OdemeYontemi == "Çek" ? "cek" : m.U_OdemeYontemi == "Nakit" ? "nakit" : m.U_OdemeYontemi == "Havale / EFT" ? "havale" : "tumu")}&doc={m.DocEntry}";
        /// <summary>
        /// Durum Özeti paneli: şirketin portföyü (X / R, kullanılabilir / planlı), vadesi gelen planlı belgeler (plan satırı, talimat, çıkış düğmesi), GMY onayı bekleyen plansız çıkış talepleri,
        /// plana aykırı çıkışlar, bankada tahsildekiler, son günlük; her belge için plan satırı bilgisi (planlar: belge → nakit akış satırı) — detay penceresindeki hızlı işlemler bunu kullanır.
        /// </summary>
        [HttpGet("Ozet")]
        public IActionResult Ozet()
        {
            string sirket = Sirket(); var bugun = DateTime.Today; string db = CekAkibetOrnek.DbName(sirket);
            var hepsi = CekAkibetOrnek.Kayitlar.Where(k => k.Sirket == sirket).ToList(); var portfoy = hepsi.Where(k => k.Portfoyde && k.PortfoyGiris.Date <= bugun).ToList();
            object P(string belge, string tip) { var l = portfoy.Where(k => k.Belge == belge && k.IslemTipi == tip).ToList(); return new { adet = l.Count, tutar = l.Sum(k => k.Tutar), kullanilabilir = l.Count(k => k.KullanimDurumu == "Kullanılabilir" && k.IslemTuru == "Portföyde"), kullanilabilirTutar = l.Where(k => k.KullanimDurumu == "Kullanılabilir" && k.IslemTuru == "Portföyde").Sum(k => k.Tutar), planli = l.Count(k => k.IslemTuru == "Planlandı"), planliTutar = l.Where(k => k.IslemTuru == "Planlandı").Sum(k => k.Tutar) }; }
            var odemeler = NakitAkisOrnek.Odemeler(db);
            object PlanJson(CekKaydi k) { var m = k.PlanDocEntry != null ? NakitAkisOrnek.Bul(k.PlanDocEntry.Value) : null; return new { docEntry = k.DocEntry, no = k.No, belge = k.Belge, tip = k.IslemTipi, musteri = k.Musteri, banka = k.Banka, vade = k.Vade.ToString("dd.MM.yyyy"), vadeGun = (k.Vade - bugun).Days, tutar = k.Tutar, islemTuru = k.IslemTuru, planlananTedarikci = k.PlanlananTedarikci, planlayan = k.Planlayan, planlamaTarihi = k.PlanlamaTarihi?.ToString("dd.MM.yyyy"), ibrazYeri = k.IbrazYeri, ibrazTuru = k.IbrazTuru, ibrazTarihi = k.IbrazTarihi?.ToString("dd.MM.yyyy"), kontrol = k.Kontrol, kullanim = k.KullanimDurumu, dolasim = k.DolasimOzeti, planDocEntry = k.PlanDocEntry, planDb = m?.U_KaynakFirma, planDurum = m?.U_Durum ?? "", planTalimat = m?.U_TalimatNo ?? "", planKurum = m?.U_Kurum ?? "", cikisTalebi = m?.U_CikisTalebi ?? "", planOnay = m?.U_OnayDurumu ?? "", planLink = m == null ? null : OdemeLink(m) }; }
            var planliKayit = hepsi.Where(k => k.IslemTuru == "Planlandı").OrderBy(k => k.Vade).ToList(); var planli = planliKayit.Select(PlanJson).ToList();
            var gmy = odemeler.Where(m => !string.IsNullOrEmpty(m.U_CikisTalebi) && m.U_OnayDurumu != "Onaylandı" && m.U_Durum != "ÖDENDİ").Select(m => new { docEntry = m.DocEntry, kurum = m.U_Kurum, cekNolar = m.U_CekNolar, cikisTalebi = m.U_CikisTalebi, ibrazYeri = m.U_CikisIbrazYeri, talepEden = m.U_CikisTalepEden, tutar = m.U_TutarTL ?? 0, tarih = m.U_PlanlananTarih?.ToString("dd.MM.yyyy"), link = OdemeLink(m) }).ToList();
            var farkli = hepsi.Where(k => k.Kontrol == "Farklı").OrderByDescending(k => k.IbrazTarihi).Take(20).Select(PlanJson).ToList();
            var bankada = hepsi.Where(k => k.KonumDurumu == "Bankada").OrderBy(k => k.Vade).ToList();   // standart konum: tahsilde, teminatta, bankada ertelenmiş / riskli belgeler
            var planlar = hepsi.Where(k => k.PlanDocEntry != null || k.Portfoyde).ToDictionary(k => k.DocEntry.ToString(), k => { var m = k.PlanDocEntry != null ? NakitAkisOrnek.Bul(k.PlanDocEntry.Value) : null; return new { planDocEntry = k.PlanDocEntry, planDb = m?.U_KaynakFirma, planDurum = m?.U_Durum ?? "", planTalimat = m?.U_TalimatNo ?? "", planKurum = m?.U_Kurum ?? "", cikisTalebi = m?.U_CikisTalebi ?? "", planOnay = m?.U_OnayDurumu ?? "", planLink = m == null ? null : OdemeLink(m), secilebilir = k.Secilebilir, portfoyde = k.Portfoyde, islemTuru = k.IslemTuru, dolasim = k.DolasimOzeti }; });
            var gunluk = CekAkibetOrnek.Gunluk.Where(g => g.Sirket == sirket).Take(12).Select(g => new { tarih = g.Tarih.ToString("dd.MM.yyyy HH:mm"), g.Kullanici, g.No, g.Islem, g.Aciklama }).ToList();
            return Json(new
            {
                success = true, sirket, db, bugun = bugun.ToString("dd.MM.yyyy"), surum = CekAkibetOrnek.Gunluk.Count + CekAkibetOrnek.Hareketler.Count,
                portfoy = new { cekX = P("Çek", "X"), cekR = P("Çek", "R"), senetX = P("Senet", "X"), senetR = P("Senet", "R"), toplam = portfoy.Count, toplamTutar = portfoy.Sum(k => k.Tutar) },
                planli, planliToplam = planliKayit.Sum(k => k.Tutar), vadesiGelen = planliKayit.Count(k => k.Vade <= bugun.AddDays(7)), gmy, farkli, bankada = new { adet = bankada.Count, tutar = bankada.Sum(k => k.Tutar), buHafta = bankada.Count(k => k.Vade <= bugun.AddDays(7)) },
                nakitAkis = new { planliSatir = odemeler.Count(m => (m.U_OdemeYontemi == "Çek" || m.U_OdemeYontemi == "Senet") && m.U_Durum != "ÖDENDİ"), belgesiz = odemeler.Count(m => (m.U_OdemeYontemi == "Çek" || m.U_OdemeYontemi == "Senet") && m.U_Durum != "ÖDENDİ" && string.IsNullOrEmpty(m.U_CekDocEntryler)), acikTalimat = TalimatOrnek.Listele(db).Count(t => !t.Kapandi && (t.Yontem == "Çek" || t.Yontem == "Senet")), link = $"/Rapor79?sirket={db}&sekme=cek" },
                takipPaneli = $"/Rapor123?sirket={db}&sekme=cek", planlar, gunluk, tedarikciler = CekOrnek.TedarikciAdlari
            });
        }
        public class CekIslemIstek { public int DocEntry { get; set; } public string Tedarikci { get; set; } public string PlanlananTarih { get; set; } public string BelgeTipi { get; set; } public string BelgeNo { get; set; } public string UrunHizmet { get; set; } public string Tur { get; set; } public string IbrazYeri { get; set; } public string Not { get; set; } }
        /// <summary>Detay penceresi › "Kasa Nakit Akışı'na Gönder": portföydeki belge için nakit akışta çek / senet satırı açılır, belge planlanır (Planlandı).</summary>
        [HttpPost("NakitAkisaGonder")]
        public IActionResult NakitAkisaGonder([FromBody] CekIslemIstek i)
        {
            if (i == null) return Json(new { success = false, message = "Veri yok." });
            var tarih = DateTime.TryParse(i.PlanlananTarih, CultureInfo.InvariantCulture, DateTimeStyles.None, out var pt) ? pt.Date : (DateTime?)null;
            var r = NakitAkisOrnek.CekIcinSatirAc(i.DocEntry, i.Tedarikci, tarih, i.BelgeTipi, i.BelgeNo, i.UrunHizmet, Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, satir = r.satir == null ? null : new { docEntry = r.satir.DocEntry, dbName = r.satir.U_KaynakFirma, link = OdemeLink(r.satir) } });
        }
        /// <summary>Detay penceresi › "Tedarikçiye Çık / Bankaya İbraz": planlı belge plan satırı üzerinden ciro / ibraz edilir (satır ÖDENDİ, fiş); plansız belge GMY onayına düşer.</summary>
        [HttpPost("TedarikciyeCik")]
        public IActionResult TedarikciyeCik([FromBody] CekIslemIstek i)
        {
            if (i == null) return Json(new { success = false, message = "Veri yok." });
            var r = NakitAkisOrnek.CekCikisYap(i.DocEntry, i.Tur, i.IbrazYeri, i.BelgeNo, i.Not, Kullanici); var plan = r.docEntry != null ? NakitAkisOrnek.Bul(r.docEntry.Value) : null;
            var k = CekAkibetOrnek.Bul(i.DocEntry); if (k != null && (r.ok || r.onayaDustu)) FinekraOrnek.GunlukEkle(k.Sirket, k.No, r.onayaDustu ? "Çek Çıkış Talebi (GMY)" : i.Tur == "Bankaya Tahsile Ver" ? "Bankaya İbraz" : "Tedarikçiye Ciro", r.mesaj + " (Çek Akıbeti)", Kullanici);
            return Json(new { success = r.ok, message = r.mesaj, onayaDustu = r.onayaDustu, satir = plan == null ? null : new { docEntry = plan.DocEntry, dbName = plan.U_KaynakFirma, link = OdemeLink(plan) } });
        }
        [HttpGet("GetIbrazDetay")]
        public IActionResult GetIbrazDetay(string no) { var k = CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.IbrazBelgeNo == no) ?? CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.No == no); return Content(k == null ? "<div class='p-3 text-muted'>İbraz belgesi bulunamadı.</div>" : DetayHtml(k, "İbraz Detayı"), "text/html; charset=utf-8"); }
        [HttpGet("GetTahsilatDetay")]
        public IActionResult GetTahsilatDetay(string no) { var k = CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.TahsilatBelgeNo == no) ?? CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.No == no); return Content(k == null ? "<div class='p-3 text-muted'>Tahsilat belgesi bulunamadı.</div>" : DetayHtml(k, "Tahsilat Detayı"), "text/html; charset=utf-8"); }

        // ---------- güncellemeler (bellek içi) ----------
        public class KullanimDurumuIstek { public int DocEntry { get; set; } public string NewKullanimDurumu { get; set; } }
        [HttpPost("UpdateKullanimDurumu")]
        public IActionResult UpdateKullanimDurumu([FromBody] KullanimDurumuIstek model) => Json(new { success = false, message = "Kullanım Durumu standart raporda yer almaz; durum parametreleri İşlem Türü / Konum Durumu / Hareket Durumu / Vade Durumu / Uygunluk Durumu'dur." });
        public class IbrazBelgeIstek { public string IbrazBelgeNo { get; set; } public string NewCheckTransactionType { get; set; } }
        [HttpPost("UpdateCheckTransactionTypeByIbrazBelge")]
        public IActionResult UpdateCheckTransactionTypeByIbrazBelge([FromBody] IbrazBelgeIstek model)
        {
            var k = CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.IbrazBelgeNo == model?.IbrazBelgeNo); if (k == null) return Json(new { success = false, message = "İbraz belgesi bulunamadı." });
            var r = NakitAkisOrnek.CekCikisKontrollu(k.DocEntry, model.NewCheckTransactionType, null, null, Kullanici); return Json(new { success = r.ok, message = r.mesaj, onayaDustu = r.onayaDustu, nakitAkisDocEntry = r.docEntry });
        }
        public class TahsilatIstek { public string TahsilatBelgeNo { get; set; } public string NewCheckTransactionType { get; set; } }
        [HttpPost("UpdateCheckTransactionTypeByTahsilat")]
        public IActionResult UpdateCheckTransactionTypeByTahsilat([FromBody] TahsilatIstek model)
        {
            var k = CekAkibetOrnek.Kayitlar.FirstOrDefault(x => x.TahsilatBelgeNo == model?.TahsilatBelgeNo); if (k == null) return Json(new { success = false, message = "Tahsilat belgesi bulunamadı." });
            var r = NakitAkisOrnek.CekCikisKontrollu(k.DocEntry, model.NewCheckTransactionType, null, null, Kullanici); return Json(new { success = r.ok, message = r.mesaj, onayaDustu = r.onayaDustu, nakitAkisDocEntry = r.docEntry });
        }
        public class DocEntryIslemIstek { public int DocEntry { get; set; } public string NewCheckTransactionType { get; set; } public string Konum { get; set; } public string IbrazYeri { get; set; } public string IbrazBelgeNo { get; set; } }
        /// <summary>
        /// İşlem türü (+ konum, ibraz yeri, belge no) değişikliği: standart akış kayıt üzerinde işler (konum / hareket / vade durumu / uygunluk otomatik), güncel satır JSON olarak döner (ekran satırı yeniden çizilir).
        /// Kasa Nakit Akışı'nda planlanmamış belge için çıkış türleri uygulanmaz; GMY onayına düşer (NakitAkisOrnek.CekCikisKontrollu).
        /// </summary>
        [HttpPost("UpdateCheckTransactionTypeByDocEntry")]
        public IActionResult UpdateCheckTransactionTypeByDocEntry([FromBody] DocEntryIslemIstek model)
        {
            var r = NakitAkisOrnek.CekCikisKontrollu(model?.DocEntry ?? 0, model?.NewCheckTransactionType, model?.IbrazYeri, model?.IbrazBelgeNo, Kullanici, model?.Konum);
            var k = CekAkibetOrnek.Bul(model?.DocEntry ?? 0); string satirJson = k == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(Satir(k), JsonAyar);
            return Json(new { success = r.ok, message = r.mesaj, onayaDustu = r.onayaDustu, nakitAkisDocEntry = r.docEntry, satirJson });
        }
        /// <summary>İşlem türlerinin standart tanımı ve konum olasılıkları (ekran: işlem türü değişiklik penceresi).</summary>
        [HttpGet("IslemTurleri")]
        public IActionResult IslemTurleri() => Json(CekAkibetOrnek.IslemTurleri.Select(t => new { kod = t.Kod, ad = t.Ad, aciklama = CekAkibetOrnek.IslemTuruAciklamalari.TryGetValue(t.Ad, out var a) ? a : "", konumlar = CekAkibetOrnek.KonumSecenekleri(t.Ad) }));
        public class PlanlananTedarikciIstek { public int DocEntry { get; set; } public string NewPlannedSupplier { get; set; } }
        [HttpPost("UpdatePlannedSupplier")]
        /// <summary>Planlanan Tedarikçi — Kasa Nakit Akışı ile bütünleşik (NakitAkisOrnek.PlanTedarikciDegistir): plan satırının kurumu değişir / satır açılır / belge satırdan çıkar; güncel satır JSON döner.</summary>
        public IActionResult UpdatePlannedSupplier([FromBody] PlanlananTedarikciIstek model)
        {
            var r = NakitAkisOrnek.PlanTedarikciDegistir(model?.DocEntry ?? 0, model?.NewPlannedSupplier, Kullanici);
            var k = CekAkibetOrnek.Bul(model?.DocEntry ?? 0); string satirJson = k == null ? null : Newtonsoft.Json.JsonConvert.SerializeObject(Satir(k), JsonAyar);
            return Json(new { success = r.ok, message = r.mesaj, satirJson, satir = r.satir == null ? null : new { docEntry = r.satir.DocEntry, durum = r.satir.U_Durum, link = OdemeLink(r.satir) } });
        }
        [HttpGet("SearchSuppliers")]
        public IActionResult SearchSuppliers(string term) { string q = (term ?? "").ToUpper(Tr); return Json(CekOrnek.TedarikciAdlari.Where(t => q == "" || t.ToUpper(Tr).Contains(q)).Take(20).ToList()); }
        [HttpGet("DownloadAttachment/{ibrazBelgeNo}")] public IActionResult DownloadAttachment(string ibrazBelgeNo) => Content("Ön yüz örneği: ibraz belgesi eki üretilmez.", "text/plain; charset=utf-8");

        public class ReportTemplateModel { public string RaporAdi { get; set; } public string SablonVerisi { get; set; } }
        [HttpPost("SaveUserTemplate")] public IActionResult SaveUserTemplate([FromBody] ReportTemplateModel model) => Json(new { success = true });
        [HttpGet("GetUserTemplate")] public IActionResult GetUserTemplate(string raporAdi) => Json(null);
        [HttpGet("GetFarkliIslemler")] public IActionResult GetFarkliIslemler() => Json(new { success = true, data = new object[0] });
        [HttpPost("SyncUBe1Aktar")] public IActionResult SyncUBe1Aktar([FromBody] object model) => Json(new { success = true, message = "Kayıt eşitlendi (ön yüz örneği)." });
    }
}
