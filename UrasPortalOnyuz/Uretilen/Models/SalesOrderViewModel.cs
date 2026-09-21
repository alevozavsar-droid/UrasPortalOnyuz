using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class SalesOrderViewModel
    {
        public decimal? ToplamMaliyetStdTL { get; set; }
        public decimal? ToplamKarZararStdTL { get; set; }
        public decimal? KarZararStdYuzdesi { get; set; }

        public decimal? ToplamMaliyetAlvTL { get; set; }
        public decimal? ToplamKarZararAlvTL { get; set; }
        public decimal? KarZararAlvYuzdesi { get; set; }
        public int DocEntry { get; set; }
        public int BelgeNumarasi { get; set; }
        public string BelgeTarihi { get; set; }
        public string BelgeAyi { get; set; }
        public string BelgeAyYil { get; set; }

        public string HesaplananVadeAyYil { get; set; }

        public string HesaplananVadeTarihi { get; set; }
        public string MusteriAdi { get; set; }
        public decimal? SiparisBelgeTutari { get; set; }
        public string BelgeParaBirimi { get; set; }
        public string SatisTemsilcisi { get; set; }
        public string OdemeKosuluGrubu { get; set; }
        public decimal? SiparisTLTutariGuncelKur { get; set; }
        public decimal? ToplamFaturaTutariFaturaPB { get; set; }
        public decimal? ToplamFaturaTutariTL { get; set; }
        public decimal? ToplamOdenenTutarFaturaPB { get; set; }
        public decimal? ToplamOdenenTutarTL { get; set; }
        public decimal? KalanTutarFaturaPB { get; set; }
        public decimal? KalanTutarTL { get; set; }
        public decimal? MusteriGuncelBakiyesi { get; set; }
        public decimal? VadesiGecmisFaturaTutariTL { get; set; }
        public decimal? ToplamMaliyetTL { get; set; }
        public decimal? ToplamKarZararTL { get; set; }
        public decimal? KarZararYuzdesi { get; set; }

        public decimal? ToplamFaturaTutariFaturaPB_KDVHaric { get; set; }
        public decimal? ToplamFaturaTutariTL_KDVHaric { get; set; }


        public int? SlpCode { get; set; }
    }
    public class SalesReportViewModel
    {
        public DateTime FaturaTarihi { get; set; }
        public string FaturaAyi { get; set; }
        public string FaturaNo { get; set; }
        public string MusteriKodu { get; set; }
        public string MusteriAdi { get; set; }
        public string SatisParaBirimi { get; set; }
        public decimal SatisTutariPB { get; set; }
        public decimal SatisTutariTL { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal ToplamKarZarar { get; set; }
        public string Durum { get; set; }
        public decimal? KarZararYuzdesi { get; set; }
    }
    public class SaleDetailsViewModel
    {
        public DateTime FaturaTarihi { get; set; }
        public string FaturaNo { get; set; }
        public string SatisParaBirimi { get; set; }
        public string MalzemeKodu { get; set; }
        public string MalzemeAciklamasi { get; set; }
        public decimal Miktar { get; set; }
        public decimal SatisFiyatiPB { get; set; }
        public decimal? SatisBelgeKuru { get; set; } // Yeni eklendi
        public decimal? SatisUSDKuru { get; set; } // Yeni eklendi
        public decimal? SatisFiyatiTL { get; set; } // Olası null değer için ? eklendi
        public decimal? SatisFiyatiUSD { get; set; } // Yeni eklendi
        public decimal? SatisTutariTL { get; set; } // Olası null değer için ? eklendi
        public decimal? SatisTutariUSD { get; set; } // Yeni eklendi
        public decimal? SonSatinAlmaFiyati { get; set; }
        public string SonSatinAlmaParaBirimi { get; set; }
        public decimal? AlisBelgeKuru { get; set; } // Yeni eklendi
        public decimal? AlisUSDKuru { get; set; } // Yeni eklendi
        public decimal? AlisFiyatiTL { get; set; } // Yeni eklendi
        public decimal? AlisFiyatiUSD { get; set; } // Yeni eklendi
        public decimal? ToplamMaliyet { get; set; } // Olası null değer için ? eklendi
        public decimal? BirimKarZarar { get; set; } // Olası null değer için ? eklendi
        public decimal? ToplamKarZarar { get; set; } // Olası null değer için ? eklendi
        public decimal? ToplamKarZararUSD { get; set; } // Yeni eklendi
        public string Durum { get; set; }
        public decimal? KarZararYuzdesi { get; set; }
        public decimal? KarZararYuzdesiUSD { get; set; } // Yeni eklendi
        public DateTime? SonSatinAlmaTarihi { get; set; }
        public string SonSatinAlmaBelgeNo { get; set; }
        public string SonTedarikci { get; set; }
    }
    public class SalesPerson
    {
        public int? SlpCode { get; set; }
        public string SlpName { get; set; }
    }

    public class UpdateSalesPersonModel
    {
        public int DocEntry { get; set; }
        public int NewSlpCode { get; set; }
    }

}