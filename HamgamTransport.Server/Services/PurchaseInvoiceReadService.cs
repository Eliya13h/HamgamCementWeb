using System.Data;
using Dapper;
using HamgamTransport.Server.Controllers.Common;
using HamgamTransport.Server.Data;

namespace HamgamTransport.Server.Services;

public interface IPurchaseInvoiceReadService
{
    Task<PurchaseInvoiceDataTableResult> QueryDataTableAsync(
        DataTableRequest request,
        CancellationToken cancellationToken = default);

    Task<string> GetNextCodePreviewAsync(CancellationToken cancellationToken = default);

    Task<PurchaseInvoiceDetailRow?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

public sealed class PurchaseInvoiceReadService : IPurchaseInvoiceReadService
{
    // ایندکس ستون‌ها باید با ترتیب ستون‌های DataTable فرانت یکی باشد
    private static readonly Dictionary<int, string> OrderColumns = new()
    {
        [0] = "pi.PurchaseInvoiceID",
        [1] = "pi.InvoiceNumber",
        [2] = "pi.DocumentType",
        [3] = "s.Name",
        [4] = "w.Name",
        [5] = "pi.InvoiceDate",
        [6] = "pi.TotalAmount",
        [7] = "pi.TotalAmountInBaseCurrency",
        [8] = "pi.IsPosted",
    };

    private readonly ISqlConnectionFactory _sql;

    public PurchaseInvoiceReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<PurchaseInvoiceDataTableResult> QueryDataTableAsync(
        DataTableRequest request,
        CancellationToken cancellationToken = default)
    {
        var start = Math.Max(request.Start, 0);
        var length = request.Length <= 0 ? 10 : Math.Min(request.Length, 100);
        var search = request.Search?.Value?.Trim() ?? string.Empty;
        var hasSearch = search.Length > 0;

        var parameters = new DynamicParameters();
        parameters.Add("Start", start);
        parameters.Add("Length", length);
        parameters.Add("Search", hasSearch ? $"%{search}%" : null);

        const string baseFrom = """
            FROM PurchaseInvoices pi
            LEFT JOIN Suppliers s ON s.SupplierID = pi.SupplierId
            LEFT JOIN Warehouses w ON w.WarehouseID = pi.WarehouseId
            LEFT JOIN Currencies cur ON cur.CurrencyID = pi.CurrencyId
            LEFT JOIN Currencies bcur ON bcur.CurrencyID = pi.BaseCurrencyId
            LEFT JOIN ProductionBatches pb ON pb.ProductionBatchID = pi.ProductionBatchId
            LEFT JOIN PurchaseInvoices ref ON ref.PurchaseInvoiceID = pi.ReferencePurchaseInvoiceId
            """;

        const string baseWhere = """
            WHERE ISNULL(pi.IsDeleted, 0) = 0
            """;

        var searchClause = hasSearch
            ? """
              AND (
                  pi.InvoiceNumber LIKE @Search
                  OR ISNULL(pi.Description, N'') LIKE @Search
                  OR ISNULL(s.Name, N'') LIKE @Search
                  OR ISNULL(w.Name, N'') LIKE @Search
              )
              """
            : string.Empty;

        var orderClause = BuildOrderClause(request.Order, "pi.InvoiceDate DESC, pi.PurchaseInvoiceID DESC");

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var recordsTotal = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) FROM PurchaseInvoices pi {baseWhere}",
                cancellationToken: cancellationToken));

        var countFilteredSql = $"""
            SELECT COUNT(1)
            {baseFrom}
            {baseWhere}
            {searchClause}
            """;

        var recordsFiltered = hasSearch
            ? await connection.ExecuteScalarAsync<int>(
                new CommandDefinition(countFilteredSql, parameters, cancellationToken: cancellationToken))
            : recordsTotal;

        var dataSql = $"""
            SELECT
                pi.PurchaseInvoiceID AS PurchaseInvoiceId,
                pi.InvoiceNumber,
                pi.SupplierId,
                ISNULL(s.Name, N'') AS SupplierName,
                pi.WarehouseId,
                ISNULL(w.Name, N'') AS WarehouseName,
                pi.InvoiceDate,
                CAST(pi.Status AS int) AS Status,
                pi.CurrencyId,
                ISNULL(cur.Name, N'') AS CurrencyName,
                ISNULL(cur.Symbol, N'') AS CurrencySymbol,
                ISNULL(bcur.Symbol, N'') AS BaseCurrencySymbol,
                pi.TotalAmount,
                pi.TotalAmountInBaseCurrency,
                CAST(pi.DocumentType AS int) AS DocumentType,
                CASE WHEN ISNULL(pi.EntrySource, 0) = 0 THEN 1 ELSE CAST(pi.EntrySource AS int) END AS EntrySource,
                pi.ProductionBatchId,
                pb.BatchNumber AS ProductionBatchNumber,
                pi.ReferencePurchaseInvoiceId,
                ref.InvoiceNumber AS ReferenceInvoiceNumber,
                CASE WHEN pi.IsPosted = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsPosted,
                (
                    SELECT COUNT(1)
                    FROM PurchaseItems x
                    WHERE x.PurchaseInvoiceId = pi.PurchaseInvoiceID
                      AND ISNULL(x.IsDeleted, 0) = 0
                ) AS ItemsCount,
                pi.Description
            {baseFrom}
            {baseWhere}
            {searchClause}
            {orderClause}
            OFFSET @Start ROWS FETCH NEXT @Length ROWS ONLY
            """;

        var rows = (await connection.QueryAsync<PurchaseInvoiceListRow>(
            new CommandDefinition(dataSql, parameters, cancellationToken: cancellationToken))).AsList();

        return new PurchaseInvoiceDataTableResult(rows, recordsTotal, recordsFiltered, start);
    }

    public async Task<string> GetNextCodePreviewAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);
        var nextId = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT ISNULL(MAX(PurchaseInvoiceID), 0) + 1 FROM PurchaseInvoices",
                cancellationToken: cancellationToken));
        return InvoiceCodeHelper.ForPurchase(nextId);
    }

    public async Task<PurchaseInvoiceDetailRow?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string headerSql = """
            SELECT
                pi.PurchaseInvoiceID AS PurchaseInvoiceId,
                pi.InvoiceNumber,
                pi.SupplierId,
                pi.WarehouseId,
                pi.InvoiceDate,
                CAST(pi.Status AS int) AS Status,
                pi.CurrencyId,
                pi.BaseCurrencyId,
                pi.ExchangeHistoryId,
                pi.BaseUnitsPerUnitAtTransaction,
                pi.TotalAmount,
                pi.TotalAmountInBaseCurrency,
                pi.SubTotalAmount,
                pi.TaxPercent,
                pi.TaxAmount,
                pi.PaymentTermDays,
                pi.DueDate,
                pi.PaidAmount,
                pi.CashBoxId,
                CASE WHEN pi.IsCash = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsCash,
                CAST(pi.DocumentType AS int) AS DocumentType,
                CASE WHEN ISNULL(pi.EntrySource, 0) = 0 THEN 1 ELSE CAST(pi.EntrySource AS int) END AS EntrySource,
                pi.ProductionBatchId,
                pb.BatchNumber AS ProductionBatchNumber,
                pi.ReferencePurchaseInvoiceId,
                ref.InvoiceNumber AS ReferenceInvoiceNumber,
                CASE WHEN pi.IsPosted = 1 THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END AS IsPosted,
                pi.PostedAt,
                pi.Description
            FROM PurchaseInvoices pi
            LEFT JOIN ProductionBatches pb ON pb.ProductionBatchID = pi.ProductionBatchId
            LEFT JOIN PurchaseInvoices ref ON ref.PurchaseInvoiceID = pi.ReferencePurchaseInvoiceId
            WHERE pi.PurchaseInvoiceID = @Id
              AND ISNULL(pi.IsDeleted, 0) = 0
            """;

        const string itemsSql = """
            SELECT
                x.PurchaseItemID AS PurchaseItemId,
                x.ProductId,
                ISNULL(p.Name, N'') AS ProductName,
                ISNULL(p.Code, N'') AS ProductCode,
                x.MeaurmentId,
                ISNULL(m.Name, N'') AS MeaurmentName,
                x.Quantity,
                x.QuantityInBase,
                x.UnitPrice,
                x.LineTotal,
                x.LineTotalInBaseCurrency,
                CASE
                    WHEN x.QuantityInBase > 0
                        THEN x.Quantity * x.ReturnedQuantityInBase / x.QuantityInBase
                    ELSE 0
                END AS ReturnedQuantity,
                x.InventoryLotId
            FROM PurchaseItems x
            LEFT JOIN Products p ON p.ProductID = x.ProductId
            LEFT JOIN Meaurments m ON m.MeaurmentID = x.MeaurmentId
            WHERE x.PurchaseInvoiceId = @Id
              AND ISNULL(x.IsDeleted, 0) = 0
            ORDER BY x.PurchaseItemID
            """;

        await using var connection = (System.Data.Common.DbConnection)await _sql.OpenAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<PurchaseInvoiceDetailRow>(
            new CommandDefinition(headerSql, new { Id = id }, cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var items = (await connection.QueryAsync<PurchaseInvoiceItemRow>(
            new CommandDefinition(itemsSql, new { Id = id }, cancellationToken: cancellationToken))).AsList();

        header.Items = items;
        return header;
    }

    private static string BuildOrderClause(List<DataTableOrder>? orders, string defaultOrder)
    {
        if (orders is null || orders.Count == 0)
        {
            return $"ORDER BY {defaultOrder}";
        }

        var parts = new List<string>();
        foreach (var order in orders)
        {
            if (!OrderColumns.TryGetValue(order.Column, out var column))
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
}

public sealed class PurchaseInvoiceDataTableResult
{
    public PurchaseInvoiceDataTableResult(
        IReadOnlyList<PurchaseInvoiceListRow> rows,
        int recordsTotal,
        int recordsFiltered,
        int start)
    {
        Rows = rows;
        RecordsTotal = recordsTotal;
        RecordsFiltered = recordsFiltered;
        Start = start;
    }

    public IReadOnlyList<PurchaseInvoiceListRow> Rows { get; }
    public int RecordsTotal { get; }
    public int RecordsFiltered { get; }
    public int Start { get; }
}

public sealed class PurchaseInvoiceListRow
{
    public int PurchaseInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public int Status { get; set; }
    public int CurrencyId { get; set; }
    public string CurrencyName { get; set; } = string.Empty;
    public string CurrencySymbol { get; set; } = string.Empty;
    public string BaseCurrencySymbol { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountInBaseCurrency { get; set; }
    public int DocumentType { get; set; }
    public int EntrySource { get; set; }
    public int? ProductionBatchId { get; set; }
    public string? ProductionBatchNumber { get; set; }
    public int? ReferencePurchaseInvoiceId { get; set; }
    public string? ReferenceInvoiceNumber { get; set; }
    public bool IsPosted { get; set; }
    public int ItemsCount { get; set; }
    public string? Description { get; set; }
}

public sealed class PurchaseInvoiceDetailRow
{
    public int PurchaseInvoiceId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public DateTime InvoiceDate { get; set; }
    public int Status { get; set; }
    public int CurrencyId { get; set; }
    public int BaseCurrencyId { get; set; }
    public int? ExchangeHistoryId { get; set; }
    public decimal BaseUnitsPerUnitAtTransaction { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountInBaseCurrency { get; set; }
    public decimal SubTotalAmount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public int PaymentTermDays { get; set; }
    public DateTime? DueDate { get; set; }
    public decimal PaidAmount { get; set; }
    public int? CashBoxId { get; set; }
    public bool IsCash { get; set; }
    public int DocumentType { get; set; }
    public int EntrySource { get; set; }
    public int? ProductionBatchId { get; set; }
    public string? ProductionBatchNumber { get; set; }
    public int? ReferencePurchaseInvoiceId { get; set; }
    public string? ReferenceInvoiceNumber { get; set; }
    public bool IsPosted { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? Description { get; set; }
    public IReadOnlyList<PurchaseInvoiceItemRow> Items { get; set; } = [];
}

public sealed class PurchaseInvoiceItemRow
{
    public int PurchaseItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal QuantityInBase { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal LineTotalInBaseCurrency { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public int? InventoryLotId { get; set; }
}
