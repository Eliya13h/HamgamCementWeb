using Dapper;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Finance;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public record CurrencyExchangeRequest(
    DateTime ExchangeDate,
    int FromCurrencyId,
    decimal FromAmount,
    int ToCurrencyId,
    decimal ToAmount,
    bool RecognizeFxDifference,
    int? FromCashBoxId,
    int? FromBankAccountId,
    int? ToCashBoxId,
    int? ToBankAccountId,
    string? Description);

public interface ICurrencyExchangeService
{
    Task<CurrencyExchangeTxn> PostAsync(CurrencyExchangeRequest request, int? userId, CancellationToken cancellationToken = default);
    Task SoftDeleteAsync(int currencyExchangeTxnId, int? userId, CancellationToken cancellationToken = default);
}

public class CurrencyExchangeService : ICurrencyExchangeService
{
    private const decimal Tolerance = 0.01m;

    private readonly AppDbContext _db;
    private readonly IJournalPostingService _journal;
    private readonly IAccountLookupService _accounts;
    private readonly ICashBalanceService _cashBalances;
    private readonly ICurrencyConversionService _currencies;
    private readonly ISqlConnectionFactory _sql;

    public CurrencyExchangeService(
        AppDbContext db,
        IJournalPostingService journal,
        IAccountLookupService accounts,
        ICashBalanceService cashBalances,
        ICurrencyConversionService currencies,
        ISqlConnectionFactory sql)
    {
        _db = db;
        _journal = journal;
        _accounts = accounts;
        _cashBalances = cashBalances;
        _currencies = currencies;
        _sql = sql;
    }

    public async Task<CurrencyExchangeTxn> PostAsync(
        CurrencyExchangeRequest request,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        if (request.FromAmount <= 0 || request.ToAmount <= 0)
        {
            throw new InvalidOperationException("مبالغ مبدأ و مقصد باید بزرگ‌تر از صفر باشند.");
        }

        if (request.FromCurrencyId == request.ToCurrencyId)
        {
            throw new InvalidOperationException("ارز مبدأ و مقصد باید متفاوت باشند.");
        }

        var fromHasCash = request.FromCashBoxId is > 0;
        var fromHasBank = request.FromBankAccountId is > 0;
        if (fromHasCash == fromHasBank)
        {
            throw new InvalidOperationException("مبدأ باید دقیقاً یکی از صندوق یا حساب بانکی باشد.");
        }

        var toHasCash = request.ToCashBoxId is > 0;
        var toHasBank = request.ToBankAccountId is > 0;
        if (toHasCash == toHasBank)
        {
            throw new InvalidOperationException("مقصد باید دقیقاً یکی از صندوق یا حساب بانکی باشد.");
        }

        var exchangeDate = request.ExchangeDate == default ? DateTime.Now : request.ExchangeDate;

        var fromSnap = await _currencies.GetSnapshotAsync(request.FromCurrencyId, exchangeDate, cancellationToken);
        var toSnap = await _currencies.GetSnapshotAsync(request.ToCurrencyId, exchangeDate, cancellationToken);

        decimal fromBase;
        decimal toBase;
        decimal fxDiff;

        if (request.RecognizeFxDifference)
        {
            fromBase = Round4(_currencies.ConvertToBase(request.FromAmount, fromSnap));
            toBase = Round4(_currencies.ConvertToBase(request.ToAmount, toSnap));
            fxDiff = Round4(fromBase - toBase);
            if (Math.Abs(fxDiff) <= Tolerance)
            {
                fxDiff = 0;
                // هم‌تراز کردن برای جلوگیری از خطای تراز به‌خاطر گرد کردن
                toBase = fromBase;
            }
        }
        else
        {
            // حالت A: هر دو طرف با ارزش معامله (معادل پایهٔ مبلغ مقصد)
            toBase = Round4(_currencies.ConvertToBase(request.ToAmount, toSnap));
            fromBase = toBase;
            fxDiff = 0;
        }

        if (fromBase <= 0 || toBase <= 0)
        {
            throw new InvalidOperationException("معادل ارز پایه باید بزرگ‌تر از صفر باشد.");
        }

        var dealRate = Round8(request.ToAmount / request.FromAmount);

        var (fromAccountId, fromCashBoxId, fromBankAccountId) = await ResolveWalletAsync(
            request.FromCashBoxId,
            request.FromBankAccountId,
            isSource: true,
            cancellationToken);

        var (toAccountId, toCashBoxId, toBankAccountId) = await ResolveWalletAsync(
            request.ToCashBoxId,
            request.ToBankAccountId,
            isSource: false,
            cancellationToken);

        if (fromCashBoxId is int outBoxId)
        {
            await _cashBalances.EnsureSufficientBalanceAsync(
                outBoxId, request.FromCurrencyId, request.FromAmount, cancellationToken);
        }
        else if (fromBankAccountId is int outBankId)
        {
            await EnsureBankSufficientBalanceAsync(
                fromAccountId, request.FromCurrencyId, request.FromAmount, cancellationToken);
        }

        var fromCurrency = await _db.Currencies.AsNoTracking()
            .FirstAsync(c => c.CurrencyID == request.FromCurrencyId, cancellationToken);
        var toCurrency = await _db.Currencies.AsNoTracking()
            .FirstAsync(c => c.CurrencyID == request.ToCurrencyId, cancellationToken);

        var description = string.IsNullOrWhiteSpace(request.Description)
            ? $"تبدیل ارز — {FormatAmt(request.FromAmount)} {fromCurrency.CurrencyCode} → {FormatAmt(request.ToAmount)} {toCurrency.CurrencyCode}"
            : request.Description.Trim();

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        var now = DateTime.Now;
        var txn = new CurrencyExchangeTxn
        {
            ExchangeDate = exchangeDate,
            FromCurrencyId = request.FromCurrencyId,
            FromAmount = request.FromAmount,
            FromAmountInBaseCurrency = fromBase,
            ToCurrencyId = request.ToCurrencyId,
            ToAmount = request.ToAmount,
            ToAmountInBaseCurrency = toBase,
            DealRate = dealRate,
            RecognizeFxDifference = request.RecognizeFxDifference,
            SystemFromBaseUnitsPerUnit = fromSnap.BaseUnitsPerUnit,
            SystemToBaseUnitsPerUnit = toSnap.BaseUnitsPerUnit,
            FxDifferenceInBaseCurrency = fxDiff,
            FromCashBoxId = fromCashBoxId,
            FromBankAccountId = fromBankAccountId,
            ToCashBoxId = toCashBoxId,
            ToBankAccountId = toBankAccountId,
            ExchangeHistoryFromId = fromSnap.ExchangeHistoryId,
            ExchangeHistoryToId = toSnap.ExchangeHistoryId,
            Description = description,
            IsActive = true,
            IsDeleted = false,
            CreatedAt = now,
            CreatedBy = userId,
        };

        _db.CurrencyExchangeTxns.Add(txn);
        await _db.SaveChangesAsync(cancellationToken);

        var baseCurrency = await _currencies.GetBaseCurrencyAsync(cancellationToken);
        var lines = new List<JournalLineDraft>
        {
            // بدهکار مقصد (ورود ارز)
            new(
                toAccountId,
                request.ToAmount,
                0,
                toBase,
                0,
                request.ToCurrencyId,
                $"ورود {toCurrency.CurrencyCode}",
                CashBoxId: toCashBoxId),
            // بستانکار مبدأ (خروج ارز)
            new(
                fromAccountId,
                0,
                request.FromAmount,
                0,
                fromBase,
                request.FromCurrencyId,
                $"خروج {fromCurrency.CurrencyCode}",
                CashBoxId: fromCashBoxId),
        };

        if (request.RecognizeFxDifference && Math.Abs(fxDiff) > Tolerance)
        {
            var absDiff = Math.Abs(fxDiff);
            if (fxDiff > 0)
            {
                // از دست دادن ارزش بیشتر از دریافت → زیان تسعیر
                var lossAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.FxLoss, cancellationToken);
                lines.Add(new(
                    lossAccount.AccountID,
                    absDiff,
                    0,
                    absDiff,
                    0,
                    baseCurrency.CurrencyID,
                    "زیان تسعیر ارز"));
            }
            else
            {
                // دریافت بیشتر از ارزش سیستم → سود تسعیر
                var gainAccount = await _accounts.GetBySystemCodeAsync(AccountSystemCode.FxGain, cancellationToken);
                lines.Add(new(
                    gainAccount.AccountID,
                    0,
                    absDiff,
                    0,
                    absDiff,
                    baseCurrency.CurrencyID,
                    "سود تسعیر ارز"));
            }
        }

        var journal = await _journal.PostAsync(
            exchangeDate,
            description,
            JournalSource.CurrencyExchange,
            txn.CurrencyExchangeTxnID,
            baseCurrency.CurrencyID,
            lines,
            userId,
            cancellationToken);

        txn.JournalEntryId = journal.JournalEntryID;
        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return txn;
    }

    public async Task SoftDeleteAsync(int currencyExchangeTxnId, int? userId, CancellationToken cancellationToken = default)
    {
        var txn = await _db.CurrencyExchangeTxns
            .FirstOrDefaultAsync(t => t.CurrencyExchangeTxnID == currencyExchangeTxnId && t.IsDeleted != true, cancellationToken)
            ?? throw new InvalidOperationException("سند تبدیل ارز یافت نشد.");

        await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

        await _journal.ReverseBySourceAsync(
            JournalSource.CurrencyExchange,
            txn.CurrencyExchangeTxnID,
            userId,
            cancellationToken: cancellationToken);

        var now = DateTime.Now;
        txn.IsDeleted = true;
        txn.IsActive = false;
        txn.DeletedAt = now;
        txn.DeletedBy = userId;
        txn.JournalEntryId = null;

        await _db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
    }

    private async Task<(int AccountId, int? CashBoxId, int? BankAccountId)> ResolveWalletAsync(
        int? cashBoxId,
        int? bankAccountId,
        bool isSource,
        CancellationToken cancellationToken)
    {
        var label = isSource ? "مبدأ" : "مقصد";

        if (cashBoxId is > 0)
        {
            var box = await _db.CashBoxes
                .FirstOrDefaultAsync(c => c.CashBoxID == cashBoxId && c.IsDeleted != true && c.IsActive == true, cancellationToken)
                ?? throw new InvalidOperationException($"صندوق {label} یافت نشد یا غیرفعال است.");
            return (box.AccountId, box.CashBoxID, null);
        }

        var bank = await _db.BankAccounts
            .FirstOrDefaultAsync(b => b.BankAccountID == bankAccountId && b.IsDeleted != true && b.IsActive == true, cancellationToken)
            ?? throw new InvalidOperationException($"حساب بانکی {label} یافت نشد یا غیرفعال است.");
        return (bank.AccountId, null, bank.BankAccountID);
    }

    private async Task EnsureBankSufficientBalanceAsync(
        int accountId,
        int currencyId,
        decimal amount,
        CancellationToken cancellationToken)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var balance = await connection.ExecuteScalarAsync<decimal?>(
            """
            SELECT SUM(jl.Debit - jl.Credit)
            FROM JournalLines jl
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            WHERE jl.AccountId = @AccountId
              AND jl.CurrencyId = @CurrencyId
              AND ISNULL(jl.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
            """,
            new { AccountId = accountId, CurrencyId = currencyId }) ?? 0m;

        if (balance + 0.0001m < amount)
        {
            var label = await connection.ExecuteScalarAsync<string>(
                """
                SELECT COALESCE(NULLIF(CurrencyCode, ''), NULLIF(Symbol, ''), Name)
                FROM Currencies
                WHERE CurrencyID = @CurrencyId
                """,
                new { CurrencyId = currencyId }) ?? currencyId.ToString();

            throw new InvalidOperationException(
                $"موجودی {label} حساب بانکی کافی نیست. موجودی: {FormatAmt(balance)} — موردنیاز: {FormatAmt(amount)}");
        }
    }

    private static decimal Round4(decimal value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);

    private static decimal Round8(decimal value) => Math.Round(value, 8, MidpointRounding.AwayFromZero);

    private static string FormatAmt(decimal value) =>
        value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
}
