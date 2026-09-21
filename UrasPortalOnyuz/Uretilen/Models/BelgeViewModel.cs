using System;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class BelgeViewModel
    {
        public int? Hafta { get; set; }
        public string? TarihAraligi { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd.MM.yy}", ApplyFormatInEditMode = true)]

        public string? BelgeTarihiAraligi { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd.MM.yy}", ApplyFormatInEditMode = true)]
        public DateTime? OrderDate { get; set; }
        public string? TedarikciAlacakli { get; set; }
        public string? DocNum { get; set; }
        public string DosyaYolu { get; set; } // Yeni alan
        public string? FaturaNo { get; set; }
        public int? TotalRecords { get; set; }
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? OdeplanTarihi { get; set; }
        public string? OdeplanNot { get; set; }
        public string? OdemeNedeni { get; set; }
        public string? MutabakatDurumu { get; set; }

        [DisplayFormat(DataFormatString = "{0:dd.MM.yy}", ApplyFormatInEditMode = true)]
        public DateTime? VadeTarihi { get; set; }
        [DisplayFormat(DataFormatString = "{0:dd.MM.yy}", ApplyFormatInEditMode = true)]
        public DateTime? GerceklesenOdemeTarihi { get; set; }
        public int? GunFarki { get; set; }

        public decimal? TutarRawInKurus { get; set; }
        public decimal? OdenenTutarRawInKurus { get; set; }

        public decimal? KalanTutar  { get; set; }

        public string? Tutar { get; set; }
        public string? OdenenTutar { get; set; }

        public string? ParaBirimi { get; set; }
        public string? OdemeSekli { get; set; }
        public decimal? GuncelTLTutar { get; set; }
        public string? Durum { get; set; }
        public string? Belge { get; set; }
        public int? U_BE1_SIRA { get; set; }
        public decimal? U_BE1_YUZDE { get; set; }
        public decimal? U_BE1_TUTAR { get; set; }
        public string? ObjType { get; set; }
        public string? Sahip { get; set; }
        public string? CardCode { get; set; }
        public int? DocEntry { get; set; }
    }

    public class RaporViewModel
    {
        public List<BelgeViewModel>? Belgeler { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalRecords { get; set; }
        public int TotalPages { get; set; }
        public string? SortColumn { get; set; }
        public string? SortDirection { get; set; }
        public Dictionary<string, List<string>> Filters { get; set; } = new Dictionary<string, List<string>>();
        public Dictionary<string, List<string>> FilterOptions { get; set; } = new Dictionary<string, List<string>>();
    }

    public class PaymentInstructionRequest
    {
        public string? CompanyIban { get; set; }
        public string? CompanyBankName { get; set; }
        public string? CompanyBranchName { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? CompanyAccountNumber { get; set; }
        public string? Notes { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? CompanyCurrency { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierBankName { get; set; }
        public string? SupplierIban { get; set; }


        public List<CekViewModel>? CheckPayments { get; set; }

        public string? InstructionType { get; set; }


        public List<PaymentDetail>? PaymentDetails { get; set; }

        public SupplierPaymentDetail? SupplierPayment { get; set; }


        public List<PersonalManualPaymentDetail>? PersonalManualPayments { get; set; }
     
        public List<SupplierPaymentDetail>? SupplierPayments { get; set; }


        public List<string> PaymentMethods { get; set; }
        public decimal? NakitAmount { get; set; }
        public List<CekViewModel> Checks { get; set; }
    }

    public class PaymentDetail
    {
        public string? PaymentMethod { get; set; }
        public decimal? Amount { get; set; }
        public string? Description { get; set; }
        public string? SupplierName { get; set; }
        public string? SupplierCardCode { get; set; }
        public string? SupplierIban { get; set; }
        public string? BankName { get; set; }
        public string? ReceiverName { get; set; }
        public string? ReceiverIdNumber { get; set; }
        public List<DocumentDetail>? Documents { get; set; }
        public List<CheckDetail>? Checks { get; set; }
    }

    public class PersonalManualPaymentDetail
    {
        public string? ReceiverName { get; set; }
        public string? ReceiverIdNumber { get; set; }

        [Required(ErrorMessage = "Banka seçimi zorunludur.")]
        public string? BankName { get; set; }

        [Required(ErrorMessage = "IBAN zorunludur.")]
        public string? Iban { get; set; }

        [Required(ErrorMessage = "Tutar girişi zorunludur.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Geçerli bir tutar giriniz.")]
        public decimal? Amount { get; set; }

        [Required(ErrorMessage = "Açıklama girişi zorunludur.")]
        public string? Description { get; set; }
    }

    public class CheckDetail
    {
        public string? CekNumarasi { get; set; }
        public string? BankaAdi { get; set; }
        public string? CiroEden { get; set; }
        public string? AsilBorclu { get; set; }
        public decimal? Tutar { get; set; }
        public string? ParaBirimi { get; set; }
        public string DosyaYolu { get; set; } // Yeni alan
        public DateTime? VadeTarihi { get; set; }
    }




    public class SupplierPaymentDetail
    {
        public string? SupplierName { get; set; }
        public string? SupplierCardCode { get; set; }
        public string? SupplierIban { get; set; }
        public string? BankName { get; set; }
        public string? PaymentMethod { get; set; }
        public string DosyaYolu { get; set; } // Yeni alan

        public List<CheckDetail>? Checks { get; set; }
        public string? Description { get; set; }
        public decimal? Amount { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<DocumentDetail>? Documents { get; set; }
    }
    public class OdemeViewModel
    {
        [Key] // Bu satır, bu özelliğin birincil anahtar olduğunu belirtir.

        public int DocEntry { get; set; }
        public string DosyaYolu { get; set; } // Yeni alan
        public string DocNum { get; set; }
        public int Period { get; set; }
        public int Instance { get; set; }
        public int Series { get; set; }
        public string Handwrtten { get; set; }
        public string Canceled { get; set; }
        public int Object { get; set; }
        public int LogInst { get; set; }
        public int UserSign { get; set; }

        public string Transfered { get; set; }
        public string Status { get; set; }
        public DateTime CreateDate { get; set; }
        public int CreateTime { get; set; }
        public DateTime UpdateDate { get; set; }
        public int UpdateTime { get; set; }
        public string DataSource { get; set; }
        public string RequestStatus { get; set; }
        public int Creator { get; set; }
        public string Remark { get; set; }
        public string U_Id { get; set; }
        public int U_Hafta { get; set; }
        public string U_TarihAraligi { get; set; }
        [Required(ErrorMessage = "Belge Tarihi zorunludur.")]
        public DateTime? U_OrderDate { get; set; }
        public string U_TedarikciAlacakli { get; set; }
        public string U_DocNum { get; set; }
        public string U_FaturaNo { get; set; }
        [Required(ErrorMessage = "Ödeme Planı Tarihi zorunludur.")]
        public DateTime? U_OdeplanTarihi { get; set; }
        public string U_OdeplanNot { get; set; }
        public decimal U_OdemeYuzdesi { get; set; }
        public decimal U_OdenecekTutar { get; set; }
        public string U_OdemeNedeni { get; set; }
        public string U_MutabakatDurumu { get; set; }
        public string U_ApprovalStatus { get; set; }

        public DateTime? U_VadeTarihi { get; set; }
        public DateTime? U_GerceklesenOdemeTarihi { get; set; }
        public int U_GunFarki { get; set; }
        [Required(ErrorMessage = "Tutar zorunludur.")]
        public decimal U_Tutar { get; set; }
        public decimal U_OdenenTutar { get; set; }
        [Required(ErrorMessage = "Para Birimi zorunludur.")]
        public string U_ParaBirimi { get; set; }
        public string U_OdemeSekli { get; set; }
        public decimal U_GuncelTLTutar { get; set; }
        public string U_Belge { get; set; }
        public int? U_U_BE1_SIRA { get; set; }
        public string U_ObjType { get; set; }
        public string U_Sahip { get; set; }
        [Required(ErrorMessage = "Tedarikçi Kodu zorunludur.")]
        public string U_CardCode { get; set; }
    }
    public class DocumentDetail
    {
        public string? DocNum { get; set; }
        public string? ObjType { get; set; }
        public string? FaturaNo { get; set; }
        public decimal? GuncelTLTutar { get; set; }
        public string? BelgeTipi { get; set; }
        public int? DocEntry { get; set; }
        public string? Notes { get; set; }
        public decimal? TalimatTutari { get; set; }
    }
    public class SiralamaViewModel
    {
        public int? DocEntry { get; set; }
        public string ObjType { get; set; }
        public int? U_BE1_SIRA { get; set; }
        public int? U_U_BE1_SIRA { get; set; }
        public string DocNum { get; set; } // Bu satırı ekleyin
    }
    public class SupplierBankInfo
    {
        public string? BankName { get; set; }
        public string? Iban { get; set; }
    }

    public class CompanyBankInfo
    {
        public string? BankName { get; set; }
        public string? Iban { get; set; }
        public string? BranchName { get; set; }
        public string? AccountNumber { get; set; }
        public string? ParaBirimi { get; set; }
    }

    public class Supplier
    {
        public string? CardCode { get; set; }
        public string? CardName { get; set; }
        public string? BankName { get; set; }
        public string? Iban { get; set; }
    }
}