using System;

namespace WebApplication3.Models
{
    public class Rapor71ViewModel
    {
        public int DocEntry { get; set; }
        public string CekNumarasi { get; set; }
        public string BankaAdi { get; set; }
        public string Sube { get; set; }
        public string CekKimdenGeldi { get; set; }
        public string AsilBorclu { get; set; }
        public decimal? Tutar { get; set; }
        public string ParaBirimi { get; set; }
        public DateTime? VadeTarihi { get; set; }
        public string VadeAyiVeYili { get; set; }
        public string PortfoyVadeAyiYili { get; set; }
        public DateTime? PortfoyeGirisTarihi { get; set; }
        public DateTime? IbrazTarihi { get; set; }
        public string TahsilatBelgeNo { get; set; }
        public string IbrazBelgeNo { get; set; }
        public string PlanlananTedarikci { get; set; }
        public string HareketDurumu { get; set; }
        public string BelgeTipi { get; set; }
        public string CekIslemTuru { get; set; }
        public string IslemTipiAdi { get; set; }
        public string CekKimeVerildi { get; set; }
        public decimal? TlKarsiligi { get; set; }
        public string KullanımDurumu { get; set; }
    }
}