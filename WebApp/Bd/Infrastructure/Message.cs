using IdentityCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bd.Infrastructure
{
    public class Message
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; }

        public Post Post { get; set; }

        public Guid PostId { get; set; }
        public Guid UserId { get; set; }
        public virtual AppUser? User { get; set; }
        
        
    }
}
