using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Invoice;

namespace HamgamCementWeb.Server.Data.Models.Finance;

// دریافت از مشتری / پرداخت به تأمین‌کننده — مستقل یا تخصیص به فاکتور
public class PartySettlement : BaseEntity
{
    [Key]
    public int PartySettlementID { get; set; }

    // ۱=مشتری، ۲=تأمین‌کننده
    public PartySettlementPartyType PartyType { get; set; }

    // شناسه مشتری یا تأمین‌کننده بر اساس PartyType
    public int PartyId { get; set; }

    public DateTime SettlementDate { get; set; } = DateTime.Now;

    public int CurrencyId { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal AmountInBaseCurrency { get; set; }

    // صندوق نقدی طرف تسویه (یکی از صندوق یا بانک الزامی است)
    public int? CashBoxId { get; set; }

    // حساب بانکی طرف تسویه
    public int? BankAccountId { get; set; }

    // تخصیص اختیاری به فاکتور فروش
    public int? SaleInvoiceId { get; set; }

    // تخصیص اختیاری به فاکتور خرید
    public int? PurchaseInvoiceId { get; set; }

    // تخصیص اختیاری به قسط فاکتور
    public int? InstallmentId { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    // سند دفترروزنامه پس از ثبت
    public int? JournalEntryId { get; set; }

    [ForeignKey(nameof(CurrencyId))]
    public virtual Currency Currency { get; set; } = null!;

    [ForeignKey(nameof(CashBoxId))]
    public virtual CashBox? CashBox { get; set; }

    [ForeignKey(nameof(BankAccountId))]
    public virtual BankAccount? BankAccount { get; set; }

    [ForeignKey(nameof(SaleInvoiceId))]
    public virtual SaleInvoice? SaleInvoice { get; set; }

    [ForeignKey(nameof(PurchaseInvoiceId))]
    public virtual PurchaseInvoice? PurchaseInvoice { get; set; }

    [ForeignKey(nameof(InstallmentId))]
    public virtual InvoiceInstallment? Installment { get; set; }

    [ForeignKey(nameof(JournalEntryId))]
    public virtual JournalEntry? JournalEntry { get; set; }
}
