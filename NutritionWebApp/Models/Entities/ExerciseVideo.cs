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
        // THÊM CÁC TRƯỜNG MỚI:
        public string? Title { get; set; } // Tiêu đề video
        public string? Duration { get; set; } // Thời lượng video (Ví dụ: "PT10M30S" hoặc đã format)
    }
}
