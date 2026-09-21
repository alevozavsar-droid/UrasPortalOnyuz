using System;

namespace WebApplication3.Models
{
    public class Rapor26ViewModel
    {
        public string CardCode { get; set; }
        public string MusteriFirmaAdi { get; set; }
        public string SatisSorumlusu { get; set; }


        public decimal? RBakiye { get; set; }
        public decimal? XBakiye { get; set; }
        public decimal? Hesap420Bakiyesi { get; set; }
        public decimal? GenelBakiyeFarki { get; set; } // (OCRD.Balance) - (R + X + 420)


        public int? RYaslananBakiyeGecikmeGun { get; set; }
        public decimal? RYaslananBakiye { get; set; }

        public int? XYaslananBakiyeGecikmeGun { get; set; }
        public decimal? XYaslananBakiye { get; set; }
    }
}