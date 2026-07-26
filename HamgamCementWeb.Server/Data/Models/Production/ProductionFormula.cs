using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Product;
using ProductEntity = HamgamCementWeb.Server.Data.Models.Product.Product;

namespace HamgamCementWeb.Server.Data.Models.Production;

// فرمول ساخت محصول — مواد و هزینه‌ها برای یک مقدار پایه
public class ProductionFormula : BaseEntity
{
    [Key]
    public int ProductionFormulaID { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // محصولی که با این فرمول ساخته می‌شود
    public int ProductId { get; set; }

    public int MeaurmentId { get; set; }

    // مقدار پایه فرمول (مثلاً ۱ تن) — مقیاس تولید نسبت به این مقدار است
    [Column(TypeName = "decimal(18,6)")]
    public decimal BaseQuantity { get; set; } = 1;

    public ProductionFormulaMode Mode { get; set; } = ProductionFormulaMode.Fixed;

    // فقط یک فرمول فعال برای هر محصول می‌تواند پیش‌فرض باشد
    public bool IsDefault { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(ProductId))]
    public virtual ProductEntity Product { get; set; } = null!;

    [ForeignKey(nameof(MeaurmentId))]
    public virtual Meaurment Meaurment { get; set; } = null!;

    public virtual ICollection<ProductionFormulaMaterialLine> MaterialLines { get; set; } = [];

    public virtual ICollection<ProductionFormulaCostLine> CostLines { get; set; } = [];
}
