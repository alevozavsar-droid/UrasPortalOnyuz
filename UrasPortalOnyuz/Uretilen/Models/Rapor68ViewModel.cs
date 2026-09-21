using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class Rapor68ViewModel
    {

        public List<Rapor68Item> Satirlar { get; set; } = new List<Rapor68Item>();


        public List<DosyaSecenek> DosyaListesi { get; set; } = new List<DosyaSecenek>();
    }

    public class Rapor68Item
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public int TransId { get; set; } // Güncelleme için anahtar (PK)
        public string Ref1 { get; set; }
        public DateTime TaxDate { get; set; }
        public DateTime DueDate { get; set; }
        public string IhracatDosyaNo { get; set; } // Mevcut değer
        public string ParaBirimi { get; set; }
        public decimal TutarUlusal { get; set; }
        public decimal TutarDoviz { get; set; }
    }

    public class DosyaSecenek
    {
        public string DosyaNo { get; set; }
        public string CariAdi { get; set; }
    }

    public class UpdateRequest1
    {
        public int TransId { get; set; }
        public string DosyaNo { get; set; }
    }
}