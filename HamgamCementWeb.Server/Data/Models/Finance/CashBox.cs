using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// صندوق نقدی — سلسله‌مراتبی؛ انتقال پایان شیفت به صندوق والد
public class CashBox : BaseEntity
{
    [Key]
    public int CashBoxID { get; set; }

    [MaxLength(30)]
    public string Code { get; set; } = string.Empty;

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // صندوق بالاتر برای تحویل پایان شیفت — مرکزی بدون والد است
    public int? ParentCashBoxId { get; set; }

    // حساب تفصیلی متصل به این صندوق زیر معین صندوق‌ها
    public int AccountId { get; set; }

    // صندوق تنخواه — شارژ از والد تا سقف
    public bool IsPettyCash { get; set; }

    // سقف مانده تنخواه به ارز پایه
    [Column(TypeName = "decimal(18,4)")]
    public decimal CeilingAmountInBase { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [ForeignKey(nameof(ParentCashBoxId))]
    public virtual CashBox? ParentCashBox { get; set; }

    public virtual ICollection<CashBox> Children { get; set; } = [];

    [ForeignKey(nameof(AccountId))]
    public virtual Account Account { get; set; } = null!;

    public virtual ICollection<CashBoxUser> Users { get; set; } = [];

    public virtual ICollection<CashShift> Shifts { get; set; } = [];
}
