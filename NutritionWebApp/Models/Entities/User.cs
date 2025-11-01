using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutritionWebApp.Models.Entities
{
    [Table("Users")]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;
        public int? Age { get; set; }
        public float? Height { get; set; }
        public float? Weight { get; set; }
        public bool? Gender { get; set; } // true: Nam
        public string? Goal { get; set; }
        public string? ActivityLevel { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Navigation
        public virtual ICollection<FoodHistory> FoodHistories { get; set; } = new List<FoodHistory>();
    }
}
