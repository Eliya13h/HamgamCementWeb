using System.ComponentModel.DataAnnotations;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// دسته‌بندی عواید حسابداری
public class RevenueCategory : BaseEntity
{
    [Key]
    public int RevenueCategoryID { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // کد ثابت برای دسته‌های سیستمی مثل فروش محصولات
    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // دسته‌های سیستمی قابل حذف نیستند
    public bool IsSystem { get; set; }

    public virtual ICollection<Revenue> Revenues { get; set; } = [];
}
