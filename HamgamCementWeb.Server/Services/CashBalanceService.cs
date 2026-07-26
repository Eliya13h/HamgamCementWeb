using System.Data;
using Dapper;

namespace HamgamCementWeb.Server.Services;

public record CashCurrencyBalance(
    int CurrencyId,
    string CurrencyCode,
    string Symbol,
    string Name,
    bool IsBaseCurrency,
    decimal Amount,
    decimal AmountInBase);

// خلاصه وضعیت یک صندوق برای صفحه آمار و تحلیل
public record CashBoxOverview(
    int CashBoxId,
    string Code,
    string Name,
    string? ParentName,
    bool IsActive,
    bool HasOpenShift,
    string? OpenShiftUserName,
    decimal TotalInBase,
    IReadOnlyList<CashCurrencyBalance> Balances);

public interface ICashBalanceService
{
    Task<IReadOnlyList<CashCurrencyBalance>> GetBalancesAsync(int cashBoxId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashBoxOverview>> GetOverviewAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetBalanceAsync(int cashBoxId, int currencyId, CancellationToken cancellationToken = default);
    Task EnsureSufficientBalanceAsync(int cashBoxId, int currencyId, decimal amount, CancellationToken cancellationToken = default);
}

public class CashBalanceService : ICashBalanceService
{
    private readonly ISqlConnectionFactory _sql;

    public CashBalanceService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<IReadOnlyList<CashCurrencyBalance>> GetBalancesAsync(
        int cashBoxId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<CashCurrencyBalance>(
            """
            SELECT c.CurrencyID AS CurrencyId,
                   c.CurrencyCode AS CurrencyCode,
                   c.Symbol AS Symbol,
                   c.Name AS Name,
                   c.IsBaseCurrency AS IsBaseCurrency,
                   ISNULL(b.Amount, 0) AS Amount,
                   ISNULL(b.AmountInBase, 0) AS AmountInBase
            FROM Currencies c
            LEFT JOIN (
                SELECT jl.CurrencyId,
                       SUM(jl.Debit - jl.Credit) AS Amount,
                       SUM(jl.DebitInBaseCurrency - jl.CreditInBaseCurrency) AS AmountInBase
                FROM JournalLines jl
                INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
                WHERE jl.CashBoxId = @CashBoxId
                  AND ISNULL(jl.IsDeleted, 0) = 0
                  AND ISNULL(je.IsDeleted, 0) = 0
                  AND je.IsPosted = 1
                GROUP BY jl.CurrencyId
            ) b ON b.CurrencyId = c.CurrencyID
            WHERE ISNULL(c.IsDeleted, 0) = 0
              AND ISNULL(c.IsActive, 1) = 1
            ORDER BY c.IsBaseCurrency DESC, c.CurrencyCode
            """,
            new { CashBoxId = cashBoxId });

        return rows.AsList();
    }

    // وضعیت و موجودی همه صندوق‌ها (برای کارت صندوق در آمار و تحلیل)
    public async Task<IReadOnlyList<CashBoxOverview>> GetOverviewAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var boxes = (await connection.QueryAsync<CashBoxOverviewRow>(
            """
            SELECT c.CashBoxID AS CashBoxId,
                   c.Code AS Code,
                   c.Name AS Name,
                   p.Name AS ParentName,
                   CAST(CASE WHEN ISNULL(c.IsActive, 1) = 1 THEN 1 ELSE 0 END AS bit) AS IsActive,
                   CAST(CASE WHEN os.CashShiftID IS NOT NULL THEN 1 ELSE 0 END AS bit) AS HasOpenShift,
                   os.UserName AS OpenShiftUserName
            FROM CashBoxes c
            LEFT JOIN CashBoxes p ON p.CashBoxID = c.ParentCashBoxId AND ISNULL(p.IsDeleted, 0) = 0
            LEFT JOIN (
                SELECT s.CashBoxId,
                       s.CashShiftID,
                       u.UserName,
                       ROW_NUMBER() OVER (PARTITION BY s.CashBoxId ORDER BY s.OpenedAt DESC) AS Rn
                FROM CashShifts s
                INNER JOIN Users u ON u.UserID = s.UserId
                WHERE ISNULL(s.IsDeleted, 0) = 0
                  AND s.Status = 1
            ) os ON os.CashBoxId = c.CashBoxID AND os.Rn = 1
            WHERE ISNULL(c.IsDeleted, 0) = 0
            ORDER BY c.Code
            """)).AsList();

        var balanceRows = (await connection.QueryAsync<CashBoxBalanceRow>(
            """
            SELECT jl.CashBoxId AS CashBoxId,
                   cur.CurrencyID AS CurrencyId,
                   cur.CurrencyCode AS CurrencyCode,
                   cur.Symbol AS Symbol,
                   cur.Name AS Name,
                   CAST(CASE WHEN cur.IsBaseCurrency = 1 THEN 1 ELSE 0 END AS bit) AS IsBaseCurrency,
                   SUM(jl.Debit - jl.Credit) AS Amount,
                   SUM(jl.DebitInBaseCurrency - jl.CreditInBaseCurrency) AS AmountInBase
            FROM JournalLines jl
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            INNER JOIN Currencies cur ON cur.CurrencyID = jl.CurrencyId
            WHERE jl.CashBoxId IS NOT NULL
              AND ISNULL(jl.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
              AND ISNULL(cur.IsDeleted, 0) = 0
            GROUP BY jl.CashBoxId, cur.CurrencyID, cur.CurrencyCode, cur.Symbol, cur.Name, cur.IsBaseCurrency
            HAVING SUM(jl.Debit - jl.Credit) <> 0
               OR SUM(jl.DebitInBaseCurrency - jl.CreditInBaseCurrency) <> 0
            """)).AsList();

        var balancesByBox = balanceRows
            .GroupBy(r => r.CashBoxId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CashCurrencyBalance>)g
                    .Select(r => new CashCurrencyBalance(
                        r.CurrencyId,
                        r.CurrencyCode ?? string.Empty,
                        r.Symbol ?? string.Empty,
                        r.Name ?? string.Empty,
                        r.IsBaseCurrency,
                        r.Amount,
                        r.AmountInBase))
                    .OrderByDescending(b => b.IsBaseCurrency)
                    .ThenBy(b => b.CurrencyCode)
                    .ToList());

        return boxes.Select(box =>
        {
            var balances = balancesByBox.TryGetValue(box.CashBoxId, out var list)
                ? list
                : Array.Empty<CashCurrencyBalance>();
            return new CashBoxOverview(
                box.CashBoxId,
                box.Code ?? string.Empty,
                box.Name ?? string.Empty,
                box.ParentName,
                box.IsActive,
                box.HasOpenShift,
                box.OpenShiftUserName,
                balances.Sum(b => b.AmountInBase),
                balances);
        }).ToList();
    }

    private sealed class CashBoxOverviewRow
    {
        public int CashBoxId { get; init; }
        public string? Code { get; init; }
        public string? Name { get; init; }
        public string? ParentName { get; init; }
        public bool IsActive { get; init; }
        public bool HasOpenShift { get; init; }
        public string? OpenShiftUserName { get; init; }
    }

    private sealed class CashBoxBalanceRow
    {
        public int CashBoxId { get; init; }
        public int CurrencyId { get; init; }
        public string? CurrencyCode { get; init; }
        public string? Symbol { get; init; }
        public string? Name { get; init; }
        public bool IsBaseCurrency { get; init; }
        public decimal Amount { get; init; }
        public decimal AmountInBase { get; init; }
    }

    public async Task<decimal> GetBalanceAsync(
        int cashBoxId,
        int currencyId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var amount = await connection.ExecuteScalarAsync<decimal?>(
            """
            SELECT SUM(jl.Debit - jl.Credit)
            FROM JournalLines jl
            INNER JOIN JournalEntries je ON je.JournalEntryID = jl.JournalEntryId
            WHERE jl.CashBoxId = @CashBoxId
              AND jl.CurrencyId = @CurrencyId
              AND ISNULL(jl.IsDeleted, 0) = 0
              AND ISNULL(je.IsDeleted, 0) = 0
              AND je.IsPosted = 1
            """,
            new { CashBoxId = cashBoxId, CurrencyId = currencyId });

        return amount ?? 0m;
    }

    public async Task EnsureSufficientBalanceAsync(
        int cashBoxId,
        int currencyId,
        decimal amount,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0)
        {
            return;
        }

        var balance = await GetBalanceAsync(cashBoxId, currencyId, cancellationToken);
        if (balance + 0.0001m < amount)
        {
            await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
            var label = await connection.ExecuteScalarAsync<string>(
                """
                SELECT COALESCE(NULLIF(CurrencyCode, ''), NULLIF(Symbol, ''), Name)
                FROM Currencies
                WHERE CurrencyID = @CurrencyId
                """,
                new { CurrencyId = currencyId }) ?? currencyId.ToString();

            throw new InvalidOperationException(
                $"موجودی {label} صندوق کافی نیست. موجودی: {FormatAmount(balance)} — موردنیاز: {FormatAmount(amount)}");
        }
    }

    // بدون صفرهای اعشار اضافه وقتی عدد صحیح است (مثلاً 100 به‌جای 100.0000)
    private static string FormatAmount(decimal value) =>
        value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);
}
