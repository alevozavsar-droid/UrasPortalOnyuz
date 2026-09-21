using WebApplication3.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>İrsaliye / Fatura Aktarım Denetimi — örnek belgelerle.</summary>
    [Authorize]
    [Route("[controller]")]
    public class Rapor145Controller : Controller
    {
        [HttpGet(""), HttpGet("Index")]
        public IActionResult Index() { ViewBag.KaynakDb = "URASKIMYA"; ViewBag.HedefDb = "SELVI"; ViewBag.Milat = "14.04.2026"; ViewBag.KaynakCari = "M1408"; return View(); }

        private static List<Dictionary<string, object>> Belgeler(string objType) => OrnekVeri.AktarimBelgeleri(objType).Cast<Dictionary<string, object>>().ToList();

        [HttpGet("GetOzetData")]
        public IActionResult GetOzetData()
        {
            var k = Belgeler("15").Concat(Belgeler("13")).ToList();
            int akan = k.Count(x => x["HedefDocEntry"] != null), akmayan = k.Count(x => x["HedefDocEntry"] == null);
            return Json(new { success = true, data = new { akan, akmayan, hatali = 5, kuyrukta = 0, atlanan = 12, servisBaslangic = "14.04.2026" } });
        }
        [HttpGet("GetAkanData")] public IActionResult GetAkanData() => Json(new { success = true, data = Belgeler("15").Concat(Belgeler("13")).Where(x => x["HedefDocEntry"] != null).Select(x => new { ObjType = (int)x["DocEntry"] > 17000 ? "13" : "15", KaynakTipAdi = (int)x["DocEntry"] > 17000 ? "Satış Faturası" : "Satış İrsaliyesi", HedefTipAdi = (int)x["DocEntry"] > 17000 ? "Alış Faturası" : "Alış İrsaliyesi", DocEntry = x["DocEntry"], DocNum = x["DocNum"], DocDate = x["DocDate"], DocTotal = x["DocTotal"], HedefDocEntry = x["HedefDocEntry"], HedefDocNum = x["HedefDocNum"], HedefTarih = x["HedefTarih"], HedefTutar = x["HedefTutar"], CariAdi = "SELVİ KİMYA A.Ş." }).ToList() });
        [HttpGet("GetAkmayanData")] public IActionResult GetAkmayanData() => Json(new { success = true, data = Belgeler("15").Concat(Belgeler("13")).Where(x => x["HedefDocEntry"] == null).Select(x => new { ObjType = (int)x["DocEntry"] > 17000 ? "13" : "15", KaynakTipAdi = (int)x["DocEntry"] > 17000 ? "Satış Faturası" : "Satış İrsaliyesi", HedefTipAdi = (int)x["DocEntry"] > 17000 ? "Alış Faturası" : "Alış İrsaliyesi", DocEntry = x["DocEntry"], DocNum = x["DocNum"], DocDate = x["DocDate"], DocTotal = x["DocTotal"], CariAdi = "SELVİ KİMYA A.Ş.", KuyruktaMi = false, Durum = "", HataMesaji = "" }).ToList() });
        [HttpGet("GetHataData")] public IActionResult GetHataData() => Json(new { success = true, data = new object[] {
            new { Id = 1, ObjType = "15", DocEntry = "866", DocNum = 866, TipAdi = "Satış İrsaliyesi", Durum = "E", HataMesaji = "[15-866] Kaynak belgedeki şu stokların hedefte aktif karşılığı yok -> UR09.PP.EQGY.10, UR09.PP.EQYE.10", IslemTarihi = DateTime.Now.AddHours(-2), DenemeSayisi = 3, Sebep = "Kalem eşleşmesi yok", Detay = "Hedef şirkette bu kalemler tanımlı değil.", Cozum = "Kalemleri eşleştirip Kuyruğa Al deyin.", AltBelgeObjType = "", AltBelgeDocEntry = "", Bloke = true, Etiketler = new string[] { "Belge 15/866" } },
            new { Id = 2, ObjType = "13", DocEntry = "17576", DocNum = 17576, TipAdi = "Satış Faturası", Durum = "E", HataMesaji = "[13-17576] HATA: Faturanın bağlı olduğu Alt Belge (Kaynak ObjType: 15, DocEntry: 17574) henüz SELVI şirketinde bulunamadı! Kuyruğa eklendi.", IslemTarihi = DateTime.Now.AddHours(-1), DenemeSayisi = 2, Sebep = "Faturanın bağlı olduğu irsaliye henüz hedefte yok.", Detay = "Önce irsaliye aktarılmalı.", Cozum = "Alt belgeyi kuyruğa alın.", AltBelgeObjType = "15", AltBelgeDocEntry = "17574", Bloke = true, Etiketler = new string[] { "Belge 13/17576", "Önce: 15/17574" } }
        } });
        [HttpGet("GetAtlananData")] public IActionResult GetAtlananData() => Json(new { success = true, data = Enumerable.Range(1, 12).Select(i => new { Id = 100 + i, ObjType = i % 2 == 0 ? "13" : "15", DocEntry = (700 + i).ToString(), DocNum = 700 + i, TipAdi = i % 2 == 0 ? "Satış Faturası" : "Satış İrsaliyesi", HataMesaji = "Bilerek atlandı: numune belge", IslemTarihi = DateTime.Today.AddDays(-i) }) });
        [HttpGet("GetEslesmeSorunlari")] public IActionResult GetEslesmeSorunlari() => Json(new { success = true, toplam = 2, data = new object[] { new { KaynakKod = "UR09.PP.EQGY.10", KaynakAd = "PF GREY (10 KG)", HedefKod = "", Adaylar = "T07.PP.EQGY.10", BelgeSayisi = 3, ObjType = "15", DocEntry = "866" }, new { KaynakKod = "UR09.PP.EQYE.10", KaynakAd = "PF YELLOW (10 KG)", HedefKod = "", Adaylar = "T07.PP.EQYE.10", BelgeSayisi = 3, ObjType = "15", DocEntry = "866" } } });
        [HttpGet("GetEslesmeler")] public IActionResult GetEslesmeler() => Json(new { success = true, data = OrnekVeri.Kalemler.Take(8).Select(k => new { KaynakKod = k.Kod, KaynakAd = k.Ad, HedefKod = "T07." + k.Kod.Substring(5), HedefAd = k.Ad, Kaynak = "U_BE1_URASKODU" }) });
        [HttpGet("GetSebepDagilimi")] public IActionResult GetSebepDagilimi() => Json(new { success = true, data = new object[] { new { Sebep = "Kalem eşleşmesi yok", Adet = 3 }, new { Sebep = "Alt belge hedefte yok", Adet = 1 }, new { Sebep = "E-belge numarası hatalı", Adet = 1 } } });
        [HttpGet("GetKalemAdaylari")] public IActionResult GetKalemAdaylari(string kaynakKod) => Json(new { success = true, kaynakAd = OrnekVeri.Kalemler.FirstOrDefault(k => k.Kod == kaynakKod).Ad, data = OrnekVeri.Kalemler.Take(6).Select(k => new { ItemCode = "T07." + k.Kod.Substring(5), ItemName = k.Ad, UrasKodu = "", Puan = 90 }) });
        [HttpPost("KuyrugaEkle")] public IActionResult KuyrugaEkle([FromBody] List<Dictionary<string, string>> ogeler) => Json(new { success = true, data = (ogeler ?? new List<Dictionary<string, string>>()).Select(o => new { etiket = o.TryGetValue("Etiket", out var e) ? e : "?", basarili = true, mesaj = "Örnek: kuyruğa eklendi (bellek içi)." }) });
        [HttpPost("KalemEslestir")] public IActionResult KalemEslestir() => Json(new { success = true, message = "Örnek: eşleştirme kaydedildi." });
        [HttpPost("BagiKaldir")] public IActionResult BagiKaldir() => Json(new { success = true, message = "Örnek: bağ kaldırıldı." });

        [HttpGet("GetAylikKarsilastirma")]
        public IActionResult GetAylikKarsilastirma(string objType = "15")
        {
            var k = Belgeler(objType);
            var fazla = Enumerable.Range(1, 6).Select(i => new Dictionary<string, object> { ["DocEntry"] = 4600 + i, ["DocNum"] = 4600 + i, ["DocDate"] = new DateTime(2026, 8, 3 + i * 4), ["DocTotal"] = 6000m * i, ["DocCur"] = "TRY", ["CardCode"] = "T0223", ["KaynakEntry"] = "", ["NumAtCard"] = $"URS2026{3100 + i:0000000}" }).ToList();
            return Json(new { success = true, tipAdi = objType == "13" ? "Satış Faturası" : "Satış İrsaliyesi", tutarKiyasla = objType == "13" || objType == "14", kaynak = k, fazla });
        }
        [HttpGet("GetKalemKarsilastirma")]
        public IActionResult GetKalemKarsilastirma(string objType, int docEntry, int hedefDocEntry)
        {
            var rnd = new Random(docEntry);
            return Json(new { success = true, tutarKiyasla = objType == "13", satirlar = OrnekVeri.Kalemler.Take(6).Select((x, i) => { decimal m = rnd.Next(1, 20); bool fark = i == 2; return new { itemCode = x.Kod, hedefKod = "T07." + x.Kod.Substring(5), ad = x.Ad, birim = x.Birim, kMiktar = m, kFiyat = x.Fiyat, kTutar = m * x.Fiyat, hMiktar = fark ? m - 1 : m, hFiyat = x.Fiyat, hTutar = (fark ? m - 1 : m) * x.Fiyat, eslesme = "kod", durum = fark ? "fark" : "ayni", farkNedeni = fark ? "miktar, tutar" : "" }; }) });
        }
        [HttpPost("HedefIptalVeYenidenAktar")] public IActionResult HedefIptalVeYenidenAktar([FromBody] List<Dictionary<string, object>> istekler) => Json(new { success = true, data = (istekler ?? new List<Dictionary<string, object>>()).Select(i => new { etiket = i.TryGetValue("Etiket", out var e) ? e?.ToString() : "?", basarili = false, mesaj = "Ön yüz örneği: SAP'de iptal yapılmaz." }) });
        [HttpPost("HedefOnar")] public IActionResult HedefOnar([FromBody] List<Dictionary<string, object>> istekler) => Json(new { success = true, data = (istekler ?? new List<Dictionary<string, object>>()).Select(i => new { etiket = i.TryGetValue("Etiket", out var e) ? e?.ToString() : "?", basarili = false, mesaj = "Ön yüz örneği: Service Layer yaması yapılmaz." }) });
    }
}
