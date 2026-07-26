using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// خط سند دفترروزنامه — بدهکار یا بستانکار
public class JournalLine : BaseEntity
{
    [Key]
    public int JournalLineID { get; set; }

    public int JournalEntryId { get; set; }

    public int AccountId { get; set; }

    public int LineNo { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int CurrencyId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Credit { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DebitInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CreditInBaseCurrency { get; set; }

    // ارجاع اختیاری به صندوق برای گزارش گردش نقد
    public int? CashBoxId { get; set; }

    // مشتری یا تأمین‌کننده مرتبط با خط تفصیلی
    public int? PartyId { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry JournalEntry { get; set; } = null!;

    [ForeignKey(nameof(AccountId))]
    public virtual Account Account { get; set; } = null!;

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(CashBoxId))]
    public virtual CashBox? CashBox { get; set; }
}
