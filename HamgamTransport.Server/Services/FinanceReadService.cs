using Dapper;
using HamgamTransport.Server.Data;
using System.Data;

namespace HamgamTransport.Server.Services;

public interface IFinanceReadService
{
    Task<(int Total, int Filtered, IReadOnlyList<ExpenseListRow> Rows)> GetExpensesAsync(
        int start, int length, string? search, CancellationToken cancellationToken = default);

    Task<(int Total, int Filtered, IReadOnlyList<RevenueListRow> Rows)> GetRevenuesAsync(
        int start, int length, string? search, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccountTreeRow>> GetAccountTreeAsync(CancellationToken cancellationToken = default);
}

public class FinanceReadService : IFinanceReadService
{
    private readonly ISqlConnectionFactory _sql;

    public FinanceReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<(int Total, int Filtered, IReadOnlyList<ExpenseListRow> Rows)> GetExpensesAsync(
        int start, int length, string? search, CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        const string baseWhere = "WHERE e.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += """
                 AND (e.Title LIKE @Search OR ISNULL(e.Description,'') LIKE @Search
                      OR ISNULL(s.Name,'') LIKE @Search OR c.Name LIKE @Search)
                """;
            p.Add("Search", $"%{search}%");
        }

        var total = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM Expenses e {baseWhere}");
        var filtered = await connection.ExecuteScalarAsync<int>(
            $"""
             SELECT COUNT(1) FROM Expenses e
             LEFT JOIN Suppliers s ON s.SupplierID = e.SupplierId
             INNER JOIN ExpenseCategories c ON c.ExpenseCategoryID = e.ExpenseCategoryId
             {where}
             """, p);

        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await connection.QueryAsync<ExpenseListRow>(
            $"""
             SELECT e.ExpenseID AS ExpenseId,
                    e.Title,
                    e.ExpenseDate,
                    c.Name AS CategoryName,
                    e.ExpenseCategoryId,
                    CAST(e.Source AS int) AS Source,
                    e.SupplierId,
                    s.Name AS SupplierName,
                    e.CurrencyId,
                    cur.CurrencyCode,
                    cur.Symbol AS CurrencySymbol,
                    e.Amount,
                    e.AmountInBaseCurrency,
                    e.Description,
                    e.JournalEntryId,
                    (SELECT TOP 1 i.InvoiceNumber FROM PurchaseInvoices i
                     WHERE i.ExpenseId = e.ExpenseID AND i.IsDeleted = 0) AS InvoiceNumber
             FROM Expenses e
             LEFT JOIN Suppliers s ON s.SupplierID = e.SupplierId
             INNER JOIN ExpenseCategories c ON c.ExpenseCategoryID = e.ExpenseCategoryId
             INNER JOIN Currencies cur ON cur.CurrencyID = e.CurrencyId
             {where}
             ORDER BY e.ExpenseDate DESC, e.ExpenseID DESC
             OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, p)).ToList();

        return (total, filtered, rows);
    }

    public async Task<(int Total, int Filtered, IReadOnlyList<RevenueListRow> Rows)> GetRevenuesAsync(
        int start, int length, string? search, CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        const string baseWhere = "WHERE r.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(search))
        {
            where += """
                 AND (r.Title LIKE @Search OR ISNULL(r.Description,'') LIKE @Search
                      OR ISNULL(cu.Name,'') LIKE @Search OR c.Name LIKE @Search)
                """;
            p.Add("Search", $"%{search}%");
        }

        var total = await connection.ExecuteScalarAsync<int>(
            $"SELECT COUNT(1) FROM Revenues r {baseWhere}");
        var filtered = await connection.ExecuteScalarAsync<int>(
            $"""
             SELECT COUNT(1) FROM Revenues r
             LEFT JOIN Customers cu ON cu.CustomerID = r.CustomerId
             INNER JOIN RevenueCategories c ON c.RevenueCategoryID = r.RevenueCategoryId
             {where}
             """, p);

        p.Add("Offset", start);
        p.Add("Fetch", length);

        var rows = (await connection.QueryAsync<RevenueListRow>(
            $"""
             SELECT r.RevenueID AS RevenueId,
                    r.Title,
                    r.RevenueDate,
                    c.Name AS CategoryName,
                    r.RevenueCategoryId,
                    CAST(r.Source AS int) AS Source,
                    r.CustomerId,
                    cu.Name AS CustomerName,
                    r.CurrencyId,
                    cur.CurrencyCode,
                    cur.Symbol AS CurrencySymbol,
                    r.Amount,
                    r.AmountInBaseCurrency,
                    r.ProfitInBaseCurrency,
                    r.Description,
                    r.JournalEntryId,
                    (SELECT TOP 1 i.InvoiceNumber FROM SaleInvoices i
                     WHERE i.RevenueId = r.RevenueID AND i.IsDeleted = 0) AS InvoiceNumber
             FROM Revenues r
             LEFT JOIN Customers cu ON cu.CustomerID = r.CustomerId
             INNER JOIN RevenueCategories c ON c.RevenueCategoryID = r.RevenueCategoryId
             INNER JOIN Currencies cur ON cur.CurrencyID = r.CurrencyId
             {where}
             ORDER BY r.RevenueDate DESC, r.RevenueID DESC
             OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
             """, p)).ToList();

        return (total, filtered, rows);
    }

    public async Task<IReadOnlyList<AccountTreeRow>> GetAccountTreeAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var rows = await connection.QueryAsync<AccountTreeRow>(
            """
            SELECT AccountID AS AccountId, Code, Name, CAST(Level AS int) AS Level,
                   ParentAccountId, CAST(AccountType AS int) AS AccountType,
                   CAST(Nature AS int) AS Nature, IsPostable, IsSystem, SystemCode
            FROM Accounts
            WHERE IsDeleted = 0
            ORDER BY Code
            """);
        return rows.ToList();
    }
}

public sealed class ExpenseListRow
{
    public int ExpenseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ExpenseDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int ExpenseCategoryId { get; set; }
    public int Source { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? CurrencySymbol { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
    public string? Description { get; set; }
    public int? JournalEntryId { get; set; }
    public string? InvoiceNumber { get; set; }
}

public sealed class RevenueListRow
{
    public int RevenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime RevenueDate { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int RevenueCategoryId { get; set; }
    public int Source { get; set; }
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string? CurrencySymbol { get; set; }
    public decimal Amount { get; set; }
    public decimal AmountInBaseCurrency { get; set; }
    public decimal ProfitInBaseCurrency { get; set; }
    public string? Description { get; set; }
    public int? JournalEntryId { get; set; }
    public string? InvoiceNumber { get; set; }
}

public sealed class AccountTreeRow
{
    public int AccountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public int? ParentAccountId { get; set; }
    public int AccountType { get; set; }
    public int Nature { get; set; }
    public bool IsPostable { get; set; }
    public bool IsSystem { get; set; }
    public string? SystemCode { get; set; }
}
