using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.Inventory;
using HamgamTransport.Server.Data.Models.People;
using HamgamTransport.Server.Data.Models.Production;

namespace HamgamTransport.Server.Data.Models.Invoice;

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

    // سند تولید مرتبط — برای ردیابی اقلام واردشده از تولید (legacy؛ ورود جدید از تولید پشتیبانی نمی‌شود)
    public int? ProductionBatchId { get; set; }

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

    // جمع اقلام بدون مالیات (ارز فاکتور)
    [Column(TypeName = "decimal(18,4)")]
    public decimal SubTotalAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal SubTotalAmountInBaseCurrency { get; set; }

    // درصد مالیات پیش‌نویس
    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxPercent { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TaxAmountInBaseCurrency { get; set; }

    // مهلت پرداخت به روز
    public int PaymentTermDays { get; set; }

    // تاریخ سررسید فاکتور نسیه
    public DateTime? DueDate { get; set; }

    // مبلغ پرداخت‌شده — برای ثبت در جدول مصارف هنگام ثبت نهایی
    [Column(TypeName = "decimal(18,4)")]
    public decimal PaidAmount { get; set; }

    // صندوق پرداخت نقدی خرید — وقتی PaidAmount > 0 برای خروج نقد از این صندوق استفاده می‌شود
    public int? CashBoxId { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? ExpenseId { get; set; }

    // سند دفترروزنامه پس از ثبت نهایی
    public int? JournalEntryId { get; set; }

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

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }

    [ForeignKey(nameof(ReferencePurchaseInvoiceId))]
    public virtual PurchaseInvoice? ReferencePurchaseInvoice { get; set; }

    [ForeignKey(nameof(ProductionBatchId))]
    public virtual Production.ProductionBatch? ProductionBatch { get; set; }

    [ForeignKey(nameof(CashBoxId))]
    public virtual CashBox? CashBox { get; set; }

    public virtual ICollection<PurchaseInvoice> ReturnDocuments { get; set; } = [];

    public virtual ICollection<PurchaseItem> Items { get; set; } = [];
}
