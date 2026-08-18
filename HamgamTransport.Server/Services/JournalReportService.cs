using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;

namespace HamgamTransport.Server.Services;

public enum JournalReportType
{
    Purchase,
    Sale,
    Revenue,
    Expense,
    Production,
    General,
    // سرویس‌ها و هزینه‌های سفر ترانسپورت
    Transport,
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

    // دفتر روزنامه استاندارد (دابل‌انتری) — روزنامچه عمومی
    Task<StiReport> BuildStandardGeneralJournalReportAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    // نسخه چاپ HTML (A4) برای روزنامچه عمومی / عواید / مصارف / حمل
    Task<StandardJournalPrintModel> BuildStandardJournalPrintModelAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    Task<StandardJournalPrintModel> BuildFilteredJournalPrintModelAsync(
        JournalReportType type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    // چاپ HTML دفتر کل یک حساب
    Task<AccountLedgerPrintModel> BuildAccountLedgerPrintModelAsync(
        int accountId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? partyId,
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
        throw new NotSupportedException("روزنامچه خرید در سیستم ترانسپورت پشتیبانی نمی‌شود.");
    }

    public Task<StiReport> BuildSaleJournalReportAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("روزنامچه فروش در سیستم ترانسپورت پشتیبانی نمی‌شود.");
    }

    public Task<StiReport> BuildStandardGeneralJournalReportAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        return BuildStandardJournalReportAsync(dateFrom, dateTo, cancellationToken);
    }

    public async Task<StandardJournalPrintModel> BuildStandardJournalPrintModelAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        return await BuildFilteredJournalPrintModelAsync(
            JournalReportType.General,
            dateFrom,
            dateTo,
            cancellationToken);
    }

    public async Task<StandardJournalPrintModel> BuildFilteredJournalPrintModelAsync(
        JournalReportType type,
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.IsDeleted != true)
            .Select(a => new AccountCodeNode
            {
                AccountId = a.AccountID,
                Code = a.Code,
                Name = a.Name,
                Level = a.Level,
                ParentAccountId = a.ParentAccountId,
            })
            .ToListAsync(cancellationToken);

        var accountMap = accounts.ToDictionary(a => a.AccountId);
        var sourceFilter = ResolveSourceFilter(type);
        var opening = await LoadOpeningBalanceAsync(dateFrom, sourceFilter, cancellationToken);
        var entries = await LoadStandardJournalEntriesAsync(dateFrom, dateTo, sourceFilter, cancellationToken);
        var rows = MapStandardJournalRows(entries, accountMap);
        var info = BuildInfo(settings, GetHtmlJournalReportTitle(type), dateFrom, dateTo);

        return new StandardJournalPrintModel
        {
            PersianCompanyName = info.PersianCompanyName,
            EnglishCompanyName = info.EnglishCompanyName,
            ReportTitle = info.ReportTitle,
            ReportRangeDate = info.ReportRangeDate,
            PrintDate = info.PrintDate,
            CompanyLogoDataUri = ToImageDataUri(info.CompanyLogo),
            ZmLogoDataUri = ToImageDataUri(info.ZmLogo),
            OpeningDebit = opening.Debit,
            OpeningCredit = opening.Credit,
            Pages = BuildPrintPages(rows, opening.Debit, opening.Credit),
        };
    }

    public async Task<AccountLedgerPrintModel> BuildAccountLedgerPrintModelAsync(
        int accountId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? partyId,
        CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.AccountID == accountId && a.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("حساب یافت نشد.");

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

        var openingQuery = _db.JournalLines
            .AsNoTracking()
            .Where(l =>
                l.AccountId == accountId &&
                l.IsDeleted != true &&
                l.JournalEntry.IsDeleted != true &&
                l.JournalEntry.IsPosted &&
                l.JournalEntry.EntryDate < start);
        if (partyId is > 0)
        {
            openingQuery = openingQuery.Where(l => l.PartyId == partyId);
        }

        var openingDebit = await openingQuery.SumAsync(l => l.DebitInBaseCurrency, cancellationToken);
        var openingCredit = await openingQuery.SumAsync(l => l.CreditInBaseCurrency, cancellationToken);
        var openingBalance = openingDebit - openingCredit;

        var linesQuery = _db.JournalLines
            .AsNoTracking()
            .Where(l =>
                l.AccountId == accountId &&
                l.IsDeleted != true &&
                l.JournalEntry.IsDeleted != true &&
                l.JournalEntry.IsPosted &&
                l.JournalEntry.EntryDate >= start &&
                l.JournalEntry.EntryDate <= endInclusive);
        if (partyId is > 0)
        {
            linesQuery = linesQuery.Where(l => l.PartyId == partyId);
        }

        var lines = await linesQuery
            .OrderBy(l => l.JournalEntry.EntryDate)
            .ThenBy(l => l.JournalEntry.JournalEntryID)
            .ThenBy(l => l.LineNo)
            .Select(l => new
            {
                l.JournalEntry.EntryNumber,
                l.JournalEntry.EntryDate,
                EntryDescription = l.JournalEntry.Description,
                LineDescription = l.Description,
                Debit = l.DebitInBaseCurrency,
                Credit = l.CreditInBaseCurrency,
            })
            .ToListAsync(cancellationToken);

        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var info = BuildInfo(
            settings,
            $"دفتر کل — {account.Code} {account.Name}",
            start,
            end);

        var running = openingBalance;
        var rows = new List<AccountLedgerPrintRow>();
        foreach (var line in lines)
        {
            running += line.Debit - line.Credit;
            var desc = !string.IsNullOrWhiteSpace(line.LineDescription)
                ? line.LineDescription!
                : (line.EntryDescription ?? string.Empty);
            rows.Add(new AccountLedgerPrintRow
            {
                ShamsiDate = JalaliDateHelper.FormatDate(line.EntryDate),
                EntryNumber = line.EntryNumber,
                Description = desc,
                Debit = line.Debit,
                Credit = line.Credit,
                RunningBalance = running,
            });
        }

        return new AccountLedgerPrintModel
        {
            PersianCompanyName = info.PersianCompanyName,
            EnglishCompanyName = info.EnglishCompanyName,
            ReportTitle = info.ReportTitle,
            ReportRangeDate = info.ReportRangeDate,
            PrintDate = info.PrintDate,
            CompanyLogoDataUri = ToImageDataUri(info.CompanyLogo),
            ZmLogoDataUri = ToImageDataUri(info.ZmLogo),
            AccountCode = account.Code,
            AccountName = account.Name,
            OpeningBalance = openingBalance,
            ClosingBalance = running,
            PeriodDebit = rows.Sum(r => r.Debit),
            PeriodCredit = rows.Sum(r => r.Credit),
            Rows = rows,
        };
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

        if (type is JournalReportType.General)
        {
            return await BuildStandardJournalReportAsync(dateFrom, dateTo, cancellationToken);
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
            JournalReportType.Transport => query.Where(e =>
                e.Source == JournalSource.TransportTrip || e.Source == JournalSource.TripExpense),
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
        JournalReportType.Transport => "روزنامچه حمل",
        JournalReportType.General => "روزنامچه عمومی",
        _ => "روزنامچه",
    };

    private async Task<StiReport> BuildStandardJournalReportAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var accounts = await _db.Accounts
            .AsNoTracking()
            .Where(a => a.IsDeleted != true)
            .Select(a => new AccountCodeNode
            {
                AccountId = a.AccountID,
                Code = a.Code,
                Name = a.Name,
                Level = a.Level,
                ParentAccountId = a.ParentAccountId,
            })
            .ToListAsync(cancellationToken);

        var accountMap = accounts.ToDictionary(a => a.AccountId);

        var opening = await LoadOpeningBalanceAsync(dateFrom, null, cancellationToken);
        var entries = await LoadStandardJournalEntriesAsync(dateFrom, dateTo, null, cancellationToken);
        var rows = MapStandardJournalRows(entries, accountMap);

        var info = BuildInfo(settings, "دفتر روزنامه عمومی", dateFrom, dateTo);
        info.OpeningDebit = opening.Debit;
        info.OpeningCredit = opening.Credit;

        return BuildStandardReport(info, rows);
    }

    private static JournalSource[]? ResolveSourceFilter(JournalReportType type) => type switch
    {
        JournalReportType.General => null,
        JournalReportType.Revenue => [JournalSource.Revenue],
        JournalReportType.Expense => [JournalSource.Expense],
        JournalReportType.Transport => [JournalSource.TransportTrip, JournalSource.TripExpense],
        JournalReportType.Production => [JournalSource.Production],
        _ => null,
    };

    private static string GetHtmlJournalReportTitle(JournalReportType type) => type switch
    {
        JournalReportType.Revenue => "دفتر روزنامه عواید",
        JournalReportType.Expense => "دفتر روزنامه مصارف",
        JournalReportType.Transport => "دفتر روزنامه حمل و سرویس",
        JournalReportType.Production => "دفتر روزنامه تولید",
        _ => "دفتر روزنامه عمومی",
    };

    private async Task<(decimal Debit, decimal Credit)> LoadOpeningBalanceAsync(
        DateTime? dateFrom,
        JournalSource[]? sourceFilter,
        CancellationToken cancellationToken)
    {
        // بدون تاریخ شروع: مانده افتتاحیه گزارش صفر است (از ابتدای دفاتر)
        if (!dateFrom.HasValue)
        {
            return (0m, 0m);
        }

        var cutoff = dateFrom.Value.Date;
        var query = _db.JournalLines
            .AsNoTracking()
            .Where(l =>
                l.IsDeleted != true &&
                l.JournalEntry.IsDeleted != true &&
                l.JournalEntry.IsPosted &&
                l.JournalEntry.EntryDate < cutoff);

        if (sourceFilter is { Length: > 0 })
        {
            query = query.Where(l => sourceFilter.Contains(l.JournalEntry.Source));
        }

        var debit = await query.SumAsync(l => l.DebitInBaseCurrency, cancellationToken);
        var credit = await query.SumAsync(l => l.CreditInBaseCurrency, cancellationToken);
        return (debit, credit);
    }

    private async Task<List<StandardJournalEntryLoad>> LoadStandardJournalEntriesAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        JournalSource[]? sourceFilter,
        CancellationToken cancellationToken)
    {
        var query = _db.JournalEntries
            .AsNoTracking()
            .Include(e => e.Lines)
            .Where(e => e.IsDeleted != true && e.IsPosted);

        if (sourceFilter is { Length: > 0 })
        {
            query = query.Where(e => sourceFilter.Contains(e.Source));
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(e => e.EntryDate >= dateFrom.Value.Date);
        }

        if (dateTo.HasValue)
        {
            var end = dateTo.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(e => e.EntryDate <= end);
        }

        var entries = await query
            .OrderBy(e => e.EntryDate)
            .ThenBy(e => e.EntryNumber)
            .ThenBy(e => e.JournalEntryID)
            .ToListAsync(cancellationToken);

        return entries.Select(e => new StandardJournalEntryLoad
        {
            JournalEntryId = e.JournalEntryID,
            EntryNumber = e.EntryNumber,
            EntryDate = e.EntryDate,
            Description = e.Description,
            Source = (int)e.Source,
            Lines = e.Lines
                .Where(l => l.IsDeleted != true)
                .Select(l => new StandardJournalLineLoad
                {
                    AccountId = l.AccountId,
                    LineNo = l.LineNo,
                    Description = l.Description,
                    DebitInBase = l.DebitInBaseCurrency,
                    CreditInBase = l.CreditInBaseCurrency,
                })
                .ToList(),
        }).ToList();
    }

    private static List<StandardJurnalRow> MapStandardJournalRows(
        IReadOnlyList<StandardJournalEntryLoad> entries,
        IReadOnlyDictionary<int, AccountCodeNode> accountMap)
    {
        var rows = new List<StandardJurnalRow>();
        var rowNumber = 0;

        foreach (var entry in entries)
        {
            var orderedLines = entry.Lines
                .OrderBy(l => l.CreditInBase > 0 && l.DebitInBase <= 0 ? 1 : 0) // دیبت‌ها اول
                .ThenBy(l => l.LineNo)
                .ToList();

            if (orderedLines.Count == 0)
            {
                continue;
            }

            rowNumber++;
            var shamsiDate = JalaliDateHelper.FormatDate(entry.EntryDate);
            var isFirst = true;

            foreach (var line in orderedLines)
            {
                accountMap.TryGetValue(line.AccountId, out var account);
                var isCredit = line.CreditInBase > 0 && line.DebitInBase <= 0;
                var accountName = account?.Name?.Trim() ?? string.Empty;
                var lineDesc = line.Description?.Trim() ?? string.Empty;
                var entryDesc = entry.Description?.Trim() ?? string.Empty;

                string description;
                if (!string.IsNullOrWhiteSpace(lineDesc))
                {
                    description = lineDesc;
                }
                else if (!string.IsNullOrWhiteSpace(accountName) && isFirst && !string.IsNullOrWhiteSpace(entryDesc))
                {
                    description = $"{accountName} — {entryDesc}";
                }
                else if (!string.IsNullOrWhiteSpace(accountName))
                {
                    description = accountName;
                }
                else
                {
                    description = entryDesc;
                }

                rows.Add(new StandardJurnalRow
                {
                    RowNumber = rowNumber,
                    ShamsiDate = shamsiDate,
                    AccountCode = ResolveKolAccountCode(line.AccountId, accountMap),
                    Description = description,
                    PostRefNumber = entry.EntryNumber,
                    Debet = line.DebitInBase,
                    Credit = line.CreditInBase,
                    IsFirstLineOfEntry = isFirst,
                    IsCredit = isCredit,
                });

                isFirst = false;
            }
        }

        return rows;
    }

    private static string ResolveKolAccountCode(
        int accountId,
        IReadOnlyDictionary<int, AccountCodeNode> accountMap)
    {
        if (!accountMap.TryGetValue(accountId, out var current))
        {
            return string.Empty;
        }

        var guard = 0;
        while (current is not null && guard++ < 16)
        {
            if (current.Level == AccountLevel.Kol)
            {
                return current.Code;
            }

            if (current.ParentAccountId is not int parentId ||
                !accountMap.TryGetValue(parentId, out current))
            {
                break;
            }
        }

        return accountMap.TryGetValue(accountId, out var fallback) ? fallback.Code : string.Empty;
    }

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

    private StiReport BuildStandardReport(JournalReportInfo info, IReadOnlyList<StandardJurnalRow> rows)
    {
        var reportPath = Path.Combine(_env.ContentRootPath, "Reports", "StandardJurnal.mrt");
        if (!File.Exists(reportPath))
        {
            throw new FileNotFoundException("فایل گزارش دفتر روزنامه یافت نشد.", reportPath);
        }

        var report = new StiReport();
        report.Load(reportPath);
        report.RegBusinessObject("Info", info);
        report.RegBusinessObject("JurnalRow", rows);
        report.Dictionary.Synchronize();
        ApplyReportImages(report, info);
        // Interpretation: از خطای Compile عبارت‌های جمع صفحه/مانده جلوگیری می‌کند
        report.CalculationMode = StiCalculationMode.Interpretation;
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

    private static string? ToImageDataUri(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

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

    private static List<StandardJournalPrintPage> BuildPrintPages(
        IReadOnlyList<StandardJurnalRow> rows,
        decimal openingDebit,
        decimal openingCredit)
    {
        // حدود ظرفیت سطر داده در صفحه A4 با سربرگ + مانده + جمع
        const int maxLinesPerPage = 28;
        var pages = new List<StandardJournalPrintPage>();
        var broughtForwardDebit = openingDebit;
        var broughtForwardCredit = openingCredit;
        var currentRows = new List<StandardJurnalRow>();
        var pageDebit = 0m;
        var pageCredit = 0m;

        void FlushPage()
        {
            pages.Add(new StandardJournalPrintPage
            {
                PageNumber = pages.Count + 1,
                BroughtForwardDebit = broughtForwardDebit,
                BroughtForwardCredit = broughtForwardCredit,
                Rows = currentRows.ToList(),
                TotalDebit = broughtForwardDebit + pageDebit,
                TotalCredit = broughtForwardCredit + pageCredit,
            });

            broughtForwardDebit += pageDebit;
            broughtForwardCredit += pageCredit;
            currentRows = [];
            pageDebit = 0m;
            pageCredit = 0m;
        }

        foreach (var entryGroup in rows.GroupBy(r => r.RowNumber))
        {
            var entryRows = entryGroup.ToList();
            if (currentRows.Count > 0 &&
                currentRows.Count + entryRows.Count > maxLinesPerPage)
            {
                FlushPage();
            }

            foreach (var row in entryRows)
            {
                if (currentRows.Count >= maxLinesPerPage)
                {
                    FlushPage();
                }

                currentRows.Add(row);
                pageDebit += row.Debet;
                pageCredit += row.Credit;
            }
        }

        if (currentRows.Count > 0 || pages.Count == 0)
        {
            FlushPage();
        }

        var totalPages = pages.Count;
        foreach (var page in pages)
        {
            page.TotalPages = totalPages;
        }

        return pages;
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
            "hamgamtransport.client",
            "public",
            fileName)));

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
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

    private sealed class StandardJournalEntryLoad
    {
        public int JournalEntryId { get; set; }
        public string EntryNumber { get; set; } = string.Empty;
        public DateTime EntryDate { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Source { get; set; }
        public List<StandardJournalLineLoad> Lines { get; set; } = [];
    }

    private sealed class StandardJournalLineLoad
    {
        public int AccountId { get; set; }
        public int LineNo { get; set; }
        public string? Description { get; set; }
        public decimal DebitInBase { get; set; }
        public decimal CreditInBase { get; set; }
    }

    private sealed class AccountCodeNode
    {
        public int AccountId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public AccountLevel Level { get; set; }
        public int? ParentAccountId { get; set; }
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
    // مانده افتتاحیه برای سطر «مانده از روز قبل» در صفحه اول دفتر روزنامه
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
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

public class StandardJurnalRow
{
    public string PostRefNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShamsiDate { get; set; } = string.Empty;
    public int RowNumber { get; set; }
    public decimal Debet { get; set; }
    public decimal Credit { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public bool IsFirstLineOfEntry { get; set; }
    public bool IsCredit { get; set; }
}

public class StandardJournalPrintModel
{
    public string PersianCompanyName { get; set; } = string.Empty;
    public string EnglishCompanyName { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public string ReportRangeDate { get; set; } = string.Empty;
    public string PrintDate { get; set; } = string.Empty;
    public string? CompanyLogoDataUri { get; set; }
    public string? ZmLogoDataUri { get; set; }
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
    public List<StandardJournalPrintPage> Pages { get; set; } = [];
}

public class StandardJournalPrintPage
{
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public decimal BroughtForwardDebit { get; set; }
    public decimal BroughtForwardCredit { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public List<StandardJurnalRow> Rows { get; set; } = [];
}

public class AccountLedgerPrintModel
{
    public string PersianCompanyName { get; set; } = string.Empty;
    public string EnglishCompanyName { get; set; } = string.Empty;
    public string ReportTitle { get; set; } = string.Empty;
    public string ReportRangeDate { get; set; } = string.Empty;
    public string PrintDate { get; set; } = string.Empty;
    public string? CompanyLogoDataUri { get; set; }
    public string? ZmLogoDataUri { get; set; }
    public string AccountCode { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public List<AccountLedgerPrintRow> Rows { get; set; } = [];
}

public class AccountLedgerPrintRow
{
    public string ShamsiDate { get; set; } = string.Empty;
    public string EntryNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}
