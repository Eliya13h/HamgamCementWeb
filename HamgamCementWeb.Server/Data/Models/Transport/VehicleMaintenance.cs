using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // ثبت تعمیرات و سرویس‌های دوره‌ای وسیله نقلیه
    public class VehicleMaintenance : BaseEntity
    {
        [Key]
        public int VehicleMaintenanceID { get; set; }

        public int VehicleId { get; set; }

        // عنوان تعمیر / سرویس (مثلاً تعویض روغن موتور)
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        public DateTime MaintenanceDate { get; set; }

        // کیلومتر شمار در زمان سرویس
        [Column(TypeName = "decimal(18,4)")]
        public decimal? OdometerKm { get; set; }

        // هزینه تعمیر / سرویس
        [Column(TypeName = "decimal(18,4)")]
        public decimal Cost { get; set; }

        // نام تعمیرگاه / تعمیرکار
        [MaxLength(200)]
        public string? WorkshopName { get; set; }

        // تاریخ سرویس بعدی برای پیگیری سرویس‌های دوره‌ای
        public DateTime? NextServiceDate { get; set; }

        public string? Description { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }
    }
}
