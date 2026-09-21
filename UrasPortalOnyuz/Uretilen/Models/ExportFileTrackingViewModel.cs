using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{

    public class ExportFileTrackingViewModel
    {
        public string IhracatDosyaNumarasi { get; set; }
        public string MusteriTemsilcisi { get; set; }
        public string SirketAdi { get; set; }
        public string FaturaNo { get; set; }
        public string MusteriFirmaAdi { get; set; }
        public string Ulke { get; set; }
        public string GumrukBeyannameIntacTarihi { get; set; }
        public string TaahhutDolumTarihi { get; set; }
        public int? KalanGunSayisi { get; set; }
        public string GumrukBeyannameNumarasi { get; set; }
        public string GumrukBeyannameTutari { get; set; }
        public string OdemeYontemi { get; set; }
        public string OdemeTarihi1 { get; set; }
        public string OdemeTutari1 { get; set; }
        public string OdemeTarihi2 { get; set; }
        public string OdemeTutari2 { get; set; }
        public string OdemeTarihi3 { get; set; }
        public string OdemeTutari3 { get; set; }
        public string OdemeTarihi4 { get; set; }
        public string OdemeTutari4 { get; set; }
        public string OdemeGelecekBanka { get; set; }
        public string KalanOdemeTutari { get; set; }
        public string FaturaVadeTarihi { get; set; }
        public string VadeyeGoreGecikme { get; set; }
        public string BelgeTuru { get; set; }
        public int? SatisSiparisNo { get; set; }
        public string FaturaAyi { get; set; }
    }
}