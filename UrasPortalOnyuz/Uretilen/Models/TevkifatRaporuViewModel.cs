using System;
using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class TevkifatRaporuViewModel
    {

        [Display(Name = "Fatura Tarihi")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime FaturaTarihi { get; set; }

        [Display(Name = "E-Fatura No")]
        public string EFaturaNo { get; set; }

        [Display(Name = "Vergi No")]
        public string VergiNo { get; set; }
        public string IslemTipi { get; set; }

        public string BelgeTipi { get; set; }
        [Display(Name = "Tedarikçi Adı")]
        public string TedarikciAdi { get; set; }

        [Display(Name = "Para Birimi")]
        public string ParaBirimi { get; set; }


        [Display(Name = "Satır No")]
        public int? SatirNo { get; set; } // Kalem satır numarası

        [Display(Name = "Kalem Kodu")]
        public string KalemKodu { get; set; } // Kalem/Hizmet kodu

        [Display(Name = "Kalem Açıklaması")]
        public string KalemAciklamasi { get; set; } // Kalem/Hizmet açıklaması



        [Display(Name = "Tevkifat Oranı")]
        [DisplayFormat(DataFormatString = "{0:P2}", ApplyFormatInEditMode = true)]
        public decimal TevkifatOrani { get; set; } // Örn: 0.10, 0.50

        [Display(Name = "WT Kodu")]
        public string WTKodu { get; set; }

        [Display(Name = "Tevkifat Türü Adı")]
        public string TevkifatTuruAdi { get; set; }





        [Display(Name = "Vergi Hariç Tutar")] // İsim sadeleştirildi
        public decimal ToplamVergiHaricTutar { get; set; } // Satırın KDV hariç tutarı

        [Display(Name = "KDV Tutarı")] // İsim sadeleştirildi
        public decimal ToplamKDVTutari { get; set; } // Satırın KDV tutarı

        [Display(Name = "Tevkifatlı Tutar")] // İsim sadeleştirildi
        public decimal ToplamTevkifatliTutar { get; set; } // Satırın Tevkif edilen KDV tutarı

        [Display(Name = "Tevkifatsız Tutar")] // İsim sadeleştirildi
        public decimal ToplamTevkifatsizTutar { get; set; } // Satırın Tevkifat sonrası ödenecek KDV tutarı

        [Display(Name = "Vergi Hariç Tutar (TL)")]
        public decimal BelgeBazliVergiHaricTutarTL { get; set; } // Satırın KDV hariç TL karşılığı

        [Display(Name = "KDV Tutarı (TL)")]
        public decimal BelgeBazliKDVTutariTL { get; set; } // Satırın KDV TL karşılığı
    }
}