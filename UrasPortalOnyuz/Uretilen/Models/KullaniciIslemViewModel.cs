using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{







    public class KullaniciIslemViewModel
    {

        public string BelgeAnaVeriID { get; set; }
        public string OlusturanKullanici { get; set; }
        public DateTime Tarih { get; set; }
        public string BelgeTipi { get; set; }
        public string SirketAdi { get; set; }
    }








    public class UserViewModel
    {
        public string UserCode { get; set; }
        public string UserName { get; set; }
    }




    public class CompanyLoginStats
    {
        public string SirketAdi { get; set; }
        public DateTime? LastLoginDate { get; set; }

        public int LoginCount { get; set; } = 0;
        public decimal? DailyFrequency { get; set; } // Günlük Ortalama Giriş Sıklığı
        public int DistinctDays { get; set; }
    }




    public class UserActivityStats
    {
        public int TotalLoginCount { get; set; } = 0;
        public List<CompanyLoginStats> CompanyLoginDetails { get; set; } = new List<CompanyLoginStats>();
    }













    public class KullaniciAktiviteHamViewModel
    {

        public string Tur { get; set; } // Login veya Logoff
        public string KullaniciAdi { get; set; }
        public DateTime Tarih { get; set; } // Sadece tarih kısmı
        public string Saat { get; set; } // HH:MM formatında dönüştürülmüş saat
        public string BilgisayarAdi { get; set; }
        public string ClientIP { get; set; }
        public string SirketAdi { get; set; }


        public DateTime FullDateTime { get; set; }
    }
}