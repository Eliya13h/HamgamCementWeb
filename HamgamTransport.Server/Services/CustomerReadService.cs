using System.Data;
using Dapper;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;
using HamgamTransport.Server.Data.Models.Invoice;
using Microsoft.EntityFrameworkCore;

namespace HamgamTransport.Server.Services;

public interface ICustomerReadService
{
    Task<IReadOnlyList<CustomerListItem>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<CustomerDetailRow?> GetDetailAsync(int customerId, bool includeDeleted, CancellationToken cancellationToken = default);

    Task<CustomerDataTableResult> QueryDataTableAsync(
        CustomerDataTableQuery query,
        CancellationToken cancellationToken = default);

    Task<CustomerInvoiceDataTableResult> QuerySaleInvoicesDataTableAsync(
        int customerId,
        CustomerInvoiceDataTableQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> CustomerExistsAsync(int customerId, bool includeDeleted, CancellationToken cancellationToken = default);

    Task<string> GetBaseCurrencySymbolAsync(CancellationToken cancellationToken = default);
}

public sealed class CustomerReadService : ICustomerReadService
{
    private const string SummaryCte = """
        WITH PurchaseTotals AS (
            SELECT
                si.CustomerId,
                SUM(CASE
                    WHEN si.DocumentType = 1 THEN si.TotalAmountInBaseCurrency
                    WHEN si.DocumentType = 3 THEN -si.TotalAmountInBaseCurrency
                    ELSE 0
                END) AS TotalPurchase,
                -- دریافت فاکتور + بازپرداخت برگشت فروش (PaidAmount برگشت با علامت منفی برای فرمول مانده)
                SUM(CASE
                    WHEN si.DocumentType = 1 THEN
                        CASE
                            WHEN si.TotalAmount <> 0
                                THEN ROUND(si.PaidAmount * si.TotalAmountInBaseCurrency / si.TotalAmount, 4)
                            ELSE ROUND(si.PaidAmount * ISNULL(NULLIF(si.BaseUnitsPerUnitAtTransaction, 0), 1), 4)
                        END
                    WHEN si.DocumentType = 3 THEN
                        -CASE
                            WHEN si.TotalAmount <> 0
                                THEN ROUND(si.PaidAmount * si.TotalAmountInBaseCurrency / si.TotalAmount, 4)
                            ELSE ROUND(si.PaidAmount * ISNULL(NULLIF(si.BaseUnitsPerUnitAtTransaction, 0), 1), 4)
                        END
                    ELSE 0
                END) AS InvoicePayment
            FROM SaleInvoices si
            WHERE ISNULL(si.IsDeleted, 0) = 0
              AND si.IsPosted = 1
            GROUP BY si.CustomerId
        ),
        -- دریافت/پرداخت مستقل از فاکتور (تخصیص‌شده‌ها داخل PaidAmount فاکتور هستند)
        SettlementTotals AS (
            SELECT
                ps.PartyId AS CustomerId,
                SUM(ps.AmountInBaseCurrency) AS UnallocatedPayment
            FROM PartySettlements ps
            WHERE ISNULL(ps.IsDeleted, 0) = 0
              AND ps.PartyType = 1
              AND ps.SaleInvoiceId IS NULL
              AND ps.PurchaseInvoiceId IS NULL
            GROUP BY ps.PartyId
        ),
        CustomerSummary AS (
            SELECT
                c.CustomerID AS CustomerId,
                c.Name,
                ISNULL(
                    acc.Code,
                    CONCAT(N'121-', RIGHT(CONCAT(N'00000', CAST(c.CustomerID AS varchar(10))), 5))
                ) AS AccountCode,
                c.PhoneNumber,
                c.Address,
                c.City,
                c.Country,
                ISNULL(c.InitialBalance, 0) AS InitialBalance,
                c.CustomerType,
                CASE WHEN ISNULL(c.IsActive, 0) = 1 THEN 1 ELSE 0 END AS IsActive,
                CASE WHEN ISNULL(c.IsDeleted, 0) = 1 THEN 1 ELSE 0 END AS IsDeleted,
                ISNULL(pt.TotalPurchase, 0) AS TotalPurchase,
                ISNULL(pt.InvoicePayment, 0) + ISNULL(st.UnallocatedPayment, 0) AS TotalPayment,
                -- مانده منفی = مشتری به ما بدهکار است (فروش − دریافت)
                ISNULL(c.InitialBalance, 0)
                    - ISNULL(pt.TotalPurchase, 0)
                    + ISNULL(pt.InvoicePayment, 0)
                    + ISNULL(st.UnallocatedPayment, 0) AS Balance,
                CASE
                    WHEN ISNULL(c.InitialBalance, 0) - ISNULL(pt.TotalPurchase, 0) + ISNULL(pt.InvoicePayment, 0) + ISNULL(st.UnallocatedPayment, 0) > 0 THEN N'طلبکار'
                    WHEN ISNULL(c.InitialBalance, 0) - ISNULL(pt.TotalPurchase, 0) + ISNULL(pt.InvoicePayment, 0) + ISNULL(st.UnallocatedPayment, 0) < 0 THEN N'بدهکار'
                    ELSE N'تسویه'
                END AS AccountStatus,
                CASE
                    WHEN ISNULL(c.InitialBalance, 0) - ISNULL(pt.TotalPurchase, 0) + ISNULL(pt.InvoicePayment, 0) + ISNULL(st.UnallocatedPayment, 0) > 0 THEN 'creditor'
                    WHEN ISNULL(c.InitialBalance, 0) - ISNULL(pt.TotalPurchase, 0) + ISNULL(pt.InvoicePayment, 0) + ISNULL(st.UnallocatedPayment, 0) < 0 THEN 'debtor'
                    ELSE 'settled'
                END AS AccountStatusCode,
                c.CreatedAt
            FROM Customers c
            LEFT JOIN PurchaseTotals pt ON pt.CustomerId = c.CustomerID
            LEFT JOIN SettlementTotals st ON st.CustomerId = c.CustomerID
            LEFT JOIN Accounts acc
                ON acc.SystemCode = CONCAT(N'CUST_', c.CustomerID)
               AND ISNULL(acc.IsDeleted, 0) = 0
            WHERE (@IncludeDeleted = 1 OR ISNULL(c.IsDeleted, 0) = 0)
        )
        """;

    private static readonly Dictionary<int, string> CustomerOrderColumns = new()
    {
        [1] = "s.Name",
        [2] = "s.AccountCode",
        [3] = "s.PhoneNumber",
        [4] = "s.InitialBalance",
        [5] = "s.TotalPurchase",
        [6] = "s.TotalPayment",
        [7] = "s.Balance",
        [8] = "s.AccountStatus",
    };

    private static readonly Dictionary<int, string> InvoiceOrderColumns = new()
    {
        [1] = "si.InvoiceNumber",
        [2] = "si.InvoiceDate",
        [4] = "si.TotalAmountInBaseCurrency",
        [5] = """
            CASE
                WHEN si.TotalAmount <> 0
                    THEN ROUND(si.PaidAmount * si.TotalAmountInBaseCurrency / si.TotalAmount, 4)
                ELSE ROUND(si.PaidAmount * ISNULL(NULLIF(si.BaseUnitsPerUnitAtTransaction, 0), 1), 4)
            END
            """,
        [6] = "si.Status",
    };

    private readonly AppDbContext _db;

    public CustomerReadService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<CustomerListItem>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                c.CustomerID AS CustomerId,
                c.Name
            FROM Customers c
            WHERE ISNULL(c.IsDeleted, 0) = 0
              AND ISNULL(c.IsActive, 0) = 1
            ORDER BY c.Name
            """;

        var connection = await OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<CustomerListItem>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.AsList();
    }

    public async Task<CustomerDetailRow?> GetDetailAsync(
        int customerId,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var sql = SummaryCte + """
            SELECT TOP (1)
                s.CustomerId,
                s.Name,
                s.AccountCode,
                s.PhoneNumber,
                s.Address,
                s.City,
                s.Country,
                s.InitialBalance,
                s.CustomerType,
                s.IsActive,
                s.IsDeleted,
                s.TotalPurchase,
                s.TotalPayment,
                s.Balance,
                s.AccountStatus,
                s.AccountStatusCode,
                s.CreatedAt
            FROM CustomerSummary s
            WHERE s.CustomerId = @CustomerId
            """;

        var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<CustomerDetailRow>(
            new CommandDefinition(
                sql,
                new { CustomerId = customerId, IncludeDeleted = includeDeleted ? 1 : 0 },
                cancellationToken: cancellationToken));
    }

    public async Task<CustomerDataTableResult> QueryDataTableAsync(
        CustomerDataTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;
        var wantsDebtor = hasSearch && search.Contains("بده", StringComparison.Ordinal);
        var wantsCreditor = hasSearch && search.Contains("طلب", StringComparison.Ordinal);
        var wantsSettled = hasSearch && search.Contains("تسویه", StringComparison.Ordinal);
        decimal parsedNumber = 0m;
        var numericSearch = hasSearch &&
            decimal.TryParse(search.Replace(",", string.Empty), out parsedNumber);

        var parameters = new DynamicParameters();
        parameters.Add("IncludeDeleted", query.IncludeDeleted ? 1 : 0);
        parameters.Add("Start", query.Start);
        parameters.Add("Length", query.Length);
        parameters.Add("Search", hasSearch ? $"%{search}%": null);
        parameters.Add("WantsDebtor", wantsDebtor ? 1 : 0);
        parameters.Add("WantsCreditor", wantsCreditor ? 1 : 0);
        parameters.Add("WantsSettled", wantsSettled ? 1 : 0);
        parameters.Add("NumericSearch", numericSearch ? 1 : 0);
        parameters.Add("ParsedNumber", numericSearch ? parsedNumber : 0m);

        const string countSql = """
            SELECT COUNT(*)
            FROM Customers c
            WHERE (@IncludeDeleted = 1 OR ISNULL(c.IsDeleted, 0) = 0)
            """;

        var searchClause = hasSearch
            ? """
              AND (
                  s.Name LIKE @Search
                  OR s.AccountCode LIKE @Search
                  OR s.PhoneNumber LIKE @Search
                  OR s.AccountStatus LIKE @Search
                  OR (@WantsDebtor = 1 AND s.Balance < 0)
                  OR (@WantsCreditor = 1 AND s.Balance > 0)
                  OR (@WantsSettled = 1 AND s.Balance = 0)
                  OR (
                      @NumericSearch = 1
                      AND (
                          s.InitialBalance = @ParsedNumber
                          OR s.TotalPurchase = @ParsedNumber
                          OR s.TotalPayment = @ParsedNumber
                          OR s.Balance = @ParsedNumber
                      )
                  )
              )
              """
            : string.Empty;

        var orderClause = BuildOrderClause(CustomerOrderColumns, query.Order, "s.Name ASC");
        var dataSql = SummaryCte + $"""
            SELECT
                s.CustomerId,
                s.Name,
                s.AccountCode,
                s.PhoneNumber,
                s.Address,
                s.City,
                s.Country,
                s.InitialBalance,
                s.CustomerType,
                s.IsActive,
                s.IsDeleted,
                s.TotalPurchase,
                s.TotalPayment,
                s.Balance,
                s.AccountStatus,
                s.AccountStatusCode,
                COUNT(*) OVER() AS RecordsFiltered
            FROM CustomerSummary s
            WHERE 1 = 1
            {searchClause}
            {orderClause}
            OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY
            """;

        var connection = await OpenConnectionAsync(cancellationToken);

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<CustomerSummaryRow>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))).AsList();

        var recordsFiltered = rows.Count > 0
            ? rows[0].RecordsFiltered
            : hasSearch
                ? await CountFilteredAsync(connection, searchClause, parameters, cancellationToken)
                : recordsTotal;

        return new CustomerDataTableResult(rows, recordsTotal, recordsFiltered);
    }

    public async Task<CustomerInvoiceDataTableResult> QuerySaleInvoicesDataTableAsync(
        int customerId,
        CustomerInvoiceDataTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;

        var parameters = new DynamicParameters();
        parameters.Add("CustomerId", customerId);
        parameters.Add("Start", query.Start);
        parameters.Add("Length", query.Length);
        parameters.Add("Search", hasSearch ? $"%{search}%" : null);
        parameters.Add("SearchRaw", hasSearch ? search : null);
        parameters.Add("StatusProforma", (int)InvoiceStatus.Proforma);
        parameters.Add("StatusOrder", (int)InvoiceStatus.Order);
        parameters.Add("StatusQuotation", (int)InvoiceStatus.Quotation);
        parameters.Add("StatusInvoice", (int)InvoiceStatus.Invoice);

        const string baseWhere = """
            FROM SaleInvoices si
            WHERE ISNULL(si.IsDeleted, 0) = 0
              AND si.CustomerId = @CustomerId
            """;

        var searchClause = hasSearch
            ? """
              AND (
                  si.InvoiceNumber LIKE @Search
                  OR ISNULL(si.Description, '') LIKE @Search
                  OR CAST(si.TotalAmountInBaseCurrency AS NVARCHAR(50)) LIKE @Search
                  OR CAST(
                        CASE
                            WHEN si.TotalAmount <> 0
                                THEN ROUND(si.PaidAmount * si.TotalAmountInBaseCurrency / si.TotalAmount, 4)
                            ELSE ROUND(si.PaidAmount * ISNULL(NULLIF(si.BaseUnitsPerUnitAtTransaction, 0), 1), 4)
                        END
                     AS NVARCHAR(50)) LIKE @Search
                  OR (@SearchRaw LIKE N'%پیش%' AND si.Status = @StatusProforma)
                  OR (@SearchRaw LIKE N'%آردر%' AND si.Status = @StatusOrder)
                  OR (@SearchRaw LIKE N'%استعلام%' AND si.Status = @StatusQuotation)
                  OR (@SearchRaw LIKE N'%فاکتور%' AND si.Status = @StatusInvoice)
              )
              """
            : string.Empty;

        var orderClause = BuildOrderClause(InvoiceOrderColumns, query.Order, "si.InvoiceDate DESC");

        var countSql = "SELECT COUNT(*) " + baseWhere + searchClause;
        var dataSql = $"""
            SELECT
                si.SaleInvoiceID AS SaleInvoiceId,
                si.InvoiceNumber,
                si.InvoiceDate,
                si.Status,
                si.TotalAmountInBaseCurrency AS TotalAmount,
                CASE
                    WHEN si.TotalAmount <> 0
                        THEN ROUND(si.PaidAmount * si.TotalAmountInBaseCurrency / si.TotalAmount, 4)
                    ELSE ROUND(si.PaidAmount * ISNULL(NULLIF(si.BaseUnitsPerUnitAtTransaction, 0), 1), 4)
                END AS PaidAmount,
                (
                    SELECT COUNT(*)
                    FROM SalesItems x
                    WHERE x.SaleInvoiceId = si.SaleInvoiceID
                      AND ISNULL(x.IsDeleted, 0) = 0
                ) AS ItemsCount,
                si.IsPosted,
                COUNT(*) OVER() AS RecordsFiltered
            {baseWhere}
            {searchClause}
            {orderClause}
            OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY
            """;

        const string totalsSql = """
            SELECT
                ISNULL(SUM(CASE
                    WHEN si.DocumentType = 1 THEN si.TotalAmountInBaseCurrency
                    WHEN si.DocumentType = 3 THEN -si.TotalAmountInBaseCurrency
                    ELSE 0
                END), 0) AS TotalPurchase,
                ISNULL(SUM(CASE
                    WHEN si.DocumentType = 1 THEN
                        CASE
                            WHEN si.TotalAmount <> 0
                                THEN ROUND(si.PaidAmount * si.TotalAmountInBaseCurrency / si.TotalAmount, 4)
                            ELSE ROUND(si.PaidAmount * ISNULL(NULLIF(si.BaseUnitsPerUnitAtTransaction, 0), 1), 4)
                        END
                    WHEN si.DocumentType = 3 THEN
                        -CASE
                            WHEN si.TotalAmount <> 0
                                THEN ROUND(si.PaidAmount * si.TotalAmountInBaseCurrency / si.TotalAmount, 4)
                            ELSE ROUND(si.PaidAmount * ISNULL(NULLIF(si.BaseUnitsPerUnitAtTransaction, 0), 1), 4)
                        END
                    ELSE 0
                END), 0) AS TotalPayment
            FROM SaleInvoices si
            WHERE ISNULL(si.IsDeleted, 0) = 0
              AND si.IsPosted = 1
              AND si.CustomerId = @CustomerId
            """;

        var connection = await OpenConnectionAsync(cancellationToken);

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) " + baseWhere,
                new { CustomerId = customerId },
                cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<CustomerInvoiceRow>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))).AsList();

        var recordsFiltered = rows.Count > 0
            ? rows[0].RecordsFiltered
            : hasSearch
                ? await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken))
                : recordsTotal;

        var totals = await connection.QuerySingleAsync<CustomerInvoiceTotals>(
            new CommandDefinition(
                totalsSql,
                new { CustomerId = customerId },
                cancellationToken: cancellationToken));

        return new CustomerInvoiceDataTableResult(rows, recordsTotal, recordsFiltered, totals);
    }

    public async Task<bool> CustomerExistsAsync(
        int customerId,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM Customers c
                WHERE c.CustomerID = @CustomerId
                  AND (@IncludeDeleted = 1 OR ISNULL(c.IsDeleted, 0) = 0)
            ) THEN 1 ELSE 0 END
            """;

        var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { CustomerId = customerId, IncludeDeleted = includeDeleted ? 1 : 0 },
                cancellationToken: cancellationToken)) == 1;
    }

    public async Task<string> GetBaseCurrencySymbolAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT TOP (1) ISNULL(c.Symbol, '')
            FROM Currencies c
            WHERE ISNULL(c.IsDeleted, 0) = 0
              AND c.IsBaseCurrency = 1
            """;

        var connection = await OpenConnectionAsync(cancellationToken);
        var symbol = await connection.ExecuteScalarAsync<string>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return symbol ?? string.Empty;
    }

    private static async Task<int> CountFilteredAsync(
        IDbConnection connection,
        string searchClause,
        DynamicParameters parameters,
        CancellationToken cancellationToken)
    {
        var sql = SummaryCte + $"""
            SELECT COUNT(*)
            FROM CustomerSummary s
            WHERE 1 = 1
            {searchClause}
            """;

        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, parameters, cancellationToken: cancellationToken));
    }

    private static string BuildOrderClause(
        IReadOnlyDictionary<int, string> columns,
        List<DataTableOrder>? orders,
        string defaultOrder)
    {
        if (orders is null || orders.Count == 0)
        {
            return $"ORDER BY {defaultOrder}";
        }

        var parts = new List<string>();
        foreach (var order in orders)
        {
            if (!columns.TryGetValue(order.Column, out var column))
            {
                continue;
            }

            var direction = string.Equals(order.Dir, "desc", StringComparison.OrdinalIgnoreCase)
                ? "DESC"
                : "ASC";
            parts.Add($"{column} {direction}");
        }

        return parts.Count > 0
            ? "ORDER BY " + string.Join(", ", parts)
            : $"ORDER BY {defaultOrder}";
    }

    private async Task<IDbConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        return connection;
    }
}

public sealed class CustomerListItem
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class CustomerDetailRow
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountCode { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public PersonType CustomerType { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal Balance { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public string AccountStatusCode { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

public sealed class CustomerSummaryRow
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? AccountCode { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public PersonType CustomerType { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal Balance { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public string AccountStatusCode { get; set; } = string.Empty;
    public int RecordsFiltered { get; set; }
}

public sealed class CustomerInvoiceRow
{
    public int SaleInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int ItemsCount { get; set; }
    public bool IsPosted { get; set; }
    public int RecordsFiltered { get; set; }
}

public sealed class CustomerInvoiceTotals
{
    public decimal TotalPurchase { get; set; }
    public decimal TotalPayment { get; set; }
}

public sealed class CustomerDataTableQuery
{
    public bool IncludeDeleted { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? Search { get; set; }
    public List<DataTableOrder>? Order { get; set; }
}

public sealed class CustomerInvoiceDataTableQuery
{
    public int Start { get; set; }
    public int Length { get; set; }
    public string? Search { get; set; }
    public List<DataTableOrder>? Order { get; set; }
}

public sealed record CustomerDataTableResult(
    IReadOnlyList<CustomerSummaryRow> Rows,
    int RecordsTotal,
    int RecordsFiltered);

public sealed record CustomerInvoiceDataTableResult(
    IReadOnlyList<CustomerInvoiceRow> Rows,
    int RecordsTotal,
    int RecordsFiltered,
    CustomerInvoiceTotals Totals);
