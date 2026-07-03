using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.People;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // سفر حمل و نقل — تخصیص یک وسیله نقلیه به یک مسیر برای انتقال بار
    public class TransportTrip : BaseEntity
    {
        [Key]
        public int TransportTripID { get; set; }

        // شماره یکتای سفر
        [MaxLength(50)]
        public string TripNumber { get; set; } = string.Empty;

        public int VehicleId { get; set; }

        public int TransportRouteId { get; set; }

        // راننده سفر — اگر خالی باشد از راننده پیش‌فرض وسیله نقلیه استفاده می‌شود
        public int? DriverId { get; set; }

        // شرح بار / محموله
        [MaxLength(500)]
        public string? CargoDescription { get; set; }

        // وزن بار به تن
        [Column(TypeName = "decimal(18,4)")]
        public decimal? CargoWeightTon { get; set; }

        public DateTime DepartureDate { get; set; }

        public DateTime? ArrivalDate { get; set; }

        // سوخت مصرف‌شده در سفر به لیتر (برای حساب عملکرد وسیله)
        [Column(TypeName = "decimal(18,4)")]
        public decimal? FuelConsumedLiters { get; set; }

        // کیلومتر شمار در شروع سفر
        [Column(TypeName = "decimal(18,4)")]
        public decimal? OdometerStart { get; set; }

        // کیلومتر شمار در پایان سفر
        [Column(TypeName = "decimal(18,4)")]
        public decimal? OdometerEnd { get; set; }

        public TripStatus Status { get; set; } = TripStatus.Planned;

        // درآمد سفر
        [Column(TypeName = "decimal(18,4)")]
        public decimal TripRevenue { get; set; }

        public string? Description { get; set; }

        [ForeignKey(nameof(DriverId))]
        public virtual Driver? Driver { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }

        [ForeignKey(nameof(TransportRouteId))]
        public virtual TransportRoute? Route { get; set; }
    }
}
