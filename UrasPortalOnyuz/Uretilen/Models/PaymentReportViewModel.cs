using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    
        public class PaymentReportViewModel
        {
            public string Belge { get; set; }
            public int DocEntry { get; set; }
            public string TarihAraligi { get; set; }
            public DateTime? OrderDate { get; set; }
            public string TedarikciAlacakli { get; set; }
            public int DocNum { get; set; }
            public string FaturaNo { get; set; }
            public string OdemeNedeni { get; set; }
            public string MutabakatDurumu { get; set; }
            public DateTime? VadeTarihi { get; set; }
            public DateTime? GerceklesenOdemeTarihi { get; set; }
            public int? GunFarki { get; set; }
            public decimal? Tutar { get; set; }
            public decimal? OdenenTutar { get; set; }
            public string ParaBirimi { get; set; }
            public string OdemeSekli { get; set; }
            public decimal? GuncelTLTutar { get; set; }
            public string Durum { get; set; }
            public string Sahip { get; set; }
        }
    
}
