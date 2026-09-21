using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace WebApplication3.Models
{


  



    public class AccountBalanceViewModel
    {
        public string CompanyName { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public decimal TLBalance { get; set; }
        public decimal CurrencyBalance { get; set; }
        public string Currency { get; set; } // TRY, USD, EUR vb.
    }
    public class RaporSummaryViewModel
    {
        public decimal TotalAcikGecikmisTutar { get; set; }
        public decimal TotalAcikKalanTutar { get; set; }
        public double AvgAcikGecikmisAdat { get; set; }
        public int TotalAcikGecikmisAdet { get; set; }
        public int EnEskiGecikmisGunFarki { get; set; }
        public string EnEskiGecikmisFaturaNo { get; set; }
    }


    public class CariEkstreViewModel
    {
        [Display(Name = "İşlem Anahtarı")]

        public int? IslemNo { get; set; }
        public string Phone1 { get; set; }
        public string Street { get; set; }
        public string County { get; set; } // İlçe
        public string Sirket { get; set; }
        public string State { get; set; }  // Şehir
        public string Country { get; set; } // Ülke
        public string CheckNum { get; set; }
        public int SatirNo { get; set; } // <-- Eklenecek/Kontrol Edilecek Alan

        [Display(Name = "Kayıt Tarihi")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
        public DateTime? KayitTarihi { get; set; }

        [Display(Name = "Vade Tarihi")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy}")]
        public DateTime? VadeTarihi { get; set; }

        [Display(Name = "Muhatap/Hesap Kodu")]
        public string MuhatapKodu { get; set; }

        [Display(Name = "Muhatap Adı")]
        public string MuhatapAdi { get; set; }

        public bool IsAylikToplam { get; set; } = false;

        public bool IsSummaryRow { get; set; }
        public int S { get; set; }

        [Display(Name = "Satır Ayrıntıları")]
        public string Aciklama { get; set; }

        [Display(Name = "Aktarım Tipi")]
        public string AktarimTipi { get; set; }

        [Display(Name = "İşlem Tipi")]
        public string IslemTipi { get; set; }

        [Display(Name = "Borç (TRY)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TRYB { get; set; }

        [Display(Name = "Alacak (TRY)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TRYA { get; set; }

        [Display(Name = "Kümülatif Bakiye (TRY)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? TRY_KmlBky { get; set; }

        [Display(Name = "Borç (İşlem PB)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? IslemB { get; set; }

        [Display(Name = "Alacak (İşlem PB)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? IslemA { get; set; }

        [Display(Name = "Kümülatif Bakiye (İşlem PB)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? Islem_KmlBky { get; set; }

        [Display(Name = "İşlem Para Birimi")]
        public string IslemPB { get; set; }
    }


    public class CariDetailViewModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Address { get; set; } // Genellikle tam adres satırı
        public string Street { get; set; } // Sokak
        public string County { get; set; } // İlçe/İl (DB'ye göre değişebilir)
        public string State { get; set; }  // İl/Eyalet
        public string Country { get; set; } // Ülke
    }



    public class CariViewModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }

        public string Address { get; set; }
        public string Phone { get; set; }
    }


    public class CariEkstreRaporuViewModel
    {
        public string AnaBpCode { get; set; }
        public string AnaBpName { get; set; }
        public List<CariEkstreViewModel> AnaEkstre { get; set; } = new List<CariEkstreViewModel>();

        public string BagliBpCode { get; set; }
        public string BagliBpName { get; set; }
        public List<CariEkstreViewModel> BagliEkstre { get; set; } = new List<CariEkstreViewModel>();

        public bool HasBagliCari => !string.IsNullOrEmpty(BagliBpCode) && BagliEkstre.Any();


        public bool IncludeConnectedBp { get; set; }


        public List<AccountBalanceViewModel> BpRelatedAccountBalances { get; set; } = new List<AccountBalanceViewModel>();



        public string SelectedDbDisplay { get; set; }
        public string SelectedBpCode { get; set; }
        public string SelectedBpName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public List<string> SelectedAktarimTipi { get; set; }
        public bool IncludeInitialBalance { get; set; }
        public List<CariViewModel> CariList { get; set; }
        public List<string> AktarimTipiList { get; set; }
    }
}