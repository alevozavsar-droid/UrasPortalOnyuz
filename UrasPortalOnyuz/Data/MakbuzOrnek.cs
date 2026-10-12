using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FinansRaporlama.Controllers;

namespace WebApplication3.Data
{
    /// <summary>Kasa tediye makbuzu: onaylı nakit ödeme Kasa Nakit Akışı'ndan kesilir; satır ÖDENDİ olur, muhasebe fişi (Data/MuhasebeOrnek.cs) otomatik atılır, avans ise modülde teslim edilmiş sayılır.</summary>
    public class KasaMakbuzu
    {
        public int Id { get; set; } public string No { get; set; } public DateTime Tarih { get; set; } public string DbName { get; set; } public string Sirket { get; set; } public string Kasa { get; set; } = "";
        public int DocEntry { get; set; } public string Kurum { get; set; } = ""; public string FaturaSahibi { get; set; } = ""; public string BelgeTipi { get; set; } = ""; public string BelgeNo { get; set; } = ""; public string Aciklama { get; set; } = "";
        public decimal Tutar { get; set; } public string PB { get; set; } = "TRY"; public decimal TutarTL { get; set; } public string TeslimAlan { get; set; } = ""; public string Kasiyer { get; set; } = "";
        public string Onaylayan { get; set; } = ""; public DateTime? OnayTarihi { get; set; } public string AvansNo { get; set; } = ""; public string TalimatNo { get; set; } = ""; public string YevmiyeNo { get; set; } = ""; public string Durum { get; set; } = "Kesildi";
        public string TutarYazi => MakbuzOrnek.Yaziyla(Tutar, PB);
    }

    public static class MakbuzOrnek
    {
        public static readonly object Kilit = new object();
        static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");
        static readonly List<KasaMakbuzu> _makbuzlar = new List<KasaMakbuzu>();
        static int _sonId; static bool _tohumlandi;

        public static List<KasaMakbuzu> Listele(string dbName) { Tohumla(); lock (Kilit) return _makbuzlar.Where(m => string.IsNullOrEmpty(dbName) || m.DbName == dbName).OrderByDescending(m => m.Tarih).ToList(); }
        public static KasaMakbuzu Bul(string no) { Tohumla(); lock (Kilit) return _makbuzlar.FirstOrDefault(m => m.No == no); }

        /// <summary>Tohumdaki ödenmiş nakit satırlarının makbuzları (satırdaki MK-… numarasıyla) — yazdırılabilir olsun diye.</summary>
        static void Tohumla()
        {
            if (_tohumlandi) return;
            lock (Kilit)
            {
                if (_tohumlandi) return; _tohumlandi = true;
                foreach (var s in NakitAkisOrnek.Sirketler)
                    foreach (var m in NakitAkisOrnek.Odemeler(s.DbName).Where(x => x.U_OdemeYontemi == "Nakit" && x.U_Durum == "ÖDENDİ" && !string.IsNullOrEmpty(x.U_MakbuzNo)))
                    {
                        if (_makbuzlar.Any(x => x.No == m.U_MakbuzNo)) continue;
                        _makbuzlar.Add(new KasaMakbuzu { Id = ++_sonId, No = m.U_MakbuzNo, Tarih = (m.U_OdenmeTarihi ?? DateTime.Today).AddHours(14), DbName = s.DbName, Sirket = s.Ad, Kasa = m.U_Kasa, DocEntry = m.DocEntry, Kurum = m.U_Kurum, FaturaSahibi = m.U_FaturaSahibi, BelgeTipi = m.U_BagliBelgeTipi ?? "", BelgeNo = m.U_FaturaSiparisNo ?? "", Aciklama = m.U_UrunHizmet ?? "", Tutar = m.U_OdenenTutar ?? m.U_Tutar ?? 0, PB = m.U_ParaBirimi ?? "TRY", TutarTL = m.U_TutarTL ?? 0, TeslimAlan = m.U_TeslimAlan, Kasiyer = "Kerem Aksoy", Onaylayan = m.U_Onaylayan, OnayTarihi = m.U_OnayTarihi, AvansNo = m.U_AvansNo ?? "", TalimatNo = m.U_TalimatNo ?? "", YevmiyeNo = m.U_YevmiyeNo ?? "" });
                    }
            }
        }

        /// <summary>Makbuz kesme: satır Nakit ve onaylı olmalı (nakit onaycıları: NakitAkisOrnek.NakitOnaycilari); kesilince satır ÖDENDİ, fiş ve avans olayı NakitAkisOrnek.OdendiIsaretle ile.</summary>
        public static (bool ok, string mesaj, KasaMakbuzu makbuz) Olustur(string dbName, int docEntry, string teslimAlan, string kasa, string kullanici, string talimatNo = null)
        {
            Tohumla();
            var m = NakitAkisOrnek.Bul(dbName, docEntry); if (m == null) return (false, "Ödeme satırı bulunamadı.", null);
            if (m.U_OdemeYontemi != "Nakit") return (false, "Makbuz yalnız nakit ödeme için kesilir.", null);
            if (m.U_OnayDurumu != "Onaylandı") return (false, $"Nakit ödeme onaylanmadan makbuz kesilemez (onay: {string.Join(" / ", NakitAkisOrnek.NakitOnaycilari)}).", null);
            if (m.U_Durum == "ÖDENDİ" && !string.IsNullOrEmpty(m.U_MakbuzNo)) { var v = Bul(m.U_MakbuzNo); if (v != null) return (false, $"Bu satırın makbuzu zaten kesilmiş: {m.U_MakbuzNo}.", v); }
            KasaMakbuzu mk;
            lock (Kilit)
            {
                mk = new KasaMakbuzu { Id = ++_sonId, No = $"KTM-{DateTime.Today.Year}-{_sonId:0000}", Tarih = DateTime.Now, DbName = dbName, Sirket = NakitAkisOrnek.SirketAdi(dbName), Kasa = string.IsNullOrEmpty(kasa) ? (string.IsNullOrEmpty(m.U_Kasa) ? NakitAkisOrnek.Kasalar[0] : m.U_Kasa) : kasa, DocEntry = m.DocEntry, Kurum = m.U_Kurum, FaturaSahibi = m.U_FaturaSahibi, BelgeTipi = m.U_BagliBelgeTipi ?? "", BelgeNo = m.U_FaturaSiparisNo ?? "", Aciklama = m.U_UrunHizmet ?? "", Tutar = m.U_Tutar ?? 0, PB = m.U_ParaBirimi ?? "TRY", TutarTL = m.U_TutarTL ?? 0, TeslimAlan = string.IsNullOrEmpty(teslimAlan) ? (string.IsNullOrEmpty(m.U_TeslimAlan) ? m.U_Kurum : m.U_TeslimAlan) : teslimAlan, Kasiyer = kullanici ?? "", Onaylayan = m.U_Onaylayan, OnayTarihi = m.U_OnayTarihi, AvansNo = m.U_AvansNo ?? "", TalimatNo = talimatNo ?? m.U_TalimatNo ?? "" };
                _makbuzlar.Add(mk);
            }
            NakitAkisOrnek.MakbuzIsle(dbName, docEntry, mk.No, mk.Kasa, mk.TeslimAlan, kullanici);
            var m2 = NakitAkisOrnek.Bul(dbName, docEntry); mk.YevmiyeNo = m2?.U_YevmiyeNo ?? "";
            return (true, $"{mk.No} kesildi: {mk.Kurum} · {mk.Tutar.ToString("N2", Tr)} {mk.PB} · {mk.Kasa} · teslim alan {mk.TeslimAlan}" + (mk.YevmiyeNo != "" ? $" · muhasebe fişi {mk.YevmiyeNo}" : "") + (mk.AvansNo != "" ? $" · avans {mk.AvansNo} teslim edildi" : "") + ".", mk);
        }

        public static object Json(KasaMakbuzu k) => k == null ? null : new { no = k.No, tarih = k.Tarih.ToString("yyyy-MM-dd HH:mm"), dbName = k.DbName, sirket = k.Sirket, kasa = k.Kasa, docEntry = k.DocEntry, kurum = k.Kurum, faturaSahibi = k.FaturaSahibi, belgeTipi = k.BelgeTipi, belgeNo = k.BelgeNo, aciklama = k.Aciklama, tutar = k.Tutar, pb = k.PB, tutarTL = k.TutarTL, tutarYazi = k.TutarYazi, teslimAlan = k.TeslimAlan, kasiyer = k.Kasiyer, onaylayan = k.Onaylayan, onayTarihi = k.OnayTarihi?.ToString("yyyy-MM-dd HH:mm"), avansNo = k.AvansNo, talimatNo = k.TalimatNo, yevmiyeNo = k.YevmiyeNo, durum = k.Durum, link = "/Rapor79/Makbuz?no=" + Uri.EscapeDataString(k.No) };

        // ---------- tutarın yazıyla gösterimi (makbuz) ----------
        static readonly string[] Birler = { "", "Bir", "İki", "Üç", "Dört", "Beş", "Altı", "Yedi", "Sekiz", "Dokuz" };
        static readonly string[] Onlar = { "", "On", "Yirmi", "Otuz", "Kırk", "Elli", "Altmış", "Yetmiş", "Seksen", "Doksan" };
        static string UcHane(int n)
        {
            string s = ""; int y = n / 100, o = n % 100 / 10, b = n % 10;
            if (y > 0) s += (y == 1 ? "" : Birler[y]) + "Yüz";
            if (o > 0) s += Onlar[o]; if (b > 0) s += Birler[b]; return s;
        }
        public static string Yaziyla(decimal tutar, string pb)
        {
            long tam = (long)Math.Floor(Math.Abs(tutar)); int kurus = (int)Math.Round((Math.Abs(tutar) - tam) * 100);
            string[] basamak = { "", "Bin", "Milyon", "Milyar" }; string s = ""; int i = 0;
            if (tam == 0) s = "Sıfır";
            while (tam > 0 && i < basamak.Length) { int h = (int)(tam % 1000); if (h > 0) s = (h == 1 && i == 1 ? "" : UcHane(h)) + basamak[i] + s; tam /= 1000; i++; }
            string ana = pb == "TRY" || string.IsNullOrEmpty(pb) ? "TürkLirası" : pb, alt = pb == "TRY" || string.IsNullOrEmpty(pb) ? "Kuruş" : "Cent";
            return $"Yalnız {s}{ana}" + (kurus > 0 ? $" {UcHane(kurus)}{alt}" : "") + ".";
        }
    }
}
