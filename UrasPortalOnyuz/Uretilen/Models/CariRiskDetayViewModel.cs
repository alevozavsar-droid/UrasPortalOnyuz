using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{

    public class CariRiskRaporuViewModel
    {

        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }       // C=Müşteri, S=Tedarikçi (SAP OCRD)
        public string CariTipi { get; set; }        // Birleşik satır için: Müşteri / Tedarikçi / Karma
        public string SatisSorumlusu { get; set; }


        public decimal SeciliSelviBakiye { get; set; }
        public string SelviBaslik { get; set; }
        public decimal GenelToplamBakiye { get; set; }




        public DateTime? SonTahsilatTarihi { get; set; }

        public decimal Fifo_X_Acik { get; set; }
        public decimal Fifo_X_Gecmis { get; set; }
        public int Fifo_X_Adat { get; set; }
        public decimal Fifo_X_AdatPayi { get; set; } // Hesaplama için ara değer


        public decimal Fifo_R_Acik { get; set; }
        public int GroupNum { get; set; }
        public decimal Fifo_R_Gecmis { get; set; }
        public int Fifo_R_Adat { get; set; }
        public decimal Fifo_R_AdatPayi { get; set; } // Hesaplama için ara değer






        public decimal Raw_AdatPayi_Local_X { get; set; }
        public decimal Raw_Risk_Local_X { get; set; }
        public decimal Raw_AdatPayi_Local_R { get; set; }
        public decimal Raw_Risk_Local_R { get; set; }
        public decimal Raw_AdatPayi_Selvi_Avrupa { get; set; }
        public decimal Raw_Risk_Selvi_Avrupa { get; set; }
        public decimal Raw_AdatPayi_Selvi_Uras { get; set; }
        public decimal Raw_Risk_Selvi_Uras { get; set; }
        public decimal Raw_AdatPayi_Selvi_Diger { get; set; }
        public decimal Raw_Risk_Selvi_Diger { get; set; }


        public int AdatX { get; set; }
        public int AdatR { get; set; }


        public decimal AcikHesap_X { get; set; }
        public decimal VadesiGecmis_X { get; set; }
        public decimal PortfoyCek_X { get; set; }
        public decimal IbrazCek_X { get; set; }
        public decimal ToplamBakiye_X { get; set; } // Muhasebe hesaplarının toplamı


        public decimal BakiyeToplamGercek { get; set; }

        public decimal BakiyeDiger { get; set; }


        public decimal Bakiye120_X { get; set; }
        public decimal Bakiye136_X { get; set; }
        public decimal Bakiye159_X { get; set; }
        public decimal Bakiye320_X { get; set; }
        public decimal Bakiye336_X { get; set; }
        public decimal Bakiye340_X { get; set; }
        public decimal Bakiye420_X { get; set; }
        public decimal Bakiye440_X { get; set; }


        public decimal AcikHesap_R { get; set; }
        public decimal VadesiGecmis_R { get; set; }
        public decimal PortfoyCek_R { get; set; }
        public decimal IbrazCek_R { get; set; }
        public decimal ToplamBakiye_R { get; set; } // Muhasebe hesaplarının toplamı


        public decimal Bakiye120_R { get; set; }
        public decimal Bakiye136_R { get; set; }
        public decimal Bakiye159_R { get; set; }
        public decimal Bakiye320_R { get; set; }
        public decimal Bakiye336_R { get; set; }
        public decimal Bakiye340_R { get; set; }
        public decimal Bakiye420_R { get; set; }
        public decimal Bakiye440_R { get; set; }


        public DateTime? EnEskiVade { get; set; }
        public int GecikmeGun { get; set; }
    }


    public class CariRiskDetayViewModel
    {
        public List<RiskSatir> Satirlar { get; set; }
        public RiskOzet Ozet { get; set; }

        public CariRiskDetayViewModel()
        {
            Satirlar = new List<RiskSatir>();
            Ozet = new RiskOzet();
        }
    }


    public class RiskSatir
    {
        public string OzelKod { get; set; }
        public string Yon { get; set; }
        public decimal Tutar { get; set; }
        public string VadeTarihi { get; set; }
        public int GecikmeGun { get; set; }
    }

    public class RiskOzet
    {

        public decimal X_AcikBakiye { get; set; }
        public decimal X_VadesiGecmis { get; set; }
        public int X_AdatGun { get; set; }


        public decimal R_AcikBakiye { get; set; }
        public decimal R_VadesiGecmis { get; set; }
        public int R_AdatGun { get; set; }

        public decimal GenelToplam { get; set; }
    }
}