using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public interface IPartyOpeningBalanceService
{
    Task<JournalEntry> PostCustomerOpeningAsync(
        int customerId,
        string customerName,
        decimal amountInBase,
        DateTime? entryDate,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<JournalEntry> PostSupplierOpeningAsync(
        int supplierId,
        string supplierName,
        decimal amountInBase,
        DateTime? entryDate,
        int? userId,
        CancellationToken cancellationToken = default);

    Task<bool> HasCustomerOpeningAsync(int customerId, CancellationToken cancellationToken = default);
    Task<bool> HasSupplierOpeningAsync(int supplierId, CancellationToken cancellationToken = default);

    // معکوس سند افتتاحیه فعال (در صورت وجود)
    Task ReverseCustomerOpeningAsync(int customerId, int? userId, CancellationToken cancellationToken = default);
    Task ReverseSupplierOpeningAsync(int supplierId, int? userId, CancellationToken cancellationToken = default);

    // همگام‌سازی مانده اولیه با دفتر: reverse قبلی + post مبلغ جدید در صورت ≠ 0
    Task SyncCustomerOpeningAsync(
        int customerId,
        string customerName,
        decimal amountInBase,
        int? userId,
        CancellationToken cancellationToken = default);

    Task SyncSupplierOpeningAsync(
        int supplierId,
        string supplierName,
        decimal amountInBase,
        int? userId,
        CancellationToken cancellationToken = default);

    // آیا طرف غیر از افتتاحیه گردش دفتر دارد؟
    Task<bool> HasCustomerGlActivityAsync(int customerId, CancellationToken cancellationToken = default);
    Task<bool> HasSupplierGlActivityAsync(int supplierId, CancellationToken cancellationToken = default);
}

public class PartyOpeningBalanceService : IPartyOpeningBalanceService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICurrencyConversionService _currency;

    public PartyOpeningBalanceService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICurrencyConversionService currency)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _currency = currency;
    }

    public Task<bool> HasCustomerOpeningAsync(int customerId, CancellationToken cancellationToken = default) =>
        HasActiveOpeningAsync(JournalSource.CustomerOpeningBalance, customerId, cancellationToken);

    public Task<bool> HasSupplierOpeningAsync(int supplierId, CancellationToken cancellationToken = default) =>
        HasActiveOpeningAsync(JournalSource.SupplierOpeningBalance, supplierId, cancellationToken);

    public async Task ReverseCustomerOpeningAsync(int customerId, int? userId, CancellationToken cancellationToken = default) =>
        await _journal.ReverseBySourceAsync(JournalSource.CustomerOpeningBalance, customerId, userId, cancellationToken: cancellationToken);

    public async Task ReverseSupplierOpeningAsync(int supplierId, int? userId, CancellationToken cancellationToken = default) =>
        await _journal.ReverseBySourceAsync(JournalSource.SupplierOpeningBalance, supplierId, userId, cancellationToken: cancellationToken);

    public async Task SyncCustomerOpeningAsync(
        int customerId,
        string customerName,
        decimal amountInBase,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        await ReverseCustomerOpeningAsync(customerId, userId, cancellationToken);
        if (amountInBase != 0)
        {
            await PostCustomerOpeningAsync(customerId, customerName, amountInBase, DateTime.Today, userId, cancellationToken);
        }
    }

    public async Task SyncSupplierOpeningAsync(
        int supplierId,
        string supplierName,
        decimal amountInBase,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        await ReverseSupplierOpeningAsync(supplierId, userId, cancellationToken);
        if (amountInBase != 0)
        {
            await PostSupplierOpeningAsync(supplierId, supplierName, amountInBase, DateTime.Today, userId, cancellationToken);
        }
    }

    public async Task<bool> HasCustomerGlActivityAsync(int customerId, CancellationToken cancellationToken = default) =>
        await HasPartyLineActivityAsync(customerId, JournalSource.CustomerOpeningBalance, cancellationToken);

    public async Task<bool> HasSupplierGlActivityAsync(int supplierId, CancellationToken cancellationToken = default) =>
        await HasPartyLineActivityAsync(supplierId, JournalSource.SupplierOpeningBalance, cancellationToken);

    public async Task<JournalEntry> PostCustomerOpeningAsync(
        int customerId,
        string customerName,
        decimal amountInBase,
        DateTime? entryDate,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (amountInBase == 0)
        {
            throw new InvalidOperationException("مانده اولیه برای ثبت در دفتر نمی‌تواند صفر باشد.");
        }

        if (await HasCustomerOpeningAsync(customerId, cancellationToken))
        {
            throw new InvalidOperationException("مانده اولیه این مشتری قبلاً در دفتر ثبت شده است.");
        }

        var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
        var partyAccount = await _accounts.EnsureCustomerAccountAsync(customerId, customerName, cancellationToken);
        var openingAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.EquityOpening, cancellationToken);
        var date = (entryDate ?? DateTime.Today).Date;
        var abs = Math.Abs(amountInBase);
        var currencyId = baseCurrency.CurrencyID;

        // قرارداد بالانس مشتری: مثبت=طلبکار، منفی=بدهکار (مشتری به ما بدهکار است)
        List<JournalLineDraft> lines;
        if (amountInBase < 0)
        {
            // بدهکار: بدهکار دریافتنی مشتری — بستانکار افتتاحیه
            lines =
            [
                new(partyAccount.AccountID, abs, 0, abs, 0, currencyId,
                    $"مانده اولیه بدهکار مشتری — {customerName}", PartyId: customerId),
                new(openingAccount.AccountID, 0, abs, 0, abs, currencyId,
                    $"طرف مقابل مانده اولیه بدهکار مشتری — {customerName}"),
            ];
        }
        else
        {
            // طلبکار: بدهکار افتتاحیه — بستانکار دریافتنی مشتری
            lines =
            [
                new(openingAccount.AccountID, abs, 0, abs, 0, currencyId,
                    $"طرف مقابل مانده اولیه طلبکار مشتری — {customerName}"),
                new(partyAccount.AccountID, 0, abs, 0, abs, currencyId,
                    $"مانده اولیه طلبکار مشتری — {customerName}", PartyId: customerId),
            ];
        }

        return await _journal.PostAsync(
            date,
            $"مانده اولیه مشتری — {customerName}",
            JournalSource.CustomerOpeningBalance,
            customerId,
            currencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostSupplierOpeningAsync(
        int supplierId,
        string supplierName,
        decimal amountInBase,
        DateTime? entryDate,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (amountInBase == 0)
        {
            throw new InvalidOperationException("مانده اولیه برای ثبت در دفتر نمی‌تواند صفر باشد.");
        }

        if (await HasSupplierOpeningAsync(supplierId, cancellationToken))
        {
            throw new InvalidOperationException("مانده اولیه این تأمین‌کننده قبلاً در دفتر ثبت شده است.");
        }

        var baseCurrency = await _currency.GetBaseCurrencyAsync(cancellationToken);
        var partyAccount = await _accounts.EnsureSupplierAccountAsync(supplierId, supplierName, cancellationToken);
        var openingAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.EquityOpening, cancellationToken);
        var date = (entryDate ?? DateTime.Today).Date;
        var abs = Math.Abs(amountInBase);
        var currencyId = baseCurrency.CurrencyID;

        // قرارداد بالانس تأمین‌کننده: مثبت=طلبکار (ما بدهکاریم)، منفی=بدهکار
        List<JournalLineDraft> lines;
        if (amountInBase > 0)
        {
            // طلبکار: بدهکار افتتاحیه — بستانکار پرداختنی تأمین‌کننده
            lines =
            [
                new(openingAccount.AccountID, abs, 0, abs, 0, currencyId,
                    $"طرف مقابل مانده اولیه تأمین‌کننده — {supplierName}"),
                new(partyAccount.AccountID, 0, abs, 0, abs, currencyId,
                    $"مانده اولیه تأمین‌کننده — {supplierName}", PartyId: supplierId),
            ];
        }
        else
        {
            // بدهکار: بدهکار پرداختنی تأمین‌کننده — بستانکار افتتاحیه
            lines =
            [
                new(partyAccount.AccountID, abs, 0, abs, 0, currencyId,
                    $"مانده اولیه بدهکار تأمین‌کننده — {supplierName}", PartyId: supplierId),
                new(openingAccount.AccountID, 0, abs, 0, abs, currencyId,
                    $"طرف مقابل مانده اولیه بدهکار تأمین‌کننده — {supplierName}"),
            ];
        }

        return await _journal.PostAsync(
            date,
            $"مانده اولیه تأمین‌کننده — {supplierName}",
            JournalSource.SupplierOpeningBalance,
            supplierId,
            currencyId,
            lines,
            userId,
            cancellationToken);
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

    private async Task<bool> HasPartyLineActivityAsync(
        int partyId,
        JournalSource openingSource,
        CancellationToken cancellationToken)
    {
        var openingEntryIds = await _db.JournalEntries
            .Where(e => e.IsDeleted != true
                        && e.Source == openingSource
                        && e.SourceId == partyId)
            .Select(e => e.JournalEntryID)
            .ToListAsync(cancellationToken);

        var reversalIds = await _db.JournalEntries
            .Where(e => e.IsDeleted != true
                        && e.Source == JournalSource.ManualReversal
                        && e.SourceId != null
                        && openingEntryIds.Contains(e.SourceId.Value))
            .Select(e => e.JournalEntryID)
            .ToListAsync(cancellationToken);

        var ignored = openingEntryIds.Concat(reversalIds).ToHashSet();

        return await _db.JournalLines.AnyAsync(
            l => l.IsDeleted != true
                 && l.PartyId == partyId
                 && !ignored.Contains(l.JournalEntryId),
            cancellationToken);
    }
}
