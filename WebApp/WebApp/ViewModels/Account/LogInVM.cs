using System.ComponentModel.DataAnnotations;

namespace IdentityCore.ViewModels.Account
{
    public class LogInVM
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Email")]

        public string? UserName { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string? Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; } = false;
    }
}
