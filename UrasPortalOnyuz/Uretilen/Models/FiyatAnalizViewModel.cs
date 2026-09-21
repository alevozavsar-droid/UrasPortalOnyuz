using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class FiyatAnalizViewModel
    {

        public int DocEntry { get; set; }


        [Display(Name = "Müşteri Adı")]
        public string Customer { get; set; }

        [Display(Name = "Ülke")]
        public string Ulke { get; set; }

        [Display(Name = "Ay")]
        public string Ay { get; set; }

        [Display(Name = "Para Birimi")]
        public string ParaBirimi { get; set; }


        [Display(Name = "Kazan İçi USD")]
        public decimal? KazanIciUSD { get; set; }

        [Display(Name = "Cofas Limiti")]
        public decimal? CofasLimiti { get; set; }

        [Display(Name = "Vade")]
        public string Vade { get; set; }

        [Display(Name = "Bakiye")]
        public decimal? Bakiye { get; set; }

        [Display(Name = "Exwork Liman")]
        public decimal? ExwLiman { get; set; }

        [Display(Name = "Exwork Banka")]
        public decimal? ExwBanka { get; set; }

        [Display(Name = "Exwork Gümrük")]
        public decimal? ExwGumruk { get; set; }

        [Display(Name = "Exwork Extra")]
        public decimal? ExwExtra { get; set; }

        [Display(Name = "Exwork Diğer")]
        public decimal? ExwDiger { get; set; }

        [Display(Name = "Exwork Toplam")]
        public decimal? ExwToplam { get; set; }

        [Display(Name = "Exwork Masraf")]
        public decimal? ExwMasraf { get; set; }

        [Display(Name = "FOB Liman")]
        public decimal? FobLiman { get; set; }

        [Display(Name = "FOB Banka")]
        public decimal? FobBanka { get; set; }

        [Display(Name = "FOB Gümrük")]
        public decimal? FobGumruk { get; set; }

        [Display(Name = "FOB Diğer 1")]
        public decimal? FobDiger1 { get; set; }

        [Display(Name = "FOB Diğer 2")]
        public decimal? FobDiger2 { get; set; }

        [Display(Name = "FOB Toplam")]
        public decimal? FobToplam { get; set; }

        [Display(Name = "FOB Masraf")]
        public decimal? FobMasraf { get; set; }

        [Display(Name = "Tonaj IBC")]
        public decimal? TonajIbc { get; set; }

        [Display(Name = "Tonaj Drums")]
        public decimal? TonajDrum { get; set; }

        [Display(Name = "Tonaj Bulk")]
        public decimal? TonajBulk { get; set; }


        [Display(Name = "1. Yarı Mamül Adı")]
        public string YariMamul1 { get; set; }

        [Display(Name = "1. Yarı Mamül Fiyatı")]
        public decimal? YariMamulFiyat1 { get; set; }

        [Display(Name = "1. Yarı Mamül Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? YariMamulTarih1 { get; set; }

        [Display(Name = "2. Yarı Mamül Adı")]
        public string YariMamul2 { get; set; }

        [Display(Name = "2. Yarı Mamül Fiyatı")]
        public decimal? YariMamulFiyat2 { get; set; }

        [Display(Name = "2. Yarı Mamül Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? YariMamulTarih2 { get; set; }

        [Display(Name = "Açıklama")]
        public string Aciklama { get; set; }
    }
}
