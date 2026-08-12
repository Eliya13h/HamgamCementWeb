using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Inventory;
using HamgamTransport.Server.Data.Models.Product;
using ProductEntity = HamgamTransport.Server.Data.Models.Product.Product;

namespace HamgamTransport.Server.Data.Models.Production;

// ردیف مواد فرمول — مقدار به ازای مقدار پایه
public class ProductionFormulaMaterialLine : BaseEntity
{
    [Key]
    public int ProductionFormulaMaterialLineID { get; set; }

    public int ProductionFormulaId { get; set; }

    public int ProductId { get; set; }

    public int MeaurmentId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    // انبار پیش‌فرض مصرف (اختیاری — Raw یا Semi)
    public int? DefaultWarehouseId { get; set; }

    [ForeignKey(nameof(ProductionFormulaId))]
    public virtual ProductionFormula Formula { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual ProductEntity Product { get; set; } = null!;

    [ForeignKey(nameof(MeaurmentId))]
    public virtual Meaurment Meaurment { get; set; } = null!;

    [ForeignKey(nameof(DefaultWarehouseId))]
    public virtual Warehouse? DefaultWarehouse { get; set; }
}
