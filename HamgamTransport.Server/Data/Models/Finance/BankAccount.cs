using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Finance;

// حساب بانکی — تفصیلی زیر معین بانک‌ها (SYS_BANKS)
public class BankAccount : BaseEntity
{
    [Key]
    public int BankAccountID { get; set; }

    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // شماره حساب بانکی (اختیاری — برای نمایش/گزارش)
    [MaxLength(50)]
    public string? AccountNumber { get; set; }

    // حساب تفصیلی متصل به این حساب بانکی زیر معین بانک‌ها
    public int AccountId { get; set; }

    // ارز پیش‌فرض حساب بانکی (اختیاری)
    public int? CurrencyId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [ForeignKey(nameof(AccountId))]
    public virtual Account Account { get; set; } = null!;

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency? Currency { get; set; }
}
