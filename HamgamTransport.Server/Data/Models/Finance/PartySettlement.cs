using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Finance;

// دریافت از مشتری / پرداخت به تأمین‌کننده / مالک / راننده
public class PartySettlement : BaseEntity
{
    [Key]
    public int PartySettlementID { get; set; }

    public PartySettlementPartyType PartyType { get; set; }

    public int PartyId { get; set; }

    public DateTime SettlementDate { get; set; } = DateTime.Now;

    public int CurrencyId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    public int? CashBoxId { get; set; }

    public int? BankAccountId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(CashBoxId))]
    public virtual CashBox? CashBox { get; set; }

    [ForeignKey(nameof(BankAccountId))]
    public virtual BankAccount? BankAccount { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }
}
