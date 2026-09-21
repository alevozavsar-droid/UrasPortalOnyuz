// IadeEdilenMalzemeViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class IadeEdilenMalzemeViewModel
    {
        [Display(Name = "Fatura No")]
        public int FaturaNo { get; set; }

        [Display(Name = "Fatura Tarihi")]
        public DateTime FaturaTarihi { get; set; }

        [Display(Name = "Tedarikçi Kodu")]
        public string TedarikciKodu { get; set; }

        [Display(Name = "Tedarikçi Adı")]
        public string TedarikciAdi { get; set; }

        [Display(Name = "Ürün Kodu")]
        public string UrunKodu { get; set; }

        [Display(Name = "Ürün Adı")]
        public string UrunAdi { get; set; }

        [Display(Name = "İade Miktarı")]
        public decimal IadeMiktari { get; set; }

        [Display(Name = "Birim Fiyat (İade)")]
        public decimal BirimFiyat { get; set; }

        [Display(Name = "İade Tutarı")]
        public decimal IadeTutari { get; set; } // Satır toplamı

        [Display(Name = "Toplam İade Tutarı")]
        public decimal ToplamIadeTutari { get; set; } // Belge toplamı

        [Display(Name = "Açıklama")]
        public string Aciklama { get; set; } // Belge Açıklaması (Comments)
    }


   
}