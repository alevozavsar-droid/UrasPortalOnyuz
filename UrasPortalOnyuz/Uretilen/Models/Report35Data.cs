using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebApplication3.Models
{
    public class Report35Data
    {

        public string IslemTipi { get; set; }
        public decimal Ocak { get; set; }
        public decimal Subat { get; set; }
        public decimal Mart { get; set; }
        public decimal Nisan { get; set; }
        public decimal Mayis { get; set; }
        public decimal Haziran { get; set; }
        public decimal Temmuz { get; set; }
        public decimal Agustos { get; set; }
        public decimal Eylul { get; set; }
        public decimal Ekim { get; set; }
        public decimal Kasim { get; set; }
        public decimal Aralik { get; set; }
        public decimal Toplam { get; set; }



        [Column("2025 AYLIK ORTALAMA")]
        public decimal YillikOrtalama { get; set; }


        public decimal Sira { get; set; }
    }
    public class TahsilatData
    {
        public string IslemTipi { get; set; }

        public decimal Ocak { get; set; }
        public decimal Subat { get; set; }
        public decimal Mart { get; set; }
        public decimal Nisan { get; set; }
        public decimal Mayis { get; set; }
        public int RowType { get; set; }
        public decimal Haziran { get; set; }
        public decimal Temmuz { get; set; }
        public decimal Agustos { get; set; }
        public decimal Eylul { get; set; }
        public decimal Ekim { get; set; }
        public decimal Kasim { get; set; }
        public decimal Aralik { get; set; }

        public decimal Toplam { get; set; }


        public decimal YillikOrtalama { get; set; }


        public decimal Sira { get; set; }


        public string RaporTuru { get; set; }
    }
}