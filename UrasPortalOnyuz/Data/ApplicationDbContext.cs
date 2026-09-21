using System.Linq;
using WebApplication3.Controllers;
using WebApplication3.Models;

namespace WebApplication3.Data
{
    /// <summary>
    /// Ana projedeki EF DbContext'in bellek içi karşılığı: görünümler ve servis taklitleri aynı üye adlarını kullanır,
    /// veri Data/OrnekVeri.cs'den gelir. Hiçbir veritabanına bağlanmaz.
    /// </summary>
    public class ApplicationDbContext
    {
        public IQueryable<AppMenu> AppMenus => OrnekVeri.Menuler.AsQueryable();
        public IQueryable<AppMenuCategory> AppMenuCategories => OrnekVeri.Basliklar.AsQueryable();
        public IQueryable<AppDatabase> AppDatabases => OrnekVeri.Sirketler.AsQueryable();
        public IQueryable<AppRole> AppRoles => OrnekVeri.Roller.AsQueryable();
        public IQueryable<UserFavoriteMenu> UserFavoriteMenus => OrnekVeri.Favoriler.AsQueryable();
        public IQueryable<AppUserPermissionProfile> AppUserPermissionProfiles => Enumerable.Empty<AppUserPermissionProfile>().AsQueryable();
        public IQueryable<AppUserMenuPermission> AppUserMenuPermissions => Enumerable.Empty<AppUserMenuPermission>().AsQueryable();
        public IQueryable<AppUserDatabasePermission> AppUserDatabasePermissions => Enumerable.Empty<AppUserDatabasePermission>().AsQueryable();
        public int SaveChanges() => 0;
    }
}
