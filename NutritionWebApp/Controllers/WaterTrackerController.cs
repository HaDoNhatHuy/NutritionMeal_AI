using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;

namespace NutritionWebApp.Controllers
{
    public class WaterTrackerController : Controller
    {
        private readonly DataContext _context;

        public WaterTrackerController(DataContext context)
        {
            _context = context;
        }

        // GET: /WaterTracker/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var today = DateTime.Today;

            // Lấy hoặc tạo water intake hôm nay
            var todayIntake = await _context.WaterIntake
                .Include(w => w.WaterLogs)
                .FirstOrDefaultAsync(w => w.UserId == userId.Value && w.IntakeDate.Date == today);

            if (todayIntake == null)
            {
                // Tính goal dựa trên cân nặng (nếu có)
                var user = await _context.Users.FindAsync(userId.Value);
                var goalMl = 2000; // Default 2L

                if (user?.Weight != null)
                {
                    goalMl = (int)(user.Weight.Value * 35); // 35ml/kg
                }

                todayIntake = new WaterIntake
                {
                    UserId = userId.Value,
                    IntakeDate = today,
                    TotalMl = 0,
                    GoalMl = goalMl
                };

                _context.WaterIntake.Add(todayIntake);
                await _context.SaveChangesAsync();
            }

            // Lấy lịch sử 7 ngày
            var history = await _context.WaterIntake
                .Where(w => w.UserId == userId.Value && w.IntakeDate >= today.AddDays(-6))
                .OrderBy(w => w.IntakeDate)
                .ToListAsync();

            ViewBag.TodayIntake = todayIntake;
            ViewBag.History = history;

            return View();
        }

        // POST: /WaterTracker/AddWater
        [HttpPost]
        public async Task<IActionResult> AddWater([FromBody] AddWaterRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            try
            {
                var today = DateTime.Today;

                var todayIntake = await _context.WaterIntake.Include(w => w.WaterLogs)
                    .FirstOrDefaultAsync(w => w.UserId == userId.Value && w.IntakeDate.Date == today);

                if (todayIntake == null)
                {
                    return Json(new { error = "Không tìm thấy dữ liệu hôm nay" });
                }

                // Thêm log
                var waterLog = new WaterLog
                {
                    IntakeId = todayIntake.IntakeId,
                    AmountMl = request.AmountMl,
                    LoggedAt = DateTime.Now
                };

                _context.WaterLogs.Add(waterLog);

                // Cập nhật tổng
                todayIntake.TotalMl += request.AmountMl;

                await _context.SaveChangesAsync();

                //var progress = (double)todayIntake.TotalMl / todayIntake.GoalMl * 100;
                double progress = todayIntake.GoalMl > 0 ? ((double)todayIntake.TotalMl / todayIntake.GoalMl) * 100 : 0;

                return Json(new
                {
                    success = true,
                    totalMl = todayIntake.TotalMl,
                    goalMl = todayIntake.GoalMl,
                    progress = Math.Min(progress, 100)
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // POST: /WaterTracker/SetGoal
        [HttpPost]
        public async Task<IActionResult> SetGoal([FromBody] SetGoalRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            try
            {
                var today = DateTime.Today;

                var todayIntake = await _context.WaterIntake
                    .FirstOrDefaultAsync(w => w.UserId == userId.Value && w.IntakeDate.Date == today);

                if (todayIntake == null)
                {
                    return Json(new { error = "Không tìm thấy dữ liệu hôm nay" });
                }

                todayIntake.GoalMl = request.GoalMl;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // GET: /WaterTracker/History
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var history = await _context.WaterIntake
                .Where(w => w.UserId == userId.Value)
                .OrderByDescending(w => w.IntakeDate)
                .Take(30)
                .ToListAsync();

            return View(history);
        }
    }

    public class AddWaterRequest
    {
        public int AmountMl { get; set; }
    }

    public class SetGoalRequest
    {
        public int GoalMl { get; set; }
    }
}