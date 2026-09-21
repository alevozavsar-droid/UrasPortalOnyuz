using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace WebApplication3.Models
{

    public class UrunViewModel1
    {


        public string Id { get; set; }
        public string Text { get; set; }
    }




    public class DepoViewModel
    {
        public string DepoKodu { get; set; }
        public string DepoAdi { get; set; }
    }


    public class StokHareketViewModel
    {
        public string UrunKodu { get; set; }
        public string UrunIsmi { get; set; }
        public DateTime Tarih { get; set; }
        public int Saat { get; set; }
        public string Depo { get; set; }
        public string BelgeNo { get; set; }
        public string Aciklama { get; set; }
        public string HareketTipi { get; set; }
        public decimal GirenMiktar { get; set; }
        public decimal GirisFiyati { get; set; }
        public string GirisParaBirimi { get; set; }
        public decimal GirisTutari { get; set; }
        public decimal CikanMiktar { get; set; }
        public decimal CikisFiyati { get; set; }
        public string CikisParaBirimi { get; set; }
        public decimal CikisTutari { get; set; }
        public decimal KumulatifDepoStok { get; set; }
        public decimal IslemAnindakiDepoStok { get; set; }


        public string TarihDisplay => Tarih.ToString("dd.MM.yyyy", new CultureInfo("tr-TR"));
        public string SaatDisplay
        {
            get
            {
                if (Saat > 0)
                {

                    return (Saat / 100).ToString("00") + ":" + (Saat % 100).ToString("00");
                }
                return "-";
            }
        }
    }
}