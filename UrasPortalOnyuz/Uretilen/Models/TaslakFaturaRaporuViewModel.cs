// TaslakFaturaRaporuViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class TaslakFaturaRaporuViewModel
    {
        [Display(Name = "Taslak No")]
        public int TaslakNo { get; set; }

        [Display(Name = "Fatura Tarihi")]
        public DateTime FaturaTarihi { get; set; }

        [Display(Name = "Tedarikçi Adı")]
        public string TedarikciAdi { get; set; }

        [Display(Name = "Tutar")]
        public decimal Tutar { get; set; }

        [Display(Name = "Sahip Adı")]
        public string SahipAdi { get; set; } // Belgeyi oluşturan/sahip olan kullanıcının adı
    }


  
}