// Controllers/RecipeController.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NutritionWebApp.Models.DataAccess;
using NutritionWebApp.Models.Entities;
using NutritionWebApp.ViewModels;
using System.Text.Json; // Để đọc/ghi JSON Ingredients/Instructions

namespace NutritionWebApp.Controllers
{
    public class RecipeController : Controller
    {
        private readonly DataContext _context;

        public RecipeController(DataContext context)
        {
            _context = context;
        }

        // GET: /Recipe/Index - Hiển thị danh sách công thức
        //[HttpGet]
        //public async Task<IActionResult> Index(string category = "all")
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");

        //    var query = _context.Recipes
        //        .Where(r => r.IsPublic); // Chỉ hiển thị các công thức công khai

        //    if (category != "all")
        //    {
        //        query = query.Where(r => r.Category == category);
        //    }

        //    var recipes = await query
        //        .OrderByDescending(r => r.Likes) // Ưu tiên công thức được thích nhiều
        //        .ThenByDescending(r => r.CreatedAt)
        //        .Take(20) // Giới hạn 20 công thức
        //        .ToListAsync();

        //    // Lấy danh sách RecipeId mà người dùng đã thích (nếu đã đăng nhập)
        //    if (userId.HasValue)
        //    {
        //        var likedRecipeIds = await _context.RecipeLikes
        //           .Where(l => l.UserId == userId.Value)
        //           .Select(l => l.RecipeId)
        //           .ToListAsync();
        //        ViewBag.LikedRecipeIds = likedRecipeIds;
        //    }
        //    ViewBag.PopularRecipeIds = recipes.Where(r => r.Likes >= 10).Select(r => r.RecipeId).ToList();

        //    ViewBag.CurrentCategory = category;
        //    return View(recipes);
        //}
        [HttpGet]
        public async Task<IActionResult> Index(
                string category = "all",
                string sort = "newest",
                string search = "",
                int? minCal = null, int? maxCal = null, // Lọc Calo
                string mealType = "all" // Lọc bữa
            )
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            var query = _context.Recipes.Where(r => r.IsPublic);

            // 1. Filter cơ bản
            if (category != "all") query = query.Where(r => r.Category == category);
            if (!string.IsNullOrEmpty(search)) query = query.Where(r => r.RecipeName.Contains(search));

            // 2. Filter nâng cao (Meal Type - nếu bạn lưu MealType trong Category hoặc field riêng)
            // Giả sử Category chứa MealType (Sáng, Trưa, Tối), nếu không thì sửa logic tùy DB
            if (mealType != "all") query = query.Where(r => r.Category == mealType);

            // 3. Filter Calories
            if (minCal.HasValue) query = query.Where(r => r.Calories >= minCal.Value);
            if (maxCal.HasValue) query = query.Where(r => r.Calories <= maxCal.Value);

            // 4. Sort
            query = sort switch
            {
                "popular" => query.OrderByDescending(r => r.Likes),
                "low-cal" => query.OrderBy(r => r.Calories),
                "high-protein" => query.OrderByDescending(r => r.Protein), // Thêm sort Protein
                _ => query.OrderByDescending(r => r.CreatedAt)
            };

            var recipes = await query.Take(50).ToListAsync(); // Limit 50

            // Lấy liked recipes
            if (userId.HasValue)
            {
                var likedRecipeIds = await _context.RecipeLikes
                    .Where(l => l.UserId == userId.Value)
                    .Select(l => l.RecipeId)
                    .ToListAsync();
                ViewBag.LikedRecipeIds = likedRecipeIds;
            }

            ViewBag.CurrentCategory = category;
            ViewBag.CurrentSort = sort;
            ViewBag.SearchTerm = search;
            ViewBag.MinCal = minCal;
            ViewBag.MaxCal = maxCal;
            ViewBag.MealType = mealType;

            return View(recipes);
        }
        // UPDATE HÀM SAVE ĐỂ NHẬN ẢNH
        [HttpPost]
        public async Task<IActionResult> Save([FromForm] SaveRecipeRequest request, IFormFile? RecipeImage)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Yêu cầu đăng nhập." });

            string? imageUrl = null;

            // Xử lý Upload ảnh
            if (RecipeImage != null && RecipeImage.Length > 0)
            {
                var fileName = Guid.NewGuid().ToString() + Path.GetExtension(RecipeImage.FileName);
                var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads/recipes");
                Directory.CreateDirectory(uploadPath); // Tạo thư mục nếu chưa có

                using (var stream = new FileStream(Path.Combine(uploadPath, fileName), FileMode.Create))
                {
                    await RecipeImage.CopyToAsync(stream);
                }
                imageUrl = "/uploads/recipes/" + fileName;
            }

            var newRecipe = new Recipe
            {
                CreatedByUserId = userId.Value,
                RecipeName = request.RecipeName,
                Description = request.Description,
                Category = request.Category,
                Calories = request.Calories,
                Protein = request.Protein,
                Carbs = request.Carbs,
                Fat = request.Fat,
                Ingredients = System.Text.Json.JsonSerializer.Serialize(request.Ingredients),
                Instructions = System.Text.Json.JsonSerializer.Serialize(request.Instructions),
                CookingTime = request.CookingTime,
                IsPublic = request.IsPublic,
                ImageUrl = imageUrl, // Lưu URL ảnh
                Likes = 0,
                CreatedAt = DateTime.Now
            };

            _context.Recipes.Add(newRecipe);
            await _context.SaveChangesAsync();

            return Json(new { success = true, recipeId = newRecipe.RecipeId });
        }

        // GET: /Recipe/Details/{id} - Xem chi tiết công thức
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var recipe = await _context.Recipes
                .Include(r => r.CreatedBy)
                .FirstOrDefaultAsync(r => r.RecipeId == id && r.IsPublic);

            if (recipe == null) return NotFound();

            // Kiểm tra xem người dùng đã thích công thức này chưa
            bool isLiked = false;
            if (userId.HasValue)
            {
                isLiked = await _context.RecipeLikes
                    .AnyAsync(l => l.RecipeId == id && l.UserId == userId.Value);
            }

            ViewBag.IsLiked = isLiked;
            return View(recipe);
        }

        // POST: /Recipe/Like/{id} - Thêm/Xóa Like
        [HttpPost]
        public async Task<IActionResult> ToggleLike(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { success = false, error = "Yêu cầu đăng nhập." });

            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe == null) return Json(new { success = false, error = "Không tìm thấy công thức." });

            var existingLike = await _context.RecipeLikes
                .FirstOrDefaultAsync(l => l.RecipeId == id && l.UserId == userId.Value);

            if (existingLike != null)
            {
                // Un-like
                _context.RecipeLikes.Remove(existingLike);
                recipe.Likes = Math.Max(0, recipe.Likes - 1);
                await _context.SaveChangesAsync();
                return Json(new { success = true, action = "unliked", newCount = recipe.Likes });
            }
            else
            {
                // Like
                var newLike = new RecipeLike { RecipeId = id, UserId = userId.Value, LikedAt = DateTime.Now };
                _context.RecipeLikes.Add(newLike);
                recipe.Likes += 1;
                await _context.SaveChangesAsync();
                return Json(new { success = true, action = "liked", newCount = recipe.Likes });
            }
        }

        // GET: /Recipe/Create - Form tạo mới (Hoặc từ AI)
        //[HttpGet]
        //public IActionResult Create()
        //{
        //    var userId = HttpContext.Session.GetInt32("UserId");
        //    if (!userId.HasValue) return RedirectToAction("Login", "Account");
        //    // View này cần một form lớn cho phép nhập chi tiết: Tên, Calo, Macros, Nguyên liệu (JSON/Textarea), Hướng dẫn (JSON/Textarea)
        //    return View(new Recipe());
        //}

        // POST: /Recipe/Create
        // Logic lưu công thức thủ công hoặc công thức AI (Chuyển đổi từ JSON AI sang Recipe Entity)
        // (Chúng ta bỏ qua phần code lưu chi tiết để giữ trọng tâm, nhưng logic sẽ tương tự như các POST khác)
        // Yêu cầu: validate model và lưu vào _context.Recipes, set IsPublic = true/false tùy chọn.


        // GET: /Recipe/Create - Form tạo mới (Hoặc nơi hiển thị kết quả AI để lưu)
        [HttpGet]
        public IActionResult Create()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return RedirectToAction("Login", "Account");

            // Gửi danh sách Category để người dùng chọn (tái sử dụng từ Index)
            ViewBag.Categories = new List<string> { "Sáng", "Trưa", "Tối", "Ăn nhẹ", "Ăn chay", "Khác" };

            // Nếu có TempData chứa kết quả AI, hiển thị nó để review
            if (TempData.ContainsKey("AI_Recipe_Result"))
            {
                var jsonString = TempData["AI_Recipe_Result"] as string;
                var aiRecipe = JsonSerializer.Deserialize<SaveRecipeRequest>(jsonString!);
                return View(aiRecipe); // Gửi dữ liệu AI đến View để review/lưu
            }

            return View(new SaveRecipeRequest()); // Trả về form trống cho nhập thủ công
        }

        // POST: /Recipe/Save - Lưu công thức từ form hoặc từ kết quả AI
        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveRecipeRequest request)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue) return Json(new { error = "Yêu cầu đăng nhập." });

            if (string.IsNullOrEmpty(request.RecipeName) || request.Calories <= 0)
            {
                return Json(new { success = false, error = "Tên công thức và Calo là bắt buộc." });
            }

            // Chuyển đổi List<string> Ingredients/Instructions thành JSON string để lưu vào DB [4]
            var ingredientsJson = JsonSerializer.Serialize(request.Ingredients);
            var instructionsJson = JsonSerializer.Serialize(request.Instructions);

            var newRecipe = new Recipe
            {
                CreatedByUserId = userId.Value,
                RecipeName = request.RecipeName,
                Description = request.Description,
                Category = request.Category,
                Calories = request.Calories,
                Protein = request.Protein,
                Carbs = request.Carbs,
                Fat = request.Fat,
                Ingredients = ingredientsJson,
                Instructions = instructionsJson,
                CookingTime = request.CookingTime,
                IsPublic = request.IsPublic,
                Likes = 0,
                CreatedAt = DateTime.Now
            };

            _context.Recipes.Add(newRecipe);
            await _context.SaveChangesAsync();

            return Json(new { success = true, recipeId = newRecipe.RecipeId });
        }

        // NEW: Action trung gian để lưu kết quả AI từ chatbox vào TempData và chuyển hướng
        // Endpoint này được gọi từ chatbox.js sau khi AI tạo công thức thành công
        [HttpPost]
        public IActionResult ReviewAndSaveRecipe([FromBody] SaveRecipeRequest request)
        {
            // Lưu kết quả JSON vào TempData và chuyển hướng đến trang Create/Review
            TempData["AI_Recipe_Result"] = JsonSerializer.Serialize(request);
            return Json(new { success = true, redirectUrl = Url.Action("Create", "Recipe") });
        }
    }
}