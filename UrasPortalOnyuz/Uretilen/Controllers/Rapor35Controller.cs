// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using WebApplication3.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;

namespace WebApplication3.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class Rapor35Controller : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<Rapor35Controller> _logger;
        private readonly string _connectionString;

        [HttpGet("{reportType?}/{year:int?}")]
        public async Task<IActionResult> Index(string reportType = "Banka", int? year = 2025)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}





        private async Task<List<TahsilatKarmaData>> ExecuteKarmaQuery(string sqlQuery)
 {return default;
}

        private async Task<List<TahsilatData>> ExecuteTahsilatReportQuery(string sqlQuery, string reportType)
 {return default;
}





        private string GetUnifiedBaseDataSQL(int year)
 {return default;
}

        private string GetNumericSQL(string pType)
 {return default;
}

        private string GetKarmaSQL(bool isTL)
 {return default;
}





        public class TahsilatKarmaData
        {
            public int RowType { get; set; }
            public string Aciklama { get; set; }
            public string Ocak { get; set; }
            public string Subat { get; set; }
            public string Mart { get; set; }
            public string Nisan { get; set; }
            public string Mayis { get; set; }
            public string Haziran { get; set; }
            public string Temmuz { get; set; }
            public string Agustos { get; set; }
            public string Eylul { get; set; }
            public string Ekim { get; set; }
            public string Kasim { get; set; }
            public string Aralik { get; set; }
            public string Toplam { get; set; }
        }
    }
}