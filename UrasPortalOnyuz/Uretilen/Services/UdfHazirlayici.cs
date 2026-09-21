// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace WebApplication3.Services
{







    public static class UdfHazirlayici
    {
        public sealed class Alan
        {
            public string Tablo { get; set; }      // OITM, OINV ...
            public string Ad { get; set; }         // U_ ön eki OLMADAN: BE1_BCORAN
            public string Aciklama { get; set; }
            public int Boy { get; set; } = 50;
            public string Kolon => "U_" + Ad;
            public Alan(string tablo, string ad, string aciklama, int boy)  {}
        }

        public sealed class Sonuc
        {
            public List<string> Olusturulan { get; } = new List<string>();

            public HashSet<string> Eksik { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public string Mesaj { get; set; }
            public bool EksikMi(string tablo, string kolon)  {return default;
}
        }


        public static readonly Alan[] KalemIkinciBirimAlanlari =
        {
            new Alan("OITM", "BE1_IkOBm", "İkinci Birim Miktar", 15),
            new Alan("OITM", "BE1_BCORAN", "Birim Cinsi Çeviri Oranı", 10),
        };

        private static readonly ConcurrentDictionary<string, bool> _varOlanlar = new ConcurrentDictionary<string, bool>(StringComparer.OrdinalIgnoreCase);


        public static Sonuc Hazirla(ControllerBase c, IConfiguration cfg, string connectionString, string dbKey, IEnumerable<Alan> alanlar, ILogger log = null)
 {return default;
}

        public static async Task<Sonuc> HazirlaAsync(ControllerBase c, IConfiguration cfg, string connectionString, string dbKey, IEnumerable<Alan> alanlar, ILogger log = null)
 {return default;
}
    }
}
