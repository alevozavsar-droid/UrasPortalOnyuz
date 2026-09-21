// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WebApplication3.Services
{



















    public class IhracatBilgileriModel
    {
        public bool Ihracat { get; set; }
        public string IhracatDosyaNo { get; set; }
        public string Incoterm { get; set; }
        public string TransportMode { get; set; }
        public string KapCinsi { get; set; }
        public string KapAdedi { get; set; }
        public string KapNo { get; set; }
        public string KapMarka { get; set; }
        public string OdemeSekli { get; set; }
        public string OdemeKanali { get; set; }
        public string Iban { get; set; }
    }

    public static class IhracatBilgileri
    {
        public class KodAd { public string Kod { get; set; } public string Ad { get; set; } }


        public static readonly List<KodAd> Incoterms = new List<KodAd>
        {
            new KodAd { Kod = "EXW", Ad = "EXW - İşyerinde teslim" },
            new KodAd { Kod = "FCA", Ad = "FCA - Taşıyıcıya teslim" },
            new KodAd { Kod = "FAS", Ad = "FAS - Gemi doğrultusunda teslim" },
            new KodAd { Kod = "FOB", Ad = "FOB - Gemide teslim" },
            new KodAd { Kod = "CFR", Ad = "CFR - Masraflar ve navlun ödenmiş" },
            new KodAd { Kod = "CIF", Ad = "CIF - Masraflar, sigorta ve navlun ödenmiş" },
            new KodAd { Kod = "CPT", Ad = "CPT - Taşıma ödenmiş" },
            new KodAd { Kod = "CIP", Ad = "CIP - Taşıma ve sigorta ödenmiş" },
            new KodAd { Kod = "DAP", Ad = "DAP - Belirlenen yerde teslim" },
            new KodAd { Kod = "DPU", Ad = "DPU - Boşaltılmış olarak teslim" },
            new KodAd { Kod = "DDP", Ad = "DDP - Gümrük vergileri ödenmiş teslim" }
        };


        public static readonly List<KodAd> TasimaSekilleri = new List<KodAd>
        {
            new KodAd { Kod = "1", Ad = "1 - Deniz taşımacılığı" },
            new KodAd { Kod = "2", Ad = "2 - Demiryolu taşımacılığı" },
            new KodAd { Kod = "3", Ad = "3 - Karayolu taşımacılığı" },
            new KodAd { Kod = "4", Ad = "4 - Hava taşımacılığı" },
            new KodAd { Kod = "5", Ad = "5 - Posta" },
            new KodAd { Kod = "6", Ad = "6 - Kombine taşımacılık" },
            new KodAd { Kod = "7", Ad = "7 - Sabit nakliyat" },
            new KodAd { Kod = "8", Ad = "8 - Ülke içi su taşımacılığı" },
            new KodAd { Kod = "9", Ad = "9 - Uygun olmayan taşıma şekli" }
        };


        public static readonly List<KodAd> KapCinsleri = new List<KodAd>
        {
            new KodAd { Kod = "PX", Ad = "PX - Palet" },
            new KodAd { Kod = "CT", Ad = "CT - Karton" },
            new KodAd { Kod = "BX", Ad = "BX - Kutu" },
            new KodAd { Kod = "PK", Ad = "PK - Paket" },
            new KodAd { Kod = "BG", Ad = "BG - Torba" },
            new KodAd { Kod = "SA", Ad = "SA - Çuval" },
            new KodAd { Kod = "43", Ad = "43 - Big bag (esnek konteyner)" },
            new KodAd { Kod = "DR", Ad = "DR - Fıçı / varil" },
            new KodAd { Kod = "JC", Ad = "JC - Bidon (dikdörtgen)" },
            new KodAd { Kod = "CA", Ad = "CA - Teneke" },
            new KodAd { Kod = "BE", Ad = "BE - Balya" },
            new KodAd { Kod = "RO", Ad = "RO - Rulo" },
            new KodAd { Kod = "CS", Ad = "CS - Kasa / sandık" },
            new KodAd { Kod = "CR", Ad = "CR - Kafes / kasa" },
            new KodAd { Kod = "CN", Ad = "CN - Konteyner" },
            new KodAd { Kod = "TK", Ad = "TK - Tank" },
            new KodAd { Kod = "IB", Ad = "IB - IBC (orta boy dökme kap)" },
            new KodAd { Kod = "NE", Ad = "NE - Ambalajsız / dökme" }
        };

        public class AlanTanimi { public string Tablo; public string Alias; public string Baslik; public int Boyut; }


        public static List<AlanTanimi> GerekliAlanlar()
 {return default;
}


        public static Dictionary<string, object> SlBaslikAlanlari(IhracatBilgileriModel m)
 {return default;
}


        public static string GtipNormalize(string gtip)
 {return default;
}




        public static string Dogrula(IhracatBilgileriModel m, IList<string> satirGtipleri, string sevkUlkesi, string faturaTipi, string muafKodu)
 {return default;
}





        public static void BaslikSqlYaz(string connectionString, string tablo, int docEntry, IhracatBilgileriModel m)
 {}


        public static void SatirGtipSqlYaz(string connectionString, string satirTablosu, int docEntry, IList<string> gtipler)
 {}





        public static List<string> KalemGtipGuncelle(string connectionString, IEnumerable<(string ItemCode, string Gtip)> satirlar)
 {return default;
}


        public static List<KodAd> KodListesi(string connectionString, string tablo)
 {return default;
}

        public class IhracatDosyasi
        {
            public string DosyaNo { get; set; }
            public string Tanim { get; set; }
            public string CardCode { get; set; }
            public string Ulke { get; set; }
            public string Incoterm { get; set; }
            public string OdemeSekli { get; set; }
            public string Kapali { get; set; }
        }


        public static List<IhracatDosyasi> Dosyalar(string connectionString, string cardCode)
 {return default;
}





        public static async Task<List<string>> EksikAlanlariAcAsync(string connectionString, string dbAdi, string slUrl, string kullanici, string sifre)
 {return default;
}
    }
}
