using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Inventory;

namespace HamgamCementWeb.Server.Data.Models.Production;

// سند گزارش روزانه تولید — مصرف مواد خام/نیمه‌خام و تولید محصول پردازش‌شده
public class ProductionBatch : BaseEntity
{
    [Key]
    public int ProductionBatchID { get; set; }

    [MaxLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    public DateTime ProductionDate { get; set; } = DateTime.Now;

    // انبار مقصد محصول تولیدی (مواد پردازش‌شده)
    public int OutputWarehouseId { get; set; }

    public ProductionBatchStatus Status { get; set; } = ProductionBatchStatus.Draft;

    public bool IsPosted { get; set; }

    public DateTime? PostedAt { get; set; }

    // هزینه ثابت و متغیر تولید — در بهای تمام‌شده محصول لحاظ می‌شود
    [Column(TypeName = "decimal(18,4)")]
    public decimal FixedCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal VariableCost { get; set; }

    // جمع بهای مواد مصرفی (ارز پایه) — پس از server-side محاسبه می‌شود
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalMaterialCostInBase { get; set; }

    // آیا خروجی این سند به چرخه فروش (فاکتور خرید) منتقل شده است
    public bool IsTransferredToSales { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [ForeignKey(nameof(OutputWarehouseId))]
    public virtual Warehouse OutputWarehouse { get; set; } = null!;

    public virtual ICollection<ProductionInputLine> InputLines { get; set; } = [];

    public virtual ICollection<ProductionOutputLine> OutputLines { get; set; } = [];
}
