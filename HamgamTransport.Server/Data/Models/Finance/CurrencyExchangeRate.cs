using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Finance
{
    /// <summary>
    /// نرخ جاری هر ارز نسبت به ارز پایه — برای جستجوی سریع نرخ فعلی.
    /// با هر تغییر نرخ، رکورد متناظر در CurrencyExchangeHistory ثبت و این جدول به‌روز می‌شود.
    /// </summary>
    public class CurrencyExchangeRate : BaseEntity
    {
        [Key]
        public int CurrencyExchangeRateID { get; set; }

        // ارز غیرپایه‌ای که نرخش ثبت می‌شود (ارز پایه نباید در این جدول ردیف داشته باشد)
        [ForeignKey(nameof(Currency))]
        public int CurrencyID { get; set; }
        public virtual Currency? Currency { get; set; }

        // ارز پایه سیستم در زمان ثبت این نرخ
        [ForeignKey(nameof(BaseCurrency))]
        public int BaseCurrencyID { get; set; }
        public virtual Currency? BaseCurrency { get; set; }

        // چند واحد از ارز پایه معادل ۱ واحد این ارز است
        // مثال: پایه=ریال، دلار=۵۰۰٬۰۰۰ → BaseUnitsPerUnit = 500000
        [Column(TypeName = "decimal(18,8)")]
        public decimal BaseUnitsPerUnit { get; set; }

        // تاریخ شروع اعتبار این نرخ
        public DateTime EffectiveFrom { get; set; }

        // ارجاع به رکورد تاریخچه‌ای که منبع این نرخ جاری است
        [ForeignKey(nameof(SourceHistory))]
        public int? SourceHistoryID { get; set; }
        public virtual CurrencyExchangeHistory? SourceHistory { get; set; }
    }
}
