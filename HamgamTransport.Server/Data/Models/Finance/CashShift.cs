using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Finance;

// شیفت کاری صندوق — باز تا بستن و تحویل به صندوق بالاتر
public class CashShift : BaseEntity
{
    [Key]
    public int CashShiftID { get; set; }

    public int CashBoxId { get; set; }

    public int UserId { get; set; }

    public CashShiftStatus Status { get; set; } = CashShiftStatus.Open;

    public DateTime OpenedAt { get; set; } = DateTime.Now;

    public DateTime? ClosedAt { get; set; }

    // جمع معادل پایه موجودی‌های اعلامی ابتدای شیفت (از OpeningLines)
    [Column(TypeName = "decimal(18,4)")]
    public decimal OpeningBalanceInBase { get; set; }

    // جمع معادل پایه مبالغ تحویلی هنگام بستن (از خطوط انتقال)
    [Column(TypeName = "decimal(18,4)")]
    public decimal ClosingTransferAmountInBase { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public int? CashTransferId { get; set; }

    [ForeignKey(nameof(CashBoxId))]
    public virtual CashBox CashBox { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;

    // ناوبری بدون FK معکوس روی CashTransfer — جلوگیری از رابطه دوطرفه مبهم
    public virtual CashTransfer? CashTransfer { get; set; }

    public virtual ICollection<CashShiftOpeningLine> OpeningLines { get; set; } = [];
}
