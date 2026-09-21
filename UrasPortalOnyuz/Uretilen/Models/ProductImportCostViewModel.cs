using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class ProductImportCostViewModel
    {
        public string TedarikciAdi { get; set; }
        public string Kalemİsmi { get; set; }
        public decimal? Miktar { get; set; }
        public decimal? TemelBelgeFiyati { get; set; }
        public string TemelBelgeParaBirimi { get; set; }
        public decimal? IthalatMaliyetiKuru { get; set; }
        public decimal? BirimFiyatTL { get; set; }

       
        public decimal? MaliyetFarkiYuzde { get; set; }
        public decimal? BirimGiderTL { get; set; }
        public decimal? BirimMaliyetTL { get; set; }
        public decimal? BirimMaliyetBelgeParaBirimi { get; set; }
        public int? MalGirisiNo { get; set; }
        public DateTime? MalGirisiTarihi { get; set; }
        public int? MaliyetBelgeNo { get; set; }
    }
}
