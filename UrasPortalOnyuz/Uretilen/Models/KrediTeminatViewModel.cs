// WebApplication3/Models/KrediTeminatViewModel.cs (TAMAMI)

using System;
using System.Collections.Generic;

namespace WebApplication3.Models
{

    public class KrediTeminatViewModel
    {

        public int DocEntry { get; set; }
        public int? DocNum { get; set; }


        public string DosyaYolu { get; set; }


        public string FirmaAdi { get; set; }
        public string SozlesmeNumarasi { get; set; }
        public string BankaAdi { get; set; }
        public string KrediTuru { get; set; }
        public DateTime? KrediBaslangicTarihi { get; set; }
        public DateTime? KrediBitisTarihi { get; set; }
        public string DovizCinsi { get; set; }
        public string KrediDurumu { get; set; }
        public decimal? Anapara { get; set; }


        public decimal? FaizTutari { get; set; } // YENİ ALAN (U_U_FaizTutari)
        public decimal? FaizOrani { get; set; } // YENİ ALAN (U_U_FaizOrani)
        public decimal? AnaParaFaizliTutar { get; set; }
        public decimal? BSMV { get; set; } // YENİ ALAN (U_U_BSMV)
        public decimal? KKDF { get; set; } // YENİ ALAN (U_U_KKDF)
        public decimal? KDV { get; set; } // YENİ ALAN (U_U_KDV)
        public decimal? AylikGecikmeCezasi { get; set; } // YENİ ALAN (U_U_GecikmeCeza)
        public decimal? DigerGiderler { get; set; }
        public decimal? TumMaliyetlerDahil { get; set; } // Toplam Kredi Tutarı


        public int? ToplamTaksitSayisi { get; set; }
        public int? OdenenTaksitSayisi { get; set; }
        public int? KalanTaksitSayisi { get; set; }
        public decimal? AylikTaksitTutari { get; set; }
        public decimal? ToplamOdenenTutar { get; set; }
        public decimal? KalanOdemeTutari { get; set; }
        public decimal? KalanAnaparaTutari { get; set; } // YENİ ALAN (U_U_KalanAnaparaTutar)
        public decimal? GuncelBakiye { get; set; }


        public string TeminatTuruCek { get; set; } // TeminatTuru
        public int? ToplamCekAdedi { get; set; }
        public decimal? TeminatOlarakVerilenMusteriCekleriToplami { get; set; }
        public decimal? TahsilEdilenCekTutari { get; set; }
        public decimal? TahsilEdilemeyenCekTutari { get; set; }
        public decimal? TahsilOrani { get; set; }
        public decimal? TAnaparadanDusulenTutar { get; set; } // Tutar
        public decimal? TFaizdenDusulenTutar { get; set; } // Tutar
        public decimal? TeminatSonrasiGuncelBakiye { get; set; } // Bakiye


        public DateTime? YapilandirmaTarihi { get; set; }
        public decimal? YAPAnaparadanDusulen { get; set; }
        public decimal? YAPFaizdenDusulen { get; set; }
        public decimal? YAPToplamBorcAzalisi { get; set; }
        public decimal? YAPToplamBorcAzalisOrani { get; set; }
        public decimal? YAPYeniAnaparaTutari { get; set; }
        public decimal? YAPYeniFaizTutari { get; set; }
        public decimal? YAPYeniToplamBorc { get; set; }
        public decimal? YAPYeniFaizOrani { get; set; }
        public int? YAPYeniVade { get; set; }
        public decimal? YAPYeniAylikTaksit { get; set; }
        public decimal? YAPDigerMasraflardanDusulen { get; set; } // Yeni Eklendi
        public decimal? YAPYeniDigerGiderlerToplami { get; set; } // YENİ ALAN (U_U_YenDigerGider)


        public DateTime? IlkTaksitTarihi { get; set; } // YENİ ALAN (U_U_IlkTaksitTrh)
        public DateTime? SonTaksitTarihi { get; set; } // YENİ ALAN (U_U_SonTaksitTrh)


        public string SatirRengi { get; set; }
        public string HucreRenkleri { get; set; }
        public string SutunKrediTuruRenk { get; set; }


        public List<TeminatDetayViewModel> TeminatDetaylari { get; set; } = new List<TeminatDetayViewModel>();
        public List<KefaletDetayViewModel> KefaletDetaylari { get; set; } = new List<KefaletDetayViewModel>();
    }


    public class TeminatDetayViewModel
    {
        public int U_DocEntry { get; set; }
        public int U_LineId { get; set; }
        public string TeminatTuru { get; set; }
        public int? Derece { get; set; }
        public string IpotekVeren { get; set; }
        public string AitOlduguTaraf { get; set; }
        public string Tur { get; set; }
        public decimal? EkspertizDegeri { get; set; }
        public decimal? IpotekOrani { get; set; }
        public decimal? IpotekBedeli { get; set; }
        public DateTime? EkspertizTarihi { get; set; }
    }

    public class KefaletDetayViewModel
    {
        public int U_DocEntry { get; set; }
        public int U_LineId { get; set; }
        public string KefilAdiUnvani { get; set; }
        public string KefaletTuru { get; set; }
        public string KefilTuru { get; set; }
        public decimal? KefaletPayOrani { get; set; }
        public string Kapsam { get; set; }
    }
}