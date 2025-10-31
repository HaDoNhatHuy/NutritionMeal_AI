using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.Entities;
using System;

namespace NutritionWebApp.Models.DataAccess
{
    public class DataContext: DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<FoodHistory> FoodHistory { get; set; }
        public DbSet<ExerciseVideo> ExerciseVideos { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FoodHistory>()
                .HasOne(f => f.User)
                .WithMany(u => u.FoodHistories)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
