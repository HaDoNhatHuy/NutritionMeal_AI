using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("WaterIntake")]
    public class WaterIntake
    {
        [Key]
        public int IntakeId { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public DateTime IntakeDate { get; set; }
        public int TotalMl { get; set; }
        public int GoalMl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<WaterLog> WaterLogs { get; set; } = new List<WaterLog>();
    }
}
