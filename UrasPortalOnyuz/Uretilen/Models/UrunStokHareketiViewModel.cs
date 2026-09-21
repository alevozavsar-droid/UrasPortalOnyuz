using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace WebApplication3.Models
{
    public class UrunStokHareketiViewModel
    {
       
        public string UrunKodu { get; set; }

       
        public string UrunIsmi { get; set; }

   
        public DateTime? Tarih { get; set; }


        public string BelgeNo { get; set; }

      
        public string Aciklama { get; set; }

      
        public string HareketTipi { get; set; }


        public decimal? GirenMiktar { get; set; }

       
        public decimal? GirisFiyati { get; set; }

        public string GirisParaBirimi { get; set; }

      
 
        public decimal? GirisTutari { get; set; }

      
        public decimal? CikanMiktar { get; set; }


        public decimal? CikisFiyati { get; set; }


        public string CikisParaBirimi { get; set; }


        public decimal? CikisTutari { get; set; }


        public decimal? KumulatifStok { get; set; }

   
        public decimal? SatisMiktari { get; set; }


        public decimal? FaturaMiktari { get; set; }


        public string SaticiKodu { get; set; }

        public string SaticiIsmi { get; set; }


        public decimal? IthalatMaliyetliBirimFiyati { get; set; }


        public decimal? IthalatMiktari { get; set; }


        public decimal? IthalatMaliyetliToplamTutar { get; set; }

        public decimal? GuncelStokMiktari { get; set; }
    }



    public class UrunViewModel
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
    }
}