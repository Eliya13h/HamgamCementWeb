using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Finance;

// قالب سند تکراری ماهانه — صدور دستی با دکمه
public class RecurringJournalTemplate : BaseEntity
{
    [Key]
    public int RecurringJournalTemplateID { get; set; }

    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public virtual ICollection<RecurringJournalTemplateLine> Lines { get; set; } = [];
}

public class RecurringJournalTemplateLine : BaseEntity
{
    [Key]
    public int RecurringJournalTemplateLineID { get; set; }

    public int RecurringJournalTemplateId { get; set; }

    public int LineNo { get; set; }

    public int AccountId { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal DebitInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CreditInBaseCurrency { get; set; }

    // مرکز هزینه اختیاری
    public int? CostCenterId { get; set; }

    [ForeignKey(nameof(RecurringJournalTemplateId))]
    public virtual RecurringJournalTemplate Template { get; set; } = null!;

    [ForeignKey(nameof(AccountId))]
    public virtual Account Account { get; set; } = null!;

    [ForeignKey(nameof(CostCenterId))]
    public virtual CostCenter? CostCenter { get; set; }
}
