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

        // BẢNG HỆ SỐ HOẠT ĐỘNG (MỚI)
        private double GetActivityFactor(string? level)
        {
            return level switch
            {
                "Sedentary" => 1.2,
                "Light" => 1.375,
                "Moderate" => 1.55,
                "Heavy" => 1.725,
                "Extreme" => 1.9,
                _ => 1.55,
            };
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
                Goal = user.Goal,
                ActivityLevel = user.ActivityLevel,
                Pathology = user.Pathology
            };

            // TÍNH BMR/TDEE TẠI ĐÂY
            if (user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                ViewBag.BMR = CalculateBMR(user);
                ViewBag.TDEE = CalculateTDEE(ViewBag.BMR, user.ActivityLevel);
            }

            return View(model);
        }

        //[HttpPost]
        //public async Task<IActionResult> Index(SettingsViewModel model)
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");
        //    if (!userId.HasValue) return RedirectToAction("Login", "Account");

        //    if (ModelState.IsValid)
        //    {
        //        var user = await _context.Users.FindAsync(userId.Value);
        //        if (user == null) return NotFound();

        //        user.Age = model.Age;
        //        user.Height = model.Height;
        //        user.Weight = model.Weight;
        //        user.Gender = model.Gender;
        //        user.Goal = model.Goal;
        //        user.ActivityLevel = model.ActivityLevel;

        //        _context.Update(user);
        //        await _context.SaveChangesAsync();

        //        ViewBag.Message = "Cập nhật thành công!";

        //        // TÍNH LẠI BMR/TDEE SAU KHI CẬP NHẬT
        //        if (model.Age.HasValue && model.Height.HasValue && model.Weight.HasValue)
        //        {
        //            var tempUser = new User { Age = model.Age, Height = model.Height, Weight = model.Weight, Gender = model.Gender };
        //            ViewBag.BMR = CalculateBMR(tempUser);
        //            ViewBag.TDEE = CalculateTDEE(ViewBag.BMR, tempUser.ActivityLevel);
        //        }
        //    }

        //    return View(model);
        //}
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
                user.Weight = model.Weight;
                user.Gender = model.Gender;
                user.Goal = model.Goal;
                user.ActivityLevel = model.ActivityLevel;
                user.Pathology = model.Pathology;

                // Sửa lỗi cú pháp trong nguồn [2]
                // user.Weight = user.Weight; -> user.Weight = model.Weight;

                _context.Update(user);
                await _context.SaveChangesAsync();

                // THAY ĐỔI: Chuyển hướng người dùng đến một View Mode mới
                // Lưu một thông báo tạm thời vào TempData
                TempData["SuccessMessage"] = "Cài đặt của bạn đã được cập nhật thành công!";
                return RedirectToAction("ViewStats");
            }

            // Nếu validation thất bại, quay lại View Index (Edit Mode)
            // Cần tính lại BMR/TDEE nếu validation thất bại (Logic này giữ nguyên)
            if (model.Age.HasValue && model.Height.HasValue && model.Weight.HasValue)
            {
                var tempUser = new User { Age = model.Age, Height = model.Height, Weight = model.Weight, Gender = model.Gender, ActivityLevel = model.ActivityLevel };
                ViewBag.BMR = CalculateBMR(tempUser);
                ViewBag.TDEE = CalculateTDEE(ViewBag.BMR, tempUser.ActivityLevel);
            }

            return View(model);
        }
        [HttpGet]
        public async Task<IActionResult> ViewStats()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var user = await _context.Users.FindAsync(userId.Value);
            if (user == null) return NotFound();

            // Tính toán lại BMR/TDEE để hiển thị
            if (user.Age.HasValue && user.Height.HasValue && user.Weight.HasValue)
            {
                ViewBag.BMR = CalculateBMR(user);
                ViewBag.TDEE = CalculateTDEE(ViewBag.BMR, user.ActivityLevel);
            }

            // Lấy thông báo thành công (nếu có)
            ViewBag.SuccessMessage = TempData["SuccessMessage"];

            // Gửi đối tượng User trực tiếp đến View để hiển thị
            return View(user);
        }
        // Hàm tính BMR
        public double CalculateBMR(User user)
        {
            if (user.Gender == true) // Nam
                return 88.362 + (13.397 * user.Weight.Value) + (4.799 * user.Height.Value) - (5.677 * user.Age.Value);
            else // Nữ
                return 447.593 + (9.247 * user.Weight.Value) + (3.098 * user.Height.Value) - (4.330 * user.Age.Value);
        }

        // Hàm tính TDEE MỚI (Thay thế hàm cũ [4])
        public double CalculateTDEE(double bmr, string? activityLevel)
        {
            return bmr * GetActivityFactor(activityLevel);
        }
        // Định nghĩa cấu trúc đơn giản để trả về mục tiêu
        public class MacroGoals
        {
            public double ProteinGrams { get; set; }
            public double CarbGrams { get; set; }
            public double FatGrams { get; set; }
            public double ProteinPct { get; set; }
            public double CarbPct { get; set; }
            public double FatPct { get; set; }
        }

        /// <summary>
        /// Tính toán mục tiêu Macros hàng ngày (gram) dựa trên TDEE và Goal.
        /// </summary>
        public MacroGoals GetMacroGoalsInGrams(double tdee, string? goal)
        {
            // Tỷ lệ Calories: Protein/Carb = 4 kcal/g, Fat = 9 kcal/g
            double proteinPct = 0.30; // Mặc định 30%
            double carbPct = 0.40;    // Mặc định 40%
            double fatPct = 0.30;     // Mặc định 30%

            // Áp dụng tỷ lệ dựa trên Mục tiêu (Goal)
            if (goal == "GiamCan") // Giảm cân: Cao Protein, Carb vừa, Fat thấp
            {
                proteinPct = 0.40; // 40% protein
                carbPct = 0.35;    // 35% carb
                fatPct = 0.25;     // 25% fat
            }
            else if (goal == "TangCan") // Tăng cân: Carb cao, Protein vừa
            {
                proteinPct = 0.25; // 25% protein
                carbPct = 0.55;    // 55% carb
                fatPct = 0.20;     // 20% fat
            }
            else if (goal == "EatClean" || goal == "AnChay") // Duy trì/EatClean: Cân bằng
            {
                // Giữ nguyên mặc định hoặc tùy chỉnh thêm
            }

            // Tính toán Calories từ tỷ lệ
            double proteinCal = tdee * proteinPct;
            double carbCal = tdee * carbPct;
            double fatCal = tdee * fatPct;

            // Chuyển Calo thành Grams
            double proteinGrams = proteinCal / 4;
            double carbGrams = carbCal / 4;
            double fatGrams = fatCal / 9;

            return new MacroGoals
            {
                ProteinGrams = proteinGrams,
                CarbGrams = carbGrams,
                FatGrams = fatGrams,
                ProteinPct = proteinPct * 100,
                CarbPct = carbPct * 100,
                FatPct = fatPct * 100
            };
        }
    }
}
