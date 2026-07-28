using HamgamCementWeb.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;

namespace HamgamCementWeb.Server.Controllers.Reports;

[Authorize]
public class ReportViewerController : Controller
{
    private const string PurchaseInvoiceSessionKey = "ReportPurchaseInvoiceId";
    private const string SaleInvoiceSessionKey = "ReportSaleInvoiceId";
    private const string JournalReportTypeSessionKey = "JournalReportType";
    private const string JournalDateFromSessionKey = "JournalDateFrom";
    private const string JournalDateToSessionKey = "JournalDateTo";
    private const string ProductsCategoryIdSessionKey = "ReportProductsCategoryId";
    private const string ProductsActiveOnlySessionKey = "ReportProductsActiveOnly";
    private const string ProductsBelowMinStockSessionKey = "ReportProductsBelowMinStock";

    private readonly IInvoiceReportService _invoiceReports;
    private readonly IJournalReportService _journalReports;
    private readonly IProductReportService _productReports;
    private readonly IWebHostEnvironment _env;

    public ReportViewerController(
        IInvoiceReportService invoiceReports,
        IJournalReportService journalReports,
        IProductReportService productReports,
        IWebHostEnvironment env)
    {
        _invoiceReports = invoiceReports;
        _journalReports = journalReports;
        _productReports = productReports;
        _env = env;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult GetReport()
    {
        var report = new StiReport();
        report.Load(StiNetCoreHelper.MapPath(this, "Reports/Report.mrt"));
        return StiNetCoreViewer.GetReportResult(this, report);
    }

    [HttpGet]
    public IActionResult Invoice(int purchaseInvoiceId)
    {
        if (purchaseInvoiceId <= 0)
        {
            return BadRequest("شناسه فاکتور نامعتبر است.");
        }

        ClearJournalSession();
        ClearProductsSession();
        HttpContext.Session.SetInt32(PurchaseInvoiceSessionKey, purchaseInvoiceId);
        HttpContext.Session.Remove(SaleInvoiceSessionKey);
        return View();
    }

    [HttpGet]
    public IActionResult SaleInvoice(int saleInvoiceId)
    {
        if (saleInvoiceId <= 0)
        {
            return BadRequest("شناسه فاکتور نامعتبر است.");
        }

        ClearJournalSession();
        ClearProductsSession();
        HttpContext.Session.SetInt32(SaleInvoiceSessionKey, saleInvoiceId);
        HttpContext.Session.Remove(PurchaseInvoiceSessionKey);
        return View("Invoice");
    }

    [HttpGet]
    public IActionResult Journal(string type, string? dateFrom, string? dateTo)
    {
        var hasFrom = ReportInputHelper.TryParseReportDate(dateFrom, out var parsedFrom);
        var hasTo = ReportInputHelper.TryParseReportDate(dateTo, out var parsedTo);

        DateTime? fromDate = hasFrom ? parsedFrom : null;
        DateTime? toDate = hasTo ? parsedTo : null;

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return BadRequest("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        }

        if (!TryParseJournalType(type, out var journalType))
        {
            return BadRequest("نوع روزنامچه نامعتبر است.");
        }

        if (journalType is not (JournalReportType.Purchase or JournalReportType.Sale))
        {
            if (!fromDate.HasValue || !toDate.HasValue)
            {
                return BadRequest("بازه تاریخ نامعتبر است.");
            }
        }

        HttpContext.Session.Remove(PurchaseInvoiceSessionKey);
        HttpContext.Session.Remove(SaleInvoiceSessionKey);
        ClearProductsSession();
        HttpContext.Session.SetString(JournalReportTypeSessionKey, journalType.ToString());
        HttpContext.Session.SetString(
            JournalDateFromSessionKey,
            fromDate?.Date.ToString("O") ?? string.Empty);
        HttpContext.Session.SetString(
            JournalDateToSessionKey,
            toDate?.Date.ToString("O") ?? string.Empty);

        return View();
    }

    public async Task<IActionResult> GetInvoiceReport(CancellationToken cancellationToken)
    {
        var saleInvoiceId = HttpContext.Session.GetInt32(SaleInvoiceSessionKey);
        if (saleInvoiceId is > 0)
        {
            try
            {
                var saleReport = await _invoiceReports.BuildSalesInvoiceReportAsync(saleInvoiceId.Value, cancellationToken);
                return StiNetCoreViewer.GetReportResult(this, saleReport);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
        }

        var purchaseInvoiceId = HttpContext.Session.GetInt32(PurchaseInvoiceSessionKey);
        if (purchaseInvoiceId is not > 0)
        {
            return BadRequest("فاکتور برای چاپ مشخص نشده است.");
        }

        try
        {
            var report = await _invoiceReports.BuildPurchaseInvoiceReportAsync(purchaseInvoiceId.Value, cancellationToken);
            return StiNetCoreViewer.GetReportResult(this, report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet]
    public IActionResult Products(int? categoryId, string? activeOnly, bool belowMinStock = false)
    {
        ClearJournalSession();
        HttpContext.Session.Remove(PurchaseInvoiceSessionKey);
        HttpContext.Session.Remove(SaleInvoiceSessionKey);

        if (categoryId is > 0)
        {
            HttpContext.Session.SetInt32(ProductsCategoryIdSessionKey, categoryId.Value);
        }
        else
        {
            HttpContext.Session.Remove(ProductsCategoryIdSessionKey);
        }

        if (string.Equals(activeOnly, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(activeOnly, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(activeOnly, "active", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetString(ProductsActiveOnlySessionKey, "true");
        }
        else if (string.Equals(activeOnly, "false", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(activeOnly, "0", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(activeOnly, "inactive", StringComparison.OrdinalIgnoreCase))
        {
            HttpContext.Session.SetString(ProductsActiveOnlySessionKey, "false");
        }
        else
        {
            HttpContext.Session.Remove(ProductsActiveOnlySessionKey);
        }

        HttpContext.Session.SetString(
            ProductsBelowMinStockSessionKey,
            belowMinStock ? "true" : "false");

        return View();
    }

    public async Task<IActionResult> GetProductsReport(CancellationToken cancellationToken)
    {
        var categoryId = HttpContext.Session.GetInt32(ProductsCategoryIdSessionKey);
        var activeOnlyValue = HttpContext.Session.GetString(ProductsActiveOnlySessionKey);
        var belowMinStockValue = HttpContext.Session.GetString(ProductsBelowMinStockSessionKey);

        bool? activeOnly = activeOnlyValue switch
        {
            "true" => true,
            "false" => false,
            _ => null,
        };
        var belowMinStock = string.Equals(belowMinStockValue, "true", StringComparison.OrdinalIgnoreCase);

        try
        {
            var report = await _productReports.BuildProductsReportAsync(
                categoryId,
                activeOnly,
                belowMinStock,
                cancellationToken);
            return StiNetCoreViewer.GetReportResult(this, report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    public async Task<IActionResult> GetJournalReport(CancellationToken cancellationToken)
    {
        var typeValue = HttpContext.Session.GetString(JournalReportTypeSessionKey);
        var dateFromValue = HttpContext.Session.GetString(JournalDateFromSessionKey);
        var dateToValue = HttpContext.Session.GetString(JournalDateToSessionKey);

        if (string.IsNullOrWhiteSpace(typeValue) ||
            !Enum.TryParse<JournalReportType>(typeValue, out var journalType))
        {
            return BadRequest("پارامترهای گزارش روزنامچه مشخص نشده است.");
        }

        DateTime? dateFrom = null;
        DateTime? dateTo = null;

        if (!string.IsNullOrWhiteSpace(dateFromValue))
        {
            if (!DateTime.TryParse(dateFromValue, out var parsedFrom))
            {
                return BadRequest("پارامترهای گزارش روزنامچه مشخص نشده است.");
            }

            dateFrom = parsedFrom;
        }

        if (!string.IsNullOrWhiteSpace(dateToValue))
        {
            if (!DateTime.TryParse(dateToValue, out var parsedTo))
            {
                return BadRequest("پارامترهای گزارش روزنامچه مشخص نشده است.");
            }

            dateTo = parsedTo;
        }

        if (dateFrom.HasValue && dateTo.HasValue && dateFrom.Value.Date > dateTo.Value.Date)
        {
            return BadRequest("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        }

        try
        {
            var report = journalType switch
            {
                JournalReportType.Purchase => await _journalReports.BuildPurchaseJournalReportAsync(dateFrom, dateTo, cancellationToken),
                JournalReportType.Sale => await _journalReports.BuildSaleJournalReportAsync(dateFrom, dateTo, cancellationToken),
                JournalReportType.Revenue or JournalReportType.Expense or JournalReportType.Production or JournalReportType.General
                    => await _journalReports.BuildOperationalJournalReportAsync(journalType, dateFrom, dateTo, cancellationToken),
                _ => throw new InvalidOperationException("این نوع روزنامچه هنوز پیاده‌سازی نشده است."),
            };

            return StiNetCoreViewer.GetReportResult(this, report);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    public IActionResult InvoiceViewerEvent()
    {
        // قبل از Export/Print به PDF فونت و تنظیمات embed دوباره تضمین شود
        ReportFontHelper.ConfigurePdfExportDefaults();
        ReportFontHelper.EnsureNotoNastaliqRegistered(_env);
        return StiNetCoreViewer.ViewerEventResult(this);
    }

    public IActionResult JournalViewerEvent()
    {
        ReportFontHelper.ConfigurePdfExportDefaults();
        ReportFontHelper.EnsureNotoNastaliqRegistered(_env);
        return StiNetCoreViewer.ViewerEventResult(this);
    }

    public IActionResult ProductsViewerEvent()
    {
        ReportFontHelper.ConfigurePdfExportDefaults();
        ReportFontHelper.EnsureNotoNastaliqRegistered(_env);
        return StiNetCoreViewer.ViewerEventResult(this);
    }

    public IActionResult ViewerEvent()
    {
        return StiNetCoreViewer.ViewerEventResult(this);
    }

    private void ClearJournalSession()
    {
        HttpContext.Session.Remove(JournalReportTypeSessionKey);
        HttpContext.Session.Remove(JournalDateFromSessionKey);
        HttpContext.Session.Remove(JournalDateToSessionKey);
    }

    private void ClearProductsSession()
    {
        HttpContext.Session.Remove(ProductsCategoryIdSessionKey);
        HttpContext.Session.Remove(ProductsActiveOnlySessionKey);
        HttpContext.Session.Remove(ProductsBelowMinStockSessionKey);
    }

    private static bool TryParseJournalType(string? type, out JournalReportType journalType)
    {
        journalType = default;

        if (string.IsNullOrWhiteSpace(type))
        {
            return false;
        }

        return type.Trim().ToLowerInvariant() switch
        {
            "purchase" or "buy" or "خرید" => Assign(JournalReportType.Purchase, out journalType),
            "sale" or "sales" or "فروش" => Assign(JournalReportType.Sale, out journalType),
            "revenue" or "revenues" or "عواید" => Assign(JournalReportType.Revenue, out journalType),
            "expense" or "expenses" or "مصارف" => Assign(JournalReportType.Expense, out journalType),
            "production" or "تولید" => Assign(JournalReportType.Production, out journalType),
            "general" or "عمومی" => Assign(JournalReportType.General, out journalType),
            _ => Enum.TryParse(type, true, out journalType),
        };

        static bool Assign(JournalReportType value, out JournalReportType result)
        {
            result = value;
            return true;
        }
    }
}
