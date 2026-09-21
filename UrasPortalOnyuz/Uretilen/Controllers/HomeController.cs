// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using WebApplication3.Models;
using WebApplication3.Data;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using System.Net;
using System.Net.Mail;

namespace WebApplication3.Controllers
{



    public class HomeIndexViewModel
    {
        public Dictionary<string, List<AppMenu>> RaporGruplari { get; set; } = new Dictionary<string, List<AppMenu>>();
        public List<AppDatabase> AuthorizedDatabases { get; set; } = new List<AppDatabase>();


        public List<AppMenu> FavoriteMenus { get; set; } = new List<AppMenu>();
        public List<int> FavoriteMenuIds { get; set; } = new List<int>();
    }
}




namespace WebApplication3.Models
{
    [Table("Z_UserFavoriteMenus")]
    public class UserFavoriteMenu
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string UserCode { get; set; }

        [Required]
        public int MenuId { get; set; }
    }

    [Table("Z_IT_SorunIstek")]
    public class ITSorunIstek
    {
        [Key]
        public int Id { get; set; }

        public string UserCode { get; set; }
        public string Email { get; set; }
        public string TalepTipi { get; set; }
        public string Baslik { get; set; }
        public string Aciklama { get; set; }
        public string Durum { get; set; }
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? TamamlanmaTarihi { get; set; }
        public string TamamlayanKullanici { get; set; }






        [NotMapped] public string Oncelik { get; set; }
        [NotMapped] public string Kategori { get; set; }
        [NotMapped] public string Departman { get; set; }
        [NotMapped] public string Telefon { get; set; }
        [NotMapped] public string Atanan { get; set; }
        [NotMapped] public string CozumNotu { get; set; }
        [NotMapped] public DateTime? SonGuncelleme { get; set; }


        [NotMapped] public string KullaniciAdi { get; set; }
        [NotMapped] public string TamamlayanAdi { get; set; }
        [NotMapped] public string AtananAdi { get; set; }
        [NotMapped] public int EkSayisi { get; set; }
        [NotMapped] public int YorumSayisi { get; set; }

        [NotMapped]
        public bool Kapali => Durum == "Tamamlandı" || Durum == "İptal";


        [NotMapped]
        public double GecenSaat => Kapali && TamamlanmaTarihi.HasValue
            ? (TamamlanmaTarihi.Value - OlusturmaTarihi).TotalHours
            : (DateTime.Now - OlusturmaTarihi).TotalHours;

        [NotMapped]
        public string GecenSureMetni
        {
            get
            {
                var s = GecenSaat;
                if (s < 1) return Math.Max(1, (int)(s * 60)) + " dk";
                if (s < 48) return Math.Round(s, 1).ToString("0.#") + " saat";
                return Math.Round(s / 24, 1).ToString("0.#") + " gün";
            }
        }


        [NotMapped]
        public string SlaDurumu
        {
            get
            {
                if (Kapali) return "kapali";
                var s = GecenSaat;
                if (Oncelik == "Kritik") return s > 4 ? "gecikmis" : (s > 1 ? "bekliyor" : "yeni");
                if (Oncelik == "Yüksek") return s > 24 ? "gecikmis" : (s > 8 ? "bekliyor" : "yeni");
                return s > 72 ? "gecikmis" : (s > 24 ? "bekliyor" : "yeni");
            }
        }
    }

    [Table("Z_IT_TalepEk")]
    public class ITTalepEk
    {
        [Key]
        public int Id { get; set; }
        public int TalepId { get; set; }
        public string DosyaAdi { get; set; }
        public string DosyaYolu { get; set; }
        public string Uzanti { get; set; }
        public long? Boyut { get; set; }
        public bool ResimMi { get; set; }
        public string YukleyenKullanici { get; set; }
        public DateTime? YuklemeTarihi { get; set; }

        [NotMapped]
        public string BoyutMetni
        {
            get
            {
                var b = Boyut ?? 0;
                if (b < 1024) return b + " B";
                if (b < 1024 * 1024) return Math.Round(b / 1024d, 1).ToString("0.#") + " KB";
                return Math.Round(b / (1024d * 1024d), 2).ToString("0.##") + " MB";
            }
        }
    }

    [Table("Z_IT_TalepYorum")]
    public class ITTalepYorum
    {
        [Key]
        public int Id { get; set; }
        public int TalepId { get; set; }
        public string Kullanici { get; set; }
        public string Yorum { get; set; }
        public bool SistemNotuMu { get; set; }
        public DateTime? Tarih { get; set; }

        [NotMapped] public string KullaniciAdi { get; set; }

        [NotMapped]
        public string KullanciGosterim => string.IsNullOrWhiteSpace(KullaniciAdi) ? Kullanici : KullaniciAdi;
    }

    public class ITPersonelOgesi
    {
        public string Kod { get; set; }
        public string Ad { get; set; }
    }

    public class ITIstatistik
    {
        public int Toplam { get; set; }
        public int Acik { get; set; }
        public int Islemde { get; set; }
        public int Beklemede { get; set; }
        public int Tamamlanan { get; set; }
        public int Iptal { get; set; }
        public int BugunGelen { get; set; }
        public int Kritik { get; set; }

        public double OrtCozumSaati { get; set; }

        public double MedyanCozumSaati { get; set; }

        public double TakvimOrtCozumSaati { get; set; }
        public int CozumOrneklemi { get; set; }

        public string OrtCozumMetni => SaatMetni(OrtCozumSaati);
        public string MedyanCozumMetni => SaatMetni(MedyanCozumSaati);
        public string TakvimOrtCozumMetni => SaatMetni(TakvimOrtCozumSaati, true);


        public static string SaatMetni(double saat, bool takvim = false)
        {
            if (saat <= 0) return "-";
            double gunSaat = takvim ? 24.0 : IsSaatiHesabi.GunlukSaat;
            if (saat < 1) return Math.Round(saat * 60).ToString("0") + " dk";
            if (saat < gunSaat * 2) return saat.ToString("0.#") + " saat";
            return Math.Round(saat / gunSaat, 1).ToString("0.#") + (takvim ? " gün" : " iş günü");
        }
    }





    public static class IsSaatiHesabi
    {
        public static readonly TimeSpan Baslangic = new TimeSpan(8, 30, 0);
        public static readonly TimeSpan Bitis = new TimeSpan(18, 0, 0);
        public static double GunlukSaat => (Bitis - Baslangic).TotalHours;   // 9,5

        public static double IsSaati(DateTime baslangic, DateTime bitis)
        {
            if (bitis <= baslangic) return 0;
            double toplam = 0;
            var gun = baslangic.Date;
            while (gun <= bitis.Date)
            {
                if (gun.DayOfWeek != DayOfWeek.Saturday && gun.DayOfWeek != DayOfWeek.Sunday)
                {
                    DateTime gunBas = gun + Baslangic, gunBit = gun + Bitis;
                    DateTime b = baslangic > gunBas ? baslangic : gunBas;
                    DateTime s = bitis < gunBit ? bitis : gunBit;
                    if (s > b) toplam += (s - b).TotalHours;
                }
                gun = gun.AddDays(1);
            }
            return Math.Round(toplam, 2);
        }

        public static double Medyan(List<double> degerler)
        {
            if (degerler == null || degerler.Count == 0) return 0;
            var s = degerler.OrderBy(x => x).ToList();
            int n = s.Count;
            return n % 2 == 1 ? s[n / 2] : (s[n / 2 - 1] + s[n / 2]) / 2.0;
        }
    }


    public class ZoomKullaniciSatiri
    {
        public string KullaniciKodu { get; set; }
        public string AdSoyad { get; set; }
        public string Yetki { get; set; }
        public string ZoomMetni { get; set; }
        public DateTime? Guncelleme { get; set; }


        public int Zoom
        {
            get
            {
                int z;
                return int.TryParse(ZoomMetni, out z) && z > 0 ? z : 100;
            }
        }
    }

    public class ITDestekViewModel
    {
        public List<ITSorunIstek> Talepler { get; set; } = new List<ITSorunIstek>();
        public List<ITSorunIstek> AcikIstekler { get; set; } = new List<ITSorunIstek>();
        public List<ITSorunIstek> TamamlananIstekler { get; set; } = new List<ITSorunIstek>();
        public List<ITPersonelOgesi> Personeller { get; set; } = new List<ITPersonelOgesi>();

        public ITIstatistik Istatistik { get; set; } = new ITIstatistik();

        public bool IsIT02 { get; set; }
        public bool ITYetkilisi { get; set; }
        public string KullaniciKodu { get; set; }
        public string KullaniciAdi { get; set; }
        public string Email { get; set; }
        public string Hata { get; set; }

        public string[] TalepTipleri { get; set; } = new string[0];
        public string[] Kategoriler { get; set; } = new string[0];
        public string[] Oncelikler { get; set; } = new string[0];
        public string[] Durumlar { get; set; } = new string[0];
    }
}