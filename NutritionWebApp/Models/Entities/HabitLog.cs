using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("HabitLogs")]
    public class HabitLog
    {
        [Key]
        public int LogId { get; set; }

        [ForeignKey("HabitId")]
        public int HabitId { get; set; }
        public virtual Habit Habit { get; set; } = null!;

        public DateTime LogDate { get; set; }
        public bool Completed { get; set; }
        public int? ActualValue { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
