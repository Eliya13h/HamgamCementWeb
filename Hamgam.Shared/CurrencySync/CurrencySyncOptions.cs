namespace Hamgam.Shared.CurrencySync;

public class CurrencySyncOptions
{
    public const string SectionName = "CurrencySync";

    /// <summary>کد سیستم جاری: Cement یا Transport</summary>
    public string SystemCode { get; set; } = SystemCodes.Cement;

    public string LocalConnectionStringName { get; set; } = "Local";

    public string ReferenceConnectionStringName { get; set; } = "Reference";
}
