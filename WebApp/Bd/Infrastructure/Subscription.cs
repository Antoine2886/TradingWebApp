using IdentityCore.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bd.Infrastructure
{
    public class Subscription
    {
        public Guid Id { get; set; }

        public int ActualID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public double Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public virtual AppUser User { get; set; }

        public Guid UserId { get; set; }
    }
}
