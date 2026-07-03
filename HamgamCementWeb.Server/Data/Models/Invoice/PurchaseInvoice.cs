using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Inventory;
using HamgamCementWeb.Server.Data.Models.People;
using HamgamCementWeb.Server.Data.Models.Production;

namespace HamgamCementWeb.Server.Data.Models.Invoice;

public class PurchaseInvoice : BaseEntity
{
    [Key]
    public int PurchaseInvoiceID { get; set; }

    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public bool IsCash { get; set; } = true;

    public DateTime InvoiceDate { get; set; } = DateTime.Now;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Quotation;

    // نوع سند: فاکتور عادی یا برگشت از خرید
    public InvoiceDocumentType DocumentType { get; set; } = InvoiceDocumentType.Invoice;

    // منبع ورود کالا — خرید از بازار یا انتقال از بخش تولید
    public PurchaseEntrySource EntrySource { get; set; } = PurchaseEntrySource.Market;

    // سند تولید مرتبط — برای ردیابی اقلام واردشده از تولید
    public int? ProductionBatchId { get; set; }

    // هزینه ثابت و متغیر فاکتور — در جمع فاکتور و بهای واحد لحاظ می‌شود
    [Column(TypeName = "decimal(18,4)")]
    public decimal FixedCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal VariableCost { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal FixedCostInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal VariableCostInBaseCurrency { get; set; }

    // فاکتور مبدأ — برای اسناد برگشت از خرید
    public int? ReferencePurchaseInvoiceId { get; set; }

    // پس از ثبت نهایی — موجودی و مصارف ثبت می‌شود
    public bool IsPosted { get; set; }

    public DateTime? PostedAt { get; set; }

    public int CurrencyId { get; set; }
    public int BaseCurrencyId { get; set; }

    public int? ExchangeHistoryId { get; set; }

    [Column(TypeName = "decimal(18,8)")]
    public decimal BaseUnitsPerUnitAtTransaction { get; set; } = 1;

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalAmountInBaseCurrency { get; set; }

    // مبلغ پرداخت‌شده — برای ثبت در جدول مصارف هنگام ثبت نهایی
    [Column(TypeName = "decimal(18,4)")]
    public decimal PaidAmount { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? ExpenseId { get; set; }

    [ForeignKey(nameof(SupplierId))]
    public virtual Supplier Supplier { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    public virtual Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(BaseCurrencyId))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    [ForeignKey(nameof(ExchangeHistoryId))]
    public virtual CurrencyExchangeHistory? ExchangeHistory { get; set; }

    public virtual Expense? Expense { get; set; }

    [ForeignKey(nameof(ReferencePurchaseInvoiceId))]
    public virtual PurchaseInvoice? ReferencePurchaseInvoice { get; set; }

    [ForeignKey(nameof(ProductionBatchId))]
    public virtual Production.ProductionBatch? ProductionBatch { get; set; }

    public virtual ICollection<PurchaseInvoice> ReturnDocuments { get; set; } = [];

    public virtual ICollection<PurchaseItem> Items { get; set; } = [];
}
