using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class CekViewModel
    {
        public int? DocEntry { get; set; }
        public string? CekNumarasi { get; set; }
        public string? CiroEden { get; set; }
        public string? AsilBorclu { get; set; }
        public decimal? Tutar { get; set; }
        public string? ParaBirimi { get; set; }
        public DateTime? VadeTarihi { get; set; }
        public string? BankaAdi { get; set; }
        public string? CardCode { get; set; }
    }
}