using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class CompanySalesData
    {
        public string Aciklama { get; set; } // 'AÇIKLAMA'
        public decimal Ocak { get; set; }
        public decimal Subat { get; set; }
        public decimal Mart { get; set; }
        public decimal Nisan { get; set; }
        public decimal Mayis { get; set; }
        public decimal Haziran { get; set; }
        public decimal Temmuz { get; set; }
        public decimal Agustos { get; set; }
        public decimal Eylul { get; set; }
        public decimal Ekim { get; set; }
        public decimal Kasim { get; set; }
        public decimal Aralik { get; set; }
        public decimal Toplam { get; set; } // 'TOPLAM'
        public decimal YillikOrtalama { get; set; } // '2025 AYLIK ORTALAMA'
        public string ToplamDikeyAnaliz { get; set; } // 'TOPLAM DİKEY ANALİZ' (formatted as percentage)
    }
}
