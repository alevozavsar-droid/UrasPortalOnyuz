using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class InvoiceDetailViewModel
    {
        public int FaturaBelgeNumarasi { get; set; }
        public int FaturaNumarasi { get; set; }
        public string FaturaTarihi { get; set; }
        public string MusteriKodu { get; set; }
        public string MusteriAdi { get; set; }
        public string UrunKodu { get; set; }
        public string UrunAciklamasi { get; set; }

        public decimal? SatilanMiktar { get; set; }
        public decimal? BirimSatisFiyatiOrijinalPB_VergiDahil { get; set; }
        public decimal? BirimSatisFiyatiOrijinalPB_VergiHaric { get; set; }
        public string BelgeParaBirimi { get; set; }
        public decimal? BirimSatisFiyatiTL_VergiHaric { get; set; }
        public decimal? VergiDahilBirimFiyatTL { get; set; }
        public decimal? KdvYuzdesi { get; set; }


        public decimal? UrunMaliyetiTL_BOM { get; set; }
        public decimal UrunMaliyetiTL_ALV { get; set; }
        public decimal? BirimKarZararTL { get; set; }
        public decimal? ToplamKarZararTL { get; set; }
        public decimal? KarZararYuzdesi { get; set; }

        public decimal? UrunMaliyetiUSD { get; set; }
        public string AnaUrunMaliyetKaynagi { get; set; }


        public List<ProductCostDetailViewModel> ProductCostDetails { get; set; } = new List<ProductCostDetailViewModel>();


        public string AltKalemKodu { get; set; }
        public string AltKalemAdi { get; set; }
        public decimal? AltKalemMiktari_BOMaGore { get; set; }
        public decimal? AltKalemBirimMaliyetTL { get; set; }
        public decimal? AltKalemToplamMaliyetTL { get; set; }
        public string AltKalemMaliyetKaynagi { get; set; }
        public string AltKalemMaliyetFaturaNo { get; set; }
    }

    public class ProductCostDetailViewModel
    {
        public string UrunAgacKodu { get; set; }
        public string UrunAgacAdi { get; set; }
        public string KalemKodu { get; set; }
        public string KalemAdi { get; set; }


        public string Miktar { get; set; }
        public string BirimMaliyet { get; set; } // Hata buradaydı, string yapıldı
        public string ParaBirimi { get; set; }
        public string MaliyetKaynagi { get; set; }
        public string FaturaNumarasi { get; set; }

        public string BirimMaliyetUSD { get; set; }
        public string ToplamMaliyetUSD { get; set; }


        public string BirimMaliyetALV { get; set; }
        public string ParaBirimiALV { get; set; }
        public string MaliyetKaynagiALV { get; set; }
        public string BirimMaliyetALV_USD { get; set; }
        public string ToplamMaliyetALV_USD { get; set; }
        public string ToplamMaliyetALV_TL { get; set; }

        public string StokAdiMevcutDB { get; set; }
        public string AlvUrunAgaciBulunduMu { get; set; }

        public int? Level { get; set; }
        public bool IsBom { get; set; }
        public int SiraNo { get; set; }
    }
}