using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models;
using HamgamCementWeb.Server.Data.Models.Finance;
using HamgamCementWeb.Server.Data.Models.Invoice;
using Microsoft.EntityFrameworkCore;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;

namespace HamgamCementWeb.Server.Services;

public enum JournalReportType
{
    Purchase,
    Sale,
    Revenue,
    Expense,
    Production,
    General,
}

public interface IJournalReportService
{
    Task<StiReport> BuildPurchaseJournalReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);

    Task<StiReport> BuildSaleJournalReportAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);

    Task<StiReport> BuildOperationalJournalReportAsync(
        JournalReportType type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);
}

public class JournalReportService : IJournalReportService
{
    private const int GeneralSettingsId = 1;
    private const string DefaultZmLogoWebPath = "/zm_logo.jpg";

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;

    public JournalReportService(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public Task<StiReport> BuildPurchaseJournalReportAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        return BuildInvoiceJournalReportAsync(
            "روزنامچه خرید",
            dateFrom,
            dateTo,
            LoadPurchaseRowsAsync,
            GetPurchaseReturnDescription,
            cancellationToken);
    }

    public Task<StiReport> BuildSaleJournalReportAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        return BuildInvoiceJournalReportAsync(
            "روزنامچه فروش",
            dateFrom,
            dateTo,
            LoadSaleRowsAsync,
            GetSaleReturnDescription,
            cancellationToken);
    }

    public async Task<StiReport> BuildOperationalJournalReportAsync(
        JournalReportType type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        if (type is JournalReportType.Purchase or JournalReportType.Sale)
        {
            throw new InvalidOperationException("برای روزنامچه خرید/فروش از متد اختصاصی استفاده کنید.");
        }

        var rows = await LoadOperationalJournalRowsAsync(type, dateFrom, dateTo, cancellationToken);
        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var baseSymbol = await _db.Currencies
            .AsNoTracking()
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.Symbol)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

        var info = BuildInfo(settings, GetOperationalReportTitle(type), dateFrom, dateTo);
        var products = rows
            .Select((row, index) => new JournalReportProduct
            {
                InvoiceNumber = row.EntryNumber,
                ProductName = row.Description,
                ProductQTY = 0,
                ProductPrice = string.Empty,
                SubTotal = FormatMoney(row.AmountInBase, baseSymbol),
                Description = JournalSourceLabels.Label(row.Source),
                ShamsiDate = JalaliDateHelper.FormatDate(row.EntryDate),
                RowNumber = index + 1,
            })
            .ToList();

        return BuildReport(info, products);
    }

    private async Task<StiReport> BuildInvoiceJournalReportAsync(
        string reportTitle,
        DateTime? dateFrom,
        DateTime? dateTo,
        Func<DateTime?, DateTime?, CancellationToken, Task<List<JournalInvoiceItemRow>>> loadRowsAsync,
        Func<JournalInvoiceItemRow, string?> getReturnDescription,
        CancellationToken cancellationToken)
    {
        var rows = await loadRowsAsync(dateFrom, dateTo, cancellationToken);
        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var info = BuildInfo(settings, reportTitle, dateFrom, dateTo);
        var products = rows
            .Select((row, index) => MapProductRow(row, index + 1, getReturnDescription(row)))
            .ToList();

        return BuildReport(info, products);
    }

    private async Task<List<JournalInvoiceItemRow>> LoadPurchaseRowsAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var query = _db.PurchaseItems
            .AsNoTracking()
            .Where(i =>
                i.IsDeleted != true &&
                i.Invoice.IsDeleted != true &&
                i.Invoice.IsPosted &&
                (
                    i.Invoice.DocumentType == InvoiceDocumentType.PurchaseReturn ||
                    (i.Invoice.DocumentType == InvoiceDocumentType.Invoice &&
                     i.Invoice.Status == InvoiceStatus.Invoice)));

        if (dateFrom.HasValue)
        {
            query = query.Where(i => i.Invoice.InvoiceDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(i => i.Invoice.InvoiceDate <= end);
        }

        return await query
            .OrderBy(i => i.Invoice.InvoiceDate)
            .ThenBy(i => i.Invoice.InvoiceNumber)
            .ThenBy(i => i.PurchaseItemID)
            .Select(i => new JournalInvoiceItemRow
            {
                InvoiceNumber = i.Invoice.InvoiceNumber,
                ProductName = i.Product.Name,
                ProductCode = i.Product.Code,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
                LineTotalInBaseCurrency = i.LineTotalInBaseCurrency,
                InvoiceDate = i.Invoice.InvoiceDate,
                InvoiceSymbol = i.Invoice.Currency != null ? i.Invoice.Currency.Symbol : string.Empty,
                BaseSymbol = i.Invoice.BaseCurrency != null ? i.Invoice.BaseCurrency.Symbol : string.Empty,
                IsMultiCurrency = i.Invoice.CurrencyId != i.Invoice.BaseCurrencyId,
                DocumentType = i.Invoice.DocumentType,
                EntrySource = i.Invoice.EntrySource,
                ReferenceEntrySource = i.Invoice.ReferencePurchaseInvoice != null
                    ? i.Invoice.ReferencePurchaseInvoice.EntrySource
                    : null,
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<JournalInvoiceItemRow>> LoadSaleRowsAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var query = _db.SalesItems
            .AsNoTracking()
            .Where(i =>
                i.IsDeleted != true &&
                i.Invoice.IsDeleted != true &&
                i.Invoice.IsPosted &&
                (
                    i.Invoice.DocumentType == InvoiceDocumentType.SaleReturn ||
                    (i.Invoice.DocumentType == InvoiceDocumentType.Invoice &&
                     (i.Invoice.Status == InvoiceStatus.Order || i.Invoice.Status == InvoiceStatus.Invoice))));

        if (dateFrom.HasValue)
        {
            query = query.Where(i => i.Invoice.InvoiceDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(i => i.Invoice.InvoiceDate <= end);
        }

        return await query
            .OrderBy(i => i.Invoice.InvoiceDate)
            .ThenBy(i => i.Invoice.InvoiceNumber)
            .ThenBy(i => i.SalesItemID)
            .Select(i => new JournalInvoiceItemRow
            {
                InvoiceNumber = i.Invoice.InvoiceNumber,
                ProductName = i.Product.Name,
                ProductCode = i.Product.Code,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                LineTotal = i.LineTotal,
                LineTotalInBaseCurrency = i.LineTotalInBaseCurrency,
                InvoiceDate = i.Invoice.InvoiceDate,
                InvoiceSymbol = i.Invoice.Currency != null ? i.Invoice.Currency.Symbol : string.Empty,
                BaseSymbol = i.Invoice.BaseCurrency != null ? i.Invoice.BaseCurrency.Symbol : string.Empty,
                IsMultiCurrency = i.Invoice.CurrencyId != i.Invoice.BaseCurrencyId,
                DocumentType = i.Invoice.DocumentType,
            })
            .ToListAsync(cancellationToken);
    }

    private static string? GetPurchaseReturnDescription(JournalInvoiceItemRow row)
    {
        if (row.DocumentType != InvoiceDocumentType.PurchaseReturn)
        {
            return null;
        }

        if (row.EntrySource == PurchaseEntrySource.Production ||
            row.ReferenceEntrySource == PurchaseEntrySource.Production)
        {
            return "برگشت از تولید";
        }

        return "برگشت از خرید";
    }

    private static string? GetSaleReturnDescription(JournalInvoiceItemRow row)
    {
        return row.DocumentType == InvoiceDocumentType.SaleReturn ? "برگشت از فروش" : null;
    }

    private async Task<List<OperationalJournalRow>> LoadOperationalJournalRowsAsync(
        JournalReportType type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var query = _db.JournalEntries
            .AsNoTracking()
            .Where(e => e.IsDeleted != true && e.IsPosted);

        query = ApplyOperationalSourceFilter(query, type);

        if (dateFrom.HasValue)
        {
            query = query.Where(e => e.EntryDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.EntryDate <= end);
        }

        return await query
            .OrderBy(e => e.EntryDate)
            .ThenBy(e => e.EntryNumber)
            .ThenBy(e => e.JournalEntryID)
            .Select(e => new OperationalJournalRow
            {
                EntryNumber = e.EntryNumber,
                Description = e.Description,
                EntryDate = e.EntryDate,
                AmountInBase = e.TotalDebitInBaseCurrency,
                Source = (int)e.Source,
            })
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<JournalEntry> ApplyOperationalSourceFilter(
        IQueryable<JournalEntry> query,
        JournalReportType type)
    {
        return type switch
        {
            JournalReportType.Revenue => query.Where(e => e.Source == JournalSource.Revenue),
            JournalReportType.Expense => query.Where(e => e.Source == JournalSource.Expense),
            JournalReportType.Production => query.Where(e => e.Source == JournalSource.Production),
            JournalReportType.General => query.Where(e =>
                e.Source == JournalSource.Manual
                || (e.Source != JournalSource.PurchaseInvoice
                    && e.Source != JournalSource.SaleInvoice
                    && e.Source != JournalSource.Expense
                    && e.Source != JournalSource.Revenue
                    && e.Source != JournalSource.Production)),
            _ => throw new InvalidOperationException("نوع روزنامچه عملیاتی نامعتبر است."),
        };
    }

    private static string GetOperationalReportTitle(JournalReportType type) => type switch
    {
        JournalReportType.Revenue => "روزنامچه عواید",
        JournalReportType.Expense => "روزنامچه مصارف",
        JournalReportType.Production => "روزنامچه تولید",
        JournalReportType.General => "روزنامچه عمومی",
        _ => "روزنامچه",
    };

    private JournalReportInfo BuildInfo(GeneralSettings settings, string reportTitle, DateTime? dateFrom, DateTime? dateTo)
    {
        var zmLogoWebPath = string.IsNullOrWhiteSpace(settings.ZmLogoPath) ? DefaultZmLogoWebPath : settings.ZmLogoPath;
        var reportRangeDate = (dateFrom, dateTo) switch
        {
            ({ } from, { } to) => $"از {JalaliDateHelper.FormatDate(from)} تا {JalaliDateHelper.FormatDate(to)}",
            ({ } from, null) => $"از {JalaliDateHelper.FormatDate(from)} تا انتها",
            (null, { } to) => $"از ابتدا تا {JalaliDateHelper.FormatDate(to)}",
            _ => "همه دوره",
        };

        return new JournalReportInfo
        {
            PersianCompanyName = settings.PersianCompanyName,
            EnglishCompanyName = settings.EnglishCompanyName,
            ZmLogo = ResolveLogoPath(zmLogoWebPath),
            CompanyLogo = ResolveLogoPath(settings.CompanyLogoPath),
            PrintDate = JalaliDateHelper.FormatDate(DateTime.Now),
            ReportTitle = reportTitle,
            ReportRangeDate = reportRangeDate,
        };
    }

    private static JournalReportProduct MapProductRow(
        JournalInvoiceItemRow row,
        int rowNumber,
        string? returnDescription)
    {
        var unitPriceInBase = row.Quantity > 0
            ? row.LineTotalInBaseCurrency / row.Quantity
            : 0m;

        var descriptionParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(returnDescription))
        {
            descriptionParts.Add(returnDescription);
        }

        if (row.IsMultiCurrency)
        {
            descriptionParts.Add($"قیمت: {FormatMoney(row.UnitPrice, row.InvoiceSymbol)}");
            descriptionParts.Add($"جمع: {FormatMoney(row.LineTotal, row.InvoiceSymbol)}");
        }

        return new JournalReportProduct
        {
            InvoiceNumber = row.InvoiceNumber,
            ProductName = FormatProductDesc(row.ProductName, row.ProductCode),
            ProductQTY = row.Quantity,
            ProductPrice = FormatMoney(unitPriceInBase, row.BaseSymbol),
            SubTotal = FormatMoney(row.LineTotalInBaseCurrency, row.BaseSymbol),
            Description = descriptionParts.Count > 0 ? string.Join(" — ", descriptionParts) : string.Empty,
            ShamsiDate = JalaliDateHelper.FormatDate(row.InvoiceDate),
            RowNumber = rowNumber,
        };
    }

    private StiReport BuildReport(JournalReportInfo info, IReadOnlyList<JournalReportProduct> products)
    {
        var reportPath = Path.Combine(_env.ContentRootPath, "Reports", "Jurnal.mrt");
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("فایل گزارش روزنامچه یافت نشد.", reportPath);
        }

        var report = new StiReport();
        report.Load(reportPath);
        report.RegBusinessObject("Info", info);
        report.RegBusinessObject("Products", products);
        report.Dictionary.Synchronize();
        ApplyReportImages(report, info);
        report.Compile();
        ReportFontHelper.ApplyNotoNastaliqSemiBold(report, _env, "Text1", 14F);
        report.Render();
        return report;
    }

    private static void ApplyReportImages(StiReport report, JournalReportInfo info)
    {
        // در mrt نام Imageها با فیلدهای Info جابجا شده: CompanyLogo ← ZmLogo ، ZmLogo ← CompanyLogo
        SetReportImage(report, "CompanyLogo", info.ZmLogo);
        SetReportImage(report, "ZmLogo", info.CompanyLogo);
    }

    [SupportedOSPlatform("windows")]
    private static void SetReportImage(StiReport report, string componentName, string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return;
        }

        if (report.GetComponentByName(componentName) is not StiImage image)
        {
            return;
        }

        using var stream = new MemoryStream(File.ReadAllBytes(path));
        image.Image = Image.FromStream(stream);
    }

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

    private sealed class OperationalJournalRow
    {
        public string EntryNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public decimal AmountInBase { get; set; }
        public int Source { get; set; }
    }

    private sealed class JournalInvoiceItemRow
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? ProductCode { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }
        public decimal LineTotalInBaseCurrency { get; set; }
        public DateTime InvoiceDate { get; set; }
        public string InvoiceSymbol { get; set; } = string.Empty;
        public string BaseSymbol { get; set; } = string.Empty;
        public bool IsMultiCurrency { get; set; }
        public InvoiceDocumentType DocumentType { get; set; }
        public PurchaseEntrySource EntrySource { get; set; }
        public PurchaseEntrySource? ReferenceEntrySource { get; set; }
    }
}

public class JournalReportInfo
{
    public string CompanyLogo { get; set; } = string.Empty;
    public string EnglishCompanyName { get; set; } = string.Empty;
    public string PersianCompanyName { get; set; } = string.Empty;
    public string ZmLogo { get; set; } = string.Empty;
    public string PrintDate { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public string ReportRangeDate { get; set; } = string.Empty;
}

public class JournalReportProduct
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal ProductQTY { get; set; }
    public string ProductPrice { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShamsiDate { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public string SubTotal { get; set; } = string.Empty;
}
