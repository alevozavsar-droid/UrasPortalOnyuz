using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// İhracat Belge Gönderim Takibi — Satış ve Operasyon › Yurtdışı Satış › İhracat Operasyon Takibi (Yönetim › Özel Raporlar'da aynı ekran). Üç sekme:
    ///   Aylık Özet        : ay ay belge gönderimi (adet; akreditif ibrazı / vesaik / müşteri), belge adedi, gönderim masrafı, teslim, ibraz süresi uyumu, rezerv,
    ///                       tahsil edilen; 12 ay + Toplam + Aylık Ort.; operasyon uzmanı, kargo firması ve alıcı tipi tabloları (sade)
    ///   Müşteri Bazlı     : cari kart bilgileri (CARİ_MASTER) ile müşteri başına gönderim / masraf / akreditif sonucu
    ///   Gönderim Listesi  : tüm belge gönderimleri filtre, KPI, matris ve detay drawer'ı (belge seti, akreditif ve ibraz, kargo, hareketler, cari kart) ile
    /// Veri kaynakları ve örnek kayıtlar: Data/IhracatBelgeOrnek.cs (müşteri kartları Data/CariKartOrnek.cs). Filtreleme / özetler istemcide (wwwroot/js/ihracat-belge.js).
    /// </summary>
    [Authorize]
    public class IhracatBelgeController : Controller
    {
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "İhracat Belge Gönderim Takibi";
            ViewBag.Sirketler = IhracatBelgeOrnek.Sirketler;
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Sekme = sekme == "musteri" || sekme == "liste" ? sekme : "ozet";
            return View();
        }

        [HttpGet]
        public IActionResult Veri()
        {
            lock (IhracatBelgeOrnek.Kilit)
            {
                var liste = IhracatBelgeOrnek.Liste;
                return Json(new
                {
                    success = true,
                    bugun = DateTime.Today.ToString("yyyy-MM-dd"),
                    yil = DateTime.Today.Year,
                    sirketler = IhracatBelgeOrnek.Sirketler,
                    durumlar = IhracatBelgeOrnek.Durumlar,
                    odemeSekilleri = IhracatBelgeOrnek.OdemeSekilleri,
                    aliciTipleri = IhracatBelgeOrnek.AliciTipleri,
                    amaclar = IhracatBelgeOrnek.Amaclar,
                    bankaDurumlari = IhracatBelgeOrnek.BankaDurumlari,
                    ibrazRiskleri = IhracatBelgeOrnek.IbrazRiskleri,
                    belgeTurleri = IhracatBelgeOrnek.BelgeTurleri,
                    kargoFirmalari = IhracatBelgeOrnek.KargoAdlari,
                    gonderimTurleri = IhracatBelgeOrnek.GonderimTurleri,
                    gonderenler = IhracatBelgeOrnek.Gonderenler,
                    // Cari kart filtre listeleri Müşteri Cari Kartları ekranıyla aynı kaynaktan (CariKartOrnek §14)
                    siniflar = CariKartOrnek.Siniflar("Müşteri", "Yurtdışı"),
                    ulkeler = IhracatBelgeOrnek.Musteriler().Select(k => k.Ulke).Distinct().OrderBy(x => x, StringComparer.Create(CariKartOrnek.Tr, false)),
                    iliskiDurumlari = CariKartOrnek.IliskiDurumlari,
                    roller = CariKartOrnek.Roller,
                    faaliyetHiyerarsisi = CariKartOrnek.FaaliyetHiyerarsisi,
                    erpSirketleri = IhracatBelgeOrnek.Musteriler().Select(k => k.Sirket).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x),
                    cariler = IhracatBelgeOrnek.Musteriler().Select(CariDto),
                    gonderimler = liste.Select(t => new
                    {
                        t.Id, t.No, tarih = t.Tarih.ToString("yyyy-MM-dd"), tarihTr = Trh(t.Tarih), t.Sirket, t.DosyaNo, t.FaturaNo, faturaTarihiTr = Trh(t.FaturaTarihi), t.FaturaTutari, t.ParaBirimi, t.Kur, t.FaturaTutariTl, t.OdemeSekli, t.TeslimSekli, yuklemeTr = Trh(t.YuklemeTarihi),
                        t.CariKodu, t.Cari, t.Ulke, t.Sehir, t.AliciTipi, t.Alici, t.AliciAdres, t.AliciUlke, t.AliciKisi, t.Amac,
                        t.AkreditifNo, t.AmirBanka, t.IhbarBankasi, ibrazSonTarihi = t.IbrazSonTarihi?.ToString("yyyy-MM-dd"), ibrazSonTr = Trh(t.IbrazSonTarihi), akreditifVadesiTr = Trh(t.AkreditifVadesi), bankaDurumu = t.BankaDurumu ?? "", t.RezervNotu, odemeTr = Trh(t.OdemeTarihi), ibrazRiski = t.IbrazRiski, ibrazKalanGun = t.IbrazKalanGun,
                        belgeAdedi = t.BelgeAdedi, belgeTurSayisi = t.Belgeler.Count, belgeler = t.Belgeler.Select(b => new { b.Sira, b.Tur, b.No, b.Adet, b.Nitelik, b.Not }), belgeOzeti = string.Join(", ", t.Belgeler.Select(b => b.Tur + (b.Adet > 1 ? " ×" + b.Adet : ""))),
                        t.KargoFirmasi, t.GonderimTuru, t.GonderimNo, t.GonderimNoLink, t.ZarfAdedi, t.Agirlik, t.Masraf, t.MasrafPB, t.MasrafDoviz, t.MasrafKime,
                        tahminiTeslimTr = Trh(t.TahminiTeslim), teslimTarihi = t.TeslimTarihi?.ToString("yyyy-MM-dd"), teslimTr = Trh(t.TeslimTarihi), t.TeslimAlan, teslimSuresi = t.TeslimSuresi, t.Durum, t.SonHareket,
                        hareketler = t.Hareketler.OrderBy(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("dd.MM.yyyy HH:mm"), h.Yer, h.Aciklama, h.Tip }),
                        t.Gonderen, t.Onaylayan, t.Not, t.Kaynak
                    })
                });
            }
        }

        private static string Trh(DateTime? d) => d?.ToString("dd.MM.yyyy") ?? "";

        /// <summary>CARİ_MASTER alanları (§3 sırası) + faaliyet satırları — Müşteri Cari Kartları ekranıyla aynı standart.</summary>
        private static object CariDto(CariKart k) => new
        {
            k.Kod, k.KisaAd, k.KisaltmaliUnvan, k.ResmiUnvan, k.VergiNo, k.VergiDairesi, k.KartTipi, k.Bolge, k.Ulke, k.Sehir, k.Sinif, k.IliskiDurumu, k.Rol, k.EsCariKodu,
            esKartAd = string.IsNullOrEmpty(k.EsCariKodu) ? "" : (CariKartOrnek.KartBul(k.EsCariKodu)?.KisaAd ?? ""),
            k.Yetkili, k.Telefon, k.Eposta, k.VadeSarti, k.Durum, durumKaynagi = k.DurumKaynagi, sonHareketTr = Trh(CariKartOrnek.SonHareket(k).Tarih), k.Not, k.Sirket, olusturmaTarihi = Trh(k.OlusturmaTarihi), k.Olusturan, k.KaynakTalepNo, anaFaaliyet = k.AnaFaaliyet,
            faaliyetler = k.Faaliyetler.Select(f => new { f.Sira, f.AnaGrup, f.Grup, f.SatisTemsilcisi, f.AnaFaaliyet, f.UrunSinifi, f.UrunGrubu, f.UrunSerisi, f.UrunAdi, f.Durum, baslangicTr = Trh(f.BaslangicTarihi), f.Not }),
            temsilciler = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()),
            anaGruplar = string.Join(", ", k.Faaliyetler.Select(f => f.AnaGrup).Distinct())
        };
    }
}
