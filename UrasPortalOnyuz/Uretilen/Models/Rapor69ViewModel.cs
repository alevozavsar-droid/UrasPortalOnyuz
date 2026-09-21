using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class Rapor69ViewModel
    {
        public List<Rapor69Item> Satirlar { get; set; } = new List<Rapor69Item>();
        public List<DosyaSecenek69> DosyaListesi { get; set; } = new List<DosyaSecenek69>();
    }

    public class Rapor69Item
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public int TransId { get; set; }
        public string Ref1 { get; set; }
        public DateTime TaxDate { get; set; }
        public DateTime DueDate { get; set; }
        public string IthalatDosyaNo { get; set; }
        public string ParaBirimi { get; set; }
        public decimal TutarUlusal { get; set; }
        public decimal TutarDoviz { get; set; }
    }

    public class DosyaSecenek69
    {
        public string DosyaNo { get; set; }
        public string CariAdi { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; } // YENİ: Hesap İsmi
    }
}