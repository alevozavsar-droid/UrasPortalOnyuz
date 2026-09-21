using DocumentFormat.OpenXml.Math;
using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace WebApplication3.Models
{




    public class ReconciliationSimulationViewModel
    {


        [Display(Name = "Cari Kodu")]
        public string CariKodu { get; set; }

        [Display(Name = "Cari Bilgisi")]
        public string CariBilgisi { get; set; }

        [Display(Name = "İşlem No")]
        public string TransId { get; set; }





        [Display(Name = "Satır No")]
        public int? LineID { get; set; }

        [Display(Name = "İşlem Tipi")]
        public string IslemTipi { get; set; }

        [Display(Name = "Bakiye Tipi")]
        public string BakiyeTipi { get; set; } // 'BORÇ', 'ALACAK'



        [Display(Name = "Belge Tarihi")]
        [DataType(DataType.Date)]
        public DateTime RefDate { get; set; }

        [Display(Name = "Vade Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        [Display(Name = "Sıralama Tarihi")]
        [DataType(DataType.Date)]
        public DateTime? SiralamaTarihi { get; set; } // FIFO mantığına göre hesaplanan tarih

        [Display(Name = "Aktarım Tipi")]
        public string AktarimTipi { get; set; } // U_BE1_AKTAR



        [Display(Name = "Para Birimi")]
        public string ParaBirimi { get; set; } // Satırın orijinal para birimi (EUR, USD, TRY)

        [Display(Name = "Kur Farkı PB")]
        public string KurFarkiParaBirimi { get; set; } // U_BE1_TransKind




        [Display(Name = "Kalan Bakiye (TL)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal KAPANMAYAN_KALAN_BAKİYE { get; set; }


        [Display(Name = "Kalan Bakiye (Döviz)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal DovizliKalanBakiye { get; set; }


        [Display(Name = "Kapanan Tutar (TL)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal KapananTutar { get; set; }


        [Display(Name = "Kapanan Tutar (Döviz)")]
        [DisplayFormat(DataFormatString = "{0:N2}")]
        public decimal KapananDoviz { get; set; }

        [Display(Name = "Orijinal Açık Bakiye")]
        public decimal OrijinalAcikBakiye { get; set; } // İsteğe bağlı, View'da hesaplanabilir



        [Display(Name = "Satır Tipi")]
        public string SiraTipi { get; set; } // 'HEADER' (Ana Kayıt) veya 'DETAIL' (Eşleşme)

        [Display(Name = "Kapatma Grup No")]
        public int? KapatmaGrubuNo { get; set; } // Eşleşen işlemleri gruplamak için ID

        [Display(Name = "Kapatan İşlem")]
        public string KapatanIslemNo { get; set; } // Karşı bacağın TransId'si

        [Display(Name = "Tahsilat Açıklama")]
        public string TahsilatAciklamaDetayi { get; set; }

        [Display(Name = "Mutabakat Durumu")]
        public string MutabakatDurumu { get; set; } // 'KAPANDI', 'AÇIK', 'DETAY'



        [Display(Name = "Kapatmada Kullanılan Karşı Bakiye")]
        public decimal ToplamKullanilabilirKarsiBakiye { get; set; }

        [Display(Name = "Vadesi Geçmiş Bakiye")]
        public decimal VadesiGecmisKalanBakiye { get; set; }

        [Display(Name = "Gecikme Günü")]
        public int GecikmeGunu { get; set; }
    }
    public class InvoicePaymentViewModel
    {

        public string MusteriKodu { get; set; }
        public string MusteriBilgisi { get; set; }
        public string Aciklama { get; set; }

        public int? FaturaNo { get; set; }
        public string TransId { get; set; }
        public int? YevmiyeNo { get; set; }

        public string IslemNoStr { get; set; }
        public string IslemTipi { get; set; }
        public DateTime? FaturaTarihi { get; set; }
        public string FaturaTarihiAyYil { get; set; }
        public int? MusteriVadeGunu { get; set; }
        public DateTime? VadeTarihi { get; set; }
        public string VadeTarihiAyYil { get; set; }
        public string OdemeTuru { get; set; }
        public decimal? Borc { get; set; }
        public decimal? Alacak { get; set; }
        public decimal? Kalan { get; set; }
        public decimal? Bakiye { get; set; }
        public string CekNo { get; set; }
        public string FindeksNotu { get; set; }


        public DateTime? OdemeTarihi { get; set; }
        public string OdemeTarihiAyYil { get; set; }
        public DateTime? CekVadesi { get; set; }
        public string CekVadesiAyYil { get; set; }


        public int? TahsilatYaslandirma { get; set; }
        public int? AdatYaslandirma { get; set; } // SQL'den gelen Vade Gecikme Günü
        public string MutabakatDurumu { get; set; }
        public int? MutabakatNumarasi { get; set; }
        public string FaturaTuru { get; set; }


        public string DurumBilgisi { get; set; }


        public int? SiralamaNo { get; set; }
        public DateTime? SiralamaTarihi { get; set; }
        public int? SiraTipi { get; set; }



        public static string CalculateDurum(InvoicePaymentViewModel model, DateTime today)
        {

            if (model.Borc.HasValue && model.Borc.Value > 0 && !model.Alacak.HasValue)
            {


                decimal kalan = model.Kalan.GetValueOrDefault(0);
                int adatGun = model.AdatYaslandirma.GetValueOrDefault(0);

                if (Math.Abs(kalan) < 0.005M)
                {
                    return "KAPANDI";
                }

                if (kalan > 0.005M && adatGun > 0)
                {
                    return "BORÇ VADESİ GEÇTİ";
                }

                return "AÇIK";
            }


            return model.IslemTipi ?? "BİLİNMİYOR";
        }
    }
}