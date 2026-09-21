using System;

namespace WebApplication3.Models
{
    public class CariKarsilastirmaViewModel
    {
        public DateTime? Tarih { get; set; }
        public string IslemTipi { get; set; } // MF, ÖD v

        public string CariKodu { get; set; } // YENİ: Hangi Cari?
        public string CariAdi { get; set; }

        public string Aciklama { get; set; }
        public string IslemNo { get; set; } // <--- YENİ EKLENDİ
        public string ParaBirimi { get; set; }
        public decimal BorcTL { get; set; }
        public decimal AlacakTL { get; set; }
        public decimal BorcYPB { get; set; }
        public decimal AlacakYPB { get; set; }


        public string Kaynak { get; set; } // "DB", "Excel", "Eşleşti"
        public string DurumMesaji { get; set; } // "Excel'de Yok", "DB'de Yok", "Tam Eşleşme"
        public string SatirRengi { get; set; } // Bootstrap class: table-danger, table-warning, table-success
    }
}