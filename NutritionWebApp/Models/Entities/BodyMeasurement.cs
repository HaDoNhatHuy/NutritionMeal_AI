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
        public float? Weight { get; set; }
        public float? Waist { get; set; }
        public float? Chest { get; set; }
        public float? Hips { get; set; }
        public float? Arms { get; set; }
        public float? Thighs { get; set; }
        public float? BodyFatPercentage { get; set; }
        public float? MuscleMass { get; set; }
        public string? PhotoUrl { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
