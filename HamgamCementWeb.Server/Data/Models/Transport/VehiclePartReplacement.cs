using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // ثبت تعویض قطعات و لوازم مصرفی وسیله نقلیه
    public class VehiclePartReplacement : BaseEntity
    {
        [Key]
        public int VehiclePartReplacementID { get; set; }

        public int VehicleId { get; set; }

        // نام قطعه / لوازم مصرفی (مثلاً فیلتر روغن)
        [MaxLength(300)]
        public string PartName { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Quantity { get; set; } = 1;

        // قیمت واحد قطعه
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }

        // هزینه کل = تعداد × قیمت واحد — هنگام ذخیره محاسبه می‌شود
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalCost { get; set; }

        public DateTime ReplacementDate { get; set; }

        // کیلومتر شمار در زمان تعویض
        [Column(TypeName = "decimal(18,4)")]
        public decimal? OdometerKm { get; set; }

        public string? Description { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }
    }
}
