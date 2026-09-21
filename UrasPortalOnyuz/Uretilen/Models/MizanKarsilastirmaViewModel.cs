using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{


    public class MizanKarsilastirmaViewModel
    {
        public List<MizanKarsilastirmaItem> Items { get; set; } = new List<MizanKarsilastirmaItem>();
    }

    public class MizanKarsilastirmaItem
    {
        public string HesapKodu { get; set; }
        public string HesapAdi { get; set; }
        public int Level { get; set; }

        public decimal KaynakBorc { get; set; }
        public decimal KaynakAlacak { get; set; }
        public decimal KaynakBakiye { get; set; }

        public decimal HedefBorc { get; set; }
        public decimal HedefAlacak { get; set; }
        public decimal HedefBakiye { get; set; }

        public decimal FarkBakiye { get; set; }
    }

    public class KarsilastirmaliMuavinRow
    {
        public DateTime Tarih { get; set; }
        public int TransId { get; set; }
        public string Aciklama { get; set; }
        public string AktarimTipi { get; set; }
        public decimal Tutar { get; set; }


        public int SrcTransId { get; set; }
    }

    public class MuavinEslesmeRow
    {
        public DateTime TarihSort { get; set; }
        public KarsilastirmaliMuavinRow Source { get; set; }
        public KarsilastirmaliMuavinRow Target { get; set; }
        public bool IsEslesmeTamam { get; set; }
    }



   
}