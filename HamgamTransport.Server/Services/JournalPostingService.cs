using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public record JournalLineDraft(
    int AccountId,
    decimal Debit,
    decimal Credit,
    decimal DebitInBaseCurrency,
    decimal CreditInBaseCurrency,
    int CurrencyId,
    string? Description = null,
    int? CashBoxId = null,
    int? PartyId = null,
    int? CostCenterId = null);

public interface IJournalPostingService
{
    Task<JournalEntry> PostAsync(
        DateTime entryDate,
        string description,
        JournalSource source,
        int? sourceId,
        int baseCurrencyId,
        IReadOnlyList<JournalLineDraft> lines,
        int? userId,
        CancellationToken cancellationToken = default);

    Task SoftDeleteBySourceAsync(
        JournalSource source,
        int sourceId,
        int? userId,
        CancellationToken cancellationToken = default);

    // اسناد Posted را معکوس می‌کند؛ پیش‌نویس‌های ثبت‌نشده را soft-delete می‌کند
    Task ReverseBySourceAsync(
        JournalSource source,
        int sourceId,
        int? userId,
        DateTime? reverseDate = null,
        CancellationToken cancellationToken = default);

    Task SoftDeleteEntryAsync(
        int journalEntryId,
        int? userId,
        CancellationToken cancellationToken = default);

    // معکوس سند Posted — خطوط مخالف، سند اصلی دست‌نخورده می‌ماند
    Task<JournalEntry> ReverseEntryAsync(
        int journalEntryId,
        int? userId,
        DateTime? reverseDate = null,
        CancellationToken cancellationToken = default);
}

public class JournalPostingService : IJournalPostingService
{
    private readonly AppDbContext _db;

    public JournalPostingService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<JournalEntry> PostAsync(
        DateTime entryDate,
        string description,
        JournalSource source,
        int? sourceId,
        int baseCurrencyId,
        IReadOnlyList<JournalLineDraft> lines,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        await EnsureFiscalYearOpenAsync(entryDate, source, cancellationToken);
        await EnsureFiscalPeriodOpenAsync(entryDate, source, cancellationToken);

        if (lines is null || lines.Count == 0)
        {
            throw new InvalidOperationException("سند حسابداری باید حداقل یک ردیف داشته باشد.");
        }

        foreach (var line in lines)
        {
            if (line.Debit < 0 || line.Credit < 0 || line.DebitInBaseCurrency < 0 || line.CreditInBaseCurrency < 0)
            {
                throw new InvalidOperationException("مبالغ بدهکار و بستانکار نمی‌توانند منفی باشند.");
            }

            if ((line.Debit > 0 && line.Credit > 0) || (line.Debit == 0 && line.Credit == 0))
            {
                throw new InvalidOperationException("هر ردیف سند باید فقط بدهکار یا فقط بستانکار باشد.");
            }

            if ((line.DebitInBaseCurrency > 0 && line.CreditInBaseCurrency > 0)
                || (line.DebitInBaseCurrency == 0 && line.CreditInBaseCurrency == 0))
            {
                throw new InvalidOperationException("هر ردیف سند در ارز پایه باید فقط بدهکار یا فقط بستانکار باشد.");
            }
        }

        var totalDebit = lines.Sum(l => l.DebitInBaseCurrency);
        var totalCredit = lines.Sum(l => l.CreditInBaseCurrency);
        if (Math.Abs(totalDebit - totalCredit) > 0.01m)
        {
            throw new InvalidOperationException(
                $"سند نامتوازن است. بدهکار: {totalDebit:N2} — بستانکار: {totalCredit:N2}");
        }

        var accountIds = lines.Select(l => l.AccountId).Distinct().ToList();
        var accounts = await _db.Accounts
            .Where(a => accountIds.Contains(a.AccountID) && a.IsDeleted != true)
            .ToListAsync(cancellationToken);

        if (accounts.Count != accountIds.Count)
        {
            throw new InvalidOperationException("یکی از حساب‌های سند یافت نشد.");
        }

        if (accounts.Any(a => !a.IsPostable))
        {
            throw new InvalidOperationException("ثبت فقط روی حساب‌های قابل‌ثبت (معین/تفصیلی) مجاز است.");
        }

        var now = DateTime.Now;
        var entry = new JournalEntry
        {
            EntryNumber = await NextEntryNumberAsync(cancellationToken),
            EntryDate = entryDate,
            Description = description,
            Source = source,
            SourceId = sourceId,
            BaseCurrencyId = baseCurrencyId,
            TotalDebitInBaseCurrency = totalDebit,
            TotalCreditInBaseCurrency = totalCredit,
            IsPosted = true,
            PostedAt = now,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        var lineNo = 1;
        foreach (var draft in lines)
        {
            entry.Lines.Add(new JournalLine
            {
                AccountId = draft.AccountId,
                LineNo = lineNo++,
                Description = draft.Description,
                CurrencyId = draft.CurrencyId,
                Debit = draft.Debit,
                Credit = draft.Credit,
                DebitInBaseCurrency = draft.DebitInBaseCurrency,
                CreditInBaseCurrency = draft.CreditInBaseCurrency,
                CashBoxId = draft.CashBoxId,
                PartyId = draft.PartyId,
                CostCenterId = draft.CostCenterId,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = now,
                CreatedBy = userId,
            });
        }

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return entry;
    }

    public Task SoftDeleteBySourceAsync(
        JournalSource source,
        int sourceId,
        int? userId,
        CancellationToken cancellationToken = default) =>
        // سازگاری قدیمی: اسناد Posted باید معکوس شوند نه پاک
        ReverseBySourceAsync(source, sourceId, userId, null, cancellationToken);

    public async Task ReverseBySourceAsync(
        JournalSource source,
        int sourceId,
        int? userId,
        DateTime? reverseDate = null,
        CancellationToken cancellationToken = default)
    {
        var entries = await _db.JournalEntries
            .Include(e => e.Lines)
            .Where(e => e.Source == source && e.SourceId == sourceId && e.IsDeleted != true)
            .OrderBy(e => e.JournalEntryID)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;
        var draftIds = new List<int>();

        foreach (var entry in entries)
        {
            if (!entry.IsPosted)
            {
                SoftDeleteEntryCore(entry, userId, now);
                draftIds.Add(entry.JournalEntryID);
                continue;
            }

            var alreadyReversed = await _db.JournalEntries.AnyAsync(
                e => e.Source == JournalSource.ManualReversal
                     && e.SourceId == entry.JournalEntryID
                     && e.IsDeleted != true,
                cancellationToken);
            if (alreadyReversed)
            {
                continue;
            }

            await ReverseEntryAsync(entry.JournalEntryID, userId, reverseDate, cancellationToken);
        }

        if (draftIds.Count > 0)
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task SoftDeleteEntryAsync(
        int journalEntryId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var entry = await _db.JournalEntries
            .Include(e => e.Lines)
            .FirstOrDefaultAsync(e => e.JournalEntryID == journalEntryId && e.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سند یافت نشد.");

        if (entry.Source != JournalSource.Manual && entry.Source != JournalSource.ManualReversal)
        {
            throw new InvalidOperationException("فقط اسناد دستی از این مسیر قابل حذف هستند.");
        }

        // سند Posted با معکوس ابطال می‌شود — حذف مستقیم ممنوع
        if (entry.IsPosted)
        {
            await ReverseEntryAsync(journalEntryId, userId, null, cancellationToken);
            return;
        }

        SoftDeleteEntryCore(entry, userId, DateTime.Now);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<JournalEntry> ReverseEntryAsync(
        int journalEntryId,
        int? userId,
        DateTime? reverseDate = null,
        CancellationToken cancellationToken = default)
    {
        var original = await _db.JournalEntries
            .Include(e => e.Lines.Where(l => l.IsDeleted != true))
            .FirstOrDefaultAsync(e => e.JournalEntryID == journalEntryId && e.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سند یافت نشد.");

        if (!original.IsPosted)
        {
            throw new InvalidOperationException("فقط سند ثبت‌شده قابل معکوس است.");
        }

        var alreadyReversed = await _db.JournalEntries.AnyAsync(
            e => e.Source == JournalSource.ManualReversal
                 && e.SourceId == original.JournalEntryID
                 && e.IsDeleted != true,
            cancellationToken);
        if (alreadyReversed)
        {
            throw new InvalidOperationException("این سند قبلاً معکوس شده است.");
        }

        var drafts = original.Lines
            .OrderBy(l => l.LineNo)
            .Select(l => new JournalLineDraft(
                l.AccountId,
                l.Credit,
                l.Debit,
                l.CreditInBaseCurrency,
                l.DebitInBaseCurrency,
                l.CurrencyId,
                string.IsNullOrWhiteSpace(l.Description) ? $"معکوس {original.EntryNumber}" : $"معکوس — {l.Description}",
                l.CashBoxId,
                l.PartyId,
                l.CostCenterId))
            .ToList();

        return await PostAsync(
            reverseDate ?? DateTime.Now,
            $"معکوس سند {original.EntryNumber}",
            JournalSource.ManualReversal,
            original.JournalEntryID,
            original.BaseCurrencyId,
            drafts,
            userId,
            cancellationToken);
    }

    private static void SoftDeleteEntryCore(JournalEntry entry, int? userId, DateTime now)
    {
        entry.IsDeleted = true;
        entry.IsActive = false;
        entry.DeletedAt = now;
        entry.DeletedBy = userId;

        foreach (var line in entry.Lines)
        {
            line.IsDeleted = true;
            line.IsActive = false;
            line.DeletedAt = now;
            line.DeletedBy = userId;
        }
    }

    private async Task EnsureFiscalYearOpenAsync(
        DateTime entryDate,
        JournalSource source,
        CancellationToken cancellationToken)
    {
        if (IsClosingSource(source))
        {
            return;
        }

        var isClosed = await _db.FiscalYears.AnyAsync(
            y => y.IsDeleted != true
                 && y.Status == FiscalYearStatus.Closed
                 && entryDate >= y.StartDate
                 && entryDate <= y.EndDate,
            cancellationToken);

        if (isClosed)
        {
            var solar = JalaliDateHelper.GetSolarYear(entryDate);
            throw new InvalidOperationException(
                $"سال مالی {solar} بسته است؛ ثبت سند با تاریخ داخل این سال مجاز نیست.");
        }
    }

    private async Task EnsureFiscalPeriodOpenAsync(
        DateTime entryDate,
        JournalSource source,
        CancellationToken cancellationToken)
    {
        if (IsClosingSource(source))
        {
            return;
        }

        var solarYear = JalaliDateHelper.GetSolarYear(entryDate);
        var month = JalaliDateHelper.GetSolarMonth(entryDate);

        var closed = await _db.FiscalPeriods.AnyAsync(
            p => p.IsDeleted != true
                 && p.SolarYear == solarYear
                 && p.Month == month
                 && p.Status == FiscalYearStatus.Closed,
            cancellationToken);

        if (closed)
        {
            throw new InvalidOperationException(
                $"دوره مالی {solarYear}/{month:D2} بسته است؛ ثبت سند در این ماه مجاز نیست.");
        }
    }

    private static bool IsClosingSource(JournalSource source) =>
        source is JournalSource.YearEndClosing
            or JournalSource.YearEndReversal
            or JournalSource.EquityYearAllocation
            or JournalSource.EquityYearAllocationReversal
            or JournalSource.ManualReversal;

    private async Task<string> NextEntryNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.Now.Year;
        var prefix = $"JE-{year}-";
        var last = await _db.JournalEntries
            .AsNoTracking()
            .Where(e => e.EntryNumber.StartsWith(prefix) && e.IsDeleted != true)
            .OrderByDescending(e => e.EntryNumber)
            .Select(e => e.EntryNumber)
            .FirstOrDefaultAsync(cancellationToken);

        var next = 1;
        if (last is not null && last.Length > prefix.Length
            && int.TryParse(last[prefix.Length..], out var n))
        {
            next = n + 1;
        }

        return $"{prefix}{next:D6}";
    }
}
