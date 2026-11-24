// ============================================================
// SATINALMA FİYAT ANALİZ RAPORU (/UrunFiyatKarsilastirma, 23 Kas 2026) — Tedarik Zinciri › Fiyat ve Alım Analizi.
// Kullanıcı: "Satınalma Fiyat Analizi raporunu geliştirelim, temsilci seçelim, TL fiyat, dövizli fiyat, detaylı rapor hazırla."
//
// Kaynaklar (portalın öbür ekranlarıyla aynı, canlı okunur; ayrı fatura kaydı tutulmaz):
//   • Alış faturaları: tedarikçi cari kartlarının Cari Mutabakat / Cari Ekstre hareketleri (Data/MutabakatOrnek.cs; BelgeTipi = Fatura, Alacak > 0).
//     Tedarikçi Fatura ve Ödeme Takip ile Tedarikçi Konsolide Ödeme Takip raporlarındaki faturalarla AYNI belgeler — tutarlar birbirini tutar.
//   • Tedarikçi, bölge, ülke, cari sınıfı, şirket, vade şartı: cari kart (Data/CariKartOrnek.cs).
//   • Satınalma Temsilcisi: tedarikçi kartının ana faaliyetindeki temsilci; kartta yoksa kartın bağlı olduğu şirketin satınalmacısı
//     (AlimOnayOrnek.Satinalmacilar; Cari Mutabakat'taki iç yetkili kuralıyla aynı).
//   • Kur: fatura günü TCMB serisi (TahsilatOrnek.KurOto) — her alımın ₺, $ ve € birim fiyatı o günün kuruyla hesaplanır.
// Cari ekstre faturalarında kalem ayrıntısı tutulmadığından fatura KALEMLERİ burada türetilir (Konsolide Satış Raporu'ndaki yöntem):
//   tedarikçinin malzeme listesinden (aşağıdaki Katalog; kodlar Konsolide Stok Raporu ve Aylık Alım Onay Ekranı'ndaki stok kodlarıyla aynı) 1 – 3 malzeme seçilir,
//   fatura KDV'siz tutarı bunlara dağıtılır. Türetme belgeye göre deterministiktir (aynı fatura her açılışta aynı kalemleri verir);
//   kalem toplamı fatura KDV'siz tutarına kuruşu kuruşuna eşittir.
// Birim fiyat modeli: ₺ ile alınan malzemede fiyat, kurdaki değişimin bir bölümünü (hammadde %90, ambalaj %75, fason %60, hizmet %55, kira %50) taşır;
//   dövizle alınan malzemede döviz fiyatı piyasa dalgasıyla değişir. Böylece "₺ fiyat arttı ama döviz fiyatı düştü" ayrımı raporda görülebilir.
// VERİ ÖRNEKTİR: portalda gerçek alış faturası kalem kaynağı (SAP satınalma faturası satırları) bağlı değildir; bağlanınca yalnız Kalemler() değişir.
// Ekran: Controllers/Rapor6Controller.cs + Views/Rapor6/Index.cshtml + wwwroot/js/satinalma-fiyat.js
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public static class SatinalmaFiyatOrnek
    {
        /// <summary>Yurtiçi alışta KDV oranı (%); yurtdışı tedarikçi faturaları ithalattır, KDV'sizdir.</summary>
        public const decimal KdvOran = 20m;

        private class Mlz { public string Kod, Ad, Birim, Kat, Pb; public decimal Fiyat; public double Gecis; }
        /// <summary>(kod, ad, birim, kategori, bugünkü birim fiyat, fiyatın para birimi)</summary>
        private static Mlz M(string kod, string ad, string birim, string kat, decimal fiyat, string pb = "TL")
            => new Mlz { Kod = kod, Ad = ad, Birim = birim, Kat = kat, Fiyat = fiyat, Pb = pb, Gecis = kat == "Hammadde" ? 0.90 : kat == "Ambalaj" ? 0.75 : kat == "Fason" ? 0.60 : kat == "Kira" ? 0.50 : 0.55 };

        // Tedarikçi cari kodu → aldığımız malzemeler. Aynı malzemenin birden çok tedarikçisi vardır (Tedarikçi Karşılaştırması sekmesi).
        private static readonly Dictionary<string, Mlz[]> Katalog = new Dictionary<string, Mlz[]>
        {
            ["320.001"] = new[] { M("HM-LDPE-01", "Polietilen LDPE Granül (G03-5)", "kg", "Hammadde", 41.80m), M("HM-EA-02", "Etil Asetat Teknik", "kg", "Hammadde", 38.50m), M("HM-GLK-15", "Monoetilen Glikol", "kg", "Hammadde", 38.00m) },
            ["320.002"] = new[] { M("HM-NAOH-21", "Sodyum Hidroksit %50 Çözelti", "kg", "Hammadde", 14.25m), M("HM-TIO2-22", "Titanyum Dioksit Rutil", "kg", "Hammadde", 118.00m), M("HM-AMN-14", "Amonyak Çözeltisi %25", "kg", "Hammadde", 14.50m),
                                  M("HM-PGM-04", "Karbon Siyahı N330", "kg", "Hammadde", 2.32m, "USD"), M("HM-PGM-01", "Pigment Mavi 15:3", "kg", "Hammadde", 9.95m, "USD") },
            ["320.003"] = new[] { M("AM-KOLI-01", "Oluklu Mukavva Koli 40x30x30", "adet", "Ambalaj", 18.40m), M("AM-STRC-02", "Streç Film 50cm 17µ", "rulo", "Ambalaj", 214.00m), M("AM-MSR-13", "Karton Masura 76 mm", "adet", "Ambalaj", 21.00m) },
            ["320.004"] = new[] { M("FS-BSK-01", "Fason Emprime Baskı İşçiliği", "kg", "Fason", 46.00m), M("FS-KNF-02", "Fason Konfeksiyon Dikim", "adet", "Fason", 28.50m) },
            ["320.005"] = new[] { M("HM-SLK-16", "Silikon Yumuşatıcı Bazı", "kg", "Hammadde", 142.00m), M("HM-DSP-17", "Dispergatör (Islatıcı)", "kg", "Hammadde", 128.00m) },
            ["320.006"] = new[] { M("HZ-TEM-01", "Endüstriyel Temizlik Hizmeti", "adam-gün", "Hizmet", 3_250m), M("SF-TMZ-05", "Endüstriyel Temizlik Kimyasalı", "lt", "Sarf Malzeme", 58.00m) },
            ["320.007"] = new[] { M("HZ-DEN-02", "ISO 9001 Gözetim Denetimi", "adam-gün", "Hizmet", 18_500m) },
            ["320.008"] = new[] { M("HM-AKR-11", "Akrilik Emülsiyon (Binder Reçine)", "kg", "Hammadde", 1.12m, "EUR"), M("HM-KVM-13", "Kıvamlaştırıcı (Sentetik)", "kg", "Hammadde", 1.78m, "EUR"), M("HM-STR-12", "Stiren Akrilik Kopolimer", "kg", "Hammadde", 1.02m, "EUR") },
            ["320.009"] = new[] { M("HZ-PAZ-03", "Yurtdışı Pazarlama ve Temsil Hizmeti", "adam-gün", "Hizmet", 420m, "EUR") },
            ["320.010"] = new[] { M("HM-SLK-16", "Silikon Yumuşatıcı Bazı", "kg", "Hammadde", 138.50m), M("HM-KVM-13", "Kıvamlaştırıcı (Sentetik)", "kg", "Hammadde", 84.00m) },
            ["320.045"] = new[] { M("HM-AKR-11", "Akrilik Emülsiyon (Binder Reçine)", "kg", "Hammadde", 55.20m), M("HM-STR-12", "Stiren Akrilik Kopolimer", "kg", "Hammadde", 49.00m), M("HM-TIO2-22", "Titanyum Dioksit Rutil", "kg", "Hammadde", 121.50m) },
            ["320.058"] = new[] { M("AM-KOLI-01", "Oluklu Mukavva Koli 40x30x30", "adet", "Ambalaj", 18.95m), M("AM-PLT-10", "Ahşap Palet 100x120", "adet", "Ambalaj", 310.00m), M("AM-STRC-02", "Streç Film 50cm 17µ", "rulo", "Ambalaj", 219.00m) },
            ["320.011"] = new[] { M("HZ-KRA-01", "Depo Kirası", "m²", "Kira", 185.00m), M("HZ-KRA-02", "Ofis Kirası", "m²", "Kira", 310.00m) }
        };

        /// <summary>Süreçten bağımsız kararlı tohum (string.GetHashCode her çalıştırmada değişir).</summary>
        private static int Tohum(string s) { unchecked { uint h = 2166136261; foreach (char c in s ?? "") { h ^= c; h *= 16777619; } return (int)(h & 0x7fffffff); } }
        private static string Gun(DateTime t) => t.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        private static string Pb(string pb) => TahsilatOrnek.TlMi(pb) ? "TL" : pb;

        /// <summary>Tedarikçinin satınalma temsilcisi: kartın ana faaliyetindeki temsilci; yoksa bağlı şirketin satınalmacısı.</summary>
        public static string Temsilci(CariKart k)
        {
            string kart = k.Faaliyetler.Where(f => f.AnaFaaliyet).Select(f => f.SatisTemsilcisi).FirstOrDefault(x => !string.IsNullOrEmpty(x)) ?? k.Faaliyetler.Select(f => f.SatisTemsilcisi).FirstOrDefault(x => !string.IsNullOrEmpty(x));
            if (!string.IsNullOrEmpty(kart)) return kart;
            return AlimOnayOrnek.Satinalmacilar.FirstOrDefault(s => s.Sirket == k.Sirket).Ad ?? AlimOnayOrnek.Satinalmacilar[0].Ad;
        }

        /// <summary>Malzemenin o günkü birim fiyatı (kendi para biriminde): bugünkü fiyat × kur geçişi (yalnız ₺ fiyatlı malzemede) × piyasa dalgası. Bugün için çarpan 1'dir.</summary>
        private static decimal ModelFiyat(Mlz m, DateTime t, decimal kurUsd, decimal kurUsdBugun)
        {
            int s = Tohum(m.Kod); double gun = (t.Date - DateTime.Today).TotalDays;
            double a = 0.03 + (s % 60) / 1000.0, periyot = 240 + s % 200, faz = (s % 628) / 100.0, egilim = ((s >> 3) % 13 - 6) / 100.0;
            double dalga = a * (Math.Sin(2 * Math.PI * gun / periyot + faz) - Math.Sin(faz)) + egilim * gun / 365.0;
            double kurCarpan = m.Pb == "TL" && kurUsdBugun > 0 ? Math.Pow((double)(kurUsd / kurUsdBugun), m.Gecis) : 1.0;
            return m.Fiyat * (decimal)(kurCarpan * (1 + dalga));
        }

        /// <summary>Miktar adımı: birime göre; düşük adetli alımda fiyat sapmasın diye adım küçülür.</summary>
        private static decimal Miktar(string birim, decimal ham)
        {
            decimal adim = birim == "kg" ? 25m : birim == "lt" ? 10m : birim == "adam-gün" ? 0.5m : 1m, enAz = birim == "kg" || birim == "lt" ? 1m : birim == "adam-gün" ? 0.25m : 1m;
            while (adim > enAz && ham / adim < 40m) adim = Math.Max(enAz, adim / (adim > 1m ? 5m : 2m));
            return Math.Max(adim, Math.Round(ham / adim, 0, MidpointRounding.AwayFromZero) * adim);
        }

        /// <summary>Raporun tüm verisi: alış faturası kalemleri (her satır bir malzeme alımı; ₺ / $ / € birim fiyat ve tutarlarıyla), bugünkü kurlar.</summary>
        public static object Paket()
        {
            var bugun = DateTime.Today; int id = 0; var satirlar = new List<object>();
            var kurlar = new Dictionary<string, decimal>();
            decimal Kur(string pb, DateTime t) { if (TahsilatOrnek.TlMi(pb)) return 1m; string a = pb + t.ToString("yyyyMMdd"); if (!kurlar.TryGetValue(a, out var v)) kurlar[a] = v = TahsilatOrnek.KurOto(pb, t); return v; }
            decimal usdBugun = Kur("USD", bugun), eurBugun = Kur("EUR", bugun);

            foreach (var k in CariKartOrnek.Kartlar.Where(x => x.KartTipi == "Tedarikçi").OrderBy(x => x.Kod))
            {
                if (!Katalog.TryGetValue(k.Kod, out var liste)) continue;
                var c = MutabakatOrnek.Bul(k.Kod); if (c == null) continue;
                List<MtHareket> har; lock (MutabakatOrnek.Kilit) har = c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Alacak > 0).OrderBy(h => h.Tarih).ThenBy(h => h.Id).ToList();
                string temsilci = Temsilci(k), sirket = k.Sirket == "Selvi Kimya" ? "Selvi" : (k.Sirket ?? ""); bool yurtici = k.Bolge == "Yurtiçi";
                foreach (var h in har)
                {
                    string pb = Pb(h.ParaBirimi); decimal kur = Kur(pb, h.Tarih), kurUsd = Kur("USD", h.Tarih), kurEur = Kur("EUR", h.Tarih);
                    decimal net = Math.Round(h.Alacak / (1 + (yurtici ? KdvOran : 0m) / 100m), 2); if (net <= 0) continue;
                    var rnd = new Random(Tohum(k.Kod + "|" + h.BelgeNo));
                    // faturanın para birimiyle fiyatlanan malzemeler önce; yoksa tedarikçinin bütün malzemeleri (fiyat fatura kuruna çevrilir)
                    var aday = liste.Where(m => m.Pb == pb).ToList(); if (aday.Count == 0) aday = liste.ToList();
                    int n = Math.Min(aday.Count, rnd.Next(1, 4));
                    var secilen = aday.OrderBy(x => rnd.Next()).Take(n).ToList();
                    var agirlik = secilen.Select(x => 1 + rnd.NextDouble() * 2).ToList(); double top = agirlik.Sum(); decimal kalan = net;
                    for (int i = 0; i < secilen.Count; i++)
                    {
                        var m = secilen[i]; decimal pay = i == secilen.Count - 1 ? kalan : Math.Round(net * (decimal)(agirlik[i] / top), 2); kalan -= pay; if (pay <= 0) continue;
                        // model fiyat (malzemenin para biriminde) → fatura para birimi; kaleme özgü ± %1,5 pazarlık payı
                        decimal model = ModelFiyat(m, h.Tarih, kurUsd, usdBugun) * (m.Pb == pb ? 1m : Kur(m.Pb, h.Tarih) / (kur <= 0 ? 1m : kur)) * (0.985m + (decimal)rnd.NextDouble() * 0.03m);
                        decimal miktar = Miktar(m.Birim, model <= 0 ? 1m : pay / model), fiyat = Math.Round(pay / miktar, 4);
                        decimal fTl = fiyat * kur, tTl = Math.Round(pay * kur, 2);
                        satirlar.Add(new
                        {
                            id = ++id, tarih = Gun(h.Tarih), no = h.BelgeNo ?? "", sirket, cariKod = k.Kod, cari = k.KisaAd ?? "", unvan = k.ResmiUnvan ?? "", bolge = k.Bolge ?? "", ulke = k.Ulke ?? "", sehir = k.Sehir ?? "",
                            sinif = k.Sinif ?? "", iliski = k.IliskiDurumu ?? "", kosul = k.VadeSarti ?? "", temsilci, islem = h.IslemTipi ?? "", kod = m.Kod, ad = m.Ad, kategori = m.Kat, birim = m.Birim, miktar, pb, fiyat, kur,
                            fTl = Math.Round(fTl, 4), fUsd = Math.Round(fTl / kurUsd, 4), fEur = Math.Round(fTl / kurEur, 4), tutar = pay, tTl, tUsd = Math.Round(tTl / kurUsd, 2), tEur = Math.Round(tTl / kurEur, 2)
                        });
                    }
                }
            }
            return new { bugun = Gun(bugun), kur = new { usd = usdBugun, eur = eurBugun }, kdvOran = KdvOran, ornek = true, satirlar };
        }
    }
}
