using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    /// <summary>Ön yüz örneği: şifre denetimi yok, girilen kullanıcı kodu ile oturum açılır (ör. it02 / herhangi bir şifre).</summary>
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            if (User?.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel { UserCode = "it02" });
        }

        [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            if (string.IsNullOrWhiteSpace(model?.UserCode)) { ModelState.AddModelError("", "Kullanıcı kodu girin."); return View(model); }
            string kod = model.UserCode.Trim().ToLowerInvariant();
            var claims = new List<Claim> { new Claim("User", kod), new Claim(ClaimTypes.Name, kod), new Claim(ClaimTypes.Role, "User"), new Claim("UserAuthority", "A") };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)), new AuthenticationProperties { IsPersistent = model.RememberMe });
            if (string.IsNullOrEmpty(Request.Cookies["SelectedDatabase"])) Response.Cookies.Append("SelectedDatabase", OrnekVeri.Sirketler[0].DbKey);
            return Redirect(string.IsNullOrEmpty(returnUrl) ? "/Home/Index" : returnUrl);
        }

        public async Task<IActionResult> Logout() { await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme); return RedirectToAction("Login"); }
        [AllowAnonymous] public IActionResult AccessDenied() => View();
        [AllowAnonymous] public IActionResult SetPassword() => Content("Ön yüz örneğinde şifre değiştirme yok.");
    }
}
