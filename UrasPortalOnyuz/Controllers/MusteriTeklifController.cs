using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication3.Data;

namespace WebApplication3.Controllers
{
    /// <summary>
    /// Müşteri Teklifleri — Satış ve Operasyon › Müşteri ve CRM Yönetimi › Teklif Yönetimi (Yönetim › Özel Raporlar'da aynı ekran). Üç sekme:
    ///   Aylık Teklif Özeti : ay ay verilen teklif (adet / tutar), satışa dönen (adet / tutar), dönüşüm oranı, açık ve kaybedilen teklifler;
    ///                        12 ay + Toplam + Aylık Ort.; şirket bazlı ve satış temsilcisi bazlı kırılım
    ///   Müşteri Bazlı      : cari kart bilgileri (CARİ_MASTER) ile birlikte müşteri başına teklif / satışa dönüşüm özeti
    ///   Teklif Listesi     : tüm grup şirketlerinin teklifleri filtre, KPI, matris ve detay drawer'ı (teklif kalemleri + cari kart bilgileri) ile
    /// Veri kaynakları ve örnek kayıtlar: Data/TeklifOrnek.cs (müşteri kartları Data/CariKartOrnek.cs). Filtreleme / özetler istemcide (wwwroot/js/musteri-teklif.js).
    /// </summary>
    [Authorize]
    public class MusteriTeklifController : Controller
    {
        /// <param name="sekme">ozet (varsayılan) | musteri | liste (derin bağlantı)</param>
        public IActionResult Index(string sekme)
        {
            ViewData["Title"] = "Müşteri Teklifleri";
            ViewBag.Sirketler = TeklifOrnek.Sirketler;
            ViewBag.Yil = DateTime.Today.Year;
            ViewBag.Sekme = sekme == "musteri" || sekme == "liste" ? sekme : "ozet";
            return View();
        }

        /// <summary>Konsolide teklif listesi + teklif verilen müşterilerin cari kart bilgileri + seçim listeleri. Tarih / yıl filtreleri istemcide uygulanır.</summary>
        [HttpGet]
        public IActionResult Veri()
        {
            lock (TeklifOrnek.Kilit)
            {
                var liste = TeklifOrnek.Liste;
                return Json(new
                {
                    success = true,
                    bugun = DateTime.Today.ToString("yyyy-MM-dd"),
                    yil = DateTime.Today.Year,
                    sirketler = TeklifOrnek.Sirketler,
                    durumlar = TeklifOrnek.Durumlar,
                    anaGruplar = TeklifOrnek.AnaGruplar,
                    kaybetmeNedenleri = TeklifOrnek.KaybetmeNedenleri,
                    temsilciler = liste.Select(t => t.Temsilci).Distinct().OrderBy(x => x, StringComparer.Create(CariKartOrnek.Tr, false)),
                    // Cari kart filtre listeleri Müşteri Cari Kartları ekranıyla aynı kaynaktan (CariKartOrnek §14)
                    siniflar = CariKartOrnek.Siniflar("Müşteri", "Yurtiçi").Union(CariKartOrnek.Siniflar("Müşteri", "Yurtdışı")).Distinct(),
                    bolgeler = CariKartOrnek.Bolgeler,
                    ulkeler = TeklifOrnek.Musteriler().Select(k => k.Ulke).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x == "Türkiye" ? "" : x, StringComparer.Create(CariKartOrnek.Tr, false)),
                    iliskiDurumlari = CariKartOrnek.IliskiDurumlari,
                    roller = CariKartOrnek.Roller,
                    faaliyetHiyerarsisi = CariKartOrnek.FaaliyetHiyerarsisi,
                    erpSirketleri = TeklifOrnek.Musteriler().Select(k => k.Sirket).Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x),
                    paraBirimleri = liste.Select(t => t.ParaBirimi).Distinct().OrderBy(x => x == "TL" ? "" : x),
                    kurlar = TeklifOrnek.Kurlar,
                    cariler = TeklifOrnek.Musteriler().Select(CariDto),
                    teklifler = liste.Select(t => new
                    {
                        t.Id, t.No, tarih = t.Tarih.ToString("yyyy-MM-dd"), tarihTr = Trh(t.Tarih), t.Sirket, t.CariKodu, t.Cari, t.Temsilci, t.AnaGrup, t.FaaliyetGrubu, t.Konu,
                        t.Tutar, t.ParaBirimi, t.Kur, t.TutarTl, gecerlilik = t.Gecerlilik.ToString("yyyy-MM-dd"), gecerlilikTr = Trh(t.Gecerlilik), t.Durum, t.Revizyon,
                        kararTarihi = t.KararTarihi?.ToString("yyyy-MM-dd"), kararTr = Trh(t.KararTarihi), t.SiparisNo, siparisTarihi = t.SiparisTarihi?.ToString("yyyy-MM-dd"), siparisTr = Trh(t.SiparisTarihi),
                        t.SatisTutariTl, t.FaturaNo, t.KaybetmeNedeni, t.Aciklama, t.Hazirlayan, t.Onaylayan, t.Kaynak, kalemSayisi = t.Kalemler.Count,
                        kalemler = t.Kalemler.Select(k => new { k.Sira, k.UrunSinifi, k.UrunGrubu, k.UrunSerisi, k.Urun, k.Miktar, k.Birim, k.BirimFiyat, k.Tutar, hiyerarsi = string.Join(" > ", new[] { k.UrunSinifi, k.UrunGrubu, k.UrunSerisi }.Where(x => !string.IsNullOrEmpty(x))) })
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
            faaliyetler = k.Faaliyetler.Select(f => new { f.Sira, f.AnaGrup, f.Grup, f.SatisTemsilcisi, f.AnaFaaliyet, f.UrunSinifi, f.UrunGrubu, f.UrunSerisi, f.UrunAdi, f.Durum, baslangicTr = Trh(f.BaslangicTarihi), f.Not, hiyerarsi = string.Join(" > ", new[] { f.UrunSinifi, f.UrunGrubu, f.UrunSerisi, f.UrunAdi }.Where(x => !string.IsNullOrEmpty(x))) }),
            temsilciler = string.Join(", ", k.Faaliyetler.Select(f => f.SatisTemsilcisi).Where(x => !string.IsNullOrEmpty(x)).Distinct()),
            anaGruplar = string.Join(", ", k.Faaliyetler.Select(f => f.AnaGrup).Distinct())
        };
    }
}
