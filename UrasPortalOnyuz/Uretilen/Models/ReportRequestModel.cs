using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class ReportRequestModel
    {
        public List<Filter> Filters { get; set; }
        public Sort Sort { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 250;
    }





    public class FinancialReportData
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
        public decimal YillikOrtalama { get; set; }




    }

  
    public class ReportLineData
    {

        public string Aciklama { get; set; } = string.Empty;


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
        public decimal YillikOrtalama { get; set; }


        public string ToplamDikeyAnaliz { get; set; } = "N/A";


        public int? Sira { get; set; }
    }
    public class ReportViewModel
    {
        public List<ReportLineData> CiroRaporu { get; set; } = new List<ReportLineData>();
        public List<ReportLineData> NakitTahsilatRaporu { get; set; } = new List<ReportLineData>();
        public List<ReportLineData> BankaTahsilatRaporu { get; set; } = new List<ReportLineData>();
        public List<ReportLineData> CekTahsilatRaporu { get; set; } = new List<ReportLineData>();
    }

    public class Filter
    {
        public int ColumnIndex { get; set; }
        public List<string> Values { get; set; }
    }

    public class Sort
    {
        public int ColumnIndex { get; set; }
        public string Direction { get; set; } // 'asc', 'desc', 'none'
    }
}