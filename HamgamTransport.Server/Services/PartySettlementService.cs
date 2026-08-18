using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public record PartySettlementRequest(
    PartySettlementPartyType PartyType,
    int PartyId,
    DateTime SettlementDate,
    int CurrencyId,
    decimal Amount,
    decimal? AmountInBaseCurrency,
    int? CashBoxId,
    int? BankAccountId,
    string? Description);

public interface IPartySettlementService
{
    Task<PartySettlement> PostAsync(PartySettlementRequest request, int? userId, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(int partySettlementId, int? userId, CancellationToken cancellationToken = default);
}

public class PartySettlementService : IPartySettlementService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICashBalanceService _cashBalances;
    private readonly ICurrencyConversionService _currencies;

    public PartySettlementService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICashBalanceService cashBalances,
        ICurrencyConversionService currencies)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _cashBalances = cashBalances;
        _currencies = currencies;
    }

    public async Task<PartySettlement> PostAsync(
        PartySettlementRequest request,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new InvalidOperationException("مبلغ باید بزرگ‌تر از صفر باشد.");
        }

        var hasCash = request.CashBoxId is > 0;
        var hasBank = request.BankAccountId is > 0;
        if (hasCash == hasBank)
        {
            throw new InvalidOperationException("یکی از صندوق یا حساب بانکی باید مشخص شود.");
        }

        var currencyExists = await _db.Currencies
            .AnyAsync(c => c.CurrencyID == request.CurrencyId && c.IsDeleted != true, cancellationToken);
        if (!currencyExists)
        {
            throw new InvalidOperationException("ارز یافت نشد.");
        }

        var settlementDate = request.SettlementDate == default ? DateTime.Now : request.SettlementDate;
        var amountBase = request.AmountInBaseCurrency is > 0
            ? request.AmountInBaseCurrency.Value
            : _currencies.ConvertToBase(
                request.Amount,
                await _currencies.GetSnapshotAsync(request.CurrencyId, settlementDate, cancellationToken));

        if (amountBase <= 0)
        {
            throw new InvalidOperationException("معادل ارز پایه باید بزرگ‌تر از صفر باشد.");
        }

        int settlementAccountId;
        int? cashBoxId = null;
        int? bankAccountId = null;

        if (hasCash)
        {
            var box = await _db.CashBoxes
                .FirstOrDefaultAsync(c => c.CashBoxID == request.CashBoxId && c.IsDeleted != true && c.IsActive == true, cancellationToken)
                ?? throw new InvalidOperationException("صندوق یافت نشد یا غیرفعال است.");
            settlementAccountId = box.AccountId;
            cashBoxId = box.CashBoxID;
        }
        else
        {
            var bank = await _db.BankAccounts
                .FirstOrDefaultAsync(b => b.BankAccountID == request.BankAccountId && b.IsDeleted != true && b.IsActive == true, cancellationToken)
                ?? throw new InvalidOperationException("حساب بانکی یافت نشد یا غیرفعال است.");
            settlementAccountId = bank.AccountId;
            bankAccountId = bank.BankAccountID;
        }

        Account partyAccount;
        string partyName;
        string description;

        if (request.PartyType == PartySettlementPartyType.Customer)
        {
            var customer = await _db.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CustomerID == request.PartyId && c.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("مشتری یافت نشد.");
            partyName = customer.Name;
            partyAccount = await _accounts.EnsureCustomerAccountAsync(customer.CustomerID, customer.Name, cancellationToken);
            description = string.IsNullOrWhiteSpace(request.Description)
                ? $"دریافت از مشتری — {partyName}"
                : request.Description.Trim();
        }
        else if (request.PartyType == PartySettlementPartyType.Supplier)
        {
            var supplier = await _db.Suppliers
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.SupplierID == request.PartyId && s.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("تأمین‌کننده یافت نشد.");
            partyName = supplier.Name;
            partyAccount = await _accounts.EnsureSupplierAccountAsync(supplier.SupplierID, supplier.Name, cancellationToken);
            description = string.IsNullOrWhiteSpace(request.Description)
                ? $"پرداخت به تأمین‌کننده — {partyName}"
                : request.Description.Trim();
        }
        else if (request.PartyType == PartySettlementPartyType.VehicleOwner)
        {
            var owner = await _db.VehicleOwners
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.VehicleOwnerId == request.PartyId && o.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("مالک وسیله یافت نشد.");
            partyName = owner.Name;
            partyAccount = await _accounts.EnsureVehicleOwnerAccountAsync(owner.VehicleOwnerId, owner.Name, cancellationToken);
            description = string.IsNullOrWhiteSpace(request.Description)
                ? $"پرداخت به مالک وسیله — {partyName}"
                : request.Description.Trim();
        }
        else if (request.PartyType == PartySettlementPartyType.Driver)
        {
            var driver = await _db.Drivers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.DriverId == request.PartyId && d.IsDeleted != true, cancellationToken)
                ?? throw new InvalidOperationException("راننده یافت نشد.");
            partyName = driver.Name;
            partyAccount = await _accounts.EnsureDriverAccountAsync(driver.DriverId, driver.Name, cancellationToken);
            description = string.IsNullOrWhiteSpace(request.Description)
                ? $"پرداخت به راننده — {partyName}"
                : request.Description.Trim();
        }
        else
        {
            throw new InvalidOperationException("نوع طرف تسویه نامعتبر است.");
        }

        if ((request.PartyType == PartySettlementPartyType.Supplier
                || request.PartyType == PartySettlementPartyType.VehicleOwner
                || request.PartyType == PartySettlementPartyType.Driver)
            && cashBoxId is int outBoxId)
        {
            await _cashBalances.EnsureSufficientBalanceAsync(outBoxId, request.CurrencyId, request.Amount, cancellationToken);
        }

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.Now;
        var settlement = new PartySettlement
        {
            PartyType = request.PartyType,
            PartyId = request.PartyId,
            SettlementDate = settlementDate,
            CurrencyId = request.CurrencyId,
            Amount = request.Amount,
            AmountInBaseCurrency = amountBase,
            CashBoxId = cashBoxId,
            BankAccountId = bankAccountId,
            Description = description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        _db.PartySettlements.Add(settlement);
        await _db.SaveChangesAsync(cancellationToken);

        List<JournalLineDraft> lines;
        if (request.PartyType == PartySettlementPartyType.Customer)
        {
            lines =
            [
                new(settlementAccountId, request.Amount, 0, amountBase, 0, request.CurrencyId,
                    $"دریافت از {partyName}", CashBoxId: cashBoxId),
                new(partyAccount.AccountID, 0, request.Amount, 0, amountBase, request.CurrencyId,
                    $"تسویه دریافتنی — {partyName}", PartyId: request.PartyId),
            ];
        }
        else
        {
            lines =
            [
                new(partyAccount.AccountID, request.Amount, 0, amountBase, 0, request.CurrencyId,
                    $"تسویه پرداختنی — {partyName}", PartyId: request.PartyId),
                new(settlementAccountId, 0, request.Amount, 0, amountBase, request.CurrencyId,
                    $"پرداخت به {partyName}", CashBoxId: cashBoxId),
            ];
        }

        var baseCurrency = await _currencies.GetBaseCurrencyAsync(cancellationToken);
        var journal = await _journal.PostAsync(
            settlementDate,
            description,
            JournalSource.PartySettlement,
            settlement.PartySettlementID,
            baseCurrency.CurrencyID,
            lines,
            userId,
            cancellationToken);

        settlement.JournalEntryId = journal.JournalEntryID;
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return settlement;
    }

    public async Task SoftDeleteAsync(int partySettlementId, int? userId, CancellationToken cancellationToken = default)
    {
        var settlement = await _db.PartySettlements
            .FirstOrDefaultAsync(s => s.PartySettlementID == partySettlementId && s.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("تسویه یافت نشد.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        await _journal.ReverseBySourceAsync(
            JournalSource.PartySettlement,
            settlement.PartySettlementID,
            userId,
            cancellationToken: cancellationToken);

        var now = DateTime.Now;
        settlement.IsDeleted = true;
        settlement.IsActive = false;
        settlement.DeletedAt = now;
        settlement.DeletedBy = userId;
        settlement.JournalEntryId = null;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }
}
