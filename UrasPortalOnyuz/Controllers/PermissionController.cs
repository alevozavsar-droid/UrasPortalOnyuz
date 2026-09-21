using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using WebApplication3.Data;
using WebApplication3.Models;

namespace WebApplication3.Controllers
{
    /// <summary>Sistem Yetkileri — listeler örnek; kaydetme işlemleri bellek içi ya da bilgilendirme amaçlı.</summary>
    [Authorize]
    public class PermissionController : Controller
    {
        public static void KategoriSemasiHazirla(ApplicationDbContext ctx) { }

        /// <summary>Veritabanı adından görünen ad önerisi (ana projedeki kuralın sadeleştirilmiş hali).</summary>
        public static string SirketAdiOner(string dbAdi)
        {
            if (string.IsNullOrWhiteSpace(dbAdi)) return "";
            string s = dbAdi.Trim(); bool asMi = Regex.IsMatch(s, "_A\\.?S$|_AS$|2026$", RegexOptions.IgnoreCase);
            s = Regex.Replace(s, "_A\\.?S$|_AS$|_2026$", "", RegexOptions.IgnoreCase).Replace('_', ' ').ToLowerInvariant();
            s = string.Join(" ", s.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
            return asMi ? s + " A.Ş." : s;
        }

        public IActionResult Index()
        {
            var menuler = OrnekVeri.Menuler.Where(m => m.IsActive).ToList();
            ViewBag.Roles = OrnekVeri.Roller;
            ViewBag.Databases = OrnekVeri.Sirketler.Where(d => d.IsActive).OrderBy(d => d.Display).ToList();
            ViewBag.TumSirketler = OrnekVeri.Sirketler.OrderBy(d => d.Display).ToList();
            ViewBag.SirketDbAdlari = OrnekVeri.Sirketler.ToDictionary(s => s.DbKey, s => s.Display.ToUpperInvariant().Replace(' ', '_').Replace("A.Ş.", "AS"));
            ViewBag.MenuCategories = OrnekVeri.Basliklar;
            ViewBag.Categories = OrnekVeri.Basliklar.Select(b => b.Name).ToList();
            return View(menuler.GroupBy(m => m.Category).ToDictionary(g => g.Key, g => g.OrderBy(m => m.DisplayOrder).ToList()));
        }

        [HttpGet] public IActionResult KisiListesi() => Json(new { success = true, data = OrnekVeri.Kullanicilar.Select(k => new { userCode = k.Kod, userName = k.Ad, rolKodu = k.Yetki, kaynak = "ROL", menuSayisi = OrnekVeri.Menuler.Count, sirketSayisi = OrnekVeri.Sirketler.Count, email = k.Kod + "@ornek.local" }) });
        [HttpGet] public IActionResult KisiYetkileri(string userCode) => Json(new { success = true, userCode, kaynak = "ROL", rolKodu = OrnekVeri.Kullanicilar.FirstOrDefault(k => k.Kod == userCode).Yetki, menuler = OrnekVeri.Menuler.Select(m => m.Id), sirketler = OrnekVeri.Sirketler.Select(s => s.Id) });
        [HttpGet] public IActionResult GetSapUsers() => Json(OrnekVeri.Kullanicilar.Select(k => new { userCode = k.Kod, userName = k.Ad, email = k.Kod + "@ornek.local", locked = false, yetki = k.Yetki }));
        [HttpGet] public IActionResult GetUnregisteredReports() => Json(new { success = true, data = new object[0] });
        [HttpGet] public IActionResult RaporKullanicilari() => Json(new { success = true, data = OrnekVeri.Kullanicilar.Select(k => new { userCode = k.Kod, userName = k.Ad, yetkili = true }) });
        [HttpGet] public IActionResult MenuDuzeniOnizle() => Json(new { success = true, degisiklik = new object[0], mesaj = "Örnek projede menü düzeni zaten önerilen yapıda." });
        [HttpPost] public IActionResult KisiYetkileriKaydet() => Ornek();
        [HttpPost] public IActionResult KisidenKopyala() => Ornek();
        [HttpPost] public IActionResult RaporKullaniciAyarla() => Ornek();
        [HttpPost] public IActionResult SaveCategory() => Ornek();
        [HttpPost] public IActionResult RenameCategory() => Ornek();
        [HttpPost] public IActionResult DeleteCategory() => Ornek();
        [HttpPost] public IActionResult UpdateMenu() => Ornek();
        [HttpPost] public IActionResult DeleteMenu() => Ornek();
        [HttpPost] public IActionResult ToggleMenuActive() => Ornek();
        [HttpPost] public IActionResult AddNewReport() => Ornek();
        [HttpPost] public IActionResult AddDetectedReports() => Ornek();
        [HttpPost] public IActionResult UpdateDatabaseDisplay() => Ornek();
        [HttpPost] public IActionResult SirketAktifDegistir() => Ornek();
        [HttpPost] public IActionResult SirketAdlariniDoldur() => Ornek();
        [HttpPost] public IActionResult SyncDatabasesFromConfig() => Ornek();
        [HttpPost] public IActionResult MenuDuzeniUygula() => Ornek();
        [HttpPost] public IActionResult DeleteSapUser() => Ornek();
        [HttpPost] public IActionResult SetSapUserLocked() => Ornek();
        private IActionResult Ornek() => Json(new { success = true, message = "Ön yüz örneği: değişiklik kalıcı değildir (veritabanı yok)." });
    }

    /// <summary>WhatsApp sekmesi (Sistem Yetkileri içinde) için örnek uçlar.</summary>
    [Authorize]
    [Route("[controller]")]
    public class WhatsAppController : Controller
    {
        private static readonly List<object> _liste = new List<object> { new { telefon = "905320000001", userCode = "sat5", ad = "Tolga Yaman", aktif = true, varsayilanDbKey = "DefaultConnection", lid = "" } };
        [HttpGet("Liste")] public IActionResult Liste() => Json(new { success = true, liste = _liste, kullanicilar = OrnekVeri.Kullanicilar.Select(k => new { kod = k.Kod, ad = k.Ad }), anahtarTanimli = true });
        [HttpGet("Loglar")] public IActionResult Loglar() => Json(new { success = true, loglar = new object[] { new { tarih = DateTime.Now.AddMinutes(-20), telefon = "905320000001", mesaj = "ekstre aksa", cevap = "AKSA TEKSTİL … ekstre ektedir.", dosyaVar = true } } });
        [HttpPost("Kaydet")] public IActionResult Kaydet() => Json(new { success = true });
        [HttpPost("AktifDegistir")] public IActionResult AktifDegistir() => Json(new { success = true });
        [HttpPost("Sil")] public IActionResult Sil() => Json(new { success = true });
        [HttpPost("Simule")] public IActionResult Simule(string mesaj) => Json(new { success = true, cevap = "Ön yüz örneği asistanı: \"" + (mesaj ?? "") + "\" mesajını aldım. Cari adı yazınca liste, bakiye ya da ekstre gelir.", dosyaAdi = (string)null, dosyaBoyut = 0, secenekler = new { baslik = "Ne istiyorsun?", secimler = new object[] { new { etiket = "💰 Bakiye", komut = "bakiye" }, new { etiket = "📄 Ekstre", komut = "ekstre_devam" } } } });
    }
}
