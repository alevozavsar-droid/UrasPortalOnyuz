using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Numune Gönderim Takip Raporu — Satış ve Operasyon › Müşteri ve CRM Yönetimi › Numune Yönetimi (Yönetim › Özel Raporlar'da aynı ekran). Üç sekme:
    ///   Aylık Numune Özeti : ay ay gönderilen numune (adet, yurtiçi / yurtdışı), numune bedeli, gönderim masrafı (kargo + gümrük + sigorta), teslim,
    ///                        teklife / siparişe dönen; 12 ay + Toplam + Aylık Ort.; şirket, kargo firması, temsilci ve amaç kırılımı
    ///   Müşteri Bazlı      : cari kart bilgileri (CARİ_MASTER) ile müşteri başına numune / masraf / sonuç özeti
    ///   Numune Listesi     : tüm gönderimler filtre, KPI, matris ve detay drawer'ı (kalemler, kargo takibi, hareketler, sonuç, cari kart) ile
    /// Veri kaynakları ve örnek kayıtlar: Data/NumuneOrnek.cs (müşteri kartları Data/CariKartOrnek.cs, teklif bağlantısı Data/TeklifOrnek.cs). Filtreleme / özetler istemcide (wwwroot/js/numune-gonderim.js).
    /// </summary>
    [Authorize]
    public class NumuneGonderimController : Controller
    {
        /// <param name="sekme">ozet (varsayılan) | musteri | liste (derin bağlantı)</param>
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Numune Gönderim Takip Raporu";
            ViewBag.Sirketler = NumuneOrnek.Sirketler;
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Sekme = sekme == "musteri" || sekme == "liste" ? sekme : "ozet";
            return View();
        }

        [HttpGet]
        public IActionResult Veri()
        {
            lock (NumuneOrnek.Kilit)
            {
                var liste = NumuneOrnek.Liste;
                return Json(new
                {
                    success = true,
                    bugun = DateTime.Today.ToString("yyyy-MM-dd"),
                    yil = DateTime.Today.Year,
                    sirketler = NumuneOrnek.Sirketler,
                    durumlar = NumuneOrnek.Durumlar,
                    sonuclar = NumuneOrnek.Sonuclar,
                    amaclar = NumuneOrnek.Amaclar,
                    kargoFirmalari = NumuneOrnek.KargoAdlari,
                    gonderimTurleri = NumuneOrnek.GonderimTurleri,
                    masrafKimeler = NumuneOrnek.MasrafKimeler,
                    anaGruplar = TeklifOrnek.AnaGruplar,
                    temsilciler = liste.Select(t => t.Temsilci).Distinct().OrderBy(x => x, StringComparer.Create(CariKartOrnek.Tr, false)),
                    // Cari kart filtre listeleri Müşteri Cari Kartları ekranıyla aynı kaynaktan (CariKartOrnek §14)
                    siniflar = CariKartOrnek.Siniflar("Müşteri", "Yurtiçi").Union(CariKartOrnek.Siniflar("Müşteri", "Yurtdışı")).Distinct(),
                    bolgeler = CariKartOrnek.Bolgeler,
                    ulkeler = NumuneOrnek.Musteriler().Select(k => k.Ulke).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x == "Türkiye" ? "" : x, StringComparer.Create(CariKartOrnek.Tr, false)),
                    iliskiDurumlari = CariKartOrnek.IliskiDurumlari,
                    roller = CariKartOrnek.Roller,
                    faaliyetHiyerarsisi = CariKartOrnek.FaaliyetHiyerarsisi,
                    erpSirketleri = NumuneOrnek.Musteriler().Select(k => k.Sirket).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x),
                    paraBirimleri = liste.Select(t => t.ParaBirimi).Distinct().OrderBy(x => x == "TL" ? "" : x),
                    cariler = NumuneOrnek.Musteriler().Select(CariDto),
                    numuneler = liste.Select(t => new
                    {
                        t.Id, t.No, tarih = t.Tarih.ToString("yyyy-MM-dd"), tarihTr = Trh(t.Tarih), t.Sirket, t.CariKodu, t.Cari, t.Bolge, t.Ulke, t.Sehir, t.AliciKisi, t.AliciAdres, t.AliciTelefon, t.Temsilci, t.AnaGrup, t.FaaliyetGrubu, t.Amac, t.Konu,
                        kalemSayisi = t.Kalemler.Count, kalemler = t.Kalemler.Select(k => new { k.Sira, k.Urun, k.Parti, k.Miktar, k.Birim, k.BirimBedel, k.Bedel, hiyerarsi = string.Join(" › ", new[] { k.UrunSinifi, k.UrunGrubu, k.UrunSerisi }.Where(x => !string.IsNullOrEmpty(x))) }),
                        t.NumuneBedeli, t.ParaBirimi, t.Kur, t.ProformaBedeli,
                        t.KargoFirmasi, t.GonderimTuru, t.GonderimNo, t.GonderimNoLink, t.Agirlik, t.Desi, t.KargoMasrafi, t.MasrafPB, t.MasrafDoviz, t.GumrukMasrafi, t.SigortaMasrafi, toplamMasraf = t.ToplamMasraf, t.MasrafKime,
                        tahminiTeslim = t.TahminiTeslim.ToString("yyyy-MM-dd"), tahminiTeslimTr = Trh(t.TahminiTeslim), teslimTarihi = t.TeslimTarihi?.ToString("yyyy-MM-dd"), teslimTr = Trh(t.TeslimTarihi), t.TeslimAlan, teslimSuresi = t.TeslimSuresi,
                        t.Durum, t.SonHareket, hareketler = t.Hareketler.OrderBy(h => h.Zaman).Select(h => new { zaman = h.Zaman.ToString("dd.MM.yyyy HH:mm"), h.Yer, h.Aciklama, h.Tip }),
                        t.IrsaliyeNo, t.ProformaNo, t.BeyannameNo,
                        t.Sonuc, geriBildirimTr = Trh(t.GeriBildirimTarihi), t.GeriBildirim, t.IlgiliTeklifNo, t.IlgiliSiparisNo, t.IlgiliSatisTl, t.Hazirlayan, t.Onaylayan, t.Not, t.Kaynak
                    })
                });
            }
        }

        private static string Trh(DateTime? d) => d?.ToString("dd.MM.yyyy") ?? "";

        /// <summary>CARİ_MASTER alanları (§3 sırası) + faaliyet satırları — Müşteri Cari Kartları ekranıyla aynı standart (MusteriTeklifController ile aynı DTO).</summary>
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
