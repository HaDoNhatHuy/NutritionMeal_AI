// Controllers/FoodTrackerController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;
using System;
using System.Linq;

namespace NutritionWebApp.Controllers
{
    public class FoodTrackerController : Controller
    {
        private readonly DataContext _context;

        public FoodTrackerController(DataContext context)
        {
            _context = context;
        }

        // POST Model cho việc nhập thủ công
        public class ManualFoodLogRequest
        {
            public string FoodName { get; set; } = string.Empty;
            public string MealType { get; set; } = "AnVat";
            public double Calories { get; set; }
            public double Protein { get; set; }
            public double Carbs { get; set; }
            public double Fat { get; set; }
            public DateTime LogDate { get; set; } = DateTime.Today; // Có thể dùng cho log lùi ngày
        }

        // GET: /FoodTracker/Index (Hiển thị form và lịch sử 30 ngày)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Tải 30 ngày lịch sử ăn uống (Cắt từ Report logic [3])
            var startDate = DateTime.Now.AddDays(-30);
            var history = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value && f.AnalyzedAt >= startDate)
                .OrderByDescending(f => f.AnalyzedAt)
                .ToListAsync();

            // Nhóm lịch sử theo ngày
            var historyByDate = history
                .GroupBy(f => f.AnalyzedAt.Date)
                .OrderByDescending(g => g.Key)
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(f => f.AnalyzedAt).ToList()
                );

            ViewBag.HistoryByDate = historyByDate;

            // Cần thông tin để hiển thị nút "Tải thêm"
            if (historyByDate.Any())
            {
                ViewBag.OffsetDate = historyByDate.Last().Key.ToString("yyyy-MM-dd");
            }
            ViewBag.HasMore = history.Count >= 30; // Giả sử nếu tải đủ 30 item thì có thể còn

            return View(new ManualFoodLogRequest());
        }

        // POST: /FoodTracker/LogMeal
        [HttpPost]
        public async Task<IActionResult> LogMeal([FromBody] ManualFoodLogRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            if (request.Calories <= 0 || string.IsNullOrEmpty(request.FoodName))
            {
                return Json(new { success = false, error = "Tên món ăn và Calo là bắt buộc." });
            }

            var food = new FoodHistory
            {
                UserId = userId.Value,
                FoodName = request.FoodName,
                Calories = request.Calories,
                Protein = request.Protein,
                Carbs = request.Carbs,
                Fat = request.Fat,
                MealType = request.MealType,
                // Sử dụng ngày nhập, nếu chỉ có ngày thì thêm giờ hiện tại để phân biệt logs
                AnalyzedAt = request.LogDate.Date.Add(DateTime.Now.TimeOfDay)
            };

            _context.FoodHistory.Add(food);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Tái sử dụng logic tải thêm lịch sử từ ReportController.LoadHistory [5]
        [HttpGet]
        public async Task<IActionResult> LoadHistory(string offsetDate, int pageSize = 30)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Content("");

            if (!DateTime.TryParse(offsetDate, out DateTime parsedOffsetDate))
            {
                return Content("");
            }

            // Lấy dữ liệu cũ hơn ngày offsetDate
            var history = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value && f.AnalyzedAt.Date < parsedOffsetDate.Date)
                .OrderByDescending(f => f.AnalyzedAt)
                .Take(pageSize)
                .ToListAsync();

            bool hasMore = history.Count == pageSize;

            var historyByDate = history
                .GroupBy(f => f.AnalyzedAt.Date)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(f => f.AnalyzedAt).ToList());

            ViewBag.HasMore = hasMore;

            // Sử dụng lại Partial View của Report
            return PartialView("~/Views/Report/_FoodHistoryPartial.cshtml", historyByDate);
        }
    }
}