using Azure.Identity;
using Bd.Infrastructure;
using IdentityCore.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IdentityCore.Infrastructure
{
    public class AppUser(string userName) : IdentityUser<Guid>(userName)
    {
        // public required string FirstName { get; set; }
        // public required string LastName { get; set; }
        // public string FullName => $"{FirstName} {LastName}";
        public required string VisibleName { get; set; }  // Actual username

        public ICollection<Post> Posts { get; set; } = new List<Post>();

        public ICollection<UserFriend> Friends { get; set; } = new List<UserFriend>(); 

        public string ProfilePicture { get; set; }

        // this will be a json object
        public List<string>? notifications { get; set; } = new List<string>();

        public DateTime joinedAt { get; set; }

        // public ICollection<Post> Followed_Posts { get; set; } = new List<Post>();
        // Balance and Orders
        public Balance Balance { get; set; } = new Balance();
        public ICollection<Order> Orders { get; set; } = new List<Order>();

        public Subscription Subscription { get; set; }
    }

}


public class UserFriend
{
    public Guid UserId { get; set; }
    public Guid FriendId { get; set; }
    public string Status { get; set; } // Pending, Accepted, Rejected

    public AppUser user { get; set; }

    public AppUser friend { get; set; }

}

// Enum to represent the status of the friendship
public enum FriendshipStatus
{
    Pending,
    Accepted,
    Rejected
}