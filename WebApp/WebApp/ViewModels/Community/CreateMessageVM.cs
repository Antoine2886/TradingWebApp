using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace WebApp.ViewModels.Community
{
    public class CreateMessageVM
    {
        [Required(ErrorMessage = "The Message field is required.")]
        public string text { get; set; }

        //public AppUser User { get; set; }

        public Post post { get; set; }

        public AppUser user { get; set; }

    }
}