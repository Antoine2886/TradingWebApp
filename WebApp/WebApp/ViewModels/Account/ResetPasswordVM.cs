using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Account
{
    public class ResetPasswordVM
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Password do not match")]
        public string? ConfirmPassword { get; set; }
        public string? UserId { get;  set; }
        public string? Token { get;  set; }
        public string? Token2 { get; set; }
    }
}
