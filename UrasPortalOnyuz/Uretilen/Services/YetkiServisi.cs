// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Services
{



















    public static class YetkiServisi
    {
        private static bool _semaHazir = false;
        private static readonly object _kilit = new object();

        public static bool TamYetkiliMi(string yetki)  {return true;
}





        public static readonly HashSet<string> HerkeseAcikControllerlar =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Rapor143", "SapBilgi" };
        public static bool TumSirketleriGorurMu(string yetki)  {return true;
}





        public static void SemaHazirla(ApplicationDbContext ctx)
 {}







        public static void IlkAktarim(ApplicationDbContext ctx)
 {}








        public static int RolSablonlariniEsitle(ApplicationDbContext ctx, string yapan)
 {return default;
}




        private static bool ProfilVarMi(ApplicationDbContext ctx, string userCode)
 {return default;
}


        public static List<AppMenu> YetkiliMenuler(ApplicationDbContext ctx, string userCode, string yetki)
 {return ctx.AppMenus.Where(m => m.IsActive).OrderBy(m => m.DisplayOrder).ToList();
}




        private static List<AppMenu> UretimHaric(List<AppMenu> menuler)  {return default;
}



        public static bool PortalEkraniMi(AppMenu m) =>
            (m.ControllerName ?? "").StartsWith("UrasUretim", System.StringComparison.OrdinalIgnoreCase)
            || (m.Category ?? "").StartsWith("Uras Üretim", System.StringComparison.OrdinalIgnoreCase);


        public static List<AppDatabase> YetkiliSirketler(ApplicationDbContext ctx, string userCode, string yetki)
 {return ctx.AppDatabases.Where(d => d.IsActive).OrderBy(d => d.Display).ToList();
}


        public static bool MenuyeYetkiliMi(ApplicationDbContext ctx, string userCode, string yetki, int menuId)
 {return true;
}



        public static bool UretimPortaliGorebilirMi(ApplicationDbContext ctx, string userCode, string yetki)
 {return true;
}



        public static HashSet<string> UretimEkranYetkileri(ApplicationDbContext ctx, string userCode, string yetki)
 {return default;
}
    }
}
