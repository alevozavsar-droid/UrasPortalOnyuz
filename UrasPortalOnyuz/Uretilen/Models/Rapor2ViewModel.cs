using System;

namespace WebApplication3.Models
{
    public class Rapor2ViewModel
    {
        public int DocEntry { get; set; }
        public string CekNumarasi { get; set; }


        public string BankaAdi { get; set; } // 'Banka Adı' sütunu için
        public string Sube { get; set; }     // 'Şube' sütunu için

        public string HareketDurumu { get; set; }
        public string IslemTuru { get; set; }
        public string KonumDurumu { get; set; }
        public string VadeDurumu { get; set; }
        public string Aciklama { get; set; }
        public string UygunlukDurumu { get; set; }

        public string CekKimdenGeldi { get; set; }
        public string AsilBorclu { get; set; }
        public decimal? Tutar { get; set; }
        public string ParaBirimi { get; set; }
        public DateTime? VadeTarihi { get; set; }
        public string VadeAyiVeYili { get; set; }
        public string VadeliCekIbrazi { get; set; }
        

        public string PortfoyVadeAyiYili { get; set; }

        public DateTime? PortfoyeGirisTarihi { get; set; }
        public DateTime? IbrazTarihi { get; set; }


        public string TahsilatBelgeNo { get; set; }

        public string IbrazBelgeNo { get; set; }
        public string PlanlananTedarikci { get; set; }
        public string BelgeTipi { get; set; }
        public string CekIslemTuru { get; set; }
        public string IslemTipiAdi { get; set; }
        public string CekKimeVerildi { get; set; }
        public decimal? TlKarsiligi { get; set; }
        public string KullanımDurumu { get; set; }


        public string CekinBankasi { get; set; }
        public string GelmeNedeni { get; set; }
        public string SirketAdi { get; set; }
        public DateTime? SirlamaTarihi { get; set; }
        public string CekBankaAdi { get; set; }
    }

    public class CheckTransactionType
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }



}