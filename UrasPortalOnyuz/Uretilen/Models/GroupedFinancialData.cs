using System;
using System.Globalization;
using System.Linq;

namespace WebApplication3.Models
{






    public class GroupedFinancialData
    {
        public string Grup { get; set; }


        public decimal UrsMakineX { get; set; }
        public decimal UrsMakineR { get; set; }

        public decimal AvrupaPaperX { get; set; }
        public decimal AvrupaPaperR { get; set; }

        public decimal AlvKimyaX { get; set; }
        public decimal AlvKimyaR { get; set; }

        public decimal AlvFiloX { get; set; }
        public decimal AlvFiloR { get; set; }

        public decimal RgbTekstilX { get; set; }
        public decimal RgbTekstilR { get; set; }

        public decimal UrasKimyaX { get; set; }
        public decimal UrasKimyaR { get; set; }

        public decimal SelviX { get; set; }
        public decimal SelviR { get; set; }

        public decimal AsiaDMX { get; set; }
        public decimal AsiaDMR { get; set; }

        public decimal AvrasyaX { get; set; }
        public decimal AvrasyaR { get; set; }

        public decimal DafKimyaX { get; set; }
        public decimal DafKimyaR { get; set; }


        public decimal UrsHoldingX { get; set; }
        public decimal UrsHoldingR { get; set; }
        public decimal PowerX { get; set; }
        public decimal PowerR { get; set; }



        public decimal RowTotal => UrsMakineX + UrsMakineR + AvrupaPaperX + AvrupaPaperR + AlvKimyaX + AlvKimyaR +
                                   AlvFiloX + AlvFiloR + RgbTekstilX + RgbTekstilR + UrasKimyaX + UrasKimyaR +
                                   SelviX + SelviR + AsiaDMX + AsiaDMR + AvrasyaX + AvrasyaR + DafKimyaX + DafKimyaR +
                                   UrsHoldingX + UrsHoldingR + PowerX + PowerR;


        public string DisplayRowTotal => RowTotal.ToString("N2", CultureInfo.CurrentCulture);
    }




 






    public class InterCompanyReportData
    {
        public string Durum { get; set; }
        public DateTime Tarih { get; set; }
        public string IslemTipi { get; set; }
        public string HesapAdi1 { get; set; }
        public string HesapKodu1 { get; set; }
        public int Yevmiye1 { get; set; }
        public decimal Tutar1 { get; set; }
        public string HesapAdi2 { get; set; }
        public string HesapKodu2 { get; set; }
        public int Yevmiye2 { get; set; }
        public decimal Tutar2 { get; set; }
        public decimal Fark { get; set; }

        public string DisplayTutar1 => Tutar1.ToString("N2", CultureInfo.CurrentCulture);
        public string DisplayTutar2 => Tutar2.ToString("N2", CultureInfo.CurrentCulture);
        public string DisplayFark => Fark.ToString("N2", CultureInfo.CurrentCulture);
    }




    public class ComparisonSummary
    {
        public int MatchedCount { get; set; }
        public int MissingInCompany1 { get; set; }
        public int MissingInCompany2 { get; set; }
        public decimal NetDifference { get; set; }


        public string StatusSummary => NetDifference == 0 ? "Tam Mutabakat" : $"Net Fark: {NetDifference.ToString("N2", CultureInfo.CurrentCulture)}";
    }
}