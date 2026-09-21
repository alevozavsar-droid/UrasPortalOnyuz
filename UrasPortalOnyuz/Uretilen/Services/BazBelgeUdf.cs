// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebApplication3.Services
{













    public static class BazBelgeUdf
    {
        private static readonly Dictionary<int, (string Hdr, string Ln)> _tablolar = new Dictionary<int, (string, string)>
        {
            { 13, ("OINV", "INV1") }, { 14, ("ORIN", "RIN1") }, { 15, ("ODLN", "DLN1") }, { 16, ("ORDN", "RDN1") }, { 17, ("ORDR", "RDR1") },
            { 18, ("OPCH", "PCH1") }, { 19, ("ORPC", "RPC1") }, { 20, ("OPDN", "PDN1") }, { 21, ("ORPD", "RPD1") }, { 22, ("OPOR", "POR1") },
            { 23, ("OQUT", "QUT1") }, { 540000006, ("OPQT", "PQT1") }
        };


        private static readonly HashSet<string> _haricBaslik = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "U_BE1_EFATNO", "U_BE1_UUID", "U_BE1_GIBDURUM", "U_BE1_PROFILEID", "U_BE1_SENDDESPATCH",
            "U_BE1_TARGETDOC", "U_BE1_TARGETTYPE", "U_BE1_DISDOCENTRY",
            "U_BE1_TASLAKONAY", "U_BE1_TASLAKONAY2", "U_BE1_ONAYTEXT", "U_BE1_OPERATOR",
            "U_BE1_MTBKT", "U_BE1_ODMLINK1", "U_BE1_ODMLINK2"
        };

        private static readonly HashSet<string> _haricSatir = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "U_BE1_GIDER", "U_BE1_YZDRMDRM"
        };
        private static bool Haric(string kolon, bool satir)
 {return default;
}

        private static JToken Deger(object v)
 {return default;
}








        public static object EkranAlanlari(string connectionString, int baseType, int baseEntry, ILogger log = null)
 {return default;
}




        public sealed class Paket
        {
            public string Sade { get; set; }                              // eklemesiz gövde (SL reddederse bununla tekrar denenir)
            public Dictionary<string, JToken> Baslik { get; } = new Dictionary<string, JToken>(StringComparer.OrdinalIgnoreCase);
            public Dictionary<int, Dictionary<string, JToken>> Satir { get; } = new Dictionary<int, Dictionary<string, JToken>>();   // DocumentLines sırası = yeni LineNum
            public string HedefBaslik { get; set; }
            public string HedefSatir { get; set; }
            public bool SqlGerekli { get; set; }
            public HashSet<string> SaatAlanlari { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);   // SAP saat UDF'leri: SL'e "HH:mm:ss", SQL'e HHmm
            public int Adet => Baslik.Count + Satir.Values.Sum(s => s.Count);
            public IEnumerable<string> Anahtarlar => Baslik.Keys.Concat(Satir.Values.SelectMany(s => s.Keys)).Distinct(StringComparer.OrdinalIgnoreCase);
        }


        public static string Birlestir(string slGovde, string connectionString, string hedefBaslik, string hedefSatir, out Paket paket, ILogger log = null,
            int kopyaTip = 0, int kopyaEntry = 0, IList<int?> kopyaSatirlari = null)
 {paket = default;
return default;
}




        public static async Task<(HttpResponseMessage Cevap, string Metin)> PostAsync(HttpClient client, string hedef, string govde, Paket paket, ILogger log = null)
 {return default;
}


        public static void GerekirseSqlYaz(string connectionString, int yeniDocEntry, Paket paket, ILogger log = null)
 {}
    }
}
