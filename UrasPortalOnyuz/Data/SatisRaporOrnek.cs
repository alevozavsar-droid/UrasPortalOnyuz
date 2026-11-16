// ============================================================
// KONSOLİDE SATIŞ RAPORU (/SatisRaporu, 13 Kas 2026) — Yurtiçi Satış › Satış Analizleri ve Performans altındaki yedi raporun
// (Satış Raporu, Satış Ekstre Raporu, Satış Analiz Raporu, Satış Analiz Dashboard, Satışçı Analiz Raporu, Konsolide Prim Raporu,
// Tahsilat Prim Tablosu) bütün süzgeç, sütun ve bilgilerini TEK ekranda toplayan raporun veri paketi.
//
// Kaynaklar (portalın öbür ekranlarıyla aynı, canlı okunur; ayrı kayıt tutulmaz):
//   • Faturalar ve tahsilatlar: Cari Mutabakat / Cari Ekstre hareketleri (Data/MutabakatOrnek.cs) — müşteri carilerinin borç faturaları,
//     iade faturaları, tahsilat / çek / senet hareketleri; fatura kalanı ve kapama durumu oradan.
//   • Açık siparişler, teslimatlar ve siparişten kesilen faturalar: Satış Siparişi (Data/SatisSiparisOrnek.cs; Sipariş → Faturalandı).
//   • Müşteri, müşteri grubu, ülke, temsilci, ödeme koşulu, şirket: cari kart (Data/CariKartOrnek.cs); ürün ve maliyet: Fiyat Yönetimi (Data/FiyatOrnek.cs).
//   • Kur: fatura tarihindeki TCMB serisi (TahsilatOrnek.KurOto).
// Cari ekstre faturalarında kalem ayrıntısı tutulmadığından fatura KALEMLERİ burada türetilir: müşterinin cari kartındaki faaliyet gruplarından biri
// (temsilcisiyle birlikte) seçilir, o grubun ürünleri özel / liste fiyatıyla fatura tutarına dağıtılır. Türetme belgeye göre deterministiktir
// (aynı fatura her açılışta aynı kalemleri verir); kalem toplamı fatura KDV'siz tutarına kuruşu kuruşuna eşittir.
// Ekran: Controllers/SatisRaporuController.cs + Views/SatisRaporu/Index.cshtml + wwwroot/js/satis-raporu.js
// ============================================================
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebApplication3.Data
{
    public static class SatisRaporOrnek
    {
        /// <summary>Yurtiçi satışta KDV oranı (%); yurtdışı müşteri faturaları ihracattır, KDV'sizdir.</summary>
        public const decimal KdvOran = 20m;
        /// <summary>Konsolide Prim Raporu'ndaki prim oranı (%); ekranda değiştirilebilir.</summary>
        public const decimal PrimOran = 1.3m;

        private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        private static readonly ConcurrentDictionary<string, (List<object> kalemler, string temsilci)> _kalemOnbellek = new ConcurrentDictionary<string, (List<object>, string)>();
        private static readonly ConcurrentDictionary<string, List<FyUrun>> _grupUrunleri = new ConcurrentDictionary<string, List<FyUrun>>();

        /// <summary>Süreçten bağımsız kararlı tohum (string.GetHashCode her çalıştırmada değişir).</summary>
        private static int Tohum(string s) { unchecked { uint h = 2166136261; foreach (char c in s ?? "") { h ^= c; h *= 16777619; } return (int)(h & 0x7fffffff); } }
        private static string Gun(DateTime? t) => t.HasValue ? t.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";
        private static string SirketAdi(string ad) => ad == "Selvi Kimya" ? "Selvi" : (ad ?? "");
        private static string Depo(string sirket) => SatisSiparisOrnek.SirketBul(sirket == "Selvi" ? "Selvi Kimya" : sirket).Depo;

        /// <summary>Faaliyet grubunun ürünleri: Fiyat Yönetimi'nde o aileden ürün varsa onlar; yoksa (kağıt, inşaat binderi …) grup adından türetilen ürünler.</summary>
        private static List<FyUrun> GrupUrunleri(CariFaaliyet f)
        {
            string anahtar = (f.AnaGrup ?? "") + "|" + (f.Grup ?? "");
            return _grupUrunleri.GetOrAdd(anahtar, a0 =>
            {
                var l = FiyatOrnek.Urunler.Where(u => u.GrupAnahtari == anahtar && u.Durum == "Aktif").ToList();
                if (l.Count > 0) return l;
                string Kisa(string s) => new string((s ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Where(w => w.Length > 2).Select(w => char.ToUpper(w[0], Tr)).ToArray());
                string onEk = (Kisa(f.AnaGrup) + "-" + Kisa(f.Grup)).Trim('-'); var rnd = new Random(Tohum(anahtar));
                bool kagit = (f.AnaGrup ?? "").StartsWith("Kağıt");
                for (int i = 1; i <= 5; i++)
                    l.Add(new FyUrun { Kod = $"{onEk}-{i:000}", Ad = kagit ? $"{f.Grup} {new[] { 45, 57, 70, 90, 100 }[i - 1]} gr" : $"{Kisa(f.Grup)} {i * 100 + rnd.Next(0, 5) * 10}", AnaGrup = f.AnaGrup, Grup = f.Grup, UrunSinifi = string.IsNullOrEmpty(f.UrunSinifi) ? f.Grup : f.UrunSinifi, UrunGrubu = string.IsNullOrEmpty(f.UrunGrubu) ? "Standart" : f.UrunGrubu, Seri = string.IsNullOrEmpty(f.UrunSerisi) ? Kisa(f.Grup) + " Serisi" : f.UrunSerisi, Birim = "kg", Maliyet = Math.Round(0.9m + (decimal)rnd.NextDouble() * 2.3m, 2) });
                return l;
            });
        }

        /// <summary>Ürünün müşteriye USD birim fiyatı: Muhataplar İçin Özel Fiyat / Liste Fiyatı; bulunamazsa maliyet + %28–44.</summary>
        private static decimal UsdFiyat(string cariKodu, FyUrun u)
        {
            decimal usd = 0; try { usd = SatisSiparisOrnek.Fiyat(cariKodu, u, "USD").fiyat; } catch { }
            return usd > 0 ? usd : Math.Round(u.Maliyet * (1.28m + Tohum(u.Kod + cariKodu) % 17 / 100m), 4);
        }

        /// <summary>Cari ekstre faturasının kalemleri ve temsilcisi (belgeye göre deterministik; kalem toplamı = KDV'siz tutar).</summary>
        private static (List<object> kalemler, string temsilci) KalemTuret(CariKart k, MtHareket h, string pb, decimal net, decimal kur, decimal kurUsd)
        {
            return _kalemOnbellek.GetOrAdd($"{k.Kod}|{h.BelgeNo}|{pb}|{net}|{FiyatOrnek.MaliyetSurum}", a0 =>
            {
                var rnd = new Random(Tohum(k.Kod + "|" + h.BelgeNo)); var l = new List<object>();
                var faaliyetler = k.Faaliyetler.Where(x => x.Durum != "Pasif").ToList(); if (faaliyetler.Count == 0) faaliyetler = k.Faaliyetler.ToList();
                // ana faaliyet ağırlıklı seçim (üçte iki), kalan öbür faaliyet grupları
                CariFaaliyet f = faaliyetler.Count == 0 ? null : (rnd.Next(3) < 2 ? faaliyetler.FirstOrDefault(x => x.AnaFaaliyet) ?? faaliyetler[0] : faaliyetler[rnd.Next(faaliyetler.Count)]);
                string temsilci = f?.SatisTemsilcisi ?? SatisSiparisOrnek.Temsilci(k);
                var aday = f == null ? FiyatOrnek.Urunler.Take(12).ToList() : GrupUrunleri(f);
                int n = Math.Min(aday.Count, Math.Abs(net) < (pb == "TL" ? 150_000m : 9_000m) ? rnd.Next(1, 3) : rnd.Next(2, 5));
                var secilen = aday.OrderBy(x => rnd.Next()).Take(Math.Max(1, n)).ToList();
                var agirlik = secilen.Select(x => 1 + rnd.NextDouble() * 2).ToList(); double top = agirlik.Sum(); decimal kalan = net;
                for (int i = 0; i < secilen.Count; i++)
                {
                    var u = secilen[i]; decimal pay = i == secilen.Count - 1 ? kalan : Math.Round(net * (decimal)(agirlik[i] / top), 2); kalan -= pay;
                    // birim fiyat: USD fiyat → belge para birimi (fatura günü kuru), kaleme özgü ± %12 pazarlık payı; miktar 25 kg'ın katı
                    decimal fiyat = UsdFiyat(k.Kod, u) * kurUsd / (kur <= 0 ? 1 : kur) * (0.88m + (decimal)rnd.NextDouble() * 0.24m);
                    decimal miktar = fiyat <= 0 ? 25 : Math.Max(25, Math.Round(Math.Abs(pay) / fiyat / 25m, 0) * 25m);
                    l.Add(new { kod = u.Kod, ad = u.Ad, anaGrup = u.AnaGrup ?? "", grup = u.Grup ?? "", sinif = u.UrunSinifi ?? "", urunGrubu = u.UrunGrubu ?? "", seri = u.Seri ?? "", birim = u.Birim ?? "kg", miktar = pay < 0 ? -miktar : miktar, fiyat = Math.Round(Math.Abs(pay) / miktar, 4), tutar = pay, maliyetTl = Math.Round((pay < 0 ? -miktar : miktar) * FiyatOrnek.GercekMaliyet(u) * kurUsd, 2) });   // maliyet = üretim + ambalaj (risk payı fiyatlandırma içindir, gerçekleşen maliyet değildir)
                }
                return (l, temsilci);
            });
        }

        /// <summary>Raporun tüm verisi: belgeler (fatura, iade, açık sipariş, teslimat; kalemleriyle), tahsilatlar, müşteri bakiyeleri, kurlar.</summary>
        public static object Paket()
        {
            var bugun = DateTime.Today; int id = 0;
            var belgeler = new List<object>(); var tahsilatlar = new List<object>(); var cariler = new List<object>();
            var kurlar = new Dictionary<string, decimal>();
            decimal Kur(string pb, DateTime t) { if (TahsilatOrnek.TlMi(pb)) return 1m; string a = pb + t.ToString("yyyyMMdd"); if (!kurlar.TryGetValue(a, out var v)) kurlar[a] = v = TahsilatOrnek.KurOto(pb, t); return v; }
            // Tahsilat modülünde işlenen tahsilat satırları (yöntem, banka) → hareket belge no
            var islenen = new Dictionary<string, TahsilatSatir>();
            try { foreach (var t in TahsilatOrnek.Tahsilatlar.Where(x => x.Islendi)) foreach (var s in t.Satirlar) if (!string.IsNullOrEmpty(s.HareketNo)) islenen[t.CariKodu + "|" + s.HareketNo] = s; } catch { }

            foreach (var k in CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Müşteri").OrderBy(x => x.Kod))
            {
                var c = MutabakatOrnek.Bul(k.Kod); if (c == null) continue;
                List<MtHareket> har; lock (MutabakatOrnek.Kilit) har = c.Hareketler.OrderBy(x => x.Tarih).ThenBy(x => x.Id).ToList();
                string anaTemsilci = SatisSiparisOrnek.Temsilci(k), sirket = SirketAdi(k.Sirket); bool yurtici = k.Bolge == "Yurtiçi";
                foreach (var h in har)
                {
                    string pb = TahsilatOrnek.TlMi(h.ParaBirimi) ? "TL" : h.ParaBirimi;
                    bool fatura = h.BelgeTipi == "Fatura" && h.Borc > 0, iade = h.BelgeTipi == "İade Faturası" && h.Alacak > 0;
                    if (fatura || iade)
                    {
                        decimal kur = Kur(pb, h.Tarih), kurUsd = Kur("USD", h.Tarih), kurEur = Kur("EUR", h.Tarih);
                        decimal brut = fatura ? h.Borc : -h.Alacak, kdv = yurtici ? KdvOran : 0m, net = Math.Round(brut / (1 + kdv / 100m), 2);
                        bool fark = (h.BelgeNo ?? "").StartsWith("KFF-") || (h.BelgeNo ?? "").StartsWith("VFF-");   // Tahsilat modülünün kestiği kur / vade farkı faturası: tek hizmet kalemi
                        List<object> kalemler; string temsilci;
                        if (fark) { bool kf = h.BelgeNo.StartsWith("KFF-"); kalemler = new List<object> { new { kod = kf ? "HZM-KUR" : "HZM-VADE", ad = kf ? "Kur Farkı" : "Vade Farkı", anaGrup = "Finansal", grup = "Fark Faturaları", sinif = "Finansal", urunGrubu = "Fark Faturaları", seri = "", birim = "Adet", miktar = 1m, fiyat = net, tutar = net, maliyetTl = 0m } }; temsilci = anaTemsilci; }
                        else (kalemler, temsilci) = KalemTuret(k, h, pb, net, kur, kurUsd);
                        belgeler.Add(new
                        {
                            id = ++id, tur = fatura ? "Fatura" : "İade Faturası", no = h.BelgeNo ?? "", tarih = Gun(h.Tarih), vade = Gun(h.Vade ?? h.Tarih), sirket, cariKod = k.Kod, cari = k.KisaAd ?? "", grup = k.Sinif ?? "", pazar = k.Bolge ?? "", ulke = k.Ulke ?? "", sehir = k.Sehir ?? "", iliski = k.IliskiDurumu ?? "",
                            temsilci = temsilci ?? "", kosul = k.VadeSarti ?? "", pb, kur, kurUsd, kurEur, tutar = brut, net, kalan = fatura ? h.Kalan : 0m, islem = h.IslemTipi ?? "", durum = fatura ? (h.KapamaDurumu ?? "Açık") : "Kapalı", depo = Depo(sirket), kaynak = "Cari Ekstre", aciklama = h.Aciklama ?? "", kalemler, sip = (object)null
                        });
                    }
                    else if (h.Alacak > 0 && (h.BelgeTipi == "Tahsilat" || h.BelgeTipi == "Çek" || h.BelgeTipi == "Senet"))
                    {
                        islenen.TryGetValue(k.Kod + "|" + h.BelgeNo, out var s); string ac = h.Aciklama ?? "";
                        string tur = h.BelgeTipi == "Çek" ? "Çek" : h.BelgeTipi == "Senet" ? "Senet" : s != null ? s.Yontem : TahsilatOrnek.Yontemler.FirstOrDefault(y => ac.StartsWith(y)) ?? "Havale / EFT";
                        bool vadeli = tur == "Çek" || tur == "Senet";
                        // banka: Tahsilat modülünde girilen banka; ekstre tohumu hareketlerinde banka tutulmadığından belgeye göre sabit seçilir
                        string banka = s != null && !string.IsNullOrEmpty(s.Banka) ? s.Banka : tur == "Nakit" ? "" : TahsilatOrnek.Bankalar[Tohum(k.Kod + h.BelgeNo) % TahsilatOrnek.Bankalar.Length];
                        tahsilatlar.Add(new
                        {
                            id = ++id, tarih = Gun(h.Tarih), no = h.BelgeNo ?? "", cariKod = k.Kod, cari = k.KisaAd ?? "", grup = k.Sinif ?? "", pazar = k.Bolge ?? "", ulke = k.Ulke ?? "", iliski = k.IliskiDurumu ?? "", temsilci = anaTemsilci ?? "", sirket, islem = h.IslemTipi ?? "", tur, pb, kur = Kur(pb, h.Tarih), tutar = h.Alacak,
                            hesap = vadeli ? tur + " Portföyü" : tur == "Nakit" ? "Merkez Kasa" : $"{banka} · {pb}", cekNo = vadeli ? (h.BelgeNo ?? "") : "", cekVade = vadeli ? Gun(h.Vade) : "", banka, durum = vadeli ? (h.KapamaDurumu ?? "") : (h.Kalan > 0.005m ? "Kapama Bekliyor" : "İşlendi"), aciklama = ac
                        });
                    }
                }
                // güncel bakiye: ekstre hareketlerinden para birimi bazında (borç − alacak), bugünkü kurla TL
                var bakiye = har.GroupBy(x => TahsilatOrnek.TlMi(x.ParaBirimi) ? "TL" : x.ParaBirimi).Select(g => new { pb = g.Key, net = Math.Round(g.Sum(x => x.Borc - x.Alacak), 2) }).Where(x => x.net != 0).ToList();
                cariler.Add(new { kod = k.Kod, ad = k.KisaAd ?? "", unvan = k.ResmiUnvan ?? "", kartTipi = k.KartTipi ?? "", rol = k.Rol ?? "", durum = k.Durum ?? "", grup = k.Sinif ?? "", pazar = k.Bolge ?? "", ulke = k.Ulke ?? "", sehir = k.Sehir ?? "", iliski = k.IliskiDurumu ?? "", temsilci = anaTemsilci ?? "", sirket, kosul = k.VadeSarti ?? "", bakiye, bakiyeTl = Math.Round(bakiye.Sum(x => x.net * Kur(x.pb, bugun)), 2) });
            }

            // Satış Siparişi: açık siparişler (Sipariş / Hazırlanıyor / Yüklemeye Hazır), teslimatlar (Yüklendi) ve siparişten kesilen faturalar (Faturalandı)
            foreach (var s in SatisSiparisOrnek.Liste(null).Where(x => x.Asama >= 1).OrderBy(x => x.Tarih))
            {
                var k = CariKartOrnek.KartBul(s.CariKodu); bool yurtici = s.Bolge == "Yurtiçi"; bool fat = s.Durum == "Faturalandı";
                var t = fat ? (s.FaturaTarihi ?? s.Tarih) : s.Tarih; string pb = TahsilatOrnek.TlMi(s.ParaBirimi) ? "TL" : s.ParaBirimi;
                decimal kur = Kur(pb, t), kurUsd = Kur("USD", t), kurEur = Kur("EUR", t), net = s.Tutar, brut = Math.Round(net * (1 + (yurtici ? KdvOran : 0m) / 100m), 2);
                var m = Regex.Match(s.OdemeSekli ?? "", @"\d+"); var vade = t.AddDays(m.Success ? int.Parse(m.Value) : 0);
                var kalemler = s.Kalemler.Select(x => { var u = FiyatOrnek.UrunBul(x.UrunKod); var g = (x.Grup ?? "").Split(new[] { " / " }, 2, StringSplitOptions.None); return (object)new { kod = x.UrunKod ?? "", ad = x.Urun ?? "", anaGrup = u?.AnaGrup ?? g[0], grup = u?.Grup ?? (g.Length > 1 ? g[1] : ""), sinif = u?.UrunSinifi ?? "", urunGrubu = u?.UrunGrubu ?? "", seri = u?.Seri ?? "", birim = x.Birim ?? "kg", miktar = x.Miktar, fiyat = x.BirimFiyat, tutar = x.Tutar, maliyetTl = Math.Round(x.Miktar * (u == null ? 0 : FiyatOrnek.GercekMaliyet(u)) * kurUsd, 2) }; }).ToList();
                belgeler.Add(new
                {
                    id = ++id, tur = fat ? "Fatura" : s.Acik ? "Açık Sipariş" : "Teslimat", no = fat && !string.IsNullOrEmpty(s.FaturaNo) ? s.FaturaNo : s.No ?? "", tarih = Gun(t), vade = Gun(vade), sirket = SirketAdi(s.FaturaKesen), cariKod = s.CariKodu ?? "", cari = s.Cari ?? "", grup = k?.Sinif ?? "", pazar = s.Bolge ?? "", ulke = s.Ulke ?? "", sehir = s.Sehir ?? "", iliski = k?.IliskiDurumu ?? "",
                    temsilci = s.Temsilci ?? "", kosul = s.OdemeSekli ?? "", pb, kur, kurUsd, kurEur, tutar = brut, net, kalan = fat ? brut : 0m, islem = "", durum = fat ? "Açık" : s.Durum, depo = s.YuklemeYeri ?? "", kaynak = "Satış Siparişi", aciklama = s.Not ?? "", kalemler,
                    sip = (object)new { no = s.No ?? "", talepNo = s.TalepNo ?? "", siparisTarihi = Gun(s.SiparisTarihi ?? s.Tarih), asama = s.Durum, marka = SatisSiparisOrnek.MarkaBul(s.Marka).Ad, gonderici = SirketAdi(s.Gonderici), yukleme = Gun(s.YuklemeTarihi), saat = s.YuklemeSaati ?? "", yer = s.YuklemeYeri ?? "", nakliye = s.Nakliye ?? "", firma = s.NakliyeFirmasi ?? "", teslim = s.TeslimSekli ?? "", kg = s.ToplamKg, koli = s.ToplamKoli, gecikti = s.Gecikti, faturaNo = s.FaturaNo ?? "", musteriSiparisNo = s.MusteriSiparisNo ?? "", teklifNo = s.TeklifNo ?? "", siparisKaynak = s.Kaynak ?? "" }
                });
            }
            return new { bugun = Gun(bugun), kur = new { usd = Kur("USD", bugun), eur = Kur("EUR", bugun) }, primOran = PrimOran, kdvOran = KdvOran, belgeler, tahsilatlar, cariler };
        }
    }
}
