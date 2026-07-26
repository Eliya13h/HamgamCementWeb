using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// حساب دفترکل — کدینگ چهارسطحی (گروه / کل / معین / تفصیلی)
public class Account : BaseEntity
{
    [Key]
    public int AccountID { get; set; }

    [MaxLength(32)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public AccountLevel Level { get; set; }

    public int? ParentAccountId { get; set; }

    public AccountType AccountType { get; set; }

    public AccountNature Nature { get; set; }

    // فقط حساب‌های قابل‌ثبت در سند (معمولاً تفصیلی یا معین بدون فرزند اجباری)
    public bool IsPostable { get; set; }

    public bool IsSystem { get; set; }

    // کد ثابت سیستمی برای Posting — مستقل از نام فارسی
    [MaxLength(50)]
    public string? SystemCode { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [ForeignKey(nameof(ParentAccountId))]
    public virtual Account? ParentAccount { get; set; }

    public virtual ICollection<Account> Children { get; set; } = [];
}
