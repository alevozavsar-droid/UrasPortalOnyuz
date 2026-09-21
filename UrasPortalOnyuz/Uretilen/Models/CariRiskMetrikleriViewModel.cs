using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace WebApplication3.Models
{
    public class CariRiskMetrikleriViewModel
    {
        [DisplayName("Cari Kodu")]
        public string CariKodu { get; set; }

        [DisplayName("Cari Adı")]
        public string CariAdi { get; set; }

        [DisplayName("Toplam Risk (?)")]
        [DataType(DataType.Currency)]
        public decimal? ToplamRisk { get; set; }

        [DisplayName("Portföydeki Çek (Vadesi Gelmemiş) (?)")]
        [DataType(DataType.Currency)]
        public decimal? PortfoydekiCek { get; set; }

        [DisplayName("Toplam Açık Fatura (?)")]
        [DataType(DataType.Currency)]
        public decimal? ToplamAcikFatura { get; set; }

        [DisplayName("V. Geçmiş Tutar (?)")]
        [DataType(DataType.Currency)]
        public decimal? VGecmisTutar { get; set; }

        [DisplayName("V. Gelecek Tutar (?)")]
        [DataType(DataType.Currency)]
        public decimal? VGelecekTutar { get; set; }

        [DisplayName("V.G. Adet")]
        public int? VGecmisAdet { get; set; }

        [DisplayName("Ort. Adat (Gün)")]
        public double? OrtalamaAdat { get; set; }

        [DisplayName("Ort. Tah. Yaş. (Gün)")]
        public double? OrtalamaTahsilatYaslandirma { get; set; }

        [DisplayName("En Eski Vade Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? EnEskiVadeTarihi { get; set; }

        [DisplayName("En Eski Fark (Gün)")]
        public int? EnEskiFarkGun { get; set; }

        [DisplayName("En Son Teslimat")]
        [DataType(DataType.Date)]
        public DateTime? EnSonTeslimat { get; set; }

        [DisplayName("Son Tahsilat")]
        [DataType(DataType.Date)]
        public DateTime? SonTahsilat { get; set; }
    }

}