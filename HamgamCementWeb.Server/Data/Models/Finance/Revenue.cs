using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Invoice;
using HamgamCementWeb.Server.Data.Models.People;

namespace HamgamCementWeb.Server.Data.Models.Finance;

public class Revenue : BaseEntity
{
    [Key]
    public int RevenueID { get; set; }

    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    public DateTime RevenueDate { get; set; } = DateTime.Now;

    public int? CustomerId { get; set; }

    public int CurrencyId { get; set; }
    public int BaseCurrencyId { get; set; }

    public int? ExchangeHistoryId { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal BaseUnitsPerUnitAtTransaction { get; set; } = 1;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    // سود FIFO این فاکتور (ارز پایه)
    [Column(TypeName = "decimal(18,4)")]
    public decimal ProfitInBaseCurrency { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    // دسته‌بندی حسابداری عاید
    public int RevenueCategoryId { get; set; }

    // مرکز هزینه اختیاری برای گزارش تحلیلی
    public int? CostCenterId { get; set; }

    // منبع ثبت: فاکتور فروش، متفرقه و ...
    public FinancialEntrySource Source { get; set; } = FinancialEntrySource.Miscellaneous;

    // سند دفترروزنامه متناظر با این عاید
    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer? Customer { get; set; }

    [ForeignKey(nameof(RevenueCategoryId))]
    public virtual RevenueCategory Category { get; set; } = null!;

    [ForeignKey(nameof(CostCenterId))]
    public virtual CostCenter? CostCenter { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }

    // ناوبری معکوس — FK فقط روی SaleInvoice.RevenueId است
    public virtual SaleInvoice? SaleInvoice { get; set; }

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(BaseCurrencyId))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    [ForeignKey(nameof(ExchangeHistoryId))]
    public virtual CurrencyExchangeHistory? ExchangeHistory { get; set; }
}
