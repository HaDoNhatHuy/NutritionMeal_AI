using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;

namespace NutritionWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly DataContext _context;

        public HomeController(ILogger<HomeController> logger, DataContext context)
        {
            _logger = logger;
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> Analyze(IFormFile image)
        {
            if (image == null || image.Length == 0)
                return Json(new { error = "Vui lòng chọn ảnh" });

            try
            {
                using var client = new HttpClient();
                using var content = new MultipartFormDataContent();
                using var stream = image.OpenReadStream();
                content.Add(new StreamContent(stream), "image", image.FileName);

                var response = await client.PostAsync("http://localhost:5000/analyze", content);
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                    return Json(new { error = "AI lỗi: " + jsonString });

                // Lưu vào DB nếu đã login
                var userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    dynamic result = System.Text.Json.JsonSerializer.Deserialize<object>(jsonString);
                    var food = new FoodHistory
                    {
                        UserId = userId.Value,
                        FoodName = result.GetProperty("food_name").GetString(),
                        Calories = (float)result.GetProperty("calories").GetDouble(),
                        Protein = (float)result.GetProperty("protein").GetDouble(),
                        Carbs = (float)result.GetProperty("carbs").GetDouble(),
                        Fat = (float)result.GetProperty("fat").GetDouble(),
                        ImageUrl = $"/uploads/{Guid.NewGuid()}_{image.FileName}",
                        AnalyzedAt = DateTime.Now
                    };

                    // Lưu ảnh
                    var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", food.ImageUrl.Split('/').Last());
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    using var fileStream = new FileStream(path, FileMode.Create);
                    await image.CopyToAsync(fileStream);

                    _context.FoodHistory.Add(food);
                    await _context.SaveChangesAsync();
                }

                return Content(jsonString, "application/json");
            }
            catch (Exception ex)
            {
                return Json(new { error = "Lỗi: " + ex.Message });
            }
        }
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
