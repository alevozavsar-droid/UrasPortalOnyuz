using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Data;

namespace WebApplication3.Helpers
{

    public class UretimDisEkran
    {
        public string Controller { get; set; }
        public string Action { get; set; }
        public string Etiket { get; set; }
        public string Icon { get; set; }

        public string Url => "/" + Controller + "/" + Action + "?portal=1";
    }




    public class UretimModul
    {
        public string Key { get; set; }        // controller adı (aktif vurgusu için)
        public string Baslik { get; set; }      // modül başlığı
        public string Ikon { get; set; }        // font-awesome ikon sınıfı (fa-... kısmı)
        public (string Action, string Etiket)[] Ekranlar { get; set; }
    }

    public static class UretimMenu
    {





        public static Dictionary<string, List<UretimDisEkran>> DisEkranlar(ApplicationDbContext ctx)
        {
            var sonuc = new Dictionary<string, List<UretimDisEkran>>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var satirlar = ctx.AppMenus
                    .Where(m => m.IsActive && m.Category.StartsWith("Uras Üretim") && !m.ControllerName.StartsWith("UrasUretim"))
                    .OrderBy(m => m.DisplayOrder).ThenBy(m => m.MenuTitle)
                    .Select(m => new { m.Category, m.ControllerName, m.ActionName, m.MenuTitle, m.Icon })
                    .ToList();
                foreach (var s in satirlar)
                {
                    string ek = s.Category.Contains(":") ? s.Category.Substring(s.Category.IndexOf(':') + 1).Trim() : "";
                    var mod = Moduller.FirstOrDefault(m => ek.StartsWith(m.Baslik, StringComparison.OrdinalIgnoreCase))
                              ?? Moduller.First(m => m.Key == "UrasUretimRapor");
                    if (!sonuc.TryGetValue(mod.Key, out var liste)) sonuc[mod.Key] = liste = new List<UretimDisEkran>();
                    liste.Add(new UretimDisEkran { Controller = s.ControllerName, Action = s.ActionName, Etiket = s.MenuTitle, Icon = s.Icon });
                }
            }
            catch { }
            return sonuc;
        }

        public static readonly UretimModul[] Moduller = new[]
        {
            new UretimModul {
                Key = "UrasUretimUretim", Baslik = "Üretim", Ikon = "fa-gears",
                Ekranlar = new (string, string)[] {
                    ("UretimSiparisi", "Üretim Siparişi"),
                    ("SiloDolum", "Silo Dolum"),
                    ("SahaUretim", "Saha Üretim"),
                    ("Numune", "Numune"),
                    ("SeriPartiTanim", "Seri / Parti Tanım"),
                    ("KalemAnaVerileri", "Kalem Ana Verileri"),
                    ("UrunAyristirma", "Ürün Ayrıştırma"),
                    ("UrunDonusumu", "Ürün Dönüşümü"),
                }
            },
            new UretimModul {
                Key = "UrasUretimStok", Baslik = "Stok", Ikon = "fa-boxes-stacked",
                Ekranlar = new (string, string)[] {
                    ("DepoStokRaporu", "Depo Stok Raporu"),
                    ("MalGirisi", "Mal Girişi"),
                    ("MalCikisi", "Mal Çıkışı"),
                    ("DepoNakli", "Depo Nakli"),
                    ("StokSayim", "Stok Sayım"),
                    ("SayimEksik", "Sayım Eksik"),
                    ("StokKayitListesi", "Stok Kayıt Listesi"),
                    ("EtiketYazdir", "Etiket Yazdır"),
                }
            },
            new UretimModul {
                Key = "UrasUretimKalite", Baslik = "Kalite", Ikon = "fa-clipboard-check",
                Ekranlar = new (string, string)[] {
                    ("KaliteKontrol", "Kalite Kontrol"),
                    ("GirisKK", "Giriş KK"),
                    ("SatinalmaKK", "Satınalma KK"),
                    ("UrunKaliteKontrol", "Ürün Kalite Kontrol"),
                    ("PlastikBazliKK", "Plastik Bazlı KK"),
                    ("SuBazliKK", "Su Bazlı KK"),
                    ("PHKalibrasyon", "pH Kalibrasyon"),
                    ("Yaslandirma", "Yaşlandırma"),
                    ("Tutanak", "Tutanak"),
                    ("MusteriSikayet", "Müşteri Şikayet"),
                }
            },
            new UretimModul {
                Key = "UrasUretimBakim", Baslik = "Bakım", Ikon = "fa-screwdriver-wrench",
                Ekranlar = new (string, string)[] {
                    ("Index", "Bakım Yönetimi"),
                }
            },
            new UretimModul {
                Key = "UrasUretimUrunAgaci", Baslik = "Ürün Ağacı", Ikon = "fa-sitemap",
                Ekranlar = new (string, string)[] {
                    ("Index", "Ürün Ağacı"),
                    ("ReceteMaliyet", "Reçete Maliyet"),
                }
            },
            new UretimModul {
                Key = "UrasUretimRapor", Baslik = "Raporlar", Ikon = "fa-chart-column",
                Ekranlar = new (string, string)[] {
                    ("Index", "Rapor Merkezi"),
                    ("StokYasi", "Parti Yaşlandırma"),
                    ("Silo", "Silo Raporu"),
                    ("HammaddeTuketim", "Hammadde Tüketim"),
                    ("LotSira", "Lot Sıra Kontrol"),
                    ("Uretim", "Üretim Raporu"),
                    ("Performans", "Performans"),
                    ("Sayim", "Sayım"),
                    ("Izlenebilirlik", "İzlenebilirlik"),
                }
            },
            new UretimModul {
                Key = "UrasUretimAyarlar", Baslik = "Ayarlar", Ikon = "fa-sliders",
                Ekranlar = new (string, string)[] {
                    ("Index", "Genel Ayarlar"),
                    ("MailAyarlari", "Mail Ayarları"),
                }
            },
        };
    }
}
