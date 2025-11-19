using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("MealPlans")]
    public class MealPlan
    {
        [Key]
        public int MealPlanId { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string PlanName { get; set; } = string.Empty;
        public int DailyCalories { get; set; }
        public double DailyProtein { get; set; }
        public double DailyCarbs { get; set; }
        public double DailyFat { get; set; }
        public int Duration { get; set; } // Days
        public double? Budget { get; set; }
        public string MealPlanJson { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }
}
