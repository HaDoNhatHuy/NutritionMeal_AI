using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("WaterLogs")]
    public class WaterLog
    {
        [Key]
        public int LogId { get; set; }

        [ForeignKey("IntakeId")]
        public int IntakeId { get; set; }
        public virtual WaterIntake WaterIntake { get; set; } = null!;

        public int AmountMl { get; set; }
        public DateTime LoggedAt { get; set; } = DateTime.Now;
    }
}
