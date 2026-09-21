// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using WebApplication3.Services;

namespace WebApplication3.Controllers
{
    public class GeminiController : Controller
    {
        private readonly YapayZekaIstemcisi _yapayZeka;

        [HttpGet]
        public IActionResult Index()
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(string prompt)
 {WebApplication3.OrnekDoldurucu.Hazirla(this);
return View("Index");
}
    }
}