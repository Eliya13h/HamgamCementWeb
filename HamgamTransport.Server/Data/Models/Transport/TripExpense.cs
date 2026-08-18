using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Finance;

namespace HamgamTransport.Server.Data.Models.Transport;

public class TripExpense : BaseEntity
{
    [Key]
    public int TripExpenseId { get; set; }

    public int TransportTripId { get; set; }
    public virtual TransportTrip? TransportTrip { get; set; }

    public int TripExpenseCategoryId { get; set; }
    public virtual TripExpenseCategory? Category { get; set; }

    public string Title { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    public int CurrencyId { get; set; }
    public virtual Currency? Currency { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal ExchangeRate { get; set; } = 1m;

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    // وسیله‌ای که هزینه به آن تخصیص می‌یابد
    public int? VehicleId { get; set; }
    public virtual Vehicle? Vehicle { get; set; }

    public int? CashBoxId { get; set; }
    public int? BankAccountId { get; set; }

    // طرف حساب هزینه وقتی پرداخت از صندوق/بانک نیست (مالک، راننده، مشتری، تأمین‌کننده)
    public PartySettlementPartyType? PartyType { get; set; }
    public int? PartyId { get; set; }

    public int? JournalEntryId { get; set; }
    public bool IsPosted { get; set; }
}
