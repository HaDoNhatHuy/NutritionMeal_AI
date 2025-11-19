using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("WorkoutPlans")]
    public class WorkoutPlan
    {
        [Key]
        public int PlanId { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [Required]
        public string PlanName { get; set; } = string.Empty;

        public string Goal { get; set; } = string.Empty; // TangCo, GiamMo, TangSucBen
        public string Level { get; set; } = string.Empty; // Newbie, Intermediate, Advanced
        public int Duration { get; set; } // Weeks
        public int Frequency { get; set; } // Days per week
        public string? Equipment { get; set; }
        public string PlanJson { get; set; } = string.Empty; // Full JSON
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
