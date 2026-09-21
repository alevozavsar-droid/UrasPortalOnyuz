// KdvOzetRaporuViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class KdvOzetRaporuViewModel
    {
        [Display(Name = "Fatura No")]
        public int FaturaNo { get; set; }

        [Display(Name = "Fatura Tarihi")]
        public DateTime FaturaTarihi { get; set; }

        [Display(Name = "Fatura Ayı")]
        public string FaturaAyi { get; set; }

        [Display(Name = "Cari Kodu")]
        public string CariKodu { get; set; }

        [Display(Name = "Cari Adı")]
        public string CariAdi { get; set; }

        [Display(Name = "Para Birimi")]
        public string ParaBirimi { get; set; }

        [Display(Name = "Döviz Kuru")]
        public decimal DovizKuru { get; set; }

        [Display(Name = "KDV Grubu Kodu")]
        public string KdvGrubuKodu { get; set; }

        [Display(Name = "KDV Grubu Adı")]
        public string KdvGrubuAdi { get; set; }

        [Display(Name = "KDV Muhasebe Kodu")]
        public string KdvMuhasebeKodu { get; set; }

        [Display(Name = "KDV Muhasebe Adı")]
        public string KdvMuhasebeAdi { get; set; }


        public decimal SatisToplamiLPB { get; set; }
        public decimal KdvToplamiLPB { get; set; }
        public decimal KdvSatisToplamiLPB { get; set; }


        public decimal SatisToplamiBPB { get; set; }
        public decimal KdvToplamiBPB { get; set; }
        public decimal KdvSatisToplamiBPB { get; set; }

        [Display(Name = "Belge Türü")]
        public string BelgeTuru { get; set; }
    }


   
}