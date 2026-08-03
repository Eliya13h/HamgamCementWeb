using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Finance;

namespace HamgamCementWeb.Server.Data.Models.Production;

// ردیف هزینه فرمول — دستمزد / سربار / جانبی / ثابت
public class ProductionFormulaCostLine : BaseEntity
{
    [Key]
    public int ProductionFormulaCostLineID { get; set; }

    public int ProductionFormulaId { get; set; }

    public ProductionCostType CostType { get; set; }

    // دسته هزینه تولید (داینامیک)؛ برای ردیف‌های قدیمی می‌تواند خالی باشد
    public int? ProductionCostCategoryId { get; set; }

    [MaxLength(200)]
    public string? Description { get; set; }

    public ProductionCostAmountMode AmountMode { get; set; } = ProductionCostAmountMode.PerBase;

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    // حساب اختیاری؛ در صورت خالی بودن از حساب سیستمی نوع هزینه استفاده می‌شود
    public int? AccountId { get; set; }

    [ForeignKey(nameof(ProductionFormulaId))]
    public virtual ProductionFormula Formula { get; set; } = null!;

    [ForeignKey(nameof(AccountId))]
    public virtual Account? Account { get; set; }

    [ForeignKey(nameof(ProductionCostCategoryId))]
    public virtual ProductionCostCategory? CostCategory { get; set; }
}
