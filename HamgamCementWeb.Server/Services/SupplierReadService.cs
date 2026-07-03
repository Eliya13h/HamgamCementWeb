using System.Data;
using Dapper;
using HamgamCementWeb.Server.Controllers.Transport;
using HamgamCementWeb.Server.Data;
using HamgamCementWeb.Server.Data.Models.Invoice;
using Microsoft.EntityFrameworkCore;

namespace HamgamCementWeb.Server.Services;

public interface ISupplierReadService
{
    Task<IReadOnlyList<SupplierListItem>> ListActiveAsync(CancellationToken cancellationToken = default);

    Task<SupplierDetailRow?> GetDetailAsync(int supplierId, bool includeDeleted, CancellationToken cancellationToken = default);

    Task<SupplierDataTableResult> QueryDataTableAsync(
        SupplierDataTableQuery query,
        CancellationToken cancellationToken = default);

    Task<SupplierInvoiceDataTableResult> QueryPurchaseInvoicesDataTableAsync(
        int supplierId,
        SupplierInvoiceDataTableQuery query,
        CancellationToken cancellationToken = default);

    Task<bool> SupplierExistsAsync(int supplierId, bool includeDeleted, CancellationToken cancellationToken = default);

    Task<string> GetBaseCurrencySymbolAsync(CancellationToken cancellationToken = default);
}

public sealed class SupplierReadService : ISupplierReadService
{
    private const string SummaryCte = """
        WITH PurchaseTotals AS (
            SELECT
                pi.SupplierId,
                SUM(CASE
                    WHEN pi.DocumentType = 1 THEN pi.TotalAmountInBaseCurrency
                    WHEN pi.DocumentType = 2 THEN -pi.TotalAmountInBaseCurrency
                    ELSE 0
                END) AS TotalPurchase,
                SUM(CASE
                    WHEN pi.DocumentType = 1 THEN pi.PaidAmount
                    WHEN pi.DocumentType = 2 THEN -pi.PaidAmount
                    ELSE 0
                END) AS TotalPayment
            FROM PurchaseInvoices pi
            WHERE ISNULL(pi.IsDeleted, 0) = 0
              AND pi.IsPosted = 1
            GROUP BY pi.SupplierId
        ),
        SupplierSummary AS (
            SELECT
                s.SupplierID AS SupplierId,
                s.Title,
                s.Name,
                s.PhoneNumber,
                s.Address,
                s.City,
                s.Country,
                ISNULL(s.InitialBalance, 0) AS InitialBalance,
                s.SupplierType,
                CASE WHEN ISNULL(s.IsActive, 0) = 1 THEN 1 ELSE 0 END AS IsActive,
                CASE WHEN ISNULL(s.IsDeleted, 0) = 1 THEN 1 ELSE 0 END AS IsDeleted,
                ISNULL(pt.TotalPurchase, 0) AS TotalPurchase,
                ISNULL(pt.TotalPayment, 0) AS TotalPayment,
                ISNULL(s.InitialBalance, 0) + ISNULL(pt.TotalPurchase, 0) - ISNULL(pt.TotalPayment, 0) AS Balance,
                CASE
                    WHEN ISNULL(s.InitialBalance, 0) + ISNULL(pt.TotalPurchase, 0) - ISNULL(pt.TotalPayment, 0) > 0 THEN N'بدهکار'
                    WHEN ISNULL(s.InitialBalance, 0) + ISNULL(pt.TotalPurchase, 0) - ISNULL(pt.TotalPayment, 0) < 0 THEN N'طلبکار'
                    ELSE N'تسویه'
                END AS AccountStatus,
                CASE
                    WHEN ISNULL(s.InitialBalance, 0) + ISNULL(pt.TotalPurchase, 0) - ISNULL(pt.TotalPayment, 0) > 0 THEN 'debtor'
                    WHEN ISNULL(s.InitialBalance, 0) + ISNULL(pt.TotalPurchase, 0) - ISNULL(pt.TotalPayment, 0) < 0 THEN 'creditor'
                    ELSE 'settled'
                END AS AccountStatusCode,
                s.CreatedAt
            FROM Suppliers s
            LEFT JOIN PurchaseTotals pt ON pt.SupplierId = s.SupplierID
            WHERE (@IncludeDeleted = 1 OR ISNULL(s.IsDeleted, 0) = 0)
        )
        """;

    private static readonly Dictionary<int, string> SupplierOrderColumns = new()
    {
        [1] = "s.Name",
        [2] = "s.PhoneNumber",
        [3] = "s.InitialBalance",
        [4] = "s.TotalPurchase",
        [5] = "s.TotalPayment",
        [6] = "s.Balance",
        [7] = "s.AccountStatus",
    };

    private static readonly Dictionary<int, string> InvoiceOrderColumns = new()
    {
        [1] = "pi.InvoiceNumber",
        [2] = "pi.InvoiceDate",
        [4] = "pi.TotalAmountInBaseCurrency",
        [5] = "pi.PaidAmount",
        [6] = "pi.Status",
    };

    private readonly AppDbContext _db;

    public SupplierReadService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SupplierListItem>> ListActiveAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                s.SupplierID AS SupplierId,
                s.Name
            FROM Suppliers s
            WHERE ISNULL(s.IsDeleted, 0) = 0
              AND ISNULL(s.IsActive, 0) = 1
            ORDER BY s.Name
            """;

        var connection = await OpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<SupplierListItem>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows.AsList();
    }

    public async Task<SupplierDetailRow?> GetDetailAsync(
        int supplierId,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var sql = SummaryCte + """
            SELECT TOP (1)
                s.SupplierId,
                s.Title,
                s.Name,
                s.PhoneNumber,
                s.Address,
                s.City,
                s.Country,
                s.InitialBalance,
                s.SupplierType,
                s.IsActive,
                s.IsDeleted,
                s.TotalPurchase,
                s.TotalPayment,
                s.Balance,
                s.AccountStatus,
                s.AccountStatusCode,
                s.CreatedAt
            FROM SupplierSummary s
            WHERE s.SupplierId = @SupplierId
            """;

        var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<SupplierDetailRow>(
            new CommandDefinition(
                sql,
                new { SupplierId = supplierId, IncludeDeleted = includeDeleted ? 1 : 0 },
                cancellationToken: cancellationToken));
    }

    public async Task<SupplierDataTableResult> QueryDataTableAsync(
        SupplierDataTableQuery query,
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
        parameters.Add("Search", hasSearch ? $"%{search}%" : null);
        parameters.Add("WantsDebtor", wantsDebtor ? 1 : 0);
        parameters.Add("WantsCreditor", wantsCreditor ? 1 : 0);
        parameters.Add("WantsSettled", wantsSettled ? 1 : 0);
        parameters.Add("NumericSearch", numericSearch ? 1 : 0);
        parameters.Add("ParsedNumber", numericSearch ? parsedNumber : 0m);

        const string countSql = """
            SELECT COUNT(*)
            FROM Suppliers s
            WHERE (@IncludeDeleted = 1 OR ISNULL(s.IsDeleted, 0) = 0)
            """;

        var searchClause = hasSearch
            ? """
              AND (
                  s.Name LIKE @Search
                  OR s.PhoneNumber LIKE @Search
                  OR s.AccountStatus LIKE @Search
                  OR (@WantsDebtor = 1 AND s.Balance > 0)
                  OR (@WantsCreditor = 1 AND s.Balance < 0)
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

        var orderClause = BuildOrderClause(SupplierOrderColumns, query.Order, "s.Name ASC");
        var dataSql = SummaryCte + $"""
            SELECT
                s.SupplierId,
                s.Title,
                s.Name,
                s.PhoneNumber,
                s.Address,
                s.City,
                s.Country,
                s.InitialBalance,
                s.SupplierType,
                s.IsActive,
                s.IsDeleted,
                s.TotalPurchase,
                s.TotalPayment,
                s.Balance,
                s.AccountStatus,
                s.AccountStatusCode,
                COUNT(*) OVER() AS RecordsFiltered
            FROM SupplierSummary s
            WHERE 1 = 1
            {searchClause}
            {orderClause}
            OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY
            """;

        var connection = await OpenConnectionAsync(cancellationToken);

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<SupplierSummaryRow>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))).AsList();

        var recordsFiltered = rows.Count > 0
            ? rows[0].RecordsFiltered
            : hasSearch
                ? await CountFilteredAsync(connection, searchClause, parameters, cancellationToken)
                : recordsTotal;

        return new SupplierDataTableResult(rows, recordsTotal, recordsFiltered);
    }

    public async Task<SupplierInvoiceDataTableResult> QueryPurchaseInvoicesDataTableAsync(
        int supplierId,
        SupplierInvoiceDataTableQuery query,
        CancellationToken cancellationToken = default)
    {
        var search = query.Search?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;

        var parameters = new DynamicParameters();
        parameters.Add("SupplierId", supplierId);
        parameters.Add("Start", query.Start);
        parameters.Add("Length", query.Length);
        parameters.Add("Search", hasSearch ? $"%{search}%" : null);
        parameters.Add("SearchRaw", hasSearch ? search : null);
        parameters.Add("StatusProforma", (int)InvoiceStatus.Proforma);
        parameters.Add("StatusOrder", (int)InvoiceStatus.Order);
        parameters.Add("StatusQuotation", (int)InvoiceStatus.Quotation);
        parameters.Add("StatusInvoice", (int)InvoiceStatus.Inoivce);

        const string baseWhere = """
            FROM PurchaseInvoices pi
            WHERE ISNULL(pi.IsDeleted, 0) = 0
              AND pi.SupplierId = @SupplierId
            """;

        var searchClause = hasSearch
            ? """
              AND (
                  pi.InvoiceNumber LIKE @Search
                  OR ISNULL(pi.Description, '') LIKE @Search
                  OR CAST(pi.TotalAmountInBaseCurrency AS NVARCHAR(50)) LIKE @Search
                  OR CAST(pi.PaidAmount AS NVARCHAR(50)) LIKE @Search
                  OR (@SearchRaw LIKE N'%پیش%' AND pi.Status = @StatusProforma)
                  OR (@SearchRaw LIKE N'%آردر%' AND pi.Status = @StatusOrder)
                  OR (@SearchRaw LIKE N'%استعلام%' AND pi.Status = @StatusQuotation)
                  OR (@SearchRaw LIKE N'%فاکتور%' AND pi.Status = @StatusInvoice)
              )
              """
            : string.Empty;

        var orderClause = BuildOrderClause(InvoiceOrderColumns, query.Order, "pi.InvoiceDate DESC");

        var countSql = "SELECT COUNT(*) " + baseWhere + searchClause;
        var dataSql = $"""
            SELECT
                pi.PurchaseInvoiceID AS PurchaseInvoiceId,
                pi.InvoiceNumber,
                pi.InvoiceDate,
                pi.Status,
                pi.TotalAmountInBaseCurrency AS TotalAmount,
                pi.PaidAmount,
                (
                    SELECT COUNT(*)
                    FROM PurchaseItems x
                    WHERE x.PurchaseInvoiceId = pi.PurchaseInvoiceID
                      AND ISNULL(x.IsDeleted, 0) = 0
                ) AS ItemsCount,
                pi.IsPosted,
                COUNT(*) OVER() AS RecordsFiltered
            {baseWhere}
            {searchClause}
            {orderClause}
            OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY
            """;

        const string totalsSql = """
            SELECT
                ISNULL(SUM(CASE
                    WHEN pi.DocumentType = 1 THEN pi.TotalAmountInBaseCurrency
                    WHEN pi.DocumentType = 2 THEN -pi.TotalAmountInBaseCurrency
                    ELSE 0
                END), 0) AS TotalPurchase,
                ISNULL(SUM(CASE
                    WHEN pi.DocumentType = 1 THEN pi.PaidAmount
                    WHEN pi.DocumentType = 2 THEN -pi.PaidAmount
                    ELSE 0
                END), 0) AS TotalPayment
            FROM PurchaseInvoices pi
            WHERE ISNULL(pi.IsDeleted, 0) = 0
              AND pi.IsPosted = 1
              AND pi.SupplierId = @SupplierId
            """;

        var connection = await OpenConnectionAsync(cancellationToken);

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) " + baseWhere,
                new { SupplierId = supplierId },
                cancellationToken: cancellationToken));

        var rows = (await connection.QueryAsync<SupplierInvoiceRow>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))).AsList();

        var recordsFiltered = rows.Count > 0
            ? rows[0].RecordsFiltered
            : hasSearch
                ? await connection.ExecuteScalarAsync<int>(
                    new CommandDefinition(countSql, parameters, cancellationToken: cancellationToken))
                : recordsTotal;

        var totals = await connection.QuerySingleAsync<SupplierInvoiceTotals>(
            new CommandDefinition(
                totalsSql,
                new { SupplierId = supplierId },
                cancellationToken: cancellationToken));

        return new SupplierInvoiceDataTableResult(rows, recordsTotal, recordsFiltered, totals);
    }

    public async Task<bool> SupplierExistsAsync(
        int supplierId,
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS (
                SELECT 1
                FROM Suppliers s
                WHERE s.SupplierID = @SupplierId
                  AND (@IncludeDeleted = 1 OR ISNULL(s.IsDeleted, 0) = 0)
            ) THEN 1 ELSE 0 END
            """;

        var connection = await OpenConnectionAsync(cancellationToken);
        return await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                sql,
                new { SupplierId = supplierId, IncludeDeleted = includeDeleted ? 1 : 0 },
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
            FROM SupplierSummary s
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

public sealed class SupplierListItem
{
    public int SupplierId { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class SupplierDetailRow
{
    public int SupplierId { get; set; }
    public PersonTitle Title { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public PersonType SupplierType { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal Balance { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public string AccountStatusCode { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
}

public sealed class SupplierSummaryRow
{
    public int SupplierId { get; set; }
    public PersonTitle Title { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal InitialBalance { get; set; }
    public PersonType SupplierType { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public decimal TotalPurchase { get; set; }
    public decimal TotalPayment { get; set; }
    public decimal Balance { get; set; }
    public string AccountStatus { get; set; } = string.Empty;
    public string AccountStatusCode { get; set; } = string.Empty;
    public int RecordsFiltered { get; set; }
}

public sealed class SupplierInvoiceRow
{
    public int PurchaseInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public InvoiceStatus Status { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public int ItemsCount { get; set; }
    public bool IsPosted { get; set; }
    public int RecordsFiltered { get; set; }
}

public sealed class SupplierInvoiceTotals
{
    public decimal TotalPurchase { get; set; }
    public decimal TotalPayment { get; set; }
}

public sealed class SupplierDataTableQuery
{
    public bool IncludeDeleted { get; set; }
    public int Start { get; set; }
    public int Length { get; set; }
    public string? Search { get; set; }
    public List<DataTableOrder>? Order { get; set; }
}

public sealed class SupplierInvoiceDataTableQuery
{
    public int Start { get; set; }
    public int Length { get; set; }
    public string? Search { get; set; }
    public List<DataTableOrder>? Order { get; set; }
}

public sealed record SupplierDataTableResult(
    IReadOnlyList<SupplierSummaryRow> Rows,
    int RecordsTotal,
    int RecordsFiltered);

public sealed record SupplierInvoiceDataTableResult(
    IReadOnlyList<SupplierInvoiceRow> Rows,
    int RecordsTotal,
    int RecordsFiltered,
    SupplierInvoiceTotals Totals);
