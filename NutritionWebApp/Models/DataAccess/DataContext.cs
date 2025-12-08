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
        public DbSet<ChatHistory> ChatHistory { get; set; }
        // NEW: Workout Planner
        public DbSet<WorkoutPlan> WorkoutPlans { get; set; }
        public DbSet<WorkoutProgress> WorkoutProgress { get; set; }

        // NEW: Meal Planner
        public DbSet<MealPlan> MealPlans { get; set; }

        // NEW: Water Tracker
        public DbSet<WaterIntake> WaterIntake { get; set; }
        public DbSet<WaterLog> WaterLogs { get; set; }

        // NEW: Body Measurements
        public DbSet<BodyMeasurement> BodyMeasurements { get; set; }

        // NEW: Habit Tracker
        public DbSet<Habit> Habits { get; set; }
        public DbSet<HabitLog> HabitLogs { get; set; }

        // NEW: Recipes
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<RecipeLike> RecipeLikes { get; set; }
        public DbSet<RecipeReview> RecipeReviews { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<FoodHistory>()
                .HasOne(f => f.User)
                .WithMany(u => u.FoodHistories)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            // Workout Plan relationships
            modelBuilder.Entity<WorkoutPlan>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Bổ sung: Định nghĩa mối quan hệ giữa WorkoutProgress và User
            // để ngăn chặn chu trình xóa dây chuyền kép.
            modelBuilder.Entity<WorkoutProgress>()
                .HasOne(wp => wp.User)
                .WithMany()
                .HasForeignKey(wp => wp.UserId)
                // QUAN TRỌNG: Đặt thành Restrict để phá vỡ chu trình
                // Khi User bị xóa, những bản ghi WorkoutProgress trực tiếp này 
                // sẽ không bị xóa nếu chúng vẫn còn PlanId hợp lệ.
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<WorkoutProgress>()
                .HasOne(w => w.WorkoutPlan)
                .WithMany()
                .HasForeignKey(w => w.PlanId)
                .OnDelete(DeleteBehavior.Cascade);

            // Water Intake relationships
            modelBuilder.Entity<WaterIntake>()
                .HasOne(w => w.User)
                .WithMany()
                .HasForeignKey(w => w.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<WaterLog>()
                .HasOne(w => w.WaterIntake)
                .WithMany(i => i.WaterLogs)
                .HasForeignKey(w => w.IntakeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Habit relationships
            modelBuilder.Entity<Habit>()
                .HasOne(h => h.User)
                .WithMany()
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<HabitLog>()
                .HasOne(h => h.Habit)
                .WithMany(hab => hab.HabitLogs)
                .HasForeignKey(h => h.HabitId)
                .OnDelete(DeleteBehavior.Cascade);

            // Recipe relationships
            modelBuilder.Entity<Recipe>()
                .HasOne(r => r.CreatedBy)
                .WithMany()
                .HasForeignKey(r => r.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<RecipeLike>()
                .HasOne(r => r.Recipe)
                .WithMany(rec => rec.RecipeLikes)
                .HasForeignKey(r => r.RecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraints
            modelBuilder.Entity<WaterIntake>()
                .HasIndex(w => new { w.UserId, w.IntakeDate })
                .IsUnique();

            modelBuilder.Entity<HabitLog>()
                .HasIndex(h => new { h.HabitId, h.LogDate })
                .IsUnique();

            modelBuilder.Entity<RecipeLike>()
                .HasIndex(r => new { r.RecipeId, r.UserId })
                .IsUnique();

            // Cấu hình RecipeReview (F12)
            modelBuilder.Entity<RecipeReview>()
                .HasOne(r => r.Recipe)
                .WithMany() // Nếu Recipe Entity không có List<RecipeReview>
                .HasForeignKey(r => r.RecipeId)
                .OnDelete(DeleteBehavior.Cascade); // Xóa review khi công thức bị xóa

            modelBuilder.Entity<RecipeReview>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Giữ user nếu review bị xóa

            // UNIQUE INDEX: Đảm bảo mỗi người dùng chỉ đánh giá 1 lần/công thức
            modelBuilder.Entity<RecipeReview>()
                .HasIndex(r => new { r.RecipeId, r.UserId })
                .IsUnique();
        }
    }
}
