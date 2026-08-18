using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Finance;

public class Expense : BaseEntity
{
    [Key]
    public int ExpenseID { get; set; }

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public DateTime ExpenseDate { get; set; } = DateTime.Now;

    public int? SupplierId { get; set; }

    public int CurrencyId { get; set; }
    public int BaseCurrencyId { get; set; }

    public int? ExchangeHistoryId { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal BaseUnitsPerUnitAtTransaction { get; set; } = 1;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int ExpenseCategoryId { get; set; }

    public FinancialEntrySource Source { get; set; } = FinancialEntrySource.Miscellaneous;

    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey(nameof(ExpenseCategoryId))]
    public virtual ExpenseCategory Category { get; set; } = null!;

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(BaseCurrencyId))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    [ForeignKey(nameof(ExchangeHistoryId))]
    public virtual CurrencyExchangeHistory? ExchangeHistory { get; set; }
}
