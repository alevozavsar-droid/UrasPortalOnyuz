// Dosya: WebApplication3\Models\OrderTrackingViewModel.cs

using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{



    public class OrderTrackingViewModel
    {

        public string SiparisAlanFirma { get; set; }
        public string FaturaKesilenFirma { get; set; }
        public int? SiparisNo { get; set; }


        public decimal? FaturaTutari { get; set; }


        public decimal? TahsilatTutari { get; set; }
        public decimal? KalanBakiyeYuzde { get; set; }

        public int? TeklifNo { get; set; }

        public int? FaturaNo { get; set; } // RAPOR İÇİN GEREKLİ ALAN

        public string MusteriAdi { get; set; }
        public string Ulke { get; set; }
        public DateTime? SiparisTarihi { get; set; }


        public DateTime? OnOdemeTarihi { get; set; }
        public DateTime? PlanlananTeslimTarihi { get; set; }
        public string PlanlananHafta { get; set; }
        public int? SiparisTeslimatSuresi { get; set; }
        public string SatisSorumlusu { get; set; }
        public string UrunKodu { get; set; }
        public string UrunModeli { get; set; }
        public string SiparisDurumu { get; set; }
        public DateTime? UretimBaslangicTarihi { get; set; }
        public DateTime? PlanlananBitisTarihi { get; set; }
        public DateTime? GerceklesenBitisTarihi { get; set; }
        public decimal? Stokta { get; set; }
        public decimal? Ihtiyac { get; set; }
        public string Durum { get; set; }
        public string NakliyePlani { get; set; }
        public DateTime? FabrikaCikisTarihi { get; set; }
        public string TeslimatDurumu { get; set; }


        public DateTime? AnlasilanBeyannameTarihi { get; set; }
        public DateTime? NavlunYuklemeTarihi { get; set; }
        public DateTime? TahminiMusteriTeslimatTarihi { get; set; }
        public DateTime? PlanlananKurulumTarihi { get; set; }
        public string Not { get; set; }
        public string IhracatDosyaNo { get; set; }
        public decimal? GuncelTLTutar { get; set; }
    }




    public class ProformaFaturaLine
    {
        public string ItemDescription { get; set; }
        public string ItemCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalLineAmount { get; set; }
        public string LineDescription { get; set; }
        public string SatirAciklama { get; set; }
        public decimal? SatirMiktar { get; set; }
    }




    public class ProformaFaturaViewModel
    {

        public int DocNum { get; set; }
        public DateTime DocDate { get; set; }
        public string DocCur { get; set; }


        public string SellerCompanyName { get; set; } = "URS MAKİNA SANAYİ VE TİCARET A.Ş.";
        public string SellerAddressLine1 { get; set; } = "KOSB İHSAN DEDE CAD. NO:6 ERTUN PLAZA";
        public string SellerCityCountry { get; set; } = "GEBZE / KOCAELİ / TÜRKİYE";
        public string SellerCityInvoice { get; set; } = "İSTANBUL / TURKIYE";


        public string CardName { get; set; }
        public string AddressName { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string ZipCode { get; set; }
        public string StateName { get; set; }
        public string CountryName { get; set; }
        public string CustomerNote { get; set; }


        public string Incoterms { get; set; }
        public string PaymentGroup { get; set; }
        public string PaymentCondition { get; set; }
        public DateTime? DispatchTimeStart { get; set; }
        public DateTime? DispatchTimeEnd { get; set; }
        public string SalesEmployee { get; set; }
        public string SalesEmployeeEmail { get; set; }

        public string ShipmentNote1 { get; set; }
        public string ShipmentNote2 { get; set; }


        public decimal TotalItemsAmount { get; set; }
        public decimal FreightCost { get; set; }
        public decimal TotalDocAmountWithFreight { get; set; }


        public string BankName { get; set; }
        public string Swift { get; set; }
        public string IBAN { get; set; }
        public string Branch { get; set; }


        public List<ProformaFaturaLine> Lines { get; set; } = new List<ProformaFaturaLine>();
    }




    public class PackingListLineViewModel
    {

        public string ItemCode { get; set; }
        public string VolumeNo { get; set; }
        public string DetailNo { get; set; }


        public string ItemDescription { get; set; }
        public decimal Quantity { get; set; }


        public decimal? NetAgirlik { get; set; }
        public decimal? BrutAgirlik { get; set; }
        public string OlculerCM { get; set; }
        public string SatirNotu { get; set; }


        public string LineType { get; set; } // U_LINETYPE
    }




    public class PackingListViewModel
    {

        public string IhracatDosyaNo { get; set; }
        public int FaturaNo { get; set; }
        public DateTime? Tarih { get; set; } = DateTime.Now;
        public string AnaAciklama { get; set; } // U_ANAACIKLAMA (Main Description)
        public string GenelNot { get; set; }    // U_NOT (Genel Not)


        public string GondericiAdi { get; set; }
        public string GondericiAdresi { get; set; } // U_SEVKADRESI
        public string AliciAdi { get; set; }
        public string AliciAdresi { get; set; }      // U_ALICIADRES


        public string Nakliyeci { get; set; }
        public string TruckID { get; set; }


        public decimal TotalNetAgirlik { get; set; }
        public decimal TotalBrutAgirlik { get; set; }
        public int TotalDemirPlatform { get; set; }

        public List<PackingListLineViewModel> Lines { get; set; } = new List<PackingListLineViewModel>();
    }
}