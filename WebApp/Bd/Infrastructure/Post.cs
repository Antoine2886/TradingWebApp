using Azure.Identity;
using Bd.Enums;
using IdentityCore.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Bd.Infrastructure
{
    public class Post
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public Guid UserId { get; set; }
        public virtual Stock? Stock { get; set; } = null;

   // [ForeignKey("UserName")]
        public virtual AppUser? User { get; set; }
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

      //[NotMapped]
        public List<Like>? Likes { get; set; } = new List<Like>();

      
      
        // Add a foreign key for ChartSettings
        public Guid? ChartSettingsId { get; set; }
        public virtual ChartSettings ChartSettings { get; set; }
        // Add the PostType enum property
        public PostType? Type { get; set; }
    }

    public class Like
    {
        public Guid Id { get; set; }

        [ForeignKey("Post")]
        public Guid PostId { get; set; }
        public virtual Post Post { get; set; }

        [ForeignKey("User")]
        public Guid UserId { get; set; }
        public virtual AppUser User { get; set; }
    }


}
