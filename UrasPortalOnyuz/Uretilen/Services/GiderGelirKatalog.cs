// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using ClosedXML.Excel;

namespace WebApplication3.Services
{













    public static class GiderGelirKatalog
    {
        public static readonly string MerkezDbKey = "DefaultConnection";
        public static readonly string SatirUdf = "U_BE1_GIDER";   // belge satırına (PCH1/INV1/RIN1/RPC1/DRF1/JDT1) yazılan tam kod

        public class Kalem
        {
            public int Id { get; set; }
            public string Tip { get; set; }            // GIDER | GELIR | TIS
            public int AnaGrupNo { get; set; }
            public string AnaGrup { get; set; }
            public int AltGrupNo { get; set; }
            public string AltGrup { get; set; }
            public int KalemNo { get; set; }
            public string Kalem_ { get; set; }
            public string KokKod { get; set; }
            public string MahsupDurumu { get; set; }
            public string MahsupAnaGrup { get; set; }
            public string MahsupAltGrup { get; set; }
            public string MahsupKalem { get; set; }
            public string MahsupTuru { get; set; }
            public string MahsupNotu { get; set; }
            public string MahsupHedefKokKod { get; set; }
            public string KontrolDurumu { get; set; }
            public string Sinif { get; set; }
            public string EtkiAlani { get; set; }
            public string UygulamaTuru { get; set; }
            public string Raporlama { get; set; }
            public string Aciklama { get; set; }
            public bool Aktif { get; set; } = true;
        }

        public class Sirket { public string SirketKodu { get; set; } public string Ad { get; set; } public bool Aktif { get; set; } public int Sira { get; set; } }
        public class SirketDb { public string DbName { get; set; } public string SirketKodu { get; set; } public string DbKey { get; set; } }

        public class IceAktarSonuc
        {
            public int Gider, Gelir, Tis, Sirket, Guncellenen, Eklenen, Gecis, Silinen;
            public List<string> Uyarilar { get; } = new List<string>();
            public override string ToString()  {return default;
}
        }

        public class KodTalep
        {
            public int Id { get; set; }
            public string Tip { get; set; }
            public string AnaGrup { get; set; } public string AltGrup { get; set; } public string Kalem { get; set; }
            public string Aciklama { get; set; } public string HesapKodu { get; set; }
            public string Ekran { get; set; } public string SirketDb { get; set; }
            public string TalepEden { get; set; } public string TalepEdenAd { get; set; } public string TalepEdenMail { get; set; }
            public DateTime TalepTarihi { get; set; }
            public string Durum { get; set; }
            public string Onaylayan { get; set; } public DateTime? OnayTarihi { get; set; } public string OnayNotu { get; set; }
            public string OlusanKokKod { get; set; } public int? OlusanKalemId { get; set; }
        }

        public class SabitCari
        {
            public int Id { get; set; }
            public string SirketKodu { get; set; } public string SirketDb { get; set; }
            public string CardCode { get; set; } public string CardName { get; set; }
            public string Tip { get; set; } public string KokKod { get; set; } public string Aciklama { get; set; }
            public string Durum { get; set; }
            public string TalepEden { get; set; } public DateTime TalepTarihi { get; set; }
            public string Onaylayan { get; set; } public DateTime? OnayTarihi { get; set; } public string OnayNotu { get; set; }
            public bool Aktif { get; set; }
            public string KalemAdi { get; set; }   // listelerken katalogdan
        }

        public class SatirAtama
        {
            public int Id { get; set; }
            public string DbKey { get; set; } public string SirketDb { get; set; }
            public int TransId { get; set; } public int LineId { get; set; }
            public string BelgeTuru { get; set; } public string BelgeNo { get; set; } public string Tarih { get; set; } public string Cari { get; set; }
            public string HesapKodu { get; set; } public string HesapAdi { get; set; } public decimal? Tutar { get; set; } public string Kod { get; set; }
            public string AtananKullanici { get; set; } public string Atayan { get; set; } public DateTime AtamaTarihi { get; set; } public string AtamaNotu { get; set; }
            public string Durum { get; set; } public string OnayAciklama { get; set; } public DateTime? OnayTarihi { get; set; } public string OnayKod { get; set; }
            public string AtananAd { get; set; }
            public string AtayanAd { get; set; }
        }

        public class KodGecis
        {
            public int Id { get; set; }
            public string EskiKokKod { get; set; } public string YeniKokKod { get; set; }
            public string EskiAnaGrup { get; set; } public string EskiAltGrup { get; set; } public string Kalem { get; set; }
            public string YeniAnaGrup { get; set; } public string YeniAltGrup { get; set; } public string SirketEki { get; set; }
        }


        public static string Onek(string tip)  {return default;
}

        public static string TipFromKod(string kod)
 {return default;
}
        public static string KokKodUret(string tip, int a, int b, int k)  {return default;
}
        public static string TamKod(string kokKod, string sirketKodu)  {return default;
}

        public static string KokKodAl(string tamKod)
 {return default;
}

        public static bool KodZorunluMu(string hesapKodu)  {return default;
}





        private static readonly HashSet<string> _giderNitelikli6 = new HashSet<string>
        {
            "610","611","612","613","614","615","616","617","618","619",
            "620","621","622","623","624","625","626","627","628","629",
            "630","631","632","633","634","635","636","637","638","639",
            "653","654","655","656","657","658","659",
            "660","661","662","663","664","665","666","667","668","669",
            "680","681","682","683","684","685","686","687","688","689",
            "691"
        };








        public static readonly HashSet<string> KodIstenmeyenHesap3 = new HashSet<string> { "620", "621", "761", "771", "781" };



        public static string HesapTipi(string hesapKodu)
 {return default;
}


        private static readonly object _kilit = new object();
        private static bool _semaHazir;


        public static string Normalize(string s)
 {return default;
}
    }
}
