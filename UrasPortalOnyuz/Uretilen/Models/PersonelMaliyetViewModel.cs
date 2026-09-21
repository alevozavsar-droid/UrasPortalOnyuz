using System;

namespace WebApplication3.Models
{
    public class PersonelMaliyetViewModel
    {



        public int DocEntry { get; set; }
        public string No { get; set; }
        public string FirmaSGK { get; set; }
        public string AdSoyad { get; set; }
        public string Unvan { get; set; }
        public string Bolum { get; set; }
        public string Gorev { get; set; }
        public string Not { get; set; }
        public DateTime? IseGiris { get; set; }
        public string EmeklilikDurumu { get; set; }




        public decimal X_NetUcret { get; set; }
        public decimal ToplamNetUcret { get; set; } // Excel'deki Toplam Net
        public decimal R_NetUcret { get; set; }


        public decimal Avans { get; set; }
        public decimal Icra { get; set; }
        public decimal DamgaVergisi { get; set; }
        public decimal GelirVergisi { get; set; }
        public decimal IssizlikIsci { get; set; }
        public decimal SGKIsci { get; set; }
        public decimal ToplamBrutUcret { get; set; } // Excel'deki Toplam Brüt


        public decimal SGKIsveren { get; set; }
        public decimal IssizlikIsveren { get; set; }
        public decimal ToplamIsverenPayi { get; set; }


        public decimal FazlaMesai { get; set; }
        public decimal Bayram { get; set; }
        public decimal GeceFarki { get; set; }
        public decimal Pazar { get; set; }
        public decimal ResmiTatil { get; set; }
        public decimal ToplamEkCalisma { get; set; }


        public decimal Yemek { get; set; }
        public decimal Yol { get; set; }
        public decimal Servis { get; set; }
        public decimal Egitim { get; set; }
        public decimal ToplamSosyalYardim { get; set; }


        public decimal SatisPrimi { get; set; }
        public decimal OzelOdeme { get; set; }
        public decimal ToplamPrim { get; set; }


        public decimal OzelSaglikSigortasi { get; set; }
        public decimal Kiyafet { get; set; }
        public decimal KoruyucuEkipman { get; set; }
        public decimal DogumYardimi { get; set; }
        public decimal EvlilikYardimi { get; set; }
        public decimal ToplamYanHaklar { get; set; }


        public decimal KidemTazminati { get; set; }
        public decimal IhbarTazminati { get; set; }
        public decimal IseIade { get; set; }
        public decimal YillikIzin { get; set; }
        public decimal RaporFarki { get; set; }
        public decimal DogumIzni { get; set; }
        public decimal ToplamBrutOdemeler { get; set; }


        public decimal GenelToplamMaliyet { get; set; } // Excel'deki GENEL TOPLAM MALİYET
        public decimal EskiGenelMaliyet { get; set; }   // Veritabanında saklanan maliyet referansı




        public decimal ZamOrani { get; set; }   // R-Net Zammı
        public decimal XZamOrani { get; set; }  // X-Net Zammı
        public decimal YeniGenelMaliyet { get; set; } // Veritabanı alanı (Opsiyonel)






        public Personel2026Data Calculated2025 { get; set; }


        public Personel2026Data Calculated2026 { get; set; }
    }


    public class Personel2026Data
    {
        public decimal R_NetUcret { get; set; }
        public decimal ToplamNetUcret { get; set; } // (X-Net + R-Net)
        public decimal ToplamBrutUcret { get; set; }

        public decimal SGKIsci { get; set; }
        public decimal IssizlikIsci { get; set; }
        public decimal GelirVergisi { get; set; }
        public decimal DamgaVergisi { get; set; }


        public decimal Avans { get; set; }
        public decimal Icra { get; set; }

        public decimal SGKIsveren { get; set; }
        public decimal IssizlikIsveren { get; set; }
        public decimal ToplamIsverenPayi { get; set; }

        public decimal ToplamMaliyet { get; set; }
    }
}