using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class MusteriRaporuViewModel
    {
        public string MusteriKodu { get; set; }
        public string MusteriAdi { get; set; }
        public decimal? GuncelBakiye { get; set; }
        public decimal VadesiGecmisBakiye { get; set; }
        public decimal VadesiGelmemisBakiye { get; set; }
        public int AdatOrtalamaGecikmeSuresi { get; set; }
        public string SatisCalisani { get; set; }
        public string MusteriGrubu { get; set; }
        public int AdatIlkGecmisVadeTarihi { get; set; }
        public DateTime? SonSevkiyatTarihi { get; set; }
        public DateTime? SonTahsilatTarihi { get; set; }
    }
}