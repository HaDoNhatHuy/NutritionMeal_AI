using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.ViewModels
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Nhập họ tên")]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required, MinLength(6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
