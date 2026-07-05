using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HamgamCementWeb.Server.Data.Models.Finance;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // ردیف مصرف حمل و نقل — هر مصرف داخل یک فاکتور و با دسته‌بندی مشخص ثبت می‌شود
    public class TransportExpense : BaseEntity
    {
        [Key]
        public int TransportExpenseID { get; set; }

        public int TransportInvoiceId { get; set; }

        public int ExpensesCategoryId { get; set; }

        // عنوان مصرف (مثلاً سوخت‌گیری در مرز)
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,4)")]
        public decimal Amount { get; set; }

        // ارز مبلغ مصرف (اختیاری — پیش‌فرض ارز پایه سیستم)
        public int? CurrencyId { get; set; }

        // اضافه شد برای اسنپ‌شات ارز و تبدیل مبلغ به ارز پایه (مشابه Expense حسابداری)
        public int BaseCurrencyId { get; set; }

        // اضافه شد برای ارجاع به رکورد تاریخچه نرخ استفاده‌شده در لحظه ثبت
        public int? ExchangeHistoryId { get; set; }

        // اضافه شد برای ذخیره نرخ تبدیل به ارز پایه در لحظه تراکنش
        [Column(TypeName = "decimal(18,8)")]
        public decimal BaseUnitsPerUnitAtTransaction { get; set; } = 1;

        // اضافه شد برای نگهداری معادل مبلغ به ارز پایه تا جمع چندارزی درست باشد
        [Column(TypeName = "decimal(18,4)")]
        public decimal AmountInBaseCurrency { get; set; }

        public DateTime ExpenseDate { get; set; }

        public string? Description { get; set; }

        [ForeignKey(nameof(TransportInvoiceId))]
        public virtual TransportInvoice? Invoice { get; set; }

        [ForeignKey(nameof(ExpensesCategoryId))]
        public virtual ExpensesCategory? Category { get; set; }

        [ForeignKey(nameof(CurrencyId))]
        public virtual Currency? Currency { get; set; }
    }
}
