using System.ComponentModel.DataAnnotations;

namespace Hamgam.Shared.Data.Models;

public class ReferenceCurrency : ReferenceBaseEntity
{
    [Key]
    public int CurrencyID { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;

    [MaxLength(3)]
    public string CurrencyCode { get; set; } = string.Empty;

    public string? Description { get; set; }
    public bool IsBaseCurrency { get; set; }
    public byte DecimalPlaces { get; set; }

    // استفاده در هر دو سیستم (سیمان + ترانسپورت)
    public bool UseInBothSystems { get; set; }

    // سیستمی که این ارز را ایجاد کرده: Cement یا Transport
    [MaxLength(20)]
    public string OriginSystem { get; set; } = string.Empty;

    public virtual ICollection<ReferenceCurrencyExchangeRate> ExchangeRates { get; set; } = [];
    public virtual ICollection<ReferenceCurrencyExchangeHistory> ExchangeHistories { get; set; } = [];
}
