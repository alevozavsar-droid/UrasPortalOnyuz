using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace WebApplication3.Models
{
    public class StockMovementAnalysisViewModel
    {
        public string StokKodu { get; set; }
        public string StokAdi { get; set; }
        public decimal? GuncelStokMiktari { get; set; }
        public decimal? Son30GunToplamCikisMiktari { get; set; }
        public decimal? Son60GunToplamCikisMiktari { get; set; }
        public decimal? Son90GunToplamCikisMiktari { get; set; }
        public decimal? Son30GunGunlukCikisOrt { get; set; }
        public decimal? Son60GunGunlukCikisOrt { get; set; }
        public decimal? Son90GunGunlukCikisOrt { get; set; }
        public decimal? KalanStokGun30GunOrt { get; set; }
        public decimal? KalanStokGun60GunOrt { get; set; }
        public decimal? KalanStokGun90GunOrt { get; set; }
    }



}