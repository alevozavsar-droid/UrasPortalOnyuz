// WebApplication3.Models/Rapor52ViewModel.cs

using System;
using System.Globalization;

namespace WebApplication3.Models
{
    public class VendorReconciliationSimulationViewModel
    {
        public string CariKodu { get; set; }
        public string CariBilgisi { get; set; }
        public string BakiyeTipi { get; set; } // BORÇ (Tedarikçiye Borcumuz), ALACAK (Tedarikçiden Alacağımız)
        public string SiraTipi { get; set; } // HEADER veya DETAIL
        public string TransId { get; set; } // İşlem Numarası
        public int? KapatmaGrubuNo { get; set; }
        public string IslemTipi { get; set; }
        public DateTime RefDate { get; set; }
        public DateTime? DueDate { get; set; }
        public DateTime? SiralamaTarihi { get; set; }
        public string AktarimTipi { get; set; }

        public decimal KAPANMAYAN_KALAN_BAKİYE { get; set; } // Kapanmayan orjinal bakiye (Header satırında)
        public decimal KapananTutar { get; set; } // Kapatılan miktar (Detail satırında)
        public string KapatanIslemNo { get; set; } // Kapatan işlemin belgesi/kayıt no
        public string MutabakatDurumu { get; set; }
    }
    public class Rapor52ViewModel
    {
        public string TedarikciKodu { get; set; } // Yeni: Tedarikci Kodu
        public string TedarikciBilgisi { get; set; } // Yeni: Tedarikci Adı

        public int? FaturaNo { get; set; } // Belge Numarası (Satın Alma Faturası)
        public string IslemNoStr { get; set; } // İşlem Numarası
        public string IslemTipi { get; set; } // Örn: SATINALMA FATURASI, ÖDEME, FATURA DIŞI ALACAK

        public DateTime? BelgeTarihi { get; set; } // Belge/Kayıt Tarihi
        public DateTime? VadeTarihi { get; set; } // Fatura Vade Tarihi

        public string OdemeSekli { get; set; } // Ödeme Şekli (Fatura üzerindeki)
        public string OdemeTuru { get; set; } // Ödeme/Tahsilat Şekli (İşlem üzerindeki)

        public decimal? Borc { get; set; } // Tedarikçiye Borç Tutarı (Satın Alma Faturasının Tutarını gösterir)
        public decimal? Alacak { get; set; } // Tedarikçiye Ödenen/Kapanan Tutar (Ödeme/Kredi notu)
        public decimal? Kalan { get; set; } // Açık Kalan Bakiye (Borç)
        public decimal? Bakiye { get; set; } // Hesap Bakiyesi

        public string CekNo { get; set; } // Çek Numarası
        public string FindeksNotu { get; set; } // Tedarikçide genelde boş

        public DateTime? KritikOdemeVadesi { get; set; } // AVM veya Gerçek Ödeme/Vade Tarihi
        public DateTime? CekVadesi { get; set; } // Çek Vade Tarihi

        public int? OdemeYaslandirma { get; set; } // Yeni: Ödeme Yaşl. (Gün)
        public int? AdatYaslandirma { get; set; } // Adat Yaşl. (Gün)

        public string MutabakatDurumu { get; set; }
        public int? MutabakatNumarasi { get; set; }
        public string FaturaTuru { get; set; }
        public int? YevmiyeNo { get; set; }

        public string BelgeTarihiAyYil { get; set; }
        public string VadeTarihiAyYil { get; set; }
        public string OdemeTarihiAyYil { get; set; }
        public string CekVadesiAyYil { get; set; }


        public int? TedarikciVadeGunu { get; set; }

        public int? SiralamaNo { get; set; }
        public DateTime? SiralamaTarihi { get; set; }
        public int? SiraTipi { get; set; }


        public string DurumBilgisi { get; set; }

        public static string CalculateDurum(Rapor52ViewModel model, DateTime today)
        {
            if (model.Kalan.GetValueOrDefault(0) < 0.005M && model.IslemTipi != "ÖDEME" && model.IslemTipi != "FATURA DIŞI ALACAK" && model.IslemTipi != "KREDİ NOTU")
            {
                return "KAPANDI";
            }
            if (model.AdatYaslandirma.GetValueOrDefault(0) > 0)
            {
                return "BORÇ VADESİ GEÇTİ";
            }
            if (model.IslemTipi == "ÖDEME" || model.IslemTipi == "KREDİ NOTU")
            {

                return "Ödendi";
            }
            if (model.IslemTipi.StartsWith("FATURA DIŞI ALACAK") || model.IslemTipi == "SATINALMA FATURASI")
            {

                return "AÇIK";
            }

            return "DİĞER";
        }
    }
}