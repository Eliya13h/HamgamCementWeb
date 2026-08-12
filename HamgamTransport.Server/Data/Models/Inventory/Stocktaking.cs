using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;

namespace HamgamTransport.Server.Data.Models.Inventory
{
    // سند انبارگردانی
    public class Stocktaking : BaseEntity
    {
        [Key]
        public int StocktakingID { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public int WarehouseId { get; set; }

        public DateTime StocktakingDate { get; set; } = DateTime.Now;

        public StocktakingStatus Status { get; set; } = StocktakingStatus.Draft;

        // لینک سند دابل‌انتری کسری/اضافی (حساب موجودی ↔ SYS_INV_ADJ)
        public int? JournalEntryId { get; set; }

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; } = null!;

        [ForeignKey(nameof(JournalEntryId))]
        public virtual JournalEntry? JournalEntry { get; set; }

        public virtual ICollection<StocktakingLine> Lines { get; set; } = [];
    }
}
