using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Transport;

public class VehiclePair : BaseEntity
{
    [Key]
    public int VehiclePairId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public int? PrimaryVehicleId { get; set; }
    public virtual Vehicle? PrimaryVehicle { get; set; }

    public int? SecondaryVehicleId { get; set; }
    public virtual Vehicle? SecondaryVehicle { get; set; }

    // سهم پیش‌فرض مالک کشنده و بونکر (درصد)
    [Column(TypeName = "decimal(8,4)")]
    public decimal PrimarySharePercent { get; set; } = 60m;

    [Column(TypeName = "decimal(8,4)")]
    public decimal SecondarySharePercent { get; set; } = 40m;
}
