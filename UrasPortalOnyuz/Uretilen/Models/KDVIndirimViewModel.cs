using System;
using System.ComponentModel.DataAnnotations.Schema; // [NotMapped] için önerilir

namespace WebApplication3.Models
{
    public class KDVIndirimViewModel
    {

        public DateTime Tarih { get; set; }
        public string AlisFaturasininSerisi { get; set; }
        public string AlisFaturasininSiraNosu { get; set; }
        public string SaticininAdiUnvani { get; set; }
        public string SaticininVergiKimlikNumarasi { get; set; }
        public string AlinanMalHizmetinCinsi { get; set; }
        public string AlinanMalHizmetinMiktari { get; set; }
        public decimal KDVHaricTutari { get; set; }
        public decimal KDVsi { get; set; }
        public decimal TevkifataTabiOlmayanIndirilenKDV { get; set; }
        public decimal IkiNoluBeyannamedeOdenenKDV { get; set; }
        public decimal ToplamIndirilenKDVTutari { get; set; }
        public string GGBTescilNosu { get; set; }



        public string BelgeninIndirimHakkiKullanilanDonem { get; set; }

        public string IslemTipi { get; set; }

        /*



        [NotMapped] // Entity Framework Core kullanılıyorsa, bu alanın DB'ye eşlenmemesi için
        public string HesaplananIndirimDonemi
        {
            get { return Tarih.ToString("yyyyMM"); }
        }
        */
    }


  
}