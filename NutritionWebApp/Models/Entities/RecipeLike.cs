using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("RecipeLikes")]
    public class RecipeLike
    {
        [Key]
        public int LikeId { get; set; }

        [ForeignKey("RecipeId")]
        public int RecipeId { get; set; }
        public virtual Recipe Recipe { get; set; } = null!;

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public DateTime LikedAt { get; set; } = DateTime.Now;
    }
}
