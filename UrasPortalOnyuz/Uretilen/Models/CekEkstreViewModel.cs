using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{
    public class CekEkstreSonucModel
    {
        public string CekNo { get; set; }
        public string HikayeHtml { get; set; }
        public CekEkstreViewModel MainRecord { get; set; }
        public List<CekEkstreViewModel> Hareketler { get; set; } = new List<CekEkstreViewModel>();
    }
    public class R123_OnayDurumModel
    {
        public int Id { get; set; }
        public string DbKey { get; set; }
        public int IslemNo { get; set; }
        public string HesapTipi { get; set; }
        public string Durum { get; set; } // "ONAYLANDI" veya "EKSİK"
        public string NotBasligi { get; set; }
        public string NotIcerigi { get; set; }
        public string KullaniciKodu { get; set; }
        public string KullaniciAdi { get; set; }
        public DateTime IslemTarihi { get; set; }
    }
    public class CekEkstreViewModel

    {
        public string BelgeTipi { get; set; }
        public int DocEntry { get; set; }
        public int SequenceID { get; set; }
        public DateTime IslemTarihiObj { get; set; }
        public string IbrazTarihi { get; set; }
        public string CekNumarasi { get; set; }
        public string BankaAdi { get; set; }
        public string Sube { get; set; }
        public string CekKimdenGeldi { get; set; }
        public string CekKimdenGeldiKod { get; set; }
        public string AsilBorclu { get; set; }
        public string CekIslemTuru { get; set; }
        public string CekKimeVerildi { get; set; }
        public string CekKimeVerildiKod { get; set; }
        public string IslemTipi { get; set; }
        public string HareketDurumu { get; set; }
        public string KullanimDurumu { get; set; }
        public decimal CekTutari { get; set; }
        public string ParaBirimi { get; set; }
        public string VadeTarihi { get; set; }
        public decimal TLKarsiligi { get; set; }
        public string IslemiYapan { get; set; }
        public bool IptalMi { get; set; }


        public string BagliTahsilatNo { get; set; }
        public string BagliIbrazNo { get; set; }
        public string BagliVadeliIbrazNo { get; set; }


        public string SirketAdi { get; set; }
    }

    public class CekListeItem
    {
        public string CheckKey { get; set; }
        public string CheckNum { get; set; }
        public string BankCode { get; set; }
        public decimal CheckSum { get; set; }
        public string Currency { get; set; }
    }



    public class CekAlanFarki
    {
        public string Alan { get; set; }
        public string Eski { get; set; }
        public string Yeni { get; set; }
        public bool Degisti => !string.Equals((Eski ?? "").Trim(), (Yeni ?? "").Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public class CekDegisiklikOzeti
    {
        public bool YeniGirisVarMi { get; set; }
        public int? YeniDocEntry { get; set; }
        public string YeniIslemTarihi { get; set; }
        public List<CekAlanFarki> Farklar { get; set; } = new List<CekAlanFarki>();
    }
}