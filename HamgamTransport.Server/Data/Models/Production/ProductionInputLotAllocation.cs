using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Inventory;

namespace HamgamTransport.Server.Data.Models.Production;

// جزئیات تخصیص FIFO برای هر ردیف مصرف تولید — ردیابی اینکه از کدام Lot و چه مقدار مصرف شد؛
// برای نمایش کامل Trace و امکان برگشت دقیق (Unpost) مواد به همان Lotها لازم است.
public class ProductionInputLotAllocation : BaseEntity
{
    [Key]
    public int ProductionInputLotAllocationID { get; set; }

    public int ProductionInputLineId { get; set; }

    public int InventoryLotId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal QuantityInBase { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCostInBase { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineCostInBase { get; set; }

    [ForeignKey(nameof(ProductionInputLineId))]
    public virtual ProductionInputLine InputLine { get; set; } = null!;

    [ForeignKey(nameof(InventoryLotId))]
    public virtual InventoryLot InventoryLot { get; set; } = null!;
}
