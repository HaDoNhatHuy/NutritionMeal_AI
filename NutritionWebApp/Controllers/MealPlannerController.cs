using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;
using System.Text.Json;

namespace NutritionWebApp.Controllers
{
    public class MealPlannerController : Controller
    {
        private readonly DataContext _context;
        private readonly SettingsController _settingsController;

        public MealPlannerController(DataContext context)
        {
            _context = context;
            _settingsController = new SettingsController(context);
        }

        // GET: /MealPlanner/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var plans = await _context.MealPlans
                .Where(m => m.UserId == userId.Value)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            return View(plans);
        }

        // GET: /MealPlanner/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Tính TDEE và Macros
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null && user.Age.HasValue && user.Weight.HasValue && user.Height.HasValue)
            {
                var bmr = _settingsController.CalculateBMR(user);
                var tdee = _settingsController.CalculateTDEE(bmr, user.ActivityLevel);
                var macros = _settingsController.GetMacroGoalsInGrams(tdee, user.Goal);

                ViewBag.TDEE = tdee;
                ViewBag.Macros = macros;
            }

            return View();
        }

        // POST: /MealPlanner/Generate
        [HttpPost]
        public async Task<IActionResult> Generate([FromBody] MealPlanRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            try
            {
                var user = await _context.Users.FindAsync(userId.Value);

                var aiPayload = new
                {
                    dailyCalories = request.DailyCalories,
                    dailyProtein = request.DailyProtein,
                    dailyCarbs = request.DailyCarbs,
                    dailyFat = request.DailyFat,
                    duration = request.Duration,
                    budget = request.Budget > 0 ? $"{request.Budget:N0} VND" : "Không giới hạn",
                    mealCount = request.MealCount,
                    dietaryRestrictions = string.IsNullOrEmpty(user?.Pathology) ? "Không" : user.Pathology,
                    goal = user?.Goal ?? "Duy trì"
                };

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10);
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(aiPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("http://localhost:5000/generate_meal_plan", jsonContent);
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { error = "Lỗi AI Meal Planner: " + jsonString });
                }

                // Lưu vào Database
                var planData = JsonSerializer.Deserialize<JsonElement>(jsonString);

                var mealPlan = new MealPlan
                {
                    UserId = userId.Value,
                    PlanName = planData.GetProperty("planName").GetString() ?? "Meal Plan",
                    DailyCalories = request.DailyCalories,
                    DailyProtein = request.DailyProtein,
                    DailyCarbs = request.DailyCarbs,
                    DailyFat = request.DailyFat,
                    Duration = request.Duration,
                    Budget = request.Budget,
                    MealPlanJson = jsonString,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.MealPlans.Add(mealPlan);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    planId = mealPlan.MealPlanId,
                    plan = planData
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Lỗi: {ex.Message}" });
            }
        }

        // GET: /MealPlanner/View/{id}
        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var plan = await _context.MealPlans
                .FirstOrDefaultAsync(m => m.MealPlanId == id && m.UserId == userId.Value);

            if (plan == null) return NotFound();

            var planData = JsonSerializer.Deserialize<JsonElement>(plan.MealPlanJson);
            ViewBag.PlanData = planData;
            ViewBag.Plan = plan;

            return View();
        }

        // DELETE: /MealPlanner/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var plan = await _context.MealPlans
                .FirstOrDefaultAsync(m => m.MealPlanId == id && m.UserId == userId.Value);

            if (plan == null) return Json(new { error = "Không tìm thấy kế hoạch" });

            _context.MealPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }

    public class MealPlanRequest
    {
        public int DailyCalories { get; set; }
        public double DailyProtein { get; set; }
        public double DailyCarbs { get; set; }
        public double DailyFat { get; set; }
        public int Duration { get; set; } = 7; // days
        public double Budget { get; set; } = 0; // VND
        public int MealCount { get; set; } = 3; // meals per day
    }
}