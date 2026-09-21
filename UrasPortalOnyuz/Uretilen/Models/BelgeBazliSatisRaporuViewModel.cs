// BelgeBazliSatisRaporuViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class BelgeBazliSatisRaporuViewModel
    {
        public int FaturaNo { get; set; }
        public DateTime FaturaTarihi { get; set; }
        public string FaturaAyi { get; set; }
        public string CariKodu { get; set; }
        public string CariAdi { get; set; }
        public string ParaBirimi { get; set; }
        public decimal DovizKuru { get; set; }


        public decimal BelgeToplamiLPB { get; set; }
        public decimal KdvToplamiLPB { get; set; }
        public decimal KdvBelgeToplamiLPB { get; set; }
        public string FaturaTipi { get; set; }
        public string MuafiyetKodu { get; set; }

        public decimal BelgeToplamiBPB { get; set; }
        public decimal KdvToplamiBPB { get; set; }
        public decimal KdvBelgeToplamiBPB { get; set; }
        public string BelgeNumarasi { get; set; }
        public decimal KdvOrani { get; set; }
        public string BelgeTuru { get; set; }
    }


   
}