using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;
using NutritionWebApp.Services;
using System.Text.Json;

namespace NutritionWebApp.Controllers
{
    public class WorkoutController : Controller
    {
        private readonly DataContext _context;
        private readonly IExerciseDbService _exerciseDbService;

        public WorkoutController(DataContext context, IExerciseDbService exerciseDbService)
        {
            _context = context;
            _exerciseDbService = exerciseDbService;
        }

        // GET: /Workout/Index - Danh sách kế hoạch
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var plans = await _context.WorkoutPlans
                .Where(w => w.UserId == userId.Value)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(plans);
        }

        // GET: /Workout/Create - Form tạo kế hoạch
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Lấy danh sách Body Parts từ ExerciseDB
            var bodyParts = await _exerciseDbService.GetBodyPartList();
            ViewBag.BodyParts = bodyParts;

            return View();
        }

        // POST: /Workout/GeneratePlan - Gọi AI tạo plan
        [HttpPost]
        public async Task<IActionResult> GeneratePlan([FromBody] WorkoutRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            try
            {
                // 1. Lấy bài tập từ ExerciseDB theo equipment
                List<Exercise> exercises = new List<Exercise>();

                if (request.Equipment == "GymFull")
                {
                    // Lấy nhiều loại thiết bị
                    var barbellEx = await _exerciseDbService.GetExercisesByEquipment("barbell");
                    var dumbbellEx = await _exerciseDbService.GetExercisesByEquipment("dumbbell");
                    var cableEx = await _exerciseDbService.GetExercisesByEquipment("cable");
                    exercises.AddRange(barbellEx);
                    exercises.AddRange(dumbbellEx);
                    exercises.AddRange(cableEx);
                }
                else if (request.Equipment == "Home")
                {
                    var bodyWeightEx = await _exerciseDbService.GetExercisesByEquipment("body weight");
                    exercises.AddRange(bodyWeightEx);
                }
                else // Minimal
                {
                    var dumbbellEx = await _exerciseDbService.GetExercisesByEquipment("dumbbell");
                    var bandEx = await _exerciseDbService.GetExercisesByEquipment("band");
                    exercises.AddRange(dumbbellEx);
                    exercises.AddRange(bandEx);
                }

                // 2. Nếu có weak body parts, ưu tiên lấy thêm
                if (request.WeakBodyParts != null && request.WeakBodyParts.Any())
                {
                    foreach (var bodyPart in request.WeakBodyParts)
                    {
                        var specificEx = await _exerciseDbService.GetExercisesByBodyPart(bodyPart);
                        exercises.AddRange(specificEx);
                    }
                }

                // 3. Loại bỏ duplicate và giới hạn
                exercises = exercises
                    .GroupBy(e => e.Id)
                    .Select(g => g.First())
                    .Take(100) // Giới hạn 100 bài tập
                    .ToList();

                // 4. Gọi Python AI Service
                var aiPayload = new
                {
                    goal = request.Goal,
                    level = request.Level,
                    duration = request.Duration,
                    frequency = request.Frequency,
                    equipment = request.Equipment,
                    exercises = exercises.Select(e => new
                    {
                        name = e.Name,
                        target = e.Target,
                        equipment = e.Equipment,
                        gifUrl = e.GifUrl,
                        instructions = e.Instructions
                    }),
                    weakBodyParts = request.WeakBodyParts ?? new List<string>(),
                    injuries = request.Injuries ?? "Không"
                };

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(10);
                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(aiPayload),
                    System.Text.Encoding.UTF8,
                    "application/json"
                );

                var response = await client.PostAsync("http://localhost:5000/generate_workout_plan", jsonContent);
                var jsonString = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { error = "Lỗi AI: " + jsonString });
                }

                // 5. Lưu vào Database
                var planData = JsonSerializer.Deserialize<JsonElement>(jsonString);

                var workoutPlan = new WorkoutPlan
                {
                    UserId = userId.Value,
                    PlanName = planData.GetProperty("planName").GetString() ?? "Workout Plan",
                    Goal = request.Goal,
                    Level = request.Level,
                    Duration = 4, // 4 weeks
                    Frequency = request.Frequency,
                    Equipment = request.Equipment,
                    PlanJson = jsonString,
                    CreatedAt = DateTime.Now,
                    IsActive = true
                };

                _context.WorkoutPlans.Add(workoutPlan);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    planId = workoutPlan.PlanId,
                    plan = planData
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = $"Lỗi: {ex.Message}" });
            }
        }

        // GET: /Workout/View/{id} - Xem chi tiết plan
        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            var plan = await _context.WorkoutPlans
                .FirstOrDefaultAsync(w => w.PlanId == id && w.UserId == userId.Value);

            if (plan == null) return NotFound();

            // Parse JSON
            var planData = JsonSerializer.Deserialize<JsonElement>(plan.PlanJson);
            ViewBag.PlanData = planData;
            ViewBag.Plan = plan;

            // Lấy progress
            var progress = await _context.WorkoutProgress
                .Where(p => p.PlanId == id)
                .ToListAsync();

            ViewBag.Progress = progress;

            return View();
        }

        // POST: /Workout/UpdateProgress - Đánh dấu hoàn thành
        [HttpPost]
        public async Task<IActionResult> UpdateProgress([FromBody]ProgressUpdate update)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });
            try
            {
                var existing = await _context.WorkoutProgress
                    .FirstOrDefaultAsync(p =>
                        p.PlanId == update.PlanId &&
                        p.WeekNumber == update.WeekNumber &&
                        p.DayNumber == update.DayNumber &&
                        p.ExerciseName == update.ExerciseName &&
                        p.UserId == userId.Value);

                if (existing != null)
                {
                    existing.Completed = update.Completed;
                    // CẬP NHẬT CÁC TRƯỜNG MỚI (F15):
                    existing.SetsCompleted = update.SetsCompleted;
                    existing.RepsCompleted = update.RepsCompleted;
                    existing.WeightUsed = update.WeightUsed;
                    existing.Notes = update.Notes;
                    existing.Rating = update.Rating;
                    existing.CompletedAt = DateTime.Now;
                }
                else if (update.Completed)
                {
                    var newProgress = new WorkoutProgress
                    {
                        PlanId = update.PlanId,
                        UserId = userId.Value,
                        WeekNumber = update.WeekNumber,
                        DayNumber = update.DayNumber,
                        ExerciseName = update.ExerciseName,
                        Completed = update.Completed,
                        // LƯU TRƯỜNG MỚI (F15):
                        SetsCompleted = update.SetsCompleted,
                        RepsCompleted = update.RepsCompleted,
                        WeightUsed = update.WeightUsed,
                        Notes = update.Notes,
                        Rating = update.Rating,
                        CompletedAt = DateTime.Now
                    };
                    _context.WorkoutProgress.Add(newProgress);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }

        // DELETE: /Workout/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Chưa đăng nhập" });

            var plan = await _context.WorkoutPlans
                .FirstOrDefaultAsync(w => w.PlanId == id && w.UserId == userId.Value);

            if (plan == null) return Json(new { error = "Không tìm thấy kế hoạch" });

            _context.WorkoutPlans.Remove(plan);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: /Workout/Library - Trang chính Thư viện Bài tập
        //    [HttpGet]
        //    public async Task<IActionResult> Library(string group = "all")
        //    {
        //        // Lấy danh sách nhóm cơ từ API (để tạo bộ lọc)
        //        var bodyParts = await _exerciseDbService.GetBodyPartList(); // [4]
        //        ViewBag.BodyParts = bodyParts;
        //        ViewBag.CurrentGroup = group;

        //        List<Exercise> exercises = new List<Exercise>();

        //        if (group == "all" || string.IsNullOrEmpty(group))
        //        {
        //            // Lấy tất cả bài tập (hoặc một mẫu đại diện)
        //            // Lưu ý: Việc lấy TẤT CẢ bài tập có thể rất lớn,
        //            // nên chúng ta sẽ lấy mẫu 100-200 bài tập theo Body Part List
        //            // (Chúng ta sẽ gọi GetExercisesByBodyPart cho các nhóm cơ chính)
        //            var principalParts = new List<string> { "back", "cardio", "chest", "shoulders", "upper arms", "upper legs", "waist" };

        //            // Tải các bài tập chính:
        //            foreach (var part in principalParts)
        //            {
        //                var partExercises = await _exerciseDbService.GetExercisesByBodyPart(part);
        //                exercises.AddRange(partExercises);
        //            }

        //            // Loại bỏ trùng lặp và giới hạn
        //            exercises = exercises
        //                .GroupBy(e => e.Id)
        //                .Select(g => g.First())
        //                .Take(200) // Giới hạn tổng số bài tập hiển thị
        //                .ToList();
        //        }
        //        else
        //        {
        //            // Lọc theo nhóm cơ cụ thể nếu được chọn
        //            exercises = await _exerciseDbService.GetExercisesByBodyPart(group); // [2]
        //        }

        //        // Sắp xếp theo nhóm cơ trước khi gửi
        //        exercises = exercises.OrderBy(e => e.BodyPart).ToList();

        //        return View(exercises);
        //    }
        //}
        [HttpGet]
        public async Task<IActionResult> Library(string bodyPart = "all", string equipment = "all", string target = "all")
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");
            // 1. Lấy dữ liệu cho các dropdown bộ lọc
            ViewBag.BodyParts = await _exerciseDbService.GetBodyPartList();
            ViewBag.Equipments = await _exerciseDbService.GetEquipmentList();
            ViewBag.Targets = await _exerciseDbService.GetTargetList(); // Cần thêm hàm này ở Service bước 1

            // 2. Giữ lại giá trị đang chọn để hiển thị lại trên UI
            ViewBag.CurrentBodyPart = bodyPart;
            ViewBag.CurrentEquipment = equipment;
            ViewBag.CurrentTarget = target;

            // 3. Gọi hàm lọc tổng hợp
            // (Bạn cần cast sang ExerciseDbService nếu Interface chưa kịp update, hoặc update Interface)
            var exercises = await ((ExerciseDbService)_exerciseDbService).FilterExercises(bodyPart, equipment, target);

            // 4. Giới hạn số lượng hiển thị để trang load nhanh
            return View(exercises.Take(50).ToList());
        }
        // Model cho việc lưu bài tập yêu thích
        public class CustomExerciseSaveRequest
        {
            public string ExerciseId { get; set; } = string.Empty;
            public string ExerciseName { get; set; } = string.Empty;
            public string BodyPart { get; set; } = string.Empty;
        }

        // POST: /Workout/SaveCustomExercise (F10)
        [HttpPost]
        public async Task<IActionResult> SaveCustomExercise([FromBody] CustomExerciseSaveRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { success = false, error = "Chưa đăng nhập" });

            // Logic này sẽ phức tạp nếu chúng ta muốn lưu Sets/Reps tùy chỉnh.
            // Tạm thời, chúng ta sẽ tạo một "kế hoạch tập luyện" mới dạng tùy chỉnh (Custom Plan) 
            // hoặc lưu vào một Entity riêng để sau này dùng cho Workout Plan Builder.

            // Tạm thời, chỉ trả về thành công để client biết đã được lưu vào danh sách yêu thích
            // Giả định: Bạn đã có cơ chế lưu trữ Custom Exercise Library cho User.
            // Trong môi trường này, chúng ta chỉ xác nhận rằng dữ liệu đã được nhận.

            // Ví dụ về việc tạo một "kế hoạch" tạm thời (Nên dùng Entity riêng):
            var customPlan = new WorkoutPlan
            {
                UserId = userId.Value,
                PlanName = $"Bài tập yêu thích: {request.ExerciseName}",
                Goal = "Custom",
                Level = "N/A",
                Duration = 0,
                Frequency = 0,
                Equipment = request.BodyPart,
                PlanJson = System.Text.Json.JsonSerializer.Serialize(new
                {
                    ExerciseId = request.ExerciseId,
                    Name = request.ExerciseName,
                    Note = "Đã lưu từ thư viện bài tập."
                }),
                CreatedAt = DateTime.Now,
                IsActive = false // Là bản ghi lưu trữ, không phải plan chính
            };

            _context.WorkoutPlans.Add(customPlan);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Đã lưu '{request.ExerciseName}' vào thư viện cá nhân." });
        }

        // Request Models
        public class WorkoutRequest
        {
            public string Goal { get; set; } = string.Empty; // TangCo, GiamMo, TangSucBen, TongThe
            public string Level { get; set; } = string.Empty; // Newbie, Intermediate, Advanced
            public int Duration { get; set; } // phút/buổi
            public int Frequency { get; set; } // ngày/tuần
            public string Equipment { get; set; } = string.Empty; // GymFull, Home, Minimal
            public List<string>? WeakBodyParts { get; set; }
            public string? Injuries { get; set; }
        }

        public class ProgressUpdate
        {
            public int PlanId { get; set; }
            public int WeekNumber { get; set; }
            public int DayNumber { get; set; }
            public string ExerciseName { get; set; } = string.Empty;
            public bool Completed { get; set; }
            public int SetsCompleted { get; set; }
            public string? RepsCompleted { get; set; }
            public string? WeightUsed { get; set; }
            public string? Notes { get; set; }
            public int? Rating { get; set; }
        }
    }
}
