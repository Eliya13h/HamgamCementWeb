using System.ComponentModel.DataAnnotations;

namespace HamgamTransport.Server.Data.Models.Transport;

public class VehicleType : BaseEntity
{
    [Key]
    public int VehicleTypeId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    // نقش پیش‌فرض در جفت (کشنده/بونکر/تک)
    public VehicleRole DefaultRole { get; set; } = VehicleRole.Primary;
}
