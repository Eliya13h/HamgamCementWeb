using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Finance;

// خط انتقال چندارزی پایان شیفت — هر ارز جدا
public class CashTransferLine : BaseEntity
{
    [Key]
    public int CashTransferLineID { get; set; }

    public int CashTransferId { get; set; }

    public int CurrencyId { get; set; }

    // مبلغ به ارز سند
    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    // معادل ارز پایه برای توازن دفتر
    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    [ForeignKey(nameof(CashTransferId))]
    public virtual CashTransfer CashTransfer { get; set; } = null!;

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;
}
