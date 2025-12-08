using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutritionWebApp.Models.Entities
{
    [Table("RecipeReviews")]
    public class RecipeReview
    {
        [Key]
        public int ReviewId { get; set; }

        [ForeignKey("RecipeId")]
        public int RecipeId { get; set; }
        public virtual Recipe Recipe { get; set; } = null!;

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        [Required]
        [Range(1, 5, ErrorMessage = "Rating phải từ 1 đến 5 sao")]
        public int Rating { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}