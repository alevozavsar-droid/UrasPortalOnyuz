// <ornek-uretildi/> Bu dosya ana projeden OTOMATIK uretildi: govdeler sokuldu, baglanti yok.
#pragma warning disable
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using WebApplication3.Data;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Reflection;

namespace WebApplication3.Models
{
    [Table("App_Roles")]
    public class AppRole
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string RoleCode { get; set; }

        [Required, StringLength(100)]
        public string RoleName { get; set; }

        [StringLength(255)]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<AppRoleMenuPermission> RoleMenuPermissions { get; set; }
        public virtual ICollection<AppRoleDatabasePermission> RoleDatabasePermissions { get; set; }
    }

    [Table("App_Menus")]
    public class AppMenu
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Category { get; set; }

        [Required, StringLength(150)]
        public string MenuTitle { get; set; }

        [Required, StringLength(100)]
        public string ControllerName { get; set; }

        [Required, StringLength(100)]
        public string ActionName { get; set; }

        [StringLength(100)]
        public string Icon { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<AppRoleMenuPermission> RoleMenuPermissions { get; set; }
    }

    [Table("App_RoleMenuPermissions")]
    public class AppRoleMenuPermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int MenuId { get; set; }

        [StringLength(100)]
        public string AssignedBy { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [ForeignKey("RoleId")]
        public virtual AppRole Role { get; set; }

        [ForeignKey("MenuId")]
        public virtual AppMenu Menu { get; set; }
    }

    [Table("App_Databases")]
    public class AppDatabase
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string DbKey { get; set; }

        [Required, StringLength(100)]
        public string Display { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public virtual ICollection<AppRoleDatabasePermission> RoleDatabasePermissions { get; set; }
    }




    [Table("App_UserPermissionProfiles")]
    public class AppUserPermissionProfile
    {
        [Key, StringLength(50)]
        public string UserCode { get; set; }


        [Required, StringLength(20)]
        public string Kaynak { get; set; } = "ROL";

        [StringLength(50)]
        public string RolKodu { get; set; }

        public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;
        public DateTime? GuncellemeTarihi { get; set; }

        [StringLength(100)]
        public string Guncelleyen { get; set; }
    }

    [Table("App_UserMenuPermissions")]
    public class AppUserMenuPermission
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string UserCode { get; set; }

        [Required]
        public int MenuId { get; set; }

        [StringLength(100)]
        public string AssignedBy { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [ForeignKey("MenuId")]
        public virtual AppMenu Menu { get; set; }
    }

    [Table("App_UserDatabasePermissions")]
    public class AppUserDatabasePermission
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(50)]
        public string UserCode { get; set; }

        [Required]
        public int DatabaseId { get; set; }

        [StringLength(100)]
        public string AssignedBy { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [ForeignKey("DatabaseId")]
        public virtual AppDatabase Database { get; set; }
    }





    [Table("App_MenuCategories")]
    public class AppMenuCategory
    {
        [Key]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(100)]
        public string Icon { get; set; }


        [StringLength(100)]
        public string UstGrup { get; set; }

        public int DisplayOrder { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }

    [Table("App_RoleDatabasePermissions")]
    public class AppRoleDatabasePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int DatabaseId { get; set; }

        [StringLength(100)]
        public string AssignedBy { get; set; }

        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [ForeignKey("RoleId")]
        public virtual AppRole Role { get; set; }

        [ForeignKey("DatabaseId")]
        public virtual AppDatabase Database { get; set; }
    }

    public class SapUserDto
    {
        public int InternalKey { get; set; }
        public string UserCode { get; set; }
        public string UserName { get; set; }
        public string eMail { get; set; }
        public string U_BE1_PASSWORD { get; set; }
        public string U_BE1_YETKI { get; set; }

        public string Locked { get; set; }
    }
}

namespace WebApplication3.Controllers
{
    using WebApplication3.Models;
}