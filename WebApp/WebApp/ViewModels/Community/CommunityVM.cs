using Bd.Infrastructure;
using IdentityCore.Infrastructure;

namespace WebApp.ViewModels.Community
{
    public class CommunityVM
    {
        public AppUser User { get; set; }
        public IEnumerable<Post> Posts { get; set; }
    }

}