// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApplication3.Services
{









    public class PlanliOdemeServisi
    {
        private readonly IConfiguration _yapilandirma;
        private readonly EmailService _eposta;
        private readonly ILogger<PlanliOdemeServisi> _gunluk;


        public static readonly List<(string DbName, string Ad)> Sirketler = new List<(string, string)>
        {
            ("URASKIMYA",    "URAS KİMYA"),
            ("URSMAKINE",    "URS MAKİNE"),
            ("ALV_KIMYA",    "ALV KİMYA"),
            ("AVRUPA_PAPER", "AVRUPA PAPER"),
            ("SELVI",        "SELVİ KİMYA"),
            ("DRN",          "DRN"),
            ("ALVFILO",      "ALV FİLO"),
            ("URAS_HOLDING", "URAS HOLDİNG")
        };

        public PlanliOdemeServisi(IConfiguration yapilandirma, EmailService eposta, ILogger<PlanliOdemeServisi> gunluk)
 {}


        private string PortalBaglantisi()  {return default;
}

        private string SirketBaglantisi(string dbName)
 {return default;
}







        private static int _semaHazir;












        public static List<PlanliOdemeTaksit> TaksitleriUret(PlanliOdeme plan)
 {return default;
}









        public static DateTime OdemeTarihi(int yil, int ay, int ayinGunu)
        {
            int sonGun = DateTime.DaysInMonth(yil, ay);
            return new DateTime(yil, ay, Math.Min(Math.Max(ayinGunu, 1), sonGun));
        }


        public static DateTime HatirlatmaTarihi(int yil, int ay, int ayinGunu, int gunOnce)
 {return default;
}





        public class GunlukSonuc
        {
            public int HatirlatmaGonderildi { get; set; }
            public int SatirOlusturuldu { get; set; }
            public List<string> Hatalar { get; } = new List<string>();
        }






        public async Task<GunlukSonuc> GunuIsleAsync(DateTime gun)
 {return default;
}






        public async Task<(bool Basarili, string Mesaj)> HatirlatmayiSimdiGonderAsync(int planId)
 {return default;
}





        private static string PlanTipiAdi(string tip)  {return default;
}

        private static string Kacir(string s)  {return default;
}




















        public async Task<(bool Basarili, string Mesaj)> NakitAkisiSatiriOlusturAsync(PlanliOdeme plan, DateTime odemeTarihi, PlanliOdemeTaksit taksit = null)
 {return default;
}





        private static readonly ConcurrentDictionary<string, Dictionary<string, int>> _kolonGenisligi =
            new ConcurrentDictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        private async Task<Dictionary<string, int>> KolonGenislikleriAsync(string dbName)
 {return default;
}

        private static string Kirp(Dictionary<string, int> genislikler, string kolon, string deger)
 {return default;
}


        private async Task IslemGunluguneYazAsync(string dbName, int docEntry, string aciklama)
 {}

        private async Task<decimal> GuncelKurAsync(string dbName, string paraBirimi, DateTime tarih)
 {return default;
}
    }

    public class PlanliOdeme
    {
        public int Id { get; set; }
        public string SirketDb { get; set; }
        public string OdemeSebebi { get; set; }
        public string Aciklama { get; set; }
        public string Kurum { get; set; }
        public decimal Tutar { get; set; }
        public string ParaBirimi { get; set; }
        public int AyinGunu { get; set; }
        public int HatirlatmaGunOnce { get; set; }
        public string OdemeYontemi { get; set; }
        public string Satinalmaci { get; set; }
        public DateTime? BaslangicTarihi { get; set; }
        public DateTime? BitisTarihi { get; set; }
        public bool Aktif { get; set; }
        public string OlusturanKullanici { get; set; }
        public DateTime? OlusturmaTarihi { get; set; }
        public DateTime? SonGuncelleme { get; set; }





        public string PlanTipi { get; set; } = "ESIT";
        public int? TaksitSayisi { get; set; }
        public decimal? AnaparaToplam { get; set; }
        public decimal? IlkFaiz { get; set; }
        public decimal? FaizArtis { get; set; }

        public string FaizArtisTipi { get; set; }
        public bool TaksitliMi => PlanTipi == "ARTAN" || PlanTipi == "YAPILANDIRMA" || (TaksitSayisi ?? 0) > 0;
    }

    public class PlanliOdemeTaksit
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public int TaksitNo { get; set; }
        public string Donem { get; set; }
        public DateTime OdemeTarihi { get; set; }
        public decimal Anapara { get; set; }
        public decimal Faiz { get; set; }
        public decimal GecikmeBedeli { get; set; }
        public decimal Toplam { get; set; }
        public string Aciklama { get; set; }
        public bool Odendi { get; set; }
        public int? NakitAkisDocEntry { get; set; }
        public string Guncelleyen { get; set; }
        public DateTime? GuncellemeTarihi { get; set; }
    }
}
