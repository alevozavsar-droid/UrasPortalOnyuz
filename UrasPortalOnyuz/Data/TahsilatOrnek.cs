// ============================================================
// TAHSİLAT (Müşteri tahsilatı) — Tahsilat ekranı (Controllers/TahsilatController.cs, Views/Rapor117/Index.cshtml, wwwroot/js/tahsilat.js)
//   • Mod: BAKİYEDEN (tutar en eski vadeli açık faturalardan başlayarak FIFO dağıtılır) ya da FATURA BAZLI (seçilen faturalara elle tutar yazılır)
//   • Her dağıtım satırı için OTOMATİK: gecikme günü (tahsilat tarihi − fatura vadesi), VADE FARKI (kalan × tahsilat kuru × aylık oran / 30 × gecikme)
//     ve KUR FARKI (dövizli faturada: tutar × (tahsilat kuru − fatura tarihindeki kur)). Fatura kuru örnek veride günümüz kurundan geriye doğru türetilir.
//   • Kayıt: Cari Mutabakat hareketlerine Tahsilat (X / R işlem tipiyle) eklenir, faturaların kalanı düşer; dağıtılamayan kısım bakiyeye avans olarak kalır.
//   • Basit fatura kesme: hesaplanan kur farkı / vade farkı için tek tıkla KDV'li fark faturası (KFF / VFF) — cari hesabına Fatura hareketi olarak işlenir.
// ============================================================
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Data
{
    public class TahsilatDagitim
    {
        public string BelgeNo { get; set; } public DateTime FaturaTarihi { get; set; } public DateTime Vade { get; set; } public string Pb { get; set; } public string IslemTipi { get; set; }
        public decimal Kalan { get; set; } public decimal Tutar { get; set; }           // fatura para biriminde
        public decimal FaturaKuru { get; set; } public decimal TahsilatKuru { get; set; }
        public int GecikmeGun { get; set; } public decimal VadeFarki { get; set; } public decimal KurFarki { get; set; }   // TL
        public decimal TutarTl => Math.Round(Tutar * TahsilatKuru, 2);
    }
    public class TahsilatKaydi
    {
        public string No { get; set; } public DateTime Tarih { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string Sirket { get; set; }
        public string Yontem { get; set; } public string ParaBirimi { get; set; } public decimal Tutar { get; set; } public decimal Kur { get; set; } public decimal TutarTl => Math.Round(Tutar * Kur, 2);
        public string IslemTipi { get; set; } = "X"; public string Mod { get; set; } = "Bakiye"; public string Aciklama { get; set; } = ""; public string BelgeNo { get; set; } = "";
        public List<TahsilatDagitim> Dagitim { get; set; } = new List<TahsilatDagitim>();
        public decimal Dagitilan => Math.Round(Dagitim.Sum(d => d.Tutar), 2); public decimal Avans => Math.Round(Tutar - Dagitilan, 2);
        public decimal KurFarki => Math.Round(Dagitim.Sum(d => d.KurFarki), 2); public decimal VadeFarki => Math.Round(Dagitim.Sum(d => d.VadeFarki), 2);
        public string KurFarkiFaturaNo { get; set; } = ""; public string VadeFarkiFaturaNo { get; set; } = "";
        public string Kullanici { get; set; } = ""; public DateTime Zaman { get; set; }
    }
    public class FarkFaturasi
    {
        public string No { get; set; } public DateTime Tarih { get; set; } public DateTime Vade { get; set; } public string CariKodu { get; set; } public string CariAd { get; set; } public string CariUnvan { get; set; } public string VergiNo { get; set; } public string VergiDairesi { get; set; }
        public string Tur { get; set; }   // Kur Farkı | Vade Farkı
        public string TahsilatNo { get; set; } public string IslemTipi { get; set; } = "X";
        public List<(string Aciklama, decimal Tutar)> Kalemler { get; set; } = new List<(string, decimal)>();
        public decimal Matrah => Math.Round(Kalemler.Sum(k => k.Tutar), 2); public decimal KdvOran { get; set; } = 20m; public decimal Kdv => Math.Round(Matrah * KdvOran / 100m, 2); public decimal Toplam => Math.Round(Matrah + Kdv, 2);
        public string Kullanici { get; set; } = ""; public DateTime Zaman { get; set; } public string Durum { get; set; } = "Kesildi";
    }

    public static class TahsilatOrnek
    {
        public static readonly CultureInfo Tr = CariKartOrnek.Tr; private static readonly object Kilit = new object();
        /// <summary>Vade farkı aylık oranı (%); günlük = oran / 30. Fark faturalarında KDV oranı (%).</summary>
        public static decimal VadeFarkiAylikOran = 3.5m; public static decimal KdvOran = 20m;
        /// <summary>Vade farkı uygulanmayan tolerans (gün): bu kadar gecikme için fark hesaplanmaz.</summary>
        public static int VadeToleransGun = 0;
        public static readonly string[] Yontemler = { "Havale / EFT", "Nakit", "Çek", "Kredi Kartı", "Senet" };
        public static readonly string[] IslemTipleri = { "X", "R" };
        public static Dictionary<string, decimal> Kurlar => MutabakatOrnek.Kurlar;
        private static readonly List<TahsilatKaydi> _tahsilatlar = new List<TahsilatKaydi>(); private static readonly List<FarkFaturasi> _faturalar = new List<FarkFaturasi>();
        private static int _sayac = 0, _kff = 0, _vff = 0; private static bool _tohumlandi;
        public static List<TahsilatKaydi> Tahsilatlar { get { Tohumla(); lock (Kilit) return _tahsilatlar.OrderByDescending(t => t.Zaman).ToList(); } }
        public static List<FarkFaturasi> Faturalar { get { Tohumla(); lock (Kilit) return _faturalar.OrderByDescending(f => f.Zaman).ToList(); } }
        public static TahsilatKaydi TahsilatBul(string no) { Tohumla(); lock (Kilit) return _tahsilatlar.FirstOrDefault(t => t.No == no); }
        public static FarkFaturasi FaturaBul(string no) { Tohumla(); lock (Kilit) return _faturalar.FirstOrDefault(t => t.No == no); }
        static decimal Kur(string pb) => Kurlar.TryGetValue(pb ?? "TL", out var k) ? k : 1m;
        /// <summary>Fatura tarihindeki kur (örnek): güncel kur, aylık ≈ %2,2 değer kaybı varsayımıyla geriye götürülür; TL faturada 1.</summary>
        public static decimal FaturaKuru(MtHareket f)
        {
            if (f.ParaBirimi == "TL" || f.ParaBirimi == "TRY") return 1m;
            int ay = Math.Max(0, (DateTime.Today.Year - f.Tarih.Year) * 12 + DateTime.Today.Month - f.Tarih.Month);
            return Math.Round(Kur(f.ParaBirimi) / (1 + 0.022m * ay), 4);
        }
        /// <summary>Carinin açık satış faturaları (vade sırasıyla).</summary>
        public static List<MtHareket> AcikFaturalar(string kod)
        {
            var c = MutabakatOrnek.Bul(kod); if (c == null) return new List<MtHareket>();
            return c.Hareketler.Where(h => h.BelgeTipi == "Fatura" && h.Borc > 0 && h.Kalan > 0.005m).OrderBy(h => h.Vade ?? h.Tarih).ThenBy(h => h.Tarih).ToList();
        }
        /// <summary>
        /// Dağıtım hesabı: mod "Bakiye" → tahsilat para birimindeki açık faturalara FIFO; "Fatura" → secim (belgeNo → tutar). Her satırda gecikme, vade farkı ve kur farkı otomatik.
        /// </summary>
        public static List<TahsilatDagitim> Hesapla(string kod, DateTime tarih, string pb, decimal tutar, decimal kur, string mod, Dictionary<string, decimal> secim)
        {
            var l = new List<TahsilatDagitim>(); var faturalar = AcikFaturalar(kod).Where(f => f.ParaBirimi == pb).ToList(); decimal kalanTutar = Math.Round(tutar, 2);
            foreach (var f in faturalar)
            {
                decimal al;
                if (mod == "Fatura") { if (secim == null || !secim.TryGetValue(f.BelgeNo, out var s) || s <= 0) continue; al = Math.Min(Math.Round(s, 2), f.Kalan); }
                else { if (kalanTutar <= 0) break; al = Math.Min(f.Kalan, kalanTutar); kalanTutar = Math.Round(kalanTutar - al, 2); }
                if (al <= 0) continue;
                var vade = (f.Vade ?? f.Tarih).Date; int gec = Math.Max(0, (tarih.Date - vade).Days - VadeToleransGun); decimal fk = FaturaKuru(f);
                l.Add(new TahsilatDagitim { BelgeNo = f.BelgeNo, FaturaTarihi = f.Tarih, Vade = vade, Pb = pb, IslemTipi = f.IslemTipi, Kalan = f.Kalan, Tutar = al, FaturaKuru = fk, TahsilatKuru = kur, GecikmeGun = gec,
                    VadeFarki = Math.Round(al * kur * VadeFarkiAylikOran / 100m / 30m * gec, 2), KurFarki = pb == "TL" ? 0 : Math.Round(al * (kur - fk), 2) });
            }
            return l;
        }
        /// <summary>Tahsilatı kaydeder: Cari Mutabakat'a hareket (Tahsilat / Çek / Senet), faturaların kalanı düşer. Hata metni ya da null.</summary>
        public static string Kaydet(string kod, DateTime tarih, string yontem, string islemTipi, string pb, decimal tutar, decimal kur, string mod, Dictionary<string, decimal> secim, string aciklama, string kullanici, out TahsilatKaydi kayit, DateTime? simdi = null)
        {
            kayit = null; Tohumla();
            var k = CariKartOrnek.KartBul(kod); if (k == null) return "Cari bulunamadı."; if (k.KartTipi != "Müşteri") return "Tahsilat yalnız müşteri carilerinde kaydedilir.";
            if (tutar <= 0) return "Tutar sıfırdan büyük olmalı."; if (kur <= 0) return "Kur sıfırdan büyük olmalı."; if (!Yontemler.Contains(yontem ?? "")) return "Tahsilat yöntemi seçin."; if (!IslemTipleri.Contains(islemTipi ?? "")) return "İşlem tipi X ya da R olmalı.";
            var dag = Hesapla(kod, tarih, pb, tutar, kur, mod, secim);
            if (mod == "Fatura" && dag.Count == 0) return "Fatura bazlı tahsilatta en az bir faturaya tutar yazın.";
            if (mod == "Fatura" && Math.Round(dag.Sum(d => d.Tutar), 2) > Math.Round(tutar, 2) + 0.005m) return $"Faturalara dağıtılan tutar ({dag.Sum(d => d.Tutar).ToString("N2", Tr)} {pb}) tahsilat tutarını ({tutar.ToString("N2", Tr)} {pb}) aşıyor.";
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var no = $"TH-{tarih:yyyy}-{++_sayac:00000}";
                var h = new MtHareket { Tarih = tarih.Date, ParaBirimi = pb, BelgeTipi = yontem == "Çek" ? "Çek" : yontem == "Senet" ? "Senet" : "Tahsilat", BelgeNo = no, Aciklama = $"{yontem} · {(mod == "Fatura" ? "fatura bazlı" : "bakiyeden")}{(dag.Count > 0 ? " · " + string.Join(", ", dag.Select(d => d.BelgeNo)) : "")}{(string.IsNullOrWhiteSpace(aciklama) ? "" : " · " + aciklama.Trim())}", Alacak = Math.Round(tutar, 2), KapamaDurumu = yontem == "Çek" || yontem == "Senet" ? "Vade Bekliyor" : "—", IslemTipi = islemTipi };
                if (yontem == "Çek" || yontem == "Senet") h.Vade = tarih.Date.AddDays(60);
                MutabakatOrnek.HareketEkle(kod, h, dag.Select(d => (d.BelgeNo, d.Tutar)));
                kayit = new TahsilatKaydi { No = no, Tarih = tarih.Date, CariKodu = k.Kod, CariAd = k.KisaAd, Sirket = k.Sirket, Yontem = yontem, ParaBirimi = pb, Tutar = Math.Round(tutar, 2), Kur = kur, IslemTipi = islemTipi, Mod = mod == "Fatura" ? "Fatura" : "Bakiye", Aciklama = (aciklama ?? "").Trim(), BelgeNo = no, Dagitim = dag, Kullanici = kullanici ?? "", Zaman = z };
                _tahsilatlar.Insert(0, kayit);
                CariKartOrnek.Gunluk(k, z, kullanici, "Tahsilat", "", $"{no} · {tutar.ToString("N2", Tr)} {pb} ({yontem}, {islemTipi}){(kayit.VadeFarki > 0 ? $" · vade farkı {kayit.VadeFarki.ToString("N0", Tr)} ₺" : "")}{(kayit.KurFarki != 0 ? $" · kur farkı {kayit.KurFarki.ToString("N0", Tr)} ₺" : "")}", "Tahsilat", kayit.Aciklama);
                return null;
            }
        }
        /// <summary>Kur farkı / vade farkı faturası keser (KDV'li, 30 gün vadeli) ve cari hesabına Fatura olarak işler. Hata metni ya da null.</summary>
        public static string FaturaKes(string tahsilatNo, string tur, string kullanici, out FarkFaturasi fatura, DateTime? simdi = null)
        {
            fatura = null; var t = TahsilatBul(tahsilatNo); if (t == null) return "Tahsilat bulunamadı.";
            bool kur = tur == "Kur Farkı"; if (!kur && tur != "Vade Farkı") return "Fatura türü Kur Farkı ya da Vade Farkı olmalı.";
            if (kur && !string.IsNullOrEmpty(t.KurFarkiFaturaNo)) return $"Kur farkı faturası zaten kesildi: {t.KurFarkiFaturaNo}."; if (!kur && !string.IsNullOrEmpty(t.VadeFarkiFaturaNo)) return $"Vade farkı faturası zaten kesildi: {t.VadeFarkiFaturaNo}.";
            var kalemler = t.Dagitim.Where(d => (kur ? d.KurFarki : d.VadeFarki) > 0.005m).Select(d => (kur ? $"{d.BelgeNo} kur farkı: {d.Tutar.ToString("N2", Tr)} {d.Pb} × ({d.TahsilatKuru.ToString("N4", Tr)} − {d.FaturaKuru.ToString("N4", Tr)})" : $"{d.BelgeNo} vade farkı: {d.GecikmeGun} gün gecikme × %{VadeFarkiAylikOran.ToString("0.##", Tr)} / ay", kur ? d.KurFarki : d.VadeFarki)).ToList();
            if (kalemler.Count == 0) return kur ? "Bu tahsilatta pozitif kur farkı yok." : "Bu tahsilatta vade farkı yok (gecikme yok).";
            var k = CariKartOrnek.KartBul(t.CariKodu);
            lock (Kilit)
            {
                var z = simdi ?? DateTime.Now; var no = kur ? $"KFF-{z:yyyy}-{++_kff:0000}" : $"VFF-{z:yyyy}-{++_vff:0000}";
                fatura = new FarkFaturasi { No = no, Tarih = z.Date, Vade = z.Date.AddDays(30), CariKodu = t.CariKodu, CariAd = t.CariAd, CariUnvan = k?.ResmiUnvan ?? t.CariAd, VergiNo = k?.VergiNo ?? "", VergiDairesi = k?.VergiDairesi ?? "", Tur = tur, TahsilatNo = t.No, IslemTipi = t.IslemTipi, Kalemler = kalemler, KdvOran = KdvOran, Kullanici = kullanici ?? "", Zaman = z };
                _faturalar.Insert(0, fatura); if (kur) t.KurFarkiFaturaNo = no; else t.VadeFarkiFaturaNo = no;
                MutabakatOrnek.HareketEkle(t.CariKodu, new MtHareket { Tarih = z.Date, ParaBirimi = "TL", BelgeTipi = "Fatura", BelgeNo = no, Aciklama = $"{tur} faturası · {t.No}", Borc = fatura.Toplam, Vade = fatura.Vade, KapamaDurumu = "Açık", Kalan = fatura.Toplam, IslemTipi = t.IslemTipi });
                if (k != null) CariKartOrnek.Gunluk(k, z, kullanici, tur + " Faturası", "", $"{no} · {fatura.Toplam.ToString("N2", Tr)} ₺ (KDV dahil) · tahsilat {t.No}", "Tahsilat");
                return null;
            }
        }
        /// <summary>Örnek kayıtlar: bakiyeden TL tahsilat, fatura bazlı USD tahsilat (kur farkı), gecikmeli tahsilat (vade farkı faturası kesilmiş).</summary>
        private static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true; var b = DateTime.Today;
                TahsilatKaydi k1, k2, k3;
                Kaydet("120.003", b.AddDays(-6), "Havale / EFT", "X", "TL", 850_000m, 1m, "Bakiye", null, "Haftalık ödeme planı", "Nazlı Erdem", out k1, b.AddDays(-6).AddHours(14));
                var usd = AcikFaturalar("120.001").Where(f => f.ParaBirimi == "USD").Take(2).ToList();
                if (usd.Count > 0) Kaydet("120.001", b.AddDays(-3), "Havale / EFT", "X", "USD", Math.Round(usd.Sum(f => f.Kalan), 2), Kurlar["USD"], "Fatura", usd.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => g.Sum(x => x.Kalan)), "USD faturalar kapatıldı", "Nazlı Erdem", out k2, b.AddDays(-3).AddHours(11));
                var gec = AcikFaturalar("120.011").Where(f => f.ParaBirimi == "TL" && (f.Vade ?? f.Tarih) < b.AddDays(-30)).Take(1).ToList();
                if (gec.Count > 0) { Kaydet("120.011", b.AddDays(-1), "Çek", "R", "TL", gec[0].Kalan, 1m, "Fatura", gec.GroupBy(f => f.BelgeNo).ToDictionary(g => g.Key, g => g.Sum(x => x.Kalan)), "Gecikmiş fatura çekle tahsil", "Nazlı Erdem", out k3, b.AddDays(-1).AddHours(16)); if (k3 != null && k3.VadeFarki > 0) FaturaKes(k3.No, "Vade Farkı", "Nazlı Erdem", out _, b.AddDays(-1).AddHours(16).AddMinutes(20)); }
            }
        }
    }
}
