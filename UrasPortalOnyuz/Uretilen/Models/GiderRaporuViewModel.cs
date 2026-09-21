using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class GiderRaporuViewModel
    {

        public string SirketAdi { get; set; }

        [Display(Name = "Ana Kategori")]
        public string TKategori { get; set; }

        [Display(Name = "Gider Türü")]
        public string TTur { get; set; }

        [Display(Name = "Hesap Açıklaması")]
        public string Descr { get; set; }

        [Display(Name = "Borç")]
        public decimal Borc { get; set; }

        [Display(Name = "Alacak")]
        public decimal Alacak { get; set; }

        [Display(Name = "Bakiye")]
        public decimal Bakiye { get; set; }

        [Display(Name = "Aktarım Durumu")]
        public string U_BE1_AKTAR { get; set; }

        [Display(Name = "Satır Açıklaması")]
        public string LineMemo { get; set; }

        [Display(Name = "Hesap Kodu")]
        public string AcctCode { get; set; }

        [Display(Name = "Belge Tarihi")]
        public DateTime RefDate { get; set; }

        [Display(Name = "Tedarikçi/Müşteri Adı")]
        public string CardName { get; set; }
    }

   
}