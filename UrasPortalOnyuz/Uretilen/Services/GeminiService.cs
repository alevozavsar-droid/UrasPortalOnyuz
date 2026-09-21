// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace WebApplication3.Services
{
    public class GeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public GeminiService(HttpClient httpClient, IConfiguration configuration)
 {}

        public async Task<string> GenerateTextAsync(string prompt)
 {return default;
}

        private async Task<(bool Success, bool Is404, string ResponseText, string ErrorMessage)> TryGenerateContentAsync(string modelName, string prompt)
 {return default;
}

        private async Task<string> DiscoverAndGenerateAsync(string prompt)
 {return default;
}
    }
}