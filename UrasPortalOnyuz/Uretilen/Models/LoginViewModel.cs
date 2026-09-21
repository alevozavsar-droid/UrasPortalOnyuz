using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Kullanıcı Kodu alanı zorunludur.")]
        [Display(Name = "Kullanıcı Kodu")]
        public string UserCode { get; set; }

        [Required(ErrorMessage = "Şifre alanı zorunludur.")] // Geri eklendi
        [DataType(DataType.Password)]
        [Display(Name = "Şifre")]
        [StringLength(100, MinimumLength = 1, ErrorMessage = "Şifre en az {2} karakter uzunluğunda olmalıdır.")]
        public string Password { get; set; }

        [Display(Name = "Beni Hatırla")]
        public bool RememberMe { get; set; }
    }
}
