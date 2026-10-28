using System;
using System.Collections.Generic;

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
        public string IbrazBelgeYolu { get; set; }          // imzalı ibraz / ciro belgesi (Takip Paneli'nden yüklenir; /Rapor123/IbrazBelgeDosya?cek=…)
        public string IbrazBelgeDosya { get; set; }
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

        // ---- Konsolide Finansal Takip Paneli › Çek · Senet sekmesiyle ortak sütunlar (Dolaşım, Satır Kaynağı · İşlem, Nakit Akış Eşleşmesi, Kontrol, Fiş) ----
        public string CekTipi { get; set; }                 // X | R
        public string Dolasim { get; set; }                 // dolaşım planı özeti: A → B → C ("" = plan yok)
        public int DolasimGerceklesen { get; set; }
        public int DolasimToplam { get; set; }
        public string DolasimSiradaki { get; set; }         // "Grup İçi Ciro → Alv Kimya (05.11.2026)"
        public List<Rapor2DolasimAdim> DolasimAdimlar { get; set; } = new List<Rapor2DolasimAdim>();
        public string Kontrol { get; set; }                 // Uyumlu | Farklı | İbraz Bekliyor | Plansız İbraz | ""
        public string Kaynak { get; set; }                  // Kasa Nakit Akışı · Plan | Çek Akıbeti · Portföy Girişi | Çek Akıbeti · Ciro | Dolaşım Planı · Gerçekleşen Adım …
        public string KaynakDetay { get; set; }
        public string SatirIslem { get; set; }              // portfoy (Nakit Akışa Ekle / İbraz Et) | planli (Tedarikçiye Çık) | gmy (onay bekliyor) | yok (işlem gerekmez)
        public int? PlanDocEntry { get; set; }              // Kasa Nakit Akışı ödeme satırı
        public string PlanDurum { get; set; }
        public string PlanTalimat { get; set; }
        public string PlanLink { get; set; }
        public string CikisTalebi { get; set; }
        public string PlanOnay { get; set; }
        public string FisNo { get; set; }                   // muhasebe fişi (panelle aynı numara)
        public string FisKaynak { get; set; }
        public string FisTarih { get; set; }
        // ---- Çek / Senet Durum Parametreleri Standart Raporu: otomatik türetilen alanların gerekçesi ----
        public string UygunlukNedeni { get; set; }          // "" = Uygun; aksi halde Uygun Değil nedeni (Planlandı, Vadesi Geçti, Bankada …)
        public string VadeYakinligi { get; set; }           // Bugün | Bu Hafta | Bu Ay | İleri Vadeli | Vadesi Geçti (bilgi)
        public string IbrazTuru { get; set; }               // Ciro | Bankaya İbraz | Teminat | Avukat | Elden Tahsil | İade | İptal | ""
    }

    public class Rapor2DolasimAdim { public int Sira { get; set; } public string Tur { get; set; } public string Eden { get; set; } public string Alan { get; set; } public string Durum { get; set; } public string Tarih { get; set; } public string BelgeNo { get; set; } }

    public class CheckTransactionType
    {
        public string Code { get; set; }
        public string Description { get; set; }
    }



}