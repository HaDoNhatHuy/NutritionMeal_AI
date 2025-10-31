using Microsoft.AspNetCore.Mvc;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;
using NutritionWebApp.ViewModels;
using System;

namespace NutritionWebApp.Controllers
{
    public class SettingsController : Controller
    {
        private readonly DataContext _context;

        public SettingsController(DataContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            var model = new SettingsViewModel
            {
                Age = user.Age,
                Height = user.Height,
                Weight = user.Weight,
                Gender = user.Gender,
                Goal = user.Goal
            };

            // TÍNH BMR/TDEE TẠI ĐÂY
            if (user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                ViewBag.BMR = CalculateBMR(user);
                ViewBag.TDEE = CalculateTDEE(ViewBag.BMR);
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Index(SettingsViewModel model)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user == null) return NotFound();

                user.Age = model.Age;
                user.Height = model.Height;
                user.Weight = user.Weight;
                user.Gender = model.Gender;
                user.Goal = model.Goal;

                _context.Update(user);
                await _context.SaveChangesAsync();

                ViewBag.Message = "Cập nhật thành công!";

                // TÍNH LẠI BMR/TDEE SAU KHI CẬP NHẬT
                if (model.Age.HasValue && model.Height.HasValue && model.Weight.HasValue)
                {
                    var tempUser = new User { Age = model.Age, Height = model.Height, Weight = model.Weight, Gender = model.Gender };
                    ViewBag.BMR = CalculateBMR(tempUser);
                    ViewBag.TDEE = CalculateTDEE(ViewBag.BMR);
                }
            }

            return View(model);
        }

        // Hàm tính BMR
        private double CalculateBMR(User user)
        {
            if (user.Gender == true) // Nam
                return 88.362 + (13.397 * user.Weight.Value) + (4.799 * user.Height.Value) - (5.677 * user.Age.Value);
            else // Nữ
                return 447.593 + (9.247 * user.Weight.Value) + (3.098 * user.Height.Value) - (4.330 * user.Age.Value);
        }

        // Hàm tính TDEE
        private double CalculateTDEE(double bmr) => bmr * 1.55; // Trung bình
    }
}
