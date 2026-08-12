using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Finance;

namespace HamgamTransport.Server.Data.Models.People;

public class VehicleOwner : BaseEntity
{
    [Key]
    public int VehicleOwnerId { get; set; }

    public PersonTitle Title { get; set; } = PersonTitle.Mr;
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public PersonType OwnerType { get; set; } = PersonType.NaturalPerson;

    [Column(TypeName = "decimal(18,4)")]
    public decimal InitialBalance { get; set; }

    // حساب پرداختنی تفصیلی مالک
    public int? AccountId { get; set; }
    public virtual Account? Account { get; set; }
}
