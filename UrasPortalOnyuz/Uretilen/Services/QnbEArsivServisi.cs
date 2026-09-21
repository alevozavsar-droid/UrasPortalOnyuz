// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace WebApplication3.Services
{





























    public class QnbEArsivServisi
    {
        private readonly IConfiguration _yapilandirma;
        private readonly ILogger<QnbEArsivServisi> _gunluk;
        private readonly IHttpClientFactory _istemciUreteci;




        private static readonly string AdAlaniEArsiv = "http://service.earsiv.uut.cs.com.tr/";
        private static readonly string AdAlaniWsse =
            "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";

        private static readonly string CanliAdres = "https://earsivportal.efinans.com.tr/earsiv/ws";
        private static readonly string TestAdres = "https://earsivtest.qnbesolutions.com.tr/earsiv/ws";

        public QnbEArsivServisi(IConfiguration yapilandirma, ILogger<QnbEArsivServisi> gunluk,
                                IHttpClientFactory istemciUreteci)
 {}

        private string TemelAdres(bool testMi)  {return default;
}

        public class EArsivSonuc
        {
            public bool Basarili { get; set; }
            public string SonucKodu { get; set; }
            public string Mesaj { get; set; }

            public Dictionary<string, string> Ekler { get; } = new Dictionary<string, string>();

            public string HamYanit { get; set; }
        }












        public async Task<EArsivSonuc> FaturaOlusturAsync(
            string kullanici, string parola, string saticiVkn, string ublXml,
            string sube = "DFLT", string kasa = "DFLT", string erpKodu = "ERP1",
            bool testMi = false)
 {return default;
}


        public enum BelgeBicimi
        {

            Ubl = 1,

            Html = 2,

            Pdf = 3
        }

        public class BelgeSonuc
        {
            public bool Basarili { get; set; }
            public string SonucKodu { get; set; }
            public string Mesaj { get; set; }
            public byte[] Icerik { get; set; }

            public string Bicim { get; set; }
        }











        public async Task<BelgeSonuc> BelgeGetirAsync(
            string kullanici, string parola, string saticiVkn,
            string faturaNo, string faturaUuid = null,
            BelgeBicimi bicim = BelgeBicimi.Pdf, bool testMi = false)
 {return default;
}




        public Task<EArsivSonuc> BaglantiyiSinaAsync(
            string kullanici, string parola, string saticiVkn, bool testMi = false)
 {return System.Threading.Tasks.Task.FromResult<EArsivSonuc>(default);
}












        public async Task<EArsivSonuc> YapilandirmaAyarlariAlAsync(
            string kullanici, string parola, string saticiVkn, bool testMi = false)
 {return default;
}



        private void Coz(string govde, EArsivSonuc sonuc)
 {}

        private static string Zarf(string kullanici, string parola, string govde)  {return default;
}

        private static string Kacir(string s)  {return default;
}

        private async Task<(string Govde, string Hata)> GonderAsync(string adres, string zarf)
 {return default;
}
    }
}
