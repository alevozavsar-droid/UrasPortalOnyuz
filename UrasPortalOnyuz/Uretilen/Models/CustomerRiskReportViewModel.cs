using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class CustomerRiskReportViewModel
    {


        public string Veritabani { get; set; }
        public string MusteriKodu { get; set; }
        public string MusteriFirmaAdi { get; set; }


        public decimal? RBakiye { get; set; }
        public decimal? XBakiye { get; set; }


        public int? RYaslananBakiyeGecikmeGunSayisi { get; set; }
        public decimal? RYaslananBakiye { get; set; }
        public int? XYaslananBakiyeGecikmeGunSayisi { get; set; }
        public decimal? XYaslananBakiye { get; set; }


        public decimal? Hesap420Bakiyesi { get; set; }   // SQL: '420 HESAP BAKİYESİ'
        public decimal? GenelBakiyeFarki { get; set; }   // SQL: 'GENEL BAKİYE FARKI'


        public string SatisSorumlusu { get; set; }
        public string MusteriGrubu { get; set; }
        public decimal? SonAyFaturaLimiti { get; set; } // LIMIT_FATURA * Ay
    }




    /*
    public class FilterModel
    {
        public string Field { get; set; }
        public string Value { get; set; }
    }
    public class SortModel
    {
        public string Field { get; set; }
        public string Direction { get; set; } // "asc" or "desc"
    }
    */

    public class ExportRequestModel1
    {
        public List<FilterModel> Filters { get; set; }
        public SortModel Sort { get; set; }


        public int MonthFilter { get; set; }
    }
}