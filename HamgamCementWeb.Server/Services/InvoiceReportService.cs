using System.Collections.Concurrent;
using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models;
using HamgamCementWeb.Server.Data.Models.Invoice;
using HamgamCementWeb.Server.Data.Models.Product;
using Microsoft.EntityFrameworkCore;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;

namespace HamgamCementWeb.Server.Services;

public interface IInvoiceReportService
{
    Task<StiReport> BuildPurchaseInvoiceReportAsync(int purchaseInvoiceId, CancellationToken cancellationToken = default);
    Task<StiReport> BuildSalesInvoiceReportAsync(int saleInvoiceId, CancellationToken cancellationToken = default);
}

public class InvoiceReportService : IInvoiceReportService
{
    private const int GeneralSettingsId = 1;
    private const string DefaultZmLogoWebPath = "/zm_logo.jpg";

    private static readonly object InvoiceTemplateLock = new();
    private static StiReport? InvoiceTemplate;
    private static DateTime InvoiceTemplateWriteTimeUtc;
    private static readonly ConcurrentDictionary<string, CachedLogoBytes> LogoBytesCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public InvoiceReportService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<StiReport> BuildPurchaseInvoiceReportAsync(
        int purchaseInvoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _db.PurchaseInvoices
            .AsNoTracking()
            .Include(i => i.Supplier)
            .Include(i => i.Currency)
            .Include(i => i.BaseCurrency)
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
                .ThenInclude(x => x.Product)
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
                .ThenInclude(x => x.Meaurment)
            .FirstOrDefaultAsync(i => i.PurchaseInvoiceID == purchaseInvoiceId && i.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("فاکتور خرید یافت نشد.");

        var invoiceSymbol = invoice.Currency?.Symbol ?? string.Empty;
        var baseSymbol = invoice.BaseCurrency?.Symbol ?? string.Empty;
        var isMultiCurrency = invoice.CurrencyId != invoice.BaseCurrencyId;
        var remaining = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);
        var paidInBase = invoice.TotalAmount > 0
            ? Math.Round(invoice.PaidAmount * invoice.TotalAmountInBaseCurrency / invoice.TotalAmount, 4)
            : 0m;
        var remainingInBase = Math.Max(0, invoice.TotalAmountInBaseCurrency - paidInBase);

        var info = await CreateInfoAsync(cancellationToken);
        info.InvoiceNumber = invoice.InvoiceNumber;
        info.InvoiceType = invoice.DocumentType switch
        {
            InvoiceDocumentType.PurchaseReturn => "برگشت از خرید",
            _ => $"{GetStatusName(invoice.Status)} خرید",
        };
        info.InvoiceRecipient = invoice.Supplier?.Name ?? string.Empty;
        info.InvoiceDate = JalaliDateHelper.FormatDate(invoice.InvoiceDate);
        info.CurrencySymbol = baseSymbol;
        info.IsChanged = isMultiCurrency;
        info.ShowPayment = true;
        info.TotalInvoice = FormatMoney(invoice.TotalAmount, invoiceSymbol);
        info.TotalPaid = FormatMoney(invoice.PaidAmount, invoiceSymbol);
        info.Remaining = FormatMoney(remaining, invoiceSymbol);
        info.TotalInvoiceBaseCurrency = FormatMoney(invoice.TotalAmountInBaseCurrency, baseSymbol);
        info.TotalPaidBaseCurrency = FormatMoney(paidInBase, baseSymbol);
        info.RemainingBaseCurrency = FormatMoney(remainingInBase, baseSymbol);
        info.Status = invoice.Status;

        var items = invoice.Items
            .OrderBy(x => x.PurchaseItemID)
            .Select((item, index) => new InvoiceReportItem
            {
                Row = index + 1,
                Desc = FormatProductDesc(item.Product?.Name, item.Product?.Code),
                Qty = FormatQuantityWithUnit(item.Quantity, item.Meaurment),
                // UnitPrice در دیتابیس فی واحد پایه است؛ در چاپ فی واحد خریداری‌شده نشان داده می‌شود
                Price = FormatMoney(GetUnitPriceInTransactionUnit(item.UnitPrice, item.Quantity, item.LineTotal), invoiceSymbol),
                SubTotal = FormatMoney(item.LineTotal, invoiceSymbol),
                Comment = string.Empty,
            })
            .ToList();

        return BuildReport(info, items);
    }

    public async Task<StiReport> BuildSalesInvoiceReportAsync(
        int saleInvoiceId,
        CancellationToken cancellationToken = default)
    {
        var invoice = await _db.SaleInvoices
            .AsNoTracking()
            .Include(i => i.Customer)
            .Include(i => i.Currency)
            .Include(i => i.BaseCurrency)
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
                .ThenInclude(x => x.Product)
            .Include(i => i.Items.Where(x => x.IsDeleted != true))
                .ThenInclude(x => x.Meaurment)
            .FirstOrDefaultAsync(i => i.SaleInvoiceID == saleInvoiceId && i.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("فاکتور فروش یافت نشد.");

        var invoiceSymbol = invoice.Currency?.Symbol ?? string.Empty;
        var baseSymbol = invoice.BaseCurrency?.Symbol ?? string.Empty;
        var isMultiCurrency = invoice.CurrencyId != invoice.BaseCurrencyId;
        var showPayment = InvoiceStatusRules.ShowsPayment(invoice.Status);
        var remaining = Math.Max(0, invoice.TotalAmount - invoice.PaidAmount);
        var paidInBase = invoice.TotalAmount > 0
            ? Math.Round(invoice.PaidAmount * invoice.TotalAmountInBaseCurrency / invoice.TotalAmount, 4)
            : 0m;
        var remainingInBase = Math.Max(0, invoice.TotalAmountInBaseCurrency - paidInBase);

        var info = await CreateInfoAsync(cancellationToken);
        info.InvoiceNumber = invoice.InvoiceNumber;
        info.InvoiceType = invoice.DocumentType switch
        {
            InvoiceDocumentType.SaleReturn => "برگشت از فروش",
            _ => $"{GetStatusName(invoice.Status)} فروش",
        };
        info.InvoiceRecipient = invoice.Customer?.Name ?? string.Empty;
        info.InvoiceDate = JalaliDateHelper.FormatDate(invoice.InvoiceDate);
        info.CurrencySymbol = baseSymbol;
        info.IsChanged = isMultiCurrency;
        info.ShowPayment = showPayment;
        info.TotalInvoice = FormatMoney(invoice.TotalAmount, invoiceSymbol);
        info.TotalPaid = showPayment ? FormatMoney(invoice.PaidAmount, invoiceSymbol) : string.Empty;
        info.Remaining = showPayment ? FormatMoney(remaining, invoiceSymbol) : string.Empty;
        info.TotalInvoiceBaseCurrency = FormatMoney(invoice.TotalAmountInBaseCurrency, baseSymbol);
        info.TotalPaidBaseCurrency = showPayment ? FormatMoney(paidInBase, baseSymbol) : string.Empty;
        info.RemainingBaseCurrency = showPayment ? FormatMoney(remainingInBase, baseSymbol) : string.Empty;
        info.Status = invoice.Status;

        var items = invoice.Items
            .OrderBy(x => x.SalesItemID)
            .Select((item, index) => new InvoiceReportItem
            {
                Row = index + 1,
                Desc = FormatProductDesc(item.Product?.Name, item.Product?.Code),
                Qty = FormatQuantityWithUnit(item.Quantity, item.Meaurment),
                // UnitPrice در دیتابیس فی واحد پایه است؛ در چاپ فی واحد فروخته‌شده نشان داده می‌شود
                Price = FormatMoney(GetUnitPriceInTransactionUnit(item.UnitPrice, item.Quantity, item.LineTotal), invoiceSymbol),
                SubTotal = FormatMoney(item.LineTotal, invoiceSymbol),
                Comment = string.Empty,
            })
            .ToList();

        return BuildReport(info, items);
    }

    private async Task<InvoiceReportInfo> CreateInfoAsync(CancellationToken cancellationToken)
    {
        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var zmLogoWebPath = string.IsNullOrWhiteSpace(settings.ZmLogoPath)
            ? DefaultZmLogoWebPath
            : settings.ZmLogoPath;

        return new InvoiceReportInfo
        {
            PersianCompanyName = settings.PersianCompanyName ?? string.Empty,
            EnglishCompanyName = settings.EnglishCompanyName ?? string.Empty,
            CompanyAddress = settings.CompanyAddress?.Trim() ?? string.Empty,
            CompanyEmail = settings.CompanyEmail?.Trim() ?? string.Empty,
            CompanyPhones = FormatCompanyPhones(
                settings.CompanyPhoneNumber1,
                settings.CompanyPhoneNumber2,
                settings.CompanyPhoneNumber3),
            ZmLogo = ResolveLogoPath(zmLogoWebPath),
            CompanyLogo = ResolveLogoPath(settings.CompanyLogoPath),
            PrintDate = JalaliDateHelper.FormatDate(DateTime.Now),
        };
    }

    private static readonly string[] MultiCurrencyComponentNames =
    [
        "Text24",
        "Text25",
        "Text26",
        "Text27",
        "Text28",
        "Text29",
    ];

    private static readonly string[] PaymentComponentNames =
    [
        "Text19",
        "Text20",
        "Text21",
        "Text22",
        "Text23",
        "Text25",
        "Text26",
        "Text27",
        "Text28",
        "Text29",
    ];

    private StiReport BuildReport(InvoiceReportInfo info, IReadOnlyList<InvoiceReportItem> items)
    {
        // Interpretation + بدون Compile/Render: Viewer خودش رندر می‌کند؛ Compile روی هر درخواست خیلی کند است
        var report = CreateInvoiceReportFromTemplate();
        report.RegBusinessObject("Info", info);
        report.RegBusinessObject("Items", items);
        report.Dictionary.Synchronize();
        ApplyReportImages(report, info);
        ApplyMultiCurrencyVisibility(report, info.IsChanged);
        ApplyPaymentVisibility(report, info.ShowPayment, info.IsChanged);
        // برای PDF: فونت باید در StiFontCollection با همان نام کامپوننت باشد و EmbeddedFonts روشن باشد.
        ReportFontHelper.PrepareNotoNastaliqForPdf(report, _env, "Text34", 14F);
        report.Render(false);
        return report;
    }

    private StiReport CreateInvoiceReportFromTemplate()
    {
        var reportPath = Path.Combine(_env.ContentRootPath, "Reports", "Inoive.mrt");
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("فایل گزارش فاکتور یافت نشد.", reportPath);
        }

        var writeTimeUtc = File.GetLastWriteTimeUtc(reportPath);
        lock (InvoiceTemplateLock)
        {
            if (InvoiceTemplate is null || InvoiceTemplateWriteTimeUtc != writeTimeUtc)
            {
                var template = new StiReport();
                template.Load(reportPath);
                template.CalculationMode = StiCalculationMode.Interpretation;
                InvoiceTemplate = template;
                InvoiceTemplateWriteTimeUtc = writeTimeUtc;
            }

            return (StiReport)InvoiceTemplate.Clone();
        }
    }

    private static void ApplyReportImages(StiReport report, InvoiceReportInfo info)
    {
        // در mrt نام Imageها با فیلدهای Info جابجا شده: CompanyLogo ← ZmLogo ، ZmLogo ← CompanyLogo
        SetReportImage(report, "CompanyLogo", info.ZmLogo);
        SetReportImage(report, "ZmLogo", info.CompanyLogo);
    }

    [SupportedOSPlatform("windows")]
    private static void SetReportImage(StiReport report, string componentName, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (report.GetComponentByName(componentName) is not StiImage image)
        {
            return;
        }

        var bytes = GetCachedLogoBytes(path);
        if (bytes is null || bytes.Length == 0)
        {
            return;
        }

        using var stream = new MemoryStream(bytes);
        image.Image = Image.FromStream(stream);
    }

    private static byte[]? GetCachedLogoBytes(string path)
    {
        if (!File.Exists(path))
        {
            LogoBytesCache.TryRemove(path, out _);
            return null;
        }

        var writeTimeUtc = File.GetLastWriteTimeUtc(path);
        if (LogoBytesCache.TryGetValue(path, out var cached) && cached.WriteTimeUtc == writeTimeUtc)
        {
            return cached.Bytes;
        }

        var bytes = File.ReadAllBytes(path);
        LogoBytesCache[path] = new CachedLogoBytes(bytes, writeTimeUtc);
        return bytes;
    }

    private sealed record CachedLogoBytes(byte[] Bytes, DateTime WriteTimeUtc);

    private string ResolveLogoPath(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath))
        {
            return string.Empty;
        }

        var relativePath = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fileName = Path.GetFileName(relativePath);
        var candidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(_env.WebRootPath))
        {
            candidates.Add(Path.Combine(_env.WebRootPath, relativePath));
            candidates.Add(Path.Combine(_env.WebRootPath, fileName));
        }

        candidates.Add(Path.GetFullPath(Path.Combine(
            _env.ContentRootPath,
            "..",
            "hamgamcementweb.client",
            "public",
            fileName)));

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }

    private static void ApplyMultiCurrencyVisibility(StiReport report, bool showBaseCurrencySection)
    {
        foreach (var componentName in MultiCurrencyComponentNames)
        {
            if (report.GetComponentByName(componentName) is StiComponent component)
            {
                component.Enabled = showBaseCurrencySection;
            }
        }
    }

    private static void ApplyPaymentVisibility(StiReport report, bool showPayment, bool showBaseCurrencySection)
    {
        foreach (var componentName in PaymentComponentNames)
        {
            if (report.GetComponentByName(componentName) is not StiComponent component)
            {
                continue;
            }

            var isBaseCurrencyPayment = componentName is "Text25" or "Text26" or "Text27" or "Text28" or "Text29";
            component.Enabled = showPayment && (!isBaseCurrencyPayment || showBaseCurrencySection);
        }

        if (report.GetComponentByName("FooterBand1") is StiFooterBand footerBand)
        {
            if (!showPayment)
            {
                footerBand.Height = showBaseCurrencySection ? 18d : 18d;
            }
            else
            {
                footerBand.Height = showBaseCurrencySection ? 36d : 18d;
            }
        }
    }

    private static string FormatCompanyPhones(params string?[] phones)
    {
        var parts = phones
            .Select(p => p?.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Cast<string>()
            .ToArray();

        return parts.Length == 0 ? string.Empty : string.Join(" - ", parts);
    }

    /// <summary>
    /// UnitPrice ذخیره‌شده فی هر واحد پایه است (LineTotal = QuantityInBase × UnitPrice).
    /// برای چاپ، فی هر واحد معامله (خرید/فروش) لازم است: LineTotal ÷ Quantity.
    /// </summary>
    private static decimal GetUnitPriceInTransactionUnit(
        decimal unitPricePerBase,
        decimal quantity,
        decimal lineTotal)
    {
        if (quantity > 0)
        {
            return lineTotal / quantity;
        }

        return unitPricePerBase;
    }

    private static string FormatQuantityWithUnit(decimal quantity, Meaurment? meaurment)
    {
        if (quantity <= 0)
        {
            return string.Empty;
        }

        var formatted = quantity.ToString("#,##0.##", CultureInfo.InvariantCulture);
        var unit = meaurment?.Symbol ?? meaurment?.Name ?? string.Empty;
        return string.IsNullOrWhiteSpace(unit) ? formatted : $"{formatted} {unit}";
    }

    private static string FormatProductDesc(string? name, string? code)
    {
        var productName = name?.Trim() ?? string.Empty;
        var productCode = code?.Trim() ?? string.Empty;

        if (productName.Length > 0 && productCode.Length > 0)
        {
            return $"{productName} ({productCode})";
        }

        return productName.Length > 0 ? productName : productCode;
    }

    private static string FormatMoney(decimal amount, string symbol)
    {
        var formatted = amount.ToString("#,##0.##", CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(symbol) ? formatted : $"{formatted} {symbol}";
    }

    private static string GetStatusName(InvoiceStatus status) => status switch
    {
        InvoiceStatus.Quotation => "استعلام قیمت",
        InvoiceStatus.Proforma => "پیش فاکتور",
        InvoiceStatus.Order => "آردر",
        InvoiceStatus.Invoice => "فاکتور",
        _ => status.ToString(),
    };
}

public class InvoiceReportInfo
{
    public string InvoiceType { get; set; } = string.Empty;
    public string InvoiceRecipient { get; set; } = string.Empty;
    public string InvoiceDate { get; set; } = string.Empty;
    public string PrintDate { get; set; } = string.Empty;
    public string TotalInvoice { get; set; } = string.Empty;
    public string TotalPaid { get; set; } = string.Empty;
    public string Remaining { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string WaterMark { get; set; } = string.Empty;
    public bool IsChanged { get; set; }
    public bool ShowPayment { get; set; } = true;
    public string CurrencySymbol { get; set; } = string.Empty;
    public string TotalInvoiceBaseCurrency { get; set; } = string.Empty;
    public string TotalPaidBaseCurrency { get; set; } = string.Empty;
    public string RemainingBaseCurrency { get; set; } = string.Empty;
    public InvoiceStatus Status { get; set; }

    // هدر/فوتر داینامیک از GeneralSettings (هم‌خوان با Inoive.mrt)
    public string PersianCompanyName { get; set; } = string.Empty;
    public string EnglishCompanyName { get; set; } = string.Empty;
    public string CompanyPhones { get; set; } = string.Empty;
    public string CompanyEmail { get; set; } = string.Empty;
    public string CompanyAddress { get; set; } = string.Empty;
    public string CompanyLogo { get; set; } = string.Empty;
    public string ZmLogo { get; set; } = string.Empty;
}

public class InvoiceReportItem
{
    public int Row { get; set; }
    public string Desc { get; set; } = string.Empty;
    public string Qty { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string SubTotal { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}
