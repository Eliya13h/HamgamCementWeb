using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Invoice;
using HamgamCementWeb.Server.Data.Models.People;

namespace HamgamCementWeb.Server.Data.Models.Finance;

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

    // دسته‌بندی حسابداری مصرف
    public int ExpenseCategoryId { get; set; }

    // مرکز هزینه اختیاری برای گزارش تحلیلی
    public int? CostCenterId { get; set; }

    // منبع ثبت: فاکتور خرید، متفرقه و ...
    public FinancialEntrySource Source { get; set; } = FinancialEntrySource.Miscellaneous;

    // سند دفترروزنامه متناظر با این مصرف
    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey(nameof(ExpenseCategoryId))]
    public virtual ExpenseCategory Category { get; set; } = null!;

    [ForeignKey(nameof(CostCenterId))]
    public virtual CostCenter? CostCenter { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }

    // ناوبری معکوس — FK فقط روی PurchaseInvoice.ExpenseId است
    public virtual PurchaseInvoice? PurchaseInvoice { get; set; }

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(BaseCurrencyId))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    [ForeignKey(nameof(ExchangeHistoryId))]
    public virtual CurrencyExchangeHistory? ExchangeHistory { get; set; }
}
