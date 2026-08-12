using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamgamTransport.Server.Data.Models.Finance
{
    public class Currency : BaseEntity
    {
        [Key]
        public int CurrencyID { get; set; }

        public string Name { get; set; } = string.Empty;

        // اصلاح املای Symbole — نماد نمایشی ارز (مثلاً ﷼، $)
        public string Symbol { get; set; } = string.Empty;

        // کد استاندارد ISO 4217 (مثلاً IRR، USD)
        [MaxLength(3)]
        public string CurrencyCode { get; set; } = string.Empty;

        public string? Description { get; set; }

        // فقط یک ارز در سیستم باید ارز پایه باشد؛ بقیه نرخ‌شان نسبت به این ارز ذخیره می‌شود
        public bool IsBaseCurrency { get; set; } = false;

        // تعداد رقم اعشار برای گرد کردن مبالغ این ارز
        public byte DecimalPlaces { get; set; } = 0;

        // استفاده در هر دو سیستم (سیمان + ترانسپورت)
        public bool UseInBothSystems { get; set; }

        // سیستمی که ارز در آن ایجاد شده: Cement یا Transport
        [MaxLength(20)]
        public string OriginSystem { get; set; } = string.Empty;

        public virtual ICollection<CurrencyExchangeRate> ExchangeRates { get; set; } = [];
        public virtual ICollection<CurrencyExchangeHistory> ExchangeHistories { get; set; } = [];
    }
}
