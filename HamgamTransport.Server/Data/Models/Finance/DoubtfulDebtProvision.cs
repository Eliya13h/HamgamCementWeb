using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Finance;

// ذخیره مطالبات مشکوک الوصول — مبلغ دستی
public class DoubtfulDebtProvision : BaseEntity
{
    [Key]
    public int DoubtfulDebtProvisionID { get; set; }

    public DateTime ProvisionDate { get; set; } = DateTime.Now;

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }
}
