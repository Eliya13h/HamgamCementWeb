using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.People;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// اتصال کاربر به صندوق — یک صندوق می‌تواند چند کاربر داشته باشد
public class CashBoxUser : BaseEntity
{
    [Key]
    public int CashBoxUserID { get; set; }

    public int CashBoxId { get; set; }

    public int UserId { get; set; }

    [ForeignKey(nameof(CashBoxId))]
    public virtual CashBox CashBox { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public virtual User User { get; set; } = null!;
}
