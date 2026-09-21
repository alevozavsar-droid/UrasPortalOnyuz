using System;

namespace WebApplication3.Models
{
    public class Rapor65ViewModel
    {
        public string CariKodu { get; set; }
        public string CariAdi { get; set; }
        public string SatisSorumlusu { get; set; }

        public decimal Borc { get; set; }
        public decimal Alacak { get; set; }
        public decimal Bakiye { get; set; }

        public decimal DovizBakiye { get; set; }
        public string ParaBirimi { get; set; }

        public DateTime? SonHareketTarihi { get; set; }
    }
}