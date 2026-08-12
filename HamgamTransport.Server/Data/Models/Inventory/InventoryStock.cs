using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProductEntity = HamgamTransport.Server.Data.Models.Product.Product;

namespace HamgamTransport.Server.Data.Models.Inventory
{
    // موجودی فعلی هر محصول در هر انبار — به واحد پایه گروه واحد محصول
    public class InventoryStock : BaseEntity
    {
        [Key]
        public int InventoryStockID { get; set; }

        public int WarehouseId { get; set; }
        public int ProductId { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal QuantityInBase { get; set; }

        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public virtual ProductEntity Product { get; set; } = null!;
    }
}
