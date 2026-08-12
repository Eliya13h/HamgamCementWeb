using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ProductEntity = HamgamTransport.Server.Data.Models.Product.Product;

namespace HamgamTransport.Server.Data.Models.Inventory
{
    // دسته موجودی برای FIFO — هر ورود (خرید) یک Lot جدا با ترتیب زمانی
    public class InventoryLot : BaseEntity
    {
        [Key]
        public int InventoryLotID { get; set; }

        [MaxLength(50)]
        public string LotCode { get; set; } = string.Empty;

        public int ProductId { get; set; }
        public int WarehouseId { get; set; }

        public DateTime ReceivedAt { get; set; } = DateTime.Now;

        // ترتیب دریافت برای FIFO (کمتر = قدیمی‌تر)
        public long ReceiptSequence { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal ReceivedQuantityInBase { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal RemainingQuantityInBase { get; set; }

        // بهای هر واحد پایه در لحظه دریافت (ارز پایه)
        [Column(TypeName = "decimal(18,4)")]
        public decimal UnitCost { get; set; }

    public int? PurchaseInvoiceId { get; set; }
    public int? PurchaseItemId { get; set; }

    // سند تولید مرتبط — برای ردیابی Lotهای تولیدی
    public int? ProductionBatchId { get; set; }

    [ForeignKey(nameof(ProductId))]
        public virtual ProductEntity Product { get; set; } = null!;

        [ForeignKey(nameof(WarehouseId))]
        public virtual Warehouse Warehouse { get; set; } = null!;
    }
}
