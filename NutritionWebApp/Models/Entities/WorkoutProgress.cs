using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("WorkoutProgress")]
    public class WorkoutProgress
    {
        [Key]
        public int ProgressId { get; set; }

        [ForeignKey("PlanId")]
        public int PlanId { get; set; }
        public virtual WorkoutPlan WorkoutPlan { get; set; } = null!;

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public int WeekNumber { get; set; }
        public int DayNumber { get; set; }
        public string ExerciseName { get; set; } = string.Empty;
        public int SetsCompleted { get; set; }
        public string? RepsCompleted { get; set; }
        public string? WeightUsed { get; set; }
        public bool Completed { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }
        public int? Rating { get; set; }
    }
}
