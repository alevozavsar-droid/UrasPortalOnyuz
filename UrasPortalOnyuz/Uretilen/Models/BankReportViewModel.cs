using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class BankReportViewModel
    {
        public string BankaAdi { get; set; }
        public string ParaBirimi { get; set; }
        public decimal Bakiye { get; set; }
        public decimal KullanilabilirBakiye { get; set; }
        public decimal KrediRiski { get; set; }
        public decimal KrediLimiti { get; set; }
        public string KaynakFirma { get; set; }
        public DateTime UpdateDate { get; set; }
        public string FormattedUpdateTime { get; set; }
        public decimal GuncelTLTutari { get; set; }
    }
}