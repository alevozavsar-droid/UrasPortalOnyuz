// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WebApplication3.Services
{





    public class SapKimlikEksikException : Exception
    {
        public SapKimlikEksikException(string mesaj) : base(mesaj)  {}
    }




























    public static class SapKimlik
    {
        public class Bilgi
        {
            public string KullaniciAdi { get; set; }
            public string Sifre { get; set; }
            public string DbKey { get; set; }

            public bool KendiKullanicisi { get; set; }

            public string Kaynak { get; set; }
        }

        public class KayitliBilgi
        {
            public string UserCode { get; set; }
            public string DbKey { get; set; }
            public string SapKullanici { get; set; }
            public string SapSifre { get; set; }      // korunmus (sifreli) metin
            public bool KendiKullanicisi { get; set; }
            public DateTime? Guncelleme { get; set; }
        }

        private static readonly string ItemsOnEki = "__SapKimlik:";
        private static readonly string KorumaAmaci = "SapBilgi.Sifre.v1";
        public static readonly string EkranYolu = "/SapBilgi";

        public static string KullaniciAdi(ControllerBase c)  {return default;
}
        public static string Sifre(ControllerBase c)  {return default;
}
        public static string KullaniciAdi(ControllerBase c, string dbKey)  {return default;
}
        public static string Sifre(ControllerBase c, string dbKey)  {return default;
}


        public static string SeciliVeritabani(HttpContext http, IConfiguration cfg)
 {return default;
}






        public static Bilgi Coz(ControllerBase c, string dbKey)
 {return default;
}

        private static Bilgi Hesapla(HttpContext http, IConfiguration cfg, string dbKey)
 {return default;
}

        public static Bilgi Varsayilan(IConfiguration cfg, string dbKey, string neden = null)
 {return default;
}


        public static bool KendiHesabiHazir(IConfiguration cfg, IDataProtectionProvider koruyucu, string userCode, string dbKey)
 {return true;
}

        public static string SirketAdi(IConfiguration cfg, string dbKey)
 {return default;
}




        private static string BaglantiDizesi(IConfiguration cfg)  {return default;
}

        public static void TabloyuHazirla(IConfiguration cfg)
 {}


        public static KayitliBilgi Oku(IConfiguration cfg, string userCode, string dbKey)
 {return default;
}


        public static List<KayitliBilgi> OkuTumu(IConfiguration cfg, string userCode)
 {return default;
}


        public static void Kaydet(IConfiguration cfg, IDataProtectionProvider koruyucu, string userCode, string dbKey, string sapKullanici, string sifre, bool kendiKullanicisi)
 {}


        public static void Sil(IConfiguration cfg, string userCode, string dbKey = null)
 {}


        public static string SifreyiCoz(IDataProtectionProvider koruyucu, string korunmus)
 {return default;
}






        public static IReadOnlyList<string> VarsayilanHesapKullanabilenler(IConfiguration cfg)
 {return default;
}

        public static bool VarsayilanHesapKullanabilir(IConfiguration cfg, string userCode)
 {return true;
}

        public static bool EkranaErisebilir(IConfiguration cfg, string userCode)  {return default;
}








        private static readonly HashSet<string> SapYazanControllerlar = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Rapor4",
            "Rapor20",
            "Rapor21",
            "Rapor28",
            "Rapor65",
            "Rapor82",
            "Rapor83",
            "Rapor84",
            "Rapor87",
            "Rapor88",
            "Rapor94",
            "Rapor95",
            "Rapor97",
            "Rapor98",
            "Rapor99",
            "Rapor100",
            "Rapor101",
            "Rapor103",
            "Rapor105",
            "Rapor106",
            "Rapor107",
            "Rapor108",
            "Rapor109",
            "Rapor110",
            "Rapor111",
            "Rapor112",
            "Rapor113",
            "Rapor114",
            "Rapor115",
            "Rapor116",
            "Rapor158",
            "Rapor159",
            "Rapor160",
            "Rapor117",
            "Rapor118",
            "Rapor119",
            "Rapor120",
            "Rapor122",
            "Rapor123",
            "Rapor128",
            "Rapor129",
            "Rapor131",
            "Rapor134",
            "Rapor135",
            "Rapor136",
            "Rapor137",
            "Rapor139",
            "Rapor141",
            "Rapor143",
            "Rapor144",
            "Rapor145",
            "Rapor149",
            "Rapor173",
            "MobilSiparis"
        };

        public static bool SapYazanEkranMi(string controllerAdi)
 {return default;
}
    }








    public class SapKimlikEksikFiltresi : Microsoft.AspNetCore.Mvc.Filters.IExceptionFilter
    {
        public void OnException(Microsoft.AspNetCore.Mvc.Filters.ExceptionContext ctx)
 {}
    }
}
