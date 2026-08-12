using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Finance;

namespace HamgamTransport.Server.Data.Models.People;

public class Driver : BaseEntity
{
    [Key]
    public int DriverId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;

    // مالک مرتبط (اختیاری)
    public int? VehicleOwnerId { get; set; }
    public virtual VehicleOwner? VehicleOwner { get; set; }

    // درصد پیش‌فرض سهم سود راننده
    [Column(TypeName = "decimal(8,4)")]
    public decimal? DefaultProfitSharePercent { get; set; }

    public int? AccountId { get; set; }
    public virtual Account? Account { get; set; }
}
