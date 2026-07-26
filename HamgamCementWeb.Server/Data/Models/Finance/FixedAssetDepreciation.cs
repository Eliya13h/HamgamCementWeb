using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// ردیف استهلاک دوره‌ای دارایی ثابت (ماه شمسی)
public class FixedAssetDepreciation : BaseEntity
{
    [Key]
    public int FixedAssetDepreciationID { get; set; }

    public int FixedAssetId { get; set; }

    // سال و ماه شمسی دوره استهلاک
    public int PeriodSolarYear { get; set; }
    public int PeriodMonth { get; set; }

    public DateTime DepreciationDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(FixedAssetId))]
    public virtual FixedAsset FixedAsset { get; set; } = null!;

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }
}
