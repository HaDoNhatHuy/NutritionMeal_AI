using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("Habits")]
    public class Habit
    {
        [Key]
        public int HabitId { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string HabitName { get; set; } = string.Empty;
        public string HabitType { get; set; } = string.Empty; // Water, Sleep, Exercise
        public int? GoalValue { get; set; }
        public string? IconEmoji { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;

        public virtual ICollection<HabitLog> HabitLogs { get; set; } = new List<HabitLog>();
    }
}
