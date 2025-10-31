using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutritionWebApp.Models.Entities
{
    [Table("FoodHistory")]
    public class FoodHistory
    {
        [Key]
        public int HistoryId { get; set; }
        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public string FoodName { get; set; } = string.Empty;
        public float Calories { get; set; }
        public float Protein { get; set; }
        public float Carbs { get; set; }
        public float Fat { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime AnalyzedAt { get; set; } = DateTime.Now;
    }
}
