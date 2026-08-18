using System.Drawing;
using System.Globalization;
using System.Runtime.Versioning;
using Dapper;
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

    // دفتر روزنامه استاندارد (دابل‌انتری) — روزنامچه عمومی
    Task<StiReport> BuildStandardGeneralJournalReportAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    // نسخه چاپ HTML (A4) برای روزنامچه عمومی
    Task<StandardJournalPrintModel> BuildStandardJournalPrintModelAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default);

    // نسخه چاپ HTML (A4) برای گردش حساب / دفتر کل
    Task<AccountLedgerPrintModel> BuildAccountLedgerPrintModelAsync(
        int accountId,
        DateTime? dateFrom,
        DateTime? dateTo,
        int? partyId = null,
        int? costCenterId = null,
        CancellationToken cancellationToken = default);
}

public class JournalReportService : IJournalReportService
{
    private const int GeneralSettingsId = 1;
    private const string DefaultZmLogoWebPath = "/zm_logo.jpg";

    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ISqlConnectionFactory _sql;

    public JournalReportService(AppDbContext db, IWebHostEnvironment env, ISqlConnectionFactory sql)
    {
        _db = db;
        _env = env;
        _sql = sql;
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
        var opening = await LoadOpeningBalanceAsync(dateFrom, cancellationToken);
        var entries = await LoadStandardJournalEntriesAsync(dateFrom, dateTo, cancellationToken);
        var rows = MapStandardJournalRows(entries, accountMap);
        var info = BuildInfo(settings, "دفتر روزنامه عمومی", dateFrom, dateTo);

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
        int? partyId = null,
        int? costCenterId = null,
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
        string? costCenterLabel = null;
        if (costCenterId is > 0)
        {
            costCenterLabel = await _db.CostCenters.AsNoTracking()
                .Where(c => c.CostCenterID == costCenterId && c.IsDeleted != true)
                .Select(c => c.Code + " — " + c.Name)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("مرکز هزینه یافت نشد.");
        }

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var opening = await connection.QueryFirstAsync<(decimal Debit, decimal Credit)>(
            """
            SELECT ISNULL(SUM(jl.DebitInBaseCurrency), 0) AS Debit,
                   ISNULL(SUM(jl.CreditInBaseCurrency), 0) AS Credit
            FROM JournalLines jl
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            WHERE jl.AccountId = @AccountId
              AND ISNULL(jl.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate < @Start
              AND (@PartyId IS NULL OR jl.PartyId = @PartyId)
              AND (@CostCenterId IS NULL OR jl.CostCenterId = @CostCenterId)
            """,
            new { AccountId = accountId, Start = start, PartyId = partyId, CostCenterId = costCenterId });

        var lines = (await connection.QueryAsync<(
            string EntryNumber,
            DateTime EntryDate,
            string? EntryDescription,
            string? LineDescription,
            decimal Debit,
            decimal Credit,
            int? CostCenterId,
            string? CostCenterCode,
            string? CostCenterName)>(
            """
            SELECT je.EntryNumber AS EntryNumber,
                   je.EntryDate AS EntryDate,
                   je.Description AS EntryDescription,
                   l.Description AS LineDescription,
                   l.DebitInBaseCurrency AS Debit,
                   l.CreditInBaseCurrency AS Credit,
                   l.CostCenterId AS CostCenterId,
                   cc.Code AS CostCenterCode,
                   cc.Name AS CostCenterName
            FROM JournalLines l
            INNER JOIN JournalEntries je ON je.JournalEntryID = l.JournalEntryId
            LEFT JOIN CostCenters cc ON cc.CostCenterID = l.CostCenterId AND ISNULL(cc.IsDeleted, 0) = 0
            WHERE l.AccountId = @AccountId
              AND ISNULL(l.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate >= @Start
              AND je.EntryDate <= @EndInclusive
              AND (@PartyId IS NULL OR l.PartyId = @PartyId)
              AND (@CostCenterId IS NULL OR l.CostCenterId = @CostCenterId)
            ORDER BY je.EntryDate, je.JournalEntryID, l.[LineNo]
            """,
            new
            {
                AccountId = accountId,
                Start = start,
                EndInclusive = endInclusive,
                PartyId = partyId,
                CostCenterId = costCenterId,
            })).AsList();

        var settings = await _db.GeneralSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == GeneralSettingsId, cancellationToken)
            ?? new GeneralSettings();

        var title = $"دفتر کل — {account.Code} {account.Name}";
        if (!string.IsNullOrWhiteSpace(costCenterLabel))
        {
            title += $" | مرکز هزینه: {costCenterLabel}";
        }

        var info = BuildInfo(settings, title, start, end);
        var openingBalance = opening.Debit - opening.Credit;
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
                CostCenterLabel = line.CostCenterId is > 0 && !string.IsNullOrWhiteSpace(line.CostCenterCode)
                    ? $"{line.CostCenterCode} — {line.CostCenterName}"
                    : null,
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
            CostCenterFilterLabel = costCenterLabel,
            OpeningBalance = openingBalance,
            ClosingBalance = running,
            PeriodDebit = rows.Sum(r => r.Debit),
            PeriodCredit = rows.Sum(r => r.Credit),
            OpeningDebit = opening.Debit,
            OpeningCredit = opening.Credit,
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

        var opening = await LoadOpeningBalanceAsync(dateFrom, cancellationToken);
        var entries = await LoadStandardJournalEntriesAsync(dateFrom, dateTo, cancellationToken);
        var rows = MapStandardJournalRows(entries, accountMap);

        var info = BuildInfo(settings, "دفتر روزنامه عمومی", dateFrom, dateTo);
        info.OpeningDebit = opening.Debit;
        info.OpeningCredit = opening.Credit;

        return BuildStandardReport(info, rows);
    }

    private async Task<(decimal Debit, decimal Credit)> LoadOpeningBalanceAsync(
        DateTime? dateFrom,
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

        var debit = await query.SumAsync(l => l.DebitInBaseCurrency, cancellationToken);
        var credit = await query.SumAsync(l => l.CreditInBaseCurrency, cancellationToken);
        return (debit, credit);
    }

    private async Task<List<StandardJournalEntryLoad>> LoadStandardJournalEntriesAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken)
    {
        var query = _db.JournalEntries
            .AsNoTracking()
            .Include(e => e.Lines)
            .Where(e => e.IsDeleted != true && e.IsPosted);

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
    public string? CostCenterFilterLabel { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public decimal PeriodDebit { get; set; }
    public decimal PeriodCredit { get; set; }
    public decimal OpeningDebit { get; set; }
    public decimal OpeningCredit { get; set; }
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
    public string? CostCenterLabel { get; set; }
}
