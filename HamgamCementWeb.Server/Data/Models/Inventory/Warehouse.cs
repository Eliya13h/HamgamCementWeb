using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Product;

namespace HamgamCementWeb.Server.Data.Models.Inventory
{
    public class Warehouse : BaseEntity
    {
        [Key]
        public int WarehouseID { get; set; }

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public WarehouseType WarehouseType { get; set; } = WarehouseType.RawMaterials;

        [MaxLength(500)]
        public string? Location { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        // ظرفیت انبار
        [Column(TypeName = "decimal(18,6)")]
        public decimal? Capacity { get; set; }

        // واحد اندازه‌گیری ظرفیت
        public int? CapacityMeaurmentId { get; set; }

        [ForeignKey(nameof(CapacityMeaurmentId))]
        public virtual Meaurment? CapacityMeaurment { get; set; }

        public virtual ICollection<InventoryStock> Stocks { get; set; } = [];
        public virtual ICollection<Stocktaking> Stocktakings { get; set; } = [];
        public virtual ICollection<InventoryLot> Lots { get; set; } = [];
    }
}
