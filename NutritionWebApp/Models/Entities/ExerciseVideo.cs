using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NutritionWebApp.Models.Entities
{
    [Table("ExerciseVideo")]
    public class ExerciseVideo
    {
        [Key]
        public int Id { get; set; }
        public string MuscleGroup { get; set; } = string.Empty;
        public string YoutubeVideoUrl { get; set; } = string.Empty;
    }
}
