using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class MusteriCekBakiyeViewModel
    {
        public string MusteriBilgisi { get; set; }
        public decimal? ToplamBakiye { get; set; }
        public decimal? VadesiGelecekTutar { get; set; }
        public decimal? VadesiGecmisTutar { get; set; }
        public decimal? AlinanCekToplamBakiye { get; set; }
        public decimal? AlinanCekVadesiGecmisBakiye { get; set; }
        public decimal? PortfoyCekToplam { get; set; }
    }
}