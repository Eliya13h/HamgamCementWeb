using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Transport;

public class TransportTrip : BaseEntity
{
    [Key]
    public int TransportTripId { get; set; }

    public string TripNumber { get; set; } = string.Empty;
    public DateTime TripDate { get; set; }
    public TripStatus Status { get; set; } = TripStatus.Planned;

    public int CustomerId { get; set; }
    public virtual Customer? Customer { get; set; }

    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,4)")]
    public decimal WeightTon { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal RatePerTon { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    public int CurrencyId { get; set; }
    public virtual Currency? Currency { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal ExchangeRate { get; set; } = 1m;

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    public int? VehiclePairId { get; set; }
    public virtual VehiclePair? VehiclePair { get; set; }

    public int? PrimaryVehicleId { get; set; }
    public virtual Vehicle? PrimaryVehicle { get; set; }

    public int? SecondaryVehicleId { get; set; }
    public virtual Vehicle? SecondaryVehicle { get; set; }

    public int? DriverId { get; set; }
    public virtual Driver? Driver { get; set; }

    // بازنویسی سهم مالکان در سطح سفر
    [Column(TypeName = "decimal(8,4)")]
    public decimal? PrimaryOwnerSharePercent { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal? SecondaryOwnerSharePercent { get; set; }

    public DriverCompensationType DriverCompensationType { get; set; } = DriverCompensationType.FixedAmount;

    [Column(TypeName = "decimal(18,4)")]
    public decimal? DriverFixedAmount { get; set; }

    [Column(TypeName = "decimal(8,4)")]
    public decimal? DriverProfitSharePercent { get; set; }

    public string? Notes { get; set; }

    public int? RevenueJournalEntryId { get; set; }
    public bool IsRevenuePosted { get; set; }

    public virtual ICollection<TripExpense> Expenses { get; set; } = new List<TripExpense>();
}
