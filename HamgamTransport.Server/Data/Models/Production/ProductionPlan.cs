using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data.Models.Product;
using ProductEntity = HamgamTransport.Server.Data.Models.Product.Product;

namespace HamgamTransport.Server.Data.Models.Production;

// برنامه تولید — برنامه‌ریزی مقدار تولید محصول
public class ProductionPlan : BaseEntity
{
    [Key]
    public int ProductionPlanID { get; set; }

    public DateTime PlanDate { get; set; } = DateTime.Now;

    public int ProductId { get; set; }

    public int MeaurmentId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal PlannedQuantity { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual ProductEntity Product { get; set; } = null!;

    [ForeignKey(nameof(MeaurmentId))]
    public virtual Meaurment Meaurment { get; set; } = null!;
}
