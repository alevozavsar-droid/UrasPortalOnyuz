using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace WebApplication3
{
    /// <summary>
    /// Ön yüz örneği: kimlik doğrulama çerez tabanlı ama şifre denetimi yok (her kullanıcı kodu / şifre kabul edilir),
    /// veritabanı yok, tüm veriler bellekteki örnek listelerden gelir.
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration) { Configuration = configuration; }
        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddSession(o => { o.IdleTimeout = TimeSpan.FromHours(8); o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; });
            services.AddScoped<Data.ApplicationDbContext>();   // bellek içi örnek "veritabanı"
            services.AddControllersWithViews(o => o.MaxModelBindingCollectionSize = 5000)
                    .AddRazorRuntimeCompilation();
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(o =>
                {
                    o.LoginPath = "/Account/Login"; o.LogoutPath = "/Account/Logout"; o.AccessDeniedPath = "/Account/AccessDenied";
                    o.ExpireTimeSpan = TimeSpan.FromDays(7); o.SlidingExpiration = true;
                    o.Events.OnRedirectToLogin = ctx => { if (AjaxMi(ctx.Request)) { ctx.Response.StatusCode = 401; return Task.CompletedTask; } ctx.Response.Redirect(ctx.RedirectUri); return Task.CompletedTask; };
                });
            services.AddAuthorization();
        }

        private static bool AjaxMi(HttpRequest r) => r.Headers["X-Requested-With"] == "XMLHttpRequest" || (r.Headers["Accept"].ToString() ?? "").Contains("application/json");

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment()) app.UseDeveloperExceptionPage(); else app.UseExceptionHandler("/Home/Error");

            var kultur = new CultureInfo("tr-TR");
            kultur.NumberFormat.NumberDecimalSeparator = "."; kultur.NumberFormat.CurrencyDecimalSeparator = "."; kultur.NumberFormat.NumberGroupSeparator = ",";
            app.UseRequestLocalization(new RequestLocalizationOptions { DefaultRequestCulture = new RequestCulture(kultur), SupportedCultures = new List<CultureInfo> { kultur }, SupportedUICultures = new List<CultureInfo> { kultur } });

            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            // Örnek projede bulunmayan AJAX uçları 404 yerine anlaşılır bir JSON döner (ekranlar kilitlenmez)
            app.Use(async (ctx, next) =>
            {
                await next();
                if (ctx.Response.StatusCode == 404 && !ctx.Response.HasStarted && AjaxMi(ctx.Request))
                {
                    ctx.Response.StatusCode = 200; ctx.Response.ContentType = "application/json; charset=utf-8";
                    await ctx.Response.WriteAsync("{\"success\":false,\"message\":\"Bu işlem yalnızca ön yüz örneğinde yok (veritabanı bağlantısı gerektirir).\",\"data\":[]}");
                }
            });

            app.UseEndpoints(e =>
            {
                e.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
