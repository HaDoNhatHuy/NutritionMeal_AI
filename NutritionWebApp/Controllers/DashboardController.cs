using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Controllers; // Để dùng SettingsController
using System.Text.Json;
using System.Linq; // Cần cho LINQ Sum và Where

namespace NutritionWebApp.Controllers
{
    public class DashboardController : Controller
    {
        private readonly DataContext _context;
        // Dùng constructor injection và tạo SettingsController nội bộ (nếu cần)
        // hoặc truy cập logic trực tiếp. Giả định DataContext là đủ.
        private readonly SettingsController _settingsController;

        public DashboardController(DataContext context)
        {
            _context = context;
            // Khởi tạo SettingsController để tái sử dụng logic tính toán
            _settingsController = new SettingsController(context);
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            // 1. Lấy TDEE và Mục tiêu Macros
            double tdee = 0;
            SettingsController.MacroGoals? macroGoals = null;
            if (user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                var bmr = _settingsController.CalculateBMR(user);
                tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);
                macroGoals = _settingsController.GetMacroGoalsInGrams(tdee, user.Goal);
            }
            ViewBag.TDEE = tdee;
            ViewBag.MacroGoals = macroGoals;

            // 2. Lấy tổng quan dinh dưỡng hôm nay
            var today = DateTime.Today;
            var todayHistory = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value && f.AnalyzedAt.Date == today)
                .ToListAsync();

            ViewBag.TotalCaloriesToday = todayHistory.Sum(f => f.Calories);
            ViewBag.TotalProteinToday = todayHistory.Sum(f => f.Protein);
            ViewBag.TotalCarbsToday = todayHistory.Sum(f => f.Carbs);
            ViewBag.TotalFatToday = todayHistory.Sum(f => f.Fat);

            // 3. Lấy cân nặng hiện tại (lưu trong User entity)
            ViewBag.CurrentWeight = user.Weight;

            // 4. Lấy dữ liệu 7 ngày gần nhất cho biểu đồ nhỏ
            var last7Days = DateTime.Today.AddDays(-7);

            // BƯỚC 1: Truy vấn Database (Async) - Chỉ lấy dữ liệu thô
            var rawData = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value && f.AnalyzedAt >= last7Days)
                .GroupBy(f => f.AnalyzedAt.Date)
                .Select(g => new
                {
                    DateObject = g.Key,
                    TotalCalories = g.Sum(f => f.Calories)
                })
                .OrderBy(d => d.DateObject)
                .ToListAsync(); // Dùng ToListAsync để thực thi bất đồng bộ và trả về List

            // BƯỚC 2: Format dữ liệu (Client-side / In-memory)
            // Lúc này rawData đã là List trong RAM, ta có thể thoải mái format string
            var history7DaysQuery = rawData
                .Select(d => new
                {
                    Date = d.DateObject.ToString("dd/MM"),
                    d.TotalCalories
                })
                .ToList();

            ViewBag.DailyCaloriesJson = System.Text.Json.JsonSerializer.Serialize(history7DaysQuery);

            return View(user);
        }
    }
}