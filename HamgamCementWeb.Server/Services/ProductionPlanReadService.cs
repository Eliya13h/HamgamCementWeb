using System.Data.Common;
using Dapper;

namespace HamgamCementWeb.Server.Services;

public interface IProductionPlanReadService
{
    Task<(int Total, int Filtered, IReadOnlyList<ProductionPlanListRow> Rows)> GetDataTableAsync(
        int start,
        int length,
        string? search,
        string orderColumn,
        bool ascending,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductionPlanOptionRow>> GetListAsync(
        int? productId,
        int start = 0,
        int length = 100,
        CancellationToken cancellationToken = default);

    Task<ProductionPlanDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>
/// خواندن برنامه‌های تولید با Dapper + صفحه‌بندی.
/// </summary>
public class ProductionPlanReadService : IProductionPlanReadService
{
    private static readonly HashSet<string> AllowedOrderColumns = new(StringComparer.OrdinalIgnoreCase)
    {
        "PlanDate", "PlannedQuantity", "ProductName",
    };

    private readonly ISqlConnectionFactory _sql;

    public ProductionPlanReadService(ISqlConnectionFactory sql)
    {
        _sql = sql;
    }

    public async Task<(int Total, int Filtered, IReadOnlyList<ProductionPlanListRow> Rows)> GetDataTableAsync(
        int start,
        int length,
        string? search,
        string orderColumn,
        bool ascending,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var orderBy = AllowedOrderColumns.Contains(orderColumn) ? orderColumn : "PlanDate";
        var dir = ascending ? "ASC" : "DESC";
        var orderSql = orderBy switch
        {
            "PlannedQuantity" => $"p.PlannedQuantity {dir}",
            "ProductName" => $"pr.Name {dir}",
            _ => $"p.PlanDate {dir}",
        };

        const string baseWhere = "WHERE p.IsDeleted = 0";
        var where = baseWhere;
        var parameters = new DynamicParameters();

        if (!string.IsNullOrWhiteSpace(search))
        {
            where += """
                 AND (pr.Name LIKE @Search OR ISNULL(p.Notes, N'') LIKE @Search OR ISNULL(pr.Code, N'') LIKE @Search)
                """;
            parameters.Add("Search", $"%{search.Trim()}%");
        }

        var total = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"SELECT COUNT(1) FROM dbo.ProductionPlans p {baseWhere}",
                cancellationToken: cancellationToken));

        var filtered = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                $"""
                 SELECT COUNT(1)
                 FROM dbo.ProductionPlans p
                 INNER JOIN dbo.Products pr ON pr.ProductID = p.ProductId
                 {where}
                 """,
                parameters,
                cancellationToken: cancellationToken));

        parameters.Add("Offset", start);
        parameters.Add("Fetch", length);

        var rows = (await connection.QueryAsync<ProductionPlanListRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     p.ProductionPlanID AS ProductionPlanId,
                     p.PlanDate,
                     p.ProductId,
                     pr.Name AS ProductName,
                     pr.Code AS ProductCode,
                     p.MeaurmentId,
                     m.Name AS MeaurmentName,
                     p.PlannedQuantity,
                     p.Notes,
                     (SELECT COUNT(1) FROM dbo.ProductionBatches b
                      WHERE b.IsDeleted = 0 AND b.ProductionPlanId = p.ProductionPlanID) AS LinkedBatchesCount,
                     (SELECT COUNT(1) FROM dbo.ProductionBatches b
                      WHERE b.IsDeleted = 0 AND b.ProductionPlanId = p.ProductionPlanID AND b.IsPosted = 1) AS PostedBatchesCount
                 FROM dbo.ProductionPlans p
                 INNER JOIN dbo.Products pr ON pr.ProductID = p.ProductId
                 INNER JOIN dbo.Meaurments m ON m.MeaurmentID = p.MeaurmentId
                 {where}
                 ORDER BY {orderSql}, p.ProductionPlanID DESC
                 OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
                 """,
                parameters,
                cancellationToken: cancellationToken))).ToList();

        return (total, filtered, rows);
    }

    public async Task<IReadOnlyList<ProductionPlanOptionRow>> GetListAsync(
        int? productId,
        int start = 0,
        int length = 100,
        CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        var where = "WHERE p.IsDeleted = 0";
        var parameters = new DynamicParameters();
        if (productId is > 0)
        {
            where += " AND p.ProductId = @ProductId";
            parameters.Add("ProductId", productId.Value);
        }

        var fetch = length <= 0 ? 100 : Math.Min(length, 200);
        var offset = Math.Max(start, 0);
        parameters.Add("Offset", offset);
        parameters.Add("Fetch", fetch);

        var rows = await connection.QueryAsync<ProductionPlanOptionRow>(
            new CommandDefinition(
                $"""
                 SELECT
                     p.ProductionPlanID AS Value,
                     pr.Name + N' — ' + CONVERT(varchar(10), p.PlanDate, 23) + N' — ' +
                     FORMAT(p.PlannedQuantity, '0.####') + N' ' + m.Name AS Label,
                     p.ProductId,
                     p.MeaurmentId,
                     p.PlannedQuantity,
                     CONVERT(varchar(10), p.PlanDate, 23) AS PlanDate,
                     (
                         SELECT TOP (1) f.ProductionFormulaID
                         FROM dbo.ProductionFormulas f
                         WHERE f.IsDeleted = 0
                           AND f.ProductId = p.ProductId
                           AND f.IsDefault = 1
                     ) AS DefaultFormulaId
                 FROM dbo.ProductionPlans p
                 INNER JOIN dbo.Products pr ON pr.ProductID = p.ProductId
                 INNER JOIN dbo.Meaurments m ON m.MeaurmentID = p.MeaurmentId
                 {where}
                 ORDER BY p.PlanDate DESC, p.ProductionPlanID DESC
                 OFFSET @Offset ROWS FETCH NEXT @Fetch ROWS ONLY
                 """,
                parameters,
                cancellationToken: cancellationToken));

        return rows.ToList();
    }

    public async Task<ProductionPlanDetailDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = (DbConnection)await _sql.OpenAsync(cancellationToken);

        return await connection.QuerySingleOrDefaultAsync<ProductionPlanDetailDto>(
            new CommandDefinition(
                """
                SELECT
                    p.ProductionPlanID AS ProductionPlanId,
                    CONVERT(varchar(10), p.PlanDate, 23) AS PlanDate,
                    p.ProductId,
                    pr.Name AS ProductName,
                    p.MeaurmentId,
                    m.Name AS MeaurmentName,
                    p.PlannedQuantity,
                    p.Notes,
                    (
                        SELECT TOP (1) f.ProductionFormulaID
                        FROM dbo.ProductionFormulas f
                        WHERE f.IsDeleted = 0
                          AND f.ProductId = p.ProductId
                          AND f.IsDefault = 1
                    ) AS DefaultFormulaId
                FROM dbo.ProductionPlans p
                INNER JOIN dbo.Products pr ON pr.ProductID = p.ProductId
                INNER JOIN dbo.Meaurments m ON m.MeaurmentID = p.MeaurmentId
                WHERE p.ProductionPlanID = @Id AND p.IsDeleted = 0
                """,
                new { Id = id },
                cancellationToken: cancellationToken));
    }
}

public sealed class ProductionPlanListRow
{
    public int ProductionPlanId { get; set; }
    public DateTime PlanDate { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductCode { get; set; }
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public string? Notes { get; set; }
    public int LinkedBatchesCount { get; set; }
    public int PostedBatchesCount { get; set; }
}

public sealed class ProductionPlanOptionRow
{
    public int Value { get; set; }
    public string Label { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int MeaurmentId { get; set; }
    public decimal PlannedQuantity { get; set; }
    public string PlanDate { get; set; } = string.Empty;
    public int? DefaultFormulaId { get; set; }
}

public sealed class ProductionPlanDetailDto
{
    public int ProductionPlanId { get; set; }
    public string PlanDate { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int MeaurmentId { get; set; }
    public string MeaurmentName { get; set; } = string.Empty;
    public decimal PlannedQuantity { get; set; }
    public string? Notes { get; set; }
    public int? DefaultFormulaId { get; set; }
}
