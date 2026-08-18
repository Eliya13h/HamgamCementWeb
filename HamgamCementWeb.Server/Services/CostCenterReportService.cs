using Dapper;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public interface ICostCenterReportService
{
    Task<CostCenterReportPrintModel> BuildPrintModelAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        int? costCenterId = null,
        int? accountId = null,
        CancellationToken cancellationToken = default);
}

public class CostCenterReportService : ICostCenterReportService
{
    private const int GeneralSettingsId = 1;
    private const string DefaultZmLogoWebPath = "/zm_logo.jpg";

    private readonly AppDbContext _db;
    private readonly ISqlConnectionFactory _sql;
    private readonly IWebHostEnvironment _env;

    public CostCenterReportService(AppDbContext db, ISqlConnectionFactory sql, IWebHostEnvironment env)
    {
        _db = db;
        _sql = sql;
        _env = env;
    }

    public async Task<CostCenterReportPrintModel> BuildPrintModelAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        int? costCenterId = null,
        int? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var solarYear = JalaliDateHelper.GetSolarYear(today);
        var (yearStart, _) = JalaliDateHelper.GetSolarYearRange(solarYear);
        var start = (dateFrom ?? yearStart).Date;
        var end = (dateTo ?? today).Date;
        if (start > end)
        {
            throw new InvalidOperationException("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        }

        var endInclusive = end.AddDays(1).AddTicks(-1);

        string? costCenterFilterLabel = null;
        if (costCenterId is > 0)
        {
            costCenterFilterLabel = await _db.CostCenters.AsNoTracking()
                .Where(c => c.CostCenterID == costCenterId && c.IsDeleted != true)
                .Select(c => c.Code + " — " + c.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("مرکز هزینه یافت نشد.");
        }

        string? accountFilterLabel = null;
        if (accountId is > 0)
        {
            accountFilterLabel = await _db.Accounts.AsNoTracking()
                .Where(a => a.AccountID == accountId && a.IsDeleted != true)
                .Select(a => a.Code + " — " + a.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("حساب یافت نشد.");
        }

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var summary = (await connection.QueryAsync<CostCenterReportSummaryRow>(
            """
            SELECT cc.CostCenterID AS CostCenterId,
                   cc.Code AS Code,
                   cc.Name AS Name,
                   ISNULL(SUM(jl.DebitInBaseCurrency), 0) AS Debit,
                   ISNULL(SUM(jl.CreditInBaseCurrency), 0) AS Credit
            FROM JournalLines jl
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            INNER JOIN CostCenters cc ON cc.CostCenterID = jl.CostCenterId
            WHERE ISNULL(jl.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND jl.CostCenterId IS NOT NULL
              AND ISNULL(cc.IsDeleted, 0) = 0
              AND je.EntryDate >= @Start
              AND je.EntryDate <= @EndInclusive
              AND (@CostCenterId IS NULL OR jl.CostCenterId = @CostCenterId)
              AND (@AccountId IS NULL OR jl.AccountId = @AccountId)
            GROUP BY cc.CostCenterID, cc.Code, cc.Name
            ORDER BY cc.Code
            """,
            new { Start = start, EndInclusive = endInclusive, CostCenterId = costCenterId, AccountId = accountId })).AsList();

        foreach (var row in summary)
        {
            row.Net = row.Debit - row.Credit;
        }

        var details = (await connection.QueryAsync<(
            DateTime EntryDate,
            string EntryNumber,
            string AccountCode,
            string AccountName,
            string? LineDescription,
            string? EntryDescription,
            string CostCenterCode,
            string CostCenterName,
            decimal Debit,
            decimal Credit)>(
            """
            SELECT je.EntryDate AS EntryDate,
                   je.EntryNumber AS EntryNumber,
                   a.Code AS AccountCode,
                   a.Name AS AccountName,
                   jl.Description AS LineDescription,
                   je.Description AS EntryDescription,
                   cc.Code AS CostCenterCode,
                   cc.Name AS CostCenterName,
                   jl.DebitInBaseCurrency AS Debit,
                   jl.CreditInBaseCurrency AS Credit
            FROM JournalLines jl
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            INNER JOIN Accounts a ON a.AccountID = jl.AccountId
            INNER JOIN CostCenters cc ON cc.CostCenterID = jl.CostCenterId
            WHERE ISNULL(jl.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND jl.CostCenterId IS NOT NULL
              AND ISNULL(cc.IsDeleted, 0) = 0
              AND je.EntryDate >= @Start
              AND je.EntryDate <= @EndInclusive
              AND (@CostCenterId IS NULL OR jl.CostCenterId = @CostCenterId)
              AND (@AccountId IS NULL OR jl.AccountId = @AccountId)
            ORDER BY je.EntryDate, je.JournalEntryID, jl.[LineNo]
            """,
            new { Start = start, EndInclusive = endInclusive, CostCenterId = costCenterId, AccountId = accountId })).AsList();

        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var title = "گزارش مراکز هزینه";
        var range = $"از {JalaliDateHelper.FormatDate(start)} تا {JalaliDateHelper.FormatDate(end)}";
        var zmLogoWebPath = string.IsNullOrWhiteSpace(settings.ZmLogoPath) ? DefaultZmLogoWebPath : settings.ZmLogoPath;

        return new CostCenterReportPrintModel
        {
            PersianCompanyName = settings.PersianCompanyName,
            EnglishCompanyName = settings.EnglishCompanyName,
            ReportTitle = title,
            ReportRangeDate = range,
            PrintDate = JalaliDateHelper.FormatDate(DateTime.Now),
            CompanyLogoDataUri = ToImageDataUri(ResolveLogoPath(settings.CompanyLogoPath)),
            ZmLogoDataUri = ToImageDataUri(ResolveLogoPath(zmLogoWebPath)),
            CostCenterFilterLabel = costCenterFilterLabel,
            AccountFilterLabel = accountFilterLabel,
            Summary = summary,
            Details = details.Select(d => new CostCenterReportDetailRow
            {
                ShamsiDate = JalaliDateHelper.FormatDate(d.EntryDate),
                EntryNumber = d.EntryNumber,
                AccountLabel = $"{d.AccountCode} — {d.AccountName}",
                CostCenterLabel = $"{d.CostCenterCode} — {d.CostCenterName}",
                Description = !string.IsNullOrWhiteSpace(d.LineDescription)
                    ? d.LineDescription!
                    : (d.EntryDescription ?? string.Empty),
                Debit = d.Debit,
                Credit = d.Credit,
            }).ToList(),
            TotalDebit = summary.Sum(s => s.Debit),
            TotalCredit = summary.Sum(s => s.Credit),
            TotalNet = summary.Sum(s => s.Net),
        };
    }

    private string? ResolveLogoPath(string? webPath)
    {
        if (string.IsNullOrWhiteSpace(webPath)) return null;
        var relative = webPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var www = Path.Combine(_env.WebRootPath ?? string.Empty, relative);
        if (File.Exists(www)) return www;
        var content = Path.Combine(_env.ContentRootPath, relative);
        return File.Exists(content) ? content : null;
    }

    private static string? ToImageDataUri(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) return null;
        var bytes = File.ReadAllBytes(path);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        var mime = ext switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".svg" => "image/svg+xml",
            _ => "image/jpeg",
        };
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }
}

public class CostCenterReportPrintModel
{
    public string PersianCompanyName { get; set; } = string.Empty;
    public string EnglishCompanyName { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public string ReportRangeDate { get; set; } = string.Empty;
    public string PrintDate { get; set; } = string.Empty;
    public string? CompanyLogoDataUri { get; set; }
    public string? ZmLogoDataUri { get; set; }
    public string? CostCenterFilterLabel { get; set; }
    public string? AccountFilterLabel { get; set; }
    public List<CostCenterReportSummaryRow> Summary { get; set; } = [];
    public List<CostCenterReportDetailRow> Details { get; set; } = [];
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public decimal TotalNet { get; set; }
}

public class CostCenterReportSummaryRow
{
    public int CostCenterId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal Net { get; set; }
}

public class CostCenterReportDetailRow
{
    public string ShamsiDate { get; set; } = string.Empty;
    public string EntryNumber { get; set; } = string.Empty;
    public string AccountLabel { get; set; } = string.Empty;
    public string CostCenterLabel { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
}
