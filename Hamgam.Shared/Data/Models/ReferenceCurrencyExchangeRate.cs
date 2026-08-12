using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Hamgam.Shared.Data.Models;

public class ReferenceCurrencyExchangeRate : ReferenceBaseEntity
{
    [Key]
    public int CurrencyExchangeRateID { get; set; }

    [ForeignKey(nameof(Currency))]
    public int CurrencyID { get; set; }
    public virtual ReferenceCurrency? Currency { get; set; }

    [ForeignKey(nameof(BaseCurrency))]
    public int BaseCurrencyID { get; set; }
    public virtual ReferenceCurrency? BaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal BaseUnitsPerUnit { get; set; }

    public DateTime EffectiveFrom { get; set; }

    [ForeignKey(nameof(SourceHistory))]
    public int? SourceHistoryID { get; set; }
    public virtual ReferenceCurrencyExchangeHistory? SourceHistory { get; set; }
}
