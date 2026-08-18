using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

/// <summary>
/// ساخت سند دابل‌انتری از رویدادهای عملیاتی (مصرف، عاید، انتقال صندوق).
/// </summary>
public interface IOperationalGlService
{
    Task<JournalEntry> PostMiscExpenseAsync(Expense expense, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostMiscRevenueAsync(Revenue revenue, int? userId, int? cashBoxId, CancellationToken cancellationToken = default);
    Task<JournalEntry> PostCashTransferAsync(CashTransfer transfer, Account fromAccount, Account toAccount, int? userId, CancellationToken cancellationToken = default);
    Task<int> ResolveSettlementAccountIdAsync(int? cashBoxId, CancellationToken cancellationToken = default);
}

public class OperationalGlService : IOperationalGlService
{
    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICashBalanceService _cashBalances;

    public OperationalGlService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICashBalanceService cashBalances)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _cashBalances = cashBalances;
    }

    public async Task<int> ResolveSettlementAccountIdAsync(int? cashBoxId, CancellationToken cancellationToken = default)
    {
        if (cashBoxId is int id)
        {
            var box = await _db.CashBoxes
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.CashBoxID == id && c.IsDeleted != true, cancellationToken);
            if (box is not null)
            {
                return box.AccountId;
            }
        }

        throw new InvalidOperationException("صندوق یا حساب بانکی برای تسویه نقدی الزامی است.");
    }

    public async Task<JournalEntry> PostMiscExpenseAsync(
        Expense expense,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.ExpenseCategories
            .FirstAsync(c => c.ExpenseCategoryID == expense.ExpenseCategoryId, cancellationToken);

        var expenseAccountId = category.AccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.MiscExpense, cancellationToken)).AccountID;

        int creditAccountId;
        int? lineCashBoxId = null;
        if (expense.SupplierId is int supplierId)
        {
            var name = await _db.Suppliers.Where(s => s.SupplierID == supplierId).Select(s => s.Name).FirstAsync(cancellationToken);
            creditAccountId = (await _accounts.EnsureSupplierAccountAsync(supplierId, name, cancellationToken)).AccountID;
        }
        else
        {
            creditAccountId = await ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            await EnsureCashOutAsync(cashBoxId, expense.CurrencyId, expense.Amount, cancellationToken);
            lineCashBoxId = cashBoxId;
        }

        var lines = new List<JournalLineDraft>
        {
            new(expenseAccountId, expense.Amount, 0, expense.AmountInBaseCurrency, 0, expense.CurrencyId, expense.Title),
            new(creditAccountId, 0, expense.Amount, 0, expense.AmountInBaseCurrency, expense.CurrencyId, expense.Title,
                CashBoxId: lineCashBoxId,
                PartyId: expense.SupplierId),
        };

        return await _journal.PostAsync(
            expense.ExpenseDate,
            expense.Title,
            JournalSource.Expense,
            expense.ExpenseID,
            expense.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public async Task<JournalEntry> PostMiscRevenueAsync(
        Revenue revenue,
        int? userId,
        int? cashBoxId,
        CancellationToken cancellationToken = default)
    {
        var category = await _db.RevenueCategories
            .FirstAsync(c => c.RevenueCategoryID == revenue.RevenueCategoryId, cancellationToken);

        var revenueAccountId = category.AccountId
            ?? (await _accounts.GetBySystemCodeAsync(AccountSystemCode.OtherRevenue, cancellationToken)).AccountID;

        int debitAccountId;
        int? lineCashBoxId = null;
        if (revenue.CustomerId is int customerId)
        {
            var name = await _db.Customers.Where(c => c.CustomerID == customerId).Select(c => c.Name).FirstAsync(cancellationToken);
            debitAccountId = (await _accounts.EnsureCustomerAccountAsync(customerId, name, cancellationToken)).AccountID;
        }
        else
        {
            debitAccountId = await ResolveSettlementAccountIdAsync(cashBoxId, cancellationToken);
            lineCashBoxId = cashBoxId;
        }

        var lines = new List<JournalLineDraft>
        {
            new(debitAccountId, revenue.Amount, 0, revenue.AmountInBaseCurrency, 0, revenue.CurrencyId, revenue.Title,
                CashBoxId: lineCashBoxId,
                PartyId: revenue.CustomerId),
            new(revenueAccountId, 0, revenue.Amount, 0, revenue.AmountInBaseCurrency, revenue.CurrencyId, revenue.Title),
        };

        return await _journal.PostAsync(
            revenue.RevenueDate,
            revenue.Title,
            JournalSource.Revenue,
            revenue.RevenueID,
            revenue.BaseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    public Task<JournalEntry> PostCashTransferAsync(
        CashTransfer transfer,
        Account fromAccount,
        Account toAccount,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        return PostCashTransferCoreAsync(transfer, fromAccount, toAccount, userId, cancellationToken);
    }

    private async Task<JournalEntry> PostCashTransferCoreAsync(
        CashTransfer transfer,
        Account fromAccount,
        Account toAccount,
        int? userId,
        CancellationToken cancellationToken)
    {
        var baseCurrencyId = await ResolveBaseCurrencyIdAsync(cancellationToken);
        var transferLines = transfer.Lines?.Where(l => l.IsDeleted != true).ToList() ?? [];

        if (transferLines.Count == 0 && transfer.AmountInBaseCurrency > 0)
        {
            transferLines =
            [
                new CashTransferLine
                {
                    CurrencyId = baseCurrencyId,
                    Amount = transfer.AmountInBaseCurrency,
                    AmountInBaseCurrency = transfer.AmountInBaseCurrency,
                },
            ];
        }

        if (transferLines.Count == 0)
        {
            throw new InvalidOperationException("خطوط انتقال صندوق خالی است.");
        }

        var lines = new List<JournalLineDraft>();
        foreach (var line in transferLines)
        {
            lines.Add(new(
                toAccount.AccountID,
                line.Amount, 0,
                line.AmountInBaseCurrency, 0,
                line.CurrencyId,
                "ورود انتقال صندوق",
                CashBoxId: transfer.ToCashBoxId));
            lines.Add(new(
                fromAccount.AccountID,
                0, line.Amount,
                0, line.AmountInBaseCurrency,
                line.CurrencyId,
                "خروج انتقال صندوق",
                CashBoxId: transfer.FromCashBoxId));
        }

        return await _journal.PostAsync(
            transfer.TransferDate,
            transfer.Description ?? "انتقال صندوق",
            JournalSource.CashTransfer,
            transfer.CashTransferID,
            baseCurrencyId,
            lines,
            userId,
            cancellationToken);
    }

    private async Task EnsureCashOutAsync(
        int? cashBoxId,
        int currencyId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        if (cashBoxId is not int id || amount <= 0)
        {
            return;
        }

        await _cashBalances.EnsureSufficientBalanceAsync(id, currencyId, amount, cancellationToken);
    }

    private async Task<int> ResolveBaseCurrencyIdAsync(CancellationToken cancellationToken)
    {
        var baseCurrencyId = await _db.Currencies
            .Where(c => c.IsBaseCurrency && c.IsDeleted != true)
            .Select(c => c.CurrencyID)
            .FirstOrDefaultAsync(cancellationToken);

        if (baseCurrencyId == 0)
        {
            baseCurrencyId = await _db.Currencies
                .Where(c => c.IsDeleted != true)
                .OrderBy(c => c.CurrencyID)
                .Select(c => c.CurrencyID)
                .FirstAsync(cancellationToken);
        }

        return baseCurrencyId;
    }
}
