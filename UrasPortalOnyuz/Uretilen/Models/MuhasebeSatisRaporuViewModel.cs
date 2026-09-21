// MuhasebeSatisRaporuViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class MuhasebeSatisRaporuViewModel
    {
        public int FaturaNo { get; set; }
        public DateTime FaturaTarihi { get; set; }
        public DateTime BelgeTarihi { get; set; }

        public string FaturaAyi { get; set; }
        public string CariKodu { get; set; }
        public string CariAdi { get; set; }
        public string ParaBirimi { get; set; }
        public decimal DovizKuru { get; set; }
        public string StokHesapKodu { get; set; }
        public string StokHesapAciklamasi { get; set; }
        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
        public string IslemTipi { get; set; }
        public decimal IkinciMiktar { get; set; }


        public decimal SatisToplamiLPB { get; set; }
        public decimal KdvSatirLPB { get; set; }
        public decimal KdvSatisToplamiLPB { get; set; }


        public decimal SatisToplamiBPB { get; set; }
        public decimal KdvSatirBPB { get; set; }
        public decimal KdvSatisToplamiBPB { get; set; }
        public string BelgeNumarasi { get; set; }
        public decimal KdvOrani { get; set; }
        public string KdvGrubuKodu { get; set; }
        public string KdvGrubuAdi { get; set; }
        public string KdvMuhasebeKodu { get; set; }
        public string KdvMuhasebeAdi { get; set; }
        public string FaturaTipi { get; set; }
         public string MuafiyetKodu { get; set; }
        public string BelgeTuru { get; set; }
    }


  
}