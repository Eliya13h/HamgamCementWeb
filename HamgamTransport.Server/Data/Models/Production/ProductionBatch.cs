using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.Inventory;

namespace HamgamTransport.Server.Data.Models.Production;

// سند تولید روزانه — بر اساس فرمول ساخت؛ مصرف مواد و ورود محصول ساخته‌شده
public class ProductionBatch : BaseEntity
{
    [Key]
    public int ProductionBatchID { get; set; }

    [MaxLength(50)]
    public string BatchNumber { get; set; } = string.Empty;

    public DateTime ProductionDate { get; set; } = DateTime.Now;

    // فرمول مبنا (برای اسناد جدید اجباری است)
    public int? ProductionFormulaId { get; set; }

    // لینک اختیاری به برنامه تولید — برای اتصال برنامه‌ریزی به سند واقعی
    public int? ProductionPlanId { get; set; }

    // انبار مقصد محصول تولیدی (مواد پردازش‌شده)
    public int OutputWarehouseId { get; set; }

    public ProductionBatchStatus Status { get; set; } = ProductionBatchStatus.Draft;

    public bool IsPosted { get; set; }

    public DateTime? PostedAt { get; set; }

    // مشتق از CostLines (نوع Fixed) — فقط برای سازگاری گزارش‌های قدیمی؛ منبع حقیقت CostLines است
    [Column(TypeName = "decimal(18,4)")]
    public decimal FixedCost { get; set; }

    // مشتق از CostLines (غیر Fixed) — فقط برای سازگاری گزارش‌های قدیمی؛ منبع حقیقت CostLines است
    [Column(TypeName = "decimal(18,4)")]
    public decimal VariableCost { get; set; }

    // جمع بهای مواد مصرفی (ارز پایه) — پس از ثبت نهایی
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalMaterialCostInBase { get; set; }

    // جمع هزینه‌های غیرمواد پس از ثبت
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalConversionCostInBase { get; set; }

    // بهای تمام‌شده کل = مواد + تبدیل
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalCostInBase { get; set; }

    // منسوخ: قبلاً برای پل فاکتور خرید «ورود از تولید» بود — دیگر استفاده عملیاتی ندارد
    public bool IsTransferredToSales { get; set; }

    public int? JournalEntryId { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    [ForeignKey(nameof(ProductionFormulaId))]
    public virtual ProductionFormula? Formula { get; set; }

    [ForeignKey(nameof(ProductionPlanId))]
    public virtual ProductionPlan? Plan { get; set; }

    [ForeignKey(nameof(OutputWarehouseId))]
    public virtual Warehouse OutputWarehouse { get; set; } = null!;

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }

    public virtual ICollection<ProductionInputLine> InputLines { get; set; } = [];

    public virtual ICollection<ProductionOutputLine> OutputLines { get; set; } = [];

    public virtual ICollection<ProductionBatchCostLine> CostLines { get; set; } = [];
}
