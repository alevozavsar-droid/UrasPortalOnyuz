using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication3.Models
{
    public class ProductCostDetailViewModel1
    {
        [Display(Name = "Maliyet Belge No")]
        public int? MaliyetBelgeNo { get; set; }

        [Display(Name = "Maliyet Belge Tarihi")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
        public DateTime? MaliyetBelgeTarihi { get; set; }

        [Display(Name = "Kalem İsmi")]
        public string Kalemİsmi { get; set; }

        [Display(Name = "Miktar")]
        public decimal? Miktar { get; set; }

        [Display(Name = "Temel Belge Fiyatı")]
        [DisplayFormat(DataFormatString = "{0:N4}")]
        public decimal? TemelBelgeFiyati { get; set; }

        [Display(Name = "Temel Belge Para Birimi")]
        public string TemelBelgeParaBirimi { get; set; }

        [Display(Name = "Gider Kalemi")]
        public string GiderKalemi { get; set; }

        [Display(Name = "Gider Tutarı (TL)")]
        [DisplayFormat(DataFormatString = "{0:N4}")]
        public decimal? GiderTutariTL { get; set; }

        [Display(Name = "Gider Tutarı (Yabancı Para)")]
        [DisplayFormat(DataFormatString = "{0:N4}")]
        public string? GiderTutariYabanciPara { get; set; }

        [Display(Name = "İthalat Maliyeti Kuru")]
        [DisplayFormat(DataFormatString = "{0:N4}")]
        public decimal? IthalatMaliyetiKuru { get; set; }
    }
}