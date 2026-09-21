// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{
    public class PendingNoteViewModel
    {
        public int Id { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string SirketDb { get; set; }
        public string Kullanici { get; set; }
        public string NotIcerigi { get; set; }
        public DateTime KayitTarihi { get; set; }
        public string AtananKisiMail { get; set; }
        public string RoutingGroup { get; set; }
        public string IslemNotu { get; set; }
    }

    public class CompleteRequestModel
    {
        public List<int> SelectedIds { get; set; }
        public string UserNote { get; set; }
        public string ActionType { get; set; } // "COMPLETE" veya "FORWARD"
        public string ForwardEmail { get; set; } // Yönlendirilen yeni kişinin maili
    }

    [Authorize]
    [Route("[controller]")]
    public class Rapor133Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor133Controller> _logger;
        private readonly string _merkeziKuyrukDbKey = "DefaultConnection5";

        [HttpGet]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index", WebApplication3.OrnekDoldurucu.Liste<WebApplication3.Controllers.PendingNoteViewModel>(12));
}

        private List<PendingNoteViewModel> GetPendingNotes()
 {return default;
}

        private string GetCardNameFromCompanyDb(string sirketDb, string cardCode)
 {return default;
}

        private string GetRoutingGroup(string sirketDb)
 {return default;
}

        [HttpPost("ProcessNotes")]
        public async Task<IActionResult> ProcessNotes([FromBody] CompleteRequestModel req)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}

        private string BuildMailTableRows(List<PendingNoteViewModel> notes)
 {return default;
}

        [HttpPost("SendBulkEmails")]
        public async Task<IActionResult> SendBulkEmails([FromBody] List<int> selectedIds)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}
    }
}