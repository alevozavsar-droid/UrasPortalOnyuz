// KonsolideAlimViewModel.cs

using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class KonsolideAlimViewModel
    {
        [Display(Name = "DocEntry")]
        public int DocEntry { get; set; }
        public string IthalatDosyasi { get; set; }
        public string BelgeTuru { get; set; }
        [Display(Name = "Fatura Tarihi")]
        public DateTime FaturaTarihi { get; set; }

        [Display(Name = "Tedarikçi Adı")]
        public string TedarikciAdi { get; set; }

        [Display(Name = "Kod")]
        public string Kod { get; set; } // Stok Kodu veya Hesap Kodu

        [Display(Name = "Açıklama")]
        public string Aciklama { get; set; } // Stok Adı veya Hesap Adı

        [Display(Name = "Miktar")]
        public decimal Miktar { get; set; }

        [Display(Name = "Birim Cinsi")]
        public string BirimCinsi { get; set; }

        public decimal KdvTutari { get; set; }

        [Display(Name = "Fiyat")]
        public decimal Fiyat { get; set; }

        [Display(Name = "Vergi Hariç Tutar")]
        public decimal VergiHaricTutar { get; set; }

        [Display(Name = "Fatura Ref. Numarası")]
        public string FaturaRefNumarasi { get; set; } // T0.NumAtCard
    }


   
}