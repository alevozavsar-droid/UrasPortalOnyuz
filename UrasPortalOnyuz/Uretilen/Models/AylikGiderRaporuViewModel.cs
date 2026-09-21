// AylikGiderRaporuViewModel.cs

namespace WebApplication3.Models
{
    public class AylikGiderRaporuViewModel
    {
        public string Kategori { get; set; }
        public string Tur { get; set; }
        public string Descr { get; set; }


        public decimal Ocak { get; set; }
        public decimal Şubat { get; set; }
        public decimal Mart { get; set; }
        public decimal Nisan { get; set; }
        public decimal Mayıs { get; set; }
        public decimal Haziran { get; set; }
        public decimal Temmuz { get; set; }
        public decimal Ağustos { get; set; }
        public decimal Eylül { get; set; }
        public decimal Ekim { get; set; }
        public decimal Kasım { get; set; }
        public decimal Aralık { get; set; }


        public decimal YilToplami => Ocak + Şubat + Mart + Nisan + Mayıs + Haziran + Temmuz + Ağustos + Eylül + Ekim + Kasım + Aralık;


        public string SortCategory { get; set; }
        public int GroupingTUR { get; set; }
    }


 
}