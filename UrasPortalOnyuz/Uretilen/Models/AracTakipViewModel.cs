using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // Bu using gerekli

namespace WebApplication3.Models
{




    [Table("TBL_ARACLAR")] // EF Core'a SQL'deki gerçek tablo adını söyler
    public class Arac
    {
        [Key]
        public int AracID { get; set; }

        [Required(ErrorMessage = "Plaka zorunludur.")]
        [StringLength(15)]
        public string Plaka { get; set; }

        [Required(ErrorMessage = "Marka zorunludur.")]
        [StringLength(50)]
        public string Marka { get; set; }

        [Required(ErrorMessage = "Model zorunludur.")]
        [StringLength(50)]
        public string Model { get; set; }

        [Display(Name = "Model Yılı")]
        [Range(1900, 2100)]
        public int ModelYili { get; set; }

        [Required(ErrorMessage = "Şasi No zorunludur.")]
        [StringLength(50)]
        public string SaseNo { get; set; }

        [Display(Name = "Aktif Mi?")]
        public bool AktifMi { get; set; } = true;

        [Display(Name = "Araç Tipi")]
        [StringLength(30)]
        public string AracTipi { get; set; }
    }

    [Table("TBL_PERSONEL")] // EF Core'a SQL'deki gerçek tablo adını söyler
    public class Personel
    {
        [Key]
        public int PersonelID { get; set; }

        [Required(ErrorMessage = "Ad Soyad zorunludur.")]
        [Display(Name = "Ad Soyad")]
        [StringLength(100)]
        public string AdSoyad { get; set; }

        [Display(Name = "Sicil No")]
        [StringLength(20)]
        public string SicilNo { get; set; }

        [Required(ErrorMessage = "Departman zorunludur.")]
        [StringLength(50)]
        public string Departman { get; set; }

        [Display(Name = "Aktif Çalışan")]
        public bool AktifCalisanMi { get; set; } = true;
    }

    [Table("TBL_ARAC_ATAMA")] // EF Core'a SQL'deki gerçek tablo adını söyler
    public class AracAtama
    {
        [Key]
        public int AtamaID { get; set; }

        public int AracID { get; set; }
        public int PersonelID { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime BaslangicTarihi { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Bitiş Tarihi")]
        public DateTime? BitisTarihi { get; set; }

        [Display(Name = "Başlangıç KM")]
        [Range(0, 9999999)]
        public int BaslangicKM { get; set; }

        [Display(Name = "Bitiş KM")]
        [Range(0, 9999999)]
        public int? BitisKM { get; set; }

        [Required(ErrorMessage = "Açıklama/Amaç zorunludur.")]
        [Display(Name = "Atama Amacı")]
        [StringLength(255)]
        public string Aciklama { get; set; }


        public virtual Arac Arac { get; set; }
        public virtual Personel Personel { get; set; }
    }






    public class AracTakipViewModel
    {
        public string Plaka { get; set; }
        public string Marka { get; set; }
        public string Model { get; set; }
        public string AtananPersonel { get; set; }
        public string Departman { get; set; }
        public DateTime BaslangicTarihi { get; set; }
        public int BaslangicKM { get; set; }
        public string AtamaAmaci { get; set; }
    }


    public class YeniAtamaViewModel
    {
        public IEnumerable<SelectListItem> AracSecenekleri { get; set; }
        public IEnumerable<SelectListItem> PersonelSecenekleri { get; set; }

        [Required(ErrorMessage = "Araç seçimi zorunludur.")]
        [Display(Name = "Seçilen Araç")]
        public int SecilenAracID { get; set; }

        [Required(ErrorMessage = "Personel seçimi zorunludur.")]
        [Display(Name = "Atanacak Personel")]
        public int SecilenPersonelID { get; set; }

        [Required(ErrorMessage = "Başlangıç tarihi zorunludur.")]
        [DataType(DataType.DateTime)]
        [Display(Name = "Başlangıç Tarihi")]
        public DateTime BaslangicTarihi { get; set; } = DateTime.Now;

        [Required(ErrorMessage = "Başlangıç KM zorunludur.")]
        [Display(Name = "Başlangıç KM")]
        [Range(0, 9999999, ErrorMessage = "Geçerli bir KM girin.")]
        public int BaslangicKM { get; set; }

        [Required(ErrorMessage = "Atama amacı zorunludur.")]
        [Display(Name = "Atama Amacı")]
        [MaxLength(255)]
        public string Aciklama { get; set; }
    }
}