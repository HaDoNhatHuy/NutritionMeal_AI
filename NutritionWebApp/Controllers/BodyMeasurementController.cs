// Controllers/BodyMeasurementController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;
using System;
using System.Linq;
using System.Text.Json;

namespace NutritionWebApp.Controllers
{
    public class BodyMeasurementController : Controller
    {
        private readonly DataContext _context;

        public BodyMeasurementController(DataContext context)
        {
            _context = context;
        }

        // GET: /BodyMeasurement/Index
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Lấy 30 lần đo gần nhất để hiển thị biểu đồ và danh sách
            var measurements = await _context.BodyMeasurements
                .Where(m => m.UserId == userId.Value)
                .OrderByDescending(m => m.MeasureDate)
                .Take(30)
                .ToListAsync();

            // Truyền dữ liệu cho biểu đồ (Chart.js)
            ViewBag.ChartDataJson = JsonSerializer.Serialize(
                measurements.Select(m => new {
                    Date = m.MeasureDate.ToString("yyyy-MM-dd"),
                    Weight = m.Weight,
                    BodyFat = m.BodyFatPercentage
                }).OrderBy(x => x.Date)
            );

            return View(measurements);
        }

        // POST: /BodyMeasurement/Add 
        [HttpPost]
        public async Task<IActionResult> Add([FromBody] BodyMeasurement measurement)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            // Kiểm tra dữ liệu bắt buộc (Weight)
            if (!measurement.Weight.HasValue || measurement.Weight.Value <= 0)
            {
                return Json(new { success = false, error = "Cân nặng là bắt buộc." });
            }

            measurement.UserId = userId.Value;
            // Nếu ngày đo không được nhập, lấy ngày hôm nay
            if (measurement.MeasureDate == DateTime.MinValue)
            {
                measurement.MeasureDate = DateTime.Today;
            }

            // Cập nhật cân nặng mới nhất vào User Entity [31]
            var user = await _context.Users.FindAsync(userId.Value);
            if (user != null)
            {
                user.Weight = measurement.Weight.Value;
                _context.Users.Update(user);
            }

            _context.BodyMeasurements.Add(measurement);
            await _context.SaveChangesAsync();
            return Json(new { success = true, id = measurement.MeasurementId });
        }

        // POST: /BodyMeasurement/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var measurement = await _context.BodyMeasurements
                .FirstOrDefaultAsync(m => m.MeasurementId == id && m.UserId == userId.Value);

            if (measurement == null)
            {
                return Json(new { error = "Không tìm thấy dữ liệu." });
            }

            _context.BodyMeasurements.Remove(measurement);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}