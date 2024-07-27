using IdentityCore.Infrastructure;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace Bd.Infrastructure
{
    public class Context : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
    {

        // Add DbSet properties for other entities as needed
        public DbSet<Token> Tokens { get; set; }
         public DbSet<Stock> Stocks { get; set; }
         public DbSet<StockData> StockData { get; set; }
         public DbSet<Post> Posts { get; set; }
         public DbSet<Order> Orders { get; set; }
         public DbSet<Balance> Balances { get; set; }

        public DbSet<Message> Messages { get; set; }
        public DbSet<ChartSettings> ChartSettings { get; set; }
        public DbSet<Drawing> Drawings { get; set; }
        public Context(DbContextOptions<Context> options) : base(options) { }

        public DbSet<UserFriend> Friends { get; set; }

        public DbSet<Like> likes { get; set; }

       public DbSet<Subscription> Subscriptions { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (Environment.GetEnvironmentVariable("TEST_ENVIRONMENT") == "true")
            {
                return SaveChanges();
            }
            return await base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //  modelBuilder.Entity<AppUser>()
            //   .HasQueryFilter(u => !u.IsDeleted);

            Configuring_Relationships(modelBuilder);

            modelBuilder.Entity<AppUser>()
              .HasIndex(u => u.VisibleName) // Create index on Post column
               .HasDatabaseName("idx_visibleName");


            modelBuilder.Entity<Like>()
                .HasOne(l => l.Post)
                .WithMany(p => p.Likes)
                .HasForeignKey(l => l.PostId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Like>()
                .HasOne(l => l.User)
                .WithMany()
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Stock>()
                .HasIndex(s => s.Name)
                .IsUnique();

            modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Subscription)
            .WithOne(s => s.User)
            .HasForeignKey<Subscription>(s => s.UserId);

        }

        public void Configuring_Relationships(ModelBuilder modelBuilder)
        {

            User_and_Friends(modelBuilder);
            Posts_and_Messages(modelBuilder);

        }

        public void User_and_Friends(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserFriend>()
            .HasKey(uf => new { uf.UserId, uf.FriendId, uf.Status });

            modelBuilder.Entity<UserFriend>()
                .HasOne(uf => uf.user)
                .WithMany(u => u.Friends)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFriend>()
                .HasOne(uf => uf.friend)
                .WithMany()
                .HasForeignKey(uf => uf.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UserFriend>()
                .Property(uf => uf.Status)
                .HasDefaultValue("Pending");
        }

        public void Posts_and_Messages(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Post>()
                    .HasOne(p => p.User)
                    .WithMany(u => u.Posts)
                    .HasForeignKey(p => p.UserId)
                    .OnDelete(DeleteBehavior.Cascade); // When User is deleted, delete all associated Posts

            modelBuilder.Entity<Message>()
                .HasOne(m => m.Post)
                .WithMany(p => p.Messages)
                .HasForeignKey(m => m.PostId)
                .OnDelete(DeleteBehavior.Cascade); // When Post is deleted, delete all associated Messages

            modelBuilder.Entity<Message>()
                .HasOne(m => m.User)
                .WithMany()
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade); // When User is deleted, delete all associated Messages

            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasIndex(s => s.VisibleName)
                .IsUnique();
        }


    }
}
