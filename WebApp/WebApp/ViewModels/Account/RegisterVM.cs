using System.ComponentModel.DataAnnotations;
namespace IdentityCore.ViewModels.Account
{
    public class RegisterVM
    {
      //  [Required]
      //  [Display(Name = "First Name")]
      //  public string? FirstName { get; set; }
       // [Required]
       // [Display(Name = "Last name")]

       // public string? LastName { get; set; }
        [Required]
        [Display(Name = "Email")]
        public string? Email { get; set; }

        [Required]
        [DataType(DataType.Text)]
        [Display(Name = "Username")]
        public string? VisibleName { get; set; }

        public string? Password { get; set; }
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        [Compare("Password", ErrorMessage = "Password do not match")]
        public string? ConfirmPassword { get; set; }
        [Required]
        [Display(Name = "Terms Accepted")]
        public bool TermsAccepted { get; set; }

        //[Required]
        //public string RecaptchaResponse { get; set; } // Add this property




    }
}
