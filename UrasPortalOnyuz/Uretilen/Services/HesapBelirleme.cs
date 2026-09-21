// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Services
{








    public static class HesapBelirleme
    {
        public class SatirGirdi
        {
            public int Sira { get; set; }
            public string ItemCode { get; set; }
            public string AccountCode { get; set; }     // hizmet satırı
            public string WarehouseCode { get; set; }
            public string VatGroup { get; set; }
            public int? BaseType { get; set; }
            public int? BaseEntry { get; set; }
            public int? BaseLine { get; set; }
            public string Gider { get; set; }           // seçilen gider/gelir kodu
        }

        private static readonly Dictionary<int, string> _bazSatirTablolari = new Dictionary<int, string>
        {
            { 22, "POR1" }, { 20, "PDN1" }, { 18, "PCH1" }, { 21, "RPD1" }, { 19, "RPC1" },
            { 17, "RDR1" }, { 15, "DLN1" }, { 13, "INV1" }, { 16, "RDN1" }, { 14, "RIN1" }
        };





        public static string GiderKoduKontrol(string connectionString, IList<SatirGirdi> satirlar, string cardCode, DateTime tarih, bool satis, string satirTablosu)
 {return default;
}
    }
}
