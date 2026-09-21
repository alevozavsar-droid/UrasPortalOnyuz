// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace WebApplication3.Services
{











    public class YapayZekaIstemcisi
    {
        private static readonly string VarsayilanUcNoktasi = "https://api.anthropic.com/v1/messages";
        private static readonly string SurumBasligi = "2023-06-01";

        private readonly HttpClient _istemci;
        private readonly string _anahtar;
        private readonly string _model;
        private readonly string _ucNoktasi;

        public YapayZekaIstemcisi(HttpClient istemci, IConfiguration yapilandirma)
 {}

        public bool AnahtarVar => _anahtar.Length > 0;


        public Task<string> MetinUretAsync(string kullaniciMesaji, string sistemYonergesi = null, int enFazlaJeton = 16000)
 {return System.Threading.Tasks.Task.FromResult<string>(default);
}





        public async Task<string> CevapAlAsync(string sistemYonergesi, string kullaniciMesaji, int enFazlaJeton = 16000)
 {return default;
}





        private static string MetniCikar(string ham)
 {return default;
}

        private static string HataMesajiCikar(string ham, HttpStatusCode kod)
 {return default;
}
    }
}
