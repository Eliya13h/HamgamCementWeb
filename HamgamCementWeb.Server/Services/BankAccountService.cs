using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public interface IBankAccountService
{
    Task<BankAccount> CreateAsync(
        string? code,
        string name,
        string? accountNumber,
        int? currencyId,
        string? description,
        int? createdBy,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        int bankAccountId,
        string name,
        string? accountNumber,
        int? currencyId,
        string? description,
        bool isActive,
        int? updatedBy,
        CancellationToken cancellationToken = default);
}

public class BankAccountService : IBankAccountService
{
    private readonly AppDbContext _db;
    private readonly IAccountLookupService _accounts;

    public BankAccountService(AppDbContext db, IAccountLookupService accounts)
    {
        _db = db;
        _accounts = accounts;
    }

    public async Task<BankAccount> CreateAsync(
        string? code,
        string name,
        string? accountNumber,
        int? currencyId,
        string? description,
        int? createdBy,
        CancellationToken cancellationToken = default)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("نام حساب بانکی الزامی است.");
        }

        // کد حساب بانکی به‌صورت خودکار تولید می‌شود تا با کدینگ حساب‌ها تداخل نکند
        code = string.IsNullOrWhiteSpace(code)
            ? await GenerateNextCodeAsync(cancellationToken)
            : code.Trim();

        if (await _db.BankAccounts.AnyAsync(b => b.Code == code && b.IsDeleted != true, cancellationToken))
        {
            throw new InvalidOperationException("کد حساب بانکی تکراری است.");
        }

        if (currencyId is int cid)
        {
            var currencyExists = await _db.Currencies
                .AnyAsync(c => c.CurrencyID == cid && c.IsDeleted != true, cancellationToken);
            if (!currencyExists)
            {
                throw new InvalidOperationException("ارز یافت نشد.");
            }
        }

        var account = await _accounts.EnsureBankAccountAsync(code, name, cancellationToken);
        var now = DateTime.Now;
        var bank = new BankAccount
        {
            Code = code,
            Name = name,
            AccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber.Trim(),
            AccountId = account.AccountID,
            CurrencyId = currencyId,
            Description = description?.Trim(),
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = createdBy,
        };

        _db.BankAccounts.Add(bank);
        await _db.SaveChangesAsync(cancellationToken);
        return bank;
    }

    public async Task UpdateAsync(
        int bankAccountId,
        string name,
        string? accountNumber,
        int? currencyId,
        string? description,
        bool isActive,
        int? updatedBy,
        CancellationToken cancellationToken = default)
    {
        var bank = await _db.BankAccounts
            .FirstOrDefaultAsync(b => b.BankAccountID == bankAccountId && b.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("حساب بانکی یافت نشد.");

        if (currencyId is int cid)
        {
            var currencyExists = await _db.Currencies
                .AnyAsync(c => c.CurrencyID == cid && c.IsDeleted != true, cancellationToken);
            if (!currencyExists)
            {
                throw new InvalidOperationException("ارز یافت نشد.");
            }
        }

        bank.Name = name.Trim();
        bank.AccountNumber = string.IsNullOrWhiteSpace(accountNumber) ? null : accountNumber.Trim();
        bank.CurrencyId = currencyId;
        bank.Description = description?.Trim();
        bank.IsActive = isActive;
        bank.IsUpdated = true;
        bank.UpdatedAt = DateTime.Now;
        bank.UpdatedBy = updatedBy;

        var account = await _db.Accounts.FirstAsync(a => a.AccountID == bank.AccountId, cancellationToken);
        account.Name = bank.Name;
        account.IsUpdated = true;
        account.UpdatedAt = DateTime.Now;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> GenerateNextCodeAsync(CancellationToken cancellationToken)
    {
        var codes = await _db.BankAccounts
            .IgnoreQueryFilters()
            .Select(b => b.Code)
            .ToListAsync(cancellationToken);

        var maxSequence = codes
            .Select(c => int.TryParse(c, out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        return (maxSequence + 1).ToString("D5");
    }
}
