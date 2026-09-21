// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Services
{













    public static class YansitmaServisi
    {

        public static readonly Dictionary<string, string> SirketOnek = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["URASKIMYA"] = "UK",
            ["ALV_KIMYA"] = "ALV",
            ["SELVI"] = "SLV",
            ["ASIA_KIMYA"] = "ASY",
            ["AVRASYA"] = "AVR",
            ["DRN"] = "DRN",
            ["URAS_BASKI"] = "URB",
        };


        public static bool NativeAlanVarMi(string kaynakDbAdi, string hedefDbAdi)  {return default;
}

        public class HesapKarsiligi { public string Kod; public string Ad; public bool Otomatik; }


        public class KalemKarsiligi { public string Kod; public string Ad; public bool Otomatik; }
    }


}
