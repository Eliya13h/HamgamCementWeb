using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Finance;

// کارت دارایی ثابت — ثبت خرید و نگهداری ارزش دفتری
public class FixedAsset : BaseEntity
{
    [Key]
    public int FixedAssetID { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Name { get; set; } = string.Empty;

    public int FixedAssetCategoryId { get; set; }

    public DateTime AcquisitionDate { get; set; } = DateTime.Now;

    public int? SupplierId { get; set; }

    public int CurrencyId { get; set; }
    public int BaseCurrencyId { get; set; }
    public int? ExchangeHistoryId { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal BaseUnitsPerUnitAtTransaction { get; set; } = 1;

    // بهای تمام‌شده خرید
    [Column(TypeName = "decimal(18,4)")]
    public decimal CostAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal CostAmountInBaseCurrency { get; set; }

    // ارزش اسقاط
    [Column(TypeName = "decimal(18,4)")]
    public decimal SalvageValue { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SalvageValueInBaseCurrency { get; set; }

    // عمر مفید به ماه
    public int UsefulLifeMonths { get; set; }

    public DepreciationMethod DepreciationMethod { get; set; } = DepreciationMethod.StraightLine;

    // استهلاک انباشته به ارز پایه
    [Column(TypeName = "decimal(18,4)")]
    public decimal AccumulatedDepreciationInBaseCurrency { get; set; }

    public FixedAssetStatus Status { get; set; } = FixedAssetStatus.Active;

    [MaxLength(2000)]
    public string? Description { get; set; }

    // سند خرید دارایی
    public int? AcquisitionJournalEntryId { get; set; }

    public DateTime? DisposalDate { get; set; }

    // مبلغ فروش/اسقاط (ارز معامله)
    [Column(TypeName = "decimal(18,4)")]
    public decimal? DisposalAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal? DisposalAmountInBaseCurrency { get; set; }

    public int? DisposalJournalEntryId { get; set; }

    [ForeignKey(nameof(FixedAssetCategoryId))]
    public virtual FixedAssetCategory Category { get; set; } = null!;

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier? Supplier { get; set; }

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(BaseCurrencyId))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    [ForeignKey(nameof(ExchangeHistoryId))]
    public virtual CurrencyExchangeHistory? ExchangeHistory { get; set; }

    [ForeignKey(nameof(AcquisitionJournalEntryId))]
    public virtual JournalEntry? AcquisitionJournalEntry { get; set; }

    [ForeignKey(nameof(DisposalJournalEntryId))]
    public virtual JournalEntry? DisposalJournalEntry { get; set; }

    public virtual ICollection<FixedAssetDepreciation> Depreciations { get; set; } = [];

    [NotMapped]
    public decimal BookValueInBaseCurrency =>
        CostAmountInBaseCurrency - AccumulatedDepreciationInBaseCurrency;
}
