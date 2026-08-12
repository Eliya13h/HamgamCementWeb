using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Finance;

// سند عملیاتی حقوق صاحبان سهام — هر رکورد یک JournalEntry متوازن دارد
public class ShareholderEquityTxn : BaseEntity
{
    [Key]
    public int ShareholderEquityTxnID { get; set; }

    public ShareholderEquityTxnType TxnType { get; set; }

    public int ShareholderId { get; set; }

    public DateTime TxnDate { get; set; } = DateTime.Now;

    public int CurrencyId { get; set; }

    public int BaseCurrencyId { get; set; }

    public int? ExchangeHistoryId { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal BaseUnitsPerUnitAtTransaction { get; set; } = 1;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    // بخش از مبلغ کل که از سود انباشته/سهم سود کسر شده (ارز پایه) — برای تفکیک خودکار توزیع سود
    [Column(TypeName = "decimal(18,4)")]
    public decimal ProfitPortionInBase { get; set; }

    // بخش مازاد که از سرمایه همان سهام‌دار کسر شده (ارز پایه)
    [Column(TypeName = "decimal(18,4)")]
    public decimal CapitalPortionInBase { get; set; }

    // برای آورده/برداشت/توزیع نقدی الزامی است
    public int? CashBoxId { get; set; }

    // فقط برای توزیع سود معنا دارد؛ پیش‌فرض نقدی
    public EquitySettlementMode SettlementMode { get; set; } = EquitySettlementMode.Cash;

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(ShareholderId))]
    public virtual Shareholder Shareholder { get; set; } = null!;

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(BaseCurrencyId))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    [ForeignKey(nameof(ExchangeHistoryId))]
    public virtual CurrencyExchangeHistory? ExchangeHistory { get; set; }

    [ForeignKey(nameof(CashBoxId))]
    public virtual CashBox? CashBox { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }
}
