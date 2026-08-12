using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using HamgamTransport.Server.Data.Models.Inventory;
using HamgamTransport.Server.Data.Models.People;

namespace HamgamTransport.Server.Data.Models.Invoice;

public class SaleInvoice : BaseEntity
{
    [Key]
    public int SaleInvoiceID { get; set; }

    [MaxLength(50)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public int WarehouseId { get; set; }

    public DateTime InvoiceDate { get; set; } = DateTime.Now;
    public bool IsCash { get; set; } = true;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Quotation;

    // نوع سند: فاکتور عادی یا برگشت از فروش
    public InvoiceDocumentType DocumentType { get; set; } = InvoiceDocumentType.Invoice;

    // فاکتور مبدأ — برای اسناد برگشت از فروش
    public int? ReferenceSaleInvoiceId { get; set; }

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

    // مهلت پرداخت به روز (برای محاسبه سررسید)
    public int PaymentTermDays { get; set; }

    // تاریخ سررسید فاکتور نسیه
    public DateTime? DueDate { get; set; }

    // مبلغ دریافت‌شده از مشتری (ارز فاکتور)
    [Column(TypeName = "decimal(18,4)")]
    public decimal PaidAmount { get; set; }

    // بهای تمام‌شده FIFO (ارز پایه)
    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalCostInBaseCurrency { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal TotalProfitInBaseCurrency { get; set; }

    [MaxLength(2000)]
    public string? Description { get; set; }

    public int? RevenueId { get; set; }

    // سند دفترروزنامه پس از ثبت نهایی
    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(CustomerId))]
    public virtual Customer Customer { get; set; } = null!;

    [ForeignKey(nameof(WarehouseId))]
    public virtual Warehouse Warehouse { get; set; } = null!;

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(BaseCurrencyId))]
    public virtual Currency BaseCurrency { get; set; } = null!;

    [ForeignKey(nameof(ExchangeHistoryId))]
    public virtual CurrencyExchangeHistory? ExchangeHistory { get; set; }

    public virtual Revenue? Revenue { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }

    [ForeignKey(nameof(ReferenceSaleInvoiceId))]
    public virtual SaleInvoice? ReferenceSaleInvoice { get; set; }

    public virtual ICollection<SaleInvoice> ReturnDocuments { get; set; } = [];

    public virtual ICollection<SalesItem> Items { get; set; } = [];
}
