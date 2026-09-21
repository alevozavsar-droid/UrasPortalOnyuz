using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Controllers;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    /// <summary>
    /// ÖN YÜZ ÖRNEĞİ — tüm ekranların kullandığı sahte veriler. Veritabanı yok; burayı değiştirerek örnekleri çoğaltabilirsin.
    /// </summary>
    public static class OrnekVeri
    {
        // ---------------- kullanıcılar ----------------
        public static readonly List<(string Kod, string Ad, string Yetki)> Kullanicilar = new List<(string, string, string)>
        {
            ("it02", "Ali Veli", "A"), ("fns2", "Kerem Aksoy", "FN"), ("muh36", "Nazlı Erdem", "MUH"), ("sat5", "Tolga Yaman", "ST"),
            ("sat8", "Gizem Tan", "ST"), ("muh58", "Onur Bal", "MUH"), ("ykb", "Sevgi Ay", "B"), ("muh19", "Hülya Er", "MUH"),
            ("muh45", "Canan Su", "MUH"), ("dns", "Esra Kaya", "T"), ("isg1", "Mert Doğan", "UR"), ("fns1", "Pınar Ak", "FN")
        };
        public static string KullaniciAdi(string kod) => Kullanicilar.FirstOrDefault(k => string.Equals(k.Kod, kod, StringComparison.OrdinalIgnoreCase)).Ad ?? kod;

        // ---------------- şirketler ----------------
        public static readonly List<AppDatabase> Sirketler = new List<AppDatabase>
        {
            new AppDatabase { Id = 1, DbKey = "DefaultConnection",   Display = "Uras Kimya" },
            new AppDatabase { Id = 2, DbKey = "DefaultConnection5",  Display = "Selvi" },
            new AppDatabase { Id = 3, DbKey = "DefaultConnection2",  Display = "Avrupa Paper" },
            new AppDatabase { Id = 4, DbKey = "DefaultConnection3",  Display = "Alv Kimya" },
            new AppDatabase { Id = 5, DbKey = "DefaultConnection4",  Display = "Daf Kimya" },
            new AppDatabase { Id = 6, DbKey = "DefaultConnection25", Display = "Uras Kimya A.Ş." },
            new AppDatabase { Id = 7, DbKey = "DefaultConnection30", Display = "Uras Power" },
            new AppDatabase { Id = 8, DbKey = "DefaultConnection10", Display = "Uras Holding" },
        };
        public static string SirketAdi(string dbKey) => Sirketler.FirstOrDefault(s => s.DbKey == dbKey)?.Display ?? Sirketler[0].Display;

        // ---------------- roller ----------------
        public static readonly List<AppRole> Roller = new List<AppRole>
        {
            new AppRole { Id = 1, RoleCode = "A",   RoleName = "Yönetici" }, new AppRole { Id = 2, RoleCode = "FN", RoleName = "Finans" },
            new AppRole { Id = 3, RoleCode = "MUH", RoleName = "Muhasebe" }, new AppRole { Id = 4, RoleCode = "ST", RoleName = "Satış" },
            new AppRole { Id = 5, RoleCode = "T",   RoleName = "Tahsilat" }, new AppRole { Id = 6, RoleCode = "UR", RoleName = "Üretim" },
            new AppRole { Id = 7, RoleCode = "B",   RoleName = "Bakış (salt okunur)" },
        };

        // ---------------- menü başlıkları ve ekranlar ----------------

        // Tum ekranlar: Data/MenuKatalogu.cs (ana projedeki menu tohumundan uretildi)
        public static readonly List<AppMenu> Menuler = MenuKatalogu.Olustur();

        private static readonly Dictionary<string, (string Ikon, string UstGrup)> _baslikBilgi = new Dictionary<string, (string, string)>
        {
            ["Muhasebe"] = ("fas fa-scale-balanced", "Finans & Muhasebe"), ["Finans"] = ("fas fa-vault", "Finans & Muhasebe"), ["Tahsilat"] = ("fas fa-hand-holding-dollar", "Finans & Muhasebe"), ["Yapılan Ödemeler"] = ("fas fa-money-bill-transfer", "Finans & Muhasebe"),
            ["Satış"] = ("fas fa-chart-line", "Satış & Cari"), ["Sipariş"] = ("fas fa-cart-shopping", "Satış & Cari"), ["Cari İşler"] = ("fas fa-handshake", "Satış & Cari"), ["İhracat"] = ("fas fa-ship", "Satış & Cari"), ["E-Belge"] = ("fas fa-file-invoice", "Satış & Cari"),
            ["Satınalma"] = ("fas fa-truck-field", "Satınalma & İthalat"), ["İthalat"] = ("fas fa-plane-arrival", "Satınalma & İthalat"),
            ["Üretim"] = ("fas fa-industry", "Üretim & Stok"), ["Stok"] = ("fas fa-boxes-stacked", "Üretim & Stok"), ["Uras Üretim"] = ("fas fa-flask", "Üretim & Stok"),
            ["Aktarım"] = ("fas fa-right-left", "Sistem & Diğer"), ["Bilgi İşlem"] = ("fas fa-server", "Sistem & Diğer"), ["Özel Raporlar"] = ("fas fa-star", "Sistem & Diğer"), ["Danışman"] = ("fas fa-user-tie", "Sistem & Diğer"), ["Diğer Ekranlar"] = ("fas fa-toolbox", "Sistem & Diğer"),
        };
        public static readonly List<AppMenuCategory> Basliklar = Menuler.Select(m => m.Category).Distinct().Select((ad, i) => new AppMenuCategory { Id = i + 1, Name = ad, Icon = _baslikBilgi.TryGetValue(ad, out var b) ? b.Ikon : "fas fa-folder", UstGrup = _baslikBilgi.TryGetValue(ad, out var b2) ? b2.UstGrup : "Sistem & Diğer", DisplayOrder = i + 1, IsActive = true }).ToList();

        public static int MenuId(string controller) => Menuler.FirstOrDefault(m => m.ControllerName == controller)?.Id ?? 0;
        public static readonly List<UserFavoriteMenu> Favoriler = new List<UserFavoriteMenu> { new UserFavoriteMenu { Id = 1, UserCode = "it02", MenuId = MenuId("Rapor20") }, new UserFavoriteMenu { Id = 2, UserCode = "it02", MenuId = MenuId("Rapor109") }, new UserFavoriteMenu { Id = 3, UserCode = "it02", MenuId = MenuId("Rapor145") } };

        // ---------------- cariler / kalemler ----------------
        public static readonly List<CariViewModel> Cariler = new List<CariViewModel>
        {
            new CariViewModel { CardCode = "M0001", CardName = "AKSA TEKSTİL SAN. VE TİC. A.Ş.", Address = "Organize Sanayi Bölgesi 3. Cad. No:12 Bursa", Phone = "0224 000 00 01" },
            new CariViewModel { CardCode = "M0002", CardName = "DENİZ BOYA KİMYA LTD. ŞTİ.",      Address = "İkitelli OSB Aykosan San. Sit. İstanbul", Phone = "0212 000 00 02" },
            new CariViewModel { CardCode = "M0003", CardName = "EGE MAKROSEL AMBALAJ A.Ş.",       Address = "Atatürk OSB 10001 Sk. No:5 İzmir",        Phone = "0232 000 00 03" },
            new CariViewModel { CardCode = "M0004", CardName = "KARADENİZ EMPRİME SAN. LTD.",     Address = "Çerkezköy OSB Tekirdağ",                    Phone = "0282 000 00 04" },
            new CariViewModel { CardCode = "M0005", CardName = "URAS HOLDİNG A.Ş.",               Address = "Merkez Mah. Sanayi Cad. No:1 İstanbul",     Phone = "0212 000 00 05" },
            new CariViewModel { CardCode = "T0001", CardName = "BASF TÜRK KİMYA SAN. LTD. ŞTİ.",  Address = "Dilovası OSB Kocaeli",                      Phone = "0262 000 00 06" },
            new CariViewModel { CardCode = "T0002", CardName = "PETKİM PETROKİMYA HOLDİNG A.Ş.",  Address = "Aliağa İzmir",                              Phone = "0232 000 00 07" },
            new CariViewModel { CardCode = "T0003", CardName = "ANADOLU LOJİSTİK A.Ş.",           Address = "Hadımköy İstanbul",                         Phone = "0212 000 00 08" },
        };

        public static readonly List<(string Kod, string Ad, string Birim, decimal Fiyat)> Kalemler = new List<(string, string, string, decimal)>
        {
            ("UR02.S0.W20.40", "S 20 WHITE (40 Kg) ZDHC LEVEL 3", "Adet", 5956.16m), ("UR02.S0.C10.30", "S 10 CLEAR (30 Kg) ZDHC LEVEL 3", "Adet", 4185.41m),
            ("UR03.D1.MC0.20", "DM 10 CLEAR (20 Kg) ZDHC LEVEL 3", "Adet", 2834.99m), ("UR01.GP.000.30", "GLITTER BASE (30 Kg) ZDHC LEVEL 3", "Adet", 5674.45m),
            ("T07.JTM.CYA.01", "CYAN MASTER (1 KG) ZDHC LEVEL 3", "Adet", 491.87m), ("T07.JTM.MAG.01", "MAGENTA MASTER (1 KG) ZDHC LEVEL 3", "Adet", 491.87m),
            ("T07.JTE.BLC.01", "BLACK EXTRA (1 KG) ZDHC LEVEL 3", "Adet", 581.31m), ("UB02.RB.FLO.05", "FLUOR RUBIN (5 Kg) ZDHC LEVEL 3", "Adet", 1730.51m),
            ("UR02.VT.PV2.30", "PV 2000 FOIL ADHESIVE (30 Kg)", "Adet", 10061.08m), ("URB01.FX.000.01", "A 25 FIXATOR (1 Kg)", "Adet", 1022.65m),
            ("T07.DTF.TPW.00", "DTF TRANSFER POWDER WHITE", "Kg", 335.37m), ("UR11.DB.PIG.01", "DTF-TEXTILE INK BLACK (1 KG)", "Adet", 581.31m),
        };

        // ---------------- cari ekstre satırları ----------------
        public static List<Rapor20CariEkstreViewModel> CariEkstre(string kod, DateTime bas, DateTime bit)
        {
            var cari = Cariler.FirstOrDefault(c => c.CardCode == kod) ?? Cariler[0];
            var rnd = new Random(kod.GetHashCode());
            var liste = new List<Rapor20CariEkstreViewModel>();
            decimal bakiye = 0;
            liste.Add(new Rapor20CariEkstreViewModel { IslemNo = 0, SatirNo = 0, KayitTarihi = bas, VadeTarihi = bas, MuhatapKodu = cari.CardCode, MuhatapAdi = cari.CardName, Aciklama = "Devir Bakiyesi", IslemTipi = "Devir", AktarimTipi = "", TRYB = 125340.50m, TRYA = 0, IslemB = 125340.50m, IslemA = 0, IslemPB = "TRY", Sirket = "Uras Kimya", BakiyeDurumu = "Açık" });
            bakiye = 125340.50m; liste[0].TRY_KmlBky = bakiye; liste[0].Islem_KmlBky = bakiye;
            var tarih = bas; int no = 17000;
            while (tarih <= bit && liste.Count < 60)
            {
                tarih = tarih.AddDays(rnd.Next(2, 9)); if (tarih > bit) break;
                bool fatura = rnd.Next(10) < 6;
                decimal tutar = Math.Round((decimal)(rnd.Next(8, 260) * 1000 + rnd.Next(0, 99)) + rnd.Next(0, 99) / 100m, 2);
                var s = new Rapor20CariEkstreViewModel
                {
                    IslemNo = ++no, SatirNo = 1, KayitTarihi = tarih, VadeTarihi = tarih.AddDays(fatura ? 60 : 0),
                    MuhatapKodu = cari.CardCode, MuhatapAdi = cari.CardName, Phone1 = cari.Phone, Street = cari.Address,
                    Aciklama = fatura ? $"Satış Faturası - Belge No: {no} · URS2026{no:0000000}" : (rnd.Next(2) == 0 ? $"Tahsilat - Belge No: {no} (Havale/EFT · Garanti BBVA)" : $"Tahsilat - Belge No: {no} (Çek No: {rnd.Next(1000000, 9999999)} · Vade: {tarih.AddDays(90):dd.MM.yyyy})"),
                    IslemTipi = fatura ? "Fatura" : "Tahsilat", AktarimTipi = fatura ? "X" : "R", IslemPB = "TRY", Sirket = "Uras Kimya",
                    TRYB = fatura ? tutar : 0, TRYA = fatura ? 0 : tutar, IslemB = fatura ? tutar : 0, IslemA = fatura ? 0 : tutar, CheckNum = fatura ? null : (rnd.Next(2) == 0 ? rnd.Next(1000000, 9999999).ToString() : null)
                };
                bakiye += (s.TRYB ?? 0) - (s.TRYA ?? 0); s.TRY_KmlBky = bakiye; s.Islem_KmlBky = bakiye; s.BakiyeDurumu = fatura && rnd.Next(3) == 0 ? "Kapalı" : "Açık";
                liste.Add(s);
            }
            return liste;
        }

        // ---------------- hesap planı (mizan) ----------------
        public static readonly List<HesapViewModel> HesapPlani = new List<HesapViewModel>
        {
            H("1", "DÖNEN VARLIKLAR", 1, ""), H("100", "KASA", 2, "1"), H("100.01", "TL Kasası", 3, "100"), H("100.01.001", "Merkez Kasa", 4, "100.01", "Y"),
            H("102", "BANKALAR", 2, "1"), H("102.01", "Vadesiz TL", 3, "102"), H("102.01.001", "Garanti BBVA 6300-1234", 4, "102.01", "Y"), H("102.01.002", "İş Bankası 1001-5678", 4, "102.01", "Y"),
            H("120", "ALICILAR", 2, "1"), H("120.01", "Yurt İçi Alıcılar", 3, "120"), H("120.01.001", "Müşteriler", 4, "120.01", "Y"),
            H("153", "TİCARİ MALLAR", 2, "1"), H("153.01", "Mamul Stokları", 3, "153"), H("153.01.001", "Boya ve Kimyasallar", 4, "153.01", "Y"),
            H("191", "İNDİRİLECEK KDV", 2, "1"), H("191.01", "İndirilecek KDV", 3, "191"), H("191.01.001", "%20 KDV", 4, "191.01", "Y"),
            H("3", "KISA VADELİ YABANCI KAYNAKLAR", 1, ""), H("320", "SATICILAR", 2, "3"), H("320.01", "Yurt İçi Satıcılar", 3, "320"), H("320.01.001", "Tedarikçiler", 4, "320.01", "Y"),
            H("391", "HESAPLANAN KDV", 2, "3"), H("391.01", "Hesaplanan KDV", 3, "391"), H("391.01.001", "%20 KDV", 4, "391.01", "Y"),
            H("6", "GELİR TABLOSU HESAPLARI", 1, ""), H("600", "YURT İÇİ SATIŞLAR", 2, "6"), H("600.01", "Mamul Satışları", 3, "600"), H("600.01.001", "Boya Satışları", 4, "600.01", "Y"),
            H("7", "MALİYET HESAPLARI", 1, ""), H("770", "GENEL YÖNETİM GİDERLERİ", 2, "7"), H("770.01", "Personel Giderleri", 3, "770"), H("770.01.001", "Ücretler", 4, "770.01", "Y"),
        };
        private static HesapViewModel H(string kod, string ad, int seviye, string ust, string postable = "N") => new HesapViewModel { HesapKodu = kod, HesapAdi = ad, Level = seviye, Levels = seviye, ParentKodu = ust, Postable = postable };

        public static AktarimRaporuViewModel Mizan()
        {
            var rnd = new Random(28);
            var m = new AktarimRaporuViewModel { HesapHareketleri = new List<HesapOzeti>(), DovizliHesapHareketleri = new List<HesapOzetiDovizli>() };
            foreach (var h in HesapPlani)
            {
                decimal borc = Math.Round((decimal)rnd.Next(50, 900) * 1000 + rnd.Next(0, 99), 2), alacak = Math.Round((decimal)rnd.Next(50, 900) * 1000 + rnd.Next(0, 99), 2);
                if (h.HesapKodu.StartsWith("6")) borc = Math.Round(alacak * 0.1m, 2); if (h.HesapKodu.StartsWith("7")) alacak = Math.Round(borc * 0.05m, 2);
                m.HesapHareketleri.Add(new HesapOzeti { HesapKodu = h.HesapKodu, HesapAdi = h.HesapAdi, ToplamBorc = borc, ToplamAlacak = alacak, ToplamBakiye = borc - alacak, DistinctBy = h.Level });
            }
            foreach (var pb in new[] { "USD", "EUR" })
                m.DovizliHesapHareketleri.Add(new HesapOzetiDovizli { HesapKodu = "102.02.00" + (pb == "USD" ? "1" : "2"), HesapAdi = $"Garanti BBVA {pb} Hesabı", DovizCinsi = pb, TLBorc = 2450000, TLAlacak = 1830000, TLBakiye = 620000, DovizBorc = pb == "USD" ? 61250 : 56300, DovizAlacak = pb == "USD" ? 45750 : 42100, DovizBakiye = pb == "USD" ? 15500 : 14200, DistinctBy = "4" });
            return m;
        }

        // ---------------- mesajlar (bellek içi) ----------------
        public class Mesaj { public int Id; public string Gonderen; public string Alici; public string Metin; public DateTime Tarih; public DateTime? Okundu; public List<MesajEk> Ekler = new List<MesajEk>(); }
        public class MesajEk { public int Id; public string Ad; public string Tur; public long Boyut; public byte[] Icerik; }
        public static readonly List<Mesaj> Mesajlar = new List<Mesaj>
        {
            new Mesaj { Id = 1, Gonderen = "fns2", Alici = "it02", Metin = "Kasa nakit akış raporunda dünkü tahsilatlar görünmüyor, bakabilir misin?", Tarih = DateTime.Today.AddDays(-1).AddHours(14), Okundu = DateTime.Today.AddDays(-1).AddHours(15) },
            new Mesaj { Id = 2, Gonderen = "it02", Alici = "fns2", Metin = "Baktım, aktarım tipi filtresi X seçiliydi. Hepsi seçince geliyor 👍", Tarih = DateTime.Today.AddDays(-1).AddHours(15), Okundu = DateTime.Today.AddDays(-1).AddHours(15) },
            new Mesaj { Id = 3, Gonderen = "muh36", Alici = "it02", Metin = "Selvi için Ağustos irsaliyelerinin karşılaştırmasını atabilir misin?", Tarih = DateTime.Today.AddHours(9), Okundu = null },
            new Mesaj { Id = 4, Gonderen = "sat5", Alici = "it02", Metin = "AKSA'nın ekstresini WhatsApp'tan aldım, teşekkürler 🙏", Tarih = DateTime.Today.AddHours(10), Okundu = null },
        };
        public static int SonMesajId() => Mesajlar.Count == 0 ? 0 : Mesajlar.Max(m => m.Id);
        public static int SonEkId() => Mesajlar.SelectMany(m => m.Ekler).Select(e => e.Id).DefaultIfEmpty(0).Max();

        // ---------------- aktarım denetimi (Rapor145) ----------------
        public static List<object> AktarimBelgeleri(string objType)
        {
            var rnd = new Random(objType == "13" ? 13 : 15);
            var liste = new List<object>();
            var bas = new DateTime(2026, 4, 14); int docNum = objType == "13" ? 17300 : 800, hedefNo = objType == "13" ? 4200 : 1400;
            for (int i = 0; i < 90; i++)
            {
                var t = bas.AddDays(rnd.Next(0, 150)); docNum++; hedefNo++;
                decimal tutar = Math.Round((decimal)rnd.Next(5, 900) * 1000 + rnd.Next(0, 99), 2);
                int durum = rnd.Next(100);   // 78 eşleşti, 10 hedefte yok, 12 fark
                bool hedefVar = durum >= 10; bool fark = durum >= 88;
                liste.Add(new Dictionary<string, object>
                {
                    ["DocEntry"] = docNum, ["DocNum"] = docNum, ["DocDate"] = t, ["DocTotal"] = tutar, ["DocCur"] = "TRY", ["NumAtCard"] = objType == "13" ? $"URS2026{docNum:0000000}" : $"IUR2026{docNum:0000000}",
                    ["HedefDocEntry"] = hedefVar ? (object)hedefNo : null, ["HedefDocNum"] = hedefVar ? (object)hedefNo : null, ["HedefTarih"] = hedefVar ? (object)t.AddDays(rnd.Next(0, 3)) : null,
                    ["HedefTutar"] = hedefVar ? (object)(fark ? tutar - Math.Round(tutar * 0.0177m, 2) : tutar) : null, ["HedefPb"] = hedefVar ? "TRY" : null, ["HedefCari"] = hedefVar ? "T0223" : null,
                    ["EslesmeYolu"] = hedefVar ? (rnd.Next(4) == 0 ? "belgeno" : "entry") : "", ["KalemFark"] = fark ? rnd.Next(1, 3) : 0, ["KalemEksik"] = fark && rnd.Next(2) == 0 ? 1 : 0, ["KalemFazla"] = 0
                });
            }
            return liste.OrderBy(o => (DateTime)((Dictionary<string, object>)o)["DocDate"]).ToList();
        }
    }
}
