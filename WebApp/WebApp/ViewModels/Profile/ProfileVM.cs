using IdentityCore.Infrastructure;

namespace WebApp.ViewModels.Profile
{
    public class ProfileVM
    {
        public AppUser loggedin_user { get; set; }

        public AppUser user_viewing { get; set; }
        
    }
}
