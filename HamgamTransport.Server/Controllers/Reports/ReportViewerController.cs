using HamgamTransport.Server.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stimulsoft.Report;
using Stimulsoft.Report.Mvc;

namespace HamgamTransport.Server.Controllers.Reports;

[Authorize]
public class ReportViewerController : Controller
{
    private const string JournalReportTypeSessionKey = "JournalReportType";
    private const string JournalDateFromSessionKey = "JournalDateFrom";
    private const string JournalDateToSessionKey = "JournalDateTo";

    private readonly IJournalReportService _journalReports;
    private readonly IWebHostEnvironment _env;

    public ReportViewerController(
        IJournalReportService journalReports,
        IWebHostEnvironment env)
    {
        _journalReports = journalReports;
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
    public async Task<IActionResult> Journal(string type, string? dateFrom, string? dateTo, CancellationToken cancellationToken)
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

        if (journalType is not (
            JournalReportType.General or
            JournalReportType.Revenue or
            JournalReportType.Expense or
            JournalReportType.Transport))
        {
            return BadRequest("این نوع روزنامچه در سیستم ترانسپورت پشتیبانی نمی‌شود.");
        }

        try
        {
            var model = await _journalReports.BuildFilteredJournalPrintModelAsync(
                journalType,
                fromDate,
                toDate,
                cancellationToken);
            return View("StandardJournal", model);
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
    public async Task<IActionResult> Ledger(
        int accountId,
        string? dateFrom,
        string? dateTo,
        int? partyId,
        CancellationToken cancellationToken)
    {
        if (accountId <= 0)
        {
            return BadRequest("شناسه حساب نامعتبر است.");
        }

        var hasFrom = ReportInputHelper.TryParseReportDate(dateFrom, out var parsedFrom);
        var hasTo = ReportInputHelper.TryParseReportDate(dateTo, out var parsedTo);
        DateTime? fromDate = hasFrom ? parsedFrom : null;
        DateTime? toDate = hasTo ? parsedTo : null;

        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
        {
            return BadRequest("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        }

        try
        {
            var model = await _journalReports.BuildAccountLedgerPrintModelAsync(
                accountId,
                fromDate,
                toDate,
                partyId,
                cancellationToken);
            return View("AccountLedger", model);
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
                JournalReportType.General => await _journalReports.BuildStandardGeneralJournalReportAsync(dateFrom, dateTo, cancellationToken),
                JournalReportType.Revenue or JournalReportType.Expense or JournalReportType.Transport
                    => await _journalReports.BuildOperationalJournalReportAsync(journalType, dateFrom, dateTo, cancellationToken),
                _ => throw new InvalidOperationException("این نوع روزنامچه در سیستم ترانسپورت پشتیبانی نمی‌شود."),
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

    public IActionResult JournalViewerEvent()
    {
        ReportFontHelper.ConfigurePdfExportDefaults();
        ReportFontHelper.EnsureNotoNastaliqRegistered(_env);
        return StiNetCoreViewer.ViewerEventResult(this);
    }

    public IActionResult ViewerEvent()
    {
        return StiNetCoreViewer.ViewerEventResult(this);
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
            "revenue" or "revenues" or "عواید" => Assign(JournalReportType.Revenue, out journalType),
            "expense" or "expenses" or "مصارف" => Assign(JournalReportType.Expense, out journalType),
            "transport" or "trip" or "حمل" or "سرویس" => Assign(JournalReportType.Transport, out journalType),
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
