// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WebApplication3.Services
{














    public class PlanliOdemeZamanlayici : BackgroundService
    {
        private readonly IServiceProvider _saglayici;
        private readonly ILogger<PlanliOdemeZamanlayici> _gunluk;
        private static readonly TimeSpan Aralik = TimeSpan.FromHours(1);

        public PlanliOdemeZamanlayici(IServiceProvider saglayici, ILogger<PlanliOdemeZamanlayici> gunluk)
 {}

        protected override async Task ExecuteAsync(CancellationToken iptal)
 {}
    }
}
