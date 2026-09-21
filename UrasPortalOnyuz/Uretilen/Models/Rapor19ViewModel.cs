using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class Rapor19ViewModel
    {
        public int Levels { get; set; }
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public decimal? BorcTRY { get; set; }
        public decimal? AlacakTRY { get; set; }
        public decimal? BorcEUR { get; set; }
        public decimal? AlacakEUR { get; set; }
        public decimal? BorcUSD { get; set; }
        public decimal? AlacakUSD { get; set; }
        public decimal? BakiyeTRY { get; set; }
        public decimal? BakiyeEUR { get; set; }
        public decimal? BakiyeUSD { get; set; }
    }
}