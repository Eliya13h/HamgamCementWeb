using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.People;

namespace HamgamCementWeb.Server.Data.Models.Production;

// دسته‌بندی هزینه‌های تولید — دو مورد سیستمی (مستقیم/غیرمستقیم) + دسته‌های داینامیک کاربر
public class ProductionCostCategory : BaseEntity
{
    [Key]
    public int ProductionCostCategoryID { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // کد ثابت برای دسته‌های سیستمی: DIRECT_WAGE / OVERHEAD
    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // دسته‌های سیستمی قابل حذف نیستند
    public bool IsSystem { get; set; }

    // نگاشت به نوع هزینه برای سند حسابداری تولید
    public ProductionCostType CostType { get; set; }

    // حساب اختیاری؛ در صورت خالی بودن از حساب سیستمی CostType استفاده می‌شود
    public int? AccountId { get; set; }

    public int SortOrder { get; set; }

    [ForeignKey(nameof(AccountId))]
    public virtual Account? Account { get; set; }

    public virtual ICollection<ProductionCostCategoryDepartment> Departments { get; set; } = [];
}

// اتصال بخش‌های سازمانی به هزینه مستقیم/غیرمستقیم برای جمع پایه حقوق
public class ProductionCostCategoryDepartment
{
    public int ProductionCostCategoryId { get; set; }

    public int DepartmentId { get; set; }

    [ForeignKey(nameof(ProductionCostCategoryId))]
    public virtual ProductionCostCategory Category { get; set; } = null!;

    [ForeignKey(nameof(DepartmentId))]
    public virtual Department Department { get; set; } = null!;
}

public static class ProductionCostCategoryCode
{
    public const string DirectWage = "DIRECT_WAGE";
    public const string Overhead = "OVERHEAD";
}
