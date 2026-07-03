using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.Product;
using ProductEntity = HamgamCementWeb.Server.Data.Models.Product.Product;

namespace HamgamCementWeb.Server.Data.Models.Invoice;

public class PurchaseItem : BaseEntity
{
    [Key]
    public int PurchaseItemID { get; set; }

    public int PurchaseInvoiceId { get; set; }
    public int ProductId { get; set; }
    public int MeaurmentId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal QuantityInBase { get; set; }

    // قیمت واحد به ارز فاکتور
    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineTotal { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineTotalInBaseCurrency { get; set; }

    public int? InventoryLotId { get; set; }

    // ردیف مبدأ — برای ردیف‌های سند برگشت از خرید
    public int? ReferencePurchaseItemId { get; set; }

    // مقدار برگشت‌شده تجمعی (واحد پایه) از این ردیف خرید
    [Column(TypeName = "decimal(18,6)")]
    public decimal ReturnedQuantityInBase { get; set; }

    [ForeignKey(nameof(PurchaseInvoiceId))]
    public virtual PurchaseInvoice Invoice { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual ProductEntity Product { get; set; } = null!;

    [ForeignKey(nameof(MeaurmentId))]
    public virtual Meaurment Meaurment { get; set; } = null!;

    [ForeignKey(nameof(InventoryLotId))]
    public virtual InventoryLot? InventoryLot { get; set; }

    [ForeignKey(nameof(ReferencePurchaseItemId))]
    public virtual PurchaseItem? ReferencePurchaseItem { get; set; }
}
