using Dapper;
using HamgamTransport.Server.Data;

namespace HamgamTransport.Server.Services;

public interface IFinanceStatementService
{
    Task<object> GetProfitAndLossAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        DateTime? compareFrom = null,
        DateTime? compareTo = null,
        CancellationToken cancellationToken = default);

    // سود/زیان خالص بازه به ارز پایه — برای سقف توزیع سود سهام‌داران
    Task<decimal> GetNetIncomeInBaseAsync(
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default);

    Task<object> GetBalanceSheetAsync(
        DateTime? asOf,
        DateTime? compareAsOf = null,
        CancellationToken cancellationToken = default);

    Task<object> GetTrialBalanceAsync(DateTime? asOf, CancellationToken cancellationToken = default);
    Task<object> GetArAgingAsync(DateTime? asOf, CancellationToken cancellationToken = default);
    Task<object> GetApAgingAsync(DateTime? asOf, CancellationToken cancellationToken = default);
    Task<object> GetCashFlowAsync(DateTime? dateFrom, DateTime? dateTo, CancellationToken cancellationToken = default);
}

public class FinanceStatementService : IFinanceStatementService
{
    private readonly ISqlConnectionFactory _sql;

    public FinanceStatementService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    // صورت سود و زیان چندارزی — مبالغ هر ارز جدا + جمع معادل پایه
    public async Task<object> GetProfitAndLossAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        DateTime? compareFrom = null,
        DateTime? compareTo = null,
        CancellationToken cancellationToken = default)
    {
        var current = await GetProfitAndLossCoreAsync(dateFrom, dateTo, cancellationToken);
        if (!compareFrom.HasValue && !compareTo.HasValue)
        {
            return current;
        }

        var compare = await GetProfitAndLossCoreAsync(compareFrom, compareTo, cancellationToken);
        return new { current, compare };
    }

    public async Task<decimal> GetNetIncomeInBaseAsync(
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken = default)
    {
        var rows = await QueryPlCurrencyBalancesAsync(dateFrom, dateTo, cancellationToken);
        var totalRevenue = rows
            .Where(r => r.AccountType == (int)AccountType.Revenue)
            .Sum(r => r.CreditInBase - r.DebitInBase);
        var totalCogs = rows
            .Where(r => r.AccountType == (int)AccountType.Cogs)
            .Sum(r => r.DebitInBase - r.CreditInBase);
        var totalExpense = rows
            .Where(r => r.AccountType == (int)AccountType.Expense)
            .Sum(r => r.DebitInBase - r.CreditInBase);
        return totalRevenue - totalCogs - totalExpense;
    }

    private async Task<object> GetProfitAndLossCoreAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
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

        var rows = await QueryPlCurrencyBalancesAsync(start, end, cancellationToken);

        var revenues = BuildAccountGroups(rows, AccountType.Revenue, isPl: true);
        var cogs = BuildAccountGroups(rows, AccountType.Cogs, isPl: true);
        var expenses = BuildAccountGroups(rows, AccountType.Expense, isPl: true);

        var totalRevenue = revenues.Sum(x => x.AmountInBase);
        var totalCogs = cogs.Sum(x => x.AmountInBase);
        var totalExpense = expenses.Sum(x => x.AmountInBase);
        var grossProfit = totalRevenue - totalCogs;
        var netIncome = grossProfit - totalExpense;

        var byCurrency = BuildPlCurrencyTotals(rows).Select(MapPlCurrencyTotal).ToList();

        return new
        {
            from = JalaliDateHelper.FormatDate(start),
            to = JalaliDateHelper.FormatDate(end.Date),
            fromLabel = JalaliDateHelper.FormatDateWithMonthName(start),
            toLabel = JalaliDateHelper.FormatDateWithMonthName(end.Date),
            totals = new
            {
                revenue = totalRevenue,
                cogs = totalCogs,
                expense = totalExpense,
                grossProfit,
                netIncome,
            },
            byCurrency,
            revenues = revenues.Select(MapAccountGroup).ToList(),
            cogs = cogs.Select(MapAccountGroup).ToList(),
            expenses = expenses.Select(MapAccountGroup).ToList(),
        };
    }

    private async Task<List<CurrencyBalanceRow>> QueryPlCurrencyBalancesAsync(
        DateTime dateFrom,
        DateTime dateTo,
        CancellationToken cancellationToken)
    {
        var start = dateFrom.Date;
        var end = dateTo.Date.AddDays(1).AddTicks(-1);
        if (start > end)
        {
            throw new InvalidOperationException("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        }

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        return (await connection.QueryAsync<CurrencyBalanceRow>(
            """
            SELECT a.AccountID AS AccountId,
                   a.Code,
                   a.Name,
                   CAST(a.AccountType AS int) AS AccountType,
                   CAST(a.Nature AS int) AS Nature,
                   CAST(a.Level AS int) AS Level,
                   a.SystemCode,
                   cur.CurrencyID AS CurrencyId,
                   cur.CurrencyCode,
                   cur.Symbol,
                   cur.Name AS CurrencyName,
                   CAST(CASE WHEN cur.IsBaseCurrency = 1 THEN 1 ELSE 0 END AS bit) AS IsBaseCurrency,
                   ISNULL(SUM(jl.Debit), 0) AS Debit,
                   ISNULL(SUM(jl.Credit), 0) AS Credit,
                   ISNULL(SUM(jl.DebitInBaseCurrency), 0) AS DebitInBase,
                   ISNULL(SUM(jl.CreditInBaseCurrency), 0) AS CreditInBase
            FROM Accounts a
            INNER JOIN JournalLines jl ON jl.AccountId = a.AccountID AND ISNULL(jl.IsDeleted, 0) = 0
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            INNER JOIN Currencies cur ON cur.CurrencyID = jl.CurrencyId AND ISNULL(cur.IsDeleted, 0) = 0
            WHERE ISNULL(a.IsDeleted, 0) = 0
              AND a.IsPostable = 1
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate >= @RangeStart
              AND je.EntryDate <= @RangeEnd
              AND a.AccountType IN (@Revenue, @Expense, @Cogs)
              AND je.Source NOT IN (@YearEndClosing, @YearEndReversal)
            GROUP BY a.AccountID, a.Code, a.Name, a.AccountType, a.Nature, a.Level, a.SystemCode,
                     cur.CurrencyID, cur.CurrencyCode, cur.Symbol, cur.Name, cur.IsBaseCurrency
            HAVING ABS(ISNULL(SUM(jl.DebitInBaseCurrency), 0) - ISNULL(SUM(jl.CreditInBaseCurrency), 0)) >= 0.01
                OR ABS(ISNULL(SUM(jl.Debit), 0) - ISNULL(SUM(jl.Credit), 0)) >= 0.01
            ORDER BY a.Code, cur.IsBaseCurrency DESC, cur.CurrencyCode
            """,
            new
            {
                RangeStart = start,
                RangeEnd = end,
                Revenue = (int)AccountType.Revenue,
                Expense = (int)AccountType.Expense,
                Cogs = (int)AccountType.Cogs,
                YearEndClosing = (int)JournalSource.YearEndClosing,
                YearEndReversal = (int)JournalSource.YearEndReversal,
            })).AsList();
    }

    // ترازنامه چندارزی — هر حساب با مانده‌های ارزی + معادل پایه؛ تراز فقط روی پایه
    public async Task<object> GetBalanceSheetAsync(
        DateTime? asOf,
        DateTime? compareAsOf = null,
        CancellationToken cancellationToken = default)
    {
        var current = await GetBalanceSheetCoreAsync(asOf, cancellationToken);
        if (!compareAsOf.HasValue)
        {
            return current;
        }

        var compare = await GetBalanceSheetCoreAsync(compareAsOf, cancellationToken);
        return new { current, compare };
    }

    private async Task<object> GetBalanceSheetCoreAsync(
        DateTime? asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = (asOf ?? DateTime.Today).Date;
        var asOfEnd = asOfDate.AddDays(1).AddTicks(-1);
        var solarYear = JalaliDateHelper.GetSolarYear(asOfDate);

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var permanentRows = (await connection.QueryAsync<CurrencyBalanceRow>(
            """
            SELECT a.AccountID AS AccountId,
                   a.Code,
                   a.Name,
                   CAST(a.AccountType AS int) AS AccountType,
                   CAST(a.Nature AS int) AS Nature,
                   CAST(a.Level AS int) AS Level,
                   a.SystemCode,
                   cur.CurrencyID AS CurrencyId,
                   cur.CurrencyCode,
                   cur.Symbol,
                   cur.Name AS CurrencyName,
                   CAST(CASE WHEN cur.IsBaseCurrency = 1 THEN 1 ELSE 0 END AS bit) AS IsBaseCurrency,
                   ISNULL(SUM(jl.Debit), 0) AS Debit,
                   ISNULL(SUM(jl.Credit), 0) AS Credit,
                   ISNULL(SUM(jl.DebitInBaseCurrency), 0) AS DebitInBase,
                   ISNULL(SUM(jl.CreditInBaseCurrency), 0) AS CreditInBase
            FROM Accounts a
            INNER JOIN JournalLines jl ON jl.AccountId = a.AccountID AND ISNULL(jl.IsDeleted, 0) = 0
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            INNER JOIN Currencies cur ON cur.CurrencyID = jl.CurrencyId AND ISNULL(cur.IsDeleted, 0) = 0
            WHERE ISNULL(a.IsDeleted, 0) = 0
              AND a.IsPostable = 1
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate <= @AsOfEnd
              AND a.AccountType IN (@Asset, @Liability, @Equity)
            GROUP BY a.AccountID, a.Code, a.Name, a.AccountType, a.Nature, a.Level, a.SystemCode,
                     cur.CurrencyID, cur.CurrencyCode, cur.Symbol, cur.Name, cur.IsBaseCurrency
            HAVING ABS(ISNULL(SUM(jl.DebitInBaseCurrency), 0) - ISNULL(SUM(jl.CreditInBaseCurrency), 0)) >= 0.01
                OR ABS(ISNULL(SUM(jl.Debit), 0) - ISNULL(SUM(jl.Credit), 0)) >= 0.01
            ORDER BY a.Code, cur.IsBaseCurrency DESC, cur.CurrencyCode
            """,
            new
            {
                AsOfEnd = asOfEnd,
                Asset = (int)AccountType.Asset,
                Liability = (int)AccountType.Liability,
                Equity = (int)AccountType.Equity,
            })).AsList();

        // مانده حساب‌های موقت تا تاریخ — شامل اسناد اختتام/معکوس تا بعد از بستن سال دوباره شمرده نشود
        // (اختتام سود را به سود انباشته برده؛ اگر اسناد اختتام حذف شوند، سود هم در حقوق مالکانه و هم به‌عنوان «سود جاری» می‌آید)
        var plRows = (await connection.QueryAsync<CurrencyBalanceRow>(
            """
            SELECT a.AccountID AS AccountId,
                   a.Code,
                   a.Name,
                   CAST(a.AccountType AS int) AS AccountType,
                   CAST(a.Nature AS int) AS Nature,
                   CAST(a.Level AS int) AS Level,
                   a.SystemCode,
                   cur.CurrencyID AS CurrencyId,
                   cur.CurrencyCode,
                   cur.Symbol,
                   cur.Name AS CurrencyName,
                   CAST(CASE WHEN cur.IsBaseCurrency = 1 THEN 1 ELSE 0 END AS bit) AS IsBaseCurrency,
                   ISNULL(SUM(jl.Debit), 0) AS Debit,
                   ISNULL(SUM(jl.Credit), 0) AS Credit,
                   ISNULL(SUM(jl.DebitInBaseCurrency), 0) AS DebitInBase,
                   ISNULL(SUM(jl.CreditInBaseCurrency), 0) AS CreditInBase
            FROM Accounts a
            INNER JOIN JournalLines jl ON jl.AccountId = a.AccountID AND ISNULL(jl.IsDeleted, 0) = 0
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            INNER JOIN Currencies cur ON cur.CurrencyID = jl.CurrencyId AND ISNULL(cur.IsDeleted, 0) = 0
            WHERE ISNULL(a.IsDeleted, 0) = 0
              AND a.IsPostable = 1
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate <= @AsOfEnd
              AND a.AccountType IN (@Revenue, @Expense, @Cogs)
            GROUP BY a.AccountID, a.Code, a.Name, a.AccountType, a.Nature, a.Level, a.SystemCode,
                     cur.CurrencyID, cur.CurrencyCode, cur.Symbol, cur.Name, cur.IsBaseCurrency
            HAVING ABS(ISNULL(SUM(jl.DebitInBaseCurrency), 0) - ISNULL(SUM(jl.CreditInBaseCurrency), 0)) >= 0.01
                OR ABS(ISNULL(SUM(jl.Debit), 0) - ISNULL(SUM(jl.Credit), 0)) >= 0.01
            """,
            new
            {
                AsOfEnd = asOfEnd,
                Revenue = (int)AccountType.Revenue,
                Expense = (int)AccountType.Expense,
                Cogs = (int)AccountType.Cogs,
            })).AsList();

        var assets = BuildAccountGroups(permanentRows, AccountType.Asset, isPl: false);
        var liabilities = BuildAccountGroups(permanentRows, AccountType.Liability, isPl: false);
        var equity = BuildAccountGroups(permanentRows, AccountType.Equity, isPl: false);

        var totalAssets = assets.Sum(x => x.AmountInBase);
        var totalLiabilities = liabilities.Sum(x => x.AmountInBase);
        var totalEquity = equity.Sum(x => x.AmountInBase);

        var plTotals = BuildPlCurrencyTotals(plRows);
        var currentNetIncome = plTotals.Sum(x => x.NetIncomeInBase);
        var currentNetByCurrency = plTotals
            .Where(x => Math.Abs(x.NetIncome) >= 0.01m || Math.Abs(x.NetIncomeInBase) >= 0.01m)
            .Select(x => new
            {
                x.CurrencyId,
                x.CurrencyCode,
                x.Symbol,
                x.CurrencyName,
                x.IsBaseCurrency,
                amount = x.NetIncome,
                amountInBase = x.NetIncomeInBase,
            })
            .ToList();

        var totalEquityWithIncome = totalEquity + currentNetIncome;
        var totalLiabilitiesAndEquity = totalLiabilities + totalEquityWithIncome;
        var difference = totalAssets - totalLiabilitiesAndEquity;

        var byCurrency = BuildBsCurrencyTotals(permanentRows, plRows);

        return new
        {
            asOf = JalaliDateHelper.FormatDate(asOfDate),
            asOfLabel = JalaliDateHelper.FormatDateWithMonthName(asOfDate),
            solarYear,
            totals = new
            {
                assets = totalAssets,
                liabilities = totalLiabilities,
                equity = totalEquity,
                currentNetIncome,
                equityWithIncome = totalEquityWithIncome,
                liabilitiesAndEquity = totalLiabilitiesAndEquity,
                difference,
                isBalanced = Math.Abs(difference) < 0.01m,
            },
            byCurrency,
            currentNetByCurrency,
            assets = assets.Select(MapAccountGroup).ToList(),
            liabilities = liabilities.Select(MapAccountGroup).ToList(),
            equity = equity.Select(MapAccountGroup).ToList(),
        };
    }

    // تراز آزمایشی تا تاریخ — جمع بدهکار/بستانکار ارز پایه برای حساب‌های قابل‌ثبت
    public async Task<object> GetTrialBalanceAsync(
        DateTime? asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = (asOf ?? DateTime.Today).Date;
        var asOfEnd = asOfDate.AddDays(1).AddTicks(-1);

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<TrialBalanceRow>(
            """
            SELECT a.AccountID AS AccountId,
                   a.Code,
                   a.Name,
                   CAST(a.AccountType AS int) AS AccountType,
                   CAST(a.Nature AS int) AS Nature,
                   CAST(a.Level AS int) AS Level,
                   ISNULL(SUM(jl.DebitInBaseCurrency), 0) AS DebitInBase,
                   ISNULL(SUM(jl.CreditInBaseCurrency), 0) AS CreditInBase
            FROM Accounts a
            INNER JOIN JournalLines jl ON jl.AccountId = a.AccountID AND ISNULL(jl.IsDeleted, 0) = 0
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            WHERE ISNULL(a.IsDeleted, 0) = 0
              AND a.IsPostable = 1
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate <= @AsOfEnd
            GROUP BY a.AccountID, a.Code, a.Name, a.AccountType, a.Nature, a.Level
            HAVING ABS(ISNULL(SUM(jl.DebitInBaseCurrency), 0)) >= 0.01
                OR ABS(ISNULL(SUM(jl.CreditInBaseCurrency), 0)) >= 0.01
            ORDER BY a.Code
            """,
            new { AsOfEnd = asOfEnd })).AsList();

        var lines = rows.Select(r =>
        {
            var net = r.DebitInBase - r.CreditInBase;
            var debitBalance = net > 0.005m ? net : 0m;
            var creditBalance = net < -0.005m ? -net : 0m;
            return new
            {
                accountId = r.AccountId,
                code = r.Code,
                name = r.Name,
                accountType = r.AccountType,
                nature = r.Nature,
                level = r.Level,
                debitTotal = r.DebitInBase,
                creditTotal = r.CreditInBase,
                debitBalance,
                creditBalance,
            };
        }).ToList();

        var totalDebit = lines.Sum(x => x.debitTotal);
        var totalCredit = lines.Sum(x => x.creditTotal);
        var totalDebitBalance = lines.Sum(x => x.debitBalance);
        var totalCreditBalance = lines.Sum(x => x.creditBalance);

        return new
        {
            asOf = JalaliDateHelper.FormatDate(asOfDate),
            asOfLabel = JalaliDateHelper.FormatDateWithMonthName(asOfDate),
            totals = new
            {
                debitTotal = totalDebit,
                creditTotal = totalCredit,
                debitBalance = totalDebitBalance,
                creditBalance = totalCreditBalance,
                isBalanced = Math.Abs(totalDebit - totalCredit) < 0.01m
                             && Math.Abs(totalDebitBalance - totalCreditBalance) < 0.01m,
            },
            lines,
        };
    }

    // سررسید دریافتنی — فاکتورهای فروش باز تا تاریخ
    public async Task<object> GetArAgingAsync(
        DateTime? asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = (asOf ?? DateTime.Today).Date;
        var asOfEnd = asOfDate.AddDays(1).AddTicks(-1);

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<AgingInvoiceRow>(
            """
            SELECT i.SaleInvoiceID AS InvoiceId,
                   i.InvoiceNumber,
                   i.CustomerId AS PartyId,
                   c.Name AS PartyName,
                   i.InvoiceDate,
                   ins.DueDate,
                   ins.Amount - (
                       ins.PaidAmount - ISNULL((
                           SELECT SUM(ps.Amount)
                           FROM PartySettlements ps
                           WHERE ps.InstallmentId = ins.InvoiceInstallmentID
                             AND ISNULL(ps.IsDeleted, 0) = 0
                             AND ps.SettlementDate > @AsOfEnd
                       ), 0)
                   ) AS OpenAmount,
                   CASE WHEN i.TotalAmount > 0
                        THEN (
                            ins.Amount - (
                                ins.PaidAmount - ISNULL((
                                    SELECT SUM(ps.Amount)
                                    FROM PartySettlements ps
                                    WHERE ps.InstallmentId = ins.InvoiceInstallmentID
                                      AND ISNULL(ps.IsDeleted, 0) = 0
                                      AND ps.SettlementDate > @AsOfEnd
                                ), 0)
                            )
                        ) * (i.TotalAmountInBaseCurrency / i.TotalAmount)
                        ELSE 0 END AS OpenAmountInBase
            FROM SaleInvoices i
            INNER JOIN Customers c ON c.CustomerID = i.CustomerId AND ISNULL(c.IsDeleted, 0) = 0
            INNER JOIN InvoiceInstallments ins ON ins.InvoiceId = i.SaleInvoiceID
              AND ins.InvoiceKind = @SaleInstallmentKind AND ISNULL(ins.IsDeleted, 0) = 0
            WHERE ISNULL(i.IsDeleted, 0) = 0
              AND i.IsPosted = 1
              AND i.DocumentType = @InvoiceDocType
              AND i.InvoiceDate <= @AsOfEnd
              AND ins.Amount - (
                  ins.PaidAmount - ISNULL((
                      SELECT SUM(ps.Amount)
                      FROM PartySettlements ps
                      WHERE ps.InstallmentId = ins.InvoiceInstallmentID
                        AND ISNULL(ps.IsDeleted, 0) = 0
                        AND ps.SettlementDate > @AsOfEnd
                  ), 0)
              ) > 0.01

            UNION ALL

            SELECT i.SaleInvoiceID AS InvoiceId,
                   i.InvoiceNumber,
                   i.CustomerId AS PartyId,
                   c.Name AS PartyName,
                   i.InvoiceDate,
                   ISNULL(i.DueDate, i.InvoiceDate) AS DueDate,
                   i.TotalAmount - (
                       i.PaidAmount - ISNULL((
                           SELECT SUM(ps.Amount)
                           FROM PartySettlements ps
                           WHERE ps.SaleInvoiceId = i.SaleInvoiceID
                             AND ps.InstallmentId IS NULL
                             AND ISNULL(ps.IsDeleted, 0) = 0
                             AND ps.SettlementDate > @AsOfEnd
                       ), 0)
                   ) AS OpenAmount,
                   i.TotalAmountInBaseCurrency
                     - CASE WHEN i.TotalAmount > 0
                            THEN (
                                i.PaidAmount - ISNULL((
                                    SELECT SUM(ps.Amount)
                                    FROM PartySettlements ps
                                    WHERE ps.SaleInvoiceId = i.SaleInvoiceID
                                      AND ps.InstallmentId IS NULL
                                      AND ISNULL(ps.IsDeleted, 0) = 0
                                      AND ps.SettlementDate > @AsOfEnd
                                ), 0)
                            ) * (i.TotalAmountInBaseCurrency / i.TotalAmount)
                            ELSE 0 END AS OpenAmountInBase
            FROM SaleInvoices i
            INNER JOIN Customers c ON c.CustomerID = i.CustomerId AND ISNULL(c.IsDeleted, 0) = 0
            WHERE ISNULL(i.IsDeleted, 0) = 0
              AND i.IsPosted = 1
              AND i.DocumentType = @InvoiceDocType
              AND i.InvoiceDate <= @AsOfEnd
              AND i.TotalAmount - (
                  i.PaidAmount - ISNULL((
                      SELECT SUM(ps.Amount)
                      FROM PartySettlements ps
                      WHERE ps.SaleInvoiceId = i.SaleInvoiceID
                        AND ps.InstallmentId IS NULL
                        AND ISNULL(ps.IsDeleted, 0) = 0
                        AND ps.SettlementDate > @AsOfEnd
                  ), 0)
              ) > 0.01
              AND NOT EXISTS (
                  SELECT 1 FROM InvoiceInstallments ins
                  WHERE ins.InvoiceId = i.SaleInvoiceID
                    AND ins.InvoiceKind = @SaleInstallmentKind
                    AND ISNULL(ins.IsDeleted, 0) = 0)
            ORDER BY InvoiceDate, InvoiceNumber
            """,
            new
            {
                AsOfEnd = asOfEnd,
                InvoiceDocType = (int)InvoiceDocumentType.Invoice,
                SaleInstallmentKind = (int)InvoiceInstallmentKind.Sale,
            })).AsList();

        return BuildAgingResult(asOfDate, rows);
    }

    // سررسید پرداختنی — فاکتورهای خرید باز تا تاریخ
    public async Task<object> GetApAgingAsync(
        DateTime? asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfDate = (asOf ?? DateTime.Today).Date;
        var asOfEnd = asOfDate.AddDays(1).AddTicks(-1);

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var rows = (await connection.QueryAsync<AgingInvoiceRow>(
            """
            SELECT i.PurchaseInvoiceID AS InvoiceId,
                   i.InvoiceNumber,
                   i.SupplierId AS PartyId,
                   s.Name AS PartyName,
                   i.InvoiceDate,
                   ins.DueDate,
                   ins.Amount - (
                       ins.PaidAmount - ISNULL((
                           SELECT SUM(ps.Amount)
                           FROM PartySettlements ps
                           WHERE ps.InstallmentId = ins.InvoiceInstallmentID
                             AND ISNULL(ps.IsDeleted, 0) = 0
                             AND ps.SettlementDate > @AsOfEnd
                       ), 0)
                   ) AS OpenAmount,
                   CASE WHEN i.TotalAmount > 0
                        THEN (
                            ins.Amount - (
                                ins.PaidAmount - ISNULL((
                                    SELECT SUM(ps.Amount)
                                    FROM PartySettlements ps
                                    WHERE ps.InstallmentId = ins.InvoiceInstallmentID
                                      AND ISNULL(ps.IsDeleted, 0) = 0
                                      AND ps.SettlementDate > @AsOfEnd
                                ), 0)
                            )
                        ) * (i.TotalAmountInBaseCurrency / i.TotalAmount)
                        ELSE 0 END AS OpenAmountInBase
            FROM PurchaseInvoices i
            INNER JOIN Suppliers s ON s.SupplierID = i.SupplierId AND ISNULL(s.IsDeleted, 0) = 0
            INNER JOIN InvoiceInstallments ins ON ins.InvoiceId = i.PurchaseInvoiceID
              AND ins.InvoiceKind = @PurchaseInstallmentKind AND ISNULL(ins.IsDeleted, 0) = 0
            WHERE ISNULL(i.IsDeleted, 0) = 0
              AND i.IsPosted = 1
              AND i.DocumentType = @InvoiceDocType
              AND i.InvoiceDate <= @AsOfEnd
              AND ins.Amount - (
                  ins.PaidAmount - ISNULL((
                      SELECT SUM(ps.Amount)
                      FROM PartySettlements ps
                      WHERE ps.InstallmentId = ins.InvoiceInstallmentID
                        AND ISNULL(ps.IsDeleted, 0) = 0
                        AND ps.SettlementDate > @AsOfEnd
                  ), 0)
              ) > 0.01

            UNION ALL

            SELECT i.PurchaseInvoiceID AS InvoiceId,
                   i.InvoiceNumber,
                   i.SupplierId AS PartyId,
                   s.Name AS PartyName,
                   i.InvoiceDate,
                   ISNULL(i.DueDate, i.InvoiceDate) AS DueDate,
                   i.TotalAmount - (
                       i.PaidAmount - ISNULL((
                           SELECT SUM(ps.Amount)
                           FROM PartySettlements ps
                           WHERE ps.PurchaseInvoiceId = i.PurchaseInvoiceID
                             AND ps.InstallmentId IS NULL
                             AND ISNULL(ps.IsDeleted, 0) = 0
                             AND ps.SettlementDate > @AsOfEnd
                       ), 0)
                   ) AS OpenAmount,
                   i.TotalAmountInBaseCurrency
                     - CASE WHEN i.TotalAmount > 0
                            THEN (
                                i.PaidAmount - ISNULL((
                                    SELECT SUM(ps.Amount)
                                    FROM PartySettlements ps
                                    WHERE ps.PurchaseInvoiceId = i.PurchaseInvoiceID
                                      AND ps.InstallmentId IS NULL
                                      AND ISNULL(ps.IsDeleted, 0) = 0
                                      AND ps.SettlementDate > @AsOfEnd
                                ), 0)
                            ) * (i.TotalAmountInBaseCurrency / i.TotalAmount)
                            ELSE 0 END AS OpenAmountInBase
            FROM PurchaseInvoices i
            INNER JOIN Suppliers s ON s.SupplierID = i.SupplierId AND ISNULL(s.IsDeleted, 0) = 0
            WHERE ISNULL(i.IsDeleted, 0) = 0
              AND i.IsPosted = 1
              AND i.DocumentType = @InvoiceDocType
              AND i.InvoiceDate <= @AsOfEnd
              AND i.TotalAmount - (
                  i.PaidAmount - ISNULL((
                      SELECT SUM(ps.Amount)
                      FROM PartySettlements ps
                      WHERE ps.PurchaseInvoiceId = i.PurchaseInvoiceID
                        AND ps.InstallmentId IS NULL
                        AND ISNULL(ps.IsDeleted, 0) = 0
                        AND ps.SettlementDate > @AsOfEnd
                  ), 0)
              ) > 0.01
              AND NOT EXISTS (
                  SELECT 1 FROM InvoiceInstallments ins
                  WHERE ins.InvoiceId = i.PurchaseInvoiceID
                    AND ins.InvoiceKind = @PurchaseInstallmentKind
                    AND ISNULL(ins.IsDeleted, 0) = 0)
            ORDER BY InvoiceDate, InvoiceNumber
            """,
            new
            {
                AsOfEnd = asOfEnd,
                InvoiceDocType = (int)InvoiceDocumentType.Invoice,
                PurchaseInstallmentKind = (int)InvoiceInstallmentKind.Purchase,
            })).AsList();

        return BuildAgingResult(asOfDate, rows);
    }

    // صورت جریان وجوه نقد بر مبنای تغییر خالص حساب‌های نقد و بانک در اسناد ثبت‌شده
    public async Task<object> GetCashFlowAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var (yearStart, _) = JalaliDateHelper.GetSolarYearRange(JalaliDateHelper.GetSolarYear(today));
        var start = (dateFrom ?? yearStart).Date;
        var end = (dateTo ?? today).Date.AddDays(1).AddTicks(-1);
        if (start > end)
        {
            throw new InvalidOperationException("تاریخ شروع نباید بعد از تاریخ پایان باشد.");
        }

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var rows = (await connection.QueryAsync<CashFlowRow>(
            """
            WITH CashAccountTree AS (
                SELECT AccountID
                FROM Accounts
                WHERE SystemCode IN (@CashBoxesCode, @BanksCode)
                  AND ISNULL(IsDeleted, 0) = 0
                UNION ALL
                SELECT child.AccountID
                FROM Accounts child
                INNER JOIN CashAccountTree parent ON child.ParentAccountId = parent.AccountID
                WHERE ISNULL(child.IsDeleted, 0) = 0
            )
            SELECT je.Source,
                   ISNULL(SUM(jl.DebitInBaseCurrency - jl.CreditInBaseCurrency), 0) AS NetChangeInBase
            FROM JournalLines jl
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            INNER JOIN Accounts a ON a.AccountID = jl.AccountId
            WHERE ISNULL(jl.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND je.EntryDate >= @RangeStart
              AND je.EntryDate <= @RangeEnd
              AND (jl.CashBoxId IS NOT NULL OR a.AccountID IN (SELECT AccountID FROM CashAccountTree))
            GROUP BY je.Source
            """,
            new
            {
                RangeStart = start,
                RangeEnd = end,
                CashBoxesCode = AccountSystemCode.CashBoxes,
                BanksCode = AccountSystemCode.Banks,
            })).AsList();

        decimal operating = 0, investing = 0, financing = 0;
        foreach (var row in rows)
        {
            var source = (JournalSource)row.Source;
            if (source is JournalSource.FixedAssetAcquire or JournalSource.FixedAssetDispose)
            {
                investing += row.NetChangeInBase;
            }
            else if (source is JournalSource.EquityCapitalContribution
                or JournalSource.EquityCapitalWithdrawal
                or JournalSource.EquityProfitDistribution
                or JournalSource.EquityOpeningBalance
                or JournalSource.EquityYearAllocation
                or JournalSource.EquityYearAllocationReversal)
            {
                financing += row.NetChangeInBase;
            }
            else
            {
                operating += row.NetChangeInBase;
            }
        }

        return new
        {
            from = JalaliDateHelper.FormatDate(start),
            to = JalaliDateHelper.FormatDate(end.Date),
            fromLabel = JalaliDateHelper.FormatDateWithMonthName(start),
            toLabel = JalaliDateHelper.FormatDateWithMonthName(end.Date),
            operating,
            investing,
            financing,
            netChange = operating + investing + financing,
            bySource = rows.Select(x => new
            {
                source = x.Source,
                sourceLabel = JournalSourceLabels.Label(x.Source),
                netChangeInBase = x.NetChangeInBase,
            }).ToList(),
        };
    }

    private static object BuildAgingResult(DateTime asOfDate, IReadOnlyList<AgingInvoiceRow> rows)
    {
        var lines = rows.Select(r =>
        {
            var days = Math.Max(0, (asOfDate - r.DueDate.Date).Days);
            var bucket = GetAgingBucket(days);
            return new
            {
                invoiceId = r.InvoiceId,
                invoiceNumber = r.InvoiceNumber,
                partyId = r.PartyId,
                partyName = r.PartyName,
                invoiceDate = JalaliDateHelper.FormatDate(r.InvoiceDate),
                dueDate = JalaliDateHelper.FormatDate(r.DueDate),
                daysOutstanding = days,
                bucket,
                openAmount = r.OpenAmount,
                openAmountInBase = r.OpenAmountInBase,
            };
        }).ToList();

        decimal SumBucket(string bucket) =>
            lines.Where(x => x.bucket == bucket).Sum(x => x.openAmountInBase);

        return new
        {
            asOf = JalaliDateHelper.FormatDate(asOfDate),
            asOfLabel = JalaliDateHelper.FormatDateWithMonthName(asOfDate),
            totals = new
            {
                current0To30 = SumBucket("0-30"),
                days31To60 = SumBucket("31-60"),
                days61To90 = SumBucket("61-90"),
                over90 = SumBucket("90+"),
                total = lines.Sum(x => x.openAmountInBase),
            },
            lines,
        };
    }

    private static string GetAgingBucket(int daysOutstanding) => daysOutstanding switch
    {
        <= 30 => "0-30",
        <= 60 => "31-60",
        <= 90 => "61-90",
        _ => "90+",
    };

    private static List<AccountGroup> BuildAccountGroups(
        IEnumerable<CurrencyBalanceRow> rows,
        AccountType type,
        bool isPl)
    {
        return rows
            .Where(r => (AccountType)r.AccountType == type)
            .GroupBy(r => new { r.AccountId, r.Code, r.Name, r.Level, r.SystemCode, r.Nature, r.AccountType })
            .Select(g =>
            {
                var currencies = g
                    .Select(r =>
                    {
                        var amount = isPl ? SignedPlAmount(r) : SignedBsAmount(r);
                        var amountInBase = isPl ? SignedPlAmountInBase(r) : SignedBsAmountInBase(r);
                        return new CurrencyAmount(
                            r.CurrencyId,
                            r.CurrencyCode,
                            r.Symbol,
                            r.CurrencyName,
                            r.IsBaseCurrency,
                            amount,
                            amountInBase);
                    })
                    .Where(c => Math.Abs(c.Amount) >= 0.01m || Math.Abs(c.AmountInBase) >= 0.01m)
                    .OrderByDescending(c => c.IsBaseCurrency)
                    .ThenBy(c => c.CurrencyCode)
                    .ToList();

                return new AccountGroup(
                    g.Key.AccountId,
                    g.Key.Code,
                    g.Key.Name,
                    g.Key.Level,
                    g.Key.SystemCode,
                    (AccountType)g.Key.AccountType,
                    (AccountNature)g.Key.Nature,
                    currencies.Sum(c => c.AmountInBase),
                    currencies);
            })
            .Where(a => Math.Abs(a.AmountInBase) >= 0.01m || a.Currencies.Count > 0)
            .OrderBy(a => a.Code)
            .ToList();
    }

    private static List<PlCurrencyTotal> BuildPlCurrencyTotals(IEnumerable<CurrencyBalanceRow> rows)
    {
        return rows
            .GroupBy(r => new { r.CurrencyId, r.CurrencyCode, r.Symbol, r.CurrencyName, r.IsBaseCurrency })
            .Select(g =>
            {
                decimal revenue = 0, expense = 0, cogs = 0;
                decimal revenueBase = 0, expenseBase = 0, cogsBase = 0;
                foreach (var row in g)
                {
                    switch ((AccountType)row.AccountType)
                    {
                        case AccountType.Revenue:
                            revenue += SignedPlAmount(row);
                            revenueBase += SignedPlAmountInBase(row);
                            break;
                        case AccountType.Expense:
                            expense += SignedPlAmount(row);
                            expenseBase += SignedPlAmountInBase(row);
                            break;
                        case AccountType.Cogs:
                            cogs += SignedPlAmount(row);
                            cogsBase += SignedPlAmountInBase(row);
                            break;
                    }
                }

                return new PlCurrencyTotal(
                    g.Key.CurrencyId,
                    g.Key.CurrencyCode,
                    g.Key.Symbol,
                    g.Key.CurrencyName,
                    g.Key.IsBaseCurrency,
                    revenue,
                    cogs,
                    expense,
                    revenue - cogs,
                    revenue - cogs - expense,
                    revenueBase,
                    cogsBase,
                    expenseBase,
                    revenueBase - cogsBase,
                    revenueBase - cogsBase - expenseBase);
            })
            .Where(x =>
                Math.Abs(x.RevenueInBase) >= 0.01m
                || Math.Abs(x.CogsInBase) >= 0.01m
                || Math.Abs(x.ExpenseInBase) >= 0.01m
                || Math.Abs(x.Revenue) >= 0.01m
                || Math.Abs(x.Cogs) >= 0.01m
                || Math.Abs(x.Expense) >= 0.01m)
            .OrderByDescending(x => x.IsBaseCurrency)
            .ThenBy(x => x.CurrencyCode)
            .ToList();
    }

    private static List<object> BuildBsCurrencyTotals(
        IEnumerable<CurrencyBalanceRow> permanentRows,
        IEnumerable<CurrencyBalanceRow> plRows)
    {
        var currencyKeys = permanentRows
            .Select(r => new { r.CurrencyId, r.CurrencyCode, r.Symbol, r.CurrencyName, r.IsBaseCurrency })
            .Concat(plRows.Select(r => new { r.CurrencyId, r.CurrencyCode, r.Symbol, r.CurrencyName, r.IsBaseCurrency }))
            .Distinct()
            .OrderByDescending(x => x.IsBaseCurrency)
            .ThenBy(x => x.CurrencyCode)
            .ToList();

        var result = new List<object>();
        foreach (var key in currencyKeys)
        {
            var assets = permanentRows
                .Where(r => r.CurrencyId == key.CurrencyId && (AccountType)r.AccountType == AccountType.Asset)
                .Sum(SignedBsAmount);
            var liabilities = permanentRows
                .Where(r => r.CurrencyId == key.CurrencyId && (AccountType)r.AccountType == AccountType.Liability)
                .Sum(SignedBsAmount);
            var equity = permanentRows
                .Where(r => r.CurrencyId == key.CurrencyId && (AccountType)r.AccountType == AccountType.Equity)
                .Sum(SignedBsAmount);
            var assetsBase = permanentRows
                .Where(r => r.CurrencyId == key.CurrencyId && (AccountType)r.AccountType == AccountType.Asset)
                .Sum(SignedBsAmountInBase);
            var liabilitiesBase = permanentRows
                .Where(r => r.CurrencyId == key.CurrencyId && (AccountType)r.AccountType == AccountType.Liability)
                .Sum(SignedBsAmountInBase);
            var equityBase = permanentRows
                .Where(r => r.CurrencyId == key.CurrencyId && (AccountType)r.AccountType == AccountType.Equity)
                .Sum(SignedBsAmountInBase);

            var netIncome = 0m;
            var netIncomeBase = 0m;
            foreach (var row in plRows.Where(r => r.CurrencyId == key.CurrencyId))
            {
                switch ((AccountType)row.AccountType)
                {
                    case AccountType.Revenue:
                        netIncome += SignedPlAmount(row);
                        netIncomeBase += SignedPlAmountInBase(row);
                        break;
                    case AccountType.Expense:
                    case AccountType.Cogs:
                        netIncome -= SignedPlAmount(row);
                        netIncomeBase -= SignedPlAmountInBase(row);
                        break;
                }
            }

            if (Math.Abs(assets) < 0.01m
                && Math.Abs(liabilities) < 0.01m
                && Math.Abs(equity) < 0.01m
                && Math.Abs(netIncome) < 0.01m
                && Math.Abs(assetsBase) < 0.01m
                && Math.Abs(liabilitiesBase) < 0.01m
                && Math.Abs(equityBase) < 0.01m
                && Math.Abs(netIncomeBase) < 0.01m)
            {
                continue;
            }

            result.Add(new
            {
                currencyId = key.CurrencyId,
                currencyCode = key.CurrencyCode,
                symbol = key.Symbol,
                currencyName = key.CurrencyName,
                isBaseCurrency = key.IsBaseCurrency,
                assets,
                liabilities,
                equity,
                currentNetIncome = netIncome,
                equityWithIncome = equity + netIncome,
                assetsInBase = assetsBase,
                liabilitiesInBase = liabilitiesBase,
                equityInBase = equityBase,
                currentNetIncomeInBase = netIncomeBase,
                equityWithIncomeInBase = equityBase + netIncomeBase,
            });
        }

        return result;
    }

    private static object MapAccountGroup(AccountGroup group) => new
    {
        accountId = group.AccountId,
        code = group.Code,
        name = group.Name,
        level = group.Level,
        systemCode = group.SystemCode,
        accountType = (int)group.AccountType,
        nature = (int)group.Nature,
        amountInBase = group.AmountInBase,
        currencies = group.Currencies.Select(c => new
        {
            currencyId = c.CurrencyId,
            currencyCode = c.CurrencyCode,
            symbol = c.Symbol,
            currencyName = c.CurrencyName,
            isBaseCurrency = c.IsBaseCurrency,
            amount = c.Amount,
            amountInBase = c.AmountInBase,
        }).ToList(),
    };

    private static object MapPlCurrencyTotal(PlCurrencyTotal x) => new
    {
        currencyId = x.CurrencyId,
        currencyCode = x.CurrencyCode,
        symbol = x.Symbol,
        currencyName = x.CurrencyName,
        isBaseCurrency = x.IsBaseCurrency,
        revenue = x.Revenue,
        cogs = x.Cogs,
        expense = x.Expense,
        grossProfit = x.GrossProfit,
        netIncome = x.NetIncome,
        revenueInBase = x.RevenueInBase,
        cogsInBase = x.CogsInBase,
        expenseInBase = x.ExpenseInBase,
        grossProfitInBase = x.GrossProfitInBase,
        netIncomeInBase = x.NetIncomeInBase,
    };

    // درآمد: ماهیت بستانکار؛ هزینه/بهای تمام‌شده: ماهیت بدهکار
    private static decimal SignedPlAmount(CurrencyBalanceRow row) =>
        (AccountType)row.AccountType switch
        {
            AccountType.Revenue => row.Credit - row.Debit,
            AccountType.Expense or AccountType.Cogs => row.Debit - row.Credit,
            _ => SignedByNature(row.Debit, row.Credit, (AccountNature)row.Nature),
        };

    private static decimal SignedPlAmountInBase(CurrencyBalanceRow row) =>
        (AccountType)row.AccountType switch
        {
            AccountType.Revenue => row.CreditInBase - row.DebitInBase,
            AccountType.Expense or AccountType.Cogs => row.DebitInBase - row.CreditInBase,
            _ => SignedByNature(row.DebitInBase, row.CreditInBase, (AccountNature)row.Nature),
        };

    private static decimal SignedBsAmount(CurrencyBalanceRow row) =>
        (AccountType)row.AccountType switch
        {
            AccountType.Asset => row.Debit - row.Credit,
            AccountType.Liability or AccountType.Equity => row.Credit - row.Debit,
            _ => SignedByNature(row.Debit, row.Credit, (AccountNature)row.Nature),
        };

    private static decimal SignedBsAmountInBase(CurrencyBalanceRow row) =>
        (AccountType)row.AccountType switch
        {
            AccountType.Asset => row.DebitInBase - row.CreditInBase,
            AccountType.Liability or AccountType.Equity => row.CreditInBase - row.DebitInBase,
            _ => SignedByNature(row.DebitInBase, row.CreditInBase, (AccountNature)row.Nature),
        };

    private static decimal SignedByNature(decimal debit, decimal credit, AccountNature nature) =>
        nature == AccountNature.Credit ? credit - debit : debit - credit;

    private sealed class CurrencyBalanceRow
    {
        public int AccountId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int AccountType { get; set; }
        public int Nature { get; set; }
        public int Level { get; set; }
        public string? SystemCode { get; set; }
        public int CurrencyId { get; set; }
        public string CurrencyCode { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public string CurrencyName { get; set; } = string.Empty;
        public bool IsBaseCurrency { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal DebitInBase { get; set; }
        public decimal CreditInBase { get; set; }
    }

    private sealed class TrialBalanceRow
    {
        public int AccountId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int AccountType { get; set; }
        public int Nature { get; set; }
        public int Level { get; set; }
        public decimal DebitInBase { get; set; }
        public decimal CreditInBase { get; set; }
    }

    private sealed class AgingInvoiceRow
    {
        public int InvoiceId { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int PartyId { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal OpenAmount { get; set; }
        public decimal OpenAmountInBase { get; set; }
    }

    private sealed class CashFlowRow
    {
        public int Source { get; set; }
        public decimal NetChangeInBase { get; set; }
    }

    private sealed record CurrencyAmount(
        int CurrencyId,
        string CurrencyCode,
        string Symbol,
        string CurrencyName,
        bool IsBaseCurrency,
        decimal Amount,
        decimal AmountInBase);

    private sealed record AccountGroup(
        int AccountId,
        string Code,
        string Name,
        int Level,
        string? SystemCode,
        AccountType AccountType,
        AccountNature Nature,
        decimal AmountInBase,
        List<CurrencyAmount> Currencies);

    private sealed record PlCurrencyTotal(
        int CurrencyId,
        string CurrencyCode,
        string Symbol,
        string CurrencyName,
        bool IsBaseCurrency,
        decimal Revenue,
        decimal Cogs,
        decimal Expense,
        decimal GrossProfit,
        decimal NetIncome,
        decimal RevenueInBase,
        decimal CogsInBase,
        decimal ExpenseInBase,
        decimal GrossProfitInBase,
        decimal NetIncomeInBase);
}
