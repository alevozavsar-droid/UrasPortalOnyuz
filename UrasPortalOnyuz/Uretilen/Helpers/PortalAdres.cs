using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace WebApplication3.Helpers
{






    public static class PortalAdres
    {
        public static string Al(HttpRequest istek, IConfiguration ayar)
        {
            string ayarli = ayar?["Portal:Url"];
            if (!string.IsNullOrWhiteSpace(ayarli)) return ayarli.TrimEnd('/');

            if (istek != null && istek.Host.HasValue)
                return $"{istek.Scheme}://{istek.Host}{istek.PathBase}".TrimEnd('/');

            return "https://localhost:44338";
        }
    }
}
