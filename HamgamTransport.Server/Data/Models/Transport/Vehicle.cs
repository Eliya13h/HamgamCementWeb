using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Transport;

public class Vehicle : BaseEntity
{
    [Key]
    public int VehicleId { get; set; }

    // کد خودکار VH-00001
    public string Code { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;

    public int VehicleTypeId { get; set; }
    public virtual VehicleType? VehicleType { get; set; }

    public int VehicleOwnerId { get; set; }
    public virtual VehicleOwner? VehicleOwner { get; set; }

    public string ChassisNumber { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? ManufactureYear { get; set; }

    // وزن و حجم فقط برای بونکر
    [Column(TypeName = "decimal(18,4)")]
    public decimal? WeightTon { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? Volume { get; set; }

    // سهم پیش‌فرض درآمد مالک این وسیله (درصد)
    [Column(TypeName = "decimal(8,4)")]
    public decimal? DefaultIncomeSharePercent { get; set; }

    // راننده پیش‌فرض — برای هر نوع جز بونکر
    public int? DefaultDriverId { get; set; }
    public virtual Driver? DefaultDriver { get; set; }

    // مرکز هزینه برای گزارش سود/زیان هر وسیله
    public int? CostCenterId { get; set; }
    public virtual CostCenter? CostCenter { get; set; }

    public int? VehiclePairId { get; set; }
    public virtual VehiclePair? VehiclePair { get; set; }

    public VehicleRole RoleInPair { get; set; } = VehicleRole.Primary;
}
