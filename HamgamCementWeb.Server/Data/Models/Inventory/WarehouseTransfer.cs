using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;

namespace HamgamCementWeb.Server.Data.Models.Inventory;

// سند انتقال کالا بین دو انبار (فیزیکی + دابل‌انتری در صورت تفاوت حساب موجودی)
public class WarehouseTransfer : BaseEntity
{
    [Key]
    public int WarehouseTransferID { get; set; }

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public DateTime TransferDate { get; set; } = DateTime.Now;

    public int FromWarehouseId { get; set; }

    public int ToWarehouseId { get; set; }

    public WarehouseTransferStatus Status { get; set; } = WarehouseTransferStatus.Draft;

    public bool IsPosted { get; set; }

    public DateTime? PostedAt { get; set; }

    // جمع بهای تمام‌شده اقلام منتقل‌شده (ارز پایه)
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalCostInBaseCurrency { get; set; }

    // لینک به سند دفتر — فقط وقتی حساب موجودی مبدأ و مقصد متفاوت باشد پر می‌شود
    public int? JournalEntryId { get; set; }

    [MaxLength(2000)]
    public string? Notes { get; set; }

    [ForeignKey(nameof(FromWarehouseId))]
    public virtual Warehouse FromWarehouse { get; set; } = null!;

    [ForeignKey(nameof(ToWarehouseId))]
    public virtual Warehouse ToWarehouse { get; set; } = null!;

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }

    public virtual ICollection<WarehouseTransferLine> Lines { get; set; } = [];
}
