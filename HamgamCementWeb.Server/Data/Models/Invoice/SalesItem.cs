using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Product;
using ProductEntity = HamgamCementWeb.Server.Data.Models.Product.Product;

namespace HamgamCementWeb.Server.Data.Models.Invoice;

public class SalesItem : BaseEntity
{
    [Key]
    public int SalesItemID { get; set; }

    public int SaleInvoiceId { get; set; }
    public int ProductId { get; set; }
    public int MeaurmentId { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal Quantity { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal QuantityInBase { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineTotal { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineTotalInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineCostInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal LineProfitInBaseCurrency { get; set; }

    // ردیف مبدأ — برای ردیف‌های سند برگشت از فروش
    public int? ReferenceSalesItemId { get; set; }

    // مقدار برگشت‌شده تجمعی (واحد پایه) از این ردیف فروش
    [Column(TypeName = "decimal(18,6)")]
    public decimal ReturnedQuantityInBase { get; set; }

    [ForeignKey(nameof(SaleInvoiceId))]
    public virtual SaleInvoice Invoice { get; set; } = null!;

    [ForeignKey(nameof(ProductId))]
    public virtual ProductEntity Product { get; set; } = null!;

    [ForeignKey(nameof(MeaurmentId))]
    public virtual Meaurment Meaurment { get; set; } = null!;

    [ForeignKey(nameof(ReferenceSalesItemId))]
    public virtual SalesItem? ReferenceSalesItem { get; set; }

    public virtual ICollection<SaleItemLotAllocation> LotAllocations { get; set; } = [];
}
