// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System.Net.Mail;
using System.Net;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.IO;
using System.Text;
using System.Data;
using System.Globalization;
using WebApplication3.Models;




namespace WebApplication3.Models
{
    public class MutabakatViewModel
    {
        public MutabakatFilterModel Filters { get; set; }
        public List<MutabakatListeItem> Items { get; set; }
        public List<string> AvailableIslemTipleri { get; set; }
    }

    public class MutabakatFilterModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CariTipi { get; set; }
        public List<string> SelectedIslemTipleri { get; set; }
        public bool IncludeConnected { get; set; }
    }

    public class MutabakatListeItem
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }
        public string Email { get; set; }
        public decimal BalanceTL { get; set; }
        public decimal BalanceFC { get; set; }
        public string Currency { get; set; }

        public decimal BalanceTL_R { get; set; }
        public string Currency_R { get; set; }
        public decimal BalanceTL_X { get; set; }
        public string Currency_X { get; set; }
        public decimal BalanceTL_Y { get; set; }
        public string Currency_Y { get; set; }
        public decimal BalanceTL_Diger { get; set; }
        public string Currency_Diger { get; set; }

        public int LastStatus { get; set; }
        public string CustomerNote { get; set; }
        public DateTime? LastEmailDate { get; set; }
    }

    public class MutabakatCevapModel
    {
        public Guid Token { get; set; }
        public string CardName { get; set; }
        public decimal Bakiye { get; set; }
        public string ParaBirimi { get; set; }
        public string Tarih { get; set; }
        public int CevapDurumu { get; set; }
        public string Aciklama { get; set; }
    }

    public class MutabakatSonucModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal Bakiye { get; set; }
        public string ParaBirimi { get; set; }
        public DateTime GonderimTarihi { get; set; }
        public string Gonderen { get; set; }
        public int Durum { get; set; }
        public string MusteriNotu { get; set; }
        public DateTime? CevapTarihi { get; set; }
        public string IpAdresi { get; set; }
    }


    public class EkstreDetayItem
    {
        public string Tarih { get; set; }
        public string IslemNo { get; set; }
        public string BelgeNo { get; set; }
        public string Aciklama { get; set; }
        public string ParaBirimi { get; set; }
        public decimal BorcTL { get; set; }
        public decimal AlacakTL { get; set; }
        public decimal BakiyeTL { get; set; }
        public decimal BorcFC { get; set; }
        public decimal AlacakFC { get; set; }
        public decimal BakiyeFC { get; set; }
    }
}




namespace WebApplication3.Controllers
{
    [Authorize]
    public class Rapor59Controller : Controller
    {
        private readonly IConfiguration _configuration;

        private readonly List<DatabaseConfig> _databases = new List<DatabaseConfig>
        {
            new DatabaseConfig { Key = "DefaultConnection", Display = "URASKIMYA" },
            new DatabaseConfig { Key = "DefaultConnection1", Display = "URSMAKINE" },
            new DatabaseConfig { Key = "DefaultConnection2", Display = "AVRUPA_PAPER" },
            new DatabaseConfig { Key = "DefaultConnection3", Display = "ALV_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection4", Display = "DAF_KIMYA" },
            new DatabaseConfig { Key = "DefaultConnection5", Display = "SELVI" },
            new DatabaseConfig { Key = "DefaultConnection6", Display = "ALVFILO" },
            new DatabaseConfig { Key = "DefaultConnection7", Display = "AVRASYA" },
            new DatabaseConfig { Key = "DefaultConnection8", Display = "ASIA" },
            new DatabaseConfig { Key = "DefaultConnection9", Display = "DEKORLIM" },
            new DatabaseConfig { Key = "DefaultConnection10", Display = "URAS_HOLDING" },
            new DatabaseConfig { Key = "DefaultConnection32", Display = "URASHOLDING_2026" },
            new DatabaseConfig { Key = "DefaultConnection33", Display = "DAFKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection34", Display = "AVRUPAPAPER_2026" },
            new DatabaseConfig { Key = "DefaultConnection35", Display = "ALVFILO_2026" },
            new DatabaseConfig { Key = "DefaultConnection36", Display = "URASBASKI_2026" },

            new DatabaseConfig { Key = "DefaultConnection14", Display = "ALVFILO_AS" },
            new DatabaseConfig { Key = "DefaultConnection15", Display = "AVRUPAPAPER_AS" },
            new DatabaseConfig { Key = "DefaultConnection16", Display = "DAFKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection17", Display = "SELVI_A.S" },
            new DatabaseConfig { Key = "DefaultConnection18", Display = "URASHOLDING_A.S" },
            new DatabaseConfig { Key = "DefaultConnection19", Display = "DRN" },
            new DatabaseConfig { Key = "DefaultConnection20", Display = "ALVKIMYA_2026" },
            new DatabaseConfig { Key = "DefaultConnection21", Display = "URSMAKINE_2026" },
            new DatabaseConfig { Key = "DefaultConnection22", Display = "SELVI_2026" },
            new DatabaseConfig { Key = "DefaultConnection23", Display = "TESTURASKIMYA_A.SS" },
            new DatabaseConfig { Key = "DefaultConnection24", Display = "TESTSELVI" },
            new DatabaseConfig { Key = "DefaultConnection25", Display = "URASKIMYA_AS" },
            new DatabaseConfig { Key = "DefaultConnection26", Display = "URAS_BASKI" },
            new DatabaseConfig { Key = "DefaultConnection27", Display = "DELTA_POWER" },
            new DatabaseConfig { Key = "DefaultConnection28", Display = "MORAL_POWER" },
            new DatabaseConfig { Key = "DefaultConnection29", Display = "SADE_POWER" },
            new DatabaseConfig { Key = "DefaultConnection30", Display = "URAS_POWER" },
        };


        private string GetSelectedDatabase()
 {return default;
}

        private string GetConnectionString(string key)
 {return default;
}

        private List<string> GetAktarimTipleriList(string connStr)
 {return default;
}

        private string GetBpName(string connStr, string bpCode)  {return default;
}
        private string GetConnectedBpCode(string connStr, string bpCode)  {return default;
}


        [HttpPost]
        public IActionResult UpdateCustomerEmail(string cardCode, string email)
 {return Json(new { success = true, message = "E-Mail güncellendi." });
}





        private class BpRawInfo { public string CardCode { get; set; } public string CardName { get; set; } public string CardType { get; set; } public string Email { get; set; } public string ConnBP { get; set; } public int SonDurum { get; set; } public string MusteriNotu { get; set; } public DateTime? SonGonderim { get; set; } }

        private class BalanceRaw { public string Key { get; set; } public string Currency { get; set; } public string AktarimTipi { get; set; } public decimal TotalTL { get; set; } public decimal TotalFC { get; set; } }

        public IActionResult Index(MutabakatFilterModel filter)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Models.MutabakatViewModel>());
}

        [HttpPost]
        public IActionResult GetBalanceBreakdown(string cardCode, string endDate, List<string> selectedTypes, bool includeConnected)
 {return Json(global::WebApplication3.OrnekDoldurucu.Satirlar(12, new[] { "Kategori", "HesapKodu", "HesapAdi", "AktarimTipi", "Curr", "ToplamTL", "ToplamFC" }));
}




        [HttpPost]
        public IActionResult GetAccountDetails(string hesapKodu, string aktarimTipi, string kategori)
 {return Json(global::WebApplication3.OrnekDoldurucu.Liste<global::WebApplication3.Models.EkstreDetayItem>(12));
}





        public class SendMailRequest { public List<MutabakatListeItem> SelectedItems { get; set; } public MutabakatFilterModel Filters { get; set; } }

        [HttpPost]
        public IActionResult SendEmails([FromBody] SendMailRequest request)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private byte[] GeneratePdfBytes(string connectionString, string bpCode, DateTime? startDate, DateTime? endDate, List<string> aktarimTipi, bool includeInitialBalance, bool includeConnectedBp, string orientation)
 {return default;
}

        private Guid CreateMutabakatRecord(string connStr, string cardCode, string cardName, string email, decimal balance, string currency)  {return default;
}
        private void SendSmtpMail(string toEmail, string subject, string body, byte[] attachmentData, string attachmentName)  {}

        [AllowAnonymous]
        [HttpGet]
        public IActionResult MusteriCevap(string token)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("MusteriCevap", WebApplication3.OrnekDoldurucu.Yeni<WebApplication3.Models.MutabakatCevapModel>());
}

        [AllowAnonymous]
        [HttpPost]
        public IActionResult CevapKaydet(MutabakatCevapModel model)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        [Authorize]
        public IActionResult DurumRaporu(string cardCode)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("DurumRaporu", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Models.MutabakatSonucModel>(12));
}
    }
}