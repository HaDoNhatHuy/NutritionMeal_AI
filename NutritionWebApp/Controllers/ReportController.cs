using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;

namespace NutritionWebApp.Controllers
{
    public class ReportController : Controller
    {
        private readonly DataContext _context;
        private readonly SettingsController _settingsController;
        public ReportController(DataContext context)
        {
            _context = context;
            _settingsController = new SettingsController(context);
        }
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId"); 
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Lấy dữ liệu 30 ngày gần nhất 
            var history = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value && f.AnalyzedAt >= DateTime.Now.AddDays(-30))
                .OrderByDescending(f => f.AnalyzedAt)
                .ToListAsync();

            // Tính TDEE mục tiêu (để so sánh)
            double tdee = 0;
            var user = await _context.Users.FindAsync(userId.Value); 
            if (user != null && user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                var bmr = _settingsController.CalculateBMR(user); 
                tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);

                // BỔ SUNG: TÍNH MACRO GOALS
                var macroGoals = _settingsController.GetMacroGoalsInGrams(tdee, user.Goal);

                // Gửi Mục tiêu Macros đến View
                ViewBag.MacroGoals = macroGoals;
                ViewBag.UserGoal = user.Goal;
            }
            ViewBag.TDEE = tdee;

            // Tổng hợp dữ liệu theo ngày
            var aggregatedData = history
                .GroupBy(f => f.AnalyzedAt.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("yyyy-MM-dd"),
                    TotalCalories = g.Sum(f => f.Calories),
                    TotalProtein = g.Sum(f => f.Protein),
                    TotalCarbs = g.Sum(f => f.Carbs),
                    TotalFat = g.Sum(f => f.Fat)
                })
                .OrderBy(d => d.Date)
                .ToList();

            // Chuyển dữ liệu sang JSON cho Chart.js sử dụng trong View
            ViewBag.AggregatedDataJson = System.Text.Json.JsonSerializer.Serialize(aggregatedData);

            return View(history);
        }
    }
}
