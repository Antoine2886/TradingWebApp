using IdentityCore.Infrastructure;

namespace WebApp.ViewModels.Settings
{
    public class UserSettingsViewModel
    {
        public string newUserName { get; set; }
        public IFormFile? profilePicture { get; set; }
        public string ExistingAvatarPath { get; set; }

        public AppUser user { get; set; }


    }
}
