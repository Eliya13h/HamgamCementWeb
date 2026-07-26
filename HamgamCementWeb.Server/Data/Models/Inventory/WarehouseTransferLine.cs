using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Product;
using ProductEntity = HamgamCementWeb.Server.Data.Models.Product.Product;

namespace HamgamCementWeb.Server.Data.Models.Inventory;

// ردیف انتقال کالا بین انبارها
public class WarehouseTransferLine : BaseEntity
{
    [Key]
    public int WarehouseTransferLineID { get; set; }

    public int WarehouseTransferId { get; set; }

    public int ProductId { get; set; }

    public int MeaurmentId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    // معادل واحد پایه (کیلوگرم)
    [Column(TypeName = "decimal(18,6)")]
    public decimal QuantityInBase { get; set; }

    // بهای واحد پایه پس از تخصیص FIFO (ارز پایه)
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCostInBase { get; set; }

    // بهای کل ردیف = QuantityInBase × UnitCostInBase
    [Column(TypeName = "decimal(18,4)")]
    public decimal LineCostInBase { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(WarehouseTransferId))]
    public virtual WarehouseTransfer WarehouseTransfer { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual ProductEntity Product { get; set; } = null!;

    [ForeignKey(nameof(MeaurmentId))]
    public virtual Meaurment Meaurment { get; set; } = null!;
}
