using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamCementWeb.Server.Data.Models.Transport
{
    // فاکتور مصارف حمل و نقل — تمامی مصارف یک وسیله/سفر داخل یک فاکتور ثبت می‌شود
    public class TransportInvoice : BaseEntity
    {
        [Key]
        public int TransportInvoiceID { get; set; }

        // شماره یکتای فاکتور
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        public int VehicleId { get; set; }

        // سفر مربوطه (اختیاری — برخی مصارف خارج از سفر هستند)
        public int? TransportTripId { get; set; }

        public DateTime InvoiceDate { get; set; }

        // جمع کل مبالغ ردیف‌های فاکتور — هنگام ذخیره محاسبه و نگهداری می‌شود
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalAmount { get; set; }

        // اضافه شد برای نگهداری جمع کل به ارز پایه (چون ردیف‌ها می‌توانند ارزهای مختلف داشته باشند)
        [Column(TypeName = "decimal(18,4)")]
        public decimal TotalAmountInBaseCurrency { get; set; }

        // اضافه شد برای لینک فاکتور مصارف حمل‌ونقل به رکورد مصرف حسابداری (مشابه PurchaseInvoice.ExpenseId)
        public int? ExpenseId { get; set; }

        public string? Description { get; set; }

        [ForeignKey(nameof(VehicleId))]
        public virtual Vehicle? Vehicle { get; set; }

        [ForeignKey(nameof(TransportTripId))]
        public virtual TransportTrip? Trip { get; set; }

        [ForeignKey(nameof(ExpenseId))]
        public virtual Finance.Expense? Expense { get; set; }

        public virtual ICollection<TransportExpense> Expenses { get; set; } = [];
    }
}
