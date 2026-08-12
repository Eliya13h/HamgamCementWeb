using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Finance;

namespace HamgamTransport.Server.Data.Models.Production;

// اسنپ‌شات هزینه روی سند تولید (مستقل از تغییرات بعدی فرمول)
public class ProductionBatchCostLine : BaseEntity
{
    [Key]
    public int ProductionBatchCostLineID { get; set; }

    public int ProductionBatchId { get; set; }

    public ProductionCostType CostType { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    public int? AccountId { get; set; }

    [ForeignKey(nameof(ProductionBatchId))]
    public virtual ProductionBatch Batch { get; set; } = null!;

    [ForeignKey(nameof(AccountId))]
    public virtual Account? Account { get; set; }
}
