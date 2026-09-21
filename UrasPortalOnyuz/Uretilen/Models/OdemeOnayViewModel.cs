using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;



namespace WebApplication3.Models
{

    public class OdemeOnayViewModel
    {
        public int Id { get; set; }
        public string U_TedarikciAlacakli { get; set; }
        public string U_CardCode { get; set; }
        public string U_DocNum { get; set; }
        public string U_FaturaNo { get; set; }
        public DateTime? U_OrderDate { get; set; }
        public DateTime? U_OdeplanTarihi { get; set; }
        public decimal U_Tutar { get; set; }
        public string U_ParaBirimi { get; set; }
        public string U_OdemeNedeni { get; set; }
        public string U_OdeplanNot { get; set; }
        public string U_Belge { get; set; } = "Manuel";
        public string U_Sahip { get; set; }
        public string U_OdemeSekli { get; set; }
        public int U_U_BE1_SIRA { get; set; }
        public DateTime TalepTarihi { get; set; }
        public string TalepEdenKullanici { get; set; }
        public string OnayDurumu { get; set; } // "Bekleniyor", "Onaylandı", "Reddedildi"
        public string OnaylayanKullanici { get; set; }
        public DateTime? OnayTarihi { get; set; }
    }
}