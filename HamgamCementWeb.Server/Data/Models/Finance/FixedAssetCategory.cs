using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// دسته‌بندی دارایی ثابت — نگاشت به حساب معین اموال / استهلاک
public class FixedAssetCategory : BaseEntity
{
    [Key]
    public int FixedAssetCategoryID { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // کد ثابت برای دسته‌های سیستمی (مثلاً MACHINERY)
    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    public bool IsSystem { get; set; }

    // حساب معین دارایی (بدهکار هنگام خرید)
    public int? AssetAccountId { get; set; }

    // حساب استهلاک انباشته (بستانکار هنگام استهلاک)
    public int? AccumulatedDepreciationAccountId { get; set; }

    // حساب هزینه استهلاک (بدهکار هنگام استهلاک)
    public int? DepreciationExpenseAccountId { get; set; }

    // عمر مفید پیش‌فرض به ماه
    public int DefaultUsefulLifeMonths { get; set; } = 60;

    [ForeignKey(nameof(AssetAccountId))]
    public virtual Account? AssetAccount { get; set; }

    [ForeignKey(nameof(AccumulatedDepreciationAccountId))]
    public virtual Account? AccumulatedDepreciationAccount { get; set; }

    [ForeignKey(nameof(DepreciationExpenseAccountId))]
    public virtual Account? DepreciationExpenseAccount { get; set; }

    public virtual ICollection<FixedAsset> Assets { get; set; } = [];
}
