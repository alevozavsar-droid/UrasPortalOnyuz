// Uras Üretim Portalı ekranlarının AJAX uçları için ÖRNEK veriler (ön yüz örneği; veritabanı yok).
// Görünümlerin (Views/UrasUretim*) beklediği alan adlarıyla birebir üretilir.
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Data
{
    public static class UretimOrnek
    {
        /// <summary>Görünümler alan adlarını C#'taki gibi (PascalCase) bekler; MVC'nin varsayılan camelCase dönüşümü kapatılır.</summary>
        public static readonly System.Text.Json.JsonSerializerOptions PascalCase = new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = null, DictionaryKeyPolicy = null };

        private static object Ok(object data) => new { success = true, ornek = true, message = "Ön yüz örneği", data };
        private static readonly string[] Depolar = { "01", "02", "03" };
        private static readonly string[] DepoAdlari = { "Hammadde Deposu", "Mamul Deposu", "Ambalaj Deposu" };
        private static string Gun(int geri) => DateTime.Today.AddDays(-geri).ToString("yyyy-MM-dd");
        private static string GunSaat(int geri, int saat) => DateTime.Today.AddDays(-geri).AddHours(saat).ToString("dd.MM.yyyy HH:mm");

        // ---------- Ayarlar ----------
        public static object Kullanicilar() => Ok(OrnekVeri.Kullanicilar.Select((k, i) => new { Code = k.Kod, Username = k.Ad, WebCode = k.Yetki == "A" ? "ADMIN" : k.Yetki == "UR" ? "URETIM" : "OPERATOR", Ozel = i % 4 == 0 }).ToList());

        private static readonly (string key, string ad, string grup, string aciklama)[] _tanimlar =
        {
            ("uretim.siparis", "Üretim Siparişi", "Üretim", "Üretim siparişi açma / kapama"),
            ("uretim.saha", "Saha Üretim", "Üretim", "Saha üretim girişleri"),
            ("uretim.silo", "Silo Dolum", "Üretim", "Silo dolum kayıtları"),
            ("kalite.giris", "Giriş Kalite Kontrol", "Kalite", "Hammadde giriş KK"),
            ("kalite.urun", "Ürün Kalite Kontrol", "Kalite", "Mamul KK"),
            ("kalite.tutanak", "Karantina / Red Tutanağı", "Kalite", "Tutanak oluşturma"),
            ("stok.giris", "Mal Girişi", "Stok", "Depoya mal girişi"),
            ("stok.cikis", "Mal Çıkışı", "Stok", "Depodan mal çıkışı"),
            ("stok.sayim", "Stok Sayım", "Stok", "Sayım belgesi"),
            ("rapor.tum", "Raporlar", "Rapor", "Tüm üretim raporları"),
            ("ayar.yetki", "Yetkilendirme", "Ayarlar", "Kullanıcı yetkileri"),
        };
        private static readonly (string kod, string ad)[] _roller = { ("ADMIN", "Yönetici"), ("URETIM", "Üretim Sorumlusu"), ("KALITE", "Kalite Sorumlusu"), ("OPERATOR", "Operatör") };

        public static object Tanimlar() => Ok(new
        {
            tanimlar = _tanimlar.Select(t => new { t.key, t.ad, t.grup, t.aciklama }).ToList(),
            roller = _roller.Select(r => new { r.kod, r.ad }).ToList()
        });

        public static object KullaniciYetki(string code, string webCode)
        {
            var defaults = new Dictionary<string, bool>();
            foreach (var t in _tanimlar)
                defaults[t.key] = webCode == "ADMIN" || (webCode == "URETIM" && (t.grup == "Üretim" || t.grup == "Stok" || t.grup == "Rapor")) || (webCode == "KALITE" && (t.grup == "Kalite" || t.grup == "Rapor")) || (webCode == "OPERATOR" && t.key == "uretim.saha");
            var perms = new Dictionary<string, bool>(defaults);
            if (!string.IsNullOrEmpty(code) && code.Length % 2 == 0) perms["rapor.tum"] = !perms["rapor.tum"];   // "özel yetki" örneği
            return Ok(new { perms, defaults });
        }

        // ---------- Raporlar ----------
        public static object HammaddeTuketim()
        {
            var rnd = new Random(11);
            var kalemler = OrnekVeri.Kalemler.Take(10).Select((k, i) => new { ItemCode = k.Kod, ItemName = k.Ad, Miktar = Math.Round(12000m - i * 950 + rnd.Next(0, 400), 1), Birim = "kg", IsEmri = 40 - i * 3 }).ToList();
            return Ok(new
            {
                KapsamYok = false,
                IsEmriSayisi = 186,
                Kalemler = kalemler,
                KaynakDagilimi = new[] { new { Kaynak = "IGE", KaynakAd = "Üretim Emri Çıkışı", Miktar = 61250m, Satir = 412 }, new { Kaynak = "MAN", KaynakAd = "Elle Çıkış", Miktar = 8900m, Satir = 37 }, new { Kaynak = "SILO", KaynakAd = "Silo Tüketimi", Miktar = 14300m, Satir = 58 } },
                Gunluk = Enumerable.Range(0, 30).Select(i => new { Gun = DateTime.Today.AddDays(-29 + i).ToString("dd.MM"), Miktar = 1800 + rnd.Next(0, 1500) }).ToList(),
                Partiler = kalemler.SelectMany(k => Enumerable.Range(1, 3).Select(p => new { k.ItemCode, Parti = DateTime.Today.AddDays(-p * 9).ToString("yyMMdd") + "-" + p, Miktar = Math.Round(k.Miktar / 3, 1), IsEmri = 1000 + p * 7, IlkKullanim = Gun(p * 9), SonKullanim = Gun(p * 9 - 4), Kaynak = p == 2 ? "MAN" : "IGE" })).ToList(),
                Urunler = kalemler.SelectMany(k => OrnekVeri.Kalemler.Skip(6).Take(2).Select(u => new { k.ItemCode, UrunKodu = u.Kod, UrunAdi = u.Ad, Miktar = Math.Round(k.Miktar / 2, 1), IsEmri = 12 })).ToList()
            });
        }

        public static object LotSira()
        {
            var rnd = new Random(7);
            var kalemler = OrnekVeri.Kalemler.Take(8).Select((k, i) => new
            {
                ItemCode = k.Kod, ItemName = k.Ad, Adet = 12 + i, Atlama = i == 2 ? 1 : 0, Mukerrer = i == 5 ? 1 : 0, Lotsuz = i == 3 ? 2 : 0,
                OworSonLot = DateTime.Today.AddDays(-i).ToString("yyMMdd") + (120 + i).ToString("0000"), OworSonBitis = GunSaat(i, 15), OworSonNo = 120 + i,
                SayacNo = (int?)(120 + i + (i == 6 ? 1 : 0)), SayacUyumsuz = i == 6 ? 1 : 0, SorunVar = (i == 2 || i == 3 || i == 5 || i == 6) ? 1 : 0
            }).ToList();
            var satirlar = kalemler.SelectMany(k => Enumerable.Range(0, 5).Select(j => new
            {
                DocNum = 5000 + rnd.Next(0, 900), k.ItemCode, k.ItemName, Lot = j == 1 && k.Lotsuz > 0 ? null : DateTime.Today.AddDays(-j * 3).ToString("yyMMdd") + (k.OworSonNo - j).ToString("0000"), No = (int?)(k.OworSonNo - j),
                OncekiLot = DateTime.Today.AddDays(-(j + 1) * 3).ToString("yyMMdd") + (k.OworSonNo - j - 1).ToString("0000"), OncekiDocNum = 4990 + rnd.Next(0, 900),
                Sorun = j == 2 && k.Atlama > 0 ? "ATLAMA" : (j == 3 && k.Mukerrer > 0 ? "MUKERRER" : (j == 1 && k.Lotsuz > 0 ? "LOTSUZ" : "")), Atlanan = j == 2 && k.Atlama > 0 ? (k.OworSonNo - 2).ToString() : "",
                BitZam = GunSaat(j * 3, 14), Operator = new[] { "M. Doğan", "E. Kaya", "S. Ay" }[j % 3], Planlanan = 1000 + j * 50, Uretilen = 980 + j * 50, StokParti = j % 2, Status = "L", Durum = "Kapalı"
            })).ToList();
            return Ok(new { Digit = 4, MaxNo = 9999, Kalemler = kalemler, Satirlar = satirlar, Loglar = new[] { new { Tarih = GunSaat(2, 9), ItemCode = kalemler[0].ItemCode, EskiNo = 118, Kullanici = "it02", Aciklama = "Sayaç elle düzeltildi" } } });
        }

        public static object KalemLotSira(string itemCode)
        {
            var k = OrnekVeri.Kalemler.FirstOrDefault(x => x.Kod == itemCode);
            var satirlar = Enumerable.Range(0, 12).Select(j => new { DocNum = 5200 + j, Lot = DateTime.Today.AddDays(-j * 4).ToString("yyMMdd") + (140 - j).ToString("0000"), No = (int?)(140 - j), Sorun = j == 4 ? "ATLAMA" : "", Atlanan = j == 4 ? "0135" : "", BitZam = GunSaat(j * 4, 16), Operator = "M. Doğan", Planlanan = 1000, Uretilen = 990, StokParti = j % 2, Status = "L", Durum = "Kapalı" }).ToList();
            return Ok(new { ItemCode = itemCode, ItemName = k.Ad, Satirlar = satirlar });
        }

        public static object SayimRaporu()
        {
            var rnd = new Random(5);
            var belgeler = Enumerable.Range(0, 6).Select(i => new { DocEntry = 300 + i, DocNum = 300 + i, Tarih = Gun(i * 5), Depolar = Depolar[i % 3], Status = i == 0 ? "O" : "C", SayilanSatir = 18 + i, SatirSayisi = 20 + i, FarkliSatir = 2 + i % 3, Fazla = Math.Round(12.5m + i, 2), Eksik = Math.Round(-8.25m - i, 2) }).ToList();
            var kalemler = belgeler.SelectMany(b => OrnekVeri.Kalemler.Take(6).Select((k, j) =>
            {
                decimal sistem = 100 + j * 25 + rnd.Next(0, 40), sayilan = sistem + (j % 3 == 0 ? rnd.Next(-6, 7) : 0);
                return new { b.DocEntry, b.DocNum, LineNum = j, ItemCode = k.Kod, ItemName = k.Ad, WhsCode = b.Depolar, Sistem = sistem, Sayilan = sayilan, Fark = sayilan - sistem, FarkDeger = Math.Round((sayilan - sistem) * k.Fiyat, 2), Uom = k.Birim, PartiYonetimli = j % 2 == 0 };
            })).ToList();
            return Ok(new
            {
                Belgeler = belgeler, Kalemler = kalemler,
                Partiler = kalemler.Where(k => k.PartiYonetimli).Select(k => new { k.DocEntry, k.LineNum, PartiNo = DateTime.Today.AddDays(-k.LineNum * 7).ToString("yyMMdd") + "-1", k.Sistem, k.Sayilan }).ToList(),
                Depolar = Depolar.Select((d, i) => new { WhsCode = d, WhsName = DepoAdlari[i] }).ToList(),
                Gunluk = belgeler.Select(b => new { Gun = b.Tarih.Substring(5), b.Fazla, b.Eksik }).Reverse().ToList(),
                KapsamDisi = 0
            });
        }

        public static object StokYasi()
        {
            var rnd = new Random(3);
            string[] kovalar = { "0-30", "31-60", "61-90", "91-180", "180+" };
            var satirlar = OrnekVeri.Kalemler.SelectMany((k, i) => Enumerable.Range(1, 2).Select(p =>
            {
                int yas = (i * 23 + p * 41) % 260;
                string kova = yas <= 30 ? kovalar[0] : yas <= 60 ? kovalar[1] : yas <= 90 ? kovalar[2] : yas <= 180 ? kovalar[3] : kovalar[4];
                var giris = DateTime.Today.AddDays(-yas);
                return new { ItemCode = k.Kod, ItemName = k.Ad, Tip = k.Kod.StartsWith("T") ? "HM" : "MM", Parti = giris.ToString("yyMMdd") + "-" + p, WhsCode = Depolar[i % 3], Miktar = Math.Round(50m + rnd.Next(0, 900), 2), Birim = k.Birim, GirisTarihi = giris.ToString("dd.MM.yyyy"), YasGun = yas, Kova = kova, SonKullanma = giris.AddMonths(6).ToString("dd.MM.yyyy"), SuresiDoldu = giris.AddMonths(6) < DateTime.Today ? 1 : 0 };
            })).OrderByDescending(s => s.YasGun).ToList();
            return Ok(new
            {
                Depolar = Depolar.Select((d, i) => new { WhsCode = d, WhsName = DepoAdlari[i] }).ToList(),
                Satirlar = satirlar,
                Kovalar = kovalar.Select(kv => new { Kova = kv, PartiSayisi = satirlar.Count(s => s.Kova == kv), Miktar = satirlar.Where(s => s.Kova == kv).Sum(s => s.Miktar) }).ToList()
            });
        }

        // ---------- Kalem ana verileri ----------
        private static readonly (int kod, string ad)[] _gruplar = { (100, "Hammadde"), (101, "Yarı Mamul"), (102, "Mamul"), (103, "Ambalaj") };
        private static readonly (string kod, string ad)[] _birimler = { ("KG", "Kilogram"), ("ADET", "Adet"), ("LT", "Litre"), ("KOLI", "Koli") };

        public static object ItemGroups() => Ok(_gruplar.Select(g => new { ItmsGrpCod = g.kod, ItmsGrpNam = g.ad }).ToList());
        public static object UomList() => Ok(_birimler.Select(b => new { UomCode = b.kod, UomName = b.ad }).ToList());
        public static object KalemStats() => Ok(new { Toplam = 1284, Etkin = 1197, EtkinDegil = 87, Partili = 640 });

        public static object Kalemler(string search, int page, int pageSize)
        {
            var rnd = new Random(9);
            var tum = OrnekVeri.Kalemler.Select((k, i) => new
            {
                ItemCode = k.Kod, ItemName = k.Ad, ForeignName = k.Ad.ToUpperInvariant(), ItmsGrpCod = _gruplar[i % 4].kod, ItemGroupName = _gruplar[i % 4].ad,
                UomCode = k.Birim.ToUpperInvariant() == "ADET" ? "ADET" : "KG", SecondUomCode = i % 3 == 0 ? "KOLI" : "", ManBtchNum = i % 2 == 0 ? "Y" : "N", Frozenfor = i == 7 ? "Y" : "N", OnHand = Math.Round(100m + rnd.Next(0, 5000), 2)
            }).ToList();
            if (!string.IsNullOrWhiteSpace(search)) { string q = search.ToLower(new System.Globalization.CultureInfo("tr-TR")); tum = tum.Where(x => x.ItemCode.ToLower().Contains(q) || x.ItemName.ToLower(new System.Globalization.CultureInfo("tr-TR")).Contains(q)).ToList(); }
            if (page < 1) page = 1; if (pageSize < 1) pageSize = 50;
            return Ok(new { toplam = tum.Count, liste = tum.Skip((page - 1) * pageSize).Take(pageSize).ToList() });
        }
    }
}
