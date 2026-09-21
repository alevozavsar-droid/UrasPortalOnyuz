using System.Collections.Generic;
using System;
namespace WebApplication3.Models
{


    public class Rapor51ViewModel
    {
        public List<OpenRequestViewModel> OpenRequests { get; set; }
        public List<OpenOrderViewModel> OpenOrders { get; set; }
    }

    public class OpenRequestViewModel
    {
        public int TalepNo { get; set; }
        public DateTime? TalepTarihi { get; set; }
        public DateTime? IhtiyacTarihi { get; set; }
        public string StokKodu { get; set; }
        public string Aciklama { get; set; }
        public string Aciklama2 { get; set; }
        public decimal Miktar { get; set; }
        public string OwnerAdi { get; set; }
        public string BelgeyiOlusturanKullanici { get; set; }
    }

    public class OpenOrderViewModel
    {
        public int SiparisNo { get; set; }
        public DateTime? SiparisTarihi { get; set; }
        public string StokKodu { get; set; }
        public string Aciklama { get; set; }
        public string Aciklama2 { get; set; }
        public decimal Miktar { get; set; }
        public decimal BirimFiyat { get; set; }
        public string ParaBirimi { get; set; }
        public string OwnerAdi { get; set; }
        public string BelgeyiOlusturanKullanici { get; set; }
    }



   
}