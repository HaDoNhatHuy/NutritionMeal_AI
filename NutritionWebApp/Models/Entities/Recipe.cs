using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("Recipes")]
    public class Recipe
    {
        [Key]
        public int RecipeId { get; set; }

        [ForeignKey("CreatedByUserId")]
        public int? CreatedByUserId { get; set; }
        public virtual User? CreatedBy { get; set; }

        public string RecipeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int? CookingTime { get; set; }
        public int Servings { get; set; } = 1;
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public string? Ingredients { get; set; } // JSON
        public string? Instructions { get; set; } // JSON
        public string? ImageUrl { get; set; }
        public bool IsPublic { get; set; }
        public int Likes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<RecipeLike> RecipeLikes { get; set; } = new List<RecipeLike>();
    }
}
