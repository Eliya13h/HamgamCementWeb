using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.Product;
using ProductEntity = HamgamCementWeb.Server.Data.Models.Product.Product;

namespace HamgamCementWeb.Server.Data.Models.Production;

// ردیف مصرف مواد در تولید — فقط از انبار مواد خام یا نیمه‌خام
public class ProductionInputLine : BaseEntity
{
    [Key]
    public int ProductionInputLineID { get; set; }

    public int ProductionBatchId { get; set; }

    public int WarehouseId { get; set; }

    public int ProductId { get; set; }

    public int MeaurmentId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal QuantityInBase { get; set; }

    // بهای مصرف FIFO (ارز پایه)
    [Column(TypeName = "decimal(18,4)")]
    public decimal MaterialCostInBase { get; set; }

    [ForeignKey(nameof(ProductionBatchId))]
    public virtual ProductionBatch Batch { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    public virtual Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual ProductEntity Product { get; set; } = null!;

    [ForeignKey(nameof(MeaurmentId))]
    public virtual Meaurment Meaurment { get; set; } = null!;
}
