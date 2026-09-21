
using System;
using System.Globalization;

namespace WebApplication3.Models
{

    public class TedarikciFinansalBorcRaporuViewModel
    {
        public string CardCode { get; set; }
        public string Tedarikci { get; set; } // Tedarikci Adı
        public string SatisSorumlusu { get; set; } // Satış Sorumlusu (Genellikle tedarikçiler için farklı bir alan kullanılır, ancak mevcut SQL'deki gibi tutuldu)

        public decimal? TedarikciGuncelBakiyesi { get; set; } // OCRD.Balance


        public decimal? ToplamAcikBorc { get; set; } // Toplam Açık Bakiye (Fatura + Fatura Dışı)
        public decimal? VadesiGecmisBorcTutar { get; set; } // V. Geçmiş Borç Tutar
        public decimal? VadesiGelecekBorcTutar { get; set; } // V. Gelecek Borç Tutar


        public decimal? MutabakattaAcikOdeme { get; set; } // Mutabakatta Açık Ödeme (Borç Toplam)
        public decimal? MutabakattaKapanacakOdeme { get; set; } // Mutabakatta Kapanacak Ödeme (Alacak Toplam)


        public int? VadesiGecmisAdet { get; set; } // V.G. Adet (Fatura + Fatura Dışı)
        public double? OrtalamaAdat { get; set; } // Ort. Adat (Gün)
        public double? OrtalamaOdemeYaslandirma { get; set; } // Ort. Ödeme Yaş. (Gün)
        public int? EnEskiFarkGun { get; set; } // En Eski Fark (Gün)


        public DateTime? EnEskiVadeTarihi { get; set; }
        public DateTime? EnSonSatinAlmaTarihi { get; set; } // En Son Satın Alma Faturası
        public DateTime? SonOdemeTarihi { get; set; }
    }
}