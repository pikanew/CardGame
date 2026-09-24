using Game_project.Models;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Game_project.Data
{
    public class GameDbContext : DbContext
    {
        public GameDbContext(DbContextOptions<GameDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<PlayerStats> PlayerStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .HasOne(u => u.Stats)
                .WithOne(s => s.User)
                .HasForeignKey<PlayerStats>(s => s.UserId);
        }
    }
}
