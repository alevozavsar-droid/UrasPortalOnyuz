using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class AlvFiyatViewModel
    {
        public int DocEntry { get; set; }
        public int? DocNum { get; set; }
        public string? CustomerCode { get; set; }
        public string? CountryCode { get; set; }
        public string? Month { get; set; }
        public string? Currency { get; set; }
        public decimal EuroUsdParity { get; set; }
        public decimal CofasLimit { get; set; }
        public string? Term { get; set; }
        public decimal Balance { get; set; }


        public decimal ExworkLiman { get; set; }
        public decimal ExworkBanka { get; set; }
        public decimal ExworkGumruk { get; set; }
        public decimal ExworkExtra { get; set; } // Yeni eklendi
        public decimal ExworkDiger { get; set; }
        public decimal ExworkToplam { get; set; }
        public decimal ExworkMasraf { get; set; }


        public decimal FobLiman { get; set; }
        public decimal FobBanka { get; set; }
        public decimal FobGumruk { get; set; }
        public decimal FobExtra { get; set; } // U_BE1_FOBDIGER1 için
        public decimal FobDiger { get; set; } // U_BE1_FOBDIGER2 için
        public decimal FobToplam { get; set; }
        public decimal FobMasraf { get; set; }


        public decimal FlexiNavlun { get; set; }
        public decimal FlexiBanka { get; set; }
        public decimal FlexiGumruk { get; set; }
        public decimal FlexiNakliye { get; set; }
        public decimal FlexiFlexi { get; set; }
        public decimal FlexiToplam { get; set; }
        public decimal FlexiMasraf { get; set; }


        public decimal TonajIbc { get; set; }
        public decimal TonajDrums { get; set; }
        public decimal TonajBulk { get; set; }


        public string? Yarimamul1Name { get; set; }
        public decimal Yarimamul1Price { get; set; }
        public DateTime? Yarimamul1Date { get; set; } // Yeni eklendi
        public string? Yarimamul2Name { get; set; }
        public decimal Yarimamul2Price { get; set; }
        public DateTime? Yarimamul2Date { get; set; } // Yeni eklendi

        public string? Aciklama { get; set; }

        public List<AlvFiyatDetay> Details { get; set; } = new List<AlvFiyatDetay>();
    }

    public class AlvFiyatDetay
    {
        public int DocEntry { get; set; }
        public int LineId { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public string? CounterType { get; set; }
        public decimal KazanIciUsd { get; set; }
        public decimal KazanIciEur { get; set; }
        public decimal KarlilikOrani { get; set; }
        public decimal KarlilikYuzdesi { get; set; }
        public decimal AmbalajIbc { get; set; }
        public decimal AmbalajDrums { get; set; }

        public decimal AmbalajBulk { get; set; }
        public decimal Indirim { get; set; }
        public decimal Kar { get; set; }
        public decimal Komisyon { get; set; }
        public string? Vade { get; set; }
        public decimal ExwMasraf { get; set; }
        public decimal ExwIbcUsdKg { get; set; }
        public decimal ExwDrumsUsdKg { get; set; }
        public decimal FobIbcUsdKg { get; set; }
        public decimal FobDrumsUsdKg { get; set; }
    }
}