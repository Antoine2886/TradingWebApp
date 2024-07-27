using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Account
{
    public class ForgotPasswordVM
    {
        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Email")]
        public string? UserName { get; set; }
    }
}
