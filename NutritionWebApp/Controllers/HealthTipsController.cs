using Microsoft.AspNetCore.Mvc;

namespace NutritionWebApp.Controllers
{
    public class HealthTipsController : Controller
    {
        // Trang chủ Health Tips
        public IActionResult Index()
        {
            return View();
        }

        // Tháp dinh dưỡng
        public IActionResult NutritionPyramid()
        {
            return View();
        }

        // Uống đủ nước
        public IActionResult Hydration()
        {
            return View();
        }

        // Tập luyện khoa học
        public IActionResult Exercise()
        {
            return View();
        }

        // Giấc ngủ và sức khỏe
        public IActionResult Sleep()
        {
            return View();
        }

        // Quản lý stress
        public IActionResult Stress()
        {
            return View();
        }
    }
}