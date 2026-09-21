using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{




























    public class MutabakatItem
    {

        public DateTime Tarih { get; set; }
        public string IslemTipi { get; set; } // Fatura, Dekont vb.
        public string Aciklama { get; set; }
        public string ParaBirimi { get; set; }
        public decimal BorcTL { get; set; }
        public decimal AlacakTL { get; set; }
        public decimal BorcYPB { get; set; }
        public decimal AlacakYPB { get; set; }


        public string Kaynak { get; set; } // "SİSTEM" veya "EXCEL"
        public string Durum { get; set; }  // "EŞLEŞTİ", "SİSTEMDE YOK", "EXCELDE YOK"
        public string SatirRengi { get; set; } // table-success (Yeşil), table-danger (Kırmızı)
    }

    public class Rapor72ViewModel
    {
        public List<MutabakatItem> Sonuclar { get; set; } = new List<MutabakatItem>();
        public string SelectedBpCode { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public List<CariViewModel> CariListesi { get; set; }
    }























    public class SendEmailRequestModel
    {
        public List<MutabakatListeItem> SelectedItems { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }


    public class EkstreMailRequest
    {
        public string BpCode { get; set; }
        public string Email { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public List<string> AktarimTipi { get; set; }
        public bool IncludeInitialBalance { get; set; }
        public bool IncludeConnectedBp { get; set; }
    }




























   
}