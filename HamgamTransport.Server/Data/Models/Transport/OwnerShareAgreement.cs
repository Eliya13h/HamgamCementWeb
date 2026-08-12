using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Transport;

public class OwnerShareAgreement : BaseEntity
{
    [Key]
    public int OwnerShareAgreementId { get; set; }

    public int VehiclePairId { get; set; }
    public virtual VehiclePair? VehiclePair { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal PrimarySharePercent { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal SecondarySharePercent { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
}
