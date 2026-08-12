using HamgamTransport.Server.Data;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public record AccountingIntegrityIssue(string Code, string Message, int? RelatedId = null);

public interface IAccountingIntegrityService
{
    Task<IReadOnlyList<AccountingIntegrityIssue>> CheckAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// بررسی فقط‌خواندنی ناسازگاری‌های دابل‌انتری قبل از پرداکشن.
/// </summary>
public class AccountingIntegrityService : IAccountingIntegrityService
{
    private readonly AppDbContext _db;

    public AccountingIntegrityService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AccountingIntegrityIssue>> CheckAsync(CancellationToken cancellationToken = default)
    {
        var issues = new List<AccountingIntegrityIssue>();

        var expenses = await _db.Expenses
            .AsNoTracking()
            .Where(e => e.IsDeleted != true && e.Amount > 0)
            .Select(e => new { e.ExpenseID, e.JournalEntryId, e.Title })
            .ToListAsync(cancellationToken);

        foreach (var expense in expenses)
        {
            if (expense.JournalEntryId is not int jeId)
            {
                issues.Add(new("ExpenseMissingJournal",
                    $"مصرف «{expense.Title}» بدون سند دفتر است.", expense.ExpenseID));
                continue;
            }

            if (!await HasActiveJournalAsync(jeId, cancellationToken))
            {
                issues.Add(new("ExpenseOrphanJournalLink",
                    $"مصرف «{expense.Title}» به سند نامعتبر/معکوس‌شده لینک شده است.", expense.ExpenseID));
            }
        }

        var revenues = await _db.Revenues
            .AsNoTracking()
            .Where(r => r.IsDeleted != true && r.Amount > 0)
            .Select(r => new { r.RevenueID, r.JournalEntryId, r.Title })
            .ToListAsync(cancellationToken);

        foreach (var revenue in revenues)
        {
            if (revenue.JournalEntryId is not int jeId)
            {
                issues.Add(new("RevenueMissingJournal",
                    $"عاید «{revenue.Title}» بدون سند دفتر است.", revenue.RevenueID));
                continue;
            }

            if (!await HasActiveJournalAsync(jeId, cancellationToken))
            {
                issues.Add(new("RevenueOrphanJournalLink",
                    $"عاید «{revenue.Title}» به سند نامعتبر/معکوس‌شده لینک شده است.", revenue.RevenueID));
            }
        }

        var customers = await _db.Customers
            .AsNoTracking()
            .Where(c => c.IsDeleted != true && c.InitialBalance > 0)
            .Select(c => new { c.CustomerID, c.Name, c.InitialBalance })
            .ToListAsync(cancellationToken);

        foreach (var customer in customers)
        {
            if (!await HasActiveOpeningAsync(JournalSource.CustomerOpeningBalance, customer.CustomerID, cancellationToken))
            {
                issues.Add(new("CustomerOpeningMissing",
                    $"مشتری «{customer.Name}» مانده اولیه دارد ولی سند افتتاحیه فعال ندارد.", customer.CustomerID));
            }
        }

        var suppliers = await _db.Suppliers
            .AsNoTracking()
            .Where(s => s.IsDeleted != true && s.InitialBalance > 0)
            .Select(s => new { s.SupplierID, s.Name, s.InitialBalance })
            .ToListAsync(cancellationToken);

        foreach (var supplier in suppliers)
        {
            if (!await HasActiveOpeningAsync(JournalSource.SupplierOpeningBalance, supplier.SupplierID, cancellationToken))
            {
                issues.Add(new("SupplierOpeningMissing",
                    $"تأمین‌کننده «{supplier.Name}» مانده اولیه دارد ولی سند افتتاحیه فعال ندارد.", supplier.SupplierID));
            }
        }

        var parentPosts = await (
            from line in _db.JournalLines.AsNoTracking()
            join entry in _db.JournalEntries.AsNoTracking() on line.JournalEntryId equals entry.JournalEntryID
            join account in _db.Accounts.AsNoTracking() on line.AccountId equals account.AccountID
            where line.IsDeleted != true
                  && entry.IsDeleted != true
                  && account.IsPostable == false
            select new { entry.JournalEntryID, entry.EntryNumber, account.Code, account.Name }
        ).Take(50).ToListAsync(cancellationToken);

        foreach (var row in parentPosts)
        {
            issues.Add(new("PostedOnNonPostableAccount",
                $"سند {row.EntryNumber} روی حساب غیرقابل‌ثبت {row.Code} ({row.Name}) ثبت شده.", row.JournalEntryID));
        }

        var softDeletedPosted = await _db.JournalEntries
            .AsNoTracking()
            .Where(e => e.IsDeleted == true && e.IsPosted)
            .Select(e => new { e.JournalEntryID, e.EntryNumber })
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var row in softDeletedPosted)
        {
            issues.Add(new("PostedSoftDeleted",
                $"سند Posted با شماره {row.EntryNumber} soft-delete شده (باید معکوس می‌شد).", row.JournalEntryID));
        }

        return issues;
    }

    private async Task<bool> HasActiveJournalAsync(int journalEntryId, CancellationToken cancellationToken)
    {
        var entry = await _db.JournalEntries.AsNoTracking()
            .FirstOrDefaultAsync(e => e.JournalEntryID == journalEntryId && e.IsDeleted != true, cancellationToken);
        if (entry is null || !entry.IsPosted)
        {
            return false;
        }

        var reversed = await _db.JournalEntries.AnyAsync(
            e => e.IsDeleted != true
                 && e.Source == JournalSource.ManualReversal
                 && e.SourceId == journalEntryId,
            cancellationToken);
        return !reversed;
    }

    private async Task<bool> HasActiveOpeningAsync(
        JournalSource source,
        int sourceId,
        CancellationToken cancellationToken)
    {
        var openingIds = await _db.JournalEntries
            .Where(e => e.IsDeleted != true
                        && e.IsPosted
                        && e.Source == source
                        && e.SourceId == sourceId)
            .Select(e => e.JournalEntryID)
            .ToListAsync(cancellationToken);

        if (openingIds.Count == 0)
        {
            return false;
        }

        var reversedIds = await _db.JournalEntries
            .Where(e => e.IsDeleted != true
                        && e.Source == JournalSource.ManualReversal
                        && e.SourceId != null
                        && openingIds.Contains(e.SourceId.Value))
            .Select(e => e.SourceId!.Value)
            .ToListAsync(cancellationToken);

        return openingIds.Any(id => !reversedIds.Contains(id));
    }
}
