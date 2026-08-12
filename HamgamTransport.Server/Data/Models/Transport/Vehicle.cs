using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Transport;

public class Vehicle : BaseEntity
{
    [Key]
    public int VehicleId { get; set; }

    public string PlateNumber { get; set; } = string.Empty;
    public int VehicleTypeId { get; set; }
    public virtual VehicleType? VehicleType { get; set; }

    public int VehicleOwnerId { get; set; }
    public virtual VehicleOwner? VehicleOwner { get; set; }

    // مرکز هزینه برای گزارش سود/زیان هر وسیله
    public int? CostCenterId { get; set; }
    public virtual CostCenter? CostCenter { get; set; }

    public int? VehiclePairId { get; set; }
    public virtual VehiclePair? VehiclePair { get; set; }

    public VehicleRole RoleInPair { get; set; } = VehicleRole.Primary;
}
