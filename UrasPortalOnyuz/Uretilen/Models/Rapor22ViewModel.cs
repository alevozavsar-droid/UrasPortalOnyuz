using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication3.Models
{
    public class Rapor22ViewModel
    {
        public int DocEntry { get; set; }
        public string CekNumarasi { get; set; }
        public string CekKimdenGeldi { get; set; }
        public string AsilBorclu { get; set; }
        public decimal? Tutar { get; set; }
        public string ParaBirimi { get; set; }
        public DateTime? VadeTarihi { get; set; }
        public string VadeAyiVeYili { get; set; }
        public DateTime? PortfoyeGirisTarihi { get; set; }
        public DateTime? IbrazTarihi { get; set; }
        public int? TahsilatBelgeNo { get; set; }
        public string IbrazBelgeNo { get; set; }
        public string PlanlananTedarikci { get; set; }
        public string HareketDurumu { get; set; }
        public string BelgeTipi { get; set; }
        public string CekIslemTuru { get; set; }
        public string IslemTipiAdi { get; set; }
        public string CekKimeVerildi { get; set; }
        public decimal? TlKarsiligi { get; set; }
        public string SirketAdi { get; set; }
        public DateTime? SiralamaTarihi { get; set; }
        public string CekinBankasi { get; set; }
    }

  

    public class FilterData
    {
        public int ColumnIndex { get; set; }
        public string Type { get; set; }
        public List<string> Values { get; set; }
    }

    public class SortData
    {
        public int ColumnIndex { get; set; }
        public string SortType { get; set; }
        public string Direction { get; set; }
    }
}