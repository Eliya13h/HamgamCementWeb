using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// انتقال مانده صندوق به صندوق بالاتر در پایان شیفت
public class CashTransfer : BaseEntity
{
    [Key]
    public int CashTransferID { get; set; }

    public int FromCashBoxId { get; set; }

    public int ToCashBoxId { get; set; }

    // برای گزارش؛ رابطه اصلی از CashShift.CashTransferId است
    public int? CashShiftId { get; set; }

    public DateTime TransferDate { get; set; } = DateTime.Now;

    // جمع معادل پایه خطوط انتقال چندارزی
    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(FromCashBoxId))]
    public virtual CashBox FromCashBox { get; set; } = null!;

    [ForeignKey(nameof(ToCashBoxId))]
    public virtual CashBox ToCashBox { get; set; } = null!;

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }

    public virtual ICollection<CashTransferLine> Lines { get; set; } = [];
}
