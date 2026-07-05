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

    private readonly IInvoiceReportService _invoiceReports;
    private readonly IJournalReportService _journalReports;

    public ReportViewerController(
        IInvoiceReportService invoiceReports,
        IJournalReportService journalReports)
    {
        _invoiceReports = invoiceReports;
        _journalReports = journalReports;
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
        HttpContext.Session.SetInt32(SaleInvoiceSessionKey, saleInvoiceId);
        HttpContext.Session.Remove(PurchaseInvoiceSessionKey);
        return View("Invoice");
    }

    [HttpGet]
    public IActionResult Journal(string type, string dateFrom, string dateTo)
    {
        if (!ReportInputHelper.TryParseReportDate(dateFrom, out var fromDate) ||
            !ReportInputHelper.TryParseReportDate(dateTo, out var toDate))
        {
            return BadRequest("بازه تاریخ نامعتبر است.");
        }

        if (fromDate.Date > toDate.Date)
        {
            return BadRequest("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        }

        if (!TryParseJournalType(type, out var journalType))
        {
            return BadRequest("نوع روزنامچه نامعتبر است.");
        }

        if (journalType is not (JournalReportType.Purchase or JournalReportType.Sale))
        {
            return BadRequest("این نوع روزنامچه هنوز پیاده‌سازی نشده است.");
        }

        HttpContext.Session.Remove(PurchaseInvoiceSessionKey);
        HttpContext.Session.Remove(SaleInvoiceSessionKey);
        HttpContext.Session.SetString(JournalReportTypeSessionKey, journalType.ToString());
        HttpContext.Session.SetString(JournalDateFromSessionKey, fromDate.Date.ToString("O"));
        HttpContext.Session.SetString(JournalDateToSessionKey, toDate.Date.ToString("O"));

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

    public async Task<IActionResult> GetJournalReport(CancellationToken cancellationToken)
    {
        var typeValue = HttpContext.Session.GetString(JournalReportTypeSessionKey);
        var dateFromValue = HttpContext.Session.GetString(JournalDateFromSessionKey);
        var dateToValue = HttpContext.Session.GetString(JournalDateToSessionKey);

        if (string.IsNullOrWhiteSpace(typeValue) ||
            !DateTime.TryParse(dateFromValue, out var dateFrom) ||
            !DateTime.TryParse(dateToValue, out var dateTo) ||
            !Enum.TryParse<JournalReportType>(typeValue, out var journalType))
        {
            return BadRequest("پارامترهای گزارش روزنامچه مشخص نشده است.");
        }

        try
        {
            var report = journalType switch
            {
                JournalReportType.Purchase => await _journalReports.BuildPurchaseJournalReportAsync(dateFrom, dateTo, cancellationToken),
                JournalReportType.Sale => await _journalReports.BuildSaleJournalReportAsync(dateFrom, dateTo, cancellationToken),
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
        return StiNetCoreViewer.ViewerEventResult(this);
    }

    public IActionResult JournalViewerEvent()
    {
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
