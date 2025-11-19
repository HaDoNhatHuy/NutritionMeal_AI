namespace NutritionWebApp.ViewModels
{
    public class RecipeResultViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        // Dùng List<string> cho Nguyên liệu và Hướng dẫn để dễ hiển thị.
        public List<string> Ingredients { get; set; } = new List<string>();
        public List<string> Instructions { get; set; } = new List<string>();
        public double CaloriesTotal { get; set; }
        public double ProteinGrams { get; set; }
        public double CarbGrams { get; set; }
        public double FatGrams { get; set; }
        public string Advice { get; set; } = string.Empty; // Lời khuyên kèm theo
    }
    public class SaveRecipeRequest
    {
        // Dữ liệu chính từ AI (hoặc nhập thủ công)
        public string RecipeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Category { get; set; }
        public int CookingTime { get; set; }
        public double Calories { get; set; }
        public double Protein { get; set; }
        public double Carbs { get; set; }
        public double Fat { get; set; }
        public List<string> Ingredients { get; set; } = new List<string>();
        public List<string> Instructions { get; set; } = new List<string>();

        // Metadata người dùng quyết định
        public bool IsPublic { get; set; } = true;
    }
}
