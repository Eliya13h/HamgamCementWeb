using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// دسته‌بندی مصارف حسابداری (جدا از مصارف حمل‌ونقل)
public class ExpenseCategory : BaseEntity
{
    [Key]
    public int ExpenseCategoryID { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // کد ثابت برای دسته‌های سیستمی مثل خرید محصولات
    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // دسته‌های سیستمی قابل حذف نیستند
    public bool IsSystem { get; set; }

    public virtual ICollection<Expense> Expenses { get; set; } = [];
}
