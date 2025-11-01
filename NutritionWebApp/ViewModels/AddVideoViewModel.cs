using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.ViewModels
{
    public class AddVideoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập URL YouTube.")]
        //[Url(ErrorMessage = "URL không hợp lệ.")]
        public string YoutubeVideoUrl { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng chọn nhóm cơ.")]
        public string MuscleGroup { get; set; } = string.Empty;
    }
}