using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;

namespace HamgamCementWeb.Server.Data.Models.Inventory
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

        [MaxLength(2000)]
        public string? Notes { get; set; }

        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; } = null!;

        public virtual ICollection<StocktakingLine> Lines { get; set; } = [];
    }
}
