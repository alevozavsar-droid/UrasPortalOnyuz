using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class AktarimRaporuViewModel
    {
        public List<HesapOzeti> HesapHareketleri { get; set; }
        public List<HesapOzetiDovizli> DovizliHesapHareketleri { get; set; }
    }

    public class HesapOzeti
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public decimal ToplamBorc { get; set; }
        public decimal ToplamAlacak { get; set; }
        public decimal ToplamBakiye { get; set; }
        public decimal DistinctBy { get; set; }

    }

    public class HesapOzetiDovizli
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public string DovizCinsi { get; set; }


        public decimal TLBorc { get; set; }
        public decimal TLAlacak { get; set; }
        public decimal TLBakiye { get; set; }

        public decimal DovizBorc { get; set; }
        public decimal DovizAlacak { get; set; }
        public decimal DovizBakiye { get; set; }
        public string DistinctBy { get; set; }

    }

    public class HesapViewModel
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public int Level { get; set; }
        public int Levels { get; set; }
        public string ParentKodu { get; set; }
        public string Postable { get; set; }

    }
}