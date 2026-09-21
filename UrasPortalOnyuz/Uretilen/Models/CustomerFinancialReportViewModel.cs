using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class CustomerFinancialReportViewModel
    {

        public string CardCode { get; set; } // [Cari Kodu]
        public string Musteri { get; set; }  // [Cari Adı]
        public string SatisSorumlusu { get; set; } // Bu alan SQL'de çekilmiyor, ancak görünümde mevcut. OCRD/OSLP join'i controller'da eklenmeli.


        public decimal? MusteriGuncelBakiyesi { get; set; } // [Güncel Bakiye]
        public decimal? MutabakattaAcikTahsilat { get; set; } // [Mutabakatta Açık Tahsilat (?)]
        public decimal? MutabakattaKapanacakTahsilat { get; set; } // [Mutabakatta Açık Tahsilat (?)]



        public decimal? ToplamRisk { get; set; } // [Toplam Risk (?)]
        public decimal? PortfoydekiCek { get; set; } // [Portföydeki Çek (Vadesi Gelmemiş) (?)]
        public decimal? ToplamAcikFatura { get; set; } // [Toplam Açık Fatura (?)]
        public decimal? VadesiGecmisTutar { get; set; } // [V. Geçmiş Tutar (?)]
        public decimal? VadesiGelecekTutar { get; set; } // [V. Gelecek Tutar (?)]


        public int? VadesiGecmisAdet { get; set; } // [V.G. Adet]
        public double? OrtalamaAdat { get; set; } // [Ort. Adat (Gün)]
        public double? OrtalamaTahsilatYaslandirma { get; set; } // [Ort. Tah. Yaş. (Gün)]
        public int? EnEskiFarkGun { get; set; } // [En Eski Fark (Gün)]


        public DateTime? EnEskiVadeTarihi { get; set; } // [En Eski Vade Tarihi]
        public DateTime? EnSonTeslimatTarihi { get; set; } // [En Son Teslimat]
        public DateTime? SonTahsilatTarihi { get; set; } // [Son Tahsilat]
        public List<CustomerNoteViewModel> CustomerNotes { get; set; } = new List<CustomerNoteViewModel>();
    }
    public class CustomerNoteViewModel
    {
        public int NoteId { get; set; }
        public string CardCode { get; set; }
        public DateTime? NotTarihi { get; set; }
        public string KisaNot { get; set; } // U_Not1
        public string KullaniciKodu { get; set; }
        public DateTime? EklemeTarihi { get; set; }
    }
    public class NewNoteModel
    {
        public string CardCode { get; set; }
        public string Note { get; set; }
    }


    public class NoteUpdateModel
    {
        public int NoteId { get; set; }
        public string Note { get; set; }
    }


    public class NoteDeleteModel
    {
        public int NoteId { get; set; }
}

}