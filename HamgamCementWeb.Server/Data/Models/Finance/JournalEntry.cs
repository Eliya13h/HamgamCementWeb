using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// سند دفترروزنامه — هسته ثبت دوطرفه
public class JournalEntry : BaseEntity
{
    [Key]
    public int JournalEntryID { get; set; }

    [MaxLength(40)]
    public string EntryNumber { get; set; } = string.Empty;

    public DateTime EntryDate { get; set; } = DateTime.Now;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public JournalSource Source { get; set; }

    // شناسه سند مبدأ (فاکتور، مصرف، انتقال صندوق و ...)
    public int? SourceId { get; set; }

    public int BaseCurrencyId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalDebitInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalCreditInBaseCurrency { get; set; }

    public bool IsPosted { get; set; } = true;

    public DateTime? PostedAt { get; set; }

    [ForeignKey(nameof(BaseCurrencyId))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    public virtual ICollection<JournalLine> Lines { get; set; } = [];
}
