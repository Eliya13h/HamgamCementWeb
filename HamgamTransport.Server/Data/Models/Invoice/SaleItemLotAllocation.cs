using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Inventory;

namespace HamgamTransport.Server.Data.Models.Invoice;

// جزئیات تخصیص FIFO برای هر ردیف فروش — ردیابی از کدام Lot/خرید فروخته شده
public class SaleItemLotAllocation : BaseEntity
{
    [Key]
    public int SaleItemLotAllocationID { get; set; }

    public int SalesItemId { get; set; }
    public int InventoryLotId { get; set; }

    public int? PurchaseInvoiceId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal QuantityInBase { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCostInBase { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineCostInBase { get; set; }

    [ForeignKey(nameof(SalesItemId))]
    public virtual SalesItem SalesItem { get; set; } = null!;

    [ForeignKey(nameof(InventoryLotId))]
    public virtual InventoryLot InventoryLot { get; set; } = null!;
}
