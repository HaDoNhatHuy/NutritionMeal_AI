using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using System;

namespace NutritionWebApp.ViewModels
{
    public class AddMeasurementViewModel
    {
        // Fields từ BodyMeasurement
        [Required(ErrorMessage = "Cân nặng là bắt buộc")]
        public double? Weight { get; set; }

        public double? BodyFatPercentage { get; set; }

        public DateTime MeasureDate { get; set; } = DateTime.Today;

        // Fields mới cho ảnh (F6)
        public IFormFile? BeforeImage { get; set; }
        public IFormFile? AfterImage { get; set; }

        // Thêm trường URL ảnh để lưu trữ sau khi upload (Nếu bạn lưu URL trong Entity BodyMeasurement)
        public string? BeforeImageUrl { get; set; }
        public string? AfterImageUrl { get; set; }
    }
}