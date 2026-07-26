using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// موجودی اعلامی ابتدای شیفت به تفکیک ارز
public class CashShiftOpeningLine : BaseEntity
{
    [Key]
    public int CashShiftOpeningLineID { get; set; }

    public int CashShiftId { get; set; }

    public int CurrencyId { get; set; }

    // مبلغ اعلامی به همان ارز
    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [ForeignKey(nameof(CashShiftId))]
    public virtual CashShift CashShift { get; set; } = null!;

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;
}
