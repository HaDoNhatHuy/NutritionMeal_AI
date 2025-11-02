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
            // Khởi tạo SettingsController để tính TDEE
            _settingsController = new SettingsController(context);
        }
        //[HttpGet]
        //public async Task<IActionResult> History()
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");

        //    if (!userId.HasValue) return RedirectToAction("Login", "Account");

        //    // Truy vấn tất cả lịch sử chat của người dùng, sắp xếp theo thời gian tăng dần
        //    var history = await _context.ChatHistory
        //        .Where(c => c.UserId == userId.Value)
        //        .OrderBy(c => c.Timestamp) // Lấy từ cũ nhất đến mới nhất
        //        .ToListAsync();

        //    return View(history);
        //}
        [HttpGet]
        public async Task<IActionResult> History() // Action này giờ là Danh sách Session
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Truy vấn tất cả lịch sử, sau đó nhóm theo Ngày (Timestamp.Date)
            var sessions = await _context.ChatHistory
                .Where(c => c.UserId == userId.Value)
                .GroupBy(c => c.Timestamp.Date) // Nhóm theo ngày
                .Select(g => new ChatSessionSummary // Ánh xạ sang DTO mới
                {
                    SessionDate = g.Key,
                    Title = "Cuộc trò chuyện ngày " + g.Key.ToString("dd/MM/yyyy"),
                    MessageCount = g.Count(),
                    // Lấy nội dung của tin nhắn cuối cùng để làm preview
                    LastMessagePreview = g.OrderByDescending(x => x.Timestamp).First().Content
                })
                .OrderByDescending(s => s.SessionDate) // Sắp xếp ngày mới nhất lên đầu
                .ToListAsync();

            // Trả về một View mới (chúng ta sẽ tạo ở Bước 3)
            return View("ConversationList", sessions);
        }
        [HttpGet]
        // Nhận tham số ngày (SessionDate) để lọc tin nhắn
        public async Task<IActionResult> ViewSession(DateTime date)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Lấy tất cả tin nhắn của người dùng trong ngày cụ thể
            var messages = await _context.ChatHistory
                .Where(c => c.UserId == userId.Value && c.Timestamp.Date == date.Date)
                .OrderBy(c => c.Timestamp) // Lấy từ cũ nhất đến mới nhất
                .ToListAsync();

            ViewData["Title"] = "Chi tiết trò chuyện " + date.ToString("dd/MM/yyyy");
            // Sử dụng lại View History.cshtml cũ để hiển thị nội dung tin nhắn
            // (View History.cshtml hiện đang nhận Model là List<ChatHistory> [3])
            return View("ConversationDetail", messages);
        }
        //[HttpGet]
        //public async Task<IActionResult> GetRecentHistory()
        //{
        //    // Kiểm tra Session [1]
        //    var userId = HttpContext.Session.GetInt32("UserId");

        //    if (!userId.HasValue)
        //        // Trả về danh sách rỗng an toàn nếu chưa đăng nhập 
        //        return Json(new List<object>());

        //    // Truy vấn 10 tin nhắn gần nhất [2]
        //    var history = await _context.ChatHistory
        //        .Where(c => c.UserId == userId.Value)
        //        .OrderByDescending(c => c.Timestamp) // Lấy từ mới nhất về
        //        .Take(10) // Chỉ lấy 10
        //        .OrderBy(c => c.Timestamp) // Đảo ngược lại để hiển thị theo thứ tự thời gian (từ cũ đến mới)
        //        .Select(c => new
        //        {
        //            c.Role, // "User" hoặc "AI" [2]
        //            c.Content
        //        })
        //        .ToListAsync();

        //    // Trả về JSON chứa 10 tin nhắn
        //    return Json(history);
        //}


        [HttpPost]
        public async Task<IActionResult> GetAdvice([FromBody] Dictionary<string, string> payload)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var user = await _context.Users
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return Json(new { error = "Không tìm thấy người dùng" });

            var userQuestion = payload["question"]; // Lấy câu hỏi người dùng

            // 1. LƯU TIN NHẮN CỦA NGƯỜI DÙNG
            var userMessage = new ChatHistory
            {
                UserId = userId.Value,
                Role = "User", // Role được định nghĩa trong ChatHistory là User hoặc AI [1]
                Content = userQuestion,
                Timestamp = DateTime.Now
            };
            _context.ChatHistory.Add(userMessage);
            // Lưu ngay để đảm bảo tin nhắn người dùng được ghi nhận trước khi gọi AI
            await _context.SaveChangesAsync();

            // 2. Tính TDEE động và Lấy Lịch sử ăn uống gần nhất (5 bữa)
            double tdee = 0;
            if (user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                // Khởi tạo SettingsController để tính BMR/TDEE [4]
                var bmr = _settingsController.CalculateBMR(user);
                tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);
            }

            var recentHistory = await _context.FoodHistory
                .Where(f => f.UserId == userId.Value)
                .OrderByDescending(f => f.AnalyzedAt)
                .Take(5)
                .Select(f => new
                {
                    f.FoodName,
                    f.Calories,
                    f.Protein,
                    f.AnalyzedAt
                })
                .ToListAsync();
            // BỔ SUNG: 2.5 Lấy Lịch sử đối thoại gần nhất (ví dụ: 10 tin nhắn)
            var chatHistoryContext = await _context.ChatHistory
                .Where(c => c.UserId == userId.Value)
                // Lấy 10 tin nhắn gần nhất, bao gồm cả tin nhắn vừa lưu ở bước 1
                .OrderByDescending(c => c.Timestamp)
                .Take(10)
                .OrderBy(c => c.Timestamp) // Đảo ngược lại để gửi theo thứ tự thời gian
                .Select(c => new
                {
                    c.Role,
                    c.Content
                })
                .ToListAsync();

            // 3. Chuẩn bị Payload gửi đi (Không thay đổi)
            var chatPayload = new
            {
                question = userQuestion, // Dùng biến đã lưu
                stats = new
                {
                    user.Age,
                    user.Weight,
                    user.Goal,
                    TDEE = tdee.ToString("F0")
                },
                history = recentHistory,
                // THÊM CHAT HISTORY ĐỂ DUY TRÌ NGỮ CẢNH
                chat_context = chatHistoryContext
            };

            // 4. Gọi Flask AI Service
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

            // 5. PHÂN TÍCH VÀ LƯU TIN NHẮN CỦA AI VÀO CHAT HISTORY

            // Deserialize JSON để lấy advice
            dynamic result = System.Text.Json.JsonSerializer.Deserialize<object>(jsonString)!;
            // Đảm bảo lấy được chuỗi advice
            string aiAdvice = result.GetProperty("advice").GetString();

            var aiMessage = new ChatHistory
            {
                UserId = userId.Value,
                Role = "AI", // Role: AI
                Content = aiAdvice,
                Timestamp = DateTime.Now
            };

            _context.ChatHistory.Add(aiMessage);
            await _context.SaveChangesAsync(); // Lưu tin nhắn AI

            return Content(jsonString, "application/json");
        }
        [HttpPost]
        public async Task<IActionResult> GetProactiveAdvice()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var user = await _context.Users
                                .AsNoTracking()
                                .FirstOrDefaultAsync(u => u.UserId == userId.Value);
            if (user == null) return Json(new { error = "Không tìm thấy người dùng" });

            // --- 1. TÍNH TOÁN TDEE & MACRO GOALS ---
            double tdee = 0;
            SettingsController.MacroGoals? macroGoals = null; // Cần truy cập class MacroGoals

            if (user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                var bmr = _settingsController.CalculateBMR(user);
                tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);

                // Lấy Mục tiêu Macro (đã được định nghĩa trong SettingsController)
                macroGoals = _settingsController.GetMacroGoalsInGrams(tdee, user.Goal);
            }

            // --- 2. LẤY LỊCH SỬ ĂN UỐNG HÔM NAY ---
            // Tính toán từ đầu ngày hôm nay
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
                    f.MealType, // Sử dụng MealType mới thêm
                    f.AnalyzedAt
                })
                .ToListAsync();

            // Tính tổng Macros/Calo đã nạp hôm nay
            var totalCaloriesToday = todayHistory.Sum(f => f.Calories);
            var totalProteinToday = todayHistory.Sum(f => f.Protein);
            var totalCarbsToday = todayHistory.Sum(f => f.Carbs);
            var totalFatToday = todayHistory.Sum(f => f.Fat);

            // --- 3. CHUẨN BỊ PAYLOAD ---
            var chatPayload = new
            {
                // Gửi toàn bộ thông tin cá nhân và mục tiêu
                stats = new
                {
                    user.Goal,
                    TDEE = tdee.ToString("F0")
                },
                // Gửi Macro Goals
                macroGoals = macroGoals,

                // Gửi tổng hợp Macros/Calo đã ăn hôm nay
                dailySummary = new
                {
                    TotalCalories = totalCaloriesToday.ToString("F0"),
                    TotalProtein = totalProteinToday.ToString("F0"),
                    TotalCarbs = totalCarbsToday.ToString("F0"),
                    TotalFat = totalFatToday.ToString("F0")
                },
                // Gửi lịch sử chi tiết hôm nay
                history = todayHistory
            };

            // --- 4. GỌI FLASK AI SERVICE (Tạo endpoint mới: /proactive_advise) ---
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

            // AI Response chỉ là một Advice, không cần lưu vào ChatHistory nếu không phải là cuộc trò chuyện

            return Content(jsonString, "application/json");
        }
        [HttpPost]
        public async Task<IActionResult> GenerateRecipe([FromBody] Dictionary<string, string> payload)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == userId.Value);

            // Kiểm tra thông tin cá nhân cần thiết để tính TDEE
            if (user == null || !user.Age.HasValue || !user.Weight.HasValue || !user.Height.HasValue)
                return Json(new { error = "Vui lòng cập nhật đầy đủ Tuổi, Chiều cao, Cân nặng và Mục tiêu trong Cài đặt." });

            // Lấy yêu cầu tùy chỉnh từ người dùng (ví dụ: "dùng thịt gà" hoặc "bữa sáng nhanh")
            var customRequest = payload.GetValueOrDefault("request", "một bữa ăn nhẹ giàu Protein");

            // --- 1. TÍNH TOÁN TDEE & MACRO GOALS (KHÔNG DÙNG DI) ---
            double tdee = 0;
            // Khởi tạo SettingsController thủ công theo cấu trúc hiện tại [1, 2]
            // Sử dụng _settingsController đã được khởi tạo trong constructor
            var bmr = _settingsController.CalculateBMR(user);
            tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);
            // Lấy Mục tiêu Macro [3, 4]
            SettingsController.MacroGoals? macroGoals = _settingsController.GetMacroGoalsInGrams(tdee, user.Goal);

            // --- 2. CHUẨN BỊ PAYLOAD GỬI ĐẾN PYTHON FLASK ---
            var recipePayload = new
            {
                goal = user.Goal, // Giảm cân/Tăng cân
                custom_request = customRequest,
                TDEE = tdee.ToString("F0"),
                // Gửi toàn bộ mục tiêu Macros đã tính
                macro_goals = macroGoals
            };

            // --- 3. GỌI FLASK AI SERVICE (Endpoint mới: /generate_recipe) ---
            using var client = new HttpClient();
            var jsonContent = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(recipePayload),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            // Lưu ý: ApplicationUrl mặc định là http://localhost:5196 [5]
            var response = await client.PostAsync("http://localhost:5000/generate_recipe", jsonContent);
            var jsonString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return Json(new { error = "Lỗi AI Recipe: " + jsonString });

            // Trả về JSON từ AI (sẽ được JavaScript phân tích và hiển thị)
            return Content(jsonString, "application/json");
        }
    }
}