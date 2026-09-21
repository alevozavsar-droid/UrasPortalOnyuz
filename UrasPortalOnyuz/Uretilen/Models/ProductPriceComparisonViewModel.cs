using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class ProductPriceComparisonViewModel
    {
        [Display(Name = "Malzeme Kodu")]
        public string? MalzemeKodu { get; set; }

        [Display(Name = "Malzeme Açıklaması")]
        public string? MalzemeAciklamasi { get; set; }


        [Display(Name = "Son Fatura No")]
        public int? SonFaturaNo { get; set; }

        [Display(Name = "Son Fatura Tarihi")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? SonFaturaTarihi { get; set; }

        [Display(Name = "Son Birim Fiyat")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? SonBirimFiyat { get; set; }

        [Display(Name = "Son Para Birimi")]
        public string? SonParaBirimi { get; set; }

        [Display(Name = "Son Fiyat (TRY)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? SonFiyatTRY { get; set; }

        [Display(Name = "Son Fiyat (EUR)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? SonFiyatEUR { get; set; }

        [Display(Name = "Son Fiyat (USD)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? SonFiyatUSD { get; set; }

        [Display(Name = "Son Miktar")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? SonMiktar { get; set; }

        [Display(Name = "Son Ölçü Birimi")]
        public string? SonOlcubirimi { get; set; }

        [Display(Name = "Son Satıcı Kodu")]
        public string? SonSaticiKodu { get; set; }

        [Display(Name = "Son Satıcı Adı")]
        public string? SonSaticiAdi { get; set; }

        [Display(Name = "Son Satıcı Grup Adı")]
        public string? SonSaticiGrupAdi { get; set; }



        [Display(Name = "Önceki Fatura No")]
        public int? OncekiFaturaNo { get; set; }

        [Display(Name = "Önceki Fatura Tarihi")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}", ApplyFormatInEditMode = true)]
        public DateTime? OncekiFaturaTarihi { get; set; }

        [Display(Name = "Önceki Birim Fiyat")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? OncekiBirimFiyat { get; set; }

        [Display(Name = "Önceki Para Birimi")]
        public string? OncekiParaBirimi { get; set; }

        [Display(Name = "Önceki Fiyat (TRY)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? OncekiFiyatTRY { get; set; }

        [Display(Name = "Önceki Fiyat (EUR)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? OncekiFiyatEUR { get; set; }

        [Display(Name = "Önceki Fiyat (USD)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? OncekiFiyatUSD { get; set; }

        [Display(Name = "Önceki Miktar")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? OncekiMiktar { get; set; }

        [Display(Name = "Önceki Ölçü Birimi")]
        public string? OncekiOlcubirimi { get; set; }

        [Display(Name = "Önceki Satıcı Kodu")]
        public string? OncekiSaticiKodu { get; set; }

        [Display(Name = "Önceki Satıcı Adı")]
        public string? OncekiSaticiAdi { get; set; }

        [Display(Name = "Önceki Satıcı Grup Adı")]
        public string? OncekiSaticiGrupAdi { get; set; }



        [Display(Name = "Ölçü Birimi Durumu")]
        public string? OlcubirimiDurumu { get; set; }

        [Display(Name = "Fiyat Farkı (TRY)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? FiyatFarkiTRY { get; set; }

        [Display(Name = "Fiyat Değişim % (TRY)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? FiyatDegisimYuzdesiTRY { get; set; }

        [Display(Name = "Fiyat Farkı (EUR)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? FiyatFarkiEUR { get; set; }

        [Display(Name = "Fiyat Değişim % (EUR)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? FiyatDegisimYuzdesiEUR { get; set; }

        [Display(Name = "Fiyat Farkı (USD)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? FiyatFarkiUSD { get; set; }

        [Display(Name = "Fiyat Değişim % (USD)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal? FiyatDegisimYuzdesiUSD { get; set; }

        [Display(Name = "Para Birimi Durumu")]
        public string? ParaBirimiDurumu { get; set; }
    }
}