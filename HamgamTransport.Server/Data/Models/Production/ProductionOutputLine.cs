using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Product;
using ProductEntity = HamgamTransport.Server.Data.Models.Product.Product;

namespace HamgamTransport.Server.Data.Models.Production;

// ردیف محصول تولیدی
public class ProductionOutputLine : BaseEntity
{
    [Key]
    public int ProductionOutputLineID { get; set; }

    public int ProductionBatchId { get; set; }

    public int ProductId { get; set; }

    public int MeaurmentId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal QuantityInBase { get; set; }

    // بهای واحد پایه — پس از ثبت نهایی محاسبه می‌شود
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitCostInBase { get; set; }

    public int? InventoryLotId { get; set; }

    [ForeignKey(nameof(ProductionBatchId))]
    public virtual ProductionBatch Batch { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual ProductEntity Product { get; set; } = null!;

    [ForeignKey(nameof(MeaurmentId))]
    public virtual Meaurment Meaurment { get; set; } = null!;
}
