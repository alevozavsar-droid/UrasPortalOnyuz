using System;

namespace WebApplication3.Models
{
    public class Rapor58ViewModel
    {
        public int FaturaNo { get; set; }
        public DateTime? IntacTarihi { get; set; }
        public DateTime BelgeTarihi { get; set; }
        public string FaturaAyi { get; set; }
        public string CariKodu { get; set; }
        public string CariAdi { get; set; }
        public string ParaBirimi { get; set; }
        public decimal DovizKuru { get; set; }
        public string StokHesapKodu { get; set; }
        public string StokHesapAciklamasi { get; set; }
        public string GtipKodu { get; set; } // Yeni
        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }


        public decimal SatisToplamiLPB { get; set; }
        public decimal KdvSatirLPB { get; set; }
        public decimal KdvSatisToplamiLPB { get; set; }


        public decimal SatisToplamiBPB { get; set; }
        public decimal KdvSatirBPB { get; set; }
        public decimal KdvSatisToplamiBPB { get; set; }

        public decimal KdvOrani { get; set; }
        public string KdvGrubuKodu { get; set; }
        public string KdvGrubuAdi { get; set; }
        public string KdvMuhasebeKodu { get; set; }
        public string KdvMuhasebeAdi { get; set; }
        public string BelgeTuru { get; set; }


        public string AktarDurumu { get; set; }
        public string IhracatDosyaNo { get; set; }
        public string BeyannameNo { get; set; }
    }
}