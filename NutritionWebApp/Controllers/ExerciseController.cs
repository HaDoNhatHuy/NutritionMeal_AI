using Microsoft.AspNetCore.Mvc;
using NutritionWebApp.Models.DataAccess;
using System;

namespace NutritionWebApp.Controllers
{
    public class ExerciseController : Controller
    {
        private readonly DataContext _context;
        public ExerciseController(DataContext context) => _context = context;

        public IActionResult Index(string group = "")
        {
            var videos = string.IsNullOrEmpty(group)
                ? _context.ExerciseVideos.ToList()
                : _context.ExerciseVideos.Where(v => v.MuscleGroup == group).ToList();
            ViewBag.Group = group;
            return View(videos);
        }
    }
}
