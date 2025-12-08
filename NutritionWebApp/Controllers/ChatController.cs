using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;
using NutritionWebApp.ViewModels;

namespace NutritionWebApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly DataContext _context;
        private readonly SettingsController _settingsController;

        public ChatController(DataContext context)
        {
            _context = context;
            _settingsController = new SettingsController(context);
        }
        // GET: /Chat/Index (Thay thế cho Chatbox Popup và chuyển hướng từ History cũ) (F3)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Tái sử dụng logic tải các phiên chat [9]
            var sessions = await _context.ChatHistory
                .Where(c => c.UserId == userId.Value)
                .GroupBy(c => c.Timestamp.Date)
                .Select(g => new ChatSessionSummary
                {
                    SessionDate = g.Key,
                    Title = "Trò chuyện ngày " + g.Key.ToString("dd/MM/yyyy"),
                    MessageCount = g.Count(),
                    LastMessagePreview = g.OrderByDescending(x => x.Timestamp).First().Content
                })
                .OrderByDescending(s => s.SessionDate)
                .ToListAsync();

            // Sẽ render View mới: Views/Chat/Index.cshtml
            return View("Index", sessions);
        }

        //// Lịch sử chat - Hiển thị theo ngày (không cần AI summary)
        //[HttpGet]
        //public async Task<IActionResult> History()
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");
        //    if (!userId.HasValue) return RedirectToAction("Login", "Account");

        //    // Nhóm tin nhắn theo ngày
        //    var sessions = await _context.ChatHistory
        //        .Where(c => c.UserId == userId.Value)
        //        .GroupBy(c => c.Timestamp.Date)
        //        .Select(g => new ChatSessionSummary
        //        {
        //            SessionDate = g.Key,
        //            // Tiêu đề đơn giản: "Trò chuyện ngày DD/MM/YYYY"
        //            Title = "Trò chuyện ngày " + g.Key.ToString("dd/MM/yyyy"),
        //            MessageCount = g.Count(),
        //            LastMessagePreview = g.OrderByDescending(x => x.Timestamp).First().Content
        //        })
        //        .OrderByDescending(s => s.SessionDate)
        //        .ToListAsync();

        //    return View("ConversationList", sessions);
        //}

        // Xem chi tiết một phiên chat theo ngày
        [HttpGet]
        public async Task<IActionResult> ViewSession(DateTime date)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var messages = await _context.ChatHistory
                .Where(c => c.UserId == userId.Value && c.Timestamp.Date == date.Date)
                .OrderBy(c => c.Timestamp)
                .ToListAsync();

            ViewData["Title"] = "Chi tiết trò chuyện " + date.ToString("dd/MM/yyyy");
            return View("ConversationDetail", messages);
        }

        // Xóa một phiên chat theo ngày
        [HttpPost]
        public async Task<IActionResult> DeleteSession([FromBody] Dictionary<string, string> payload)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
                return Json(new { success = false, error = "Chưa đăng nhập" });

            if (!payload.TryGetValue("date", out var dateString) || !DateTime.TryParse(dateString, out DateTime date))
                return Json(new { success = false, error = "Dữ liệu ngày không hợp lệ." });

            var messagesToDelete = await _context.ChatHistory
                .Where(c => c.UserId == userId.Value && c.Timestamp.Date == date.Date)
                .ToListAsync();

            if (!messagesToDelete.Any())
                return Json(new { success = false, error = "Không tìm thấy phiên trò chuyện để xóa." });

            _context.ChatHistory.RemoveRange(messagesToDelete);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Gửi câu hỏi đến AI (giữ nguyên)
        [HttpPost]
        public async Task<IActionResult> GetAdvice([FromBody] Dictionary<string, string> payload)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null) return Json(new { error = "Không tìm thấy người dùng" });

            var userQuestion = payload["question"];

            // Lưu tin nhắn người dùng
            var userMessage = new ChatHistory
            {
                UserId = userId.Value,
                Role = "User",
                Content = userQuestion,
                Timestamp = DateTime.Now
            };
            _context.ChatHistory.Add(userMessage);
            await _context.SaveChangesAsync();

            // Tính TDEE
            double tdee = 0;
            if (user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                var bmr = _settingsController.CalculateBMR(user);
                tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);
            }

            // Lấy lịch sử ăn uống
            var recentHistory = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value)
                .OrderByDescending(f => f.AnalyzedAt)
                .Take(5)
                .Select(f => new { f.FoodName, f.Calories, f.Protein, f.AnalyzedAt })
                .ToListAsync();

            // Lấy lịch sử chat
            var chatHistoryContext = await _context.ChatHistory
                .Where(c => c.UserId == userId.Value)
                .OrderByDescending(c => c.Timestamp)
                .Take(10)
                .OrderBy(c => c.Timestamp)
                .Select(c => new { c.Role, c.Content })
                .ToListAsync();

            // Chuẩn bị payload gửi AI
            var chatPayload = new
            {
                question = userQuestion,
                stats = new
                {
                    user.Age,
                    user.Weight,
                    user.Goal,
                    TDEE = tdee.ToString("F0"),
                    Pathology = user.Pathology ?? "Không"
                },
                history = recentHistory,
                chat_context = chatHistoryContext
            };

            // Gọi Flask AI
            using var client = new HttpClient();
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(chatPayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("http://localhost:5000/advise_chat", jsonContent);
            var jsonString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Json(new { error = "Lỗi AI Chat: " + jsonString });

            // Lưu tin nhắn AI
            dynamic result = System.Text.Json.JsonSerializer.Deserialize<object>(jsonString)!;
            string aiAdvice = result.GetProperty("advice").GetString();

            var aiMessage = new ChatHistory
            {
                UserId = userId.Value,
                Role = "AI",
                Content = aiAdvice,
                Timestamp = DateTime.Now
            };
            _context.ChatHistory.Add(aiMessage);
            await _context.SaveChangesAsync();

            return Content(jsonString, "application/json");
        }

        // Lời khuyên chủ động (giữ nguyên)
        [HttpPost]
        public async Task<IActionResult> GetProactiveAdvice()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null) return Json(new { error = "Không tìm thấy người dùng" });

            double tdee = 0;
            SettingsController.MacroGoals? macroGoals = null;

            if (user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                var bmr = _settingsController.CalculateBMR(user);
                tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);
                macroGoals = _settingsController.GetMacroGoalsInGrams(tdee, user.Goal);
            }

            var todayStart = DateTime.Today;
            var todayHistory = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value && f.AnalyzedAt >= todayStart)
                .Select(f => new
                {
                    f.FoodName,
                    f.Calories,
                    f.Protein,
                    f.Carbs,
                    f.Fat,
                    f.MealType,
                    f.AnalyzedAt
                })
                .ToListAsync();

            var totalCaloriesToday = todayHistory.Sum(f => f.Calories);
            var totalProteinToday = todayHistory.Sum(f => f.Protein);
            var totalCarbsToday = todayHistory.Sum(f => f.Carbs);
            var totalFatToday = todayHistory.Sum(f => f.Fat);

            var chatPayload = new
            {
                stats = new
                {
                    user.Goal,
                    TDEE = tdee.ToString("F0")
                },
                macroGoals = macroGoals,
                dailySummary = new
                {
                    TotalCalories = totalCaloriesToday.ToString("F0"),
                    TotalProtein = totalProteinToday.ToString("F0"),
                    TotalCarbs = totalCarbsToday.ToString("F0"),
                    TotalFat = totalFatToday.ToString("F0")
                },
                history = todayHistory
            };

            using var client = new HttpClient();
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(chatPayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("http://localhost:5000/proactive_advise", jsonContent);
            var jsonString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Json(new { error = "Lỗi AI Proactive: " + jsonString });

            return Content(jsonString, "application/json");
        }

        // Tạo công thức món ăn (giữ nguyên)
        [HttpPost]
        public async Task<IActionResult> GenerateRecipe([FromBody] Dictionary<string, string> payload)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            if (user == null || !user.Age.HasValue || !user.Weight.HasValue || !user.Height.HasValue)
                return Json(new { error = "Vui lòng cập nhật đầy đủ thông tin trong Cài đặt." });

            var customRequest = payload.GetValueOrDefault("request", "một bữa ăn nhẹ giàu Protein");

            double tdee = 0;
            var bmr = _settingsController.CalculateBMR(user);
            tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);
            SettingsController.MacroGoals? macroGoals = _settingsController.GetMacroGoalsInGrams(tdee, user.Goal);

            var recipePayload = new
            {
                goal = user.Goal,
                custom_request = customRequest,
                TDEE = tdee.ToString("F0"),
                macro_goals = macroGoals,
                Pathology = user.Pathology ?? "Không"
            };

            using var client = new HttpClient();
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(recipePayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            var response = await client.PostAsync("http://localhost:5000/generate_recipe", jsonContent);
            var jsonString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Json(new { error = "Lỗi AI Recipe: " + jsonString });

            return Content(jsonString, "application/json");
        }
    }
    
}