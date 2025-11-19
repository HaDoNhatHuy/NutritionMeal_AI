using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;

namespace NutritionWebApp.Controllers
{
    public class HabitsController : Controller
    {
        private readonly DataContext _context;

        public HabitsController(DataContext context)
        {
            _context = context;
        }

        // GET: /Habits/Index
        // Controllers/HabitsController.cs

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var habits = await _context.Habits
                .Where(h => h.UserId == userId.Value && h.IsActive)
                .Include(h => h.HabitLogs) // Đảm bảo load logs để tính streak
                .ToListAsync();

            var today = DateTime.Today;

            // Lấy logs hôm nay để check trạng thái hoàn thành
            var todayLogs = await _context.HabitLogs
                .Where(h => habits.Select(hab => hab.HabitId).Contains(h.HabitId) && h.LogDate.Date == today)
                .ToListAsync();

            // --- CODE MỚI: Tính Streak cho từng habit và đưa vào Dictionary ---
            var habitStreaks = new Dictionary<int, int>();
            foreach (var habit in habits)
            {
                // Tái sử dụng hàm CalculateStreak có sẵn trong Controller
                habitStreaks[habit.HabitId] = await CalculateStreak(habit.HabitId);
            }

            ViewBag.TodayLogs = todayLogs;
            ViewBag.HabitStreaks = habitStreaks; // Truyền data sang View

            return View(habits);
        }

        // POST: /Habits/Create
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateHabitRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            try
            {
                var habit = new Habit
                {
                    UserId = userId.Value,
                    HabitName = request.HabitName,
                    HabitType = request.HabitType,
                    GoalValue = request.GoalValue,
                    IconEmoji = request.IconEmoji,
                    IsActive = true
                };

                _context.Habits.Add(habit);
                await _context.SaveChangesAsync();

                return Json(new { success = true, habitId = habit.HabitId });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // POST: /Habits/LogHabit
        [HttpPost]
        public async Task<IActionResult> LogHabit([FromBody] LogHabitRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            try
            {
                var today = DateTime.Today;

                var existingLog = await _context.HabitLogs
                    .FirstOrDefaultAsync(h => h.HabitId == request.HabitId && h.LogDate.Date == today);

                if (existingLog != null)
                {
                    existingLog.Completed = request.Completed;
                    existingLog.ActualValue = request.ActualValue;
                    existingLog.Notes = request.Notes;
                }
                else
                {
                    var newLog = new HabitLog
                    {
                        HabitId = request.HabitId,
                        LogDate = today,
                        Completed = request.Completed,
                        ActualValue = request.ActualValue,
                        Notes = request.Notes
                    };
                    _context.HabitLogs.Add(newLog);
                }

                await _context.SaveChangesAsync();

                // Tính streak (số ngày liên tiếp hoàn thành)
                var streak = await CalculateStreak(request.HabitId);

                return Json(new { success = true, streak });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: /Habits/Statistics/{id}
        [HttpGet]
        public async Task<IActionResult> Statistics(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var habit = await _context.Habits
                .Include(h => h.HabitLogs)
                .FirstOrDefaultAsync(h => h.HabitId == id && h.UserId == userId.Value);

            if (habit == null) return NotFound();

            // Thống kê 30 ngày
            var thirtyDaysAgo = DateTime.Today.AddDays(-30);
            var logs = habit.HabitLogs
                .Where(l => l.LogDate >= thirtyDaysAgo)
                .OrderBy(l => l.LogDate)
                .ToList();

            var completionRate = logs.Any()
                ? (double)logs.Count(l => l.Completed) / logs.Count * 100
                : 0;

            var currentStreak = await CalculateStreak(id);
            var longestStreak = await CalculateLongestStreak(id);

            ViewBag.Habit = habit;
            ViewBag.Logs = logs;
            ViewBag.CompletionRate = completionRate;
            ViewBag.CurrentStreak = currentStreak;
            ViewBag.LongestStreak = longestStreak;

            return View();
        }

        // DELETE: /Habits/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var habit = await _context.Habits
                .FirstOrDefaultAsync(h => h.HabitId == id && h.UserId == userId.Value);

            if (habit == null) return Json(new { error = "Không tìm thấy thói quen" });

            habit.IsActive = false;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Helper: Tính streak hiện tại
        private async Task<int> CalculateStreak(int habitId)
        {
            var logs = await _context.HabitLogs
                .Where(h => h.HabitId == habitId && h.Completed)
                .OrderByDescending(h => h.LogDate)
                .ToListAsync();

            if (!logs.Any()) return 0;

            var streak = 0;
            var currentDate = DateTime.Today;

            foreach (var log in logs)
            {
                if (log.LogDate.Date == currentDate)
                {
                    streak++;
                    currentDate = currentDate.AddDays(-1);
                }
                else
                {
                    break;
                }
            }

            return streak;
        }

        // Helper: Tính streak dài nhất
        private async Task<int> CalculateLongestStreak(int habitId)
        {
            var logs = await _context.HabitLogs
                .Where(h => h.HabitId == habitId && h.Completed)
                .OrderBy(h => h.LogDate)
                .ToListAsync();

            if (!logs.Any()) return 0;

            var maxStreak = 0;
            var currentStreak = 1;

            for (int i = 1; i < logs.Count; i++)
            {
                var diff = (logs[i].LogDate - logs[i - 1].LogDate).Days;
                if (diff == 1)
                {
                    currentStreak++;
                }
                else
                {
                    maxStreak = Math.Max(maxStreak, currentStreak);
                    currentStreak = 1;
                }
            }

            return Math.Max(maxStreak, currentStreak);
        }
    }

    public class CreateHabitRequest
    {
        public string HabitName { get; set; } = string.Empty;
        public string HabitType { get; set; } = string.Empty;
        public int? GoalValue { get; set; }
        public string? IconEmoji { get; set; }
    }

    public class LogHabitRequest
    {
        public int HabitId { get; set; }
        public bool Completed { get; set; }
        public int? ActualValue { get; set; }
        public string? Notes { get; set; }
    }
}