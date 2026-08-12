using System.Data.Common;
using Dapper;
using HamgamTransport.Server.Data;

namespace HamgamTransport.Server.Services;

public interface IProductionBatchReadService
{
    Task<(int Total, int Filtered, IReadOnlyList<ProductionBatchListRow> Rows)> GetDataTableAsync(
        int start,
        int length,
        string? search,
        string orderColumn,
        bool ascending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionBatchOptionRow>> GetListAsync(
        int start = 0,
        int length = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// جزئیات سند — خطوط مصرف/خروجی/هزینه فقط در این مرحله بارگذاری می‌شوند (lazy نسبت به لیست).
    /// </summary>
    Task<ProductionBatchDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ProductionTraceResult?> GetTraceAsync(int productionBatchId, CancellationToken cancellationToken = default);

    /// <summary>
    /// دادهٔ خام پیش‌نمایش ثبت (بدون تخصیص FIFO) — برای لایهٔ Posting.
    /// </summary>
    Task<ProductionBatchPreviewLoadDto?> LoadPreviewBatchAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// خواندن اسناد تولید روزانه با Dapper + صفحه‌بندی OFFSET/FETCH.
/// </summary>
public class ProductionBatchReadService : IProductionBatchReadService
{
    private static readonly HashSet<string> AllowedOrderColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "BatchNumber", "ProductionDate", "Status", "TotalCostInBase",
    };

    private readonly ISqlConnectionFactory _sql;

    public ProductionBatchReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<(int Total, int Filtered, IReadOnlyList<ProductionBatchListRow> Rows)> GetDataTableAsync(
        int start,
        int length,
        string? search,
        string orderColumn,
        bool ascending,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var orderBy = AllowedOrderColumns.Contains(orderColumn) ? orderColumn : "ProductionDate";
        var dir = ascending ? "ASC" : "DESC";
        var orderSql = orderBy switch
        {
            "BatchNumber" => $"b.BatchNumber {dir}",
            "Status" => $"b.Status {dir}",
            "TotalCostInBase" => $"b.TotalCostInBase {dir}",
            _ => $"b.ProductionDate {dir}",
        };

        const string baseWhere = "WHERE b.IsDeleted = 0";
        var where = baseWhere;
        var p = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            where += """
                 AND (
                    b.BatchNumber LIKE @Search
                    OR w.Name LIKE @Search
                    OR ISNULL(f.Name, N'') LIKE @Search
                    OR ISNULL(b.Description, N'') LIKE @Search
                 )
                """;
            p.Add("Search", $"%{search.Trim()}%");
        }

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) FROM dbo.ProductionBatches b {baseWhere}",
                cancellationToken: cancellationToken));

        var filtered = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"""
                 SELECT COUNT(1)
                 FROM dbo.ProductionBatches b
                 INNER JOIN dbo.Warehouses w ON w.WarehouseID = b.OutputWarehouseId
                 LEFT JOIN dbo.ProductionFormulas f ON f.ProductionFormulaID = b.ProductionFormulaId
                 {where}
                 """,
                p,
                cancellationToken: cancellationToken));

        p.Add("Offset", start);
        p.Add("Fetch", length);

        // لیست سبک: فقط شمارش خطوط، بدون بارگذاری ناوبری‌ها
        var rows = (await connection.QueryAsync<ProductionBatchListRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     b.ProductionBatchID AS ProductionBatchId,
                     b.BatchNumber,
                     b.ProductionDate,
                     b.ProductionFormulaId,
                     f.Name AS FormulaName,
                     b.ProductionPlanId,
                     CASE
                         WHEN pl.ProductionPlanID IS NULL THEN NULL
                         ELSE pr.Name + N' / ' + CONVERT(varchar(10), pl.PlanDate, 23)
                     END AS PlanLabel,
                     b.OutputWarehouseId,
                     w.Name AS OutputWarehouseName,
                     CAST(b.Status AS int) AS Status,
                     CAST(CASE WHEN b.IsPosted = 1 THEN 1 ELSE 0 END AS bit) AS IsPosted,
                     b.TotalMaterialCostInBase,
                     b.TotalConversionCostInBase,
                     b.TotalCostInBase,
                     (SELECT COUNT(1) FROM dbo.ProductionInputLines i
                      WHERE i.ProductionBatchId = b.ProductionBatchID AND i.IsDeleted = 0) AS InputLinesCount,
                     (SELECT COUNT(1) FROM dbo.ProductionOutputLines o
                      WHERE o.ProductionBatchId = b.ProductionBatchID AND o.IsDeleted = 0) AS OutputLinesCount,
                     b.Description
                 FROM dbo.ProductionBatches b
                 INNER JOIN dbo.Warehouses w ON w.WarehouseID = b.OutputWarehouseId
                 LEFT JOIN dbo.ProductionFormulas f ON f.ProductionFormulaID = b.ProductionFormulaId
                 LEFT JOIN dbo.ProductionPlans pl ON pl.ProductionPlanID = b.ProductionPlanId
                 LEFT JOIN dbo.Products pr ON pr.ProductID = pl.ProductId
                 {where}
                 ORDER BY {orderSql}, b.ProductionBatchID DESC
                 OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
                 """,
                p,
                cancellationToken: cancellationToken))).ToList();

        return (total, filtered, rows);
    }

    public async Task<IReadOnlyList<ProductionBatchOptionRow>> GetListAsync(
        int start = 0,
        int length = 100,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var fetch = length <= 0 ? 100 : Math.Min(length, 200);
        var offset = Math.Max(start, 0);

        var rows = await connection.QueryAsync<ProductionBatchOptionRow>(
            new CommandDefinition(
                """
                SELECT
                    b.ProductionBatchID AS Value,
                    b.BatchNumber AS Label,
                    b.ProductionDate,
                    b.OutputWarehouseId
                FROM dbo.ProductionBatches b
                WHERE b.IsDeleted = 0 AND b.IsPosted = 1
                ORDER BY b.ProductionDate DESC, b.ProductionBatchID DESC
                OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
                """,
                new { Offset = offset, Fetch = fetch },
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<ProductionBatchDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<ProductionBatchHeaderRow>(
            new CommandDefinition(
                """
                SELECT
                    b.ProductionBatchID AS ProductionBatchId,
                    b.BatchNumber,
                    b.ProductionDate,
                    b.ProductionFormulaId,
                    f.Name AS FormulaName,
                    CAST(f.Mode AS int) AS FormulaMode,
                    b.ProductionPlanId,
                    CASE
                        WHEN pl.ProductionPlanID IS NULL THEN NULL
                        ELSE pr.Name + N' / ' + CONVERT(varchar(10), pl.PlanDate, 23)
                    END AS PlanLabel,
                    b.OutputWarehouseId,
                    w.Name AS OutputWarehouseName,
                    CAST(b.Status AS int) AS Status,
                    CAST(CASE WHEN b.IsPosted = 1 THEN 1 ELSE 0 END AS bit) AS IsPosted,
                    b.TotalMaterialCostInBase,
                    b.TotalConversionCostInBase,
                    b.TotalCostInBase,
                    b.JournalEntryId,
                    b.Description
                FROM dbo.ProductionBatches b
                INNER JOIN dbo.Warehouses w ON w.WarehouseID = b.OutputWarehouseId
                LEFT JOIN dbo.ProductionFormulas f ON f.ProductionFormulaID = b.ProductionFormulaId
                LEFT JOIN dbo.ProductionPlans pl ON pl.ProductionPlanID = b.ProductionPlanId
                LEFT JOIN dbo.Products pr ON pr.ProductID = pl.ProductId
                WHERE b.ProductionBatchID = @Id AND b.IsDeleted = 0
                """,
                new { Id = id },
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        // Lazy: خطوط فقط بعد از یافتن هدر
        var inputLines = (await connection.QueryAsync<ProductionBatchInputRow>(
            new CommandDefinition(
                """
                SELECT
                    i.ProductionInputLineID AS ProductionInputLineId,
                    i.WarehouseId,
                    wh.Name AS WarehouseName,
                    i.ProductId,
                    p.Name AS ProductName,
                    i.MeaurmentId,
                    m.Name AS MeaurmentName,
                    i.Quantity,
                    i.QuantityInBase,
                    i.MaterialCostInBase
                FROM dbo.ProductionInputLines i
                INNER JOIN dbo.Warehouses wh ON wh.WarehouseID = i.WarehouseId
                INNER JOIN dbo.Products p ON p.ProductID = i.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = i.MeaurmentId
                WHERE i.ProductionBatchId = @Id AND i.IsDeleted = 0
                ORDER BY i.ProductionInputLineID
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        var outputLines = (await connection.QueryAsync<ProductionBatchOutputRow>(
            new CommandDefinition(
                """
                SELECT
                    o.ProductionOutputLineID AS ProductionOutputLineId,
                    o.ProductId,
                    p.Name AS ProductName,
                    o.MeaurmentId,
                    m.Name AS MeaurmentName,
                    o.Quantity,
                    o.QuantityInBase,
                    o.UnitCostInBase,
                    o.InventoryLotId
                FROM dbo.ProductionOutputLines o
                INNER JOIN dbo.Products p ON p.ProductID = o.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = o.MeaurmentId
                WHERE o.ProductionBatchId = @Id AND o.IsDeleted = 0
                ORDER BY o.ProductionOutputLineID
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        var costLines = (await connection.QueryAsync<ProductionBatchCostRow>(
            new CommandDefinition(
                """
                SELECT
                    c.ProductionBatchCostLineID AS ProductionBatchCostLineId,
                    CAST(c.CostType AS int) AS CostType,
                    c.Description,
                    c.Amount,
                    c.AccountId
                FROM dbo.ProductionBatchCostLines c
                WHERE c.ProductionBatchId = @Id AND c.IsDeleted = 0
                ORDER BY c.ProductionBatchCostLineID
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        return new ProductionBatchDetailDto
        {
            ProductionBatchId = header.ProductionBatchId,
            BatchNumber = header.BatchNumber,
            ProductionDate = header.ProductionDate,
            ProductionFormulaId = header.ProductionFormulaId,
            FormulaName = header.FormulaName,
            FormulaMode = header.FormulaMode,
            ProductionPlanId = header.ProductionPlanId,
            PlanLabel = header.PlanLabel,
            OutputWarehouseId = header.OutputWarehouseId,
            OutputWarehouseName = header.OutputWarehouseName,
            Status = header.Status,
            IsPosted = header.IsPosted,
            TotalMaterialCostInBase = header.TotalMaterialCostInBase,
            TotalConversionCostInBase = header.TotalConversionCostInBase,
            TotalCostInBase = header.TotalCostInBase,
            JournalEntryId = header.JournalEntryId,
            Description = header.Description,
            InputLines = inputLines,
            OutputLines = outputLines,
            CostLines = costLines,
        };
    }

    public async Task<ProductionTraceResult?> GetTraceAsync(
        int productionBatchId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<ProductionBatchTraceHeaderRow>(
            new CommandDefinition(
                """
                SELECT
                    b.ProductionBatchID AS ProductionBatchId,
                    b.BatchNumber,
                    b.ProductionDate,
                    w.Name AS OutputWarehouseName,
                    b.TotalMaterialCostInBase,
                    b.TotalConversionCostInBase,
                    b.TotalCostInBase,
                    b.FixedCost,
                    b.VariableCost,
                    b.JournalEntryId
                FROM dbo.ProductionBatches b
                INNER JOIN dbo.Warehouses w ON w.WarehouseID = b.OutputWarehouseId
                WHERE b.ProductionBatchID = @Id AND b.IsDeleted = 0
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var inputLines = (await connection.QueryAsync(
            new CommandDefinition(
                """
                SELECT
                    i.ProductionInputLineID AS productionInputLineId,
                    i.WarehouseId AS warehouseId,
                    wh.Name AS warehouseName,
                    i.ProductId AS productId,
                    p.Name AS productName,
                    i.Quantity AS quantity,
                    i.QuantityInBase AS quantityInBase,
                    m.Name AS meaurmentName,
                    i.MaterialCostInBase AS materialCostInBase
                FROM dbo.ProductionInputLines i
                INNER JOIN dbo.Warehouses wh ON wh.WarehouseID = i.WarehouseId
                INNER JOIN dbo.Products p ON p.ProductID = i.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = i.MeaurmentId
                WHERE i.ProductionBatchId = @Id AND i.IsDeleted = 0
                ORDER BY i.ProductionInputLineID
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken))).ToList();

        var outputLines = (await connection.QueryAsync(
            new CommandDefinition(
                """
                SELECT
                    o.ProductionOutputLineID AS productionOutputLineId,
                    o.ProductId AS productId,
                    p.Name AS productName,
                    o.Quantity AS quantity,
                    o.QuantityInBase AS quantityInBase,
                    m.Name AS meaurmentName,
                    o.UnitCostInBase AS unitCostInBase,
                    o.InventoryLotId AS inventoryLotId
                FROM dbo.ProductionOutputLines o
                INNER JOIN dbo.Products p ON p.ProductID = o.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = o.MeaurmentId
                WHERE o.ProductionBatchId = @Id AND o.IsDeleted = 0
                ORDER BY o.ProductionOutputLineID
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken))).ToList();

        var costLines = (await connection.QueryAsync(
            new CommandDefinition(
                """
                SELECT
                    c.ProductionBatchCostLineID AS productionBatchCostLineId,
                    CAST(c.CostType AS int) AS costType,
                    c.Description AS description,
                    c.Amount AS amount,
                    c.AccountId AS accountId
                FROM dbo.ProductionBatchCostLines c
                WHERE c.ProductionBatchId = @Id AND c.IsDeleted = 0
                ORDER BY c.ProductionBatchCostLineID
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken))).ToList();

        var lotsRaw = (await connection.QueryAsync<ProductionTraceLotRaw>(
            new CommandDefinition(
                """
                SELECT
                    l.InventoryLotID AS InventoryLotId,
                    l.LotCode,
                    l.ProductId,
                    p.Name AS ProductName,
                    l.ReceivedQuantityInBase,
                    l.RemainingQuantityInBase,
                    l.UnitCost,
                    l.PurchaseInvoiceId
                FROM dbo.InventoryLots l
                INNER JOIN dbo.Products p ON p.ProductID = l.ProductId
                WHERE l.ProductionBatchId = @Id AND l.IsDeleted = 0
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken))).ToList();

        var purchaseIds = lotsRaw
            .Where(l => l.PurchaseInvoiceId.HasValue)
            .Select(l => l.PurchaseInvoiceId!.Value)
            .Distinct()
            .ToList();

        var invoiceNumbers = new Dictionary<int, string>();
        if (purchaseIds.Count > 0)
        {
            var invoiceRows = await connection.QueryAsync<(int PurchaseInvoiceID, string InvoiceNumber)>(
                new CommandDefinition(
                    """
                    SELECT PurchaseInvoiceID, InvoiceNumber
                    FROM dbo.PurchaseInvoices
                    WHERE PurchaseInvoiceID IN @Ids
                    """,
                    new { Ids = purchaseIds },
                    cancellationToken: cancellationToken));
            invoiceNumbers = invoiceRows.ToDictionary(x => x.PurchaseInvoiceID, x => x.InvoiceNumber);
        }

        var lots = lotsRaw
            .Select(l => new ProductionTraceLot(
                l.InventoryLotId,
                l.LotCode,
                l.ProductId,
                l.ProductName,
                l.ReceivedQuantityInBase,
                l.RemainingQuantityInBase,
                l.UnitCost,
                l.PurchaseInvoiceId,
                l.PurchaseInvoiceId.HasValue && invoiceNumbers.TryGetValue(l.PurchaseInvoiceId.Value, out var num)
                    ? num
                    : null))
            .ToList();

        var consumedLots = (await connection.QueryAsync<ProductionTraceConsumedLot>(
            new CommandDefinition(
                """
                SELECT
                    a.ProductionInputLineId,
                    i.ProductId,
                    p.Name AS ProductName,
                    a.InventoryLotId,
                    l.LotCode,
                    a.QuantityInBase,
                    a.UnitCostInBase,
                    a.LineCostInBase
                FROM dbo.ProductionInputLotAllocations a
                INNER JOIN dbo.ProductionInputLines i ON i.ProductionInputLineID = a.ProductionInputLineId
                INNER JOIN dbo.Products p ON p.ProductID = i.ProductId
                INNER JOIN dbo.InventoryLots l ON l.InventoryLotID = a.InventoryLotId
                WHERE i.ProductionBatchId = @Id
                  AND i.IsDeleted = 0
                  AND a.IsDeleted = 0
                ORDER BY a.ProductionInputLotAllocationID
                """,
                new { Id = productionBatchId },
                cancellationToken: cancellationToken))).ToList();

        var lotIds = lotsRaw.Select(l => l.InventoryLotId).ToList();
        List<ProductionTraceSale> sales = [];
        if (lotIds.Count > 0)
        {
            sales = (await connection.QueryAsync<ProductionTraceSale>(
                new CommandDefinition(
                    """
                    SELECT
                        si.SaleInvoiceId,
                        inv.InvoiceNumber,
                        inv.InvoiceDate,
                        a.QuantityInBase,
                        a.InventoryLotId,
                        l.LotCode
                    FROM dbo.SaleItemLotAllocations a
                    INNER JOIN dbo.SalesItems si ON si.SalesItemID = a.SalesItemId
                    INNER JOIN dbo.SaleInvoices inv ON inv.SaleInvoiceID = si.SaleInvoiceId
                    INNER JOIN dbo.InventoryLots l ON l.InventoryLotID = a.InventoryLotId
                    WHERE a.InventoryLotId IN @LotIds AND a.IsDeleted = 0
                    ORDER BY inv.InvoiceDate, inv.SaleInvoiceID
                    """,
                    new { LotIds = lotIds },
                    cancellationToken: cancellationToken))).ToList();
        }

        var totalCost = header.TotalCostInBase > 0
            ? header.TotalCostInBase
            : header.TotalMaterialCostInBase + header.FixedCost + header.VariableCost;

        return new ProductionTraceResult(
            header.ProductionBatchId,
            header.BatchNumber,
            header.ProductionDate,
            header.OutputWarehouseName,
            header.TotalMaterialCostInBase,
            header.TotalConversionCostInBase,
            totalCost,
            header.FixedCost,
            header.VariableCost,
            header.JournalEntryId,
            inputLines.Cast<object>().ToList(),
            outputLines.Cast<object>().ToList(),
            costLines.Cast<object>().ToList(),
            lots,
            consumedLots,
            sales);
    }

    public async Task<ProductionBatchPreviewLoadDto?> LoadPreviewBatchAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var header = await connection.QuerySingleOrDefaultAsync<ProductionBatchPreviewHeaderRow>(
            new CommandDefinition(
                """
                SELECT
                    b.ProductionBatchID AS ProductionBatchId,
                    b.BatchNumber,
                    CAST(CASE WHEN b.IsPosted = 1 THEN 1 ELSE 0 END AS bit) AS IsPosted,
                    b.OutputWarehouseId,
                    w.Name AS OutputWarehouseName,
                    CAST(w.WarehouseType AS int) AS OutputWarehouseType,
                    b.FixedCost,
                    b.VariableCost
                FROM dbo.ProductionBatches b
                INNER JOIN dbo.Warehouses w ON w.WarehouseID = b.OutputWarehouseId
                WHERE b.ProductionBatchID = @Id AND b.IsDeleted = 0
                """,
                new { Id = id },
                cancellationToken: cancellationToken));

        if (header is null)
        {
            return null;
        }

        var inputs = (await connection.QueryAsync<ProductionBatchPreviewInputRow>(
            new CommandDefinition(
                """
                SELECT
                    i.ProductionInputLineID AS ProductionInputLineId,
                    i.ProductId,
                    p.Name AS ProductName,
                    i.WarehouseId,
                    wh.Name AS WarehouseName,
                    i.MeaurmentId,
                    m.Name AS MeaurmentName,
                    i.Quantity
                FROM dbo.ProductionInputLines i
                INNER JOIN dbo.Products p ON p.ProductID = i.ProductId
                INNER JOIN dbo.Warehouses wh ON wh.WarehouseID = i.WarehouseId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = i.MeaurmentId
                WHERE i.ProductionBatchId = @Id AND i.IsDeleted = 0
                ORDER BY i.ProductionInputLineID
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        var outputs = (await connection.QueryAsync<ProductionBatchPreviewOutputRow>(
            new CommandDefinition(
                """
                SELECT
                    o.ProductId,
                    p.Name AS ProductName,
                    o.MeaurmentId,
                    m.Name AS MeaurmentName,
                    o.Quantity
                FROM dbo.ProductionOutputLines o
                INNER JOIN dbo.Products p ON p.ProductID = o.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = o.MeaurmentId
                WHERE o.ProductionBatchId = @Id AND o.IsDeleted = 0
                ORDER BY o.ProductionOutputLineID
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        var costs = (await connection.QueryAsync<ProductionBatchCostRow>(
            new CommandDefinition(
                """
                SELECT
                    c.ProductionBatchCostLineID AS ProductionBatchCostLineId,
                    CAST(c.CostType AS int) AS CostType,
                    c.Description,
                    c.Amount,
                    c.AccountId
                FROM dbo.ProductionBatchCostLines c
                WHERE c.ProductionBatchId = @Id AND c.IsDeleted = 0
                ORDER BY c.ProductionBatchCostLineID
                """,
                new { Id = id },
                cancellationToken: cancellationToken))).ToList();

        // موجودی در یک کوئری برای همه ردیف‌های مصرف
        var stockKeys = inputs
            .Select(i => (i.ProductId, i.WarehouseId))
            .Distinct()
            .ToList();

        var stockMap = new Dictionary<(int ProductId, int WarehouseId), decimal>();
        if (stockKeys.Count > 0)
        {
            var productIds = stockKeys.Select(k => k.ProductId).Distinct().ToList();
            var warehouseIds = stockKeys.Select(k => k.WarehouseId).Distinct().ToList();
            var stockRows = await connection.QueryAsync<(int ProductId, int WarehouseId, decimal QuantityInBase)>(
                new CommandDefinition(
                    """
                    SELECT ProductId, WarehouseId, QuantityInBase
                    FROM dbo.InventoryStocks
                    WHERE IsDeleted = 0
                      AND ProductId IN @ProductIds
                      AND WarehouseId IN @WarehouseIds
                    """,
                    new { ProductIds = productIds, WarehouseIds = warehouseIds },
                    cancellationToken: cancellationToken));

            foreach (var row in stockRows)
            {
                stockMap[(row.ProductId, row.WarehouseId)] = row.QuantityInBase;
            }
        }

        return new ProductionBatchPreviewLoadDto
        {
            Header = header,
            InputLines = inputs,
            OutputLines = outputs,
            CostLines = costs,
            AvailableStockByKey = stockMap,
        };
    }

    private sealed class ProductionBatchHeaderRow
    {
        public int ProductionBatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ProductionDate { get; set; }
        public int? ProductionFormulaId { get; set; }
        public string? FormulaName { get; set; }
        public int? FormulaMode { get; set; }
        public int? ProductionPlanId { get; set; }
        public string? PlanLabel { get; set; }
        public int OutputWarehouseId { get; set; }
        public string OutputWarehouseName { get; set; } = string.Empty;
        public int Status { get; set; }
        public bool IsPosted { get; set; }
        public decimal TotalMaterialCostInBase { get; set; }
        public decimal TotalConversionCostInBase { get; set; }
        public decimal TotalCostInBase { get; set; }
        public int? JournalEntryId { get; set; }
        public string? Description { get; set; }
    }

    private sealed class ProductionBatchTraceHeaderRow
    {
        public int ProductionBatchId { get; set; }
        public string BatchNumber { get; set; } = string.Empty;
        public DateTime ProductionDate { get; set; }
        public string OutputWarehouseName { get; set; } = string.Empty;
        public decimal TotalMaterialCostInBase { get; set; }
        public decimal TotalConversionCostInBase { get; set; }
        public decimal TotalCostInBase { get; set; }
        public decimal FixedCost { get; set; }
        public decimal VariableCost { get; set; }
        public int? JournalEntryId { get; set; }
    }

    private sealed class ProductionTraceLotRaw
    {
        public int InventoryLotId { get; set; }
        public string LotCode { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ReceivedQuantityInBase { get; set; }
        public decimal RemainingQuantityInBase { get; set; }
        public decimal UnitCost { get; set; }
        public int? PurchaseInvoiceId { get; set; }
    }
}

public sealed class ProductionBatchListRow
{
    public int ProductionBatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ProductionDate { get; set; }
    public int? ProductionFormulaId { get; set; }
    public string? FormulaName { get; set; }
    public int? ProductionPlanId { get; set; }
    public string? PlanLabel { get; set; }
    public int OutputWarehouseId { get; set; }
    public string OutputWarehouseName { get; set; } = string.Empty;
    public int Status { get; set; }
    public bool IsPosted { get; set; }
    public decimal TotalMaterialCostInBase { get; set; }
    public decimal TotalConversionCostInBase { get; set; }
    public decimal TotalCostInBase { get; set; }
    public int InputLinesCount { get; set; }
    public int OutputLinesCount { get; set; }
    public string? Description { get; set; }
}

public sealed class ProductionBatchOptionRow
{
    public int Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime ProductionDate { get; set; }
    public int OutputWarehouseId { get; set; }
}

public sealed class ProductionBatchInputRow
{
    public int ProductionInputLineId { get; set; }
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal QuantityInBase { get; set; }
    public decimal MaterialCostInBase { get; set; }
}

public sealed class ProductionBatchOutputRow
{
    public int ProductionOutputLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal QuantityInBase { get; set; }
    public decimal UnitCostInBase { get; set; }
    public int? InventoryLotId { get; set; }
}

public sealed class ProductionBatchCostRow
{
    public int ProductionBatchCostLineId { get; set; }
    public int CostType { get; set; }
    public string? Description { get; set; }
    public decimal Amount { get; set; }
    public int? AccountId { get; set; }
}

public sealed class ProductionBatchDetailDto
{
    public int ProductionBatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ProductionDate { get; set; }
    public int? ProductionFormulaId { get; set; }
    public string? FormulaName { get; set; }
    public int? FormulaMode { get; set; }
    public int? ProductionPlanId { get; set; }
    public string? PlanLabel { get; set; }
    public int OutputWarehouseId { get; set; }
    public string OutputWarehouseName { get; set; } = string.Empty;
    public int Status { get; set; }
    public bool IsPosted { get; set; }
    public decimal TotalMaterialCostInBase { get; set; }
    public decimal TotalConversionCostInBase { get; set; }
    public decimal TotalCostInBase { get; set; }
    public int? JournalEntryId { get; set; }
    public string? Description { get; set; }
    public List<ProductionBatchInputRow> InputLines { get; set; } = [];
    public List<ProductionBatchOutputRow> OutputLines { get; set; } = [];
    public List<ProductionBatchCostRow> CostLines { get; set; } = [];
}

public sealed class ProductionBatchPreviewHeaderRow
{
    public int ProductionBatchId { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public bool IsPosted { get; set; }
    public int OutputWarehouseId { get; set; }
    public string OutputWarehouseName { get; set; } = string.Empty;
    public int OutputWarehouseType { get; set; }
    public decimal FixedCost { get; set; }
    public decimal VariableCost { get; set; }
}

public sealed class ProductionBatchPreviewInputRow
{
    public int ProductionInputLineId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public sealed class ProductionBatchPreviewOutputRow
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
}

public sealed class ProductionBatchPreviewLoadDto
{
    public ProductionBatchPreviewHeaderRow Header { get; set; } = null!;
    public List<ProductionBatchPreviewInputRow> InputLines { get; set; } = [];
    public List<ProductionBatchPreviewOutputRow> OutputLines { get; set; } = [];
    public List<ProductionBatchCostRow> CostLines { get; set; } = [];
    public Dictionary<(int ProductId, int WarehouseId), decimal> AvailableStockByKey { get; set; } = new();
}
