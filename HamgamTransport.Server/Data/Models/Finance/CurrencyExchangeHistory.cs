using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Finance
{
    /// <summary>
    /// تاریخچه تغییرات نرخ هر ارز نسبت به ارز پایه.
    /// برای محاسبه ارزش معاملات در گذشته، نرخ معتبر در تاریخ معامله از این جدول خوانده می‌شود.
    /// </summary>
    public class CurrencyExchangeHistory : BaseEntity
    {
        [Key]
        public int HistoryID { get; set; }

        // ارز غیرپایه
        [ForeignKey(nameof(Currency))]
        public int CurrencyID { get; set; }
        public virtual Currency? Currency { get; set; }

        // ارز پایه در زمان ثبت
        [ForeignKey(nameof(BaseCurrency))]
        public int BaseCurrencyID { get; set; }
        public virtual Currency? BaseCurrency { get; set; }

        // نرخ جدید: چند واحد از ارز پایه معادل ۱ واحد این ارز
        [Column(TypeName = "decimal(18,8)")]
        public decimal BaseUnitsPerUnit { get; set; }

        // نرخ قبلی (برای گزارش تغییر و مقایسه)
        [Column(TypeName = "decimal(18,8)")]
        public decimal? PreviousBaseUnitsPerUnit { get; set; }

        // شروع اعتبار این نرخ
        public DateTime EffectiveFrom { get; set; }

        // پایان اعتبار؛ null یعنی آخرین نرخ ثبت‌شده تا زمان جایگزینی
        public DateTime? EffectiveTo { get; set; }

        // دلیل تغییر (دستی، بانک مرکزی، ...)
        [MaxLength(500)]
        public string? ChangeReason { get; set; }
    }
}
