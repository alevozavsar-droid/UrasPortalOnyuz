using System.ComponentModel.DataAnnotations;

namespace WebApplication3.Models
{
    public class SetPasswordViewModel
    {
        [Required(ErrorMessage = "Kullanıcı Kodu alanı zorunludur.")]
        [Display(Name = "Kullanıcı Kodu")]
        public string UserCode { get; set; }


        [DataType(DataType.Password)]
        [Display(Name = "Eski Şifre")]

        public string OldPassword { get; set; }

        [Required(ErrorMessage = "Yeni Şifre alanı zorunludur.")]
        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Yeni şifre en az {2} karakter uzunluğunda olmalıdır.")] // Minimum uzunluk 6 yapıldı
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Yeni Şifre Tekrar")]
        [Compare("NewPassword", ErrorMessage = "Şifreler eşleşmiyor.")]
        public string ConfirmPassword { get; set; }
    }
}
