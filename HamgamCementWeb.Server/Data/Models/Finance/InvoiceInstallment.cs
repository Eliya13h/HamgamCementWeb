using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// قسط فاکتور فروش یا خرید
public class InvoiceInstallment : BaseEntity
{
    [Key]
    public int InvoiceInstallmentID { get; set; }

    // ۱=فروش، ۲=خرید
    public InvoiceInstallmentKind InvoiceKind { get; set; }

    // شناسه فاکتور فروش یا خرید بر اساس InvoiceKind
    public int InvoiceId { get; set; }

    public int InstallmentNo { get; set; }

    public DateTime DueDate { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    // مبلغ دریافت/پرداخت‌شده از این قسط
    [Column(TypeName = "decimal(18,4)")]
    public decimal PaidAmount { get; set; }
}
