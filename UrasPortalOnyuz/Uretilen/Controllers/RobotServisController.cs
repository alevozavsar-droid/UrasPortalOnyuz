// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.ServiceProcess;
using System.Threading.Tasks;

namespace WebApplication3.Controllers
{













    [Route("[controller]")]
    public class RobotServisController : Controller
    {








        private static readonly List<RobotServisi> Servisler = new List<RobotServisi>
        {
            new RobotServisi { ServisAdi = "UrasSapRobotService",    Baslik = "URAS Kimya Aktarım Robotu",     Panel = "Rapor137" },
            new RobotServisi { ServisAdi = "SelviSapRobotu",         Baslik = "SELVİ Kimya Aktarım Robotu",    Panel = "Rapor98"  },
            new RobotServisi { ServisAdi = "SASİATransfer",          Baslik = "ASIA Kimya Aktarım Robotu",     Panel = "Rapor99"  },
            new RobotServisi { ServisAdi = "AvrasyaSapRobotService", Baslik = "Avrasya Aktarım Robotu",        Panel = "Rapor144" },
            new RobotServisi { ServisAdi = "DafSapRobotService", Baslik = "Daf Aktarım Robotu (2026)", Panel = "Rapor153" },
            new RobotServisi { ServisAdi = "AvrupaPaperSapRobotService", Baslik = "AVRUPA PAPER Aktarım Robotu (2026)", Panel = "Rapor154" },
            new RobotServisi { ServisAdi = "UrasHoldingSapRobotService", Baslik = "URAS HOLDİNG Aktarım Robotu (2026)", Panel = "Rapor155" },
            new RobotServisi { ServisAdi = "AlvFiloSapRobotService", Baslik = "ALV FİLO Aktarım Robotu (2026)", Panel = "Rapor156" },
            new RobotServisi { ServisAdi = "UrasBaskiSapRobotService", Baslik = "URAS BASKI Aktarım Robotu (2026)", Panel = "Rapor157" },
            new RobotServisi { ServisAdi = "TestUrasKimyaAktarim",   Baslik = "İrsaliye / Fatura Aktarımı",    Panel = "Rapor145" },
            new RobotServisi { ServisAdi = "SelkimyaPurchaseRobot",  Baslik = "Satınalma Talebi Aktarımı",     Panel = "Rapor85"  },
            new RobotServisi { ServisAdi = "SAP_Kapama_Robotu",      Baslik = "Kapama Robotu",                 Panel = ""         },



            new RobotServisi { ServisAdi = "Service1",               Baslik = "ALV Kimya Aktarım Robotu",      Panel = ""         },
            new RobotServisi { ServisAdi = "Service2",               Baslik = "URS Makine Aktarım Robotu",     Panel = ""         }
        };

        public class RobotServisi
        {
            public string ServisAdi { get; set; }
            public string Baslik { get; set; }
            public string Panel { get; set; }
        }

        public IActionResult Index()  {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}


        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenSCManagerW(string makine, string veritabani, uint erisim);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr OpenServiceW(IntPtr scYoneticisi, string servisAdi, uint erisim);

        [DllImport("advapi32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseServiceHandle(IntPtr tutamac);

        private const uint SC_MANAGER_CONNECT = 0x0001;
        private const uint SERVICE_START = 0x0010;
        private const uint SERVICE_STOP = 0x0020;
        private const uint SERVICE_QUERY_STATUS = 0x0004;






        private static int YonetimYetkisiVarMi(string servisAdi)
 {return default;
}






        private static int Win32HataKodu(Exception ex)
 {return default;
}


        private static string HataAciklamasi(int kod, string servisAdi)
 {return default;
}




        [HttpGet("GetDurumlar")]
        public IActionResult GetDurumlar()
 {return Json(new { success = true, data = new object[0] });
}






        private static bool DurumuBekle(ServiceController sc, ServiceControllerStatus hedef, int saniye)
 {return default;
}




        [HttpPost("Islem")]
        public async Task<IActionResult> Islem(string servisAdi, string islem)
 {return Json(new { success = true, message = global::WebApplication3.OrnekDoldurucu.Deger<string>("message", 0) });
}
    }
}
