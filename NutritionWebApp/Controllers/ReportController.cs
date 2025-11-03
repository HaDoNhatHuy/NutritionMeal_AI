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
        public async Task<IActionResult> Index(string period = "30days")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Xác định khoảng thời gian dựa trên tham số period
            DateTime startDate;
            switch (period)
            {
                case "7days":
                    startDate = DateTime.Now.AddDays(-7);
                    break;
                case "30days":
                    startDate = DateTime.Now.AddDays(-30);
                    break;
                case "90days":
                    startDate = DateTime.Now.AddDays(-90);
                    break;
                case "year":
                    startDate = DateTime.Now.AddYears(-1);
                    break;
                default:
                    startDate = DateTime.Now.AddDays(-30);
                    break;
            }

            // Lấy dữ liệu theo khoảng thời gian
            var history = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value && f.AnalyzedAt >= startDate)
                .OrderByDescending(f => f.AnalyzedAt)
                .ToListAsync();

            // Tính TDEE mục tiêu
            double tdee = 0;
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                var bmr = _settingsController.CalculateBMR(user);
                tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);
                var macroGoals = _settingsController.GetMacroGoalsInGrams(tdee, user.Goal);

                ViewBag.MacroGoals = macroGoals;
                ViewBag.UserGoal = user.Goal;
            }

            ViewBag.TDEE = tdee;
            ViewBag.CurrentPeriod = period;

            // 1. Tổng hợp dữ liệu theo ngày cho biểu đồ Calorie
            var dailyData = history
                .GroupBy(f => f.AnalyzedAt.Date)
                .Select(g => new
                {
                    Date = g.Key.ToString("dd/MM"),
                    FullDate = g.Key.ToString("yyyy-MM-dd"),
                    TotalCalories = g.Sum(f => f.Calories),
                    TotalProtein = g.Sum(f => f.Protein),
                    TotalCarbs = g.Sum(f => f.Carbs),
                    TotalFat = g.Sum(f => f.Fat)
                })
                .OrderBy(d => d.FullDate)
                .ToList();

            ViewBag.DailyDataJson = System.Text.Json.JsonSerializer.Serialize(dailyData);

            // 2. Tổng hợp theo tuần (cho view dài hạn)
            var weeklyData = history
                .GroupBy(f => new
                {
                    Year = f.AnalyzedAt.Year,
                    Week = System.Globalization.CultureInfo.CurrentCulture.Calendar
                        .GetWeekOfYear(f.AnalyzedAt,
                            System.Globalization.CalendarWeekRule.FirstDay,
                            DayOfWeek.Monday)
                })
                .Select(g => new
                {
                    Label = $"T{g.Key.Week}/{g.Key.Year}",
                    AvgCalories = g.Average(f => f.Calories),
                    AvgProtein = g.Average(f => f.Protein),
                    AvgCarbs = g.Average(f => f.Carbs),
                    AvgFat = g.Average(f => f.Fat)
                })
                .ToList();

            ViewBag.WeeklyDataJson = System.Text.Json.JsonSerializer.Serialize(weeklyData);

            // 3. Tổng hợp theo tháng
            var monthlyData = history
                .GroupBy(f => new { f.AnalyzedAt.Year, f.AnalyzedAt.Month })
                .Select(g => new
                {
                    Label = $"{g.Key.Month}/{g.Key.Year}",
                    AvgCalories = g.Average(f => f.Calories),
                    AvgProtein = g.Average(f => f.Protein),
                    AvgCarbs = g.Average(f => f.Carbs),
                    AvgFat = g.Average(f => f.Fat),
                    TotalMeals = g.Count()
                })
                .OrderBy(d => d.Label)
                .ToList();

            ViewBag.MonthlyDataJson = System.Text.Json.JsonSerializer.Serialize(monthlyData);

            // 4. Phân bổ theo loại bữa ăn
            var mealTypeData = history
                .GroupBy(f => f.MealType)
                .Where(g => !string.IsNullOrEmpty(g.Key))
                .Select(g => new
                {
                    MealType = g.Key,
                    TotalCalories = g.Sum(f => f.Calories),
                    Count = g.Count()
                })
                .ToList();

            ViewBag.MealTypeDataJson = System.Text.Json.JsonSerializer.Serialize(mealTypeData);

            // 5. Phân bổ theo giờ trong ngày
            var hourlyData = history
                .GroupBy(f => f.AnalyzedAt.Hour)
                .Select(g => new
                {
                    Hour = g.Key,
                    TotalCalories = g.Sum(f => f.Calories),
                    Count = g.Count()
                })
                .OrderBy(h => h.Hour)
                .ToList();

            ViewBag.HourlyDataJson = System.Text.Json.JsonSerializer.Serialize(hourlyData);

            // 6. Tổng hợp Macros trung bình
            var totalProtein = history.Sum(f => f.Protein);
            var totalCarbs = history.Sum(f => f.Carbs);
            var totalFat = history.Sum(f => f.Fat);

            ViewBag.MacroTotals = new { totalProtein, totalCarbs, totalFat };

            // 7. Thống kê tổng quan
            ViewBag.TotalMeals = history.Count;
            ViewBag.AvgCaloriesPerDay = dailyData.Any() ? dailyData.Average(d => d.TotalCalories) : 0;
            ViewBag.HighestCalorieDay = dailyData.OrderByDescending(d => d.TotalCalories).FirstOrDefault();
            ViewBag.LowestCalorieDay = dailyData.OrderBy(d => d.TotalCalories).FirstOrDefault();

            // 8. Nhóm lịch sử theo ngày cho phần History
            var historyByDate = history
                .GroupBy(f => f.AnalyzedAt.Date)
                .OrderByDescending(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(f => f.AnalyzedAt).ToList()
                );

            ViewBag.HistoryByDate = historyByDate;

            return View(history);
        }
    }
}