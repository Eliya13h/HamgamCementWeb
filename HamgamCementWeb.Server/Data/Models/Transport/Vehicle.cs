using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.People;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // وسیله نقلیه سنگین شرکت (کشنده، بونکر و ...)
    public class Vehicle : BaseEntity
    {
        [Key]
        public int VehicleID { get; set; }

        // کد یکتای داخلی وسیله نقلیه
        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        // شماره پلاک وسیله نقلیه
        [MaxLength(50)]
        public string PlateNumber { get; set; } = string.Empty;

        public int VehicleTypeId { get; set; }

        // برند / مدل وسیله نقلیه (مثلاً Volvo FH16)
        [MaxLength(200)]
        public string Brand { get; set; } = string.Empty;

        // سال ساخت
        public int? ModelYear { get; set; }

        [MaxLength(50)]
        public string? Color { get; set; }

        [MaxLength(100)]
        public string? ChassisNumber { get; set; }

        [MaxLength(100)]
        public string? EngineNumber { get; set; }

        // ظرفیت تانک سوخت به لیتر
        [Column(TypeName = "decimal(18,4)")]
        public decimal? FuelTankCapacity { get; set; }

        public string? Description { get; set; }

        // راننده پیش‌فرض وسیله نقلیه — FK در Fluent API
        public int? DefaultDriverId { get; set; }

        // صاحب / مالک وسیله نقلیه — هر مالک می‌تواند چند وسیله داشته باشد
        public int? VehicleOwnerId { get; set; }

        // لینک اختیاری به کارت دارایی ثابت — برای گزارش استهلاک ناوگان
        public int? FixedAssetId { get; set; }

        public virtual Driver? DefaultDriver { get; set; }

        [ForeignKey(nameof(VehicleOwnerId))]
        public virtual VehicleOwner? Owner { get; set; }

        [ForeignKey(nameof(VehicleTypeId))]
        public virtual VehicleType? VehicleType { get; set; }

        [ForeignKey(nameof(FixedAssetId))]
        public virtual FixedAsset? FixedAsset { get; set; }
    }
}
