using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace NutritionWebApp.Models.Entities
{
    [Table("BodyMeasurements")]
    public class BodyMeasurement
    {
        [Key]
        public int MeasurementId { get; set; }

        [ForeignKey("UserId")]
        public int UserId { get; set; }
        public virtual User User { get; set; } = null!;

        public DateTime MeasureDate { get; set; }
        public double? Weight { get; set; }
        public double? Waist { get; set; }
        public double? Chest { get; set; }
        public double? Hips { get; set; }
        public double? Arms { get; set; }
        public double? Thighs { get; set; }
        public double? BodyFatPercentage { get; set; }
        public double? MuscleMass { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
