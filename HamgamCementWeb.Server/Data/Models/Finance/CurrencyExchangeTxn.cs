using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// خرید/فروش (تبدیل) ارز داخلی — جابه‌جایی بین صندوق/بانک با سند دابل‌انتری
public class CurrencyExchangeTxn : BaseEntity
{
    [Key]
    public int CurrencyExchangeTxnID { get; set; }

    public DateTime ExchangeDate { get; set; } = DateTime.Now;

    // ارز و مبلغ خروجی
    public int FromCurrencyId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal FromAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal FromAmountInBaseCurrency { get; set; }

    // ارز و مبلغ ورودی
    public int ToCurrencyId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal ToAmountInBaseCurrency { get; set; }

    // نرخ معامله: چند واحد ارز مقصد به‌ازای ۱ واحد ارز مبدأ
    [Column(TypeName = "decimal(18,8)")]
    public decimal DealRate { get; set; }

    // true = شناسایی سود/زیان نسبت به نرخ سیستم؛ false = هر دو طرف با ارزش معامله
    public bool RecognizeFxDifference { get; set; }

    // اسنپ‌شات نرخ سیستم (BaseUnitsPerUnit) در تاریخ سند
    [Column(TypeName = "decimal(18,8)")]
    public decimal SystemFromBaseUnitsPerUnit { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal SystemToBaseUnitsPerUnit { get; set; }

    // مثبت = سود تسعیر، منفی = زیان، صفر = بدون خط تسعیر
    [Column(TypeName = "decimal(18,4)")]
    public decimal FxDifferenceInBaseCurrency { get; set; }

    // مبدأ: دقیقاً یکی از صندوق یا بانک
    public int? FromCashBoxId { get; set; }

    public int? FromBankAccountId { get; set; }

    // مقصد: دقیقاً یکی از صندوق یا بانک
    public int? ToCashBoxId { get; set; }

    public int? ToBankAccountId { get; set; }

    public int? ExchangeHistoryFromId { get; set; }

    public int? ExchangeHistoryToId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // سند دفترروزنامه پس از ثبت
    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(FromCurrencyId))]
    public virtual Currency FromCurrency { get; set; } = null!;

    [ForeignKey(nameof(ToCurrencyId))]
    public virtual Currency ToCurrency { get; set; } = null!;

    [ForeignKey(nameof(FromCashBoxId))]
    public virtual CashBox? FromCashBox { get; set; }

    [ForeignKey(nameof(FromBankAccountId))]
    public virtual BankAccount? FromBankAccount { get; set; }

    [ForeignKey(nameof(ToCashBoxId))]
    public virtual CashBox? ToCashBox { get; set; }

    [ForeignKey(nameof(ToBankAccountId))]
    public virtual BankAccount? ToBankAccount { get; set; }

    [ForeignKey(nameof(ExchangeHistoryFromId))]
    public virtual CurrencyExchangeHistory? ExchangeHistoryFrom { get; set; }

    [ForeignKey(nameof(ExchangeHistoryToId))]
    public virtual CurrencyExchangeHistory? ExchangeHistoryTo { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }
}
