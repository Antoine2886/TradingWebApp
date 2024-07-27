using IdentityCore.Infrastructure;

namespace WebApp.ViewModels.Settings
{
    public class BillingVM
    {
        public AppUser user { get; set; }

        public List<string>? subscription_options_name;
        public List<double>? subscription_options_price;
        public List<string>? subscription_options_description;
        public List<string>? subscription_options_priceId;
    }
}
