    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using NutritionWebApp.Models.DataAccess;
    using NutritionWebApp.Models.Entities;
    using NutritionWebApp.Services;
    using NutritionWebApp.ViewModels;
    using System;
    using System.Text.Json;
    using System.Text.RegularExpressions; 

    namespace NutritionWebApp.Controllers
    {
        public class ExerciseController : Controller
        {
            private readonly DataContext _context;
            private readonly IYoutubeService _youtubeService; 
            private const string ADMIN_EMAIL = "nhathuy.hado@gmail.com";

            public ExerciseController(DataContext context, IYoutubeService youtubeService)
            {
                _context = context;
                _youtubeService = youtubeService;
            }
            // Hàm kiểm tra quyền Admin
            private async Task<bool> IsAdmin()
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue) return false;

                var user = await _context.Users.FindAsync(userId.Value);
                // So sánh email của người dùng đang đăng nhập với email Admin
                return user != null && user.Email.ToLower() == ADMIN_EMAIL.ToLower();
            }

            public IActionResult Index(string group = "")
            {
                var videos = string.IsNullOrEmpty(group)
                    ? _context.ExerciseVideos.ToList()
                    : _context.ExerciseVideos.Where(v => v.MuscleGroup == group).ToList();
                ViewBag.Group = group;
                return View(videos);
            }
            [HttpGet]
            // Endpoint này chỉ dùng cho AI tra cứu
            public async Task<IActionResult> GetVideosJson(string group)
            {
                // Không cần kiểm tra đăng nhập vì đây là API nội bộ cho AI
                if (string.IsNullOrEmpty(group))
                {
                    return Json(new { error = "Vui lòng cung cấp tên nhóm cơ." });
                }

                // Tra cứu video dựa trên tên nhóm cơ
                var videos = await _context.ExerciseVideos
                    .Where(v => v.MuscleGroup.ToLower() == group.ToLower())
                    .Select(v => new
                    {
                        v.MuscleGroup,
                        v.YoutubeVideoUrl // [2]
                                          // Giả định Model ExerciseVideo có thêm Title và Duration
                                          // Tạm thời chỉ lấy URL và Group để AI trả lời
                    })
                    .ToListAsync();

                if (!videos.Any())
                {
                    return Json(new { error = $"Không tìm thấy video nào cho nhóm cơ {group}." });
                }

                return Json(videos);
            }
            /// <summary>
            /// Trích xuất giá trị SRC (link embed) từ chuỗi IFRAME hoặc URL thô.
            /// </summary>
            private string? ExtractEmbedSrc(string rawInput)
            {
                if (string.IsNullOrWhiteSpace(rawInput)) return null;

                // 1. Xử lý trường hợp người dùng nhập HTML IFRAME đầy đủ             
                if (rawInput.Contains("<iframe"))
                {
                    int srcStart = rawInput.IndexOf("src=\"");
                    if (srcStart == -1) return null;
                    srcStart += 5;
                    int srcEnd = rawInput.IndexOf("\"", srcStart);
                    if (srcEnd == -1) return null;
                    string srcUrl = rawInput.Substring(srcStart, srcEnd - srcStart);

                    // Code gốc đang cắt ?si= (theo [1]), nhưng nếu bạn muốn giữ nó:
                    // Cần kiểm tra lại: Nếu bạn muốn giữ ?si= (để theo dõi nguồn chia sẻ), 
                    // BẠN KHÔNG ĐƯỢC CẮT NÓ Ở ĐÂY. 
                    // Nếu bạn đã xóa logic cắt ?si= ở các bước trước, giữ nguyên.
                    // Logic gốc:
                    // int qIndex = srcUrl.IndexOf('?'); 
                    // if (qIndex != -1) { srcUrl = srcUrl.Substring(0, qIndex); } 

                    if (srcUrl.Contains("youtube.com/embed/"))
                    {
                        return srcUrl; // Trả về nguyên URL nhúng từ IFRAME
                    }
                    return null;
                }

                // 2. Nếu người dùng chỉ nhập URL thô (youtu.be/ID, watch?v=ID)

                // --> FIX LỖI: Lấy ID và chuyển thành URL nhúng chuẩn
                // Gọi hàm ExtractVideoId (đã sửa ở Bước 1)
                var videoId = _youtubeService.ExtractVideoId(rawInput);

                if (videoId == null)
                {
                    return null; // Không trích xuất được ID
                }

                // TẠO URL NHÚNG CHUẨN và trả về để lưu vào DB [4]
                return $"https://www.youtube.com/embed/{videoId}";
            }
            [HttpGet]
            public async Task<IActionResult> AddVideo()
            {
                // Kiểm tra quyền: Chỉ Admin mới được truy cập [1, 2]
                if (!await IsAdmin())
                {
                    // Chuyển hướng hoặc báo lỗi nếu không phải Admin
                    return RedirectToAction("Index", "Home");
                }
                // Định nghĩa danh sách nhóm cơ để hiển thị trong form
                ViewBag.Groups = new[] { "Đùi", "Lưng", "Ngực", "Bụng", "Vai", "Tay trước", "Tay sau" };
                return View(new AddVideoViewModel());
            }

            [HttpPost]
            public async Task<IActionResult> AddVideo(AddVideoViewModel model)
            {
                if (!await IsAdmin())
                {
                    return RedirectToAction("Index", "Home");
                }
                ViewBag.Groups = new[] { "Đùi", "Lưng", "Ngực", "Bụng", "Vai", "Tay trước", "Tay sau" };

                if (!ModelState.IsValid)
                {
                    return View(model);
                }
                string? finalEmbedUrl = ExtractEmbedSrc(model.YoutubeVideoUrl);
                if (finalEmbedUrl == null)
                {
                    // Nếu người dùng nhập link IFRAME mà không thể tách SRC, báo lỗi
                    ModelState.AddModelError("YoutubeVideoUrl", "Không thể trích xuất URL nhúng (SRC) từ chuỗi nhập vào. Vui lòng kiểm tra lại cú pháp IFRAME.");
                    // Cần tải lại ViewBag.Groups trước khi trả về View
                    ViewBag.Groups = new[] { "Đùi", "Lưng", "Ngực", "Bụng", "Vai", "Tay trước", "Tay sau" };
                    return View(model);
                }
                // Gọi YouTube Service để lấy Title và Duration
                var (title, duration, error) = await _youtubeService.GetVideoMetadataAsync(finalEmbedUrl);

                if (error != null)
                {
                    ModelState.AddModelError("", $"Lỗi YouTube API: {error}. Vui lòng kiểm tra lại link.");
                    // Cần tải lại ViewBag.Groups trước khi trả về View
                    ViewBag.Groups = new[] { "Đùi", "Lưng", "Ngực", "Bụng", "Vai", "Tay trước", "Tay sau" };
                    return View(model);
                }


                // Lưu vào Database [3, 4]
                var newVideo = new ExerciseVideo
                {
                    MuscleGroup = model.MuscleGroup,
                    YoutubeVideoUrl = finalEmbedUrl,
                    Title = title,
                    Duration = duration
                };

                _context.ExerciseVideos.Add(newVideo);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Đã thêm video '{title}' thành công!";
                // Sau khi thêm, chuyển hướng về trang danh sách bài tập
                return RedirectToAction("Index", "Exercise");
            }            
        }
    }
