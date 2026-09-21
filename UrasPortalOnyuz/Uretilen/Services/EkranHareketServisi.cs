// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Services
{









    public static class EkranHareketServisi
    {
        private static int _semaHazir;
        private const int VarsayilanGeriyeGun = 7;      // ekranı hiç açmamış kullanıcı için pencere
        private const int EnFazlaGeriyeGun = 60;        // eski hareketler rozete girmez

        private static string Baglanti(IConfiguration cfg)  {return default;
}


        public static void Kaydet(IConfiguration cfg, string controller, string action, string kullanici, string aciklama, string sirketDb = null)
 {}


        public static void GorulduIsaretle(IConfiguration cfg, string kullanici, string controller)
 {}

        public class Rozet { public string Controller { get; set; } public int Adet { get; set; } public string SonKullanici { get; set; } public DateTime? SonTarih { get; set; } public string SonAciklama { get; set; } }


        public static List<Rozet> Rozetler(IConfiguration cfg, string kullanici)
 {return default;
}


        public static List<dynamic> SonHareketler(IConfiguration cfg, string controller, int adet = 30)
 {return default;
}

        private static string Kirp(string s, int n)  {return default;
}
    }
}
