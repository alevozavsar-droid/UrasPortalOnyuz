// HesapKategorizasyonViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class HesapKategorizasyonViewModel
    {
        [Display(Name = "Hesap Kodu")]
        public string AcctCode { get; set; }

        [Display(Name = "Hesap Adı")]
        public string AcctName { get; set; }

        [Display(Name = "Gider Türü Açıklaması")]
        public string GiderTuruAciklamasi { get; set; } // t1.Descr'dan geliyor
    }


   
}