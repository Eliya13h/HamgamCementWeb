using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public record FiscalYearClosingPreview(
    int FiscalYearId,
    int SolarYear,
    DateTime StartDate,
    DateTime EndDate,
    decimal TotalRevenueInBase,
    decimal TotalExpenseInBase,
    decimal TotalCogsInBase,
    decimal NetIncomeInBase,
    int TemporaryAccountCount);

public interface IFiscalYearCloseService
{
    Task EnsureCurrentYearsAsync(CancellationToken cancellationToken = default);
    Task<FiscalYearClosingPreview> GetClosingPreviewAsync(int fiscalYearId, CancellationToken cancellationToken = default);
    Task<FiscalYear> CloseAsync(int fiscalYearId, int adminUserId, CancellationToken cancellationToken = default);
    Task<FiscalYear> ReopenAsync(int fiscalYearId, int adminUserId, CancellationToken cancellationToken = default);
}

public class FiscalYearCloseService : IFiscalYearCloseService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICurrencyConversionService _currencies;
    private readonly IShareholderEquityPostingService _equity;

    public FiscalYearCloseService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICurrencyConversionService currencies,
        IShareholderEquityPostingService equity)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _currencies = currencies;
        _equity = equity;
    }

    public async Task EnsureCurrentYearsAsync(CancellationToken cancellationToken = default)
    {
        var currentSolar = JalaliDateHelper.GetSolarYear(DateTime.Today);
        // سال جاری و سال قبل را در صورت نبودن بساز تا لیست خالی نماند
        foreach (var year in new[] { currentSolar - 1, currentSolar })
        {
            if (year < 1300)
            {
                continue;
            }

            var exists = await _db.FiscalYears
                .AnyAsync(y => y.SolarYear == year && y.IsDeleted != true, cancellationToken);
            if (exists)
            {
                continue;
            }

            var (start, end) = JalaliDateHelper.GetSolarYearRange(year);
            _db.FiscalYears.Add(new FiscalYear
            {
                SolarYear = year,
                StartDate = start,
                EndDate = end,
                Status = FiscalYearStatus.Open,
                NetIncomeInBaseCurrency = 0,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.Now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FiscalYearClosingPreview> GetClosingPreviewAsync(
        int fiscalYearId,
        CancellationToken cancellationToken = default)
    {
        var year = await GetRequiredYearAsync(fiscalYearId, cancellationToken);
        var totals = await ComputeTemporaryTotalsAsync(year.StartDate, year.EndDate, cancellationToken);
        return new FiscalYearClosingPreview(
            year.FiscalYearID,
            year.SolarYear,
            year.StartDate,
            year.EndDate,
            totals.Revenue,
            totals.Expense,
            totals.Cogs,
            totals.NetIncome,
            totals.AccountCount);
    }

    public async Task<FiscalYear> CloseAsync(
        int fiscalYearId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var year = await GetRequiredYearAsync(fiscalYearId, cancellationToken);
        if (year.Status == FiscalYearStatus.Closed)
        {
            throw new InvalidOperationException("این سال مالی قبلاً بسته شده است.");
        }

        var balances = await GetTemporaryAccountBalancesAsync(year.StartDate, year.EndDate, cancellationToken);
        var totals = Summarize(balances);
        JournalEntry? closingEntry = null;
        JournalEntry? allocationEntry = null;
        var baseCurrency = await _currencies.GetBaseCurrencyAsync(cancellationToken);

        // قبل از هر پست: اگر قرار است تخصیص انجام شود، درصدها باید معتبر باشند
        if (Math.Abs(totals.NetIncome) >= 0.01m)
        {
            await _equity.ValidateSharePercentagesAsync(cancellationToken);
        }

        if (balances.Count > 0)
        {
            var retained = await _accounts.ResolveRetainedEarningsPostableAsync(cancellationToken);
            var drafts = new List<JournalLineDraft>();

            foreach (var row in balances)
            {
                var netDoc = row.Debit - row.Credit;
                var netBase = row.DebitInBase - row.CreditInBase;
                if (Math.Abs(netDoc) < 0.01m && Math.Abs(netBase) < 0.01m)
                {
                    continue;
                }

                // اگر مانده ارزی صفر و فقط معادل پایه مانده باشد، با ارز پایه می‌بندیم
                var currencyId = Math.Abs(netDoc) >= 0.01m ? row.CurrencyId : baseCurrency.CurrencyID;
                var docAmount = Math.Abs(netDoc) >= 0.01m ? Math.Abs(netDoc) : Math.Abs(netBase);
                var baseAmount = Math.Abs(netBase) >= 0.01m ? Math.Abs(netBase) : docAmount;
                var isDebitBalance = netBase > 0.005m || (Math.Abs(netBase) < 0.005m && netDoc > 0);

                // مانده بدهکار → بستانکار می‌کنیم تا صفر شود؛ مانده بستانکار → بدهکار
                if (isDebitBalance)
                {
                    drafts.Add(new JournalLineDraft(
                        row.AccountId,
                        0,
                        docAmount,
                        0,
                        baseAmount,
                        currencyId,
                        $"اختتام حساب {row.Code} ({row.CurrencyCode})"));
                }
                else
                {
                    drafts.Add(new JournalLineDraft(
                        row.AccountId,
                        docAmount,
                        0,
                        baseAmount,
                        0,
                        currencyId,
                        $"اختتام حساب {row.Code} ({row.CurrencyCode})"));
                }
            }

            // مابه‌التفاوت به سود انباشته
            var balancing = drafts.Sum(d => d.DebitInBaseCurrency) - drafts.Sum(d => d.CreditInBaseCurrency);
            if (Math.Abs(balancing) >= 0.01m)
            {
                if (balancing > 0)
                {
                    // بدهکار بیشتر بوده → بستانکار سود انباشته (سود)
                    drafts.Add(new JournalLineDraft(
                        retained.AccountID,
                        0,
                        balancing,
                        0,
                        balancing,
                        baseCurrency.CurrencyID,
                        "انتقال سود/زیان به سود انباشته"));
                }
                else
                {
                    var amount = Math.Abs(balancing);
                    drafts.Add(new JournalLineDraft(
                        retained.AccountID,
                        amount,
                        0,
                        amount,
                        0,
                        baseCurrency.CurrencyID,
                        "انتقال سود/زیان به سود انباشته"));
                }
            }

            if (drafts.Count > 0)
            {
                closingEntry = await _journal.PostAsync(
                    year.EndDate.Date,
                    $"سند اختتام سال مالی {year.SolarYear}",
                    JournalSource.YearEndClosing,
                    year.FiscalYearID,
                    baseCurrency.CurrencyID,
                    drafts,
                    adminUserId,
                    cancellationToken);
            }
        }

        // تخصیص سود/زیان خالص به تفصیلی سرمایه هر سهامدار
        if (Math.Abs(totals.NetIncome) >= 0.01m)
        {
            allocationEntry = await _equity.PostYearAllocationAsync(
                year,
                totals.NetIncome,
                baseCurrency.CurrencyID,
                adminUserId,
                cancellationToken);
        }

        var now = DateTime.Now;
        year.Status = FiscalYearStatus.Closed;
        year.ClosedAt = now;
        year.ClosedByUserId = adminUserId;
        year.ClosingJournalEntryId = closingEntry?.JournalEntryID;
        year.EquityAllocationJournalEntryId = allocationEntry?.JournalEntryID;
        year.NetIncomeInBaseCurrency = totals.NetIncome;
        year.UpdatedAt = now;
        year.UpdatedBy = adminUserId;
        year.IsUpdated = true;

        await _db.SaveChangesAsync(cancellationToken);

        // سال شمسی بعدی را در صورت نبودن باز کن تا کار عملیاتی قطع نشود
        await EnsureYearExistsAsync(year.SolarYear + 1, cancellationToken);

        return year;
    }

    public async Task<FiscalYear> ReopenAsync(
        int fiscalYearId,
        int adminUserId,
        CancellationToken cancellationToken = default)
    {
        var year = await GetRequiredYearAsync(fiscalYearId, cancellationToken);
        if (year.Status != FiscalYearStatus.Closed)
        {
            throw new InvalidOperationException("این سال مالی باز است و نیازی به بازگشایی ندارد.");
        }

        // اول معکوس تخصیص سرمایه، بعد معکوس اختتام
        await ReverseJournalIfExistsAsync(
            year.EquityAllocationJournalEntryId,
            year,
            JournalSource.EquityYearAllocationReversal,
            $"معکوس تخصیص سرمایه سال مالی {year.SolarYear}",
            adminUserId,
            cancellationToken);

        await ReverseJournalIfExistsAsync(
            year.ClosingJournalEntryId,
            year,
            JournalSource.YearEndReversal,
            $"معکوس اختتام سال مالی {year.SolarYear}",
            adminUserId,
            cancellationToken);

        var now = DateTime.Now;
        year.Status = FiscalYearStatus.Open;
        year.ClosedAt = null;
        year.ClosedByUserId = null;
        year.ClosingJournalEntryId = null;
        year.EquityAllocationJournalEntryId = null;
        year.NetIncomeInBaseCurrency = 0;
        year.UpdatedAt = now;
        year.UpdatedBy = adminUserId;
        year.IsUpdated = true;

        await _db.SaveChangesAsync(cancellationToken);
        return year;
    }

    private async Task ReverseJournalIfExistsAsync(
        int? journalEntryId,
        FiscalYear year,
        JournalSource reversalSource,
        string description,
        int adminUserId,
        CancellationToken cancellationToken)
    {
        if (journalEntryId is not int closingId)
        {
            return;
        }

        var original = await _db.JournalEntries
            .AsNoTracking()
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(
                e => e.JournalEntryID == closingId && e.IsDeleted != true,
                cancellationToken);

        if (original is null)
        {
            return;
        }

        var baseCurrency = await _currencies.GetBaseCurrencyAsync(cancellationToken);
        var reverseDrafts = original.Lines
            .Where(l => l.IsDeleted != true)
            .Select(l => new JournalLineDraft(
                l.AccountId,
                l.Credit,
                l.Debit,
                l.CreditInBaseCurrency,
                l.DebitInBaseCurrency,
                l.CurrencyId > 0 ? l.CurrencyId : baseCurrency.CurrencyID,
                $"معکوس: {l.Description}"))
            .ToList();

        if (reverseDrafts.Count == 0)
        {
            return;
        }

        await _journal.PostAsync(
            year.EndDate.Date,
            description,
            reversalSource,
            year.FiscalYearID,
            original.BaseCurrencyId,
            reverseDrafts,
            adminUserId,
            cancellationToken);
    }

    private async Task EnsureYearExistsAsync(int solarYear, CancellationToken cancellationToken)
    {
        var exists = await _db.FiscalYears
            .AnyAsync(y => y.SolarYear == solarYear && y.IsDeleted != true, cancellationToken);
        if (exists)
        {
            return;
        }

        var (start, end) = JalaliDateHelper.GetSolarYearRange(solarYear);
        _db.FiscalYears.Add(new FiscalYear
        {
            SolarYear = solarYear,
            StartDate = start,
            EndDate = end,
            Status = FiscalYearStatus.Open,
            NetIncomeInBaseCurrency = 0,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = DateTime.Now,
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<FiscalYear> GetRequiredYearAsync(int fiscalYearId, CancellationToken cancellationToken)
    {
        return await _db.FiscalYears
            .FirstOrDefaultAsync(y => y.FiscalYearID == fiscalYearId && y.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سال مالی یافت نشد.");
    }

    private async Task<List<TempBalanceRow>> GetTemporaryAccountBalancesAsync(
        DateTime start,
        DateTime end,
        CancellationToken cancellationToken)
    {
        var rows = await (
            from line in _db.JournalLines.AsNoTracking()
            join entry in _db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.JournalEntryID
            join account in _db.Accounts.AsNoTracking() on line.AccountId equals account.AccountID
            join currency in _db.Currencies.AsNoTracking() on line.CurrencyId equals currency.CurrencyID
            where entry.IsDeleted != true
                  && entry.IsPosted
                  && line.IsDeleted != true
                  && account.IsDeleted != true
                  && account.IsPostable
                  && currency.IsDeleted != true
                  && entry.EntryDate >= start
                  && entry.EntryDate <= end
                  && (account.AccountType == AccountType.Revenue
                      || account.AccountType == AccountType.Expense
                      || account.AccountType == AccountType.Cogs)
                  && entry.Source != JournalSource.YearEndClosing
                  && entry.Source != JournalSource.YearEndReversal
            group new { line, currency } by new
            {
                line.AccountId,
                account.Code,
                account.Name,
                account.AccountType,
                line.CurrencyId,
                currency.CurrencyCode,
            } into g
            select new TempBalanceRow(
                g.Key.AccountId,
                g.Key.Code,
                g.Key.Name,
                g.Key.AccountType,
                g.Key.CurrencyId,
                g.Key.CurrencyCode,
                g.Sum(x => x.line.Debit),
                g.Sum(x => x.line.Credit),
                g.Sum(x => x.line.DebitInBaseCurrency),
                g.Sum(x => x.line.CreditInBaseCurrency))
        ).ToListAsync(cancellationToken);

        return rows
            .Where(r => Math.Abs(r.DebitInBase - r.CreditInBase) >= 0.01m
                        || Math.Abs(r.Debit - r.Credit) >= 0.01m)
            .OrderBy(r => r.Code)
            .ThenBy(r => r.CurrencyCode)
            .ToList();
    }

    private async Task<(decimal Revenue, decimal Expense, decimal Cogs, decimal NetIncome, int AccountCount)>
        ComputeTemporaryTotalsAsync(DateTime start, DateTime end, CancellationToken cancellationToken)
    {
        var balances = await GetTemporaryAccountBalancesAsync(start, end, cancellationToken);
        var totals = Summarize(balances);
        return (totals.Revenue, totals.Expense, totals.Cogs, totals.NetIncome, balances.Count);
    }

    private static (decimal Revenue, decimal Expense, decimal Cogs, decimal NetIncome) Summarize(
        IReadOnlyList<TempBalanceRow> balances)
    {
        decimal revenue = 0;
        decimal expense = 0;
        decimal cogs = 0;

        foreach (var row in balances)
        {
            var creditNature = row.CreditInBase - row.DebitInBase;
            var debitNature = row.DebitInBase - row.CreditInBase;
            switch (row.AccountType)
            {
                case AccountType.Revenue:
                    revenue += creditNature;
                    break;
                case AccountType.Expense:
                    expense += debitNature;
                    break;
                case AccountType.Cogs:
                    cogs += debitNature;
                    break;
            }
        }

        var netIncome = revenue - expense - cogs;
        return (revenue, expense, cogs, netIncome);
    }

    private sealed record TempBalanceRow(
        int AccountId,
        string Code,
        string Name,
        AccountType AccountType,
        int CurrencyId,
        string CurrencyCode,
        decimal Debit,
        decimal Credit,
        decimal DebitInBase,
        decimal CreditInBase);
}
