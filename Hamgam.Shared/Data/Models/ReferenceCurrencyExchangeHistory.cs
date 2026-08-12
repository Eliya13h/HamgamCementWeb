using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hamgam.Shared.Data.Models;

public class ReferenceCurrencyExchangeHistory : ReferenceBaseEntity
{
    [Key]
    public int HistoryID { get; set; }

    [ForeignKey(nameof(Currency))]
    public int CurrencyID { get; set; }
    public virtual ReferenceCurrency? Currency { get; set; }

    [ForeignKey(nameof(BaseCurrency))]
    public int BaseCurrencyID { get; set; }
    public virtual ReferenceCurrency? BaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal BaseUnitsPerUnit { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal? PreviousBaseUnitsPerUnit { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    [MaxLength(500)]
    public string? ChangeReason { get; set; }
}
